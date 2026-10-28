using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The device our screens lay themselves out for (docs/ui/phone-build.md, §3), read live: <see cref="Layout"/> follows
/// the window's shape, so a phone turned while the game runs lays every view out again. A phone is told from a tablet by
/// its screen's physical short side (<see cref="DeviceLayouts.IsPhone"/>); on a PC <c>--phone landscape</c> plays one
/// (the touch mode, a window of the reference phone's shape fitted to the monitor, <see cref="OpenPhoneWindow"/>).
/// <see cref="TouchMode"/> stays the input's switch (no hover, the long press); this is the layout's.
/// </summary>
internal static class Device
{
    private static bool? _phone;
    private static DeviceLayout _layout;
    private static PhoneFrame.Insets? _insets;
    private static Window? _root;

    /// <summary>The layout for the window as it is now.</summary>
    public static DeviceLayout Layout => _layout;

    /// <summary>
    /// Whether our views take the touch mode's layout, the tablet's: on the tablet, and on a phone wherever a screen has
    /// no phone layout of its own yet (each phone step gives its screens theirs).
    /// </summary>
    public static bool TouchLayout => _layout != DeviceLayout.Pc;

    /// <summary>Whether the device is a phone, held either way.</summary>
    public static bool Phone => _phone ??= OS.GetName() == "Android"
        ? DisplayServer.ScreenGetSize() is var screen && DeviceLayouts.IsPhone(Math.Min(screen.X, screen.Y), DisplayServer.ScreenGetDpi())
        : PhoneSwitch is not null;

    /// <summary><c>--phone landscape</c>'s orientation on a PC; null without the switch.</summary>
    private static string? PhoneSwitch
    {
        get
        {
            var args = OS.GetCmdlineUserArgs();
            int at = Array.IndexOf(args, "--phone");
            return at < 0 ? null : at + 1 < args.Length ? args[at + 1] : "";
        }
    }

    /// <summary>
    /// Starts following the main window: the layout and the safe area read again whenever its size changes (a phone
    /// turned, a window resized). Once a launch.
    /// </summary>
    public static void Follow(Window root)
    {
        if (_root is not null)
            return;
        _root = root;
        root.SizeChanged += Read;
        Read();
        GD.Print($"Kichikuou: device {_layout}{(OS.GetName() == "Android" ? $", screen {DisplayServer.ScreenGetSize()} at {DisplayServer.ScreenGetDpi()} dpi" : "")}.");
    }

    /// <summary>
    /// The phone's frame held landscape over a room of <paramref name="size"/> (the canvas's units, its corner at the
    /// screen's), with the system's safe area on Android and the reference phone's elsewhere.
    /// </summary>
    public static PhoneFrame Frame(Vector2 size) => PhoneFrame.Landscape(size.X, size.Y, _insets);

    /// <summary>
    /// The phone's frame held landscape over <paramref name="view"/>'s parent — the screen's room (the game's content, the
    /// gallery's stage), its corner at the parent's — for a view with a phone layout of its own; null on any other device.
    /// </summary>
    public static PhoneFrame? Landscape(Control view) => _layout == DeviceLayout.PhoneLandscape ? Frame(view.GetParentAreaSize()) : null;

    /// <summary>
    /// <c>--phone landscape</c> on a PC: the window at the reference phone's shape (2712 × 1220), as large as the monitor's
    /// usable area allows with the window's frame round it, at most the reference's size, centred. Upright comes with
    /// step P0. False when the switch asks for something else.
    /// </summary>
    public static bool OpenPhoneWindow()
    {
        if (OS.GetName() == "Android" || PhoneSwitch is not { } held || DisplayServer.GetName() == "headless")
            return true;
        if (held != "landscape")
        {
            GD.PushError($"Kichikuou: --phone takes landscape (upright comes with step P0), not \"{held}\".");
            return false;
        }
        var usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
        var room = usable.Size - new Vector2I(16, 48);                    // the window's border and title bar
        float scale = Math.Min(1, Math.Min(room.X / (float)PhoneFrame.ReferenceLong, room.Y / (float)PhoneFrame.ReferenceShort));
        var size = new Vector2I((int)(PhoneFrame.ReferenceLong * scale), (int)(PhoneFrame.ReferenceShort * scale));
        DisplayServer.WindowSetSize(size);
        DisplayServer.WindowSetPosition(usable.Position + (usable.Size - size) / 2);
        GD.Print($"Kichikuou: a phone held landscape, window {size.X}×{size.Y}.");
        return true;
    }

    private static void Read()
    {
        var window = DisplayServer.WindowGetSize();
        _layout = DeviceLayouts.Decide(TouchMode.On, Phone, window.X, window.Y);
        if (OS.GetName() != "Android" || window.X <= 0 || _root is null)
            return;
        _insets = SafeInsets(window, _root.GetVisibleRect().Size);
        GD.Print($"Kichikuou: {_layout}, window {window}, safe area {DisplayServer.GetDisplaySafeArea()}, insets {_insets}.");   // the Poco's own (phone-build.md, §7)
    }

    /// <summary>
    /// The system's safe area (the cutout's and the bars') as insets in the canvas's units: the window fills the screen
    /// on Android (full screen, immersive), and the canvas is the window scaled (<c>canvas_items</c>).
    /// </summary>
    private static PhoneFrame.Insets SafeInsets(Vector2I window, Vector2 canvas)
    {
        var safe = DisplayServer.GetDisplaySafeArea();
        float k = canvas.X / window.X;
        return new PhoneFrame.Insets(safe.Position.X * k, safe.Position.Y * k, (window.X - safe.End.X) * k, (window.Y - safe.End.Y) * k);
    }
}
