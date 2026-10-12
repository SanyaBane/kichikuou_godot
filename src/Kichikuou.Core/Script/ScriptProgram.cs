using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

/// <summary>
/// One scenario file as a goto program (<see cref="ScriptStatement"/>s with resolved jump
/// targets), read from the player's decompile at run time: a story scene runs from statement 0
/// with the scene number set by its caller (the original's <c>Ev_Entry</c>), exactly as the
/// original page call does.
/// </summary>
public sealed class ScriptProgram
{
    /// <summary>A program of <paramref name="file"/>.</summary>
    public ScriptProgram(string file, IReadOnlyList<ScriptStatement> statements, IReadOnlyDictionary<string, int> labels)
    {
        File = file;
        Statements = statements;
        Labels = labels;
    }

    /// <summary>The scenario file, e.g. <c>Gｿｳﾙ.ADV</c>.</summary>
    public string File { get; }

    /// <summary>The statements in file order; jump targets index this list (the count = past the end).</summary>
    public IReadOnlyList<ScriptStatement> Statements { get; }

    /// <summary>Every label of the file → the index of the statement after it.</summary>
    public IReadOnlyDictionary<string, int> Labels { get; }

    /// <summary>
    /// The dispatch table at the top of the file, if any: every <c>{Ev_Entry = n: … @label: }</c>
    /// block before the file's first label (some set a flag before the jump, e.g.
    /// <c>Gｴｸｽ.ADV:1-3</c>; some files run statements before the table, e.g. <c>GAL.ADV</c>), in
    /// order, where <paramref name="entryVariable"/> is the variable holding the scene number (the
    /// variant's <see cref="VariantProfile.EventEntryVariable"/>). Information only — running the
    /// program performs the same dispatch, and an unmatched <c>Ev_Entry</c> falls through into the
    /// code after the table (the default scene).
    /// </summary>
    public IReadOnlyList<SceneEntry> FindEntries(int entryVariable)
    {
        var entries = new List<SceneEntry>();
        int firstLabel = Labels.Count > 0 ? Labels.Values.Min() : Statements.Count;
        for (int i = 0; i < firstLabel; i++)
        {
            if (Statements[i] is IfStatement
                {
                    Condition: BinaryExpr { Op: "=", Left: VarExpr v, Right: ConstExpr entry },
                } test
                && v.Var == entryVariable
                && test.EndTarget > i + 1
                && Statements[test.EndTarget - 1] is GotoStatement jump)
                entries.Add(new SceneEntry(entry.Value, jump.Label, jump.Target));
        }
        return entries;
    }
}

/// <summary>One line of a scene file's dispatch table: <c>Ev_Entry = <paramref name="Entry"/></c> → <paramref name="Label"/>.</summary>
public sealed record SceneEntry(Sys3Int Entry, string Label, int Target);

/// <summary>Reads scenario files as programs from the player's decompile.</summary>
public interface ISceneSource
{
    /// <summary>The whole of <paramref name="file"/> as a program.</summary>
    /// <exception cref="ScenarioFormatException">The file holds a form outside the supported ones (the message names the line).</exception>
    ScriptProgram ReadProgram(string file);
}
