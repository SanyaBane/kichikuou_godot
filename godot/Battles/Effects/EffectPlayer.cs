using Godot;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>How an effect's light is laid over the scene.</summary>
internal enum EffectBlend
{
    /// <summary>1 − (1 − scene)(1 − effect): bright parts brighten, never past white (Sengoku Rance's draw method 1, likely).</summary>
    Screen,

    /// <summary>scene + effect, clamped.</summary>
    Add,
}

/// <summary>
/// Plays one exchange effect over what is drawn under it (docs/ui/battle-screen.md, "The exchange's effects"): the
/// effect is drawn on black in a viewport of its own (<see cref="EffectCanvas"/>, the design space of Sengoku Rance's
/// 800×314 frames scaled ×1.28 (<see cref="DesignScale"/>) to <see cref="Width"/> × <see cref="Height"/>) and that picture laid over the scene by
/// <see cref="Blend"/>, mirrored when the enemy acts (the actor's half is the left one). Presentation only.
/// </summary>
internal sealed partial class EffectPlayer : Control
{
    public const float DesignScale = 1.28f;
    public const int Width = 1024, Height = 402;

    private const string BlendShader = """
        shader_type canvas_item;
        uniform sampler2D screen : hint_screen_texture, filter_linear;
        uniform bool add;
        void fragment() {
            vec3 effect = texture(TEXTURE, UV).rgb;
            vec3 scene = texture(screen, SCREEN_UV).rgb;
            COLOR = vec4(add ? min(scene + effect, vec3(1.0)) : 1.0 - (1.0 - scene) * (1.0 - effect), 1.0);
        }
        """;

    private readonly SubViewport _view;
    private readonly EffectCanvas _canvas;
    private readonly TextureRect _output;
    private readonly ShaderMaterial _material;

    public EffectPlayer()
    {
        CustomMinimumSize = new Vector2(Width, Height);
        Size = CustomMinimumSize;
        MouseFilter = MouseFilterEnum.Ignore;
        _view = new SubViewport
        {
            Size = new Vector2I(Width, Height),
            TransparentBg = false,
            RenderTargetUpdateMode = SubViewport.UpdateMode.Always,
        };
        var black = new ColorRect { Color = Colors.Black, Size = new Vector2(Width, Height) };
        _view.AddChild(black);
        _canvas = new EffectCanvas { Scale = new Vector2(DesignScale, DesignScale) };
        _view.AddChild(_canvas);
        AddChild(_view);
        _material = new ShaderMaterial { Shader = new Shader { Code = BlendShader } };
        _output = new TextureRect
        {
            Texture = _view.GetTexture(),
            Size = new Vector2(Width, Height),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            Material = _material,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        AddChild(_output);
        Visible = false;
    }

    /// <summary>The time shown, in frames (<see cref="EffectCanvas.FrameSeconds"/>) from the effect's start.</summary>
    public float Frame
    {
        get => _canvas.Frame;
        set => _canvas.Frame = value;
    }

    /// <summary>Whether the time runs on its own.</summary>
    public bool Playing { get; set; }

    /// <summary>Starts again from frame 0 at the end instead of stopping (the viewer).</summary>
    public bool Loop { get; set; }

    /// <summary>How fast the time runs: 1 is the effect's own 1.5 s.</summary>
    public float Speed { get; set; } = 1;

    /// <summary>When the enemy acts: the picture mirrored, its actor's half on the right.</summary>
    public bool Mirrored
    {
        get => _output.FlipH;
        set => _output.FlipH = value;
    }

    public EffectBlend Blend
    {
        get => (bool)_material.GetShaderParameter("add") ? EffectBlend.Add : EffectBlend.Screen;
        set => _material.SetShaderParameter("add", value == EffectBlend.Add);
    }

    /// <summary>Shows an effect from its start; <see cref="Playing"/> runs it.</summary>
    public void Start(IReadOnlyList<Particle> particles)
    {
        _canvas.Particles = particles;
        Frame = 0;
        Visible = true;
    }

    /// <summary>The effect alone as it stands, on black (<see cref="Width"/> × <see cref="Height"/>).</summary>
    public Image EffectImage() => _view.GetTexture().GetImage();

    public override void _Process(double delta)
    {
        if (!Playing)
            return;
        float frame = Frame + (float)(delta / EffectCanvas.FrameSeconds) * Speed;
        if (frame >= EffectCanvas.Frames)
        {
            if (!Loop)
                Playing = false;
            frame = Loop ? frame % EffectCanvas.Frames : EffectCanvas.Frames;
        }
        Frame = frame;
    }
}
