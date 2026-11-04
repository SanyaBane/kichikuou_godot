using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Battles;

// The touch mode's rails (owner's B2, 2026-10-24; docs/ui/touch.md, "The tablet"): no hover, so the target is chosen by a
// tap on its card (or preselected, TouchTargets) and the choices stand as buttons in the scene's blurred sides — left the
// acting unit's commands without a target (its context menu's), right the attacks on the target (its banners'). The
// banners on the card are not shown. A rail button chooses at its release over it, as the context menu's items. Drawn in
// lacquer and gold (owner's A, 2026-10-24; docs/ui/touch.md, "The map's and the battle's buttons, round 1"): the rails as
// our panels, the rows as the command panel's, Retreat as Turn END, the attacks lit gold, the target framed in gold. With
// the damage forecast each attack button carries its damage on a dark strip (the owner's D2, 2026-10-27;
// docs/ui/damage-forecast.md, §12).
internal sealed partial class BattleCommandLayer
{
    // The rails stand beside the cards, in the screen's lower half where the thumbs are (owner's A, 2026-10-25; docs/ui/touch.md,
    // "The battle within the thumbs' reach"; the board narrower between them, BattleBoardView.Narrow). Both fill from the
    // foot up (owner, 2026-10-25): Defend, Wait, … Retreat on the left, the target's first attack under the right thumb.
    private static readonly Rect2 LeftRail = new(0, 414, SkipButtons.BattleLeftRailRight, 386), RightRail = new(1140, 414, 140, 386);
    private const float RailInset = 4, RailPad = 10, RailText = 18;     // the panel 4 in from the rail, the rows 10 off its foot
    private const float LeftTop = 60, LeftButton = 54, LeftGap = 8, LeftWidth = 116;       // under the acting unit's name
    private const float RightTop = 78, RightButton = 76, RightGap = 10, RightWidth = 116;  // under TARGET and the target's name
    private const float NameText = 19, TargetText = 17, TitleText = 13, AttackText = 19, RetreatText = 15;
    private static readonly Color TargetName = Color8(150, 225, 255);

    // The target's name under TARGET (the owner, 2026-10-27): one line at 17, its middle 46 below the rail's top, down to 14
    // when it must; a name wider still breaks at the space that balances its lines best, both at up to 15, their middles 38
    // and 55 — "2nd Assault / Leader", the widest enemy name, at 14. A name of one word only shrinks.
    private const float TargetMiddle = 46, TargetOneLine = 14, TargetTwoLines = 15, TargetFirstLine = 38, TargetSecondLine = 55;
    private static readonly Color AttackTop = Color8(255, 214, 110), AttackBottom = Color8(214, 140, 40);
    private static readonly Color AttackEdge = Color8(255, 244, 210), AttackLine = Color8(150, 100, 30), AttackInk = Color8(40, 24, 8);
    private static readonly Color HeldGlow = new(1f, 0.72f, 0.42f);  // a rail button or an order toggle held

    // The damage forecast on an attack button of 76 (the owner's D2): the name in the brush face at 17, its middle 19 below
    // the top; a dark strip from 32 below the top to 6 above the foot, 7 in from the sides; on it "Trp." and its figure, their
    // middle at 43, and "HP" and its figure at 59, each line centred, in the game's font at 15 (the word 13), the banners'
    // inks and outline. Every line's ink, its outline included, stays 2 inside the strip (the owner, 2026-10-27); a line too
    // wide shrinks to fit. A button the rail makes lower is laid out smaller in proportion.
    private const float ForecastName = 17, ForecastNameMiddle = 19, StripTop = 32, StripFoot = 6, StripSide = 7, StripRadius = 3;
    private const float TroopsLine = 43, HpLine = 59, LineFigure = 15, LineOutline = 2, StripMargin = 2;
    private static readonly Color StripColour = Color8(14, 10, 8) with { A = 225 / 255f };

    // The target's frame in the touch mode: gold brackets with a warm glow and a gold diamond on top of the card.
    private static readonly Color TouchTargetColour = Color8(222, 182, 96), TouchTargetGlow = Color8(255, 200, 100);
    private const float DiamondRadius = 7;

