using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// What our Harem and Treasure screens share on the phone (docs/ui/phone-build.md, steps L7 and P7; docs/ui/phone.md, §15,
/// LH2 and LT2, and rounds P39-P40): the column's title band, the grid's empty cells and its scroll bar, a plain button of
/// the foot. Drawn in a <see cref="PhoneUnits"/> drawing, the places <see cref="PhonePickers"/>'.
/// </summary>
internal static class PhonePickerParts
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    /// <summary>The baseline that puts <paramref name="font"/>'s line at <paramref name="size"/> with its middle at <paramref name="middle"/>.</summary>
    public static float Middle(Font font, int size, float middle) => middle + (font.GetAscent(size) - font.GetDescent(size)) / 2;

    /// <summary>The screen's name at the column's left (the brush face), the date at its right, the divider under them.</summary>
    public static void DrawTitle(CanvasItem c, PhoneUnits u, Font brush, Font cinzel, string title, string date)
    {
        var column = u.Rect(PhonePickers.Column(u.Frame));
        int size = u.Size(PhonePickers.TitleText);
        c.DrawString(brush, new Vector2(column.Position.X + u.Px(PhonePickers.TitleIn), u.Px(PhonePickers.TitleTop) + brush.GetAscent(size)),
            title, fontSize: size, modulate: Ui.Name);
        int dateSize = u.Size(PhonePickers.DateText);
        c.DrawString(cinzel, new Vector2(column.End.X - cinzel.GetStringSize(date, fontSize: dateSize).X,
            u.Px(PhonePickers.DateTop) + cinzel.GetAscent(dateSize)), date, fontSize: dateSize, modulate: Ui.Name);
        Ui.DrawDivider(c, column.Position.X, column.End.X, u.Px(PhonePickers.Divider), u.K);
    }

    /// <summary>Held upright (PH2, PT2): the screen's name at the band's left (the brush face), the date at its right, the divider under them.</summary>
    public static void DrawPortraitTitle(CanvasItem c, PhoneUnits u, Font brush, Font cinzel, string title, string date)
    {
        var band = u.Rect(PhonePickersPortrait.Band(u.Frame));
        float y = band.Position.Y;
        int size = u.Size(PhonePickersPortrait.TitleText);
        c.DrawString(brush, new Vector2(band.Position.X + u.Px(PhonePickersPortrait.TitleIn), y + brush.GetAscent(size)), title, fontSize: size,
            modulate: Ui.Name);
        int dateSize = u.Size(PhonePickersPortrait.DateText);
        c.DrawString(cinzel, new Vector2(band.End.X - u.Px(PhonePickersPortrait.DateRight) - cinzel.GetStringSize(date, fontSize: dateSize).X,
            y + u.Px(PhonePickersPortrait.DateTop) + cinzel.GetAscent(dateSize)), date, fontSize: dateSize, modulate: Ui.Name);
        Ui.DrawDivider(c, band.Position.X + u.Px(PhonePickersPortrait.DividerIn), band.End.X - u.Px(PhonePickersPortrait.DividerIn),
            y + u.Px(PhonePickersPortrait.Divider), u.K);
    }

    /// <summary>The reference scroll bar right of the grid, while the grid scrolls.</summary>
    public static void DrawScrollBar(CanvasItem c, PhoneUnits u, PhonePickersPortrait.ScrolledGrid grid, int scroll)
    {
        if (grid.MaxScroll == 0)
            return;
        var (track, thumb) = grid.Bar(u.Frame, scroll);
        Ui.DrawScrollBar(c, u.Rect(track), u.Rect(thumb), u.Px(PhoneScrollBar.Rim));
    }

    /// <summary>A cell no entry holds: dimmed, as the PC's.</summary>
    public static void DrawEmptyCell(CanvasItem c, Rect2 cell, float k)
    {
        c.DrawRect(cell, new Color(Ui.OffButton, 0.55f));
        c.DrawRect(cell, new Color(Ui.OffLine, 0.6f), false, Lacquer.Line(1, k));
    }

    /// <summary>
    /// A plain button of the foot: a panel, its word in Cinzel 700 at <paramref name="size"/> (the group's, <see cref="Room"/>),
    /// lit while hovered.
    /// </summary>
    public static void DrawButton(CanvasItem c, PhoneUnits u, Rect2 box, string word, bool lit, int size)
    {
        Ui.DrawPanel(c, box, u.K, lit ? Ui.RowHover : null);
        SideCells.DrawCentred(c, box, SideCells.Cinzel, word, size, lit ? Ui.Name : Ui.Text);
    }

    /// <summary>The room a button's word has in <paramref name="box"/>: 15 reference pixels clear at either side.</summary>
    public static float Room(PhoneUnits u, Rect2 box) => box.Size.X - u.Px(30);
}
