using System.Globalization;
using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// Our enemy attack screen on the phone held landscape (docs/ui/phone-build.md, step L6; docs/ui/phone.md, §14, LA2 and LA2n
// with the owner's changes): across the whole width between the hole's strips over the wave backdrop — the province's
// panel and the army's at the left, their content in their middle, the army's cards as the scout report's; at the right
// thumb a column of its own: the title, the date and the news, Maris's question, Abandon City | Ninja Scout, Intercept at
// the thumb. Ninja Scout's list opens above its button, two ninjas a row; Abandon City asks in the callout above its button.
// Drawn in the frame's layout pixels over the whole screen (PhoneUnits), the places PhoneAttack's.
internal sealed partial class EnemyAttackView
{
    private PhoneFrame? _phone;

    private void DrawPhone(EnemyAttack attack, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        _sheet.DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        Ui.DrawWaveBackdrop(_sheet, _k, new Vector2(f.Screen.W, f.Screen.H) / u.K);
        PhoneProvince(u, f, attack);
        var (vx, vy) = PhoneAttack.Vs(f);
        int vs = u.Size(PhoneAttack.VsText);
        CentredAt(_brush, "VS", vs, vx / u.K, Middle(_brush, vs, vy / u.K), Ui.Gold);
        PhoneArmy(u, f, attack);
        PhoneColumn(u, f, attack);
        if (_listOpen)
            PhoneList(u, attack);
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>The baseline that puts <paramref name="font"/>'s line at <paramref name="size"/> with its middle at <paramref name="middle"/>.</summary>
    private static float Middle(Font font, int size, float middle) => middle + (font.GetAscent(size) - font.GetDescent(size)) / 2;

    /// <summary>A panel's caption at its top left.</summary>
    private void PhoneCaption(PhoneUnits u, Rect2 panel, string text)
    {
        int size = u.Size(PhoneAttack.CaptionText);
        _sheet.DrawString(_cinzel, new Vector2(panel.Position.X + u.Px(PhoneAttack.CaptionIn),
            panel.Position.Y + u.Px(PhoneAttack.CaptionTop) + _cinzel.GetAscent(size)), text, fontSize: size, modulate: Ui.Subtitle);
    }

    /// <summary>The province: its picture and the four figures under it, in the panel's middle.</summary>
    private void PhoneProvince(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttack.Province(f));
        Ui.DrawPanel(_sheet, panel, _k);
        PhoneCaption(u, panel, "DEFENDING · " + attack.Province.ToUpperInvariant());
        var picture = u.Rect(PhoneAttack.Picture(f));
        if (Picture(attack.PictureCg) is { } texture)
            _sheet.DrawTextureRect(texture, picture, false);
        _sheet.DrawRect(picture, Ui.Gold, false, Lacquer.Line(1, _k));
        var rows = FigureRows(attack);
        int label = u.Size(PhoneAttack.FigureLabel), value = u.Size(PhoneAttack.FigureValue);
        for (int i = 0; i < rows.Length && i < PhoneAttack.Figures; i++)
        {
            var row = u.Rect(PhoneAttack.Figure(f, i));
            float mid = row.GetCenter().Y;
            _sheet.DrawString(_font!, new Vector2(row.Position.X, Middle(_font!, label, mid)), rows[i].Label, fontSize: label,
                modulate: Ui.Label);
            float w = _font!.GetStringSize(rows[i].Value, fontSize: value).X;
            _sheet.DrawString(_font, new Vector2(row.End.X - w, Middle(_font, value, mid)), rows[i].Value, fontSize: value,
                modulate: Ui.Value);
            _sheet.DrawLine(new Vector2(row.Position.X, row.End.Y), row.End, Ui.Seam, Lacquer.Line(1, _k));
        }
    }

    /// <summary>The army: the four places' cards, the front row left facing the province, the totals and the report's source under them.</summary>
    private void PhoneArmy(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttack.Army(f));
        Ui.DrawPanel(_sheet, panel, _k);
        PhoneCaption(u, panel, "ATTACKING · " + attack.Army.ToUpperInvariant());
        var report = attack.Report;

