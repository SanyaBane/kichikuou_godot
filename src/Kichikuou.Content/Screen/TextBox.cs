namespace Kichikuou.Content.Screen;

/// <summary>
/// The text of one message box, kept as placed characters so that a screen composed by hand can
/// draw it again over whatever is under it: the cursor, line breaks and cell widths of the
/// engine's message text (<see cref="MessageWriter"/>), each character with the size and colour
/// it was printed in.
/// </summary>
public sealed class TextBox((int X, int Y, int W, int H) rect)
{
    private readonly List<(int X, int Y, int Size, int Color, char C)> _chars = [];

    /// <summary>The text rectangle (script pixels).</summary>
    public (int X, int Y, int W, int H) Rect { get; private set; } = rect;

    /// <summary>The left edge and top of the next character's cell.</summary>
    public (int X, int Y) Cursor { get; private set; } = (rect.X, rect.Y + 2);

    /// <summary>Whether anything is printed.</summary>
    public bool IsEmpty => _chars.Count == 0;

    /// <summary>The characters printed, in order (for tests and a UI that lays text out itself).</summary>
    public string Text => new(_chars.Select(c => c.C).ToArray());

    /// <summary>Moves the box (a window opened) and clears it.</summary>
    public void Open((int X, int Y, int W, int H) rect)
    {
        Rect = rect;
        Clear();
    }

    /// <summary>Removes the text and puts the cursor home.</summary>
    public void Clear()
    {
        _chars.Clear();
        Home();
    }

    /// <summary><c>Y 1, 1</c>: the cursor to the first line; the text stays.</summary>
    public void Home() => Cursor = (Rect.X, Rect.Y + 2);

    /// <summary><c>R</c> at <paramref name="size"/>.</summary>
    public void NewLine(int size) => Cursor = (Rect.X, Cursor.Y + size + 2);

    /// <summary>Prints <paramref name="text"/> at the cursor (<c>\n</c> is <c>R</c>).</summary>
    public void Print(string text, int size, int color)
    {
        foreach (char c in text)
        {
            if (c == '\n')
            {
                NewLine(size);
                continue;
            }
            if (c != ' ' && c != '　')
                _chars.Add((Cursor.X, Cursor.Y, size, color, c));
            Cursor = (Cursor.X + MessageWriter.CellWidth(c, size), Cursor.Y);
        }
    }

    /// <summary>
    /// An opaque drawing over the screen at (<paramref name="x"/>, <paramref name="y"/>): the
    /// characters whose cells it reaches are gone (the engine prints into the screen, so a copy over
    /// the text erases it — <c>OVER.ADV:2208</c> restores its text area so); the cursor stays.
    /// </summary>
    public void Wipe(int x, int y, int w, int h) =>
        _chars.RemoveAll(c => c.X < x + w && x < c.X + MessageWriter.CellWidth(c.C, c.Size) && c.Y < y + h && y < c.Y + c.Size);

    /// <summary>Draws the text onto <paramref name="surface"/> in its palette's colours.</summary>
    public void Draw(Surface surface, IGlyphRenderer glyphs)
    {
        foreach (var (x, y, size, color, c) in _chars)
            MessageWriter.DrawString(surface, glyphs, c.ToString(), x, y, size, color);
    }
}
