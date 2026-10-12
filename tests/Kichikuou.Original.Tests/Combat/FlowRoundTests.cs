using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using static Kichikuou.Original.Tests.Combat.Sys3Assert;

namespace Kichikuou.Original.Tests.Combat;

/// <summary>FIGT.ADV:105-446 — round start, step and turn order (01 §5, §6).</summary>
public class FlowRoundTests
{
    [Theory]
    [InlineData(3, 51, 100, true)]
    [InlineData(3, 50, 100, false)]
    [InlineData(4, 26, 100, true)]
    [InlineData(4, 25, 100, false)]
    [InlineData(2, 100, 100, false)]
    [InlineData(3, 40000, 65534, true)]   // losses * 2 saturates at 65535
    public void Modes_3_and_4_retreat_at_round_start(int mode, int losses, int start, bool retreats)
    {
        var rig = new FlowRig();
        rig.State.Bt_EnemyRetreatMode = (Sys3Int)mode;
        rig.State.Bt_EnemyLosses = (Sys3Int)losses;
        rig.State.Bt_EnemyTroopsStart = (Sys3Int)start;
        Assert.Equal(retreats, rig.Battle.RoundStartRetreats());
    }

    [Theory]
    [InlineData(1, 1, 5000)]
    [InlineData(1, 2, 5000)]
    [InlineData(1, 3, 800)]    // dungeon duel keeps its limit
    [InlineData(0, 1, 800)]
    public async Task Header_forces_the_duel_limit_outside_dungeons(int duel, int kind, int limit)
    {
        var rig = new FlowRig();
        rig.State.Bt_IsDuel = (Sys3Int)duel;
        rig.State.Bt_Kind = (Sys3Int)kind;
        rig.State.Bt_TroopLimit = 800;
        await rig.Battle.RoundHeaderAsync();
        Is(limit, rig.State.Bt_TroopLimit);
    }

    [Theory]
    [InlineData(2, 30, 2, 80, 0)]
    [InlineData(1, 30, 11, 0, 40)]
    [InlineData(3, 20, 12, 0, 70)]
    [InlineData(2, 0, 1, 10, 0)]
    public async Task Header_advantages_go_to_Tmp0001_and_Tmp0002(int kind, int wall, int strategy, int player, int enemy)
    {
        var rig = new FlowRig();
        rig.State.Bt_Kind = (Sys3Int)kind;
        rig.State.Bt_WallBonus = (Sys3Int)wall;
        rig.State.Bt_StrategyResult = (Sys3Int)strategy;
        rig.State.Bt_RoundsLeft = 4;
        rig.Input.Keys.Enqueue(13);

        await rig.Battle.RoundHeaderAsync();

        Is(player, rig.State.Tmp0001);
        Is(enemy, rig.State.Tmp0002);
        Is(13, rig.State.RetVal);   // IK 0
        var header = Assert.IsType<RoundHeaderEvent>(Assert.Single(rig.View.Events));
        Is(player, header.PlayerAdvantage);
        Is(enemy, header.EnemyAdvantage);
        Is(4, header.RoundsLeft);
        Assert.Equal(" Rance Army", rig.State.Strings[7]);
    }

    [Theory]
    [InlineData(250, 7, 243, new[] { 25 })]
    [InlineData(19, 0, 18, new int[0])]   // quotient 1: no draw, 1 lost
    [InlineData(9, 0, 9, new int[0])]     // quotient 0: no draw, nothing lost
    public void Land_of_death_drains_rnd_troops_over_10(int troops, int draw, int left, int[] draws)
    {
        var rig = draw == 0 ? new FlowRig() : new FlowRig(draw);
        rig.State.Tmp0001 = (Sys3Int)troops;
        rig.Battle.LandOfDeathDrain();
        Is(left, rig.State.Tmp0001);
        Is(troops / 10, rig.State.Tmp0002);
        Assert.Equal(draws, rig.Random.Draws);
    }

    [Fact]
    public void Slot_states_reset_to_0_and_empty_slots_to_100()
    {
        var rig = new FlowRig();
        var s = rig.State;
        rig.PlayerSlots((1, 10), (2, 10));
        rig.EnemySlots((30, 10));
        s.Bt_PSlot1State = 1;
        s.Bt_PSlot2State = 15;
        s.Bt_ESlot1State = 20;
        s.Bt_PSlot4State = 1;
        rig.Battle.ResetSlotStates();
        Is(0, s.Bt_PSlot1State);
        Is(0, s.Bt_PSlot2State);
        Is(100, s.Bt_PSlot3State);
        Is(100, s.Bt_PSlot4State);
        Is(0, s.Bt_ESlot1State);
        Is(100, s.Bt_ESlot2State);
        Is(100, s.Bt_ESlot4State);
    }

