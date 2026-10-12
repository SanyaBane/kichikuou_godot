using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// <c>ComputeExchange</c> (<c>L_035fb</c>, <c>FIGT.ADV:2200-2449</c>) end to end, the special
/// overrides (<c>2324-2413</c>) and the demon shields (<c>2414-2448</c>);
/// docs/battle/02-resolution.md §6.2, §3.3. Every expected value is derived by hand in the comments.
/// </summary>
public class ResolutionComputeExchangeTests
{
    /// <summary>The 16 results in the order PTVC, PTVT, PCVT, PCVC, ETVC, ETVT, ECVT, ECVC (shots, dmg).</summary>
    private static int[] Results(GameState s) =>
    [
        (int)s.Bt_PTroopVsCmdrShots, (int)s.Bt_PTroopVsCmdrDmg,
        (int)s.Bt_PTroopVsTroopShots, (int)s.Bt_PTroopVsTroopDmg,
        (int)s.Bt_PCmdrVsTroopShots, (int)s.Bt_PCmdrVsTroopDmg,
        (int)s.Bt_PCmdrVsCmdrShots, (int)s.Bt_PCmdrVsCmdrDmg,
        (int)s.Bt_ETroopVsCmdrShots, (int)s.Bt_ETroopVsCmdrDmg,
        (int)s.Bt_ETroopVsTroopShots, (int)s.Bt_ETroopVsTroopDmg,
        (int)s.Bt_ECmdrVsTroopShots, (int)s.Bt_ECmdrVsTroopDmg,
        (int)s.Bt_ECmdrVsCmdrShots, (int)s.Bt_ECmdrVsCmdrDmg,
    ];

    private static void SetAllResults(GameState s, Sys3Int shots, Sys3Int dmg)
    {
        s.Bt_PTroopVsCmdrShots = shots; s.Bt_PTroopVsCmdrDmg = dmg;
        s.Bt_PTroopVsTroopShots = shots; s.Bt_PTroopVsTroopDmg = dmg;
        s.Bt_PCmdrVsTroopShots = shots; s.Bt_PCmdrVsTroopDmg = dmg;
        s.Bt_PCmdrVsCmdrShots = shots; s.Bt_PCmdrVsCmdrDmg = dmg;
        s.Bt_ETroopVsCmdrShots = shots; s.Bt_ETroopVsCmdrDmg = dmg;
        s.Bt_ETroopVsTroopShots = shots; s.Bt_ETroopVsTroopDmg = dmg;
        s.Bt_ECmdrVsTroopShots = shots; s.Bt_ECmdrVsTroopDmg = dmg;
        s.Bt_ECmdrVsCmdrShots = shots; s.Bt_ECmdrVsCmdrDmg = dmg;
    }

    private static int[] Repeat(int value, int count) => Enumerable.Repeat(value, count).ToArray();

    // ------------------------------------------------------------------------------------
    // Scenario A: 30 vs 20 troops, direct attack, the enemy counters; all six sub-exchanges
    // that apply run with ≤ 80 shots.
    // ------------------------------------------------------------------------------------

    private static ResolutionFixture ScenarioA(params int[] draws)
    {
        var fx = ResolutionFixture.Create(draws);
        var s = fx.State;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_PlayerActSlot = 1;
        s.Bt_PSlot1State = 1; // the actor has acted (Q12)
        s.Bt_EnemyActSlot = 1;
        s.Bt_PlayerAction = 0;
        s.Bt_EnemyAction = 0;
        var u = fx.Unit(1);
        u.Troops = 30; u.TroopAtk = 8; u.TroopDef = 8; u.Atk = 10; u.Def = 8; u.Attacks = 10;
        u.HP = 20; u.Magic = 3; u.Level = 2;
        var c = fx.Cmdr(1);
        c.Troops = 20; c.TroopAtk = 6; c.TroopDef = 6; c.Atk = 8; c.Def = 6; c.Attacks = 10;
        c.HP = 15; c.Magic = 2;
        return fx;
    }

