using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles.Effects;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;
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
internal sealed partial class BattleBoardView : Control, IUiPaletteUser
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    // The touch mode's board stands between the rails, which come down beside it into the thumbs' reach (owner's A,
    // 2026-10-25; docs/ui/touch.md, "The battle within the thumbs' reach"): the cards narrower, their text as large. Read
    // live (docs/ui/phone-build.md, §3).
    private static bool Narrow => Device.TouchLayout;

    // The phone held landscape (docs/ui/phone-build.md, step L3; docs/ui/phone.md, §12 LB2): the scene's own picture at 70%
    // of its height, its sides blurred out to the frame, the cards taller under it (PhoneBattle's places); the rails in the
    // black sides (BattleCommandLayer). Read live.
    private static bool Phone => Device.Layout == DeviceLayout.PhoneLandscape;

    // The phone held upright (docs/ui/phone-build.md, step P3; docs/ui/phone.md, §6 PB3): the board over the whole room at
    // the PC's width, its corner the room's — the scene at the width on top, our army's rows under it, the gauge across
    // between the front rows, the enemy's rows at the thumb, Skip on its strip, the deck at the foot (PhoneBattleUpright's
    // places, the board's height its room's). Read live.
    private static bool Upright => Device.Layout == DeviceLayout.PhoneUpright;

    private static Rect2 R(PhoneFrame.Box b) => new(b.X, b.Y, b.W, b.H);

    /// <summary>A card's type in layout pixels on the phone, held landscape or upright (reference pixels of each); null elsewhere.</summary>
    private static float? CardType(float landscape, float upright) =>
        Phone ? PhoneBattle.Layout(landscape) : Upright ? PhoneBattleUpright.Layout(upright) : null;

    // The order strip and the limit's plate at the scene's foot and top: the PC's size, upright the landscape phone's (the
    // same millimetres either way: 1.6 of the PC's layout pixels).
    private static float SceneMarks => Upright ? PhoneBattleUpright.LayoutPerReference / PhoneBattle.LayoutPerReference : 1;

    // The layout in the pixels of a 1280×800 window (two to a script pixel); upright of the board, 1280 wide.
    private static Rect2 SidesArea => Upright ? R(PhoneBattleUpright.Scene) : Phone ? new(0, 0, 1280, PhoneBattle.SceneBand) : new(0, 0, 1280, 410);
    private static Rect2 SceneArea => Upright ? R(PhoneBattleUpright.Scene) : Phone ? R(PhoneBattle.ScenePicture) : new(128, 0, 1024, 410);
    private static Rect2 WindowsArea => Upright || Phone                       // 640×400 script pixels ×1.025, at the scene's scale
        ? Windows(Upright ? PhoneBattleUpright.SceneScale : PhoneBattle.SceneScale)
        : new(312, 0, 656, 410);
    private static Rect2 Windows(float k) => new(640 - 328 * k, 0, 656 * k, 410 * k);
    private static Rect2 StripArea => new(10, SceneFoot - 36 * SceneMarks, 1260, 32 * SceneMarks);   // over the scene's foot (owner, 2026-10-16)
    private static float SceneFoot => SidesArea.End.Y;
    private static Rect2 PlayerNameArea => Upright ? R(PhoneBattleUpright.Names(true)) : Phone ? R(PhoneBattle.Names(true))
        : Narrow ? new(146, 424, 454, 24) : new(10, 424, 574, 24);              // up under the separator (owner, 2026-10-16)
    private static Rect2 EnemyNameArea => Upright ? R(PhoneBattleUpright.Names(false)) : Phone ? R(PhoneBattle.Names(false))
        : Narrow ? new(680, 424, 454, 24) : new(696, 424, 574, 24);
    private static Rect2 GaugeArea => Upright ? R(PhoneBattleUpright.Gauge) : Phone ? R(PhoneBattle.Gauge)
        : Narrow ? new(612, 450, 56, 324) : new(608, 450, 64, 324);             // the caption a word a line (owner, 2026-10-24)
    // The speaker's portrait beside the balloon; on the phone in the scene's blurred side, so that no sprite is covered
    // (docs/ui/phone.md, §17, LB3b); upright at the scene's top corner as the PC's at the width, the balloon beside it (§11,
    // PB4b; docs/ui/phone-build.md, step P4).
    private static Rect2 SpeakerLeft => Upright ? R(PhoneBattleStatesUpright.Speaker(player: true))
        : Phone ? new(8, 4, SceneArea.Position.X - 16, SceneFoot - 8) : new(8, 6, 300, 398);
    private static Rect2 SpeakerRight => Upright ? R(PhoneBattleStatesUpright.Speaker(player: false))
        : Phone ? new(SceneArea.End.X + 8, 4, SceneArea.Position.X - 16, SceneFoot - 8) : new(972, 6, 300, 398);
    private static Vector2 CardSize => Upright ? R(PhoneBattleUpright.Card(0, 0)).Size : Phone ? R(PhoneBattle.Card(0, 0)).Size
        : Narrow ? new(223, 150) : new(282, 150);
    private static readonly float[] CardRows = [450, 608];
    private static float TextSize => CardType(PhoneBattle.CardText, PhoneBattleUpright.CardText) ?? 17;

    /// <summary>The cards' columns: the player's back row far left, its front row, the enemy's front row, its back row far right.</summary>
    private static int ColumnOf(BattlePlace place) => (place.Side, place.Slot) switch
    {
        (BattleSide.Player, 3 or 4) => 0,
        (BattleSide.Player, _) => 1,
        (BattleSide.Enemy, 1 or 2) => 2,
        _ => 3,
    };

    private static float Column(BattlePlace place) => ColumnOf(place) switch
    {
        0 => Narrow ? 146 : 10,
        1 => Narrow ? 377 : 302,
        2 => Narrow ? 680 : 696,
        _ => Narrow ? 911 : 988,
    };

    // Upright the columns are rows (our back row on top), an army's slots 1 and 3 at the left (PB3).
    internal static Rect2 CardArea(BattlePlace place) => Upright ? R(PhoneBattleUpright.Card(ColumnOf(place), (place.Slot - 1) % 2))
        : Phone ? R(PhoneBattle.Card(ColumnOf(place), (place.Slot - 1) % 2))
        : new(Column(place), CardRows[(place.Slot - 1) % 2], CardSize.X, CardSize.Y);

    // The touch mode's standing orders at an own card's foot (owner's B, 2026-10-25; docs/ui/touch.md, "The standing orders
    // on the cards"): Auto-Atk and Auto-Def side by side across the text column, under the state line; the order's lit,
    // Manual when neither is. Not on the acting card, whose rail's Auto-Atk and Auto-Def hand its turn over instead.
    internal static readonly (StandingOrder Order, string Text)[] OrderToggles = [(StandingOrder.AutoAttack, "Auto-Atk"), (StandingOrder.AutoDefend, "Auto-Def")];
    private const float OrderHeight = 32, OrderBottom = 8, OrderGap = 5;
    private static float OrderText => CardType(PhoneBattle.StatsText, PhoneBattleUpright.TogglesText) ?? 14;

    /// <summary>Whether <paramref name="card"/> at <paramref name="place"/> shows the order toggles (the touch mode's own cards but the acting one).</summary>
    internal static bool ShowsOrders(BattlePlace place, BattleCard card, bool acting) =>
        Narrow && place.Side == BattleSide.Player && !acting && card.Order is StandingOrder.Manual or StandingOrder.AutoAttack or StandingOrder.AutoDefend;

    /// <summary>Order toggle <paramref name="toggle"/> (<see cref="OrderToggles"/>) on <paramref name="place"/>'s card, in layout pixels.</summary>
    internal static Rect2 OrderToggleArea(BattlePlace place, int toggle)
    {
        var card = CardArea(place);
        if (Phone)                                                      // across the card's whole foot, 4.6 mm high (LB2)
        {
            float inset = PhoneBattle.Layout(PhoneBattle.TogglesInset), h = PhoneBattle.Layout(PhoneBattle.TogglesHeight);
            float w = (card.Size.X - 2 * inset - inset) / 2;
            return new Rect2(card.Position.X + inset + toggle * (w + inset), card.End.Y - inset - h, w, h);
        }
        if (Upright)                                                    // across the text column's foot (PB3's slim card)
        {
            float pad = PhoneBattleUpright.Layout(PhoneBattleUpright.CardPad), inset = PhoneBattleUpright.Layout(PhoneBattleUpright.TogglesInset);
            float h = PhoneBattleUpright.Layout(PhoneBattleUpright.TogglesHeight);
            float from = card.Position.X + pad + CardPortrait.Width + 2 * pad, to = card.End.X - pad;
            float w = (to - from - inset) / 2;
            return new Rect2(from + toggle * (w + inset), card.End.Y - inset - h, w, h);
        }
        float left = card.Position.X + 4 + CardPortrait.Width + 8, right = card.End.X - 4;   // the text column: padding, face, separation
        float width = (right - left - OrderGap) / 2;
        return new Rect2(left + toggle * (width + OrderGap), card.End.Y - OrderBottom - OrderHeight, width, OrderHeight);
    }

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
    private Rect2 PanelArea => new(0, SceneFoot, 1280, _height - SceneFoot);

    /// <summary>The board's height in layout pixels: the 1280×800 window's, upright the room's (<see cref="PhoneBattleUpright.Height"/>).</summary>
    private float _height = 800;
    private const double SpotFadeSeconds = 0.15;

    /// <summary>How dark the spotlight makes the panel (Option → Battle, 0.5 by default).</summary>
    private static float SpotDarkness => (float)BattleScreenSettings.Get(BattleScreenSettings.SpotDarkness);

    private readonly FacePictures _faces;
    private FaceCrops _crops = new();
    private readonly Theme _theme = new();
    private readonly ColorRect _back;
    private readonly TextureRect _stones;                   // the board's backdrop, BoardStones
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
    private readonly Dictionary<int, Control> _orderStrips = [];      // the touch mode's order toggles by unit record
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
    private (Vector2 Origin, float Scale, bool Narrow)? _placed;     // what Place laid the screen out for
    private PhoneFrame? _placedPhone;                                // and the phone's frame, or null
    private PhoneFrame? _placedUpright;                              // and the phone's frame held upright, or null
    private readonly Control _deck;                                  // upright the deck's panel, empty while an exchange plays (PB3x)

    public BattleBoardView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
        Theme = _theme;
        _back = new ColorRect { MouseFilter = MouseFilterEnum.Ignore };
        AddChild(_back);
        _stones = Picture();                                                // under the cards (owner, 2026-10-24)
        AddChild(_stones);
        _deck = new Control { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _deck.Draw += () =>
        {
            _deck.DrawSetTransform(Vector2.Zero, 0, new Vector2(_scale, _scale));
            Ui.DrawPanel(_deck, new Rect2(Vector2.Zero, _deck.Size / _scale), _scale);
            _deck.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        };
        AddChild(_deck);
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
            _placed = null;                                                 // laid out again
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
        _dialog = new BattleDialogLayer { RetreatAt = () => _commands.RetreatButton };   // over everything; on the phone beside Retreat
        AddChild(_dialog);
        _advantageTip = new AdvantageTip();                                 // shown only while no dialog is
        AddChild(_advantageTip);
        ApplyPalette();
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
    /// numbers. Each start plays once; none with Option → Battle → "Effects" off, or when it would be at once
    /// (<paramref name="instant"/>: <c>--fast</c>, a screen held at a moment). It plays out by itself, nothing waits for
    /// it — but <paramref name="stepped"/> (the message skip; owner, 2026-10-29) the exchange's waits step it instead
    /// (<see cref="StepSkippedEffect"/>) while its blow is on (<see cref="BattlePresenter.Strike"/>); the skip turned
    /// off, it plays on from there.
    /// </summary>
    private void PlayEffect(GamePresenter presenter, bool instant, bool stepped)
    {
        var battle = presenter.Battle;
        if (EffectStepped && battle.Strike is null)
            StopSteppedEffect();                                        // the blow is over: what is left of its effect goes
        else if (EffectStepped && !stepped)
        {
            EffectStepped = false;                                      // the skip turned off: the effect plays on from where it stands
            _effect.Playing = true;
        }
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
        EffectStepped = stepped;
        _effect.Playing = !stepped;
    }

    /// <summary>
    /// Whether the message skip's waits step the exchange's effect (<see cref="StepSkippedEffect"/>): from its start under
    /// the skip until its blow is over, the skip is turned off or the effect has run out.
    /// </summary>
    public bool EffectStepped { get; private set; }

    /// <summary>How many of its frames (<see cref="EffectCanvas.FrameSeconds"/>) the effect moves on at each skipped wait.</summary>
    private const int SkippedWaitFrames = 2;

    /// <summary>
    /// One of the exchange's waits passed under the message skip (<c>GameScreen.KeyWaitAsync</c>): a stepped effect moves
    /// on <see cref="SkippedWaitFrames"/> of its frames — its ten shots' waits, the pause after them and the result's wait
    /// show frames 0, 2 … 22 of 30, the part before its tail (owner, 2026-10-29); it goes when it runs out.
    /// </summary>
    public void StepSkippedEffect()
    {
        if (!EffectStepped)
            return;
        _effect.Frame += SkippedWaitFrames;
        if (_effect.Frame >= EffectCanvas.Frames)
            StopSteppedEffect();
    }

    private void StopSteppedEffect()
    {
        EffectStepped = false;
        _effect.Visible = false;
    }

    private int _guards;

    /// <summary>
    /// A defence (docs/ui/battle-screen.md, "The exchange's effects"): our Guard effect after Sengoku Rance's guard
    /// actions', a wall of light raised in front of the defender on <paramref name="side"/>'s half, mirrored for the
    /// enemy's — a unit's Defend (<c>FIGT.ADV:1399</c>, its badge flashing on the card), a commander's
    /// (<see cref="BattlePresenter.EnemyDefence"/>). False when it does not play: Option → Battle → "Effects"
    /// off, or the armies' moves at once (skipping, <c>--fast</c>); else it lasts <see cref="GuardEffect.Seconds"/>.
    /// </summary>
    public bool PlayGuard(BattleSide side)
    {
        if (_armiesInstant || BattleScreenSettings.Get(BattleScreenSettings.Effects) < 0.5)
            return false;
        EffectStepped = false;
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

    /// <summary>Option → Display: what the screen keeps in its palette's colours set again, the cards built again.</summary>
    public void ApplyPalette()
    {
        _theme.SetColor("font_color", "Label", Ui.Text);
        _back.Color = Back;
        _stones.Texture = BoardStones.For(Ui, GaugeCaptionArea, StonesArea);
        _strip.ApplyPalette();
        _playerAdvantage.SetColour(Ui.HoverLine);
        _enemyAdvantage.SetColour(Ui.HoverLine);
        _cardsKey = _stripKey = "?";
        _placed = null;                                                 // every layer drawn again in the new colours
        _gauge.QueueRedraw();
        _limit.QueueRedraw();
        _separator.QueueRedraw();
    }

    /// <summary>The screen's backdrop where the stones are not: the panels' lacquer, opaque.</summary>
    private static Color Back => Ui.Panel with { A = 1 };

    /// <summary>The rim round the words on the stones: the armies' names, their advantages, the gauge's caption.</summary>
    private static readonly Color LabelRim = new(0.03f, 0.025f, 0.02f, 0.8f);

    /// <summary>Where the gauge's "n TURNS LEFT" stands, in layout pixels: the stones are darker round it.</summary>
    private static Rect2 GaugeCaptionArea => Upright
        ? new(GaugeArea.Position.X + PhoneBattleUpright.Layout(PhoneBattleUpright.CaptionLeft - 20), GaugeArea.Position.Y,
            PhoneBattleUpright.Layout(340), GaugeArea.Size.Y)
        : new(GaugeArea.Position.X, GaugeArea.Position.Y + TubeH + 10 * GaugeK, GaugeArea.Size.X, 76 * GaugeK);

    /// <summary>Where the stones lie: under the separator to the screen's foot; upright to the deck's top.</summary>
    private Rect2 StonesArea => new(0, SceneFoot + 4, 1280, (Upright ? PhoneBattleUpright.DeckTopY : _height) - SceneFoot - 4);

    /// <summary>The armies' names and their advantages on the phone (reference pixels; LB2's 30, a little larger).</summary>
    private const float PhoneNamesText = 32;

    /// <summary>Option → Display: the screen's text in the chosen font.</summary>
    public void SetFont(GameFont font)
    {
        _theme.DefaultFont = DeploymentTableView.LoadFont(font);
        _balloon.SetFont(_theme.DefaultFont);
        _info.SetFont(_theme.DefaultFont);
        _placed = null;
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
        // Nothing moved: the layers redraw themselves. Laid out again every frame, the armies' names were shaped and their
        // lines sorted again each time (a theme override set is a theme change) and every layer drawn again
        // (docs/ui/phone-build.md, "The battle's speed").
        var phone = Device.Landscape(this);
        var upright = Device.Upright(this);
        if (upright is not null)                                            // over the whole room (PB3)
            (origin, scale) = (new Vector2(upright.Room.X, upright.Room.Y), PhoneBattleUpright.Scale(upright));
        if (_placed == (origin, scale, Narrow) && _placedPhone == phone && _placedUpright == upright)
            return;
        float height = upright is null ? 800 : PhoneBattleUpright.Height(upright);
        if (_placedPhone is null != phone is null || _placedUpright is null != upright is null || height != _height)
        {
            _height = height;
            _cardsKey = _stripKey = _speakerKey = "?";                  // the cards' insides and the speaker at the device's sizes
            _stones.Texture = BoardStones.For(Ui, GaugeCaptionArea, StonesArea);
            bool across = upright is not null;                          // upright the enemy's name at the left as ours (PB3)
            _enemyArmyLine.Alignment = across ? BoxContainer.AlignmentMode.Begin : BoxContainer.AlignmentMode.End;
            _enemyArmyLine.MoveChild(_enemyName, across ? 0 : _enemyArmyLine.GetChildCount() - 1);
            _deck.Visible = across;
            var counters = phone is null ? _sceneClip : _sidesClip;      // the phone's counters in the scene's blurred sides (LB2x)
            if (_counters.GetParent() != counters)
            {
                _counters.GetParent().RemoveChild(_counters);
                counters.AddChild(_counters);
            }
        }
        _placed = (origin, scale, Narrow);
        _placedPhone = phone;
        _placedUpright = upright;
        if (scale != _scale)
            _cardsKey = _stripKey = "?";                                // the faces and the text at the new size
        _origin = origin;
        _scale = scale;
        void At(Control c, Rect2 area)
        {
            c.Position = origin + area.Position * scale;
            c.Size = area.Size * scale;
        }
        var board = new Rect2(0, 0, 1280, _height);
        At(_back, board);
        At(_stones, StonesArea);
        if (upright is not null)
            At(_deck, R(PhoneBattleUpright.Deck(_height)));
        _deck.QueueRedraw();
        At(_sidesClip, SidesArea);
        At(_sceneClip, SceneArea);
        foreach (var layer in new[] { _field, _front })
            (layer.Position, layer.Size) = (Vector2.Zero, _sceneClip.Size);
        float sceneScale = scale * SceneArea.Size.X / 1024;                // the effect at the scene's own scale
        _effect.Position = new Vector2(0, SceneArea.Size.Y - EffectPlayer.Height * sceneScale / scale) * scale;     // at the scene's foot
        _effect.Scale = new Vector2(sceneScale, sceneScale);
        _counters.Position = Vector2.Zero;
        if (upright is not null)                                            // the PC's over the scene at its scale (PB3x)
            _counters.Layout(_sceneClip.Size, scale * PhoneBattleUpright.SceneScale);
        else if (phone is null)
            _counters.Layout(_sceneClip.Size, scale);
        else
            _counters.Layout(_sidesClip.Size, scale, (SceneArea.Position.X, SceneArea.End.X));
        PlaceArmies();
        At(_limit, SceneArea);
        _limit.QueueRedraw();
        At(_windows, WindowsArea);
        At(_stripClip, StripClipArea);
        PlaceStrip();
        _strip.SetScale(scale * SceneMarks);
        At(_playerArmyLine, PlayerNameArea);
        At(_enemyArmyLine, EnemyNameArea);
        foreach (var line in new[] { _playerArmyLine, _enemyArmyLine })
        {
            line.AddThemeConstantOverride("separation", (int)Math.Round(16 * scale));
            foreach (var label in line.GetChildren().OfType<Label>())                  // a dark rim: they stand on the stones
            {
                label.AddThemeConstantOverride("outline_size", Math.Max(2, (int)Math.Round(5 * scale)));
                label.AddThemeColorOverride("font_outline_color", LabelRim);
                if (phone is not null)                                                   // under the cards' 2.3 mm (LB2's 30)
                    label.AddThemeFontSizeOverride("font_size", (int)Math.Round(PhoneBattle.Layout(PhoneNamesText) * scale));
                else if (upright is not null)                                            // PB3's 32
                    label.AddThemeFontSizeOverride("font_size", (int)Math.Round(PhoneBattleUpright.Layout(PhoneNamesText) * scale));
                else
                    label.RemoveThemeFontSizeOverride("font_size");
            }
        }
        _opening.Position = origin + SidesArea.Position * scale;
        _opening.Layout(scale);
        At(_gauge, GaugeArea);
        At(_speaker, _speakerArea);
        At(_spotlight, board);
        At(_separator, SeparatorArea);
        _separator.QueueRedraw();
        _spotlight.QueueRedraw();
        At(_info, board);
        _info.Layout(scale);
        At(_commands, board);
        _commands.Layout(CardArea, scale, phone ?? upright, _height);
        At(_dialog, board);
        _dialog.Layout(scale, phone ?? upright, _height);
        At(_advantageTip, board);
        _advantageTip.Layout(scale);
        At(_balloon, board);
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
    /// <paramref name="instant"/>: the gauge's spent round does not flash; <paramref name="slidesInstant"/>: the armies and the cards' counters stand at once,
    /// and no exchange's effect plays unless <paramref name="effectStepped"/>, the message skip's (<see cref="StepSkippedEffect"/>).
    /// </summary>
    public void Refresh(GamePresenter presenter, IEnumerable<WindowShape> inputWindows, bool screenChanged, BattlePlace? hovered,
        bool instant, bool slidesInstant, bool effectStepped = false)
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
        PlayEffect(presenter, slidesInstant && !effectStepped, effectStepped);
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
        RefreshStrip(s, presenter.Order);
        var gauge = (Math.Max(presenter.Order.Rounds, s.Bt_RoundsLeft.Value), s.Bt_RoundsLeft.Value);
        if (gauge != _gaugeValues)
        {
            if (!instant && gauge.Item1 == _gaugeValues.Rounds && gauge.Item2 < _gaugeValues.Left)
                (_spentSegment, _spentAt) = (_gaugeValues.Rounds - _gaugeValues.Left, UiClock.Msec);   // the round that went out flashes
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
        RedrawOrderStrips();
    }

    private void RedrawOrderStrips()
    {
        foreach (var strip in _orderStrips.Values)
        {
            if (IsInstanceValid(strip))
                strip.QueueRedraw();
        }
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
        if (_spotShown == 0)
            RedrawOrderStrips();                                         // the fighters' toggles back once it has faded
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

    // The action's pill (owner, 2026-10-13, mock-up D): under the card's state line, on the side away from the face —
    // an enemy's at the card's left, a player's at its right (owner, 2026-10-25).
    private const float PillHeight = 24, PillBottom = 8, PillText = 16, PillIcon = 16, PillInset = 4, PhonePill = 1.6f;
    private static readonly Color PillInk = new(0.12f, 0.09f, 0.05f);

    /// <summary>A pill in the accent colour on the fighter's card: the action's icon and our name for it (<see cref="BattleCommands.ActionName"/>).</summary>
    private void DrawPill(Fighter fighter)
    {
        float s = _scale, k = Phone ? PhonePill                         // the phone's cards' type: LB2x's pill (58 high)
            : Upright ? PhoneBattleUpright.Layout(PhoneBattleUpright.PillHeight) / PillHeight : 1;  // PB3x's (42)
        var font = _theme.DefaultFont;
        int size = Math.Max(8, (int)Math.Round(PillText * k * s));
        string text = BattleCommands.ActionName(fighter.Action);
        float textWidth = font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X;
        float h = PillHeight * k * s, icon = PillIcon * k * s, gap = 6 * k * s, side = 9 * k * s;
        float width = side + icon + gap + textWidth + side;
        float left = fighter.Side == BattleSide.Player
            ? (fighter.Card.End.X - PillInset * k) * s - width
            : (fighter.Card.Position.X + PillInset * k) * s;
        var rect = new Rect2(left, (fighter.Card.End.Y - (PillBottom + PillHeight) * k) * s, width, h);
        var box = new StyleBoxFlat { BgColor = Ui.HoverLine with { A = _spotShown } };
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
        if (_cardsKey == "?")
            _cardKeys.Clear();                                           // every card built again (the scale, the faces, the palette)
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
        BattlePlace? actingPlace = null;
        for (int i = 0; i < cards.Count; i++)
        {
            var place = SlotSnapshot.Places[i];
            var (panel, box) = _cards[place];
            var card = cards[i];
            bool acting = order.Acting == place && card is not null && card.Record == order.ActingRecord;
            if (acting)
                actingPlace = place;
            box.BgColor = card is null ? Colors.Transparent : acting ? Ui.Row.Lerp(Ui.HoverLine, ActingTint) : Ui.Row;   // an empty place: the stones in a line
            box.BorderColor = Border(place, card, acting);
            int pad = Pad;
            box.ContentMarginLeft = box.ContentMarginRight = box.ContentMarginTop = box.ContentMarginBottom = pad;
            // Only a card whose contents changed is built again: an exchange's blow changes the two fighters' alone (all
            // eight built again cost the phone ~10 ms and their layout more; docs/ui/phone-build.md, "The battle's speed").
            string cardKey = $"{card}|{acting}";
            if (_cardKeys.GetValueOrDefault(place) == cardKey)
                continue;
            _cardKeys[place] = cardKey;
            foreach (var record in _orderLabels.Where(o => panel.IsAncestorOf(o.Value)).Select(o => o.Key).ToList())
                _orderLabels.Remove(record);
            foreach (var record in _orderStrips.Where(o => panel.IsAncestorOf(o.Value)).Select(o => o.Key).ToList())
                _orderStrips.Remove(record);
            foreach (var child in panel.GetChildren())
            {
                panel.RemoveChild(child);
                child.QueueFree();
            }
            panel.AddChild(card is null ? new Control { MouseFilter = MouseFilterEnum.Ignore } : Contents(card, acting, pad));
            if (card is not null && ShowsOrders(place, card, acting))
                panel.AddChild(OrderStrip(place, card));                    // over the contents, the panel's size
        }
        if (actingPlace != _actingPlace)
        {
            _actingPlace = actingPlace;
            (_blinkFrom, _blinkOn) = (UiClock.Msec, true);        // a new actor: the frame shown first
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

    /// <summary>What each place's card was built showing (its card and whether it acts).</summary>
    private readonly Dictionary<BattlePlace, string> _cardKeys = [];

    /// <summary>A card's frame: the accent while it acts, lit under the pointer (<see cref="_cardsHovered"/>), else plain; an empty place's line.</summary>
    private Color Border(BattlePlace place, BattleCard? card, bool acting) =>
        card is null ? Ui.PlaceLine : acting ? Ui.HoverLine : _cardsHovered == place ? Ui.Gold : Ui.Gold;

    /// <summary>The cards' padding inside their frame, in window pixels: four units of the acting frame.</summary>
    private int Pad => Math.Max(1, (int)Math.Round((CardType(PhoneBattle.CardPad, PhoneBattleUpright.CardPad) ?? 4) * _scale));

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
            var pointer = TouchMode.GlobalPointer(this);
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
    /// from beyond its own side, linear, the front row in Option → Battle → "Cards: slide in" (640 layout pixels;
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
        bool on = (UiClock.Msec - _blinkFrom) / BlinkMs % 2 == 0;
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
        Color[] edge = [Colors.Black, Ui.HoverLine, Ui.HoverLine, Colors.Black];
        for (int i = 0; i < edge.Length; i++)
            Ring(i, edge[i]);
        if (!_blinkOn)
            return;
        Color[] blink = [Ui.HoverLine, Colors.White, Colors.White, Ui.HoverLine];
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
        (_flashUnit, _flashFrom) = (unit, UiClock.Msec);
        StepOrderFlash();
    }

    private void StepOrderFlash()
    {
        if (_flashUnit is not { } unit)
            return;
        if ((UiClock.Msec - _flashFrom) / 1000.0 >= FlashRise + FlashHold + FlashFade)
        {
            RestoreOrderLabel();
            return;
        }
        if (_orderLabels.TryGetValue(unit, out var label) && IsInstanceValid(label))
            label.AddThemeColorOverride("font_color", FlashColour());
        RedrawOrderStrip(unit);
    }

    private void RestoreOrderLabel()
    {
        if (_flashUnit is { } unit && _orderLabels.TryGetValue(unit, out var label) && IsInstanceValid(label))
            label.AddThemeColorOverride("font_color", Ui.Quiet);
        int? flashed = _flashUnit;
        _flashUnit = null;
        if (flashed is { } u)
            RedrawOrderStrip(u);
    }

    private void RedrawOrderStrip(int unit)
    {
        if (_orderStrips.TryGetValue(unit, out var strip) && IsInstanceValid(strip))
            strip.QueueRedraw();
    }

    /// <summary>The flashing label's colour now: the muted colour toward its saturated self and back.</summary>
    private Color FlashColour() => Ui.Quiet.Lerp(Saturated(Ui.Quiet, Ui.Text), FlashLevel());

    /// <summary>How far the flash is now: 0 to 1 as it rises, 1 while it holds, back to 0 as it fades.</summary>
    private float FlashLevel()
    {
        double t = (UiClock.Msec - _flashFrom) / 1000.0;
        float k = t < FlashRise ? (float)(t / FlashRise)
            : t < FlashRise + FlashHold ? 1
            : (float)Math.Max(0, 1 - (t - FlashRise - FlashHold) / FlashFade);
        return k * k * (3 - 2 * k);
    }

    /// <summary>The touch mode's order toggles on <paramref name="card"/>'s panel (<see cref="OrderToggleArea"/>), redrawn by the flash.</summary>
    private Control OrderStrip(BattlePlace place, BattleCard card)
    {
        var strip = new Control { MouseFilter = MouseFilterEnum.Ignore };
        strip.Draw += () => DrawOrderStrip(strip, place, card);
        _orderStrips[card.Record] = strip;
        return strip;
    }

    /// <summary>
    /// The order toggles: a row's box each, the card's order lit in the accent as the action's pill; an order given (the
    /// script's message, <see cref="FlashOrder"/>) glows round the lit one, or round both for Manual. Not on a fighter's
    /// card while the exchange's spotlight shows: its pill stands there.
    /// </summary>
    private void DrawOrderStrip(Control strip, BattlePlace place, BattleCard card)
    {
        var area = CardArea(place);
        if ((_spotOn || _spotShown > 0) && _fighters.Any(f => f.Card == area))
            return;
        if (GetThemeDefaultFont() is not { } font)
            return;
        float s = _scale;
        strip.DrawSetTransform(-new Vector2(Pad, Pad), 0, new Vector2(s, s));     // in layout pixels from the card's corner
        float flash = card.Record == _flashUnit ? FlashLevel() : 0;
        for (int i = 0; i < OrderToggles.Length; i++)
        {
            var (order, text) = OrderToggles[i];
            var toggle = OrderToggleArea(place, i);
            var r = new Rect2(toggle.Position - area.Position, toggle.Size);
            bool lit = card.Order == order;
            if (flash > 0 && (lit || card.Order == StandingOrder.Manual))
            {
                strip.DrawRect(r.Grow(5), Ui.HoverLine with { A = 0.18f * flash });
                strip.DrawRect(r.Grow(2.5f), Ui.HoverLine with { A = 0.4f * flash });
            }
            if (lit)
            {
                Color top = Ui.HoverLine.Lightened(0.25f), bottom = Ui.HoverLine.Darkened(0.2f);
                strip.DrawPolygon([r.Position, new(r.End.X, r.Position.Y), r.End, new(r.Position.X, r.End.Y)], [top, top, bottom, bottom]);
                strip.DrawRect(r, Ui.Gold, false, Lacquer.Line(1, s));
            }
            else
                Ui.DrawRowBox(strip, r, false, s);
            int px = (int)Math.Round(OrderText);
            while (px > 10 && font.GetStringSize(text, fontSize: px).X > r.Size.X - 6)
                px--;
            strip.DrawString(font, new Vector2(r.Position.X, Lacquer.Baseline(r, font, px)), text, HorizontalAlignment.Center, r.Size.X, px,
                lit ? PillInk : Ui.Quiet);
        }
        strip.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>
    /// <paramref name="muted"/> made vivid: its hue at full saturation, as bright as the text on a dark card (as dark on a
    /// light one); a grey without a hue of its own takes the accent's.
    /// </summary>
    private Color Saturated(Color muted, Color text)
    {
        muted.ToHsv(out float h, out float s, out float v);
        if (s < 0.02f)
            Ui.HoverLine.ToHsv(out h, out _, out _);
        bool dark = text.Luminance > muted.Luminance;
        return Color.FromHsv(h, 0.85f, dark ? 1f : Math.Min(v, 0.55f));
    }

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
            card.Stats, card.Focus, _scale);

        var text = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 0);
        var name = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        name.AddThemeConstantOverride("separation", pad * 2);
        float? nameType = CardType(PhoneBattle.NameText, PhoneBattleUpright.NameText);
        name.AddChild(PhoneSized(Line(card.Place.Slot.ToString(), Ui.Quiet, expand: false), nameType));
        name.AddChild(PhoneSized(Line(card.Name), nameType));
        text.AddChild(name);
        text.AddChild(PhoneSized(Line(card.TroopType == "" ? "—" : card.TroopType, Ui.Quiet),
            CardType(PhoneBattle.QuietText, PhoneBattleUpright.QuietText), TextRoom(pad)));
        var side = card.Place.Side == BattleSide.Player ? PlayerColour : EnemyColour;
        foreach (var (gauge, colour) in new[] { (Gauge.Troops, side), (Gauge.Hp, HpColour) })
        {
            var label = Line("");
            var (bar, trail, fill) = Bar(colour);
            text.AddChild(label);
            text.AddChild(bar);
            Track(card, gauge, label, trail, fill);                         // the number and the bar, running to a new value
            if (Phone || Upright)                                           // as large as its widest figures let it be (LB2's fit)
                label.AddThemeFontSizeOverride("font_size", Fitted(Widest(card, gauge), (int)Math.Round(TextSize * _scale), TextRoom(pad)));
        }
        // The touch mode's own cards show their order on the toggles (OrderStrip), the acting one none: it is choosing.
        string order = Narrow && card.Place.Side == BattleSide.Player ? "" : card.Order switch
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
        // The line fits rather than end in "…" (owner, 2026-10-25: "Auto-Def · Defend · Acting" read "Auto-Def · Def…"): too
        // long, it drops "Acting" (the card's fill and blinking frame say it), then shrinks, down to seven eighths. The text
        // column is the card less its padding, the face and the separation.
        int full = (int)Math.Round((CardType(PhoneBattle.QuietText, PhoneBattleUpright.QuietText) ?? TextSize) * _scale), fitted = full;
        float room = TextRoom(pad);
        string Whole() => order + (order != "" && state != "" ? " · " : "") + state;
        bool Fits(int size) => _theme.DefaultFont.GetStringSize(Whole(), fontSize: size).X <= room;
        if (!Fits(full) && acting && state.EndsWith("Acting"))
            state = state[..^"Acting".Length].TrimEnd(' ', '·');
        while (fitted > full * 7 / 8 && !Fits(fitted))
            fitted--;
        if (order != "")
        {
            var orderLabel = Line(order, Ui.Quiet, expand: false);   // its own label: it flashes (FlashOrder)
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
        stateLine.AddChild(Line(state, Ui.Quiet));
        if (fitted < full || Phone || Upright)
        {
            foreach (var label in stateLine.GetChildren().OfType<Label>())         // an override is the node's own, not its children's
                label.AddThemeFontSizeOverride("font_size", fitted);
        }
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

    /// <summary>The card's text column's width in window pixels: the card less its padding, the face and the separation.</summary>
    private float TextRoom(int pad) => (CardSize.X - pad * 2 / _scale - CardPortrait.Width - pad * 2 / _scale - 2) * _scale;

    /// <summary>
    /// On the phone, <paramref name="label"/> at <paramref name="size"/> layout pixels (<see cref="CardType"/>: LB2's card the
    /// name 45, the quiet lines 36; PB3's 36 and 28), smaller when its text would not fit <paramref name="room"/> (window
    /// pixels; LB2's fit); elsewhere (null) as it is.
    /// </summary>
    private Label PhoneSized(Label label, float? size, float room = float.MaxValue)
    {
        if (size is { } layout)
            label.AddThemeFontSizeOverride("font_size", Fitted(label.Text, (int)Math.Round(layout * _scale), room));
        return label;
    }

    /// <summary><paramref name="size"/>, or as much smaller as <paramref name="text"/> needs to fit <paramref name="room"/> (window pixels).</summary>
    private int Fitted(string text, int size, float room)
    {
        while (size > 8 && _theme.DefaultFont.GetStringSize(text, fontSize: size).X > room)
            size--;
        return size;
    }

    /// <summary>A bar, its damage trail and its fill (their shares are their right anchors, set by <see cref="Track"/>).</summary>
    private (Control Bar, Control Trail, Control Fill) Bar(Color colour)
    {
        var back = new ColorRect
        {
            Color = Back,
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
    /// The round's order (<see cref="OrderStrip"/>), at once: it changes while the strip is out of the exchange's way
    /// (owner, 2026-10-21: it comes back with the new order).
    /// </summary>
    private void RefreshStrip(GameState s, RoundOrder order)
    {
        var chips = order.Chips(s);
        string key = string.Join(";", chips);
        if (key == _stripKey && (order.Battle, order.Round) == _stripRound)
            return;
        _stripKey = key;
        _stripRound = (order.Battle, order.Round);
        _strip.Show(chips);
    }

    // ------------------------------------------------------------------ out of the exchange's way

    // While an exchange is on (the spotlight's signal, BattleScene.InEngagement; owner, 2026-10-16, as Sengoku Rance's) the
    // limit's plate slides up out over the screen's top and the order strip down behind the separator; they come back when
    // it is over, the strip with the order's change already in place (RefreshStrip). Eased, in AwaySeconds; at once while
    // skipping and with --fast.
    private const double AwaySeconds = 0.3;
    private static Rect2 StripClipArea => new(0, 0, 1280, SceneFoot - 2);   // down to the separator's top
    private static float StripAway => (408 - 374 + 2) * SceneMarks;       // the strip's top below the clip's foot
    private static float LimitAway => (8 + 28 + 12) * SceneMarks;         // the plate's foot above the scene's top, its shadow too
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
    // left below in Cinzel and "TURNS LEFT" a word a line, on the last round both in the warning colour ("LAST TURN").
    // Layout pixels.
    private const float TubeWidth = 48, TubeHeight = 238, TubeRadius = 12, SegmentWidth = 32, SegmentPad = 8, SegmentGap = 6;

    // On the phone the gauge stands as high as both rows of cards (LB2): its parts 1.2 times the PC's, the tube taller, the
    // caption's words as large as the column between the cards lets them be.
    private static float GaugeK => Phone ? 1.2f : 1;                     // upright the gauge lies across (DrawGaugeAcross)
    private static float TubeH => Phone ? GaugeArea.Size.Y - 80 * GaugeK : TubeHeight;
    private static float CaptionText => Phone ? 13 : 11;
    private const float PulseSeconds = 2, SpentFlashSeconds = 0.4f;
    private static readonly Color TubeFill = Color.Color8(24, 25, 28), SlotFill = Color.Color8(36, 37, 41), SlotEdge = Color.Color8(70, 72, 78);

    /// <summary>Whether the gauge animates now (the round under way pulses, or a spent one flashes).</summary>
    private bool GaugeMoves => _gaugeValues.Left > 0 || _spentSegment >= 0;

    private void DrawGauge()
    {
        if (Upright)
        {
            DrawGaugeAcross();
            return;
        }
        var (rounds, left) = _gaugeValues;
        float s = _scale;
        float cx = _gauge.Size.X / 2;
        bool last = left == 1;
        var lit = last ? Ui.Minus : Ui.HoverLine;
        float k = GaugeK, tubeH = TubeH;
        var tube = new Rect2(cx - TubeWidth * k / 2 * s, 0, TubeWidth * k * s, tubeH * s);
        var box = new StyleBoxFlat { BgColor = TubeFill, BorderColor = Ui.Gold, AntiAliasing = true };
        box.SetCornerRadiusAll((int)Math.Round(TubeRadius * s));
        box.SetBorderWidthAll(Math.Max(1, (int)Math.Round(2 * s)));
        _gauge.DrawStyleBox(box, tube);
        _gauge.DrawRect(new Rect2(tube.Position + new Vector2(4, 3) * s, new Vector2(tube.Size.X - 8 * s, 7 * s)), new Color(0, 0, 0, 0.35f));
        if (rounds > 0)
        {
            float height = (tubeH - 2 * SegmentPad * k - SegmentGap * k * (rounds - 1)) / rounds;
            double pulse = 0.5 - 0.5 * Math.Cos(UiClock.Msec / 1000.0 * 2 * Math.PI / PulseSeconds);
            double flash = _spentSegment >= 0 ? (UiClock.Msec - _spentAt) / 1000.0 / SpentFlashSeconds : 1;
            if (flash >= 1)
                _spentSegment = -1;
            for (int i = 0; i < rounds; i++)
            {
                var r = new Rect2(cx - SegmentWidth * k / 2 * s, (SegmentPad * k + i * (height + SegmentGap * k)) * s, SegmentWidth * k * s, height * s);
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
        var colour = last ? Ui.Minus : Ui.HoverLine;
        int number = (int)Math.Round(34 * k * s), caption = Math.Max(8, (int)Math.Round(CaptionText * s));
        float y = (tubeH + 24 * k) * s;
        string digits = left.ToString();
        float baseline = y + (font.GetAscent(number) - font.GetDescent(number)) / 2;
        _gauge.DrawStringOutline(font, new Vector2(0, baseline), digits, HorizontalAlignment.Center, _gauge.Size.X, number,
            Math.Max(2, (int)Math.Round(4 * s)), Color.Color8(20, 20, 24));
        _gauge.DrawString(font, new Vector2(0, baseline), digits, HorizontalAlignment.Center, _gauge.Size.X, number, colour);
        string[] words = last ? ["LAST", "TURN"] : ["TURNS", "LEFT"];
        for (int i = 0; i < words.Length; i++)
        {
            var at = new Vector2(0, (tubeH + (58 + 14 * i) * k) * s);
            _gauge.DrawStringOutline(font, at, words[i], HorizontalAlignment.Center, _gauge.Size.X, caption,
                Math.Max(2, (int)Math.Round(4 * s)), LabelRim);
            _gauge.DrawString(font, at, words[i], HorizontalAlignment.Center, _gauge.Size.X, caption, last ? Ui.Minus : Ui.Quiet);
        }
    }

    /// <summary>
    /// The gauge upright (PB3): the tube turned across in the band between the front rows, the spent rounds dark slots from
    /// the left, the round under way pulsing, the number left and "TURNS LEFT" right of it on one line.
    /// </summary>
    private void DrawGaugeAcross()
    {
        var (rounds, left) = _gaugeValues;
        float s = _scale;
        static float L(float reference) => PhoneBattleUpright.Layout(reference);
        bool last = left == 1;
        var lit = last ? Ui.Minus : Ui.HoverLine;
        var tube = new Rect2(L(PhoneBattleUpright.TubeLeft) * s, L(PhoneBattleUpright.TubeTop) * s,
            L(PhoneBattleUpright.TubeWidth) * s, L(PhoneBattleUpright.TubeHeight) * s);
        var box = new StyleBoxFlat { BgColor = TubeFill, BorderColor = Ui.Gold, AntiAliasing = true };
        box.SetCornerRadiusAll((int)Math.Round(TubeRadius * s));
        box.SetBorderWidthAll(Math.Max(1, (int)Math.Round(2 * s)));
        _gauge.DrawStyleBox(box, tube);
        if (rounds > 0)
        {
            float pad = L(8) * s, gap = L(12) * s;
            float width = (tube.Size.X - 2 * pad - gap * (rounds - 1)) / rounds;
            double pulse = 0.5 - 0.5 * Math.Cos(UiClock.Msec / 1000.0 * 2 * Math.PI / PulseSeconds);
            double flash = _spentSegment >= 0 ? (UiClock.Msec - _spentAt) / 1000.0 / SpentFlashSeconds : 1;
            if (flash >= 1)
                _spentSegment = -1;
            for (int i = 0; i < rounds; i++)
            {
                var r = new Rect2(tube.Position.X + pad + i * (width + gap), tube.Position.Y + pad, width, tube.Size.Y - 2 * pad);
                if (i < rounds - left)
                {
                    DrawSlot(r);
                    if (i == _spentSegment)
                        DrawSegment(r, Colors.White.Lerp(lit, (float)Math.Min(1, flash * 2)), 1 - (float)flash);
                    continue;
                }
                if (i != rounds - left)
                {
                    DrawSegment(r, lit, 1);
                    continue;
                }
                float p = (float)pulse;
                DrawSegment(r, lit.Darkened(0.2f * (1 - p)).Lerp(Colors.White, 0.55f * p), 1, 0.4f + 0.6f * p, 8 + 10 * p, 1 + 1.5f * p);
            }
        }
        var font = _opening.CinzelBold;
        var colour = last ? Ui.Minus : Ui.HoverLine;
        int number = (int)Math.Round(L(PhoneBattleUpright.CaptionNumber) * s), caption = (int)Math.Round(L(PhoneBattleUpright.CaptionWords) * s);
        float x = L(PhoneBattleUpright.CaptionLeft) * s, middle = _gauge.Size.Y / 2, rim = Math.Max(2, (int)Math.Round(4 * s));
        string digits = left.ToString(), words = last ? "LAST TURN" : "TURNS LEFT";
        float baseline = middle + (font.GetAscent(number) - font.GetDescent(number)) / 2;
        _gauge.DrawStringOutline(font, new Vector2(x, baseline), digits, HorizontalAlignment.Left, -1, number, (int)rim, Color.Color8(20, 20, 24));
        _gauge.DrawString(font, new Vector2(x, baseline), digits, HorizontalAlignment.Left, -1, number, colour);
        x += font.GetStringSize(digits, fontSize: number).X + L(18) * s;
        baseline = middle + (font.GetAscent(caption) - font.GetDescent(caption)) / 2;
        _gauge.DrawStringOutline(font, new Vector2(x, baseline), words, HorizontalAlignment.Left, -1, caption, (int)rim, LabelRim);
        _gauge.DrawString(font, new Vector2(x, baseline), words, HorizontalAlignment.Left, -1, caption, last ? Ui.Minus : Ui.Quiet);
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
        float s = _scale * SceneMarks;                                    // upright the landscape phone's size
        int fontSize = Math.Max(8, (int)Math.Round(17 * s));
        if (font is null || _limitText == "")
            return;
        float width = font.GetStringSize(_limitText, HorizontalAlignment.Left, -1, fontSize).X + 2 * LimitPadding * s;
        var plate = new Rect2((_limit.Size.X - width) / 2, (LimitCentreY - LimitHeight / 2) * s - Eased(_away) * LimitAway * _scale, width, LimitHeight * s);
        var box = new StyleBoxFlat
        {
            BgColor = new Color(Ui.Panel, 0.8f),
            BorderColor = Ui.Gold,
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        };
        box.SetCornerRadiusAll((int)Math.Round(6 * s));
        _limit.DrawStyleBox(box, plate);
        float baseline = plate.Position.Y + (plate.Size.Y + font.GetAscent(fontSize) - font.GetDescent(fontSize)) / 2;
        _limit.DrawString(font, new Vector2(plate.Position.X, baseline), _limitText, HorizontalAlignment.Center, plate.Size.X,
            fontSize, Ui.Text);
    }

    // The separator between the scene and the panel (owner, 2026-10-16): a bar across the screen at the scene's foot in the
    // frames' colour, lit from above, an accent line along its top brightest in the middle, a shadow cast on the panel.
    private static Rect2 SeparatorArea => new(0, SceneFoot - 4, 1280, 22);
    private const float SeparatorTop = 2, SeparatorBar = 6, SeparatorShadow = 12;

    private void DrawSeparator()
    {
        float s = _scale, w = _separator.Size.X;
        float top = SeparatorTop * s, bar = Math.Max(2, SeparatorBar * s), bottom = top + bar;
        var shadow = new Color(0, 0, 0, 0.45f);
        _separator.DrawRect(new Rect2(0, top - Math.Max(1, s), w, Math.Max(1, s)), shadow);           // its edge on the scene
        _separator.DrawPolygon([new(0, bottom), new(w, bottom), new(w, bottom + SeparatorShadow * s), new(0, bottom + SeparatorShadow * s)],
            [shadow, shadow, shadow with { A = 0 }, shadow with { A = 0 }]);
        var (light, dark) = (Ui.Gold.Lightened(0.3f), Ui.Gold.Darkened(0.45f));
        _separator.DrawPolygon([new(0, top), new(w, top), new(w, bottom), new(0, bottom)], [light, light, dark, dark]);
        float line = Math.Max(1, 1.5f * s);
        var faint = Ui.HoverLine with { A = 0.15f };
        var bright = Ui.HoverLine with { A = 0.85f };
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
