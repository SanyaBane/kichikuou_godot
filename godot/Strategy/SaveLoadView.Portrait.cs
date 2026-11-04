using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// Our Save/Load screen on the phone held upright (docs/ui/phone-build.md, step P7; docs/ui/phone.md, §9, PL2 as the owner
// changed it, and §25, PY2): one page down the room over the wave backdrop — the title band with Load | Save at its right,
// the two lists' tabs, the heading with the sweep, the list in one-line rows above the page, nine in view on the reference,
// the rest a finger's drag away (the scroll bar at the right); the chosen save's page standing on the bar (its title with
// Delete, the map cut at its foot, the figures in two columns, the comment with Edit); the red button | Back in the bar.
// The list, the rows, the page, the questions and the bar are held landscape's drawing at PhoneSavesPortrait's places
// (Places picks the orientation's); the band and the comment's card are upright's own. The card (PY2) stands just over
// Android's keyboard: whose comment, the field two lines high, the count, Keep | Cancel at its foot. Its words are wrapped
// and drawn here with the caret; the line edit in it takes the keys (and Android's keyboard) unseen, a tap in the field
// moving its caret to the letter tapped.
internal sealed partial class SaveLoadView
{
    private string _drawnText = "";     // the card's words and caret as drawn last (the line edit changed unseen: a redraw)
    private int _drawnCaret = -1;

    /// <summary>The comment's field in the card's: the line edit takes the keys there, its words and caret drawn by the view.</summary>
    private const int FieldHit = 603;

    /// <summary>Whether the phone is held upright: the line edit's words are drawn by the card.</summary>
    private bool Portrait => _phone is { IsPortrait: true };

    /// <summary>The comment's field's colours: the palette's ink, held upright none (the card draws the words wrapped).</summary>
    private void ColourEdit()
    {
        var ink = Portrait ? Colors.Transparent : Ui.Ink;
        _edit.AddThemeColorOverride("font_color", ink);
        _edit.AddThemeColorOverride("caret_color", ink);
        if (Portrait)
            _edit.AddThemeColorOverride("selection_color", Colors.Transparent);
        else
            _edit.RemoveThemeColorOverride("selection_color");
        _edit.MouseFilter = Portrait ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
    }

    /// <summary>The card's words or caret changed without a signal (typed by unattended play, the caret moved by a key): a redraw.</summary>
    private void FollowField()
    {
        if (Portrait && (_edit.Text != _drawnText || _edit.CaretColumn != _drawnCaret))
            QueueRedraw();
    }

    // ---------------------------------------------------------------- the places of either orientation

