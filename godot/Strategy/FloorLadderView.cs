using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our dungeon floor menu (docs/ui/dungeon-menu.md, the owner's 3e-3g, 2026-10-25): instead of the selection box over
/// the dungeon's picture, a lacquer ladder right of it — Ascend (▲) above the floor, Descend (▼) below it, Retreat as a
/// red button at its foot. The way deeper has the gold arrow and its caption in gold, the way back is quiet; a way the
/// script does not offer is left out and its place stays empty, so nothing moves between floors. A button's click
/// answers the script's menu with its item; a right click cancels (0), which only opens the menu again, as the
/// original's. Laid out in the 1280×800 screen's pixels, scaled with the picture. In the touch mode (the owner's B,
/// 2026-10-26; docs/ui/dungeon-menu.md, §5) it is a rail in the lower right corner, right of window 113, within the
/// right thumb's reach: Retreat at its top, furthest from the resting thumb, then Ascend, the floor and a larger Descend
/// in the corner. The touch mode's rail also holds the dungeon's menu before a search (<see cref="OpenEntry"/>; §6, the
/// owner's B): Start Search where Descend stands once the search begins, Return on top as Retreat, the other items
/// between them, the rail growing upwards. On the phone held landscape the tablet's rail stays in the frame at the phone's
/// sizes (LF2, LF2e; <c>FloorLadderView.Phone.cs</c>); held upright it stands at the right thumb on the bar's foot, window
/// 113 left of it and the floor's picture at the width over them (PF1, PF1e; <c>FloorLadderView.Portrait.cs</c>).
/// </summary>
internal sealed partial class FloorLadderView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Messages);

    // Right of the dungeon's picture (WAO_SUB 331: its frame spans x 355-925, y 193-536 on the screen), level with it.
    private const float X = 948, Y = 196, W = 248, H = 340, Inset = 16, WayH = 72, RetreatH = 40;

    // The touch rail: right of window 113 (x 248-1032 under the picture) to the screen's edge, down to its foot.
    private const float TouchX = 1040, TouchY = 424, TouchW = 236, TouchH = 372, TouchInset = 12;

    // The menu before a search on the touch rail: Start Search in Descend's place, a row's pitch, Return's height.
    private const float EntryPitch = 54, EntryRowH = 46, EntryReturnH = 60;

    private enum Part { None, Up, Down, Retreat, Start, Return, Other0 }

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private static bool Touch => Device.TouchLayout;                // the tablet's layout, read live (docs/ui/phone-build.md, §3)
    private FontFile _gothic;
    private FloorLadder? _ladder;
    private DungeonEntryMenu? _entry;
    private float _k = 1;                           // window pixels to a screen pixel
    private Part _hovered, _pressed;
    private TaskCompletionSource<int>? _answer;

    public FloorLadderView(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>Shows <paramref name="ladder"/>; <see cref="ChooseAsync"/> waits for its answer.</summary>
    public void Open(FloorLadder ladder)
    {
        _ladder = ladder;
        _entry = null;
        _hovered = _pressed = Part.None;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>Shows the dungeon's menu before a search on the touch rail; <see cref="ChooseAsync"/> waits for its answer.</summary>
    public void OpenEntry(DungeonEntryMenu entry)
    {
        _entry = entry;
        _ladder = null;
        _hovered = _pressed = Part.None;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>The script's answer: a way's or Retreat's choice, or 0 for a cancel.</summary>
    public Task<int> ChooseAsync()
    {
        _answer = new TaskCompletionSource<int>();
        return _answer.Task;
    }

    public void Close()
    {
        _answer?.TrySetCanceled();
        _answer = null;
        _ladder = null;
        _entry = null;
        Visible = false;
    }

    /// <summary>
    /// Lays the layer over the screen's picture (<paramref name="area"/>, in its parent's coordinates); on the phone held
    /// landscape over the whole screen in its frame's layout pixels, the rail at the phone's sizes (LF2, <see cref="PhoneLadder"/>;
    /// upright PF1, <see cref="PhoneLadderPortrait"/>).
    /// </summary>
    public void Place(Rect2 area)
    {
        var phone = Device.Held(this);
        if (phone is not null)
            area = new Rect2(Vector2.Zero, GetParentAreaSize());
        float k = phone?.Scale ?? area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f && phone == _phone)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        _phone = phone;
        QueueRedraw();
    }

    private Rect2 PanelRect => _phone is { } f ? PhonePanelRect(f)
        : _entry is not null ? new Rect2(TouchX, EntryTop, TouchW, TouchY + TouchH - EntryTop)
        : Touch ? new Rect2(TouchX, TouchY, TouchW, TouchH) : new Rect2(X, Y, W, H);

    /// <summary>The entry rail's top: over Start Search the other items, then Return.</summary>
    private float EntryTop => StartRect.Position.Y - 14 - (_entry?.Others.Count ?? 0) * EntryPitch - EntryReturnH - 28;

    /// <summary>Start Search: the touch ladder's Descend.</summary>
    private static Rect2 StartRect => new(TouchX + TouchInset, TouchY + 256, TouchW - 2 * TouchInset, 104);

    private Rect2 RectOf(Part part)
    {
        if (_phone is { } f)
            return PhoneRectOf(f, part);
        if (_entry is not null)
        {
            float x = TouchX + TouchInset, w = TouchW - 2 * TouchInset, top = EntryTop;
            return part switch
            {
                Part.Start => StartRect,
                Part.Return => new Rect2(x, top + 14, w, EntryReturnH),
                _ => new Rect2(x, top + 28 + EntryReturnH + (part - Part.Other0) * EntryPitch, w, EntryRowH),
            };
        }
        if (Touch)
        {
            float x = TouchX + TouchInset, w = TouchW - 2 * TouchInset;
            return part switch
            {
                Part.Retreat => new Rect2(x, TouchY + 14, w, 60),
                Part.Up => new Rect2(x, TouchY + 104, w, 78),
                _ => new Rect2(x, TouchY + 256, w, 104),
            };
        }
        return part switch
        {
            Part.Up => new Rect2(X + Inset, Y + 18, W - 2 * Inset, WayH),
            Part.Down => new Rect2(X + Inset, Y + 174, W - 2 * Inset, WayH),
            _ => new Rect2(X + Inset, Y + H - 16 - RetreatH, W - 2 * Inset, RetreatH),
        };
    }

    /// <summary>A part's answer to the menu, or null when the script does not offer it here.</summary>
    private int? Choice(Part part)
    {
        if (_entry is { } e)
            return part switch
            {
                Part.Start => e.Start,
                Part.Return => e.Return,
                >= Part.Other0 when part - Part.Other0 < e.Others.Count => e.Others[part - Part.Other0].Choice,
                _ => null,
            };
        return _ladder is not { } l ? null : part switch
        {
            Part.Up => l.Up?.Choice,
            Part.Down => l.Down?.Choice,
            Part.Retreat => l.Retreat,
            _ => null,
        };
    }

    private IEnumerable<Part> Parts => _entry is { } e
        ? [Part.Start, Part.Return, .. Enumerable.Range(0, e.Others.Count).Select(i => Part.Other0 + i)]
        : [Part.Up, Part.Down, Part.Retreat];

    private Part PartAt(Vector2 window)
    {
        var at = window / _k;
        foreach (var part in Parts)
            if (Choice(part) is not null && RectOf(part).HasPoint(at))
                return part;
        return Part.None;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_answer is null)
            return;
        switch (e)
        {
            case InputEventMouseMotion motion:
                var part = PartAt(motion.Position);
                if (part != _hovered)
                {
                    _hovered = part;
                    MouseDefaultCursorShape = part != Part.None ? CursorShape.PointingHand : CursorShape.Arrow;
                    QueueRedraw();
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                Answer(0);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                var at = PartAt(button.Position);
                if (button.Pressed)
                    _pressed = at;
                else if (at != Part.None && at == _pressed && Choice(at) is { } choice)
                    Answer(choice);
                else
                    _pressed = Part.None;
                QueueRedraw();
                break;
        }
        AcceptEvent();
    }

    private void Answer(int choice)
    {
        var answer = _answer;
        _answer = null;
        answer?.TrySetResult(choice);
    }

    public override void _Draw()
    {
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (_phone is { } phone)
        {
            DrawPhone(new PhoneUnits(phone));
            return;
        }
        if (_entry is { } entry)
        {
            DrawEntry(entry);
            return;
        }
        if (_ladder is not { } l)
            return;
        var panel = PanelRect;
        Ui.DrawPanel(this, panel, _k);
        int size = RungSize(l);
        if (l.Up is { } up)
            DrawWay(Part.Up, up, true, false, size);
        // The floor between the arrows.
        float mid = panel.GetCenter().X, floorY = Touch ? TouchY + 200 : Y + 112;
        const string label = "FLOOR";
        float lw = _cinzel.GetStringSize(label, fontSize: 12).X;
        DrawString(_cinzel, new Vector2(mid - lw / 2, floorY + (_cinzel.GetAscent(12) - _cinzel.GetDescent(12)) / 2), label,
            fontSize: 12, modulate: Ui.Subtitle);
        string floor = $"{l.Floor}F";
        float fw = _brush.GetStringSize(floor, fontSize: 34).X;
        DrawString(_brush, new Vector2(mid - fw / 2, floorY + 30 + (_brush.GetAscent(34) - _brush.GetDescent(34)) / 2), floor,
            fontSize: 34, modulate: Ui.Name);
        // The touch rail's Descend is larger: it sits in the corner, under the thumb.
        if (l.Down is { } down)
            DrawWay(Part.Down, down, false, Touch, size);
        float inset = Touch ? TouchInset : Inset;
        Ui.DrawDivider(this, panel.Position.X + inset + 8, panel.End.X - inset - 8, Touch ? TouchY + 90 : Y + 266, _k);
        var retreat = RectOf(Part.Retreat);
        Ui.DrawRedButton(this, retreat, _hovered == Part.Retreat, _pressed == Part.Retreat && _hovered == Part.Retreat, _k);
        float rw = _gothic.GetStringSize(l.RetreatText, fontSize: size).X;      // a rung's font: its red fill sets it apart
        DrawString(_gothic, new Vector2(retreat.GetCenter().X - rw / 2, Lacquer.Baseline(retreat, _gothic, size)),
            l.RetreatText, fontSize: size, modulate: Color.Color8(250, 226, 160));
    }

    /// <summary>The rungs' words, the ways' and Retreat's, at one size in the game's font (docs/ui/style.md, "Buttons side by side").</summary>
    private const int RungText = 24;

    /// <summary><see cref="RungText"/>, or as much less as the longest word needs in its rung (one fit for the ladder).</summary>
    private int RungSize(FloorLadder l)
    {
        float Room(Part part, bool large) => RectOf(part).Size.X - (large ? 66 : 62) - 8;
        int size = SideCells.Fit(_gothic, l.RetreatText, RungText, RectOf(Part.Retreat).Size.X - 16);
        if (l.Up is { } up)
            size = SideCells.Fit(_gothic, up.Text, size, Room(Part.Up, false));
        if (l.Down is { } down)
            size = SideCells.Fit(_gothic, down.Text, size, Room(Part.Down, Touch));
        return size;
    }

    private void DrawEntry(DungeonEntryMenu entry)
    {
        Ui.DrawPanel(this, PanelRect, _k);
        var back = RectOf(Part.Return);
        var start = RectOf(Part.Start);
        float tx = start.Position.X + 66;
        int size = EntrySize(entry, 22, start.End.X - 8 - tx, back.Size.X - 24);
        Ui.DrawRowBox(this, back, _hovered == Part.Return, _k);
        float bw = _gothic.GetStringSize(entry.ReturnText, fontSize: size).X;
        DrawString(_gothic, new Vector2(back.GetCenter().X - bw / 2, Lacquer.Baseline(back, _gothic, size)), entry.ReturnText,
            fontSize: size, modulate: Ui.Text);
        for (int i = 0; i < entry.Others.Count; i++)
        {
            var part = Part.Other0 + i;
            var rect = RectOf(part);
            Ui.DrawRowBox(this, rect, _hovered == part, _k);
            DrawString(_gothic, new Vector2(rect.Position.X + 16, Lacquer.Baseline(rect, _gothic, size)), entry.Others[i].Text,
                HorizontalAlignment.Left, rect.Size.X - 24, size, Ui.Text);
        }
        Ui.DrawRowBox(this, start, _hovered == Part.Start, _k);
        if (_hovered != Part.Start)
            DrawRect(start, Ui.Gold, false, Lacquer.Line(2, _k));
        float cy = start.GetCenter().Y, cx = start.Position.X + 34;
        Lacquer.DrawSmoothPolygon(this, [new(cx - 18, cy - 15), new(cx + 18, cy - 15), new(cx, cy + 15)], Ui.Gold, _k);
        DrawString(_gothic, new Vector2(tx, cy - 12 + (_gothic.GetAscent(size) - _gothic.GetDescent(size)) / 2), entry.StartText,
            fontSize: size, modulate: Ui.Text);
        DrawString(_gothic, new Vector2(tx + 1, cy + 19 + (_gothic.GetAscent(14) - _gothic.GetDescent(14)) / 2), "a unit goes down",
            fontSize: 14, modulate: Ui.Name);
    }

    /// <summary>
    /// The one size of the words of the menu before a search (owner, 2026-11-01): <paramref name="most"/>, or as much less as
    /// Start Search needs in <paramref name="startRoom"/> and any other word in <paramref name="rowRoom"/>.
    /// </summary>
    private int EntrySize(DungeonEntryMenu entry, int most, float startRoom, float rowRoom)
    {
        int size = SideCells.Fit(_gothic, entry.StartText, most, startRoom);
        foreach (string word in entry.Others.Select(o => o.Text).Append(entry.ReturnText))
            size = SideCells.Fit(_gothic, word, size, rowRoom);
        return size;
    }

    private void DrawWay(Part part, FloorWay way, bool up, bool large, int size)
    {
        var rect = RectOf(part);
        Ui.DrawRowBox(this, rect, _hovered == part, _k);
        float cy = rect.GetCenter().Y, cx = rect.Position.X + 34, half = large ? 18 : 15, h = large ? 15 : 13;
        Vector2[] arrow = up
            ? [new(cx - half, cy + h), new(cx + half, cy + h), new(cx, cy - h)]
            : [new(cx - half, cy - h), new(cx + half, cy - h), new(cx, cy + h)];
        Lacquer.DrawSmoothPolygon(this, arrow, way.Deeper ? Ui.Gold : Ui.Quiet, _k);
        float tx = rect.Position.X + (large ? 66 : 62);
        DrawString(_gothic, new Vector2(tx, cy - (large ? 12 : 11) + (_gothic.GetAscent(size) - _gothic.GetDescent(size)) / 2),
            way.Text, fontSize: size, modulate: Ui.Text);
        string caption = $"{(way.Deeper ? "deeper" : "back")} · to {way.To}F";
        DrawString(_gothic, new Vector2(tx + 1, cy + (large ? 19 : 17) + (_gothic.GetAscent(14) - _gothic.GetDescent(14)) / 2),
            caption, fontSize: 14, modulate: way.Deeper ? Ui.Name : Ui.Quiet);
    }
}
