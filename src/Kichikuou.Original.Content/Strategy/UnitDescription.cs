using System.Text;
using System.Text.RegularExpressions;
using Kichikuou.Content.Graphics;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// A unit's description as text, read from the panel at the right of its status CG (600 + n), where the
/// English patch drew it in a pixel font of its own (docs/ui/unit-descriptions.md). The text is not in the
/// scenario: it is only these pixels, so it is read from the player's CG each time, never kept in the
/// repository. The font has no anti-aliasing — its letters are pure white on the panel's greys — so the
/// reading is exact: every piece of white between empty columns of a line is looked up whole by its
/// pixels' hash (<see cref="UnitDescriptionGlyphs"/>), letters that touch included.
/// </summary>
public static partial class UnitDescription
{
    /// <summary>The first column of the panel's text area.</summary>
    public const int Left = 207;

    /// <summary>The column after the panel's text area.</summary>
    public const int Right = 302;

    /// <summary>The height of a line of the font: two rows above the small letters, five for them, two below.</summary>
    public const int LineHeight = 9;

    /// <summary>The CG's width of every status CG with a panel.</summary>
    public const int Width = 304;

    /// <summary>
    /// The description in the panel of status CG <paramref name="number"/> as paragraphs, the lines of each
    /// joined (a word broken at a line's end joined again); <c>null</c> when the CG is not a status CG, its
    /// panel is empty (600, 692, 696) or not in the font the table knows (654's Japanese).
    /// </summary>
    public static IReadOnlyList<string>? Read(CgImage cg, int number)
    {
        if (Rows(cg) is not var (top, bottom))
            return null;
        var white = WhiteMask(cg);
        bool White(int x, int y) => y >= 0 && y < cg.Height && white[y * cg.Width + x];

        var count = new int[cg.Height];
        for (int y = top; y < bottom; y++)
            for (int x = Left; x < Right; x++)
                if (White(x, y))
                    count[y]++;
        int Count(int y) => y >= top && y < bottom ? count[y] : 0;

        var paragraphs = new List<List<(int Line, string Text)>>();
        int line = 0, previousEnd = int.MinValue;
        foreach (var (start, end) in Runs(count, top, bottom))
        {
            if (Fit(start, end, Count) is not var (first, lines))
                return null;
            if (first != previousEnd)
                paragraphs.Add([]);
            previousEnd = first + lines * LineHeight;
            for (int i = 0; i < lines; i++, line++)
            {
                if (ReadLine(number, line, first + i * LineHeight, White) is not { } text)
                    return null;
                paragraphs[^1].Add((line, text));
            }
        }
        var result = paragraphs.Select(p => Join(number, p)).Where(p => p.Length > 0).ToList();
        return result.Count == 0 ? null : result;
    }

    /// <summary>The rows of the panel's inside: the CG 304×186 (600-612) has it at 1-184, the CG 304×196 at 6-189.</summary>
    private static (int Top, int Bottom)? Rows(CgImage cg) => (cg.Width, cg.Height) switch
    {
        (Width, 186) => (1, 185),
        (Width, 196) => (6, 190),
        _ => null,
    };

    /// <summary>Which pixels are the font's white, by the CG's own palette.</summary>
    private static bool[] WhiteMask(CgImage cg)
    {
        var palette = cg.FilePalette;
        var mask = new bool[cg.Indices.Length];
        for (int i = 0; i < mask.Length; i++)
        {
            int o = cg.Indices[i] * 3;
            mask[i] = palette[o] == 255 && palette[o + 1] == 255 && palette[o + 2] == 255;
        }
        return mask;
    }

    /// <summary>
    /// The runs of rows with white in them. Rows within a line may be empty (a line of small letters only), and the
    /// rows between two lines are empty when the upper has no descender and the lower no capital, so runs two rows
    /// apart or less are one; a paragraph's gap is wider.
    /// </summary>
    private static List<(int Start, int End)> Runs(int[] count, int top, int bottom)
    {
        var runs = new List<(int Start, int End)>();
        for (int y = top; y < bottom;)
        {
            if (count[y] == 0)
            {
                y++;
                continue;
            }
            int start = y;
            while (y < bottom && count[y] != 0)
                y++;
            if (runs.Count > 0 && start - runs[^1].End <= 2)
                runs[^1] = (runs[^1].Start, y);
            else
                runs.Add((start, y));
        }
        return runs;
    }

