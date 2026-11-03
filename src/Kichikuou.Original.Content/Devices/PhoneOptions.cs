namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The options window on the phone held landscape (docs/ui/phone-build.md, step L8; docs/ui/phone.md, §16: LO1, the owner's
/// choice): the paper sheet across the whole width between the hole's strips over the wave backdrop, its five paper tabs on
/// top as on the PC (<see cref="Tab"/>), the title at their right; each tab's settings in one column across the sheet
/// (<see cref="Across"/>), scrolled between the sheet's top and Defaults | Close (owner, 2026-11-03: <see cref="View"/>,
/// <see cref="ColumnTop"/>, <see cref="MaxScroll"/>, <see cref="ScrollBar"/>), Messages' first row in two columns
/// (<see cref="Column"/>), Defaults | Close at the right column's foot, Close at the right thumb (<see cref="Defaults"/>,
/// <see cref="Close"/>). The settings are the PC's parts at PO1's
/// type (<see cref="PartScale"/>). The figures are the mock-up's (<c>tools/ui/mock_phone.py</c>, <c>lo1</c>,
/// <c>messages_columns</c>, <c>paper_button</c>) in the reference phone's pixels, the boxes in the canvas's units; a longer
/// screen widens the sheet and its columns. Interface only. Engine-independent.
/// </summary>
public static class PhoneOptions
{
    /// <summary>
    /// The reference pixels a unit of the PC's parts takes (their drawing's scale): PO1's type — the settings' names (the
    /// PC's 17) at 2.3 mm, their explanations (13) at 1.7 mm, the boxes (20) at 2.6 mm.
    /// </summary>
    public const float PartScale = 2.32f;

    /// <summary>The sheet's edges in from the hole's strips; how far over the gesture zone it ends.</summary>
    private const float ScreenIn = 6, FootOver = 10;

    private static float Left(PhoneFrame f) => f.Safe.Left + f.Px(ScreenIn);

    private static float Right(PhoneFrame f) => f.Screen.W - f.Safe.Right - f.Px(ScreenIn);

    private static float Foot(PhoneFrame f) => f.Screen.H - f.Safe.Bottom - f.Px(FootOver);

    // ------------------------------------------------------------------ the tabs and the title

    /// <summary>The tabs: as many, their top, width, the gap between two, their foot (the sheet's top); a tab not chosen this much lower.</summary>
    public const int Tabs = 5;

    private const float TabsTop = 16, TabWidth = 330, TabGap = 10, TabsFoot = 126, TabDrop = 12;

    /// <summary>The tabs' words (Cinzel, shrunk to the tab's width less 20) and the chosen tab's vermilion edge.</summary>
    public const float TabText = 34, TabTextRoom = 20, TabEdge = 9;

    /// <summary>Tab <paramref name="index"/> (0 Messages … 4 Tweaks), from the sheet's left: the chosen one joined to the sheet from the top, the others lower.</summary>
    public static PhoneFrame.Box Tab(PhoneFrame f, int index, bool chosen)
    {
        float top = TabsTop + (chosen ? 0 : TabDrop);
        return new PhoneFrame.Box(Left(f) + f.Px(index * (TabWidth + TabGap)), f.Px(top), f.Px(TabWidth), f.Px(TabsFoot - top));
    }

    /// <summary>The title ("Options", the brush face) at the sheet's right over the tabs: its right in from the sheet's, its top, its type.</summary>
    public const float TitleIn = 4, TitleTop = 20, TitleText = 62;

    /// <summary>The title's right edge.</summary>
    public static float TitleRight(PhoneFrame f) => Right(f) - f.Px(TitleIn);

    // ------------------------------------------------------------------ the sheet and its columns (messages_columns)

    /// <summary>The paper sheet under the tabs, down to the room's foot.</summary>
    public static PhoneFrame.Box Sheet(PhoneFrame f)
    {
        float top = f.Px(TabsFoot);
        return new PhoneFrame.Box(Left(f), top, Right(f) - Left(f), Foot(f) - top);
    }

    /// <summary>The columns: in from the sheet's sides, from its top; half the gap between them each side of its middle; the divider in from the sheet's top and foot.</summary>
    private const float ColumnIn = 40, ColumnStart = 36, MiddleGap = 30, DividerIn = 40;

    private static float Middle(PhoneFrame f) => Sheet(f).X + Sheet(f).W / 2;

    /// <summary>Column <paramref name="index"/> unscrolled: Messages' first row, 0 the text speed at the left, 1 the message skip at the right.</summary>
    public static PhoneFrame.Box Column(PhoneFrame f, int index)
    {
        var sheet = Sheet(f);
        float top = sheet.Y + f.Px(ColumnStart), bottom = sheet.Bottom - f.Px(DividerIn);
        float x = index == 0 ? sheet.X + f.Px(ColumnIn) : Middle(f) + f.Px(MiddleGap);
        float right = index == 0 ? Middle(f) - f.Px(MiddleGap) : sheet.Right - f.Px(ColumnIn);
        return new PhoneFrame.Box(x, top, right - x, bottom - top);
    }

    /// <summary>Both columns as one, unscrolled: the one column every tab is laid out in.</summary>
    public static PhoneFrame.Box Across(PhoneFrame f)
    {
        var left = Column(f, 0);
        return new PhoneFrame.Box(left.X, left.Y, Column(f, 1).Right - left.X, left.H);
    }

    /// <summary>The rule between the columns: its x and its ends.</summary>
    public static (float X, float Top, float Bottom) Divider(PhoneFrame f)
    {
        var sheet = Sheet(f);
        return (Middle(f), sheet.Y + f.Px(DividerIn), sheet.Bottom - f.Px(DividerIn));
    }

    // ------------------------------------------------------------------ the one column, scrolled (owner, 2026-11-03)

    /// <summary>
    /// The paper over what the column shows; the room between the view's foot and Defaults | Close; the least room under
    /// the column's last line at its end; the scroll bar's right in from the sheet's (its groove right of the column's
    /// <see cref="ColumnIn"/>, its gap to it <see cref="PhoneScrollBar.Gap"/>).
    /// </summary>
    private const float ViewIn = 12, ViewFoot = 20, ColumnFoot = 20, BarIn = ColumnIn - PhoneScrollBar.Width - PhoneScrollBar.Gap;

    /// <summary>
    /// Where the tab's column is seen (owner, 2026-11-03: every tab in one column, scrolled; docs/ui/options.md, "Frame
    /// rate"): <see cref="Across"/>'s width from the sheet's top margin down to over Defaults | Close; what scrolls out of it
    /// is cut at its edges.
    /// </summary>
    public static PhoneFrame.Box View(PhoneFrame f)
    {
        var across = Across(f);
        float top = Sheet(f).Y + f.Px(ViewIn);
        return new PhoneFrame.Box(across.X, top, across.W, Math.Max(0, Defaults(f).Y - f.Px(ViewFoot) - top));
    }

    /// <summary>The column's top with it scrolled by <paramref name="scroll"/> (canvas units).</summary>
    public static float ColumnTop(PhoneFrame f, float scroll) => Sheet(f).Y + f.Px(ColumnStart) - scroll;

    /// <summary>How far a column <paramref name="height"/> high (canvas units) scrolls: until its foot stands its margin over the view's.</summary>
    public static float MaxScroll(PhoneFrame f, float height) =>
        Math.Max(0, ColumnTop(f, 0) + height - (View(f).Bottom - f.Px(ColumnFoot)));

    /// <summary>
    /// The reference scroll bar at the sheet's right along <see cref="View"/> for a column <paramref name="height"/> high
    /// scrolled by <paramref name="scroll"/>: its groove and its thumb; null while the column fits.
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

    // ------------------------------------------------------------------ Defaults | Close (lo1, paper_button)

    /// <summary>The buttons: this far over the sheet's foot, as high; Defaults as wide, Close this far right of it to the right column's end.</summary>
    private const float ButtonsUp = 30, ButtonHeight = 130, DefaultsWidth = 380, ButtonsGap = 20;

    /// <summary>Defaults' word (Cinzel) and Close's (the brush face).</summary>
    public const float DefaultsText = 36, CloseText = 56;

    /// <summary>Defaults, a paper button at the right column's foot.</summary>
    public static PhoneFrame.Box Defaults(PhoneFrame f)
    {
        float y = Sheet(f).Bottom - f.Px(ButtonsUp + ButtonHeight);
        return new PhoneFrame.Box(Column(f, 1).X, y, f.Px(DefaultsWidth), f.Px(ButtonHeight));
    }

    /// <summary>Close, the red button right of Defaults to the right column's end: at the right thumb.</summary>
    public static PhoneFrame.Box Close(PhoneFrame f)
    {
        var defaults = Defaults(f);
        float x = defaults.Right + f.Px(ButtonsGap);
        return new PhoneFrame.Box(x, defaults.Y, Column(f, 1).Right - x, defaults.H);
    }

    // ------------------------------------------------------------------ a question (Forget, the game folder)

    /// <summary>The question's box: the PC's 500 × 200 at the parts' scale, over the sheet's middle.</summary>
    public static PhoneFrame.Box Question(PhoneFrame f)
    {
        var sheet = Sheet(f);
        float w = f.Px(500 * PartScale), h = f.Px(200 * PartScale);
        return new PhoneFrame.Box(Middle(f) - w / 2, sheet.Y + (sheet.H - h) / 2, w, h);
    }
}
