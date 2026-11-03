using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// Every tab in one column, scrolled, on every device (owner, 2026-11-03; docs/ui/options.md, "Frame rate"): on the PC and
// the tablet the 1280×800 sheet between the tabs and its foot (Defaults, Close), held landscape the sheet over Defaults |
// Close (PhoneOptions.View), held upright the sheet down to the bar (PhoneOptionsUpright.View). What scrolls past the view
// is cut at its edges: the item's clip is the column's view, the frame, the backdrop, the tabs, the foot, the bar and a
// question drawn outside it (clip ignored), the preview's 121, a child, cut with the column. The reference scroll bar at
// the sheet's right while the tab scrolls (the layout's 8 px on the PC and the tablet, the phone's elsewhere, on the
// paper). The wheel scrolls it over the view, a finger in the touch mode (FingerScroll, as the save list's: a quick release
// flicks it on), the mouse on the PC by the bar's thumb as well.
internal sealed partial class OptionsView
{
    private readonly FingerScroll _finger = new();
    private float _scroll, _scrollFrom;                         // the column's scroll (canvas units), and where a drag took it
    private readonly Dictionary<Tab, float> _columnHeight = [];  // each tab's column as last drawn (canvas units)
    private float? _thumbGrab;                                  // the PC's thumb held by the mouse: how far down it (canvas units)

    /// <summary>
    /// The PC's view, in the 1280×800 layout: the sheet's inside from under the tabs to over its foot; the least room under
    /// the column's last line at its end; the bar's right in from the sheet's; a notch of the wheel.
    /// </summary>
    private static readonly Rect2 PcView = new(Sheet.Position.X + 1, Sheet.Position.Y + 2, Sheet.Size.X - 2, CloseButton.Position.Y - 12 - (Sheet.Position.Y + 2));

    private const float PcColumnFoot = 14, PcBarIn = 8, PcWheel = 40;

    /// <summary>A notch of the wheel over the phone's column, in reference pixels.</summary>
    private const float PhoneWheel = 120;

    /// <summary>A box of the canvas's units as a rectangle.</summary>
    private static Rect2 Rect(PhoneFrame.Box box) => new(box.X, box.Y, box.W, box.H);

    /// <summary>Where the column is seen, in the canvas's units.</summary>
    private Rect2 ViewBox() => _phone switch
    {
        { IsUpright: true } f => Rect(PhoneOptionsUpright.View(f)),
        { } f => Rect(PhoneOptions.View(f)),
        _ => new Rect2(PcView.Position * _k, PcView.Size * _k),
    };

    /// <summary>The column's clip, in the item's own units (the canvas's): the sheet's width, the view's height.</summary>
    private Rect2 ClipBox()
    {
        var view = ViewBox();
        var sheet = _phone switch
        {
            { IsUpright: true } f => Rect(PhoneOptionsUpright.Sheet(f)),
            { } f => Rect(PhoneOptions.Sheet(f)),
            _ => new Rect2(Sheet.Position * _k, Sheet.Size * _k),
        };
        return new Rect2(sheet.Position.X, view.Position.Y, sheet.Size.X, view.Size.Y);
    }

    /// <summary>The column's left, its top at the scroll and its width, in the parts' units (no height: it is as long as its tab).</summary>
    private (float X, float Top, float W) ColumnPlace()
    {
        switch (_phone)
        {
            case { IsUpright: true } f:
                var view = PhoneOptionsUpright.View(f);
                return (view.X / _k, PhoneOptionsUpright.ColumnTop(f, _scroll) / _k, view.W / _k);
            case { } f:
                var across = PhoneOptions.Across(f);
                return (across.X / _k, PhoneOptions.ColumnTop(f, _scroll) / _k, across.W / _k);
            default:
                return (ContentX, ContentY - _scroll / _k, ContentW);
        }
    }

    /// <summary>How far the tab shown scrolls (canvas units), its column as last drawn.</summary>
    private float MaxScroll()
    {
        float height = _columnHeight.GetValueOrDefault(_tab);
        return _phone switch
        {
            { IsUpright: true } f => PhoneOptionsUpright.MaxScroll(f, height),
            { } f => PhoneOptions.MaxScroll(f, height),
            _ => Math.Max(0, ContentY * _k + height - (PcView.End.Y - PcColumnFoot) * _k),
        };
    }

