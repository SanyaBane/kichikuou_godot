using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// All Data on the phone held upright (docs/ui/phone-build.md, step P8c; docs/ui/phone.md, §22, PG2m and PG2): our page over
// the whole screen — the wave backdrop, the title band with the date, the map in miniature cut from the engine's picture
// (the presenter leaves the graph off it, GamePresenter.DataGraphCovered) at the room's width in a thin frame, a lacquer
// sheet under it; over the menu of graphs the four as finger rows, over a graph the four as tabs (the shown one lit), the
// script's title, the plot laid out again at the phone's type from the presenter's bars (DataBar: the palette's colours,
// the spending stacked from the foot, a bar's figure over it, "OV" inside a cut one), the caption, the scale as eight chips
// in a row at the thumb (the shown one lit); Back in the bar at the right thumb. Every press over the page is ours. Drawn in
// the frame's layout pixels over the whole screen (PhoneUnits), the places PhoneDataPortrait's.
internal sealed partial class AllDataView
{
    private static readonly Color OverInk = Colors.White, OverLine = new(0, 0, 0, 0.8f);

    /// <summary>The engine's picture (1280 × 800 layout pixels), whose map in miniature the view shows upright.</summary>
    public Texture2D? Screen { get; set; }

    /// <summary>Held upright over the map's view with no menu open (between the file's steps): the menu's page, deaf.</summary>
    public void ShowMap(Sides sides)
    {
        if (_answer is null)
            Show(Mode.Menu, sides);
    }

    /// <summary>A part's box held upright: a graph's row over the menu, its tab over a graph; a chip; Back.</summary>
    private Rect2 PortraitRectOf(PhoneUnits u, int part) => u.Rect(part < ChipBase
        ? _mode == Mode.Menu ? PhoneDataPortrait.MenuRow(u.Frame, part) : PhoneDataPortrait.Tab(u.Frame, part)
        : part < BackPart ? PhoneDataPortrait.Chip(u.Frame, part - ChipBase) : PhoneDataPortrait.Back(u.Frame));

    private void DrawPortrait(PhoneUnits u, Sides s)
    {
        var f = u.Frame;
        Ui.DrawWaveBackdrop(this, u.K, new Vector2(f.Screen.W, f.Screen.H) / u.K);
        PhonePickerParts.DrawPortraitTitle(this, u, _brush, _cinzel, "All Data", s.Date);
        var miniature = u.Rect(PhoneDataPortrait.Miniature(f));
        var (sx, sy, sw, sh) = PhoneDataPortrait.Source;
        if (Screen is { } screen)
            DrawTextureRectRegion(screen, miniature, new Rect2(sx, sy, sw, sh));
        DrawRect(miniature, Ui.DimGold, false, Lacquer.Line(u.Px(2), u.K));
        Ui.DrawPanel(this, u.Rect(PhoneDataPortrait.Sheet(f)), u.K);
        if (_mode == Mode.Menu)
        {
            for (int i = 0; i < PhoneDataPortrait.Graphs && i < s.Graphs.Count; i++)
                MenuRow(u, PortraitRectOf(u, i), s.Graphs[i], _pressed == i);
        }
        else
            DrawPortraitGraph(u, s);
        var back = PortraitRectOf(u, BackPart);
        bool held = _pressed == BackPart;
        Ui.DrawPanel(this, back, u.K, held ? Ui.RowHover : null);
        int backSize = SideCells.Fit(SideCells.Cinzel, BackWord, SideCells.GroupSize(u), back.Size.X - u.Px(30));   // the bar's cells' size
        Centred(SideCells.Cinzel, backSize, back.GetCenter(), BackWord, held ? Ui.Name : Ui.Text);
    }

    /// <summary>A graph's finger row over the menu: its name at the left in the game's font.</summary>
    private void MenuRow(PhoneUnits u, Rect2 box, string name, bool held)
    {
        DrawRect(box, held ? Ui.RowHover : Ui.Row);
        DrawRect(box, held ? Ui.HoverLine : Ui.DimGold, false, Lacquer.Line(u.Px(3), u.K));
        float x = box.Position.X + u.Px(PhoneDataPortrait.RowTextIn);
        int size = SideCells.Fit(_gothic, name, u.Size(PhoneDataPortrait.RowText), box.End.X - x - u.Px(16));
        DrawString(_gothic, new Vector2(x, PhonePickerParts.Middle(_gothic, size, box.GetCenter().Y)), name, fontSize: size,
            modulate: held ? Ui.Name : Ui.Text);
    }

