using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// Our Save/Load screen on the phone held landscape (docs/ui/phone-build.md, step L7; docs/ui/phone.md, §15, LL1, and §26,
// LY1): the PC's split across the whole width between the hole's strips over the wave backdrop — at the left the title,
// Load | Save from COMMAND, the two lists' tabs, the heading with the sweep and the list in one-line rows (a comment only on
// the page, its star on the seal), as many as fit, the rest a finger's drag away (the scroll bar); at the right the chosen
// save's page — its title with Delete, the map at the page's width cut lower for the figures in three columns and the
// comment with Edit (an auto-save's lines on its load under it), the red button | Back at its foot, Back at the right
// thumb; Delete's and the sweep's questions in callouts at them. Edit raises the comment's own card just over Android's
// keyboard (LY1), the screen dimmed: whose comment, the field in one line, the count, Keep | Cancel at the right thumb; the
// keyboard's ✓ keeps it, Back cancels (the keyboard it closes, or Back over the card). Drawn in the frame's layout pixels
// over the whole screen (PhoneUnits), the places PhoneSaves'.
internal sealed partial class SaveLoadView
{
    private PhoneFrame? _phone;
    private float _keyboard;            // the keyboard's height in the canvas's units as reported last (0: none yet)
    private bool _keyboardSeen;         // a keyboard reported up since the card opened
    private double _keyboardGone;       // how long none has been reported since
    private static bool _keyboardLogged; // its height logged once a launch (docs/ui/phone-build.md, §7)

    /// <summary>How long the keyboard stays down before the card takes it for Android's Back (✓ hides it too, after its Enter).</summary>
    private const double KeyboardGoneSeconds = 0.4;

    /// <summary>An auto-save's page: its two lines on what its load does under the comment, the map cut lower for them.</summary>
    private bool PageLines => !_chosenNew && ChosenSave is { IsAuto: true };

    /// <summary>The comment's card's answers at <paramref name="at"/> (layout pixels); a press off them does nothing.</summary>
    private int CardHitAt(Vector2 at)
    {
        var f = _phone!;
        if (Phone.Rect(Places.Keep(f, _keyboard)).HasPoint(at))
            return KeepHit;
        if (Phone.Rect(Places.Cancel(f, _keyboard)).HasPoint(at))
            return CancelHit;
        return f.IsUpright && Phone.Rect(PhoneSavesUpright.Field(f, _keyboard)).HasPoint(at) ? FieldHit : 0;
    }

    private void WatchKeyboard()
    {
        _keyboardSeen = false;
        _keyboardGone = 0;
    }

    /// <summary>The keyboard Android reports up, in the canvas's units (0: none, or no virtual keyboard: a PC playing a phone).</summary>
    private float KeyboardHeight()
    {
        if (!DisplayServer.HasFeature(DisplayServer.Feature.VirtualKeyboard))
            return 0;
        int pixels = DisplayServer.VirtualKeyboardGetHeight();
        float window = GetWindow().Size.Y;
        return pixels > 0 && window > 0 ? pixels * GetViewportRect().Size.Y / window : 0;
    }

    /// <summary>
    /// While the card is up: it follows the keyboard's top as Android reports it, and once a keyboard was up and has stayed
    /// down for <see cref="KeyboardGoneSeconds"/> with nothing pressed, Android's Back closed it — the comment is left as it was.
    /// </summary>
    private void FollowKeyboard(double delta)
    {
        if (!_editing || _phone is null)
            return;
        FollowField();
        float height = KeyboardHeight();
        if (height <= 0)
        {
            if (_keyboardSeen && _pressed == 0 && (_keyboardGone += delta) >= KeyboardGoneSeconds)
                EndEdit(keep: false);
            return;
        }
        _keyboardSeen = true;
        _keyboardGone = 0;
        if (!_keyboardLogged)
        {
            _keyboardLogged = true;
            GD.Print($"Kichikuou: Android's keyboard is {DisplayServer.VirtualKeyboardGetHeight()} px high of the window's "
                + $"{GetWindow().Size.Y} (the comment's card stands over it).");
        }
        if (Math.Abs(height - _keyboard) < 0.5f)
            return;
        _keyboard = height;
        PlaceEdit();
        QueueRedraw();
    }

