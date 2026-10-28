using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The touch mode's lists of saves (owner, 2026-10-26; docs/ui/touch.md, "The lists scrolled by a finger"): a list scrolls
// by the wheel alone, which a tablet has not, so a finger dragged over it scrolls it a row for every row's height it
// moves, a flick going on after the release (FingerScroll). A row's press stays a tap at the release, as a click is.
internal sealed partial class SaveLoadView
{
    private readonly FingerScroll _finger = new();
    private int _scrollFrom;            // the first row in view when the finger's scroll started
    private float _flingAt;             // where a flick has scrolled the rows to, in layout pixels (a row: Pitch)

    /// <summary>The first save of the list shown in view, and the farthest it goes (unattended play).</summary>
    public int ListScroll => _scroll;

    public int ListScrollEnd => MaxScroll;

    /// <summary>Whether a flick still scrolls the list (unattended play).</summary>
    public bool Flinging => _finger.Flinging;

    /// <summary>The list's rows in global coordinates, below New save (unattended play).</summary>
    public Rect2 ListRowsArea
    {
        get
        {
            var (top, bottom) = _phone is { } f ? PhoneRowsArea(f) : (new Vector2(ListX, RowsTop), new Vector2(ListX + RowW, ListBottom));
            (top, bottom) = (Global(top), Global(bottom));
            return new Rect2(top, bottom - top);
        }
    }

    /// <summary>The phone's rows in layout pixels, below New save: the first row's top left, the groove's foot at the row's right.</summary>
    private (Vector2, Vector2) PhoneRowsArea(PhoneFrame f)
    {
        var row = Phone.Rect(PhoneSaves.Row(f, 0, NewRowShown));
        var groove = Phone.Rect(PhoneSaves.ScrollBar(f, NewRowShown));
        return (row.Position, new Vector2(row.End.X, groove.End.Y));
    }

    /// <summary>Whether the list shown takes a finger's scroll now: no question open, no comment's card up.</summary>
    private bool ListScrolls => !_confirm && !_sweep && !(_editing && _phone is not null);

    /// <summary>The list's column in layout pixels: New save and the rows.</summary>
    private Rect2 ListArea => _phone is { } f ? Phone.Rect(PhoneSaves.Scrolled(f, NewRowShown))
        : new(ListX, ListTop, ListW, ListBottom - ListTop);

    /// <summary>
    /// The touch mode's finger over the list: a press there is watched — moved past <see cref="FingerScroll.Slop"/>
    /// it scrolls the rows (no tap), a row for each <see cref="Pitch"/> the finger moves, and its quick release
    /// flicks them on; a press while a flick goes on stops it. Returns whether <paramref name="e"/> was taken; a press, and
    /// the release of one that did not scroll, go on to the screen's own handling (a tap acts at the release).
    /// </summary>
    private bool ListTouch(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseMotion motion when _finger.Down:
                if (_finger.Move(motion.Position))
                    StartScroll();
                if (_finger.Dragging)
                    SetScroll(_scrollFrom - (int)MathF.Round((motion.Position.Y - _finger.Origin.Y) / (Pitch * _k)));
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                if (ListScrolls && ListArea.HasPoint(press.Position / _k) && _finger.Press(press.Position))
                    StartScroll();                                      // it stopped a flick: it scrolls, never taps
                return false;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _finger.Down:
                if (!_finger.Release(release.Position))
                    return false;
                _flingAt = _scroll * Pitch;
                _pressed = 0;
                QueueRedraw();
                return true;
        }
        return false;
    }

    /// <summary>The press scrolls from here on: no tap, no row lit (the rows move under the finger).</summary>
    private void StartScroll()
    {
        _scrollFrom = _scroll;
        _pressed = _hovered = 0;
        QueueRedraw();
    }

    /// <summary>A flick going on: the rows scroll on at its speed as it slows, stopping at either end of the list.</summary>
    public override void _Process(double delta)
    {
        FollowKeyboard(delta);
        if (!_finger.Flinging)
            return;
        if (_answer is null || !ListScrolls)
        {
            _finger.Cancel();
            return;
        }
        _finger.StopX();
        _flingAt -= _finger.Step(delta).Y / _k;
        float end = MaxScroll * Pitch;
        if (_flingAt <= 0 || _flingAt >= end)
        {
            _flingAt = Math.Clamp(_flingAt, 0, end);
            _finger.StopY();
        }
        SetScroll((int)MathF.Round(_flingAt / Pitch));
    }
}
