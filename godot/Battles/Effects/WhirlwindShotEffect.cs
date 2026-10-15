using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The Whirlwind Shot effect, ours after Sengoku Rance's Piercing Tempest, effect 40660 (docs/sengoku/04-screen.md
/// §2.4.1; not traced): Isoroku's (unit 27) shot at the enemy commander. A whirlwind in the middle: six arms of glowing
/// beads, white at their heads and teal down their tails, with six teal strands outside them, winding in as they turn,
/// from a wide ring (frame 0) to a tight spiral bright at its eye (~11), then drawing in and fading (12-17); a shot of
/// white blobs streaks right from the eye (12-13), then a white burst there with long cream rays fanning up and down
/// (13-17) and a slim white stream led by an arrow of light to the right edge, dying from its root (~18) to the edge (~23); the burst closes into a
/// tall white oval (16-19) that stands as a glassy arc, its rim to the left, fading teal (18-27); teal stars twinkle in
/// clumps along the beam's line (17-27) under a thin white needle and cyan streaks rushing right (18-25). The same seed
/// makes the same effect. Its shot is stretched right 1.75× past the eye, through the enemy's army, and the whole drawn
/// 244 units left and 20 up of where it was laid out, onto Isoroku's sprite (owner, 2026-10-15).
/// </summary>
internal static class WhirlwindShotEffect
{
    private const float Width = EffectCanvas.DesignWidth;

    /// <summary>The whirlwind's eye at frame 0, the burst's centre; the beam's line.</summary>
    private static readonly Vector2 Eye = new(414, 168);

    private const float Line = 178, Arms = 6;

    /// <summary>The eye at a frame: drifting a little left and down as the arms wind in.</summary>
    private static Vector2 EyeAt(float frame) => Eye + new Vector2(-0.7f, 0.8f) * Math.Clamp(frame, 0, 12);

    /// <summary>How far the arms' heads stand from the eye at a frame: winding in from the wide ring.</summary>
    private static float HeadRadius(float frame) => Math.Max(28, 210 - 16 * frame);

    /// <summary>How the whole whirlwind draws in about its eye after the shot: whole to frame 12, half by 17.</summary>
    private static float Closing(float frame) => 1 - 0.1f * Math.Clamp(frame - 12, 0, 5);

    /// <summary>When the beam dies at <paramref name="x"/>: from its root at ~18 to the right edge at ~23.</summary>
    private static float BeamEnd(float x) => Lerp(x, [440, 520, 600, 700, 800], [18.3f, 19, 19.8f, 21.3f, 23]);

    // The colours (light added over the scene).
    private static readonly Color White = new(1f, 1f, 1f), Bead = new(0.9f, 1f, 0.97f), Teal = new(0.4f, 0.9f, 0.75f);
    private static readonly Color Cream = new(1f, 0.94f, 0.72f), Glass = new(0.6f, 1f, 1f), GlassDim = new(0.3f, 0.58f, 0.62f);
    private static readonly Color Cyan = new(0.45f, 1f, 1f), BeamEdge = new(0.85f, 1f, 1f), Star = new(0.45f, 0.95f, 0.8f);

    /// <summary>
    /// The effect as laid out, its shot (from frame 12: the burst, the beam, the oval, the needle, the streaks, the stars)
    /// stretched right past the eye by <see cref="Reach"/> so that it runs on through the enemy's army, then moved onto
    /// Isoroku's sprite (owner, 2026-10-15); the whirlwind keeps its shape.
    /// </summary>
    public static IReadOnlyList<Particle> Build(int seed) =>
        ParticleLayout.StretchedRight(Layout(seed), Eye.X + 60, Reach, p => p.Birth >= 11.4f)
            .Select(p => p with { Position = p.Position + Shift }).ToList();

    private static readonly Vector2 Shift = new(-244, -20);

    private const float Reach = 1.75f;

