using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The battle's opening on our battle screen (Option → Interface → New; docs/ui/battle-screen.md, "The opening";
/// owner, 2026-10-14, variant C after Sengoku Rance's): over the scene while the battle's first round header waits
/// for its click (<see cref="BattleOpening"/>), fading in over the field (<see cref="BattleScreenSettings.OpeningAppearSeconds"/>) — the battle's own field cut in two along a slanted seam, the player's
/// half tinted blue and the enemy's red, each with its army's name (Shojumaru, wrapped into up to three lines) and its
/// advantage, "VS" between them, a plate on top with the battle's kind and place, its limit and its rounds (Cinzel).
/// After the click the halves part to the sides and the plate rises (<see cref="BattleScreenSettings.OpeningSeconds"/>),
/// uncovering the scene. It decides nothing: the click is the header's key wait, the script's. Points are layout
/// pixels of the 1280×800 screen.
/// </summary>
internal sealed partial class BattleOpeningLayer : Control
{
    private static readonly Rect2 SidesArea = new(0, 0, 1280, 410);
    private static readonly Rect2 SceneArea = new(128, 0, 1024, 410);
    private const float SeamTop = 680, SeamBottom = 600, Height = 410;
    private const float NameEdge = 50, NameGap = 90, NameBase = 330, NameSize = 64, NameMin = 24, AdvantageSize = 28;
    private const float VsSize = 84, VsY = 300, Leave = 700, PlateRise = 200;
    private static readonly Rect2 Plate = new(400, 16, 480, 134);
    private static readonly Color PlayerTint = new(69 / 255f, 117 / 255f, 219 / 255f, 0.47f);
    private static readonly Color EnemyTint = new(209 / 255f, 77 / 255f, 77 / 255f, 0.47f);
    private static readonly Color Ink = new(20 / 255f, 20 / 255f, 30 / 255f), AdvantageColour = new(1, 236 / 255f, 170 / 255f);
    private static readonly Color VsInk = new(40 / 255f, 20 / 255f, 10 / 255f);
    private const string NameFont = "res://fonts/Shojumaru-Regular.ttf", PlateFont = "res://fonts/Cinzel-VariableFont_wght.ttf";

    private readonly FontFile _names = Load(NameFont);
    private readonly FontVariation _plate = new() { BaseFont = Load(PlateFont) };
    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private readonly Half _player, _enemy;
    private readonly Control _top;
    private BattleOpening? _opening;
    private double _leaving = -1;                   // seconds into the leave; -1 when not leaving
    private double _appearing;                      // seconds since the opening came up (it fades in)
    private float _scale = 1;

    public BattleOpeningLayer()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;                        // the plate rises out of the scene, not over the menu bar
        Visible = false;
        _plate.VariationOpentype = new Godot.Collections.Dictionary
        {
            [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = 700,
        };
        _player = new Half(this, left: true);
        _enemy = new Half(this, left: false);
        AddChild(_player);
        AddChild(_enemy);
        _top = new Control { MouseFilter = MouseFilterEnum.Ignore };
        _top.Draw += DrawTop;
        AddChild(_top);
    }

    private static FontFile Load(string path) => new() { Data = Godot.FileAccess.GetFileAsBytes(path) };

