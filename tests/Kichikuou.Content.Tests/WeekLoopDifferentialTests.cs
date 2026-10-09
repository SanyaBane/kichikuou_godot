using System.Text;
using Kichikuou.Content.Scripts;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The hand port of <c>MAIN.ADV</c> (<see cref="WeekLoop"/>) against the file itself run from the
/// player's decompile by <see cref="SceneRunner"/>, on random states: both loops run for one to
/// three weeks and must make the same input requests, call the same files with the same
/// <c>Ev_Entry</c>, show the same events and leave the same memory. Every other file is a hook
/// that scribbles over what <c>COMMAND</c>, the save screen and <c>Iﾊﾝﾗﾝ</c> may change — the
/// units, the money, the cursors, scratch, the music, the story stage. A run stops at the autosave
/// of the week after the last one, or at the first Leazas week, where the port fails as not ported
/// just before the autosave the file calls (<c>MAIN.ADV:83</c>), so both runs stop at the same
/// point. Skipped without the decompile.
/// </summary>
public class WeekLoopDifferentialTests(ITestOutputHelper output)
{
    private const string Main = "MAIN.ADV";
    private const int Cases = 1500;

    [Fact]
    public async Task The_port_runs_MAIN_exactly_as_the_decompile()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, Main)))
            return;
        var program = new ScenarioScripts(LocalData.ScenarioDirectory).ReadProgram(Main);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int seed = 1; seed <= Cases; seed++)
        {
            var rng = new Random(seed);
            var state = RandomState(rng);
            int weeks = rng.Next(1, 4);
            var port = await RunAsync(Copy(state), seed, weeks, null);
            var original = await RunAsync(Copy(state), seed, weeks, program);

            int at = FirstDifference(original.Log, port.Log);
            Assert.True(at < 0, $"seed {seed}: the runs differ at entry {at}\n{Around(original.Log, port.Log, at)}");
            var differences = Differences(original.State.Memory, port.State.Memory).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));

            foreach (string feature in Features(port.Log).Distinct())
                seen[feature] = seen.GetValueOrDefault(feature) + 1;
        }

        output.WriteLine($"{Cases} runs");
        foreach (var (feature, count) in seen.OrderBy(f => f.Key, StringComparer.Ordinal))
            output.WriteLine($"  {feature}: {count}");
        string[] expected =
        [
            "call ﾛｰﾄﾞｾｰﾌﾞ.ADV", "call COMMAND.ADV", "call Iﾊﾝﾗﾝ.ADV", "stop weeks", "stop leazas",
            "music", "no music", "key", "wait", "weeks 2+",
        ];
        var missed = expected.Where(e => !seen.ContainsKey(e)).ToList();
        Assert.True(missed.Count == 0, "never reached: " + string.Join(", ", missed));
    }

    private sealed record Run(GameState State, List<string> Log);

    private static async Task<Run> RunAsync(GameState state, int seed, int weeks, ScriptProgram? program)
    {
        var log = new List<string>();
        var hooks = new ScribblingHooks(state, seed, weeks, log);
        var input = new RandomInput(seed, log);
        var view = new LoggingView(log);
        try
        {
            if (program is null)
            {
                var script = new ScriptContext(state, new NoRandom(), view, input, hooks);
                try
                {
                    await new WeekLoop(script).RunAsync();
                }
                catch (NotSupportedException e) when (e.Message.StartsWith("MAIN.ADV:83", StringComparison.Ordinal))
                {
                    // The Leazas week: the file goes on to its autosave (L_01875: RetVal := 3, the
                    // call), where the hooks stop it.
                    state.RetVal = 3;
                    log.Add("stop leazas");
                    return new Run(state, log);
                }
            }
            else
            {
                var runner = new SceneRunner(new OneProgram(program), hooks, file => file == Main);
                var script = new ScriptContext(state, new NoRandom(), view, input, runner);
                runner.Bind(script);
                await runner.RunFileAsync(Main);
            }
            log.Add("end");    // MAIN never returns
        }
        catch (StopException)
        {
        }
        return new Run(state, log);
    }

    private static IEnumerable<string> Features(List<string> log)
    {
        int saves = 0;
        foreach (string entry in log)
        {
            if (entry.StartsWith("call ", StringComparison.Ordinal))
            {
                if (entry.StartsWith("call ﾛｰﾄﾞｾｰﾌﾞ", StringComparison.Ordinal) && ++saves == 2)
                    yield return "weeks 2+";
                yield return entry[..entry.IndexOf(" entry", StringComparison.Ordinal)];
            }
            else if (entry.StartsWith("stop ", StringComparison.Ordinal))
                yield return entry;
            else if (entry.StartsWith("MusicEvent", StringComparison.Ordinal))
                yield return "music";
            else if (entry.StartsWith("key ", StringComparison.Ordinal))
                yield return "key";
            else if (entry.StartsWith("wait ", StringComparison.Ordinal))
                yield return "wait";
        }
        if (!log.Any(e => e.StartsWith("MusicEvent", StringComparison.Ordinal)))
            yield return "no music";
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
            sb.Append($"  {i,5} MAIN.ADV: {(i < original.Count ? original[i] : "-")}\n");
            sb.Append($"  {i,5} port:     {(i < port.Count ? port[i] : "-")}\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Every unbound variable and the record and history pages random, then the prologue most of
    /// the time, units 1-6 with any status and troops near their maximum (and near the 16-bit
    /// ceiling now and then), the history cursor where the harem may leave it, music on or off.
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
        for (int i = 0; i < state.Memory.PageSize(2); i++)
            state.Memory.WritePage(2, i, (Sys3Int)rng.Next(0, 1000));
        for (int s = 0; s < state.Strings.Length; s++)
            state.Strings[s] = $"s{s}";

        state.StoryStage = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(1, 5) : rng.Next(10) == 0 ? 99 : 0);
        state.Week = (Sys3Int)(rng.Next(10) == 0 ? rng.Next(0, 400) : rng.Next(8, 30));
        state.Gold = (Sys3Int)rng.Next(0, 200);
        state.BgmEnabled = (Sys3Int)rng.Next(0, 2);
        state.Hist_Cur = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 20) : 0);
        state.Hist_PrevCur = (Sys3Int)rng.Next(0, 3);
        state.Dgns[(Sys3Int)1].MapX = (Sys3Int)rng.Next(0, 1920);
        state.Dgns[(Sys3Int)1].MapY = (Sys3Int)rng.Next(0, 800);
        state.MapCamX = (Sys3Int)rng.Next(0, 1920);
        state.MapCamY = (Sys3Int)rng.Next(0, 800);
        for (int u = 1; u <= 6; u++)
            RandomUnit(state.Units[(Sys3Int)u], rng);
        return state;
    }

    private static void RandomUnit(UnitRecord unit, Random rng)
    {
        unit.Status = (Sys3Int)rng.Next(0, 3);
        unit.MaxHP = (Sys3Int)rng.Next(0, 200);
        unit.HP = (Sys3Int)rng.Next(0, 200);
        unit.MaxTroops = (Sys3Int)(rng.Next(8) == 0 ? 65535 - rng.Next(0, 3) : rng.Next(0, 3000));
        unit.Troops = (Sys3Int)(rng.Next(3) == 0 ? Math.Max(0, unit.MaxTroops.Value - rng.Next(0, 4)) : rng.Next(0, 3000));
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

    /// <summary><c>MAIN.ADV</c>, read once.</summary>
    private sealed class OneProgram(ScriptProgram program) : ISceneSource
    {
        public ScriptProgram ReadProgram(string file) =>
            file == Main ? program : throw new InvalidOperationException($"{file} is a hook here.");
    }

    /// <summary>The thieves' week draws nothing (and the Leazas week is stopped before its draws).</summary>
    private sealed class NoRandom : IScriptRandom
    {
        public Sys3Int Next(Sys3Int n) => throw new InvalidOperationException($"Unexpected draw Y 4, {n}.");
    }

    private sealed class StopException() : Exception("The run is over.");

    /// <summary>Keys and pauses answered at random and logged; <c>MAIN</c> reads no pointer.</summary>
    private sealed class RandomInput(int seed, List<string> log) : ISceneInput
    {
        private readonly Random _rng = new(seed * 31 + 7);
        private int _answers;

        public Task<Sys3Int> WaitForKeyAsync()
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(4) == 0 ? _rng.Next(1, 33) : 0);
            log.Add($"key {key}");
            return Task.FromResult(key);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(5) == 0 ? 1 : 0);
            log.Add($"wait {ticks} {key}");
            return Task.FromResult(key);
        }

        public Task<MouseState> GetMouseAsync() => throw new InvalidOperationException("IM");

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => throw new InvalidOperationException("IC");

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => throw new InvalidOperationException("]");

        public Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException("SR 0");

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");

        public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");

        public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");

        public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");

        private void Spend()
        {
            if (++_answers > 2000)
                throw new InvalidOperationException("The input budget ran out: the loop does not stop.");
        }
    }

    /// <summary>
    /// The other files as hooks: each call is logged with what it sees, then writes what the save
    /// screen, the player phase and the rebellion scene write — the same writes for the same
    /// (seed, call) in both runs. The autosave after the last week, or of a Leazas week, ends the run.
    /// </summary>
    private sealed class ScribblingHooks(GameState state, int seed, int weeks, List<string> log) : IStoryHooks
    {
        private int _calls;
        private int _saves;

        public Task RunEventAsync(string file, Sys3Int entry)
        {
            if (state.Ev_Entry != entry)
                throw new InvalidOperationException($"{file}: Ev_Entry {state.Ev_Entry.Value}, entry {entry.Value}");
            if (file == "ﾛｰﾄﾞｾｰﾌﾞ.ADV" && state.RetVal == 3)
            {
                if (state.StoryStage != 0)
                {
                    log.Add("stop leazas");
                    throw new StopException();
                }
                if (++_saves > weeks)
                {
                    log.Add("stop weeks");
                    throw new StopException();
                }
            }
            log.Add($"call {file} entry {entry} RetVal {state.RetVal} CallArg {state.CallArg} Week {state.Week} Stage {state.StoryStage} " +
                    $"Funds {state.History[state.Hist_Cur].Funds} Unit_Cur {state.Unit_Cur} Dgn_Cur {state.Dgn_Cur} Tmp {state.Tmp0001},{state.Tmp0002},{state.Tmp0003}");

            var rng = new Random(seed * 1000 + ++_calls);
            if (rng.Next(2) == 0)
                state.RetVal = (Sys3Int)rng.Next(0, 40);
            if (rng.Next(2) == 0)
                state.CallArg = (Sys3Int)rng.Next(0, 14);
            if (rng.Next(2) == 0)
                state.Tmp0001 = (Sys3Int)rng.Next(0, 600);
            if (rng.Next(2) == 0)
                state.Tmp0021 = (Sys3Int)rng.Next(0, 600);
            if (rng.Next(2) == 0)
                state.Unit_Cur = (Sys3Int)rng.Next(0, 52);
            if (rng.Next(2) == 0)
                state.Dgn_Cur = (Sys3Int)rng.Next(0, 31);
            if (rng.Next(3) == 0)
                state.Ui_WinType = (Sys3Int)rng.Next(0, 2) * 43;
            if (rng.Next(4) == 0)
                state.BgmEnabled = (Sys3Int)rng.Next(0, 2);
            if (rng.Next(4) == 0)
                state.CurrentBgm = (Sys3Int)rng.Next(0, 20);
            if (rng.Next(4) == 0)
                state.FadeSpeed = (Sys3Int)rng.Next(0, 10);
            if (file == "COMMAND.ADV")
            {
                state.Gold = (Sys3Int)rng.Next(0, 300);
                if (rng.Next(3) == 0)
                    state.Hist_Cur = (Sys3Int)rng.Next(0, 20);     // the harem's girl index (CMD-14)
                for (int u = 1; u <= 6; u++)
                    if (rng.Next(3) == 0)
                        RandomUnit(state.Units[(Sys3Int)u], rng);
                if (rng.Next(5) == 0)
                    state.StoryStage = 99;                          // the lost duel
                else if (rng.Next(20) == 0)
                    state.StoryStage = (Sys3Int)rng.Next(0, 5);
                if (rng.Next(3) == 0)
                    state.Ev_Entry = (Sys3Int)rng.Next(0, 30);
            }
            if (file == "Iﾊﾝﾗﾝ.ADV")
            {
                state.Week = state.Week + 1;
                if (rng.Next(4) == 0)
                    state.StoryStage = 0;                           // back to the thieves, to go on looping
            }
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
                yield return $"VAR{v:D4} {VariableCatalog.Variables[v].Name}: MAIN.ADV {expected.Var(v).Value}, port {actual.Var(v).Value}";
        foreach (int page in expected.AllocatedPages)
            for (int i = 0; i < expected.PageSize(page); i++)
                if (expected.ReadPage(page, i) != actual.ReadPage(page, i))
                    yield return $"page {page}[{i}]: MAIN.ADV {expected.ReadPage(page, i).Value}, port {actual.ReadPage(page, i).Value}";
        for (int s = 0; s < expected.Strings.Length; s++)
            if (expected.Strings[s] != actual.Strings[s])
                yield return $"string {s}: MAIN.ADV \"{expected.Strings[s]}\", port \"{actual.Strings[s]}\"";
    }
}
