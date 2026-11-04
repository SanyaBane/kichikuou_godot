using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// A province's menu on the phone held landscape (docs/ui/phone-build.md, step L2; docs/ui/phone.md, §12, LM1c): the
// province ringed, its card beside it in the frame at the phone's type (the city's picture, the name and the army on one
// line, the figures a line each, the label left and the figure right), its commands in the right side from the foot up as
// the command panel's rows, Return at the foot where Commands stands; the dungeon's menu keeps its place's caption over
// the rows. Past what the side holds (seven: Leazas Castle's places) the commands stand in two columns in a panel at the
// frame's right edge, the card left of it (the owner's PE, docs/ui/touch.md, "Fifteen commands"). A tap off the card, the
// ring, the panel and the rows cancels, as on the tablet. Over the whole screen in the frame's
// layout pixels (PhoneUnits); the places are PhoneMap's. Held upright (step P2; phone.md, §3, PM1c) the card and the
// commands rise as a sheet over the map's foot — the city's picture at its top left, the name, the army and the figures
// beside it, the commands across it from the foot up — Return at the bar's right, and the map is drawn higher so the
// ringed province stays over the sheet (PhoneMapPortrait.Province, PortraitMapView.LiftFor).
internal sealed partial class ProvinceMenuView
{
    private PhoneFrame? _phone;                         // the phone: the rail in the right side, or the sheet upright
    private (int X, int Y)? _mapAt;                     // upright: the tapped province's map point

    /// <summary>The map held upright, which the sheet lifts and the ring is placed on; null elsewhere.</summary>
    public PortraitMapView? Map { get; set; }

    /// <summary>The status band over the map held upright (<see cref="StatusStripView.PortraitBand"/>); null for its least.</summary>
    public PhoneFrame.Box? PortraitBand { get; set; }

    private PhoneMapPortrait.ProvinceSheet Sheet(PhoneFrame f, ProvinceMenu m) =>
        PhoneMapPortrait.Province(f, PortraitBand ?? PhoneMapPortrait.StatusBand(f, 1).Band, m.Card.Count, Items, m.Place is not null);

    /// <summary>The ringed province's point held upright, in the canvas's units, on the map as drawn; null without one.</summary>
    private (float X, float Y)? PortraitPoint => _mapAt is { } p && Map is { Phone: not null } map ? map.View.FromMap(p.X, p.Y) : null;

    /// <summary>The name's least size beside the army (2.3 mm): a longer name has the army under it.</summary>
    private const float PhoneNameMin = 40;

