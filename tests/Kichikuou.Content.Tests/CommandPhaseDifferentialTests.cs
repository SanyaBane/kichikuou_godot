using System.Text;
using Kichikuou.Content.Scripts;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The hand port of <c>COMMAND.ADV</c> (<see cref="CommandPhase"/>) against the file itself run from
/// the player's decompile by <see cref="SceneRunner"/>, on random states and random pointer
/// gestures: both runs must make the same input requests (and so get the same answers), call the
/// same files with the same <c>Ev_Entry</c>, show the same events and leave the same memory. Every
/// other file is a hook that scribbles over cursors, scratch, flags and the story stage, the way
/// the commands and scenes do. Skipped without the decompile.
/// </summary>
public class CommandPhaseDifferentialTests(ITestOutputHelper output)
{
    private const string Command = "COMMAND.ADV";
    private const int Cases = 800;

    [Fact]
    public async Task The_port_runs_COMMAND_exactly_as_the_decompile()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, Command)))
            return;
        var program = new ScenarioScripts(LocalData.ScenarioDirectory).ReadProgram(Command);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        int stopped = 0, cases = DifferentialRuns.Cases(Cases);

        for (int seed = 1; seed <= cases; seed++)
        {
            var state = RandomState(new Random(seed));
            var port = await RunAsync(Copy(state), seed, null);
            var original = await RunAsync(Copy(state), seed, program);

            int at = FirstDifference(original.Log, port.Log);
            Assert.True(at < 0, $"seed {seed}: the runs differ at entry {at}\n{Around(original.Log, port.Log, at)}");
            Assert.Equal(original.Stopped, port.Stopped);
            var differences = Differences(original.State.Memory, port.State.Memory).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));

            if (port.Stopped)
                stopped++;
            foreach (string feature in Features(port.Log))
                seen[feature] = seen.GetValueOrDefault(feature) + 1;
        }

        output.WriteLine($"{cases} runs, {stopped} stopped by the input budget");
        foreach (var (feature, count) in seen.OrderBy(f => f.Key, StringComparer.Ordinal))
            output.WriteLine($"  {feature}: {count}");
        string[] expected =
        [
            "call COM.ADV", "call Cﾃｷﾄｼ.ADV", "call Cﾃｷﾄｼ2.ADV", "call Cｼﾊｲﾄｼ.ADV", "call Cﾒｲｷｭｳ.ADV",
            "call Cﾃｲﾄ.ADV", "call Cﾊｰﾚﾑ.ADV", "call Cﾀｶﾗ.ADV", "call Cｼｮｳﾆﾝ.ADV", "call Cﾌﾞｶ.ADV",
            "call 各種DATA.ADV", "call Iｶﾝｶﾞｴ.ADV", "call TEST.ADV", "call ﾛｰﾄﾞｾｰﾌﾞ.ADV",
            "call Gｶﾌｪ.ADV", "call Gｼｨﾙ2.ADV", "call Iﾄｳｿﾞｸ.ADV", "call ｵｰﾌﾟﾆﾝｸﾞ.ADV",
            "call Gﾏﾘｽ.ADV", "call Gｱｰﾔ.ADV", "cursor", "drag step", "Tange Dog", "end",
        ];
        var missed = expected.Where(e => !seen.ContainsKey(e)).ToList();
        DifferentialRuns.AssertReached(missed, output);
    }

    /// <summary>
    /// The drag-scroll of <c>WAO_MAP.ADV</c> op 1 (<c>ScriptContext.Map.cs</c>, which both runs above
    /// share) against the file run from the decompile, on random cameras and random pointer
    /// sequences (presses, moves, other buttons, presses while the view coasts).
    /// </summary>
    [Fact]
    public async Task The_drag_scroll_runs_exactly_as_WAO_MAP()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "WAO_MAP.ADV")))
            return;
        var program = new ScenarioScripts(LocalData.ScenarioDirectory).ReadProgram("WAO_MAP.ADV");
        int steps = 0, drags = DifferentialRuns.Cases(2000);
        for (int seed = 1; seed <= drags; seed++)
        {
            var rng = new Random(seed);
            var state = new GameState();
            state.MapCamX = (Sys3Int)rng.Next(0, 1400);
            state.MapCamY = (Sys3Int)rng.Next(0, 500);
            state.Ui_DragPending = (Sys3Int)rng.Next(0, 2);
            state.Ui_DragX = (Sys3Int)rng.Next(0, 640);
            state.Ui_DragY = (Sys3Int)rng.Next(0, 400);
            state.Ui_ScrollWait = (Sys3Int)rng.Next(0, 3);
            for (int v = 21; v <= 30; v++)
                state.Memory.Var(v) = (Sys3Int)rng.Next(0, 50);

            var port = await DragAsync(Copy(state), seed, null);
            var original = await DragAsync(Copy(state), seed, program);
            int at = FirstDifference(original.Log, port.Log);
            Assert.True(at < 0, $"seed {seed}: the runs differ at entry {at}\n{Around(original.Log, port.Log, at)}");
            var differences = Differences(original.State.Memory, port.State.Memory).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));
            steps += port.Log.Count(e => e.StartsWith("EngineEvent CC", StringComparison.Ordinal));
        }
        output.WriteLine($"{drags} drags, {steps} scroll steps");
        Assert.True(steps > drags / 2);
    }

    private static async Task<Run> DragAsync(GameState state, int seed, ScriptProgram? program)
    {
        var log = new List<string>();
        var input = new RandomPointer(seed, log);
        var view = new LoggingView(log);
        bool stopped = false;
        try
        {
            if (program is null)
            {
                var script = new ScriptContext(state, new NoRandom(), view, input, new ScribblingHooks(state, seed, log));
                await script.WaoMapCallAsync(1);
                log.RemoveAt(0);    // the WaoMapEvent the port raises for the call
            }
            else
            {
                var runner = new SceneRunner(new NamedProgram(program), new ScribblingHooks(state, seed, log), _ => true);
                var script = new ScriptContext(state, new NoRandom(), view, input, runner);
                runner.Bind(script);
                state.CallArg = 1;
                await runner.RunFileAsync("WAO_MAP.ADV");
            }
        }
        catch (InputBudgetException)
        {
            stopped = true;
        }
        return new Run(state, log, stopped);
    }

    /// <summary>One program under its own name.</summary>
    private sealed class NamedProgram(ScriptProgram program) : ISceneSource
    {
        public ScriptProgram ReadProgram(string file) =>
            file == program.File ? program : throw new InvalidOperationException($"{file} is a hook here.");
    }

    /// <summary>A random pointer: mostly the left button held while it wanders, releases, now and then another button.</summary>
    private sealed class RandomPointer(int seed, List<string> log) : ISceneInput
    {
        private readonly Random _rng = new(seed * 17 + 3);
        private int _x = 320;
        private int _y = 200;
        private int _button = 16;
        private int _reads;
        private int _cursor = 1;

        public Task<MouseState> GetMouseAsync()
        {
            if (++_reads > 200)
                throw new InputBudgetException();
            int r = _rng.Next(100);
            _button = r < 70 ? _button : r < 85 ? 0 : r < 95 ? 16 : _rng.Next(2) == 0 ? 32 : 48;
            if (_rng.Next(3) != 0)
            {
                _x = Math.Clamp(_x + _rng.Next(-80, 81), 0, 639);
                _y = Math.Clamp(_y + _rng.Next(-50, 51), 0, 399);
            }
            var m = new MouseState((Sys3Int)_button, (Sys3Int)_x, (Sys3Int)_y);
            log.Add($"IM {m.Button} {m.X} {m.Y}");
            return Task.FromResult(m);
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape)
        {
            int old = _cursor;
            _cursor = shape.Value;
            log.Add($"IC {shape} {old}");
            return Task.FromResult((Sys3Int)old);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            log.Add($"wait {ticks}");
            return Task.FromResult((Sys3Int)_rng.Next(0, 2));
        }

        public Task<Sys3Int> WaitForKeyAsync() => throw new InvalidOperationException("IK");

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => throw new InvalidOperationException("]");

        public Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException("SR 0");

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");

        public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");

        public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");

        public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");
    }

    private sealed record Run(GameState State, List<string> Log, bool Stopped);

    private static async Task<Run> RunAsync(GameState state, int seed, ScriptProgram? program)
    {
        var log = new List<string>();
        var hooks = new ScribblingHooks(state, seed, log);
        var input = new GestureInput(state, seed, log);
        var view = new LoggingView(log);
        bool stopped = false;
        try
        {
            if (program is null)
            {
                var script = new ScriptContext(state, new NoRandom(), view, input, hooks);
                await new CommandPhase(script).RunAsync();
            }
            else
            {
                var runner = new SceneRunner(new OneProgram(program), hooks, file => file == Command);
                var script = new ScriptContext(state, new NoRandom(), view, input, runner);
                runner.Bind(script);
                await runner.RunFileAsync(Command);
            }
            log.Add("end");
        }
        catch (InputBudgetException)
        {
            stopped = true;
        }
        return new Run(state, log, stopped);
    }

    private static IEnumerable<string> Features(List<string> log)
    {
        foreach (string entry in log)
        {
            if (entry.StartsWith("call ", StringComparison.Ordinal))
                yield return entry[..entry.IndexOf(" entry", StringComparison.Ordinal)];
            else if (entry.StartsWith("IC ", StringComparison.Ordinal))
                yield return "cursor";
            else if (entry.StartsWith("EngineEvent CC", StringComparison.Ordinal))
                yield return "drag step";
            else if (entry.Contains("COMMAND.ADV:397", StringComparison.Ordinal))
                yield return "Tange Dog";
            else if (entry == "end")
                yield return "end";
        }
    }

    private static int FirstDifference(List<string> a, List<string> b)
    {
        for (int i = 0; i < Math.Min(a.Count, b.Count); i++)
            if (a[i] != b[i])
                return i;
        return a.Count == b.Count ? -1 : Math.Min(a.Count, b.Count);
    }

    private static string Around(List<string> original, List<string> port, int at)
    {
        var sb = new StringBuilder();
        for (int i = Math.Max(0, at - 8); i < at + 3; i++)
        {
            sb.Append($"  {i,5} COMMAND.ADV: {(i < original.Count ? original[i] : "-")}\n");
            sb.Append($"  {i,5} port:        {(i < port.Count ? port[i] : "-")}\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Every unbound variable and the record page random, then a map the gestures can aim at:
    /// province and dungeon positions on the 1920×800 map, owners, neighbours, the camera, the
    /// construction timer, the Tange Dog's holder, the prologue half the time.
    /// </summary>
    private static GameState RandomState(Random rng)
    {
        var state = new GameState();
        double zero = rng.NextDouble() * 0.6;
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (!GameState.IsBound(v))
                state.Memory.Var(v) = (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(5) == 0 ? rng.Next(0, 3000) : rng.Next(0, 12));
        for (int i = 0; i < state.Memory.PageSize(1); i++)
            state.Memory.WritePage(1, i, (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(0, 4)));
        for (int s = 0; s < state.Strings.Length; s++)
            state.Strings[s] = $"s{s}";

        state.StoryStage = (Sys3Int)(rng.Next(2) == 0 ? 0 : rng.Next(1, 5));
        state.Week = (Sys3Int)rng.Next(9, 60);
        state.Prov_Count = (Sys3Int)(rng.Next(4) == 0 ? rng.Next(0, 100) : 100);
        state.Dgn_Count = (Sys3Int)(rng.Next(4) == 0 ? rng.Next(0, 30) : 30);
        for (int p = 0; p <= 100; p++)
        {
            var prov = state.Provs[(Sys3Int)p];
            prov.MapX = (Sys3Int)rng.Next(0, 1920);
            prov.MapY = (Sys3Int)rng.Next(0, 800);
            prov.Owner = (Sys3Int)(rng.Next(3) == 0 ? 1 : rng.Next(0, 11));
            prov.Adj1 = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 100));
            prov.Adj2 = (Sys3Int)(rng.Next(2) == 0 ? 0 : rng.Next(1, 100));
            prov.Adj3 = (Sys3Int)(rng.Next(2) == 0 ? 0 : rng.Next(1, 100));
            prov.Adj4 = (Sys3Int)(rng.Next(3) != 0 ? 0 : rng.Next(1, 100));
            prov.Adj5 = (Sys3Int)(rng.Next(4) != 0 ? 0 : rng.Next(1, 100));
        }
        for (int d = 0; d <= 30; d++)
        {
            var dgn = state.Dgns[(Sys3Int)d];
            dgn.MapX = (Sys3Int)rng.Next(0, 1920);
            dgn.MapY = (Sys3Int)rng.Next(0, 800);
            dgn.Prov = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 100));
        }
        state.MapTargetX = (Sys3Int)rng.Next(0, 1920);
        state.MapTargetY = (Sys3Int)rng.Next(0, 800);
        state.MapCamX = (Sys3Int)rng.Next(0, 1920);
        state.MapCamY = (Sys3Int)rng.Next(0, 800);
        state.Ui_DragPending = (Sys3Int)(rng.Next(8) == 0 ? 1 : 0);
        state.Ui_ScrollWait = (Sys3Int)rng.Next(0, 3);
        state.DebugMode = (Sys3Int)(rng.Next(8) == 0 ? 1 : 0);
        state.Ev_CommandMode = (Sys3Int)(rng.Next(10) == 0 ? rng.Next(1, 3) : 0);
        state.BuildWeeksLeft = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(1, 4) : 0);
        state.Bld_GirlsAcademy = (Sys3Int)rng.Next(0, 3);
        state.Bld_Library = (Sys3Int)rng.Next(0, 3);
        state.Bld_Hospital = (Sys3Int)rng.Next(0, 3);
        state.Bld_SMTower = (Sys3Int)rng.Next(0, 3);
        state.Bld_MariaLab = (Sys3Int)rng.Next(0, 3);
        state.Bld_MagicLab = (Sys3Int)rng.Next(0, 3);
        state.F_Sill_Died = (Sys3Int)(rng.Next(4) == 0 ? 1 : 0);
        state.TangeDog_UsedThisWeek = (Sys3Int)(rng.Next(3) == 0 ? 1 : 0);
        state.Unit_StrBase = (Sys3Int)rng.Next(0, 40);
        state.Items[(Sys3Int)34].Holder = (Sys3Int)(rng.Next(3) switch { 0 => 0, 1 => 100, _ => rng.Next(1, 51) });
        for (int u = 1; u <= 50; u++)
            state.Units[(Sys3Int)u].Status = (Sys3Int)rng.Next(0, 3);
        return state;
    }

    private static GameState Copy(GameState source)
    {
        var copy = new GameState();
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            copy.Memory.Var(v) = source.Memory.Var(v);
        foreach (int page in source.Memory.AllocatedPages)
            for (int i = 0; i < source.Memory.PageSize(page); i++)
                copy.Memory.WritePage(page, i, source.Memory.ReadPage(page, i));
        Array.Copy(source.Strings, copy.Strings, source.Strings.Length);
        return copy;
    }

    /// <summary><c>COMMAND.ADV</c>, read once.</summary>
    private sealed class OneProgram(ScriptProgram program) : ISceneSource
    {
        public ScriptProgram ReadProgram(string file) =>
            file == Command ? program : throw new InvalidOperationException($"{file} is a hook here.");
    }

    /// <summary><c>COMMAND</c> draws nothing; a draw is a divergence.</summary>
    private sealed class NoRandom : IScriptRandom
    {
        public Sys3Int Next(Sys3Int n) => throw new InvalidOperationException($"Unexpected draw Y 4, {n}.");
    }

    private sealed class InputBudgetException() : Exception("The input budget ran out.");

    /// <summary>
    /// The player as random pointer gestures, answered one <c>IM</c> at a time and logged with
    /// every other input. Each gesture starts with three reads at rest (the release waits before
    /// the map's own poll), so its press lands in the map loop, where the camera is the view's
    /// corner: a press aimed at a map point is placed on screen against the camera of that
    /// moment. Clicks go mostly to provinces and dungeons; after 30 gestures only menus, ending
    /// with Turn END (the last item).
    /// </summary>
    private sealed class GestureInput(GameState state, int seed, List<string> log) : ISceneInput
    {
        private readonly Random _rng = new(seed * 31 + 7);
        private readonly Queue<Func<MouseState>> _reads = new();
        private int _gestures;
        private int _answers;
        private int _cursor = 1;

        private bool Ending => _gestures > 30;

        public Task<MouseState> GetMouseAsync()
        {
            Spend();
            if (_reads.Count == 0)
                Plan();
            var m = _reads.Dequeue()();
            log.Add($"IM {m.Button} {m.X} {m.Y}");
            return Task.FromResult(m);
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape)
        {
            Spend();
            int old = _cursor;
            _cursor = shape.Value;
            log.Add($"IC {shape} {old}");
            return Task.FromResult((Sys3Int)old);
        }

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            Spend();
            int choice = Ending ? items.Count : _rng.Next(7) == 0 ? 0 : _rng.Next(1, items.Count + 1);
            log.Add($"menu {string.Join(" | ", items.Select(Format))} -> {choice}");
            return Task.FromResult(choice);
        }

        public Task<Sys3Int> WaitForKeyAsync()
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(4) == 0 ? 1 : 0);
            log.Add($"key {key}");
            return Task.FromResult(key);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            Spend();
            log.Add($"wait {ticks}");
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException("SR 0");

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");

        public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");

        public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");

        public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");

        private void Spend()
        {
            if (++_answers > 4000)
                throw new InputBudgetException();
        }

        private void Plan()
        {
            _gestures++;
            for (int i = 0; i < 3; i++)
                Rest();
            int kind = Ending ? 0 : _rng.Next(100);
            if (kind < 25)
            {
                Press(32, Screen(_rng.Next(0, 640), _rng.Next(0, 400)), 0);    // right click: the menu
            }
            else if (kind < 55)
            {
                // A province: the prologue's two most of the time in stage 0.
                int p = state.StoryStage == 0 && _rng.Next(2) == 0 ? 33 + _rng.Next(2) : _rng.Next(0, 101);
                Press(16, MapPoint(() => state.Provs[(Sys3Int)p].MapX, () => state.Provs[(Sys3Int)p].MapY, 11), _rng.Next(3) == 0 ? _rng.Next(1, 4) : 0);
            }
            else if (kind < 70)
            {
                int d = _rng.Next(0, 31);
                Press(16, MapPoint(() => state.Dgns[(Sys3Int)d].MapX, () => state.Dgns[(Sys3Int)d].MapY, 9), 0);
            }
            else if (kind < 80)
            {
                Press(16, Screen(_rng.Next(0, 640), _rng.Next(0, 400)), _rng.Next(0, 3));
            }
            else if (kind < 92)
            {
                Drag();
            }
            else
            {
                int button = _rng.Next(3) switch { 0 => 48, 1 => 32, _ => 16 };
                int x = _rng.Next(0, 640), y = _rng.Next(0, 400);
                for (int i = _rng.Next(1, 4); i > 0; i--)
                    _reads.Enqueue(() => new MouseState((Sys3Int)button, (Sys3Int)x, (Sys3Int)y));
                Rest();
            }
        }

        private void Rest() => _reads.Enqueue(() => new MouseState(Sys3Int.Zero, (Sys3Int)_lastX, (Sys3Int)_lastY));

        private int _lastX;
        private int _lastY;

        /// <summary>A press (held for <paramref name="held"/> more reads) and the release at the same point.</summary>
        private void Press(int button, Func<(int X, int Y)> at, int held)
        {
            (int X, int Y)? point = null;
            _reads.Enqueue(() =>
            {
                point = at();
                (_lastX, _lastY) = point.Value;
                return new MouseState((Sys3Int)button, (Sys3Int)_lastX, (Sys3Int)_lastY);
            });
            for (int i = 0; i < held; i++)
                _reads.Enqueue(() => new MouseState((Sys3Int)button, (Sys3Int)point!.Value.X, (Sys3Int)point.Value.Y));
            Rest();
        }

        /// <summary>A drag: a press, moves while held, the release, sometimes a press again while it coasts.</summary>
        private void Drag()
        {
            int x = _rng.Next(0, 640), y = _rng.Next(0, 400);
            _reads.Enqueue(() => { (_lastX, _lastY) = (x, y); return new MouseState((Sys3Int)16, (Sys3Int)x, (Sys3Int)y); });
            for (int i = _rng.Next(1, 5); i > 0; i--)
            {
                x = Math.Clamp(x + _rng.Next(-60, 61), 0, 639);
                y = Math.Clamp(y + _rng.Next(-40, 41), 0, 399);
                var (mx, my) = (x, y);
                int moves = _rng.Next(1, 3);
                for (int k = 0; k < moves; k++)
                    _reads.Enqueue(() => { (_lastX, _lastY) = (mx, my); return new MouseState((Sys3Int)16, (Sys3Int)mx, (Sys3Int)my); });
            }
            Rest();
            if (_rng.Next(3) == 0)
            {
                _reads.Enqueue(() => new MouseState((Sys3Int)16, (Sys3Int)_lastX, (Sys3Int)_lastY));
                Rest();
            }
        }

        private static Func<(int, int)> Screen(int x, int y) => () => (x, y);

        /// <summary>A map point near (<paramref name="x"/>, <paramref name="y"/>), placed on screen against the view's corner at the press.</summary>
        private Func<(int, int)> MapPoint(Func<Sys3Int> x, Func<Sys3Int> y, int spread)
        {
            int dx = _rng.Next(-spread, spread + 1), dy = _rng.Next(-spread, spread + 1);
            return () => (Math.Clamp(x().Value + dx - state.MapCamX.Value, 0, 639), Math.Clamp(y().Value + dy - state.MapCamY.Value, 0, 399));
        }

        private static string Format(SceneMenuItem item) =>
            $"{item.At} {string.Join(",", item.Texts.Select(t => $"{t.Text}[{string.Join(",", t.Numbers)}][{string.Join(",", t.Strings)}]"))}";
    }

    /// <summary>
    /// The other files as hooks: each call is logged with the <c>Ev_Entry</c> it sees, then the
    /// hook writes what commands and scenes write — cursors, scratch, <c>RetVal</c>, the camera,
    /// the weekly flags, a forced command mode cleared, the prologue ended by <c>Cﾃｷﾄｼ</c> — the
    /// same writes for the same (seed, call) in both runs.
    /// </summary>
    private sealed class ScribblingHooks(GameState state, int seed, List<string> log) : IStoryHooks
    {
        private int _calls;

        public Task RunEventAsync(string file, Sys3Int entry)
        {
            if (state.Ev_Entry != entry)
                throw new InvalidOperationException($"{file}: Ev_Entry {state.Ev_Entry.Value}, entry {entry.Value}");
            log.Add($"call {file} entry {entry} RetVal {state.RetVal} CallArg {state.CallArg} Prov_Cur {state.Prov_Cur} Dgn_Cur {state.Dgn_Cur}");
            var rng = new Random(seed * 1000 + ++_calls);
            if (rng.Next(2) == 0)
                state.Unit_Cur = (Sys3Int)rng.Next(0, 52);
            if (rng.Next(2) == 0)
                state.Prov_Cur = (Sys3Int)rng.Next(0, 100);
            if (rng.Next(2) == 0)
                state.Dgn_Cur = (Sys3Int)rng.Next(0, 31);
            if (rng.Next(2) == 0)
                state.Item_Cur = (Sys3Int)rng.Next(0, 50);
            if (rng.Next(2) == 0)
                state.RetVal = (Sys3Int)rng.Next(0, 40);
            if (rng.Next(2) == 0)
                state.CallArg = (Sys3Int)rng.Next(0, 14);
            if (rng.Next(2) == 0)
                state.Tmp0021 = (Sys3Int)rng.Next(0, 600);
            if (rng.Next(3) == 0)
            {
                state.MapCamX = (Sys3Int)rng.Next(0, 1920);
                state.MapCamY = (Sys3Int)rng.Next(0, 800);
            }
            if (rng.Next(3) == 0)
                state.Ui_WinType = (Sys3Int)rng.Next(0, 2) * 43;
            if (state.Ev_CommandMode != 0 && rng.Next(2) == 0)
                state.Ev_CommandMode = 0;
            if (rng.Next(2) == 0)
                state.Ui_CmdHaremAvail = (Sys3Int)rng.Next(0, 2);
            if (rng.Next(2) == 0)
                state.Ui_CmdSubordAvail = (Sys3Int)rng.Next(0, 2);
            if (rng.Next(3) == 0)
                state.Ui_CmdBuildAvail = Sys3Int.Zero;
            if (rng.Next(3) == 0)
                state.Ui_CmdMerchantAvail = Sys3Int.Zero;
            if (rng.Next(3) == 0)
                state.CmdAvail_SubTroops = Sys3Int.Zero;
            if (rng.Next(3) == 0)
                state.CmdAvail_SubTalk = Sys3Int.Zero;
            if (file == "Cﾃｷﾄｼ.ADV" && state.StoryStage == 0 && rng.Next(3) == 0)
                state.StoryStage = 99;
            return Task.CompletedTask;
        }
    }

    /// <summary>Logs every event with its lists spelled out.</summary>
    private sealed class LoggingView(List<string> log) : IScriptView
    {
        public Task ShowAsync(ScriptEvent e)
        {
            if (e is ScreenEvent)
                return Task.CompletedTask;                              // the port's semantic events: no statement in the script
            log.Add(e switch
            {
                ValueMessageEvent v => $"ValueMessageEvent {v.Text} [{string.Join(",", v.Values)}] [{string.Join(",", v.StringVariables ?? [])}]",
                MessageEvent m => $"MessageEvent {m.Text} [{string.Join(",", m.StringVariables ?? [])}]",
                EngineEvent g => $"EngineEvent {g.Command} [{string.Join(",", g.Args)}]",
                _ => e.ToString(),
            });
            return Task.CompletedTask;
        }
    }

    private static IEnumerable<string> Differences(ScriptMemory expected, ScriptMemory actual)
    {
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (expected.Var(v) != actual.Var(v))
                yield return $"VAR{v:D4} {VariableCatalog.Variables[v].Name}: COMMAND.ADV {expected.Var(v).Value}, port {actual.Var(v).Value}";
        foreach (int page in expected.AllocatedPages)
            for (int i = 0; i < expected.PageSize(page); i++)
                if (expected.ReadPage(page, i) != actual.ReadPage(page, i))
                    yield return $"page {page}[{i}]: COMMAND.ADV {expected.ReadPage(page, i).Value}, port {actual.ReadPage(page, i).Value}";
        for (int s = 0; s < expected.Strings.Length; s++)
            if (expected.Strings[s] != actual.Strings[s])
                yield return $"string {s}: COMMAND.ADV \"{expected.Strings[s]}\", port \"{actual.Strings[s]}\"";
    }
}
