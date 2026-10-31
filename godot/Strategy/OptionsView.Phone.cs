using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The options window on the phone held landscape (docs/ui/phone-build.md, step L8; docs/ui/phone.md, §16, LO1): the paper
// sheet across the whole width between the hole's strips over the wave backdrop, its five paper tabs on top as on the PC,
// the title at their right; each tab in two columns — Messages: the text speed (its preview the game's 121 at 2.7 mm) and
// the read text's colours left, the message skip right; Display: the font left, the palette and the screen's orientation
// (step P0) right, the thousands and the unit tables' row heights across (their tiles need the width), the screen margins
// at the left column's foot beside Defaults | Close (docs/ui/screen-margins.md); Battle: the effects
// across, the timings in the two
// columns; Game: the folder across; Tweaks: the line on what they are across, Battle left, Scenes right —, Defaults |
// Close at the right column's foot, Close at the right thumb. The settings are the PC's parts drawn at PO1's type
// (PhoneOptions.PartScale: the drawing's scale, _k), their text at the canvas's own size (Text) so it stays sharp; the
// places PhoneOptions'.
internal sealed partial class OptionsView
{
    private PhoneFrame? _phone;
    private readonly Layer _backdrop;

    // Messages' left column on the phone, in the parts' units from its top: the speed's slider, the preview under it; the
    // note 17 under the preview, the read text's head 31 under the preview, its tiles 30 under the head.
    private const float PhoneSpeedSlider = 48, PhonePreviewTop = 66, PhoneNoteUnder = 17, PhoneReadUnder = 31, PhoneTilesUnder = 30;

    // Display's right column on the phone, in the parts' units: the orientation this far in, right of the palette's two swatches.
    private const float PhoneOrientationIn = 200;

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
    /// The preview on the phone: the game's 121 at the reference phone's own pixels — its text at PO1's 2.7 mm —, centred in
    /// the left column under the speed's slider.
    /// </summary>
    private Rect2 PhonePreviewRect(PhoneFrame f)
    {
        var column = f.IsUpright ? UprightColumn(f) : Part(PhoneOptions.Column(f, 0));
        var panel = _sample.Panel;
        float w = P(panel.W), h = P(panel.H);
        return new Rect2(column.Position.X + (column.Size.X - w) / 2, column.Position.Y + PhonePreviewTop, w, h);
    }

    private void DrawPhone(PhoneFrame f)
    {
        if (f.IsUpright)
        {
            DrawUpright(f);
            return;
        }
        DrawPhoneTabs(f);
        int titleSize = Ref(PhoneOptions.TitleText);
        float titleWidth = _brush.GetStringSize("Options", fontSize: titleSize).X;
        Text(_brush, new Vector2(PhoneOptions.TitleRight(f) / _k - titleWidth, P(PhoneOptions.TitleTop) + _brush.GetAscent(titleSize)),
            "Options", fontSize: titleSize, modulate: Ui.Name);
        Rect2 left = Part(PhoneOptions.Column(f, 0)), right = Part(PhoneOptions.Column(f, 1)), across = Part(PhoneOptions.Across(f));
        switch (_tab)
        {
            case Tab.Messages:
                DrawMessagesPhone(f, left, right);
                break;
            case Tab.Display:
                DrawDisplayPhone(f, left, right, across);
                break;
            case Tab.Battle:
                DrawBattlePhone(f, left, right, across);
                break;
            case Tab.Game:
                GameSection(across.Position.X, across.Position.Y, across.Size.X);
                break;
            case Tab.Tweaks:
                DrawTweaksPhone(f, left, right, across);
                break;
        }
        DrawPhoneFoot(f);
        if (_question is not null)
            DrawQuestion(_question);
    }

