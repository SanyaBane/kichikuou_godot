using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The touch mode's Skip and Log buttons' places (docs/ui/touch.md, "The Skip and Log buttons").</summary>
public class SkipButtonsTests
{
    // The panels of MessageWindow: 111/112, 121/122, 113/123, 131.
    private static readonly (int, int, int, int) LeftPanel = (436, 4, 834, 254), RightPanel = (58, 512, 822, 254),
        NarrationPanel = (248, 536, 784, 258), CgBarPanel = (0, 588, 1280, 212);

    [Fact]
    public void WithoutAWindowThePairIsSideBySideAtTheBottomLeft()
    {
        var (skip, log) = SkipButtons.Scene(null);
        Assert.Equal(new SkipButtons.Box(16, 700, 190, 84), skip);
        Assert.Equal(new SkipButtons.Box(220, 700, 150, 84), log);
    }

    [Fact]
    public void ALeftSpeechLeavesTheCornerFree()
    {
        Assert.Equal(SkipButtons.Scene(null), SkipButtons.Scene(LeftPanel));
    }

    [Fact]
    public void BesideTheNarrationLogStandsAboveSkip()
    {
        var (skip, log) = SkipButtons.Scene(NarrationPanel);
        Assert.Equal(new SkipButtons.Box(16, 700, 190, 84), skip);
        Assert.Equal(new SkipButtons.Box(16, 602, 150, 84), log);
        Assert.False(skip.Overlaps(NarrationPanel) || log.Overlaps(NarrationPanel));
    }

    [Theory]
    [InlineData(588)]
    [InlineData(512)]
    public void AWindowOverTheCornerHasThePairAboveIt(int top)
    {
        var panel = top == 588 ? CgBarPanel : RightPanel;
        var (skip, log) = SkipButtons.Scene(panel);
        Assert.Equal(top - 40 - 84, skip.Y);
        Assert.Equal(skip.Y, log.Y);
        Assert.Equal((16, 220), (skip.X, log.X));
        Assert.False(skip.Overlaps(panel) || log.Overlaps(panel));
    }

    [Fact]
    public void TheBattlesSkipStandsUnderTheCards()
    {
        var skip = SkipButtons.Battle;
        Assert.True(skip.Y >= 758 && skip.Y + skip.H <= 800);
    }
}
