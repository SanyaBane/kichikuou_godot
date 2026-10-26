using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.State;
using Kichikuou.TestSupport;

namespace Kichikuou.Original.Tests.Combat;

/// <summary>
/// The damage forecast (<see cref="ExchangeForecast"/>; docs/ui/damage-forecast.md, §3, §10) on states built by hand:
/// the game's state untouched, the same numbers twice, what the attack can reach. Its golden part is
/// <c>Golden/ExchangeForecastGoldenTests</c>.
/// </summary>
public class ExchangeForecastTests
{
    private static readonly FigtSelChoice MeleeOnSlot1 = new(FigtSelCommand.Direct, 4);

    /// <summary>
    /// Rance (unit 1) in player slot 1, acting, against commander 1 in enemy slot 1 at an attack (<c>Bt_Kind</c> 1):
    /// 30 troops against 20, the limit far above both, as <c>ResolutionComputeExchangeTests</c>' scenario A.
    /// </summary>
    private static GameState Small()
    {
        var s = new GameState();
        s.Bt_Kind = 1;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_PlayerSlot1 = 1;
        s.Bt_EnemySlot1 = 1;
        s.Bt_PlayerActSlot = 1;
        s.Bt_PSlot1State = 1;
        s.Bt_PSlot1Focus = 2;
        var u = s.Units[1];
        u.Status = 1; u.Troops = 30; u.TroopAtk = 8; u.TroopDef = 8; u.Atk = 10; u.Def = 8; u.Attacks = 10;
        u.HP = 20; u.Magic = 3; u.Level = 2;
        var c = s.Cmdrs[1];
        c.Status = 1; c.Troops = 20; c.TroopAtk = 6; c.TroopDef = 6; c.Atk = 8; c.Def = 6; c.Attacks = 10;
        c.HP = 15; c.Magic = 2;
        return s;
    }

    [Fact]
    public void Leaves_the_game_state_as_it_was()
    {
        var state = Small();
        var before = ScriptState.Copy(state);

        ExchangeForecast.Of(state, MeleeOnSlot1);
        ExchangeForecast.Of(state, new FigtSelChoice(FigtSelCommand.RanceAttack, 4));      // spends Focus on its copy

        Assert.Empty(Differential.Differences("before", before.Memory, state.Memory, v => VariableCatalog.Variables[v].Name));
    }

    [Fact]
    public void Gives_the_same_numbers_twice()
    {
        var state = Small();

        Assert.Equal(ExchangeForecast.Of(state, MeleeOnSlot1), ExchangeForecast.Of(state, MeleeOnSlot1));
    }

    [Fact]
    public void A_melee_on_a_small_army_reaches_its_troops_and_its_commander()
    {
        var forecast = ExchangeForecast.Of(Small(), MeleeOnSlot1);

        var troops = Assert.NotNull(forecast.Troops);
        var hp = Assert.NotNull(forecast.Hp);
        Assert.True(troops.Min <= troops.Low && troops.Low <= troops.High && troops.High <= troops.Max);
        Assert.InRange(troops.Max, 1, 20);                                   // never more than the commander's 20 troops
        Assert.InRange(hp.Max, 1, 15);
    }

    [Fact]
    public void A_loss_is_no_more_than_the_commander_has()
    {
        var state = Small();
        state.Cmdrs[1].HP = 1;
        state.Cmdrs[1].Troops = 3;
        state.Units[1].Troops = 400;                                          // 400 / 3 * 2 = 266 shots at him

        var forecast = ExchangeForecast.Of(state, MeleeOnSlot1);

        Assert.Equal(3, forecast.Troops!.Value.Max);
        Assert.Equal(new DamageSpread(1, 1, 1, 1), forecast.Hp);
    }

    [Fact]
    public void Whirlwind_Shot_reaches_the_commander_alone()
    {
        var forecast = ExchangeForecast.Of(Small(), new FigtSelChoice(FigtSelCommand.WhirlwindShot, 4));

        Assert.Null(forecast.Troops);
        Assert.Equal(new DamageSpread(1, 1, 1, 1), forecast.Hp);
    }

    [Fact]
    public void A_demon_shield_keeps_the_commander_out_of_reach()
    {
        var state = Small();
        state.Cmdrs[1].Class = 10;                                            // shield 1: neither stream hurts him

        var shot = ExchangeForecast.Of(state, new FigtSelChoice(FigtSelCommand.WhirlwindShot, 4));
        var melee = ExchangeForecast.Of(state, MeleeOnSlot1);

        Assert.Equal(new ExchangeForecast(null, null), shot);
        Assert.NotNull(melee.Troops);
        Assert.Null(melee.Hp);
    }

    [Fact]
    public void Armies_over_the_limit_fight_troops_alone()
    {
        var state = Small();
        state.Bt_TroopLimit = 232;
        state.Units[1].Troops = 451;                                          // no commander stream of ours (02 Q24)
        state.Cmdrs[1].Troops = 300;                                          // no troops shoot at him

        var forecast = ExchangeForecast.Of(state, MeleeOnSlot1);

        Assert.NotNull(forecast.Troops);
        Assert.Null(forecast.Hp);
    }

    [Fact]
    public void A_commander_without_troops_has_none_to_lose()
    {
        var state = Small();
        state.Cmdrs[1].Troops = 0;

        var forecast = ExchangeForecast.Of(state, MeleeOnSlot1);

        Assert.Null(forecast.Troops);
        Assert.NotNull(forecast.Hp);
    }

    [Theory]
    [InlineData(FigtSelCommand.Defend, 99)]
    [InlineData(FigtSelCommand.Direct, 99)]
    [InlineData(FigtSelCommand.Direct, 1)]
    public void Is_for_an_attack_on_an_enemy_slot(ushort command, ushort argument) =>
        Assert.Throws<ArgumentException>(() => ExchangeForecast.Of(Small(), new FigtSelChoice(command, argument)));

    [Fact]
    public void The_spread_is_the_whole_range_and_the_usual_ninety_percent()
    {
        int[] values = [.. Enumerable.Range(1, 100).Reverse()];

        Assert.Equal(new DamageSpread(1, 5, 95, 100), DamageSpread.Of(values));
        Assert.Equal(new DamageSpread(7, 7, 7, 7), DamageSpread.Of([7]));
    }
}
