using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>FIGT.ADV:1-104 — initialisation and strategists (01 §3).</summary>
public class FlowSetupTests
{
    [Fact]
    public void InitBattle_copies_parties_and_clears_focus_and_orders()
    {
        var rig = new FlowRig(2);
        var s = rig.State;
        rig.PlayerParty(1, 5, 0, 0);
        rig.EnemyParty(30, 31, 32, 0);
        s.Bt_PSlot1Focus = 3;
        s.Bt_PSlot4Focus = 1;
        s.Bt_PlayerOrder2 = 2;
        s.Bt_PlayerSlot4 = 9;   // stale from a previous battle

        rig.Battle.InitBattle();

        Is(1, s.Bt_PlayerSlot1);
        Is(5, s.Bt_PlayerSlot2);
        Is(0, s.Bt_PlayerSlot3);
        Is(0, s.Bt_PlayerSlot4);
        Is(30, s.Bt_EnemySlot1);
        Is(32, s.Bt_EnemySlot3);
        Is(0, s.Bt_EnemySlot4);
        Is(0, s.Bt_PSlot1Focus);
        Is(0, s.Bt_PSlot4Focus);
        Is(0, s.Bt_PlayerOrder2);
    }

    [Theory]
    [InlineData(0, 1, 4)]
    [InlineData(0, 2, 5)]
    [InlineData(6, 2, 3)]   // mode 6: 3 rounds, the draw is still made
    public void InitBattle_rounds_are_3_plus_rnd2(int retreatMode, int draw, int rounds)
    {
        var rig = new FlowRig(draw);
        rig.State.Bt_EnemyRetreatMode = (Sys3Int)retreatMode;
        rig.Battle.InitBattle();
        Is(rounds, rig.State.Bt_RoundsLeft);
        Assert.Equal([2], rig.Random.Draws);
    }

    [Fact]
    public void InitBattle_resets_counters_and_sums_troops_moving_the_cursors()
    {
        var rig = new FlowRig(1);
        var s = rig.State;
        rig.PlayerParty(3, 0, 7, 0);
        rig.EnemyParty(40, 41, 0, 0);
        s.Units[3].Troops = 100;
        s.Units[7].Troops = 250;
        s.Cmdrs[40].Troops = 1000;
        s.Cmdrs[41].Troops = 5;
        s.Bt_PlayerLosses = 9;
        s.Bt_EnemyLosses = 9;
        s.Bt_StrategyResult = 2;
        s.Bt_SpecialAtk = 4;
        s.Bt_EnemyRetreat = 3;
        s.VAR0603 = 1;

        rig.Battle.InitBattle();

        Is(350, s.Bt_PlayerTroopsStart);
        Is(1005, s.Bt_EnemyTroopsStart);
        Is(0, s.Bt_PlayerLosses);
        Is(0, s.Bt_EnemyLosses);
        Is(0, s.Bt_StrategyResult);
        Is(0, s.Bt_SpecialAtk);
        Is(0, s.Bt_EnemyRetreat);
        Is(0, s.VAR0603);
        Is(7, s.Unit_Cur);    // last summed player record
        Is(41, s.Cmdr_Cur);   // last summed enemy record
        Is(1005, s.CallArg);
    }

    [Fact]
    public void TroopSum_saturates()
    {
        var rig = new FlowRig();
        rig.PlayerSlots((1, 40000), (2, 40000));
        rig.State.CallArg = 1;
        rig.Battle.TroopSum();
        Is(65535, rig.State.CallArg);
    }

    [Fact]
    public void TroopSum_with_other_side_code_does_nothing()
    {
        var rig = new FlowRig();
        rig.PlayerSlots((1, 400));
        rig.State.CallArg = 3;
        rig.Battle.TroopSum();
        Is(3, rig.State.CallArg);
    }

    [Fact]
    public void ChooseStrategists_ties_keep_the_earlier_slot_and_items_add()
    {
        var rig = new FlowRig();
        var s = rig.State;
        rig.PlayerSlots((1, 0), (2, 0), (3, 0));
        s.Units[1].Strategy = 30;
        s.Units[2].Strategy = 30;          // tie: slot 1 stays
        s.Units[3].Strategy = 25;
        s.Units[3].Item = 12;              // +10 from an effect-6 item → 35, best
        s.Items[12].Effect = 6;
        s.Items[12].Amount = 10;
        rig.EnemySlots((50, 0), (51, 0));
        s.Cmdrs[50].Strategy = 20;
        s.Cmdrs[51].Strategy = 20;         // tie: slot 1 stays

        rig.Battle.ChooseStrategists();

        Is(3, s.Bt_PlayerStrategist);
        Is(35, s.Tmp0021);
        Is(50, s.Bt_EnemyStrategist);
        Is(20, s.Tmp0022);
        Is(12, s.Item_Cur);
        Is(3, s.Unit_Cur);
        Is(51, s.Cmdr_Cur);
    }

