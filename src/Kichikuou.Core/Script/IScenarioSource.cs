namespace Kichikuou.Core.Script;

/// <summary>
/// Scenario code the port reads from the player's decompiled scenario at run time instead of
/// committing it: the record data files and the literals of hand-ported files.
/// </summary>
public interface IScenarioSource
{
    /// <summary>
    /// The statements of a straight-line data file (<c>SET_MAP.ADV</c>, <c>SET_CHR.ADV</c>,
    /// <c>SET_ITEM.ADV</c>), up to and including its return.
    /// </summary>
    /// <exception cref="ScenarioFormatException">The file holds a statement outside the supported forms.</exception>
    IReadOnlyList<ScriptStatement> ReadFile(string file);

    /// <summary>The single statement at <paramref name="at"/> (e.g. an <c>MS n, "text"</c> of a hand-ported file).</summary>
    ScriptStatement ReadStatement(ScriptRef at);

    /// <summary>
    /// The straight-line body of a label from <paramref name="at"/> up to and including its return
    /// (<c>\0:</c>) — record data such as <c>Cﾒｲｷｭｳ</c>'s commander setters: assignments, <c>MS</c>,
    /// <c>MM</c> and <c>R</c> (a <see cref="LineBreakStatement"/>); <c>ZH n:</c> is left out.
    /// </summary>
    /// <exception cref="ScenarioFormatException">The body holds another statement, or no return.</exception>
    IReadOnlyList<ScriptStatement> ReadBlock(ScriptRef at) =>
        throw new InvalidOperationException($"{at}: this scenario source reads no blocks.");
}
