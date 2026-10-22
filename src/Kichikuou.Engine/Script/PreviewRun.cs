using System.Diagnostics.CodeAnalysis;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>
/// One run of the dry run (<see cref="ScenePreview{TState}"/>): the parts a caller builds its context over — the
/// state copy (<see cref="PreviewRun{TState}.State"/>), the scripted generator (<see cref="Random"/>), the recording
/// view (<see cref="View"/>), the scripted input (<see cref="Input"/>, or a caller's input derived from
/// <see cref="PreviewInput"/>), the save store that ends the path (<see cref="Saves"/>) — and what the run has met so
/// far: its decisions and its lines.
/// </summary>
public abstract class PreviewRun
{
    private readonly PreviewDecision[] _prefix;
    private readonly int _maxSteps;
    private readonly Stack<PreviewDecision[]> _pending;
    private readonly List<PreviewDecision> _decisions = [];
    private readonly List<ScriptRef> _lines = [];
    private int _steps;

    private protected PreviewRun(ScriptState state, PreviewDecision[] prefix, int maxSteps, Stack<PreviewDecision[]> pending)
    {
        StateCopy = state;
        _prefix = prefix;
        _maxSteps = maxSteps;
        _pending = pending;
        Random = new ScriptedRandom(this);
        View = new RecordingView(this);
        Input = new PreviewInput(this);
        Saves = new StoppingSaves(this);
    }

    /// <summary>The run's copy of the state.</summary>
    private protected ScriptState StateCopy { get; }

    /// <summary>The generator the run's context draws with: each draw of 2 or more is a decision.</summary>
    public IPlacedRandom Random { get; }

    /// <summary>The view the run's context shows on: it notes the lines and drops every event.</summary>
    public IScriptView View { get; }

    /// <summary>
    /// The run's scene input (<see cref="PreviewInput"/>). A caller whose rules want more of the input (the original's
    /// battle input) derives its own from <see cref="PreviewInput"/> over this run and gives that to its context instead.
    /// </summary>
    public PreviewInput Input { get; }

    /// <summary>
    /// A save store for the run's context (<see cref="ScriptContext.Saves"/>): any read or write of it ends the path
    /// (<see cref="PreviewEnd.Stopped"/>, "save data"), so a preview never touches a slot and never plays on with a slot it
    /// cannot read. A talk or a visit never saves.
    /// </summary>
    public ISaveStore Saves { get; }

    /// <summary>The decisions made so far, in order.</summary>
    public IReadOnlyList<PreviewDecision> Decisions => _decisions;

    /// <summary>The lines shown so far, in order (<see cref="PreviewPath.Lines"/>).</summary>
    public IReadOnlyList<ScriptRef> Lines => _lines;

    /// <summary>
    /// Ends the path here as <see cref="PreviewEnd.Stopped"/> with <paramref name="reason"/>: for the caller's hooks at a
    /// call the preview does not run (a battle, <c>FIGT_A</c>), which counts as something happening.
    /// </summary>
    [DoesNotReturn]
    public void Stop(string reason) => throw new PreviewStoppedException(this, reason);

    /// <summary>
    /// One step of the run — an event, an input, a draw; past the preview's <see cref="ScenePreview{TState}.MaxSteps"/>
    /// the path ends as <see cref="PreviewEnd.Aborted"/>. The run's own parts count theirs; a caller's input counts the
    /// commands it adds.
    /// </summary>
    public void Step()
    {
        if (++_steps > _maxSteps)
            throw new PreviewAbortedException(this, $"more than {_maxSteps} steps");
    }

