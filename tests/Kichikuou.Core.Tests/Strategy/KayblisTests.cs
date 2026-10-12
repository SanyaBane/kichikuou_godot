using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// Kayblis's files (milestone 3 step 3): the defender picker <c>Tｹｲﾌﾞﾘｽ2</c>
/// (<see cref="KayblisDefenders"/>). Also compared with the decompile on random states (Content
/// tests, <c>NationTurnDifferentialTests</c>).
/// </summary>
public class KayblisTests
{
    /// <summary>
    /// Story stage 3, the Monster Generals (86-89) with maxima of 800, 400, 400, 400 and 60 HP, the
    /// named commanders alive (Camilla (91) with her maximum of 200), Warg met; the draws scripted.
    /// </summary>
    private sealed class KayblisRig
    {
        public KayblisRig(params int[] draws)
        {
            Random = new ScriptedRandom(draws);
            Script = new OriginalScriptContext(State, Random, View, Input, Story);
            State.StoryStage = 3;
            State.N_Warg_Stage = 1;
            int[] maxima = [800, 400, 400, 400];
            for (int k = 0; k < 4; k++)
            {
                State.Cmdrs[86 + k].MaxTroops = (Sys3Int)maxima[k];
                State.Cmdrs[86 + k].MaxHP = 60;
            }
            foreach (int c in new[] { 91, 133, 134, 135, 159, 160, 161, 162, 163, 164, 181, 182, 183 })
                State.Cmdrs[c].Status = 1;
            State.Cmdrs[91].MaxTroops = 200;
        }

        public GameState State { get; } = new();
        public ScriptedRandom Random { get; }
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public OriginalScriptContext Script { get; }

        public int[] Party =>
            [State.Bt_EnemyParty1.Value, State.Bt_EnemyParty2.Value, State.Bt_EnemyParty3.Value, State.Bt_EnemyParty4.Value];

        /// <summary>
        /// An attack on <paramref name="province"/> with walls of 7 (zeroed by <c>Cﾃｷﾄｼ.ADV:317</c>
        /// first); unless <paramref name="again"/>, not the province attacked last.
        /// </summary>
        public Task DefendersAsync(int province, bool again = false)
        {
            State.Prov_Cur = (Sys3Int)province;
            State.Provs[province].Defense = 7;
            State.Provs[province].BattleArea = 900;
            State.Bt_WallBonus = 0;
            if (!again && State.Bt_GarrisonProv.Value == province)
                State.Bt_GarrisonProv = 0;
            return new KayblisDefenders(Script, new Literals()).RunAsync();
        }
    }

