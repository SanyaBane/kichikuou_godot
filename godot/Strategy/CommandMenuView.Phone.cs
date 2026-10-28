using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The command panel on the phone held landscape (docs/ui/phone-build.md, step L2; docs/ui/phone.md, §12, LM1b): the rails
// are the black sides — the week's commands in the left under their caption (the status's figures over it are the
// strip's), Turn END on the right's top and the other items from the foot up over Close (the map's button, CommandsButton)
// — the rows two lines, the tag over the name, 14.5 × 7.0 mm; the map dimmed, a tap anywhere but a row closes the panel.
// Over the whole screen in the frame's layout pixels (PhoneUnits); the rows' places are PhoneMap's.
internal sealed partial class CommandMenuView
{
    private PhoneFrame? _phone;                         // the phone held landscape: the rails in the sides
    private readonly FontVariation _caption = Lacquer.Cinzel(700);

    /// <summary>The map under the panel dimmed on the phone (the mock-up's 150 of 255).</summary>
    private static readonly Color Dim = new(0, 0, 0, 150 / 255f);

    /// <summary>On the phone held landscape, the panel over its parent's whole area; false elsewhere.</summary>
    private bool PlacePhone()
    {
        var phone = Device.Landscape(this);
        if (phone is not { } f)
        {
            if (_phone is not null)
                (_phone, _k) = (null, 0);                // placed again by the caller
            return false;
        }
        var size = GetParentAreaSize();
        if (f == _phone && Position == Vector2.Zero && Size == size)
            return true;
        (_phone, Position, Size) = (f, Vector2.Zero, size);
        QueueRedraw();
        return true;
    }

    /// <summary>Row <paramref name="row"/>'s box on the phone (the canvas's units): the commands, the items, Turn END.</summary>
    private PhoneFrame.Box PhoneRow(PhoneFrame f, int row) =>
        row < Commands ? PhoneMap.CommandRows(f, Commands)[row]
        : row < Commands + Links ? PhoneMap.LinkRows(f, Links)[row - Commands]
        : PhoneMap.TurnEnd(f);

    private int PhoneRowAt(PhoneFrame f, Vector2 at)
    {
        for (int r = 0; r < RowCount; r++)
            if (PhoneRow(f, r).Contains(at.X, at.Y))
                return r;
        return -1;
    }

    /// <summary>The map's middle on the phone, global: no row there.</summary>
    private Vector2 PhoneOutsidePoint(PhoneFrame f) =>
        GetGlobalRect().Position + new Vector2(f.Frame.X + f.Frame.W / 2, f.Frame.Y + f.Frame.H / 2);

    private void DrawPhone(PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        DrawRect(u.Rect(f.Frame), Dim);
        var caption = u.Rect(PhoneMap.CaptionBox(f));
        const string week = "THIS WEEK";
        int captionSize = u.Size(26);
        DrawString(_caption, new Vector2(caption.Position.X + u.Px(6), caption.Position.Y + _caption.GetAscent(captionSize)), week,
            fontSize: captionSize, modulate: Ui.Subtitle);
        for (int r = 0; r < Commands; r++)
        {
            var item = _menu!.Commands[r];
            SideCells.DrawRow(this, u, u.Rect(PhoneRow(f, r)), _gothic, item.Text, item.Enabled, r == _hovered || r == _pressed,
                TagOf(item.Action), item.WeeksText is not null ? item.WeeksLeft : null, Ui);
        }
        for (int r = Commands; r < Commands + Links; r++)
            SideCells.DrawRow(this, u, u.Rect(PhoneRow(f, r)), _gothic, _menu!.Links[r - Commands].Text,
                held: r == _hovered || r == _pressed, palette: Ui);
        int end = RowCount - 1;
        var rect = u.Rect(PhoneRow(f, end));
        Ui.DrawRedButton(this, rect, end == _hovered, end == _pressed && end == _hovered, u.K);
        string text = _menu!.TurnEndText;
        var brush = SideCells.Brush;
        int size = SideCells.Fit(brush, text, u.Size(44), rect.Size.X - u.Px(30));
        float tw = brush.GetStringSize(text, fontSize: size).X;
        DrawString(brush, new Vector2(rect.GetCenter().X - tw / 2, Lacquer.Baseline(rect, brush, size)), text, fontSize: size,
            modulate: Color.Color8(250, 226, 160));
    }
}