    [Fact]
    public void Scenario_A_small_melee_with_counter()
    {
        var fx = ScenarioA(
            10, 50,                                  // a: hit 16 + 2 = 18; 2 shots (30 / 20 * 2) → 1 hit
            4, 1, 100, 34, 35, 2, 3, 99, 10, 20,     // b: hit 32 + 2 = 34; RND(10) = 4 → 9 shots → 6 hits
            1,                                       // c: 28 shots * 18 / 100 = 5, +1 → 6
            2, 5, 6, 1, 100, 50, 4, 7,               // e: hit 8 - 2 - 1 = 5; RND(10) = 2 → 7 shots → 3 hits
            1);                                      // f: hit 6 - 2 - 1 = 3; 20 * 3 / 100 = 0, +1 → 1
        fx.Battle.ComputeExchange();
        // d: 20 / 30 * 2 = 0 shots → ETroopsVsCmdr does not run.
        Assert.Equal([2, 1, 28, 6, 9, 6, 0, 0, 0, 0, 20, 1, 7, 3, 0, 0], Results(fx.State));
        Assert.Equal([100, 100, 10, .. Repeat(100, 9), 3, 10, .. Repeat(100, 7), 3], fx.Random.Draws);
        Assert.Equal(1, (int)fx.State.Bt_PlayerSlotDef);
        Assert.Equal(0, (int)fx.State.Bt_EnemySlotDef);
        Assert.Equal(30, (int)fx.State.Bt_PlayerEngaged);
        Assert.Equal(20, (int)fx.State.Bt_EnemyEngaged);
        Assert.Equal(1, (int)fx.State.Bt_PlayerAllEngaged);
        Assert.Equal(1, (int)fx.State.Bt_EnemyAllEngaged);
        Assert.Equal(3, (int)fx.State.Bt_HitChance);
        Assert.Equal(0, fx.Random.Remaining);
    }

    [Fact]
    public void Scenario_A_partner_draws_come_first_in_each_troop_routine()
    {
        // Partner 7 in slot 2: RND(3) inside AtkMods (a, c) and DefMods (e, f); 3 = no bonus.
        var fx = ScenarioA(
            3, 10, 50,
            4, 1, 100, 34, 35, 2, 3, 99, 10, 20,
            3, 1,
            3, 2, 5, 6, 1, 100, 50, 4, 7,
            3, 1);
        fx.Unit(1).Partner = 7;
        fx.State.Bt_PlayerSlot1 = 1;
        fx.State.Bt_PlayerSlot2 = 7;
        fx.Battle.ComputeExchange();
        Assert.Equal([2, 1, 28, 6, 9, 6, 0, 0, 0, 0, 20, 1, 7, 3, 0, 0], Results(fx.State));
        Assert.Equal(
            [3, 100, 100, 10, .. Repeat(100, 9), 3, 3, 3, 10, .. Repeat(100, 7), 3, 3],
            fx.Random.Draws);
    }

    [Fact]
    public void Results_are_reset_at_entry()
    {
        var fx = ScenarioA(10, 50, 4, 1, 100, 34, 35, 2, 3, 99, 10, 20, 1, 2, 5, 6, 1, 100, 50, 4, 7, 1);
        SetAllResults(fx.State, 999, 999);
        fx.Battle.ComputeExchange();
        Assert.Equal(0, (int)fx.State.Bt_PCmdrVsCmdrShots);
        Assert.Equal(0, (int)fx.State.Bt_ETroopVsCmdrDmg);
        Assert.Equal(0, (int)fx.State.Bt_ECmdrVsCmdrDmg);
    }

    // ------------------------------------------------------------------------------------
    // Scenario B: 2000 troops against 20 with 100 % hit chance — > 80 paths, the 655 cap,
    // phantom shots, stale operands for the enemy's action 99.
    // ------------------------------------------------------------------------------------

