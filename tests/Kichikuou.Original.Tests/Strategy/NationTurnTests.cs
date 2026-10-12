using Kichikuou.Original.Tests.Combat;
using Kichikuou.Original.Tests.Script;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary>
/// The nation template (<see cref="NationFile"/>) and the rebels' files on it: the weekly turn
/// <c>TCﾘｰｻﾞｽ</c> (<see cref="RebelTurn"/>) and the defender picker <c>TCﾘｰｻﾞｽ2</c>
/// (<see cref="RebelDefenders"/>). Both are also compared with the decompile on random states
/// (Content tests, <c>NationTurnDifferentialTests</c>) and replayed on the owner's capture
/// (<c>NationReplayTests</c>).
/// </summary>
public class NationTurnTests
{
    /// <summary>
    /// Stage 1 as <c>Iﾊﾝﾗﾝ</c> leaves it: the rebels hold 3-7, the player 1, 2 and 8; rebel
    /// commanders 1-7 alive with 300 troops each. No draws: the files of N1 draw nothing.
    /// </summary>
    private sealed class NationRig
    {
        public NationRig()
        {
            Script = new OriginalScriptContext(State, Random, View, Input, Story);
            for (int p = 1; p <= 8; p++)
                State.Provs[p].Owner = (Sys3Int)(p is >= 3 and <= 7 ? 2 : 1);
            for (int c = 1; c <= 7; c++)
            {
                State.Cmdrs[c].Status = 1;
                State.Cmdrs[c].Troops = 300;
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
        public ScriptedRandom Random { get; } = new();
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public OriginalScriptContext Script { get; }

        /// <summary>Each <c>FIGT_B</c> call: the province, the enemy party and its retreat mode.</summary>
        public List<(int Prov, int[] Party, int Retreat)> Battles { get; } = [];

        public Task TurnAsync() => new RebelTurn(Script, new Literals()).RunAsync();

        public Task DefendersAsync(int province)
        {
            State.Prov_Cur = (Sys3Int)province;
            return new RebelDefenders(Script, new Literals()).RunAsync();
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
    public async Task The_first_rebel_turn_plays_Exs_scene_then_armies_A_and_B_attack()
    {
        var rig = new NationRig();
        rig.State.F_LeazasRebels_State = 1;                     // Iｼﾚｲﾌﾞ 1 set it this week

        await rig.TurnAsync();

        Assert.Equal(("Gｴｸｽ.ADV", 10), rig.Story.Calls[0]);
        Is(2, rig.State.F_LeazasRebels_State);
        Is(2, rig.State.Ev_PhaseNation);
        Assert.Equal("TCﾘｰｻﾞｽ.ADV:3", rig.State.Strings[10]);
        // A (3, 5, 4) takes the first of 4, 3, 2, 1 the player holds: 2; B (1, 2, 6, 7) the first of 7, 8, 1: 8.
        Assert.Equal(2, rig.Battles.Count);
        Assert.Equal(2, rig.Battles[0].Prov);
        Assert.Equal([3, 5, 4, 0], rig.Battles[0].Party);
        Assert.Equal(8, rig.Battles[1].Prov);
        Assert.Equal([1, 2, 6, 7], rig.Battles[1].Party);
        Assert.All(rig.Battles, b => Assert.Equal(4, b.Retreat));
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task Later_turns_play_no_scene_and_leave_the_cursors_where_the_walk_and_the_sum_ended()
    {
        var rig = new NationRig();
        rig.State.F_LeazasRebels_State = 2;
        rig.Story.OnCall = null;

        await rig.TurnAsync();

        Assert.Equal(["FIGT_B.ADV", "FIGT_B.ADV"], rig.Story.Calls.Select(c => c.File));
        Is(8, rig.State.Prov_Cur);                              // B attacked 8
        Is(7, rig.State.Cmdr_Cur);                              // the sum ends on slot 4
        Is(1200, rig.State.CallArg);
        Is(1, rig.State.Tmp0001);                               // B's list stays in scratch
        Is(7, rig.State.Tmp0004);
        Is(0, rig.State.Tmp0005);
    }

    [Fact]
    public async Task Commanders_under_190_troops_stay_home_and_an_army_under_500_does_not_march()
    {
        var rig = new NationRig();
        rig.State.Cmdrs[3].Troops = 189;
        rig.State.Cmdrs[4].Troops = 189;                        // A: only 5 (300) — under 500
        rig.State.Cmdrs[1].Status = 0;                          // B: 2, 6, 7 = 900
        rig.State.Cmdrs[2].Troops = 190;

        await rig.TurnAsync();

        var b = Assert.Single(rig.Battles);
        Assert.Equal(8, b.Prov);
        Assert.Equal([2, 6, 7, 0], b.Party);
        Is(790, rig.State.CallArg);
    }

    [Fact]
    public async Task The_picker_stops_once_slot_4_is_taken_from_the_fourth_entry_on()
    {
        var rig = new NationRig();
        rig.State.Provs[5].Owner = 1;                           // the player holds 5: army C alone
        rig.State.Provs[6].Owner = 1;

        await rig.TurnAsync();

        var c = Assert.Single(rig.Battles);
        Assert.Equal(6, c.Prov);                                // C attacks 6 before 5
        Assert.Equal([1, 2, 3, 6], c.Party);                    // 4, 5, 7 never tested
        Is(6, rig.State.Cmdr_Cur);
    }

    [Fact]
    public async Task An_empty_slot_reads_commander_record_0()
    {
        var rig = new NationRig();
        rig.State.Provs[5].Owner = 1;
        for (int c = 2; c <= 7; c++)
            rig.State.Cmdrs[c].Status = 0;
        rig.State.Cmdrs[1].Troops = 450;
        // QUIRK: NAT-12 — record 0's troops (the field before it, aliased) join the sum of every empty slot.
        rig.State.Cmdrs[0].Troops = 20;

        await rig.TurnAsync();

        var c1 = Assert.Single(rig.Battles);
        Assert.Equal([1, 0, 0, 0], c1.Party);
        Is(510, rig.State.CallArg);
    }

    [Fact]
    public async Task Army_B_falls_into_army_C_when_the_player_holds_none_of_7_8_1()
    {
        var rig = new NationRig();
        rig.State.Provs[8].Owner = 2;
        rig.State.Provs[1].Owner = 0;                           // only after a lost castle, which ends the game
        rig.State.Provs[6].Owner = 1;

        await rig.TurnAsync();

        // QUIRK: NAT-06 — A attacks 2, B finds no target and C attacks 6 the same week.
        Assert.Equal([2, 6], rig.Battles.Select(b => b.Prov));
        Assert.Equal([1, 2, 3, 6], rig.Battles[1].Party);
    }

    [Fact]
    public async Task A_battle_that_jumps_to_the_ending_ends_the_turn()
    {
        var rig = new NationRig();
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "FIGT_B.ADV")
                ScriptContext.JumpTo("OVER.ADV");
        };

        var e = await Assert.ThrowsAsync<ScenarioJumpException>(rig.TurnAsync);

        Assert.Equal("OVER.ADV", e.File);
        Assert.Single(rig.Story.Calls);
    }

    [Theory]
    [InlineData(7, new[] { 1, 2, 6, 7 }, 4)]
    [InlineData(8, new[] { 1, 2, 6, 7 }, 4)]
    [InlineData(2, new[] { 3, 5, 4, 0 }, 4)]
    [InlineData(3, new[] { 3, 5, 4, 0 }, 4)]
    [InlineData(4, new[] { 3, 5, 4, 0 }, 4)]
    [InlineData(5, new[] { 1, 2, 3, 6 }, 2)]
    [InlineData(6, new[] { 1, 2, 3, 6 }, 1)]
    [InlineData(1, new[] { 1, 2, 6, 7 }, 4)]                   // any other province: army B
    public async Task The_defenders_are_chosen_by_province(int province, int[] party, int retreat)
    {
        var rig = new NationRig();
        rig.State.Bt_EnemyRetreatMode = 20;

        await rig.DefendersAsync(province);

        int[] picked = [rig.State.Bt_EnemyParty1.Value, rig.State.Bt_EnemyParty2.Value,
            rig.State.Bt_EnemyParty3.Value, rig.State.Bt_EnemyParty4.Value];
        Assert.Equal(party, picked);
        Is(retreat, rig.State.Bt_EnemyRetreatMode);
        Is(2, rig.State.Ev_PhaseNation);
        Assert.Equal("TCﾘｰｻﾞｽ2.ADV:3", rig.State.Strings[10]);
        Assert.Empty(rig.Story.Calls);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task Only_Oaks_has_walls()
    {
        var rig = new NationRig();
        rig.State.Provs[6].BattleArea = 640;
        rig.State.Provs[6].Defense = 12;
        rig.State.Provs[4].Defense = 9;

        await rig.DefendersAsync(6);
        Is(640, rig.State.Bt_TroopLimit);
        Is(12, rig.State.Bt_WallBonus);

        await rig.DefendersAsync(4);
        // QUIRK: NAT-17 — province 4's walls (9) are not used.
        // QUIRK: NAT-16 — the limit is written for nothing: FIGT_A recomputes it from the walls.
        Is(5000, rig.State.Bt_TroopLimit);
        Is(0, rig.State.Bt_WallBonus);
    }

    [Fact]
    public async Task Defenders_under_100_troops_keep_the_last_retreat_mode()
    {
        var rig = new NationRig();
        rig.State.Bt_EnemyRetreatMode = 50;                     // what the last battle left
        rig.State.Cmdrs[3].Troops = 40;
        rig.State.Cmdrs[5].Status = 0;
        rig.State.Cmdrs[4].Troops = 59;

        await rig.DefendersAsync(3);

        // QUIRK: BFL-13 — A musters 99 troops: they fight with the stale mode 50.
        Assert.Equal(3, rig.State.Bt_EnemyParty1.Value);
        Assert.Equal(4, rig.State.Bt_EnemyParty2.Value);
        Is(99, rig.State.CallArg);
        Is(50, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task With_every_defender_dead_the_party_is_empty()
    {
        var rig = new NationRig();
        for (int c = 1; c <= 7; c++)
            rig.State.Cmdrs[c].Status = 0;

        await rig.DefendersAsync(5);

        Is(0, rig.State.Bt_EnemyParty1);                        // Cﾃｷﾄｼ.ADV:395 takes the city unopposed
        Is(2, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public void Both_rebel_files_are_routed()
    {
        Assert.Contains("TCﾘｰｻﾞｽ.ADV", StrategyRules.PortedFiles);
        Assert.Contains("TCﾘｰｻﾞｽ2.ADV", StrategyRules.PortedFiles);
    }
}
