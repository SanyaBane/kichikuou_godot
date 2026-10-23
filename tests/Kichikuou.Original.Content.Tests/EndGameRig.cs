using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using Kichikuou.Original.Setup;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// A game for the end-of-game tests (<see cref="IkamiEndTests"/>, <see cref="GodsDoorEndTests"/>, <see cref="OverTests"/>):
/// the boot and the new game from the player's files with the rule files routed and the scenes run
/// from the decompile (every draw 1), every call to another file recorded, the input an
/// <see cref="EndInput"/>.
/// </summary>
internal sealed class EndGame
{
    /// <summary>
    /// The garrisons of <c>Iｶﾐ</c>'s 2F, 3F, 4F and 5F, in the order they are picked: Bound (unit 3),
    /// Soul (unit 4), Leila (unit 5), Rick (unit 7).
    /// </summary>
    public static readonly int[] Garrisons = [3, 4, 5, 7];

    private readonly CallRecorder _recorder;

    private EndGame(GameState state, OriginalScriptContext script, EndInput input, RecordingView view, CallRecorder recorder)
    {
        State = state;
        Script = script;
        Input = input;
        View = view;
        _recorder = recorder;
    }

    public GameState State { get; }

    public OriginalScriptContext Script { get; }

    public EndInput Input { get; }

    /// <summary>Every presentation event, the boot's and the new game's included.</summary>
    public RecordingView View { get; }

    /// <summary>Every call to another file as "file entry", in order.</summary>
    public List<string> Calls => _recorder.Calls;

    /// <summary>The battles fought (<c>FIGT</c> calls).</summary>
    public int Fights => Calls.Count(c => c.StartsWith("FIGT.ADV", StringComparison.Ordinal));

    /// <summary>Runs on every call, after it is recorded and before it runs.</summary>
    public Action<string>? OnCall { get => _recorder.OnCall; set => _recorder.OnCall = value; }

    /// <summary>The files the rules do not port (the gallery, the music room, ...): recorded, not run.</summary>
    public RecordingRules Unported { get; private init; } = null!;

    /// <summary>The draws the script made (1 for each unless queued).</summary>
    public QueuedRandom Random { get; private init; } = null!;

    /// <summary>
    /// The booted new game (or only the boot, <paramref name="newGame"/> false: the title is next),
    /// its calls cleared; it needs the decompile (its tests are <see cref="ScenarioFactAttribute"/>s). <paramref name="saves"/>
    /// are the slots (empty by default); <paramref name="random"/> the draws (1 for each by default).
    /// </summary>
    public static async Task<EndGame> BootAsync(bool newGame = true, ISaveStore? saves = null, QueuedRandom? random = null)
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var unported = new RecordingRules { State = state };
        var rules = new StrategyRules(scripts, unported);
        var runner = new SceneRunner(scripts, rules);
        var input = new EndInput(state);
        var recorder = new CallRecorder(runner, state);
        var view = new RecordingView();
        random ??= new QueuedRandom { DefaultToOne = true };
        var script = new OriginalScriptContext(state, random, view, input, recorder)
        {
            Saves = saves ?? new MemorySaveStore(),
        };
        runner.Bind(script);
        rules.Bind(script);
        var setup = new GameSetup(script, scripts);
        await setup.BootAsync();
        if (newGame)
            await setup.NewGameAsync();
        recorder.Calls.Clear();
        return new EndGame(state, script, input, view, recorder) { Unported = unported, Random = random };
    }

    /// <summary>Every call to another file, recorded with its entry, then run.</summary>
    private sealed class CallRecorder(IStoryHooks target, GameState state) : IStoryHooks
    {
        public List<string> Calls { get; } = [];

        public Action<string>? OnCall { get; set; }

        public Task RunEventAsync(string file, Sys3Int entry)
        {
            Calls.Add($"{file} {state.Ev_Entry.Value}");
            OnCall?.Invoke(file);
            return target.RunEventAsync(file, entry);
        }
    }
}

/// <summary>
/// The answers to the menus, in order: an item's place (1-based, 0 a cancel) or the item registered
/// at a line of a file (its <c>$label$</c>), which fails when the menu does not offer it.
/// </summary>
internal sealed class MenuAnswers
{
    private readonly Queue<Func<IReadOnlyList<SceneMenuItem>, int>> _answers = new();

    public int Count => _answers.Count;

    public void Enqueue(int choice) => _answers.Enqueue(_ => choice);

    /// <summary>An answer decided by the menu's items (a UI's, such as our Subordinates screen's order).</summary>
    public void Enqueue(Func<IReadOnlyList<SceneMenuItem>, int> answer) => _answers.Enqueue(answer);

    /// <summary>The item at <paramref name="file"/>:<paramref name="line"/>.</summary>
    public void Enqueue(string file, int line) => _answers.Enqueue(items =>
    {
        for (int i = 0; i < items.Count; i++)
            if (items[i].At == new ScriptRef(file, line))
                return i + 1;
        throw new InvalidOperationException($"{file}:{line} is not offered: {string.Join(", ", items.Select(item => item.At))}.");
    });

    public int Dequeue(IReadOnlyList<SceneMenuItem> items) => _answers.Dequeue()(items);
}

