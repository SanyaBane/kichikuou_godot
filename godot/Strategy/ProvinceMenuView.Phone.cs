using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// A province's menu on the phone held landscape (docs/ui/phone-build.md, step L2; docs/ui/phone.md, §12, LM1c): the
// province ringed, its card beside it in the frame at the phone's type (the city's picture, the name and the army on one
// line, the figures a line each, the label left and the figure right), its commands in the right side from the foot up as
// the command panel's rows, Return at the foot where Commands stands; the dungeon's menu keeps its place's caption over
// the rows. A tap off the card, the ring and the rows cancels, as on the tablet. Over the whole screen in the frame's
// layout pixels (PhoneUnits); the places are PhoneMap's.
internal sealed partial class ProvinceMenuView
{
    private PhoneFrame? _phone;                         // the phone held landscape: the rail in the right side

    /// <summary>The name's least size beside the army (2.3 mm): a longer name has the army under it.</summary>
    private const float PhoneNameMin = 40;

    /// <summary>On the phone held landscape, the menu over its parent's whole area; false elsewhere.</summary>
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

    /// <summary>The tapped province's point in the canvas's units; null when none.</summary>
    private static (float X, float Y)? PhonePoint(PhoneFrame f, Vector2? at) =>
        at is { } p ? (f.Frame.X + p.X * f.Scale, f.Frame.Y + p.Y * f.Scale) : null;

    private int Items => Math.Max(0, RowCount - 1);

    /// <summary>Whether the name and the army take a line each: the name would be below <see cref="PhoneNameMin"/> beside the army.</summary>
    private bool PhoneTwoLines(PhoneFrame f, ProvinceMenu m)
    {
        var u = new PhoneUnits(f);
        float room = u.Px(512) - _cinzel.GetStringSize(m.Army, fontSize: u.Size(26)).X - u.Px(20);
        return _brush.GetStringSize(m.Name, fontSize: u.Size(PhoneNameMin)).X > room;
    }

    private PhoneMap.Card PhoneCard(PhoneFrame f, ProvinceMenu m) =>
        PhoneMap.ProvinceCard(f, PhonePoint(f, _at), m.Card.Count, PhoneTwoLines(f, m));

    private IReadOnlyList<PhoneFrame.Box> PhoneRows(PhoneFrame f) => PhoneMap.ProvinceRows(f, Items, _menu?.Place is not null);

    /// <summary>Row <paramref name="row"/>'s box on the phone (the canvas's units): the commands from the foot up, then Return.</summary>
    private PhoneFrame.Box PhoneRow(PhoneFrame f, int row) => row < Items ? PhoneRows(f)[row] : PhoneMap.FootCell(f);

    private int PhoneRowAt(PhoneFrame f, Vector2 at)
    {
        for (int r = 0; r < RowCount; r++)
            if (PhoneRow(f, r).Contains(at.X, at.Y))
                return r;
        return -1;
    }

    /// <summary>A tap at <paramref name="at"/> (the screen's) is off the card, the ring and the rows: it cancels.</summary>
    private bool PhoneOutside(PhoneFrame f, Vector2 at)
    {
        if (PhoneRowAt(f, at) >= 0 || _menu is not { } m || PhoneCard(f, m).Box.Contains(at.X, at.Y))
            return false;
        return PhonePoint(f, _at) is not var (x, y) || new Vector2(x, y).DistanceTo(at) > f.Px(PhoneMap.RingRadius) + f.Px(8);
    }

    /// <summary>A point of the frame off the card and the ring, global: where unattended play taps to close the menu.</summary>
    private Vector2 PhoneOutsidePoint(PhoneFrame f)
    {
        var frame = f.Frame;
        float m = f.Px(80);
        Vector2[] candidates = [new(frame.X + m, frame.Bottom - m), new(frame.X + m, frame.Y + m), new(frame.Right - m, frame.Y + m),
            new(frame.X + frame.W / 2, frame.Bottom - m)];
        var point = candidates.FirstOrDefault(p => PhoneOutside(f, p), candidates[0]);
        return GetGlobalRect().Position + point;
    }

