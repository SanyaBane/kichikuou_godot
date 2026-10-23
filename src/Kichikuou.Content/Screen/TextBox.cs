namespace Kichikuou.Content.Screen;

/// <summary>
/// The text of one message box, kept as placed characters so that a screen composed by hand can
/// draw it again over whatever is under it: the cursor, line breaks and cell widths of the
/// engine's message text (<see cref="MessageWriter"/>), each character with the size and colour
/// it was printed in, and whether the player had read the message it came from before (<see cref="ReadBefore"/>).
/// </summary>
public sealed class TextBox((int X, int Y, int W, int H) rect)
{
    private readonly List<(int X, int Y, int Size, int Color, char C)> _chars = [];
    private readonly List<bool?> _read = [];        // each character's: its message read before or not; null for no message's

    /// <summary>The text rectangle (script pixels).</summary>
    public (int X, int Y, int W, int H) Rect { get; private set; } = rect;

    /// <summary>The left edge and top of the next character's cell.</summary>
    public (int X, int Y) Cursor { get; private set; } = (rect.X, rect.Y + 2);

    /// <summary>Whether anything is printed.</summary>
    public bool IsEmpty => _chars.Count == 0;

    /// <summary>The characters printed, in order (for tests and a UI that lays text out itself).</summary>
    public string Text => new(_chars.Select(c => c.C).ToArray());

    /// <summary>
    /// The characters printed, in order, each at the left edge and top of its cell (script pixels) with the size and
    /// palette colour it was printed in; the spaces are not kept (a UI that draws the text itself puts each at its cell).
    /// </summary>
    public IReadOnlyList<(int X, int Y, int Size, int Color, char C)> Chars => _chars;

    /// <summary>
    /// Whether the box holds a message's characters and every message it holds was read before it was printed: the
    /// already-read text's colour (docs/ui/read-text.md). Characters printed from no message (a string variable, the
    /// port's own text) count neither way.
    /// </summary>
    public bool ReadBefore => _read.Contains(true) && !_read.Contains(false);

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
        _read.Clear();
        Home();
    }

    /// <summary><c>Y 1, 1</c>: the cursor to the first line; the text stays.</summary>
    public void Home() => Cursor = (Rect.X, Rect.Y + 2);

    /// <summary>
    /// A character another writer placed (<see cref="MessageWriter.Printed"/>) at its cell (<paramref name="x"/>,
    /// <paramref name="y"/>), with its size and colour; the cursor moves past it. <paramref name="read"/>: whether its
    /// message was read before (<see cref="ReadBefore"/>), null when it comes from none.
    /// </summary>
    public void Put(int x, int y, int size, int color, char c, bool? read = null)
    {
        _chars.Add((x, y, size, color, c));
        _read.Add(read);
        Cursor = (x + MessageWriter.CellWidth(c, size), y);
    }

    /// <summary><c>R</c> at <paramref name="size"/>.</summary>
    public void NewLine(int size) => Cursor = (Rect.X, Cursor.Y + size + 2);

    /// <summary>Prints <paramref name="text"/> at the cursor (<c>\n</c> is <c>R</c>); <paramref name="read"/>: whether its message was read
    /// before (<see cref="ReadBefore"/>), null when it comes from none.</summary>
    public void Print(string text, int size, int color, bool? read = null)
    {
        foreach (char c in text)
        {
            if (c == '\n')
            {
                NewLine(size);
                continue;
            }
            if (c != ' ' && c != '　')
            {
                _chars.Add((Cursor.X, Cursor.Y, size, color, c));
                _read.Add(read);
            }
            Cursor = (Cursor.X + MessageWriter.CellWidth(c, size), Cursor.Y);
        }
    }

    /// <summary>
    /// An opaque drawing over the screen at (<paramref name="x"/>, <paramref name="y"/>): the
    /// characters whose cells it reaches are gone (the engine prints into the screen, so a copy over
    /// the text erases it — <c>OVER.ADV:2208</c> restores its text area so); the cursor stays.
    /// </summary>
    public void Wipe(int x, int y, int w, int h)
    {
        for (int i = _chars.Count - 1; i >= 0; i--)
        {
            var c = _chars[i];
            if (c.X < x + w && x < c.X + MessageWriter.CellWidth(c.C, c.Size) && c.Y < y + h && y < c.Y + c.Size)
            {
                _chars.RemoveAt(i);
                _read.RemoveAt(i);
            }
        }
    }

    /// <summary>Draws the text onto <paramref name="surface"/> in its palette's colours.</summary>
    public void Draw(Surface surface, IGlyphRenderer glyphs)
    {
        foreach (var (x, y, size, color, c) in _chars)
            MessageWriter.DrawString(surface, glyphs, c.ToString(), x, y, size, color);
    }
}
