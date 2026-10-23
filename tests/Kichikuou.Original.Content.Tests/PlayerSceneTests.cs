using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Golden;
using Kichikuou.Original.Setup;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The scenes of milestone 1 run from the player's decompile through <c>SceneRunner</c> on the
/// world the boot and the new game build (skipped without the decompile). Checks the state
/// effects listed in docs/strategy/README.md ("What the prologue reaches").
/// </summary>
public class PlayerSceneTests(ITestOutputHelper output)
{
    private static readonly string[] Milestone1 =
        ["Iﾄｳｿﾞｸ.ADV", "Iﾌﾟﾛﾛｰｸﾞ.ADV", "Iｼﾚｲﾌﾞ.ADV", "Iﾊﾝﾗﾝ.ADV", "ｵｰﾌﾟﾆﾝｸﾞ.ADV", "Gｼｨﾙ.ADV", "Gｿｳﾙ.ADV", "Gﾊﾞｳﾝﾄﾞ.ADV", "Gｱﾅｾﾙ.ADV"];

    /// <summary>The variable groups a scene may change without it being a world change.</summary>
    private static readonly HashSet<string> Presentation = new(StringComparer.Ordinal) { "ui", "scratch", "system" };

    /// <summary>A rig after the boot and the new game (prologue included).</summary>
    private static async Task<SceneRig> NewGameAsync(QueuedRandom? random = null)
    {
        var rig = new SceneRig(LocalData.ScenarioDirectory, random ?? new QueuedRandom { DefaultToOne = true });
        var setup = new GameSetup(rig.Script, new ScenarioScripts(LocalData.ScenarioDirectory));
        await setup.BootAsync();
        await setup.NewGameAsync();
        return rig;
    }

    [ScenarioFact]
    public async Task The_prologue_scene_sets_only_the_music_and_the_dungeon_cursor()
    {
        var rig = new SceneRig(LocalData.ScenarioDirectory);
        var setup = new GameSetup(rig.Script, new ScenarioScripts(LocalData.ScenarioDirectory));
        await setup.BootAsync();
        rig.Rules.Calls.Clear();   // SET.ADV:340 loads the global save page through ﾛｰﾄﾞｾｰﾌﾞ
        var before = Snapshot.Of(rig.State);

        await setup.NewGameAsync();

        // ｽﾀMENU's Week := 8 and Gold := 50 (StoryStage := 0 changes nothing), then the prologue's
        // Dgn_Cur := 1 and CurrentBgm := 1 (a system variable).
        var changes = Snapshot.Of(rig.State).WorldChangesSince(before);
        foreach (string c in changes)
            output.WriteLine(c);
        Assert.Equal(new[] { "VAR0162 Dgn_Cur: 26 -> 1", "VAR0519 Gold: 0 -> 50", WeekChange }.Order(), changes.Order());
        Assert.Equal(1, rig.State.CurrentBgm.Value);
        Assert.Empty(rig.Random.Draws);
        Assert.Empty(rig.Rules.Calls);
    }

    /// <summary>
    /// Scenes of the prologue and their whole effect on the world: talks write nothing, the harem
    /// visits set the EXP reward <c>Cﾊｰﾚﾑ</c> pays (and Sill's and Soul's open the thieves step),
    /// the week-9 scene leaves the province cursor on 34.
    /// </summary>
    [ScenarioTheory]
    [InlineData("Gｿｳﾙ.ADV", 4)]
    [InlineData("Gｿｳﾙ.ADV", 20)]
    [InlineData("Gﾊﾞｳﾝﾄﾞ.ADV", 20)]
    [InlineData("Gｼｨﾙ.ADV", 20)]
    [InlineData("Iｼﾚｲﾌﾞ.ADV", 10, "VAR0158 Prov_Cur: 91 -> 34")]
    [InlineData("Gｼｨﾙ.ADV", 10, "VAR0248 Harem_ExpReward: 0 -> 5", "VAR0398 VAR0398: 0 -> 1")]
    [InlineData("Gｿｳﾙ.ADV", 10, "VAR0248 Harem_ExpReward: 0 -> 3", "VAR0398 VAR0398: 0 -> 1")]
    public async Task Prologue_scenes_change_only_what_they_should(string file, int entry, params string[] expected)
    {
        var rig = await NewGameAsync();
        rig.State.Ev_Entry = (Sys3Int)entry;
        var before = Snapshot.Of(rig.State);

        await rig.Script.CallFileAsync(file);

        Assert.Equal(expected, Snapshot.Of(rig.State).WorldChangesSince(before));
        output.WriteLine($"draws: {string.Join(", ", rig.Random.Draws)}; events: {rig.View.Events.Count}; inputs: {rig.Input.Answers}");
    }

    [ScenarioFact]
    public async Task Anasels_first_harem_visit_advances_the_thieves_arc()
    {
        var rig = await NewGameAsync();
        rig.State.F_Thieves_State = 2;
        rig.State.Ev_Entry = 10;
        var before = Snapshot.Of(rig.State);

        await rig.Script.CallFileAsync("Gｱﾅｾﾙ.ADV");

        Assert.Equal(
            ["VAR0158 Prov_Cur: 91 -> 39", "VAR0248 Harem_ExpReward: 0 -> 4", "VAR0329 F_Thieves_State: 2 -> 3"],
            Snapshot.Of(rig.State).WorldChangesSince(before));
    }

