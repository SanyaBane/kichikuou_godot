namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our Harem and Treasure screens on the phone held landscape (docs/ui/phone-build.md, step L7; docs/ui/phone.md, §15: LH2
/// and LT2, then rounds P39 and P40, §51-§56, the owner's): across the whole width between the hole's strips — at the right
/// thumb the pickers' cells five a row (<see cref="Grid"/>), scrolled under the finger with the reference scroll bar, its
/// ticks and the pills at filled cells out of view, the buttons under it: Treasure's Characters | Items (<see cref="Tab"/>;
/// while a first pick waits its prompt and Back in their place, <see cref="Prompt"/>, <see cref="Back"/>), the harem's
/// Summon in their place (<see cref="Summon"/>), Close at the right (<see cref="End"/>); at the left a column as wide as
/// the grid leaves (<see cref="Column"/>) with the title and the date, the picture at its width (<see cref="Picture"/>, the
/// PC's shape), the details under it, Treasure's touch mode button at the column's right on the buttons' foot
/// (<see cref="Action"/>). The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>lh2</c>, <c>lt2</c>,
/// <c>column_title</c>, <c>girl_details</c>, <c>item_details</c>, <c>harem_cell</c>, <c>treasure_cell_two</c>;
/// <c>tools/ui/mock_treasure_scroll.py</c>, <c>mock_harem_scroll.py</c>) in the reference phone's pixels, the boxes in the
/// canvas's units; on a longer screen the column grows till its picture fills the height over the details, the grid takes
/// the rest. Interface only. Engine-independent.
/// </summary>
public static class PhonePickers
{
    // ------------------------------------------------------------------ the split

    /// <summary>The room's edges in from the hole's strips; the gap between the column and the grid.</summary>
    private const float ScreenIn = 6, GridGap = 40;

    /// <summary>The grid's top, and how far over the gesture zone the foot's buttons end.</summary>
    private const float GridTop = 16, FootOver = 10;

    private static float Left(PhoneFrame f) => f.Safe.Left + f.Px(ScreenIn);

    private static float Right(PhoneFrame f) => f.Screen.W - f.Safe.Right - f.Px(ScreenIn);

    private static float ButtonsFoot(PhoneFrame f) => f.Screen.H - f.Safe.Bottom - f.Px(FootOver);

    /// <summary>
    /// The grid: five cells a row, at least as wide as the folded grid's were on the reference phone and its rows at least as
    /// high (the owner: their 7 mm kept); the cells 8 apart across and 6 down; the grid 20 over the buttons; the details'
    /// room under the picture (their name, figures and values, <see cref="ItemValueTop"/> on).
    /// </summary>
    public const int Columns = 5;

    private const float CellWidth = 210, RowHeight = 122, CellGap = 8, RowGap = 6, GridOverButtons = 20, DetailsRoom = 160;

    /// <summary>The grid's least width: five cells, their gaps and the scroll bar's room.</summary>
    private static float GridLeast(PhoneFrame f) => f.Px(Columns * CellWidth + (Columns - 1) * CellGap) + PhoneScrollBar.Room(f);

    /// <summary>
    /// The column at the left: as wide as the room left of the grid, but no wider than a picture as tall as the height over
    /// the details holds (a longer screen gives the rest to the grid).
    /// </summary>
    public static PhoneFrame.Box Column(PhoneFrame f)
    {
        float tallest = ButtonsFoot(f) - f.Px(PictureTop + DetailsUnder + DetailsRoom);
        float w = Math.Max(0, Math.Min(tallest / PictureShape, Right(f) - Left(f) - f.Px(GridGap) - GridLeast(f)));
        return new PhoneFrame.Box(Left(f), 0, w, f.Screen.H - f.Safe.Bottom);
    }

    private static float GridLeft(PhoneFrame f) => Column(f).Right + f.Px(GridGap);

    // ------------------------------------------------------------------ the column (column_title, lh2, lt2)

    /// <summary>
    /// The title band, reference pixels from the screen's top: the title's left (in from the column's), top and type (the
    /// brush face), the date's top (at the column's right) and type, the divider.
    /// </summary>
    public const float TitleIn = 4, TitleTop = 14, TitleText = 56, DateTop = 36, DateText = 28, Divider = 110;

    /// <summary>The picture's top (4 under the band), its shape (the PC's picture panel, 666 × 416); the details 12 under it, 14 in.</summary>
    private const float PictureTop = 128, PictureShape = 416f / 666f, DetailsUnder = 12, DetailsIn = 14;

    /// <summary>The picture panel (the original's frame, its own words in it) at the column's width.</summary>
    public static PhoneFrame.Box Picture(PhoneFrame f)
    {
        var column = Column(f);
        return new PhoneFrame.Box(column.X, f.Px(PictureTop), column.W, column.W * PictureShape);
    }

    /// <summary>The details' left and top, under the picture.</summary>
    public static (float X, float Y) Details(PhoneFrame f) => (Left(f) + f.Px(DetailsIn), Picture(f).Bottom + f.Px(DetailsUnder));

    /// <summary>
    /// The harem's details, reference pixels from their top and left (<c>girl_details</c>): the heart's middle and size, the
    /// name's left, middle and type (the brush face); the stats' labels' top and type, their values' top and type, the gap
    /// between the stats' columns.
    /// </summary>
    public const float HeartAt = 16, HeartMiddle = 34, HeartSize = 16, NameIn = 46, NameMiddle = 34, NameText = 58,
        LabelTop = 92, LabelText = 24, ValueTop = 128, ValueText = 40, StatGap = 60;

    /// <summary>
    /// The treasure's details (<c>item_details</c>): the name's middle and type, the figures' labels' and values' tops (their
    /// type the harem's), the gap between the figures' columns; the details end this far short of the column's right, left
    /// of the touch mode's button.
    /// </summary>
    public const float ItemNameMiddle = 30, ItemNameText = 52, ItemLabelTop = 82, ItemValueTop = 118, FigureGap = 50, ItemDetailsRight = 470;

    /// <summary>The touch mode's button: as wide and as high as this at the column's right; a callout 40 left of it.</summary>
    private const float ActionWidth = 440, ActionHeight = 150, BesideGap = 40;

    /// <summary>
    /// The treasure's touch mode button (Take Off, Equip, …) at the column's right beside the details, its foot on the
    /// buttons' under the grid (the owner, P39).
    /// </summary>
    public static PhoneFrame.Box Action(PhoneFrame f) =>
        new(Column(f).Right - f.Px(ActionWidth), ButtonsFoot(f) - f.Px(ActionHeight), f.Px(ActionWidth), f.Px(ActionHeight));

    /// <summary>
    /// Where a callout beside the button stands (the script's questions, ours, a refusal): left of it, 40 away, down to its
    /// foot from the picture's top — the callout stands on the foot, as tall as its text.
    /// </summary>
    public static PhoneFrame.Box BesideAction(PhoneFrame f)
    {
        var action = Action(f);
        float x = Left(f), top = Picture(f).Y;
        return new PhoneFrame.Box(x, top, action.X - f.Px(BesideGap) - x, action.Bottom - top);
    }

    // ------------------------------------------------------------------ the buttons under the grid

    /// <summary>
    /// Every button's type, Cinzel 700 (docs/ui/style.md, "Buttons side by side"): the tabs, Close, Back, Summon and the
    /// treasure's red button alike, the group shrunk together where a word is too long for its box.
    /// </summary>
    public const float ButtonText = 38;

    /// <summary>The buttons: 130 high, Close 320 wide at the right, the row left of it ending 16 short; the tabs 12 apart.</summary>
    private const float ButtonsHeight = 130, EndWidth = 320, ButtonsGap = 16, TabsGap = 12;

    /// <summary>Close at the right under the grid, its right at the scroll bar's (a way back stands right of the action, owner, 2026-10-30).</summary>
    public static PhoneFrame.Box End(PhoneFrame f) =>
        new(Right(f) - f.Px(EndWidth), ButtonsFoot(f) - f.Px(ButtonsHeight), f.Px(EndWidth), f.Px(ButtonsHeight));

    /// <summary>The row under the grid left of Close: Treasure's tabs, the harem's Summon.</summary>
    private static PhoneFrame.Box ButtonsRow(PhoneFrame f)
    {
        var end = End(f);
        float x = GridLeft(f);
        return new PhoneFrame.Box(x, end.Y, end.X - f.Px(ButtonsGap) - x, end.H);
    }

    /// <summary>The harem's Summon, red, left of Close under the grid (the owner's H1, round P40).</summary>
    public static PhoneFrame.Box Summon(PhoneFrame f) => ButtonsRow(f);

    /// <summary>The treasure's tab <paramref name="view"/>: 0 Characters, 1 Items.</summary>
    public static PhoneFrame.Box Tab(PhoneFrame f, int view)
    {
        var row = ButtonsRow(f);
        float w = (row.W - f.Px(TabsGap)) / 2;
        return new PhoneFrame.Box(row.X + view * (w + f.Px(TabsGap)), row.Y, w, row.H);
    }

    /// <summary>Back's width at the tabs' row's right while a first pick waits.</summary>
    private const float BackWidth = 220;

    /// <summary>A first pick's prompt in the tabs' place: its face and two lines, Back right of it.</summary>
    public static PhoneFrame.Box Prompt(PhoneFrame f)
    {
        var row = ButtonsRow(f);
        return new PhoneFrame.Box(row.X, row.Y, row.W - f.Px(BackWidth + TabsGap), row.H);
    }

    /// <summary>Back at the tabs' row's right: the pick dropped (the original's right click).</summary>
    public static PhoneFrame.Box Back(PhoneFrame f)
    {
        var row = ButtonsRow(f);
        return new PhoneFrame.Box(row.Right - f.Px(BackWidth), row.Y, f.Px(BackWidth), row.H);
    }

    /// <summary>The prompt's insides: the face 12 in, the text 16 right of it, its two lines' middles (of the height) and type.</summary>
    public const float PromptFaceIn = 12, PromptTextGap = 16, PromptFirst = 0.33f, PromptSecond = 0.7f, PromptText = 36;

    // ------------------------------------------------------------------ the grid (rounds P39, P40)

    /// <summary>
    /// The grid right of the column, from the top down to 20 over the buttons: five cells a row, as many rows in view as the
    /// height holds at least 122 high, stretched to fill it; the scroll bar right of it while it scrolls.
    /// </summary>
    public static PhonePickersPortrait.ScrolledGrid Grid(PhoneFrame f)
    {
        float x = GridLeft(f), top = f.Px(GridTop), w = Right(f) - x;
        float h = Math.Max(0, End(f).Y - f.Px(GridOverButtons) - top), gap = f.Px(RowGap);
        int rows = (PhonePickersPortrait.Cells + Columns - 1) / Columns;
        int shown = Math.Clamp((int)Math.Floor((h + gap) / (f.Px(RowHeight) + gap) + 0.001f), 1, rows);
        var area = new PhoneFrame.Box(x, top, w, h);
        var grid = new PhonePickersPortrait.ScrolledGrid(area, (h + gap) / shown - gap, gap, f.Px(CellGap), Columns: Columns);
        float room = PhoneScrollBar.Room(f);
        return grid.MaxScroll == 0 ? grid : grid with { Area = area with { W = w - room }, BarRoom = room };
    }

    /// <summary>
    /// A harem cell's insides (<c>harem_cell</c>, a narrow cell): the heart's middle in from the left and its size, the name's
    /// left and type (Cinzel), its room's end in from the right; the visit's mark at the top right — in, its middle, its type.
    /// </summary>
    public const float CellHeartIn = 24, CellHeart = 12, CellNameIn = 44, CellNameText = 36, CellRight = 12, CellMarkIn = 12,
        CellMarkMiddle = 26, CellMarkText = 22;

    /// <summary>
    /// A harem name too long for its cell's line breaks at a space into two lines of this type (the owner's C, 2026-11-04,
    /// <c>tools/ui/mock_harem_name_lines.py</c>: Royal Guards (13)), their middles this part of it apart, the heart at their middle.
    /// </summary>
    public const float CellTwoLineText = 30, CellTwoLinePitch = 1.05f;

    /// <summary>
    /// A treasure cell's insides (<c>treasure_cell_two</c>): the name over the holder, the cells being narrow — the name 14 in,
    /// its room ending 12 short of the right, its middle (of the height) and type; the holder at the right, its middle and type.
    /// </summary>
    public const float ItemCellIn = 14, ItemNameMiddleShare = 0.36f, ItemCellText = 34, HolderMiddleShare = 0.74f, HolderText = 26;
}
