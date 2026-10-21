using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Freya's follow-up effect, ours (the owner took its v4, 2026-10-21; the ninja's one weak shot at the enemy commander,
/// docs/battle/02-resolution.md §7.3, as a lash of the whip in her hands, CG 1176, forked at its end). A pale swish at
/// her hand as she swings (frames 0-1.5); then the whip's end comes down on the target out of the air above it: a
/// silver line sweeping down from above, from her side (turned 35° counter-clockwise), its tip lagging and catching up as it straightens, splitting into its
/// two tails, a faint blur behind it (1-3); the two tails crack at once: two parallel welts cut across the target,
/// white in a pink glow, a small ring and spark at each tip (3-8), then a few sparks and pink motes (3-11). The same seed
/// makes the same effect.
/// </summary>
internal static class FreyaEffect
{
    /// <summary>Her whip hand (CG 1176: the hand at x ~180, y ~147 of the scene).</summary>
    private static readonly Vector2 Hand = new(228, 176);

    /// <summary>Where the tails crack: the front of the enemy's army.</summary>
    private static readonly Vector2 Target = new(585, 168);

    /// <summary>
    /// The whole sweep turned 35° counter-clockwise from where it was first laid out, so the whip comes from her side
    /// rather than out of the sky (owner, 2026-10-21).
    /// </summary>
    private const float Turned = -35 * MathF.PI / 180;

    /// <summary>The whip's end shown, its length; the direction it lies in at the crack (a little down to the right).</summary>
    private const float Length = 200, Landing = 0.75f + Turned;

    /// <summary>The direction it sweeps down from (straight up) and how far its tip lags behind at the start.</summary>
    private const float Raised = -0.9f + Turned, Lag = 1f;

    /// <summary>When the sweep starts and the tails crack.</summary>
    private const float SweepAt = 1, Crack = 3;

    /// <summary>Where along the whip the tails part, and how far apart they land.</summary>
    private const float Fork = 0.8f, Apart = 20;

    /// <summary>The point the whip's end swings about: the crack lies <see cref="Length"/> from it along <see cref="Landing"/>.</summary>
    private static readonly Vector2 Pivot = Target - Vector2.FromAngle(Landing) * Length;

    // The colours (light added over the scene): the whip's silver, the crack's white and her hair's pink.
    private static readonly Color Silver = new(0.88f, 0.9f, 1f), White = new(1f, 1f, 1f), Pink = new(1f, 0.45f, 0.7f);
    private static readonly Color Rose = new(1f, 0.7f, 0.85f);

    /// <summary>
    /// The whip at <paramref name="s"/> of its length (0 the pivot, 1 the tip) on the tail <paramref name="side"/> (−1, 0,
    /// +1) when the sweep is <paramref name="k"/> done (0-1): it turns from <see cref="Raised"/> to <see cref="Landing"/>
    /// and bends back along its length by a lag that eases to nothing at the crack; past <see cref="Fork"/> the tails part.
    /// </summary>
    private static Vector2 Whip(float s, int side, float k)
    {
        float turn = Raised + (Landing - Raised) * (1 - (1 - k) * (1 - k)), lag = Lag * (1 - k) * (1 - k);
        var point = Pivot;
        const int steps = 24;
        for (int i = 0; i < steps; i++)
        {
            float u = (i + 0.5f) / steps * s;
            point += Vector2.FromAngle(turn - lag * u * u) * (Length * s / steps);
        }
        if (s > Fork && side != 0)
        {
            float direction = turn - lag * s * s;
            point += Vector2.FromAngle(direction + MathF.PI / 2) * side * Apart * MathF.Pow((s - Fork) / (1 - Fork), 1.3f);
        }
        return point;
    }

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        void Segment(Vector2 a, Vector2 b, float birth, float life, float thickness, Color colour, float strength) =>
            particles.Add(new Particle(EffectTextures.Bar, birth, life, (a + b) / 2, Vector2.Zero, (b - a).Angle(), 0,
                new Vector2(a.DistanceTo(b) * 1.8f, thickness), colour, t => strength * (1 - t), _ => 1));

