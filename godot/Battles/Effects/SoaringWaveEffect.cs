using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The T2 Soaring Wave effect, ours (no counterpart in Sengoku Rance; the owner's design, 2026-10-15): Rolex's
/// (unit 47) special 7, his blow at the enemy troops and commander (docs/battle/02-resolution.md §3.3), in the blues of
/// his sprite (CG 1252). The four waves his sprite shows standing on the ground right of him light up where they
/// stand (frames 0-2) and roll right toward the enemy, growing a little (to ~1.3×) and trailing spray and pink
/// dust at their feet; each one bursts in white and blue flashes as it runs into the enemy's ranks (~5-12), throwing
/// droplets, and breaks up past them into a fading mist (to ~20). Nothing is drawn behind Rolex. The same seed makes
/// the same effect.
/// </summary>
internal static class SoaringWaveEffect
{
    /// <summary>
    /// The waves as CG 1252 shows them (its pixel bounds, in the design space): the middle of each and its width and
    /// height; the texture's shape fills <see cref="Fill"/> of its square.
    /// </summary>
    private static readonly (Vector2 Centre, Vector2 Size)[] Waves =
    [
        (new(360, 194), new(75, 125)),
        (new(400, 175), new(88, 150)),
        (new(444, 225), new(87, 112)),
        (new(507, 206), new(75, 112)),
    ];

    private const float Fill = 0.875f;

    /// <summary>The waves drawn wider than the sprite's, their feet where its are (owner, 2026-10-15).</summary>
    private const float Wide = 1.3f;

    /// <summary>When the waves set off (they light up from frame 0), how fast they run (units a frame), how long they live.</summary>
    private const float Start = 1.5f, Speed = 30, Life = 13;

    /// <summary>How much a wave grows over its life.</summary>
    private const float Growth = 0.3f;

    /// <summary>Where the enemy's ranks begin and end: the waves burst between them.</summary>
    private const float RanksFrom = 535, RanksTo = 780;

    // The colours (light added over the scene), CG 1252's: foam 215/250/255, light 195/240/255, pale 175/205/235,
    // mid 115/165/195; the dust at the waves' feet 255/230/235 and 215/190/195.
    private static readonly Color Foam = new(0.84f, 0.98f, 1), Light = new(0.76f, 0.94f, 1), Pale = new(0.69f, 0.8f, 0.92f);
    private static readonly Color Mid = new(0.45f, 0.65f, 0.76f);

    /// <summary>The body: the sprite's light blue with less red, so that laid over the sand it stays blue, not white.</summary>
    private static readonly Color Body = new(0.4f, 0.68f, 0.88f);
    private static readonly Color Dust = new(0.84f, 0.75f, 0.76f), DustDark = new(0.4f, 0.32f, 0.33f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        foreach (var (sprite, spriteSize) in Waves)
        {
            var size = spriteSize with { X = spriteSize.X * Wide };
            var centre = sprite - new Vector2(size.X - spriteSize.X, 0) / 2;
            float speed = Speed * Range(0.92f, 1.08f), start = Start + Range(-0.3f, 0.3f);
            // Where the wave stands at a time (frames from the effect's start): its middle and its scale, its foot kept
            // on the ground as it grows.
            float ScaleAt(float frame) => 1 + Growth * Math.Clamp((frame - start) / Life, 0, 1);
            Vector2 MiddleAt(float frame) =>
                centre + new Vector2(speed * Math.Max(0, frame - start), -(ScaleAt(frame) - 1) * size.Y / 2);
            Vector2 FootAt(float frame) => MiddleAt(frame) + new Vector2(size.X, size.Y) * ScaleAt(frame) / 2;

            // The wave lighting up where the sprite's stands (frames 0 to its start), then running: its body, its
            // crest, and two fainter copies just behind it as it moves.
            float end = start + Life;
            Vector2 Move(float t, float birth, float life) => MiddleAt(birth + t * life) - centre;
            float Grow(float t, float birth, float life) => ScaleAt(birth + t * life);
            foreach (var (lag, strength) in new[] { (0f, 1f), (0.6f, 0.4f), (1.2f, 0.18f) })
            {
                float birth = lag == 0 ? -0.5f : start + lag, life = end - birth;
                float Alpha(float t)
                {
                    float frame = birth + t * life;
                    float fadeIn = lag == 0 ? Math.Clamp((frame + 0.5f) / 2, 0, 1) : Math.Clamp((frame - start - lag) / 1.5f, 0, 1);
                    // Breaking up past the ranks: out over the last third of the life.
                    float fadeOut = 1 - Math.Clamp((frame - (end - Life / 3)) / (Life / 3), 0, 1);
                    return strength * fadeIn * fadeOut;
                }
                var shape = size / Fill;
                particles.Add(new Particle(EffectTextures.SurgeBody, birth, life, centre, Vector2.Zero, 0, 0, shape, Body, Alpha, t => Grow(t, birth, life),
                    Move: t => Move(t, birth, life)));
                if (lag == 0)
                {
                    // Its body a second time, in the sprite's deeper blue, to make it less see-through (owner, 2026-10-15).
                    particles.Add(new Particle(EffectTextures.SurgeBody, birth, life, centre, Vector2.Zero, 0, 0, shape, Mid, t => 0.5f * Alpha(t),
                        t => Grow(t, birth, life), Move: t => Move(t, birth, life)));
                    particles.Add(new Particle(EffectTextures.SurgeEdge, birth, life, centre, Vector2.Zero, 0, 0, shape, Foam, Alpha,
                        t => Grow(t, birth, life), Move: t => Move(t, birth, life)));
                    // A glow round it while it lights up.
                    particles.Add(new Particle(EffectTextures.Glow, -0.5f, start + 1.5f, centre + new Vector2(size.X * 0.15f, 0), Vector2.Zero, 0, 0,
                        size * 1.4f, Light, t => 0.5f * (t < 0.3f ? t / 0.3f : 1 - (t - 0.3f) / 0.7f), _ => 1));
                }
            }

            // The spray at its foot: droplets thrown up and back as it runs, falling.
            for (int i = 0; i < 26; i++)
            {
                float birth = Range(start, end - Life / 3), dropSize = Range(2, 5);
                var at = FootAt(birth) + new Vector2(Range(-size.X * 0.5f, 0), Range(-12, 0));
                var velocity = new Vector2(Range(-6, 10), Range(-16, -6));
                particles.Add(new Particle(EffectTextures.Dot, birth, Range(3, 5), at, velocity, 0, 0, new Vector2(dropSize, dropSize), Foam,
                    t => 0.9f * (1 - t), _ => 1, Move: t => new Vector2(0, 55 * t * t)));
            }
            // The dust it kicks up from the ground, as the sprite's at the waves' feet: pink spikes thrown up and back,
            // and a low haze of it left behind.
            for (int i = 0; i < 14; i++)
            {
                float birth = Range(start - 0.5f, end - Life / 2), length = Range(18, 40), angle = Range(-2.6f, -1.7f);
                var at = FootAt(birth) + new Vector2(Range(-size.X * 0.7f, -size.X * 0.05f), Range(-4, 4));
                particles.Add(new Particle(EffectTextures.Spike, birth, Range(2, 3.5f), at, Vector2.FromAngle(angle) * Range(4, 9), angle, 0,
                    new Vector2(length, length / 5), Dust, t => 0.85f * (1 - t), t => 0.6f + 0.6f * t));
            }
            for (int i = 0; i < 6; i++)
            {
                float birth = Range(start, end - Life / 2), puff = Range(24, 40);
                var at = FootAt(birth) + new Vector2(Range(-size.X * 0.6f, -size.X * 0.1f), Range(-6, 2));
                particles.Add(new Particle(EffectTextures.Puff, birth, Range(4, 6), at, new Vector2(Range(1, 3), Range(-1.5f, -0.5f)), Range(0, MathF.Tau), 0,
                    new Vector2(puff * 1.6f, puff), Dust, t => 0.3f * Math.Min(1, t * 6) * (1 - t), t => 1 + 0.4f * t,
                    Ramp: t => Dust.Lerp(DustDark, t)));
            }

            // The bursts: where its front runs into the ranks, two or three flashes up its height, each throwing shards
            // and droplets forward and up.
            for (float x = RanksFrom + Range(0, 40); x < RanksTo; x += Range(55, 80))
            {
                float frame = start;
                while (frame < end && FootAt(frame).X < x)
                    frame += 0.25f;
                if (frame >= end - 1)
                    break;
                var foot = FootAt(frame);
                float height = size.Y * ScaleAt(frame);
                var at = new Vector2(foot.X - 10, foot.Y - Range(0.2f, 0.7f) * height);
                float flash = Range(70, 110);
                particles.Add(new Particle(EffectTextures.Glow, frame, 3, at, Vector2.Zero, 0, 0, new Vector2(flash, flash), Foam,
                    t => 1 - t, t => 1 + 0.4f * t));
                particles.Add(new Particle(EffectTextures.Burst, frame, 2.5f, at, Vector2.Zero, Range(0, MathF.Tau), 0,
                    new Vector2(flash * 1.3f, flash * 1.3f), Light, t => 0.9f * (1 - t), t => 0.7f + 0.6f * t));
                particles.Add(new Particle(EffectTextures.Shock, frame + 0.3f, 2.5f, at + new Vector2(10, 0), new Vector2(10, 0), 0, 0,
                    new Vector2(flash * 0.5f, flash * 1.1f), Pale, t => 0.8f * (1 - t), t => 0.8f + 0.5f * t));
                for (int i = 0; i < 10; i++)
                {
                    float angle = Range(-1.3f, 0.5f), length = Range(18, 40);
                    var direction = Vector2.FromAngle(angle);
                    particles.Add(new Particle(EffectTextures.Dash, frame, Range(2, 3.5f), at + direction * Range(10, 25), direction * Range(14, 26), angle, 0,
                        new Vector2(length, 3), Foam, t => 1 - t, _ => 1));
                }
                for (int i = 0; i < 8; i++)
                {
                    float dropSize = Range(3, 6);
                    particles.Add(new Particle(EffectTextures.Dot, frame + Range(0, 1), Range(3, 5), at, new Vector2(Range(0, 14), Range(-20, -6)), 0, 0,
                        new Vector2(dropSize, dropSize), Light, t => 0.9f * (1 - t), _ => 1, Move: t => new Vector2(0, 70 * t * t)));
                }
            }

            // Its mist when it breaks up past the ranks.
            for (int i = 0; i < 5; i++)
            {
                float birth = end - Life / 3 + Range(0, 3), mist = Range(90, 140);
                var at = MiddleAt(birth) + new Vector2(Range(-20, 20), Range(-30, 30));
                particles.Add(new Particle(EffectTextures.Haze, birth, Range(5, 7), at, new Vector2(Range(2, 5), Range(-2, 0)), 0, 0,
                    new Vector2(mist * 1.3f, mist), Mid, t => 0.25f * Math.Min(1, t * 4) * (1 - t), t => 1 + 0.3f * t));
            }
        }
        return particles;
    }
}
