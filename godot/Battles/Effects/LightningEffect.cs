using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The lightning magic (docs/ui/mage-lines.md, "The plan: magic effects by element"), ours after Sengoku Rance's
/// Thunderstrike, 40630, in blue-white where its own is green-white (owner, 2026-10-21; docs/sengoku/04-screen.md §2.4.1,
/// "The magic's elements"; not traced). On the target's half: a bolt striking down on the target with jagged branches
/// spread over the sky above it (frame 0), flickering anew each frame; a white flash at its foot with rays fanning out
/// toward the actor (1-5); a glowing ellipse on the ground, filled at first, spreading as a ring (6-16) while thin
/// strands of the bolt linger; star sparkles and soft specks scattered over the half and a little beyond, fading by
/// frame ~27. The same seed makes the same effect.
/// </summary>
internal static class LightningEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>Where the bolt strikes, on the ground at the target; the flash and the ring are about it.</summary>
    private static readonly Vector2 Strike = new(605, 235);

    // The colours (light added over the scene): white, electric blue, a paler blue, a faint haze.
    private static readonly Color White = new(0.95f, 0.98f, 1f), Blue = new(0.45f, 0.68f, 1f);
    private static readonly Color Pale = new(0.72f, 0.85f, 1f), Haze = new(0.35f, 0.55f, 1f, 0.3f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // A jagged path from one point to another: the straight line split in halves again and again, each midpoint
        // pushed aside by a share of its half's length.
        List<Vector2> Path(Vector2 from, Vector2 to, float jag, int depth)
        {
            var points = new List<Vector2> { from, to };
            for (int level = 0; level < depth; level++)
            {
                var next = new List<Vector2> { points[0] };
                for (int i = 1; i < points.Count; i++)
                {
                    var a = points[i - 1];
                    var b = points[i];
                    var side = (b - a).Orthogonal().Normalized();
                    next.Add((a + b) / 2 + side * Range(-jag, jag) * (b - a).Length());
                    next.Add(b);
                }
                points = next;
            }
            return points;
        }
        // A bolt drawn along a path for a frame or so: a white core over a wider blue glow, each a bar per segment.
        void Bolt(List<Vector2> path, float birth, float life, float core, float alpha)
        {
            for (int i = 1; i < path.Count; i++)
            {
                var a = path[i - 1];
                var b = path[i];
                var at = (a + b) / 2;
                float angle = (b - a).Angle(), length = (b - a).Length() * 1.15f + 2;
                particles.Add(new Particle(EffectTextures.Bar, birth, life, at, Vector2.Zero, angle, 0,
                    new Vector2(length, core * 14), Blue * new Color(1, 1, 1, 0.55f * alpha), Flicker, _ => 1));
                particles.Add(new Particle(EffectTextures.Bar, birth, life, at, Vector2.Zero, angle, 0,
                    new Vector2(length, core * 4), White * new Color(1, 1, 1, alpha), Flicker, _ => 1));
            }
        }

        // The bolt: a trunk from the sky to the target and branches from all over the sky above it, joining the trunk on
        // its way down; struck anew each frame, thinning from frame ~5 (0-9).
        for (int frame = 0; frame < 10; frame++)
        {
            float birth = frame - 0.5f, strength = frame < 7 ? 1 : 1 - (frame - 6) / 5f;
            var top = new Vector2(Strike.X + Range(-12, 12), -10);
            var trunk = Path(top, Strike + new Vector2(0, 80), 0.08f, 5);
            Bolt(trunk, birth, 1.4f, 2.2f * strength, strength);
            particles.Add(new Particle(EffectTextures.Bar, birth, 1.4f, new Vector2(Strike.X, (Strike.Y - 10) / 2), Vector2.Zero,
                MathF.PI / 2, 0, new Vector2(Strike.Y + 40, 120), Blue * new Color(1, 1, 1, 0.4f * strength), Flicker, _ => 1));
            int branches = frame < 7 ? 11 : 5;
            for (int i = 0; i < branches; i++)
            {
                var from = new Vector2(Range(420, Width + 20), Range(-10, 120));
                var join = trunk[random.Next(trunk.Count / 4, trunk.Count * 3 / 4)];
                Bolt(Path(from, join, 0.3f, 5), birth, 1.4f, Range(0.5f, 1f) * strength, 0.8f * strength);
            }
        }
        // The strands lingering in the ring: thin straight-ish threads down the bolt's path (9-16).
        for (int frame = 9; frame < 17; frame += 2)
        {
            float strength = 1 - (frame - 9) / 9f;
            for (int i = 0; i < 3; i++)
            {
                var top = new Vector2(Strike.X + Range(-25, 30), -10);
                Bolt(Path(top, Strike + new Vector2(Range(-30, 30), Range(30, 80)), 0.04f, 4), frame, 2.2f, 0.45f, 0.6f * strength);
            }
        }

        // The flash: a white blaze at the bolt's foot, lumpy at its edge, with a broad blue haze and rays fanning out (1-5).
        particles.Add(new Particle(EffectTextures.Haze, 0.5f, 6, Strike, Vector2.Zero, 0, 0, new Vector2(1300, 420), Haze,
            Swell, t => 1 + 0.2f * t));
        particles.Add(new Particle(EffectTextures.Rays, 0.6f, 5.5f, Strike, Vector2.Zero, 0, 0, new Vector2(1500, 380),
            Pale * new Color(1, 1, 1, 0.75f), Swell, t => 1 + 0.15f * t));
        particles.Add(new Particle(EffectTextures.Rays, 0.8f, 5, Strike, Vector2.Zero, 0.05f, 0, new Vector2(1100, 260),
            White * new Color(1, 1, 1, 0.5f), Swell, t => 1 + 0.15f * t));
        particles.Add(new Particle(EffectTextures.Glow, 0.5f, 5.5f, Strike + new Vector2(0, -5), Vector2.Zero, 0, 0,
            new Vector2(560, 330), White, Swell, t => 0.8f + 0.3f * t));
        for (int i = 0; i < 14; i++)
        {
            float birth = Range(0.6f, 2.5f), life = Range(2.5f, 4);
            var at = Strike + new Vector2(Range(-170, 170), Range(-80, 70));
            float size = Range(80, 160);
            particles.Add(new Particle(EffectTextures.Billow, birth, life, at, Vector2.Zero, Range(0, MathF.Tau), 0,
                new Vector2(size * 1.3f, size), White * new Color(1, 1, 1, 0.8f), Swell, t => 1 + 0.25f * t));
        }

        // The ring: a glowing ellipse on the ground about the strike, filled and bright at first (6-9), spreading as a
        // ring with a bright rim (9-16), fading as it spreads.
        var ringAt = Strike + new Vector2(-5, -10);
        particles.Add(new Particle(EffectTextures.Glow, 4.5f, 6.5f, ringAt, Vector2.Zero, 0, 0, new Vector2(320, 165),
            White * new Color(1, 1, 1, 0.9f), Swell, t => 1 + 0.6f * t));
        particles.Add(new Particle(EffectTextures.Bokeh, 4.8f, 5.5f, ringAt, Vector2.Zero, 0, 0, new Vector2(250, 125),
            White * new Color(1, 1, 1, 0.8f), Swell, t => 1 + 0.35f * t));
        particles.Add(new Particle(EffectTextures.Sphere, 5.5f, 12, ringAt, Vector2.Zero, 0, 0, new Vector2(250, 120),
            new Color(0.8f, 0.92f, 1f), RingFade, t => 1 + 1.4f * MathF.Sqrt(t)));
        particles.Add(new Particle(EffectTextures.Haze, 6, 11, ringAt, Vector2.Zero, 0, 0, new Vector2(250, 120),
            Blue * new Color(1, 1, 1, 0.25f), RingFade, t => 1 + 1.4f * MathF.Sqrt(t)));

        // The sparkles: star glints scattered over the target's half and thinly beyond it, twinkling as they fade
        // (2-27), drifting out from the strike.
        for (int i = 0; i < 230; i++)
        {
            float birth = 2 + 12 * MathF.Pow((float)random.NextDouble(), 1.4f), life = Range(8, 16);
            var at = new Vector2(Width - 470 * MathF.Pow((float)random.NextDouble(), 1.1f), Range(0, Height));
            if (random.NextDouble() < 0.15)
                at.X = Range(0, 420);
            var drift = (at - Strike).Normalized() * Range(0.5f, 2f);
            float size = Range(16, 44);
            var colour = random.NextDouble() < 0.5 ? White : Pale;
            particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, drift, Range(-0.3f, 0.3f), 0,
                new Vector2(size, size), colour, Twinkle(Range(0, MathF.Tau)), t => 1 - 0.4f * t));
            if (random.NextDouble() < 0.5)
                particles.Add(new Particle(EffectTextures.Sparkle, birth, life, at, drift, MathF.PI / 4, 0,
                    new Vector2(size, size) * 0.6f, colour * new Color(1, 1, 1, 0.6f), Twinkle(Range(0, MathF.Tau)), t => 1 - 0.4f * t));
        }
        // The specks: soft blue blobs among them, thickest about the ring (4-26).
        for (int i = 0; i < 230; i++)
        {
            float birth = Range(3, 15), life = Range(6, 12);
            var at = new Vector2(Width - 460 * MathF.Pow((float)random.NextDouble(), 0.9f), Range(0, Height));
            var drift = (at - Strike).Normalized() * Range(0.3f, 1.5f);
            float size = Range(22, 55);
            particles.Add(new Particle(EffectTextures.Glow, birth, life, at, drift, 0, 0, new Vector2(size, size),
                Pale * new Color(1, 1, 1, 0.8f), Swell, _ => 1));
        }
        return particles;
    }

    /// <summary>A bolt's flicker: at once, held, then out quickly.</summary>
    private static float Flicker(float t) => t < 0.6f ? 1 : 1 - (t - 0.6f) / 0.4f;

    /// <summary>The ring: in quickly, full for a third of its life, then fading slowly.</summary>
    private static float RingFade(float t) => t < 0.08f ? t / 0.08f : t < 0.3f ? 1 : MathF.Pow(1 - (t - 0.3f) / 0.7f, 1.3f);

    /// <summary>A glint's twinkle: its fade by age, flickering with a phase of its own.</summary>
    private static Func<float, float> Twinkle(float phase) =>
        t => Swell(t) * (0.65f + 0.35f * MathF.Sin(phase + t * 25));

    /// <summary>In quickly, full to the middle of its life, then out.</summary>
    private static float Swell(float t) => t < 0.15f ? t / 0.15f : t < 0.55f ? 1 : MathF.Pow(1 - (t - 0.55f) / 0.45f, 1.4f);
}
