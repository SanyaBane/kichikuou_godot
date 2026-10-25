using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The touch mode's Skip and Log buttons' places (docs/ui/touch.md, "The dialogue buttons, round 2").</summary>
public class SkipButtonsTests
{
    // The panels of MessageWindow: 111/112, 121/122, 113/123, 131.
    private static readonly (int, int, int, int) LeftPanel = (436, 4, 834, 254), RightPanel = (58, 512, 822, 254),
        NarrationPanel = (248, 536, 784, 258), CgBarPanel = (0, 588, 1280, 212);

    [Fact]
    public void TheCellsStandInTheDrawerSkipOverLog()
    {
        var (drawer, skip, log) = (SkipButtons.Drawer, SkipButtons.SkipCell, SkipButtons.LogCell);
        Assert.Equal((0, 0), (skip.X, log.X));
        Assert.Equal(skip.Y + skip.H, log.Y);
        Assert.True(drawer.Y < skip.Y && log.Y + log.H < drawer.Y + drawer.H && skip.W < drawer.W);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EveryWindowButOneLeavesTheDrawerFree(int which)
    {
        var panel = new[] { LeftPanel, RightPanel, NarrationPanel }[which];
        Assert.False(SkipButtons.Drawer.Overlaps(panel));
        Assert.Equal(1, SkipButtons.WindowScale(panel));
        Assert.Equal(1, SkipButtons.WindowScale(null));
    }

    [Fact]
    public void The131IsDrawnSmallerRightOfItsDock()
    {
        float k = SkipButtons.WindowScale(CgBarPanel);
        Assert.Equal(1220 / 1280f, k);
        var dock = SkipButtons.Dock(CgBarPanel);
        Assert.Equal(new SkipButtons.Box(0, 588, 56, 202), dock);
        float left = 1280 - 1280 * k;                       // the scaled window's left edge
        Assert.Equal(dock.X + dock.W + 4, left);
        // The cells stand on the dock, its foot on the screen.
        Assert.True(SkipButtons.SkipCell.Y > dock.Y && SkipButtons.LogCell.Y + SkipButtons.LogCell.H < dock.Y + dock.H);
        Assert.True(dock.Y + dock.H <= 800);
    }

    [Fact]
    public void TheBattlesSkipStandsUnderTheCards()
    {
        var skip = SkipButtons.Battle;
        Assert.True(skip.Y >= 758 && skip.Y + skip.H <= 800);
        Assert.True(skip.X >= SkipButtons.BattleLeftRailRight);         // clear of the left rail beside the cards
    }
}
