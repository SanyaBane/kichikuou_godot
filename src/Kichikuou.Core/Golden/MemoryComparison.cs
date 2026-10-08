using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Golden;

/// <summary>One memory location where the core and the original differ.</summary>
/// <param name="Location">What differs, e.g. <c>VAR0517 Week</c> or <c>Units[3].Troops (page1[99])</c>.</param>
/// <param name="Expected">The original's value.</param>
/// <param name="Actual">The core's value.</param>
/// <param name="Ignored">Excluded by the <see cref="GoldenFilter"/> (listed, not a failure).</param>
public readonly record struct MemoryDifference(string Location, string Expected, string Actual, bool Ignored)
{
    /// <inheritdoc />
    public override string ToString() =>
        $"{Location}: original {Expected}, core {Actual}{(Ignored ? " (ignored)" : "")}";
}

/// <summary>
/// Which locations a golden comparison ignores (<c>docs/golden-tests.md</c>, "What the comparison
/// ignores"): the <c>ui</c> group of the dictionary, variables and page fields, except the
/// variables that are rule state despite the prefix.
/// </summary>
public sealed class GoldenFilter
{
    /// <summary><c>ui</c>-group variables that rules read, so they are compared.</summary>
    public static readonly IReadOnlySet<string> RuleStateUiVariables = new HashSet<string> { "Ui_CmdAttackAvail" };

    private readonly bool[] _ignoredVars;

    private GoldenFilter(bool[] ignoredVars) => _ignoredVars = ignoredVars;

    /// <summary>The filter the golden tests use.</summary>
    public static GoldenFilter Default { get; } = new(
        VariableCatalog.Variables
            .Select(v => v.Group == "ui" && !RuleStateUiVariables.Contains(v.Name))
            .ToArray());

    /// <summary>Compares everything.</summary>
    public static GoldenFilter None { get; } = new(new bool[ScriptMemory.VariableCount]);

    /// <summary>Whether a variable (page 0) is ignored.</summary>
    public bool IgnoresVar(int number) => number < _ignoredVars.Length && _ignoredVars[number];

    /// <summary>Whether a page slot is ignored: it belongs to a field of an ignored bound variable.</summary>
    public bool IgnoresSlot(int page, int index) =>
        VariableCatalog.FindSlot(page, index) is { } binding && IgnoresVar(binding.Var);
}

/// <summary>Compares the core's memory with a dump.</summary>
public static class MemoryComparison
{
    /// <summary>
    /// Every location where <paramref name="actual"/> differs from <paramref name="expected"/>:
    /// variables 0..805 (and non-zero variables beyond), every page slot and size, every string,
    /// and the generator state when <paramref name="generator"/> is given.
    /// </summary>
    public static List<MemoryDifference> Compare(MemoryDump expected, ScriptMemory actual, Mt19937Legacy? generator,
        GoldenFilter filter)
    {
        var diffs = new List<MemoryDifference>();

        for (int i = 0; i < Math.Max(expected.Vars.Length, ScriptMemory.VariableCount); i++)
        {
            ushort e = i < expected.Vars.Length ? expected.Vars[i] : (ushort)0;
            ushort a = i < ScriptMemory.VariableCount ? actual.Var(i).Value : (ushort)0;
            if (e != a)
                diffs.Add(new(VariableCatalog.DescribeVar(i), e.ToString(), a.ToString(), filter.IgnoresVar(i)));
        }

        var pages = new SortedSet<int>(expected.Pages.Keys);
        pages.UnionWith(actual.AllocatedPages);
        foreach (int page in pages)
        {
            var e = expected.Pages.GetValueOrDefault(page);
            int actualSize = actual.PageSize(page);
            if ((e?.Length ?? 0) != actualSize)
                diffs.Add(new($"page {page} size", Size(e?.Length ?? 0), Size(actualSize), false));
            int size = Math.Max(e?.Length ?? 0, actualSize);
            for (int i = 0; i < size; i++)
            {
                ushort ev = e is not null && i < e.Length ? e[i] : (ushort)0;
                ushort av = actual.ReadPage(page, i).Value;
                if (ev != av)
                {
                    diffs.Add(new($"{VariableCatalog.DescribeSlot(page, i)} (page{page}[{i}])",
                        ev.ToString(), av.ToString(), filter.IgnoresSlot(page, i)));
                }
            }
        }

        int strings = Math.Max(actual.Strings.Length, expected.Strings.Count == 0 ? 0 : expected.Strings.Keys.Max() + 1);
        for (int i = 0; i < strings; i++)
        {
            string e = expected.Strings.GetValueOrDefault(i, string.Empty);
            string a = i < actual.Strings.Length ? actual.Strings[i] : string.Empty;
            if (e != a)
                diffs.Add(new($"string {i}", Quote(e), Quote(a), false));
        }

        if (generator is not null)
        {
            if (generator.StateIndex != expected.RngIndex || !generator.CopyState().AsSpan().SequenceEqual(expected.RngState))
            {
                diffs.Add(new("generator state", $"mti {expected.RngIndex}", $"mti {generator.StateIndex}", false));
            }
        }
        return diffs;
    }

    private static string Size(int size) => size == 0 ? "not allocated" : size.ToString();

    private static string Quote(string s) => $"\"{s}\"";
}