        // The swing at her hand: a pale swish arc and a small glint (frames 0-1.5).
        particles.Add(new Particle(EffectTextures.Hook, -0.3f, 1.8f, Hand + new Vector2(18, -22), new Vector2(2, 0), -0.4f, 0.25f, new Vector2(40, 70),
            Silver, t => 0.7f * Fade(t), t => 0.8f + 0.3f * t));
        particles.Add(new Particle(EffectTextures.Sparkle, -0.3f, 2, Hand, Vector2.Zero, 0.3f, 0, new Vector2(30, 30), White, Fade, _ => 1));

        // The sweep: the whip's end drawn at short steps, each copy fading fast, so a frame shows it with a faint blur
        // behind; its root fades into the air, its tails part near the tip (frames 0.8-3).
        const int copies = 22, segments = 28;
        for (int c = 0; c <= copies; c++)
        {
            float k = (float)c / copies, birth = SweepAt + (Crack - SweepAt) * k;
            for (int i = 0; i < segments; i++)
            {
                float s0 = (float)i / segments, s1 = (float)(i + 1) / segments;
                float strength = Math.Min(1, s0 / 0.45f) * (0.6f + 0.4f * k), thickness = 6 - 2.5f * s0;
                foreach (int side in s0 < Fork ? new[] { 0 } : new[] { -1, 1 })
                    Segment(Whip(s0, side, k), Whip(s1, side, k), birth, 0.28f, s0 < Fork ? thickness : thickness * 0.8f, Silver, strength);
            }
            // The tips' blur: a bright dash at each tail's tip, lingering a little longer.
            foreach (int side in new[] { -1, 1 })
            {
                Vector2 a = Whip(0.97f, side, k), b = Whip(1, side, k);
                particles.Add(new Particle(EffectTextures.Dash, birth, 1, b, Vector2.Zero, (b - a).Angle(), 0, new Vector2(20, 4), White,
                    t => 0.5f * (0.4f + 0.6f * k) * (1 - t), _ => 1));
            }
        }

        // The crack: at each tail's tip a welt drawn across the target in a breath, white in a pink glow, and a small ring
        // and spark (frames 3-8).
        var along = Vector2.FromAngle(Landing);
        foreach (int side in new[] { -1, 1 })
        {
            var tip = Whip(1, side, 1);
            const int weltSegments = 10;
            const float weltLength = 120;
            for (int i = 0; i < weltSegments; i++)
            {
                float f0 = (float)i / weltSegments - 0.5f, f1 = (float)(i + 1) / weltSegments - 0.5f, birth = Crack + 0.5f * (i / (float)weltSegments);
                Vector2 a = tip + along * weltLength * f0, b = tip + along * weltLength * f1;
                Segment(a, b, birth, 4.5f, 5, White, 1);
                Segment(a, b, birth, 5.5f, 20, Pink, 0.55f);
            }
            particles.Add(new Particle(EffectTextures.Ring, Crack, 3, tip, Vector2.Zero, 0, 0, new Vector2(40, 40), White, Fade, t => 0.6f + 1.2f * t));
            particles.Add(new Particle(EffectTextures.Sparkle, Crack, 3, tip, Vector2.Zero, 0.3f, 0, new Vector2(60, 60), Rose, Fade, t => 0.8f + 0.4f * t));
        }
        particles.Add(new Particle(EffectTextures.Glow, Crack, 3, Target, Vector2.Zero, 0, 0, new Vector2(110, 110), White, t => 0.55f * Fade(t), _ => 1));

        // The sparks thrown off the crack (frames 3-7).
        for (int i = 0; i < 7; i++)
        {
            float angle = Range(-1.6f, 1.6f);
            particles.Add(new Particle(EffectTextures.Dash, Crack + Range(0, 0.5f), Range(3, 5), Target, Vector2.FromAngle(angle) * Range(7, 13), angle, 0,
                new Vector2(Range(22, 32), 4), i % 2 == 0 ? White : Rose, Fade, _ => 1));
        }

        // The motes: a few pink and white specks drifting about the welts (frames 4.5-11).
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
