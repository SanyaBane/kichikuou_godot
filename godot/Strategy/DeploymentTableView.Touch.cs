using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The touch mode's header (owner, 2026-10-24; docs/ui/touch.md, "The tablet"): a touch screen has no hover for the
// headers' tooltips, so a long press on a header shows its full name (HeaderTip) and a tap sorts as a click does.
internal sealed partial class DeploymentTableView
{
    /// <summary>How long a finger stays on a header to show its tip (the battle cards' long press).</summary>
    private const double LongPressSeconds = 0.5;

    /// <summary>How far the finger may wander meanwhile (this control's pixels): further, it is neither a tap nor a long press.</summary>
    private const float LongPressSlop = 12;

    private readonly HeaderTip _tip = new();
    private bool _headerDown;           // a press began on the header and is not released yet
    private int _headerColumn = -1;     // its column, -1 once the finger wandered off
    private Vector2 _headerPress;       // where it came down
    private int _headerPresses;         // the presses counted: a timer acts only for its own
    private bool _headerLong;           // its long press showed the tip

    /// <summary>The header tip's text while it is shown, else null (unattended play).</summary>
    public string? HeaderTipShown => _tip.Shown;

    /// <summary>The column the rows are sorted by (unattended play).</summary>
    public DeploymentColumn SortColumn => _sort;

    /// <summary>
    /// The touch mode's presses on the header, from the mouse Godot makes of a touch or (with <c>--touch</c>) the mouse
    /// itself: held <see cref="LongPressSeconds"/> the column's tip, released sooner a sort, moved away nothing. Returns
    /// whether <paramref name="event"/> was taken (the tree's own header handling then never sees it).
    /// </summary>
    private bool HeaderTouch(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            if (_headerDown && motion.Position.DistanceTo(_headerPress) > LongPressSlop)
                _headerColumn = -1;
            return false;
        }
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
            return false;
        if (button.Pressed)
        {
            if (HeaderColumnAt(button.Position) is not (var column and >= 0))
                return false;
            (_headerDown, _headerColumn, _headerPress, _headerLong) = (true, column, button.Position, false);
            int press = ++_headerPresses;
            GetTree().CreateTimer(LongPressSeconds).Timeout += () =>
            {
                if (press != _headerPresses || !_headerDown || _headerColumn != column || !IsVisibleInTree())
                    return;
                _headerLong = true;
                ShowHeaderTip(column, _headerPress.X);              // no vibration (owner, 2026-10-24)
            };
            return true;
        }
        if (!_headerDown)
            return false;
        _headerDown = false;
        _headerPresses++;
        if (!_headerLong && _headerColumn > 0)
            SortBy(_columns[_headerColumn - 1]);
        return true;
    }

    /// <summary><paramref name="column"/>'s full name over its header, centred on <paramref name="x"/> (this control's pixels).</summary>
    private void ShowHeaderTip(int column, float x)
    {
        string text = column == 0 ? _chooser ? "" : "Place in the party" : _columns[column - 1].FullName;
        if (text.Length == 0)
            return;
        var header = new Rect2(GlobalPosition, new Vector2(Size.X, HeaderBottom));
        _tip.ShowAt(text, _font, _fontSize, header, GlobalPosition.X + x);
    }

    /// <summary>
    /// The header's bottom edge in this control's pixels: the panel's top border, then the title buttons as tall as Godot
    /// makes them (their text's height and the margins of <see cref="ApplyLedger"/>'s style boxes).
    /// </summary>
    private float HeaderBottom => 1 + _titleFont.GetHeight(TitleSize) + 6;

    /// <summary>The visible horizontal scroll bar, if any.</summary>
    private HScrollBar? HScroll => GetChildren(includeInternal: true).OfType<HScrollBar>().FirstOrDefault(b => b.Visible);

    /// <summary>The column whose header is at <paramref name="local"/>, or -1 (not the header; past the last column).</summary>
    private int HeaderColumnAt(Vector2 local)
    {
        if (local.Y < 0 || local.Y >= HeaderBottom)
            return -1;
        float x = local.X - 1 + (float)(HScroll?.Value ?? 0), right = 0;
        for (int c = 0; c < Columns; c++)
        {
            right += GetColumnWidth(c);
            if (x < right)
                return c;
        }
        return -1;
    }

    /// <summary><paramref name="column"/>'s header's centre in global coordinates, or null when it is scrolled out of sight (unattended play).</summary>
    public Vector2? HeaderCentre(DeploymentColumn column)
    {
        int index = _columns.ToList().FindIndex(c => c.Column == column) + 1;
        if (index <= 0)
            return null;
        float left = 1 - (float)(HScroll?.Value ?? 0);
        for (int c = 0; c < index; c++)
            left += GetColumnWidth(c);
        float centre = left + GetColumnWidth(index) / 2f;
        if (centre < 0 || centre > Size.X)
            return null;
        return GlobalPosition + new Vector2(centre, HeaderBottom / 2);
    }
}
