using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// Helman's files (milestone 3 step 1): the defender picker <c>Tﾍﾙﾏﾝ2</c>
/// (<see cref="HelmanDefenders"/>), the nation turn <c>Tﾍﾙﾏﾝ</c> (<see cref="HelmanTurn"/>) and the
/// Patton army's turn after Helman's fall, <c>Tﾊﾟｯﾄﾝ</c> (<see cref="PattonTurn"/>).
/// Also compared with the decompile on random states (Content
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

        public Task TurnAsync() => new HelmanTurn(Script, new Literals()).RunAsync();

        /// <summary>The lines of the messages shown, in order.</summary>
        public List<int> Lines => [.. View.Events.OfType<MessageEvent>().Select(m => m.Text.Line)];
    }

    /// <summary>
    /// A turn where nothing marches unless a test lets it: Freya's scene played, Lelyukov
    /// (commander 22) and Rolex (commander 50) dead, the other armies' flags at 0, no province the
    /// player's.
    /// </summary>
    private static HelmanRig QuietWeek(params int[] draws)
    {
        var rig = new HelmanRig(draws);
        rig.State.N_Freya_State = 10;
        rig.State.Cmdrs[22].Status = 0;
        rig.State.Cmdrs[50].Status = 0;
        return rig;
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

    // -------------------------------------------------------------------------------------------
    // Tﾍﾙﾏﾝ, the nation turn
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task A_quiet_week_draws_once_brings_Tainyar_back_and_ends_with_Stessels_scene()
    {
        var rig = QuietWeek(2);
        rig.State.Cmdrs[26].Status = 0;
        rig.State.Cmdrs[26].MaxHP = 80;
        rig.State.Cmdrs[26].MaxTroops = 700;

        await rig.TurnAsync();

        Is(4, rig.State.Ev_PhaseNation);
        Assert.Equal("Tﾍﾙﾏﾝ.ADV:3", rig.State.Strings[10]);
        Assert.Equal([2], rig.Random.Draws);                    // :60, every turn
        Assert.Equal([("Gｽﾃｯｾﾙ.ADV", 14)], rig.Story.Calls);    // nothing marked the week
        Is(1, rig.State.Cmdrs[26].Status);                      // neither Log B nor Sudori 13 is the player's
        Is(80, rig.State.Cmdrs[26].HP);
        Is(700, rig.State.Cmdrs[26].Troops);
        Is(40, rig.State.Prov_Cur);                             // :66

        rig = QuietWeek(2);
        rig.State.Cmdrs[26].Status = 0;
        rig.State.Provs[41].Owner = 1;
        await rig.TurnAsync();
        Is(0, rig.State.Cmdrs[26].Status);
    }

    [Fact]
    public async Task The_1st_army_marches_whatever_its_strength_on_the_first_province_the_player_holds()
    {
        var rig = QuietWeek(2);
        rig.State.Cmdrs[22].Status = 1;
        for (int c = 22; c <= 30; c++)
            rig.State.Cmdrs[c].Troops = 10;                     // all under 190: one commander each order
        rig.State.Provs[31].Owner = 1;                          // Log B
        rig.State.Provs[34].Owner = 1;                          // Golak, earlier in the list

        await rig.TurnAsync();

        Assert.Equal([("FIGT_B.ADV", 0)], rig.Story.Calls);     // the week is marked: no scene 14
        Is(34, rig.State.Prov_Cur);
        Assert.Equal("Tﾍﾙﾏﾝ.ADV:101", rig.State.Strings[10]);
        Is(3, rig.State.Bt_EnemyRetreatMode);
        // Jackal's order and Laumiere's both muster 10: on a tie the second stays.
        Is(10, rig.State.Tmp0011);
        Is(10, rig.State.Tmp0012);
        Assert.Equal([27, 0, 0, 0], rig.Party);
        Is(1, rig.State.Ev_WeeklyEventDone);
    }

    [Fact]
    public async Task At_Maunes_a_roll_sends_Lelyukovs_scene_or_Tainyar_alone()
    {
        var rig = QuietWeek(2, 1, 2, 2);
        rig.State.Cmdrs[22].Status = 1;
        rig.State.Provs[13].Owner = 1;

        await rig.TurnAsync();
        Assert.Equal([("Gﾚﾘｭｰｺﾌ.ADV", 5)], rig.Story.Calls);   // its scene has the battle

        await rig.TurnAsync();
        Assert.Equal(("FIGT_B.ADV", 5), rig.Story.Calls[^1]);
        Assert.Equal([26, 0, 0, 0], rig.Party);
        Is(13, rig.State.Prov_Cur);
        Assert.Equal([2, 2, 2, 2], rig.Random.Draws);
    }

    [Fact]
    public async Task The_1st_army_takes_Maunes_from_the_monsters_without_a_battle()
    {
        var rig = QuietWeek(2);
        rig.State.Cmdrs[22].Status = 1;
        rig.State.Provs[13].Owner = 8;
        rig.State.Provs[13].MapX = 300;
        rig.State.Provs[13].MapY = 200;
        rig.State.Provs[13].BgCg = 1010;

        await rig.TurnAsync();

        Assert.Empty(rig.Story.Calls);
        Is(4, rig.State.Provs[13].Owner);
        Assert.Equal([689, 695, 702], rig.Lines);               // name, "VS Monster Army", "took control"
        Assert.Equal([1, 4, 3], rig.View.Events.OfType<MapEvent>().Select(e => e.Mode.Value));
        Assert.Equal(3, rig.View.Events.OfType<WindowEvent>().Count(e => e.Op == 1));
    }

    [Fact]
    public async Task Pattons_second_scene_needs_another_scene_to_have_played_that_week()
    {
        var rig = QuietWeek(1, 1);
        rig.State.F_Patton_State = 1;

        await rig.TurnAsync();
        // QUIRK: NAT-08 — the roll came up 1, but nothing had marked the week.
        Assert.Equal([("Gｽﾃｯｾﾙ.ADV", 14)], rig.Story.Calls);

        rig.Story.Calls.Clear();
        rig.State.F_Lelyukov_State = 1;                         // his retreat scene marks the week
        await rig.TurnAsync();
        Assert.Equal([("Gﾚﾘｭｰｺﾌ.ADV", 2), ("Gﾊﾟｯﾄﾝ.ADV", 2)], rig.Story.Calls);
    }

    [Fact]
    public async Task The_2nd_army_waits_for_Aristoles_and_marches_with_1500_troops()
    {
        var rig = QuietWeek(2, 2, 2);
        rig.State.F_Aristoles_Helman2ndState = 1;
        rig.State.Provs[44].Owner = 1;                          // Micrograd

        await rig.TurnAsync();
        Is(2, rig.State.F_Aristoles_Helman2ndState);
        Is(1200, rig.State.CallArg);                            // 37, 38, 34, 35: too weak
        Assert.Equal([("Gｽﾃｯｾﾙ.ADV", 14)], rig.Story.Calls);

        for (int c = 32; c <= 38; c++)
            rig.State.Cmdrs[c].Troops = 400;
        rig.Story.Calls.Clear();
        await rig.TurnAsync();
        Is(3, rig.State.F_Aristoles_Helman2ndState);
        Assert.Equal([("FIGT_B.ADV", 14)], rig.Story.Calls);
        Assert.Equal("Tﾍﾙﾏﾝ.ADV:223", rig.State.Strings[10]);
        Is(4, rig.State.Bt_EnemyRetreatMode);

        rig.Story.Calls.Clear();
        await rig.TurnAsync();
        Assert.Equal([("Gｱﾘｽﾄﾚｽ.ADV", 5), ("FIGT_B.ADV", 5)], rig.Story.Calls);
    }

    [Fact]
    public async Task At_Micrograd_the_3rd_army_marches_again_without_Capricorn_and_Ortega()
    {
        var rig = QuietWeek(2);
        rig.State.F_Mineva_State = 1;
        rig.State.Cmdrs[184].Status = 1;
        rig.State.Cmdrs[184].Troops = 1000;
        rig.State.Cmdrs[185].Status = 1;
        rig.State.Cmdrs[185].Troops = 1000;
        rig.State.Provs[44].Owner = 1;

        await rig.TurnAsync();

        Assert.Equal([("Gﾐﾈﾊﾞ.ADV", 1), ("Gﾐﾈﾊﾞ.ADV", 4), ("FIGT_B.ADV", 4)], rig.Story.Calls);
        Is(2600, rig.State.CallArg);                            // 184, 185, 42, 43 — not summed again
        Assert.Equal([42, 43, 39, 40], rig.Party);
        Is(44, rig.State.Prov_Cur);
        Is(3, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task The_4th_army_counts_Neros_states_and_once_he_turns_marches_without_marking_the_week()
    {
        var rig = QuietWeek(2, 2);
        rig.State.F_Nero_Helman4thState = 1;
        rig.State.Provs[40].Owner = 1;                          // Pawn

        await rig.TurnAsync();
        Is(2, rig.State.F_Nero_Helman4thState);
        Assert.Equal([("FIGT_B.ADV", 0)], rig.Story.Calls);
        Assert.Equal([49, 46, 47, 44], rig.Party);
        Is(4, rig.State.Bt_EnemyRetreatMode);

        rig.Story.Calls.Clear();
        rig.State.F_Nero_Helman4thState = 6;
        await rig.TurnAsync();
        Assert.Equal([47, 46, 49, 45], rig.Party);
        Is(1200, rig.State.CallArg);
        Is(3, rig.State.Bt_EnemyRetreatMode);
        Assert.Equal("Tﾍﾙﾏﾝ.ADV:386", rig.State.Strings[10]);
        // QUIRK: NAT-18 — the battle leaves the week unmarked: the quiet week's scene follows.
        Assert.Equal([("FIGT_B.ADV", 0), ("Gｽﾃｯｾﾙ.ADV", 14)], rig.Story.Calls);
    }

    [Fact]
    public async Task Rolex_takes_two_rolls_and_two_scenes_before_the_5th_army_marches()
    {
        var rig = QuietWeek(2, 1);
        rig.State.Cmdrs[50].Status = 1;

        await rig.TurnAsync();
        Is(1, rig.State.F_Rolex_State);
        Assert.Equal([2, 2], rig.Random.Draws);                 // the first roll ended the army's turn
        Assert.Equal([("Gｽﾃｯｾﾙ.ADV", 14)], rig.Story.Calls);

        rig = QuietWeek(2, 2, 2);
        rig.State.Cmdrs[50].Status = 1;
        rig.State.F_Rolex_State = 4;
        rig.State.Provs[39].Owner = 1;                          // Rosesgrad
        await rig.TurnAsync();
        Assert.Equal([("FIGT_B.ADV", 0)], rig.Story.Calls);
        Assert.Equal([53, 50, 52, 51], rig.Party);
        Assert.Equal("Tﾍﾙﾏﾝ.ADV:449", rig.State.Strings[10]);
        Assert.Equal([2, 2, 2], rig.Random.Draws);
    }

    [Fact]
    public async Task The_Crusaders_bring_Olhound_back_with_fresh_troops_every_turn()
    {
        var rig = QuietWeek(2, 250);
        rig.State.N_Olhound_Stage = 1;
        rig.State.Cmdrs[60].MaxHP = 90;
        rig.State.Provs[43].Owner = 1;                          // Path

        await rig.TurnAsync();

        Assert.Equal([("Gｵｰﾙﾊｳﾝﾄ.ADV", 1), ("FIGT_B.ADV", 1)], rig.Story.Calls);
        Assert.Equal([2, 500], rig.Random.Draws);
        Assert.Equal([60, 0, 0, 0], rig.Party);
        Is(1, rig.State.Cmdrs[60].Status);
        Is(90, rig.State.Cmdrs[60].HP);
        Is(750, rig.State.Cmdrs[60].Troops);
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Is(43, rig.State.Prov_Cur);
    }

    [Fact]
    public void Tﾍﾙﾏﾝ_is_routed() => Assert.Contains("Tﾍﾙﾏﾝ.ADV", StrategyRules.PortedFiles);

    // -------------------------------------------------------------------------------------------
    // Tﾊﾟｯﾄﾝ, the Patton army's turn after Helman's fall
    // -------------------------------------------------------------------------------------------

    private static Task PattonTurnAsync(HelmanRig rig) => new PattonTurn(rig.Script, new Literals()).RunAsync();

    [Fact]
    public async Task The_Patton_army_plays_its_story_scene_and_marches_on_the_first_province_the_player_holds()
    {
        var rig = new HelmanRig();
        rig.State.F_PattonArmy_State = 1;
        rig.State.Provs[41].Owner = 1;                          // Sudori 13
        rig.State.Provs[45].Owner = 1;                          // Lang Bau, later in the list

        await PattonTurnAsync(rig);

        Is(5, rig.State.Ev_PhaseNation);
        Assert.Equal("Tﾊﾟｯﾄﾝ.ADV:4", rig.State.Strings[10]);
        Assert.Equal([("Gﾊﾟｯﾄﾝ.ADV", 7), ("FIGT_B.ADV", 7)], rig.Story.Calls);
        Assert.Equal([54, 55, 56, 57], rig.Party);
        Is(1200, rig.State.CallArg);
        Is(41, rig.State.Prov_Cur);
        Is(4, rig.State.Bt_EnemyRetreatMode);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task Aristoles_joins_once_past_state_9_and_a_weak_army_stays_home()
    {
        var rig = new HelmanRig();
        rig.State.F_Aristoles_Helman2ndState = 10;
        rig.State.Cmdrs[55].Status = 0;
        for (int c = 54; c <= 57; c++)
            rig.State.Cmdrs[c].Troops = 100;
        rig.State.Cmdrs[32].Troops = 150;
        rig.State.Provs[46].Owner = 1;                          // Sudori 17

        await PattonTurnAsync(rig);

        // Nobody is left out for weakness, but 100 × 3 + 150 is under 500.
        Assert.Equal([54, 56, 57, 32], rig.Party);
        Is(450, rig.State.CallArg);
        Assert.Empty(rig.Story.Calls);

        rig.State.Cmdrs[32].Troops = 200;
        await PattonTurnAsync(rig);
        Assert.Equal([("FIGT_B.ADV", 0)], rig.Story.Calls);
        Is(46, rig.State.Prov_Cur);
    }

    [Fact]
    public async Task The_story_scenes_are_tested_in_order_each_after_the_one_before()
    {
        var rig = new HelmanRig();
        rig.State.F_PattonArmy_State = 6;
        rig.Story.OnCall = (file, entry) =>
        {
            if (file == "Gﾊﾟｯﾄﾝ.ADV" && entry == 11)
                rig.State.F_PattonArmy_State = 3;               // a scene moving the story on
            if (file == "Gﾊﾟｯﾄﾝ.ADV" && entry == 9)
                rig.State.Bt_EnemyRetreatMode = 1;
        };

        await PattonTurnAsync(rig);

        Assert.Equal([("Gﾊﾟｯﾄﾝ.ADV", 11), ("Gﾊﾟｯﾄﾝ.ADV", 9)], rig.Story.Calls);
        Is(1, rig.State.Bt_EnemyRetreatMode);                   // set once, before the scenes

        rig.Story.Calls.Clear();
        rig.Story.OnCall = null;
        rig.State.F_PattonArmy_State = 5;
        await PattonTurnAsync(rig);
        Assert.Equal([("Gｱﾘｽﾄﾚｽ.ADV", 3)], rig.Story.Calls);
    }

    [Fact]
    public void Tﾊﾟｯﾄﾝ_is_routed() => Assert.Contains("Tﾊﾟｯﾄﾝ.ADV", StrategyRules.PortedFiles);
}
