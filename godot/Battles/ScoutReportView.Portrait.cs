using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The scout report on the phone held upright (docs/ui/phone-build.md, step P4; docs/ui/phone.md, §11, PB4s): a screen of its
// own at the phone's type — Kanami and her report, the approaching commanders' cards under it as the enemy attack's, the
// front row first, held from above in its bracket (PA1f2a; §33, PB4sf2), the two as one block in the middle of the room
// over Rance's answer (§34, PB4sf2m2), which stands at the foot on the bar (with Maris's comment, his a row higher and hers
// at the foot, mirrored). Drawn over the whole screen in the frame's layout pixels (PhoneUnits), the places PhoneBattleStatesPortrait's.
internal sealed partial class ScoutReportView
{
    private void DrawPortrait(ScoutReport report, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        _sheet.DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        bool replied = report.Reply != "", commented = replied && report.Comment != "";
        float foot = PhoneBattleStatesPortrait.BlockFoot(f, replied, commented);
        if (_portrait is not null)
            PhonePortrait(_portrait, u.Rect(PhoneBattleStatesPortrait.NinjaPortrait(f, foot)));

        // Her report in three lines (PB4s): the heading and the army's name bold, the totals under them.
        var balloon = u.Rect(PhoneBattleStatesPortrait.ReportBalloon(f, foot));
        var ink = SpeechBalloon.DrawBalloon(_sheet, balloon, tailLeft: true, u.Px(PhoneBattleStatesPortrait.TailDepth) / 18, false,
            u.Px(PhoneBattleStatesPortrait.ReportTail));
        int size = u.Size(PhoneBattleStatesPortrait.ReportText);
        var line = balloon.Position + new Vector2(u.Px(PhoneBattleStatesPortrait.TextSide), u.Px(PhoneBattleStatesPortrait.TextTop));
        float width = balloon.Size.X - 2 * u.Px(PhoneBattleStatesPortrait.TextSide);
        foreach (var (text, bold) in new[] { ("The Approaching Enemy", true), ($"[ {report.Army} ]", true),
                     ($"Commanders {report.Commanders}   Total troops {Number(report.Troops)}", false) })
        {
            Font font = bold ? _bold : _font!;
            _sheet.DrawString(font, line + new Vector2(0, font.GetAscent(size)), text, HorizontalAlignment.Left, width, size, ink);
            line.Y += size * PhoneBattleStatesPortrait.LinePitch;
        }

        int caption = u.Size(PhoneBattleStatesPortrait.RowCaption);
        for (int row = 0; row < 2; row++)
        {
            var (cx, cy) = PhoneBattleStatesPortrait.RowCaptionAt(f, row, foot);
            string text = row == 0 ? "FRONT ROW" : "BACK ROW";
            float w = _cinzel.GetStringSize(text, fontSize: caption).X;
            _sheet.DrawString(_cinzel, new Vector2(cx / u.K - w / 2, cy / u.K + (_cinzel.GetAscent(caption) - _cinzel.GetDescent(caption)) / 2),
                text, fontSize: caption, modulate: Ui.Subtitle);
        }
        var (left, right, middle, bracketLine, cards) = PhoneBattleStatesPortrait.Bracket(f, foot);
        FacingBracket.Draw(_sheet, u, Ui, u.K, left, right, middle, bracketLine, cards, down: false);
        for (int slot = 1; slot <= 4; slot++)
            PhoneCard(u, u.Rect(PhoneBattleStatesPortrait.ScoutCard(f, slot, foot)), slot, report.Places[slot - 1]);

        if (replied)
        {
            if (_rance is not null)
                PhonePortrait(_rance, u.Rect(PhoneBattleStatesPortrait.SpeakerPortrait(f, right: false, commented)));
            PortraitSaying(u, f, report.Reply, comment: false, commented);
            if (commented)
            {
                if (_advisor is not null)
                    PhonePortrait(_advisor, u.Rect(PhoneBattleStatesPortrait.SpeakerPortrait(f, right: true, commented)));
                PortraitSaying(u, f, report.Comment, comment: true, commented);
            }
        }
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Rance's answer (or Maris's comment) in our balloon beside its speaker, one line per line of the script's, its tail at the speaker.</summary>
    private void PortraitSaying(PhoneUnits u, PhoneFrame f, string said, bool comment, bool commented)
    {
        string[] lines = said.Split('\n');
        int size = u.Size(PhoneBattleStatesPortrait.ReplyText);
        float pitch = size * PhoneBattleStatesPortrait.LinePitch;
        var box = u.Rect(PhoneBattleStatesPortrait.ReplyBalloon(f, comment, commented, ((lines.Length - 1) * pitch + size) * u.K));
        var ink = SpeechBalloon.DrawBalloon(_sheet, box, tailLeft: !comment, u.Px(PhoneBattleStatesPortrait.TailDepth) / 18, false,
            u.Px(PhoneBattleStatesPortrait.ReplyTail));
        var line = box.Position + new Vector2(u.Px(PhoneBattleStatesPortrait.TextSide), u.Px(PhoneBattleStatesPortrait.TextTop));
        foreach (string text in lines)
        {
            _sheet.DrawString(_font!, line + new Vector2(0, _font!.GetAscent(size)), text, HorizontalAlignment.Left,
                box.Size.X - 2 * u.Px(PhoneBattleStatesPortrait.TextSide), size, ink);
            line.Y += pitch;
        }
    }
}
