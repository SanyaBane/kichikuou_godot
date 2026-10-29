using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Treasure screen (docs/ui/pickers.md, round V): <c>Sｱｲﾃﾑ</c>'s own screen in lacquer, as the harem's — the
/// wave backdrop; a title band (the screen's name, the date); at the left the picture panel of the entry under the
/// pointer (a unit's status CG, an item's CG with its description; the original's frame), the original's line under it
/// as figures and the first pick's prompt with Back; at the right the picker's 3×17 grid, each character or item at its
/// own number's cell and the other cells empty, its view's two tabs (the bar's Item/Character) and End. Drawn by hand
/// in the 1280×800 screen's pixels (<see cref="Lacquer"/>). It decides nothing: a cell's click, a tab, Back (or a right
/// press) and End go to <see cref="GameScreen"/>, which gives the picker its player's click (<see cref="TreasureRoster"/>);
/// the script still refuses, asks and equips, its windows and menus drawn over this screen.
/// In the touch mode (docs/ui/pickers.md, §6, the owner's round X) there is no hover: a tap on a cell only selects it — lit
/// as the PC's hover, its picture and line in the panel — and a red button at the info panel's right acts on it
/// (<see cref="TreasureButton"/>: Choose an item, Choose a character, Equip, Take off), greyed with its reason where the
/// picker would refuse the click. On the phone held landscape the screen takes the whole screen, its grid folded after its
/// ninth row (docs/ui/phone-build.md, step L7, LT2).
/// </summary>
internal sealed partial class TreasureView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    // In the 1280×800 screen's pixels, after the mock-up.
    private static readonly Rect2 TitlePanel = new(16, 16, 1248, 56);
    private static readonly Rect2 PictureArea = new(16, 88, 666, 416);
    private static readonly Rect2 InfoPanel = new(16, 516, 666, 268);
    private static readonly Rect2 GridPanel = new(690, 88, 574, 696);
    private static readonly Rect2 PromptFace = new(40, 650, 112, 112);
    private static readonly Rect2 BackArea = new(548, 730, 110, 34);
    private static readonly Rect2 EndArea = new(1106, 726, 140, 40);
    private static readonly Rect2 ActionArea = new(432, 541, 232, 70);    // the touch mode's button, under the picture's description
    private const float TabletW = 176, TabletH = 32, TabletStepX = 180, TabletStepY = 36, GridX = 708, GridY = 106;
    private const float TabX = 708, TabY = 726, TabW = 176, TabH = 40, TabStep = 182;
    private const int Columns = 3;

    private const int NoButton = -1, BackButton = -2, EndButton = -3, TabCharacters = -4, TabItems = -5, ActionButton = -6;   // a cell is its number

    private readonly FacePictures _faces;
    private readonly Layer _backdrop, _face;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700);
    private FontFile _gothic;
    private Vector2 _origin;
    private float _k = 1;
    private GameState? _state;
    private int _view;
    private IReadOnlyList<TreasureEntry> _entries = [];
    private TreasureDetails? _details;
    private TreasurePrompt? _prompt;
    private int _shown;                                 // the entry whose picture shows: the one hovered last (touch: the one selected)
    private int _selected;                              // touch: the entry a tap selected (0: none)
    private TreasureButton? _button;                    // touch: what the button does with it
    private string _key = "", _date = "";
    private ImageTexture? _picture, _faceTexture;
    private (int, int) _pictureKey = (-1, -1);
    private FaceCrops _crops = new();
    private int _faceCg = -1, _faceSize;
    private int _hovered = NoButton, _pressed = NoButton;

    /// <summary>A cell's click: the entry's number.</summary>
    public event Action<int>? PickPressed;

    /// <summary>The other view's tab: the bar's Item/Character.</summary>
    public event Action? TurnPressed;

    /// <summary>Back, or a right press on the screen: the original's right click.</summary>
    public event Action? BackPressed;

    /// <summary>End.</summary>
    public event Action? EndPressed;

    public TreasureView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        _backdrop = new Layer(c =>
        {
            c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
            Ui.DrawWaveBackdrop(c, _k, _phone is null ? null : c.Size / _k);      // the phone's: the whole screen
        }) { ClipContents = true };
        AddChild(_backdrop);
        _face = new Layer(DrawScreen);
        AddChild(_face);
    }

    /// <summary>Whether the screen takes presses (not while a click is on its way to the script).</summary>
    public bool Busy { get; set; }

    /// <summary>The cell a callout of the script's asks or refuses about, marked in vermilion (0: none).</summary>
    public int Asked
    {
        get => _asked;
        set
        {
            if (value == _asked)
                return;
            _asked = value;
            _face.QueueRedraw();
        }
    }

    private int _asked;

    /// <summary>Option → Display.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
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
    /// Covers the screen whose script pixel (0, 0) is at <paramref name="origin"/>, <paramref name="pixelsPerScript"/> screen
    /// pixels to a script pixel; on the phone, held either way, the whole screen in its frame's layout pixels (LT2, PT2).
    /// </summary>
    public void Place(Vector2 origin, float pixelsPerScript)
    {
        Position = Vector2.Zero;
        Size = GetParentAreaSize();
        var phone = Device.Held(this);
        float k = phone?.Scale ?? pixelsPerScript / 2;
        if (phone is not null)
            origin = Vector2.Zero;
        var area = phone is not null ? Size : new Vector2(1280, 800) * k;
        if (origin == _origin && Math.Abs(k - _k) < 0.0001f && _backdrop.Size == area && phone == _phone)
            return;
        _origin = origin;
        _k = k;
        _phone = phone;
        foreach (var layer in new[] { _backdrop, _face })
        {
            layer.Position = origin;
            layer.Size = area;
            layer.QueueRedraw();
        }
    }

    /// <summary>
    /// The view, its entries, the picture of the entry hovered last (touch: selected) and the prompt, from
    /// <paramref name="state"/>.
    /// </summary>
    public void Refresh(GameState state)
    {
        var seen = _state;
        _state = state;
        int view = TreasureRoster.View(state);
        if (view != _view)
        {
            _shown = 0;                                 // the view turned: the empty panel until an entry is hovered (tapped)
            if (seen is not null)
                _selected = 0;                          // not a selection made before the first state (the gallery's)
        }
        _view = view;
        var entries = TreasureRoster.Entries(state);
        if (TouchMode.On)
        {
            if (entries.All(e => e.Number != _selected))
                _selected = 0;
            _shown = _selected;
        }
        else if (_hovered > 0)
            _shown = _hovered;
        _button = TouchMode.On ? TreasureRoster.Button(state, _selected) : null;
        var details = TreasureRoster.Details(state, _shown);
        var prompt = TreasureRoster.Prompt(state);
        _date = HaremRoster.DateText(state.Week.Value) ?? "";
        var pictureKey = details is null ? (0, view == 0 ? TreasureRoster.UnitEmptyCg : TreasureRoster.ItemEmptyCg) : (details.PictureCg, details.EmptyCg);
        if (pictureKey != _pictureKey)
        {
            _pictureKey = pictureKey;
            _picture = _faces.PicturePanel(pictureKey.Item1, TreasureRoster.ScreenCg, pictureKey.Item2);
        }
        string key = $"{view}|{string.Join(",", entries)}|{details?.Name}|{string.Join(",", details?.Figures ?? [])}|{prompt}|{_date}|{Busy}|{_selected}|{_button}";
        if (key == _key)
            return;
        _key = key;
        _entries = entries;
        _details = details;
        _prompt = prompt;
        _face.QueueRedraw();
    }

    /// <summary>
    /// The pointer put on entry <paramref name="number"/>'s cell (0: off the grid), as a hover puts it — in the touch mode the
    /// entry selected, as a tap selects it (unattended play's dumps, the gallery).
    /// </summary>
    public void ShowHover(int number)
    {
        if (TouchMode.On)
            Select(number);
        else
            Hover(number > 0 ? number : NoButton);
    }

    /// <summary>The touch mode's selection: entry <paramref name="number"/> (0: none), its picture and the button for it.</summary>
    private void Select(int number)
    {
        if (number == _selected)
            return;
        _selected = number;
        _key = "";
        if (_state is not null)
            Refresh(_state);
        _face.QueueRedraw();
    }

    // ---------------------------------------------------------------- the buttons

    /// <summary>Cell <paramref name="number"/>: the picker's grid, left to right, top to bottom.</summary>
    internal static Rect2 CellRect(int number) =>
        new(GridX + (number - 1) % Columns * TabletStepX, GridY + (number - 1) / Columns * TabletStepY, TabletW, TabletH);

    /// <summary>The touch mode's button: the script's questions and our own stand beside it (the owner's X4).</summary>
    internal static Rect2 ActionRect => ActionArea;

    /// <summary>Where a callout beside the button stands: the info panel left of it, over the line and the prompt.</summary>
    internal static Rect2 BesideAction => new(InfoPanel.Position.X + 12, InfoPanel.Position.Y + 12,
        ActionArea.Position.X - 16 - InfoPanel.Position.X - 12, InfoPanel.Size.Y - 24);

    private static Rect2 TabRect(int view) => new(TabX + view * TabStep, TabY, TabW, TabH);

    /// <summary>
    /// Shows <paramref name="lines"/> in <paramref name="callout"/> about cell <paramref name="cell"/> as the device lays the
    /// screen out over <paramref name="picture"/> (the picture's area, the callout's parent's coordinates): under the cell on
    /// the PC, beside the button in the touch mode (the owner's X4), on the phone held landscape beside the button at the
    /// phone's sizes, left of it (step L7), held upright across the room over the button in the bar (step P7). The game's
    /// and the screen gallery's.
    /// </summary>
    internal void ShowCallout(TreasureCallout callout, IReadOnlyList<string> lines, int cell, bool waiting, Rect2 picture)
    {
        if (_phone is { } phone)
        {
            callout.PlacePhone(phone);
            var (action, beside) = phone.IsUpright
                ? (PhonePickersUpright.Action(phone), PhonePickersUpright.BesideAction(phone))
                : (PhonePickers.Action(phone), PhonePickers.BesideAction(phone));
            callout.Show(lines, callout.FromGlobal(Global(action)), waiting, callout.FromGlobal(Global(beside)));
            return;
        }
        if (TouchMode.On)
            callout.Show(lines, ActionRect, waiting, BesideAction);
        else
            callout.Show(lines, CellRect(cell), waiting);
        callout.Place(picture);
    }

    /// <summary>A box of the phone's screen in global coordinates (the screen's units start at the view's corner).</summary>
    private Rect2 Global(PhoneFrame.Box box) => new(GlobalPosition + new Vector2(box.X, box.Y), new Vector2(box.W, box.H));

    private int ButtonAt(Vector2 local)
    {
        if (_phone is { } phone)
            return phone.IsUpright ? UprightButtonAt(phone, local) : PhoneButtonAt(phone, local);
        var at = (local - _origin) / _k;
        if (EndArea.HasPoint(at))
            return EndButton;
        if (_button is not null && ActionArea.HasPoint(at))
            return ActionButton;
        if (_prompt is not null && BackArea.HasPoint(at))
            return BackButton;
        if (TabRect(0).HasPoint(at))
            return TabCharacters;
        if (TabRect(1).HasPoint(at))
            return TabItems;
        foreach (var entry in _entries)
            if (CellRect(entry.Number).HasPoint(at))
                return entry.Number;
        return NoButton;
    }

    private bool Enabled(int button) => !Busy && button switch
    {
        TabCharacters => _view != 0,
        TabItems => _view != 1,
        ActionButton => _button is { Refusal: null },
        _ => true,
    };

    public override void _GuiInput(InputEvent e)
    {
        if (_phone is { IsUpright: true } && GridTouch(e))
        {
            AcceptEvent();
            return;
        }
        switch (e)
        {
            case InputEventMouseMotion motion:
                Hover(ButtonAt(motion.Position));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                if (!Busy)
                    BackPressed?.Invoke();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                int button = ButtonAt(click.Position);
                if (click.Pressed)
                    _pressed = button != NoButton && Enabled(button) ? button : NoButton;
                else
                {
                    if (button == _pressed && button != NoButton && Enabled(button))
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
        _key = "";
        if (_state is not null)
            Refresh(_state);
        _face.QueueRedraw();
    }

    private void Click(int button)
    {
        switch (button)
        {
            case EndButton:
                EndPressed?.Invoke();
                break;
            case BackButton:
                BackPressed?.Invoke();
                break;
            case TabCharacters or TabItems:
                TurnPressed?.Invoke();
                break;
            case ActionButton:
                PickPressed?.Invoke(_selected);                 // the selected cell's click, as the PC's
                break;
            default:
                if (TouchMode.On)
                    Select(button);                             // a tap only selects
                else
                    PickPressed?.Invoke(button);
                break;
        }
    }

    // ---------------------------------------------------------------- drawing

    private float Line(float width) => Lacquer.Line(width, _k);

    private void DrawScreen(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (_phone is { } phone)
        {
            if (phone.IsUpright)
                DrawUpright(c, new PhoneUnits(phone));
            else
                DrawPhone(c, new PhoneUnits(phone));
            return;
        }
        DrawTitle(c);
        DrawPicture(c, PictureArea);
        DrawInfo(c);
        DrawGrid(c);
    }

    private void DrawTitle(Control c)
    {
        Ui.DrawPanel(c, TitlePanel, _k);
        var band = new Rect2(TitlePanel.Position.X + 22, TitlePanel.Position.Y + 11, TitlePanel.Size.X - 44, 34);
        c.DrawString(_brush, new Vector2(band.Position.X, Lacquer.Baseline(band, _brush, 24)), "Treasure", fontSize: 24, modulate: Ui.Name);
        float dw = _cinzel.GetStringSize(_date, fontSize: 16).X;
        c.DrawString(_cinzel, new Vector2(band.End.X - dw, Lacquer.Baseline(band, _cinzel, 16) + 1), _date, fontSize: 16, modulate: Ui.Name);
    }

    /// <summary>The picture panel as the picker shows it (the gold frame is the original's), with a shadow.</summary>
    private void DrawPicture(Control c, Rect2 area)
    {
        Shadowed(c, area);
        if (_picture is not null)
            c.DrawTextureRect(_picture, area, false);
        else
            c.DrawRect(area, Colors.Black);
    }

    /// <summary>The original's line as a name, a subtitle and figures; under it the first pick's prompt, or how to pick.</summary>
    private void DrawInfo(Control c)
    {
        Ui.DrawPanel(c, InfoPanel, _k);
        float x = InfoPanel.Position.X + 24, y = InfoPanel.Position.Y;
        if (_details is { } d)
        {
            var band = new Rect2(x, y + 14, 600, 36);
            c.DrawString(_brush, new Vector2(x, Lacquer.Baseline(band, _brush, 24)), d.Name, fontSize: 24, modulate: Ui.Name);
            float nw = _brush.GetStringSize(d.Name, fontSize: 24).X;
            c.DrawString(_cinzel, new Vector2(x + nw + 14, Lacquer.Baseline(band, _cinzel, 13) + 3), d.Subtitle, fontSize: 13,
                modulate: d.SubtitleOff ? Ui.Minus : Ui.Subtitle);
            float fx = x;
            foreach (var figure in d.Figures)
            {
                c.DrawString(_cinzel, new Vector2(fx, y + 74), figure.Label, fontSize: 11, modulate: Ui.Label);
                c.DrawString(_gothic, new Vector2(fx, y + 98), figure.Value, fontSize: 18, modulate: Ui.Value);
                fx += Math.Max(110, _gothic.GetStringSize(figure.Value, fontSize: 18).X + 26);
            }
        }
        else if (TouchMode.On && _prompt is { } first)
            c.DrawString(_gothic, new Vector2(x, y + 70), first.UnitFirst ? "Tap an item to see it." : "Tap a character to see it.",
                fontSize: 15, modulate: Ui.Quiet);
        if (_button is { } b)
            DrawAction(c, b);
        Ui.DrawDivider(c, InfoPanel.Position.X + 16, InfoPanel.End.X - 16, y + 116, _k);
        // Touch: a callout beside the button stands over the line, as tall as its text; what it leaves of the hint and the
        // prompt under it is not drawn (the owner's A, 2026-10-27), Back is.
        bool covered = TouchMode.On && _asked != 0;
        if (_prompt is not { } p)
        {
            if (covered)
                return;
            string verb = TouchMode.On ? "Tap" : "Pick";
            c.DrawString(_gothic, new Vector2(x, y + 152), $"{verb} a character, then the item to equip —", fontSize: 14, modulate: Ui.Quiet);
            c.DrawString(_gothic, new Vector2(x, y + 174), "or an item, then the character to equip it.", fontSize: 14, modulate: Ui.Quiet);
            return;
        }
        if (covered)
        {
            DrawButton(c, BackArea, "Back", BackButton, 15);
            return;
        }
        // A unit's face as the deployment screen cuts it; an item by its name alone (the original's shrunk picture with its
        // description could not be read).
        float tx = x;
        if (p.UnitFirst && FaceTexture(p.FaceCg, PromptFace.Size.X) is { } face)
        {
            Shadowed(c, PromptFace);
            c.DrawTextureRect(face, PromptFace, false);
            c.DrawRect(PromptFace, Ui.Gold, false, Line(1));
            tx = PromptFace.End.X + 24;
        }
        if (p.UnitFirst)
        {
            c.DrawString(_brush, new Vector2(tx, y + 168), p.Name, fontSize: 22, modulate: Ui.Name);
            c.DrawString(_gothic, new Vector2(tx, y + 196), "is equipping —", fontSize: 16, modulate: Ui.Text);
            c.DrawString(_gothic, new Vector2(tx, y + 222), "Which item?", fontSize: 16, modulate: Ui.Text);
        }
        else
        {
            c.DrawString(_gothic, new Vector2(tx, y + 166), "Equipping", fontSize: 16, modulate: Ui.Text);
            c.DrawString(_brush, new Vector2(tx, y + 196), p.Name, fontSize: 22, modulate: Ui.Name);
            c.DrawString(_gothic, new Vector2(tx, y + 222), "Which character?", fontSize: 16, modulate: Ui.Text);
        }
        DrawButton(c, BackArea, "Back", BackButton, 15);
    }

    /// <summary>
    /// The touch mode's button (the owner's X3, X6): red lacquer with its word, or greyed with the picker's refusal under the
    /// word; never the selection's name.
    /// </summary>
    private void DrawAction(Control c, TreasureButton b)
    {
        var r = ActionArea;
        float cx = r.GetCenter().X;
        if (b.Refusal is null)
        {
            bool hover = !Busy && _hovered == ActionButton;
            Ui.DrawRedButton(c, r, hover, hover && _pressed == ActionButton, _k);
            Centred(c, _cinzel, b.Label, 18, Color.Color8(250, 226, 160), cx, Lacquer.Baseline(r, _cinzel, 18));
            return;
        }
        c.DrawRect(r, Ui.OffButton);
        c.DrawRect(r, Ui.OffLine, false, Line(1));
        Centred(c, _cinzel, b.Label, 18, Ui.OffText, cx, Lacquer.Baseline(new Rect2(r.Position.X, r.Position.Y + 6, r.Size.X, 34), _cinzel, 18));
        Centred(c, _gothic, b.Refusal, 14, Ui.Minus, cx, Lacquer.Baseline(new Rect2(r.Position.X, r.Position.Y + 38, r.Size.X, 24), _gothic, 14));
    }

    /// <summary>
    /// The picker's grid: every entry at its own number's cell, three to a row (<c>Sｱｲﾃﾑ</c>'s 3×17), the other cells
    /// empty; a unit's name (a diamond when it holds an item, dim on Chaos and Nikkou), an item's in its colour with its
    /// holder (a padlock on Chaos and Nikkou); the view's tabs and End.
    /// </summary>
    private void DrawGrid(Control c)
    {
        Ui.DrawPanel(c, GridPanel, _k);
        var listed = _entries.ToDictionary(e => e.Number);
        for (int n = 1; n <= TreasureRoster.Cells; n++)
        {
            var r = CellRect(n);
            if (!listed.TryGetValue(n, out var entry))
            {
                c.DrawRect(r, new Color(Ui.OffButton, 0.55f));
                c.DrawRect(r, new Color(Ui.OffLine, 0.6f), false, Line(1));
                continue;
            }
            bool hover = TouchMode.On ? n == _selected : n == _hovered && !Busy;     // touch: the selection, lit as a hover
            if (n == _asked)
            {
                c.DrawRect(r, Ui.ChosenFill);            // the cell the callout asks about
                c.DrawRect(r, Ui.Vermilion, false, Line(2));
            }
            else
            {
                c.DrawRect(r, hover ? Ui.RowHover : Ui.Row);
                c.DrawRect(r, hover ? Ui.HoverLine : Ui.DimGold, false, Line(hover ? 2 : 1));
            }
            float mid = r.Position.Y + r.Size.Y / 2;
            Color ink = entry.Ink switch
            {
                TreasureInk.Off => Ui.OffText,
                TreasureInk.Held => Ui.Name,
                _ => hover ? Ui.Name : Ui.Text,
            };
            float right = r.End.X - 8;
            if (entry.Holder != "")
            {
                float hw = _gothic.GetStringSize(entry.Holder, fontSize: 10).X;
                c.DrawString(_gothic, new Vector2(right - hw, mid + 4), entry.Holder, fontSize: 10, modulate: Ui.Quiet);
                right -= hw + 6;
            }
            if (entry.Fixed && _view == 1)
            {
                DrawLock(c, new Vector2(right - 6, mid + 1), Ui.Quiet);
                right -= 16;
            }
            if (entry.HoldsItem)
            {
                Lacquer.DrawDiamond(c, new Vector2(right - 4, mid), 4, entry.Fixed ? Ui.OffLine : Ui.Gold);
                right -= 12;
            }
            c.DrawString(_gothic, new Vector2(r.Position.X + 10, mid + 5), entry.Name, HorizontalAlignment.Left,
                right - r.Position.X - 14, fontSize: 14, modulate: ink);
        }
        for (int view = 0; view < 2; view++)
        {
            var r = TabRect(view);
            bool on = view == _view, hover = !on && !Busy && _hovered == (view == 0 ? TabCharacters : TabItems);
            c.DrawRect(r, on ? Ui.ChosenFill : hover ? Ui.RowHover : Ui.Row);
            c.DrawRect(r, on ? Ui.Gold : hover ? Ui.HoverLine : Ui.DimGold, false, Line(on || hover ? 2 : 1));
            string label = view == 0 ? "Characters" : "Items";
            Centred(c, _cinzel, label, 15, on || hover ? Ui.Name : Ui.Quiet, r.GetCenter().X, Lacquer.Baseline(r, _cinzel, 15));
        }
        DrawButton(c, EndArea, "End", EndButton, 17);
    }

    private void DrawButton(Control c, Rect2 r, string label, int button, int size)
    {
        bool hover = !Busy && _hovered == button;
        c.DrawRect(r, hover ? Ui.RowHover : Ui.Row);
        c.DrawRect(r, hover ? Ui.HoverLine : Ui.DimGold, false, Line(hover ? 2 : 1));
        Centred(c, _cinzel, label, size, hover ? Ui.Name : Ui.Text, r.GetCenter().X, Lacquer.Baseline(r, _cinzel, size));
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

    private static void Shadowed(Control c, Rect2 p)
    {
        for (int i = 10; i >= 1; i--)
            c.DrawRect(new Rect2(p.Position.X - i + 3, p.Position.Y - i + 8, p.Size.X + 2 * i, p.Size.Y + 2 * i), new Color(0, 0, 0, 0.06f));
    }

    /// <summary>A small padlock: a shackle over a body, <paramref name="s"/> times the PC's size.</summary>
    private void DrawLock(Control c, Vector2 at, Color colour, float s = 1)
    {
        c.DrawArc(at + new Vector2(0, -4 * s), 3.5f * s, Mathf.Pi, Mathf.Tau, 12, colour, Line(1.6f * s));
        c.DrawRect(new Rect2(at.X - 5 * s, at.Y - 4 * s, 10 * s, 8 * s), colour);
    }

    private static void Centred(Control c, Font font, string text, int size, Color colour, float centre, float baseline)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(centre - w / 2, baseline), text, fontSize: size, modulate: colour);
    }

    /// <summary>A layer of the screen drawn by its view.</summary>
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
