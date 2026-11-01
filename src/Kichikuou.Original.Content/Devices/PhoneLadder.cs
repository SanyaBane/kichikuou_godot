namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The dungeon's menus on the phone held landscape (docs/ui/phone-build.md, step L8; docs/ui/phone.md, §16: LF2 and LF2e,
/// the owner's choice): the tablet's rail kept in the frame, at its right foot beside window 113, at the phone's sizes — its
/// rungs 8.5 × 20.3 mm, their words in one line, its panel growing upwards; the right side stays empty. On a floor
/// (<see cref="Way"/>, <see cref="Floor"/>, <see cref="Retreat"/>): Descend in the corner, the floor over it, Ascend,
/// Retreat on top. Before a search (<see cref="Start"/>, <see cref="Other"/>, <see cref="Return"/>): Start Search in
/// Descend's place, the dungeon's other items (Merim, God's Door) over it, Return on top. The figures are the mock-up's
/// (<c>tools/ui/mock_phone.py</c>, <c>lf2_rail</c>, <c>floor_rail</c>, <c>ladder_rung</c>) in the reference phone's pixels,
/// the boxes in the canvas's units. Interface only. Engine-independent.
/// </summary>
public static class PhoneLadder
{
    /// <summary>The rail: in from the frame's right, as wide; how far over the gesture zone it ends.</summary>
    private const float RailIn = 16, RailWidth = 356, FootOver = 10;

    /// <summary>A rung's height, the gap between two, the floor's block between Ascend and Descend.</summary>
    private const float RungHeight = 150, Gap = 14, FloorHeight = 110;

    /// <summary>The panel's margin round the rail.</summary>
    private const float PanelMargin = 14;

    private static float X(PhoneFrame f) => f.Frame.Right - f.Px(RailIn + RailWidth);

    private static float Foot(PhoneFrame f) => f.Screen.H - f.Safe.Bottom - f.Px(FootOver);

    private static PhoneFrame.Box Rung(PhoneFrame f, float top, float height = RungHeight) =>
        new(X(f), top, f.Px(RailWidth), f.Px(height));

    // ------------------------------------------------------------------ on a floor (lf2, floor_rail)

    /// <summary>A way's rung: Descend (<paramref name="down"/>) in the corner, Ascend over the floor.</summary>
    public static PhoneFrame.Box Way(PhoneFrame f, bool down) =>
        down ? Rung(f, Foot(f) - f.Px(RungHeight)) : Rung(f, Floor(f).Y - f.Px(Gap + RungHeight));

    /// <summary>The floor's block between the ways: "FLOOR" over the floor's number.</summary>
    public static PhoneFrame.Box Floor(PhoneFrame f) => Rung(f, Way(f, down: true).Y - f.Px(Gap + FloorHeight), FloorHeight);

    /// <summary>Retreat, the red button on top.</summary>
    public static PhoneFrame.Box Retreat(PhoneFrame f) => Rung(f, Way(f, down: false).Y - f.Px(Gap + RungHeight));

    /// <summary>The ladder's panel round its rungs.</summary>
    public static PhoneFrame.Box LadderPanel(PhoneFrame f) => Panel(f, Retreat(f).Y);

    // ------------------------------------------------------------------ before a search (lf2e)

    /// <summary>Start Search in Descend's place.</summary>
    public static PhoneFrame.Box Start(PhoneFrame f) => Way(f, down: true);

    /// <summary>The dungeon's other item <paramref name="index"/> of <paramref name="count"/> over Start Search, the first highest.</summary>
    public static PhoneFrame.Box Other(PhoneFrame f, int index, int count) =>
        Rung(f, Start(f).Y - (count - index) * f.Px(RungHeight + Gap));

    /// <summary>Return on top, over the other items.</summary>
    public static PhoneFrame.Box Return(PhoneFrame f, int others) => Rung(f, Start(f).Y - (others + 1) * f.Px(RungHeight + Gap));

    /// <summary>The menu's panel round its rows.</summary>
    public static PhoneFrame.Box EntryPanel(PhoneFrame f, int others) => Panel(f, Return(f, others).Y);

    private static PhoneFrame.Box Panel(PhoneFrame f, float top)
    {
        float m = f.Px(PanelMargin);
        return new PhoneFrame.Box(X(f) - m, top - m, f.Px(RailWidth) + 2 * m, Foot(f) - top + 2 * m);
    }

    // ------------------------------------------------------------------ the rungs' insides (ladder_rung)

    /// <summary>
    /// A rung's insides, reference pixels: the arrow's middle in from the left and its half size; the word's left, its middle
    /// over the rung's and its type (shrunk to the rung less <see cref="WordRoom"/>); the sub-line's left, its middle under the
    /// rung's and its type.
    /// </summary>
    public const float ArrowIn = 50, ArrowSize = 20, WordIn = 96, WordUp = 20, WordText = 46, WordRoom = 110, SubIn = 98,
        SubDown = 30, SubText = 28;

    /// <summary>Retreat's word (the brush face).</summary>
    public const float RetreatText = 52;

    /// <summary>The floor's block: "FLOOR"'s middle under its top and type (Cinzel), the number's middle and type (the brush face).</summary>
    public const float FloorLabelMiddle = 26, FloorLabelText = 24, FloorMiddle = 74, FloorText = 56;
}
