using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The touch mode's round <b>Commands</b> button at the map's bottom right (owner's M3, 2026-10-24; docs/ui/touch.md,
/// "The tablet"): shown with our name plates in <c>COMMAND</c>'s map wait, a tap on it is <see cref="Tapped"/> — the
/// screen answers it with the right click that opens the command panel, which a touch screen has no button for. Its
/// other events (the right click it causes, Back's) go on to the screen through <see cref="Forwarded"/>. While our
/// command panel is open it reads <b>Close</b> (<see cref="Closes"/>; owner, 2026-10-24), and its tap closes the panel.
/// </summary>
internal sealed partial class CommandsButton : Control
{
    // In the map's layout pixels (1280 across the picture): 190×84, 16 in from the picture's right and bottom edges.
    private static readonly Vector2 ButtonSize = new(190, 84);
    private const float Margin = 16, Text = 26, Border = 2;

    private DeploymentPalette _colours = DeploymentPalette.Chosen;
    private float _scale = 1;
    private bool _down, _inside, _closes;

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
        _scale = area.Size.X / 1280;
        Size = ButtonSize * _scale;
        Position = area.End - (ButtonSize + new Vector2(Margin, Margin)) * _scale;
        QueueRedraw();
    }

    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
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
        float s = _scale;
        var box = new StyleBoxFlat
        {
            BgColor = _down && _inside ? _colours.ButtonPressed : _colours.Button,
            BorderColor = _colours.ButtonBorder,
            AntiAliasing = true,
            ShadowColor = new Color(0, 0, 0, 0.35f),
            ShadowSize = (int)Math.Round(6 * s),
            ShadowOffset = new Vector2(2, 3) * s,
        };
        box.SetBorderWidthAll((int)Math.Max(1, Math.Round(Border * s)));
        box.SetCornerRadiusAll((int)Math.Round(Size.Y / 2));
        DrawStyleBox(box, new Rect2(Vector2.Zero, Size));
        if (GetThemeDefaultFont() is not { } font)
            return;
        int size = (int)Math.Round(Text * s);
        float baseline = (Size.Y + font.GetAscent(size) - font.GetDescent(size)) / 2;
        DrawString(font, new Vector2(0, baseline), _closes ? "Close" : "Commands", HorizontalAlignment.Center, Size.X, size, _colours.ButtonText);
    }
}
