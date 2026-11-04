using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The retreat question on the phone held landscape (docs/ui/phone-build.md, step L3; docs/ui/phone.md, §17, LB2r1): the
// house's callout in the frame beside the left rail's Retreat, its tip at the button, which is lit; the question and the
// note over a rule, the answers side by side a thumb's height, Retreat red and last. The screen dimmed, the left rail's
// panel with Retreat alone over it (the other commands are gone once Retreat is chosen). A tap off the callout is a cancel
// (Cease Retreat), as on the tablet. In the layer's layout pixels (the frame's); the places are PhoneBattle's.
// Held upright (docs/ui/phone-build.md, step P3; docs/ui/phone.md, §6, PB3r) the same callout without its tip in the
// screen's middle across the board, the whole screen dimmed 65% under it, Retreat too (owner, 2026-11-04; it stood over
// the deck's right column beside Retreat). In the board's layout pixels; the places are PhoneBattleUpright's.
internal sealed partial class BattleDialogLayer
{
    private PhoneFrame? _phone;                         // the phone held either way: the question beside Retreat
    private float _height = 800;                        // upright the board's height (layout pixels)

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

    /// <summary>A box of the board's layout pixels as a rectangle (upright).</summary>
    private static Rect2 B(PhoneFrame.Box b) => new(b.X, b.Y, b.W, b.H);

    /// <summary>The question upright: in the screen's middle (the screen in the board's layout pixels, as the dim covers it).</summary>
    private PhoneFrame.Box UprightQuestion()
    {
        var f = _phone!;
        return PhoneBattleUpright.RetreatQuestion(-f.Room.Y / f.Scale, (f.Screen.H - f.Room.Y) / f.Scale);
    }

    /// <summary>The dim upright, stronger than beside the button: the question stands alone in the middle (owner, 2026-11-04).</summary>
    private static readonly Color UprightDim = new(0, 0, 0, 0.65f);

    /// <summary>The callout's type and insides in layout pixels, held landscape (LB2r1) or upright (PB3r).</summary>
    private readonly record struct Callout(float OnButton, float TipHalf, float TipLength, float TextIn, float TextOut, float QuestionTop,
        float QuestionText, float NoteTop, float NoteText, float Rule, float RuleIn, float AnswerText, float RetreatText);

    private void DrawPhone(PhoneFrame f)
    {
        float s = _scale;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(s, s));               // in layout pixels below
        Rect2? retreat;
        Callout c;
        if (f.IsUpright)
        {
            static float U(float reference) => PhoneBattleUpright.Layout(reference);
            DrawRect(new Rect2(0, -f.Room.Y / f.Scale, PhoneBattleUpright.Width, f.Screen.H / f.Scale), UprightDim);   // the whole screen
            retreat = null;                                                 // the question alone in the middle, Retreat under the dim
            c = new Callout(U(PhoneBattleUpright.RetreatText), 0, 0,
                U(PhoneBattleUpright.QuestionIn), U(PhoneBattleUpright.QuestionIn + 20), U(PhoneBattleUpright.QuestionTop),
                U(PhoneBattleUpright.QuestionText), U(PhoneBattleUpright.NoteTop), U(PhoneBattleUpright.NoteText),
                U(PhoneBattleUpright.QuestionRule), U(24), U(PhoneBattleUpright.AnswerText), U(PhoneBattleUpright.RetreatText));
        }
        else
        {
            DrawRect(L(f.Screen), Dim);
            Ui.DrawPanel(this, L(PhoneBattle.LeftPanel(f)), s);
            retreat = L(PhoneRetreat(f));
            c = new Callout(P(PhoneBattle.AnswerText + 2), P(PhoneBattle.TipHalf), P(PhoneBattle.TipLength), P(40), P(60),
                P(PhoneBattle.QuestionTop), P(PhoneBattle.QuestionText), P(PhoneBattle.NoteTop), P(PhoneBattle.NoteText),
                P(PhoneBattle.QuestionRule), P(30), P(PhoneBattle.AnswerText), P(PhoneBattle.RetreatText));
        }
        var box = Box;
        if (retreat is { } button)
        {
            Ui.DrawRedButton(this, button, true, false, s);                 // lit while it asks
            DrawRect(button.Grow(P(5)), Ui.HoverLine, false, Lacquer.Line(P(5), s));
            int warning = Enumerable.Range(0, _dialog!.Items.Count).FirstOrDefault(i => _dialog.Items[i].Warning, -1);
            if (warning >= 0)
                PhoneCentred(_brush, _labels[warning], c.OnButton, button, Color.Color8(250, 226, 160));
        }
        DrawRect(new Rect2(box.Position + new Vector2(P(6), P(8)), box.Size), new Color(0, 0, 0, 0.3f));
        Ui.DrawPanel(this, box, s);
        if (retreat is { } at)                                              // the tip at the button it stands beside
        {
            float cy = at.GetCenter().Y;
            DrawColoredPolygon([new(box.Position.X + 2, cy - c.TipHalf), new(box.Position.X - c.TipLength, cy), new(box.Position.X + 2, cy + c.TipHalf)],
                Ui.Gold);
        }
        if (GetThemeDefaultFont() is { } font)
        {
            float left = box.Position.X + c.TextIn;
            int question = (int)Math.Round(c.QuestionText), note = (int)Math.Round(c.NoteText);
            DrawString(font, new Vector2(left, box.Position.Y + c.QuestionTop + font.GetAscent(question)), _dialog!.Question,
                HorizontalAlignment.Left, box.Size.X - c.TextOut, question, Ui.Text);
            DrawString(font, new Vector2(left, box.Position.Y + c.NoteTop + font.GetAscent(note)), _dialog.Note,
                HorizontalAlignment.Left, box.Size.X - c.TextOut, note, Ui.Quiet);
            float y = box.Position.Y + c.Rule;
            DrawLine(new Vector2(box.Position.X + c.RuleIn, y), new Vector2(box.End.X - c.RuleIn, y), Ui.DimGold, Lacquer.Line(1, s));
            for (int i = 0; i < _dialog.Items.Count; i++)
            {
                var r = ItemRect(i);
                bool hot = i == _hot || i == _pressed;
                bool warn = _dialog.Items[i].Warning;
                if (warn)
                    Ui.DrawRedButton(this, r, hot, false, s);
                else
                    Ui.DrawRowBox(this, r, hot, s);
                PhoneCentred(warn ? _brush : font, _labels[i], warn ? c.RetreatText : c.AnswerText, r,
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
