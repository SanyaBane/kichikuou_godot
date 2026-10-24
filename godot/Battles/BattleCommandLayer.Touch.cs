using Godot;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

// The touch mode's rails (owner's B2, 2026-10-24; docs/ui/touch.md, "The tablet"): no hover, so the target is chosen by a
// tap on its card (or preselected, TouchTargets) and the choices stand as buttons in the scene's blurred sides — left the
// acting unit's commands without a target (its context menu's), right the attacks on the target (its banners'). The
// banners on the card are not shown. A rail button chooses at its release over it, as the context menu's items.
internal sealed partial class BattleCommandLayer
{
    // The rails stand in the scene's blurred sides (the scene's own picture is 1024 wide between them).
    private static readonly Rect2 LeftRail = new(0, 0, 128, 410), RightRail = new(1152, 0, 128, 410);
    private const float RailPad = 8, RailWidth = 112, RailRadius = 10, RailNameText = 17, RailTitleText = 13, RailText = 18;
    private const float LeftTop = 40, LeftButton = 56, LeftGap = 8;       // the acting unit's name above its buttons
    private const float RightTop = 62, RightButton = 64, RightGap = 10;   // TARGET and the target's name above them
    private static readonly Color RetreatTop = Color8(196, 72, 56), RetreatBottom = Color8(126, 30, 24);
    private static readonly Color TargetName = Color8(150, 225, 255);

    // The target frame's breathing on Android (owner's A, 2026-10-24): the brackets 2-5 out of the card and the glow
    // dimmer and brighter, once every 1.2 s; on PC (--touch included) the frame stands still.
    private const float PulseSeconds = 1.2f, PulseNear = 2, PulseFar = 5, PulseDim = 0.6f, PulseBright = 1.3f;
    private static readonly bool Pulses = OS.GetName() == "Android";

