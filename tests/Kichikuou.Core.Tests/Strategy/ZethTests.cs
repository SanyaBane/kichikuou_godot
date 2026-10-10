using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// Zeth's files (milestone 3 step 2): the defender picker <c>Tｾﾞｽ2</c> (<see cref="ZethDefenders"/>).
/// Also compared with the decompile on random states (Content tests, <c>NationTurnDifferentialTests</c>).
/// </summary>
public class ZethTests
{
    /// <summary>
    /// The Zeth war on, every Zeth leader 101-128 alive with 300 troops, the garrison Wall (94), Wall
    /// (95), Mage Leader (96) with maxima of 400, 300 and 200 troops and 50 HP, a party of Rance (1)
    /// and Sill (2); the draws scripted.
    /// </summary>
    private sealed class ZethRig
    {
        public ZethRig(params int[] draws)
        {
            Random = new ScriptedRandom(draws);
            Script = new ScriptContext(State, Random, View, Input, Story);
            State.F_Zeth_War = 1;
            for (int c = 101; c <= 128; c++)
            {
                State.Cmdrs[c].Status = 1;
                State.Cmdrs[c].Troops = 300;
            }
            int[] maxima = [400, 300, 200];
            for (int k = 0; k < 3; k++)
            {
                State.Cmdrs[94 + k].MaxTroops = (Sys3Int)maxima[k];
                State.Cmdrs[94 + k].MaxHP = 50;
            }
            State.Bt_PlayerParty1 = 1;
            State.Bt_PlayerParty2 = 2;
        }

        public GameState State { get; } = new();
        public ScriptedRandom Random { get; }
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public ScriptContext Script { get; }

        public int[] Party =>
            [State.Bt_EnemyParty1.Value, State.Bt_EnemyParty2.Value, State.Bt_EnemyParty3.Value, State.Bt_EnemyParty4.Value];

