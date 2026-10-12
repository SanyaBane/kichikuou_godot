using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// The Creator's phase <c>Tﾙﾄﾞﾗ</c> (<see cref="CreatorTurn"/>, milestone 5 step 1): the first phase's
/// scene, the counter, the seven searches with their give-ups, the spared provinces and the castle
/// roll, the Angel Knights' attacks and the earthquakes. Also compared with the decompile on random
/// states (Content tests, <c>NationTurnDifferentialTests</c>).
/// </summary>
public class CreatorTurnTests
{
    /// <summary>
    /// Stage 4 in the week after the first phase (the counter at 2): no province the player's yet,
    /// the Angel Knight (146) and Angel Knight (147) worn down, the province names at string 2000 on.
    /// Each <c>FIGT_B</c> call is recorded with its province, party and retreat mode.
    /// </summary>
    private sealed class CreatorRig
    {
        public CreatorRig(params int[] draws)
        {
            Random = new ScriptedRandom(draws);
            Script = new OriginalScriptContext(State, Random, new QuakeWatch(this), Input, Story);
            State.StoryStage = 4;
            State.N_Rudrasaum_Turns = 2;
            State.Prov_StrBase = 2000;
            for (int p = 1; p <= 91; p++)
            {
                State.Provs[p].Owner = 10;
                State.Strings[2000 + p - 1] = $"province {p}";
            }
            foreach (int knight in new[] { 146, 147 })
            {
                State.Cmdrs[knight].Status = 1;
                State.Cmdrs[knight].HP = 3;
                State.Cmdrs[knight].MaxHP = 17;
                State.Cmdrs[knight].Troops = 120;
                State.Cmdrs[knight].MaxTroops = 3500;
            }
            Story.OnCall = (file, _) =>
            {
                if (file == "FIGT_B.ADV")
                    Battles.Add((State.Prov_Cur.Value,
                        [State.Bt_EnemyParty1.Value, State.Bt_EnemyParty2.Value, State.Bt_EnemyParty3.Value, State.Bt_EnemyParty4.Value],
                        State.Bt_EnemyRetreatMode.Value));
            };
        }

        public GameState State { get; } = new();
        public ScriptedRandom Random { get; }
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public OriginalScriptContext Script { get; }

        /// <summary>Each <c>FIGT_B</c> call: the province, the enemy party and its retreat mode.</summary>
        public List<(int Prov, int[] Party, int Retreat)> Battles { get; } = [];

        /// <summary>The province of each earthquake, at its second message (<c>Tﾙﾄﾞﾗ.ADV:74</c>).</summary>
        public List<int> Quakes { get; } = [];

        public void Own(params int[] provinces)
        {
            foreach (int p in provinces)
                State.Provs[p].Owner = 1;
        }

        public Task RunAsync() => new CreatorTurn(Script).RunAsync();

        public List<int> Windows => View.Events.OfType<WindowEvent>().Select(w => (int)w.Op.Value).ToList();

        /// <summary>Records the events, and the cursor at each earthquake's second message.</summary>
        private sealed class QuakeWatch(CreatorRig rig) : IScriptView
        {
            public Task ShowAsync(ScriptEvent e)
            {
                if (e is MessageEvent { Text.Line: 74 })
                    rig.Quakes.Add(rig.State.Prov_Cur.Value);
                return rig.View.ShowAsync(e);
            }
        }
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, actual.Value);

    [Fact]
    public async Task The_first_phase_plays_Gﾙﾄﾞﾗ_whose_counter_of_1_the_phase_makes_2()
    {
        var rig = new CreatorRig(75, 75, 75, 75, 75, 75, 75);
        rig.State.N_Rudrasaum_Turns = 0;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Gﾙﾄﾞﾗ.ADV")
                rig.State.N_Rudrasaum_Turns = 1;                // Gﾙﾄﾞﾗ.ADV:5
        };

        await rig.RunAsync();

