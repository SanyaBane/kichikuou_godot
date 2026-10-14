using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// One piece of an effect: a texture drawn from <see cref="Birth"/> for <see cref="Life"/> frames (a frame is
/// <see cref="EffectCanvas.FrameSeconds"/>), centred at <see cref="Position"/> plus <see cref="Velocity"/> per frame,
/// turned by <see cref="Rotation"/> plus <see cref="Spin"/> per frame, <see cref="Size"/> (a negative axis flips it)
/// times <see cref="Grow"/>, tinted <see cref="Colour"/> times <see cref="Alpha"/>. <see cref="Alpha"/> and
/// <see cref="Grow"/> take the age as a fraction of the life, 0 to 1, and so do the optional <see cref="Ramp"/> (the
/// colour by age, in place of <see cref="Colour"/>) and <see cref="Move"/> (the offset from <see cref="Position"/> by age,
/// added to the velocity's). Positions and sizes are in the design space, <see cref="EffectCanvas.DesignWidth"/> ×
/// <see cref="EffectCanvas.DesignHeight"/>, the actor on the left.
/// </summary>
internal sealed record Particle(
    Texture2D Texture, float Birth, float Life, Vector2 Position, Vector2 Velocity, float Rotation, float Spin,
    Vector2 Size, Color Colour, Func<float, float> Alpha, Func<float, float> Grow,
    Func<float, Color>? Ramp = null, Func<float, Vector2>? Move = null);

/// <summary>
/// Draws an effect's particles as they stand at <see cref="Frame"/>, each added to what is under it (the canvas
/// sits on black in <see cref="EffectPlayer"/>'s own viewport, as Sengoku Rance's frames are black where nothing is).
/// The design space is Sengoku Rance's frame, 800×314 (docs/sengoku/04-screen.md §2.4.1); the node's scale fits it
/// to the viewport.
/// </summary>
internal sealed partial class EffectCanvas : Node2D
{
    public const float DesignWidth = 800, DesignHeight = 314;

    /// <summary>One frame of Sengoku Rance's effects: 50 ms; an effect is <see cref="Frames"/> of them, 1.5 s.</summary>
    public const double FrameSeconds = 0.05;
    public const int Frames = 30;

    private IReadOnlyList<Particle> _particles = [];
    private float _frame;

    public EffectCanvas()
    {
        Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add };
    }

    /// <summary>The particles drawn.</summary>
    public IReadOnlyList<Particle> Particles
    {
        get => _particles;
        set
        {
            _particles = value;
            QueueRedraw();
        }
    }

    /// <summary>The time drawn, in frames from the effect's start (fractions between them).</summary>
    public float Frame
    {
        get => _frame;
        set
        {
            _frame = value;
            QueueRedraw();
        }
    }

    public override void _Draw()
    {
        foreach (var p in _particles)
        {
            float age = _frame - p.Birth;
            if (age < 0 || age >= p.Life)
                continue;
            float t = age / p.Life;
            float alpha = p.Alpha(t);
            if (alpha <= 0)
                continue;
            var size = p.Size * p.Grow(t);
            var at = p.Position + p.Velocity * age + (p.Move?.Invoke(t) ?? Vector2.Zero);
            DrawSetTransform(at, p.Rotation + p.Spin * age, new Vector2(Math.Sign(size.X), Math.Sign(size.Y)));
            var extent = size.Abs();
            var colour = p.Ramp?.Invoke(t) ?? p.Colour;
            DrawTextureRect(p.Texture, new Rect2(-extent / 2, extent), false, colour * new Color(1, 1, 1, alpha));
        }
        DrawSetTransform(Vector2.Zero);
    }
}
