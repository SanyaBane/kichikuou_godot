using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The Vanish Bomb effect, ours after Sengoku Rance's Begone Bomb, effect 40560 (docs/sengoku/04-screen.md §2.4.1; not
/// traced; the owner's choice, 2026-10-14; 30 frames in the archive): Miki's (unit 32) blast. A wall of white light —
/// lumps and four-point sparkles, sparser and pink toward the right edge — closes in on a point left of the middle from
/// the frame's edges (frames 0-8), the hole in it shrinking to nothing; then, larger than Sengoku Rance's — the game's
/// mightiest blow, as Miki's sprite (CG 1529: a huge bomb, white and a little blue) shows it (owner, 2026-10-15) — a
/// white flash there whites out the whole frame (9-13) round an ice-white sphere swelling past the frame's height,
/// which throws long spikes off its edge and flattens into a white ellipse (10-15); a flat ice-white rim then spreads from
/// it far past the frame's edges with a blue shock wave behind it and a blue glow over the whole frame (13-24),
/// with long rays from the centre, cyan, gold and white star bursts, orange motes and steel-blue flecks inside it (13-26).
/// White and a little blue, as her sprite's bomb, with no green (owner, 2026-10-15: v1's shock wave, glow and rim
/// were green and mint, its flecks teal; now blue, ice and steel blue); drawn 70 units left and 70 up of where it is
/// laid out. The same seed makes the same effect.
/// </summary>
internal static class VanishBombEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>Where the light closes in, the flash's and the rays' centre.</summary>
    private static readonly Vector2 Centre = new(295, 185);

    /// <summary>The rim's centre: a little below the flash's.</summary>
    private static readonly Vector2 RimCentre = new(295, 200);

    /// <summary>
    /// When the edge of the hole in the light passes a point at <paramref name="distance"/> from the centre (across; the
    /// hole's height is its width / 1.12): from past the frame's edges before frame 0 to nothing at 8.
    /// </summary>
    private static float Closing(float distance) =>
        Lerp(distance, [0, 60, 120, 165, 200, 245, 290, 365, 400, 520], [8, 7, 6, 5, 4, 3, 2, 1, 0, -2]);

    /// <summary>The rim's outer half-width at a frame.</summary>
    private static float RimWidth(float frame) => Lerp(frame, [13.5f, 14, 16, 18, 20, 22, 24], [220, 290, 430, 560, 680, 790, 880]);

    private const float RimAspect = 0.45f;

    /// <summary>The sphere's radius at a frame: swelling from the flash past the frame's height.</summary>
    private static float SphereRadius(float frame) => Lerp(frame, [8.5f, 9.5f, 10.5f, 12, 13.5f, 15.5f], [60, 230, 330, 380, 400, 420]);

    // The colours (light added over the scene).
    private static readonly Color White = new(1f, 1f, 1f), Pink = new(1f, 0.78f, 1f), Lilac = new(0.95f, 0.85f, 1f);
    private static readonly Color Violet = new(0.6f, 0.45f, 0.63f), Ray = new(0.85f, 0.82f, 0.92f), Cyan = new(0.55f, 0.85f, 1f);
    private static readonly Color Gold = new(1f, 0.85f, 0.5f), Orange = new(1f, 0.6f, 0.2f), Amber = new(1f, 0.75f, 0.4f);
    private static readonly Color Teal = new(0.42f, 0.56f, 0.82f);

    // Miki's blast (CG 1529): the bomb's ice white, the rim's mint, the shock wave's green.
    private static readonly Color Ice = new(0.77f, 0.94f, 1f), Mint = new(0.9f, 0.95f, 1f), Green = new(0.2f, 0.36f, 0.78f);

    /// <summary>The effect as laid out, moved onto Miki's bomb (owner, 2026-10-15).</summary>
    public static IReadOnlyList<Particle> Build(int seed) =>
        Layout(seed).Select(p => p with { Position = p.Position + Shift }).ToList();

    private static readonly Vector2 Shift = new(-70, -70);

    private static IReadOnlyList<Particle> Layout(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The light closing in: lumps and sparkles over the whole frame, each lit as the hole's edge passes it and
        // gone at the flash; sparser, dimmer and pink past x 540 (frames 0-8).
        for (int i = 0; i < 420; i++)
        {
            var at = new Vector2(Range(-20, Width + 20), Range(-20, Height + 20));
            float right = Math.Clamp((at.X - 540) / 260, 0, 1);
            if (random.NextDouble() > 1 - 0.35f * right)
                continue;
            float distance = MathF.Sqrt((at.X - Centre.X) * (at.X - Centre.X) + (at.Y - Centre.Y) * 1.12f * ((at.Y - Centre.Y) * 1.12f));
            float birth = Closing(distance) - 0.3f, life = Math.Max(1, 8.8f + Range(0, 0.5f) - birth);
            float lead = 0.6f / life, strength = Range(0.24f, 0.36f), floor = 0.2f + 0.1f * right;
            float Alpha(float t) => strength * (t < lead ? t / lead : 1) * (floor + (1 - floor) * MathF.Exp(-t * life / 2.5f)) * Math.Min(1, (1 - t) * life / 0.4f);
            particles.Add(new Particle(EffectTextures.Billow, birth, life, at, Vector2.Zero, Range(0, MathF.Tau), 0,
                Vector2.One * Range(50, 110), White, Alpha, _ => 1));
            if (i % 3 != 2)
                particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at + new Vector2(Range(-15, 15), Range(-15, 15)), Vector2.Zero, 0, 0,
                    Vector2.One * Range(25, 45), at.X > 430 ? Pink : White, t => Alpha(t) / strength, _ => 1));
        }

        // The pink sparkles over the right, twinkling from the light's arrival to the flash's end (frames 3-13).
        for (int i = 0; i < 400; i++)
        {
            float birth = Range(3.5f, 12.5f), life = Range(2, 4);
            var at = new Vector2(Range(450, Width), Range(0, Height));
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, Vector2.Zero, 0, 0, Vector2.One * Range(16, 34),
                random.NextDouble() < 0.6 ? Pink : Lilac, t => 0.9f * Fade(t), _ => 1));
        }

        // The flash: a white glow swelling over the whole frame and shrinking (frames 8.4-13.8), a white-out of the whole
        // frame (9.3-11.7), a brief violet haze (8.6-11) and the green glow of the blast after it (10-21), then the flash
        // flattening into a white ellipse (12.2-15).
        for (int i = 0; i < 3; i++)
            particles.Add(new Particle(EffectTextures.Haze, 8.4f, 5.4f, Centre, Vector2.Zero, 0, 0, new Vector2(1, 0.8f), White,
                t => t < 0.08f ? t / 0.08f : t < 0.8f ? 1 : 1 - (t - 0.8f) / 0.2f,
                t => Lerp(8.4f + 5.4f * t, [8.4f, 9, 10, 11, 12, 13, 13.8f], [300, 850, 1150, 1050, 850, 650, 500])));
        particles.Add(new Particle(EffectTextures.Haze, 9.3f, 2.4f, new Vector2(Width / 2, Height / 2), Vector2.Zero, 0, 0, new Vector2(2600, 1800), White,
            t => t < 0.15f ? t / 0.15f : t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f, _ => 1));
        particles.Add(new Particle(EffectTextures.Haze, 8.6f, 2.4f, Centre, Vector2.Zero, 0, 0, new Vector2(2600, 1800), Violet,
            t => t < 0.2f ? t / 0.2f : 1 - (t - 0.2f) / 0.8f, _ => 1));
        particles.Add(new Particle(EffectTextures.Haze, 10, 11, Centre, Vector2.Zero, 0, 0, new Vector2(2800, 2000), Green,
            t => 0.85f * (t < 0.15f ? t / 0.15f : t < 0.35f ? 1 : 1 - (t - 0.35f) / 0.65f), _ => 1));
        for (int i = 0; i < 3; i++)
            particles.Add(new Particle(EffectTextures.Haze, 12.2f, 3.8f, RimCentre, Vector2.Zero, 0, 0, new Vector2(760, 280), White,
                t => t < 0.2f ? t / 0.2f : 1 - (t - 0.2f) / 0.8f, t => 0.9f + 0.3f * t));

        // The bomb: an ice-white sphere with a bright rim swelling from the flash past the frame's height, its white core
        // burning through the white-out (frames 8.5-15.5).
        particles.Add(new Particle(EffectTextures.Sphere, 8.5f, 7, Centre, Vector2.Zero, 0, 0, Vector2.One, Ice,
            t => t < 0.1f ? t / 0.1f : t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f, t => 2 * SphereRadius(8.5f + 7 * t)));
        particles.Add(new Particle(EffectTextures.Sphere, 9, 6, Centre, Vector2.Zero, 0, 0, Vector2.One, White,
            t => 0.7f * (t < 0.1f ? t / 0.1f : t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f), t => 1.7f * SphereRadius(9 + 6 * t)));
        particles.Add(new Particle(EffectTextures.Glow, 8.5f, 6, Centre, Vector2.Zero, 0, 0, Vector2.One, White,
            t => t < 0.1f ? t / 0.1f : t < 0.55f ? 1 : 1 - (t - 0.55f) / 0.45f, t => 2.4f * SphereRadius(8.5f + 6 * t)));

        // The spikes thrown off the sphere's edge, long and wide (frames 10-15.5).
        for (int i = 0; i < 130; i++)
        {
            float angle = Range(0, MathF.Tau), length = Range(180, 460), birth = Range(10, 13.5f);
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.85f).Normalized();
            var at = Centre + direction * (SphereRadius(birth) * Range(0.8f, 1) + length / 2);
            particles.Add(new Particle(EffectTextures.Spike, birth, Range(2, 3), at, direction * 30, direction.Angle(), 0,
                new Vector2(length, Range(18, 34)), Ice.Lerp(White, 0.5f), t => 0.9f * Fade(t), _ => 1));
        }

        // The rim: a thick flat mint ring spreading from the burst far past the frame's edges, greying as it fades (frames
        // 13.5-20.5), the blast's green shock wave just behind it (14-23).
        foreach (var (strength, scale) in new[] { (1f, 1f), (1f, 0.9f), (0.7f, 0.82f) })
            particles.Add(new Particle(EffectTextures.Rim, 13.5f, 10.5f, RimCentre, Vector2.Zero, 0, 0, new Vector2(1, RimAspect), Mint,
                t =>
                {
                    float frame = 13.5f + 10.5f * t;
                    return strength * (frame < 14 ? (frame - 13.5f) / 0.5f : frame < 16.5f ? 1 : Lerp(frame, [16.5f, 19, 20.5f], [1, 0.3f, 0]));
                },
                t => 2 * scale * RimWidth(13.5f + 10.5f * t) / 0.97f));
        foreach (float scale in new[] { 0.75f, 0.62f })
            particles.Add(new Particle(EffectTextures.Rim, 14, 9, RimCentre, Vector2.Zero, 0, 0, new Vector2(1, RimAspect), Green,
                t => Lerp(14 + 9 * t, [14, 15, 18, 23], [0, 1, 0.8f, 0]), t => 2 * scale * RimWidth(14 + 9 * t) / 0.97f));
        // As it fades it blurs into a broad soft grey band (frames 16-24).
        particles.Add(new Particle(EffectTextures.Ring, 16, 8, RimCentre, Vector2.Zero, 0, 0, new Vector2(1, RimAspect), White,
            t => Lerp(16 + 8 * t, [16, 18.5f, 21, 24], [0, 0.6f, 0.35f, 0]), t => 2 * 0.95f * RimWidth(16 + 8 * t) / 0.7f));
        // A grey glow filling it, spreading with it (frames 13.5-23).
        particles.Add(new Particle(EffectTextures.Haze, 13.5f, 9.5f, RimCentre, Vector2.Zero, 0, 0, new Vector2(1, RimAspect), White,
            t => 0.75f * (t < 0.05f ? t / 0.05f : t < 0.3f ? 1 : 1 - (t - 0.4f) / 0.6f), t => 2.1f * RimWidth(13.5f + 9.5f * t)));

        // The rays: long thin needles from the centre in every direction, longer across (frames 12.5-26).
        for (int i = 0; i < 70; i++)
        {
            float angle = Range(0, MathF.Tau), length = Range(160, 560), birth = Range(12.5f, 15), life = Math.Min(Range(8, 12), 27 - birth);
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * 0.7f).Normalized();
            particles.Add(new Particle(EffectTextures.Streak, birth, life, Centre + direction * (length / 2 + Range(0, 40)), Vector2.Zero,
                direction.Angle(), 0, new Vector2(length, Range(6, 12)), Ray,
                t => 0.6f * (t < 0.1f ? t / 0.1f : t < 0.5f ? 1 : 1 - (t - 0.5f) / 0.5f), _ => 1));
        }

        // The centre's star (frames 13.5-25).
        particles.Add(new Particle(EffectTextures.Sparkle, 13.5f, 11.5f, Centre, Vector2.Zero, 0, 0, new Vector2(70, 70), White,
            t => t < 0.05f ? t / 0.05f : 1 - t, _ => 1));
        particles.Add(new Particle(EffectTextures.Glow, 13.5f, 11.5f, Centre, Vector2.Zero, 0, 0, new Vector2(24, 24), White,
            t => t < 0.05f ? t / 0.05f : 1 - t, _ => 1));

        // Inside the rim: star bursts (cyan, gold, white), orange motes and teal flecks, each somewhere inside the rim
        // as it is when it appears and drifting out with it (frames 13.5-26).
        Vector2 Inside(float birth, out Vector2 drift)
        {
            float angle = Range(0, MathF.Tau), reach = MathF.Sqrt(Range(0.02f, 1));
            var direction = new Vector2(MathF.Cos(angle), MathF.Sin(angle) * RimAspect);
            drift = direction * reach * 25;
            return RimCentre + direction * RimWidth(birth) * reach;
        }
        for (int i = 0; i < 160; i++)
        {
            float birth = Range(13.5f, 20), life = Math.Min(Range(3, 6), 24.5f - birth), pick = (float)random.NextDouble();
            var at = Inside(birth, out var drift);
            var colour = pick < 0.6f ? Cyan : pick < 0.85f ? Gold : White;
            float size = Range(20, 50);
            particles.Add(new Particle(EffectTextures.Rays, birth, life, at, drift, Range(0, MathF.Tau), 0, Vector2.One * size, colour,
                t => 0.9f * Fade(t), _ => 1));
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, drift, 0, 0, Vector2.One * size * 0.8f, colour,
                t => 0.9f * Fade(t), _ => 1));
        }
        for (int i = 0; i < 160; i++)
        {
            float birth = Range(14, 20), life = Math.Min(Range(4, 8), 25 - birth);
            var at = Inside(birth, out var drift);
            particles.Add(new Particle(EffectTextures.Bokeh, birth, life, at, drift, 0, 0, Vector2.One * Range(6, 13),
                random.NextDouble() < 0.6 ? Orange : Amber, t => 0.85f * Fade(t), _ => 1));
        }
        for (int i = 0; i < 1000; i++)
        {
            float birth = Range(14, 20), life = Math.Min(Range(3, 6), 24.5f - birth);
            var at = Inside(birth, out var drift);
            float angle = (at - Centre).Angle();
            particles.Add(new Particle(EffectTextures.Dash, birth, life, at, drift, angle, 0, new Vector2(Range(18, 40), 3), Teal,
                t => 0.6f * Fade(t), _ => 1));
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

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