    /// <summary>The comment's field in the card's: its words 26 in, at the card's type (held upright unseen, the card drawing them).</summary>
    private void PlacePhoneEdit(PhoneFrame f)
    {
        var field = f.IsUpright ? PhoneSavesUpright.Field(f, _keyboard) : PhoneSaves.Field(f, _keyboard);
        float inset = f.Px(26);
        _edit.Position = new Vector2(field.X + inset, field.Y);
        _edit.Size = new Vector2(field.W - 2 * inset, field.H);
        _edit.AddThemeFontOverride("font", _gothic);
        _edit.AddThemeFontSizeOverride("font_size", Math.Max(8, (int)Math.Round(f.Px(f.IsUpright ? PhoneSavesUpright.FieldText : PhoneSaves.FieldText))));
    }

    /// <summary>The finger's rows' pitch in layout pixels.</summary>
    private float Pitch => _phone is { } f ? Places.Pitch(f) / f.Scale : RowPitch;

    // ---------------------------------------------------------------- drawing

    private void DrawPhone(PhoneUnits u, SaveList list)
    {
        var f = u.Frame;
        bool save = list == SaveList.Save;
        if (f.IsUpright)
            DrawUprightBand(u, save);
        else
        {
            var column = u.Rect(Places.List(f));
            int title = u.Size(PhoneSaves.TitleText);
            DrawString(_brush, new Vector2(column.Position.X + u.Px(PhoneSaves.TitleIn), u.Px(PhoneSaves.TitleTop) + _brush.GetAscent(title)),
                save ? "Save" : "Load", fontSize: title, modulate: Ui.Name);
            if (_tabs)
                foreach (var tab in (SaveList[])[SaveList.Load, SaveList.Save])
                    PhoneChip(TabBox(tab), tab.ToString(), tab == _list, _hovered == TabHit + (int)tab, u.Size(PhoneSaves.ChipText), u);
            Ui.DrawDivider(this, column.Position.X, column.End.X, u.Px(PhoneSaves.Divider), _k);
        }
        PhoneChip(SideBox(false), $"Manual saves · {_shelf.Manuals.Count}", !_autoSide, _hovered == SideHit, u.Size(PhoneSaves.TabText), u);
        PhoneChip(SideBox(true), $"Auto-saves · {_shelf.Autos.Count}", _autoSide, _hovered == SideHit + 1, u.Size(PhoneSaves.TabText), u);
        PhoneList(u);
        if (f.IsUpright)
            DrawUprightPageDivider(u);
        PhonePage(u, save);
        if (_confirm)
            PhoneDeleteQuestion(u);
        if (_sweep)
            PhoneSweepQuestion(u);
        if (_editing)
        {
            if (f.IsUpright)
                UprightCard(u);
            else
                PhoneCard(u);
        }
    }

    /// <summary>Load | Save and the lists' tabs: the chosen one in vermilion, as the PC's.</summary>
    private void PhoneChip(Rect2 r, string text, bool on, bool hover, int size, PhoneUnits u)
    {
        if (on)
        {
            DrawRect(r, ChosenFill);
            DrawRect(r, Ui.Vermilion, false, Line(2));
        }
        else
            Ui.DrawRowBox(this, r, hover, _k);
        size = SideCells.Fit(_cinzel, text, size, r.Size.X - u.Px(30));
        TextCentred(_cinzel, size, r.GetCenter().X, r.GetCenter().Y, text, on ? Ui.Name : hover ? Ui.Text : Ui.Quiet);
    }

