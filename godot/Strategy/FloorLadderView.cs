using Godot;
using Kichikuou.Game.Battles;
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
/// in the corner.
/// </summary>
internal sealed partial class FloorLadderView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Messages);

    // Right of the dungeon's picture (WAO_SUB 331: its frame spans x 355-925, y 193-536 on the screen), level with it.
    private const float X = 948, Y = 196, W = 248, H = 340, Inset = 16, WayH = 72, RetreatH = 40;

    // The touch rail: right of window 113 (x 248-1032 under the picture) to the screen's edge, down to its foot.
    private const float TouchX = 1040, TouchY = 424, TouchW = 236, TouchH = 372, TouchInset = 12;

    private enum Part { None, Up, Down, Retreat }

    private readonly FontFile _brush = Lacquer.Brush();
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);
    private readonly bool _touch = TouchMode.On;
    private FontFile _gothic;
    private FloorLadder? _ladder;
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

    private Rect2 PanelRect => _touch ? new Rect2(TouchX, TouchY, TouchW, TouchH) : new Rect2(X, Y, W, H);

    private Rect2 RectOf(Part part)
    {
        if (_touch)
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
    private int? Choice(Part part) => _ladder is not { } l ? null : part switch
    {
        Part.Up => l.Up?.Choice,
        Part.Down => l.Down?.Choice,
        Part.Retreat => l.Retreat,
        _ => null,
    };

    private Part PartAt(Vector2 window)
    {
        var at = window / _k;
        foreach (var part in (Part[])[Part.Up, Part.Down, Part.Retreat])
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
        if (_ladder is not { } l)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        var panel = PanelRect;
        Ui.DrawPanel(this, panel, _k);
        if (l.Up is { } up)
            DrawWay(Part.Up, up, true, false);
        // The floor between the arrows.
        float mid = panel.GetCenter().X, floorY = _touch ? TouchY + 200 : Y + 112;
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
            DrawWay(Part.Down, down, false, _touch);
        float inset = _touch ? TouchInset : Inset;
        Ui.DrawDivider(this, panel.Position.X + inset + 8, panel.End.X - inset - 8, _touch ? TouchY + 90 : Y + 266, _k);
        var retreat = RectOf(Part.Retreat);
        int retreatSize = _touch ? 22 : 20;
        Ui.DrawRedButton(this, retreat, _hovered == Part.Retreat, _pressed == Part.Retreat && _hovered == Part.Retreat, _k);
        float rw = _brush.GetStringSize(l.RetreatText, fontSize: retreatSize).X;
        DrawString(_brush, new Vector2(retreat.GetCenter().X - rw / 2, Lacquer.Baseline(retreat, _brush, retreatSize)),
            l.RetreatText, fontSize: retreatSize, modulate: Color.Color8(250, 226, 160));
    }

    private void DrawWay(Part part, FloorWay way, bool up, bool large)
    {
        var rect = RectOf(part);
        Ui.DrawRowBox(this, rect, _hovered == part, _k);
        float cy = rect.GetCenter().Y, cx = rect.Position.X + 34, half = large ? 18 : 15, h = large ? 15 : 13;
        Vector2[] arrow = up
            ? [new(cx - half, cy + h), new(cx + half, cy + h), new(cx, cy - h)]
            : [new(cx - half, cy - h), new(cx + half, cy - h), new(cx, cy + h)];
        Lacquer.DrawSmoothPolygon(this, arrow, way.Deeper ? Ui.Gold : Ui.Quiet, _k);
        float tx = rect.Position.X + (large ? 66 : 62);
        int size = large ? 26 : 24;
        DrawString(_gothic, new Vector2(tx, cy - (large ? 12 : 11) + (_gothic.GetAscent(size) - _gothic.GetDescent(size)) / 2),
            way.Text, fontSize: size, modulate: Ui.Text);
        string caption = $"{(way.Deeper ? "deeper" : "back")} · to {way.To}F";
        DrawString(_gothic, new Vector2(tx + 1, cy + (large ? 19 : 17) + (_gothic.GetAscent(14) - _gothic.GetDescent(14)) / 2),
            caption, fontSize: 14, modulate: way.Deeper ? Ui.Name : Ui.Quiet);
    }
}
