using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The unit ledger's paper tabs (docs/ui/unit-ledger.md, the owner's B1-B5): Overview, Troops, Commander over the
/// ledger's paper as the options window's tabs — the chosen one paper with a vermilion top, joined to the paper
/// (<see cref="DeploymentTableView.OpenTop"/>), the others darker paper, lit while hovered — and right of them, while the
/// rows are in the order of a column the tab does not show, that order as a gold seal (the owner's C2 in C3's colours,
/// round D). Drawn by the screen that holds it, in the 1280×800 screen's pixels; its tab is remembered for that screen
/// (<see cref="LedgerTabChoice"/>). In the touch mode the tabs are as tall as a touch row; on the phone held landscape they
/// take the row and the sizes the screen gives (<see cref="Phone"/>; docs/ui/phone-build.md, step L5).
/// </summary>
internal sealed class LedgerTabsBar
{
    private static UiPalette Ui => UiPalettes.For(UiPart.Screens);

    /// <summary>The tabs' row height in screen pixels: a touch row's in the touch mode.</summary>
    public static float RowHeight => Device.TouchLayout ? 46 : 34;

    private const int TabSize = 16, OrderSize = 14;
    private readonly string _screen;
    private readonly Rect2 _pcRow;
    private readonly FontVariation _cinzel = Lacquer.Cinzel(600), _cinzelBold = Lacquer.Cinzel(800);
    private LedgerTab? _pressed;

    /// <summary>
    /// The tabs of <paramref name="screen"/> ("deployment", "subordinates") in <paramref name="row"/>: from the tabs' top
    /// to the paper's top edge, as wide as the paper and starting at its left edge.
    /// </summary>
    public LedgerTabsBar(string screen, Rect2 row)
    {
        _screen = screen;
        _pcRow = row;
        Tab = LedgerTabChoice.Get(screen);
    }

    /// <summary>
    /// The phone's tabs (LD2, LU1), in the screen's drawing units: their row, each tab's width, the room between them, how
    /// much lower an unchosen one starts, the tabs' type and the order seal's; the first tab at the row's left. Null: the
    /// PC's and the tablet's.
    /// </summary>
    public readonly record struct PhoneTabs(Rect2 Row, float Width, float Gap, float Drop, int Text, int OrderText);

    /// <summary>The phone's layout while the screen lays itself out for one, else null.</summary>
    public PhoneTabs? Phone { get; set; }

    private Rect2 Row => Phone?.Row ?? _pcRow;

    private int TabText => Phone?.Text ?? TabSize;

    private int OrderText => Phone?.OrderText ?? OrderSize;

    /// <summary>The tab shown.</summary>
    public LedgerTab Tab { get; private set; }

    /// <summary>The tab under the pointer (null: none).</summary>
    public LedgerTab? Hovered { get; private set; }

    /// <summary>Shows <paramref name="tab"/>; <paramref name="remember"/> keeps it for the screen (the screen gallery's entries do not).</summary>
    public void Show(LedgerTab tab, bool remember = true)
    {
        Tab = tab;
        if (remember)
            LedgerTabChoice.Set(_screen, tab);
    }

    /// <summary>The tabs with their places: the chosen one a little taller, reaching the row's top.</summary>
    public IEnumerable<(LedgerTab Tab, Rect2 Rect)> Tabs()
    {
        float x = Row.Position.X + (Phone is null ? 8 : 0);
        foreach (var tab in LedgerTabs.All)
        {
            float w = Phone?.Width ?? _cinzelBold.GetStringSize(LedgerTabs.Title(tab), fontSize: TabSize).X + 40;
            float top = Row.Position.Y + (tab == Tab ? 0 : Phone?.Drop ?? 5);
            yield return (tab, new Rect2(x, top, w, Row.End.Y - top));
            x += w + (Phone?.Gap ?? 5);
        }
    }

    /// <summary>The tab at <paramref name="at"/> (screen pixels), its row's whole height, or null.</summary>
    public LedgerTab? TabAt(Vector2 at)
    {
        foreach (var (tab, rect) in Tabs())
            if (at.X >= rect.Position.X && at.X < rect.End.X && at.Y >= Row.Position.Y && at.Y < Row.End.Y)
                return tab;
        return null;
    }

