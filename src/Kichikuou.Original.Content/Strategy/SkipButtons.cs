namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// Where our touch mode's Skip and Log buttons stand (owner, 2026-10-24; docs/ui/touch.md, "The Skip and Log buttons"),
/// in the 1280×800 screen's pixels. In the scenes the pair is at the bottom left, 16 in from the picture's edges as the
/// map's Commands button is at the bottom right: side by side, else Log above Skip where a message window leaves no
/// room beside it (the narration's), else side by side above the window (131 and the right speech cover the bottom
/// left), clear of 131's speaker plate. In our battle screen Skip alone stands in the free strip under the cards.
/// Interface only. Engine-independent.
/// </summary>
public static class SkipButtons
{
    /// <summary>A button's rectangle (screen pixels).</summary>
    public readonly record struct Box(int X, int Y, int W, int H)
    {
        /// <summary>Whether it shares any pixel with the panel <paramref name="p"/>.</summary>
        public bool Overlaps((int X, int Y, int W, int H) p) => X < p.X + p.W && p.X < X + W && Y < p.Y + p.H && p.Y < Y + H;
    }

    /// <summary>The screen's height.</summary>
    private const int ScreenHeight = 800;

    /// <summary>The scenes' buttons: their height (the Commands button's), widths, the margin and the gap between them.</summary>
    private const int Height = 84, SkipWidth = 190, LogWidth = 150, Margin = 16, Gap = 14;

    /// <summary>
    /// Above a window: the room between the pair and the window's top edge, clear of 131's speaker plate
    /// (<c>MessageWindowView.NameTab</c>: 26 above the edge).
    /// </summary>
    private const int Above = 40;

    /// <summary>
    /// The scenes' Skip and Log beside the message window whose panel is <paramref name="panel"/>
    /// (<see cref="MessageWindow.Panel"/>), or none.
    /// </summary>
    public static (Box Skip, Box Log) Scene((int X, int Y, int W, int H)? panel)
    {
        int bottom = ScreenHeight - Margin - Height;
        var skip = new Box(Margin, bottom, SkipWidth, Height);
        var side = (skip, new Box(Margin + SkipWidth + Gap, bottom, LogWidth, Height));
        if (panel is not { } p || !Overlaps(side, p))
            return side;
        var stacked = (skip, new Box(Margin, bottom - Gap - Height, LogWidth, Height));
        if (!Overlaps(stacked, p))
            return stacked;
        int top = Math.Max(Margin, p.Y - Above - Height);
        return (skip with { Y = top }, side.Item2 with { Y = top });
    }

    /// <summary>
    /// Our battle screen's Skip: in the strip under the cards (y 758-800, free on every battle screen, the player's turn
    /// included), at its left — the scenes' corner.
    /// </summary>
    public static Box Battle { get; } = new(12, ScreenHeight - 38, 150, 34);

    private static bool Overlaps((Box Skip, Box Log) pair, (int X, int Y, int W, int H) panel) =>
        pair.Skip.Overlaps(panel) || pair.Log.Overlaps(panel);
}
