using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.Strategy;

// The ledger on the phone held upright (docs/ui/phone-build.md, step P5; docs/ui/phone.md, §20, the owner's PD2s2): two-line
// rows, the seal (or the chosen row's diamond), №, the face and the name over the unit's type over the troops' and the HP's
// bars fixed at the left, the tab's other columns beside them in one line, scrolled sideways under a finger, each tab its own scroll.
// The tree keeps its rows, its sort and its gestures; its first column is the fixed part's room, which the overlays cover —
// the header's (DrawOverlay) and the rows' (DrawFixedRows, clipped under the header) —, so the columns scrolled sideways
// pass under it. A shadow at the fixed part's edge while columns have passed under it, a shadow and ▶ at the view's right
// edge while some lie beyond it.
internal sealed partial class DeploymentTableView
{
    /// <summary>The phone's frame held upright while the table is laid out so (<see cref="PlaceUpright"/>), else null.</summary>
    private PhoneFrame? _upright;

    /// <summary>The fixed part's width in this control's pixels (0: none).</summary>
    private float _fixedWidth;

    /// <summary>Over the rows under the header: the fixed part and the shadows (upright only).</summary>
    private readonly Overlay _fixedRows;

    /// <summary><see cref="HeaderColumnAt"/>'s columns for the fixed part's headings: № (+0), the name (+1), the type (+2).</summary>
    private const int FixedHeader = 1000;

    /// <summary>Each tab's sideways scroll, kept while another is shown; the one to take once the tree has laid its columns out.</summary>
    private readonly Dictionary<LedgerTab, double> _tabScroll = [];

    private double? _pendingScroll;
    private ulong _pendingFrame;

    /// <summary>After a turn, the unit whose row stood first in view, to stand there again once the rows are laid out (0: none).</summary>
    private int _pendingTop;

    /// <summary>
    /// Covers <paramref name="rect"/> on the phone held upright (PD2s2): rows <see cref="PhoneLedgerUpright.Row"/> high, the
    /// figures <see cref="PhoneLedgerUpright.Text"/>, the columns <see cref="PhoneLedgerUpright.ColumnGap"/> apart, the
    /// fixed part <see cref="PhoneLedgerUpright.Fixed"/> wide (reference pixels of <paramref name="phone"/>).
    /// </summary>
    public void PlaceUpright(Rect2 rect, PhoneFrame phone)
    {
        HoldLandscape(null);
        bool entering = _upright is null;
        _upright = phone;
        _fixedWidth = phone.Px(PhoneLedgerUpright.Fixed);
        if (entering)
            ShowColumns();
        int separation = (int)Math.Round(phone.Px(6));
        int size = (int)Math.Round(phone.Px(PhoneLedgerUpright.Text));
        Lay(rect, size, separation, (int)Math.Round(phone.Px(PhoneLedgerUpright.Row)) - 2 * separation,
            (int)Math.Round(phone.Px(PhoneLedgerUpright.ColumnGap)));
        float right = rect.Size.X - (VScroll?.Size.X ?? 0), bottom = rect.Size.Y - (HScroll?.Size.Y ?? 0);
        _fixedRows.Position = new Vector2(0, HeaderBottom);
        _fixedRows.Size = new Vector2(Math.Max(0, right - 1), Math.Max(0, bottom - HeaderBottom - 1));
        _fixedRows.Visible = true;
    }

    /// <summary>Back to every column in the tree when the table is laid out otherwise.</summary>
    private void LeaveUpright()
    {
        if (_upright is null)
            return;
        if (Tab is { } tab)
            _tabScroll[tab] = HScroll?.Value ?? 0;                      // kept for a turn back upright
        (_upright, _fixedWidth, _pendingScroll) = (null, 0, null);
        _fixedRows.Visible = false;
        ShowColumns();
    }

    /// <summary>The name and the type with the troops of <paramref name="unit"/>'s row (a long press's picture sheet), or null.</summary>
    public (string Name, string Detail)? Caption(int unit) =>
        _rows.FirstOrDefault(r => r.Unit == unit) is { } row
            ? (row.Text(DeploymentColumn.Name), $"{row.Text(DeploymentColumn.TroopType)} · {row.Text(DeploymentColumn.Troops)}")
            : null;

    /// <summary>Upright, the sideways scroll of the tab left kept and the next one's taken once its columns are laid out.</summary>
    private void KeepTabScroll(LedgerTab? from, LedgerTab? to)
    {
        if (_upright is null)
            return;
        if (from is { } left)
            _tabScroll[left] = HScroll?.Value ?? 0;
        _pendingScroll = to is { } next && _tabScroll.TryGetValue(next, out double value) ? value : 0;
        _pendingFrame = Godot.Engine.GetProcessFrames();
    }

