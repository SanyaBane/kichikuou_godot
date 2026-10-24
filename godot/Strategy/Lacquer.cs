using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The lacquer style's fonts and the parts that take no colour of a palette: a line's width, a text's baseline, a smooth
/// polygon, a dashed frame, a diamond. Its colours and the parts drawn in them are a <see cref="UiPalette"/>'s, each
/// screen's from <see cref="UiPalettes.For"/>. Sizes are in the 1280×800 screen's pixels; <c>k</c> is the window pixels
/// to a screen pixel.
/// </summary>
internal static class Lacquer
{
    public static FontFile LoadFont(string path) => new() { Data = Godot.FileAccess.GetFileAsBytes(path) };

    /// <summary>Shojumaru, the names' brush face.</summary>
    public static FontFile Brush() => LoadFont("res://fonts/Shojumaru-Regular.ttf");

    /// <summary>Cinzel at <paramref name="weight"/> (500 by default), the subtitles' face.</summary>
    public static FontVariation Cinzel(int weight = 500) => new()
    {
        BaseFont = LoadFont("res://fonts/Cinzel-VariableFont_wght.ttf"),
        VariationOpentype = new Godot.Collections.Dictionary
        {
            [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = weight,
        },
    };

    /// <summary>
    /// A line's width, at least one window pixel: a thinner one may fall between the pixels' centres when the
    /// screen is scaled down and vanish (the first row's top edge did, owner, 2026-10-16).
    /// </summary>
    public static float Line(float width, float k) => Math.Max(width, 1 / k);

    /// <summary>
    /// A filled polygon with smooth edges. Godot fills a coloured polygon without antialiasing, and a slanted edge shows
    /// its steps even under an antialiased outline (the deployment screen's facing head, owner, 2026-10-24): the shape is
    /// rasterized here at the window's own pixels, each pixel's cover sampled 4×4, and drawn as a texture, kept for the
    /// next frame that draws the same shape at the same size. <paramref name="k"/> is the draw transform's scale
    /// (<c>DrawSetTransform</c>), which the canvas item's own transform does not include.
    /// </summary>
    public static void DrawSmoothPolygon(CanvasItem c, Vector2[] points, Color colour, float k)
    {
        var toWindow = c.GetViewport().GetFinalTransform() * c.GetGlobalTransformWithCanvas();
        float scale = toWindow.X.Length() * k;                      // the window's own pixels to a unit of the points
        var min = points.Aggregate(points[0], (a, p) => a.Min(p)).Floor() - Vector2.One;
        var max = points.Aggregate(points[0], (a, p) => a.Max(p)).Ceil() + Vector2.One;
        int w = Math.Max(1, (int)Math.Ceiling((max.X - min.X) * scale)), h = Math.Max(1, (int)Math.Ceiling((max.Y - min.Y) * scale));
        var shape = points.Select(p => (p - min) * scale).ToArray();
        string key = $"{string.Join(";", shape.Select(p => $"{p.X:F2},{p.Y:F2}"))}|{colour.ToRgba32()}|{w}x{h}";
        if (!SmoothFills.TryGetValue(key, out var texture))
        {
            const int Samples = 4;
            var image = Image.CreateEmpty(w, h, false, Image.Format.Rgba8);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int cover = 0;
                    for (int sy = 0; sy < Samples; sy++)
                        for (int sx = 0; sx < Samples; sx++)
                            if (Inside(shape, new Vector2(x + (sx + 0.5f) / Samples, y + (sy + 0.5f) / Samples)))
                                cover++;
                    image.SetPixel(x, y, colour with { A = colour.A * cover / (Samples * Samples) });
                }
            SmoothFills[key] = texture = ImageTexture.CreateFromImage(image);
        }
        c.DrawTextureRect(texture, new Rect2(min, new Vector2(w, h) / scale), false);
    }

    private static readonly Dictionary<string, ImageTexture> SmoothFills = [];

    /// <summary>Whether <paramref name="p"/> is inside <paramref name="polygon"/> (the crossings of a ray to the right).</summary>
    private static bool Inside(Vector2[] polygon, Vector2 p)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
        {
            Vector2 a = polygon[i], b = polygon[j];
            if ((a.Y > p.Y) != (b.Y > p.Y) && p.X < (b.X - a.X) * (p.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    /// <summary>The baseline that centres text of <paramref name="size"/> in <paramref name="rect"/>.</summary>
    public static float Baseline(Rect2 rect, Font font, int size) =>
        rect.Position.Y + rect.Size.Y / 2 + (font.GetAscent(size) - font.GetDescent(size)) / 2;

    /// <summary>A frame of short dashes around <paramref name="rect"/>: an empty place, or one not known yet.</summary>
    public static void DrawDashed(CanvasItem c, Rect2 rect, Color colour, float k, float dash = 8, float step = 14)
    {
        float width = Line(1, k);
        for (float x = rect.Position.X; x < rect.End.X; x += step)
        {
            float x1 = Math.Min(x + dash, rect.End.X);
            c.DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x1, rect.Position.Y), colour, width);
            c.DrawLine(new Vector2(x, rect.End.Y), new Vector2(x1, rect.End.Y), colour, width);
        }
        for (float y = rect.Position.Y; y < rect.End.Y; y += step)
        {
            float y1 = Math.Min(y + dash, rect.End.Y);
            c.DrawLine(new Vector2(rect.Position.X, y), new Vector2(rect.Position.X, y1), colour, width);
            c.DrawLine(new Vector2(rect.End.X, y), new Vector2(rect.End.X, y1), colour, width);
        }
    }

    /// <summary>A small diamond centred at <paramref name="at"/>.</summary>
    public static void DrawDiamond(CanvasItem c, Vector2 at, float r, Color colour) =>
        c.DrawColoredPolygon([at + new Vector2(0, -r), at + new Vector2(r, 0), at + new Vector2(0, r), at + new Vector2(-r, 0)], colour);
}
