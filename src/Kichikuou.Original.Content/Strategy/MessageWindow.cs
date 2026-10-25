using System.Text;
using System.Text.RegularExpressions;
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

    /// <summary>131: the full-width bar under the event CGs, the speaker's name in its text (<c>Sill「Ah... My clothes...」</c>).</summary>
    CgBar,
}

/// <summary>A character of the window's text at its cell (the 1280×800 screen's pixels), its colour when not the window's own.</summary>
public readonly record struct MessageGlyph(int X, int Y, int Size, char C, (byte R, byte G, byte B)? Colour);

/// <summary>
/// The message window as our interface draws it (docs/ui/messages.md, variant A, steps 1-2): the windows 111/112/121/122/113/123
/// and 131 where the original has them, as a lacquer panel of their kind, the text at the cells <see cref="TextBox"/> placed it in
/// (monospaced, the script's own line breaks — owner, 2026-10-18). Everything is in the 1280×800 screen's pixels: the panel
/// covers the original's frame and its tail whole (the mock-ups' numbers, §4). In 131 the speaker's name of
/// <c>Name「…」</c> (a thought: <c>Name（…）</c>) is split off onto a plate (<see cref="Speaker"/>), its closing bracket
/// dropped and the lines moved left by the name's cells (mock-ups A-7 to A-9).
/// </summary>
public sealed class MessageWindow
{
    /// <summary>The colour every window opens with (<c>GamePresenter.Window</c>: 15): the panel's own text colour.</summary>
    public const int PlainColor = 15;

    /// <summary>The colour 131 opens with (<c>GamePresenter.Window</c>: 190): its own text colour.</summary>
    public const int CgBarColor = 190;

    /// <summary>The size of a place's title (<c>ZM 32</c>).</summary>
    public const int TitleSize = 32;

    // The frames WAO_SUB opens (OriginalScriptContext.Wao.cs): 111, 112, 113/123, 121, 122.
    private const int SpeechLeftCg = 807, ThoughtLeftCg = 808, SpeechRightCg = 810, ThoughtRightCg = 811;

    /// <summary>The frame of the narration, 113 and 123 (<c>WAO_SUB.ADV:239-247</c>).</summary>
    public const int NarrationCg = 809;

    /// <summary>The frame of 131 (<c>WAO_SUB.ADV:216-233</c>).</summary>
    public const int CgBarCg = 815;

    private static readonly (int X, int Y, int W, int H) LeftPanel = (436, 4, 834, 254);
    private static readonly (int X, int Y, int W, int H) RightPanel = (58, 512, 822, 254);
    private static readonly (int X, int Y, int W, int H) NarrationPanel = (248, 536, 784, 258);
    private static readonly (int X, int Y, int W, int H) CgBarPanel = (0, 588, 1280, 212);

    /// <summary>
    /// A speaker's name at the start of 131's text, before <c>「</c> (<c>｢</c> from a string variable, <c>Hankaku</c>) or <c>（</c>: words of letters, digits and signs, each
    /// beginning with a capital, a digit or <c>?</c> (<c>Rance</c>, <c>Pink-Haired Girl</c>, <c>PG-7</c>, <c>???</c>) — not
    /// a sentence that happens to quote (<c>I’ll service（</c>).
    /// </summary>
    private static readonly Regex SpeakerName =
        new(@"^([A-Z0-9?][A-Za-z0-9?.'’\-]*(?: [A-Z0-9?][A-Za-z0-9?.'’\-]*)*)([「｢（])", RegexOptions.CultureInvariant);

    private MessageWindow(MessageKind kind, (int X, int Y, int W, int H) panel, IReadOnlyList<MessageGlyph> glyphs,
        IReadOnlyList<string> lines, string title, string subtitle, Speech speech = default)
    {
        Kind = kind;
        Panel = panel;
        Glyphs = glyphs;
        Lines = lines;
        Title = title;
        Subtitle = subtitle;
        Speaker = speech.Name ?? "";
        Thinks = speech.Thinks;
        Unclosed = speech.Unclosed;
        _prefix = speech.Prefix;
    }

    /// <summary>131's speaker, the name split off the text; empty for narration and the other windows.</summary>
    public string Speaker { get; }

    /// <summary>131's speaker thinks: the text was <c>Name（…）</c>.</summary>
    public bool Thinks { get; }

    /// <summary>
    /// 131's speech or thought is not closed at the window's end (<c>Gﾗﾌｧﾘｱ.ADV:652-657</c>): a next window without a
    /// name, its first line indented to the speech's text, goes on with it (<see cref="Of"/>'s <c>continuing</c>).
    /// </summary>
    public bool Unclosed { get; }