    /// <summary>The phone's places held landscape (<see cref="PhoneSaves"/>) or upright (<see cref="PhoneSavesPortrait"/>).</summary>
    private static class Places
    {
        public static PhoneFrame.Box List(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.List(f) : PhoneSaves.List(f);

        public static PhoneFrame.Box Chip(PhoneFrame f, bool save) => f.IsPortrait ? PhoneSavesPortrait.Chip(f, save) : PhoneSaves.Chip(f, save);

        public static PhoneFrame.Box Tab(PhoneFrame f, bool auto) => f.IsPortrait ? PhoneSavesPortrait.Tab(f, auto) : PhoneSaves.Tab(f, auto);

        public static float HeadMiddle(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.HeadMiddle(f) : PhoneSaves.HeadMiddle(f);

        public static PhoneFrame.Box Sweep(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.Sweep(f) : PhoneSaves.Sweep(f);

        public static PhoneFrame.Box New(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.New(f) : PhoneSaves.New(f);

        public static float Pitch(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.Pitch(f) : PhoneSaves.Pitch(f);

        public static int RowsShown(PhoneFrame f, bool newRow) =>
            f.IsPortrait ? PhoneSavesPortrait.RowsShown(f, newRow) : PhoneSaves.RowsShown(f, newRow);

        public static PhoneFrame.Box Row(PhoneFrame f, int place, bool newRow) =>
            f.IsPortrait ? PhoneSavesPortrait.Row(f, place, newRow) : PhoneSaves.Row(f, place, newRow);

        public static PhoneFrame.Box ScrollBar(PhoneFrame f, bool newRow) =>
            f.IsPortrait ? PhoneSavesPortrait.ScrollBar(f, newRow) : PhoneSaves.ScrollBar(f, newRow);

        public static PhoneFrame.Box Scrolled(PhoneFrame f, bool newRow) =>
            f.IsPortrait ? PhoneSavesPortrait.Scrolled(f, newRow) : PhoneSaves.Scrolled(f, newRow);

        public static PhoneFrame.Box Page(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.Page(f) : PhoneSaves.Page(f);

        public static float TitleMiddle(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.TitleMiddle(f) : PhoneSaves.TitleMiddle(f);

        public static float PageTitleText(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.PageTitleText : PhoneSaves.PageTitleText;

        public static PhoneFrame.Box Delete(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.Delete(f) : PhoneSaves.Delete(f);

        public static PhoneFrame.Box Map(PhoneFrame f, bool lines) => f.IsPortrait ? PhoneSavesPortrait.Map(f, lines) : PhoneSaves.Map(f, lines);

        public static (float X, float Y, float W) Figure(PhoneFrame f, int index, bool lines) =>
            f.IsPortrait ? PhoneSavesPortrait.Figure(f, index, lines) : PhoneSaves.Figure(f, index, lines);

        public static float ValueDown(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.ValueDown : PhoneSaves.ValueDown;

        public static float ValueRoom(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.ValueRoom : PhoneSaves.ValueRoom;

        public static float CommentLabel(PhoneFrame f, bool lines) =>
            f.IsPortrait ? PhoneSavesPortrait.CommentLabel(f, lines) : PhoneSaves.CommentLabel(f, lines);

        public static PhoneFrame.Box Comment(PhoneFrame f, bool lines) => f.IsPortrait ? PhoneSavesPortrait.Comment(f, lines) : PhoneSaves.Comment(f, lines);

        public static PhoneFrame.Box Edit(PhoneFrame f, bool lines) => f.IsPortrait ? PhoneSavesPortrait.Edit(f, lines) : PhoneSaves.Edit(f, lines);

        public static float LineMiddle(PhoneFrame f, int index) => f.IsPortrait ? PhoneSavesPortrait.LineMiddle(f, index) : PhoneSaves.LineMiddle(f, index);

        public static PhoneFrame.Box Back(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.Back(f) : PhoneSaves.Back(f);

        public static PhoneFrame.Box Button(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.Button(f) : PhoneSaves.Button(f);

        public static float ButtonText(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.ButtonText : PhoneSaves.ButtonText;

        public static PhoneFrame.Box DeleteQuestion(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.DeleteQuestion(f) : PhoneSaves.DeleteQuestion(f);

        public static PhoneFrame.Box SweepQuestion(PhoneFrame f) => f.IsPortrait ? PhoneSavesPortrait.SweepQuestion(f) : PhoneSaves.SweepQuestion(f);

        public static PhoneFrame.Box Answer(PhoneFrame f, PhoneFrame.Box question, bool yes) =>
            f.IsPortrait ? PhoneSavesPortrait.Answer(f, question, yes) : PhoneSaves.Answer(f, question, yes);

        public static PhoneFrame.Box Keep(PhoneFrame f, float keyboard) => f.IsPortrait ? PhoneSavesPortrait.Keep(f, keyboard) : PhoneSaves.Keep(f, keyboard);

        public static PhoneFrame.Box Cancel(PhoneFrame f, float keyboard) =>
            f.IsPortrait ? PhoneSavesPortrait.Cancel(f, keyboard) : PhoneSaves.Cancel(f, keyboard);
    }

    // ---------------------------------------------------------------- drawing

    /// <summary>The title band (<c>save_title</c>): the title in the brush face, Load | Save at its right, the divider under it.</summary>
    private void DrawPortraitBand(PhoneUnits u, bool save)
    {
        var room = u.Rect(u.Frame.Room);
        int title = u.Size(PhoneSavesPortrait.TitleText);
        DrawString(_brush, new Vector2(room.Position.X + u.Px(PhoneSavesPortrait.TitleIn),
            room.Position.Y + u.Px(PhoneSavesPortrait.TitleTop) + _brush.GetAscent(title)), save ? "Save" : "Load", fontSize: title, modulate: Ui.Name);
        if (_tabs)
            foreach (var tab in (SaveList[])[SaveList.Load, SaveList.Save])
                PhoneChip(TabBox(tab), tab.ToString(), tab == _list, _hovered == TabHit + (int)tab, u.Size(PhoneSavesPortrait.ChipText), u);
        float divider = room.Position.Y + u.Px(PhoneSavesPortrait.Divider), inset = u.Px(PhoneSavesPortrait.DividerIn);
        Ui.DrawDivider(this, room.Position.X + inset, room.End.X - inset, divider, _k);
    }

    /// <summary>The divider between the list and the page standing on the bar.</summary>
    private void DrawPortraitPageDivider(PhoneUnits u)
    {
        var room = u.Rect(u.Frame.Room);
        float inset = u.Px(PhoneSavesPortrait.DividerIn);
        Ui.DrawDivider(this, room.Position.X + inset, room.End.X - inset, PhoneSavesPortrait.PageDividerY(u.Frame) / u.K, _k);
    }

    /// <summary>The sweep's question's tip, up at the sweep's middle from the callout's top.</summary>
    private void DrawPortraitSweepTip(PhoneUnits u, Rect2 box)
    {
        float tip = SweepBox.GetCenter().X, top = box.Position.Y, t = u.Px(PhoneSaves.Tip);
        DrawColoredPolygon([new(tip - t, top + 2), new(tip + t, top + 2), new(tip, top - t)], Ui.Gold);
    }

    /// <summary>
    /// The comment's own card over the dimmed screen (<c>comment_card</c>, PY2): "COMMENT" at the left, whose at the right,
    /// the field two lines high (paper in a vermilion frame, the words wrapped, the caret), the count under its right,
    /// Keep | Cancel at the card's foot.
    /// </summary>
    private void PortraitCard(PhoneUnits u)
    {
        var f = u.Frame;
        DrawRect(new Rect2(Vector2.Zero, Size / _k), new Color(0, 0, 0, 0.6f));
        var card = u.Rect(PhoneSavesPortrait.Card(f, _keyboard));
        Ui.DrawPanel(this, card, _k);
        float middle = card.Position.Y + u.Px(PhoneSavesPortrait.CardLabelMiddle), x = card.Position.X + u.Px(PhoneSavesPortrait.CardLabelIn);
        int label = u.Size(PhoneSavesPortrait.CardLabelText);
        Text(_cinzelLight, label, x, middle, "COMMENT", Ui.Subtitle);
        if (ChosenSave is { } held)
        {
            string whose = held.IsAuto ? $"{held.RingText} auto-save · {held.DateText}" : $"{held.Label} · {held.DateText}";
            float right = card.End.X - u.Px(PhoneSavesPortrait.CardLabelIn);
            float room = right - x - _cinzelLight.GetStringSize("COMMENT", fontSize: label).X - u.Px(32);
            TextRight(_cinzel, SideCells.Fit(_cinzel, whose, u.Size(PhoneSavesPortrait.CardTitleText), room), right, middle, whose, Ui.Name);
        }
        var field = u.Rect(PhoneSavesPortrait.Field(f, _keyboard));
        DrawRect(field, Ui.Paper);
        DrawRect(field, Ui.Vermilion, false, Line(2));
        DrawFieldWords(u, field);
        var (countRight, countMiddle) = PhoneSavesPortrait.Count(f, _keyboard);
        TextRight(_gothic, u.Size(PhoneSavesPortrait.CountText), countRight / u.K, countMiddle / u.K, $"{_edit.Text.Length} / {CommentLength}", Ui.Quiet);
        var cancel = u.Rect(PhoneSavesPortrait.Cancel(f, _keyboard));
        bool hover = _hovered == CancelHit;
        Ui.DrawRowBox(this, cancel, hover, _k);
        TextCentred(_brush, u.Size(PhoneSavesPortrait.CancelText), cancel.GetCenter().X, cancel.GetCenter().Y, "Cancel", hover ? Ui.Name : Ui.Text);
        var keep = u.Rect(PhoneSavesPortrait.Keep(f, _keyboard));
        bool lit = _hovered == KeepHit;
        Ui.DrawRedButton(this, keep, lit, lit && _pressed == KeepHit, _k);
        TextCentred(_brush, u.Size(PhoneSavesPortrait.KeepText), keep.GetCenter().X, keep.GetCenter().Y, "Keep", Ui.Name);
    }

    /// <summary>The field's words as they wrap at the largest type that keeps them to its lines, the block in its middle.</summary>
    private (IReadOnlyList<PhoneSavesPortrait.FieldLine> Lines, int Size, float Top, float Pitch) FieldLayout(PhoneUnits u, Rect2 field)
    {
        string text = _edit.Text;
        float room = field.Size.X - 2 * u.Px(PhoneSavesPortrait.FieldTextIn);
        int size = u.Size(PhoneSavesPortrait.FieldText), least = Math.Max(1, size * 2 / 3);
        var lines = PhoneSavesPortrait.Wrap(text, s => _gothic.GetStringSize(s, fontSize: size).X, room);
        while (lines.Count > PhoneSavesPortrait.FieldLines && size > least)
        {
            size--;
            int at = size;
            lines = PhoneSavesPortrait.Wrap(text, s => _gothic.GetStringSize(s, fontSize: at).X, room);
        }
        float pitch = size * PhoneSavesPortrait.FieldLeading;
        return (lines, size, field.GetCenter().Y - lines.Count * pitch / 2, pitch);
    }

    private void DrawFieldWords(PhoneUnits u, Rect2 field)
    {
        var (lines, size, top, pitch) = FieldLayout(u, field);
        float x = field.Position.X + u.Px(PhoneSavesPortrait.FieldTextIn);
        for (int i = 0; i < lines.Count; i++)
            Text(_gothic, size, x, top + (i + 0.5f) * pitch, lines[i].Text, Ui.Ink);
        var (line, before) = PhoneSavesPortrait.CaretAt(lines, _edit.CaretColumn);
        float cx = x + _gothic.GetStringSize(before, fontSize: size).X + u.Px(4), cy = top + (line + 0.5f) * pitch;
        DrawRect(new Rect2(cx, cy - size * 0.6f, u.Px(4), size * 1.2f), Ui.Ink);
        _drawnText = _edit.Text;
        _drawnCaret = _edit.CaretColumn;
    }

    /// <summary>A tap in the field: the caret before the letter nearest it.</summary>
    private void PlaceCaret(Vector2 at)
    {
        if (_phone is not { } f)
            return;
        var u = new PhoneUnits(f);
        var field = u.Rect(PhoneSavesPortrait.Field(f, _keyboard));
        var (lines, size, top, pitch) = FieldLayout(u, field);
        int line = Math.Clamp((int)MathF.Floor((at.Y - top) / pitch), 0, lines.Count - 1);
        float x = at.X - field.Position.X - u.Px(PhoneSavesPortrait.FieldTextIn);
        _edit.CaretColumn = PhoneSavesPortrait.ColumnAt(lines, line, x, s => _gothic.GetStringSize(s, fontSize: size).X);
        QueueRedraw();
    }
}