    private void DrawPhone(PhoneFrame f, ProvinceMenu m)
    {
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        bool two = PhoneTwoLines(f, m);
        var card = PhoneMap.ProvinceCard(f, PhonePoint(f, _at), m.Card.Count, two);
        var box = u.Rect(card.Box);
        // The ring round the province and the card's triangle pointing at it.
        Vector2? at = PhonePoint(f, _at) is var (px, py) ? new Vector2(px, py) / u.K : null;
        if (at is { } p)
        {
            DrawArc(p, u.Px(PhoneMap.RingRadius + 8), 0, MathF.Tau, 64, Ui.HoverLine, Lacquer.Line(u.Px(2), u.K));
            DrawArc(p, u.Px(PhoneMap.RingRadius), 0, MathF.Tau, 64, Ui.Gold, Lacquer.Line(u.Px(6), u.K));
        }
        Ui.DrawPanel(this, box, u.K);
        if (at is { } q)
        {
            bool left = box.End.X <= q.X;
            float edge = left ? box.End.X - 1 : box.Position.X + 1;
            float tip = left ? q.X - u.Px(PhoneMap.RingRadius + 10) : q.X + u.Px(PhoneMap.RingRadius + 10);
            float cy = Math.Clamp(q.Y, box.Position.Y + u.Px(40), box.End.Y - u.Px(40));
            Vector2[] triangle = [new(edge, cy - u.Px(30)), new(tip, q.Y), new(edge, cy + u.Px(30))];
            DrawColoredPolygon(triangle, Ui.Panel with { A = 1 });
            DrawPolyline(triangle, Ui.Gold, Lacquer.Line(u.Px(3), u.K));
        }
        // The city's picture.
        var picture = u.Rect(card.Picture);
        if (_picture is not null)
            DrawTextureRect(_picture, picture, false);
        DrawRect(picture, Ui.Gold, false, Lacquer.Line(u.Px(3), u.K));
        // The name and the army: on one line, the army at the right; a long name has the army under it.
        float x0 = picture.Position.X, x1 = picture.End.X, nameTop = card.NameTop / u.K;
        int armySize = u.Size(26);
        float armyWidth = _cinzel.GetStringSize(m.Army, fontSize: armySize).X;
        int nameSize = SideCells.Fit(_brush, m.Name, u.Size(52), two ? x1 - x0 : x1 - x0 - armyWidth - u.Px(20));
        DrawString(_brush, new Vector2(x0, nameTop + _brush.GetAscent(nameSize)), m.Name, fontSize: nameSize, modulate: Ui.Name);
        float armyTop = two ? nameTop + u.Px(64) : nameTop + u.Px(20);
        DrawString(_cinzel, new Vector2(two ? x0 : x1 - armyWidth, armyTop + _cinzel.GetAscent(armySize)), m.Army,
            fontSize: armySize, modulate: Ui.Subtitle);
        // The figures, a line each: the label left, the figure right.
        int size = u.Size(34);
        for (int i = 0; i < m.Card.Count; i++)
        {
            string line = m.Card[i];
            int colon = line.IndexOf(": ", StringComparison.Ordinal);
            string label = colon < 0 ? "" : line[..colon], value = colon < 0 ? line : line[(colon + 2)..];
            var row = new Rect2(x0, (card.FiguresTop + i * card.FigurePitch) / u.K, x1 - x0, card.FigurePitch / u.K);
            float lw = _gothic.GetStringSize(label, fontSize: size).X;
            int valueSize = SideCells.Fit(_gothic, value, size, x1 - x0 - lw - u.Px(24));
            DrawString(_gothic, new Vector2(x0, Lacquer.Baseline(row, _gothic, size)), label, fontSize: size, modulate: Ui.Label);
            float vw = _gothic.GetStringSize(value, fontSize: valueSize).X;
            DrawString(_gothic, new Vector2(x1 - vw, Lacquer.Baseline(row, _gothic, valueSize)), value, fontSize: valueSize,
                modulate: value == "??" ? Ui.Label : Ui.Value);
        }
        // The place a later menu belongs to (the dungeon) between gold rules over the rows.
        var rows = PhoneRows(f);
        if (m.Place is { } place)
        {
            var band = u.Rect(PhoneMap.PlaceBox(f, rows));
            int placeSize = SideCells.Fit(_cinzel, place, u.Size(30), band.Size.X - u.Px(60));
            float pw = _cinzel.GetStringSize(place, fontSize: placeSize).X, mid = band.GetCenter().X, cy = band.GetCenter().Y;
            DrawString(_cinzel, new Vector2(mid - pw / 2, cy + (_cinzel.GetAscent(placeSize) - _cinzel.GetDescent(placeSize)) / 2), place,
                fontSize: placeSize, modulate: Ui.Name);
            DrawLine(new Vector2(band.Position.X, cy), new Vector2(mid - pw / 2 - u.Px(12), cy), Ui.DimGold, Lacquer.Line(u.Px(2), u.K));
            DrawLine(new Vector2(mid + pw / 2 + u.Px(12), cy), new Vector2(band.End.X, cy), Ui.DimGold, Lacquer.Line(u.Px(2), u.K));
        }
        // The commands from the foot up, then Return at the foot.
        for (int r = 0; r < Items; r++)
        {
            var item = m.Items[r];
            SideCells.DrawRow(this, u, u.Rect(rows[r]), _gothic, item.Text, item.Enabled, r == _hovered || r == _pressed,
                TagOf(item.Action), palette: Ui);
        }
        int back = RowCount - 1;
        SideCells.DrawButton(this, u, u.Rect(PhoneMap.FootCell(f)), SideCells.Icon.Close, "Return",
            back == _pressed || back == _hovered, Ui);
    }
}
