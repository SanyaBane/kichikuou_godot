using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Our own command input on the battle screen (Option → Interface → New; docs/ui/battle-screen.md, step 4,
/// "Next — our own command input"): <c>FIGT_SEL</c> answered without the original's prompt and popups.
/// The attacks are banners on the enemy card under the pointer (<see cref="BattleCommands.Banners"/>), the
/// only way to attack; a right click on a card opens its context menu at the pointer
/// (<see cref="BattleCommands.ContextMenu"/>). Every choice is one the core's menu offers — the rules stay
/// the core's. Points are layout pixels of the 1280×800 screen (<see cref="BattleBoardView"/>).
/// </summary>
internal sealed partial class BattleCommandLayer : Control
{
    // The banners (as Sengoku Rance's): 200×40, 5 apart, one column 22 out of the card's left edge; their places are
    // fixed from the top whatever their number (owner, 2026-10-13), three of them centred on the card's height.
    private static readonly Vector2 BannerSize = new(200, 40);
    private const float BannerGap = 5, BannerOut = 22, BannerSlant = 12, BannerPoint = 18, BannerText = 20;
    private const int BannerPlaces = 3;
    private const float BannerAlpha = 0.8f;
    private static readonly Color BannerTop = Color8(255, 228, 90), BannerBottom = Color8(226, 128, 20);
    private static readonly Color HotTop = Color8(255, 170, 90), HotBottom = Color8(196, 70, 30);
    private static readonly Color Glow = new(1f, 0.72f, 0.42f);
    private static readonly Color TextOutline = new(0.22f, 0.1f, 0.02f, 0.95f);

    // The context menu: rows of the cards' text size, a column for the current order's mark.
    private const float RowHeight = 30, MenuPad = 8, MarkWidth = 18, MenuMinWidth = 130, MenuText = 17;

    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private Func<BattlePlace, Rect2> _cardArea = _ => default;
    private float _scale = 1;
    private BattleCommands? _commands;
    private GameState? _state;
    private Func<ScriptRef, string> _text = _ => "";
    private BattlePlace? _hovered;                                  // the card under the pointer (or whose banners it is on)
    private IReadOnlyList<(string Text, FigtSelChoice Choice)> _banners = [];
    private int _hotBanner = -1;
    private (BattlePlace Place, Rect2 Box, IReadOnlyList<(string Text, FigtSelChoice Choice, bool Current)> Items)? _menu;
    private int _hotItem = -1;
    private int _pressedItem = -1;                                  // the item a left press began on (chosen at its release)

    public BattleCommandLayer()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 10;
    }

    private static Color Color8(int r, int g, int b) => Color.Color8((byte)r, (byte)g, (byte)b);

    /// <summary>Whether the player is choosing through this layer.</summary>
    public bool Active => _commands is not null;

    /// <summary>The answer once the player has chosen.</summary>
    public FigtSelChoice? Result { get; private set; }

    /// <summary>The card under the pointer, or whose banners it is on (the scene shows that enemy).</summary>
    public BattlePlace? Hovered => _hovered;

    /// <summary>
    /// The card lit as under the pointer: the one whose menu is open, else the hovered card — an own card, or
    /// an enemy's with banners (one no attack reaches stays grey).
    /// </summary>
    public BattlePlace? Lit => _menu?.Place ?? (_hovered is { Side: BattleSide.Enemy } && _banners.Count == 0 ? null : _hovered);

    /// <summary>Whether a context menu is open.</summary>
    public bool MenuOpen => _menu is not null;

    /// <summary>The open menu's items (unattended play's checks).</summary>
    public IReadOnlyList<FigtSelChoice> MenuItems => _menu is { } m ? [.. m.Items.Select(i => i.Choice)] : [];

    /// <summary>The banners shown (unattended play's checks).</summary>
    public IReadOnlyList<FigtSelChoice> Banners => [.. _banners.Select(b => b.Choice)];

    /// <summary>The centre of the shown banner that gives <paramref name="choice"/> (layout pixels), or null.</summary>
    public Vector2? BannerCentre(FigtSelChoice choice)
    {
        int i = _banners.Select(b => b.Choice).ToList().IndexOf(choice);
        return _hovered is { } target && i >= 0 ? BannerRect(target, i).GetCenter() : null;
    }

    /// <summary>The centre of the open menu's item <paramref name="item"/> (layout pixels), or null.</summary>
    public Vector2? ItemCentre(int item) => _menu is { } m && item >= 0 && item < m.Items.Count
        ? new Vector2(m.Box.GetCenter().X, m.Box.Position.Y + MenuPad + (item + 0.5f) * RowHeight)
        : null;

    /// <summary>Starts a choice from the core's menu; <paramref name="text"/> reads a label.</summary>
    public void Begin(BattleCommands commands, GameState state, Func<ScriptRef, string> text)
    {
        (_commands, _state, _text) = (commands, state, text);
        Result = null;
        (_hovered, _banners, _hotBanner, _menu, _hotItem) = (null, [], -1, null, -1);
        QueueRedraw();
    }

    /// <summary>Ends the choice (answered, or the interface changed): nothing is shown.</summary>
    public void End()
    {
        _commands = null;
        _state = null;
        (_hovered, _banners, _hotBanner, _menu, _hotItem) = (null, [], -1, null, -1);
        QueueRedraw();
    }

    /// <summary>The layout: <paramref name="cardArea"/> gives a card's rectangle, <paramref name="scale"/> screen pixels to a layout pixel.</summary>
    public void Layout(Func<BattlePlace, Rect2> cardArea, float scale)
    {
        _cardArea = cardArea;
        _scale = scale;
        QueueRedraw();
    }

    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        QueueRedraw();
    }

    // ------------------------------------------------------------------ input

    /// <summary>The pointer moved to layout point <paramref name="p"/>.</summary>
    public void Move(Vector2 p)
    {
        if (_commands is null)
            return;
        if (_menu is { } menu)
        {
            SetHotItem(ItemAt(menu, p));
            return;
        }
        var place = _hovered is { Side: BattleSide.Enemy } target && BannerAt(target, p) >= 0 ? target : CardAt(p);
        if (place != _hovered)
        {
            _hovered = place;
            _banners = place is { } t
                ? [.. _commands.Banners(t).Select(b => (_text(b.Label).Trim(), b.Choice))]
                : [];
            QueueRedraw();
        }
        int hot = _hovered is { } h ? BannerAt(h, p) : -1;
        if (hot != _hotBanner)
        {
            _hotBanner = hot;
            QueueRedraw();
        }
    }

    /// <summary>
    /// A button pressed at <paramref name="p"/>: a left press on a banner chooses it (as the original's click
    /// on a target, at the press), one on a menu item chooses it at its release there (as the original's
    /// popups), one outside the open menu closes it, anywhere else it does nothing; a right press on a card
    /// opens its menu there (closing an open one), elsewhere it closes the menu.
    /// </summary>
    public void Press(bool right, Vector2 p)
    {
        if (_commands is null)
            return;
        _pressedItem = -1;
        if (_menu is { } menu)
        {
            int item = ItemAt(menu, p);
            if (!right && item >= 0)
            {
                _pressedItem = item;
                return;
            }
            if (!right && menu.Box.HasPoint(p))
                return;                                             // the menu's frame: nothing
            CloseMenu();
            if (!right)
            {
                Move(p);
                return;
            }
        }
        if (!right)
        {
            if (_hovered is { } target && BannerAt(target, p) is var b and >= 0)
                Result = _banners[b].Choice;
            return;
        }
        var place = _hovered is { Side: BattleSide.Enemy } t && BannerAt(t, p) >= 0 ? t : CardAt(p);
        if (place is { } card)
            OpenMenu(card, p);
        Move(p);
    }

    /// <summary>A button released at <paramref name="p"/>: a left press that began on a menu item chooses it if the pointer is still on it.</summary>
    public void Release(bool right, Vector2 p)
    {
        if (_commands is null || right)
            return;
        if (_menu is { } menu && _pressedItem >= 0 && ItemAt(menu, p) == _pressedItem)
            Result = menu.Items[_pressedItem].Choice;
        _pressedItem = -1;
    }

    /// <summary>Esc: closes the open menu (true), else does nothing (false).</summary>
    public bool Escape()
    {
        if (_menu is null)
            return false;
        CloseMenu();
        return true;
    }

    private void OpenMenu(BattlePlace place, Vector2 at)
    {
        var order = place.Side == BattleSide.Player && _state is { } s ? BattleBoard.Card(s, place)?.Order : null;
        var items = _commands!.ContextMenu(place, order).Select(c => (_text(c.Label).Trim(), c.Choice, c.Current)).ToList();
        if (items.Count == 0)
            return;
        float width = MenuMinWidth;
        if (GetThemeDefaultFont() is { } font)
        {
            foreach (var (text, _, _) in items)
                width = Math.Max(width, font.GetStringSize(text, HorizontalAlignment.Left, -1, (int)Math.Round(MenuText * _scale)).X / _scale + MarkWidth + MenuPad * 2);
        }
        var size = new Vector2(width, items.Count * RowHeight + MenuPad * 2);
        var box = new Rect2(new Vector2(Math.Clamp(at.X, 0, 1280 - size.X), Math.Clamp(at.Y, 0, 800 - size.Y)), size);
        _menu = (place, box, items);
        _hotItem = ItemAt(_menu.Value, at);
        (_hovered, _banners, _hotBanner) = (null, [], -1);
        QueueRedraw();
    }

    private void CloseMenu()
    {
        _menu = null;
        _hotItem = _pressedItem = -1;
        QueueRedraw();
    }

    private void SetHotItem(int item)
    {
        if (item == _hotItem)
            return;
        _hotItem = item;
        QueueRedraw();
    }

    // ------------------------------------------------------------------ geometry (layout pixels)

    /// <summary>The card under <paramref name="p"/> that holds a unit, or null.</summary>
    private BattlePlace? CardAt(Vector2 p)
    {
        if (_state is not { } s)
            return null;
        foreach (var place in SlotSnapshot.Places)
        {
            if (_cardArea(place).HasPoint(p) && BattleBoard.Record(s, place) != 0)
                return place;
        }
        return null;
    }

    /// <summary>Banner <paramref name="i"/> on <paramref name="target"/>'s card: its fixed place, the first at the top.</summary>
    private Rect2 BannerRect(BattlePlace target, int i)
    {
        var card = _cardArea(target);
        float column = BannerPlaces * BannerSize.Y + (BannerPlaces - 1) * BannerGap;
        return new Rect2(card.Position.X - BannerOut, card.Position.Y + (card.Size.Y - column) / 2 + i * (BannerSize.Y + BannerGap), BannerSize);
    }

    /// <summary>The banner of <paramref name="target"/> under <paramref name="p"/> (its pennant shape), or -1.</summary>
    private int BannerAt(BattlePlace target, Vector2 p)
    {
        if (target != _hovered)
            return -1;
        for (int i = 0; i < _banners.Count; i++)
        {
            if (Geometry2D.IsPointInPolygon(p, Pennant(BannerRect(target, i), 1)))
                return i;
        }
        return -1;
    }

    private static int ItemAt((BattlePlace Place, Rect2 Box, IReadOnlyList<(string, FigtSelChoice, bool)> Items) menu, Vector2 p)
    {
        var box = menu.Box;
        if (!box.HasPoint(p))
            return -1;
        int row = (int)Math.Floor((p.Y - box.Position.Y - MenuPad) / RowHeight);
        return row >= 0 && row < menu.Items.Count ? row : -1;
    }

    /// <summary>A pennant in <paramref name="r"/>: the left end cut aslant, the right end a point (towards the enemy's face), scaled by <paramref name="scale"/>.</summary>
    private static Vector2[] Pennant(Rect2 r, float scale)
    {
        var (x, y, w, h) = (r.Position.X, r.Position.Y, r.Size.X, r.Size.Y);
        return [.. new Vector2[]
        {
            new(x + BannerSlant, y), new(x + w - BannerPoint, y), new(x + w, y + h / 2), new(x + w - BannerPoint, y + h), new(x, y + h),
        }.Select(v => v * scale)];
    }

    // ------------------------------------------------------------------ drawing

    public override void _Draw()
    {
        if (_commands is null)
            return;
        var font = GetThemeDefaultFont();
        if (_hovered is { } target && _menu is null)
        {
            for (int i = 0; i < _banners.Count; i++)
                DrawBanner(BannerRect(target, i), _banners[i].Text, i == _hotBanner, font);
        }
        if (_menu is { } menu)
            DrawMenu(menu, font);
    }

    private void DrawBanner(Rect2 r, string text, bool hot, Font? font)
    {
        float s = _scale;
        var shape = Pennant(r, s);
        var (top, bottom) = hot ? (HotTop, HotBottom) : (BannerTop, BannerBottom);
        var outline = shape.Append(shape[0]).ToArray();
        // a soft shadow below and right; the hovered one glows
        DrawColoredPolygon([.. shape.Select(v => v + new Vector2(4, 5) * s)], new Color(0, 0, 0, 0.18f));
        DrawColoredPolygon([.. shape.Select(v => v + new Vector2(2, 3) * s)], new Color(0, 0, 0, 0.25f));
        if (hot)
        {
            DrawPolyline(outline, Glow with { A = 0.18f }, 14 * s, antialiased: true);
            DrawPolyline(outline, Glow with { A = 0.35f }, 7 * s, antialiased: true);
        }
        // a vertical gradient: each corner coloured by its height
        float y0 = r.Position.Y * s, h = r.Size.Y * s;
        var colours = shape.Select(v => top.Lerp(bottom, (v.Y - y0) / h) with { A = BannerAlpha }).ToArray();
        DrawPolygon(shape, colours);
        DrawPolyline(outline, Colors.White, Math.Max(1, 2 * s), antialiased: true);
        if (font is null)
            return;
        int size = (int)Math.Round(BannerText * s);
        float left = (r.Position.X + BannerSlant) * s, width = (r.Size.X - BannerSlant - BannerPoint) * s;
        float baseline = y0 + (h + font.GetAscent(size) - font.GetDescent(size)) / 2;
        var at = new Vector2(left, baseline);
        DrawStringOutline(font, at, text, HorizontalAlignment.Center, width, size, Math.Max(2, (int)Math.Round(5 * s)), TextOutline);
        DrawString(font, at, text, HorizontalAlignment.Center, width, size, Colors.White);
    }

    private void DrawMenu((BattlePlace Place, Rect2 Box, IReadOnlyList<(string Text, FigtSelChoice Choice, bool Current)> Items) menu, Font? font)
    {
        float s = _scale;
        var box = new Rect2(menu.Box.Position * s, menu.Box.Size * s);
        DrawRect(new Rect2(box.Position + new Vector2(4, 5) * s, box.Size), new Color(0, 0, 0, 0.3f));
        DrawRect(box, _colours.Card);
        DrawRect(box, _colours.ButtonBorder, filled: false, width: Math.Max(1, 2 * s));
        int size = (int)Math.Round(MenuText * s);
        for (int i = 0; i < menu.Items.Count; i++)
        {
            var row = new Rect2(box.Position + new Vector2(MenuPad - 4, MenuPad + i * RowHeight) * s, new Vector2(menu.Box.Size.X - (MenuPad - 4) * 2, RowHeight) * s);
            if (i == _hotItem)
                DrawRect(row, _colours.ButtonHover);
            var (text, _, current) = menu.Items[i];
            if (current)
                DrawCircle(row.Position + new Vector2((4 + MarkWidth / 2) * s, row.Size.Y / 2), 4 * s, _colours.Accent);
            if (font is null)
                continue;
            float baseline = row.Position.Y + (row.Size.Y + font.GetAscent(size) - font.GetDescent(size)) / 2;
            DrawString(font, new Vector2(row.Position.X + (4 + MarkWidth) * s, baseline), text, HorizontalAlignment.Left, -1, size,
                current ? _colours.Accent : _colours.Text);
        }
    }
}