    /// <summary>A tab's kept scroll, a frame after its columns were laid out (the tree sizes its bar as it draws).</summary>
    private void TakePendingScroll()
    {
        if (Godot.Engine.GetProcessFrames() <= _pendingFrame)
            return;
        if (_pendingScroll is { } value)
        {
            _pendingScroll = null;
            if (HScroll is { } bar)
                bar.Value = value;
        }
        if (_pendingTop != 0)
        {
            ScrollToTop(_pendingTop);
            _pendingTop = 0;
        }
    }

    /// <summary>The unit whose row stands first in view under the header (0: none).</summary>
    private int TopUnit()
    {
        for (var item = GetRoot()?.GetFirstChild(); item is not null; item = item.GetNext())
        {
            if (GetItemAreaRect(item, 0).End.Y > HeaderBottom + 1)
                return item.GetMetadata(0).AsInt32();
        }
        return 0;
    }

    /// <summary>
    /// A turn of the phone laid the rows out again (a row's height is another, so the scroll's pixels show other rows): the
    /// row that stood first in view, <paramref name="top"/>'s, stands first again, and upright the tab's sideways scroll is
    /// the one it had there — both a frame later, once the tree has laid its rows out.
    /// </summary>
    private void KeepTopAcrossTurn(int top)
    {
        _pendingTop = top;
        if (_upright is not null && Tab is { } tab && _tabScroll.TryGetValue(tab, out double across))
            _pendingScroll = across;
        _pendingFrame = Godot.Engine.GetProcessFrames();
    }

    /// <summary><paramref name="unit"/>'s row first in view under the header, as far as the scroll goes.</summary>
    private void ScrollToTop(int unit)
    {
        if (VScroll is not { } bar)
            return;
        for (var item = GetRoot()?.GetFirstChild(); item is not null; item = item.GetNext())
        {
            if (item.GetMetadata(0).AsInt32() != unit)
                continue;
            bar.Value = bar.Value + GetItemAreaRect(item, 0).Position.Y - HeaderBottom;
            return;
        }
    }

    // ---------------------------------------------------------------- the fixed part's headings

    private DeploymentColumnInfo? FixedInfo(int column) =>
        column >= FixedHeader && column - FixedHeader < PhoneLedgerUpright.FixedColumns.Count
            ? _all.FirstOrDefault(c => c.Column == PhoneLedgerUpright.FixedColumns[column - FixedHeader])
            : null;

    private string FixedTitle(int i) => FixedInfo(FixedHeader + i)?.Header ?? "";

    /// <summary>The fixed part's heading at <paramref name="x"/> (<see cref="FixedHeader"/> + its index), or -1 outside it.</summary>
    private int FixedHeaderAt(float x)
    {
        if (_upright is not { } f || x >= _fixedWidth)
            return -1;
        float name = f.Px(PhoneLedgerUpright.HeadNameX);
        if (x < name - f.Px(8))
            return FixedHeader;
        float split = name + TitleWidth(FixedTitle(1) + _down) + TitleWidth(" · ") / 2;
        return x < split ? FixedHeader + 1 : FixedHeader + 2;
    }

    /// <summary>The middle of the fixed part's heading for <paramref name="column"/>, or null when it has none there.</summary>
    private float? FixedHeaderCentre(DeploymentColumn column)
    {
        if (_upright is not { } f)
            return null;
        float name = f.Px(PhoneLedgerUpright.HeadNameX);
        return column switch
        {
            DeploymentColumn.Number => f.Px(PhoneLedgerUpright.HeadNumberRight) - TitleWidth(FixedTitle(0)) / 2,
            DeploymentColumn.Name => name + TitleWidth(FixedTitle(1)) / 2,
            DeploymentColumn.TroopType => name + TitleWidth($"{FixedTitle(1)} · ") + TitleWidth(FixedTitle(2)) / 2,
            _ => null,
        };
    }

    private float TitleWidth(string text) => _titleFont.GetStringSize(text, fontSize: TitleSize).X;

