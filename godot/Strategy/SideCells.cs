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
/// its word right of it — Menu, Close, Skip, Log. Lit gold (Skip while the skip runs, Close while what it closes is up); a
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

    /// <summary>Draws the cell in <paramref name="box"/> (layout pixels).</summary>
    public static void Draw(CanvasItem c, PhoneUnits u, Rect2 box, Icon icon, string word, bool lit = false,
        bool disabled = false, bool held = false)
    {
        Ui.DrawPanel(c, box, u.K, lit ? Lit : null);
        if (held && !disabled)
            c.DrawRect(box.Grow(-u.Px(6)), Held);
        var ink = disabled ? Ui.OffText : lit ? Ui.Name : Ui.Gold;
        DrawIcon(c, u, icon, new Vector2(box.Position.X + u.Px(60), box.GetCenter().Y), ink);
        int size = u.Size(36);
        var font = Font;
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
