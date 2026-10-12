using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// The picker screens: <c>Sﾌﾞﾀｲ</c> (<see cref="DeploymentScreen"/>), <c>Sﾌﾞｶ</c>
/// (<see cref="SubordinateScreen"/>), <c>Sﾊｰﾚﾑ</c> (<see cref="HaremScreen"/>) and <c>Sｱｲﾃﾑ</c> (<see cref="ItemScreen"/>), answered with
/// pointer gestures on their grids and bars. Each file is also compared with the decompile on
/// random states and random pointers (Content tests, <c>CommandSliceDifferentialTests</c>).
/// </summary>
public class PickerScreenTests
{
    private sealed class PickerRig
    {
        public PickerRig()
        {
            Script = new OriginalScriptContext(State, new ScriptedRandom(), View, Input, Story);
            for (int s = 0; s < State.Strings.Length; s++)
                State.Strings[s] = $"s{s}";
            State.Unit_StrBase = 20;
        }

        public GameState State { get; } = new();
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public OriginalScriptContext Script { get; }
        public LiteralSource Source { get; } = new();

        public void Pick(int id)
        {
            var (x, y) = GesturePoints.PickerCell(id);
            Input.Pointer.Click(x, y);
        }

        public void Button(int button)
        {
            var (x, y) = GesturePoints.PickerButton(button);
            Input.Pointer.Click(x, y);
        }

        public List<int> Lines => View.Events
            .Select(e => e switch { MessageEvent m => m.Text.Line, ValueMessageEvent v => v.Text.Line, _ => 0 })
            .Where(l => l != 0)
            .ToList();

        public List<int> Sounds => View.Events.OfType<SoundEvent>().Select(s => (int)s.Sound.Value).ToList();

        public List<int> Windows => View.Events.OfType<WindowEvent>().Select(w => (int)w.Op.Value).ToList();

        public int[] Party =>
        [
            State.Bt_PlayerParty1.Value, State.Bt_PlayerParty2.Value, State.Bt_PlayerParty3.Value, State.Bt_PlayerParty4.Value,
        ];
    }

