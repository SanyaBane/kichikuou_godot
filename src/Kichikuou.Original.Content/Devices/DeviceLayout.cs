namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The device our screens lay themselves out for (docs/ui/phone-build.md, §3): the PC (the mouse, the 1280×800 layout),
/// the tablet (the touch mode's layout in the frame), and the phone held landscape or upright (a layout of its own per
/// screen, in the reference phone's pixels, <see cref="PhoneFrame"/>). Interface only.
/// </summary>
public enum DeviceLayout
{
    /// <summary>The PC: the mouse, the 1280×800 layout, the menu bar.</summary>
    Pc,
    /// <summary>A tablet, or a PC with <c>--touch</c>: the touch mode's layout in the frame.</summary>
    Tablet,
    /// <summary>A phone held landscape: the frame at the screen's height, our controls in the black sides.</summary>
    PhoneLandscape,
    /// <summary>A phone held upright (step P0 on).</summary>
    PhoneUpright,
}

/// <summary>How the device is told (docs/ui/phone-build.md, §3, and its §6: no setting). Engine-independent.</summary>
public static class DeviceLayouts
{
    /// <summary>
    /// A touch screen whose short side is under this many millimetres is a phone: the Xiaomi Poco X6 Pro's is 69.5, the
    /// Galaxy Tab S7 FE's 167, far enough apart that Android's density buckets (the dpi it reports rounded to 240, 480…)
    /// never move one across.
    /// </summary>
    public const double PhoneShortSide = 90;

    /// <summary>Whether a screen whose short side is <paramref name="shortSide"/> pixels at <paramref name="dpi"/> is a phone's.</summary>
    public static bool IsPhone(int shortSide, double dpi) => dpi > 0 && shortSide * 25.4 / dpi < PhoneShortSide;

    /// <summary>
    /// The layout for a device that is a <paramref name="phone"/> or not, in the touch mode or not, whose window is
    /// <paramref name="width"/> × <paramref name="height"/>: a phone's by the window's shape (a square one is landscape),
    /// else the tablet's in the touch mode, else the PC's.
    /// </summary>
    public static DeviceLayout Decide(bool touch, bool phone, float width, float height) =>
        phone ? width >= height ? DeviceLayout.PhoneLandscape : DeviceLayout.PhoneUpright
        : touch ? DeviceLayout.Tablet : DeviceLayout.Pc;
}
