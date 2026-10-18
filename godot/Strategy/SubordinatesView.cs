using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Subordinates screen (docs/ui/subordinates.md, the owner's mock-up F1): over the whole screen while
/// <c>Sﾌﾞｶ</c> is open and while one of its orders runs — the table of the units across the top
/// (<see cref="SubordinatesScreen.Columns"/>), the chosen unit's status CG at the bottom left, the orders'
/// panel at the bottom right (the budget, Replenish, Add New Troops' steps, Talk, End) and, after an order,
/// a plate saying what it did. It decides nothing: the presses go to <see cref="GameScreen"/>, which answers
/// the script through a <see cref="SubordinateOrder"/>.
/// </summary>
internal sealed partial class SubordinatesView : Control
{
    // In script pixels (640×400), after the mock-up's 1280×800.
    private static readonly Rect2 TableArea = new(8, 8, 624, 172);
    private static readonly Rect2 PortraitArea = new(8, 184, 333, 208);   // the picture panel's 314×196 as tall as the orders'
    private static readonly Rect2 PanelArea = new(345, 184, 287, 208);
    private static readonly Rect2 BudgetArea = new(6, 3, 275, 12);         // the panel's own, from here on
    private static readonly Rect2 ReplenishArea = new(6, 18, 275, 33);
    private static readonly Rect2 AddLabelArea = new(6, 56, 275, 14);
    private static readonly Rect2 StepsArea = new(6, 73, 275, 28);
    private static readonly Rect2 TalkArea = new(6, 114, 110, 42);
    private static readonly Rect2 ResultArea = new(122, 114, 159, 42);
    private static readonly Rect2 EndArea = new(211, 176, 70, 26);

    /// <summary>How long the plate stays (seconds).</summary>
    private const double ResultSeconds = 4;

    private DeploymentPalette _colours = DeploymentPalette.Chosen;   // Option → Palette
    private readonly FacePictures _faces;
    private readonly Theme _theme = new();
    private readonly ColorRect _backdrop;
    private readonly DeploymentTableView _table;
    private readonly TextureRect _portrait;
    private readonly Panel _panel;
    private readonly StyleBoxFlat _panelBox = new() { BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1 };
    private readonly Label _budget, _addLabel;
    private readonly OrderButton _replenish;
    private readonly HBoxContainer _steps;
    private readonly List<OrderButton> _stepButtons = [];
    private readonly Button _talk, _end;
    private readonly Panel _result;
    private readonly StyleBoxFlat _resultBox = new() { BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1 };
    private readonly Label _resultText;
    private FaceCrops _crops = new();
    private float _pixels = 1;
    private string _panelKey = "";
    private int _portraitCg = -1;
    private double _resultLeft;

    /// <summary>An order's button pressed: the order, the unit and Add More's step.</summary>
    public event Action<SubordinateOrderKind, int, int>? OrderPressed;

    /// <summary>End pressed, or a right press on the screen.</summary>
    public event Action? EndPressed;

    public SubordinatesView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        MouseFilter = MouseFilterEnum.Stop;                     // over everything of the screen, the map's plates included
        Visible = false;
        Theme = _theme;
        _backdrop = new ColorRect { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_backdrop);
        _table = new DeploymentTableView(font, SubordinatesScreen.Columns, chooser: true, textSize: 9) { Visible = true };
        _table.RowPressed += unit => _table.Chosen = unit;
        _table.RightPressed += () => EndPressed?.Invoke();
        AddChild(_table);
        _portrait = new TextureRect
        {
            MouseFilter = MouseFilterEnum.Ignore,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
        };
        AddChild(_portrait);
        _panel = new Panel { MouseFilter = MouseFilterEnum.Ignore };
        _panel.AddThemeStyleboxOverride("panel", _panelBox);
        AddChild(_panel);
        _budget = new Label { HorizontalAlignment = HorizontalAlignment.Right, MouseFilter = MouseFilterEnum.Ignore };
        _panel.AddChild(_budget);
        _replenish = new OrderButton("Replenish");
        _replenish.Pressed += () => Order(SubordinateOrderKind.Replenish, 0);
        _panel.AddChild(_replenish);
        _addLabel = new Label { Text = "Add New Troops", MouseFilter = MouseFilterEnum.Ignore };
        _panel.AddChild(_addLabel);
        _steps = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _steps.AddThemeConstantOverride("separation", 4);
        _panel.AddChild(_steps);
        _talk = new Button { Text = "Talk", FocusMode = FocusModeEnum.None, ExpandIcon = true, IconAlignment = HorizontalAlignment.Left };
        _talk.Pressed += () => Order(SubordinateOrderKind.Talk, 0);
        _panel.AddChild(_talk);
        _result = new Panel { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _result.AddThemeStyleboxOverride("panel", _resultBox);
        _resultText = new Label { MouseFilter = MouseFilterEnum.Ignore, VerticalAlignment = VerticalAlignment.Center };
        _result.AddChild(_resultText);
        _panel.AddChild(_result);
        _end = new Button { Text = "End", FocusMode = FocusModeEnum.None };
        _end.Pressed += () => EndPressed?.Invoke();
        _panel.AddChild(_end);
        foreach (var button in new Control[] { _replenish, _talk, _end })
            button.GuiInput += RightClick;
        ApplyPalette();
        SetFont(font);
    }

    /// <summary>The unit chosen in the table.</summary>
    public int Chosen => _table.Chosen;

    /// <summary>Whether the panel's buttons take presses (not while an order runs).</summary>
    public bool Busy { get; set; }

    /// <summary>Option → Palette: the screen in <paramref name="palette"/>'s colours at once.</summary>
    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        _table.SetPalette(palette);
        ApplyPalette();
    }

    /// <summary>Option → Fonts: the screen's text in the chosen font, as the deployment table's.</summary>
    public void SetFont(GameFont font)
    {
        _theme.DefaultFont = DeploymentTableView.LoadFont(font);
        _table.SetFont(font);
    }

    /// <summary>The squares of the faces: the built-in ones and the owner's edits from Debug → Portraits (read as the screen opens).</summary>
    public void LoadCrops()
    {
        _crops = FaceCropFile.Load();
        _panelKey = "";
    }

    private void ApplyPalette()
    {
        var c = _colours;
        _theme.SetColor("font_color", "Label", c.Text);
        _backdrop.Color = c.Back;
        _panelBox.BgColor = c.Back;
        _panelBox.BorderColor = c.Border;
        _resultBox.BgColor = c.Card;
        _resultBox.BorderColor = c.Accent;
        _budget.AddThemeColorOverride("font_color", c.Muted);
        Style(_replenish);
        Style(_talk);
        Style(_end);
        foreach (var step in _stepButtons)
            Style(step);
        _panelKey = "";
    }

    private void Style(Button b)
    {
        b.AddThemeStyleboxOverride("normal", ButtonBox(_colours.Button));
        b.AddThemeStyleboxOverride("hover", ButtonBox(_colours.ButtonHover));
        b.AddThemeStyleboxOverride("pressed", ButtonBox(_colours.ButtonPressed));
        b.AddThemeStyleboxOverride("disabled", ButtonBox(_colours.ButtonDisabled, _colours.Muted with { A = 0.4f }));
        b.AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color" })
            b.AddThemeColorOverride(state, _colours.ButtonText);
        b.AddThemeColorOverride("font_disabled_color", _colours.GreyText);
        if (b is OrderButton order)
            order.SetColours(_colours.ButtonText, _colours.Muted, _colours.GreyText);
    }

    private StyleBoxFlat ButtonBox(Color color, Color? border = null) => new()
    {
        BgColor = color, BorderColor = border ?? _colours.ButtonBorder,
        BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
    };

    /// <summary>
    /// Covers the screen whose script pixel (0, 0) is at <paramref name="origin"/> (in its parent's
    /// coordinates), <paramref name="pixelsPerScript"/> screen pixels to a script pixel.
    /// </summary>
    public void Place(Vector2 origin, float pixelsPerScript)
    {
        Position = Vector2.Zero;
        Size = GetParentAreaSize();
        void At(Control c, Rect2 area, Vector2 from)
        {
            c.Position = from + area.Position * pixelsPerScript;
            c.Size = area.Size * pixelsPerScript;
        }
        At(_backdrop, new Rect2(0, 0, 640, 400), origin);
        _table.Place(new Rect2(origin + TableArea.Position * pixelsPerScript, TableArea.Size * pixelsPerScript), pixelsPerScript);
        At(_portrait, PortraitArea, origin);
        At(_panel, PanelArea, origin);
        At(_budget, BudgetArea, Vector2.Zero);
        At(_replenish, ReplenishArea, Vector2.Zero);
        At(_addLabel, AddLabelArea, Vector2.Zero);
        At(_steps, StepsArea, Vector2.Zero);
        At(_talk, TalkArea, Vector2.Zero);
        At(_result, ResultArea, Vector2.Zero);
        At(_end, EndArea, Vector2.Zero);
        _resultText.Position = new Vector2(6, 0) * pixelsPerScript;
        _resultText.Size = (ResultArea.Size - new Vector2(12, 0)) * pixelsPerScript;
        int size = Math.Max(8, (int)Math.Round(9 * pixelsPerScript));
        _theme.DefaultFontSize = size;
        _talk.AddThemeFontSizeOverride("font_size", Math.Max(10, (int)Math.Round(11 * pixelsPerScript)));
        _end.AddThemeFontSizeOverride("font_size", Math.Max(10, (int)Math.Round(10 * pixelsPerScript)));
        _replenish.SetSizes(Math.Max(10, (int)Math.Round(11 * pixelsPerScript)), Math.Max(8, (int)Math.Round(7.5f * pixelsPerScript)));
        foreach (var step in _stepButtons)
            step.SetSizes(Math.Max(10, (int)Math.Round(10 * pixelsPerScript)), Math.Max(8, (int)Math.Round(7 * pixelsPerScript)));
        if (pixelsPerScript != _pixels)
        {
            _pixels = pixelsPerScript;
            _panelKey = "";                                     // the face shrunk to the new size
        }
    }

    /// <summary>
    /// The table and the panel from <paramref name="state"/>; the chosen row kept while it is listed, else
    /// the first unit that can act (<see cref="SubordinatesScreen.FirstChoice"/>).
    /// </summary>
    public void Refresh(GameState state, double delta)
    {
        _table.Refresh(state);
        if (_table.Chosen == 0 || !_table.Lists(_table.Chosen))
            _table.Chosen = SubordinatesScreen.FirstChoice(state);
        if (_resultLeft > 0 && (_resultLeft -= delta) <= 0)
            _result.Visible = false;
        if (_table.Chosen == 0)
            return;
        var panel = SubordinatesScreen.Panel(state, _table.Chosen);
        string key = $"{panel}|{string.Join(",", panel.Steps)}|{Busy}";
        if (key == _panelKey)
            return;
        _panelKey = key;
        Fill(panel);
    }

    /// <summary>What an order did, on the plate beside Talk for a few seconds.</summary>
    public void ShowResult(SubordinateResult result)
    {
        _resultText.Text = $"{result.Line}\n{result.Budget}";
        _result.Visible = true;
        _resultLeft = ResultSeconds;
    }

    private void Fill(SubordinatePanel panel)
    {
        _budget.Text = $"Budget {panel.Gold * 100}K";
        if (panel.PortraitCg != _portraitCg)
        {
            _portraitCg = panel.PortraitCg;
            _portrait.Texture = PicturePanel(panel.PortraitCg);
        }
        var r = panel.Replenish;
        _replenish.Set(r.Enabled && !Busy, r.Enabled ? $"+{r.Men} troops · {r.Price * 100}K" : r.Note);

        _addLabel.Visible = panel.AddOffered;
        while (_stepButtons.Count < panel.Steps.Count)
        {
            var b = new OrderButton("");
            int step = _stepButtons.Count;
            b.Pressed += () => Order(SubordinateOrderKind.AddTroops, step);
            b.GuiInput += RightClick;
            Style(b);
            _stepButtons.Add(b);
            _steps.AddChild(b);
        }
        for (int i = 0; i < _stepButtons.Count; i++)
        {
            var b = _stepButtons[i];
            b.Visible = i < panel.Steps.Count;
            if (!b.Visible)
                continue;
            var s = panel.Steps[i];
            b.Title = $"+{s.Men}";
            b.Set(s.Enabled && !Busy, $"{s.Price * 100}K");
            b.TooltipText = s.Enabled ? "" : s.Note;
            b.SetSizes(Math.Max(10, (int)Math.Round(10 * _pixels)), Math.Max(8, (int)Math.Round(7 * _pixels)));
        }
        // A step as wide as a quarter of the row; more than four share it.
        int across = Math.Max(4, panel.Steps.Count);
        float width = (StepsArea.Size.X - 4 * (across - 1)) / across * _pixels;
        foreach (var b in _stepButtons)
            b.CustomMinimumSize = new Vector2(width, 0);

        var t = panel.Talk;
        _talk.Disabled = !t.Enabled || Busy;
        _talk.TooltipText = t.Enabled ? "" : t.Note;
        _talk.Icon = panel.FaceCg == 0 ? null : _faces.SquareTexture(panel.FaceCg, _faces.CropOf(panel.FaceCg, _crops),
            Math.Max(16, (int)Math.Round((TalkArea.Size.Y - 8) * _pixels)));
        _end.Disabled = Busy;
    }

    /// <summary>
    /// The picture panel as <c>Sﾌﾞｶ</c> shows it: the screen's frame (<c>G 870, 40</c>, then <c>G 600</c>,
    /// <c>Sﾌﾞｶ.ADV:5-13</c>) with the unit's status CG drawn into it at its own place (<c>:144</c>), cut out
    /// as the item picker's prompt cuts the same panel (script pixels (6, 6), 314×196) — the frame is the
    /// background's, not the CG's.
    /// </summary>
    private ImageTexture? PicturePanel(int portraitCg)
    {
        var cgs = _faces.Cgs;
        if (cgs.Get(portraitCg) is not { } portrait)
            return null;
        var screen = new Surface(Surface.ScreenWidth, Surface.ScreenHeight);
        if (cgs.Get(870) is { } frame)
            screen.DrawCg(frame, frame.X, frame.Y, Surface.SpriteKey);
        if (cgs.Get(600) is { } empty)
            screen.DrawCg(empty, empty.X, empty.Y);
        screen.DrawCg(portrait, portrait.X, portrait.Y);
        var (x, y, w, h) = PictureCut;
        return ImageTexture.CreateFromImage(Image.CreateFromData(w * Surface.Scale, h * Surface.Scale, false,
            Image.Format.Rgba8, screen.ReadArea(x, y, w, h)));
    }

    /// <summary>The picture panel on the original's screen (script pixels), <c>Sｱｲﾃﾑ.ADV:101-110</c>'s cut.</summary>
    private static readonly (int X, int Y, int W, int H) PictureCut = (6, 6, 314, 196);

    private void Order(SubordinateOrderKind kind, int step)
    {
        if (!Busy && _table.Chosen != 0)
            OrderPressed?.Invoke(kind, _table.Chosen, step);
    }

    /// <summary>An order's button as the mouse presses it (unattended play goes through here).</summary>
    public void Press(SubordinateOrderKind kind, int unit, int step = 0)
    {
        _table.Chosen = unit;
        OrderPressed?.Invoke(kind, unit, step);
    }

    private void RightClick(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true } && !Busy)
        {
            EndPressed?.Invoke();
            AcceptEvent();
        }
    }

    public override void _GuiInput(InputEvent e) => RightClick(e);

    /// <summary>A button with a title over a smaller note (its figures, or why it is greyed).</summary>
    private sealed partial class OrderButton : Button
    {
        private readonly Label _title, _note;
        private Color _text, _muted, _grey;

        public OrderButton(string title)
        {
            FocusMode = FocusModeEnum.None;
            var column = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
            column.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            column.AddThemeConstantOverride("separation", 0);
            _title = new Label { Text = title, HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            _note = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            column.AddChild(_title);
            column.AddChild(_note);
            AddChild(column);
        }

        public string Title
        {
            get => _title.Text;
            set => _title.Text = value;
        }

        public void Set(bool enabled, string note)
        {
            Disabled = !enabled;
            _note.Text = note;
            _title.AddThemeColorOverride("font_color", enabled ? _text : _grey);
            _note.AddThemeColorOverride("font_color", enabled ? _muted : _grey);
        }

        public void SetColours(Color text, Color muted, Color grey) => (_text, _muted, _grey) = (text, muted, grey);

        public void SetSizes(int title, int note)
        {
            _title.AddThemeFontSizeOverride("font_size", title);
            _note.AddThemeFontSizeOverride("font_size", note);
        }
    }
}
