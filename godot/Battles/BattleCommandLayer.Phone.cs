using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Battles;

// The rails on the phone held landscape (docs/ui/phone-build.md, step L3; docs/ui/phone.md, §12, LB2): out of the frame in
// the black sides, at a finger's size — the acting unit's name on the left rail under the battle's Skip and its commands
// from the foot up, 7.0 mm high (Retreat on top); TARGET and the target's name on the right rail and its attacks from the
// foot up, 14.8 mm high (Melee at the thumb, the damage forecast on its strip). Drawn as the tablet's, in the layer's layout
// pixels (the frame's), the sides lying left of 0 and right of 1280; the places are PhoneBattle's.
internal sealed partial class BattleCommandLayer
{
    private PhoneFrame? _phone;                         // the phone held landscape: the rails in the sides

    /// <summary>The rails' type on the phone, reference pixels: a command's 40, Retreat's 42, "No attack reaches it" 34, a long target name's two lines 44.</summary>
    private const float PhoneRowText = 40, PhoneRetreatText = 42, PhoneNoAttack = 34, PhoneTargetTwo = 44;

    /// <summary>A box of the canvas's units in the layer's layout pixels (the frame's).</summary>
    private Rect2 L(PhoneFrame.Box b)
    {
        var f = _phone!;
        return new Rect2((b.X - f.Frame.X) / f.Scale, (b.Y - f.Frame.Y) / f.Scale, b.W / f.Scale, b.H / f.Scale);
    }

    /// <summary>A length of <paramref name="reference"/> reference pixels in layout pixels.</summary>
    private float P(float reference) => _phone!.Px(reference) / _phone.Scale;

    /// <summary>The phone's rail buttons: the commands from the left side's foot up, the attacks from the right side's.</summary>
    private IEnumerable<(Rect2 Rect, string Text, FigtSelChoice Choice, (bool Right, int Index) Place)> PhoneRailButtons(PhoneFrame f)
    {
        var left = PhoneBattle.CommandRows(f, _railCommands.Count);
        for (int i = 0; i < _railCommands.Count; i++)
            yield return (L(left[i]), _railCommands[i].Text, _railCommands[i].Choice, (false, i));
        var right = PhoneBattle.AttackRows(f, _banners.Count);
        for (int i = 0; i < _banners.Count; i++)
            yield return (L(right[i]), _banners[i].Text, _banners[i].Choice, (true, i));
    }

    /// <summary>Retreat's button on the left rail (layout pixels), or null when the acting unit cannot retreat.</summary>
    public Rect2? RetreatButton => RailButtons().Where(b => b.Choice.Command == FigtSelCommand.Retreat).Select(b => (Rect2?)b.Rect).FirstOrDefault();

    /// <summary>The phone's rails: the panels the sides' height, the names and rules over the rows, the rows as the tablet's.</summary>
    private void DrawPhoneRails(PhoneFrame f, Font? font)
    {
        float s = _scale;
        DrawSetTransform(Vector2.Zero, 0, new Vector2(s, s));               // in layout pixels below
        Ui.DrawPanel(this, L(PhoneBattle.LeftPanel(f)), s);
        Ui.DrawPanel(this, L(PhoneBattle.RightPanel(f)), s);
        if (font is not null && _state is { } state)
        {
            Centred(_brush, BattleBoard.Card(state, _commands!.Acting)?.Name ?? "", P(PhoneBattle.ActingText),
                L(PhoneBattle.LeftLine(f, PhoneBattle.ActingMiddle, 70)), Ui.Name);
            PhoneRule(f, false);
            Centred(_cinzel, "TARGET", P(PhoneBattle.TargetTitleText), L(PhoneBattle.RightLine(f, PhoneBattle.TargetTitleMiddle, 50)), Ui.Subtitle);
            if (_target is { } t)
            {
                PhoneTargetName(f, BattleBoard.Card(state, t)?.Name ?? "");
                if (_banners.Count == 0)
                {
                    Centred(font, "No attack", P(PhoneNoAttack), L(PhoneBattle.RightLine(f, PhoneBattle.RightRule + 50, 50)), Ui.Quiet);
                    Centred(font, "reaches it", P(PhoneNoAttack), L(PhoneBattle.RightLine(f, PhoneBattle.RightRule + 96, 50)), Ui.Quiet);
                }
            }
            PhoneRule(f, true);
            foreach (var (rect, text, choice, place) in RailButtons())
            {
                bool hot = place == _hotRail;
                if (place.Right)
                    AttackButton(rect, text, hot, font, DamageOf(choice));
                else if (choice.Command == FigtSelCommand.Retreat)
                {
                    Ui.DrawRedButton(this, rect, hot, false, s);
                    Centred(_brush, text, P(PhoneRetreatText), rect, Color8(250, 226, 160));
                }
                else
                {
                    Ui.DrawRowBox(this, rect, hot, s);
                    Centred(font, text, P(PhoneRowText), rect, Ui.Text);
                }
            }
        }
        DrawSetTransform(Vector2.Zero, 0, Vector2.One);
    }

    /// <summary>The rule under the acting unit's name (left) or under the target's (right).</summary>
    private void PhoneRule(PhoneFrame f, bool right)
    {
        var line = L(right ? PhoneBattle.RightLine(f, PhoneBattle.RightRule, 0) : PhoneBattle.LeftLine(f, PhoneBattle.LeftRule, 0));
        DrawLine(new Vector2(line.Position.X + P(12), line.Position.Y), new Vector2(line.End.X - P(12), line.Position.Y), Ui.DimGold,
            Lacquer.Line(1, _scale));
    }

    /// <summary>The target's name under TARGET: one line at 56, or two at 44 when one would have to be smaller than 40.</summary>
    private void PhoneTargetName(PhoneFrame f, string name)
    {
        var one = L(PhoneBattle.RightLine(f, PhoneBattle.TargetMiddle, 70));
        if (_brush.GetStringSize(name, fontSize: (int)P(40)).X > one.Size.X - 10 && Split(name) is { } lines)
        {
            Centred(_brush, lines.First, P(PhoneTargetTwo), L(PhoneBattle.RightLine(f, PhoneBattle.TargetMiddle - 24, 50)), TargetName);
            Centred(_brush, lines.Second, P(PhoneTargetTwo), L(PhoneBattle.RightLine(f, PhoneBattle.TargetMiddle + 24, 50)), TargetName);
        }
        else
            Centred(_brush, name, P(PhoneBattle.TargetText), one, TargetName);
    }
}
