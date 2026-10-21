using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The ice magic (docs/ui/mage-lines.md, "The plan: magic effects by element"), ours after Sengoku Rance's Miko Storm,
/// 40730, recoloured white-blue (owner, 2026-10-21; docs/sengoku/04-screen.md §2.4.1, "The magic's elements"; not
/// traced) — Sengoku Rance has no ice of its own. A flurry on the target's half: nothing at frame 0, the first ice blowing
/// in at the bottom right (2-5), a dense whirl of ice shards and snowflakes turning about a centre that climbs up and left
/// over the half (6-12), fullest at 12-18 with thin curly wind lines, bright cold glows and sparkles in it, then thinning
/// as it turns on (18-26), gone by frame ~29. The same seed makes the same effect.
/// </summary>
internal static class IceEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    // The colours (light added over the scene): white, ice blue, a paler blue for the wind, a faint haze.
    private static readonly Color White = new(0.92f, 0.97f, 1f), Ice = new(0.62f, 0.84f, 1f);
    private static readonly Color Wind = new(0.75f, 0.88f, 1f, 0.55f), Haze = new(0.45f, 0.65f, 1f, 0.22f);

    /// <summary>The whirl's centre at a frame: in from the bottom right corner, up and left to its place, then drifting on.</summary>
    private static Vector2 CentreAt(float frame)
    {
        var start = new Vector2(800, 310);
        var place = new Vector2(635, 165);
        var end = new Vector2(610, 150);
        if (frame <= 2)
            return start;
        if (frame <= 12)
        {
            float t = (frame - 2) / 10;
            return start.Lerp(place, 1 - (1 - t) * (1 - t));
        }
        return place.Lerp(end, Math.Min(1, (frame - 12) / 17));
    }

    /// <summary>The whirl's radius at a frame: small as it blows in, full from frame ~12.</summary>
    private static float SpreadAt(float frame) => Math.Clamp((frame - 1) / 11, 0.15f, 1f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // A piece turning about the centre: its radius (squeezed to an ellipse), its angle at birth and its turn per frame
        // (the whirl turns counter-clockwise on screen, the outer pieces slower).
        Func<float, Vector2> Orbit(float birth, float life, float radius, float angle, float turn) => t =>
        {
            float frame = birth + t * life;
            float a = angle - turn * (frame - birth);
            float r = radius * SpreadAt(frame);
            return CentreAt(frame) + new Vector2(MathF.Cos(a) * r * 1.3f, MathF.Sin(a) * r * 0.9f);
        };
        // A radius in the whirl, the inner part fuller.
        float Radius(float outer) => outer * MathF.Pow((float)random.NextDouble(), 0.65f) + 10;
        // The births: most from frame 2 to 16, fewer to 20.
        float Birth() => 2 + 18 * MathF.Pow((float)random.NextDouble(), 1.3f);

        // The haze: a faint cold body under everything.
        for (int i = 0; i < 4; i++)
        {
            float birth = 3 + i * 3, life = 14;
            var offset = new Vector2(Range(-60, 60), Range(-40, 40));
            particles.Add(new Particle(EffectTextures.Haze, birth, life, Vector2.Zero, Vector2.Zero, 0, 0,
                new Vector2(420, 320), Haze, Swell, t => 0.7f + 0.4f * t,
                Move: t => CentreAt(birth + t * life) + offset * SpreadAt(birth + t * life)));
        }

        // The wind: thin pale arcs laid along the whirl, turning with it.
        for (int i = 0; i < 46; i++)
        {
            float birth = Birth(), life = Range(6, 10);
            float radius = Radius(210), angle = Range(0, MathF.Tau), turn = Range(0.1f, 0.18f) * 120 / (radius + 60);
            float length = Range(80, 190);
            particles.Add(new Particle(EffectTextures.BowArc, birth, life, Vector2.Zero, Vector2.Zero,
                angle, -turn, new Vector2(length * 0.3f, length), Wind, Swell, t => 1 + 0.2f * t,
                Move: Orbit(birth, life, radius, angle, turn)));
        }
        // A few curls, as the reference's wind lines curl at their ends.
        for (int i = 0; i < 7; i++)
        {
            float birth = Birth(), life = Range(5, 8);
            float size = Range(35, 60);
            particles.Add(new Particle(EffectTextures.Swirl, birth, life, Vector2.Zero, Vector2.Zero, Range(0, MathF.Tau), -0.15f,
                new Vector2(size, size), Wind, Swell, t => 1 + 0.3f * t,
                Move: Orbit(birth, life, Radius(190), Range(0, MathF.Tau), Range(0.08f, 0.14f))));
        }

        // The cold glows: bright soft orbs riding the whirl.
        for (int i = 0; i < 30; i++)
        {
            float birth = Birth(), life = Range(4, 8);
            float size = Range(55, 130);
            particles.Add(new Particle(EffectTextures.Glow, birth, life, Vector2.Zero, Vector2.Zero, 0, 0,
                new Vector2(size, size), White * new Color(1, 1, 1, 0.85f), Swell, t => 1 + 0.3f * t,
                Move: Orbit(birth, life, Radius(190), Range(0, MathF.Tau), Range(0.08f, 0.16f))));
        }

        // The masses: bright frosty billows where the flurry is thickest, riding the whirl.
        for (int i = 0; i < 50; i++)
        {
            float birth = Birth(), life = Range(5, 9);
            float size = Range(60, 130);
            particles.Add(new Particle(EffectTextures.Billow, birth, life, Vector2.Zero, Vector2.Zero, Range(0, MathF.Tau), 0,
                new Vector2(size * 1.3f, size), Ice * new Color(1, 1, 1, 0.45f), Swell, t => 0.8f + 0.4f * t,
                Move: Orbit(birth, life, Radius(200), Range(0, MathF.Tau), Range(0.1f, 0.16f))));
        }

        // The shards: ice diamonds tumbling round the whirl in a dense flurry, white turning ice blue as they age.
        for (int i = 0; i < 720; i++)
        {
            float birth = Birth(), life = Range(7, 12);
            float radius = Radius(240);
            float size = Range(15, 32);
            particles.Add(new Particle(EffectTextures.Kite, birth, life, Vector2.Zero, Vector2.Zero, Range(0, MathF.Tau),
                Range(-0.4f, 0.4f), new Vector2(size * 1.6f, size), White, Swell, _ => 1,
                Ramp: t => White.Lerp(Ice, t),
                Move: Orbit(birth, life, radius, Range(0, MathF.Tau), Range(0.12f, 0.2f) * 110 / (radius + 40))));
        }

        // The snowflakes: larger, fewer, spinning slowly.
        for (int i = 0; i < 45; i++)
        {
            float birth = Birth(), life = Range(7, 11);
            float size = Range(20, 40);
            particles.Add(new Particle(EffectTextures.Flake, birth, life, Vector2.Zero, Vector2.Zero, Range(0, MathF.Tau),
                Range(-0.12f, 0.12f), new Vector2(size, size), White, Swell, _ => 1,
                Move: Orbit(birth, life, Radius(230), Range(0, MathF.Tau), Range(0.08f, 0.14f))));
        }

        // The sparkles: four-point glints flashing in the whirl.
        for (int i = 0; i < 36; i++)
        {
            float birth = Range(5, 24), life = Range(2, 4);
            float size = Range(25, 55);
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, Vector2.Zero, Vector2.Zero, Range(-0.2f, 0.2f), 0,
                new Vector2(size, size), White, Fade, t => 1 - 0.3f * t,
                Move: Orbit(birth, life, Radius(220), Range(0, MathF.Tau), 0.1f)));
        }

        // The last of the snow: fine specks drifting off the whirl's edge as it thins (16-29), within the half.
        for (int i = 0; i < 70; i++)
        {
            float birth = Range(15, 25), life = Range(4, 7);
            float angle = Range(0, MathF.Tau);
            var at = CentreAt(birth) + new Vector2(MathF.Cos(angle) * 1.3f, MathF.Sin(angle) * 0.9f) * Range(60, 240);
            at = new Vector2(Math.Clamp(at.X, Width / 2 + 20, Width), Math.Clamp(at.Y, 0, Height));
            float size = Range(5, 10);
            particles.Add(new Particle(EffectTextures.Glow, birth, life, at, new Vector2(Range(-2, 1), Range(0.5f, 2)), 0, 0,
                new Vector2(size, size) * 3, Ice * new Color(1, 1, 1, 0.8f), Fade, _ => 1));
        }
        return particles;
    }

    /// <summary>In quickly, full to the middle of its life, then out.</summary>
    private static float Swell(float t) => t < 0.15f ? t / 0.15f : t < 0.55f ? 1 : MathF.Pow(1 - (t - 0.55f) / 0.45f, 1.4f);

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
