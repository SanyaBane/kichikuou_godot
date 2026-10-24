namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// Where our touch mode's Skip and Log buttons stand (owner, 2026-10-24; docs/ui/touch.md, "The dialogue buttons, round
/// 2"), in the 1280×800 screen's pixels. In the scenes they are a drawer glued to the screen's left edge, Skip over Log,
/// always at the same place (the owner's B1/B2): between the edge and every window, under the left portrait. A window
/// that would cover it (131, the bar across the foot) is drawn smaller right of it, scaled about its top right corner,
/// and the drawer's cells stand on a dock as high as the window (the owner's B3b). In our battle screen Skip alone stands
/// in the free strip under the cards. Interface only. Engine-independent.
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

    /// <summary>The drawer's cells: their width and height (10.6 × 17.1 mm on the tablet) and the first one's top.</summary>
    private const int CellWidth = 50, CellHeight = 84, CellTop = 592;

    /// <summary>The dock's width over a window drawn smaller, and the room between it and the window.</summary>
    private const int DockWidth = 56, DockGap = 4;

    /// <summary>The drawer's plate: the cells and 2 pixels round them (none at the screen's edge).</summary>
    public static Box Drawer { get; } = new(0, CellTop - 2, CellWidth + 2, 2 * CellHeight + 4);

    /// <summary>The Skip cell, the drawer's upper one.</summary>
    public static Box SkipCell { get; } = new(0, CellTop, CellWidth, CellHeight);

    /// <summary>The Log cell, under Skip.</summary>
    public static Box LogCell { get; } = new(0, CellTop + CellHeight, CellWidth, CellHeight);

    /// <summary>
    /// The scale a message window whose panel is <paramref name="panel"/> (<see cref="MessageWindow.Panel"/>) is drawn at,
    /// about its top right corner, so that it ends <see cref="DockGap"/> right of the dock: under 1 for a window that
    /// covers the drawer (131), else 1.
    /// </summary>
    public static float WindowScale((int X, int Y, int W, int H)? panel) =>
        panel is { } p && Drawer.Overlaps(p) ? (p.X + p.W - DockWidth - DockGap) / (float)p.W : 1;

    /// <summary>The dock the drawer's cells stand on beside a window drawn smaller (<see cref="WindowScale"/>): as high as it.</summary>
    public static Box Dock((int X, int Y, int W, int H) panel) =>
        new(0, panel.Y, DockWidth, (int)Math.Round(panel.H * WindowScale(panel)));

    /// <summary>
    /// Our battle screen's Skip: in the strip under the cards (y 758-800, free on every battle screen, the player's turn
    /// included), at its left — the scenes' corner.
    /// </summary>
    public static Box Battle { get; } = new(12, ScreenHeight - 38, 150, 34);
}
