using Godot;

namespace Kichikuou.Game.Strategy;

// Pointer events made by code — Back's right click, unattended play's clicks — fed through Godot's input, so the script's
// reads and every control of ours see them as a mouse's.
public partial class GameScreen
{
    /// <summary>The pointer moved to <paramref name="at"/> (the viewport's coordinates).</summary>
    private void SendPointerMotion(Vector2 at)
    {
        var window = WindowPoint(at);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = window, GlobalPosition = window });
    }

    /// <summary>The pointer dragged to <paramref name="at"/> with the left button down (the viewport's coordinates): a finger's move.</summary>
    private void SendPointerDrag(Vector2 at)
    {
        var window = WindowPoint(at);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = window, GlobalPosition = window, ButtonMask = MouseButtonMask.Left });
    }

    /// <summary><paramref name="button"/> went down or up at <paramref name="at"/> (the viewport's coordinates).</summary>
    private void SendPointerButton(Vector2 at, MouseButton button, bool pressed)
    {
        var window = WindowPoint(at);
        var mask = button switch
        {
            MouseButton.Left => MouseButtonMask.Left,
            MouseButton.Right => MouseButtonMask.Right,
            _ => default,                                   // the wheel's notch holds no button
        };
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = button, Pressed = pressed, Position = window, GlobalPosition = window, ButtonMask = pressed ? mask : 0,
        });
    }

    /// <summary>
    /// <paramref name="at"/> in the window's pixels: Godot's input takes an event there, as the system's, and maps it
    /// to the viewport's through the stretch — the same on a window at its own size, twice apart on the tablet.
    /// </summary>
    private Vector2 WindowPoint(Vector2 at) => GetViewport().GetScreenTransform() * at;
}
