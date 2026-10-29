using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// Our Harem screen on the phone held upright (docs/ui/phone-build.md, step P7; docs/ui/phone.md, §9, PH2): the title band
// under the hole, her picture at the room's width, her name, visits and last summons under it, then the picker's grid in
// rows of 7.3 mm, as many in view as the room holds, scrolled by a finger's drag (a flick goes on, FingerScroll) or the
// wheel, a thin bar in the room's right margin; End | Summon in the bar. A girl chosen out of view scrolls the grid the
// least that shows her. Drawn in the frame's layout pixels over the whole screen (PhoneUnits), the places
// PhonePickersUpright's; her details, her cells and Summon as held landscape draws them (HaremView.Phone.cs).
internal sealed partial class HaremView
{
    private readonly FingerScroll _finger = new();
    private int _scroll, _scrollFrom, _revealed;
    private float _flingAt;

    /// <summary>The button at <paramref name="at"/> (the screen's units): Summon, End, or a girl's cell in view.</summary>
    private int UprightButtonAt(PhoneFrame f, Vector2 at)
    {
        if (PhonePickersUpright.Action(f).Contains(at.X, at.Y))
            return SummonButton;
        if (PhonePickersUpright.End(f).Contains(at.X, at.Y))
            return EndButton;
        int cell = PhonePickersUpright.HaremGrid(f).CellAt(_scroll, at.X, at.Y);
        return _girls.Any(g => g.Number == cell) ? cell : NoButton;
    }

    private void DrawUpright(Control c, PhoneUnits u)
    {
        var f = u.Frame;
        var grid = PhonePickersUpright.HaremGrid(f);
        RevealChosen(grid);
        PhonePickerParts.DrawUprightTitle(c, u, _brush, _cinzel, "Harem", _date);
        DrawPicture(c, u.Rect(PhonePickersUpright.Picture(f)));
        var (dx, dy) = PhonePickersUpright.Details(f);
        DrawDetails(c, u, dx / u.K, dy / u.K, PhonePickersUpright.DetailsRight(f) / u.K);
        var offered = _girls.ToDictionary(g => g.Number);
        for (int n = 1; n <= Cells; n++)
        {
            if (grid.Cell(n, _scroll) is not { } cell)
                continue;
            if (offered.TryGetValue(n, out var girl))
                DrawGirlCell(c, u, u.Rect(cell), girl, PhonePickersUpright.CellHeartIn, PhonePickersUpright.CellNameIn);
            else
                PhonePickerParts.DrawEmptyCell(c, u.Rect(cell), _k);
        }
        PhonePickerParts.DrawScrollBar(c, u, grid, _scroll);
        bool hoverEnd = !Busy && _hovered == EndButton;
        PhonePickerParts.DrawButton(c, u, u.Rect(PhonePickersUpright.End(f)), _brush, "End", hoverEnd);
        DrawSummon(c, u, u.Rect(PhonePickersUpright.Action(f)));
    }

    /// <summary>A girl newly chosen out of view scrolls the grid the least that shows her; the scroll kept within the grid.</summary>
    private void RevealChosen(PhonePickersUpright.ScrolledGrid grid)
    {
        if (_chosen != 0 && _chosen != _revealed)
        {
            _revealed = _chosen;
            _scroll = grid.Reveal(_chosen, _scroll);
        }
        _scroll = Math.Clamp(_scroll, 0, grid.MaxScroll);
    }

    /// <summary>
    /// The finger over the grid (as the save list's, <c>SaveLoadView.Touch.cs</c>): moved past <see cref="FingerScroll.Slop"/>
    /// it scrolls the rows (no tap), a row for each row's pitch it moves, and its quick release flicks them on; a press while a
    /// flick goes on stops it; the wheel scrolls a row a notch. Returns whether <paramref name="e"/> was taken; a press, and
    /// the release of one that did not scroll, go on to the screen's own handling (a tap chooses at the release).
    /// </summary>
    private bool GridTouch(InputEvent e)
    {
        if (_phone is not { } f || Busy)
            return false;
        var grid = PhonePickersUpright.HaremGrid(f);
        var area = new Rect2(grid.Area.X, grid.Area.Y, grid.Area.W, grid.Area.H);
        switch (e)
        {
            case InputEventMouseMotion motion when _finger.Down:
                if (_finger.Move(motion.Position))
                    StartScroll();
                if (_finger.Dragging)
                    SetScroll(_scrollFrom - (int)MathF.Round((motion.Position.Y - _finger.Origin.Y) / grid.Pitch), grid);
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } press:
                if (area.HasPoint(press.Position) && _finger.Press(press.Position))
                    StartScroll();                                      // it stopped a flick: it scrolls, never taps
                return false;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false } release when _finger.Down:
                if (!_finger.Release(release.Position))
                    return false;
                _flingAt = _scroll * grid.Pitch;
                _pressed = NoButton;
                _face.QueueRedraw();
                return true;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp or MouseButton.WheelDown, Pressed: true } wheel
                when area.HasPoint(wheel.Position):
                SetScroll(_scroll + (wheel.ButtonIndex == MouseButton.WheelDown ? 1 : -1), grid);
                return true;
        }
        return false;
    }

    /// <summary>The press scrolls from here on: no tap, no cell lit (the rows move under the finger).</summary>
    private void StartScroll()
    {
        _scrollFrom = _scroll;
        _pressed = _hovered = NoButton;
        _face.QueueRedraw();
    }

    private void SetScroll(int scroll, PhonePickersUpright.ScrolledGrid grid)
    {
        scroll = Math.Clamp(scroll, 0, grid.MaxScroll);
        if (scroll == _scroll)
            return;
        _scroll = scroll;
        _face.QueueRedraw();
    }

    /// <summary>A flick going on: the rows scroll on at its speed as it slows, stopping at either end of the grid.</summary>
    public override void _Process(double delta)
    {
        if (!_finger.Flinging)
            return;
        if (!Visible || _phone is not { IsUpright: true } f)
        {
            _finger.Cancel();
            return;
        }
        var grid = PhonePickersUpright.HaremGrid(f);
        _finger.StopX();
        _flingAt -= _finger.Step(delta).Y;
        float end = grid.MaxScroll * grid.Pitch;
        if (_flingAt <= 0 || _flingAt >= end)
        {
            _flingAt = Math.Clamp(_flingAt, 0, end);
            _finger.StopY();
        }
        SetScroll((int)MathF.Round(_flingAt / grid.Pitch), grid);
    }
}
