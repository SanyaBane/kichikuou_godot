using Kichikuou.Engine.Sys3;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// The exchange helpers of <c>FIGT.ADV:2137-2199</c> and <c>2450-2483</c>
/// (docs/battle/02-resolution.md §6.1). Expected values derived by hand from the script.
/// </summary>
public class ResolutionHelpersTests
{
    // ---- L_0347f HitChance ----

    [Theory]
    [InlineData(10, 5, 40)]      // (10-5)*8
    [InlineData(30, 10, 100)]    // 160 > 99 → 100
    [InlineData(7, 5, 16)]       // +2 → 16
    [InlineData(6, 5, 8)]        // Q2: +1 → 8
    [InlineData(5, 5, 8)]        // Q2: (8+5)-5 = 8
    [InlineData(5, 9, 4)]        // (8+5)-9
    [InlineData(2, 20, 1)]       // (8+2)-20 floors at 0 → 1
    [InlineData(65535, 0, 100)]  // 65535*8 saturates, > 99 → 100
    [InlineData(65535, 65535, 1)] // 8+65535 saturates at 65535, -65535 = 0 → 1
    public void HitChance_follows_L_0347f(int atk, int def, int expected)
    {
        var fx = ResolutionFixture.Create();
        fx.State.Tmp0001 = (Sys3Int)atk;
        fx.State.Tmp0002 = (Sys3Int)def;
        fx.Battle.HitChance();
        Assert.Equal(expected, (int)fx.State.Bt_HitChance);
        Assert.Empty(fx.Random.Draws);
    }

    // ---- L_034d3 ClampHit ----

    [Theory]
    [InlineData(150, 100)]
    [InlineData(100, 100)]
    [InlineData(0, 1)]
    [InlineData(37, 37)]
    public void ClampHit_limits_to_1_100(int value, int expected)
    {
        var fx = ResolutionFixture.Create();
        fx.State.Bt_HitChance = (Sys3Int)value;
        fx.Battle.ClampHit();
        Assert.Equal(expected, (int)fx.State.Bt_HitChance);
    }

    // ---- L_03502 Spread ----

    [Theory]
    [InlineData(5, 1, 6)]
    [InlineData(5, 2, 4)]
    [InlineData(5, 3, 5)]
    [InlineData(10, 1, 11)]
    [InlineData(0, 2, 0)] // floors at 0
    [InlineData(0, 1, 1)] // Q3: zero expected hits can still kill one
    public void Spread_below_11_moves_by_one(int expected, int draw, int result)
    {
        var fx = ResolutionFixture.Create(draw);
        fx.State.CallArg = (Sys3Int)expected;
        fx.State.Bt_RoundStep = 99;
        fx.Battle.Spread();
        Assert.Equal(result, (int)fx.State.CallArg);
        Assert.Equal([3], fx.Random.Draws);
        Assert.Equal(99, (int)fx.State.Bt_RoundStep); // small path leaves it alone
    }

    [Theory]
    [InlineData(11, 1, 1, 10)]       // k=1: 11-2+1
    [InlineData(11, 3, 1, 12)]       // 11-2+3
    [InlineData(100, 1, 10, 81)]     // Q5: k=10: 100-20+1 (lowest)
    [InlineData(100, 30, 10, 110)]   // 100-20+30 (highest)
    [InlineData(655, 195, 65, 720)]  // Q1+Q5: the largest possible troop kill before strategy
    public void Spread_from_11_is_biased(int expected, int draw, int k, int result)
    {
        var fx = ResolutionFixture.Create(draw);
        fx.State.CallArg = (Sys3Int)expected;
        fx.Battle.Spread();
        Assert.Equal(result, (int)fx.State.CallArg);
        Assert.Equal([k * 3], fx.Random.Draws);
        Assert.Equal(k, (int)fx.State.Bt_RoundStep); // Q4: VAR0623 clobbered
        Assert.Equal(draw, (int)fx.State.RetVal);
    }

    // ---- L_0356f / L_035b5 RollShots ----

    [Fact]
    public void RollShots_hits_when_the_roll_is_at_most_the_hit_chance()
    {
        var fx = ResolutionFixture.Create(50, 51, 1);
        fx.State.Tmp0001 = 3;
        fx.State.Bt_HitChance = 50;
        fx.Battle.RollShots();
        Assert.Equal(2, (int)fx.State.CallArg);
        Assert.Equal([100, 100, 100], fx.Random.Draws);
        Assert.Equal(4, (int)fx.State.Tmp0004);
    }

