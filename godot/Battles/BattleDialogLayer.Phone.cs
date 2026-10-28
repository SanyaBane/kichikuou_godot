using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The retreat question on the phone held landscape (docs/ui/phone-build.md, step L3; docs/ui/phone.md, §17, LB2r1): the
// house's callout in the frame beside the left rail's Retreat, its tip at the button, which is lit; the question and the
// note over a rule, the answers side by side a thumb's height, Retreat red and last. The screen dimmed, the left rail's
// panel with Retreat alone over it (the other commands are gone once Retreat is chosen). A tap off the callout is a cancel
// (Cease Retreat), as on the tablet. In the layer's layout pixels (the frame's); the places are PhoneBattle's.
internal sealed partial class BattleDialogLayer
{
    private PhoneFrame? _phone;                         // the phone held landscape: the question beside Retreat

    /// <summary>Retreat's button on the left rail when the question opened (layout pixels; the command layer's), or null.</summary>
    public Func<Rect2?>? RetreatAt { get; set; }

    /// <summary>A box of the canvas's units in the layer's layout pixels (the frame's).</summary>
    private Rect2 L(PhoneFrame.Box b)
    {
        var f = _phone!;
        return new Rect2((b.X - f.Frame.X) / f.Scale, (b.Y - f.Frame.Y) / f.Scale, b.W / f.Scale, b.H / f.Scale);
    }

    /// <summary>A length of <paramref name="reference"/> reference pixels in layout pixels.</summary>
    private float P(float reference) => _phone!.Px(reference) / _phone.Scale;

    /// <summary>Retreat's button in the canvas's units: the rail's, or the top of five commands' when none was laid out.</summary>
    private PhoneFrame.Box PhoneRetreat(PhoneFrame f)
    {
        if (RetreatAt?.Invoke() is not { } r)
            return PhoneBattle.CommandRows(f, 5)[4];
        return new PhoneFrame.Box(f.Frame.X + r.Position.X * f.Scale, f.Frame.Y + r.Position.Y * f.Scale, r.Size.X * f.Scale, r.Size.Y * f.Scale);
    }

    private PhoneFrame.Box PhoneQuestion(PhoneFrame f) => PhoneBattle.RetreatQuestion(f, PhoneRetreat(f));

    private void DrawPhone(PhoneFrame f)
    {
        float s = _scale;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(s, s));               // in layout pixels below
        DrawRect(L(f.Screen), Dim);
        Ui.DrawPanel(this, L(PhoneBattle.LeftPanel(f)), s);
        var retreat = L(PhoneRetreat(f));
        Ui.DrawRedButton(this, retreat, true, false, s);                    // lit while it asks
        DrawRect(retreat.Grow(P(5)), Ui.HoverLine, false, Lacquer.Line(P(5), s));
        int warning = Enumerable.Range(0, _dialog!.Items.Count).FirstOrDefault(i => _dialog.Items[i].Warning, -1);
        if (warning >= 0)
            PhoneCentred(_brush, _labels[warning], P(PhoneBattle.AnswerText + 2), retreat, Color.Color8(250, 226, 160));
        var box = Box;
        float cy = retreat.GetCenter().Y;
        Vector2[] tip = [new(box.Position.X + 2, cy - P(PhoneBattle.TipHalf)), new(box.Position.X - P(PhoneBattle.TipLength), cy),
            new(box.Position.X + 2, cy + P(PhoneBattle.TipHalf))];
        DrawRect(new Rect2(box.Position + new Vector2(P(6), P(8)), box.Size), new Color(0, 0, 0, 0.3f));
        Ui.DrawPanel(this, box, s);
        DrawColoredPolygon(tip, Ui.Gold);
        if (GetThemeDefaultFont() is { } font)
        {
            float left = box.Position.X + P(40);
            int question = (int)Math.Round(P(PhoneBattle.QuestionText)), note = (int)Math.Round(P(PhoneBattle.NoteText));
            DrawString(font, new Vector2(left, box.Position.Y + P(PhoneBattle.QuestionTop) + font.GetAscent(question)), _dialog.Question,
                HorizontalAlignment.Left, box.Size.X - P(60), question, Ui.Text);
            DrawString(font, new Vector2(left, box.Position.Y + P(PhoneBattle.NoteTop) + font.GetAscent(note)), _dialog.Note,
                HorizontalAlignment.Left, box.Size.X - P(60), note, Ui.Quiet);
            float y = box.Position.Y + P(PhoneBattle.QuestionRule);
            DrawLine(new Vector2(box.Position.X + P(30), y), new Vector2(box.End.X - P(30), y), Ui.DimGold, Lacquer.Line(1, s));
            for (int i = 0; i < _dialog.Items.Count; i++)
            {
                var r = ItemRect(i);
                bool hot = i == _hot || i == _pressed;
                bool warn = _dialog.Items[i].Warning;
                if (warn)
                    Ui.DrawRedButton(this, r, hot, false, s);
                else
                    Ui.DrawRowBox(this, r, hot, s);
                PhoneCentred(warn ? _brush : font, _labels[i], P(warn ? PhoneBattle.RetreatText : PhoneBattle.AnswerText), r,
                    warn ? Color.Color8(250, 226, 160) : Ui.Text);
            }
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary><paramref name="text"/> centred in <paramref name="r"/> at <paramref name="size"/>, smaller if it would not fit.</summary>
    private void PhoneCentred(Font font, string text, float size, Rect2 r, Color colour)
    {
        int px = (int)Math.Round(size);
        while (px > 11 && font.GetStringSize(text, fontSize: px).X > r.Size.X - P(24))
            px--;
        DrawString(font, new Vector2(r.Position.X, Lacquer.Baseline(r, font, px)), text, HorizontalAlignment.Center, r.Size.X, px, colour);
    }
}
