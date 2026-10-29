using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// All Data on the phone held landscape (docs/ui/phone-build.md, step L9; docs/ui/phone.md, §23, the owner's LG2m and LG2):
/// the original's screen stays in the frame without the PC's menu box, and our controls stand in the black sides — the four
/// graphs as cells under the Menu cell at the left thumb (the shown one lit), Back at the right thumb and, over a graph, the
/// scale as eight chips over Back (the shown scale lit); a graph's figures are drawn again over its bars at the phone's type
/// (the presenter leaves them off, <see cref="GamePresenter.DataFiguresCovered"/>). Over the menu of graphs a cell answers it
/// with its graph and Back with Return; over the scale menu (a long press on a graph) a chip answers it, Back cancels it (the
/// graph left, as the menu's own cancel), a graph's cell cancels it and gives the menu of graphs its graph next, the shown
/// graph's cell keeps it at its scale. While a graph waits for the click a tap is told the screen (<see cref="Tapped"/>),
/// which queues the original's clicks: Back the left click that leaves the graph, a chip the right click and the scale menu
/// answered with it, another graph's cell the left click and the menu answered with that graph; a tap on the frame stays the
/// original's. Held upright the same menus and taps on our own page (PG2m, PG2; <c>AllDataView.Upright.cs</c>); on no other
/// device it shows. Drawn in the frame's layout pixels over the whole screen (<see cref="PhoneUnits"/>), the places
/// <see cref="PhoneData"/>'s.
/// </summary>
internal sealed partial class AllDataView : Control
{
    /// <summary>What the screen shows: the menu of graphs, a graph waiting for the click, the scale menu over it.</summary>
    public enum Mode { Menu, Graph, Scale }

    /// <summary>
    /// What the sides show, read from the presenter (<see cref="Of"/>): the graphs' names, Back's, the scales', the shown
    /// graph (-1 over the map), the shown scale's chip (-1 when the scale is none of them, CMD-25) and the bars' figures;
    /// for the page held upright the bars themselves, the graph's title and caption, the date and the palette's colours
    /// the bars and the plot are drawn in.
    /// </summary>
    public sealed record Sides(IReadOnlyList<string> Graphs, IReadOnlyList<string> Scales, int Shown, int Scale,
        IReadOnlyList<DataFigure> Figures, IReadOnlyList<DataBar> Bars, string Title, string Caption, string Date,
        IReadOnlyDictionary<int, Color> Inks)
    {
        private static readonly int[] ScaleValues = [1, 2, 4, 8, 16, 32, 64, 128];

        public static Sides Of(GamePresenter p) => new(p.DataGraphNames, p.DataScaleNames, p.Data switch
        {
            DataView.Funds => 0,
            DataView.SundryExpenses => 1,
            DataView.MilitarySpending => 2,
            DataView.Casualties => 3,
            _ => -1,
        }, Array.IndexOf(ScaleValues, p.DataScale), p.DataFigures, [.. p.DataBars], p.DataTitle, p.DataCaption,
            HaremRoster.DateText(p.State?.Week.Value ?? 0) ?? "", InksOf(p));

        private static Dictionary<int, Color> InksOf(GamePresenter p)
        {
            var inks = new Dictionary<int, Color>();
            foreach (int entry in p.DataBars.SelectMany(b => b.Stack.Select(part => part.Color).Prepend(b.Color))
                         .Append(GamePresenter.DataPlotColor).Append(GamePresenter.DataFrameColor))
            {
                var (r, g, b) = p.Screen.Palette[entry];
                inks[entry] = Color.Color8(r, g, b);
            }
            return inks;
        }

        /// <summary>What the drawing depends on, to draw again only on a change.</summary>
        public string Key => $"{Shown}|{Scale}|{string.Join(",", Figures)}|{Title}|{Date}|"
            + string.Join(",", Bars.Select(b => $"{b.Height}/{string.Join("/", b.Stack.Select(part => part.Height))}"));
    }

