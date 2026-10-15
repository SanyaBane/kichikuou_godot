using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The T2 Goliath effect, ours after Sengoku Rance's Gigai Punch, 46820 (docs/sengoku/04-screen.md §2.4.1; not traced;
/// the owner's choice, 2026-10-14): the giant's blow at the enemy troops (docs/battle/02-resolution.md §3.3), 15 frames
/// as the reference (owner, 2026-10-15; the rest of ours empty). Four white claws rake down and right over the
/// enemy's half, growing from their roots at the top (frames 0-3) round a white flash; they turn yellow and orange
/// (4-5) and soften into orange strokes crossed by a long slash rising to the right, red by frame 7, dark by 9; a fan
/// of gold rays and sparkles flies from the flash up and to the left (3-11); on the left a beige dust cloud rises
/// from the giant's feet, gone by 12. The same seed makes the same effect.
/// </summary>
internal static class GoliathEffect
{
    /// <summary>The flash's middle, where the claws cross the slash.</summary>
    private static readonly Vector2 Centre = new(555, 185);

    /// <summary>The claws at full length: root and point (the fourth one shorter).</summary>
    private static readonly (Vector2 Root, Vector2 Tip, float Width)[] Claws =
    [
        (new(410, 40), new(572, 314), 46),
        (new(438, 28), new(642, 314), 50),
        (new(466, 18), new(712, 282), 42),
        (new(492, 8), new(682, 214), 28),
    ];

    /// <summary>The claws' share of their length by frame.</summary>
    private static float Reach(float frame) => Lerp(frame, [0, 1, 2, 3], [0.35f, 0.6f, 0.85f, 1]);

    // The colours (light added over the scene).
    private static readonly Color Gold = new(0.9f, 0.72f, 0.42f), Ember = new(0.85f, 0.42f, 0.14f);
    private static readonly Color Dust = new(0.27f, 0.26f, 0.18f), Grain = new(0.43f, 0.42f, 0.29f);

    /// <summary>The soft strokes' colour by frame: orange, red, dark red, gone.</summary>
    private static Color Stroke(float frame) =>
        frame < 5 ? new Color(1, 0.75f, 0.25f).Lerp(new Color(1, 0.6f, 0.15f), Math.Clamp(frame - 4, 0, 1))
        : frame < 7 ? new Color(1, 0.6f, 0.15f).Lerp(new Color(0.85f, 0.2f, 0.04f), (frame - 5) / 2)
        : new Color(0.85f, 0.2f, 0.04f).Lerp(new Color(0.3f, 0.01f, 0), Math.Clamp((frame - 7) / 2, 0, 1));

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();
        void Lit(Texture2D texture, float frame, Vector2 at, float rotation, Vector2 size, Color colour, float alpha) =>
            particles.Add(new Particle(texture, frame - 0.5f, 1, at, Vector2.Zero, rotation, 0, size, colour, _ => alpha, _ => 1));

        // The claws: each frame its own, longer to frame 3, white turning yellow and orange (4-5).
        for (int frame = 0; frame <= 6; frame++)
        {
            var colour = frame <= 3 ? Colors.White : frame == 4 ? new Color(1, 0.95f, 0.78f) : new Color(1, 0.75f, 0.35f);
            float alpha = Lerp(frame, [4, 5, 6], [1, 0.6f, 0.2f]);
            foreach (var (root, tip, width) in Claws)
            {
                var along = tip - root;
                float length = along.Length() * Reach(frame);
                Lit(EffectTextures.Claw, frame, root + along.Normalized() * length / 2, along.Angle(), new Vector2(length, width * Lerp(frame, [0, 1, 2], [0.45f, 0.65f, 1])), colour, alpha);
                // Their roots warm first (frames 2-5): short orange fingers along each.
                if (frame >= 2)
                    Lit(EffectTextures.Streak, frame, root + along.Normalized() * 45, along.Angle(), new Vector2(110, 12), new Color(1, 0.55f, 0.15f), 0.8f);
            }
        }

        // The flash: a starburst at frame 1, a white mass to frame 4, a pale yellow core at 5.
        for (int frame = 1; frame <= 6; frame++)
        {
            float size = Lerp(frame, [1, 2, 3, 4, 5, 6], [170, 250, 290, 280, 200, 120]);
            var colour = frame <= 4 ? Colors.White : new Color(1, 0.9f, 0.55f);
            float alpha = Lerp(frame, [4, 5, 6], [1, 0.8f, 0.35f]);
            Lit(EffectTextures.Glow, frame, Centre + new Vector2(Range(-8, 8), Range(-8, 8)), 0, new Vector2(size, size), colour, alpha);
            if (frame is >= 2 and <= 4)
                Lit(EffectTextures.Billow, frame, Centre + new Vector2(-12, 5), Range(0, MathF.Tau), new Vector2(size * 0.7f, size * 0.62f), Colors.White, 0.9f);
            if (frame <= 4)
                Lit(EffectTextures.Rays, frame, Centre, Range(0, MathF.Tau), new Vector2(size, size), Colors.White, 0.45f);
        }

