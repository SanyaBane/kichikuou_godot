using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The phantoms' volley, Mill's (unit 25) Phantom Beasts' Indirect attack (owner, 2026-11-04; ours, after the owner's
/// mock-ups B and C, <c>tools/ui/mock_mill_shots.py</c>): instead of arrows she sends many little phantoms of her troop's
/// manner — round, pastel, translucent, a little larger than her head — flying level across the field from her hand at
/// the enemy, a little spirit (B: two dot eyes, a wisp tail streaming behind it) and a jelly drop (C: squashed by its
/// speed, eyes and a smile, a trail of sparkles, bobbing on a low wave) in turn, green, blue, pink and orange in turn.
/// They leave over frames 0-13, each crossing in ~7 frames on its own lane (the lanes widening toward the enemy), and burst
/// on the target's half into a pastel puff and sparkles; the last puffs are gone by frame ~28. The eyes and the smile are
/// cut out of the body (<see cref="EffectTextures.PhantomSpirit"/>): the effect only adds light, so they are the scene
/// showing through. The same seed makes the same effect.
/// </summary>
internal static class PhantomEffect
{
    /// <summary>Mill's outstretched hand where her sprites hold it (CG 1179-1181, design space), whence the phantoms leave.</summary>
    private static readonly Vector2 Hand = new(150, 182);

    /// <summary>How far they fly: to the enemy's ranks on the target's half.</summary>
    private const float ReachFrom = 560, ReachTo = 720;

    /// <summary>The troop's pastels.</summary>
    private static readonly Color[] Pastels =
    [
        new(0.72f, 0.9f, 0.47f), new(0.75f, 0.85f, 0.96f), new(0.96f, 0.67f, 0.8f), new(0.97f, 0.75f, 0.43f),
    ];

    private static readonly Color Shine = new(1f, 1f, 1f, 0.85f), Twinkle = new(1f, 1f, 0.94f, 0.85f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        const int count = 20;
        for (int k = 0; k < count; k++)
        {
            bool drop = k % 2 == 1;                                 // B and C in turn (owner)
            var colour = Pastels[k % Pastels.Length];
            float birth = 13f * k / (count - 1) + Range(-0.3f, 0.3f);
            float flight = Range(6, 8);
            float size = Range(40, 52);                             // her head ~27 in the design space: a little more, as drawn
            var start = Hand + new Vector2(Range(10, 25), Range(-8, 8));
            var end = new Vector2(Range(ReachFrom, ReachTo), Hand.Y + Range(-55, 45));
            var path = end - start;
            float wave = drop ? Range(8, 14) : 0, phase = Range(0, MathF.Tau);
            Vector2 Along(float t) => path * t + new Vector2(0, wave * MathF.Sin(t * 9 + phase));
            // Leaving the hand it swells from small; at the end it is gone into its burst.
            float Grow(float t) => t < 0.15f ? 0.4f + 4 * t : 1;
            float Alpha(float t) => t < 0.1f ? t / 0.1f : t > 0.92f ? (1 - t) / 0.08f : 1;
            var body = drop ? new Vector2(size * 1.25f, size * 0.8f) : new Vector2(size * 1.08f, size * 0.94f);

            if (!drop)
            {
                // The wisp tail, behind the body, a little longer than it.
                particles.Add(new Particle(EffectTextures.Wisp, birth, flight, start - new Vector2(size * 0.85f, 0), Vector2.Zero, 0, 0,
                    new Vector2(size * 2.2f, size * 0.95f), colour * new Color(1, 1, 1, 0.85f), Alpha, Grow, Move: Along));
            }
            // The halo, the body and the shine on it.
            particles.Add(new Particle(EffectTextures.Glow, birth, flight, start, Vector2.Zero, 0, 0, body * 1.9f,
                colour * new Color(1, 1, 1, 0.35f), Alpha, Grow, Move: Along));
            particles.Add(new Particle(drop ? EffectTextures.PhantomDrop : EffectTextures.PhantomSpirit, birth, flight, start,
                Vector2.Zero, 0, 0, body, colour, Alpha, Grow, Move: Along));
            particles.Add(new Particle(EffectTextures.PhantomShine, birth, flight, start, Vector2.Zero, 0, 0, body, Shine, Alpha, Grow,
                Move: Along));

            if (drop)
            {
                // The sparkles it leaves behind, each standing where the drop was and fading.
                for (float t = 0.15f; t < 0.95f; t += 0.12f)
                {
                    var at = start + Along(t) - new Vector2(size * 0.6f, 0) + new Vector2(Range(-4, 4), Range(-6, 6));
                    float sparkle = Range(10, 18);
                    particles.Add(new Particle(EffectTextures.Sparkle, birth + t * flight, Range(1.5f, 3), at, Vector2.Zero,
                        Range(-0.3f, 0.3f), 0, new Vector2(sparkle, sparkle), Twinkle, Fade, t2 => 1 - 0.5f * t2));
                }
            }

            // The burst on the enemy: a pastel puff swelling and a few sparkles thrown out.
            float hit = birth + flight * 0.95f;
            particles.Add(new Particle(EffectTextures.Glow, hit, Range(4, 6), end, Vector2.Zero, 0, 0, new Vector2(size, size) * 2.4f,
                colour * new Color(1, 1, 1, 0.75f), Fade, t => 0.6f + 0.9f * t));
            particles.Add(new Particle(EffectTextures.Puff, hit, Range(5, 8), end, new Vector2(Range(-0.5f, 0.5f), -0.6f),
                Range(0, MathF.Tau), 0, new Vector2(size, size) * 1.8f, colour * new Color(1, 1, 1, 0.6f), Fade, t => 0.7f + 0.8f * t));
            for (int n = 0; n < 4; n++)
            {
                float angle = Range(0, MathF.Tau), sparkle = Range(12, 22);
                particles.Add(new Particle(EffectTextures.Sparkle, hit + Range(0, 1), Range(2, 4), end,
                    Vector2.FromAngle(angle) * Range(3, 7), Range(-0.3f, 0.3f), 0, new Vector2(sparkle, sparkle), Twinkle, Fade, _ => 1));
            }
        }
        return particles;
    }

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
