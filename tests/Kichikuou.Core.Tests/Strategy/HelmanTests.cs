using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// Helman's files (milestone 3 step 1): the defender picker <c>Tﾍﾙﾏﾝ2</c>
/// (<see cref="HelmanDefenders"/>). Also compared with the decompile on random states (Content
/// tests, <c>NationTurnDifferentialTests</c>).
/// </summary>
public class HelmanTests
{
    /// <summary>
    /// The Helman war on (<c>Gｽﾃｯｾﾙ</c> 1 played), every Helman commander 22-59 alive with 300
    /// troops; the draws scripted.
    /// </summary>
    private sealed class HelmanRig
    {
        public HelmanRig(params int[] draws)
        {
            Random = new ScriptedRandom(draws);
            Script = new ScriptContext(State, Random, View, Input, Story);
            State.F_Helman_War = 1;
            State.F_StesselSheila_Stage = 1;
            for (int c = 22; c <= 59; c++)
            {
                State.Cmdrs[c].Status = 1;
                State.Cmdrs[c].Troops = 300;
                State.Cmdrs[c].MaxHP = 100;
            }
        }

        public GameState State { get; } = new();
        public ScriptedRandom Random { get; }
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public ScriptContext Script { get; }

        public int[] Party =>
            [State.Bt_EnemyParty1.Value, State.Bt_EnemyParty2.Value, State.Bt_EnemyParty3.Value, State.Bt_EnemyParty4.Value];

