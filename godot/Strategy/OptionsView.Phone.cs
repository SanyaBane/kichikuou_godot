using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The options window on the phone held landscape (docs/ui/phone-build.md, step L8; docs/ui/phone.md, §16, LO1): the paper
// sheet across the whole width between the hole's strips over the wave backdrop, its five paper tabs on top as on the PC,
// the title at their right; each tab in one column across the sheet, scrolled between its top and Defaults | Close (owner,
// 2026-11-03; docs/ui/options.md, "Frame rate"; OptionsView.Scroll.cs) — Messages: the text speed (its preview the game's
// 121 at 2.7 mm) left and the message skip right in one row, the read text's colours in one row under them; Display: the
// screen's orientation (step P0) with its four ways across, the screen margins (docs/ui/screen-margins.md), the frame rate,
// then the PC's font, palette, thousands and unit tables' row heights; Battle: the effects, the timings one under another;
// Game: the folder; Tweaks: the PC's —, Defaults | Close at the right column's foot, Close at the right thumb. The settings
// are the PC's parts drawn at PO1's type (PhoneOptions.PartScale: the drawing's scale, _k), their text at the canvas's own
// size (Text) so it stays sharp; the places PhoneOptions'.
internal sealed partial class OptionsView
{
    private PhoneFrame? _phone;

    // Messages' left column on the phone, in the parts' units from its top: the speed's slider, the preview under it; the
    // note 17 under the preview, the read text's head 31 under the preview (upright), its tiles 30 under the head.
    private const float PhoneSpeedSlider = 48, PhonePreviewTop = 66, PhoneNoteUnder = 17, PhoneReadUnder = 31, PhoneTilesUnder = 30;

    /// <summary>A box of the canvas's units in the parts' units.</summary>
    private Rect2 Part(PhoneFrame.Box box) => new(box.X / _k, box.Y / _k, box.W / _k, box.H / _k);

    /// <summary>A length of <paramref name="reference"/> reference pixels in the parts' units.</summary>
    private static float P(float reference) => reference / PhoneOptions.PartScale;

    /// <summary>A font size of <paramref name="reference"/> reference pixels in the parts' units.</summary>
    private static int Ref(float reference) => Math.Max(1, (int)Math.Round(P(reference)));

    /// <summary>
    /// A string in the parts' units. On the phone its glyphs are drawn at the canvas's own size, not scaled up from the
    /// parts' size by the drawing's scale (they would blur); elsewhere it is <see cref="CanvasItem.DrawString"/> as it is.
    /// </summary>
    private void Text(Font font, Vector2 at, string text, HorizontalAlignment alignment = HorizontalAlignment.Left, float width = -1,
        int fontSize = 16, Color? modulate = null)
    {
        if (_phone is null)
        {
            DrawString(font, at, text, alignment, width, fontSize, modulate);
            return;
        }
        DrawSetTransform(Vector2.Zero);
        DrawString(font, at * _k, text, alignment, width < 0 ? -1 : width * _k, Math.Max(1, (int)Math.Round(fontSize * _k)), modulate);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
    }

    /// <summary>
    /// The preview on the phone: the game's 121 at the reference phone's own pixels — its text at PO1's 2.7 mm —, centred
    /// under the speed's slider in the column (upright) or in Messages' left column (held landscape), scrolled with it.
    /// </summary>
    private Rect2 PhonePreviewRect(PhoneFrame f)
    {
        var (x, top, w) = ColumnPlace();
        if (!f.IsPortrait)
        {
            var left = Part(PhoneOptions.Column(f, 0));
            (x, w) = (left.Position.X, left.Size.X);
        }
        var panel = _sample.Panel;
        float pw = P(panel.W), ph = P(panel.H);
        return new Rect2(x + (w - pw) / 2, top + PhonePreviewTop, pw, ph);
    }

    /// <summary>The wave backdrop over the whole screen, outside the column's clip.</summary>
    private void DrawPhoneBackdrop(PhoneFrame f)
    {
        DrawSetTransform(Vector2.Zero, 0, new Vector2(f.Scale, f.Scale));
        Ui.DrawWaveBackdrop(this, f.Scale, new Vector2(f.Screen.W, f.Screen.H) / f.Scale);
        DrawSetTransform(Vector2.Zero, 0, new Vector2(_k, _k));
    }

    private void DrawPhone(PhoneFrame f)
    {
        if (f.IsPortrait)
        {
            DrawPortrait(f);
            return;
        }
        BeginClip();
        DrawPhoneBackdrop(f);
        DrawPhoneTabs(f);
        int titleSize = Ref(PhoneOptions.TitleText);
        float titleWidth = _brush.GetStringSize("Options", fontSize: titleSize).X;
        Text(_brush, new Vector2(PhoneOptions.TitleRight(f) / _k - titleWidth, P(PhoneOptions.TitleTop) + _brush.GetAscent(titleSize)),
            "Options", fontSize: titleSize, modulate: Ui.Name);
        DrawColumn((x, top, w) => _tab switch
        {
            Tab.Messages => DrawMessagesPhone(f, top),
            Tab.Display => DrawDisplayPhone(x, top, w),
            Tab.Battle => DrawBattleColumn(x, top, w),
            Tab.Game => GameSection(x, top, w),
            _ => DrawTweaks(x, top, w),
        });
        DrawPhoneFoot(f);
        if (_question is not null)
            DrawQuestion(_question);
    }

