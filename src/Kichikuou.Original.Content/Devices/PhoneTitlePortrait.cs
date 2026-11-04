using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The title (<c>ｽﾀMENU</c>) on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §44-§45: PE6,
/// the owner's choice): our own screen over the engine's picture. Over the whole screen CG 301's stone texture at the
/// screen's width, mirrored over and under itself, its friezes at the screen's top and foot (<see cref="Tiles"/>,
/// <see cref="FriezeTop"/>, <see cref="FriezeFoot"/>); its logo laid whole in the middle of the room over the rows
/// (<see cref="Logo"/>); a finger row for each button the title draws (<see cref="Buttons"/>, <see cref="Row"/>) — the jokes
/// on top, then the extras, Alice, Load and Start, red, at the right thumb (<see cref="Order"/>) — the button's character,
/// cut without its word (<see cref="PhoneTitleArt"/>), in a column nearer the middle, their middles on one line, and its
/// word left-aligned right of it (<see cref="Column"/>, <see cref="Icon"/>). A row is a click in the middle of its button's
/// box as the title loaded it (<see cref="Centre"/>), a long press there the right click: the button's description,
/// window 113, under the picture's place centred on the logo (<see cref="DescriptionPictureTop"/>), the rows hidden. The
/// figures are the mock-up's (<c>tools/ui/mock_title_upright.py</c>: <c>rows</c>, <c>column</c>, <c>lay_logo</c>,
/// <c>background_pe5</c>) in the reference phone's pixels; the boxes in the canvas's units, the pictures' sizes in their CG
/// pixels. Interface only. Engine-independent.
/// </summary>
public static class PhoneTitlePortrait
{
    /// <summary>The title's buttons with a row (<c>ｽﾀMENU.ADV:805</c>'s boxes 0-7; 8-10 do nothing, 11 is never loaded).</summary>
    public const int Count = 8;

    /// <summary>Start (button 6), the red row at the thumb.</summary>
    public const int Start = 6;

    /// <summary>The rows' order top down: Spy and Please, Destiny, B.G.M and C.G, Alice, Load, Start.</summary>
    public static readonly IReadOnlyList<int> Order = [4, 3, 0, 1, 7, 2, 5, 6];

    /// <summary>Each button's word, as the title's picture prints it (CG 302/303).</summary>
    public static readonly IReadOnlyList<string> Words = ["Destiny", "B.G.M", "Alice", "Please", "Spy", "Load", "Start", "C.G"];

    /// <summary>
    /// The buttons the title draws, in the rows' order: each whose menu item is 1 (<c>ｽﾀMENU.ADV:87-96</c>; a first launch
    /// Alice, Load and Start, a clear Destiny, B.G.M and C.G, the rolls Please and Spy).
    /// </summary>
    public static IReadOnlyList<int> Buttons(GameState s) => [.. Order.Where(n => s.MenuItems[(Sys3Int)n].MenuItem == 1)];

    /// <summary>The middle of <paramref name="button"/>'s box as the title loaded it (script pixels): where its row clicks.</summary>
    public static (int X, int Y) Centre(GameState s, int button)
    {
        var b = s.Buttons[(Sys3Int)button];
        return ((b.X1 + b.W / (Sys3Int)2).Value, (b.Y1 + b.H / (Sys3Int)2).Value);
    }

    /// <summary>A row's height (11.8 mm), the room between two, the rows' foot over the bar, their distance from the room's sides.</summary>
    private const float RowHeight = 208, RowGap = 14, RowsOver = 20, RowIn = 40;

    /// <summary>A character's room in a row (the slot less its margins), and its scale at most: twice its CG pixels.</summary>
    private const float IconWidth = 230, IconHeight = RowHeight - 22, IconMost = 2;

    /// <summary>A row's word: the brush face, 3.8 mm, this far right of the characters' column.</summary>
    public const float WordText = 66;
    private const float WordGap = 44;

    /// <summary>The logo's scale (reference pixels a CG pixel), and its room's distance from the room's top and the rows.</summary>
    private const float LogoScale = 2.2f, LogoRoom = 40;

    /// <summary>CG 301's width: the texture's scale is the screen's width over it.</summary>
    private const float TextureWidth = 640;

    private static float Top(PhoneFrame f, int rows) =>
        f.Bar.Y - f.Px(RowsOver + rows * RowHeight + Math.Max(0, rows - 1) * RowGap);

