using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The scout report on the phone held landscape (docs/ui/phone-build.md, step L4; docs/ui/phone.md, §17, LB3s): across the
// whole width between the hole's strips, at the phone's type — the ninja and her report on top, the approaching
// commanders' cards under it as the enemy attack's (the place on a gold diamond, the face at the right, TROOPS at the
// foot), the front row left; Rance's answer at the foot left, Maris's comment, when she has one, at the foot right. Drawn
// over the whole screen in the frame's layout pixels (PhoneUnits), the places PhoneBattleStates'.
internal sealed partial class ScoutReportView
{
    private PhoneFrame? _phone;
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600);

    private void DrawPhone(ScoutReport report, PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        _sheet.DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        if (_portrait is not null)
            PhonePortrait(_portrait, u.Rect(PhoneBattleStates.NinjaPortrait(f)));

        var balloon = u.Rect(PhoneBattleStates.ReportBalloon(f));
        var ink = SpeechBalloon.DrawBalloon(_sheet, balloon, tailLeft: true, u.Px(PhoneBattleStates.TailDepth) / 18, false,
            u.Px(PhoneBattleStates.ReportTail));
        int size = u.Size(PhoneBattleStates.ReportText);
        var line = balloon.Position + new Vector2(u.Px(PhoneBattleStates.TextSide), u.Px(PhoneBattleStates.TextTop));
        float width = balloon.Size.X - 2 * u.Px(PhoneBattleStates.TextSide);
        _sheet.DrawString(_bold, line + new Vector2(0, _bold.GetAscent(size)), $"The Approaching Enemy  [ {report.Army} ]",
            HorizontalAlignment.Left, width, size, ink);
        line.Y += size * PhoneBattleStates.LinePitch;
        _sheet.DrawString(_font!, line + new Vector2(0, _font!.GetAscent(size)),
            $"Commanders {report.Commanders}      Total troops {Number(report.Troops)}", HorizontalAlignment.Left, width, size, ink);

        int caption = u.Size(PhoneBattleStates.RowCaption);
        for (int column = 0; column < 2; column++)
        {
            var (cx, cy) = PhoneBattleStates.RowCaptionAt(f, column);
            string text = column == 0 ? "FRONT ROW" : "BACK ROW";
            float w = _cinzel.GetStringSize(text, fontSize: caption).X;
            _sheet.DrawString(_cinzel, new Vector2(cx / u.K - w / 2, cy / u.K + (_cinzel.GetAscent(caption) - _cinzel.GetDescent(caption)) / 2),
                text, fontSize: caption, modulate: Ui.Subtitle);
        }
        for (int slot = 1; slot <= 4; slot++)
            PhoneCard(u, u.Rect(PhoneBattleStates.ScoutCard(f, slot)), slot, report.Places[slot - 1]);

        if (report.Reply != "")
        {
            if (_rance is not null)
                PhonePortrait(_rance, u.Rect(PhoneBattleStates.SpeakerPortrait(f, right: false)));
            PhoneSaying(u, f, report.Reply, comment: false);
            if (report.Comment != "")
            {
                if (_advisor is not null)
                    PhonePortrait(_advisor, u.Rect(PhoneBattleStates.SpeakerPortrait(f, right: true)));
                PhoneSaying(u, f, report.Comment, comment: true);
            }
        }
        _sheet.DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private void PhonePortrait(ImageTexture picture, Rect2 area)
    {
        DrawShadow(area, 1);
        _sheet.DrawTextureRect(picture, area, false);
        _sheet.DrawRect(area, Ui.DimGold, false, Lacquer.Line(1, 1));
    }

    /// <summary>Rance's answer (or Maris's comment) in our balloon at the foot, one line per line of the script's, its tail at the speaker.</summary>
    private void PhoneSaying(PhoneUnits u, PhoneFrame f, string said, bool comment)
    {
        string[] lines = said.Split('\n');
        int size = u.Size(PhoneBattleStates.ReplyText);
        float pitch = size * PhoneBattleStates.LinePitch;
        var box = u.Rect(PhoneBattleStates.ReplyBalloon(f, comment, ((lines.Length - 1) * pitch + size) * u.K));
        var ink = SpeechBalloon.DrawBalloon(_sheet, box, tailLeft: !comment, u.Px(PhoneBattleStates.TailDepth) / 18, false,
            u.Px(PhoneBattleStates.ReplyTail));
        var line = box.Position + new Vector2(u.Px(PhoneBattleStates.TextSide), u.Px(PhoneBattleStates.TextTop));
        foreach (string text in lines)
        {
            _sheet.DrawString(_font!, line + new Vector2(0, _font!.GetAscent(size)), text, HorizontalAlignment.Left,
                box.Size.X - 2 * u.Px(PhoneBattleStates.TextSide), size, ink);
            line.Y += pitch;
        }
    }

    /// <summary>A scouted place's card as the enemy attack's (LA2): the place on a gold diamond, name, type, TROOPS and the figure, the face at the right.</summary>
    private void PhoneCard(PhoneUnits u, Rect2 card, int slot, ScoutedCommander? commander)
    {
        if (commander is not null)
        {
            PhoneCommanderCard.Draw(_sheet, u, Ui, card, slot, commander, _font!, _cinzel, _faces, _crops, PhoneBattleStates.CardFace);
            return;
        }
        _sheet.DrawRect(card, Ui.Panel with { A = 1 });
        _sheet.DrawRect(card, Ui.DimGold, false, Lacquer.Line(1, 1));
        int text = u.Size(PhoneBattleStates.CardName);
        string empty = $"{slot}  empty";
        float w = _font!.GetStringSize(empty, fontSize: text).X;
        _sheet.DrawString(_font, card.GetCenter() + new Vector2(-w / 2, (_font.GetAscent(text) - _font.GetDescent(text)) / 2), empty,
            fontSize: text, modulate: Ui.Quiet);
    }
}