    /// <summary>The cells of the name and its bracket (screen pixels), which the lines under it are indented by.</summary>
    private readonly int _prefix;

    /// <summary>The speaker found in 131's text: the name, a thought or not, still open, the prefix's width.</summary>
    private readonly record struct Speech(string? Name, bool Thinks, bool Unclosed, int Prefix);

    /// <summary>The window's kind.</summary>
    public MessageKind Kind { get; }

    /// <summary>
    /// Whether the player read every message on the window before (<see cref="TextBox.ReadBefore"/>): its text takes the
    /// already-read text's colour (docs/ui/read-text.md).
    /// </summary>
    public bool Read { get; private init; }

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
    /// <c>B4</c> while the window stays; in 131 one that starts outside it): then the original's drawing stays.
    /// <paramref name="colour"/> gives a palette entry's colour, for characters printed in another colour than the window's
    /// own. <paramref name="continuing"/> is the 131 shown before, whose speech a window without a name may go on with
    /// (<see cref="Unclosed"/>).
    /// </summary>
    public static MessageWindow? Of(int frameCg, TextBox text, Func<int, (byte R, byte G, byte B)> colour, MessageWindow? continuing = null)
    {
        (MessageKind Kind, (int X, int Y, int W, int H) Panel)? shape = frameCg switch
        {
            SpeechLeftCg => (MessageKind.SpeechLeft, LeftPanel),
            ThoughtLeftCg => (MessageKind.ThoughtLeft, LeftPanel),
            SpeechRightCg => (MessageKind.SpeechRight, RightPanel),
            ThoughtRightCg => (MessageKind.ThoughtRight, RightPanel),
            NarrationCg => (MessageKind.Narration, NarrationPanel),
            CgBarCg => (MessageKind.CgBar, CgBarPanel),
            _ => null,
        };
        if (shape is not var (kind, panel))
            return null;
        const int k = Surface.Scale;
        int plain = kind == MessageKind.CgBar ? CgBarColor : PlainColor;
        var glyphs = new List<MessageGlyph>(text.Chars.Count);
        foreach (var (x, y, size, index, c) in text.Chars)
        {
            var glyph = new MessageGlyph(x * k, y * k, size * k, c, index == plain ? null : colour(index));
            // 131's text may run off the screen's edge and foot, as the engine's does (a line of 52 characters with a
            // full-width ’ in it, Gｶﾅﾐ.ADV; a fourth line): only where a character starts counts there.
            var (w, h) = kind == MessageKind.CgBar ? (1, 1) : (MessageWriter.CellWidth(c, size) * k, glyph.Size);
            if (glyph.X < panel.X || glyph.Y < panel.Y || glyph.X + w > panel.X + panel.W || glyph.Y + h > panel.Y + panel.H)
                return null;
            glyphs.Add(glyph);
        }
        if (kind == MessageKind.CgBar)
            return CgBar(panel, glyphs, text.Rect.X * k, continuing, text.ReadBefore);
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
        return new MessageWindow(kind, panel, glyphs, lines, title, subtitle) { Read = text.ReadBefore };
    }

    /// <summary>
    /// A speech of the right portrait's (121) holding <paramref name="lines"/>, printed where <c>WAO_SUB</c> prints it
    /// (<c>WAO_SUB.ADV:198, 212-214</c>: <c>ZM 24</c>, <c>B3 8, 43, 265, 378, 141</c>) in the window's own colour: Option →
    /// Text Speed…'s preview (docs/ui/messages.md, §5 step 4), <paramref name="read"/> when it counts as read before (the
    /// already-read text's colour, docs/ui/read-text.md).
    /// </summary>
    public static MessageWindow Sample(IReadOnlyList<string> lines, bool read = false) => SampleOf(MessageKind.SpeechRight, lines, read);

