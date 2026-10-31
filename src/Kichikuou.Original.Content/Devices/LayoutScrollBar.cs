namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The reference scroll bar (<see cref="PhoneScrollBar"/>, the owner's S2) on the 1280×800 layout of the PC and the tablet
/// (owner, 2026-10-31: round T's T2, docs/ui/touch.md, "Round T: the reference scroll bar"): a groove <see cref="Width"/>
/// layout pixels wide (1.6 mm on the owner's tablet, the phone's 1.1 mm as the eye sees it at the tablet's longer reach),
/// the thumb <see cref="ThumbIn"/> in, the list it scrolls <see cref="Gap"/> left of it; its rim one pixel of the window
/// (the drawing's to work out). The figures are the mock-up's (<c>tools/ui/mock_scroll_bar_tablet.py</c>) in layout pixels.
/// Interface only. Engine-independent.
/// </summary>
public static class LayoutScrollBar
{
    /// <summary>The groove's width; the gap between it and the list; the thumb's distance in from the groove's sides and ends; its least length.</summary>
    public const float Width = 8, Gap = 4, ThumbIn = 1, ThumbLeast = 24;

    /// <summary>The groove with its right at <paramref name="right"/>, from <paramref name="top"/> down <paramref name="height"/>.</summary>
    public static PhoneFrame.Box Groove(float right, float top, float height) => new(right - Width, top, Width, height);

    /// <summary>
    /// The thumb in <paramref name="groove"/>: as long as the share <paramref name="shown"/> of the list in view (at least
    /// <see cref="ThumbLeast"/>), at <paramref name="at"/> of its way (0 the list's top, 1 its end).
    /// </summary>
    public static PhoneFrame.Box Thumb(PhoneFrame.Box groove, float shown, float at) =>
        PhoneScrollBar.ThumbOf(groove, ThumbIn, ThumbLeast, shown, at);
}
