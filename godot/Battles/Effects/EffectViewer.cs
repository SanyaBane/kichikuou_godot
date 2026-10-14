using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Debug → Effects (ours, development; docs/ui/battle-screen.md, "The exchange's effects"): our exchange effects
/// over a battlefield the size of our scene (1024×410), for the owner to judge before they are wired into the
/// exchange. The battlefield is CG 900 with its overlay and, once a game has booted, Rance's (unit 1) and Ex's
/// (commander 1) armies at the troop limit; or plain black, as Sengoku Rance's frames are. The effect stands at the
/// scene's foot (y 8: 1024×402). The effect (melee, bow, magic, Rance Attack, the white and the black beam, Whirlwind Shot, High Speed, By la Way), Play, Loop, a frame slider (30 frames of 50 ms), the
/// enemy acting (mirrored), the blend, the speed and the seed. <see cref="SaveFramesAsync"/> writes every frame for a
/// side-by-side sheet.
/// </summary>
internal sealed partial class EffectViewer : Window
{
    private const int StageWidth = 1024, StageHeight = 410;

    private readonly Image? _battlefield;
    private SubViewport _stage = null!;
    private TextureRect _field = null!;
    private EffectPlayer _player = null!;
    private HSlider _slider = null!;
    private Label _time = null!;
    private CheckBox _loop = null!;
    private SpinBox _seed = null!;
    private OptionButton _effect = null!;
    private bool _sliding;

    /// <param name="cgs">The player's CGs.</param>
    /// <param name="state">The running game's state, or null before the boot (the battlefield without armies then).</param>
    public EffectViewer(CgLibrary cgs, GameState? state)
    {
        _battlefield = Battlefield(cgs, state);
        Title = "Effects (debug)";
        Size = new Vector2I(StageWidth + 24, StageHeight + 120);
        Exclusive = true;
        Transient = true;
    }

