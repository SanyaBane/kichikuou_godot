namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// Our Harem and Treasure screens on the phone held landscape (docs/ui/phone-build.md, step L7; docs/ui/phone.md, §15: LH2
/// and LT2, the owner's choice): across the whole width between the hole's strips — at the left a column with the title
/// and the date, the picture (<see cref="Picture"/>, the PC's shape) and the details under it, the buttons at the left
/// thumb; at the right thumb the pickers' 3 × 17 grid folded after its ninth row (<see cref="Cell"/>: rows 1-9 in the left
/// half, 10-17 in the right, a gold rule between them, every cell in view and each girl or item still at her number's
/// cell, the order read down the left half, then the right). The harem's foot is Summon | End (<see cref="Summon"/>,
/// <see cref="HaremEnd"/>); the treasure's Characters | Items and End (<see cref="Tab"/>, <see cref="TreasureEnd"/>), the
/// touch mode's button beside the details (<see cref="Action"/>), and while a first pick waits for its second the pick's
/// prompt and Back in the tabs' place (<see cref="Prompt"/>, <see cref="Back"/>: the tabs would drop the pick as Back
/// does). The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>lh2</c>, <c>lt2</c>, <c>folded_grid</c>,
/// <c>column_title</c>, <c>girl_details</c>, <c>item_details</c>, <c>harem_cell</c>, <c>treasure_cell_two</c>) in the
/// reference phone's pixels, the boxes in the canvas's units; a longer screen widens the grid (the column keeps its width).
/// Interface only. Engine-independent.
/// </summary>
public static class PhonePickers
{
    // ------------------------------------------------------------------ the split

    /// <summary>The room's edges in from the hole's strips; the column's width and the gap between it and the grid.</summary>
    private const float ScreenIn = 6, ColumnWidth = 1104, GridGap = 40;

    /// <summary>The grid's top, and how far over the gesture zone it and the foot's buttons end.</summary>
    private const float GridTop = 16, FootOver = 10;

    private static float Left(PhoneFrame f) => f.Safe.Left + f.Px(ScreenIn);

    private static float Right(PhoneFrame f) => f.Screen.W - f.Safe.Right - f.Px(ScreenIn);

    private static float ButtonsFoot(PhoneFrame f) => f.Screen.H - f.Safe.Bottom - f.Px(FootOver);

    /// <summary>The column at the left: the title, the picture, the details and the buttons, over the gesture zone.</summary>
    public static PhoneFrame.Box Column(PhoneFrame f) => new(Left(f), 0, f.Px(ColumnWidth), f.Screen.H - f.Safe.Bottom);

    // ------------------------------------------------------------------ the column (column_title, lh2, lt2)

    /// <summary>
    /// The title band, reference pixels from the screen's top: the title's left (in from the column's), top and type (the
    /// brush face), the date's top (at the column's right) and type, the divider.
    /// </summary>
    public const float TitleIn = 4, TitleTop = 14, TitleText = 56, DateTop = 36, DateText = 28, Divider = 110;

    /// <summary>The picture's top (4 under the band), its shape (the PC's picture panel, 666 × 416); the details 12 under it, 14 in.</summary>
    private const float PictureTop = 128, PictureShape = 416f / 666f, DetailsUnder = 12, DetailsIn = 14;

    /// <summary>The picture panel (the original's frame, its own words in it) at the column's width.</summary>
    public static PhoneFrame.Box Picture(PhoneFrame f) =>
        new(Left(f), f.Px(PictureTop), f.Px(ColumnWidth), f.Px(ColumnWidth * PictureShape));

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

    /// <summary>The touch mode's button: as wide as this at the column's right, 10 under the details' top, as high; a callout 40 left of it.</summary>
    private const float ActionWidth = 440, ActionDown = 10, ActionHeight = 150, BesideGap = 40;

    /// <summary>The button's type.</summary>
    public const float ActionText = 54;

    /// <summary>The treasure's touch mode button (Take Off, Equip, …) beside the details, at the column's right.</summary>
    public static PhoneFrame.Box Action(PhoneFrame f)
    {
        var column = Column(f);
        return new PhoneFrame.Box(column.Right - f.Px(ActionWidth), Details(f).Y + f.Px(ActionDown), f.Px(ActionWidth), f.Px(ActionHeight));
    }

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

    // ------------------------------------------------------------------ the foot's buttons

    /// <summary>The harem's buttons: 140 high, End 320 wide at the column's right, Summon from the column's left to 16 short of it.</summary>
    private const float HaremButtons = 140, EndWidth = 320, ButtonsGap = 16;

    /// <summary>The harem's End at the column's right: a way back stands right of the action (owner, 2026-10-30).</summary>
    public static PhoneFrame.Box HaremEnd(PhoneFrame f) =>
        new(Column(f).Right - f.Px(EndWidth), ButtonsFoot(f) - f.Px(HaremButtons), f.Px(EndWidth), f.Px(HaremButtons));

