using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// Our Treasure screen on the phone held landscape (docs/ui/phone-build.md, step L7; docs/ui/phone.md, §15, LT2): across the
// whole width between the hole's strips over the wave backdrop — at the left the title and the date, the selected entry's
// picture at the column's width, its name and figures under it with the touch mode's button beside them, Characters | Items
// and End at the left thumb (while a first pick waits, its prompt and Back in the tabs' place); at the right thumb the
// picker's grid folded after its ninth row, every entry at its number's cell, an item's name over its holder and shrunk
// where long. Drawn in the frame's layout pixels over the whole screen (PhoneUnits), the places PhonePickers'.
internal sealed partial class TreasureView
{
    private PhoneFrame? _phone;

    /// <summary>The button at <paramref name="at"/> (the screen's units): End, the touch mode's button, Back or a tab, or an entry's cell.</summary>
    private int PhoneButtonAt(PhoneFrame f, Vector2 at)
    {
        bool In(PhoneFrame.Box box) => box.Contains(at.X, at.Y);
        if (In(PhonePickers.TreasureEnd(f)))
            return EndButton;
        if (_button is not null && In(PhonePickers.Action(f)))
            return ActionButton;
        if (_prompt is not null)
        {
            if (In(PhonePickers.Back(f)))
                return BackButton;
        }
        else if (In(PhonePickers.Tab(f, 0)))
            return TabCharacters;
        else if (In(PhonePickers.Tab(f, 1)))
            return TabItems;
        int cell = PhonePickers.CellAt(f, at.X, at.Y);
        return _entries.Any(e => e.Number == cell) ? cell : NoButton;
    }

    private void DrawPhone(Control c, PhoneUnits u)
    {
        var f = u.Frame;
        PhonePickerParts.DrawTitle(c, u, _brush, _cinzel, "Treasure", _date);
        DrawPicture(c, u.Rect(PhonePickers.Picture(f)));
        PhoneDetails(c, u);
        if (_button is { } button)
            PhoneAction(c, u, button);
        if (_prompt is { } prompt)
        {
            PhonePrompt(c, u, prompt);
            PhonePickerParts.DrawButton(c, u, u.Rect(PhonePickers.Back(f)), _brush, "Back", !Busy && _hovered == BackButton);
        }
        else
            PhoneTabs(c, u);
        PhonePickerParts.DrawButton(c, u, u.Rect(PhonePickers.TreasureEnd(f)), _brush, "End", !Busy && _hovered == EndButton);
        PhoneGrid(c, u);
    }

    /// <summary>
    /// The selected entry's line: its name with the subtitle after it, the figures under them in columns, the values as much
    /// smaller as the room needs; with nothing selected how to pick.
    /// </summary>
    private void PhoneDetails(Control c, PhoneUnits u)
    {
        var (dx, dy) = PhonePickers.Details(u.Frame);
        float x = dx / u.K, y = dy / u.K;
        float room = (PhonePickers.Column(u.Frame).Right - u.Frame.Px(PhonePickers.ItemDetailsRight)) / u.K - x;
        int label = u.Size(PhonePickers.LabelText);
        if (_details is not { } d)
        {
            string[] hint = _prompt is { } first ? [first.UnitFirst ? "Tap an item to see it." : "Tap a character to see it."]
                : ["Tap a character, then the item to equip —", "or an item, then the character to equip it."];
            int size = hint.Min(line => SideCells.Fit(_gothic, line, u.Size(30), room));
            for (int i = 0; i < hint.Length; i++)
                c.DrawString(_gothic, new Vector2(x, y + u.Px(30 + 48 * i) + _gothic.GetAscent(size)), hint[i], fontSize: size, modulate: Ui.Quiet);
            return;
        }
        float gap = u.Px(20), sw = _cinzel.GetStringSize(d.Subtitle, fontSize: label).X;
        int name = SideCells.Fit(_brush, d.Name, u.Size(PhonePickers.ItemNameText), room - sw - gap);
        float mid = y + u.Px(PhonePickers.ItemNameMiddle);
        c.DrawString(_brush, new Vector2(x, PhonePickerParts.Middle(_brush, name, mid)), d.Name, fontSize: name, modulate: Ui.Name);
        float nw = _brush.GetStringSize(d.Name, fontSize: name).X;
        c.DrawString(_cinzel, new Vector2(x + nw + gap, PhonePickerParts.Middle(_cinzel, label, mid)), d.Subtitle, fontSize: label,
            modulate: d.SubtitleOff ? Ui.Minus : Ui.Subtitle);

        int value = u.Size(PhonePickers.ValueText);
        float columns = u.Px(PhonePickers.FigureGap);
        float Width(TreasureFigure figure, int size) =>
            Math.Max(_cinzel.GetStringSize(figure.Label, fontSize: label).X, _gothic.GetStringSize(figure.Value, fontSize: size).X);
        while (value > 8 && d.Figures.Sum(figure => Width(figure, value)) + columns * (d.Figures.Count - 1) > room)
            value--;
        float fx = x;
        foreach (var figure in d.Figures)
        {
            c.DrawString(_cinzel, new Vector2(fx, y + u.Px(PhonePickers.ItemLabelTop) + _cinzel.GetAscent(label)), figure.Label, fontSize: label,
                modulate: Ui.Label);
            c.DrawString(_gothic, new Vector2(fx, y + u.Px(PhonePickers.ItemValueTop) + _gothic.GetAscent(value)), figure.Value, fontSize: value,
                modulate: Ui.Value);
            fx += Width(figure, value) + columns;
        }
    }

