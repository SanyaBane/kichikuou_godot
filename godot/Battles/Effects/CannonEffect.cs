using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The cannon effect, Maria's (unit 16) portable cannons' Indirect attack (owner, 2026-11-04), ours after Sengoku Rance's
/// Cannoneer effect 40330, which her Tulip plays there (docs/sengoku/04-screen.md §2.4.1; not traced). On the actor's
/// half (left) a flash at each barrel's mouth as her ranks fire (frames 0-2, the mouths where her sprites hold them,
/// CG 1167-1169), then pale gunsmoke billowing out of them, swelling and drifting back and up, thinning by frame ~22;
/// shells streak right across the field (1-6). On the target's half (right) the shells burst (from frame 5): a white flash,
/// fire billows white at the heart and yellow, orange and smoke-red toward the edges, spark streaks thrown up and falling,
/// a second smaller burst beside it (8), the fire burning to frame ~24, then embers and a dark smoke settling to nothing by frame ~29. The same seed
/// makes the same effect.
/// </summary>
internal static class CannonEffect
{
    private const float Half = EffectCanvas.DesignWidth / 2, Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>The barrels' mouths on the actor's half (design space), as Maria's ranks hold them.</summary>
    private static readonly Vector2[] Muzzles =
    [
        new(75, 145), new(97, 193), new(65, 227), new(181, 168), new(169, 215), new(269, 145), new(312, 194), new(281, 227),
    ];

    /// <summary>Where the main burst and the second one stand, on the target's half.</summary>
    private static readonly Vector2 Burst = new(610, 190), Second = new(700, 230);

    // The colours (light added over the scene).
    private static readonly Color Flash = new(1f, 0.95f, 0.75f), FlashHalo = new(1f, 0.7f, 0.3f, 0.8f);
    private static readonly Color Gunsmoke = new(0.42f, 0.4f, 0.34f, 0.5f), GunsmokeOld = new(0.2f, 0.19f, 0.16f, 0.4f);
    private static readonly Color Shell = new(1f, 0.85f, 0.5f, 0.9f);
    private static readonly Color White = new(1f, 0.97f, 0.88f), Yellow = new(1f, 0.8f, 0.3f);
    private static readonly Color Orange = new(0.95f, 0.42f, 0.08f), Soot = new(0.3f, 0.1f, 0.03f);
    private static readonly Color Spark = new(1f, 0.82f, 0.45f, 0.9f), Ember = new(1f, 0.55f, 0.15f, 0.85f);
    private static readonly Color Smoke = new(0.16f, 0.12f, 0.1f, 0.45f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The volley: each barrel flashes in turn over frames 0-2, a hot core in a warm halo, and fires a shell.
        foreach (var muzzle in Muzzles)
        {
            float shot = Range(-0.4f, 2);
            var mouth = muzzle + new Vector2(Range(-4, 4), Range(-4, 4));
            particles.Add(new Particle(EffectTextures.Glow, shot, 2.5f, mouth, Vector2.Zero, 0, 0, new Vector2(110, 90), FlashHalo,
                Fade, t => 1 + 0.4f * t));
            particles.Add(new Particle(EffectTextures.Burst, shot, 1.8f, mouth + new Vector2(10, 0), Vector2.Zero, Range(-0.2f, 0.2f), 0,
                new Vector2(70, 40), Flash, Fade, t => 1 + 0.6f * t));
            // The shell: a short bright streak across the field to the bursts, fast, a frame or two behind the flash.
            var target = Burst + new Vector2(Range(-90, 90), Range(-60, 60));
            var path = target - mouth;
            particles.Add(new Particle(EffectTextures.Dash, shot + 0.6f, 3.5f, mouth, Vector2.Zero, path.Angle(), 0,
                new Vector2(60, 5), Shell, t => t < 0.85f ? 1 : (1 - t) / 0.15f, _ => 1, Move: t => path * t));
            // The gunsmoke out of the mouth: pale billows pushed forward a little, then drifting back and up as they swell.
            for (int i = 0; i < 5; i++)
            {
                float size = Range(45, 80), birth = shot + Range(0.3f, 2.5f);
                var push = new Vector2(Range(25, 70), Range(-15, 10));
                particles.Add(new Particle(EffectTextures.Billow, birth, Range(14, 20), mouth, new Vector2(Range(-1.6f, -0.4f), Range(-1.2f, -0.2f)),
                    Range(0, MathF.Tau), Range(-0.02f, 0.02f), new Vector2(size * 1.5f, size), Gunsmoke, Settle,
                    t => 0.6f + 1.6f * MathF.Sqrt(t), Ramp: t => Gunsmoke.Lerp(GunsmokeOld, t),
                    Move: t => push * (1 - MathF.Pow(1 - Math.Min(1, t * 4), 3))));
            }
        }

        AddBurst(particles, random, Burst, 5, 1f);
        AddBurst(particles, random, Second, 8, 0.6f);

        // The dark smoke the bursts leave, rising slowly over the target's half.
        for (int i = 0; i < 26; i++)
        {
            float size = Range(90, 160);
            particles.Add(new Particle(EffectTextures.Billow, Range(10, 16), Range(12, 18),
                Burst + new Vector2(Range(-130, 170), Range(-60, 70)), new Vector2(Range(-0.5f, 0.8f), Range(-1.6f, -0.4f)),
                Range(0, MathF.Tau), 0, new Vector2(size * 1.4f, size), Smoke, Settle, t => 1 + 0.6f * t));
        }
        return particles;
    }

