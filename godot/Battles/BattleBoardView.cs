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
/// gauge between them. It decides nothing: <see cref="GameScreen"/> turns a pointer over a card into
/// the script's pointer on that card's panel (<see cref="ToScript"/>), and elsewhere into the point of
/// the windows' layer under it.
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

    private static Rect2 CardArea(BattlePlace place) =>
        new(Column(place), CardRows[(place.Slot - 1) % 2], CardSize.X, CardSize.Y);

    private static readonly Color PlayerColour = new(0.27f, 0.46f, 0.86f);
    private static readonly Color EnemyColour = new(0.82f, 0.3f, 0.3f);
    private static readonly Color HpColour = new(0.42f, 0.76f, 0.42f);

    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private readonly FacePictures _faces;
    private FaceCrops _crops = new();
    private readonly Theme _theme = new();
    private readonly ColorRect _back;
    private readonly Control _sidesClip;
    private readonly TextureRect _sides;
    private readonly TextureRect _scene;
    private readonly TextureRect _windows;
    private readonly PanelContainer _speaker;
    private readonly StyleBoxFlat _speakerBox;
    private readonly TextureRect _speakerFace;
    private readonly Label _speakerName;
    private readonly HBoxContainer _strip;
    private readonly Label _playerName, _enemyName;
    private readonly Control _gauge;
    private readonly Dictionary<BattlePlace, (PanelContainer Card, StyleBoxFlat Box)> _cards = [];
    private readonly Dictionary<int, ImageTexture?> _portraits = [];
    private ImageTexture? _sceneTexture, _sidesTexture, _windowsTexture;
    private byte[]? _sceneRgba, _windowsRgba;
    private int _sceneVersion = -1;
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
        _scene = Picture();
        AddChild(_scene);

        _speakerBox = new StyleBoxFlat { BorderWidthLeft = 3, BorderWidthTop = 3, BorderWidthRight = 3, BorderWidthBottom = 3 };
        _speaker = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore, Visible = false };
        _speaker.AddThemeStyleboxOverride("panel", _speakerBox);
        var speakerColumn = new VBoxContainer { MouseFilter = MouseFilterEnum.Ignore };
        speakerColumn.AddThemeConstantOverride("separation", 0);
        _speakerFace = Picture();
        _speakerFace.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        _speakerFace.SizeFlagsVertical = SizeFlags.ExpandFill;
        speakerColumn.AddChild(_speakerFace);
        _speakerName = new Label { HorizontalAlignment = HorizontalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
        speakerColumn.AddChild(_speakerName);
        _speaker.AddChild(speakerColumn);
        AddChild(_speaker);

        _windows = Picture();
        AddChild(_windows);

        _strip = new HBoxContainer { MouseFilter = MouseFilterEnum.Ignore, Alignment = BoxContainer.AlignmentMode.Center };
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

    /// <summary>Option → Palette: the screen in <paramref name="palette"/>'s colours at once.</summary>
    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        _theme.SetColor("font_color", "Label", palette.Text);
        _back.Color = palette.Back;
        _speakerBox.BgColor = palette.Back;
        _speakerBox.BorderColor = palette.Accent;
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
        At(_scene, SceneArea);
        At(_windows, WindowsArea);
        At(_strip, StripArea);
        At(_playerName, PlayerNameArea);
        At(_enemyName, EnemyNameArea);
        At(_gauge, GaugeArea);
        At(_speaker, _speakerArea);
        foreach (var (place, (card, _)) in _cards)
            At(card, CardArea(place));
        _theme.DefaultFontSize = Math.Max(8, (int)Math.Round(TextSize * scale));
    }

    /// <summary>
    /// Fills the screen from the presenter: the scene's picture when it changed, the windows' layer when
    /// <paramref name="screenChanged"/> (with the input screen's own windows, <paramref name="inputWindows"/>),
    /// the cards, the strip, the gauge and the speaker when what they show changed.
    /// </summary>
    public void Refresh(GamePresenter presenter, IEnumerable<WindowShape> inputWindows, bool screenChanged, BattlePlace? hovered)
    {
        if (presenter.State is not { } s)
            return;
        if (presenter.Scene.Version != _sceneVersion)
        {
            _sceneVersion = presenter.Scene.Version;
            UploadScene(presenter.Scene.Surface);
        }
        if (screenChanged || _windowsTexture is null)
        {
            _windowsRgba = presenter.BattleWindows(inputWindows).Compose(presenter.Screen, _windowsRgba);
            var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
                false, Image.Format.Rgba8, _windowsRgba);
            Upload(ref _windowsTexture, image);
            _windows.Texture = _windowsTexture;
        }
        RefreshSpeaker(s);
        RefreshCards(s, presenter.Order, hovered);
        RefreshStrip(s, presenter.Order);
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

    /// <summary>The scene's 640×256 picture, and the sides: the same blurred (shrunk and grown again).</summary>
    private void UploadScene(Surface scene)
    {
        _sceneRgba = scene.ReadScreen(_sceneRgba);
        int w = BattleScene.Width * Surface.Scale, h = BattleScene.Height * Surface.Scale;
        var picture = Image.CreateFromData(w, h, false, Image.Format.Rgba8, _sceneRgba.AsSpan(0, w * h * 4).ToArray());
        Upload(ref _sceneTexture, picture);
        _scene.Texture = _sceneTexture;
        var blurred = (Image)picture.Duplicate();
        blurred.Resize(w / 16, h / 16, Image.Interpolation.Bilinear);
        blurred.Resize(w, h, Image.Interpolation.Bilinear);
        Upload(ref _sidesTexture, blurred);
        _sides.Texture = _sidesTexture;
    }

    private static void Upload(ref ImageTexture? texture, Image image)
    {
        if (texture is null || texture.GetWidth() != image.GetWidth() || texture.GetHeight() != image.GetHeight())
            texture = ImageTexture.CreateFromImage(image);
        else
            texture.Update(image);
    }

    /// <summary>While a bubble speaks (<see cref="BattleBoard.Speaker"/>): the speaker's whole face on its side, framed, the name below.</summary>
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
        _speakerFace.Texture = face;
        _speakerName.Text = speaker.Name;
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

    // ------------------------------------------------------------------ the cards

    private void RefreshCards(GameState s, RoundOrder order, BattlePlace? hovered)
    {
        var states = order.States(s);                                    // at a round header, the round to come
        var cards = SlotSnapshot.Places.Select(p => BattleBoard.Card(s, p, states)).ToList();
        string key = string.Join(";", cards) + $"|{order.Acting}|{hovered}";
        if (key == _cardsKey)
            return;
        _cardsKey = key;
        for (int i = 0; i < cards.Count; i++)
        {
            var place = SlotSnapshot.Places[i];
            var (panel, box) = _cards[place];
            var card = cards[i];
            bool acting = order.Acting == place && card is not null && card.Record == order.ActingRecord;
            box.BgColor = card is null ? _colours.Back : _colours.Card;
            box.BorderColor = acting ? _colours.Accent : hovered == place && card is not null ? _colours.ButtonBorder : _colours.Border;
            int pad = Math.Max(1, (int)Math.Round(4 * _scale));
            box.ContentMarginLeft = box.ContentMarginRight = box.ContentMarginTop = box.ContentMarginBottom = pad;
            foreach (var child in panel.GetChildren())
            {
                panel.RemoveChild(child);
                child.QueueFree();
            }
            panel.AddChild(card is null ? Empty(place) : Contents(card, acting, pad));
        }
    }

    private Label Empty(BattlePlace place) => Line($"{place.Slot}  empty", _colours.Muted, center: true);

    /// <summary>A card: the face (dimmed once the unit has acted, but while it acts; the enemy's on the right), place and name, troop type, troops and HP with bars, the player's standing order.</summary>
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
            Modulate = card.Acted && !acting ? new Color(0.45f, 0.45f, 0.45f) : Colors.White,
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

    /// <summary>The round's order: the acted grey, the acting framed, the rest in their side's colour.</summary>
    private void RefreshStrip(GameState s, RoundOrder order)
    {
        var chips = order.Chips(s);
        string key = string.Join(";", chips);
        if (key == _stripKey)
            return;
        _stripKey = key;
        foreach (var child in _strip.GetChildren())
        {
            _strip.RemoveChild(child);
            child.QueueFree();
        }
        _strip.AddThemeConstantOverride("separation", (int)Math.Round(4 * _scale));
        foreach (var chip in chips)
        {
            var side = chip.Side == BattleSide.Player ? PlayerColour : EnemyColour;
            int pad = (int)Math.Round(6 * _scale);
            var box = new StyleBoxFlat
            {
                BgColor = chip.State == ChipState.Acted ? _colours.ButtonDisabled : side.Darkened(0.35f),
                BorderColor = chip.State == ChipState.Acting ? _colours.Accent : side,
                BorderWidthLeft = 2, BorderWidthTop = 2, BorderWidthRight = 2, BorderWidthBottom = 2,
                ContentMarginLeft = pad, ContentMarginRight = pad,
            };
            if (chip.State == ChipState.Acted)
                box.BorderColor = _colours.ButtonDisabled;
            var panel = new PanelContainer { MouseFilter = MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", box);
            panel.AddChild(Line(chip.Name, chip.State == ChipState.Acted ? _colours.GreyText : _colours.Text, expand: false, center: true));
            _strip.AddChild(panel);
        }
    }

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

    /// <summary>
    /// The script's point for a pointer at <paramref name="local"/>: on a card, the centre of its slot
    /// panel (<see cref="BattleBoard.PanelCentre"/>); elsewhere the
    /// point of the windows' layer under it, clamped to the screen.
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
    public Vector2 FromScript(Vector2I script) =>
        _origin + WindowsArea.Position * _scale + (new Vector2(script.X, script.Y) + new Vector2(0.5f, 0.5f)) * (WindowsArea.Size.X / Surface.ScreenWidth * _scale);
}
