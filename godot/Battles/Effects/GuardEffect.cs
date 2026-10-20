using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The Guard effect, ours after Sengoku Rance's guard actions' effect, 45080 (docs/sengoku/04-screen.md §2.4.1; not
/// traced): a unit's Defend (docs/battle/01-flow.md §8.3), 14 frames as the reference (the rest of ours empty), all on
/// the actor's half. A curtain of horizontal blue-violet streaks sweeps in from the left edge (frames 0-3) and stops at
/// the middle, where its front stands as a wall of light, lavender turning white and pink toward the foot, its tail
/// trailing back to the left (4-9); the wall flares (10-11) and breaks up into a mosaic of tiles spreading back over the
/// actor's ranks, blue and then dark (11-13), gone by 14. The wall's face is a straight edge at <see cref="Face"/>. The
/// same seed makes the same effect.
/// </summary>
internal static class GuardEffect
{
    /// <summary>Where the wall stands: its face, the curtain's front from frame 3 on.</summary>
    public const float Face = 400;

    /// <summary>The curtain's front by frame: from the left edge to the face in three frames.</summary>
    private static float Front(float frame) => Lerp(frame, [0, 1, 2, 3], [150, 240, 330, Face]);

    /// <summary>How far the curtain trails behind its front by frame.</summary>
    private static float Length(float frame) => Lerp(frame, [0, 3, 6, 9, 10, 12], [140, 190, 230, 260, 280, 240]);

    // The colours (light added over the scene): the streaks' blue-violet, their bright cores, the wall's lavender and the
    // pinkish white at its foot.
    private static readonly Color Blue = new(0.36f, 0.36f, 0.86f), Core = new(0.78f, 0.76f, 1);
    private static readonly Color Lavender = new(0.55f, 0.5f, 1), Pink = new(1, 0.85f, 1);

    /// <summary>The mosaic's first blocks: bluer than the wall.</summary>
    private static readonly Color Mosaic = new(0.44f, 0.41f, 1);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        var particles = new List<Particle>();
        void Lit(Texture2D texture, float frame, Vector2 at, float rotation, Vector2 size, Color colour, float alpha) =>
            particles.Add(new Particle(texture, frame - 0.5f, 1, at, Vector2.Zero, rotation, 0, size, colour, _ => alpha, _ => 1));

        // The streaks' rows, the same from frame to frame: a height, a thickness, a brightness, some with a bright core.
        var rows = Enumerable.Range(0, 90)
            .Select(_ => (Y: Range(2, 312), Thick: Range(5, 16), Bright: Range(0.35f, 1), Core: random.NextDouble() < 0.18))
            .ToList();

        // The curtain: each frame its rows lit anew, the length of each a little uneven, flickering, dimming once it has
        // stopped and as the wall breaks up (10-12).
        for (int frame = 0; frame <= 12; frame++)
        {
            float front = Front(frame), length = Length(frame);
            float fade = Lerp(frame, [0, 3, 6, 9, 10, 11, 12], [1, 1, 0.8f, 0.65f, 0.45f, 0.25f, 0.1f]);
            foreach (var row in rows)
            {
                float len = length * Range(0.75f, 1.05f);
                // The streak's bright part lies past its middle (EffectTextures.Streak): its right end at the front.
                var at = new Vector2(front - len * 0.5f, row.Y + Range(-1, 1));
                float alpha = fade * row.Bright * Range(0.75f, 1);
                Lit(EffectTextures.Streak, frame, at, 0, new Vector2(len, row.Thick), Blue, alpha);
                if (row.Core)
                    Lit(EffectTextures.Streak, frame, at + new Vector2(len * 0.12f, 0), 0, new Vector2(len * 0.6f, row.Thick * 0.45f), Core, alpha * 0.7f);
            }
            // A faint blue body behind the rows.
            Lit(EffectTextures.Haze, frame, new Vector2(front - length * 0.3f, 157), 0, new Vector2(length * 1.1f, 420), Blue, 0.22f * fade);
        }

        // The wall: a band of light along the face (frames 3-10), widening and brightening, lavender turning a pinkish
        // white toward its foot, in blocks stacked down the face from frame 5, a faint straight edge at the face.
        for (int frame = 3; frame <= 10; frame++)
        {
            float width = Lerp(frame, [3, 5, 9, 10], [16, 34, 56, 70]);
            float alpha = Lerp(frame, [3, 5, 9, 10], [0.4f, 0.65f, 0.9f, 0.95f]);
            Lit(EffectTextures.Bar, frame, new Vector2(Face - width * 0.45f, 157), MathF.PI / 2, new Vector2(360, width * 1.4f), Lavender, alpha * 0.6f);
            Lit(EffectTextures.Glow, frame, new Vector2(Face - width * 0.4f, 250), 0, new Vector2(width * 1.4f, 170), Pink, alpha * Lerp(frame, [3, 6, 9, 10], [0.15f, 0.4f, 0.5f, 0.35f]));
            Lit(EffectTextures.Tile, frame, new Vector2(Face - 2, 157), 0, new Vector2(4, 330), Core, alpha * 0.3f);
            if (frame < 5)
                continue;
            for (float y = Range(-30, 0); y < 314;)
            {
                float w = width * Range(0.55f, 1), h = Range(40, 80);
                float low = Math.Clamp((y + h / 2) / 314, 0, 1);
                Lit(EffectTextures.Tile, frame, new Vector2(Face - w / 2, y + h / 2), 0, new Vector2(w, h * 1.04f), Lavender.Lerp(Pink, low * low * 0.8f),
                    alpha * Range(0.55f, 0.8f));
                y += h;
            }
        }

        // The mosaic: the wall breaking up, pixelated (frames 10-13) — a grid of blocks from the face back to the left, each
        // row's left end a little uneven, the blocks growing coarser as they spread over the actor's ranks; brighter toward
        // the face and the foot, lavender (pink at the foot near the face), then blue, then dark. No block passes the face.
        for (int frame = 10; frame <= 13; frame++)
        {
            float left = Lerp(frame, [10, 11, 12, 13], [310, 200, 70, 0]);
            float cell = Lerp(frame, [10, 11, 12, 13], [26, 40, 54, 66]);
            float alpha = Lerp(frame, [10, 11, 12, 13], [0.85f, 1, 1, 0.6f]);
            float even = Lerp(frame, [11, 12], [0.45f, 0.65f]);            // the blocks' share lit whatever their place
            var colour = frame <= 11 ? Mosaic : frame == 12 ? new Color(0.32f, 0.3f, 0.92f) : new Color(0.17f, 0.14f, 0.55f);
            for (float y = Range(-cell, 0); y < 314; y += cell * 1.2f)
            {
                float rowLeft = left + Range(-40, 50);
                for (float x = Face - cell, w = cell; x + w > rowLeft; w = cell * Range(0.8f, 1.6f), x -= w)
                {
                    float near = Math.Clamp((x + w / 2 - rowLeft) / Math.Max(1, Face - rowLeft), 0, 1);
                    float low = Math.Clamp((y + cell * 0.6f) / 314, 0, 1);
                    var tint = frame <= 11 ? colour.Lerp(Pink, near * near * low * 0.7f) : colour;
                    float a = alpha * (even + (1 - even) * MathF.Pow(near, 0.7f)) * (0.75f + 0.25f * low) * Range(0.8f, 1.05f);
                    Lit(EffectTextures.Tile, frame, new Vector2(x + w / 2, y + cell * 0.6f), 0, new Vector2(w * 1.04f, cell * 1.25f), tint, Math.Min(a, 1));
                }
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