    /// <summary>
    /// A decision of <paramref name="kind"/> among <paramref name="count"/>: the prefix's while it lasts (checked to
    /// fit), else the first value, the others queued as new prefixes (in the order of their values, the first on top).
    /// </summary>
    internal int Decide(PreviewDecisionKind kind, int count, ScriptRef? at)
    {
        Step();
        int index = _decisions.Count;
        int value;
        if (index < _prefix.Length)
        {
            var wanted = _prefix[index];
            if (wanted.Kind != kind || wanted.Count != count || wanted.At is { } place && at is { } here && place != here
                || Array.IndexOf(Values(kind, count), wanted.Value) < 0)
                throw new InvalidOperationException(
                    $"Decision {index + 1} of the prefix ({wanted}) does not fit the run's: a {kind} of {count}{(at is { } a ? $" at {a}" : "")}.");
            value = wanted.Value;
        }
        else
        {
            var values = Values(kind, count);
            value = values[0];
            for (int i = values.Length - 1; i >= 1; i--)
                _pending.Push([.. _decisions, new PreviewDecision(kind, count, values[i], at)]);
        }
        _decisions.Add(new PreviewDecision(kind, count, value, at));
        return value;
    }

    /// <summary>The values of a decision in the order they are run: a draw's 1..n, a menu's items 1..n, then its cancel.</summary>
    private static int[] Values(PreviewDecisionKind kind, int count) => kind == PreviewDecisionKind.Draw
        ? [.. Enumerable.Range(1, count)]
        : [.. Enumerable.Range(1, count), 0];

    /// <summary>Notes a line shown.</summary>
    internal void Line(ScriptRef line) => _lines.Add(line);

    /// <summary>The path ended before it used its whole prefix: the forced decisions do not fit.</summary>
    internal void CheckPrefixUsed()
    {
        if (_decisions.Count < _prefix.Length)
            throw new InvalidOperationException(
                $"The path made {_decisions.Count} decisions, the prefix has {_prefix.Length} (the next: {_prefix[_decisions.Count]}).");
    }

    /// <summary><c>Y 4, n</c>: n of 0 or 1 gives n without a decision, as the engine draws nothing then.</summary>
    private sealed class ScriptedRandom(PreviewRun run) : IPlacedRandom
    {
        public Sys3Int Next(Sys3Int n) => Draw(n, null);

        public Sys3Int Next(Sys3Int n, ScriptRef at) => Draw(n, at);

        private Sys3Int Draw(Sys3Int n, ScriptRef? at)
        {
            if (n.Value <= 1)
            {
                run.Step();
                return n;
            }
            return (Sys3Int)run.Decide(PreviewDecisionKind.Draw, n.Value, at);
        }
    }

    /// <summary>Notes the line of every message (<see cref="MessageEvent"/>, <see cref="ValueMessageEvent"/>) and drops the events.</summary>
    private sealed class RecordingView(PreviewRun run) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            run.Step();
            switch (scriptEvent)
            {
                case MessageEvent m:
                    run.Line(m.Text);
                    break;
                case ValueMessageEvent m:
                    run.Line(m.Text);
                    break;
            }
            return Task.CompletedTask;
        }
    }

    /// <summary>Ends the path at any touch of the save data.</summary>
    private sealed class StoppingSaves(PreviewRun run) : ISaveStore
    {
        public SaveData? Read(int slot) => Touch();

        public bool Write(int slot, SaveData data) => Touch() is not null;

        public DateTime? Timestamp(int slot) => Touch()?.Written;

        [DoesNotReturn]
        private SaveData? Touch()
        {
            run.Stop("save data");
            return null;
        }
    }
}

/// <summary>A run of the dry run on a state of <typeparamref name="TState"/>.</summary>
public sealed class PreviewRun<TState> : PreviewRun
    where TState : ScriptState
{
    internal PreviewRun(TState state, PreviewDecision[] prefix, int maxSteps, Stack<PreviewDecision[]> pending)
        : base(state, prefix, maxSteps, pending)
    {
    }

    /// <summary>The run's own copy of the state, the one its context runs on.</summary>
    public TState State => (TState)StateCopy;
}

