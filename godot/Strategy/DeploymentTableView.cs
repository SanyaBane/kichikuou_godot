using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The deployment table (owner's decision 25, docs/ui/deployment-table.md): our own screen over
/// <c>Sﾌﾞﾀｲ</c>'s unit grid — a row per listed unit (<see cref="DeploymentTable.Rows"/>), the
/// columns' compact headers with their full names as tooltips, sorted by a header click (ascending,
/// again descending, a small mark on the header with its room kept; by number at first),
/// scrolling both ways, the units deployed this week grey, the party's members marked with their
/// place in a first column. It decides nothing: the presses go to <see cref="GameScreen"/>, which
/// answers the script with the gestures it expects.
/// </summary>
internal sealed partial class DeploymentTableView : Tree
{
    /// <summary>The text size in script pixels (the grid's names are 12); scaled with the screen.</summary>
    private const float TextSize = 10;

    /// <summary>The party column's header: a circle (both fonts have it), its full name the tooltip.</summary>
    private const string PartyMark = "●";

    private static readonly Color Panel = new(0.04f, 0.05f, 0.12f, 0.94f);
    private static readonly Color Text = new(0.95f, 0.95f, 0.95f);
    private static readonly Color GreyText = new(0.5f, 0.5f, 0.55f);
    private static readonly Color MemberBack = new(0.2f, 0.3f, 0.6f, 0.6f);
    private static readonly Color Title = new(0.12f, 0.14f, 0.26f);

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

    public DeploymentTableView(GameFont font)
    {
        Columns = DeploymentTable.Columns.Count + 1;
        HideRoot = true;
        ColumnTitlesVisible = true;
        FocusMode = FocusModeEnum.None;
        SelectMode = SelectModeEnum.Row;
        Visible = false;
        SetColumnTitle(0, PartyMark);
        SetColumnTitleAlignment(0, HorizontalAlignment.Center);
        SetColumnTitleTooltipText(0, "Place in the party");
        for (int c = 0; c < DeploymentTable.Columns.Count; c++)
        {
            var info = DeploymentTable.Columns[c];
            SetColumnTitle(c + 1, info.Header);
            SetColumnTitleTooltipText(c + 1, info.FullName);
            SetColumnTitleAlignment(c + 1, Alignment(info));
        }
        for (int c = 0; c < Columns; c++)
        {
            SetColumnExpand(c, false);
            SetColumnClipContent(c, true);
        }
        AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = Panel });
        AddThemeStyleboxOverride("focus", new StyleBoxEmpty());
        var title = new StyleBoxFlat { BgColor = Title, ContentMarginLeft = 4, ContentMarginRight = 4 };
        AddThemeStyleboxOverride("title_button_normal", title);
        AddThemeStyleboxOverride("title_button_hover", new StyleBoxFlat { BgColor = Title.Lightened(0.2f), ContentMarginLeft = 4, ContentMarginRight = 4 });
        AddThemeStyleboxOverride("title_button_pressed", title);
        AddThemeColorOverride("font_color", Text);
        AddThemeColorOverride("title_button_color", Text);
        AddThemeConstantOverride("h_separation", 6);
        AddThemeConstantOverride("v_separation", 2);
        AddThemeConstantOverride("item_margin", 0);
        ColumnTitleClicked += (column, button) =>
        {
            if (button == (long)MouseButton.Left && column > 0)
                SortBy(DeploymentTable.Columns[(int)column - 1].Column);
        };
        SetFont(font);
    }

    /// <summary>Option → Fonts: the table's text in the chosen font (regular, unlike the surface's bold).</summary>
    public void SetFont(GameFont font)
    {
        _font = new FontFile
        {
            Data = font == GameFont.MsGothic && GodotGlyphRenderer.MsGothicInstalled
                ? System.IO.File.ReadAllBytes(GodotGlyphRenderer.MsGothicPath)
                : Godot.FileAccess.GetFileAsBytes(GodotGlyphRenderer.BizUdGothicPath),
        };
        AddThemeFontOverride("font", _font);
        AddThemeFontOverride("title_button_font", _font);
        // The sort mark: the narrowest pair the font has, so the header grows little.
        (_up, _down) = SortMarks
            .Where(m => _font.HasChar(m.Up[0]) && _font.HasChar(m.Down[0]))
            .MinBy(m => Math.Max(_font.GetStringSize(m.Up, fontSize: 16).X, _font.GetStringSize(m.Down, fontSize: 16).X));
        Fill();
    }

    /// <summary>
    /// Covers <paramref name="rect"/> (in its parent's coordinates) with the text scaled by
    /// <paramref name="pixelsPerScript"/>, the screen's pixels per script pixel.
    /// </summary>
    public void Place(Rect2 rect, float pixelsPerScript)
    {
        Position = rect.Position;
        Size = rect.Size;
        int size = Math.Max(8, (int)Math.Round(TextSize * pixelsPerScript));
        if (size == _fontSize)
            return;
        _fontSize = size;
        AddThemeFontSizeOverride("font_size", size);
        AddThemeFontSizeOverride("title_button_font_size", size);
        Fill();
    }

    /// <summary>The rows from <paramref name="state"/>, filled in again when anything shown changed.</summary>
    public void Refresh(GameState state)
    {
        var rows = DeploymentTable.Rows(state);
        if (rows.SequenceEqual(_rows))
            return;
        _rows = rows;
        Fill();
    }

    /// <summary>Whether <paramref name="unit"/> has a row.</summary>
    public bool Lists(int unit) => _rows.Any(r => r.Unit == unit);

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

    /// <summary>A header click: by that column ascending, again descending.</summary>
    private void SortBy(DeploymentColumn column)
    {
        _descending = column == _sort && !_descending;
        _sort = column;
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
        Fit(0, PartyMark);
        Fit(0, "4");
        for (int c = 0; c < DeploymentTable.Columns.Count; c++)
        {
            var info = DeploymentTable.Columns[c];
            string header = info.Header + (info.Column == _sort ? _descending ? _down : _up : "");
            SetColumnTitle(c + 1, header);
            Fit(c + 1, info.Header + _down);                           // room for the mark: a sort never widens a column
        }
        foreach (var row in DeploymentTable.Sort(_rows, _sort, _descending))
        {
            var item = CreateItem(root);
            item.SetMetadata(0, row.Unit);
            item.SetText(0, row.PartyPlace > 0 ? row.PartyPlace.ToString() : "");
            item.SetTextAlignment(0, HorizontalAlignment.Center);
            for (int c = 0; c < DeploymentTable.Columns.Count; c++)
            {
                var info = DeploymentTable.Columns[c];
                string text = row.Text(info.Column);
                item.SetText(c + 1, text);
                item.SetTextAlignment(c + 1, Alignment(info));
                Fit(c + 1, text);
            }
            for (int c = 0; c < Columns; c++)
            {
                if (row.Grey)
                    item.SetCustomColor(c, GreyText);
                if (row.PartyPlace > 0)
                    item.SetCustomBgColor(c, MemberBack);
            }
        }
        int separation = GetThemeConstant("h_separation");
        for (int c = 0; c < Columns; c++)
            SetColumnCustomMinimumWidth(c, (int)Math.Ceiling(widths[c]) + separation + 6);
    }
}
