using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The strategic presenter (<see cref="GamePresenter"/>) on whole runs of the player's game,
/// headless: every event is shown and the screen composed at every input, the way a UI does, and
/// the player's choices go through the presenter's own hit tests (the picker grid, the menu).
/// Skipped without the decompile.
/// </summary>
public partial class GamePresenterTests(ITestOutputHelper output)
{
    /// <summary>
    /// Week 9 of a new game played through <see cref="GameSession"/>: Sill's CG scene, Pillage on
    /// province 34 with the deployment picker and the battle, a talk picked in the subordinate
    /// picker, a harem visit, the hideout, a save into slot 2 on the save screen, Turn END; week 10:
    /// the subordinate picker left with End (its file leaves it open, the map covers it), Turn END.
    /// Every text resolves and every screen is drawn; no picker outlives the map.
    /// </summary>
    [ScenarioFact]
    public async Task A_prologue_week_is_drawn_through_the_presenter()
    {
        var run = await RunAsync(stopAtWeek: 11);
        var player = run.Player;
        player.Map.Enqueue(Gesture.Province(34));                           // Pillage → Sﾌﾞﾀｲ
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Cell, 1)); // Rance,
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Button, 1)); // Attack
        player.Map.Enqueue(Gesture.Menu(2));                                // Subordinates → Sﾌﾞｶ
        player.Pickers.Enqueue((PickerKind.Subordinates, PickerArea.Cell, 3)); // Bound
        player.Map.Enqueue(Gesture.Menu(1));                                // Harem → Sﾊｰﾚﾑ
        player.Pickers.Enqueue((PickerKind.Harem, PickerArea.Cell, 1));     // Sill
        player.Map.Enqueue(Gesture.Dungeon(1));                             // the hideout
        player.Map.Enqueue(Gesture.Menu(4));                                // Load/Save:
        player.Answers["ﾛｰﾄﾞｾｰﾌﾞ.ADV"] = new([2, 1, 0, 3]);                 // Save, slot "1", back, Don't
        player.Map.Enqueue(Gesture.Menu(5));                                // Turn END
        player.Map.Enqueue(Gesture.Menu(2));                                // week 10: Subordinates,
        player.Pickers.Enqueue((PickerKind.Subordinates, PickerArea.Button, 3)); // End (Sﾌﾞｶ stays open, the map covers it)
        player.Map.Enqueue(Gesture.Menu(5));                                // Turn END

        await Assert.ThrowsAsync<WeekOverException>(() => run.Session.NewGameAsync());

        var p = run.Presenter;
        output.WriteLine($"composed {player.Composed}, pointer reads {player.Reads}, menus {player.Menus}, pickers {string.Join(",", player.PickersSeen)}");
        Assert.Empty(p.Unresolved.Concat(p.Battle.Unresolved).Distinct());
        Assert.Empty(player.Map);
        Assert.Empty(player.Pickers);
        Assert.Equal(0, player.Pending);
        Assert.Contains(2, run.Saves.Slots);                                // the save screen saved
        Assert.Equal([PickerKind.Deployment, PickerKind.Subordinates, PickerKind.Harem], player.PickersSeen.Distinct());
        Assert.True(player.BattleFrames > 0, "the battle was drawn");
        Assert.True(player.WindowTexts > 10, "message windows were drawn with text");
        Assert.All(player.MenuTexts, t => Assert.NotEmpty(t.Trim()));         // every menu item has its text
        Assert.All(player.MenuViews, m => Assert.NotNull(m.Window));          // menus as data: in a selection window,
        Assert.Contains(player.MenuViews, m => m.Items.Any(i => i.Mark == '○' && !i.Text.StartsWith('○')));   // the marks split off
        Assert.Equal(11, run.Session.State.Week.Value);
    }

    /// <summary><see cref="MenuEntry.FromText"/>: a leading ○, × or △ is the item's mark; the text keeps the rest.</summary>
    [Theory]
    [InlineData("○Harem", '○', "Harem")]
    [InlineData("×Construction", '×', "Construction")]
    [InlineData("△Subordinates", '△', "Subordinates")]
    [InlineData("Turn END", null, "Turn END")]
    [InlineData("", null, "")]
    public void A_menu_item_splits_off_its_mark(string printed, char? mark, string text)
    {
        var entry = MenuEntry.FromText(printed);
        Assert.Equal(mark, entry.Mark);
        Assert.Equal(text, entry.Text);
        Assert.Equal(printed, entry.Shown);
    }

    /// <summary>
    /// The rebels' turn (<c>TCﾘｰｻﾞｽ</c>) on the booted world with the rebels holding 3-7: army A
    /// attacks province 2 and army B province 8, each through <c>FIGT_B</c>, whose intercept opens
    /// the deployment picker inside the battle — drawn by hand over the battle's screen and clicked
    /// through its own hit test (a unit, then Intercept) — and the battle is fought.
    /// </summary>
    [ScenarioFact]
    public async Task A_defence_picks_its_party_in_a_picker_drawn_over_the_battle()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var state = run.Session.State;
        state.StoryStage = 1;
        state.F_LeazasRebels_State = 2;
        for (int p = 1; p <= 8; p++)
            state.Provs[p].Owner = (Sys3Int)(p is >= 3 and <= 7 ? 2 : 1);
        for (int c = 1; c <= 7; c++)
        {
            state.Cmdrs[c].Status = 1;
            state.Cmdrs[c].Troops = 300;
        }
        var player = run.Player;
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Cell, 2));   // Sill (Rance's death would end the game),
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Button, 1)); // Intercept
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Cell, 3));   // then Bound,
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Button, 1)); // Intercept

        await run.Session.Script.CallFileAsync("TCﾘｰｻﾞｽ.ADV");

        var p2 = run.Presenter;
        output.WriteLine($"composed {player.Composed}, battle frames {player.BattleFrames}, picker frames in battle {player.BattlePickerFrames}");
        Assert.Empty(p2.Unresolved.Concat(p2.Battle.Unresolved).Distinct());
        Assert.Empty(player.Pickers);
        Assert.Equal([PickerKind.Deployment, PickerKind.Deployment], player.PickersSeen);
        Assert.True(player.BattlePickerFrames > 0, "the picker was composed over the battle");
        Assert.Null(p2.Picker);
        Assert.False(p2.InBattle);
    }

    /// <summary>
    /// The Treasure command (<c>Cﾀｶﾗ</c> → <c>Sｱｲﾃﾑ</c>) on the booted world, played through the
    /// item screen drawn by hand: a unit hovered (its details), picked (the prompt "&lt;unit&gt; is
    /// equipping / Which item?" over the shrunk picture panel), the pick dropped with a right click,
    /// the item view, a held item asked about (the question in the narration window, Yes/No at the
    /// pointer, which sits on No) and kept, an item from stock picked first (its prompt) and
    /// equipped on a unit, End. Every click goes through the drawn screen's hit test.
    /// </summary>
    [ScenarioFact]
    public async Task The_item_screen_is_drawn_from_its_views_and_picks()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var s = run.Session.State;
        s.StoryStage = 1;
        s.Ui_ItemViewMode = 0;
        s.Units[5].Status = 1;
        s.Units[5].Item = 4;
        s.Items[4].Holder = 5;
        s.Items[3].Holder = 100;
        s.Items[3].Effect = 1;
        var player = run.Player;
        player.Pickers.Enqueue(new(PickerKind.Items, PickerArea.Cell, 5, PickerPress.Move));    // unit 5 hovered,
        player.Pickers.Enqueue((PickerKind.Items, PickerArea.Cell, 5));                          // picked,
        player.Pickers.Enqueue(new(PickerKind.Items, PickerArea.Cell, 5, PickerPress.Right));   // dropped (998)
        player.Pickers.Enqueue((PickerKind.Items, PickerArea.Button, 2));                        // the item view
        player.Pickers.Enqueue((PickerKind.Items, PickerArea.Cell, 4));                          // item 4 (N. Writings), held: unequip it?
        player.Answers["Sｱｲﾃﾑ.ADV"] = new([2]);                                                  // No
        player.Pickers.Enqueue((PickerKind.Items, PickerArea.Cell, 3));                          // item 3 (Pop. Staff) from stock,
        player.Pickers.Enqueue((PickerKind.Items, PickerArea.Cell, 5));                          // on unit 5
        player.Pickers.Enqueue((PickerKind.Items, PickerArea.Button, 3));                        // End
        var prompts = new List<string[]>();
        var questions = new List<(string Window, int UnderPointer)>();
        bool details = false;
        int quietColours = 0, promptColours = 0;
        player.OnRead = () =>
        {
            if (run.Presenter.Picker is not { Kind: PickerKind.Items } picker)
                return;
            details |= s.Tmp0026 != 0 && picker.PanelText(9) is { IsEmpty: false };
            int colours = Colours(run.Presenter.Screen, 11, 273, 186, 116);
            if (picker.PromptLines(s) is { } lines)
            {
                if (prompts.Count == 0 || !prompts[^1].SequenceEqual(lines))
                    prompts.Add([.. lines]);
                promptColours = Math.Max(promptColours, colours);
            }
            else
            {
                quietColours = Math.Max(quietColours, colours);
            }
        };
        player.OnMenu = () => questions.Add((run.Presenter.WindowText.Text, run.Presenter.MenuHitTest(player.Last.X.Value, player.Last.Y.Value)));

        await run.Session.Script.CallFileAsync("Cﾀｶﾗ.ADV");

        var p = run.Presenter;
        output.WriteLine($"prompts {string.Join(" | ", prompts.Select(l => string.Join("/", l)))}; question {questions.FirstOrDefault()}; colours {quietColours} → {promptColours}");
        Assert.Empty(p.Unresolved);
        Assert.Empty(player.Pickers);
        Assert.Equal([PickerKind.Items], player.PickersSeen);
        Assert.True(details, "the hovered unit's details were drawn");
        string Str(Sys3Int index) => Hankaku.ToHalfWidth(s.Strings[index.Value]);
        Assert.Equal(2, prompts.Count);
        Assert.Equal(Str(s.Unit_StrBase + 4 * 3), prompts[0][0]);                    // "<unit 5>" / is equipping / Which item?
        Assert.Equal(Str(s.Item_StrBase + 2), prompts[1][1]);                         // Equipping / "<item 3>" / Which character?
        Assert.All(prompts.SelectMany(l => l), line => Assert.NotEmpty(line.Trim()));
        Assert.True(promptColours > quietColours + 8, "the shrunk picture panel is in the prompt");
        var (window, underPointer) = Assert.Single(questions);
        Assert.NotEmpty(window.Trim());                                              // "… would you like to unequip it?"
        Assert.Equal(1, underPointer);                                               // Yes/No opens with No under the pointer
        Assert.Equal(3, s.Units[5].Item.Value);                                      // equipped,
        Assert.Equal(5, s.Items[3].Holder.Value);
        Assert.Equal(100, s.Items[4].Holder.Value);                                  // its own item back in stock
        Assert.Null(p.Picker);
    }

    /// <summary>
    /// Subordinates at stage 1: a unit picked, its troop menu cancelled, back in the picker, End. <c>Sﾌﾞｶ</c>'s mode 4
    /// puts back the page its mode 2 kept at (640, 400), with the picker closed: the picker drawn by hand comes back
    /// over the screen it opened over, not over what an older page left there (owner's report, 2026-10-14).
    /// </summary>
    [ScenarioFact]
    public async Task Subordinates_comes_back_from_a_unit_over_the_screen_it_opened_over()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var s = run.Session.State;
        s.StoryStage = 1;
        s.Ui_CmdSubordAvail = 1;
        s.CmdAvail_SubTroops = 1;
        s.CmdAvail_SubTalk = 1;
        s.Units[3].Status = 1;
        await run.Session.Script.EngineAsync("CF", [640, 400, 640, 400, 9]);                     // an older page
        var audit = new PageAudit(Cgs.Value);
        run.Presenter.Audit = audit;
        var player = run.Player;
        player.Pickers.Enqueue((PickerKind.Subordinates, PickerArea.Cell, 3));                   // Bound,
        player.Answers["Cﾌﾞｶ.ADV"] = new([0]);                                                  // the troop menu cancelled,
        player.Pickers.Enqueue((PickerKind.Subordinates, PickerArea.Button, 3));                 // End
        var underPicker = new List<(byte, byte, byte, byte)[]>();
        bool opened = false;
        player.OnShow = e => opened |= e is PickerScreenEvent { Open: true };
        player.OnRead = () =>
        {
            if (!opened || run.Presenter.Picker is not { Kind: PickerKind.Subordinates })
                return;
            opened = false;
            var screen = run.Presenter.Screen;
            var pixels = new List<(byte, byte, byte, byte)>();
            for (int y = 260; y < 400; y += 2)                                                  // the map below the details panel
                for (int x = 0; x < 320; x += 2)
                    pixels.Add(screen.PixelAt(x, y));
            underPicker.Add([.. pixels]);
        };

        await run.Session.Script.CallFileAsync("Cﾌﾞｶ.ADV");

        Assert.Empty(run.Presenter.Unresolved);
        Assert.Empty(player.Pickers);
        Assert.Equal(2, underPicker.Count);                                                      // opened, and back from the unit
        Assert.Empty(audit.Findings);
        Assert.Equal(underPicker[0], underPicker[1]);
    }

    /// <summary>
    /// All Data (<c>各種DATA</c>) on the booted world with 16 weeks of distinct history: the map in
    /// miniature with the menu, the funds graph (a bar cut at 300 with "OV"), the scale menu from a
    /// right click and the graph again at 4x (the bar now 45 high), each other graph, Return. The
    /// screen is drawn by hand; the file's own drawing never reaches the stage.
    /// </summary>
    [ScenarioFact]
    public async Task All_data_draws_the_map_view_and_every_graph()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var s = run.Session.State;
        for (int k = 0; k < 16; k++)
        {
            var h = s.History[(Sys3Int)k];
            h.Funds = (Sys3Int)(k == 0 ? 5000 : 300 * k);
            h.MilitaryExp = (Sys3Int)(k == 0 ? 0 : 100 * k);
            h.SundryExp = (Sys3Int)(k == 0 ? 0 : 50 * k);
            h.Casualties = (Sys3Int)(20 * k);
        }
        var player = run.Player;
        player.Answers["各種DATA.ADV"] = new([1, 3, 2, 3, 4, 5]);           // Funds, (the scale menu) 4x, Sundry, Military, Casualties, Return
        player.DataPresses.Enqueue(32);                                    // the funds graph: right → the scale menu
        foreach (var _ in Enumerable.Range(0, 4))
            player.DataPresses.Enqueue(16);                                // left: back to the map view
        var views = new List<DataView>();
        var bars = new List<(DataView View, int Scale, int Bottom0, int Top0, int Bottom15)>();
        bool menuOverMap = false;
        player.OnShow = e =>
        {
            if (e is DataScreenEvent d)
                views.Add(d.View);
        };
        player.OnRead = () =>
        {
            var p = run.Presenter;
            if (p.Data is DataView.Closed or DataView.Map)
                return;
            int At(int x, int y)
            {
                var (r, g, b, _) = p.Screen.PixelAt(x, y);
                return new[] { 207, 249, 252, 190 }.FirstOrDefault(i => p.Screen.Color(i) == (r, g, b), -1);
            }
            var scale = p.Data == DataView.Casualties ? s.Ui_DamageGraphScale : s.Ui_MoneyGraphScale;
            // Record 0 stands at x 72 (funds 5000, nothing else), record 15 at x 552.
            bars.Add((p.Data, scale.Value, At(76, 349), At(76, 55), At(556, 349)));
        };
        player.OnMenu = () => menuOverMap |= run.Presenter.Data == DataView.Map;

        await run.Session.Script.CallFileAsync("各種DATA.ADV");

        var presenter = run.Presenter;
        output.WriteLine($"views {string.Join(",", views)}; bars {string.Join(" ", bars.Distinct())}");
        Assert.Empty(presenter.Unresolved);
        Assert.Empty(player.DataPresses);
        Assert.Equal(
            [DataView.Map, DataView.Funds, DataView.Funds, DataView.Map, DataView.SundryExpenses, DataView.Map,
             DataView.MilitarySpending, DataView.Map, DataView.Casualties, DataView.Map, DataView.Closed], views);
        Assert.Equal(DataView.Closed, presenter.Data);
        Assert.True(menuOverMap, "the menu is drawn over the map view");
        Assert.Contains((DataView.Funds, 16, 207, 207, 249), bars);                  // 5000 / 13 cut at 300 (OV); military at the foot of record 15
        Assert.Contains((DataView.Funds, 4, 207, 190, 249), bars);                   // 5000 / 109 = 45 high
        Assert.Contains((DataView.SundryExpenses, 4, 190, 190, 252), bars);          // record 0 empty, record 15 750 / 109 = 6
        Assert.Contains((DataView.MilitarySpending, 4, 190, 190, 249), bars);
        Assert.Contains((DataView.Casualties, 16, 190, 190, 249), bars);             // 300 / 13 = 23
        Assert.Equal(4, s.Ui_MoneyGraphScale.Value);
        Assert.Equal(16, s.Ui_DamageGraphScale.Value);
    }

    /// <summary>
    /// All Data for the phone held landscape (docs/ui/phone-build.md, step L9): the graphs' and the scales' names as the
    /// menus print them, the shown graph's scale, and each bar's figure kept for the phone to draw again — left off the
    /// screen while it does (LG2).
    /// </summary>
    [ScenarioFact]
    public async Task All_data_keeps_the_figures_and_names_for_the_phone()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var s = run.Session.State;
        for (int k = 0; k < 16; k++)
            s.History[(Sys3Int)k].Funds = (Sys3Int)(k == 0 ? 5000 : 100 * k);
        var player = run.Player;
        player.Answers["各種DATA.ADV"] = new([1, 5]);                      // Funds, Return
        player.DataPresses.Enqueue(16);
        var p = run.Presenter;
        var seen = new List<(int Scale, int Count, DataFigure First, DataFigure Last, bool Ink)>();
        bool covered = false;
        player.OnRead = () =>
        {
            if (p.Data != DataView.Funds)
                return;
            bool Ink()
            {
                // Record 0's figure at (72 − 24, 350 − 300 − 11): the black over the plot's frame, or the number's ink.
                for (int y = 39; y < 48; y++)
                    for (int x = 48; x < 64; x++)
                        if (p.Screen.PixelAt(x, y) is var (r, g, b, _) && (r, g, b) != p.Screen.Color(0))
                            return true;
                return false;
            }
            p.DataFiguresCovered = covered;
            p.Compose();
            seen.Add((p.DataScale, p.DataFigures.Count, p.DataFigures[0], p.DataFigures[^1], Ink()));
            covered = !covered;
        };

        await run.Session.Script.CallFileAsync("各種DATA.ADV");

        Assert.Equal(["All Funds", "Sundry Expenses", "Military Spending", "All Casualties"], p.DataGraphNames);
        Assert.Equal("Return", p.DataReturnName);
        Assert.Equal(["1x", "2x", "4x", "8x", "16x", "32x", "64x", "128x"], p.DataScaleNames);
        Assert.Contains((16, 16, new DataFigure(72, 50, "50"), new DataFigure(552, 350 - 1500 / 13, "15"), true), seen);
        Assert.Contains((16, 16, new DataFigure(72, 50, "50"), new DataFigure(552, 350 - 1500 / 13, "15"), false), seen);
        Assert.Equal(0, p.DataScale);
    }

    /// <summary>
    /// The phone held upright (docs/ui/phone.md, §22, PG2) lays a graph out again under the map in miniature: the presenter
    /// keeps every bar as drawn — its height, colour, the spending stacked in a funds' bar, its figure and "OV" when cut —
    /// with the graph's title and caption, and while <see cref="GamePresenter.DataGraphCovered"/> leaves the whole graph off
    /// the screen (the plot's grey under the miniature not drawn).
    /// </summary>
    [ScenarioFact]
    public async Task All_data_keeps_the_bars_and_leaves_the_graph_off_for_the_phone_portrait()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var s = run.Session.State;
        for (int k = 0; k < 16; k++)
            s.History[(Sys3Int)k].Funds = (Sys3Int)(k == 0 ? 5000 : 100 * k);
        s.History[(Sys3Int)1].MilitaryExp = (Sys3Int)26;
        s.History[(Sys3Int)1].SundryExp = (Sys3Int)39;
        var player = run.Player;
        player.Answers["各種DATA.ADV"] = new([1, 5]);                      // Funds, Return
        player.DataPresses.Enqueue(16);
        var p = run.Presenter;
        var seen = new List<(IReadOnlyList<DataBar> Bars, string Title, string Caption, bool Plot)>();
        bool covered = false;
        player.OnRead = () =>
        {
            if (p.Data != DataView.Funds)
                return;
            p.DataGraphCovered = covered;
            p.Compose();
            // Left of the first bar under the miniature: the plot's grey, or the cleared screen's black.
            bool plot = p.Screen.PixelAt(60, 345) is var (r, g, b, _) && (r, g, b) == p.Screen.Color(GamePresenter.DataPlotColor);
            seen.Add(([.. p.DataBars], p.DataTitle, p.DataCaption, plot));
            covered = !covered;
        };

        await run.Session.Script.CallFileAsync("各種DATA.ADV");

        Assert.Equal([true, false], seen.Select(x => x.Plot).Take(2));
        foreach (var (bars, title, caption, _) in seen.Take(2))
        {
            Assert.Equal(16, bars.Count);
            Assert.Equal(new DataBar(72, 300, 207, bars[0].Stack, "50", "OV"), bars[0]);        // 5000 / 13 cut at 300
            Assert.Equal([new DataSpan(0, 0, 249), new DataSpan(0, 0, 252)], bars[0].Stack);
            Assert.Equal((104, 100 / 13, 207, "1"), (bars[1].X, bars[1].Height, bars[1].Color, bars[1].Figure));
            Assert.Equal([new DataSpan(0, 26 / 13, 249), new DataSpan(26 / 13, 39 / 13, 252)], bars[1].Stack);
            Assert.Null(bars[1].Over);
            Assert.Equal((552, 1500 / 13, "15"), (bars[15].X, bars[15].Height, bars[15].Figure));
            Assert.StartsWith("【All Funds】", title);
            Assert.EndsWith("16x", title);
            Assert.StartsWith("New", caption);
        }
    }

    /// <summary>
    /// A search of the Monsters' Cave (<c>Cﾒｲｷｭｳ</c>, dungeon 2) at stage 1 on the booted world,
    /// through the router: Start Search, Bound picked in the deployment picker drawn by hand, the
    /// arrival over the cave's picture, a floor down with a battle (every draw is 1: monsters on
    /// each move) drawn by the battle's presenter with <c>Bt_Kind</c> 3, Retreat cancelled into the
    /// second, empty menu (DGN-07: an empty box that nothing hits, left by a cancel), the floor again,
    /// Retreat, Finish.
    /// </summary>
    [ScenarioFact]
    public async Task A_dungeon_search_draws_its_floors_menus_and_battle()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var s = run.Session.State;
        s.StoryStage = 1;
        s.Dgn_Cur = 2;
        s.Ui_CmdDungeonAvail = 1;
        var player = run.Player;
        player.Answers["Cﾒｲｷｭｳ.ADV"] = new([1, 1, 3, 0, 3, 1]);          // Start Search; Descend; Retreat, cancel; (empty); Retreat, Finish
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Cell, 3));   // Bound,
        player.Pickers.Enqueue((PickerKind.Deployment, PickerArea.Button, 1)); // Go
        var battles = new List<(int Kind, int Enemy)>();
        var menus = new List<(string[] Items, string Window, int Hit)>();
        var centres = new HashSet<int>();
        int sceneShown = 0, rounds = 0, stageDrawnInBattle = 0, splitOverStage = 0;
        var headers = new List<(string? Opening, bool Later)>();
        player.OnShow = e =>
        {
            if (e is RoundHeaderEvent)
                headers.Add((run.Presenter.Opening?.Title, run.Presenter.LaterRoundHeader));
            if (e is BattleScreenEvent { Open: true })
                battles.Add((s.Bt_Kind.Value, s.Bt_EnemyParty1.Value));
            if (run.Presenter.BattleSceneShows)
                sceneShown++;
            rounds = Math.Max(rounds, run.Presenter.Order.Rounds);
            if (run.Presenter.InBattle && e is EngineEvent { Command: "G" })
            {
                stageDrawnInBattle++;                                       // 死亡捕獲's scene draws on the stage: the original's screen shows
                if (run.Presenter.BattleSceneShows)
                    splitOverStage++;
            }
        };
        player.OnRead = () =>
        {
            if (!run.Presenter.InBattle)
                centres.Add(s.Ui_CenterCg.Value);
        };
        player.OnMenu = () => menus.Add(([.. run.Presenter.MenuItems!], run.Presenter.WindowText.Text,
            run.Presenter.MenuHitTest(player.Last.X.Value, player.Last.Y.Value)));

        await run.Session.Script.CallFileAsync("Cﾒｲｷｭｳ.ADV");

        var p = run.Presenter;
        foreach (var (items, window, hit) in menus)
            output.WriteLine($"[{string.Join(" | ", items)}] hit {hit} over \"{window.Replace("\n", "/")}\"");
        output.WriteLine($"battles {string.Join(",", battles)}; centres {string.Join(",", centres)}; battle frames {player.BattleFrames}");
        Assert.Empty(p.Unresolved.Concat(p.Battle.Unresolved).Distinct());
        Assert.Empty(player.Pickers);
        Assert.Empty(player.Answers["Cﾒｲｷｭｳ.ADV"]);
        Assert.Equal([PickerKind.Deployment], player.PickersSeen);
        Assert.Equal([(3, 83), (3, 83)], battles);                            // FIGT and 死亡捕獲 of a dungeon battle with the Sergeant Hanny
        Assert.True(player.BattleFrames > 0, "the battle was drawn");
        Assert.True(sceneShown > 0, "our battle screen's scene followed the battle");
        Assert.True(rounds > 0, "the round order saw the round header");
        Assert.Equal(("Dungeon Battle", false), headers[0]);                 // the battle's opening (docs/ui/battle-screen.md, "The opening")
        Assert.All(headers.Skip(1), h => Assert.Equal((null, true), h));       // the later rounds' headers, answered at once on our screen
        output.WriteLine($"scene shown at {sceneShown} events; {stageDrawnInBattle} G in the battle, {splitOverStage} with the split on");
        Assert.Equal(0, splitOverStage);
        Assert.Contains(1031, centres);                                      // the cave's picture
        Assert.Equal(7, menus.Count);
        Assert.Empty(menus[4].Items);                                         // the empty ]: a box with nothing in it,
        Assert.Equal(-1, menus[4].Hit);                                       // nothing to hit
        Assert.All(menus.Where(m => m.Items.Length > 0).SelectMany(m => m.Items), t => Assert.NotEmpty(t.Trim()));
        string name = new([.. Hankaku.ToHalfWidth(s.Strings[8]).Where(c => !char.IsWhiteSpace(c))]);
        Assert.Contains(name + "2F", menus[2].Window);                        // the header under the floor menu (the drawn characters)
        Assert.Equal(2, s.Dgns[2].SavedFloor.Value);
        Assert.Equal(1, s.Dgns[2].State.Value);
        Assert.Equal(0, s.Bt_Kind.Value);
        Assert.False(p.InBattle);
        Assert.Null(p.Picker);
    }

    /// <summary>The number of distinct colours in a rectangle of <paramref name="screen"/> (every other pixel).</summary>
    private static int Colours(Kichikuou.Content.Screen.Surface screen, int x, int y, int w, int h)
    {
        var seen = new HashSet<(byte, byte, byte, byte)>();
        for (int py = y; py < y + h; py += 2)
            for (int px = x; px < x + w; px += 2)
                seen.Add(screen.PixelAt(px, py));
        return seen.Count;
    }

    /// <summary>
    /// <c>ｵｰﾌﾟﾆﾝｸﾞ</c> run on the booted game: its drawing commands run on the stage (CGs off screen,
    /// copies onto the screen, fills), its texts resolve, and the screen shows pictures.
    /// </summary>
    [ScenarioFact]
    public async Task The_opening_draws_on_the_stage()
    {
        var run = await RunAsync(stopAtWeek: 0);

        await run.Session.Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");

        var p = run.Presenter;
        output.WriteLine($"composed {run.Player.Composed}, lit frames {run.Player.LitFrames}");
        Assert.Empty(p.Unresolved);
        Assert.True(run.Player.LitFrames > 10, "the opening's pictures reached the screen");
    }

    /// <summary>
    /// <c>ｵｰﾌﾟﾆﾝｸﾞ</c> skipped whole by the message skip, its stage commands dropped as a UI drops them
    /// (<see cref="GamePresenter.IsSkippedStageCommand"/>), then <c>Iﾊﾝﾗﾝ</c>: the wedding's picture (<c>G 105</c>,
    /// <c>:1146</c>, the scene's first <c>G</c>) is on the screen at the priest's line (<c>:1182</c>). The opening's last
    /// <c>J0 0, 400</c> (<c>:1407</c>) was kept while its <c>G</c> was dropped and placed the wedding below the screen, its
    /// palette over the map. Skipped without the player's files.
    /// </summary>
    [ScenarioFact]
    public async Task The_rebellion_draws_the_wedding_after_a_skipped_opening()
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var cgs = new CgLibrary(LocalData.CgDirectory);
        var p = new GamePresenter(new ScenarioText(LocalData.ScenarioDirectory), cgs, new BoxGlyphs(), scripts);
        var player = new Player(state, p);
        var view = new SkippingView(player, p);
        var session = new GameSession(state, new QueuedRandom { DefaultToOne = true }, view, player, scripts, scripts,
            new MemorySaveStore(), new RecordingRules { State = state });
        p.Bind(state);
        await session.BootAsync();
        var wedding = cgs.Get(105)!;
        int? differ = null;
        player.OnShow = e =>
        {
            if (e is not MessageEvent { Text: { File: "Iﾊﾝﾗﾝ.ADV", Line: 1182 } })
                return;
            p.Compose();
            var rgba = wedding.ToRgba(null, p.Stage.Palette);
            differ = 0;
            for (int y = 0; y < wedding.Height; y += 7)
                for (int x = 0; x < wedding.Width; x += 7)
                {
                    var (r, g, b, _) = p.Screen.PixelAt(x, y);
                    int i = (y * wedding.Width + x) * 4;
                    if (r != rgba[i] || g != rgba[i + 1] || b != rgba[i + 2])
                        differ++;
                }
            throw new WeekOverException();
        };

        view.Skipping = true;
        await session.Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");
        view.Skipping = false;
        await Assert.ThrowsAsync<WeekOverException>(() => session.Script.CallFileAsync("Iﾊﾝﾗﾝ.ADV"));

        Assert.Equal(0, differ);
    }

    /// <summary>
    /// A UI's skip of a whole scene (<c>GameScreen.ShowAsync</c>): while <see cref="Skipping"/>, the stage commands
    /// are dropped; the first event after them clears the stage (<c>EC 0</c>) before it is shown.
    /// </summary>
    private sealed class SkippingView(IScriptView inner, GamePresenter presenter) : IScriptView
    {
        private bool _dropped;

        public bool Skipping { get; set; }

        public Task ShowAsync(ScriptEvent e)
        {
            if (Skipping && GamePresenter.IsSkippedStageCommand(e))
            {
                _dropped = true;
                return Task.CompletedTask;
            }
            if (!Skipping && _dropped)
            {
                _dropped = false;
                presenter.Show(new EngineEvent("EC", [Sys3Int.Zero]));
            }
            return inner.ShowAsync(e);
        }
    }

    /// <summary>
    /// <c>OVER</c> run on the booted game for ending 13, a second clear (milestone 6): the ending's
    /// picture, the staff roll's first frame (skipped by a key held through the pauses, <c>:79-81</c>:
    /// its 1200 steps would compose 1200 screens) and the Completion Report draw on the stage, the
    /// report's texts resolve in the window and each entry's text is wiped by the restored text area
    /// (<c>:2208</c>), and with <see cref="GamePresenter.MaskEventCgs"/> the ending's picture is
    /// masked (it is not drawn in the full-screen window) — without the window's frame, which the
    /// picture covers (<c>:29-34</c>) — while the report is not.
    /// </summary>
    [ScenarioFact]
    public async Task The_ending_draws_on_the_stage_with_its_picture_masked_for_the_agent()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var p = run.Presenter;
        p.MaskEventCgs = true;
        var grey = ((byte)96, (byte)96, (byte)96, (byte)255);
        (byte, byte, byte, byte)? atEnding = null, frame = null, atReport = null;
        var texts = new List<string>();
        run.Player.OnShow = e =>
        {
            if (e is PauseEvent { Ticks.Value: 360 })                   // :45, the ending's picture shown
            {
                atEnding = ScreenAt(p, 320, 100);
                frame = p.Screen.PixelAt(140, 280);                     // window 113's frame (CG 809) would be here
            }
            if (e is KeyWaitEvent && !p.WindowText.IsEmpty)
            {
                atReport ??= ScreenAt(p, 320, 100);                     // the report's first entry
                texts.Add(p.WindowText.Text);
            }
        };
        var s = run.Session.State;
        s.EndingId = 13;
        s.Fates[79].Fate = 1;
        s.F_Sill_Fate = 1;
        s.Units[2].Status = 1;
        run.Player.PauseKey = 1;

        await Assert.ThrowsAsync<GameQuitException>(() => run.Session.Script.CallFileAsync("OVER.ADV"));

        output.WriteLine($"composed {run.Player.Composed}, window texts {run.Player.WindowTexts}");
        output.WriteLine(string.Join(" | ", texts));
        Assert.Empty(p.Unresolved);
        Assert.Equal(grey, atEnding);
        Assert.Equal(grey, frame);
        Assert.NotNull(atReport);
        Assert.NotEqual(grey, atReport);
        Assert.Contains(run.Player.Shown, e => e is EngineEvent { Command: "G" } g && g.Args[0].Value == 857);
        Assert.True(texts.Count > 3, "the report's entries reached the window");
        Assert.Equal("CompletionReport", texts[0]);                     // :201 (the window keeps no spaces)
        Assert.StartsWith("SillPlain", texts[1], StringComparison.Ordinal);
        Assert.All(texts.Skip(2), t => Assert.DoesNotContain("SillPlain", t, StringComparison.Ordinal));
        Assert.Equal(2, s.Fates[79].Fate.Value);
    }

    /// <summary>
    /// The title (milestone 6 step 2) through <see cref="GameSession.TitleAsync"/>: the logo, then
    /// <c>ｽﾀMENU</c> draws on the stage — the background (CG 301) and the buttons from the off-screen
    /// sheet (CG 303 at (640, 400)) composed at (640, 0) and copied onto the screen — and a click on
    /// "Start" begins the game: the prologue scene, then <c>MAIN</c> up to week 9's autosave.
    /// </summary>
    [ScenarioFact]
    public async Task The_title_draws_on_the_stage_and_Start_begins_the_game()
    {
        var run = await RunAsync(stopAtWeek: 9);
        var p = run.Presenter;
        bool clicked = false;
        (byte, byte, byte, byte)[]? screen = null, expected = null;
        (int X, int Y)[] points = [(10, 200), (600, 50), (347, 331), (120, 77)];
        run.Player.OnRead = () =>
        {
            if (clicked || !run.Player.Shown.Any(e => e is MusicEvent { Track.Value: 27 }))
                return;                                                 // the first read after SS 27 is the title's (:97)
            clicked = true;
            screen = [.. points.Select(pt => p.Screen.PixelAt(pt.X, pt.Y))];
            expected = [.. points.Select(pt => p.Stage.PixelAt(640 + pt.X, pt.Y))];
            Assert.Equal(p.Stage.PixelAt(640 + 347, 400 + 331), p.Screen.PixelAt(347, 331));   // button 6 as CG 303 has it
            run.Player.Click(347, 331);
        };

        await Assert.ThrowsAsync<WeekOverException>(() => run.Session.TitleAsync());

        Assert.True(clicked, "the title waited for the mouse");
        Assert.Equal(expected, screen);
        Assert.Contains(screen!, px => px != ((byte)0, (byte)0, (byte)0, (byte)255));
        Assert.Empty(p.Unresolved);
        Assert.Contains(run.Player.Shown, e => e is EngineEvent { Command: "G" } g && g.Args[0].Value == 1700);
        Assert.Equal(9, run.Session.State.Week.Value);
    }

    /// <summary>
    /// Fortune Check (<c>HAPPY_C</c>, milestone 6 step 3) from the title, on the stage: page 1 (CG 837
    /// at (640, 0)) with Menad's stamp for both halves (CG 841's (0, 400), copied to (1014, 78)) shown,
    /// a hovered cell lit by <c>CB</c>, and her detail composed at (0, 400) — the sheet (CG 839), the
    /// portrait, the six lines — then shown whole; a right click goes back to the title. The
    /// portraits are not event CGs: <see cref="GamePresenter.MaskEventCgs"/> leaves the screen as drawn.
    /// </summary>
    [ScenarioFact]
    public async Task Fortune_Check_draws_its_pages_and_a_detail_on_the_stage()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var p = run.Presenter;
        p.MaskEventCgs = true;
        var s = run.Session.State;
        s.Fates[79].Fate = 1;                                           // a clear: the button is there
        s.Fates[7].Fate = 100;                                          // Menad, both halves
        int phase = 0;
        (byte, byte, byte, byte)[]? page = null, pageExpected = null, detail = null, detailExpected = null;
        (byte, byte, byte, byte)? stamp = null, stampExpected = null, lit = null, unlit = null;
        int textDiffers = 0;
        (int X, int Y)[] pagePoints = [(5, 5), (89, 27), (300, 200), (491, 379), (600, 390)];
        (int X, int Y)[] detailPoints = [(40, 60), (120, 150), (300, 100), (400, 300), (620, 380)];
        run.Player.OnShow = e =>
        {
            if (phase != 3 || e is not KeyWaitEvent)
                return;
            ScreenAt(p, 0, 0);
            detail = [.. detailPoints.Select(pt => p.Screen.PixelAt(pt.X, pt.Y))];
            detailExpected = [.. detailPoints.Select(pt => p.Stage.PixelAt(pt.X, 400 + pt.Y))];
            for (int x = 246; x < 500; x++)                             // the first line's text over the clean sheet
                for (int y = 92; y < 112; y++)
                    if (p.Screen.PixelAt(x, y) != p.Stage.PixelAt(640 + x, 400 + y))
                        textDiffers++;
            phase = 4;
        };
        run.Player.OnRead = () =>
        {
            var shown = run.Player.Shown;
            switch (phase)
            {
                case 0 when shown.Any(e => e is MusicEvent { Track.Value: 27 }):
                    run.Player.Click(120, 77);                          // Fortune Check (ｽﾀMENU.ADV:805, button 0)
                    phase = 1;
                    break;
                case 1 when shown.Any(e => e is EngineEvent { Command: "G" } g && g.Args[0].Value == 839):
                    page = [.. pagePoints.Select(pt => p.Screen.PixelAt(pt.X, pt.Y))];
                    pageExpected = [.. pagePoints.Select(pt => p.Stage.PixelAt(640 + pt.X, pt.Y))];
                    stamp = p.Screen.PixelAt(1014 - 640 + 20, 78 + 10);
                    stampExpected = p.Stage.PixelAt(20, 410);
                    unlit = p.Screen.PixelAt(223, 87);
                    run.Player.Hover(297, 91);                          // Menad's cell (fate 7)
                    phase = 2;
                    break;
                case 2 when run.Player.Pending == 0 && run.Player.Last.X.Value == 297:
                    lit = p.Screen.PixelAt(223, 87);                    // the CB at (223, 75), 148 × 25
                    run.Player.Click(297, 91);
                    phase = 3;
                    break;
                case 4:
                    run.Player.RightClick(300, 200);
                    phase = 5;
                    break;
                case 5 when shown.Count(e => e is EngineEvent { Command: "G" } g && g.Args[0].Value == 301) == 2:
                    throw new BackAtTitleException();
            }
        };

        await Assert.ThrowsAsync<BackAtTitleException>(() => run.Session.TitleAsync());

        Assert.Equal(5, phase);
        Assert.Equal(pageExpected, page);
        Assert.Contains(page!, px => px != ((byte)0, (byte)0, (byte)0, (byte)255));
        Assert.Equal(stampExpected, stamp);
        Assert.NotEqual(unlit, lit);
        Assert.Equal(detailExpected, detail);
        Assert.True(textDiffers > 20, $"the text reached the detail ({textDiffers} pixels)");
        Assert.Contains(run.Player.Shown, e => e is EngineEvent { Command: "G" } g && g.Args[0].Value == 8);   // Menad's portrait
        Assert.Empty(p.Unresolved);
    }

    /// <summary>
    /// The gallery (<c>CG</c>, milestone 7 step 2) from the title, on the stage. Its sheets are reduced
    /// event pictures: with <see cref="GamePresenter.MaskEventCgs"/> the stage is masked from the
    /// sheets (CG 844-848) until the title's background (CG 301) comes back. Page 3's sheet sits in the
    /// map's tile area (CG 846 at (1280, 0), survey-gallery §4.3): composing the screen does not draw
    /// the map over it, so the tab shows the sheet as it was loaded.
    /// </summary>
    [ScenarioFact]
    public async Task The_gallery_draws_its_pages_on_the_stage_masked_for_the_agent()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var p = run.Presenter;
        run.Session.State.Fates[79].Fate = 1;                           // a clear: the button is there
        var grey = ((byte)96, (byte)96, (byte)96, (byte)255);
        int phase = 0;
        (byte, byte, byte, byte)? masked = null, title = null;
        (byte, byte, byte, byte)[]? sheet = null, page = null;
        (int X, int Y)[] points = [(5, 5), (60, 70), (300, 200), (500, 300), (620, 390)];
        static bool IsG(ScriptEvent e, int cg) => e is EngineEvent { Command: "G" } g && g.Args[0].Value == cg;
        run.Player.OnRead = () =>
        {
            var shown = run.Player.Shown;
            switch (phase)
            {
                case 0 when shown.Any(e => e is MusicEvent { Track.Value: 27 }):
                    run.Player.Click(551, 319);                         // the gallery (ｽﾀMENU.ADV:805, button 7)
                    phase = 1;
                    break;
                case 1 when shown.Any(e => IsG(e, 848)):
                    p.MaskEventCgs = true;
                    masked = ScreenAt(p, 320, 200);
                    p.MaskEventCgs = false;
                    sheet = [.. points.Select(pt => p.Stage.PixelAt(1280 + pt.X, pt.Y))];
                    run.Player.Hover(168, 14);                          // tab 3 (CG.ADV:186-190)
                    run.Player.Click(168, 14);
                    phase = 2;
                    break;
                case 2 when shown.Any(e => e is EngineEvent { Command: "CC" } c && c.Args[0].Value == 1280 && c.Args[1].Value == 0):
                    ScreenAt(p, 0, 0);
                    page = [.. points.Select(pt => p.Screen.PixelAt(pt.X, pt.Y))];
                    run.Player.Hover(595, 12);                          // Exit
                    run.Player.Click(595, 12);
                    phase = 3;
                    break;
                case 3 when shown.Count(e => IsG(e, 301)) == 2:
                    p.MaskEventCgs = true;
                    title = ScreenAt(p, 320, 200);
                    throw new BackAtTitleException();
            }
        };

        await Assert.ThrowsAsync<BackAtTitleException>(() => run.Session.TitleAsync());

        Assert.Equal(grey, masked);
        Assert.Equal(sheet, page);
        Assert.Contains(page!, px => px != ((byte)0, (byte)0, (byte)0, (byte)255));
        Assert.NotEqual(grey, title);
        Assert.Empty(p.Unresolved);
    }

    /// <summary>
    /// The music room (<c>MMODE</c>, milestone 7 step 3) from the title, on the stage: its screen (CG
    /// 693 and the 28 titles written over it) differs from the title's, its texts resolve, a title
    /// played starts its track, and End puts the title back — silent (STR-37). No event CG: the mask
    /// leaves it as drawn.
    /// </summary>
    [ScenarioFact]
    public async Task The_music_room_draws_on_the_stage_and_End_puts_the_title_back()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var p = run.Presenter;
        p.MaskEventCgs = true;
        run.Session.State.Fates[79].Fate = 1;                           // a clear: the button is there
        var grey = ((byte)96, (byte)96, (byte)96, (byte)255);
        int phase = 0;
        (int X, int Y)[] points = [(60, 60), (382, 34), (490, 200), (598, 354), (300, 380)];
        (byte, byte, byte, byte)[]? title = null, room = null, back = null;
        static bool IsG(ScriptEvent e, int cg) => e is EngineEvent { Command: "G" } g && g.Args[0].Value == cg;
        (byte, byte, byte, byte)[] Screen()
        {
            ScreenAt(p, 0, 0);
            return [.. points.Select(pt => p.Screen.PixelAt(pt.X, pt.Y))];
        }
        run.Player.OnRead = () =>
        {
            var shown = run.Player.Shown;
            switch (phase)
            {
                case 0 when shown.Any(e => e is MusicEvent { Track.Value: 27 }):
                    title = Screen();
                    run.Player.Click(341, 63);                          // the music room (ｽﾀMENU.ADV:805, button 1)
                    phase = 1;
                    break;
                case 1 when shown.Any(e => IsG(e, 693)):
                    room = Screen();
                    run.Player.Click(382, 34);                          // the first title
                    run.Player.Click(598, 354);                         // End
                    phase = 2;
                    break;
                case 2 when shown.Count(e => IsG(e, 301)) == 2:
                    back = Screen();
                    throw new BackAtTitleException();
            }
        };

        await Assert.ThrowsAsync<BackAtTitleException>(() => run.Session.TitleAsync());

        Assert.NotEqual(title, room);
        Assert.DoesNotContain(grey, room!);
        Assert.Equal(title, back);
        int played = run.Player.Shown.FindIndex(e => e is MusicEvent { Track.Value: 1 });
        Assert.True(played >= 0);
        Assert.DoesNotContain(run.Player.Shown.Skip(played), e => e is MusicEvent { Track.Value: 27 });
        Assert.Empty(p.Unresolved);
    }

    private sealed class BackAtTitleException : Exception;

    private static (byte, byte, byte, byte) ScreenAt(GamePresenter p, int x, int y)
    {
        p.Flush();
        p.Invalidate();
        p.Compose();
        return p.Screen.PixelAt(x, y);
    }

    /// <summary>
    /// <see cref="GamePresenter.MaskEventCgs"/>: while the full-screen window is open the stage is a
    /// grey placeholder; off (the default) and with any other window the stage shows as drawn.
    /// </summary>
    [Fact]
    public void The_debug_mask_hides_the_stage_under_the_full_screen_window()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState();
        presenter.Bind(state);
        presenter.Show(new EngineEvent("CF", [(Sys3Int)0, (Sys3Int)0, (Sys3Int)640, (Sys3Int)400, (Sys3Int)15]));
        var drawn = presenter.Stage.PixelAt(320, 100);

        state.Ui_WinType = 31;
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(drawn, presenter.Screen.PixelAt(320, 100));                // off by default

        presenter.MaskEventCgs = true;
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(((byte)96, (byte)96, (byte)96, (byte)255), presenter.Screen.PixelAt(320, 100));

        state.Ui_WinType = 11;
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(drawn, presenter.Screen.PixelAt(320, 100));
    }

    /// <summary>
    /// A scene a battle file calls draws on the battle's surface: its raw drawing reaches the stage
    /// while the battle's presenter shows the events, and the debug mask hides the stage under its
    /// full-screen window there too.
    /// </summary>
    [Fact]
    public void A_scene_inside_a_battle_draws_on_the_stage_and_is_masked()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState();
        presenter.Bind(state);
        presenter.Show(new BattleScreenEvent("死亡捕獲.ADV", true));
        presenter.Show(new EngineEvent("CF", [(Sys3Int)0, (Sys3Int)0, (Sys3Int)640, (Sys3Int)400, (Sys3Int)15]));
        var drawn = presenter.Stage.PixelAt(320, 100);
        Assert.NotEqual((0, 0, 0, 255), drawn);
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(drawn, presenter.Screen.PixelAt(320, 100));

        state.Ui_WinType = 31;
        presenter.MaskEventCgs = true;
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(((byte)96, (byte)96, (byte)96, (byte)255), presenter.Screen.PixelAt(320, 100));
    }

    /// <summary>
    /// <see cref="GamePresenter.MapViewShown"/> is the view on the screen: the map shown at the camera
    /// centred, a drag's step at the corner it copies from — and unchanged when a still click's release
    /// centres the camera again (<c>COMMAND.ADV:94-98</c>) without drawing.
    /// </summary>
    [Fact]
    public void The_map_view_shown_follows_the_drawing_not_the_camera()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState { MapCamX = 700, MapCamY = 300 };
        presenter.Bind(state);
        Assert.Null(presenter.MapViewShown);
        presenter.Show(new MapEvent(0));
        Assert.Equal((380, 100), presenter.MapViewShown);

        presenter.Show(new MapScreenEvent(true));
        state.MapCamX = 380;                                                    // the release wait: the corner
        state.MapCamY = 100;
        state.MapCamX = 420;                                                    // a drag's step
        presenter.Show(new EngineEvent("CC", [(Sys3Int)(420 + 1280), (Sys3Int)100, (Sys3Int)640, (Sys3Int)400, Sys3Int.Zero, Sys3Int.Zero]));
        Assert.Equal((420, 100), presenter.MapViewShown);

        state.MapCamX = 740;                                                    // a still click's release: centred, nothing drawn
        state.MapCamY = 300;
        Assert.Equal((420, 100), presenter.MapViewShown);
        presenter.Show(new EngineEvent("CC", [(Sys3Int)1280, Sys3Int.Zero, (Sys3Int)32, (Sys3Int)32, (Sys3Int)8, (Sys3Int)8]));
        Assert.Equal((420, 100), presenter.MapViewShown);                       // not a view of the map
    }

    /// <summary>
    /// A view that does not show the screen takes its changes without composing (<see cref="GamePresenter.TakeChange"/>,
    /// once per change); the next <see cref="GamePresenter.Compose"/> draws what changed meanwhile.
    /// </summary>
    [Fact]
    public void A_change_taken_without_composing_is_drawn_by_the_next_compose()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        presenter.Bind(new GameState());
        presenter.Show(new BattleScreenEvent("FIGT_A.ADV", true));
        presenter.Compose();
        Assert.False(presenter.TakeChange());

        presenter.Show(new EngineEvent("CF", [(Sys3Int)0, (Sys3Int)0, (Sys3Int)640, (Sys3Int)400, (Sys3Int)15]));
        var drawn = presenter.Stage.PixelAt(320, 100);
        Assert.True(presenter.TakeChange());
        Assert.False(presenter.TakeChange());
        Assert.NotEqual(drawn, presenter.Screen.PixelAt(320, 100));

        Assert.True(presenter.Compose());
        Assert.Equal(drawn, presenter.Screen.PixelAt(320, 100));
        Assert.False(presenter.Compose());
    }

    /// <summary>
    /// With our province menu (<see cref="GamePresenter.CoverCityLeadIn"/>) a city file's lead-in is covered
    /// from its picture (<c>WAO_SUB</c> 331 of the city's <c>Prov.BgCg</c>) through the window 113 and its text
    /// to the menu, so the release wait before the menu shows nothing of the original's; the menu ends it,
    /// and so does anything else the file shows (a key wait). After our menu, the way out (the pointer put back,
    /// the window closed, the picture removed) is covered up to the map. Another picture, or no cover asked,
    /// starts nothing.
    /// </summary>
    [Fact]
    public void A_city_files_lead_in_is_covered_up_to_the_menu()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState { Prov_Cur = 6 };
        state.Provs[6].BgCg = 340;
        presenter.Bind(state);
        presenter.CoverCityLeadIn = true;

        presenter.Show(new WindowEvent(331, 340));
        Assert.True(presenter.MenuCovered);
        presenter.Show(new WindowEvent(113, 340));
        presenter.Show(new MessageEvent(new ScriptRef("Cﾃｷﾄｼ.ADV", 20)));
        presenter.Show(new LineBreakEvent());
        Assert.True(presenter.MenuCovered);
        presenter.OpenMenu([]);
        Assert.False(presenter.MenuCovered);                            // the UI covers the menu itself
        presenter.MenuCovered = true;

        presenter.CloseMenu();                                                  // a cancel: the lead-out stays covered
        Assert.True(presenter.MenuCovered);
        presenter.Show(new MessageClearEvent());
        presenter.Show(new EngineEvent("IZ", [(Sys3Int)320, (Sys3Int)200]));
        presenter.Show(new WindowEvent(0, 340));
        presenter.Show(new WindowEvent(332, 340));
        Assert.True(presenter.MenuCovered);
        presenter.Show(new MapEvent(0));                                        // back on the map
        Assert.False(presenter.MenuCovered);

        presenter.OpenMenu([]);                                                 // another menu: nothing covered after it
        presenter.CloseMenu();
        Assert.False(presenter.MenuCovered);

        presenter.Show(new WindowEvent(331, 340));
        presenter.Show(new WindowEvent(1, 340));                                // a key wait: shown
        Assert.False(presenter.MenuCovered);

        presenter.Show(new WindowEvent(331, 341));                              // not the city's picture
        Assert.False(presenter.MenuCovered);
        state.Ev_CommandMode = 1;                                               // a forced trip takes over
        presenter.Show(new WindowEvent(331, 340));
        Assert.False(presenter.MenuCovered);
        state.Ev_CommandMode = 0;
        presenter.CoverCityLeadIn = false;
        presenter.Show(new WindowEvent(331, 340));
        Assert.False(presenter.MenuCovered);

        presenter.CoverCityLeadIn = true;
        presenter.Show(new WindowEvent(331, 340));
        presenter.EndCityLeadIn();                                              // GameScreen's key waits and pauses
        Assert.False(presenter.MenuCovered);
    }

    /// <summary>
    /// With <see cref="GamePresenter.CoverBriefings"/> <c>Cﾃｷﾄｼ</c>'s briefing after our province menu's Attack is played
    /// unseen: the lead-out stays covered through Rance's portrait and window, his text starts the briefing, which goes on
    /// through its key waits, Maris's portrait and her two lines and ends at what else the file shows — the window, the
    /// pictures and the log as ever then. A refusal after Rance's line ends it at its own text; no cover asked, no briefing.
    /// </summary>
    [Fact]
    public void An_attacks_briefing_is_played_unseen()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState { Prov_Cur = 6, Ui_WinType = 21, Ui_WinFrameCg = 810 };   // the window the script keeps open
        state.Provs[6].BgCg = 340;
        presenter.Bind(state);
        presenter.CoverCityLeadIn = true;
        presenter.CoverBriefings = true;
        presenter.CoverMessages = true;

        void Attack()
        {
            presenter.Show(new WindowEvent(331, 340));
            presenter.Show(new WindowEvent(113, 340));
            presenter.OpenMenu([]);
            presenter.MenuCovered = true;
            presenter.CloseMenu();                                              // Attack chosen on our menu
            presenter.Show(new WindowEvent(0, 340));
            presenter.Show(new WindowEvent(311, 1));                            // Rance
            presenter.Show(new WindowEvent(111, 1));
            Assert.True(presenter.MenuCovered);
            Assert.False(presenter.Briefing);
            presenter.Show(new MessageEvent(new ScriptRef("Cﾃｷﾄｼ.ADV", 139)));
            Assert.True(presenter.Briefing);
            presenter.Show(new WindowEvent(1, 1));                              // the key wait, answered by the UI
            presenter.EndCityLeadIn();
            Assert.False(presenter.MenuCovered);
            Assert.True(presenter.Briefing);
            Assert.Null(presenter.MessageWindow);
            presenter.AfterKey();
            presenter.Show(new WindowEvent(0, 1));
            presenter.Show(new WindowEvent(321, 6));                            // Maris
            presenter.Show(new WindowEvent(121, 6));
        }

        Attack();
        presenter.Show(new MessageEvent(new ScriptRef("Cﾃｷﾄｼ.ADV", 162)));
        presenter.Show(new WindowEvent(1, 6));
        presenter.AfterKey();
        presenter.Show(new MessageEvent(new ScriptRef("Cﾃｷﾄｼ.ADV", 165)));
        presenter.Show(new WindowEvent(1, 6));
        presenter.AfterKey();
        presenter.Show(new WindowEvent(0, 6));
        Assert.True(presenter.Briefing);
        Assert.Empty(presenter.Log.Pages);
        presenter.Show(new WindowEvent(322, 6));                                // on to the picker
        Assert.False(presenter.Briefing);

        Attack();
        presenter.Show(new MessageEvent(new ScriptRef("Cﾃｷﾄｼ.ADV", 3833)));      // the rebels' first week: Maris refuses
        Assert.False(presenter.Briefing);
        Assert.NotNull(presenter.MessageWindow);

        presenter.Show(new WindowEvent(0, 6));
        presenter.CoverBriefings = false;
        presenter.Show(new WindowEvent(111, 1));
        presenter.Show(new MessageEvent(new ScriptRef("Cﾃｷﾄｼ.ADV", 139)));
        Assert.False(presenter.Briefing);
    }

    /// <summary>
    /// With <see cref="GamePresenter.CoverBriefings"/> <c>Cﾒｲｷｭｳ</c>'s briefing after a dungeon's Start Search is played unseen:
    /// Rance's text starts it, which goes on through its key waits, Maris's portrait and her line about the unit and ends at
    /// the picker. Maris's advice on the snow ends it and is shown; her line about the unit after it starts it again.
    /// </summary>
    [Fact]
    public void A_searchs_briefing_is_played_unseen()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        presenter.Bind(new GameState { Dgn_Cur = 4, Ui_WinType = 21, Ui_WinFrameCg = 810 });
        presenter.CoverBriefings = true;
        presenter.CoverMessages = true;

        void Search()
        {
            presenter.OpenMenu([]);
            presenter.CloseMenu();                                              // Start Search
            presenter.Show(new WindowEvent(0, 1));
            presenter.Show(new WindowEvent(311, 1));                            // Rance
            presenter.Show(new WindowEvent(111, 1));
            Assert.False(presenter.Briefing);
            presenter.Show(new MessageEvent(new ScriptRef("Cﾒｲｷｭｳ.ADV", 312)));
            Assert.True(presenter.Briefing);
            Assert.Null(presenter.MessageWindow);
            presenter.Show(new WindowEvent(1, 1));                              // the key wait, answered by the UI
            presenter.AfterKey();
            presenter.Show(new WindowEvent(0, 1));
            presenter.Show(new WindowEvent(321, 6));                            // Maris
        }

        void UnitLine()
        {
            presenter.Show(new WindowEvent(121, 6));
            presenter.Show(new MessageEvent(new ScriptRef("Cﾒｲｷｭｳ.ADV", 324)));
            Assert.True(presenter.Briefing);
            Assert.Null(presenter.MessageWindow);
            presenter.Show(new WindowEvent(1, 6));
            presenter.AfterKey();
            presenter.Show(new WindowEvent(0, 6));
            Assert.True(presenter.Briefing);
            presenter.Show(new EngineEvent("B3", [8, 8, 274, 296, 120, 0]));    // Sﾌﾞﾀｲ
            Assert.False(presenter.Briefing);
        }

        Search();
        UnitLine();
        Assert.Empty(presenter.Log.Pages);

        Search();
        presenter.Show(new WindowEvent(121, 6));
        presenter.Show(new MessageEvent(new ScriptRef("Cﾒｲｷｭｳ.ADV", 5377)));     // no Snow Charm: "are you sure about this?"
        Assert.False(presenter.Briefing);
        Assert.NotNull(presenter.MessageWindow);
        presenter.Show(new WindowEvent(1, 6));
        presenter.AfterKey();
        presenter.Show(new WindowEvent(0, 6));
        UnitLine();
    }

    /// <summary>
    /// <c>Gｸﾘｰﾑ</c> 1, which <c>死亡捕獲</c> calls (<c>Battle.Flow.DeathCapture.cs</c>), run from the
    /// player's files inside the battle's screen: its pictures (<c>G 806</c>, <c>G 128</c>) reach the
    /// stage, its text resolves, and with the debug mask every frame under its full-screen window is the
    /// grey placeholder. Skipped without the player's files.
    /// </summary>
    [ScenarioFact]
    public async Task A_death_scene_draws_its_pictures_over_the_battle()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var p = run.Presenter;
        var s = run.Session.State;
        p.MaskEventCgs = true;
        int drawn = 0, version = 0, masked = 0, full = 0;
        run.Player.OnShow = e =>
        {
            if (e is EngineEvent && p.Stage.ScreenVersion != version)
                drawn++;
            version = p.Stage.ScreenVersion;
            if (!p.InBattle || s.Ui_WinType != 31 || p.FadeAmount != 0)        // faded out, the placeholder is dark too
                return;
            full++;
            if (ScreenAt(p, 320, 200) == ((byte)96, (byte)96, (byte)96, (byte)255))
                masked++;
        };
        await run.Session.Script.ShowAsync(new BattleScreenEvent("死亡捕獲.ADV", true));
        version = p.Stage.ScreenVersion;

        await run.Session.Script.EventAsync("Gｸﾘｰﾑ.ADV", 1);

        Assert.True(p.InBattle);
        Assert.True(drawn >= 2, $"{drawn} drawing commands reached the stage");
        Assert.True(full > 0, "the full-screen window opened");
        Assert.Equal(full, masked);
        Assert.Empty(p.Unresolved.Concat(p.Battle.Unresolved).Distinct());
    }

    /// <summary>
    /// The scenes <c>死亡捕獲</c> calls (<c>Battle.Flow.DeathCapture.cs</c>) — <c>Gｸﾘｰﾑ</c> 1's death, all narration, and
    /// <c>Gﾒﾙﾌｪｲｽ</c> 1's, speeches and a question — log the same pages inside the battle's screen as they do as scenes of
    /// their own: their dialogue is a scene's, off the field. Skipped without the player's files.
    /// </summary>
    [ScenarioTheory]
    [InlineData("Gｸﾘｰﾑ.ADV")]
    [InlineData("Gﾒﾙﾌｪｲｽ.ADV")]
    public async Task A_scene_the_battle_called_is_logged_as_any_scene(string file)
    {
        async Task<IReadOnlyList<LogPage>> LoggedAsync(bool inBattle)
        {
            var run = await RunAsync(stopAtWeek: 0);
            int waits = 0;
            run.Player.OnShow = e => waits += e is WindowEvent { Op.Value: 1 or 5 or 11 } && run.Session.State.Ui_WinType != 0 ? 1 : 0;
            int before = run.Presenter.Log.Pages.Count;
            if (inBattle)
                await run.Session.Script.ShowAsync(new BattleScreenEvent("死亡捕獲.ADV", true));
            await run.Session.Script.EventAsync(file, 1);
            var pages = run.Presenter.Log.Pages.Skip(before).ToList();
            output.WriteLine($"{(inBattle ? "inside 死亡捕獲" : "as a scene")}: {waits} key waits of a window, {pages.Count} pages logged");
            foreach (var page in pages.Where(p => p.Kind != LogKind.Narration).Take(3))
                output.WriteLine($"  {page.Kind} {page.Speaker}: {page.Lines[0]}");
            return pages;
        }

        var scene = await LoggedAsync(inBattle: false);
        var battle = await LoggedAsync(inBattle: true);

        Assert.NotEmpty(scene);
        Assert.Equal(scene.Select(p => (p.Kind, p.Speaker, string.Join("\n", p.Lines))),
            battle.Select(p => (p.Kind, p.Speaker, string.Join("\n", p.Lines))));
    }

    /// <summary>
    /// <c>Gﾒﾙﾌｪｲｽ</c> 1, which <c>死亡捕獲</c> calls when Melfeis (commander 4) falls, asks its question
    /// (<c>Gﾒﾙﾌｪｲｽ.ADV:2430-2440</c>) inside the battle's screen: the menu is drawn over it (it was
    /// left out in a battle, an invisible menu the player could still click) and is a window of our
    /// battle screen's layer. Skipped without the player's files.
    /// </summary>
    [ScenarioFact]
    public async Task A_menu_of_a_scene_the_battle_called_is_drawn_over_the_battle()
    {
        var run = await RunAsync(stopAtWeek: 0);
        var p = run.Presenter;
        var menus = new List<(string[] Items, int Changed, bool Masked)>();
        run.Player.OnMenu = () =>
        {
            var (x, y, w) = p.Menu!.Window!.Value;
            int changed = 0;
            for (int dx = 0; dx < w; dx++)
                if (p.Screen.PixelAt(x + dx, y + 2) != p.Stage.PixelAt(p.Stage.View.X + x + dx, p.Stage.View.Y + y + 2))
                    changed++;
            menus.Add(([.. p.MenuItems!], changed, p.BattleWindows().Contains(x + 2, y + 2)));
        };
        await run.Session.Script.ShowAsync(new BattleScreenEvent("死亡捕獲.ADV", true));

        await run.Session.Script.EventAsync("Gﾒﾙﾌｪｲｽ.ADV", 1);

        Assert.True(p.InBattle);
        var (items, changed, masked) = Assert.Single(menus);
        output.WriteLine($"[{string.Join(" | ", items)}]: {changed} pixels of its row drawn");
        Assert.Equal(2, items.Length);
        Assert.True(changed > 0, "the menu is drawn over the battle's screen");
        Assert.True(masked, "the menu is in our battle screen's windows' layer");
    }

    /// <summary>
    /// <c>WAO_SUB</c> 0 on the full-screen window restores nothing (<c>WAO_SUB.ADV:267-270</c>):
    /// after <c>Iﾄｳｿﾞｸ.ADV</c>'s last line (<c>:1839-1843</c>) the frame and the text stay on the
    /// screen while <c>ｵｰﾌﾟﾆﾝｸﾞ</c> waits for its music, until something draws over them.
    /// Skipped without the player's CGs.
    /// </summary>
    [LocalFolderFact("cg")]
    public void Closing_the_full_screen_window_leaves_it_on_the_screen()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(LocalData.CgDirectory), new BoxGlyphs());
        var state = new GameState();
        presenter.Bind(state);
        presenter.Show(new EngineEvent("CF", [(Sys3Int)0, (Sys3Int)0, (Sys3Int)640, (Sys3Int)400, (Sys3Int)15]));
        presenter.Show(new WindowEvent(131, 0));
        state.Ui_WinType = 31;
        state.Ui_WinFrameCg = 815;
        presenter.Show(new MessageEvent(new ScriptRef("Iﾄｳｿﾞｸ.ADV", 1839)));
        presenter.Invalidate();
        presenter.Compose();
        var shown = presenter.Screen.ReadScreen();

        presenter.Show(new WindowEvent(0, 0));
        state.Ui_WinType = 0;
        presenter.Invalidate();
        presenter.Compose();
        Assert.Equal(shown, presenter.Screen.ReadScreen());

        presenter.Show(new EngineEvent("CF", [(Sys3Int)0, (Sys3Int)0, (Sys3Int)640, (Sys3Int)400, (Sys3Int)0]));
        presenter.Compose();
        Assert.Equal((0, 0, 0, 255), presenter.Screen.PixelAt(320, 350));      // drawn over
    }

    /// <summary>
    /// <c>ｵｰﾌﾟﾆﾝｸﾞ.ADV:49-58</c>: <c>PC 1</c> draws a CG off screen without its palette, <c>PC 6</c>
    /// loads the palette without drawing, <c>CC</c> brings it on screen — the same picture as the
    /// CG drawn there with <c>PC 7</c>, though another CG's palette was loaded when it was drawn
    /// (<c>cg_fflg</c>: 1 draws, 4 loads). Skipped without the player's CGs.
    /// </summary>
    [LocalFolderFact("cg")]
    public void A_cg_drawn_without_its_palette_shows_in_the_palette_loaded_later()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        Sys3Int[] Args(params int[] values) => values.Select(v => (Sys3Int)v).ToArray();
        GamePresenter Presenter()
        {
            var p = new GamePresenter(new ScenarioText(missing), new CgLibrary(LocalData.CgDirectory), new BoxGlyphs());
            p.Bind(new GameState());
            return p;
        }
        byte[] Shown(GamePresenter p)
        {
            p.Invalidate();
            p.Compose();
            return p.Screen.ReadScreen();
        }

        var expected = Presenter();
        expected.Show(new EngineEvent("J0", Args(0, 0)));
        expected.Show(new EngineEvent("G", Args(1734)));

        var opening = Presenter();
        opening.Show(new EngineEvent("G", Args(1731)));                 // another palette (PC 7)
        opening.Show(new EngineEvent("CF", Args(0, 0, 1280, 400, 0)));
        opening.Show(new EngineEvent("J0", Args(640, 0)));
        opening.Show(new EngineEvent("PC", Args(1)));
        opening.Show(new EngineEvent("G", Args(1734)));
        opening.Show(new EngineEvent("PC", Args(7)));
        opening.Show(new EngineEvent("CC", Args(640, 0, 640, 400, 0, 0)));
        Assert.NotEqual(Shown(expected), Shown(opening));                 // still 1731's colours

        opening.Show(new EngineEvent("PC", Args(6)));
        opening.Show(new EngineEvent("G", Args(1734)));
        opening.Show(new EngineEvent("PC", Args(7)));
        Assert.Equal(Shown(expected), Shown(opening));
    }

    /// <summary>
    /// <c>ｵｰﾌﾟﾆﾝｸﾞ.ADV</c>'s zoom (<c>:226-302</c>, <c>CM</c> from the hidden page onto the screen)
    /// and its scroll (<c>:930-938</c>, <c>WV 0, y, 640, 400</c> and back) reach the stage and the screen.
    /// </summary>
    [Fact]
    public void The_openings_scaled_copies_and_view_reach_the_screen()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        presenter.Bind(new GameState());
        Sys3Int[] Args(params int[] values) => values.Select(v => (Sys3Int)v).ToArray();

        presenter.Show(new EngineEvent("CF", Args(640, 0, 640, 400, 15)));
        presenter.Show(new EngineEvent("CM", Args(640, 0, 480, 300, 0, 0, 640, 400, 0)));
        presenter.Compose();
        var white = presenter.Stage.PixelAt(640, 0);
        Assert.Equal(white, presenter.Screen.PixelAt(639, 399));

        presenter.Show(new EngineEvent("CF", Args(0, 400, 640, 400, 0)));
        presenter.Show(new EngineEvent("WV", Args(0, 400, 640, 400)));
        presenter.Compose();
        Assert.Equal(presenter.Stage.PixelAt(0, 400), presenter.Screen.PixelAt(0, 0));
        Assert.NotEqual(white, presenter.Screen.PixelAt(0, 0));

        presenter.Show(new EngineEvent("WV", Args(0, 0, 640, 400)));
        presenter.Compose();
        Assert.Equal(white, presenter.Screen.PixelAt(0, 0));
    }

    /// <summary>
    /// <c>PF</c>/<c>PW</c> last <c>rate * 16 * 1000 / 60</c> ms as xsystem35's <c>ags_fadeIn</c>/<c>ags_fadeOut</c>
    /// (<c>ags.c</c>), not <c>rate</c> × 16 ms: the scenes' <c>PF1 FadeSpeed</c> (4) takes 1066 ms.
    /// </summary>
    [Fact]
    public void A_fade_lasts_as_long_as_the_engines()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        presenter.Bind(new GameState());

        presenter.Show(new EngineEvent("PF1", [(Sys3Int)4]));
        var fadeOut = presenter.TakeFade();
        Assert.Equal(new FadeRequest(0, 255, false, 4), fadeOut);
        Assert.Equal(TimeSpan.FromMilliseconds(1066), fadeOut!.Duration);

        presenter.Show(new EngineEvent("PW0", [(Sys3Int)1]));
        Assert.Equal(TimeSpan.FromMilliseconds(266), presenter.TakeFade()!.Duration);
    }

    /// <summary>
    /// <c>Iｼﾚｲﾌﾞ.ADV:78-89</c>: <c>M.ADV</c> 1 scrolls the map with full-screen copies of the view,
    /// which wipe <c>WAO_SUB</c>'s pictures although their variables still hold them; only the
    /// right portrait is drawn again (<c>321</c>), so Rance and the centre picture stay gone.
    /// Skipped without the player's CGs.
    /// </summary>
    [LocalFolderFact("cg")]
    public void A_map_scroll_wipes_the_pictures_until_they_are_drawn_again()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(LocalData.CgDirectory), new BoxGlyphs());
        var state = new GameState { Ui_PortraitL = 1, Ui_PortraitR = 4, Ui_CenterCg = 1008 };
        presenter.Bind(state);
        bool Shown(int x, int y, int w, int h)
        {
            presenter.Compose();
            for (int py = y; py < y + h; py += 2)
                for (int px = x; px < x + w; px += 2)
                    if (presenter.Screen.PixelAt(px, py) != presenter.Stage.PixelAt(px, py))
                        return true;
            return false;
        }
        bool Left() => Shown(0, 0, 176, 256);
        bool Right() => Shown(448, 144, 192, 256);
        bool Centre() => Shown(200, 120, 64, 64);
        Assert.True(Left() && Right() && Centre());

        presenter.Show(new MapEvent(1));
        presenter.Show(new PauseEvent((Sys3Int)5));                      // a scroll step: the view over the whole screen
        Assert.False(Left() || Right() || Centre());

        presenter.Show(new WindowEvent((Sys3Int)321, (Sys3Int)4));       // Soul drawn again
        Assert.True(Right());
        Assert.False(Left() || Centre());

        presenter.Show(new WindowEvent((Sys3Int)311, (Sys3Int)1));       // a later 311 draws Rance again
        Assert.True(Left());
    }

    /// <summary>
    /// <c>M.ADV</c> 10 blinks the target mark after its selection sound (<c>PCMPLAY</c>): the sound
    /// does not end the animation, and the first pause draws the mark at (<c>Tmp0025</c>, <c>Tmp0026</c>).
    /// </summary>
    [Fact]
    public void The_map_mark_blinks_after_the_selection_sound()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState { Tmp0025 = 100, Tmp0026 = 120 };
        presenter.Bind(state);
        // The mark's cell off screen (CG 814 draws there; the player's files are not needed here).
        presenter.Show(new EngineEvent("CF", [(Sys3Int)640, Sys3Int.Zero, (Sys3Int)32, (Sys3Int)32, (Sys3Int)15]));
        var mark = presenter.Stage.PixelAt(640, 0);
        Assert.NotEqual(mark, presenter.Stage.PixelAt(110, 130));

        presenter.Show(new MapEvent(10));
        presenter.Show(new SoundEvent(Sys3Int.Zero, (Sys3Int)109));
        presenter.Show(new PauseEvent((Sys3Int)8));

        Assert.Equal(mark, presenter.Stage.PixelAt(110, 130));
    }

    /// <summary>
    /// The same blink inside a battle file (<c>FIGT_B</c>'s enemy attack scrolls to the province and calls
    /// <c>M.ADV</c> 10): the battle's presenter draws it, and its sound does not end the animation either.
    /// </summary>
    [Fact]
    public void The_map_mark_blinks_after_the_selection_sound_in_a_battle_file()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(missing), new CgLibrary(missing), new BoxGlyphs());
        var state = new GameState { Tmp0025 = 100, Tmp0026 = 120 };
        presenter.Bind(state);
        presenter.Show(new BattleScreenEvent("FIGT_B.ADV", true));
        presenter.Show(new EngineEvent("CF", [(Sys3Int)640, Sys3Int.Zero, (Sys3Int)32, (Sys3Int)32, (Sys3Int)15]));
        var mark = presenter.Stage.PixelAt(640, 0);
        Assert.NotEqual(mark, presenter.Stage.PixelAt(110, 130));

        presenter.Show(new MapEvent(10));
        presenter.Show(new SoundEvent(Sys3Int.Zero, (Sys3Int)109));
        presenter.Show(new PauseEvent((Sys3Int)8));

        Assert.Equal(mark, presenter.Stage.PixelAt(110, 130));
    }

    private sealed record Run(GameSession Session, GamePresenter Presenter, Player Player, StoppingSaves Saves);

    /// <summary>A booted session whose view is a presenter and whose input plays through it.</summary>
    private static async Task<Run> RunAsync(int stopAtWeek)
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var presenter = new GamePresenter(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(LocalData.CgDirectory),
            new BoxGlyphs(), scripts) { MaskEventCgs = Environment.GetEnvironmentVariable("KICHIKUOU_SHOTS_DIR") is not null };
        var player = new Player(state, presenter);
        var saves = new StoppingSaves(stopAtWeek);
        var session = new GameSession(state, new QueuedRandom { DefaultToOne = true }, player, player, scripts, scripts, saves,
            new RecordingRules { State = state });
        presenter.Bind(state);
        await session.BootAsync();
        return new Run(session, presenter, player, saves);
    }

    private sealed class WeekOverException : Exception;

    /// <summary>Save slots in memory; the autosave of week <c>stopAtWeek</c> ends the run.</summary>
    private sealed class StoppingSaves(int stopAtWeek) : ISaveStore
    {
        private readonly MemorySaveStore _slots = new();

        public IEnumerable<int> Slots => _slots.Slots;

        public SaveData? Read(int slot) => _slots.Read(slot);

        public bool Write(int slot, SaveData data)
        {
            if (slot == 1 && data.Variables[517].Value == stopAtWeek)            // Week
                throw new WeekOverException();
            return _slots.Write(slot, data);
        }

        public DateTime? Timestamp(int slot) => _slots.Timestamp(slot);
    }

    /// <summary>A map gesture: a province or a dungeon clicked, or the command menu's item.</summary>
    private sealed record Gesture(int ProvinceId, int DungeonId, int MenuItem)
    {
        public static Gesture Province(int p) => new(p, 0, 0);

        public static Gesture Dungeon(int d) => new(0, d, 0);

        public static Gesture Menu(int item) => new(0, 0, item);
    }

    /// <summary>What the player does at a place of a picker.</summary>
    private enum PickerPress { Left, Right, Move }

    /// <summary>A step in a picker: a place (through <see cref="PickerView.HitTest"/>) and what is done there; a plain tuple is a left click.</summary>
    private sealed record PickerStep(PickerKind Kind, PickerArea Area, int Number, PickerPress Press = PickerPress.Left)
    {
        public static implicit operator PickerStep((PickerKind Kind, PickerArea Area, int Number) click) =>
            new(click.Kind, click.Area, click.Number);
    }

    /// <summary>
    /// The player as a UI on the presenter: the view shows every event and the screen is composed
    /// at every input; map gestures are queued as pointer gestures when the map loop starts
    /// waiting (<see cref="MapScreenEvent"/>), picker steps go through
    /// <see cref="PickerView.HitTest"/>, All Data's graphs get the presses queued for them, menus
    /// are shown and answered by file. A test looks at the screen through the hooks.
    /// </summary>
    private sealed class Player(GameState state, GamePresenter presenter) : IGameInput, IScriptView
    {
        private readonly PointerScript _pointer = new();
        private readonly Queue<int> _commands = new();
        private int _timer;

        public Queue<Gesture> Map { get; } = new();

        public Queue<PickerStep> Pickers { get; } = new();

        /// <summary>The buttons pressed (16 left, 32 right) at All Data's graph waits, in order.</summary>
        public Queue<int> DataPresses { get; } = new();

        /// <summary>Called after every event is shown.</summary>
        public Action<ScriptEvent>? OnShow { get; set; }

        /// <summary>Called at every pointer read, after the screen is composed.</summary>
        public Action? OnRead { get; set; }

        /// <summary>Called when a menu is shown, before it is answered.</summary>
        public Action? OnMenu { get; set; }

        /// <summary>The pointer as last read.</summary>
        public MouseState Last { get; private set; }

        public Dictionary<string, Queue<int>> Answers { get; } = new(StringComparer.Ordinal);

        public List<PickerKind> PickersSeen { get; } = [];

        public List<string> MenuTexts { get; } = [];

        /// <summary>Every menu as the presenter keeps it for a UI (<see cref="GamePresenter.Menu"/>).</summary>
        public List<MenuView> MenuViews { get; } = [];

        public int Composed { get; private set; }

        public int Reads { get; private set; }

        public int Menus { get; private set; }

        public int BattleFrames { get; private set; }

        public int BattlePickerFrames { get; private set; }

        public int WindowTexts { get; private set; }

        public int LitFrames { get; private set; }

        public int Pending => _pointer.Pending;

        /// <summary>A left click at a screen point, after the pointer has settled.</summary>
        public void Click(int x, int y) => _pointer.Click(x, y);

        /// <summary>The pointer moved to a screen point, after it has settled (<see cref="PointerScript.Hover"/>).</summary>
        public void Hover(int x, int y) => _pointer.Hover(x, y);

        /// <summary>A right click at a screen point, after the pointer has settled.</summary>
        public void RightClick(int x, int y) => _pointer.RightClick(x, y);

        /// <summary>Every event shown, in order.</summary>
        public List<ScriptEvent> Shown { get; } = [];

        /// <summary>The key every pause returns (held through it); 0 by default.</summary>
        public Sys3Int PauseKey { get; set; }

        /// <summary>
        /// A random player when set: where nothing is queued a menu gets a random item (or the cancel) and the pointer,
        /// when idle, a random click, right click or move anywhere on the screen.
        /// </summary>
        public Random? Fuzz { get; set; }

        public Task ShowAsync(ScriptEvent e)
        {
            Shown.Add(e);
            presenter.Show(e);
            if (e is MapScreenEvent { Waiting: true })
                Assert.Null(presenter.Picker);                                  // the map is drawn over every picker
            if (e is MapScreenEvent { Waiting: true } && _pointer.Pending == 0 && Map.TryDequeue(out var g))
            {
                // The camera is still the centre here (COMMAND.ADV:51-58 turns it into the corner).
                if (g.MenuItem != 0)
                {
                    _pointer.RightClick(5, 6);
                    _commands.Enqueue(g.MenuItem);
                }
                else
                {
                    var (x, y) = g.ProvinceId != 0 ? (state.Provs[g.ProvinceId].MapX, state.Provs[g.ProvinceId].MapY)
                        : (state.Dgns[g.DungeonId].MapX, state.Dgns[g.DungeonId].MapY);
                    var at = GesturePoints.OnScreen(state, x, y) ?? throw new InvalidOperationException($"({x}, {y}) is not in view.");
                    _pointer.Click(at.X, at.Y);
                }
            }
            if (e is PickerScreenEvent { Open: true } open)
                PickersSeen.Add(open.Kind);
            OnShow?.Invoke(e);
            return Task.CompletedTask;
        }

        private static readonly string? Shots = Environment.GetEnvironmentVariable("KICHIKUOU_SHOTS_DIR");
        private int _shot;

        /// <summary>With <c>KICHIKUOU_SHOTS_DIR</c> set, the screen as a PPM file there (for looking at the drawing).</summary>
        private void Shot(string what)
        {
            if (Shots is null)
                return;
            Directory.CreateDirectory(Shots);
            var rgba = presenter.Screen.ReadScreen();
            using var file = File.Create(Path.Combine(Shots, $"{_shot++:D4}-{what}.ppm"));
            var header = System.Text.Encoding.ASCII.GetBytes("P6 640 400 255 ");
            file.Write(header);
            for (int y = 0; y < 400; y++)
                for (int x = 0; x < 640; x++)
                {
                    int o = (y * 2 * 1280 + x * 2) * 4;
                    file.Write(rgba, o, 3);
                }
        }

        /// <summary>What a UI does before it waits: finish the drawing and compose the screen.</summary>
        private void Frame()
        {
            presenter.Flush();
            if (!presenter.Compose())
                return;
            Composed++;
            if (presenter.InBattle)
                BattleFrames++;
            if (presenter.InBattle && presenter.Picker is not null)
                BattlePickerFrames++;
            if (!presenter.WindowText.IsEmpty)
                WindowTexts++;
            var (r, g, b, _) = presenter.Screen.PixelAt(320, 200);
            if (r + g + b > 0)
                LitFrames++;
        }

        public Task<MouseState> GetMouseAsync()
        {
            if (++Reads > 20_000)
                throw new InvalidOperationException("The pointer was read 20000 times.");
            Frame();
            OnRead?.Invoke();
            if (presenter.Picker is { } picker && _pointer.Pending == 0 && Pickers.TryPeek(out var pick) && pick.Kind == picker.Kind)
            {
                Pickers.Dequeue();
                var at = pick.Area == PickerArea.Cell ? GesturePoints.PickerCell(pick.Number) : GesturePoints.PickerButton(pick.Number);
                Assert.Equal((pick.Area, pick.Number), picker.HitTest(at.X, at.Y));   // the drawn screen agrees with the script's
                switch (pick.Press)
                {
                    case PickerPress.Left: _pointer.Click(at.X, at.Y); break;
                    case PickerPress.Right: _pointer.RightClick(at.X, at.Y); break;
                    case PickerPress.Move: _pointer.Move(at.X, at.Y); break;
                }
                Shot("picker");
            }
            else if (presenter.Data is not (DataView.Closed or DataView.Map) && _pointer.Pending == 0 && DataPresses.TryDequeue(out int button))
            {
                Shot("data");
                if (button == 32)
                    _pointer.RightClick(320, 200);
                else
                    _pointer.Click(320, 200);
            }
            else if (Fuzz is { } rng && _pointer.Pending == 0)
            {
                int x = rng.Next(0, 640), y = rng.Next(0, 400), press = rng.Next(20);
                if (presenter.Picker is not null && rng.Next(4) != 0)
                    (x, y) = rng.Next(10) < 8 ? GesturePoints.PickerCell(rng.Next(1, 51)) : GesturePoints.PickerButton(rng.Next(1, 4));
                if (press < 12)
                    _pointer.Click(x, y);
                else if (press < 16)
                    _pointer.RightClick(x, y);
                else
                    _pointer.Move(x, y);
            }
            Last = _pointer.Next(out _);
            return Task.FromResult(Last);
        }

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            _pointer.Interrupt();
            Menus++;
            presenter.OpenMenu(items);
            Frame();
            MenuTexts.AddRange(presenter.MenuItems!);
            MenuViews.Add(presenter.Menu!);
            OnMenu?.Invoke();
            Shot("menu");
            string file = items.Count > 0 ? items[0].At.File : "";
            int choice = file switch
            {
                "" => 0,                                                        // an empty ] (DGN-07): only a cancel leaves it
                "COMMAND.ADV" when _commands.Count > 0 || Fuzz is null => _commands.Dequeue(),
                "Cﾃｷﾄｼ.ADV" when Fuzz is null => items.Select((item, i) => (item, i)).Single(x => x.item.At.Line == 70).i + 1,
                _ when Answers.TryGetValue(file, out var answers) && answers.Count > 0 => answers.Dequeue(),
                _ when Fuzz is { } rng => rng.Next(0, items.Count + 1),
                _ => 1,
            };
            presenter.CloseMenu();
            return Task.FromResult(choice);
        }

        public Task<Sys3Int> WaitForKeyAsync()
        {
            _pointer.Interrupt();
            Frame();
            Shot("key");
            presenter.AfterKey();
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            _pointer.Interrupt();
            Frame();
            return Task.FromResult(PauseKey);
        }

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            _pointer.Interrupt();
            Frame();
            var direct = menu.Find(FigtSelCommand.Direct);
            return Task.FromResult(direct is { Arguments.Count: > 0 }
                ? new FigtSelChoice(FigtSelCommand.Direct, direct.Arguments[0])
                : new FigtSelChoice(FigtSelCommand.Defend, menu.Find(FigtSelCommand.Defend)!.Arguments[0]));
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            _pointer.Interrupt();
            Frame();
            return Task.FromResult<Sys3Int?>(menu.Options[0].Code);
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Task.FromResult((Sys3Int)1);

        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)2026, (Sys3Int)10, (Sys3Int)9, (Sys3Int)12, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)6]);

        public Task ResetTimerAsync(Sys3Int value)
        {
            _timer = value.Value;
            return Task.CompletedTask;
        }

        /// <summary><c>ZT n</c>: 100 ticks pass on every read (the opening times its pictures).</summary>
        public Task<Sys3Int> GetTimerAsync(int mode)
        {
            _timer += 100;
            Frame();
            return Task.FromResult((Sys3Int)(_timer & 0xffff));
        }

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);

        public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);
    }
}
