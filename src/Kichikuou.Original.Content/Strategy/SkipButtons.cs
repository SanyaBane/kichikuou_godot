namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// Where our touch mode's Skip and Log buttons stand (owner, 2026-10-24; docs/ui/touch.md, "The dialogue buttons, round
/// 2"), in the 1280×800 screen's pixels. In the scenes they are two plates glued to the screen's left edge, Skip over Log
/// with 16 pixels of the scene between them (the owner's G3, 2026-10-25), always at the same place (the owner's B1/B2):
/// between the edge and every window, under the left portrait. A window that would cover them (131, the bar across the
/// foot) is drawn smaller right of them, scaled about its top right corner (the owner's B3b). In our battle screen Skip
/// alone stands in the free strip under the cards. Interface only. Engine-independent.
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

    /// <summary>The cells: their width and height (10.6 × 17.1 mm on the tablet) and Skip's top.</summary>
    private const int CellWidth = 50, CellHeight = 84, CellTop = 592;

    /// <summary>The room between the two plates (3.2 mm on the tablet).</summary>
    private const int PlateGap = 16;

    /// <summary>The column a window drawn smaller leaves the plates at the screen's left edge, and the room right of it.</summary>
    private const int ColumnWidth = 56, ColumnGap = 4;

    /// <summary>The Skip cell, the upper one.</summary>
    public static Box SkipCell { get; } = new(0, CellTop, CellWidth, CellHeight);

    /// <summary>Skip's plate: its cell and 2 pixels round it (none at the screen's edge).</summary>
    public static Box SkipPlate { get; } = new(0, CellTop - 2, CellWidth + 2, CellHeight + 4);

    /// <summary>Log's plate, <see cref="PlateGap"/> under Skip's.</summary>
    public static Box LogPlate { get; } = SkipPlate with { Y = SkipPlate.Y + SkipPlate.H + PlateGap };

    /// <summary>The Log cell, in its plate as Skip's is in its own.</summary>
    public static Box LogCell { get; } = SkipCell with { Y = LogPlate.Y + 2 };

    /// <summary>Both plates and the room between them: what the buttons keep free of windows.</summary>
    public static Box Drawer { get; } = SkipPlate with { H = LogPlate.Y + LogPlate.H - SkipPlate.Y };

    /// <summary>
    /// The scale a message window whose panel is <paramref name="panel"/> (<see cref="MessageWindow.Panel"/>) is drawn at,
    /// about its top right corner, so that it ends <see cref="ColumnGap"/> right of the plates' column: under 1 for a
    /// window that covers the plates (131), else 1.
    /// </summary>
    public static float WindowScale((int X, int Y, int W, int H)? panel) =>
        panel is { } p && Drawer.Overlaps(p) ? (p.X + p.W - ColumnWidth - ColumnGap) / (float)p.W : 1;

    /// <summary>
    /// Our battle screen's Skip: in the strip under the cards (y 758-800, free on every battle screen, the player's turn
    /// included), at its left, right of the left rail that stands beside the cards (x 0-140; owner's A, 2026-10-25).
    /// </summary>
    public static Box Battle { get; } = new(146, ScreenHeight - 38, 150, 34);

    /// <summary>The right edge of the touch battle screen's left rail, which <see cref="Battle"/> keeps clear of.</summary>
    public const int BattleLeftRailRight = 140;
}