    // The target frame's breathing on Android (owner's A, 2026-10-24): the brackets 2-5 out of the card and the glow
    // dimmer and brighter, once every 1.2 s; on PC (--touch included) the frame stands still.
    private const float PulseSeconds = 1.2f, PulseNear = 2, PulseFar = 5, PulseDim = 0.6f, PulseBright = 1.3f;
    private static readonly bool Pulses = OS.GetName() == "Android";

    private static bool Touch => Device.TouchLayout;                // the tablet's layout, read live (docs/ui/phone-build.md, §3)
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private double _pulse;                                          // seconds since the target was set
    private BattlePlace? _target;                                   // the enemy the right rail attacks
    private IReadOnlyList<(string Text, FigtSelChoice Choice)> _railCommands = [];
    private (bool Right, int Index) _hotRail = (false, -1), _pressedRail = (false, -1);

    // The order toggles on the own cards (BattleBoardView.OrderToggleArea, which draws them; owner's B, 2026-10-25; on the
    // PC and the acting card too, owner 2026-11-04): each gives its order, the lit one Manual back — the choices the card's
    // long press or right click offers (FIGT_SEL 50-52; the acting card's Auto-Atk and Auto-Def, 8/9) —, chosen at the
    // release over the toggle as a rail button.
    private IReadOnlyList<(Rect2 Rect, FigtSelChoice Choice)> _toggles = [];
    private int _hotToggle = -1, _pressedToggle = -1;

    /// <summary>The touch mode's target (the enemy the right rail's attacks go to), or null.</summary>
    public BattlePlace? Target => Touch ? _target : null;

    /// <summary>The centre of the rail button or the order toggle that gives <paramref name="choice"/> (layout pixels), or null.</summary>
    public Vector2? RailCentre(FigtSelChoice choice)
    {
        foreach (var (rect, _, c, _) in RailButtons())
        {
            if (c == choice)
                return rect.GetCenter();
        }
        foreach (var (rect, c) in _toggles)
        {
            if (c == choice)
                return rect.GetCenter();
        }
        return null;
    }

    /// <summary>
    /// The touch mode's start of a choice: the acting unit's commands on the left, <paramref name="target"/>'s attacks on
    /// the right, the other own units' order toggles.
    /// </summary>
    private void BeginTouch(BattlePlace? target)
    {
        _railCommands = [.. _commands!.ContextMenu(_commands.Acting).Select(c => (c.Name ?? _text(c.Label).Trim(), c.Choice))];
        (_hotRail, _pressedRail) = ((false, -1), (false, -1));
        _toggles = [.. OrderToggleChoices()];
        (_hotToggle, _pressedToggle) = (-1, -1);
        SetTarget(target);
    }

    /// <summary>Each order toggle the board shows and the choice it gives: its order, or Manual when that is the unit's.</summary>
    private IEnumerable<(Rect2 Rect, FigtSelChoice Choice)> OrderToggleChoices()
    {
        foreach (var place in SlotSnapshot.Places)
        {
            if (place.Side != BattleSide.Player || BattleBoard.Card(_state!, place) is not { } card || !BattleBoardView.ShowsOrders(place, card))
                continue;
            bool acting = place == _commands!.Acting;
            var items = _commands.ContextMenu(place, card.Order);
            for (int i = 0; i < BattleBoardView.OrderToggles.Length; i++)
            {
                var order = BattleBoardView.OrderToggles[i].Order;
                var gives = acting ? (order == StandingOrder.AutoAttack ? FigtSelCommand.AutoAttack : FigtSelCommand.AutoDefend)
                    : card.Order == order ? FigtSelCommand.OrderManual
                    : order == StandingOrder.AutoAttack ? FigtSelCommand.OrderAttack : FigtSelCommand.OrderDefend;
                if (items.FirstOrDefault(c => c.Choice.Command == gives) is { } item)
                    yield return (BattleBoardView.OrderToggleArea(place, i), item.Choice);
            }
        }
    }