    [Fact]
    public void StrategyValue_ignores_items_of_other_effects()
    {
        var rig = new FlowRig();
        var s = rig.State;
        s.Unit_Cur = 4;
        s.Units[4].Strategy = 12;
        s.Units[4].Item = 3;
        s.Items[3].Effect = 5;
        s.Items[3].Amount = 50;
        rig.Battle.StrategyValue();
        Is(12, s.Tmp0023);
        Is(3, s.Item_Cur);
    }

    [Fact]
    public void ChooseStrategists_reads_slot_1_without_checking_it()
    {
        var rig = new FlowRig();
        var s = rig.State;
        s.Bt_PlayerSlot1 = 0;              // record 0 is read
        s.Units[0].Strategy = 9;
        rig.EnemySlots((50, 0));
        rig.Battle.ChooseStrategists();
        Is(9, s.Tmp0021);
        Is(0, s.Bt_PlayerStrategist);
    }

    [Fact]
    public async Task FightAsync_runs_setup_before_the_round_loop()
    {
        var rig = new FlowRig(1);
        rig.PlayerParty(1);
        rig.EnemyParty(30);
        rig.State.Units[1].Troops = 100;
        rig.State.Cmdrs[30].Troops = 80;

        Assert.True(await FlowRig.UntilStub(rig.Battle.FightAsync));

        Is(1, rig.State.Bt_PlayerSlot1);
        Is(30, rig.State.Bt_EnemySlot1);
        Is(4, rig.State.Bt_RoundsLeft);
        Assert.Equal(2, rig.Random.Draws[0]); // the round-limit draw; the battle then goes on
        var sound = Assert.IsType<SoundEvent>(rig.View.Events[0]);
        Is(1, sound.Mode);
        Is(102, sound.Sound);
    }
}

/// <summary>FIGT作.ADV — the pre-battle strategy (01 §4).</summary>
public class FlowStrategyTests
{
    private static FlowRig Rig(int side, int best, int ebest, params int[] draws)
    {
        var rig = new FlowRig(draws);
        rig.State.CallArg = (Sys3Int)side;
        rig.State.Tmp0021 = (Sys3Int)best;
        rig.State.Tmp0022 = (Sys3Int)ebest;
        rig.State.Bt_PlayerStrategist = 3;
        rig.State.Bt_EnemyStrategist = 60;
        return rig;
    }

    /// <summary>
    /// A successful strategy loads its screen (STR-43): the player's 899, the battle's background, the
    /// unit's sprite by troops and face (<c>FIGT作.ADV:16-32</c>); the enemy's 899, 900, the
    /// commander's sprite and face (<c>:358-374</c>). A failed one loads nothing.
    /// </summary>
    [Theory]
    [InlineData(1, 40)]
    [InlineData(2, 40)]
    [InlineData(1, 41)]
    public async Task A_successful_strategy_counts_the_CGs_of_its_screen(int side, int roll)
    {
        var rig = side == 1 ? Rig(1, 60, 20, roll) : Rig(2, 20, 60, roll);
        var s = rig.State;
        rig.Battle.Script.BindCgLoadCounter(196);
        s.Bt_BackgroundCg = 905;
        s.Bt_TroopLimit = 100;
        var unit = s.Units[3];
        unit.Troops = 50;
        (unit.CgFull, unit.CgPartial, unit.CgAlone, unit.FaceCg) = ((Sys3Int)1310, (Sys3Int)1311, (Sys3Int)1312, (Sys3Int)1313);
        var cmdr = s.Cmdrs[60];
        cmdr.Troops = 100;
        (cmdr.CgFull, cmdr.CgPartial, cmdr.CgAlone, cmdr.FaceCg) = ((Sys3Int)1320, (Sys3Int)1321, (Sys3Int)1322, (Sys3Int)1323);
        int[] cgs = [899, 900, 905, 1310, 1311, 1312, 1313, 1320, 1321, 1322, 1323];

        await rig.Battle.StrategyAsync();

        int[] expected = (side, roll) switch
        {
            (1, 40) => [1, 0, 1, 0, 1, 0, 1, 0, 0, 0, 0],      // troops 50 below the limit: partial
            (2, 40) => [1, 1, 0, 0, 0, 0, 0, 1, 0, 0, 1],      // troops at the limit: full
            _ => new int[cgs.Length],
        };
        Assert.Equal(expected, cgs.Select(cg => (int)s.CgSeen[cg].CgSeen.Value));
    }

