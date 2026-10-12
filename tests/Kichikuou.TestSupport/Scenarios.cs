using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.TestSupport;

/// <summary>The file under test, read once: every other file a scene runner reads is a hook here.</summary>
public sealed class OneProgram(ScriptProgram program) : ISceneSource
{
    public ScriptProgram ReadProgram(string file) =>
        file == program.File ? program : throw new InvalidOperationException($"{file} is a hook here.");
}

/// <summary>
/// The literals (<c>MS n, "…"</c>) as "file:line", standing in for the player's files: every
/// statement read sets string <c>n</c> (<paramref name="stringVariable"/> of its reference) to the
/// reference itself.
/// </summary>
public sealed class Literals(Func<ScriptRef, int> stringVariable) : IScenarioSource
{
    /// <summary>Every statement read sets string <paramref name="stringVariable"/>.</summary>
    public Literals(int stringVariable) : this(_ => stringVariable)
    {
    }

    public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

    public ScriptStatement ReadStatement(ScriptRef at) => new SetStringStatement(at, stringVariable(at), at.ToString());
}

/// <summary>No player's files: a code path that reads one fails.</summary>
public sealed class NoScenario : IScenarioSource
{
    public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

    public ScriptStatement ReadStatement(ScriptRef at) => throw new InvalidOperationException(at.ToString());
}

/// <summary>No other file runs: a call is a divergence.</summary>
public sealed class NoHooks : IStoryHooks
{
    public Task RunEventAsync(string file, Sys3Int entry) => throw new InvalidOperationException($"Unexpected call of {file}.");
}