        int caption = u.Size(PhoneAttack.RowCaption);
        for (int column = 0; column < 2; column++)
        {
            var (cx, cy) = PhoneAttack.RowCaptionAt(f, column);
            CentredAt(_cinzelBold, column == 0 ? "FRONT ROW" : "BACK ROW", caption, cx / u.K, Middle(_cinzelBold, caption, cy / u.K), Ui.Subtitle);
            if (column == 0)
            {
                float ax = cx / u.K - u.Px(120), ay = cy / u.K, a = u.Px(8);      // the front row faces the province
                _sheet.DrawColoredPolygon([new(ax + a, ay - a), new(ax + a, ay + a), new(ax - a, ay)], Ui.Subtitle);
            }
        }
        for (int slot = 1; slot <= 4; slot++)
        {
            var card = u.Rect(PhoneAttack.Card(f, slot));
            if (report is null)
                PhoneUnknownCard(u, card, slot);
            else if (report.Places[slot - 1] is { } commander)
                PhoneCommanderCard.Draw(_sheet, u, Ui, card, slot, commander, _font!, _cinzel, _faces, _crops, PhoneAttack.CardFace);
            else
                PhoneEmptyCard(u, card, slot);
        }
        var (tip, middle, depth, half) = PhoneAttack.Pointer(f);
        float tx = tip / u.K, ty = middle / u.K, d = depth / u.K, h = half / u.K;
        Lacquer.DrawSmoothPolygon(_sheet, [new(tx, ty), new(tx + d, ty - h), new(tx + d, ty + h)], Ui.Gold, _k);