    /// <summary>The heading, New save in Save's manual saves, the rows in view and the scroll bar's groove.</summary>
    private void PhoneList(PhoneUnits u)
    {
        var f = u.Frame;
        var column = u.Rect(Places.List(f));
        float middle = Places.HeadMiddle(f) / u.K;
        int head = u.Size(PhoneSaves.HeadText);
        Text(_cinzelLight, head, column.Position.X + u.Px(PhoneSaves.HeadIn), middle, "NEWEST FIRST", Ui.Subtitle);
        if (_autoSide)
            TextRight(_gothic, head, column.End.X, middle, "the game keeps the last ten weeks", Ui.Label);
        else
            PhoneSweepButton(u);
        if (NewRowShown)
            PhoneNewRow(u);
        foreach (var (id, rect) in Rows())
            if (id is { } held && Find(held) is { } slot)
                PhoneRow(u, rect, slot);
        if (Shown.Count == 0)
            TextCentred(_cinzelLight, u.Size(PhoneSaves.EmptyText), column.GetCenter().X,
                Places.Row(f, 0, NewRowShown).Y / u.K + u.Px(PhoneSaves.EmptyDown),
                _autoSide ? "No auto-saves yet." : "No manual saves yet.", Ui.OffText);
        // the reference bar (the owner's S2); its groove stays when every save fits, as the PC's
        var groove = Places.ScrollBar(f, NewRowShown);
        int count = Shown.Count;
        Rect2? thumb = count > RowsShown
            ? u.Rect(PhoneScrollBar.Thumb(f, groove, (float)RowsShown / count, (float)_scroll / (count - RowsShown)))
            : null;
        Ui.DrawScrollBar(this, u.Rect(groove), thumb, u.Px(PhoneScrollBar.Rim));
    }

    /// <summary>"Delete all without ★ (n)", off while every manual save has a comment.</summary>
    private void PhoneSweepButton(PhoneUnits u)
    {
        var r = SweepBox;
        int count = UncommentedCount;
        bool off = count == 0, hover = _hovered == SweepHit || _sweep;
        if (off)
        {
            DrawRect(r, Ui.OffButton);
            DrawRect(r, Ui.OffLine, false, Frame(1));
        }
        else
            Ui.DrawRowBox(this, r, hover, _k);
        const string label = "Delete all without";
        string tail = $"({count})";
        int size = u.Size(PhoneSaves.SweepText);
        float star = u.Px(PhoneSaves.SweepStar), gap = u.Px(10);
        float lw = _gothic.GetStringSize(label, fontSize: size).X, tw = _gothic.GetStringSize(tail, fontSize: size).X;
        float x = r.GetCenter().X - (lw + 2 * gap + 2 * star + tw) / 2, y = r.GetCenter().Y;
        var colour = off ? Ui.OffText : hover ? Ui.Name : Ui.Text;
        Text(_gothic, size, x, y, label, colour);
        DrawStar(new Vector2(x + lw + gap + star, y), star, off ? Ui.OffText : Ui.Gold);
        Text(_gothic, size, x + lw + 2 * gap + 2 * star, y, tail, colour);
    }