    /// <summary>The paper tabs at the mock-up's places (held landscape's or upright's), the chosen one joined to the sheet with its vermilion edge.</summary>
    private void DrawPhoneTabs(PhoneFrame f)
    {
        bool portrait = f.IsPortrait;
        PhoneFrame.Box Place(int i, bool chosen) => portrait ? PhoneOptionsPortrait.Tab(f, i, chosen) : PhoneOptions.Tab(f, i, chosen);
        Rect2 current = default;
        for (int i = 0; i < Tabs.Length; i++)
        {
            var tab = Tabs[i];
            string name = tab.ToString();
            bool on = tab == _tab;
            var rect = Part(Place(i, on));
            DrawRect(rect, on ? Ui.Paper : _hover == "tab-" + name ? Field : Ui.PaperHead);
            DrawRect(rect, on ? Ui.PaperHead : Ui.InkGrey, false, Line(1));
            if (on)
                current = rect;
            var font = on ? _cinzelBold : _cinzel;
            int size = portrait
                ? SideCells.Fit(font, name, Ref(PhoneOptionsPortrait.TabText), rect.Size.X - P(PhoneOptionsPortrait.TabTextRoom))
                : SideCells.Fit(font, name, Ref(PhoneOptions.TabText), rect.Size.X - P(PhoneOptions.TabTextRoom));
            float middle = Part(Place(i, chosen: true)).GetCenter().Y + P(6);    // the words level on every tab
            Text(font, new Vector2(rect.Position.X, middle + (font.GetAscent(size) - font.GetDescent(size)) / 2), name,
                HorizontalAlignment.Center, rect.Size.X, size, on ? Ui.HeadInk : Ui.InkQuiet);
            _spots.Add(new Spot("tab-" + name, rect, () => ShowTab(tab)));
        }
        var sheet = Part(portrait ? PhoneOptionsPortrait.Sheet(f) : PhoneOptions.Sheet(f));
        DrawRect(sheet, Ui.Paper);
        DrawRect(sheet, Ui.PaperHead, false, Line(1));
        DrawRect(new Rect2(current.Position.X + 1, sheet.Position.Y - 4, current.Size.X - 2, 8), Ui.Paper);
        DrawRect(new Rect2(current.Position.X, current.Position.Y, current.Size.X,
            P(portrait ? PhoneOptionsPortrait.TabEdge : PhoneOptions.TabEdge)), Ui.Vermilion);
    }

    /// <summary>Defaults, a paper button (deaf on Game, whose folder has no default), and Close, red, at the right thumb.</summary>
    private void DrawPhoneFoot(PhoneFrame f)
    {
        var defaults = Part(PhoneOptions.Defaults(f));
        bool enabled = _tab != Tab.Game, lit = enabled && _hover == "defaults";
        DrawRect(defaults, lit ? Field : Ui.PaperHead);
        DrawRect(defaults, Ui.InkQuiet, false, Line(1));
        int size = Ref(PhoneOptions.DefaultsText);
        Text(_cinzelBold, new Vector2(defaults.Position.X, Lacquer.Baseline(defaults, _cinzelBold, size)), "Defaults",
            HorizontalAlignment.Center, defaults.Size.X, size, enabled ? Ui.HeadInk : Ui.InkGrey);
        if (enabled)
            _spots.Add(new Spot("defaults", defaults, AskDefaults));
        var close = Part(PhoneOptions.Close(f));
        bool closeLit = _hover == "close";
        Ui.DrawRedButton(this, close, closeLit, closeLit && _pressed == "close", _k);
        int closeSize = SideCells.Fit(_brush, "Close", Ref(PhoneOptions.CloseText), close.Size.X - P(30));
        Text(_brush, new Vector2(close.Position.X, Lacquer.Baseline(close, _brush, closeSize)), "Close", HorizontalAlignment.Center,
            close.Size.X, closeSize, Ui.Name);
        _spots.Add(new Spot("close", close, Close));
    }

    // ---------------------------------------------------------------- the tabs' columns (the parts' units; each returns its last line)

