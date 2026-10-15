using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// Debug → Effects (ours, development; docs/ui/battle-screen.md, "The exchange's effects"): our exchange effects
/// over a battlefield the size of our scene (1024×410), for the owner to judge before they are wired into the
/// exchange. The battlefield is CG 900 with its overlay and, once a game has booted, Ex's (commander 1) army at the
/// troop limit; on the player's side Rance's (unit 1) army for melee and magic, Isoroku's (unit 27) archers for the bow (once a game has booted) and for
/// a special the sprite its user shows while using it (<c>FIGT.ADV:1660-1713</c>: Patton's (unit 18) CG 1530, the
/// ninjas' own; docs/battle/02-resolution.md §4), the user picked from the special's users; or plain black, as Sengoku
/// Rance's frames are. The effect stands at the scene's foot (y 8: 1024×402). The effect and its user, Play, Loop, a
/// frame slider (30 frames of 50 ms), the enemy acting (mirrored), the blend, the speed, the seed and an offset to try
/// the effect at another place (X and Y in the design space, 800 × 314; owner, 2026-10-15). <see cref="SaveFramesAsync"/> writes every frame for a
/// side-by-side sheet.
/// </summary>
internal sealed partial class EffectViewer : Window
{
    private const int StageWidth = 1024, StageHeight = 410;

    private readonly CgLibrary _cgs;
    private readonly GameState? _state;
    private SubViewport _stage = null!;
    private TextureRect _field = null!;
    private EffectPlayer _player = null!;
    private HSlider _slider = null!;
    private Label _time = null!;
    private CheckBox _loop = null!;
    private SpinBox _seed = null!;
    private OptionButton _effect = null!;
    private OptionButton _user = null!;
    private bool _sliding;

