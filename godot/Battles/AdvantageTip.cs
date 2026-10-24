using Godot;
using Kichikuou.Game.Strategy;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The tooltip of an advantage next to an army's name on our battle screen (owner, 2026-10-15; the texts are
/// <see cref="Original.Content.Battles.BattleAdvantage.Parts"/>): up at once while the pointer is on the label, no
/// delay, in the context menus' style, kept on the screen. Points are layout pixels of the 1280×800 screen.
/// </summary>
internal sealed partial class AdvantageTip : Control
{
    private const float Width = 360, Pad = 10, TextSize = 17, Below = 20, Right = 14;

    private DeploymentPalette _colours = DeploymentPalette.BattleChosen;
    private float _scale = 1;
    private string _text = "";
    private Vector2 _pointer;                                       // in this control's coordinates

    public AdvantageTip()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ZIndex = 20;                                                // over the cards and the command layer
        Visible = false;
    }

    public void SetPalette(DeploymentPalette palette)
    {
        _colours = palette;
        QueueRedraw();
    }

    public void Layout(float scale)
    {
        _scale = scale;
        QueueRedraw();
    }

    /// <summary>Shows <paramref name="text"/> by the pointer, or hides the tip when it is null.</summary>
    public void ShowAt(string? text)
    {
        if (text is null)
        {
            Visible = false;
            return;
        }
        var pointer = TouchMode.LocalPointer(this);
        if (Visible && text == _text && pointer == _pointer)
            return;
        (_text, _pointer) = (text, pointer);
        Visible = true;
        QueueRedraw();
    }

    public override void _Draw()
    {
        if (GetThemeDefaultFont() is not { } font)
            return;
        float s = _scale;
        int size = (int)Math.Round(TextSize * s);
        float width = (Width - 2 * Pad) * s, pad = Pad * s;
        var text = font.GetMultilineStringSize(_text, HorizontalAlignment.Left, width, size);
        var box = new Vector2(text.X + 2 * pad, text.Y + 2 * pad);
        var at = _pointer + new Vector2(Right, Below) * s;
        if (at.Y + box.Y > Size.Y)                                  // no room below: above the pointer
            at.Y = _pointer.Y - Below * s / 2 - box.Y;
        at.X = Math.Clamp(at.X, 0, Math.Max(0, Size.X - box.X));
        at.Y = Math.Clamp(at.Y, 0, Math.Max(0, Size.Y - box.Y));
        var r = new Rect2(at, box);
        DrawRect(new Rect2(r.Position + new Vector2(4, 5) * s, r.Size), new Color(0, 0, 0, 0.3f));
        DrawRect(r, _colours.Card);
        DrawRect(r, _colours.ButtonBorder, filled: false, width: Math.Max(1, 2 * s));
        DrawMultilineString(font, at + new Vector2(pad, pad + font.GetAscent(size)), _text, HorizontalAlignment.Left, width, size,
            modulate: _colours.Text);
    }
}