    private static IReadOnlyList<Particle> Layout(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The whirlwind: six arms of beads turning and winding in, each bead's place set by the frame (its head at
        // HeadRadius, its tail curling back by its distance along the arm, the whole a little squat); white glows at the
        // heads, smaller teal ones with four-point glints down the tails, the tails growing out over the first seven
        // frames; and six all-teal strands just outside them from frame ~2 (frames 0-17.5).
        for (int arm = 0; arm < 2 * Arms; arm++)
        {
            bool outer = arm >= Arms;
            float phase = arm * MathF.Tau / Arms + (outer ? 0.4f : 0) + Range(-0.1f, 0.1f);
            for (float along = 0; along <= (outer ? 140 : 180); along += outer ? 9 : 7)
            {
                // An arm is 30 long at frame 0 and grows to its whole length by 7; an outer strand from frame 2.
                float birth = outer ? 2 + along / 30 : along <= 30 ? -0.5f : 7 * (along - 30) / 165, jitter = Range(-3, 3);
                float teal = outer ? 1 : Math.Clamp((along - 60) / 40, 0, 1), end = Lerp(teal, [0, 1], [15, 16.8f]) + Range(-0.5f, 0.5f);
                float life = end - birth, a = along + (outer ? 25 : 0), fade = Lerp(teal, [0, 1], [10, 12.8f]);
                Vector2 Offset(float t)
                {
                    float frame = birth + t * life;
                    float radius = HeadRadius(frame) + a + jitter, angle = phase + 0.11f * frame - a / 120;
                    return EyeAt(frame) - Eye + Vector2.FromAngle(angle) * radius * Closing(frame) * new Vector2(1, 0.88f);
                }
                float FadeOut(float t)
                {
                    float frame = birth + t * life;
                    return (frame - birth < 0.4f ? (frame - birth) / 0.4f : 1) * Math.Clamp((end - frame) / (end - fade), 0, 1);
                }
                float size = Lerp(teal, [0, 1], [42, 32]) * Range(0.85f, 1.15f);
                var colour = Bead.Lerp(Teal, teal);
                particles.Add(new Particle(EffectTextures.Glow, birth, life, Eye, Vector2.Zero, 0, 0, new Vector2(size, size),
                    colour, t => FadeOut(t) * (0.85f - 0.1f * teal), _ => 1, Move: Offset));
                if (teal < 0.5f)
                {
                    particles.Add(new Particle(EffectTextures.Dot, birth, life, Eye, Vector2.Zero, 0, 0, new Vector2(10, 10), White,
                        t => 0.6f * FadeOut(t), _ => 1, Move: Offset));
                    particles.Add(new Particle(EffectTextures.Haze, birth, life, Eye, Vector2.Zero, 0, 0, new Vector2(95, 95), Bead,
                        t => 0.2f * FadeOut(t), _ => 1, Move: Offset));
                }
                else if (random.NextDouble() < 0.6)
                    particles.Add(new Particle(EffectTextures.Sparkle, birth, life, Eye, Vector2.Zero, 0, 0, new Vector2(20, 20), White,
                        t => 0.7f * FadeOut(t), _ => 1, Move: Offset));
            }
        }

        // The eye: a glow brightening as the heads gather (frames 7-13) and white glints round it (8-13).
        particles.Add(new Particle(EffectTextures.Glow, 7, 6.5f, EyeAt(12), Vector2.Zero, 0, 0, new Vector2(170, 150), White,
            t => 0.45f * (t < 0.7f ? t / 0.7f : 1 - (t - 0.7f) / 0.3f), _ => 1));
        for (int i = 0; i < 24; i++)
        {
            float angle = Range(0, MathF.Tau), distance = Range(15, 110);
            particles.Add(new Particle(EffectTextures.Sparkle, Range(7.5f, 12), Range(1.5f, 3), EyeAt(11) + Vector2.FromAngle(angle) * distance,
                Vector2.FromAngle(angle + 1.6f) * 3, 0, 0, new Vector2(16, 16), White, Fade, _ => 1));
        }

        // The shot: white blobs streaking right from the eye (frames 12-14).
        for (int i = 0; i < 16; i++)
        {
            float size = Range(35, 70);
            particles.Add(new Particle(EffectTextures.Glow, Range(12.1f, 12.9f), Range(1.2f, 1.8f), new Vector2(Range(440, 500), Line + Range(-4, 4)),
                new Vector2(Range(150, 300), 0), 0, 0, new Vector2(size, size * 0.75f), White, Fade, _ => 1));
        }

        // The burst: a white glow and disc on the eye (frames 13-18) with long cream rays fanning up and down (13-17).
        var burst = EyeAt(12) + new Vector2(8, 2);
        particles.Add(new Particle(EffectTextures.Glow, 12.6f, 7.5f, burst, Vector2.Zero, 0, 0, new Vector2(280, 240), White,
            t => t < 0.1f ? t / 0.1f : 1 - MathF.Pow((t - 0.1f) / 0.9f, 1.5f), t => 1 - 0.3f * t));
        for (int i = 0; i < 16; i++)
        {
            float size = Range(45, 80);
            particles.Add(new Particle(EffectTextures.Billow, Range(12.8f, 13.4f), Range(5, 6.5f), burst + new Vector2(Range(-30, 35), Range(-35, 35)),
                Vector2.Zero, Range(0, MathF.Tau), 0, new Vector2(size, size), White, t => 0.8f * Held(t), t => 1 - 0.3f * t));
        }
        for (int i = 0; i < 40; i++)
        {
            bool wide = i >= 32;
            float angle = wide ? Range(-MathF.PI, MathF.PI) : (i % 2 == 0 ? -1 : 1) * MathF.PI / 2 + 0.22f * Normal(random);
            float length = wide ? Range(70, 130) : Range(160, 240), thickness = Range(16, 30);
            var direction = Vector2.FromAngle(angle);
            particles.Add(new Particle(EffectTextures.Spike, Range(13.1f, 13.8f), Range(3.3f, 4.3f), burst + direction * (15 + length / 2),
                Vector2.Zero, angle, 0, new Vector2(length, thickness), Cream, t => (t < 0.12f ? t / 0.12f : 1 - (t - 0.12f) / 0.88f) * 0.9f,
                t => 1 - 0.35f * t));
        }

        // The beam: a slim stream of white billows from the burst to the right edge (frames 13-14), dying from the root
        // (17.5-22.5), over a soft glow — an arrow's shot, not a blast (owner, 2026-10-15: v1's were 45-75 high) — led by a
        // white arrow of light flying from the burst to the edge (12.8-14.4).
        for (float x = 425; x <= Width + 30; x += 11)
        {
            float birth = 13 + (x - 425) / 500, death = BeamEnd(x) + Range(-0.6f, 0.6f), size = Range(16, 28) * Math.Min(1, 0.6f + (x - 425) / 100);
            particles.Add(new Particle(EffectTextures.Billow, birth, death - birth, new Vector2(x, Line + Range(-9, 9)), new Vector2(Range(0, 4), 0),
                0, 0, new Vector2(size * 2.2f, size), White, t => 0.75f * Held(t), t => 0.9f + 0.2f * t,
                Ramp: t => White.Lerp(BeamEdge, t)));
        }
        particles.Add(new Particle(EffectTextures.Haze, 13.2f, 8.5f, new Vector2(640, Line), new Vector2(3, 0), 0, 0, new Vector2(520, 50), BeamEdge,
            t => 0.35f * Held(t), _ => 1));
        particles.Add(new Particle(EffectTextures.Arrow, 12.8f, 1.6f, new Vector2(480, Line), new Vector2((Width + 60 - 480) / 1.6f, 0), 0, 0,
            new Vector2(150, 22), White, _ => 1, _ => 1));

        // The flecks round the beam: white specks (frames 14-22).
        for (int i = 0; i < 90; i++)
        {
            float size = Range(3, 6);
            particles.Add(new Particle(EffectTextures.Dot, Range(14, 21), Range(2, 4), new Vector2(Range(430, Width), Line + 45 * Normal(random)),
                new Vector2(Range(2, 8), 0), 0, 0, new Vector2(size, size), White, Fade, _ => 1));
        }

        // The lens: the burst closing into a tall white oval (frames 16.5-19.5), then the arc standing with its rim to the
        // left, glassy and turning teal as it fades (18-27).
        particles.Add(new Particle(EffectTextures.Glow, 15.6f, 3, burst, Vector2.Zero, 0, 0, new Vector2(75, 300), White,
            t => t < 0.4f ? t / 0.4f : 1 - (t - 0.4f) / 0.6f, t => 1 - 0.3f * t));
        particles.Add(new Particle(EffectTextures.Glow, 16, 2.5f, burst, Vector2.Zero, 0, 0, new Vector2(40, 230), White,
            t => t < 0.4f ? t / 0.4f : 1 - (t - 0.4f) / 0.6f, _ => 1));
        particles.Add(new Particle(EffectTextures.Lens, 17.3f, 2.4f, burst + new Vector2(12, 0), Vector2.Zero, 0, 0, new Vector2(-70, 330), White,
            t => 0.7f * (t < 0.3f ? t / 0.3f : 1 - (t - 0.3f) / 0.7f), _ => 1));
        particles.Add(new Particle(EffectTextures.Lens, 17.3f, 10.5f, burst + new Vector2(8, 0), new Vector2(-3, 0), 0, 0, new Vector2(150, 400), White,
            t => 0.8f * (t < 0.1f ? t / 0.1f : t < 0.45f ? 1 : 1 - MathF.Pow((t - 0.45f) / 0.55f, 1.1f)), t => 1 + 0.1f * t,
            Ramp: t => t < 0.3f ? White.Lerp(Glass, t / 0.3f) : Glass.Lerp(GlassDim, (t - 0.3f) / 0.7f)));

        // The needle: a thin white line along the beam's line from the arc (frames 18-22), then cyan streaks rushing right
        // on the target's side (20-25).
        particles.Add(new Particle(EffectTextures.Streak, 18, 4, new Vector2(620, Line), new Vector2(8, 0), 0, 0, new Vector2(480, 12), White,
            t => t < 0.1f ? t / 0.1f : 1 - t, _ => 1));
        for (int i = 0; i < 10; i++)
        {
            float length = Range(200, 320);
            particles.Add(new Particle(EffectTextures.Streak, Range(19.5f, 23), Range(2, 3), new Vector2(Range(600, 720), Line + Range(-14, 12)),
                new Vector2(Range(15, 30), 0), 0, 0, new Vector2(length, Range(7, 14)), Cyan, t => 0.9f * Fade(t), _ => 1));
        }

        // The stars: teal glows with four-point glints in clumps along the beam's line, twinkling (frames 16.5-27).
        var clumps = new List<Vector2>();
        for (int i = 0; i < 64; i++)
            clumps.Add(new Vector2((Range(340, 800) + Range(340, 800)) / 2, Math.Clamp(Line + 45 * Normal(random), 60, 295)));
        for (int i = 0; i < 300; i++)
        {
            float birth = Range(16.5f, 21.5f), life = Math.Min(Range(4, 7.5f), 27.5f - birth), size = Range(22, 44);
            var at = clumps[random.Next(clumps.Count)] + new Vector2(14 * Normal(random), 11 * Normal(random));
            var drift = new Vector2(Range(0, 2), Range(-0.5f, 0.5f));
            float twinkle = Range(0, MathF.Tau);
            float Alpha(float t) => (0.75f + 0.25f * MathF.Sin(twinkle + t * 20)) * Fade(t);
            particles.Add(new Particle(EffectTextures.Glow, birth, life, at, drift, 0, 0, new Vector2(size, size), Star, Alpha, _ => 1));
            if (i % 2 == 0)
                particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, drift, 0, 0, new Vector2(size, size) * 1.1f, Star.Lerp(White, 0.5f),
                    t => 0.6f * Alpha(t), _ => 1));
        }
        return particles;
    }

    /// <summary>The piecewise-linear curve through (<paramref name="xs"/>, <paramref name="ys"/>) at <paramref name="x"/>, flat past its ends.</summary>
    private static float Lerp(float x, ReadOnlySpan<float> xs, ReadOnlySpan<float> ys)
    {
        if (x <= xs[0])
            return ys[0];
        for (int i = 1; i < xs.Length; i++)
        {
            if (x <= xs[i])
                return ys[i - 1] + (ys[i] - ys[i - 1]) * (x - xs[i - 1]) / (xs[i] - xs[i - 1]);
        }
        return ys[^1];
    }

    /// <summary>A standard normal draw (Box-Muller).</summary>
    private static float Normal(Random random) =>
        MathF.Sqrt(-2 * MathF.Log(1 - (float)random.NextDouble())) * MathF.Cos(MathF.Tau * (float)random.NextDouble());

    /// <summary>In at once, full to three quarters of the life, then out.</summary>
    private static float Held(float t) => t < 0.06f ? t / 0.06f : t < 0.75f ? 1 : 1 - (t - 0.75f) / 0.25f;

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
