namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// All Data on the phone held landscape (docs/ui/phone-build.md, step L9; docs/ui/phone.md, §23: LG2m and LG2, the owner's
/// choice): the original's screen stays in the frame — the map in miniature, a graph as it draws it — and our controls go to
/// the black sides. The left side holds the Menu cell on top and the four graphs as cells at the left thumb (7.5 mm, their
/// names in two lines; the shown graph lit); the right side Back at the thumb, and over a graph the scale as eight chips in
/// two columns over it (6.9 × 6.4 mm). The PC's menu box is not drawn; a graph's figures over its bars are drawn again at
/// the phone's type (<see cref="Figure"/>). The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>, <c>side_graphs</c>,
/// <c>graph_cell</c>, <c>lg2</c>, <c>scale_chips</c>, <c>funds_relabelled</c>) in the reference phone's pixels, the boxes in
/// the canvas's units. Interface only. Engine-independent.
/// </summary>
public static class PhoneData
{
    /// <summary>The graphs and the chips.</summary>
    public const int Graphs = 4, Scales = 8;

    /// <summary>The rails in the sides: in from the hole's strip and from the frame (the mock-ups' rails, 14.5 mm).</summary>
    private const float StripIn = 6, FrameIn = 10;

    /// <summary>A graph's cell, the room between two; how far over the gesture zone the side's foot is.</summary>
    private const float CellHeight = 132, CellGap = 14, FootOver = 10;

    /// <summary>Back's height; the chips' height, the room between them, between them and Back.</summary>
    private const float BackHeight = 150, ChipHeight = 112, ChipGap = 12, ChipsOver = 30;

    /// <summary>A side's heading (GRAPHS, SCALE): in from the rail's left, its middle over the first cell or chip.</summary>
    private const float HeadingIn = 4, GraphsUp = 32, ScaleUp = 36;

    /// <summary>A figure's baseline over the bar's frame line, in layout pixels (the mock-up's <c>top - 5</c> over the line's grey).</summary>
    private const float FigureOver = 6;

    /// <summary>
    /// The type: a graph's name (two lines this far apart; the four cells of the left side Cinzel 700 at one size), the
    /// headings (Cinzel), the right side's buttons — the chips and Back, Cinzel 700 at one size (docs/ui/style.md, "Buttons
    /// side by side") —, a figure's (2.3 mm). Each group shrinks together where a word is too long for its box.
    /// </summary>
    public const float CellText = 38, CellLine = 46, HeadingText = 26, ButtonText = 38, FigureText = 40;

    private static float Foot(PhoneFrame f) => f.Screen.H - f.Safe.Bottom - f.Px(FootOver);

    private static (float X, float W) LeftRail(PhoneFrame f) =>
        (f.LeftRoom.X + f.Px(StripIn), f.LeftRoom.W - f.Px(StripIn + FrameIn));

    private static (float X, float W) RightRail(PhoneFrame f) =>
        (f.RightRoom.X + f.Px(FrameIn), f.RightRoom.W - f.Px(StripIn + FrameIn));

    /// <summary>Graph <paramref name="index"/>'s cell (0 All Funds … 3 All Casualties), the last at the left thumb.</summary>
    public static PhoneFrame.Box Graph(PhoneFrame f, int index)
    {
        var (x, w) = LeftRail(f);
        float top = Foot(f) - f.Px(Graphs * CellHeight + (Graphs - 1) * CellGap);
        return new PhoneFrame.Box(x, top + index * f.Px(CellHeight + CellGap), w, f.Px(CellHeight));
    }

    /// <summary>GRAPHS: its left and middle, over the first cell.</summary>
    public static (float X, float Middle) GraphsHeading(PhoneFrame f) => (Graph(f, 0).X + f.Px(HeadingIn), Graph(f, 0).Y - f.Px(GraphsUp));

    /// <summary>Back (the menu's Return, a graph's left click) at the right thumb.</summary>
    public static PhoneFrame.Box Back(PhoneFrame f)
    {
        var (x, w) = RightRail(f);
        return new PhoneFrame.Box(x, Foot(f) - f.Px(BackHeight), w, f.Px(BackHeight));
    }

    /// <summary>Chip <paramref name="index"/> (0 1x … 7 128x), two a row over Back.</summary>
    public static PhoneFrame.Box Chip(PhoneFrame f, int index)
    {
        var (x, w) = RightRail(f);
        int rows = Scales / 2;
        float top = Back(f).Y - f.Px(ChipsOver + rows * ChipHeight + (rows - 1) * ChipGap);
        float cw = (w - f.Px(ChipGap)) / 2;
        int row = index / 2, column = index % 2;
        return new PhoneFrame.Box(x + column * (cw + f.Px(ChipGap)), top + row * f.Px(ChipHeight + ChipGap), cw, f.Px(ChipHeight));
    }

    /// <summary>SCALE: its left and middle, over the first chips.</summary>
    public static (float X, float Middle) ScaleHeading(PhoneFrame f) => (Chip(f, 0).X + f.Px(HeadingIn), Chip(f, 0).Y - f.Px(ScaleUp));

    /// <summary>
    /// A bar's figure drawn again over it (LG2): its middle and baseline for the bar whose left is <paramref name="barX"/> and
    /// whose top is <paramref name="barTop"/> on the screen (script pixels; the bar 16 wide, framed one pixel over its top) —
    /// centred on the bar, a little over its frame.
    /// </summary>
    public static (float X, float Baseline) Figure(PhoneFrame f, int barX, int barTop) =>
        (f.Frame.X + (2 * barX + 16) * f.Scale, f.Frame.Y + (2 * (barTop - 1) - FigureOver) * f.Scale);
}
