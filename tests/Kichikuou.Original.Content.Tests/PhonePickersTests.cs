using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhonePickers"/>: our Harem and Treasure screens on the phone held landscape (docs/ui/phone-build.md, step L7),
/// at the mock-ups' figures on the reference phone (docs/ui/phone.md, §15: LH2 and LT2; rounds P39 and P40, §51-§56).
/// </summary>
public class PhonePickersTests
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
    public void The_column_takes_the_room_the_five_column_grid_leaves()
    {
        // The grid at least five cells of 210, 8 apart, and the scroll bar's 30; the column the rest of 2480 less the 40
        // between them; the picture at its width, the details under it.
        Equal(new(116, 0, 1328, 1170), PhonePickers.Column(Reference));
        Equal(new(116, 128, 1328, 1328 * 416f / 666), PhonePickers.Picture(Reference));
        var (x, y) = PhonePickers.Details(Reference);
        Assert.Equal(130, x, Close);
        Assert.Equal(128 + 1328 * 416f / 666 + 12, y, Close);
    }

    [Fact]
    public void The_harem_s_Summon_and_Close_stand_under_the_grid()
    {
        // H1 (round P40): Summon in Treasure's tabs' place, Close 320 at the right, both 130 high on the foot.
        Equal(new(2276, 1030, 320, 130), PhonePickers.End(Reference));
        Equal(new(1484, 1030, 776, 130), PhonePickers.Summon(Reference));
    }

    [Fact]
    public void The_treasure_s_tabs_stand_under_the_grid_its_button_on_their_foot()
    {
        // The tabs 12 apart ending 16 short of Close; Take Off at the column's right, its foot on theirs (the owner, P39).
        Equal(new(1484, 1030, 382, 130), PhonePickers.Tab(Reference, 0));
        Equal(new(1878, 1030, 382, 130), PhonePickers.Tab(Reference, 1));
        Equal(new(1004, 1010, 440, 150), PhonePickers.Action(Reference));
        Assert.Equal(PhonePickers.End(Reference).Bottom, PhonePickers.Action(Reference).Bottom, Close);
        Assert.Equal(974, PhonePickers.Column(Reference).Right - PhonePickers.ItemDetailsRight, Close);
    }

    [Fact]
    public void A_first_pick_s_prompt_and_Back_take_the_tabs_place_and_a_callout_stands_left_of_the_button()
    {
        Equal(new(1484, 1030, 544, 130), PhonePickers.Prompt(Reference));
        Equal(new(2040, 1030, 220, 130), PhonePickers.Back(Reference));
        Assert.Equal(PhonePickers.Tab(Reference, 1).Right, PhonePickers.Back(Reference).Right, Close);
        Equal(new(116, 128, 848, 1032), PhonePickers.BesideAction(Reference));
    }

    [Fact]
    public void The_grid_shows_seven_of_its_eleven_rows_five_cells_a_row()
    {
        // From 16 down to 20 over the buttons (994): seven rows of at least 122 fit, stretched to fill it; the cells 210
        // wide, the scroll bar's 30 at the right.
        var grid = PhonePickers.Grid(Reference);
        float pitch = 1000 / 7f;
        Assert.Equal(11, grid.Rows);
        Assert.Equal(7, grid.Shown);
        Assert.Equal(4, grid.MaxScroll);
        Equal(new(1484, 16, 1082, 994), grid.Area);
        Assert.Equal(30, grid.BarRoom, Close);
        Equal(new(1484, 16, 210, pitch - 6), grid.Cell(1, 0)!.Value);
        Equal(new(1484 + 218, 16 + pitch, 210, pitch - 6), grid.Cell(7, 0)!.Value);
        Assert.Null(grid.Cell(36, 0));                                                           // row 8
        Equal(new(1484, 16 + 6 * pitch, 210, pitch - 6), grid.Cell(51, 4)!.Value);               // the last row's one cell
        var (track, _) = grid.Bar(Reference, 0);
        Equal(new(2576, 16, 20, 994), track);
        Assert.Equal(track.Y + 0.5f * track.H / 11, grid.TickY(track, 0), Close);
    }

    [Fact]
    public void A_girl_is_found_at_her_number_s_cell_at_a_scroll()
    {
        // Royal Guards (13) in row 2; Saitou (50) in row 9, in view once scrolled by 4.
        var grid = PhonePickers.Grid(Reference);
        var royal = grid.Cell(13, 0)!.Value;
        Assert.Equal(13, grid.CellAt(0, royal.X + royal.W / 2, royal.Y + royal.H / 2));
        var saitou = grid.Cell(50, 4)!.Value;
        Assert.Equal(50, grid.CellAt(4, saitou.X + saitou.W / 2, saitou.Y + saitou.H / 2));
        Assert.Equal(0, grid.CellAt(0, 600, 400));                                               // the picture
        Assert.Equal((0, 3, 0, 41), grid.Hidden([1, 11, 12, 13, 41, 47, 50], 0));
        Assert.Equal((4, 0, 13, 0), grid.Hidden([1, 11, 12, 13, 41, 47, 50], 4));
    }

    [Theory]
    [InlineData(2712, 1220)]
    [InlineData(2400, 1080)]
    [InlineData(3200, 1440)]
    public void The_parts_keep_apart(float width, float height)
    {
        var f = PhoneFrame.Landscape(width, height);
        var grid = PhonePickers.Grid(f);
        Assert.True(PhonePickers.Column(f).Right < grid.Area.X);
        Assert.True(grid.Area.Bottom <= PhonePickers.End(f).Y);
        Assert.True(PhonePickers.Picture(f).Bottom < PhonePickers.Action(f).Y);
        Assert.Equal(PhonePickers.End(f).Right, grid.Area.Right + grid.BarRoom, Close);
        Assert.True(PhonePickers.Summon(f).Right < PhonePickers.End(f).X);
        Assert.True(grid.RowHeight >= f.Px(122) - Close);
    }

    [Fact]
    public void A_longer_screen_gives_the_grid_the_room_the_picture_cannot_use()
    {
        // 2900 wide: the picture as tall as the height over the details holds (1160 - 128 - 12 - 160), the grid the rest.
        var longer = PhoneFrame.Landscape(2900, 1220);
        Assert.Equal(860, PhonePickers.Picture(longer).H, Close);
        Assert.True(PhonePickers.Grid(longer).Area.W > PhonePickers.Grid(Reference).Area.W + 100);
    }
}
