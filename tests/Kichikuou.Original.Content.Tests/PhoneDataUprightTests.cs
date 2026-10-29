using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneDataUpright"/>: All Data on the phone held upright (docs/ui/phone-build.md, step P8c), at the mock-ups'
/// figures on the reference phone (docs/ui/phone.md, §22: PG2m and PG2).
/// </summary>
public class PhoneDataUprightTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Upright(1220, 2712);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void The_miniature_stands_under_the_title_band_at_the_room_s_width_and_the_sheet_under_it()
    {
        // pg2_top: the miniature 12 under plain_title's 226, 16 in, in its shape (1280 × 532); the sheet 20 under it to 24 over the bar.
        Equal(new(16, 238, 1188, 1188 * 532f / 1280), PhoneDataUpright.Miniature(Reference));
        Equal(new(16, 751.7625f, 1188, 2480 - 751.7625f), PhoneDataUpright.Sheet(Reference));
    }

    [Fact]
    public void The_menu_s_four_graphs_are_finger_rows_in_the_sheet_s_middle()
    {
        // pg2m: rows 150 high, 18 apart, 48 in, the four centred in the sheet.
        float top = (751.7625f + 2480) / 2 - 327;
        Equal(new(48, top, 1124, 150), PhoneDataUpright.MenuRow(Reference, 0));
        Equal(new(48, top + 3 * 168, 1124, 150), PhoneDataUpright.MenuRow(Reference, 3));
    }

    [Fact]
    public void A_graph_has_its_tabs_on_top_the_plot_between_them_and_the_chips_in_a_row_at_the_thumb()
    {
        // graph_tabs 26 under the sheet's top, 124 high, 12 apart from 40 to PW − 40; the title 52 under them.
        Equal(new(40, 777.7625f, 276, 124), PhoneDataUpright.Tab(Reference, 0));
        Equal(new(904, 777.7625f, 276, 124), PhoneDataUpright.Tab(Reference, 3));
        Assert.Equal(953.7625f, PhoneDataUpright.TitleMiddle(Reference), Close);
        // scale_chips: the eight in one row, 120 high, 64 over the bar; SCALE 30 over them.
        Equal(new(40, 2320, 132, 120), PhoneDataUpright.Chip(Reference, 0));
        Equal(new(1048, 2320, 132, 120), PhoneDataUpright.Chip(Reference, 7));
        Assert.Equal((44f, 2290f), PhoneDataUpright.ScaleHeading(Reference));
        // The plot 150 under the tabs to 130 over the chips (64.6 mm high), the caption 44 under it.
        Equal(new(40, 1051.7625f, 1140, 2190 - 1051.7625f), PhoneDataUpright.Plot(Reference));
        Assert.Equal((44f, 2234f), PhoneDataUpright.Caption(Reference));
        // end_alone: Back at the bar's right, 420 wide.
        Equal(new(784, 2504, 420, 148), PhoneDataUpright.Back(Reference));
    }

    [Fact]
    public void The_bars_stand_in_sixteen_slots_their_heights_a_share_of_the_plot()
    {
        // funds_plot: slot 1140 / 16, the bar 0.62 of it centred; the tallest (300) reaches 4 under the plot's top.
        var (x0, w) = PhoneDataUpright.Column(Reference, 72);
        Assert.Equal(40 + 71.25f / 2 - 71.25f * 0.62f / 2, x0, Close);
        Assert.Equal(71.25f * 0.62f, w, Close);
        var (x15, _) = PhoneDataUpright.Column(Reference, 72 + 15 * 32);
        Assert.Equal(x0 + 15 * 71.25f, x15, Close);
        Assert.Equal(71.25f, PhoneDataUpright.Slot(Reference), Close);
        Assert.Equal(2186, PhoneDataUpright.Level(Reference, 0), Close);
        Assert.Equal(1051.7625f + 4, PhoneDataUpright.Level(Reference, 300), Close);
        // A figure centred on its bar, its baseline 10 over the bar's top.
        var (fx, baseline) = PhoneDataUpright.Figure(Reference, 72, 150);
        Assert.Equal(40 + 71.25f / 2, fx, Close);
        Assert.Equal(PhoneDataUpright.Level(Reference, 150) - 10, baseline, Close);
    }

    [Fact]
    public void A_longer_screen_gives_the_plot_more_height()
    {
        var longer = PhoneFrame.Upright(1220, 2800);
        Assert.Equal(PhoneDataUpright.Plot(Reference).H + 88, PhoneDataUpright.Plot(longer).H, Close);
        Assert.Equal(PhoneDataUpright.Tab(Reference, 0).Y, PhoneDataUpright.Tab(longer, 0).Y, Close);
    }
}