    /// <summary>The touch mode's button beside the details: red lacquer with its word, or greyed with the picker's refusal under it.</summary>
    private void PhoneAction(Control c, PhoneUnits u, TreasureButton b)
    {
        var r = u.Rect(PhonePickers.Action(u.Frame));
        float cx = r.GetCenter().X, room = r.Size.X - u.Px(30);
        if (b.Refusal is null)
        {
            bool hover = !Busy && _hovered == ActionButton;
            Ui.DrawRedButton(c, r, hover, hover && _pressed == ActionButton, _k);
            int size = SideCells.Fit(_cinzel, b.Label, u.Size(PhonePickers.ActionText * 0.8f), room);
            Centred(c, _cinzel, b.Label, size, Color.Color8(250, 226, 160), cx, PhonePickerParts.Middle(_cinzel, size, r.GetCenter().Y));
            return;
        }
        c.DrawRect(r, Ui.OffButton);
        c.DrawRect(r, Ui.OffLine, false, Line(1));
        int word = SideCells.Fit(_cinzel, b.Label, u.Size(PhonePickers.ActionText * 0.7f), room);
        Centred(c, _cinzel, b.Label, word, Ui.OffText, cx, PhonePickerParts.Middle(_cinzel, word, r.Position.Y + r.Size.Y * 0.34f));
        int why = SideCells.Fit(_gothic, b.Refusal, u.Size(28), room);
        Centred(c, _gothic, b.Refusal, why, Ui.Minus, cx, PhonePickerParts.Middle(_gothic, why, r.Position.Y + r.Size.Y * 0.72f));
    }

    /// <summary>A first pick's prompt in the tabs' place: the unit's face, "Rance is equipping —" over "Which item?" (an item's the other way round).</summary>
    private void PhonePrompt(Control c, PhoneUnits u, TreasurePrompt p)
    {
        var box = u.Rect(PhonePickers.Prompt(u.Frame));
        Ui.DrawPanel(c, box, _k);
        float inset = u.Px(PhonePickers.PromptFaceIn), side = box.Size.Y - 2 * inset;
        float tx = box.Position.X + u.Px(28);
        if (p.UnitFirst && FaceTexture(p.FaceCg, side) is { } face)
        {
            var square = new Rect2(box.Position.X + inset, box.Position.Y + inset, side, side);
            c.DrawTextureRect(face, square, false);
            c.DrawRect(square, Ui.Gold, false, Line(1));
            tx = square.End.X + u.Px(PhonePickers.PromptTextGap);
        }
        float room = box.End.X - u.Px(24) - tx;
        var (lead, tail) = p.UnitFirst ? ("", " is equipping —") : ("Equipping ", " —");
        string question = p.UnitFirst ? "Which item?" : "Which character?";
        int size = u.Size(PhonePickers.PromptText);
        float Run(int s) => _gothic.GetStringSize(lead + tail, fontSize: s).X + _brush.GetStringSize(p.Name, fontSize: s).X;
        while (size > 8 && (Run(size) > room || _gothic.GetStringSize(question, fontSize: size).X > room))
            size--;
        float first = box.Position.Y + box.Size.Y * PhonePickers.PromptFirst, x = tx;
        c.DrawString(_gothic, new Vector2(x, PhonePickerParts.Middle(_gothic, size, first)), lead, fontSize: size, modulate: Ui.Text);
        x += _gothic.GetStringSize(lead, fontSize: size).X;
        c.DrawString(_brush, new Vector2(x, PhonePickerParts.Middle(_brush, size, first)), p.Name, fontSize: size, modulate: Ui.Name);
        x += _brush.GetStringSize(p.Name, fontSize: size).X;
        c.DrawString(_gothic, new Vector2(x, PhonePickerParts.Middle(_gothic, size, first)), tail, fontSize: size, modulate: Ui.Text);
        c.DrawString(_gothic, new Vector2(tx, PhonePickerParts.Middle(_gothic, size, box.Position.Y + box.Size.Y * PhonePickers.PromptSecond)),
            question, fontSize: size, modulate: Ui.Text);
    }

