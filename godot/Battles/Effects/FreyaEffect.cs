using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Freya's follow-up effect, ours (owner, 2026-10-21: the ninja's one weak shot at the enemy commander,
/// docs/battle/02-resolution.md §7.3, as a lash of the whip in her hands, CG 1176, forked at its end). The whip curls once
/// over her hand (frames 0-1.2), then lashes out in a long arc to the front of the enemy's army, splitting into its two
/// tails over the last quarter, its line fading behind the tips (1.2-3.6); the two tails crack at once: two parallel
/// welts cut across the target, white in a pink glow, a small ring and spark at each tip (3.6-9), then a few sparks
/// and pink motes (4-12). The same seed makes the same effect.
/// </summary>
internal static class FreyaEffect
{
    /// <summary>Her whip hand (CG 1176: the hand at x ~180, y ~147 of the scene).</summary>
    private static readonly Vector2 Hand = new(228, 176);

    /// <summary>Where the tails crack: the front of the enemy's army.</summary>
    private static readonly Vector2 Target = new(585, 168);

    /// <summary>The curl over her hand: its centre and radius.</summary>
    private static readonly Vector2 CurlCentre = new(240, 132);
    private const float CurlRadius = 34;

    /// <summary>When the curl ends and the lash cracks.</summary>
    private const float CurlEnds = 1.2f, Crack = 3.6f;

    /// <summary>Where along the lash the tails part.</summary>
    private const float Fork = 0.75f;

    /// <summary>The welts' direction: from upper left down to the right.</summary>
    private const float WeltAngle = 1.05f;

    // The colours (light added over the scene): the whip's silver, the crack's white and her hair's pink.
    private static readonly Color Silver = new(0.88f, 0.9f, 1f), White = new(1f, 1f, 1f), Pink = new(1f, 0.45f, 0.7f);
    private static readonly Color Rose = new(1f, 0.7f, 0.85f);

    /// <summary>The curl at <paramref name="u"/> (0-1): once round from her hand, up and over, back near it.</summary>
    private static Vector2 Curl(float u)
    {
        float start = (Hand - CurlCentre).Angle(), angle = start - MathF.Tau * u;
        return CurlCentre + Vector2.FromAngle(angle) * CurlRadius * (0.85f + 0.15f * u);
    }

    /// <summary>
    /// The lash at <paramref name="s"/> (0 the curl's end, 1 the crack), on the tail <paramref name="side"/> (−1 or +1):
    /// a high arc with a ripple, the tails parting past <see cref="Fork"/>.
    /// </summary>
    private static Vector2 Lash(float s, int side)
    {
        var from = Curl(1);
        var point = from.Lerp(Target, s) + new Vector2(0, -85 * MathF.Sin(MathF.PI * s) + 12 * MathF.Sin(3 * MathF.PI * s));
        if (s <= Fork)
            return point;
        float apart = MathF.Pow((s - Fork) / (1 - Fork), 1.3f) * 20;
        return point + Vector2.FromAngle(WeltAngle + MathF.PI / 2) * apart * side;
    }

    /// <summary>When the tips pass <paramref name="s"/> of the lash: fast out, slowing a little before the crack.</summary>
    private static float LashAt(float s) => CurlEnds + (Crack - CurlEnds) * (1 - MathF.Sqrt(1 - s));

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        void Segment(Vector2 a, Vector2 b, float birth, float life, float thickness, Color colour) =>
            particles.Add(new Particle(EffectTextures.Bar, birth, life, (a + b) / 2, Vector2.Zero, (b - a).Angle(), 0,
                new Vector2(a.DistanceTo(b) * 1.8f, thickness), colour, t => 1 - t, _ => 1));

        // The curl: once round over her hand, its line lit as the tip passes (frames 0-1.2).
        const int curlSegments = 20;
        for (int i = 0; i < curlSegments; i++)
        {
            float u = (float)i / curlSegments;
            Segment(Curl(u), Curl(u + 1f / curlSegments), CurlEnds * u, 1.2f, 4.5f, Silver);
        }

