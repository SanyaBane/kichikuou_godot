namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The screen margins' calibration (docs/ui/screen-margins.md, the owner's C): over the whole screen, the screen it was
/// opened from laid out live inside the margins under it — the margins a gold <see cref="Frame"/> with thick corner marks,
/// a <see cref="Tab"/> hanging inward from each edge to drag it, and a panel in the frame's middle with − / + for each
/// edge (<see cref="Minus"/>, <see cref="Plus"/>), Reset | Done. The tabs are taken at their inner end, 9 mm in, out of
/// Android's back gesture at the sides and its home gesture at the foot, and an edge follows the finger's move, not the
/// finger (<see cref="Dragged"/>); the tab on the camera's edge stands at a third of it, off the hole (upright the top,
/// landscape both sides, as either may hold it). Sized by the millimetre, so it is the same size on a phone and on a
/// tablet: the figures are the reference phone's pixels (<see cref="PhoneFrame"/>, 17.6 a millimetre) and the mock-up's
/// (<c>tools/ui/mock_screen_margins.py</c>, C-up, C-land), the boxes in the canvas's units with the screen's top left
/// corner at 0. Interface only. Engine-independent.
/// </summary>
public sealed class MarginsCalibration
{
    /// <summary>A side of the screen, whose tab moves its edge's margin (left and right both the sides').</summary>
    public enum Side
    {
        /// <summary>The top's tab, hanging down.</summary>
        Top,
        /// <summary>The foot's, rising.</summary>
        Bottom,
        /// <summary>The left side's, reaching right.</summary>
        Left,
        /// <summary>The right side's, reaching left.</summary>
        Right,
    }

    /// <summary>What a press lands on.</summary>
    public enum Kind
    {
        /// <summary>Nothing: the panel itself or the screen under the frame.</summary>
        None,
        /// <summary>A tab, to drag its edge.</summary>
        Tab,
        /// <summary>An edge's −.</summary>
        Minus,
        /// <summary>An edge's +.</summary>
        Plus,
        /// <summary>Reset: every edge to none.</summary>
        Reset,
        /// <summary>Done: back to the options.</summary>
        Done,
    }

    /// <summary>What a press lands on: a tab (<see cref="Side"/>), an edge's − or + (<see cref="Edge"/>), Reset, Done or nothing.</summary>
    public readonly record struct Target(Kind Kind, ScreenMargins.Edge Edge = default, Side Side = default);

    /// <summary>The reference phone's pixels a millimetre (446 ppi).</summary>
    public const float ReferencePerMillimetre = 446 / 25.4f;

    /// <summary>The frame's gold line, the corner marks' width and arms.</summary>
    public const float FrameLine = 5, MarkWidth = 12, MarkArm = 110;

    /// <summary>A tab's reach in from its edge and its thickness, in millimetres; where its ridges stand along its reach.</summary>
    public const float TabReach = 9, TabThick = 7, RidgesAt = 0.62f;

    /// <summary>Where along the camera's edge its tab stands (a third of the way, off the hole in its middle).</summary>
    public const float CameraTabAt = 0.3f;

    /// <summary>Where along the sides the side tabs stand upright: a quarter of the way down, over the panel however wide the margins.</summary>
    public const float UprightSideTabAt = 0.25f;

    /// <summary>How dark the screen under the frame is kept (the mock-up's 55%).</summary>
    public const float Dim = 0.55f;

    /// <summary>The title ("Screen margins", Cinzel), the orientation's name, the steppers' labels and values, the buttons' words.</summary>
    public const float TitleText = 52, OrientationText = 40, LabelText = 44, ValueText = 48, ResetText = 36, DoneText = 44;

    // The panel upright (C-up): its size; the title's middle and the orientation's from its top; the steppers' rows from
    // its top, their height and pitch; the sides in; a value's width between its buttons; Reset | Done from its foot, as
    // high, Reset as wide, the gap.
    private const float UprightWidth = 800, UprightHeight = 820, UprightTitle = 70, UprightOrientation = 140,
        UprightRows = 200, Row = 112, UprightPitch = 138, PanelIn = 50, ValueWidth = 230,
        UprightButtonsUp = 150, UprightButton = 112, UprightReset = 300, ButtonsGap = 20;

    // The panel landscape (C-land): its size; the title's middle from its top; the steppers' row from its top, their
    // labels' baseline over it, the gap between two; Reset | Done from its foot, as high, Reset as wide, Done as wide.
    private const float LandWidth = 1500, LandHeight = 600, LandTitle = 70, LandRows = 200, LabelOver = 14,
        LandColumnsGap = 70, LandIn = 60, LandButtonsUp = 60, LandButton = 130, LandReset = 220, LandDone = 460;

    private readonly PhoneFrame.Insets _margins;