    /// <summary>One shell's burst at <paramref name="at"/> from frame <paramref name="start"/>, <paramref name="scale"/> its size.</summary>
    private static void AddBurst(List<Particle> particles, Random random, Vector2 at, float start, float scale)
    {
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);

        // The flash: a white glow and a burst of rays, a frame and a half.
        particles.Add(new Particle(EffectTextures.Glow, start - 0.5f, 3, at, Vector2.Zero, 0, 0, new Vector2(320, 300) * scale, White,
            Fade, t => 1 + 0.5f * t));
        particles.Add(new Particle(EffectTextures.Rays, start - 0.5f, 2.5f, at, Vector2.Zero, Range(0, MathF.Tau), 0.04f,
            new Vector2(300, 300) * scale, FlashHalo, Fade, t => 1 + 0.3f * t));

        // The fire: billows thrown out from the heart, fast at first and slowing, white at birth and burning to soot.
        for (int i = 0; i < (int)(70 * scale); i++)
        {
            float birth = start + 8 * MathF.Pow((float)random.NextDouble(), 1.6f), angle = Range(0, MathF.Tau);
            float reach = (60 + (birth - start) * 16) * scale * MathF.Pow((float)random.NextDouble(), 0.6f) + 10;
            var direction = new Vector2(MathF.Cos(angle) * 1.5f, MathF.Sin(angle) * 1.05f - 0.25f);
            float size = Range(80, 150) * scale;
            particles.Add(new Particle(EffectTextures.Puff, birth, Range(11, 15), at, Vector2.Zero, Range(0, MathF.Tau),
                Range(-0.02f, 0.02f), new Vector2(size, size), White, Billow, t => 0.6f + 0.6f * t,
                Ramp: t => FireColour(0.6f * t + (birth - start) / 20),
                Move: t => direction * reach * (1 - MathF.Pow(1 - t, 3)) + new Vector2(0, -12 * t)));
        }

        // The sparks: thin streaks thrown up and out, slowing and falling back as they fade.
        for (int i = 0; i < (int)(60 * scale); i++)
        {
            float angle = -MathF.PI / 2 + Range(-1.1f, 1.1f), speed = Range(9, 20) * scale;
            var velocity = Vector2.FromAngle(angle) * speed;
            particles.Add(new Particle(EffectTextures.Dash, start + Range(0, 4), Range(4, 7), at + new Vector2(Range(-30, 30), Range(-20, 10)),
                Vector2.Zero, angle, 0, new Vector2(Range(45, 90), 5), Spark, Fade, _ => 1,
                Move: t => velocity * 8 * t + new Vector2(0, 60 * t * t)));
        }

        // The embers: small glowing bits drifting out of the fire as it dies.
        for (int i = 0; i < (int)(35 * scale); i++)
        {
            float angle = Range(0, MathF.Tau), size = Range(4, 8);
            particles.Add(new Particle(EffectTextures.Glow, start + Range(4, 14), Range(5, 9),
                at + Vector2.FromAngle(angle) * Range(20, 130) * scale, Vector2.FromAngle(angle) * Range(1, 4) + new Vector2(0, -0.8f),
                0, 0, new Vector2(size, size) * 3, Ember, Fade, _ => 1));
        }
    }

    /// <summary>A billow's colour by age (past 1 it stays soot): white, yellow, orange, soot.</summary>
    private static Color FireColour(float t) =>
        t < 0.2f ? White.Lerp(Yellow, t / 0.2f)
        : t < 0.5f ? Yellow.Lerp(Orange, (t - 0.2f) / 0.3f)
        : Orange.Lerp(Soot, Math.Min(1, (t - 0.5f) / 0.5f));

    /// <summary>A billow: in at once, full to the middle of its life, then out.</summary>
    private static float Billow(float t) => t < 0.08f ? t / 0.08f * 0.75f : t < 0.4f ? 0.75f : 0.75f * MathF.Pow(1 - (t - 0.4f) / 0.6f, 1.5f);

    /// <summary>Smoke: in softly, then thinning slowly.</summary>
    private static float Settle(float t) => t < 0.15f ? t / 0.15f : MathF.Pow(1 - (t - 0.15f) / 0.85f, 1.3f);

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
