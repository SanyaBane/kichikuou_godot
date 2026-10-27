namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// The room our interface gives the map above its top edge while our status strip lies over it (docs/ui/command-screen.md,
/// "The map's room under the strip"; owner, 2026-10-27). The script's camera stops at the map's top (<c>WAO_MAP.ADV</c>: the
/// view's top is a 16-bit value, never below 0), so the provinces along it — the Great Wall of Helman A (49) at 28, the
/// Tower of Evil (78) at 37 — would lie under the strip at every camera. While the view's top is within <see cref="Range"/>
/// of the edge, the picture is shown lower by <see cref="Shift"/> script pixels: the whole strip's height at the edge, so
/// the map's top row lies right under it, and none from <see cref="Range"/> on; the rows it pushes off the screen's foot
/// are reached by scrolling. Ours, no rule changes: the script's camera stays the original's, and the pointer it reads is
/// turned from the picture's to the view's by <see cref="ScriptY"/>.
/// </summary>
public sealed class MapHeadroom
{
    /// <summary>The status strip's height in script pixels (50 of the picture's 1280).</summary>
    public const int Strip = 25;

    /// <summary>The view's tops over which the room closes: in them the picture moves 1.25 times as far as the camera.</summary>
    public const int Range = 100;

    /// <summary>The view's lowest top (<c>WAO_MAP.ADV</c>: the map 800 high, the view 400).</summary>
    public const int MaxTop = 400;

    private const int ScreenHeight = 400;

    private int? _grabbed;

    /// <summary>How much lower the picture is shown at the view's top <paramref name="top"/>, in script pixels.</summary>
    public static int Shift(int top) => top <= 0 ? Strip : top >= Range ? 0 : (Strip * (Range - top) + Range / 2) / Range;

    /// <summary>The map row at the screen's top when the view's top is <paramref name="top"/>: the picture's corner.</summary>
    public static int Shown(int top) => top - Shift(top);

    /// <summary>
    /// The view's top at which the screen's top shows the map row <paramref name="row"/> — where a step of the camera moves
    /// the picture by two rows (in the room's range), the one that shows the row above it — within the camera's 0 to
    /// <see cref="MaxTop"/>.
    /// </summary>
    public static int TopShowing(int row)
    {
        if (row >= Range)
            return Math.Min(row, MaxTop);
        int top = 0;
        while (top < Range && Shown(top + 1) <= row)
            top++;
        return top;
    }

    /// <summary>
    /// The Y the script reads for the pointer at <paramref name="y"/> on the picture, the view's top <paramref name="top"/>
    /// shown: the view's row under it. While the left button is held (<paramref name="held"/>), the map row its press took
    /// stays under the pointer as the script drags the camera by the pointer's steps (<c>WAO_MAP.ADV:29-43</c>: the camera
    /// is the pressed row less the Y read): the Y read is the pressed row less the camera that shows it at the pointer.
    /// Dragged past the map's edge, where the camera stops, the pointer takes the row under it, so a step back scrolls at
    /// once, as in the original. The release is read as the press's last step (the script's coasting takes the last step,
    /// <c>WAO_MAP.ADV:45-51</c>), then the row is let go. Clamped to the screen.
    /// </summary>
    public int ScriptY(int y, int top, bool held)
    {
        if (_grabbed is not { } row)
        {
            int read = Clamp(y - Shift(top));
            if (held)
                _grabbed = read + top;
            return read;
        }
        int target = Math.Clamp(row - y, Shown(0), Shown(MaxTop));     // the map row the screen's top is to show
        row = y + target;
        _grabbed = held ? row : null;
        return Clamp(row - TopShowing(target));
    }

    /// <summary>The pointer leaves the map: a press held over it is forgotten.</summary>
    public void Release() => _grabbed = null;

    private static int Clamp(int y) => Math.Clamp(y, 0, ScreenHeight - 1);
}