    private MarginsCalibration(float width, float height, PhoneFrame.Insets margins, float perMillimetre, bool upright)
    {
        Screen = new PhoneFrame.Box(0, 0, width, height);
        _margins = margins;
        PerMillimetre = perMillimetre;
        Unit = perMillimetre / ReferencePerMillimetre;
        IsUpright = upright;
    }

    /// <summary>
    /// The calibration over a screen of <paramref name="width"/> × <paramref name="height"/> whose <paramref name="margins"/>
    /// are as given (the canvas's units), at <paramref name="perMillimetre"/> canvas units a millimetre, held
    /// <paramref name="upright"/> or landscape.
    /// </summary>
    public static MarginsCalibration For(float width, float height, PhoneFrame.Insets margins, float perMillimetre, bool upright) =>
        new(width, height, margins, perMillimetre, upright);

    /// <summary>The whole screen.</summary>
    public PhoneFrame.Box Screen { get; }

    /// <summary>The canvas's units a millimetre.</summary>
    public float PerMillimetre { get; }

    /// <summary>The canvas's units a reference pixel.</summary>
    public float Unit { get; }

    /// <summary>Whether the device is held upright (the panel's layout, the tabs' places).</summary>
    public bool IsUpright { get; }

    /// <summary>A length of <paramref name="reference"/> reference pixels in the canvas's units.</summary>
    public float Px(float reference) => reference * Unit;

    /// <summary>The screen less the margins: what our screens are laid out in, the gold frame's line round it.</summary>
    public PhoneFrame.Box Frame => new(_margins.Left, _margins.Top, Math.Max(0, Screen.W - _margins.Left - _margins.Right),
        Math.Max(0, Screen.H - _margins.Top - _margins.Bottom));

    /// <summary>The edge whose margin <paramref name="side"/>'s tab moves.</summary>
    public static ScreenMargins.Edge EdgeOf(Side side) => side switch
    {
        Side.Top => ScreenMargins.Edge.Top,
        Side.Bottom => ScreenMargins.Edge.Bottom,
        _ => ScreenMargins.Edge.Sides,
    };

    /// <summary>
    /// <paramref name="side"/>'s tab: from the frame's edge inward by <see cref="TabReach"/>, <see cref="TabThick"/> across,
    /// in the edge's middle, at <see cref="CameraTabAt"/> of it on the camera's edge, at <see cref="UprightSideTabAt"/> of the
    /// sides upright.
    /// </summary>
    public PhoneFrame.Box Tab(Side side)
    {
        var f = Frame;
        float reach = TabReach * PerMillimetre, thick = TabThick * PerMillimetre;
        float alongX = f.X + f.W * (side == Side.Top && IsUpright ? CameraTabAt : 0.5f);
        float alongY = f.Y + f.H * (IsUpright ? UprightSideTabAt : CameraTabAt);
        return side switch
        {
            Side.Top => new(alongX - thick / 2, f.Y, thick, reach),
            Side.Bottom => new(alongX - thick / 2, f.Bottom - reach, thick, reach),
            Side.Left => new(f.X, alongY - thick / 2, reach, thick),
            _ => new(f.Right - reach, alongY - thick / 2, reach, thick),
        };
    }

    /// <summary>Where <paramref name="side"/>'s tab has its ridges, the finger's place: <see cref="RidgesAt"/> of its reach in.</summary>
    public (float X, float Y) Ridges(Side side)
    {
        var t = Tab(side);
        return side switch
        {
            Side.Top => (t.X + t.W / 2, t.Y + t.H * RidgesAt),
            Side.Bottom => (t.X + t.W / 2, t.Bottom - t.H * RidgesAt),
            Side.Left => (t.X + t.W * RidgesAt, t.Y + t.H / 2),
            _ => (t.Right - t.W * RidgesAt, t.Y + t.H / 2),
        };
    }

    /// <summary>The panel in the frame's middle.</summary>
    public PhoneFrame.Box Panel
    {
        get
        {
            var f = Frame;
            float w = Px(IsUpright ? UprightWidth : LandWidth), h = Px(IsUpright ? UprightHeight : LandHeight);
            return new(f.X + (f.W - w) / 2, f.Y + (f.H - h) / 2, w, h);
        }
    }

    /// <summary>The title's place: upright its middle (centred), landscape its left end's middle.</summary>
    public (float X, float Y) Title => IsUpright
        ? (Panel.X + Panel.W / 2, Panel.Y + Px(UprightTitle))
        : (Panel.X + Px(LandIn), Panel.Y + Px(LandTitle));

    /// <summary>The orientation's name ("Portrait", "Landscape"): upright its middle under the title, landscape its right end's middle.</summary>
    public (float X, float Y) Orientation => IsUpright
        ? (Panel.X + Panel.W / 2, Panel.Y + Px(UprightOrientation))
        : (Panel.Right - Px(LandIn), Panel.Y + Px(LandTitle));

    /// <summary>The orientation's name for the panel.</summary>
    public string OrientationName => IsUpright ? "Portrait" : "Landscape";