    private int ToggleAt(Vector2 p)
    {
        for (int i = 0; i < _toggles.Count; i++)
        {
            if (_toggles[i].Rect.HasPoint(p))
                return i;
        }
        return -1;
    }

    private void SetHotToggle(int toggle)
    {
        if (toggle == _hotToggle)
            return;
        _hotToggle = toggle;
        QueueRedraw();
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
        _pressedToggle = ToggleAt(p);
        if (_pressedToggle >= 0)
        {
            SetHotToggle(_pressedToggle);
            return;
        }
        if (CardAt(p) is { Side: BattleSide.Enemy } enemy && enemy != _target)
            SetTarget(enemy);
    }

    /// <summary>The touch mode's left release: the rail button or the order toggle pressed is chosen if the finger is still on it.</summary>
    private void TouchRelease(Vector2 p)
    {
        var pressed = _pressedRail;
        _pressedRail = (false, -1);
        if (pressed.Index >= 0 && RailAt(p) == pressed)
            Result = RailButtons().First(b => b.Place == pressed).Choice;
        SetHotRail((false, -1));
        int toggle = _pressedToggle;
        _pressedToggle = -1;
        if (toggle >= 0 && ToggleAt(p) == toggle)
            Result = _toggles[toggle].Choice;
        SetHotToggle(-1);
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
        if (_phone is { } phone)
        {
            foreach (var button in phone.IsPortrait ? DeckButtons() : PhoneRailButtons(phone))
                yield return button;
            yield break;
        }
        var left = Column(LeftRail, LeftTop, LeftButton, LeftGap, LeftWidth, _railCommands.Count);
        for (int i = 0; i < _railCommands.Count; i++)
            yield return (left[i], _railCommands[i].Text, _railCommands[i].Choice, (false, i));
        var right = Column(RightRail, RightTop, RightButton, RightGap, RightWidth, _banners.Count);
        for (int i = 0; i < _banners.Count; i++)
            yield return (right[i], _banners[i].Text, _banners[i].Choice, (true, i));
    }

