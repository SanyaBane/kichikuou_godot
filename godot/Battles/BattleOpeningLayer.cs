using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The battle's opening on our battle screen (our interface; docs/ui/battle-screen.md, "The opening";
/// owner, 2026-10-14, variant C after Sengoku Rance's): over the scene while the battle's first round header waits
/// for its click (<see cref="BattleOpening"/>), fading in over the field (<see cref="BattleScreenSettings.OpeningAppearSeconds"/>) — the battle's own field cut in two along a slanted seam, the player's
/// half tinted blue and the enemy's red, each with its army's name (Shojumaru, wrapped into up to three lines) and its
/// advantage, "VS" between them, a plate on top with the battle's kind and place, its limit and its rounds (Cinzel).
/// After the click (<see cref="BattleScreenSettings.OpeningSeconds"/>) the tints, the seam and "VS" fade where they are,
/// the names part to the sides and the plate rises (owner, 2026-10-15; the halves parted with their pictures before),
/// uncovering the scene. It decides nothing: the click is the header's key wait, the script's. Points are layout
/// pixels of the 1280×800 screen. On the phone held landscape (docs/ui/phone-build.md, step L4; LB3o1) it covers the
/// lower scene band, laid out again at the phone's type: the plate wider, the names centred beside it.
/// </summary>
internal sealed partial class BattleOpeningLayer : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Battle);

    private static bool Phone => Device.Layout == DeviceLayout.PhoneLandscape;

    // Held upright (docs/ui/phone-build.md, step P4; PB4o3) it covers the scene at the board's top, laid out again at the
    // phone's type: the plate on top, the names at its sides, VS between them (PhoneBattleStatesUpright's places).
    private static bool Upright => Device.Layout == DeviceLayout.PhoneUpright;

    /// <summary>The scene's band the layer covers: the PC's 410, the phone's lower one, upright the board's scene.</summary>
    private static float Height => Phone ? PhoneBattle.SceneBand : Upright ? PhoneBattleUpright.SceneFoot : 410;
    private static Vector2 AreaSize => new(1280, Height);
    private const float SeamTop = 680, SeamBottom = 600;

    /// <summary>The seam's x at the area's top and foot: upright the mock-up's split.</summary>
    private static (float Top, float Foot) Seam => Upright ? PhoneBattleStatesUpright.Seam : (SeamTop, SeamBottom);
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

    private static FontFile Load(string path) => Lacquer.LoadFont(path);

    /// <summary>The plate's font, Cinzel Bold (the rounds gauge's number shares it).</summary>
    public Font CinzelBold => _plate;

    /// <summary>Whether the opening shows, standing or leaving.</summary>
    public bool Active => Visible;

    /// <summary>Lays the layer over the scene's area, <paramref name="scale"/> window pixels to a layout pixel (its position is set by the screen).</summary>
    public void Layout(float scale)
    {
        _scale = scale;
        Size = AreaSize * scale;
        _top.Size = Size;
        SplitTitle();                               // the device may have changed: the plate's type and width with it
        _player.Layout(scale);
        _enemy.Layout(scale);
        Step(0);
    }

    // The title's lines on the plate and how much taller the plate stands for its second (layout pixels; owner, 2026-11-04:
    // "Great Wall of Helman A — Offensive Battle" is wider than every device's plate, so a battle's title is always its
    // place over its kind, a duel's and a dungeon's one line; the plate one line taller, the limit and turns under it and, on a phone, what stands under the
    // plate lowered — VS and, upright, the names' room, its foot kept over the advantage).
    private string[] _title = [];
    private float _taller;

    /// <summary>The title's type and middle on the plate (layout pixels), the plate and its bar's inset.</summary>
    private static float TitleSize => Phone ? P(PhoneBattleStates.PlaceText) : Upright ? U(PhoneBattleStatesUpright.PlaceText) : 30;
    private static float TitleMiddle => Phone ? P(PhoneBattleStates.PlaceMiddle) : Upright ? U(PhoneBattleStatesUpright.PlaceMiddle) : 33;
    private static Rect2 PlateArea => Phone ? R(PhoneBattleStates.OpeningPlate) : Upright ? R(PhoneBattleStatesUpright.OpeningPlate) : Plate;
    private static float BarInset => Phone ? P(PhoneBattleStates.BarInset) : Upright ? U(PhoneBattleStatesUpright.BarInset) : 8;

    /// <summary>The title as its place over its kind ("Oaks" / "Offensive Battle"); a duel's and a dungeon's on one line.</summary>
    private void SplitTitle()
    {
        _title = [];
        _taller = 0;
        if (_opening is not { } o)
            return;
        int at = o.Title.IndexOf(" — ", StringComparison.Ordinal);
        if (at < 0)                                                         // "Duel", "Dungeon Battle": one line
        {
            _title = [o.Title];
            return;
        }
        _title = [o.Title[..at], o.Title[(at + 3)..]];
        _taller = TitlePitch;
    }

    /// <summary>The distance between the title's two lines' middles (layout pixels; the PC's 34 under a place of 30).</summary>
    private static float TitlePitch => TitleSize * 34 / 30;

    /// <summary>The kind's type under the place, the owner's B (2026-11-04): 19 against the place's 30 on the PC.</summary>
    private static float KindSize => TitleSize * 19 / 30;

    private float TitleWidth(string text, float size) => _plate.GetStringSize(text, HorizontalAlignment.Left, -1, Math.Max(1, (int)Math.Round(size))).X;

    /// <summary>The largest type up to <paramref name="largest"/> (else the title's) that fits <paramref name="text"/> in the bar, in window pixels.</summary>
    private int TitlePx(string text, float largest = 0)
    {
        float room = PlateArea.Size.X - 4 * BarInset, size = largest > 0 ? largest : TitleSize;
        while (size > 8 && TitleWidth(text, size) > room)
            size -= 1;
        return Px(size);
    }

    /// <summary>
    /// The layer stands while <paramref name="opening"/> is given, leaves when it goes (at once when <paramref name="instant"/>).
    /// Whether an opening came up now (a battle's first header).
    /// </summary>
    public bool Refresh(BattleOpening? opening, bool instant)
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
                SplitTitle();
                _player.Show(opening.PlayerArmy, opening.PlayerEffects.Text);
                _enemy.Show(opening.EnemyArmy, opening.EnemyEffects.Text);
                Step(0);
                return true;
            }
            return false;
        }
        if (!Visible)
            return false;
        if (_leaving < 0)
            _leaving = 0;                           // the click: the opening leaves
        if (instant)
            Hide();
        return false;
    }

    /// <summary>Whether the opening stands (shown, the click not given yet).</summary>
    public bool Standing => Visible && _leaving < 0;

    /// <summary>How much the opening covers the scene: its fade-in while standing, then down to 0 as it leaves.</summary>
    public float Cover => Visible ? Modulate.A * (1 - _left) : 0;

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

    /// <summary>
    /// <paramref name="t"/> (0-1) into the leave: the tints, the seam and "VS" fade as it goes, the names part and the
    /// plate rises, eased in.
    /// </summary>
    private void Step(float t)
    {
        float ease = t * t;
        _left = t;
        _player.Leave(-Leave * ease, 1 - t);
        _enemy.Leave(Leave * ease, 1 - t);
        _rise = (PlateRise + _taller) * ease;                                // a taller plate rises as much farther
        _top.QueueRedraw();
    }

    private float _rise, _left;

    /// <summary>The seams on the halves' edges, "VS" while standing, the plate.</summary>
    private void DrawTop()
    {
        if (_opening is not { } o)
            return;
        float s = _scale;
        float width = Math.Max(2, 4 * s);
        float shown = 1 - _left;
        _top.DrawLine(new Vector2(Seam.Top, 0) * s, new Vector2(Seam.Foot, Height) * s, new Color(Colors.White, shown), width, true);
        var vs = Phone ? new Vector2(PhoneBattleStates.OpeningVs.X, PhoneBattleStates.OpeningVs.Y)
            : Upright ? new Vector2(PhoneBattleStatesUpright.OpeningVs.X, PhoneBattleStatesUpright.OpeningVs.Y) : new Vector2(640, VsY);
        if (Phone)
            vs.Y += _taller;                                                // under the plate there: lowered with it
        else if (Upright)
            vs.Y += _taller / 2;                                            // between the names, whose room lost its top
        Outlined(_top, _names, vs * s, "VS", Px(Phone ? P(PhoneBattleStates.VsText) : Upright ? U(PhoneBattleStatesUpright.VsText) : VsSize),
            new Color(Ui.HoverLine, shown), new Color(VsInk, shown), Px(Phone || Upright ? 10 : 8), HorizontalAlignment.Center, centreY: true);

        var area = PlateArea;
        area.Size += new Vector2(0, _taller);
        var plate = new Rect2((area.Position - new Vector2(0, _rise)) * s, area.Size * s);
        var box = new StyleBoxFlat
        {
            BgColor = new Color(Ui.Row, 0.94f),
            BorderColor = Ui.HoverLine,
            ShadowColor = new Color(0, 0, 0, 0.55f),
            ShadowSize = Px(8),
            ShadowOffset = new Vector2(0, 5 * s),
            AntiAliasing = true,
        };
        box.SetCornerRadiusAll(Px(10));
        box.SetBorderWidthAll(Math.Max(1, Px(3)));
        _top.DrawStyleBox(box, plate);
        var bar = new StyleBoxFlat { BgColor = Ui.Row.Darkened(0.2f) };
        bar.SetCornerRadiusAll(Px(6));
        float inset = BarInset;
        float barHeight = (Phone ? P(PhoneBattleStates.BarHeight) : Upright ? U(PhoneBattleStatesUpright.BarHeight) : 50) + _taller;
        _top.DrawStyleBox(bar, new Rect2(plate.Position + new Vector2(inset, inset) * s, new Vector2(area.Size.X - 2 * inset, barHeight) * s));
        void Line(string text, float y, int size, Color colour) =>
            _top.DrawString(_plate, new Vector2(plate.Position.X, plate.Position.Y + y * s + Centred(_plate, size)), text,
                HorizontalAlignment.Center, plate.Size.X, size, colour);
        string? turnsText = o.Turns is { } turns ? turns == 1 ? "1 turn" : $"{turns} turns" : null;
        if (_title.Length == 1)                                             // "Duel", "Dungeon Battle"
            Line(_title[0], TitleMiddle, TitlePx(_title[0]), Ui.Text);
        else if (_title.Length == 2)                                        // the owner's B, the place in C's gold (2026-11-04)
        {
            Line(_title[0], TitleMiddle, TitlePx(_title[0]), Ui.Name);
            int kindPx = TitlePx(_title[1], KindSize);
            float ky = TitleMiddle + TitlePitch;
            Line(_title[1], ky, kindPx, Ui.Quiet);
            KindRules(plate, ky * s, _plate.GetStringSize(_title[1], HorizontalAlignment.Left, -1, kindPx).X);
        }
        float down = _taller;                                               // the limit and the turns under a second line
        if (Phone)
        {
            Line(o.Limit, P(PhoneBattleStates.LimitMiddle) + down, Px(P(PhoneBattleStates.LimitText)), Ui.Text);
            if (turnsText is not null)
                Line(turnsText, P(PhoneBattleStates.TurnsMiddle) + down, Px(P(PhoneBattleStates.TurnsText)), Ui.Quiet);
            return;
        }
        if (Upright)
        {
            Line(o.Limit, U(PhoneBattleStatesUpright.LimitMiddle) + down, Px(U(PhoneBattleStatesUpright.LimitText)), Ui.Text);
            if (turnsText is not null)
                Line(turnsText, U(PhoneBattleStatesUpright.TurnsMiddle) + down, Px(U(PhoneBattleStatesUpright.TurnsText)), Ui.Quiet);
            return;
        }
        Line(o.Limit, 84 + down, Px(22), Ui.Text);
        if (turnsText is not null)
            Line(turnsText, 115 + down, Px(20), Ui.Quiet);
    }

    /// <summary>
    /// The gold rules either side of the battle's kind (the owner's B): 40 long, 14 off its ends, a small diamond at their
    /// inner end, in the PC's layout pixels scaled with the title's type; <paramref name="y"/> and <paramref name="width"/>
    /// in window pixels from the plate's top.
    /// </summary>
    private void KindRules(Rect2 plate, float y, float width)
    {
        float k = TitleSize / 30 * _scale, cx = plate.GetCenter().X, cy = plate.Position.Y + y;
        for (int side = -1; side <= 1; side += 2)
        {
            float a = cx + side * (width / 2 + 14 * k);
            _top.DrawLine(new Vector2(a, cy), new Vector2(a + side * 40 * k, cy), Ui.Gold, Math.Max(1, 2 * k), true);
            _top.DrawColoredPolygon(
                [new(a, cy - 3 * k), new(a + side * 3 * k, cy), new(a, cy + 3 * k), new(a - side * 3 * k, cy)], Ui.Gold);
        }
    }

    /// <summary>A length of <paramref name="reference"/> reference pixels in layout pixels.</summary>
    private static float P(float reference) => PhoneBattle.Layout(reference);

    /// <summary>A length of <paramref name="reference"/> reference pixels in the upright board's layout pixels.</summary>
    private static float U(float reference) => PhoneBattleUpright.Layout(reference);

    private static Rect2 R(PhoneFrame.Box b) => new(b.X, b.Y, b.W, b.H);

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
    /// One half of the opening: its side's tint over the scene, clipped to its side of the seam (the half draws the mask,
    /// its children show only inside it), with its army's name and advantage, which part outwards as the opening leaves.
    /// </summary>
    private sealed partial class Half : Control
    {
        private readonly BattleOpeningLayer _layer;
        private readonly bool _left;
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
            _tint = new ColorRect { Color = left ? PlayerTint : EnemyTint, MouseFilter = MouseFilterEnum.Ignore };
            AddChild(_tint);
            _text = new Control { MouseFilter = MouseFilterEnum.Ignore };
            _text.Draw += DrawText;
            AddChild(_text);
        }

        public void Layout(float scale)
        {
            _scale = scale;
            Size = AreaSize * scale;
            (_tint.Position, _tint.Size) = (Vector2.Zero, Size);
            _text.Size = Size;
            _text.Position = new Vector2(_shift * scale, 0);
            QueueRedraw();
            _text.QueueRedraw();
        }

        /// <summary>The leave: the names <paramref name="shift"/> layout pixels sideways, the tint at <paramref name="tint"/> of its alpha.</summary>
        public void Leave(float shift, float tint)
        {
            _shift = shift;
            _text.Position = new Vector2(shift * _scale, 0);
            var colour = _left ? PlayerTint : EnemyTint;
            _tint.Color = colour with { A = colour.A * tint };
        }

        private float _shift;

        public void Show(string name, string advantage)
        {
            (_name, _advantage) = (name, advantage);
            _text.QueueRedraw();
        }

        /// <summary>The mask: this side of the seam (the scene's left part to the seam, or from it to the right).</summary>
        public override void _Draw()
        {
            float s = _scale;
            var (top, foot) = Seam;
            Vector2[] points = _left
                ? [new(0, 0), new(top * s, 0), new(foot * s, Height * s), new(0, Height * s)]
                : [new(top * s, 0), new(1280 * s, 0), new(1280 * s, Height * s), new(foot * s, Height * s)];
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
            if (Phone)
            {
                DrawPhoneText();
                return;
            }
            if (Upright)
            {
                DrawUprightText();
                return;
            }
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

        /// <summary>
        /// The phone's (LB3o1): the name centred in its room beside the plate (the fewest lines up to three, balanced, at the
        /// largest size that fits), its lines centred in the room's height; the advantage centred under it at the band's foot.
        /// </summary>
        private void DrawPhoneText()
        {
            var font = _layer._names;
            float s = _scale;
            var room = R(PhoneBattleStates.OpeningNames(_left, _advantage != ""));
            float size = P(PhoneBattleStates.NameText), least = P(PhoneBattleStates.NameMin);
            IReadOnlyList<string>? lines = null;
            for (; size >= least; size -= 2)
            {
                int px = (int)Math.Round(size * s);
                lines = BattleOpening.Wrap(_name, 3, line => font.GetStringSize(line, HorizontalAlignment.Left, -1, px).X / s, room.Size.X);
                if (lines is not null)
                    break;
            }
            lines ??= [_name];
            size = Math.Max(size, least);
            float pitch = size + P(PhoneBattleStates.NameGap);
            float cx = room.GetCenter().X, first = room.GetCenter().Y - (lines.Count - 1) * pitch / 2;
            for (int i = 0; i < lines.Count; i++)
                Outlined(_text, font, new Vector2(cx, first + i * pitch) * s, lines[i], (int)Math.Round(size * s), Colors.White, Ink,
                    (int)Math.Round(10 * s), HorizontalAlignment.Center, centreY: true);
            if (_advantage != "")
                Outlined(_text, font, new Vector2(cx, PhoneBattleStates.OpeningAdvantage) * s, _advantage,
                    (int)Math.Round(P(PhoneBattleStates.AdvantageText) * s), AdvantageColour, Ink, (int)Math.Round(6 * s),
                    HorizontalAlignment.Center, centreY: true);
        }

        /// <summary>
        /// Held upright (PB4o3): the name centred in its room at the plate's side (the fewest lines up to three, balanced, at
        /// the largest size that fits), its lines centred on the room's middle; the advantage centred under it.
        /// </summary>
        private void DrawUprightText()
        {
            var font = _layer._names;
            float s = _scale;
            var room = R(PhoneBattleStatesUpright.OpeningNames(_left));
            // Under a taller plate the room starts lower and keeps its foot over the advantage: its lines fit its height too.
            float taller = _layer._taller;
            room = new Rect2(room.Position + new Vector2(0, taller), room.Size - new Vector2(0, taller));
            float size = U(PhoneBattleStatesUpright.NameText), least = U(PhoneBattleStatesUpright.NameMin);
            IReadOnlyList<string>? lines = null;
            for (; size >= least; size -= 2)
            {
                int px = (int)Math.Round(size * s);
                lines = BattleOpening.Wrap(_name, 3, line => font.GetStringSize(line, HorizontalAlignment.Left, -1, px).X / s, room.Size.X);
                if (lines is not null && (taller <= 0 || lines.Count * (size + U(PhoneBattleStatesUpright.NameGap)) <= room.Size.Y))
                    break;
            }
            lines ??= [_name];
            size = Math.Max(size, least);
            float pitch = size + U(PhoneBattleStatesUpright.NameGap);
            float cx = room.GetCenter().X, first = room.GetCenter().Y - (lines.Count - 1) * pitch / 2;
            for (int i = 0; i < lines.Count; i++)
                Outlined(_text, font, new Vector2(cx, first + i * pitch) * s, lines[i], (int)Math.Round(size * s), Colors.White, Ink,
                    (int)Math.Round(10 * s), HorizontalAlignment.Center, centreY: true);
            if (_advantage != "")
                Outlined(_text, font, new Vector2(cx, PhoneBattleStatesUpright.OpeningAdvantage) * s, _advantage,
                    (int)Math.Round(U(PhoneBattleStatesUpright.AdvantageText) * s), AdvantageColour, Ink, (int)Math.Round(6 * s),
                    HorizontalAlignment.Center, centreY: true);
        }
    }
}
