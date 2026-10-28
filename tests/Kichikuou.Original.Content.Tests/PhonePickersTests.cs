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
    public void The_harem_s_End_and_Summon_stand_at_the_left_thumb()
    {
        // lh2: foot = (FOOT - 10 - 140, FOOT - 10); End (LX0, …, LX0 + 320), Summon (LX0 + 336, …, LX0 + PIC_W2).
        Equal(new(116, 1020, 320, 140), PhonePickers.HaremEnd(Reference));
        Equal(new(452, 1020, 768, 140), PhonePickers.Summon(Reference));
    }

    [Fact]
    public void The_treasure_s_tabs_and_End_stand_at_the_foot_its_button_beside_the_details()
    {
        // lt2: foot = (FOOT - 10 - 130, FOOT - 10); the tabs (LX0, …, LX0 + PIC_W2 - 336) 12 apart, End (LX0 + PIC_W2 - 320, …);
        // Take Off (LX0 + PIC_W2 - 440, y + 10, LX0 + PIC_W2, y + 160) beside the details.
        Equal(new(116, 1030, 378, 130), PhonePickers.Tab(Reference, 0));
        Equal(new(506, 1030, 378, 130), PhonePickers.Tab(Reference, 1));
        Equal(new(900, 1030, 320, 130), PhonePickers.TreasureEnd(Reference));
        float details = PhonePickers.Details(Reference).Y;
        Equal(new(780, details + 10, 440, 150), PhonePickers.Action(Reference));
        Assert.Equal(750, PhonePickers.Column(Reference).Right - PhonePickers.ItemDetailsRight, Close);   // item_details(…, LX0 + PIC_W2 - 470)
    }

    [Fact]
    public void A_first_pick_s_prompt_and_Back_take_the_tabs_place_and_a_callout_stands_left_of_the_button()
    {
        Equal(new(116, 1030, 536, 130), PhonePickers.Prompt(Reference));
        Equal(new(664, 1030, 220, 130), PhonePickers.Back(Reference));
        Assert.Equal(PhonePickers.Tab(Reference, 1).Right, PhonePickers.Back(Reference).Right, Close);
        var action = PhonePickers.Action(Reference);
        Equal(new(116, 128, 624, action.Bottom - 128), PhonePickers.BesideAction(Reference));
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
