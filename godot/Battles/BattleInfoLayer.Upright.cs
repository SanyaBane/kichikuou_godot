using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The info boxes on the phone held upright (docs/ui/phone-build.md, step P4; docs/ui/phone.md, §11, PB4r and PB4b): the
// results plate over the dimmed board at the screen's width (the title 5 mm, the damages 6 mm), a level-up's rows on the same
// plate; our narration panel — a fall's box too — the same way, where the plate stands; a defence's ribbon and caption at
// held landscape's millimetres (DrawDefence). In the board's layout pixels, the places PhoneBattleStatesUpright's.
internal sealed partial class BattleInfoLayer
{
    private static bool Upright => Device.Layout == DeviceLayout.PhoneUpright;

    /// <summary>A length of <paramref name="reference"/> reference pixels in the upright board's layout pixels.</summary>
    private static float U(float reference) => PhoneBattleUpright.Layout(reference);

    /// <summary>A font size of <paramref name="reference"/> reference pixels in the upright board's layout pixels.</summary>
    private static int UPx(float reference) => Math.Max(6, (int)Math.Round(U(reference)));

    /// <summary>The board under the layer, in its layout pixels: upright as high as the room.</summary>
    private Rect2 Board => new(Vector2.Zero, Size / _k);

    private void DrawUprightResults(BattleResults r)
    {
        DrawRect(Board, new Color(0, 0, 0, PhoneVeil));
        var plate = R(PhoneBattleStatesUpright.ResultsPlate(r.LevelUps.Count, Board.Size.Y));
        Ui.DrawPanel(this, plate, _k);
        float y = plate.Position.Y, cx = plate.GetCenter().X;
        int title = UPx(PhoneBattleStatesUpright.TitleText);
        Mid(_brush, cx + 2, y + U(PhoneBattleStatesUpright.TitleMiddle) + 2, r.Title, title, Colors.Black);
        Mid(_brush, cx, y + U(PhoneBattleStatesUpright.TitleMiddle), r.Title, title, Ui.Name);
        if (r.Reason.Length > 0)
        {
            int reason = UPx(PhoneBattleStatesUpright.ReasonText);
            Mid(_semi, cx, y + U(PhoneBattleStatesUpright.ReasonMiddle), Fit(_semi, r.Reason.ToUpperInvariant(), reason, plate.Size.X - U(80)),
                reason, Ui.Subtitle);
        }
        float side = U(PhoneBattleStatesUpright.DividerSide);
        Ui.DrawDivider(this, plate.Position.X + side, plate.End.X - side, y + U(PhoneBattleStatesUpright.Divider), _k);
        float quarter = plate.Size.X / 4;
        int army = UPx(PhoneBattleStatesUpright.ArmyText), damages = UPx(PhoneBattleStatesUpright.DamagesText);
        int word = UPx(PhoneBattleStatesUpright.DamagesWordText);
        foreach (var (name, value, ax) in new[] { (r.PlayerArmy, r.PlayerDamages, cx - quarter), (r.EnemyArmy, r.EnemyDamages, cx + quarter) })
        {
            Mid(_semi, ax, y + U(PhoneBattleStatesUpright.ArmyMiddle), Fit(_semi, name.ToUpperInvariant(), army, quarter * 2 - U(40)), army,
                Ui.Label);
            Mid(_bold, ax, y + U(PhoneBattleStatesUpright.DamagesMiddle), Figures.Number(value), damages, Ui.Minus);
            Mid(_semi, ax, y + U(PhoneBattleStatesUpright.DamagesWordMiddle), "DAMAGES", word, Ui.Subtitle);
        }
        DrawLine(new Vector2(cx, y + U(PhoneBattleStatesUpright.RuleTop)), new Vector2(cx, y + U(PhoneBattleStatesUpright.RuleFoot)),
            Ui.DimGold, Lacquer.Line(1, _k));
        Mid(_bold, cx, y + U(PhoneBattleStatesUpright.ExpMiddle), (r.Exp + " EXP obtained").ToUpperInvariant(),
            UPx(PhoneBattleStatesUpright.ExpText), Ui.Text);
        if (r.LevelUps.Count > 0)
        {
            Ui.DrawDivider(this, plate.Position.X + side, plate.End.X - side, y + U(720 + PhoneBattleStatesUpright.LevelDivider), _k);
            int wordSize = UPx(PhoneBattleStatesUpright.LevelWord), nameSize = UPx(PhoneBattleStatesUpright.LevelName);
            int gainsSize = UPx(PhoneBattleStatesUpright.LevelGains);
            for (int i = 0; i < r.LevelUps.Count; i++)
            {
                var up = r.LevelUps[i];
                var row = R(PhoneBattleStatesUpright.LevelUpRow(R(plate), i));
                Ui.DrawRowBox(this, row, false, _k);
                float x = row.Position.X + U(24);
                DrawString(_bold, new Vector2(x, Lacquer.Baseline(row, _bold, wordSize)), "LEVEL UP", fontSize: wordSize, modulate: Accent);
                x += _bold.GetStringSize("LEVEL UP", fontSize: wordSize).X + U(30);
                DrawString(_semi, new Vector2(x, Lacquer.Baseline(row, _semi, nameSize)), up.Unit, fontSize: nameSize, modulate: Ui.Name);
                float gx = Math.Max(row.Position.X + U(420), x + _semi.GetStringSize(up.Unit, fontSize: nameSize).X + U(30));
                string gains = string.Join("  ·  ", up.Gains);
                DrawString(_cinzel, new Vector2(gx, Lacquer.Baseline(row, _cinzel, gainsSize)), Fit(_cinzel, gains, gainsSize, row.End.X - U(16) - gx),
                    fontSize: gainsSize, modulate: Ui.Value);
            }
        }
        if (_waiting)
            UprightTriangleAt(new Vector2(plate.End.X - U(PhoneBattleStatesUpright.TriangleIn), plate.End.Y - U(PhoneBattleStatesUpright.TriangleUp)));
    }

