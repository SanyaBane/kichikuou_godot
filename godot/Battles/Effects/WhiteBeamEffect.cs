using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The White Destruction Beam effect, ours after Sengoku Rance's White Destruction Beam, effect 40460
/// (docs/sengoku/04-screen.md §2.4.1; not traced): White D. Beam and EPO Light, and, laid over the scene dark
/// (<see cref="EffectPlayer.Dark"/>), Black D. Beam (owner, 2026-10-14). A white flash on the actor's half in a haze
/// of soft rays filling the frame (frames 0-3), breaking up in a violet halo with sparkles on its rim (3-5) into a
/// white disc that swells into a sphere of light, mottled inside and fading by ~17; a needle of light shoots right
/// from it (2-4), widening into a teal cone over the target's half (6-13) with swirls and white stars; then a mass of
/// horizontal streaks rushes right, white turning cyan, blue and violet, its back drawing toward the right edge
/// (12-22), the last violet streaks gone by ~27. Teal sparkles twinkle on the actor's half throughout (4-26) and a
/// grey glow lingers in the middle (12-25). The same seed makes the same effect. Each white beam's user has its own
/// class placing it (<see cref="ShizukaBeamEffect"/>, <see cref="MagicBeamEffect"/>, <see cref="EpoLightEffect"/>;
/// owner, 2026-10-15); the black beam is drawn as laid out here.
/// </summary>
internal static class WhiteBeamEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>The flash's and the sphere's centre; the beam's line.</summary>
    private static readonly Vector2 Orb = new(300, 168);

    /// <summary>The sphere's radius from frame 4.5, swelling.</summary>
    private static float SphereRadius(float frame) => 75 + 11 * Math.Max(0, frame - 5) + 1.1f * MathF.Pow(Math.Max(0, frame - 9), 2);

    /// <summary>Where the streaks' mass begins at a frame: behind the sphere, then drawing back toward the right edge.</summary>
    private static float MassBack(float frame) => Lerp(frame, [11, 15, 18, 20, 21, 22, 24, 27], [230, 260, 320, 400, 450, 500, 600, 700]);

    /// <summary>When the white mass dies down at <paramref name="x"/>: from behind the sphere at ~15 to the right at ~21.</summary>
    private static float MassEnd(float x) => Lerp(x, [280, 400, 500, 600, 700, 800], [15, 18, 19.5f, 20.5f, 21, 21.5f]);

    /// <summary>When the cone's light reaches <paramref name="x"/>.</summary>
    private static float ConeReaches(float x) => 6 + Math.Max(0, x - 380) / 60;

    // The colours (light added over the scene).
    private static readonly Color White = new(1f, 1f, 1f), Haze = new(0.7f, 0.82f, 0.82f), RayTint = new(0.6f, 0.75f, 0.75f, 0.13f);
    private static readonly Color Halo = new(0.42f, 0.32f, 0.85f), Pink = new(1f, 0.82f, 0.97f), Teal = new(0.55f, 0.95f, 0.95f);
    private static readonly Color TealHaze = new(0.42f, 0.62f, 0.62f), Cyan = new(0.6f, 0.95f, 1f), Blue = new(0.45f, 0.6f, 1f);
    private static readonly Color Violet = new(0.5f, 0.42f, 1f), Grey = new(0.5f, 0.53f, 0.53f), Star = new(0.8f, 1f, 1f);
    private static readonly Color StarViolet = new(0.6f, 0.55f, 1f), Cloud = new(0.9f, 0.95f, 0.95f, 0.8f);

    /// <summary>
    /// The beam laid out by <see cref="Build"/>, turned by <paramref name="tilt"/> (radians, + turning its far end
    /// down) about the flash and moved by <paramref name="offset"/> (the design space): a user's own place.
    /// </summary>
    public static IReadOnlyList<Particle> Placed(int seed, Vector2 offset, float tilt = 0)
    {
        var turn = Transform2D.Identity.Rotated(tilt);
        Vector2 Turned(Vector2 v) => turn.BasisXform(v);
        return Build(seed).Select(p => p with
        {
            Position = Orb + Turned(p.Position - Orb) + offset,
            Velocity = Turned(p.Velocity),
            Rotation = p.Rotation + tilt,
            Move = p.Move is { } move ? t => Turned(move(t)) : null,
        }).ToList();
    }

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The flash: a white disc in a white glow and a cyan-grey haze of soft rays filling the frame (frames 0-3), a
        // violet halo as it breaks up (3-5).
        particles.Add(new Particle(EffectTextures.Bokeh, -0.5f, 5.3f, Orb, Vector2.Zero, 0, 0, Vector2.One, White,
            t => t < 0.87f ? 1 : 1 - (t - 0.87f) / 0.13f, t => Lerp(t, [0, 0.3f, 0.65f, 1], [300, 340, 280, 200])));
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 4.5f, Orb, Vector2.Zero, 0, 0, new Vector2(720, 640), White,
            t => t < 0.55f ? 1 : 1 - (t - 0.55f) / 0.45f, t => 1 + 0.15f * t, Ramp: t => White.Lerp(Haze, t)));
        particles.Add(new Particle(EffectTextures.Haze, -0.5f, 5, Orb + new Vector2(30, 0), Vector2.Zero, 0, 0, new Vector2(1000, 800), Haze,
            t => t < 0.45f ? 1 : 1 - (t - 0.45f) / 0.55f, _ => 1));
        foreach (float turn in new[] { 0f, 0.4f, 0.9f })
            particles.Add(new Particle(EffectTextures.Rays, -0.5f, 4.5f, Orb, Vector2.Zero, turn, 0.01f, new Vector2(1000, 1000), RayTint,
                t => t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f, t => 1 + 0.1f * t));
        particles.Add(new Particle(EffectTextures.Haze, 2, 4, Orb, Vector2.Zero, 0, 0, new Vector2(900, 700), Halo,
            t => t < 0.3f ? t / 0.3f : 1 - (t - 0.3f) / 0.7f, t => 1 - 0.15f * t));

        // Its break-up: lumps of white round the rim, pink-tinged, with glints on them (frames 2.5-5).
        for (int i = 0; i < 60; i++)
        {
            float angle = Range(0, MathF.Tau), distance = Range(80, 140), size = Range(28, 60);
            var at = Orb + Vector2.FromAngle(angle) * distance;
            particles.Add(new Particle(EffectTextures.Billow, Range(2.6f, 3.6f), Range(1.6f, 2.4f), at, Vector2.FromAngle(angle) * 5,
                Range(0, MathF.Tau), 0, new Vector2(size, size), i % 3 == 0 ? Pink : White, Fade, t => 1 + 0.3f * t));
            if (i % 2 == 0)
                particles.Add(new Particle(EffectTextures.Sparkle, Range(3, 4.2f), Range(1.5f, 2.5f), at + Vector2.FromAngle(angle) * 15,
                    Vector2.FromAngle(angle) * 5, 0, 0, new Vector2(30, 30), White, Fade, _ => 1));
        }

        // The disc: white and flat at frame 5, turning into a sphere of light that swells, mottled inside, and fades
        // (5-17).
        particles.Add(new Particle(EffectTextures.Bokeh, 4.3f, 2.2f, Orb, Vector2.Zero, 0, 0, Vector2.One, White,
            t => t < 0.15f ? t / 0.15f : t < 0.45f ? 1 : 1 - (t - 0.45f) / 0.55f, t => 2 * SphereRadius(4.3f + 2.2f * t) * 1.02f));
        particles.Add(new Particle(EffectTextures.Sphere, 4.8f, 12.5f, Orb, Vector2.Zero, 0, 0, Vector2.One, White,
            t => (t < 0.08f ? t / 0.08f : 1) * (t < 0.45f ? 1 : 1 - MathF.Pow((t - 0.45f) / 0.55f, 0.8f)),
            t => 2 * SphereRadius(4.8f + 12.5f * t)));
        for (int i = 0; i < 130; i++)
        {
            float birth = Range(5, 12), radius = SphereRadius(birth);
            float angle = i % 3 == 0 ? Range(-MathF.PI, MathF.PI) : Range(-1.4f, 1.4f), share = MathF.Sqrt((float)random.NextDouble()) * 0.85f;
            float size = Range(22, 52);
            var at = Orb + Vector2.FromAngle(angle) * radius * share;
            // Spreading with the sphere as it swells (~11 a frame at its rim), drifting right.
            var drift = Vector2.FromAngle(angle) * 11 * share + new Vector2(3, 0);
            particles.Add(new Particle(EffectTextures.Billow, birth, Math.Max(2.5f, Math.Min(MassEnd(at.X) - 2, 14) - birth) + Range(-1, 1), at,
                drift, Range(0, MathF.Tau), 0, new Vector2(size, size * Range(0.6f, 1)), Cloud, t => 0.55f * Held(t), t => 0.8f + 0.5f * t));
        }

        // The needle: shooting right from the flash's rim (frames 2-4), then the beam itself with a soft glow at its
        // root and a bright core line to the right edge (5-13).
        foreach (var (birth, length) in new[] { (1.8f, 160f), (2.6f, 320f), (3.3f, 420f) })
        {
            float from = Orb.X + 150;
            particles.Add(new Particle(EffectTextures.Spike, birth, 6.2f - birth, new Vector2(from + length / 2, Orb.Y), Vector2.Zero, 0, 0,
                new Vector2(length, 22), White, t => t < 0.1f ? t / 0.1f : 1 - 0.6f * t, _ => 1));
        }
        particles.Add(new Particle(EffectTextures.Spike, 4.5f, 9, new Vector2(Orb.X + 300, Orb.Y), new Vector2(2, 0), 0, 0,
            new Vector2(560, 50), White, t => t < 0.1f ? t / 0.1f : 1 - MathF.Pow((t - 0.1f) / 0.9f, 1.5f), t => 1 + 0.6f * t,
            Ramp: t => White.Lerp(Cyan, t)));
        particles.Add(new Particle(EffectTextures.Glow, 4.8f, 6, new Vector2(Orb.X + 120, Orb.Y), new Vector2(6, 0), 0, 0, new Vector2(300, 150),
            White, t => t < 0.2f ? t / 0.2f : 1 - (t - 0.2f) / 0.8f, t => 1 + 0.5f * t, Ramp: t => White.Lerp(Cyan, t)));
        particles.Add(new Particle(EffectTextures.Haze, 5, 7, new Vector2(Orb.X + 150, Orb.Y), new Vector2(8, 0), 0, 0, new Vector2(340, 220),
            Cloud, t => t < 0.2f ? t / 0.2f : 1 - (t - 0.2f) / 0.8f, t => 1 + 0.4f * t));
        particles.Add(new Particle(EffectTextures.Streak, 5, 9, new Vector2(Orb.X + 300, Orb.Y), Vector2.Zero, 0, 0, new Vector2(620, 8), Cyan,
            t => t < 0.1f ? t / 0.1f : 1 - t, _ => 1));
        particles.Add(new Particle(EffectTextures.Glow, 6, 4, new Vector2(620, Orb.Y), new Vector2(30, 0), 0, 0, new Vector2(110, 36), Cyan,
            Fade, _ => 1));

        // The cone: teal light widening right from the sphere over the target's half (frames 6-14), and a broad haze
        // there that lingers under the streaks (8-22).
        particles.Add(new Particle(EffectTextures.Spike, 5.5f, 9.5f, new Vector2(Orb.X + 330, Orb.Y), new Vector2(3, 0), 0, 0,
            new Vector2(-580, 650), Teal, t => t < 0.25f ? t / 0.25f : 1 - (t - 0.25f) / 0.75f, t => 1 + 0.4f * t));
        particles.Add(new Particle(EffectTextures.Haze, 7.5f, 14.5f, new Vector2(700, Orb.Y), new Vector2(2, 0), 0, 0, new Vector2(900, 620), TealHaze,
            t => t < 0.2f ? t / 0.2f : 1 - MathF.Pow((t - 0.2f) / 0.8f, 1.3f), t => 1 + 0.2f * t));
        particles.Add(new Particle(EffectTextures.Haze, 11, 16, new Vector2(820, Orb.Y), Vector2.Zero, 0, 0, new Vector2(760, 640), TealHaze,
            t => t < 0.15f ? t / 0.15f : t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f, _ => 1));

        // The bright clouds behind the streaks: from the sphere toward the middle of the target's half as the cone
        // reaches them, each dying as the mass draws back (frames 7-21).
        for (int i = 0; i < 40; i++)
        {
            float x = Range(360, 580), birth = ConeReaches(x) + Range(0, 3), death = MassEnd(x) - 2 + Range(-1.2f, 1.2f);
            float spread = 35 + (x - 360) * 0.18f, size = Range(35, 70);
            particles.Add(new Particle(EffectTextures.Billow, birth, Math.Max(2, death - birth), new Vector2(x, Orb.Y + spread * Normal(random) * 0.8f),
                new Vector2(Range(3, 9), 0), 0, 0, new Vector2(size * 1.8f, size * 0.8f), x < 500 ? Cloud : Cloud.Lerp(Cyan, 0.5f),
                t => 0.6f * Held(t), t => 0.85f + 0.3f * t));
        }

        // The streaks: horizontal needles rushing right from the mass's back, white turning cyan, then blue and violet
        // as the mass draws back (frames 11-24), a few violet ones last on the right (to ~27).
        for (int i = 0; i < 640; i++)
        {
            float birth = 10.5f + 12.5f * MathF.Pow((float)random.NextDouble(), 1.1f), back = MassBack(birth);
            float reach = Lerp(birth, [11, 15, 18], [560, 700, Width + 40]);  // the early ones short of the right edge
            float length = Range(80, 280), x = back + (reach - back) * MathF.Pow((float)random.NextDouble(), 1.4f);
            float y = Math.Clamp(Orb.Y + 65 * Normal(random), 8, Height - 8), thickness = Range(3, 11);
            var colour = birth < 19 ? White.Lerp(Cyan, (birth - 10.5f) / 8.5f * 0.6f)
                : Cyan.Lerp(random.NextDouble() < 0.5 ? Blue : Violet, Math.Clamp((birth - 19) / 4, 0, 1) * 0.8f);
            float strength = birth < 21 ? 0.85f : 0.6f;
            particles.Add(new Particle(EffectTextures.Streak, birth, Range(2, 4.5f), new Vector2(x, y), new Vector2(Range(14, 30), 0), 0, 0,
                new Vector2(length, thickness), colour, t => Fade(t) * strength, t => 1 + 0.3f * t));
        }
        for (int i = 0; i < 70; i++)
        {
            float birth = Range(12, 21), back = MassBack(birth), size = Range(20, 50);
            particles.Add(new Particle(EffectTextures.Glow, birth, Range(2, 4), new Vector2(Range(back + 30, Width), Orb.Y + 55 * Normal(random)),
                new Vector2(Range(8, 20), 0), 0, 0, new Vector2(size, size), birth < 18 ? White : Cyan, Fade, _ => 1));
        }
        for (int i = 0; i < 14; i++)
        {
            float birth = Range(21.5f, 25), length = Range(120, 260);
            particles.Add(new Particle(EffectTextures.Streak, birth, Range(2.5f, 3.5f), new Vector2(Range(560, 800), Orb.Y + Range(-50, 20)),
                new Vector2(Range(6, 12), 0), 0, 0, new Vector2(length, Range(6, 10)), i % 2 == 0 ? Violet : Blue, t => 0.7f * Fade(t), _ => 1));
        }

        // The swirls: teal spirals turning on the target's half, bluer and dimmer later (frames 8-26).
        for (int i = 0; i < 10; i++)
        {
            float birth = Range(7.5f, 22), size = Range(45, 90), strength = birth < 15 ? 0.7f : 0.3f;
            var colour = birth < 15 ? Teal : Teal.Lerp(Blue, 0.6f);
            particles.Add(new Particle(EffectTextures.Swirl, birth, Range(5, 8), new Vector2(Range(500, 790), Range(25, 295)),
                new Vector2(Range(0, 3), Range(-1, 1)), Range(0, MathF.Tau), Range(0.15f, 0.3f), new Vector2(size, size), colour,
                t => (t < 0.15f ? t / 0.15f : 1 - (t - 0.15f) / 0.85f) * strength, t => 0.6f + 0.6f * t));
            particles.Add(new Particle(EffectTextures.Glow, birth, Range(5, 8), new Vector2(Range(500, 790), Range(25, 295)),
                Vector2.Zero, 0, 0, new Vector2(size, size) * 0.7f, colour, t => Fade(t) * strength * 0.6f, _ => 1));
        }

        // The stars: white flecks and sparkles on the target's half, drifting right (frames 8-22).
        for (int i = 0; i < 130; i++)
        {
            float birth = Range(8, 21.5f), size = Range(6, 22);
            var at = new Vector2(Range(Math.Max(420, MassBack(birth)), 800), Range(10, 304));
            bool sparkle = i % 3 == 0;
            particles.Add(new Particle(sparkle ? EffectTextures.Sparkle : EffectTextures.Dot, birth, Range(2, 5), at,
                new Vector2(Range(2, 8), 0), 0, 0, new Vector2(size, size) * (sparkle ? 1.8f : 1), Star, Fade, _ => 1));
        }

        // The sparkles: four-point stars in teal glows round the disc's rim (frame 4), scattering over the actor's half
        // and more joining, twinkling till they fade (to ~26), a few violet.
        for (int i = 0; i < 110; i++)
        {
            bool ring = i < 70;
            float birth = ring ? Range(3.5f, 5.5f) : Range(5, 14);
            Vector2 at, drift;
            if (ring)
            {
                float angle = Range(0, MathF.Tau);
                at = Orb + Vector2.FromAngle(angle) * Range(90, 160);
                drift = Vector2.FromAngle(angle) * Range(2, 6) + new Vector2(-2, 0);
            }
            else
            {
                at = new Vector2(Range(10, 470), Range(5, 309));
                drift = new Vector2(Range(-1.5f, 0.5f), Range(-0.5f, 0.5f));
            }
            float size = Range(24, 60), twinkle = Range(0, MathF.Tau), end = Range(17, 27);
            var colour = i % 7 == 0 ? StarViolet : Star;
            float Alpha(float t) => (0.65f + 0.35f * MathF.Sin(twinkle + t * 25)) * (t < 0.04f ? t / 0.04f : 1 - MathF.Pow((t - 0.04f) / 0.96f, 1.3f));
            particles.Add(new Particle(EffectTextures.Sparkle, birth, end - birth, at, drift, 0, 0, new Vector2(size, size), colour,
                t => 0.6f * Alpha(t), _ => 1));
            particles.Add(new Particle(EffectTextures.Glow, birth, end - birth, at, drift, 0, 0, new Vector2(size, size) * 1.5f,
                colour * new Color(0.6f, 0.9f, 0.9f, 0.9f), Alpha, _ => 1));
        }

        // The teal glows lingering on the target's half (frames 17-27).
        for (int i = 0; i < 40; i++)
        {
            float size = Range(15, 45);
            particles.Add(new Particle(EffectTextures.Glow, Range(16, 23), Range(4, 7), new Vector2(Range(450, 800), Range(10, 304)),
                new Vector2(Range(0, 2), 0), 0, 0, new Vector2(size, size) * 1.4f, TealHaze, t => 0.7f * Fade(t), _ => 1));
        }

        // The grey glow lingering in the middle, where the sphere was, and over the target's half (frames 10-27).
        particles.Add(new Particle(EffectTextures.Haze, 10, 17, new Vector2(530, Orb.Y), new Vector2(3, 0), 0, 0, new Vector2(1000, 560), Grey,
            t => t < 0.15f ? t / 0.15f : t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f, _ => 1));
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
