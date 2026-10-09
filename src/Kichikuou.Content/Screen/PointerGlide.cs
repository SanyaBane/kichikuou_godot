namespace Kichikuou.Content.Screen;

/// <summary>
/// The path of xsystem35's pointer warp (<c>IZ</c>, <c>ags_setCursorLocation</c>, <c>ags.c:740-751</c>):
/// seven points on an ease-out cubic (<c>d·(1 - (1 - i/8)³)</c> in integer shifts), 50/7 ms apart,
/// then the target itself.
/// </summary>
public static class PointerGlide
{
    /// <summary>Milliseconds between two points (<c>cursor_move_time / 7</c>).</summary>
    public const int StepMilliseconds = 50 / 7;

    /// <summary>The seven intermediate points from <paramref name="from"/> towards <paramref name="to"/>, then <paramref name="to"/>.</summary>
    public static IReadOnlyList<(int X, int Y)> Path((int X, int Y) from, (int X, int Y) to)
    {
        int dx = to.X - from.X, dy = to.Y - from.Y;
        var points = new List<(int X, int Y)>(8);
        for (int i = 1; i < 8; i++)
            points.Add((Step(dx, i) + from.X, Step(dy, i) + from.Y));
        points.Add(to);
        return points;
    }

    private static int Step(int d, int i) => ((d * i * i * i) >> 9) - ((3 * d * i * i) >> 6) + ((3 * d * i) >> 3);
}
