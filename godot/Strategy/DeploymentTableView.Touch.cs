using Godot;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// The touch mode's table (docs/ui/touch.md, "The tablet"): a touch screen has no hover for the headers' tooltips, so a
// long press on a header shows its full name (HeaderTip) and a tap sorts as a click does (owner, 2026-10-24); its scroll
// bars are too thin for a finger, so a finger dragged over the table scrolls it both ways, a flick going on after the
// release (owner, 2026-10-26; FingerScroll). A row's or a Refill button's press is therefore a tap: it acts at the release.
internal sealed partial class DeploymentTableView
{
    /// <summary>How long a finger stays on a header to show its tip (the battle cards' long press).</summary>
    private const double LongPressSeconds = 0.5;

    private readonly HeaderTip _tip = new();
    private readonly FingerScroll _finger = new();
    private Vector2 _touchFrom;         // where the press came down
    private Vector2 _scrollFrom;        // the scroll bars' values when its scroll started
    private int _headerColumn = -1;     // the header's column it came down on, -1 on the rows or once it scrolls
    private int _touchUnit;             // the row's unit it came down on (0: none)
    private int _touchRefill;           // the Refill button's unit it came down on (0: none)
    private int _touchPresses;          // the presses counted: a timer acts only for its own
    private bool _headerLong;           // its long press showed the tip
    private Vector2 _flingAt;           // the scroll bars' values a flick has reached
    private bool _scrolled;             // the last press scrolled the table: the hovered unit is the one it came down on
    private int _scrolledUnit;          // that unit (0: none)

    /// <summary>The header tip's text while it is shown, else null (unattended play).</summary>
    public string? HeaderTipShown => _tip.Shown;

    /// <summary>The column the rows are sorted by (unattended play).</summary>
    public DeploymentColumn SortColumn => _sort;

    /// <summary>The scroll bars' values (0 for a bar not shown; unattended play).</summary>
    public Vector2 ScrollValues => new((float)(HScroll?.Value ?? 0), (float)(VScroll?.Value ?? 0));

    /// <summary>The scroll bars' farthest values (0 for a bar not shown; unattended play).</summary>
    public Vector2 ScrollEnds => new(HScroll is { } h ? End(h) : 0, VScroll is { } v ? End(v) : 0);

    /// <summary>Whether a flick still scrolls the table (unattended play).</summary>
    public bool Flinging => _finger.Flinging;

    /// <summary>The rows' area in global coordinates: below the header, inside the scroll bars (unattended play).</summary>
    public Rect2 RowsArea
    {
        get
        {
            float right = Size.X - (VScroll?.Size.X ?? 0), bottom = Size.Y - (HScroll?.Size.Y ?? 0);
            return GetGlobalTransform() * new Rect2(1, HeaderBottom, right - 2, bottom - HeaderBottom - 1);
        }
    }

    /// <summary>The unit whose row is at <paramref name="global"/>, or 0 (unattended play).</summary>
    public int UnitAtPoint(Vector2 global) => UnitAt(GetGlobalTransform().AffineInverse() * global);

