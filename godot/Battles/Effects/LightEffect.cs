using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The light magic (docs/ui/mage-lines.md, "The plan: magic effects by element"), ours after Sengoku Rance's Angel Cutter,
/// 40430 (owner, 2026-10-21; docs/sengoku/04-screen.md §2.4.1, "The magic's elements"; not traced). Thrown from the
/// actor's side: a small warm orb (frame 0) swelling into a white sun in an orange-gold halo with rays (1-2), gold specks
/// gathering round it (3), then a stream of crescent blades, white and pale grey, their hollows behind them, sweeping right
/// across the field with white streaks (3-27), a faint glassy sphere left where the orb was (6-16), warm glints lingering
/// on the actor's side and yellow glints and curls among the blades on the target's (12-26). The same seed makes the
/// same effect.
/// </summary>
internal static class LightEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>Where the orb is, on the actor's side, and the blades start from.</summary>
    private static readonly Vector2 Orb = new(305, 170);

    // The colours (light added over the scene).
    private static readonly Color White = new(1f, 0.99f, 0.95f), Blade = new(0.78f, 0.78f, 0.75f);
    private static readonly Color Orange = new(1f, 0.55f, 0.25f), Gold = new(1f, 0.8f, 0.4f), Yellow = new(1f, 0.88f, 0.3f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The orb: a warm spark (0) swelling into a white sun in an orange halo turning gold, with rays (1-3), then
        // fading into the blades' start (to ~8).
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 2.5f, Orb, Vector2.Zero, 0, 0, new Vector2(110, 110),
            Orange * new Color(1, 1, 1, 0.8f), Swell, t => 1 + 1.5f * t));
        particles.Add(new Particle(EffectTextures.Glow, 0.3f, 5, Orb, Vector2.Zero, 0, 0, new Vector2(420, 420),
            Orange * new Color(1, 1, 1, 0.8f), Swell, _ => 1, Ramp: t => Orange.Lerp(Gold, Math.Min(1, t * 2)) * new Color(1, 1, 1, 0.8f)));
        particles.Add(new Particle(EffectTextures.Haze, 0.3f, 5, Orb, Vector2.Zero, 0, 0, new Vector2(380, 380),
            Gold * new Color(1, 1, 1, 0.55f), Swell, t => 1 + 0.15f * t));
        particles.Add(new Particle(EffectTextures.Rays, 0.5f, 3, Orb, Vector2.Zero, Range(0, MathF.Tau), 0.04f,
            new Vector2(330, 330), Gold * new Color(1, 1, 1, 0.6f), Swell, t => 1 + 0.2f * t));
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 9, Orb, Vector2.Zero, 0, 0, new Vector2(60, 60), White,
            Swell, t => 1 + 2.4f * MathF.Min(1, t * 3)));
        for (int i = 0; i < 10; i++)
        {
            var at = Orb + Vector2.Right.Rotated(Range(0, MathF.Tau)) * Range(0, 40);
            float size = Range(60, 100);
            particles.Add(new Particle(EffectTextures.Billow, Range(0.5f, 2), Range(3, 6), at, Vector2.Zero, Range(0, MathF.Tau), 0,
                new Vector2(size, size), White * new Color(1, 1, 1, 0.75f), Swell, t => 1 + 0.3f * t));
        }

        // The specks: warm glints gathering round the sun (2-5), then scattered on the actor's side, lingering (to ~26).
        for (int i = 0; i < 150; i++)
        {
            bool round = i < 50;
            var at = round
                ? Orb + Vector2.Right.Rotated(Range(0, MathF.Tau)) * new Vector2(1.1f, 1) * Range(45, 95)
                : new Vector2(Range(60, 480), Range(0, Height));
            float birth = round ? Range(1.8f, 3.5f) : Range(4, 9), life = round ? Range(5, 9) : Range(10, 18);
            float size = Range(20, 44), phase = Range(0, MathF.Tau);
            var colour = random.NextDouble() < 0.5 ? Gold : Orange;
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, new Vector2(Range(-0.3f, 0.3f), Range(-0.3f, 0.3f)),
                MathF.PI / 4 * random.Next(2), 0, new Vector2(size, size), colour,
                t => Swell(t) * (0.6f + 0.4f * MathF.Sin(phase + t * 18)), _ => 1));
            particles.Add(new Particle(EffectTextures.Glow, birth, life, at, Vector2.Zero, 0, 0, new Vector2(size, size) * 0.8f,
                colour * new Color(1, 1, 1, 0.5f), Swell, _ => 1));
        }

        // The sphere: a faint glassy ball where the orb was, a little to its right (6-16).
        particles.Add(new Particle(EffectTextures.Sphere, 5.5f, 11, Orb + new Vector2(25, -5), Vector2.Zero, 0, 0,
            new Vector2(240, 240), White * new Color(1, 1, 1, 0.45f), Swell, t => 1 + 0.15f * t));

        // The blades: crescents, their hollows facing back, flying right from the orb in a broadening stream, the bigger
        // ones paler, each with a white streak behind it (3-27).
        for (int i = 0; i < 130; i++)
        {
            float birth = 3 + 15 * MathF.Pow((float)random.NextDouble(), 1.3f), life = Range(6, 10);
            float spread = Math.Min(1, (birth - 2) / 6);
            var from = Orb + new Vector2(Range(-20, 60), Range(-120, 120) * (0.4f + 0.6f * spread));
            float speed = Range(40, 110), climb = Range(-6f, 6f) * (0.3f + 0.7f * spread);
            float size = Range(30, 100) * (random.NextDouble() < 0.12 ? 1.6f : 1);
            var colour = (size > 90 ? Blade : White.Lerp(Blade, Range(0, 0.8f))) * new Color(1, 1, 1, Range(0.45f, 0.85f));
            var velocity = new Vector2(speed, climb);
            particles.Add(new Particle(EffectTextures.Sickle, birth, life, from, velocity, Range(-0.15f, 0.15f), 0,
                new Vector2(size * 1.25f, size * 0.85f), colour, Swell, t => 0.8f + 0.4f * t));
            if (random.NextDouble() < 0.6)
                particles.Add(new Particle(EffectTextures.Streak, birth, life, from - new Vector2(size * 0.8f, 0), velocity, 0, 0,
                    new Vector2(size * 3, size * 0.35f), White * new Color(1, 1, 1, 0.55f), Swell, _ => 1));
        }
        // The light at the stream's root: a white glow the blades leave from (4-12).
        particles.Add(new Particle(EffectTextures.Glow, 3.5f, 9, Orb + new Vector2(70, 5), Vector2.Zero, 0, 0,
            new Vector2(300, 220), White * new Color(1, 1, 1, 0.8f), Swell, t => 1 + 0.4f * t));

        // The yellow glints and curls among the blades on the target's side (11-26).
        for (int i = 0; i < 120; i++)
        {
            var at = new Vector2(Range(400, Width), Orb.Y + Range(-130, 130) * MathF.Sqrt((float)random.NextDouble()));
            float birth = Range(10.5f, 20), life = Range(5, 9);
            float size = Range(14, 34), phase = Range(0, MathF.Tau);
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, new Vector2(Range(1, 4), 0), 0, 0,
                new Vector2(size, size), Yellow, t => Swell(t) * (0.6f + 0.4f * MathF.Sin(phase + t * 18)), _ => 1));
            particles.Add(new Particle(EffectTextures.Glow, birth, life, at, new Vector2(Range(1, 4), 0), 0, 0,
                new Vector2(size, size) * 2.2f, Yellow * new Color(1, 1, 1, 0.65f), Swell, _ => 1));
        }
        for (int i = 0; i < 12; i++)
        {
            var at = new Vector2(Range(480, Width), Range(20, Height - 20));
            float size = Range(30, 55);
            particles.Add(new Particle(EffectTextures.Swirl, Range(11, 20), Range(5, 8), at, new Vector2(2, 0), Range(0, MathF.Tau), 0.1f,
                new Vector2(size, size), Yellow * new Color(1, 1, 1, 0.6f), Swell, _ => 1));
        }
        return particles;
    }

    /// <summary>In quickly, full to the middle of its life, then out.</summary>
    private static float Swell(float t) => t < 0.15f ? t / 0.15f : t < 0.55f ? 1 : MathF.Pow(1 - (t - 0.55f) / 0.45f, 1.4f);
}