    /// <summary>Whether the opening shows, standing or leaving.</summary>
    public bool Active => Visible;

    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        _top.QueueRedraw();
    }

    /// <summary>Lays the layer over the scene's area, <paramref name="scale"/> window pixels to a layout pixel (its position is set by the screen).</summary>
    public void Layout(float scale)
    {
        _scale = scale;
        Size = SidesArea.Size * scale;
        _top.Size = Size;
        _player.Layout(scale);
        _enemy.Layout(scale);
        Step(0);
    }

    /// <summary>
    /// The scene's pictures the halves carry (the blurred sides, the battlefield and its front), as our screen shows them
    /// now; the layer stands while <paramref name="opening"/> is given, leaves when it goes (at once when <paramref name="instant"/>).
    /// </summary>
    public void Refresh(BattleOpening? opening, Texture2D? sides, Texture2D? field, Texture2D? front, bool instant)
    {
        if (opening is not null)
        {
            if (opening != _opening || _leaving >= 0 || !Visible)
            {
                _opening = opening;
                _leaving = -1;
                Visible = true;
                _appearing = instant ? double.MaxValue : 0;     // the field alone first, then the opening fades in (owner, 2026-10-14)
                Appear();
                _player.Show(opening.PlayerArmy, opening.PlayerEffects.Text);
                _enemy.Show(opening.EnemyArmy, opening.EnemyEffects.Text);
                Step(0);
            }
            _player.SetPictures(sides, field, front);
            _enemy.SetPictures(sides, field, front);
            return;
        }
        if (!Visible)
            return;
        if (_leaving < 0)
            _leaving = 0;                           // the click: the halves part
        if (instant)
            Hide();
    }

    /// <summary>How far the opening has faded in, 0 (the field alone) to 1 (<see cref="BattleScreenSettings.OpeningAppearSeconds"/>).</summary>
    public float Shown => Visible ? Modulate.A : 0;

    private void Appear()
    {
        double seconds = BattleScreenSettings.Get(BattleScreenSettings.OpeningAppearSeconds);
        Modulate = new Color(1, 1, 1, seconds <= 0 ? 1 : (float)Math.Min(1, _appearing / seconds));
    }

    public override void _Process(double delta)
    {
        if (!Visible)
            return;
        if (Modulate.A < 1)
        {
            _appearing += delta;
            Appear();
        }
        if (_leaving < 0)
            return;
        _leaving += delta;
        double seconds = BattleScreenSettings.Get(BattleScreenSettings.OpeningSeconds);
        if (seconds <= 0 || _leaving >= seconds)
        {
            Hide();
            return;
        }
        Step((float)(_leaving / seconds));
    }

    private new void Hide()
    {
        Visible = false;
        _opening = null;
        _leaving = -1;
    }

    /// <summary>Places the halves and the plate <paramref name="t"/> (0-1) into the leave, eased in.</summary>
    private void Step(float t)
    {
        float ease = t * t;
        _player.Position = new Vector2(-Leave * ease * _scale, 0);
        _enemy.Position = new Vector2(Leave * ease * _scale, 0);
        _rise = PlateRise * ease;
        _top.QueueRedraw();
    }

    private float _rise;

    /// <summary>The seams on the halves' edges, "VS" while standing, the plate.</summary>
    private void DrawTop()
    {
        if (_opening is not { } o)
            return;
        float s = _scale;
        float width = Math.Max(2, 4 * s);
        _top.DrawLine(new Vector2(SeamTop, 0) * s + _player.Position, new Vector2(SeamBottom, Height) * s + _player.Position, Colors.White, width, true);
        if (_leaving > 0)
            _top.DrawLine(new Vector2(SeamTop, 0) * s + _enemy.Position, new Vector2(SeamBottom, Height) * s + _enemy.Position, Colors.White, width, true);
        else
            Outlined(_top, _names, new Vector2(640, VsY) * s, "VS", Px(VsSize), _colours.Accent, VsInk, Px(8), HorizontalAlignment.Center, centreY: true);

        var plate = new Rect2((Plate.Position - new Vector2(0, _rise)) * s, Plate.Size * s);
        var box = new StyleBoxFlat
        {
            BgColor = new Color(_colours.Card, 0.94f),
            BorderColor = _colours.Accent,
            ShadowColor = new Color(0, 0, 0, 0.55f),
            ShadowSize = Px(8),
            ShadowOffset = new Vector2(0, 5 * s),
            AntiAliasing = true,
        };
        box.SetCornerRadiusAll(Px(10));
        box.SetBorderWidthAll(Math.Max(1, Px(3)));
        _top.DrawStyleBox(box, plate);
        var bar = new StyleBoxFlat { BgColor = _colours.Card.Darkened(0.2f) };
        bar.SetCornerRadiusAll(Px(6));
        _top.DrawStyleBox(bar, new Rect2(plate.Position + new Vector2(8, 8) * s, new Vector2(Plate.Size.X - 16, 50) * s));
        void Line(string text, float y, int size, Color colour) =>
            _top.DrawString(_plate, new Vector2(plate.Position.X, plate.Position.Y + y * s + Centred(_plate, size)), text,
                HorizontalAlignment.Center, plate.Size.X, size, colour);
        Line(o.Title, 33, Px(30), _colours.Text);
        Line(o.Limit, 84, Px(22), _colours.Text);
        if (o.Turns is { } turns)
            Line(turns == 1 ? "1 turn" : $"{turns} turns", 115, Px(20), _colours.Muted);
    }

    /// <summary>The baseline offset that centres a line of <paramref name="size"/> on a point.</summary>
    private static float Centred(Font font, int size) => (font.GetAscent(size) - font.GetDescent(size)) / 2;

    private int Px(float size) => Math.Max(1, (int)Math.Round(size * _scale));

    /// <summary>Text with a dark outline (the outline first, the text over it).</summary>
    private static void Outlined(CanvasItem canvas, Font font, Vector2 at, string text, int size, Color colour, Color ink, int outline,
        HorizontalAlignment align, bool centreY = false, float width = -1)
    {
        var pos = align switch
        {
            HorizontalAlignment.Center => at - new Vector2(width < 0 ? font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X / 2 : width / 2, 0),
            HorizontalAlignment.Right => at - new Vector2(font.GetStringSize(text, HorizontalAlignment.Left, -1, size).X, 0),
            _ => at,
        };
        if (centreY)
            pos.Y += Centred(font, size);
        canvas.DrawStringOutline(font, pos, text, HorizontalAlignment.Left, -1, size, outline, ink);
        canvas.DrawString(font, pos, text, HorizontalAlignment.Left, -1, size, colour);
    }

    /// <summary>
    /// One half of the opening: its own copy of the scene's pictures and its side's tint, clipped to its side of the
    /// seam (the half draws the mask, its children show only inside it), with its army's name and advantage.
    /// </summary>
    private sealed partial class Half : Control
    {
        private readonly BattleOpeningLayer _layer;
        private readonly bool _left;
        private readonly TextureRect _sides, _field, _front;
        private readonly ColorRect _tint;
        private readonly Control _text;
        private string _name = "";
        private string _advantage = "";                // what the side's advantage does, "" without one
        private float _scale = 1;

        public Half(BattleOpeningLayer layer, bool left)
        {
            _layer = layer;
            _left = left;
            MouseFilter = MouseFilterEnum.Ignore;
            ClipChildren = ClipChildrenMode.Only;
            _sides = Picture(TextureRect.StretchModeEnum.KeepAspectCovered);
            _sides.Modulate = new Color(0.45f, 0.45f, 0.45f);           // as the screen's blurred sides
            _field = Picture(TextureRect.StretchModeEnum.Scale);
            _front = Picture(TextureRect.StretchModeEnum.Scale);
            _tint = new ColorRect { Color = left ? PlayerTint : EnemyTint, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(_tint);
            _text = new Control { MouseFilter = MouseFilterEnum.Ignore };
            _text.Draw += DrawText;
            AddChild(_text);
        }

        private TextureRect Picture(TextureRect.StretchModeEnum stretch)
        {
            var picture = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = stretch,
                TextureFilter = TextureFilterEnum.Linear,
                MouseFilter = MouseFilterEnum.Ignore,
            };
            AddChild(picture);
            return picture;
        }

        public void Layout(float scale)
        {
            _scale = scale;
            Size = SidesArea.Size * scale;
            (_sides.Position, _sides.Size) = (Vector2.Zero, Size);
            (_field.Position, _field.Size) = (SceneArea.Position * scale, SceneArea.Size * scale);
            (_front.Position, _front.Size) = (_field.Position, _field.Size);
            (_tint.Position, _tint.Size) = (Vector2.Zero, Size);
            (_text.Position, _text.Size) = (Vector2.Zero, Size);
            QueueRedraw();
            _text.QueueRedraw();
        }

        public void SetPictures(Texture2D? sides, Texture2D? field, Texture2D? front) =>
            (_sides.Texture, _field.Texture, _front.Texture) = (sides, field, front);

        public void Show(string name, string advantage)
        {
            (_name, _advantage) = (name, advantage);
            _text.QueueRedraw();
        }

        /// <summary>The mask: this side of the seam (the scene's left part to the seam, or from it to the right).</summary>
        public override void _Draw()
        {
            float s = _scale;
            Vector2[] points = _left
                ? [new(0, 0), new(SeamTop * s, 0), new(SeamBottom * s, Height * s), new(0, Height * s)]
                : [new(SeamTop * s, 0), new(1280 * s, 0), new(1280 * s, Height * s), new(SeamBottom * s, Height * s)];
            DrawColoredPolygon(points, Colors.White);
        }

        /// <summary>
        /// The army's name, its lines ending at <see cref="NameBase"/>, kept <see cref="NameGap"/> off the seam (the
        /// fewest lines up to three, balanced, at the largest size from <see cref="NameSize"/> that fits), and its advantage.
        /// </summary>
        private void DrawText()
        {
            if (_name == "")
                return;
            var font = _layer._names;
            float s = _scale;
            float room = _left ? SeamTop - 80 * NameBase / Height - NameGap - NameEdge : 1280 - NameEdge - SeamTop - NameGap;
            float size = NameSize;
            IReadOnlyList<string>? lines = null;
            for (; size >= NameMin; size -= 4)
            {
                int px = (int)Math.Round(size * s);
                lines = BattleOpening.Wrap(_name, 3, line => font.GetStringSize(line, HorizontalAlignment.Left, -1, px).X / s, room);
                if (lines is not null)
                    break;
            }
            lines ??= [_name];
            size = Math.Max(size, NameMin);
            int namePx = (int)Math.Round(size * s);
            var align = _left ? HorizontalAlignment.Left : HorizontalAlignment.Right;
            float x = (_left ? NameEdge : 1280 - NameEdge) * s;
            for (int i = 0; i < lines.Count; i++)
            {
                float baseline = (NameBase - (lines.Count - 1 - i) * size) * s;
                Outlined(_text, font, new Vector2(x, baseline), lines[i], namePx, Colors.White, Ink, (int)Math.Round(10 * s), align);
            }
            if (_advantage != "")
                Outlined(_text, font, new Vector2(x, (NameBase + 42) * s), _advantage, (int)Math.Round(AdvantageSize * s),
                    AdvantageColour, Ink, (int)Math.Round(6 * s), align);
        }
    }
}
