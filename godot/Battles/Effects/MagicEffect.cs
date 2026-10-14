using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// The magic effect, ours after Sengoku Rance's Mage effect 40360, Fire Blast (docs/sengoku/04-screen.md §2.4.1; not
/// traced), the same for every magic class (owner, 2026-10-14). All of it on the target's half (right): a glowing orb
/// with rays (frames 0-1), a white-hot ellipse with flames licking up (2-5), a fireball of billows over the whole half,
/// white at its heart and yellow, orange and dark red toward its edges, with a pale shockwave arc running off its left
/// edge toward the actor (6-15); then orange billows breaking up around a white core, gold sparkles, fine ember trails
/// and embers (14-24), the core fading by frame ~27. The same seed makes the same effect.
/// </summary>
internal static class MagicEffect
{
    private const float Half = EffectCanvas.DesignWidth / 2, Width = EffectCanvas.DesignWidth, Height = EffectCanvas.DesignHeight;

    /// <summary>Where the blast starts, on the target's half.</summary>
    private static readonly Vector2 Centre = new(645, 195);

    // The colours (light added over the scene).
    private static readonly Color White = new(1f, 0.97f, 0.85f), Yellow = new(1f, 0.82f, 0.3f);
    private static readonly Color Orange = new(0.95f, 0.45f, 0.1f), Smoke = new(0.45f, 0.12f, 0.03f);
    private static readonly Color Halo = new(1f, 0.6f, 0.25f, 0.85f), Flame = new(0.85f, 0.95f, 0.35f, 0.8f);
    private static readonly Color Wave = new(0.6f, 0.6f, 0.63f, 0.75f), Gold = new(1f, 0.8f, 0.4f);
    private static readonly Color Ember = new(1f, 0.6f, 0.2f, 0.8f), Trail = new(0.95f, 0.85f, 0.55f, 0.6f);
    private static readonly Color Core = new(1f, 0.93f, 0.82f, 0.9f);

