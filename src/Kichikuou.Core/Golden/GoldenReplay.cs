using System.Text;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Golden;

/// <summary>
/// The core stopped matching the original's event stream (a draw, an input or a hook call
/// arrived where the original did something else); the replay cannot go on.
/// </summary>
public sealed class GoldenDivergenceException(string message) : Exception(message);

/// <summary>The differences found at one point of a replay.</summary>
/// <param name="Name">Where: <c>exit</c>, or <c>hook-enter Gｴｸｽ.ADV (line 812)</c>.</param>
/// <param name="Differences">Every difference, ignored ones included.</param>
public sealed record GoldenCheckpoint(string Name, IReadOnlyList<MemoryDifference> Differences)
{
    /// <summary>The differences that count.</summary>
    public IEnumerable<MemoryDifference> Failures => Differences.Where(d => !d.Ignored);
}

/// <summary>The outcome of replaying one capture.</summary>
public sealed class GoldenReport(GoldenCapture capture)
{
    /// <summary>The replayed capture.</summary>
    public GoldenCapture Capture { get; } = capture;

    /// <summary>Memory comparisons in replay order (hook entries, then the exit).</summary>
    public List<GoldenCheckpoint> Checkpoints { get; } = [];

    /// <summary>Why the replay stopped early, or <c>null</c>.</summary>
    public string? Divergence { get; set; }

    /// <summary>Events consumed before the end or the divergence (for context).</summary>
    public int ConsumedRecords { get; set; }

    /// <summary>The replay ran to the end and every compared location matched.</summary>
    public bool Passed => Divergence is null && Checkpoints.All(c => !c.Failures.Any());

    /// <summary>A readable account: the divergence, then each checkpoint's failures (capped).</summary>
    public string Describe(int maxLines = 60)
    {
        var sb = new StringBuilder();
        sb.Append($"{Capture.Source}: {Capture.Root}, {Capture.Records.Count} events, seed {Capture.Seed}");
        sb.Append(Passed ? " — matches the original.\n" : " — DIFFERS from the original.\n");
        if (Divergence is not null)
            sb.Append($"Diverged after {ConsumedRecords} events: {Divergence}\n");
        foreach (var checkpoint in Checkpoints)
        {
            var failures = checkpoint.Failures.ToList();
            int ignored = checkpoint.Differences.Count - failures.Count;
            sb.Append($"[{checkpoint.Name}] {failures.Count} differences, {ignored} ignored\n");
            foreach (var d in failures.Take(maxLines))
                sb.Append($"  {d}\n");
            if (failures.Count > maxLines)
                sb.Append($"  … {failures.Count - maxLines} more\n");
            var ignoredList = checkpoint.Differences.Where(d => d.Ignored).ToList();
            foreach (var d in ignoredList.Take(maxLines))
                sb.Append($"  {d}\n");
            if (ignoredList.Count > maxLines)
                sb.Append($"  … {ignoredList.Count - maxLines} more ignored\n");
        }
        return sb.ToString();
    }
}

/// <summary>
/// Replays a <see cref="GoldenCapture"/> through the battle port (<c>docs/golden-tests.md</c>,
/// "Replay in the core"): loads the entry memory, answers draws, inputs and hook calls from the
/// recorded events, and compares the memory with the original at each hook entry and at the end.
/// </summary>
public sealed class GoldenReplay
{
    /// <summary>The roots the replay has an entry point for (page calls into the battle and <c>COM.ADV</c>).</summary>
    public static readonly IReadOnlySet<string> ReplayableRoots = new HashSet<string> { "FIGT_A.ADV", "FIGT_B.ADV", "FIGT.ADV", "COM.ADV" };

    /// <summary>Whether a capture with this header can be replayed (its root has an entry point).</summary>
    public static bool CanReplay(GoldenHeader header) =>
        header.RootKind == GoldenRootKind.PageCall && ReplayableRoots.Contains(header.Root);

    /// <summary>
    /// Records the core does not consume: <c>IM</c> in the battle only waits for the button's
    /// release (<c>FIGT.ADV:829</c>, <c>FIGT_B.ADV:47</c>, …; <c>RetVal</c> ends 0, the position goes
    /// to the ignored <c>Ui_MouseX/Y</c>), and no ported file sets the cursor. Scenes run their
    /// <c>IM</c> statements, so a replay through a <see cref="SceneRunner"/> consumes the mouse.
    /// </summary>
    private bool IsPassive(GoldenRecord record) => record is CursorRecord || (record is MouseRecord && !_scenes);