        var (x0, x1, top) = PhoneAttack.Totals(f);
        float left = x0 / u.K, right = x1 / u.K, y = top / u.K, mid = y + u.Px(PhoneAttack.TotalsMiddle);
        int label = u.Size(PhoneAttack.TotalsLabel), text = u.Size(PhoneAttack.TotalsText);
        _sheet.DrawString(_cinzel, new Vector2(left, Middle(_cinzel, label, mid)), "COMMANDERS", fontSize: label, modulate: Ui.Subtitle);
        _sheet.DrawString(_font!, new Vector2(left + u.Px(PhoneAttack.CountAt), Middle(_font!, text, mid)),
            report is null ? "??" : report.Commanders.ToString(CultureInfo.InvariantCulture), fontSize: text,
            modulate: report is null ? Ui.OffText : Ui.Value);
        string troops = report is null ? "??" : Figures.Number(report.Troops);
        float troopsX = right - _font!.GetStringSize(troops, fontSize: text).X;
        _sheet.DrawString(_font, new Vector2(troopsX, Middle(_font, text, mid)), troops, fontSize: text,
            modulate: report is null ? Ui.OffText : Ui.Value);
        float lw = _cinzel.GetStringSize("TOTAL TROOPS", fontSize: label).X;
        _sheet.DrawString(_cinzel, new Vector2(troopsX - u.Px(16) - lw, Middle(_cinzel, label, mid)), "TOTAL TROOPS", fontSize: label,
            modulate: Ui.Subtitle);
        float rule = y + u.Px(PhoneAttack.TotalsRule);
        _sheet.DrawLine(new Vector2(left, rule), new Vector2(right, rule), Ui.DimGold, Lacquer.Line(1, _k));
        string note = report is null
            ? attack.CanScout ? "Send a ninja to learn them." : "No ninja can be sent this week."
            : $"{NinjaName(attack, report.NinjaCg)}'s report";
        int noteSize = u.Size(PhoneAttack.NoteText);
        _sheet.DrawString(_font, new Vector2(left, y + u.Px(PhoneAttack.NoteTop) + _font.GetAscent(noteSize)), note, fontSize: noteSize,
            modulate: Ui.Quiet);
    }

    /// <summary>The face's square of a card at the phone's sizes: at its right, 12 in, at most <see cref="PhoneAttack.CardFace"/>.</summary>
    private static Rect2 PhoneFace(PhoneUnits u, Rect2 card)
    {
        float inset = u.Px(PhoneBattleStates.FaceIn), size = Math.Min(card.Size.Y - 2 * inset, u.Px(PhoneAttack.CardFace));
        return new Rect2(card.End.X - inset - size, card.Position.Y + inset, size, size);
    }

    /// <summary>A place not scouted yet, as the PC's: dashed, a "?" where the face goes, "Unknown", its troops "??".</summary>
    private void PhoneUnknownCard(PhoneUnits u, Rect2 card, int slot)
    {
        _sheet.DrawRect(card, Ui.Row with { A = 0.6f });
        Lacquer.DrawDashed(_sheet, card, Ui.DimGold, _k);
        var face = PhoneFace(u, card);
        Lacquer.DrawDashed(_sheet, face, Ui.OffLine, _k, 4, 8);
        int mark = u.Size(60);
        CentredAt(_brush, "?", mark, face.GetCenter().X, Middle(_brush, mark, face.GetCenter().Y), Ui.DimGold);

        float x0 = card.Position.X, right = face.Position.X - u.Px(30);
        var at = new Vector2(x0 + u.Px(40), card.Position.Y + u.Px(44));
        float r = u.Px(PhoneBattleStates.Badge);
        Lacquer.DrawDiamond(_sheet, at, r, Ui.DimGold);
        Lacquer.DrawDiamond(_sheet, at, r - u.Px(3), Ui.Row);
        int number = (int)Math.Round(r * 1.1f);
        CentredAt(_cinzel, slot.ToString(CultureInfo.InvariantCulture), number, at.X, Middle(_cinzel, number, at.Y), Ui.DimGold);
        int name = u.Size(32);
        _sheet.DrawString(_cinzel, new Vector2(x0 + u.Px(80), Middle(_cinzel, name, at.Y)), "Unknown", fontSize: name, modulate: Ui.OffText);
        int label = u.Size(PhoneBattleStates.CardLabel), troops = u.Size(PhoneBattleStates.CardTroops);
        _sheet.DrawString(_cinzel, new Vector2(x0 + u.Px(22), card.End.Y - u.Px(30) - _cinzel.GetDescent(label)), "TROOPS",
            fontSize: label, modulate: Ui.OffText);
        _sheet.DrawString(_font!, new Vector2(right - _font!.GetStringSize("??", fontSize: troops).X, card.End.Y - u.Px(28) - _font.GetDescent(troops)),
            "??", fontSize: troops, modulate: Ui.OffText);
    }

    /// <summary>A place the army leaves empty: our party's empty place at the phone's type (the deployment's).</summary>
    private void PhoneEmptyCard(PhoneUnits u, Rect2 card, int slot)
    {
        _sheet.DrawRect(card, Ui.Row with { A = 0.6f });
        Lacquer.DrawDashed(_sheet, card, Ui.DimGold, _k);
        int big = u.Size(PhoneLedger.EmptyText), small = u.Size(PhoneLedger.EmptyLabel);
        var c = card.GetCenter();
        CentredAt(_cinzel, slot.ToString(CultureInfo.InvariantCulture), big, c.X, Middle(_cinzel, big, c.Y), Ui.DimGold);
        CentredAt(_cinzel, "empty", small, c.X, Middle(_cinzel, small, c.Y + u.Px(50)), Ui.DimGold);
    }

    /// <summary>The column at the right thumb: the title and the news, Maris's question, the answers.</summary>
    private void PhoneColumn(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var column = u.Rect(PhoneAttack.Column(f));
        float x0 = column.Position.X, x1 = column.End.X;
        float flagWidth = u.Px(PhoneAttack.FlagSize), flagHeight = flagWidth * 38 / 48;    // the PC's flag's shape
        if (Picture(attack.FlagCg) is { } flag)
            _sheet.DrawTextureRect(flag, new Rect2(x0 + u.Px(PhoneAttack.FlagIn), u.Px(PhoneAttack.FlagMiddle) - flagHeight / 2, flagWidth, flagHeight),
                false);
        int title = u.Size(PhoneAttack.TitleText);
        _sheet.DrawString(_brush, new Vector2(x0 + u.Px(PhoneAttack.TitleIn), u.Px(PhoneAttack.TitleTop) + _brush.GetAscent(title)),
            "Enemy Attack", fontSize: title, modulate: Ui.Name);
        string date = attack.Date.Replace(", ", " · ");
        int dateSize = u.Size(PhoneAttack.DateText);
        _sheet.DrawString(_cinzelBold, new Vector2(x1 - _cinzelBold.GetStringSize(date, fontSize: dateSize).X,
            u.Px(PhoneAttack.DateTop) + _cinzelBold.GetAscent(dateSize)), date, fontSize: dateSize, modulate: Ui.Name);
        string[] news = [$"The {attack.Army}", $"appeared around {attack.Province}."];
        float newsX = x0 + u.Px(PhoneAttack.NewsIn), room = x1 - newsX;
        int newsSize = news.Min(line => SideCells.Fit(_font!, line, u.Size(PhoneAttack.NewsText), room));
        for (int i = 0; i < news.Length; i++)
            _sheet.DrawString(_font!, new Vector2(newsX, u.Px(PhoneAttack.NewsTop + i * PhoneAttack.NewsPitch) + _font!.GetAscent(newsSize)),
                news[i], fontSize: newsSize, modulate: Ui.Text);
        Ui.DrawDivider(_sheet, x0, x1, u.Px(PhoneAttack.Divider), _k);

        PhoneAdvisor(u, f, attack);
        PhoneAnswers(u, attack);
    }

    /// <summary>Maris's face square and her question in her own lines, one size for all.</summary>
    private void PhoneAdvisor(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttack.Advisor(f));
        Ui.DrawPanel(_sheet, panel, _k);
        float faceSize = u.Px(PhoneAttack.AdvisorFace);
        DrawFace(new Rect2(panel.Position.X + u.Px(PhoneAttack.AdvisorFaceIn), panel.Position.Y + u.Px(PhoneAttack.AdvisorFaceTop), faceSize, faceSize),
            EnemyAttack.AdvisorCg);
        float tx = panel.Position.X + u.Px(PhoneAttack.AdvisorTextIn), room = panel.End.X - u.Px(PhoneAttack.AdvisorRight) - tx;
        int name = u.Size(PhoneAttack.AdvisorName);
        _sheet.DrawString(_cinzel, new Vector2(tx, panel.Position.Y + u.Px(PhoneAttack.AdvisorNameTop) + _cinzel.GetAscent(name)), "MARIS",
            fontSize: name, modulate: Ui.Subtitle);
        var lines = attack.AdvisorSays.Take(4).ToList();
        int size = lines.Count == 0 ? u.Size(PhoneAttack.AdvisorText)
            : lines.Min(line => SideCells.Fit(_font!, line, u.Size(PhoneAttack.AdvisorText), room));
        for (int i = 0; i < lines.Count; i++)
            _sheet.DrawString(_font!, new Vector2(tx, panel.Position.Y + u.Px(PhoneAttack.AdvisorLinesTop) + i * size * PhoneAttack.AdvisorPitch
                + _font!.GetAscent(size)), lines[i], fontSize: size, modulate: Ui.Text);
    }

    /// <summary>Abandon City (the danger button) | Ninja Scout, and Intercept at the thumb.</summary>
    private void PhoneAnswers(PhoneUnits u, EnemyAttack attack)
    {
        bool Hovered(Spot spot) => _hover.Spot == spot && !_callout.MenuOpen;
        bool Down(Spot spot) => _pressed.Spot == spot && _hover.Spot == spot;

        var abandon = AbandonRect;
        bool abandonLit = Hovered(Spot.Abandon) || _callout.MenuOpen;
        Ui.DrawDangerButton(_sheet, abandon, abandonLit, _k);
        float middle = abandon.GetCenter().Y, s = u.Px(52) / 18;                       // the flag 52 high (the PC's 18)
        DrawWhiteFlag(abandon.Position.X + u.Px(36) - 2 * s, middle - u.Px(26), s);
        const string word = "Abandon City";
        float wordLeft = abandon.Position.X + u.Px(92), wordRoom = abandon.End.X - u.Px(8) - wordLeft;
        int wordSize = SideCells.Fit(_brush, word, u.Size(PhoneAttack.AbandonText), wordRoom - u.Px(10));
        CentredAt(_brush, word, wordSize, wordLeft + wordRoom / 2, Middle(_brush, wordSize, middle), abandonLit ? Ui.Name : Ui.DangerText);

        var scout = ScoutRect;
        bool canScout = attack.CanScout, scoutLit = canScout && (Hovered(Spot.Scout) || _listOpen);
        if (canScout)
            Ui.DrawPanel(_sheet, scout, _k, scoutLit ? Ui.RowHover : null);
        else
        {
            _sheet.DrawRect(scout, Ui.OffButton);
            _sheet.DrawRect(scout, Ui.OffLine, false, Lacquer.Line(1, _k));
        }
        int scoutSize = SideCells.Fit(_brush, "Ninja Scout", u.Size(PhoneAttack.ScoutText), scout.Size.X - u.Px(40));
        CentredAt(_brush, "Ninja Scout", scoutSize, scout.GetCenter().X, Middle(_brush, scoutSize, scout.Position.Y + u.Px(56)),
            canScout ? Ui.Name : Ui.OffText);
        int free = attack.Ninjas.Count(n => n.Free);
        string sub = attack.Ninjas.Count == 0 ? "no ninja in service" : $"{free} of {attack.Ninjas.Count} free";
        int subSize = SideCells.Fit(_font!, sub, u.Size(PhoneAttack.ScoutNote), scout.Size.X - u.Px(24));
        CentredAt(_font!, sub, subSize, scout.GetCenter().X, Middle(_font!, subSize, scout.End.Y - u.Px(38)), canScout ? Ui.Quiet : Ui.OffText);

        // Intercept, half as high as first drawn: its two lines closer (the mock-ups' low button).
        var intercept = InterceptRect;
        Ui.DrawRedButton(_sheet, intercept, Hovered(Spot.Intercept), Down(Spot.Intercept), _k);
        int interceptSize = u.Size(PhoneAttack.InterceptText), noteSize = u.Size(PhoneAttack.InterceptNote);
        CentredAt(_brush, "Intercept", interceptSize, intercept.GetCenter().X,
            Middle(_brush, interceptSize, intercept.Position.Y + intercept.Size.Y * 0.36f), Ui.Name);
        CentredAt(_font!, "choose the units", noteSize, intercept.GetCenter().X,
            Middle(_font!, noteSize, intercept.End.Y - intercept.Size.Y * 0.2f), Color.Color8(236, 206, 160));
    }

    /// <summary>Ninja Scout's list above its button, the column's width, two ninjas a row; the spent ones dimmed.</summary>
    private void PhoneList(PhoneUnits u, EnemyAttack attack)
    {
        var list = ListRect(attack);
        Ui.DrawPanel(_sheet, list, _k, Ui.CalloutFill with { A = 1 });              // over Maris's question: opaque
        var button = ScoutRect;
        float tip = button.GetCenter().X, half = u.Px(PhoneAttack.TipHalf);
        _sheet.DrawColoredPolygon([new(tip - half, list.End.Y - u.Px(2)), new(tip + half, list.End.Y - u.Px(2)),
            new(tip, button.Position.Y - u.Px(PhoneAttack.TipAbove))], Ui.Gold);
        int head = u.Size(PhoneAttack.ListTitle);
        _sheet.DrawString(_cinzel, new Vector2(list.Position.X + u.Px(PhoneAttack.ListTitleIn),
            Middle(_cinzel, head, list.Position.Y + u.Px(PhoneAttack.ListTitleMiddle))), "SEND TO SCOUT", fontSize: head, modulate: Ui.Subtitle);
        for (int i = 0; i < attack.Ninjas.Count; i++)
        {
            var ninja = attack.Ninjas[i];
            var row = ListRowRect(attack, i);
            bool hover = ninja.Free && _hover == (Spot.Ninja, ninja.Code);
            if (ninja.Free)
                Ui.DrawRowBox(_sheet, row, hover, _k);
            else
            {
                _sheet.DrawRect(row, Ui.OffButton);
                _sheet.DrawRect(row, Ui.OffLine, false, Lacquer.Line(1, _k));
            }
            float inset = u.Px(PhoneAttack.RowFaceIn), size = row.Size.Y - 2 * inset;
            var face = new Rect2(row.Position + new Vector2(inset, inset), new Vector2(size, size));
            if (_faces.SquareTexture(ninja.Portrait, _faces.CropOf(ninja.Portrait, _crops), (int)Math.Round(size * _k)) is { } square)
                _sheet.DrawTextureRect(square, face, false, ninja.Free ? Colors.White : new Color(0.45f, 0.42f, 0.4f));
            _sheet.DrawRect(face, Ui.DimGold, false, Lacquer.Line(1, _k));
            float tx = row.Position.X + row.Size.Y + u.Px(PhoneAttack.RowTextGap), room = row.End.X - inset - tx;
            string name = ninja.Name.ToUpperInvariant();
            int nameSize = SideCells.Fit(_cinzelBold, name, u.Size(PhoneAttack.RowName), room);
            _sheet.DrawString(_cinzelBold, new Vector2(tx, Middle(_cinzelBold, nameSize, row.Position.Y + row.Size.Y * PhoneAttack.RowNameMiddle)),
                name, fontSize: nameSize, modulate: ninja.Free ? Ui.Name : Ui.OffText);
            string state = ninja.Free ? "free this week" : "used this week";
            int stateSize = SideCells.Fit(_font!, state, u.Size(PhoneAttack.RowState), room);
            _sheet.DrawString(_font!, new Vector2(tx, Middle(_font!, stateSize, row.Position.Y + row.Size.Y * PhoneAttack.RowStateMiddle)),
                state, fontSize: stateSize, modulate: ninja.Free ? Ui.Quiet : Ui.OffText);
        }
    }
}