    private void PhoneNewRow(PhoneUnits u)
    {
        var rect = NewBox;
        if (_chosenNew)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Ui.Vermilion, false, Line(2));
        }
        else
        {
            bool hover = _hovered == NewHit;
            DrawRect(rect, hover ? Ui.RowHover : Ui.RowShade);
            DrawDashed(rect, hover ? Ui.HoverLine : Ui.DimGold);
        }
        float cy = rect.GetCenter().Y;
        Text(_cinzel, u.Size(48), rect.Position.X + u.Px(36), cy, "+", Ui.Name);
        Text(_cinzel, u.Size(34), rect.Position.X + u.Px(88), cy, "New save", _chosenNew || _hovered == NewHit ? Ui.Name : Ui.Quiet);
    }

    /// <summary>
    /// A save's row in one line (<c>save_row</c>): the seal, a comment's star on its top left corner, the short date, the time
    /// written at the right, an auto-save's kind on its tag left of the time; the sweep asking strikes through those it deletes.
    /// </summary>
    private void PhoneRow(PhoneUnits u, Rect2 rect, SaveSlot held)
    {
        bool auto = _autoSide;
        int place = Rows().ToList().FindIndex(r => r.Id == held.Id);
        if (held.Id == _chosen && !_chosenNew)
        {
            DrawRect(rect, ChosenFill);
            DrawRect(rect, Ui.Vermilion, false, Line(2));
        }
        else
        {
            Ui.DrawRowBox(this, rect, _hovered == RowHit + place, _k);
            if (auto)
                DrawRect(rect, AutoGold, false, Line(1));
        }
        float x0 = rect.Position.X, mid = rect.GetCenter().Y, side = u.Px(PhoneSaves.Seal);
        var seal = new Rect2(x0 + u.Px(PhoneSaves.SealIn), mid - side / 2, side, side);
        DrawRect(seal, auto ? AutoGold : Ui.Vermilion);
        DrawRect(seal.Grow(-u.Px(5)), Ui.SealText with { A = 0.45f }, false, Line(1));
        TextCentred(_cinzel, u.Size(held.Seal.Length > 2 ? 28 : 36), seal.GetCenter().X, mid + u.Px(2), held.Seal, Ui.SealText);
        Text(_gothic, u.Size(PhoneSaves.DateText), x0 + u.Px(PhoneSaves.DateIn), mid, held.ShortDate, Ui.Text);
        int written = u.Size(PhoneSaves.WrittenText);
        float wx = rect.End.X - u.Px(PhoneSaves.WrittenIn) - _gothic.GetStringSize(held.WrittenShort, fontSize: written).X;
        Text(_gothic, written, wx, mid, held.WrittenShort, Ui.Quiet);
        if (auto)
            PhoneRingTag(u, wx - u.Px(PhoneSaves.RingGap), mid, held, u.Size(PhoneSaves.RingText));
        if (held.Commented)
        {
            DrawStar(seal.Position, u.Px(PhoneSaves.Star) * 9 / 7, StarRim);   // the PC's rim around the gold
            DrawStar(seal.Position, u.Px(PhoneSaves.Star), Ui.Gold);
        }
        else if (_sweep && !auto)
        {
            DrawRect(rect, GoneVeil);                                   // the sweep asking: this row would go
            DrawLine(new Vector2(x0 + u.Px(12), mid), new Vector2(rect.End.X - u.Px(12), mid), Ui.Vermilion, Line(1.5f));
        }
    }

    /// <summary>An auto-save's kind on its tag (the PC's colours), right-aligned at <paramref name="right"/>; its left edge.</summary>
    private float PhoneRingTag(PhoneUnits u, float right, float middle, SaveSlot held, int size)
    {
        string text = held.RingText;
        float w = _gothic.GetStringSize(text, fontSize: size).X + u.Px(28), h = size + u.Px(14);
        var rect = new Rect2(right - w, middle - h / 2, w, h);
        DrawRect(rect, held.Kind == SlotKind.TurnEnd ? TurnEndFill : TurnStartFill);
        DrawRect(rect, Ui.Gold, false, Line(1));
        TextCentred(_gothic, size, rect.GetCenter().X, middle, text, TagText);
        return rect.Position.X;
    }

    /// <summary>The chosen save's page (<c>land_save_page</c>): its title with Delete, the map cut lower, the figures, the comment, the foot.</summary>
    private void PhonePage(PhoneUnits u, bool save)
    {
        var f = u.Frame;
        var held = ChosenSave;
        var shown = _chosenNew ? _now : held;
        bool lines = PageLines;
        PhoneBack(u);
        var page = u.Rect(Places.Page(f));
        var mapRect = u.Rect(Places.Map(f, lines));
        if (shown is null)
        {
            string none = _autoSide ? "No auto-save chosen." : save ? "Choose New save or a save to save over." : "No saved games.";
            TextCentred(_cinzelLight, u.Size(36), mapRect.GetCenter().X, mapRect.GetCenter().Y, none, Ui.Quiet);
            return;
        }
        PhonePageTitle(u, page, held, shown);
        if (MapOf(shown) is { } map)
        {
            float whole = mapRect.Size.X * PhoneSaves.MapShape;         // cut at its foot: the top part at the page's width
            DrawTextureRectRegion(map, mapRect, new Rect2(0, 0, map.GetWidth(), map.GetHeight() * mapRect.Size.Y / whole));
        }
        else
            DrawRect(mapRect, Colors.Black);
        DrawRect(mapRect, Ui.DimGold, false, Line(1));
        (string, string)[] stats =
        [
            ("Stage", shown.StageText), ("Budget", shown.BudgetText), ("Realm", shown.RealmText),
            ("Units", shown.Units.ToString()), ("Troops", Figures.Number(shown.Troops)), ("Written", _chosenNew ? "now" : shown.WrittenText),
        ];
        int label = u.Size(PhoneSaves.LabelText);
        for (int i = 0; i < stats.Length; i++)
        {
            var (fx, fy, fw) = Places.Figure(f, i, lines);
            float x = fx / u.K, y = fy / u.K;
            DrawString(_cinzelLight, new Vector2(x, y + _cinzelLight.GetAscent(label)), stats[i].Item1.ToUpperInvariant(), fontSize: label,
                modulate: Ui.Subtitle);
            int value = SideCells.Fit(_gothic, stats[i].Item2, u.Size(PhoneSaves.ValueText), (fw - f.Px(Places.ValueRoom(f))) / u.K);
            DrawString(_gothic, new Vector2(x, y + u.Px(Places.ValueDown(f)) + _gothic.GetAscent(value)), stats[i].Item2, fontSize: value,
                modulate: Ui.Value);
        }
        if (_chosenNew)
            Text(_gothic, u.Size(30), page.Position.X + u.Px(PhoneSaves.FigureIn), u.Rect(Places.Comment(f, false)).GetCenter().Y,
                "A new manual save. Its comment can be written once it is saved.", Ui.Text);
        else
        {
            PhoneComment(u, held!, lines);
            if (lines)
            {
                var (first, second) = LoadLines(held!);
                int size = u.Size(PhoneSaves.LineText);
                Text(_gothic, size, page.Position.X, Places.LineMiddle(f, 0) / u.K, Fit(first, size, page.Size.X), Ui.Quiet);
                Text(_gothic, size, page.Position.X, Places.LineMiddle(f, 1) / u.K, Fit(second, size, page.Size.X), Ui.Quiet);
            }
        }
        string? saving = save && _now is { } now ? $"Saving: {now.DateText} · {now.BudgetText} · {now.RealmText} · {Figures.Number(now.Troops)} troops" : null;
        if (ButtonShown)
            PhoneButton(u, _chosenNew || !save ? (save ? "Save" : "Load") : $"Save over {held!.Label}", saving);
        else if (save)
            PhoneOffButton(u);
        if (held is not null)
            PhoneDelete(u);
    }

    /// <summary>The page's title, an auto-save's Auto and kind after it, shrunk to end short of Delete.</summary>
    private void PhonePageTitle(PhoneUnits u, Rect2 page, SaveSlot? held, SaveSlot shown)
    {
        string title = _chosenNew ? $"New save · {shown.DateText}" : held!.Id?.Kind == SaveKind.Auto ? shown.DateText : $"{held.Label} · {shown.DateText}";
        float x = page.Position.X + u.Px(4), middle = Places.TitleMiddle(u.Frame) / u.K;
        float room = (held is not null ? DeleteBox.Position.X : page.End.X) - u.Px(24) - x;
        bool auto = held?.Id?.Kind == SaveKind.Auto;
        int tag = u.Size(PhoneSaves.TagText);
        float badge = auto ? _gothic.GetStringSize("Auto", fontSize: tag).X + u.Px(40) : 0;
        float ring = auto ? _gothic.GetStringSize(held!.RingText, fontSize: tag).X + u.Px(28) : 0;
        float tags = auto ? u.Px(24) + badge + u.Px(16) + ring : 0;
        int size = SideCells.Fit(_cinzel, title, u.Size(Places.PageTitleText(u.Frame)), room - tags);
        Text(_cinzel, size, x, middle, title, Ui.Name);
        if (!auto)
            return;
        float bx = x + _cinzel.GetStringSize(title, fontSize: size).X + u.Px(24), h = tag + u.Px(16);
        DrawRect(new Rect2(bx, middle - h / 2, badge, h), AutoGold);
        TextCentred(_gothic, tag, bx + badge / 2, middle, "Auto", TagText);
        PhoneRingTag(u, bx + badge + u.Px(16) + ring, middle, held!, tag);
    }

    /// <summary>The comment's label, its box and Edit (<c>land_save_page</c>).</summary>
    private void PhoneComment(PhoneUnits u, SaveSlot held, bool lines)
    {
        var f = u.Frame;
        int label = u.Size(PhoneSaves.LabelText);
        DrawString(_cinzelLight, new Vector2(u.Rect(Places.Page(f)).Position.X + u.Px(PhoneSaves.FigureIn),
            Places.CommentLabel(f, lines) / u.K + _cinzelLight.GetAscent(label)), "COMMENT", fontSize: label, modulate: Ui.Subtitle);
        var box = u.Rect(Places.Comment(f, lines));
        DrawRect(box, Ui.Row);
        DrawRect(box, Ui.DimGold, false, Line(1));
        bool any = held.Comment.Length > 0;
        int size = u.Size(PhoneSaves.CommentText);
        float x = box.Position.X + u.Px(PhoneSaves.CommentIn);
        Text(_gothic, size, x, box.GetCenter().Y, any ? Fit(held.Comment, size, box.End.X - u.Px(PhoneSaves.CommentIn) - x) : "none — Edit writes one",
            any ? Ui.Value : Ui.Label);
        var edit = EditBox;
        bool hover = _hovered == EditHit;
        Ui.DrawRowBox(this, edit, hover, _k);
        TextCentred(_brush, u.Size(PhoneSaves.EditText), edit.GetCenter().X, edit.GetCenter().Y, "Edit", hover ? Ui.Name : Ui.Text);
    }

    /// <summary>Back at the foot's right (<c>land_back</c>): a panel, the gold triangle, its word in the brush face.</summary>
    private void PhoneBack(PhoneUnits u)
    {
        var r = BackBox;
        bool hover = _hovered == BackHit;
        Ui.DrawPanel(this, r, _k, hover ? Ui.RowHover : null);
        float cy = r.GetCenter().Y, ax = r.Position.X + u.Px(52);
        DrawColoredPolygon([new(ax + u.Px(32), cy - u.Px(18)), new(ax, cy), new(ax + u.Px(32), cy + u.Px(18))], Ui.Gold);
        int size = SideCells.Fit(_brush, "Back", u.Size(PhoneSaves.BackText), r.End.X - ax - u.Px(70));
        TextCentred(_brush, size, (ax + u.Px(44) + r.End.X) / 2, cy, "Back", hover ? Ui.Name : Ui.Text);
    }

    /// <summary>The red button: its word, in Save what a save writes under it.</summary>
    private void PhoneButton(PhoneUnits u, string word, string? saving)
    {
        var r = ButtonBox;
        bool hover = _hovered == ButtonHit;
        Ui.DrawRedButton(this, r, hover, hover && _pressed == ButtonHit, _k);
        float cx = r.GetCenter().X, room = r.Size.X - u.Px(60);
        if (saving is null)
        {
            TextCentred(_brush, SideCells.Fit(_brush, word, u.Size(Places.ButtonText(u.Frame)), room), cx, r.GetCenter().Y, word, Ui.Name);
            return;
        }
        TextCentred(_brush, SideCells.Fit(_brush, word, u.Size(Places.ButtonText(u.Frame) * 0.86f), room), cx, r.Position.Y + r.Size.Y * 0.4f, word, Ui.Name);
        int line = SideCells.Fit(_gothic, saving, u.Size(PhoneSaves.ButtonLineText), room);
        TextCentred(_gothic, line, cx, r.Position.Y + r.Size.Y * 0.77f, saving, Ui.Name with { A = 0.85f });
    }

    /// <summary>The red button off in Save: an auto-save is the game's own, not saved over.</summary>
    private void PhoneOffButton(PhoneUnits u)
    {
        var r = ButtonBox;
        DrawRect(r, Ui.OffButton);
        DrawRect(r, Ui.OffLine, false, Frame(2));
        TextCentred(_brush, u.Size(Places.ButtonText(u.Frame) * 0.86f), r.GetCenter().X, r.Position.Y + r.Size.Y * 0.4f, "Save", Ui.OffText);
        TextCentred(_gothic, u.Size(PhoneSaves.ButtonLineText), r.GetCenter().X, r.Position.Y + r.Size.Y * 0.77f, "auto-saves are the game's own", Ui.OffText);
    }

    /// <summary>Delete as the PC's danger button (<c>bin_button</c>): its bin before the word.</summary>
    private void PhoneDelete(PhoneUnits u)
    {
        var r = DeleteBox;
        bool hover = _hovered == DeleteHit || _confirm;
        Ui.DrawDangerButton(this, r, hover, _k);
        var colour = hover ? Ui.Name : Ui.DangerText;
        float s = u.Px(22) / 14;                                        // the bin 22 reference pixels wide
        DrawBin(r.Position.X + u.Px(30), r.GetCenter().Y - 8 * s, colour, s);
        TextCentred(_cinzel, u.Size(PhoneSaves.DeleteText), (r.Position.X + u.Px(70) + r.End.X) / 2, r.GetCenter().Y, "Delete", colour);
    }

    /// <summary>Delete's question hanging under it (the PC's words): Delete | Keep.</summary>
    private void PhoneDeleteQuestion(PhoneUnits u)
    {
        if (ChosenSave is not { } held)
            return;
        var box = u.Rect(Places.DeleteQuestion(u.Frame));
        Ui.DrawPanel(this, box, _k);
        float tip = DeleteBox.GetCenter().X, top = box.Position.Y, t = u.Px(PhoneSaves.Tip);
        DrawColoredPolygon([new(tip - t, top + 2), new(tip + t, top + 2), new(tip, top - t)], Ui.Gold);
        string what = held.IsAuto ? $"this {held.RingText} auto-save" : $"{held.Label}, {held.DateText}";
        PhoneQuestionLines(u, box, $"Delete {what}?", "It cannot be undone.");
        PhoneAnswers(u, YesBox, NoBox, "Delete", DeleteYesHit, DeleteNoHit);
    }

    /// <summary>The sweep's question beside it, the tip pointing left at it so the list stays in view: Delete n | Keep.</summary>
    private void PhoneSweepQuestion(PhoneUnits u)
    {
        var shelf = _shelf.Manuals;
        int gone = SaveSlots.Uncommented(shelf).Count, kept = shelf.Count - gone;
        var box = u.Rect(Places.SweepQuestion(u.Frame));
        Ui.DrawPanel(this, box, _k);
        if (u.Frame.IsUpright)
            DrawUprightSweepTip(u, box);                                // held upright it hangs under the sweep
        else
        {
            float left = box.Position.X, tip = SweepBox.GetCenter().Y, t = u.Px(PhoneSaves.Tip);
            DrawColoredPolygon([new(left + 2, tip - t), new(left + 2, tip + t), new(left - t, tip)], Ui.Gold);
        }
        string stay = kept == 0 ? "That is every manual save." : kept == 1 ? "The one with a comment (★) stays." : $"The {kept} with one (★) stay.";
        PhoneQuestionLines(u, box, gone == 1 ? "Delete the manual save without a comment?" : $"Delete the {gone} manual saves without a comment?",
            stay + " It cannot be undone.");
        PhoneAnswers(u, SweepYesBox, SweepNoBox, $"Delete {gone}", SweepYesHit, SweepNoHit);
    }

    private void PhoneQuestionLines(PhoneUnits u, Rect2 box, string first, string second)
    {
        int size = u.Size(PhoneSaves.QuestionText);
        float x = box.Position.X + u.Px(PhoneSaves.QuestionIn), room = box.End.X - u.Px(PhoneSaves.QuestionIn) - x;
        Text(_gothic, size, x, box.Position.Y + u.Px(PhoneSaves.QuestionFirst), Fit(first, size, room), Ui.Text);
        Text(_gothic, size, x, box.Position.Y + u.Px(PhoneSaves.QuestionSecond), Fit(second, size, room), Ui.Text);
    }

    private void PhoneAnswers(PhoneUnits u, Rect2 yes, Rect2 no, string word, int yesHit, int noHit)
    {
        bool lit = _hovered == yesHit;
        Ui.DrawRedButton(this, yes, lit, lit && _pressed == yesHit, _k);
        TextCentred(_brush, SideCells.Fit(_brush, word, u.Size(PhoneSaves.AnswerText), yes.Size.X - u.Px(24)), yes.GetCenter().X, yes.GetCenter().Y,
            word, Ui.Name);
        bool keep = _hovered == noHit;
        Ui.DrawRowBox(this, no, keep, _k);
        TextCentred(_cinzel, u.Size(34), no.GetCenter().X, no.GetCenter().Y, "Keep", keep ? Ui.Name : Ui.Text);
    }

    /// <summary>
    /// The comment's own card over the dimmed screen (<c>ly1</c>): "COMMENT" and whose, the field (paper in a vermilion
    /// frame, the line edit in it), the count under its right, Keep | Cancel.
    /// </summary>
    private void PhoneCard(PhoneUnits u)
    {
        var f = u.Frame;
        DrawRect(new Rect2(Vector2.Zero, Size / _k), new Color(0, 0, 0, 0.6f));
        var card = u.Rect(PhoneSaves.Card(f, _keyboard));
        Ui.DrawPanel(this, card, _k);
        float middle = card.Position.Y + u.Px(PhoneSaves.CardLabelMiddle);
        Text(_cinzelLight, u.Size(PhoneSaves.CardLabelText), card.Position.X + u.Px(PhoneSaves.CardLabelIn), middle, "COMMENT", Ui.Subtitle);
        if (ChosenSave is { } held)
        {
            string whose = held.IsAuto ? $"{held.RingText} auto-save · {held.DateText}" : $"{held.Label} · {held.DateText}";
            float x = card.Position.X + u.Px(PhoneSaves.CardTitleIn);
            int size = SideCells.Fit(_cinzel, whose, u.Size(PhoneSaves.CardTitleText), card.End.X - u.Px(32) - x);
            Text(_cinzel, size, x, middle, whose, Ui.Name);
        }
        var field = u.Rect(PhoneSaves.Field(f, _keyboard));
        DrawRect(field, Ui.Paper);
        DrawRect(field, Ui.Vermilion, false, Line(2));
        var (right, top) = PhoneSaves.Count(f, _keyboard);
        int count = u.Size(PhoneSaves.CountText);
        string text = $"{_edit.Text.Length} / {CommentLength}";
        DrawString(_gothic, new Vector2(right / u.K - _gothic.GetStringSize(text, fontSize: count).X, top / u.K + _gothic.GetAscent(count)), text,
            fontSize: count, modulate: Ui.Quiet);
        var cancel = u.Rect(PhoneSaves.Cancel(f, _keyboard));
        bool hover = _hovered == CancelHit;
        Ui.DrawRowBox(this, cancel, hover, _k);
        TextCentred(_brush, u.Size(PhoneSaves.CancelText), cancel.GetCenter().X, cancel.GetCenter().Y, "Cancel", hover ? Ui.Name : Ui.Text);
        var keep = u.Rect(PhoneSaves.Keep(f, _keyboard));
        bool lit = _hovered == KeepHit;
        Ui.DrawRedButton(this, keep, lit, lit && _pressed == KeepHit, _k);
        TextCentred(_brush, u.Size(PhoneSaves.KeepText), keep.GetCenter().X, keep.GetCenter().Y, "Keep", Ui.Name);
    }

    /// <summary>A layer of the screen drawn by its view.</summary>
    private sealed partial class Layer : Control
    {
        private readonly Action<Control> _paint;

        public Layer(Action<Control> paint)
        {
            _paint = paint;
            MouseFilter = MouseFilterEnum.Ignore;
        }

        public override void _Draw() => _paint(this);
    }
}
