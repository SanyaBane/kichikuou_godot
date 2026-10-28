using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The touch mode's <b>Commands</b> button at the map's bottom right (owner's M3, 2026-10-24; docs/ui/touch.md, "The
/// tablet"): shown with our name plates in <c>COMMAND</c>'s map wait, a tap on it is <see cref="Tapped"/> — the
/// screen answers it with the right click that opens the command panel, which a touch screen has no button for. Its
/// other events (the right click it causes, Back's) go on to the screen through <see cref="Forwarded"/>. While our
/// command panel is open it reads <b>Close</b> (<see cref="Closes"/>; owner, 2026-10-24), and its tap closes the panel.
/// Drawn in lacquer and gold as our panels (owner's A, 2026-10-24; "The map's and the battle's buttons, round 1"): the
/// double gold frame and its studs, an icon, the brush face of Turn END. On the phone held landscape it stands in the
/// right side at the thumb, its icon over its word (docs/ui/phone.md, §12, LM1; <see cref="PhoneMap.CommandsCell"/>), and
/// as Close a row's height where it stood (<see cref="PhoneMap.FootCell"/>).
/// </summary>
internal sealed partial class CommandsButton : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Map);

    // In the map's layout pixels (1280 across the picture): 220×72, 16 in from the picture's right and bottom edges.
    private static readonly Vector2 ButtonSize = new(220, 72);
    private const float Margin = 16, Text = 19, Icon = 24;

    private readonly FontFile _brush = Lacquer.Brush();
    private float _scale = 1;
    private bool _down, _inside, _closes;
    private PhoneFrame? _phone;                     // the phone held landscape: in the right side

    public CommandsButton()
    {
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        MouseExited += () => SetInside(false);
        MouseEntered += () => SetInside(true);
    }

    /// <summary>A tap on the button (its release over it).</summary>
    public event Action? Tapped;

    /// <summary>An event but a left button's: the screen's own.</summary>
    public event Action<InputEvent>? Forwarded;

    /// <summary>Whether the button reads Close: our command panel is open, and the tap closes it.</summary>
    public bool Closes
    {
        get => _closes;
        set
        {
            if (value == _closes)
                return;
            _closes = value;
            _down = false;
            QueueRedraw();
        }
    }

    /// <summary>The button's centre (its parent's coordinates).</summary>
    public Vector2 Centre => Position + Size / 2;

    /// <summary>Places the button on the picture <paramref name="area"/> (its parent's coordinates).</summary>
    public void Place(Rect2 area)
    {
        if (Device.Landscape(this) is { } f)
        {
            var cell = _closes ? PhoneMap.FootCell(f) : PhoneMap.CommandsCell(f);
            (_phone, Position, Size) = (f, new Vector2(cell.X, cell.Y), new Vector2(cell.W, cell.H));
            QueueRedraw();
            return;
        }
        _phone = null;
        _scale = area.Size.X / 1280;
        Size = ButtonSize * _scale;
        Position = area.End - (ButtonSize + new Vector2(Margin, Margin)) * _scale;
        QueueRedraw();
    }

    public override void _GuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            Forwarded?.Invoke(e);
            return;
        }
        AcceptEvent();
        if (button.Pressed)
            _down = true;
        else if (_down)
        {
            _down = false;
            if (new Rect2(Vector2.Zero, Size).HasPoint(button.Position))
                Tapped?.Invoke();
        }
        QueueRedraw();
    }

    private void SetInside(bool inside)
    {
        _inside = inside;
        if (!inside)
            _down = false;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (_phone is { } phone)
        {
            var u = new PhoneUnits(phone);
            DrawSetTransform(-Position, 0, new Vector2(u.K, u.K));
            SideCells.DrawButton(this, u, new Rect2(Position / u.K, Size / u.K), _closes ? SideCells.Icon.Close : SideCells.Icon.Menu,
                _closes ? "Close" : "Commands", _down && _inside, Ui);
            return;
        }
        float k = _scale;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(k, k));               // in layout pixels below
        var box = new Rect2(Vector2.Zero, ButtonSize);
        bool pressed = _down && _inside;
        Ui.DrawPanel(this, box, k, pressed ? Ui.RowHover : null);
        float cy = box.Size.Y / 2;
        var icon = new Vector2(36, cy);
        if (_closes)
        {
            float h = Icon * 0.35f, w = Lacquer.Line(3.5f, k);
            DrawLine(icon - new Vector2(h, h), icon + new Vector2(h, h), Ui.Gold, w, true);
            DrawLine(icon + new Vector2(-h, h), icon + new Vector2(h, -h), Ui.Gold, w, true);
        }
        else
        {
            float bar = Icon * 0.13f;
            for (int i = -1; i <= 1; i++)
                DrawRect(new Rect2(icon.X - Icon / 2, icon.Y + i * Icon * 0.35f - bar / 2, Icon, bar), Ui.Gold);
        }
        var label = new Rect2(60, 0, box.Size.X - 70, box.Size.Y);
        string text = _closes ? "Close" : "Commands";
        DrawString(_brush, new Vector2(label.Position.X, Lacquer.Baseline(label, _brush, (int)Text)), text,
            HorizontalAlignment.Center, label.Size.X, (int)Text, Ui.Name);
    }
}
