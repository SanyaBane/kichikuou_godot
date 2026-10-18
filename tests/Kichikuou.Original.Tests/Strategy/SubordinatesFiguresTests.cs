using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary>
/// The figures of <c>Cﾌﾞｶ</c> that the port and our own Subordinates screen share
/// (<see cref="SubordinatesCommand.Offer"/>, <see cref="SubordinatesCommand.AddMore"/>,
/// <see cref="SubordinatesCommand.Refill"/>; docs/ui/subordinates.md).
/// </summary>
public sealed class SubordinatesFiguresTests
{
    private static GameState State(int unit, int cls, int troopAtk, int week = 18)
    {
        var state = new GameState { Week = (Sys3Int)week };
        state.Units[unit].Class = (Sys3Int)cls;
        state.Units[unit].TroopAtk = (Sys3Int)troopAtk;
        return state;
    }

    [Theory]
    [InlineData(12, 0, 8, 54, 25)]      // Haurein (unit 12): soldiers, 50 + 18 / 4
    [InlineData(8, 0, 10, 114, 25)]     // Cordoba (unit 8): knights, 110
    [InlineData(11, 2, 3, 104, 25)]     // Asuka (unit 11): mages, 100
    [InlineData(3, 0, 6, 34, 50)]       // Bound (unit 3): 30 for 50 men
    [InlineData(5, 0, 9, 204, 25)]      // Leila (unit 5): 200
    [InlineData(2, 0, 6, 324, 10)]      // Sill (unit 2): none of the above, 320 for 10 men
    [InlineData(25, 0, 8, 0, 25)]       // Mill (unit 25): no offer
    [InlineData(48, 0, 8, 0, 25)]       // unit 48: no offer
    public void The_offer_is_by_class_troop_attack_and_unit_plus_a_quarter_of_the_week(int unit, int cls, int troopAtk, int price, int men)
    {
        var (p, m) = SubordinatesCommand.Offer(State(unit, cls, troopAtk), (Sys3Int)unit);

        Assert.Equal(price, p.Value);
        Assert.Equal(men, m.Value);
    }

    [Fact]
    public void Add_more_doubles_the_men_and_triples_the_price_while_it_is_under_10000()
    {
        var steps = new List<(int Price, int Men)>();
        (Sys3Int price, Sys3Int men) = ((Sys3Int)54, (Sys3Int)25);
        while (true)
        {
            steps.Add((price.Value, men.Value));
            if (!SubordinatesCommand.CanAddMore(price))
                break;
            (price, men) = SubordinatesCommand.AddMore(price, men);
        }

        Assert.Equal([(54, 25), (162, 50), (486, 100), (1458, 200), (4374, 400), (13122, 800)], steps);
    }

    [Theory]
    [InlineData(50, 1400, 1350, 27)]    // one per 50 men
    [InlineData(761, 800, 39, 1)]       // under 50: 1
    [InlineData(1200, 1000, 0, 1)]      // QUIRK: CMD-20 — above the maximum, 0 for 1
    public void Refill_is_the_deficiency_at_one_per_fifty_men(int troops, int max, int deficiency, int price)
    {
        var state = new GameState();
        state.Units[1].Troops = (Sys3Int)troops;
        state.Units[1].MaxTroops = (Sys3Int)max;

        var (d, p) = SubordinatesCommand.Refill(state.Units[1]);

        Assert.Equal(deficiency, d.Value);
        Assert.Equal(price, p.Value);
    }
}