    /// <summary>Our panel (a fall's box too) where the results plate stands, at 2.6 mm, over the dimmed board.</summary>
    private void DrawUprightPanel(IReadOnlyList<string> sentences)
    {
        int size = UPx(PhoneBattleStatesUpright.PanelText);
        var lines = sentences.SelectMany(s => Wrap(s, U(PhoneBattleStatesUpright.PanelTextWidth), size)).ToList();
        if (lines.Count == 0)
            lines.Add("");
        DrawRect(Board, new Color(0, 0, 0, PhoneVeil));
        var box = R(PhoneBattleStatesUpright.Panel(lines.Count, lines.Max(l => _gothic.GetStringSize(l, fontSize: size).X)));
        Ui.DrawPanel(this, box, _k, MessageWindowView.NarrationFill);
        float pitch = U(PhoneBattleStatesUpright.PanelPitch);
        for (int i = 0; i < lines.Count; i++)
        {
            float middle = box.Position.Y + U(PhoneBattleStatesUpright.PanelTop) + (i + 0.5f) * pitch;
            DrawString(_gothic, new Vector2(box.Position.X + U(PhoneBattleStatesUpright.PanelSide),
                middle + (_gothic.GetAscent(size) - _gothic.GetDescent(size)) / 2), lines[i], fontSize: size, modulate: MessageWindowView.NarrationText);
        }
        if (_waiting)
            UprightTriangleAt(new Vector2(box.End.X - U(40), box.End.Y - U(34)));
    }

    /// <summary>The click's triangle at <paramref name="at"/>, at the upright plate's size (held landscape's, a little smaller, PB4r's 22).</summary>
    private void UprightTriangleAt(Vector2 at)
    {
        float k = PhoneTriangle * PhoneBattleStatesUpright.TriangleSize / PhoneBattleStates.TriangleSize * PhoneBattleStatesUpright.Larger;
        DrawSetTransform(at * _k, 0, new Vector2(_k, _k) * k);
        Ui.DrawTriangle(this, Vector2.Zero, _k * k);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
    }

    private static PhoneFrame.Box R(Rect2 r) => new(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y);
}
