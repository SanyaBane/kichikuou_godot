namespace Kichikuou.Engine.Script;

/// <summary>
/// Ours, the dry run (owner's decision 30, docs/ui/talk-marks.md §2, §8 step 3): runs a start — a talk, a
/// visit, any async entry a caller builds on a context — on copies of <paramref name="source"/> again and again,
/// once per path the start can take, nothing shown, played, counted or saved. Each run follows a script of
/// decisions (<see cref="PreviewDecision"/>: the value of each draw, the item of each menu); at the first decision
/// past its script it takes the first value and queues the others, so the paths are run depth first, one run each,
/// in the order of their values (a draw's 1..n, a menu's items 1..n and then its cancel, 0). Key waits are answered
/// at once by a click, pauses run out, the clock and the devices give neutral values (<see cref="PreviewInput"/>).
/// </summary>
/// <remarks>
/// The engine knows no rule: <paramref name="start"/> builds the context it needs over the run's parts
/// (<see cref="PreviewRun{TState}"/>: the state copy, the scripted generator, the recording view, the scripted
/// input, the refusing save store) and runs its entry — a fresh context, so no CG load counter is bound (never
/// call <see cref="ScriptContext.BindCgLoadCounter"/> on it). A path ends when the start returns, at a jump out
/// (<see cref="ScenarioJumpException"/>: an ending, a game over), a quit (<see cref="GameQuitException"/>), a load
/// (<see cref="GameLoadedException"/>), a stop the caller's hooks raise (<see cref="PreviewRun.Stop"/>: a call the
/// router does not run, a battle — something happens there) or past <see cref="MaxSteps"/> (a loop that never
/// ends); any other exception is a bug and goes on up. The start must be deterministic given the state and the
/// decisions, and must touch nothing outside the run (not <paramref name="source"/>, not the game's view).
/// Everything the run's parts answer completes at once, so a run is synchronous: <see cref="Run"/> can be called
/// from inside a draw of a real game (step 5); a start that awaits anything else is an error.
/// <code>
/// var scenes = new CachedSceneSource(scripts);             // the programs read once for every run
/// var preview = new ScenePreview&lt;GameState&gt;(state, async run =&gt;
/// {
///     var runner = new SceneRunner(scenes, rules);              // the caller's hooks: run.Stop("FIGT_A.ADV") at a battle
///     var input = new GamePreviewInput(run);                    // the caller's: a PreviewInput that is an IGameInput
///     var script = new OriginalScriptContext(run.State, run.Random, run.View, input, runner)
///     {
///         Saves = run.Saves,
///     };
///     runner.Bind(script);
///     await script.EventAsync("Gｼｨﾙ.ADV", 20);
/// });
/// var result = preview.Run(inspect: (path, end) =&gt; { /* compare end with state */ });
/// </code>
/// </remarks>
/// <typeparam name="TState">The variant's state type; each run works on a copy of its own (<see cref="ScriptState.CopyTo"/>).</typeparam>
public sealed class ScenePreview<TState>(TState source, Func<PreviewRun<TState>, Task> start)
    where TState : ScriptState, new()
{
    /// <summary>The default of <see cref="MaxPaths"/>.</summary>
    public const int DefaultMaxPaths = 256;

    /// <summary>The default of <see cref="MaxSteps"/>.</summary>
    public const int DefaultMaxSteps = 100_000;

    /// <summary>
    /// The most paths run; when more are left the result is <see cref="PreviewResult.Truncated"/> (for the marks:
    /// "something may happen", docs/ui/talk-marks.md §8). A draw of n alone makes n paths.
    /// </summary>
    public int MaxPaths { get; init; } = DefaultMaxPaths;

    /// <summary>
    /// The most steps of one run — every event shown, every input answered, every draw (<see cref="PreviewRun.Step"/>);
    /// past it the path ends as <see cref="PreviewEnd.Aborted"/>. A loop that shows and asks nothing is not caught (it
    /// would hang the original too).
    /// </summary>
    public int MaxSteps { get; init; } = DefaultMaxSteps;

    /// <summary>
    /// Whether every path keeps its end state (<see cref="PreviewPath.EndState"/>, a state of its own each); by default
    /// one state is reused for every run and a path's end is seen only through <see cref="Run"/>'s inspection.
    /// </summary>
    public bool KeepEndStates { get; init; }

    /// <summary>
    /// Runs every path from the source state as it is now. Every path follows
    /// <paramref name="forced"/> first (step 5: the decisions a real run has made); <paramref name="inspect"/> sees each
    /// path with its end state as it ends, before the next run reuses that state.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// The forced decisions do not fit what the start meets (another kind, count or place, a value out of range, or
    /// more decisions than the path makes), or the start awaited something the run does not answer at once.
    /// </exception>
    public PreviewResult Run(IReadOnlyList<PreviewDecision>? forced = null, Action<PreviewPath, TState>? inspect = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxPaths, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(MaxSteps, 1);
        var pending = new Stack<PreviewDecision[]>();
        pending.Push(forced?.ToArray() ?? []);
        var paths = new List<PreviewPath>();
        TState? reused = null;
        while (pending.Count > 0)
        {
            if (paths.Count == MaxPaths)
                return new PreviewResult(paths, Truncated: true);
            var state = KeepEndStates ? new TState() : (reused ??= new TState());
            source.CopyTo(state);
            var run = new PreviewRun<TState>(state, pending.Pop(), MaxSteps, pending);
            var (end, reason) = Execute(run);
            run.CheckPrefixUsed();
            var path = new PreviewPath([.. run.Decisions], [.. run.Lines], end, reason, KeepEndStates ? state : null)
            {
                DecisionLines = [.. run.DecisionLines],
            };
            paths.Add(path);
            inspect?.Invoke(path, state);
        }
        return new PreviewResult(paths, Truncated: false);
    }

    /// <summary>One run of the start, synchronously, and how its path ended.</summary>
    private (PreviewEnd End, string? Reason) Execute(PreviewRun<TState> run)
    {
        try
        {
            var task = start(run);
            if (!task.IsCompleted)
                throw new InvalidOperationException("The previewed start awaited something the dry run does not answer at once.");
            task.GetAwaiter().GetResult();
            return (PreviewEnd.Returned, null);
        }
        catch (ScenarioJumpException jump)
        {
            return (PreviewEnd.Jumped, jump.File);
        }
        catch (GameQuitException)
        {
            return (PreviewEnd.Quit, null);
        }
        catch (GameLoadedException loaded)
        {
            return (PreviewEnd.Loaded, $"slot {loaded.Slot}");
        }
        catch (PreviewStoppedException stop) when (ReferenceEquals(stop.Run, run))
        {
            return (PreviewEnd.Stopped, stop.Reason);
        }
        catch (PreviewAbortedException abort) when (ReferenceEquals(abort.Run, run))
        {
            return (PreviewEnd.Aborted, abort.Reason);
        }
        catch (PreviewRepeatedException repeat) when (ReferenceEquals(repeat.Run, run))
        {
            return (PreviewEnd.Repeated, repeat.Reason);
        }
    }
}