    private readonly GoldenCapture _capture;
    private readonly GoldenFilter _filter;
    private readonly GoldenReport _report;
    private readonly GameState _state = new();
    private readonly List<GoldenRecord> _consumed = [];
    private int _next;
    private long _mouseRepeats;
    private bool _scenes;
    private Mt19937Legacy _generator;
    private XSystem35Random _random;

    private GoldenReplay(GoldenCapture capture, GoldenFilter filter)
    {
        _capture = capture;
        _filter = filter;
        _report = new GoldenReport(capture);
        _generator = capture.Enter.CreateGenerator();
        _random = new XSystem35Random(_generator);
    }

    /// <summary>The state the replay runs on (after the run: the core's final memory).</summary>
    public GameState State => _state;

    /// <summary>
    /// Replays a capture and reports how the core compares with the original. <paramref name="view"/>
    /// receives the battle's presentation events (none are shown by default), so a screen can
    /// play a capture back as it happened; <paramref name="started"/> gets the replay's state
    /// once the entry memory is loaded (story-scene dumps are loaded into the same object).
    /// <paramref name="entryPoint"/> runs the root through code outside the battle (e.g. the boot,
    /// which needs the player's scenario files), on the replay's context; without it the root
    /// must be one of <see cref="ReplayableRoots"/>. <paramref name="scenes"/> makes the scene
    /// runtime from the replay's hooks (the files it does not run as scenes stay hooks): the
    /// context then runs scene files, and the replay answers menus, the mouse and the clock.
    /// </summary>
    public static async Task<GoldenReport> RunAsync(
        GoldenCapture capture,
        GoldenFilter? filter = null,
        IScriptView? view = null,
        Action<GameState>? started = null,
        Func<ScriptContext, Task>? entryPoint = null,
        Func<IStoryHooks, SceneRunner>? scenes = null)
    {
        var replay = new GoldenReplay(capture, filter ?? GoldenFilter.For(capture));
        await replay.RunAsync(view ?? new NullView(), started, entryPoint, scenes);
        return replay._report;
    }

    private async Task RunAsync(
        IScriptView view, Action<GameState>? started, Func<ScriptContext, Task>? entryPoint, Func<IStoryHooks, SceneRunner>? scenes)
    {
        try
        {
            // A boot capture enters SET.ADV before its DS lines: the layout is checked on the exit dump.
            CheckLayout(_capture.Enter.Bindings.Count > 0 ? _capture.Enter : _capture.Exit);
            _capture.Enter.ApplyTo(_state.Memory);
            started?.Invoke(_state);
            var hooks = new Hooks(this);
            var runner = scenes?.Invoke(hooks);
            _scenes = runner is not null;
            var battle = new Battle(_state, new Random(this), view, new Input(this), runner ?? (IStoryHooks)hooks);
            runner?.Bind(battle.Script);
            Func<Task> entry = entryPoint is not null
                ? () => entryPoint(battle.Script)
                : !CanReplay(_capture.Header)
                ? throw new GoldenFormatException($"No entry point for root {_capture.Header.RootName} (group {_capture.Group}).")
                : _capture.Root switch
                {
                    "FIGT_A.ADV" => battle.AttackProvinceAsync,
                    "FIGT_B.ADV" => battle.DefendProvinceAsync,
                    "COM.ADV" => new WeeklyEvents(battle.Script).RunAsync,
                    _ => battle.FightAsync,
                };

            string? jumped = null;
            try
            {
                await entry();
            }
            catch (ScenarioJumpException e)
            {
                jumped = e.File;
            }

            if (jumped is not null)
            {
                var jump = Take<JumpRecord>($"jump to {jumped}");
                if (jump.File != jumped)
                    throw Diverged($"the core jumped to {jumped}, the original to {jump.File} at {jump.Where}");
            }
            if (NextRecord() is { } left)
                throw Diverged($"the core's battle ended ({jumped ?? "return"}), but the original went on with {Describe(left)}");
            if (_capture.JumpTarget != jumped)
                throw Diverged($"the core ended with {jumped ?? "return"}, the original with {_capture.JumpTarget ?? "return"}");

            _report.Checkpoints.Add(new GoldenCheckpoint("exit",
                MemoryComparison.Compare(_capture.Exit, _state.Memory, _generator, _filter)));
        }
        catch (GoldenDivergenceException e)
        {
            _report.Divergence = e.Message;
        }
        _report.ConsumedRecords = _consumed.Count;
    }