    private readonly bool _touch = Strategy.TouchMode.On;
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
        var left = Column(LeftRail, LeftTop, LeftButton, LeftGap, _railCommands.Count);
        for (int i = 0; i < _railCommands.Count; i++)
            yield return (left[i], _railCommands[i].Text, _railCommands[i].Choice, (false, i));
        var right = Column(RightRail, RightTop, RightButton, RightGap, _banners.Count);
        for (int i = 0; i < _banners.Count; i++)
            yield return (right[i], _banners[i].Text, _banners[i].Choice, (true, i));
    }

    /// <summary>
    /// <paramref name="count"/> buttons down <paramref name="rail"/> from <paramref name="top"/>: <paramref name="height"/>
    /// high, <paramref name="gap"/> apart, smaller when that many do not fit.
    /// </summary>
    private static Rect2[] Column(Rect2 rail, float top, float height, float gap, int count)
    {
        float room = rail.Size.Y - top - RailPad;
        float h = count == 0 ? height : Math.Min(height, (room - gap * (count - 1)) / count);
        float x = rail.Position.X + (rail.Size.X - RailWidth) / 2;
        return [.. Enumerable.Range(0, count).Select(i => new Rect2(x, rail.Position.Y + top + i * (h + gap), RailWidth, h))];
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

    /// <summary>The target's brackets on <paramref name="card"/>: faded when no attack reaches it, breathing on Android.</summary>
    private void DrawTouchTarget(Rect2 card)
    {
        float alpha = _banners.Count > 0 ? 1 : 0.45f;
        if (!Pulses)
        {
            DrawTargetFrame(card, alpha);
            return;
        }
        float breath = (1 - MathF.Cos(2 * MathF.PI * (float)_pulse / PulseSeconds)) / 2;     // 0 near and dim, 1 far and bright
        DrawTargetFrame(card, alpha, PulseNear + (PulseFar - PulseNear) * breath, PulseDim + (PulseBright - PulseDim) * breath);
    }

    private void DrawRails(Font? font)
    {
        float s = _scale;
        foreach (var rail in (Rect2[])[LeftRail, RightRail])
        {
            var r = new Rect2(rail.Position * s, rail.Size * s);
            DrawRect(r, _colours.Back with { A = 0.92f });
            float edge = rail == LeftRail ? r.End.X - s : r.Position.X + s;
            DrawLine(new Vector2(edge, r.Position.Y), new Vector2(edge, r.End.Y), _colours.ButtonBorder with { A = 0.8f }, Math.Max(1, 2 * s));
        }
        if (font is null || _state is not { } state)
            return;
        string acting = BattleBoard.Card(state, _commands!.Acting)?.Name ?? "";
        RailLabel(font, LeftRail, 22, acting, RailNameText, _colours.Accent);
        RailLabel(font, RightRail, 22, "TARGET", RailTitleText, _colours.Muted);
        if (_target is { } t)
        {
            RailLabel(font, RightRail, 44, BattleBoard.Card(state, t)?.Name ?? "", RailNameText, TargetName);
            if (_banners.Count == 0)
            {
                RailLabel(font, RightRail, RightTop + 20, "No attack", RailNameText, _colours.Muted);
                RailLabel(font, RightRail, RightTop + 42, "reaches it", RailNameText, _colours.Muted);
            }
        }
        foreach (var (rect, text, choice, place) in RailButtons())
        {
            bool hot = place == _hotRail;
            var (top, bottom, border) = place.Right ? (hot ? HotTop : BannerTop, hot ? HotBottom : BannerBottom, Colors.White)
                : choice.Command == FigtSelCommand.Retreat ? (RetreatTop, RetreatBottom, _colours.ButtonBorder)
                : (hot ? _colours.ButtonHover : _colours.Button, _colours.ButtonPressed, _colours.ButtonBorder);
            if (hot && !place.Right)
                (top, bottom) = (top.Lightened(0.15f), bottom.Lightened(0.15f));
            DrawRailButton(rect, text, top, bottom, border, place.Right, hot, font);
        }
    }

    /// <summary>One line centred on <paramref name="rail"/>, its middle at <paramref name="y"/> (layout pixels).</summary>
    private void RailLabel(Font font, Rect2 rail, float y, string text, float size, Color colour)
    {
        float s = _scale;
        int px = (int)Math.Round(size * s);
        float baseline = y * s + (font.GetAscent(px) - font.GetDescent(px)) / 2;
        DrawString(font, new Vector2((rail.Position.X + 4) * s, baseline), text, HorizontalAlignment.Center, (rail.Size.X - 8) * s, px, colour);
    }

    private void DrawRailButton(Rect2 r, string text, Color top, Color bottom, Color border, bool gold, bool hot, Font font)
    {
        float s = _scale;
        var shape = RoundedRect(new Rect2(r.Position * s, r.Size * s), RailRadius * s);
        var outline = shape.Append(shape[0]).ToArray();
        DrawColoredPolygon([.. shape.Select(v => v + new Vector2(2, 3) * s)], new Color(0, 0, 0, 0.3f));
        if (hot)
        {
            DrawPolyline(outline, Glow with { A = 0.18f }, 14 * s, antialiased: true);
            DrawPolyline(outline, Glow with { A = 0.35f }, 7 * s, antialiased: true);
        }
        float y0 = r.Position.Y * s, h = r.Size.Y * s;
        DrawPolygon(shape, [.. shape.Select(v => top.Lerp(bottom, (v.Y - y0) / h))]);
        DrawPolyline(outline, border, Math.Max(1, 2 * s), antialiased: true);
        int size = (int)Math.Round((gold ? BannerText : RailText) * s);
        float baseline = y0 + (h + font.GetAscent(size) - font.GetDescent(size)) / 2;
        var at = new Vector2(r.Position.X * s, baseline);
        if (gold)
            DrawStringOutline(font, at, text, HorizontalAlignment.Center, r.Size.X * s, size, Math.Max(2, (int)Math.Round(5 * s)), TextOutline);
        DrawString(font, at, text, HorizontalAlignment.Center, r.Size.X * s, size, gold ? Colors.White : _colours.ButtonText);
    }

    /// <summary>A rounded rectangle's outline (window pixels), each corner a quarter circle of <paramref name="radius"/>.</summary>
    private static Vector2[] RoundedRect(Rect2 r, float radius)
    {
        var points = new List<Vector2>();
        foreach (var (centre, start) in new[]
        {
            (r.Position + new Vector2(radius, radius), MathF.PI),
            (new Vector2(r.End.X - radius, r.Position.Y + radius), 1.5f * MathF.PI),
            (r.End - new Vector2(radius, radius), 0f),
            (new Vector2(r.Position.X + radius, r.End.Y - radius), 0.5f * MathF.PI),
        })
        {
            for (int i = 0; i <= 4; i++)
            {
                float a = start + i * MathF.PI / 8;
                points.Add(centre + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius);
            }
        }
        return [.. points];
    }
}
