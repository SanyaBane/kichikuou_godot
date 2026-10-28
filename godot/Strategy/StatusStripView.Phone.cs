using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The status on the phone held landscape (docs/ui/phone-build.md, step L2; docs/ui/phone.md, §12, LM1): not over the map
// but in the black sides — the date, the budget and the troops stacked under the Menu cell, the week's uses by group in
// the right side; while the command panel is open only the figures on the left side's top, while a province's menu is
// open the status alone. Drawn over the whole screen in the frame's layout pixels (PhoneUnits), at the mock-ups' type.
internal sealed partial class StatusStripView
{
    /// <summary>What the sides hold besides the status: what is open over the map.</summary>
    public enum SideMode
    {
        /// <summary>The map's wait: the status under the Menu cell, the week's uses in the right side (LM1a).</summary>
        Map,

        /// <summary>A province's menu: the status under the Menu cell, its commands in the right side (LM1c).</summary>
        Province,

        /// <summary>The command panel: the figures on the left side's top, the panel's rails under and beside them (LM1b).</summary>
        Commands,
    }

    private PhoneFrame? _phone;                         // the phone held landscape: drawn in the sides
    private SideMode _mode;

    /// <summary>What the sides hold on the phone besides the status.</summary>
    public SideMode Mode
    {
        get => _mode;
        set
        {
            if (value == _mode)
                return;
            _mode = value;
            SetTip(false);
            QueueRedraw();
        }
    }

    /// <summary>On the phone held landscape, the strip over its parent's whole area, drawn in the sides; false elsewhere.</summary>
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

    /// <summary>Whether a point (the control's coordinates, the screen's) is on what the sides show and take: the status, the week's uses.</summary>
    private bool SideHasPoint(PhoneFrame f, Vector2 point)
    {
        if (_mode == SideMode.Commands)
            return false;
        if (PhoneMap.StatusBox(f).Contains(point.X, point.Y))
            return true;
        if (_mode != SideMode.Map || _strip is not { } s)
            return false;
        var groups = PhoneMap.Tags(f, [.. s.Groups.Select(g => g.Uses.Count)]);
        if (groups.Count == 0)
            return false;
        float bottom = groups.Max(g => g.Tags.Count > 0 ? g.Tags[^1].Bottom : g.Caption.Bottom);
        var first = groups[0].Caption;
        return new PhoneFrame.Box(first.X, first.Y, first.W, bottom - first.Y).Contains(point.X, point.Y);
    }

