using System.Text.RegularExpressions;
using Kichikuou.Content;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// The scenario's own sums of gold as our message windows and menus write them (owner, 2026-10-24): the thousands
/// separated as <see cref="Figures"/> does, only at the places docs/ui/gold-figures.md verified to be money, by
/// file and line. A literal's figures before <c>K</c> (and the troops on the merchant's lines) are grouped; a figure
/// printed by <c>H 0</c> is grouped together with the <c>00</c> its next text line starts with ("777" + "00KG" →
/// "77,700KG", a zero "0KG"). The <c>H 3</c> / <c>H 5</c> places and the original's screens keep the original's
/// numbers, and so do <c>COMMAND</c>'s status window and <c>MAIN</c>'s week windows: our status strip and week report
/// read their figures from that text and write them themselves.
/// </summary>
public static partial class ScenarioFigures
{
    /// <summary>Text lines whose figures are money (docs/ui/gold-figures.md, §3.1).</summary>
    public static readonly IReadOnlySet<ScriptRef> Literals = Refs(
        ("Cｼｮｳﾆﾝ.ADV", [252, 254, 267, 271, 589, 591, 604, 608, 1303, 1315, 1344, 1358, 1372, 1386, 1399]),
        ("Cﾃｲﾄ.ADV", [343, 430, 532, 713, 818, 908]),
        ("Cﾃｷﾄｼ.ADV", [2306, 3491]),
        ("Gｶﾊﾟｰﾗ.ADV", [2039, 2045, 2051, 2123, 2146, 2169]),
        ("Gﾄｳｹﾞﾝ.ADV", [111, 139, 224]),
        ("Gﾚﾍﾞｯｶ.ADV", [34, 46]),
        ("Iﾄｳｿﾞｸ.ADV", [688, 702]));

    /// <summary>The <c>H 0</c> lines that print the treasury or a price in 100K (§3.2), each followed by <c>00K…</c>.</summary>
    public static readonly IReadOnlySet<ScriptRef> Printed = Refs(
        ("Cｼｮｳﾆﾝ.ADV", [257, 594, 1077, 1328]),
        ("Cｼﾊｲﾄｼ.ADV", [890, 1532, 1548, 1566, 1574]),
        ("Cﾃｲﾄ.ADV", [346, 433, 535, 716, 821, 911]),
        ("Cﾃｷﾄｼ.ADV", [2310, 3495]),
        ("Cﾌﾞｶ.ADV", [876]),
        ("Gｶﾊﾟｰﾗ.ADV", [2027]),
        ("Gﾄｳｹﾞﾝ.ADV", [148]),
        ("Gﾌﾚｲｱ.ADV", [3073]),
        ("Gﾚﾍﾞｯｶ.ADV", [52]),
        ("Iﾄｳｿﾞｸ.ADV", [698, 711]));

    /// <summary>
    /// <paramref name="run"/> with the figures at the verified places written as text, and the numbers left for its
    /// other <c>H</c>s; <paramref name="run"/> and <paramref name="numbers"/> themselves where it has none.
    /// </summary>
    public static (TextRun Run, IReadOnlyList<Sys3Int> Numbers) Group(TextRun run, IReadOnlyList<Sys3Int> numbers) =>
        Group(run, numbers, Figures.Separator);

    /// <inheritdoc cref="Group(TextRun, IReadOnlyList{Sys3Int})"/>
    public static (TextRun Run, IReadOnlyList<Sys3Int> Numbers) Group(
        TextRun run, IReadOnlyList<Sys3Int> numbers, ThousandsSeparator separator)
    {
        if (run.SegmentLines.Count != run.Segments.Count)
            return (run, numbers);
        string file = run.Source.File;
        var segments = new List<TextSegment>(run.Segments.Count);
        var left = new List<Sys3Int>(numbers.Count);
        bool changed = false;
        int next = 0;
        for (int i = 0; i < run.Segments.Count; i++)
        {
            var at = new ScriptRef(file, run.SegmentLines[i]);
            switch (run.Segments[i])
            {
                case NumberSlot { Width: 0 } when Printed.Contains(at) && next < numbers.Count
                                                  && i + 1 < run.Segments.Count && run.Segments[i + 1] is LiteralText { Text: var suffix }
                                                  && suffix.StartsWith("00", StringComparison.Ordinal):
                    segments.Add(new LiteralText(Number(numbers[next++].Value * 100, separator) + suffix[2..]));
                    i++;
                    changed = true;
                    break;
                case LiteralText { Text: var text } when Literals.Contains(at):
                    segments.Add(new LiteralText(Figure().Replace(Hankaku.ToHalfWidth(text),
                        m => Number(int.Parse(m.Value), separator))));
                    changed = true;
                    break;
                case NumberSlot n:
                    segments.Add(n);
                    if (next < numbers.Count)
                        left.Add(numbers[next++]);
                    break;
                case var segment:
                    segments.Add(segment);
                    break;
            }
        }
        return changed ? (new TextRun(run.Source, segments), left) : (run, numbers);
    }

    /// <summary>
    /// <see cref="Figures.Number(int, ThousandsSeparator)"/> with a half-width blank for its no-break space: the
    /// message windows' cells count every character outside ASCII as full width.
    /// </summary>
    private static string Number(int n, ThousandsSeparator separator) =>
        Figures.Number(n, separator).Replace(' ', ' ');

    /// <summary>A figure before <c>K</c> (a sum) or <c>" Troops"</c> (the merchant's offers).</summary>
    [GeneratedRegex(@"[0-9]+(?=K| Troops)")]
    private static partial Regex Figure();

    private static HashSet<ScriptRef> Refs(params (string File, int[] Lines)[] places) =>
        places.SelectMany(p => p.Lines.Select(line => new ScriptRef(p.File, line))).ToHashSet();
}
