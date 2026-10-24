using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

// The touch mode's rails (owner's B2, 2026-10-24; docs/ui/touch.md, "The tablet"): no hover, so the target is chosen by a
// tap on its card (or preselected, TouchTargets) and the choices stand as buttons in the scene's blurred sides — left the
// acting unit's commands without a target (its context menu's), right the attacks on the target (its banners'). The
// banners on the card are not shown. A rail button chooses at its release over it, as the context menu's items. Drawn in
// lacquer and gold (owner's A, 2026-10-24; docs/ui/touch.md, "The map's and the battle's buttons, round 1"): the rails as
// our panels, the rows as the command panel's, Retreat as Turn END, the attacks lit gold, the target framed in gold.
internal sealed partial class BattleCommandLayer
{
    // The rails stand in the scene's blurred sides (the scene's own picture is 1024 wide between them).
    private static readonly Rect2 LeftRail = new(0, 0, 128, 410), RightRail = new(1152, 0, 128, 410);
    private const float RailInset = 4, RailPad = 10, RailText = 18;     // the panel 4 in from the rail, the rows 10 off its foot
    private const float LeftTop = 64, LeftButton = 56, LeftGap = 10, LeftWidth = 104;      // under the acting unit's name
    private const float RightTop = 80, RightButton = 66, RightGap = 10, RightWidth = 108;  // under TARGET and the target's name
    private const float NameText = 19, TargetText = 17, TitleText = 13, AttackText = 19, RetreatText = 15;
    private static readonly Color TargetName = Color8(150, 225, 255);
    private static readonly Color AttackTop = Color8(255, 214, 110), AttackBottom = Color8(214, 140, 40);
    private static readonly Color AttackEdge = Color8(255, 244, 210), AttackLine = Color8(150, 100, 30), AttackInk = Color8(40, 24, 8);

    // The target's frame in the touch mode: gold brackets with a warm glow and a gold diamond on top of the card.
    private static readonly Color TouchTargetColour = Color8(222, 182, 96), TouchTargetGlow = Color8(255, 200, 100);
    private const float DiamondRadius = 7;

    // The target frame's breathing on Android (owner's A, 2026-10-24): the brackets 2-5 out of the card and the glow
    // dimmer and brighter, once every 1.2 s; on PC (--touch included) the frame stands still.
    private const float PulseSeconds = 1.2f, PulseNear = 2, PulseFar = 5, PulseDim = 0.6f, PulseBright = 1.3f;
    private static readonly bool Pulses = OS.GetName() == "Android";

    private readonly bool _touch = TouchMode.On;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private double _pulse;                                          // seconds since the target was set
    private BattlePlace? _target;                                   // the enemy the right rail attacks
    private IReadOnlyList<(string Text, FigtSelChoice Choice)> _railCommands = [];
    private (bool Right, int Index) _hotRail = (false, -1), _pressedRail = (false, -1);

    /// <summary>The touch mode's target (the enemy the right rail's attacks go to), or null.</summary>
    public BattlePlace? Target => _touch ? _target : null;

    /// <summary>The centre of the rail button that gives <paramref name="choice"/> (layout pixels), or null.</summary>
    public Vector2? RailCentre(FigtSelChoice choice)
    {
        foreach (var (rect, _, c, _) in RailButtons())
        {
            if (c == choice)
                return rect.GetCenter();
        }
        return null;
    }

    /// <summary>The touch mode's start of a choice: the acting unit's commands on the left, <paramref name="target"/>'s attacks on the right.</summary>
    private void BeginTouch(BattlePlace? target)
    {
        _railCommands = [.. _commands!.ContextMenu(_commands.Acting).Select(c => (c.Name ?? _text(c.Label).Trim(), c.Choice))];
        (_hotRail, _pressedRail) = ((false, -1), (false, -1));
        SetTarget(target);
    }

    /// <summary>Makes <paramref name="target"/> the target: its card framed, the scene showing it, its attacks on the right rail.</summary>
    private void SetTarget(BattlePlace? target)
    {
        _target = target;
        _pulse = 0;                                                 // a new target breathes from the near end
        _hovered = target;
        _banners = target is { } t ? [.. _commands!.Banners(t).Select(b => (b.Name ?? _text(b.Label).Trim(), b.Choice))] : [];
        _hotBanner = -1;
        QueueRedraw();
    }

