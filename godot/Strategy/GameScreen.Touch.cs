using Godot;

namespace Kichikuou.Game.Strategy;

// A touch screen's way to the right button (docs/backlog.md, "Android").
public partial class GameScreen
{
    /// <summary>
    /// Android's Back (the button or the gesture; project.godot keeps it from quitting) is a right click where the
    /// last touch left the pointer: the original cancels with the right button, and a touch screen has none. It goes
    /// through Godot's input, so the script's reads and our own screens see it as a mouse's.
    /// </summary>
    private void BackAsRightClick() =>
        _ = AutoViewClickAsync(GetViewport().GetMousePosition(), MouseButton.Right);
}