    /// <summary>The tabs, the title, the plot with its bars and figures, the caption, SCALE and the chips.</summary>
    private void DrawPortraitGraph(PhoneUnits u, Sides s)
    {
        var f = u.Frame;
        int graphs = Math.Min(PhoneDataPortrait.Graphs, s.Graphs.Count), scales = Math.Min(PhoneDataPortrait.Scales, s.Scales.Count);
        // The tabs and the chips: Cinzel 700 at one size (docs/ui/style.md, "Buttons side by side"), shrunk together.
        int buttons = Math.Min(TabSize(u, PhoneDataPortrait.ButtonText, [.. Enumerable.Range(0, graphs).Select(i => (s.Graphs[i], PortraitRectOf(u, i)))], 20),
            SideCells.FitAll(_cinzel, u.Size(PhoneDataPortrait.ButtonText),
                [.. Enumerable.Range(0, scales).Select(i => (s.Scales[i], PortraitRectOf(u, ChipBase + i).Size.X - u.Px(16)))]));
        for (int i = 0; i < graphs; i++)
            Tab(u, PortraitRectOf(u, i), s.Graphs[i], i == s.Shown, _pressed == i, buttons);
        float middle = (f.Room.X + f.Room.W / 2) / u.K;
        int titleSize = SideCells.Fit(_gothic, s.Title, u.Size(PhoneDataPortrait.TitleText),
            (f.Room.W - f.Px(PhoneDataPortrait.TitleNarrower)) / u.K);
        Centred(_gothic, titleSize, new Vector2(middle, PhoneDataPortrait.TitleMiddle(f) / u.K), s.Title, Ui.Text);

        var plot = u.Rect(PhoneDataPortrait.Plot(f));
        float line = Lacquer.Line(u.Px(2), u.K);
        DrawRect(plot, Ink(s, GamePresenter.DataPlotColor));
        DrawRect(plot, Ink(s, GamePresenter.DataFrameColor), false, line);
        int figureSize = u.Size(PhoneDataPortrait.FigureText);
        float figureRoom = (PhoneDataPortrait.Slot(f) - 2 * f.Px(PhoneDataPortrait.FigureRoom)) / u.K;
        foreach (var bar in s.Bars)
        {
            var (x, w) = PhoneDataPortrait.Column(f, bar.X);
            Rect2 Span(int from, int height)
            {
                float top = PhoneDataPortrait.Level(f, from + height) / u.K, bottom = PhoneDataPortrait.Level(f, from) / u.K;
                return new Rect2(x / u.K, top, w / u.K, bottom - top);
            }
            if (bar.Height > 0)
            {
                DrawRect(Span(0, bar.Height), Ink(s, bar.Color));
                DrawRect(Span(0, bar.Height), Ink(s, GamePresenter.DataFrameColor), false, line);
            }
            foreach (var part in bar.Stack)                         // over the bar and its frame, as the file draws them
                if (part.Height > 0)
                    DrawRect(Span(part.Foot, part.Height), Ink(s, part.Color));
            var (fx, baseline) = PhoneDataPortrait.Figure(f, bar.X, bar.Height);
            int size = SideCells.Fit(_gothic, bar.Figure, figureSize, figureRoom);
            float fw = _gothic.GetStringSize(bar.Figure, fontSize: size).X;
            DrawString(_gothic, new Vector2(fx / u.K - fw / 2, baseline / u.K), bar.Figure, fontSize: size, modulate: FigureInk);
            if (bar.Over is { } over)
            {
                int overSize = SideCells.Fit(_gothic, over, u.Size(PhoneDataPortrait.OverText), w / u.K);
                var at = new Vector2(fx / u.K - _gothic.GetStringSize(over, fontSize: overSize).X / 2,
                    PhonePickerParts.Middle(_gothic, overSize, (PhoneDataPortrait.Level(f, bar.Height) + f.Px(PhoneDataPortrait.OverDown)) / u.K));
                DrawStringOutline(_gothic, at, over, fontSize: overSize, size: Math.Max(1, (int)u.Px(6)), modulate: OverLine);
                DrawString(_gothic, at, over, fontSize: overSize, modulate: OverInk);
            }
        }

        var (cx, cy) = PhoneDataPortrait.Caption(f);
        int captionSize = SideCells.Fit(_gothic, s.Caption, u.Size(PhoneDataPortrait.CaptionText), plot.Size.X);
        DrawString(_gothic, new Vector2(cx / u.K, PhonePickerParts.Middle(_gothic, captionSize, cy / u.K)), s.Caption,
            fontSize: captionSize, modulate: Ui.Quiet);
        var (hx, hy) = PhoneDataPortrait.ScaleHeading(f);
        Heading(u, "SCALE", hx, hy, PhoneDataPortrait.HeadingText);
        for (int i = 0; i < scales; i++)
            Chip(u, PortraitRectOf(u, ChipBase + i), s.Scales[i], i == s.Scale, _pressed == ChipBase + i, buttons);
    }

    /// <summary>A graph's tab: its name in two lines (split at the first space) at the group's <paramref name="size"/>, lit gold while shown.</summary>
    private void Tab(PhoneUnits u, Rect2 box, string name, bool lit, bool held, int size)
    {
        DrawRect(box, lit || held ? Ui.RowHover : Ui.Row);
        DrawRect(box, lit ? Ui.Gold : held ? Ui.HoverLine : Ui.DimGold, false, Lacquer.Line(u.Px(lit ? 3 : 2), u.K));
        string[] lines = TabLines(name);
        var middle = box.GetCenter();
        for (int i = 0; i < lines.Length; i++)
        {
            float dy = (i - (lines.Length - 1) / 2f) * 2 * u.Px(PhoneDataPortrait.TabLine);
            Centred(_cinzel, size, middle + new Vector2(0, dy), lines[i], lit ? Ui.Name : held ? Ui.Text : Ui.Quiet);
        }
    }

    private static Color Ink(Sides s, int entry) => s.Inks.TryGetValue(entry, out var ink) ? ink : Colors.Gray;
}
