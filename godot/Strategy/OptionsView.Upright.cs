using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The options window on the phone held upright (docs/ui/phone-build.md, step P8a; docs/ui/phone.md, §10 and §21: PO1 with
// PO1o): the title under the hole, the five paper tabs under it across the room, the sheet under them down to the bar; each
// tab in one column — Messages: the text speed and its preview, the message skip, the read text; Display: the screen's
// orientation on top as PO1o's rows, then the font, the palette, the thousands one under another and the unit tables' row
// heights two a row; Battle: the effects, the timings one under another; Game: the folder; Tweaks: held landscape's left
// column, then its right —, scrolled by a finger within the sheet (FingerScroll, as the save list's: a quick release
// flicks it on), a thin bar at the sheet's right while it scrolls. What scrolls past the sheet's margins is cut there: the
// item's clip is the column's view, the backdrop, the tabs, the bar and a question drawn outside it (clip ignored), the
// preview's 121, a child, cut with the column. Defaults | Close in the bar, Close at the right thumb. The parts are held
// landscape's at PO1's type (PhoneOptions.PartScale); the places PhoneOptionsUpright's.
internal sealed partial class OptionsView
{
    private readonly FingerScroll _finger = new();
    private float _scroll, _scrollFrom;                         // the column's scroll (canvas units), and where a drag took it
    private readonly Dictionary<Tab, float> _columnHeight = [];  // each tab's column as last drawn (canvas units)

    /// <summary>The space between a section's last line and the next section's head, in the parts' units.</summary>
    private const float UprightGap = 36;

    /// <summary>A notch of the wheel over the column, in reference pixels.</summary>
    private const float UprightWheel = 120;

    /// <summary>The column's left, its top at the scroll and its width, in the parts' units (no height: it is as long as its tab).</summary>
    private Rect2 UprightColumn(PhoneFrame f)
    {
        var view = PhoneOptionsUpright.View(f);
        return new Rect2(view.X / _k, PhoneOptionsUpright.ColumnTop(f, _scroll) / _k, view.W / _k, 0);
    }

    private float MaxScroll(PhoneFrame f) => PhoneOptionsUpright.MaxScroll(f, _columnHeight.GetValueOrDefault(_tab));

    /// <summary>The column's clip, in the item's own units: the sheet's width, the view's height.</summary>
    private static Rect2 UprightClip(PhoneFrame f)
    {
        var sheet = PhoneOptionsUpright.Sheet(f);
        var view = PhoneOptionsUpright.View(f);
        return new Rect2(sheet.X, view.Y, sheet.W, view.H);
    }

    private void DrawUpright(PhoneFrame f)
    {
        var item = GetCanvasItem();
        var clip = UprightClip(f);
        RenderingServer.CanvasItemSetCustomRect(item, true, clip);   // the Control sets its own again at each drawing
        RenderingServer.CanvasItemSetClip(item, true);
        RenderingServer.CanvasItemAddClipIgnore(item, true);

        DrawSetTransform(Vector2.Zero, 0, new Vector2(f.Scale, f.Scale));
        Ui.DrawWaveBackdrop(this, f.Scale, new Vector2(f.Screen.W, f.Screen.H) / f.Scale);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        var (tx, ty) = PhoneOptionsUpright.Title(f);
        int titleSize = Ref(PhoneOptionsUpright.TitleText);
        Text(_brush, new Vector2(tx / _k, ty / _k + _brush.GetAscent(titleSize)), "Options", fontSize: titleSize, modulate: Ui.Name);
        DrawPhoneTabs(f);

        RenderingServer.CanvasItemAddClipIgnore(item, false);
        int first = _spots.Count;
        var column = UprightColumn(f);
        float x = column.Position.X, top = column.Position.Y, w = column.Size.X;
        float foot = _tab switch
        {
            Tab.Messages => DrawMessagesUpright(x, top, w),
            Tab.Display => DrawDisplayUpright(x, top, w),
            Tab.Battle => DrawBattleUpright(x, top, w),
            Tab.Game => GameSection(x, top, w),
            _ => DrawTweaksUpright(x, top, w),
        };
        _columnHeight[_tab] = (foot - top) * _k;
        ClipSpots(first, new Rect2(clip.Position / _k, clip.Size / _k));
        RenderingServer.CanvasItemAddClipIgnore(item, true);

        if (PhoneOptionsUpright.ScrollBar(f, _columnHeight[_tab], _scroll) is { } bar)     // the reference on the paper, PB
            Ui.DrawScrollBar(this, Part(bar.Track), Part(bar.Thumb), f.Px(PhoneScrollBar.Rim) / _k, paper: true);
        DrawUprightFoot(f);
        if (_question is not null)
            DrawQuestion(_question);
        if (_scroll > MaxScroll(f))                     // the column is shorter than when it was scrolled (a font, a turn)
            Callable.From(() => SetScroll(f, _scroll)).CallDeferred();    }

    /// <summary>The column's spots from <paramref name="first"/> on cut to <paramref name="clip"/> (the parts' units): one scrolled out of it is gone.</summary>
    private void ClipSpots(int first, Rect2 clip)
    {
        for (int i = _spots.Count - 1; i >= first; i--)
        {
            if (clip.Intersects(_spots[i].Rect))
                _spots[i] = _spots[i] with { Rect = clip.Intersection(_spots[i].Rect) };
            else
                _spots.RemoveAt(i);
        }
    }

    /// <summary>Defaults, a lacquer plate (deaf on Game), and Close, red, in the bar — drawn in the frame's layout pixels as the bar's other buttons.</summary>
    private void DrawUprightFoot(PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        var defaults = u.Rect(PhoneOptionsUpright.Defaults(f));
        bool enabled = _tab != Tab.Game, lit = enabled && _hover == "defaults";
        Ui.DrawPanel(this, defaults, u.K, lit ? Ui.RowHover : null);
        int size = SideCells.Fit(_cinzelBold, "Defaults", u.Size(PhoneOptionsUpright.DefaultsText), defaults.Size.X - u.Px(30));
        DrawString(_cinzelBold, new Vector2(defaults.Position.X, Lacquer.Baseline(defaults, _cinzelBold, size)), "Defaults",
            HorizontalAlignment.Center, defaults.Size.X, size, !enabled ? Ui.OffText : lit ? Ui.Name : Ui.Text);
        var close = u.Rect(PhoneOptionsUpright.Close(f));
        bool closeLit = _hover == "close";
        Ui.DrawRedButton(this, close, closeLit, closeLit && _pressed == "close", u.K);
        int closeSize = SideCells.Fit(_brush, "Close", u.Size(PhoneOptionsUpright.CloseText), close.Size.X - u.Px(30));
        DrawString(_brush, new Vector2(close.Position.X, Lacquer.Baseline(close, _brush, closeSize)), "Close",
            HorizontalAlignment.Center, close.Size.X, closeSize, Ui.Name);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (enabled)
            _spots.Add(new Spot("defaults", Part(PhoneOptionsUpright.Defaults(f)), Defaults));
        _spots.Add(new Spot("close", Part(PhoneOptionsUpright.Close(f)), Close));
    }

    // ---------------------------------------------------------------- the tabs' columns (the parts' units; each returns its last line)

    /// <summary>Messages (PO1): the text speed, its preview centred under it, the message skip, the read text.</summary>
    private float DrawMessagesUpright(float x, float y, float w)
    {
        SectionHead(x, y, "Text speed", w);
        SpeedSlider(x, y + PhoneSpeedSlider, w);
        y = PreviewRect.End.Y + PhoneNoteUnder;
        Text(_gothic, new Vector2(x, y), ReadTextChoice.Index == ReadTextChoice.None ? "Read text keeps the window's colour."
            : "The preview counts as read.", fontSize: 13, modulate: Ui.InkQuiet);
        y += UprightGap;
        MessageSkipColumn(x, y, w);                 // the mouse not offered in the touch mode: its foot under "Forget them…"
        return ReadTextSection(x, y + 165 + UprightGap, w);
    }

    /// <summary>
    /// Display (PO1o): the screen's orientation on top, a row for each way; then the font, the palette, the thousands one
    /// under another (a row has no room for them side by side) and the unit tables' row heights two a row.
    /// </summary>
    private float DrawDisplayUpright(float x, float y, float w)
    {
        y = OrientationRows(x, y, w) + UprightGap;
        SectionHead(x, y, "Screen margins", w);                 // docs/ui/screen-margins.md: a row as the orientation's
        y += P(PhoneOptionsUpright.OrientationRowsUnder);
        MarginsField(new Rect2(x, y, w, P(PhoneOptionsUpright.OrientationRow)));
        y += P(PhoneOptionsUpright.OrientationRow) + UprightGap;
        FontSection(x, y, w);
        y += 74 + 20 + UprightGap;
        PaletteSection(x, y, w);
        y += 100 + UprightGap;
        SectionHead(x, y, "Thousands in gold, troops and damage", w);
        for (int i = 0; i < ThousandsChoice.All.Length; i++)
        {
            var separator = ThousandsChoice.All[i];
            Radio("thousands-" + separator, x, y + 38 + i * 36, ThousandsChoice.Chosen == separator, ThousandsChoice.Sample(separator),
                _gothic, () => SetThousands(separator));
        }
        y += 38 + (ThousandsChoice.All.Length - 1) * 36 + 20 + UprightGap;
        return RowsUpright(x, y, w);
    }

    /// <summary>PO1o's rows: each way a row the whole of which taps, its radio, its name and its line under it; the chosen one's row lit.</summary>
    private float OrientationRows(float x, float y, float w)
    {
        SectionHead(x, y, "Screen orientation", w);
        y += P(PhoneOptionsUpright.OrientationRowsUnder);
        var all = OrientationChoice.All;
        float height = P(PhoneOptionsUpright.OrientationRow), textX = x + P(PhoneOptionsUpright.OrientationTextIn);
        for (int i = 0; i < all.Length; i++)
        {
            var (way, name, hint) = all[i];
            string id = "orientation-" + way;
            bool on = OrientationChoice.Chosen == way, lit = _hover == id;
            var row = new Rect2(x, y, w, height);
            DrawRect(row, on ? Field : Ui.Paper);
            DrawRect(row, lit ? Ui.Vermilion : Ui.PaperRule, false, Line(lit ? 2 : 1));
            float middle = y + P(PhoneOptionsUpright.OrientationNameMiddle);
            RadioMark(new Vector2(x + P(PhoneOptionsUpright.OrientationRadioIn), middle), on, lit);
            Text(_gothic, new Vector2(textX, PhonePickerParts.Middle(_gothic, 17, middle)), name, fontSize: 17, modulate: Ui.Ink);
            int hintSize = SideCells.Fit(_gothic, hint, 13, row.End.X - textX - 8);
            Text(_gothic, new Vector2(textX, PhonePickerParts.Middle(_gothic, hintSize, y + P(PhoneOptionsUpright.OrientationHintMiddle))),
                hint, fontSize: hintSize, modulate: Ui.InkQuiet);
            _spots.Add(new Spot(id, row, () => { OrientationChoice.Chosen = way; QueueRedraw(); }));
            y += P(PhoneOptionsUpright.OrientationPitch);
        }
        return y - P(PhoneOptionsUpright.OrientationPitch - PhoneOptionsUpright.OrientationRow);
    }

    /// <summary>
    /// The unit tables' row heights: the note under the head, the tiles two a row (a third would crowd their figures), each
    /// row of tiles as tall as its taller one and its labels over it.
    /// </summary>
    private float RowsUpright(float x, float y, float w)
    {
        SectionHead(x, y, "Unit tables: row height", w);
        int lines = Wrap("Subordinates and deployment. Each device keeps its own.", x, y + 42, w, 13, Ui.InkQuiet);
        float top = y + 42 + (lines - 1) * WrapPitch(13) + 40, tile = (w - RowTileGap) / 2;
        var heights = LedgerRowsChoice.Heights;
        for (int i = 0; i < heights.Length; i += 2)
        {
            RowTile(x, top, tile, heights[i]);
            int tallest = heights[i];
            if (i + 1 < heights.Length)
            {
                RowTile(x + tile + RowTileGap, top, tile, heights[i + 1]);
                tallest = Math.Max(tallest, heights[i + 1]);
            }
            top += 2 * tallest + 2 + 40;            // a later row's spot wins where the tiles' tall spots reach into it
        }
        return top - 40;
    }

    /// <summary>Battle: the effects, the timings one under another.</summary>
    private float DrawBattleUpright(float x, float y, float w)
    {
        y = EffectsSection(x, y, w, wrap: true) + UprightGap;
        SectionHead(x, y, "Timings", w);
        y += 36;
        foreach (var setting in Timings)
        {
            TimingRow(setting, x, y, w);
            y += 46;
        }
        return y - 6;
    }

    /// <summary>
    /// Tweaks: what they are; Battle — the damage forecast, "Chaos and Nikkou reach demons" with its three choices one under
    /// another, the ninja follow-up's chances in a row in short words —; Scenes. The hints wrapped into the column.
    /// </summary>
    private float DrawTweaksUpright(float x, float y, float w)
    {
        int lines = Wrap(TweaksIntro, x, y + 14, w, 14, Ui.InkQuiet);
        y += 14 + (lines - 1) * WrapPitch(14) + 30;
        SectionHead(x, y, "Battle", w);
        float foot = ForecastBox(x, y + 38, w);
        float tx = x + 32, tw = w - 32;
        Text(_gothic, new Vector2(tx, foot + 24), "Chaos and Nikkou reach demons", fontSize: 17, modulate: Ui.Ink);
        lines = Wrap(ReachHint, tx, foot + 43, tw, 13, Ui.InkQuiet);
        float ry = foot + 43 + (lines - 1) * WrapPitch(13) + 14;
        for (int i = 0; i < Reaches.Length; i++)
            ReachRadio(i, tx, ry + i * 36);
        y = ry + Reaches.Length * 36 + 14;
        lines = Wrap(ReachNote, tx, y, tw, 13, Ui.InkQuiet);
        y += (lines - 1) * WrapPitch(13) + 40;

        Text(_gothic, new Vector2(tx, y), "Ninja follow-up", fontSize: 17, modulate: Ui.Ink);
        lines = Wrap(NinjaHint, tx, y + 19, tw, 13, Ui.InkQuiet);
        ry = y + 19 + (lines - 1) * WrapPitch(13) + 14;
        for (int i = 0; i < PhoneNinjas.Length; i++)
            NinjaRadio(PhoneNinjas[i], tx + i * (tw / PhoneNinjas.Length), ry);
        Text(_gothic, new Vector2(tx, ry + 50), "33% is the original's.", fontSize: 13, modulate: Ui.InkQuiet);
        y = ry + 50 + UprightGap;
        SectionHead(x, y, "Scenes", w);
        return NewScenesBox(x, y + 38, w);
    }

    // ---------------------------------------------------------------- the finger

    /// <summary>
    /// The finger or the wheel over the column: returns whether <paramref name="e"/> was taken. A press goes on to the spots
    /// (a tap acts at its release, a slider's press drags it); moved past the slop it scrolls the column instead, and its
    /// quick release flicks it on; a press while a flick goes on stops it, never tapping.
    /// </summary>
    private bool FingerTouch(InputEvent e, PhoneFrame f)
    {
        var view = PhoneOptionsUpright.View(f);
        var area = new Rect2(view.X, view.Y, view.W, view.H);
        switch (e)
        {
            case InputEventMouseMotion motion when _finger.Down:
                if (_finger.Move(motion.Position))
                    StartScroll();
                if (!_finger.Dragging)
                    return false;
                SetScroll(f, _scrollFrom - (motion.Position.Y - _finger.Origin.Y));
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                if (!area.HasPoint(press.Position) || SpotAt(press.Position / _k)?.Slider is not null)
                    return false;
                if (!_finger.Press(press.Position))
                    return false;
                StartScroll();                              // it stopped a flick: it scrolls from here, never taps
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _finger.Down:
                if (!_finger.Release(release.Position))
                    return false;
                _pressed = null;
                QueueRedraw();
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel
                when area.HasPoint(wheel.Position):
                SetScroll(f, _scroll + (wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1) * f.Px(UprightWheel));
                return true;
        }
        return false;
    }

    private void StartScroll()
    {
        _scrollFrom = _scroll;
        _pressed = null;
        _hover = null;
        QueueRedraw();
    }

    /// <summary>The column scrolled to <paramref name="scroll"/>, kept within its ends; the preview follows it.</summary>
    private void SetScroll(PhoneFrame f, float scroll)
    {
        scroll = Math.Clamp(scroll, 0, MaxScroll(f));
        if (Math.Abs(scroll - _scroll) < 0.01f)
            return;
        _scroll = scroll;
        PlaceChildren();
        QueueRedraw();
    }

    /// <summary>A frame of a flick going on; the column slows to a stop at either end.</summary>
    private void Fling(double delta)
    {
        if (_phone is not { IsUpright: true } f)
        {
            _finger.Cancel();
            return;
        }
        _finger.StopX();
        float max = MaxScroll(f), scroll = _scroll - _finger.Step(delta).Y;
        if (scroll <= 0 || scroll >= max)
        {
            scroll = Math.Clamp(scroll, 0, max);
            _finger.StopY();
        }
        SetScroll(f, scroll);
    }
}
