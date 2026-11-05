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
// pixels over the whole screen (PhoneUnits), the places PhonePickersPortrait's; the details, the button, the prompt, the
// tabs and the cells as held landscape draws them (TreasureView.Phone.cs).
internal sealed partial class TreasureView
{
    private readonly PickerGridScroll _grid = new();
    private readonly PickerMoreCells _more = new();      // the hint at filled cells out of view (the owner's 3)
    private int _gridView = -1;

    /// <summary>The button at <paramref name="at"/> (the screen's units): End, the red button, Back or a tab, or an entry's cell in view.</summary>
    private int PortraitButtonAt(PhoneFrame f, Vector2 at)
    {
        bool In(PhoneFrame.Box box) => box.Contains(at.X, at.Y);
        if (In(PhonePickersPortrait.End(f)))
            return EndButton;
        if (_button is not null && In(PhonePickersPortrait.Action(f)))
            return ActionButton;
        if (_prompt is not null)
        {
            if (In(PhonePickersPortrait.Back(f)))
                return BackButton;
        }
        else if (In(PhonePickersPortrait.Tab(f, 0)))
            return TabCharacters;
        else if (In(PhonePickersPortrait.Tab(f, 1)))
            return TabItems;
        int cell = GridOf(f).CellAt(_grid.Scroll, at.X, at.Y);
        return _entries.Any(e => e.Number == cell) ? cell : NoButton;
    }

    private void DrawPortrait(Control c, PhoneUnits u)
    {
        var f = u.Frame;
        var grid = ScrolledGrid(f);
        PhonePickerParts.DrawPortraitTitle(c, u, _brush, _cinzel, "Treasure", _date);
        DrawPicture(c, u.Rect(PhonePickersPortrait.Picture(f)));
        var (dx, dy) = PhonePickersPortrait.Details(f);
        DrawDetails(c, u, dx / u.K, dy / u.K, (PhonePickersPortrait.DetailsRight(f) - dx) / u.K);
        var end = u.Rect(PhonePickersPortrait.End(f));
        _buttonSize = ButtonSize(u, PhonePickersPortrait.ButtonText, u.Rect(PhonePickersPortrait.Tab(f, 0)),
            u.Rect(PhonePickersPortrait.Back(f)), end, u.Rect(PhonePickersPortrait.Action(f)));
        if (_prompt is { } prompt)
        {
            DrawPrompt(c, u, prompt, u.Rect(PhonePickersPortrait.Prompt(f)));
            PhonePickerParts.DrawButton(c, u, u.Rect(PhonePickersPortrait.Back(f)), "Back", !Busy && _hovered == BackButton, _buttonSize);
        }
        else
            DrawTabs(c, u, view => u.Rect(PhonePickersPortrait.Tab(f, view)));
        DrawScrolledGrid(c, u, grid, portrait: true);
        PhonePickerParts.DrawButton(c, u, end, "Close", !Busy && _hovered == EndButton, _buttonSize);
        if (_button is { } button)
            DrawAction(c, u, button, u.Rect(PhonePickersPortrait.Action(f)));
    }

    /// <summary>The grid as the phone is held: upright under the tabs, held landscape in five columns at the right (P39).</summary>
    private static PhonePickersPortrait.ScrolledGrid GridOf(PhoneFrame f) =>
        f.IsPortrait ? PhonePickersPortrait.TreasureGrid(f) : PhonePickers.TreasureGrid(f);

    /// <summary>The grid for this drawing: from its first row when the other view is shown, the choice scrolled into view.</summary>
    private PhonePickersPortrait.ScrolledGrid ScrolledGrid(PhoneFrame f)
    {
        var grid = GridOf(f);
        if (_view != _gridView)                                 // the other view's entries: from its first row
        {
            _gridView = _view;
            _grid.Restart();
        }
        _grid.Reveal(grid, _selected);
        return grid;
    }

    /// <summary>
    /// The rows in view: each entry's cell (<paramref name="portrait"/>: an item's holder at the right of its line, else
    /// under its name), the other cells empty; the scroll bar with its ticks and the pills at filled cells out of view.
    /// </summary>
    private void DrawScrolledGrid(Control c, PhoneUnits u, PhonePickersPortrait.ScrolledGrid grid, bool portrait)
    {
        var listed = _entries.ToDictionary(e => e.Number);
        for (int n = 1; n <= TreasureRoster.Cells; n++)
        {
            if (grid.Cell(n, _grid.Scroll) is not { } cell)
                continue;
            if (listed.TryGetValue(n, out var entry))
                DrawEntryCell(c, u, u.Rect(cell), entry, n, portrait);
            else
                PhonePickerParts.DrawEmptyCell(c, u.Rect(cell), _k);
        }
        PhonePickerParts.DrawScrollBar(c, u, grid, _grid.Scroll);
        _more.Draw(c, u, _cinzel, grid, _grid.Scroll, FilledCells);
    }

    /// <summary>The phone's grid scrolled the least that shows cell <paramref name="number"/> in the view shown (the screen gallery's scrolled look).</summary>
    public void ShowCell(int number)
    {
        if (_phone is { } f)
        {
            _gridView = _view;
            _grid.Show(GridOf(f), number);
        }
        _face.QueueRedraw();
    }

    /// <summary>The cells an entry holds in the view shown: the hint's (<see cref="PickerMoreCells"/>).</summary>
    private int[] FilledCells => [.. _entries.Select(e => e.Number)];

    private bool GridTouch(InputEvent e) =>
        _phone is { } f && !Busy
        && (_more.Touch(e, cell => _grid.Show(GridOf(f), cell), _face.QueueRedraw)
            || _grid.Touch(e, GridOf(f), StartScroll, _face.QueueRedraw));

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
