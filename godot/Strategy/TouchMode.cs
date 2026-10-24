using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The touch mode (owner, 2026-10-24; docs/ui/touch.md, "The tablet"): what our screens add where there is no hover
/// and no right button — the battle's rails, the map's Commands button, the target frame's pulse. On on Android, and
/// for a launch with <c>--touch</c> (development: a look in a window, the mouse standing in for the finger).
/// </summary>
internal static class TouchMode
{
    private static bool? _on;

    /// <summary>Whether the touch mode is on.</summary>
    public static bool On => _on ??= OS.GetName() == "Android" || OS.GetCmdlineUserArgs().Contains("--touch");
}
