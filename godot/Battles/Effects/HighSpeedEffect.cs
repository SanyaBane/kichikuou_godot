using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The High Speed effect, ours after Sengoku Rance's Commander Charge, effect 41400 (docs/sengoku/04-screen.md §2.4.1;
/// not traced; the owner's choice, 2026-10-15): Meglass's (unit 30) dash at the enemy commander. A white burst on the
/// actor's half on a thick white bar across the whole frame in a royal-blue glow (frames 0-2), the bar thinning to a
/// thread (gone by ~19); ice petals bursting out of it, flying out and slowing, turning blue (1-15, faint to ~22); a
/// white disc with a bright rim swelling from the burst (3-7) into a blue-grey halo of rings (7-15); then blue, ice and a
/// few lavender crescents spinning in from the middle and drifting right with pale blue four-point sparkles (9-25), the sparkles
/// to ~28. In Meglass's blues, after her sprite (CG 1527; owner, 2026-10-15). The same seed makes the same effect.
/// </summary>
internal static class HighSpeedEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>The burst's centre; the bar's line.</summary>
    private static readonly Vector2 Burst = new(312, 162);

    /// <summary>The disc's radius at a frame: swelling from the burst, slower as it turns into the halo.</summary>
    private static float DiscRadius(float frame) => Lerp(frame, [2.5f, 4, 6, 9, 12, 16], [80, 115, 150, 205, 250, 290]);

    /// <summary>Where the crescents come in at a frame: from the middle, then further right.</summary>
    private static float CrescentsFrom(float frame) => Lerp(frame, [9, 13, 17, 21, 24], [450, 390, 480, 580, 640]);

    // The colours (light added over the scene): Meglass's blues.
    private static readonly Color White = new(1f, 1f, 1f), Royal = new(0.22f, 0.39f, 0.81f), Sky = new(0.47f, 0.71f, 0.94f);
    private static readonly Color PetalIce = new(0.76f, 0.94f, 1f), PetalBlue = new(0.39f, 0.63f, 0.91f), PetalDim = new(0.2f, 0.28f, 0.6f);
    private static readonly Color Halo = new(0.5f, 0.55f, 0.75f), Blue = new(0.39f, 0.63f, 0.94f), Ice = new(0.76f, 0.94f, 1f);
    private static readonly Color Lavender = new(0.51f, 0.43f, 0.67f), Spark = new(0.57f, 0.8f, 1f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The bar: a thick white band across the whole frame thinning to a thread, each thinner one taking over as the one
        // before fades (frames 0-19).
        foreach (var (birth, life, thickness) in new[]
                 {
                     (-0.5f, 2f, 280f), (0.3f, 2.2f, 170f), (1.3f, 2.5f, 90f), (2.5f, 3f, 52f), (4, 4.5f, 32f), (6.5f, 5.5f, 20f),
                     (10, 6, 11f), (14, 5f, 6f),
                 })
            particles.Add(new Particle(EffectTextures.Bar, birth, life, new Vector2(Width / 2 + 20, Burst.Y), Vector2.Zero, 0, 0,
                new Vector2(Width + 60, thickness), White, t => t < 0.2f ? t / 0.2f : t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f, _ => 1));

        // The burst: a white glow in a royal-blue haze turning sky blue (frames 0-3).
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 4, Burst, Vector2.Zero, 0, 0, new Vector2(460, 300), White,
            t => t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f, t => 1 - 0.3f * t));
        particles.Add(new Particle(EffectTextures.Haze, -0.5f, 4, Burst, Vector2.Zero, 0, 0, new Vector2(700, 540), Royal,
            t => t < 0.3f ? 0.8f : 0.8f * (1 - (t - 0.3f) / 0.7f), _ => 1, Ramp: t => Royal.Lerp(Sky, t)));

        // The disc: white with a bright rim, swelling from the burst (frames 2.5-7), then the grey halo of rings it turns
        // into (6-16).
        particles.Add(new Particle(EffectTextures.Sphere, 2.5f, 5, Burst, Vector2.Zero, 0, 0, Vector2.One, White,
            t => t < 0.1f ? t / 0.1f : t < 0.4f ? 1 : 1 - (t - 0.4f) / 0.6f, t => 2 * DiscRadius(2.5f + 5 * t)));
        particles.Add(new Particle(EffectTextures.Bokeh, 2.5f, 4.5f, Burst, Vector2.Zero, 0, 0, Vector2.One, White,
            t => 0.55f * (t < 0.1f ? t / 0.1f : t < 0.3f ? 1 : 1 - (t - 0.3f) / 0.7f), t => 2 * DiscRadius(2.5f + 4.5f * t)));
        foreach (float share in new[] { 1f, 0.72f })
            particles.Add(new Particle(EffectTextures.Sphere, 5.5f, 10.5f, Burst, Vector2.Zero, 0, 0, Vector2.One, Halo,
                t => t < 0.15f ? t / 0.15f : 1 - (t - 0.15f) / 0.85f, t => 2 * share * DiscRadius(5.5f + 10.5f * t)));

        // The petals: soft ellipses bursting out radially, ice turning blue and slowing (frames 0-15, faint to ~22).
        for (int i = 0; i < 170; i++)
        {
            float birth = Range(-0.3f, 3), angle = Range(0, MathF.Tau), from = Range(20, 70), reach = 40 + 360 * MathF.Pow((float)random.NextDouble(), 1.2f), pace = Range(2, 4);
            float life = Range(10, 21), length = Range(45, 75);
            var direction = Vector2.FromAngle(angle) * new Vector2(1, 0.85f);
            particles.Add(new Particle(EffectTextures.Petal, birth, life, Burst, Vector2.Zero, angle, 0, new Vector2(length, length * 0.3f),
                PetalIce, t => t < 0.04f ? t / 0.04f : t < 0.3f ? 0.9f : 0.9f * MathF.Pow(1 - (t - 0.3f) / 0.7f, 1.5f), _ => 1,
                Ramp: t => t < 0.2f ? PetalIce.Lerp(PetalBlue, t / 0.2f) : PetalBlue.Lerp(PetalDim, (t - 0.2f) / 0.8f),
                Move: t => direction * (from + reach * (1 - MathF.Exp(-t * life / pace)))));
        }

        // The crescents: blue, ice and a few lavender half-moons spinning in from the middle and drifting right, each with
        // two fainter copies trailing its turn (frames 9-25).
        for (int i = 0; i < 110; i++)
        {
            float birth = 9 + 12 * MathF.Pow((float)random.NextDouble(), 1.3f), life = Math.Max(2, Math.Min(Range(3, 5.5f), 24.5f - birth));
            var at = new Vector2(Math.Min(Width + 20, CrescentsFrom(birth) + Range(-90, 200)), Range(5, Height - 5));
            float size = Range(75, 140), pick = (float)random.NextDouble();
            var colour = pick < 0.55f ? Blue : pick < 0.85f ? Ice : Lavender;
            var velocity = new Vector2(Range(16, 30), Range(-4, 4));
            float rotation = Range(0, MathF.Tau), spin = Range(0.15f, 0.35f) * (random.NextDouble() < 0.5 ? -1 : 1);
            for (int copy = 0; copy < 3; copy++)
            {
                float strength = (copy == 0 ? 1 : copy == 1 ? 0.45f : 0.2f) * (birth < 17 ? 1 : 0.6f);
                particles.Add(new Particle(EffectTextures.Moon, birth, life, at, velocity, rotation - spin * 0.6f * copy, spin,
                    new Vector2(size, size), colour, t => strength * (t < 0.15f ? t / 0.15f : t < 0.55f ? 1 : 1 - (t - 0.55f) / 0.45f),
                    t => 0.8f + 0.3f * t));
            }
        }

        // The sparkles: pale blue four-point stars and glints among the crescents (frames 10-28).
        for (int i = 0; i < 240; i++)
        {
            float birth = 10 + 17 * MathF.Pow((float)random.NextDouble(), 1.1f), life = Math.Min(Range(2, 5), 28.5f - birth);
            var at = new Vector2(Math.Min(Width - 5, CrescentsFrom(birth) + Range(-120, 220)), Range(5, Height - 5));
            float size = Range(40, 90), twinkle = Range(0, MathF.Tau);
            float Alpha(float t) => (0.75f + 0.25f * MathF.Sin(twinkle + t * 15)) * Fade(t);
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, new Vector2(Range(2, 8), 0), 0, 0, new Vector2(size, size), Spark,
                Alpha, _ => 1));
            if (i % 2 == 0)
                particles.Add(new Particle(EffectTextures.Glow, birth, life, at, new Vector2(Range(2, 8), 0), 0, 0, new Vector2(size, size) * 0.5f,
                    i % 2 == 0 ? White : Spark, t => 0.8f * Alpha(t), _ => 1));
        }
        return particles;
    }

    /// <summary>The piecewise-linear curve through (<paramref name="xs"/>, <paramref name="ys"/>) at <paramref name="x"/>, flat past its ends.</summary>
    private static float Lerp(float x, ReadOnlySpan<float> xs, ReadOnlySpan<float> ys)
    {
        if (x <= xs[0])
            return ys[0];
        for (int i = 1; i < xs.Length; i++)
        {
            if (x <= xs[i])
                return ys[i - 1] + (ys[i] - ys[i - 1]) * (x - xs[i - 1]) / (xs[i] - xs[i - 1]);
        }
        return ys[^1];
    }

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
