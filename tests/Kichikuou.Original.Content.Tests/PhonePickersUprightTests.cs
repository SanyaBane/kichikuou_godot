using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhonePickersUpright"/>: our Harem and Treasure screens on the phone held upright (docs/ui/phone-build.md, step
/// P7), at the mock-ups' figures on the reference phone (docs/ui/phone.md, §9: PH2 and PT2).
/// </summary>
public class PhonePickersUprightTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Upright(1220, 2712);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.X, actual.Value.X, Close);
        Assert.Equal(expected.Y, actual.Value.Y, Close);
        Assert.Equal(expected.W, actual.Value.W, Close);
        Assert.Equal(expected.H, actual.Value.H, Close);
    }

    [Fact]
    public void The_band_the_picture_and_the_details_stand_under_the_hole()
    {
        // plain_title at TOP + 4, 112 high; ph2: the picture at (16, y + 6) the room's width less 32, here the PC's picture
        // panel's shape (666 × 416; the mock's dump crop was 663 × 412), the details 14 under it at x 30.
        Equal(new(0, 114, 1220, 112), PhonePickersUpright.Band(Reference));
        float h = 1188 * 416f / 666;
        Equal(new(16, 232, 1188, h), PhonePickersUpright.Picture(Reference));
        var (x, y) = PhonePickersUpright.Details(Reference);
        Assert.Equal(30, x, Close);
        Assert.Equal(232 + h + 14, y, Close);
        Assert.Equal(1190, PhonePickersUpright.DetailsRight(Reference), Close);
    }

    [Fact]
    public void The_harem_s_grid_shows_nine_rows_of_its_seventeen()
    {
        // ph2: the grid 190 under the details' top to BAR[0] - 20, rows of 128, 6 apart; three cells a row, 8 apart.
        var grid = PhonePickersUpright.HaremGrid(Reference);
        float top = 232 + 1188 * 416f / 666 + 14 + 190;
        Equal(new(16, top, 1188, 2484 - top), grid.Area);
        Assert.Equal(9, grid.Shown);
        Assert.Equal(8, grid.MaxScroll);
        float w = (1188 - 16) / 3f;
        Equal(new(16, top, w, 128), grid.Cell(1, 0));
        Equal(new(16 + 2 * (w + 8), top + 8 * 134, w, 128), grid.Cell(27, 0));
        Assert.Null(grid.Cell(28, 0));
        Equal(new(16, top + 8 * 134, w, 128), grid.Cell(28, 1));
        Assert.Null(grid.Cell(1, 1));
        Assert.Equal(1, grid.CellAt(0, 20, top + 2));
        Assert.Equal(4, grid.CellAt(1, 20, top + 2));
        Assert.Equal(0, grid.CellAt(0, 16 + w + 4, top + 2));                          // between two cells
    }

    [Fact]
    public void A_cell_out_of_view_is_revealed_by_the_least_scroll()
    {
        var grid = PhonePickersUpright.HaremGrid(Reference);
        Assert.Equal(5, grid.Reveal(40, 0));                                            // row 13 the last shown
        Assert.Equal(0, grid.Reveal(2, 5));
        Assert.Equal(3, grid.Reveal(30, 3));                                            // row 9 already shown
        Assert.Equal(8, grid.Reveal(51, 20));
    }

    [Fact]
    public void The_scroll_bar_stands_in_the_room_s_right_margin()
    {
        var grid = PhonePickersUpright.HaremGrid(Reference);
        var (track, top) = PhonePickersUpright.ScrollBar(Reference, grid, 0);
        Equal(new(1209, grid.Area.Y, 6, grid.Area.H), track);
        Assert.Equal(grid.Area.H * 9 / 17, top.H, Close);
        Assert.Equal(track.Y, top.Y, Close);
        var (_, bottom) = PhonePickersUpright.ScrollBar(Reference, grid, 8);
        Assert.Equal(track.Bottom, bottom.Bottom, Close);
        Assert.True(track.X >= grid.Area.Right);
    }

    [Fact]
    public void The_treasure_s_tabs_and_grid_stand_under_its_details()
    {
        // pt2: the tabs 180 under the details' top, (16, y, PW - 16, y + 110) halved 12 apart; the grid 14 under them to
        // BAR[0] - 20 in rows of 118: nine of 17 rows in view.
        float details = 232 + 1188 * 416f / 666 + 14, tabs = details + 180;
        Equal(new(16, tabs, 588, 110), PhonePickersUpright.Tab(Reference, 0));
        Equal(new(616, tabs, 588, 110), PhonePickersUpright.Tab(Reference, 1));
        Equal(new(16, tabs, 956, 110), PhonePickersUpright.Prompt(Reference));
        Equal(new(984, tabs, 220, 110), PhonePickersUpright.Back(Reference));
        var grid = PhonePickersUpright.TreasureGrid(Reference);
        Equal(new(16, tabs + 124, 1188, 2484 - tabs - 124), grid.Area);
        Assert.Equal(124, grid.Pitch, Close);
        Assert.Equal(9, grid.Shown);
        Equal(new(16, 232, 1188, 2504 - 40 - 232), PhonePickersUpright.BesideAction(Reference));
    }

    [Fact]
    public void End_and_the_red_button_share_the_bar()
    {
        // end_and: End (16, BAR[0], PW - 32 - 760, BAR[1]), the red button (PW - 16 - 760, BAR[0], PW - 16, BAR[1]).
        Equal(new(16, 2504, 412, 148), PhonePickersUpright.End(Reference));
        Equal(new(444, 2504, 760, 148), PhonePickersUpright.Action(Reference));
    }

    [Fact]
    public void A_longer_screen_shows_more_rows()
    {
        var longer = PhoneFrame.Upright(1220, 2712 * 1.1f);
        Assert.True(PhonePickersUpright.HaremGrid(longer).Shown > PhonePickersUpright.HaremGrid(Reference).Shown);
        var poco = PhoneFrame.Upright(1220, 2712, new PhoneFrame.Insets(0, 120, 0, 0));
        Assert.Equal(124, PhonePickersUpright.Band(poco).Y, Close);
        Assert.Equal(9, PhonePickersUpright.HaremGrid(poco).Shown);
    }
}