    [Fact]
    public void Scenario_B_large_volleys_saturation_and_phantom_shots()
    {
        var fx = ResolutionFixture.Create(30, 40, 1, 7, 1);
        var s = fx.State;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_PlayerActSlot = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_PlayerAction = 0;
        s.Bt_EnemyAction = 99;
        var u = fx.Unit(1);
        u.Troops = 2000; u.TroopAtk = 20; u.Atk = 10; u.Attacks = 0; u.TroopDef = 10; u.Def = 10; u.HP = 30;
        var c = fx.Cmdr(1);
        c.Troops = 20; c.Def = 5; c.TroopDef = 5; c.Atk = 5; c.Attacks = 0; c.HP = 30; c.TroopAtk = 5;

        fx.Battle.ComputeExchange();

        // a: 2000 / 20 * 2 = 200 shots at 100 % → 200 → k 20: 160 + RND(60) = 30 → 190
        // b: hit 40; RND(0) does not draw → 0 shots → Q3 phantom roll 40 → 1 hit
        // c: 1800 shots * 100 saturates → Q1 655 → k 65: 525 + RND(195) = 1 → 526;
        //    526 + 1 > 20 engaged → 20 - 1 = 19
        // d: 20 / 2000 * 2 = 0 → skipped
        // e: action 99, stale Tmp0001 = 20 / Tmp0002 = 5 → 100; RND(0) → 0 shots → phantom roll 7; zeroed
        // f: action 99, stale Tmp0001 = 0 → hit 3; 20 * 3 / 100 = 0 → RND(3) = 1; zeroed
        Assert.Equal([200, 190, 1800, 19, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], Results(s));
        Assert.Equal([60, 100, 195, 100, 3], fx.Random.Draws);
        Assert.Equal(3, (int)s.Bt_HitChance);
        Assert.Equal(65, (int)s.Bt_RoundStep); // Q4: last large spread
    }

    // ------------------------------------------------------------------------------------
    // Scenario C: 5 vs 5 troops — both commanders fight each other (engaged < 10).
    // ------------------------------------------------------------------------------------

    [Fact]
    public void Scenario_C_commander_duels_below_10_engaged()
    {
        var fx = ResolutionFixture.Create(
            4, 5,      // a: hit 3 + 1 = 4, 2 shots → 1
            6, 2,      // b: hit 16 + 1 = 17; RND(30 / 5) = 6 > 5 → RND(6) = 2 shots
            17, 18,    //    → 1 hit
            3,         // c: hit 9, 3 shots → 0, RND(3) = 3 → 0
            1, 2,      // d: 2 shots, hit 3 - 1 - 1 = 1 → 1
            1, 5,      // e: hit 7 - 1 - 1 = 5; RND(12 / 5) = 1 shot → hit
            1);        // f: hit 8 - 1 - 1 = 6, 3 shots → 0, +1 → 1
        var s = fx.State;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_PlayerActSlot = 1;
        s.Bt_PSlot1State = 1;
        s.Bt_EnemyActSlot = 1;
        var u = fx.Unit(1);
        u.Troops = 5; u.TroopAtk = 5; u.Atk = 12; u.Def = 10; u.Attacks = 30; u.Level = 1; u.TroopDef = 5; u.HP = 20;
        var c = fx.Cmdr(1);
        c.Troops = 5; c.TroopAtk = 5; c.TroopDef = 5; c.Atk = 9; c.Def = 10; c.Attacks = 12; c.HP = 20;

        fx.Battle.ComputeExchange();

        Assert.Equal([2, 1, 3, 0, 0, 0, 2, 1, 2, 1, 3, 1, 0, 0, 1, 1], Results(s));
        Assert.Equal([100, 100, 6, 6, 100, 100, 3, 100, 100, 2, 100, 3], fx.Random.Draws);
    }

    // ------------------------------------------------------------------------------------
    // Scenario D: armies above the battle limit (Q24) and the strategy result.
    // ------------------------------------------------------------------------------------

