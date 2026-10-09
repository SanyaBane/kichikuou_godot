namespace Kichikuou.Content.Screen;

/// <summary>A rendered glyph: coverage 0-255 per stored pixel.</summary>
/// <param name="Width">Bitmap width in stored pixels.</param>
/// <param name="Height">Bitmap height in stored pixels.</param>
/// <param name="Coverage">Row-major coverage, <paramref name="Width"/> × <paramref name="Height"/>.</param>
/// <param name="Left">Offset from the pen x to the bitmap's left edge.</param>
/// <param name="Top">Offset from the top of the character cell to the bitmap's top edge.</param>
public sealed record Glyph(int Width, int Height, byte[] Coverage, int Left, int Top);

/// <summary>
/// Renders characters of the game font (MS Gothic, bold — docs/battle/screen-layout.md §7.1) at a
/// pixel size in stored pixels (script size × <see cref="Surface.Scale"/>). Implemented by the
/// presentation engine.
/// </summary>
public interface IGlyphRenderer
{
    /// <summary>The glyph of <paramref name="c"/>, or <c>null</c> for an empty one (space, unknown).</summary>
    Glyph? Render(char c, int pixelSize);
}

/// <summary>
/// The message text engine as the battle uses it (docs/battle/screen-layout.md §0.3): one open
/// text window (<c>B3</c>/<c>B4</c>), a cursor, the text colour (<c>ZC 1</c>) and size
/// (<c>ZM</c>). Text is never wrapped; every character takes one cell — half the size for a
/// half-width character, the full size otherwise.
/// </summary>
public sealed class MessageWriter(Surface surface, IGlyphRenderer glyphs)
{
    /// <summary>The text rectangle of the open window (script pixels).</summary>
    public (int X, int Y, int W, int H) Window { get; private set; }

    /// <summary>The cursor: left edge and top of the next character's cell.</summary>
    public (int X, int Y) Cursor { get; set; }

    /// <summary><c>ZC 1</c>: the palette index of message text.</summary>
    public int Color { get; set; } = 15;

    /// <summary><c>ZM</c>: the message size in script pixels.</summary>
    public int Size { get; set; } = 24;

    /// <summary><c>B3 n, x, y, w, h</c> + <c>B4 n, 0, 0, 0, 1, 0</c>: selects a window and puts the cursor home.</summary>
    public void Open(int x, int y, int w, int h)
    {
        Window = (x, y, w, h);
        Home();
    }

    /// <summary><c>Y 1, 1</c> (and opening a window): the cursor to the window's first line.</summary>
    public void Home() => Cursor = (Window.X, Window.Y + 2);

    /// <summary><c>R</c>: back to the window's left edge, one line down at the current size.</summary>
    public void NewLine() => Cursor = (Window.X, Cursor.Y + Size + 2);

    /// <summary>Prints text at the cursor; <c>\n</c> is <c>R</c>.</summary>
    public void Print(string text)
    {
        foreach (char c in text)
        {
            if (c == '\n')
            {
                NewLine();
                continue;
            }
            Cursor = (Cursor.X + DrawChar(surface, glyphs, c, Cursor.X, Cursor.Y, Size, surface.Color(Color)), Cursor.Y);
        }
    }

    /// <summary>Draws a string at a cell position without a window (selection items); returns the width drawn.</summary>
    public static int DrawString(Surface surface, IGlyphRenderer glyphs, string text, int x, int y, int size, int colorIndex)
    {
        int start = x;
        var color = surface.Color(colorIndex);
        foreach (char c in text)
            x += DrawChar(surface, glyphs, c, x, y, size, color);
        return x - start;
    }

    /// <summary>Draws a string in a colour outside the palette; returns the width drawn.</summary>
    public static int DrawString(Surface surface, IGlyphRenderer glyphs, string text, int x, int y, int size, (byte R, byte G, byte B) color)
    {
        int start = x;
        foreach (char c in text)
            x += DrawChar(surface, glyphs, c, x, y, size, color);
        return x - start;
    }

    /// <summary>The cell width of <paramref name="c"/> at <paramref name="size"/> (script pixels).</summary>
    public static int CellWidth(char c, int size) => IsHalfWidth(c) ? size / 2 : size;

    /// <summary>Whether <paramref name="c"/> takes a half-width cell (ASCII, Latin-1, half-width katakana).</summary>
    public static bool IsHalfWidth(char c) => c < 0x100 || c is >= '｡' and <= 'ﾟ';

    private static int DrawChar(Surface surface, IGlyphRenderer glyphs, char c, int x, int y, int size, (byte, byte, byte) color)
    {
        if (glyphs.Render(c, size * Surface.Scale) is { } g)
            surface.Blend(g.Coverage, g.Width, g.Height, x * Surface.Scale + g.Left, y * Surface.Scale + g.Top, color);
        return CellWidth(c, size);
    }
}
