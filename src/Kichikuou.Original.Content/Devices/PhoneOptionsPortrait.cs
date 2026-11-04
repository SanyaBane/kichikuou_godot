namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The options window on the phone held upright (docs/ui/phone-build.md, step P8a; docs/ui/phone.md, §10 and §21: PO1 with
/// PO1o, the owner's choice): the title under the hole (<see cref="Title"/>), the five paper tabs under it across the room
/// (<see cref="Tab"/>), the paper sheet under them down to the bar (<see cref="Sheet"/>), each tab's settings in one column
/// scrolled by a finger within the sheet (<see cref="View"/>, <see cref="ColumnTop"/>, <see cref="MaxScroll"/>), a thin bar
/// at the sheet's right while it scrolls (<see cref="ScrollBar"/>); Defaults | Close in the bar, Close at the right thumb
/// (<see cref="Defaults"/>, <see cref="Close"/>). The settings are the PC's parts at PO1's type, as held landscape's
/// (<see cref="PhoneOptions.PartScale"/>); Display's orientation on top as PO1o's rows (<see cref="OrientationRow"/>). The
/// figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>: <c>po1</c>, <c>po1o</c>, <c>defaults_close</c>) in the
/// reference phone's pixels from the room's top and the bar; the boxes in the canvas's units. A longer screen gives the
/// column more room. Interface only. Engine-independent.
/// </summary>
public static class PhoneOptionsPortrait
{
    // ------------------------------------------------------------------ the title and the tabs (po1)

    /// <summary>The title ("Options", the brush face): its left in from the room's, its top under the room's, its type.</summary>
    public const float TitleIn = 30, TitleTop = 4, TitleText = 62;

    /// <summary>The title's left and top.</summary>
    public static (float X, float Y) Title(PhoneFrame f) => (f.Room.X + f.Px(TitleIn), f.Room.Y + f.Px(TitleTop));

    /// <summary>The tabs: their top under the room's, their row's distance from the room's sides, the gap between two, their height; a tab not chosen this much lower.</summary>
    private const float TabsTop = 112, TabsIn = 24, TabGap = 8, TabHeight = 110, TabDrop = 12;

    /// <summary>The tabs' words (Cinzel, shrunk to the tab's width less the room), the chosen tab's vermilion edge.</summary>
    public const float TabText = 30, TabTextRoom = 16, TabEdge = 9;

    /// <summary>Tab <paramref name="index"/> (0 Messages … 4 Tweaks) across the room: the chosen one joined to the sheet from the top, the others lower.</summary>
    public static PhoneFrame.Box Tab(PhoneFrame f, int index, bool chosen)
    {
        float w = (f.Room.W - f.Px(2 * TabsIn + (PhoneOptions.Tabs - 1) * TabGap)) / PhoneOptions.Tabs;
        float top = f.Room.Y + f.Px(TabsTop + (chosen ? 0 : TabDrop));
        return new PhoneFrame.Box(f.Room.X + f.Px(TabsIn) + index * (w + f.Px(TabGap)), top, w,
            f.Room.Y + f.Px(TabsTop + TabHeight) - top);
    }

    // ------------------------------------------------------------------ the sheet and its column

    /// <summary>The sheet's distance from the room's sides and over the bar.</summary>
    private const float SheetIn = 24, SheetFoot = 20;

    /// <summary>The paper sheet under the tabs, down to the bar.</summary>
    public static PhoneFrame.Box Sheet(PhoneFrame f)
    {
        float top = f.Room.Y + f.Px(TabsTop + TabHeight);
        return new PhoneFrame.Box(f.Room.X + f.Px(SheetIn), top, Math.Max(0, f.Room.W - f.Px(2 * SheetIn)),
            Math.Max(0, f.Bar.Y - f.Px(SheetFoot) - top));
    }

    /// <summary>The column's distance from the sheet's left; its top under the sheet's, unscrolled; the least room left under its foot.</summary>
    private const float ColumnIn = 30, ColumnStart = 40, ColumnFoot = 30;

    /// <summary>The scroll bar's right in from the sheet's (the reference, <see cref="PhoneScrollBar"/>: the owner's S2 and PB, docs/ui/phone.md §43).</summary>
    private const float BarIn = 6;

    /// <summary>The column's distance from the sheet's right: the bar's room and its distance from the sheet's right, whether the tab scrolls or not.</summary>
    private static float ColumnRight(PhoneFrame f) => f.Px(BarIn) + PhoneScrollBar.Room(f);

    /// <summary>The margin of paper over and under what the column shows: a setting scrolled past it is cut there.</summary>
    private const float ViewIn = 12;

    /// <summary>Where the column is seen: its width, between the sheet's margins; what scrolls out of it is cut at its edges.</summary>
    public static PhoneFrame.Box View(PhoneFrame f)
    {
        var sheet = Sheet(f);
        return new PhoneFrame.Box(sheet.X + f.Px(ColumnIn), sheet.Y + f.Px(ViewIn), Math.Max(0, sheet.W - f.Px(ColumnIn) - ColumnRight(f)),
            Math.Max(0, sheet.H - f.Px(2 * ViewIn)));
    }

    /// <summary>The column's top with it scrolled by <paramref name="scroll"/> (canvas units).</summary>
    public static float ColumnTop(PhoneFrame f, float scroll) => Sheet(f).Y + f.Px(ColumnStart) - scroll;

    /// <summary>How far a column <paramref name="height"/> high (canvas units) scrolls: until its foot stands its margin over the sheet's.</summary>
    public static float MaxScroll(PhoneFrame f, float height) =>
        Math.Max(0, ColumnTop(f, 0) + height - (Sheet(f).Bottom - f.Px(ColumnFoot)));

    /// <summary>
    /// The reference scroll bar at the sheet's right along <see cref="View"/> for a column <paramref name="height"/> high
    /// scrolled by <paramref name="scroll"/>: its groove and its thumb (as long as the share of the column in view); null
    /// while the column fits.
    /// </summary>
    public static (PhoneFrame.Box Track, PhoneFrame.Box Thumb)? ScrollBar(PhoneFrame f, float height, float scroll)
    {
        float max = MaxScroll(f, height);
        if (max <= 0)
            return null;
        var view = View(f);
        var track = PhoneScrollBar.Groove(f, Sheet(f).Right - f.Px(BarIn), view.Y, view.H);
        return (track, PhoneScrollBar.Thumb(f, track, (height - max) / height, scroll / max));
    }

    // ------------------------------------------------------------------ Display's orientation (po1o)

    /// <summary>
    /// PO1o's row for each way of the screen's orientation, in reference pixels from the row's top and left: its height and
    /// pitch, the radio's centre in from its left (its middle the name's), the name's left and middle (its type the
    /// settings' names'), the hint's middle; the rows' top under the section's head.
    /// </summary>
    public const float OrientationRow = 140, OrientationPitch = 150, OrientationRadioIn = 46, OrientationTextIn = 96,
        OrientationNameMiddle = 52, OrientationHintMiddle = 104, OrientationRowsUnder = 64;

    // ------------------------------------------------------------------ Defaults | Close (defaults_close)

    /// <summary>Defaults' width at the bar's left, the gap to Close.</summary>
    private const float DefaultsWidth = 404, BarGap = 16;

    /// <summary>Defaults' and Close's words, Cinzel 700 at one size (docs/ui/style.md, "Buttons side by side"), shrunk together.</summary>
    public const float ButtonText = 42;

    /// <summary>Defaults, a lacquer plate at the bar's left.</summary>
    public static PhoneFrame.Box Defaults(PhoneFrame f) => new(f.Bar.X, f.Bar.Y, Math.Min(f.Px(DefaultsWidth), f.Bar.W), f.Bar.H);

    /// <summary>Close, the red button right of Defaults to the bar's end: at the right thumb.</summary>
    public static PhoneFrame.Box Close(PhoneFrame f)
    {
        float x = Defaults(f).Right + f.Px(BarGap);
        return new PhoneFrame.Box(x, f.Bar.Y, Math.Max(0, f.Bar.Right - x), f.Bar.H);
    }

    // ------------------------------------------------------------------ a question (Forget, the game folder)

    /// <summary>The question's least distance from the sheet's sides.</summary>
    private const float QuestionIn = 20;

    /// <summary>The question's box: the PC's 500 × 200 at the parts' scale, as wide as the sheet allows, over the sheet's middle.</summary>
    public static PhoneFrame.Box Question(PhoneFrame f)
    {
        var sheet = Sheet(f);
        float w = Math.Min(f.Px(500 * PhoneOptions.PartScale), sheet.W - f.Px(2 * QuestionIn));
        float h = f.Px(200 * PhoneOptions.PartScale);
        return new PhoneFrame.Box(sheet.X + (sheet.W - w) / 2, sheet.Y + (sheet.H - h) / 2, w, h);
    }
}