        /// <summary>An attack on <paramref name="province"/>; unless <paramref name="again"/>, not the province attacked last.</summary>
        public Task DefendersAsync(int province, bool again = false)
        {
            State.Prov_Cur = (Sys3Int)province;
            if (!again && State.Bt_GarrisonProv.Value == province)
                State.Bt_GarrisonProv = 0;
            return new ZethDefenders(Script, new Literals()).RunAsync();
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
    public async Task The_first_attack_starts_the_war_and_the_garrison_defends_at_full_strength()
    {
        var rig = new ZethRig();
        rig.State.F_Zeth_War = 0;
        rig.State.Bt_GarrisonProv = 58;
        rig.State.Bt_GarrisonHits = 3;
        rig.State.Provs[59].Defense = 4;

        await rig.DefendersAsync(59);                           // Sabasaba

        Assert.Equal([("Iｾﾞｽ.ADV", 1)], rig.Story.Calls);
        Is(1, rig.State.F_Zeth_War);
        Is(7, rig.State.Ev_PhaseNation);
        Assert.Equal("Tｾﾞｽ2.ADV:3", rig.State.Strings[10]);
        Assert.Equal([94, 95, 96, 0], rig.Party);
        Is(400, rig.State.Cmdrs[94].Troops);
        Is(200, rig.State.Cmdrs[96].Troops);
        Is(50, rig.State.Cmdrs[95].HP);
        Is(1, rig.State.Cmdrs[96].Status);
        Is(59, rig.State.Bt_GarrisonProv);                      // a new province: the count starts again
        Is(0, rig.State.Bt_GarrisonHits);
        Is(4, rig.State.Bt_WallBonus);
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task Attacked_again_the_garrison_loses_up_to_half_its_troops_each()
    {
        var rig = new ZethRig(150, 1);
        rig.State.Bt_GarrisonProv = 59;
        rig.State.Bt_GarrisonHits = 2;
        rig.State.Cmdrs[96].MaxTroops = 1;                      // Y 4, 0: no draw

        await rig.DefendersAsync(59, again: true);

        Assert.Equal([200, 150], rig.Random.Draws);
        Is(250, rig.State.Cmdrs[94].Troops);
        Is(299, rig.State.Cmdrs[95].Troops);
        Is(1, rig.State.Cmdrs[96].Troops);
        Is(2, rig.State.Bt_GarrisonHits);                       // kept
    }

    [Fact]
    public async Task Parapara_Fortress_is_fought_without_walls()
    {
        var rig = new ZethRig();
        rig.State.Provs[16].Defense = 9;
        rig.State.Provs[16].BattleArea = 300;

        await rig.DefendersAsync(16);

        Is(0, rig.State.Bt_WallBonus);
        Is(5000, rig.State.Bt_TroopLimit);
        Assert.Equal([94, 95, 96, 0], rig.Party);
    }

    [Fact]
    public async Task A_generals_city_sends_his_army_and_the_general_alone_gets_the_two_Walls()
    {
        var rig = new ZethRig();

        await rig.DefendersAsync(58);                           // Adam's Fortress
        Assert.Equal([115, 116, 114, 113], rig.Party);
        Is(1, rig.State.Bt_EnemyRetreatMode);

        for (int c = 114; c <= 116; c++)
            rig.State.Cmdrs[c].Status = 0;
        await rig.DefendersAsync(58);
        Assert.Equal([94, 95, 113, 0], rig.Party);
        Is(10, rig.State.Cmdrs[94].Troops);
        Is(10, rig.State.Cmdrs[95].Troops);

        rig.State.Cmdrs[113].Status = 0;                        // the general dead: the garrison
        await rig.DefendersAsync(58);
        Assert.Equal([94, 95, 96, 0], rig.Party);
        Is(400, rig.State.Cmdrs[94].Troops);
    }

    [Fact]
    public async Task The_barrier_sends_the_army_back_unless_a_unit_holds_the_Barrier_Blade()
    {
        var rig = new ZethRig();

        await rig.DefendersAsync(67);                           // Nagi's Tower
        Is(0, rig.State.Bt_PlayerParty1);                       // Cﾃｷﾄｼ.ADV:381-386 ends the attack
        Assert.Empty(rig.Story.Calls);

        rig.State.Bt_PlayerParty1 = 1;
        rig.State.Units[2].Item = 12;                           // Sill holds the Barrier Blade
        await rig.DefendersAsync(67);
        Assert.Equal([("Gﾅｷﾞ.ADV", 5)], rig.Story.Calls);
        Assert.Equal([108, 109, 107, 0], rig.Party);
        Is(1, rig.State.Bt_PlayerParty1);

        rig.State.Units[2].Item = 0;
        rig.State.F_ZethBarrier_State = 2;                      // the barrier is down
        await rig.DefendersAsync(68);                           // Chizuko's Tower
        Assert.Equal(("Gﾁｽﾞｺ.ADV", 1), rig.Story.Calls[^1]);
        Assert.Equal([103, 102, 101, 0], rig.Party);
    }

    [Fact]
    public async Task At_Magic_s_Tower_her_scene_may_send_Alex_and_without_her_the_Mage_Leader_defends_alone()
    {
        var rig = new ZethRig();
        rig.State.F_ZethBarrier_State = 2;
        rig.Story.OnCall = (file, entry) =>
        {
            if (file == "Gﾏｼﾞｯｸ.ADV")
                rig.State.Tmp0001 = 1;
        };

        await rig.DefendersAsync(66);
        Assert.Equal([("Gﾏｼﾞｯｸ.ADV", 1)], rig.Story.Calls);
        Assert.Equal([128, 127, 126, 125], rig.Party);

        rig.Story.OnCall = null;
        await rig.DefendersAsync(66);
        Assert.Equal([112, 111, 110, 0], rig.Party);

        rig.State.Cmdrs[110].Status = 0;
        await rig.DefendersAsync(66);
        Assert.Equal([96, 0, 0, 0], rig.Party);
    }

    [Fact]
    public async Task Two_units_or_more_set_Papayas_trap()
    {
        var rig = new ZethRig();
        rig.State.F_Papaya_State = 1;
        rig.State.Bt_PlayerParty2 = 0;

        await rig.DefendersAsync(59);
        Is(1, rig.State.F_Papaya_State);

        rig.State.Bt_PlayerParty2 = 2;
        await rig.DefendersAsync(59);
        Is(2, rig.State.F_Papaya_State);                        // Cﾃｷﾄｼ.ADV:388-394 plays Gﾊﾟﾊﾟｲｱ 2
    }

    [Fact]
    public void Tｾﾞｽ2_is_routed() => Assert.Contains("Tｾﾞｽ2.ADV", StrategyRules.PortedFiles);
}