    /// <summary>The pointer at <paramref name="at"/>: whether the hovered tab changed (the screen draws again).</summary>
    public bool Hover(Vector2 at)
    {
        var over = TabAt(at);
        if (over == Hovered)
            return false;
        Hovered = over;
        return true;
    }

    /// <summary>
    /// A left button at <paramref name="at"/>: a press on a tab, then its release on the same tab shows it (buttons act on
    /// release). Returns the tab shown by the release, else null.
    /// </summary>
    public LedgerTab? Press(Vector2 at, bool pressed)
    {
        var tab = TabAt(at);
        if (pressed)
        {
            _pressed = tab;
            return null;
        }
        bool shown = tab is not null && tab == _pressed && tab != Tab;
        _pressed = null;
        if (!shown)
            return null;
        Show(tab!.Value);
        return tab;
    }

    /// <summary>The paper's top edge under the chosen tab, in the table's local pixels (<paramref name="k"/> window pixels a screen pixel).</summary>
    public (float From, float To) OpenSpan(float k)
    {
        var rect = Tabs().First(t => t.Tab == Tab).Rect;
        return ((rect.Position.X - Row.Position.X) * k + 1, (rect.End.X - Row.Position.X) * k - 1);
    }

    /// <summary>
    /// The tabs and, while the order's column is not in the tab shown, the order's seal at the row's right — what
    /// <paramref name="table"/> says of its sort.
    /// </summary>
    public void Draw(CanvasItem c, float k, DeploymentTableView table)
    {
        foreach (var (tab, rect) in Tabs())
        {
            bool on = tab == Tab;
            c.DrawRect(rect, on ? Ui.Paper : Hovered == tab ? Ui.Field : Ui.PaperHead);
            c.DrawRect(rect, on ? Ui.PaperHead : Ui.InkGrey, false, Lacquer.Line(1, k));
            var font = on ? _cinzelBold : _cinzel;
            c.DrawString(font, new Vector2(rect.Position.X, Lacquer.Baseline(rect, font, TabText) + (on ? 1 : 0)), LedgerTabs.Title(tab),
                HorizontalAlignment.Center, rect.Size.X, TabText, on ? Ui.HeadInk : Ui.InkQuiet);
            if (on)
                c.DrawRect(new Rect2(rect.Position.X, rect.Position.Y, rect.Size.X, 4 * TabText / (float)TabSize), Ui.Vermilion);
        }
        if (!table.SortShown)
            DrawOrder(c, k, table);
    }

    /// <summary>
    /// "Sorted by T.Atk ↓" as a seal at the row's right (round D's D2): gold with a dim inner rule, the words in Cinzel
    /// and the mark in the table's font, lacquer-dark.
    /// </summary>
    private void DrawOrder(CanvasItem c, float k, DeploymentTableView table)
    {
        string words = $"Sorted by {table.SortHeader}", mark = table.SortMark;
        float m = OrderText / (float)OrderSize;
        float ww = _cinzelBold.GetStringSize(words, fontSize: OrderText).X;
        float mw = table.TextFont.GetStringSize(mark, fontSize: OrderText + 1).X;
        float w = ww + 6 * m + mw + 28 * m, h = Math.Min(28 * m, Row.Size.Y - 6 * m);
        var rect = new Rect2(Row.End.X - 2 - w, Row.Position.Y + (Row.Size.Y - 6 - h) / 2 + 1, w, h);
        c.DrawStyleBox(new StyleBoxFlat
        {
            BgColor = Ui.Gold,
            CornerRadiusTopLeft = 3, CornerRadiusTopRight = 3, CornerRadiusBottomLeft = 3, CornerRadiusBottomRight = 3,
            AntiAliasing = true,
        }, rect);
        c.DrawRect(rect.Grow(-2), Ui.DimGold, false, Lacquer.Line(1, k));
        float baseline = Lacquer.Baseline(rect, _cinzelBold, OrderText) + 1;
        c.DrawString(_cinzelBold, new Vector2(rect.Position.X + 14 * m, baseline), words, fontSize: OrderText, modulate: Ui.Row);
        c.DrawString(table.TextFont, new Vector2(rect.Position.X + 14 * m + ww + 6 * m, baseline), mark, fontSize: OrderText + 1,
            modulate: Ui.Row);
    }
}
