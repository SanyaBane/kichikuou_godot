using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Freya's follow-up effect, ours (owner, 2026-10-21: the ninja's one weak shot at the enemy commander,
/// docs/battle/02-resolution.md §7.3, as a lash of the whip in her hands, CG 1176). A small flick at her hand, the lash
/// snaking out from it to the front of the enemy's army, its line fading behind its tip (frames 0-4), and at the tip
/// the crack: a small white flash, a pale blue spark, two snapping arcs and a few sparks (3-9), then a few faint motes
/// (5-13). The same seed makes the same effect.
/// </summary>
internal static class FreyaEffect
{
    /// <summary>Her whip hand (CG 1176: the hand at x ~180, y ~147 of the scene).</summary>
    private static readonly Vector2 Hand = new(228, 176);

    /// <summary>Where the lash cracks: the front of the enemy's army.</summary>
    private static readonly Vector2 Target = new(585, 168);

    /// <summary>The frames the tip takes from her hand to the crack.</summary>
    private const float Reach = 3.4f;

    /// <summary>The lash's segments.</summary>
    private const int Segments = 56;

    // The colours (light added over the scene): the whip's silver, her cloak's blue.
    private static readonly Color Silver = new(0.88f, 0.9f, 1f), White = new(1f, 1f, 1f), Blue = new(0.5f, 0.68f, 1f);

    /// <summary>
    /// The lash's line at <paramref name="s"/> (0 her hand, 1 the crack): rising in an arc over the field with a ripple
    /// running along it.
    /// </summary>
    private static Vector2 Line(float s)
    {
        var straight = Hand.Lerp(Target, s);
        return straight + new Vector2(0, -70 * MathF.Sin(MathF.PI * s) + 16 * MathF.Sin(3 * MathF.PI * s));
    }

    /// <summary>When the tip passes <paramref name="s"/>: fast out, slowing a little before the crack.</summary>
    private static float TipAt(float s) => Reach * (1 - MathF.Sqrt(1 - s));

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The flick: a small glint at her hand (frames 0-2).
        particles.Add(new Particle(EffectTextures.Sparkle, -0.3f, 2.5f, Hand, Vector2.Zero, 0.3f, 0, new Vector2(34, 34), White, Fade, _ => 1));

        // The lash: short bars along the line, each lit as the tip passes and fading behind it (frames 0-4).
        for (int i = 0; i < Segments; i++)
        {
            float s0 = (float)i / Segments, s1 = (float)(i + 1) / Segments;
            Vector2 a = Line(s0), b = Line(s1);
            float thickness = 5.5f - 3 * s0;
            particles.Add(new Particle(EffectTextures.Bar, TipAt(s0), 1.4f + 0.8f * s0, (a + b) / 2, Vector2.Zero, (b - a).Angle(), 0,
                new Vector2(a.DistanceTo(b) * 1.8f, thickness), Silver, t => 1 - t, _ => 1));
        }
        // The tip: a bright dash running ahead of the line.
        for (int i = 0; i < 12; i++)
        {
            float s = i / 12f;
            Vector2 a = Line(s), b = Line(s + 1 / 12f);
            particles.Add(new Particle(EffectTextures.Dash, TipAt(s), TipAt(s + 1 / 12f) - TipAt(s) + 0.2f, a, (b - a) / (TipAt(s + 1 / 12f) - TipAt(s)),
                (b - a).Angle(), 0, new Vector2(26, 5), White, _ => 0.9f, _ => 1));
        }

        // The crack: a small white flash, a pale blue spark, two arcs snapping out from the tip and a few sparks (frames 3-9).
        float crack = Reach;
        particles.Add(new Particle(EffectTextures.Glow, crack, 3, Target, Vector2.Zero, 0, 0, new Vector2(100, 100), White, t => 0.7f * Fade(t), _ => 1));
        particles.Add(new Particle(EffectTextures.Sparkle, crack, 4, Target, Vector2.Zero, 0.2f, 0, new Vector2(96, 96), Blue, Fade, t => 0.8f + 0.4f * t));
        particles.Add(new Particle(EffectTextures.Sparkle, crack, 3, Target, Vector2.Zero, 0.2f, 0, new Vector2(52, 52), White, Fade, t => 0.8f + 0.4f * t));
        foreach (float tilt in new[] { -0.5f, 0.6f })
            particles.Add(new Particle(EffectTextures.Hook, crack + 0.2f, 2.5f, Target + new Vector2(14, 0), new Vector2(5, 0), tilt, 0, new Vector2(46, 84),
                White, Fade, t => 0.7f + 0.6f * t));
        for (int i = 0; i < 6; i++)
        {
            float angle = Range(-1.6f, 1.6f);
            particles.Add(new Particle(EffectTextures.Dash, crack + Range(0, 0.5f), Range(3, 5), Target, Vector2.FromAngle(angle) * Range(7, 13), angle, 0,
                new Vector2(Range(22, 34), 4), i % 2 == 0 ? White : Blue, Fade, _ => 1));
        }

        // The motes: a few faint blue-white specks drifting about the crack (frames 5-13).
        for (int i = 0; i < 7; i++)
        {
            float twinkle = Range(0, MathF.Tau);
            float Alpha(float t) => (0.5f + 0.2f * MathF.Sin(twinkle + t * 12)) * Fade(t);
            particles.Add(new Particle(EffectTextures.Sparkle, crack + Range(1.5f, 4), Range(5, 7), Target + new Vector2(Range(-40, 50), Range(-50, 30)),
                new Vector2(Range(-0.4f, 0.6f), Range(0.5f, 1.2f)), 0, 0, new Vector2(1, 1) * Range(14, 22), i % 2 == 0 ? White : Blue, Alpha, _ => 1));
        }
        return particles;
    }

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
