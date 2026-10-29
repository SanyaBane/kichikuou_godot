using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The dungeon's menus on the phone held landscape (docs/ui/phone-build.md, step L8; docs/ui/phone.md, §16, LF2 and LF2e):
// the tablet's rail kept in the frame, at its right foot beside window 113, at the phone's sizes — each rung's word in one
// line, its sub-line under it, the panel growing upwards; the right side stays empty. On a floor Descend in the corner, the
// floor over it, Ascend, Retreat on top, a way not offered left out in its place; before a search Start Search in Descend's
// place, the dungeon's other items over it, Return on top. Drawn in the frame's layout pixels over the whole screen
// (PhoneUnits), the places PhoneLadder's.
internal sealed partial class FloorLadderView
{
    private PhoneFrame? _phone;

    private Rect2 PhonePanelRect(PhoneFrame f)
    {
        if (f.IsUpright)
            return UprightRailRect(f);
        var u = new PhoneUnits(f);
        return u.Rect(_entry is { } e ? PhoneLadder.EntryPanel(f, e.Others.Count) : PhoneLadder.LadderPanel(f));
    }

    private Rect2 PhoneRectOf(PhoneFrame f, Part part)
    {
        if (f.IsUpright)
            return UprightRectOf(f, part);
        var u = new PhoneUnits(f);
        if (_entry is { } e)
            return u.Rect(part switch
            {
                Part.Start => PhoneLadder.Start(f),
                Part.Return => PhoneLadder.Return(f, e.Others.Count),
                _ => PhoneLadder.Other(f, part - Part.Other0, e.Others.Count),
            });
        return u.Rect(part switch
        {
            Part.Up => PhoneLadder.Way(f, down: false),
            Part.Down => PhoneLadder.Way(f, down: true),
            _ => PhoneLadder.Retreat(f),
        });
    }

    private void DrawPhone(PhoneUnits u)
    {
        if (u.Frame.IsUpright)
            DrawUprightFrame(u);
        if (_entry is { } entry)
        {
            DrawPhoneEntry(u, entry);
            return;
        }
        if (_ladder is not { } l)
            return;
        if (!u.Frame.IsUpright)                                 // upright the rungs stand on the black (PF1)
            Ui.DrawPanel(this, PanelRect, _k);
        if (l.Up is { } up)
            PhoneRung(u, Part.Up, up.Text, Caption(up), true, up.Deeper);
        var floor = u.Rect(u.Frame.IsUpright ? PhoneLadderUpright.Floor(u.Frame) : PhoneLadder.Floor(u.Frame));
        float mid = floor.GetCenter().X;
        int labelSize = u.Size(PhoneLadder.FloorLabelText), floorSize = u.Size(PhoneLadder.FloorText);
        Centred(_cinzel, labelSize, mid, floor.Position.Y + u.Px(PhoneLadder.FloorLabelMiddle), "FLOOR", Ui.Subtitle);
        Centred(_brush, floorSize, mid, floor.Position.Y + u.Px(PhoneLadder.FloorMiddle), $"{l.Floor}F", Ui.Name);
        if (l.Down is { } down)
            PhoneRung(u, Part.Down, down.Text, Caption(down), false, down.Deeper);
        var retreat = RectOf(Part.Retreat);
        Ui.DrawRedButton(this, retreat, _hovered == Part.Retreat, _pressed == Part.Retreat && _hovered == Part.Retreat, _k);
        int size = SideCells.Fit(_brush, l.RetreatText, u.Size(PhoneLadder.RetreatText), retreat.Size.X - u.Px(30));
        Centred(_brush, size, retreat.GetCenter().X, retreat.GetCenter().Y, l.RetreatText, Color.Color8(250, 226, 160));
    }

    private static string Caption(FloorWay way) => $"{(way.Deeper ? "deeper" : "back")} · to {way.To}F";

    /// <summary>Before a search: Return on top, the other items under it, Start Search in the corner framed in gold.</summary>
    private void DrawPhoneEntry(PhoneUnits u, DungeonEntryMenu entry)
    {
        if (!u.Frame.IsUpright)
            Ui.DrawPanel(this, PanelRect, _k);
        PhoneRow(u, Part.Return, entry.ReturnText, _hovered == Part.Return ? Ui.Text : Ui.Quiet);
        for (int i = 0; i < entry.Others.Count; i++)
            PhoneRow(u, Part.Other0 + i, entry.Others[i].Text, Ui.Text);
        var start = RectOf(Part.Start);
        PhoneRung(u, Part.Start, entry.StartText, "a unit goes down", false, true);
        if (_hovered != Part.Start)
            DrawRect(start, Ui.Gold, false, Lacquer.Line(2, _k));
    }

    /// <summary>A rung (ladder_rung): its arrow at the left, its word in one line over its sub-line; the way deeper in gold.</summary>
    private void PhoneRung(PhoneUnits u, Part part, string word, string sub, bool up, bool deeper)
    {
        var box = RectOf(part);
        Ui.DrawRowBox(this, box, _hovered == part, _k);
        float cy = box.GetCenter().Y, ax = box.Position.X + u.Px(PhoneLadder.ArrowIn), s = u.Px(PhoneLadder.ArrowSize);
        Vector2[] arrow = up
            ? [new(ax - s, cy + s), new(ax + s, cy + s), new(ax, cy - s)]
            : [new(ax - s, cy - s), new(ax + s, cy - s), new(ax, cy + s)];
        Lacquer.DrawSmoothPolygon(this, arrow, deeper ? Ui.Gold : Ui.Quiet, _k);
        int size = SideCells.Fit(_gothic, word, u.Size(PhoneLadder.WordText), box.Size.X - u.Px(PhoneLadder.WordRoom));
        LeftAt(_gothic, size, box.Position.X + u.Px(PhoneLadder.WordIn), cy - u.Px(PhoneLadder.WordUp), word, Ui.Text);
        LeftAt(_gothic, u.Size(PhoneLadder.SubText), box.Position.X + u.Px(PhoneLadder.SubIn), cy + u.Px(PhoneLadder.SubDown), sub,
            deeper ? Ui.Name : Ui.Quiet);
    }

    /// <summary>A plain row (Return, the dungeon's other items): its word centred.</summary>
    private void PhoneRow(PhoneUnits u, Part part, string word, Color ink)
    {
        var box = RectOf(part);
        Ui.DrawRowBox(this, box, _hovered == part, _k);
        int size = SideCells.Fit(_gothic, word, u.Size(PhoneLadder.RowText), box.Size.X - u.Px(40));
        Centred(_gothic, size, box.GetCenter().X, box.GetCenter().Y, word, ink);
    }

    private void Centred(Font font, int size, float x, float middle, string text, Color ink)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        LeftAt(font, size, x - w / 2, middle, text, ink);
    }

    private void LeftAt(Font font, int size, float x, float middle, string text, Color ink) =>
        DrawString(font, new Vector2(x, middle + (font.GetAscent(size) - font.GetDescent(size)) / 2), text, fontSize: size, modulate: ink);
}
