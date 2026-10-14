namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// An exchange's blow as it lands (docs/ui/battle-screen.md, "The cards' counters"): the troops and HP the result
/// screen shows for the two fighters (<c>FIGTATTK.ADV:136-232</c>, the current values less the two damages,
/// saturating) — what the core writes into them only after the result's key wait (<c>ApplyExchangeAsync</c>).
/// Our cards show these from the blow on, so their counters run down with the scene's.
/// </summary>
public sealed record BattleStrike(int Unit, int UnitTroops, int UnitHp, int Cmdr, int CmdrTroops, int CmdrHp)
{
    /// <summary><paramref name="card"/> with the blow's values when it is one of the two fighters.</summary>
    public BattleCard Apply(BattleCard card) => card.Place.Side switch
    {
        BattleSide.Player when card.Record == Unit => card with { Troops = UnitTroops, Hp = UnitHp },
        BattleSide.Enemy when card.Record == Cmdr => card with { Troops = CmdrTroops, Hp = CmdrHp },
        _ => card,
    };
}