    /// <summary>The scroll bar's groove and thumb and its rim, in the parts' units; null while the tab fits.</summary>
    private (Rect2 Groove, Rect2 Thumb, float Rim)? ScrollBarParts()
    {
        float height = _columnHeight.GetValueOrDefault(_tab);
        var bar = _phone switch
        {
            { IsUpright: true } f => PhoneOptionsUpright.ScrollBar(f, height, _scroll),
            { } f => PhoneOptions.ScrollBar(f, height, _scroll),
            _ => null,
        };
        if (_phone is { } phone)
            return bar is { } b ? (Part(b.Track), Part(b.Thumb), phone.Px(PhoneScrollBar.Rim) / _k) : null;
        float max = MaxScroll();
        if (max <= 0)
            return null;
        var groove = LayoutScrollBar.Groove(PcView.End.X - PcBarIn, PcView.Position.Y + 8, PcView.Size.Y - 12);
        var thumb = LayoutScrollBar.Thumb(groove, (height - max) / height, _scroll / max);
        return (new Rect2(groove.X, groove.Y, groove.W, groove.H), new Rect2(thumb.X, thumb.Y, thumb.W, thumb.H), 1 / _k);
    }

    /// <summary>
    /// The tab's column through the view, clipped to it, its height kept for the scroll, the spots scrolled out of it gone;
    /// then the bar. The drawing before it ignores the clip (<see cref="BeginClip"/>), and so does the drawing after it.
    /// </summary>
    private void DrawColumn(Func<float, float, float, float> draw)
    {
        var item = GetCanvasItem();
        var clip = ClipBox();
        RenderingServer.CanvasItemAddClipIgnore(item, false);
        int first = _spots.Count;
        var (x, top, w) = ColumnPlace();
        float foot = draw(x, top, w);
        _columnHeight[_tab] = (foot - top) * _k;
        ClipSpots(first, new Rect2(clip.Position / _k, clip.Size / _k));
        RenderingServer.CanvasItemAddClipIgnore(item, true);
        if (ScrollBarParts() is { } bar)
            Ui.DrawScrollBar(this, bar.Groove, bar.Thumb, bar.Rim, paper: true);
        if (_atEnd && Math.Abs(_scroll - MaxScroll()) > 0.01f)
            Callable.From(() => SetScroll(MaxScroll())).CallDeferred();
        else if (_scroll > MaxScroll())                // the column is shorter than when it was scrolled (a font, a turn)
            Callable.From(() => SetScroll(_scroll)).CallDeferred();
    }

    private bool _atEnd;

    /// <summary>The tab's column held at its end whatever it is laid out as. Public for the looks (the screen gallery).</summary>
    public void ScrollToEnd()
    {
        _atEnd = true;
        QueueRedraw();
    }

    /// <summary>The item clipped to the column's view, all drawn from here on ignoring the clip until <see cref="DrawColumn"/>.</summary>
    private void BeginClip()
    {
        var item = GetCanvasItem();
        RenderingServer.CanvasItemSetCustomRect(item, true, ClipBox());   // the Control sets its own again at each drawing
        RenderingServer.CanvasItemSetClip(item, true);
        RenderingServer.CanvasItemAddClipIgnore(item, true);
    }

    /// <summary>The column's spots from <paramref name="first"/> on cut to <paramref name="clip"/> (the parts' units): one scrolled out of it is gone.</summary>
    private void ClipSpots(int first, Rect2 clip)
    {
        for (int i = _spots.Count - 1; i >= first; i--)
        {
            if (clip.Intersects(_spots[i].Rect))
                _spots[i] = _spots[i] with { Rect = clip.Intersection(_spots[i].Rect) };
            else
                _spots.RemoveAt(i);
        }
    }

    // ---------------------------------------------------------------- the wheel, the finger and the PC's thumb