    /// <summary>Summon, red, left of End.</summary>
    public static PhoneFrame.Box Summon(PhoneFrame f)
    {
        var end = HaremEnd(f);
        float x = Left(f);
        return new PhoneFrame.Box(x, end.Y, end.X - f.Px(ButtonsGap) - x, end.H);
    }

    /// <summary>The foot's type: End's word, Summon's (the brush face), the tabs' (Cinzel).</summary>
    public const float EndText = 44, SummonText = 56, TabText = 34;

    /// <summary>The treasure's foot: 130 high; the tabs 12 apart, ending 16 short of End.</summary>
    private const float TreasureButtons = 130, TabsGap = 12;

    /// <summary>The treasure's End at the column's right.</summary>
    public static PhoneFrame.Box TreasureEnd(PhoneFrame f)
    {
        float right = Column(f).Right;
        return new PhoneFrame.Box(right - f.Px(EndWidth), ButtonsFoot(f) - f.Px(TreasureButtons), f.Px(EndWidth), f.Px(TreasureButtons));
    }

    private static PhoneFrame.Box TabsRow(PhoneFrame f)
    {
        var end = TreasureEnd(f);
        float x = Left(f);
        return new PhoneFrame.Box(x, end.Y, end.X - f.Px(ButtonsGap) - x, end.H);
    }

    /// <summary>The view's tab <paramref name="view"/>: 0 Characters, 1 Items.</summary>
    public static PhoneFrame.Box Tab(PhoneFrame f, int view)
    {
        var row = TabsRow(f);
        float w = (row.W - f.Px(TabsGap)) / 2;
        return new PhoneFrame.Box(row.X + view * (w + f.Px(TabsGap)), row.Y, w, row.H);
    }

    /// <summary>Back's width at the tabs' row's right while a first pick waits.</summary>
    private const float BackWidth = 220;

    /// <summary>A first pick's prompt in the tabs' place: its face and two lines, Back right of it.</summary>
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

    /// <summary>The prompt's insides: the face 12 in, the text 16 right of it, its two lines' middles (of the height) and type.</summary>
    public const float PromptFaceIn = 12, PromptTextGap = 16, PromptFirst = 0.33f, PromptSecond = 0.7f, PromptText = 36;

    // ------------------------------------------------------------------ the folded grid (folded_grid)

    /// <summary>The pickers' grid: three columns of 17 rows, cut after the ninth.</summary>
    public const int Columns = 3, Rows = 17, Fold = 9;

    /// <summary>The cells' gaps: between the halves, between two cells of a row, between two rows; the rule's ends in from the grid's.</summary>
    private const float HalvesGap = 40, CellGap = 8, RowGap = 6, RuleIn = 12;

    /// <summary>The grid at the right thumb: from 40 right of the column to the hole's strip, over the whole height.</summary>
    public static PhoneFrame.Box Grid(PhoneFrame f)
    {
        float x = Left(f) + f.Px(ColumnWidth + GridGap), top = f.Px(GridTop);
        return new PhoneFrame.Box(x, top, Right(f) - x, ButtonsFoot(f) - top);
    }

    /// <summary>
    /// Cell <paramref name="number"/> (1-51, left to right, top to bottom as the picker's): its row in the left half while it
    /// is one of the first nine, else in the right half; the rows as high as nine of them fill the grid.
    /// </summary>
    public static PhoneFrame.Box Cell(PhoneFrame f, int number)
    {
        var grid = Grid(f);
        int row = (number - 1) / Columns, column = (number - 1) % Columns;
        bool right = row >= Fold;
        float half = (grid.W - f.Px(HalvesGap)) / 2;
        float w = (half - f.Px((Columns - 1) * CellGap)) / Columns;
        float pitch = (grid.H + f.Px(RowGap)) / Fold;
        float x = grid.X + (right ? half + f.Px(HalvesGap) : 0) + column * (w + f.Px(CellGap));
        float y = grid.Y + (right ? row - Fold : row) * pitch;
        return new PhoneFrame.Box(x, y, w, pitch - f.Px(RowGap));
    }

    /// <summary>The number of the cell at (<paramref name="x"/>, <paramref name="y"/>), or 0 off every cell.</summary>
    public static int CellAt(PhoneFrame f, float x, float y)
    {
        for (int n = 1; n <= Columns * Rows; n++)
            if (Cell(f, n).Contains(x, y))
                return n;
        return 0;
    }

    /// <summary>The gold rule between the halves: its x and its ends.</summary>
    public static (float X, float Top, float Bottom) Rule(PhoneFrame f)
    {
        var grid = Grid(f);
        return (grid.X + (grid.W - f.Px(HalvesGap)) / 2 + f.Px(HalvesGap) / 2, grid.Y + f.Px(RuleIn), grid.Bottom - f.Px(RuleIn));
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
