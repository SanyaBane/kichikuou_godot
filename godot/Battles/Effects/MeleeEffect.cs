using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The melee effect, ours after Sengoku Rance's Samurai effect 40000 (docs/sengoku/04-screen.md §2.4.1; not traced):
/// on the actor's half (left) the dust the armies raise as they clash (owner, 2026-10-15: dust on the field, not a
/// glow) — dim sand billows rising from the ground at the clash (x ~460), spreading back over the actor's ranks and
/// up to ~90 % of the height,
/// clods and jets of dirt kicked up, the billows settling by frame ~24; on the target's half (right) crescent slashes that
/// flash for a frame or two (white cores, pale blue to violet edges, every size and angle), dark blue-teal "urchins"
/// that swell and fade, four-point sparkles, small rings and streaks — brightest at frame 3, new slashes until frame
/// ~21, all gone by frame ~27. The same seed makes the same effect.
/// </summary>
internal static class MeleeEffect
{
    private const float Half = EffectCanvas.DesignWidth / 2, Height = EffectCanvas.DesignHeight;

    /// <summary>Where the armies clash: the dust rises most there and spreads back to the left.</summary>
    private const float Clash = 460;

    // The colours (light added over the scene).
    private static readonly Color Dust = new(0.3f, 0.25f, 0.17f, 0.28f), DustOld = new(0.18f, 0.14f, 0.1f, 0.28f), Clod = new(0.72f, 0.62f, 0.46f);
    private static readonly Color SlashWhite = new(0.9f, 0.92f, 0.95f), SlashWhiteEdge = new(0.55f, 0.78f, 1f);
    private static readonly Color SlashBlue = new(0.45f, 0.66f, 0.85f), SlashBlueEdge = new(0.7f, 0.88f, 1f);
    private static readonly Color SlashViolet = new(0.33f, 0.36f, 0.88f), SlashVioletEdge = new(0.55f, 0.55f, 1f);
    private static readonly Color UrchinTeal = new(0.3f, 0.58f, 0.62f, 0.8f), UrchinSteel = new(0.25f, 0.44f, 0.68f, 0.8f);
    private static readonly Color UrchinPale = new(0.66f, 0.72f, 0.74f, 0.8f), UrchinDeep = new(0.16f, 0.3f, 0.5f, 0.75f);
    private static readonly Color SparkleWhite = new(1f, 1f, 1f), SparkleCyan = new(0.7f, 0.95f, 1f);
    private static readonly Color RingGrey = new(0.7f, 0.76f, 0.76f, 0.55f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        int Count(float mean) => (int)MathF.Floor(mean + (float)random.NextDouble());
        var particles = new List<Particle>();

        // The dust: dim sand billows rising from the ground, first at the clash, then back over the actor's ranks, and
        // up to about a tenth of the height from the top (owner, 2026-10-15), the higher ones a little later, swelling
        // and drifting up and back as they thin out.
        for (int i = 0; i < 60; i++)
        {
            float back = (float)random.NextDouble(), x = Clash - 30 - 420 * back, size = Range(60, 100);
            float high = MathF.Pow((float)random.NextDouble(), 0.8f), y = Height + 10 - high * (Height - 30);
            float birth = back * 5 + high * 3 + Range(-0.5f, 1.5f), life = Range(13, 20);
            particles.Add(new Particle(EffectTextures.Billow, birth, life, new Vector2(x, y),
                new Vector2(Range(-1.4f, 0.2f), Range(-2.2f, -0.6f)), Range(0, MathF.Tau), Range(-0.01f, 0.01f),
                new Vector2(size * 1.8f, size), Dust, Settle, t => 1 + 1.4f * MathF.Sqrt(t), Ramp: t => Dust.Lerp(DustOld, t)));
        }
        // The dirt kicked up at the clash: clods thrown up and falling back, and short jets of dust.
        for (int i = 0; i < 70; i++)
        {
            float size = Range(2, 4.5f);
            particles.Add(new Particle(EffectTextures.Dot, Range(0, 10), Range(3, 5), new Vector2(Clash - Range(0, 260), Range(280, Height)),
                new Vector2(Range(-6, 4), Range(-15, -6)), 0, 0, new Vector2(size, size), Clod, t => 0.85f * (1 - t * t), _ => 1,
                Move: t => new Vector2(0, 45 * t * t)));
        }
        for (int i = 0; i < 24; i++)
        {
            float angle = -MathF.PI / 2 - Range(-0.2f, 0.9f), length = Range(40, 85);
            var direction = Vector2.FromAngle(angle);
            particles.Add(new Particle(EffectTextures.Spike, Range(0, 9), Range(2, 3.5f),
                new Vector2(Clash - Range(0, 280), Range(285, Height)) + direction * length / 2, direction * Range(3, 7), angle, 0,
                new Vector2(length, length / 4), Dust, t => 0.8f * (1 - t), t => 0.6f + 0.6f * t));
        }

        // The slashes: several each frame, most at the start, new ones until frame ~21, small violet ones after.
        for (int k = 0; k <= 24; k++)
        {
            float mean = k < 4 ? 5 : k < 9 ? 4 : k < 18 ? 3 : k < 22 ? 2 : 0.6f;
            for (int n = Count(mean); n > 0; n--)
            {
                bool big = random.NextDouble() < (k < 8 ? 0.3 : 0.15) && k < 22;
                float radius = big ? Range(55, 105) : Range(14, 45);
                var (body, edge) = k >= 22 || (!big && random.NextDouble() < 0.25) ? (SlashViolet, SlashVioletEdge)
                    : big || random.NextDouble() < 0.5 ? (SlashWhite, SlashWhiteEdge) : (SlashBlue, SlashBlueEdge);
                float birth = k - Range(0.1f, 0.7f), life = Range(1.4f, 2.4f);
                var position = new Vector2(Range(Half + 20, EffectCanvas.DesignWidth + 10), Range(-10, Height + 6));
                float rotation = Range(-1f, 1.3f), squash = Range(0.7f, 1.35f);
                var size = new Vector2(2 * radius * squash, 2 * radius / squash * (random.NextDouble() < 0.3 ? -1 : 1));
                foreach (var (texture, colour) in new[] { (EffectTextures.CrescentBody, body), (EffectTextures.CrescentEdge, edge) })
                    particles.Add(new Particle(texture, birth, life, position, Vector2.Zero, rotation, 0.15f, size, colour,
                        Flash, t => 1 + 0.15f * t));
            }
        }

        // The urchins: swelling rings with spikes, a couple a frame to frame 10, then fewer; a few large deep blue ones.
        for (int k = 1; k <= 16; k++)
        {
            for (int n = Count(k < 11 ? 1.8f : 1); n > 0; n--)
            {
                float radius = Range(16, 42);
                double pick = random.NextDouble();
                var colour = pick < 0.55 ? UrchinTeal : pick < 0.85 ? UrchinSteel : UrchinPale;
                particles.Add(new Particle(EffectTextures.Urchin, k - Range(0, 1), Range(5, 9),
                    new Vector2(Range(Half + 10, EffectCanvas.DesignWidth), Range(10, Height)), Vector2.Zero,
                    Range(0, MathF.Tau), 0, new Vector2(2 * radius, 2 * radius), colour, Swell, t => 1 + 0.35f * t));
            }
            if (k is 2 or 4 or 7 or 10 or 13)
            {
                float radius = Range(55, 80);
                particles.Add(new Particle(EffectTextures.Urchin, k - Range(0, 1), Range(7, 11),
                    new Vector2(Range(Half + 40, EffectCanvas.DesignWidth - 30), Range(60, Height)), Vector2.Zero,
                    Range(0, MathF.Tau), 0, new Vector2(2 * radius, 2 * radius), UrchinDeep, Swell, t => 1 + 0.25f * t));
            }
        }

        // The sparkles: four-point stars blinking for a frame or three, on the target's half alone.
        for (int k = 0; k <= 22; k++)
        {
            for (int n = Count(k < 13 ? 9 : 4); n > 0; n--)
            {
                var position = new Vector2(Range(Half + 10, EffectCanvas.DesignWidth), Range(0, Height));
                float rotation = random.NextDouble() < 0.7 ? Range(-0.15f, 0.15f) : Range(0, MathF.PI / 2);
                float size = Range(22, 64);
                particles.Add(new Particle(EffectTextures.Sparkle, k - Range(0, 1), Range(1, 3), position, Vector2.Zero,
                    rotation, 0, new Vector2(size, size), random.NextDouble() < 0.6 ? SparkleWhite : SparkleCyan,
                    Blink, t => 1 - 0.3f * t));
            }
        }

        // The small rings and the streaks.
        for (int k = 0; k <= 20; k++)
        {
            for (int n = Count(0.9f); n > 0; n--)
            {
                float size = Range(6, 16);
                particles.Add(new Particle(EffectTextures.Ring, k - Range(0, 1), Range(3, 6),
                    new Vector2(Range(Half + 10, EffectCanvas.DesignWidth), Range(0, Height)), Vector2.Zero, 0, 0,
                    new Vector2(size, size), RingGrey, Swell, t => 1 + 0.2f * t));
            }
            for (int n = Count(1.5f); n > 0; n--)
            {
                particles.Add(new Particle(EffectTextures.Dash, k - Range(0, 1), 1,
                    new Vector2(Range(Half, EffectCanvas.DesignWidth), Range(0, Height)), Vector2.Zero,
                    Range(0, MathF.Tau), 0, new Vector2(Range(14, 34), 4), random.NextDouble() < 0.5 ? SparkleWhite : SparkleCyan,
                    Blink, _ => 1));
            }
        }
        return particles;
    }

    /// <summary>In over the first tenth, held, then thinning out over the second half (a dust billow).</summary>
    private static float Settle(float t) => t < 0.1f ? t / 0.1f : t < 0.45f ? 1 : 1 - (t - 0.45f) / 0.55f;

    /// <summary>Full at once, then fading over the life (a slash).</summary>
    private static float Flash(float t) => t < 0.1f ? t / 0.1f : 1 - MathF.Pow((t - 0.1f) / 0.9f, 1.5f);

    /// <summary>In over the first sixth, then out (an urchin, a ring).</summary>
    private static float Swell(float t) => t < 0.15f ? t / 0.15f : 1 - MathF.Pow((t - 0.15f) / 0.85f, 2);

    /// <summary>Bright at once, fading straight (a sparkle, a streak).</summary>
    private static float Blink(float t) => t < 0.05f ? t / 0.05f : 1 - t;
}
