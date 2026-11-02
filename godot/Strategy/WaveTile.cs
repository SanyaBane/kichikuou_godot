using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The seigaiha wave pattern of our screens' backdrop as one repeating picture (docs/memory.md): drawn wave by wave, as filled
/// circles and antialiased arcs, it took ~95 MB of Godot's memory and 66 MB of GPU buffers per screen on the phone held upright.
/// The waves' circles of radius <see cref="R"/> stand in rows <see cref="R"/>/2 apart, 2<see cref="R"/> apart along a row, every
/// other row half a step along; each row covers the one before, so a tile of 2<see cref="R"/> × <see cref="R"/> repeats exactly.
/// It is rasterized at the window's own pixels and kept for the launch, one per look (a palette's colours, the scale, the line).
/// </summary>
internal static class WaveTile
{
    private const float R = 34, StepX = 2 * R, StepY = R / 2;
    private static readonly float[] Radii = [R, R * 0.72f, R * 0.44f];

    private static readonly Dictionary<(Color, Color, int, int, int), (CanvasTexture Texture, Vector2I Pixels)> Tiles = [];

    /// <summary>Fills <paramref name="area"/> of <paramref name="c"/> with the pattern from its origin; <paramref name="k"/> is the draw transform's scale, <paramref name="line"/> an arc's width, both as in <see cref="Lacquer.DrawSmoothPolygon"/>.</summary>
    public static void Draw(CanvasItem c, Rect2 area, Color lacquer, Color wave, float line, float k)
    {
        var toWindow = c.GetViewport().GetFinalTransform() * c.GetGlobalTransformWithCanvas();
        float scale = Math.Max(toWindow.X.Length() * k, 0.25f);                 // the window's pixels to a unit; 0 while minimized
        var pixels = new Vector2I(Math.Max(1, (int)Math.Round(StepX * scale)), Math.Max(1, (int)Math.Round(R * scale)));
        var key = (lacquer, wave, pixels.X, pixels.Y, (int)Math.Round(line * scale * 100));
        if (!Tiles.TryGetValue(key, out var tile))
            Tiles[key] = tile = (Make(pixels, lacquer, wave, line, pixels.X / StepX), pixels);
        var source = new Rect2(area.Position.X / StepX * pixels.X, area.Position.Y / R * pixels.Y, area.Size.X / StepX * pixels.X, area.Size.Y / R * pixels.Y);
        c.DrawTextureRectRegion(tile.Texture, area, source, clipUV: false);
    }

    private static CanvasTexture Make(Vector2I pixels, Color lacquer, Color wave, float line, float scale)
    {
        var rgba = new byte[pixels.X * pixels.Y * 4];
        for (int y = 0; y < pixels.Y; y++)
        {
            for (int x = 0; x < pixels.X; x++)
            {
                var colour = Pixel((x + 0.5f) / pixels.X * StepX, (y + 0.5f) / pixels.Y * R, lacquer, wave, line, scale);
                int at = (y * pixels.X + x) * 4;
                rgba[at] = (byte)colour.R8;
                rgba[at + 1] = (byte)colour.G8;
                rgba[at + 2] = (byte)colour.B8;
                rgba[at + 3] = 255;
            }
        }
        using var image = Image.CreateFromData(pixels.X, pixels.Y, false, Image.Format.Rgba8, rgba);
        return new CanvasTexture { DiffuseTexture = ImageTexture.CreateFromImage(image), TextureRepeat = CanvasItem.TextureRepeatEnum.Enabled };
    }

    /// <summary>The colour at a point of the tile: from the last drawn row back, the outer arc of each wave just outside it, then the wave that covers the point with its three arcs.</summary>
    private static Color Pixel(float x, float y, Color lacquer, Color wave, float line, float scale)
    {
        var colour = lacquer;
        for (int row = (int)MathF.Ceiling((y + R + line) / StepY); ; row--)
        {
            float along = x - (row % 2 == 0 ? 0 : R);
            float dx = along - MathF.Round(along / StepX) * StepX, dy = y - row * StepY;
            float d = MathF.Sqrt(dx * dx + dy * dy);
            if (d > R)
            {
                colour = Over(colour, wave, Cover(d, R, line, scale));
                continue;
            }
            foreach (float radius in Radii)
                colour = Over(colour, wave, Cover(d, radius, line, scale));
            return colour;
        }
    }

    /// <summary>How much of a window pixel an arc of <paramref name="radius"/> covers at distance <paramref name="d"/> from its centre.</summary>
    private static float Cover(float d, float radius, float line, float scale) =>
        Math.Clamp((line / 2 - MathF.Abs(d - radius)) * scale + 0.5f, 0, 1);

    private static Color Over(Color under, Color wave, float cover) => under.Lerp(wave with { A = 1 }, wave.A * cover);
}
