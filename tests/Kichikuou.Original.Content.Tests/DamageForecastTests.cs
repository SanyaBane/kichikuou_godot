using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.State;
using Kichikuou.TestSupport;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The damage forecast's model (docs/ui/damage-forecast.md, §8 step 2): a banner's figures and the forecasts of one
/// <c>FIGT_SEL</c>, run off the game's thread on a snapshot.
/// </summary>
public class DamageForecastTests
{
    [Fact]
    public void A_range_reads_as_the_cards_figures() => Assert.Multiple(
        () => Assert.Equal("29–37", BannerDamage.Range(new DamageSpread(26, 29, 37, 40))),
        () => Assert.Equal("60", BannerDamage.Range(new DamageSpread(55, 60, 60, 60))),
        () => Assert.Equal("1,250–2,400", BannerDamage.Range(new DamageSpread(1250, 1250, 2400, 2490))));

    [Fact]
    public void A_loss_out_of_reach_has_no_figure()
    {
        var damage = BannerDamage.Of(new ExchangeForecast(null, new DamageSpread(1, 1, 1, 1)));

        Assert.Equal(new BannerDamage(null, "1"), damage);
    }

    [Fact]
    public async Task Every_banner_of_every_enemy_card_is_forecast_the_first_targets_first()
    {
        var (state, commands) = Turn();

        var forecasts = DamageForecasts.Start(state, commands, new BattlePlace(BattleSide.Enemy, 2));
        await forecasts.Done;

        // Rance (unit 1), a melee unit in front, reaches the enemy's front row alone: Melee on slots 1 and 2.
        Assert.Equal([new FigtSelChoice(FigtSelCommand.Direct, 5), new FigtSelChoice(FigtSelCommand.Direct, 4)], forecasts.Attacks);
        Assert.Equal(2, forecasts.Count);
        foreach (var attack in forecasts.Attacks)
            Assert.Equal(BannerDamage.Of(ExchangeForecast.Of(state, attack)), forecasts.For(attack));
        Assert.Null(forecasts.For(new FigtSelChoice(FigtSelCommand.Defend, BattleCommands.NoButton)));
    }

    [Fact]
    public async Task The_forecasts_read_the_state_as_it_was_at_their_start()
    {
        var (state, commands) = Turn();
        var melee = new FigtSelChoice(FigtSelCommand.Direct, 4);
        var expected = BannerDamage.Of(ExchangeForecast.Of(state, melee));

        var forecasts = DamageForecasts.Start(state, commands);
        state.Cmdrs[101].Troops = 0;                                           // the game goes on after the answer
        state.Cmdrs[101].HP = 0;
        await forecasts.Done;

        Assert.Equal(expected, forecasts.For(melee));
    }

    /// <summary>
    /// Rance (unit 1) acting from player slot 1 at an attack, 120 troops; commanders 101-103 in enemy slots 1-3, of 40,
    /// 60 and 80 troops; the core's own menu.
    /// </summary>
    private static (GameState State, BattleCommands Commands) Turn()
    {
        var s = new GameState();
        var battle = new Battle(s, new NoRandom(), new NullView(), new NoBattleInput(), new NoHooks());
        s.Bt_Kind = 1;
        s.Bt_TroopLimit = 232;
        s.Bt_PlayerSlot1 = 1;
        s.Bt_PlayerActSlot = 1;
        s.Bt_PSlot1State = 1;
        s.Unit_Cur = 1;
        var u = s.Units[1];
        u.Status = 1; u.Troops = 120; u.TroopAtk = 9; u.TroopDef = 7; u.Atk = 12; u.Def = 9; u.Attacks = 12; u.HP = 30;
        (s.Bt_EnemySlot1, s.Bt_EnemySlot2, s.Bt_EnemySlot3) = (101, 102, 103);
        for (int i = 1; i <= 3; i++)
        {
            var c = s.Cmdrs[100 + i];
            c.Status = 1; c.Troops = (Sys3Int)(20 + 20 * i); c.TroopAtk = 6; c.TroopDef = 6; c.Def = 7; c.Attacks = 8; c.HP = 6;
        }
        (s.Bt_PlayerSlotsUsed, s.Bt_EnemySlotsUsed) = (1, 3);
        return (s, new BattleCommands(battle.BuildFigtSelMenu()));
    }

    private sealed class NoBattleInput : UnexpectedInput, IBattleInput
    {
        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => throw new InvalidOperationException("Unexpected FIGT_SEL");
        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => throw new InvalidOperationException("Unexpected menu");
    }
}
