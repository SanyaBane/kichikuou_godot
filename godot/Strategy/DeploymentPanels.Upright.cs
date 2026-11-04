using Godot;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The deployment table on the phone held upright (docs/ui/phone-build.md, step P5; docs/ui/phone.md, §7 and §20, PD2): the
// title band, the party under it as the battle shows our army (the back row on top, then the front row, facing down, held
// from below in the bracket whose head points down — the owner's PD2f2, §32; each card with its ✕), the troops and the
// war expenses beside the head, then the ledger's tabs and the ledger at the thumb
// (GameScreen places it, two-line rows scrolled sideways), Attack | Stop in the bar. A unit's picture comes by a long press
// on its row (UnitPictureView). Drawn in the frame's layout pixels over the whole screen (PhoneUnits), the places
// PhoneLedgerUpright's.
internal sealed partial class DeploymentPanels
{
    private void DrawUpright(Control c, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        if (Table is not null)
            _tabs.Draw(c, _k, Table);
        if (_party is not { } party)
            return;
        var band = Box(PhoneLedgerUpright.Band(f));
        DrawBand(c, u, band, "Deployment", Subtitle(party), _date);

        var panel = Box(PhoneLedgerUpright.Party(f));
        Ui.DrawPanel(c, panel, _k);
        float x0 = panel.Position.X + u.Px(PhoneLedgerUpright.HeadingX), x1 = panel.End.X - u.Px(PhoneLedgerUpright.BudgetRight);
        float mid = panel.Position.Y + u.Px(PhoneLedgerUpright.HeadingMiddle);
        LeftAt(c, _brush, "Party", u.Size(PhoneLedgerUpright.HeadingText), Ui.Name, x0, mid);
        int label = u.Size(PhoneLedgerUpright.LabelText), figure = u.Size(PhoneLedgerUpright.BudgetText);
        float x = RightAt(c, _cinzelLight, "GOLD", label, Ui.Subtitle, x1, mid) - u.Px(12);
        x = RightAt(c, _cinzel, Figures.GoldShort(party.Gold * 100), figure, Ui.Name, x, mid) - u.Px(14);
        RightAt(c, _cinzelLight, "Budget", label, Ui.Subtitle, x, mid);

        int caption = u.Size(PhoneLedgerUpright.CaptionText);
        float back = PhoneLedgerUpright.CaptionMiddle(f, front: false) / _k, front = PhoneLedgerUpright.CaptionMiddle(f, front: true) / _k;
        if (party.Places.Count > 2)                             // places 3 and 4 are the back row's
            CentredMiddle(c, _cinzel, "Back row", caption, Ui.Label, panel.GetCenter().X, back);
        // The way the party faces: down, as our army in the battle (PB3); "Front row" inside the bracket (PD2f2).
        var (left, right, middle, line, cards) = PhoneLedgerUpright.Bracket(f, party.Places.Count);
        CentredMiddle(c, _cinzel, "Front row", caption, Ui.Label, middle / _k, front);
        FacingBracket.Draw(c, u, Ui, _k, left, right, middle, line, cards, down: true);

        for (int place = 1; place <= party.Places.Count; place++)
            DrawPhoneCard(c, u, place, party.Places[place - 1]);
        if (_dropOn != 0)
            DrawDrop(c, _dropOn);

        float totals = panel.Position.Y + u.Px(PhoneLedgerUpright.TotalsMiddle);
        int totalsLabel = u.Size(PhoneLedgerUpright.TotalsLabel), totalsText = u.Size(PhoneLedgerUpright.TotalsText);
        LeftAt(c, _cinzelLight, "Troops", totalsLabel, Ui.Subtitle, panel.Position.X + u.Px(PhoneLedgerUpright.TotalsX), totals);
        LeftAt(c, _gothic, Figures.Number(party.Troops), totalsText, Ui.Value, panel.Position.X + u.Px(PhoneLedgerUpright.TotalsFigureX), totals);
        float expenses = RightAt(c, _gothic, Figures.GoldShort(party.Cost * 100), totalsText, party.Affordable ? Ui.Value : Ui.Minus,
            panel.End.X - u.Px(PhoneLedgerUpright.TotalsRight), totals);
        RightAt(c, _cinzelLight, "War expenses", totalsLabel, Ui.Subtitle, expenses - u.Px(16), totals);

        DrawPhoneBar(c, u, Figures.GoldShort(party.Cost * 100));
    }

    /// <summary>
    /// The title band held upright (<c>title_band</c>; Subordinates' too): the title in the brush at the left, the date at the
    /// right, the subtitle under them, a divider.
    /// </summary>
    internal static void DrawBand(Control c, PhoneUnits u, Rect2 band, string title, string subtitle, string date)
    {
        FontFile brush = SideCells.Brush;
        var cinzel = Lacquer.Cinzel(700);
        var light = Lacquer.Cinzel(600);
        float top = band.Position.Y;
        int titleSize = u.Size(PhoneLedgerUpright.TitleText), dateSize = u.Size(PhoneLedgerUpright.DateText),
            subSize = u.Size(PhoneLedgerUpright.SubtitleText);
        float dateWidth = cinzel.GetStringSize(date, fontSize: dateSize).X;
        float right = band.End.X - u.Px(PhoneLedgerUpright.DateRight);
        float titleX = band.Position.X + u.Px(PhoneLedgerUpright.TitleX);
        int fitted = SideCells.Fit(brush, title, titleSize, right - dateWidth - u.Px(24) - titleX);
        c.DrawString(brush, new Vector2(titleX, top + brush.GetAscent(fitted)), title, fontSize: fitted, modulate: Ui.Name);
        c.DrawString(cinzel, new Vector2(right - dateWidth, top + u.Px(PhoneLedgerUpright.DateTop) + cinzel.GetAscent(dateSize)), date,
            fontSize: dateSize, modulate: Ui.Name);
        c.DrawString(light, new Vector2(band.Position.X + u.Px(PhoneLedgerUpright.SubtitleX),
            top + u.Px(PhoneLedgerUpright.SubtitleTop) + light.GetAscent(subSize)), subtitle, fontSize: subSize, modulate: Ui.Subtitle);
        Ui.DrawDivider(c, band.Position.X + u.Px(PhoneLedgerUpright.DividerIn), band.End.X - u.Px(PhoneLedgerUpright.DividerIn),
            top + u.Px(PhoneLedgerUpright.Divider), u.K);
    }

    /// <summary>Draws <paramref name="text"/> centred on <paramref name="centre"/>, its middle at <paramref name="middle"/>; returns where it ends.</summary>
    private static float CentredMiddle(Control c, Font font, string text, int size, Color colour, float centre, float middle)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        return LeftAt(c, font, text, size, colour, centre - w / 2, middle);
    }
}
