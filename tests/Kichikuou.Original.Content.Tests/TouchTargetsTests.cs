using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The touch mode's preselected target (docs/ui/touch.md, "The tablet").</summary>
public class TouchTargetsTests
{
    private static readonly Func<int, bool> All = _ => true;

    [Fact]
    public void TheBattlesFirstTurnTakesTheFirstEnemyCard()
    {
        var targets = new TouchTargets();
        Assert.Equal(2, targets.Preselect(1, [0, 31, 32, 0], All));
    }

    [Fact]
    public void AUnitKeepsItsOwnEnemy()
    {
        var targets = new TouchTargets();
        targets.Attacked(unit: 12, enemy: 33, slot: 3);
        targets.Attacked(unit: 1, enemy: 31, slot: 1);
        Assert.Equal(3, targets.Preselect(12, [31, 32, 33, 34], All));
    }

    [Fact]
    public void TheEnemyIsFollowedWhenTheCardsCloseUp()
    {
        var targets = new TouchTargets();
        targets.Attacked(unit: 12, enemy: 33, slot: 3);
        // the first enemy fell: the third card is now the second
        Assert.Equal(2, targets.Preselect(12, [32, 33, 34, 0], All));
    }

    [Fact]
    public void AUnitThatHasNotAttackedTakesThePreviousUnitsTarget()
    {
        var targets = new TouchTargets();
        targets.Attacked(unit: 1, enemy: 32, slot: 2);
        Assert.Equal(2, targets.Preselect(6, [31, 32, 33, 0], All));
    }

    [Fact]
    public void AFallenEnemyGivesWayToThePreviousTargetThenTheFirstCard()
    {
        var targets = new TouchTargets();
        targets.Attacked(unit: 12, enemy: 33, slot: 3);
        targets.Attacked(unit: 1, enemy: 34, slot: 4);
        Assert.Equal(4, targets.Preselect(12, [31, 32, 0, 34], All));
        Assert.Equal(1, targets.Preselect(12, [31, 32, 0, 0], All));
    }

    [Fact]
    public void AnEnemyOutOfReachIsPassedOver()
    {
        var targets = new TouchTargets();
        targets.Attacked(unit: 12, enemy: 33, slot: 3);
        // a melee unit: only the front row (slots 1-2) is in reach
        Assert.Equal(2, targets.Preselect(1, [0, 32, 33, 0], n => n <= 2));
        // none in reach: the first card all the same
        Assert.Equal(3, targets.Preselect(1, [0, 0, 33, 34], _ => false));
    }

    [Fact]
    public void EnemiesOfOneRecordAreToldApartByTheirSlot()
    {
        var targets = new TouchTargets();
        targets.Attacked(unit: 1, enemy: 80, slot: 2);
        Assert.Equal(2, targets.Preselect(1, [80, 80, 0, 0], All));
        Assert.Equal(1, targets.Preselect(1, [80, 0, 0, 0], All));
    }

    [Fact]
    public void ANewBattleForgetsEveryTarget()
    {
        var targets = new TouchTargets();
        targets.Attacked(unit: 1, enemy: 33, slot: 3);
        targets.Clear();
        Assert.Equal(1, targets.Preselect(1, [31, 32, 33, 0], All));
    }

    [Fact]
    public void AnEmptySideHasNoTarget() =>
        Assert.Null(new TouchTargets().Preselect(1, [0, 0, 0, 0], All));
}