    /// <summary>
    /// Pillage on province 33, the first time (<c>Iﾄｳｿﾞｸ.ADV:110-953</c>): the thieves join, the
    /// party picker and the battle are rule files, a won battle draws the banter (<c>Y 4, 4</c>,
    /// <c>:342</c>) and, away from Anasel's province, pays one gold.
    /// </summary>
    [ScenarioFact]
    public async Task The_first_pillage_runs_the_picker_and_the_battle_and_pays_a_gold()
    {
        var rig = await NewGameAsync();
        var random = new QueuedRandom(4);
        rig = await NewGameAsync(random);
        rig!.State.Prov_Cur = 33;
        rig.State.Ui_CmdAttackAvail = 1;
        rig.Rules.Calls.Clear();
        rig.Rules.Effects["Sﾌﾞﾀｲ.ADV"] = s => s.Bt_PlayerParty1 = 1;
        rig.Rules.Effects["FIGT_A.ADV"] = s =>
        {
            Assert.Equal(1, s.Bt_Kind.Value);
            s.Bt_Result = 2;
        };

        await rig.EventAsync("Iﾄｳｿﾞｸ.ADV", 1);

        Assert.Equal([("Sﾌﾞﾀｲ.ADV", 1), ("FIGT_A.ADV", 1)], rig.Rules.Calls);
        Assert.Equal([4], random.Draws);
        Assert.Equal(1, rig.State.F_Thieves_State.Value);
        Assert.Equal(0, rig.State.Ui_CmdAttackAvail.Value);
        Assert.Equal(0, rig.State.Bt_Kind.Value);
        Assert.Equal(51, rig.State.Gold.Value);
    }

    [ScenarioFact]
    public async Task The_opening_runs_to_its_end()
    {
        var rig = await NewGameAsync();

        await rig.Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");

        Assert.Equal(1, rig.State.F_OpeningSeen.Value);
        output.WriteLine($"events: {rig.View.Events.Count}; inputs: {rig.Input.Answers}");
    }

    /// <summary>
    /// Every entry of every milestone-1 scene runs from the new game's world with default
    /// answers (first menu item, key 0, draws of 1) and the rule files stubbed, and ends with its
    /// page return or a jump to OVER — a smoke test of the runtime over real scenes.
    /// </summary>
    [ScenarioFact]
    public async Task Every_entry_of_the_milestone_scenes_runs_to_its_end()
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var failures = new List<string>();
        foreach (string file in Milestone1)
        {
            var entries = scripts.ReadProgram(file).FindEntries(OriginalVariant.Profile.EventEntryVariable).Select(e => e.Entry.Value).Distinct().ToList();
            if (entries.Count == 0)
                entries.Add(0);
            foreach (int entry in entries)
            {
                var rig = await NewGameAsync();
                string outcome;
                try
                {
                    await rig.EventAsync(file, entry);
                    outcome = "returned";
                }
                catch (ScenarioJumpException jump)
                {
                    outcome = $"jumped to {jump.File}";
                }
                catch (Exception e)
                {
                    outcome = $"FAILED: {e.GetType().Name}: {e.Message}";
                    failures.Add($"{file} {entry}: {outcome}");
                }
                output.WriteLine($"{file} {entry}: {outcome}; draws {rig.Random.Draws.Count}, inputs {rig.Input.Answers}, "
                    + $"rules [{string.Join(", ", rig.Rules.Calls.Select(c => $"{c.File} {c.Entry}"))}]");
            }
        }
        Assert.Empty(failures);
    }

    private static string WeekChange => $"{VariableCatalog.DescribeVar(VariableCatalog.Variables.ToList().FindIndex(v => v.Name == "Week"))}: 0 -> 8";

    /// <summary>Variables, pages and strings at one moment.</summary>
    private sealed record Snapshot(int[] Vars, Dictionary<int, int[]> Pages, string[] Strings)
    {
        public static Snapshot Of(GameState state) => new(
            Enumerable.Range(0, VariableCatalog.Variables.Count).Select(n => (int)state.Memory.Var(n).Value).ToArray(),
            VariableCatalog.Pages.ToDictionary(p => p.Page, p => Enumerable.Range(0, p.Size).Select(i => (int)state.Memory.ReadPage(p.Page, i).Value).ToArray()),
            (string[])state.Strings.Clone());

        /// <summary>
        /// Every change outside the presentation and scratch groups and the engine's CG load counts
        /// (<see cref="GoldenFilter.IgnoredVariables"/>, STR-43), described.
        /// </summary>
        public List<string> WorldChangesSince(Snapshot before)
        {
            var changes = new List<string>();
            for (int n = 0; n < Vars.Length; n++)
            {
                if (Vars[n] != before.Vars[n] && !Presentation.Contains(VariableCatalog.Variables[n].Group))
                    changes.Add($"{VariableCatalog.DescribeVar(n)}: {before.Vars[n]} -> {Vars[n]}");
            }
            foreach (var (page, values) in Pages)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    if (values[i] != before.Pages[page][i] && !IsEngineCounter(page, i))
                        changes.Add($"{VariableCatalog.DescribeSlot(page, i)}: {before.Pages[page][i]} -> {values[i]}");
                }
            }
            return changes;
        }

        private static bool IsEngineCounter(int page, int index) =>
            VariableCatalog.FindSlot(page, index) is { } binding
            && GoldenFilter.IgnoredVariables.Contains(VariableCatalog.Variables[binding.Var].Name);
    }
}
