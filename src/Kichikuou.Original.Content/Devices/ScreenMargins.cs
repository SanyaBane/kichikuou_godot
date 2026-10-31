using System.Globalization;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The player's margins from the screen's edges on a touch device (docs/ui/screen-margins.md; owner, 2026-10-31): a
/// screen's rounded corners and a film whose edges are not clear hide a strip that the system's safe area does not report,
/// so every screen of ours is laid out inside these as if the screen were that much smaller. Each way the device is held
/// keeps its own set: the top, the bottom and the sides (left and right alike: landscape the phone may be turned round,
/// and a phone is alike left and right upright), in tenths of a millimetre from 0 to <see cref="Max"/>. Interface only.
/// Engine-independent.
/// </summary>
public readonly record struct ScreenMargins(int Top, int Bottom, int Sides)
{
    /// <summary>An edge the player sets.</summary>
    public enum Edge
    {
        /// <summary>The screen's top as it is held.</summary>
        Top,
        /// <summary>Its foot.</summary>
        Bottom,
        /// <summary>Its left and right sides alike.</summary>
        Sides,
    }

    /// <summary>The edges in the order the calibration lists them.</summary>
    public static readonly Edge[] Edges = [Edge.Top, Edge.Bottom, Edge.Sides];

    /// <summary>The widest margin, in tenths of a millimetre (8 mm).</summary>
    public const int Max = 80;

    /// <summary>No margins: the screen as the system's safe area leaves it.</summary>
    public static readonly ScreenMargins None = default;

    /// <summary><paramref name="edge"/>'s margin, in tenths of a millimetre.</summary>
    public int this[Edge edge] => edge switch { Edge.Top => Top, Edge.Bottom => Bottom, _ => Sides };

    /// <summary>These margins with <paramref name="edge"/>'s at <paramref name="tenths"/>, held within 0 and <see cref="Max"/>.</summary>
    public ScreenMargins With(Edge edge, int tenths)
    {
        int v = Math.Clamp(tenths, 0, Max);
        return edge switch { Edge.Top => this with { Top = v }, Edge.Bottom => this with { Bottom = v }, _ => this with { Sides = v } };
    }

    /// <summary>These margins with <paramref name="edge"/>'s moved <paramref name="steps"/> tenths of a millimetre.</summary>
    public ScreenMargins Stepped(Edge edge, int steps) => With(edge, this[edge] + steps);

    /// <summary>Each held within 0 and <see cref="Max"/> (a value read from the settings file).</summary>
    public ScreenMargins Clamped() => new(Math.Clamp(Top, 0, Max), Math.Clamp(Bottom, 0, Max), Math.Clamp(Sides, 0, Max));

    /// <summary>The edge's name as the calibration and Option → Display show it.</summary>
    public static string Name(Edge edge) => edge switch { Edge.Top => "Top", Edge.Bottom => "Bottom", _ => "Sides" };

    /// <summary><paramref name="tenths"/> of a millimetre as the player reads them: "2.8 mm".</summary>
    public static string Millimetres(int tenths) => (tenths / 10.0).ToString("0.0", CultureInfo.InvariantCulture) + " mm";

    /// <summary>
    /// The set in words for Option → Display: "none", "2.8 mm all round", or each edge ("top 1.5 · bottom 2.8 · sides 0.0 mm").
    /// </summary>
    public string Words
    {
        get
        {
            if (this == None)
                return "none";
            if (Top == Bottom && Bottom == Sides)
                return Millimetres(Top) + " all round";
            static string N(int t) => (t / 10.0).ToString("0.0", CultureInfo.InvariantCulture);
            return $"top {N(Top)} · bottom {N(Bottom)} · sides {N(Sides)} mm";
        }
    }

    /// <summary>The margins as insets, at <paramref name="perMillimetre"/> units a millimetre.</summary>
    public PhoneFrame.Insets Insets(float perMillimetre)
    {
        float k = perMillimetre / 10;
        return new PhoneFrame.Insets(Sides * k, Top * k, Sides * k, Bottom * k);
    }
}
