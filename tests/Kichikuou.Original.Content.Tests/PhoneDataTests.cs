using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneData"/>: All Data on the phone held landscape (docs/ui/phone-build.md, step L9), at the mock-ups' figures
/// on the reference phone (docs/ui/phone.md, §23: LG2m, LG2).
/// </summary>
public class PhoneDataTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Landscape(2712, 1220);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void The_graphs_stand_in_the_left_side_at_the_thumb()
    {
        // side_graphs: RAIL_L = (116, 370); h 132, gap 14, the last 10 over FOOT (1170); GRAPHS at (x0 + 4, y - 32).
        Equal(new(116, 590, 254, 132), PhoneData.Graph(Reference, 0));
        Equal(new(116, 1028, 254, 132), PhoneData.Graph(Reference, 3));
        Assert.Equal((120f, 558f), PhoneData.GraphsHeading(Reference));
    }

    [Fact]
    public void Back_and_the_chips_stand_in_the_right_side()
    {
        // lg2: RAIL_R = (2342, 2596); Back (x0, FOOT - 10 - 150, x1, FOOT - 10); the chips 30 over it, four rows of two 12
        // apart, 112 high; SCALE at (x0 + 4, y - 36).
        Equal(new(2342, 1010, 254, 150), PhoneData.Back(Reference));
        Equal(new(2342, 496, 121, 112), PhoneData.Chip(Reference, 0));
        Equal(new(2475, 744, 121, 112), PhoneData.Chip(Reference, 5));
        Equal(new(2475, 868, 121, 112), PhoneData.Chip(Reference, 7));
        Assert.Equal(PhoneData.Back(Reference).Y - 30, PhoneData.Chip(Reference, 7).Bottom, Close);
        Assert.Equal((2346f, 460f), PhoneData.ScaleHeading(Reference));
    }

    [Fact]
    public void A_figure_stands_centred_over_its_bar()
    {
        // funds_relabelled: the bar's middle, the baseline 5 over the grey just over its frame line, at the frame's 1.525.
        var (x, baseline) = PhoneData.Figure(Reference, 72, 250);
        Assert.Equal(380 + 160 * 1.525f, x, Close);
        Assert.Equal((2 * 249 - 6) * 1.525f, baseline, Close);
        Assert.Equal(380 + (160 + 15 * 64) * 1.525f, PhoneData.Figure(Reference, 72 + 15 * 32, 250).X, Close);
    }

    [Fact]
    public void A_longer_screen_keeps_everything_in_the_sides()
    {
        var f = PhoneFrame.Landscape(3000, 1220);
        for (int i = 0; i < PhoneData.Graphs; i++)
        {
            var cell = PhoneData.Graph(f, i);
            Assert.True(cell.X >= f.LeftRoom.X && cell.Right <= f.Frame.X);
        }
        for (int i = 0; i < PhoneData.Scales; i++)
        {
            var chip = PhoneData.Chip(f, i);
            Assert.True(chip.X >= f.Frame.Right && chip.Right <= f.RightRoom.Right && chip.Bottom <= PhoneData.Back(f).Y);
        }
        Assert.True(PhoneData.Back(f).Bottom <= f.Screen.H - f.Safe.Bottom);
        Assert.True(PhoneData.GraphsHeading(f).Middle > PhoneScenes.MenuCell(f).Bottom);
    }
}
