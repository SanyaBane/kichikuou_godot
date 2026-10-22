namespace Kichikuou.Engine.Script;

/// <summary>
/// A rule set's patches inside the scenes the <see cref="SceneRunner"/> runs from the player's files: where an owner's
/// decision changes what one statement of a scene does, the rules answer a test (<see cref="ConditionAsync"/>) or take
/// a step of their own after a statement (<see cref="AfterAsync"/>). Ours (the original's decision 30 is the first:
/// Kanami's choice in Code's event 4, Kentarou's hint lines; docs/ui/talk-marks.md §8 step 6). The places are fixed, so
/// the runner marks them once per program and asks nothing elsewhere; whether a patch acts is the rules' answer at
/// the moment (an option of the player's), so a hook leaves the scene as the original where it does not.
/// </summary>
/// <remarks>
/// A place is a statement's <see cref="ScriptStatement.At"/>: every statement of that line is asked (the decompile
/// writes one statement a line, so it is one). The dry run (<see cref="ScenePreview{TState}"/>) builds its runner
/// without hooks.
/// </remarks>
public interface ISceneHooks
{
    /// <summary>The tests (<see cref="IfStatement"/>s, <c>{cond:</c>) whose condition the rules answer with <see cref="ConditionAsync"/>.</summary>
    IReadOnlyCollection<ScriptRef> Conditions { get; }

    /// <summary>The statements after which the rules take a step of their own with <see cref="AfterAsync"/>.</summary>
    IReadOnlyCollection<ScriptRef> Steps { get; }

    /// <summary>
    /// The test at <paramref name="at"/>, of which the scenario's answer is <paramref name="holds"/> (its condition
    /// evaluated, nonzero): returns the answer the scene goes on with — true runs the test's block, false skips it.
    /// </summary>
    Task<bool> ConditionAsync(ScriptContext script, ScriptRef at, bool holds);

    /// <summary>
    /// The statement at <paramref name="at"/> has run (and moved on as it does: a jump or a call has been made); the next
    /// statement runs after it. Not reached after a statement that leaves the page (<c>%0:</c>, a jump to another file).
    /// </summary>
    Task AfterAsync(ScriptContext script, ScriptRef at);
}
