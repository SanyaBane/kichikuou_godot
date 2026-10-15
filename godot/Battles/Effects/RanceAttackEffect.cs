using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The Rance Attack effect, ours after Sengoku Rance's own Rance Attack, effect 40500 (docs/sengoku/04-screen.md
/// §2.4.1; not traced), one effect for the commander's blow alone (owner, 2026-10-14). An eruption in the middle:
/// long white spikes fanning up from a white-hot foot with flat glassy rings spreading on the ground (frames 0-6), the
/// spikes turning blue; the white mass then spreads right over the target's half behind a pale arc of a shock front
/// throwing sparks (frames 5-10), fills it (9-12) and draws back to the right edge, its trailing side burning in rows
/// of blue flame tongues (12-26), gone by ~22. Sand-coloured dust billows on the actor's half (0-15) and drifts over
/// the target's later (14-26); a cluster of pale blue motes hangs in the middle from frame ~9, fading by ~27; the rings
/// spread wide and darken to blue (to ~18). The same seed makes the same effect.
/// </summary>
internal static class RanceAttackEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>The eruption's foot.</summary>
    private static readonly Vector2 Foot = new(410, 245);

    /// <summary>Where the mass's back stands from frame 10, drawing back to the right edge.</summary>
    private static float Back(float frame) => Math.Min(740, 330 + Math.Max(0, frame - 10) * 50);

    /// <summary>When the mass dies down at <paramref name="x"/>: from the middle at frame ~10 to the right edge at ~22.</summary>
    private static float MassEnd(float x)
    {
        ReadOnlySpan<float> xs = [350, 450, 550, 650, 750, 820], ends = [10.5f, 12, 13.5f, 16.5f, 21.5f, 24];
        for (int i = 1; i < xs.Length; i++)
        {
            if (x <= xs[i])
                return ends[i - 1] + (ends[i] - ends[i - 1]) * Math.Max(0, x - xs[i - 1]) / (xs[i] - xs[i - 1]);
        }
        return ends[^1];
    }

    /// <summary>Where the mass's front stands, spreading right from frame 5.</summary>
    private static float FrontAt(float frame) => Math.Min(Width + 40, 545 + Math.Max(0, frame - 5.5f) * 55);

    // The colours (light added over the scene).
    private static readonly Color White = new(1f, 1f, 1f), Ice = new(0.75f, 0.95f, 1f), Blue = new(0.32f, 0.42f, 0.95f);
    private static readonly Color Mass = new(1f, 1f, 0.98f, 0.45f), MassEdge = new(0.55f, 0.75f, 1f, 0.45f), RingIce = new(0.7f, 0.92f, 1f, 0.75f);
    private static readonly Color RingBlue = new(0.25f, 0.35f, 0.85f, 0.4f), Disc = new(0.18f, 0.26f, 0.7f, 0.45f);
    private static readonly Color Front = new(0.88f, 0.92f, 0.95f, 1f);
    private static readonly Color Dust = new(0.78f, 0.74f, 0.62f, 0.19f), DustLate = new(0.66f, 0.57f, 0.36f, 0.36f);
    private static readonly Color Speck = new(1f, 0.96f, 0.8f, 0.45f), Fleck = new(0.9f, 0.97f, 1f, 0.9f);
    private static readonly Color Mote = new(0.55f, 0.65f, 1f, 0.6f), MoteCore = new(0.75f, 0.82f, 1f), Flame = new(0.42f, 0.55f, 1f, 0.55f);
    private static readonly Color FlameHot = new(0.8f, 0.9f, 1f, 0.6f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The spikes: long wedges fanning up from the foot, white, then ice and blue as they age and thin out.
        for (int i = 0; i < 140; i++)
        {
            float birth = 4 * MathF.Pow((float)random.NextDouble(), 1.5f) - 1.5f, life = Range(3.5f, 7.5f);
            float angle = -MathF.PI / 2 + Range(-1.4f, 1.4f);
            float length = Range(200, 480), thickness = Range(20, 46);
            var foot = Foot + new Vector2(Range(-90, 90), Range(-25, 15));
            var direction = Vector2.FromAngle(angle);
            particles.Add(new Particle(EffectTextures.Spike, birth, life, foot + direction * length / 2, direction * 6, angle, 0,
                new Vector2(length, thickness), White, t => t < 0.1f ? t / 0.1f : 1 - MathF.Pow((t - 0.1f) / 0.9f, 1.4f),
                t => 1 + 0.15f * t,
                Ramp: t => t < 0.35f ? White.Lerp(Ice, t / 0.35f) : Ice.Lerp(Blue, Math.Min(1, (t - 0.35f) / 0.5f))));
        }

        // The white-hot foot: a glow and puffs swelling where the spikes leave, standing to frame ~10.
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 9, Foot + new Vector2(0, -40), Vector2.Zero, 0, 0,
            new Vector2(380, 320), White, t => t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f, t => 1 + 0.5f * t));
        for (int i = 0; i < 34; i++)
        {
            var at = new Vector2(Range(345, 530), Range(30, Height));
            float birth = Range(-0.5f, 4), size = Range(100, 180);
            particles.Add(new Particle(EffectTextures.Billow, birth, Range(9.5f, 11.5f) - birth, at, Vector2.Zero, Range(0, MathF.Tau), 0,
                new Vector2(size, size), Mass, Held, t => 0.6f + 0.6f * t));
        }

        // The ground's glow: a flat white ellipse under the foot, inside the rings.
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 9, Foot + new Vector2(0, 30), Vector2.Zero, 0, 0,
            new Vector2(300, 80), White, t => t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f, t => 1 + 0.8f * t));

        // The mass: white puffs filling behind its front as it spreads right, each dying when the back passes it.
        for (int i = 0; i < 130; i++)
        {
            float birth = Range(4, 12), front = FrontAt(birth);
            float x = front - (front - 330) * MathF.Pow((float)random.NextDouble(), 1.6f);
            float death = MassEnd(x) + Range(-1.5f, 1.5f);
            float size = Range(130, 220);
            particles.Add(new Particle(EffectTextures.Billow, birth, Math.Max(3, death - birth), new Vector2(x, Range(-10, Height + 10)),
                new Vector2(3, 0), Range(0, MathF.Tau), Range(-0.02f, 0.02f), new Vector2(size * 1.2f, size), Mass, Held,
                t => 0.85f + 0.3f * t, Ramp: t => Mass.Lerp(MassEdge, Math.Clamp((t - 0.6f) / 0.4f, 0, 1))));
        }

        // The shock front: a pale arc bulging right just ahead of the mass, from the middle to the right edge (frames 5-11).
        particles.Add(new Particle(EffectTextures.Shock, 4.5f, 7, new Vector2(FrontAt(5.5f) - 116, Height / 2), new Vector2(55, 0), 0, 0,
            new Vector2(260, 440), Front, Fade, t => 1 + 0.15f * t));

        // The sparks the front throws right: short streaks with a bright head.
        for (int i = 0; i < 50; i++)
        {
            float birth = Range(4.5f, 11), angle = Range(-0.5f, 0.5f);
            var at = new Vector2(FrontAt(birth) + Range(-10, 50), Range(20, Height - 10));
            var velocity = Vector2.FromAngle(angle) * Range(25, 45);
            float length = Range(30, 70);
            particles.Add(new Particle(EffectTextures.Dash, birth, Range(2, 4), at, velocity, angle, 0, new Vector2(length, 3), Ice,
                Fade, _ => 1));
            particles.Add(new Particle(EffectTextures.Dot, birth, Range(2, 4), at + Vector2.FromAngle(angle) * length / 2, velocity,
                0, 0, new Vector2(5, 5), White, Fade, _ => 1));
        }

        // The rings on the ground: flat glassy ellipses spreading from the foot and drifting left, ice turning blue,
        // over a dark blue disc.
        for (int i = 0; i < 5; i++)
        {
            float birth = -0.8f + i * 1f, life = Range(15, 18);
            float width = Range(150, 200);
            var at = Foot + new Vector2(Range(-15, 15), 28 + i * 3);
            foreach (float thickness in new[] { 0.16f, 0.24f })
                particles.Add(new Particle(EffectTextures.Ring, birth, life, at, new Vector2(-10, -2.5f), 0, 0,
                    new Vector2(width, width * thickness), RingIce, Fade, t => 1 + 3.2f * MathF.Sqrt(t),
                    Ramp: t => RingIce.Lerp(RingBlue, Math.Min(1, t * 1.6f))));
        }
        particles.Add(new Particle(EffectTextures.Glow, 4, 15, Foot + new Vector2(-60, 25), new Vector2(-8, -3), 0, 0,
            new Vector2(420, 120), Disc, Swell, t => 1 + 0.7f * t));

        // The dust: billows on the actor's half, from a clump beside the foot spreading left and up, fading by frame
        // ~15, with its grain and a few bright flecks; later a browner cloud over the target's half, behind the mass.
        var puffs = new List<(Vector2 At, float Radius)>();
        for (int i = 0; i < 40; i++)
        {
            float birth = 5 * MathF.Pow((float)random.NextDouble(), 1.5f) - 0.5f;
            float spread = Math.Max(0, birth) / 5;
            var at = new Vector2(215 - 150 * spread + Range(-70, 70), 215 - 120 * spread + Range(-50, 60));
            float r0 = Range(25, 42), rf = Range(50, 90);
            puffs.Add((at, rf));
            particles.Add(new Particle(EffectTextures.Puff, birth, Range(14, 16.5f) - birth, at,
                new Vector2(Range(-1.5f, 0.3f), Range(-1.5f, -0.3f)), Range(0, MathF.Tau), Range(-0.01f, 0.01f), new Vector2(2, 2),
                Dust, t => t < 0.35f ? 1 : 1 - (t - 0.35f) / 0.65f, t => r0 + (rf - r0) * (1 - MathF.Pow(1 - Math.Min(1, t * 3), 3))));
        }
        for (int i = 0; i < 350; i++)
        {
            var (at, radius) = puffs[random.Next(puffs.Count)];
            float angle = Range(0, MathF.Tau), distance = MathF.Sqrt((float)random.NextDouble()) * radius * 0.8f;
            float size = Range(1.5f, 3.2f);
            particles.Add(new Particle(EffectTextures.Dot, Range(1, 4), Range(8, 12), at + Vector2.FromAngle(angle) * distance,
                new Vector2(Range(-0.8f, 0.3f), Range(-1.2f, -0.4f)), 0, 0, new Vector2(size, size), Speck, Fade, _ => 1));
        }
        for (int i = 0; i < 22; i++)
        {
            float size = Range(12, 30);
            particles.Add(new Particle(EffectTextures.Glow, Range(5, 12), Range(2, 5), new Vector2(Range(140, 340), Range(30, 290)),
                Vector2.Zero, 0, 0, new Vector2(size, size), Fleck, Fade, _ => 1));
        }
        for (int i = 0; i < 24; i++)
        {
            float size = Range(110, 190);
            particles.Add(new Particle(EffectTextures.Puff, Range(13, 17), Range(9, 12), new Vector2(Range(470, Width), Range(-10, Height)),
                new Vector2(Range(0.5f, 2), Range(-1, 0.5f)), Range(0, MathF.Tau), 0, new Vector2(size, size), DustLate,
                t => t < 0.25f ? t / 0.25f : 1 - (t - 0.25f) / 0.75f, t => 0.8f + 0.3f * t));
        }

        // The flames: rows of blue tongues on the mass's back as it draws back, leaning right, each trailing two fainter
        // copies down and to the left; the ones nearest the mass hot, the late ones dimmer.
        for (int i = 0; i < 150; i++)
        {
            float birth = Range(11.5f, 24), life = Range(2.5f, 5);
            float x = Math.Min(Width - 10, Back(birth) - Range(-40, 90)), height = Range(60, 140);
            var at = new Vector2(x, Range(60, Height + 30) - height / 2);
            var colour = x > Back(birth) - 15 && birth < 19 ? FlameHot : Flame;
            float late = Math.Clamp((23.5f - birth) / 5, 0, 1), rotation = Range(-0.1f, 0.3f);
            for (int copy = 0; copy < 3; copy++)
            {
                float strength = late * (copy == 0 ? 1 : copy == 1 ? 0.55f : 0.3f);
                particles.Add(new Particle(EffectTextures.Tongue, birth, life, at + new Vector2(-14, 8) * copy, new Vector2(6, -3),
                    rotation, 0, new Vector2(height * 0.8f, height), colour, t => Fade(t) * strength, t => 1 + 0.1f * t));
            }
        }

        // The motes: a cluster of pale blue glows hanging in the middle, twinkling and fading, a few scattered up and left.
        for (int i = 0; i < 160; i++)
        {
            bool scattered = i >= 130;
            float birth = scattered ? Range(10, 15) : Range(7, 13);
            var at = scattered
                ? new Vector2(Range(40, 330), Range(0, 130))
                : new Vector2(400 + 35 * Normal(random), Math.Clamp(145 + 70 * Normal(random), 5, 300));
            float size = scattered ? Range(16, 30) : Range(28, 60), twinkle = Range(0, MathF.Tau), end = Range(25, 28.5f);
            var drift = new Vector2(Range(-0.4f, 0.4f), Range(-0.6f, 0.2f));
            float Alpha(float t) =>
                (0.7f + 0.3f * MathF.Sin(twinkle + t * 30)) * (t < 0.05f ? t / 0.05f : 1 - MathF.Pow((t - 0.05f) / 0.95f, 0.7f));
            particles.Add(new Particle(EffectTextures.Glow, birth, end - birth, at, drift, 0, 0, new Vector2(size, size), Mote, Alpha, _ => 1));
            particles.Add(new Particle(EffectTextures.Dot, birth, end - birth, at, drift, 0, 0, new Vector2(size, size) * 0.3f, MoteCore,
                Alpha, _ => 1));
        }
        return particles;
    }

    /// <summary>
    /// By la Way, Rick's (unit 7) special 1 (owner, 2026-10-15): the same effect recoloured red, as his sprite (CG 1521,
    /// red 245, 40, 35) — every colour where blue leads or that is white turned to red: white to a pinkish red (salmon,
    /// then white, only where the light piles up), ice to red, the blues deep red; the sand dust reddened to a rust brown.
    /// </summary>
    public static IReadOnlyList<Particle> BuildRed(int seed) =>
        Build(seed).Select(p => p with { Colour = Red(p.Colour), Ramp = p.Ramp is { } ramp ? t => Red(ramp(t)) : null }).ToList();

    private static Color Red(Color c)
    {
        if (c.B < c.R - 0.05f)
            return new Color(c.R, 0.7f * c.G, 0.6f * c.B, c.A);
        float top = Math.Max(c.G, c.B), white = Math.Min(c.R, Math.Min(c.G, c.B));
        return new Color(top, 0.14f * white + (top - white) * 0.05f, 0.12f * white, c.A);
    }

    /// <summary>A standard normal draw (Box-Muller).</summary>
    private static float Normal(Random random) =>
        MathF.Sqrt(-2 * MathF.Log(1 - (float)random.NextDouble())) * MathF.Cos(MathF.Tau * (float)random.NextDouble());

    /// <summary>In at once, full to three quarters of the life, then out (the white mass).</summary>
    private static float Held(float t) => t < 0.06f ? t / 0.06f : t < 0.75f ? 1 : 1 - (t - 0.75f) / 0.25f;

    /// <summary>In over the first sixth, then out (the dark disc).</summary>
    private static float Swell(float t) => t < 0.15f ? t / 0.15f : 1 - MathF.Pow((t - 0.15f) / 0.85f, 2);

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