    /// <summary>
    /// <paramref name="edge"/>'s row: upright the panel's width less its sides, one under another; landscape a column of
    /// three across, the label over it.
    /// </summary>
    public PhoneFrame.Box StepperRow(ScreenMargins.Edge edge)
    {
        int i = Array.IndexOf(ScreenMargins.Edges, edge);
        var p = Panel;
        if (IsUpright)
            return new(p.X + Px(PanelIn), p.Y + Px(UprightRows + i * UprightPitch), p.W - 2 * Px(PanelIn), Px(Row));
        float w = (p.W - 2 * Px(LandIn) - 2 * Px(LandColumnsGap)) / 3;
        return new(p.X + Px(LandIn) + i * (w + Px(LandColumnsGap)), p.Y + Px(LandRows), w, Px(Row));
    }

    /// <summary>
    /// <paramref name="edge"/>'s label: upright its left end's middle in the row, landscape its middle's baseline over the
    /// column.
    /// </summary>
    public (float X, float Y) Label(ScreenMargins.Edge edge)
    {
        var r = StepperRow(edge);
        return IsUpright ? (r.X, r.Y + r.H / 2) : (r.X + r.W / 2, r.Y - Px(LabelOver));
    }

    /// <summary><paramref name="edge"/>'s − : upright left of its value at the row's right, landscape at the column's left.</summary>
    public PhoneFrame.Box Minus(ScreenMargins.Edge edge)
    {
        var r = StepperRow(edge);
        return IsUpright ? new(r.Right - 2 * r.H - Px(ValueWidth), r.Y, r.H, r.H) : new(r.X, r.Y, r.H, r.H);
    }

    /// <summary><paramref name="edge"/>'s + at the row's right.</summary>
    public PhoneFrame.Box Plus(ScreenMargins.Edge edge)
    {
        var r = StepperRow(edge);
        return new(r.Right - r.H, r.Y, r.H, r.H);
    }

    /// <summary><paramref name="edge"/>'s value: between its − and +.</summary>
    public PhoneFrame.Box Value(ScreenMargins.Edge edge)
    {
        var minus = Minus(edge);
        var plus = Plus(edge);
        return new(minus.Right, minus.Y, plus.X - minus.Right, minus.H);
    }

    /// <summary>Reset (a lacquer button): upright at the panel's foot's left, landscape left of Done.</summary>
    public PhoneFrame.Box Reset
    {
        get
        {
            var p = Panel;
            if (IsUpright)
                return new(p.X + Px(PanelIn), p.Bottom - Px(UprightButtonsUp), Px(UprightReset), Px(UprightButton));
            var done = Done;
            return new(done.X - Px(ButtonsGap) - Px(LandReset), done.Y, Px(LandReset), done.H);
        }
    }

    /// <summary>Done (the red button): upright right of Reset to the panel's side, landscape at the panel's foot's right.</summary>
    public PhoneFrame.Box Done
    {
        get
        {
            var p = Panel;
            if (!IsUpright)
                return new(p.Right - Px(LandIn) - Px(LandDone), p.Bottom - Px(LandButtonsUp) - Px(LandButton), Px(LandDone), Px(LandButton));
            float x = p.X + Px(PanelIn) + Px(UprightReset) + Px(ButtonsGap);
            return new(x, p.Bottom - Px(UprightButtonsUp), p.Right - Px(PanelIn) - x, Px(UprightButton));
        }
    }

    /// <summary>What a press at (<paramref name="x"/>, <paramref name="y"/>) lands on: the panel's buttons first, then the tabs.</summary>
    public Target Hit(float x, float y)
    {
        foreach (var edge in ScreenMargins.Edges)
        {
            if (Minus(edge).Contains(x, y))
                return new(Kind.Minus, edge);
            if (Plus(edge).Contains(x, y))
                return new(Kind.Plus, edge);
        }
        if (Reset.Contains(x, y))
            return new(Kind.Reset);
        if (Done.Contains(x, y))
            return new(Kind.Done);
        if (Panel.Contains(x, y))
            return default;
        foreach (var side in Enum.GetValues<Side>())
        {
            if (Tab(side).Contains(x, y))
                return new(Kind.Tab, EdgeOf(side), side);
        }
        return default;
    }

    /// <summary>
    /// The margins after <paramref name="side"/>'s tab, taken at <paramref name="start"/>, has moved
    /// (<paramref name="dx"/>, <paramref name="dy"/>) canvas units: its edge follows the move inward or outward, to the
    /// nearest tenth of a millimetre, within 0 and <see cref="ScreenMargins.Max"/>.
    /// </summary>
    public static ScreenMargins Dragged(ScreenMargins start, Side side, float dx, float dy, float perMillimetre)
    {
        float inward = side switch { Side.Top => dy, Side.Bottom => -dy, Side.Left => dx, _ => -dx };
        var edge = EdgeOf(side);
        return start.With(edge, start[edge] + (int)MathF.Round(inward / perMillimetre * 10));
    }
}
