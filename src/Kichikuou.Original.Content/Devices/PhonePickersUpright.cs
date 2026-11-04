namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our Harem and Treasure screens on the phone held upright (docs/ui/phone-build.md, step P7; docs/ui/phone.md, §9: PH2 and
/// PT2, the owner's choice): the title band under the hole (<see cref="Band"/>: the title, the date at the right, a
/// divider), the picture at the room's width in the PC's shape (<see cref="Picture"/>, its own words 2.4 mm), the details
/// under it (<see cref="Details"/>), then the pickers' 3 × 17 grid in taller cells than held landscape's, as many rows in
/// view as its room holds, the rest a drag away (<see cref="ScrolledGrid"/>: the harem's <see cref="HaremGrid"/>), the
/// reference scroll bar right of it (<see cref="ScrollBar"/>, the owner's S2); the red button | End in the bar
/// (<see cref="Action"/>, <see cref="End"/>). The
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
    /// A grid of the pickers' cells scrolled by whole rows: its area, its rows' height, the width it leaves at its right for
    /// the scroll bar (none while every row is in view); how many rows its area shows, how far it scrolls, where a cell
    /// stands at a scroll, and which cell is at a point.
    /// </summary>
    public sealed record ScrolledGrid(PhoneFrame.Box Area, float RowHeight, float Gap, float CellGap, float BarRoom = 0)
    {
        /// <summary>A row and the gap under it.</summary>
        public float Pitch => RowHeight + Gap;

        /// <summary>Where a finger or the wheel scrolls the grid: its area and the bar's room right of it.</summary>
        public PhoneFrame.Box Scrolled => Area with { W = Area.W + BarRoom };

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

        /// <summary>
        /// The filled cells (<paramref name="filled"/>, their numbers) out of view at <paramref name="scroll"/>: how many are
        /// above the rows shown and how many below, and the nearest of each (0: none) — the hint's (the owner's 3, 2026-11-04).
        /// </summary>
        public (int Above, int Below, int NearestAbove, int NearestBelow) Hidden(IEnumerable<int> filled, int scroll)
        {
            int above = 0, below = 0, nearestAbove = 0, nearestBelow = 0;
            foreach (int n in filled.Order())
            {
                int row = (n - 1) / Columns;
                if (row < scroll)
                {
                    above++;
                    nearestAbove = n;                               // the last above is the nearest
                }
                else if (row >= scroll + Shown)
                {
                    below++;
                    if (nearestBelow == 0)
                        nearestBelow = n;
                }
            }
            return (above, below, nearestAbove, nearestBelow);
        }
    }

    // ------------------------------------------------------------------ the hint at cells out of view (mock_more_cells.py, 3)

    /// <summary>
    /// The pill over the grid's edge row that tells of filled cells out of view ("▼ 2 more below", "▲ 3 more above"; the
    /// owner's 3, 2026-11-04): 68 high, 8 in from the rows' edge, its text's type, the room each side of the arrow and the
    /// text, the arrow's room and its half width; its shadow this far down.
    /// </summary>
    public const float PillHeight = 68, PillEdge = 8, PillText = 28, PillPad = 36, PillArrow = 36, PillArrowHalf = 11,
        PillShadow = 8;

    /// <summary>
    /// The pill at the grid's top (<paramref name="above"/>) or foot, centred over its rows, as wide as its text
    /// (<paramref name="textWidth"/>, the canvas's units) needs.
    /// </summary>
    public static PhoneFrame.Box MorePill(PhoneFrame f, ScrolledGrid grid, bool above, float textWidth)
    {
        float h = f.Px(PillHeight), w = textWidth + f.Px(2 * PillPad + PillArrow);
        float y = above ? grid.Area.Y + f.Px(PillEdge) : grid.Area.Y + grid.Shown * grid.Pitch - grid.Gap - f.Px(PillEdge) - h;
        return new PhoneFrame.Box(grid.Area.X + (grid.Area.W - w) / 2, y, w, h);
    }

    /// <summary>The pill's words: "2 more below", "1 more above".</summary>
    public static string MoreText(int count, bool above) => $"{count} more {(above ? "above" : "below")}";

    /// <summary>
    /// The ticks on the scroll bar at the rows holding a filled cell (the owner's 3): reaching this far left of the groove,
    /// this thick, an ink rim round them this wide.
    /// </summary>
    public const float TickOut = 6, TickThick = 5, TickRim = 2;

    /// <summary>A tick's middle for row <paramref name="row"/> (0-16): the whole list laid along <paramref name="track"/>.</summary>
    public static float TickY(PhoneFrame.Box track, int row) => track.Y + (row + 0.5f) * track.H / Rows;

    /// <summary>The harem's grid: 190 under the details' top, down to 20 over the bar, its rows 102 high (5.8 mm; 128 till the owner's B, 2026-11-04, `tools/ui/mock_picker_rows.py`: 20 % lower, more rows in view).</summary>
    private const float HaremDetails = 190, HaremRow = 102;

    /// <summary>The harem's grid under her details.</summary>
    public static ScrolledGrid HaremGrid(PhoneFrame f) => Grid(f, Details(f).Y + f.Px(HaremDetails), HaremRow);

    /// <summary>
    /// A grid from <paramref name="top"/> down to <see cref="GridFoot"/> over the bar, the room's width less its sides, in rows
    /// <paramref name="row"/> high; while it scrolls, narrower by the scroll bar's room (the owner's S2, docs/ui/phone.md §41).
    /// </summary>
    private static ScrolledGrid Grid(PhoneFrame f, float top, float row)
    {
        float foot = f.Bar.Y - f.Px(GridFoot), w = f.Room.W - f.Px(2 * SideIn);
        var area = new PhoneFrame.Box(f.Room.X + f.Px(SideIn), top, w, Math.Max(0, foot - top));
        var grid = new ScrolledGrid(area, f.Px(row), f.Px(RowGap), f.Px(CellGap));
        float room = PhoneScrollBar.Room(f);
        return grid.MaxScroll == 0 ? grid : grid with { Area = area with { W = w - room }, BarRoom = room };
    }

    /// <summary>A harem cell's insides in a wide cell (<c>harem_cell</c>): the heart's middle in from the left, the name's left.</summary>
    public const float CellHeartIn = 34, CellNameIn = 62;

    // ------------------------------------------------------------------ the treasure's tabs and grid (pt2, lacquer_tabs, treasure_cell)

    /// <summary>The treasure's tabs: 180 under the details' top, 110 high, 12 apart; Back's width in their row while a first pick waits; the grid 14 under them in rows of 94 (5.4 mm; 118 till the owner's B, 2026-11-04).</summary>
    private const float TabsUnder = 180, TabsHeight = 110, TabsGap = 12, BackWidth = 220, TreasureGridUnder = 14, TreasureRow = 94;

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
    public static ScrolledGrid TreasureGrid(PhoneFrame f) => Grid(f, TabsRow(f).Bottom + f.Px(TreasureGridUnder), TreasureRow);

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

    /// <summary>
    /// The scroll bar right of <paramref name="grid"/> (<see cref="PhoneScrollBar"/>, the owner's S2): its groove as tall as
    /// the rows in view, its right at the picture's, the tabs' and End's; its thumb at <paramref name="scroll"/>.
    /// </summary>
    public static (PhoneFrame.Box Track, PhoneFrame.Box Thumb) ScrollBar(PhoneFrame f, ScrolledGrid grid, int scroll)
    {
        var track = PhoneScrollBar.Groove(f, f.Room.Right - f.Px(SideIn), grid.Area.Y, grid.Shown * grid.Pitch - grid.Gap);
        float at = grid.MaxScroll == 0 ? 0 : (float)Math.Clamp(scroll, 0, grid.MaxScroll) / grid.MaxScroll;
        return (track, PhoneScrollBar.Thumb(f, track, (float)grid.Shown / Rows, at));
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
