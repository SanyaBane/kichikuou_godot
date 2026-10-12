using Kichikuou.Engine.Sys3;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// The six sub-exchanges of <c>ComputeExchange</c> called directly (<c>FIGT.ADV:2484-2857</c>,
/// docs/battle/02-resolution.md §6.3-§6.9), with unit 1 against commander 1.
/// </summary>
public class ResolutionSubExchangeTests
{
    private static ResolutionFixture Arrange(params int[] draws)
    {
        var fx = ResolutionFixture.Create(draws);
        fx.State.Unit_Cur = 1;
        fx.State.Cmdr_Cur = 1;
        fx.State.Item_Cur = 0;
        fx.State.Bt_EnemyEngaged = 1000;
        fx.State.Bt_PlayerEngaged = 1000;
        return fx;
    }

    private static int[] Repeat(int value, int count) => Enumerable.Repeat(value, count).ToArray();

    // ---- §6.3 PlayerTroopsVsCmdr (L_03d77) ----

    [Fact]
    public void PlayerTroopsVsCmdr_80_shots_roll_each_shot()
    {
        var fx = Arrange(Repeat(1, 80));
        fx.Unit(1).TroopAtk = 10;
        fx.Cmdr(1).Def = 10;
        fx.State.Bt_PTroopVsCmdrShots = 80;
        fx.Battle.PlayerTroopsVsCmdr();
        Assert.Equal(8, (int)fx.State.Bt_HitChance);
        Assert.Equal(80, (int)fx.State.Bt_PTroopVsCmdrDmg);
        Assert.Equal(Repeat(100, 80), fx.Random.Draws);
    }

    [Fact]
    public void PlayerTroopsVsCmdr_81_shots_use_one_spread_draw()
    {
        var fx = Arrange(3);
        fx.Unit(1).TroopAtk = 10;
        fx.Cmdr(1).Def = 10;
        fx.State.Bt_PTroopVsCmdrShots = 81;
        fx.Battle.PlayerTroopsVsCmdr();
        // 81 * 8 / 100 = 6, RND(3) = 3 → unchanged
        Assert.Equal(6, (int)fx.State.Bt_PTroopVsCmdrDmg);
        Assert.Equal(81, (int)fx.State.Bt_PTroopVsCmdrShots);
        Assert.Equal([3], fx.Random.Draws);
    }

    [Fact]
    public void PlayerTroopsVsCmdr_expected_hits_saturate_at_655()
    {
        // Q1: 1000 * 100 saturates at 65535 before / 100.
        var fx = Arrange(100);
        fx.Unit(1).TroopAtk = 30;
        fx.Cmdr(1).Def = 10;
        fx.State.Bt_PTroopVsCmdrShots = 1000;
        fx.Battle.PlayerTroopsVsCmdr();
        // CallArg 655 → k = 65: 655 - 130 + 100
        Assert.Equal(625, (int)fx.State.Bt_PTroopVsCmdrDmg);
        Assert.Equal([195], fx.Random.Draws);
    }

    [Fact]
    public void PlayerTroopsVsCmdr_adds_level_and_subtracts_the_target_slot_state()
    {
        var fx = Arrange(25);
        fx.Unit(1).TroopAtk = 10;
        fx.Unit(1).Level = 5;
        fx.Cmdr(1).Def = 5;
        fx.State.Bt_EnemySlotDef = 20; // AI defended
        fx.State.Bt_PTroopVsCmdrShots = 1;
        fx.Battle.PlayerTroopsVsCmdr();
        Assert.Equal(25, (int)fx.State.Bt_HitChance); // 40 + 5 - 20
        Assert.Equal(1, (int)fx.State.Bt_PTroopVsCmdrDmg);
    }

    [Fact]
    public void PlayerTroopsVsCmdr_item_effect_3_adds_to_physical_troop_attack_only()
    {
        var fx = Arrange(1, 1);
        fx.State.Items[3].Effect = 3;
        fx.State.Items[3].Amount = 10;
        fx.State.Item_Cur = 3;
        fx.Unit(1).TroopAtk = 6;
        fx.Unit(1).Magic = 6;
        fx.Cmdr(1).Def = 4;
        fx.Cmdr(1).Magic = 4;
        fx.State.Bt_PTroopVsCmdrShots = 1;

        fx.State.Bt_PlayerAction = 1;
        fx.Battle.PlayerTroopsVsCmdr();
        Assert.Equal(96, (int)fx.State.Bt_HitChance); // (16 - 4) * 8

        fx.State.Bt_PlayerAction = 2;
        fx.Battle.PlayerTroopsVsCmdr();
        Assert.Equal(16, (int)fx.State.Bt_HitChance); // (6 - 4) * 8, no item
    }

