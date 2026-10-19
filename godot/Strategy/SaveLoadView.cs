using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our Save/Load screen over <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s slot lists (docs/ui/save-load.md, §5: the owner's layout C, 2026-10-20):
/// one lacquer panel, the ten slots as a narrow list on the left — a seal, the short date, the kind, the time written;
/// Auto in a gold-framed panel of its own on top, the nine manual saves under it in the order of the Slot | Date switch
/// (§7, the owner's E: a second click on the active button turns its direction; <see cref="SaveOrderChoice"/> keeps it) —
/// and the chosen slot's page on the right: its map large (All Data's miniature, <see cref="SaveSlot.Tiles"/> and
/// <see cref="SaveSlot.Marks"/>), its figures, and a red button that answers the script's menu with the slot's item
/// ("Load", "Save over Slot n", "Save"; one line each, the owner's 2026-10-20) — the second click, so no question. A
/// row's click only chooses it; an empty slot takes none in Load, Auto none in Save. From <c>COMMAND</c> the Load | Save tabs switch lists
/// (<see cref="LoadSaveShortcut.Switch"/>: the list left with 0, the script's Load / Save / Don't menu answered with the
/// other). A right click or Esc answers 0. Laid out in the 1280×800 screen's pixels, scaled with the picture. After its
/// answer the screen stays drawn, taking no clicks, until the script shows the next list or the save screen closes.
/// </summary>
internal sealed partial class SaveLoadView : Control
{
    // hits: 1-10 the slots' rows, 101/102 the tabs, the button, 300/301 the order's Slot and Date
    private const int ButtonHit = 200, TabHit = 100, SortHit = 300;

    private static readonly Rect2 PanelRect = new(30, 24, 1220, 752);
    private static readonly Rect2 MapRect = new(406, 118, 820, 341);
    private static readonly Rect2 ButtonRect = new(896, 694, 330, 58);
    private static readonly Rect2 AutoRect = new(52, 96, 330, 60);
    private const float RowX = 52, RowW = 330, RowH = 57, RowPitch = 62, ManualY = 212, PageX = 406, PageY = 84;
    private const float AutoHeadY = 86, StripY = 168, StripHeadY = 190, SortY = 177;

    private static readonly Color ChosenFill = Color.Color8(60, 30, 22);
    private static readonly Color AutoGold = Color.Color8(138, 104, 34);
    private static readonly Color TurnStartTag = Color.Color8(86, 70, 50);
    private static readonly Color MidTurnTag = Color.Color8(36, 92, 96);       // the command panel's Trade colour
    private static readonly Color TagText = Color.Color8(246, 232, 206);

    private readonly FacePictures _pictures;
    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(700), _cinzelLight = Lacquer.Cinzel(600);
    private readonly Dictionary<string, ImageTexture?> _maps = [];
    private FontFile _gothic;
    private SaveList? _list;
    private IReadOnlyList<SaveSlot?> _slots = [];
    private SlotOrder _order = SaveOrderChoice.Order;
    private int[] _manual = [];
    private SaveSlot? _now;
    private bool _tabs;
    private string _back = "";
    private int? _chosen;
    private float _k = 1;
    private int _hovered, _pressed;
    private TaskCompletionSource<(int Choice, SaveList? Switch)>? _answer;

    public SaveLoadView(GameFont font, FacePictures pictures)
    {
        _pictures = pictures;
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>The list shown, or null.</summary>
    public SaveList? List => _list;

    /// <summary>The slot whose page is shown, or null.</summary>
    public int? Chosen => _chosen;

    /// <summary>The manual saves' order.</summary>
    public SlotOrder Order => _order;

    /// <summary>The manual saves (slots 2-10) top to bottom.</summary>
    public IReadOnlyList<int> ManualSlots => _manual;

    /// <summary>
    /// Shows <paramref name="list"/> over <paramref name="slots"/> (slots 1-10, null for an empty one); <paramref name="now"/>
    /// is the game a save stores. The slot chosen before is kept when the same list opens again (after a save the script
    /// lists the slots again), else <see cref="SaveSlots.FirstChoice"/>. <see cref="ChooseAsync"/> waits for the answer.
    /// </summary>
    public void Open(SaveList list, IReadOnlyList<SaveSlot?> slots, SaveSlot? now, bool tabs, string back)
    {
        bool again = Visible && _list == list && _chosen is { } kept && SaveSlots.Takes(list, kept, slots[kept - 1]);
        _list = list;
        _slots = slots;
        _manual = [.. SaveSlots.Order(slots, _order)];
        _now = now;
        _tabs = tabs;
        _back = back;
        if (!again)
            _chosen = SaveSlots.FirstChoice(list, slots);
        _hovered = _pressed = 0;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>The script's answer: the chosen slot's item, or 0 to leave the list — with the other list for a tab.</summary>
    public Task<(int Choice, SaveList? Switch)> ChooseAsync()
    {
        _answer = new TaskCompletionSource<(int, SaveList?)>();
        return _answer.Task;
    }

    /// <summary>The menu was answered: the screen stays drawn and takes no clicks until the next list or <see cref="Close"/>.</summary>
    public void Release()
    {
        _answer?.TrySetCanceled();
        _answer = null;
        _hovered = _pressed = 0;
        MouseDefaultCursorShape = CursorShape.Arrow;
        QueueRedraw();
    }

    /// <summary>The save screen closed: hidden, the choice forgotten.</summary>
    public void Close()
    {
        Release();
        _list = null;
        _chosen = null;
        Visible = false;
    }

    /// <summary>Lays the layer over the screen's picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        float k = area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        QueueRedraw();
    }

    // ---------------------------------------------------------------- unattended play's points

    /// <summary>A slot's row centre in global coordinates (unattended play's clicks).</summary>
    public Vector2 RowCentre(int slot) => Global(RowRect(slot).GetCenter());

    /// <summary>A tab's centre in global coordinates.</summary>
    public Vector2 TabCentre(SaveList list) => Global(TabRect(list).GetCenter());

    /// <summary>The red button's centre in global coordinates.</summary>
    public Vector2 ButtonCentre => Global(ButtonRect.GetCenter());

    /// <summary>An order button's centre in global coordinates.</summary>
    public Vector2 SortCentre(SlotSortKey key) => Global(SortRect(key).GetCenter());

    private Vector2 Global(Vector2 layout) => GetGlobalTransformWithCanvas() * (layout * _k);

    // ---------------------------------------------------------------- input

    /// <summary>Auto's panel, or a manual save's row at its place in the order.</summary>
    private Rect2 RowRect(int slot)
    {
        if (slot == 1)
            return AutoRect;
        int place = Array.IndexOf(_manual, slot);
        return place < 0 ? new Rect2() : new Rect2(RowX, ManualY + place * RowPitch, RowW, RowH);
    }

    private static Rect2 TabRect(SaveList list) => new(PanelRect.End.X - 220 + (list == SaveList.Save ? 96 : 0), PanelRect.Position.Y + 11, 90, 30);

    /// <summary>Slot | Date, right-aligned with the list over the manual saves.</summary>
    private static Rect2 SortRect(SlotSortKey key) => key == SlotSortKey.Slot
        ? new Rect2(RowX + RowW - 144, SortY, 66, 26)
        : new Rect2(RowX + RowW - 74, SortY, 74, 26);

    private bool Takes(int slot) => _list is { } list && slot >= 1 && slot <= _slots.Count && SaveSlots.Takes(list, slot, _slots[slot - 1]);

    private bool ButtonShown => _chosen is { } slot && Takes(slot);

    private int HitAt(Vector2 window)
    {
        var at = window / _k;
        if (ButtonShown && ButtonRect.HasPoint(at))
            return ButtonHit;
        if (_tabs)
            foreach (var list in (SaveList[])[SaveList.Load, SaveList.Save])
                if (list != _list && TabRect(list).HasPoint(at))
                    return TabHit + (int)list;
        foreach (var key in (SlotSortKey[])[SlotSortKey.Slot, SlotSortKey.Date])
            if (SortRect(key).HasPoint(at))
                return SortHit + (int)key;
        for (int slot = 1; slot <= _slots.Count; slot++)
            if (Takes(slot) && RowRect(slot).HasPoint(at))
                return slot;
        return 0;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_answer is null)
        {
            AcceptEvent();                                              // drawn between two lists: nothing passes through
            return;
        }
        switch (e)
        {
            case InputEventMouseMotion motion:
                int hit = HitAt(motion.Position);
                if (hit != _hovered)
                {
                    _hovered = hit;
                    MouseDefaultCursorShape = hit != 0 ? CursorShape.PointingHand : CursorShape.Arrow;
                    QueueRedraw();
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                Answer((0, null));
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = HitAt(button.Position);
                if (button.Pressed)
                    _pressed = at;
                else if (at != 0 && at == _pressed)
                    Act(at);
                else
                    _pressed = 0;
                QueueRedraw();
                break;
        }
        AcceptEvent();
    }

    public override void _UnhandledKeyInput(InputEvent e)
    {
        if (_answer is not null && e is InputEventKey { Keycode: Key.Escape, Pressed: true })
        {
            Answer((0, null));
            GetViewport().SetInputAsHandled();
        }
    }

    private void Act(int hit)
    {
        _pressed = 0;
        if (hit == ButtonHit && _list is { } list && _chosen is { } slot)
            Answer((SaveSlots.ItemOf(list, slot), null));
        else if (hit >= SortHit)
        {
            _order = _order.Clicked((SlotSortKey)(hit - SortHit));
            SaveOrderChoice.Order = _order;
            _manual = [.. SaveSlots.Order(_slots, _order)];
        }
        else if (hit > TabHit)
            Answer((0, (SaveList)(hit - TabHit)));
        else
            _chosen = hit;
    }

    private void Answer((int, SaveList?) answer)
    {
        var task = _answer;
        _answer = null;
        task?.TrySetResult(answer);
    }

    // ---------------------------------------------------------------- drawing

    private float Line(float width) => Lacquer.Line(width, _k);

    /// <summary>The baseline that puts text of <paramref name="size"/> with its middle at <paramref name="y"/>.</summary>
    private static float Middle(Font font, int size, float y) => y + (font.GetAscent(size) - font.GetDescent(size)) / 2;

    private void Text(Font font, int size, float x, float middle, string text, Color colour) =>
        DrawString(font, new Vector2(x, Middle(font, size, middle)), text, fontSize: size, modulate: colour);

    private void TextRight(Font font, int size, float right, float middle, string text, Color colour) =>
        Text(font, size, right - font.GetStringSize(text, fontSize: size).X, middle, text, colour);

    private void TextCentred(Font font, int size, float centre, float middle, string text, Color colour) =>
        Text(font, size, centre - font.GetStringSize(text, fontSize: size).X / 2, middle, text, colour);

    public override void _Draw()
    {
        if (_list is not { } list)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        DrawRect(new Rect2(0, 0, 1280, 800), new Color(0, 0, 0, 120 / 255f));
        Lacquer.DrawPanel(this, PanelRect, _k);
        bool save = list == SaveList.Save;
        string title = save ? "Save" : "Load";
        float x = PanelRect.Position.X + 22, y = PanelRect.Position.Y + 25;
        Text(_brush, 24, x, y, title, Lacquer.Name);
        Text(_cinzelLight, 13, x + _brush.GetStringSize(title, fontSize: 24).X + 14, y + 2,
            save ? "the game as it is now" : "choose a game to go on with", Lacquer.Subtitle);
        Lacquer.DrawDivider(this, PanelRect.Position.X + 16, PanelRect.End.X - 16, PanelRect.Position.Y + 44, _k);
        if (_tabs)
        {
            DrawTab(SaveList.Load);
            DrawTab(SaveList.Save);
        }
        DrawList();
        DrawPage(list);
    }

    /// <summary>Auto's panel under its heading, then the manual saves under theirs with the order's switch, in that order.</summary>
    private void DrawList()
    {
        float right = RowX + RowW;
        Text(_cinzel, 11, RowX, AutoHeadY, "AUTO-SAVE", Lacquer.Subtitle);
        TextRight(_gothic, 11, right, AutoHeadY, "the game's own, each turn", Lacquer.Label);
        if (_slots.Count > 0)
            DrawRow(1);
        DrawLine(new Vector2(RowX, StripY), new Vector2(right, StripY), Lacquer.DimGold, Line(1));
        Text(_cinzel, 11, RowX, StripHeadY, "MANUAL SAVES", Lacquer.Subtitle);
        DrawSort(SlotSortKey.Slot);
        DrawSort(SlotSortKey.Date);
        foreach (int slot in _manual)
            DrawRow(slot);
    }

    /// <summary>An order button as a tab; the active one with its direction, a triangle down for descending.</summary>
    private void DrawSort(SlotSortKey key)
    {
        var rect = SortRect(key);
        bool active = key == _order.Key, hover = _hovered == SortHit + (int)key;
        if (active)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Lacquer.Vermilion, false, Line(2));
        }
        else
            Lacquer.DrawRowBox(this, rect, hover, _k);
        var colour = active ? Lacquer.Name : hover ? Lacquer.Text : Lacquer.Quiet;
        string text = key.ToString();
        float cx = rect.GetCenter().X, cy = rect.GetCenter().Y;
        if (!active)
        {
            TextCentred(_cinzel, 13, cx, cy, text, colour);
            return;
        }
        float tx = cx - 6;
        TextCentred(_cinzel, 13, tx, cy, text, colour);
        float ax = tx + _cinzel.GetStringSize(text, fontSize: 13).X / 2 + 9;
        Vector2[] arrow = _order.Descending
            ? [new(ax - 4, cy - 2), new(ax + 4, cy - 2), new(ax, cy + 3)]
            : [new(ax - 4, cy + 3), new(ax + 4, cy + 3), new(ax, cy - 2)];
        DrawColoredPolygon(arrow, colour);
    }

    private void DrawTab(SaveList tab)
    {
        var rect = TabRect(tab);
        bool active = tab == _list, hover = _hovered == TabHit + (int)tab;
        if (active)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Lacquer.Vermilion, false, Line(2));
        }
        else
            Lacquer.DrawRowBox(this, rect, hover, _k);
        TextCentred(_cinzel, 15, rect.GetCenter().X, rect.GetCenter().Y, tab.ToString(), active ? Lacquer.Name : hover ? Lacquer.Text : Lacquer.Quiet);
    }

    private void DrawRow(int slot)
    {
        var rect = RowRect(slot);
        var held = _slots[slot - 1];
        bool off = _list == SaveList.Save && slot == 1;                // Auto: the game's to write
        if (off)
        {
            DrawRect(rect, Lacquer.OffButton);
            DrawRect(rect, Lacquer.OffLine, false, Line(1));
        }
        else if (slot == _chosen)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Lacquer.Vermilion, false, Line(2));
        }
        else
        {
            Lacquer.DrawRowBox(this, rect, slot == _hovered, _k);
            if (slot == 1)
                DrawRect(rect, AutoGold, false, Line(2));                // Auto's panel: a gold frame
        }
        float x0 = rect.Position.X, y0 = rect.Position.Y, top = rect.Size.Y >= 60 ? 17 : 15;
        DrawSeal(new Rect2(x0 + 10, rect.GetCenter().Y - 12, 24, 24), slot == 1 ? "A" : (slot - 1).ToString(),
            held is null || off ? Lacquer.OffLine : slot == 1 ? AutoGold : Lacquer.Vermilion);
        if (held is null)
        {
            Text(_cinzelLight, 15, x0 + 46, rect.GetCenter().Y, "Empty", Lacquer.OffText);
            return;
        }
        Text(_cinzel, 15, x0 + 46, y0 + top, held.ShortDate, off ? Lacquer.OffText : Lacquer.Name);
        DrawKindTag(x0 + 46, y0 + top + 13, held, off);
        bool key = _order.Key == SlotSortKey.Date && slot != 1 && !off;   // sorted by date: the time is the bright line
        TextRight(_gothic, key ? 13 : 12, rect.End.X - 10, y0 + top + 23, held.WrittenShort,
            off ? Lacquer.OffText : key ? Lacquer.Value : Lacquer.Label);
    }

    private void DrawSeal(Rect2 rect, string text, Color fill)
    {
        DrawRect(rect, fill);
        DrawRect(rect.Grow(-2), Lacquer.SealText with { A = 0.45f }, false, Line(1));
        TextCentred(_cinzel, 14, rect.GetCenter().X, rect.GetCenter().Y + 1, text, Lacquer.SealText);
    }

    /// <summary>The slot's kind as a tag: Auto gold, a turn's start quiet, a save of the moment teal. Returns its width.</summary>
    private float DrawKindTag(float x, float y, SaveSlot slot, bool off = false)
    {
        string text = slot.KindText;
        float w = _gothic.GetStringSize(text, fontSize: 12).X + 14;
        var colour = slot.Kind switch
        {
            SlotKind.Auto => AutoGold,
            SlotKind.TurnStart => TurnStartTag,
            _ => MidTurnTag,
        };
        DrawRect(new Rect2(x, y, w, 20), off ? Lacquer.OffButton : colour);
        Text(_gothic, 12, x + 7, y + 10, text, off ? Lacquer.OffText : TagText);
        return w;
    }

    private void DrawPage(SaveList list)
    {
        bool save = list == SaveList.Save;
        if (_chosen is not { } chosen)
        {
            TextCentred(_cinzelLight, 18, MapRect.GetCenter().X, MapRect.GetCenter().Y, save ? "No slot to save into." : "No saved games.", Lacquer.Quiet);
            Text(_gothic, 13, PageX, ButtonRect.Position.Y + 40, $"Right click: {_back}", Lacquer.Quiet);
            return;
        }
        var held = _slots[chosen - 1];
        var shown = held ?? _now;                                      // an empty slot to save into: the game it will hold
        string label = SaveSlots.LabelOf(chosen);
        if (held is not null)
        {
            string title = $"{label} · {held.DateText}";
            Text(_cinzel, 22, PageX, PageY + 14, title, Lacquer.Name);
            DrawKindTag(PageX + _cinzel.GetStringSize(title, fontSize: 22).X + 16, PageY + 4, held);
        }
        else
        {
            string title = $"{label} · Empty";
            Text(_cinzel, 22, PageX, PageY + 14, title, Lacquer.Name);
            Text(_cinzelLight, 13, PageX + _cinzel.GetStringSize(title, fontSize: 22).X + 16, PageY + 15, "the game below will be saved here", Lacquer.Subtitle);
        }
        if (shown is not null)
        {
            if (MapOf(shown) is { } map)
                DrawTextureRect(map, MapRect, false);
            else
                DrawRect(MapRect, Colors.Black);
            DrawRect(MapRect, Lacquer.DimGold, false, Line(1));
            float gy = MapRect.End.Y + 16;
            (string, string)[] stats =
            [
                ("Stage", shown.StageText), ("Budget", shown.BudgetText), ("Realm", shown.RealmText),
                ("Units", shown.Units.ToString()), ("Troops", shown.Troops.ToString()), ("Written", shown.WrittenText),
            ];
            for (int i = 0; i < stats.Length; i++)
            {
                float sx = PageX + i % 3 * 280, sy = gy + i / 3 * 50;
                DrawString(_gothic, new Vector2(sx, sy + _gothic.GetAscent(13)), stats[i].Item1, fontSize: 13, modulate: Lacquer.Label);
                DrawString(_gothic, new Vector2(sx, sy + 18 + _gothic.GetAscent(17)), stats[i].Item2, fontSize: 17, modulate: Lacquer.Value);
            }
        }
        float by = ButtonRect.Position.Y;
        if (save)
        {
            if (_now is { } now)
            {
                Text(_gothic, 15, PageX, by + 16, $"Saving: {now.DateText}, mid-turn", Lacquer.Text);
                Text(_gothic, 14, PageX, by + 40, $"{now.BudgetText} · {now.RealmText} · {now.Troops} troops", Lacquer.Quiet);
            }
            DrawButton(held is null ? "Save" : $"Save over {label}");
        }
        else if (held is not null)
        {
            Text(_gothic, 15, PageX, by + 16, LoadNote(held), Lacquer.Text);
            Text(_gothic, 13, PageX, by + 40, $"Right click: {_back}", Lacquer.Quiet);
            DrawButton("Load");
        }
    }

    /// <summary>What a load does to Auto: a save of the moment leaves it (decision 26), a turn's start becomes it (<c>QC 1, n</c>).</summary>
    private string LoadNote(SaveSlot held) => held.Kind switch
    {
        SlotKind.Auto => "The game's own save, at its turn's start.",
        SlotKind.TurnStart => "Auto becomes this game too.",
        _ => _slots[0] is { } auto ? $"Auto stays as it is: {auto.DateText}." : "Auto stays as it is.",
    };

    private void DrawButton(string title)
    {
        bool hover = _hovered == ButtonHit;
        Lacquer.DrawRedButton(this, ButtonRect, hover, hover && _pressed == ButtonHit, _k);
        TextCentred(_brush, 22, ButtonRect.GetCenter().X, ButtonRect.GetCenter().Y, title, Lacquer.Name);
    }

    /// <summary>
    /// The slot's map as All Data draws it (<c>各種DATA.ADV:1-11</c>): its six tiles at a third, the markers over them at
    /// their own size — here at twice that (1280×533) for the page's large map; cached by tiles and markers.
    /// </summary>
    private ImageTexture? MapOf(SaveSlot slot)
    {
        string key = string.Join(",", slot.Tiles) + "|" + string.Join(";", slot.Marks.Select(m => $"{m.Cg},{m.X},{m.Y}"));
        if (_maps.TryGetValue(key, out var cached))
            return cached;
        ImageTexture? texture = null;
        var full = Image.CreateEmpty(1920, 800, false, Image.Format.Rgba8);
        bool any = false;
        for (int i = 0; i < slot.Tiles.Count; i++)
        {
            if (_pictures.Picture(slot.Tiles[i]) is not { } tile)
                continue;
            any = true;
            full.BlitRect(tile, new Rect2I(0, 0, Math.Min(640, tile.GetWidth()), Math.Min(400, tile.GetHeight())), new Vector2I(i % 3 * 640, i / 3 * 400));
        }
        if (any)
        {
            full.Resize(1280, 533, Image.Interpolation.Lanczos);
            foreach (var mark in slot.Marks)
            {
                if (_pictures.Picture(mark.Cg) is not { } picture)
                    continue;
                var big = (Image)picture.Duplicate();
                big.Resize(picture.GetWidth() * 2, picture.GetHeight() * 2, Image.Interpolation.Nearest);
                full.BlendRect(big, new Rect2I(Vector2I.Zero, big.GetSize()), new Vector2I(mark.X * 2, mark.Y * 2));
            }
            texture = ImageTexture.CreateFromImage(full);
        }
        _maps[key] = texture;
        return texture;
    }
}