    private static ResolutionFixture ScenarioD(int strategy)
    {
        var fx = ResolutionFixture.Create(
            8, 9, 1, 100,              // a: 1000 / 500 * 2 = 4 shots at 8 % → 2
            10,                        // c: 996 * 8 / 100 = 79 → k 7: 65 + 10 = 75
            4, 16, 17, 1, 1, 50, 50,   // e: hit 16; RND(4) = 4 → 6 shots → 3
            12);                       // f: hit 8; 500 * 8 / 100 = 40 → k 4: 32 + 12 = 44
        var s = fx.State;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 1000;
        s.Bt_PlayerActSlot = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_StrategyResult = (Sys3Int)strategy;
        var u = fx.Unit(1);
        u.Troops = 3000; u.TroopAtk = 10; u.TroopDef = 10; u.Atk = 50; u.Attacks = 50;
        var c = fx.Cmdr(1);
        c.Troops = 500; c.Def = 10; c.TroopDef = 8; c.TroopDefMod = 2; c.Atk = 12; c.Attacks = 4;
        c.TroopAtk = 10; c.TroopAtkMod = 1;
        return fx;
    }

    [Fact]
    public void Scenario_D_a_commander_over_the_limit_does_not_fight_and_is_not_shot()
    {
        var fx = ScenarioD(0);
        fx.Battle.ComputeExchange();
        // b and d are skipped: the player's army exceeds the limit.
        Assert.Equal([4, 2, 996, 75, 0, 0, 0, 0, 0, 0, 500, 44, 6, 3, 0, 0], Results(fx.State));
        Assert.Equal([100, 100, 100, 100, 21, 4, .. Repeat(100, 6), 12], fx.Random.Draws);
        Assert.Equal(1000, (int)fx.State.Bt_PlayerEngaged);
        Assert.Equal(0, (int)fx.State.Bt_PlayerAllEngaged);
        Assert.Equal(500, (int)fx.State.Bt_EnemyEngaged);
        Assert.Equal(1, (int)fx.State.Bt_EnemyAllEngaged);
    }

    [Theory]
    [InlineData(1, 40, 82)]   // E - 44/10, P + 75/10
    [InlineData(2, 22, 112)]  // E - 44/2,  P + 75/2
    [InlineData(11, 48, 68)]  // E + 44/10, P - 75/10
    [InlineData(12, 66, 38)]  // E + 44/2,  P - 75/2
    [InlineData(3, 44, 75)]   // no effect
    public void Strategy_changes_only_troop_vs_troop_damage(int strategy, int enemy, int player)
    {
        var fx = ScenarioD(strategy);
        fx.Battle.ComputeExchange();
        Assert.Equal(enemy, (int)fx.State.Bt_ETroopVsTroopDmg);
        Assert.Equal(player, (int)fx.State.Bt_PTroopVsTroopDmg);
        Assert.Equal(3, (int)fx.State.Bt_ECmdrVsTroopDmg);
        Assert.Equal(2, (int)fx.State.Bt_PTroopVsCmdrDmg);
    }

    [Fact]
    public void Strategy_bonus_is_clamped_afterwards_by_the_enemy_engaged()
    {
        // Scenario D's armies with 80 enemy troops.
        var fx = ResolutionFixture.Create(
            [.. Repeat(1, 24),         // a: 1000 / 80 * 2 = 24 shots at 8 % → 24 hits
             10,                       // c: 976 * 8 / 100 = 78 → k 7: 64 + 10 = 74
             4, 16, 17, 1, 1, 50, 50,  // e: 6 shots → 3
             3]);                      // f: 80 * 8 / 100 = 6 → RND(3) = 3 → 6
        var s = fx.State;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 1000;
        s.Bt_PlayerActSlot = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_StrategyResult = 2;
        var u = fx.Unit(1);
        u.Troops = 3000; u.TroopAtk = 10; u.TroopDef = 10;
        var c = fx.Cmdr(1);
        c.Troops = 80; c.Def = 10; c.TroopDef = 8; c.TroopDefMod = 2; c.Atk = 12; c.Attacks = 4;
        c.TroopAtk = 10; c.TroopAtkMod = 1;

        fx.Battle.ComputeExchange();

        // strategy 2: E 6 - 3 = 3; P 74 + 37 = 111 > 80 engaged → 80 - 0
        Assert.Equal([24, 24, 976, 80, 0, 0, 0, 0, 0, 0, 80, 3, 6, 3, 0, 0], Results(s));
        Assert.Equal([.. Repeat(100, 24), 21, 4, .. Repeat(100, 6), 3], fx.Random.Draws);
        Assert.Equal(37, (int)s.Tmp0001);
    }