    [Fact]
    public async Task Player_strategy_fails_when_the_roll_exceeds_the_difference()
    {
        var rig = Rig(1, 60, 20, 41);   // d = 40 < 41
        await rig.Battle.StrategyAsync();
        Is(0, rig.State.Bt_StrategyResult);
        Is(40, rig.State.Tmp0021);
        Assert.Equal([100], rig.Random.Draws);
        Assert.Empty(rig.View.Events);
    }

    [Fact]
    public async Task Player_strategy_succeeds_when_the_roll_equals_the_difference()
    {
        var rig = Rig(1, 60, 20, 40);   // d = 40, not < 40; margin 0
        await rig.Battle.StrategyAsync();
        Is(1, rig.State.Bt_StrategyResult);
        Is(0, rig.State.Tmp0021);
        Is(3, rig.State.Unit_Cur);
        Assert.Equal([37, 315, 321, 327], rig.Lines);   // generic line for unit 3
        var screen = Assert.IsType<StrategyScreenEvent>(rig.View.Events.First(e => e is StrategyScreenEvent));
        Is(1, screen.Side);
        Is(3, screen.Strategist);
    }

    [Theory]
    [InlineData(49, 2)]   // margin 51 > 50
    [InlineData(50, 1)]   // margin 50
    public async Task Player_great_success_needs_a_margin_above_50(int roll, int result)
    {
        var rig = Rig(1, 100, 0, roll);
        await rig.Battle.StrategyAsync();
        Is(result, rig.State.Bt_StrategyResult);
        Assert.Equal(result == 2 ? 337 : 327, rig.Lines[^1]);
    }

    [Theory]
    [InlineData(10, 11, 219)]
    [InlineData(3, 4, 236)]
    public async Task Cream_counts_her_tactics(int before, int after, int line)
    {
        var rig = Rig(1, 90, 0, 1);
        rig.State.Bt_PlayerStrategist = 28;
        rig.State.N_Cream_Tactics = (Sys3Int)before;
        await rig.Battle.StrategyAsync();
        Is(after, rig.State.N_Cream_Tactics);
        Assert.Contains(line, rig.Lines);
    }

    [Theory]
    [InlineData(2, 142, 50)]
    [InlineData(37, 141, 298)]
    [InlineData(36, 142, 284)]
    public async Task Player_strategists_have_their_own_line(int unit, int op, int line)
    {
        var rig = Rig(1, 90, 0, 1);
        rig.State.Bt_PlayerStrategist = (Sys3Int)unit;
        await rig.Battle.StrategyAsync();
        Assert.Equal(line, rig.Lines[1]);
        var windows = rig.View.Events.OfType<WindowEvent>().Select(w => (int)w.Op).ToList();
        Assert.Equal([143, 11, 10, op, 11, 10, 143, 11, 10], windows);
    }

    [Fact]
    public async Task Enemy_strategy_success_sets_11_or_12_and_moves_the_commander_cursor()
    {
        var rig = Rig(2, 10, 80, 10);   // d = 70, margin 60
        await rig.Battle.StrategyAsync();
        Is(12, rig.State.Bt_StrategyResult);
        Is(60, rig.State.Tmp0022);
        Is(60, rig.State.Cmdr_Cur);
        Assert.Equal([378, 457, 463, 479], rig.Lines);
        Assert.Equal([100], rig.Random.Draws);
    }

    [Fact]
    public async Task Enemy_strategy_failure_changes_nothing_else()
    {
        var rig = Rig(2, 10, 30, 21);   // d = 20 < 21
        await rig.Battle.StrategyAsync();
        Is(0, rig.State.Bt_StrategyResult);
        Is(20, rig.State.Tmp0022);
        Is(0, rig.State.Cmdr_Cur);
    }

    [Fact]
    public async Task Enemy_commander_22_speaks_in_window_152()
    {
        var rig = Rig(2, 0, 50, 5);
        rig.State.Bt_EnemyStrategist = 22;
        await rig.Battle.StrategyAsync();
        Is(11, rig.State.Bt_StrategyResult);
        Assert.Equal([378, 430, 463, 469], rig.Lines);
        Assert.Contains(rig.View.Events.OfType<WindowEvent>(), w => w.Op == 152);
    }

    [Fact]
    public async Task Side_codes_other_than_2_fall_into_the_player_side()
    {
        var rig = Rig(0, 50, 0, 1);
        await rig.Battle.StrategyAsync();
        Is(1, rig.State.Bt_StrategyResult);
    }

    [Fact]
    public async Task Each_message_wait_leaves_the_key_in_RetVal()
    {
        var rig = Rig(1, 50, 0, 1);
        rig.Input.Keys.Enqueue(13);
        rig.Input.Keys.Enqueue(14);
        rig.Input.Keys.Enqueue(27);
        await rig.Battle.StrategyAsync();
        Is(27, rig.State.RetVal);
        Assert.Equal(3, rig.Input.KeyWaits);
    }
}
