using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our <c>]</c> menu (docs/ui/messages.md, §5 step 3; the owner's mock-up M1, 2026-10-18): a scene's menu as a lacquer
/// panel of rows where the original's selection box opens — the rows as the command panel's, the hovered one lit; the
/// marks ○ a gold ring, × a dim cross, △ a gold triangle, the text in the game's font. A row's click answers the
/// script's menu with its item (1-based); a right click cancels (0), as the original's box. On the phone held landscape
/// the rows stand at the frame's right foot, at the right thumb, in finger rows (docs/ui/phone.md, §19, LS1m). Laid out
/// in the 1280×800 screen's pixels, scaled with the picture.
/// </summary>
internal sealed partial class SceneMenuView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Messages);

    private const float Pad = 14;

    private static float MarkRoom => MathF.Round(34 * RowScale);

    // The rows: the mock-up's for a short menu (36 high, 8 apart, text 30); from six items the original's pitch (36: 30
    // high, 6 apart, text 26), so a long one (the save slots) takes about the box's room and leaves the window under it
    // free. Debug → Menu Row Height's second slider (MenuRowsChoice.DialogueHeight, 40 by default, owner 2026-10-25) scales them all.
    private static float RowScale => MenuRowsChoice.DialogueHeight / 36f;

    private float RowH => MathF.Round((Rows >= 6 ? 30 : 36) * RowScale);

    private float RowPitch => RowH + MathF.Round((Rows >= 6 ? 6 : 8) * RowScale);

    private int TextSize => (int)MathF.Round((Rows >= 6 ? 26 : 30) * RowScale);

    private FontFile _gothic;
    private MenuView? _menu;
    private Rect2 _box;                             // the original's box (screen pixels): our panel starts at its corner
    private float _width;
    private float _k = 1;                           // window pixels to a screen pixel
    private PhoneFrame? _phone;                     // the phone held landscape: the rows at the frame's right foot
    private Rect2[]? _phoneRows;                    // and where they stand (screen pixels)
    private Rect2 _phonePanel;
    private int _phoneText;
    private int _hovered = -1, _pressed = -1;
    private TaskCompletionSource<int>? _answer;

    public SceneMenuView(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    // An open menu follows Debug → Menu Row Height at once.
    public override void _EnterTree() => MenuRowsChoice.Changed += Relayout;

    public override void _ExitTree() => MenuRowsChoice.Changed -= Relayout;

    public void SetFont(GameFont font)
    {
        _gothic = DeploymentTableView.LoadFont(font);
        Relayout();
    }

    private void Relayout()
    {
        if (_menu is not null)
            _width = PanelWidth(_menu);
        PhoneLayout();
        QueueRedraw();
    }

    /// <summary>
    /// On the phone held landscape (docs/ui/phone.md, §19, LS1m) the rows at the frame's right foot, at the right thumb, in
    /// finger rows (<see cref="PhoneScenes.SceneMenu"/>), in the screen's pixels; elsewhere none (the original box's place).
    /// </summary>
    private void PhoneLayout()
    {
        _phoneRows = null;
        if (_phone is not { } f || _menu is not { } menu)
            return;
        float k = f.Scale;                                              // the canvas's units a screen pixel
        int size = (int)Math.Round(PhoneScenes.ItemTextSize(f) / k);
        float widest = menu.Items.Max(i => _gothic.GetStringSize(i.Text, fontSize: size).X + (i.Mark is null ? 0 : 34 * size / 30f)) * k;
        var rows = PhoneScenes.SceneMenu(f, menu.Items.Count, widest);
        var (x0, y0) = f.IsUpright ? (0f, 0f) : (f.Frame.X, f.Frame.Y);  // upright the layer covers the screen
        Rect2 Screen(PhoneFrame.Box b) => new((b.X - x0) / k, (b.Y - y0) / k, b.W / k, b.H / k);
        _phoneRows = [.. rows.Rows.Select(Screen)];
        _phonePanel = Screen(rows.Panel);
        _phoneText = Math.Max(8, (int)Math.Round(rows.TextSize / k));
    }

    /// <summary>Shows <paramref name="menu"/> at the original box's place (<paramref name="box"/>, screen pixels).</summary>
    public void Open(MenuView menu, (int X, int Y, int W, int H) box)
    {
        _menu = menu;
        _box = new Rect2(box.X, box.Y, box.W, box.H);
        _width = PanelWidth(menu);
        _hovered = _pressed = -1;
        Visible = true;
        PhoneLayout();
        QueueRedraw();
    }

    /// <summary>The script's answer: an item (1-based), or 0 for a cancel.</summary>
    public Task<int> ChooseAsync()
    {
        _answer = new TaskCompletionSource<int>();
        return _answer.Task;
    }

    public void Close()
    {
        _answer?.TrySetCanceled();
        _answer = null;
        _menu = null;
        Visible = false;
    }

    /// <summary>Lays the layer over the screen's picture (<paramref name="area"/>, in its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        var phone = Device.Held(this);
        if (phone is { IsUpright: true })
            area = new Rect2(Vector2.Zero, GetParentAreaSize());        // the rows under the picture, in the bar's place (PS2m)
        float k = phone is { IsUpright: true } up ? up.Scale : area.Size.X / 1280f;
        if (Position == area.Position && Size == area.Size && Math.Abs(k - _k) < 0.001f && phone == _phone)
            return;
        Position = area.Position;
        Size = area.Size;
        _k = k;
        _phone = phone;
        PhoneLayout();
        QueueRedraw();
    }

    private int Rows => _menu?.Items.Count ?? 0;

    /// <summary>As wide as the original's box, or its widest item, plus the rows' margins; kept inside the screen.</summary>
    private float PanelWidth(MenuView menu)
    {
        float widest = menu.Items.Max(i => _gothic.GetStringSize(i.Text, fontSize: TextSize).X + (i.Mark is null ? 0 : MarkRoom));
        return Math.Min(1280 - 16, Math.Max(_box.Size.X + 2 * Pad, widest + 2 * Pad + 36));
    }

    private Rect2 Panel
    {
        get
        {
            if (_phoneRows is not null)
                return _phonePanel;
            float h = 2 * Pad + Rows * RowPitch - (RowPitch - RowH);
            float x = Math.Clamp(_box.Position.X - Pad, 8, 1280 - 8 - _width);
            float y = Math.Clamp(_box.Position.Y - Pad, 8, Math.Max(8, 800 - 8 - h));
            return new Rect2(x, y, _width, h);
        }
    }

    private Rect2 RowRect(int row)
    {
        if (_phoneRows is { } rows)
            return rows[row];
        var p = Panel;
        return new Rect2(p.Position.X + Pad, p.Position.Y + Pad + row * RowPitch, p.Size.X - 2 * Pad, RowH);
    }

    private int RowAt(Vector2 window)
    {
        var at = window / _k;
        for (int r = 0; r < Rows; r++)
            if (RowRect(r).HasPoint(at))
                return r;
        return -1;
    }

    public override void _GuiInput(InputEvent e)
    {
        if (_answer is null)
            return;
        switch (e)
        {
            case InputEventMouseMotion motion:
                int row = RowAt(motion.Position);
                if (row != _hovered)
                {
                    _hovered = row;
                    MouseDefaultCursorShape = row >= 0 ? CursorShape.PointingHand : CursorShape.Arrow;
                    QueueRedraw();
                }
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Right, Pressed: false }:
                Answer(0);
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left } button:
                int at = RowAt(button.Position);
                if (button.Pressed)
                    _pressed = at;
                else if (at >= 0 && at == _pressed)
                    Answer(at + 1);
                else
                    _pressed = -1;
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

    private float Line(float width) => Lacquer.Line(width, _k);

    public override void _Draw()
    {
        if (_menu is null)
            return;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        Ui.DrawPanel(this, Panel, _k);
        for (int r = 0; r < Rows; r++)
            DrawRow(r, _menu.Items[r]);
    }

    private void DrawRow(int r, MenuEntry item)
    {
        var rect = RowRect(r);
        bool hover = r == _hovered;
        Ui.DrawRowBox(this, rect, hover, _k);
        int size = _phoneRows is null ? TextSize : _phoneText;
        float s = _phoneRows is null ? RowScale : size / 30f;       // the marks with the type
        float x = rect.Position.X + (_phoneRows is null ? 14 : 24 * size / 48f);
        if (item.Mark is { } mark)
        {
            var at = new Vector2(rect.Position.X + 20 * s, rect.GetCenter().Y);
            switch (mark)
            {
                case '○':
                    DrawArc(at, 7 * s, 0, Mathf.Tau, 24, Ui.Gold, Line(2), true);
                    break;
                case '×':
                    DrawLine(at + new Vector2(-6, -6) * s, at + new Vector2(6, 6) * s, Ui.OffText, Line(2), true);
                    DrawLine(at + new Vector2(-6, 6) * s, at + new Vector2(6, -6) * s, Ui.OffText, Line(2), true);
                    break;
                default:
                    DrawPolyline([at + new Vector2(0, -7) * s, at + new Vector2(7, 6) * s, at + new Vector2(-7, 6) * s, at + new Vector2(0, -7) * s],
                        Ui.Gold, Line(2), true);
                    break;
            }
            x = rect.Position.X + MathF.Round(34 * s) + 4;
        }
        DrawString(_gothic, new Vector2(x, Lacquer.Baseline(rect, _gothic, size)), item.Text, fontSize: size,
            modulate: hover ? Ui.Name : Ui.Text);
    }
}