    /// <summary>
    /// Messages (lo1, <c>messages_columns</c>; the owner, 2026-11-03): the text speed and its preview at the left and the
    /// message skip at the right in one row, the rule between them; under them across the column the read text, its tiles
    /// ("Don't change" and the nine colours) in one row at their two-row width.
    /// </summary>
    private float DrawMessagesPhone(PhoneFrame f, float top)
    {
        var (cx, _, cw) = ColumnPlace();
        Rect2 left = Part(PhoneOptions.Column(f, 0)), right = Part(PhoneOptions.Column(f, 1));
        float x = left.Position.X, w = left.Size.X;
        SectionHead(x, top, "Text speed", w);
        SpeedSlider(x, top + PhoneSpeedSlider, w);
        float skip = MessageSkipColumn(right.Position.X, top, right.Size.X);
        float note = PreviewRect.End.Y + PhoneNoteUnder;
        Text(_gothic, new Vector2(x, note), ReadTextChoice.Index == ReadTextChoice.None ? "Read text keeps the window's colour."
            : "The preview counts as read.", fontSize: 13, modulate: Ui.InkQuiet);
        float row = Math.Max(note, skip);
        var (ruleX, _, _) = PhoneOptions.Divider(f);
        DrawLine(new Vector2(ruleX / _k, top + P(4)), new Vector2(ruleX / _k, row + 6), Ui.PaperRule, Line(1));
        return ReadTextSection(cx, row + 32, cw, ReadTileWidth(w));
    }

    /// <summary>The read text's colour tiles' width in a column <paramref name="w"/> wide: four in the first row, five in each after it.</summary>
    private static float ReadTileWidth(float w) => (w - P(320) - 4 * P(14)) / 5;

    /// <summary>
    /// The read text's head at <paramref name="y"/>, its tiles ("Don't change" and the colours: in a column
    /// <paramref name="w"/> wide four colours in the first row and five in each after it; with <paramref name="tileWidth"/>
    /// all in one row, that wide), the chosen one's name and its words wrapped into <paramref name="w"/>; returns their last
    /// baseline.
    /// </summary>
    private float ReadTextSection(float x, float y, float w, float tileWidth = 0)
    {
        int chosen = ReadTextChoice.Index, count = ReadTextChoice.Colours.Length;
        SectionHead(x, y, "Already-read text", w);
        // read_text: "Don't change" 300 wide, the colours from 320 in, 14 apart, 100 high, the second row 16 under the first.
        float tileH = P(100), gap = P(14), first = P(300), after = P(320), rowGap = P(16);
        float sw = tileWidth > 0 ? tileWidth : ReadTileWidth(w), top = y + PhoneTilesUnder;
        int firstRow = tileWidth > 0 ? count : 4;
        ReadTile("read-none", new Rect2(x, top, first, tileH), chosen == ReadTextChoice.None, null, ReadTextChoice.None);
        for (int i = 0; i < count; i++)
        {
            int row = i < firstRow ? 0 : 1 + (i - firstRow) / 5, place = i < firstRow ? i : (i - firstRow) % 5;
            float tx = (row == 0 ? x + after : x) + place * (sw + gap);
            ReadTile("read-" + i, new Rect2(tx, top + row * (tileH + rowGap), sw, tileH), chosen == i, ReadTextChoice.Colours[i].Colour, i);
        }
        int rows = firstRow >= count ? 1 : 1 + (count - firstRow + 4) / 5;
        y = top + rows * tileH + (rows - 1) * rowGap + 23;
        Text(_gothic, new Vector2(x, y), chosen == ReadTextChoice.None ? "Don't change" : ReadTextChoice.Colours[chosen].Name,
            fontSize: 17, modulate: Ui.Ink);
        int lines = Wrap(string.Join(" ", ReadTextLines), x, y + 20, w, 13, Ui.InkQuiet);
        return y + 20 + (lines - 1) * WrapPitch(13);
    }

    /// <summary>
    /// Display (the upright's order across the sheet): the screen's orientation, its four ways across; the screen margins, a
    /// field across with the way the phone is held first; the frame rate; then the PC's font, palette, thousands and unit
    /// tables' row heights.
    /// </summary>
    private float DrawDisplayPhone(float x, float y, float w)
    {
        OrientationSection(x, y, w);
        y += 118;
        SectionHead(x, y, "Screen margins", w);
        float field = P(PhoneOptionsPortrait.OrientationRow);
        MarginsField(new Rect2(x, y + 36, w, field));
        y += 36 + field + 40;
        return DisplaySections(x, y, w);
    }

    /// <summary>
    /// The phone's screen orientation (the owner's PO1o, <see cref="OrientationChoice"/>), a phone's only: its four ways as
    /// radios across from 38 under the head, the chosen way's line under them.
    /// </summary>
    private void OrientationSection(float x, float y, float w)
    {
        SectionHead(x, y, "Screen orientation", w);
        var all = OrientationChoice.All;
        for (int i = 0; i < all.Length; i++)
        {
            var way = all[i].Way;
            Radio("orientation-" + way, x + i * (w / all.Length), y + 38, OrientationChoice.Chosen == way, all[i].Name, _gothic,
                () => OrientationChoice.Chosen = way);
        }
        string hint = Array.Find(all, o => o.Way == OrientationChoice.Chosen).Hint;
        Text(_gothic, new Vector2(x, y + 76), hint, fontSize: SideCells.Fit(_gothic, hint, 13, w), modulate: Ui.InkQuiet);
    }
}
