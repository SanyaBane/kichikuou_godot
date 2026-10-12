using Kichikuou.Original.Tests.Combat;
using Kichikuou.Original.Tests.Script;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary>
/// JAPAN's files (milestone 3 step 5): the defender picker <c>TJAPAN2</c>
/// (<see cref="JapanDefenders"/>) and the turn <c>TJAPAN</c> (<see cref="JapanTurn"/>, with Gekkou's
/// and Shinobu's attempts). Also compared with the decompile on random states (Content tests,
/// <c>NationTurnDifferentialTests</c>).
/// </summary>
public class JapanTests
{
    /// <summary>JAPAN's commanders, Nobunaga Oda (72) to Isoroku Yamamoto (82), alive with 100 troops each; a stale retreat mode 50.</summary>
    private sealed class JapanRig
    {
        public JapanRig()
        {
            Script = new OriginalScriptContext(State, new ScriptedRandom(), View, Input, Story);
            for (int c = 72; c <= 82; c++)
            {
                State.Cmdrs[c].Status = 1;
                State.Cmdrs[c].Troops = 100;
            }
            State.Bt_EnemyRetreatMode = 50;
        }

        public GameState State { get; } = new();
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public OriginalScriptContext Script { get; }

        public int[] Party =>
            [State.Bt_EnemyParty1.Value, State.Bt_EnemyParty2.Value, State.Bt_EnemyParty3.Value, State.Bt_EnemyParty4.Value];

        /// <summary>An attack on <paramref name="province"/>, with walls of 7 and a battle area of 900.</summary>
        public Task DefendersAsync(int province)
        {
            State.Prov_Cur = (Sys3Int)province;
            State.Provs[province].Defense = 7;
            State.Provs[province].BattleArea = 900;
            return new JapanDefenders(Script, new Literals()).RunAsync();
        }
    }