    /// <summary>Row <paramref name="index"/> of <paramref name="rows"/>, the last standing over the bar.</summary>
    public static PhoneFrame.Box Row(PhoneFrame f, int index, int rows) =>
        new(f.Room.X + f.Px(RowIn), Top(f, rows) + index * f.Px(RowHeight + RowGap), Math.Max(0, f.Room.W - f.Px(2 * RowIn)), f.Px(RowHeight));

    /// <summary>A character <paramref name="width"/> × <paramref name="height"/> CG pixels as a row shows it.</summary>
    public static (float W, float H) IconSize(PhoneFrame f, int width, int height)
    {
        float k = Math.Min(IconMost, Math.Min(IconWidth / width, IconHeight / height));
        return (f.Px(width * k), f.Px(height * k));
    }

    /// <summary>
    /// The characters' column and the words' left edge: the widest character (<paramref name="widestIcon"/>, of all eight,
    /// so nothing moves when a button is unlocked), the gap and the widest word (<paramref name="widestWord"/>) centred on the
    /// screen as one.
    /// </summary>
    public static (float Left, float Words) Column(PhoneFrame f, float widestIcon, float widestWord)
    {
        float left = f.Screen.X + (f.Screen.W - (widestIcon + f.Px(WordGap) + widestWord)) / 2;
        return (left, left + widestIcon + f.Px(WordGap));
    }

    /// <summary>A character of <paramref name="size"/> in <paramref name="row"/>: its middle on the column's, in the row's middle.</summary>
    public static PhoneFrame.Box Icon(PhoneFrame.Box row, float left, float widestIcon, (float W, float H) size) =>
        new(left + (widestIcon - size.W) / 2, row.Y + (row.H - size.H) / 2, size.W, size.H);

    /// <summary>The logo, <paramref name="width"/> × <paramref name="height"/> CG pixels, in the middle of the room over <paramref name="rows"/> rows.</summary>
    public static PhoneFrame.Box Logo(PhoneFrame f, int rows, int width, int height)
    {
        float w = f.Px(width * LogoScale), h = f.Px(height * LogoScale);
        float top = f.Room.Y + f.Px(LogoRoom), foot = Top(f, rows) - f.Px(LogoRoom);
        return new PhoneFrame.Box(f.Screen.X + (f.Screen.W - w) / 2, (top + foot - h) / 2, w, h);
    }

    /// <summary>
    /// The texture's copies down the screen, <paramref name="height"/> CG pixels high each at the screen's width, from the
    /// screen's top; every second one is drawn upside down (its mirror image, so the stone runs on without a seam).
    /// </summary>
    public static IEnumerable<(PhoneFrame.Box Box, bool Flipped)> Tiles(PhoneFrame f, int height)
    {
        float h = height * f.Screen.W / TextureWidth;
        for (int i = 0; f.Screen.Y + i * h < f.Screen.Bottom; i++)
            yield return (new PhoneFrame.Box(f.Screen.X, f.Screen.Y + i * h, f.Screen.W, h), i % 2 == 1);
    }

    /// <summary>The top frieze, <paramref name="height"/> CG pixels high, at the screen's top.</summary>
    public static PhoneFrame.Box FriezeTop(PhoneFrame f, int height) =>
        new(f.Screen.X, f.Screen.Y, f.Screen.W, height * f.Screen.W / TextureWidth);

    /// <summary>The foot frieze, <paramref name="height"/> CG pixels high, at the screen's foot.</summary>
    public static PhoneFrame.Box FriezeFoot(PhoneFrame f, int height)
    {
        float h = height * f.Screen.W / TextureWidth;
        return new PhoneFrame.Box(f.Screen.X, f.Screen.Bottom - h, f.Screen.W, h);
    }

    /// <summary>
    /// Where the engine's picture stands while the title shows a button's description (the rows hidden): centred on the
    /// logo (<paramref name="logoHeight"/> CG pixels high), so the window under it (PS2's narration) comes under the logo.
    /// </summary>
    public static float DescriptionPictureTop(PhoneFrame f, int rows, int logoHeight)
    {
        var logo = Logo(f, rows, 1, logoHeight);
        return logo.Y + logo.H / 2 - f.PictureAt(0).H / 2;
    }
}
