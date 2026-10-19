using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// A Replenish asked for on our deployment screen (owner's decision 27, docs/ui/deployment-table.md, "Replenish on the
/// deployment screen"): the request on the context (<see cref="OriginalScriptContext.ReplenishRequest"/>) and a left click
/// at the unit's cell, which <c>Sﾌﾞﾀｲ</c> answers with <see cref="DeploymentScreen.ReplenishCode"/> — the screen does the
/// order and sums its party again. Keeps the unit's troops and the gold from before, for the plate after it
/// (the Subordinates screen's R4).
/// </summary>
public sealed class DeploymentReplenish
{
    private readonly OriginalScriptContext _script;
    private readonly int _troops, _maxTroops, _gold;

    private DeploymentReplenish(OriginalScriptContext script, int unit)
    {
        _script = script;
        Unit = unit;
        var u = script.State.Units[unit];
        (_troops, _maxTroops, _gold) = (u.Troops.Value, u.MaxTroops.Value, script.State.Gold.Value);
    }

    /// <summary>The unit asked for.</summary>
    public int Unit { get; }

    /// <summary>
    /// Asks <c>Sﾌﾞﾀｲ</c> to replenish <paramref name="unit"/>: the request set and a click at the unit's cell queued on
    /// <paramref name="pointer"/>. Null when the unit cannot be replenished now (<see cref="SubordinatesCommand.CanReplenish"/>).
    /// </summary>
    public static DeploymentReplenish? Start(OriginalScriptContext script, int unit, PointerScript pointer)
    {
        if (!SubordinatesCommand.CanReplenish(script.State, (Sys3Int)unit))
            return null;
        var order = new DeploymentReplenish(script, unit);
        script.ReplenishRequest = (Sys3Int)unit;
        var (x, y) = GesturePoints.PickerCell(unit);
        pointer.Click(x, y);
        return order;
    }

    /// <summary>Whether <c>Sﾌﾞﾀｲ</c> has taken the request (done, or found it could no longer be done).</summary>
    public bool Done => _script.ReplenishRequest == 0;

    /// <summary>What it did, for the plate; null before it is done or when nothing changed.</summary>
    public SubordinateResult? Result(GameState state)
    {
        var u = state.Units[Unit];
        if (!Done || state.Gold.Value == _gold)
            return null;
        return new SubordinateResult(SubordinateOrderKind.Replenish, DeploymentTable.UnitName(state, Unit),
            DeploymentTable.TroopType(state, Unit), u.FaceCg.Value, _troops, u.Troops.Value, _maxTroops, u.MaxTroops.Value,
            _gold - state.Gold.Value, state.Gold.Value);
    }
}
