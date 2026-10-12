using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>各種DATA.ADV</c>, the All Data command (<c>COMMAND.ADV:365-369</c>;
/// <c>docs/strategy/survey-commands.md</c> §3.10): the map in miniature with its markers and a
/// menu of four graphs over the last 16 weeks of history (records 0-15, this week first) — the
/// funds with the sundry and military spending stacked in each bar, the sundry expenses, the
/// military spending, the casualties. A graph waits for a click: the left button goes back to the
/// menu, the right one opens the scale menu (1x-128x; kept in <c>Ui_MoneyGraphDiv/Scale</c> for
/// the three money graphs, <c>Ui_DamageGraphDiv/Scale</c> for the casualties, 16x until the first
/// visit sets them). Changes nothing else; every exit leaves <c>Hist_Cur</c> at 0. Under xsystem35
/// the weekly shift leaves every week of history at last week's values (STR-01), so the bars are level.
/// </summary>
public sealed class AllDataCommand(OriginalScriptContext script) : CommandFile(script, "各種DATA.ADV")
{
    /// <summary>The scale menu's divisors per bar height (<c>Tmp0021</c> = 300 × divisor, <c>:350-381</c>) and labels.</summary>
    private static readonly (int Range, int Scale)[] Scales =
        [(10000 * 6 + 5535, 1), (10000 * 3 + 2767, 2), (10000 * 1 + 6383, 4), (8191, 8), (4095, 16), (2047, 32), (1023, 64), (511, 128)];

    /// <summary>One single-bar graph: the history field, the bar's colour, the label's divisor and x offset, and its lines.</summary>
    private sealed record Graph(DataView View, Func<HistRecord, Sys3Int> Value, int Color, int LabelDivisor, int LabelDx, int Title, int Label, int Over);

    private static readonly Graph Sundry = new(DataView.SundryExpenses, h => h.SundryExp, 252, 100, 24, 146, 166, 173);
    private static readonly Graph Military = new(DataView.MilitarySpending, h => h.MilitaryExp, 249, 100, 24, 195, 215, 222);
    private static readonly Graph Casualties = new(DataView.Casualties, h => h.Casualties, 249, 10, 20, 244, 264, 271);

    /// <summary><c>各種DATA.ADV:1-65</c>: the map view, the menu until Return.</summary>
    public async Task RunAsync()
    {
        await Script.ShowAsync(new DataScreenEvent(DataView.Map));
        await EngineAsync("CC", 0, 0, 640, 400, 0, 400);
        await EngineAsync("EC", 0);
        await EngineAsync("CM", 1280, 0, 1920, 800, 0, 67, 640, 266, 0);
        State.Ui_MarkCur = 0;
        while (State.Ui_MarkCur < State.Ui_MarkCount)                   // L_00054: the markers at a third
        {
            var mark = State.Marks[State.Ui_MarkCur];
            await EngineAsync("J0", mark.X / 3 - 8, mark.Y / 3 + 67 - 8);
            await EngineAsync("G", mark.Cg - 10000, 40);
            State.Ui_MarkCur = State.Ui_MarkCur + 1;
        }
        if (State.Ui_MoneyGraphDiv == 0)
        {
            State.Ui_MoneyGraphDiv = (Sys3Int)4095 / 300;
            State.Ui_MoneyGraphScale = 16;
        }
        if (State.Ui_DamageGraphDiv == 0)
        {
            State.Ui_DamageGraphDiv = (Sys3Int)4095 / 300;
            State.Ui_DamageGraphScale = 16;
        }

        while (true)                                                    // L_000c8
        {
            await EngineAsync("CC", 0, 0, 640, 400, 640, 400);
            int choice;
            while (true)                                                // L_000da
            {
                await WaitForReleaseAsync();
                var items = new List<SceneMenuItem> { MenuItem(28) };  // All Funds
                await LineBreakAsync();
                items.Add(MenuItem(32));                                // Sundry Expenses
                await LineBreakAsync();
                items.Add(MenuItem(36));                                // Military Spending
                await LineBreakAsync();
                items.Add(MenuItem(40));                                // All Casualties
                await LineBreakAsync();
                items.Add(MenuItem(44));                                // Return
                choice = await SelectAsync(items);
                if (choice != 0)
                    break;
                // A cancel: after the release, the next press — left (or both) shows the menu again.
                await WaitForReleaseAsync();                            // L_0013a
                await WaitForPressAsync();                              // L_00155
                if (State.RetVal == 16 || State.RetVal == 48)
                    continue;
                choice = 5;
                break;
            }
            switch (choice)
            {
                case 1:
                    await FundsAsync();                                 // L_001a4
                    break;
                case 2:
                    await MoneyGraphAsync(Sundry);                      // L_003ac
                    break;
                case 3:
                    await MoneyGraphAsync(Military);                    // L_004f6
                    break;
                case 4:
                    await CasualtiesAsync();                            // L_00640
                    break;
                default:
                    State.Hist_Cur = 0;                                 // L_00187
                    await EngineAsync("CC", 0, 400, 640, 400, 0, 0);
                    await EngineAsync("ZM", 24);
                    await Script.ShowAsync(new DataScreenEvent(DataView.Closed));
                    return;
            }
        }
    }

    /// <summary>
    /// <c>L_001a4</c> (<c>:66-140</c>): the funds, each week's bar (colour 207, framed) with the
    /// military (249) and sundry (252) spending stacked from the bottom, the funds in hundreds over
    /// it ("OV" when the bar is cut at 300); again after a new scale, then the map view.
    /// </summary>
    private async Task FundsAsync()
    {
        do
        {
            await FrameAsync(DataView.Funds);                           // \L_00937
            await EngineAsync("T", 64, 18);
            await EngineAsync("ZM", 20);
            await ValuesAsync(71, State.Ui_MoneyGraphScale);           // "【All Funds】 … nx"
            State.Hist_Cur = 0;
            State.Tmp0021 = 64 + 8;
            do                                                          // L_001dd
            {
                State.Tmp0027 = 0;
                State.Tmp0022 = Hist.Funds / State.Ui_MoneyGraphDiv;
                State.Tmp0023 = Hist.MilitaryExp / State.Ui_MoneyGraphDiv;
                State.Tmp0024 = Hist.SundryExp / State.Ui_MoneyGraphDiv;
                if (State.Tmp0022 > 300)
                {
                    State.Tmp0022 = 300;
                    State.Tmp0027 = 1;
                }
                if (State.Tmp0023 > 300)
                {
                    State.Tmp0023 = 300;
                    State.Tmp0024 = 0;
                }
                if (State.Tmp0023 + State.Tmp0024 > 300)
                    State.Tmp0024 = (Sys3Int)300 - State.Tmp0023;
                if (State.Tmp0024 > 300)
                    State.Tmp0024 = 300;
                if (State.Tmp0022 < State.Tmp0023 + State.Tmp0024)
                {
                    State.Tmp0025 = State.Tmp0023;
                    State.Tmp0026 = State.Tmp0024;
                    if (State.Tmp0023 > State.Tmp0024)
                        State.Tmp0025 = State.Tmp0025 - 1;
                    if (State.Tmp0023 < State.Tmp0024)
                        State.Tmp0026 = State.Tmp0026 - 1;
                    State.Tmp0023 = State.Tmp0025;
                    State.Tmp0024 = State.Tmp0026;
                }
                await BarAsync(207);
                await EngineAsync("CF", State.Tmp0021, (Sys3Int)350 - State.Tmp0023, 16, State.Tmp0023, 249);
                await EngineAsync("CF", State.Tmp0021, (Sys3Int)350 - State.Tmp0023 - State.Tmp0024, 16, State.Tmp0024, 252);
                await LabelAsync(24, 117, 124, Hist.Funds / 100);
            }
            while (State.Hist_Cur < 16);
            State.Tmp0021 = State.Ui_MoneyGraphDiv * 300;
            await WaitAsync();                                          // \L_00789
            if (State.Tmp0021 == 0)
                break;
            State.Ui_MoneyGraphDiv = State.Tmp0021 / 300;
            State.Ui_MoneyGraphScale = State.Tmp0022;
        }
        while (true);
        await BackToMapAsync();
    }

    /// <summary><c>L_003ac</c> (<c>:141-189</c>), <c>L_004f6</c> (<c>:190-238</c>): a money graph of one field, then the map view.</summary>
    private async Task MoneyGraphAsync(Graph graph)
    {
        do
        {
            await SingleBarsAsync(graph, State.Ui_MoneyGraphScale, State.Ui_MoneyGraphDiv);
            State.Tmp0021 = State.Ui_MoneyGraphDiv * 300;
            await WaitAsync();                                          // \L_00789
            if (State.Tmp0021 == 0)
                break;
            State.Ui_MoneyGraphDiv = State.Tmp0021 / 300;
            State.Ui_MoneyGraphScale = State.Tmp0022;
        }
        while (true);
        await BackToMapAsync();
    }

    /// <summary><c>L_00640</c> (<c>:239-287</c>): the casualties in tens, on their own scale, then the map view.</summary>
    private async Task CasualtiesAsync()
    {
        do
        {
            await SingleBarsAsync(Casualties, State.Ui_DamageGraphScale, State.Ui_DamageGraphDiv);
            State.Tmp0021 = State.Ui_DamageGraphDiv * 300;
            await WaitAsync();                                          // \L_00789
            if (State.Tmp0021 == 0)
                break;
            State.Ui_DamageGraphDiv = State.Tmp0021 / 300;
            State.Ui_DamageGraphScale = State.Tmp0022;
        }
        while (true);
        await BackToMapAsync();
    }

    /// <summary>The frame, the title with the scale, then week by week a bar of one field and its label.</summary>
    private async Task SingleBarsAsync(Graph graph, Sys3Int scale, Sys3Int divisor)
    {
        await FrameAsync(graph.View);                                   // \L_00937
        await EngineAsync("T", 64, 18);
        await EngineAsync("ZM", 20);
        await ValuesAsync(graph.Title, scale);                          // "【…】 … nx"
        State.Hist_Cur = 0;
        State.Tmp0021 = 64 + 8;
        do
        {
            State.Tmp0027 = 0;
            State.Tmp0022 = graph.Value(Hist) / divisor;
            if (State.Tmp0022 > 300)
            {
                State.Tmp0022 = 300;
                State.Tmp0027 = 1;
            }
            await BarAsync(graph.Color);
            await LabelAsync(graph.LabelDx, graph.Label, graph.Over, graph.Value(Hist) / (Sys3Int)graph.LabelDivisor);
        }
        while (State.Hist_Cur < 16);
    }

    /// <summary>A bar of height <c>Tmp0022</c> at <c>Tmp0021</c>, framed when it has a height.</summary>
    private async Task BarAsync(int color)
    {
        await EngineAsync("CF", State.Tmp0021, (Sys3Int)350 - State.Tmp0022, 16, State.Tmp0022, (Sys3Int)color);
        if (State.Tmp0022 != 0)
            await EngineAsync("CB", State.Tmp0021 - 1, (Sys3Int)350 - State.Tmp0022 - 1, 16 + 2, State.Tmp0022 + 2, 255);
    }

    /// <summary>The week's number over its bar, "OV" on a bar cut at 300; then the next week (<c>Tmp0021</c> + 32, <c>Hist_Cur</c> + 1).</summary>
    private async Task LabelAsync(int dx, int line, int over, Sys3Int value)
    {
        await EngineAsync("T", State.Tmp0021 - (Sys3Int)dx, (Sys3Int)350 - State.Tmp0022 - 11);
        await EngineAsync("ZM", 8);
        await EngineAsync("ZB", 1);
        await ValuesAsync(line, value);
        await EngineAsync("ZB", 7);
        if (State.Tmp0027 != 0)
        {
            await EngineAsync("T", State.Tmp0021, (Sys3Int)350 - State.Tmp0022 + 1);
            await EngineAsync("ZM", 8);
            await EngineAsync("ZB", 1);
            await TextAsync(over);                                      // "OV"
            await EngineAsync("ZB", 7);
        }
        State.Tmp0021 = State.Tmp0021 + 32;
        State.Hist_Cur = State.Hist_Cur + 1;
    }

    /// <summary><c>L_00937</c> (<c>:388-396</c>): the graph's frame and the caption under it.</summary>
    private async Task FrameAsync(DataView view)
    {
        await Script.ShowAsync(new DataScreenEvent(view));
        await EngineAsync("CB", 64 - 8 - 1, 50 - 1, 512 + 8 * 2 + 2, 300 + 2, 255);
        await EngineAsync("CF", 64 - 8, 50, 512 + 8 * 2, 300, 190);
        await EngineAsync("CC", 640, 400, 640, 49, 0, 0);
        await EngineAsync("T", 64 + 8, 352);
        await EngineAsync("ZM", 16);
        await TextAsync(395);                                           // "New → Old (Per 1 Week)"
    }

    /// <summary>The map view back (<c>CC 640, 400, 640, 400, 0, 0</c>), then the menu (<c>@L_000c8</c>).</summary>
    private async Task BackToMapAsync()
    {
        await Script.ShowAsync(new DataScreenEvent(DataView.Map));
        await EngineAsync("CC", 640, 400, 640, 400, 0, 0);
    }

    /// <summary>
    /// <c>L_00789</c> (<c>:288-309</c>): after the release, the next press — the left button leaves
    /// the graph (<c>Tmp0021</c> := 0), the right one asks for a scale (<c>L_00804</c>); any other
    /// press keeps <c>Tmp0021</c> and the stale <c>Tmp0022</c> (CMD-25). Then the release.
    /// </summary>
    private async Task WaitAsync()
    {
        await WaitForReleaseAsync();
        await WaitForPressAsync();                                      // L_007a4
        // QUIRK: CMD-25 — another press (both buttons) keeps Tmp0021: the scale label becomes Tmp0022.
        if (State.RetVal == 16)
            State.Tmp0021 = 0;
        if (State.RetVal == 32)
            await ScaleAsync();                                         // \L_00804
        await WaitForReleaseAsync();                                    // L_007e4
    }

    /// <summary>
    /// <c>L_00804</c> (<c>:310-387</c>): the scale menu, 1x-128x: the divisor range into
    /// <c>Tmp0021</c> and the label into <c>Tmp0022</c>; a cancel sets <c>Tmp0021</c> to 0.
    /// </summary>
    private async Task ScaleAsync()
    {
        await WaitForReleaseAsync();
        var items = new List<SceneMenuItem>();
        for (int i = 0; i < Scales.Length; i++)
        {
            if (i > 0)
                await LineBreakAsync();
            items.Add(MenuItem(316 + 4 * i));                           // "1x" … "128x"
        }
        int choice = await SelectAsync(items);
        if (choice == 0)
            State.Tmp0021 = 0;
        else
        {
            State.Tmp0021 = (Sys3Int)Scales[choice - 1].Range;
            State.Tmp0022 = (Sys3Int)Scales[choice - 1].Scale;
        }
        await WaitForReleaseAsync();                                    // L_00917
    }

    /// <summary><c>IM Ui_MouseX, Ui_MouseY:</c> repeated until a button is down.</summary>
    private async Task WaitForPressAsync()
    {
        do
        {
            var m = await MouseAsync();
            State.Ui_MouseX = m.X;
            State.Ui_MouseY = m.Y;
        }
        while (State.RetVal == 0);
    }
}
