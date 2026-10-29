using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// Our enemy attack screen on the phone held upright (docs/ui/phone-build.md, step P6; docs/ui/phone.md, §8, PA1 and PA1n
// with the owner's change): the PC's screen stacked over the whole room on the wave backdrop — the head (the flag, the
// title, the date and the news), the province's panel (its picture, its figures beside it), "VS", the army's panel (the
// front row on top facing up at the province, the back row under it, the totals), Maris's question, then the answers at
// the thumb: Abandon City | Ninja Scout, Intercept in the bar. The answers, Ninja Scout's list, the cards and the totals
// are held landscape's (EnemyAttackView.Phone.cs) at PhoneAttackUpright's places; Abandon City asks in the callout above
// its button. Drawn in the frame's layout pixels over the whole screen (PhoneUnits).
internal sealed partial class EnemyAttackView
{
    private void DrawUpright(EnemyAttack attack, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        _sheet.DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        Ui.DrawWaveBackdrop(_sheet, _k, new Vector2(f.Screen.W, f.Screen.H) / u.K);
        UprightHead(u, f, attack);
        UprightProvince(u, f, attack);
        var (vx, vy) = PhoneAttackUpright.Vs(f);
        int vs = u.Size(PhoneAttackUpright.VsText);
        CentredAt(_brush, "VS", vs, vx / u.K, Middle(_brush, vs, vy / u.K), Ui.Gold);
        UprightArmy(u, f, attack);
        UprightAdvisor(u, f, attack);
        PhoneAnswers(u, attack);
        if (_listOpen)
            PhoneList(u, attack);
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>The head under the hole: the attacker's flag, the title, the date at the right, the news, the divider.</summary>
    private void UprightHead(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var head = u.Rect(PhoneAttackUpright.Head(f));
        float x0 = head.Position.X, x1 = head.End.X, y = head.Position.Y;
        float flagWidth = u.Px(PhoneAttackUpright.FlagSize), flagHeight = flagWidth * 38 / 48;    // the PC's flag's shape
        if (Picture(attack.FlagCg) is { } flag)
            _sheet.DrawTextureRect(flag, new Rect2(x0 + u.Px(PhoneAttackUpright.FlagIn), y + u.Px(PhoneAttackUpright.FlagMiddle) - flagHeight / 2,
                flagWidth, flagHeight), false);
        int title = u.Size(PhoneAttackUpright.TitleText);
        _sheet.DrawString(_brush, new Vector2(x0 + u.Px(PhoneAttackUpright.TitleIn), y + _brush.GetAscent(title)), "Enemy Attack",
            fontSize: title, modulate: Ui.Name);
        string date = attack.Date.Replace(", ", " · ");
        int dateSize = u.Size(PhoneAttackUpright.DateText);
        _sheet.DrawString(_cinzelBold, new Vector2(x1 - u.Px(PhoneAttackUpright.DateRight) - _cinzelBold.GetStringSize(date, fontSize: dateSize).X,
            y + u.Px(PhoneAttackUpright.DateTop) + _cinzelBold.GetAscent(dateSize)), date, fontSize: dateSize, modulate: Ui.Name);
        string news = $"The {attack.Army} appeared around {attack.Province}.";
        float newsX = x0 + u.Px(PhoneAttackUpright.NewsIn);
        int newsSize = SideCells.Fit(_font!, news, u.Size(PhoneAttackUpright.NewsText), x1 - u.Px(PhoneAttackUpright.NewsIn) - newsX);
        _sheet.DrawString(_font!, new Vector2(newsX, y + u.Px(PhoneAttackUpright.NewsTop) + _font!.GetAscent(newsSize)), news,
            fontSize: newsSize, modulate: Ui.Text);
        Ui.DrawDivider(_sheet, x0 + u.Px(PhoneAttackUpright.DividerIn), x1 - u.Px(PhoneAttackUpright.DividerIn),
            y + u.Px(PhoneAttackUpright.Divider), _k);
    }

    /// <summary>The province: its picture at the left, its four figures beside it, each a label over its value.</summary>
    private void UprightProvince(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttackUpright.Province(f));
        Ui.DrawPanel(_sheet, panel, _k);
        PhoneCaption(u, panel, "DEFENDING · " + attack.Province.ToUpperInvariant());
        var picture = u.Rect(PhoneAttackUpright.Picture(f));
        if (Picture(attack.PictureCg) is { } texture)
            _sheet.DrawTextureRect(texture, picture, false);
        _sheet.DrawRect(picture, Ui.Gold, false, Lacquer.Line(1, _k));
        var rows = FigureRows(attack);
        int label = u.Size(PhoneAttackUpright.FigureLabel), value = u.Size(PhoneAttackUpright.FigureValue);
        for (int i = 0; i < rows.Length && i < PhoneAttackUpright.Figures; i++)
        {
            var row = u.Rect(PhoneAttackUpright.Figure(f, i));
            float room = row.Size.X;
            int labelSize = SideCells.Fit(_font!, rows[i].Label, label, room);
            _sheet.DrawString(_font!, new Vector2(row.Position.X, row.Position.Y + _font!.GetAscent(labelSize)), rows[i].Label,
                fontSize: labelSize, modulate: Ui.Label);
            int valueSize = SideCells.Fit(_font, rows[i].Value, value, room);
            float w = _font.GetStringSize(rows[i].Value, fontSize: valueSize).X;
            _sheet.DrawString(_font, new Vector2(row.End.X - w, row.Position.Y + u.Px(PhoneAttackUpright.ValueTop) + _font.GetAscent(valueSize)),
                rows[i].Value, fontSize: valueSize, modulate: Ui.Value);
            _sheet.DrawLine(new Vector2(row.Position.X, row.End.Y), row.End, Ui.Seam, Lacquer.Line(1, _k));
        }
    }