    // ------------------------------------------------------------------------------------
    // Specials inside ComputeExchange
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// Unit 1 with no troops against 200 enemy troops, direct attack with no counter: the base
    /// exchange draws [100 (b phantom), 3 (d spread), 100 (e), 3 (f spread)].
    /// </summary>
    private static ResolutionFixture SpecialBase(params int[] draws)
    {
        var fx = ResolutionFixture.Create(draws);
        var s = fx.State;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_PlayerActSlot = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_PlayerAction = 0;
        s.Bt_EnemyAction = 99;
        var u = fx.Unit(1);
        u.Troops = 0; u.Atk = 10; u.Def = 10; u.Attacks = 0; u.Magic = 5; u.HP = 30;
        var c = fx.Cmdr(1);
        c.Troops = 200; c.TroopAtk = 5; c.TroopDef = 5; c.Atk = 5; c.Def = 5; c.Attacks = 0; c.HP = 40; c.Magic = 5;
        return fx;
    }

    [Fact]
    public void Special_0_base_exchange()
    {
        var fx = SpecialBase(40, 2, 1, 1);
        fx.Battle.ComputeExchange();
        // b: hit 40, 0 shots (Unit_Attacks 0) → phantom hit. d: 200 / 0 * 2 = 0 and no player
        // troops → 200 shots, action 99 → zeroed. e, f: zeroed.
        Assert.Equal([0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], Results(fx.State));
        Assert.Equal([100, 3, 100, 3], fx.Random.Draws);
    }

    [Fact]
    public void Special_1_reruns_commander_vs_commander_and_zeroes_the_rest()
    {
        var fx = SpecialBase(40, 2, 1, 1, 40);
        fx.State.Bt_SpecialAtk = 1;
        fx.Battle.ComputeExchange();
        // L_0412d: hit 40, RND(0) → 1 shot, roll 40 → hit; PCVT (base 0/1) zeroed.
        Assert.Equal([0, 0, 0, 0, 0, 0, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0], Results(fx.State));
        Assert.Equal([100, 3, 100, 3, 100], fx.Random.Draws);
    }

    [Fact]
    public void Special_2_reruns_commander_vs_troops_against_the_whole_army()
    {
        // Magic: hit (8 + 5) - 5 = 8; b: special 2 → 200 shots → 16 → k 1: 14 + RND(3).
        // b leaves Tmp0001 = 200, so d (stale) hits 100 %: 200 → k 20: 160 + RND(60); e: 100 % →
        // RND(0) → 1 roll; f: stale Tmp0001 = 1 → hit 4, 0 shots → spread.
        var fx = SpecialBase(1, 30, 1, 1, 3);
        fx.State.Bt_PlayerAction = 2;
        fx.State.Bt_SpecialAtk = 2;
        fx.Battle.ComputeExchange();
        Assert.Equal([0, 0, 0, 0, 200, 17, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0], Results(fx.State));
        Assert.Equal([3, 60, 100, 3, 3], fx.Random.Draws);
    }

    [Fact]
    public void Special_3_against_a_demon_with_a_stale_unit_51()
    {
        // The ninja: unit 51 keeps a kaiju's Attacks 100 (02 §1.4), so b draws RND(100) and
        // spreads; the special then sets 1 shot / 1 damage and the demon shield zeroes it.
        var fx = ResolutionFixture.Create(50, 10, 1, 2, 1, 1, 1);
        var s = fx.State;
        s.Unit_Cur = 51;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_PlayerActSlot = 1;
        s.Bt_PSlot1State = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_PlayerAction = 0;
        s.Bt_EnemyAction = 99;
        s.Bt_SpecialAtk = 3;
        var u = fx.Unit(51);
        u.Troops = 0; u.HP = 3; u.Atk = 20; u.Attacks = 100;
        var c = fx.Cmdr(1);
        c.Troops = 100; c.Def = 5; c.TroopDef = 5; c.Attacks = 10; c.Atk = 5; c.HP = 30; c.Class = 10;

        fx.Battle.ComputeExchange();

        // b: hit 100; RND(100) = 50 → 100 shots → 100 → k 10: 80 + 10 = 90.
        // d: 100 shots, stale 100 vs 5 → 99 → 99 → k 9: 81 + RND(27); zeroed.
        // e: stale → 99; RND(2) = 2 shots → 2 rolls; zeroed.  f: stale 2 vs 5 → 4; RND(3); zeroed.
        Assert.Equal([0, 0, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0], Results(s));
        Assert.Equal([100, 30, 27, 2, 100, 100, 3], fx.Random.Draws);
        Assert.Equal(1, (int)s.Bt_EnemyCmdrShield);
    }