/// <summary>
/// Key waits and pauses answered at once; the mouse from a <see cref="PointerScript"/> (the
/// pickers' clicks), settling again after every key, pause and menu; the menus from
/// <see cref="Menu"/>; every battle turn answered by <see cref="Command"/> (Repel by default). A run
/// that reads the mouse with nothing left to click fails instead of waiting for ever.
/// </summary>
internal sealed class EndInput(GameState state) : IGameInput
{
    /// <summary>The pointer's gestures; replaced for one that does not settle first (a button already down at a loop's first read).</summary>
    public PointerScript Pointer { get; set; } = new();

    public MenuAnswers Menu { get; } = new();

    /// <summary>Every menu shown: its items' places in the scenario.</summary>
    public List<ScriptRef[]> Menus { get; } = [];

    public List<int> MenuSizes => [.. Menus.Select(m => m.Length)];

    public int Reads { get; private set; }

    private int _idle;

    /// <summary>The answer to a battle turn; Repel by default.</summary>
    public Func<GameState, FigtSelMenu, FigtSelChoice> Command { get; set; } = (_, menu) =>
        new FigtSelChoice(FigtSelCommand.Repel, (menu.Find(FigtSelCommand.Repel)
            ?? throw new InvalidOperationException("No Repel: is DebugMode 1?")).Arguments[0]);

    /// <summary>A picker's clicks: each unit's cell, then the first button (Attack, Search).</summary>
    public void Pick(params int[] units)
    {
        foreach (int unit in units)
        {
            var at = GesturePoints.PickerCell(unit);
            Pointer.Click(at.X, at.Y);
            var go = GesturePoints.PickerButton(1);
            Pointer.Click(go.X, go.Y);
        }
    }

    /// <summary>Runs at every key wait (a UI that answers some itself, such as our Subordinates screen's order).</summary>
    public Action? OnKey { get; set; }

    public Task<Sys3Int> WaitForKeyAsync()
    {
        Pointer.Interrupt();
        OnKey?.Invoke();
        return Task.FromResult(Sys3Int.Zero);
    }

    public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
    {
        Pointer.Interrupt();
        return Task.FromResult(Sys3Int.Zero);
    }

    public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);

    /// <summary>Runs before a read while the pointer has nothing queued: a UI that decides its next gestures from the state (the deployment table).</summary>
    public Action? Idle { get; set; }

    public Task<MouseState> GetMouseAsync()
    {
        Reads++;
        if (Pointer.Pending == 0)
            Idle?.Invoke();
        var m = Pointer.Next(out bool read);
        _idle = read || Pointer.Pending > 0 ? 0 : _idle + 1;
        if (_idle > 1000)
            throw new PointerIdleException();
        return Task.FromResult(m);
    }

    public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Task.FromResult(shape);

    public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
    {
        Menus.Add([.. items.Select(item => item.At)]);
        Pointer.Interrupt();
        if (Menu.Count == 0)
            throw new InvalidOperationException($"A menu with no answer left: {string.Join(", ", items.Select(item => item.At))}.");
        return Task.FromResult(Menu.Dequeue(items));
    }

    public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => Task.FromResult(Command(state, menu));

    public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => throw new InvalidOperationException("A battle menu: no defence is fought here.");
    public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");
    /// <summary>The engine timer <c>ZT</c> reads, as <c>GameScreen</c> does; without one, a <c>ZT</c> fails.</summary>
    public EngineTimer? Timer { get; set; }

    /// <summary>Whether the message skip is on for <see cref="Timer"/>.</summary>
    public bool Skipping { get; set; }

    /// <summary>The frames the timer's reads let pass: one (1/60 s) before every read, none while <see cref="Skipping"/>.</summary>
    public int TimerFrames { get; private set; }

    private TimeSpan _now;

    public Task ResetTimerAsync(Sys3Int value)
    {
        (Timer ?? throw new InvalidOperationException("ZT 1")).Restart(value.Value, _now);
        return Task.CompletedTask;
    }

    public Task<Sys3Int> GetTimerAsync(int mode)
    {
        var timer = Timer ?? throw new InvalidOperationException("ZT n");
        if (!Skipping)
        {
            _now += TimeSpan.FromSeconds(1 / 60.0);
            TimerFrames++;
        }
        return Task.FromResult(EngineTimer.InUnits(timer.Read(_now, Skipping), mode));
    }

    /// <summary>What <c>SC</c> answers (track, minute, second, frame): 999 each, nothing playing, unless set.</summary>
    public IReadOnlyList<Sys3Int> CdPosition { get; set; } = [(Sys3Int)999, (Sys3Int)999, (Sys3Int)999, (Sys3Int)999];

    /// <summary>How often <c>SC</c> was read (the music room's time display polls it).</summary>
    public int CdReads { get; private set; }

    public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync()
    {
        CdReads++;
        return Task.FromResult(CdPosition);
    }
    /// <summary><c>SR 1</c>: no MIDI plays (the music is <c>SS</c>), so 0 at once, as xsystem35 answers <c>OVER.ADV:160</c>.</summary>
    public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);
}

/// <summary>The script polls the mouse with every gesture done (e.g. the title waits again): the run ends.</summary>
internal sealed class PointerIdleException() : InvalidOperationException("The mouse is read with nothing left to click.");
