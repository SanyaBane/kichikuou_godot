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

    /// <summary>An arrow along x, its head at +x: a thin shaft, a pointed head and a V of fletching at the back (8:1).</summary>
    public static Texture2D Arrow => _arrow ??= Make(256, ArrowShape, 32);

    /// <summary>A bow's release: thin spikes of every length bursting from a small bright core.</summary>
    public static Texture2D Burst => _burst ??= Make(128, BurstShape);

    /// <summary>A bow: a thin arc bulging toward +x, ±60° about it, its ends tapering.</summary>
    public static Texture2D BowArc => _bowArc ??= Make(128, BowArcShape);

    /// <summary>An arrow's hit: a "crown", the upper half of a ring with jagged points outward, and a glow.</summary>
    public static Texture2D Splat => _splat ??= Make(128, SplatShape);

    /// <summary>A bokeh disc: flat inside, a slightly brighter rim, a soft edge.</summary>
    public static Texture2D Bokeh => _bokeh ??= Make(64, BokehShape);

    private static Texture2D? _puff, _crescentBody, _crescentEdge, _urchin, _sparkle, _ring, _dash, _dot;
    /// <summary>A soft glow: a bright core in a wide falling halo.</summary>
    public static Texture2D Glow => _glow ??= Make(128, GlowShape);

    /// <summary>A flare's rays: many fine lines from the centre, fading outward.</summary>
    public static Texture2D Rays => _rays ??= Make(256, RaysShape);

    /// <summary>A shockwave: a soft wide band on an arc bulging toward +x, ±70° about it, a faint glassy fill inside.</summary>
    public static Texture2D Shock => _shock ??= Make(128, ShockShape);

    private static Texture2D? _arrow, _burst, _bowArc, _splat, _bokeh, _glow, _rays, _shock;

    /// <summary>A spike along x, its point at +x: a long thin wedge, widest near its foot (−x), with a bright spine.</summary>
    public static Texture2D Spike => _spike ??= Make(256, SpikeShape, 32);

    /// <summary>A flame's tongue: a filled blade rising from a wide foot (+y) to a point leaning right at the top, its left edge brightest.</summary>
    public static Texture2D Tongue => _tongue ??= Make(128, TongueShape);

    /// <summary>A billow of a bright mass: a soft noisy disc, like the puff without its grain.</summary>
    public static Texture2D Billow => _billow ??= Make(128, BillowShape);

    private static Texture2D? _spike, _tongue, _billow;

    /// <summary>A sphere of light: a faint mottled disc with a bright rim.</summary>
    public static Texture2D Sphere => _sphere ??= Make(256, SphereShape);

    /// <summary>A swirl: a thin glowing spiral of two turns round a bright eye.</summary>
    public static Texture2D Swirl => _swirl ??= Make(128, SwirlShape);

    /// <summary>A streak along x: a long thin needle, its tail (−x) longer than its head, brightest past the middle.</summary>
    public static Texture2D Streak => _streak ??= Make(256, StreakShape, 16);

    /// <summary>A haze: a broad soft glow, far flatter than <see cref="Glow"/>, fading to nothing at its edge.</summary>
    public static Texture2D Haze => _haze ??= Make(128, HazeShape);

    private static Texture2D? _sphere, _swirl, _streak, _haze;

    /// <summary>A lens: a tall arc bulging toward −x, a bright rim on its outer (−x) side and a glassy fill fading toward +x, its ends soft.</summary>
    public static Texture2D Lens => _lens ??= Make(128, LensShape, 256);

    private static Texture2D? _lens;

    /// <summary>A bar along x: even along its length, soft across it, its ends fading.</summary>
    public static Texture2D Bar => _bar ??= Make(256, BarShape, 32);

    /// <summary>A petal: a soft filled ellipse (stretched along x), brighter in its middle.</summary>
    public static Texture2D Petal => _petal ??= Make(64, PetalShape);

    /// <summary>A half-moon: a disc less a larger disc offset to −x, its hollow facing left, thickest at +x, its tips tapering; brighter at its outer edge.</summary>
    public static Texture2D Moon => _moon ??= Make(128, MoonShape);

    private static Texture2D? _bar, _petal, _moon;

    /// <summary>A shuriken: a four-point star with concave sides and sharp tips on the axes, a round hole a little off its middle, its edges a little brighter.</summary>
    public static Texture2D Shuriken => _shuriken ??= Make(128, ShurikenShape);

    /// <summary>A shuriken's glint: a crescent bulging toward −x, its outer edge bitten into three points, in a soft glow.</summary>
    public static Texture2D Wing => _wing ??= Make(128, WingShape);

    private static Texture2D? _shuriken, _wing;

    /// <summary>A blast's rim: a ring with a sharp inner edge (radius 0.8) and a frayed outer one fading by 0.97.</summary>
    public static Texture2D Rim => _rim ??= Make(256, RimShape);

    private static Texture2D? _rim;

    /// <summary>A sickle: a crescent filled evenly with sharp edges, a disc less a disc of nearly its size offset to −x, its hollow facing left and its horns pointed.</summary>
    public static Texture2D Sickle => _sickle ??= Make(256, SickleShape);

    private static Texture2D? _sickle;

    /// <summary>
    /// A size × <paramref name="height"/> texture (square by default) whose alpha at each pixel is <paramref name="alpha"/>
    /// of (x, y) in [−1, 1] — both axes, whatever the shape.
    /// </summary>
    private static Texture2D Make(int size, Func<float, float, float> alpha, int height = 0)
    {
        int width = size;
        height = height > 0 ? height : size;
        var data = new byte[width * height * 4];
        for (int py = 0; py < height; py++)
        {
            for (int px = 0; px < width; px++)
            {
                float x = (px + 0.5f) / width * 2 - 1, y = (py + 0.5f) / height * 2 - 1;
                int at = (py * width + px) * 4;
                data[at] = data[at + 1] = data[at + 2] = 255;
                data[at + 3] = (byte)Math.Round(Math.Clamp(alpha(x, y), 0, 1) * 255);
            }
        }
        var image = Image.CreateFromData(width, height, false, Image.Format.Rgba8, data);
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

    // The arrow's parts along x (y is 8× finer: 2/32 is a pixel across, 2/256 along).
    private static float ArrowShape(float x, float y)
    {
        float ay = MathF.Abs(y);
        float shaft = Edge(0.2f - ay, 0.06f) * Edge(x + 0.88f, 0.01f) * Edge(0.75f - x, 0.01f);
        float head = Edge((1 - x) / 0.3f * 0.6f - ay, 0.06f) * Edge(x - 0.68f, 0.01f);
        float t = (x + 1) / 0.26f;                       // 0 at the back end, 1 where the fletching meets the shaft
        float fins = t is >= 0 and <= 1
            ? Edge(0.7f * (1 - t) + 0.08f - ay, 0.06f) * Edge(ay - (0.7f * (1 - t) - 0.3f), 0.06f)
            : 0;
        return Math.Max(Math.Max(shaft, head), fins);
    }

    private static float BurstShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        if (r > 1)
            return 0;
        float angle = MathF.Atan2(y, x);
        float spikes = 0;
        for (int i = 0; i < Spikes.Length; i += 4)
        {
            float d = MathF.Abs(MathF.IEEERemainder(angle - Spikes[i], MathF.Tau)) * r;
            float t = r / (Spikes[i + 1] + 0.15f);
            if (t < 1)
                spikes += Gauss(d, 0.02f * (1 - t) + 0.004f) * (1 - t);
        }
        return spikes + Gauss(r, 0.1f);
    }

    private static float BowArcShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        float angle = MathF.Abs(MathF.Atan2(y, x));
        float taper = 1 - SmoothStep(0.6f, 1.1f, angle);
        return Gauss(r - 0.8f, 0.045f + 0.04f * taper) * taper;
    }

    private static float SplatShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        float angle = MathF.Atan2(y, x);                    // the upper half is −π..0
        float upper = SmoothStep(0.15f, -0.25f, MathF.Sin(angle) + 0.1f);
        float points = MathF.Pow(MathF.Abs(MathF.Cos(angle * 3.5f)), 6) * 0.25f;
        float crown = Edge(0.6f + points - r, 0.03f) * Edge(r - 0.45f, 0.03f) * upper;
        return crown + 0.35f * Gauss(r - 0.5f, 0.2f) * upper;
    }

    private static float BokehShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        return (0.75f + 0.25f * SmoothStep(0.6f, 0.85f, r)) * (1 - SmoothStep(0.85f, 1f, r));
    }

    private static float ShockShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        float taper = 1 - SmoothStep(0.8f, 1.25f, MathF.Abs(MathF.Atan2(y, x)));
        float band = Gauss(r - 0.78f, 0.07f), fill = 0.35f * SmoothStep(0.35f, 0.75f, r) * (1 - SmoothStep(0.76f, 0.82f, r));
        return (band + fill) * taper;
    }

    private static float GlowShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        return (0.75f * Gauss(r, 0.2f) + 0.4f * MathF.Pow(Math.Max(0, 1 - r), 3)) * (1 - SmoothStep(0.85f, 1f, r));
    }

    private static float RaysShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        if (r > 1)
            return 0;
        float angle = MathF.Atan2(y, x);
        float rays = 0;
        for (int i = 0; i < Spikes.Length; i += 2)
        {
            float d = MathF.Abs(MathF.IEEERemainder(angle - Spikes[i], MathF.Tau)) * r;
            rays += Gauss(d, 0.006f + 0.01f * r) * MathF.Pow(1 - r, 1.5f) * (0.4f + Spikes[i + 1]);
        }
        return rays * SmoothStep(0.05f, 0.2f, r);
    }

    // Along x from the foot (−1) to the point (+1); y is 8× finer.
    private static float SpikeShape(float x, float y)
    {
        float along = (x + 1) / 2;
        float width = 0.75f * MathF.Pow(1 - along, 1.3f) + 0.04f;
        float wedge = Gauss(y, width * 0.5f) * 0.55f + Gauss(y, width * 0.15f) * 0.6f;
        return wedge * SmoothStep(0f, 0.12f, along) * (1 - SmoothStep(0.85f, 1f, along));
    }

    private static float BillowShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        return (1 - SmoothStep(0.45f, 1f, r)) * (0.7f + 0.3f * Fbm(x * 3 + 11, y * 3 + 5));
    }

    // From the foot (y = 1) to the point (y = −1): the spine leans right toward the top, the blade narrows to it.
    private static float TongueShape(float x, float y)
    {
        float up = (1 - y) / 2;                                   // 0 at the foot, 1 at the point
        float spine = 0.55f * up * up - 0.1f;
        float half = 0.6f * MathF.Pow(Math.Max(0, 1 - up), 0.8f) + 0.01f;
        float across = (x - spine) / half;                        // −1 on the left edge, +1 on the right
        if (across is < -1.2f or > 1.2f)
            return 0;
        float body = Edge(1 - MathF.Abs(across), 0.25f);
        float rim = Gauss(across + 0.8f, 0.18f);
        return (0.45f * body + 0.55f * rim * body) * SmoothStep(1f, 0.7f, y);
    }

    private static float SphereShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        float fill = 0.3f + 0.6f * MathF.Pow(Fbm(x * 3.5f + 21, y * 3.5f + 9), 3) + 0.2f * r * r;
        float rim = 0.35f * Gauss(r - 0.92f, 0.06f) * (1 - 0.4f * Math.Max(0, x));      // softer on the right
        return (fill + rim) * (1 - SmoothStep(0.9f, 1f, r));
    }

    private static float HazeShape(float x, float y) => MathF.Pow(Math.Max(0, 1 - MathF.Sqrt(x * x + y * y)), 1.6f);

    // An Archimedean spiral of two turns, r = (angle + 2πn) / 4π, its line thinning and fading toward the eye.
    private static float SwirlShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y) / 0.9f;
        if (r > 1.05f)
            return 0;
        float angle = MathF.Atan2(y, x);
        if (angle < 0)
            angle += MathF.Tau;
        float line = 0;
        for (int n = 0; n < 2; n++)
        {
            float along = (angle + MathF.Tau * n) / (2 * MathF.Tau);
            line = Math.Max(line, Gauss(r - along, 0.02f + 0.03f * along) * (0.4f + 0.6f * along));
        }
        return line * (1 - SmoothStep(0.9f, 1.05f, r)) + 0.7f * Gauss(r, 0.1f);
    }

    // Along x from the tail (−1) to the head (+1); y is 16× finer.
    private static float StreakShape(float x, float y)
    {
        float body = SmoothStep(-1f, 0.1f, x) * (1 - SmoothStep(0.55f, 1f, x));
        return Gauss(y, 0.12f + 0.3f * body) * body;
    }

    // The rim runs from x −0.55 in the middle to −0.1 at the ends; the fill falls off over ~0.5 behind it.
    private static float LensShape(float x, float y)
    {
        float rim = -0.55f + 0.45f * y * y, d = x - rim;
        float ends = 1 - SmoothStep(0.8f, 1f, MathF.Abs(y));
        float glow = 0.75f * Gauss(d, 0.12f) + (d > 0 ? 0.65f * MathF.Exp(-d / 0.5f) : 0);
        return glow * ends * (1 - SmoothStep(0.2f, 1f, x));
    }

    // Along x from end to end; y is 8× finer.
    private static float BarShape(float x, float y) => Gauss(y, 0.3f) * (1 - SmoothStep(0.8f, 1f, MathF.Abs(x)));

    private static float MoonShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y), hollow = MathF.Sqrt((x + 0.32f) * (x + 0.32f) + y * y);
        float body = (1 - SmoothStep(0.88f, 0.96f, r)) * SmoothStep(0.86f, 0.94f, hollow);
        return body * (0.6f + 0.4f * SmoothStep(0.6f, 0.95f, r));
    }

    private static float PetalShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        return (0.6f + 0.4f * Math.Max(0, 1 - r)) * (1 - SmoothStep(0.5f, 1f, r));
    }

    private static float ShurikenShape(float x, float y)
    {
        float f = MathF.Pow(MathF.Abs(x), 0.56f) + MathF.Pow(MathF.Abs(y), 0.56f);
        float hole = MathF.Sqrt((x - 0.03f) * (x - 0.03f) + (y - 0.02f) * (y - 0.02f));
        float body = (1 - SmoothStep(0.92f, 1f, f)) * SmoothStep(0.21f, 0.25f, hole);
        return body * (0.82f + 0.18f * SmoothStep(0.7f, 0.92f, f));
    }

    private static float WingShape(float x, float y)
    {
        // A crescent: a disc less a disc offset to +x (its tips at about (0.45, ±0.77)), two bites out of its outer edge
        // leaving a point between them; brighter toward the outer edge, in a soft glow.
        float outer = 0.8f - MathF.Sqrt((x - 0.25f) * (x - 0.25f) + y * y);
        float hollow = MathF.Sqrt((x - 0.7f) * (x - 0.7f) + y * y) - 0.8f;
        float bites = float.MaxValue;
        foreach (float angle in new[] { -0.55f, 0.55f })
        {
            float bx = 0.25f - 0.98f * MathF.Cos(angle), by = 0.98f * MathF.Sin(angle);
            bites = Math.Min(bites, MathF.Sqrt((x - bx) * (x - bx) + (y - by) * (y - by)) - 0.3f);
        }
        float d = Math.Min(outer, Math.Min(hollow, bites));
        float body = SmoothStep(-0.02f, 0.02f, d) * (0.7f + 0.3f * SmoothStep(0.12f, 0.02f, outer));
        return Math.Min(1, body + 0.4f * MathF.Exp(-Math.Max(0, -d) / 0.08f));
    }

    private static float RimShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y), angle = MathF.Atan2(y, x);
        float fray = 0.04f * (Fbm(MathF.Cos(angle) * 6 + 3, MathF.Sin(angle) * 6 + 5) - 0.5f);
        return SmoothStep(0.76f, 0.8f, r) * (1 - SmoothStep(0.84f + fray, 0.97f + fray, r)) * (0.85f + 0.15f * Fbm(x * 9, y * 9));
    }

    private static float SickleShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y), hollow = MathF.Sqrt((x + 0.55f) * (x + 0.55f) + y * y);
        return Edge(0.95f - r, 0.015f) * Edge(hollow - 0.9f, 0.015f);
    }

    /// <summary>A soft edge at <paramref name="v"/> = 0: 0 below −<paramref name="width"/>/2, 1 above +<paramref name="width"/>/2.</summary>
    private static float Edge(float v, float width) => Math.Clamp(v / width + 0.5f, 0, 1);

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
