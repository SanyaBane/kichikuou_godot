using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.TestSupport;

namespace Kichikuou.Engine.Tests.Script;

/// <summary>
/// A variant's state for the engine's own tests: 100 plain variables (none bound to a page), the
/// event entry in the last one, no page saved, no scene file, no scene skipped whole.
/// </summary>
public sealed class TestState() : ScriptState(Variant)
{
    /// <summary>The test variant's facts.</summary>
    public static readonly VariantProfile Variant = new()
    {
        VariableCount = 100,
        EventEntryVariable = 99,
        SavedPages = [],
        IsSceneFile = _ => false,
        WholeScenes = new HashSet<string>(),
    };

    protected override VariableBinding? BindingOf(int number) =>
        (uint)number < (uint)Variant.VariableCount ? null : throw new ArgumentOutOfRangeException(nameof(number));
}

/// <summary>The engine's script context over a fresh <see cref="TestState"/>: it draws nothing and calls no file.</summary>
public sealed class EngineRig
{
    public EngineRig() => Script = new ScriptContext(State, new NoRandom(), View, Input, new NoHooks());

    public TestState State { get; } = new();
    public RecordingView View { get; } = new();
    public PauseInput Input { get; } = new();
    public ScriptContext Script { get; }
}

/// <summary>Collects the events shown.</summary>
public sealed class RecordingView : IScriptView
{
    public List<ScriptEvent> Events { get; } = [];

    public Task ShowAsync(ScriptEvent scriptEvent)
    {
        Events.Add(scriptEvent);
        return Task.CompletedTask;
    }
}

/// <summary>Answers the <c>Y 3</c> pauses and records them; any other input is unexpected.</summary>
public sealed class PauseInput : UnexpectedInput
{
    /// <summary>Keys that cut pauses short; 0 (ran out) once empty.</summary>
    public Queue<int> PauseKeys { get; } = new();

    /// <summary>The ticks of every pause, in order.</summary>
    public List<int> Pauses { get; } = [];

    /// <summary>Whether a key may cut each pause short (the engine's <c>Y3waitFlags</c>), in order.</summary>
    public List<bool> PauseCancelables { get; } = [];

    public override Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
    {
        Pauses.Add(ticks.Value);
        PauseCancelables.Add(cancelable);
        return Task.FromResult(PauseKeys.Count > 0 ? (Sys3Int)PauseKeys.Dequeue() : Sys3Int.Zero);
    }
}
