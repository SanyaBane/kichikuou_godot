namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The dungeon's menus on the phone held upright (docs/ui/phone-build.md, step P8b; docs/ui/phone.md, §10: PF1 and PF1e, the
/// owner's choice): the tablet's rail at the right thumb standing on the bar's foot (<see cref="Way"/>, <see cref="Floor"/>,
/// <see cref="Retreat"/>: Descend in the corner, the floor over it, Ascend, Retreat on top; before a search
/// <see cref="Start"/> in Descend's place, the dungeon's other items over it (<see cref="Other"/>), <see cref="Return"/> on
/// top, the rail growing upwards), window 113's lines left of it (<see cref="Window"/>), the floor's picture — the
/// <see cref="Source"/> of the engine's layout — at the room's width over them (<see cref="Picture"/>). The figures are the
/// mock-ups' (<c>tools/ui/mock_phone.py</c>: <c>pf1</c>, <c>pf1e</c>, <c>pf_frame</c>, <c>floor_rail</c>,
/// <c>info_window</c>) in the reference phone's pixels from the room's sides and the bar; the boxes in the canvas's units.
/// The rungs' insides are held landscape's (<see cref="PhoneLadder"/>). Interface only. Engine-independent.
/// </summary>
public static class PhoneLadderPortrait
{
    /// <summary>The rail's distance from the room's right, its width.</summary>
    private const float RailIn = 16, RailWidth = 440;

    /// <summary>A rung's height, the gap between two, the floor's block between Ascend and Descend.</summary>
    private const float RungHeight = 150, Gap = 14, FloorHeight = 110;

    private static float X(PhoneFrame f) => f.Room.Right - f.Px(RailIn + RailWidth);

    /// <summary>The rail's foot: the bar's.</summary>
    private static float Foot(PhoneFrame f) => f.Bar.Bottom;

    private static PhoneFrame.Box Rung(PhoneFrame f, float top, float height = RungHeight) =>
        new(X(f), top, f.Px(RailWidth), f.Px(height));

    // ------------------------------------------------------------------ on a floor (pf1, floor_rail)

    /// <summary>A way's rung: Descend (<paramref name="down"/>) in the corner, Ascend over the floor.</summary>
    public static PhoneFrame.Box Way(PhoneFrame f, bool down) =>
        down ? Rung(f, Foot(f) - f.Px(RungHeight)) : Rung(f, Floor(f).Y - f.Px(Gap + RungHeight));

    /// <summary>The floor's block between the ways: "FLOOR" over the floor's number.</summary>
    public static PhoneFrame.Box Floor(PhoneFrame f) => Rung(f, Way(f, down: true).Y - f.Px(Gap + FloorHeight), FloorHeight);

    /// <summary>Retreat, the red button on top.</summary>
    public static PhoneFrame.Box Retreat(PhoneFrame f) => Rung(f, Way(f, down: false).Y - f.Px(Gap + RungHeight));

    // ------------------------------------------------------------------ before a search (pf1e)

    /// <summary>Start Search in Descend's place.</summary>
    public static PhoneFrame.Box Start(PhoneFrame f) => Way(f, down: true);

    /// <summary>The dungeon's other item <paramref name="index"/> of <paramref name="count"/> over Start Search, the first highest.</summary>
    public static PhoneFrame.Box Other(PhoneFrame f, int index, int count) =>
        Rung(f, Start(f).Y - (count - index) * f.Px(RungHeight + Gap));

    /// <summary>Return on top, over the other items.</summary>
    public static PhoneFrame.Box Return(PhoneFrame f, int others) => Rung(f, Start(f).Y - (others + 1) * f.Px(RungHeight + Gap));

    // ------------------------------------------------------------------ the rail's top, window 113 and the picture (pf_frame, info_window)

    /// <summary>The rail's top: Retreat's on a floor, Return's before a search (<paramref name="others"/> the other items).</summary>
    public static float Top(PhoneFrame f, bool entry, int others) => entry ? Return(f, others).Y : Retreat(f).Y;

    /// <summary>Window 113's distance from the room's left and from the rail.</summary>
    private const float WindowIn = 16;

    /// <summary>Window 113 left of the rail, from its top to its foot.</summary>
    public static PhoneFrame.Box Window(PhoneFrame f, bool entry, int others)
    {
        float x = f.Room.X + f.Px(WindowIn), top = Top(f, entry, others);
        return new PhoneFrame.Box(x, top, Math.Max(0, X(f) - f.Px(WindowIn) - x), Foot(f) - top);
    }

    /// <summary>Window 113's lines: their left in from the window's, the first's top under its top, their type (3.3 mm) and pitch.</summary>
    public const float LinesIn = 34, LinesTop = 30, LinesText = 58, LinesPitch = 58 * 1.2f;

    /// <summary>The floor's picture in the engine's 1280 × 800 layout: inside <c>WAO_SUB</c> 331's frame.</summary>
    public static readonly (int X, int Y, int W, int H) Source = (364, 204, 552, 328);

    /// <summary>The picture's distance from the room's sides and over the rail's top.</summary>
    private const float PictureIn = 16, PictureOver = 40;

    /// <summary>The floor's picture at the room's width less its margins, in its own shape, its foot over the rail's top.</summary>
    public static PhoneFrame.Box Picture(PhoneFrame f, bool entry, int others)
    {
        float w = Math.Max(0, f.Room.W - f.Px(2 * PictureIn)), h = w * Source.H / Source.W;
        return new PhoneFrame.Box(f.Room.X + f.Px(PictureIn), Top(f, entry, others) - f.Px(PictureOver) - h, w, h);
    }
}
