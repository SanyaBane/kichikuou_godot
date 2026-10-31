namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The reference scroll bar of our phone screens (owner, 2026-10-31: round P31's S2, docs/ui/phone.md, §40-§41): a groove
/// <see cref="Width"/> wide (1.1 mm on the reference phone) with a dim gold rim, the thumb in gold inside it
/// <see cref="ThumbIn"/> in, as long as the share of the list in view; the list it scrolls stands <see cref="Gap"/> left of
/// it. A sign only: the finger scrolls the list anywhere. The figures are the mock-up's (<c>tools/ui/mock_scroll_bar.py</c>)
/// in the reference phone's pixels; the boxes in the canvas's units. Interface only. Engine-independent.
/// </summary>
public static class PhoneScrollBar
{
    /// <summary>The groove's width; the gap between it and the list; the thumb's distance in from the groove's sides and ends; its least length; the rim's width.</summary>
    public const float Width = 20, Gap = 10, ThumbIn = 3, ThumbLeast = 60, Rim = 2;

    /// <summary>The width a list that scrolls leaves at its right: the bar and the gap.</summary>
    public static float Room(PhoneFrame f) => f.Px(Width + Gap);

    /// <summary>The groove with its right at <paramref name="right"/>, from <paramref name="top"/> down <paramref name="height"/>.</summary>
    public static PhoneFrame.Box Groove(PhoneFrame f, float right, float top, float height) =>
        new(right - f.Px(Width), top, f.Px(Width), height);

    /// <summary>
    /// The thumb in <paramref name="groove"/>: as long as the share <paramref name="shown"/> of the list in view (at least
    /// <see cref="ThumbLeast"/>), at <paramref name="at"/> of its way (0 the list's top, 1 its end).
    /// </summary>
    public static PhoneFrame.Box Thumb(PhoneFrame f, PhoneFrame.Box groove, float shown, float at) =>
        ThumbOf(groove, f.Px(ThumbIn), f.Px(ThumbLeast), shown, at);

    /// <summary>The thumb in an upright <paramref name="groove"/>, <paramref name="inset"/> in from it, at least <paramref name="least"/> long.</summary>
    internal static PhoneFrame.Box ThumbOf(PhoneFrame.Box groove, float inset, float least, float shown, float at)
    {
        float room = Math.Max(0, groove.H - 2 * inset);
        float h = Math.Min(room, Math.Max(least, room * Math.Clamp(shown, 0, 1)));
        return new PhoneFrame.Box(groove.X + inset, groove.Y + inset + (room - h) * Math.Clamp(at, 0, 1), groove.W - 2 * inset, h);
    }
}
