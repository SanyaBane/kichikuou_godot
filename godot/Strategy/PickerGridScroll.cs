using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The scroll of a picker's grid on the phone held upright (docs/ui/phone-build.md, step P7; PH2, PT2), the Harem's and
/// Treasure's: by whole rows, under a finger as the save list's (<c>SaveLoadView.Touch.cs</c>) — moved past
/// <see cref="FingerScroll.Slop"/> a press scrolls the rows (no tap), a row for each row's pitch it moves, and its quick
/// release flicks them on; a press while a flick goes on stops it — or the wheel, a row a notch; an entry newly chosen out
/// of view scrolls the grid the least that shows it. The grid's places are <see cref="PhonePickersPortrait.ScrolledGrid"/>'s,
/// in the view's units.
/// </summary>
internal sealed class PickerGridScroll
{
    private readonly FingerScroll _finger = new();
    private int _from, _revealed;
    private float _flingAt;

    /// <summary>The rows scrolled past.</summary>
    public int Scroll { get; private set; }

    /// <summary>The grid shows other entries (the treasure's other view): from its first row, the choice to be revealed again.</summary>
    public void Restart()
    {
        Scroll = 0;
        _revealed = 0;
        _finger.Cancel();
    }

    /// <summary>The grid scrolled the least that shows cell <paramref name="number"/> (the hint's pill pressed), a flick stopped.</summary>
    public void Show(PhonePickersPortrait.ScrolledGrid grid, int number)
    {
        _finger.Cancel();
        Scroll = grid.Reveal(number, Scroll);
    }

    /// <summary>Entry <paramref name="chosen"/> (a cell's number, 0 none) newly chosen out of view is scrolled into it; the scroll kept within <paramref name="grid"/>.</summary>
    public void Reveal(PhonePickersPortrait.ScrolledGrid grid, int chosen)
    {
        if (chosen != 0 && chosen != _revealed)
        {
            _revealed = chosen;
            Scroll = grid.Reveal(chosen, Scroll);
        }
        Scroll = Math.Clamp(Scroll, 0, grid.MaxScroll);
    }

    /// <summary>
    /// The finger or the wheel over <paramref name="grid"/>: returns whether <paramref name="e"/> was taken; a press, and the
    /// release of one that did not scroll, go on to the view's own handling (a tap acts at the release).
    /// <paramref name="started"/> is told when a press starts scrolling (the view drops its pressed and lit cells),
    /// <paramref name="changed"/> when the rows move.
    /// </summary>
    public bool Touch(InputEvent e, PhonePickersPortrait.ScrolledGrid grid, Action started, Action changed)
    {
        var area = new Rect2(grid.Scrolled.X, grid.Scrolled.Y, grid.Scrolled.W, grid.Scrolled.H);
        switch (e)
        {
            case InputEventMouseMotion motion when _finger.Down:
                if (_finger.Move(motion.Position))
                    Start(started);
                if (_finger.Dragging)
                    Set(_from - (int)MathF.Round((motion.Position.Y - _finger.Origin.Y) / grid.Pitch), grid, changed);
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                if (area.HasPoint(press.Position) && _finger.Press(press.Position))
                    Start(started);                                     // it stopped a flick: it scrolls, never taps
                return false;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _finger.Down:
                if (!_finger.Release(release.Position))
                    return false;
                _flingAt = Scroll * grid.Pitch;
                started();
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel
                when area.HasPoint(wheel.Position):
                Set(Scroll + (wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1), grid, changed);
                return true;
        }
        return false;
    }

    /// <summary>A frame of a flick going on over <paramref name="grid"/> (null: the grid is gone, the flick stops); the rows slow to a stop at either end.</summary>
    public void Process(double delta, PhonePickersPortrait.ScrolledGrid? grid, Action changed)
    {
        if (!_finger.Flinging)
            return;
        if (grid is null)
        {
            _finger.Cancel();
            return;
        }
        _finger.StopX();
        _flingAt -= _finger.Step(delta).Y;
        float end = grid.MaxScroll * grid.Pitch;
        if (_flingAt <= 0 || _flingAt >= end)
        {
            _flingAt = Math.Clamp(_flingAt, 0, end);
            _finger.StopY();
        }
        Set((int)MathF.Round(_flingAt / grid.Pitch), grid, changed);
    }

    private void Start(Action started)
    {
        _from = Scroll;
        started();
    }

    private void Set(int scroll, PhonePickersPortrait.ScrolledGrid grid, Action changed)
    {
        scroll = Math.Clamp(scroll, 0, grid.MaxScroll);
        if (scroll == Scroll)
            return;
        Scroll = scroll;
        changed();
    }
}
