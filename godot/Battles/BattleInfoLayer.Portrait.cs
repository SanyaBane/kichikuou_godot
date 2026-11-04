using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The info boxes on the phone held upright (docs/ui/phone-build.md, step P4; docs/ui/phone.md, §11, PB4r and PB4b): the
// results plate over the dimmed board at the screen's width (the title 5 mm, the damages 6 mm), a level-up's rows on the same
// plate; our narration panel — a fall's box too — the same way, where the plate stands; a defence's ribbon and caption at
// held landscape's millimetres (DrawDefence). In the board's layout pixels, the places PhoneBattleStatesPortrait's.
internal sealed partial class BattleInfoLayer
{
    private static bool Portrait => Device.Layout == DeviceLayout.PhonePortrait;

    /// <summary>A length of <paramref name="reference"/> reference pixels in the upright board's layout pixels.</summary>
    private static float U(float reference) => PhoneBattlePortrait.Layout(reference);

    /// <summary>A font size of <paramref name="reference"/> reference pixels in the upright board's layout pixels.</summary>
    private static int UPx(float reference) => Math.Max(6, (int)Math.Round(U(reference)));

    /// <summary>The board under the layer, in its layout pixels: upright as high as the room.</summary>
    private Rect2 Board => new(Vector2.Zero, Size / _k);

    private void DrawPortraitResults(BattleResults r)
    {
        DrawRect(Board, new Color(0, 0, 0, PhoneVeil));
        var (_, _, rowsWidth) = PhoneBattleStatesPortrait.LevelRows(PhoneBattleStatesPortrait.ResultsPlate(0, Board.Size.Y));
        var levels = new LevelMetrics(UPx(PhoneBattleStatesPortrait.LevelLabel), UPx(PhoneBattleStatesPortrait.LevelName),
            UPx(PhoneBattleStatesPortrait.LevelGains), U(PhoneBattleStatesPortrait.LevelLeft), U(PhoneBattleStatesPortrait.LevelRight),
            U(PhoneBattleStatesPortrait.LevelPad), U(PhoneBattleStatesPortrait.LevelGap), U(PhoneBattleStatesPortrait.LevelBetween), rowsWidth,
            WholeNames: false);
        var (rows, column, rowsHeight) = LevelRows(r.LevelUps, levels);
        var box = PhoneBattleStatesPortrait.ResultsPlate(rowsHeight, Board.Size.Y);
        var plate = R(box);
        Ui.DrawPanel(this, plate, _k);
        float y = plate.Position.Y, cx = plate.GetCenter().X;
        int title = UPx(PhoneBattleStatesPortrait.TitleText);
        Mid(_brush, cx + 2, y + U(PhoneBattleStatesPortrait.TitleMiddle) + 2, r.Title, title, Colors.Black);
        Mid(_brush, cx, y + U(PhoneBattleStatesPortrait.TitleMiddle), r.Title, title, Ui.Name);
        if (r.Reason.Length > 0)
        {
            int reason = UPx(PhoneBattleStatesPortrait.ReasonText);
            Mid(_semi, cx, y + U(PhoneBattleStatesPortrait.ReasonMiddle), Fit(_semi, r.Reason.ToUpperInvariant(), reason, plate.Size.X - U(80)),
                reason, Ui.Subtitle);
        }
        float side = U(PhoneBattleStatesPortrait.DividerSide);
        Ui.DrawDivider(this, plate.Position.X + side, plate.End.X - side, y + U(PhoneBattleStatesPortrait.Divider), _k);
        float quarter = plate.Size.X / 4;
        int army = UPx(PhoneBattleStatesPortrait.ArmyText), damages = UPx(PhoneBattleStatesPortrait.DamagesText);
        int word = UPx(PhoneBattleStatesPortrait.DamagesWordText);
        foreach (var (name, value, ax) in new[] { (r.PlayerArmy, r.PlayerDamages, cx - quarter), (r.EnemyArmy, r.EnemyDamages, cx + quarter) })
        {
            Mid(_semi, ax, y + U(PhoneBattleStatesPortrait.ArmyMiddle), Fit(_semi, name.ToUpperInvariant(), army, quarter * 2 - U(40)), army,
                Ui.Label);
            Mid(_bold, ax, y + U(PhoneBattleStatesPortrait.DamagesMiddle), Figures.Number(value), damages, Ui.Minus);
            Mid(_semi, ax, y + U(PhoneBattleStatesPortrait.DamagesWordMiddle), "DAMAGES", word, Ui.Subtitle);
        }
        DrawLine(new Vector2(cx, y + U(PhoneBattleStatesPortrait.RuleTop)), new Vector2(cx, y + U(PhoneBattleStatesPortrait.RuleFoot)),
            Ui.DimGold, Lacquer.Line(1, _k));
        Mid(_bold, cx, y + U(PhoneBattleStatesPortrait.ExpMiddle), (r.Exp + " EXP obtained").ToUpperInvariant(),
            UPx(PhoneBattleStatesPortrait.ExpText), Ui.Text);
        if (r.LevelUps.Count > 0)
        {
            Ui.DrawDivider(this, plate.Position.X + side, plate.End.X - side, y + U(720 + PhoneBattleStatesPortrait.LevelDivider), _k);
            var (rx, ry, _) = PhoneBattleStatesPortrait.LevelRows(box);
            DrawLevelRows(rx, ry, rows, column, levels);
        }
        if (_waiting)
            PortraitTriangleAt(new Vector2(plate.End.X - U(PhoneBattleStatesPortrait.TriangleIn), plate.End.Y - U(PhoneBattleStatesPortrait.TriangleUp)));
    }

