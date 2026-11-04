using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Our own command input on the battle screen (our interface; docs/ui/battle-screen.md, step 4,
/// "Next — our own command input"): <c>FIGT_SEL</c> answered without the original's prompt and popups.
/// The attacks are banners on the enemy card under the pointer (<see cref="BattleCommands.Banners"/>), the
/// only way to attack; a right click on a card opens its context menu at the pointer
/// (<see cref="BattleCommands.ContextMenu"/>). Every choice is one the core's menu offers — the rules stay
/// the core's. With Option → Tweaks → "Damage forecast" each banner carries the damage its attack is likely to deal
/// (<see cref="DamageForecasts"/>; docs/ui/damage-forecast.md). Points are layout pixels of the 1280×800 screen
/// (<see cref="BattleBoardView"/>).
/// </summary>
internal sealed partial class BattleCommandLayer : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    // The banners (as Sengoku Rance's): 200×40, 5 apart, one column 22 out of the card's left edge; their places are
    // fixed from the top whatever their number (owner, 2026-10-13), three of them centred on the card's height. In the
    // player's blue, the hovered one a pale sky blue glowing (the owner's C1, 2026-10-26; docs/ui/damage-forecast.md, §11).
    private static readonly Vector2 BannerSize = new(200, 40);
    private const float BannerGap = 5, BannerOut = 22, BannerSlant = 12, BannerPoint = 18, BannerText = 20;
    private const int BannerPlaces = 3;
    private const float BannerAlpha = 0.8f;
    private static readonly Color BannerTop = Color8(122, 166, 245), BannerBottom = Color8(50, 90, 198);
    private static readonly Color HotTop = Color8(192, 226, 255), HotBottom = Color8(92, 152, 246);
    private static readonly Color BannerGlow = Color8(170, 212, 255);
    private static readonly Color TextOutline = Color8(6, 10, 30) with { A = 0.95f };

    // The damage forecast on a banner (the owner's B2, 2026-10-26, laid out as round F's F2, 2026-10-27;
    // docs/ui/damage-forecast.md, §10 and §14): 48 high, so three stand 8 over the card's height; the label 19, its middle
    // 14 below the banner's top; the damage under it, its middle at 34: "Trp." and its figure from 18, "HP" and its figure
    // from 110, each in the bars' hue lightened (the owner's C1), the figure 16 and its word 13, outlined 1.5 px; a figure
    // too wide for its place shrinks to fit.
    private static readonly Vector2 ForecastBannerSize = new(200, 48);
    private const float ForecastLabel = 19, ForecastLabelMiddle = 14, ForecastFigure = 16, ForecastWord = 13, ForecastMiddle = 34;
    private const float ForecastTroopsAt = 18, ForecastHpAt = 110, ForecastGap = 8;
    // Godot's outline size for the damage's 1.5 px ring: the size is about four times the ring's width (3, as first built,
    // drew one under 1 px; measured on the screen gallery's dump against the mock-up, docs/ui/damage-forecast.md, §14)
    private const float ForecastOutline = 6;
    private static readonly Color TroopsInk = Color8(232, 146, 140), HpInk = Color8(156, 216, 156);

    // The target's frame (as Sengoku Rance's): cyan brackets on the corners of the enemy card whose banners show — each
    // an L bent round a corner of 7, its arms 60 along the card and 46 down it — 3 out of the card's edge, 4 wide, a white
    // line inside and a cyan glow; under the banners. Fixed colours, as the banners', so it never reads as the acting
    // card's frame (the palette's accent).
    private const float TargetOut = 3, TargetWidth = 4, TargetRadius = 7, TargetGlow = 10, TargetArmX = 60, TargetArmY = 46;
    private static readonly Color TargetColour = Color8(70, 205, 255), TargetInner = new(1, 1, 1, 0.85f);

    // The context menu: rows of the cards' text size, a column for the current order's mark.
    private const float PcRowHeight = 30, PcMenuPad = 8, PcMarkWidth = 18, PcMenuMinWidth = 130, PcMenuText = 17;

    // The cards' menu: the PC's, or on the phone at a finger's size (docs/ui/phone-build.md, steps L4 and P4).
    private int _menuCount = 1;
    private float RowHeight => _phone is null ? PcRowHeight
        : _phone.IsPortrait ? PhoneBattleStatesPortrait.MenuRowHeight(_menuCount, _height) : PhoneBattleStates.MenuRowHeight(_menuCount);
    private float MenuPad => _phone is null ? PcMenuPad : P(PhoneBattleStates.MenuPad);
    private float MarkWidth => _phone is null ? PcMarkWidth : P(PhoneBattleStates.MenuMark);
    private float MenuMinWidth => _phone is null ? PcMenuMinWidth : P(PhoneBattleStates.MenuMinWidth);
    private float MenuText => _phone is null ? PcMenuText : P(PhoneBattleStates.MenuText);

    private Func<BattlePlace, Rect2> _cardArea = _ => default;
    private float _scale = 1;
    private BattleCommands? _commands;
    private GameState? _state;
    private Func<ScriptRef, string> _text = _ => "";
    private BattlePlace? _hovered;                                  // the card under the pointer (or whose banners it is on)
    private IReadOnlyList<(string Text, FigtSelChoice Choice)> _banners = [];
    private int _hotBanner = -1;
    private (BattlePlace Place, Rect2 Box, IReadOnlyList<(string Text, FigtSelChoice Choice, bool Current)> Items)? _menu;
    private int _hotItem = -1;
    private int _pressedItem = -1;                                  // the item a left press began on (chosen at its release)
    private DamageForecasts? _forecasts;                            // this choice's, while the forecast shows
    private int _forecastsDrawn = -1;                               // how many of them the last drawing had
    private bool _forecastShown;

    public BattleCommandLayer()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 10;
    }

    private static Color Color8(int r, int g, int b) => Color.Color8((byte)r, (byte)g, (byte)b);

    /// <summary>Whether the player is choosing through this layer.</summary>
    public bool Active => _commands is not null;

    /// <summary>The answer once the player has chosen.</summary>
    public FigtSelChoice? Result { get; private set; }

    /// <summary>
    /// Whether the banners show the damage forecast: null (the default) follows Option → Tweaks → "Damage forecast"
    /// (<see cref="BattleScreenSettings.Forecast"/>); the screen gallery and unattended play's dumps set it. A change
    /// applies at once.
    /// </summary>
    public bool? Forecast
    {
        get => _forecast;
        set
        {
            _forecast = value;
            if (_commands is null)
                return;
            StartForecasts();
            QueueRedraw();
        }
    }

    private bool? _forecast;

    /// <summary>Whether the damage forecast shows now.</summary>
    private bool ForecastOn => _forecast ?? BattleScreenSettings.Get(BattleScreenSettings.Forecast) >= 0.5;

    /// <summary>The forecasts of this choice running or done (null: none started), for a dump that waits for them.</summary>
    public Task? ForecastsDone => _forecasts?.Done;

    /// <summary>The card under the pointer, or whose banners it is on (the scene shows that enemy).</summary>
    public BattlePlace? Hovered => _hovered;

    /// <summary>
    /// The card lit as under the pointer: the one whose menu is open, else the hovered card — an own card, or
    /// an enemy's with banners (one no attack reaches stays grey).
    /// </summary>
    public BattlePlace? Lit => _menu?.Place ?? (_hovered is { Side: BattleSide.Enemy } && _banners.Count == 0 ? null : _hovered);

    /// <summary>Whether a context menu is open.</summary>
    public bool MenuOpen => _menu is not null;

    /// <summary>The open menu's items (unattended play's checks).</summary>
    public IReadOnlyList<FigtSelChoice> MenuItems => _menu is { } m ? [.. m.Items.Select(i => i.Choice)] : [];

    /// <summary>The banners shown (unattended play's checks).</summary>
    public IReadOnlyList<FigtSelChoice> Banners => [.. _banners.Select(b => b.Choice)];

    /// <summary>The centre of the shown banner that gives <paramref name="choice"/> (layout pixels), or null.</summary>
    public Vector2? BannerCentre(FigtSelChoice choice)
    {
        int i = _banners.Select(b => b.Choice).ToList().IndexOf(choice);
        return _hovered is { } target && i >= 0 ? BannerRect(target, i).GetCenter() : null;
    }

    /// <summary>The centre of the open menu's item <paramref name="item"/> (layout pixels), or null.</summary>
    public Vector2? ItemCentre(int item) => _menu is { } m && item >= 0 && item < m.Items.Count
        ? new Vector2(m.Box.GetCenter().X, m.Box.Position.Y + MenuPad + (item + 0.5f) * RowHeight)
        : null;

    /// <summary>
    /// Starts a choice from the core's menu; <paramref name="text"/> reads a label. In the touch mode
    /// <paramref name="target"/> is the enemy preselected as the target.
    /// </summary>
    public void Begin(BattleCommands commands, GameState state, Func<ScriptRef, string> text, BattlePlace? target = null)
    {
        (_commands, _state, _text) = (commands, state, text);
        Result = null;
        (_hovered, _banners, _hotBanner, _menu, _hotItem) = (null, [], -1, null, -1);
        (_forecasts, _forecastsDrawn) = (null, -1);
        if (Touch)
            BeginTouch(target);
        else
        {
            _toggles = [.. OrderToggleChoices()];                   // the PC's order toggles, hovered and clicked
            (_hotToggle, _pressedToggle) = (-1, -1);
        }
        StartForecasts();
        QueueRedraw();
    }

    /// <summary>
    /// Starts this choice's forecasts once the forecast shows (at the start, or when the option is switched on under it),
    /// on the state as <c>FIGT_SEL</c> asks: the touch mode's target's banners first.
    /// </summary>
    private void StartForecasts()
    {
        _forecastShown = ForecastOn;
        if (_forecastShown && _forecasts is null && _commands is { } commands && _state is { } state)
        {
            HangWatch.Note("the damage forecasts start, off this thread");
            _forecasts = DamageForecasts.Start(state, commands, Touch ? _target : null, DemonReachChoice.Chosen);
        }
    }

    public override void _Process(double delta)
    {
        if (_commands is null)
            return;
        if (ForecastOn != _forecastShown)
        {
            StartForecasts();
            QueueRedraw();
        }
        if (_forecasts is { } forecasts && forecasts.Count != _forecastsDrawn)
        {
            _forecastsDrawn = forecasts.Count;
            if (forecasts.Done.IsFaulted)
                GD.PushError($"Kichikuou: the damage forecast failed: {forecasts.Done.Exception?.GetBaseException()}");
            QueueRedraw();
        }
        ProcessPulse(delta);
    }

    /// <summary>The damage of <paramref name="choice"/>'s banner while the forecast shows and its forecast is done, else null.</summary>
    private BannerDamage? DamageOf(FigtSelChoice choice) => _forecastShown ? _forecasts?.For(choice) : null;

    /// <summary>Ends the choice (answered, or the interface changed): nothing is shown.</summary>
    public void End()
    {
        _commands = null;
        _state = null;
        (_hovered, _banners, _hotBanner, _menu, _hotItem) = (null, [], -1, null, -1);
        (_forecasts, _forecastsDrawn) = (null, -1);                 // a forecast still running is left to finish unread
        QueueRedraw();
    }

    /// <summary>
    /// The layout: <paramref name="cardArea"/> gives a card's rectangle, <paramref name="scale"/> screen pixels to a layout
    /// pixel; <paramref name="phone"/> the phone's frame held landscape (the rails in its sides) or upright (the deck at the
    /// board's foot, <paramref name="height"/> layout pixels down), else null.
    /// </summary>
    public void Layout(Func<BattlePlace, Rect2> cardArea, float scale, Original.Content.Devices.PhoneFrame? phone = null, float height = 800)
    {
        bool turned = phone != _phone;
        _cardArea = cardArea;
        _scale = scale;
        _phone = phone;
        _height = height;
        if (turned && _commands is not null)
        {
            if (_menu is not null && Touch)
                CloseMenu();                                        // a turn: the cards' menu stood at the old layout's point
            _toggles = [.. OrderToggleChoices()];                   // the order toggles where the board draws them now
            (_hotToggle, _pressedToggle) = (-1, -1);
        }
        QueueRedraw();
    }

    // ------------------------------------------------------------------ input

    /// <summary>The pointer moved to layout point <paramref name="p"/>.</summary>
    public void Move(Vector2 p)
    {
        if (_commands is null)
            return;
        if (_menu is { } menu)
        {
            SetHotItem(ItemAt(menu, p));
            return;
        }
        if (Touch)
        {
            SetHotRail(_pressedRail.Index >= 0 && RailAt(p) == _pressedRail ? _pressedRail : (false, -1));
            SetHotToggle(_pressedToggle >= 0 && ToggleAt(p) == _pressedToggle ? _pressedToggle : -1);
            return;                                                 // no hover: the target is a tap's
        }
        int toggle = ToggleAt(p);                                   // the PC's toggles light under the pointer, the pressed one alone
        SetHotToggle(_pressedToggle < 0 || toggle == _pressedToggle ? toggle : -1);
        var place = _hovered is { Side: BattleSide.Enemy } target && BannerAt(target, p) >= 0 ? target : CardAt(p);
        if (place != _hovered)
        {
            _hovered = place;
            _banners = place is { } t
                ? [.. _commands.Banners(t).Select(b => (b.Name ?? _text(b.Label).Trim(), b.Choice))]
                : [];
            QueueRedraw();
        }
        int hot = _hovered is { } h ? BannerAt(h, p) : -1;
        if (hot != _hotBanner)
        {
            _hotBanner = hot;
            QueueRedraw();
        }
    }

    /// <summary>
    /// A button pressed at <paramref name="p"/>: a left press on a banner chooses it (as the original's click
    /// on a target, at the press), one on a menu item chooses it at its release there (as the original's
    /// popups), one outside the open menu closes it, anywhere else it does nothing; a right press on a card
    /// opens its menu there (closing an open one), elsewhere it closes the menu.
    /// </summary>
    public void Press(bool right, Vector2 p)
    {
        if (_commands is null)
            return;
        (_pressedItem, _pressedToggle) = (-1, -1);
        if (_menu is { } menu)
        {
            int item = ItemAt(menu, p);
            if (!right && item >= 0)
            {
                _pressedItem = item;
                return;
            }
            if (!right && menu.Box.HasPoint(p))
                return;                                             // the menu's frame: nothing
            CloseMenu();
            if (!right)
            {
                Move(p);
                return;
            }
        }
        if (!right && Touch)
        {
            TouchPress(p);
            return;
        }
        if (!right)
        {
            if (_hovered is { } target && BannerAt(target, p) is var b and >= 0)
                Result = _banners[b].Choice;
            else if ((_pressedToggle = ToggleAt(p)) >= 0)
                SetHotToggle(_pressedToggle);                       // an order toggle: chosen at its release there
            return;
        }
        var place = _hovered is { Side: BattleSide.Enemy } t && BannerAt(t, p) >= 0 ? t : CardAt(p);
        if (place is { } card)
            OpenMenu(card, p);
        Move(p);
    }

    /// <summary>A button released at <paramref name="p"/>: a left press that began on a menu item chooses it if the pointer is still on it.</summary>
    public void Release(bool right, Vector2 p)
    {
        if (_commands is null || right)
            return;
        if (Touch && _menu is null)
        {
            TouchRelease(p);
            return;
        }
        if (_menu is { } menu && _pressedItem >= 0 && ItemAt(menu, p) == _pressedItem)
            Result = menu.Items[_pressedItem].Choice;
        _pressedItem = -1;
        int toggle = _pressedToggle;
        _pressedToggle = -1;
        if (_menu is null && toggle >= 0 && ToggleAt(p) == toggle)
            Result = _toggles[toggle].Choice;
    }

    /// <summary>Esc: closes the open menu (true), else does nothing (false).</summary>
    public bool Escape()
    {
        if (_menu is null)
            return false;
        CloseMenu();
        return true;
    }

    private void OpenMenu(BattlePlace place, Vector2 at)
    {
        var order = place.Side == BattleSide.Player && _state is { } s ? BattleBoard.Card(s, place)?.Order : null;
        var items = _commands!.ContextMenu(place, order).Select(c => (c.Name ?? _text(c.Label).Trim(), c.Choice, c.Current)).ToList();
        if (items.Count == 0)
            return;
        _menuCount = items.Count;
        float width = MenuMinWidth;
        if (GetThemeDefaultFont() is { } font)
        {
            foreach (var (text, _, _) in items)
                width = Math.Max(width, font.GetStringSize(text, HorizontalAlignment.Left, -1, (int)Math.Round(MenuText * _scale)).X / _scale + MarkWidth + MenuPad * 2);
        }
        var size = new Vector2(width, items.Count * RowHeight + MenuPad * 2);
        var box = new Rect2(new Vector2(Math.Clamp(at.X, 0, 1280 - size.X), Math.Clamp(at.Y, 0, _height - size.Y)), size);
        _menu = (place, box, items);
        _hotItem = ItemAt(_menu.Value, at);
        _hotToggle = _pressedToggle = -1;                           // a long press on an order toggle opens the menu instead
        (_hovered, _banners, _hotBanner) = (null, [], -1);
        QueueRedraw();
    }

    private void CloseMenu()
    {
        _menu = null;
        _hotItem = _pressedItem = -1;
        if (Touch && _commands is not null)
            SetTarget(_target);                                     // the touch mode's target again
        QueueRedraw();
    }

    private void SetHotItem(int item)
    {
        if (item == _hotItem)
            return;
        _hotItem = item;
        QueueRedraw();
    }

    // ------------------------------------------------------------------ geometry (layout pixels)

    /// <summary>The card under <paramref name="p"/> that holds a unit, or null.</summary>
    private BattlePlace? CardAt(Vector2 p)
    {
        if (_state is not { } s)
            return null;
        foreach (var place in SlotSnapshot.Places)
        {
            if (_cardArea(place).HasPoint(p) && BattleBoard.Record(s, place) != 0)
                return place;
        }
        return null;
    }

    /// <summary>
    /// Banner <paramref name="i"/> on <paramref name="target"/>'s card: its fixed place, the first at the top; taller while
    /// the forecast shows.
    /// </summary>
    private Rect2 BannerRect(BattlePlace target, int i)
    {
        var card = _cardArea(target);
        var size = _forecastShown ? ForecastBannerSize : BannerSize;
        float column = BannerPlaces * size.Y + (BannerPlaces - 1) * BannerGap;
        return new Rect2(card.Position.X - BannerOut, card.Position.Y + (card.Size.Y - column) / 2 + i * (size.Y + BannerGap), size);
    }

    /// <summary>The banner of <paramref name="target"/> under <paramref name="p"/> (its pennant shape), or -1.</summary>
    private int BannerAt(BattlePlace target, Vector2 p)
    {
        if (target != _hovered || Touch)
            return -1;                                              // the touch mode shows no banners: its rail does

        for (int i = 0; i < _banners.Count; i++)
        {
            if (Geometry2D.IsPointInPolygon(p, Pennant(BannerRect(target, i), 1)))
                return i;
        }
        return -1;
    }

    private int ItemAt((BattlePlace Place, Rect2 Box, IReadOnlyList<(string, FigtSelChoice, bool)> Items) menu, Vector2 p)
    {
        var box = menu.Box;
        if (!box.HasPoint(p))
            return -1;
        int row = (int)Math.Floor((p.Y - box.Position.Y - MenuPad) / RowHeight);
        return row >= 0 && row < menu.Items.Count ? row : -1;
    }

    /// <summary>A pennant in <paramref name="r"/>: the left end cut aslant, the right end a point (towards the enemy's face), scaled by <paramref name="scale"/>.</summary>
    private static Vector2[] Pennant(Rect2 r, float scale)
    {
        var (x, y, w, h) = (r.Position.X, r.Position.Y, r.Size.X, r.Size.Y);
        return [.. new Vector2[]
        {
            new(x + BannerSlant, y), new(x + w - BannerPoint, y), new(x + w, y + h / 2), new(x + w - BannerPoint, y + h), new(x, y + h),
        }.Select(v => v * scale)];
    }

    // ------------------------------------------------------------------ drawing

    public override void _Draw()
    {
        if (_commands is null)
            return;
        var font = GetThemeDefaultFont();
        if (Touch)
        {
            DrawRails(font);
            if (_target is { } t && _menu is null)
                DrawTouchTarget(_cardArea(t));
            if (_hotToggle >= 0 && _menu is null)
                DrawHeldToggle(_toggles[_hotToggle].Rect);
        }
        else if (_menu is null)
        {
            if (_hovered is { } target)
            {
                if (target.Side == BattleSide.Enemy && _banners.Count > 0)
                    DrawTargetFrame(_cardArea(target));
                for (int i = 0; i < _banners.Count; i++)
                    DrawBanner(BannerRect(target, i), _banners[i].Text, i == _hotBanner, font, DamageOf(_banners[i].Choice));
            }
            if (_hotToggle >= 0)
                DrawHeldToggle(_toggles[_hotToggle].Rect);
        }
        if (_menu is { } menu)
            DrawMenu(menu, font);
    }

    /// <summary>
    /// The frame on the target's card <paramref name="card"/> (layout pixels): a bracket on each corner — its glow, the
    /// cyan line, the white line inside it; <paramref name="alpha"/> fades it (the touch mode's target no attack reaches),
    /// <paramref name="line"/> and <paramref name="halo"/> recolour the line and its glow (the touch mode's gold).
    /// </summary>
    private void DrawTargetFrame(Rect2 card, float alpha = 1, float outset = TargetOut, float glow = 1, Color? line = null, Color? halo = null)
    {
        float s = _scale;
        var colour = line ?? TargetColour;
        var shine = halo ?? TargetColour;
        var outer = new Rect2(card.Position * s, card.Size * s).Grow(outset * s);
        float width = Math.Max(2, TargetWidth * s);
        foreach (var (hx, vy) in new[] { (1, 1), (-1, 1), (1, -1), (-1, -1) })
        {
            var corner = new Vector2(hx > 0 ? outer.Position.X : outer.End.X, vy > 0 ? outer.Position.Y : outer.End.Y);
            var middle = Bracket(corner, hx, vy, width / 2);
            DrawPolyline(middle, shine with { A = 0.15f * alpha * glow }, width + 2 * TargetGlow * s, antialiased: true);
            DrawPolyline(middle, shine with { A = 0.3f * alpha * glow }, width + TargetGlow * s, antialiased: true);
            DrawPolyline(middle, colour with { A = alpha }, width, antialiased: true);
            DrawPolyline(Bracket(corner, hx, vy, width), TargetInner with { A = TargetInner.A * alpha }, Math.Max(1, s), antialiased: true);
        }
    }

    /// <summary>
    /// The bracket on the frame's corner <paramref name="corner"/> (window pixels), <paramref name="inset"/> in from its outer
    /// edge: from the end of its arm along the card, round the corner, to the end of its arm down the card; its arms run
    /// towards <paramref name="hx"/> and <paramref name="vy"/> (±1).
    /// </summary>
    private Vector2[] Bracket(Vector2 corner, int hx, int vy, float inset)
    {
        float s = _scale;
        var c = corner + new Vector2(hx, vy) * inset;
        float radius = Math.Max(1, TargetRadius * s - inset);
        var centre = c + new Vector2(hx, vy) * radius;
        var points = new List<Vector2> { c + new Vector2(hx * (TargetArmX * s - inset), 0) };
        for (int i = 0; i <= 6; i++)
        {
            float t = i * MathF.PI / 12;
            points.Add(centre + new Vector2(-hx * radius * MathF.Sin(t), -vy * radius * MathF.Cos(t)));
        }
        points.Add(c + new Vector2(0, vy * (TargetArmY * s - inset)));
        return [.. points];
    }

    /// <summary>
    /// A banner in <paramref name="r"/>: the pennant, its label; while the forecast shows the label higher and
    /// <paramref name="damage"/> under it (nothing there while its forecast runs).
    /// </summary>
    private void DrawBanner(Rect2 r, string text, bool hot, Font? font, BannerDamage? damage)
    {
        float s = _scale;
        var shape = Pennant(r, s);
        var (top, bottom) = hot ? (HotTop, HotBottom) : (BannerTop, BannerBottom);
        var outline = shape.Append(shape[0]).ToArray();
        // a soft shadow below and right; the hovered one glows
        DrawColoredPolygon([.. shape.Select(v => v + new Vector2(4, 5) * s)], new Color(0, 0, 0, 0.18f));
        DrawColoredPolygon([.. shape.Select(v => v + new Vector2(2, 3) * s)], new Color(0, 0, 0, 0.25f));
        if (hot)
        {
            DrawPolyline(outline, BannerGlow with { A = 0.18f }, 14 * s, antialiased: true);
            DrawPolyline(outline, BannerGlow with { A = 0.35f }, 7 * s, antialiased: true);
        }
        // a vertical gradient: each corner coloured by its height
        float y0 = r.Position.Y * s, h = r.Size.Y * s;
        var colours = shape.Select(v => top.Lerp(bottom, (v.Y - y0) / h) with { A = BannerAlpha }).ToArray();
        DrawPolygon(shape, colours);
        DrawPolyline(outline, Colors.White, Math.Max(1, 2 * s), antialiased: true);
        if (font is null)
            return;
        int size = (int)Math.Round((_forecastShown ? ForecastLabel : BannerText) * s);
        float left = (r.Position.X + BannerSlant) * s, width = (r.Size.X - BannerSlant - BannerPoint) * s;
        float middle = _forecastShown ? y0 + ForecastLabelMiddle * s : y0 + h / 2;
        float baseline = middle + (font.GetAscent(size) - font.GetDescent(size)) / 2;
        var at = new Vector2(left, baseline);
        DrawStringOutline(font, at, text, HorizontalAlignment.Center, width, size, Math.Max(2, (int)Math.Round(5 * s)), TextOutline);
        DrawString(font, at, text, HorizontalAlignment.Center, width, size, Colors.White);
        if (damage is null)
            return;
        float y = r.Position.Y + ForecastMiddle, x = r.Position.X;
        DrawDamage(font, BannerDamage.TroopsWord, damage.Troops, x + ForecastTroopsAt, ForecastHpAt - ForecastTroopsAt - ForecastGap, y,
            TroopsInk);
        DrawDamage(font, BannerDamage.HpWord, damage.Hp, x + ForecastHpAt, r.Size.X - BannerPoint - ForecastHpAt, y, HpInk);
    }

    /// <summary>
    /// One loss of the forecast (layout pixels): <paramref name="word"/> and <paramref name="figure"/> from
    /// <paramref name="x"/>, their middle at <paramref name="middle"/>, in <paramref name="room"/>; the figure at
    /// <see cref="ForecastFigure"/>, the word at <see cref="ForecastWord"/>, both shrunk to fit; nothing for no figure
    /// (out of reach).
    /// </summary>
    private void DrawDamage(Font font, string word, string? figure, float x, float room, float middle, Color ink)
    {
        if (figure is null)
            return;
        float s = _scale;
        int px = (int)Math.Round(ForecastFigure * s), tag = (int)Math.Round(ForecastWord * s);
        float Width(int f, int t) => font.GetStringSize(word + " ", fontSize: t).X + font.GetStringSize(figure, fontSize: f).X;
        while (px > Math.Round(8 * s) && Width(px, tag) > room * s)
            (px, tag) = (px - 1, tag - 1);
        int outline = Math.Max(2, (int)Math.Round(ForecastOutline * s));
        float left = x * s;
        foreach (var (text, fs) in new[] { (word, tag), (figure, px) })
        {
            var at = new Vector2(left, middle * s + (font.GetAscent(fs) - font.GetDescent(fs)) / 2);
            DrawStringOutline(font, at, text, HorizontalAlignment.Left, -1, fs, outline, TextOutline);
            DrawString(font, at, text, HorizontalAlignment.Left, -1, fs, ink);
            left += font.GetStringSize(text + " ", fontSize: fs).X;
        }
    }

    private void DrawMenu((BattlePlace Place, Rect2 Box, IReadOnlyList<(string Text, FigtSelChoice Choice, bool Current)> Items) menu, Font? font)
    {
        float s = _scale;
        var box = new Rect2(menu.Box.Position * s, menu.Box.Size * s);
        DrawRect(new Rect2(box.Position + new Vector2(4, 5) * s, box.Size), new Color(0, 0, 0, 0.3f));
        DrawRect(box, Ui.Row);
        DrawRect(box, Ui.Gold, filled: false, width: Math.Max(1, 2 * s));
        int size = (int)Math.Round(MenuText * s);
        for (int i = 0; i < menu.Items.Count; i++)
        {
            var row = new Rect2(box.Position + new Vector2(MenuPad - 4, MenuPad + i * RowHeight) * s, new Vector2(menu.Box.Size.X - (MenuPad - 4) * 2, RowHeight) * s);
            if (i == _hotItem)
                DrawRect(row, Ui.RowHover);
            var (text, _, current) = menu.Items[i];
            if (current)
                DrawCircle(row.Position + new Vector2((4 + MarkWidth / 2) * s, row.Size.Y / 2), MarkWidth / 4.5f * s, Ui.HoverLine);
            if (font is null)
                continue;
            float baseline = row.Position.Y + (row.Size.Y + font.GetAscent(size) - font.GetDescent(size)) / 2;
            DrawString(font, new Vector2(row.Position.X + (4 + MarkWidth) * s, baseline), text, HorizontalAlignment.Left, -1, size,
                current ? Ui.HoverLine : Ui.Text);
        }
    }
}
