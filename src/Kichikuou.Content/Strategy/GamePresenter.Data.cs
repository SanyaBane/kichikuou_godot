using Kichikuou.Content.Screen;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Strategy;

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
    /// from the scale and divisor GameState holds.
    /// </summary>
    private void DrawGraph(GameState s, int title, Sys3Int scale, Sys3Int divisor, DataGraph? graph)
    {
        Screen.DrawBox(64 - 8 - 1, 50 - 1, 512 + 8 * 2 + 2, 300 + 2, 255);
        Screen.Fill(64 - 8, 50, 512 + 8 * 2, 300, 190);
        DataText(ReadText(new ScriptRef(DataFile, 395)), 64 + 8, 352, 16);
        DataText(ReadText(new ScriptRef(DataFile, title), scale), 64, 18, 20);
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
        Screen.Fill(x, ((Sys3Int)350 - military).Value, 16, military.Value, 249);
        Screen.Fill(x, ((Sys3Int)350 - military - sundry).Value, 16, sundry.Value, 252);
        Label(x, 24, funds, ReadText(new ScriptRef(DataFile, 117), record.Funds / 100), over ? 124 : null);
    }

    /// <summary><c>:154-187</c> and the like: one field's bar, cut at 300 with "OV", its value over it (in hundreds; the casualties in tens).</summary>
    private void DrawBar(int x, DataGraph graph, Sys3Int value, Sys3Int divisor)
    {
        Sys3Int height = value / divisor;
        bool over = height > 300;
        if (over)
            height = 300;
        Bar(x, height, graph.Color);
        Label(x, graph.LabelDx, height, ReadText(new ScriptRef(DataFile, graph.Label), value / (Sys3Int)graph.LabelDivisor), over ? graph.Over : null);
    }

    /// <summary>A bar of <paramref name="height"/> on y 350, framed in 255 when it has a height.</summary>
    private void Bar(int x, Sys3Int height, int color)
    {
        int h = height.Value;
        Screen.Fill(x, 350 - h, 16, h, color);
        if (h != 0)
            Screen.DrawBox(x - 1, 350 - h - 1, 16 + 2, h + 2, 255);
    }

    /// <summary>The number at (x − dx, 350 − h − 11) and "OV" at (x, 350 − h + 1), size 8 (the file sets bold, <c>ZB 1</c>).</summary>
    private void Label(int x, int dx, Sys3Int height, string number, int? over)
    {
        int h = height.Value;
        DataText(number, x - dx, 350 - h - 11, 8);
        if (over is int line)
            DataText(ReadText(new ScriptRef(DataFile, line)), x, 350 - h + 1, 8);
    }

    private void DataText(string text, int x, int y, int size) =>
        MessageWriter.DrawString(Screen, _glyphs, text.Replace("\n", ""), x, y, size, _engine.TextColor);
}
