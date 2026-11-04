using Godot;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.Strategy;

// The Subordinates screen on the phone held landscape (docs/ui/phone-build.md, step L5; docs/ui/phone.md, §13, LU1 with the
// owner's change): the ledger fills the frame under its tabs; the left side holds the chosen unit's Add New Troops from the
// foot up, the dearest on top; the right side the title, the units ready, the date and the budget, then Talk (the unit's
// face), Replenish and End at the thumb. A troop order's question stands in a callout beside its button (LU1q: left of
// Replenish, right of a step), Yes | No as on the PC (a way back stands right of the action, owner, 2026-10-30). The unit's picture comes by a long press on its row. Drawn in the
// frame's layout pixels over the whole screen (PhoneUnits), the places PhoneLedger's.
internal sealed partial class SubordinatesView
{
    private PhoneFrame? _phone;

    /// <summary>A box of the canvas's units in the layout pixels the screen draws in on the phone.</summary>
    private Rect2 Box(PhoneFrame.Box b) => new(b.X / _k, b.Y / _k, b.W / _k, b.H / _k);

    /// <summary>
    /// A troop order's question on the phone, in the callout's reference pixels: Replenish's left of its button (LU1q), a
    /// step's right of it, each standing on its button's foot.
    /// </summary>
    private void ShowPhoneQuestion(PhoneFrame f, Rect2 button, bool replenish)
    {
        float r = _k / f.Unit;                                  // layout pixels to the reference's
        var cell = new Rect2(button.Position * r, button.Size * r);
        if (f.IsPortrait)
        {
            _callout.Show(SubordinatesScreen.Question, cell, waiting: false);   // PU1q: under its button, over it near the foot
            return;
        }
        PhoneFrame.Box room = replenish
            ? PhoneLedger.ReplenishQuestion(f)
            : new PhoneFrame.Box(button.End.X * _k + f.Px(40), f.LeftRoom.Y, f.Px(620), button.End.Y * _k - f.LeftRoom.Y);
        var beside = new Rect2(room.X / f.Unit, room.Y / f.Unit, room.W / f.Unit, room.H / f.Unit);
        _callout.Show(SubordinatesScreen.Question, cell, waiting: false, beside);
    }

    private void DrawPhone(Control c, PhoneFrame f)
    {
        if (f.IsPortrait)
        {
            DrawPortrait(c, f);
            return;
        }
        var u = new PhoneUnits(f);
        _tabs.Draw(c, _k, _table);
        if (_panel is not { } panel)
            return;

        var left = Box(PhoneLedger.LeftLine(f, 0, 0));
        float lx = left.Position.X + u.Px(4), lw = left.Size.X - u.Px(8);
        Top(c, _brush, "Orders", u.Size(PhoneLedger.OrdersText), Ui.Name, lx, u.Px(PhoneLedger.OrdersTop), lw);
        Top(c, _cinzel, panel.Name.ToUpperInvariant(), u.Size(PhoneLedger.UnitText), Ui.Text, lx, u.Px(PhoneLedger.UnitTop), lw);
        if (panel.AddOffered)
        {
            bool any = panel.Steps.Any(s => s.Enabled) && !Busy;
            // The caption over the steps it heads (owner, 2026-11-04), StepsUnder over the highest: at AddTop when they fill the side.
            var steps = PhoneLedger.Steps(f, panel.Steps.Count);
            float addTop = steps.Count == 0 ? u.Px(PhoneLedger.AddTop)
                : steps.Min(s => Box(s).Position.Y) - u.Px(PhoneLedger.StepsUnder);
            Top(c, _cinzel, "Add New Troops", u.Size(PhoneLedger.AddText), any ? Ui.Label : Ui.OffText, lx, addTop, lw);
            for (int i = 0; i < panel.Steps.Count; i++)
                DrawPhoneStep(c, u, i, panel.Steps[i]);
        }

        var right = Box(PhoneLedger.RightLine(f, 0, 0));
        float rx = right.Position.X + u.Px(4), rw = right.Size.X - u.Px(8);
        Top(c, _brush, "Subordinates", u.Size(PhoneLedger.SideTitleText), Ui.Name, rx, u.Px(PhoneLedger.SideTitleTop), rw);
        Top(c, _cinzelLight, _ready == 1 ? "1 unit ready" : $"{_ready} units ready", u.Size(PhoneLedger.ReadyText), Ui.Subtitle, rx,
            u.Px(PhoneLedger.ReadyTop), rw);
        int split = _date.IndexOf(" · ", StringComparison.Ordinal);
        Top(c, _cinzel, split < 0 ? _date : _date[..split], u.Size(PhoneLedger.YearText), Ui.Name, rx, u.Px(PhoneLedger.YearTop), rw);
        if (split >= 0)
            Top(c, _cinzel, _date[(split + 3)..], u.Size(PhoneLedger.WeekText), Ui.Name, rx, u.Px(PhoneLedger.WeekTop), rw);
        float rule = u.Px(PhoneLedger.SideRule);
        c.DrawLine(new Vector2(rx, rule), new Vector2(rx + rw, rule), Ui.DimGold, Line(1));
        Top(c, _cinzelLight, "Budget", u.Size(PhoneLedger.LabelText), Ui.Subtitle, rx, u.Px(PhoneLedger.BudgetLabelTop), rw);
        Top(c, _cinzel, Figures.GoldShort(panel.Gold * 100), u.Size(PhoneLedger.SideBudgetText), Ui.Name, rx, u.Px(PhoneLedger.SideBudgetTop), rw);

        var rep = RectOf(ReplenishButton)!.Value;
        var end = RectOf(EndButton)!.Value;
        int word = OrdersSize(u, PhoneLedger.OrderButtonText, PhoneLedger.TalkFaceIn);
        DrawPhoneTalk(c, u, panel, word);
        var r = panel.Replenish;
        string figures = $"{Figures.Signed(r.Men)} · {Figures.GoldShort(r.Price * 100)}";
        string note = r.Enabled ? figures : r.Note;
        int small = SideCells.Fit(_gothic, note, u.Size(PhoneLedger.ReplenishNote), rep.Size.X - u.Px(24));
        if (Enabled(ReplenishButton))
            DrawPrimary(c, rep, "Replenish", note, ReplenishButton, word, small, _cinzel);
        else
            DrawPlain(c, rep, "Replenish", note, ReplenishButton, _cinzel, word, small);
        DrawPlain(c, end, "End", null, EndButton, _cinzel, word, 0);
    }