        public Task DefendersAsync(int province, int owner = 4)
        {
            State.Prov_Cur = (Sys3Int)province;
            State.Provs[province].Owner = (Sys3Int)owner;
            return new HelmanDefenders(Script, new Literals()).RunAsync();
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
    public async Task The_first_attack_on_Helman_plays_Stessels_scene_and_the_1st_army_holds_the_pass()
    {
        var rig = new HelmanRig();
        rig.State.F_StesselSheila_Stage = 0;
        rig.State.Provs[31].BattleArea = 120;
        rig.State.Provs[31].Defense = 3;

        await rig.DefendersAsync(31);                           // Log B, one unit through the pass

        Assert.Equal([("Gｽﾃｯｾﾙ.ADV", 1)], rig.Story.Calls);
        Is(4, rig.State.Ev_PhaseNation);
        Assert.Equal("Tﾍﾙﾏﾝ2.ADV:28", rig.State.Strings[10]);
        Is(120, rig.State.Bt_TroopLimit);
        Is(3, rig.State.Bt_WallBonus);
        // Both orders muster 1200: on a tie the second (Laumiere first) stays.
        Assert.Equal([27, 28, 29, 30], rig.Party);
        Is(1200, rig.State.Tmp0011);
        Is(1200, rig.State.Tmp0012);
        Is(1, rig.State.Bt_EnemyRetreatMode);                   // a single unit at the pass
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task The_1st_army_takes_its_stronger_order_and_leaves_out_the_weak()
    {
        var rig = new HelmanRig();
        rig.State.Bt_PlayerParty2 = 5;
        rig.State.Cmdrs[28].Troops = 100;                       // under 190: left out once slot 1 is taken
        rig.State.Cmdrs[29].Status = 0;

        await rig.DefendersAsync(38);                           // Siberia

        // First order: 22, 23, 24, 25 = 1200; second: 27, 30, 24, 25 (28 too weak, 29 dead) = 1200
        // — a tie, the second stays.
        Assert.Equal([27, 30, 24, 25], rig.Party);
        Is(4, rig.State.Bt_EnemyRetreatMode);

        rig.State.Cmdrs[27].Troops = 200;                       // the second order now musters 1100
        await rig.DefendersAsync(38);
        Assert.Equal([22, 23, 24, 25], rig.Party);
        Is(1200, rig.State.Tmp0011);
        Is(1100, rig.State.Tmp0012);
    }

    [Fact]
    public async Task At_Volgo_Z_Laumiere_defends_alone_while_she_lives()
    {
        var rig = new HelmanRig();

        await rig.DefendersAsync(30);

        Assert.Equal([27, 0, 0, 0], rig.Party);
        Is(27, rig.State.Tmp0001);
        Is(0, rig.State.Tmp0002);
    }

    [Fact]
    public async Task Without_Lelyukov_the_militia_defends_with_its_two_draws()
    {
        var rig = new HelmanRig(400, 7);
        rig.State.Cmdrs[22].Status = 0;
        rig.State.Cmdrs[58].Status = 0;

        await rig.DefendersAsync(34);                           // Golak

        Assert.Equal([58, 59, 0, 0], rig.Party);
        Assert.Equal([800, 800], rig.Random.Draws);
        Is(1, rig.State.Cmdrs[58].Status);
        Is(500, rig.State.Cmdrs[58].Troops);
        Is(107, rig.State.Cmdrs[59].Troops);
        Is(100, rig.State.Cmdrs[59].HP);
        Is(1, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task At_Lang_Bau_the_3rd_army_calls_up_the_militia_and_takes_everyone()
    {
        var rig = new HelmanRig(1, 1);
        rig.State.Cmdrs[42].Troops = 10;
        rig.State.Cmdrs[43].Troops = 10;
        rig.State.Cmdrs[40].Status = 0;

        await rig.DefendersAsync(45);

        // No filter at Lang Bau: the weak 42 and 43 fight; the militia fills the last slot.
        Assert.Equal([42, 43, 39, 41], rig.Party);
        Is(58, rig.State.Tmp0006);
        Is(59, rig.State.Tmp0007);
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Assert.Equal(2, rig.Random.Draws.Count);
    }

    [Fact]
    public async Task At_Micrograd_the_militia_holds_until_Mineva_is_met()
    {
        var rig = new HelmanRig(1, 1);

        await rig.DefendersAsync(44);

        Is(1, rig.State.F_Mineva_State);                        // :6-8 — met on the way in
        Assert.Equal([42, 43, 39, 40], rig.Party);
        Is(4, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task After_Helman_falls_its_people_defend_and_Sheila_may_call_the_attack_off()
    {
        var rig = new HelmanRig(1, 1);
        rig.State.F_Helman_War = 2;
        rig.State.F_Sheila_State = 3;
        rig.State.F_Sheila_Flag = 1;
        rig.Story.OnCall = (file, entry) =>
        {
            if (file == "Gｼｰﾗ.ADV" && entry == 2)
                rig.State.Tmp0001 = 1;
        };

        await rig.DefendersAsync(40);

        Assert.Equal([("Gｼｰﾗ.ADV", 2)], rig.Story.Calls);
        Assert.Equal("Tﾍﾙﾏﾝ2.ADV:11", rig.State.Strings[10]);
        Is(43, rig.State.Unit_Cur);
        Is(0, rig.State.Bt_EnemyParty1);                        // Cﾃｷﾄｼ.ADV:395 takes the city unopposed
        Assert.Empty(rig.Random.Draws);

        rig.Story.OnCall = null;
        rig.State.F_Sheila_Flag = 0;
        await rig.DefendersAsync(40);
        Assert.Equal(("Gｼｰﾗ.ADV", 1), rig.Story.Calls[^1]);
        Assert.Equal([58, 59, 0, 0], rig.Party);
    }

    [Fact]
    public async Task The_4th_army_plays_Neros_first_scene_and_the_Ancient_Ruins_Ruberans()
    {
        var rig = new HelmanRig();

        await rig.DefendersAsync(37);                           // Cossack
        Assert.Equal([49, 46, 47, 44], rig.Party);
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Assert.Equal(("Gﾈﾛ.ADV", 1), rig.Story.Calls[^1]);

        rig.State.Cmdrs[31].Status = 1;
        await rig.DefendersAsync(35);
        Assert.Equal([31, 0, 0, 0], rig.Party);
        Assert.Equal(("Gﾙｰﾍﾞﾗﾝ.ADV", 1), rig.Story.Calls[^1]);
    }

    [Fact]
    public async Task Archgrad_after_Lelyukov_fell_back_is_his_last_stand()
    {
        var rig = new HelmanRig();
        rig.State.F_Lelyukov_State = 1;

        await rig.DefendersAsync(36);

        Assert.Equal([22, 0, 0, 0], rig.Party);
        Is(10, rig.State.Cmdrs[22].Troops);
        Is(10, rig.State.F_Lelyukov_State);
        Is(1, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public void Tﾍﾙﾏﾝ2_is_routed() => Assert.Contains("Tﾍﾙﾏﾝ2.ADV", StrategyRules.PortedFiles);
}
