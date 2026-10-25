using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The clock our views animate by (pulses, bobs, blinks, flashes): Godot's, or while the screen gallery dumps (docs/ui/
/// gallery.md, §5) one that starts at the same time for every entry and goes a sixtieth of a second a frame, so two dumps
/// of the same screen catch its animations at the same point.
/// </summary>
public static class UiClock
{
    private static ulong? _frames;

    /// <summary>Milliseconds, as <see cref="Time.GetTicksMsec"/>.</summary>
    public static ulong Msec => _frames ?? Time.GetTicksMsec();

    /// <summary>The frames' clock from now on, standing at <paramref name="start"/> until <see cref="Step"/>.</summary>
    public static void Frames(ulong start) => _frames = start;

    /// <summary>One frame on the frames' clock (nothing on Godot's).</summary>
    public static void Step()
    {
        if (_frames is { } now)
            _frames = now + 1000 / 60;
    }

    /// <summary>Godot's clock again.</summary>
    public static void Real() => _frames = null;
}