/// <summary>
/// The dry run's scene input (<see cref="PreviewRun.Input"/>): a menu is a decision of the run (its items' texts noted
/// as lines first), a key wait is answered at once by a click (Return, 16: the scripts go on — a right click, 32,
/// would close a window), a pause runs out (0), the mouse is released at (0, 0), the cursor keeps the engine's
/// shapes (1 at first), the date is a fixed one (Saturday, 1 January 2000, midnight), the timer runs a second at each
/// read (a wait on it ends), nothing plays (track 0; the CD at 999). A caller's input derives from it to answer more
/// (the original's battle input, <c>IGameInput</c>) — counting each command it adds with <see cref="PreviewRun.Step"/>
/// or ending the path with <see cref="PreviewRun.Stop"/>.
/// </summary>
public class PreviewInput(PreviewRun run) : ISceneInput
{
    /// <summary>The key a key wait answers: Return, as a left click gives (xsystem35's key code 16).</summary>
    public const int Click = 16;

    private Sys3Int _cursor = 1;
    private int _timer;
    private int _seconds;

    /// <summary>The run this input answers for.</summary>
    protected PreviewRun Run => run;

    /// <inheritdoc />
    public Task<Sys3Int> WaitForKeyAsync()
    {
        run.Step();
        return Task.FromResult((Sys3Int)Click);
    }

    /// <inheritdoc />
    public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
    {
        run.Step();
        return Task.FromResult(Sys3Int.Zero);
    }

    /// <inheritdoc />
    public Task<Sys3Int> GetPlayingTrackAsync()
    {
        run.Step();
        return Task.FromResult(Sys3Int.Zero);
    }

    /// <inheritdoc />
    public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
    {
        foreach (var item in items)
            foreach (var text in item.Texts)
                run.Line(text.Text);
        return Task.FromResult(run.Decide(PreviewDecisionKind.Menu, items.Count, items.Count > 0 ? items[0].At : null));
    }

    /// <inheritdoc />
    public Task<MouseState> GetMouseAsync()
    {
        run.Step();
        return Task.FromResult(new MouseState(Sys3Int.Zero, Sys3Int.Zero, Sys3Int.Zero));
    }

    /// <inheritdoc />
    public Task<Sys3Int> SetCursorAsync(Sys3Int shape)
    {
        run.Step();
        var before = _cursor;
        _cursor = shape;
        return Task.FromResult(before);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Sys3Int>> GetDateAsync()
    {
        run.Step();
        return Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)2000, (Sys3Int)1, (Sys3Int)1, Sys3Int.Zero, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)7]);
    }

    /// <inheritdoc />
    public Task ResetTimerAsync(Sys3Int value)
    {
        run.Step();
        _timer = value.Value;
        _seconds = 0;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task<Sys3Int> GetTimerAsync(int mode)
    {
        run.Step();
        int perSecond = mode switch { 2 => 10, 3 => 30, 4 => 60, 5 => 100, _ => throw new ArgumentOutOfRangeException(nameof(mode)) };
        return Task.FromResult((Sys3Int)Math.Min(ushort.MaxValue, _timer + ++_seconds * perSecond));
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync()
    {
        run.Step();
        return Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999]);
    }

    /// <inheritdoc />
    public Task<Sys3Int> GetMidiTrackAsync()
    {
        run.Step();
        return Task.FromResult(Sys3Int.Zero);
    }
}

/// <summary>A run of the dry run stopped by its caller (<see cref="PreviewRun.Stop"/>); the preview ends the path with it.</summary>
public sealed class PreviewStoppedException : Exception
{
    internal PreviewStoppedException(PreviewRun run, string reason)
        : base($"The dry run stopped: {reason}.")
    {
        Run = run;
        Reason = reason;
    }

    /// <summary>Why the path stopped (e.g. the file of a call the preview does not run).</summary>
    public string Reason { get; }

    /// <summary>The run that stopped.</summary>
    internal PreviewRun Run { get; }
}

/// <summary>A run of the dry run took more than its steps; the preview ends the path as aborted.</summary>
internal sealed class PreviewAbortedException(PreviewRun run, string reason) : Exception($"The dry run aborted: {reason}.")
{
    /// <summary>The run that was aborted.</summary>
    public PreviewRun Run { get; } = run;

    /// <summary>Why (the cap passed).</summary>
    public string Reason { get; } = reason;
}
