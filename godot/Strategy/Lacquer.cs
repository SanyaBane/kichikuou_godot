using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The lacquer-and-gold style of our map panels (the owner's mock-up 4a, docs/ui/command-screen.md): the
/// colours, the fonts, and the parts both the province menu (<see cref="ProvinceMenuView"/>) and the command
/// panel (<see cref="CommandMenuView"/>) draw — the panel with its double gold frame and corners, a row's box,
/// a tag, a divider. Sizes are in the 1280×800 screen's pixels; <c>k</c> is the window pixels to a screen pixel.
/// </summary>
internal static class Lacquer
{
    public static readonly Color Panel = new(22 / 255f, 16 / 255f, 12 / 255f, 0.96f);
    public static readonly Color Gold = Color.Color8(196, 160, 86);
    public static readonly Color DimGold = Color.Color8(110, 86, 46);
    public static readonly Color Name = Color.Color8(232, 199, 122);
    public static readonly Color Subtitle = Color.Color8(160, 140, 104);
    public static readonly Color Label = Color.Color8(150, 134, 108);
    public static readonly Color Value = Color.Color8(236, 226, 204);
    public static readonly Color Row = Color.Color8(34, 26, 20);
    public static readonly Color RowHover = Color.Color8(64, 48, 28);
    public static readonly Color HoverLine = Color.Color8(222, 186, 104);
    public static readonly Color Text = Color.Color8(244, 232, 206);
    public static readonly Color Quiet = Color.Color8(176, 160, 130);
    public static readonly Color OffText = Color.Color8(98, 88, 74);
    public static readonly Color OffTag = Color.Color8(52, 46, 40);
    public static readonly Color SpentPip = Color.Color8(60, 50, 38);

    public static FontFile LoadFont(string path) => new() { Data = Godot.FileAccess.GetFileAsBytes(path) };

    /// <summary>Shojumaru, the names' brush face.</summary>
    public static FontFile Brush() => LoadFont("res://fonts/Shojumaru-Regular.ttf");

    /// <summary>Cinzel at weight 500, the subtitles' face.</summary>
    public static FontVariation Cinzel() => new()
    {
        BaseFont = LoadFont("res://fonts/Cinzel-VariableFont_wght.ttf"),
        VariationOpentype = new Godot.Collections.Dictionary
        {
            [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = 500,
        },
    };

    /// <summary>
    /// A line's width, at least one window pixel: a thinner one may fall between the pixels' centres when the
    /// screen is scaled down and vanish (the first row's top edge did, owner, 2026-10-16).
    /// </summary>
    public static float Line(float width, float k) => Math.Max(width, 1 / k);

    /// <summary>The baseline that centres text of <paramref name="size"/> in <paramref name="rect"/>.</summary>
    public static float Baseline(Rect2 rect, Font font, int size) =>
        rect.Position.Y + rect.Size.Y / 2 + (font.GetAscent(size) - font.GetDescent(size)) / 2;

    /// <summary>The panel: its shadow, the lacquer, the double gold frame and its corners.</summary>
    public static void DrawPanel(CanvasItem c, Rect2 box, float k)
    {
        float x = box.Position.X, y = box.Position.Y, w = box.Size.X, h = box.Size.Y;
        for (int i = 6; i >= 1; i--)
            c.DrawRect(new Rect2(x - i + 2, y - i + 6, w + 2 * i, h + 2 * i), new Color(0, 0, 0, 0.06f));
        c.DrawRect(box, Panel);
        c.DrawRect(new Rect2(x + 1, y + 1, w - 2, h - 2), Gold, false, Line(2, k));
        c.DrawRect(new Rect2(x + 5.5f, y + 5.5f, w - 11, h - 11), DimGold, false, Line(1, k));
        foreach (var (cx, cy, sx, sy) in new[] { (x, y, 1, 1), (x + w, y, -1, 1), (x, y + h, 1, -1), (x + w, y + h, -1, -1) })
        {
            c.DrawPolyline([new(cx + sx * 9, cy + sy * 2), new(cx + sx * 9, cy + sy * 9), new(cx + sx * 2, cy + sy * 9)], Gold, Line(2, k));
            c.DrawColoredPolygon([new(cx + sx * 9, cy + sy * 14), new(cx + sx * 14, cy + sy * 9), new(cx + sx * 9, cy + sy * 4), new(cx + sx * 4, cy + sy * 9)], Gold);
        }
    }

    /// <summary>A row's box: dark with a dim frame, or lit with a bright one when hovered.</summary>
    public static void DrawRowBox(CanvasItem c, Rect2 rect, bool hover, float k)
    {
        c.DrawRect(rect, hover ? RowHover : Row);
        c.DrawRect(rect, hover ? HoverLine : DimGold, false, Line(hover ? 2 : 1, k));
    }

    /// <summary>A command's tag: its colour framed in gold, or grey when the command is off.</summary>
    public static void DrawTag(CanvasItem c, Font font, Rect2 rect, string tag, Color colour, bool on, float k)
    {
        c.DrawRect(rect, on ? colour : OffTag);
        if (on)
            c.DrawRect(rect, Gold, false, Line(1, k));
        float tw = font.GetStringSize(tag, fontSize: 13).X;
        c.DrawString(font, new Vector2(rect.GetCenter().X - tw / 2, Baseline(rect, font, 13)), tag, fontSize: 13, modulate: on ? Text : OffText);
    }

    /// <summary>A divider: two dim rules with a gold diamond between them.</summary>
    public static void DrawDivider(CanvasItem c, float x0, float x1, float y, float k)
    {
        float m = (x0 + x1) / 2;
        c.DrawLine(new Vector2(x0, y), new Vector2(m - 10, y), DimGold, Line(1, k));
        c.DrawLine(new Vector2(m + 10, y), new Vector2(x1, y), DimGold, Line(1, k));
        c.DrawColoredPolygon([new(m, y - 4), new(m + 4, y), new(m, y + 4), new(m - 4, y)], Gold);
    }
}
