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
/// greyed cell is deaf; a held one shows the press. Drawn in a <see cref="PhoneUnits"/> drawing. Every cell and map button
/// of a side or of the bar writes its word in Cinzel 700 at one size, <see cref="GroupSize"/> (docs/ui/style.md, "Buttons
/// side by side").
/// </summary>
internal static class SideCells
{
    /// <summary>A cell's icon.</summary>
    public enum Icon { Menu, Close, Skip, Log }

    private static UiPalette Ui => UiPalettes.For(UiPart.Messages);

    private static readonly Color Lit = Color.Color8(96, 72, 30), Held = new(1, 1, 1, 0.08f);

    private static FontFile? _font;

    /// <summary>The Menu sheet's tiles' and rows' face: the bundled BIZ UDGothic, whatever the game's font (the mock-ups').</summary>
    public static FontFile Font => _font ??= Lacquer.LoadFont(GodotGlyphRenderer.BizUdGothicPath);

    /// <summary><paramref name="size"/>, or as much smaller as <paramref name="text"/> needs to fit <paramref name="width"/> (the mock-ups' <c>fit</c>).</summary>
    public static int Fit(Font font, string text, int size, float width)
    {
        while (size > 8 && font.GetStringSize(text, fontSize: size).X > width)
            size--;
        return size;
    }

    /// <summary>
    /// One size for a group of buttons side by side (docs/ui/style.md, "Buttons side by side"): <paramref name="size"/>, or
    /// as much smaller as the longest of <paramref name="words"/> needs to fit its room — the whole group shrunk together.
    /// </summary>
    public static int FitAll(Font font, int size, params (string Text, float Width)[] words)
    {
        foreach (var (text, width) in words)
            size = Fit(font, text, size, width);
        return size;
    }

    private static FontVariation? _cinzel;

    /// <summary>The button boxes' face: Cinzel 700 (docs/ui/style.md, "Buttons side by side").</summary>
    public static FontVariation Cinzel => _cinzel ??= Lacquer.Cinzel(700);

    /// <summary><paramref name="word"/> centred in <paramref name="box"/> at <paramref name="size"/> in <paramref name="font"/>.</summary>
    public static void DrawCentred(CanvasItem c, Rect2 box, Font font, string word, int size, Color ink)
    {
        float w = font.GetStringSize(word, fontSize: size).X;
        c.DrawString(font, new Vector2(box.GetCenter().X - w / 2,
            box.GetCenter().Y + (font.GetAscent(size) - font.GetDescent(size)) / 2), word, fontSize: size, modulate: ink);
    }

    /// <summary>The words a side's or the bar's cells write, the map's buttons' (the group <see cref="GroupSize"/> fits).</summary>
    private static readonly string[] CellWords = ["Menu", "Close", "Skip", "ON", "Log"], ButtonWords = ["Commands", "Close", "Return", "Back"];

    /// <summary>
    /// The one size every cell and map button of a side (held landscape) or of the bar (upright) writes its word at, Cinzel
    /// 700: <see cref="PhoneScenes.CellText"/> or <see cref="PhoneScenes.BarCellText"/>, or as much smaller as the longest
    /// word needs in its box — the whole group shrunk together.
    /// </summary>
    public static int GroupSize(PhoneUnits u)
    {
        var f = u.Frame;
        Rect2[] cells = [u.Rect(PhoneScenes.MenuCell(f)), u.Rect(PhoneScenes.LogCell(f)), u.Rect(PhoneScenes.SkipCell(f))];
        float button = f.IsPortrait ? BarButtonRoom(u, u.Rect(PhoneMapPortrait.CommandsCell(f))) : ButtonRoom(u, u.Rect(PhoneMap.CommandsCell(f)));
        List<(string, float)> words = [.. ButtonWords.Select(w => (w, button))];
        foreach (var cell in cells)
            words.AddRange(CellWords.Select(w => (w, CellRoom(u, cell))));
        return FitAll(Cinzel, u.Size(f.IsPortrait ? PhoneScenes.BarCellText : PhoneScenes.CellText), [.. words]);
    }

    /// <summary>The group's size for <paramref name="word"/> in <paramref name="room"/>: smaller only where a box of its own is narrower than the group's.</summary>
    private static int SizeIn(PhoneUnits u, string word, float room) => Fit(Cinzel, word, GroupSize(u), room);

    private static float ButtonRoom(PhoneUnits u, Rect2 box) => box.Size.X - u.Px(30);

    private static float BarButtonRoom(PhoneUnits u, Rect2 box) => box.Size.X - u.Px(70 + 50 + 20);

    /// <summary>A cell's room for its word: upright (the icon over it) its width less 16; in a side the word centred 150 in, clear of the icon.</summary>
    private static float CellRoom(PhoneUnits u, Rect2 box) =>
        Upright(box) ? box.Size.X - u.Px(16) : 2 * Math.Min(u.Px(150 - 94), box.Size.X - u.Px(150 + 8));

    private static bool Upright(Rect2 box) => box.Size.Y > box.Size.X * 0.6f;

