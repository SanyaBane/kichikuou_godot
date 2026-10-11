using Kichikuou.Content.Graphics;
using Kichikuou.Content.Scripts;
using Kichikuou.Content.Strategy;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The strategic presenter (<see cref="GamePresenter"/>) on whole runs of the player's game,
/// headless: every event is shown and the screen composed at every input, the way a UI does, and
/// the player's choices go through the presenter's own hit tests (the picker grid, the menu).
/// Skipped without the decompile.
/// </summary>
public class GamePresenterTests(ITestOutputHelper output)
{
    /// <summary>
    /// Week 9 of a new game played through <see cref="GameSession"/>: Sill's CG scene, Pillage on
    /// province 34 with the deployment picker and the battle, a talk picked in the subordinate
    /// picker, a harem visit, the hideout, a save into slot 2 on the save screen, Turn END; week 10:
    /// the subordinate picker left with End (its file leaves it open, the map covers it), Turn END.
    /// Every text resolves and every screen is drawn; no picker outlives the map.
    /// </summary>
    [Fact]
    public async Task A_prologue_week_is_drawn_through_the_presenter()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
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
        Assert.Equal(11, run.Session.State.Week.Value);
    }

    /// <summary>
    /// The rebels' turn (<c>TCﾘｰｻﾞｽ</c>) on the booted world with the rebels holding 3-7: army A
    /// attacks province 2 and army B province 8, each through <c>FIGT_B</c>, whose intercept opens
    /// the deployment picker inside the battle — drawn by hand over the battle's screen and clicked
    /// through its own hit test (a unit, then Intercept) — and the battle is fought.
    /// </summary>
    [Fact]
    public async Task A_defence_picks_its_party_in_a_picker_drawn_over_the_battle()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
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
    [Fact]
    public async Task The_item_screen_is_drawn_from_its_views_and_picks()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
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
        player.Pickers.Enqueue((PickerKind.Items, PickerArea.Cell, 4));                          // item 4, held: unequip it?
        player.Answers["Sｱｲﾃﾑ.ADV"] = new([2]);                                                  // No
        player.Pickers.Enqueue((PickerKind.Items, PickerArea.Cell, 3));                          // item 3 from stock,
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
    /// All Data (<c>各種DATA</c>) on the booted world with 16 weeks of distinct history: the map in
    /// miniature with the menu, the funds graph (a bar cut at 300 with "OV"), the scale menu from a
    /// right click and the graph again at 4x (the bar now 45 high), each other graph, Return. The
    /// screen is drawn by hand; the file's own drawing never reaches the stage.
    /// </summary>
    [Fact]
    public async Task All_data_draws_the_map_view_and_every_graph()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
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
    /// A search of the Monsters' Cave (<c>Cﾒｲｷｭｳ</c>, dungeon 2) at stage 1 on the booted world,
    /// through the router: Start Search, Bound picked in the deployment picker drawn by hand, the
    /// arrival over the cave's picture, a floor down with a battle (every draw is 1: monsters on
    /// each move) drawn by the battle's presenter with <c>Bt_Kind</c> 3, Retreat cancelled into the
    /// second, empty menu (DGN-07: an empty box that nothing hits, left by a cancel), the floor again,
    /// Retreat, Finish.
    /// </summary>
    [Fact]
    public async Task A_dungeon_search_draws_its_floors_menus_and_battle()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
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
        player.OnShow = e =>
        {
            if (e is BattleScreenEvent { Open: true })
                battles.Add((s.Bt_Kind.Value, s.Bt_EnemyParty1.Value));
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
        Assert.Contains(1031, centres);                                       // the cave's picture
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
    [Fact]
    public async Task The_opening_draws_on_the_stage()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        var run = await RunAsync(stopAtWeek: 0);

        await run.Session.Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");

        var p = run.Presenter;
        output.WriteLine($"composed {run.Player.Composed}, lit frames {run.Player.LitFrames}");
        Assert.Empty(p.Unresolved);
        Assert.True(run.Player.LitFrames > 10, "the opening's pictures reached the screen");
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
    [Fact]
    public async Task The_ending_draws_on_the_stage_with_its_picture_masked_for_the_agent()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
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
    /// <c>WAO_SUB</c> 0 on the full-screen window restores nothing (<c>WAO_SUB.ADV:267-270</c>):
    /// after <c>Iﾄｳｿﾞｸ.ADV</c>'s last line (<c>:1839-1843</c>) the frame and the text stay on the
    /// screen while <c>ｵｰﾌﾟﾆﾝｸﾞ</c> waits for its music, until something draws over them.
    /// Skipped without the player's CGs.
    /// </summary>
    [Fact]
    public void Closing_the_full_screen_window_leaves_it_on_the_screen()
    {
        if (!File.Exists(Path.Combine(LocalData.CgDirectory, "CG815.png")))
            return;
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
    [Fact]
    public void A_cg_drawn_without_its_palette_shows_in_the_palette_loaded_later()
    {
        if (!File.Exists(Path.Combine(LocalData.CgDirectory, "CG1734.png")))
            return;
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
    [Fact]
    public void A_map_scroll_wipes_the_pictures_until_they_are_drawn_again()
    {
        if (!File.Exists(Path.Combine(LocalData.CgDirectory, "CG1008.png")))
            return;
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

        public int Composed { get; private set; }

        public int Reads { get; private set; }

        public int Menus { get; private set; }

        public int BattleFrames { get; private set; }

        public int BattlePickerFrames { get; private set; }

        public int WindowTexts { get; private set; }

        public int LitFrames { get; private set; }

        public int Pending => _pointer.Pending;

        /// <summary>Every event shown, in order.</summary>
        public List<ScriptEvent> Shown { get; } = [];

        /// <summary>The key every pause returns (held through it); 0 by default.</summary>
        public Sys3Int PauseKey { get; set; }

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
                    var at = PointerScript.OnScreen(state, x, y) ?? throw new InvalidOperationException($"({x}, {y}) is not in view.");
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
                var at = pick.Area == PickerArea.Cell ? PointerScript.PickerCell(pick.Number) : PointerScript.PickerButton(pick.Number);
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
            OnMenu?.Invoke();
            Shot("menu");
            string file = items.Count > 0 ? items[0].At.File : "";
            int choice = file switch
            {
                "" => 0,                                                        // an empty ] (DGN-07): only a cancel leaves it
                "COMMAND.ADV" => _commands.Dequeue(),
                "Cﾃｷﾄｼ.ADV" => items.Select((item, i) => (item, i)).Single(x => x.item.At.Line == 70).i + 1,
                _ when Answers.TryGetValue(file, out var answers) && answers.Count > 0 => answers.Dequeue(),
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