    /// <summary>
    /// The fixed part's header over the tree's first columns: the party's dot (the deployment table's), № right-aligned,
    /// "Name · Type", the sort's mark after the sorted one; the edge's line, its shadow and the ▶ at the view's right.
    /// </summary>
    private void DrawFixedHeader(Control c)
    {
        if (_upright is not { } f)
            return;
        float w = _fixedWidth, bottom = HeaderBottom;
        c.DrawRect(new Rect2(1, 1, w - 1, bottom - 1), Ui.PaperHead);
        c.DrawLine(new Vector2(1, bottom - 0.5f), new Vector2(w, bottom - 0.5f), Ui.HeadInk, 1);
        var below = new Rect2(0, GroupStrip + 2, 0, bottom - GroupStrip - 2);
        float baseline = Lacquer.Baseline(below, _sealFont, TitleSize);
        string Marked(int i) => FixedTitle(i) + (PhoneLedgerUpright.FixedColumns[i] == _sort ? SortMark : "");
        if (!_chooser)
        {
            float dot = TitleWidth(PartyMark);
            c.DrawString(_titleFont, new Vector2(f.Px(PhoneLedgerUpright.DotX) - dot / 2, baseline), PartyMark, fontSize: TitleSize,
                modulate: Ui.HeadInk);
        }
        float number = f.Px(PhoneLedgerUpright.HeadNumberRight);
        c.DrawString(_titleFont, new Vector2(number - TitleWidth(FixedTitle(0)), baseline), Marked(0), fontSize: TitleSize, modulate: Ui.HeadInk);
        c.DrawString(_titleFont, new Vector2(f.Px(PhoneLedgerUpright.HeadNameX), baseline), $"{Marked(1)} · {Marked(2)}", fontSize: TitleSize,
            modulate: Ui.HeadInk);
        DrawShadows(c, f, 1, bottom);
        if (MoreBeyond)
        {
            float right = ViewRight, box = f.Px(PhoneLedgerUpright.MoreBox), a = f.Px(PhoneLedgerUpright.MoreArrow);
            c.DrawRect(new Rect2(right - box, below.Position.Y, box, below.Size.Y - 2), Ui.PaperHead);
            float mid = below.GetCenter().Y, x = right - box / 2;
            c.DrawColoredPolygon([new(x - a * 0.7f, mid - a), new(x + a * 0.9f, mid), new(x - a * 0.7f, mid + a)], Ui.HeadInk);
        }
    }

    /// <summary>The view's right edge: the tree's less its vertical bar.</summary>
    private float ViewRight => Size.X - (VScroll?.Size.X ?? 0) - 1;

    /// <summary>Whether columns lie beyond the view's right edge.</summary>
    private bool MoreBeyond => HScroll is { } bar && bar.Value < End(bar) - 0.5;

    /// <summary>
    /// The shadow at the fixed part's edge while columns have passed under it, the one at the view's right while some lie
    /// beyond it, and the edge's line, from <paramref name="top"/> to <paramref name="bottom"/> in <paramref name="c"/>'s pixels.
    /// </summary>
    private void DrawShadows(Control c, PhoneFrame f, float top, float bottom)
    {
        if (bottom <= top)
            return;
        float w = _fixedWidth, s = f.Px(PhoneLedgerUpright.Shadow);
        var dark = Ui.Ink with { A = 0.27f };
        var clear = Ui.Ink with { A = 0 };
        if (HScroll is { Value: > 0.5 })
            c.DrawPolygon([new(w, top), new(w + s, top), new(w + s, bottom), new(w, bottom)], [dark, clear, clear, dark]);
        if (MoreBeyond)
        {
            float right = ViewRight;
            c.DrawPolygon([new(right - s, top), new(right, top), new(right, bottom), new(right - s, bottom)], [clear, dark, dark, clear]);
        }
        c.DrawLine(new Vector2(w, top), new Vector2(w, bottom), Ui.InkQuiet, 1);
    }

    // ---------------------------------------------------------------- the fixed part's rows

    /// <summary>The fixed part of every row in view (in <see cref="_fixedRows"/>'s pixels, its top the header's foot) and the shadows.</summary>
    private void DrawFixedRows(Control c)
    {
        if (_upright is not { } f || GetRoot()?.GetFirstChild() is not { } first)
            return;
        float top = HeaderBottom, foot = 0;
        for (var item = first; item is not null; item = item.GetNext())
        {
            var rect = GetItemAreaRect(item, 0);
            float y = rect.Position.Y - top;
            foot = Math.Max(foot, y + rect.Size.Y);
            if (y + rect.Size.Y < 0 || y > c.Size.Y)
                continue;
            int unit = item.GetMetadata(0).AsInt32();
            if (_rows.FirstOrDefault(r => r.Unit == unit) is { } row)
                DrawFixedRow(c, f, new Rect2(1, y, _fixedWidth - 1, rect.Size.Y), row);
        }
        DrawShadows(c, f, 0, Math.Min(c.Size.Y, foot));
    }