    /// <summary>Characters | Items: the view shown lit, the other one turns it.</summary>
    private void PhoneTabs(Control c, PhoneUnits u)
    {
        int size = u.Size(PhonePickers.TabText);
        for (int view = 0; view < 2; view++)
        {
            var r = u.Rect(PhonePickers.Tab(u.Frame, view));
            bool on = view == _view, hover = !on && !Busy && _hovered == (view == 0 ? TabCharacters : TabItems);
            c.DrawRect(r, on ? Ui.ChosenFill : hover ? Ui.RowHover : Ui.Row);
            c.DrawRect(r, on ? Ui.Gold : hover ? Ui.HoverLine : Ui.DimGold, false, Line(on || hover ? 2 : 1));
            string label = view == 0 ? "Characters" : "Items";
            Centred(c, _cinzel, label, size, on || hover ? Ui.Name : Ui.Quiet, r.GetCenter().X, PhonePickerParts.Middle(_cinzel, size, r.GetCenter().Y));
        }
    }

    /// <summary>
    /// The folded grid: rows 1-9 at the left, 10-17 at the right, the gold rule between; a held item's name over its holder
    /// (a padlock before the holder on Chaos and Nikkou), any other entry's name in one line (a unit's diamond at its right),
    /// a long name shrunk to its cell; the selection lit, the cell a callout asks about in vermilion; the other cells empty.
    /// </summary>
    private void PhoneGrid(Control c, PhoneUnits u)
    {
        var listed = _entries.ToDictionary(e => e.Number);
        float s = u.Px(PhonePickers.ItemCellText) / 14;                    // the PC's marks at the type's size (its names 14)
        for (int n = 1; n <= TreasureRoster.Cells; n++)
        {
            var r = u.Rect(PhonePickers.Cell(u.Frame, n));
            if (!listed.TryGetValue(n, out var entry))
            {
                PhonePickerParts.DrawEmptyCell(c, r, _k);
                continue;
            }
            bool hover = TouchMode.On ? n == _selected : n == _hovered && !Busy;
            if (n == _asked)
            {
                c.DrawRect(r, Ui.ChosenFill);
                c.DrawRect(r, Ui.Vermilion, false, Line(2));
            }
            else
            {
                c.DrawRect(r, hover ? Ui.RowHover : Ui.Row);
                c.DrawRect(r, hover ? Ui.HoverLine : Ui.DimGold, false, Line(hover ? 2 : 1));
            }
            Color ink = entry.Ink switch
            {
                TreasureInk.Off => Ui.OffText,
                TreasureInk.Held => Ui.Name,
                _ => hover ? Ui.Name : Ui.Text,
            };
            float x = r.Position.X + u.Px(PhonePickers.ItemCellIn), right = r.End.X - u.Px(PhonePickers.CellRight);
            bool locked = entry.Fixed && _view == 1;
            int nameSize;
            if (entry.Holder != "")
            {
                int holder = u.Size(PhonePickers.HolderText);
                float hw = _gothic.GetStringSize(entry.Holder, fontSize: holder).X, below = r.Position.Y + r.Size.Y * PhonePickers.HolderMiddleShare;
                float hx = r.End.X - u.Px(PhonePickers.ItemCellIn) - hw;
                c.DrawString(_gothic, new Vector2(hx, PhonePickerParts.Middle(_gothic, holder, below)), entry.Holder, fontSize: holder, modulate: Ui.Quiet);
                if (locked)
                    DrawLock(c, new Vector2(hx - 12 * s, below + s), Ui.Quiet, s);
                float above = r.Position.Y + r.Size.Y * PhonePickers.ItemNameMiddleShare;
                nameSize = SideCells.Fit(_gothic, entry.Name, u.Size(PhonePickers.ItemCellText), right - x);
                c.DrawString(_gothic, new Vector2(x, PhonePickerParts.Middle(_gothic, nameSize, above)), entry.Name, fontSize: nameSize, modulate: ink);
                continue;
            }
            float mid = r.GetCenter().Y;
            if (locked)
            {
                DrawLock(c, new Vector2(right - 6 * s, mid + s), Ui.Quiet, s);
                right -= 16 * s;
            }
            if (entry.HoldsItem)
            {
                Lacquer.DrawDiamond(c, new Vector2(right - 4 * s, mid), 4 * s, entry.Fixed ? Ui.OffLine : Ui.Gold);
                right -= 12 * s;
            }
            nameSize = SideCells.Fit(_gothic, entry.Name, u.Size(PhonePickers.ItemCellText), right - x);
            c.DrawString(_gothic, new Vector2(x, PhonePickerParts.Middle(_gothic, nameSize, mid)), entry.Name, fontSize: nameSize, modulate: ink);
        }
        PhonePickerParts.DrawRule(c, u);
    }
}
