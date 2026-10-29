using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The phone's units in a view's drawing (docs/ui/phone-build.md, step L1): a phone layout draws over the whole screen in
/// the frame's layout pixels — so the palette's parts keep the frame's scale — from the screen's top left corner;
/// <see cref="Rect"/> turns a box of the canvas's units (<see cref="PhoneFrame"/>'s) into them, <see cref="Px"/> a length
/// in the reference phone's pixels (the mock-ups'). <see cref="K"/> is the drawing transform's scale.
/// </summary>
internal readonly record struct PhoneUnits(PhoneFrame Frame)
{
    /// <summary>The canvas's units a layout pixel: the drawing transform's scale.</summary>
    public float K => Frame.Scale;

    /// <summary>A box of the canvas's units in layout pixels.</summary>
    public Rect2 Rect(PhoneFrame.Box box) => new(box.X / K, box.Y / K, box.W / K, box.H / K);

    /// <summary>A length of <paramref name="reference"/> reference pixels in layout pixels.</summary>
    public float Px(float reference) => Frame.Px(reference) / K;

    /// <summary>A font size of <paramref name="reference"/> reference pixels, in layout pixels, rounded as a font size is.</summary>
    public int Size(float reference) => Math.Max(1, (int)Math.Round(Px(reference)));
}

/// <summary>
/// A cell in the phone's black side (docs/ui/phone.md, §19 and §26): a lacquer plate framed in gold, its icon at the left,
/// its word right of it — Menu, Close, Skip, Log; in the bar held upright (§21, PK1), a taller cell, the icon over the word. Lit gold (Skip while the skip runs, Close while what it closes is up); a
/// greyed cell is deaf; a held one shows the press. Drawn in a <see cref="PhoneUnits"/> drawing.
/// </summary>
internal static class SideCells
{
    /// <summary>A cell's icon.</summary>
    public enum Icon { Menu, Close, Skip, Log }

    private static UiPalette Ui => UiPalettes.For(UiPart.Messages);

    private static readonly Color Lit = Color.Color8(96, 72, 30), Held = new(1, 1, 1, 0.08f);

    private static FontFile? _font;

    /// <summary>The words' face: the bundled BIZ UDGothic, whatever the game's font (the mock-ups').</summary>
    public static FontFile Font => _font ??= Lacquer.LoadFont(GodotGlyphRenderer.BizUdGothicPath);

    /// <summary><paramref name="size"/>, or as much smaller as <paramref name="text"/> needs to fit <paramref name="width"/> (the mock-ups' <c>fit</c>).</summary>
    public static int Fit(Font font, string text, int size, float width)
    {
        while (size > 8 && font.GetStringSize(text, fontSize: size).X > width)
            size--;
        return size;
    }

    /// <summary>
    /// The map's button in a side (docs/ui/phone.md, §12, LM1): the side being narrow, its icon over its word in the brush
    /// face — Commands at the right thumb, Close and Return where it stands.
    /// </summary>
    public static void DrawButton(CanvasItem c, PhoneUnits u, Rect2 box, Icon icon, string word, bool held = false,
        UiPalette? palette = null)
    {
        var ui = palette ?? Ui;
        ui.DrawPanel(c, box, u.K, held ? ui.RowHover : null);
        float cx = box.GetCenter().X;
        DrawIcon(c, u, icon, new Vector2(cx, box.Position.Y + Math.Min(u.Px(48), box.Size.Y * 0.36f)), ui.Gold);
        var brush = Brush;
        int size = Fit(brush, word, u.Size(36), box.Size.X - u.Px(30));
        float width = brush.GetStringSize(word, fontSize: size).X;
        float middle = box.End.Y - Math.Min(u.Px(40), box.Size.Y * 0.3f);
        c.DrawString(brush, new Vector2(cx - width / 2, middle + (brush.GetAscent(size) - brush.GetDescent(size)) / 2), word,
            fontSize: size, modulate: ui.Name);
    }

