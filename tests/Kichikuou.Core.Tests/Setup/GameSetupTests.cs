using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Script;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Setup;

/// <summary><c>SET.ADV</c> and the new game (<c>GameSetup</c>) over a fake scenario.</summary>
public class GameSetupTests
{
    /// <summary>Answers the setup's reads: <c>MS n, "line"</c> literals and small data files.</summary>
    private sealed class FakeScenario : IScenarioSource
    {
        public List<string> FilesRead { get; } = [];

        public Dictionary<string, IReadOnlyList<ScriptStatement>> Files { get; } = new();

        public IReadOnlyList<ScriptStatement> ReadFile(string file)
        {
            FilesRead.Add(file);
            return Files.TryGetValue(file, out var statements) ? statements : [new ReturnStatement(new(file, 1))];
        }

        public ScriptStatement ReadStatement(ScriptRef at)
        {
            int slot = at.Line switch { 33 => 1, 35 => 2, 69 => 4, 71 => 5, 73 => 6, 99 => 8, 121 => 7, _ => 0 };
            Assert.True(at.File == "SET.ADV" && slot != 0, $"unexpected literal {at}");
            return new SetStringStatement(at, slot, $"text {at.Line}");
        }
    }

    private static (ScriptRig Rig, FakeScenario Scenario, GameSetup Setup) Create()
    {
        var rig = new ScriptRig();
        var scenario = new FakeScenario();
        return (rig, scenario, new GameSetup(rig.Script, scenario));
    }

    [Fact]
    public async Task Set_initialises_the_plain_values_and_string_bases()
    {
        var (rig, _, setup) = Create();

        await setup.SetAsync();

        var s = rig.State;
        Is(100, s.VAR0055);
        Is(4, s.FadeSpeed);
        Is(1, s.N_Kanami_State);
        Is(40, s.LadyCaravan_Prov);
        Is(900, s.Bt_BackgroundCg);
        Is(1, s.BgmEnabled);
        Is(52, s.Unit_Count);
        Is(220, s.Cmdr_Count);
        Is(100, s.Prov_Count);
        Is(50, s.Item_Count);
        Is(30, s.Dgn_Count);
        Is(300, s.Hist_Count);
        Is(20, s.Unit_StrBase);
        Is(176, s.Cmdr_StrBase);
        Is(836, s.Prov_StrBase);
        Is(936, s.Item_StrBase);
        Is(1, s.Ui_FaceOverMap);
        Is(812, s.Ui_MapCgTopL);
        Is(2, s.Ui_ScrollWait);     // SpecGraphics / SpecBaseline = 0 / 0 = 0
    }

    [Fact]
    public async Task Set_leaves_the_scratch_of_its_layout_arithmetic()
    {
        var (rig, _, setup) = Create();

        await setup.SetAsync();

        Is(10, rig.State.RetVal);       // :339, before the ﾛｰﾄﾞｾｰﾌﾞ hook
        Is(1000, rig.State.Tmp0001);    // :275 Ui_BtnCount
    }

    [Fact]
    public async Task Set_writes_Prov_BattleArea_into_the_plain_slot()
    {
        var (rig, _, setup) = Create();

        await setup.SetAsync();

        // QUIRK: DAT-01 — :104 runs before Prov_BattleArea is bound.
        Is(500, rig.State.Memory.Var(134));
        Is(0, rig.State.Provs[0].BattleArea);
    }

    [Fact]
    public async Task Set_reads_its_literals_and_the_record_files_in_order_then_calls_the_save_file()
    {
        var (rig, scenario, setup) = Create();

        await setup.SetAsync();

        Assert.Equal(["SET_MAP.ADV", "SET_CHR.ADV", "SET_ITEM.ADV"], scenario.FilesRead);
        Assert.Equal("text 33", rig.State.Strings[1]);
        Assert.Equal("text 73", rig.State.Strings[6]);
        Assert.Equal("text 121", rig.State.Strings[7]);
        Assert.Equal([("ﾛｰﾄﾞｾｰﾌﾞ.ADV", 0)], rig.Story.Calls);
    }

    [Fact]
    public async Task The_record_files_run_against_the_bound_layout()
    {
        var (rig, scenario, setup) = Create();
        var at = new ScriptRef("SET_CHR.ADV", 1);
        scenario.Files["SET_CHR.ADV"] =
        [
            new AssignStatement(at, 154, new ConstExpr(1)),     // Unit_Cur
            new AssignStatement(at, 102, new ConstExpr(1)),     // Unit_Status
            new ReturnStatement(at),
        ];

        await setup.SetAsync();

        Is(1, rig.State.Units[1].Status);
        Is(1, rig.State.Unit_Cur);
    }

    [Fact]
    public async Task Boot_sets_the_start_values_then_runs_SET()
    {
        var (rig, _, setup) = Create();

        await setup.BootAsync();

        Is(1280, rig.State.VAR0041);
        Is(200, rig.State.VAR0042);
        Is(52, rig.State.Unit_Count);
    }

    [Fact]
    public async Task A_new_game_starts_in_week_8_with_50_gold_and_plays_the_prologue()
    {
        var (rig, _, setup) = Create();

        await setup.NewGameAsync();

        Is(4, rig.State.CurrentBgm);
        Is(8, rig.State.Week);
        Is(0, rig.State.StoryStage);
        Is(50, rig.State.Gold);
        Is(0, rig.State.CallArg);
        Assert.Equal([new WaoMapEvent(3), new MapEvent(0)], rig.View.Events);
        Assert.Equal([("Iﾌﾟﾛﾛｰｸﾞ.ADV", 0)], rig.Story.Calls);
    }
}
