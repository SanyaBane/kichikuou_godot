using Godot;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.Strategy;

// The Subordinates screen on the phone held upright (docs/ui/phone-build.md, step P5; docs/ui/phone.md, §7 and §20, PU1 with
// the owner's changes and PU1e3): the title band, the ledger under its tabs (two-line rows scrolled sideways, the chosen
// unit's row vermilion), the chosen unit's orders under it — Replenish and Talk (the unit's face) on one row, Add New Troops
// three a row —, the budget at the bar's left as text and End at the right thumb. A troop order's question stands in a
// callout under its button (PU1q), over it near the foot. The unit's picture comes by a long press on its row. Drawn in the
// frame's layout pixels over the whole screen (PhoneUnits), the places PhoneLedgerPortrait's.
internal sealed partial class SubordinatesView
{
    private void DrawPortrait(Control c, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        _tabs.Draw(c, _k, _table);
        DeploymentPanels.DrawBand(c, u, Box(PhoneLedgerPortrait.Band(f)), "Subordinates", _ready == 1 ? "1 unit ready" : $"{_ready} units ready",
            _date);
        if (_panel is not { } panel)
            return;

        var orders = Box(PhoneLedgerPortrait.Orders(f));
        Ui.DrawPanel(c, orders, _k);
        float x = orders.Position.X + u.Px(PhoneLedgerPortrait.OrdersX);
        int heading = u.Size(PhoneLedgerPortrait.OrdersText), unit = u.Size(PhoneLedgerPortrait.UnitText);
        float mid = orders.Position.Y + u.Px(PhoneLedgerPortrait.OrdersMiddle);
        c.DrawString(_brush, new Vector2(x, Middle(_brush, heading, mid)), "Orders", fontSize: heading, modulate: Ui.Name);
        float after = x + _brush.GetStringSize("Orders", fontSize: heading).X + u.Px(PhoneLedgerPortrait.UnitGap);
        string name = "· " + panel.Name.ToUpperInvariant();
        int fitted = SideCells.Fit(_cinzel, name, unit, orders.End.X - u.Px(PhoneLedgerPortrait.OrdersX) - after);
        c.DrawString(_cinzel, new Vector2(after, Middle(_cinzel, fitted, orders.Position.Y + u.Px(PhoneLedgerPortrait.UnitMiddle))), name,
            fontSize: fitted, modulate: Ui.Text);

        var rep = RectOf(ReplenishButton)!.Value;
        var r = panel.Replenish;
        int word = SideCells.Fit(_brush, "Replenish", u.Size(PhoneLedgerPortrait.ReplenishText), rep.Size.X - u.Px(30));
        string note = r.Enabled ? $"{Figures.Signed(r.Men)} · {Figures.GoldShort(r.Price * 100)}" : r.Note;
        int small = SideCells.Fit(_gothic, note, u.Size(PhoneLedgerPortrait.ReplenishNote), rep.Size.X - u.Px(24));
        if (Enabled(ReplenishButton))
            DrawPrimary(c, rep, "Replenish", note, ReplenishButton, word, small);
        else
            DrawPlain(c, rep, "Replenish", note, ReplenishButton, _brush, word, small);
        DrawPortraitTalk(c, u, panel);

        if (panel.AddOffered)
        {
            bool any = panel.Steps.Any(s => s.Enabled) && !Busy;
            int add = u.Size(PhoneLedgerPortrait.AddText);
            float ax = orders.Position.X + u.Px(PhoneLedgerPortrait.OrdersX - 4), ay = orders.Position.Y + u.Px(PhoneLedgerPortrait.AddMiddle);
            c.DrawString(_cinzel, new Vector2(ax, Middle(_cinzel, add, ay)), "Add New Troops", fontSize: add, modulate: any ? Ui.Label : Ui.OffText);
            float rule = ax + _cinzel.GetStringSize("Add New Troops", fontSize: add).X + u.Px(PhoneLedgerPortrait.AddRuleGap);
            c.DrawLine(new Vector2(rule, ay), new Vector2(orders.End.X - u.Px(24), ay), Ui.DimGold, Line(1));
            int men = u.Size(PhoneLedgerPortrait.StepText), price = u.Size(PhoneLedgerPortrait.PriceText);
            for (int i = 0; i < panel.Steps.Count; i++)
            {
                var step = panel.Steps[i];
                var rect = StepRect(i);
                string cost = Figures.GoldShort(step.Price * 100);
                DrawPlain(c, rect, Figures.Signed(step.Men), cost, StepButton + i, _cinzel, men,
                    SideCells.Fit(_gothic, cost, price, rect.Size.X - u.Px(24)));
            }
        }

        var bar = Box(f.Bar);                                   // PU1e3: the budget at the bar's left, as text
        float bx = bar.Position.X + u.Px(PhoneLedgerPortrait.BudgetX);
        int label = u.Size(PhoneLedgerPortrait.BudgetLabelText), figure = u.Size(PhoneLedgerPortrait.BudgetFigureText);
        c.DrawString(_cinzelLight, new Vector2(bx, Middle(_cinzelLight, label, bar.Position.Y + u.Px(PhoneLedgerPortrait.BudgetLabelMiddle))),
            "Budget", fontSize: label, modulate: Ui.Subtitle);
        string gold = Figures.GoldShort(panel.Gold * 100);
        c.DrawString(_cinzel, new Vector2(bx, Middle(_cinzel, figure, bar.Position.Y + u.Px(PhoneLedgerPortrait.BudgetFigureMiddle))), gold,
            fontSize: figure, modulate: Ui.Name);
        c.DrawString(_cinzelLight, new Vector2(bx + _cinzel.GetStringSize(gold, fontSize: figure).X + u.Px(PhoneLedgerPortrait.BudgetUnitGap),
            Middle(_cinzelLight, label, bar.Position.Y + u.Px(PhoneLedgerPortrait.BudgetUnitMiddle))), "GOLD", fontSize: label, modulate: Ui.Subtitle);
        var end = RectOf(EndButton)!.Value;
        DrawPlain(c, end, "End", null, EndButton, _brush, SideCells.Fit(_brush, "End", u.Size(PhoneLedgerPortrait.EndText), end.Size.X - u.Px(30)), 0);
    }