    /// <summary>
    /// Every <c>MS</c> line as "file:line" into string 1; the harem's name table (<c>Sﾊｰﾚﾑ.ADV:57</c>)
    /// as "girl 1" … "girl 50".
    /// </summary>
    private sealed class LiteralSource : IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) => at.Line == 57 && at.File == "Sﾊｰﾚﾑ.ADV"
            ? new SetStringTableStatement(at, Enumerable.Range(1, 50).Select(k => $"girl {k}").ToArray())
            : new SetStringStatement(at, 1, $"{at.File}:{at.Line}");
    }

    // ---------------------------------------------------------------------------------------
    // Sﾌﾞﾀｲ
    // ---------------------------------------------------------------------------------------

    private static PickerRig Deployment(int max, int mode)
    {
        var rig = new PickerRig();
        rig.State.Tmp0021 = (Sys3Int)max;
        rig.State.Tmp0022 = (Sys3Int)mode;
        rig.State.Gold = 50;
        rig.State.Hist_Cur = 7;
        rig.State.History[7].MilitaryExp = 100;
        for (int u = 1; u <= 4; u++)
        {
            rig.State.Units[u].Status = 1;
            rig.State.Units[u].Troops = (Sys3Int)(300 * u);
            rig.State.Units[u].DeployCost = (Sys3Int)u;
        }
        return rig;
    }

    private static Task RunDeployment(PickerRig rig) => new DeploymentScreen(rig.Script, rig.Source).RunAsync();

    [Fact]
    public async Task Go_pays_the_war_expenses_and_spends_the_party()
    {
        var rig = Deployment(max: 4, mode: 1);
        rig.Pick(2);
        rig.Pick(3);
        rig.Button(1);                                                  // Attack

        await RunDeployment(rig);

        Assert.Equal([2, 3, 0, 0], rig.Party);
        Assert.Equal(501, rig.State.CallArg.Value);
        int expenses = (2 + 600 / 300) + (3 + 900 / 300);               // Σ DeployCost + Troops/300
        Assert.Equal(50 - expenses, rig.State.Gold.Value);
        Assert.Equal(100 + expenses, rig.State.History[7].MilitaryExp.Value);   // the record Hist_Cur selects
        Assert.Equal(2, rig.State.Units[2].Status.Value);
        Assert.Equal(2, rig.State.Units[3].Status.Value);
        Assert.Equal(1, rig.State.Units[1].Status.Value);
        Assert.Equal(3, rig.State.Unit_Cur.Value);                      // the last member set
        Assert.Equal(1, rig.State.Ui_SquadMode.Value);
        Assert.Equal(501, rig.State.Ui_SquadChoice.Value);
        Assert.Equal([108, 108, 108], rig.Sounds);                      // two picks, the Go
        Assert.Equal(0, rig.Input.Pointer.Pending);
    }

    [Fact]
    public async Task Clicking_a_member_moves_it_to_the_end_and_Stop_empties_the_party()
    {
        // QUIRK: CMD-16
        var rig = Deployment(max: 4, mode: 1);
        rig.Pick(2);
        rig.Pick(3);
        rig.Pick(4);
        rig.Pick(2);
        rig.Button(3);                                                  // Stop
        int[]? party = null;
        rig.Input.OnRead = () =>
        {
            if (rig.Input.Pointer.Pending == 1)                         // only Stop left
                party ??= rig.Party;
        };

        await RunDeployment(rig);

        Assert.Equal([3, 4, 2, 0], Assert.IsType<int[]>(party));
        Assert.Equal([0, 0, 0, 0], rig.Party);
        Assert.Equal(503, rig.State.CallArg.Value);
        Assert.Equal(50, rig.State.Gold.Value);
        Assert.Equal(1, rig.State.Units[2].Status.Value);
    }

    [Fact]
    public async Task Go_is_refused_without_a_party_and_without_the_funds()
    {
        var rig = Deployment(max: 4, mode: 1);
        rig.State.Gold = 4;
        rig.Button(1);                                                  // no party
        rig.Pick(4);                                                    // 4 + 1200/300 = 8 > 4
        rig.Button(1);
        rig.Button(3);

        await RunDeployment(rig);

        Assert.Contains(167, rig.Lines);                                // "You can't deploy without selecting units."
        Assert.Contains(179, rig.Lines);                                // "Funds are unsufficient. ..."
        Assert.Equal([113, 1, 0, 113, 1, 0], rig.Windows);
        Assert.Equal([90, 108, 90], rig.Sounds);
        Assert.Equal(4, rig.State.Gold.Value);
        Assert.Equal(1, rig.State.Units[4].Status.Value);
        Assert.Equal(503, rig.State.CallArg.Value);
    }

    [Fact]
    public async Task A_full_party_is_refused_and_a_right_click_takes_the_last_member_out()
    {
        var rig = Deployment(max: 1, mode: 3);
        rig.Pick(2);
        rig.Pick(3);                                                    // "Up to 1 unit can deploy."
        rig.Input.Pointer.RightClick(100, 100);                         // 2 out
        rig.Pick(3);
        rig.Button(1);                                                  // Search

        await RunDeployment(rig);

        Assert.Equal([3, 0, 0, 0], rig.Party);
        Assert.Equal(501, rig.State.CallArg.Value);
        int up = rig.Lines.IndexOf(255);
        Assert.True(up >= 0);
        Assert.Equal(264, rig.Lines[up + 1]);                           // no plural "s" (:261) for 1
        Assert.Equal(1, rig.View.Events.OfType<ValueMessageEvent>().First(v => v.Text.Line == 255).Values[0].Value);
    }

    [Theory]
    [InlineData(1, 503)]                                                // attacking: City is no button
    [InlineData(2, 502)]                                                // defending: City
    public async Task The_second_button_is_the_city_only_in_a_defence(int mode, int answer)
    {
        var rig = Deployment(max: 4, mode: mode);
        rig.Pick(1);
        rig.Button(2);
        rig.Button(3);

        await RunDeployment(rig);

        Assert.Equal(answer, rig.State.CallArg.Value);
    }

    [Fact]
    public async Task A_unit_already_spent_is_refused_and_one_without_a_status_ignored()
    {
        var rig = Deployment(max: 4, mode: 1);
        rig.State.Units[2].Status = 2;
        rig.State.Units[3].Status = 0;
        rig.Pick(2);
        rig.Pick(3);
        rig.Pick(4);
        rig.Button(1);

        await RunDeployment(rig);

        Assert.Equal([4, 0, 0, 0], rig.Party);
        Assert.Equal([90, 108, 108], rig.Sounds);
    }

    // ---------------------------------------------------------------------------------------
    // Sﾌﾞｶ
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task The_subordinate_picker_answers_the_unit_in_CallArg_and_Unit_Cur()
    {
        var rig = new PickerRig();
        rig.State.Units[1].Status = 1;
        rig.State.Units[3].Status = 2;
        rig.State.CallArg = 1;
        rig.State.RetVal = 1;
        rig.Pick(3);

        await new SubordinateScreen(rig.Script, rig.Source).RunAsync();

        Assert.Equal(3, rig.State.CallArg.Value);
        Assert.Equal(3, rig.State.Unit_Cur.Value);
        Assert.Equal(1, rig.State.Ui_SubordMode.Value);
        Assert.Equal([108], rig.Sounds);
        Assert.Contains(208, rig.Lines);                                // the details of the unit under the pointer
    }

    /// <summary>
    /// A hand-ported file's <c>G</c> counts its load like a scene's (STR-43): every CG the picker
    /// loads — the portrait of the unit under the pointer among them — once per <c>G</c>.
    /// </summary>
    [Fact]
    public async Task The_subordinate_picker_counts_the_CGs_it_loads()
    {
        var rig = new PickerRig();
        rig.Script.BindCgLoadCounter(196);
        rig.State.Units[1].Status = 1;
        rig.State.Units[3].Status = 2;
        rig.State.Units[3].PortraitCg = 1330;
        rig.State.CallArg = 1;
        rig.State.RetVal = 1;
        rig.Pick(3);

        await new SubordinateScreen(rig.Script, rig.Source).RunAsync();

        var loads = rig.View.Events.OfType<EngineEvent>().Where(e => e.Command == "G")
            .GroupBy(e => (int)e.Args[0].Value).ToDictionary(g => g.Key, g => g.Count());
        Assert.Contains(1330, loads.Keys);
        foreach (var (cg, count) in loads)
            Assert.Equal(count, rig.State.CgSeen[cg].CgSeen.Value);
    }

    [Fact]
    public async Task With_RetVal_0_only_a_unit_at_status_1_can_be_picked_and_a_click_left_of_the_list_ends()
    {
        var rig = new PickerRig();
        rig.State.Units[3].Status = 2;
        rig.State.CallArg = 1;
        rig.State.RetVal = 0;
        rig.Pick(3);
        var (x, y) = GesturePoints.PickerOutside;
        rig.Input.Pointer.Click(x, y);

        await new SubordinateScreen(rig.Script, rig.Source).RunAsync();

        Assert.Equal(503, rig.State.CallArg.Value);
        Assert.Equal(503, rig.State.Unit_Cur.Value);
        Assert.Equal([90], rig.Sounds);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public async Task The_other_modes_draw_and_return_without_reading_the_pointer(int mode)
    {
        var rig = new PickerRig();
        rig.State.CallArg = (Sys3Int)mode;

        await new SubordinateScreen(rig.Script, rig.Source).RunAsync();

        Assert.Empty(rig.Input.MouseReads);
        Assert.Equal(mode, rig.State.Ui_SubordMode.Value);
        Assert.Equal(mode == 2, rig.State.Strings[1] == "Sﾌﾞｶ.ADV:21");  // the "End" label
    }

    // ---------------------------------------------------------------------------------------
    // Sﾊｰﾚﾑ
    // ---------------------------------------------------------------------------------------

    private static PickerRig Harem(params int[] girls)
    {
        var rig = new PickerRig();
        rig.State.Week = 30;
        foreach (int k in girls)
            rig.State.Memory.Var(249 + k - 1) = 1;                      // HaremAvail_*
        return rig;
    }

    [Fact]
    public async Task The_harem_picker_lists_the_girls_on_offer_and_answers_the_one_picked()
    {
        var rig = Harem(1, 11);
        rig.State.History[11].Harem_LastWeek = 20;
        rig.Pick(12);                                                   // not on offer
        rig.Pick(11);

        await new HaremScreen(rig.Script, rig.Source).RunAsync();

        Assert.Equal(11, rig.State.CallArg.Value);
        Assert.Equal(0, rig.State.Hist_Cur.Value);                      // reset at the exit
        Assert.Equal(1, rig.State.Buffer[1].Val.Value);
        Assert.Equal(1, rig.State.Buffer[11].Val.Value);
        Assert.Equal(0, rig.State.Buffer[12].Val.Value);
        Assert.Equal("girl 11", rig.State.Strings[1]);                  // the last name listed
        Assert.Equal([108], rig.Sounds);
        // Hovering Soul showed when she was last summoned: week 20 is year 3, month 5, week 4.
        var last = rig.View.Events.OfType<ValueMessageEvent>().Last(v => v.Text.Line == 241);
        Assert.Equal([3, 5, 4], last.Values.Select(v => (int)v.Value));
        Assert.Equal(4, rig.State.DateWeek.Value);
    }

    [Fact]
    public async Task A_left_click_left_of_the_list_reopens_it_and_End_cancels()
    {
        var rig = Harem(1);
        var (x, y) = GesturePoints.PickerOutside;
        rig.Input.Pointer.Click(x, y);
        rig.Button(3);

        await new HaremScreen(rig.Script, rig.Source).RunAsync();

        Assert.Equal(503, rig.State.CallArg.Value);
        Assert.Equal(2, rig.View.Events.OfType<EngineEvent>().Count(e => e.Command == "B3"));   // the pointer loop twice
        Assert.Empty(rig.Sounds);
    }

    // ---------------------------------------------------------------------------------------
    // Sｱｲﾃﾑ
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The item screen in view <paramref name="view"/>: units 1-4 at status 1, unit 2 holding
    /// item 1 (locked), unit 3 item 6; items 5 and 7 in stock, item 8 in stock without effect;
    /// item names from string 300, unit 48's dungeon attack modifier 2.
    /// </summary>
    private static PickerRig Items(int view)
    {
        var rig = new PickerRig();
        rig.State.Item_StrBase = 300;
        rig.State.Ui_ItemViewMode = (Sys3Int)view;
        for (int u = 1; u <= 4; u++)
            rig.State.Units[u].Status = 1;
        foreach (var (item, holder) in new[] { (1, 2), (6, 3), (5, 100), (7, 100), (8, 100) })
        {
            rig.State.Items[item].Holder = (Sys3Int)holder;
            rig.State.Items[item].Effect = (Sys3Int)(item == 8 ? 0 : 3);
            if (holder != 100)
                rig.State.Units[holder].Item = (Sys3Int)item;
        }
        rig.State.Units[48].DgnAtkMod = 2;
        return rig;
    }

    private static Task RunItems(PickerRig rig) => new ItemScreen(rig.Script, rig.Source).RunAsync();

    [Fact]
    public async Task A_unit_then_an_item_equips_it_and_the_old_one_goes_to_stock()
    {
        var rig = Items(view: 0);
        rig.State.Units[4].Item = 7;
        rig.State.Items[7].Holder = 4;
        rig.Pick(4);                                                    // the unit: the item view, "Which item?"
        rig.Pick(5);                                                    // the item: equipped
        rig.Button(3);                                                  // End

        await RunItems(rig);

        Assert.Equal(5, rig.State.Units[4].Item.Value);
        Assert.Equal(4, rig.State.Items[5].Holder.Value);
        Assert.Equal(100, rig.State.Items[7].Holder.Value);             // the unit's old item back in stock
        // QUIRK: CMD-17 — taking item 5 from the stock (holder 100) wrote Unit_Item[100] = Unit_DgnAtkMod[48].
        Assert.Equal(0, rig.State.Units[48].DgnAtkMod.Value);
        Assert.Equal(0, rig.State.Ui_ItemViewMode.Value);               // the view of the first pick again
        Assert.Contains(104, rig.Lines);                                // "<unit> is equipping / Which item?"
        Assert.Equal([108, 110], rig.Sounds);
        Assert.Equal(0, rig.State.Ui_ItemSel.Value);
        Assert.Equal(0, rig.State.Unit_Cur.Value);                      // the exit: the cursors on the picks
        Assert.Equal(new PickerScreenEvent(PickerKind.Items, 0, false), rig.View.Events.OfType<ScreenEvent>().Last());
        Assert.IsType<MapEvent>(rig.View.Events[^1]);
        Assert.Equal(0, rig.Input.Pointer.Pending);
    }

    [Fact]
    public async Task Items_1_and_2_stay_where_they_are()
    {
        var rig = Items(view: 0);
        rig.Pick(2);                                                    // unit 2 holds item 1
        rig.Button(3);

        await RunItems(rig);

        Assert.Contains(254, rig.Lines);                                // "<unit>'s equipment, <item>, can't be unequipped."
        Assert.DoesNotContain(104, rig.Lines);
        Assert.Equal([113, 1, 0], rig.Windows);
        Assert.Equal([90], rig.Sounds);
        Assert.Equal(1, rig.State.Units[2].Item.Value);
        Assert.Equal(0, rig.State.Ui_ItemUnitSel.Value);
    }

    [Fact]
    public async Task An_item_without_effect_cannot_be_equipped()
    {
        var rig = Items(view: 1);
        rig.Pick(8);
        rig.Button(3);

        await RunItems(rig);

        Assert.Contains(367, rig.Lines);                                // "This item can't be equipped."
        Assert.Equal(100, rig.State.Items[8].Holder.Value);
        Assert.Equal(1, rig.State.Ui_ItemViewMode.Value);
    }

    [Fact]
    public async Task A_held_item_picked_first_is_taken_off_after_a_yes()
    {
        var rig = Items(view: 1);
        rig.Pick(6);                                                    // held by unit 3: "unequip it?"
        rig.Input.Choices.Enqueue(1);                                   // Yes
        rig.Button(3);

        await RunItems(rig);

        Assert.Contains(322, rig.Lines);
        Assert.Equal(100, rig.State.Items[6].Holder.Value);
        Assert.Equal(0, rig.State.Units[3].Item.Value);
        Assert.Equal([111, 110], rig.Sounds);
        Assert.Equal([399, 403], rig.Input.Menus.Single().Select(i => i.At.Line));
        Assert.Contains(rig.View.Events, e => e is EngineEvent { Command: "B1" } b && b.Args[0] == 2);
    }

    [Fact]
    public async Task A_held_item_moves_to_the_unit_picked_after_a_yes()
    {
        var rig = Items(view: 0);
        rig.Pick(1);
        rig.Pick(6);                                                    // held by unit 3: "remove it and equip <unit> instead?"
        rig.Input.Choices.Enqueue(1);
        rig.Button(3);

        await RunItems(rig);

        Assert.Contains(341, rig.Lines);
        Assert.Equal(6, rig.State.Units[1].Item.Value);
        Assert.Equal(1, rig.State.Items[6].Holder.Value);
        Assert.Equal(0, rig.State.Units[3].Item.Value);
        Assert.Equal([108, 111, 110], rig.Sounds);
    }

    [Fact]
    public async Task A_no_keeps_the_item_where_it_is()
    {
        var rig = Items(view: 1);
        rig.Pick(6);
        rig.Input.Choices.Enqueue(2);                                   // No
        rig.Button(3);

        await RunItems(rig);

        Assert.Equal(3, rig.State.Items[6].Holder.Value);
        Assert.Equal(6, rig.State.Units[3].Item.Value);
    }

    [Fact]
    public async Task The_bars_first_place_is_taken_for_a_unit()
    {
        // QUIRK: CMD-17 — 997 is "unit 997": its item slot is commander 165's troops.
        var rig = Items(view: 0);
        rig.State.Cmdrs[165].Troops = 0;
        rig.Button(1);
        rig.Pick(5);
        rig.Button(3);

        await RunItems(rig);

        Assert.Equal(997, rig.State.Items[5].Holder.Value);
        Assert.Equal(5, rig.State.Cmdrs[165].Troops.Value);
        var prompt = rig.View.Events.OfType<MessageEvent>().Single(m => m.Text.Line == 104);
        Assert.Equal([20 + 996 * 3], prompt.StringVariables!.Select(s => (int)s.Value));
    }

    [Fact]
    public async Task A_right_click_drops_a_half_made_pick_then_leaves()
    {
        var rig = Items(view: 0);
        rig.Pick(1);
        rig.Input.Pointer.RightClick(400, 100);                         // the pick dropped, the view back
        rig.Input.Pointer.RightClick(400, 100);                         // nothing picked: the screen ends

        await RunItems(rig);

        Assert.Equal(0, rig.State.Ui_ItemViewMode.Value);
        Assert.Equal(0, rig.State.Ui_ItemUnitSel.Value);
        Assert.Equal(0, rig.State.Units[1].Item.Value);
        Assert.Equal(0, rig.Input.Pointer.Pending);
    }

    [Fact]
    public async Task The_view_turns_and_stays_turned()
    {
        var rig = Items(view: 0);
        rig.Button(2);                                                  // Item/Character
        rig.Button(3);

        await RunItems(rig);

        Assert.Equal(1, rig.State.Ui_ItemViewMode.Value);
        Assert.Equal([108], rig.Sounds);
        Assert.Equal(2, rig.View.Events.Count(e => e is EngineEvent { Command: "J1" }));   // the screen drawn twice
    }

    // ---------------------------------------------------------------------------------------
    // The router
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("Sﾌﾞﾀｲ.ADV")]
    [InlineData("Sﾌﾞｶ.ADV")]
    [InlineData("Sﾊｰﾚﾑ.ADV")]
    [InlineData("Sｱｲﾃﾑ.ADV")]
    public void The_router_runs_the_pickers_itself(string file) =>
        Assert.Contains(file, StrategyRules.PortedFiles);

    [Fact]
    public async Task The_router_hands_a_picker_call_to_its_port()
    {
        var rig = new PickerRig();
        var unported = new ScriptedStory();
        var rules = new StrategyRules(rig.Source, unported);
        var script = new OriginalScriptContext(rig.State, new ScriptedRandom(), rig.View, rig.Input, rules);
        rules.Bind(script);
        rig.State.CallArg = 3;

        await script.CallFileAsync("Sﾌﾞｶ.ADV");

        Assert.Empty(unported.Calls);
        Assert.Equal(3, rig.State.Ui_SubordMode.Value);
        Assert.Equal(new PickerScreenEvent(PickerKind.Subordinates, 3, false), rig.View.Events[^1]);
        var cc = Assert.IsType<EngineEvent>(rig.View.Events.Single(e => e is not ScreenEvent));
        Assert.Equal("CC", cc.Command);
        Assert.Equal([0, 400, 640, 400, 0, 0], cc.Args.Select(a => (int)a.Value));
    }
}