    [Fact]
    public void Step_takes_a_pending_enemy_retreat()
    {
        var rig = new FlowRig();
        rig.State.Bt_EnemyRetreat = 3;
        rig.State.Bt_RoundStep = 4;
        Assert.Equal(FigtLabel.EnemyRetreats, rig.Battle.StepEntry());
        Is(4, rig.State.Bt_RoundStep);
    }

    [Fact]
    public void Step_resets_the_idle_counter_and_acting_slot()
    {
        var rig = new FlowRig();
        rig.State.Bt_RoundStep = 4;
        rig.State.Bt_PlayerActSlot = 2;
        Assert.Equal(FigtLabel.Pick, rig.Battle.StepEntry());
        Is(0, rig.State.Bt_RoundStep);
        Is(0, rig.State.Bt_PlayerActSlot);
    }

    [Fact]
    public void Pick_checks_annihilation_first()
    {
        var rig = new FlowRig();
        rig.EnemySlots((30, 10));
        Assert.Equal(FigtLabel.PlayerAnnihilated, rig.Battle.Pick());
        rig.PlayerSlots((1, 10));
        rig.State.Bt_EnemySlot1 = 0;
        Assert.Equal(FigtLabel.EnemyAnnihilated, rig.Battle.Pick());
    }

    [Fact]
    public void Pick_starts_Goliath_in_his_province_before_counting()
    {
        var rig = new FlowRig();
        rig.PlayerSlots((1, 10));
        rig.EnemySlots((30, 10));
        rig.State.Goliath_State = 1;
        rig.State.Goliath_Prov = 12;
        rig.State.Prov_Cur = 12;
        Assert.Equal(FigtLabel.Goliath, rig.Battle.Pick());
        Is(0, rig.State.Bt_RoundStep);
        rig.State.Prov_Cur = 13;
        Assert.NotEqual(FigtLabel.Goliath, rig.Battle.Pick());
    }

    [Fact]
    public void Pick_ends_the_round_on_the_tenth_pass()
    {
        var rig = new FlowRig();
        rig.PlayerSlots((1, 10));
        rig.EnemySlots((30, 10));
        rig.State.Bt_RoundStep = 9;
        Assert.Equal(FigtLabel.RoundEnd, rig.Battle.Pick());
        Is(10, rig.State.Bt_RoundStep);
    }

    [Fact]
    public void Pick_chooses_the_fewest_troops_ties_to_the_player()
    {
        var rig = new FlowRig();
        var s = rig.State;
        rig.PlayerSlots((1, 300), (2, 100), (3, 100));
        rig.EnemySlots((30, 100), (31, 50));
        s.Bt_ESlot2State = 1;           // the 50 has acted
        s.Bt_PSlot2Focus = 2;
        s.Bt_PlayerOrder2 = 1;

        Assert.Equal(FigtLabel.PlayerTurn, rig.Battle.Pick());

        Is(2, s.Bt_PlayerActSlot);      // ties: lower slot, player before enemy
        Is(1, s.Bt_PSlot2State);
        Is(2, s.Unit_Cur);
        Is(2, s.Bt_ActFocus);
        Is(1, s.Bt_CurOrder);
        Is(2, s.RetVal);
        Is(100, s.Tmp0002);
        Is(1, s.Bt_RoundStep);
        Is(30, s.Cmdr_Cur);             // the scan left the cursor on the last enemy compared
    }

    [Fact]
    public void Pick_gives_the_turn_to_an_enemy_with_fewer_troops()
    {
        var rig = new FlowRig();
        var s = rig.State;
        rig.PlayerSlots((1, 300));
        rig.EnemySlots((30, 400), (31, 299));
        Assert.Equal(FigtLabel.EnemyTurn, rig.Battle.Pick());
        Is(2, s.Bt_EnemyActSlot);
        Is(1, s.Bt_ESlot2State);
        Is(31, s.Cmdr_Cur);
        Is(6, s.RetVal);
    }

