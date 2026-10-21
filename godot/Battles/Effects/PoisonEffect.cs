using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The poison magic (docs/ui/mage-lines.md, "The plan: magic effects by element"), ours after Sengoku Rance's Death by
/// Poison, 41300 (owner, 2026-10-21; docs/sengoku/04-screen.md §2.4.1, "The magic's elements"; not traced). Clumps of
/// gas, marbled green, lilac, pink and slate blue (<see cref="EffectTextures.Gas"/>), on the target's half: a few small
/// ones popping up round the target among glittering specks with a flash at their foot (0-3), more and larger ones
/// bursting out of a white flash beside the target (3-7), the cloud swelling over the whole half with bright green and
/// pink-white hearts and a slate haze toward the actor (7-14), then drifting off to the top and bottom right and
/// thinning, a few stray clumps left floating toward the actor, gone by frame ~28. The same seed makes the same effect.
/// </summary>
internal static class PoisonEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>Where the cloud swells from, on the target's half.</summary>
    private static readonly Vector2 Centre = new(610, 175);

    /// <summary>The flash the big clumps burst out of (3-7).</summary>
    private static readonly Vector2 FlashAt = new(680, 215);

    // The colours (light added over the scene).
    private static readonly Color White = new(1f, 0.97f, 1f), Mint = new(0.72f, 1f, 0.76f), Green = new(0.32f, 0.85f, 0.4f);
    private static readonly Color Lilac = new(0.75f, 0.48f, 1f), Pink = new(1f, 0.58f, 0.9f), Slate = new(0.52f, 0.52f, 0.95f);
    private static readonly Color Ash = new(0.58f, 0.64f, 0.62f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        Vector2 Direction() => Vector2.Right.Rotated(Range(0, MathF.Tau));
        Texture2D Gas() => EffectTextures.Gas(random.Next(EffectTextures.GasVariants));
        Color GasColour()
        {
            double pick = random.NextDouble();
            return pick < 0.38 ? Green : pick < 0.62 ? Lilac : pick < 0.8 ? Pink : pick < 0.92 ? Slate : Ash;
        }
        var particles = new List<Particle>();

        // A clump: a marbled blob turning slowly, growing as it drifts.
        void Clump(Vector2 at, float birth, float life, float size, Color colour, float strength, Vector2 drift, float grow,
            Func<float, float>? alpha = null)
        {
            particles.Add(new Particle(Gas(), birth, life, at, drift, Range(0, MathF.Tau), Range(-0.03f, 0.03f),
                new Vector2(size, size * Range(0.85f, 1.1f)), colour * new Color(1, 1, 1, strength), alpha ?? Swell,
                t => 1 + grow * MathF.Sqrt(t)));
        }

        // The glitter: small stars twinkling over the clumps, white, mint and pink.
        void Glitter(Vector2 at, float birth, float life)
        {
            float size = Range(18, 36), phase = Range(0, MathF.Tau);
            double pick = random.NextDouble();
            var colour = pick < 0.5 ? White : pick < 0.8 ? Mint : Pink;
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, new Vector2(Range(-0.5f, 0.5f), Range(-0.5f, 0.5f)),
                Range(-0.4f, 0.4f), 0, new Vector2(size, size), colour,
                t => Swell(t) * (0.6f + 0.4f * MathF.Sin(phase + t * 18)), _ => 1));
        }

        // The first clumps: a few small ones popping up round the target (0-6), each with its glitter.
        Vector2[] first = [new(395, 175), new(570, 215), new(575, 95), new(560, 250), new(640, 140)];
        for (int i = 0; i < first.Length; i++)
        {
            float birth = i < 3 ? Range(-0.8f, 0) : Range(0.5f, 1.5f);
            Clump(first[i], birth, Range(6, 8), Range(80, 115), i % 2 == 0 ? Lilac : Green, 0.5f,
                (first[i] - Centre).Normalized() * Range(0.6f, 1.4f), 0.6f);
            for (int k = 0; k < 9; k++)
                Glitter(first[i] + Direction() * Range(0, 45), birth + Range(0, 2), Range(2.5f, 4));
        }
        // The flash at their foot (1-3).
        var footAt = new Vector2(470, 305);
        particles.Add(new Particle(EffectTextures.Glow, 0.3f, 3.2f, footAt, Vector2.Zero, 0, 0, new Vector2(240, 200),
            Mint * new Color(1, 1, 1, 0.35f), Swell, t => 1 + 0.4f * t));
        particles.Add(new Particle(EffectTextures.Rays, 0.5f, 3, footAt, Vector2.Zero, Range(0, MathF.Tau), 0.05f,
            new Vector2(220, 220), White * new Color(1, 1, 1, 0.3f), Swell, t => 1 + 0.3f * t));

        // The burst: larger clumps welling out round the target (2-8), pink and lilac low, green high.
        for (int i = 0; i < 16; i++)
        {
            var direction = Direction() * new Vector2(1.3f, 0.85f);
            var at = Centre + direction * Range(30, 120);
            var colour = at.Y > Centre.Y ? (random.NextDouble() < 0.6 ? Pink : Lilac) : (random.NextDouble() < 0.6 ? Green : Ash);
            Clump(at, Range(2, 4.5f), Range(7, 9), Range(130, 200), colour, 0.3f, direction * Range(1, 2.5f), 0.7f);
        }
        // The flash they burst out of: a white sun with rays in a pink halo beside the target (3-7).
        particles.Add(new Particle(EffectTextures.Glow, 2.5f, 5, FlashAt, Vector2.Zero, 0, 0, new Vector2(330, 300),
            Pink * new Color(1, 1, 1, 0.35f), Swell, t => 1 + 0.3f * t));
        particles.Add(new Particle(EffectTextures.Glow, 3, 4.5f, FlashAt, Vector2.Zero, 0, 0, new Vector2(150, 130),
            White * new Color(1, 1, 1, 0.6f), Swell, t => 1 + 0.5f * t));
        particles.Add(new Particle(EffectTextures.Rays, 3, 4.5f, FlashAt, Vector2.Zero, Range(0, MathF.Tau), 0.04f,
            new Vector2(300, 300), White * new Color(1, 1, 1, 0.4f), Swell, t => 1 + 0.3f * t));
        // The glitter of the burst, flung out over the half and past it (2-11).
        for (int i = 0; i < 220; i++)
        {
            var at = Centre + Direction() * new Vector2(1.5f, 0.9f) * 260 * MathF.Sqrt((float)random.NextDouble());
            Glitter(at, Range(2, 7), Range(3, 5));
        }

        // The cloud: big clumps swelling over the whole half (6-18), drifting outward and to the right.
        for (int i = 0; i < 34; i++)
        {
            var direction = Direction() * new Vector2(1.4f, 0.9f);
            var at = Centre + new Vector2(20, 0) + direction * 200 * MathF.Sqrt((float)random.NextDouble());
            Clump(at, Range(5, 8.5f), Range(10, 13), Range(170, 280), GasColour(), Range(0.13f, 0.22f),
                direction * Range(0.6f, 1.6f) + new Vector2(Range(0.5f, 1.5f), 0), 0.45f, Hold);
        }
        // Its bright hearts: a green-white glow high on the right, a pink-white one low (7-12).
        foreach (var (at, colour) in new[] { (new Vector2(640, 55), Mint), (new Vector2(560, 255), Pink), (new Vector2(760, 205), White) })
        {
            particles.Add(new Particle(EffectTextures.Glow, Range(6, 7.5f), Range(4.5f, 6), at, Vector2.Zero, 0, 0,
                new Vector2(170, 150), colour * new Color(1, 1, 1, 0.3f), Swell, t => 1 + 0.4f * t));
            for (int k = 0; k < 3; k++)
                Clump(at + Direction() * Range(0, 40), Range(6, 7.5f), Range(5, 7), Range(90, 130), White, 0.15f,
                    Vector2.Zero, 0.4f);
        }
        // The slate haze toward the actor: slate blue and lilac clumps over the half's left edge (7-15), drifting left.
        var hazeAt = new Vector2(420, 90);
        particles.Add(new Particle(EffectTextures.Haze, 6.5f, 9, hazeAt, Vector2.Zero, 0, 0, new Vector2(380, 300),
            Slate * new Color(1, 1, 1, 0.28f), Swell, t => 1 + 0.2f * t));
        for (int i = 0; i < 9; i++)
        {
            var at = hazeAt + Direction() * new Vector2(110, 70) * MathF.Sqrt((float)random.NextDouble());
            Clump(at, Range(6, 8), Range(7, 9), Range(110, 170), random.NextDouble() < 0.65 ? Slate : Lilac, 0.3f,
                new Vector2(Range(-1.2f, -0.2f), Range(-0.4f, 0.4f)), 0.4f);
        }

        // The drift: the cloud's remains floating off to the top and bottom right and thinning (13-28).
        for (int i = 0; i < 26; i++)
        {
            bool high = i % 2 == 0;
            var at = new Vector2(Range(560, 760), high ? Range(20, 140) : Range(150, 290));
            var drift = new Vector2(Range(1, 2.5f), high ? Range(-1.2f, -0.2f) : Range(0.1f, 1));
            var colour = random.NextDouble() < 0.6 ? Green : random.NextDouble() < 0.6 ? Lilac : Ash;
            Clump(at, Range(12, 16), Range(9, 14), Range(130, 240), colour, Range(0.18f, 0.28f), drift, 0.3f, Linger);
        }
        // The last two, green, slowest to go: one at the top, one at the right (20-28).
        Clump(new Vector2(640, 30), 18, 10, 110, Green, 0.32f, new Vector2(0.6f, -0.3f), 0.2f, Linger);
        Clump(new Vector2(720, 205), 18, 11, 120, Green, 0.32f, new Vector2(0.8f, 0.2f), 0.2f, Linger);
        // The strays: small clumps floating toward the actor (5-24), faint, a few glints on them.
        for (int i = 0; i < 16; i++)
        {
            var at = new Vector2(Range(150, 520), Range(15, Height - 15));
            float birth = Range(5, 12);
            Clump(at, birth, Range(9, 14), Range(40, 80), random.NextDouble() < 0.5 ? Green : Lilac, 0.38f,
                new Vector2(Range(-0.8f, 0.3f), Range(-0.4f, 0.4f)), 0.15f);
            if (i % 2 == 0)
                Glitter(at, birth + Range(1, 4), Range(3, 5));
        }
        // The late glitter: faint stars over the whole field (8-22).
        for (int i = 0; i < 60; i++)
            Glitter(new Vector2(Width - 700 * MathF.Pow((float)random.NextDouble(), 0.8f), Range(0, Height)), Range(8, 18), Range(3, 5));
        return particles;
    }

    /// <summary>In quickly, full to the middle of its life, then out.</summary>
    private static float Swell(float t) => t < 0.15f ? t / 0.15f : t < 0.55f ? 1 : MathF.Pow(1 - (t - 0.55f) / 0.45f, 1.4f);

    /// <summary>In quickly, full for two thirds of its life, then out.</summary>
    private static float Hold(float t) => t < 0.12f ? t / 0.12f : t < 0.65f ? 1 : MathF.Pow(1 - (t - 0.65f) / 0.35f, 1.3f);

    /// <summary>In slowly, then fading all the rest of its life.</summary>
    private static float Linger(float t) => t < 0.2f ? t / 0.2f : MathF.Pow(1 - (t - 0.2f) / 0.8f, 1.1f);
}
