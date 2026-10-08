using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>Returns queued values for <c>Y 4, n</c> and records every request.</summary>
public sealed class ScriptedRandom : IScriptRandom
{
    private readonly Queue<int> _values;

    public ScriptedRandom(params int[] values) => _values = new Queue<int>(values);

    /// <summary>The n of every draw that consumed a value, in order.</summary>
    public List<int> Draws { get; } = [];

    /// <summary>Values not consumed yet.</summary>
    public int Remaining => _values.Count;

    public Sys3Int Next(Sys3Int n)
    {
        if (n.Value <= 1)
            return n;
        Draws.Add(n.Value);
        if (_values.Count == 0)
            throw new InvalidOperationException($"Unexpected draw Y 4,{n} after {Draws.Count - 1} draws.");
        int value = _values.Dequeue();
        if (value < 1 || value > n.Value + 1)
            throw new InvalidOperationException($"Scripted value {value} is out of range for Y 4,{n}.");
        return (Sys3Int)value;
    }
}

/// <summary>Collects presentation events.</summary>
public sealed class RecordingView : IScriptView
{
    public List<ScriptEvent> Events { get; } = [];

    public Task ShowAsync(ScriptEvent scriptEvent)
    {
        Events.Add(scriptEvent);
        return Task.CompletedTask;
    }
}

/// <summary>Records story calls without running them.</summary>
public sealed class RecordingStory : IStoryHooks
{
    public List<(string File, int Entry)> Calls { get; } = [];

    public Task RunEventAsync(string file, Sys3Int entry)
    {
        Calls.Add((file, entry.Value));
        return Task.CompletedTask;
    }
}

/// <summary>Assertions on the <c>WAO_SUB.ADV</c> calls a test saw.</summary>
internal static class WindowAssert
{
    /// <summary>The event is a call of window operation <paramref name="op"/>.</summary>
    public static void Op(int op, ScriptEvent e) => Assert.Equal(op, (int)Assert.IsType<WindowEvent>(e).Op);

    /// <summary>The event opens a speaker or narration window (ops 141-153).</summary>
    public static void Opens(ScriptEvent e) => Assert.InRange((int)Assert.IsType<WindowEvent>(e).Op, 141, 153);
}
