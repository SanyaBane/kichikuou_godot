using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// Our Harem screen on the phone held upright (docs/ui/phone-build.md, step P7; docs/ui/phone.md, §9, PH2): the title band
// under the hole, her picture at the room's width, her name, visits and last summons under it, then the picker's grid in
// rows of 7.3 mm, as many in view as the room holds, scrolled by a finger's drag (a flick goes on) or the wheel
// (PickerGridScroll), a thin bar in the room's right margin; Summon | End in the bar. A girl chosen out of view scrolls the grid the
// least that shows her. Drawn in the frame's layout pixels over the whole screen (PhoneUnits), the places
// PhonePickersPortrait's; her details, her cells and Summon as held landscape draws them (HaremView.Phone.cs).
internal sealed partial class HaremView
{
    private readonly PickerGridScroll _grid = new();
    private readonly PickerMoreCells _more = new();      // the hint at filled cells out of view (the owner's 3)

    /// <summary>The button at <paramref name="at"/> (the screen's units): Summon, End, or a girl's cell in view.</summary>
    private int PortraitButtonAt(PhoneFrame f, Vector2 at)
    {
        if (PhonePickersPortrait.Action(f).Contains(at.X, at.Y))
            return SummonButton;
        if (PhonePickersPortrait.End(f).Contains(at.X, at.Y))
            return EndButton;
        int cell = GridOf(f).CellAt(_grid.Scroll, at.X, at.Y);
        return _girls.Any(g => g.Number == cell) ? cell : NoButton;
    }

    private void DrawPortrait(Control c, PhoneUnits u)
    {
        var f = u.Frame;
        var grid = ScrolledGrid(f);
        PhonePickerParts.DrawPortraitTitle(c, u, _brush, _cinzel, "Harem", _date);
        DrawPicture(c, u.Rect(PhonePickersPortrait.Picture(f)));
        var (dx, dy) = PhonePickersPortrait.Details(f);
        DrawDetails(c, u, dx / u.K, dy / u.K, PhonePickersPortrait.DetailsRight(f) / u.K);
        DrawScrolledGrid(c, u, grid, PhonePickersPortrait.CellHeartIn, PhonePickersPortrait.CellNameIn, twoLines: false);
        var end = u.Rect(PhonePickersPortrait.End(f));
        _buttonSize = ButtonSize(u, PhonePickersPortrait.ButtonText, end, u.Rect(PhonePickersPortrait.Action(f)));
        bool hoverEnd = !Busy && _hovered == EndButton;
        PhonePickerParts.DrawButton(c, u, end, "Close", hoverEnd, _buttonSize);
        DrawSummon(c, u, u.Rect(PhonePickersPortrait.Action(f)));
    }

    /// <summary>The grid as the phone is held: upright under her details, held landscape five cells a row at the right (P40).</summary>
    private static PhonePickersPortrait.ScrolledGrid GridOf(PhoneFrame f) =>
        f.IsPortrait ? PhonePickersPortrait.HaremGrid(f) : PhonePickers.Grid(f);

    /// <summary>The grid for this drawing, a girl newly chosen out of view scrolled into it.</summary>
    private PhonePickersPortrait.ScrolledGrid ScrolledGrid(PhoneFrame f)
    {
        var grid = GridOf(f);
        _grid.Reveal(grid, _chosen);
        return grid;
    }

    /// <summary>
    /// The rows in view: each girl's cell (her heart <paramref name="heartIn"/> and her name <paramref name="nameIn"/> in,
    /// <paramref name="twoLines"/> as <see cref="DrawGirlCell"/>'s), the other cells empty; the scroll bar with its ticks and
    /// the pills at filled cells out of view.
    /// </summary>
    private void DrawScrolledGrid(Control c, PhoneUnits u, PhonePickersPortrait.ScrolledGrid grid, float heartIn, float nameIn, bool twoLines)
    {
        var offered = _girls.ToDictionary(g => g.Number);
        for (int n = 1; n <= Cells; n++)
        {
            if (grid.Cell(n, _grid.Scroll) is not { } cell)
                continue;
            if (offered.TryGetValue(n, out var girl))
                DrawGirlCell(c, u, u.Rect(cell), girl, heartIn, nameIn, twoLines);
            else
                PhonePickerParts.DrawEmptyCell(c, u.Rect(cell), _k);
        }
        PhonePickerParts.DrawScrollBar(c, u, grid, _grid.Scroll);
        _more.Draw(c, u, _cinzel, grid, _grid.Scroll, FilledCells);
    }

    /// <summary>The cells a girl holds: the hint's (<see cref="PickerMoreCells"/>).</summary>
    private int[] FilledCells => [.. _girls.Select(g => g.Number)];

    private bool GridTouch(InputEvent e) =>
        _phone is { } f && !Busy
        && (_more.Touch(e, cell => _grid.Show(GridOf(f), cell), _face.QueueRedraw)
            || _grid.Touch(e, GridOf(f), StartScroll, _face.QueueRedraw));

    /// <summary>The grid scrolled the least that shows cell <paramref name="number"/> (the screen gallery's scrolled look).</summary>
    public void ShowCell(int number)
    {
        if (_phone is { } f)
            _grid.Show(GridOf(f), number);
        _face.QueueRedraw();
    }

    /// <summary>The press scrolls from here on: no tap, no cell lit (the rows move under the finger).</summary>
    private void StartScroll()
    {
        _pressed = _hovered = NoButton;
        _face.QueueRedraw();
    }

    /// <summary>A flick going on scrolls the rows on.</summary>
    public override void _Process(double delta) =>
        _grid.Process(delta, Visible && _phone is { } f ? GridOf(f) : null, _face.QueueRedraw);
}
