using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The touch mode's tooltip of a table's column header (docs/ui/touch.md, "The tablet"): a long press on the header
/// shows the column's full name on a lacquer plate above it (below where the screen's top leaves no room), centred on
/// the finger, kept on the screen; the next press anywhere hides it. Top-level, so the table's clip does not cut it.
/// </summary>
internal sealed partial class HeaderTip : Control
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    private string _text = "";
    private Font? _font;
    private int _size;

    public HeaderTip()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        TopLevel = true;
        ZIndex = 30;
        Visible = false;
    }

    /// <summary>The text shown, or null while hidden.</summary>
    public string? Shown => Visible ? _text : null;

    /// <summary>
    /// Shows <paramref name="text"/> in <paramref name="font"/> at <paramref name="size"/> over <paramref name="header"/>
    /// (global coordinates), centred on <paramref name="x"/>.
    /// </summary>
    public void ShowAt(string text, Font font, int size, Rect2 header, float x)
    {
        (_text, _font, _size) = (text, font, size);
        float pad = size * 0.6f, gap = size * 0.4f;
        var box = font.GetStringSize(text, HorizontalAlignment.Left, -1, size) + new Vector2(2 * pad, 2 * pad);
        var screen = GetViewportRect().Size;
        float top = header.Position.Y - gap - box.Y;
        if (top < 0)
            top = header.End.Y + gap;
        Position = new Vector2(Math.Clamp(x - box.X / 2, 0, Math.Max(0, screen.X - box.X)), top);
        Size = box;
        Visible = true;
        QueueRedraw();
    }

    public override void _Input(InputEvent @event)
    {
        if (Visible && @event is InputEventMouseButton { Pressed: true })
            Visible = false;
    }

    public override void _Draw()
    {
        if (_font is null)
            return;
        var box = new Rect2(Vector2.Zero, Size);
        DrawRect(new Rect2(box.Position + new Vector2(3, 4), box.Size), new Color(0, 0, 0, 0.3f));
        DrawRect(box, Ui.Panel);
        DrawRect(box.Grow(-1), Ui.Gold, filled: false, width: 2);
        DrawString(_font, new Vector2(_size * 0.6f, Lacquer.Baseline(box, _font, _size)), _text, HorizontalAlignment.Left, -1, _size,
            Ui.Text);
    }
}
