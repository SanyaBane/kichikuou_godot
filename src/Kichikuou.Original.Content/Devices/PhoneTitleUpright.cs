using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Devices;

/// <summary>
/// The title (<c>ｽﾀMENU</c>) on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §22: PE2 as the
/// owner changed it): a finger row under the picture for each button the title draws (<see cref="Buttons"/>), the button's
/// own word alone in the row's middle — the jokes on top, then the extras, Alice, Load and Start, red, at the right thumb
/// (<see cref="Order"/>); the rows in a lacquer panel standing on the bar (<see cref="Row"/>, <see cref="Panel"/>), the
/// picture at the room's width in the middle over them (<see cref="PictureTop"/>), its own targets kept. A row is a click in
/// the middle of its button's box as the title loaded it (<see cref="Centre"/>). The figures are the mock-up's
/// (<c>tools/ui/mock_phone.py</c>: <c>pe2</c>, <c>title_row</c>, <c>TITLE_ORDER</c>, <c>TITLE_WORDS</c>) in the reference
/// phone's pixels from the room and the bar; the boxes in the canvas's units. Interface only. Engine-independent.
/// </summary>
public static class PhoneTitleUpright
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

    /// <summary>A row's height, the room between two, the rows' foot over the bar, their distance from the room's sides.</summary>
    private const float RowHeight = 130, RowGap = 12, RowsOver = 20, RowIn = 40;

    /// <summary>The panel round the rows: over the first, its foot over the bar, its distance from the room's sides.</summary>
    private const float PanelOver = 28, PanelFoot = 6, PanelIn = 16;

    /// <summary>The room the picture's middle is reckoned to, over the rows (the mock-up's <c>top - 30</c>).</summary>
    private const float PictureOver = 30;

    /// <summary>A row's word: the brush face, 3.0 mm.</summary>
    public const float WordText = 52;

    private static float Top(PhoneFrame f, int rows) =>
        f.Bar.Y - f.Px(RowsOver + rows * RowHeight + Math.Max(0, rows - 1) * RowGap);

    /// <summary>Row <paramref name="index"/> of <paramref name="rows"/>, the last standing over the bar.</summary>
    public static PhoneFrame.Box Row(PhoneFrame f, int index, int rows) =>
        new(f.Room.X + f.Px(RowIn), Top(f, rows) + index * f.Px(RowHeight + RowGap), Math.Max(0, f.Room.W - f.Px(2 * RowIn)), f.Px(RowHeight));

    /// <summary>The lacquer panel round <paramref name="rows"/> rows.</summary>
    public static PhoneFrame.Box Panel(PhoneFrame f, int rows)
    {
        float top = Top(f, rows) - f.Px(PanelOver);
        return new PhoneFrame.Box(f.Room.X + f.Px(PanelIn), top, Math.Max(0, f.Room.W - f.Px(2 * PanelIn)), f.Bar.Y - f.Px(PanelFoot) - top);
    }

    /// <summary>The picture's top: in the middle between the room's top and the rows (less the panel's margin).</summary>
    public static float PictureTop(PhoneFrame f, int rows)
    {
        float h = f.PictureAt(0).H;
        return (f.Room.Y + Top(f, rows) - f.Px(PictureOver)) / 2 - h / 2;
    }
}