    /// <summary>The baseline that puts <paramref name="font"/>'s text of <paramref name="size"/> with its middle at <paramref name="middle"/>.</summary>
    private static float Middle(Font font, int size, float middle) => middle + (font.GetAscent(size) - font.GetDescent(size)) / 2;

    /// <summary>Talk beside Replenish (<c>talk_button</c>): the unit's face at the left, "Talk" and "WITH …" beside it; its talk's NEW or ??? pill at the top right.</summary>
    private void DrawPortraitTalk(Control c, PhoneUnits u, SubordinatePanel panel)
    {
        var rect = RectOf(TalkButton)!.Value;
        bool on = Enabled(TalkButton), hover = on && _hovered == TalkButton;
        if (!on)
        {
            c.DrawRect(rect, Ui.OffButton);
            c.DrawRect(rect, Ui.OffLine, false, Frame(1));
        }
        else
            Ui.DrawRowBox(c, rect, hover, _k);
        float inset = u.Px(PhoneLedgerPortrait.TalkFaceIn), size = rect.Size.Y - 2 * inset;
        var faceRect = new Rect2(rect.Position + new Vector2(inset, inset), new Vector2(size, size));
        if (_talkFace.Get(_faces, _crops, panel.FaceCg, size * _k) is { } face)
            c.DrawTextureRect(face, faceRect, false, on ? Colors.White : new Color(0.45f, 0.42f, 0.4f));
        c.DrawRect(faceRect, on ? Ui.Gold : Ui.OffLine, false, Line(1));
        float x = faceRect.End.X, w = rect.End.X - x;
        int text = SideCells.Fit(_brush, "Talk", u.Size(PhoneLedgerPortrait.TalkText), w - u.Px(26));
        float y0 = rect.Position.Y, y1 = rect.End.Y;
        CentredString(c, _brush, "Talk", text, !on ? Ui.OffText : hover ? Ui.Name : Ui.Text, x + w / 2,
            Middle(_brush, text, y0 + u.Px(PhoneLedgerPortrait.WordMiddle)));
        string with = "With " + panel.Name;
        int small = SideCells.Fit(_cinzelLight, with, u.Size(PhoneLedgerPortrait.TalkWith), w - u.Px(26));
        CentredString(c, _cinzelLight, with, small, on ? Ui.Subtitle : Ui.OffText, x + w / 2,
            Middle(_cinzelLight, small, y1 - u.Px(PhoneLedgerPortrait.NoteFoot)));
        if (on && PickerMarks.Label(panel.TalkMark) is { } pill)
            Ui.DrawPill(c, _cinzel, rect.End.X - u.Px(10), rect.Position.Y + u.Px(22), pill,
                panel.TalkMark == SceneMark.New ? Ui.MarkNew : Ui.MarkMaybe, u.Size(22), u.Px(30));
    }
}
