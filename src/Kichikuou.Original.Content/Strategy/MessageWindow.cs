using System.Text;
using Kichikuou.Content.Screen;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>What a <c>WAO_SUB</c> window is for, as its frame shows it (docs/ui/messages.md, §1).</summary>
public enum MessageKind
{
    /// <summary>111: the white speech box, its tail at the left portrait.</summary>
    SpeechLeft,

    /// <summary>121: the white speech box, its tail at the right portrait.</summary>
    SpeechRight,

    /// <summary>112: the dark box, a thought of the left portrait's.</summary>
    ThoughtLeft,

    /// <summary>122: the dark box, a thought of the right portrait's.</summary>
    ThoughtRight,

    /// <summary>113 / 123: the cream box — narration, status windows.</summary>
    Narration,

    /// <summary>113 holding a place's title: one line at <c>ZM 32</c>, the room under it at most (<c>Cｼﾊｲﾄｼ.ADV:1185-1192</c>).</summary>
    PlaceTitle,
}

/// <summary>A character of the window's text at its cell (the 1280×800 screen's pixels), its colour when not the window's own.</summary>
public readonly record struct MessageGlyph(int X, int Y, int Size, char C, (byte R, byte G, byte B)? Colour);

/// <summary>
/// The message window as our interface draws it (docs/ui/messages.md, variant A, step 1): the windows 111/112/121/122/113/123
/// where the original has them, as a lacquer panel of their kind, the text at the cells <see cref="TextBox"/> placed it in
/// (monospaced, the script's own line breaks — owner, 2026-10-18). Everything is in the 1280×800 screen's pixels: the panel
/// covers the original's frame and its tail whole (the mock-ups' numbers, §4).
/// </summary>
public sealed class MessageWindow
{
    /// <summary>The colour every window opens with (<c>GamePresenter.Window</c>: 15): the panel's own text colour.</summary>
    public const int PlainColor = 15;

    /// <summary>The size of a place's title (<c>ZM 32</c>).</summary>
    public const int TitleSize = 32;

    // The frames WAO_SUB opens (OriginalScriptContext.Wao.cs): 111, 112, 113/123, 121, 122.
    private const int SpeechLeftCg = 807, ThoughtLeftCg = 808, NarrationCg = 809, SpeechRightCg = 810, ThoughtRightCg = 811;

    private static readonly (int X, int Y, int W, int H) LeftPanel = (436, 4, 834, 254);
    private static readonly (int X, int Y, int W, int H) RightPanel = (58, 512, 822, 254);
    private static readonly (int X, int Y, int W, int H) NarrationPanel = (248, 536, 784, 258);

    private MessageWindow(MessageKind kind, (int X, int Y, int W, int H) panel, IReadOnlyList<MessageGlyph> glyphs,
        IReadOnlyList<string> lines, string title, string subtitle)
    {
        Kind = kind;
        Panel = panel;
        Glyphs = glyphs;
        Lines = lines;
        Title = title;
        Subtitle = subtitle;
    }

    /// <summary>The window's kind.</summary>
    public MessageKind Kind { get; }

    /// <summary>The panel's rectangle (screen pixels), without its tail.</summary>
    public (int X, int Y, int W, int H) Panel { get; }

    /// <summary>The text's characters at their cells, in the order printed (no spaces).</summary>
    public IReadOnlyList<MessageGlyph> Glyphs { get; }

    /// <summary>The text's lines, top to bottom, the spaces between the characters put back from their cells (trailing ones trimmed).</summary>
    public IReadOnlyList<string> Lines { get; }

    /// <summary>A place's title (<see cref="MessageKind.PlaceTitle"/>): its first line, trimmed; else empty.</summary>
    public string Title { get; }

    /// <summary>A place's title's second line (the room), trimmed; empty when it has none or is no title.</summary>
    public string Subtitle { get; }

    /// <summary>Whether the window is a speech's (111/121).</summary>
    public bool IsSpeech => Kind is MessageKind.SpeechLeft or MessageKind.SpeechRight;

    /// <summary>Whether the window is a thought's (112/122).</summary>
    public bool IsThought => Kind is MessageKind.ThoughtLeft or MessageKind.ThoughtRight;

    /// <summary>Whether the window's portrait is the left one (111/112): its tail leaves the panel's left side.</summary>
    public bool AtLeft => Kind is MessageKind.SpeechLeft or MessageKind.ThoughtLeft;

    /// <summary>
    /// The window of the frame <paramref name="frameCg"/> (<c>Ui_WinFrameCg</c>) with the text of <paramref name="text"/>,
    /// or null when the frame is not one of these windows or a character lies outside the panel (a text box moved by
    /// <c>B4</c> while the window stays): then the original's drawing stays. <paramref name="colour"/> gives a palette
    /// entry's colour, for characters printed in another colour than the window's own.
    /// </summary>
    public static MessageWindow? Of(int frameCg, TextBox text, Func<int, (byte R, byte G, byte B)> colour)
    {
        (MessageKind Kind, (int X, int Y, int W, int H) Panel)? shape = frameCg switch
        {
            SpeechLeftCg => (MessageKind.SpeechLeft, LeftPanel),
            ThoughtLeftCg => (MessageKind.ThoughtLeft, LeftPanel),
            SpeechRightCg => (MessageKind.SpeechRight, RightPanel),
            ThoughtRightCg => (MessageKind.ThoughtRight, RightPanel),
            NarrationCg => (MessageKind.Narration, NarrationPanel),
            _ => null,
        };
        if (shape is not var (kind, panel))
            return null;
        const int k = Surface.Scale;
        var glyphs = new List<MessageGlyph>(text.Chars.Count);
        foreach (var (x, y, size, index, c) in text.Chars)
        {
            var glyph = new MessageGlyph(x * k, y * k, size * k, c, index == PlainColor ? null : colour(index));
            int w = MessageWriter.CellWidth(c, size) * k;
            if (glyph.X < panel.X || glyph.Y < panel.Y || glyph.X + w > panel.X + panel.W || glyph.Y + glyph.Size > panel.Y + panel.H)
                return null;
            glyphs.Add(glyph);
        }
        var rows = text.Chars.GroupBy(c => c.Y).OrderBy(row => row.Key).ToArray();
        var lines = rows.Select(row => Line(row, text.Rect.X)).ToArray();
        string title = "", subtitle = "";
        if (kind == MessageKind.Narration && rows.Length is 1 or 2 && rows[0].All(c => c.Size == TitleSize)
            && rows.Skip(1).All(row => row.All(c => c.Size != TitleSize)))
        {
            kind = MessageKind.PlaceTitle;
            title = lines[0].Trim();
            subtitle = lines.Length > 1 ? lines[1].Trim() : "";
        }
        return new MessageWindow(kind, panel, glyphs, lines, title, subtitle);
    }

    /// <summary>A line's characters from the box's left edge, a half-width space for each half cell between them.</summary>
    private static string Line(IEnumerable<(int X, int Y, int Size, int Color, char C)> row, int left)
    {
        var line = new StringBuilder();
        int at = left;
        foreach (var (x, _, size, _, c) in row.OrderBy(c => c.X))
        {
            if (size >= 2 && x > at)
                line.Append(' ', (x - at) / (size / 2));
            line.Append(c);
            at = x + MessageWriter.CellWidth(c, size);
        }
        return line.ToString().TrimEnd();
    }

    /// <summary>Whether <paramref name="other"/> shows the same: the kind, the panel and every character.</summary>
    public bool SameAs(MessageWindow? other) =>
        other is not null && other.Kind == Kind && other.Panel == Panel && other.Glyphs.SequenceEqual(Glyphs);
}
