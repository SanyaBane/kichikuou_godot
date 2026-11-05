using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// Our enemy attack screen on the phone held upright (docs/ui/phone-build.md, step P6; docs/ui/phone.md, §8, PA1 and PA1n
// with the owner's change): the PC's screen stacked over the whole room on the wave backdrop — the head (the flag, the
// title, the date and the news), the province's panel (its picture, its figures beside it), "VS", the army's panel (the
// front row on top facing up at the province, held from above in the PC's bracket turned up — the owner's PA1f2a, §31 —,
// the back row under it, the totals), Maris's question, then the answers at
// the thumb: Abandon City | Ninja Scout, Intercept in the bar. The answers, Ninja Scout's list, the cards and the totals
// are held landscape's (EnemyAttackView.Phone.cs) at PhoneAttackPortrait's places; Abandon City asks in the callout above
// its button. Drawn in the frame's layout pixels over the whole screen (PhoneUnits).
internal sealed partial class EnemyAttackView
{
    private void DrawPortrait(EnemyAttack attack, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        _sheet.DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        Ui.DrawWaveBackdrop(_sheet, _k, new Vector2(f.Screen.W, f.Screen.H) / u.K);
        PortraitHead(u, f, attack);
        PortraitProvince(u, f, attack);
        var (vx, vy) = PhoneAttackPortrait.Vs(f);
        int vs = u.Size(PhoneAttackPortrait.VsText);
        CentredAt(_brush, "VS", vs, vx / u.K, Middle(_brush, vs, vy / u.K), Ui.Gold);
        PortraitArmy(u, f, attack);
        PortraitAdvisor(u, f, attack);
        PhoneAnswers(u, attack);
        if (_listOpen)
            PhoneList(u, attack);
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>The head under the hole: the attacker's flag, the title, the date at the right, the news, the divider.</summary>
    private void PortraitHead(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var head = u.Rect(PhoneAttackPortrait.Head(f));
        float x0 = head.Position.X, x1 = head.End.X, y = head.Position.Y;
        float flagWidth = u.Px(PhoneAttackPortrait.FlagSize), flagHeight = flagWidth * 38 / 48;    // the PC's flag's shape
        if (Picture(attack.FlagCg) is { } flag)
            _sheet.DrawTextureRect(flag, new Rect2(x0 + u.Px(PhoneAttackPortrait.FlagIn), y + u.Px(PhoneAttackPortrait.FlagMiddle) - flagHeight / 2,
                flagWidth, flagHeight), false);
        int title = u.Size(PhoneAttackPortrait.TitleText);
        _sheet.DrawString(_brush, new Vector2(x0 + u.Px(PhoneAttackPortrait.TitleIn), y + _brush.GetAscent(title)), "Enemy Attack",
            fontSize: title, modulate: Ui.Name);
        string date = attack.Date.Replace(", ", " · ");
        int dateSize = u.Size(PhoneAttackPortrait.DateText);
        _sheet.DrawString(_cinzelBold, new Vector2(x1 - u.Px(PhoneAttackPortrait.DateRight) - _cinzelBold.GetStringSize(date, fontSize: dateSize).X,
            y + u.Px(PhoneAttackPortrait.DateTop) + _cinzelBold.GetAscent(dateSize)), date, fontSize: dateSize, modulate: Ui.Name);
        string news = $"The {attack.Army} appeared around {attack.Province}.";
        float newsX = x0 + u.Px(PhoneAttackPortrait.NewsIn);
        int newsSize = SideCells.Fit(_font!, news, u.Size(PhoneAttackPortrait.NewsText), x1 - u.Px(PhoneAttackPortrait.NewsIn) - newsX);
        _sheet.DrawString(_font!, new Vector2(newsX, y + u.Px(PhoneAttackPortrait.NewsTop) + _font!.GetAscent(newsSize)), news,
            fontSize: newsSize, modulate: Ui.Text);
        Ui.DrawDivider(_sheet, x0 + u.Px(PhoneAttackPortrait.DividerIn), x1 - u.Px(PhoneAttackPortrait.DividerIn),
            y + u.Px(PhoneAttackPortrait.Divider), _k);
    }

    /// <summary>The province: its picture at the left, its four figures beside it, each a label over its value.</summary>
    private void PortraitProvince(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttackPortrait.Province(f));
        Ui.DrawPanel(_sheet, panel, _k);
        PhoneCaption(u, panel, "DEFENDING · " + attack.Province.ToUpperInvariant());
        var picture = u.Rect(PhoneAttackPortrait.Picture(f));
        if (Picture(attack.PictureCg) is { } texture)
            _sheet.DrawTextureRect(texture, picture, false);
        _sheet.DrawRect(picture, Ui.Gold, false, Lacquer.Line(1, _k));
        var rows = FigureRows(attack);
        int label = u.Size(PhoneAttackPortrait.FigureLabel), value = u.Size(PhoneAttackPortrait.FigureValue);
        for (int i = 0; i < rows.Length && i < PhoneAttackPortrait.Figures; i++)
        {
            var row = u.Rect(PhoneAttackPortrait.Figure(f, i));
            float room = row.Size.X;
            int labelSize = SideCells.Fit(_font!, rows[i].Label, label, room);
            _sheet.DrawString(_font!, new Vector2(row.Position.X, row.Position.Y + _font!.GetAscent(labelSize)), rows[i].Label,
                fontSize: labelSize, modulate: Ui.Label);
            int valueSize = SideCells.Fit(_font, rows[i].Value, value, room);
            float w = _font.GetStringSize(rows[i].Value, fontSize: valueSize).X;
            _sheet.DrawString(_font, new Vector2(row.End.X - w, row.Position.Y + u.Px(PhoneAttackPortrait.ValueTop) + _font.GetAscent(valueSize)),
                rows[i].Value, fontSize: valueSize, modulate: Ui.Value);
            _sheet.DrawLine(new Vector2(row.Position.X, row.End.Y), row.End, Ui.Seam, Lacquer.Line(1, _k));
        }
    }

