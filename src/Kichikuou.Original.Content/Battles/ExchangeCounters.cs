using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>One counter of an exchange: its value before the blow and after it.</summary>
public readonly record struct ExchangeCount(int From, int To);

/// <summary>
/// The exchange's counters on our scene (docs/ui/battle-screen.md, "The exchange's counters"): the two fighters'
/// troops and HP before the blow and after it — the values the result screen shows (<c>FIGTATTK.ADV:136-232</c>,
/// <see cref="BattleStrike"/>), known from the shots' first frame, since <c>FIGT</c> computes the damages before it
/// calls <c>FIGTATTK</c>. A side without troops shows no troops counter, as the original's TROOPS
/// (<c>FIGT.ADV:1830-1863</c>).
/// </summary>
public sealed record ExchangeCounters(ExchangeCount PlayerTroops, ExchangeCount PlayerHp, ExchangeCount EnemyTroops, ExchangeCount EnemyHp)
{
    /// <summary>Whether the player's unit has troops, so a troops counter.</summary>
    public bool PlayerHasTroops => PlayerTroops.From != 0;

    /// <summary>Whether the enemy commander has troops, so a troops counter.</summary>
    public bool EnemyHasTroops => EnemyTroops.From != 0;

    /// <summary>The counters of <paramref name="strike"/> from the fighters' values in <paramref name="s"/> before it.</summary>
    public static ExchangeCounters Of(GameState s, BattleStrike strike)
    {
        var unit = s.Units[strike.Unit];
        var cmdr = s.Cmdrs[strike.Cmdr];
        return new ExchangeCounters(
            new ExchangeCount(unit.Troops.Value, strike.UnitTroops), new ExchangeCount(unit.HP.Value, strike.UnitHp),
            new ExchangeCount(cmdr.Troops.Value, strike.CmdrTroops), new ExchangeCount(cmdr.HP.Value, strike.CmdrHp));
    }
}
