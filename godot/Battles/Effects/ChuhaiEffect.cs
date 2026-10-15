using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The Chuhai effect, ours after Sengoku Rance's Shogun Whirlwind, 47060 (docs/sengoku/04-screen.md §2.4.1; not
/// traced; the owner's choice, 2026-10-14): Asuka's blast at the enemy troops (docs/battle/02-resolution.md §3.3). A
/// whirlwind: a wheel of blue blades turning clockwise, swelling from a dome at the frame's foot in the middle (frames
/// 0-6) while its centre climbs and drifts right, off the top right by the end; the blades are thin hooks with bright
/// heads among soft blue kites, piled white on a sector of the rim that sweeps from the right (to frame ~6) down round
/// the bottom (12) to the lower left, with a grey-teal haze outside the rim; it dims from frame ~20, gone by ~27. The
/// same seed makes the same effect.
/// </summary>
internal static class ChuhaiEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    // The wheel by frame: its centre, its radius and the white sector's middle (degrees, clockwise from the right).
    private static readonly float[] Frames = [0, 2, 4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26];
    private static readonly float[] CentreX = [390, 395, 390, 385, 410, 490, 590, 670, 700, 750, 800, 850, 900, 940];
    private static readonly float[] CentreY = [340, 300, 260, 220, 120, 60, 30, 15, 0, -20, -50, -70, -90, -110];
    private static readonly float[] Radius = [40, 140, 210, 235, 245, 255, 265, 280, 290, 295, 300, 300, 300, 300];
    private static readonly float[] Sector = [0, 0, 0, 0, 30, 55, 90, 115, 130, 135, 135, 135, 135, 135];

    /// <summary>The effect's strength by frame: full to ~20, then dying out.</summary>
    private static float Strength(float frame) => Lerp(frame, [20, 22, 23, 24, 25, 26, 27], [1, 0.8f, 0.55f, 0.35f, 0.18f, 0.05f, 0]);

    /// <summary>The blades, kites and white blades lit a frame on a wheel of radius 260, and the haze's puffs.</summary>
    private const float Blades = 170, Kites = 140, WhiteBlades = 220, Puffs = 18;

    // The colours (light added over the scene).
    private static readonly Color Blade = new(0.55f, 0.72f, 1f), KiteBlue = new(0.33f, 0.48f, 0.85f);
    private static readonly Color White = new(0.75f, 0.88f, 1f), Mist = new(0.45f, 0.6f, 0.85f), Haze = new(0.2f, 0.26f, 0.27f);

    /// <summary>A blade's turn: its chord rising to the right, its bulge up and left (the hook is flipped).</summary>
    private const float Tilt = 0.62f;

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        for (int frame = 0; frame <= 26; frame++)
        {
            var centre = new Vector2(Lerp(frame, Frames, CentreX), Lerp(frame, Frames, CentreY));
            float radius = Lerp(frame, Frames, Radius), sector = Mathf.DegToRad(Lerp(frame, Frames, Sector));
            float area = radius * radius / (260f * 260f), strength = Strength(frame);
            float birth = frame - 0.5f;

            // A point of the wheel at a radius (a share of it) and an angle, its turning motion (clockwise) a frame.
            (Vector2 At, Vector2 Velocity) On(float share, float angle)
            {
                var direction = Vector2.FromAngle(angle);
                return (centre + direction * radius * share, new Vector2(-direction.Y, direction.X) * 18 * share);
            }
            static bool Seen(Vector2 at) => at.X > -60 && at.X < Width + 60 && at.Y > -60 && at.Y < Height + 60;
            int Count(float perFrame) => (int)(perFrame * area * strength + random.NextDouble());

            // The blades: thin hooks over the whole wheel, thinner toward its eye.
            for (int i = 0, n = Count(Blades * Lerp(frame, [10, 14], [1, 1.4f])); i < n; i++)
            {
                var (at, velocity) = On(0.05f + 1f * MathF.Sqrt((float)random.NextDouble()), Range(0, MathF.Tau));
                if (!Seen(at))
                    continue;
                float length = Range(60, 110), alpha = Range(0.7f, 1);
                particles.Add(new Particle(EffectTextures.Hook, birth, Range(1, 1.6f), at, velocity, Tilt + Range(-0.25f, 0.25f), 0,
                    new Vector2(-length * 0.7f, length), Blade, _ => alpha, _ => 1));
            }

            // The kites: soft blue diamonds among them.
            for (int i = 0, n = Count(Kites * Lerp(frame, [10, 14], [1, 1.4f])); i < n; i++)
            {
                var (at, velocity) = On(0.05f + 1f * MathF.Sqrt((float)random.NextDouble()), Range(0, MathF.Tau));
                if (!Seen(at))
                    continue;
                float size = Range(60, 100);
                particles.Add(new Particle(EffectTextures.Kite, birth, Range(1, 1.6f), at, velocity, Range(-0.6f, 0.2f), 0,
                    new Vector2(size, size * Range(0.6f, 0.9f)), KiteBlue, _ => 0.65f, _ => 1));
            }

            // The white sector: blades piled on the rim with a mist behind them.
            for (int i = 0, n = Count(WhiteBlades * Lerp(frame, [1, 4, 6], [0.15f, 0.3f, 1])); i < n; i++)
            {
                float offset = Range(-1, 1);
                var (at, velocity) = On(Range(0.72f, 1.05f), sector + Mathf.DegToRad(Lerp(frame, [4, 8, 12, 16], [50, 90, 80, 60])) * offset * (0.5f + 0.5f * MathF.Abs(offset)));
                if (!Seen(at))
                    continue;
                float length = Range(60, 110);
                particles.Add(new Particle(EffectTextures.Hook, birth, Range(1, 1.6f), at, velocity, Tilt + Range(-0.25f, 0.25f), 0,
                    new Vector2(-length * 0.7f, length), White, _ => 1, _ => 1));
                if (i % 3 == 0)
                    particles.Add(new Particle(EffectTextures.Billow, birth, Range(1, 1.6f), at, velocity, 0, 0,
                        new Vector2(Range(70, 110), Range(70, 110)), Mist, _ => 0.45f, _ => 1));
            }

            // The haze: grey-teal puffs just outside the rim, a few frames each.
            for (int i = 0, n = Count(Puffs); i < n; i++)
            {
                var (at, velocity) = On(Range(0.95f, 1.15f), Range(0, MathF.Tau));
                if (!Seen(at))
                    continue;
                float size = Range(90, 150);
                particles.Add(new Particle(EffectTextures.Billow, birth, Range(2.5f, 4), at, velocity * 0.5f, 0, 0, new Vector2(size, size), Haze,
                    t => 0.9f * (t < 0.3f ? t / 0.3f : 1 - (t - 0.3f) / 0.7f), _ => 1));
            }
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
}