        // The lash: out in a high arc, one line to the fork, two tails after it, thinning (1.2-3.6).
        const int lashSegments = 48;
        for (int i = 0; i < lashSegments; i++)
        {
            float s0 = (float)i / lashSegments, s1 = (float)(i + 1) / lashSegments, thickness = 5 - 2.5f * s0;
            foreach (int side in s0 < Fork ? new[] { 0 } : new[] { -1, 1 })
                Segment(Lash(s0, side), Lash(s1, side), LashAt(s0), 1.3f + 0.6f * s0, s0 < Fork ? thickness : thickness * 0.75f, Silver);
        }
        // The tips: a bright dash running ahead of each tail.
        for (int i = 0; i < 12; i++)
        {
            float s0 = i / 12f, s1 = (i + 1) / 12f, life = LashAt(s1) - LashAt(s0);
            foreach (int side in s0 < Fork ? new[] { 0 } : new[] { -1, 1 })
            {
                Vector2 a = Lash(s0, side), b = Lash(s1, side);
                particles.Add(new Particle(EffectTextures.Dash, LashAt(s0), life + 0.15f, a, (b - a) / life, (b - a).Angle(), 0,
                    new Vector2(24, 5), White, _ => 0.9f, _ => 1));
            }
        }

        // The crack: at each tail's tip two parallel welts drawn across the target in a breath, white in a pink glow, and
        // a small ring and spark (frames 3.6-9).
        var weltAlong = Vector2.FromAngle(WeltAngle);
        foreach (int side in new[] { -1, 1 })
        {
            var tip = Lash(1, side);
            const int weltSegments = 10;
            const float weltLength = 120;
            for (int i = 0; i < weltSegments; i++)
            {
                float f0 = (float)i / weltSegments - 0.5f, f1 = (float)(i + 1) / weltSegments - 0.5f, birth = Crack + 0.5f * (i / (float)weltSegments);
                Vector2 a = tip + weltAlong * weltLength * f0, b = tip + weltAlong * weltLength * f1;
                Segment(a, b, birth, 4.5f, 5, White);
                particles.Add(new Particle(EffectTextures.Bar, birth, 5.5f, (a + b) / 2, Vector2.Zero, WeltAngle, 0,
                    new Vector2(weltLength / weltSegments * 1.8f, 20), Pink, t => 0.55f * (1 - t), _ => 1));
            }
            particles.Add(new Particle(EffectTextures.Ring, Crack, 3, tip, Vector2.Zero, 0, 0, new Vector2(40, 40), White, Fade, t => 0.6f + 1.2f * t));
            particles.Add(new Particle(EffectTextures.Sparkle, Crack, 3, tip, Vector2.Zero, 0.3f, 0, new Vector2(60, 60), Rose, Fade, t => 0.8f + 0.4f * t));
        }
        particles.Add(new Particle(EffectTextures.Glow, Crack, 3, Target, Vector2.Zero, 0, 0, new Vector2(110, 110), White, t => 0.55f * Fade(t), _ => 1));

        // The sparks thrown off the crack (frames 3.6-8).
        for (int i = 0; i < 7; i++)
        {
            float angle = Range(-1.6f, 1.6f);
            particles.Add(new Particle(EffectTextures.Dash, Crack + Range(0, 0.5f), Range(3, 5), Target, Vector2.FromAngle(angle) * Range(7, 13), angle, 0,
                new Vector2(Range(22, 32), 4), i % 2 == 0 ? White : Rose, Fade, _ => 1));
        }

        // The motes: a few pink and white specks drifting about the welts (frames 5-12).
        for (int i = 0; i < 7; i++)
        {
            float twinkle = Range(0, MathF.Tau);
            float Alpha(float t) => (0.5f + 0.2f * MathF.Sin(twinkle + t * 12)) * Fade(t);
            particles.Add(new Particle(EffectTextures.Sparkle, Crack + Range(1.5f, 3.5f), Range(4, 6), Target + new Vector2(Range(-50, 50), Range(-55, 45)),
                new Vector2(Range(-0.4f, 0.6f), Range(0.5f, 1.2f)), 0, 0, new Vector2(1, 1) * Range(14, 22), i % 2 == 0 ? White : Rose, Alpha, _ => 1));
        }
        return particles;
    }

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
