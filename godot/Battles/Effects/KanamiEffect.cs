using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Kanami's follow-up effect, ours (owner, 2026-10-21: the ninja's one weak shot at the enemy commander,
/// docs/battle/02-resolution.md §7.3, should look far weaker than Sengoku Rance's Ninja troop, 40100; laid out after
/// Sandal Shuriken, 46780, a single throw). A small white glint where she throws, right of her figure (CG 1164), three
/// shuriken in a tight fan with faint trails flying to the target (frames 0-5), at each hit a small white flash, a
/// warm four-point spark with a white core and a few sparks thrown out (4-9), then a few small gold glints drifting down (5-16). The same seed makes the
/// same effect.
/// </summary>
internal static class KanamiEffect
{
    /// <summary>Where she throws: right of her figure, at her shoulder (CG 1164: x 138-256, y 119-210 of the scene).</summary>
    private static readonly Vector2 Launch = new(300, 158);

    /// <summary>Where the shuriken hit: the front of the enemy's army.</summary>
    private static readonly Vector2 Target = new(585, 172);

    /// <summary>The shuriken's speed, design units a frame.</summary>
    private const float Speed = 68;

    // The colours (light added over the scene): steel turning cream, the hits in her scarf's warm gold.
    private static readonly Color White = new(0.95f, 0.95f, 0.92f), Cream = new(0.88f, 0.84f, 0.62f);
    private static readonly Color Gold = new(1f, 0.78f, 0.3f), Ember = new(1f, 0.55f, 0.2f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The throw: a small white glint right of her figure (frames 0-3).
        particles.Add(new Particle(EffectTextures.Sparkle, -0.3f, 3, Launch, Vector2.Zero, Range(-0.2f, 0.2f), 0, new Vector2(46, 46), White,
            Fade, t => 0.8f + 0.4f * t));
        particles.Add(new Particle(EffectTextures.Glow, -0.3f, 3, Launch, Vector2.Zero, 0, 0, new Vector2(40, 40), White, t => 0.3f * Fade(t), _ => 1));

        // The shuriken: three in a tight fan, spinning, white turning cream, each with a faint trail; gone at the hit.
        foreach (var (delay, spread) in new[] { (0f, -16f), (0.6f, 2f), (1.2f, 18f) })
        {
            var from = Launch + new Vector2(0, spread * 0.25f);
            var to = Target + new Vector2(Range(-12, 12), spread);
            float life = from.DistanceTo(to) / Speed, size = Range(24, 28);
            var velocity = (to - from) / life;
            float angle = velocity.Angle();
            particles.Add(new Particle(EffectTextures.Shuriken, delay, life, from, velocity, Range(0, MathF.Tau), 0.9f, new Vector2(size, size), White,
                t => t < 0.1f ? t / 0.1f : 1, _ => 1, Ramp: t => White.Lerp(Cream, t)));
            particles.Add(new Particle(EffectTextures.Streak, delay, life, from - velocity.Normalized() * 30, velocity, angle, 0, new Vector2(60, 3), Cream,
                t => 0.35f * (t < 0.15f ? t / 0.15f : 1), _ => 1));
            Hit(delay + life, to);
        }

        // At a hit: a small flash, a warm four-point spark swelling and a few sparks thrown out (frames ~4-9).
        void Hit(float at, Vector2 where)
        {
            particles.Add(new Particle(EffectTextures.Glow, at, 3, where, Vector2.Zero, 0, 0, new Vector2(90, 90), White, t => 0.6f * Fade(t), _ => 1));
            float turn = Range(-0.3f, 0.3f);
            particles.Add(new Particle(EffectTextures.Sparkle, at, 4, where, Vector2.Zero, turn, 0, new Vector2(84, 84), Gold, Fade, t => 0.8f + 0.4f * t));
            particles.Add(new Particle(EffectTextures.Sparkle, at, 3, where, Vector2.Zero, turn, 0, new Vector2(48, 48), White, Fade, t => 0.8f + 0.4f * t));
            for (int i = 0; i < 5; i++)
            {
                float angle = Range(-2.2f, 2.2f);
                var velocity = Vector2.FromAngle(angle) * Range(6, 12);
                particles.Add(new Particle(EffectTextures.Dash, at + Range(0, 0.4f), Range(3, 5), where, velocity, angle, 0, new Vector2(Range(20, 32), 4),
                    i % 2 == 0 ? Gold : Ember, Fade, _ => 1));
            }
        }

        // The glints: a few small gold sparkles about the target, twinkling as they drift down (frames 5-16).
        for (int i = 0; i < 8; i++)
        {
            float birth = Range(5, 9), twinkle = Range(0, MathF.Tau);
            var at = Target + new Vector2(Range(-50, 50), Range(-60, 30));
            float Alpha(float t) => (0.55f + 0.25f * MathF.Sin(twinkle + t * 12)) * Fade(t);
            particles.Add(new Particle(EffectTextures.Sparkle, birth, Range(5, 8), at, new Vector2(Range(-0.5f, 0.5f), Range(0.8f, 1.6f)), 0, 0,
                new Vector2(1, 1) * Range(16, 26), Gold, Alpha, _ => 1));
        }
        return particles;
    }

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
