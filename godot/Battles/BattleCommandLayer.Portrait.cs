using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The deck on the phone held upright (docs/ui/phone-build.md, step P3; docs/ui/phone.md, §6, PB3): at the board's foot under
// the cards and Skip's strip — in its heading the acting unit's name at the left, TARGET and the target's name at the right,
// over a rule; the acting unit's commands in the left column from the foot up (Retreat on top), the target's attacks in the
// right one, the first at the thumb, the damage forecast on its strip. Drawn as the tablet's rails, in the board's layout
// pixels; the places are PhoneBattlePortrait's. The deck's panel is the board's: it stays, empty, while an exchange plays (PB3x).
internal sealed partial class BattleCommandLayer
{
    private float _height = 800;                        // the board's height (layout pixels): upright the deck's foot

    /// <summary>The deck's buttons: the commands from the left column's foot up, the attacks from the right one's.</summary>
    private IEnumerable<(Rect2 Rect, string Text, FigtSelChoice Choice, (bool Right, int Index) Place)> DeckButtons()
    {
        static Rect2 R(PhoneFrame.Box b) => new(b.X, b.Y, b.W, b.H);
        var left = PhoneBattlePortrait.CommandRows(_height, _railCommands.Count);
        for (int i = 0; i < _railCommands.Count; i++)
            yield return (R(left[i]), _railCommands[i].Text, _railCommands[i].Choice, (false, i));
        var right = PhoneBattlePortrait.AttackRows(_height, _banners.Count);
        for (int i = 0; i < _banners.Count; i++)
            yield return (R(right[i]), _banners[i].Text, _banners[i].Choice, (true, i));
    }

    /// <summary>The deck's heading, its rule and its rows as the tablet's (the panel under them is the board's).</summary>
    private void DrawDeck(Font? font)
    {
        float s = _scale;
        static float L(float reference) => PhoneBattlePortrait.Layout(reference);
        static Rect2 Line(bool right, float inset, float height) =>
            HeadingRect(PhoneBattlePortrait.HeadingLine(right, inset, PhoneBattlePortrait.HeadingMiddle, height));
        DrawSetTransform(Vector2.Zero, 0, new Vector2(s, s));               // in layout pixels below
        if (font is not null && _state is { } state)
        {
            LeftLine(_brush, BattleBoard.Card(state, _commands!.Acting)?.Name ?? "", L(PhoneBattlePortrait.ActingText),
                Line(false, PhoneBattlePortrait.ActingIn, 70), Ui.Name);
            LeftLine(_cinzel, "TARGET", L(PhoneBattlePortrait.TargetTitleText), Line(true, PhoneBattlePortrait.TargetTitleIn, 50), Ui.Subtitle);
            if (_target is { } t)
            {
                LeftLine(_brush, BattleBoard.Card(state, t)?.Name ?? "", L(PhoneBattlePortrait.TargetText),
                    Line(true, PhoneBattlePortrait.TargetIn, 70), TargetName);
                if (_banners.Count == 0)
                {
                    var (x, w) = PhoneBattlePortrait.Column(true);
                    var foot = PhoneBattlePortrait.AttackRows(_height, 1)[0];
                    Centred(font, "No attack reaches it", P(PhoneNoAttack), new Rect2(x, foot.Y, w, foot.H), Ui.Quiet);
                }
            }
            float y = PhoneBattlePortrait.RuleY;
            DrawLine(new Vector2(L(PhoneBattlePortrait.RuleIn), y), new Vector2(PhoneBattlePortrait.Width - L(PhoneBattlePortrait.RuleIn), y),
                Ui.DimGold, Lacquer.Line(1, s));
            float rows = RowSize(font);
            foreach (var (rect, text, choice, place) in RailButtons())
            {
                bool hot = place == _hotRail;
                if (place.Right)
                    AttackButton(rect, text, hot, font, DamageOf(choice));
                else if (choice.Command == FigtSelCommand.Retreat)
                {
                    Ui.DrawRedButton(this, rect, hot, false, s);
                    Centred(font, text, rows, rect, Color8(250, 226, 160));
                }
                else
                {
                    Ui.DrawRowBox(this, rect, hot, s);
                    Centred(font, text, rows, rect, Ui.Text);
                }
            }
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    private static Rect2 HeadingRect(PhoneFrame.Box b) => new(b.X, b.Y, b.W, b.H);

    /// <summary><paramref name="text"/> from <paramref name="r"/>'s left at <paramref name="size"/>, smaller if it would not fit.</summary>
    private void LeftLine(Font font, string text, float size, Rect2 r, Color colour)
    {
        int px = (int)size;
        while (px > 11 && font.GetStringSize(text, fontSize: px).X > r.Size.X - 10)
            px--;
        DrawString(font, new Vector2(r.Position.X, Lacquer.Baseline(r, font, px)), text, HorizontalAlignment.Left, -1, px, colour);
    }
}
