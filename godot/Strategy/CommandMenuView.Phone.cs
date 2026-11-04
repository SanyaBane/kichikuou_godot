using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The command panel on the phone held landscape (docs/ui/phone-build.md, step L2; docs/ui/phone.md, §12, LM1b): the rails
// are the black sides — the other items from the foot up in the left (the status's figures over them are the strip's),
// Turn END on the right's top and the week's commands under their caption from the foot up over Close (the map's button,
// CommandsButton; owner, 2026-10-30: the week's commands at the right thumb) — the rows two lines, the tag over the name,
// 14.5 × 7.0 mm; the map dimmed, a tap anywhere but a row closes the panel. Over the whole screen in the frame's layout
// pixels (PhoneUnits); the rows' places are PhoneMap's. Held upright (step P2; phone.md, §3, PM1b) the panel is a sheet
// over the map's foot — the other items in the left column and the week's commands in the right, both from the foot up,
// Turn END and "This week" over the commands, a row on one line, 7.1 mm — Close at the bar's right where Commands stood,
// the map above the sheet dimmed (PhoneMapPortrait.Commands).
internal sealed partial class CommandMenuView
{
    private PhoneFrame? _phone;                         // the phone: the rails in the sides, or the sheet upright
    private readonly FontVariation _caption = Lacquer.Cinzel(700);

    /// <summary>The map under the panel dimmed on the phone (the mock-up's 150 of 255).</summary>
    private static readonly Color Dim = new(0, 0, 0, 150 / 255f);

    /// <summary>The status band over the map held upright (<see cref="StatusStripView.PortraitBand"/>), which the sheet stays clear of; null for its least.</summary>
    public PhoneFrame.Box? PortraitBand { get; set; }

    /// <summary>The sheet held upright for the menu as it is.</summary>
    private PhoneMapPortrait.CommandSheet Sheet(PhoneFrame f) =>
        PhoneMapPortrait.Commands(f, PortraitBand ?? PhoneMapPortrait.StatusBand(f, 1).Band, Commands, Links);

    /// <summary>On the phone, the panel over its parent's whole area; false elsewhere.</summary>
    private bool PlacePhone()
    {
        var phone = Device.Held(this);
        if (phone is not { } f)
        {
            if (_phone is not null)
                (_phone, _k) = (null, 0);                // placed again by the caller
            return false;
        }
        var size = GetParentAreaSize();
        if (f == _phone && Position == Vector2.Zero && Size == size)
            return true;
        if (f != _phone)
            _pressed = -1;                                      // a turn: the press was on the old layout's row
        (_phone, Position, Size) = (f, Vector2.Zero, size);
        QueueRedraw();
        return true;
    }

    /// <summary>Row <paramref name="row"/>'s box on the phone (the canvas's units): the commands, the items, Turn END.</summary>
    private PhoneFrame.Box PhoneRow(PhoneFrame f, int row) =>
        f.IsPortrait ? PortraitRow(Sheet(f), row)
        : row < Commands ? PhoneMap.CommandRows(f, Commands)[row]
        : row < Commands + Links ? PhoneMap.LinkRows(f, Links)[row - Commands]
        : PhoneMap.TurnEnd(f);

    private int PhoneRowAt(PhoneFrame f, Vector2 at)
    {
        for (int r = 0; r < RowCount; r++)
            if (PhoneRow(f, r).Contains(at.X, at.Y))
                return r;
        return -1;
    }

    private PhoneFrame.Box PortraitRow(PhoneMapPortrait.CommandSheet sheet, int row) =>
        row < Commands ? sheet.Commands[row] : row < Commands + Links ? sheet.Links[row - Commands] : sheet.TurnEnd;

    /// <summary>The map's middle on the phone, global: no row there (upright the map's over the sheet).</summary>
    private Vector2 PhoneOutsidePoint(PhoneFrame f)
    {
        if (!f.IsPortrait)
            return GetGlobalRect().Position + new Vector2(f.Frame.X + f.Frame.W / 2, f.Frame.Y + f.Frame.H / 2);
        var band = PortraitBand ?? PhoneMapPortrait.StatusBand(f, 1).Band;
        return GetGlobalRect().Position + new Vector2(f.Room.X + f.Room.W / 2, (band.Bottom + Sheet(f).Sheet.Y) / 2);
    }

