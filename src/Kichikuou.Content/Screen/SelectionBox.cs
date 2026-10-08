namespace Kichikuou.Content.Screen;

/// <summary>
/// The engine's selection window (<c>$label$ 'item' $ … ]</c>, xsystem35 <c>selection.c</c>) as
/// the battle configures it: a saved area, a box of colour <c>ZC 4</c> with the 8-dot line
/// frame (<c>ZC 3</c> / <c>ZC 4</c>), items in <c>ZS</c> size and <c>ZC 2</c> colour, a two-pixel
/// outline around the hovered item (<c>EncloseType</c> 0). docs/battle/screen-layout.md §4.4-§4.6.
/// </summary>
public sealed class SelectionBox
{
    private readonly Surface _surface;
    private readonly IGlyphRenderer _glyphs;
    private readonly IReadOnlyList<string> _items;
    private readonly int _size;
    private readonly int _frameColor;
    private SavedRegion? _under;
    private SavedRegion? _underHover;

    /// <param name="surface">Where to draw.</param>
    /// <param name="glyphs">The font.</param>
    /// <param name="x">Window x (<c>B1</c>).</param>
    /// <param name="y">Window y.</param>
    /// <param name="width">Window width (<c>B1</c>); the box is 4 wider.</param>
    /// <param name="items">The item texts, in order.</param>
    /// <param name="size"><c>ZS</c>: 16 in the battle.</param>
    /// <param name="background"><c>ZC 4</c>: 18.</param>
    /// <param name="frameColor"><c>ZC 3</c>: 255.</param>
    /// <param name="textColor"><c>ZC 2</c>: 22.</param>
    public SelectionBox(Surface surface, IGlyphRenderer glyphs, int x, int y, int width, IReadOnlyList<string> items,
        int size = 16, int background = 18, int frameColor = 255, int textColor = 22)
    {
        _surface = surface;
        _glyphs = glyphs;
        _items = items;
        _size = size;
        _frameColor = frameColor;
        X = x;
        Y = y;
        Width = width;
        Background = background;
        TextColor = textColor;
    }

    /// <summary>Window x.</summary>
    public int X { get; }

    /// <summary>Window y.</summary>
    public int Y { get; }

    /// <summary>Window width as given to <c>B1</c>.</summary>
    public int Width { get; }

    /// <summary>Box background colour.</summary>
    public int Background { get; }

    /// <summary>Item text colour.</summary>
    public int TextColor { get; }

    /// <summary>The item texts.</summary>
    public IReadOnlyList<string> Items => _items;

    /// <summary>The hovered item, or −1.</summary>
    public int Hovered { get; private set; } = -1;

    /// <summary>Whether the box is on screen.</summary>
    public bool IsOpen => _under is not null;

    private int Pitch => _size + 2;

    /// <summary>The box rectangle (<c>init_selwindow</c>): 4 + width wide, 2 + pitch × items high.</summary>
    public (int X, int Y, int W, int H) Box => (X, Y, 4 + Width, 2 + Pitch * _items.Count);

    /// <summary>Saves the area and draws the box, frame and items.</summary>
    public void Open()
    {
        var (x, y, w, h) = Box;
        _under = _surface.SaveRegion(x - 8, y - 8, w + 16, h + 16);
        _surface.Fill(x, y, w, h, Background);
        // drawLineFrame: 3 frame-colour lines, 2 background, 1 frame, 2 background (outside in).
        int[] colors = [_frameColor, _frameColor, _frameColor, Background, Background, _frameColor, Background, Background];
        for (int i = 0; i < 8; i++)
        {
            int d = 8 - i;
            _surface.DrawRectangle(x - d, y - d, w + 2 * d, h + 2 * d, colors[i]);
        }
        for (int i = 0; i < _items.Count; i++)
            MessageWriter.DrawString(_surface, _glyphs, _items[i], x + 2, y + i * Pitch + 2, _size, TextColor);
    }

    /// <summary>The item under (mx, my) (<c>whereElement</c>), or −1.</summary>
    public int HitTest(int mx, int my)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            int ix = X, iy = Y + i * Pitch, iw = 2 + Width, ih = 2 + _size;
            if (mx >= ix && mx < ix + iw && my >= iy && my < iy + ih)
                return i;
        }
        return -1;
    }

    /// <summary>Moves the hover outline to the item under the mouse (<c>encloseElement</c>).</summary>
    public void Hover(int mx, int my)
    {
        int item = HitTest(mx, my);
        if (item == Hovered)
            return;
        if (_underHover is not null)
        {
            _surface.RestoreRegion(_underHover);
            _underHover = null;
        }
        Hovered = item;
        if (item < 0)
            return;
        int x = X, y = Y + item * Pitch, w = 2 + Width, h = 2 + _size;
        _underHover = _surface.SaveRegion(x, y, w + 2, h + 2);
        _surface.DrawRectangle(x, y, w + 2, h + 2, _frameColor);
        _surface.DrawRectangle(x + 1, y + 1, w, h, _frameColor);
    }

    /// <summary>Removes the box (<c>remove_selwindow</c>), restoring what was under it.</summary>
    public void Close()
    {
        if (_underHover is not null)
            _surface.RestoreRegion(_underHover);
        _underHover = null;
        if (_under is not null)
            _surface.RestoreRegion(_under);
        _under = null;
        Hovered = -1;
    }
}
