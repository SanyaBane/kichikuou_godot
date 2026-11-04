using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The info boxes on the phone held landscape (docs/ui/phone-build.md, step L4; docs/ui/phone.md, §17, LB3r and LB3b): the
// results plate in the frame's middle over the dimmed scene and board, larger than the PC's (the title 5.7 mm, the damages
// 6.8 mm), a level-up's rows on the same plate; our narration panel — a fall's box too — the same way; a defence's ribbon
// larger with the cards and its caption at 2.4 mm over the scene's foot (DrawDefence). In the frame's layout pixels, the
// places PhoneBattleStates'.
internal sealed partial class BattleInfoLayer
{
    private static bool Phone => Device.Layout == DeviceLayout.PhoneLandscape;

    /// <summary>A length of <paramref name="reference"/> reference pixels in layout pixels.</summary>
    private static float P(float reference) => PhoneBattle.Layout(reference);

    private static Rect2 R(PhoneFrame.Box b) => new(b.X, b.Y, b.W, b.H);

    /// <summary>The scene and the board dimmed under the plate or the panel (LB3r).</summary>
    private const float PhoneVeil = 0.55f;

    /// <summary>The click's triangle on the phone, this much larger than the PC's.</summary>
    private const float PhoneTriangle = 2.2f;

    private void DrawPhoneResults(BattleResults r)
    {
        DrawRect(new Rect2(0, 0, 1280, 800), new Color(0, 0, 0, PhoneVeil));
        var (_, _, rowsWidth) = PhoneBattleStates.LevelRows(PhoneBattleStates.ResultsPlate(0), 0);
        var levels = new LevelMetrics(Px(PhoneBattleStates.LevelLabel), Px(PhoneBattleStates.LevelName), Px(PhoneBattleStates.LevelGains),
            P(PhoneBattleStates.LevelLeft), P(PhoneBattleStates.LevelRight), P(PhoneBattleStates.LevelPad), P(PhoneBattleStates.LevelGap),
            P(PhoneBattleStates.LevelBetween), rowsWidth, WholeNames: true);
        var (rows, column, rowsHeight) = LevelRows(r.LevelUps, levels);
        var box = PhoneBattleStates.ResultsPlate(rowsHeight);
        float k = PhoneBattleStates.HeadScale(rowsHeight);                  // the head closed up for four long rows
        var plate = R(box);
        Ui.DrawPanel(this, plate, _k);
        float y = plate.Position.Y, cx = plate.GetCenter().X;
        int title = Px(PhoneBattleStates.TitleText);
        Mid(_brush, cx + 2, y + k * P(PhoneBattleStates.TitleMiddle) + 2, r.Title, title, Colors.Black);
        Mid(_brush, cx, y + k * P(PhoneBattleStates.TitleMiddle), r.Title, title, Ui.Name);
        if (r.Reason.Length > 0)
        {
            int reason = Px(PhoneBattleStates.ReasonText);
            Mid(_semi, cx, y + k * P(PhoneBattleStates.ReasonMiddle), Fit(_semi, r.Reason.ToUpperInvariant(), reason, plate.Size.X - P(120)), reason,
                Ui.Subtitle);
        }
        float side = P(PhoneBattleStates.DividerSide);
        Ui.DrawDivider(this, plate.Position.X + side, plate.End.X - side, y + k * P(PhoneBattleStates.Divider), _k);
        float quarter = plate.Size.X / 4;
        int army = Px(PhoneBattleStates.ArmyText), damages = Px(PhoneBattleStates.DamagesText), word = Px(PhoneBattleStates.DamagesWordText);
        foreach (var (name, value, ax) in new[] { (r.PlayerArmy, r.PlayerDamages, cx - quarter), (r.EnemyArmy, r.EnemyDamages, cx + quarter) })
        {
            Mid(_semi, ax, y + k * P(PhoneBattleStates.ArmyMiddle), Fit(_semi, name.ToUpperInvariant(), army, quarter * 2 - P(60)), army, Ui.Label);
            Mid(_bold, ax, y + k * P(PhoneBattleStates.DamagesMiddle), Figures.Number(value), damages, Ui.Minus);
            Mid(_semi, ax, y + k * P(PhoneBattleStates.DamagesWordMiddle), "DAMAGES", word, Ui.Subtitle);
        }
        DrawLine(new Vector2(cx, y + k * P(PhoneBattleStates.RuleTop)), new Vector2(cx, y + k * P(PhoneBattleStates.RuleFoot)), Ui.DimGold,
            Lacquer.Line(1, _k));
        Mid(_bold, cx, y + k * P(PhoneBattleStates.ExpMiddle), (r.Exp + " EXP obtained").ToUpperInvariant(), Px(PhoneBattleStates.ExpText), Ui.Text);
        if (rows.Count > 0)
        {
            Ui.DrawDivider(this, plate.Position.X + side, plate.End.X - side, y + k * P(720) + P(PhoneBattleStates.LevelDivider), _k);
            var (rx, ry, _) = PhoneBattleStates.LevelRows(box, rowsHeight);
            DrawLevelRows(rx, ry, rows, column, levels);
        }
        if (_waiting)
            PhoneTriangleAt(new Vector2(plate.End.X - P(PhoneBattleStates.TriangleIn), plate.End.Y - P(PhoneBattleStates.TriangleUp)));
    }

    /// <summary>Our panel (a fall's box too) in the frame's middle at the plate's type over the dimmed board.</summary>
    private void DrawPhonePanel(IReadOnlyList<string> sentences)
    {
        int size = Px(PhoneBattleStates.PanelText);
        var lines = sentences.SelectMany(s => Wrap(s, P(PhoneBattleStates.PanelTextWidth), size)).ToList();
        if (lines.Count == 0)
            lines.Add("");
        DrawRect(new Rect2(0, 0, 1280, 800), new Color(0, 0, 0, PhoneVeil));
        var box = R(PhoneBattleStates.Panel(lines.Count, lines.Max(l => _gothic.GetStringSize(l, fontSize: size).X)));
        Ui.DrawPanel(this, box, _k, MessageWindowView.NarrationFill);
        float pitch = P(PhoneBattleStates.PanelPitch);
        for (int i = 0; i < lines.Count; i++)
        {
            float middle = box.Position.Y + P(PhoneBattleStates.PanelTop) + (i + 0.5f) * pitch;
            DrawString(_gothic, new Vector2(box.Position.X + P(PhoneBattleStates.PanelSide), middle + (_gothic.GetAscent(size) - _gothic.GetDescent(size)) / 2),
                lines[i], fontSize: size, modulate: MessageWindowView.NarrationText);
        }
        if (_waiting)
            PhoneTriangleAt(new Vector2(box.End.X - P(40), box.End.Y - P(34)));
    }

    /// <summary>The click's triangle at <paramref name="at"/>, at the phone's size.</summary>
    private void PhoneTriangleAt(Vector2 at)
    {
        DrawSetTransform(at * _k, 0, new Vector2(_k, _k) * PhoneTriangle);
        Ui.DrawTriangle(this, Vector2.Zero, _k * PhoneTriangle);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
    }

    /// <summary>A font size of <paramref name="reference"/> reference pixels in layout pixels.</summary>
    private static int Px(float reference) => Math.Max(6, (int)Math.Round(P(reference)));

    /// <summary>A text centred on (<paramref name="cx"/>, <paramref name="middle"/>).</summary>
    private void Mid(Font font, float cx, float middle, string text, int size, Color colour) =>
        Middle(font, cx, middle + (font.GetAscent(size) - font.GetDescent(size)) / 2, text, size, colour);
}