    /// <summary>
    /// The map's button in a side (docs/ui/phone.md, §12, LM1): the side being narrow, its icon over its word (Cinzel 700 at
    /// the side's size) — Commands at the right thumb, Close and Return where it stands.
    /// </summary>
    public static void DrawButton(CanvasItem c, PhoneUnits u, Rect2 box, Icon icon, string word, bool held = false,
        UiPalette? palette = null)
    {
        var ui = palette ?? Ui;
        ui.DrawPanel(c, box, u.K, held ? ui.RowHover : null);
        float cx = box.GetCenter().X;
        DrawIcon(c, u, icon, new Vector2(cx, box.Position.Y + Math.Min(u.Px(48), box.Size.Y * 0.36f)), ui.Gold);
        var face = Cinzel;
        int size = SizeIn(u, word, ButtonRoom(u, box));
        float width = face.GetStringSize(word, fontSize: size).X;
        float middle = box.End.Y - Math.Min(u.Px(40), box.Size.Y * 0.3f);
        c.DrawString(face, new Vector2(cx - width / 2, middle + (face.GetAscent(size) - face.GetDescent(size)) / 2), word,
            fontSize: size, modulate: ui.Name);
    }

    /// <summary>
    /// The map's button at the bar's right held upright (docs/ui/phone.md, §3, PM1): wide, its icon at its left and its word
    /// right of it (Cinzel 700 at the bar's size) — Commands, and Close or Return in its place.
    /// </summary>
    public static void DrawBarButton(CanvasItem c, PhoneUnits u, Rect2 box, Icon icon, string word, bool held = false,
        UiPalette? palette = null)
    {
        var ui = palette ?? Ui;
        ui.DrawPanel(c, box, u.K, held ? ui.RowHover : null);
        float cy = box.GetCenter().Y, iconX = box.Position.X + u.Px(70);
        DrawIcon(c, u, icon, new Vector2(iconX, cy), ui.Gold);
        var face = Cinzel;
        float x0 = iconX + u.Px(50), x1 = box.End.X - u.Px(20);
        int size = SizeIn(u, word, BarButtonRoom(u, box));
        float width = face.GetStringSize(word, fontSize: size).X;
        c.DrawString(face, new Vector2((x0 + x1 - width) / 2, cy + (face.GetAscent(size) - face.GetDescent(size)) / 2), word,
            fontSize: size, modulate: ui.Name);
    }

    /// <summary>
    /// A row across a sheet held upright (docs/ui/phone.md, §3, PM1b, PM1c): with a tag, the tag's band at its left (150
    /// wide, a building under way's weeks on its chip) and the name after it on one line; without one, the name at the left.
    /// Dimmed when off, lit while held. <paramref name="font"/> is the game's; <paramref name="size"/> the rows' one size
    /// (docs/ui/style.md, "Buttons side by side": <see cref="LineRowRoom"/> gives each row's room), else 40 fitted.
    /// </summary>
    public static void DrawLineRow(CanvasItem c, PhoneUnits u, Rect2 box, Font font, string name, bool on = true, bool held = false,
        (string Word, Color Colour)? tag = null, int? weeks = null, UiPalette? palette = null, int? size = null)
    {
        var ui = palette ?? Ui;
        ui.DrawRowBox(c, box, held && on, u.K);
        var ink = on ? ui.Text : ui.OffText;
        float s = LineRowScale(u, box);
        float x = box.Position.X + u.Px(24);
        if (tag is var (word, colour))
        {
            var band = LineRowBand(u, box, font, word, weeks, out int tagSize);
            if (weeks is not null)
                ui.DrawBusyTag(c, font, band, word, colour, u.K, tagSize, weeks);
            else
                ui.DrawTag(c, font, band, word, colour, on, u.K, tagSize);
            x = band.End.X + u.Px(22);
        }
        int px = size ?? Fit(font, name, u.Size(40 * s), box.End.X - x - u.Px(16));
        c.DrawString(font, new Vector2(x, Lacquer.Baseline(box, font, px)), name, fontSize: px,
            modulate: weeks is not null ? ui.Quiet : ink);
    }

    /// <summary>A sheet's row's scale: 1 at 124 reference pixels high, less for a lower row.</summary>
    public static float LineRowScale(PhoneUnits u, Rect2 box) => Math.Min(1, box.Size.Y / u.Px(124));

    private static Rect2 LineRowBand(PhoneUnits u, Rect2 box, Font font, string word, int? weeks, out int tagSize)
    {
        float s = LineRowScale(u, box);
        tagSize = u.Size(30 * s);
        float tagWidth = Math.Max(u.Px(150), font.GetStringSize(word, fontSize: tagSize).X + u.Px(24))
            + (weeks is { } left ? UiPalette.ChipWidth(font, left, tagSize) : 0);
        return new Rect2(box.Position.X + u.Px(14), box.Position.Y + u.Px(16 * s), tagWidth, box.Size.Y - 2 * u.Px(16 * s));
    }

