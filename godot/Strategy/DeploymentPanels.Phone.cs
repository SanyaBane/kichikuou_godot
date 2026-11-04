using System.Globalization;
using Godot;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The deployment table on the phone held landscape (docs/ui/phone-build.md, step L5; docs/ui/phone.md, §13, LD2 with the
// owner's change): over the whole screen, the ledger wholly at the left under its tabs (GameScreen places it), and right of
// it the title, the date and the budget over the party two cards a row as the battle shows our army (the back row left,
// each card with its ✕), the troops and the war expenses under it, Attack | Stop at the right thumb. A unit's picture comes
// by a long press on its row (UnitPictureView), not by a hover. Drawn in the frame's layout pixels over the whole screen
// (PhoneUnits), the places PhoneLedger's.
internal sealed partial class DeploymentPanels
{
    private PhoneFrame? _phone;

    /// <summary>
    /// Places <paramref name="table"/> under the tabs: in <see cref="TableArea"/> of the screen whose script pixel (0, 0) is
    /// at <paramref name="origin"/>, on the phone held landscape wholly at the left (LD2), held upright at the thumb (PD2).
    /// </summary>
    public static void PlaceTable(DeploymentTableView table, Vector2 origin, float pixelsPerScript)
    {
        if (Device.Upright(table) is { } upright)
        {
            var paper = PhoneLedgerUpright.Ledger(upright, deployment: true);
            table.PlaceUpright(new Rect2(paper.X, paper.Y, paper.W, paper.H), upright);
            return;
        }
        if (Device.Landscape(table) is { } phone)
        {
            var ledger = PhoneLedger.Ledger(phone, deployment: true);
            table.PlacePhone(new Rect2(ledger.X, ledger.Y, ledger.W, ledger.H), phone);
            return;
        }
        float k = pixelsPerScript / 2;                          // window pixels to a pixel of the 1280×800 screen
        table.Place(new Rect2(origin + TableArea.Position * k, TableArea.Size * k), pixelsPerScript);
    }

    /// <summary>A box of the canvas's units in the layout pixels the screen draws in on the phone.</summary>
    private Rect2 Box(PhoneFrame.Box b) => new(b.X / _k, b.Y / _k, b.W / _k, b.H / _k);

    /// <summary>The ledger's tabs on the phone (LD2's <c>land_tabs</c>), in layout pixels.</summary>
    internal static LedgerTabsBar.PhoneTabs PhoneTabs(PhoneFrame phone, bool deployment = true)
    {
        var u = new PhoneUnits(phone);
        if (phone.IsUpright)
            return new LedgerTabsBar.PhoneTabs(u.Rect(PhoneLedgerUpright.Tabs(phone, deployment)), u.Px(PhoneLedgerUpright.TabWidth),
                u.Px(PhoneLedgerUpright.TabGap), u.Px(PhoneLedgerUpright.TabDrop), u.Size(PhoneLedgerUpright.TabText),
                u.Size(PhoneLedgerUpright.OrderText), u.Rect(PhoneLedgerUpright.OrderNote(phone)));
        return new LedgerTabsBar.PhoneTabs(u.Rect(PhoneLedger.Tabs(phone, deployment)), u.Px(PhoneLedger.TabWidth), u.Px(PhoneLedger.TabGap),
            u.Px(PhoneLedger.TabDrop), u.Size(PhoneLedger.TabText), u.Size(PhoneLedger.OrderText));
    }

    /// <summary>A party card's insides on the phone as it is held.</summary>
    private static PhoneLedger.CardInsides Insides(PhoneFrame f) => f.IsUpright ? PhoneLedgerUpright.Insides : PhoneLedger.Insides;

    private void DrawPhone(Control c, PhoneFrame f)
    {
        if (f.IsUpright)
        {
            DrawUpright(c, f);
            return;
        }
        var u = new PhoneUnits(f);
        if (Table is not null)
            _tabs.Draw(c, _k, Table);
        if (_party is not { } party)
            return;
        var part = Box(PhoneLedger.Part(f));
        float x0 = part.Position.X, x1 = part.End.X;

        int title = u.Size(PhoneLedger.TitleText), date = u.Size(PhoneLedger.DateText), sub = u.Size(PhoneLedger.SubtitleText);
        c.DrawString(_brush, new Vector2(x0, u.Px(PhoneLedger.TitleTop) + _brush.GetAscent(title)), "Deployment", fontSize: title,
            modulate: Ui.Name);
        c.DrawString(_cinzel, new Vector2(x1 - _cinzel.GetStringSize(_date, fontSize: date).X, u.Px(PhoneLedger.DateTop) + _cinzel.GetAscent(date)),
            _date, fontSize: date, modulate: Ui.Name);
        c.DrawString(_cinzelLight, new Vector2(x0 + u.Px(2), u.Px(PhoneLedger.SubtitleTop) + _cinzelLight.GetAscent(sub)), Subtitle(party),
            fontSize: sub, modulate: Ui.Subtitle);
        int label = u.Size(PhoneLedger.LabelText), figure = u.Size(PhoneLedger.BudgetText);
        float mid = u.Px(PhoneLedger.BudgetMiddle), x = x1;
        x = RightAt(c, _cinzelLight, "GOLD", label, Ui.Subtitle, x, mid) - u.Px(12);
        x = RightAt(c, _cinzel, Figures.GoldShort(party.Gold * 100), figure, Ui.Name, x, mid) - u.Px(14);
        RightAt(c, _cinzelLight, "Budget", label, Ui.Subtitle, x, mid);
        Ui.DrawDivider(c, x0, x1, u.Px(PhoneLedger.Divider), _k);

        int caption = u.Size(PhoneLedger.CaptionText);
        float cy = u.Px(PhoneLedger.CaptionMiddle);
        LeftAt(c, _cinzel, "Back row", caption, Ui.Label, CardRect(3).Position.X + u.Px(4), cy);
        LeftAt(c, _cinzel, "Front row", caption, Ui.Label, CardRect(1).Position.X + u.Px(4), cy);

        for (int place = 1; place <= party.Places.Count; place++)
            DrawPhoneCard(c, u, place, party.Places[place - 1]);
        if (_dropOn != 0)
            DrawDrop(c, _dropOn);
        var (y0, y1, middle, line, cards) = PhoneLedger.Bracket(f, party.Places.Count);     // the front row held from the right (LD2f1)
        FacingBracket.DrawSide(c, u, Ui, _k, y0, y1, middle, line, cards, right: true);

        float totals = PhoneLedger.TotalsMiddle(f) / _k;
        int totalsLabel = u.Size(PhoneLedger.TotalsLabel), totalsText = u.Size(PhoneLedger.TotalsText);
        float after = LeftAt(c, _cinzelLight, "Troops", totalsLabel, Ui.Subtitle, x0 + u.Px(4), totals);
        LeftAt(c, _gothic, Figures.Number(party.Troops), totalsText, Ui.Value, after + u.Px(20), totals);
        float expenses = RightAt(c, _gothic, Figures.GoldShort(party.Cost * 100), totalsText, party.Affordable ? Ui.Value : Ui.Minus, x1, totals);
        RightAt(c, _cinzelLight, "War expenses", totalsLabel, Ui.Subtitle, expenses - u.Px(16), totals);

        DrawPhoneBar(c, u, Figures.GoldShort(party.Cost * 100));
    }

    /// <summary>The mode and the places offered, as the PC's title band says them.</summary>
    private string Subtitle(DeploymentParty party)
    {
        int places = party.Places.Count;
        string mode = _mode switch { 1 => "Attack", 2 => "Defence", 3 => "Dungeon search", _ => "" };
        return (mode == "" ? "" : mode + " · ") + (places == 1 ? "1 unit" : $"up to {places} units");
    }

    /// <summary>Stop at the left (Back in a defence), the buttons that send the party in red lacquer right of it.</summary>
    private void DrawPhoneBar(Control c, PhoneUnits u, string cost)
    {
        int sends = BarButtons().Count(b => b.Button != 3);
        foreach (var (button, rect) in BarButtons())
        {
            bool hover = button == _hovered, down = hover && button == _pressed;
            string label = _labels[button - 1].Trim();
            if (button == 3)
            {
                if (_mode == 2)
                    label = "Back";                             // a defence's Stop goes back to our attack screen (owner, 2026-10-24)
                Ui.DrawRowBox(c, rect, hover, _k);
                float stopText = u.Frame.IsUpright ? PhoneLedgerUpright.StopText : PhoneLedger.StopText;
                int stop = SideCells.Fit(_brush, label, u.Size(stopText), rect.Size.X - u.Px(30));
                CentredString(c, _brush, label, stop, hover ? Ui.Name : Ui.Text, rect.GetCenter().X, Lacquer.Baseline(rect, _brush, stop));
                continue;
            }
            Ui.DrawRedButton(c, rect, hover, down, _k);
            string? note = (_mode, button) switch
            {
                (2, 1) => DeploymentTable.LimitNote(_limits.Plains),   // each field's limit (owner, 2026-11-04)
                (2, 2) => DeploymentTable.LimitNote(_limits.City),
                (3, _) => $"Into the dungeon  ·  {cost}",
                _ => null,
            };
            int size = SideCells.Fit(_brush, label, u.Size(sends == 1 ? PhoneLedger.SendText : PhoneLedger.SendsText), rect.Size.X - u.Px(30));
            var ink = Color.Color8(250, 226, 160);
            if (note is null)
            {
                CentredString(c, _brush, label, size, ink, rect.GetCenter().X, Lacquer.Baseline(rect, _brush, size));
                continue;
            }
            var top = new Rect2(rect.Position, new Vector2(rect.Size.X, rect.Size.Y * 0.62f));
            var bottom = new Rect2(rect.Position.X, rect.Position.Y + rect.Size.Y * 0.56f, rect.Size.X, rect.Size.Y * 0.38f);
            bool upright = u.Frame.IsUpright;
            int small = SideCells.Fit(_gothic, note, u.Size(upright ? PhoneLedgerUpright.NoteText : PhoneLedger.NoteText), rect.Size.X - u.Px(24));
            float wordLine = Lacquer.Baseline(top, _brush, size), noteLine = Lacquer.Baseline(bottom, _gothic, small);
            if (upright)                                                   // the word and its note drawn together (owner, 2026-11-04)
            {
                float gap = noteLine - _gothic.GetAscent(small) - wordLine, pull = gap * (1 - PhoneLedgerUpright.NoteGap) / 2;
                (wordLine, noteLine) = (wordLine + pull, noteLine - pull);
            }
            CentredString(c, _brush, label, size, ink, rect.GetCenter().X, wordLine);
            CentredString(c, _gothic, note, small, Color.Color8(236, 206, 160), rect.GetCenter().X, noteLine);
        }
    }

    /// <summary>
    /// A party card at the phone's type (<c>party_card</c>): the face, the place's badge and the name beside it, ✕ at the
    /// top right, the troops with their bar under them, the cost at the foot right; "empty" in a dashed frame.
    /// </summary>
    private void DrawPhoneCard(Control c, PhoneUnits u, int place, DeploymentMember? member)
    {
        var rect = CardRect(place);
        var ins = Insides(u.Frame);
        if (member is null)
        {
            c.DrawRect(rect, Ui.Row with { A = 0.6f });
            DrawDashed(c, rect, Ui.DimGold);
            int big = u.Size(ins.EmptyText), small = u.Size(ins.EmptyLabel);
            CentredString(c, _cinzel, place.ToString(CultureInfo.InvariantCulture), big, Ui.DimGold, rect.GetCenter().X, rect.GetCenter().Y);
            CentredString(c, _cinzelLight, "empty", small, Ui.DimGold, rect.GetCenter().X, rect.GetCenter().Y + u.Px(50));
            return;
        }
        bool dragged = _dragging && place == _dragFrom;
        float a = dragged ? 0.35f : 1;
        c.DrawRect(rect, Ui.Row with { A = a });
        c.DrawRect(rect, Ui.DimGold with { A = a }, false, Line(1));

        float size = u.Px(ins.Face), inset = u.Px(ins.In);
        var face = new Rect2(rect.Position + new Vector2(inset, inset), new Vector2(size, size));
        c.DrawRect(face, Ui.FaceBack with { A = a });
        if (FaceTexture(member.FaceCg, size) is { } texture)
            c.DrawTextureRect(texture, face, false, new Color(1, 1, 1, a));
        c.DrawRect(face, Ui.DimGold with { A = a }, false, Line(1));

        float x0 = rect.Position.X, y0 = rect.Position.Y;
        var badge = new Vector2(x0 + u.Px(ins.BadgeX), y0 + u.Px(ins.BadgeY));
        Ui.DrawBadge(c, _cinzel, badge, u.Px(ins.Badge), place);
        var remove = RemoveRect(place);
        float nameX = x0 + u.Px(ins.NameX), nameRoom = remove.Position.X - u.Px(10) - nameX;
        int name = SideCells.Fit(_gothic, member.Name, u.Size(ins.NameText), nameRoom);
        LeftAt(c, _gothic, member.Name, name, Ui.Name with { A = a }, nameX, badge.Y);

        float tx = x0 + u.Px(ins.TroopsX);
        int troops = u.Size(ins.TroopsText);
        float troopsBase = y0 + u.Px(ins.TroopsTop) + _gothic.GetAscent(troops);
        string now = Figures.Number(member.Troops);
        c.DrawString(_gothic, new Vector2(tx, troopsBase), now, fontSize: troops, modulate: Ui.Text with { A = a });
        c.DrawString(_gothic, new Vector2(tx + _gothic.GetStringSize(now, fontSize: troops).X, troopsBase), "/" + Figures.Number(member.MaxTroops),
            fontSize: troops, modulate: Ui.Quiet with { A = a });
        float barRight = rect.End.X - u.Px(ins.BarRight), costRight = rect.End.X - u.Px(ins.CostRight);
        DrawStrength(c, new Rect2(tx, y0 + u.Px(ins.StrengthTop), barRight - tx, u.Px(ins.StrengthHeight)), member.Troops,
            member.MaxTroops, a);

        int cost = u.Size(ins.CostText), costLabel = u.Size(ins.CostLabel);
        float costBase = rect.End.Y - u.Px(ins.CostFoot) - _gothic.GetDescent(cost);
        string price = Figures.GoldShort(member.Cost * 100);
        float pw = _gothic.GetStringSize(price, fontSize: cost).X;
        c.DrawString(_gothic, new Vector2(costRight - pw, costBase), price, fontSize: cost, modulate: Ui.Text with { A = a });
        float lw = _cinzel.GetStringSize("Cost", fontSize: costLabel).X;
        c.DrawString(_cinzel, new Vector2(costRight - pw - u.Px(14) - lw, costBase - u.Px(4)), "Cost", fontSize: costLabel,
            modulate: Ui.Subtitle with { A = a });

        bool lit = !_dragging && _hovered == RemoveButton + place;
        c.DrawRect(remove, (lit ? Color.Color8(80, 30, 20) : Color.Color8(44, 24, 18)) with { A = a });
        c.DrawRect(remove, (lit ? Ui.Minus : Color.Color8(150, 62, 44)) with { A = a }, false, Line(1));
        var m = remove.GetCenter();
        float arm = u.Px(17), width = u.Px(5);
        c.DrawLine(m - new Vector2(arm, arm), m + new Vector2(arm, arm), Ui.Minus with { A = a }, width, true);
        c.DrawLine(m + new Vector2(-arm, arm), m + new Vector2(arm, -arm), Ui.Minus with { A = a }, width, true);
    }

    /// <summary>Draws <paramref name="text"/> from <paramref name="x"/>, its middle at <paramref name="middle"/>; returns where it ends.</summary>
    private static float LeftAt(Control c, Font font, string text, int size, Color colour, float x, float middle)
    {
        c.DrawString(font, new Vector2(x, middle + (font.GetAscent(size) - font.GetDescent(size)) / 2), text, fontSize: size, modulate: colour);
        return x + font.GetStringSize(text, fontSize: size).X;
    }

    /// <summary>Draws <paramref name="text"/> ending at <paramref name="right"/>, its middle at <paramref name="middle"/>; returns where it starts.</summary>
    private static float RightAt(Control c, Font font, string text, int size, Color colour, float right, float middle)
    {
        float w = font.GetStringSize(text, fontSize: size).X;
        c.DrawString(font, new Vector2(right - w, middle + (font.GetAscent(size) - font.GetDescent(size)) / 2), text, fontSize: size,
            modulate: colour);
        return right - w;
    }
}