    /// <summary>The touch mode's left press: a rail button is held (chosen at its release), an enemy card becomes the target.</summary>
    private void TouchPress(Vector2 p)
    {
        _pressedRail = RailAt(p);
        if (_pressedRail.Index >= 0)
        {
            SetHotRail(_pressedRail);
            return;
        }
        if (CardAt(p) is { Side: BattleSide.Enemy } enemy && enemy != _target)
            SetTarget(enemy);
    }

    /// <summary>The touch mode's left release: the rail button pressed is chosen if the finger is still on it.</summary>
    private void TouchRelease(Vector2 p)
    {
        var pressed = _pressedRail;
        _pressedRail = (false, -1);
        if (pressed.Index >= 0 && RailAt(p) == pressed)
            Result = RailButtons().First(b => b.Place == pressed).Choice;
        SetHotRail((false, -1));
    }

    private void SetHotRail((bool, int) rail)
    {
        if (rail == _hotRail)
            return;
        _hotRail = rail;
        QueueRedraw();
    }

    // ------------------------------------------------------------------ geometry (layout pixels)

    /// <summary>Every rail button: its rectangle, its label, its choice, which rail and its place there.</summary>
    private IEnumerable<(Rect2 Rect, string Text, FigtSelChoice Choice, (bool Right, int Index) Place)> RailButtons()
    {
        var left = Column(LeftRail, LeftTop, LeftButton, LeftGap, LeftWidth, _railCommands.Count);
        for (int i = 0; i < _railCommands.Count; i++)
            yield return (left[i], _railCommands[i].Text, _railCommands[i].Choice, (false, i));
        var right = Column(RightRail, RightTop, RightButton, RightGap, RightWidth, _banners.Count);
        for (int i = 0; i < _banners.Count; i++)
            yield return (right[i], _banners[i].Text, _banners[i].Choice, (true, i));
    }

    /// <summary>
    /// <paramref name="count"/> buttons <paramref name="width"/> wide down <paramref name="rail"/> from <paramref name="top"/>:
    /// <paramref name="height"/> high, <paramref name="gap"/> apart, smaller when that many do not fit.
    /// </summary>
    private static Rect2[] Column(Rect2 rail, float top, float height, float gap, float width, int count)
    {
        float room = rail.Size.Y - top - RailInset - RailPad;
        float h = count == 0 ? height : Math.Min(height, (room - gap * (count - 1)) / count);
        float x = rail.Position.X + (rail.Size.X - width) / 2;
        return [.. Enumerable.Range(0, count).Select(i => new Rect2(x, rail.Position.Y + top + i * (h + gap), width, h))];
    }

    private (bool Right, int Index) RailAt(Vector2 p)
    {
        foreach (var (rect, _, _, place) in RailButtons())
        {
            if (rect.HasPoint(p))
                return place;
        }
        return (false, -1);
    }

    // ------------------------------------------------------------------ drawing

    public override void _Process(double delta)
    {
        if (!Pulses || _commands is null || _target is null || _menu is not null)
            return;
        _pulse = (_pulse + delta) % PulseSeconds;
        QueueRedraw();
    }

    /// <summary>
    /// The target's gold brackets on <paramref name="card"/> and the diamond on its top edge: faded when no attack reaches
    /// it, breathing on Android.
    /// </summary>
    private void DrawTouchTarget(Rect2 card)
    {
        float alpha = _banners.Count > 0 ? 1 : 0.45f, outset = TargetOut, glow = 1;
        if (Pulses)
        {
            float breath = (1 - MathF.Cos(2 * MathF.PI * (float)_pulse / PulseSeconds)) / 2;     // 0 near and dim, 1 far and bright
            (outset, glow) = (PulseNear + (PulseFar - PulseNear) * breath, PulseDim + (PulseBright - PulseDim) * breath);
        }
        DrawTargetFrame(card, alpha, outset, glow, TouchTargetColour, TouchTargetGlow);
        float s = _scale;
        var at = new Vector2(card.GetCenter().X * s, card.Position.Y * s - outset * s);
        float r = DiamondRadius * s;
        Vector2[] diamond = [at + new Vector2(0, -r), at + new Vector2(r, 0), at + new Vector2(0, r), at + new Vector2(-r, 0)];
        DrawCircle(at, r * 1.8f, TouchTargetGlow with { A = 0.25f * alpha * glow });
        DrawColoredPolygon(diamond, TouchTargetColour with { A = alpha });
        DrawPolyline([.. diamond, diamond[0]], AttackEdge with { A = alpha }, Math.Max(1, 1.5f * s), antialiased: true);
    }