    [Fact]
    public void PlayerTroopsVsCmdr_with_action_99_uses_stale_operands_draws_and_zeroes()
    {
        // 02 §1.4: Tmp0001/Tmp0002 are not set for action 99.
        var fx = Arrange(1, 1);
        fx.State.Bt_PlayerAction = 99;
        fx.State.Tmp0001 = 50;
        fx.State.Tmp0002 = 0;
        fx.State.Bt_PTroopVsCmdrShots = 2;
        fx.Battle.PlayerTroopsVsCmdr();
        Assert.Equal(100, (int)fx.State.Bt_HitChance);
        Assert.Equal(0, (int)fx.State.Bt_PTroopVsCmdrShots);
        Assert.Equal(0, (int)fx.State.Bt_PTroopVsCmdrDmg);
        Assert.Equal([100, 100], fx.Random.Draws);
    }

    // ---- §6.4 PlayerTroopsVsTroops (L_03e79) ----

    [Theory]
    [InlineData(1, 18, 9, 59)] // walls: 72 - 10 = 62 → CallArg 62, k 6: 62 - 12 + 9
    [InlineData(2, 21, 9, 67)] // no wall penalty for Kind 2: 72 → k 7: 72 - 14 + 9
    public void PlayerTroopsVsTroops_wall_penalty_only_when_attacking_a_city(int kind, int n, int draw, int dmg)
    {
        var fx = Arrange(draw);
        fx.State.Bt_Kind = (Sys3Int)kind;
        fx.State.Bt_WallBonus = 5;
        fx.State.Items[3].Effect = 3;
        fx.State.Items[3].Amount = 2;
        fx.State.Item_Cur = 3;
        fx.Unit(1).TroopAtk = 10;
        fx.Cmdr(1).TroopDef = 2;
        fx.Cmdr(1).TroopDefMod = 1; // Q9: added raw
        fx.State.Bt_PlayerEngaged = 100;
        fx.State.Bt_EnemyEngaged = 500;
        fx.Battle.PlayerTroopsVsTroops();
        Assert.Equal(100, (int)fx.State.Bt_PTroopVsTroopShots);
        Assert.Equal(dmg, (int)fx.State.Bt_PTroopVsTroopDmg);
        Assert.Equal([n], fx.Random.Draws);
    }

    [Fact]
    public void PlayerTroopsVsTroops_shots_are_the_engaged_minus_the_commander_shots_and_clamped()
    {
        var fx = Arrange(1);
        fx.Unit(1).TroopAtk = 30;
        fx.Cmdr(1).TroopDef = 10;
        fx.State.Bt_PlayerEngaged = 40;
        fx.State.Bt_PTroopVsCmdrShots = 15;
        fx.State.Bt_PCmdrVsTroopDmg = 10;
        fx.State.Bt_EnemyEngaged = 30;
        fx.Battle.PlayerTroopsVsTroops();
        // 25 shots * 100 % = 25 → k 2: 25 - 4 + 1 = 22; 22 + 10 > 30 → 30 - 10
        Assert.Equal(25, (int)fx.State.Bt_PTroopVsTroopShots);
        Assert.Equal(20, (int)fx.State.Bt_PTroopVsTroopDmg);
        Assert.Equal([6], fx.Random.Draws);
    }

    [Fact]
    public void PlayerTroopsVsTroops_zero_shots_still_spread()
    {
        // Q3: always the spread path, so 0 shots can kill 1.
        var fx = Arrange(1);
        fx.State.Bt_PlayerEngaged = 4;
        fx.State.Bt_PTroopVsCmdrShots = 4;
        fx.Battle.PlayerTroopsVsTroops();
        Assert.Equal(0, (int)fx.State.Bt_PTroopVsTroopShots);
        Assert.Equal(1, (int)fx.State.Bt_PTroopVsTroopDmg);
    }