    [Fact]
    public void RollShots_rolls_once_for_zero_shots()
    {
        // Q3: do-while — a phantom shot.
        var fx = ResolutionFixture.Create(1);
        fx.State.Tmp0001 = 0;
        fx.State.Bt_HitChance = 1;
        fx.Battle.RollShots();
        Assert.Equal(1, (int)fx.State.CallArg);
        Assert.Equal([100], fx.Random.Draws);
    }

    [Fact]
    public void RollShots_misses_on_n_plus_1_even_at_100_percent()
    {
        // Q20: xsystem35 can return n+1.
        var fx = ResolutionFixture.Create(100, 101);
        fx.State.Tmp0001 = 2;
        fx.State.Bt_HitChance = 100;
        fx.Battle.RollShots();
        Assert.Equal(1, (int)fx.State.CallArg);
    }

    // ---- L_03c69 AtkMods / L_03cf0 DefMods ----

    [Theory]
    [InlineData(3, 13)]   // 0-9 = +n
    [InlineData(10, 10)]  // 10 = no change
    [InlineData(13, 7)]   // 11-19 = -(n-10)
    [InlineData(19, 1)]
    public void AtkMods_decodes_the_situational_modifier(int mod, int expected)
    {
        var fx = ResolutionFixture.Create();
        fx.State.Unit_Cur = 4;
        fx.Unit(4).CurAtkMod = (Sys3Int)mod;
        fx.State.Tmp0001 = 10;
        fx.Battle.AtkMods();
        Assert.Equal(expected, (int)fx.State.Tmp0001);
        Assert.Empty(fx.Random.Draws);
    }

    [Fact]
    public void AtkMods_subtraction_floors_at_zero()
    {
        var fx = ResolutionFixture.Create();
        fx.State.Unit_Cur = 4;
        fx.Unit(4).CurAtkMod = 15;
        fx.State.Tmp0001 = 2;
        fx.Battle.AtkMods();
        Assert.Equal(0, (int)fx.State.Tmp0001);
    }

    [Theory]
    [InlineData(1, 12)]
    [InlineData(2, 11)]
    [InlineData(3, 10)]
    public void AtkMods_partner_in_a_slot_rolls_a_bonus(int draw, int expected)
    {
        var fx = ResolutionFixture.Create(draw);
        fx.State.Unit_Cur = 4;
        fx.Unit(4).Partner = 9;
        fx.State.Bt_PlayerSlot3 = 9;
        fx.State.Tmp0001 = 10;
        fx.Battle.AtkMods();
        Assert.Equal(expected, (int)fx.State.Tmp0001);
        Assert.Equal([3], fx.Random.Draws);
    }

    [Fact]
    public void Partner_zero_does_not_match_an_empty_slot()
    {
        var fx = ResolutionFixture.Create();
        fx.State.Unit_Cur = 4;
        fx.Unit(4).Partner = 0;
        fx.State.Bt_PlayerSlot1 = 4; // slots 2-4 empty (0)
        fx.State.Tmp0001 = 10;
        fx.State.Tmp0002 = 10;
        fx.Battle.AtkMods();
        fx.Battle.DefMods();
        Assert.Empty(fx.Random.Draws);
    }

    [Fact]
    public void Partner_absent_from_the_slots_gives_no_draw()
    {
        var fx = ResolutionFixture.Create();
        fx.State.Unit_Cur = 4;
        fx.Unit(4).Partner = 9;
        fx.State.Bt_PlayerSlot1 = 4;
        fx.State.Bt_PlayerSlot2 = 8;
        fx.Battle.AtkMods();
        Assert.Empty(fx.Random.Draws);
    }

    [Theory]
    [InlineData(2, 1, 14)]  // +2 and partner +2
    [InlineData(12, 2, 9)]  // -2 and partner +1
    [InlineData(12, 3, 8)]
    public void DefMods_works_on_Tmp0002(int mod, int draw, int expected)
    {
        var fx = ResolutionFixture.Create(draw);
        fx.State.Unit_Cur = 4;
        fx.Unit(4).CurDefMod = (Sys3Int)mod;
        fx.Unit(4).Partner = 6;
        fx.State.Bt_PlayerSlot4 = 6;
        fx.State.Tmp0001 = 77;
        fx.State.Tmp0002 = 10;
        fx.Battle.DefMods();
        Assert.Equal(expected, (int)fx.State.Tmp0002);
        Assert.Equal(77, (int)fx.State.Tmp0001);
        Assert.Equal([3], fx.Random.Draws);
    }
}
