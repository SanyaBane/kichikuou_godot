using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Tests;

/// <summary>Draws from a script of values (1..n); <c>Y 4</c> with n ≤ 1 draws nothing, as the engine.</summary>
public sealed class QueuedRandom(params int[] values) : IScriptRandom
{
    private readonly Queue<int> _values = new(values);

    /// <summary>The n of every draw, in order.</summary>
    public List<int> Draws { get; } = [];

    /// <summary>When the script runs out: draw 1 instead of failing.</summary>
    public bool DefaultToOne { get; init; }

    public Sys3Int Next(Sys3Int n)
    {
        if (n.Value <= 1)
            return n;
        Draws.Add(n.Value);
        if (_values.Count == 0)
            return DefaultToOne ? (Sys3Int)1 : throw new InvalidOperationException($"Unexpected draw Y 4, {n.Value}.");
        return (Sys3Int)_values.Dequeue();
    }
}

/// <summary>Records every event.</summary>
public sealed class RecordingView : IScriptView
{
    public List<ScriptEvent> Events { get; } = [];

    /// <summary>Called after every event is recorded (e.g. to look at the state a message prints).</summary>
    public Action<ScriptEvent>? OnShow { get; set; }

    public Task ShowAsync(ScriptEvent scriptEvent)
    {
        Events.Add(scriptEvent);
        OnShow?.Invoke(scriptEvent);
        return Task.CompletedTask;
    }
}

/// <summary>
/// Answers the scene inputs from queues, with defaults when a queue is empty: key 0, mouse
/// released, the first menu item, a clock that advances 100 units per read. Fails after
/// <see cref="Budget"/> answers so a scene that loops on its input cannot hang a test.
/// </summary>
public sealed class ScriptedSceneInput : ISceneInput
{
    private int _timer;

    public Queue<int> Choices { get; } = new();

    public Queue<MouseState> Mouse { get; } = new();

    public Queue<int> Keys { get; } = new();

    /// <summary>The menus shown, in order.</summary>
    public List<IReadOnlyList<SceneMenuItem>> Menus { get; } = [];

    public int Budget { get; init; } = 100_000;

    public int Answers { get; private set; }

    public Task<Sys3Int> WaitForKeyAsync()
    {
        Pointer.Interrupt();
        return Answer((Sys3Int)(Keys.Count > 0 ? Keys.Dequeue() : 0));
    }

    /// <summary>Every <c>Y 3</c> pause: its ticks and whether a key may cut it short.</summary>
    public List<(int Ticks, bool Cancelable)> Pauses { get; } = [];

    public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
    {
        Pauses.Add((ticks.Value, cancelable));
        Pointer.Interrupt();
        return Answer(Sys3Int.Zero);
    }

    public Task<Sys3Int> GetPlayingTrackAsync() => Answer(Sys3Int.Zero);

    public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
    {
        Menus.Add(items);
        Pointer.Interrupt();
        return Answer(Choices.Count > 0 ? Choices.Dequeue() : 1);
    }

    /// <summary>Gestures read after <see cref="Mouse"/> runs out (the pointer at rest when both are empty).</summary>
    public PointerScript Pointer { get; } = new();

    /// <summary>The cursor shape <c>IC</c> set last (the engine starts at 1).</summary>
    public int Cursor { get; private set; } = 1;

    public Task<MouseState> GetMouseAsync() => Answer(Mouse.Count > 0 ? Mouse.Dequeue() : Pointer.Next(out _));

    public Task<Sys3Int> SetCursorAsync(Sys3Int shape)
    {
        int old = Cursor;
        Cursor = shape.Value;
        return Answer((Sys3Int)old);
    }

    public Task<IReadOnlyList<Sys3Int>> GetDateAsync() =>
        Answer<IReadOnlyList<Sys3Int>>([(Sys3Int)2026, (Sys3Int)10, (Sys3Int)8, (Sys3Int)12, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)5]);

    public Task ResetTimerAsync(Sys3Int value)
    {
        _timer = value.Value;
        return Answer(0);
    }

    public Task<Sys3Int> GetTimerAsync(int mode)
    {
        _timer += 100;
        return Answer((Sys3Int)(_timer & 0xffff));
    }

    public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() =>
        Answer<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);

    public Task<Sys3Int> GetMidiTrackAsync() => Answer(Sys3Int.Zero);

    private Task<T> Answer<T>(T value)
    {
        if (++Answers > Budget)
            throw new InvalidOperationException($"More than {Budget} inputs: the scene loops on its input.");
        return Task.FromResult(value);
    }
}

/// <summary>Records the calls into non-scene files and runs an optional action for each (e.g. a battle's outcome).</summary>
public sealed class RecordingRules : IStoryHooks
{
    public List<(string File, int Entry)> Calls { get; } = [];

    public Dictionary<string, Action<GameState>> Effects { get; } = new(StringComparer.Ordinal);

    public GameState? State { get; set; }

    public Task RunEventAsync(string file, Sys3Int entry)
    {
        Calls.Add((file, entry.Value));
        if (Effects.TryGetValue(file, out var effect))
            effect(State ?? throw new InvalidOperationException("No state."));
        return Task.CompletedTask;
    }
}

/// <summary>A game state with a scene runner over a scenario folder.</summary>
public sealed class SceneRig
{
    public SceneRig(string scenarioDirectory, QueuedRandom? random = null)
    {
        Random = random ?? new QueuedRandom();
        Rules.State = State;
        Runner = new SceneRunner(new Scripts.ScenarioScripts(scenarioDirectory), Rules);
        Script = new OriginalScriptContext(State, Random, View, Input, Runner);
        Runner.Bind(Script);
    }

    public GameState State { get; } = new();

    public QueuedRandom Random { get; }

    public RecordingView View { get; } = new();

    public ScriptedSceneInput Input { get; } = new();

    public RecordingRules Rules { get; } = new();

    public SceneRunner Runner { get; }

    public OriginalScriptContext Script { get; }

    /// <summary><c>Ev_Entry := entry; %#file</c>, as a rule file calls a scene.</summary>
    public Task EventAsync(string file, int entry) => Script.EventAsync(file, (Sys3Int)entry);
}