    [Fact]
    public void PlayerTroopsVsTroops_magic_uses_enemy_magic_plus_troop_def_mod()
    {
        var fx = Arrange(3);
        fx.State.Bt_PlayerAction = 2;
        fx.Unit(1).Magic = 9;
        fx.Unit(1).TroopAtk = 50;
        fx.Cmdr(1).Magic = 3;
        fx.Cmdr(1).TroopDefMod = 2;
        fx.State.Bt_PlayerEngaged = 10;
        fx.Battle.PlayerTroopsVsTroops();
        Assert.Equal(32, (int)fx.State.Bt_HitChance); // (9 - (3 + 2)) * 8
        Assert.Equal(3, (int)fx.State.Bt_PTroopVsTroopDmg); // 10 * 32 / 100 = 3, RND(3) = 3
    }

    // ---- §6.5 PlayerCmdrVsTroops (L_03f93) ----

    [Fact]
    public void PlayerCmdrVsTroops_shots_are_rnd_attacks_plus_half_and_damage_capped_by_engaged()
    {
        var fx = Arrange([3, .. Repeat(1, 8)]);
        fx.State.Items[5].Effect = 1;
        fx.State.Items[5].Amount = 3;
        fx.State.Item_Cur = 5;
        fx.Unit(1).Atk = 12;
        fx.Unit(1).Attacks = 10;
        fx.Cmdr(1).TroopDef = 5;
        fx.State.Bt_EnemyEngaged = 2;
        fx.Battle.PlayerCmdrVsTroops();
        Assert.Equal(80, (int)fx.State.Bt_HitChance); // (15 - 5) * 8
        Assert.Equal(8, (int)fx.State.Bt_PCmdrVsTroopShots); // 3 + 10 / 2
        Assert.Equal(2, (int)fx.State.Bt_PCmdrVsTroopDmg); // 8 hits, capped at 2 engaged
        Assert.Equal([10, .. Repeat(100, 8)], fx.Random.Draws);
    }

    [Fact]
    public void PlayerCmdrVsTroops_chuhai_shots_are_rnd15_hundreds_capped_by_troops()
    {
        var fx = Arrange(2, 3, 15);
        fx.State.Bt_PlayerAction = 2;
        fx.State.Bt_SpecialAtk = 6;
        fx.Unit(1).Magic = 10;
        fx.Unit(1).Attacks = 4;
        fx.Cmdr(1).Magic = 5;
        fx.Cmdr(1).Troops = 250;
        fx.State.Bt_EnemyEngaged = 90;
        fx.Battle.PlayerCmdrVsTroops();
        // hit 40; RND(15) = 3 → 300 > 250 → 250 shots; 250 * 40 / 100 = 100 → k 10: 80 + 15 = 95 → cap 90
        Assert.Equal(250, (int)fx.State.Bt_PCmdrVsTroopShots);
        Assert.Equal(90, (int)fx.State.Bt_PCmdrVsTroopDmg);
        Assert.Equal([4, 15, 30], fx.Random.Draws);
    }

    [Fact]
    public void PlayerCmdrVsTroops_epo_light_adds_3_and_the_beam_is_not_capped()
    {
        var fx = Arrange(2, 1);
        fx.State.Unit_Cur = 37; // Gandhi
        fx.State.Bt_PlayerAction = 2;
        fx.State.Bt_SpecialAtk = 2;
        fx.Unit(37).Magic = 10;
        fx.Unit(37).Attacks = 4;
        fx.Cmdr(1).Magic = 5;
        fx.Cmdr(1).Troops = 250;
        fx.State.Bt_EnemyEngaged = 90;
        fx.Battle.PlayerCmdrVsTroops();
        // (13 - 5) * 8 = 64; shots = all 250 troops; 160 → k 16: 128 + 1 = 129, special 2 → no cap
        Assert.Equal(64, (int)fx.State.Bt_HitChance);
        Assert.Equal(250, (int)fx.State.Bt_PCmdrVsTroopShots);
        Assert.Equal(129, (int)fx.State.Bt_PCmdrVsTroopDmg);
        Assert.Equal([4, 48], fx.Random.Draws);
    }

    [Fact]
    public void PlayerCmdrVsTroops_magic_item_effect_5_adds_to_magic()
    {
        var fx = Arrange(1);
        fx.State.Items[7].Effect = 5;
        fx.State.Items[7].Amount = 2;
        fx.State.Item_Cur = 7;
        fx.State.Bt_PlayerAction = 2;
        fx.Unit(1).Magic = 6;
        fx.Cmdr(1).Magic = 5;
        fx.Battle.PlayerCmdrVsTroops();
        Assert.Equal(24, (int)fx.State.Bt_HitChance); // (8 - 5) * 8
    }

