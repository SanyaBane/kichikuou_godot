using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhonePickers"/>: our Harem and Treasure screens on the phone held landscape (docs/ui/phone-build.md, step L7),
/// at the mock-ups' figures on the reference phone (docs/ui/phone.md, §15: LH2 and LT2).
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
    public void The_column_holds_the_picture_at_the_left_and_the_grid_takes_the_rest()
    {
        // lh2: the column from LX0 = SIDE + 6 = 116, PIC_W2 = 1104 wide; the picture 4 under column_title's 124, the PC's
        // 666 × 416 (the mock's dump crop 663 × 412: 686 high); the grid (LX0 + PIC_W2 + 40, 16, LX1, FOOT - 10).
        Equal(new(116, 0, 1104, 1170), PhonePickers.Column(Reference));
        Equal(new(116, 128, 1104, 1104 * 416f / 666), PhonePickers.Picture(Reference));
        var (x, y) = PhonePickers.Details(Reference);
        Assert.Equal(130, x, Close);                                        // girl_details(d, LX0 + 14, …)
        Assert.Equal(128 + 1104 * 416f / 666 + 12, y, Close);
        Equal(new(1260, 16, 1336, 1144), PhonePickers.Grid(Reference));
    }

    [Fact]
    public void The_harem_s_Summon_and_End_stand_at_the_left_thumb()
    {
        // lh2's foot = (FOOT - 10 - 140, FOOT - 10), its two buttons swapped (owner, 2026-10-30): Summon (LX0, …,
        // LX0 + PIC_W2 - 336), End (LX0 + PIC_W2 - 320, …, LX0 + PIC_W2).
        Equal(new(900, 1020, 320, 140), PhonePickers.HaremEnd(Reference));
        Equal(new(116, 1020, 768, 140), PhonePickers.Summon(Reference));
    }

    [Fact]
    public void Treasure_s_column_takes_the_room_its_five_column_grid_leaves()
    {
        // Round P39 (docs/ui/phone.md, §53-§54): the grid at least five cells of 210, 8 apart, and the scroll bar's 30;
        // the column the rest of 2480 less the 40 between them; the picture at its width, the details under it.
        Equal(new(116, 0, 1328, 1170), PhonePickers.TreasureColumn(Reference));
        Equal(new(116, 128, 1328, 1328 * 416f / 666), PhonePickers.TreasurePicture(Reference));
        var (x, y) = PhonePickers.TreasureDetails(Reference);
        Assert.Equal(130, x, Close);
        Assert.Equal(128 + 1328 * 416f / 666 + 12, y, Close);
    }

    [Fact]
    public void Treasure_s_tabs_and_End_stand_under_the_grid_its_button_on_their_foot()
    {
        // The buttons under the grid (1484-2596), End 320 at the right, the tabs 12 apart ending 16 short of it; Take Off at
        // the column's right, its foot on theirs (the owner, P39).
        Equal(new(1484, 1030, 382, 130), PhonePickers.Tab(Reference, 0));
        Equal(new(1878, 1030, 382, 130), PhonePickers.Tab(Reference, 1));
        Equal(new(2276, 1030, 320, 130), PhonePickers.TreasureEnd(Reference));
        Equal(new(1004, 1010, 440, 150), PhonePickers.Action(Reference));
        Assert.Equal(PhonePickers.TreasureEnd(Reference).Bottom, PhonePickers.Action(Reference).Bottom, Close);
        Assert.Equal(974, PhonePickers.TreasureColumn(Reference).Right - PhonePickers.ItemDetailsRight, Close);
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
    public void Treasure_s_grid_shows_seven_of_its_eleven_rows_five_cells_a_row()
    {
        // From 16 down to 20 over the buttons (994): seven rows of at least 122 fit, stretched to fill it; the cells 210
        // wide, the scroll bar's 30 at the right.
        var grid = PhonePickers.TreasureGrid(Reference);
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

    [Theory]
    [InlineData(2712, 1220)]
    [InlineData(2400, 1080)]
    [InlineData(3200, 1440)]
    public void Treasure_s_parts_keep_apart(float width, float height)
    {
        var f = PhoneFrame.Landscape(width, height);
        var grid = PhonePickers.TreasureGrid(f);
        var column = PhonePickers.TreasureColumn(f);
        Assert.True(column.Right < grid.Area.X);
        Assert.True(grid.Area.Bottom <= PhonePickers.TreasureEnd(f).Y);
        Assert.True(PhonePickers.TreasurePicture(f).Bottom < PhonePickers.Action(f).Y);
        Assert.Equal(PhonePickers.TreasureEnd(f).Right, grid.Area.Right + grid.BarRoom, Close);
        Assert.True(grid.RowHeight >= f.Px(122) - Close);
    }

    [Fact]
    public void A_longer_screen_gives_treasure_s_grid_the_room_its_picture_cannot_use()
    {
        // 2900 wide: the picture as tall as the height over the details holds (1160 - 128 - 12 - 160), the grid the rest.
        var longer = PhoneFrame.Landscape(2900, 1220);
        Assert.Equal(860, PhonePickers.TreasurePicture(longer).H, Close);
        Assert.True(PhonePickers.TreasureGrid(longer).Area.W > PhonePickers.TreasureGrid(Reference).Area.W + 100);
    }

    [Fact]
    public void The_grid_is_folded_after_its_ninth_row_every_cell_at_its_number_s_place()
    {
        // folded_grid: bw = (1336 - 40) / 2 = 648, w = (648 - 16) / 3, the rows (1144 + 6) / 9 apart less 6 (the mock's
        // integer 121); rows 1-9 at x0, 10-17 at x0 + bw + 40.
        float w = (648 - 16) / 3f, pitch = 1150 / 9f;
        Equal(new(1260, 16, w, pitch - 6), PhonePickers.Cell(Reference, 1));
        Equal(new(1260 + 2 * (w + 8), 16, w, pitch - 6), PhonePickers.Cell(Reference, 3));
        Equal(new(1260 + 2 * (w + 8), 16 + 8 * pitch, w, pitch - 6), PhonePickers.Cell(Reference, 27));      // row 9, the left half's last
        Equal(new(1948, 16, w, pitch - 6), PhonePickers.Cell(Reference, 28));                                 // row 10 heads the right half
        var last = PhonePickers.Cell(Reference, 51);
        Assert.Equal(1948 + 2 * (w + 8), last.X, Close);
        Assert.Equal(16 + 7 * pitch, last.Y, Close);
        Assert.Equal(2596, last.Right, Close);
        Assert.Equal(1160, PhonePickers.Cell(Reference, 25).Bottom, Close);                                  // nine rows fill the height
        Assert.Equal((1928f, 28f, 1148f), PhonePickers.Rule(Reference));
    }

    [Fact]
    public void Anasel_is_found_at_her_number_s_cell_and_the_rule_s_gap_is_no_cell()
    {
        // HAREM_CELLS: Anasel at (3, 2), the twelfth cell.
        var anasel = PhonePickers.Cell(Reference, 12);
        Assert.Equal(12, PhonePickers.CellAt(Reference, anasel.X + anasel.W / 2, anasel.Y + anasel.H / 2));
        var (x, top, _) = PhonePickers.Rule(Reference);
        Assert.Equal(0, PhonePickers.CellAt(Reference, x, top + 100));
        Assert.Equal(0, PhonePickers.CellAt(Reference, 600, 400));                                           // the picture
    }

    [Theory]
    [InlineData(2712, 1220)]
    [InlineData(2400, 1080)]
    [InlineData(3200, 1440)]
    public void No_cell_overlaps_another_or_leaves_the_grid(float width, float height)
    {
        var f = PhoneFrame.Landscape(width, height);
        var grid = PhonePickers.Grid(f);
        var cells = Enumerable.Range(1, 51).Select(n => PhonePickers.Cell(f, n)).ToList();
        foreach (var c in cells)
        {
            Assert.True(c.X >= grid.X - Close && c.Right <= grid.Right + Close && c.Y >= grid.Y - Close && c.Bottom <= grid.Bottom + Close);
            Assert.True(c.Right <= PhonePickers.Rule(f).X || c.X >= PhonePickers.Rule(f).X);
        }
        for (int i = 0; i < cells.Count; i++)
            for (int j = i + 1; j < cells.Count; j++)
                Assert.False(cells[i].X < cells[j].Right - Close && cells[j].X < cells[i].Right - Close
                    && cells[i].Y < cells[j].Bottom - Close && cells[j].Y < cells[i].Bottom - Close, $"cells {i + 1} and {j + 1}");
        Assert.True(PhonePickers.Summon(f).Right <= grid.X);
    }

    [Fact]
    public void A_longer_screen_widens_the_grid_and_keeps_the_column()
    {
        var longer = PhoneFrame.Landscape(2900, 1220);
        Equal(PhonePickers.Column(Reference), PhonePickers.Column(longer));
        Equal(PhonePickers.Picture(Reference), PhonePickers.Picture(longer));
        Assert.Equal(1336 + 188, PhonePickers.Grid(longer).W, Close);
    }
}
