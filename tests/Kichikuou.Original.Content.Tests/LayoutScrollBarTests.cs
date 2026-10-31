using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="LayoutScrollBar"/>: the reference scroll bar on the PC's and the tablet's 1280×800 layout (round T's T2,
/// docs/ui/touch.md, "Round T: the reference scroll bar").
/// </summary>
public class LayoutScrollBarTests
{
    private const float Close = 0.01f;

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void The_save_lists_groove_stands_at_the_lists_right_and_its_thumb_inside_it()
    {
        var groove = LayoutScrollBar.Groove(382, 162, 594);                      // Save/Load's list, 52 + 330 wide
        Equal(new(374, 162, 8, 594), groove);
        Equal(new(375, 163, 6, 592 * 10f / 12), LayoutScrollBar.Thumb(groove, 10f / 12, 0));
        Equal(new(375, 163 + 592 * 2f / 12, 6, 592 * 10f / 12), LayoutScrollBar.Thumb(groove, 10f / 12, 1));
    }

    [Fact]
    public void A_long_lists_thumb_keeps_its_least_length()
    {
        var groove = LayoutScrollBar.Groove(1067, 116, 630);
        Equal(new(1060, 117 + 628 - 24, 6, 24), LayoutScrollBar.Thumb(groove, 0.01f, 1));
    }
}
