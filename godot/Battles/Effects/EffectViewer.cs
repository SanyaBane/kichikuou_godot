using Godot;
using Kichikuou.Content.Graphics;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Debug → Effects (ours, development; docs/ui/battle-screen.md, "The exchange's effects"): our exchange effects
/// over a battlefield the size of our scene (1024×410, <see cref="EffectStage"/>), for the owner to judge before they are
/// wired into the exchange. The battlefield is CG 900 with its overlay, Ex's (commander 1) army at the troop limit and on
/// the player's side the effect's user, picked from <see cref="EffectCatalog"/>'s (the plain attacks' and the magic's
/// armies, a special's the sprite its user shows while using it); or plain black, as Sengoku Rance's frames are. The
/// effect and its user, Play, Loop, a frame slider (30 frames of 50 ms), the enemy acting (mirrored; on for the poison,
/// which only the enemy casts), the blend, the speed, the seed and an offset to try the effect at another place (X and Y
/// in the design space, 800 × 314; owner, 2026-10-15). <see cref="SaveFramesAsync"/> writes every frame for a
/// side-by-side sheet.
/// </summary>
internal sealed partial class EffectViewer : Window
{
    private const int StageWidth = EffectStage.StageWidth, StageHeight = EffectStage.StageHeight;

    private readonly CgLibrary _cgs;
    private EffectStage _stage = null!;
    private TextureRect _field = null!;
    private EffectPlayer _player = null!;
    private HSlider _slider = null!;
    private Label _time = null!;
    private CheckBox _loop = null!;
    private CheckBox _mirrored = null!;
    private SpinBox _seed = null!;
    private OptionButton _effect = null!;
    private OptionButton _user = null!;
    private bool _sliding;

    /// <param name="cgs">The player's CGs.</param>
    public EffectViewer(CgLibrary cgs)
    {
        _cgs = cgs;
        Title = "Effects (debug)";
        Size = new Vector2I(StageWidth + 24, StageHeight + 160);
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

        _stage = new EffectStage();
        _field = _stage.Field;
        _player = _stage.Player;
        _player.Loop = true;
        AddChild(_stage);
        root.AddChild(new TextureRect { Texture = _stage.GetTexture(), CustomMinimumSize = new Vector2(StageWidth, StageHeight) });

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 10);
        _effect = new OptionButton();
        foreach (var effect in Effects)
            _effect.AddItem(effect.Name);
        _effect.ItemSelected += _ =>
        {
            FillUsers();
            _mirrored.ButtonPressed = Effects[_effect.Selected].EnemyCasts;
            Restart();
        };
        row.AddChild(_effect);
        _user = new OptionButton();
        _user.ItemSelected += _ => ShowField();
        row.AddChild(_user);
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
        _mirrored = new CheckBox { Text = "Enemy acts", FocusMode = Control.FocusModeEnum.None };
        _mirrored.Toggled += on => _player.Mirrored = on;
        row.AddChild(_mirrored);
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

        // Where the effect is drawn, moved from its own place (design units, 800 × 314: +x toward the target, +y down).
        var place = new HBoxContainer();
        place.AddThemeConstantOverride("separation", 10);
        place.AddChild(new Label { Text = "Offset (of 800 × 314)  X" });
        var offsetX = new SpinBox { MinValue = -800, MaxValue = 800, Step = 1, Value = 0 };
        place.AddChild(offsetX);
        place.AddChild(new Label { Text = "Y" });
        var offsetY = new SpinBox { MinValue = -314, MaxValue = 314, Step = 1, Value = 0 };
        place.AddChild(offsetY);
        offsetX.ValueChanged += v => _player.Offset = _player.Offset with { X = (float)v };
        offsetY.ValueChanged += v => _player.Offset = _player.Offset with { Y = (float)v };
        var reset = new Button { Text = "Reset", FocusMode = Control.FocusModeEnum.None };
        reset.Pressed += () =>
        {
            offsetX.Value = 0;
            offsetY.Value = 0;
        };
        place.AddChild(reset);
        root.AddChild(place);

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
        FillUsers();
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

    /// <summary>The selected effect's users in the user list (<see cref="EffectShow.Users"/>).</summary>
    private void FillUsers()
    {
        _user.Clear();
        foreach (var user in Effects[_effect.Selected].Users)
            _user.AddItem(user.Name);
        _user.Disabled = _user.ItemCount < 2;
        _user.Select(0);
        ShowField();
    }

    /// <summary>The battlefield with the selected user on the player's side.</summary>
    private void ShowField()
    {
        var effect = Effects[_effect.Selected];
        _stage.ShowField(_cgs, effect.Users[Math.Max(0, _user.Selected)], effect.EnemyArmy);
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
        foreach (var effect in Effects)
        {
            var (prefix, dark) = (effect.Prefix, effect.Dark);
            if (only is not null && !only.Contains(prefix))
                continue;
            _stage.ShowField(_cgs, effect.Users[0], effect.EnemyArmy);
            _player.Start(effect.Build(1));
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
        _player.Mirrored = _mirrored.ButtonPressed;
        ShowField();
        Restart();
    }

    /// <summary>The effects shown (<see cref="EffectCatalog.All"/>), the unused Ninja troop's too.</summary>
    private static IReadOnlyList<EffectShow> Effects => EffectCatalog.All;
}
