namespace Kichikuou.Core.Script;

/// <summary>
/// The facts the engine takes from a game variant (docs/variants.md), gathered in one instance per
/// variant instead of constants in the runtime. A variant's state type gives it
/// (<see cref="ScriptState.Profile"/>), so whatever runs on a state knows its variant.
/// </summary>
public sealed class VariantProfile
{
    /// <summary>The numbered variables the scenario declares: page 0 is <c>VAR0000</c> up to this count less one.</summary>
    public required int VariableCount { get; init; }

    /// <summary>
    /// The variable a caller sets to the scene number before the page call of a scene file, which
    /// the file's dispatch table tests (<see cref="ScriptProgram.FindEntries"/>; the original's <c>Ev_Entry</c>).
    /// </summary>
    public required int EventEntryVariable { get; init; }

    /// <summary>
    /// The array pages the boot allocates with the save flag (<c>DC n, max, 1</c>): the pages a slot
    /// keeps (<see cref="SaveData.Capture"/>) and <c>QP</c> may write.
    /// </summary>
    public required IReadOnlyList<int> SavedPages { get; init; }

    /// <summary>Whether a called file runs from the player's files as a scene (<see cref="SceneRunner"/>); any other goes to the rules.</summary>
    public required Func<string, bool> IsSceneFile { get; init; }

    /// <summary>The scenes the message skip skips whole instead of fast-forwarding (<see cref="MessageSkip.SkipsScene"/>).</summary>
    public required IReadOnlySet<string> WholeScenes { get; init; }
}