    [Fact]
    public void PlayerCmdrVsTroops_beam_without_enemy_troops_still_rolls_a_phantom_shot()
    {
        // Q3: special 2 against 0 troops: 0 shots, one roll, the hit is not capped.
        var fx = Arrange(1);
        fx.State.Bt_PlayerAction = 2;
        fx.State.Bt_SpecialAtk = 2;
        fx.Unit(1).Magic = 5;
        fx.Cmdr(1).Troops = 0;
        fx.State.Bt_EnemyEngaged = 0;
        fx.Battle.PlayerCmdrVsTroops();
        Assert.Equal(0, (int)fx.State.Bt_PCmdrVsTroopShots);
        Assert.Equal(1, (int)fx.State.Bt_PCmdrVsTroopDmg);
        Assert.Equal([100], fx.Random.Draws); // Unit_Attacks 0: RND(0) does not draw
    }

    // ---- §6.6 PlayerCmdrVsCmdr (L_0412d) ----

    [Fact]
    public void PlayerCmdrVsCmdr_rerolls_rnd6_when_above_5()
    {
        var fx = Arrange(6, 4, 1, 1, 1, 100);
        fx.Unit(1).Atk = 12;
        fx.Unit(1).Attacks = 30;
        fx.Cmdr(1).Def = 10;
        fx.Battle.PlayerCmdrVsCmdr();
        Assert.Equal(4, (int)fx.State.Bt_PCmdrVsCmdrShots);
        Assert.Equal(3, (int)fx.State.Bt_PCmdrVsCmdrDmg);
        Assert.Equal([6, 6, 100, 100, 100, 100], fx.Random.Draws);
    }

    [Theory]
    [InlineData(4)] // RND(0) → 0 → 1 shot, no draw
    [InlineData(7)] // RND(1) → 1, no draw
    public void PlayerCmdrVsCmdr_few_attacks_give_one_shot_without_a_draw(int attacks)
    {
        var fx = Arrange(1);
        fx.Unit(1).Attacks = (Sys3Int)attacks;
        fx.Battle.PlayerCmdrVsCmdr();
        Assert.Equal(1, (int)fx.State.Bt_PCmdrVsCmdrShots);
        Assert.Equal([100], fx.Random.Draws);
    }

    // ---- §6.7 EnemyTroopsVsCmdr (L_04267) ----

    [Fact]
    public void EnemyTroopsVsCmdr_physical_uses_raw_troop_atk_mod_and_item_effect_2()
    {
        var fx = Arrange(27);
        fx.State.Items[4].Effect = 2;
        fx.State.Items[4].Amount = 3;
        fx.State.Item_Cur = 4;
        fx.Cmdr(1).TroopAtk = 10;
        fx.Cmdr(1).TroopAtkMod = 2;
        fx.Unit(1).Def = 5;
        fx.Unit(1).Level = 2;
        fx.State.Bt_PlayerSlotDef = 3;
        fx.State.Bt_ETroopVsCmdrShots = 1;
        fx.Battle.EnemyTroopsVsCmdr();
        Assert.Equal(27, (int)fx.State.Bt_HitChance); // (12 - 8) * 8 - 2 - 3
        Assert.Equal(1, (int)fx.State.Bt_ETroopVsCmdrDmg);
    }

    [Theory]
    [InlineData(5, 24)] // Q10: the magic-attack item also defends: (10 - 7) * 8
    [InlineData(8, 24)]
    [InlineData(1, 56)] // other effects do not: (10 - 3) * 8
    public void EnemyTroopsVsCmdr_magic_defence_items(int effect, int hit)
    {
        var fx = Arrange(1, 1);
        fx.State.Bt_EnemyAction = 2;
        fx.State.Items[4].Effect = (Sys3Int)effect;
        fx.State.Items[4].Amount = 4;
        fx.State.Item_Cur = 4;
        fx.Cmdr(1).Magic = 10;
        fx.Unit(1).Magic = 3;
        fx.State.Bt_ETroopVsCmdrShots = 2;
        fx.Battle.EnemyTroopsVsCmdr();
        Assert.Equal(hit, (int)fx.State.Bt_HitChance);
    }