    private void DrawRails(Font? font)
    {
        float s = _scale;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(s, s));               // in layout pixels below
        foreach (var rail in (Rect2[])[LeftRail, RightRail])
            Lacquer.DrawPanel(this, rail.Grow(-RailInset), s);
        if (font is not null && _state is { } state)
        {
            RailLabel(_brush, LeftRail, 30, BattleBoard.Card(state, _commands!.Acting)?.Name ?? "", NameText, Lacquer.Name);
            RailRule(LeftRail, 52);
            RailLabel(_cinzel, RightRail, 24, "TARGET", TitleText, Lacquer.Subtitle);
            if (_target is { } t)
            {
                RailLabel(_brush, RightRail, 48, BattleBoard.Card(state, t)?.Name ?? "", TargetText, TargetName);
                if (_banners.Count == 0)
                {
                    RailLabel(font, RightRail, RightTop + 20, "No attack", RailText - 2, Lacquer.Quiet);
                    RailLabel(font, RightRail, RightTop + 42, "reaches it", RailText - 2, Lacquer.Quiet);
                }
            }
            RailRule(RightRail, 68);
            foreach (var (rect, text, choice, place) in RailButtons())
            {
                bool hot = place == _hotRail;
                if (place.Right)
                    AttackButton(rect, text, hot);
                else if (choice.Command == FigtSelCommand.Retreat)
                {
                    Lacquer.DrawRedButton(this, rect, hot, false, s);
                    Centred(_brush, text, RetreatText, rect, Color8(250, 226, 160));
                }
                else
                {
                    Lacquer.DrawRowBox(this, rect, hot, s);
                    Centred(font, text, RailText, rect, Lacquer.Text);
                }
            }
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>One line centred on <paramref name="rail"/>, its middle at <paramref name="y"/>, smaller if it would not fit.</summary>
    private void RailLabel(Font font, Rect2 rail, float y, string text, float size, Color colour) =>
        Centred(font, text, size, new Rect2(rail.Position.X + RailInset + 3, y - 20, rail.Size.X - 2 * RailInset - 6, 40), colour);

    /// <summary>A dim gold rule across the rail's panel at <paramref name="y"/>.</summary>
    private void RailRule(Rect2 rail, float y) =>
        DrawLine(new Vector2(rail.Position.X + 20, y), new Vector2(rail.End.X - 20, y), Lacquer.DimGold, Lacquer.Line(1, _scale));

    /// <summary>An attack on the right rail: lit gold, its name in the brush face; brighter with a glow while held.</summary>
    private void AttackButton(Rect2 r, string text, bool hot)
    {
        float k = _scale;
        if (hot)
        {
            DrawRect(r.Grow(6), Glow with { A = 0.18f });
            DrawRect(r.Grow(3), Glow with { A = 0.35f });
        }
        var (top, bottom) = hot ? (AttackTop.Lightened(0.2f), AttackBottom.Lightened(0.15f)) : (AttackTop, AttackBottom);
        DrawPolygon([r.Position, new(r.End.X, r.Position.Y), r.End, new(r.Position.X, r.End.Y)], [top, top, bottom, bottom]);
        DrawRect(r, AttackEdge, false, Lacquer.Line(2, k));
        DrawRect(r.Grow(-4), AttackLine, false, Lacquer.Line(1, k));
        Centred(_brush, text, AttackText, r, AttackInk);
    }

    /// <summary><paramref name="text"/> centred in <paramref name="r"/> at <paramref name="size"/>, smaller if it would not fit.</summary>
    private void Centred(Font font, string text, float size, Rect2 r, Color colour)
    {
        int px = (int)size;
        while (px > 11 && font.GetStringSize(text, fontSize: px).X > r.Size.X - 10)
            px--;
        DrawString(font, new Vector2(r.Position.X, Lacquer.Baseline(r, font, px)), text, HorizontalAlignment.Center, r.Size.X, px, colour);
    }
}
