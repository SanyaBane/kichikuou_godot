namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The scenes on the phone held landscape (docs/ui/phone-build.md, step L1; docs/ui/phone.md, §19 LS1 and LS1m, §14 LW1,
/// §26 LR2): the picture and the script's windows stay in the frame as on the PC, and our controls go to the black sides.
/// The left side holds the Menu cell on top and the drawer's Skip and Log at the left thumb, Log at the foot; a <c>]</c>
/// menu's rows stand at the frame's right foot, at the right thumb; the message log is a paper across the width, from the
/// left side's cells to the right side's room; the week report's card is drawn larger in the frame's middle. The figures
/// are the mock-ups' (<c>tools/ui/mock_phone.py</c>) in the reference phone's pixels (<see cref="PhoneFrame.Px"/>), the
/// boxes in the canvas's units. Interface only. Engine-independent.
/// </summary>
public static class PhoneScenes
{
    /// <summary>The cells' room in the left side: from the hole's strip plus the first to the frame less the second (the mock-ups' rail, 14.5 mm).</summary>
    private const float CellInner = 6, CellOuter = 10;

    /// <summary>The Menu cell: its top and height (6.3 mm).</summary>
    private const float MenuTop = 16, MenuHeight = 110;

    /// <summary>Skip and Log: each cell's height (6.7 mm), the room between them, Log's foot over the gesture zone.</summary>
    private const float DrawerHeight = 118, DrawerGap = 16, DrawerFoot = 10;

    /// <summary>The message log's paper: right of the left side's cells, its margins at the top, the right and the foot.</summary>
    private const float PaperLeft = 2, PaperTop = 14, PaperRight = 6, PaperFoot = 6;

    /// <summary>
    /// A <c>]</c> menu's rows: their height (7.0 mm), the room between them, their right edge's and foot's distance from
    /// the frame's and the gesture zone's, the narrowest row, the room round the rows in their panel and within a row
    /// before its text, and the items' type (2.7 mm). The panel keeps <see cref="RowsTop"/> under the screen's top.
    /// </summary>
    private const float RowHeight = 123, RowGap = 12, RowsRight = 30, RowsFoot = 10, RowsWidth = 500, PanelPad = 16, RowPad = 24,
        ItemSize = 48, RowsTop = 16;

    /// <summary>The week report's card: as wide as the mock-up's (75.2 mm), unless its height would leave less than this over and under it.</summary>
    private const float ReportWidth = 1320, ReportMargin = 60;

    /// <summary>The Menu cell on top of the left side (the menu bar's commands, the Menu sheet; Close while a sheet of ours is up).</summary>
    public static PhoneFrame.Box MenuCell(PhoneFrame f) => Cell(f, f.LeftRoom.Y + f.Px(MenuTop), MenuHeight);

    /// <summary>The drawer's Log at the left thumb, at the side's foot.</summary>
    public static PhoneFrame.Box LogCell(PhoneFrame f) => Cell(f, f.LeftRoom.Bottom - f.Px(DrawerFoot + DrawerHeight), DrawerHeight);

    /// <summary>The drawer's Skip, over Log.</summary>
    public static PhoneFrame.Box SkipCell(PhoneFrame f) => Cell(f, LogCell(f).Y - f.Px(DrawerGap + DrawerHeight), DrawerHeight);

    /// <summary>
    /// The message log's paper (LR2): from just right of the left side's cells — the frame's left edge — to the right
    /// side's room, the screen's height but the margins and the gesture zone; the Menu cell's place left for its Close.
    /// </summary>
    public static PhoneFrame.Box LogPaper(PhoneFrame f)
    {
        float x = f.Frame.X + f.Px(PaperLeft), y = f.LeftRoom.Y + f.Px(PaperTop);
        return new PhoneFrame.Box(x, y, f.RightRoom.Right - f.Px(PaperRight) - x, f.LeftRoom.Bottom - f.Px(PaperFoot) - y);
    }

    /// <summary>A <c>]</c> menu's panel, its rows top down in the script's order, and its items' type (the canvas's units).</summary>
    public sealed record MenuRows(PhoneFrame.Box Panel, IReadOnlyList<PhoneFrame.Box> Rows, float TextSize, float Pad);

    /// <summary>
    /// A <c>]</c> menu of <paramref name="count"/> items at the frame's right foot (LS1m), the widest item's text
    /// <paramref name="widest"/> wide at <see cref="ItemTextSize"/>: rows of a finger's height, at least the mock-up's
    /// width, wider for a long item up to half the frame; a menu too long for the screen's height has its rows (and their
    /// type) lower, as many as fit.
    /// </summary>
    public static MenuRows SceneMenu(PhoneFrame f, int count, float widest)
    {
        count = Math.Max(1, count);
        float foot = f.LeftRoom.Bottom - f.Px(RowsFoot), right = f.Frame.Right - f.Px(RowsRight);
        float room = foot - (f.LeftRoom.Y + f.Px(RowsTop + PanelPad));
        float scale = Math.Min(1, room / (count * f.Px(RowHeight) + (count - 1) * f.Px(RowGap)));
        float h = f.Px(RowHeight) * scale, gap = f.Px(RowGap) * scale, pad = f.Px(RowPad) * scale;
        float width = Math.Clamp(widest * scale + 2 * pad, f.Px(RowsWidth), f.Frame.W / 2);
        float top = foot - count * h - (count - 1) * gap;
        var rows = Enumerable.Range(0, count).Select(i => new PhoneFrame.Box(right - width, top + i * (h + gap), width, h)).ToList();
        float p = f.Px(PanelPad);
        return new MenuRows(new PhoneFrame.Box(right - width - p, top - p, width + 2 * p, foot - top + 2 * p), rows,
            ItemTextSize(f) * scale, pad);
    }

    /// <summary>A <c>]</c> menu's items' type at its full height (2.7 mm), at which <see cref="SceneMenu"/>'s widest is measured.</summary>
    public static float ItemTextSize(PhoneFrame f) => f.Px(ItemSize);

    /// <summary>
    /// The scale the PC's week report card of <paramref name="width"/> × <paramref name="height"/> layout pixels is drawn at
    /// in the frame's middle (LW1): as wide as the mock-up's card, smaller when a long account would leave less than the
    /// margin over and under it.
    /// </summary>
    public static float ReportScale(PhoneFrame f, float width, float height) =>
        Math.Min(f.Px(ReportWidth) / (width * f.Scale), (f.Frame.H - 2 * f.Px(ReportMargin)) / (height * f.Scale));

    private static PhoneFrame.Box Cell(PhoneFrame f, float y, float height)
    {
        var room = f.LeftRoom;
        return new PhoneFrame.Box(room.X + f.Px(CellInner), y, room.W - f.Px(CellInner + CellOuter), f.Px(height));
    }
}
