namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// All Data on the phone held upright (docs/ui/phone-build.md, step P8c; docs/ui/phone.md, §22: PG2m and PG2, the owner's
/// choice): our page over the whole screen — the title band under the hole (the pickers', <see cref="PhonePickersUpright.Band"/>),
/// the map in miniature cut from the engine's picture at the room's width (<see cref="Miniature"/>, its <see cref="Source"/>),
/// a sheet under it down to the bar (<see cref="Sheet"/>). Over the menu of graphs the four graphs are finger rows in the
/// sheet's middle (<see cref="MenuRow"/>); over a graph they are tabs on the sheet's top (<see cref="Tab"/>), the script's
/// title under them (<see cref="TitleMiddle"/>), the plot laid out again at the phone's type (<see cref="Plot"/>: the bars
/// in sixteen slots, <see cref="Column"/> and <see cref="Level"/>, a bar's figure over it, <see cref="Figure"/>), the caption
/// under it (<see cref="Caption"/>) and the scale as eight chips in a row at the thumb (<see cref="Chip"/>); Back in the bar
/// at the right thumb (<see cref="Back"/>). The figures are the mock-ups' (<c>tools/ui/mock_phone.py</c>: <c>pg2_top</c>,
/// <c>pg2m</c>, <c>pg2</c>, <c>graph_tabs</c>, <c>scale_chips</c>, <c>funds_plot</c>, <c>end_alone</c>) in the reference
/// phone's pixels from the room's sides, its top and the bar; the boxes in the canvas's units. A longer screen gives the
/// plot more height. Interface only. Engine-independent.
/// </summary>
public static class PhoneDataUpright
{
    /// <summary>The graphs, the chips.</summary>
    public const int Graphs = PhoneData.Graphs, Scales = PhoneData.Scales;

    // ------------------------------------------------------------------ the miniature and the sheet (pg2_top)

    /// <summary>The map in miniature in the engine's 1280 × 800 layout (<c>各種DATA.ADV:1-11</c>: <c>CM … 0, 67, 640, 266</c>, doubled).</summary>
    public static readonly (int X, int Y, int W, int H) Source = (0, 134, 1280, 532);

    /// <summary>The miniature's and the sheet's distance from the room's sides; the miniature's under the band, the sheet's under it and over the bar.</summary>
    private const float SideIn = 16, MiniatureUnder = 12, SheetUnder = 20, SheetOver = 24;

    /// <summary>The map in miniature at the room's width less its margins, in its own shape, under the title band.</summary>
    public static PhoneFrame.Box Miniature(PhoneFrame f)
    {
        float w = Math.Max(0, f.Room.W - f.Px(2 * SideIn));
        return new PhoneFrame.Box(f.Room.X + f.Px(SideIn), PhonePickersUpright.Band(f).Bottom + f.Px(MiniatureUnder), w, w * Source.H / Source.W);
    }

    /// <summary>The sheet under the miniature down to the bar.</summary>
    public static PhoneFrame.Box Sheet(PhoneFrame f)
    {
        float top = Miniature(f).Bottom + f.Px(SheetUnder);
        return new PhoneFrame.Box(f.Room.X + f.Px(SideIn), top, Math.Max(0, f.Room.W - f.Px(2 * SideIn)),
            Math.Max(0, f.Bar.Y - f.Px(SheetOver) - top));
    }

    // ------------------------------------------------------------------ the menu of graphs (pg2m)

    /// <summary>A row's distance from the room's sides, its height, the room between two.</summary>
    private const float RowIn = 48, RowHeight = 150, RowGap = 18;

    /// <summary>A row's type and its name's distance from the row's left.</summary>
    public const float RowText = 52, RowTextIn = 24;

    /// <summary>Graph <paramref name="index"/>'s row (0 All Funds … 3 All Casualties), the four in the sheet's middle.</summary>
    public static PhoneFrame.Box MenuRow(PhoneFrame f, int index)
    {
        var sheet = Sheet(f);
        float top = (sheet.Y + sheet.Bottom) / 2 - f.Px(Graphs * RowHeight + (Graphs - 1) * RowGap) / 2;
        return new PhoneFrame.Box(f.Room.X + f.Px(RowIn), top + index * f.Px(RowHeight + RowGap), Math.Max(0, f.Room.W - f.Px(2 * RowIn)),
            f.Px(RowHeight));
    }

    // ------------------------------------------------------------------ a graph (pg2, graph_tabs, scale_chips)

    /// <summary>The tabs', the plot's and the chips' distance from the room's sides; the room between two tabs or chips.</summary>
    private const float PartsIn = 40, PartGap = 12;

    /// <summary>The tabs under the sheet's top, their height.</summary>
    private const float TabsDown = 26, TabHeight = 124;

    /// <summary>A tab's type and its two lines' distance from its middle.</summary>
    public const float TabText = 36, TabLine = 22;

    private static float PartsX(PhoneFrame f) => f.Room.X + f.Px(PartsIn);

    private static float PartsW(PhoneFrame f) => Math.Max(0, f.Room.W - f.Px(2 * PartsIn));

    /// <summary>Graph <paramref name="index"/>'s tab, the four across the sheet's top.</summary>
    public static PhoneFrame.Box Tab(PhoneFrame f, int index)
    {
        float w = (PartsW(f) - (Graphs - 1) * f.Px(PartGap)) / Graphs;
        return new PhoneFrame.Box(PartsX(f) + index * (w + f.Px(PartGap)), Sheet(f).Y + f.Px(TabsDown), w, f.Px(TabHeight));
    }

