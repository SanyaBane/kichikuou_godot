using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Wichita's follow-up effect, ours (the owner took its v2, 2026-10-21: flipped and in her sprite's colours; the ninja's one weak shot at the enemy commander,
/// docs/battle/02-resolution.md §7.3, as a cut of her flaming sword, CG 1185; the slash and its fire after Sengoku Rance's
/// Fiend Slash, 46800, small). A flare at her blade's tip with a few embers (frames 0-2), then one diagonal cut at the
/// front of the enemy's army, from its upper left down to the right (1-3): a white-hot line in amber drawn along the cut in a
/// coral band with a crimson halo, a small flash where it ends; flames licking up along the cut and a few small fire billows (2-10), embers
/// rising and drifting (2-14). In her sword's fire, its colours taken from the sprite: a white core, peach and amber,
/// coral and a crimson halo. The same seed makes the same effect.
/// </summary>
internal static class WichitaEffect
{
    /// <summary>Her blade's tip (CG 1185: the sword from x ~195, y ~155 to ~270, ~95 of the scene).</summary>
    private static readonly Vector2 Tip = new(334, 116);

    /// <summary>
    /// The cut: where it starts (upper left) and ends (lower right), across the front of the enemy's army — first laid
    /// out from the upper right, after the reference, and flipped (owner, 2026-10-21).
    /// </summary>
    private static readonly Vector2 From = new(545, 95), To = new(640, 240);

    /// <summary>When the cut starts and the frames it takes.</summary>
    private const float CutAt = 1, CutTakes = 1.6f;

    /// <summary>The cut's segments.</summary>
    private const int Segments = 24;

    // The colours (light added over the scene): her sword's fire as its sprite draws it (CG 1185; owner, 2026-10-21) — a
    // white core, peach and amber flames, coral, and a crimson halo of specks.
    private static readonly Color White = new(1f, 1f, 1f), Peach = new(1f, 0.8f, 0.49f), Amber = new(0.98f, 0.63f, 0.24f);
    private static readonly Color Coral = new(0.98f, 0.43f, 0.27f), Crimson = new(0.71f, 0.22f, 0.2f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();
        float angle = (To - From).Angle();
        var along = (To - From).Normalized();

        // The flare at her blade's tip and a few embers off it (frames 0-2).
        particles.Add(new Particle(EffectTextures.Sparkle, -0.3f, 2.5f, Tip, Vector2.Zero, 0.3f, 0, new Vector2(44, 44), Peach, Fade, _ => 1));
        particles.Add(new Particle(EffectTextures.Glow, -0.3f, 2.5f, Tip, Vector2.Zero, 0, 0, new Vector2(50, 50), Coral, t => 0.5f * Fade(t), _ => 1));
        for (int i = 0; i < 5; i++)
            particles.Add(new Particle(EffectTextures.Dot, Range(-0.3f, 0.5f), Range(2.5f, 4), Tip + new Vector2(Range(-12, 12), Range(-8, 8)),
                new Vector2(Range(0.5f, 3), Range(-3, -1)), 0, 0, new Vector2(5, 5), Coral, Fade, _ => 1));

        // The cut: segments lit in turn from the upper left down to the right, a white-hot line in amber, in a coral band with a crimson halo (1-5).
        for (int i = 0; i < Segments; i++)
        {
            float s = (float)i / Segments, birth = CutAt + CutTakes * s;
            var at = From.Lerp(To, s + 0.5f / Segments);
            float length = From.DistanceTo(To) / Segments * 1.8f;
            particles.Add(new Particle(EffectTextures.Bar, birth, 3.5f, at, Vector2.Zero, angle, 0, new Vector2(length, 3), White,
                t => 1 - t, _ => 1));
            particles.Add(new Particle(EffectTextures.Bar, birth, 4, at, Vector2.Zero, angle, 0, new Vector2(length, 9), Amber,
                t => 0.9f * (1 - t), _ => 1));
            particles.Add(new Particle(EffectTextures.Bar, birth, 4.5f, at, Vector2.Zero, angle, 0, new Vector2(length, 22), Coral,
                t => 0.6f * (1 - t), _ => 1));
            particles.Add(new Particle(EffectTextures.Bar, birth, 5, at, Vector2.Zero, angle, 0, new Vector2(length, 40), Crimson,
                t => 0.4f * (1 - t), _ => 1));
        }
        // The cut's head: a small bright flare running down the line.
        particles.Add(new Particle(EffectTextures.Sparkle, CutAt, CutTakes, From, (To - From) / CutTakes, 0.4f, 0, new Vector2(40, 40), White, _ => 1, _ => 1));
        // The flash where it ends.
        particles.Add(new Particle(EffectTextures.Glow, CutAt + CutTakes, 3, To, Vector2.Zero, 0, 0, new Vector2(70, 70), Peach, t => 0.6f * Fade(t), _ => 1));
        particles.Add(new Particle(EffectTextures.Burst, CutAt + CutTakes, 2.5f, To, Vector2.Zero, Range(0, MathF.Tau), 0, new Vector2(70, 70), Peach,
            Fade, t => 0.7f + 0.5f * t));

        // The flames: tongues licking up along the cut as it is drawn, and a few small fire billows (2-10).
        for (int i = 0; i < 12; i++)
        {
            float s = Range(0.05f, 0.95f), birth = CutAt + CutTakes * s + Range(0.3f, 1.5f), height = Range(30, 55);
            var at = From.Lerp(To, s) + new Vector2(Range(-8, 8), -height * 0.35f);
            particles.Add(new Particle(EffectTextures.Tongue, birth, Range(3, 5), at, new Vector2(Range(-0.5f, 0.5f), Range(-3, -1.5f)), Range(-0.2f, 0.2f), 0,
                new Vector2(height * 0.6f, height), i % 3 == 0 ? Amber : Coral, t => 0.8f * Fade(t), t => 0.8f + 0.4f * t));
        }
        for (int i = 0; i < 5; i++)
        {
            float s = Range(0.15f, 0.85f);
            particles.Add(new Particle(EffectTextures.Billow, CutAt + CutTakes * s + Range(0.5f, 2), Range(4, 6), From.Lerp(To, s) + along.Orthogonal() * Range(-12, 12),
                new Vector2(Range(-0.5f, 0.5f), Range(-2, -0.8f)), 0, 0, new Vector2(1, 1) * Range(34, 50), i % 2 == 0 ? Coral : Crimson,
                t => 0.45f * (t < 0.15f ? t / 0.15f : 1 - (t - 0.15f) / 0.85f), t => 0.8f + 0.5f * t));
        }

        // The embers: small sparks rising off the cut and drifting (2-14).
        for (int i = 0; i < 14; i++)
        {
            float s = Range(0, 1), twinkle = Range(0, MathF.Tau);
            float Alpha(float t) => (0.7f + 0.3f * MathF.Sin(twinkle + t * 10)) * Fade(t);
            particles.Add(new Particle(EffectTextures.Sparkle, CutAt + CutTakes * s + Range(0.5f, 3), Range(4, 8), From.Lerp(To, s) + new Vector2(Range(-10, 10), 0),
                new Vector2(Range(-1, 1.5f), Range(-3.5f, -1.5f)), 0, 0, new Vector2(1, 1) * Range(12, 20), i % 3 == 0 ? Amber : i % 3 == 1 ? Coral : Crimson, Alpha, _ => 1));
        }
        return particles;
    }

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
