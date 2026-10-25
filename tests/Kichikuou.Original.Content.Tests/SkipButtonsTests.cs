using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The touch mode's Skip and Log buttons' places (docs/ui/touch.md, "The dialogue buttons, round 2").</summary>
public class SkipButtonsTests
{
    // The panels of MessageWindow: 111/112, 121/122, 113/123, 131.
    private static readonly (int, int, int, int) LeftPanel = (436, 4, 834, 254), RightPanel = (58, 512, 822, 254),
        NarrationPanel = (248, 536, 784, 258), CgBarPanel = (0, 588, 1280, 212);

    [Fact]
    public void TheCellsStandOnTwoPlatesSkipOverLog()
    {
        var (skip, log) = (SkipButtons.SkipCell, SkipButtons.LogCell);
        var (skipPlate, logPlate, drawer) = (SkipButtons.SkipPlate, SkipButtons.LogPlate, SkipButtons.Drawer);
        Assert.Equal((0, 0), (skip.X, log.X));
        Assert.Equal(new SkipButtons.Box(0, 592, 50, 84), skip);        // Skip where it was before the plates parted
        Assert.True(Inside(skip, skipPlate) && Inside(log, logPlate));
        Assert.Equal(16, logPlate.Y - (skipPlate.Y + skipPlate.H));     // the owner's G3
        Assert.True(Inside(skipPlate, drawer) && Inside(logPlate, drawer));

        static bool Inside(SkipButtons.Box a, SkipButtons.Box b) =>
            a.X >= b.X && a.Y >= b.Y && a.X + a.W <= b.X + b.W && a.Y + a.H <= b.Y + b.H;
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
    public void The131IsDrawnSmallerRightOfThePlates()
    {
        float k = SkipButtons.WindowScale(CgBarPanel);
        Assert.Equal(1220 / 1280f, k);
        float left = 1280 - 1280 * k;                       // the scaled window's left edge
        var drawer = SkipButtons.Drawer;
        Assert.True(drawer.X + drawer.W < left);
        // The plates stand on the band left of the window drawn smaller, on the screen.
        Assert.True(drawer.Y >= CgBarPanel.Item2 && drawer.Y + drawer.H <= 800);
    }

    [Fact]
    public void TheBattlesSkipStandsUnderTheCards()
    {
        var skip = SkipButtons.Battle;
        Assert.True(skip.Y >= 758 && skip.Y + skip.H <= 800);
        Assert.True(skip.X >= SkipButtons.BattleLeftRailRight);         // clear of the left rail beside the cards
    }
}
