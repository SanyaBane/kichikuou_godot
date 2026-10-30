using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The scenes on the phone held landscape (docs/ui/phone-build.md, step L1; docs/ui/phone.md, §19 LS1 and LS1m, §14 LW1,
/// §26 LR2): the picture and the script's windows stay in the frame as on the PC, and our controls go to the black sides.
/// The left side holds the Menu cell on top and the drawer's Skip and Log at the left thumb, Log at the foot; a <c>]</c>
/// menu's rows stand at the frame's right foot, at the right thumb; the message log is a paper across the width, from the
/// left side's cells to the right side's room; the week report's card is drawn larger in the frame's middle. Held upright
/// (step P1; phone.md, §18 PS2, PS2l, PS2b, PS2m, §21 PK1, §8 PW1, §25 PR2) the picture stands between the hole and the
/// bar (<see cref="PhoneFrame.Frame"/>), the message windows over or under it (<see cref="Window"/>), Menu, Log and Skip
/// in the bar's left, a <c>]</c> menu's rows at the thumb in the bar's place, the log's paper over the screen, the week
/// report's card at the width. The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>) in the reference phone's
/// pixels (<see cref="PhoneFrame.Px"/>), the boxes in the canvas's units. Interface only. Engine-independent.
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

    /// <summary>Upright: the bar's cells' width (9.7 mm) and the room between them (PK1).</summary>
    private const float BarCell = 170, BarCellGap = 14;

    /// <summary>
    /// Upright: the message windows' margin from the screen's sides, their distance from the picture, the height of a
    /// speech's, a thought's and the narration's (23.9 mm: five lines of the script's at their type, so the picture never
    /// moves) and of 131's; the text's room from the panel's side and top, 131's (under its speaker's plate); the type
    /// at most (the script's 24, 48 screen pixels): a speech's 3.6 mm, 131's 2.6 mm (PS2, PS2b).
    /// </summary>
    private const float WindowSide = 16, WindowGap = 64, WindowHeight = 420, CgBarHeight = 330, TextSide = 44, TextTop = 34,
        CgBarSide = 24, CgBarTop = 56, SpeechType = 64, CgBarType = 46, ScriptType = 48;

    /// <summary>Upright: where a speech's tail or a thought's bubbles point in the picture — at the left portrait's, the right one's (layout pixels).</summary>
    private const float LeftPortraitX = 240, RightPortraitX = 1040;

    /// <summary>
    /// Upright: a <c>]</c> menu's rows (PS2m): their height (7.4 mm) and the room between them, their sides' distance from
    /// the screen's, the panel's room over them, the room kept under the lower window; a long menu's rows get lower down
    /// to this part of their height before they rise over the window's foot.
    /// </summary>
    private const float UpRowHeight = 130, UpRowGap = 14, UpRowSide = 40, UpPanelTop = 30, UpPanelFoot = 8, UpRowsOver = 16,
        UpRowsLeast = 0.6f;

    /// <summary>Upright: the log's paper's room from the bar and the screen's top (PR2).</summary>
    private const float UpPaperTop = 10, UpPaperFoot = 20;

    /// <summary>
    /// The Menu cell on top of the left side (the menu bar's commands, the Menu sheet; Close while a sheet of ours is up);
    /// upright the bar's first cell (PK1).
    /// </summary>
    public static PhoneFrame.Box MenuCell(PhoneFrame f) =>
        f.IsUpright ? BarCellAt(f, 0) : Cell(f, f.LeftRoom.Y + f.Px(MenuTop), MenuHeight);

    /// <summary>The drawer's Log at the left thumb, at the side's foot; upright the bar's second cell.</summary>
    public static PhoneFrame.Box LogCell(PhoneFrame f) =>
        f.IsUpright ? BarCellAt(f, 1) : Cell(f, f.LeftRoom.Bottom - f.Px(DrawerFoot + DrawerHeight), DrawerHeight);

    /// <summary>The drawer's Skip, over Log; upright the bar's third cell, right of Log.</summary>
    public static PhoneFrame.Box SkipCell(PhoneFrame f) =>
        f.IsUpright ? BarCellAt(f, 2) : Cell(f, LogCell(f).Y - f.Px(DrawerGap + DrawerHeight), DrawerHeight);

    /// <summary>The message log's Close: in the Menu cell's place held landscape (LR2), in Log's upright (PR2).</summary>
    public static PhoneFrame.Box LogClose(PhoneFrame f) => f.IsUpright ? LogCell(f) : MenuCell(f);

    /// <summary>
    /// The message log's paper (LR2): from just right of the left side's cells — the frame's left edge — to the right
    /// side's room, the screen's height but the margins and the gesture zone; the Menu cell's place left for its Close.
    /// </summary>
    public static PhoneFrame.Box LogPaper(PhoneFrame f)
    {
        if (f.IsUpright)
        {
            // PR2: over the screen from the hole's strip to the bar, its sides as the bar's.
            float top = f.Room.Y + f.Px(UpPaperTop);
            return new PhoneFrame.Box(f.Bar.X, top, f.Bar.W, f.Bar.Y - f.Px(UpPaperFoot) - top);
        }
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
        if (f.IsUpright)
            return UprightMenu(f, count, widest);
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
    public static float ReportScale(PhoneFrame f, float width, float height) => f.IsUpright
        ? Math.Min(f.Bar.W / (width * f.Scale), (f.Bar.Y - f.Room.Y - 2 * f.Px(ReportMargin)) / (height * f.Scale))    // PW1: at the bar's width
        : Math.Min(f.Px(ReportWidth) / (width * f.Scale), (f.Frame.H - 2 * f.Px(ReportMargin)) / (height * f.Scale));

    /// <summary>
    /// A message window held upright (PS2, PS2l, PS2b): its <see cref="Panel"/> across the screen, <see cref="Above"/> the
    /// picture or under it; its text drawn at <see cref="Scale"/> canvas units a screen pixel (of the 1280×800 picture)
    /// with the script's text box's corner at (<see cref="TextX"/>, <see cref="TextY"/>) — the script's line breaks and
    /// cells kept, the rows <see cref="RowScale"/> apart (a little more than the cells, as the mock-ups' lines) —, and its
    /// speech's tail or thought's bubbles pointing at <see cref="PointerX"/> (null: no portrait to point at).
    /// </summary>
    public sealed record UprightWindow(PhoneFrame.Box Panel, bool Above, float Scale, float TextX, float TextY, float? PointerX)
    {
        /// <summary>The canvas's units a screen pixel down the rows: the script's 2 pixels between lines spread to the type's size.</summary>
        public float RowScale => Scale * LineSpread;
    }

    /// <summary>The rows' pitch upright over the cells': the script's (the size + 2) read too close at the phone's type.</summary>
    private const float LineSpread = 1.08f;


    /// <summary>
    /// The window of <paramref name="kind"/> whose text the script printed into <paramref name="text"/> (screen pixels) held
    /// upright, while the left and the right portrait show or not (<paramref name="portraits"/>). The window of the left
    /// portrait (111, 112), which stands over the right one at the picture's top, goes above the picture, its tail or
    /// bubbles down at it; every other — the right portrait's (121, 122), the narration and a place's title (113, 123),
    /// 131 under its CG, and a left window with no left portrait (most of Rance's lines, docs/ui/messages.md, §1) — under
    /// it, a speech's or a thought's pointing up at its portrait when that shows. A speech's, a thought's and the
    /// narration's lines at a speech's type at most, 131's at its own, smaller if the text box would not fit the panel.
    /// </summary>
    public static UprightWindow Window(PhoneFrame f, MessageKind kind, (int X, int Y, int W, int H) text, (bool Left, bool Right) portraits)
    {
        bool left = kind is MessageKind.SpeechLeft or MessageKind.ThoughtLeft;
        bool speaks = left || kind is MessageKind.SpeechRight or MessageKind.ThoughtRight;
        bool portrait = left ? portraits.Left : portraits.Right;
        bool above = left && portrait;
        bool bar = kind == MessageKind.CgBar;
        float h = f.Px(bar ? CgBarHeight : WindowHeight);
        var panel = new PhoneFrame.Box(f.Room.X + f.Px(WindowSide), above ? f.Frame.Y - f.Px(WindowGap) - h : f.Frame.Bottom + f.Px(WindowGap),
            f.Room.W - 2 * f.Px(WindowSide), h);
        float side = f.Px(bar ? CgBarSide : TextSide);
        float scale = Math.Min(f.Px(bar ? CgBarType : SpeechType) / ScriptType, (panel.W - 2 * side) / Math.Max(1, text.W));
        float? pointer = speaks && portrait ? f.Frame.X + (left ? LeftPortraitX : RightPortraitX) * f.Scale : null;
        return new UprightWindow(panel, above, scale, panel.X + side, panel.Y + f.Px(bar ? CgBarTop : TextTop), pointer);
    }

    /// <summary>
    /// The message log's lines held upright (PR2): each speaker's name — a choice's label — on a line of its own over the
    /// lines it heads, a chat's way; a place's heading and the lines without a name as they are.
    /// </summary>
    public static IReadOnlyList<LogLine> NamesOver(IReadOnlyList<LogLine> lines)
    {
        var stacked = new List<LogLine>(lines.Count * 3 / 2);
        foreach (var line in lines)
        {
            if (NameOver(line))
            {
                stacked.Add(new LogLine(line.Kind, line.Label, ""));
                stacked.Add(line with { Label = "" });
            }
            else
            {
                stacked.Add(line);
            }
        }
        return stacked;
    }

    /// <summary>
    /// Where line <paramref name="index"/> of <paramref name="lines"/> stands in <see cref="NamesOver"/>'s list (its name's
    /// line when it has one): the log's reading place kept across a turn of the phone; <see cref="FromNamesOver"/> back.
    /// </summary>
    public static int ToNamesOver(IReadOnlyList<LogLine> lines, int index)
    {
        int at = 0;
        for (int i = 0; i < Math.Min(index, lines.Count); i++)
            at += NameOver(lines[i]) ? 2 : 1;
        return at;
    }

    /// <summary>The line of <paramref name="lines"/> that line <paramref name="stacked"/> of <see cref="NamesOver"/>'s list comes from.</summary>
    public static int FromNamesOver(IReadOnlyList<LogLine> lines, int stacked)
    {
        int at = 0;
        for (int i = 0; i < lines.Count; i++)
        {
            at += NameOver(lines[i]) ? 2 : 1;
            if (at > stacked)
                return i;
        }
        return lines.Count;
    }

    /// <summary>Whether <see cref="NamesOver"/> puts <paramref name="line"/>'s name on a line of its own.</summary>
    private static bool NameOver(LogLine line) => line.Label.Length > 0 && line.Kind != LogKind.Place;

    /// <summary>
    /// A <c>]</c> menu held upright (PS2m): rows across the screen from the bar's foot up, in the script's order, the panel
    /// round them; under the lower window's foot, the rows lower for a long menu, down to <see cref="UpRowsLeast"/> of their
    /// height — below that they keep it and rise over the window. The type smaller for an item too wide for its row.
    /// </summary>
    private static MenuRows UprightMenu(PhoneFrame f, int count, float widest)
    {
        float foot = f.Bar.Bottom, x = f.Room.X + f.Px(UpRowSide), width = f.Room.W - 2 * f.Px(UpRowSide);
        float full = count * f.Px(UpRowHeight) + (count - 1) * f.Px(UpRowGap);
        float under = Window(f, MessageKind.Narration, default, default).Panel.Bottom + f.Px(UpRowsOver + UpPanelTop);
        float scale = Math.Min(1, (foot - under) / full);
        if (scale < UpRowsLeast)
            scale = Math.Min(UpRowsLeast, (foot - (f.Room.Y + f.Px(RowsTop + UpPanelTop))) / full);
        float h = f.Px(UpRowHeight) * scale, gap = f.Px(UpRowGap) * scale, pad = f.Px(RowPad);
        float top = foot - count * h - (count - 1) * gap;
        var rows = Enumerable.Range(0, count).Select(i => new PhoneFrame.Box(x, top + i * (h + gap), width, h)).ToList();
        float type = Math.Min(scale, widest > 0 ? (width - 2 * pad) / widest : 1);
        float side = f.Px(UpRowSide) - f.Px(WindowSide);
        return new MenuRows(new PhoneFrame.Box(x - side, top - f.Px(UpPanelTop), width + 2 * side, foot + f.Px(UpPanelFoot) - (top - f.Px(UpPanelTop))),
            rows, ItemTextSize(f) * type, pad);
    }

    /// <summary>The bar's cell <paramref name="index"/> from its left held upright: Menu, Log, Skip (PK1).</summary>
    private static PhoneFrame.Box BarCellAt(PhoneFrame f, int index) =>
        new(f.Bar.X + index * f.Px(BarCell + BarCellGap), f.Bar.Y, f.Px(BarCell), f.Bar.H);


    private static PhoneFrame.Box Cell(PhoneFrame f, float y, float height)
    {
        var room = f.LeftRoom;
        return new PhoneFrame.Box(room.X + f.Px(CellInner), y, room.W - f.Px(CellInner + CellOuter), f.Px(height));
    }
}