    private void DrawPhone(PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        if (f.IsPortrait)
        {
            DrawPortrait(f, u);
            return;
        }
        DrawRect(u.Rect(f.Frame), Dim);
        var caption = u.Rect(PhoneMap.CaptionBox(f));
        const string week = "THIS WEEK";
        int captionSize = u.Size(26);
        DrawString(_caption, new Vector2(caption.Position.X + u.Px(6), caption.Position.Y + _caption.GetAscent(captionSize)), week,
            fontSize: captionSize, modulate: Ui.Subtitle);
        // The rail's rows, Turn END among them: the game's font at one size (docs/ui/style.md, "Buttons side by side").
        int end = RowCount - 1;
        var rect = u.Rect(PhoneRow(f, end));
        string text = _menu!.TurnEndText;
        int size = SideCells.FitAll(_gothic, u.Size(PhoneMap.RowText * SideCells.RowScale(u, rect)),
            [.. Enumerable.Range(0, RowCount).Select(r => (RowWords(r), SideCells.RowRoom(u, u.Rect(PhoneRow(f, r)))))]);
        for (int r = 0; r < Commands; r++)
        {
            var item = _menu!.Commands[r];
            SideCells.DrawRow(this, u, u.Rect(PhoneRow(f, r)), _gothic, item.Text, item.Enabled, r == _hovered || r == _pressed,
                TagOf(item.Action), item.WeeksText is not null ? item.WeeksLeft : null, Ui, size: size);
        }
        for (int r = Commands; r < Commands + Links; r++)
            SideCells.DrawRow(this, u, u.Rect(PhoneRow(f, r)), _gothic, _menu!.Links[r - Commands].Text,
                held: r == _hovered || r == _pressed, palette: Ui, size: size);
        Ui.DrawRedButton(this, rect, end == _hovered, end == _pressed && end == _hovered, u.K);
        float tw = _gothic.GetStringSize(text, fontSize: size).X;
        DrawString(_gothic, new Vector2(rect.GetCenter().X - tw / 2, Lacquer.Baseline(rect, _gothic, size)), text, fontSize: size,
            modulate: Color.Color8(250, 226, 160));
    }

    /// <summary>Row <paramref name="r"/>'s words: a command's, a link's, Turn END's.</summary>
    private string RowWords(int r) =>
        r < Commands ? _menu!.Commands[r].Text : r < Commands + Links ? _menu!.Links[r - Commands].Text : _menu!.TurnEndText;

    /// <summary>The sheet held upright (PM1b): the map over it dimmed, Turn END and "This week" over the commands, the two columns from the foot up.</summary>
    private void DrawPortrait(PhoneFrame f, PhoneUnits u)
    {
        var sheet = Sheet(f);
        var band = PortraitBand ?? PhoneMapPortrait.StatusBand(f, 1).Band;
        DrawRect(u.Rect(new PhoneFrame.Box(f.Room.X, band.Bottom, f.Room.W, Math.Max(0, sheet.Sheet.Y - band.Bottom))), Dim);
        Ui.DrawPanel(this, u.Rect(sheet.Sheet), u.K);
        var caption = u.Rect(sheet.Caption);
        int captionSize = u.Size(32);
        DrawString(_caption, new Vector2(caption.Position.X + u.Px(6), Lacquer.Baseline(caption, _caption, captionSize)), "THIS WEEK",
            fontSize: captionSize, modulate: Ui.Subtitle);
        // The sheet's rows, Turn END among them: the game's font at one size (docs/ui/style.md, "Buttons side by side").
        int end = RowCount - 1;
        var rect = u.Rect(sheet.TurnEnd);
        string text = _menu!.TurnEndText;
        var first = u.Rect(sheet.Commands.Count > 0 ? sheet.Commands[0] : sheet.TurnEnd);
        List<(string, float)> words = [(text, SideCells.RowRoom(u, rect))];
        for (int r = 0; r < Commands; r++)
        {
            var item = _menu!.Commands[r];
            words.Add((item.Text, SideCells.LineRowRoom(u, u.Rect(sheet.Commands[r]), _gothic, TagOf(item.Action),
                item.WeeksText is not null ? item.WeeksLeft : null)));
        }
        for (int r = Commands; r < Commands + Links; r++)
            words.Add((_menu!.Links[r - Commands].Text, SideCells.LineRowRoom(u, u.Rect(sheet.Links[r - Commands]), _gothic)));
        int size = SideCells.FitAll(_gothic, u.Size(PhoneMapPortrait.RowText * SideCells.LineRowScale(u, first)), [.. words]);
        for (int r = 0; r < Commands; r++)
        {
            var item = _menu!.Commands[r];
            SideCells.DrawLineRow(this, u, u.Rect(sheet.Commands[r]), _gothic, item.Text, item.Enabled, r == _hovered || r == _pressed,
                TagOf(item.Action), item.WeeksText is not null ? item.WeeksLeft : null, Ui, size);
        }
        for (int r = Commands; r < Commands + Links; r++)
            SideCells.DrawLineRow(this, u, u.Rect(sheet.Links[r - Commands]), _gothic, _menu!.Links[r - Commands].Text,
                held: r == _hovered || r == _pressed, palette: Ui, size: size);
        Ui.DrawRedButton(this, rect, end == _hovered, end == _pressed && end == _hovered, u.K);
        float tw = _gothic.GetStringSize(text, fontSize: size).X;
        DrawString(_gothic, new Vector2(rect.GetCenter().X - tw / 2, Lacquer.Baseline(rect, _gothic, size)), text, fontSize: size,
            modulate: Color.Color8(250, 226, 160));
    }
}