    /// <summary>The dump's <c>DS</c> bindings must be the ones the generated <see cref="GameState"/> assumes.</summary>
    private static void CheckLayout(MemoryDump dump)
    {
        var expected = VariableCatalog.Bindings.Select(b => new DumpBinding(b.Var, b.Page, b.Offset, b.Cursor)).ToHashSet();
        var actual = dump.Bindings.ToHashSet();
        var missing = expected.Except(actual).ToList();
        var extra = actual.Except(expected).ToList();
        if (missing.Count > 0 || extra.Count > 0)
        {
            throw new GoldenFormatException(
                "The original's array bindings differ from the generated GameState layout: core only "
                + string.Join(", ", missing) + "; original only " + string.Join(", ", extra) + ".");
        }
        foreach (var (page, size) in VariableCatalog.Pages)
        {
            if (!dump.Pages.TryGetValue(page, out var values) || values.Length != size)
                throw new GoldenFormatException($"Page {page} has size {values?.Length ?? 0} in the original, {size} in the core.");
        }
    }

    // ---------------------------------------------------------------------------------------
    // The event stream, shared by the random source, the input and the hooks
    // ---------------------------------------------------------------------------------------

    /// <summary>The next record the core must meet, not consumed (passive records are skipped).</summary>
    private GoldenRecord? NextRecord()
    {
        while (_next < _capture.Records.Count && IsPassive(_capture.Records[_next]))
            _next++;
        return _next < _capture.Records.Count ? _capture.Records[_next] : null;
    }

    private T Take<T>(string what) where T : GoldenRecord
    {
        var record = NextRecord();
        if (record is not T match)
        {
            throw Diverged(record is null
                ? $"the core asked for {what}, but the original's battle had ended"
                : $"the core asked for {what}, but the original's next event is {Describe(record)}");
        }
        _next++;
        _consumed.Add(match);
        return match;
    }

    /// <summary>One <c>IM</c>: a mouse record answers as many reads as its count.</summary>
    private MouseRecord TakeMouse()
    {
        if (NextRecord() is not MouseRecord record)
            return Take<MouseRecord>("the mouse (IM)");
        if (++_mouseRepeats >= record.Count)
        {
            _mouseRepeats = 0;
            _next++;
            _consumed.Add(record);
        }
        return record;
    }

    private GoldenDivergenceException Diverged(string message)
    {
        var sb = new StringBuilder(message).Append('.');
        if (_consumed.Count > 0)
        {
            sb.Append(" Last events matched:");
            foreach (var r in _consumed.TakeLast(5))
                sb.Append($"\n    {Describe(r)}");
        }
        return new GoldenDivergenceException(sb.ToString());
    }

    private static string Describe(GoldenRecord record) => record switch
    {
        RndRecord r => $"Y 4,{r.N} → {r.Result} at {r.Where} (line {r.Line})",
        WaitRecord w => $"Y 3,{w.Ticks} → {w.Code} at {w.Where} (line {w.Line})",
        KeyRecord k => $"IK → {k.Code} at {k.Where} (line {k.Line})",
        MenuRecord m => $"menu choice {m.Choice}/{m.Count} at {m.Where} (line {m.Line})",
        MusicRecord m => $"SR 0 → {m.Track} at {m.Where} (line {m.Line})",
        PickRecord p => $"FIGT_SEL → ({p.RetVal}, {p.Tmp0001}) at {p.Where} (line {p.Line})",
        HookRecord h => $"call {h.File} at {h.Where} (line {h.Line})",
        JumpRecord j => $"jump to {j.File} at {j.Where} (line {j.Line})",
        MouseRecord m => $"IM → button {m.Button} at ({m.X}, {m.Y}) ×{m.Count} at {m.Where} (line {m.Line})",
        CursorRecord c => $"IC {c.Cursor} → {c.Old} at {c.Where} (line {c.Line})",
        CdPosRecord c => $"SC → {c.Track} {c.Minute}:{c.Second}.{c.Frame} at {c.Where} (line {c.Line})",
        DateRecord d => $"ZT 0 → {string.Join(' ', d.Values)} at {d.Where} (line {d.Line})",
        TimerRecord t => $"ZT {t.Command} → {t.Value} at {t.Where} (line {t.Line})",
        _ => record.ToString(),
    };

    private sealed class Random(GoldenReplay replay) : IScriptRandom
    {
        public Sys3Int Next(Sys3Int n)
        {
            var record = replay.Take<RndRecord>($"Y 4,{n}");
            if (record.N != n.Value)
                throw replay.Diverged($"the core drew Y 4,{n} where the original drew Y 4,{record.N} at {record.Where} (line {record.Line})");
            var value = replay._random.Next(n);
            if (value.Value != record.Result)
            {
                throw replay.Diverged(
                    $"Y 4,{n} gave {value} in the core and {record.Result} in the original at {record.Where} (line {record.Line}): the generator is out of step");
            }
            return value;
        }
    }