    /// <summary>
    /// The lines of a run: its first line starts up to two rows above the run (no capital or tall letter on it) and its
    /// last ends up to two rows below (no descender); of these, the fit whose small letters' rows hold the most white.
    /// </summary>
    private static (int First, int Lines)? Fit(int start, int end, Func<int, int> count)
    {
        (int Score, int First, int Lines)? best = null;
        for (int k = 0; k < 3; k++)
        {
            int first = start - k;
            int lines = (end - first + LineHeight - 1) / LineHeight;
            int spare = first + lines * LineHeight - end;
            if (spare is < 0 or > 2)
                continue;
            int score = 0;
            for (int i = 0; i < lines; i++)
                for (int r = 0; r < LineHeight; r++)
                {
                    int c = count(first + i * LineHeight + r);
                    score += r is >= 2 and <= 6 ? c : -c;
                }
            if (best is null || score > best.Value.Score)
                best = (score, first, lines);
        }
        return best is { } b ? (b.First, b.Lines) : null;
    }

    /// <summary>
    /// A line's text: its pieces of white between empty columns, each looked up whole. Two empty columns or more
    /// part words (the patch kerned its spaces: "A talented" stands one column wider than its letters), but not
    /// before a full stop, a comma or a mark, nor after a hyphen. Two apostrophes side by side are a quotation mark.
    /// </summary>
    private static string? ReadLine(int number, int line, int y, Func<int, int, bool> white)
    {
        var text = new StringBuilder();
        int piece = 0, last = -1;
        for (int x = Left; x < Right;)
        {
            if (!Column(x, y, white))
            {
                x++;
                continue;
            }
            int start = x;
            while (x < Right && Column(x, y, white))
                x++;
            string? letters = Overrides.TryGetValue((number, line, piece), out var over) ? over
                : UnitDescriptionGlyphs.Lookup(Hash(start, x, y, white));
            if (letters is null)
                return null;
            if (last >= 0 && start - last >= 2 && letters[0] is not ('.' or ',' or '!' or '?')
                && (text.Length == 0 || text[^1] != '-'))
                text.Append(' ');
            text.Append(letters);
            last = x;
            piece++;
        }
        return text.ToString().Replace("''", "\"");
    }

    private static bool Column(int x, int y, Func<int, int, bool> white)
    {
        for (int r = 0; r < LineHeight; r++)
            if (white(x, y + r))
                return true;
        return false;
    }

    /// <summary>
    /// The FNV-1a 64 hash of a piece's pixels written as its rows, '#' for white and '.' for the rest, joined by '/' —
    /// the key of <see cref="UnitDescriptionGlyphs"/>, so the table holds no picture of the patch's font.
    /// </summary>
    public static ulong Hash(int start, int end, int y, Func<int, int, bool> white)
    {
        ulong hash = 0xCBF29CE484222325;
        for (int r = 0; r < LineHeight; r++)
        {
            if (r > 0)
                hash = Step(hash, '/');
            for (int x = start; x < end; x++)
                hash = Step(hash, white(x, y + r) ? '#' : '.');
        }
        return hash;

        static ulong Step(ulong hash, char c) => (hash ^ c) * 0x100000001B3;
    }

    /// <summary>
    /// A paragraph's lines as one text: a line ending in a hyphen is a word broken in two and joins the next without
    /// it, but for the hyphens of <see cref="KeptHyphens"/>; the font's capital I is its small l, read as I where a
    /// word starts with it and no English word could (alone, before an apostrophe or a consonant but l and y).
    /// </summary>
    private static string Join(int number, List<(int Line, string Text)> lines)
    {
        var text = new StringBuilder();
        int previous = -1;
        foreach (var (line, part) in lines)
        {
            if (text.Length > 0 && text[^1] == '-')
            {
                if (!KeptHyphens.Contains((number, previous)))
                    text.Length--;
            }
            else if (text.Length > 0)
                text.Append(' ');
            text.Append(part);
            previous = line;
        }
        return string.Join(' ', text.ToString().Split(' ').Select(w => CapitalI().IsMatch(w) ? "I" + w[1..] : w));
    }

    [GeneratedRegex("^l($|['nstfmcdrpgkbvz])")]
    private static partial Regex CapitalI();

    /// <summary>
    /// Pieces the pixels alone read wrong, by CG, line (from 0, the panel's lines counted across its paragraphs) and
    /// piece within the line (from 0): the touching r and n are the font's m to the pixel, and two capitals A stand
    /// as close to the next word as to a letter.
    /// </summary>
    private static readonly Dictionary<(int Cg, int Line, int Piece), string> Overrides = new()
    {
        [(603, 0, 0)] = "A ",
        [(624, 0, 0)] = "A ",
        [(634, 10, 12)] = "rn",
        [(636, 3, 5)] = "rn",
    };

    /// <summary>Lines (CG, line) whose closing hyphen is a word's own, kept when the next line joins it.</summary>
    private static readonly HashSet<(int Cg, int Line)> KeptHyphens = [(620, 1)];
}