    [Fact]
    public void EnemyTroopsVsCmdr_large_volley_spreads_and_action_99_zeroes()
    {
        var fx = Arrange(1);
        fx.State.Bt_EnemyAction = 99;
        fx.State.Tmp0001 = 0;
        fx.State.Tmp0002 = 5;
        fx.State.Bt_ETroopVsCmdrShots = 200;
        fx.Battle.EnemyTroopsVsCmdr();
        // stale operands: (8 + 0) - 5 = 3; 200 * 3 / 100 = 6 → spread RND(3)
        Assert.Equal(3, (int)fx.State.Bt_HitChance);
        Assert.Equal([3], fx.Random.Draws);
        Assert.Equal(0, (int)fx.State.Bt_ETroopVsCmdrShots);
        Assert.Equal(0, (int)fx.State.Bt_ETroopVsCmdrDmg);
    }

    // ---- §6.8 EnemyTroopsVsTroops (L_0438f) ----

    [Fact]
    public void EnemyTroopsVsTroops_wall_penalty_when_defending_a_city_and_def_mods()
    {
        var fx = Arrange(2);
        fx.State.Bt_Kind = 2;
        fx.State.Bt_WallBonus = 3;
        fx.State.Items[2].Effect = 4;
        fx.State.Items[2].Amount = 1;
        fx.State.Item_Cur = 2;
        fx.Cmdr(1).TroopAtk = 10;
        fx.Unit(1).TroopDef = 4;
        fx.Unit(1).CurDefMod = 1;
        fx.State.Bt_EnemyEngaged = 50;
        fx.State.Bt_PlayerEngaged = 100;
        fx.Battle.EnemyTroopsVsTroops();
        // def 4 + 1 + 1 = 6; (10 - 6) * 8 = 32 - 6 = 26; 50 * 26 / 100 = 13 → k 1: 11 + 2
        Assert.Equal(26, (int)fx.State.Bt_HitChance);
        Assert.Equal(50, (int)fx.State.Bt_ETroopVsTroopShots);
        Assert.Equal(13, (int)fx.State.Bt_ETroopVsTroopDmg);
        Assert.Equal([3], fx.Random.Draws);
    }

    [Fact]
    public void EnemyTroopsVsTroops_magic_defence_uses_effect_8_and_def_mods_with_partner()
    {
        var fx = Arrange(1, 2);
        fx.State.Bt_EnemyAction = 2;
        fx.State.Items[2].Effect = 8;
        fx.State.Items[2].Amount = 2;
        fx.State.Item_Cur = 2;
        fx.Cmdr(1).Magic = 12;
        fx.Unit(1).Magic = 3;
        fx.Unit(1).Partner = 4;
        fx.State.Bt_PlayerSlot2 = 4;
        fx.State.Bt_EnemyEngaged = 5;
        fx.Battle.EnemyTroopsVsTroops();
        // def 3 + 2 + partner 2 = 7; (12 - 7) * 8 = 40; 5 * 40 / 100 = 2 → RND(3) = 2 → 1
        Assert.Equal(40, (int)fx.State.Bt_HitChance);
        Assert.Equal(1, (int)fx.State.Bt_ETroopVsTroopDmg);
        Assert.Equal([3, 3], fx.Random.Draws);
    }

    [Fact]
    public void EnemyTroopsVsTroops_clamped_by_player_engaged_minus_commander_kills()
    {
        var fx = Arrange(3);
        fx.Cmdr(1).TroopAtk = 30;
        fx.State.Bt_EnemyEngaged = 60;
        fx.State.Bt_PlayerEngaged = 20;
        fx.State.Bt_ECmdrVsTroopDmg = 15;
        fx.Battle.EnemyTroopsVsTroops();
        // hit 100: 60 → k 6: 48 + 3 = 51; 51 + 15 > 20 → 20 - 15
        Assert.Equal(5, (int)fx.State.Bt_ETroopVsTroopDmg);
    }

    // ---- §6.9 EnemyCmdrVsTroops (L_044bf) / EnemyCmdrVsCmdr (L_045f7) ----

    [Fact]
    public void EnemyCmdrVsTroops_over_80_shots_spread_and_cap_at_player_engaged()
    {
        var fx = Arrange(60, 18);
        fx.Cmdr(1).Atk = 13;
        fx.Cmdr(1).Attacks = 120;
        fx.Unit(1).TroopDef = 6;
        fx.State.Bt_PlayerEngaged = 40;
        fx.Battle.EnemyCmdrVsTroops();
        // hit 56; shots 60 + 60 = 120; 120 * 56 / 100 = 67 → k 6: 55 + 18 = 73 → cap 40
        Assert.Equal(120, (int)fx.State.Bt_ECmdrVsTroopShots);
        Assert.Equal(40, (int)fx.State.Bt_ECmdrVsTroopDmg);
        Assert.Equal([120, 18], fx.Random.Draws);
    }

