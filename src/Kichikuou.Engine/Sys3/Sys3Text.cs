using System.Globalization;
using System.Text;

namespace Kichikuou.Engine.Sys3;

/// <summary>Text formatting the engine applies to numbers (shared by messages and <c>MH</c>).</summary>
public static class Sys3Text
{
    /// <summary>
    /// xsystem35 <c>format_number</c> / <c>format_number_zenkaku</c> (<c>hankaku.c</c>):
    /// <c>%*d</c> with the width, of which only the last <paramref name="width"/> characters are
    /// kept; full-width digits and blanks when <paramref name="fullWidth"/> (<c>H</c>, <c>MH</c>).
    /// </summary>
    public static string FormatNumber(int value, int width, bool fullWidth)
    {
        string text = value.ToString(CultureInfo.InvariantCulture);
        if (width > 0)
        {
            text = text.PadLeft(width);
            text = text[^width..];
        }
        if (!fullWidth)
            return text;
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
            sb.Append(c == ' ' ? '　' : (char)('０' + (c - '0')));
        return sb.ToString();
    }
}
