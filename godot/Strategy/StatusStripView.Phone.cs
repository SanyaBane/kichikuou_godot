using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The status on the phone held landscape (docs/ui/phone-build.md, step L2; docs/ui/phone.md, §12, LM1): not over the map
// but in the black sides — the date, the budget and the troops stacked under the Menu cell, the week's uses by group in
// the right side; while the command panel is open only the figures on the left side's top, while a province's menu is
// open the status alone. Drawn over the whole screen in the frame's layout pixels (PhoneUnits), at the mock-ups' type.
// Held upright (step P2; phone.md, §3, PM1) the status is a band on top, over the map, whatever is open: the date and the
// budget on its first line, the troops under them, the week's uses as tags in rows (PhoneMapUpright).
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

    /// <summary>On the phone, the strip over its parent's whole area, drawn in the sides or the band on top; false elsewhere.</summary>
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
        (_phone, Position, Size) = (f, Vector2.Zero, size);
        QueueRedraw();
        return true;
    }

    /// <summary>Whether a point (the control's coordinates, the screen's) is on what the sides show and take: the status, the week's uses.</summary>
    private bool SideHasPoint(PhoneFrame f, Vector2 point)
    {
        if (f.IsUpright)
            return UprightBand(f).Contains(point.X, point.Y);
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
        if (f.IsUpright)
        {
            DrawBand(f, u, s);
            return;
        }
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

    /// <summary>The tags' type upright (1.6 mm) and their room round the word.</summary>
    private const float UpTagText = 28, UpTagPad = 26;

    /// <summary>
    /// The status band on top held upright (PM1a), in the canvas's units: as tall as the week's uses need at the phone's
    /// type (<see cref="PhoneMapUpright.StatusBand"/>); the map lies under it.
    /// </summary>
    public PhoneFrame.Box UprightBand(PhoneFrame f) => PhoneMapUpright.StatusBand(f, PhoneMapUpright.TagRows(UprightTags(f))).Band;

    /// <summary>The week's uses' tags across the band, each group's in order (the canvas's units).</summary>
    private IReadOnlyList<IReadOnlyList<PhoneFrame.Box>> UprightTags(PhoneFrame f)
    {
        if (_strip is not { } s)
            return [];
        var u = new PhoneUnits(f);
        int size = u.Size(UpTagText);
        return PhoneMapUpright.Tags(f, [.. s.Groups.Select(g => (IReadOnlyList<float>)[.. g.Uses.Select(use =>
            (_gothic.GetStringSize(use.Text, fontSize: size).X + u.Px(UpTagPad)
                + (use.UnderWay is { } site ? UiPalette.ChipWidth(_gothic, site.WeeksLeft, size) : 0)) * u.K)])]);
    }

    /// <summary>The band (PM1a): the lacquer across, the date and the budget, the troops, the week's uses in rows.</summary>
    private void DrawBand(PhoneFrame f, PhoneUnits u, StatusStrip s)
    {
        var status = PhoneMapUpright.StatusBand(f, PhoneMapUpright.TagRows(UprightTags(f)));
        Ui.DrawPanel(this, u.Rect(status.Band), u.K);
        float left = status.Left / u.K, right = status.Right / u.K;
        var (_, budget) = s.Status.Count > 0 ? s.Status[0] : ("", "");
        int budgetSize = u.Size(38);
        float bw = _gothic.GetStringSize(budget, fontSize: budgetSize).X;
        Line(_gothic, budget, right - bw, status.BudgetTop / u.K, budgetSize, Ui.Text);
        Line(_date, s.Date, left, status.DateTop / u.K, SideCells.Fit(_date, s.Date, u.Size(38), right - bw - u.Px(30) - left), Ui.Name);
        if (s.Status.Count > 1)
        {
            string troops = $"{s.Status[1].Label} {s.Status[1].Value}".Trim();
            Line(_gothic, troops, left, status.TroopsTop / u.K, SideCells.Fit(_gothic, troops, u.Size(34), right - left), Ui.Quiet);
        }
        var tags = UprightTags(f);
        int size = u.Size(UpTagText);
        _site = null;
        for (int g = 0; g < tags.Count; g++)
            for (int t = 0; t < tags[g].Count; t++)
            {
                var use = s.Groups[g].Uses[t];
                var tag = u.Rect(tags[g][t]);
                if (use.UnderWay is { } site)
                {
                    _site = (tag, site);
                    Ui.DrawBusyTag(this, _gothic, tag, use.Text, ColourOf(use.Action), u.K, size, site.WeeksLeft);
                }
                else
                {
                    Ui.DrawTag(this, _gothic, tag, use.Text, ColourOf(use.Action), use.Free, u.K, size);
                }
            }
        if (_tip && _site is var (at, building))
            DrawBandTip(u, at, building, status.Band.Right / u.K);
    }

    /// <summary>The building under way's tooltip held upright: under its tag, over the map, kept on the screen.</summary>
    private void DrawBandTip(PhoneUnits u, Rect2 tag, BuildingUnderWay building, float right)
    {
        string? name = building.Name;
        string weeks = building.WeeksText;
        int nameSize = u.Size(34), weeksSize = u.Size(30);
        float pad = u.Px(20);
        float w = Math.Max(name is null ? 0 : _gothic.GetStringSize(name, fontSize: nameSize).X,
            _gothic.GetStringSize(weeks, fontSize: weeksSize).X) + 2 * pad;
        float h = (name is null ? 0 : u.Px(46)) + u.Px(42) + 2 * pad - u.Px(8);
        var box = new Rect2(Math.Min(tag.Position.X, right - u.Px(16) - w), tag.End.Y + u.Px(70), w, h);
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
