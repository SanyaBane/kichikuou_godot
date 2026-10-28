using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The touch mode (owner, 2026-10-24; docs/ui/touch.md, "The tablet"): what our screens add where there is no hover
/// and no right button — the battle's rails, the map's Commands button, the target frame's pulse. On on Android, and
/// for a launch with <c>--touch</c> or <c>--phone</c> (development: a look in a window, the mouse standing in for the
/// finger). How our screens lay themselves out is <see cref="Device"/>'s.
/// </summary>
internal static class TouchMode
{
    private static bool? _on;
    private static Vector2 _touch = new(-100000, -100000);     // no touch yet: the pointer is nowhere

    /// <summary>Whether the touch mode is on.</summary>
    public static bool On => _on ??= OS.GetName() == "Android" || OS.GetCmdlineUserArgs() is var args && (args.Contains("--touch") || args.Contains("--phone"));

    /// <summary>
    /// Whether the mouse plays a finger (<c>--touch</c> on PC; owner, 2026-10-24): it moves the pointer only while a
    /// button is down, and the pointer stays where the last press or drag left it, as Android's emulated mouse does
    /// (<see cref="NoHoverFilter"/>). On Android the mouse is the finger already.
    /// </summary>
    public static bool NoHover => On && OS.GetName() != "Android";

    /// <summary>Where the last press or drag left the pointer, in the root viewport (<see cref="NoHoverFilter"/>).</summary>
    public static void Touched(Vector2 at) => _touch = at;

    /// <summary>The pointer in the root viewport: the mouse's, or while <see cref="NoHover"/> the last touch's.</summary>
    public static Vector2 Pointer(Viewport viewport) => NoHover ? _touch : viewport.GetMousePosition();

    /// <summary><see cref="CanvasItem.GetGlobalMousePosition"/>, the pointer as <see cref="Pointer"/> gives it.</summary>
    public static Vector2 GlobalPointer(CanvasItem item) =>
        NoHover ? item.GetCanvasTransform().AffineInverse() * _touch : item.GetGlobalMousePosition();

    /// <summary><see cref="CanvasItem.GetLocalMousePosition"/>, the pointer as <see cref="Pointer"/> gives it.</summary>
    public static Vector2 LocalPointer(CanvasItem item) =>
        NoHover ? item.GetGlobalTransform().AffineInverse() * GlobalPointer(item) : item.GetLocalMousePosition();
}

/// <summary>
/// While <see cref="TouchMode.NoHover"/>: drops the mouse's moves with no button down before the game window sees
/// them, so nothing lights up or opens under a pointer no finger touches, and keeps the last press's or drag's place
/// for <see cref="TouchMode.Pointer"/>. It takes the main window's input callback from Godot's own: a node's
/// <c>_Input</c> comes too late, the viewport updates the controls' hover (<c>mouse_entered</c>) before it. Every
/// other event goes on to the root viewport, as the window's callback passes it (the subwindows are embedded in it).
/// </summary>
internal static class NoHoverFilter
{
    /// <summary>Puts the filter in front of <paramref name="root"/>, the main window.</summary>
    public static void Install(Window root) =>
        DisplayServer.WindowSetInputEventCallback(Callable.From((InputEvent e) => Pass(root, e)), (int)DisplayServer.MainWindowId);

    private static void Pass(Window root, InputEvent @event)
    {
        switch (@event)
        {
            case InputEventMouseMotion { ButtonMask: 0 }:
                return;
            case InputEventMouseMotion or InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Right }:
                // Input has taken the event's place already: the root viewport's mouse position is this event's.
                TouchMode.Touched(root.GetMousePosition());
                break;
        }
        if (root.IsInsideTree())
            root.PushInput(@event);
    }
}