    private const string BackWord = "Back";

    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private static readonly Color FigureInk = Color.Color8(236, 236, 236);

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700);
    private FontFile _gothic;
    private PhoneFrame? _phone;
    private Mode _mode;
    private Sides? _sides;
    private string _key = "";
    private int _pressed = -1;                      // a part held: a graph 0-3, a chip 4-11, Back 12
    private TaskCompletionSource<(int Choice, int? Then)>? _answer;

    private const int ChipBase = PhoneData.Graphs, BackPart = PhoneData.Graphs + PhoneData.Scales;

    /// <summary>A tap while a graph waits: a graph's cell (0-3), a chip (0-7), or Back (both null).</summary>
    public event Action<int?, int?>? Tapped;

    public AllDataView(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>Whether one of the menus waits for our answer.</summary>
    public bool Choosing => _answer is not null;

    /// <summary>Shows the menu of graphs (<see cref="Mode.Menu"/>) or the scale menu (<see cref="Mode.Scale"/>); <see cref="ChooseAsync"/> waits.</summary>
    public void OpenMenu(Mode mode, Sides sides)
    {
        Show(mode, sides);
        _answer = new TaskCompletionSource<(int, int?)>();
    }

    /// <summary>
    /// The menu's answer (an item, 1-based, or 0 for a cancel) and, when a graph's cell cancelled the scale menu, the menu
    /// of graphs' answer that follows.
    /// </summary>
    public Task<(int Choice, int? Then)> ChooseAsync() => _answer?.Task ?? throw new InvalidOperationException("No menu of All Data is open.");

    /// <summary>The menu answered or left.</summary>
    public void CloseMenu()
    {
        _answer?.TrySetCanceled();
        _answer = null;
        Visible = false;
    }

    /// <summary>A graph waiting for the click: the sides of <see cref="Mode.Graph"/>, drawn again when they change.</summary>
    public void ShowGraph(Sides sides)
    {
        if (_answer is null)
            Show(Mode.Graph, sides);
    }

    public void HideSides()
    {
        if (_answer is null)
            Visible = false;
    }

    private void Show(Mode mode, Sides sides)
    {
        if (!Visible || mode != _mode || sides.Key != _key)
        {
            _pressed = -1;
            QueueRedraw();
        }
        _mode = mode;
        _sides = sides;
        _key = sides.Key;
        Visible = true;
    }

    /// <summary>Lays the view over the whole screen in the phone's frame, held either way; on any other device it never shows.</summary>
    public void Place()
    {
        var phone = Device.Held(this);
        if (phone is null)
        {
            Visible = false;
            return;
        }
        var size = GetParentAreaSize();
        if (phone == _phone && Size == size)
            return;
        Position = Vector2.Zero;
        Size = size;
        _phone = phone;
        QueueRedraw();
    }

    /// <summary>A part's box in layout pixels.</summary>
    private Rect2 RectOf(PhoneUnits u, int part) => u.Frame.IsUpright ? UprightRectOf(u, part)
        : u.Rect(part < ChipBase ? PhoneData.Graph(u.Frame, part)
            : part < BackPart ? PhoneData.Chip(u.Frame, part - ChipBase) : PhoneData.Back(u.Frame));

    private IEnumerable<int> Parts => _mode == Mode.Menu
        ? [.. Enumerable.Range(0, PhoneData.Graphs), BackPart]
        : Enumerable.Range(0, BackPart + 1);

    private int PartAt(Vector2 at)
    {
        if (_phone is not { } f)
            return -1;
        var u = new PhoneUnits(f);
        var layout = at / u.K;
        foreach (int part in Parts)
            if (RectOf(u, part).HasPoint(layout))
                return part;
        return -1;
    }

    /// <summary>
    /// A menu takes every press over the screen; a graph held landscape only those on our parts, the frame's being the
    /// original's; held upright our page every press.
    /// </summary>
    public override bool _HasPoint(Vector2 point) =>
        Visible && (_answer is not null || _phone is { IsUpright: true } || PartAt(point) >= 0);

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false } when _answer is not null:
                Answer(_mode == Mode.Menu ? BackChoice : 0);                // a long press or Back: as our Back
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = PartAt(button.Position);
                if (button.Pressed)
                    _pressed = at;
                else if (at >= 0 && at == _pressed)
                    Act(at);
                else
                    _pressed = -1;
                QueueRedraw();
                break;
        }
        AcceptEvent();
    }

    /// <summary>The menu of graphs' Return: its last item.</summary>
    private int BackChoice => PhoneData.Graphs + 1;

    private void Act(int part)
    {
        _pressed = -1;
        if (_sides is not { } s)
            return;
        int? graph = part < ChipBase ? part : null, scale = part is >= ChipBase and < BackPart ? part - ChipBase : null;
        switch (_mode)
        {
            case Mode.Menu:
                Answer(graph is int cell ? cell + 1 : BackChoice);
                break;
            case Mode.Scale when scale is int chip:
                Answer(chip + 1);
                break;
            case Mode.Scale when graph == s.Shown && graph is not null && s.Scale >= 0:
                Answer(s.Scale + 1);                                        // the shown graph kept, at its scale
                break;
            case Mode.Scale when graph is int other && other != s.Shown:
                Answer(0, other + 1);
                break;
            case Mode.Scale:
                Answer(0);
                break;
            case Mode.Graph when graph == s.Shown && graph is not null || scale == s.Scale && scale is not null:
                break;                                                      // what is shown already
            case Mode.Graph:
                Tapped?.Invoke(graph, scale);
                break;
        }
    }

    private void Answer(int choice, int? then = null)
    {
        var answer = _answer;
        _answer = null;
        answer?.TrySetResult((choice, then));
    }

    public override void _Draw()
    {
        if (_phone is not { } f || _sides is not { } s)
            return;
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        if (f.IsUpright)
        {
            DrawUpright(u, s);
            return;
        }
        var (gx, gy) = PhoneData.GraphsHeading(f);
        Heading(u, "GRAPHS", gx, gy);
        for (int i = 0; i < PhoneData.Graphs && i < s.Graphs.Count; i++)
            GraphCell(u, RectOf(u, i), s.Graphs[i], i == s.Shown && _mode != Mode.Menu, _pressed == i);
        if (_mode != Mode.Menu)
        {
            var (sx, sy) = PhoneData.ScaleHeading(f);
            Heading(u, "SCALE", sx, sy);
            for (int i = 0; i < PhoneData.Scales && i < s.Scales.Count; i++)
                Chip(u, RectOf(u, ChipBase + i), s.Scales[i], i == s.Scale, _pressed == ChipBase + i);
            int size = u.Size(PhoneData.FigureText);
            foreach (var figure in s.Figures)
            {
                var (x, baseline) = PhoneData.Figure(f, figure.X, figure.Top);
                float w = _gothic.GetStringSize(figure.Text, fontSize: size).X;
                DrawString(_gothic, new Vector2(x / u.K - w / 2, baseline / u.K), figure.Text, fontSize: size, modulate: FigureInk);
            }
        }
        var back = RectOf(u, BackPart);
        bool held = _pressed == BackPart;
        Ui.DrawPanel(this, back, u.K, held ? Ui.RowHover : null);
        int backSize = SideCells.Fit(_brush, BackWord, u.Size(PhoneData.BackText), back.Size.X - u.Px(30));
        Centred(_brush, backSize, back.GetCenter(), BackWord, held ? Ui.Name : Ui.Text);
    }

    /// <summary>A side's heading (GRAPHS, SCALE): Cinzel at its left, its middle given.</summary>
    private void Heading(PhoneUnits u, string text, float x, float middle, float type = PhoneData.HeadingText)
    {
        int size = u.Size(type);
        DrawString(_cinzel, new Vector2(x / u.K, PhonePickerParts.Middle(_cinzel, size, middle / u.K)), text, fontSize: size,
            modulate: Ui.Subtitle);
    }

    /// <summary>A graph's cell: its name in two lines (split at the first space), lit gold while shown.</summary>
    private void GraphCell(PhoneUnits u, Rect2 box, string name, bool lit, bool held)
    {
        DrawRect(box, lit || held ? Ui.RowHover : Ui.Row);
        DrawRect(box, lit ? Ui.Gold : held ? Ui.HoverLine : Ui.DimGold, false, Lacquer.Line(u.Px(lit ? 3 : 2), u.K));
        int space = name.IndexOf(' ');
        string[] lines = space > 0 ? [name[..space], name[(space + 1)..]] : [name];
        int size = u.Size(PhoneData.CellText);
        foreach (var line in lines)
            size = SideCells.Fit(_gothic, line, size, box.Size.X - u.Px(24));
        var middle = box.GetCenter();
        for (int i = 0; i < lines.Length; i++)
        {
            float dy = (i - (lines.Length - 1) / 2f) * u.Px(PhoneData.CellLine);
            Centred(_gothic, size, middle + new Vector2(0, dy), lines[i], lit ? Ui.Name : Ui.Text);
        }
    }

    /// <summary>A scale's chip: vermilion framed in gold while it is the shown scale.</summary>
    private void Chip(PhoneUnits u, Rect2 box, string word, bool lit, bool held, float type = PhoneData.ChipText)
    {
        DrawRect(box, lit ? Ui.Vermilion : held ? Ui.RowHover : Ui.Row);
        DrawRect(box, lit ? Ui.Gold : held ? Ui.HoverLine : Ui.DimGold, false, Lacquer.Line(u.Px(lit ? 3 : 2), u.K));
        int size = SideCells.Fit(_gothic, word, u.Size(type), box.Size.X - u.Px(16));
        Centred(_gothic, size, box.GetCenter(), word, lit ? Ui.Name : Ui.Text);
    }

    private void Centred(Font font, int size, Vector2 middle, string text, Color ink)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        DrawString(font, new Vector2(middle.X - w / 2, PhonePickerParts.Middle(font, size, middle.Y)), text, fontSize: size, modulate: ink);
    }
}