    /// <summary>
    /// A rail's row in a side (docs/ui/phone.md, §12, LM1b, LM1c): with a tag, the tag over the name in two lines (a side has
    /// no room for both in one), a building under way's weeks on its chip; without one, the name centred. Dimmed when off,
    /// lit while held. <paramref name="font"/> is the game's.
    /// </summary>
    public static void DrawRow(CanvasItem c, PhoneUnits u, Rect2 box, Font font, string name, bool on = true, bool held = false,
        (string Word, Color Colour)? tag = null, int? weeks = null, UiPalette? palette = null)
    {
        var ui = palette ?? Ui;
        ui.DrawRowBox(c, box, held && on, u.K);
        var ink = on ? ui.Text : ui.OffText;
        if (tag is not var (word, colour))
        {
            int centred = Fit(font, name, u.Size(38), box.Size.X - u.Px(30));
            float w = font.GetStringSize(name, fontSize: centred).X;
            c.DrawString(font, new Vector2(box.GetCenter().X - w / 2, Lacquer.Baseline(box, font, centred)), name,
                fontSize: centred, modulate: ink);
            return;
        }
        // The tag's band: 36 high, 14 in; the name's line centred 34 over the row's foot (the mock-ups' rail_cell). A low row
        // (a rail with many) keeps the proportions.
        float s = Math.Min(1, box.Size.Y / u.Px(123));
        int tagSize = u.Size(26 * s);
        float tagWidth = font.GetStringSize(word, fontSize: tagSize).X + u.Px(24 * s);
        if (weeks is { } left)
            tagWidth += UiPalette.ChipWidth(font, left, tagSize);
        var band = new Rect2(box.Position.X + u.Px(14 * s), box.Position.Y + u.Px(14 * s), tagWidth, u.Px(36 * s));
        if (weeks is not null)
            ui.DrawBusyTag(c, font, band, word, colour, u.K, tagSize, weeks);
        else
            ui.DrawTag(c, font, band, word, colour, on, u.K, tagSize);
        int size = Fit(font, name, u.Size(36 * s), box.Size.X - u.Px(30));
        var line = new Rect2(box.Position.X, box.End.Y - u.Px(34 * s) - u.Px(20 * s), box.Size.X, u.Px(40 * s));
        c.DrawString(font, new Vector2(box.Position.X + u.Px(16), Lacquer.Baseline(line, font, size)), name, fontSize: size,
            modulate: weeks is not null ? ui.Quiet : ink);
    }

    private static FontFile? _brush;

    /// <summary>The brush face of the map's button and Turn END.</summary>
    public static FontFile Brush => _brush ??= Lacquer.Brush();

    /// <summary>Draws the cell in <paramref name="box"/> (layout pixels).</summary>
    public static void Draw(CanvasItem c, PhoneUnits u, Rect2 box, Icon icon, string word, bool lit = false,
        bool disabled = false, bool held = false)
    {
        Ui.DrawPanel(c, box, u.K, lit ? Lit : null);
        if (held && !disabled)
            c.DrawRect(box.Grow(-u.Px(6)), Held);
        var ink = disabled ? Ui.OffText : lit ? Ui.Name : Ui.Gold;
        var font = Font;
        if (box.Size.Y > box.Size.X * 0.6f)
        {
            // The bar's cell upright (PK1): the icon over the word, as the mock-ups' bottom_bar.
            float cx = box.GetCenter().X;
            DrawIcon(c, u, icon, new Vector2(cx, box.Position.Y + u.Px(58)), ink);
            int small = Fit(font, word, u.Size(28), box.Size.X - u.Px(16));
            float w = font.GetStringSize(word, fontSize: small).X;
            var line = new Rect2(box.Position.X, box.Position.Y + u.Px(96), box.Size.X, u.Px(40));
            c.DrawString(font, new Vector2(cx - w / 2, Lacquer.Baseline(line, font, small)), word, fontSize: small,
                modulate: disabled ? Ui.OffText : Ui.Text);
            return;
        }
        DrawIcon(c, u, icon, new Vector2(box.Position.X + u.Px(60), box.GetCenter().Y), ink);
        int size = u.Size(36);
        float width = font.GetStringSize(word, fontSize: size).X;
        c.DrawString(font, new Vector2(box.Position.X + u.Px(150) - width / 2, Lacquer.Baseline(box, font, size)), word,
            fontSize: size, modulate: disabled ? Ui.OffText : Ui.Text);
    }

    /// <summary>An icon centred at <paramref name="c"/>, 52 reference pixels wide (the mock-ups').</summary>
    public static void DrawIcon(CanvasItem item, PhoneUnits u, Icon icon, Vector2 c, Color ink)
    {
        float r = u.Px(26);
        switch (icon)
        {
            case Icon.Menu:
                foreach (float dy in (float[])[-16, 0, 16])
                    item.DrawRect(new Rect2(c.X - r, c.Y + u.Px(dy) - u.Px(4), 2 * r, u.Px(8)), ink);
                break;
            case Icon.Log:
                foreach (var (dy, w) in ((float, float)[])[(-16, 26), (0, 26), (16, 14)])
                    item.DrawRect(new Rect2(c.X - r, c.Y + u.Px(dy) - u.Px(4), 2 * u.Px(w), u.Px(8)), ink);
                break;
            case Icon.Skip:
                foreach (float dx in (float[])[-22, 4])
                    item.DrawColoredPolygon([new(c.X + u.Px(dx), c.Y - u.Px(20)), new(c.X + u.Px(dx + 26), c.Y), new(c.X + u.Px(dx), c.Y + u.Px(20))], ink);
                break;
            case Icon.Close:
                float a = u.Px(20);
                item.DrawLine(c + new Vector2(-a, -a), c + new Vector2(a, a), ink, u.Px(8), true);
                item.DrawLine(c + new Vector2(-a, a), c + new Vector2(a, -a), ink, u.Px(8), true);
                break;
        }
    }
}