    /// <summary>
    /// <paramref name="count"/> buttons <paramref name="width"/> wide up <paramref name="rail"/> from its foot, the first
    /// lowest, no higher than <paramref name="top"/>: <paramref name="height"/> high, <paramref name="gap"/> apart, smaller
    /// when that many do not fit.
    /// </summary>
    private static Rect2[] Column(Rect2 rail, float top, float height, float gap, float width, int count)
    {
        float room = rail.Size.Y - top - RailInset - RailPad;
        float h = count == 0 ? height : Math.Min(height, (room - gap * (count - 1)) / count);
        float x = rail.Position.X + (rail.Size.X - width) / 2, foot = rail.End.Y - RailInset - RailPad;
        return [.. Enumerable.Range(0, count).Select(i => new Rect2(x, foot - h - i * (h + gap), width, h))];
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

    /// <summary>The target frame's breathing, once a frame (<see cref="_Process"/>).</summary>
    private void ProcessPulse(double delta)
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
        if (_phone is { } phone)
        {
            if (phone.IsPortrait)
                DrawDeck(font);
            else
                DrawPhoneRails(phone, font);
            return;
        }
        float s = _scale;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(s, s));               // in layout pixels below
        foreach (var rail in (Rect2[])[LeftRail, RightRail])
            Ui.DrawPanel(this, rail.Grow(-RailInset), s);
        if (font is not null && _state is { } state)
        {
            RailLabel(_brush, LeftRail, 26, BattleBoard.Card(state, _commands!.Acting)?.Name ?? "", NameText, Ui.Name);
            RailRule(LeftRail, 48);
            RailLabel(_cinzel, RightRail, 22, "TARGET", TitleText, Ui.Subtitle);
            if (_target is { } t)
            {
                TargetLabel(BattleBoard.Card(state, t)?.Name ?? "");
                if (_banners.Count == 0)
                {
                    RailLabel(font, RightRail, RightTop + 20, "No attack", RailText - 2, Ui.Quiet);
                    RailLabel(font, RightRail, RightTop + 42, "reaches it", RailText - 2, Ui.Quiet);
                }
            }
            RailRule(RightRail, 66);
            foreach (var (rect, text, choice, place) in RailButtons())
            {
                bool hot = place == _hotRail;
                if (place.Right)
                    AttackButton(rect, text, hot, font, DamageOf(choice));
                else if (choice.Command == FigtSelCommand.Retreat)
                {
                    Ui.DrawRedButton(this, rect, hot, false, s);
                    Centred(_brush, text, RetreatText, rect, Color8(250, 226, 160));
                }
                else
                {
                    Ui.DrawRowBox(this, rect, hot, s);
                    Centred(font, text, RailText, rect, Ui.Text);
                }
            }
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>One line centred on <paramref name="rail"/>, its middle <paramref name="y"/> below the rail's top, smaller if it would not fit.</summary>
    private void RailLabel(Font font, Rect2 rail, float y, string text, float size, Color colour) =>
        Centred(font, text, size, new Rect2(rail.Position.X + RailInset + 3, rail.Position.Y + y - 20, rail.Size.X - 2 * RailInset - 6, 40), colour);

    /// <summary>The target's name under TARGET: one line, or two when one would have to be smaller than 14.</summary>
    private void TargetLabel(string name)
    {
        if (LabelFit(_brush, RightRail, TargetText, TargetOneLine, name) == 0 && Split(name) is { } lines)
        {
            int px = Math.Max(LabelFit(_brush, RightRail, TargetTwoLines, 11, lines.First, lines.Second), 11);
            RailLabel(_brush, RightRail, TargetFirstLine, lines.First, px, TargetName);
            RailLabel(_brush, RightRail, TargetSecondLine, lines.Second, px, TargetName);
        }
        else
            RailLabel(_brush, RightRail, TargetMiddle, name, TargetText, TargetName);
    }

    /// <summary>
    /// The largest size from <paramref name="size"/> down to <paramref name="least"/> at which each of
    /// <paramref name="lines"/> fits a label on <paramref name="rail"/> (<see cref="RailLabel"/>'s width less
    /// <see cref="Centred"/>'s margin), else 0.
    /// </summary>
    private static int LabelFit(Font font, Rect2 rail, float size, float least, params string[] lines)
    {
        float room = rail.Size.X - 2 * RailInset - 6 - 10;
        for (int px = (int)size; px >= least; px--)
        {
            if (lines.All(line => font.GetStringSize(line, fontSize: px).X <= room))
                return px;
        }
        return 0;
    }

    /// <summary><paramref name="name"/> broken at the space that makes its wider line narrowest, or null for one word.</summary>
    private (string First, string Second)? Split(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        float Wider((string First, string Second) l) =>
            Math.Max(_brush.GetStringSize(l.First, fontSize: (int)TargetTwoLines).X, _brush.GetStringSize(l.Second, fontSize: (int)TargetTwoLines).X);
        return words.Length < 2 ? null
            : Enumerable.Range(1, words.Length - 1).Select(i => (string.Join(' ', words[..i]), string.Join(' ', words[i..]))).MinBy(Wider);
    }

    /// <summary>A dim gold rule across the rail's panel <paramref name="y"/> below the rail's top.</summary>
    private void RailRule(Rect2 rail, float y) =>
        DrawLine(new Vector2(rail.Position.X + 20, rail.Position.Y + y), new Vector2(rail.End.X - 20, rail.Position.Y + y), Ui.DimGold,
            Lacquer.Line(1, _scale));

    /// <summary>An order toggle held (the board draws the toggle): a glow round it and a bright frame, as a held rail button.</summary>
    private void DrawHeldToggle(Rect2 r)
    {
        float s = _scale;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(s, s));
        DrawRect(r.Grow(5), HeldGlow with { A = 0.18f });
        DrawRect(r.Grow(2.5f), HeldGlow with { A = 0.35f });
        DrawRect(r, Ui.RowHover with { A = 0.5f });
        DrawRect(r, Ui.HoverLine, false, Lacquer.Line(2, s));
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>
    /// An attack on the right rail: lit gold, its name in the brush face; brighter with a glow while held. While the
    /// forecast shows the name stands at the top and the strip under it carries <paramref name="damage"/> (empty while
    /// its forecast runs).
    /// </summary>
    private void AttackButton(Rect2 r, string text, bool hot, Font font, BannerDamage? damage)
    {
        float k = _scale;
        if (hot)
        {
            DrawRect(r.Grow(6), HeldGlow with { A = 0.18f });
            DrawRect(r.Grow(3), HeldGlow with { A = 0.35f });
        }
        var (top, bottom) = hot ? (AttackTop.Lightened(0.2f), AttackBottom.Lightened(0.15f)) : (AttackTop, AttackBottom);
        DrawPolygon([r.Position, new(r.End.X, r.Position.Y), r.End, new(r.Position.X, r.End.Y)], [top, top, bottom, bottom]);
        DrawRect(r, AttackEdge, false, Lacquer.Line(2, k));
        DrawRect(r.Grow(-4), AttackLine, false, Lacquer.Line(1, k));
        float q = r.Size.Y / RightButton;                               // a lower button, its forecast as much smaller
        if (!_forecastShown)
        {
            Centred(_brush, text, _phone is null ? AttackText : AttackText * q, r, AttackInk);   // the phone's at its size
            return;
        }
        Centred(_brush, text, ForecastName * q, new Rect2(r.Position.X, r.Position.Y, r.Size.X, 2 * ForecastNameMiddle * q), AttackInk);
        var strip = new Rect2(r.Position.X + StripSide, r.Position.Y + StripTop * q,
            r.Size.X - 2 * StripSide, r.Size.Y - (StripTop + StripFoot) * q);
        int radius = (int)Math.Round(StripRadius * q);
        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = StripColour, AntiAliasing = true,
            CornerRadiusTopLeft = radius, CornerRadiusTopRight = radius, CornerRadiusBottomLeft = radius, CornerRadiusBottomRight = radius,
        }, strip);
        if (damage is null)
            return;
        float room = strip.Size.X - 2 * StripMargin - LineOutline;
        DamageLine(font, BannerDamage.TroopsWord, damage.Troops, strip.GetCenter().X, r.Position.Y + TroopsLine * q, LineFigure * q, room, TroopsInk);
        DamageLine(font, BannerDamage.HpWord, damage.Hp, strip.GetCenter().X, r.Position.Y + HpLine * q, LineFigure * q, room, HpInk);
    }

    /// <summary>
    /// One line of an attack button's forecast (layout pixels, under the rails' transform): <paramref name="word"/> and
    /// <paramref name="figure"/> centred on <paramref name="centre"/>, their middle at <paramref name="middle"/>, the figure
    /// at <paramref name="size"/> and the word 2 smaller, shrunk to fit <paramref name="room"/>; nothing for no figure.
    /// </summary>
    private void DamageLine(Font font, string word, string? figure, float centre, float middle, float size, float room, Color ink)
    {
        if (figure is null)
            return;                                                     // its line stays empty: the other never moves
        int px = (int)Math.Round(size), tag = px - 2;
        float Width(int f, int t) => font.GetStringSize(word + " ", fontSize: t).X + font.GetStringSize(figure, fontSize: f).X;
        while (px > 9 && Width(px, tag) > room)
            (px, tag) = (px - 1, tag - 1);
        float left = centre - Width(px, tag) / 2;
        foreach (var (text, fs) in new[] { (word, tag), (figure, px) })
        {
            var at = new Vector2(left, middle + (font.GetAscent(fs) - font.GetDescent(fs)) / 2);
            DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, fs, (int)LineOutline, TextOutline);
            DrawString(font, at, text, HorizontalAlignment.Left, -1, fs, ink);
            left += font.GetStringSize(text + " ", fontSize: fs).X;
        }
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
