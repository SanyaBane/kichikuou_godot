using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The options window (docs/ui/options.md; the owner's mock-up D, 2026-10-21): every setting of the menu bar's Option, over
/// the game, which waits while it is open. A paper sheet in a lacquer frame with paper tabs across its top — Messages
/// (the text speed with the game's 121 typing as its preview, the message skip's switches, Forget, the mouse, the
/// already-read text's colour, <see cref="ReadTextChoice"/>, docs/ui/read-text.md), Display
/// (the font, the palette), Battle (our battle screen's effects and timings, <see cref="BattleScreenSettings"/>, the order
/// strip as their preview), Game (the game folder, "New scenes first", <see cref="NewScenesChoice"/>). Every change applies
/// and is kept at once; Defaults resets the tab shown (not the game folder); Close, Esc or a right click shut it. Forget and the game folder ask first, in a box over the sheet. Laid out
/// in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class OptionsView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Settings);

    public enum Tab { Messages, Display, Battle, Game }

    private static readonly Tab[] Tabs = [Tab.Messages, Tab.Display, Tab.Battle, Tab.Game];
    private static readonly Rect2 Frame = new(100, 56, 1080, 704);
    private static readonly Rect2 Sheet = new(126, 126, 1028, 608);
    private static readonly Rect2 CloseButton = new(1004, 686, 130, 40);
    private const float TabTop = 82, TabFoot = 127;
    private const float ContentX = 166, ContentY = 156, ContentW = 948;
    private static Color Field => Ui.Field;

    private static readonly string[] PreviewLines =
        ["I believe that we should first", "prepare our cities for their", "attacks, then defend them with", "urban warfare."];
    private const float PreviewHold = 1.2f;         // seconds the whole preview stays before it types again

    // The week-26 battle's first round: Barres, hit hard, jumps ahead of Leila (as the old Battle Screen dialog had it).
    private static readonly OrderChip[] Before =
    [
        new(BattleSide.Enemy, 4, 2, "Melfeis", ChipState.Acting), new(BattleSide.Player, 5, 4, "Leila", ChipState.Waiting),
        new(BattleSide.Player, 10, 3, "Menad", ChipState.Waiting), new(BattleSide.Player, 1, 1, "Rance", ChipState.Waiting),
        new(BattleSide.Player, 6, 2, "Barres", ChipState.Waiting), new(BattleSide.Enemy, 5, 1, "Dodge", ChipState.Waiting),
    ];
    private static readonly OrderChip[] After = [Before[0], Before[4], Before[1], Before[2], Before[3], Before[5]];

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private readonly FontVariation _cinzelBold = Lacquer.Cinzel(800);
    private FontFile _gothic;
    private readonly FontFile _bizSample = DeploymentTableView.LoadFont(GameFont.BizUdGothic);
    private readonly FontFile? _msSample = GodotGlyphRenderer.MsGothicInstalled ? DeploymentTableView.LoadFont(GameFont.MsGothic) : null;
    private readonly MessageSkip _skip;
    private readonly bool _folderLocked;
    private readonly MessageWindowView _preview;
    private readonly MessageWindow _sample = MessageWindow.Sample(PreviewLines, read: true);
    private readonly OrderStrip _strip = new();

    private Tab _tab;
    private float _k = 1;                           // window pixels to a screen pixel
    private readonly List<Spot> _spots = [];        // what the last drawing made clickable
    private string? _hover, _pressed;
    private Slider? _dragging;
    private double _held;
    private bool _jumped;
    private Question? _question;

    private sealed record Spot(string Id, Rect2 Rect, Action Click, Slider? Slider = null);

    private sealed record Slider(string Id, float X0, float X1, double Min, double Max, double Step, Action<double> Set);

    private const float SpeedColumn = 470;

    /// <summary>Where the preview's 121 sits on the Messages tab: under the speed's slider, as wide as its column.</summary>
    private Rect2 PreviewRect => new(ContentX, ContentY + 96, SpeedColumn, SpeedColumn * _sample.Panel.H / _sample.Panel.W);

    private static readonly int TimingRows = (BattleScreenSettings.All.Count(s => s.Unit != BattleScreenSettings.Toggle) + 1) / 2;

    /// <summary>Where the order strip's preview sits on the Battle tab: under the timings, Preview right of it.</summary>
    private static Rect2 StripRect => new(ContentX, ContentY + 96 + 36 + TimingRows * 46 + 6, ContentW - 130, 46);

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

    /// <summary>The player asked for another game folder and confirmed it: the import screen follows.</summary>
    public event Action? GameFolderChosen;

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
        _preview = new MessageWindowView(GodotGlyphRenderer.Chosen) { Tail = false, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_preview);
        _preview.Show(_sample, true, null, null);
        _preview.LettersPerSecond = TextSpeedChoice.LettersPerSecond(TextSpeedChoice.Step);
        AddChild(_strip);
        _strip.Show(Before, instant: true);
        ShowTab(tab);
    }

    public Tab Current => _tab;

    /// <summary>Lays the window over the screen's picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        float k = area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        PlaceChildren();
        QueueRedraw();
    }

    public void ShowTab(Tab tab)
    {
        _tab = tab;
        _question = null;
        _dragging = null;
        _preview.Visible = tab == Tab.Messages;
        _strip.Visible = tab == Tab.Battle;
        if (tab == Tab.Messages)
            _preview.Restart();
        PlaceChildren();
        QueueRedraw();
    }

    /// <summary>The preview's 121 scaled into its place on the Messages tab, the order strip into its own on Battle.</summary>
    private void PlaceChildren()
    {
        var panel = _sample.Panel;
        var preview = PreviewRect;
        float kp = preview.Size.X / panel.W;
        var origin = (preview.Position - new Vector2(panel.X, panel.Y) * kp) * _k;
        _preview.Place(new Rect2(origin, new Vector2(1280, 800) * kp * _k));
        _strip.Position = StripRect.Position * _k;
        _strip.Size = StripRect.Size * _k;
        _strip.SetScale(_k);
    }

    public override void _Process(double delta)
    {
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

    private void SetNewScenesFirst(bool on)
    {
        NewScenesChoice.On = on;
        NewScenesFirstChanged?.Invoke(on);
    }

    /// <summary>Defaults: the tab shown back to its settings' defaults.</summary>
    private void Defaults()
    {
        switch (_tab)
        {
            case Tab.Messages:
                SetTextSpeed(TextSpeedChoice.DefaultStep);
                SetReadColour(ReadTextChoice.DefaultIndex);
                _skip.SkipUnseen = false;
                _skip.FastFades = true;
                EngineState.MouseMovement = true;
                break;
            case Tab.Display:
                SetFont(GameFont.BizUdGothic);
                SetPalette(UiPalette.Lacquer);
                SetThousands(ThousandsSeparator.Comma);
                break;
            case Tab.Battle:
                foreach (var setting in BattleScreenSettings.All)
                    BattleScreenSettings.Set(setting, setting.Default);
                break;
            case Tab.Game:
                SetNewScenesFirst(true);                // the game folder is not a setting with a default
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
        if (e is not InputEventKey { Pressed: true, Keycode: Key.Escape })
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
        DrawRect(new Rect2(0, 0, 1280, 800), new Color(0, 0, 0, 0.55f));
        Ui.DrawPanel(this, Frame, _k);
        DrawTabs();
        DrawString(_brush, new Vector2(1150 - _brush.GetStringSize("Options", fontSize: 28).X, 116), "Options", fontSize: 28,
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
        }
        DrawLink("defaults", "Defaults", new Vector2(150, 706), Defaults);
        bool closeLit = _hover == "close";
        Ui.DrawRedButton(this, CloseButton, closeLit, closeLit && _pressed == "close", _k);
        DrawString(_cinzelBold, new Vector2(CloseButton.Position.X, Lacquer.Baseline(CloseButton, _cinzelBold, 17)), "Close",
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
            DrawString(on ? _cinzelBold : _cinzel, new Vector2(x, Lacquer.Baseline(rect, _cinzelBold, 18)), name,
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
        int step = TextSpeedChoice.Step;
        DrawSlider(x + 60, x + lw - 70, y + 66, (step - 1) / (float)(TextSpeedChoice.Steps - 1), step.ToString(),
            new Slider("speed", x + 60, x + lw - 70, 1, TextSpeedChoice.Steps, 1,
                v => { if ((int)v != TextSpeedChoice.Step) SetTextSpeed((int)v); }));
        DrawString(_gothic, new Vector2(x, y + 71), "Slow", fontSize: 14, modulate: Ui.InkQuiet);
        DrawString(_gothic, new Vector2(x, y + 71), "Instant", HorizontalAlignment.Right, lw, 14, Ui.InkQuiet);

        float rx = x + lw + 50, rw = ContentW - lw - 50;
        SectionHead(rx, y, "Message skip", rw);
        CheckBox("skip-unseen", rx, y + 38, _skip.SkipUnseen, "Skip unseen text", "MsgSkip and Ctrl skip unread ones too.",
            () => _skip.SkipUnseen = !_skip.SkipUnseen);
        CheckBox("fast-fades", rx, y + 92, _skip.FastFades, "MsgSkip speeds up fades", "Fades and waits run fast.",
            () => _skip.FastFades = !_skip.FastFades);
        DrawString(_gothic, new Vector2(rx, y + 165), $"{_skip.SeenCount} messages seen.", fontSize: 14, modulate: Ui.InkQuiet);
        DrawLink("forget", "Forget them…", new Vector2(rx + rw, y + 160),
            () => Ask("Forget seen messages?", $"All {_skip.SeenCount} messages seen so far will count as unseen again.", "Forget",
                () => _skip.ForgetSeen()), right: true, enabled: _skip.SeenCount > 0);
        SectionHead(rx, y + 220, "Mouse", rw);
        CheckBox("mouse", rx, y + 258, EngineState.MouseMovement, "Mouse movement", "The pointer moves to the chosen item.",
            () => EngineState.MouseMovement = !EngineState.MouseMovement);

        int chosen = ReadTextChoice.Index;
        DrawString(_gothic, new Vector2(x + 14, y + 281), chosen == ReadTextChoice.None ? "Read text keeps the window's colour."
            : "The preview counts as read.", fontSize: 13, modulate: Ui.InkQuiet);
        y += 330;
        SectionHead(x, y, "Already-read text", ContentW);
        float tx = x;
        ReadTile("read-none", new Rect2(tx, y + 40, 150, 44), chosen == ReadTextChoice.None, null, ReadTextChoice.None);
        tx += 160;
        for (int i = 0; i < ReadTextChoice.Colours.Length; i++, tx += 54)
            ReadTile("read-" + i, new Rect2(tx, y + 40, 44, 44), chosen == i, ReadTextChoice.Colours[i].Colour, i);
        DrawString(_gothic, new Vector2(x, y + 128), chosen == ReadTextChoice.None ? "Don't change" : ReadTextChoice.Colours[chosen].Name,
            fontSize: 17, modulate: Ui.Ink);
        DrawString(_gothic, new Vector2(x, y + 150), "A message window you have read before, in any game, shows its text in this colour.",
            fontSize: 13, modulate: Ui.InkQuiet);
        DrawString(_gothic, new Vector2(x, y + 170), "Speech balloons, menus and our screens keep their own. Forget them… counts them unread.",
            fontSize: 13, modulate: Ui.InkQuiet);
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
            DrawString(_gothic, new Vector2(rect.Position.X, Lacquer.Baseline(rect, _gothic, 16)), "Don't change",
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
        SectionHead(x, y, "Font", w);
        Radio("font-ms", x, y + 38, GodotGlyphRenderer.Chosen == GameFont.MsGothic,
            _msSample is null ? "MS Gothic — not installed" : "MS Gothic — 鬼畜王ランス, Rance", _msSample ?? _gothic,
            () => SetFont(GameFont.MsGothic), enabled: _msSample is not null);
        Radio("font-biz", x, y + 74, GodotGlyphRenderer.Chosen == GameFont.BizUdGothic, "BIZ UDGothic — 鬼畜王ランス, Rance",
            _bizSample, () => SetFont(GameFont.BizUdGothic));
        y += 132;
        SectionHead(x, y, "Palette", w);
        for (int i = 0; i < UiPalette.All.Count; i++)
            Swatch(x + i * 90, y + 40, UiPalette.All[i], UiPalettes.Chosen, SetPalette);
        y += 132;
        SectionHead(x, y, "Thousands in gold, troops and damage", w);
        for (int i = 0; i < ThousandsChoice.All.Length; i++)
        {
            var separator = ThousandsChoice.All[i];
            Radio("thousands-" + separator, x + i * (w / ThousandsChoice.All.Length), y + 38, ThousandsChoice.Chosen == separator,
                ThousandsChoice.Sample(separator), _gothic, () => SetThousands(separator));
        }
    }

    private void DrawBattle()
    {
        float x = ContentX, y = ContentY, w = ContentW;
        SectionHead(x, y, "Effects", w);
        var effects = BattleScreenSettings.Effects;
        CheckBox("effects", x, y + 36, BattleScreenSettings.Get(effects) >= 0.5, "Exchange effects", effects.Hint,
            () => BattleScreenSettings.Set(effects, BattleScreenSettings.Get(effects) >= 0.5 ? 0 : 1));
        y += 96;
        SectionHead(x, y, "Timings", w);
        y += 36;                                    // StripRect follows the rows
        float colW = (w - 40) / 2;
        int i = 0;
        foreach (var setting in BattleScreenSettings.All.Where(s => s.Unit != BattleScreenSettings.Toggle))
        {
            float cx = x + i % 2 * (colW + 40), cy = y + i / 2 * 46;
            double value = BattleScreenSettings.Get(setting);
            DrawString(_gothic, new Vector2(cx, cy + 13), setting.Label, fontSize: 14, modulate: Ui.Ink);
            DrawSlider(cx, cx + colW - 80, cy + 29, (float)((value - setting.Min) / (setting.Max - setting.Min)), null,
                new Slider("battle-" + setting.Key, cx, cx + colW - 80, setting.Min, setting.Max, setting.Step,
                    v => BattleScreenSettings.Set(setting, v)));
            DrawString(_gothic, new Vector2(cx, cy + 34), Format(setting, value), HorizontalAlignment.Right, colW, 14, Ui.Ink);
            i++;
        }
        var strip = StripRect;
        DrawRect(strip, UiPalettes.For(UiPart.Battle).Panel with { A = 1 });
        DrawRect(strip, Ui.InkGrey, false, Line(1));
        var preview = new Rect2(x + w - 110, strip.Position.Y + 6, 110, 34);
        Button("preview", preview, "Preview", () =>
        {
            _jumped = !_jumped;
            _strip.Show(_jumped ? After : Before, instant: false);
        });
    }

    private void DrawGame()
    {
        float x = ContentX, y = ContentY, w = ContentW;
        SectionHead(x, y, "Game folder", w);
        var field = new Rect2(x, y + 36, w, 40);
        bool lit = !_folderLocked && _hover == "folder";
        DrawRect(field, Field);
        DrawRect(field, lit ? Ui.Vermilion : Ui.InkGrey, false, Line(lit ? 2 : 1));
        var icon = new Vector2(x + 12, y + 48);
        DrawPolyline([icon + new Vector2(0, 2), icon + new Vector2(7, 2), icon + new Vector2(9, 5), icon + new Vector2(20, 5),
            icon + new Vector2(20, 16), icon + new Vector2(0, 16), icon + new Vector2(0, 2)], Ui.InkQuiet, Line(2));
        string folder = GameData.GameDirectory is { Length: > 0 } dir ? dir : "not chosen";
        DrawString(_gothic, new Vector2(x + 44, y + 62), Ellipsis(folder, w - 190, 17), fontSize: 17, modulate: Ui.Ink);
        DrawString(_gothic, new Vector2(x, y + 61), "Change…", HorizontalAlignment.Right, w - 14, 15,
            _folderLocked ? Ui.InkGrey : lit ? Ui.Vermilion : Ui.VermilionInk);
        if (!_folderLocked)
            _spots.Add(new Spot("folder", field, () => Ask("Change the game folder?",
                "Any unsaved progress will be lost: the import screen opens, then the title.", "Change", () => GameFolderChosen?.Invoke())));
        string note = _folderLocked
            ? "The developer's extract is the data (local.env or settings.cfg): a game folder would change nothing. --import turns this on."
            : "Another copy of the game: the import screen checks it and reads its scenario, then the title opens.";
        Wrap(note, x, y + 104, w, 14, Ui.InkQuiet);

        y += 170;
        SectionHead(x, y, "Scenes", w);
        CheckBox("new-scenes", x, y + 38, NewScenesChoice.On, "New scenes first",
            "Talks and harem visits play unread scenes first, with a few hints and a choice of ours.",
            () => SetNewScenesFirst(!NewScenesChoice.On));
    }

    // ---------------------------------------------------------------- the parts

    private void SectionHead(float x, float y, string title, float width)
    {
        DrawString(_cinzelBold, new Vector2(x, y + 14), title.ToUpperInvariant(), fontSize: 13, modulate: Ui.HeadInk);
        DrawLine(new Vector2(x, y + 22), new Vector2(x + width, y + 22), Ui.PaperRule, Line(1));
    }

    private void CheckBox(string id, float x, float y, bool on, string label, string? hint, Action toggle)
    {
        bool lit = _hover == id;
        var box = new Rect2(x, y, 20, 20);
        DrawRect(box, Field);
        DrawRect(box, lit ? Ui.Vermilion : Ui.InkGrey, false, Line(lit ? 2 : 1.5f));
        if (on)
            DrawPolyline([new Vector2(x + 4, y + 10), new Vector2(x + 8.5f, y + 15), new Vector2(x + 16, y + 5)], Ui.VermilionInk, Line(3), true);
        DrawString(_gothic, new Vector2(x + 32, y + 16), label, fontSize: 17, modulate: Ui.Ink);
        float w = _gothic.GetStringSize(label, fontSize: 17).X + 32;
        if (hint is not null)
        {
            DrawString(_gothic, new Vector2(x + 32, y + 35), hint, fontSize: 13, modulate: Ui.InkQuiet);
            w = Math.Max(w, _gothic.GetStringSize(hint, fontSize: 13).X + 32);
        }
        _spots.Add(new Spot(id, new Rect2(x - 4, y - 4, w + 8, hint is null ? 28 : 46), () => { toggle(); QueueRedraw(); }));
    }

    private void Radio(string id, float x, float y, bool on, string label, Font font, Action choose, bool enabled = true)
    {
        bool lit = enabled && _hover == id;
        var centre = new Vector2(x + 10, y + 10);
        DrawCircle(centre, 10, Field);
        DrawArc(centre, 10, 0, Mathf.Tau, 32, lit ? Ui.Vermilion : Ui.InkGrey, Line(lit ? 2 : 1.5f), true);
        if (on)
            DrawCircle(centre, 5, Ui.VermilionInk);
        DrawString(font, new Vector2(x + 30, y + 17), label, fontSize: 18, modulate: enabled ? Ui.Ink : Ui.InkGrey);
        if (enabled)
            _spots.Add(new Spot(id, new Rect2(x - 4, y - 6, font.GetStringSize(label, fontSize: 18).X + 40, 32), () => { choose(); QueueRedraw(); }));
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
            DrawString(_gothic, new Vector2(kx - 30, y - 16), label, HorizontalAlignment.Center, 60, 13, Ui.Ink);
        _spots.Add(new Spot(id, new Rect2(x0 - 12, y - 14, x1 - x0 + 24, 28), () => { }, slider));
    }

    private void DrawLink(string id, string text, Vector2 at, Action click, bool right = false, bool enabled = true)
    {
        bool lit = enabled && _hover == id;
        float w = _gothic.GetStringSize(text, fontSize: 15).X;
        float x = right ? at.X - w : at.X;
        var colour = !enabled ? Ui.InkGrey : lit ? Ui.Vermilion : Ui.VermilionInk;
        DrawString(_gothic, new Vector2(x, at.Y + 5), text, fontSize: 15, modulate: colour);
        if (lit)
            DrawLine(new Vector2(x, at.Y + 8), new Vector2(x + w, at.Y + 8), colour, Line(1));
        if (enabled)
            _spots.Add(new Spot(id, new Rect2(x - 4, at.Y - 12, w + 8, 24), click));
    }

    private void Button(string id, Rect2 rect, string text, Action click)
    {
        bool lit = _hover == id;
        DrawRect(rect, lit ? Ui.Paper : Field);
        DrawRect(rect, lit ? Ui.Vermilion : Ui.InkGrey, false, Line(lit ? 2 : 1));
        DrawString(_gothic, new Vector2(rect.Position.X, Lacquer.Baseline(rect, _gothic, 15)), text, HorizontalAlignment.Center,
            rect.Size.X, 15, lit ? Ui.Vermilion : Ui.VermilionInk);
        _spots.Add(new Spot(id, rect, click));
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
        DrawString(_gothic, new Vector2(x - 13, y + 60), palette.Title, HorizontalAlignment.Center, 90, 13,
            on ? Ui.Ink : Ui.InkQuiet);
        _spots.Add(new Spot(id, new Rect2(x - 4, y - 4, 72, 70), () => { choose(palette); QueueRedraw(); }));
    }

    /// <summary>The question over the sheet: its title, its text, the confirmation as the red button and Cancel; the rest waits.</summary>
    private void DrawQuestion(Question question)
    {
        _spots.Clear();
        DrawRect(Frame, new Color(0, 0, 0, 0.35f));
        var box = new Rect2(390, 300, 500, 200);
        Ui.DrawPanel(this, box, _k);
        DrawString(_cinzelBold, new Vector2(box.Position.X, box.Position.Y + 48), question.Title, HorizontalAlignment.Center,
            box.Size.X, 20, Ui.Name);
        Ui.DrawDivider(this, box.Position.X + 40, box.End.X - 40, box.Position.Y + 66, _k);
        Wrap(question.Text, box.Position.X + 40, box.Position.Y + 96, box.Size.X - 80, 15, Ui.Value);
        var confirm = new Rect2(box.End.X - 290, box.End.Y - 58, 130, 38);
        var cancel = new Rect2(box.End.X - 150, box.End.Y - 58, 110, 38);
        bool confirmLit = _hover == "confirm";
        Ui.DrawRedButton(this, confirm, confirmLit, confirmLit && _pressed == "confirm", _k);
        DrawString(_cinzelBold, new Vector2(confirm.Position.X, Lacquer.Baseline(confirm, _cinzelBold, 16)), question.Confirm,
            HorizontalAlignment.Center, confirm.Size.X, 16, Ui.SealText);
        Ui.DrawRowBox(this, cancel, _hover == "cancel", _k);
        DrawString(_gothic, new Vector2(cancel.Position.X, Lacquer.Baseline(cancel, _gothic, 16)), "Cancel", HorizontalAlignment.Center,
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

    /// <summary>Text wrapped at spaces into <paramref name="width"/>.</summary>
    private void Wrap(string text, float x, float baseline, float width, int size, Color colour)
    {
        string line = "";
        foreach (string word in text.Split(' '))
        {
            string tried = line.Length == 0 ? word : line + " " + word;
            if (line.Length > 0 && _gothic.GetStringSize(tried, fontSize: size).X > width)
            {
                DrawString(_gothic, new Vector2(x, baseline), line, fontSize: size, modulate: colour);
                baseline += size * 1.45f;
                line = word;
            }
            else
            {
                line = tried;
            }
        }
        if (line.Length > 0)
            DrawString(_gothic, new Vector2(x, baseline), line, fontSize: size, modulate: colour);
    }

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