    /// <summary>
    /// The wheel over the view on every device, a finger in the touch mode, the mouse on the PC's thumb: returns whether
    /// <paramref name="e"/> was taken.
    /// </summary>
    private bool ScrollInput(InputEvent e)
    {
        var view = ViewBox();
        if (e is InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel
            && view.HasPoint(wheel.Position))
        {
            float notch = _phone is { } f ? f.Px(PhoneWheel) : PcWheel * _k;
            SetScroll(_scroll + (wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1) * notch);
            return true;
        }
        return TouchMode.On ? FingerTouch(e, view) : ThumbDrag(e);
    }

    /// <summary>
    /// The finger over the column: a press goes on to the spots (a tap acts at its release, a slider's press drags it);
    /// moved past the slop it scrolls the column instead, and its quick release flicks it on; a press while a flick goes on
    /// stops it, never tapping.
    /// </summary>
    private bool FingerTouch(InputEvent e, Rect2 view)
    {
        switch (e)
        {
            case InputEventMouseMotion motion when _finger.Down:
                if (_finger.Move(motion.Position))
                    StartScroll();
                if (!_finger.Dragging)
                    return false;
                SetScroll(_scrollFrom - (motion.Position.Y - _finger.Origin.Y));
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                if (!view.HasPoint(press.Position) || SpotAt(press.Position / _k)?.Slider is not null)
                    return false;
                if (!_finger.Press(press.Position))
                    return false;
                StartScroll();                              // it stopped a flick: it scrolls from here, never taps
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _finger.Down:
                if (!_finger.Release(release.Position))
                    return false;
                _pressed = null;
                QueueRedraw();
                return true;
        }
        return false;
    }

    /// <summary>
    /// The mouse on the PC's bar: a press on the groove takes the thumb (centred under the pointer when it fell beside it),
    /// a move drags it, the release lets it go.
    /// </summary>
    private bool ThumbDrag(InputEvent e)
    {
        switch (e)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                if (ScrollBarParts() is not { } bar || !Scaled(bar.Groove).Grow(4 * _k).HasPoint(press.Position))
                    return false;
                var thumb = Scaled(bar.Thumb);
                if (press.Position.Y < thumb.Position.Y || press.Position.Y > thumb.End.Y)
                {
                    _thumbGrab = thumb.Size.Y / 2;
                    MoveThumb(press.Position.Y);
                }
                else
                {
                    _thumbGrab = press.Position.Y - thumb.Position.Y;
                }
                return true;
            case InputEventMouseMotion motion when _thumbGrab is not null:
                MoveThumb(motion.Position.Y);
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } when _thumbGrab is not null:
                _thumbGrab = null;
                return true;
        }
        return false;
    }

    /// <summary>The thumb's top where the pointer at <paramref name="y"/> holds it (canvas units): the column scrolled to match.</summary>
    private void MoveThumb(float y)
    {
        if (ScrollBarParts() is not { } bar || _thumbGrab is not { } grab)
            return;
        var groove = Scaled(bar.Groove);
        float inset = LayoutScrollBar.ThumbIn * _k, travel = groove.Size.Y - 2 * inset - Scaled(bar.Thumb).Size.Y;
        if (travel > 0)
            SetScroll((y - grab - groove.Position.Y - inset) / travel * MaxScroll());
    }

    /// <summary>A rectangle of the parts' units in the canvas's.</summary>
    private Rect2 Scaled(Rect2 parts) => new(parts.Position * _k, parts.Size * _k);

    private void StartScroll()
    {
        _scrollFrom = _scroll;
        _pressed = null;
        _hover = null;
        QueueRedraw();
    }

    /// <summary>The column scrolled to <paramref name="scroll"/>, kept within its ends; the preview follows it.</summary>
    private void SetScroll(float scroll)
    {
        scroll = Math.Clamp(scroll, 0, MaxScroll());
        if (Math.Abs(scroll - _scroll) < 0.01f)
            return;
        _scroll = scroll;
        PlaceChildren();
        QueueRedraw();
    }

    /// <summary>A frame of a flick going on; the column slows to a stop at either end.</summary>
    private void Fling(double delta)
    {
        _finger.StopX();
        float max = MaxScroll(), scroll = _scroll - _finger.Step(delta).Y;
        if (scroll <= 0 || scroll >= max)
        {
            scroll = Math.Clamp(scroll, 0, max);
            _finger.StopY();
        }
        SetScroll(scroll);
    }
}
