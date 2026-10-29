using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// Our Harem screen on the phone held landscape (docs/ui/phone-build.md, step L7; docs/ui/phone.md, §15, LH2): across the
// whole width between the hole's strips over the wave backdrop — at the left the title and the date, her picture at the
// column's width, her name, visits and last summons under it, End | Summon at the left thumb; at the right thumb the
// picker's grid folded after its ninth row, every girl at her number's cell. Drawn in the frame's layout pixels over the
// whole screen (PhoneUnits), the places PhonePickers'.
internal sealed partial class HaremView
{
    private PhoneFrame? _phone;

    /// <summary>The button at <paramref name="at"/> (the screen's units): Summon, End, or a girl's cell.</summary>
    private int PhoneButtonAt(PhoneFrame f, Vector2 at)
    {
        if (PhonePickers.Summon(f).Contains(at.X, at.Y))
            return SummonButton;
        if (PhonePickers.HaremEnd(f).Contains(at.X, at.Y))
            return EndButton;
        int cell = PhonePickers.CellAt(f, at.X, at.Y);
        return _girls.Any(g => g.Number == cell) ? cell : NoButton;
    }

    private void DrawPhone(Control c, PhoneUnits u)
    {
        var f = u.Frame;
        PhonePickerParts.DrawTitle(c, u, _brush, _cinzel, "Harem", _date);
        DrawPicture(c, u.Rect(PhonePickers.Picture(f)));
        PhoneDetails(c, u);
        PhoneGrid(c, u);
        bool hoverEnd = !Busy && _hovered == EndButton;
        PhonePickerParts.DrawButton(c, u, u.Rect(PhonePickers.HaremEnd(f)), _brush, "End", hoverEnd);
        PhoneSummon(c, u);
    }

    private void PhoneDetails(Control c, PhoneUnits u)
    {
        var (dx, dy) = PhonePickers.Details(u.Frame);
        DrawDetails(c, u, dx / u.K, dy / u.K, u.Rect(PhonePickers.Column(u.Frame)).End.X);
    }

    /// <summary>
    /// Her name by a heart at (<paramref name="x"/>, <paramref name="y"/>), then her visits, the last summons and what this
    /// visit may bring in columns at their widest values, up to <paramref name="right"/> (layout pixels).
    /// </summary>
    private void DrawDetails(Control c, PhoneUnits u, float x, float y, float right)
    {
        if (_girls.FirstOrDefault(g => g.Number == _chosen) is not { } girl)
            return;
        float heart = u.Px(PhonePickers.HeartSize);
        DrawHeart(c, new Vector2(x + u.Px(PhonePickers.HeartAt), y + u.Px(PhonePickers.HeartMiddle) - heart * 0.22f), heart, Plum);
        int name = u.Size(PhonePickers.NameText);
        c.DrawString(_brush, new Vector2(x + u.Px(PhonePickers.NameIn), PhonePickerParts.Middle(_brush, name, y + u.Px(PhonePickers.NameMiddle))),
            girl.Name, fontSize: name, modulate: Ui.Name);

        // The columns one after another at their widest values, so they never overlap and stay put from girl to girl; the
        // values as much smaller as the column's width needs.
        (string Label, string Widest)[] stats = [("Visits", "None yet"), ("Last summoned", "LP 9 · Month 12 · Week 4"), ("This visit", "Something may happen")];
        int label = u.Size(PhonePickers.LabelText), value = u.Size(PhonePickers.ValueText);
        float room = right - x, gap = u.Px(PhonePickers.StatGap);
        float Width(int i, int size) => Math.Max(_cinzel.GetStringSize(stats[i].Label, fontSize: label).X, _gothic.GetStringSize(stats[i].Widest, fontSize: size).X);
        while (value > 8 && stats.Select((_, i) => Width(i, value)).Sum() + 2 * gap > room)
            value--;
        string?[] values = [girl.Visits == 0 ? "None yet" : girl.Visits.ToString(), HaremRoster.DateText(girl.LastWeek) ?? "Never", HaremRoster.VisitText(girl.Mark)];
        float sx = x;
        for (int i = 0; i < stats.Length; i++)
        {
            if (values[i] is { } shown)
            {
                c.DrawString(_cinzel, new Vector2(sx, y + u.Px(PhonePickers.LabelTop) + _cinzel.GetAscent(label)), stats[i].Label, fontSize: label,
                    modulate: Ui.Label);
                c.DrawString(_gothic, new Vector2(sx, y + u.Px(PhonePickers.ValueTop) + _gothic.GetAscent(value)), shown, fontSize: value,
                    modulate: i == 2 ? MarkColour(girl.Mark) ?? Ui.Quiet : Ui.Value);
            }
            sx += Width(i, value) + gap;
        }
    }