    /// <summary>On the phone, the menu over its parent's whole area; false elsewhere.</summary>
    private bool PlacePhone()
    {
        var phone = Device.Held(this);
        if (phone is not { } f)
        {
            if (_phone is not null)
                (_phone, _k) = (null, 0);                // placed again by the caller
            return false;
        }
        if (f.IsPortrait && _menu is { } menu && _mapAt is { } p && Map is { Phone: not null } map)
        {
            var before = map.View;
            map.LiftFor(p.X, p.Y, Sheet(f, menu).Sheet.Y);
            if (map.View != before)
                QueueRedraw();
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
        PhoneMap.ProvinceCard(f, PhonePoint(f, _at), m.Card.Count, PhoneTwoLines(f, m), PhonePanel(f)?.Panel);

    /// <summary>Held landscape, past what the side holds: the commands' panel in the frame (the owner's PE); else null.</summary>
    private PhoneMap.ProvinceColumns? PhonePanel(PhoneFrame f) =>
        f.IsPortrait || _menu is not { } m ? null : PhoneMap.ProvincePanel(f, Items, m.Place is not null);

    /// <summary>The tapped province's point, unless it lies under the commands' panel (its ring and the card's triangle then left out).</summary>
    private (float X, float Y)? PhoneRingPoint(PhoneFrame f)
    {
        if (PhonePoint(f, _at) is not var (x, y))
            return null;
        float reach = f.Px(PhoneMap.RingRadius + 10);
        return PhonePanel(f)?.Panel is { } p && x > p.X - reach && x < p.Right + reach && y > p.Y - reach && y < p.Bottom + reach
            ? null : (x, y);
    }

    private IReadOnlyList<PhoneFrame.Box> PhoneRows(PhoneFrame f) =>
        f.IsPortrait && _menu is { } m ? Sheet(f, m).Rows : PhoneMap.ProvinceRows(f, Items, _menu?.Place is not null);

    /// <summary>Row <paramref name="row"/>'s box on the phone (the canvas's units): the commands from the foot up, then Return.</summary>
    private PhoneFrame.Box PhoneRow(PhoneFrame f, int row) =>
        row < Items ? PhoneRows(f)[row] : f.IsPortrait ? PhoneMapPortrait.CommandsCell(f) : PhoneMap.FootCell(f);

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
        if (f.IsPortrait)
        {
            if (PhoneRowAt(f, at) >= 0 || _menu is not { } menu || Sheet(f, menu).Sheet.Contains(at.X, at.Y))
                return false;
            return PortraitPoint is not var (ux, uy) || new Vector2(ux, uy).DistanceTo(at) > f.Px(PhoneMapPortrait.RingRadius) + f.Px(8);
        }
        if (PhoneRowAt(f, at) >= 0 || _menu is not { } m || PhoneCard(f, m).Box.Contains(at.X, at.Y)
            || PhonePanel(f)?.Panel.Contains(at.X, at.Y) == true)
            return false;
        return PhoneRingPoint(f) is not var (x, y) || new Vector2(x, y).DistanceTo(at) > f.Px(PhoneMap.RingRadius) + f.Px(8);
    }

    /// <summary>A point of the frame off the card and the ring, global: where unattended play taps to close the menu.</summary>
    private Vector2 PhoneOutsidePoint(PhoneFrame f)
    {
        if (f.IsPortrait && _menu is { } menu)
        {
            // Portrait: the map over the sheet, a corner of it away from the ring.
            float top = (PortraitBand ?? PhoneMapPortrait.StatusBand(f, 1).Band).Bottom, foot = Sheet(f, menu).Sheet.Y, margin = f.Px(80);
            Vector2[] places = [new(f.Room.X + margin, foot - margin), new(f.Room.Right - margin, foot - margin),
                new(f.Room.X + margin, top + margin), new(f.Room.Right - margin, top + margin)];
            return GetGlobalRect().Position + places.FirstOrDefault(p => PhoneOutside(f, p), places[0]);
        }
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
        if (f.IsPortrait)
        {
            DrawPortrait(f, u, m);
            return;
        }
        bool two = PhoneTwoLines(f, m);
        var columns = PhonePanel(f);
        var card = PhoneMap.ProvinceCard(f, PhonePoint(f, _at), m.Card.Count, two, columns?.Panel);
        var box = u.Rect(card.Box);
        // The ring round the province and the card's triangle pointing at it, unless the commands' panel covers it.
        Vector2? at = PhoneRingPoint(f) is var (px, py) ? new Vector2(px, py) / u.K : null;
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
        // The commands from the foot up (past what the side holds, in the frame's panel down two columns, the names at
        // the phone's type: 2.3 mm), then Return at the foot.
        if (columns is not null)
            Ui.DrawPanel(this, u.Rect(columns.Panel), u.K);
        // The rows: the game's font at one size (docs/ui/style.md, "Buttons side by side"), shrunk together.
        float scale = Items > 0 ? SideCells.RowScale(u, u.Rect(rows[0])) : 1;
        int rowSize = SideCells.FitAll(_gothic, u.Size((columns is null ? PhoneMap.RowText : PhoneMap.ColumnsRowText) * scale),
            [.. Enumerable.Range(0, Items).Select(r => (m.Items[r].Text, SideCells.RowRoom(u, u.Rect(rows[r]))))]);
        for (int r = 0; r < Items; r++)
        {
            var item = m.Items[r];
            SideCells.DrawRow(this, u, u.Rect(rows[r]), _gothic, item.Text, item.Enabled, r == _hovered || r == _pressed,
                TagOf(item.Action), palette: Ui, size: rowSize);
        }
        int back = RowCount - 1;
        SideCells.DrawButton(this, u, u.Rect(PhoneMap.FootCell(f)), SideCells.Icon.Close, "Return",
            back == _pressed || back == _hovered, Ui);
    }

    /// <summary>
    /// The sheet held upright (PM1c): the ring round the province on the map, the city's picture, the name, the army and
    /// the figures beside it, the place's caption (the dungeon's), the commands from the foot up, Return at the bar's right.
    /// </summary>
    private void DrawPortrait(PhoneFrame f, PhoneUnits u, ProvinceMenu m)
    {
        var sheet = Sheet(f, m);
        if (PortraitPoint is var (px, py))
        {
            var p = new Vector2(px, py) / u.K;
            DrawArc(p, u.Px(PhoneMapPortrait.RingRadius + 8), 0, MathF.Tau, 64, Ui.HoverLine, Lacquer.Line(u.Px(2), u.K));
            DrawArc(p, u.Px(PhoneMapPortrait.RingRadius), 0, MathF.Tau, 64, Ui.Gold, Lacquer.Line(u.Px(6), u.K));
        }
        Ui.DrawPanel(this, u.Rect(sheet.Sheet), u.K);
        var picture = u.Rect(sheet.Picture);
        if (_picture is not null)
            DrawTextureRect(_picture, picture, false);
        DrawRect(picture, Ui.Gold, false, Lacquer.Line(u.Px(3), u.K));
        float x0 = sheet.TextLeft / u.K, x1 = sheet.TextRight / u.K;
        int nameSize = SideCells.Fit(_brush, m.Name, u.Size(64), x1 - x0);
        DrawString(_brush, new Vector2(x0, sheet.NameTop / u.K + _brush.GetAscent(nameSize)), m.Name, fontSize: nameSize, modulate: Ui.Name);
        int armySize = SideCells.Fit(_cinzel, m.Army, u.Size(30), x1 - x0);
        DrawString(_cinzel, new Vector2(x0, sheet.ArmyTop / u.K + _cinzel.GetAscent(armySize)), m.Army, fontSize: armySize,
            modulate: Ui.Subtitle);
        int size = u.Size(32);
        for (int i = 0; i < m.Card.Count; i++)
        {
            string line = m.Card[i];
            int colon = line.IndexOf(": ", StringComparison.Ordinal);
            string label = colon < 0 ? "" : line[..colon], value = colon < 0 ? line : line[(colon + 2)..];
            var row = new Rect2(x0, (sheet.FiguresTop + i * sheet.FigurePitch) / u.K, x1 - x0, sheet.FigurePitch / u.K);
            float lw = _gothic.GetStringSize(label, fontSize: size).X;
            int valueSize = SideCells.Fit(_gothic, value, size, x1 - x0 - lw - u.Px(24));
            DrawString(_gothic, new Vector2(x0, Lacquer.Baseline(row, _gothic, size)), label, fontSize: size, modulate: Ui.Label);
            float vw = _gothic.GetStringSize(value, fontSize: valueSize).X;
            DrawString(_gothic, new Vector2(x1 - vw, Lacquer.Baseline(row, _gothic, valueSize)), value, fontSize: valueSize,
                modulate: value == "??" ? Ui.Label : Ui.Value);
        }
        if (m.Place is { } place && sheet.Place is { } placeBox)
        {
            var band = u.Rect(placeBox);
            int placeSize = SideCells.Fit(_cinzel, place, u.Size(34), band.Size.X - u.Px(80));
            float pw = _cinzel.GetStringSize(place, fontSize: placeSize).X, mid = band.GetCenter().X, cy = band.GetCenter().Y;
            DrawString(_cinzel, new Vector2(mid - pw / 2, cy + (_cinzel.GetAscent(placeSize) - _cinzel.GetDescent(placeSize)) / 2), place,
                fontSize: placeSize, modulate: Ui.Name);
            DrawLine(new Vector2(band.Position.X, cy), new Vector2(mid - pw / 2 - u.Px(16), cy), Ui.DimGold, Lacquer.Line(u.Px(2), u.K));
            DrawLine(new Vector2(mid + pw / 2 + u.Px(16), cy), new Vector2(band.End.X, cy), Ui.DimGold, Lacquer.Line(u.Px(2), u.K));
        }
        // The rows: the game's font at one size (docs/ui/style.md, "Buttons side by side"), shrunk together.
        float scale = Items > 0 ? SideCells.LineRowScale(u, u.Rect(sheet.Rows[0])) : 1;
        int rowSize = SideCells.FitAll(_gothic, u.Size(PhoneMapPortrait.RowText * scale),
            [.. Enumerable.Range(0, Items).Select(r => (m.Items[r].Text,
                SideCells.LineRowRoom(u, u.Rect(sheet.Rows[r]), _gothic, TagOf(m.Items[r].Action))))]);
        for (int r = 0; r < Items; r++)
        {
            var item = m.Items[r];
            SideCells.DrawLineRow(this, u, u.Rect(sheet.Rows[r]), _gothic, item.Text, item.Enabled, r == _hovered || r == _pressed,
                TagOf(item.Action), palette: Ui, size: rowSize);
        }
        int back = RowCount - 1;
        SideCells.DrawBarButton(this, u, u.Rect(PhoneMapPortrait.CommandsCell(f)), SideCells.Icon.Close, "Return",
            back == _pressed || back == _hovered, Ui);
    }
}