    [Theory]
    [InlineData(1, 1)] // Chaos breaks the shield
    [InlineData(2, 1)] // Nikkou
    [InlineData(3, 0)]
    public void Special_3_demon_shield_broken_by_chaos_or_nikkou(int item, int dmg)
    {
        var fx = ResolutionFixture.Create(50, 10, 1, 2, 1, 1, 1);
        var s = fx.State;
        s.Unit_Cur = 51;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_PlayerActSlot = 1;
        s.Bt_PSlot1State = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_EnemyAction = 99;
        s.Bt_SpecialAtk = 3;
        var u = fx.Unit(51);
        u.HP = 3; u.Atk = 20; u.Attacks = 100; u.Item = (Sys3Int)item;
        var c = fx.Cmdr(1);
        c.Troops = 100; c.Def = 5; c.TroopDef = 5; c.Attacks = 10; c.Atk = 5; c.HP = 30; c.Class = 10;
        fx.Battle.ComputeExchange();
        Assert.Equal(1, (int)s.Bt_PCmdrVsCmdrShots);
        Assert.Equal(dmg, (int)s.Bt_PCmdrVsCmdrDmg);
        Assert.Equal(item, (int)s.Item_Cur);
    }

    // ------------------------------------------------------------------------------------
    // ApplySpecialAttack (2324-2413) on preset results
    // ------------------------------------------------------------------------------------

    private static ResolutionFixture Preset(int special, params int[] draws)
    {
        var fx = ResolutionFixture.Create(draws);
        fx.State.Cmdr_Cur = 1;
        fx.Cmdr(1).Troops = 1000;
        fx.Cmdr(1).HP = 41;
        fx.State.Bt_SpecialAtk = (Sys3Int)special;
        SetAllResults(fx.State, 7, 7);
        return fx;
    }

    [Fact]
    public void Special_none_changes_nothing()
    {
        var fx = Preset(0);
        fx.Battle.ApplySpecialAttack();
        Assert.Equal(Repeat(7, 16), Results(fx.State));
    }

    [Fact]
    public void Special_3_high_speed_one_shot_one_damage()
    {
        var fx = Preset(3);
        fx.Battle.ApplySpecialAttack();
        Assert.Equal([0, 0, 0, 0, 0, 0, 1, 1, 7, 7, 7, 7, 7, 7, 7, 7], Results(fx.State));
    }

    [Fact]
    public void Special_4_vanish_bomb_half_hp_and_all_troops()
    {
        var fx = Preset(4);
        fx.Battle.ApplySpecialAttack();
        Assert.Equal([0, 0, 0, 0, 1000, 1000, 20, 20, 7, 7, 7, 7, 7, 7, 7, 7], Results(fx.State));
        Assert.Empty(fx.Random.Draws);
    }

    [Fact]
    public void Special_5_war_dance_hurts_patton_and_draws_half_the_troops()
    {
        // Q26: enemy troop → commander becomes T/5 shots, T/100 damage.
        var fx = Preset(5, 123);
        fx.Battle.ApplySpecialAttack();
        Assert.Equal([0, 0, 0, 0, 1000, 623, 0, 0, 200, 10, 7, 7, 7, 7, 7, 7], Results(fx.State));
        Assert.Equal([500], fx.Random.Draws);
    }