        Assert.Equal([("Gﾙﾄﾞﾗ.ADV", 1)], rig.Story.Calls);
        Is(2, rig.State.N_Rudrasaum_Turns);
        Is(10, rig.State.Ev_PhaseNation);
    }

    [Fact]
    public async Task Off_stage_4_the_first_phase_plays_no_scene()
    {
        var rig = new CreatorRig(75, 75, 75, 75, 75, 75, 75);
        rig.State.N_Rudrasaum_Turns = 0;
        rig.State.StoryStage = 3;

        await rig.RunAsync();

        Assert.Empty(rig.Story.Calls);
        Is(1, rig.State.N_Rudrasaum_Turns);
    }

    [Fact]
    public async Task Five_attacks_by_the_Angel_Knights_at_full_strength_then_two_earthquakes()
    {
        var rig = new CreatorRig(2, 3, 4, 5, 6, 7, 8);
        rig.Own(2, 3, 4, 5, 6, 7, 8);
        rig.State.Provs[7].Economy = 120;
        rig.State.Provs[7].Defense = 30;
        rig.State.Provs[7].BattleArea = 900;
        rig.State.Provs[7].BgCg = 1007;

        await rig.RunAsync();

        Assert.Equal([2, 3, 4, 5, 6], rig.Battles.Select(b => b.Prov));
        Assert.All(rig.Battles, b => Assert.Equal([146, 147, 0, 0], b.Party));
        Assert.All(rig.Battles, b => Assert.Equal(1, b.Retreat));
        foreach (int knight in new[] { 146, 147 })
        {
            Is(0, rig.State.Cmdrs[knight].Status);                  // NAT-11
            Is(17, rig.State.Cmdrs[knight].HP);
            Is(3500, rig.State.Cmdrs[knight].Troops);
        }
        Assert.Equal([7, 8], rig.Quakes);
        Is(0, rig.State.Provs[7].Economy);
        Is(0, rig.State.Provs[7].Defense);
        Is(1800, rig.State.Provs[7].BattleArea);
        Is(1043, rig.State.Provs[7].BgCg);
        Is(1043, rig.State.Provs[8].BgCg);
        Is(3, rig.State.N_Rudrasaum_Turns);
        Assert.Equal(Enumerable.Repeat(91, 7), rig.Random.Draws);
    }

    [Fact]
    public async Task The_earthquake_shows_the_province_and_its_name_then_the_ruin()
    {
        var rig = new CreatorRig(75, 75, 75, 75, 75, 9, 75);
        rig.Own(9);
        rig.State.Provs[9].MapX = 640;
        rig.State.Provs[9].MapY = 480;
        rig.State.Provs[9].BgCg = 1009;

        await rig.RunAsync();

        var events = rig.View.Events.Where(e => e is MapEvent or WindowEvent or MessageEvent).ToList();
        Assert.Equal(1, (int)Assert.IsType<MapEvent>(events[0]).Mode);
        Assert.Equal((331, 1009), Window(events[1]));               // the province's picture
        Assert.Equal((113, 1009), Window(events[2]));
        Assert.Equal(56, Assert.IsType<MessageEvent>(events[3]).Text.Line);     // "［<province>］"
        Assert.Equal([1, 0, 332], events.Skip(4).Take(3).Select(e => Window(e).Op));
        Assert.Equal((331, 1043), Window(events[7]));               // the ruin
        Assert.Equal(113, Window(events[8]).Op);
        Assert.Equal(74, Assert.IsType<MessageEvent>(events[9]).Text.Line);
        Assert.Equal([1, 0], events.Skip(10).Select(e => Window(e).Op));        // the ruin stays up
        Assert.Equal("province 9", rig.State.Strings[8]);
        Is(640, rig.State.MapCamX);
        Is(480, rig.State.MapCamY);
        Is(640, rig.State.Provs[9].MapX);                           // NAT-16: the jitter never runs
        Is(480, rig.State.Provs[9].MapY);
        Assert.Equal(Enumerable.Repeat(91, 7), rig.Random.Draws);

        static (int Op, int CallArg) Window(ScriptEvent e)
        {
            var w = Assert.IsType<WindowEvent>(e);
            return ((int)w.Op.Value, (int)w.CallArg.Value);
        }
    }

    [Fact]
    public async Task An_earthquake_doubles_the_battle_area_up_to_the_ceiling()
    {
        var rig = new CreatorRig(75, 75, 75, 75, 75, 9, 9);
        rig.Own(9);
        rig.State.Provs[9].BattleArea = 20000;

        await rig.RunAsync();

        Assert.Equal([9, 9], rig.Quakes);
        Is(65535, rig.State.Provs[9].BattleArea);                   // 40000, then saturated
    }

    [Fact]
    public async Task Below_5_Leazas_Castle_the_Ancient_Ruins_and_Siberia_are_spared_and_others_redrawn()
    {
        var rig = new CreatorRig(1, 35, 38, 20, 10, 75, 75, 75, 75, 75, 75);
        rig.Own(1, 35, 38, 10);
        rig.State.N_Rudrasaum_Turns = 3;                            // counted 4

        await rig.RunAsync();

        var battle = Assert.Single(rig.Battles);
        Assert.Equal(10, battle.Prov);
        Assert.Empty(rig.Quakes);
        Is(0, rig.State.Prov_Cur);
        Is(1, rig.State.Tmp0001);                                   // the last search: Nothing (75) on its first draw
        Assert.Equal(Enumerable.Repeat(91, 11), rig.Random.Draws);
    }

    [Fact]
    public async Task Nothing_ends_a_search_at_once_and_its_attack_is_skipped()
    {
        var rig = new CreatorRig(75, 75, 75, 75, 75, 75, 75);
        rig.Own(10);

        await rig.RunAsync();

        Assert.Empty(rig.Story.Calls);
        Assert.Empty(rig.Windows);
        Is(0, rig.State.Prov_Cur);
        Assert.Equal(Enumerable.Repeat(91, 7), rig.Random.Draws);
    }

    [Fact]
    public async Task A_search_gives_up_on_its_20th_try()
    {
        var rig = new CreatorRig([.. Enumerable.Repeat(20, 19), 75, 75, 75, 75, 75, 75]);

        await rig.RunAsync();

        Assert.Empty(rig.Story.Calls);
        Assert.Equal(25, rig.Random.Draws.Count);
        Assert.Equal(0, rig.Random.Remaining);
    }

    [Fact]
    public async Task At_5_nothing_is_spared_and_there_is_no_castle_roll()
    {
        var rig = new CreatorRig(1, 35, 38, 1, 35, 38, 1);
        rig.Own(1, 35, 38);
        rig.State.N_Rudrasaum_Turns = 4;                            // counted 5

        await rig.RunAsync();

        Assert.Equal([1, 35, 38, 1, 35], rig.Battles.Select(b => b.Prov));
        Assert.Equal([38, 1], rig.Quakes);
        Assert.Equal(Enumerable.Repeat(91, 7), rig.Random.Draws);
    }

    [Fact]
    public async Task From_6_on_a_draw_in_two_puts_Leazas_Castle_in_the_drawn_provinces_place()
    {
        // Nothing (75) gives up before the castle roll is drawn.
        var rig = new CreatorRig(10, 1, 10, 2, 20, 1, 75, 75, 75, 75);
        rig.Own(1, 10);
        rig.State.N_Rudrasaum_Turns = 5;                            // counted 6

        await rig.RunAsync();

        Assert.Equal([1, 10, 1], rig.Battles.Select(b => b.Prov));
        Assert.Empty(rig.Quakes);
        Assert.Equal([91, 2, 91, 2, 91, 2, 91, 91, 91, 91], rig.Random.Draws);
    }

    [Fact]
    public async Task A_battle_that_jumps_to_the_ending_ends_the_phase()
    {
        var rig = new CreatorRig(2);
        rig.Own(2);
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "FIGT_B.ADV")
                ScriptContext.JumpTo("OVER.ADV");
        };

        var e = await Assert.ThrowsAsync<ScenarioJumpException>(rig.RunAsync);

        Assert.Equal("OVER.ADV", e.File);
        Assert.Single(rig.Story.Calls);
        Assert.Equal(0, rig.Random.Remaining);
    }

    [Fact]
    public void The_phase_is_routed_and_its_empty_defender_file_is_not()
    {
        Assert.Contains("Tﾙﾄﾞﾗ.ADV", StrategyRules.PortedFiles);
        Assert.DoesNotContain("Tﾙﾄﾞﾗ2.ADV", StrategyRules.PortedFiles);   // called by nothing
    }
}
