using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Our own battle screen (Option → Interface → New, docs/ui/battle-screen.md, "The layout" and "The
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
    private static readonly Rect2 StripArea = new(10, 414, 1260, 32);
    private static readonly Rect2 PlayerNameArea = new(10, 448, 574, 24);
    private static readonly Rect2 EnemyNameArea = new(696, 448, 574, 24);
    private static readonly Rect2 GaugeArea = new(594, 474, 92, 308);
    private static readonly Rect2 SpeakerLeft = new(8, 6, 300, 398);
    private static readonly Rect2 SpeakerRight = new(972, 6, 300, 398);
    private static readonly Vector2 CardSize = new(282, 150);
    private static readonly float[] CardRows = [474, 632];
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
    private readonly ArmySlide _playerArmy, _enemyArmy;
    private bool _armiesInstant, _jumpArmies = true;
    private readonly TextureRect _windows;
    private readonly BattleCommandLayer _commands;
    private readonly BattleDialogLayer _dialog;

    /// <summary>Layout pixels to a script pixel of the windows' layer.</summary>
    private static float WindowScale => WindowsArea.Size.X / Surface.ScreenWidth;
    private readonly TextureRect _speaker;
    private readonly OrderStrip _strip;
    private readonly Label _playerName, _enemyName;
    private readonly Control _gauge;
    private readonly Dictionary<BattlePlace, (PanelContainer Card, StyleBoxFlat Box)> _cards = [];
    private readonly Control _actingFrame;
    private BattlePlace? _actingPlace;
    private ulong _blinkFrom;
    private bool _blinkOn;
    private readonly Dictionary<int, ImageTexture?> _portraits = [];
    private ImageTexture? _fieldTexture, _frontTexture, _sidesTexture, _windowsTexture;
    private byte[]? _frontRgba, _windowsRgba;
    private int _sceneVersion = -1, _pictureVersion = -1;
    private string _cardsKey = "?", _stripKey = "?", _speakerKey = "?";
    private (int Rounds, int Left) _gaugeValues;
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
        _front = Layer();
        VisibilityChanged += () => _jumpArmies = true;                     // shown again: the armies stand where they are

        _speaker = Picture();                                               // the portrait has its own frame and name
        _speaker.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _speaker.Visible = false;
        AddChild(_speaker);

        _windows = Picture();
        AddChild(_windows);

        _strip = new OrderStrip();
        AddChild(_strip);
        _playerName = new Label { MouseFilter = MouseFilterEnum.Ignore };
        _enemyName = new Label { MouseFilter = MouseFilterEnum.Ignore, HorizontalAlignment = HorizontalAlignment.Right };
        AddChild(_playerName);
        AddChild(_enemyName);
        _gauge = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _gauge.Draw += DrawGauge;
        AddChild(_gauge);
        foreach (var place in SlotSnapshot.Places)
        {
            var box = new StyleBoxFlat { BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2 };
            var card = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            card.AddThemeStyleboxOverride("panel", box);
            AddChild(card);
            _cards[place] = (card, box);
        }
        _spotlight = new Control { MouseFilter = MouseFilterEnum.Ignore, ZIndex = 2 };   // over the cards and the strip (1), under the acting frame
        _spotlight.Draw += DrawSpotlight;
        AddChild(_spotlight);
        _actingFrame = new Control { MouseFilter = MouseFilterEnum.Ignore, Visible = false, ZIndex = 2 };    // over the cards and the spotlight
        _actingFrame.Draw += DrawActingFrame;
        AddChild(_actingFrame);
        _commands = new BattleCommandLayer();                               // over the cards
        AddChild(_commands);
        _dialog = new BattleDialogLayer();                                  // over everything
        AddChild(_dialog);
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
        _cardsKey = _stripKey = "?";
        _gauge.QueueRedraw();
    }

    /// <summary>Option → Fonts: the screen's text in the chosen font.</summary>
    public void SetFont(GameFont font) => _theme.DefaultFont = DeploymentTableView.LoadFont(font);

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
        PlaceArmies();
        At(_windows, WindowsArea);
        At(_strip, StripArea);
        _strip.SetScale(scale);
        At(_playerName, PlayerNameArea);
        At(_enemyName, EnemyNameArea);
        At(_gauge, GaugeArea);
        At(_speaker, _speakerArea);
        At(_spotlight, new Rect2(0, 0, 1280, 800));
        _spotlight.QueueRedraw();
        At(_commands, new Rect2(0, 0, 1280, 800));
        _commands.Layout(CardArea, scale);
        At(_dialog, new Rect2(0, 0, 1280, 800));
        _dialog.Layout(scale);
        foreach (var (place, (card, _)) in _cards)
            At(card, CardArea(place));
        if (_actingPlace is { } acting)
            At(_actingFrame, CardArea(acting));
        _theme.DefaultFontSize = Math.Max(8, (int)Math.Round(TextSize * scale));
    }

    /// <summary>
    /// Fills the screen from the presenter: the scene's picture when it changed, the windows' layer when
    /// <paramref name="screenChanged"/> (with the input screen's own windows, <paramref name="inputWindows"/>),
    /// the cards (<paramref name="hovered"/> lit), the strip, the gauge and the speaker when what they show changed.
    /// <paramref name="instant"/>: the strip's chips jump to their places; <paramref name="slidesInstant"/>: the armies too.
    /// </summary>
    public void Refresh(GamePresenter presenter, IEnumerable<WindowShape> inputWindows, bool screenChanged, BattlePlace? hovered,
        bool instant, bool slidesInstant)
    {
        if (presenter.State is not { } s)
            return;
        _armiesInstant = slidesInstant;
        if (presenter.Scene.Version != _sceneVersion)
        {
            _sceneVersion = presenter.Scene.Version;
            UploadScene(presenter.Scene);
        }
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
        RefreshCards(s, presenter.Order, hovered);
        RefreshSpotlight(s, presenter.Scene);
        RefreshStrip(s, presenter.Order, instant);
        var gauge = (Math.Max(presenter.Order.Rounds, s.Bt_RoundsLeft.Value), s.Bt_RoundsLeft.Value);
        if (gauge != _gaugeValues)
        {
            _gaugeValues = gauge;
            _gauge.QueueRedraw();
        }
        var (player, enemy) = BattleBoard.Armies(s);
        _playerName.Text = player;
        _enemyName.Text = enemy;
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
    private readonly List<Rect2> _spotHoles = [];                    // the fighters' cards, layout pixels
    private float _spotShown;                                        // 0: off, 1: whole
    private float _spotDrawnDarkness;
    private bool _spotOn;

    /// <summary>
    /// While an exchange is on (<see cref="BattleScene.InEngagement"/>) the spotlight falls on its two fighters'
    /// cards: the player's unit (<c>Unit_Cur</c>) and the enemy's commander (<c>Cmdr_Cur</c>).
    /// </summary>
    private void RefreshSpotlight(GameState s, BattleScene scene)
    {
        bool on = scene.Active && scene.InEngagement;
        var holes = new List<Rect2>(2);
        if (on)
        {
            foreach (var place in SlotSnapshot.Places)
            {
                int fighter = place.Side == BattleSide.Player ? s.Unit_Cur.Value : s.Cmdr_Cur.Value;
                if (fighter != 0 && BattleBoard.Record(s, place) == fighter)
                    holes.Add(CardArea(place));
            }
        }
        if (on == _spotOn && (!on || holes.SequenceEqual(_spotHoles)))
            return;
        _spotOn = on;
        if (on)
        {
            _spotHoles.Clear();
            _spotHoles.AddRange(holes);                                  // when it goes off, the holes stay while it fades
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
        foreach (var hole in _spotHoles)
            dark = dark.SelectMany(r => Without(r, hole)).ToList();
        foreach (var r in dark)
            _spotlight.DrawRect(new Rect2(r.Position * _scale, r.Size * _scale), colour);
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
    private void RefreshCards(GameState s, RoundOrder order, BattlePlace? hovered)
    {
        var states = order.States(s);                                    // at a round header, the round to come
        var cards = SlotSnapshot.Places.Select(p => BattleBoard.Card(s, p, states)).ToList();
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

    public override void _Process(double delta)
    {
        StepArmies(delta);
        StepSpotlight(delta);
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

    private Label Empty(BattlePlace place) => Line($"{place.Slot}  empty", _colours.Muted, center: true);

    /// <summary>A card: the face (the enemy's on the right), place and name, troop type, troops and HP with bars, the player's standing order, "Acting" / "Acted".</summary>
    private Control Contents(BattleCard card, bool acting, int pad)
    {
        var line = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        line.AddThemeConstantOverride("separation", pad * 2);
        int size = (int)Math.Round(FaceSize * _scale);
        var face = new TextureRect
        {
            CustomMinimumSize = new Vector2(size, size),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepCentered,
            SizeFlagsVertical = SizeFlags.ShrinkCenter,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        if (card.Picture != 0)
            face.Texture = _faces.SquareTexture(card.Picture, _faces.CropOf(card.Picture, _crops), size);

        var text = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        text.AddThemeConstantOverride("separation", 0);
        var name = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        name.AddThemeConstantOverride("separation", pad * 2);
        name.AddChild(Line(card.Place.Slot.ToString(), _colours.Muted, expand: false));
        name.AddChild(Line(card.Name));
        text.AddChild(name);
        text.AddChild(Line(card.TroopType == "" ? "—" : card.TroopType, _colours.Muted));
        var side = card.Place.Side == BattleSide.Player ? PlayerColour : EnemyColour;
        text.AddChild(Line($"Troops {card.Troops}/{card.MaxTroops}"));
        text.AddChild(Bar(card.MaxTroops > 0 ? (float)card.Troops / card.MaxTroops : 0, side));
        text.AddChild(Line($"HP {card.Hp}/{card.MaxHp}"));
        text.AddChild(Bar(card.MaxHp > 0 ? (float)card.Hp / card.MaxHp : 0, HpColour));
        string state = card.Order switch
        {
            StandingOrder.AutoAttack => "Auto-Atk",
            StandingOrder.AutoDefend => "Auto-Def",
            StandingOrder.Manual => "Manual",
            _ => "",
        };
        if (card.Defending)
            state += (state == "" ? "" : " · ") + "Defending";
        if (card.Focus > 0)
            state += (state == "" ? "" : " · ") + $"Focus {card.Focus}";
        if (acting)
            state += (state == "" ? "" : " · ") + "Acting";
        else if (card.Acted && !card.Defending)
            state += (state == "" ? "" : " · ") + "Acted";
        text.AddChild(Line(state, _colours.Muted));

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

    private Control Bar(float fraction, Color colour)
    {
        var back = new ColorRect
        {
            Color = _colours.Back,
            CustomMinimumSize = new Vector2(0, Math.Max(2, (float)Math.Round(5 * _scale))),
            MouseFilter = MouseFilterEnum.Ignore,
        };
        var fill = new ColorRect { Color = colour, MouseFilter = MouseFilterEnum.Ignore };
        fill.AnchorRight = Math.Clamp(fraction, 0, 1);
        fill.AnchorBottom = 1;
        back.AddChild(fill);
        return back;
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
    /// <paramref name="instant"/> (the message skip) and when a new round begins (owner, 2026-10-12: the
    /// units that acted go back into the order, no jumps).
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
        _strip.Show(chips, instant || newRound);
    }

    private (int Battle, int Round) _stripRound = (-1, -1);

    /// <summary>One segment per round of the battle, the spent ones dark, "n left" below.</summary>
    private void DrawGauge()
    {
        var (rounds, left) = _gaugeValues;
        var size = _gauge.Size;
        var font = _theme.DefaultFont;
        int fontSize = _theme.DefaultFontSize;
        float labelHeight = fontSize * 1.6f;
        if (rounds > 0)
        {
            float gap = 4 * _scale, width = size.X * 0.5f, x = (size.X - width) / 2;
            float height = (size.Y - labelHeight - gap * (rounds - 1)) / rounds;
            for (int i = 0; i < rounds; i++)
            {
                bool spent = i < rounds - left;
                var rect = new Rect2(x, i * (height + gap), width, height);
                _gauge.DrawRect(rect, spent ? _colours.ButtonDisabled : _colours.Accent.Darkened(0.2f));
                _gauge.DrawRect(rect, _colours.Border, filled: false, width: 1);
            }
        }
        if (font is not null)
            _gauge.DrawString(font, new Vector2(0, size.Y - fontSize * 0.4f), $"{left} left", HorizontalAlignment.Center, size.X, fontSize, _colours.Text);
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
