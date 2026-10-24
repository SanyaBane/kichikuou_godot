using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// A round button of the touch mode in the Commands button's look (<see cref="CommandsButton"/>): our battle screen's
/// Skip (owner, 2026-10-24; docs/ui/touch.md, "The Skip and Log buttons"; the scenes' are <see cref="SkipDrawer"/>), an
/// icon left of the label. Lit in gold
/// while what it switches is on (<see cref="On"/>); greyed and deaf while <see cref="Disabled"/>. A tap on it (its
/// release over it) is <see cref="Tapped"/>; its other events go on to the screen through <see cref="Forwarded"/>.
/// </summary>
internal sealed partial class TouchPill : Control
{
    /// <summary>The icon left of the label.</summary>
    public enum Icon
    {
        /// <summary>Two triangles pointing right.</summary>
        Skip,
    }

    private static readonly Color LitFill = Color.Color8(240, 182, 72), LitBorder = Colors.White,
        LitText = Color.Color8(40, 24, 8), LitGlow = new(1f, 0.74f, 0.35f, 0.75f);

    private const float Border = 2;

    private readonly Icon _icon;
    private DeploymentPalette _colours = DeploymentPalette.BattleChosen;
    private float _scale = 1, _text = 24;
    private bool _down, _inside, _on, _disabled;
    private string _label;

    public TouchPill(Icon icon, string label)
    {
        _icon = icon;
        _label = label;
        MouseFilter = MouseFilterEnum.Stop;
        Visible = false;
        MouseExited += () => SetInside(false);
        MouseEntered += () => SetInside(true);
    }

    /// <summary>A tap on the button (its release over it), unless <see cref="Disabled"/>.</summary>
    public event Action? Tapped;

    /// <summary>An event but a left button's: the screen's own.</summary>
    public event Action<InputEvent>? Forwarded;

    /// <summary>Lit: what the button switches is on.</summary>
    public bool On
    {
        get => _on;
        set => Set(ref _on, value);
    }

    /// <summary>Greyed: a tap does nothing.</summary>
    public bool Disabled
    {
        get => _disabled;
        set => Set(ref _disabled, value);
    }

    /// <summary>The label.</summary>
    public string Label
    {
        get => _label;
        set
        {
            if (_label == value)
                return;
            _label = value;
            QueueRedraw();
        }
    }

    /// <summary>The button's centre (its parent's coordinates).</summary>
    public Vector2 Centre => Position + Size / 2;

    /// <summary>
    /// Places the button at <paramref name="rect"/> (its parent's coordinates), drawn at <paramref name="scale"/> (the
    /// picture's to the screen's 1280) with its label at <paramref name="text"/> layout pixels.
    /// </summary>
    public void Place(Rect2 rect, float scale, float text)
    {
        if (Position == rect.Position && Size == rect.Size && Math.Abs(scale - _scale) < 0.001f && text == _text)
            return;
        Position = rect.Position;
        Size = rect.Size;
        _scale = scale;
        _text = text;
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
            if (!_disabled && new Rect2(Vector2.Zero, Size).HasPoint(button.Position))
                Tapped?.Invoke();
        }
        QueueRedraw();
    }

    private void Set(ref bool field, bool value)
    {
        if (field == value)
            return;
        field = value;
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
        bool pressed = _down && _inside && !_disabled;
        var box = new StyleBoxFlat
        {
            BgColor = _on ? (pressed ? LitFill.Darkened(0.15f) : LitFill)
                : _disabled ? _colours.ButtonDisabled : pressed ? _colours.ButtonPressed : _colours.Button,
            BorderColor = _on ? LitBorder : _colours.ButtonBorder,
            AntiAliasing = true,
            ShadowColor = _on ? LitGlow : new Color(0, 0, 0, 0.35f),
            ShadowSize = (int)Math.Round((_on ? 12 : 6) * s),
            ShadowOffset = _on ? Vector2.Zero : new Vector2(2, 3) * s,
        };
        box.SetBorderWidthAll((int)Math.Max(1, Math.Round(Border * s)));
        box.SetCornerRadiusAll((int)Math.Round(Size.Y / 2));
        DrawStyleBox(box, new Rect2(Vector2.Zero, Size));
        var ink = _on ? LitText : _disabled ? _colours.ButtonText with { A = 0.4f } : _colours.ButtonText;
        // The icon as large as the label's letters, a low button's kept clear of the rounded end.
        float h = Size.Y, iconSize = _text * s * 0.9f, iconX = Math.Max(h * 0.5f, iconSize * 1.4f);
        var centre = new Vector2(iconX, h / 2);
        DrawSkip(centre, iconSize, ink);
        if (GetThemeDefaultFont() is not { } font)
            return;
        int size = (int)Math.Round(_text * s);
        float left = iconX + iconSize * 0.9f, right = Size.X - h * 0.2f;
        float baseline = (h + font.GetAscent(size) - font.GetDescent(size)) / 2;
        DrawString(font, new Vector2(left, baseline), _label, HorizontalAlignment.Center, right - left, size, ink);
    }

    /// <summary>The skip's icon: two triangles pointing right, <paramref name="size"/> high, centred at <paramref name="c"/>.</summary>
    private void DrawSkip(Vector2 c, float size, Color colour)
    {
        float h = size / 2;
        foreach (float dx in (float[])[-size * 0.55f, size * 0.05f])
            DrawColoredPolygon([new(c.X + dx, c.Y - h), new(c.X + dx + size * 0.6f, c.Y), new(c.X + dx, c.Y + h)], colour);
    }
}
