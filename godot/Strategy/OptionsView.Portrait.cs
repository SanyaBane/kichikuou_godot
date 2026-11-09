using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The options window on the phone held upright (docs/ui/phone-build.md, step P8a; docs/ui/phone.md, §10 and §21: PO1 with
// PO1o): the title under the hole, the five paper tabs under it across the room, the sheet under them down to the bar; each
// tab in one column — Messages: the text speed and its preview, the message skip, the read text; Display: the screen's
// orientation on top as PO1o's rows, the margins, the frame rate as the orientation's rows, then the font, the palette,
// the thousands one under another and the unit tables' row heights two a row; Battle: the effects, the timings one under
// another; Game: the folder; Tweaks: the battle's three with their choices one under another, the scenes —, scrolled within
// the sheet as every device's column (OptionsView.Scroll.cs). Defaults | Close in the bar, Close at the right thumb. The
// parts are held landscape's at PO1's type (PhoneOptions.PartScale); the places PhoneOptionsPortrait's.
internal sealed partial class OptionsView
{
    /// <summary>The space between a section's last line and the next section's head, in the parts' units.</summary>
    private const float PortraitGap = 36;

    private void DrawPortrait(PhoneFrame f)
    {
        BeginClip();
        DrawPhoneBackdrop(f);
        var (tx, ty) = PhoneOptionsPortrait.Title(f);
        int titleSize = Ref(PhoneOptionsPortrait.TitleText);
        Text(_brush, new Vector2(tx / _k, ty / _k + _brush.GetAscent(titleSize)), "Options", fontSize: titleSize, modulate: Ui.Name);
        DrawPhoneTabs(f);
        DrawColumn((x, top, w) => _tab switch
        {
            Tab.Messages => DrawMessagesPortrait(x, top, w),
            Tab.Display => DrawDisplayPortrait(x, top, w),
            Tab.Battle => DrawBattleColumn(x, top, w),
            Tab.Game => GameSection(x, top, w),
            _ => DrawTweaksPortrait(x, top, w),
        });
        DrawPortraitFoot(f);
        if (_question is not null)
            DrawQuestion(_question);
    }

    /// <summary>Defaults, a lacquer plate (deaf on Game), and Close, red, in the bar — drawn in the frame's layout pixels as the bar's other buttons.</summary>
    private void DrawPortraitFoot(PhoneFrame f)
    {
        var u = new PhoneUnits(f);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(u.K, u.K));
        var defaults = u.Rect(PhoneOptionsPortrait.Defaults(f));
        bool enabled = _tab != Tab.Game, lit = enabled && _hover == "defaults";
        Ui.DrawPanel(this, defaults, u.K, lit ? Ui.RowHover : null);
        var close = u.Rect(PhoneOptionsPortrait.Close(f));
        var face = SideCells.Cinzel;                                    // one face and size for both (docs/ui/style.md)
        int size = SideCells.FitAll(face, u.Size(PhoneOptionsPortrait.ButtonText), ("Defaults", defaults.Size.X - u.Px(30)),
            ("Close", close.Size.X - u.Px(30)));
        DrawString(face, new Vector2(defaults.Position.X, Lacquer.Baseline(defaults, face, size)), "Defaults",
            HorizontalAlignment.Center, defaults.Size.X, size, !enabled ? Ui.OffText : lit ? Ui.Name : Ui.Text);
        bool closeLit = _hover == "close";
        Ui.DrawRedButton(this, close, closeLit, closeLit && _pressed == "close", u.K);
        DrawString(face, new Vector2(close.Position.X, Lacquer.Baseline(close, face, size)), "Close",
            HorizontalAlignment.Center, close.Size.X, size, Ui.Name);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
        if (enabled)
            _spots.Add(new Spot("defaults", Part(PhoneOptionsPortrait.Defaults(f)), AskDefaults));
        _spots.Add(new Spot("close", Part(PhoneOptionsPortrait.Close(f)), Close));
    }

    // ---------------------------------------------------------------- the tabs' columns (the parts' units; each returns its last line)

    /// <summary>Messages (PO1): the text speed, its preview centred under it, the message skip, the read text.</summary>
    private float DrawMessagesPortrait(float x, float y, float w)
    {
        SectionHead(x, y, "Text speed", w);
        SpeedSlider(x, y + PhoneSpeedSlider, w);
        y = PreviewRect.End.Y + PhoneNoteUnder;
        Text(_gothic, new Vector2(x, y), ReadTextChoice.Index == ReadTextChoice.None ? "Read text keeps the window's colour."
            : "The preview counts as read.", fontSize: 13, modulate: Ui.InkQuiet);
        y += PortraitGap;
        MessageSkipColumn(x, y, w);                 // the mouse not offered in the touch mode: its foot under "Forget them…"
        return ReadTextSection(x, y + 165 + PortraitGap, w);
    }

    /// <summary>
    /// Display (PO1o): the screen's orientation on top, a row for each way; the screen margins; the frame rate as the
    /// orientation's rows (owner, 2026-11-03, mock-up A); then the font, the palette, the thousands one under another (a row
    /// has no room for them side by side) and the unit tables' row heights two a row.
    /// </summary>
    private float DrawDisplayPortrait(float x, float y, float w)
    {
        y = BoxedRows(x, y, w, "Screen orientation", [.. OrientationChoice.All.Select(o => new BoxedRow("orientation-" + o.Way, o.Name,
            o.Hint, OrientationChoice.Chosen == o.Way, () => OrientationChoice.Chosen = o.Way))]) + PortraitGap;
        SectionHead(x, y, "Screen margins", w);                 // docs/ui/screen-margins.md: a row as the orientation's
        y += P(PhoneOptionsPortrait.OrientationRowsUnder);
        MarginsField(new Rect2(x, y, w, P(PhoneOptionsPortrait.OrientationRow)));
        y += P(PhoneOptionsPortrait.OrientationRow) + PortraitGap;
        y = BoxedRows(x, y, w, "Frame rate", [.. FrameRateChoice.Rates.Select(rate => new BoxedRow("frame-rate-" + rate,
            FrameRateChoice.Name(rate), FrameRateChoice.Hint(rate), FrameRateChoice.Chosen == rate, () => FrameRateChoice.Chosen = rate))])
            + PortraitGap;
        FontSection(x, y, w);
        y += 74 + 20 + PortraitGap;
        PaletteSection(x, y, w);
        y += 100 + PortraitGap;
        SectionHead(x, y, "Thousands in gold, troops and damage", w);
        for (int i = 0; i < ThousandsChoice.All.Length; i++)
        {
            var separator = ThousandsChoice.All[i];
            Radio("thousands-" + separator, x, y + 38 + i * 36, ThousandsChoice.Chosen == separator, ThousandsChoice.Sample(separator),
                _gothic, () => SetThousands(separator));
        }
        y += 38 + (ThousandsChoice.All.Length - 1) * 36 + 20 + PortraitGap;
        return RowsPortrait(x, y, w);
    }

    /// <summary>A choice of PO1o's rows: its spot's id, its name and the line under it, whether it is chosen, what a tap does.</summary>
    private sealed record BoxedRow(string Id, string Name, string Hint, bool On, Action Choose);

    /// <summary>
    /// PO1o's rows under <paramref name="title"/> (the orientation's, the frame rate's): each choice a row the whole of which
    /// taps, its radio, its name and its line under it; the chosen one's row lit. Returns the last row's foot.
    /// </summary>
    private float BoxedRows(float x, float y, float w, string title, BoxedRow[] rows)
    {
        SectionHead(x, y, title, w);
        y += P(PhoneOptionsPortrait.OrientationRowsUnder);
        float height = P(PhoneOptionsPortrait.OrientationRow), textX = x + P(PhoneOptionsPortrait.OrientationTextIn);
        foreach (var (id, name, hint, on, choose) in rows)
        {
            bool lit = _hover == id;
            var row = new Rect2(x, y, w, height);
            DrawRect(row, on ? Field : Ui.Paper);
            DrawRect(row, lit ? Ui.Vermilion : Ui.PaperRule, false, Line(lit ? 2 : 1));
            float middle = y + P(PhoneOptionsPortrait.OrientationNameMiddle);
            RadioMark(new Vector2(x + P(PhoneOptionsPortrait.OrientationRadioIn), middle), on, lit);
            Text(_gothic, new Vector2(textX, PhonePickerParts.Middle(_gothic, 17, middle)), name, fontSize: 17, modulate: Ui.Ink);
            int hintSize = SideCells.Fit(_gothic, hint, 13, row.End.X - textX - 8);
            Text(_gothic, new Vector2(textX, PhonePickerParts.Middle(_gothic, hintSize, y + P(PhoneOptionsPortrait.OrientationHintMiddle))),
                hint, fontSize: hintSize, modulate: Ui.InkQuiet);
            _spots.Add(new Spot(id, row, () => { choose(); QueueRedraw(); }));
            y += P(PhoneOptionsPortrait.OrientationPitch);
        }
        return y - P(PhoneOptionsPortrait.OrientationPitch - PhoneOptionsPortrait.OrientationRow);
    }

    /// <summary>
    /// The unit tables' row heights: the note under the head, the tiles two a row (a third would crowd their figures), each
    /// row of tiles as tall as its taller one and its labels over it.
    /// </summary>
    private float RowsPortrait(float x, float y, float w)
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

    /// <summary>
    /// Tweaks: what they are; Battle — the damage forecast, "Chaos and Nikkou reach demons" with its three choices one under
    /// another, the ninja follow-up's chances in a row in short words —; Scenes. The hints wrapped into the column.
    /// </summary>
    private float DrawTweaksPortrait(float x, float y, float w)
    {
        int lines = Wrap(TweaksIntro, x, y + 14, w, 14, Ui.InkQuiet);
        y += 14 + (lines - 1) * WrapPitch(14) + 30;
        SectionHead(x, y, "Battle", w);
        float foot = ForecastBox(x, y + 38, w);
        // The radio groups start at the check box's edge, as on the PC (owner, 2026-11-10).
        Text(_gothic, new Vector2(x, foot + 24), "Chaos and Nikkou reach demons", fontSize: 17, modulate: Ui.Ink);
        lines = Wrap(ReachHint, x, foot + 43, w, 13, Ui.InkQuiet);
        float ry = foot + 43 + (lines - 1) * WrapPitch(13) + 14;
        for (int i = 0; i < Reaches.Length; i++)
            ReachRadio(i, x, ry + i * 36);
        y = ry + Reaches.Length * 36 + 14;
        lines = Wrap(ReachNote, x, y, w, 13, Ui.InkQuiet);
        y += (lines - 1) * WrapPitch(13) + 40;

        Text(_gothic, new Vector2(x, y), "Ninja follow-up", fontSize: 17, modulate: Ui.Ink);
        lines = Wrap(NinjaHint, x, y + 19, w, 13, Ui.InkQuiet);
        ry = y + 19 + (lines - 1) * WrapPitch(13) + 14;
        for (int i = 0; i < PhoneNinjas.Length; i++)
            NinjaRadio(PhoneNinjas[i], x + i * (w / PhoneNinjas.Length), ry);
        Text(_gothic, new Vector2(x, ry + 50), "33% is the original's.", fontSize: 13, modulate: Ui.InkQuiet);
        y = ry + 50 + PortraitGap;
        SectionHead(x, y, "Scenes", w);
        return NewScenesBox(x, y + 38, w);
    }
}
