using Godot;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.Strategy;

// Our Treasure screen on the phone held upright (docs/ui/phone-build.md, step P7; docs/ui/phone.md, §9, PT2): the title
// band under the hole, the selected entry's picture at the room's width (an item's words say what it does), its name and
// figures under it, Characters | Items (while a first pick waits, its prompt and Back in their place), then the picker's
// grid in rows of 6.7 mm, as many in view as the room holds, scrolled by a finger or the wheel (PickerGridScroll), a thin
// bar in the room's right margin, an item's name and its holder in one line; the touch mode's red button | End in the bar,
// the script's questions and refusals in a callout across the room over it (ShowCallout). Drawn in the frame's layout
// pixels over the whole screen (PhoneUnits), the places PhonePickersUpright's; the details, the button, the prompt, the
// tabs and the cells as held landscape draws them (TreasureView.Phone.cs).
internal sealed partial class TreasureView
{
    private readonly PickerGridScroll _grid = new();
    private readonly PickerMoreCells _more = new();      // the hint at filled cells out of view (the owner's 3)
    private int _gridView = -1;

    /// <summary>The button at <paramref name="at"/> (the screen's units): End, the red button, Back or a tab, or an entry's cell in view.</summary>
    private int UprightButtonAt(PhoneFrame f, Vector2 at)
    {
        bool In(PhoneFrame.Box box) => box.Contains(at.X, at.Y);
        if (In(PhonePickersUpright.End(f)))
            return EndButton;
        if (_button is not null && In(PhonePickersUpright.Action(f)))
            return ActionButton;
        if (_prompt is not null)
        {
            if (In(PhonePickersUpright.Back(f)))
                return BackButton;
        }
        else if (In(PhonePickersUpright.Tab(f, 0)))
            return TabCharacters;
        else if (In(PhonePickersUpright.Tab(f, 1)))
            return TabItems;
        int cell = PhonePickersUpright.TreasureGrid(f).CellAt(_grid.Scroll, at.X, at.Y);
        return _entries.Any(e => e.Number == cell) ? cell : NoButton;
    }

    private void DrawUpright(Control c, PhoneUnits u)
    {
        var f = u.Frame;
        var grid = PhonePickersUpright.TreasureGrid(f);
        if (_view != _gridView)                                 // the other view's entries: from its first row
        {
            _gridView = _view;
            _grid.Restart();
        }
        _grid.Reveal(grid, _selected);
        PhonePickerParts.DrawUprightTitle(c, u, _brush, _cinzel, "Treasure", _date);
        DrawPicture(c, u.Rect(PhonePickersUpright.Picture(f)));
        var (dx, dy) = PhonePickersUpright.Details(f);
        DrawDetails(c, u, dx / u.K, dy / u.K, (PhonePickersUpright.DetailsRight(f) - dx) / u.K);
        if (_prompt is { } prompt)
        {
            DrawPrompt(c, u, prompt, u.Rect(PhonePickersUpright.Prompt(f)));
            PhonePickerParts.DrawButton(c, u, u.Rect(PhonePickersUpright.Back(f)), _brush, "Back", !Busy && _hovered == BackButton);
        }
        else
            DrawTabs(c, u, view => u.Rect(PhonePickersUpright.Tab(f, view)));
        var listed = _entries.ToDictionary(e => e.Number);
        for (int n = 1; n <= TreasureRoster.Cells; n++)
        {
            if (grid.Cell(n, _grid.Scroll) is not { } cell)
                continue;
            if (listed.TryGetValue(n, out var entry))
                DrawEntryCell(c, u, u.Rect(cell), entry, n, upright: true);
            else
                PhonePickerParts.DrawEmptyCell(c, u.Rect(cell), _k);
        }
        PhonePickerParts.DrawScrollBar(c, u, grid, _grid.Scroll);
        _more.Draw(c, u, _cinzel, grid, _grid.Scroll, FilledCells);
        PhonePickerParts.DrawButton(c, u, u.Rect(PhonePickersUpright.End(f)), _brush, "End", !Busy && _hovered == EndButton);
        if (_button is { } button)
            DrawAction(c, u, button, u.Rect(PhonePickersUpright.Action(f)));
    }

    /// <summary>The cells an entry holds in the view shown: the hint's (<see cref="PickerMoreCells"/>).</summary>
    private int[] FilledCells => [.. _entries.Select(e => e.Number)];

    private bool GridTouch(InputEvent e) =>
        _phone is { } f && !Busy
        && (_more.Touch(e, cell => _grid.Show(PhonePickersUpright.TreasureGrid(f), cell), _face.QueueRedraw)
            || _grid.Touch(e, PhonePickersUpright.TreasureGrid(f), StartScroll, _face.QueueRedraw));

    /// <summary>The press scrolls from here on: no tap, no cell lit (the rows move under the finger).</summary>
    private void StartScroll()
    {
        _pressed = _hovered = NoButton;
        _face.QueueRedraw();
    }

    /// <summary>A flick going on scrolls the rows on.</summary>
    public override void _Process(double delta) =>
        _grid.Process(delta, Visible && _phone is { IsUpright: true } f ? PhonePickersUpright.TreasureGrid(f) : null, _face.QueueRedraw);
}