    [Fact]
    public void EnemyCmdrVsTroops_magic_has_no_def_mods()
    {
        var fx = Arrange(1);
        fx.State.Bt_EnemyAction = 2;
        fx.Cmdr(1).Magic = 9;
        fx.Unit(1).Magic = 4;
        fx.Unit(1).CurDefMod = 5;
        fx.Unit(1).Partner = 4;
        fx.State.Bt_PlayerSlot2 = 4;
        fx.Battle.EnemyCmdrVsTroops();
        Assert.Equal(40, (int)fx.State.Bt_HitChance);
        Assert.Equal([100], fx.Random.Draws); // no partner RND(3); Cmdr_Attacks 0: 1 phantom roll
    }

    [Fact]
    public void EnemyCmdrVsCmdr_kayblis_at_war_rolls_up_to_100_shots()
    {
        // Q22
        var fx = Arrange(1, 90, 2);
        fx.State.Cmdr_Cur = 135;
        fx.State.F_Kayblis_War = 2;
        fx.Cmdr(135).Atk = 9;
        fx.Cmdr(135).Attacks = 12;
        fx.Unit(1).Def = 10;
        fx.Unit(1).Level = 1;
        fx.State.Bt_PlayerSlotDef = 1;
        fx.Battle.EnemyCmdrVsCmdr();
        // hit (8 + 9 - 10) - 1 - 1 = 5; RND(2) = 1, RND(100) = 90; 90 * 5 / 100 = 4, RND(3) = 2 → 3
        Assert.Equal(90, (int)fx.State.Bt_ECmdrVsCmdrShots);
        Assert.Equal(3, (int)fx.State.Bt_ECmdrVsCmdrDmg);
        Assert.Equal([2, 100, 3], fx.Random.Draws);
    }

    [Fact]
    public void EnemyCmdrVsCmdr_kayblis_not_at_war_is_normal()
    {
        var fx = Arrange(1, 100);
        fx.State.Cmdr_Cur = 135;
        fx.State.F_Kayblis_War = 1;
        fx.Cmdr(135).Attacks = 12;
        fx.Battle.EnemyCmdrVsCmdr();
        Assert.Equal(1, (int)fx.State.Bt_ECmdrVsCmdrShots);
        Assert.Equal([2, 100], fx.Random.Draws);
    }

    [Fact]
    public void EnemyCmdrVsCmdr_rerolls_above_5_and_magic_defence_effect_5()
    {
        var fx = Arrange(6, 2, 50, 50);
        fx.State.Bt_EnemyAction = 2;
        fx.State.Items[9].Effect = 5;
        fx.State.Items[9].Amount = 2;
        fx.State.Item_Cur = 9;
        fx.Cmdr(1).Magic = 15;
        fx.Cmdr(1).Attacks = 30;
        fx.Unit(1).Magic = 3;
        fx.Battle.EnemyCmdrVsCmdr();
        Assert.Equal(80, (int)fx.State.Bt_HitChance); // (15 - 5) * 8
        Assert.Equal(2, (int)fx.State.Bt_ECmdrVsCmdrShots);
        Assert.Equal(2, (int)fx.State.Bt_ECmdrVsCmdrDmg);
        Assert.Equal([6, 6, 100, 100], fx.Random.Draws);
    }

    [Fact]
    public void Enemy_commander_routines_with_action_99_draw_and_zero()
    {
        // stale operands 0/0 → hit 8; RND(10) = 2 → 2 + 5 = 7 shots; then RND(10 / 5) = 1 → 1 shot
        var fx = Arrange([2, .. Repeat(1, 7), 1, 1]);
        fx.State.Bt_EnemyAction = 99;
        fx.Cmdr(1).Attacks = 10;
        fx.Battle.EnemyCmdrVsTroops();
        Assert.Equal(0, (int)fx.State.Bt_ECmdrVsTroopShots);
        Assert.Equal(0, (int)fx.State.Bt_ECmdrVsTroopDmg);
        fx.Battle.EnemyCmdrVsCmdr();
        Assert.Equal(0, (int)fx.State.Bt_ECmdrVsCmdrShots);
        Assert.Equal(0, (int)fx.State.Bt_ECmdrVsCmdrDmg);
        Assert.Equal([10, .. Repeat(100, 7), 2, 100], fx.Random.Draws);
    }
}
