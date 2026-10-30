namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our Harem and Treasure screens on the phone held upright (docs/ui/phone-build.md, step P7; docs/ui/phone.md, §9: PH2 and
/// PT2, the owner's choice): the title band under the hole (<see cref="Band"/>: the title, the date at the right, a
/// divider), the picture at the room's width in the PC's shape (<see cref="Picture"/>, its own words 2.4 mm), the details
/// under it (<see cref="Details"/>), then the pickers' 3 × 17 grid in taller cells than held landscape's, as many rows in
/// view as its room holds, the rest a drag away (<see cref="ScrolledGrid"/>: the harem's <see cref="HaremGrid"/>), a thin
/// scroll bar in the room's right margin; the red button | End in the bar (<see cref="Action"/>, <see cref="End"/>). The
/// figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>: <c>plain_title</c>, <c>ph2</c>, <c>girl_details</c>,
/// <c>picker_grid</c>, <c>harem_cell</c>, <c>end_and</c>) in the reference phone's pixels, from the room's top and the bar;
/// the boxes in the canvas's units. A longer screen gives the grid more rows in view. Interface only. Engine-independent.
/// </summary>
public static class PhonePickersUpright
{
    /// <summary>The picture's, the grid's and the bar's distance from the room's sides.</summary>
    private const float SideIn = 16;

    // ------------------------------------------------------------------ the title band (plain_title)

    /// <summary>The band: its top under the room's, its height (to the divider and 12 under it).</summary>
    private const float BandTop = 4, BandHeight = 112;

    /// <summary>The band's lines from its top and the room's sides: the title's left and type (the brush face), the date's right, top and type, the divider and its ends' distance from the sides.</summary>
    public const float TitleIn = 30, TitleText = 62, DateRight = 30, DateTop = 22, DateText = 32, Divider = 100, DividerIn = 24;

    /// <summary>The title band across the room under its top.</summary>
    public static PhoneFrame.Box Band(PhoneFrame f) => new(f.Room.X, f.Room.Y + f.Px(BandTop), f.Room.W, f.Px(BandHeight));

    // ------------------------------------------------------------------ the picture and the details (ph2, girl_details)

    /// <summary>The picture's top under the band; its shape (the PC's picture panel, 666 × 416).</summary>
    private const float PictureGap = 6, PictureShape = 416f / 666f;

    /// <summary>The details' left in from the picture's and their top under it.</summary>
    private const float DetailsIn = 14, DetailsUnder = 14;

    /// <summary>The picture panel (the original's frame, its own words in it) at the room's width.</summary>
    public static PhoneFrame.Box Picture(PhoneFrame f)
    {
        float w = f.Room.W - f.Px(2 * SideIn);
        return new PhoneFrame.Box(f.Room.X + f.Px(SideIn), Band(f).Bottom + f.Px(PictureGap), w, w * PictureShape);
    }

    /// <summary>The details' left and top under the picture (their insides as held landscape's, <see cref="PhonePickers.HeartAt"/> on).</summary>
    public static (float X, float Y) Details(PhoneFrame f)
    {
        var picture = Picture(f);
        return (picture.X + f.Px(DetailsIn), picture.Bottom + f.Px(DetailsUnder));
    }

    /// <summary>The details' right end: as far in from the room's right as their left is from its left.</summary>
    public static float DetailsRight(PhoneFrame f) => f.Room.Right - f.Px(SideIn + DetailsIn);

    // ------------------------------------------------------------------ the grid (picker_grid)

    /// <summary>The pickers' grid: three columns of 17 rows, the cells 8 apart across and 6 down.</summary>
    public const int Columns = 3, Rows = 17;

    private const float CellGap = 8, RowGap = 6;

    /// <summary>The grids' distance over the bar.</summary>
    private const float GridFoot = 20;

    /// <summary>
    /// A grid of the pickers' cells scrolled by whole rows: its area, its rows' height; how many rows its area shows, how far
    /// it scrolls, where a cell stands at a scroll, and which cell is at a point.
    /// </summary>
    public sealed record ScrolledGrid(PhoneFrame.Box Area, float RowHeight, float Gap, float CellGap)
    {
        /// <summary>A row and the gap under it.</summary>
        public float Pitch => RowHeight + Gap;

        /// <summary>The rows the area shows whole.</summary>
        public int Shown => Math.Clamp((int)Math.Floor((Area.H + Gap) / Pitch + 0.001f), 1, Rows);

        /// <summary>How far the grid scrolls: its rows past those shown.</summary>
        public int MaxScroll => Rows - Shown;

        /// <summary>
        /// Cell <paramref name="number"/> (1-51, left to right, top to bottom as the picker's) with the grid scrolled by
        /// <paramref name="scroll"/> rows; null while its row is out of view.
        /// </summary>
        public PhoneFrame.Box? Cell(int number, int scroll)
        {
            int row = (number - 1) / Columns - scroll, column = (number - 1) % Columns;
            if (row < 0 || row >= Shown)
                return null;
            float w = (Area.W - (Columns - 1) * CellGap) / Columns;
            return new PhoneFrame.Box(Area.X + column * (w + CellGap), Area.Y + row * Pitch, w, RowHeight);
        }

        /// <summary>The number of the cell shown at (<paramref name="x"/>, <paramref name="y"/>), or 0 off every cell.</summary>
        public int CellAt(int scroll, float x, float y)
        {
            for (int n = 1; n <= Columns * Rows; n++)
                if (Cell(n, scroll) is { } cell && cell.Contains(x, y))
                    return n;
            return 0;
        }

        /// <summary>The scroll that shows cell <paramref name="number"/>'s row, moving <paramref name="scroll"/> as little as it can.</summary>
        public int Reveal(int number, int scroll)
        {
            int row = (number - 1) / Columns;
            if (row < scroll)
                return Math.Min(row, MaxScroll);
            return row >= scroll + Shown ? Math.Min(MaxScroll, row - Shown + 1) : Math.Clamp(scroll, 0, MaxScroll);
        }
    }

    /// <summary>The harem's grid: 190 under the details' top, down to 20 over the bar, its rows 128 high (7.3 mm).</summary>
    private const float HaremDetails = 190, HaremRow = 128;

    /// <summary>The harem's grid under her details.</summary>
    public static ScrolledGrid HaremGrid(PhoneFrame f)
    {
        float top = Details(f).Y + f.Px(HaremDetails), foot = f.Bar.Y - f.Px(GridFoot);
        var area = new PhoneFrame.Box(f.Room.X + f.Px(SideIn), top, f.Room.W - f.Px(2 * SideIn), Math.Max(0, foot - top));
        return new ScrolledGrid(area, f.Px(HaremRow), f.Px(RowGap), f.Px(CellGap));
    }

    /// <summary>A harem cell's insides in a wide cell (<c>harem_cell</c>): the heart's middle in from the left, the name's left.</summary>
    public const float CellHeartIn = 34, CellNameIn = 62;

    // ------------------------------------------------------------------ the treasure's tabs and grid (pt2, lacquer_tabs, treasure_cell)

    /// <summary>The treasure's tabs: 180 under the details' top, 110 high, 12 apart; Back's width in their row while a first pick waits; the grid 14 under them in rows of 118 (6.7 mm).</summary>
    private const float TabsUnder = 180, TabsHeight = 110, TabsGap = 12, BackWidth = 220, TreasureGridUnder = 14, TreasureRow = 118;

    private static PhoneFrame.Box TabsRow(PhoneFrame f) =>
        new(f.Room.X + f.Px(SideIn), Details(f).Y + f.Px(TabsUnder), f.Room.W - f.Px(2 * SideIn), f.Px(TabsHeight));

    /// <summary>The view's tab <paramref name="view"/> under the details: 0 Characters, 1 Items.</summary>
    public static PhoneFrame.Box Tab(PhoneFrame f, int view)
    {
        var row = TabsRow(f);
        float w = (row.W - f.Px(TabsGap)) / 2;
        return new PhoneFrame.Box(row.X + view * (w + f.Px(TabsGap)), row.Y, w, row.H);
    }

    /// <summary>A first pick's prompt in the tabs' place, Back right of it.</summary>
    public static PhoneFrame.Box Prompt(PhoneFrame f)
    {
        var row = TabsRow(f);
        return new PhoneFrame.Box(row.X, row.Y, row.W - f.Px(BackWidth + TabsGap), row.H);
    }

    /// <summary>Back at the tabs' row's right: the pick dropped (the original's right click).</summary>
    public static PhoneFrame.Box Back(PhoneFrame f)
    {
        var row = TabsRow(f);
        return new PhoneFrame.Box(row.Right - f.Px(BackWidth), row.Y, f.Px(BackWidth), row.H);
    }

    /// <summary>The treasure's grid under its tabs.</summary>
    public static ScrolledGrid TreasureGrid(PhoneFrame f)
    {
        float top = TabsRow(f).Bottom + f.Px(TreasureGridUnder), foot = f.Bar.Y - f.Px(GridFoot);
        var area = new PhoneFrame.Box(f.Room.X + f.Px(SideIn), top, f.Room.W - f.Px(2 * SideIn), Math.Max(0, foot - top));
        return new ScrolledGrid(area, f.Px(TreasureRow), f.Px(RowGap), f.Px(CellGap));
    }

    /// <summary>
    /// A treasure cell's insides in a wide cell (<c>treasure_cell</c>): the name 18 in, its type; a holder at the right, 14
    /// in, 4 under the middle, its type; a unit's diamond's middle in from the right.
    /// </summary>
    public const float ItemCellIn = 18, ItemCellText = 34, HolderRight = 14, HolderDown = 4, HolderText = 24, DiamondRight = 30;

    /// <summary>The room a callout about the red button stands in (the script's questions, ours, a refusal): the room's width from the picture's top down to 40 over the bar, the callout standing on its foot.</summary>
    private const float BesideFoot = 40;

    /// <summary>Where a callout about the red button stands: across the room, on the grid's foot, its tail down to the button.</summary>
    public static PhoneFrame.Box BesideAction(PhoneFrame f)
    {
        float top = Picture(f).Y;
        return new PhoneFrame.Box(f.Room.X + f.Px(SideIn), top, f.Room.W - f.Px(2 * SideIn), f.Bar.Y - f.Px(BesideFoot) - top);
    }

    /// <summary>The scroll bar: as wide as this in the room's right margin, this far in from the room's right; the thumb at least this long.</summary>
    private const float BarWidth = 6, BarIn = 11, ThumbLeast = 60;

    /// <summary>The scroll bar's track beside <paramref name="grid"/> and its thumb at <paramref name="scroll"/>.</summary>
    public static (PhoneFrame.Box Track, PhoneFrame.Box Thumb) ScrollBar(PhoneFrame f, ScrolledGrid grid, int scroll)
    {
        var track = new PhoneFrame.Box(f.Room.Right - f.Px(BarIn), grid.Area.Y, f.Px(BarWidth), grid.Area.H);
        float h = Math.Max(f.Px(ThumbLeast), track.H * grid.Shown / Rows);
        float y = grid.MaxScroll == 0 ? track.Y : track.Y + (track.H - h) * Math.Clamp(scroll, 0, grid.MaxScroll) / grid.MaxScroll;
        return (track, new PhoneFrame.Box(track.X, y, track.W, h));
    }

    // ------------------------------------------------------------------ the bar (end_and)

    /// <summary>The red button's width at the bar's left; the gap after it.</summary>
    private const float ActionWidth = 760, BarGap = 16;

    /// <summary>End at the bar's right: a way back stands right of the action (owner, 2026-10-30).</summary>
    public static PhoneFrame.Box End(PhoneFrame f)
    {
        float w = Math.Max(0, f.Bar.W - f.Px(ActionWidth + BarGap));
        return new(f.Bar.Right - w, f.Bar.Y, w, f.Bar.H);
    }

    /// <summary>The red button at the bar's left: Summon, Take Off, …</summary>
    public static PhoneFrame.Box Action(PhoneFrame f) => new(f.Bar.X, f.Bar.Y, f.Px(ActionWidth), f.Bar.H);
}