    /// <summary>
    /// A window of <paramref name="kind"/> holding <paramref name="lines"/>, printed where <c>WAO_SUB</c> prints that window
    /// (<c>WAO_SUB.ADV:170-250</c>: <c>ZM 24</c> in the text box its <c>B3</c> opens) in the window's own colour: the screen
    /// gallery's samples (docs/ui/gallery.md). A place's title prints its first line at <c>ZM 32</c>; 131's lines carry the
    /// speaker as the script prints it (<c>Name「…」</c>).
    /// </summary>
    public static MessageWindow SampleOf(MessageKind kind, IReadOnlyList<string> lines, bool read = false)
    {
        var (frame, rect) = kind switch
        {
            MessageKind.SpeechLeft => (SpeechLeftCg, (235, 8, 378, 141)),
            MessageKind.ThoughtLeft => (ThoughtLeftCg, (235, 8, 378, 141)),
            MessageKind.SpeechRight => (SpeechRightCg, (43, 265, 378, 141)),
            MessageKind.ThoughtRight => (ThoughtRightCg, (43, 265, 378, 141)),
            MessageKind.Narration or MessageKind.PlaceTitle => (NarrationCg, (140, 278, 376, 140)),
            MessageKind.CgBar => (CgBarCg, (8, 305, 626, 140)),
            _ => throw new ArgumentOutOfRangeException(nameof(kind)),
        };
        int colour = kind == MessageKind.CgBar ? CgBarColor : PlainColor;
        var box = new TextBox(rect);
        for (int i = 0; i < lines.Count; i++)
        {
            int size = kind == MessageKind.PlaceTitle && i == 0 ? TitleSize : 24;
            if (i > 0)
                box.NewLine(size);
            box.Print(lines[i], size, colour, read);
        }
        return Of(frame, box, _ => (0, 0, 0))
            ?? throw new ArgumentException($"The lines do not fit the {kind} window's text box.", nameof(lines));
    }

    /// <summary>
    /// 131: the speaker's <c>Name「</c> / <c>Name（</c> off the first line, which moves left by its cells; the lines under it
    /// move left by their indent, up to the name's cells; the closing bracket at the end dropped. A window without a name
    /// whose first line is indented to <paramref name="continuing"/>'s speech, which was not closed, goes on with it.
    /// </summary>
    private static MessageWindow CgBar((int X, int Y, int W, int H) panel, List<MessageGlyph> glyphs, int left, MessageWindow? continuing,
        bool read)
    {
        var rows = glyphs.GroupBy(g => g.Y).OrderBy(row => row.Key).Select(row => row.OrderBy(g => g.X).ToList()).ToList();
        var lines = rows.Select(row => LineOf(row, left)).ToList();
        string? name = null;
        bool thinks = false;
        int prefix = 0, from = 1;
        if (lines.Count > 0 && SpeakerName.Match(lines[0]) is { Success: true } m)
        {
            name = m.Groups[1].Value;
            thinks = m.Groups[2].Value == "（";
            int drop = name.Count(c => c != ' ') + 1;                           // the name's characters and its bracket
            var bracket = rows[0][drop - 1];
            prefix = bracket.X + MessageWriter.CellWidth(bracket.C, bracket.Size) - left;
            rows[0] = [.. rows[0].Skip(drop).Select(g => g with { X = g.X - prefix })];
        }
        else if (continuing is { Unclosed: true } open && rows.Count > 0 && rows[0][0].X - left >= open._prefix - rows[0][0].Size)
        {
            (name, thinks, prefix, from) = (open.Speaker, open.Thinks, open._prefix, 0);
        }
        bool unclosed = false;
        if (name is not null)
        {
            for (int i = from; i < rows.Count; i++)
            {
                int shift = Math.Min(rows[i][0].X - left, prefix);
                rows[i] = [.. rows[i].Select(g => g with { X = g.X - shift })];
            }
            if (rows.Count > 0 && rows[^1] is { Count: > 0 } last && (thinks ? last[^1].C == '）' : last[^1].C is '」' or '｣'))
                last.RemoveAt(last.Count - 1);
            else
                unclosed = true;
            rows.RemoveAll(row => row.Count == 0);
            lines = [.. rows.Select(row => LineOf(row, left))];
        }
        return new MessageWindow(MessageKind.CgBar, panel, [.. rows.SelectMany(row => row)], lines, "", "",
            new Speech(name, thinks, unclosed, prefix)) { Read = read };
    }

    /// <summary>A row of glyphs (screen pixels) from <paramref name="left"/>, a half-width space for each half cell between them.</summary>
    private static string LineOf(IEnumerable<MessageGlyph> row, int left)
    {
        var line = new StringBuilder();
        int at = left;
        foreach (var g in row)
        {
            if (g.Size >= 2 && g.X > at)
                line.Append(' ', (g.X - at) / (g.Size / 2));
            line.Append(g.C);
            at = g.X + MessageWriter.CellWidth(g.C, g.Size);
        }
        return line.ToString().TrimEnd();
    }

    /// <summary>A line's characters from the box's left edge, a half-width space for each half cell between them.</summary>
    internal static string Line(IEnumerable<(int X, int Y, int Size, int Color, char C)> row, int left)
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

    /// <summary>Whether <paramref name="other"/> shows the same: the kind, the panel, the speaker and every character.</summary>
    public bool SameAs(MessageWindow? other) =>
        other is not null && other.Kind == Kind && other.Panel == Panel && other.Speaker == Speaker && other.Thinks == Thinks
        && other.Read == Read && other.Glyphs.SequenceEqual(Glyphs);
}
