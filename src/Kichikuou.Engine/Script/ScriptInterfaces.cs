using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>A location in the decompiled scenario, used to identify a message or scene text.</summary>
/// <param name="File">Scenario file name as decompiled, e.g. <c>FIGT_M.ADV</c>.</param>
/// <param name="Line">1-based line in the raw decompile (<c>_extract\scenario</c>).</param>
public readonly record struct ScriptRef(string File, int Line)
{
    /// <inheritdoc />
    public override string ToString() => $"{File}:{Line}";
}

/// <summary>
/// Something a script shows; the presentation layer decides how. Events of one part only
/// (e.g. <c>Combat.BattleEvent</c>) derive from it next to the code that raises them.
/// </summary>
public abstract record ScriptEvent;

/// <summary>Receives the presentation events in script order.</summary>
public interface IScriptView
{
    /// <summary>Shows one event; the script continues when the task completes.</summary>
    Task ShowAsync(ScriptEvent scriptEvent);
}

/// <summary>
/// The engine's input commands every part uses. Each method models one command and answers
/// with the value the script receives; a part's own decisions (e.g. <c>Combat.IBattleInput</c>)
/// extend it.
/// </summary>
public interface IScriptInput
{
    /// <summary>
    /// <c>IK 0</c> (message waits): waits for a key or click and returns the key code the engine
    /// stores in <c>RetVal</c> (0 while message skip is on).
    /// </summary>
    Task<Sys3Int> WaitForKeyAsync();

    /// <summary>
    /// <c>Y 3, n</c>: a timed pause of <paramref name="ticks"/> × 16 ms; returns the key code the
    /// engine stores in <c>RetVal</c> — what is held at the last poll (0 if nothing). When
    /// <paramref name="cancelable"/> (<c>ScriptContext.PauseCancelable</c>), a key or click cuts
    /// it short; otherwise it runs out. Never called for <c>Y 3, 10000/10001</c>, which only set
    /// the flag.
    /// </summary>
    Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable);

    /// <summary>
    /// <c>SR 0, var</c>: engine query of the music track that is playing, stored by the
    /// wrappers before a jingle and replayed afterwards with <c>SS var - 1</c>.
    /// </summary>
    Task<Sys3Int> GetPlayingTrackAsync();
}

/// <summary>
/// A generator that also hears where a draw is made: <see cref="ScriptContext.Rnd"/> calls
/// <see cref="Next(Sys3Int, ScriptRef)"/> for a scene's <c>Y 4, n</c> (its statement's place) and the plain
/// <see cref="IScriptRandom.Next"/> for a hand port's. Ours: the dry run's decisions name their draws
/// (<see cref="ScenePreview{TState}"/>, docs/ui/talk-marks.md §8).
/// </summary>
public interface IPlacedRandom : IScriptRandom
{
    /// <summary><c>Y 4, n</c> at <paramref name="at"/>: as <see cref="IScriptRandom.Next"/>.</summary>
    Sys3Int Next(Sys3Int n, ScriptRef at);
}

/// <summary>Calls from ported rules into non-rule scenario files (story and death scenes, unported screens).</summary>
public interface IStoryHooks
{
    /// <summary>
    /// <c>Ev_Entry := entry; call file</c> — a story scene that may change any state.
    /// </summary>
    Task RunEventAsync(string file, Sys3Int entry);
}

/// <summary>
/// <c>&amp;#FILE.ADV:</c> — the script jumped to another scenario file and never comes back (e.g.
/// the game over at <c>FIGTﾐｶDE.ADV:39</c>). Unwinds the running code; the caller runs <see cref="File"/>.
/// </summary>
public sealed class ScenarioJumpException(string file)
    : Exception($"The scenario jumped to {file}.")
{
    /// <summary>The scenario file the script jumped to, e.g. <c>OVER.ADV</c>.</summary>
    public string File { get; } = file;
}

/// <summary>
/// <c>ZZ0, 0:</c> — the engine quits (<c>cmdz.c</c>: <c>sys_exit(RetVal)</c>); the scenario does so
/// only at the end of <c>OVER.ADV</c>, after an ending. Unwinds the running code; the caller ends the
/// game (STR-10: the port goes back to the title instead, owner's decision 19).
/// </summary>
public sealed class GameQuitException(Sys3Int exitCode)
    : Exception($"The scenario quit the engine (exit code {exitCode.Value}).")
{
    /// <summary>The process's exit code the original would return: <c>RetVal</c> at the <c>ZZ0</c>.</summary>
    public Sys3Int ExitCode { get; } = exitCode;
}
