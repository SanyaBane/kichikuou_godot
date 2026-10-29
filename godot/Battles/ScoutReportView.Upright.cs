using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The scout report on the phone held upright (docs/ui/phone-build.md, step P4; docs/ui/phone.md, §11, PB4s): a screen of its
// own at the phone's type — Kanami and her report on top, the approaching commanders' cards under it as the enemy attack's,
// the front row first, Rance's answer at the foot on the bar (with Maris's comment, his a row higher and hers at the foot,
// mirrored). Drawn over the whole screen in the frame's layout pixels (PhoneUnits), the places PhoneBattleStatesUpright's.
internal sealed partial class ScoutReportView
{
    private void DrawUpright(ScoutReport report, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        _sheet.DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        if (_portrait is not null)
            PhonePortrait(_portrait, u.Rect(PhoneBattleStatesUpright.NinjaPortrait(f)));

        // Her report in three lines (PB4s): the heading and the army's name bold, the totals under them.
        var balloon = u.Rect(PhoneBattleStatesUpright.ReportBalloon(f));
        var ink = SpeechBalloon.DrawBalloon(_sheet, balloon, tailLeft: true, u.Px(PhoneBattleStatesUpright.TailDepth) / 18, false,
            u.Px(PhoneBattleStatesUpright.ReportTail));
        int size = u.Size(PhoneBattleStatesUpright.ReportText);
        var line = balloon.Position + new Vector2(u.Px(PhoneBattleStatesUpright.TextSide), u.Px(PhoneBattleStatesUpright.TextTop));
        float width = balloon.Size.X - 2 * u.Px(PhoneBattleStatesUpright.TextSide);
        foreach (var (text, bold) in new[] { ("The Approaching Enemy", true), ($"[ {report.Army} ]", true),
                     ($"Commanders {report.Commanders}   Total troops {Number(report.Troops)}", false) })
        {
            Font font = bold ? _bold : _font!;
            _sheet.DrawString(font, line + new Vector2(0, font.GetAscent(size)), text, HorizontalAlignment.Left, width, size, ink);
            line.Y += size * PhoneBattleStatesUpright.LinePitch;
        }

        int caption = u.Size(PhoneBattleStatesUpright.RowCaption);
        for (int row = 0; row < 2; row++)
        {
            var (cx, cy) = PhoneBattleStatesUpright.RowCaptionAt(f, row);
            string text = row == 0 ? "FRONT ROW" : "BACK ROW";
            float w = _cinzel.GetStringSize(text, fontSize: caption).X;
            _sheet.DrawString(_cinzel, new Vector2(cx / u.K - w / 2, cy / u.K + (_cinzel.GetAscent(caption) - _cinzel.GetDescent(caption)) / 2),
                text, fontSize: caption, modulate: Ui.Subtitle);
        }
        for (int slot = 1; slot <= 4; slot++)
            PhoneCard(u, u.Rect(PhoneBattleStatesUpright.ScoutCard(f, slot)), slot, report.Places[slot - 1]);

        if (report.Reply != "")
        {
            bool commented = report.Comment != "";
            if (_rance is not null)
                PhonePortrait(_rance, u.Rect(PhoneBattleStatesUpright.SpeakerPortrait(f, right: false, commented)));
            UprightSaying(u, f, report.Reply, comment: false, commented);
            if (commented)
            {
                if (_advisor is not null)
                    PhonePortrait(_advisor, u.Rect(PhoneBattleStatesUpright.SpeakerPortrait(f, right: true, commented)));
                UprightSaying(u, f, report.Comment, comment: true, commented);
            }
        }
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>Rance's answer (or Maris's comment) in our balloon beside its speaker, one line per line of the script's, its tail at the speaker.</summary>
    private void UprightSaying(PhoneUnits u, PhoneFrame f, string said, bool comment, bool commented)
    {
        string[] lines = said.Split('\n');
        int size = u.Size(PhoneBattleStatesUpright.ReplyText);
        float pitch = size * PhoneBattleStatesUpright.LinePitch;
        var box = u.Rect(PhoneBattleStatesUpright.ReplyBalloon(f, comment, commented, ((lines.Length - 1) * pitch + size) * u.K));
        var ink = SpeechBalloon.DrawBalloon(_sheet, box, tailLeft: !comment, u.Px(PhoneBattleStatesUpright.TailDepth) / 18, false,
            u.Px(PhoneBattleStatesUpright.ReplyTail));
        var line = box.Position + new Vector2(u.Px(PhoneBattleStatesUpright.TextSide), u.Px(PhoneBattleStatesUpright.TextTop));
        foreach (string text in lines)
        {
            _sheet.DrawString(_font!, line + new Vector2(0, _font!.GetAscent(size)), text, HorizontalAlignment.Left,
                box.Size.X - 2 * u.Px(PhoneBattleStatesUpright.TextSide), size, ink);
            line.Y += pitch;
        }
    }
}
