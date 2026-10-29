using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The title's rows on the phone held upright (docs/ui/phone-build.md, step P8d; docs/ui/phone.md, §22, PE2 as the owner
/// changed it): under the title's picture a lacquer panel standing on the bar, a finger row for each button the title draws,
/// its own word alone in the row's middle in the brush face — Start a red button at the right thumb. A row's tap is told
/// (<see cref="Chosen"/>) and becomes a click in the middle of its button's box; the picture keeps its own targets (the
/// view takes only the presses on its panel). Drawn in the frame's layout pixels over the whole screen
/// (<see cref="PhoneUnits"/>), the places <see cref="PhoneTitleUpright"/>'s. Only on the phone held upright.
/// </summary>
internal sealed partial class TitleRowsView : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private readonly FontFile _brush = Lacquer.Brush();
    private PhoneFrame? _phone;
    private IReadOnlyList<int> _buttons = [];
    private int _pressed = -1;                      // the row held

    /// <summary>A row tapped: its button (0-7).</summary>
    public event Action<int>? Chosen;

    public TitleRowsView()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
    }

    /// <summary>Shows a row for each of <paramref name="buttons"/>, in the rows' order.</summary>
    public void ShowRows(IReadOnlyList<int> buttons)
    {
        if (!Visible || !buttons.SequenceEqual(_buttons))
        {
            _buttons = buttons;
            _pressed = -1;
            QueueRedraw();
        }
        Visible = true;
    }

    public void HideRows()
    {
        Visible = false;
        _pressed = -1;
    }

    /// <summary>Lays the view over the whole screen held upright; on any other device it never shows.</summary>
    public void Place()
    {
        var phone = Device.Upright(this);
        if (phone is null)
        {
            Visible = false;
            return;
        }
        var size = GetParentAreaSize();
        if (phone == _phone && Size == size)
            return;
        Position = Vector2.Zero;
        Size = size;
        _phone = phone;
        QueueRedraw();
    }

    private int RowAt(Vector2 at)
    {
        if (_phone is not { } f)
            return -1;
        for (int i = 0; i < _buttons.Count; i++)
            if (PhoneTitleUpright.Row(f, i, _buttons.Count).Contains(at.X, at.Y))
                return i;
        return -1;
    }

    /// <summary>Only the presses on the panel: the picture's are the title's own.</summary>
    public override bool _HasPoint(Vector2 point) =>
        Visible && _phone is { } f && _buttons.Count > 0 && PhoneTitleUpright.Panel(f, _buttons.Count).Contains(point.X, point.Y);

    public override void _GuiInput(InputEvent e)
    {
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            int at = RowAt(button.Position);
            if (button.Pressed)
                _pressed = at;
            else
            {
                if (at >= 0 && at == _pressed)
                    Chosen?.Invoke(_buttons[at]);
                _pressed = -1;
            }
            QueueRedraw();
        }
        AcceptEvent();
    }

    public override void _Draw()
    {
        if (_phone is not { } f || _buttons.Count == 0)
            return;
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        Ui.DrawPanel(this, u.Rect(PhoneTitleUpright.Panel(f, _buttons.Count)), u.K);
        for (int i = 0; i < _buttons.Count; i++)
        {
            var box = u.Rect(PhoneTitleUpright.Row(f, i, _buttons.Count));
            int button = _buttons[i];
            bool held = _pressed == i;
            if (button == PhoneTitleUpright.Start)
                Ui.DrawRedButton(this, box, held, held, u.K);
            else
            {
                DrawRect(box, held ? Ui.RowHover : Ui.Row);
                DrawRect(box, held ? Ui.HoverLine : Ui.DimGold, false, Lacquer.Line(u.Px(3), u.K));
            }
            string word = PhoneTitleUpright.Words[button];
            int size = SideCells.Fit(_brush, word, u.Size(PhoneTitleUpright.WordText), box.Size.X - u.Px(30));
            float w = _brush.GetStringSize(word, fontSize: size).X;
            DrawString(_brush, new Vector2(box.GetCenter().X - w / 2, PhonePickerParts.Middle(_brush, size, box.GetCenter().Y)), word,
                fontSize: size, modulate: Ui.Name);
        }
    }
}