    /// <summary>
    /// A row's fixed part (PD2s2): the paper (gilt for the party, tinted for the chosen row), the party's seal or the chosen
    /// row's bar and diamond, № and the name on the first line, the type under it, a talk's bubble at the name's right; the
    /// unit's face between № and the name, under the name and the type a bar of the troops in a battle card's blue and one of
    /// the commander's HP in its green, no figures (owner, 2026-10-31; the owner's H4, docs/ui/phone.md §37-§38).
    /// </summary>
    private void DrawFixedRow(Control c, PhoneFrame f, Rect2 cell, DeploymentRow row)
    {
        bool chosen = _chooser && row.Unit == Chosen;
        int place = _chooser ? 0 : row.PartyPlace;
        c.DrawRect(cell, chosen ? Ui.ChosenRow : place > 0 ? Ui.PartyRow : Ui.Paper);
        c.DrawLine(new Vector2(cell.Position.X, cell.End.Y - 0.5f), new Vector2(cell.End.X, cell.End.Y - 0.5f), Ui.PaperRule, 1);
        float y = cell.Position.Y, h = cell.Size.Y, mid = y + h / 2;
        float nameMid = y + h * PhoneLedgerUpright.NameMiddle / PhoneLedgerUpright.Row;
        float typeMid = y + h * PhoneLedgerUpright.TypeMiddle / PhoneLedgerUpright.Row;
        if (chosen)
        {
            c.DrawRect(new Rect2(0, y, f.Px(PhoneLedgerUpright.ChosenBar), h), Ui.Vermilion);
            Lacquer.DrawDiamond(c, new Vector2(f.Px(PhoneLedgerUpright.DiamondX), mid), f.Px(PhoneLedgerUpright.Diamond), Ui.Vermilion);
        }
        if (place > 0)
        {
            float s = f.Px(PhoneLedgerUpright.Seal);
            Ui.DrawSeal(c, _sealFont, new Rect2(f.Px(PhoneLedgerUpright.SealX), mid - s / 2, s, s), place);
        }
        var ink = row.Grey ? Ui.InkGrey : Ui.Ink;
        int number = (int)Math.Round(f.Px(PhoneLedgerUpright.NumberText));
        string no = row.Text(DeploymentColumn.Number);
        Middle(c, no, number, ink, f.Px(PhoneLedgerUpright.NumberRight) - _font.GetStringSize(no, fontSize: number).X, nameMid);
        float side = f.Px(PhoneLedgerUpright.Face);
        DrawFace(c, new Rect2(f.Px(PhoneLedgerUpright.FaceX), mid - side / 2, side, side), row.FaceCg, row.Grey);
        SceneMark? talk = _chooser && _talkMarks.TryGetValue(row.Unit, out var mark) ? mark : null;
        float bubble = talk is null ? 0 : f.Px(PhoneLedgerUpright.NameText) * 1.4f;
        float nameX = f.Px(PhoneLedgerUpright.NameX), right = _fixedWidth - f.Px(PhoneLedgerUpright.NameRight);
        string name = row.Text(DeploymentColumn.Name);
        Middle(c, name, SideCells.Fit(_font, name, (int)Math.Round(f.Px(PhoneLedgerUpright.NameText)), right - bubble - nameX), ink, nameX, nameMid);
        string type = row.Text(DeploymentColumn.TroopType);
        float typeX = f.Px(PhoneLedgerUpright.TypeX);
        Middle(c, type, SideCells.Fit(_font, type, (int)Math.Round(f.Px(PhoneLedgerUpright.TypeText)), right - typeX),
            row.Grey ? Ui.InkGrey : Ui.InkQuiet, typeX, typeMid);
        float barTop = y + h * PhoneLedgerUpright.BarTop / PhoneLedgerUpright.Row;
        float hpTop = y + h * PhoneLedgerUpright.HpTop / PhoneLedgerUpright.Row, barHeight = f.Px(PhoneLedgerUpright.BarHeight);
        DrawGauge(c, new Rect2(nameX, barTop, right - nameX, barHeight), row.Troops, row.MaxTroops, Ui.GaugeTroops, row.Grey);
        DrawGauge(c, new Rect2(nameX, hpTop, right - nameX, barHeight), row.Hp, row.MaxHp, Ui.GaugeHp, row.Grey);
        if (talk is { } shown)
            DrawBubble(c, new Vector2(right - bubble / 2, nameMid), f.Px(PhoneLedgerUpright.NameText) * 1.2f, shown);
    }

    /// <summary><paramref name="text"/> from <paramref name="x"/>, its middle at <paramref name="middle"/>, in the table's font.</summary>
    private void Middle(Control c, string text, int size, Color colour, float x, float middle) =>
        c.DrawString(_font, new Vector2(x, middle + (_font.GetAscent(size) - _font.GetDescent(size)) / 2), text, fontSize: size, modulate: colour);
}
