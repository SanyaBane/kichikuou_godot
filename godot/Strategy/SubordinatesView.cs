using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Subordinates screen (docs/ui/subordinates.md, the owner's layout F1 in the look of mock-up G3): over the
/// whole screen while <c>Sﾌﾞｶ</c> is open and while one of its orders runs — a dark lacquer backdrop with a faint
/// gold wave pattern; the table of the units down the left as a paper ledger in a lacquer panel, its columns a tab's at a
/// time (<see cref="SubordinatesScreen.Columns"/>, <see cref="DeploymentTableView"/> as a ledger, <see cref="LedgerTabsBar"/>;
/// docs/ui/unit-ledger.md, the owner's B4-B5); the chosen unit's status CG at the top right, in the original screen's
/// gold frame; the orders' panel under it (the
/// budget, Replenish in red lacquer, Add New Troops' steps, Talk with the unit's face, End). A troop order asks first
/// in a callout at its button (the owner's C3, docs/ui/subordinates.md, §6: the Treasure screen's
/// <see cref="TreasureCallout"/>, Yes / No); after it a plate in the middle of the screen says what it did (R4/R5).
/// The talks' marks (owner's decision 30, docs/ui/talk-marks.md §6, S): a speech bubble left of No. in each row of a unit
/// that can talk, and Talk's NEW or ??? pill at its top right (none when read); none of them while the week's talk is
/// spent (<see cref="SubordinatesScreen.TalkMarks"/>).
/// Drawn by hand in the 1280×800 screen's pixels, as our map panels (<see cref="Lacquer"/>). It decides nothing: the
/// presses go to <see cref="GameScreen"/>, which answers the script through a <see cref="SubordinateOrder"/>. On the phone held
/// landscape it is laid out over the whole screen (<c>SubordinatesView.Phone.cs</c>, LU1).
/// </summary>
internal sealed partial class SubordinatesView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    // In the 1280×800 screen's pixels (the script's 640×400 at 2×), after the mock-ups (tools/ui/mock_subordinates.py;
    // tools/ui/mock_unit_ledger.py, round b: the ledger down the left, the picture and the orders on the right).
    private static readonly Rect2 TablePanel = new(16, 16, 692, 768);
    private const float TabsTop = 66;                                     // the ledger's tabs (docs/ui/unit-ledger.md)
    private static Rect2 TableArea => new(30, TabsTop + LedgerTabsBar.RowHeight, 664, 770 - TabsTop - LedgerTabsBar.RowHeight);
    private static readonly Rect2 PictureArea = new(716, 16, 548, 342);   // the picture panel's 314×196 as wide as the orders'
    private static readonly Rect2 OrdersPanel = new(716, 368, 548, 416);
    private static readonly Rect2 ReplenishArea = new(728, 424, 524, 60);
    private const float AddLabelY = 500, StepsTop = 514, StepsHeight = 56, StepGap = 6, RuleY = 586;
    private static readonly Rect2 TalkArea = new(728, 598, 220, 84);
    private static readonly Rect2 EndArea = new(1112, 718, 140, 52);
    private static readonly Color AskedLine = Color.Color8(190, 60, 40);   // the button the callout asks about

    // The buttons, as the view tells them apart: Replenish, Talk, End, then Add More's steps from StepButton.
    /// <summary>Every order's word on the PC and the tablet — Replenish, the steps, Talk, End: Cinzel 700 at one size (docs/ui/style.md, "Buttons side by side").</summary>
    private const int ButtonText = 17;
    private const int NoButton = -1, ReplenishButton = 0, TalkButton = 1, EndButton = 2, StepButton = 10;

    private readonly FacePictures _faces;
    private readonly Layer _backdrop, _face;
    private readonly OrderResultPlate _plate;
    private readonly DeploymentTableView _table;
    private readonly TreasureCallout _callout;
    private readonly UnitPictureView _picture;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700), _cinzelLight = Lacquer.Cinzel(600);
    private FontFile _gothic;
    private FaceCrops _crops = new();
    private Vector2 _origin;
    private float _k = 1;                               // window pixels to a screen pixel
    private SubordinatePanel? _panel;
    private string _panelKey = "", _date = "";
    private int _ready;
    private int _portraitCg = -1;
    private ImageTexture? _portrait;
    private readonly FaceCache _talkFace = new();
    private GameState? _state;
    private IReadOnlyDictionary<int, SceneMark> _talkMarks = new Dictionary<int, SceneMark>();
    private int _hovered = NoButton, _pressed = NoButton, _asked = NoButton;

    /// <summary>The ledger's tabs over its paper (docs/ui/unit-ledger.md).</summary>
    private readonly LedgerTabsBar _tabs = new("subordinates", new Rect2(30, TabsTop, 664, LedgerTabsBar.RowHeight));
    private string _tabsKey = "";

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
        _table = new DeploymentTableView(font, SubordinatesScreen.Columns, chooser: true, textSize: 8.3f) { Visible = true };
        _table.Tab = _tabs.Tab;
        _table.Faces = (faceCg, pixels) =>                      // the phone's Name cells
            faceCg == 0 ? null : _faces.SquareTexture(faceCg, _faces.CropOf(faceCg, _crops), Math.Max(16, pixels));
        _table.RowPressed += unit => _table.Chosen = unit;
        _table.RightPressed += () => EndPressed?.Invoke();
        AddChild(_table);
        _picture = new UnitPictureView();                       // the phone's long press on a row: over the ledger
        _table.RowHeld += unit =>
        {
            if (_state is { } state)
                _picture.Show(_faces.PicturePanel(state.Units[unit].PortraitCg.Value), _table.Caption(unit));
        };
        AddChild(_picture);
        _plate = new OrderResultPlate(font, faces);              // the plate, over the ledger too
        AddChild(_plate);
        _callout = new TreasureCallout(font);
        AddChild(_callout);
    }

    /// <summary>The unit chosen in the table.</summary>
    public int Chosen => _table.Chosen;

    /// <summary>The table (unattended play's touch exercise).</summary>
    public DeploymentTableView Table => _table;

    /// <summary>Whether the panel's buttons take presses (not while an order runs).</summary>
    public bool Busy { get; set; }

    /// <summary>The ledger's tab shown.</summary>
    public LedgerTab Tab => _tabs.Tab;

    /// <summary><paramref name="tab"/>'s centre in global coordinates (unattended play's touch exercise).</summary>
    public Vector2 TabCentre(LedgerTab tab) =>
        GetGlobalTransform() * (_origin + _tabs.Tabs().First(t => t.Tab == tab).Rect.GetCenter() * _k);

    /// <summary>Shows the ledger's <paramref name="tab"/>; <paramref name="remember"/> keeps it for later openings.</summary>
    public void ShowTab(LedgerTab tab, bool remember = true)
    {
        _tabs.Show(tab, remember);
        _table.Tab = tab;
        _face.QueueRedraw();
    }

    /// <summary>Option → Display: the screen's text in the chosen font, as the deployment table's.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        _table.SetFont(font);
        _callout.SetFont(font);
        _plate.SetFont(font);
        _face.QueueRedraw();
    }

    /// <summary>The squares of the faces: the built-in ones and the owner's edits from Debug → Portraits (read as the screen opens).</summary>
    public void LoadCrops()
    {
        _crops = FaceCropFile.Load();
        _talkFace.Clear();
        _plate.LoadCrops();
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
        var frame = new Rect2(origin, new Vector2(1280, 800) * k);
        _plate.Place(frame);                                    // the plate in the frame's middle on every device
        var phone = Device.Held(this);
        if (phone is { IsPortrait: true })
        {
            var ledger = PhoneLedgerPortrait.Ledger(phone, deployment: false);  // PU1: the ledger over the orders
            _table.PlacePortrait(new Rect2(ledger.X, ledger.Y, ledger.W, ledger.H), phone);
            _callout.PlacePhone(phone);
            (origin, k) = (Vector2.Zero, phone.Scale);
        }
        else if (phone is not null)
        {
            var ledger = PhoneLedger.Ledger(phone, deployment: false);     // LU1: the ledger fills the frame
            _table.PlacePhone(new Rect2(ledger.X, ledger.Y, ledger.W, ledger.H), phone);
            _callout.PlacePhone(phone);
            (origin, k) = (Vector2.Zero, phone.Scale);           // drawn in the frame's layout pixels over the whole screen
        }
        else
        {
            _table.Place(new Rect2(origin + TableArea.Position * k, TableArea.Size * k), pixelsPerScript);
            _callout.Place(frame);
        }
        var area = phone is null ? frame.Size : Size;
        if (origin == _origin && Math.Abs(k - _k) < 0.0001f && _backdrop.Size == area && phone == _phone)
            return;
        _origin = origin;
        _k = k;
        _phone = phone;
        _tabs.Phone = phone is null ? null : DeploymentPanels.PhoneTabs(phone, deployment: false);
        foreach (var layer in new[] { _backdrop, _face })
        {
            layer.Position = origin;
            layer.Size = area;
            layer.QueueRedraw();
        }
        if (_callout.MenuOpen && RectOf(_asked) is { } asked)   // a turn: the question at its button's new place
            ShowQuestion(asked);
    }

    /// <summary>
    /// The table and the panel from <paramref name="state"/>, the talks' marks from <paramref name="talks"/>
    /// (<see cref="PickerMarks.Talks"/>, shown as <see cref="SubordinatesScreen.TalkMarks"/> has them); the chosen row kept
    /// while it is listed, else the first unit that can act (<see cref="SubordinatesScreen.FirstChoice"/>).
    /// </summary>
    public void Refresh(GameState state, double delta, IReadOnlyDictionary<int, SceneMark>? talks = null)
    {
        _state = state;
        _talkMarks = SubordinatesScreen.TalkMarks(state, talks);
        _table.Refresh(state);
        _table.TalkMarks = _talkMarks;
        _table.OpenTop = _tabs.OpenSpan(_k);
        string tabsKey = $"{_table.Tab}|{_table.SortShown}|{_table.SortHeader}|{_table.SortMark}";
        if (tabsKey != _tabsKey)
        {
            _tabsKey = tabsKey;
            _face.QueueRedraw();
        }
        if (_table.Chosen == 0 || !_table.Lists(_table.Chosen))
            _table.Chosen = SubordinatesScreen.FirstChoice(state);
        var (year, month, week) = StatusStrip.DateOf(state.Week.Value);
        _date = $"LP {year} · Month {month} · Week {week}";
        _ready = _table.ReadyCount;
        var panel = _table.Chosen == 0 ? null : SubordinatesScreen.Panel(state, _table.Chosen, _talkMarks);
        string key = panel is null ? "" : $"{panel}|{string.Join(",", panel.Steps)}|{Busy}|{_date}|{_ready}|{Figures.Separator}";
        if (key == _panelKey)
            return;
        _panelKey = key;
        _panel = panel;
        if (panel is not null && panel.PortraitCg != _portraitCg)
        {
            _portraitCg = panel.PortraitCg;
            _portrait = _faces.PicturePanel(panel.PortraitCg);
        }
        _face.QueueRedraw();
    }

    /// <summary>
    /// What an order did, on the plate in the middle till a click anywhere closes it (null takes it away); the screen
    /// takes no other click while it shows.
    /// </summary>
    public void ShowResult(SubordinateResult? result)
    {
        _plate.Show(result);
    }

    /// <summary>
    /// An order's button as the mouse presses it (unattended play goes through here): a troop order asks first
    /// (<see cref="Answer"/> answers it as a click would), Talk goes at once.
    /// </summary>
    public void Press(SubordinateOrderKind kind, int unit, int step = 0)
    {
        _table.Chosen = unit;
        if (_state is not null)
            _panel = SubordinatesScreen.Panel(_state, unit, _talkMarks);
        if (kind == SubordinateOrderKind.Talk)
            OrderPressed?.Invoke(kind, unit, step);
        else
            Ask(kind, unit, step, kind == SubordinateOrderKind.Replenish ? ReplenishButton : StepButton + step);
    }

    /// <summary>Whether the callout asks about a troop order.</summary>
    public bool Asking => _callout.MenuOpen;

    /// <summary>Answers the callout as a click on Yes or No would (unattended play).</summary>
    public void Answer(bool yes) => _callout.Choose(yes ? 1 : 2);

    /// <summary>
    /// The question before a troop order (<see cref="SubordinatesScreen.Question"/>) in a callout at its button, the
    /// button marked; Yes gives the order, No or a right click leaves it. Nothing reaches the script before Yes.
    /// </summary>
    private async void Ask(SubordinateOrderKind kind, int unit, int step, int button)
    {
        if (_panel is null || _callout.MenuOpen || RectOf(button) is not { } rect)
            return;
        _asked = button;
        ShowQuestion(rect);
        _callout.OpenMenu(["Yes", "No"]);
        _face.QueueRedraw();
        int answer;
        try
        {
            answer = await _callout.ChooseAsync();
        }
        catch (OperationCanceledException)
        {
            answer = 0;                                         // the screen closed under the question
        }
        _callout.Hide();
        _asked = NoButton;
        _face.QueueRedraw();
        if (answer == 1 && IsVisibleInTree() && !Busy)
            OrderPressed?.Invoke(kind, unit, step);
    }

    /// <summary>The question at the asked button's <paramref name="rect"/>, as the device lays it.</summary>
    private void ShowQuestion(Rect2 rect)
    {
        if (_phone is { } phone)
            ShowPhoneQuestion(phone, rect, _asked == ReplenishButton);
        else
            _callout.Show(SubordinatesScreen.Question, rect, waiting: false);
    }

    // ---------------------------------------------------------------- the buttons

    private Rect2 StepRect(int step)
    {
        if (_phone is { } phone)
            return Box((phone.IsPortrait ? PhoneLedgerPortrait.Steps(phone, _panel?.Steps.Count ?? 0) : PhoneLedger.Steps(phone, _panel?.Steps.Count ?? 0))[step]);
        int across = Math.Max(4, _panel?.Steps.Count ?? 0);    // a step as wide as a quarter of the row; more than four share it
        float w = (ReplenishArea.Size.X - StepGap * (across - 1)) / across;
        return new Rect2(ReplenishArea.Position.X + step * (w + StepGap), StepsTop, w, StepsHeight);
    }

    private Rect2? RectOf(int button) => button switch
    {
        ReplenishButton => _phone is { } f ? Box(f.IsPortrait ? PhoneLedgerPortrait.Replenish(f) : PhoneLedger.Replenish(f)) : ReplenishArea,
        TalkButton => _phone is { } f ? Box(f.IsPortrait ? PhoneLedgerPortrait.Talk(f) : PhoneLedger.Talk(f)) : TalkArea,
        EndButton => _phone is { } f ? Box(f.IsPortrait ? PhoneLedgerPortrait.End(f) : PhoneLedger.End(f)) : EndArea,
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
        >= StepButton => button - StepButton < p.Steps.Count && p.Steps[button - StepButton].Enabled,
        _ => false,                                                     // no button (a tab, the title band)
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
                if (_tabs.Hover((motion.Position - _origin) / _k))
                    _face.QueueRedraw();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                if (!Busy)
                    EndPressed?.Invoke();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                if (_tabs.Press((click.Position - _origin) / _k, click.Pressed) is { } tab)
                    ShowTab(tab);
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
        else if (what == NotificationVisibilityChanged && !Visible && _callout is not null)   // not the constructor's own hiding
        {
            _callout.Hide();                                    // a question left open is a No
            ShowResult(null);
            _picture.Hide();
        }
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
        else if (button == TalkButton && _table.Chosen != 0)
            OrderPressed?.Invoke(SubordinateOrderKind.Talk, _table.Chosen, 0);
        else if (_table.Chosen != 0)
        {
            var (kind, step) = button == ReplenishButton
                ? (SubordinateOrderKind.Replenish, 0)
                : (SubordinateOrderKind.AddTroops, button - StepButton);
            Ask(kind, _table.Chosen, step, button);
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

    private float Frame(float width) => Lacquer.Frame(width, _k);

    /// <summary>The backdrop: dark lacquer under a faint gold wave pattern (seigaiha), darkened towards the edges.</summary>
    private void DrawBackdrop(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        Ui.DrawWaveBackdrop(c, _k, _phone is null ? null : c.Size / _k);
    }

    private void DrawScreen(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (_phone is { } phone)
        {
            DrawPhone(c, phone);
            return;
        }
        DrawTablePanel(c);
        DrawPicture(c);
        DrawOrders(c);
    }

    /// <summary>The table's panel: the title band (the screen's name, the units ready, the date) over the ledger.</summary>
    private void DrawTablePanel(Control c)
    {
        Ui.DrawPanel(c, TablePanel, _k);
        var band = new Rect2(TablePanel.Position.X + 22, TablePanel.Position.Y + 8, TablePanel.Size.X - 44, 34);
        const string title = "Subordinates";
        c.DrawString(_brush, new Vector2(band.Position.X, Lacquer.Baseline(band, _brush, 24)), title, fontSize: 24, modulate: Ui.Name);
        float tw = _brush.GetStringSize(title, fontSize: 24).X;
        c.DrawString(_cinzelLight, new Vector2(band.Position.X + tw + 16, Lacquer.Baseline(band, _cinzelLight, 13) + 1),
            _ready == 1 ? "1 unit ready" : $"{_ready} units ready", fontSize: 13, modulate: Ui.Subtitle);
        float dw = _cinzel.GetStringSize(_date, fontSize: 16).X;
        c.DrawString(_cinzel, new Vector2(band.End.X - dw, Lacquer.Baseline(band, _cinzel, 16) + 1), _date, fontSize: 16, modulate: Ui.Name);
        Ui.DrawDivider(c, TablePanel.Position.X + 16, TablePanel.End.X - 16, TablePanel.Position.Y + 44, _k);
        _tabs.Draw(c, _k, _table);
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
        Ui.DrawPanel(c, OrdersPanel, _k);
        if (_panel is not { } panel)
            return;
        var head = new Rect2(OrdersPanel.Position.X + 18, OrdersPanel.Position.Y + 10, OrdersPanel.Size.X - 36, 30);
        c.DrawString(_brush, new Vector2(head.Position.X, Lacquer.Baseline(head, _brush, 18)), "Orders", fontSize: 18, modulate: Ui.Name);
        float x = head.End.X;
        x = RightString(c, _cinzelLight, "GOLD", 11, Ui.Subtitle, x, head) - 6;
        x = RightString(c, _cinzel, Figures.GoldShort(panel.Gold * 100), 20, Ui.Name, x, head) - 10;
        RightString(c, _cinzelLight, "Budget", 12, Ui.Label, x, head);
        Ui.DrawDivider(c, OrdersPanel.Position.X + 14, OrdersPanel.End.X - 14, OrdersPanel.Position.Y + 46, _k);

        var r = panel.Replenish;
        if (Enabled(ReplenishButton))
            DrawPrimary(c, ReplenishArea, "Replenish", $"{Figures.Signed(r.Men)} troops  ·  {Figures.GoldShort(r.Price * 100)}", ReplenishButton, ButtonText, 14, _cinzel);
        else
            DrawPlain(c, ReplenishArea, "Replenish", r.Enabled ? $"{Figures.Signed(r.Men)} troops  ·  {Figures.GoldShort(r.Price * 100)}" : r.Note, ReplenishButton, _cinzel, ButtonText, 14);

        if (panel.AddOffered)
        {
            bool any = panel.Steps.Any(s => s.Enabled) && !Busy;
            const string label = "Add New Troops";
            float lx = ReplenishArea.Position.X + 2;
            c.DrawString(_cinzel, new Vector2(lx, AddLabelY + 5), label, fontSize: 14, modulate: any ? Ui.Label : Ui.OffText);
            float lw = _cinzel.GetStringSize(label, fontSize: 14).X;
            c.DrawLine(new Vector2(lx + lw + 10, AddLabelY), new Vector2(ReplenishArea.End.X - 2, AddLabelY), Ui.DimGold, Line(1));
            for (int i = 0; i < panel.Steps.Count; i++)
            {
                var s = panel.Steps[i];
                DrawPlain(c, StepRect(i), Figures.Signed(s.Men), Figures.GoldShort(s.Price * 100), StepButton + i, _cinzel, ButtonText, 12);
            }
        }
        c.DrawLine(new Vector2(ReplenishArea.Position.X + 2, RuleY), new Vector2(ReplenishArea.End.X - 2, RuleY), Ui.DimGold, Line(1));

        DrawTalk(c, panel);
        DrawPlain(c, EndArea, "End", null, EndButton, _cinzel, ButtonText, 0);
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
    private void DrawPrimary(Control c, Rect2 rect, string title, string note, int button, int titleSize = 22, int noteSize = 15, Font? titleFont = null)
    {
        bool hover = button == _hovered || button == _asked, down = button == _hovered && button == _pressed;
        Ui.DrawRedButton(c, rect, hover, down, _k);
        DrawLabels(c, rect, title, titleFont ?? _brush, titleSize, Color.Color8(250, 226, 160), note, _gothic, noteSize, Color.Color8(236, 206, 160));
    }

    /// <summary>A plain order: a dark row box, lit when hovered, sunk when pressed, grey when it cannot be clicked.</summary>
    private void DrawPlain(Control c, Rect2 rect, string title, string? note, int button, Font titleFont, int titleSize, int noteSize)
    {
        bool on = Enabled(button), hover = on && button == _hovered, down = hover && button == _pressed;
        if (!on)
        {
            c.DrawRect(rect, Ui.OffButton);
            c.DrawRect(rect, Ui.OffLine, false, Frame(1));
        }
        else
        {
            c.DrawRect(rect, down ? Ui.Row : hover ? Ui.RowHover : Ui.Row);
            c.DrawRect(rect, hover ? Ui.HoverLine : Ui.DimGold, false, Frame(hover ? 2 : 1));
        }
        DrawLabels(c, rect, title, titleFont, titleSize, !on ? Ui.OffText : hover ? Ui.Name : Ui.Text,
            note, _gothic, noteSize, on ? Ui.Quiet : Ui.OffText);
        if (button == _asked)
            c.DrawRect(rect, AskedLine, false, Line(2));
    }

    /// <summary>Talk: the unit's face in a gold frame, "Talk" and whom with (or why it is greyed); its talk's NEW or ??? pill at the top right.</summary>
    private void DrawTalk(Control c, SubordinatePanel panel)
    {
        var rect = TalkArea;
        bool on = Enabled(TalkButton), hover = on && _hovered == TalkButton;
        if (!on)
        {
            c.DrawRect(rect, Ui.OffButton);
            c.DrawRect(rect, Ui.OffLine, false, Frame(1));
        }
        else
        {
            c.DrawRect(rect, hover ? Ui.RowHover : Ui.Row);
            c.DrawRect(rect, hover ? Ui.HoverLine : Ui.DimGold, false, Frame(hover ? 2 : 1));
        }
        float size = rect.Size.Y - 12;
        var faceRect = new Rect2(rect.Position + new Vector2(6, 6), new Vector2(size, size));
        if (_talkFace.Get(_faces, _crops, panel.FaceCg, size * _k) is { } face)
            c.DrawTextureRect(face, faceRect, false, on ? Colors.White : new Color(0.45f, 0.42f, 0.4f));
        c.DrawRect(faceRect, on ? Ui.Gold : Ui.OffLine, false, Line(1));
        var text = new Rect2(faceRect.End.X + 6, rect.Position.Y, rect.End.X - faceRect.End.X - 12, rect.Size.Y);
        string note = on || string.IsNullOrEmpty(panel.Talk.Note) ? $"with {panel.Name}" : panel.Talk.Note;
        DrawLabels(c, text, "Talk", _cinzel, ButtonText, !on ? Ui.OffText : hover ? Ui.Name : Ui.Text,
            note, _cinzelLight, 11, on ? Ui.Subtitle : Ui.OffText);
        if (on && PickerMarks.Label(panel.TalkMark) is { } pill)
            Ui.DrawPill(c, _cinzel, rect.End.X - 6, rect.Position.Y + 12, pill,
                panel.TalkMark == SceneMark.New ? Ui.MarkNew : Ui.MarkMaybe, 10, 14);
    }

    /// <summary>A unit's face cut to its square, made again only when the face or its size in window pixels changes.</summary>
    private sealed class FaceCache
    {
        private int _cg = -1, _pixels;
        private ImageTexture? _texture;

        public void Clear() => _cg = -1;

        public ImageTexture? Get(FacePictures faces, FaceCrops crops, int faceCg, float windowSize)
        {
            if (faceCg == 0)
                return null;
            int pixels = Math.Max(16, (int)Math.Round(windowSize));
            if (faceCg != _cg || pixels != _pixels)
            {
                (_cg, _pixels) = (faceCg, pixels);
                _texture = faces.SquareTexture(faceCg, faces.CropOf(faceCg, crops), pixels);
            }
            return _texture;
        }
    }

    /// <summary>A layer of the screen drawn by its view (the backdrop under the table, the panels over it, the plate over all).</summary>
    private sealed partial class Layer : Control
    {
        private readonly Action<Control> _paint;

        public Layer(Action<Control> paint)
        {
            _paint = paint;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        /// <summary>The layer's input while its mouse filter stops the mouse (the plate's).</summary>
        public Action<InputEvent>? Input { get; init; }

        public override void _Draw() => _paint(this);

        public override void _GuiInput(InputEvent e)
        {
            if (Input is null)
                return;
            Input(e);
            AcceptEvent();
        }
    }
}
