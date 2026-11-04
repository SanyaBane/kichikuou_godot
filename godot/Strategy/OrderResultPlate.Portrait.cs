using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The plate after a troop order on the phone held upright (docs/ui/phone.md, §30: the owner's PU1r1 over the screen dimmed
// 65%, PU1r1d2): a card between the room's top and the bar — the title over a divider; the unit's face, its name and troop
// type beside it and the troops before → after with the men gained; a bar of the troops (the troops kept in the ledger's
// colour, the gain green, the men lost to CMD-20 red, a raised maximum ticked) with the troops after under it; Cost | Budget
// in two columns under a divider; the click's triangle. Drawn in the frame's layout pixels over the whole screen
// (PhoneUnits), the places PhoneLedgerPortrait's. Held landscape the same card at the same size, in the screen's middle
// (§49, LU1r1).
internal sealed partial class OrderResultPlate
{
    private readonly FontVariation _cinzelLight = Lacquer.Cinzel(600);

    private void DrawCard(SubordinateResult r, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        DrawRect(new Rect2(Vector2.Zero, Size / u.K), PlateDim);
        var box = u.Rect(PhoneLedgerPortrait.ResultPlate(f));
        Ui.DrawPanel(this, box, u.K, PlateFill);
        float x0 = box.Position.X, x1 = box.End.X, cx = box.GetCenter().X;
        float At(float reference) => box.Position.Y + u.Px(reference);

        int title = SideCells.Fit(_brush, r.Title, u.Size(PhoneLedgerPortrait.ResultTitleText),
            box.Size.X - 2 * u.Px(PhoneLedgerPortrait.ResultTitleIn));
        Text(_brush, r.Title, title, Ui.Name, cx - _brush.GetStringSize(r.Title, fontSize: title).X / 2, At(PhoneLedgerPortrait.ResultTitleMiddle));
        float inset = u.Px(PhoneLedgerPortrait.ResultDividerIn);
        Ui.DrawDivider(this, x0 + inset, x1 - inset, At(PhoneLedgerPortrait.ResultDivider), u.K);

        float size = u.Px(PhoneLedgerPortrait.ResultFace);
        var faceRect = new Rect2(x0 + u.Px(PhoneLedgerPortrait.ResultFaceX), At(PhoneLedgerPortrait.ResultFaceTop), size, size);
        if (Face(r.FaceCg, size * u.K) is { } face)
            DrawTextureRect(face, faceRect, false);
        DrawRect(faceRect, Ui.Gold, false, Lacquer.Frame(1, u.K));

        float tx = x0 + u.Px(PhoneLedgerPortrait.ResultNameX), right = x1 - u.Px(PhoneLedgerPortrait.ResultBarIn);
        int name = SideCells.Fit(_brush, r.Name, u.Size(PhoneLedgerPortrait.ResultNameText), right - tx);
        Text(_brush, r.Name, name, Ui.Name, tx, At(PhoneLedgerPortrait.ResultNameMiddle));
        if (r.TroopType.Length > 0)
        {
            string type = r.TroopType.ToUpperInvariant();
            int typeSize = SideCells.Fit(_cinzelLight, type, u.Size(PhoneLedgerPortrait.ResultTypeText), right - tx);
            Text(_cinzelLight, type, typeSize, Ui.Subtitle, tx + u.Px(2), At(PhoneLedgerPortrait.ResultTypeMiddle));
        }
        PortraitTroops(r, tx, At(PhoneLedgerPortrait.ResultTroopsMiddle), right - tx, u.Size(PhoneLedgerPortrait.ResultTroopsText),
            u.Size(PhoneLedgerPortrait.ResultGainText));

        float barX = x0 + u.Px(PhoneLedgerPortrait.ResultBarIn);
        PortraitBar(r.Bar, new Rect2(barX, At(PhoneLedgerPortrait.ResultBarTop), right - barX, u.Px(PhoneLedgerPortrait.ResultBarHeight)), u.K);
        float now = At(PhoneLedgerPortrait.ResultNowMiddle);
        Text(_cinzelLight, "TROOPS", u.Size(PhoneLedgerPortrait.ResultNowLabel), Ui.Label, barX, now);
        int nowSize = u.Size(PhoneLedgerPortrait.ResultNowText);
        Text(_cinzelLight, r.TroopsNow, nowSize, Ui.Quiet, right - _cinzelLight.GetStringSize(r.TroopsNow, fontSize: nowSize).X, now);
        Ui.DrawDivider(this, x0 + inset, x1 - inset, At(PhoneLedgerPortrait.ResultDivider2), u.K);

        float cin = u.Px(PhoneLedgerPortrait.ResultColumnsIn), mid = cx, top = At(PhoneLedgerPortrait.ResultColumnsTop);
        int label = u.Size(PhoneLedgerPortrait.ResultLabelText), figure = u.Size(PhoneLedgerPortrait.ResultFigureText);
        float figureMiddle = top + label + u.Px(PhoneLedgerPortrait.ResultLabelGap) + figure / 2f;
        DrawLine(new Vector2(mid, top - u.Px(10)), new Vector2(mid, figureMiddle + figure / 2f + u.Px(10)), Ui.DimGold, Lacquer.Line(1, u.K));
        foreach (var (column, word, value, colour) in new[]
                 {
                     ((x0 + cin + mid) / 2, "COST", r.CostLine, Ui.Minus),
                     ((mid + x1 - cin) / 2, "BUDGET", r.BudgetLine, Ui.Name),
                 })
        {
            Text(_cinzelLight, word, label, Ui.Label, column - _cinzelLight.GetStringSize(word, fontSize: label).X / 2, top + label / 2f);
            int fitted = SideCells.Fit(_cinzel, value, figure, mid - x0 - cin - u.Px(20));
            Text(_cinzel, value, fitted, colour, column - _cinzel.GetStringSize(value, fontSize: fitted).X / 2, figureMiddle);
        }

        // The click's triangle at the mock-ups' size (48 reference pixels wide; the PC's is 16 layout pixels).
        var at = new Vector2(x1 - u.Px(PhoneLedgerPortrait.ResultTriangleRight), box.End.Y - u.Px(PhoneLedgerPortrait.ResultTriangleFoot));
        float s = u.Px(48) / 16;
        DrawSetTransform(at * u.K, 0, new Vector2(u.K, u.K) * s);
        Ui.DrawTriangle(this, Vector2.Zero, u.K * s);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
    }

    /// <summary>The troops before → after from <paramref name="x"/>, the gain after them or, where they would pass <paramref name="width"/>, under them.</summary>
    private void PortraitTroops(SubordinateResult r, float x, float middle, float width, int size, int gainSize)
    {
        string[] parts = r.TroopsLine.Split('→', StringSplitOptions.TrimEntries);
        float gap = size * 0.22f, arrow = _gothic.GetStringSize("→", fontSize: size - 4).X;
        float line = parts.Sum(p => _cinzel.GetStringSize(p, fontSize: size).X) + (parts.Length - 1) * (arrow + 4 * gap);
        float gain = _cinzel.GetStringSize(r.Gain, fontSize: gainSize).X;
        bool beside = line + 1.2f * gap + gain <= width;
        float cx = x;
        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0)
            {
                Text(_gothic, "→", size - 4, Ui.Gold, cx + 2 * gap, middle + size * 0.04f);
                cx += arrow + 4 * gap;
            }
            Text(_cinzel, parts[i], size, Ui.Value, cx, middle);
            cx += _cinzel.GetStringSize(parts[i], fontSize: size).X;
        }
        var colour = r.Gain.StartsWith('−') ? Ui.Minus : Ui.Plus;
        if (beside)
            Text(_cinzel, r.Gain, gainSize, colour, cx + 1.2f * gap, middle + size * 0.06f);
        else
            Text(_cinzel, r.Gain, gainSize, colour, x, middle + size * 1.05f);
    }

    /// <summary>The bar: the troops kept in the ledger's colour for their share before the order, the gain green, the men lost red, a raised maximum ticked.</summary>
    private void PortraitBar(TroopsBar bar, Rect2 rect, float k)
    {
        DrawRect(rect, Ui.OffTag);
        float x = rect.Position.X, w = rect.Size.X;
        var kept = bar.Before >= 0.75f ? Ui.BarGood : bar.Before >= 0.35f ? Ui.BarLow : Ui.BarBad;      // as the ledger's bar
        DrawRect(new Rect2(x, rect.Position.Y, w * bar.Kept, rect.Size.Y), kept);
        x += w * bar.Kept;
        DrawRect(new Rect2(x, rect.Position.Y, w * bar.Gained, rect.Size.Y), Ui.Plus);
        DrawRect(new Rect2(x, rect.Position.Y, w * bar.Lost, rect.Size.Y), Ui.Minus with { A = 0.45f });
        if (bar.OldMax is { } old)
        {
            float tx = rect.Position.X + w * old, reach = rect.Size.Y * 0.36f;
            DrawLine(new Vector2(tx, rect.Position.Y - reach), new Vector2(tx, rect.End.Y + reach), Ui.Name, Lacquer.Frame(1, k));
        }
        DrawRect(rect, Ui.DimGold, false, Lacquer.Line(1, k));
    }

    /// <summary><paramref name="text"/> from <paramref name="x"/>, its middle at <paramref name="middle"/>.</summary>
    private void Text(Font font, string text, int size, Color colour, float x, float middle) =>
        DrawString(font, new Vector2(x, middle + (font.GetAscent(size) - font.GetDescent(size)) / 2), text, fontSize: size, modulate: colour);
}