        // The soft strokes: the slash rising to the right across the claws (frames 4-9), a step redder than the claws
        // blurred (5-9).
        for (int frame = 4; frame <= 9; frame++)
        {
            float alpha = Lerp(frame, [4, 5, 7, 8, 9], [0.55f, 1, 0.85f, 0.4f, 0.1f]);
            if (frame >= 5)
            {
                foreach (var (root, tip, width) in Claws)
                {
                    var along = tip - root;
                    Lit(EffectTextures.Petal, frame, (root + tip) / 2 + along.Normalized() * 20, along.Angle(), new Vector2(along.Length() * 0.95f, width), Stroke(frame - 0.5f), alpha * 0.9f);
                }
            }
            Lit(EffectTextures.Petal, frame, Centre + new Vector2(0, -15), -0.75f, new Vector2(470, 100), Stroke(frame + 1), alpha);
            Lit(EffectTextures.Petal, frame, Centre + new Vector2(95, -10), -0.75f, new Vector2(260, 60), Stroke(frame + 1), alpha * 0.8f);
        }

        // The fan: gold rays thrown from the flash up and to the left, cooling to embers, with sparkles among them.
        for (int frame = 3; frame <= 9; frame++)
        {
            int rays = (int)Lerp(frame, [3, 4, 6, 8, 9], [10, 22, 26, 10, 3]);
            float late = (frame - 5) / 4f;
            for (int i = 0; i < rays; i++)
            {
                float angle = Mathf.DegToRad(Range(188, 288)), distance = Range(40, 200), speed = Range(28, 55), life = Range(2, 4);
                var direction = Vector2.FromAngle(angle);
                float length = Range(50, 150);
                particles.Add(new Particle(EffectTextures.Streak, frame - 0.5f, life, Centre + direction * distance, direction * speed, angle, 0,
                    new Vector2(length, length / 14), Gold, t => 0.85f * (1 - t * t), _ => 1, t => Gold.Lerp(Ember, Math.Clamp(t * 1.5f + late, 0, 1))));
            }
            for (int i = 0, n = (int)Lerp(frame, [3, 5, 8, 9], [8, 20, 12, 4]); i < n; i++)
            {
                float angle = Mathf.DegToRad(Range(150, 290)), distance = Range(60, 260), size = Range(16, 34);
                var direction = Vector2.FromAngle(angle);
                particles.Add(new Particle(EffectTextures.Sparkle, frame - 0.5f, Range(1, 2), Centre + direction * distance, direction * 20, Range(-0.3f, 0.3f), 0,
                    new Vector2(size, size), new Color(1, 0.88f, 0.6f), _ => 0.9f, _ => 1));
            }
        }

        // The embers: short orange streaks strewn to the left of the flash, drifting out (frames 6-12).
        for (int frame = 6; frame <= 9; frame++)
        {
            for (int i = 0; i < 9; i++)
            {
                float angle = Mathf.DegToRad(Range(160, 240)), length = Range(25, 60);
                var direction = Vector2.FromAngle(angle);
                particles.Add(new Particle(EffectTextures.Streak, frame - 0.5f, Range(3, 5), Centre + direction * Range(110, 280), direction * Range(12, 26), angle, 0,
                    new Vector2(length, length / 10), Ember, t => 0.85f * (1 - t), _ => 1));
            }
        }

        // The dust on the left: puffs rising from the bottom and drifting up and left, swelling, gone by frame 12.
        for (int i = 0; i < 10; i++)
        {
            var at = new Vector2(Range(100, 220), Range(160, 300));
            float size = Range(55, 95);
            particles.Add(new Particle(EffectTextures.Puff, -1.5f + 0.25f * i, Range(11, 12.5f), at, new Vector2(Range(-11, -3), Range(-12, -5)), Range(0, MathF.Tau), 0,
                new Vector2(size, size), Dust, t => 0.55f * Math.Min(1, t * 8) * (1 - t * t), t => 1 + 0.5f * t));
        }
        // And its grain at the bottom left.
        for (int i = 0; i < 50; i++)
        {
            float size = Range(2, 4);
            particles.Add(new Particle(EffectTextures.Dot, Range(0, 6), Range(3, 6), new Vector2(Range(20, 160), Range(220, 312)), new Vector2(Range(-3, 1), Range(-3, 0)), 0, 0,
                new Vector2(size, size), Grain, t => 0.8f * (1 - t), _ => 1));
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
}