    public static IReadOnlyList<Particle> Build(int seed)
    {
        var random = new Random(seed);
        float Range(float from, float to) => from + (float)random.NextDouble() * (to - from);
        int Count(float mean) => (int)MathF.Floor(mean + (float)random.NextDouble());
        var particles = new List<Particle>();

        // The orb: a hot core, a warm halo and rays, growing a little over frames 0-2, then swallowed by the blast.
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 3.5f, Centre, Vector2.Zero, 0, 0, new Vector2(540, 540), Halo,
            Fade, t => 1 + 0.3f * t));
        particles.Add(new Particle(EffectTextures.Rays, -0.5f, 3, Centre, Vector2.Zero, Range(0, MathF.Tau), 0.05f,
            new Vector2(360, 360), Halo * new Color(1, 1, 1, 0.45f), Fade, t => 1 + 0.2f * t));
        particles.Add(new Particle(EffectTextures.Bokeh, -0.5f, 3.5f, Centre, Vector2.Zero, 0, 0, new Vector2(85, 85), White,
            t => 1, t => 1 + 0.5f * t));
        particles.Add(new Particle(EffectTextures.Glow, -0.5f, 4, Centre, Vector2.Zero, 0, 0, new Vector2(190, 190), White,
            t => 1, t => 1 + 0.6f * t));

        // The white-hot ellipse and the flames licking up from its top (frames 2-5).
        for (int i = 0; i < 10; i++)
        {
            var at = Centre + new Vector2(Range(-50, 50), Range(-20, 25));
            particles.Add(new Particle(EffectTextures.Puff, Range(1.2f, 2.5f), Range(4, 6), at, Vector2.Zero, Range(0, MathF.Tau), 0,
                new Vector2(Range(90, 130) * 1.4f, Range(90, 130)), White, Fade, t => 1 + 0.4f * t));
        }
        for (int i = 0; i < 14; i++)
        {
            var at = Centre + new Vector2(Range(-70, 60), Range(-55, -25));
            particles.Add(new Particle(EffectTextures.Puff, Range(2, 5), Range(2.5f, 4), at, new Vector2(Range(-1, 1), Range(-7, -3)),
                Range(0, MathF.Tau), 0, new Vector2(1, 1) * Range(22, 40), Flame, Fade, t => 1 + 0.3f * t));
        }

        // The fireball: billows thrown out from the centre (fast at first, slowing), white at birth, then yellow, orange
        // and smoke as they age; the late ones farther out, so the edges burn orange.
        // Over the whole target's half by frame ~6 and past its edges; the middle stays full.
        for (int i = 0; i < 130; i++)
        {
            float birth = 2 + 8 * MathF.Pow((float)random.NextDouble(), 1.5f);
            float angle = Range(0, MathF.Tau);
            float reach = (130 + (birth - 2) * 18) * MathF.Pow((float)random.NextDouble(), 0.6f) + 30;
            var direction = new Vector2(MathF.Cos(angle) * 1.4f, MathF.Sin(angle) * 1.15f);
            float life = Range(10, 14), size = Range(110, 200);
            particles.Add(new Particle(EffectTextures.Puff, birth, life, Centre + direction * Range(0, 20), Vector2.Zero,
                Range(0, MathF.Tau), Range(-0.02f, 0.02f), new Vector2(size, size), White,
                Billow, t => 0.6f + 0.6f * t,
                Ramp: t => FireColour(0.6f * t + (birth - 2) / 24 + reach / 1000),
                Move: t => direction * reach * (1 - MathF.Pow(1 - t, 4))));
        }

        // The flames: smaller bright tongues on the fireball's rim, rising, the top's yellow turning orange.
        for (int i = 0; i < 45; i++)
        {
            float birth = Range(3, 10), angle = Range(-MathF.PI, 0.3f);
            var at = Centre + new Vector2(MathF.Cos(angle) * 1.4f, MathF.Sin(angle) * 1.15f) * Range(120, 210);
            float size = Range(45, 90);
            particles.Add(new Particle(EffectTextures.Puff, birth, Range(4, 7), at, new Vector2(Range(-1, 1), Range(-5, -2)),
                Range(0, MathF.Tau), 0, new Vector2(size, size), Yellow, Billow, t => 0.8f + 0.5f * t,
                Ramp: t => FireColour(0.3f + 0.5f * t)));
        }

        // The shockwave: a pale glassy lens bulging toward the actor, off the fireball's left edge, running left and
        // fading (frames 3-16).
        particles.Add(new Particle(EffectTextures.Shock, 3, 13.5f, new Vector2(Centre.X - 110, Centre.Y + 10), new Vector2(-16, 0),
            MathF.PI, 0, new Vector2(330, 360), Wave, Fade, t => 1 + 0.3f * t));

        // The core: a white glow standing where the blast was, from frame ~13 to the end.
        var core = Centre + new Vector2(10, -15);
        particles.Add(new Particle(EffectTextures.Glow, 12.5f, 15, core, Vector2.Zero, 0, 0, new Vector2(260, 260), Core,
            t => t < 0.1f ? t / 0.1f : 1 - MathF.Pow((t - 0.1f) / 0.9f, 0.8f), t => 1 + 0.2f * t));

        // The sparkles, the ember trails and the embers.
        for (int k = 13; k <= 25; k++)
        {
            for (int n = Count(k < 20 ? 9 : 4); n > 0; n--)
            {
                float size = Range(30, 80);
                particles.Add(new Particle(EffectTextures.Sparkle, k - Range(0, 1), Range(2, 5),
                    new Vector2(Range(Half + 60, Width), Range(0, Height)), Vector2.Zero, Range(-0.2f, 0.2f), 0,
                    new Vector2(size, size), Gold, Fade, t => 1 - 0.3f * t));
            }
            for (int n = Count(k < 21 ? 9 : 3); n > 0; n--)
            {
                float angle = Range(0, MathF.Tau);
                var at = core + Vector2.FromAngle(angle) * Range(40, 220);
                particles.Add(new Particle(EffectTextures.Dash, k - Range(0, 1), Range(3, 6), at, Vector2.FromAngle(angle) * 3,
                    angle, 0, new Vector2(Range(50, 120), 4), Trail, Fade, _ => 1));
            }
        }
        for (int i = 0; i < 60; i++)
        {
            float birth = Range(7, 20), angle = Range(0, MathF.Tau);
            float size = Range(4, 9);
            particles.Add(new Particle(EffectTextures.Glow, birth, Range(5, 9), Centre + Vector2.FromAngle(angle) * Range(30, 160),
                Vector2.FromAngle(angle) * Range(2, 6) + new Vector2(0, -1), 0, 0, new Vector2(size, size) * 3, Ember, Fade, _ => 1));
        }
        return particles;
    }

    /// <summary>A billow's colour by age (past 1 it stays smoke): white, yellow, orange, smoke.</summary>
    private static Color FireColour(float t) =>
        t < 0.2f ? White.Lerp(Yellow, t / 0.2f)
        : t < 0.5f ? Yellow.Lerp(Orange, (t - 0.2f) / 0.3f)
        : Orange.Lerp(Smoke, Math.Min(1, (t - 0.5f) / 0.5f));

    /// <summary>A billow: in at once, full to the middle of its life, then out.</summary>
    private static float Billow(float t) => t < 0.08f ? t / 0.08f * 0.7f : t < 0.45f ? 0.7f : 0.7f * MathF.Pow(1 - (t - 0.45f) / 0.55f, 1.6f);

    /// <summary>In at once, then out straight.</summary>
    private static float Fade(float t) => t < 0.08f ? t / 0.08f : 1 - (t - 0.08f) / 0.92f;
}