    [Fact]
    public void Special_5_with_3_troops_rnd1_does_not_draw()
    {
        var fx = Preset(5);
        fx.Cmdr(1).Troops = 3;
        fx.Battle.ApplySpecialAttack();
        // RND(3 / 2 = 1) returns 1 without drawing → 1 + 1
        Assert.Equal([0, 0, 0, 0, 3, 2, 0, 0, 0, 0, 7, 7, 7, 7, 7, 7], Results(fx.State));
        Assert.Empty(fx.Random.Draws);
    }

    [Fact]
    public void Special_7_soaring_wave()
    {
        var fx = Preset(7);
        fx.Battle.ApplySpecialAttack();
        Assert.Equal([0, 0, 0, 0, 500, 500, 2, 2, 7, 7, 7, 7, 7, 7, 7, 7], Results(fx.State));
    }

    [Fact]
    public void Special_10_goliath()
    {
        var fx = Preset(10);
        fx.Battle.ApplySpecialAttack();
        Assert.Equal([0, 0, 0, 0, 1000, 500, 0, 0, 7, 7, 7, 7, 7, 7, 7, 7], Results(fx.State));
    }

    [Fact]
    public void Special_100_cheater_beam()
    {
        var fx = Preset(100);
        fx.Battle.ApplySpecialAttack();
        Assert.Equal([.. Repeat(10000, 8), .. Repeat(7, 8)], Results(fx.State));
    }

    // ------------------------------------------------------------------------------------
    // ApplyDemonShields (2414-2448)
    // ------------------------------------------------------------------------------------

    [Theory]
    // unitClass, cmdrClass, cmdr, item → playerShield, enemyShield, ETVC, ECVC, PTVC, PCVC damage
    [InlineData(10, 0, 1, 0, 1, 0, 0, 0, 5, 5)]
    [InlineData(10, 10, 1, 0, 2, 2, 0, 5, 0, 5)]
    [InlineData(0, 11, 1, 0, 0, 1, 5, 5, 0, 0)]
    [InlineData(0, 11, 1, 1, 0, 2, 5, 5, 0, 5)]
    [InlineData(0, 11, 1, 2, 0, 2, 5, 5, 0, 5)]
    [InlineData(11, 10, 146, 0, 0, 2, 5, 5, 0, 5)]
    [InlineData(11, 10, 147, 0, 0, 2, 5, 5, 0, 5)]
    [InlineData(11, 0, 187, 0, 0, 0, 5, 5, 5, 5)]
    [InlineData(11, 10, 190, 0, 0, 2, 5, 5, 0, 5)]
    [InlineData(9, 9, 1, 0, 0, 0, 5, 5, 5, 5)]
    public void Demon_shields(
        int unitClass, int cmdrClass, int cmdr, int item,
        int pShield, int eShield, int etvc, int ecvc, int ptvc, int pcvc)
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Unit_Cur = 3;
        s.Cmdr_Cur = (Sys3Int)cmdr;
        fx.Unit(3).Class = (Sys3Int)unitClass;
        fx.Unit(3).Item = (Sys3Int)item;
        fx.Cmdr(cmdr).Class = (Sys3Int)cmdrClass;
        SetAllResults(s, 9, 5);
        fx.Battle.ApplyDemonShields();
        Assert.Equal(pShield, (int)s.Bt_PlayerCmdrShield);
        Assert.Equal(eShield, (int)s.Bt_EnemyCmdrShield);
        Assert.Equal(etvc, (int)s.Bt_ETroopVsCmdrDmg);
        Assert.Equal(ecvc, (int)s.Bt_ECmdrVsCmdrDmg);
        Assert.Equal(ptvc, (int)s.Bt_PTroopVsCmdrDmg);
        Assert.Equal(pcvc, (int)s.Bt_PCmdrVsCmdrDmg);
        Assert.Equal(5, (int)s.Bt_PTroopVsTroopDmg);
        Assert.Equal(9, (int)s.Bt_PCmdrVsCmdrShots); // shields zero damage only
        Assert.Equal(9, (int)s.Bt_ETroopVsCmdrShots);
    }
}
