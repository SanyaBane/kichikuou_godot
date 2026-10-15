using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The ninja's follow-up effect, ours after Sengoku Rance's Ninja troop effect, 40100 (docs/sengoku/04-screen.md
/// §2.4.1; not traced; the owner's choice, 2026-10-14): the ninja's one shot at the enemy commander after an attack
/// (docs/battle/02-resolution.md §7.3). Shuriken thrown from the actor's half in a stream, spinning and tumbling as they
/// fly right across the frame, white-cream where they leave and gold over the target's half (frames 0-14 thrown, the
/// last gone by ~23); at the throws olive sprays and small white ticks, and tall pale arcs standing in rows and fading
/// (1-19); over the target's half a mesh of faint thin streaks with bright heads spreading left (3-28), gold glints of
/// scalloped hollows (9-28) and teal and white four-point sparkles (10-28). The same seed makes the same effect.
/// </summary>
internal static class NinjaEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>Where the shuriken leave the frame.</summary>
    private const float Exit = Width + 40;

    /// <summary>The streaks' left edge at a frame: spreading left over the target's half.</summary>
    private static float StreaksFrom(float frame) => Lerp(frame, [3, 6, 10, 14, 18], [440, 390, 330, 300, 280]);

    /// <summary>The glints' left edge at a frame.</summary>
    private static float GlintsFrom(float frame) => Lerp(frame, [9, 13, 17, 22], [540, 400, 420, 470]);

    // The colours (light added over the scene).
    private static readonly Color White = new(0.95f, 0.95f, 0.92f), Cream = new(0.88f, 0.84f, 0.62f), Gold = new(0.95f, 0.8f, 0.22f);
    private static readonly Color Spray = new(0.6f, 0.6f, 0.47f), Arc = new(0.95f, 0.92f, 0.55f), Scratch = new(0.75f, 0.72f, 0.5f);
    private static readonly Color Glint = new(1f, 0.72f, 0.12f), Teal = new(0.35f, 0.62f, 0.72f), Twinkle = new(0.9f, 0.95f, 1f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        float Sign() => random.NextDouble() < 0.5 ? -1 : 1;
        var particles = new List<Particle>();

        // The shuriken: a first volley already in flight at frame 0, then chains of three to five thrown together to
        // frame ~13, flying right, spinning, some tumbling (flattened), white-cream turning gold as they cross; at each
        // throw two olive sprays with small white ticks.
        void Shuriken(float birth, Vector2 at, Vector2 velocity)
        {
            float life = (Exit - at.X) / velocity.X, size = Range(48, 70), x0 = at.X;
            float squash = random.NextDouble() < 0.4 ? Range(0.4f, 0.8f) : 1;
            Color Colour(float t)
            {
                float x = x0 + (Exit - x0) * t;
                return x < 200 ? White : x < 300 ? White.Lerp(Cream, (x - 200) / 100) : Cream.Lerp(Gold, Math.Min(1, (x - 300) / 80));
            }
            particles.Add(new Particle(EffectTextures.Shuriken, birth, life, at, velocity, Range(0, MathF.Tau), Range(0.35f, 0.7f) * Sign(),
                new Vector2(size, size * squash), White, t => t < 0.02f ? t / 0.02f : 1, _ => 1, Ramp: Colour));
        }
        void Throw(float birth, Vector2 from)
        {
            for (int i = 0; i < 2; i++)
            {
                var spray = from + new Vector2(Range(-50, 30), Range(-40, 40));
                float life = Range(5, 9);
                particles.Add(new Particle(EffectTextures.Burst, birth + Range(0.5f, 1.5f), life, spray, new Vector2(Range(0, 2), 0),
                    Range(-0.15f, 0.15f), 0, new Vector2(Range(80, 110), Range(20, 28)), Spray, t => 0.7f * Fade(t), _ => 1));
                particles.Add(new Particle(EffectTextures.BowArc, birth + 1, life * 0.8f, spray + new Vector2(Range(-30, 0), Range(-10, 10)),
                    Vector2.Zero, 0, 0, new Vector2(-8, 22), White, t => 0.8f * Fade(t), _ => 1));
            }
        }
        for (int i = 0; i < 9; i++)
            Shuriken(Range(-2.2f, -0.6f), new Vector2(Range(120, 200), Range(90, 220)), new Vector2(Range(72, 95), Range(-10, 10)));
        Throw(-1, new Vector2(170, 150));
        foreach (float start in new[] { 0.5f, 1.5f, 2.5f, 4, 5.5f, 7, 9, 11.5f })
        {
            float birth = start + Range(-0.3f, 0.3f);
            var at = new Vector2(start < 7 ? Range(90, 200) : Range(0, 150), Range(40, 280));
            var velocity = new Vector2(Range(75, 92), Range(-18, 18));
            var behind = new Vector2(-30, Range(8, 22) * Sign());
            int count = random.Next(3, 5);
            for (int k = 0; k < count; k++)
                Shuriken(birth + 0.1f * k, at + behind * k + new Vector2(Range(-6, 6), Range(-6, 6)), velocity);
            Throw(birth, at + new Vector2(-40, 0));
        }

        // The arcs: tall pale "(" arcs standing in rows of two or three near the throws, fading (frames 1-19).
        foreach (var (birth, x, y, rows) in new[]
                 {
                     (0.5f, 170f, 135f, 1), (1.5f, 185f, 150f, 2), (3f, 175f, 120f, 3), (4.5f, 105f, 245f, 2), (6f, 200f, 255f, 1),
                     (8f, 40f, 95f, 1), (9f, 95f, 130f, 2), (10.5f, 185f, 250f, 1),
                 })
        {
            float height = Range(60, 95), life = Range(8, 11);
            for (int row = 0; row < rows; row++)
                particles.Add(new Particle(EffectTextures.BowArc, birth + 0.5f * row, life, new Vector2(x + 14 * row, y + 18 * row), Vector2.Zero, 0, 0,
                    new Vector2(-30, height * (1 - 0.12f * row)), Arc, t => t < 0.1f ? t / 0.1f : 1 - (t - 0.1f) / 0.9f, _ => 1));
        }

        // The streaks: a mesh of faint thin needles with bright heads over the target's half, most running down to the
        // right, spreading left (frames 3-27).
        for (int i = 0; i < 2000; i++)
        {
            float birth = 3 + 20 * MathF.Pow((float)random.NextDouble(), 1.1f), life = Math.Min(Range(4, 9), 27.5f - birth);
            var at = new Vector2(Range(StreaksFrom(birth), Width + 20), Range(-10, Height + 10));
            float angle = random.NextDouble() < 0.75 ? Range(0.3f, 1.2f) : Range(-0.9f, -0.2f), length = Range(30, 110);
            var direction = Vector2.FromAngle(angle);
            var velocity = direction * Range(3, 8);
            float strength = birth < 17 ? 0.16f : 0.1f;
            float Alpha(float t) => strength * (t < 0.2f ? t / 0.2f : t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f);
            particles.Add(new Particle(EffectTextures.Streak, birth, life, at, velocity, angle, 0, new Vector2(length, 2), Scratch, Alpha, _ => 1));
            if (i % 3 == 0)
                particles.Add(new Particle(EffectTextures.Dot, birth, life, at + direction * length * 0.4f, velocity, 0, 0, new Vector2(4, 4), White,
                    t => 3 * Alpha(t), _ => 1));
        }

        // The glints: gold crescents bulging left, bitten into points, in a soft glow, swelling a little and fading
        // (frames 9-27).
        for (int i = 0; i < 36; i++)
        {
            float birth = 9 + 13 * MathF.Pow((float)random.NextDouble(), 1.1f), life = Math.Min(Range(5, 8), 27.5f - birth);
            var at = new Vector2(Range(GlintsFrom(birth), birth < 18 ? 700 : 780), Range(30, Height - 10));
            float size = Range(30, 50) + 2.5f * (birth - 9), flip = random.NextDouble() < 0.8 ? 1 : -1, strength = birth < 17 ? 0.65f : 0.4f;
            float Alpha(float t) => strength * (t < 0.15f ? t / 0.15f : t < 0.4f ? 1 : 1 - (t - 0.4f) / 0.6f);
            particles.Add(new Particle(EffectTextures.Wing, birth, life, at, Vector2.Zero, Range(-0.4f, 0.4f), 0,
                new Vector2(flip * size, size), Glint, Alpha, t => 0.85f + 0.25f * t));
            particles.Add(new Particle(EffectTextures.Glow, birth, life, at, Vector2.Zero, 0, 0, new Vector2(size, size), Glint,
                t => 0.2f * Alpha(t), _ => 1));
        }

        // The sparkles: teal four-point stars in a soft glow and small white ones twinkling over the target's half
        // (frames 10-27).
        for (int i = 0; i < 110; i++)
        {
            float birth = Range(10, 25), life = Math.Min(Range(3, 6), 27.5f - birth);
            var at = new Vector2(Range(440, Width - 5), Range(5, Height - 5));
            bool teal = random.NextDouble() < 0.6;
            float size = teal ? Range(60, 100) : Range(25, 40), twinkle = Range(0, MathF.Tau);
            float Alpha(float t) => (0.6f + 0.2f * MathF.Sin(twinkle + t * 12)) * Fade(t);
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, Vector2.Zero, 0, 0, new Vector2(size, size), teal ? Teal : Twinkle,
                Alpha, _ => 1));
            if (teal)
                particles.Add(new Particle(EffectTextures.Glow, birth, life, at, Vector2.Zero, 0, 0, new Vector2(size, size) * 0.5f, Teal,
                    t => 0.35f * Alpha(t), _ => 1));
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
