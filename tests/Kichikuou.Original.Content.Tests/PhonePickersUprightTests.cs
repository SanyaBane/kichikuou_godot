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
    public void The_harem_s_grid_shows_twelve_rows_of_its_seventeen()
    {
        // ph2: the grid 190 under the details' top to BAR[0] - 20, rows of 102 (128 till 2026-11-04), 6 apart; three cells a row, 8 apart; the
        // scroll bar's 20 and its gap of 10 off the grid's right (the owner's S2, docs/ui/phone.md §41).
        var grid = PhonePickersUpright.HaremGrid(Reference);
        float top = 232 + 1188 * 416f / 666 + 14 + 190;
        Equal(new(16, top, 1158, 2484 - top), grid.Area);
        Equal(new(16, top, 1188, 2484 - top), grid.Scrolled);
        Assert.Equal(12, grid.Shown);
        Assert.Equal(5, grid.MaxScroll);
        float w = (1158 - 16) / 3f;
        Equal(new(16, top, w, 102), grid.Cell(1, 0));
        Equal(new(16 + 2 * (w + 8), top + 11 * 108, w, 102), grid.Cell(36, 0));
        Assert.Null(grid.Cell(37, 0));
        Equal(new(16, top + 11 * 108, w, 102), grid.Cell(37, 1));
        Assert.Null(grid.Cell(1, 1));
        Assert.Equal(1, grid.CellAt(0, 20, top + 2));
        Assert.Equal(4, grid.CellAt(1, 20, top + 2));
        Assert.Equal(0, grid.CellAt(0, 16 + w + 4, top + 2));                          // between two cells
    }

    [Fact]
    public void A_cell_out_of_view_is_revealed_by_the_least_scroll()
    {
        var grid = PhonePickersUpright.HaremGrid(Reference);
        Assert.Equal(2, grid.Reveal(40, 0));                                            // row 14 the last shown
        Assert.Equal(0, grid.Reveal(2, 5));
        Assert.Equal(3, grid.Reveal(30, 3));                                            // row 9 already shown
        Assert.Equal(5, grid.Reveal(51, 20));
    }

    [Fact]
    public void The_scroll_bar_stands_right_of_the_grid_as_tall_as_its_rows()
    {
        // mock_scroll_bar.py, S2: the groove 20 wide, its right at the picture's (1204), 10 right of the grid, as tall as the
        // twelve rows in view; the thumb 3 in, as long as 12 of the 17 rows.
        var grid = PhonePickersUpright.HaremGrid(Reference);
        var (track, top) = PhonePickersUpright.ScrollBar(Reference, grid, 0);
        Equal(new(1184, grid.Area.Y, 20, 12 * 108 - 6), track);
        Assert.Equal(10, track.X - grid.Area.Right, Close);
        Equal(new(1187, track.Y + 3, 14, (track.H - 6) * 12 / 17), top);
        var (_, bottom) = PhonePickersUpright.ScrollBar(Reference, grid, 5);
        Assert.Equal(track.Bottom - 3, bottom.Bottom, Close);
    }

    [Fact]
    public void The_hint_counts_the_filled_cells_out_of_view_and_finds_the_nearest()
    {
        // Twelve rows in view of 17: at scroll 0 rows 12-16 (cells 37-51) are below; at scroll 4 rows 0-3 (cells 1-12) above.
        var grid = PhonePickersUpright.HaremGrid(Reference);
        int[] filled = [13, 1, 10, 11, 41, 47, 50];
        Assert.Equal((0, 3, 0, 41), grid.Hidden(filled, 0));
        Assert.Equal((3, 1, 11, 50), grid.Hidden(filled, 4));
        Assert.Equal((0, 0, 0, 0), grid.Hidden([], 2));
        Assert.Equal("3 more below", PhonePickersUpright.MoreText(3, above: false));
    }

    [Fact]
    public void The_hint_s_pill_stands_centred_inside_the_rows_edge()
    {
        // mock_more_cells.py: 68 high, 8 in from the rows' top or foot, as wide as its text and 2 × 36 + 36 of room.
        var grid = PhonePickersUpright.HaremGrid(Reference);
        var top = PhonePickersUpright.MorePill(Reference, grid, above: true, 200);
        Equal(new(16 + (1158 - 308) / 2f, grid.Area.Y + 8, 308, 68), top);
        var foot = PhonePickersUpright.MorePill(Reference, grid, above: false, 200);
        Assert.Equal(grid.Area.Y + 12 * 108 - 6 - 8, foot.Bottom, Close);
        var (track, _) = PhonePickersUpright.ScrollBar(Reference, grid, 0);
        Assert.Equal(track.Y + 0.5f * track.H / 17, PhonePickersUpright.TickY(track, 0), Close);
    }

    [Fact]
    public void A_grid_with_every_row_in_view_keeps_the_room_s_width()
    {
        var grid = PhonePickersUpright.HaremGrid(PhoneFrame.Upright(1220, 5000));
        Assert.Equal(0, grid.MaxScroll);
        Assert.Equal(1188, grid.Area.W, Close);
        Assert.Equal(grid.Area, grid.Scrolled);
    }

    [Fact]
    public void The_treasure_s_tabs_and_grid_stand_under_its_details()
    {
        // pt2: the tabs 180 under the details' top, (16, y, PW - 16, y + 110) halved 12 apart; the grid 14 under them to
        // BAR[0] - 20 in rows of 94 (118 till 2026-11-04): eleven of 17 rows in view.
        float details = 232 + 1188 * 416f / 666 + 14, tabs = details + 180;
        Equal(new(16, tabs, 588, 110), PhonePickersUpright.Tab(Reference, 0));
        Equal(new(616, tabs, 588, 110), PhonePickersUpright.Tab(Reference, 1));
        Equal(new(16, tabs, 956, 110), PhonePickersUpright.Prompt(Reference));
        Equal(new(984, tabs, 220, 110), PhonePickersUpright.Back(Reference));
        var grid = PhonePickersUpright.TreasureGrid(Reference);
        Equal(new(16, tabs + 124, 1158, 2484 - tabs - 124), grid.Area);
        Assert.Equal(100, grid.Pitch, Close);
        Assert.Equal(11, grid.Shown);
        Equal(new(16, 232, 1188, 2504 - 40 - 232), PhonePickersUpright.BesideAction(Reference));
    }

    [Fact]
    public void The_red_button_and_End_share_the_bar()
    {
        // end_and, its two buttons swapped (owner, 2026-10-30): the red button (16, BAR[0], 16 + 760, BAR[1]), End
        // (16 + 760 + 16, BAR[0], PW - 16, BAR[1]).
        Equal(new(792, 2504, 412, 148), PhonePickersUpright.End(Reference));
        Equal(new(16, 2504, 760, 148), PhonePickersUpright.Action(Reference));
    }

    [Fact]
    public void A_longer_screen_shows_more_rows()
    {
        var longer = PhoneFrame.Upright(1220, 2712 * 1.1f);
        Assert.True(PhonePickersUpright.HaremGrid(longer).Shown > PhonePickersUpright.HaremGrid(Reference).Shown);
        var poco = PhoneFrame.Upright(1220, 2712, new PhoneFrame.Insets(0, 120, 0, 0));
        Assert.Equal(124, PhonePickersUpright.Band(poco).Y, Close);
        Assert.Equal(12, PhonePickersUpright.HaremGrid(poco).Shown);
    }
}