    /// <summary>The army: the front row's cards on top facing up at the province, the back row's under them, the totals.</summary>
    private void PortraitArmy(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttackPortrait.Army(f));
        Ui.DrawPanel(_sheet, panel, _k);
        PhoneCaption(u, panel, "ATTACKING · " + attack.Army.ToUpperInvariant());
        var report = attack.Report;

        int caption = u.Size(PhoneAttack.RowCaption);
        for (int row = 0; row < 2; row++)
        {
            var (cx, cy) = PhoneAttackPortrait.RowCaptionAt(f, row);
            float x = cx / u.K, y = cy / u.K;
            CentredAt(_cinzelBold, row == 0 ? "FRONT ROW" : "BACK ROW", caption, x, Middle(_cinzelBold, caption, y), Ui.Subtitle);
        }
        PortraitBracket(u, f);
        for (int slot = 1; slot <= 4; slot++)
        {
            var card = u.Rect(PhoneAttackPortrait.Card(f, slot));
            if (report is null)
                PhoneUnknownCard(u, card, slot, PhoneAttackPortrait.CardFace);
            else if (report.Places[slot - 1] is { } commander)
                PhoneCommanderCard.Draw(_sheet, u, Ui, card, slot, commander, _font!, _cinzel, _faces, _crops, PhoneAttackPortrait.CardFace);
            else
                PhoneEmptyCard(u, card, slot);
        }
        var (x0, x1, top) = PhoneAttackPortrait.Totals(f);
        PhoneTotals(u, x0, x1, top, attack);
        if (report is not null)                                 // PK1: the seal over the frame, the words on the caption's line
            PhoneScoutSeal(u, panel, u.Rect(PhoneAttackPortrait.Seal(f)), attack, report, PhoneAttackPortrait.SealWordsGap,
                PhoneAttackPortrait.SealLabel, PhoneAttackPortrait.SealName, nameDrop: 0, nameGap: PhoneAttackPortrait.SealNameGap);
    }

    /// <summary>
    /// The PC's bracket turned up (<see cref="DrawArmyBracket"/>; the owner's PA1f2a): a gold line over "FRONT ROW" from the
    /// front row's left to its right, its ends down to the cards, its head in the middle pointing at the province.
    /// </summary>
    private void PortraitBracket(PhoneUnits u, PhoneFrame f)
    {
        var (left, right, middle, line, cards) = PhoneAttackPortrait.Bracket(f);
        FacingBracket.Draw(_sheet, u, Ui, _k, left, right, middle, line, cards, down: false);
    }

    /// <summary>Maris's face square and her question in her own lines, one size for all.</summary>
    private void PortraitAdvisor(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttackPortrait.Advisor(f));
        Ui.DrawPanel(_sheet, panel, _k);
        float faceSize = u.Px(PhoneAttackPortrait.AdvisorFace);
        DrawFace(new Rect2(panel.Position.X + u.Px(PhoneAttackPortrait.AdvisorFaceIn), panel.Position.Y + u.Px(PhoneAttackPortrait.AdvisorFaceTop),
            faceSize, faceSize), EnemyAttack.AdvisorCg);
        float tx = panel.Position.X + u.Px(PhoneAttackPortrait.AdvisorTextIn), room = panel.End.X - u.Px(PhoneAttackPortrait.AdvisorRight) - tx;
        int name = u.Size(PhoneAttackPortrait.AdvisorName);
        _sheet.DrawString(_cinzel, new Vector2(tx, panel.Position.Y + u.Px(PhoneAttackPortrait.AdvisorNameTop) + _cinzel.GetAscent(name)), "MARIS",
            fontSize: name, modulate: Ui.Subtitle);
        var lines = attack.AdvisorSays.Take(4).ToList();
        int size = lines.Count == 0 ? u.Size(PhoneAttackPortrait.AdvisorText)
            : lines.Min(line => SideCells.Fit(_font!, line, u.Size(PhoneAttackPortrait.AdvisorText), room));
        for (int i = 0; i < lines.Count; i++)
            _sheet.DrawString(_font!, new Vector2(tx, panel.Position.Y + u.Px(PhoneAttackPortrait.AdvisorLinesTop)
                + i * size * PhoneAttackPortrait.AdvisorPitch + _font!.GetAscent(size)), lines[i], fontSize: size, modulate: Ui.Text);
    }
}
