using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

// All Data (各種DATA) drawn by hand over the stage.
public sealed partial class GamePresenter
{
    private const string DataFile = "各種DATA.ADV";

    /// <summary>One graph of <c>各種DATA</c>: the title's line, the bars' colour, the labels' line, divisor and offset, the "OV" line.</summary>
    private sealed record DataGraph(int Title, int Color, int Label, int LabelDivisor, int LabelDx, int Over);

    private static readonly DataGraph SundryGraph = new(146, 252, 166, 100, 24, 173);
    private static readonly DataGraph MilitaryGraph = new(195, 249, 215, 100, 24, 222);
    private static readonly DataGraph CasualtiesGraph = new(244, 249, 264, 10, 20, 271);

    /// <summary>
    /// What <c>各種DATA</c> shows (<see cref="DataScreenEvent"/>), or <see cref="DataView.Closed"/>:
    /// while it is open its screen is drawn by hand from GameState and the file's own drawing
    /// commands and text are left off the stage, as a picker's.
    /// </summary>
    public DataView Data { get; private set; }

    /// <summary>The palette's entries of a graph's plot and of its frames (<c>各種DATA.ADV:388-396</c>, <c>L_00937</c>).</summary>
    public const int DataPlotColor = 190, DataFrameColor = 255;

    private readonly List<DataFigure> _dataFigures = [];
    private readonly List<DataBar> _dataBars = [];
    private bool _dataFiguresCovered, _dataGraphCovered;

    /// <summary>The shown graph's figures over its bars as last drawn, newest left; empty over the map's view.</summary>
    public IReadOnlyList<DataFigure> DataFigures => _dataFigures;

    /// <summary>The shown graph's bars as last drawn, newest left; empty over the map's view.</summary>
    public IReadOnlyList<DataBar> DataBars => _dataBars;

    /// <summary>The shown graph's title with its scale, as the file prints it over the plot; empty over the map's view.</summary>
    public string DataTitle { get; private set; } = "";

    /// <summary>The shown graph's caption under the plot (<c>:395</c>); empty over the map's view.</summary>
    public string DataCaption { get; private set; } = "";

    /// <summary>
    /// The phone held upright lays a graph out again at its own type under the map in miniature (docs/ui/phone.md, §22,
    /// PG2): while set the graph — its plot, bars, figures, title and caption — is left off the screen, which keeps the
    /// miniature whole; <see cref="DataBars"/> and the rest still describe it.
    /// </summary>
    public bool DataGraphCovered
    {
        get => _dataGraphCovered;
        set
        {
            if (value == _dataGraphCovered)
                return;
            _dataGraphCovered = value;
            Invalidate();
        }
    }

    /// <summary>
    /// The phone held landscape draws a graph's figures again at its own type over the picture (docs/ui/phone.md, §23,
    /// LG2): while set they are left off the screen ("OV" stays).
    /// </summary>
    public bool DataFiguresCovered
    {
        get => _dataFiguresCovered;
        set
        {
            if (value == _dataFiguresCovered)
                return;
            _dataFiguresCovered = value;
            Invalidate();
        }
    }

    /// <summary>The menu's four graphs' names (<c>各種DATA.ADV:29-41</c>), as its items print them.</summary>
    public IReadOnlyList<string> DataGraphNames => [.. ((int[])[29, 33, 37, 41]).Select(DataItem)];

    /// <summary>The menu's Return (<c>:45</c>).</summary>
    public string DataReturnName => DataItem(45);

    /// <summary>The scale menu's items, 1x to 128x (<c>:317-345</c>), their padding taken off.</summary>
    public IReadOnlyList<string> DataScaleNames => [.. Enumerable.Range(0, 8).Select(i => DataItem(317 + 4 * i))];

    /// <summary>The shown graph's scale (1x to 128x: <c>Ui_MoneyGraphScale</c>, the casualties' <c>Ui_DamageGraphScale</c>); 0 over the map.</summary>
    public int DataScale => State is not { } s ? 0 : Data switch
    {
        DataView.Funds or DataView.SundryExpenses or DataView.MilitarySpending => s.Ui_MoneyGraphScale.Value,
        DataView.Casualties => s.Ui_DamageGraphScale.Value,
        _ => 0,
    };

    private string DataItem(int line) => ReadText(new ScriptRef(DataFile, line)).Trim(' ', '　');

    private void DataScreen(DataView view)
    {
        Data = view;
        _target = view == DataView.Closed ? TextTarget.Window : TextTarget.None;
    }

