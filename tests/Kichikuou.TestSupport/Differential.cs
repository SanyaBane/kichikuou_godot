using System.Text;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.TestSupport;

/// <summary>
/// What a differential test compares: a port against its file run from the decompile, each on a
/// copy of one random state (<see cref="ScriptState.Copy"/>), logging every input, draw, event and
/// call; the two logs and the two memories after the runs must match.
/// </summary>
public static class Differential
{
    /// <summary>The first entry where the logs differ (the shorter one's length when it is a prefix of the other), or -1.</summary>
    public static int FirstDifference(List<string> a, List<string> b)
    {
        for (int i = 0; i < Math.Min(a.Count, b.Count); i++)
            if (a[i] != b[i])
                return i;
        return a.Count == b.Count ? -1 : Math.Min(a.Count, b.Count);
    }

    /// <summary>The logs around entry <paramref name="at"/>, each entry of <paramref name="file"/>'s run above the port's.</summary>
    public static string Around(string file, List<string> original, List<string> port, int at)
    {
        var sb = new StringBuilder();
        for (int i = Math.Max(0, at - 8); i < at + 3; i++)
        {
            sb.Append($"  {i,5} {file}: {(i < original.Count ? original[i] : "-")}\n");
            sb.Append($"  {i,5} port: {(i < port.Count ? port[i] : "-")}\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Where the port's memory differs from <paramref name="file"/>'s run: the variables (with
    /// <paramref name="name"/> of each), the page slots, the strings.
    /// </summary>
    public static IEnumerable<string> Differences(string file, ScriptMemory expected, ScriptMemory actual, Func<int, string> name)
    {
        for (int v = 0; v < expected.VariableCount; v++)
            if (expected.Var(v) != actual.Var(v))
                yield return $"VAR{v:D4} {name(v)}: {file} {expected.Var(v).Value}, port {actual.Var(v).Value}";
        foreach (int page in expected.AllocatedPages)
            for (int i = 0; i < expected.PageSize(page); i++)
                if (expected.ReadPage(page, i) != actual.ReadPage(page, i))
                    yield return $"page {page}[{i}]: {file} {expected.ReadPage(page, i).Value}, port {actual.ReadPage(page, i).Value}";
        for (int s = 0; s < expected.Strings.Length; s++)
            if (expected.Strings[s] != actual.Strings[s])
                yield return $"string {s}: {file} \"{expected.Strings[s]}\", port \"{actual.Strings[s]}\"";
    }
}