    /// <summary>
    /// One Cinzel 700 size for Replenish, Talk and End (docs/ui/style.md, "Buttons side by side"): <paramref name="text"/>, or
    /// as much smaller as the longest word needs in its box (Talk's right of its face, <paramref name="faceIn"/> in).
    /// </summary>
    private int OrdersSize(PhoneUnits u, float text, float faceIn)
    {
        var talk = RectOf(TalkButton)!.Value;
        float talkRoom = talk.Size.X - (talk.Size.Y - u.Px(faceIn)) - u.Px(26);   // right of the face: its inset and its side
        return SideCells.FitAll(_cinzel, u.Size(text), ("Replenish", RectOf(ReplenishButton)!.Value.Size.X - u.Px(30)), ("Talk", talkRoom),
            ("End", RectOf(EndButton)!.Value.Size.X - u.Px(30)));
    }

    /// <summary>A step of Add New Troops (<c>lu_orders_side</c>): the men at the left, the price at the foot right; grey above the budget.</summary>
    private void DrawPhoneStep(Control c, PhoneUnits u, int i, SubordinateButton step)
    {
        var rect = StepRect(i);
        int button = StepButton + i;
        bool on = Enabled(button), hover = on && button == _hovered;
        c.DrawRect(rect, on ? hover ? Ui.RowHover : Ui.Row : Ui.OffButton);
        c.DrawRect(rect, on ? hover ? Ui.HoverLine : Ui.DimGold : Ui.OffLine, false, Line(on && hover ? 2 : 1.5f));
        if (button == _asked)
            c.DrawRect(rect, AskedLine, false, Line(2));
        int men = u.Size(PhoneLedger.StepText), price = u.Size(PhoneLedger.PriceText);
        float y = rect.Position.Y + Math.Min(u.Px(40), rect.Size.Y * 0.34f);
        c.DrawString(_cinzel, new Vector2(rect.Position.X + u.Px(20), y + (_cinzel.GetAscent(men) - _cinzel.GetDescent(men)) / 2),
            Figures.Signed(step.Men), fontSize: men, modulate: on ? Ui.Text : Ui.OffText);
        string cost = Figures.GoldShort(step.Price * 100);
        int fitted = SideCells.Fit(_gothic, cost, price, rect.Size.X - u.Px(32));
        float py = rect.End.Y - Math.Min(u.Px(30), rect.Size.Y * 0.25f);
        c.DrawString(_gothic, new Vector2(rect.End.X - u.Px(16) - _gothic.GetStringSize(cost, fontSize: fitted).X,
            py + (_gothic.GetAscent(fitted) - _gothic.GetDescent(fitted)) / 2), cost, fontSize: fitted, modulate: on ? Ui.Quiet : Ui.OffText);
    }

    /// <summary>Talk in the side (<c>side_talk</c>): the unit's face at the left, the word beside it; its talk's NEW or ??? pill at the top right.</summary>
    private void DrawPhoneTalk(Control c, PhoneUnits u, SubordinatePanel panel, int text)
    {
        var rect = RectOf(TalkButton)!.Value;
        bool on = Enabled(TalkButton), hover = on && _hovered == TalkButton;
        if (!on)
        {
            c.DrawRect(rect, Ui.OffButton);
            c.DrawRect(rect, Ui.OffLine, false, Line(1));
        }
        else
            Ui.DrawRowBox(c, rect, hover, _k);
        float inset = u.Px(PhoneLedger.TalkFaceIn), size = rect.Size.Y - 2 * inset;
        var faceRect = new Rect2(rect.Position + new Vector2(inset, inset), new Vector2(size, size));
        if (_talkFace.Get(_faces, _crops, panel.FaceCg, size * _k) is { } face)
            c.DrawTextureRect(face, faceRect, false, on ? Colors.White : new Color(0.45f, 0.42f, 0.4f));
        c.DrawRect(faceRect, on ? Ui.Gold : Ui.OffLine, false, Line(1));
        float x = faceRect.End.X, w = rect.End.X - x;
        CentredString(c, _cinzel, "Talk", text, !on ? Ui.OffText : hover ? Ui.Name : Ui.Text, x + w / 2, Lacquer.Baseline(rect, _cinzel, text));
        if (on && PickerMarks.Label(panel.TalkMark) is { } pill)
            Ui.DrawPill(c, _cinzel, rect.End.X - u.Px(10), rect.Position.Y + u.Px(22), pill,
                panel.TalkMark == SceneMark.New ? Ui.MarkNew : Ui.MarkMaybe, u.Size(22), u.Px(30));
    }

    /// <summary><paramref name="text"/> with its top at <paramref name="top"/>, shrunk to <paramref name="width"/> when it would pass it.</summary>
    private static void Top(Control c, Font font, string text, int size, Color colour, float x, float top, float width)
    {
        int fitted = SideCells.Fit(font, text, size, width);
        c.DrawString(font, new Vector2(x, top + font.GetAscent(fitted)), text, fontSize: fitted, modulate: colour);
    }
}
