using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Kaoru's follow-up effect, ours (the owner took its v1 as it is, 2026-10-21; the ninja's one weak shot at the enemy commander,
/// docs/battle/02-resolution.md §7.3, as her bare fists, CG 1182; the punch's burst and speed lines after Sengoku Rance's
/// Motonari Punch, 46620, small). A small glint at her raised palm (frames 0-2), then three blows at the front of the
/// enemy's army, two jabs and a harder last one (1.5, 3.5, 6): each a bundle of speed lines driving in from the left, a
/// white-lavender burst of spikes, a pale shockwave arc pushed right and a few sparks; a little dust and a few motes
/// after (6-14). In her hakama's lavender. The same seed makes the same effect.
/// </summary>
internal static class KaoruEffect
{
    /// <summary>Her raised palm (CG 1182: the palm at x ~205, y ~128 of the scene).</summary>
    private static readonly Vector2 Palm = new(258, 152);

    /// <summary>Where the blows land: the front of the enemy's army.</summary>
    private static readonly Vector2 Target = new(585, 170);

    // The colours (light added over the scene): white and her hakama's lavender.
    private static readonly Color White = new(1f, 1f, 1f), Lavender = new(0.7f, 0.62f, 1f), Violet = new(0.5f, 0.4f, 0.9f);
    private static readonly Color Dust = new(0.45f, 0.42f, 0.36f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The glint at her palm (frames 0-2).
        particles.Add(new Particle(EffectTextures.Sparkle, -0.3f, 2.5f, Palm, Vector2.Zero, 0.4f, 0, new Vector2(36, 36), White, Fade, _ => 1));
        particles.Add(new Particle(EffectTextures.Glow, -0.3f, 2.5f, Palm, Vector2.Zero, 0, 0, new Vector2(36, 36), Lavender, t => 0.4f * Fade(t), _ => 1));

        // The blows: two jabs and a harder last one, each a little apart.
        foreach (var (at, offset, scale) in new[] { (1.5f, new Vector2(-10, -14), 0.75f), (3.5f, new Vector2(8, 12), 0.8f), (6f, Vector2.Zero, 1.1f) })
            Blow(at, Target + offset, scale);

        // A blow: speed lines driving in from the left to the point, a burst of spikes, a shockwave arc pushed right, sparks.
        void Blow(float at, Vector2 where, float scale)
        {
            for (int i = 0; i < 7; i++)
            {
                float y = Range(-22, 22) * scale, length = Range(70, 120) * scale;
                var end = where + new Vector2(-20, y);
                particles.Add(new Particle(EffectTextures.Streak, at - 1, 1.3f, end - new Vector2(length * 0.5f + 40, 0), new Vector2(40, 0), 0, 0,
                    new Vector2(length, Range(2, 4)), i % 3 == 0 ? Lavender : White, t => 0.8f * (t < 0.3f ? t / 0.3f : 1 - (t - 0.3f) / 0.7f), _ => 1));
            }
            particles.Add(new Particle(EffectTextures.Glow, at, 2.5f, where, Vector2.Zero, 0, 0, new Vector2(90, 90) * scale, White, t => 0.6f * Fade(t), _ => 1));
            particles.Add(new Particle(EffectTextures.Burst, at, 3, where, Vector2.Zero, Range(0, MathF.Tau), 0, new Vector2(88, 88) * scale, Lavender,
                Fade, t => 0.7f + 0.5f * t));
            particles.Add(new Particle(EffectTextures.Sparkle, at, 2.5f, where, Vector2.Zero, 0, 0, new Vector2(50, 50) * scale, White, Fade, _ => 1));
            particles.Add(new Particle(EffectTextures.Shock, at + 0.3f, 3, where + new Vector2(10, 0), new Vector2(9, 0), 0, 0, new Vector2(34, 80) * scale,
                Violet, t => 0.8f * Fade(t), t => 0.6f + 0.6f * t));
            for (int i = 0; i < 4; i++)
            {
                float angle = Range(-1.4f, 1.4f);
                particles.Add(new Particle(EffectTextures.Dash, at + Range(0, 0.4f), Range(2.5f, 4), where, Vector2.FromAngle(angle) * Range(8, 13), angle, 0,
                    new Vector2(Range(18, 26), 3), i % 2 == 0 ? White : Lavender, Fade, _ => 1));
            }
        }

        // The dust: a few small puffs kicked up at the target after the last blow (frames 6-14).
        for (int i = 0; i < 5; i++)
            particles.Add(new Particle(EffectTextures.Puff, 6 + Range(0, 1), Range(6, 8), Target + new Vector2(Range(-10, 40), Range(-10, 35)),
                new Vector2(Range(1, 3), Range(-1, 0.5f)), Range(0, MathF.Tau), 0, new Vector2(1, 1) * Range(40, 60), Dust,
                t => 0.5f * (t < 0.15f ? t / 0.15f : 1 - (t - 0.15f) / 0.85f), t => 0.8f + 0.5f * t));

        // The motes: a few lavender specks drifting up and away (frames 7-14).
        for (int i = 0; i < 7; i++)
        {
            float twinkle = Range(0, MathF.Tau);
            float Alpha(float t) => (0.5f + 0.2f * MathF.Sin(twinkle + t * 12)) * Fade(t);
            particles.Add(new Particle(EffectTextures.Sparkle, Range(6.5f, 9), Range(4, 6), Target + new Vector2(Range(-30, 50), Range(-50, 20)),
                new Vector2(Range(0.5f, 2), Range(-1.5f, -0.5f)), 0, 0, new Vector2(1, 1) * Range(14, 22), i % 2 == 0 ? White : Lavender, Alpha, _ => 1));
        }
        return particles;
    }

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
