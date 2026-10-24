using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The deployment table (owner's decision 25, docs/ui/deployment-table.md): our own screen over
/// <c>Sﾌﾞﾀｲ</c>'s unit grid — a row per listed unit (<see cref="DeploymentTable.Rows"/>), the
/// columns' compact headers with their full names as tooltips (the touch mode's on a long press, a <see cref="HeaderTip"/>), sorted by a header click (the values
/// where more is better descending first, the rest ascending; again the other way; a small mark on the
/// header with its room kept; by number at first), scrolling both ways, the units deployed this week grey, the party's members marked with their
/// place in a first column. It decides nothing: the presses go to <see cref="GameScreen"/>, which
/// answers the script with the gestures it expects. Drawn as a ledger in our screens' lacquer (the owner's
/// mock-up G3, docs/ui/subordinates.md, and E1/E2, docs/ui/deployment-table.md): paper and ink, the headers in
/// Cinzel, the troops' bars; the party's places vermilion seals on gilt rows, a chooser's row a vermilion mark. A chooser's
/// first column also holds each unit's talk mark (owner's decision 30, docs/ui/talk-marks.md §6, S): a speech bubble left
/// of No., filled green with "!" (new), vermilion with "?" (a draw decides), an empty outline (read).
/// </summary>
internal sealed partial class DeploymentTableView : Tree, IUiPaletteUser
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    /// <summary>The text size in script pixels (the grid's names are 12); scaled with the screen.</summary>
    private readonly float _textSize;

    /// <summary>The party column's header: a circle (both fonts have it), its full name the tooltip.</summary>
    private const string PartyMark = "●";


    private IReadOnlyList<DeploymentRow> _rows = [];
    private ThousandsSeparator _separator = Figures.Separator;      // the rows' figures were written with it
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

    /// <summary>A left press on a unit's Refill button (the unit's number; owner's decision 27).</summary>
    public event Action<int>? RefillPressed;

    /// <summary>Whether a Refill cell the unit can be replenished from is a button (the deployment screen's, not Subordinates').</summary>
    private readonly bool _refillButtons;

    /// <summary>The unit whose Refill button is under the pointer (0: none).</summary>
    private int _refillHovered;

    /// <summary>The unit whose Replenish the screen asks about: its button stays lit (0: none).</summary>
    public int AskedRefill
    {
        get;
        set
        {
            if (field == value)
                return;
            field = value;
            QueueRedraw();
        }
    }

    /// <summary>The columns shown after the first (the deployment table's, or another screen's).</summary>
    private readonly IReadOnlyList<DeploymentColumnInfo> _columns;

    /// <summary>Whether the first column marks the chosen row (<see cref="Chosen"/>) instead of the party's places.</summary>
    private readonly bool _chooser;

    /// <summary>The headers' font: Cinzel over the text's (the text's font the fallback for the sort marks).</summary>
    private Font _titleFont = null!;

    /// <summary>The seals' numerals.</summary>
    private readonly FontVariation _sealFont = Lacquer.Cinzel(700);

    /// <summary>The talk bubbles' "!" and "?".</summary>
    private readonly FontVariation _bubbleFont = Lacquer.Cinzel(900);

    private IReadOnlyDictionary<int, SceneMark> _talkMarks = new Dictionary<int, SceneMark>();
    private string _talkMarksKey = "";

    /// <summary>
    /// A chooser's talk marks by unit (<see cref="SubordinatesScreen.TalkMarks"/>): a bubble in each marked unit's row,
    /// none in the others' (decision 30). Filled in again only when they change.
    /// </summary>
    public IReadOnlyDictionary<int, SceneMark> TalkMarks
    {
        get => _talkMarks;
        set
        {
            string key = string.Join(",", value.OrderBy(m => m.Key).Select(m => $"{m.Key}:{(int)m.Value}"));
            if (key == _talkMarksKey)
                return;
            (_talkMarks, _talkMarksKey) = (value, key);
            Fill();
        }
    }

    /// <summary>The room the talk bubbles take at the first column's right, in the text's size.</summary>
    private float BubbleRoom => _chooser ? _fontSize * 1.45f : 0;

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

    /// <summary>The deployment screen's table: <see cref="DeploymentTable.Columns"/>, the party's places marked, the Refill buttons.</summary>
    public DeploymentTableView(GameFont font) : this(font, DeploymentTable.Columns, chooser: false, textSize: 8.3f, refillButtons: true)
    {
    }

    /// <summary>
    /// A table of <paramref name="columns"/>, its text <paramref name="textSize"/> script pixels; a
    /// <paramref name="chooser"/> marks its chosen row in the first column; with <paramref name="refillButtons"/> a Refill
    /// cell is a button where the unit can be replenished.
    /// </summary>
    public DeploymentTableView(GameFont font, IReadOnlyList<DeploymentColumnInfo> columns, bool chooser, float textSize,
        bool refillButtons = false)
    {
        _columns = columns;
        _chooser = chooser;
        _textSize = textSize;
        _refillButtons = refillButtons;
        Columns = _columns.Count + 1;
        HideRoot = true;
        ColumnTitlesVisible = true;
        FocusMode = FocusModeEnum.None;
        SelectMode = SelectModeEnum.Row;
        Visible = false;
        SetColumnTitle(0, chooser ? "" : PartyMark);
        SetColumnTitleAlignment(0, HorizontalAlignment.Center);
        bool hover = !TouchMode.On;                                     // the touch mode's tips come by a long press
        SetColumnTitleTooltipText(0, chooser || !hover ? "" : "Place in the party");
        for (int c = 0; c < _columns.Count; c++)
        {
            var info = _columns[c];
            SetColumnTitle(c + 1, info.Header);
            SetColumnTitleTooltipText(c + 1, hover ? info.FullName : "");
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
        AddChild(_tip);
    }

    /// <summary>Option → Display: the ledger's look and its rows in the palette's colours again.</summary>
    public void ApplyPalette()
    {
        ApplyLedger();
        Fill();
    }

    /// <summary>The ledger's look: the paper, its header in vermilion ink over a rule, the rows' rules, a faint hover.</summary>
    private void ApplyLedger()
    {
        AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = Ui.Paper, BorderColor = Ui.DimGold,
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
        });
        StyleBoxFlat Head(Color colour) => new()
        {
            BgColor = colour, BorderColor = Ui.HeadInk, BorderWidthBottom = 1,
            ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 3, ContentMarginBottom = 3,
        };
        AddThemeStyleboxOverride("title_button_normal", Head(Ui.PaperHead));
        AddThemeStyleboxOverride("title_button_hover", Head(Ui.PaperHead.Lightened(0.12f)));
        AddThemeStyleboxOverride("title_button_pressed", Head(Ui.PaperHead));
        AddThemeColorOverride("font_color", Ui.Ink);
        AddThemeColorOverride("title_button_color", Ui.HeadInk);
        AddThemeColorOverride("guide_color", Ui.PaperRule);
        foreach (var state in new[] { "font_hovered_color", "font_selected_color", "font_hovered_selected_color" })
            AddThemeColorOverride(state, Ui.Ink);
        AddThemeStyleboxOverride("hovered", new StyleBoxFlat { BgColor = Ui.Vermilion with { A = 0.08f } });
        foreach (var state in new[] { "selected", "selected_focus", "hovered_selected", "hovered_selected_focus" })
            AddThemeStyleboxOverride(state, new StyleBoxEmpty());          // the chosen row is marked by Fill
        var bars = new Theme();
        foreach (string bar in new[] { "HScrollBar", "VScrollBar" })
        {
            bars.SetStylebox("scroll", bar, BarBox(Ui.PaperHead));
            bars.SetStylebox("grabber", bar, BarBox(Ui.InkQuiet));
            bars.SetStylebox("grabber_highlight", bar, BarBox(Ui.VermilionInk));
            bars.SetStylebox("grabber_pressed", bar, BarBox(Ui.VermilionInk));
        }
        Theme = bars;
    }

    /// <summary>A scroll bar's part: its margins make the bar as thick as the default theme's.</summary>
    private static StyleBoxFlat BarBox(Color color) => new()
    {
        BgColor = color, ContentMarginLeft = 4, ContentMarginRight = 4, ContentMarginTop = 4, ContentMarginBottom = 4,
    };

    /// <summary>Option → Display: the table's text in the chosen font (regular, unlike the surface's bold).</summary>
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

    /// <summary>The font of our own screens: the Option → Display face, regular (MS Gothic where installed, else BIZ UDGothic).</summary>
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
        if (rows.SequenceEqual(_rows) && _separator == Figures.Separator)
            return;
        _rows = rows;
        _separator = Figures.Separator;
        Fill();
    }

    /// <summary>The units with a row, in the rows' order.</summary>
    public IEnumerable<int> Units => _rows.Select(r => r.Unit);

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
    public bool HasPointer => Visible && GetGlobalRect().HasPoint(TouchMode.GlobalPointer(this));

    /// <summary>The unit whose row is under the pointer, or 0 (the header, the space below the rows, a scroll bar).</summary>
    public int HoveredUnit => HasPointer ? UnitAt(TouchMode.LocalPointer(this)) : 0;

    private int UnitAt(Vector2 local)
    {
        foreach (var child in GetChildren(includeInternal: true))
        {
            if (child is ScrollBar { Visible: true } bar && bar.GetRect().HasPoint(local))
                return 0;
        }
        return GetItemAtPosition(local) is { } item ? item.GetMetadata(0).AsInt32() : 0;
    }

    /// <summary>
    /// Left presses on rows and right presses anywhere are the game's — a left press on a Refill button its own; the rest
    /// (the header, the wheel, the scroll bars) the tree's own, but the touch mode's header (<see cref="HeaderTouch"/>).
    /// The pointer over a Refill button lights it.
    /// </summary>
    public override void _GuiInput(InputEvent @event)
    {
        if (TouchMode.On && HeaderTouch(@event))
        {
            AcceptEvent();
            return;
        }
        if (@event is InputEventMouseMotion motion)
        {
            int over = RefillAt(motion.Position);
            if (over != _refillHovered)
            {
                _refillHovered = over;
                MouseDefaultCursorShape = over != 0 ? CursorShape.PointingHand : CursorShape.Arrow;
                QueueRedraw();
            }
            return;
        }
        if (@event is not InputEventMouseButton { Pressed: true } button)
            return;
        if (button.ButtonIndex == MouseButton.Right)
        {
            RightPressed?.Invoke();
            AcceptEvent();
        }
        else if (button.ButtonIndex == MouseButton.Left && RefillAt(button.Position) is var refill and > 0)
        {
            RefillPressed?.Invoke(refill);
            AcceptEvent();
        }
        else if (button.ButtonIndex == MouseButton.Left && UnitAt(button.Position) is var unit and > 0)
        {
            RowPressed?.Invoke(unit);
            AcceptEvent();
        }
    }

    public override void _Notification(int what)
    {
        if (what == NotificationVisibilityChanged && !IsVisibleInTree())
            _tip.Visible = false;
        if (what == NotificationMouseExit && _refillHovered != 0)
        {
            _refillHovered = 0;
            MouseDefaultCursorShape = CursorShape.Arrow;
            QueueRedraw();
        }
    }

    /// <summary>The Refill column's index in the tree (-1 without one, or without buttons).</summary>
    private int RefillColumn => _refillButtons ? _columns.ToList().FindIndex(c => c.Column == DeploymentColumn.Refill) + 1 is var c and > 0 ? c : -1 : -1;

    /// <summary>The unit whose Refill button is at <paramref name="local"/>, or 0.</summary>
    private int RefillAt(Vector2 local)
    {
        if (RefillColumn < 0 || GetColumnAtPosition(local) != RefillColumn || UnitAt(local) is not (var unit and > 0))
            return 0;
        return _rows.FirstOrDefault(r => r.Unit == unit) is { CanRefill: true } ? unit : 0;
    }

    /// <summary>Whether <paramref name="unit"/>'s Refill cell is a button now (unattended play).</summary>
    public bool CanRefill(int unit) => RefillColumn >= 0 && _rows.FirstOrDefault(r => r.Unit == unit) is { CanRefill: true };

    /// <summary>A unit's Refill cell in global coordinates (the callout's place), or null when its row is not shown.</summary>
    public Rect2? RefillCellRect(int unit)
    {
        if (RefillColumn < 0)
            return null;
        for (var item = GetRoot()?.GetFirstChild(); item is not null; item = item.GetNext())
        {
            if (item.GetMetadata(0).AsInt32() != unit)
                continue;
            var rect = GetItemAreaRect(item, RefillColumn);
            return GetGlobalTransform() * rect;
        }
        return null;
    }

    /// <summary>A unit's Refill button pressed as the mouse presses it (unattended play goes through here).</summary>
    public void PressRefill(int unit)
    {
        if (!CanRefill(unit))
            throw new InvalidOperationException($"Unit {unit} has no Refill button in the deployment table.");
        RefillPressed?.Invoke(unit);
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
        widths[0] = Math.Max(widths[0], _fontSize * 1.3f) + BubbleRoom;     // the chosen row's bar and diamond, a place's seal; the talk's bubble
        if (RefillColumn > 0)
            widths[RefillColumn] += _fontSize * 0.8f;                       // the buttons' rule and padding
        for (int c = 0; c < Columns; c++)
            SetColumnCustomMinimumWidth(c, (int)Math.Ceiling(widths[c]) + separation + 6);
    }

    /// <summary>
    /// A ledger's row: the chosen one tinted with a vermilion bar and diamond in the first column, a member of the
    /// party gilt with its place a vermilion seal there, the deployed ones in grey ink, the refill's price in
    /// vermilion (on the deployment screen a button where the unit can be replenished, else greyed with its reason as the
    /// tooltip), the troops with a bar of their strength under them.
    /// </summary>
    private void LedgerRow(TreeItem item, DeploymentRow row, bool chosen, int place)
    {
        for (int c = 0; c < _columns.Count; c++)
        {
            var info = _columns[c];
            item.SetText(c + 1, row.Text(info.Column));
            item.SetTextAlignment(c + 1, Alignment(info));
            if (row.Grey)
                item.SetCustomColor(c + 1, Ui.InkGrey);
            else if (info.Column == DeploymentColumn.Refill && row.Refill > 0 && _refillButtons && !row.CanRefill)
                item.SetCustomColor(c + 1, Ui.InkQuiet);           // a price that is no button: greyed, its reason the tooltip
            else if (info.Column == DeploymentColumn.Refill && row.Refill > 0)
                item.SetCustomColor(c + 1, Ui.VermilionInk);
            else if (info.IsModifier && row.Text(info.Column) is "0" or "—")
                item.SetCustomColor(c + 1, Ui.InkFaint);           // no modifier: faint, so +n and −n stand out (the owner, 2026-10-23)
            if (info.Column == DeploymentColumn.Refill && _refillButtons && row.RefillNote != "")
                item.SetTooltipText(c + 1, row.RefillNote);
            if (info.Column == DeploymentColumn.Refill && _refillButtons && row.CanRefill)
            {
                item.SetCellMode(c + 1, TreeItem.TreeCellMode.Custom);
                var (text, unit) = (row.Text(info.Column), row.Unit);
                item.SetCustomDrawCallback(c + 1, Callable.From<TreeItem, Rect2>((_, rect) => DrawRefill(rect, text, unit)));
                item.SetTooltipText(c + 1, $"Replenish: {Figures.Signed(row.MaxTroops - row.Troops)} troops");
            }
            else if (info.IsText && info.Column != DeploymentColumn.Name)
                item.SetCustomColor(c + 1, Ui.InkQuiet);
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
                item.SetCustomBgColor(c, Ui.PartyRow);
            item.SetCellMode(0, TreeItem.TreeCellMode.Custom);
            item.SetCustomDrawCallback(0, Callable.From<TreeItem, Rect2>((_, rect) =>
            {
                float size = Math.Min(rect.Size.Y - 2, _fontSize * 1.15f);
                Ui.DrawSeal(this, _sealFont, new Rect2(rect.GetCenter() - new Vector2(size, size) / 2, new Vector2(size, size)), place);
            }));
        }
        SceneMark? talk = _chooser && _talkMarks.TryGetValue(row.Unit, out var m) ? m : null;
        if (talk is { } t)
            item.SetTooltipText(0, SubordinatesScreen.TalkNote(t));
        if (!chosen && talk is null)
            return;
        if (chosen)
            for (int c = talk is null ? 0 : 1; c < Columns; c++)
                item.SetCustomBgColor(c, Ui.ChosenRow);    // the tree tints over a custom cell: the bubble's cell tinted below
        item.SetCellMode(0, TreeItem.TreeCellMode.Custom);
        item.SetCustomDrawCallback(0, Callable.From<TreeItem, Rect2>((_, rect) =>
        {
            var mark = rect with { Size = new Vector2(rect.Size.X - BubbleRoom, rect.Size.Y) };
            if (chosen && talk is not null)
                DrawRect(rect, Ui.ChosenRow);
            if (chosen)
            {
                DrawRect(new Rect2(rect.Position.X, rect.Position.Y, Math.Max(2, rect.Size.Y * 0.14f), rect.Size.Y), Ui.Vermilion);
                Lacquer.DrawDiamond(this, mark.GetCenter() + new Vector2(rect.Size.Y * 0.1f, 0), rect.Size.Y * 0.2f, Ui.Vermilion);
            }
            if (talk is { } shown)
                DrawBubble(new Vector2(mark.End.X + BubbleRoom / 2, rect.GetCenter().Y), rect.Size.Y, shown);
        }));
    }

    /// <summary>
    /// A talk's speech bubble centred at <paramref name="at"/> in a row <paramref name="h"/> tall (mock-up S): a rounded
    /// box with its tail at the bottom left, filled green with "!" (new), vermilion with "?" (???), an empty outline (read).
    /// </summary>
    private void DrawBubble(Vector2 at, float h, SceneMark mark)
    {
        var box = new Rect2(at.X - h * 0.46f, at.Y - h * 0.3f, h * 0.92f, h * 0.54f);
        int r = Math.Max(2, (int)Math.Round(h * 0.21f));
        bool read = mark == SceneMark.Read;
        var colour = mark switch { SceneMark.New => Ui.InkGreen, SceneMark.Maybe => Ui.VermilionInk, _ => Ui.ReadOutline };
        Vector2[] tail = [new(at.X - h * 0.25f, box.End.Y - 1), new(at.X - h * 0.04f, box.End.Y - 1), new(at.X - h * 0.33f, at.Y + h * 0.42f)];
        if (read)
        {
            DrawPolyline([tail[0], tail[2], tail[1]], colour, 1, true);
            DrawStyleBox(new StyleBoxFlat
            {
                BgColor = Ui.Paper, BorderColor = colour,
                BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
                CornerRadiusTopLeft = r, CornerRadiusTopRight = r, CornerRadiusBottomLeft = r, CornerRadiusBottomRight = r,
                AntiAliasing = true,
            }, box);
            return;
        }
        DrawColoredPolygon(tail, colour);
        DrawStyleBox(new StyleBoxFlat
        {
            BgColor = colour,
            CornerRadiusTopLeft = r, CornerRadiusTopRight = r, CornerRadiusBottomLeft = r, CornerRadiusBottomRight = r,
            AntiAliasing = true,
        }, box);
        string label = mark == SceneMark.New ? "!" : "?";
        int size = Math.Max(6, (int)Math.Round(h * 0.42f));
        float baseline = box.GetCenter().Y + (_bubbleFont.GetAscent(size) - _bubbleFont.GetDescent(size)) / 2;
        DrawString(_bubbleFont, new Vector2(box.Position.X, baseline), label, HorizontalAlignment.Center, box.Size.X, size, Ui.Paper);
    }

    /// <summary>
    /// A Refill button (owner's decision 27, mock-up A1): the price right in a vermilion-ruled chip, filled vermilion with
    /// the paper's colour for its text while hovered or asked about.
    /// </summary>
    private void DrawRefill(Rect2 cell, string text, int unit)
    {
        bool lit = unit == _refillHovered || unit == AskedRefill;
        var chip = new Rect2(cell.Position.X + 3, cell.Position.Y + 2, cell.Size.X - 6, cell.Size.Y - 4);
        float corner = Math.Max(2, chip.Size.Y * 0.18f);
        var box = new StyleBoxFlat
        {
            BgColor = lit ? Ui.Vermilion : new Color(0, 0, 0, 0),
            BorderColor = Ui.VermilionInk with { A = lit ? 1 : 0.6f },
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            CornerRadiusTopLeft = (int)corner, CornerRadiusTopRight = (int)corner,
            CornerRadiusBottomLeft = (int)corner, CornerRadiusBottomRight = (int)corner,
        };
        DrawStyleBox(box, chip);
        float baseline = cell.GetCenter().Y + (_font.GetAscent(_fontSize) - _font.GetDescent(_fontSize)) / 2;
        DrawString(_font, new Vector2(chip.Position.X, baseline), text, HorizontalAlignment.Right, chip.Size.X - 5, _fontSize,
            lit ? Ui.Paper : Ui.VermilionInk);
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
            grey ? Ui.InkGrey : Ui.Ink);
        float f = Math.Clamp(troops / (float)max, 0, 1);
        var colour = f >= 0.75f ? Ui.BarGood : f >= 0.35f ? Ui.BarLow : Ui.BarBad;
        if (grey)
            colour = colour with { A = 0.45f };
        var bar = new Rect2(cell.Position.X + 2, cell.End.Y - h - 1, cell.Size.X - 4, h);
        DrawRect(bar, Ui.PaperRule);
        DrawRect(bar with { Size = new Vector2(Math.Max(1, bar.Size.X * f), h) }, colour);
    }
}