    /// <summary>Our panel (a fall's box too) where the results plate stands, at 2.6 mm, over the dimmed board.</summary>
    private void DrawPortraitPanel(IReadOnlyList<string> sentences)
    {
        int size = UPx(PhoneBattleStatesPortrait.PanelText);
        var lines = sentences.SelectMany(s => Wrap(s, U(PhoneBattleStatesPortrait.PanelTextWidth), size)).ToList();
        if (lines.Count == 0)
            lines.Add("");
        DrawRect(Board, new Color(0, 0, 0, PhoneVeil));
        var box = R(PhoneBattleStatesPortrait.Panel(lines.Count, lines.Max(l => _gothic.GetStringSize(l, fontSize: size).X)));
        Ui.DrawPanel(this, box, _k, MessageWindowView.NarrationFill);
        float pitch = U(PhoneBattleStatesPortrait.PanelPitch);
        for (int i = 0; i < lines.Count; i++)
        {
            float middle = box.Position.Y + U(PhoneBattleStatesPortrait.PanelTop) + (i + 0.5f) * pitch;
            DrawString(_gothic, new Vector2(box.Position.X + U(PhoneBattleStatesPortrait.PanelSide),
                middle + (_gothic.GetAscent(size) - _gothic.GetDescent(size)) / 2), lines[i], fontSize: size, modulate: MessageWindowView.NarrationText);
        }
        if (_waiting)
            PortraitTriangleAt(new Vector2(box.End.X - U(40), box.End.Y - U(34)));
    }

    /// <summary>The click's triangle at <paramref name="at"/>, at the upright plate's size (held landscape's, a little smaller, PB4r's 22).</summary>
    private void PortraitTriangleAt(Vector2 at)
    {
        float k = PhoneTriangle * PhoneBattleStatesPortrait.TriangleSize / PhoneBattleStates.TriangleSize * PhoneBattleStatesPortrait.Larger;
        DrawSetTransform(at * _k, 0, new Vector2(_k, _k) * k);
        Ui.DrawTriangle(this, Vector2.Zero, _k * k);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
    }

    private static PhoneFrame.Box R(Rect2 r) => new(r.Position.X, r.Position.Y, r.Size.X, r.Size.Y);
}
