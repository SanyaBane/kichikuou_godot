using Godot;
using Kichikuou.Game.Strategy;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The backdrop under the battle board's cards (owner, 2026-10-24, mock-ups S3 and S4; docs/ui/battle-screen.md, "The
/// board's backdrop"): Sengoku Rance's broken stones in spirit, drawn by code — angular stones packed edge to edge, big
/// ones first and small ones in what is left, dark gaps between, each lit from the top left with a shaded rim, the
/// whole softened and grained; darker under the armies' names along the top and under the gauge's caption, so they
/// read. In the palette's <see cref="UiPalette.Stone"/> and <see cref="UiPalette.StoneGap"/>; the same stones every
/// time (a fixed seed). Drawn at twice the layout's pixels and kept at them, each four pixels averaged, once a palette
/// (docs/memory.md, cut 10: kept at twice them, the phone's two pictures took 36 MB on the GPU).
/// </summary>
internal static class BoardStones
{
    private const int Scale = 2;
    private const int Seed = 5;

    // The stones' sizes (radius in layout pixels) and how many places each size tries, biggest first.
    private static readonly (float Radius, int Tries)[] Sizes =
        [(46, 500), (34, 1000), (26, 1800), (19, 3000), (13, 5000), (9, 7000), (6, 9000), (4, 9000)];

    private const float GapWidth = 2;                       // layout pixels kept clear round a stone
    private const float TopShade = 40, TopDark = 0.65f;     // the band under the armies' names: this much darker at the top
    private const float CaptionBlur = 14, CaptionDark = 0.55f;
    private const double Grain = 6;                         // the grain's spread, in levels of a 128 midpoint

    private static readonly Dictionary<(Color, Color, Rect2, Rect2), ImageTexture> Made = [];

    /// <summary>
    /// The stones in <paramref name="ui"/>'s colours over <paramref name="area"/> (layout pixels: under the separator to the
    /// screen's foot, the phone's taller), darker round <paramref name="caption"/>.
    /// </summary>
    public static ImageTexture For(UiPalette ui, Rect2 caption, Rect2 area)
    {
        var key = (ui.Stone, ui.StoneGap, caption, area);
        if (!Made.TryGetValue(key, out var texture))
        {
            var picture = new Picture(area);
            using var image = Image.CreateFromData(picture.Width / Scale, picture.Height / Scale, false, Image.Format.Rgb8,
                picture.Build(ui.Stone, ui.StoneGap, caption));
            Made[key] = texture = ImageTexture.CreateFromImage(image);
        }
        return texture;
    }

    /// <summary>One picture of the stones over <paramref name="area"/>, drawn at <see cref="Scale"/> times its layout pixels.</summary>
    private sealed class Picture(Rect2 area)
    {
        public readonly int Width = (int)area.Size.X * Scale, Height = (int)area.Size.Y * Scale;
        private Rect2 Area => area;

        /// <summary>The picture as RGB bytes at the layout's pixels, <see cref="Width"/> × <see cref="Height"/> drawn and averaged down.</summary>
        public byte[] Build(Color stone, Color gap, Rect2 caption)
        {
            var rgb = new float[Width * Height * 3];
            for (int i = 0; i < Width * Height; i++)
                (rgb[i * 3], rgb[i * 3 + 1], rgb[i * 3 + 2]) = (gap.R8, gap.G8, gap.B8);
            var taken = new bool[Width * Height];
            var rnd = new Random(Seed);
            float U(double a, double b) => (float)(a + rnd.NextDouble() * (b - a));
            float grey = (stone.R8 + stone.G8 + stone.B8) / 3f;
            var probe = new List<Vector2>();
            foreach (var (radius, tries) in Sizes)
            {
                float r0 = radius * Scale;
                for (int t = 0; t < tries; t++)
                {
                    var c = new Vector2(U(-r0, Width + r0), U(-r0, Height + r0));
                    float r = r0 * U(0.75, 1.2);
                    int n = rnd.Next(4, 8);
                    float start = U(0, Math.Tau), squash = U(0.5, 1), turn = U(0, Math.PI);
                    var points = new Vector2[n];
                    for (int k = 0; k < n; k++)
                    {
                        float a = start + k * MathF.Tau / n + U(-0.4, 0.4), rr = r * U(0.6, 1);
                        var e = new Vector2(rr * MathF.Cos(a), rr * squash * MathF.Sin(a));
                        points[k] = c + e.Rotated(turn);
                    }
                    probe.Clear();
                    probe.Add(c);
                    foreach (var p in points)
                    {
                        probe.Add(c + (p - c) * 0.55f);
                        probe.Add(c + (p - c) * 0.92f);
                    }
                    if (probe.Any(p => p.X >= 0 && p.Y >= 0 && p.X < Width && p.Y < Height && taken[(int)p.Y * Width + (int)p.X]))
                        continue;
                    Fill(Grown(points, c, GapWidth * Scale), (x, y) => taken[y * Width + x] = true);

                    float toward = U(0, 0.45), bright = U(0.74, 1.14);         // some stones greyer, some darker
                    float[] tone = [.. new[] { stone.R8, stone.G8, stone.B8 }.Select(v => (v + (grey - v) * toward) * bright)];
                    Paint(rgb, points, tone, 0.62f);                                                          // the shaded rim
                    Paint(rgb, [.. points.Select(p => c + (p - c) * 0.86f - new Vector2(r, r) * 0.07f)], tone, 1);
                    Paint(rgb, [.. points.Select(p => new Vector2(c.X + (p.X - c.X) * 0.55f - r * 0.2f,
                        c.Y + (p.Y - c.Y) * 0.5f - r * 0.22f))], tone, 1.07f);                                // the light on its face
                }
            }
            rgb = Soften(rgb);
            Shade(rgb, rnd, caption);
            return Kept(rgb);
        }

