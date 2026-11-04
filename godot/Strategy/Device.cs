using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The device our screens lay themselves out for (docs/ui/phone-build.md, §3), read live: <see cref="Layout"/> follows
/// the window's shape, so a phone turned while the game runs lays every view out again. A phone is told from a tablet by
/// its screen's physical short side (<see cref="DeviceLayouts.IsPhone"/>); on a PC <c>--phone landscape</c> or
/// <c>--phone upright</c> plays one (the touch mode, a window of the reference phone's shape fitted to the monitor,
/// <see cref="OpenPhoneWindow"/>). A phone takes the orientation Option → Display chose (<see cref="OrientationChoice"/>).
/// <see cref="TouchMode"/> stays the input's switch (no hover, the long press); this is the layout's.
/// </summary>
internal static class Device
{
    private static bool? _phone;
    private static DeviceLayout _layout;
    private static PhoneFrame.Insets? _insets;
    private static PhoneFrame.Insets _margins;
    private static float _perMillimetre = 1;
    private static Window? _root;

    /// <summary>
    /// The reference tablet's screen, its short side in millimetres (the Galaxy Tab S7 FE's): a PC in the touch mode plays it
    /// when it turns millimetres into the canvas's units, as a PC with <c>--phone</c> plays the reference phone.
    /// </summary>
    private const float TabletShortSide = 167;

    /// <summary>
    /// The player's screen margins (docs/ui/screen-margins.md, <see cref="ScreenMarginsChoice"/>) in the canvas's units for
    /// the way the device is held now; none on the PC with the mouse. A screen is laid out in <see cref="RoomIn"/>.
    /// </summary>
    public static PhoneFrame.Insets Margins => _margins;

    /// <summary>
    /// The canvas's units a millimetre of the screen: on Android by its reported density, on a PC playing a phone the
    /// reference phone's, on a PC in the touch mode the reference tablet's.
    /// </summary>
    public static float PerMillimetre => _perMillimetre;

    /// <summary>Whether the device is held upright: a phone's (the tablet and the PC are landscape).</summary>
    public static bool HeldUpright => _layout == DeviceLayout.PhoneUpright;

    /// <summary>The room inside the <see cref="Margins"/> of a screen of <paramref name="size"/>, in its coordinates.</summary>
    public static Rect2 RoomIn(Vector2 size) => new(new Vector2(_margins.Left, _margins.Top),
        new Vector2(Math.Max(0, size.X - _margins.Left - _margins.Right), Math.Max(0, size.Y - _margins.Top - _margins.Bottom)));

    /// <summary>The layout for the window as it is now.</summary>
    public static DeviceLayout Layout => _layout;

    /// <summary>
    /// How many times the layout changed this launch (a phone turned, a window resized across its shape): a view that keeps
    /// a gesture's or a callout's place in pixels compares it with the count it last saw and lets go of the stale ones.
    /// </summary>
    public static int Turns { get; private set; }

    /// <summary>
    /// Whether our views take the touch mode's layout, the tablet's: on the tablet, and on a phone wherever a screen has
    /// no phone layout of its own yet (each phone step gives its screens theirs).
    /// </summary>
    public static bool TouchLayout => _layout != DeviceLayout.Pc;

    /// <summary>Whether the device is a phone, held either way.</summary>
    public static bool Phone => _phone ??= OS.GetName() == "Android"
        ? DisplayServer.ScreenGetSize() is var screen && DeviceLayouts.IsPhone(Math.Min(screen.X, screen.Y), DisplayServer.ScreenGetDpi())
        : PhoneSwitch is not null;