    /// <summary>
    /// <c>各種DATA.ADV:1-11</c>: the screen cleared (<c>EC 0</c>), the map's tiles in miniature at
    /// (0, 67) (<c>CM 1280, 0, 1920, 800, 0, 67, 640, 266, 0</c> from the stage's tile area) and the
    /// markers of <c>Ui_MarkCg</c> over 10000 as that CG less 10000 at a third of their place; a
    /// graph over it (<see cref="DrawGraph"/>).
    /// </summary>
    private void DrawData(GameState s)
    {
        _dataFigures.Clear();
        _dataBars.Clear();
        DataTitle = DataCaption = "";
        Screen.Fill(0, 0, Surface.ScreenWidth, Surface.ScreenHeight, 0);
        EnsureTiles();
        Screen.ScaledCopyFrom(Stage, TileArea, 0, 1920, 800, 0, 67, 640, 266, 0);
        for (int i = 0; i < s.Ui_MarkCount.Value; i++)
        {
            var mark = s.Marks[(Sys3Int)i];
            var cg = mark.Cg - (Sys3Int)10000;
            if (cg != 0 && _cgs.Get(cg.Value) is { } picture)
                Screen.DrawCg(picture, (mark.X / (Sys3Int)3 - (Sys3Int)8).Value, (mark.Y / (Sys3Int)3 + (Sys3Int)67 - (Sys3Int)8).Value, Surface.SpriteKey);
        }
        switch (Data)
        {
            case DataView.Funds: DrawGraph(s, 71, s.Ui_MoneyGraphScale, s.Ui_MoneyGraphDiv, null); break;
            case DataView.SundryExpenses: DrawGraph(s, SundryGraph.Title, s.Ui_MoneyGraphScale, s.Ui_MoneyGraphDiv, SundryGraph); break;
            case DataView.MilitarySpending: DrawGraph(s, MilitaryGraph.Title, s.Ui_MoneyGraphScale, s.Ui_MoneyGraphDiv, MilitaryGraph); break;
            case DataView.Casualties: DrawGraph(s, CasualtiesGraph.Title, s.Ui_DamageGraphScale, s.Ui_DamageGraphDiv, CasualtiesGraph); break;
        }
    }

    /// <summary>
    /// A graph (<c>L_00937</c>, <c>:388-396</c>: the frame (55, 49, 530, 302) in 255 filled with 190,
    /// the caption at (72, 352); the title with the scale at (64, 18)) and its 16 bars, history
    /// record k at x = 72 + 32k, newest left, standing on y 350 (<see cref="DrawFundsBar"/>,
    /// <see cref="DrawBar"/>). The file draws the graph again after a new scale; here it is drawn
    /// from the scale and divisor GameState holds; it is kept (<see cref="DataBars"/>) and left off the screen while
    /// <see cref="DataGraphCovered"/>.
    /// </summary>
    private void DrawGraph(GameState s, int title, Sys3Int scale, Sys3Int divisor, DataGraph? graph)
    {
        string caption = ReadText(new ScriptRef(DataFile, 395)), heading = ReadText(new ScriptRef(DataFile, title), scale);
        DataTitle = heading.Replace("\n", "").Trim();
        DataCaption = caption.Replace("\n", "").Trim();
        if (!_dataGraphCovered)
        {
            Screen.DrawBox(64 - 8 - 1, 50 - 1, 512 + 8 * 2 + 2, 300 + 2, DataFrameColor);
            Screen.Fill(64 - 8, 50, 512 + 8 * 2, 300, DataPlotColor);
            DataText(caption, 64 + 8, 352, 16);
            DataText(heading, 64, 18, 20);
        }
        for (int k = 0; k < 16; k++)
        {
            var record = s.History[(Sys3Int)k];
            int x = 64 + 8 + 32 * k;
            if (graph is null)
                DrawFundsBar(record, x, divisor);
            else
                DrawBar(x, graph, Value(record, graph), divisor);
        }
    }

    private static Sys3Int Value(HistRecord record, DataGraph graph) =>
        graph == SundryGraph ? record.SundryExp : graph == MilitaryGraph ? record.MilitaryExp : record.Casualties;

