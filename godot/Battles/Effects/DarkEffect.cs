using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The dark magic (docs/ui/mage-lines.md, "The plan: magic effects by element"), ours after Sengoku Rance's All-Targeting
/// Monster Magic, 47030 (owner, 2026-10-21; docs/sengoku/04-screen.md §2.4.1, "The magic's elements"; not traced). On the
/// target's half: a small magenta orb among faint violet specks (frame 0) swelling into a white ball (2), pink-violet
/// specks bursting out of it in a ring round a violet rim (3-7), a white blast, mottled and pink at its heart, swelling
/// over the whole half with a violet haze at its edge (7-15) and turning magenta, a dark violet sphere left where it was
/// (16-22), then violet glows, glints and small cyan curls fading by frame ~29. The same seed makes the same effect.
/// </summary>
internal static class DarkEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>Where the orb is and the blast swells from, on the target's half.</summary>
    private static readonly Vector2 Centre = new(635, 180);

    // The colours (light added over the scene).
    private static readonly Color White = new(1f, 0.95f, 1f), PinkWhite = new(1f, 0.8f, 0.97f);
    private static readonly Color Magenta = new(1f, 0.4f, 0.92f), Violet = new(0.62f, 0.38f, 1f);
    private static readonly Color Deep = new(0.42f, 0.14f, 0.85f), Dusk = new(0.35f, 0.32f, 0.7f);
    private static readonly Color Cyan = new(0.35f, 0.8f, 0.9f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        Vector2 Direction() => Vector2.Right.Rotated(Range(0, MathF.Tau));
        var particles = new List<Particle>();

        // The faint specks: dim blue-violet discs scattered round the orb while it swells (0-7).
        for (int i = 0; i < 34; i++)
        {
            var at = Centre + Direction() * new Vector2(1.2f, 0.9f) * Range(40, 210);
            float size = Range(14, 34);
            particles.Add(new Particle(EffectTextures.Bokeh, Range(-1, 2), Range(4, 7), at, Vector2.Zero, 0, 0,
                new Vector2(size, size), Dusk * new Color(1, 1, 1, 0.35f), Swell, _ => 1));
        }

        // The orb: a small magenta ball (0) swelling into a white one in a magenta halo (2), shrinking a little as the
        // specks burst from it (4), then swallowed by the blast.
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 3.5f, Centre, Vector2.Zero, 0, 0, new Vector2(110, 110), Magenta,
            Swell, t => 1 + 2.4f * t));
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 3, Centre, Vector2.Zero, 0, 0, new Vector2(45, 45), White,
            Swell, t => 1 + 2.5f * t));
        particles.Add(new Particle(EffectTextures.Glow, 0.8f, 4, Centre, Vector2.Zero, 0, 0, new Vector2(300, 300),
            Magenta * new Color(1, 1, 1, 0.4f), Swell, t => 1 + 0.2f * t));
        // The ball's sparkle: white and magenta specks over it as it swells (1-4).
        for (int i = 0; i < 70; i++)
        {
            var at = Centre + Direction() * 52 * MathF.Sqrt((float)random.NextDouble());
            float size = Range(8, 16);
            particles.Add(new Particle(EffectTextures.Glow, Range(1, 2.5f), Range(2, 3.5f), at, Vector2.Zero, 0, 0,
                new Vector2(size, size), random.NextDouble() < 0.5 ? White : Magenta, Swell, _ => 1));
        }
        particles.Add(new Particle(EffectTextures.Billow, 0.6f, 3.5f, Centre, Vector2.Zero, 0, 0, new Vector2(150, 150), White,
            Swell, t => 1 + 0.4f * t));
        particles.Add(new Particle(EffectTextures.Glow, 1.5f, 6.5f, Centre, Vector2.Zero, 0, 0, new Vector2(190, 190), White,
            Swell, t => 1 - 0.2f * t));
        // The rim round it as the specks burst (4-7).
        particles.Add(new Particle(EffectTextures.Sphere, 4.5f, 3.5f, Centre, Vector2.Zero, 0, 0, new Vector2(140, 140), Violet,
            Swell, t => 1 + 0.4f * t));

        // The burst: pink-violet specks flying out of the orb in a ring, slowing as they go (3-9).
        for (int i = 0; i < 240; i++)
        {
            float birth = Range(2.5f, 5), life = Range(4, 6.5f);
            var direction = Direction() * new Vector2(1.1f, 0.95f);
            float from = Range(35, 70), to = from + Range(50, 130);
            float size = Range(7, 18);
            var colour = random.NextDouble() < 0.6 ? Magenta : Violet;
            particles.Add(new Particle(EffectTextures.Glow, birth, life, Centre, Vector2.Zero, 0, 0, new Vector2(size, size) * 1.6f,
                colour, Swell, _ => 1, Move: t => direction * (from + (to - from) * (1 - (1 - t) * (1 - t)))));
        }

        // The blast: white billows swelling over the whole half, fewer at its heart (so it shows mottled), turning pink
        // and magenta as they age (7-17), wider toward the actor, with a violet haze spreading past its edge.
        var blastAt = Centre + new Vector2(10, -5);
        particles.Add(new Particle(EffectTextures.Haze, 7.5f, 11, blastAt, Vector2.Zero, 0, 0, new Vector2(900, 620),
            Violet * new Color(1, 1, 1, 0.55f), Swell, t => 0.8f + 0.3f * t));
        Color Aged(float t) => t < 0.5f ? White.Lerp(PinkWhite, t * 2) : PinkWhite.Lerp(Magenta, (t - 0.5f) * 2);
        for (int i = 0; i < 28; i++)
        {
            float birth = Range(6.5f, 8.5f), life = Range(8.5f, 10);
            var direction = Direction() * new Vector2(1.35f, 0.95f);
            float radius = Range(110, 210);
            float size = Range(260, 380);
            particles.Add(new Particle(EffectTextures.Glow, birth, life, blastAt, Vector2.Zero, 0, 0,
                new Vector2(size, size), White, Hold, t => 0.6f + 0.5f * t,
                Ramp: t => Aged(t) * new Color(1, 1, 1, 0.8f), Move: t => direction * radius * (0.4f + 0.6f * MathF.Sqrt(t))));
        }
        // Its magenta afterglow as it turns (10-18).
        particles.Add(new Particle(EffectTextures.Haze, 10, 8, blastAt, Vector2.Zero, 0, 0, new Vector2(760, 560),
            Magenta * new Color(1, 1, 1, 0.65f), Swell, t => 1 + 0.15f * t));
        // Its mottling: faint billows all over it.
        for (int i = 0; i < 70; i++)
        {
            float birth = Range(6.5f, 9.5f), life = Range(6, 8.5f);
            var direction = Direction() * new Vector2(1.35f, 0.95f);
            float radius = 260 * MathF.Sqrt((float)random.NextDouble());
            float size = Range(90, 170);
            particles.Add(new Particle(EffectTextures.Billow, birth, life, blastAt, Vector2.Zero, Range(0, MathF.Tau), 0.02f,
                new Vector2(size, size), White, Swell, t => 0.7f + 0.5f * t,
                Ramp: t => Aged(t) * new Color(1, 1, 1, 0.3f), Move: t => direction * radius * (0.5f + 0.5f * MathF.Sqrt(t))));
        }
        // The heart: dim grey-violet billows, darker than the white round them.
        for (int i = 0; i < 10; i++)
        {
            var at = blastAt + Direction() * Range(0, 70);
            float size = Range(90, 160);
            particles.Add(new Particle(EffectTextures.Billow, Range(8, 10), Range(5, 7), at, Vector2.Zero, Range(0, MathF.Tau), 0,
                new Vector2(size, size), Dusk * new Color(1, 1, 1, 0.5f), Swell, t => 1 + 0.4f * t));
        }

        // The dark sphere: a violet ball with a bright rim where the blast was, a little lower (16-23).
        var sphereAt = Centre + new Vector2(25, 45);
        particles.Add(new Particle(EffectTextures.Haze, 15, 8, sphereAt, Vector2.Zero, 0, 0, new Vector2(420, 420),
            Deep, Swell, t => 1 + 0.1f * t));
        particles.Add(new Particle(EffectTextures.Sphere, 15.5f, 7.5f, sphereAt, Vector2.Zero, 0, 0, new Vector2(400, 400),
            Violet, Swell, t => 1 + 0.08f * t));
        // Its mottled violet body.
        for (int i = 0; i < 18; i++)
        {
            var at = sphereAt + Direction() * Range(0, 120);
            float size = Range(120, 200);
            particles.Add(new Particle(EffectTextures.Billow, Range(14.5f, 16.5f), Range(6, 8), at, Vector2.Zero, Range(0, MathF.Tau), 0,
                new Vector2(size, size), Violet * new Color(1, 1, 1, 0.1f), Swell, t => 1 + 0.1f * t));
        }
        // The pink-white clumps glowing in it as it fades (19-25).
        for (int i = 0; i < 16; i++)
        {
            var at = sphereAt + Direction() * Range(0, 90);
            float size = Range(40, 95);
            particles.Add(new Particle(EffectTextures.Glow, Range(18.5f, 21), Range(4, 6), at, Vector2.Zero, 0, 0,
                new Vector2(size, size), PinkWhite * new Color(1, 1, 1, 0.8f), Swell, _ => 1));
        }

        // The violet glows: soft balls over the half and past it toward the actor (15-29).
        for (int i = 0; i < 34; i++)
        {
            var at = new Vector2(Width - 560 * MathF.Pow((float)random.NextDouble(), 0.8f), Range(0, Height));
            float size = Range(50, 140);
            particles.Add(new Particle(EffectTextures.Glow, Range(14.5f, 21), Range(7, 11), at, Vector2.Zero, 0, 0,
                new Vector2(size, size), (random.NextDouble() < 0.7 ? Violet : Magenta) * new Color(1, 1, 1, 0.55f), Swell, _ => 1));
        }
        // The glints: white-pink stars twinkling on the half (19-29).
        for (int i = 0; i < 70; i++)
        {
            var at = new Vector2(Width - 400 * MathF.Pow((float)random.NextDouble(), 1.2f), Range(0, Height));
            float size = Range(16, 38), birth = Range(19, 24), life = Range(5, 9);
            float phase = Range(0, MathF.Tau);
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, Vector2.Zero, Range(-0.3f, 0.3f), 0,
                new Vector2(size, size), PinkWhite, t => Swell(t) * (0.65f + 0.35f * MathF.Sin(phase + t * 20)), _ => 1));
            particles.Add(new Particle(EffectTextures.Glow, birth, life, at, Vector2.Zero, 0, 0, new Vector2(size, size) * 0.9f,
                Violet * new Color(1, 1, 1, 0.6f), Swell, _ => 1));
        }
        // The curls: small cyan arcs scattered from the blast's edge toward the actor, drifting (14-28).
        for (int i = 0; i < 80; i++)
        {
            var at = new Vector2(Width - 650 * MathF.Pow((float)random.NextDouble(), 0.9f), Range(0, Height));
            float size = Range(7, 13);
            particles.Add(new Particle(EffectTextures.BowArc, Range(14, 20), Range(7, 11), at,
                new Vector2(Range(-0.8f, 0.8f), Range(-0.8f, 0.8f)), Range(0, MathF.Tau), Range(-0.1f, 0.1f),
                new Vector2(size, size * 1.6f), Cyan * new Color(1, 1, 1, 0.55f), Swell, _ => 1));
        }
        return particles;
    }

    /// <summary>In quickly, full to the middle of its life, then out.</summary>
    private static float Swell(float t) => t < 0.15f ? t / 0.15f : t < 0.55f ? 1 : MathF.Pow(1 - (t - 0.55f) / 0.45f, 1.4f);

    /// <summary>In quickly, full for three quarters of its life, then out.</summary>
    private static float Hold(float t) => t < 0.1f ? t / 0.1f : t < 0.75f ? 1 : MathF.Pow(1 - (t - 0.75f) / 0.25f, 1.2f);
}