    /// <summary>The paper tabs at the mock-up's places (held landscape's or upright's), the chosen one joined to the sheet with its vermilion edge.</summary>
    private void DrawPhoneTabs(PhoneFrame f)
    {
        bool upright = f.IsUpright;
        PhoneFrame.Box Place(int i, bool chosen) => upright ? PhoneOptionsUpright.Tab(f, i, chosen) : PhoneOptions.Tab(f, i, chosen);
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
            int size = upright
                ? SideCells.Fit(font, name, Ref(PhoneOptionsUpright.TabText), rect.Size.X - P(PhoneOptionsUpright.TabTextRoom))
                : SideCells.Fit(font, name, Ref(PhoneOptions.TabText), rect.Size.X - P(PhoneOptions.TabTextRoom));
            float middle = Part(Place(i, chosen: true)).GetCenter().Y + P(6);    // the words level on every tab
            Text(font, new Vector2(rect.Position.X, middle + (font.GetAscent(size) - font.GetDescent(size)) / 2), name,
                HorizontalAlignment.Center, rect.Size.X, size, on ? Ui.HeadInk : Ui.InkQuiet);
            _spots.Add(new Spot("tab-" + name, rect, () => ShowTab(tab)));
        }
        var sheet = Part(upright ? PhoneOptionsUpright.Sheet(f) : PhoneOptions.Sheet(f));
        DrawRect(sheet, Ui.Paper);
        DrawRect(sheet, Ui.PaperHead, false, Line(1));
        DrawRect(new Rect2(current.Position.X + 1, sheet.Position.Y - 4, current.Size.X - 2, 8), Ui.Paper);
        DrawRect(new Rect2(current.Position.X, current.Position.Y, current.Size.X,
            P(upright ? PhoneOptionsUpright.TabEdge : PhoneOptions.TabEdge)), Ui.Vermilion);
    }

    /// <summary>The rule between the columns from <paramref name="top"/> (the parts' units) to its foot, at most <paramref name="bottom"/>.</summary>
    private void DrawColumnRule(PhoneFrame f, float top, float bottom = float.MaxValue)
    {
        var (x, _, foot) = PhoneOptions.Divider(f);
        DrawLine(new Vector2(x / _k, top), new Vector2(x / _k, Math.Min(foot / _k, bottom)), Ui.PaperRule, Line(1));
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
            _spots.Add(new Spot("defaults", defaults, Defaults));
        var close = Part(PhoneOptions.Close(f));
        bool closeLit = _hover == "close";
        Ui.DrawRedButton(this, close, closeLit, closeLit && _pressed == "close", _k);
        int closeSize = SideCells.Fit(_brush, "Close", Ref(PhoneOptions.CloseText), close.Size.X - P(30));
        Text(_brush, new Vector2(close.Position.X, Lacquer.Baseline(close, _brush, closeSize)), "Close", HorizontalAlignment.Center,
            close.Size.X, closeSize, Ui.Name);
        _spots.Add(new Spot("close", close, Close));
    }

    /// <summary>
    /// Messages (lo1, <c>messages_columns</c>): the text speed, its preview and the read text's tiles in two rows ("Don't
    /// change" and four colours, then five) at the left, its words wrapped into the column; the message skip at the right.
    /// </summary>
    private void DrawMessagesPhone(PhoneFrame f, Rect2 left, Rect2 right)
    {
        float x = left.Position.X, y = left.Position.Y, w = left.Size.X;
        SectionHead(x, y, "Text speed", w);
        SpeedSlider(x, y + PhoneSpeedSlider, w);
        MessageSkipColumn(right.Position.X, right.Position.Y, right.Size.X);
        DrawColumnRule(f, Part(PhoneOptions.Sheet(f)).Position.Y + P(40));

        int chosen = ReadTextChoice.Index;
        y = PreviewRect.End.Y + PhoneNoteUnder;
        Text(_gothic, new Vector2(x, y), chosen == ReadTextChoice.None ? "Read text keeps the window's colour." : "The preview counts as read.",
            fontSize: 13, modulate: Ui.InkQuiet);
        ReadTextSection(x, PreviewRect.End.Y + PhoneReadUnder, w);
    }

    /// <summary>
    /// The read text's head at <paramref name="y"/>, its tiles in two rows ("Don't change" and four colours, then five), the
    /// chosen one's name and its words wrapped into <paramref name="w"/>; returns their last baseline.
    /// </summary>
    private float ReadTextSection(float x, float y, float w)
    {
        int chosen = ReadTextChoice.Index;
        SectionHead(x, y, "Already-read text", w);
        // read_text: "Don't change" 300 wide, the colours from 320 in, 14 apart, 100 high, the second row 16 under the first.
        float tileH = P(100), gap = P(14), first = P(300), after = P(320), rowGap = P(16);
        float sw = (w - after - 4 * gap) / 5, top = y + PhoneTilesUnder;
        ReadTile("read-none", new Rect2(x, top, first, tileH), chosen == ReadTextChoice.None, null, ReadTextChoice.None);
        for (int i = 0; i < ReadTextChoice.Colours.Length; i++)
        {
            int row = i < 4 ? 0 : 1 + (i - 4) / 5, place = i < 4 ? i : (i - 4) % 5;
            float tx = (row == 0 ? x + after : x) + place * (sw + gap);
            ReadTile("read-" + i, new Rect2(tx, top + row * (tileH + rowGap), sw, tileH), chosen == i, ReadTextChoice.Colours[i].Colour, i);
        }
        int rows = 1 + ReadTextChoice.Colours.Length / 5;          // four colours in the first row, five in each after it
        y = top + rows * tileH + (rows - 1) * rowGap + 23;
        Text(_gothic, new Vector2(x, y), chosen == ReadTextChoice.None ? "Don't change" : ReadTextChoice.Colours[chosen].Name,
            fontSize: 17, modulate: Ui.Ink);
        int lines = Wrap(string.Join(" ", ReadTextLines), x, y + 20, w, 13, Ui.InkQuiet);
        return y + 20 + (lines - 1) * WrapPitch(13);
    }

    /// <summary>
    /// Display: the font left, the palette right and the screen's orientation right of it (step P0, the owner's PO1o, two
    /// ways a row); the thousands and the unit tables' row heights across.
    /// </summary>
    private void DrawDisplayPhone(PhoneFrame f, Rect2 left, Rect2 right, Rect2 across)
    {
        float y = left.Position.Y;
        FontSection(left.Position.X, y, left.Size.X);
        PaletteSection(right.Position.X, y, PhoneOrientationIn - 30);
        OrientationSection(right.Position.X + PhoneOrientationIn, y, right.Size.X - PhoneOrientationIn);
        DrawColumnRule(f, Part(PhoneOptions.Sheet(f)).Position.Y + P(40), y + 106);
        ThousandsSection(across.Position.X, y + 116, across.Size.X);
        RowsSection(across.Position.X, y + 196, across.Size.X);
        var defaults = Part(PhoneOptions.Defaults(f));          // the screen margins at the left column's foot, beside Defaults | Close
        MarginsField(new Rect2(left.Position.X, defaults.Position.Y, left.Size.X, defaults.Size.Y), titled: true);
    }

    /// <summary>Battle: the effects across, the timings in the two columns (the left one's longer: the right one ends over Defaults).</summary>
    private void DrawBattlePhone(PhoneFrame f, Rect2 left, Rect2 right, Rect2 across)
    {
        float y = across.Position.Y;
        EffectsSection(across.Position.X, y, across.Size.X);
        y += 96;
        SectionHead(across.Position.X, y, "Timings", across.Size.X);
        y += 36;
        DrawColumnRule(f, y - 6);
        int i = 0;
        foreach (var setting in Timings)
        {
            var column = i % 2 == 0 ? left : right;
            TimingRow(setting, column.Position.X, y + i / 2 * 46, column.Size.X);
            i++;
        }
    }

    /// <summary>
    /// Tweaks: what they are across; Battle across both columns — at the left the damage forecast and "Chaos and Nikkou
    /// reach demons" with its three choices one under another (a row has no room for them side by side), at the right the
    /// ninja follow-up's chances in a row in short words, in thirds of the column (the owner's NP) —, Scenes at the right
    /// under it; the hints wrapped into their column.
    /// </summary>
    private void DrawTweaksPhone(PhoneFrame f, Rect2 left, Rect2 right, Rect2 across)
    {
        float y = across.Position.Y;
        Wrap(TweaksIntro, across.Position.X, y + 14, across.Size.X, 14, Ui.InkQuiet);
        y += 44;
        SectionHead(across.Position.X, y, "Battle", across.Size.X);
        DrawColumnRule(f, y + 30);
        float x = left.Position.X, w = left.Size.X;
        float foot = ForecastBox(x, y + 38, w);
        float tx = x + 32, tw = w - 32;
        Text(_gothic, new Vector2(tx, foot + 24), "Chaos and Nikkou reach demons", fontSize: 17, modulate: Ui.Ink);
        int lines = Wrap(ReachHint, tx, foot + 43, tw, 13, Ui.InkQuiet);
        float ry = foot + 43 + (lines - 1) * WrapPitch(13) + 14;
        for (int i = 0; i < Reaches.Length; i++)
            ReachRadio(i, tx, ry + i * 36);
        Wrap(ReachNote, tx, ry + Reaches.Length * 36 + 14, tw, 13, Ui.InkQuiet);

        float nx = right.Position.X + 32, nw = right.Size.X - 32;
        Text(_gothic, new Vector2(nx, y + 54), "Ninja follow-up", fontSize: 17, modulate: Ui.Ink);
        lines = Wrap(NinjaHint, nx, y + 73, nw, 13, Ui.InkQuiet);
        ry = y + 73 + (lines - 1) * WrapPitch(13) + 14;
        for (int i = 0; i < PhoneNinjas.Length; i++)
            NinjaRadio(PhoneNinjas[i], nx + i * (nw / PhoneNinjas.Length), ry);
        Text(_gothic, new Vector2(nx, ry + 50), "33% is the original's.", fontSize: 13, modulate: Ui.InkQuiet);
        float scenes = ry + 50 + 32;
        SectionHead(right.Position.X, scenes, "Scenes", right.Size.X);
        NewScenesBox(right.Position.X, scenes + 38, right.Size.X);
    }

    /// <summary>A layer of the window drawn by its view (the phone's wave backdrop behind it).</summary>
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
