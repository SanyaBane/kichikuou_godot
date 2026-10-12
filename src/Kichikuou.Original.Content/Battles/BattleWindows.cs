using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;

namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// A shape of the stage's screen that is a window now (docs/ui/battle-screen.md, "The scene between
/// exchanges"): a CG drawn with the sprite key (its non-key pixels at its position), or a rectangle (a
/// CG drawn opaque, a selection box with its frame).
/// </summary>
/// <param name="X">The left edge on the screen.</param>
/// <param name="Y">The top edge on the screen.</param>
/// <param name="W">The width.</param>
/// <param name="H">The height.</param>
/// <param name="Sprite">The CG whose non-key pixels are the shape, or null for the whole rectangle.</param>
public sealed record WindowShape(int X, int Y, int W, int H, CgImage? Sprite = null)
{
    /// <summary>A CG drawn with the key at (x, y): its pixels but those of <see cref="Surface.SpriteKey"/>.</summary>
    public static WindowShape Keyed(CgImage cg, int x, int y) => new(x, y, cg.Width, cg.Height, cg);

    /// <summary>A rectangle, whole.</summary>
    public static WindowShape Box(int x, int y, int w, int h) => new(x, y, w, h);

    /// <summary>A selection box (<see cref="SelectionBox.Box"/>) with its 8-pixel line frame outside it.</summary>
    public static WindowShape Of(SelectionBox box)
    {
        var (x, y, w, h) = box.Box;
        return Box(x - 8, y - 8, w + 16, h + 16);
    }
}

/// <summary>
/// The screen's pixels that belong to windows (<see cref="WindowShape"/>s, 640×400 script pixels), and the
/// screen shown only there: our battle screen puts the original's windows over its own scene this way.
/// </summary>
public sealed class WindowMask
{
    private const int Width = Surface.ScreenWidth, Height = Surface.ScreenHeight;

    private readonly bool[] _in = new bool[Width * Height];

    /// <summary>The union of <paramref name="shapes"/>, clipped to the screen.</summary>
    public static WindowMask Of(IEnumerable<WindowShape> shapes)
    {
        var mask = new WindowMask();
        foreach (var shape in shapes)
            mask.Add(shape);
        return mask;
    }

    /// <summary>How many script pixels are windows.</summary>
    public int Count { get; private set; }

    /// <summary>Whether no window is open.</summary>
    public bool IsEmpty => Count == 0;

    /// <summary>Whether script pixel (x, y) of the screen belongs to a window.</summary>
    public bool Contains(int x, int y) => x is >= 0 and < Width && y is >= 0 and < Height && _in[y * Width + x];

    private void Add(WindowShape shape)
    {
        int x0 = Math.Max(shape.X, 0), y0 = Math.Max(shape.Y, 0);
        int x1 = Math.Min(shape.X + shape.W, Width), y1 = Math.Min(shape.Y + shape.H, Height);
        for (int y = y0; y < y1; y++)
        {
            for (int x = x0; x < x1; x++)
            {
                if (shape.Sprite is { } cg && cg.IndexAt(x - shape.X, y - shape.Y) == Surface.SpriteKey)
                    continue;
                ref bool p = ref _in[y * Width + x];
                if (!p)
                    Count++;
                p = true;
                _top = Math.Min(_top, y);
                _bottom = Math.Max(_bottom, y + 1);
            }
        }
    }

    /// <summary>The rows the windows span: from <see cref="_top"/> to before <see cref="_bottom"/> (none: empty).</summary>
    private int _top = Height, _bottom;

    /// <summary>
    /// The visible screen of <paramref name="screen"/> as RGBA rows (<see cref="Surface.ReadScreen"/>),
    /// transparent (alpha 0) outside the windows, into <paramref name="target"/> when given.
    /// </summary>
    public byte[] Compose(Surface screen, byte[]? target = null)
    {
        int row = Width * Surface.Scale, rowBytes = row * 4 * Surface.Scale;
        target ??= new byte[rowBytes * Height];
        Array.Clear(target, 0, _top * rowBytes);                         // the rows without a window: transparent, unread
        Array.Clear(target, _bottom * rowBytes, (Height - _bottom) * rowBytes);
        if (_bottom > _top)
            screen.ReadArea(screen.View.X, screen.View.Y + _top, Width, _bottom - _top, target, _top * rowBytes);
        for (int py = _top * Surface.Scale; py < _bottom * Surface.Scale; py++)
        {
            int y = py / Surface.Scale;
            for (int px = 0; px < row; px++)
            {
                if (!_in[y * Width + px / Surface.Scale])
                    target[(py * row + px) * 4 + 3] = 0;
            }
        }
        return target;
    }
}