    /// <summary>The army: the front row's cards on top facing up at the province, the back row's under them, the totals.</summary>
    private void UprightArmy(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttackUpright.Army(f));
        Ui.DrawPanel(_sheet, panel, _k);
        PhoneCaption(u, panel, "ATTACKING · " + attack.Army.ToUpperInvariant());
        var report = attack.Report;

        int caption = u.Size(PhoneAttack.RowCaption);
        for (int row = 0; row < 2; row++)
        {
            var (cx, cy) = PhoneAttackUpright.RowCaptionAt(f, row);
            float x = cx / u.K, y = cy / u.K;
            CentredAt(_cinzelBold, row == 0 ? "FRONT ROW" : "BACK ROW", caption, x, Middle(_cinzelBold, caption, y), Ui.Subtitle);
            if (row == 0)
            {
                float ax = x + u.Px(PhoneAttackUpright.ArrowRight), a = u.Px(PhoneAttackUpright.ArrowHalf);  // the front row faces the province
                _sheet.DrawColoredPolygon([new(ax - a, y + a), new(ax + a, y + a), new(ax, y - a)], Ui.Subtitle);
            }
        }
        for (int slot = 1; slot <= 4; slot++)
        {
            var card = u.Rect(PhoneAttackUpright.Card(f, slot));
            if (report is null)
                PhoneUnknownCard(u, card, slot, PhoneAttackUpright.CardFace);
            else if (report.Places[slot - 1] is { } commander)
                PhoneCommanderCard.Draw(_sheet, u, Ui, card, slot, commander, _font!, _cinzel, _faces, _crops, PhoneAttackUpright.CardFace);
            else
                PhoneEmptyCard(u, card, slot);
        }
        var (x0, x1, top) = PhoneAttackUpright.Totals(f);
        PhoneTotals(u, x0, x1, top, attack);
    }

    /// <summary>Maris's face square and her question in her own lines, one size for all.</summary>
    private void UprightAdvisor(PhoneUnits u, PhoneFrame f, EnemyAttack attack)
    {
        var panel = u.Rect(PhoneAttackUpright.Advisor(f));
        Ui.DrawPanel(_sheet, panel, _k);
        float faceSize = u.Px(PhoneAttackUpright.AdvisorFace);
        DrawFace(new Rect2(panel.Position.X + u.Px(PhoneAttackUpright.AdvisorFaceIn), panel.Position.Y + u.Px(PhoneAttackUpright.AdvisorFaceTop),
            faceSize, faceSize), EnemyAttack.AdvisorCg);
        float tx = panel.Position.X + u.Px(PhoneAttackUpright.AdvisorTextIn), room = panel.End.X - u.Px(PhoneAttackUpright.AdvisorRight) - tx;
        int name = u.Size(PhoneAttackUpright.AdvisorName);
        _sheet.DrawString(_cinzel, new Vector2(tx, panel.Position.Y + u.Px(PhoneAttackUpright.AdvisorNameTop) + _cinzel.GetAscent(name)), "MARIS",
            fontSize: name, modulate: Ui.Subtitle);
        var lines = attack.AdvisorSays.Take(4).ToList();
        int size = lines.Count == 0 ? u.Size(PhoneAttackUpright.AdvisorText)
            : lines.Min(line => SideCells.Fit(_font!, line, u.Size(PhoneAttackUpright.AdvisorText), room));
        for (int i = 0; i < lines.Count; i++)
            _sheet.DrawString(_font!, new Vector2(tx, panel.Position.Y + u.Px(PhoneAttackUpright.AdvisorLinesTop)
                + i * size * PhoneAttackUpright.AdvisorPitch + _font!.GetAscent(size)), lines[i], fontSize: size, modulate: Ui.Text);
    }
}
