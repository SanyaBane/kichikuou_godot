using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The script's windows over our Treasure screen as a callout at the clicked cell (docs/ui/pickers.md, round W, the owner's
/// W2): a lacquer box under the cell (over it near the screen's foot), its tail on the cell, the window's text in the
/// game's font with the script's own line breaks. A refusal (a key wait) shows the click's triangle and takes no input —
/// the screen's own key wait answers it; a question's menu (<c>L_00a3f</c>'s Yes/No) is drawn as buttons inside the box,
/// the first in red lacquer: a button's click answers the script's menu with its item (1-based), a right click cancels
/// (0), as the original's box. Our own question before an item replaces a unit's (<c>TreasureRoster.ReplaceQuestion</c>)
/// shows the same way. In the touch mode the last tap is the screen's button, and the callout stands beside it instead,
/// at the top of the info panel left of it, as tall as its text, its tail on the button (docs/ui/pickers.md, §6, the owner's X4). Laid out in
/// the 1280×800 screen's pixels, scaled with the picture. On the phone held landscape (<see cref="PlacePhone"/>; docs/ui/phone-build.md,
/// step L5, LU1q) it is laid out over the whole screen in the reference phone's pixels at the phone's sizes, a callout beside its
/// anchor standing on the room's foot (its tail to the anchor on either hand), the answers filling its width with the first, in
/// red lacquer, at the right: nearer the thumb.
/// </summary>
internal sealed partial class TreasureCallout : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    /// <summary>The callout's figures: the PC's in the 1280×800 screen's pixels, the phone's in the reference phone's (LU1q).</summary>
    private sealed record Metrics(float Pad, float LineStep, float TextSize, float Gap, float TailX, float TailH, float ButtonW,
        float ButtonH, float ButtonGap, float MinWidth, int LabelSize, float TailHalf, float Edge)
    {
        public static readonly Metrics Pc = new(26, 28, 20, 14, 60, 13, 136, 38, 14, 300, 17, 13, 8);
        public static readonly Metrics Phone = new(40, 60, 46, 33, 120, 30, 258, 130, 24, 620, 44, 30, 18);
    }

    private bool _phone;
    private Vector2 _room = new(1280, 800);         // the layout's room: the picture's, or the phone's whole screen
    private (float Left, float Right) _across = (0, 1280);      // what a callout may span: the room, on the phone between the hole's strips

    private Metrics M => _phone ? Metrics.Phone : Metrics.Pc;

    private float Pad => M.Pad;

    private float LineStep => M.LineStep;

    private float TextSize => M.TextSize;

    private float Gap => M.Gap;

    private float TailX => M.TailX;

    private float TailH => M.TailH;

    private float ButtonW => M.ButtonW;

    private float ButtonH => M.ButtonH;

    private float ButtonGap => M.ButtonGap;

    private float MinWidth => M.MinWidth;
    private static Color Fill => Ui.CalloutFill;

    private readonly FontVariation _cinzel = Lacquer.Cinzel(700);
    private FontFile _gothic;
    private IReadOnlyList<string> _lines = [];
    private Rect2 _cell;
    private Rect2? _beside;                         // the area a callout beside its anchor fills (its right edge by the anchor)
    private bool _waiting;
    private IReadOnlyList<string>? _answers;
    private float _k = 1;                           // window pixels to a screen pixel
    private int _hovered = -1, _pressed = -1;
    private TaskCompletionSource<int>? _answer;

    public TreasureCallout(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Ignore;
        Visible = false;
    }

    /// <summary>Option → Display.</summary>
    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        QueueRedraw();
    }

    /// <summary>
    /// Shows the window's <paramref name="lines"/> at <paramref name="cell"/> (screen pixels), the triangle while
    /// <paramref name="waiting"/>; with <paramref name="beside"/> at the anchor's left, filling that area.
    /// </summary>
    public void Show(IReadOnlyList<string> lines, Rect2 cell, bool waiting, Rect2? beside = null)
    {
        if (Visible && lines.SequenceEqual(_lines) && cell == _cell && waiting == _waiting && beside == _beside)
            return;
        _lines = lines;
        _cell = cell;
        _beside = beside;
        _waiting = waiting;
        Visible = true;
        QueueRedraw();
    }

    /// <summary>Hides the callout, and its answers with it.</summary>
    public new void Hide()
    {
        if (!Visible)
            return;
        CloseMenu();
        Visible = false;
    }

    /// <summary>The script's menu over the window: its items as the callout's buttons.</summary>
    public void OpenMenu(IReadOnlyList<string> answers)
    {
        _answers = answers;
        _hovered = _pressed = -1;
        MouseFilter = MouseFilterEnum.Stop;
        QueueRedraw();
    }

    /// <summary>The script's answer: an item (1-based), or 0 for a cancel.</summary>
    public Task<int> ChooseAsync()
    {
        _answer = new TaskCompletionSource<int>();
        return _answer.Task;
    }

    public void CloseMenu()
    {
        _answer?.TrySetCanceled();
        _answer = null;
        _answers = null;
        MouseFilter = MouseFilterEnum.Ignore;
        MouseDefaultCursorShape = CursorShape.Arrow;
        QueueRedraw();
    }

    /// <summary>Lays the layer over the screen's picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        Lay(area, area.Size.X / 1280f, phone: false);
    }

    /// <summary>Lays the layer over <paramref name="area"/> (the picture), or on the phone held landscape over its whole screen.</summary>
    public void PlaceOnDevice(Rect2 area)
    {
        if (Device.Landscape(this) is { } phone)
            PlacePhone(phone);
        else
            Place(area);
    }

    /// <summary>
    /// Lays the layer over the phone's whole screen (the screen <paramref name="phone"/> was laid out for), in the reference
    /// phone's pixels; a callout keeps out of the hole's strips.
    /// </summary>
    public void PlacePhone(PhoneFrame phone)
    {
        Lay(new Rect2(0, 0, phone.Screen.W, phone.Screen.H), phone.Unit, phone: true);
        var across = (phone.Safe.Left / phone.Unit, (phone.Screen.W - phone.Safe.Right) / phone.Unit);
        if (across != _across)
        {
            _across = across;
            QueueRedraw();
        }
    }

    private void Lay(Rect2 area, float k, bool phone)
    {
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f && phone == _phone)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        _phone = phone;
        _room = phone ? area.Size / k : new Vector2(1280, 800);
        if (!phone)
            _across = (0, 1280);
        QueueRedraw();
    }

    /// <summary>A rectangle in global coordinates in the callout's layout pixels (a cell to show it at).</summary>
    public Rect2 FromGlobal(Rect2 global) => new((global.Position - GlobalPosition) / _k, global.Size / _k);

    private int Answers => _answers?.Count ?? 0;

    /// <summary>The lines drawn: beside an anchor the box's width is the area's, so a longer line breaks between words.</summary>
    private IReadOnlyList<string> Shown
    {
        get
        {
            if (_beside is not { } area)
                return _lines;
            float room = area.Size.X - 2 * Pad;
            var shown = new List<string>();
            foreach (string line in _lines)
            {
                string current = "";
                foreach (string word in line.Split(' '))
                {
                    string longer = current == "" ? word : $"{current} {word}";
                    if (current != "" && _gothic.GetStringSize(longer, fontSize: (int)TextSize).X > room)
                    {
                        shown.Add(current);
                        longer = word;
                    }
                    current = longer;
                }
                shown.Add(current);
            }
            return shown;
        }
    }

    /// <summary>Whether the script's menu is open in the callout.</summary>
    public bool MenuOpen => _answers is not null;

    /// <summary>The box: as wide as its widest line or its buttons, under the cell, or over it when there is no room below.</summary>
    private Rect2 Box
    {
        get
        {
            float widest = Shown.Count == 0 ? 0 : Shown.Max(l => _gothic.GetStringSize(l, fontSize: (int)TextSize).X);
            float buttons = Answers * ButtonW + Math.Max(0, Answers - 1) * ButtonGap;
            float w = Math.Max(MinWidth, Math.Max(widest, buttons) + 2 * Pad + (Answers == 0 ? 30 : 0));
            float h = Pad + Shown.Count * LineStep + (Answers > 0 ? ButtonH + 2 * Gap + 4 : 34);
            if (_beside is { } area)                                            // as tall as its text; the screen hides what is under it
                return _phone ? new Rect2(area.Position.X, area.End.Y - h, area.Size.X, h) : new Rect2(area.Position, new Vector2(area.Size.X, h));
            float x = Math.Clamp(_cell.Position.X - M.Edge, _across.Left + M.Edge, Math.Max(_across.Left + M.Edge, _across.Right - M.Edge - w));
            float y = _cell.End.Y + Gap;
            if (y + h > _room.Y - M.Edge)
                y = _cell.Position.Y - Gap - h;
            return new Rect2(x, y, w, h);
        }
    }

    private bool Above => Box.Position.Y < _cell.Position.Y;

    private Rect2 ButtonRect(int i)
    {
        var box = Box;
        if (!_phone)
            return new Rect2(box.Position.X + Pad + i * (ButtonW + ButtonGap), box.End.Y - Gap - 4 - ButtonH, ButtonW, ButtonH);
        float w = (box.Size.X - 2 * Pad - (Answers - 1) * ButtonGap) / Math.Max(1, Answers);     // the first at the right
        return new Rect2(box.Position.X + Pad + (Answers - 1 - i) * (w + ButtonGap), box.End.Y - Gap - 4 - ButtonH, w, ButtonH);
    }

    private int ButtonAt(Vector2 window)
    {
        var at = window / _k;
        for (int i = 0; i < Answers; i++)
            if (ButtonRect(i).HasPoint(at))
                return i;
        return -1;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_answer is null)
            return;
        switch (e)
        {
            case InputEventMouseMotion motion:
                int over = ButtonAt(motion.Position);
                if (over != _hovered)
                {
                    _hovered = over;
                    MouseDefaultCursorShape = over >= 0 ? CursorShape.PointingHand : CursorShape.Arrow;
                    QueueRedraw();
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                Answer(0);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = ButtonAt(button.Position);
                if (button.Pressed)
                    _pressed = at;
                else if (at >= 0 && at == _pressed)
                    Answer(at + 1);
                else
                    _pressed = -1;
                QueueRedraw();
                break;
        }
        AcceptEvent();
    }

    /// <summary>Answers the open menu as a click on its item <paramref name="choice"/> would (unattended play).</summary>
    public void Choose(int choice) => Answer(choice);

    private void Answer(int choice)
    {
        var answer = _answer;
        _answer = null;
        answer?.TrySetResult(choice);
    }

    /// <summary>The triangle bobs: drawn again every frame while it shows.</summary>
    public override void _Process(double delta)
    {
        if (Visible && _waiting && Answers == 0)
            QueueRedraw();
    }

    public override void _Draw()
    {
        if (_lines.Count == 0)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        var box = Box;
        Ui.DrawPanel(this, box, _k, _beside is null ? Fill : Fill with { A = 1 });     // beside: over the panel's line, opaque
        DrawTail(box);
        float x = box.Position.X + Pad, y = box.Position.Y + Pad;
        for (int i = 0; i < Shown.Count; i++)
        {
            var band = new Rect2(x, y + i * LineStep - 4, box.Size.X - 2 * Pad, LineStep);
            DrawString(_gothic, new Vector2(x, Lacquer.Baseline(band, _gothic, (int)TextSize)), Shown[i], fontSize: (int)TextSize,
                modulate: MessageWindowView.NarrationText);
        }
        for (int i = 0; i < Answers; i++)
        {
            var r = ButtonRect(i);
            bool hover = i == _hovered, down = hover && i == _pressed;
            if (i == 0)
                Ui.DrawRedButton(this, r, hover, down, _k);
            else
                Ui.DrawRowBox(this, r, hover, _k);
            string label = _answers![i];
            float w = _cinzel.GetStringSize(label, fontSize: M.LabelSize).X;
            DrawString(_cinzel, new Vector2(r.GetCenter().X - w / 2, Lacquer.Baseline(r, _cinzel, M.LabelSize)), label, fontSize: M.LabelSize,
                modulate: hover ? Ui.Name : Ui.Text);
        }
        if (_waiting && Answers == 0)
            Ui.DrawTriangle(this, new Vector2(box.End.X - Pad, box.End.Y - Pad * 0.85f), _k);
    }

    /// <summary>The tail: a gold wedge from the box's edge to the cell, the box's lacquer inside it.</summary>
    private void DrawTail(Rect2 box)
    {
        float m = M.TailHalf / Metrics.Pc.TailHalf;    // the PC's wedge at the device's size
        float half = M.TailHalf, inner = 10 * m;
        if (_beside is not null)
        {
            // Beside the anchor: from the box's edge facing it (its right, or on the phone its left for an anchor at its left).
            float ty = Math.Clamp(_cell.GetCenter().Y, box.Position.Y + 24 * m, box.End.Y - 24 * m);
            float d = _cell.GetCenter().X < box.Position.X ? -1 : 1, ex = d > 0 ? box.End.X : box.Position.X;
            DrawColoredPolygon([new(ex - 2 * m * d, ty - half), new(ex + TailH * d, ty), new(ex - 2 * m * d, ty + half)], Ui.Gold);
            DrawColoredPolygon([new(ex - 3 * m * d, ty - inner), new(ex + (TailH - 5 * m) * d, ty), new(ex - 3 * m * d, ty + inner)],
                Fill with { A = 1 });
            return;
        }
        float tx = Math.Clamp(_cell.Position.X + TailX, box.Position.X + 24 * m, box.End.X - 24 * m);
        float edge = Above ? box.End.Y : box.Position.Y;
        float s = Above ? 1 : -1;                       // the tail's direction: down to a cell below, up to one above
        DrawColoredPolygon([new(tx - half, edge - 2 * m * s), new(tx, edge + TailH * s), new(tx + half, edge - 2 * m * s)], Ui.Gold);
        DrawColoredPolygon([new(tx - inner, edge - 3 * m * s), new(tx, edge + (TailH - 5 * m) * s), new(tx + inner, edge - 3 * m * s)],
            Fill with { A = 1 });
    }
}