    /// <summary>
    /// <c>L_001dd</c> (<c>:78-124</c>): the funds' bar (207, framed in 255) with the military (249)
    /// and sundry (252) spending stacked from the bottom, with the script's clamps: each cut at
    /// 300, the sundry part cut to what the military leaves of 300, and when the two top the funds
    /// the larger shrunk by one.
    /// </summary>
    private void DrawFundsBar(HistRecord record, int x, Sys3Int divisor)
    {
        Sys3Int funds = record.Funds / divisor, military = record.MilitaryExp / divisor, sundry = record.SundryExp / divisor;
        Sys3Int top = 300;
        bool over = false;
        if (funds > top)
        {
            funds = top;
            over = true;
        }
        if (military > top)
        {
            military = top;
            sundry = 0;
        }
        if (military + sundry > top)
            sundry = top - military;
        if (sundry > top)
            sundry = top;
        if (funds < military + sundry)
        {
            Sys3Int m = military, n = sundry;
            if (military > sundry)
                m = m - 1;
            if (military < sundry)
                n = n - 1;
            military = m;
            sundry = n;
        }
        Bar(x, funds, 207);
        if (!_dataGraphCovered)
        {
            Screen.Fill(x, ((Sys3Int)350 - military).Value, 16, military.Value, 249);
            Screen.Fill(x, ((Sys3Int)350 - military - sundry).Value, 16, sundry.Value, 252);
        }
        string figure = Label(x, 24, funds, ReadText(new ScriptRef(DataFile, 117), record.Funds / 100), over ? 124 : null);
        _dataBars.Add(new DataBar(x, funds.Value, 207, [new(0, military.Value, 249), new(military.Value, sundry.Value, 252)],
            figure, over ? Over(124) : null));
    }

    /// <summary><c>:154-187</c> and the like: one field's bar, cut at 300 with "OV", its value over it (in hundreds; the casualties in tens).</summary>
    private void DrawBar(int x, DataGraph graph, Sys3Int value, Sys3Int divisor)
    {
        Sys3Int height = value / divisor;
        bool over = height > 300;
        if (over)
            height = 300;
        Bar(x, height, graph.Color);
        string figure = Label(x, graph.LabelDx, height, ReadText(new ScriptRef(DataFile, graph.Label), value / (Sys3Int)graph.LabelDivisor),
            over ? graph.Over : null);
        _dataBars.Add(new DataBar(x, height.Value, graph.Color, [], figure, over ? Over(graph.Over) : null));
    }

    private string Over(int line) => ReadText(new ScriptRef(DataFile, line)).Replace("\n", "").Trim();

    /// <summary>A bar of <paramref name="height"/> on y 350, framed in 255 when it has a height.</summary>
    private void Bar(int x, Sys3Int height, int color)
    {
        if (_dataGraphCovered)
            return;
        int h = height.Value;
        Screen.Fill(x, 350 - h, 16, h, color);
        if (h != 0)
            Screen.DrawBox(x - 1, 350 - h - 1, 16 + 2, h + 2, 255);
    }

    /// <summary>
    /// The number at (x − dx, 350 − h − 11) and "OV" at (x, 350 − h + 1), size 8 (the file sets bold, <c>ZB 1</c>); the
    /// number kept in <see cref="DataFigures"/> and returned, left off the screen while <see cref="DataFiguresCovered"/>
    /// (both while <see cref="DataGraphCovered"/>).
    /// </summary>
    private string Label(int x, int dx, Sys3Int height, string number, int? over)
    {
        int h = height.Value;
        string figure = number.Replace("\n", "").Trim();
        _dataFigures.Add(new DataFigure(x, 350 - h, figure));
        if (!_dataFiguresCovered && !_dataGraphCovered)
            DataText(number, x - dx, 350 - h - 11, 8);
        if (over is int line && !_dataGraphCovered)
            DataText(ReadText(new ScriptRef(DataFile, line)), x, 350 - h + 1, 8);
        return figure;
    }

    private void DataText(string text, int x, int y, int size) =>
        MessageWriter.DrawString(Screen, _glyphs, text.Replace("\n", ""), x, y, size, _engine.TextColor);
}

/// <summary>A bar's figure over a graph of All Data: the bar's left and top on the screen (script pixels) and its number, unpadded.</summary>
public sealed record DataFigure(int X, int Top, string Text);

/// <summary>
/// A bar of a graph of All Data as drawn: its left on the screen (script pixels, 16 wide), its height over the plot's foot
/// (y 350; at most 300) in the palette's entry <paramref name="Color"/>, framed in 255 when it has a height; the spending
/// stacked over it from the foot (the funds' graph: the military, then the sundry; empty on the others), its figure, and
/// its "OV" when it is cut at the plot's top.
/// </summary>
public sealed record DataBar(int X, int Height, int Color, IReadOnlyList<DataSpan> Stack, string Figure, string? Over);

/// <summary>A part stacked over a funds' bar: its foot and height over the plot's foot (script pixels), its palette entry.</summary>
public readonly record struct DataSpan(int Foot, int Height, int Color);