    private void DrawSide(PhoneFrame f, StatusStrip s)
    {
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        int dot = s.Date.IndexOf(" · ", StringComparison.Ordinal);
        string year = dot < 0 ? s.Date : s.Date[..dot], month = dot < 0 ? "" : s.Date[(dot + 3)..];
        var (_, budget) = s.Status.Count > 0 ? s.Status[0] : ("", "");
        _site = null;
        if (_mode == SideMode.Commands)
        {
            // LM1b: the date and the budget on the left's top, the week's commands under them (the panel's).
            var head = u.Rect(PhoneMap.HeaderBox(f));
            float hx = head.Position.X + u.Px(6), hw = head.Size.X - u.Px(12), hy = head.Position.Y;
            Line(_date, year, hx, hy, u.Size(30), Ui.Name);
            Line(_date, month, hx, hy + u.Px(40), SideCells.Fit(_date, month, u.Size(28), hw), Ui.Name);
            Line(_gothic, budget, hx, hy + u.Px(86), SideCells.Fit(_gothic, budget, u.Size(34), hw), Ui.Text);
            return;
        }
        // LM1a: the date, the budget's figure and its unit, the troops, stacked under the Menu cell.
        var box = u.Rect(PhoneMap.StatusBox(f));
        float x = box.Position.X + u.Px(6), w = box.Size.X - u.Px(12), y = box.Position.Y;
        Line(_date, year, x, y, u.Size(34), Ui.Name);
        Line(_date, month, x, y + u.Px(46), SideCells.Fit(_date, month, u.Size(30), w), Ui.Name);
        int space = budget.LastIndexOf(' ');
        bool unit = space > 0 && !budget[(space + 1)..].Any(char.IsDigit);
        string figure = unit ? budget[..space] : budget;
        Line(_gothic, figure, x, y + u.Px(104), SideCells.Fit(_gothic, figure, u.Size(42), w), Ui.Text);
        if (unit)
            Line(_cinzel, budget[(space + 1)..], x, y + u.Px(154), u.Size(24), Ui.Subtitle);
        if (s.Status.Count > 1)
        {
            string troops = $"{s.Status[1].Label} {s.Status[1].Value}".Trim();
            Line(_gothic, troops, x, y + u.Px(196), SideCells.Fit(_gothic, troops, u.Size(32), w), Ui.Quiet);
        }
        if (_mode != SideMode.Map)
            return;
        // The week's uses as the strip's tags by group in the right side.
        var groups = PhoneMap.Tags(f, [.. s.Groups.Select(g => g.Uses.Count)]);
        int tagSize = u.Size(28);
        for (int g = 0; g < groups.Count; g++)
        {
            var caption = u.Rect(groups[g].Caption);
            Line(_date, s.Groups[g].Caption, caption.Position.X, caption.Position.Y, u.Size(22), Ui.Subtitle);
            for (int t = 0; t < groups[g].Tags.Count; t++)
            {
                var use = s.Groups[g].Uses[t];
                var tag = u.Rect(groups[g].Tags[t]);
                if (use.UnderWay is { } site)
                {
                    _site = (tag, site);
                    Ui.DrawBusyTag(this, _gothic, tag, use.Text, ColourOf(use.Action), u.K, tagSize, site.WeeksLeft);
                }
                else
                {
                    Ui.DrawTag(this, _gothic, tag, use.Text, ColourOf(use.Action), use.Free, u.K, tagSize);
                }
            }
        }
        if (_tip && _site is var (at, building))
            DrawSideTip(u, at, building);
    }

    /// <summary>A line of text whose top is at <paramref name="y"/> (the mock-ups' anchor).</summary>
    private void Line(Font font, string text, float x, float y, int size, Color colour) =>
        DrawString(font, new Vector2(x, y + font.GetAscent(size)), text, fontSize: size, modulate: colour);

    /// <summary>
    /// The building under way's tooltip on the phone: its name and the weeks left at the phone's type, left of its tag in the
    /// frame (the side has no room for it).
    /// </summary>
    private void DrawSideTip(PhoneUnits u, Rect2 tag, BuildingUnderWay building)
    {
        string? name = building.Name;
        string weeks = building.WeeksText;
        int nameSize = u.Size(34), weeksSize = u.Size(30);
        float pad = u.Px(20);
        float w = Math.Max(name is null ? 0 : _gothic.GetStringSize(name, fontSize: nameSize).X,
            _gothic.GetStringSize(weeks, fontSize: weeksSize).X) + 2 * pad;
        float h = (name is null ? 0 : u.Px(46)) + u.Px(42) + 2 * pad - u.Px(8);
        var box = new Rect2(tag.Position.X - u.Px(18) - w, tag.Position.Y, w, h);
        DrawRect(new Rect2(box.Position + new Vector2(3, 4), box.Size), new Color(0, 0, 0, 0.3f));
        DrawRect(box, Ui.CalloutFill);
        DrawRect(box, Ui.Gold, false, Lacquer.Line(1, u.K));
        DrawRect(box.Grow(-3), Ui.DimGold, false, Lacquer.Line(1, u.K));
        float y = box.Position.Y + pad - u.Px(4);
        if (name is not null)
        {
            Line(_gothic, name, box.Position.X + pad, y, nameSize, Ui.Name);
            y += u.Px(46);
        }
        Line(_gothic, weeks, box.Position.X + pad, y, weeksSize, Ui.Quiet);
    }
}
