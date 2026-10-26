using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="LedgerTabs"/>: the unit ledger's tabs on both screens (docs/ui/unit-ledger.md, the owner's B1-B5).</summary>
public class LedgerTabsTests
{
    private static string[] Headers(LedgerTabColumns tab) => [.. tab.Columns.Select(c => c.Header)];

    [Fact]
    public void Deployment_TabsAreTheOwnersPresets_RowsInOverview()
    {
        Assert.Equal(["№", "Name", "Type", "Rows", "Troops", "Refill", "Cost"],
            Headers(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Overview)));
        Assert.Equal(["№", "Name", "T.Atk", "T.Def", "Strikes", "Mag", "Atk", "Def", "Atk", "Def", "Atk", "Def"],
            Headers(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Troops)));
        Assert.Equal(["№", "Name", "Strat%", "HP", "Lvl", "EXP", "C.Atk", "C.Def", "Item"],
            Headers(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Commander)));
    }

    [Fact]
    public void Subordinates_Overview_HasNoRows_AndKeepsTheScreensOwnFullNames()
    {
        var overview = LedgerTabs.Of(SubordinatesScreen.Columns, LedgerTab.Overview);
        Assert.Equal(["№", "Name", "Type", "Troops", "Refill", "Cost"], Headers(overview));
        Assert.Equal("Replenish's price (GOLD)", overview.Columns.Single(c => c.Column == DeploymentColumn.Refill).FullName);
        Assert.Equal("Replenish's price (GOLD): a click replenishes",
            LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Overview).Columns.Single(c => c.Column == DeploymentColumn.Refill).FullName);
    }

    [Fact]
    public void Troops_GroupsEachSituationsPair_FullNamesKept()
    {
        var troops = LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Troops);
        Assert.Equal([new LedgerGroup("Offense", 6, 7), new LedgerGroup("Defense", 8, 9), new LedgerGroup("Dungeon", 10, 11)], troops.Groups);
        Assert.Equal("Troop attack bonus in dungeons", troops.Columns[10].FullName);
        Assert.True(troops.Columns[10].IsModifier);
        Assert.Empty(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Overview).Groups);
        Assert.Empty(LedgerTabs.Of(SubordinatesScreen.Columns, LedgerTab.Commander).Groups);
    }

    [Fact]
    public void EveryColumnOfEitherScreen_IsInSomeTab()
    {
        foreach (var screen in new[] { DeploymentTable.Columns, SubordinatesScreen.Columns })
        {
            var shown = LedgerTabs.All.SelectMany(t => LedgerTabs.Of(screen, t).Columns.Select(c => c.Column)).ToHashSet();
            Assert.All(screen, c => Assert.Contains(c.Column, shown));
        }
    }

    [Theory]
    [InlineData("Overview", LedgerTab.Overview)]
    [InlineData("Troops", LedgerTab.Troops)]
    [InlineData("Commander", LedgerTab.Commander)]
    [InlineData("", LedgerTab.Overview)]
    [InlineData(null, LedgerTab.Overview)]
    [InlineData("General", LedgerTab.Overview)]
    [InlineData("7", LedgerTab.Overview)]
    public void Parse_ReadsASavedTab_ElseOverview(string? saved, LedgerTab tab) => Assert.Equal(tab, LedgerTabs.Parse(saved));
}
