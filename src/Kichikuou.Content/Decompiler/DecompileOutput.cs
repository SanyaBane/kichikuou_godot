using System.Globalization;
using System.Text;

namespace Kichikuou.Content.Decompiler;

/// <summary>
/// A scenario file or a construct the decompiler cannot read: the file (its name in the archive) and the
/// address in it, as xsys35dc's <c>error_at</c> reports them.
/// </summary>
public sealed class DecompileException(string message) : Exception(message);

/// <summary>Where the expression reader reports its errors and warnings (the decompiler, which knows the page).</summary>
internal interface IDecompileErrors
{
    /// <summary>An error at an address of the current page (<c>error_at</c>).</summary>
    DecompileException At(int address, string message);

    /// <summary>A warning at an address of the current page (<c>warning_at</c>).</summary>
    void Warning(int address, string message);
}

/// <summary>
/// The text of one page as xsys35dc writes it before its conversion: Shift JIS bytes, lines ending in LF
/// (<see cref="ToUtf8Crlf"/> makes the file of them).
/// </summary>
internal sealed class DecompileOutput
{
    private readonly MemoryStream _bytes = new();

    public void Char(char c) => _bytes.WriteByte((byte)c);

    public void Byte(byte c) => _bytes.WriteByte(c);

    public void Ascii(string s)
    {
        foreach (char c in s)
            _bytes.WriteByte((byte)c);
    }

    public void Bytes(ReadOnlySpan<byte> s) => _bytes.Write(s);

    /// <summary><c>%d</c>.</summary>
    public void Int(int n) => Ascii(n.ToString(CultureInfo.InvariantCulture));

    /// <summary><c>%05x</c>.</summary>
    public void Hex5(uint n) => Ascii(n.ToString("x5", CultureInfo.InvariantCulture));

    /// <summary><c>%04X</c>.</summary>
    public void Hex4Upper(int n) => Ascii(n.ToString("X4", CultureInfo.InvariantCulture));

    /// <summary>
    /// The file as xsys35dc 1.13 for Windows leaves it: the Shift JIS converted by <c>sjis2utf</c>
    /// (<see cref="Text.Sjis.Decode"/>), written in text mode (CRLF), UTF-8 without a byte order mark.
    /// </summary>
    public byte[] ToUtf8Crlf()
    {
        string text = Text.Sjis.Decode(_bytes.GetBuffer().AsSpan(0, (int)_bytes.Length));
        return new UTF8Encoding(false).GetBytes(text.Replace("\n", "\r\n", StringComparison.Ordinal));
    }
}