    /// <summary>
    /// The touch mode's left presses, from the mouse Godot makes of a touch or (with <c>--touch</c>) the mouse itself: on
    /// a header held <see cref="LongPressSeconds"/> the column's tip, released sooner a sort; on a row released a press of
    /// it (or of its Refill button); moved past <see cref="FingerScroll.Slop"/> anywhere a scroll that follows the finger, a
    /// flick at its release going on. A press while a flick goes on stops it and scrolls, never taps. Returns whether
    /// <paramref name="event"/> was taken (the tree's own handling then never sees it).
    /// </summary>
    private bool Touch(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion)
        {
            if (!_finger.Down)
                return false;
            if (_finger.Move(motion.Position))
                StartScroll();
            if (_finger.Dragging)
                ScrollTo(_scrollFrom - (motion.Position - _finger.Origin));
            return true;
        }
        if (@event is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
            return false;
        if (button.Pressed)
        {
            (_touchFrom, _headerLong, _scrolled) = (button.Position, false, false);
            _headerColumn = HeaderColumnAt(button.Position);
            (_touchUnit, _touchRefill) = _headerColumn >= 0 ? (0, 0) : (UnitAt(button.Position), RefillAt(button.Position));
            int press = ++_touchPresses;
            if (_finger.Press(button.Position))
                StartScroll();
            else
                LightRefill(_touchRefill);
            if (_headerColumn >= 0)
            {
                int column = _headerColumn;
                GetTree().CreateTimer(LongPressSeconds).Timeout += () =>
                {
                    if (press != _touchPresses || !_finger.Down || _headerColumn != column || !IsVisibleInTree())
                        return;
                    _headerLong = true;
                    ShowHeaderTip(column, _touchFrom.X);            // no vibration (owner, 2026-10-24)
                };
            }
            return true;
        }
        if (!_finger.Down)
            return false;
        _touchPresses++;
        LightRefill(0);
        if (_finger.Release(button.Position))
            _flingAt = ScrollValues;
        else if (_headerColumn >= 0)
        {
            if (!_headerLong && _headerColumn > 0)
                SortBy(_columns[_headerColumn - 1]);
        }
        else if (_touchRefill > 0)
            RefillPressed?.Invoke(_touchRefill);
        else if (_touchUnit > 0)
            RowPressed?.Invoke(_touchUnit);
        return true;
    }

    /// <summary>The press scrolls from here on: no tap, no long press, the hovered unit the one it came down on.</summary>
    private void StartScroll()
    {
        (_headerColumn, _scrollFrom) = (-1, ScrollValues);
        (_scrolled, _scrolledUnit) = (true, _touchUnit);
        LightRefill(0);
    }

    /// <summary>A flick going on: the table scrolls on at its speed as it slows, an axis stopping at the bar's end.</summary>
    public override void _Process(double delta)
    {
        if (!_finger.Flinging)
            return;
        var at = _flingAt - _finger.Step(delta);
        if (HScroll is not { } h || Clamp(h, ref at.X))
            _finger.StopX();
        if (VScroll is not { } v || Clamp(v, ref at.Y))
            _finger.StopY();
        _flingAt = at;
        ScrollTo(at);
    }

    /// <summary>Keeps <paramref name="value"/> within <paramref name="bar"/>'s range; returns whether it was past an end.</summary>
    private static bool Clamp(ScrollBar bar, ref float value)
    {
        float min = (float)bar.MinValue, max = End(bar);
        if (value >= min && value <= max)
            return false;
        value = Math.Clamp(value, min, max);
        return true;
    }

    /// <summary><paramref name="bar"/>'s farthest value: Godot's range ends a page before its maximum.</summary>
    private static float End(ScrollBar bar) => (float)Math.Max(bar.MinValue, bar.MaxValue - bar.Page);

    /// <summary>The shown scroll bars at <paramref name="values"/> (the bars keep them within their ranges).</summary>
    private void ScrollTo(Vector2 values)
    {
        if (HScroll is { } h)
            h.Value = values.X;
        if (VScroll is { } v)
            v.Value = values.Y;
    }

    /// <summary>The touch mode's lit Refill button: the one the finger is down on (0: none).</summary>
    private void LightRefill(int unit)
    {
        if (unit == _refillHovered)
            return;
        _refillHovered = unit;
        QueueRedraw();
    }

    /// <summary>The press, a flick and the scrolled row's hover end as the table hides.</summary>
    private void EndTouch()
    {
        _finger.Cancel();
        _scrolled = false;
        _touchPresses++;
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
    /// makes them (their text's height and the margins of <see cref="ApplyLedger"/>'s style boxes, the groups' strip among them).
    /// </summary>
    private float HeaderBottom => 1 + _titleFont.GetHeight(TitleSize) + 6 + GroupStrip;

    /// <summary>The visible horizontal scroll bar, if any.</summary>
    private HScrollBar? HScroll => GetChildren(includeInternal: true).OfType<HScrollBar>().FirstOrDefault(b => b.Visible);

    /// <summary>The visible vertical scroll bar, if any.</summary>
    private VScrollBar? VScroll => GetChildren(includeInternal: true).OfType<VScrollBar>().FirstOrDefault(b => b.Visible);

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
