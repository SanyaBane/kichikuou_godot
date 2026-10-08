using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// The picker screens: <c>Sﾌﾞﾀｲ</c> (<see cref="DeploymentScreen"/>), <c>Sﾌﾞｶ</c>
/// (<see cref="SubordinateScreen"/>) and <c>Sﾊｰﾚﾑ</c> (<see cref="HaremScreen"/>), answered with
/// pointer gestures on their grids and bars. Each file is also compared with the decompile on
/// random states and random pointers (Content tests, <c>CommandSliceDifferentialTests</c>).
/// </summary>
public class PickerScreenTests
{
    private sealed class PickerRig
    {
        public PickerRig()
        {
            Script = new ScriptContext(State, new ScriptedRandom(), View, Input, Story);
            for (int s = 0; s < State.Strings.Length; s++)
                State.Strings[s] = $"s{s}";
            State.Unit_StrBase = 20;
        }

        public GameState State { get; } = new();
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public ScriptContext Script { get; }
        public LiteralSource Source { get; } = new();

        public void Pick(int id)
        {
            var (x, y) = PointerScript.PickerCell(id);
            Input.Pointer.Click(x, y);
        }

        public void Button(int button)
        {
            var (x, y) = PointerScript.PickerButton(button);
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

    [Fact]
    public async Task With_RetVal_0_only_a_unit_at_status_1_can_be_picked_and_a_click_left_of_the_list_ends()
    {
        var rig = new PickerRig();
        rig.State.Units[3].Status = 2;
        rig.State.CallArg = 1;
        rig.State.RetVal = 0;
        rig.Pick(3);
        var (x, y) = PointerScript.PickerOutside;
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
        var (x, y) = PointerScript.PickerOutside;
        rig.Input.Pointer.Click(x, y);
        rig.Button(3);

        await new HaremScreen(rig.Script, rig.Source).RunAsync();

        Assert.Equal(503, rig.State.CallArg.Value);
        Assert.Equal(2, rig.View.Events.OfType<EngineEvent>().Count(e => e.Command == "B3"));   // the pointer loop twice
        Assert.Empty(rig.Sounds);
    }

    // ---------------------------------------------------------------------------------------
    // The router
    // ---------------------------------------------------------------------------------------

    [Theory]
    [InlineData("Sﾌﾞﾀｲ.ADV")]
    [InlineData("Sﾌﾞｶ.ADV")]
    [InlineData("Sﾊｰﾚﾑ.ADV")]
    public void The_router_runs_the_pickers_itself(string file) =>
        Assert.Contains(file, StrategyRules.PortedFiles);

    [Fact]
    public async Task The_router_hands_a_picker_call_to_its_port()
    {
        var rig = new PickerRig();
        var unported = new ScriptedStory();
        var rules = new StrategyRules(rig.Source, unported);
        var script = new ScriptContext(rig.State, new ScriptedRandom(), rig.View, rig.Input, rules);
        rules.Bind(script);
        rig.State.CallArg = 3;

        await script.CallFileAsync("Sﾌﾞｶ.ADV");

        Assert.Empty(unported.Calls);
        Assert.Equal(3, rig.State.Ui_SubordMode.Value);
        var cc = Assert.IsType<EngineEvent>(rig.View.Events.Single());
        Assert.Equal("CC", cc.Command);
        Assert.Equal([0, 400, 640, 400, 0, 0], cc.Args.Select(a => (int)a.Value));
    }
}
