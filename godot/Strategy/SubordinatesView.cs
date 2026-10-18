using System.Globalization;
using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Subordinates screen (docs/ui/subordinates.md, the owner's layout F1 in the look of mock-up G3): over the
/// whole screen while <c>Sﾌﾞｶ</c> is open and while one of its orders runs — a dark lacquer backdrop with a faint
/// gold wave pattern; the table of the units across the top as a paper ledger in a lacquer panel
/// (<see cref="SubordinatesScreen.Columns"/>, <see cref="DeploymentTableView"/> as a ledger); the chosen unit's
/// status CG at the bottom left, in the original screen's gold frame; the orders' panel at the bottom right (the
/// budget, Replenish in red lacquer, Add New Troops' steps, Talk with the unit's face, End) and, after an order, a
/// plate saying what it did. Drawn by hand in the 1280×800 screen's pixels, as our map panels (<see cref="Lacquer"/>).
/// It decides nothing: the presses go to <see cref="GameScreen"/>, which answers the script through a
/// <see cref="SubordinateOrder"/>.
/// </summary>
internal sealed partial class SubordinatesView : Control
{
    // In the 1280×800 screen's pixels (the script's 640×400 at 2×), after the mock-ups.
    private static readonly Rect2 TablePanel = new(16, 16, 1248, 344);
    private static readonly Rect2 TableArea = new(30, 66, 1220, 282);     // the ledger
    private static readonly Rect2 PictureArea = new(16, 368, 666, 416);   // the picture panel's 314×196 as tall as the orders'
    private static readonly Rect2 OrdersPanel = new(690, 368, 574, 416);
    private static readonly Rect2 ReplenishArea = new(702, 424, 550, 60);
    private const float AddLabelY = 500, StepsTop = 514, StepsHeight = 56, StepGap = 6, RuleY = 586;
    private static readonly Rect2 TalkArea = new(702, 598, 220, 84);
    private static readonly Rect2 ResultArea = new(934, 598, 318, 84);
    private static readonly Rect2 EndArea = new(1112, 718, 140, 52);

    /// <summary>How long the plate stays (seconds).</summary>
    private const double ResultSeconds = 4;

    // The buttons, as the view tells them apart: Replenish, Talk, End, then Add More's steps from StepButton.
    private const int NoButton = -1, ReplenishButton = 0, TalkButton = 1, EndButton = 2, StepButton = 10;

    private readonly FacePictures _faces;
    private readonly Layer _backdrop, _face;
    private readonly DeploymentTableView _table;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700), _cinzelLight = Lacquer.Cinzel(600);
    private FontFile _gothic;
    private FaceCrops _crops = new();
    private Vector2 _origin;
    private float _k = 1;                               // window pixels to a screen pixel
    private SubordinatePanel? _panel;
    private string _panelKey = "", _date = "";
    private int _ready;
    private int _portraitCg = -1, _faceCg = -1, _faceSize;
    private ImageTexture? _portrait, _faceTexture;
    private SubordinateResult? _result;
    private double _resultLeft;
    private int _hovered = NoButton, _pressed = NoButton;

    /// <summary>An order's button pressed: the order, the unit and Add More's step.</summary>
    public event Action<SubordinateOrderKind, int, int>? OrderPressed;

    /// <summary>End pressed, or a right press on the screen.</summary>
    public event Action? EndPressed;

    public SubordinatesView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;                     // over everything of the screen, the map's plates included
        Visible = false;
        _backdrop = new Layer(DrawBackdrop) { ClipContents = true };
        AddChild(_backdrop);
        _face = new Layer(DrawScreen);
        AddChild(_face);
        _table = new DeploymentTableView(font, SubordinatesScreen.Columns, chooser: true, textSize: 9, ledger: true) { Visible = true };
        _table.RowPressed += unit => _table.Chosen = unit;
        _table.RightPressed += () => EndPressed?.Invoke();
        AddChild(_table);
    }

    /// <summary>The unit chosen in the table.</summary>
    public int Chosen => _table.Chosen;

    /// <summary>Whether the panel's buttons take presses (not while an order runs).</summary>
    public bool Busy { get; set; }

    /// <summary>Option → Fonts: the screen's text in the chosen font, as the deployment table's.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        _table.SetFont(font);
        _face.QueueRedraw();
    }

    /// <summary>The squares of the faces: the built-in ones and the owner's edits from Debug → Portraits (read as the screen opens).</summary>
    public void LoadCrops()
    {
        _crops = FaceCropFile.Load();
        _faceCg = -1;
        _face.QueueRedraw();
    }

    /// <summary>
    /// Covers the screen whose script pixel (0, 0) is at <paramref name="origin"/> (in its parent's
    /// coordinates), <paramref name="pixelsPerScript"/> screen pixels to a script pixel.
    /// </summary>
    public void Place(Vector2 origin, float pixelsPerScript)
    {
        Position = Vector2.Zero;
        Size = GetParentAreaSize();
        float k = pixelsPerScript / 2;
        var area = new Vector2(1280, 800) * k;
        _table.Place(new Rect2(origin + TableArea.Position * k, TableArea.Size * k), pixelsPerScript);
        if (origin == _origin && Math.Abs(k - _k) < 0.0001f && _backdrop.Size == area)
            return;
        _origin = origin;
        _k = k;
        foreach (var layer in new[] { _backdrop, _face })
        {
            layer.Position = origin;
            layer.Size = area;
            layer.QueueRedraw();
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
        {
            _result = null;
            _face.QueueRedraw();
        }
        var (year, month, week) = StatusStrip.DateOf(state.Week.Value);
        _date = $"LP {year} · Month {month} · Week {week}";
        _ready = _table.ReadyCount;
        var panel = _table.Chosen == 0 ? null : SubordinatesScreen.Panel(state, _table.Chosen);
        string key = panel is null ? "" : $"{panel}|{string.Join(",", panel.Steps)}|{Busy}|{_date}|{_ready}";
        if (key == _panelKey)
            return;
        _panelKey = key;
        _panel = panel;
        if (panel is not null && panel.PortraitCg != _portraitCg)
        {
            _portraitCg = panel.PortraitCg;
            _portrait = PicturePanel(panel.PortraitCg);
        }
        _face.QueueRedraw();
    }

    /// <summary>What an order did, on the plate beside Talk for a few seconds.</summary>
    public void ShowResult(SubordinateResult result)
    {
        _result = result;
        _resultLeft = ResultSeconds;
        _face.QueueRedraw();
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

    /// <summary>An order's button as the mouse presses it (unattended play goes through here).</summary>
    public void Press(SubordinateOrderKind kind, int unit, int step = 0)
    {
        _table.Chosen = unit;
        OrderPressed?.Invoke(kind, unit, step);
    }

    // ---------------------------------------------------------------- the buttons

    private Rect2 StepRect(int step)
    {
        int across = Math.Max(4, _panel?.Steps.Count ?? 0);    // a step as wide as a quarter of the row; more than four share it
        float w = (ReplenishArea.Size.X - StepGap * (across - 1)) / across;
        return new Rect2(ReplenishArea.Position.X + step * (w + StepGap), StepsTop, w, StepsHeight);
    }

    private Rect2? RectOf(int button) => button switch
    {
        ReplenishButton => ReplenishArea,
        TalkButton => TalkArea,
        EndButton => EndArea,
        >= StepButton when _panel is { AddOffered: true } p && button - StepButton < p.Steps.Count => StepRect(button - StepButton),
        _ => null,
    };

    private IEnumerable<int> Buttons()
    {
        yield return ReplenishButton;
        if (_panel is { AddOffered: true } p)
            for (int i = 0; i < p.Steps.Count; i++)
                yield return StepButton + i;
        yield return TalkButton;
        yield return EndButton;
    }

    private bool Enabled(int button) => !Busy && _panel is { } p && button switch
    {
        ReplenishButton => p.Replenish.Enabled,
        TalkButton => p.Talk.Enabled,
        EndButton => true,
        _ => button - StepButton < p.Steps.Count && p.Steps[button - StepButton].Enabled,
    };

    private int ButtonAt(Vector2 local)
    {
        var at = (local - _origin) / _k;
        foreach (int button in Buttons())
            if (RectOf(button) is { } rect && rect.HasPoint(at))
                return button;
        return NoButton;
    }

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                Hover(ButtonAt(motion.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                if (!Busy)
                    EndPressed?.Invoke();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                int button = ButtonAt(click.Position);
                if (click.Pressed)
                    _pressed = Enabled(button) ? button : NoButton;
                else
                {
                    if (button == _pressed && Enabled(button))
                        Click(button);
                    _pressed = NoButton;
                }
                _face.QueueRedraw();
                AcceptEvent();
                break;
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit)
            Hover(NoButton);
    }

    private void Hover(int button)
    {
        if (button == _hovered)
            return;
        _hovered = button;
        _face.QueueRedraw();
    }

    private void Click(int button)
    {
        if (button == EndButton)
            EndPressed?.Invoke();
        else if (_table.Chosen != 0)
        {
            var (kind, step) = button switch
            {
                ReplenishButton => (SubordinateOrderKind.Replenish, 0),
                TalkButton => (SubordinateOrderKind.Talk, 0),
                _ => (SubordinateOrderKind.AddTroops, button - StepButton),
            };
            OrderPressed?.Invoke(kind, _table.Chosen, step);
        }
    }

    /// <summary>A greyed step's or Talk's reason, as a tooltip (Replenish shows its own on the button).</summary>
    public override string _GetTooltip(Vector2 atPosition)
    {
        if (_panel is not { } p || Busy)
            return "";
        int button = ButtonAt(atPosition);
        return button switch
        {
            TalkButton when !p.Talk.Enabled => p.Talk.Note,
            >= StepButton when button - StepButton < p.Steps.Count && !p.Steps[button - StepButton].Enabled => p.Steps[button - StepButton].Note,
            _ => "",
        };
    }

    // ---------------------------------------------------------------- drawing

    private float Line(float width) => Lacquer.Line(width, _k);

    /// <summary>The backdrop: dark lacquer under a faint gold wave pattern (seigaiha), darkened towards the edges.</summary>
    private void DrawBackdrop(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        var lacquer = Color.Color8(14, 10, 8);
        c.DrawRect(new Rect2(0, 0, 1280, 800), lacquer);
        const float R = 34;
        var wave = Lacquer.Gold with { A = 0.05f };
        for (int row = -1; row * R / 2 < 800 + R; row++)
        {
            float y = row * R / 2, off = row % 2 == 0 ? 0 : R;
            for (float x = -2 * R + off; x < 1280 + 2 * R; x += 2 * R)
            {
                var at = new Vector2(x, y);
                c.DrawCircle(at, R, lacquer);
                foreach (float r in (float[])[R, R * 0.72f, R * 0.44f])
                    c.DrawArc(at, r, 0, Mathf.Tau, 40, wave, Line(1), true);
            }
        }
        const float Inset = 220;
        var edge = new Color(0, 0, 0, 0.7f);
        var clear = new Color(0, 0, 0, 0);
        Vector2[] outer = [new(0, 0), new(1280, 0), new(1280, 800), new(0, 800)];
        Vector2[] inner = [new(Inset, Inset * 0.62f), new(1280 - Inset, Inset * 0.62f), new(1280 - Inset, 800 - Inset * 0.62f), new(Inset, 800 - Inset * 0.62f)];
        for (int i = 0; i < 4; i++)
        {
            int j = (i + 1) % 4;
            c.DrawPolygon([outer[i], outer[j], inner[j], inner[i]], [edge, edge, clear, clear]);
        }
    }

    private void DrawScreen(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        DrawTablePanel(c);
        DrawPicture(c);
        DrawOrders(c);
    }

    /// <summary>The table's panel: the title band (the screen's name, the units ready, the date) over the ledger.</summary>
    private void DrawTablePanel(Control c)
    {
        Lacquer.DrawPanel(c, TablePanel, _k);
        var band = new Rect2(TablePanel.Position.X + 22, TablePanel.Position.Y + 8, TablePanel.Size.X - 44, 34);
        const string title = "Subordinates";
        c.DrawString(_brush, new Vector2(band.Position.X, Lacquer.Baseline(band, _brush, 24)), title, fontSize: 24, modulate: Lacquer.Name);
        float tw = _brush.GetStringSize(title, fontSize: 24).X;
        c.DrawString(_cinzelLight, new Vector2(band.Position.X + tw + 16, Lacquer.Baseline(band, _cinzelLight, 13) + 1),
            _ready == 1 ? "1 unit ready" : $"{_ready} units ready", fontSize: 13, modulate: Lacquer.Subtitle);
        float dw = _cinzel.GetStringSize(_date, fontSize: 15).X;
        c.DrawString(_cinzel, new Vector2(band.End.X - dw, Lacquer.Baseline(band, _cinzel, 15) + 1), _date, fontSize: 15, modulate: Lacquer.Name);
        Lacquer.DrawDivider(c, TablePanel.Position.X + 16, TablePanel.End.X - 16, TablePanel.Position.Y + 44, _k);
    }

    /// <summary>The picture panel with a shadow under it (its gold frame is the original's).</summary>
    private void DrawPicture(Control c)
    {
        var p = PictureArea;
        for (int i = 10; i >= 1; i--)
            c.DrawRect(new Rect2(p.Position.X - i + 3, p.Position.Y - i + 8, p.Size.X + 2 * i, p.Size.Y + 2 * i), new Color(0, 0, 0, 0.06f));
        if (_portrait is not null)
            c.DrawTextureRect(_portrait, p, false);
        else
            c.DrawRect(p, Colors.Black);
    }

    /// <summary>The orders' panel: its header with the budget, Replenish, Add New Troops' steps, Talk, the plate, End.</summary>
    private void DrawOrders(Control c)
    {
        Lacquer.DrawPanel(c, OrdersPanel, _k);
        if (_panel is not { } panel)
            return;
        var head = new Rect2(OrdersPanel.Position.X + 18, OrdersPanel.Position.Y + 10, OrdersPanel.Size.X - 36, 30);
        c.DrawString(_brush, new Vector2(head.Position.X, Lacquer.Baseline(head, _brush, 18)), "Orders", fontSize: 18, modulate: Lacquer.Name);
        float x = head.End.X;
        x = RightString(c, _cinzelLight, "K GOLD", 11, Lacquer.Subtitle, x, head) - 6;
        x = RightString(c, _cinzel, (panel.Gold * 100).ToString("N0", CultureInfo.InvariantCulture), 20, Lacquer.Name, x, head) - 10;
        RightString(c, _cinzelLight, "Budget", 12, Lacquer.Label, x, head);
        Lacquer.DrawDivider(c, OrdersPanel.Position.X + 14, OrdersPanel.End.X - 14, OrdersPanel.Position.Y + 46, _k);

        var r = panel.Replenish;
        if (Enabled(ReplenishButton))
            DrawPrimary(c, ReplenishArea, "Replenish", $"+{r.Men} troops  ·  {r.Price * 100}K", ReplenishButton);
        else
            DrawPlain(c, ReplenishArea, "Replenish", r.Enabled ? $"+{r.Men} troops  ·  {r.Price * 100}K" : r.Note, ReplenishButton, _brush, 22, 14);

        if (panel.AddOffered)
        {
            bool any = panel.Steps.Any(s => s.Enabled) && !Busy;
            const string label = "Add New Troops";
            float lx = ReplenishArea.Position.X + 2;
            c.DrawString(_cinzel, new Vector2(lx, AddLabelY + 5), label, fontSize: 14, modulate: any ? Lacquer.Label : Lacquer.OffText);
            float lw = _cinzel.GetStringSize(label, fontSize: 14).X;
            c.DrawLine(new Vector2(lx + lw + 10, AddLabelY), new Vector2(ReplenishArea.End.X - 2, AddLabelY), Lacquer.DimGold, Line(1));
            for (int i = 0; i < panel.Steps.Count; i++)
            {
                var s = panel.Steps[i];
                DrawPlain(c, StepRect(i), $"+{s.Men}", $"{s.Price * 100}K", StepButton + i, _brush, 17, 12);
            }
        }
        c.DrawLine(new Vector2(ReplenishArea.Position.X + 2, RuleY), new Vector2(ReplenishArea.End.X - 2, RuleY), Lacquer.DimGold, Line(1));

        DrawTalk(c, panel);
        if (_result is { } result)
            DrawResult(c, result);
        DrawPlain(c, EndArea, "End", null, EndButton, _cinzel, 17, 0);
    }

    /// <summary>Draws <paramref name="text"/> ending at <paramref name="right"/>, centred in <paramref name="band"/>; returns where it starts.</summary>
    private static float RightString(Control c, Font font, string text, int size, Color colour, float right, Rect2 band)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(right - w, Lacquer.Baseline(band, font, size)), text, fontSize: size, modulate: colour);
        return right - w;
    }

    private static void CentredString(Control c, Font font, string text, int size, Color colour, float centre, float baseline)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(centre - w / 2, baseline), text, fontSize: size, modulate: colour);
    }

    /// <summary>A title over a note, centred in <paramref name="rect"/> (the note may be null).</summary>
    private static void DrawLabels(Control c, Rect2 rect, string title, Font titleFont, int titleSize, Color titleColour,
        string? note, Font noteFont, int noteSize, Color noteColour)
    {
        float cx = rect.GetCenter().X;
        if (string.IsNullOrEmpty(note))
        {
            CentredString(c, titleFont, title, titleSize, titleColour, cx, Lacquer.Baseline(rect, titleFont, titleSize));
            return;
        }
        var top = new Rect2(rect.Position, new Vector2(rect.Size.X, rect.Size.Y * 0.62f));
        var bottom = new Rect2(rect.Position.X, rect.Position.Y + rect.Size.Y * 0.56f, rect.Size.X, rect.Size.Y * 0.38f);
        CentredString(c, titleFont, title, titleSize, titleColour, cx, Lacquer.Baseline(top, titleFont, titleSize));
        CentredString(c, noteFont, note, noteSize, noteColour, cx, Lacquer.Baseline(bottom, noteFont, noteSize));
    }

    /// <summary>The main order: red lacquer framed in gold, as Turn END on the command panel.</summary>
    private void DrawPrimary(Control c, Rect2 rect, string title, string note, int button)
    {
        bool hover = button == _hovered, down = hover && button == _pressed;
        Color top = down ? Color.Color8(96, 22, 16) : hover ? Color.Color8(150, 40, 30) : Color.Color8(128, 32, 24);
        Color bottom = down ? Color.Color8(70, 14, 10) : hover ? Color.Color8(96, 20, 14) : Color.Color8(80, 16, 12);
        c.DrawPolygon([rect.Position, new(rect.End.X, rect.Position.Y), rect.End, new(rect.Position.X, rect.End.Y)], [top, top, bottom, bottom]);
        c.DrawRect(rect, hover ? Lacquer.HoverLine : Lacquer.Gold, false, Line(2));
        c.DrawRect(rect.Grow(-4), Color.Color8(150, 96, 52), false, Line(1));
        DrawLabels(c, rect, title, _brush, 22, Color.Color8(250, 226, 160), note, _gothic, 15, Color.Color8(236, 206, 160));
    }

    /// <summary>A plain order: a dark row box, lit when hovered, sunk when pressed, grey when it cannot be clicked.</summary>
    private void DrawPlain(Control c, Rect2 rect, string title, string? note, int button, Font titleFont, int titleSize, int noteSize)
    {
        bool on = Enabled(button), hover = on && button == _hovered, down = hover && button == _pressed;
        if (!on)
        {
            c.DrawRect(rect, Lacquer.OffButton);
            c.DrawRect(rect, Lacquer.OffLine, false, Line(1));
        }
        else
        {
            c.DrawRect(rect, down ? Lacquer.Row : hover ? Lacquer.RowHover : Lacquer.Row);
            c.DrawRect(rect, hover ? Lacquer.HoverLine : Lacquer.DimGold, false, Line(hover ? 2 : 1));
        }
        DrawLabels(c, rect, title, titleFont, titleSize, !on ? Lacquer.OffText : hover ? Lacquer.Name : Lacquer.Text,
            note, _gothic, noteSize, on ? Lacquer.Quiet : Lacquer.OffText);
    }

    /// <summary>Talk: the unit's face in a gold frame, "Talk" and whom with (or why it is greyed).</summary>
    private void DrawTalk(Control c, SubordinatePanel panel)
    {
        var rect = TalkArea;
        bool on = Enabled(TalkButton), hover = on && _hovered == TalkButton;
        if (!on)
        {
            c.DrawRect(rect, Lacquer.OffButton);
            c.DrawRect(rect, Lacquer.OffLine, false, Line(1));
        }
        else
        {
            c.DrawRect(rect, hover ? Lacquer.RowHover : Lacquer.Row);
            c.DrawRect(rect, hover ? Lacquer.HoverLine : Lacquer.DimGold, false, Line(hover ? 2 : 1));
        }
        float size = rect.Size.Y - 12;
        var faceRect = new Rect2(rect.Position + new Vector2(6, 6), new Vector2(size, size));
        if (FaceTexture(panel.FaceCg, size) is { } face)
            c.DrawTextureRect(face, faceRect, false, on ? Colors.White : new Color(0.45f, 0.42f, 0.4f));
        c.DrawRect(faceRect, on ? Lacquer.Gold : Lacquer.OffLine, false, Line(1));
        var text = new Rect2(faceRect.End.X + 6, rect.Position.Y, rect.End.X - faceRect.End.X - 12, rect.Size.Y);
        string note = on || string.IsNullOrEmpty(panel.Talk.Note) ? $"with {panel.Name}" : panel.Talk.Note;
        DrawLabels(c, text, "Talk", _brush, 20, !on ? Lacquer.OffText : hover ? Lacquer.Name : Lacquer.Text,
            note, _cinzelLight, 11, on ? Lacquer.Subtitle : Lacquer.OffText);
    }

    /// <summary>The unit's face cut to its square, made again only when the face or the window's size changes.</summary>
    private ImageTexture? FaceTexture(int faceCg, float size)
    {
        if (faceCg == 0)
            return null;
        int pixels = Math.Max(16, (int)Math.Round(size * _k));
        if (faceCg != _faceCg || pixels != _faceSize)
        {
            _faceCg = faceCg;
            _faceSize = pixels;
            _faceTexture = _faces.SquareTexture(faceCg, _faces.CropOf(faceCg, _crops), pixels);
        }
        return _faceTexture;
    }

    /// <summary>The plate: what the order did and what it cost, label and figure on each line.</summary>
    private void DrawResult(Control c, SubordinateResult result)
    {
        var rect = ResultArea;
        c.DrawRect(rect, Color.Color8(18, 26, 14, 240));
        c.DrawRect(rect, Color.Color8(120, 160, 84), false, Line(1));
        c.DrawRect(rect.Grow(-3), Color.Color8(60, 84, 44), false, Line(1));
        var lines = new[] { (result.Line, Lacquer.Plus, Lacquer.Text), (result.Budget, Lacquer.Label, Lacquer.Minus) };
        for (int i = 0; i < lines.Length; i++)
        {
            var (text, labelColour, valueColour) = lines[i];
            var (label, value) = Split(text);
            var row = new Rect2(rect.Position.X + 14, rect.Position.Y + 8 + i * 34, rect.Size.X - 28, 34);
            c.DrawString(_cinzel, new Vector2(row.Position.X, Lacquer.Baseline(row, _cinzel, 13)), label, fontSize: 13, modulate: labelColour);
            RightString(c, _gothic, value, 16, valueColour, row.End.X, row);
        }
    }

    /// <summary>"Replenished: +1191 → 1400" as its label and figure; "Budget −2300K" likewise at its first space.</summary>
    private static (string Label, string Value) Split(string line)
    {
        int at = line.IndexOf(": ", StringComparison.Ordinal);
        if (at >= 0)
            return (line[..at], line[(at + 2)..]);
        at = line.IndexOf(' ');
        return at >= 0 ? (line[..at], line[(at + 1)..]) : ("", line);
    }

    /// <summary>A layer of the screen drawn by its view (the backdrop under the table, the panels over it).</summary>
    private sealed partial class Layer : Control
    {
        private readonly Action<Control> _paint;

        public Layer(Action<Control> paint)
        {
            _paint = paint;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Draw() => _paint(this);
    }
}