    [Fact]
    public void Pick_takes_slot_1_without_comparing_and_a_commander_alone_first()
    {
        var rig = new FlowRig();
        rig.PlayerSlots((1, 65535), (2, 0));
        rig.EnemySlots((30, 1));
        Assert.Equal(FigtLabel.PlayerTurn, rig.Battle.Pick());
        Is(2, rig.State.Bt_PlayerActSlot);
    }

    [Fact]
    public void Pick_never_selects_65535_troops_outside_slot_1()
    {
        // QUIRK Q-17: strict < against the 65535 sentinel.
        var rig = new FlowRig();
        var s = rig.State;
        rig.PlayerSlots((1, 10), (2, 65535));
        rig.EnemySlots((30, 65535));
        s.Bt_PSlot1State = 1;
        Assert.Equal(FigtLabel.Pick, rig.Battle.Pick());
        Is(9, s.RetVal);
        Is(65535, s.Tmp0002);
        Is(1, s.Bt_RoundStep);
    }

    [Fact]
    public void Pick_skips_defending_units()
    {
        var rig = new FlowRig();
        var s = rig.State;
        rig.PlayerSlots((1, 10), (2, 20));
        rig.EnemySlots((30, 30));
        s.Bt_PSlot1State = 12;   // defended
        Assert.Equal(FigtLabel.PlayerTurn, rig.Battle.Pick());
        Is(2, s.Bt_PlayerActSlot);
    }
}

/// <summary>FIGT.ADV:447-594 — round end, timeout and the exits (01 §7).</summary>
public class FlowExitTests
{
    private static FlowRig Rig(params int[] draws)
    {
        var rig = new FlowRig(draws);
        rig.PlayerSlots((1, 100));
        rig.EnemySlots((30, 100));
        return rig;
    }

    [Fact]
    public async Task Duel_never_times_out()
    {
        var rig = Rig();
        rig.State.Bt_IsDuel = 1;
        rig.State.Bt_RoundsLeft = 1;
        Assert.Equal(FigtLabel.RoundStart, await rig.Battle.RoundEndAsync());
        Is(1, rig.State.Bt_RoundsLeft);
    }

    [Fact]
    public async Task A_round_passes()
    {
        var rig = Rig();
        rig.State.Bt_RoundsLeft = 3;
        Assert.Equal(FigtLabel.RoundStart, await rig.Battle.RoundEndAsync());
        Is(2, rig.State.Bt_RoundsLeft);
        Assert.Empty(rig.View.Events);
    }

    [Theory]
    [InlineData(1, 1, (int)FigtLabel.AttackerLost)]
    [InlineData(2, 1, (int)FigtLabel.DefenderWon)]
    [InlineData(1, 0, (int)FigtLabel.AttackerLost)]   // Fog Card: 0 - 1 saturates to 0
    public async Task Timeout_decides_by_kind(int kind, int rounds, int nextCode)
    {
        var next = (FigtLabel)nextCode;
        var rig = Rig();
        rig.State.Bt_Kind = (Sys3Int)kind;
        rig.State.Bt_RoundsLeft = (Sys3Int)rounds;
        rig.State.Bt_Result = 7;
        Assert.Equal(next, await rig.Battle.RoundEndAsync());
        Is(0, rig.State.Bt_Result);
        Assert.Equal([453, 458], rig.Lines);
    }

    [Fact]
    public async Task Dungeon_timeout_awards_exp_and_ends_with_result_0()
    {
        var rig = Rig(3);
        rig.State.Bt_Kind = 3;
        rig.State.Bt_RoundsLeft = 1;
        Assert.Equal(FigtLabel.End, await rig.Battle.RoundEndAsync());
        Is(0, rig.State.Bt_Result);
        Assert.Equal([15], rig.Random.Draws);
        Is(1, rig.State.Units[1].Exp);
        Assert.Equal([453, 465, 2999, 3003], rig.Lines.Take(4));
        Assert.IsType<ClearScreenEvent>(rig.View.Events[^1]);
    }

    [Fact]
    public async Task Player_annihilated_reports_and_awards_a_third()
    {
        var rig = Rig(10);   // exp 15 → 5
        rig.State.Bt_PlayerSlot1 = 0;
        rig.State.Bt_PlayerSlot2 = 0;
        Assert.Equal(FigtLabel.End, await rig.Battle.PlayerAnnihilatedAsync());
        Is(1, rig.State.Bt_Result);
        Is(5, rig.State.Tmp0001);
        Assert.Equal([498, 2999, 3003], rig.Lines);
        Assert.DoesNotContain(rig.View.Events, e => e is MapEvent);
    }