    /// <summary>The script's title under the tabs: its middle; its type (the game's font, 2.6 mm) and the room it is fitted to, less than the room's width.</summary>
    public const float TitleText = 48, TitleNarrower = 100;

    private const float TitleDown = 52;

    /// <summary>The script's title's middle under the tabs.</summary>
    public static float TitleMiddle(PhoneFrame f) => Tab(f, 0).Bottom + f.Px(TitleDown);

    /// <summary>The chips over the bar, their height.</summary>
    private const float ChipsOver = 64, ChipHeight = 120;

    /// <summary>A chip's type.</summary>
    public const float ChipText = 40;

    /// <summary>Chip <paramref name="index"/> (0 1x … 7 128x), the eight in a row at the thumb.</summary>
    public static PhoneFrame.Box Chip(PhoneFrame f, int index)
    {
        float w = (PartsW(f) - (Scales - 1) * f.Px(PartGap)) / Scales;
        return new PhoneFrame.Box(PartsX(f) + index * (w + f.Px(PartGap)), f.Bar.Y - f.Px(ChipsOver + ChipHeight), w, f.Px(ChipHeight));
    }

    /// <summary>SCALE over the chips: its type (Cinzel) and its left in from the room's.</summary>
    public const float HeadingText = 28;

    private const float HeadingIn = 44, ScaleUp = 30, PlotDown = 150, PlotOver = 130, CaptionDown = 44;

    /// <summary>SCALE: its left and middle, over the chips.</summary>
    public static (float X, float Middle) ScaleHeading(PhoneFrame f) => (f.Room.X + f.Px(HeadingIn), Chip(f, 0).Y - f.Px(ScaleUp));

    /// <summary>The plot between the tabs and the chips, across the room less its margins.</summary>
    public static PhoneFrame.Box Plot(PhoneFrame f)
    {
        float top = Tab(f, 0).Bottom + f.Px(PlotDown);
        return new PhoneFrame.Box(PartsX(f), top, PartsW(f), Math.Max(0, Chip(f, 0).Y - f.Px(PlotOver) - top));
    }

    /// <summary>The caption (New → Old) under the plot: its left and middle.</summary>
    public static (float X, float Middle) Caption(PhoneFrame f) => (f.Room.X + f.Px(HeadingIn), Plot(f).Bottom + f.Px(CaptionDown));

    /// <summary>The caption's type.</summary>
    public const float CaptionText = 40;

    // ------------------------------------------------------------------ the bars (funds_plot)

    /// <summary>The script's bars: the first's left, the room between two lefts, the tallest (script pixels).</summary>
    private const int FirstBar = 72, BarPitch = 32, Tallest = 300;

    /// <summary>A bar's share of its slot; the room left at the plot's top and foot.</summary>
    private const float BarShare = 0.62f, PlotMargin = 4;

    /// <summary>The column of the bar whose left is <paramref name="barX"/> on the screen (script pixels): centred in its slot, one of sixteen across the plot.</summary>
    public static (float X, float W) Column(PhoneFrame f, int barX)
    {
        var plot = Plot(f);
        float slot = plot.W / 16, w = slot * BarShare;
        return (plot.X + slot * ((barX - FirstBar) / (float)BarPitch + 0.5f) - w / 2, w);
    }

    /// <summary>A slot's width: the room a bar's figure is fitted to.</summary>
    public static float Slot(PhoneFrame f) => Plot(f).W / 16;

    /// <summary>The line <paramref name="height"/> script pixels over the plot's foot (the tallest bar, 300, reaches the plot's top margin).</summary>
    public static float Level(PhoneFrame f, int height)
    {
        var plot = Plot(f);
        float foot = plot.Bottom - f.Px(PlotMargin);
        return foot - height * Math.Max(0, plot.H - f.Px(2 * PlotMargin)) / Tallest;
    }

    /// <summary>A bar's figure: its type (2.3 mm), its baseline over the bar's top, the room it keeps from the slot's sides.</summary>
    public const float FigureText = 40, FigureOver = 10, FigureRoom = 4;

    /// <summary>The figure of the bar whose left is <paramref name="barX"/> and height <paramref name="height"/>: its middle and baseline.</summary>
    public static (float X, float Baseline) Figure(PhoneFrame f, int barX, int height)
    {
        var (x, w) = Column(f, barX);
        return (x + w / 2, Level(f, height) - f.Px(FigureOver));
    }

    /// <summary>"OV" inside a cut bar's top: its type and its middle under the top.</summary>
    public const float OverText = 28, OverDown = 22;

    // ------------------------------------------------------------------ the bar (end_alone)

    /// <summary>Back's width at the bar's right; its type (the brush face).</summary>
    private const float BackWidth = 420;

    /// <summary>Back's type.</summary>
    public const float BackText = 52;

    /// <summary>Back (the menu's Return, a graph's left click) at the bar's right thumb.</summary>
    public static PhoneFrame.Box Back(PhoneFrame f) =>
        new(f.Bar.Right - Math.Min(f.Bar.W, f.Px(BackWidth)), f.Bar.Y, Math.Min(f.Bar.W, f.Px(BackWidth)), f.Bar.H);
}
