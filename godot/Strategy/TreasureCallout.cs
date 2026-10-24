using Godot;
using Kichikuou.Game.Battles;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The script's windows over our Treasure screen as a callout at the clicked cell (docs/ui/pickers.md, round W, the owner's
/// W2): a lacquer box under the cell (over it near the screen's foot), its tail on the cell, the window's text in the
/// game's font with the script's own line breaks. A refusal (a key wait) shows the click's triangle and takes no input —
/// the screen's own key wait answers it; a question's menu (<c>L_00a3f</c>'s Yes/No) is drawn as buttons inside the box,
/// the first in red lacquer: a button's click answers the script's menu with its item (1-based), a right click cancels
/// (0), as the original's box. Our own question before an item replaces a unit's (<c>TreasureRoster.ReplaceQuestion</c>)
/// shows the same way. Laid out in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class TreasureCallout : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private const float Pad = 26, LineStep = 28, TextSize = 20, Gap = 14, TailX = 60, TailH = 13;
    private const float ButtonW = 136, ButtonH = 38, ButtonGap = 14, MinWidth = 300;
    private static Color Fill => Ui.CalloutFill;

    private readonly FontVariation _cinzel = Lacquer.Cinzel(700);
    private FontFile _gothic;
    private IReadOnlyList<string> _lines = [];
    private Rect2 _cell;
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
    /// <paramref name="waiting"/>.
    /// </summary>
    public void Show(IReadOnlyList<string> lines, Rect2 cell, bool waiting)
    {
        if (Visible && lines.SequenceEqual(_lines) && cell == _cell && waiting == _waiting)
            return;
        _lines = lines;
        _cell = cell;
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
        float k = area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        QueueRedraw();
    }

    private int Answers => _answers?.Count ?? 0;

    /// <summary>Whether the script's menu is open in the callout.</summary>
    public bool MenuOpen => _answers is not null;

    /// <summary>The box: as wide as its widest line or its buttons, under the cell, or over it when there is no room below.</summary>
    private Rect2 Box
    {
        get
        {
            float widest = _lines.Count == 0 ? 0 : _lines.Max(l => _gothic.GetStringSize(l, fontSize: (int)TextSize).X);
            float buttons = Answers * ButtonW + Math.Max(0, Answers - 1) * ButtonGap;
            float w = Math.Max(MinWidth, Math.Max(widest, buttons) + 2 * Pad + (Answers == 0 ? 30 : 0));
            float h = Pad + _lines.Count * LineStep + (Answers > 0 ? ButtonH + 2 * Gap + 4 : 34);
            float x = Math.Clamp(_cell.Position.X - 8, 8, 1280 - 8 - w);
            float y = _cell.End.Y + Gap;
            if (y + h > 800 - 8)
                y = _cell.Position.Y - Gap - h;
            return new Rect2(x, y, w, h);
        }
    }

    private bool Above => Box.Position.Y < _cell.Position.Y;

    private Rect2 ButtonRect(int i)
    {
        var box = Box;
        return new Rect2(box.Position.X + Pad + i * (ButtonW + ButtonGap), box.End.Y - Gap - 4 - ButtonH, ButtonW, ButtonH);
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
        Ui.DrawPanel(this, box, _k, Fill);
        DrawTail(box);
        float x = box.Position.X + Pad, y = box.Position.Y + Pad;
        for (int i = 0; i < _lines.Count; i++)
        {
            var band = new Rect2(x, y + i * LineStep - 4, box.Size.X - 2 * Pad, LineStep);
            DrawString(_gothic, new Vector2(x, Lacquer.Baseline(band, _gothic, (int)TextSize)), _lines[i], fontSize: (int)TextSize,
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
            float w = _cinzel.GetStringSize(label, fontSize: 17).X;
            DrawString(_cinzel, new Vector2(r.GetCenter().X - w / 2, Lacquer.Baseline(r, _cinzel, 17)), label, fontSize: 17,
                modulate: hover ? Ui.Name : Ui.Text);
        }
        if (_waiting && Answers == 0)
            Ui.DrawTriangle(this, new Vector2(box.End.X - 26, box.End.Y - 22), _k);
    }

    /// <summary>The tail: a gold wedge from the box's edge to the cell, the box's lacquer inside it.</summary>
    private void DrawTail(Rect2 box)
    {
        float tx = Math.Clamp(_cell.Position.X + TailX, box.Position.X + 24, box.End.X - 24);
        float edge = Above ? box.End.Y : box.Position.Y;
        float s = Above ? 1 : -1;                       // the tail's direction: down to a cell below, up to one above
        DrawColoredPolygon([new(tx - 13, edge - 2 * s), new(tx, edge + TailH * s), new(tx + 13, edge - 2 * s)], Ui.Gold);
        DrawColoredPolygon([new(tx - 10, edge - 3 * s), new(tx, edge + (TailH - 5) * s), new(tx + 10, edge - 3 * s)], Fill with { A = 1 });
    }
}