    [Fact]
    public async Task Duel_exits_return_to_the_map_before_exp()
    {
        var rig = Rig(1);
        rig.State.Bt_IsDuel = 1;
        Assert.Equal(FigtLabel.End, await rig.Battle.EnemyAnnihilatedAsync());
        Is(2, rig.State.Bt_Result);
        Assert.IsType<MapEvent>(rig.View.Events[0]);
        Assert.Equal([2999, 3003], rig.Lines);
    }

    [Fact]
    public async Task Enemy_annihilated_reports_the_win()
    {
        var rig = Rig(1);
        Assert.Equal(FigtLabel.End, await rig.Battle.EnemyAnnihilatedAsync());
        Is(2, rig.State.Bt_Result);
        Assert.Equal([518, 2999, 3003], rig.Lines);
    }

    [Fact]
    public async Task Attacker_lost_and_defender_won_set_the_result()
    {
        var rig = Rig(1, 1);
        Assert.Equal(FigtLabel.End, await rig.Battle.AttackerLostAsync());
        Is(1, rig.State.Bt_Result);
        Assert.Equal(FigtLabel.End, await rig.Battle.DefenderWonAsync());
        Is(2, rig.State.Bt_Result);
        Assert.Equal([15, 15], rig.Random.Draws);
    }

    [Fact]
    public async Task Enemy_retreat_with_empty_slot_1_is_an_annihilation()
    {
        var rig = Rig();
        rig.State.Bt_EnemySlot1 = 0;
        Assert.Equal(FigtLabel.EnemyAnnihilated, await rig.Battle.EnemyRetreatsAfterRedrawAsync());
    }

    [Fact]
    public async Task Enemy_retreat_reports_and_goes_to_defender_won()
    {
        var rig = Rig();
        Assert.Equal(FigtLabel.DefenderWon, await rig.Battle.EnemyRetreatsAfterRedrawAsync());
        Is(2, rig.State.Bt_Result);
        Assert.Equal([542], rig.Lines);
    }

    [Theory]
    [InlineData(1, 1, 552)]
    [InlineData(0, 3, 562)]
    public async Task Retreat_is_refused_in_duels_and_dungeons(int duel, int kind, int line)
    {
        var rig = Rig();
        rig.State.Bt_IsDuel = (Sys3Int)duel;
        rig.State.Bt_Kind = (Sys3Int)kind;
        Assert.Equal(FigtLabel.PlayerTurn, await rig.Battle.PlayerRetreatAsync());
        Assert.Equal([line], rig.Lines);
        Assert.Empty(rig.Input.Menus);
    }

    [Theory]
    [InlineData(1, (int)FigtLabel.RetreatConfirmed)]
    [InlineData(2, (int)FigtLabel.PlayerTurn)]
    [InlineData(null, (int)FigtLabel.PlayerTurn)]
    public async Task Retreat_menu(int? answer, int nextCode)
    {
        var next = (FigtLabel)nextCode;
        var rig = Rig();
        rig.State.Bt_Kind = 1;
        rig.Input.MenuAnswers.Enqueue(answer);
        Assert.Equal(next, await rig.Battle.PlayerRetreatAsync());
        var menu = Assert.Single(rig.Input.Menus);
        Assert.Equal(BattleMenuKind.RetreatConfirm, menu.Kind);
        Assert.Equal([573, 577], menu.Options.Select(o => o.Label.Line));
    }

    [Fact]
    public async Task Retreat_menu_rejects_an_unlisted_answer()
    {
        var rig = Rig();
        rig.State.Bt_Kind = 2;
        rig.Input.MenuAnswers.Enqueue(3);
        await Assert.ThrowsAsync<InvalidOperationException>(rig.Battle.PlayerRetreatAsync);
    }

    [Fact]
    public async Task Retreat_confirmed_loses()
    {
        var rig = Rig();
        Assert.Equal(FigtLabel.AttackerLost, await rig.Battle.RetreatConfirmedAsync());
        Is(1, rig.State.Bt_Result);
        Assert.Equal([585], rig.Lines);
    }
}