    /// <param name="cgs">The player's CGs.</param>
    /// <param name="state">The running game's state, or null before the boot (the battlefield without armies then).</param>
    public EffectViewer(CgLibrary cgs, GameState? state)
    {
        _cgs = cgs;
        _state = state;
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

        _stage = new SubViewport { Size = new Vector2I(StageWidth, StageHeight), RenderTargetUpdateMode = SubViewport.UpdateMode.Always };
        var black = new ColorRect { Color = Colors.Black, Size = new Vector2(StageWidth, StageHeight) };
        _stage.AddChild(black);
        _field = new TextureRect
        {
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
        foreach (var (name, _, _, _, _) in Effects)
            _effect.AddItem(name);
        _effect.ItemSelected += _ =>
        {
            FillUsers();
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

    /// <summary>The selected effect's users in the user list (one line, the army, for melee, bow and magic).</summary>
    private void FillUsers()
    {
        _user.Clear();
        foreach (var (name, _) in UsersOf(Effects[_effect.Selected]))
            _user.AddItem(name);
        _user.Disabled = _user.ItemCount < 2;
        _user.Select(0);
        ShowField();
    }

    /// <summary>The battlefield with the selected user on the player's side.</summary>
    private void ShowField()
    {
        var users = UsersOf(Effects[_effect.Selected]);
        var field = Battlefield(_cgs, _state, users[Math.Max(0, _user.Selected)].Cg);
        _field.Texture = field is null ? null : ImageTexture.CreateFromImage(field);
    }

    /// <summary>
    /// The users to pick from: a special's, or for melee and magic Rance's army and for the bow Isoroku's (unit 27)
    /// archers (owner, 2026-10-15; none before the boot).
    /// </summary>
    private IReadOnlyList<(string Name, int Cg)> UsersOf((string Name, string Prefix, Func<int, IReadOnlyList<Particle>> Build, bool Dark, (string Name, int Cg)[]? Users) effect) =>
        effect.Users ?? (effect.Prefix == "bow"
            ? [("Isoroku's (unit 27) archers", _state?.Units[27].CgFull.Value ?? 0)]
            : [("Rance's (unit 1) army", _state?.Units[1].CgFull.Value ?? 0)]);

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
            var (_, prefix, build, dark, _) = effect;
            if (only is not null && !only.Contains(prefix))
                continue;
            var field = Battlefield(_cgs, _state, UsersOf(effect)[0].Cg);
            _field.Texture = field is null ? null : ImageTexture.CreateFromImage(field);
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
        ShowField();
        Restart();
    }

    /// <summary>
    /// The effects shown: the list's name, the saved frames' prefix, the builder, laid over dark, and a special's users
    /// with the sprite each shows while using it (<c>FIGT.ADV:1660-1713</c>, <c>SpecialAttackSprites</c> in
    /// <c>Battle.Resolution.cs</c>; the ninja's follow-up shows the ninja's own, record 51's <c>CgFull</c>, <c>:1922-1968</c>).
    /// </summary>
    private static readonly (string Name, string Prefix, Func<int, IReadOnlyList<Particle>> Build, bool Dark, (string Name, int Cg)[]? Users)[] Effects =
    [
        ("Melee (after 40000)", "melee", MeleeEffect.Build, false, null),
        ("Bow (after 40260)", "bow", BowEffect.Build, false, null),
        ("Magic (after 40360)", "magic", MagicEffect.Build, false, null),
        ("Rance Attack (after 40500)", "rance-attack", RanceAttackEffect.Build, false, [("Rance (unit 1)", 1520)]),
        ("Kentarou-kun Slash (after 40800)", "kentarou-slash", KentarouSlashEffect.Build, false, [("Kentarou (unit 31)", 1222)]),
        ("White Beam: Shizuka (after 40460)", "white-beam-shizuka", ShizukaBeamEffect.Build, false, [("Shizuka (unit 17)", 1522)]),
        ("White Beam: Magic (after 40460)", "white-beam-magic", MagicBeamEffect.Build, false, [("Magic (unit 39)", 1523)]),
        ("EPO Light: Gandhi (after 40460)", "epo-light", EpoLightEffect.Build, false, [("Gandhi (unit 37)", 1524)]),
        ("Black Beam: Chizuko (40460 dark)", "black-beam-chizuko", ChizukoBeamEffect.Build, true, [("Chizuko (unit 38)", 1526)]),
        ("Black Beam: Nagi (40460 dark)", "black-beam-nagi", NagiBeamEffect.Build, true, [("Nagi (unit 40)", 1525)]),
        ("Whirlwind Shot (after 40660)", "whirlwind-shot", WhirlwindShotEffect.Build, false, [("Isoroku (unit 27)", 1528)]),
        ("High Speed (after 41400)", "high-speed", HighSpeedEffect.Build, false, [("Meglass (unit 30)", 1527)]),
        ("By la Way (40500 red)", "by-la-way", RanceAttackEffect.BuildRed, false, [("Rick (unit 7)", 1521)]),
        ("Ninja (after 40100)", "ninja", NinjaEffect.Build, false, [("Kanami", 1164), ("Freya", 1176), ("Kaoru", 1182), ("Wichita", 1185)]),
        ("Vanish Bomb (after 40560)", "vanish-bomb", VanishBombEffect.Build, false, [("Miki (unit 32)", 1529)]),
        ("C. War Dance (after 47400)", "war-dance", WarDanceEffect.Build, false, [("Patton (unit 18)", 1530)]),
        ("Chuhai (after 47060)", "chuhai", ChuhaiEffect.Build, false, [("Asuka (unit 11)", 1121)]),
        ("T2 Goliath (after 46820)", "goliath", GoliathEffect.Build, false, [("T2 Goliath (record 51), no sprite", 0)]),
        ("T2 Soaring Wave (ours)", "soaring-wave", SoaringWaveEffect.Build, false, [("Rolex (unit 47)", 1252)]),
    ];

    /// <summary>
    /// The battlefield as our scene shows it between exchanges (<see cref="BattleScene"/>'s order: CG 900, the enemy's
    /// army — Ex's (commander 1), once a game has booted — the player's sprite <paramref name="player"/> (0: none), the
    /// overlay 901), 640×256; null when the CGs are not there.
    /// </summary>
    private static Image? Battlefield(CgLibrary cgs, GameState? state, int player)
    {
        if (cgs.Get(900) is not { } field)
            return null;
        var layers = new List<(CgImage Cg, bool Keyed)> { (field, false) };
        foreach (int sprite in new[] { state?.Cmdrs[1].CgFull.Value ?? 0, player })
        {
            if (sprite != 0 && cgs.Get(sprite) is { } army)
                layers.Add((army, true));
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
