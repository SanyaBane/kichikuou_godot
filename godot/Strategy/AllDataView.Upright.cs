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
// the frame's layout pixels over the whole screen (PhoneUnits), the places PhoneDataUpright's.
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
    private Rect2 UprightRectOf(PhoneUnits u, int part) => u.Rect(part < ChipBase
        ? _mode == Mode.Menu ? PhoneDataUpright.MenuRow(u.Frame, part) : PhoneDataUpright.Tab(u.Frame, part)
        : part < BackPart ? PhoneDataUpright.Chip(u.Frame, part - ChipBase) : PhoneDataUpright.Back(u.Frame));

    private void DrawUpright(PhoneUnits u, Sides s)
    {
        var f = u.Frame;
        Ui.DrawWaveBackdrop(this, u.K, new Vector2(f.Screen.W, f.Screen.H) / u.K);
        PhonePickerParts.DrawUprightTitle(this, u, _brush, _cinzel, "All Data", s.Date);
        var miniature = u.Rect(PhoneDataUpright.Miniature(f));
        var (sx, sy, sw, sh) = PhoneDataUpright.Source;
        if (Screen is { } screen)
            DrawTextureRectRegion(screen, miniature, new Rect2(sx, sy, sw, sh));
        DrawRect(miniature, Ui.DimGold, false, Lacquer.Line(u.Px(2), u.K));
        Ui.DrawPanel(this, u.Rect(PhoneDataUpright.Sheet(f)), u.K);
        if (_mode == Mode.Menu)
        {
            for (int i = 0; i < PhoneDataUpright.Graphs && i < s.Graphs.Count; i++)
                MenuRow(u, UprightRectOf(u, i), s.Graphs[i], _pressed == i);
        }
        else
            DrawUprightGraph(u, s);
        var back = UprightRectOf(u, BackPart);
        bool held = _pressed == BackPart;
        Ui.DrawPanel(this, back, u.K, held ? Ui.RowHover : null);
        int backSize = SideCells.Fit(_brush, BackWord, u.Size(PhoneDataUpright.BackText), back.Size.X - u.Px(30));
        Centred(_brush, backSize, back.GetCenter(), BackWord, held ? Ui.Name : Ui.Text);
    }

    /// <summary>A graph's finger row over the menu: its name at the left in the game's font.</summary>
    private void MenuRow(PhoneUnits u, Rect2 box, string name, bool held)
    {
        DrawRect(box, held ? Ui.RowHover : Ui.Row);
        DrawRect(box, held ? Ui.HoverLine : Ui.DimGold, false, Lacquer.Line(u.Px(3), u.K));
        float x = box.Position.X + u.Px(PhoneDataUpright.RowTextIn);
        int size = SideCells.Fit(_gothic, name, u.Size(PhoneDataUpright.RowText), box.End.X - x - u.Px(16));
        DrawString(_gothic, new Vector2(x, PhonePickerParts.Middle(_gothic, size, box.GetCenter().Y)), name, fontSize: size,
            modulate: held ? Ui.Name : Ui.Text);
    }

    /// <summary>The tabs, the title, the plot with its bars and figures, the caption, SCALE and the chips.</summary>
    private void DrawUprightGraph(PhoneUnits u, Sides s)
    {
        var f = u.Frame;
        for (int i = 0; i < PhoneDataUpright.Graphs && i < s.Graphs.Count; i++)
            Tab(u, UprightRectOf(u, i), s.Graphs[i], i == s.Shown, _pressed == i);
        float middle = (f.Room.X + f.Room.W / 2) / u.K;
        int titleSize = SideCells.Fit(_gothic, s.Title, u.Size(PhoneDataUpright.TitleText),
            (f.Room.W - f.Px(PhoneDataUpright.TitleNarrower)) / u.K);
        Centred(_gothic, titleSize, new Vector2(middle, PhoneDataUpright.TitleMiddle(f) / u.K), s.Title, Ui.Text);

        var plot = u.Rect(PhoneDataUpright.Plot(f));
        float line = Lacquer.Line(u.Px(2), u.K);
        DrawRect(plot, Ink(s, GamePresenter.DataPlotColor));
        DrawRect(plot, Ink(s, GamePresenter.DataFrameColor), false, line);
        int figureSize = u.Size(PhoneDataUpright.FigureText);
        float figureRoom = (PhoneDataUpright.Slot(f) - 2 * f.Px(PhoneDataUpright.FigureRoom)) / u.K;
        foreach (var bar in s.Bars)
        {
            var (x, w) = PhoneDataUpright.Column(f, bar.X);
            Rect2 Span(int from, int height)
            {
                float top = PhoneDataUpright.Level(f, from + height) / u.K, bottom = PhoneDataUpright.Level(f, from) / u.K;
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
            var (fx, baseline) = PhoneDataUpright.Figure(f, bar.X, bar.Height);
            int size = SideCells.Fit(_gothic, bar.Figure, figureSize, figureRoom);
            float fw = _gothic.GetStringSize(bar.Figure, fontSize: size).X;
            DrawString(_gothic, new Vector2(fx / u.K - fw / 2, baseline / u.K), bar.Figure, fontSize: size, modulate: FigureInk);
            if (bar.Over is { } over)
            {
                int overSize = SideCells.Fit(_gothic, over, u.Size(PhoneDataUpright.OverText), w / u.K);
                var at = new Vector2(fx / u.K - _gothic.GetStringSize(over, fontSize: overSize).X / 2,
                    PhonePickerParts.Middle(_gothic, overSize, (PhoneDataUpright.Level(f, bar.Height) + f.Px(PhoneDataUpright.OverDown)) / u.K));
                DrawStringOutline(_gothic, at, over, fontSize: overSize, size: Math.Max(1, (int)u.Px(6)), modulate: OverLine);
                DrawString(_gothic, at, over, fontSize: overSize, modulate: OverInk);
            }
        }

        var (cx, cy) = PhoneDataUpright.Caption(f);
        int captionSize = SideCells.Fit(_gothic, s.Caption, u.Size(PhoneDataUpright.CaptionText), plot.Size.X);
        DrawString(_gothic, new Vector2(cx / u.K, PhonePickerParts.Middle(_gothic, captionSize, cy / u.K)), s.Caption,
            fontSize: captionSize, modulate: Ui.Quiet);
        var (hx, hy) = PhoneDataUpright.ScaleHeading(f);
        Heading(u, "SCALE", hx, hy, PhoneDataUpright.HeadingText);
        for (int i = 0; i < PhoneDataUpright.Scales && i < s.Scales.Count; i++)
            Chip(u, UprightRectOf(u, ChipBase + i), s.Scales[i], i == s.Scale, _pressed == ChipBase + i, PhoneDataUpright.ChipText);
    }

    /// <summary>A graph's tab: its name in two lines (split at the first space), lit gold while shown.</summary>
    private void Tab(PhoneUnits u, Rect2 box, string name, bool lit, bool held)
    {
        DrawRect(box, lit || held ? Ui.RowHover : Ui.Row);
        DrawRect(box, lit ? Ui.Gold : held ? Ui.HoverLine : Ui.DimGold, false, Lacquer.Line(u.Px(lit ? 3 : 2), u.K));
        int space = name.IndexOf(' ');
        string[] lines = space > 0 ? [name[..space], name[(space + 1)..]] : [name];
        int size = u.Size(PhoneDataUpright.TabText);
        foreach (var text in lines)
            size = SideCells.Fit(_gothic, text, size, box.Size.X - u.Px(20));
        var middle = box.GetCenter();
        for (int i = 0; i < lines.Length; i++)
        {
            float dy = (i - (lines.Length - 1) / 2f) * 2 * u.Px(PhoneDataUpright.TabLine);
            Centred(_gothic, size, middle + new Vector2(0, dy), lines[i], lit ? Ui.Name : held ? Ui.Text : Ui.Quiet);
        }
    }

    private static Color Ink(Sides s, int entry) => s.Inks.TryGetValue(entry, out var ink) ? ink : Colors.Gray;
}
