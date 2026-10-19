using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Treasure screen (docs/ui/pickers.md, the owner's mock-up S5): over the whole screen while <c>Sｱｲﾃﾑ</c> is
/// open — three columns on the wave backdrop: the items owned at the left (each a one-line tablet: its name and where it
/// is; Chaos, Nikkou and the items without effect greyed), the chosen character in the middle (name and type in the brush
/// face, a strip of figures, the battle sprite ×2 on a lit floor, the Equipment window under it with the held item's
/// picture and description), every character at the right as compact rows (name, type, the item held, a padlock on
/// Chaos and Nikkou), scrolled with the wheel, End under them. A stock item is dragged onto the Equipment window or a
/// character's row; the held item is dragged out of the window back to the items. What cannot be dragged is greyed
/// with its reason as a tooltip (the owner, 2026-10-19). Drawn by hand in the 1280×800 screen's pixels
/// (<see cref="Lacquer"/>). It decides nothing: the drops go to <see cref="GameScreen"/>, which clicks the picker
/// (<see cref="TreasureOrder"/>).
/// </summary>
internal sealed partial class TreasureView : Control
{
    // In the 1280×800 screen's pixels, after the mock-up.
    private static readonly Rect2 ItemsPanel = new(16, 16, 284, 768);
    private static readonly Rect2 StagePanel = new(308, 16, 556, 768);
    private static readonly Rect2 Window = new(324, 376, 524, 392);
    private static readonly Rect2 Picture = new(338, 424, 496, 310);
    private static readonly Rect2 CharsPanel = new(872, 16, 392, 712);
    private static readonly Rect2 EndArea = new(1134, 740, 130, 44);
    private const float ItemX = 30, ItemW = 256, ItemH = 28, ItemStep = 32, ListY = 74, ItemsBottom = 770;
    private const float CharX = 886, CharW = 364, CharH = 36, CharStep = 40, CharsBottom = 716;
    private const float Floor = 346, SpriteTop = 132;
    private static readonly int ItemRows = (int)((ItemsBottom - ListY + ItemStep - ItemH) / ItemStep);
    private static readonly int CharRows = (int)((CharsBottom - ListY + CharStep - CharH) / CharStep);

    private enum Drag { None, Stock, Held }

    private readonly FacePictures _faces;
    private readonly Layer _backdrop, _face, _sprite, _top;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700);
    private readonly Dictionary<int, ImageTexture?> _sprites = [];
    private FontFile _gothic;
    private Vector2 _origin;
    private float _k = 1;
    private GameState? _state;
    private IReadOnlyList<TreasureItem> _items = [];
    private IReadOnlyList<TreasureCharacter> _characters = [];
    private TreasureDetails? _details;
    private string _key = "";
    private int _chosen, _itemScroll, _charScroll;
    private Vector2 _pointer = new(-1, -1);
    private bool _pressedEnd;
    private Drag _drag;
    private int _dragItem;

    /// <summary>A stock item dropped on a character: equip it.</summary>
    public event Action<int, int>? EquipPressed;

    /// <summary>The chosen character's item dragged out of the Equipment window to the items: take it off.</summary>
    public event Action<int>? UnequipPressed;

    /// <summary>End pressed, or a right press on the screen.</summary>
    public event Action? EndPressed;

    public TreasureView(GameFont font, FacePictures faces)
    {
        _faces = faces;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        _backdrop = new Layer(c =>
        {
            c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
            Lacquer.DrawWaveBackdrop(c, _k);
        }) { ClipContents = true };
        _face = new Layer(DrawScreen);
        _sprite = new Layer(DrawSprite) { TextureFilter = TextureFilterEnum.Nearest };      // whole pixels, as the original's
        _top = new Layer(DrawTop);
        foreach (var layer in new[] { _backdrop, _face, _sprite, _top })
            AddChild(layer);
    }

    /// <summary>The character chosen (its unit), 0 with nobody listed.</summary>
    public int Chosen => _chosen;

    /// <summary>Whether the screen takes presses (not while an order's clicks are on their way).</summary>
    public bool Busy { get; set; }

    /// <summary>Option → Fonts.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        Redraw();
    }

    /// <summary>Covers the screen whose script pixel (0, 0) is at <paramref name="origin"/>, <paramref name="pixelsPerScript"/> screen pixels to a script pixel.</summary>
    public void Place(Vector2 origin, float pixelsPerScript)
    {
        Position = Vector2.Zero;
        Size = GetParentAreaSize();
        float k = pixelsPerScript / 2;
        var area = new Vector2(1280, 800) * k;
        if (origin == _origin && Math.Abs(k - _k) < 0.0001f && _backdrop.Size == area)
            return;
        _origin = origin;
        _k = k;
        foreach (var layer in new[] { _backdrop, _face, _sprite, _top })
        {
            layer.Position = origin;
            layer.Size = area;
            layer.QueueRedraw();
        }
    }

    /// <summary>The items, the characters and the chosen one from <paramref name="state"/>; the choice kept while it is listed.</summary>
    public void Refresh(GameState state)
    {
        _state = state;
        var characters = TreasureRoster.Characters(state);
        _chosen = TreasureRoster.FirstChoice(characters, _chosen);
        var items = TreasureRoster.Items(state, _chosen);
        var details = _chosen == 0 ? null : TreasureRoster.Details(state, _chosen);
        string key = $"{string.Join(",", items)}|{string.Join(",", characters)}|{details}|{Busy}";
        if (key == _key)
            return;
        _key = key;
        _items = items;
        _characters = characters;
        _details = details;
        _itemScroll = Math.Clamp(_itemScroll, 0, Math.Max(0, items.Count - ItemRows));
        _charScroll = Math.Clamp(_charScroll, 0, Math.Max(0, characters.Count - CharRows));
        Redraw();
    }

    /// <summary>Chooses <paramref name="unit"/> as a click on its row does (unattended play goes through here).</summary>
    public void Choose(int unit)
    {
        _chosen = unit;
        _key = "";
        if (_state is not null)
            Refresh(_state);
    }

    /// <summary>
    /// Shows a stock item dragged to <paramref name="at"/> (the screen's pixels; unattended play's dumps), or none
    /// (<paramref name="item"/> 0).
    /// </summary>
    public void ShowDrag(int item, Vector2 at)
    {
        _drag = item == 0 ? Drag.None : Drag.Stock;
        _dragItem = item;
        _pointer = at;
        Redraw();
    }

    /// <summary>The pointer put at <paramref name="at"/> (the screen's pixels) as a hover puts it (unattended play's dumps).</summary>
    public void ShowPointer(Vector2 at)
    {
        _pointer = at;
        Redraw();
    }

    /// <summary>The centre of <paramref name="item"/>'s tablet in the screen's pixels, or null when it is not in view.</summary>
    public Vector2? ItemPoint(int item)
    {
        for (int row = 0; row < ItemRows && _itemScroll + row < _items.Count; row++)
            if (_items[_itemScroll + row].Number == item)
                return ItemRect(row).GetCenter();
        return null;
    }

    /// <summary>Equip as a drop on the chosen one's window does (unattended play).</summary>
    public void Equip(int item) => EquipPressed?.Invoke(_chosen, item);

    /// <summary>Unequip as a drag out of the window does (unattended play).</summary>
    public void Unequip() => UnequipPressed?.Invoke(_chosen);

    /// <summary>Where the Equipment window's picture is, in the screen's pixels (a drop's point for unattended play).</summary>
    public static Vector2 WindowCentre => Picture.GetCenter();

    private void Redraw()
    {
        _face.QueueRedraw();
        _sprite.QueueRedraw();
        _top.QueueRedraw();
    }

    // ---------------------------------------------------------------- input

    private static Rect2 ItemRect(int row) => new(ItemX, ListY + row * ItemStep, ItemW, ItemH);

    private static Rect2 CharRect(int row) => new(CharX, ListY + row * CharStep, CharW, CharH);

    /// <summary>The item whose tablet is under <paramref name="at"/>, or null.</summary>
    private TreasureItem? ItemAt(Vector2 at)
    {
        for (int row = 0; row < ItemRows && _itemScroll + row < _items.Count; row++)
            if (ItemRect(row).HasPoint(at))
                return _items[_itemScroll + row];
        return null;
    }

    /// <summary>The character whose row is under <paramref name="at"/>, or null.</summary>
    private TreasureCharacter? CharAt(Vector2 at)
    {
        for (int row = 0; row < CharRows && _charScroll + row < _characters.Count; row++)
            if (CharRect(row).HasPoint(at))
                return _characters[_charScroll + row];
        return null;
    }

    /// <summary>The unit a stock item would go to if dropped at <paramref name="at"/>: the chosen one's window, or a character's row; 0 for none.</summary>
    private int DropTarget(Vector2 at) => Window.HasPoint(at) ? _chosen : CharAt(at)?.Unit ?? 0;

    private string? DropRefusal(int unit) =>
        _state is null || unit == 0 ? "" : TreasureRoster.DropRefusal(_state, unit, _dragItem);

    public override void _GuiInput(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion:
                _pointer = (motion.Position - _origin) / _k;
                _top.QueueRedraw();
                _face.QueueRedraw();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel:
                Scroll((wheel.Position - _origin) / _k, wheel.ButtonIndex == MouseButton.WheelUp ? -1 : 1);
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: true }:
                if (_drag != Drag.None)
                    ShowDrag(0, _pointer);                  // a drag dropped
                else if (!Busy)
                    EndPressed?.Invoke();
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } click:
                var at = (click.Position - _origin) / _k;
                _pointer = at;
                if (click.Pressed)
                    Press(at);
                else
                    Release(at);
                Redraw();
                AcceptEvent();
                break;
        }
    }

    private void Scroll(Vector2 at, int rows)
    {
        if (ItemsPanel.HasPoint(at))
            _itemScroll = Math.Clamp(_itemScroll + rows, 0, Math.Max(0, _items.Count - ItemRows));
        else if (CharsPanel.HasPoint(at))
            _charScroll = Math.Clamp(_charScroll + rows, 0, Math.Max(0, _characters.Count - CharRows));
        Redraw();
    }

    private void Press(Vector2 at)
    {
        _pressedEnd = false;
        if (Busy)
            return;
        if (EndArea.HasPoint(at))
            _pressedEnd = true;
        else if (ItemAt(at) is { Refusal: null } item)
        {
            _drag = Drag.Stock;
            _dragItem = item.Number;
        }
        else if (Picture.HasPoint(at) && _state is not null && _chosen != 0 && TreasureRoster.CanTakeOff(_state, _chosen))
        {
            _drag = Drag.Held;
            _dragItem = _details!.Item;
        }
        else if (CharAt(at) is { } character)
            Choose(character.Unit);
    }

    private void Release(Vector2 at)
    {
        var drag = _drag;
        _drag = Drag.None;
        if (Busy)
            return;
        if (_pressedEnd && EndArea.HasPoint(at))
            EndPressed?.Invoke();
        _pressedEnd = false;
        if (drag == Drag.Stock && DropTarget(at) is var unit and not 0 && DropRefusal(unit) is null)
        {
            Choose(unit);
            EquipPressed?.Invoke(unit, _dragItem);
        }
        else if (drag == Drag.Held && ItemsPanel.HasPoint(at))
            UnequipPressed?.Invoke(_chosen);
    }

    public override void _Notification(int what)
    {
        if (what == NotificationMouseExit && _drag == Drag.None)
        {
            _pointer = new Vector2(-1, -1);
            Redraw();
        }
    }

    // ---------------------------------------------------------------- drawing

    private float Line(float width) => Lacquer.Line(width, _k);

    private void DrawScreen(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        DrawItems(c);
        DrawStage(c);
        DrawWindow(c);
        DrawCharacters(c);
        DrawEnd(c);
    }

    /// <summary>The items owned: "Items" and the count, a tablet each, greyed where it cannot be dragged.</summary>
    private void DrawItems(Control c)
    {
        Lacquer.DrawPanel(c, ItemsPanel, _k);
        DrawHeading(c, ItemsPanel, "Items", _items.Count.ToString());
        if (_items.Count == 0)
            c.DrawString(_gothic, new Vector2(ItemX + 4, ListY + 20), "No items", fontSize: 14, modulate: Lacquer.OffText);
        for (int row = 0; row < ItemRows && _itemScroll + row < _items.Count; row++)
            DrawTablet(c, ItemRect(row), _items[_itemScroll + row], dragged: _drag != Drag.None && _items[_itemScroll + row].Number == _dragItem);
        DrawScrollBar(c, ItemsPanel, _itemScroll, ItemRows, _items.Count);
    }

    /// <summary>An item's tablet: a diamond, its name, where it is; greyed when it cannot be dragged (a padlock on Chaos and Nikkou).</summary>
    private void DrawTablet(Control c, Rect2 r, TreasureItem item, bool dragged = false, bool ghost = false)
    {
        bool hover = !ghost && _drag == Drag.None && !Busy && r.HasPoint(_pointer) && item.Refusal is null;
        bool off = item.Refusal is not null && !ghost;
        if (off)
        {
            c.DrawRect(r, Lacquer.OffButton);
            c.DrawRect(r, Lacquer.OffLine, false, Line(1));
        }
        else
            Lacquer.DrawRowBox(c, r, hover || ghost, _k);
        if (dragged)
            c.DrawRect(r, new Color(14 / 255f, 10 / 255f, 8 / 255f, 0.63f));
        float mid = r.Position.Y + r.Size.Y / 2;
        Lacquer.DrawDiamond(c, new Vector2(r.Position.X + 14, mid), 4.5f, item.Greyed ? Lacquer.OffLine : Lacquer.Gold);
        Color ink = item.Greyed ? Lacquer.OffText : off ? Lacquer.Quiet : Lacquer.Text;
        bool locked = TreasureRoster.Fixed(item.Number);
        float right = r.End.X - (locked ? 28 : 10);
        float ww = _gothic.GetStringSize(item.Where, fontSize: 11).X;
        c.DrawString(_gothic, new Vector2(right - ww, mid + 4), item.Where, fontSize: 11, modulate: item.Greyed ? Lacquer.OffText : Lacquer.Quiet);
        c.DrawString(_gothic, new Vector2(r.Position.X + 26, mid + 5), item.Name, HorizontalAlignment.Left, right - ww - 8 - r.Position.X - 26,
            fontSize: 14, modulate: ink);
        if (locked)
            DrawLock(c, new Vector2(r.End.X - 15, mid + 2), Lacquer.OffText);
    }

    /// <summary>The chosen character: name and type, the strip of figures, the lit floor (the sprite is its own layer).</summary>
    private void DrawStage(Control c)
    {
        Lacquer.DrawPanel(c, StagePanel, _k, Color.Color8(18, 13, 10, 250));
        if (_details is not { } d)
            return;
        float x = StagePanel.Position.X + 24;
        var band = new Rect2(x, 28, 500, 40);
        c.DrawString(_brush, new Vector2(x, Lacquer.Baseline(band, _brush, 30)), d.Name, fontSize: 30, modulate: Lacquer.Name);
        float nw = _brush.GetStringSize(d.Name, fontSize: 30).X;
        c.DrawString(_cinzel, new Vector2(x + nw + 14, Lacquer.Baseline(band, _cinzel, 15) + 3), d.Type, fontSize: 15, modulate: Lacquer.Subtitle);
        (string, string)[] figures =
        [
            ("Troops", $"{d.Troops}/{d.MaxTroops}"), ("Cost", $"{d.Cost * 100:N0}K"), ("Lv", d.Level.ToString()),
            ("HP", $"{d.Hp}/{d.MaxHp}"), ("Atk", d.Atk.ToString()), ("Def", d.Def.ToString()), ("Act", d.Attacks.ToString()),
            ("Mag", d.Magic.ToString()),
        ];
        float fx = x;
        foreach (var (label, value) in figures)
        {
            c.DrawString(_cinzel, new Vector2(fx, 86), label, fontSize: 11, modulate: Lacquer.Label);
            c.DrawString(_gothic, new Vector2(fx, 108), value, fontSize: 16, modulate: Lacquer.Value);
            fx += Math.Max(54, _gothic.GetStringSize(value, fontSize: 16).X + 18);
        }
        Lacquer.DrawDivider(c, StagePanel.Position.X + 16, StagePanel.End.X - 16, 124, _k);
        float cx = Window.GetCenter().X;
        for (int i = 14; i >= 1; i--)
            DrawEllipse(c, new Vector2(cx, Floor - 80), 160f * i / 14, 120f * i / 14, Lacquer.Name with { A = 0.028f });
        for (int i = 14; i >= 1; i--)
            DrawEllipse(c, new Vector2(cx, Floor), 110f * i / 14, 18f * i / 14, new Color(0, 0, 0, 0.12f));
        c.DrawLine(new Vector2(StagePanel.Position.X + 46, Floor + 1), new Vector2(StagePanel.End.X - 46, Floor + 1),
            Lacquer.DimGold with { A = 0.47f }, Line(1));
    }

    /// <summary>The battle sprite (<c>Unit_CgAlone</c>, cut to its pixels) ×2 on the floor — ×1 if it would not fit.</summary>
    private void DrawSprite(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (_details is not { } d || SpriteOf(d.SpriteCg) is not { } sprite)
            return;
        var size = sprite.GetSize();
        float scale = size.Y * 2 <= Floor - SpriteTop + 6 ? 2 : 1;
        var drawn = size * scale;
        c.DrawTextureRect(sprite, new Rect2(Window.GetCenter().X - drawn.X / 2, Floor + 6 - drawn.Y, drawn.X, drawn.Y), false);
    }

    private ImageTexture? SpriteOf(int cg)
    {
        if (_sprites.TryGetValue(cg, out var texture))
            return texture;
        texture = null;
        if (_faces.Picture(cg) is { } image)
        {
            var used = image.GetUsedRect();
            if (used.Size.X > 0 && used.Size.Y > 0)
                texture = ImageTexture.CreateFromImage(image.GetRegion(used));
        }
        _sprites[cg] = texture;
        return texture;
    }

    /// <summary>
    /// The Equipment window: the held item's picture (the original's CG with its description) and how to take it off, a
    /// padlock on Chaos and Nikkou, or a dashed field to drop on; during a drag of a stock item over it, the dragged item's
    /// picture lit in gold with "Equip here" (or why it takes no drop).
    /// </summary>
    private void DrawWindow(Control c)
    {
        Lacquer.DrawPanel(c, Window, _k, Color.Color8(30, 20, 14, 250));
        var title = new Rect2(Window.Position.X + 18, Window.Position.Y + 8, Window.Size.X - 36, 34);
        c.DrawString(_brush, new Vector2(title.Position.X, Lacquer.Baseline(title, _brush, 18)), "Equipment", fontSize: 18, modulate: Lacquer.Name);
        Lacquer.DrawDivider(c, Window.Position.X + 12, Window.End.X - 12, Window.Position.Y + 42, _k);
        if (_details is not { } d)
            return;
        float hintY = Picture.End.Y + 14;
        bool dropping = _drag == Drag.Stock && Window.HasPoint(_pointer);
        if (dropping && DropRefusal(_chosen) is null)
        {
            DrawPicture(c, _dragItem);
            c.DrawRect(Picture, Lacquer.Name with { A = 0.16f });
            for (int i = 8; i >= 1; i--)
                c.DrawRect(Picture.Grow(i), Lacquer.Name with { A = 0.03f }, false, Line(2));
            c.DrawRect(Picture, Lacquer.HoverLine, false, Line(3));
            var plate = new Rect2(Picture.GetCenter().X - 70, Picture.End.Y - 16, 140, 30);
            c.DrawRect(plate, Color.Color8(60, 44, 24));
            c.DrawRect(plate, Lacquer.Gold, false, Line(1));
            Centred(c, _gothic, "Equip here", 15, Lacquer.Name, plate.GetCenter().X, Lacquer.Baseline(plate, _gothic, 15));
            string back = d.Item != 0 ? $"{d.ItemName} goes back to stock" : "From the stock";
            DrawRight(c, title, TreasureRoster.ItemName(_state!, _dragItem), Lacquer.Name);
            c.DrawString(_gothic, new Vector2(Window.Position.X + 18, hintY + 4), back, fontSize: 13, modulate: Lacquer.Quiet);
            return;
        }
        if (d.Item != 0)
        {
            bool out_ = _drag == Drag.Held;
            DrawPicture(c, d.Item);
            if (out_)
                c.DrawRect(Picture, new Color(14 / 255f, 10 / 255f, 8 / 255f, 0.63f));
            DrawRight(c, title, d.ItemName, Lacquer.Name, d.Locked ? 22 : 0);
            if (d.Locked)
                DrawLock(c, new Vector2(title.End.X - 6, title.GetCenter().Y + 3), Lacquer.Quiet);
            string hint = d.Locked ? $"Never leaves {d.Name}"
                : out_ ? "Drop it on the items to take it off"
                : "Drag it out to the items to take it off";
            c.DrawString(_gothic, new Vector2(Window.Position.X + 18, hintY + 4), hint, fontSize: 13, modulate: Lacquer.Quiet);
        }
        else
        {
            c.DrawRect(Picture, new Color(34 / 255f, 26 / 255f, 20 / 255f, 0.63f));
            DrawDashed(c, Picture, Lacquer.DimGold);
            Centred(c, _cinzel, "No item", 18, Lacquer.DimGold, Picture.GetCenter().X, Picture.GetCenter().Y - 4);
            Centred(c, _gothic, "Drag an item here", 13, Lacquer.Quiet, Picture.GetCenter().X, Picture.GetCenter().Y + 22);
        }
        if (dropping && DropRefusal(_chosen) is { } refusal)
        {
            c.DrawRect(Picture, new Color(0.4f, 0.08f, 0.05f, 0.25f));
            c.DrawString(_gothic, new Vector2(Window.Position.X + 18, hintY + 4), refusal, HorizontalAlignment.Left, Window.Size.X - 36,
                fontSize: 13, modulate: Lacquer.Minus);
        }
    }

    private void DrawPicture(Control c, int item)
    {
        if (_faces.PicturePanel(750 + item, 819, 692) is { } picture)
            c.DrawTextureRect(picture, Picture, false);
        else
            c.DrawRect(Picture, Colors.Black);
    }

    /// <summary>The characters: compact rows (name, type, the item held, a padlock on Chaos and Nikkou), the chosen one lit with a gold arrowhead.</summary>
    private void DrawCharacters(Control c)
    {
        Lacquer.DrawPanel(c, CharsPanel, _k);
        DrawHeading(c, CharsPanel, "Characters", _characters.Count.ToString());
        int target = _drag == Drag.Stock ? CharAt(_pointer)?.Unit ?? 0 : 0;
        for (int row = 0; row < CharRows && _charScroll + row < _characters.Count; row++)
        {
            var ch = _characters[_charScroll + row];
            var r = CharRect(row);
            bool chosen = ch.Unit == _chosen;
            bool hover = !Busy && r.HasPoint(_pointer) && _drag == Drag.None;
            bool drop = ch.Unit == target && DropRefusal(ch.Unit) is null;
            bool refused = ch.Unit == target && !drop;
            c.DrawRect(r, chosen ? Color.Color8(60, 30, 22) : hover || drop ? Lacquer.RowHover : Lacquer.Row);
            c.DrawRect(r, drop || hover ? Lacquer.HoverLine : chosen ? Lacquer.Gold : refused ? Lacquer.Minus : Lacquer.DimGold, false, Line(drop || chosen ? 2 : 1));
            float mid = r.Position.Y + r.Size.Y / 2;
            if (chosen)
                c.DrawColoredPolygon([new(r.Position.X - 11, mid - 8), new(r.Position.X - 2, mid), new(r.Position.X - 11, mid + 8)], Lacquer.Gold);
            c.DrawString(_gothic, new Vector2(r.Position.X + 10, mid + 6), ch.Name, HorizontalAlignment.Left, 118, fontSize: 16,
                modulate: chosen ? Lacquer.Name : Lacquer.Text);
            c.DrawString(_gothic, new Vector2(r.Position.X + 134, mid + 5), ch.Type, HorizontalAlignment.Left, 96, fontSize: 12, modulate: Lacquer.Quiet);
            float right = r.End.X - (ch.Locked ? 26 : 10);
            if (drop)
            {
                string equip = "Equip here";
                float ew = _gothic.GetStringSize(equip, fontSize: 13).X;
                c.DrawString(_gothic, new Vector2(right - ew, mid + 5), equip, fontSize: 13, modulate: Lacquer.Name);
            }
            else if (ch.Item != 0)
            {
                float iw = Math.Min(_gothic.GetStringSize(ch.ItemName, fontSize: 13).X, 124);
                Lacquer.DrawDiamond(c, new Vector2(right - iw - 10, mid), 3.5f, Lacquer.Gold);
                c.DrawString(_gothic, new Vector2(right - iw, mid + 5), ch.ItemName, HorizontalAlignment.Left, 124, fontSize: 13, modulate: Lacquer.Text);
            }
            else
            {
                float dw = _gothic.GetStringSize("—", fontSize: 13).X;
                c.DrawString(_gothic, new Vector2(right - dw, mid + 5), "—", fontSize: 13, modulate: Lacquer.DimGold);
            }
            if (ch.Locked)
                DrawLock(c, new Vector2(r.End.X - 13, mid + 2), Lacquer.Quiet);
        }
        DrawScrollBar(c, CharsPanel, _charScroll, CharRows, _characters.Count);
    }

    private void DrawEnd(Control c)
    {
        bool hover = !Busy && _drag == Drag.None && EndArea.HasPoint(_pointer);
        c.DrawRect(EndArea, hover ? Lacquer.RowHover : Lacquer.Row);
        c.DrawRect(EndArea, hover ? Lacquer.HoverLine : Lacquer.DimGold, false, Line(hover ? 2 : 1));
        Centred(c, _cinzel, "End", 17, hover ? Lacquer.Name : Lacquer.Text, EndArea.GetCenter().X, Lacquer.Baseline(EndArea, _cinzel, 17));
    }

    /// <summary>Over everything: the dragged item's tablet under the pointer, or a greyed thing's reason as a tooltip.</summary>
    private void DrawTop(Control c)
    {
        c.DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (_drag != Drag.None && _items.FirstOrDefault(i => i.Number == _dragItem) is { } dragged)
        {
            var ghost = new Rect2(_pointer.X - 110, _pointer.Y - 14, 220, 28);
            for (int i = 10; i >= 1; i--)
                c.DrawRect(ghost.Grow(i), new Color(0, 0, 0, 0.04f));
            DrawTablet(c, ghost, dragged with { Refusal = null }, ghost: true);
            return;
        }
        if (Busy || _state is null)
            return;
        string? tip = ItemAt(_pointer)?.Refusal;
        if (tip is null && Picture.HasPoint(_pointer) && _details is { Locked: true } d)
            tip = $"{d.ItemName} never leaves {d.Name}";
        if (tip is null)
            return;
        float w = _gothic.GetStringSize(tip, fontSize: 13).X + 20;
        var box = new Rect2(Math.Min(_pointer.X + 14, 1272 - w), _pointer.Y + 18, w, 26);
        c.DrawRect(box.Grow(2), new Color(0, 0, 0, 0.35f));
        c.DrawRect(box, Lacquer.Panel);
        c.DrawRect(box, Lacquer.Gold, false, Line(1));
        c.DrawString(_gothic, new Vector2(box.Position.X + 10, Lacquer.Baseline(box, _gothic, 13)), tip, fontSize: 13, modulate: Lacquer.Value);
    }

    private void DrawHeading(Control c, Rect2 panel, string title, string count)
    {
        var band = new Rect2(panel.Position.X + 20, panel.Position.Y + 10, panel.Size.X - 40, 34);
        c.DrawString(_brush, new Vector2(band.Position.X, Lacquer.Baseline(band, _brush, 20)), title, fontSize: 20, modulate: Lacquer.Name);
        DrawRight(c, band, count, Lacquer.Subtitle, font: _cinzel, size: 14);
        Lacquer.DrawDivider(c, panel.Position.X + 14, panel.End.X - 14, panel.Position.Y + 46, _k);
    }

    /// <summary>A thin gold bar at the panel's right edge when its list outgrows it.</summary>
    private void DrawScrollBar(Control c, Rect2 panel, int scroll, int rows, int count)
    {
        if (count <= rows)
            return;
        float top = ListY, height = panel.End.Y - 14 - top, x = panel.End.X - 9;
        c.DrawRect(new Rect2(x, top, 3, height), Lacquer.OffTag);
        c.DrawRect(new Rect2(x, top + height * scroll / count, 3, height * rows / count), Lacquer.Gold);
    }

    private void DrawRight(Control c, Rect2 band, string text, Color colour, float inset = 0, Font? font = null, int size = 16)
    {
        font ??= _gothic;
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(band.End.X - inset - w, Lacquer.Baseline(band, font, size)), text, fontSize: size, modulate: colour);
    }

    /// <summary>A small padlock: a shackle over a body.</summary>
    private void DrawLock(Control c, Vector2 at, Color colour)
    {
        c.DrawArc(at + new Vector2(0, -4), 3.5f, Mathf.Pi, Mathf.Tau, 12, colour, Line(1.6f));
        c.DrawRect(new Rect2(at.X - 5, at.Y - 4, 10, 8), colour);
    }

    private void DrawDashed(Control c, Rect2 r, Color colour)
    {
        for (float x = r.Position.X; x < r.End.X; x += 14)
        {
            c.DrawLine(new Vector2(x, r.Position.Y), new Vector2(Math.Min(x + 8, r.End.X), r.Position.Y), colour, Line(1));
            c.DrawLine(new Vector2(x, r.End.Y - 1), new Vector2(Math.Min(x + 8, r.End.X), r.End.Y - 1), colour, Line(1));
        }
        for (float y = r.Position.Y; y < r.End.Y; y += 14)
        {
            c.DrawLine(new Vector2(r.Position.X, y), new Vector2(r.Position.X, Math.Min(y + 8, r.End.Y)), colour, Line(1));
            c.DrawLine(new Vector2(r.End.X - 1, y), new Vector2(r.End.X - 1, Math.Min(y + 8, r.End.Y)), colour, Line(1));
        }
    }

    private static void DrawEllipse(Control c, Vector2 centre, float rx, float ry, Color colour)
    {
        const int Segments = 40;
        var points = new Vector2[Segments];
        for (int i = 0; i < Segments; i++)
        {
            float a = Mathf.Tau * i / Segments;
            points[i] = centre + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
        }
        c.DrawColoredPolygon(points, colour);
    }

    private static void Centred(Control c, Font font, string text, int size, Color colour, float centre, float baseline)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(centre - w / 2, baseline), text, fontSize: size, modulate: colour);
    }

    /// <summary>A layer of the screen drawn by its view.</summary>
    private sealed partial class Layer : Control
    {
        private readonly Action<Control> _paint;

        public Layer(Action<Control> paint)
        {
            _paint = paint;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Draw() => _paint(this);
    }
}