    private void PhoneSummon(Control c, PhoneUnits u) => DrawSummon(c, u, u.Rect(PhonePickers.Summon(u.Frame)));

    /// <summary>Summon in red lacquer in <paramref name="box"/> with her name, her mark's pill at its top right.</summary>
    private void DrawSummon(Control c, PhoneUnits u, Rect2 box)
    {
        if (_girls.FirstOrDefault(g => g.Number == _chosen) is not { } girl)
            return;
        bool hover = Enabled(SummonButton) && _hovered == SummonButton;
        Ui.DrawRedButton(c, box, hover, hover && _pressed == SummonButton, _k);
        string word = $"Summon {girl.Name}";
        int size = SideCells.Fit(_brush, word, u.Size(PhonePickers.SummonText), box.Size.X - u.Px(30));
        CentredString(c, _brush, word, size, Color.Color8(250, 226, 160), box.GetCenter().X, PhonePickerParts.Middle(_brush, size, box.GetCenter().Y));
        if (PickerMarks.Label(girl.Mark) is { } pill)
            Ui.DrawPill(c, _cinzel, box.End.X - u.Px(20), box.Position.Y + u.Px(38), pill, MarkColour(girl.Mark)!.Value, u.Size(26), u.Px(40));
    }

    /// <summary>
    /// The folded grid: rows 1-9 at the left, 10-17 at the right, the gold rule between; a girl's cell her heart and her name
    /// (Cinzel, shrunk where long), her visit's mark at its top right, the chosen one lit in gold; the others' cells empty.
    /// </summary>
    private void PhoneGrid(Control c, PhoneUnits u)
    {
        var offered = _girls.ToDictionary(g => g.Number);
        for (int n = 1; n <= Cells; n++)
        {
            var r = u.Rect(PhonePickers.Cell(u.Frame, n));
            if (!offered.TryGetValue(n, out var girl))
            {
                PhonePickerParts.DrawEmptyCell(c, r, _k);
                continue;
            }
            DrawGirlCell(c, u, r, girl, PhonePickers.CellHeartIn, PhonePickers.CellNameIn);
        }
        PhonePickerParts.DrawRule(c, u);
    }

    /// <summary>
    /// A girl's cell at the phone's sizes: her heart <paramref name="heartIn"/> and her name <paramref name="nameIn"/>
    /// reference pixels in from its left (Cinzel, shrunk where long), her visit's mark at its top right, lit in gold when chosen.
    /// </summary>
    private void DrawGirlCell(Control c, PhoneUnits u, Rect2 r, HaremGirl girl, float heartIn, float nameIn)
    {
        bool chosen = girl.Number == _chosen, hover = girl.Number == _hovered;
        c.DrawRect(r, chosen || hover ? Ui.RowHover : Ui.Row);
        c.DrawRect(r, chosen ? Ui.HoverLine : hover ? Ui.Gold : Ui.DimGold, false, Line(chosen ? 2 : 1));
        float mid = r.GetCenter().Y, heart = u.Px(PhonePickers.CellHeart);
        DrawHeart(c, new Vector2(r.Position.X + u.Px(heartIn), mid - heart * 0.22f), heart, girl.Visits > 0 ? Plum : PlumDim);
        float nx = r.Position.X + u.Px(nameIn);
        int size = SideCells.Fit(_cinzel, girl.Name, u.Size(PhonePickers.CellNameText), r.End.X - u.Px(PhonePickers.CellRight) - nx);
        c.DrawString(_cinzel, new Vector2(nx, PhonePickerParts.Middle(_cinzel, size, mid)), girl.Name, fontSize: size,
            modulate: chosen || hover ? Ui.Name : Ui.Text);
        if (PickerMarks.Label(girl.Mark) is { } mark)
        {
            int markSize = u.Size(PhonePickers.CellMarkText);
            float mw = _cinzel.GetStringSize(mark, fontSize: markSize).X;
            c.DrawString(_cinzel, new Vector2(r.End.X - u.Px(PhonePickers.CellMarkIn) - mw,
                PhonePickerParts.Middle(_cinzel, markSize, r.Position.Y + u.Px(PhonePickers.CellMarkMiddle))), mark, fontSize: markSize,
                modulate: MarkColour(girl.Mark)!.Value);
        }
    }
}
