using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The Kentarou-kun Slash effect, ours after Sengoku Rance's own move of Kentarou's, 40800 (docs/sengoku/04-screen.md
/// §2.4.1; not traced; the owner's choice, 2026-10-14): Kentarou's Rance Attack (special 1 by unit 31,
/// docs/battle/02-resolution.md §3.3), in the white and mint of his sprite (CG 1222) where the reference is blue. A
/// blast thrown right across the field from a white-hot core where his sprite's blow strikes the ground (its eruption,
/// CG 1222's x 300-400; owner, 2026-10-15), moved 20 units right of it (owner); a domed shock front bursting from the core (frame 3) and running right to the edge (4-10), throwing
/// sparks, a white mass filling behind it and holding the target's half (to ~14); pale teal crescents flying right
/// through it (5-17), a column of teal light standing right of the core (7-16) with a band of fine specks;
/// sand-coloured dust over the target's half (9-26). Nothing is drawn behind Kentarou: the reference's spikes fanning
/// back to the left and its motes and dust on the actor's half are left out (owner, 2026-10-15). The same seed makes the same effect.
/// </summary>
internal static class KentarouSlashEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>The white-hot core the blast leaves, where Kentarou's sprite strikes the ground.</summary>
    private static readonly Vector2 Core = new(410, 160);

    /// <summary>The whole effect moved right after it was laid out (owner, 2026-10-15: 20 units).</summary>
    private static readonly Vector2 Shift = new(20, 0);

    /// <summary>The crescents below this height (of 70-150) are left out (owner, 2026-10-15).</summary>
    private const float SmallCrescent = 110;

    /// <summary>Where the shock front stands by frame: out of the core at 3, at the right edge by ~10.</summary>
    private static float FrontAt(float frame) => Lerp(frame, [3, 4, 6, 8, 10, 12], [520, 600, 670, 735, 800, 860]);

    /// <summary>When the white mass dies down at <paramref name="x"/>: the middle first, the right edge last.</summary>
    private static float MassEnd(float x) => Lerp(x, [200, 440, 530, 600, 700, 800], [11, 13.5f, 8, 8.5f, 13, 15.5f]);

    // The colours (light added over the scene): white, then Kentarou's mint and teal (CG 1222's 210/255/245,
    // 160/215/205, 100/175/165).
    private static readonly Color White = new(1, 1, 1), Mint = new(0.82f, 1, 0.96f);
    private static readonly Color Mass = new(1, 1, 0.98f, 0.38f), MassEdge = new(0.6f, 0.88f, 0.84f, 0.38f);
    private static readonly Color Crescent = new(0.55f, 0.84f, 0.8f, 0.9f), CrescentDark = new(0.32f, 0.6f, 0.56f, 0.9f);
    private static readonly Color Column = new(0.22f, 0.42f, 0.4f, 0.28f), Speck = new(0.7f, 0.95f, 0.92f, 0.8f);
    private static readonly Color DustLate = new(0.66f, 0.6f, 0.4f, 0.36f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The reference's spikes fanning back to the left from the core are left out: nothing is drawn behind Kentarou
        // (owner, 2026-10-15).

        // The core's face: a white mass reaching right to a bowed edge at x ~370 (frames 0-4), where the front bursts out.
        for (int i = 0; i < 14; i++)
        {
            float size = Range(110, 160);
            particles.Add(new Particle(EffectTextures.Billow, Range(-1, 0.5f), Range(4, 5.5f), new Vector2(Range(395, 465), Range(30, Height - 30)),
                Vector2.Zero, Range(0, MathF.Tau), 0, new Vector2(size * 0.9f, size), Mass, Held, t => 1 + 0.2f * t));
        }

        // The core: a white glow and billows right of Kentarou (from x 380), swelling right, standing to frame ~11.
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 9, Core + new Vector2(80, 0), Vector2.Zero, 0, 0,
            new Vector2(280, 360), White, t => t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f, t => 1 + 0.3f * t));
        for (int i = 0; i < 26; i++)
        {
            float birth = Range(-0.5f, 5), x = Range(380, 435 + 12 * Math.Max(0, birth)), size = Range(100, 170);
            particles.Add(new Particle(EffectTextures.Billow, birth, Math.Max(3, MassEnd(x) + Range(-1.5f, 1) - birth), new Vector2(x, Range(10, Height - 10)),
                Vector2.Zero, Range(0, MathF.Tau), 0, new Vector2(size, size), Mass, Held, t => 0.7f + 0.5f * t));
        }

        // The needle the front sends ahead (frame 3).
        particles.Add(new Particle(EffectTextures.Spike, 2.5f, 1, new Vector2(575, 155), Vector2.Zero, 0, 0, new Vector2(120, 14),
            new Color(0.75f, 0.78f, 0.78f), _ => 1, _ => 1));

        // The shock front: a pale domed arc running right (frames 3-11).
        for (int frame = 3; frame <= 11; frame++)
        {
            float x = FrontAt(frame), alpha = Lerp(frame, [3, 4, 9, 11], [0.6f, 1, 0.8f, 0.2f]);
            particles.Add(new Particle(EffectTextures.Shock, frame - 0.5f, 1, new Vector2(x - 110, Height / 2), Vector2.Zero, 0, 0,
                new Vector2(250, Lerp(frame, [3, 5], [260, 440])), new Color(0.88f, 0.94f, 0.93f), _ => alpha, _ => 1));
            // Filled behind it while it leaves the core (frames 4-6): the dome white to its rim.
            for (int i = 0; frame <= 6 && i < 7; i++)
            {
                float size = Range(110, 160);
                particles.Add(new Particle(EffectTextures.Billow, frame - 0.5f, 1.5f, new Vector2(x - Range(50, 150), Range(50, Height - 50)), Vector2.Zero,
                    Range(0, MathF.Tau), 0, new Vector2(size, size), White, _ => Lerp(frame, [4, 6], [0.7f, 0.4f]), _ => 1));
            }
        }

        // The mass: white billows filling behind the front as it runs, each dying in its turn.
        for (int i = 0; i < 85; i++)
        {
            float birth = Range(3.5f, 12), front = FrontAt(birth);
            float x = front - 30 - 165 * MathF.Pow((float)random.NextDouble(), 1.2f), size = Range(110, 190);
            particles.Add(new Particle(EffectTextures.Billow, birth, Math.Max(2.5f, MassEnd(x) + Range(-1.5f, 1.5f) - birth),
                new Vector2(x, Range(-10, Height + 10)), new Vector2(4, 0), Range(0, MathF.Tau), Range(-0.02f, 0.02f), new Vector2(size * 1.2f, size),
                Mass, Held, t => 0.85f + 0.3f * t, Ramp: t => Mass.Lerp(MassEdge, Math.Clamp((t - 0.5f) / 0.5f, 0, 1))));
        }

        // The sparks the front throws: thin streaks with a bright head, out from the dome.
        for (int i = 0; i < 70; i++)
        {
            float birth = Range(4, 11), angle = Range(-1.1f, 1.1f);
            var direction = Vector2.FromAngle(angle);
            var at = new Vector2(FrontAt(birth) - 110, Height / 2) + direction * Range(120, 150) * new Vector2(1, 1.6f);
            float length = Range(40, 90);
            particles.Add(new Particle(EffectTextures.Dash, birth, Range(2, 4), at, direction * Range(25, 45), angle, 0, new Vector2(length, 3), Mint, Fade, _ => 1));
            particles.Add(new Particle(EffectTextures.Dot, birth, Range(2, 4), at + direction * length / 2, direction * Range(25, 45), 0, 0, new Vector2(5, 5), White, Fade, _ => 1));
        }

        // The crescents: pale teal sickles, their hollows facing left, flying right through the mass; the large ones
        // alone (owner, 2026-10-15: the small ones left out, the rest of the effect drawn as before).
        for (int i = 0; i < 20; i++)
        {
            float birth = Range(5, 12), height = Range(70, 150);
            var at = new Vector2(Range(470, 570) + (birth - 5) * 7, Range(40, Height - 20));
            var colour = random.NextDouble() < 0.4 ? CrescentDark : Crescent;
            float life = Range(4, 7), speed = Range(12, 20), rotation = Range(-0.15f, 0.15f), width = height * Range(1.2f, 1.6f);
            if (height < SmallCrescent)
                continue;
            particles.Add(new Particle(EffectTextures.Moon, birth, life, at, new Vector2(speed, 0), rotation, 0,
                new Vector2(width, height), colour, t => t < 0.1f ? t / 0.1f : 1 - MathF.Pow((t - 0.1f) / 0.9f, 2), _ => 1));
        }

        // The column: tall bars of teal light standing in the middle (frames 7-16), and a band of fine specks drifting
        // right through it.
        for (int i = 0; i < 16; i++)
        {
            float birth = Range(6.5f, 9.5f), width = Range(40, 90);
            particles.Add(new Particle(EffectTextures.Bar, birth, Range(6, 9), new Vector2(Range(455, 545), Height / 2), Vector2.Zero, MathF.PI / 2, 0,
                new Vector2(Height + 60, width), Column, t => t < 0.15f ? t / 0.15f : 1 - (t - 0.15f) / 0.85f, _ => 1));
        }
        for (int i = 0; i < 260; i++)
        {
            float birth = Range(6, 14), size = Range(1.5f, 3.5f);
            particles.Add(new Particle(EffectTextures.Dot, birth, Range(3, 6), new Vector2(Range(460, 660), 170 + 35 * Normal(random)),
                new Vector2(Range(4, 12), Range(-1, 1)), 0, 0, new Vector2(size, size), Speck, Fade, _ => 1));
        }

        // Nothing lingers behind Kentarou (owner, 2026-10-15): the reference's cluster of motes and its dust on the
        // actor's half are left out. The dust: sand billows over the target's half (frames 9-26).
        for (int i = 0; i < 40; i++)
        {
            float size = Range(130, 220);
            particles.Add(new Particle(EffectTextures.Puff, Range(9, 15), Range(10, 13), new Vector2(Range(590, Width + 40), Range(-10, Height + 10)),
                new Vector2(Range(0.5f, 3), Range(-1, 0.5f)), Range(0, MathF.Tau), 0, new Vector2(size, size), DustLate,
                t => t < 0.25f ? t / 0.25f : 1 - (t - 0.25f) / 0.75f, t => 0.85f + 0.3f * t));
        }
        return particles.Select(p => p with { Position = p.Position + Shift }).ToList();
    }

    /// <summary>A standard normal draw (Box-Muller).</summary>
    private static float Normal(Random random) =>
        MathF.Sqrt(-2 * MathF.Log(1 - (float)random.NextDouble())) * MathF.Cos(MathF.Tau * (float)random.NextDouble());

    /// <summary>In at once, full to three quarters of the life, then out (the white mass).</summary>
    private static float Held(float t) => t < 0.06f ? t / 0.06f : t < 0.75f ? 1 : 1 - (t - 0.75f) / 0.25f;

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;

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
}
