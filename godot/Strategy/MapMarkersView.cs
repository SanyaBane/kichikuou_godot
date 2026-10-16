using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our map's markers (docs/ui/command-screen.md, "The map's markers"): a name plate under every town
/// and dungeon of the view during <c>COMMAND</c>'s map wait, over the original's map and flags. The
/// hover changes the colours only (a gold frame and glow, a lighter plate, a light spot under the
/// town); a left click on a plate is <see cref="Pressed"/>, a right click goes on to the screen (the
/// script's menu), a press anywhere else on the map stays the script's (its clicks and drags). A
/// hovered province's card (<see cref="MapMarker.Card"/>) shows right of its plate, over the plates.
/// </summary>
internal sealed partial class MapMarkersView : Control
{
    private readonly List<Plate> _plates = [];
    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private FontFile _font;
    private readonly FontVariation _badgeFont = new() { BaseFont = new FontFile { Data = Godot.FileAccess.GetFileAsBytes("res://fonts/Cinzel-VariableFont_wght.ttf") } };
    private float _pixels = 2;
    private (int X, int Y) _corner;
    private Plate? _shownHover;
    private bool _hideDimmed = MapNamesChoice.HideDimmed;
    private readonly ProvinceCard _card;

    public MapMarkersView(GameFont font)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;                            // the plates stay on the map, not over the menu bar
        Visible = false;
        _font = DeploymentTableView.LoadFont(font);
        _card = new ProvinceCard(this);
        AddChild(_card);
        _badgeFont.VariationOpentype = new Godot.Collections.Dictionary
        {
            [TextServerManager.Singleton.GetPrimaryInterface().NameToTag("wght")] = 700,
        };
    }

    /// <summary>A left click on a marker's plate.</summary>
    public event Action<MapMarker>? Pressed;

    /// <summary>The marker the pointer is over, or null.</summary>
    public MapMarker? Hovered => _plates.FirstOrDefault(p => p.Hover)?.Marker;

    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        foreach (var plate in _plates)
            plate.QueueRedraw();
        _card.QueueRedraw();
        QueueRedraw();
    }

    public void SetFont(GameFont font)
    {
        _font = DeploymentTableView.LoadFont(font);
        Relayout();
    }

    /// <summary>Option → Hide Dimmed Map Names (<see cref="MapNamesChoice"/>): the dimmed plates shown or not.</summary>
    public void SetHideDimmed(bool hide)
    {
        _hideDimmed = hide;
        foreach (var plate in _plates)
            PlacePlate(plate);
        UpdateCard();
        QueueRedraw();
    }

    /// <summary>The markers to show, as the state has them at the map wait's start.</summary>
    public void Load(IReadOnlyList<MapMarker> markers)
    {
        foreach (var plate in _plates)
            plate.QueueFree();
        _plates.Clear();
        _shownHover = null;
        foreach (var marker in markers)
        {
            var plate = new Plate(this, marker);
            _plates.Add(plate);
            AddChild(plate);
        }
        MoveChild(_card, -1);                           // over the plates
        Relayout();
    }

    /// <summary>
    /// Lays the layer over the map's picture (<paramref name="area"/>, in its parent's coordinates, at
    /// <paramref name="pixels"/> window pixels to a script pixel) with the view's corner on the map at
    /// <paramref name="corner"/>: each plate under its town, the ones whose town is out of view hidden.
    /// </summary>
    public void Place(Rect2 area, float pixels, (int X, int Y) corner)
    {
        Position = area.Position;
        Size = area.Size;
        bool rescaled = Math.Abs(pixels - _pixels) > 0.001f;
        _pixels = pixels;
        if (rescaled)
            Relayout();
        if (corner == _corner && !rescaled)
            return;
        _corner = corner;
        foreach (var plate in _plates)
            PlacePlate(plate);
        UpdateCard();
        QueueRedraw();
    }

    /// <summary>Unattended play: the shown plate of a province or dungeon, its marker and its middle on the screen; null when it is not shown.</summary>
    public (MapMarker Marker, Vector2 Centre)? PlateOf(MapHitKind kind, int id) =>
        _plates.FirstOrDefault(p => p.Visible && p.Marker.Kind == kind && p.Marker.Id == id) is { } plate
            ? (plate.Marker, plate.GetGlobalRect().GetCenter())
            : null;

    /// <summary>Unattended play's dumps: the marker shown as hovered without the pointer (null: none).</summary>
    public void ShowHover(MapMarker? marker)
    {
        _shownHover = marker is null ? null : _plates.FirstOrDefault(p => p.Marker.Kind == marker.Kind && p.Marker.Id == marker.Id);
        foreach (var plate in _plates)
            plate.QueueRedraw();
        UpdateCard();
        QueueRedraw();
    }

    private void Relayout()
    {
        foreach (var plate in _plates)
        {
            plate.Measure();
            PlacePlate(plate);
        }
        UpdateCard();
        QueueRedraw();
    }

    /// <summary>The card of the hovered (or shown hovered) province, right of its plate — left of it where the map ends — or none.</summary>
    private void UpdateCard()
    {
        var plate = _plates.FirstOrDefault(p => p.Visible && ShowsHover(p) && p.Marker.Card is not null);
        if (plate is null)
        {
            _card.Visible = false;
            return;
        }
        _card.SetLines(plate.Marker.Card!);
        float gap = 5 * _pixels;
        float x = plate.Position.X + plate.Size.X + gap;
        if (x + _card.Size.X > Size.X - 2)
            x = plate.Position.X - gap - _card.Size.X;
        float y = Math.Clamp(plate.Position.Y + (plate.Size.Y - _card.Size.Y) / 2, 2, Math.Max(2, Size.Y - _card.Size.Y - 2));
        _card.Position = new Vector2(x, y);
        _card.Visible = true;
    }

    private void PlacePlate(Plate plate)
    {
        var m = plate.Marker;
        int x = m.MapX - _corner.X, y = m.MapY - _corner.Y;
        plate.Visible = x is >= 0 and < 640 && y is >= 0 and < 400 && !(_hideDimmed && plate.Dimmed);
        // Under the town: 7 script pixels below its point, 20 under the big castles, 5 under a dungeon's mouth.
        float below = m.Kind == MapHitKind.Dungeon ? 5 : m.Big ? 20 : 7;
        plate.Position = new Vector2(x * _pixels - plate.Size.X / 2, (y + below) * _pixels);
    }

    private bool ShowsHover(Plate plate) => plate.Hover || plate == _shownHover;

    // The light spot under a hovered town: an ellipse 45 × 27 script pixels, fading out from its middle.
    public override void _Draw()
    {
        foreach (var plate in _plates)
        {
            if (!plate.Visible || !ShowsHover(plate) || !plate.Marker.Open)
                continue;
            var centre = new Vector2(plate.Marker.MapX - _corner.X, plate.Marker.MapY - _corner.Y) * _pixels;
            float radius = 22.5f * _pixels;
            DrawSetTransform(centre, 0, new Vector2(1, 0.6f));
            const int Rings = 12;
            for (int i = Rings; i >= 1; i--)
                DrawCircle(Vector2.Zero, radius * i / Rings, new Color(1f, 0.94f, 0.78f, 0.45f / Rings));
            DrawSetTransform(Vector2.Zero, 0, Vector2.One);
        }
    }

    /// <summary>
    /// A province's card (docs/ui/command-screen.md, "The province card"): its three lines, each a label
    /// ("Economy:") in the muted colour and the rest — the figure and what it does, or "??" muted too.
    /// </summary>
    private sealed partial class ProvinceCard : Control
    {
        private readonly MapMarkersView _layer;
        private IReadOnlyList<string> _lines = [];

        public ProvinceCard(MapMarkersView layer)
        {
            _layer = layer;
            MouseFilter = MouseFilterEnum.Ignore;
            Visible = false;
        }

        private float Px => _layer._pixels;

        private int FontSize => (int)Math.Round(7.5f * Px);

        private static (string Label, string Value) Split(string line)
        {
            int at = line.IndexOf(": ", StringComparison.Ordinal);
            return at < 0 ? ("", line) : (line[..(at + 1)], line[(at + 2)..]);
        }

        /// <summary>Takes the lines and sizes the card to them: 6 script pixels in from the sides, lines 11.5 apart.</summary>
        public void SetLines(IReadOnlyList<string> lines)
        {
            _lines = lines;
            var font = _layer._font;
            float space = font.GetStringSize(" ", fontSize: FontSize).X;
            float width = lines.Select(Split).Max(l => font.GetStringSize(l.Label, fontSize: FontSize).X + space
                + font.GetStringSize(l.Value, fontSize: FontSize).X);
            Size = new Vector2(MathF.Ceiling(width + 12 * Px), MathF.Ceiling(lines.Count * 11.5f * Px + 7 * Px));
            QueueRedraw();
        }

        public override void _Draw()
        {
            var c = _layer._colours;
            float px = Px;
            var box = new StyleBoxFlat { BgColor = new Color(c.Back, 0.94f), BorderColor = c.Border };
            box.SetBorderWidthAll(Math.Max(1, (int)Math.Round(px / 2)));
            box.SetCornerRadiusAll((int)Math.Round(2 * px));
            DrawStyleBox(box, new Rect2(Vector2.Zero, Size));
            var font = _layer._font;
            float space = font.GetStringSize(" ", fontSize: FontSize).X;
            float ascent = font.GetAscent(FontSize), descent = font.GetDescent(FontSize);
            for (int i = 0; i < _lines.Count; i++)
            {
                var (label, value) = Split(_lines[i]);
                float middle = 3.5f * px + (i + 0.5f) * 11.5f * px;
                float baseline = middle + (ascent - descent) / 2;
                DrawString(font, new Vector2(6 * px, baseline), label, fontSize: FontSize, modulate: c.Muted);
                float x = 6 * px + font.GetStringSize(label, fontSize: FontSize).X + space;
                DrawString(font, new Vector2(x, baseline), value, fontSize: FontSize, modulate: value == "??" ? c.Muted : c.Text);
            }
        }
    }

    /// <summary>One marker's plate: a province's name, or a dungeon's icon, name and search.</summary>
    private sealed partial class Plate : Control
    {
        private static readonly Color Cave = new(0.31f, 0.82f, 0.88f);
        private static readonly Color CaveBack = new(0.05f, 0.12f, 0.13f, 0.85f);
        private static readonly Color Locked = new(0.49f, 0.5f, 0.52f);
        private readonly MapMarkersView _layer;
        private bool _down;
        private float _textWidth, _badgeWidth;

        public Plate(MapMarkersView layer, MapMarker marker)
        {
            _layer = layer;
            Marker = marker;
            MouseFilter = MouseFilterEnum.Pass;
            MouseDefaultCursorShape = marker.Open ? CursorShape.PointingHand : CursorShape.Forbidden;
            MouseEntered += () => { Hover = true; Redraw(); _layer.UpdateCard(); };
            MouseExited += () => { Hover = false; _down = false; Redraw(); _layer.UpdateCard(); };
        }

        public MapMarker Marker { get; }

        public bool Hover { get; private set; }

        private bool IsDungeon => Marker.Kind == MapHitKind.Dungeon;

        /// <summary>Drawn dimmed: a dungeon not ours (grey), a province neither ours nor next to ours (70 %).</summary>
        public bool Dimmed => IsDungeon ? !Marker.Open : Marker.Relation == MapRelation.Other;

        private float Px => _layer._pixels;

        private int FontSize => (int)Math.Round(7.5f * Px);

        private int BadgeSize => (int)Math.Round(6 * Px);

        private void Redraw()
        {
            QueueRedraw();
            _layer.QueueRedraw();
        }

        /// <summary>The plate's size from its text: 16 script pixels high.</summary>
        public void Measure()
        {
            _textWidth = _layer._font.GetStringSize(Marker.Name, fontSize: FontSize).X;
            _badgeWidth = Marker.Progress is { } badge ? _layer._badgeFont.GetStringSize(badge, fontSize: BadgeSize).X + 6 * Px : 0;
            float width = IsDungeon
                ? 2 * Px + 12 * Px + 3.5f * Px + _textWidth + (_badgeWidth > 0 ? 3.5f * Px + _badgeWidth : 0) + 6 * Px
                : 6 * Px + _textWidth + 6 * Px;
            Size = new Vector2(MathF.Ceiling(width), MathF.Ceiling(16 * Px));
        }

        public override void _GuiInput(InputEvent e)
        {
            if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
                return;                                 // the right button goes on: the script's menu
            AcceptEvent();
            if (button.Pressed)
                _down = true;
            else if (_down)
            {
                _down = false;
                _layer.Pressed?.Invoke(Marker);
            }
        }

        public override void _Draw()
        {
            var c = _layer._colours;
            bool hover = _layer.ShowsHover(this);
            bool closed = IsDungeon && !Marker.Open;
            float px = Px;
            var box = new StyleBoxFlat
            {
                BgColor = IsDungeon ? CaveBack : new Color(c.Back, 0.82f),
                BorderColor = new Color(1, 1, 1, 0.12f),
                ShadowSize = 0,
            };
            box.SetBorderWidthAll(Math.Max(1, (int)Math.Round(px)));
            box.SetCornerRadiusAll((int)Math.Round((IsDungeon ? 8 : 3) * px));
            if (hover && !closed)
            {
                box.BgColor = c.ButtonHover;
                box.BorderColor = c.Accent;
                box.ShadowColor = new Color(c.Accent, 0.85f);
                box.ShadowSize = (int)Math.Round(5 * px);
            }
            else if (hover)
            {
                box.BorderColor = new Color(0.72f, 0.73f, 0.75f);
                box.BgColor = new Color(0.16f, 0.16f, 0.18f, 0.9f);
            }
            Modulate = new Color(1, 1, 1, closed ? (hover ? 0.85f : 0.6f) : !hover && Dimmed ? 0.7f : 1);
            DrawStyleBox(box, new Rect2(Vector2.Zero, Size));

            var text = closed ? new Color(0.79f, 0.79f, 0.8f) : hover ? Colors.White : c.Text;
            var font = _layer._font;
            float baseline = Size.Y / 2 + (font.GetAscent(FontSize) - font.GetDescent(FontSize)) / 2;
            float x = 6 * px;
            if (IsDungeon)
            {
                var tint = closed ? Locked : Cave;
                var disc = new Vector2(2 * px + 6 * px, Size.Y / 2);
                DrawCircle(disc, 6 * px, new Color(tint, 0.3f));
                DrawIcon(Marker.Dungeon, disc, 4 * px, tint);
                x = 2 * px + 12 * px + 3.5f * px;
            }
            DrawString(font, new Vector2(x, baseline), Marker.Name, fontSize: FontSize, modulate: text);
            if (Marker.Progress is { } badge)
            {
                float bx = x + _textWidth + 3.5f * px, bh = 9 * px;
                var back = new StyleBoxFlat { BgColor = badge == "✓" ? new Color("3c6e46") : new Color("8a5a1e") };
                back.SetCornerRadiusAll((int)Math.Round(2 * px));
                DrawStyleBox(back, new Rect2(bx, (Size.Y - bh) / 2, _badgeWidth, bh));
                var bf = _layer._badgeFont;
                float bb = Size.Y / 2 + (bf.GetAscent(BadgeSize) - bf.GetDescent(BadgeSize)) / 2;
                DrawString(bf, new Vector2(bx + 3 * px, bb), badge, fontSize: BadgeSize, modulate: new Color("f4e6c0"));
            }
        }

        // The dungeon's kind in its disc: a cave's mouth, a tower, a star (an event place), within a radius r.
        private void DrawIcon(DungeonKind kind, Vector2 at, float r, Color tint)
        {
            float width = Math.Max(1, r * 0.22f);
            switch (kind)
            {
                case DungeonKind.Pit:
                    var arch = new List<Vector2> { at + new Vector2(-r, r) };
                    for (int i = 0; i <= 12; i++)
                    {
                        float a = MathF.PI * i / 12;
                        arch.Add(at + new Vector2(-MathF.Cos(a) * r * 0.75f, -MathF.Sin(a) * r * 1.1f + r * 0.2f));
                    }
                    arch.Add(at + new Vector2(r, r));
                    DrawPolyline(arch.ToArray(), tint, width, antialiased: true);
                    DrawRect(new Rect2(at + new Vector2(-r * 0.25f, r * 0.15f), new Vector2(r * 0.5f, r * 0.85f)), new Color(0, 0, 0, 0.8f));
                    break;
                case DungeonKind.Tower:
                    Vector2[] tower =
                    [
                        new(-0.5f, 1), new(-0.5f, -0.4f), new(-0.75f, -0.4f), new(-0.75f, -1), new(-0.4f, -1), new(-0.4f, -0.75f),
                        new(-0.1f, -0.75f), new(-0.1f, -1), new(0.1f, -1), new(0.1f, -0.75f), new(0.4f, -0.75f), new(0.4f, -1),
                        new(0.75f, -1), new(0.75f, -0.4f), new(0.5f, -0.4f), new(0.5f, 1), new(-0.5f, 1),
                    ];
                    DrawPolyline(tower.Select(p => at + p * r).ToArray(), tint, width, antialiased: true);
                    break;
                default:
                    var star = new Vector2[11];
                    for (int i = 0; i < 10; i++)
                    {
                        float a = -MathF.PI / 2 + MathF.PI * i / 5, len = i % 2 == 0 ? r : r * 0.45f;
                        star[i] = at + new Vector2(MathF.Cos(a), MathF.Sin(a)) * len;
                    }
                    star[10] = star[0];
                    DrawColoredPolygon(star[..10], new Color(tint, 0.45f));
                    DrawPolyline(star, tint, width, antialiased: true);
                    break;
            }
        }
    }
}
