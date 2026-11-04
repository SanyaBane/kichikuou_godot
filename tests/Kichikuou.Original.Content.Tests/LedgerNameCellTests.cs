using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

public sealed class LedgerNameCellTests
{
    [Fact]
    public void Merge_TakesTheTypeAndTheRowsIntoTheNameCell()
    {
        var overview = LedgerNameCell.Merge(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Overview));
        Assert.Equal(["№", "Name · Type", "Troops", "Refill", "Cost", "Item"], overview.Columns.Select(c => c.Header));
        Assert.Empty(overview.Groups);
    }

    [Fact]
    public void Merge_KeepsTheTroopsTabsGroupsOverTheirColumns()
    {
        var shown = LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Troops);
        var merged = LedgerNameCell.Merge(shown);
        Assert.Equal(shown.Groups.Select(g => g.Name), merged.Groups.Select(g => g.Name));
        foreach (var group in merged.Groups)
            Assert.Equal("Atk", merged.Columns[group.First].Header);
    }

    [Fact]
    public void For_ScalesTheUprightRowToTheLedgersText()
    {
        var upright = LedgerNameCell.For(PhoneLedgerUpright.Text, deployment: true);
        Assert.Equal(PhoneLedgerUpright.DeploymentRows.Row, upright.Row, 3);
        Assert.Equal(PhoneLedgerUpright.DeploymentRows.Face, upright.Face, 3);
        var pc = LedgerNameCell.For(17, deployment: true);
        Assert.Equal(166 * 17 / 46f, pc.Row, 3);                                // ~61 px on the PC
        Assert.Equal(pc.HpTop + pc.BarHeight, pc.FaceTop + pc.Face, 3);         // the face down to the HP bar's foot
    }

    [Fact]
    public void Subordinates_HaveNoRowsLine_TheFaceStillDownToTheHpBarsFoot()
    {
        var merged = LedgerNameCell.Merge(LedgerTabs.Of(SubordinatesScreen.Columns, LedgerTab.Overview));
        Assert.Equal(["№", "Name · Type", "Troops", "Refill", "Cost", "Item"], merged.Columns.Select(c => c.Header));
        var pc = LedgerNameCell.For(17, deployment: false);
        Assert.Equal(0, pc.RowsMiddle);
        Assert.Equal(pc.HpTop + pc.BarHeight, pc.FaceTop + pc.Face, 3);
        Assert.True(pc.Row < LedgerNameCell.For(17, deployment: true).Row);     // a line shorter
    }
}
