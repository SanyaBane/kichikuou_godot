using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.TestSupport;

/// <summary>
/// The files a differential test does not run, as hooks that scribble on the state: each call is
/// checked against the variant's entry variable, logged with what the test describes of the state,
/// then the test writes what the called file would write, drawing from a generator of
/// (seed, call) — the same writes for the same call in both runs.
/// </summary>
public abstract class ScribblingHooks<TState>(TState state, int seed, List<string> log) : IStoryHooks
    where TState : ScriptState
{
    /// <summary>The state the hooks write.</summary>
    protected TState State => state;

    /// <summary>The run's log.</summary>
    protected List<string> Log => log;

    /// <summary>The calls so far.</summary>
    public int Calls { get; private set; }

    public Task RunEventAsync(string file, Sys3Int entry)
    {
        var expected = state.ReadVariable(state.Profile.EventEntryVariable);
        if (expected != entry)
            throw new InvalidOperationException($"{file}: entry variable {expected.Value}, entry {entry.Value}");
        Arrive(file);
        log.Add($"call {file} entry {entry} {Describe()}");
        Scribble(file, entry, new Random(seed * 1000 + ++Calls));
        return Task.CompletedTask;
    }

    /// <summary>Before a call is logged (a test may stop the run here).</summary>
    protected virtual void Arrive(string file)
    {
    }

    /// <summary>What the log notes of the state at a call.</summary>
    protected abstract string Describe();

    /// <summary>The writes of a call to <paramref name="file"/> at <paramref name="entry"/>, drawn from <paramref name="rng"/>.</summary>
    protected abstract void Scribble(string file, Sys3Int entry, Random rng);
}
