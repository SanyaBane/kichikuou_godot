namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The phone held landscape (docs/ui/phone.md, §12; docs/ui/phone-build.md, §3): the engine's 1280×800 picture — and our
/// layouts in it — fitted to the screen's height and centred, the black sides left and right of it ours to draw in. A
/// phone layout is written in the reference phone's pixels (the Xiaomi Poco X6 Pro, <see cref="ReferenceLong"/> ×
/// <see cref="ReferenceShort"/> held landscape, 17.6 a millimetre: phone.md's and the mock-ups' figures), which
/// <see cref="Unit"/> turns into the canvas's; landscape scales by the screen's height, the sides' width flexes (a
/// longer screen has wider ones). The device's edges are kept free: the camera's hole on both short sides (either may
/// hold it, as the phone is turned) and the gesture bar's zone at the foot. Every figure is in the canvas's units, the
/// screen's top left corner at 0. Interface only. Engine-independent. The upright frame comes with step P0.
/// </summary>
public sealed record PhoneFrame
{
    /// <summary>A rectangle in the canvas's units.</summary>
    public readonly record struct Box(float X, float Y, float W, float H)
    {
        /// <summary>The right edge.</summary>
        public float Right => X + W;

        /// <summary>The bottom edge.</summary>
        public float Bottom => Y + H;

        /// <summary>Whether (<paramref name="x"/>, <paramref name="y"/>) is in the box.</summary>
        public bool Contains(float x, float y) => x >= X && x < Right && y >= Y && y < Bottom;
    }

    /// <summary>How much of each edge is kept free, in the canvas's units.</summary>
    public readonly record struct Insets(float Left, float Top, float Right, float Bottom);

    /// <summary>The reference phone's screen, in its own pixels.</summary>
    public const int ReferenceLong = 2712, ReferenceShort = 1220;

    /// <summary>The camera's hole: nothing to read or tap within this many reference pixels (6.3 mm) of a short side.</summary>
    public const int Hole = 110;

    /// <summary>The gesture bar's zone at the foot, where a swipe up is the system's: no button in it (3 mm).</summary>
    public const int GestureZone = 50;

    /// <summary>The layout's own size: the engine's picture at 2×.</summary>
    private const float LayoutWidth = 1280, LayoutHeight = 800;

    private PhoneFrame(float unit, Box screen, Box frame, Insets safe)
    {
        Unit = unit;
        Screen = screen;
        Frame = frame;
        Safe = safe;
    }

    /// <summary>The canvas's units a reference pixel.</summary>
    public float Unit { get; }

    /// <summary>The whole screen.</summary>
    public Box Screen { get; }

    /// <summary>The engine's picture and the 1280×800 layout over it.</summary>
    public Box Frame { get; }

    /// <summary>The canvas's units a layout pixel (of the 1280×800 frame).</summary>
    public float Scale => Frame.W / LayoutWidth;

    /// <summary>The edges kept free: the camera's hole on both sides, the gesture zone at the foot.</summary>
    public Insets Safe { get; }

    /// <summary>The black side left of the frame, whole.</summary>
    public Box LeftSide => new(0, 0, Frame.X, Screen.H);

    /// <summary>The black side right of the frame, whole.</summary>
    public Box RightSide => new(Frame.Right, 0, Screen.W - Frame.Right, Screen.H);

    /// <summary>What our controls may take of the left side: from the hole's strip to the frame, above the gesture zone.</summary>
    public Box LeftRoom => Room(Safe.Left, Frame.X);

    /// <summary>What our controls may take of the right side, as <see cref="LeftRoom"/>.</summary>
    public Box RightRoom => Room(Frame.Right, Screen.W - Safe.Right);

    /// <summary>A length of <paramref name="reference"/> reference pixels in the canvas's units.</summary>
    public float Px(float reference) => reference * Unit;

    /// <summary>
    /// The frame of a screen of <paramref name="width"/> × <paramref name="height"/> held landscape. Its safe insets are
    /// <paramref name="reported"/>, the system's (the cutout's and the bars'), else the reference phone's (a PC playing a
    /// phone): the larger side's on both sides, so nothing moves when the phone is turned round, and at least the gesture
    /// zone at the foot, which the system's safe area does not count. The frame fills the height, unless the screen is too
    /// short a one for it between the hole's strips: then it fills the width between them, centred.
    /// </summary>
    public static PhoneFrame Landscape(float width, float height, Insets? reported = null)
    {
        float unit = height / ReferenceShort;
        float side = reported is { } r ? Math.Max(r.Left, r.Right) : Hole * unit;
        var safe = new Insets(side, reported?.Top ?? 0, side, Math.Max(reported?.Bottom ?? 0, GestureZone * unit));
        float scale = Math.Min(height / LayoutHeight, Math.Max(0, width - 2 * side) / LayoutWidth);
        float w = LayoutWidth * scale, h = LayoutHeight * scale;
        return new PhoneFrame(unit, new Box(0, 0, width, height), new Box((width - w) / 2, (height - h) / 2, w, h), safe);
    }

    private Box Room(float left, float right) =>
        new(left, Safe.Top, Math.Max(0, right - left), Math.Max(0, Screen.H - Safe.Bottom - Safe.Top));
}
