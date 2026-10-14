using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The pictures our exchange effects are made of, drawn by code (docs/ui/battle-screen.md, "The exchange's
/// effects": none of Sengoku Rance's art). Each is white with the shape in its alpha, so a draw tints it, but the
/// crescent's two parts, which are tinted apart. Made once, on first use.
/// </summary>
internal static class EffectTextures
{
    /// <summary>A soft round puff of dust: a noisy disc fading to its edge, with a fine grain of bright specks.</summary>
    public static Texture2D Puff => _puff ??= Make(128, PuffShape);

    /// <summary>A crescent's body: a disc less an offset disc, a thin blade with its hollow facing left (−x), the upper tip a fading tail.</summary>
    public static Texture2D CrescentBody => _crescentBody ??= Make(256, CrescentBodyShape);

    /// <summary>The crescent's inner edge: a bright rim along its hollow and a few thin arcs trailing inside it.</summary>
    public static Texture2D CrescentEdge => _crescentEdge ??= Make(256, CrescentEdgeShape);

    /// <summary>An "urchin": a soft ring with fine radial spikes around an empty middle.</summary>
    public static Texture2D Urchin => _urchin ??= Make(128, UrchinShape);

    /// <summary>A four-point sparkle: two thin crossed rays tapering from a small bright core.</summary>
    public static Texture2D Sparkle => _sparkle ??= Make(128, SparkleShape);

    /// <summary>A small thin ring.</summary>
    public static Texture2D Ring => _ring ??= Make(64, RingShape);

    /// <summary>A short thin streak along x, tapered at both ends.</summary>
    public static Texture2D Dash => _dash ??= Make(64, DashShape);

    /// <summary>A soft dot (a speck of the dust's grain).</summary>
    public static Texture2D Dot => _dot ??= Make(16, DotShape);

    private static Texture2D? _puff, _crescentBody, _crescentEdge, _urchin, _sparkle, _ring, _dash, _dot;

    /// <summary>A size×size texture whose alpha at each pixel is <paramref name="alpha"/> of (x, y) in [−1, 1].</summary>
    private static Texture2D Make(int size, Func<float, float, float> alpha)
    {
        var data = new byte[size * size * 4];
        for (int py = 0; py < size; py++)
        {
            for (int px = 0; px < size; px++)
            {
                float x = (px + 0.5f) / size * 2 - 1, y = (py + 0.5f) / size * 2 - 1;
                int at = (py * size + px) * 4;
                data[at] = data[at + 1] = data[at + 2] = 255;
                data[at + 3] = (byte)Math.Round(Math.Clamp(alpha(x, y), 0, 1) * 255);
            }
        }
        var image = Image.CreateFromData(size, size, false, Image.Format.Rgba8, data);
        image.GenerateMipmaps();
        return ImageTexture.CreateFromImage(image);
    }

    private static float PuffShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        float disc = 1 - SmoothStep(0.5f, 1f, r);
        float noise = 0.6f + 0.4f * Fbm(x * 4 + 7, y * 4 + 3);
        float grain = Hash(MathF.Floor((x + 1) * 64), MathF.Floor((y + 1) * 64)) > 0.94f ? 0.5f : 0;
        return disc * (noise + grain);
    }

    // The crescent: the outer disc's radius, the hollow's offset (left and a little down) and its radius — a thin
    // blade, thickest right and below, its upper tip a long fading tail.
    private const float OuterR = 0.95f, HollowX = -0.2f, HollowY = 0.05f, HollowR = 0.88f;

    private static float CrescentBodyShape(float x, float y)
    {
        float outer = 1 - SmoothStep(OuterR - 0.02f, OuterR, MathF.Sqrt(x * x + y * y));
        float hollow = SmoothStep(HollowR - 0.01f, HollowR + 0.01f, Hollow(x, y));
        return outer * hollow * Tail(x, y);
    }

    private static float Hollow(float x, float y) => MathF.Sqrt((x - HollowX) * (x - HollowX) + (y - HollowY) * (y - HollowY));

    /// <summary>The upper tip fades out as a trail.</summary>
    private static float Tail(float x, float y) => MathF.Pow(SmoothStep(-2.1f, -0.9f, MathF.Atan2(y, x)), 0.7f);

    private static float CrescentEdgeShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        float h = Hollow(x, y);
        float outer = (1 - SmoothStep(OuterR - 0.02f, OuterR, r)) * Tail(x, y);
        // The rim: bright just outside the hollow, inside the body.
        float rim = outer * Gauss(h - HollowR - 0.03f, 0.025f);
        // The trailing arcs: thin, concentric with the hollow, inside it, fading toward the lower tip (+y side).
        float angle = MathF.Atan2(y - HollowY, x - HollowX);          // 0 to the right, the body's thick part
        float along = Math.Clamp(1 - (angle + 0.6f) / 2.4f, 0, 1);    // full above the thick part, gone near the lower tip
        float arcs = 0;
        foreach (var (offset, width, strength) in Arcs)
            arcs += strength * Gauss(h - HollowR + offset, width);
        arcs *= along * SmoothStep(-1.9f, -0.9f, angle) * (1 - SmoothStep(0.9f, 1.5f, angle));
        return rim + arcs;
    }

    private static readonly (float Offset, float Width, float Strength)[] Arcs =
        [(0.02f, 0.012f, 0.9f), (0.06f, 0.01f, 0.6f), (0.11f, 0.008f, 0.45f), (0.17f, 0.007f, 0.3f)];

    private static readonly float[] Spikes = MakeSpikes();

    private static float[] MakeSpikes()
    {
        var random = new Random(40000);
        var spikes = new float[160];
        for (int i = 0; i < spikes.Length; i += 2)
        {
            spikes[i] = (float)(random.NextDouble() * Math.Tau);           // angle
            spikes[i + 1] = 0.25f + (float)random.NextDouble() * 0.55f;    // length from inside the ring
        }
        return spikes;
    }

    private static float UrchinShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        if (r > 1)
            return 0;
        float angle = MathF.Atan2(y, x);
        float spikes = 0;
        for (int i = 0; i < Spikes.Length; i += 2)
        {
            float d = MathF.Abs(MathF.IEEERemainder(angle - Spikes[i], MathF.Tau)) * r;  // across the ray
            float t = (r - 0.28f) / Spikes[i + 1];                                       // along it
            if (t is > 0 and < 1)
                spikes += Gauss(d, 0.014f) * MathF.Sin(MathF.PI * MathF.Sqrt(t)) * (1 - t);
        }
        float ring = 0.45f * Gauss(r - 0.42f, 0.07f);
        float glow = 0.3f * Gauss(r - 0.45f, 0.18f);
        return (ring + 0.8f * spikes + glow) * (1 - SmoothStep(0.8f, 1f, r));
    }

    private static float SparkleShape(float x, float y)
    {
        float ax = MathF.Abs(x), ay = MathF.Abs(y);
        float rays = Gauss(y, 0.012f + 0.02f * (1 - ax)) * MathF.Pow(1 - Math.Min(ax, 1), 3)
            + Gauss(x, 0.012f + 0.02f * (1 - ay)) * MathF.Pow(1 - Math.Min(ay, 1), 3);
        float core = Gauss(MathF.Sqrt(x * x + y * y), 0.08f);
        return rays + core;
    }

    private static float RingShape(float x, float y) => Gauss(MathF.Sqrt(x * x + y * y) - 0.7f, 0.08f);

    private static float DashShape(float x, float y) => Gauss(y, 0.06f) * MathF.Pow(Math.Max(0, 1 - x * x), 2);

    private static float DotShape(float x, float y) => Gauss(MathF.Sqrt(x * x + y * y), 0.35f);

    private static float Gauss(float d, float width) => MathF.Exp(-(d * d) / (2 * width * width));

    private static float SmoothStep(float from, float to, float v)
    {
        float t = Math.Clamp((v - from) / (to - from), 0, 1);
        return t * t * (3 - 2 * t);
    }

    private static float Hash(float x, float y)
    {
        float h = MathF.Sin(x * 127.1f + y * 311.7f) * 43758.5453f;
        return h - MathF.Floor(h);
    }

    private static float ValueNoise(float x, float y)
    {
        float ix = MathF.Floor(x), iy = MathF.Floor(y), fx = x - ix, fy = y - iy;
        float sx = fx * fx * (3 - 2 * fx), sy = fy * fy * (3 - 2 * fy);
        float a = Hash(ix, iy), b = Hash(ix + 1, iy), c = Hash(ix, iy + 1), d = Hash(ix + 1, iy + 1);
        return a + (b - a) * sx + (c - a) * sy + (a - b - c + d) * sx * sy;
    }

    private static float Fbm(float x, float y) =>
        0.5f * ValueNoise(x, y) + 0.3f * ValueNoise(x * 2.1f, y * 2.1f) + 0.2f * ValueNoise(x * 4.3f, y * 4.3f);
}