    /// <summary>A sheet's row's room for its name (<see cref="DrawLineRow"/>): from after its tag, if any, to 16 short of its right.</summary>
    public static float LineRowRoom(PhoneUnits u, Rect2 box, Font font, (string Word, Color Colour)? tag = null, int? weeks = null)
    {
        float x = tag is var (word, _) ? LineRowBand(u, box, font, word, weeks, out _).End.X + u.Px(22) : box.Position.X + u.Px(24);
        return box.End.X - x - u.Px(16);
    }

    /// <summary>A rail's row's room for its name (<see cref="DrawRow"/>), tagged or not.</summary>
    public static float RowRoom(PhoneUnits u, Rect2 box) => box.Size.X - u.Px(30);

    /// <summary>A rail's row's scale: 1 at 123 reference pixels high, less for a lower row (a rail with many).</summary>
    public static float RowScale(PhoneUnits u, Rect2 box) => Math.Min(1, box.Size.Y / u.Px(123));

    /// <summary>
    /// A rail's row in a side (docs/ui/phone.md, §12, LM1b, LM1c): with a tag, the tag over the name in two lines (a side has
    /// no room for both in one), a building under way's weeks on its chip; without one, the name centred. Dimmed when off,
    /// lit while held. <paramref name="font"/> is the game's; <paramref name="size"/> the rail's rows' one size (docs/ui/style.md,
    /// "Buttons side by side": <see cref="RowRoom"/> gives each row's room), else <paramref name="nameSize"/> (reference
    /// pixels, a row wider than a side's has room for more) fitted.
    /// </summary>
    public static void DrawRow(CanvasItem c, PhoneUnits u, Rect2 box, Font font, string name, bool on = true, bool held = false,
        (string Word, Color Colour)? tag = null, int? weeks = null, UiPalette? palette = null, float nameSize = 36, int? size = null)
    {
        var ui = palette ?? Ui;
        ui.DrawRowBox(c, box, held && on, u.K);
        var ink = on ? ui.Text : ui.OffText;
        if (tag is not var (word, colour))
        {
            int centred = size ?? Fit(font, name, u.Size(38), RowRoom(u, box));
            float w = font.GetStringSize(name, fontSize: centred).X;
            c.DrawString(font, new Vector2(box.GetCenter().X - w / 2, Lacquer.Baseline(box, font, centred)), name,
                fontSize: centred, modulate: ink);
            return;
        }
        // The tag's band: 36 high, 14 in; the name's line centred 34 over the row's foot (the mock-ups' rail_cell). A low row
        // (a rail with many) keeps the proportions.
        float s = RowScale(u, box);
        int tagSize = u.Size(26 * s);
        float tagWidth = font.GetStringSize(word, fontSize: tagSize).X + u.Px(24 * s);
        if (weeks is { } left)
            tagWidth += UiPalette.ChipWidth(font, left, tagSize);
        var band = new Rect2(box.Position.X + u.Px(14 * s), box.Position.Y + u.Px(14 * s), tagWidth, u.Px(36 * s));
        if (weeks is not null)
            ui.DrawBusyTag(c, font, band, word, colour, u.K, tagSize, weeks);
        else
            ui.DrawTag(c, font, band, word, colour, on, u.K, tagSize);
        int px = size ?? Fit(font, name, u.Size(nameSize * s), RowRoom(u, box));
        var line = new Rect2(box.Position.X, box.End.Y - u.Px(34 * s) - u.Px(20 * s), box.Size.X, u.Px(40 * s));
        c.DrawString(font, new Vector2(box.Position.X + u.Px(16), Lacquer.Baseline(line, font, px)), name, fontSize: px,
            modulate: weeks is not null ? ui.Quiet : ink);
    }

    private static FontFile? _brush;

    /// <summary>The brush face (the screens' names).</summary>
    public static FontFile Brush => _brush ??= Lacquer.Brush();

    /// <summary>Draws the cell in <paramref name="box"/> (layout pixels).</summary>
    public static void Draw(CanvasItem c, PhoneUnits u, Rect2 box, Icon icon, string word, bool lit = false,
        bool disabled = false, bool held = false)
    {
        Ui.DrawPanel(c, box, u.K, lit ? Lit : null);
        if (held && !disabled)
            c.DrawRect(box.Grow(-u.Px(6)), Held);
        var ink = disabled ? Ui.OffText : lit ? Ui.Name : Ui.Gold;
        var font = Cinzel;
        int size = SizeIn(u, word, CellRoom(u, box));
        if (Upright(box))
        {
            // The bar's cell upright (PK1): the icon over the word, as the mock-ups' bottom_bar.
            float cx = box.GetCenter().X;
            DrawIcon(c, u, icon, new Vector2(cx, box.Position.Y + u.Px(58)), ink);
            int small = size;
            float w = font.GetStringSize(word, fontSize: small).X;
            var line = new Rect2(box.Position.X, box.Position.Y + u.Px(96), box.Size.X, u.Px(40));
            c.DrawString(font, new Vector2(cx - w / 2, Lacquer.Baseline(line, font, small)), word, fontSize: small,
                modulate: disabled ? Ui.OffText : Ui.Text);
            return;
        }
        DrawIcon(c, u, icon, new Vector2(box.Position.X + u.Px(60), box.GetCenter().Y), ink);
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
