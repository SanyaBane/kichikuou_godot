using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The bow effect, ours after Sengoku Rance's Archer effect 40260 (docs/sengoku/04-screen.md §2.4.1; not traced): on
/// the actor's half (left) a volley of white arrows leaving up and to the right from a line of release bursts (frames
/// 0-6), the bows' arcs lingering (to ~21) and a second, level volley (frames 9-14); on the target's half gold arrows
/// streaking in level (frames 2-17), cream arrows falling down to the right (frames 8-28), gold "crowns" where they
/// hit, flat teal streaks and a cloud of bokeh that fades by frame ~27. No dust. The same seed makes the same effect.
/// </summary>
internal static class BowEffect
{
    private const float Half = EffectCanvas.DesignWidth / 2, Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    // The colours (light added over the scene).
    private static readonly Color WhiteArrow = new(0.92f, 0.9f, 0.78f), GoldArrow = new(1f, 0.83f, 0.28f);
    private static readonly Color FallingArrow = new(0.93f, 0.89f, 0.7f), Release = new(0.95f, 0.92f, 0.78f, 0.85f);
    private static readonly Color Bow = new(0.95f, 0.85f, 0.45f), Crown = new(0.85f, 0.62f, 0.14f);
    private static readonly Color Streak = new(0.35f, 0.6f, 0.66f, 0.8f), Bokeh = new(0.55f, 0.5f, 0.38f, 0.3f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        int Count(float mean) => (int)MathF.Floor(mean + (float)random.NextDouble());
        var particles = new List<Particle>();

        // An arrow flying at an angle (radians, 0 to the right) and a speed (design pixels a frame), its head first.
        void Arrow(float birth, float life, Vector2 at, float angle, float speed, float length, Color colour) =>
            particles.Add(new Particle(EffectTextures.Arrow, birth, life, at, Vector2.FromAngle(angle) * speed, angle, 0,
                new Vector2(length, length / 6), colour, Steady, _ => 1));

        // The first volley: release bursts along a line running down to the left, white arrows leaving up and right.
        for (int k = 0; k <= 6; k++)
        {
            var line = new Vector2(230, 80).Lerp(new Vector2(70, 250), k / 6f);
            for (int n = Count(k < 3 ? 5 : 3.5f); n > 0; n--)
            {
                var at = line + new Vector2(Range(-40, 40), Range(-30, 30));
                float birth = k - Range(0, 0.8f);
                particles.Add(new Particle(EffectTextures.Burst, birth, Range(6, 11), at, new Vector2(Range(-1, 0), Range(0.5f, 1.5f)), Range(0, MathF.Tau), 0,
                    new Vector2(1, 1) * Range(40, 64), Release, Linger, t => 1 + 0.2f * t));
                Arrow(birth, 9, at + new Vector2(20, -8), Range(-0.55f, -0.3f), Range(70, 110), Range(80, 130), WhiteArrow);
            }
        }

        // The bows: arcs that stand after the volley and fade, a short white glint beside each.
        for (int k = 3; k <= 9; k++)
        {
            for (int n = Count(1.5f); n > 0; n--)
            {
                var at = new Vector2(Range(30, 230), Range(110, 290));
                float birth = k - Range(0, 1), size = Range(70, 110);
                particles.Add(new Particle(EffectTextures.BowArc, birth, Range(9, 16), at, new Vector2(0, Range(-0.3f, 0.3f)),
                    Range(-0.35f, 0.15f), 0, new Vector2(size * 0.6f, size), Bow, Linger, _ => 1));
                particles.Add(new Particle(EffectTextures.Dash, birth, 2, at + new Vector2(size * 0.15f, Range(-8, 8)), new Vector2(6, 0), 0, 0,
                    new Vector2(Range(16, 26), 4), WhiteArrow, Blink, _ => 1));
            }
        }

        // The second volley: level white arrows leaving the actor's half to the right.
        for (int k = 9; k <= 14; k++)
        {
            for (int n = Count(2.5f); n > 0; n--)
                Arrow(k - Range(0, 1), 8, new Vector2(Range(60, 260), Range(190, 275)), Range(-0.05f, 0.05f), Range(55, 75),
                    Range(80, 120), WhiteArrow);
        }

        // The gold arrows over the target's half: level, high at first, lower later.
        for (int k = 2; k <= 17; k++)
        {
            for (int n = Count(k < 9 ? 2.5f : 1.5f); n > 0; n--)
            {
                float y = k < 9 ? Range(15, 150) : Range(140, 295);
                Arrow(k - Range(0, 1), 7, new Vector2(Range(Half - 20, Half + 140), y), Range(-0.1f, 0f), Range(60, 90),
                    Range(85, 125), GoldArrow);
            }
        }

        // The falling arrows: in from above, down and to the right, to the end.
        for (int k = 8; k <= 26; k++)
        {
            for (int n = Count(k <= 22 ? 1f : 0.5f); n > 0; n--)
            {
                float angle = Range(0.75f, 1f);
                var at = new Vector2(Range(Half + 60, Width - 40), Range(-40, 120));
                Arrow(k - Range(0, 1), 10, at, angle, Range(22, 34), Range(85, 120), FallingArrow);
            }
        }

        // The hits: gold crowns flaring and fading, flat teal streaks.
        for (int k = 6; k <= 21; k++)
        {
            for (int n = Count(1.8f); n > 0; n--)
            {
                float size = Range(32, 70);
                particles.Add(new Particle(EffectTextures.Splat, k - Range(0, 1), Range(4, 8),
                    new Vector2(Range(Half + 50, Width - 10), Range(110, Height - 10)), Vector2.Zero, Range(-0.3f, 0.3f), 0,
                    new Vector2(size, size), Crown, Swell, t => 1 + 0.2f * t));
            }
            for (int n = Count(1.2f); n > 0; n--)
            {
                float length = Range(40, 75);
                particles.Add(new Particle(EffectTextures.CrescentBody, k - Range(0, 1), Range(6, 10),
                    new Vector2(Range(Half + 40, Width), Range(120, Height)), new Vector2(Range(0.5f, 1.5f), 0),
                    -MathF.PI / 2 + Range(-0.15f, 0.15f), 0, new Vector2(length * 0.2f, length), Streak, Swell, _ => 1));
            }
        }

        // The bokeh: soft discs over the target's half from frame 9, fading slowly to frame ~27.
        for (int i = 0; i < 90; i++)
        {
            float birth = Range(8.5f, 16), size = Range(12, 34);
            particles.Add(new Particle(EffectTextures.Bokeh, birth, 27.5f - birth,
                new Vector2(Range(Half + 30, Width), Range(40, Height)), new Vector2(Range(-0.2f, 0.2f), Range(-0.5f, 0)),
                0, 0, new Vector2(size, size), Bokeh, Linger, t => 1 + 0.15f * t));
        }
        return particles;
    }

    /// <summary>Full for its whole life (an arrow, gone when it leaves).</summary>
    private static float Steady(float t) => t < 0.03f ? t / 0.03f : 1;

    /// <summary>In at once, then out slowly (a bow, the bokeh).</summary>
    private static float Linger(float t) => t < 0.1f ? t / 0.1f : 1 - (t - 0.1f) / 0.9f;

    /// <summary>In over the first sixth, then out (a hit).</summary>
    private static float Swell(float t) => t < 0.15f ? t / 0.15f : 1 - MathF.Pow((t - 0.15f) / 0.85f, 2);

    /// <summary>Bright at once, fading straight (a release, a glint).</summary>
    private static float Blink(float t) => t < 0.05f ? t / 0.05f : 1 - t;
}
