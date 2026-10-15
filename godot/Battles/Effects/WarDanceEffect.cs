using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The C. War Dance effect, ours after Sengoku Rance's Ieyasu's Special Move, 47400 (docs/sengoku/04-screen.md §2.4.1;
/// not traced; the owner's choice, 2026-10-14): Patton's flurry of blows at the enemy troops (docs/battle/02-resolution.md
/// §3.3). Rows of flat crescents bulging right, each lit for one frame, a new set every frame — salmon over the middle,
/// grey past it and violet toward the right edge, white where they pile up — their front running right from the
/// middle to the edge (frames 2-7), thinning from frame ~10 and drawing back to the right (19-25); behind them faint
/// streaky wisps drifting right, the last fading by ~27. The same seed makes the same effect.
/// </summary>
internal static class WarDanceEffect
{
    private const float Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>The crescents lit at each frame.</summary>
    private static readonly float[] Counts =
        [0, 0, 1.5f, 9, 18, 24, 30, 34, 40, 40, 36, 32, 30, 28, 24, 17, 15, 13, 12, 11, 9, 7, 5, 3, 2, 1];

    /// <summary>The crescents' left edge at a frame: from the middle, drawing back to the right at the end.</summary>
    private static float From(float frame) => Lerp(frame, [2, 3, 14, 18, 19, 20, 21, 22, 23, 24, 25], [300, 290, 300, 330, 370, 420, 480, 520, 560, 600, 700]);

    /// <summary>The crescents' front at a frame: running right from the middle to the edge.</summary>
    private static float To(float frame) => Lerp(frame, [2, 3, 4, 5, 6, 7, 8], [390, 560, 600, 640, 700, 780, 830]);

    /// <summary>How the crescents crowd between their edges at a frame: above 1 toward the left, below 1 toward the right.</summary>
    private static float Bias(float frame) => Lerp(frame, [6, 10, 16, 20], [1.4f, 0.9f, 0.85f, 1]);

    /// <summary>The crescents per count, the wisps per crescent and their strength.</summary>
    private const float Density = 1.8f, WispShare = 1f, WispAlpha = 0.75f;

    // The colours (light added over the scene): a crescent alone; two make the next shade, three white.
    private static readonly Color Salmon = new(0.48f, 0.29f, 0.29f), Grey = new(0.37f, 0.29f, 0.29f);
    private static readonly Color Violet = new(0.32f, 0.22f, 0.3f), Plum = new(0.29f, 0.14f, 0.29f);
    private static readonly Color Wisp = new(0.5f, 0.3f, 0.3f), Ash = new(0.42f, 0.3f, 0.36f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();

        // The colour of a crescent at x and frame: salmon to ~430, grey to ~540, then violet, turning plum late.
        Color Colour(float x, float frame)
        {
            var violet = Grey.Lerp(Violet, Math.Clamp((frame - 5) / 4, 0, 1)).Lerp(Plum, Math.Clamp((frame - 14) / 8, 0, 1));
            return x < 430 ? Salmon : x < 540 ? Salmon.Lerp(Grey, (x - 430) / 110) : x < 630 ? Grey.Lerp(violet, (x - 540) / 90) : violet;
        }

        // The crescents: each lit for one frame, centred on the frame, a new set every frame.
        for (int frame = 0; frame < Counts.Length; frame++)
        {
            float count = Counts[frame], from = From(frame), to = To(frame);
            int n = (int)(Density * count) + (random.NextDouble() < Density * count % 1 ? 1 : 0);
            for (int i = 0; i < n; i++)
            {
                // Larger and more often wide toward the right.
                float x = from + (to - from) * MathF.Pow((float)random.NextDouble(), Bias(frame)), right = Math.Clamp((x - 480) / 250, 0, 1);
                float height = (36 + 150 * MathF.Pow((float)random.NextDouble(), 1.5f)) * (1 + 0.25f * right);
                float width = random.NextDouble() < 0.2 + 0.25 * right ? height * Range(1.4f, 2.4f) : height * Range(0.55f, 0.9f);
                var at = new Vector2(Math.Min(x, to - 0.3f * width), Range(-20, Height + 20));
                particles.Add(new Particle(EffectTextures.Sickle, frame - 0.5f, 1, at, new Vector2(Range(4, 12), 0), Range(-0.2f, 0.2f), 0,
                    new Vector2(width, height), Colour(at.X, frame), _ => 1, _ => 1));
            }

            // Behind them the wisps: faint streaky smears drifting right, a few frames each.
            int wisps = (int)(count * WispShare);
            for (int i = 0; i < wisps; i++)
            {
                float length = Range(110, 200), life = Range(2.5f, 4.5f);
                var at = new Vector2(Range(from - 90, Math.Max(from, (from + to) / 2)), Range(10, Height - 10));
                var colour = frame < 19 ? Wisp : Ash;
                particles.Add(new Particle(EffectTextures.Puff, frame - 0.5f, life, at, new Vector2(Range(6, 14), Range(-1, 1)), Range(-0.08f, 0.08f), 0,
                    new Vector2(length, Range(12, 22)), colour, t => WispAlpha * (t < 0.2f ? t / 0.2f : 1 - (t - 0.2f) / 0.8f), _ => 1));
            }
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