/// <summary>What a decision of the dry run chose between.</summary>
public enum PreviewDecisionKind
{
    /// <summary><c>Y 4, n</c> with n of 2 or more: a value 1..n (n of 0 or 1 draws nothing and is no decision).</summary>
    Draw,

    /// <summary><c>]</c>: an item 1..n, or 0 for the cancel (the right click).</summary>
    Menu,
}

/// <summary>
/// One decision of a path: a draw's value or a menu's choice, with its count (the draw's n, the menu's items) and,
/// where it is known, its place — a scene's <c>Y 4</c> statement (<see cref="IPlacedRandom"/>; a hand port's draw has
/// none), a menu's first item (<see cref="SceneMenuItem.At"/>; an empty menu has none).
/// </summary>
public readonly record struct PreviewDecision(PreviewDecisionKind Kind, int Count, int Value, ScriptRef? At = null)
{
    /// <summary>A draw of <paramref name="value"/> of 1..<paramref name="n"/>.</summary>
    public static PreviewDecision Draw(int n, int value, ScriptRef? at = null) => new(PreviewDecisionKind.Draw, n, value, at);

    /// <summary>Item <paramref name="value"/> (0: the cancel) of a menu of <paramref name="items"/>.</summary>
    public static PreviewDecision Menu(int items, int value, ScriptRef? at = null) => new(PreviewDecisionKind.Menu, items, value, at);

    /// <inheritdoc />
    public override string ToString() => $"{Kind} {Value} of {Count}{(At is { } at ? $" at {at}" : "")}";
}