    public override void _Ready()
    {
        var background = new Panel();
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);
        var root = new VBoxContainer();
        root.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.KeepSize, 12);
        AddChild(root);

        _stage = new SubViewport { Size = new Vector2I(StageWidth, StageHeight), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        var black = new ColorRect { Color = Colors.Black, Size = new Vector2(StageWidth, StageHeight) };
        _stage.AddChild(black);
        _field = new TextureRect
        {
            Texture = _battlefield is null ? null : ImageTexture.CreateFromImage(_battlefield),
            Size = new Vector2(StageWidth, StageHeight),
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = CanvasItem.TextureFilterEnum.Linear,
        };
        _stage.AddChild(_field);
        _player = new EffectPlayer { Position = new Vector2(0, StageHeight - EffectPlayer.Height), Loop = true };
        _stage.AddChild(_player);
        AddChild(_stage);
        root.AddChild(new TextureRect { Texture = _stage.GetTexture(), CustomMinimumSize = new Vector2(StageWidth, StageHeight) });

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        _effect = new OptionButton();
        foreach (var (name, _, _, _) in Effects)
            _effect.AddItem(name);
        _effect.ItemSelected += _ => Restart();
        row.AddChild(_effect);
        var play = new Button { Text = "Play", FocusMode = Control.FocusModeEnum.None };
        play.Pressed += () =>
        {
            if (_player.Frame >= EffectCanvas.Frames - 0.01f)
                _player.Frame = 0;
            _player.Playing = !_player.Playing;
        };
        row.AddChild(play);
        _loop = new CheckBox { Text = "Loop", ButtonPressed = true, FocusMode = Control.FocusModeEnum.None };
        _loop.Toggled += on => _player.Loop = on;
        row.AddChild(_loop);
        var mirrored = new CheckBox { Text = "Enemy acts", FocusMode = Control.FocusModeEnum.None };
        mirrored.Toggled += on => _player.Mirrored = on;
        row.AddChild(mirrored);
        var blend = new OptionButton();
        blend.AddItem("Screen");
        blend.AddItem("Add");
        blend.ItemSelected += i => _player.Blend = (EffectBlend)(int)i;
        row.AddChild(blend);
        var ground = new OptionButton();
        ground.AddItem("Battlefield");
        ground.AddItem("Black");
        ground.ItemSelected += i => _field.Visible = i == 0;
        row.AddChild(ground);
        var speed = new OptionButton();
        foreach (var s in new[] { "1×", "½×", "¼×" })
            speed.AddItem(s);
        speed.ItemSelected += i => _player.Speed = 1f / (1 << (int)i);
        row.AddChild(speed);
        row.AddChild(new Label { Text = "Seed" });
        _seed = new SpinBox { MinValue = 1, MaxValue = 99999, Value = 1 };
        _seed.ValueChanged += _ => Restart();
        row.AddChild(_seed);
        root.AddChild(row);

        var timeline = new HBoxContainer();
        _slider = new HSlider
        {
            MinValue = 0,
            MaxValue = EffectCanvas.Frames - 1,
            Step = 1,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            FocusMode = Control.FocusModeEnum.None,
        };
        _slider.ValueChanged += v =>
        {
            if (_sliding)
                return;
            _player.Playing = false;
            _player.Frame = (float)v;
        };
        timeline.AddChild(_slider);
        _time = new Label { CustomMinimumSize = new Vector2(170, 0) };
        timeline.AddChild(_time);
        root.AddChild(timeline);

        CloseRequested += QueueFree;
        Restart();
        _player.Playing = true;
    }

    public override void _Process(double delta)
    {
        _sliding = true;
        _slider.Value = Math.Floor(_player.Frame);
        _sliding = false;
        _time.Text = $"frame {Math.Min(Math.Floor(_player.Frame), EffectCanvas.Frames - 1),2} · {_player.Frame * EffectCanvas.FrameSeconds * 1000,5:0} ms";
    }

    private void Restart()
    {
        bool playing = _player.Playing;
        var effect = Effects[_effect.Selected];
        _player.Start(effect.Build((int)_seed.Value));
        _player.Dark = effect.Dark;
        _player.Playing = playing;
    }

    /// <summary>
    /// Development (<c>--effects --shots DIR</c>): every frame of each effect with seed 1, the effect alone on black
    /// (<c>melee-NN.png</c>, <c>bow-NN.png</c>, 1024×402) and over the battlefield (<c>melee-scene-NN.png</c>, 1024×410), as
    /// the player is and the enemy acting (<c>melee-enemy-scene-NN.png</c>); a dark one over the battlefield only. Needs a
    /// window (a headless run draws nothing). <paramref name="only"/> names the prefixes saved (null: all).
    /// </summary>
    public async Task SaveFramesAsync(string dir, IReadOnlyCollection<string>? only = null)
    {
        System.IO.Directory.CreateDirectory(dir);
        _player.Playing = false;
        foreach (var (_, prefix, build, dark) in Effects)
        {
            if (only is not null && !only.Contains(prefix))
                continue;
            _player.Start(build(1));
            _player.Dark = dark;
            foreach (bool mirrored in new[] { false, true })
            {
                _player.Mirrored = mirrored;
                for (int k = 0; k < EffectCanvas.Frames; k++)
                {
                    _player.Frame = k;
                    for (int i = 0; i < 3; i++)
                        await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
                    if (!mirrored && !dark)                         // a dark effect's picture is its light
                        _player.EffectImage().SavePng(System.IO.Path.Combine(dir, $"{prefix}-{k:00}.png"));
                    _stage.GetTexture().GetImage().SavePng(System.IO.Path.Combine(dir, $"{prefix}-{(mirrored ? "enemy-" : "")}scene-{k:00}.png"));
                }
            }
        }
        _player.Mirrored = false;
        Restart();
    }

    /// <summary>The effects shown: the list's name, the saved frames' prefix, the builder, laid over dark.</summary>
    private static readonly (string Name, string Prefix, Func<int, IReadOnlyList<Particle>> Build, bool Dark)[] Effects =
    [
        ("Melee (after 40000)", "melee", MeleeEffect.Build, false),
        ("Bow (after 40260)", "bow", BowEffect.Build, false),
        ("Magic (after 40360)", "magic", MagicEffect.Build, false),
        ("Rance Attack (after 40500)", "rance-attack", RanceAttackEffect.Build, false),
        ("White Beam (after 40460)", "white-beam", WhiteBeamEffect.Build, false),
        ("Black Beam (40460 dark)", "black-beam", WhiteBeamEffect.Build, true),
        ("Whirlwind Shot (after 40660)", "whirlwind-shot", WhirlwindShotEffect.Build, false),
        ("High Speed (after 41400)", "high-speed", HighSpeedEffect.Build, false),
        ("By la Way (40500 red)", "by-la-way", RanceAttackEffect.BuildRed, false),
    ];

    /// <summary>
    /// The battlefield as our scene shows it between exchanges (<see cref="BattleScene"/>'s order: CG 900, the enemy's
    /// army, the player's, the overlay 901), 640×256; null when the CGs are not there.
    /// </summary>
    private static Image? Battlefield(CgLibrary cgs, GameState? state)
    {
        if (cgs.Get(900) is not { } field)
            return null;
        var layers = new List<(CgImage Cg, bool Keyed)> { (field, false) };
        if (state is not null)
        {
            foreach (int sprite in new[] { state.Cmdrs[1].CgFull.Value, state.Units[1].CgFull.Value })
            {
                if (sprite != 0 && cgs.Get(sprite) is { } army)
                    layers.Add((army, true));
            }
        }
        if (cgs.Get(901) is { } overlay)
            layers.Add((overlay, true));
        var palette = Palette.Initial;
        foreach (var (cg, _) in layers)
            palette = cg.PaletteOver(palette);
        var rgba = new byte[BattleScene.Width * BattleScene.Height * 4];
        foreach (var (cg, keyed) in layers)
        {
            var pixels = cg.ToRgba(keyed ? 40 : null, palette);
            for (int y = Math.Max(0, cg.Y); y < Math.Min(BattleScene.Height, cg.Y + cg.Height); y++)
            {
                for (int x = Math.Max(0, cg.X); x < Math.Min(BattleScene.Width, cg.X + cg.Width); x++)
                {
                    int from = ((y - cg.Y) * cg.Width + x - cg.X) * 4, to = (y * BattleScene.Width + x) * 4;
                    if (pixels[from + 3] != 0)
                        Array.Copy(pixels, from, rgba, to, 4);
                }
            }
        }
        return Image.CreateFromData(BattleScene.Width, BattleScene.Height, false, Image.Format.Rgba8, rgba);
    }
}