    /// <summary>The army name (<c>MS 10, "…"</c>) as "file:line", standing in for the player's files.</summary>
    private sealed class Literals : IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) => new SetStringStatement(at, 10, at.ToString());
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, actual.Value);

    [Fact]
    public async Task Nagasaki_fields_Katsuie_to_Denden_Mori_and_starts_the_war_even_over_an_alliance()
    {
        var rig = new JapanRig();
        rig.State.F_Japan_State = 3;

        await rig.DefendersAsync(55);

        Is(6, rig.State.Ev_PhaseNation);
        Assert.Equal("TJAPAN2.ADV:3", rig.State.Strings[10]);
        Is(1, rig.State.F_Japan_State);
        Assert.Equal([75, 76, 77, 78], rig.Party);
        Is(7, rig.State.Bt_WallBonus);
        Is(900, rig.State.Bt_TroopLimit);
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public async Task Nagasaki_under_10_troops_and_elsewhere_under_500_keep_the_last_retreat_mode()
    {
        var rig = new JapanRig();
        for (int c = 75; c <= 78; c++)
            rig.State.Cmdrs[c].Troops = 2;                          // 8 in all
        await rig.DefendersAsync(55);
        Is(8, rig.State.CallArg);
        Is(50, rig.State.Bt_EnemyRetreatMode);                      // BFL-13

        rig = new JapanRig();                                       // 400 at most anywhere else
        await rig.DefendersAsync(57);
        Is(400, rig.State.CallArg);
        Is(50, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task Osaka_fields_the_whole_army_with_Nobunaga_first()
    {
        var rig = new JapanRig();
        rig.State.Cmdrs[73].Status = 0;                             // Kenichi Oda fallen

        await rig.DefendersAsync(56);

        Assert.Equal([72, 74, 78, 77], rig.Party);
        Is(1, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task Elsewhere_the_strongest_list_defends_and_the_earlier_wins_a_tie()
    {
        var rig = new JapanRig();
        rig.State.Cmdrs[80].Troops = 300;                           // the last four: 600
        await rig.DefendersAsync(57);
        Is(3, rig.State.Tmp0022);
        Assert.Equal([79, 80, 81, 82], rig.Party);
        Is(600, rig.State.CallArg);
        Is(4, rig.State.Bt_EnemyRetreatMode);

        rig = new JapanRig();
        rig.State.Cmdrs[76].Troops = 300;                           // the first four: 600, the whole army 400
        await rig.DefendersAsync(57);
        Is(2, rig.State.Tmp0022);
        Assert.Equal([75, 76, 77, 78], rig.Party);

        rig = new JapanRig();
        rig.State.Cmdrs[76].Troops = 300;
        rig.State.Cmdrs[72].Troops = 300;                           // the whole army: 600 too, and first
        await rig.DefendersAsync(57);
        Is(1, rig.State.Tmp0022);
        Assert.Equal([72, 73, 74, 78], rig.Party);
    }

    [Fact]
    public void TJAPAN2_is_routed() => Assert.Contains("TJAPAN2.ADV", StrategyRules.PortedFiles);

    /// <summary>
    /// JAPAN's turn on <paramref name="draws"/>: its commanders with 200 troops each, the walk's
    /// provinces held by nobody in particular (owner 4).
    /// </summary>
    private static (JapanRig Rig, ScriptedRandom Random) Turn(params int[] draws)
    {
        var rig = new JapanRig();
        for (int c = 72; c <= 82; c++)
            rig.State.Cmdrs[c].Troops = 200;
        foreach (int p in new[] { 55, 28, 27, 25, 26, 24, 23, 6, 3, 2, 5, 4, 1 })
            rig.State.Provs[p].Owner = 4;
        return (rig, new ScriptedRandom(draws));
    }

    private static Task TurnAsync(JapanRig rig, ScriptedRandom random) =>
        new JapanTurn(new OriginalScriptContext(rig.State, random, rig.View, rig.Input, rig.Story), new Literals()).RunAsync();

    [Fact]
    public async Task A_quiet_week_plays_a_random_event_when_nothing_happened()
    {
        var (rig, random) = Turn(2, 2, 1, 3);                       // no Kouhime, quiet, IJAPAN 7
        await TurnAsync(rig, random);
        Assert.Equal([2, 2, 4, 4], random.Draws);
        Assert.Equal([("IJAPAN.ADV", 7)], rig.Story.Calls);
        Is(3, rig.State.Bt_EnemyRetreatMode);
        Assert.Equal("TJAPAN.ADV:3", rig.State.Strings[10]);

        (rig, random) = Turn(1, 2, 1);                              // Kouhime's scene 4 first: no event after it
        rig.State.F_Kou_State = 1;
        await TurnAsync(rig, random);
        Assert.Equal([("Gｺｳﾋﾒ.ADV", 4)], rig.Story.Calls);
        Is(1, rig.State.Ev_WeeklyEventDone);
    }

    [Fact]
    public async Task The_drawn_army_attacks_the_first_province_the_player_holds()
    {
        var (rig, random) = Turn(2, 2, 2, 1);                       // the whole army
        rig.State.Cmdrs[73].Troops = 100;
        rig.State.Cmdrs[74].Troops = 150;                           // under 190: left out once Nobunaga leads
        rig.State.Cmdrs[72].Troops = 300;
        rig.State.Provs[6].Owner = 1;                               // Oaks
        await TurnAsync(rig, random);
        Assert.Equal([72, 78, 77, 75], rig.Party);
        Assert.Equal([("FIGT_B.ADV", 0)], rig.Story.Calls);
        Is(6, rig.State.Prov_Cur);

        (rig, random) = Turn(2, 2, 2, 4);                           // One-Shot Tarou's four, 100 troops each
        for (int c = 79; c <= 82; c++)
            rig.State.Cmdrs[c].Troops = 100;
        rig.State.Provs[6].Owner = 1;
        await TurnAsync(rig, random);
        Assert.Equal([79, 0, 0, 0], rig.Party);                     // the others under 190: left out
        Is(100, rig.State.CallArg);                                 // too weak to march
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public async Task On_its_way_JAPAN_takes_Hanna_from_the_cities_but_passes_M_Land_by()
    {
        var (rig, random) = Turn(2, 2, 2, 2);                       // Katsuie's four, 800 troops
        rig.State.Provs[25].Owner = 3;                              // M Land: passed by (NAT-22)
        rig.State.Provs[26].Owner = 3;                              // Hanna: taken
        rig.State.Provs[6].Owner = 1;
        await TurnAsync(rig, random);
        Assert.Empty(rig.Story.Calls);
        Is(6, rig.State.Provs[26].Owner);
        Is(3, rig.State.Provs[25].Owner);
        Is(26, rig.State.Prov_Cur);
        Assert.Contains(162, rig.View.Events.OfType<MessageEvent>().Select(m => m.Text.Line));

        (rig, random) = Turn(2, 2, 2, 2);                           // nothing to attack: the walk runs out
        await TurnAsync(rig, random);
        Assert.Empty(rig.Story.Calls);
        Is(1, rig.State.Prov_Cur);
        Is(6, rig.State.Provs[1].Owner);                            // NAT-23: Leazas Castle taken
    }

    private static IEnumerable<int> Lines(JapanRig rig) => rig.View.Events.OfType<MessageEvent>().Select(m => m.Text.Line);

    [Fact]
    public async Task Gekkou_kills_only_a_unit_that_fought_this_week_and_takes_the_weeks_event()
    {
        var (rig, random) = Turn(1, 1, 2, 1, 1, 1);                 // Gekkou, an attempt, Rick; quiet week
        rig.State.N_Gekkou_Attempts = 2;
        rig.State.F_Kou_State = 1;
        rig.State.Units[6].Status = 2;
        rig.State.Units[7].Status = 2;
        await TurnAsync(rig, random);
        Assert.Equal([2, 2, 6, 2, 2, 4], random.Draws);
        Is(0, rig.State.Units[7].Status);                           // NAT-14: Rick fought, killed
        Is(2, rig.State.Units[6].Status);
        Is(7, rig.State.Unit_Cur);
        Is(2, rig.State.N_Gekkou_Attempts);                         // Gekkou's attempts do not count
        Is(1, rig.State.Ev_WeeklyEventDone);
        Assert.Empty(rig.Story.Calls);                              // no Kouhime, no random event
        Assert.Contains(844, Lines(rig));

        (rig, random) = Turn(1, 1, 2, 1, 1, 1);
        rig.State.N_Gekkou_Attempts = 2;
        rig.State.Units[7].Status = 1;                              // idle: he wins the duel
        await TurnAsync(rig, random);
        Is(1, rig.State.Units[7].Status);
        Assert.Contains(922, Lines(rig));
        Assert.DoesNotContain(844, Lines(rig));

        (rig, random) = Turn(1, 1, 2, 1, 1, 1);
        rig.State.N_Gekkou_Attempts = 2;                            // Rick dead: nobody found
        await TurnAsync(rig, random);
        Is(15, rig.State.Unit_Cur);
        Assert.Contains(518, Lines(rig));
    }

    [Fact]
    public async Task Gekkou_steals_half_the_gold_or_a_quarter_or_nothing()
    {
        foreach (var (draw, gold) in new[] { (1, 500), (2, 750), (3, 1001) })
        {
            var (rig, random) = Turn(1, 2, draw, 1, 1, 1);
            rig.State.N_Gekkou_Attempts = 1;
            rig.State.Gold = 1001;
            await TurnAsync(rig, random);
            Assert.Equal([2, 2, 3, 2, 2, 4], random.Draws);
            Is(gold, rig.State.Gold);
            Is(draw, rig.State.Tmp0690);
        }
    }

    [Fact]
    public async Task Shinobu_ends_the_game_when_Rance_fought_this_week_and_counts_otherwise()
    {
        var (rig, random) = Turn(1, 2);                             // Rance fought: a draw in three, 2
        rig.State.N_Gekkou_Attempts = 3;
        rig.State.Units[1].Status = 2;
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => TurnAsync(rig, random));
        Assert.Equal("OVER.ADV", jump.File);
        Is(4, rig.State.EndingId);
        Is(4, rig.State.N_Gekkou_Attempts);
        Assert.Equal([2, 3], random.Draws);

        (rig, random) = Turn(2, 1, 1, 1);                           // Rance idle: he runs off, the turn goes on
        rig.State.N_Gekkou_Attempts = 3;
        rig.State.Units[1].Status = 1;
        await TurnAsync(rig, random);
        Is(4, rig.State.N_Gekkou_Attempts);
        Is(0, rig.State.EndingId);
        Assert.Contains(2118, Lines(rig));
        Assert.DoesNotContain(2438, Lines(rig));
    }

    [Fact]
    public async Task On_the_fifth_count_Shinobu_is_caught_and_a_dead_Leila_still_gives_an_order()
    {
        var (rig, random) = Turn(2, 1, 1, 1);
        rig.State.N_Gekkou_Attempts = 4;
        rig.State.Units[5].Status = 1;                              // Leila alive
        await TurnAsync(rig, random);
        Is(5, rig.State.N_Gekkou_Attempts);
        Assert.Contains(2438, Lines(rig));
        Assert.Contains(2557, Lines(rig));
        Assert.DoesNotContain(2384, Lines(rig));
        Assert.DoesNotContain(2536, Lines(rig));

        (rig, random) = Turn(2, 1, 1, 1);
        rig.State.N_Gekkou_Attempts = 4;                            // Leila dead: Maris speaks
        await TurnAsync(rig, random);
        Assert.Contains(2384, Lines(rig));
        Assert.Contains(2536, Lines(rig));
        Assert.Contains(2557, Lines(rig));                          // NAT-24: Leila's order too
        Assert.Contains(2660, Lines(rig));
        Assert.DoesNotContain(2683, Lines(rig));

        (rig, random) = Turn(1, 1, 1);                              // five counted: no more attempts
        rig.State.N_Gekkou_Attempts = 5;
        await TurnAsync(rig, random);
        Assert.Equal([2, 2, 4], random.Draws);
    }

    [Fact]
    public void TJAPAN_is_routed() => Assert.Contains("TJAPAN.ADV", StrategyRules.PortedFiles);
}