/// <summary>How a path of the dry run ended.</summary>
public enum PreviewEnd
{
    /// <summary>The start returned.</summary>
    Returned,

    /// <summary>The script jumped to another file (<c>&amp;</c>: an ending, a game over); the reason is the file.</summary>
    Jumped,

    /// <summary>The script quit the engine (<c>ZZ0</c>).</summary>
    Quit,

    /// <summary>A save was loaded (<c>LD</c>, only through a save store of the caller's); the reason names the slot.</summary>
    Loaded,

    /// <summary>The caller's code stopped the path (<see cref="PreviewRun.Stop"/>, also a touch of the save data); the reason is its.</summary>
    Stopped,

    /// <summary>The run took more than its steps (<see cref="ScenePreview{TState}.MaxSteps"/>): a loop that does not end.</summary>
    Aborted,

    /// <summary>
    /// The run met a decision at a place where it had made one already, of the same kind and count, with the same memory
    /// (<see cref="Sys3.ScriptMemory.Fingerprint"/>): from there it would go on as from the first time — a menu's cancel that
    /// shows the menu again, a draw drawn again until another value comes —, so the path ends before it. Decisions a
    /// caller forces are never cut. A decision without a place (a hand port's draw) is not checked.
    /// </summary>
    Repeated,
}

/// <summary>
/// One path of the dry run: its decisions in order, the lines it showed in order (every message run and every text of
/// a menu's items, as the record of read lines counts them, <see cref="ScriptContext.SelectAsync"/>; repeats included),
/// how it ended (<paramref name="Reason"/>: the jump's file, the stop's reason, …) and, when the preview keeps them,
/// its end state.
/// </summary>
public sealed record PreviewPath(
    IReadOnlyList<PreviewDecision> Decisions,
    IReadOnlyList<ScriptRef> Lines,
    PreviewEnd End,
    string? Reason,
    ScriptState? EndState)
{
    /// <summary>
    /// For each of <see cref="Decisions"/>, how many of <see cref="Lines"/> the path had shown before it (a menu's items
    /// among them: they are on the screen before the choice); so the lines between two decisions are the ones the first
    /// decision's value led to. Empty for a path made by hand without them (every line then counts as shown first).
    /// </summary>
    public IReadOnlyList<int> DecisionLines { get; init; } = [];

    /// <summary>The path's draws (its decisions that were <see cref="PreviewDecisionKind.Draw"/>s), in order.</summary>
    public IEnumerable<PreviewDecision> Draws => Decisions.Where(d => d.Kind == PreviewDecisionKind.Draw);

    /// <summary>Whether the path shows a line <paramref name="asOf"/> has not read (<see cref="ScriptState.IsLineRead"/>).</summary>
    public bool ShowsUnreadLine(ScriptState asOf) => Lines.Any(line => !asOf.IsLineRead(line));
}

/// <summary>Every path the dry run ran, in order; <paramref name="Truncated"/> when more were left past its cap.</summary>
public sealed record PreviewResult(IReadOnlyList<PreviewPath> Paths, bool Truncated);
