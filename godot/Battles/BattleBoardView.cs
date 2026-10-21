using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles.Effects;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Our own battle screen (our interface, docs/ui/battle-screen.md, "The layout" and "The
/// scene between exchanges"): the armies' scene on top (<see cref="BattleScene"/>, its sides the same
/// picture blurred and darkened), the original's windows over it (the stage's screen shown only inside
/// them, <see cref="WindowMask"/>), the speaker's portrait beside a speech bubble, the round's order
/// (<see cref="RoundOrder"/>), the two armies' cards (<see cref="BattleBoard.Cards"/>) and the rounds
/// gauge between them, and our own command input over the cards (<see cref="Commands"/>). It decides
/// nothing: <see cref="GameScreen"/> turns a pointer over a card into the script's pointer on that card's
/// panel (<see cref="ToScript"/>), and elsewhere into the point of the windows' layer under it.
/// </summary>
internal sealed partial class BattleBoardView : Control
{
    // The layout in the pixels of a 1280×800 window (two to a script pixel).
    private static readonly Rect2 SidesArea = new(0, 0, 1280, 410);
    private static readonly Rect2 SceneArea = new(128, 0, 1024, 410);
    private static readonly Rect2 WindowsArea = new(312, 0, 656, 410);       // 640×400 script pixels ×1.025
    private static readonly Rect2 StripArea = new(10, 374, 1260, 32);         // over the scene's foot (owner, 2026-10-16)
    private static readonly Rect2 PlayerNameArea = new(10, 424, 574, 24);   // up under the separator (owner, 2026-10-16)
    private static readonly Rect2 EnemyNameArea = new(696, 424, 574, 24);
    private static readonly Rect2 GaugeArea = new(594, 450, 92, 308);
    private static readonly Rect2 SpeakerLeft = new(8, 6, 300, 398);
    private static readonly Rect2 SpeakerRight = new(972, 6, 300, 398);
    private static readonly Vector2 CardSize = new(282, 150);
    private static readonly float[] CardRows = [450, 608];
    private const float FaceSize = 118;
    private const float TextSize = 17;

    /// <summary>The cards' columns: the player's back row far left, its front row, the enemy's front row, its back row far right.</summary>
    private static float Column(BattlePlace place) => (place.Side, place.Slot) switch
    {
        (BattleSide.Player, 3 or 4) => 10,
        (BattleSide.Player, _) => 302,
        (BattleSide.Enemy, 1 or 2) => 696,
        _ => 988,
    };

    internal static Rect2 CardArea(BattlePlace place) =>
        new(Column(place), CardRows[(place.Slot - 1) % 2], CardSize.X, CardSize.Y);

    private static readonly Color PlayerColour = new(0.27f, 0.46f, 0.86f);
    private static readonly Color EnemyColour = new(0.82f, 0.3f, 0.3f);
    private static readonly Color HpColour = new(0.42f, 0.76f, 0.42f);

    // The acting card (Sengoku Rance's, docs/sengoku/04-screen.md §6.2 and §6.5; owner, 2026-10-13): its own
    // fill, and a frame that shows and hides every 500 ms.
    private const float ActingTint = 0.3f;                          // the card's colour toward the accent
    private const ulong BlinkMs = 500;

    // An order given (owner, 2026-10-15): instead of the original's message the order's label on the unit's card turns
    // saturated for a moment and back — it rises, holds, then fades.
    private const double FlashRise = 0.12, FlashHold = 0.35, FlashFade = 0.35;

    // The spotlight (Sengoku Rance's, docs/sengoku/04-screen.md §6.5; owner, 2026-10-13): while an exchange is on,
    // everything below the scene is darkened but the two cards that fight.
    private static readonly Rect2 PanelArea = new(0, 410, 1280, 390);
    private const double SpotFadeSeconds = 0.15;

    /// <summary>How dark the spotlight makes the panel (Option → Battle Screen…, 0.5 by default).</summary>
    private static float SpotDarkness => (float)BattleScreenSettings.Get(BattleScreenSettings.SpotDarkness);

    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private readonly FacePictures _faces;
    private FaceCrops _crops = new();
    private readonly Theme _theme = new();
    private readonly ColorRect _back;
    private readonly Control _sidesClip;
    private readonly TextureRect _sides;
    private readonly Control _sceneClip;                    // the scene's layers, clipped to its area
    private readonly TextureRect _field, _front;
    private readonly EffectPlayer _effect;
    private readonly ExchangeCountersView _counters;
    private int _effectsSeen;
    private readonly ArmySlide _playerArmy, _enemyArmy;
    private bool _armiesInstant, _jumpArmies = true;
    private readonly TextureRect _windows;
    private readonly SpeechBalloon _balloon;
    private readonly BattleInfoLayer _info;
    private readonly BattleCommandLayer _commands;
    private readonly BattleDialogLayer _dialog;

    /// <summary>Layout pixels to a script pixel of the windows' layer.</summary>
    private static float WindowScale => WindowsArea.Size.X / Surface.ScreenWidth;
    private readonly TextureRect _speaker;
    private readonly OrderStrip _strip;
    private readonly Control _stripClip;                                // the strip slides behind the separator (owner, 2026-10-16)
    private readonly Label _playerName, _enemyName;
    private readonly AdvantageLabels _playerAdvantage = new(), _enemyAdvantage = new();
    private readonly HBoxContainer _playerArmyLine, _enemyArmyLine;     // the army's name and its advantage over the cards
    private readonly AdvantageTip _advantageTip;
    private readonly BattleOpeningLayer _opening;
    private readonly Control _limit;                                    // the battle's limit at the scene's top (owner, 2026-10-15/16)
    private string _limitText = "";

    /// <summary>How far the battle's opening has faded in (0 none or not up, 1 whole).</summary>
    public float OpeningShown => _opening.Shown;

    /// <summary>Whether the battle's opening has left and the order strip's flags have flown in (<see cref="OrderStrip.Hold"/>).</summary>
    public bool OpeningSettled => !_opening.Visible && !_strip.Flying;
    private readonly Control _gauge;
    private readonly Dictionary<BattlePlace, (PanelContainer Card, StyleBoxFlat Box)> _cards = [];
    private readonly Control _cardsClip;
    private readonly Dictionary<BattlePlace, float> _cardTravel = [];  // how far each card slides in (layout pixels)
    private double _cardsSliding = -1;                                  // seconds into the cards' slide; -1 when they stand
    private int _cardSlides;                                            // the slides so far (each its own random stagger)
    private bool _cardsFade;                                            // a dungeon's: the cards fade in at their places instead
    private readonly Control _actingFrame;
    private BattlePlace? _actingPlace;
    private ulong _blinkFrom;
    private bool _blinkOn;
    private readonly Dictionary<int, Label> _orderLabels = [];        // the player's cards' order labels by unit record
    private int? _flashUnit;
    private ulong _flashFrom;
    private readonly Dictionary<int, ImageTexture?> _portraits = [];
    private ImageTexture? _fieldTexture, _frontTexture, _sidesTexture, _windowsTexture;
    private byte[]? _frontRgba, _windowsRgba;
    private int _sceneVersion = -1, _pictureVersion = -1;
    private string _cardsKey = "?", _stripKey = "?", _speakerKey = "?";
    private (int Rounds, int Left) _gaugeValues;
    private int _spentSegment = -1;                 // the gauge's segment that went out last, while it flashes
    private ulong _spentAt;
    private Rect2 _speakerArea = SpeakerLeft;
    private Vector2 _origin;
    private float _scale = 1;                       // window pixels to a layout pixel

    public BattleBoardView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        Theme = _theme;
        _back = new ColorRect { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_back);
        _sidesClip = new Control { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_sidesClip);
        _sides = new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            Modulate = new Color(0.45f, 0.45f, 0.45f),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _sides.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _sidesClip.AddChild(_sides);
        _sceneClip = new Control { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_sceneClip);
        _field = Layer();
        _enemyArmy = new ArmySlide(BattleSide.Enemy, Layer());              // the player's over the enemy's, as the picture draws them
        _playerArmy = new ArmySlide(BattleSide.Player, Layer());
        _effect = new EffectPlayer { HideWhenDone = true };                 // over the armies, under the numbers
        _sceneClip.AddChild(_effect);
        _front = Layer();
        _counters = new ExchangeCountersView();                             // over the front: the scene's own marks are moved up
        _sceneClip.AddChild(_counters);
        VisibilityChanged += () =>
        {
            _jumpArmies = true;                                             // shown again: the armies stand where they are
            _meters.Clear();                                                // and the counters show what they hold (another battle, perhaps)
        };

        _speaker = Picture();                                               // the portrait has its own frame and name
        _speaker.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _speaker.Visible = false;
        AddChild(_speaker);

        _limit = new Control { MouseFilter = MouseFilterEnum.Ignore, ClipContents = true };   // over the scene, under its windows; slides out over its top
        _limit.Draw += DrawLimit;
        AddChild(_limit);

        _windows = Picture();
        AddChild(_windows);
        _opening = new BattleOpeningLayer();                                // over the scene and the windows: the battle's first header
        AddChild(_opening);
        _balloon = new SpeechBalloon();                                     // over the scene's lower edge and the strip
        AddChild(_balloon);

        _stripClip = new Control { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_stripClip);
        _strip = new OrderStrip();
        _stripClip.AddChild(_strip);
        _playerName = new Label { MouseFilter = MouseFilterEnum.Ignore };
        _enemyName = new Label { MouseFilter = MouseFilterEnum.Ignore };
        _playerArmyLine = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        _enemyArmyLine = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.End };
        _playerArmyLine.AddChild(_playerName);
        foreach (var label in _playerAdvantage.Labels)
            _playerArmyLine.AddChild(label);
        foreach (var label in _enemyAdvantage.Labels)
            _enemyArmyLine.AddChild(label);
        _enemyArmyLine.AddChild(_enemyName);
        AddChild(_playerArmyLine);
        AddChild(_enemyArmyLine);
        _gauge = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _gauge.Draw += DrawGauge;
        AddChild(_gauge);
        _cardsClip = new Control { ClipContents = true, MouseFilter = MouseFilterEnum.Ignore };   // the cards slide in from beyond its edges
        AddChild(_cardsClip);
        foreach (var place in SlotSnapshot.Places)
        {
            var box = new StyleBoxFlat { BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2 };
            var card = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            card.AddThemeStyleboxOverride("panel", box);
            _cardsClip.AddChild(card);
            _cards[place] = (card, box);
        }
        _spotlight = new Control { MouseFilter = MouseFilterEnum.Ignore, ZIndex = 2 };   // over the cards and the strip (1), under the acting frame
        _spotlight.Draw += DrawSpotlight;
        AddChild(_spotlight);
        _separator = new Control { MouseFilter = MouseFilterEnum.Ignore, ZIndex = 2 };   // over the strip (1) and the spotlight, under the balloon (3)
        _separator.Draw += DrawSeparator;
        AddChild(_separator);
        _actingFrame = new Control { MouseFilter = MouseFilterEnum.Ignore, Visible = false, ZIndex = 2 };    // over the cards and the spotlight
        _actingFrame.Draw += DrawActingFrame;
        AddChild(_actingFrame);
        _info = new BattleInfoLayer();                                      // the info boxes: over the cards and the scene
        AddChild(_info);
        _commands = new BattleCommandLayer();                               // over the cards
        AddChild(_commands);
        _dialog = new BattleDialogLayer();                                  // over everything
        AddChild(_dialog);
        _advantageTip = new AdvantageTip();                                 // shown only while no dialog is
        AddChild(_advantageTip);
        SetPalette(_colours);
        SetFont(font);
    }

    private static TextureRect Picture() => new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = TextureFilterEnum.Linear,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    /// <summary>
    /// The exchange's effect (docs/ui/battle-screen.md, "The exchange's effects"): from the shots' first frame, the
    /// firing side's (<see cref="BattlePresenter.Effect"/>), mirrored for the enemy's, over the armies and under the
    /// numbers. Each start plays once; none with Option → Battle Screen… → "Effects" off, or when it would be at once
    /// (<paramref name="instant"/>: skipping, <c>--fast</c>). It plays out by itself, nothing waits for it.
    /// </summary>
    private void PlayEffect(GamePresenter presenter, bool instant)
    {
        var battle = presenter.Battle;
        if (battle.EffectsStarted == _effectsSeen)
            return;
        _effectsSeen = battle.EffectsStarted;
        if (instant || !presenter.Scene.InEngagement || BattleScreenSettings.Get(BattleScreenSettings.Effects) < 0.5
            || battle.Effect is not { } effect)
            return;
        _effect.Mirrored = effect.Side == BattleSide.Enemy;
        _effect.Dark = effect.Kind is ExchangeEffectKind.ChizukoBeam or ExchangeEffectKind.NagiBeam or ExchangeEffectKind.DarkMagic;
        _effect.Start(effect.Kind switch
        {
            ExchangeEffectKind.Bow => BowEffect.Build(_effectsSeen),
            ExchangeEffectKind.IceMagic => IceEffect.Build(_effectsSeen),
            ExchangeEffectKind.LightningMagic => LightningEffect.Build(_effectsSeen),
            ExchangeEffectKind.DarkMagic => DarkEffect.Build(_effectsSeen),
            ExchangeEffectKind.LightMagic => LightEffect.Build(_effectsSeen),
            ExchangeEffectKind.PoisonMagic => PoisonEffect.Build(_effectsSeen),
            ExchangeEffectKind.Magic => MagicEffect.Build(_effectsSeen),
            ExchangeEffectKind.RanceAttack => RanceAttackEffect.Build(_effectsSeen),
            ExchangeEffectKind.KentarouSlash => KentarouSlashEffect.Build(_effectsSeen),
            ExchangeEffectKind.ShizukaBeam => ShizukaBeamEffect.Build(_effectsSeen),
            ExchangeEffectKind.MagicBeam => MagicBeamEffect.Build(_effectsSeen),
            ExchangeEffectKind.EpoLight => EpoLightEffect.Build(_effectsSeen),
            ExchangeEffectKind.ChizukoBeam => ChizukoBeamEffect.Build(_effectsSeen),
            ExchangeEffectKind.NagiBeam => NagiBeamEffect.Build(_effectsSeen),
            ExchangeEffectKind.WhirlwindShot => WhirlwindShotEffect.Build(_effectsSeen),
            ExchangeEffectKind.HighSpeed => HighSpeedEffect.Build(_effectsSeen),
            ExchangeEffectKind.ByLaWay => RanceAttackEffect.BuildRed(_effectsSeen),
            ExchangeEffectKind.NinjaFollowUp => NinjaEffect.Build(_effectsSeen),
            ExchangeEffectKind.KanamiFollowUp => KanamiEffect.Build(_effectsSeen),
            ExchangeEffectKind.FreyaFollowUp => FreyaEffect.Build(_effectsSeen),
            ExchangeEffectKind.KaoruFollowUp => KaoruEffect.Build(_effectsSeen),
            ExchangeEffectKind.WichitaFollowUp => WichitaEffect.Build(_effectsSeen),
            ExchangeEffectKind.VanishBomb => VanishBombEffect.Build(_effectsSeen),
            ExchangeEffectKind.WarDance => WarDanceEffect.Build(_effectsSeen),
            ExchangeEffectKind.Chuhai => ChuhaiEffect.Build(_effectsSeen),
            ExchangeEffectKind.Goliath => GoliathEffect.Build(_effectsSeen),
            ExchangeEffectKind.SoaringWave => SoaringWaveEffect.Build(_effectsSeen),
            _ => MeleeEffect.Build(_effectsSeen),
        });
        _effect.Playing = true;
    }

    private int _guards;

    /// <summary>
    /// A defence (docs/ui/battle-screen.md, "The exchange's effects"): our Guard effect after Sengoku Rance's guard
    /// actions', a wall of light raised in front of the defender on <paramref name="side"/>'s half, mirrored for the
    /// enemy's — a unit's Defend (<c>FIGT.ADV:1399</c>, its badge flashing on the card), a commander's
    /// (<see cref="BattlePresenter.EnemyDefence"/>). False when it does not play: Option → Battle Screen… → "Effects"
    /// off, or the armies' moves at once (skipping, <c>--fast</c>); else it lasts <see cref="GuardEffect.Seconds"/>.
    /// </summary>
    public bool PlayGuard(BattleSide side)
    {
        if (_armiesInstant || BattleScreenSettings.Get(BattleScreenSettings.Effects) < 0.5)
            return false;
        _effect.Mirrored = side == BattleSide.Enemy;
        _effect.Dark = false;
        _effect.Start(GuardEffect.Build(++_guards));
        _effect.Playing = true;
        return true;
    }

    /// <summary>A layer of the scene (<see cref="BattleScene.FieldY"/>), over the ones before it.</summary>
    private TextureRect Layer()
    {
        var layer = Picture();
        _sceneClip.AddChild(layer);
        return layer;
    }

    /// <summary>Option → Palette: the screen in <paramref name="palette"/>'s colours at once.</summary>
    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        _theme.SetColor("font_color", "Label", palette.Text);
        _back.Color = palette.Back;
        _strip.SetPalette(palette);
        _commands.SetPalette(palette);
        _dialog.SetPalette(palette);
        _balloon.SetPalette(palette);
        _opening.SetPalette(palette);
        _playerAdvantage.SetColour(palette.Accent);
        _enemyAdvantage.SetColour(palette.Accent);
        _advantageTip.SetPalette(palette);
        _cardsKey = _stripKey = "?";
        _gauge.QueueRedraw();
        _limit.QueueRedraw();
        _separator.QueueRedraw();
    }

    /// <summary>Option → Fonts: the screen's text in the chosen font.</summary>
    public void SetFont(GameFont font)
    {
        _theme.DefaultFont = DeploymentTableView.LoadFont(font);
        _balloon.SetFont(_theme.DefaultFont);
        _info.SetFont(_theme.DefaultFont);
    }

    /// <summary>The battle's info boxes as ours (docs/ui/messages.md, §5 step 5).</summary>
    public BattleInfoLayer Info => _info;

    /// <summary>The squares of the faces: the built-in ones and the owner's edits (read as the screen opens).</summary>
    public void LoadCrops()
    {
        _crops = FaceCropFile.Load();
        _cardsKey = "?";
    }

    /// <summary>
    /// Lays the screen out over the area whose layout pixel (0, 0) is at <paramref name="origin"/> (in its
    /// parent's coordinates), <paramref name="scale"/> screen pixels to a layout pixel (1 in a 1280×800 window).
    /// </summary>
    public void Place(Vector2 origin, float scale)
    {
        if (scale != _scale)
            _cardsKey = _stripKey = "?";                                // the faces and the text at the new size
        _origin = origin;
        _scale = scale;
        void At(Control c, Rect2 area)
        {
            c.Position = origin + area.Position * scale;
            c.Size = area.Size * scale;
        }
        At(_back, new Rect2(0, 0, 1280, 800));
        At(_sidesClip, SidesArea);
        At(_sceneClip, SceneArea);
        foreach (var layer in new[] { _field, _front })
            (layer.Position, layer.Size) = (Vector2.Zero, _sceneClip.Size);
        _effect.Position = new Vector2(0, SceneArea.Size.Y - EffectPlayer.Height) * scale;     // at the scene's foot
        _effect.Scale = new Vector2(scale, scale);
        _counters.Position = Vector2.Zero;
        _counters.Layout(_sceneClip.Size, scale);
        PlaceArmies();
        At(_limit, SceneArea);
        _limit.QueueRedraw();
        At(_windows, WindowsArea);
        At(_stripClip, StripClipArea);
        PlaceStrip();
        _strip.SetScale(scale);
        At(_playerArmyLine, PlayerNameArea);
        At(_enemyArmyLine, EnemyNameArea);
        foreach (var line in new[] { _playerArmyLine, _enemyArmyLine })
            line.AddThemeConstantOverride("separation", (int)Math.Round(16 * scale));
        _opening.Position = origin + SidesArea.Position * scale;
        _opening.Layout(scale);
        At(_gauge, GaugeArea);
        At(_speaker, _speakerArea);
        At(_spotlight, new Rect2(0, 0, 1280, 800));
        At(_separator, SeparatorArea);
        _separator.QueueRedraw();
        _spotlight.QueueRedraw();
        At(_info, new Rect2(0, 0, 1280, 800));
        _info.Layout(scale);
        At(_commands, new Rect2(0, 0, 1280, 800));
        _commands.Layout(CardArea, scale);
        At(_dialog, new Rect2(0, 0, 1280, 800));
        _dialog.Layout(scale);
        At(_advantageTip, new Rect2(0, 0, 1280, 800));
        _advantageTip.Layout(scale);
        At(_balloon, new Rect2(0, 0, 1280, 800));
        _balloon.Layout(scale);
        At(_cardsClip, PanelArea);
        PlaceCards();
        if (_actingPlace is { } acting)
            At(_actingFrame, CardArea(acting));
        _theme.DefaultFontSize = Math.Max(8, (int)Math.Round(TextSize * scale));
    }

    /// <summary>
    /// Fills the screen from the presenter: the scene's picture when it changed, the windows' layer when
    /// <paramref name="screenChanged"/> (with the input screen's own windows, <paramref name="inputWindows"/>),
    /// the cards (<paramref name="hovered"/> lit), the strip, the gauge and the speaker when what they show changed.
    /// <paramref name="instant"/>: the strip's chips jump to their places; <paramref name="slidesInstant"/>: the armies and the cards' counters too.
    /// </summary>
    public void Refresh(GamePresenter presenter, IEnumerable<WindowShape> inputWindows, bool screenChanged, BattlePlace? hovered,
        bool instant, bool slidesInstant)
    {
        if (presenter.State is not { } s)
            return;
        _armiesInstant = _metersInstant = slidesInstant;
        presenter.Scene.Numbers = false;                // ours are the exchange's counters and the cards (owner, 2026-10-15, -16)
        if (presenter.Scene.Version != _sceneVersion)
        {
            _sceneVersion = presenter.Scene.Version;
            UploadScene(presenter.Scene);
        }
        PlayEffect(presenter, slidesInstant);
        _counters.Show(presenter.Battle.ExchangeCounters, presenter.Battle.EffectsStarted, slidesInstant);
        if (screenChanged || _windowsTexture is null)
        {
            var mask = presenter.BattleWindows(inputWindows);
            _windows.Visible = !mask.IsEmpty;                                  // no window: nothing to read or upload
            if (!mask.IsEmpty || _windowsTexture is null)
            {
                presenter.Compose();                                           // GameScreen leaves it to us while we cover the view
                _windowsRgba = mask.Compose(presenter.Screen, _windowsRgba);
                var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
                    false, Image.Format.Rgba8, _windowsRgba);
                Upload(ref _windowsTexture, image);
                _windows.Texture = _windowsTexture;
            }
        }
        RefreshSpeaker(s);
        _balloon.Show(presenter.Battle.Speech);
        _info.Speaking = _speaker.Visible || presenter.Battle.Speech is not null;     // the defence's caption gives way
        RefreshCards(s, presenter.Order, presenter.Battle.Strike, hovered);
        RefreshSpotlight(s, presenter.Scene);
        RefreshStrip(s, presenter.Order, instant);
        var gauge = (Math.Max(presenter.Order.Rounds, s.Bt_RoundsLeft.Value), s.Bt_RoundsLeft.Value);
        if (gauge != _gaugeValues)
        {
            if (!instant && gauge.Item1 == _gaugeValues.Rounds && gauge.Item2 < _gaugeValues.Left)
                (_spentSegment, _spentAt) = (_gaugeValues.Rounds - _gaugeValues.Left, Time.GetTicksMsec());   // the round that went out flashes
            else
                _spentSegment = -1;
            _gaugeValues = gauge;
            _gauge.QueueRedraw();
        }
        var (player, enemy) = BattleBoard.Armies(s);
        _playerName.Text = player;
        _enemyName.Text = enemy;
        var (playerAdvantage, enemyAdvantage) = BattleOpening.Effects(s);   // what the header's advantages do, all battle long (owner, 2026-10-15)
        _playerAdvantage.Set(playerAdvantage);
        _enemyAdvantage.Set(enemyAdvantage);
        string limit = BattleOpening.LimitText(s);
        if (limit != _limitText)
        {
            _limitText = limit;
            _limit.QueueRedraw();
        }
        if (_opening.Refresh(presenter.Opening, instant: slidesInstant) && !slidesInstant)
            StartCardsSlide(fade: s.Bt_Kind == 3);                              // under the opening, as Sengoku Rance's
        _strip.Hold(_opening.Standing, slidesInstant);                          // the flags fly in after the opening's click
    }

    /// <summary>
    /// The scene's 640×256 picture as its layers (<see cref="BattleScene.FieldY"/>), each read and uploaded only
    /// when it was drawn again (owner, 2026-10-13: a hovered enemy costs its army's layer alone), all of them when
    /// the palette changed: the front (the overlay and the numbers); the battlefield, and the sides from it —
    /// blurred (shrunk and grown again), without the armies; the armies (<see cref="ArmySlide"/>).
    /// </summary>
    private void UploadScene(BattleScene scene)
    {
        const int w = BattleScene.Width * Surface.Scale, h = BattleScene.Height * Surface.Scale;
        Image AsImage(byte[] rgba) => Image.CreateFromData(w, h, false, Image.Format.Rgba8, rgba);
        bool all = scene.PaletteVersion != _seen.Palette;                     // every layer shows in the new palette
        if (all || scene.FrontVersion != _seen.Front)
        {
            _frontRgba = scene.Surface.ReadLayer(0, BattleScene.FrontY, BattleScene.Width, BattleScene.Height, _frontRgba);
            Upload(ref _frontTexture, AsImage(_frontRgba));
            _front.Texture = _frontTexture;
        }
        if (all || scene.FieldVersion != _seen.Field)
        {
            _layerRgba = scene.Surface.ReadArea(0, BattleScene.FieldY, BattleScene.Width, BattleScene.Height, _layerRgba);
            Upload(ref _fieldTexture, AsImage(_layerRgba));
            _field.Texture = _fieldTexture;
            var blurred = AsImage(_layerRgba);
            blurred.Resize(w / 16, h / 16, Image.Interpolation.Bilinear);
            blurred.Resize(w, h, Image.Interpolation.Bilinear);
            Upload(ref _sidesTexture, blurred);
            _sides.Texture = _sidesTexture;
        }
        if (all || scene.EnemyLayerVersion != _seen.Enemy)
        {
            _layerRgba = scene.Surface.ReadLayer(BattleScene.EnemyArmyX, BattleScene.ArmyY, BattleScene.Width, BattleScene.Height, _layerRgba);
            _enemyArmy.Arrive(scene.EnemyArmy, AsImage(_layerRgba));
        }
        if (all || scene.PlayerLayerVersion != _seen.Player)
        {
            _layerRgba = scene.Surface.ReadLayer(0, BattleScene.ArmyY, BattleScene.Width, BattleScene.Height, _layerRgba);
            _playerArmy.Arrive(scene.PlayerArmy, AsImage(_layerRgba));
        }
        _seen = (scene.PaletteVersion, scene.FrontVersion, scene.FieldVersion, scene.EnemyLayerVersion, scene.PlayerLayerVersion);
        _pictureVersion = scene.PictureVersion;
    }

    private (int Palette, int Front, int Field, int Enemy, int Player) _seen = (-1, -1, -1, -1, -1);
    private byte[]? _layerRgba;

    private static void Upload(ref ImageTexture? texture, Image image)
    {
        if (texture is null || texture.GetWidth() != image.GetWidth() || texture.GetHeight() != image.GetHeight())
            texture = ImageTexture.CreateFromImage(image);
        else
            texture.Update(image);
    }

    /// <summary>While a bubble speaks (<see cref="BattleBoard.Speaker"/>): the speaker's whole portrait on its side (its frame and name are in the picture).</summary>
    private void RefreshSpeaker(GameState s)
    {
        var speaker = BattleBoard.Speaker(s);
        string key = speaker is { } sp ? $"{sp.Side}/{sp.FaceCg}/{sp.Name}" : "";
        if (key == _speakerKey)
            return;
        _speakerKey = key;
        if (speaker is null || Portrait(speaker.FaceCg) is not { } face)
        {
            _speaker.Visible = false;
            return;
        }
        _speaker.Texture = face;
        _speakerArea = speaker.Side == BattleSide.Player ? SpeakerLeft : SpeakerRight;
        _speaker.Position = _origin + _speakerArea.Position * _scale;
        _speaker.Size = _speakerArea.Size * _scale;
        _speaker.Visible = true;
    }

    private ImageTexture? Portrait(int cg)
    {
        if (!_portraits.TryGetValue(cg, out var texture))
        {
            texture = _faces.Picture(cg) is { } image ? ImageTexture.CreateFromImage(image) : null;
            _portraits[cg] = texture;
        }
        return texture;
    }

    // ------------------------------------------------------------------ the armies' slides

    /// <summary>
    /// Whether the armies stand where the scene has them (its latest picture taken, no slide under way): the
    /// exchange waits for it (<see cref="GameScreen"/>), the player's choice does not.
    /// </summary>
    public bool ArmiesSettled(BattleScene scene) => scene.PictureVersion == _pictureVersion && _playerArmy.Settled && _enemyArmy.Settled;

    /// <summary>The armies' slides go on (<see cref="BattleScreenSettings.ArmySeconds"/>; at once while skipping and when the screen is shown again).</summary>
    private void StepArmies(double delta)
    {
        double seconds = BattleScreenSettings.Get(BattleScreenSettings.ArmySeconds);
        bool instant = _armiesInstant || _jumpArmies || seconds <= 0;
        _jumpArmies = false;
        float amount = instant ? 1 : (float)(delta / seconds);
        _playerArmy.Step(amount, instant);
        _enemyArmy.Step(amount, instant);
        PlaceArmies();
    }

    private void PlaceArmies()
    {
        float unit = _sceneClip.Size.X / BattleScene.Width;              // window pixels to a script pixel of the scene
        _playerArmy.Place(_sceneClip.Size, unit);
        _enemyArmy.Place(_sceneClip.Size, unit);
    }

    /// <summary>
    /// One side's army in the scene, sliding as Sengoku Rance's (docs/sengoku/04-screen.md §2.3; owner, 2026-10-13):
    /// horizontally and evenly, from just outside the scene on its side (the player's left, the enemy's right) into
    /// place and back. A new army (a new <see cref="BattleScene.Army.Arrival"/>, even of the same record: the armies
    /// parted after an exchange) waits for the one shown to slide out, then slides in; the same arrival with other
    /// sprites (fewer troops, an exchange's) changes in place.
    /// </summary>
    private sealed class ArmySlide(BattleSide side, TextureRect rect)
    {
        private BattleScene.Army _shown, _target;
        private Image? _pending;                                    // the target's picture while the shown army leaves
        private ImageTexture? _texture;
        private float _in;                                          // 1: in place; 0: its sprites' edge at the scene's

        public bool Settled => _shown.Arrival == _target.Arrival && (_shown.Record == 0 || _in >= 1);

        /// <summary>The scene's picture of this side changed: <paramref name="army"/> drawn in <paramref name="picture"/>.</summary>
        public void Arrive(BattleScene.Army army, Image picture)
        {
            _target = army;
            if (army.Arrival == _shown.Arrival)
            {
                _shown = army;
                _pending = null;
                Show(picture);
            }
            else
            {
                _pending = picture;
            }
        }

        /// <summary>Moves by <paramref name="amount"/> of a whole slide.</summary>
        public void Step(float amount, bool instant)
        {
            if (_shown.Arrival != _target.Arrival)
            {
                if (_shown.Record != 0 && _in > 0 && !instant)
                {
                    _in = Math.Max(0, _in - amount);
                    return;
                }
                _shown = _target;
                if (_pending is not null)
                    Show(_pending);
                _pending = null;
                _in = instant ? 1 : 0;
            }
            else if (_shown.Record != 0 && _in < 1)
            {
                _in = instant ? 1 : Math.Min(1, _in + amount);
            }
        }

        public void Place(Vector2 size, float unit)
        {
            float outside = side == BattleSide.Player ? -_shown.Right : BattleScene.Width - _shown.Left;
            rect.Position = new Vector2((float)Math.Round((1 - _in) * outside * unit), 0);
            rect.Size = size;
        }

        private void Show(Image picture)
        {
            if (_texture is null || _texture.GetWidth() != picture.GetWidth() || _texture.GetHeight() != picture.GetHeight())
                _texture = ImageTexture.CreateFromImage(picture);
            else
                _texture.Update(picture);
            rect.Texture = _texture;
        }
    }

    // ------------------------------------------------------------------ the spotlight

    private readonly Control _spotlight;
    private readonly Control _separator;
    private readonly List<Fighter> _fighters = [];                   // the fighters' cards (layout pixels) and actions
    private float _spotShown;                                        // 0: off, 1: whole
    private float _spotDrawnDarkness;
    private bool _spotOn;

    /// <summary>A fighter's card in the spotlight and what it does in the exchange (<see cref="ExchangeAction.None"/> until known).</summary>
    private readonly record struct Fighter(Rect2 Card, BattleSide Side, ExchangeAction Action);

    /// <summary>
    /// While an exchange is on (<see cref="BattleScene.InEngagement"/>) the spotlight falls on its two fighters'
    /// cards — the player's unit (<c>Unit_Cur</c>) and the enemy's commander (<c>Cmdr_Cur</c>) — each with a
    /// pill naming its action (<see cref="BattleScene.Actions"/>; owner, 2026-10-13, in place of the "vs" window).
    /// </summary>
    private void RefreshSpotlight(GameState s, BattleScene scene)
    {
        bool on = scene.Active && scene.InEngagement;
        var fighters = new List<Fighter>(2);
        if (on)
        {
            foreach (var place in SlotSnapshot.Places)
            {
                int fighter = place.Side == BattleSide.Player ? s.Unit_Cur.Value : s.Cmdr_Cur.Value;
                if (fighter != 0 && BattleBoard.Record(s, place) == fighter)
                    fighters.Add(new Fighter(CardArea(place), place.Side,
                        place.Side == BattleSide.Player ? scene.Actions.Player : scene.Actions.Enemy));
            }
        }
        if (on == _spotOn && (!on || fighters.SequenceEqual(_fighters)))
            return;
        _spotOn = on;
        if (on)
        {
            _fighters.Clear();
            _fighters.AddRange(fighters);                                // when it goes off, they stay while it fades
        }
        _spotlight.QueueRedraw();
    }

    /// <summary>The spotlight fades in and out (<see cref="SpotFadeSeconds"/>; at once while skipping and with <c>--fast</c>).</summary>
    private void StepSpotlight(double delta)
    {
        if (_spotShown > 0 && SpotDarkness != _spotDrawnDarkness)
            _spotlight.QueueRedraw();                                    // the option moved while it shows
        float target = _spotOn ? 1 : 0;
        if (_spotShown == target)
            return;
        float step = _armiesInstant ? 1 : (float)(delta / SpotFadeSeconds);
        _spotShown = target > _spotShown ? Math.Min(target, _spotShown + step) : Math.Max(target, _spotShown - step);
        _spotlight.QueueRedraw();
    }

    /// <summary>Black at <see cref="SpotDarkness"/> over the panel below the scene, but over the fighters' cards.</summary>
    private void DrawSpotlight()
    {
        _spotDrawnDarkness = SpotDarkness;
        if (_spotShown <= 0)
            return;
        var colour = new Color(0, 0, 0, _spotDrawnDarkness * _spotShown);
        IEnumerable<Rect2> dark = [PanelArea];
        foreach (var fighter in _fighters)
            dark = dark.SelectMany(r => Without(r, fighter.Card)).ToList();
        foreach (var r in dark)
            _spotlight.DrawRect(new Rect2(r.Position * _scale, r.Size * _scale), colour);
        foreach (var fighter in _fighters)
        {
            if (fighter.Action != ExchangeAction.None)
                DrawPill(fighter);
        }
    }

    // The action's pill (owner, 2026-10-13, mock-up D): under the card's state line, at the text column's left.
    private const float PillHeight = 24, PillBottom = 8, PillText = 16, PillIcon = 16;
    private static readonly Color PillInk = new(0.12f, 0.09f, 0.05f);

    /// <summary>A pill in the accent colour on the fighter's card: the action's icon and our name for it (<see cref="BattleCommands.ActionName"/>).</summary>
    private void DrawPill(Fighter fighter)
    {
        float s = _scale;
        var font = _theme.DefaultFont;
        int size = Math.Max(8, (int)Math.Round(PillText * s));
        string text = BattleCommands.ActionName(fighter.Action);
        float textWidth = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        float column = fighter.Side == BattleSide.Player ? 4 + FaceSize + 8 : 4;   // the text column: padding, face, separation
        float h = PillHeight * s, icon = PillIcon * s, gap = 6 * s, side = 9 * s;
        var rect = new Rect2((fighter.Card.Position.X + column) * s, (fighter.Card.End.Y - PillBottom - PillHeight) * s,
            side + icon + gap + textWidth + side, h);
        var box = new StyleBoxFlat { BgColor = _colours.Accent with { A = _spotShown } };
        box.SetCornerRadiusAll((int)(h / 2));
        _spotlight.DrawStyleBox(box, rect);
        var ink = PillInk with { A = _spotShown };
        DrawActionIcon(fighter.Action, new Vector2(rect.Position.X + side + icon / 2, rect.GetCenter().Y), icon, ink);
        float baseline = rect.GetCenter().Y + (font.GetAscent(size) - font.GetDescent(size)) / 2;
        _spotlight.DrawString(font, new Vector2(rect.Position.X + side + icon + gap, baseline), text,
            HorizontalAlignment.Left, -1, size, ink);
    }

    /// <summary>The action's icon, <paramref name="size"/> across at <paramref name="c"/>: a sword, a bow, a star, a burst, a shield.</summary>
    private void DrawActionIcon(ExchangeAction action, Vector2 c, float size, Color ink)
    {
        float k = size / 2, w = Math.Max(1.5f, size / 7);
        Vector2 P(float x, float y) => c + new Vector2(x, y) * k;
        Vector2[] Star(int points, float inner) => [.. Enumerable.Range(0, points * 2).Select(i =>
        {
            float a = -MathF.PI / 2 + i * MathF.PI / points, r = i % 2 == 0 ? 1 : inner;
            return P(r * MathF.Cos(a), r * MathF.Sin(a));
        })];
        switch (action)
        {
            case ExchangeAction.Melee:                                   // a sword, point up and to the right
                _spotlight.DrawLine(P(-0.85f, 0.85f), P(0.9f, -0.9f), ink, w * 1.2f, antialiased: true);
                _spotlight.DrawLine(P(-0.85f, 0.05f), P(-0.05f, 0.85f), ink, w * 1.2f, antialiased: true);
                break;
            case ExchangeAction.Bow:                                     // a bow and its arrow
                _spotlight.DrawArc(P(0.35f, 0), k * 1.05f, MathF.PI * 0.62f, MathF.PI * 1.38f, 12, ink, w, antialiased: true);
                _spotlight.DrawLine(P(-0.04f, -0.97f), P(-0.04f, 0.97f), ink, w * 0.6f, antialiased: true);   // the string, end to end
                _spotlight.DrawLine(P(-0.95f, 0), P(0.95f, 0), ink, w, antialiased: true);
                _spotlight.DrawColoredPolygon([P(1f, 0), P(0.55f, -0.3f), P(0.55f, 0.3f)], ink);
                break;
            case ExchangeAction.Magic:                                   // a star
                _spotlight.DrawColoredPolygon(Star(5, 0.42f), ink);
                break;
            case ExchangeAction.Special:                                 // a burst
                _spotlight.DrawColoredPolygon(Star(8, 0.5f), ink);
                break;
            case ExchangeAction.Defense:                                 // a shield
                _spotlight.DrawColoredPolygon([P(-0.8f, -0.85f), P(0.8f, -0.85f), P(0.8f, 0.05f), P(0, 0.95f), P(-0.8f, 0.05f)], ink);
                break;
        }
    }

    /// <summary><paramref name="r"/> without <paramref name="hole"/>: up to four rectangles (above, below, left, right of it).</summary>
    private static IEnumerable<Rect2> Without(Rect2 r, Rect2 hole)
    {
        var cut = r.Intersection(hole);
        if (!cut.HasArea())
        {
            yield return r;
            yield break;
        }
        if (cut.Position.Y > r.Position.Y)
            yield return new Rect2(r.Position.X, r.Position.Y, r.Size.X, cut.Position.Y - r.Position.Y);
        if (cut.End.Y < r.End.Y)
            yield return new Rect2(r.Position.X, cut.End.Y, r.Size.X, r.End.Y - cut.End.Y);
        if (cut.Position.X > r.Position.X)
            yield return new Rect2(r.Position.X, cut.Position.Y, cut.Position.X - r.Position.X, cut.Size.Y);
        if (cut.End.X < r.End.X)
            yield return new Rect2(cut.End.X, cut.Position.Y, r.End.X - cut.End.X, cut.Size.Y);
    }

    // ------------------------------------------------------------------ the cards

    /// <summary>
    /// The cards, built again only when what they show changed; the pointer moving over them changes their
    /// frames alone (owner, 2026-10-13: hovering must not cost frames).
    /// </summary>
    private void RefreshCards(GameState s, RoundOrder order, BattleStrike? strike, BattlePlace? hovered)
    {
        _strike = strike;
        var states = order.States(s);                                    // at a round header, the round to come
        var cards = SlotSnapshot.Places.Select(p => BattleBoard.Card(s, p, states))
            .Select(c => c is not null && strike is not null ? strike.Apply(c) : c)   // the blow on the fighters, from the moment it lands
            .ToList();
        string key = string.Join(";", cards) + $"|{order.Acting}";
        if (key == _cardsKey)
        {
            if (hovered != _cardsHovered)
            {
                _cardsHovered = hovered;
                for (int i = 0; i < cards.Count; i++)
                    _cards[SlotSnapshot.Places[i]].Box.BorderColor = Border(SlotSnapshot.Places[i], cards[i], acting: SlotSnapshot.Places[i] == _actingPlace);
            }
            return;
        }
        _cardsKey = key;
        _cardsHovered = hovered;
        _orderLabels.Clear();
        BattlePlace? actingPlace = null;
        for (int i = 0; i < cards.Count; i++)
        {
            var place = SlotSnapshot.Places[i];
            var (panel, box) = _cards[place];
            var card = cards[i];
            bool acting = order.Acting == place && card is not null && card.Record == order.ActingRecord;
            if (acting)
                actingPlace = place;
            box.BgColor = card is null ? _colours.Back : acting ? _colours.Card.Lerp(_colours.Accent, ActingTint) : _colours.Card;
            box.BorderColor = Border(place, card, acting);
            int pad = Pad;
            box.ContentMarginLeft = box.ContentMarginRight = box.ContentMarginTop = box.ContentMarginBottom = pad;
            foreach (var child in panel.GetChildren())
            {
                panel.RemoveChild(child);
                child.QueueFree();
            }
            panel.AddChild(card is null ? Empty(place) : Contents(card, acting, pad));
        }
        if (actingPlace != _actingPlace)
        {
            _actingPlace = actingPlace;
            (_blinkFrom, _blinkOn) = (Time.GetTicksMsec(), true);        // a new actor: the frame shown first
        }
        _actingFrame.Visible = actingPlace is not null;
        if (actingPlace is { } a)
        {
            var area = CardArea(a);
            _actingFrame.Position = _origin + area.Position * _scale;
            _actingFrame.Size = area.Size * _scale;
        }
        _actingFrame.QueueRedraw();
    }

    private BattlePlace? _cardsHovered;

    /// <summary>A card's frame: the accent while it acts, lit under the pointer (<see cref="_cardsHovered"/>), else plain.</summary>
    private Color Border(BattlePlace place, BattleCard? card, bool acting) =>
        acting ? _colours.Accent : _cardsHovered == place && card is not null ? _colours.ButtonBorder : _colours.Border;

    /// <summary>The cards' padding inside their frame, in window pixels: four units of the acting frame.</summary>
    private int Pad => Math.Max(1, (int)Math.Round(4 * _scale));

    /// <summary>
    /// The tooltip of the advantage under the pointer (owner, 2026-10-15): at once, while the screen shows, the battle's
    /// opening is gone and no dialog is up.
    /// </summary>
    /// <summary>Unattended play: the layout centres of the advantages shown, the player's then the enemy's.</summary>
    public IEnumerable<Vector2> AdvantageCentres() =>
        _playerAdvantage.Centres().Concat(_enemyAdvantage.Centres()).Select(g => ToLayout(g - GlobalPosition)).ToList();

    /// <summary>Whether the advantages' tooltip is up.</summary>
    public bool AdvantageTipShown => _advantageTip.Visible;

    private void StepAdvantageTip()
    {
        string? tip = null;
        if (IsVisibleInTree() && _opening.Shown <= 0 && !_dialog.Active)
        {
            var pointer = GetGlobalMousePosition();
            tip = _playerAdvantage.TipAt(pointer) ?? _enemyAdvantage.TipAt(pointer);
        }
        _advantageTip.ShowAt(tip);
    }

    /// <summary>
    /// A side's advantages next to its army's name (<see cref="BattleAdvantage.Parts"/>), each its own label so that each
    /// has its own tooltip: a part, the dot between two, a part.
    /// </summary>
    private sealed class AdvantageLabels
    {
        public readonly Label[] Labels = [new(), new() { Text = "·" }, new()];
        private readonly string[] _tips = ["", ""];
        private BattleAdvantage? _shown;

        public AdvantageLabels()
        {
            foreach (var label in Labels)
            {
                label.MouseFilter = MouseFilterEnum.Ignore;
                label.Visible = false;
            }
        }

        public void SetColour(Color colour)
        {
            foreach (var label in Labels)
                label.AddThemeColorOverride("font_color", colour);
        }

        public void Set(BattleAdvantage advantage)
        {
            if (advantage == _shown)
                return;
            _shown = advantage;
            var parts = advantage.Parts;
            for (int i = 0; i < 2; i++)
            {
                var label = Labels[i * 2];
                label.Visible = i < parts.Count;
                (label.Text, _tips[i]) = i < parts.Count ? parts[i] : ("", "");
            }
            Labels[1].Visible = parts.Count == 2;
        }

        /// <summary>The global centres of the parts shown.</summary>
        public IEnumerable<Vector2> Centres() =>
            new[] { Labels[0], Labels[2] }.Where(l => l.Visible).Select(l => l.GetGlobalRect().GetCenter());

        /// <summary>The tooltip of the part at <paramref name="global"/> (a global point), null off them.</summary>
        public string? TipAt(Vector2 global)
        {
            for (int i = 0; i < 2; i++)
                if (Labels[i * 2].Visible && Labels[i * 2].GetGlobalRect().HasPoint(global))
                    return _tips[i];
            return null;
        }
    }

    /// <summary>
    /// The cards' slide when a battle begins (owner, 2026-10-15; Sengoku Rance's, docs/sengoku/04-screen.md §6.5): each
    /// from beyond its own side, linear, the front row in Option → Battle Screen… → "Cards: slide in" (640 layout pixels;
    /// Sengoku Rance's 400 px in 1 s), each card a random distance further (the back row 160 more), so they come at
    /// slightly different times. The stagger is our own random draw, never the script's. In a dungeon
    /// (<paramref name="fade"/>, <c>Bt_Kind</c> 3) the cards stand at their places and come out of the dark instead
    /// (owner, 2026-10-21): each grows opaque in the time its slide would take.
    /// </summary>
    private void StartCardsSlide(bool fade)
    {
        if (BattleScreenSettings.Get(BattleScreenSettings.CardsSlideSeconds) <= 0)
            return;
        var random = new Random(++_cardSlides);
        foreach (var place in SlotSnapshot.Places)
            _cardTravel[place] = CardsSlideDistance + (place.Slot <= 2 ? 0 : 160) + random.Next(160);
        _cardsFade = fade;
        _cardsSliding = 0;
        PlaceCards();
    }

    private const float CardsSlideDistance = 640;

    private void StepCards(double delta)
    {
        if (_cardsSliding < 0)
            return;
        _cardsSliding += delta;
        double seconds = BattleScreenSettings.Get(BattleScreenSettings.CardsSlideSeconds);
        if (seconds <= 0 || _cardTravel.Values.All(d => _cardsSliding * CardsSlideDistance / seconds >= d))
            _cardsSliding = -1;
        PlaceCards();
    }

    /// <summary>The cards at their places, or where their slide has them (in a dungeon, as opaque as their fade has them).</summary>
    private void PlaceCards()
    {
        double seconds = BattleScreenSettings.Get(BattleScreenSettings.CardsSlideSeconds);
        foreach (var (place, (card, _)) in _cards)
        {
            float left = 0, alpha = 1;
            if (_cardsSliding >= 0 && seconds > 0 && _cardTravel.TryGetValue(place, out float travel))
            {
                if (_cardsFade)
                    alpha = (float)Math.Min(1, _cardsSliding * CardsSlideDistance / seconds / travel);
                else
                    left = (float)Math.Max(0, travel - _cardsSliding * CardsSlideDistance / seconds);
            }
            var at = CardArea(place).Position - PanelArea.Position + new Vector2(place.Side == BattleSide.Player ? -left : left, 0);
            card.Position = at * _scale;
            card.Size = CardSize * _scale;
            card.Modulate = new Color(1, 1, 1, alpha);
        }
    }

    public override void _Process(double delta)
    {
        StepCards(delta);
        if (GaugeMoves && _gauge.IsVisibleInTree())
            _gauge.QueueRedraw();
        _limit.Modulate = new Color(1, 1, 1, 1 - _opening.Cover);     // the opening's plate says the same while it covers the scene
        StepArmies(delta);
        StepSpotlight(delta);
        StepAway(delta);
        StepMeters();
        StepOrderFlash();
        StepAdvantageTip();
        if (_actingPlace is null)
            return;
        bool on = (Time.GetTicksMsec() - _blinkFrom) / BlinkMs % 2 == 0;
        if (on == _blinkOn)
            return;
        _blinkOn = on;
        _actingFrame.QueueRedraw();
    }

    /// <summary>
    /// The acting card's frame, in units of a quarter of the padding (a pixel of Sengoku Rance's card): the
    /// card's own edge black, accent, accent, black from the outside in; while the blinking frame shows, it
    /// covers that edge one unit further out — accent, white, white, accent — and leaves the inner black.
    /// </summary>
    private void DrawActingFrame()
    {
        float u = Pad / 4f;
        var card = new Rect2(Vector2.Zero, _actingFrame.Size);
        void Ring(int at, Color colour)                               // the ring `at` units in from the card's edge
        {
            var r = card.Grow(-at * u);
            _actingFrame.DrawRect(new Rect2(r.Position, new Vector2(r.Size.X, u)), colour);
            _actingFrame.DrawRect(new Rect2(r.Position.X, r.End.Y - u, r.Size.X, u), colour);
            _actingFrame.DrawRect(new Rect2(r.Position.X, r.Position.Y + u, u, r.Size.Y - 2 * u), colour);
            _actingFrame.DrawRect(new Rect2(r.End.X - u, r.Position.Y + u, u, r.Size.Y - 2 * u), colour);
        }
        Color[] edge = [Colors.Black, _colours.Accent, _colours.Accent, Colors.Black];
        for (int i = 0; i < edge.Length; i++)
            Ring(i, edge[i]);
        if (!_blinkOn)
            return;
        Color[] blink = [_colours.Accent, Colors.White, Colors.White, _colours.Accent];
        for (int i = 0; i < blink.Length; i++)
            Ring(i - 1, blink[i]);
    }

    /// <summary>
    /// An order message the script would show for <paramref name="unit"/> (<see cref="BattlePresenter.OrderedUnit"/>):
    /// the order's label on its card flashes instead — the card built again while it flashes keeps it.
    /// </summary>
    public void FlashOrder(int unit)
    {
        RestoreOrderLabel();
        (_flashUnit, _flashFrom) = (unit, Time.GetTicksMsec());
        StepOrderFlash();
    }

    private void StepOrderFlash()
    {
        if (_flashUnit is not { } unit)
            return;
        if ((Time.GetTicksMsec() - _flashFrom) / 1000.0 >= FlashRise + FlashHold + FlashFade)
        {
            RestoreOrderLabel();
            return;
        }
        if (_orderLabels.TryGetValue(unit, out var label) && IsInstanceValid(label))
            label.AddThemeColorOverride("font_color", FlashColour());
    }

    private void RestoreOrderLabel()
    {
        if (_flashUnit is { } unit && _orderLabels.TryGetValue(unit, out var label) && IsInstanceValid(label))
            label.AddThemeColorOverride("font_color", _colours.Muted);
        _flashUnit = null;
    }

    /// <summary>The flashing label's colour now: the muted colour toward its saturated self and back.</summary>
    private Color FlashColour()
    {
        double t = (Time.GetTicksMsec() - _flashFrom) / 1000.0;
        float k = t < FlashRise ? (float)(t / FlashRise)
            : t < FlashRise + FlashHold ? 1
            : (float)Math.Max(0, 1 - (t - FlashRise - FlashHold) / FlashFade);
        return _colours.Muted.Lerp(Saturated(_colours.Muted, _colours.Text), k * k * (3 - 2 * k));
    }

    /// <summary>
    /// <paramref name="muted"/> made vivid: its hue at full saturation, as bright as the text on a dark card (as dark on a
    /// light one); a grey without a hue of its own takes the accent's.
    /// </summary>
    private Color Saturated(Color muted, Color text)
    {
        muted.ToHsv(out float h, out float s, out float v);
        if (s < 0.02f)
            _colours.Accent.ToHsv(out h, out _, out _);
        bool dark = text.Luminance > muted.Luminance;
        return Color.FromHsv(h, 0.85f, dark ? 1f : Math.Min(v, 0.55f));
    }

    private Label Empty(BattlePlace place) => Line($"{place.Slot}  empty", _colours.Muted, center: true);

    /// <summary>
    /// A card: the portrait with the unit's stats (<see cref="CardPortrait"/>; the enemy's on the right), place and name,
    /// troop type, troops and HP with bars, the player's standing order, "Acting" / "Acted".
    /// </summary>
    private Control Contents(BattleCard card, bool acting, int pad)
    {
        var line = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        line.AddThemeConstantOverride("separation", pad * 2);
        int size = (int)Math.Round(CardPortrait.Face * _scale);
        var face = new CardPortrait(card.Picture != 0 ? _faces.SquareTexture(card.Picture, _faces.CropOf(card.Picture, _crops), size) : null,
            card.Stats, card.Focus, _colours, _scale);

        var text = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 0);
        var name = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        name.AddThemeConstantOverride("separation", pad * 2);
        name.AddChild(Line(card.Place.Slot.ToString(), _colours.Muted, expand: false));
        name.AddChild(Line(card.Name));
        text.AddChild(name);
        text.AddChild(Line(card.TroopType == "" ? "—" : card.TroopType, _colours.Muted));
        var side = card.Place.Side == BattleSide.Player ? PlayerColour : EnemyColour;
        foreach (var (gauge, colour) in new[] { (Gauge.Troops, side), (Gauge.Hp, HpColour) })
        {
            var label = Line("");
            var (bar, trail, fill) = Bar(colour);
            text.AddChild(label);
            text.AddChild(bar);
            Track(card, gauge, label, trail, fill);                         // the number and the bar, running to a new value
        }
        string order = card.Order switch
        {
            StandingOrder.AutoAttack => "Auto-Atk",
            StandingOrder.AutoDefend => "Auto-Def",
            StandingOrder.Manual => "Manual",
            _ => "",
        };
        string state = "";
        if (card.Defending)
            state += (state == "" ? "" : " · ") + "Defend";      // "Manual · Defending" overran the line (owner, 2026-10-21)
        if (acting)
            state += (state == "" ? "" : " · ") + "Acting";
        else if (card.Acted && !card.Defending)
            state += (state == "" ? "" : " · ") + "Acted";
        var stateLine = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        stateLine.AddThemeConstantOverride("separation", 0);
        if (order != "")
        {
            var orderLabel = Line(order, _colours.Muted, expand: false);   // its own label: it flashes (FlashOrder)
            stateLine.AddChild(orderLabel);
            if (card.Place.Side == BattleSide.Player)
            {
                _orderLabels[card.Record] = orderLabel;
                if (card.Record == _flashUnit)
                    orderLabel.AddThemeColorOverride("font_color", FlashColour());
            }
            if (state != "")
                state = " · " + state;
        }
        stateLine.AddChild(Line(state, _colours.Muted));
        text.AddChild(stateLine);

        if (card.Place.Side == BattleSide.Player)
        {
            line.AddChild(face);
            line.AddChild(text);
        }
        else
        {
            line.AddChild(text);
            line.AddChild(face);
        }
        return line;
    }

    /// <summary>A bar, its damage trail and its fill (their shares are their right anchors, set by <see cref="Track"/>).</summary>
    private (Control Bar, Control Trail, Control Fill) Bar(Color colour)
    {
        var back = new ColorRect
        {
            Color = _colours.Back,
            CustomMinimumSize = new Vector2(0, Math.Max(2, (float)Math.Round(5 * _scale))),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var trail = new ColorRect { Color = colour.Lerp(Colors.White, TrailLight), MouseFilter = MouseFilterEnum.Ignore };
        var fill = new ColorRect { Color = colour, MouseFilter = MouseFilterEnum.Ignore };
        foreach (var part in new[] { trail, fill })                         // the fill over the trail
        {
            part.AnchorBottom = 1;
            back.AddChild(part);
        }
        return (back, trail, fill);
    }

    private static Label Line(string text, Color? color = null, bool expand = true, bool center = false)
    {
        var label = new Label
        {
            Text = text,
            ClipText = expand,
            TextOverrunBehavior = expand ? TextServer.OverrunBehavior.TrimEllipsis : TextServer.OverrunBehavior.NoTrimming,
            SizeFlagsHorizontal = expand ? SizeFlags.ExpandFill : SizeFlags.Fill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        if (center)
        {
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            label.SizeFlagsVertical = SizeFlags.ExpandFill;
        }
        if (color is { } c)
            label.AddThemeColorOverride("font_color", c);
        return label;
    }

    // ------------------------------------------------------------------ the strip and the gauge

    /// <summary>
    /// The round's order (<see cref="OrderStrip"/>): the chips move on their own; at once while
    /// <paramref name="instant"/> (the message skip), when a new round begins (owner, 2026-10-12: the
    /// units that acted go back into the order, no jumps) and while the strip is out of the exchange's way
    /// (owner, 2026-10-21: it comes back with the new order; a move after its return came while the next turn had begun).
    /// </summary>
    private void RefreshStrip(GameState s, RoundOrder order, bool instant)
    {
        var chips = order.Chips(s);
        string key = string.Join(";", chips);
        if (key == _stripKey && (order.Battle, order.Round) == _stripRound)
            return;
        _stripKey = key;
        bool newRound = (order.Battle, order.Round) != _stripRound;     // a new round, or a new battle's first
        _stripRound = (order.Battle, order.Round);
        _strip.Show(chips, instant || newRound || _spotOn || _away > 0);
    }

    // ------------------------------------------------------------------ out of the exchange's way

    // While an exchange is on (the spotlight's signal, BattleScene.InEngagement; owner, 2026-10-16, as Sengoku Rance's) the
    // limit's plate slides up out over the screen's top and the order strip down behind the separator; they come back when
    // it is over, the strip with the order's change already in place (RefreshStrip). Eased, in AwaySeconds; at once while
    // skipping and with --fast.
    private const double AwaySeconds = 0.3;
    private static readonly Rect2 StripClipArea = new(0, 0, 1280, 408);   // down to the separator's top
    private const float StripAway = 408 - 374 + 2;                        // the strip's top below the clip's foot
    private const float LimitAway = 8 + 28 + 12;                          // the plate's foot above the scene's top, its shadow too
    private float _away;                                                  // 0: in place, 1: out of the way

    private static float Eased(float x) => x * x * (3 - 2 * x);

    private void StepAway(double delta)
    {
        float target = _spotOn ? 1 : 0;
        if (_away != target)
        {
            float step = _armiesInstant ? 1 : (float)(delta / AwaySeconds);
            _away = target > _away ? Math.Min(target, _away + step) : Math.Max(target, _away - step);
            PlaceStrip();
            _limit.QueueRedraw();
        }
    }

    /// <summary>The strip at its place in its clip, or as far down as it is out of the way.</summary>
    private void PlaceStrip()
    {
        _strip.Position = (StripArea.Position - StripClipArea.Position + new Vector2(0, Eased(_away) * StripAway)) * _scale;
        _strip.Size = StripArea.Size * _scale;
    }

    private (int Battle, int Round) _stripRound = (-1, -1);

    // The rounds gauge (owner, 2026-10-15, mock-up A of three, after Sengoku Rance's time gauge, docs/sengoku/04-screen.md §7):
    // a recessed tube in the frame's colour, a glowing segment per round left, lit from the bottom, the spent ones dark
    // slots from the top; the round under way pulses slowly, a round that goes out flashes white and fades; the number
    // left below in Cinzel and "TURNS LEFT", on the last round both in the warning colour ("LAST TURN"). Layout pixels.
    private const float TubeWidth = 48, TubeHeight = 238, TubeRadius = 12, SegmentWidth = 32, SegmentPad = 8, SegmentGap = 6;
    private const float PulseSeconds = 2, SpentFlashSeconds = 0.4f;
    private static readonly Color TubeFill = Color.Color8(24, 25, 28), SlotFill = Color.Color8(36, 37, 41), SlotEdge = Color.Color8(70, 72, 78);

    /// <summary>Whether the gauge animates now (the round under way pulses, or a spent one flashes).</summary>
    private bool GaugeMoves => _gaugeValues.Left > 0 || _spentSegment >= 0;

    private void DrawGauge()
    {
        var (rounds, left) = _gaugeValues;
        float s = _scale;
        float cx = _gauge.Size.X / 2;
        bool last = left == 1;
        var lit = last ? _colours.Warning : _colours.Accent;
        var tube = new Rect2(cx - TubeWidth / 2 * s, 0, TubeWidth * s, TubeHeight * s);
        var box = new StyleBoxFlat { BgColor = TubeFill, BorderColor = _colours.Border, AntiAliasing = true };
        box.SetCornerRadiusAll((int)Math.Round(TubeRadius * s));
        box.SetBorderWidthAll(Math.Max(1, (int)Math.Round(2 * s)));
        _gauge.DrawStyleBox(box, tube);
        _gauge.DrawRect(new Rect2(tube.Position + new Vector2(4, 3) * s, new Vector2(tube.Size.X - 8 * s, 7 * s)), new Color(0, 0, 0, 0.35f));
        if (rounds > 0)
        {
            float height = (TubeHeight - 2 * SegmentPad - SegmentGap * (rounds - 1)) / rounds;
            double pulse = 0.5 - 0.5 * Math.Cos(Time.GetTicksMsec() / 1000.0 * 2 * Math.PI / PulseSeconds);
            double flash = _spentSegment >= 0 ? (Time.GetTicksMsec() - _spentAt) / 1000.0 / SpentFlashSeconds : 1;
            if (flash >= 1)
                _spentSegment = -1;
            for (int i = 0; i < rounds; i++)
            {
                var r = new Rect2(cx - SegmentWidth / 2 * s, (SegmentPad + i * (height + SegmentGap)) * s, SegmentWidth * s, height * s);
                if (i < rounds - left)
                {
                    DrawSlot(r);
                    if (i == _spentSegment)                                     // going out: white, then fading into the slot
                        DrawSegment(r, Colors.White.Lerp(lit, (float)Math.Min(1, flash * 2)), 1 - (float)flash);
                    continue;
                }
                if (i != rounds - left)
                {
                    DrawSegment(r, lit, 1);
                    continue;
                }
                // The round under way (owner, 2026-10-15: the pulse was hardly seen): from a little darker than the others
                // to nearly white, its halo growing and its edge thickening.
                float p = (float)pulse;
                DrawSegment(r, lit.Darkened(0.2f * (1 - p)).Lerp(Colors.White, 0.55f * p), 1, 0.4f + 0.6f * p, 8 + 10 * p, 1 + 1.5f * p);
            }
        }
        var font = _opening.CinzelBold;
        var colour = last ? _colours.Warning : _colours.Accent;
        int number = (int)Math.Round(34 * s), caption = Math.Max(8, (int)Math.Round(11 * s));
        float y = (TubeHeight + 24) * s;
        string digits = left.ToString();
        float baseline = y + (font.GetAscent(number) - font.GetDescent(number)) / 2;
        _gauge.DrawStringOutline(font, new Vector2(0, baseline), digits, HorizontalAlignment.Center, _gauge.Size.X, number,
            Math.Max(2, (int)Math.Round(4 * s)), Color.Color8(20, 20, 24));
        _gauge.DrawString(font, new Vector2(0, baseline), digits, HorizontalAlignment.Center, _gauge.Size.X, number, colour);
        _gauge.DrawString(font, new Vector2(0, (TubeHeight + 58) * s), last ? "LAST TURN" : "TURNS LEFT", HorizontalAlignment.Center,
            _gauge.Size.X, caption, last ? _colours.Warning : _colours.Muted);
    }

    /// <summary>A spent round: a dark slot.</summary>
    private void DrawSlot(Rect2 r)
    {
        var slot = new StyleBoxFlat { BgColor = SlotFill, BorderColor = SlotEdge, AntiAliasing = true };
        slot.SetCornerRadiusAll((int)Math.Round(6 * _scale));
        slot.SetBorderWidthAll(1);
        _gauge.DrawStyleBox(slot, r);
    }

    /// <summary>
    /// A round left: a pill lit in <paramref name="colour"/> from light at the top to dark at the bottom, a light edge and a
    /// highlight, glowing (<paramref name="glow"/> of the full glow, <paramref name="halo"/> layout pixels wide), its edge
    /// <paramref name="edge"/> wide, all at <paramref name="alpha"/>.
    /// </summary>
    private void DrawSegment(Rect2 r, Color colour, float alpha, float glow = 0.55f, float halo = 8, float edge = 1)
    {
        float s = _scale;
        int radius = (int)Math.Round(6 * s);
        var shine = new StyleBoxFlat { DrawCenter = false, ShadowColor = new Color(colour.Darkened(0.1f), 0.8f * glow * alpha), ShadowSize = (int)Math.Round(halo * s) };
        shine.SetCornerRadiusAll(radius);
        _gauge.DrawStyleBox(shine, r);
        var points = RoundedRect(r, radius * 1f);
        var top = colour.Lightened(0.3f);
        var bottom = colour.Darkened(0.25f);
        _gauge.DrawPolygon(points, [.. points.Select(p => new Color(top.Lerp(bottom, (p.Y - r.Position.Y) / r.Size.Y), alpha))]);
        _gauge.DrawPolyline([.. points, points[0]], new Color(1, 0.94f, 0.8f, alpha), Math.Max(1, edge * s), true);
        _gauge.DrawLine(r.Position + new Vector2(5, 4) * s, new Vector2(r.End.X - 5 * s, r.Position.Y + 4 * s), new Color(1, 1, 1, 0.6f * alpha), Math.Max(1, 2 * s));
    }

    /// <summary>The outline of <paramref name="r"/> with corners of <paramref name="radius"/>, clockwise.</summary>
    private static Vector2[] RoundedRect(Rect2 r, float radius)
    {
        radius = Math.Min(radius, Math.Min(r.Size.X, r.Size.Y) / 2);
        var points = new List<Vector2>();
        (Vector2 Centre, float From)[] corners =
        [
            (r.Position + new Vector2(radius, radius), MathF.PI),
            (new Vector2(r.End.X - radius, r.Position.Y + radius), 1.5f * MathF.PI),
            (r.End - new Vector2(radius, radius), 0),
            (new Vector2(r.Position.X + radius, r.End.Y - radius), 0.5f * MathF.PI),
        ];
        foreach (var (centre, from) in corners)
        {
            for (int i = 0; i <= 4; i++)
            {
                float a = from + i * MathF.PI / 8;
                points.Add(centre + new Vector2(MathF.Cos(a), MathF.Sin(a)) * radius);
            }
        }
        return [.. points];
    }

    // The battle's limit (owner, 2026-10-15): a plate centred at the scene's top, 8 below the screen's edge (owner,
    // 2026-10-16; at its foot until then) — what the first round header's second and third lines say (BattleOpening.LimitText).
    private const float LimitCentreY = 8 + 28 / 2f, LimitHeight = 28, LimitPadding = 16;

    private void DrawLimit()
    {
        var font = _theme.DefaultFont;
        int fontSize = _theme.DefaultFontSize;
        if (font is null || _limitText == "")
            return;
        float s = _scale;
        float width = font.GetStringSize(_limitText, HorizontalAlignment.Left, -1, fontSize).X + 2 * LimitPadding * s;
        var plate = new Rect2((_limit.Size.X - width) / 2, (LimitCentreY - LimitHeight / 2 - Eased(_away) * LimitAway) * s, width, LimitHeight * s);
        var box = new StyleBoxFlat
        {
            BgColor = new Color(_colours.Back, 0.8f),
            BorderColor = _colours.Border,
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        };
        box.SetCornerRadiusAll((int)Math.Round(6 * s));
        _limit.DrawStyleBox(box, plate);
        float baseline = plate.Position.Y + (plate.Size.Y + font.GetAscent(fontSize) - font.GetDescent(fontSize)) / 2;
        _limit.DrawString(font, new Vector2(plate.Position.X, baseline), _limitText, HorizontalAlignment.Center, plate.Size.X,
            fontSize, _colours.Text);
    }

    // The separator between the scene and the panel (owner, 2026-10-16): a bar across the screen at the scene's foot in the
    // frames' colour, lit from above, an accent line along its top brightest in the middle, a shadow cast on the panel.
    private static readonly Rect2 SeparatorArea = new(0, 406, 1280, 22);
    private const float SeparatorTop = 2, SeparatorBar = 6, SeparatorShadow = 12;

    private void DrawSeparator()
    {
        float s = _scale, w = _separator.Size.X;
        float top = SeparatorTop * s, bar = Math.Max(2, SeparatorBar * s), bottom = top + bar;
        var shadow = new Color(0, 0, 0, 0.45f);
        _separator.DrawRect(new Rect2(0, top - Math.Max(1, s), w, Math.Max(1, s)), shadow);           // its edge on the scene
        _separator.DrawPolygon([new(0, bottom), new(w, bottom), new(w, bottom + SeparatorShadow * s), new(0, bottom + SeparatorShadow * s)],
            [shadow, shadow, shadow with { A = 0 }, shadow with { A = 0 }]);
        var (light, dark) = (_colours.Border.Lightened(0.3f), _colours.Border.Darkened(0.45f));
        _separator.DrawPolygon([new(0, top), new(w, top), new(w, bottom), new(0, bottom)], [light, light, dark, dark]);
        float line = Math.Max(1, 1.5f * s);
        var faint = _colours.Accent with { A = 0.15f };
        var bright = _colours.Accent with { A = 0.85f };
        _separator.DrawPolygon([new(0, top), new(w / 2, top), new(w / 2, top + line), new(0, top + line)], [faint, bright, bright, faint]);
        _separator.DrawPolygon([new(w / 2, top), new(w, top), new(w, top + line), new(w / 2, top + line)], [bright, faint, faint, bright]);
        _separator.DrawRect(new Rect2(0, bottom - Math.Max(1, s), w, Math.Max(1, s)), new Color(0, 0, 0, 0.5f));
    }

    // ------------------------------------------------------------------ the pointer

    /// <summary>The centre of <paramref name="place"/>'s card, in the parent's coordinates (unattended play's clicks).</summary>
    public Vector2 CardCentre(BattlePlace place)
    {
        var area = CardArea(place);
        return _origin + (area.Position + area.Size / 2) * _scale;
    }

    /// <summary>The place whose card is under <paramref name="local"/> (in the parent's coordinates) and holds a unit, or null.</summary>
    public BattlePlace? PlaceAt(Vector2 local, GameState s)
    {
        foreach (var place in SlotSnapshot.Places)
        {
            var area = CardArea(place);
            var rect = new Rect2(_origin + area.Position * _scale, area.Size * _scale);
            if (rect.HasPoint(local) && BattleBoard.Record(s, place) != 0)
                return place;
        }
        return null;
    }

    /// <summary>Our command input over the cards (<c>FIGT_SEL</c> in New).</summary>
    public BattleCommandLayer Commands => _commands;

    /// <summary>Our dialog for a script menu of the battle (the retreat confirmation in New).</summary>
    public BattleDialogLayer Dialog => _dialog;

    /// <summary>The layout point under <paramref name="local"/> (in the parent's coordinates).</summary>
    public Vector2 ToLayout(Vector2 local) => (local - _origin) / _scale;

    /// <summary>Layout point <paramref name="layout"/> in the parent's coordinates.</summary>
    public Vector2 FromLayout(Vector2 layout) => _origin + layout * _scale;

    /// <summary>
    /// The script's point for a pointer at <paramref name="local"/>: on a card, the centre of its slot
    /// panel (<see cref="BattleBoard.PanelCentre"/>; the original's <c>FIGT_SEL</c>, when the interface
    /// changed while it was open); elsewhere the point of the windows' layer under it, clamped to the screen.
    /// </summary>
    public Vector2I ToScript(Vector2 local, GameState s)
    {
        if (PlaceAt(local, s) is { } place)
        {
            var (x, y) = BattleBoard.PanelCentre(place);
            return new Vector2I(x, y);
        }
        var p = (local - _origin - WindowsArea.Position * _scale) / (WindowsArea.Size.X / Surface.ScreenWidth * _scale);
        return new Vector2I(Math.Clamp((int)Math.Floor(p.X), 0, Surface.ScreenWidth - 1),
            Math.Clamp((int)Math.Floor(p.Y), 0, Surface.ScreenHeight - 1));
    }

    /// <summary>The centre of script pixel <paramref name="script"/> on the windows' layer, in the parent's coordinates.</summary>
    public Vector2 FromScript(Vector2I script)
    {
        var centre = new Vector2(script.X, script.Y) + new Vector2(0.5f, 0.5f);
        return _origin + (WindowsArea.Position + centre * WindowScale) * _scale;
    }
}
