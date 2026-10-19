using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The deployment table (owner's decision 25, docs/ui/deployment-table.md): our own screen over
/// <c>Sﾌﾞﾀｲ</c>'s unit grid — a row per listed unit (<see cref="DeploymentTable.Rows"/>), the
/// columns' compact headers with their full names as tooltips, sorted by a header click (the values
/// where more is better descending first, the rest ascending; again the other way; a small mark on the
/// header with its room kept; by number at first), scrolling both ways, the units deployed this week grey, the party's members marked with their
/// place in a first column. It decides nothing: the presses go to <see cref="GameScreen"/>, which
/// answers the script with the gestures it expects. Drawn as a ledger in our screens' lacquer (the owner's
/// mock-up G3, docs/ui/subordinates.md, and E1/E2, docs/ui/deployment-table.md): paper and ink, the headers in
/// Cinzel, the troops' bars; the party's places vermilion seals on gilt rows, a chooser's row a vermilion mark.
/// </summary>
internal sealed partial class DeploymentTableView : Tree
{
    /// <summary>The text size in script pixels (the grid's names are 12); scaled with the screen.</summary>
    private readonly float _textSize;

    /// <summary>The party column's header: a circle (both fonts have it), its full name the tooltip.</summary>
    private const string PartyMark = "●";


    private IReadOnlyList<DeploymentRow> _rows = [];
    private DeploymentColumn _sort = DeploymentColumn.Number;
    private bool _descending;
    private FontFile _font = null!;
    private string _up = "▲", _down = "▼";

    /// <summary>The sort marks to choose from (ascending, descending); MS Gothic has the small triangles, BIZ UDGothic not.</summary>
    private static readonly (string Up, string Down)[] SortMarks = [("▴", "▾"), ("˄", "˅"), ("↑", "↓"), ("▲", "▼")];
    private int _fontSize;

    /// <summary>A left press on a unit's row (the unit's number).</summary>
    public event Action<int>? RowPressed;

    /// <summary>A right press anywhere on the table.</summary>
    public event Action? RightPressed;

    /// <summary>The columns shown after the first (the deployment table's, or another screen's).</summary>
    private readonly IReadOnlyList<DeploymentColumnInfo> _columns;

    /// <summary>Whether the first column marks the chosen row (<see cref="Chosen"/>) instead of the party's places.</summary>
    private readonly bool _chooser;

    /// <summary>The headers' font: Cinzel over the text's (the text's font the fallback for the sort marks).</summary>
    private Font _titleFont = null!;

    /// <summary>The seals' numerals.</summary>
    private readonly FontVariation _sealFont = Lacquer.Cinzel(700);

    /// <summary>The headers' size: smaller than the text's (Cinzel's capitals are wide).</summary>
    private int TitleSize => Math.Max(8, (int)Math.Round(_fontSize * 0.82f));

    /// <summary>The chosen unit's row, marked and highlighted when the table is a chooser (our Subordinates screen).</summary>
    public int Chosen
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            Fill();
        }
    }

    /// <summary>The deployment screen's table: <see cref="DeploymentTable.Columns"/>, the party's places marked.</summary>
    public DeploymentTableView(GameFont font) : this(font, DeploymentTable.Columns, chooser: false, textSize: 8.3f)
    {
    }

    /// <summary>
    /// A table of <paramref name="columns"/>, its text <paramref name="textSize"/> script pixels; a
    /// <paramref name="chooser"/> marks its chosen row in the first column.
    /// </summary>
    public DeploymentTableView(GameFont font, IReadOnlyList<DeploymentColumnInfo> columns, bool chooser, float textSize)
    {
        _columns = columns;
        _chooser = chooser;
        _textSize = textSize;
        Columns = _columns.Count + 1;
        HideRoot = true;
        ColumnTitlesVisible = true;
        FocusMode = FocusModeEnum.None;
        SelectMode = SelectModeEnum.Row;
        Visible = false;
        SetColumnTitle(0, chooser ? "" : PartyMark);
        SetColumnTitleAlignment(0, HorizontalAlignment.Center);
        SetColumnTitleTooltipText(0, chooser ? "" : "Place in the party");
        for (int c = 0; c < _columns.Count; c++)
        {
            var info = _columns[c];
            SetColumnTitle(c + 1, info.Header);
            SetColumnTitleTooltipText(c + 1, info.FullName);
            SetColumnTitleAlignment(c + 1, Alignment(info));
        }
        for (int c = 0; c < Columns; c++)
        {
            SetColumnExpand(c, c == Columns - 1);                  // the last column to the paper's edge
            SetColumnClipContent(c, true);
        }
        AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        ApplyLedger();
        AddThemeConstantOverride("h_separation", 6);
        AddThemeConstantOverride("v_separation", 2);
        AddThemeConstantOverride("item_margin", 0);
        ColumnTitleClicked += (column, button) =>
        {
            if (button == (long)MouseButton.Left && column > 0)
                SortBy(_columns[(int)column - 1]);
        };
        SetFont(font);
    }

    /// <summary>The ledger's look: the paper, its header in vermilion ink over a rule, the rows' rules, a faint hover.</summary>
    private void ApplyLedger()
    {
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Lacquer.Paper, BorderColor = Lacquer.DimGold,
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        });
        StyleBoxFlat Head(Color colour) => new()
        {
            BgColor = colour, BorderColor = Lacquer.HeadInk, BorderWidthBottom = 1,
            ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 3, ContentMarginBottom = 3,
        };
        AddThemeStyleboxOverride("title_button_normal", Head(Lacquer.PaperHead));
        AddThemeStyleboxOverride("title_button_hover", Head(Lacquer.PaperHead.Lightened(0.12f)));
        AddThemeStyleboxOverride("title_button_pressed", Head(Lacquer.PaperHead));
        AddThemeColorOverride("font_color", Lacquer.Ink);
        AddThemeColorOverride("title_button_color", Lacquer.HeadInk);
        AddThemeColorOverride("guide_color", Lacquer.PaperRule);
        foreach (var state in new[] { "font_hovered_color", "font_selected_color", "font_hovered_selected_color" })
            AddThemeColorOverride(state, Lacquer.Ink);
        AddThemeStyleboxOverride("hovered", new StyleBoxFlat { BgColor = Lacquer.Vermilion with { A = 0.08f } });
        foreach (var state in new[] { "selected", "selected_focus", "hovered_selected", "hovered_selected_focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty());          // the chosen row is marked by Fill
        var bars = new Theme();
        foreach (string bar in new[] { "HScrollBar", "VScrollBar" })
        {
            bars.SetStylebox("scroll", bar, BarBox(Lacquer.PaperHead));
            bars.SetStylebox("grabber", bar, BarBox(Lacquer.InkQuiet));
            bars.SetStylebox("grabber_highlight", bar, BarBox(Lacquer.VermilionInk));
            bars.SetStylebox("grabber_pressed", bar, BarBox(Lacquer.VermilionInk));
        }
        Theme = bars;
    }

    /// <summary>A scroll bar's part: its margins make the bar as thick as the default theme's.</summary>
    private static StyleBoxFlat BarBox(Color color) => new()
    {
        BgColor = color, ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4,
    };

    /// <summary>Option → Fonts: the table's text in the chosen font (regular, unlike the surface's bold).</summary>
    public void SetFont(GameFont font)
    {
        _font = LoadFont(font);
        _titleFont = CinzelOver(_font);
        AddThemeFontOverride("font", _font);
        AddThemeFontOverride("title_button_font", _titleFont);
        // The sort mark: the narrowest pair the font has, so the header grows little.
        (_up, _down) = SortMarks
            .Where(m => _font.HasChar(m.Up[0]) && _font.HasChar(m.Down[0]))
            .MinBy(m => Math.Max(_font.GetStringSize(m.Up, fontSize: 16).X, _font.GetStringSize(m.Down, fontSize: 16).X));
        Fill();
    }

    /// <summary>Cinzel at 700 for the headers, <paramref name="text"/> its fallback (the sort marks).</summary>
    private static FontVariation CinzelOver(FontFile text)
    {
        var cinzel = Lacquer.Cinzel(700);
        cinzel.Fallbacks = [text];
        return cinzel;
    }

    /// <summary>The font of our own screens: the Option → Fonts face, regular (MS Gothic where installed, else BIZ UDGothic).</summary>
    public static FontFile LoadFont(GameFont font) => new()
    {
        Data = font == GameFont.MsGothic && GodotGlyphRenderer.MsGothicInstalled
            ? System.IO.File.ReadAllBytes(GodotGlyphRenderer.MsGothicPath)
            : Godot.FileAccess.GetFileAsBytes(GodotGlyphRenderer.BizUdGothicPath),
    };

    /// <summary>
    /// Covers <paramref name="rect"/> (in its parent's coordinates) with the text scaled by
    /// <paramref name="pixelsPerScript"/>, the screen's pixels per script pixel.
    /// </summary>
    public void Place(Rect2 rect, float pixelsPerScript)
    {
        Position = rect.Position;
        Size = rect.Size;
        int size = Math.Max(8, (int)Math.Round(_textSize * pixelsPerScript));
        if (size == _fontSize)
            return;
        _fontSize = size;
        AddThemeFontSizeOverride("font_size", size);
        AddThemeFontSizeOverride("title_button_font_size", TitleSize);
        AddThemeConstantOverride("v_separation", (int)Math.Round(1.5f * pixelsPerScript));   // the ledger's rows breathe
        Fill();
    }

    /// <summary>
    /// The rows from <paramref name="state"/>, filled in again when anything shown changed; the party
    /// places from <paramref name="order"/> while a drop's clicks run (the order it asked for).
    /// </summary>
    public void Refresh(GameState state, IReadOnlyList<int>? order = null)
    {
        var rows = DeploymentTable.Rows(state, order);
        if (rows.SequenceEqual(_rows))
            return;
        _rows = rows;
        Fill();
    }

    /// <summary>Whether <paramref name="unit"/> has a row.</summary>
    public bool Lists(int unit) => _rows.Any(r => r.Unit == unit);

    /// <summary>The rows not greyed: the units that can still act this week.</summary>
    public int ReadyCount => _rows.Count(r => !r.Grey);

    /// <summary>A press on <paramref name="unit"/>'s row as the mouse makes it (unattended play goes through here).</summary>
    public void Press(int unit)
    {
        if (!Lists(unit))
            throw new InvalidOperationException($"Unit {unit} has no row in the deployment table.");
        RowPressed?.Invoke(unit);
    }

    /// <summary>Whether the pointer is over the table.</summary>
    public bool HasPointer => Visible && GetGlobalRect().HasPoint(GetGlobalMousePosition());

    /// <summary>The unit whose row is under the pointer, or 0 (the header, the space below the rows, a scroll bar).</summary>
    public int HoveredUnit => HasPointer ? UnitAt(GetLocalMousePosition()) : 0;

    private int UnitAt(Vector2 local)
    {
        foreach (var child in GetChildren(includeInternal: true))
        {
            if (child is ScrollBar { Visible: true } bar && bar.GetRect().HasPoint(local))
                return 0;
        }
        return GetItemAtPosition(local) is { } item ? item.GetMetadata(0).AsInt32() : 0;
    }

    /// <summary>Left presses on rows and right presses anywhere are the game's; the rest (the header, the wheel, the scroll bars) the tree's own.</summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is not InputEventMouseButton { Pressed: true } button)
            return;
        if (button.ButtonIndex == MouseButton.Right)
        {
            RightPressed?.Invoke();
            AcceptEvent();
        }
        else if (button.ButtonIndex == MouseButton.Left && UnitAt(button.Position) is var unit and > 0)
        {
            RowPressed?.Invoke(unit);
            AcceptEvent();
        }
    }

    /// <summary>Text columns and the troops (the owner's choice) on the left, the numbers on the right.</summary>
    private static HorizontalAlignment Alignment(DeploymentColumnInfo info) =>
        info.IsText || info.Column == DeploymentColumn.Troops ? HorizontalAlignment.Left : HorizontalAlignment.Right;

    /// <summary>A header click: by that column, best first for a value where more is better (<see cref="DeploymentColumnInfo.HighFirst"/>), else ascending; again the other way.</summary>
    private void SortBy(DeploymentColumnInfo column)
    {
        _descending = column.Column == _sort ? !_descending : column.HighFirst;
        _sort = column.Column;
        Fill();
    }

    /// <summary>The rows in the sort's order, the sorted column's header marked, each column as wide as its widest text.</summary>
    private void Fill()
    {
        Clear();
        var root = CreateItem();
        var widths = new float[Columns];
        float Width(string text) => _font.GetStringSize(text, HorizontalAlignment.Left, -1, Math.Max(_fontSize, 8)).X;
        void Fit(int column, string text) => widths[column] = Math.Max(widths[column], Width(text));
        void FitTitle(int column, string text) =>
            widths[column] = Math.Max(widths[column], _titleFont.GetStringSize(text, HorizontalAlignment.Left, -1, TitleSize).X);
        Fit(0, PartyMark);
        for (int c = 0; c < _columns.Count; c++)
        {
            var info = _columns[c];
            string header = info.Header + (info.Column == _sort ? _descending ? _down : _up : "");
            SetColumnTitle(c + 1, header);
            FitTitle(c + 1, info.Header + _down);                      // room for the mark: a sort never widens a column
        }
        foreach (var row in DeploymentTable.Sort(_rows, _sort, _descending))
        {
            var item = CreateItem(root);
            item.SetMetadata(0, row.Unit);
            LedgerRow(item, row, chosen: _chooser && row.Unit == Chosen, place: _chooser ? 0 : row.PartyPlace);
            for (int c = 0; c < _columns.Count; c++)
                Fit(c + 1, row.Text(_columns[c].Column));
        }
        int separation = GetThemeConstant("h_separation");
        widths[0] = Math.Max(widths[0], _fontSize * 1.3f);                  // the chosen row's bar and diamond, a place's seal
        for (int c = 0; c < Columns; c++)
            SetColumnCustomMinimumWidth(c, (int)Math.Ceiling(widths[c]) + separation + 6);
    }

    /// <summary>
    /// A ledger's row: the chosen one tinted with a vermilion bar and diamond in the first column, a member of the
    /// party gilt with its place a vermilion seal there, the deployed ones in grey ink, the refill's price in
    /// vermilion, the troops with a bar of their strength under them.
    /// </summary>
    private void LedgerRow(TreeItem item, DeploymentRow row, bool chosen, int place)
    {
        for (int c = 0; c < _columns.Count; c++)
        {
            var info = _columns[c];
            item.SetText(c + 1, row.Text(info.Column));
            item.SetTextAlignment(c + 1, Alignment(info));
            if (row.Grey)
                item.SetCustomColor(c + 1, Lacquer.InkGrey);
            else if (info.Column == DeploymentColumn.Refill && row.Refill > 0)
                item.SetCustomColor(c + 1, Lacquer.VermilionInk);
            else if (info.IsText && info.Column != DeploymentColumn.Name)
                item.SetCustomColor(c + 1, Lacquer.InkQuiet);
            if (info.Column == DeploymentColumn.Troops && row.MaxTroops > 0)
            {
                item.SetCellMode(c + 1, TreeItem.TreeCellMode.Custom);
                var (text, troops, max, grey) = (row.Text(info.Column), row.Troops, row.MaxTroops, row.Grey);
                item.SetCustomDrawCallback(c + 1, Callable.From<TreeItem, Rect2>((_, rect) => DrawTroops(rect, text, troops, max, grey)));
            }
        }
        if (place > 0)
        {
            for (int c = 0; c < Columns; c++)
                item.SetCustomBgColor(c, Lacquer.PartyRow);
            item.SetCellMode(0, TreeItem.TreeCellMode.Custom);
            item.SetCustomDrawCallback(0, Callable.From<TreeItem, Rect2>((_, rect) =>
            {
                float size = Math.Min(rect.Size.Y - 2, _fontSize * 1.15f);
                Lacquer.DrawSeal(this, _sealFont, new Rect2(rect.GetCenter() - new Vector2(size, size) / 2, new Vector2(size, size)), place);
            }));
        }
        if (!chosen)
            return;
        for (int c = 0; c < Columns; c++)
            item.SetCustomBgColor(c, Lacquer.ChosenRow);
        item.SetCellMode(0, TreeItem.TreeCellMode.Custom);
        item.SetCustomDrawCallback(0, Callable.From<TreeItem, Rect2>((_, rect) =>
        {
            DrawRect(new Rect2(rect.Position.X, rect.Position.Y, Math.Max(2, rect.Size.Y * 0.14f), rect.Size.Y), Lacquer.Vermilion);
            Lacquer.DrawDiamond(this, rect.GetCenter() + new Vector2(rect.Size.Y * 0.1f, 0), rect.Size.Y * 0.2f, Lacquer.Vermilion);
        }));
    }

    /// <summary>
    /// The troops' figures (a custom cell draws no text of its own) a little raised, and their strength under them:
    /// green from three quarters, amber from a third, else red.
    /// </summary>
    private void DrawTroops(Rect2 cell, string text, int troops, int max, bool grey)
    {
        float h = Math.Max(2, cell.Size.Y * 0.11f);
        float baseline = cell.GetCenter().Y + (_font.GetAscent(_fontSize) - _font.GetDescent(_fontSize)) / 2 - h / 2;
        DrawString(_font, new Vector2(cell.Position.X + 2, baseline), text, HorizontalAlignment.Left, cell.Size.X - 2, _fontSize,
            grey ? Lacquer.InkGrey : Lacquer.Ink);
        float f = Math.Clamp(troops / (float)max, 0, 1);
        var colour = f >= 0.75f ? Lacquer.BarGood : f >= 0.35f ? Lacquer.BarLow : Lacquer.BarBad;
        if (grey)
            colour = colour with { A = 0.45f };
        var bar = new Rect2(cell.Position.X + 2, cell.End.Y - h - 1, cell.Size.X - 4, h);
        DrawRect(bar, Lacquer.PaperRule);
        DrawRect(bar with { Size = new Vector2(Math.Max(1, bar.Size.X * f), h) }, colour);
    }
}
