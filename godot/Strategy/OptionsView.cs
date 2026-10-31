using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The options window (docs/ui/options.md; the owner's mock-up D, 2026-10-21): every setting of the menu bar's Option, over
/// the game, which waits while it is open. A paper sheet in a lacquer frame with paper tabs across its top — Messages
/// (the text speed with the game's 121 typing as its preview, the message skip's switches, Forget, the mouse
/// (<see cref="MouseMovementChoice"/>, not in the touch mode), the
/// already-read text's colour, <see cref="ReadTextChoice"/>, docs/ui/read-text.md), Display
/// (the font, the palette, the thousands, the unit tables' row height, <see cref="LedgerRowsChoice"/>; on a phone its
/// orientation, <see cref="OrientationChoice"/>; in the touch layout the screen margins, <see cref="ScreenMarginsChoice"/>,
/// adjusted over the screen, <see cref="MarginsAdjust"/>), Battle (our
/// battle screen's effects and timings, <see cref="BattleScreenSettings"/>), Game (the
/// game folder), Tweaks (what changes the original's play: the damage forecast, "Chaos and Nikkou reach demons",
/// <see cref="DemonReachChoice"/>, "Ninja follow-up", <see cref="NinjaChanceChoice"/>, "New scenes first",
/// <see cref="NewScenesChoice"/>; owner, 2026-10-27). Every change applies
/// and is kept at once; Defaults resets the tab shown (Game has none: the game folder has no default); Close, Esc or a right click shut it. Forget and the game folder ask first, in a box over the sheet. Laid out
/// in the 1280×800 screen's pixels, scaled with the picture; on the phone held landscape across the whole screen, each tab
/// in two columns (LO1, <c>OptionsView.Phone.cs</c>); held upright in one column scrolled by a finger (PO1 with PO1o,
/// <c>OptionsView.Upright.cs</c>).
/// </summary>
internal sealed partial class OptionsView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Settings);

    public enum Tab { Messages, Display, Battle, Game, Tweaks }

    private static readonly Tab[] Tabs = [Tab.Messages, Tab.Display, Tab.Battle, Tab.Game, Tab.Tweaks];
    private static readonly Rect2 Frame = new(100, 56, 1080, 704);
    private static readonly Rect2 Sheet = new(126, 126, 1028, 608);
    private static readonly Rect2 CloseButton = new(1004, 686, 130, 40);
    private const float TabTop = 82, TabFoot = 127;
    private const float ContentX = 166, ContentY = 156, ContentW = 948;
    private static Color Field => Ui.Field;

    private static readonly string[] PreviewLines =
        ["I believe that we should first", "prepare our cities for their", "attacks, then defend them with", "urban warfare."];
    private const float PreviewHold = 1.2f;         // seconds the whole preview stays before it types again

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private readonly FontVariation _cinzelBold = Lacquer.Cinzel(800);
    private FontFile _gothic;
    private readonly FontVariation _sealFont = Lacquer.Cinzel(700);    // the row tiles' party seal, as the tables'
    private readonly FontFile _bizSample = DeploymentTableView.LoadFont(GameFont.BizUdGothic);
    private readonly FontFile? _msSample = GodotGlyphRenderer.MsGothicInstalled ? DeploymentTableView.LoadFont(GameFont.MsGothic) : null;
    private readonly MessageSkip _skip;
    private readonly bool _folderLocked;
    private readonly MessageWindowView _preview;
    private readonly MessageWindow _sample = MessageWindow.Sample(PreviewLines, read: true);

    private Tab _tab;
    private float _k = 1;                           // window pixels to a screen pixel
    private readonly List<Spot> _spots = [];        // what the last drawing made clickable
    private string? _hover, _pressed;
    private Slider? _dragging;
    private double _held;
    private Question? _question;

    private sealed record Spot(string Id, Rect2 Rect, Action Click, Slider? Slider = null);

    private sealed record Slider(string Id, float X0, float X1, double Min, double Max, double Step, Action<double> Set);

    private const float SpeedColumn = 470;

    /// <summary>Where the preview's 121 sits on the Messages tab: under the speed's slider, as wide as its column (the phone's, <see cref="PhonePreviewRect"/>).</summary>
    private Rect2 PreviewRect => _phone is { } f ? PhonePreviewRect(f)
        : new(ContentX, ContentY + 96, SpeedColumn, SpeedColumn * _sample.Panel.H / _sample.Panel.W);

    private sealed record Question(string Title, string Text, string Confirm, Action Confirmed);

    /// <summary>The text speed's step changed (it is kept already).</summary>
    public event Action<int>? TextSpeedChanged;

    /// <summary>A font was chosen (it is kept already).</summary>
    public event Action<GameFont>? FontChosen;

    /// <summary>The already-read text's colour was chosen (it is kept already, <see cref="ReadTextChoice"/>).</summary>
    public event Action? ReadColourChosen;

    /// <summary>The palette was chosen (it is kept already, <see cref="UiPalettes"/>).</summary>
    public event Action? PaletteChosen;

    /// <summary>The thousands' separator was chosen (it is kept already, <see cref="ThousandsChoice"/>).</summary>
    public event Action? ThousandsChosen;

    /// <summary>"New scenes first" was switched (it is kept already).</summary>
    public event Action<bool>? NewScenesFirstChanged;

    /// <summary>"Chaos and Nikkou reach demons" was chosen (it is kept already, <see cref="DemonReachChoice"/>).</summary>
    public event Action<DemonReach>? DemonReachChosen;

    /// <summary>The ninja follow-up's chance was chosen (it is kept already, <see cref="NinjaChanceChoice"/>).</summary>
    public event Action<NinjaChance>? NinjaChanceChosen;

    /// <summary>The player asked for another game folder and confirmed it: the import screen follows.</summary>
    public event Action? GameFolderChosen;

    /// <summary>The player asked to adjust the screen margins: the calibration opens over the screen, this window hidden meanwhile.</summary>
    public event Action? MarginsAdjust;

    /// <summary>The window closed.</summary>
    public event Action? Closed;

    /// <summary>The window over the game: <paramref name="skip"/> is the launch's message skip; with <paramref name="folderLocked"/> the game folder cannot change.</summary>
    public OptionsView(MessageSkip skip, bool folderLocked, Tab tab = Tab.Messages)
    {
        _skip = skip;
        _folderLocked = folderLocked;
        _gothic = DeploymentTableView.LoadFont(GodotGlyphRenderer.Chosen);
        MouseFilter = MouseFilterEnum.Stop;
        ZIndex = 1000;                                      // over everything, the battle screen's layers (up to 20) included; Godot's limit is 4096
        _backdrop = new Layer(c =>
        {
            float k = _phone?.Scale ?? 1;
            c.DrawSetTransform(Vector2.Zero, 0, new Vector2(k, k));
            Ui.DrawWaveBackdrop(c, k, c.Size / k);
        }) { ShowBehindParent = true, Visible = false };
        AddChild(_backdrop);
        _preview = new MessageWindowView(GodotGlyphRenderer.Chosen) { Tail = false, Leaves = false, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_preview);
        _preview.Show(_sample, true, null, null);
        _preview.LettersPerSecond = TextSpeedChoice.LettersPerSecond(TextSpeedChoice.Step);
        ShowTab(tab);
    }

    public Tab Current => _tab;

    /// <summary>
    /// Lays the window over the screen's picture (<paramref name="area"/>, in its parent's coordinates); on the phone over
    /// the whole screen, the parts at PO1's type (held landscape LO1, <see cref="PhoneOptions"/>; upright PO1,
    /// <see cref="PhoneOptionsUpright"/>).
    /// </summary>
    public void Place(Rect2 area)
    {
        var phone = Device.Held(this);
        if (phone is not null)
            area = new Rect2(Vector2.Zero, GetParentAreaSize());
        float k = phone is not null ? phone.Px(PhoneOptions.PartScale) : area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f && phone == _phone)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        if (phone != _phone)
            (_dragging, _pressed) = (null, null);                   // a turn: a slider's drag and a press were the old layout's
        _phone = phone;
        _backdrop.Visible = phone is { IsUpright: false };        // upright the view draws it, out of its column's clip
        _backdrop.Size = area.Size;
        _backdrop.QueueRedraw();
        PlaceChildren();
        QueueRedraw();
    }

    public void ShowTab(Tab tab)
    {
        _tab = tab;
        _question = null;
        _dragging = null;
        _scroll = 0;                                        // held upright each tab's column from its top
        _finger.Cancel();
        _preview.Visible = tab == Tab.Messages;
        if (tab == Tab.Messages)
            _preview.Restart();
        PlaceChildren();
        QueueRedraw();
    }

    /// <summary>The preview's 121 scaled into its place on the Messages tab.</summary>
    private void PlaceChildren()
    {
        var panel = _sample.Panel;
        var preview = PreviewRect;
        float kp = preview.Size.X / panel.W;
        var origin = (preview.Position - new Vector2(panel.X, panel.Y) * kp) * _k;
        _preview.Place(new Rect2(origin, new Vector2(1280, 800) * kp * _k));
    }

    public override void _Process(double delta)
    {
        if (_finger.Flinging)
            Fling(delta);
        if (_tab != Tab.Messages || _preview.Typing || double.IsPositiveInfinity(_preview.LettersPerSecond))
            return;
        _held += delta;
        if (_held < PreviewHold)
            return;
        _held = 0;
        _preview.Restart();
    }

    /// <summary>The window's font follows Display's choice at once.</summary>
    private void SetFont(GameFont font)
    {
        GodotGlyphRenderer.Chosen = font;
        _gothic = DeploymentTableView.LoadFont(font);
        _preview.SetFont(font);
        FontChosen?.Invoke(font);
    }

    private void SetPalette(UiPalette palette)
    {
        UiPalettes.Chosen = palette;
        UiPalettes.Repaint(this);
        PaletteChosen?.Invoke();
    }

    private void SetThousands(ThousandsSeparator separator)
    {
        ThousandsChoice.Chosen = separator;
        ThousandsChosen?.Invoke();
    }

    /// <summary>The ledgers' row height: kept at once, the tables open take it at their next frame.</summary>
    private static void SetLedgerRows(int height) => LedgerRowsChoice.Chosen = height;

    private void SetTextSpeed(int step)
    {
        TextSpeedChoice.Step = step;
        _preview.LettersPerSecond = TextSpeedChoice.LettersPerSecond(TextSpeedChoice.Step);
        _preview.Restart();
        _held = 0;
        TextSpeedChanged?.Invoke(TextSpeedChoice.Step);
    }

    private void SetReadColour(int index)
    {
        ReadTextChoice.Index = index;
        _preview.QueueRedraw();
        ReadColourChosen?.Invoke();
    }

    /// <summary>The message skip's switches: on the launch's skip at once and kept (<see cref="MessageSkipChoice"/>).</summary>
    private void SetSkipUnseen(bool on)
    {
        MessageSkipChoice.SkipUnseen = on;
        _skip.SkipUnseen = on;
    }

    private void SetFastFades(bool on)
    {
        MessageSkipChoice.FastFades = on;
        _skip.FastFades = on;
    }

    private void SetNewScenesFirst(bool on)
    {
        NewScenesChoice.On = on;
        NewScenesFirstChanged?.Invoke(on);
    }

    private void SetDemonReach(DemonReach reach)
    {
        DemonReachChoice.Chosen = reach;
        DemonReachChosen?.Invoke(reach);
    }

    private void SetNinjaChance(NinjaChance chance)
    {
        NinjaChanceChoice.Chosen = chance;
        NinjaChanceChosen?.Invoke(chance);
    }

    /// <summary>Defaults: the tab shown back to its settings' defaults.</summary>
    private void Defaults()
    {
        switch (_tab)
        {
            case Tab.Messages:
                SetTextSpeed(TextSpeedChoice.DefaultStep);
                SetReadColour(ReadTextChoice.DefaultIndex);
                SetSkipUnseen(true);
                SetFastFades(true);
                if (!TouchMode.On)                          // not offered there: the PC's choice stays
                    MouseMovementChoice.On = true;
                break;
            case Tab.Display:
                SetFont(GameFont.BizUdGothic);
                SetPalette(UiPalette.Lacquer);
                SetThousands(ThousandsSeparator.Comma);
                SetLedgerRows(LedgerRowsChoice.Default);     // the PC's or the touch mode's own
                if (Device.Phone)
                    OrientationChoice.Chosen = OrientationChoice.Way.System;
                if (Device.TouchLayout)
                {
                    ScreenMarginsChoice.Set(upright: true, ScreenMargins.None);
                    ScreenMarginsChoice.Set(upright: false, ScreenMargins.None);
                }
                break;
            case Tab.Battle:
                foreach (var setting in BattleScreenSettings.All.Where(s => s != BattleScreenSettings.Forecast))
                    BattleScreenSettings.Set(setting, setting.Default);
                break;
            case Tab.Tweaks:
                BattleScreenSettings.Set(BattleScreenSettings.Forecast, BattleScreenSettings.Forecast.Default);
                SetDemonReach(DemonReach.Original);
                SetNinjaChance(NinjaChance.Original);
                SetNewScenesFirst(true);
                break;
        }
        QueueRedraw();
    }

    public void Close()
    {
        if (IsQueuedForDeletion())
            return;
        Closed?.Invoke();
        QueueFree();
    }

    // ---------------------------------------------------------------- input

    private Spot? SpotAt(Vector2 at)
    {
        for (int i = _spots.Count - 1; i >= 0; i--)
            if (_spots[i].Rect.HasPoint(at))
                return _spots[i];
        return null;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_phone is { IsUpright: true } upright && _question is null && FingerTouch(e, upright))
        {
            AcceptEvent();
            return;
        }
        switch (e)
        {
            case InputEventMouseMotion motion:
            {
                var at = motion.Position / _k;
                if (_dragging is { } slider)
                    Drag(slider, at.X);
                string? hover = SpotAt(at)?.Id;
                if (hover != _hover)
                {
                    _hover = hover;
                    QueueRedraw();
                }
                MouseDefaultCursorShape = hover is not null ? CursorShape.PointingHand : CursorShape.Arrow;
                break;
            }
            case InputEventMouseButton { ButtonIndex: Godot.MouseButton.Right, Pressed: false }:
                Back();
                break;
            case InputEventMouseButton { ButtonIndex: Godot.MouseButton.Left } click:
            {
                var at = click.Position / _k;
                var spot = SpotAt(at);
                if (click.Pressed)
                {
                    _pressed = spot?.Id;
                    if (spot?.Slider is { } slider)
                    {
                        _dragging = slider;
                        Drag(slider, at.X);
                    }
                }
                else
                {
                    bool dragged = _dragging is not null;
                    _dragging = null;
                    if (!dragged && spot is not null && spot.Id == _pressed)
                        spot.Click();
                    _pressed = null;
                }
                QueueRedraw();
                break;
            }
        }
        AcceptEvent();
    }

    public override void _Input(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Keycode: Key.Escape } || !Visible)     // hidden under the margins' calibration
            return;
        Back();
        GetViewport().SetInputAsHandled();
    }

    /// <summary>Esc or a right click: the question away, else the window.</summary>
    private void Back()
    {
        if (_question is not null)
        {
            _question = null;
            QueueRedraw();
        }
        else
        {
            Close();
        }
    }

    private void Drag(Slider slider, float x)
    {
        double t = Math.Clamp((x - slider.X0) / (slider.X1 - slider.X0), 0, 1);
        double value = slider.Min + t * (slider.Max - slider.Min);
        value = slider.Min + Math.Round((value - slider.Min) / slider.Step) * slider.Step;
        slider.Set(Math.Clamp(value, slider.Min, slider.Max));
        QueueRedraw();
    }

    private void Ask(string title, string text, string confirm, Action confirmed)
    {
        _question = new Question(title, text, confirm, confirmed);
        _dragging = null;
        QueueRedraw();
    }

    // ---------------------------------------------------------------- drawing (mock-up D)

    private float Line(float width) => Lacquer.Line(width, _k);

    public override void _Draw()
    {
        _spots.Clear();
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (_phone is { } phone)
        {
            DrawPhone(phone);
            return;
        }
        DrawRect(new Rect2(0, 0, 1280, 800), new Color(0, 0, 0, 0.55f));
        Ui.DrawPanel(this, Frame, _k);
        DrawTabs();
        Text(_brush, new Vector2(1150 - _brush.GetStringSize("Options", fontSize: 28).X, 116), "Options", fontSize: 28,
            modulate: Ui.Name);
        switch (_tab)
        {
            case Tab.Messages:
                DrawMessages();
                break;
            case Tab.Display:
                DrawDisplay();
                break;
            case Tab.Battle:
                DrawBattle();
                break;
            case Tab.Game:
                DrawGame();
                break;
            case Tab.Tweaks:
                DrawTweaks();
                break;
        }
        DrawLink("defaults", "Defaults", new Vector2(150, 706), Defaults, enabled: _tab != Tab.Game);   // the game folder has no default
        bool closeLit = _hover == "close";
        Ui.DrawRedButton(this, CloseButton, closeLit, closeLit && _pressed == "close", _k);
        Text(_cinzelBold, new Vector2(CloseButton.Position.X, Lacquer.Baseline(CloseButton, _cinzelBold, 17)), "Close",
            HorizontalAlignment.Center, CloseButton.Size.X, 17, Ui.SealText);
        _spots.Add(new Spot("close", CloseButton, Close));
        if (_question is not null)
            DrawQuestion(_question);
    }

    /// <summary>Paper tabs: the current one joined to the sheet with a vermilion edge, the others darker paper.</summary>
    private void DrawTabs()
    {
        float x = Sheet.Position.X;
        Rect2 current = default;
        foreach (var tab in Tabs)
        {
            string name = tab.ToString();
            float w = _cinzelBold.GetStringSize(name, fontSize: 18).X + 52;
            bool on = tab == _tab;
            var rect = new Rect2(x, on ? TabTop : TabTop + 6, w, TabFoot - (on ? TabTop : TabTop + 6));
            DrawRect(rect, on ? Ui.Paper : _hover == "tab-" + name ? Field : Ui.PaperHead);
            DrawRect(rect, on ? Ui.PaperHead : Ui.InkGrey, false, Line(1));
            if (on)
                current = rect;
            Text(on ? _cinzelBold : _cinzel, new Vector2(x, Lacquer.Baseline(rect, _cinzelBold, 18)), name,
                HorizontalAlignment.Center, w, 18, on ? Ui.HeadInk : Ui.InkQuiet);
            _spots.Add(new Spot("tab-" + name, rect, () => ShowTab(tab)));
            x += w + 6;
        }
        DrawRect(Sheet, Ui.Paper);
        DrawRect(Sheet, Ui.PaperHead, false, Line(1));
        DrawRect(new Rect2(current.Position.X + 1, TabFoot - 4, current.Size.X - 2, 8), Ui.Paper);
        DrawRect(new Rect2(current.Position.X, TabTop, current.Size.X, 4), Ui.Vermilion);
    }

    private void DrawMessages()
    {
        float x = ContentX, y = ContentY, lw = SpeedColumn;
        SectionHead(x, y, "Text speed", lw);
        SpeedSlider(x, y + 66, lw);
        MessageSkipColumn(x + lw + 50, y, ContentW - lw - 50);

        int chosen = ReadTextChoice.Index;
        Text(_gothic, new Vector2(x + 14, y + 281), chosen == ReadTextChoice.None ? "Read text keeps the window's colour."
            : "The preview counts as read.", fontSize: 13, modulate: Ui.InkQuiet);
        y += 330;
        SectionHead(x, y, "Already-read text", ContentW);
        float tx = x;
        ReadTile("read-none", new Rect2(tx, y + 40, 150, 44), chosen == ReadTextChoice.None, null, ReadTextChoice.None);
        tx += 160;
        for (int i = 0; i < ReadTextChoice.Colours.Length; i++, tx += 54)
            ReadTile("read-" + i, new Rect2(tx, y + 40, 44, 44), chosen == i, ReadTextChoice.Colours[i].Colour, i);
        Text(_gothic, new Vector2(x, y + 128), chosen == ReadTextChoice.None ? "Don't change" : ReadTextChoice.Colours[chosen].Name,
            fontSize: 17, modulate: Ui.Ink);
        Text(_gothic, new Vector2(x, y + 150), ReadTextLines[0], fontSize: 13, modulate: Ui.InkQuiet);
        Text(_gothic, new Vector2(x, y + 170), ReadTextLines[1], fontSize: 13, modulate: Ui.InkQuiet);
    }

    /// <summary>What the already-read text's colour does, in two lines.</summary>
    private static readonly string[] ReadTextLines =
    [
        "A message window you have read before, in any game, shows its text in this colour.",
        "The battle's balloons too, darker on white; menus and our screens keep theirs. Forget them… counts them unread.",
    ];

    /// <summary>The text speed's slider at <paramref name="y"/>, "Slow" and "Instant" at its ends, across <paramref name="width"/>.</summary>
    private void SpeedSlider(float x, float y, float width)
    {
        int step = TextSpeedChoice.Step;
        DrawSlider(x + 60, x + width - 70, y, (step - 1) / (float)(TextSpeedChoice.Steps - 1), step.ToString(),
            new Slider("speed", x + 60, x + width - 70, 1, TextSpeedChoice.Steps, 1,
                v => { if ((int)v != TextSpeedChoice.Step) SetTextSpeed((int)v); }));
        Text(_gothic, new Vector2(x, y + 5), "Slow", fontSize: 14, modulate: Ui.InkQuiet);
        Text(_gothic, new Vector2(x, y + 5), "Instant", HorizontalAlignment.Right, width, 14, Ui.InkQuiet);
    }

    /// <summary>Messages' second column: the message skip's switches, the seen messages and Forget, the mouse (not in the touch mode).</summary>
    private void MessageSkipColumn(float rx, float y, float rw)
    {
        SectionHead(rx, y, "Message skip", rw);
        CheckBox("skip-unseen", rx, y + 38, _skip.SkipUnseen, "Skip unseen text", "MsgSkip and Ctrl skip unread ones too.",
            () => SetSkipUnseen(!_skip.SkipUnseen));
        CheckBox("fast-fades", rx, y + 92, _skip.FastFades, "MsgSkip speeds up fades", "Fades and waits run fast.",
            () => SetFastFades(!_skip.FastFades));
        Text(_gothic, new Vector2(rx, y + 165), $"{_skip.SeenCount} messages seen.", fontSize: 14, modulate: Ui.InkQuiet);
        DrawLink("forget", "Forget them…", new Vector2(rx + rw, y + 160),
            () => Ask("Forget seen messages?", $"All {_skip.SeenCount} messages seen so far will count as unseen again.", "Forget",
                () => _skip.ForgetSeen()), right: true, enabled: _skip.SeenCount > 0);
        if (!TouchMode.On)                                  // off there, MouseMovementChoice
        {
            SectionHead(rx, y + 220, "Mouse", rw);
            CheckBox("mouse", rx, y + 258, MouseMovementChoice.On, "Mouse movement", "After a menu, the pointer glides back where it was.",
                () => MouseMovementChoice.On = !MouseMovementChoice.On);
        }
    }

    /// <summary>
    /// A tile of "Already-read text" (after Rance X's row): lacquer, so the colour shows as on the windows, with its colour as
    /// a square, or "Don't change" when <paramref name="colour"/> is null; framed in vermilion when chosen.
    /// </summary>
    private void ReadTile(string id, Rect2 rect, bool on, Color? colour, int index)
    {
        bool lit = _hover == id;
        DrawRect(rect, Ui.Panel with { A = 1 });
        DrawRect(rect, Ui.DimGold, false, Line(1));
        if (colour is { } c)
            DrawRect(rect.Grow(-8), c);
        else
            Text(_gothic, new Vector2(rect.Position.X, Lacquer.Baseline(rect, _gothic, 16)), "Don't change",
                HorizontalAlignment.Center, rect.Size.X, 16, Ui.Text);
        if (on)
            DrawRect(rect.Grow(3), Ui.Vermilion, false, Line(3));
        else if (lit)
            DrawRect(rect.Grow(2), Ui.HeadInk, false, Line(2));
        _spots.Add(new Spot(id, rect.Grow(4), () => { SetReadColour(index); QueueRedraw(); }));
    }

    private void DrawDisplay()
    {
        float x = ContentX, y = ContentY, w = ContentW;
        FontSection(x, y, w);
        y += 132;
        if (Device.TouchLayout)                     // the tablet: the screen margins beside the palette
        {
            PaletteSection(x, y, MarginsIn - 40);
            SectionHead(x + MarginsIn, y, "Screen margins", w - MarginsIn);
            MarginsField(new Rect2(x + MarginsIn, y + 36, w - MarginsIn, 40));
        }
        else
        {
            PaletteSection(x, y, w);
        }
        y += 132;
        ThousandsSection(x, y, w);
        y += 104;
        RowsSection(x, y, w);
    }

    private void FontSection(float x, float y, float w)
    {
        SectionHead(x, y, "Font", w);
        Radio("font-ms", x, y + 38, GodotGlyphRenderer.Chosen == GameFont.MsGothic,
            _msSample is null ? "MS Gothic — not installed" : "MS Gothic — 鬼畜王ランス, Rance", _msSample ?? _gothic,
            () => SetFont(GameFont.MsGothic), enabled: _msSample is not null);
        Radio("font-biz", x, y + 74, GodotGlyphRenderer.Chosen == GameFont.BizUdGothic, "BIZ UDGothic — 鬼畜王ランス, Rance",
            _bizSample, () => SetFont(GameFont.BizUdGothic));
    }

    /// <summary>
    /// The phone's screen orientation (the owner's PO1o, <see cref="OrientationChoice"/>), a phone's only: its four ways as
    /// radios two a row from 38 under the head, the chosen way's line under them, shrunk to <paramref name="w"/>.
    /// </summary>
    private void OrientationSection(float x, float y, float w)
    {
        SectionHead(x, y, "Screen orientation", w);
        var all = OrientationChoice.All;
        for (int i = 0; i < all.Length; i++)
        {
            var way = all[i].Way;
            Radio("orientation-" + way, x + i % 2 * (w / 2), y + 38 + i / 2 * 36, OrientationChoice.Chosen == way, all[i].Name,
                _gothic, () => OrientationChoice.Chosen = way);
        }
        string hint = Array.Find(all, o => o.Way == OrientationChoice.Chosen).Hint;
        Text(_gothic, new Vector2(x, y + 106), hint, fontSize: SideCells.Fit(_gothic, hint, 13, w), modulate: Ui.InkQuiet);
    }

    /// <summary>On the PC's sheet in the touch layout (the tablet), how far in the screen margins stand, right of the palette's swatches.</summary>
    private const float MarginsIn = 340;

    /// <summary>
    /// The screen margins (docs/ui/screen-margins.md, the owner's C), in the touch layout only: a field with the sets in
    /// words — on a phone the way it is held now and the other under it, on the tablet landscape's alone — and Adjust…; a
    /// tap anywhere on it opens the calibration over the screen (<see cref="MarginsAdjust"/>). <paramref name="titled"/>:
    /// with no section head over it (the phone held landscape, at the left column's foot), its name on its first line and
    /// both sets on the second.
    /// </summary>
    private void MarginsField(Rect2 field, bool titled = false)
    {
        bool lit = _hover == "margins";
        DrawRect(field, Field);
        DrawRect(field, lit ? Ui.Vermilion : Ui.InkGrey, false, Line(lit ? 2 : 1));
        const string adjust = "Adjust…";
        float adjustW = _gothic.GetStringSize(adjust, fontSize: 15).X;
        float textX = field.Position.X + 12, textW = field.Size.X - adjustW - 40;
        static string Set(bool upright) => (upright ? "Upright: " : "Landscape: ") + ScreenMarginsChoice.For(upright).Words;
        bool upright = Device.HeldUpright;
        if (Device.Phone)
        {
            string first = titled ? "Screen margins" : Set(upright), second = titled ? $"{Set(upright)} · {Set(!upright)}" : Set(!upright);
            Text(_gothic, new Vector2(textX, PhonePickerParts.Middle(_gothic, 17, field.Position.Y + field.Size.Y * 0.36f)),
                Ellipsis(first, textW, 17), fontSize: 17, modulate: Ui.Ink);
            Text(_gothic, new Vector2(textX, PhonePickerParts.Middle(_gothic, 13, field.Position.Y + field.Size.Y * 0.72f)),
                Ellipsis(second, textW, 13), fontSize: 13, modulate: Ui.InkQuiet);
        }
        else
        {
            Text(_gothic, new Vector2(textX, PhonePickerParts.Middle(_gothic, 17, field.GetCenter().Y)),
                Ellipsis(Set(false), textW, 17), fontSize: 17, modulate: Ui.Ink);
        }
        Text(_gothic, new Vector2(field.Position.X, PhonePickerParts.Middle(_gothic, 15, field.GetCenter().Y)), adjust,
            HorizontalAlignment.Right, field.Size.X - 14, 15, lit ? Ui.Vermilion : Ui.VermilionInk);
        _spots.Add(new Spot("margins", field, () => MarginsAdjust?.Invoke()));
    }

    private void PaletteSection(float x, float y, float w)
    {
        SectionHead(x, y, "Palette", w);
        for (int i = 0; i < UiPalette.All.Count; i++)
            Swatch(x + i * 90, y + 40, UiPalette.All[i], UiPalettes.Chosen, SetPalette);
    }

    private void ThousandsSection(float x, float y, float w)
    {
        SectionHead(x, y, "Thousands in gold, troops and damage", w);
        for (int i = 0; i < ThousandsChoice.All.Length; i++)
        {
            var separator = ThousandsChoice.All[i];
            Radio("thousands-" + separator, x + i * (w / ThousandsChoice.All.Length), y + 38, ThousandsChoice.Chosen == separator,
                ThousandsChoice.Sample(separator), _gothic, () => SetThousands(separator));
        }
    }

    /// <summary>The unit tables' row heights: a tile each across <paramref name="w"/>, the tiles from 58 under the head.</summary>
    private void RowsSection(float x, float y, float w)
    {
        SectionHead(x, y, "Unit tables: row height", w);
        Text(_gothic, new Vector2(x, y + 14), "Subordinates and deployment. Each device keeps its own.",
            HorizontalAlignment.Right, w, 13, Ui.InkQuiet);
        int count = LedgerRowsChoice.Heights.Length;
        float tile = (w - (count - 1) * RowTileGap) / count;
        for (int i = 0; i < count; i++)
            RowTile(x + i * (tile + RowTileGap), y + 58, tile, LedgerRowsChoice.Heights[i]);
    }

    private const float RowTileGap = 17;

    /// <summary>
    /// A row height's tile (the owner's O2, docs/ui/unit-ledger.md §8): two ledger rows <paramref name="height"/> tall as the
    /// tables draw them in their palette — Rance's gilt with his party's seal, Leila's plain —, framed in vermilion when
    /// chosen, its height over it with the mode's default marked. The whole column is its target, the tallest tile's height.
    /// </summary>
    private void RowTile(float x, float y, float w, int height)
    {
        string id = $"rows-{height}";
        var table = UiPalettes.For(UiPart.Screens);
        bool on = LedgerRowsChoice.Chosen == height, lit = _hover == id;
        var rect = new Rect2(x, y, w, 2 * height + 2);
        DrawRect(rect, table.Paper);
        (string Number, string Name, string Cost, int Place)[] rows = [("1", "Rance", "300K", 1), ("5", "Leila", "1,200K", 0)];
        for (int i = 0; i < rows.Length; i++)
        {
            var row = new Rect2(x + 1, y + 1 + i * height, w - 2, height);
            float mid = row.GetCenter().Y;
            if (rows[i].Place > 0)
            {
                DrawRect(row, table.PartyRow);
                table.DrawSeal(this, _sealFont, new Rect2(x + 7, mid - 9.5f, 19, 19), rows[i].Place);
            }
            DrawLine(new Vector2(row.Position.X, row.End.Y - 0.5f), new Vector2(row.End.X, row.End.Y - 0.5f), table.PaperRule, Line(1));
            float baseline = mid + (_gothic.GetAscent(17) - _gothic.GetDescent(17)) / 2;
            Text(_gothic, new Vector2(x, baseline), rows[i].Number, HorizontalAlignment.Right, 58, 17, table.Ink);
            Text(_gothic, new Vector2(x + 64, baseline), rows[i].Name, fontSize: 17, modulate: table.Ink);
            Text(_gothic, new Vector2(x, baseline), rows[i].Cost, HorizontalAlignment.Right, w - 9, 17, table.Ink);
        }
        DrawRect(rect, Ui.InkGrey, false, Line(1));
        if (on)
            DrawRect(rect.Grow(3), Ui.Vermilion, false, Line(3));
        else if (lit)
            DrawRect(rect.Grow(2), Ui.HeadInk, false, Line(2));
        string label = height == LedgerRowsChoice.Default ? $"{height} px — default" : $"{height} px";
        Text(_gothic, new Vector2(x, y - 10), label, HorizontalAlignment.Center, w, 14, on ? Ui.Ink : Ui.InkQuiet);
        int tallest = LedgerRowsChoice.Heights.Max();
        _spots.Add(new Spot(id, new Rect2(x - 4, y - 28, w + 8, 2 * tallest + 34), () => { SetLedgerRows(height); QueueRedraw(); }));
    }

    private void DrawBattle()
    {
        float x = ContentX, y = ContentY, w = ContentW;
        EffectsSection(x, y, w);
        y += 96;
        SectionHead(x, y, "Timings", w);
        y += 36;
        float colW = (w - 40) / 2;
        int i = 0;
        foreach (var setting in Timings)
        {
            TimingRow(setting, x + i % 2 * (colW + 40), y + i / 2 * 46, colW);
            i++;
        }
    }

    private static IEnumerable<BattleScreenSettings.Setting> Timings =>
        BattleScreenSettings.All.Where(s => s.Unit != BattleScreenSettings.Toggle);

    /// <summary>The effects' switch; with <paramref name="wrap"/> (upright's column) its hint wraps into the width. Returns its foot.</summary>
    private float EffectsSection(float x, float y, float w, bool wrap = false)
    {
        SectionHead(x, y, "Effects", w);
        var effects = BattleScreenSettings.Effects;
        return CheckBox("effects", x, y + 36, BattleScreenSettings.Get(effects) >= 0.5, "Exchange effects", effects.Hint,
            () => BattleScreenSettings.Set(effects, BattleScreenSettings.Get(effects) >= 0.5 ? 0 : 1), wrap ? w : 0);
    }

    /// <summary>A battle timing: its label over its slider, its value at the column's right, 46 high.</summary>
    private void TimingRow(BattleScreenSettings.Setting setting, float cx, float cy, float colW)
    {
        double value = BattleScreenSettings.Get(setting);
        Text(_gothic, new Vector2(cx, cy + 13), setting.Label, fontSize: 14, modulate: Ui.Ink);
        DrawSlider(cx, cx + colW - 80, cy + 29, (float)((value - setting.Min) / (setting.Max - setting.Min)), null,
            new Slider("battle-" + setting.Key, cx, cx + colW - 80, setting.Min, setting.Max, setting.Step,
                v => BattleScreenSettings.Set(setting, v)));
        Text(_gothic, new Vector2(cx, cy + 34), Format(setting, value), HorizontalAlignment.Right, colW, 14, Ui.Ink);
    }

    private void DrawGame() => GameSection(ContentX, ContentY, ContentW);

    /// <summary>The game folder's field and its note; returns the note's last baseline.</summary>
    private float GameSection(float x, float y, float w)
    {
        SectionHead(x, y, "Game folder", w);
        var field = new Rect2(x, y + 36, w, 40);
        bool lit = !_folderLocked && _hover == "folder";
        DrawRect(field, Field);
        DrawRect(field, lit ? Ui.Vermilion : Ui.InkGrey, false, Line(lit ? 2 : 1));
        var icon = new Vector2(x + 12, y + 48);
        DrawPolyline([icon + new Vector2(0, 2), icon + new Vector2(7, 2), icon + new Vector2(9, 5), icon + new Vector2(20, 5),
            icon + new Vector2(20, 16), icon + new Vector2(0, 16), icon + new Vector2(0, 2)], Ui.InkQuiet, Line(2));
        string folder = GameData.GameDirectory is { Length: > 0 } dir ? dir : "not chosen";
        Text(_gothic, new Vector2(x + 44, y + 62), Ellipsis(folder, w - 190, 17), fontSize: 17, modulate: Ui.Ink);
        Text(_gothic, new Vector2(x, y + 61), "Change…", HorizontalAlignment.Right, w - 14, 15,
            _folderLocked ? Ui.InkGrey : lit ? Ui.Vermilion : Ui.VermilionInk);
        if (!_folderLocked)
            _spots.Add(new Spot("folder", field, () => Ask("Change the game folder?",
                "Any unsaved progress will be lost: the import screen opens, then the title.", "Change", () => GameFolderChosen?.Invoke())));
        string note = _folderLocked
            ? "The developer's extract is the data (local.env or settings.cfg): a game folder would change nothing. --import turns this on."
            : "Another copy of the game: the import screen checks it and reads its scenario, then the title opens.";
        int lines = Wrap(note, x, y + 104, w, 14, Ui.InkQuiet);
        return y + 104 + (lines - 1) * WrapPitch(14);
    }

    /// <summary>
    /// What changes the original's play (owner, 2026-10-27; docs/tweaks.md): the battle's damage forecast, BRS-24's fix
    /// (decision 32: off, the owner's A or B, as radios) and the ninja follow-up's chance (decision 33, the owner's N1
    /// without Never: the thirds from low to high, in the reach radios' columns), the scenes' order.
    /// </summary>
    private void DrawTweaks()
    {
        float x = ContentX, y = ContentY, w = ContentW;
        Wrap(TweaksIntro, x, y + 14, w, 14, Ui.InkQuiet);
        y += 44;
        SectionHead(x, y, "Battle", w);
        ForecastBox(x, y + 38);
        float tx = x + 32, tw = w - 32;
        Text(_gothic, new Vector2(tx, y + 120), "Chaos and Nikkou reach demons", fontSize: 17, modulate: Ui.Ink);
        Text(_gothic, new Vector2(tx, y + 139), ReachHint, fontSize: 13, modulate: Ui.InkQuiet);
        for (int i = 0; i < Reaches.Length; i++)
            ReachRadio(i, tx + i * (tw / Reaches.Length), y + 154);
        Text(_gothic, new Vector2(tx, y + 200), ReachNote, fontSize: 13, modulate: Ui.InkQuiet);
        Text(_gothic, new Vector2(tx, y + 247), "Ninja follow-up", fontSize: 17, modulate: Ui.Ink);
        Text(_gothic, new Vector2(tx, y + 266), NinjaHint, fontSize: 13, modulate: Ui.InkQuiet);
        for (int i = 0; i < Ninjas.Length; i++)
            NinjaRadio(Ninjas[i], tx + i * (tw / Ninjas.Length), y + 281);
        y += 338;
        SectionHead(x, y, "Scenes", w);
        NewScenesBox(x, y + 38);
    }

    private const string TweaksIntro = "Each setting here changes how the original game plays. Everything else keeps the original's rules.";

    private const string ReachHint =
        "Against a demon commander, Chaos's or Nikkou's holder fights in person even when his troops exceed the battle's limit.";

    private const string ReachNote =
        "On the field: the demon's troops may shoot at him, as at any commander in the fight. Out of reach: they never do.";

    private static readonly (DemonReach Reach, string Label)[] Reaches =
        [(DemonReach.Original, "Off — as the original"), (DemonReach.OnField, "On the field"), (DemonReach.OutOfReach, "Out of reach")];

    private const string NinjaHint =
        "After Menad, Hubert, Gandhi or Magic attacks, their ninja may strike the enemy commander for 1 HP. Ninja never kills.";

    /// <summary>
    /// The ninja follow-up's chances, low to high (the owner's N1, Never left out), R14's thirds named as percentages (the
    /// owner, 2026-10-29); the phone's words are short (NP).
    /// </summary>
    private static readonly (NinjaChance Chance, string Label)[] Ninjas =
        [(NinjaChance.Original, "33% — as the original"), (NinjaChance.TwoInThree, "67%"), (NinjaChance.Always, "100%")];

    private static readonly (NinjaChance Chance, string Label)[] PhoneNinjas =
        [(NinjaChance.Original, "33%"), (NinjaChance.TwoInThree, "67%"), (NinjaChance.Always, "100%")];

    private void NinjaRadio((NinjaChance Chance, string Label) radio, float x, float y) =>
        Radio("ninja-" + radio.Chance, x, y, NinjaChanceChoice.Chosen == radio.Chance, radio.Label, _gothic,
            () => SetNinjaChance(radio.Chance));

    private float ForecastBox(float x, float y, float width = 0)
    {
        var forecast = BattleScreenSettings.Forecast;
        return CheckBox("forecast", x, y, BattleScreenSettings.Get(forecast) >= 0.5, "Damage forecast", forecast.Hint,
            () => BattleScreenSettings.Set(forecast, BattleScreenSettings.Get(forecast) >= 0.5 ? 0 : 1), width);
    }

    private void ReachRadio(int index, float x, float y)
    {
        var reach = Reaches[index].Reach;
        Radio("reach-" + reach, x, y, DemonReachChoice.Chosen == reach, Reaches[index].Label, _gothic, () => SetDemonReach(reach));
    }

    private float NewScenesBox(float x, float y, float width = 0) =>
        CheckBox("new-scenes", x, y, NewScenesChoice.On, "New scenes first",
            "Talks and harem visits play unread scenes first, with a few hints and a choice of ours.",
            () => SetNewScenesFirst(!NewScenesChoice.On), width);

    // ---------------------------------------------------------------- the parts

    private void SectionHead(float x, float y, string title, float width)
    {
        Text(_cinzelBold, new Vector2(x, y + 14), title.ToUpperInvariant(), fontSize: 13, modulate: Ui.HeadInk);
        DrawLine(new Vector2(x, y + 22), new Vector2(x + width, y + 22), Ui.PaperRule, Line(1));
    }

    /// <summary>
    /// A check box with its label and the hint under it, the whole row its target; with <paramref name="width"/> (the
    /// phone's column) the hint wraps into it and the row takes the column. Returns the row's foot.
    /// </summary>
    private float CheckBox(string id, float x, float y, bool on, string label, string? hint, Action toggle, float width = 0)
    {
        bool lit = _hover == id;
        var box = new Rect2(x, y, 20, 20);
        DrawRect(box, Field);
        DrawRect(box, lit ? Ui.Vermilion : Ui.InkGrey, false, Line(lit ? 2 : 1.5f));
        if (on)
            DrawPolyline([new Vector2(x + 4, y + 10), new Vector2(x + 8.5f, y + 15), new Vector2(x + 16, y + 5)], Ui.VermilionInk, Line(3), true);
        Text(_gothic, new Vector2(x + 32, y + 16), label, fontSize: 17, modulate: Ui.Ink);
        float w = _gothic.GetStringSize(label, fontSize: 17).X + 32, h = hint is null ? 28 : 46;
        if (hint is not null && width > 0)
        {
            int lines = Wrap(hint, x + 32, y + 35, width - 32, 13, Ui.InkQuiet);
            w = width;
            h += (lines - 1) * WrapPitch(13);
        }
        else if (hint is not null)
        {
            Text(_gothic, new Vector2(x + 32, y + 35), hint, fontSize: 13, modulate: Ui.InkQuiet);
            w = Math.Max(w, _gothic.GetStringSize(hint, fontSize: 13).X + 32);
        }
        _spots.Add(new Spot(id, new Rect2(x - 4, y - 4, w + 8, h), () => { toggle(); QueueRedraw(); }));
        return y - 4 + h;
    }

    private void Radio(string id, float x, float y, bool on, string label, Font font, Action choose, bool enabled = true)
    {
        bool lit = enabled && _hover == id;
        RadioMark(new Vector2(x + 10, y + 10), on, lit);
        Text(font, new Vector2(x + 30, y + 17), label, fontSize: 18, modulate: enabled ? Ui.Ink : Ui.InkGrey);
        if (enabled)
            _spots.Add(new Spot(id, new Rect2(x - 4, y - 6, font.GetStringSize(label, fontSize: 18).X + 40, 32), () => { choose(); QueueRedraw(); }));
    }

    /// <summary>A radio's circle at <paramref name="centre"/>, its dot when chosen.</summary>
    private void RadioMark(Vector2 centre, bool on, bool lit)
    {
        DrawCircle(centre, 10, Field);
        DrawArc(centre, 10, 0, Mathf.Tau, 32, lit ? Ui.Vermilion : Ui.InkGrey, Line(lit ? 2 : 1.5f), true);
        if (on)
            DrawCircle(centre, 5, Ui.VermilionInk);
    }

    /// <summary>A track filled up to the knob (a vermilion disc), the value over it when given; dragging or clicking the track sets it.</summary>
    private void DrawSlider(float x0, float x1, float y, float fraction, string? label, Slider slider)
    {
        string id = slider.Id;
        bool lit = _hover == id || _dragging?.Id == id;
        DrawRect(new Rect2(x0, y - 3, x1 - x0, 6), Field);
        DrawRect(new Rect2(x0, y - 3, x1 - x0, 6), Ui.InkGrey, false, Line(1));
        float kx = x0 + (x1 - x0) * Math.Clamp(fraction, 0, 1);
        DrawRect(new Rect2(x0 + 1, y - 2, kx - x0, 4), Ui.VermilionInk);
        DrawCircle(new Vector2(kx, y), lit ? 10 : 9, Ui.Vermilion);
        DrawArc(new Vector2(kx, y), lit ? 10 : 9, 0, Mathf.Tau, 32, Ui.Ink, Line(1.5f), true);
        if (label is not null)
            Text(_gothic, new Vector2(kx - 30, y - 16), label, HorizontalAlignment.Center, 60, 13, Ui.Ink);
        _spots.Add(new Spot(id, new Rect2(x0 - 12, y - 14, x1 - x0 + 24, 28), () => { }, slider));
    }

    private void DrawLink(string id, string text, Vector2 at, Action click, bool right = false, bool enabled = true)
    {
        bool lit = enabled && _hover == id;
        float w = _gothic.GetStringSize(text, fontSize: 15).X;
        float x = right ? at.X - w : at.X;
        var colour = !enabled ? Ui.InkGrey : lit ? Ui.Vermilion : Ui.VermilionInk;
        Text(_gothic, new Vector2(x, at.Y + 5), text, fontSize: 15, modulate: colour);
        if (lit)
            DrawLine(new Vector2(x, at.Y + 8), new Vector2(x + w, at.Y + 8), colour, Line(1));
        if (enabled)
            _spots.Add(new Spot(id, new Rect2(x - 4, at.Y - 12, w + 8, 24), click));
    }

    /// <summary>A palette's swatch: its panel with its gold as a bar, framed in vermilion when chosen.</summary>
    private void Swatch(float x, float y, UiPalette palette, UiPalette chosen, Action<UiPalette> choose)
    {
        string id = $"palette-{palette.Key}";
        bool on = palette == chosen, lit = _hover == id;
        var rect = new Rect2(x, y, 64, 40);
        DrawRect(rect, palette.Panel with { A = 1 });
        DrawRect(new Rect2(x + 8, y + 26, 48, 5), palette.Gold);
        DrawRect(rect, on ? Ui.Vermilion : lit ? Ui.VermilionInk : Ui.InkGrey, false, Line(on ? 3 : lit ? 2 : 1));
        Text(_gothic, new Vector2(x - 13, y + 60), palette.Title, HorizontalAlignment.Center, 90, 13,
            on ? Ui.Ink : Ui.InkQuiet);
        _spots.Add(new Spot(id, new Rect2(x - 4, y - 4, 72, 70), () => { choose(palette); QueueRedraw(); }));
    }

    /// <summary>The question over the sheet: its title, its text, the confirmation as the red button and Cancel; the rest waits.</summary>
    private void DrawQuestion(Question question)
    {
        _spots.Clear();
        DrawRect(_phone is null ? Frame : new Rect2(Vector2.Zero, Size / _k), new Color(0, 0, 0, 0.35f));
        var box = _phone is { } f ? Part(f.IsUpright ? PhoneOptionsUpright.Question(f) : PhoneOptions.Question(f))
            : new Rect2(390, 300, 500, 200);
        Ui.DrawPanel(this, box, _k);
        Text(_cinzelBold, new Vector2(box.Position.X, box.Position.Y + 48), question.Title, HorizontalAlignment.Center,
            box.Size.X, 20, Ui.Name);
        Ui.DrawDivider(this, box.Position.X + 40, box.End.X - 40, box.Position.Y + 66, _k);
        Wrap(question.Text, box.Position.X + 40, box.Position.Y + 96, box.Size.X - 80, 15, Ui.Value);
        var confirm = new Rect2(box.End.X - 290, box.End.Y - 58, 130, 38);
        var cancel = new Rect2(box.End.X - 150, box.End.Y - 58, 110, 38);
        bool confirmLit = _hover == "confirm";
        Ui.DrawRedButton(this, confirm, confirmLit, confirmLit && _pressed == "confirm", _k);
        Text(_cinzelBold, new Vector2(confirm.Position.X, Lacquer.Baseline(confirm, _cinzelBold, 16)), question.Confirm,
            HorizontalAlignment.Center, confirm.Size.X, 16, Ui.SealText);
        Ui.DrawRowBox(this, cancel, _hover == "cancel", _k);
        Text(_gothic, new Vector2(cancel.Position.X, Lacquer.Baseline(cancel, _gothic, 16)), "Cancel", HorizontalAlignment.Center,
            cancel.Size.X, 16, _hover == "cancel" ? Ui.Name : Ui.Text);
        _spots.Add(new Spot("confirm", confirm, () =>
        {
            _question = null;
            question.Confirmed();
            QueueRedraw();
        }));
        _spots.Add(new Spot("cancel", cancel, Back));
    }

    private static string Format(BattleScreenSettings.Setting setting, double value) =>
        setting.Unit == "%" ? $"{value * 100:F0} %" : $"{value:F2} {setting.Unit}";

    /// <summary>Text wrapped at spaces into <paramref name="width"/>, <see cref="WrapPitch"/> apart; returns the lines drawn.</summary>
    private int Wrap(string text, float x, float baseline, float width, int size, Color colour)
    {
        string line = "";
        int lines = 0;
        foreach (string word in text.Split(' '))
        {
            string tried = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && _gothic.GetStringSize(tried, fontSize: size).X > width)
            {
                Text(_gothic, new Vector2(x, baseline), line, fontSize: size, modulate: colour);
                baseline += WrapPitch(size);
                lines++;
                line = word;
            }
            else
            {
                line = tried;
            }
        }
        if (line.Length > 0)
        {
            Text(_gothic, new Vector2(x, baseline), line, fontSize: size, modulate: colour);
            lines++;
        }
        return lines;
    }

    private static float WrapPitch(int size) => size * 1.45f;

    /// <summary>A path cut at its start to fit, as "…\Kichikuou Rance".</summary>
    private string Ellipsis(string text, float width, int size)
    {
        if (_gothic.GetStringSize(text, fontSize: size).X <= width)
            return text;
        for (int cut = 1; cut < text.Length; cut++)
        {
            string tried = "…" + text[cut..];
            if (_gothic.GetStringSize(tried, fontSize: size).X <= width)
                return tried;
        }
        return text;
    }
}
