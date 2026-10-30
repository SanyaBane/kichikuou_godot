using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// Our Harem screen on the phone held upright (docs/ui/phone-build.md, step P7; docs/ui/phone.md, §9, PH2): the title band
// under the hole, her picture at the room's width, her name, visits and last summons under it, then the picker's grid in
// rows of 7.3 mm, as many in view as the room holds, scrolled by a finger's drag (a flick goes on) or the wheel
// (PickerGridScroll), a thin bar in the room's right margin; Summon | End in the bar. A girl chosen out of view scrolls the grid the
// least that shows her. Drawn in the frame's layout pixels over the whole screen (PhoneUnits), the places
// PhonePickersUpright's; her details, her cells and Summon as held landscape draws them (HaremView.Phone.cs).
internal sealed partial class HaremView
{
    private readonly PickerGridScroll _grid = new();

    /// <summary>The button at <paramref name="at"/> (the screen's units): Summon, End, or a girl's cell in view.</summary>
    private int UprightButtonAt(PhoneFrame f, Vector2 at)
    {
        if (PhonePickersUpright.Action(f).Contains(at.X, at.Y))
            return SummonButton;
        if (PhonePickersUpright.End(f).Contains(at.X, at.Y))
            return EndButton;
        int cell = PhonePickersUpright.HaremGrid(f).CellAt(_grid.Scroll, at.X, at.Y);
        return _girls.Any(g => g.Number == cell) ? cell : NoButton;
    }

    private void DrawUpright(Control c, PhoneUnits u)
    {
        var f = u.Frame;
        var grid = PhonePickersUpright.HaremGrid(f);
        _grid.Reveal(grid, _chosen);
        PhonePickerParts.DrawUprightTitle(c, u, _brush, _cinzel, "Harem", _date);
        DrawPicture(c, u.Rect(PhonePickersUpright.Picture(f)));
        var (dx, dy) = PhonePickersUpright.Details(f);
        DrawDetails(c, u, dx / u.K, dy / u.K, PhonePickersUpright.DetailsRight(f) / u.K);
        var offered = _girls.ToDictionary(g => g.Number);
        for (int n = 1; n <= Cells; n++)
        {
            if (grid.Cell(n, _grid.Scroll) is not { } cell)
                continue;
            if (offered.TryGetValue(n, out var girl))
                DrawGirlCell(c, u, u.Rect(cell), girl, PhonePickersUpright.CellHeartIn, PhonePickersUpright.CellNameIn);
            else
                PhonePickerParts.DrawEmptyCell(c, u.Rect(cell), _k);
        }
        PhonePickerParts.DrawScrollBar(c, u, grid, _grid.Scroll);
        bool hoverEnd = !Busy && _hovered == EndButton;
        PhonePickerParts.DrawButton(c, u, u.Rect(PhonePickersUpright.End(f)), _brush, "End", hoverEnd);
        DrawSummon(c, u, u.Rect(PhonePickersUpright.Action(f)));
    }

    private bool GridTouch(InputEvent e) =>
        _phone is { } f && !Busy && _grid.Touch(e, PhonePickersUpright.HaremGrid(f), StartScroll, _face.QueueRedraw);

    /// <summary>The press scrolls from here on: no tap, no cell lit (the rows move under the finger).</summary>
    private void StartScroll()
    {
        _pressed = _hovered = NoButton;
        _face.QueueRedraw();
    }

    /// <summary>A flick going on scrolls the rows on.</summary>
    public override void _Process(double delta) =>
        _grid.Process(delta, Visible && _phone is { IsUpright: true } f ? PhonePickersUpright.HaremGrid(f) : null, _face.QueueRedraw);
}