        /// <summary>Each <see cref="Scale"/> × <see cref="Scale"/> pixels averaged into one: what the GPU showed of the drawn picture at 1:1.</summary>
        private byte[] Kept(float[] rgb)
        {
            int across = Width / Scale, down = Height / Scale;
            var bytes = new byte[across * down * 3];
            for (int y = 0; y < down; y++)
                for (int x = 0; x < across; x++)
                    for (int k = 0; k < 3; k++)
                    {
                        float sum = 0;
                        for (int dy = 0; dy < Scale; dy++)
                            for (int dx = 0; dx < Scale; dx++)
                                sum += rgb[((y * Scale + dy) * Width + x * Scale + dx) * 3 + k];
                        bytes[(y * across + x) * 3 + k] = (byte)Math.Clamp((int)Math.Round(sum / (Scale * Scale)), 0, 255);
                    }
            return bytes;
        }

        /// <summary><paramref name="points"/> moved <paramref name="by"/> pixels further from <paramref name="c"/>.</summary>
        private static Vector2[] Grown(Vector2[] points, Vector2 c, float by) =>
            [.. points.Select(p => c + (p - c) * ((p - c).Length() + by) / Math.Max(1, (p - c).Length()))];

        private void Paint(float[] rgb, Vector2[] points, float[] tone, float factor) =>
            Fill(points, (x, y) =>
            {
                int i = (y * Width + x) * 3;
                for (int k = 0; k < 3; k++)
                    rgb[i + k] = Math.Min(255, tone[k] * factor);
            });

        /// <summary>Calls <paramref name="pixel"/> for every pixel whose centre is inside the polygon (even-odd), clipped to the picture.</summary>
        private void Fill(Vector2[] points, Action<int, int> pixel)
        {
            float top = points.Min(p => p.Y), bottom = points.Max(p => p.Y);
            var crossings = new List<float>();
            for (int y = Math.Max(0, (int)MathF.Floor(top)); y <= Math.Min(Height - 1, (int)MathF.Ceiling(bottom)); y++)
            {
                float cy = y + 0.5f;
                crossings.Clear();
                for (int k = 0; k < points.Length; k++)
                {
                    Vector2 a = points[k], b = points[(k + 1) % points.Length];
                    if ((a.Y <= cy) != (b.Y <= cy))
                        crossings.Add(a.X + (cy - a.Y) / (b.Y - a.Y) * (b.X - a.X));
                }
                crossings.Sort();
                for (int k = 0; k + 1 < crossings.Count; k += 2)
                    for (int x = Math.Max(0, (int)MathF.Ceiling(crossings[k] - 0.5f)); x <= Math.Min(Width - 1, (int)MathF.Floor(crossings[k + 1] - 0.5f)); x++)
                        pixel(x, y);
            }
        }

        /// <summary>A 1-2-1 blur across and down: the stones' edges soft.</summary>
        private float[] Soften(float[] rgb)
        {
            var across = new float[rgb.Length];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    for (int k = 0; k < 3; k++)
                    {
                        int l = Math.Max(0, x - 1), r = Math.Min(Width - 1, x + 1);
                        across[(y * Width + x) * 3 + k] = (rgb[(y * Width + l) * 3 + k] + 2 * rgb[(y * Width + x) * 3 + k] + rgb[(y * Width + r) * 3 + k]) / 4;
                    }
            var down = new float[rgb.Length];
            for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    for (int k = 0; k < 3; k++)
                    {
                        int u = Math.Max(0, y - 1), d = Math.Min(Height - 1, y + 1);
                        down[(y * Width + x) * 3 + k] = (across[(u * Width + x) * 3 + k] + 2 * across[(y * Width + x) * 3 + k] + across[(d * Width + x) * 3 + k]) / 4;
                    }
            return down;
        }

        /// <summary>The grain (one draw a layout pixel), the band along the top and the shade round the caption.</summary>
        private void Shade(float[] rgb, Random rnd, Rect2 caption)
        {
            int cellsAcross = Width / Scale;
            var grain = new float[cellsAcross * (Height / Scale)];
            for (int i = 0; i < grain.Length; i++)
            {
                double u1 = 1 - rnd.NextDouble(), u2 = rnd.NextDouble();
                grain[i] = (float)(1 + Grain * Math.Sqrt(-2 * Math.Log(u1)) * Math.Cos(Math.Tau * u2) / 128);
            }
            var spot = new Rect2(caption.Position - Area.Position, caption.Size);
            for (int y = 0; y < Height; y++)
            {
                float ly = (y + 0.5f) / Scale;
                float band = ly < TopShade ? 1 - TopDark * (1 - ly / TopShade) : 1;
                for (int x = 0; x < Width; x++)
                {
                    float lx = (x + 0.5f) / Scale;
                    float dx = Math.Max(Math.Max(spot.Position.X - lx, lx - spot.End.X), 0);
                    float dy = Math.Max(Math.Max(spot.Position.Y - ly, ly - spot.End.Y), 0);
                    float s = Math.Clamp(1 - MathF.Sqrt(dx * dx + dy * dy) / (2 * CaptionBlur), 0, 1);  // 1 inside, 0 far off
                    float near = s * s * (3 - 2 * s);
                    float f = band * (1 - CaptionDark * near) * grain[(y / Scale) * cellsAcross + x / Scale];
                    int i = (y * Width + x) * 3;
                    for (int k = 0; k < 3; k++)
                        rgb[i + k] *= f;
                }
            }
        }
    }
}
