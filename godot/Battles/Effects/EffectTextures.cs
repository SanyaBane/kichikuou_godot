using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The pictures our exchange effects are made of, drawn by code (docs/ui/battle-screen.md, "The exchange's
/// effects": none of Sengoku Rance's art). Each is white with the shape in its alpha, so a draw tints it, but the
/// crescent's two parts, which are tinted apart. Each one's mask is drawn once, ahead of play by <see cref="Prepare"/> or
/// on first use; the texture is made from it on first use.
/// </summary>
internal static class EffectTextures
{
    /// <summary>Every mask below (each adds itself; declared first, so it is there before them).</summary>
    private static readonly List<Mask> All = [];

    private static int _prepared;

    /// <summary>
    /// Draws every texture's mask on a worker thread, once a launch (<c>Main</c> starts it): drawn per pixel in C#, an
    /// effect's masks cost its first play in a launch ~130 ms on the phone otherwise (docs/backlog.md, "Android"). The
    /// textures are still made from them on first use, on the main thread; a use before its mask is drawn waits for it.
    /// </summary>
    public static void Prepare()
    {
        if (Interlocked.Exchange(ref _prepared, 1) == 0)
            Task.Run(() =>
            {
                var clock = System.Diagnostics.Stopwatch.StartNew();
                foreach (var mask in All)
                    mask.Prepare();
                GD.Print($"Kichikuou: the exchange effects' {All.Count} masks drawn ahead in {clock.ElapsedMilliseconds} ms.");
            });
    }

    /// <summary>A texture's mask (its image, drawn once, whichever thread asks first) and the texture made from it on first use.</summary>
    private sealed class Mask
    {
        private readonly Lazy<Image> _image;
        private Texture2D? _texture;

        public Mask(int size, Func<float, float, float> alpha, int height = 0)
        {
            _image = new Lazy<Image>(() => Draw(size, alpha, height));
            All.Add(this);
        }

        public void Prepare() => _ = _image.Value;

        public Texture2D Texture => _texture ??= ImageTexture.CreateFromImage(_image.Value);
    }

    /// <summary>A soft round puff of dust: a noisy disc fading to its edge, with a fine grain of bright specks.</summary>
    public static Texture2D Puff => PuffMask.Texture;

    private static readonly Mask PuffMask = new(128, PuffShape);

    /// <summary>A crescent's body: a disc less an offset disc, a thin blade with its hollow facing left (−x), the upper tip a fading tail.</summary>
    public static Texture2D CrescentBody => CrescentBodyMask.Texture;

    private static readonly Mask CrescentBodyMask = new(256, CrescentBodyShape);

    /// <summary>The crescent's inner edge: a bright rim along its hollow and a few thin arcs trailing inside it.</summary>
    public static Texture2D CrescentEdge => CrescentEdgeMask.Texture;

    private static readonly Mask CrescentEdgeMask = new(256, CrescentEdgeShape);

    /// <summary>An "urchin": a soft ring with fine radial spikes around an empty middle.</summary>
    public static Texture2D Urchin => UrchinMask.Texture;

    private static readonly Mask UrchinMask = new(128, UrchinShape);

    /// <summary>A four-point sparkle: two thin crossed rays tapering from a small bright core.</summary>
    public static Texture2D Sparkle => SparkleMask.Texture;

    private static readonly Mask SparkleMask = new(128, SparkleShape);

    /// <summary>A small thin ring.</summary>
    public static Texture2D Ring => RingMask.Texture;

    private static readonly Mask RingMask = new(64, RingShape);

    /// <summary>A short thin streak along x, tapered at both ends.</summary>
    public static Texture2D Dash => DashMask.Texture;

    private static readonly Mask DashMask = new(64, DashShape);

    /// <summary>A soft dot (a speck of the dust's grain).</summary>
    public static Texture2D Dot => DotMask.Texture;

    private static readonly Mask DotMask = new(16, DotShape);

    /// <summary>An arrow along x, its head at +x: a thin shaft, a pointed head and a V of fletching at the back (8:1).</summary>
    public static Texture2D Arrow => ArrowMask.Texture;

    private static readonly Mask ArrowMask = new(256, ArrowShape, 32);

    /// <summary>A bow's release: thin spikes of every length bursting from a small bright core.</summary>
    public static Texture2D Burst => BurstMask.Texture;

    private static readonly Mask BurstMask = new(128, BurstShape);

    /// <summary>A bow: a thin arc bulging toward +x, ±60° about it, its ends tapering.</summary>
    public static Texture2D BowArc => BowArcMask.Texture;

    private static readonly Mask BowArcMask = new(128, BowArcShape);

    /// <summary>An arrow's hit: a "crown", the upper half of a ring with jagged points outward, and a glow.</summary>
    public static Texture2D Splat => SplatMask.Texture;

    private static readonly Mask SplatMask = new(128, SplatShape);

    /// <summary>A bokeh disc: flat inside, a slightly brighter rim, a soft edge.</summary>
    public static Texture2D Bokeh => BokehMask.Texture;

    private static readonly Mask BokehMask = new(64, BokehShape);
    /// <summary>A soft glow: a bright core in a wide falling halo.</summary>
    public static Texture2D Glow => GlowMask.Texture;

    private static readonly Mask GlowMask = new(128, GlowShape);

    /// <summary>A flare's rays: many fine lines from the centre, fading outward.</summary>
    public static Texture2D Rays => RaysMask.Texture;

    private static readonly Mask RaysMask = new(256, RaysShape);

    /// <summary>A shockwave: a soft wide band on an arc bulging toward +x, ±70° about it, a faint glassy fill inside.</summary>
    public static Texture2D Shock => ShockMask.Texture;

    private static readonly Mask ShockMask = new(128, ShockShape);

    /// <summary>A spike along x, its point at +x: a long thin wedge, widest near its foot (−x), with a bright spine.</summary>
    public static Texture2D Spike => SpikeMask.Texture;

    private static readonly Mask SpikeMask = new(256, SpikeShape, 32);

    /// <summary>A flame's tongue: a filled blade rising from a wide foot (+y) to a point leaning right at the top, its left edge brightest.</summary>
    public static Texture2D Tongue => TongueMask.Texture;

    private static readonly Mask TongueMask = new(128, TongueShape);

    /// <summary>A billow of a bright mass: a soft noisy disc, like the puff without its grain.</summary>
    public static Texture2D Billow => BillowMask.Texture;

    private static readonly Mask BillowMask = new(128, BillowShape);

    /// <summary>A sphere of light: a faint mottled disc with a bright rim.</summary>
    public static Texture2D Sphere => SphereMask.Texture;

    private static readonly Mask SphereMask = new(256, SphereShape);

    /// <summary>A swirl: a thin glowing spiral of two turns round a bright eye.</summary>
    public static Texture2D Swirl => SwirlMask.Texture;

    private static readonly Mask SwirlMask = new(128, SwirlShape);

    /// <summary>A streak along x: a long thin needle, its tail (−x) longer than its head, brightest past the middle.</summary>
    public static Texture2D Streak => StreakMask.Texture;

    private static readonly Mask StreakMask = new(256, StreakShape, 16);

    /// <summary>A haze: a broad soft glow, far flatter than <see cref="Glow"/>, fading to nothing at its edge.</summary>
    public static Texture2D Haze => HazeMask.Texture;

    private static readonly Mask HazeMask = new(128, HazeShape);

    /// <summary>A lens: a tall arc bulging toward −x, a bright rim on its outer (−x) side and a glassy fill fading toward +x, its ends soft.</summary>
    public static Texture2D Lens => LensMask.Texture;

    private static readonly Mask LensMask = new(128, LensShape, 256);

    /// <summary>A bar along x: even along its length, soft across it, its ends fading.</summary>
    public static Texture2D Bar => BarMask.Texture;

    private static readonly Mask BarMask = new(256, BarShape, 32);

    /// <summary>A petal: a soft filled ellipse (stretched along x), brighter in its middle.</summary>
    public static Texture2D Petal => PetalMask.Texture;

    private static readonly Mask PetalMask = new(64, PetalShape);

    /// <summary>A half-moon: a disc less a larger disc offset to −x, its hollow facing left, thickest at +x, its tips tapering; brighter at its outer edge.</summary>
    public static Texture2D Moon => MoonMask.Texture;

    private static readonly Mask MoonMask = new(128, MoonShape);

    /// <summary>A shuriken: a four-point star with concave sides and sharp tips on the axes, a round hole a little off its middle, its edges a little brighter.</summary>
    public static Texture2D Shuriken => ShurikenMask.Texture;

    private static readonly Mask ShurikenMask = new(128, ShurikenShape);

    /// <summary>A shuriken's glint: a crescent bulging toward −x, its outer edge bitten into three points, in a soft glow.</summary>
    public static Texture2D Wing => WingMask.Texture;

    private static readonly Mask WingMask = new(128, WingShape);

    /// <summary>A blast's rim: a ring with a sharp inner edge (radius 0.8) and a frayed outer one fading by 0.97.</summary>
    public static Texture2D Rim => RimMask.Texture;

    private static readonly Mask RimMask = new(256, RimShape);

    /// <summary>A sickle: a crescent filled evenly with sharp edges, a disc less a disc of nearly its size offset to −x, its hollow facing left and its horns pointed.</summary>
    public static Texture2D Sickle => SickleMask.Texture;

    private static readonly Mask SickleMask = new(256, SickleShape);

    /// <summary>A blade's hook: a thin arc bulging toward +x, ±60° about it, faint at its foot (+y) and thickening to a bright head near its top (−y).</summary>
    public static Texture2D Hook => HookMask.Texture;

    private static readonly Mask HookMask = new(256, HookShape);

    /// <summary>A kite: a soft diamond, longer along x, its points rounded off by the blur.</summary>
    public static Texture2D Kite => KiteMask.Texture;

    private static readonly Mask KiteMask = new(64, KiteShape);

    /// <summary>A claw: a solid blade along x from a thin root (−x) to a sharp point (+x), widest three quarters along, bowed a little toward −y, in a soft glow (4:1).</summary>
    public static Texture2D Claw => ClawMask.Texture;

    private static readonly Mask ClawMask = new(256, ClawShape, 64);

    /// <summary>
    /// A wave standing on the ground: a quarter arc rising from its foot at the right (+x, +y) up and back to a thin
    /// tip at the top left, its hollow facing left and down, thickest near the foot; faintly streaked across.
    /// </summary>
    public static Texture2D SurgeBody => SurgeBodyMask.Texture;

    private static readonly Mask SurgeBodyMask = new(256, SurgeBodyShape);

    /// <summary>The wave's crest: a bright rim along its outer (+x, −y) edge, fading toward the tip.</summary>
    public static Texture2D SurgeEdge => SurgeEdgeMask.Texture;

    private static readonly Mask SurgeEdgeMask = new(256, SurgeEdgeShape);

    /// <summary>A snowflake: six thin arms from a small bright core, each with two pairs of short side branches, in a soft glow.</summary>
    public static Texture2D Flake => FlakeMask.Texture;

    private static readonly Mask FlakeMask = new(128, FlakeShape);

    /// <summary>A tile: a flat rectangle, its edges a little soft (the mosaic a barrier breaks up into).</summary>
    public static Texture2D Tile => TileMask.Texture;

    private static readonly Mask TileMask = new(64, TileShape);

    /// <summary>
    /// A light blade's body: a crescent bulging toward +x, its hollow behind it (−x), filled faintly at the hollow and
    /// brightening toward its outer edge, its horns sweeping back and dissolving into wisps.
    /// </summary>
    public static Texture2D BladeBody => BladeBodyMask.Texture;

    private static readonly Mask BladeBodyMask = new(128, BladeBodyShape);

    /// <summary>The blade's cutting edge: a thin bright rim along its outer (+x) edge in a soft halo, the horns fading.</summary>
    public static Texture2D BladeEdge => BladeEdgeMask.Texture;

    private static readonly Mask BladeEdgeMask = new(128, BladeEdgeShape);

    /// <summary>
    /// A clump of poison gas, one of <see cref="GasVariants"/>: a lumpy blob, its outline frayed, its body marbled with
    /// dark creases between brighter folds, a few highlights on the folds.
    /// </summary>
    public static Texture2D Gas(int variant) => GasMasks[variant].Texture;

    public const int GasVariants = 3;

    private static readonly Mask[] GasMasks =
        [.. Enumerable.Range(0, GasVariants).Select(variant => new Mask(256, (x, y) => GasShape(x, y, variant)))];

    /// <summary>
    /// A size ×<paramref name="height"/> image (square by default) whose alpha at each pixel is <paramref name="alpha"/>
    /// of (x, y) in [−1, 1] — both axes, whatever the shape —, its mipmaps made.
    /// </summary>
    private static Image Draw(int size, Func<float, float, float> alpha, int height)
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
        return image;
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

    private static float TileShape(float x, float y) => (1 - SmoothStep(0.94f, 1f, MathF.Abs(x))) * (1 - SmoothStep(0.94f, 1f, MathF.Abs(y)));

    private static float MoonShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y), hollow = MathF.Sqrt((x + 0.32f) * (x + 0.32f) + y * y);
        float body = (1 - SmoothStep(0.88f, 0.96f, r)) * SmoothStep(0.86f, 0.94f, hollow);
        return body * (0.6f + 0.4f * SmoothStep(0.6f, 0.95f, r));
    }

    // Six arms at 60°, each a line to radius 0.85 with branches at 0.4 and 0.62 of its length, angled ±60° outward.
    private static float FlakeShape(float x, float y)
    {
        float r = MathF.Sqrt(x * x + y * y);
        if (r > 1)
            return 0;
        float angle = MathF.Atan2(y, x);
        float sector = MathF.Round(angle / (MathF.PI / 3)) * (MathF.PI / 3);
        // The point in its arm's frame: u along the arm, v across it.
        float u = r * MathF.Cos(angle - sector), v = MathF.Abs(r * MathF.Sin(angle - sector));
        float arm = Gauss(v, 0.035f) * (1 - SmoothStep(0.75f, 0.88f, u)) * SmoothStep(-0.05f, 0.05f, u);
        float branches = 0;
        foreach (var (at, length) in new[] { (0.4f, 0.22f), (0.62f, 0.15f) })
        {
            // A branch from (at, 0) outward at 60°: its distance to that segment.
            float du = u - at, along = du * 0.5f + v * 0.866f;
            float across = MathF.Abs(-du * 0.866f + v * 0.5f);
            if (along > 0 && along < length)
                branches = Math.Max(branches, Gauss(across, 0.03f) * (1 - along / length * 0.5f));
        }
        float core = Gauss(r, 0.1f);
        float glow = 0.25f * MathF.Pow(Math.Max(0, 1 - r), 2);
        return Math.Max(arm, branches) + core + glow;
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

    private static float HookShape(float x, float y)
    {
        // An arc of the circle round (−1.2, 0) through (0.7, 0); along it from the foot (y 0.9) to the head (y −0.9).
        float d = MathF.Abs(MathF.Sqrt((x + 1.2f) * (x + 1.2f) + y * y) - 1.9f), along = Math.Clamp((0.92f - y) / 1.84f, 0, 1);
        float width = 0.018f + 0.045f * along;
        return Gauss(d, width) * MathF.Pow(along, 1.5f) * (1 - SmoothStep(0.86f, 0.96f, -y));
    }

    private static float KiteShape(float x, float y) => 1 - SmoothStep(0.1f, 1, MathF.Abs(x) + MathF.Abs(y) / 0.6f);

    // Along x from the root (−1) to the point (+1); y is 4× finer. The half-width peaks at 0.6 about three quarters along.
    private static float ClawShape(float x, float y)
    {
        float along = Math.Clamp((x + 1) / 2, 0, 1);
        float half = 1.67f * MathF.Pow(along, 1.4f) * MathF.Pow(1 - along, 0.45f) + 0.02f;
        float d = MathF.Abs(y + 0.3f * MathF.Sin(MathF.PI * along)) - half;
        return Math.Min(1, Edge(-d, 0.08f) + 0.35f * MathF.Exp(-Math.Max(0, d) / 0.12f) * SmoothStep(0, 0.3f, along));
    }

    // The wave: an arc of the circle round (−0.85, 0.95), radius 1.75, from its tip at the top (−0.85, −0.8) to its foot
    // at the right (0.9, 0.95); its depth inward from the outer edge grows from 0 at the tip to 0.62 at the foot.
    private const float SurgeX = -0.85f, SurgeY = 0.95f, SurgeR = 1.75f;

    /// <summary>Along the wave from its tip (0) to its foot (1), the depth inside its outer edge, the depth it has there.</summary>
    private static (float Along, float Depth, float Thickness) Surge(float x, float y)
    {
        float along = (MathF.Atan2(y - SurgeY, x - SurgeX) + MathF.PI / 2) / (MathF.PI / 2);
        float depth = SurgeR - MathF.Sqrt((x - SurgeX) * (x - SurgeX) + (y - SurgeY) * (y - SurgeY));
        return (along, depth, 0.62f * MathF.Pow(Math.Clamp(along, 0, 1), 0.6f));
    }

    private static float SurgeBodyShape(float x, float y)
    {
        var (along, depth, thickness) = Surge(x, y);
        if (along is < 0 or > 1.02f)
            return 0;
        float inside = Edge(depth, 0.03f) * Edge(thickness - depth, 0.06f);
        // Streaks across it, ragged at its inner edge; brighter toward the outer edge; the foot soft on the ground.
        float streaks = 0.7f + 0.3f * Fbm(along * 22, depth * 9);
        float outer = 0.65f + 0.35f * (1 - Math.Clamp(depth / Math.Max(thickness, 0.01f), 0, 1));
        return inside * streaks * outer * (1 - SmoothStep(0.93f, 1.02f, along)) * SmoothStep(0, 0.08f, along);
    }

    private static float SurgeEdgeShape(float x, float y)
    {
        var (along, depth, _) = Surge(x, y);
        if (along is < 0 or > 1.02f)
            return 0;
        return Gauss(depth - 0.025f, 0.022f) * SmoothStep(0, 0.25f, along) * (1 - SmoothStep(0.9f, 1.02f, along));
    }

    // The blade lies between an outer ellipse (centre −0.3, semi-axes 1.2 × 0.95: its front at x 0.9) and the hollow's
    // (centre −0.95, 1.45 × 1.05: its front at 0.5); the distances to both come from the implicit functions over their
    // gradients. Returns the distance inside the outer edge, outside the hollow, and how far across from hollow to edge.
    private static (float ToEdge, float ToHollow, float Across) Blade(float x, float y)
    {
        float ox = (x + 0.3f) / 1.2f, oy = y / 0.95f, hx = (x + 0.95f) / 1.45f, hy = y / 1.05f;
        float toEdge = (1 - ox * ox - oy * oy) / (2 * MathF.Sqrt(ox * ox / 1.44f + oy * oy / 0.9025f) + 1e-4f);
        float toHollow = (hx * hx + hy * hy - 1) / (2 * MathF.Sqrt(hx * hx / 2.1025f + hy * hy / 1.1025f) + 1e-4f);
        float across = Math.Clamp(toHollow / (toHollow + Math.Max(toEdge, 0) + 1e-4f), 0, 1);
        return (toEdge, toHollow, across);
    }

    private static float BladeBodyShape(float x, float y)
    {
        var (toEdge, toHollow, across) = Blade(x, y);
        float horns = (1 - SmoothStep(0.4f, 0.93f, MathF.Abs(y))) * (0.65f + 0.35f * Fbm(x * 5 + 2, y * 5 + 9));
        return Edge(toEdge, 0.02f) * Edge(toHollow, 0.03f) * (0.2f + 0.8f * across * across) * horns;
    }

    private static float BladeEdgeShape(float x, float y)
    {
        var (toEdge, toHollow, _) = Blade(x, y);
        float horns = 1 - SmoothStep(0.55f, 0.95f, MathF.Abs(y));
        return Edge(toHollow, 0.04f) * (Gauss(toEdge, 0.016f) + 0.35f * Gauss(toEdge, 0.08f)) * horns;
    }

    // The outline: a disc whose radius the noise pushes in and out; the marbling: the contour lines of a warped noise,
    // dark where it crosses them (the creases), bright halfway between (the folds), a little brighter toward the top left.
    private static float GasShape(float x, float y, int variant)
    {
        float ox = 13.7f * variant + 3, oy = 7.3f * variant + 1;
        float r = MathF.Sqrt(x * x + y * y);
        float lobes = r + 0.85f * (Fbm(x * 1.6f + ox, y * 1.6f + oy) - 0.5f) + 0.2f * (Fbm(x * 4.5f + oy, y * 4.5f + ox) - 0.5f);
        float body = (1 - SmoothStep(0.3f, 0.75f, lobes)) * (1 - SmoothStep(0.8f, 1f, r));
        if (body <= 0)
            return 0;
        float warp = Fbm(x * 1.8f + oy, y * 1.8f + ox);
        float marble = Fbm(x * 2.2f + warp * 1.8f + ox, y * 2.2f - warp * 1.8f + oy);
        float wave = MathF.Sin(marble * 16);
        float crease = Gauss(wave + 0.9f, 0.4f), fold = Gauss(wave - 0.5f, 0.6f);
        float light = 0.55f + 0.3f * fold - 0.3f * crease + 0.15f * Math.Max(0, -(x + y) * 0.5f);
        return body * Math.Clamp(light, 0.15f, 1);
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