    /// <summary><c>--phone landscape</c>'s or <c>--phone upright</c>'s orientation on a PC; null without the switch.</summary>
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
        RenderingServer.SetDefaultClearColor(Colors.Black);    // outside the margins, under a screen laid out inside them
        root.SizeChanged += Read;
        ScreenMarginsChoice.Changed += MarginsChanged;
        Read();
        GD.Print($"Kichikuou: device {_layout}{(OS.GetName() == "Android" ? $", screen {DisplayServer.ScreenGetSize()} at {DisplayServer.ScreenGetDpi()} dpi" : "")}.");
        if (OS.GetName() == "Android" && Phone)
            OrientationChoice.Apply();
    }

    /// <summary>
    /// The phone's frame as it is held over a room of <paramref name="size"/> (the canvas's units, its corner at the
    /// screen's, or at the <see cref="Margins"/>' inside a screen laid out in them), with the system's safe area on Android
    /// and the reference phone's elsewhere, less what the margins keep free already; held upright the picture where
    /// <see cref="PictureTop"/> puts it.
    /// </summary>
    public static PhoneFrame Frame(Vector2 size)
    {
        var frame = PhoneFrame.For(size.X, size.Y, _insets, _margins,
            _layout is DeviceLayout.PhoneUpright or DeviceLayout.PhoneLandscape ? HeldUpright : null);
        return frame.IsUpright && PictureTop?.Invoke(frame) is float top ? frame.WithPictureAt(top) : frame;
    }

    /// <summary>
    /// Held upright, where the screen shown now stands the engine's picture (its top, the canvas's units, from the frame as
    /// it would be), when not in the room's middle: the title over its rows (PE2, <c>GameScreen.Title.cs</c>); null, or a
    /// null top, the middle.
    /// </summary>
    public static Func<PhoneFrame, float?>? PictureTop { get; set; }

    /// <summary>
    /// The phone's frame held landscape over <paramref name="view"/>'s parent — the screen's room (the game's content, the
    /// gallery's stage), its corner at the parent's — for a view with a phone layout of its own; null on any other device.
    /// </summary>
    public static PhoneFrame? Landscape(Control view) => _layout == DeviceLayout.PhoneLandscape ? Frame(view.GetParentAreaSize()) : null;

    /// <summary>The phone's frame held upright over <paramref name="view"/>'s parent, as <see cref="Landscape"/>; null on any other device.</summary>
    public static PhoneFrame? Upright(Control view) => _layout == DeviceLayout.PhoneUpright ? Frame(view.GetParentAreaSize()) : null;

    /// <summary>
    /// The phone's frame held either way over <paramref name="view"/>'s parent, as <see cref="Landscape"/>, for a view whose
    /// phone layout serves both (its model's figures follow <see cref="PhoneFrame.IsUpright"/>); null on any other device.
    /// </summary>
    public static PhoneFrame? Held(Control view) => Landscape(view) ?? Upright(view);

    /// <summary>
    /// The phone's frame held either way over a room of <paramref name="room"/> inside the <see cref="Margins"/> (a screen of
    /// its own at the root, <see cref="RoomIn"/>); null on any other device.
    /// </summary>
    public static PhoneFrame? HeldIn(Vector2 room) =>
        _layout is DeviceLayout.PhoneLandscape or DeviceLayout.PhoneUpright ? Frame(room) : null;

    /// <summary>
    /// <c>--phone landscape</c> or <c>--phone portrait</c> (also <c>upright</c>, its former name) on a PC: the window at the reference phone's shape held so
    /// (<see cref="TurnWindow"/>). False when the switch asks for something else.
    /// </summary>
    public static bool OpenPhoneWindow()
    {
        if (OS.GetName() == "Android" || PhoneSwitch is not { } held)
            return true;
        if (held is not ("landscape" or "portrait" or "upright"))
        {
            GD.PushError($"Kichikuou: --phone takes landscape or portrait, not \"{held}\".");
            return false;
        }
        TurnWindow(held is "portrait" or "upright");
        return true;
    }

    /// <summary>
    /// A PC playing a phone held <paramref name="upright"/> or landscape: the window at the reference phone's shape
    /// (1220 × 2712 or 2712 × 1220), as large as the monitor's usable area allows with the window's frame round it, at most
    /// the reference's size, centred; headless (no monitor) the canvas at half the reference's shape. The layout follows
    /// the new size (<see cref="Follow"/>), as a phone's turn.
    /// </summary>
    public static void TurnWindow(bool upright)
    {
        int wide = upright ? PhoneFrame.ReferenceShort : PhoneFrame.ReferenceLong;
        int high = upright ? PhoneFrame.ReferenceLong : PhoneFrame.ReferenceShort;
        if (DisplayServer.GetName() == "headless")
        {
            if (_root is not null)
                _root.Size = new Vector2I(wide / 2, high / 2);
            return;
        }
        var usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
        var room = usable.Size - new Vector2I(16, 48);                    // the window's border and title bar
        float scale = Math.Min(1, Math.Min(room.X / (float)wide, room.Y / (float)high));
        var size = new Vector2I((int)(wide * scale), (int)(high * scale));
        DisplayServer.WindowSetSize(size);
        DisplayServer.WindowSetPosition(usable.Position + (usable.Size - size) / 2);
        GD.Print($"Kichikuou: a phone held {(upright ? "in portrait" : "landscape")}, window {size.X}×{size.Y}.");
    }

    /// <summary>Whether a PC plays a phone (<c>--phone</c>), whose window a choice of the orientation turns.</summary>
    public static bool PlaysPhone => OS.GetName() != "Android" && PhoneSwitch is not null;

    private static int? _turnEvery;

    /// <summary>
    /// <c>--turn-every n</c> on a PC playing a phone (docs/ui/phone-build.md, step P9): the window turned every
    /// <c>n</c> frames, upright to landscape and back, so unattended play goes through turns on every screen it reaches.
    /// Called once a frame; nothing without the switch.
    /// </summary>
    public static void TurnWhenDue()
    {
        if (_turnEvery is null)
        {
            var args = OS.GetCmdlineUserArgs();
            int at = Array.IndexOf(args, "--turn-every");
            _turnEvery = PlaysPhone && at >= 0 && at + 1 < args.Length && int.TryParse(args[at + 1], out int n) && n > 0 ? n : 0;
        }
        if (_turnEvery == 0 || Godot.Engine.GetProcessFrames() % (ulong)_turnEvery != 0)
            return;
        TurnWindow(_layout != DeviceLayout.PhoneUpright);
    }

    private static void Read()
    {
        var window = _root?.Size ?? DisplayServer.WindowGetSize();     // the root's own: headless has no window to ask
        var was = _layout;
        _layout = DeviceLayouts.Decide(TouchMode.On, Phone, window.X, window.Y);
        if (_layout != was)
            Turns++;
        if (OS.GetName() != "Android" && _layout != was && Phone)
            GD.Print($"Kichikuou: {_layout}, window {window.X}×{window.Y}.");
        ReadMargins(window);
        if (OS.GetName() != "Android" || window.X <= 0 || _root is null)
            return;
        _insets = SafeInsets(window, _root.GetVisibleRect().Size);
        GD.Print($"Kichikuou: {_layout}, window {window}, safe area {DisplayServer.GetDisplaySafeArea()}, insets {_insets}.");   // the Poco's own (phone-build.md, §7)
    }

    /// <summary>
    /// The millimetre in the canvas's units and the player's margins for the layout as it is: none on the PC with the mouse,
    /// else the set for the way the device is held (<see cref="ScreenMarginsChoice"/>).
    /// </summary>
    private static void ReadMargins(Vector2I window)
    {
        if (_root is null || window.X <= 0 || window.Y <= 0)
            return;
        var canvas = _root.GetVisibleRect().Size;
        float shortSide = Math.Min(canvas.X, canvas.Y);
        _perMillimetre = PlaysPhone ? shortSide / PhoneFrame.ReferenceShort * MarginsCalibration.ReferencePerMillimetre
            : OS.GetName() == "Android" && DisplayServer.ScreenGetDpi() is > 0 and var dpi ? dpi / 25.4f * canvas.X / window.X
            : shortSide / TabletShortSide;
        _margins = TouchLayout ? ScreenMarginsChoice.For(HeldUpright).Insets(_perMillimetre) : default;
    }

    /// <summary>A set of margins changed: every screen lays itself out again, as after a turn (<see cref="Turns"/>).</summary>
    private static void MarginsChanged()
    {
        ReadMargins(_root?.Size ?? DisplayServer.WindowGetSize());
        Turns++;
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