    private sealed class Input(GoldenReplay replay) : IBattleInput, ISceneInput
    {
        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            var record = replay.Take<MenuRecord>("a menu choice");
            if (record.Count != items.Count)
            {
                throw replay.Diverged(
                    $"the menu has {items.Count} items in the core and {record.Count} in the original at {record.Where} (line {record.Line})");
            }
            return Task.FromResult(record.Choice);
        }

        public Task<MouseState> GetMouseAsync()
        {
            var record = replay.TakeMouse();
            return Task.FromResult(new MouseState((Sys3Int)record.Button, (Sys3Int)record.X, (Sys3Int)record.Y));
        }

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() =>
            Task.FromResult<IReadOnlyList<Sys3Int>>(replay.Take<DateRecord>("the date (ZT 0)").Values.Select(v => (Sys3Int)v).ToArray());

        /// <summary><c>ZT 1</c> is not recorded: the timer reads after it carry the values.</summary>
        public Task ResetTimerAsync(Sys3Int value) => Task.CompletedTask;

        public Task<Sys3Int> GetTimerAsync(int mode)
        {
            var record = replay.Take<TimerRecord>($"the timer (ZT {mode})");
            if (record.Command != mode)
                throw replay.Diverged($"the core read ZT {mode} where the original read ZT {record.Command} at {record.Where} (line {record.Line})");
            return Task.FromResult((Sys3Int)record.Value);
        }

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync()
        {
            var r = replay.Take<CdPosRecord>("the CD position (SC)");
            return Task.FromResult<IReadOnlyList<Sys3Int>>([(Sys3Int)r.Track, (Sys3Int)r.Minute, (Sys3Int)r.Second, (Sys3Int)r.Frame]);
        }

        /// <summary><c>SR 1</c> (MIDI) is not recorded; the original plays CD audio, so no MIDI track plays.</summary>
        public Task<Sys3Int> GetMidiTrackAsync() => Task.FromResult(Sys3Int.Zero);

        public Task<Sys3Int> WaitForKeyAsync() =>
            Task.FromResult((Sys3Int)replay.Take<KeyRecord>("a key (IK)").Code);

        public Task<Sys3Int> PauseAsync(Sys3Int ticks)
        {
            var record = replay.Take<WaitRecord>($"a pause (Y 3,{ticks})");
            if (record.Ticks != ticks.Value)
                throw replay.Diverged($"the core paused Y 3,{ticks} where the original paused Y 3,{record.Ticks} at {record.Where} (line {record.Line})");
            return Task.FromResult((Sys3Int)record.Code);
        }

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            var record = replay.Take<PickRecord>("a FIGT_SEL result");
            var choice = new FigtSelChoice((Sys3Int)record.RetVal, (Sys3Int)record.Tmp0001);
            if (!menu.Allows(choice))
            {
                throw replay.Diverged(
                    $"FIGT_SEL returned ({record.RetVal}, {record.Tmp0001}) at {record.Where} (line {record.Line}), which the core's menu does not offer");
            }
            return Task.FromResult(choice);
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            var record = replay.Take<MenuRecord>($"a choice in the {menu.Kind} menu");
            if (record.Count != menu.Options.Count)
            {
                throw replay.Diverged(
                    $"the {menu.Kind} menu has {menu.Options.Count} items in the core and {record.Count} in the original at {record.Where} (line {record.Line})");
            }
            return Task.FromResult(record.Choice == 0 ? null : (Sys3Int?)menu.Options[record.Choice - 1].Code);
        }

        public Task<Sys3Int> GetPlayingTrackAsync() =>
            Task.FromResult((Sys3Int)replay.Take<MusicRecord>("the playing track (SR 0)").Track);
    }

    private sealed class Hooks(GoldenReplay replay) : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry)
        {
            var record = replay.Take<HookRecord>($"a call of {file} (entry {entry})");
            if (record.File != file)
                throw replay.Diverged($"the core called {file} where the original called {record.File} at {record.Where} (line {record.Line})");

            replay._report.Checkpoints.Add(new GoldenCheckpoint(
                $"hook-enter {record.File} at {record.Where} (line {record.Line})",
                MemoryComparison.Compare(record.Enter, replay._state.Memory, replay._generator, replay._filter)));

            record.Exit.ApplyTo(replay._state.Memory);
            replay._generator = record.Exit.CreateGenerator();
            replay._random = new XSystem35Random(replay._generator);
            return Task.CompletedTask;
        }
    }

    private sealed class NullView : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent) => Task.CompletedTask;
    }
}