    /// <summary>The army name (<c>MS 10, "…"</c>) as "file:line", standing in for the player's files.</summary>
    private sealed class Literals : Kichikuou.Core.Setup.IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) => new SetStringStatement(at, 10, at.ToString());
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, actual.Value);

    [Fact]
    public async Task The_Monster_Generals_are_called_up_with_a_share_of_their_maximum_and_a_draw()
    {
        var rig = new KayblisRig(50, 10, 20, 30);
        rig.State.Cmdrs[86].HP = 1;

        await rig.DefendersAsync(80);                           // a province with no garrison of its own

        Is(8, rig.State.Ev_PhaseNation);
        Assert.Equal("Tｹｲﾌﾞﾘｽ2.ADV:3", rig.State.Strings[10]);
        Assert.Equal([200, 100, 100, 100], rig.Random.Draws);   // a quarter of the maximum
        Is(250, rig.State.Cmdrs[86].Troops);                    // 800 / 4 + 50
        Is(130, rig.State.Cmdrs[89].Troops);
        Is(60, rig.State.Cmdrs[86].HP);
        Is(1, rig.State.Cmdrs[88].Status);
        Is(80, rig.State.Bt_GarrisonProv);
        Assert.Equal([86, 87, 88, 89], rig.Party);
        Is(1, rig.State.Bt_EnemyRetreatMode);

        // The same province again after one hit: 1 / (4 + 2) and a draw of up to 1 / (2 + 1).
        rig = new KayblisRig(1, 1, 1, 1);
        rig.State.Bt_GarrisonProv = 80;
        rig.State.Bt_GarrisonHits = 1;
        await rig.DefendersAsync(80, again: true);
        Assert.Equal([266, 133, 133, 133], rig.Random.Draws);
        Is(134, rig.State.Cmdrs[86].Troops);                    // 800 / 6 + 1
        Is(1, rig.State.Bt_GarrisonHits);
    }

    [Fact]
    public async Task Before_stage_3_the_gate_moves_the_story_on_and_keeps_its_walls()
    {
        var rig = new KayblisRig(1, 1, 1, 1);
        rig.State.StoryStage = 2;

        await rig.DefendersAsync(89);                           // Grief Valley

        Is(3, rig.State.StoryStage);
        Is(7, rig.State.Bt_WallBonus);
        Assert.Equal([86, 87, 88, 89], rig.Party);
        Assert.Empty(rig.Story.Calls);

        // QUIRK: NAT-09 — Tearing Forest (91) only at stage 2; the other three at any other stage too.
        rig = new KayblisRig(1, 1, 1, 1, 2);
        rig.State.StoryStage = 1;
        await rig.DefendersAsync(91);
        Is(1, rig.State.StoryStage);
        rig = new KayblisRig(1, 1, 1, 1);
        rig.State.StoryStage = 4;
        await rig.DefendersAsync(90);                           // Bone Forest
        Is(3, rig.State.StoryStage);
    }

    [Fact]
    public async Task Elsewhere_before_stage_3_two_Monster_Generals_or_four_hold_the_plains()
    {
        var rig = new KayblisRig(1, 1, 1, 1, 2);
        rig.State.StoryStage = 2;

        await rig.DefendersAsync(80);                           // not the gate

        Assert.Equal([86, 87, 0, 0], rig.Party);
        Is(5000, rig.State.Bt_TroopLimit);
        Is(0, rig.State.Bt_WallBonus);
        Is(2, rig.State.StoryStage);

        rig = new KayblisRig(1, 1, 1, 1, 1);
        rig.State.StoryStage = 2;
        await rig.DefendersAsync(81);
        Assert.Equal([86, 87, 88, 89], rig.Party);
    }

    [Fact]
    public async Task At_the_Tower_of_Evil_Warg_replaces_the_battle_until_she_is_met()
    {
        var rig = new KayblisRig(1, 1, 1, 1);
        rig.State.N_Warg_Stage = 0;
        rig.State.Bt_EnemyParty1 = 42;

        await rig.DefendersAsync(78);

        Assert.Equal([("Gﾜｰｸﾞ.ADV", 1)], rig.Story.Calls);
        Is(42, rig.State.Bt_EnemyParty1);                       // no pick: Cﾃｷﾄｼ.ADV:356-358 ends the attack

        rig = new KayblisRig(1, 1, 1, 1);
        await rig.DefendersAsync(78);
        Assert.Empty(rig.Story.Calls);
        Assert.Equal([86, 87, 88, 89], rig.Party);
        Is(0, rig.State.Bt_WallBonus);                          // QUIRK: NAT-21
    }

    [Fact]
    public async Task At_stage_3_the_Houses_and_castles_have_their_own_garrisons()
    {
        var rig = new KayblisRig(1, 1, 1, 1);
        await rig.DefendersAsync(86);                           // the Blue House
        Assert.Equal([160, 161, 159, 0], rig.Party);

        rig = new KayblisRig(1, 1, 1, 1);
        await rig.DefendersAsync(85);                           // the Red House
        Assert.Equal([163, 164, 162, 0], rig.Party);

        rig = new KayblisRig(1, 1, 1, 1);
        await rig.DefendersAsync(87);                           // Kesselring's Castle
        Assert.Equal([133, 87, 88, 89], rig.Party);
        Is(0, rig.State.Bt_WallBonus);                          // QUIRK: NAT-21

        rig = new KayblisRig(1, 1, 1, 1);
        await rig.DefendersAsync(88);                           // Camilla's Castle
        Assert.Equal([91, 88, 0, 0], rig.Party);
        Is(2000, rig.State.Cmdrs[91].MaxTroops);                // QUIRK: NAT-10
        Is(2000, rig.State.Cmdrs[91].Troops);

        rig = new KayblisRig(1, 1, 1, 1);
        await rig.DefendersAsync(84);                           // Kayblis's Castle
        Assert.Equal([134, 87, 88, 0], rig.Party);
        Is(7, rig.State.Bt_WallBonus);

        rig = new KayblisRig(1, 1, 1, 1);
        rig.State.Cmdrs[134].Status = 0;
        await rig.DefendersAsync(84);
        Assert.Equal([86, 87, 88, 89], rig.Party);
        Is(0, rig.State.Bt_WallBonus);
    }

    [Fact]
    public async Task At_the_Demon_Lords_Castle_Kayblis_speaks_first_on_the_first_attack()
    {
        var rig = new KayblisRig(1, 1, 1, 1);

        await rig.DefendersAsync(79);

        Assert.Equal([("Gｹｲﾌﾞﾘｽ.ADV", 4)], rig.Story.Calls);
        Assert.Equal([181, 182, 183, 135], rig.Party);
        Is(7, rig.State.Bt_WallBonus);
        Is(1, rig.State.Bt_EnemyRetreatMode);

        rig = new KayblisRig(1, 1, 1, 1);
        rig.State.F_DemonCastle_Attacked = 1;
        await rig.DefendersAsync(79);
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public void Tｹｲﾌﾞﾘｽ2_is_routed() => Assert.Contains("Tｹｲﾌﾞﾘｽ2.ADV", StrategyRules.PortedFiles);
}
