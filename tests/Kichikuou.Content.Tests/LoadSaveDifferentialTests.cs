using System.Text;
using Kichikuou.Content.Scripts;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The hand port of <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c> (<see cref="LoadSaveScreen"/>) against the file itself run
/// from the player's decompile by <see cref="SceneRunner"/>, on random states and random save
/// slots (empty, filled, failing to write): every mode (<c>RetVal</c> 0, 1, 2, 3, 10, 11 and
/// others) and the way back after a load (<c>:34</c> with <c>RetVal</c> 0, the file run from there)
/// must make the same input requests, show the same events (the menus with their captured
/// texts), leave the same memory and the same slots with the same times, and load the same slot.
/// Skipped without the decompile.
/// </summary>
public class LoadSaveDifferentialTests(ITestOutputHelper output)
{
    private const string LoadSave = "ﾛｰﾄﾞｾｰﾌﾞ.ADV";
    private const int Cases = 800;
    private static readonly ScriptRef AutosaveCall = new("MAIN.ADV", 20);

    [Fact]
    public async Task The_port_runs_LoadSave_exactly_as_the_decompile()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, LoadSave)))
            return;
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var program = scripts.ReadProgram(LoadSave);
        var resume = FromStatement(program, AfterQd(program));
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);

        for (int seed = 1; seed <= Cases; seed++)
        {
            var rng = new Random(seed);
            var state = RandomState(rng);
            var slots = RandomSlots(rng, state);
            var failing = rng.Next(8) == 0 ? new HashSet<int> { 1, rng.Next(2, 11) } : [];
            int mode = rng.Next(16) switch
            {
                0 or 1 => 0,
                2 or 3 => 1,
                4 or 5 or 6 => 2,
                7 or 8 => 3,
                9 => 10,
                10 => 11,
                11 => rng.Next(4, 10),
                _ => -1,                                    // the way back after a load
            };
            // The way back after a load starts with RetVal 0; 1 and 255 (what QD leaves) take the autosave's notice and error paths.
            state.RetVal = (Sys3Int)(mode >= 0 ? mode : rng.Next(3) == 0 ? rng.Next(2) * 254 + 1 : 0);

            var port = await RunAsync(Copy(state), slots, failing, seed, script => mode >= 0
                ? new LoadSaveScreen(script, scripts).RunAsync()
                : new LoadSaveScreen(script, scripts).ResumeAsync());
            var original = await RunAsync(Copy(state), slots, failing, seed, script =>
            {
                var runner = new SceneRunner(new OneProgram(mode >= 0 ? program : resume), new NoHooks(), file => file == LoadSave);
                runner.Bind(script);
                return runner.RunFileAsync(LoadSave);
            }, bindRunner: true);

            int at = FirstDifference(original.Log, port.Log);
            Assert.True(at < 0, $"seed {seed} (mode {mode}): the runs differ at entry {at}\n{Around(original.Log, port.Log, at)}");
            var differences = Differences(original.State.Memory, port.State.Memory).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed} (mode {mode}): memory differs\n  " + string.Join("\n  ", differences));
            for (int slot = 1; slot <= ISaveStore.SlotCount; slot++)
            {
                Assert.True(Bytes(original.Store, slot).SequenceEqual(Bytes(port.Store, slot)), $"seed {seed} (mode {mode}): slot {slot} differs");
                Assert.Equal(original.Store.Timestamp(slot), port.Store.Timestamp(slot));
            }

            foreach (string feature in Features(port.Log, mode))
                seen[feature] = seen.GetValueOrDefault(feature) + 1;
        }

        output.WriteLine($"{Cases} runs");
        foreach (var (feature, count) in seen.OrderBy(f => f.Key, StringComparer.Ordinal))
            output.WriteLine($"  {feature}: {count}");
        string[] expected =
        [
            "mode 0", "mode 1", "mode 2", "mode 3", "mode 10", "mode 11", "mode other", "resume",
            "loaded 1", "loaded 2+", "saved", "write refused", "empty slot item", "relisted", "menu load", "menu save", "menu cancel",
            "save error", "load complete",
        ];
        var missed = expected.Where(e => !seen.ContainsKey(e)).ToList();
        Assert.True(missed.Count == 0, "never reached: " + string.Join(", ", missed));
    }

    private sealed record Run(GameState State, List<string> Log, TestStore Store);

    private static async Task<Run> RunAsync(
        GameState state, IReadOnlyDictionary<int, SaveData> slots, IReadOnlySet<int> failing, int seed,
        Func<ScriptContext, Task> run, bool bindRunner = false)
    {
        var log = new List<string>();
        var store = new TestStore(failing, log);
        foreach (var (slot, data) in slots)
            store.Fill(slot, data);
        var script = new ScriptContext(state, new NoRandom(), new LoggingView(log), new RandomInput(seed, log), new NoHooks())
        {
            Saves = store,
            SaveResume = AutosaveCall,
        };
        try
        {
            await run(script);
            log.Add("return");
        }
        catch (GameLoadedException e)
        {
            log.Add($"loaded {e.Slot} {e.Resume}");
        }
        return new Run(state, log, store);
    }

    /// <summary>The statement right after the file's only <c>QD</c>, where a load comes back.</summary>
    private static int AfterQd(ScriptProgram program)
    {
        for (int i = 0; i < program.Statements.Count; i++)
            if (program.Statements[i] is SaveDataStatement { Command: "QD" })
                return i + 1;
        throw new InvalidOperationException("No QD in the file.");
    }

    /// <summary>The program with its first statement turned into a jump to <paramref name="start"/>: runs from there.</summary>
    private static ScriptProgram FromStatement(ScriptProgram program, int start)
    {
        var statements = program.Statements.ToList();
        statements[0] = new GotoStatement(statements[0].At, "resume", start);
        return new ScriptProgram(program.File, statements, program.Labels);
    }

    private static IEnumerable<string> Features(List<string> log, int mode)
    {
        yield return mode switch { -1 => "resume", 0 or 1 or 2 or 3 or 10 or 11 => $"mode {mode}", _ => "mode other" };
        foreach (string entry in log)
        {
            if (entry.StartsWith("loaded ", StringComparison.Ordinal))
                yield return entry.StartsWith("loaded 1 ", StringComparison.Ordinal) ? "loaded 1" : "loaded 2+";
            else if (entry.StartsWith("menu ", StringComparison.Ordinal) && entry.Contains("@ﾛｰﾄﾞｾｰﾌﾞ.ADV:319 ", StringComparison.Ordinal))
                yield return "empty slot item";
            else if (entry.StartsWith("choice ", StringComparison.Ordinal))
            {
                string chosen = entry[(entry.LastIndexOf(' ') + 1)..];
                if (entry.Contains("ﾛｰﾄﾞｾｰﾌﾞ.ADV:91,", StringComparison.Ordinal))
                    yield return chosen switch { "ﾛｰﾄﾞｾｰﾌﾞ.ADV:91" => "menu load", "ﾛｰﾄﾞｾｰﾌﾞ.ADV:95" => "menu save", _ => "menu cancel" };
                else if (chosen == "ﾛｰﾄﾞｾｰﾌﾞ.ADV:319")
                    yield return "relisted";
                else if (chosen.StartsWith("ﾛｰﾄﾞｾｰﾌﾞ.ADV:", StringComparison.Ordinal) && int.Parse(chosen[(chosen.IndexOf(':') + 1)..]) is >= 355 and <= 443)
                    yield return "saved";
            }
            else if (entry.StartsWith("write ", StringComparison.Ordinal) && entry.EndsWith(" refused", StringComparison.Ordinal))
                yield return "write refused";
            else if (entry == "MessageEvent ﾛｰﾄﾞｾｰﾌﾞ.ADV:41 []")
                yield return "save error";
            else if (entry == "MessageEvent ﾛｰﾄﾞｾｰﾌﾞ.ADV:71 []")
                yield return "load complete";
        }
    }

    private static byte[] Bytes(TestStore store, int slot)
    {
        var data = store.Peek(slot);
        if (data is null)
            return [];
        var stream = new MemoryStream();
        data.WriteTo(stream);
        return stream.ToArray();
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
            sb.Append($"  {i,5} file: {(i < original.Count ? original[i] : "-")}\n");
            sb.Append($"  {i,5} port: {(i < port.Count ? port[i] : "-")}\n");
        }
        return sb.ToString();
    }

    /// <summary>Every unbound variable small and random, the scratch buffer and page 4 random, a week, a window open or not.</summary>
    private static GameState RandomState(Random rng)
    {
        var state = new GameState();
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (!GameState.IsBound(v))
                state.Memory.Var(v) = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(5) == 0 ? rng.Next(0, 3000) : rng.Next(0, 12));
        for (int i = 0; i < state.Memory.PageSize(3); i += 1 + rng.Next(50))
            state.Memory.WritePage(3, i, (Sys3Int)rng.Next(0, 100));
        for (int i = 0; i < state.Memory.PageSize(4); i += 1 + rng.Next(30))
            state.Memory.WritePage(4, i, (Sys3Int)rng.Next(0, 5));
        for (int s = 0; s < 20; s++)
            state.Strings[s] = $"s{s}";
        state.Week = (Sys3Int)rng.Next(1, 600);
        state.Ui_WinType = (Sys3Int)(rng.Next(3) == 0 ? 0 : 43);
        state.Ui_SaveLoadDirect = (Sys3Int)rng.Next(0, 2);
        state.CurrentBgm = (Sys3Int)rng.Next(0, 20);
        state.Buf_Cur = (Sys3Int)rng.Next(0, 50);
        state.MapCamX = (Sys3Int)rng.Next(0, 1920);
        state.MapCamY = (Sys3Int)rng.Next(0, 800);
        return state;
    }

    /// <summary>
    /// Slots 1-10 (and now and then 11) empty or filled: copies of one random save with their own
    /// week (<c>Tmp0021</c>), number (<c>Tmp0022</c>), probe (<c>Tmp0039</c>), page 4 and call.
    /// </summary>
    private static Dictionary<int, SaveData> RandomSlots(Random rng, GameState state)
    {
        var slots = new Dictionary<int, SaveData>();
        var source = Copy(state);
        source.Week = (Sys3Int)rng.Next(1, 600);
        var baseline = SaveData.Capture(source.Memory, AutosaveCall);
        double empty = rng.NextDouble();
        for (int slot = 1; slot <= 11; slot++)
        {
            if (rng.NextDouble() < empty || slot == 11 && rng.Next(4) != 0)
                continue;
            var data = rng.Next(5) == 0
                ? new SaveData(new ScriptRef("MAIN.ADV", 83), baseline.Variables, baseline.Pages, baseline.Strings).Clone()
                : baseline.Clone();
            data.Variables[21] = (Sys3Int)rng.Next(1, 600);
            data.Variables[22] = (Sys3Int)slot;
            data.Variables[39] = (Sys3Int)rng.Next(0, 9);
            data.Variables[194] = (Sys3Int)rng.Next(0, 9);
            var fates = data.Pages[4];
            for (int i = 0; i < 30; i++)
                fates[rng.Next(fates.Length)] = (Sys3Int)rng.Next(0, 9);
            slots[slot] = data;
        }
        return slots;
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

    private static IEnumerable<string> Differences(ScriptMemory expected, ScriptMemory actual)
    {
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (expected.Var(v) != actual.Var(v))
                yield return $"VAR{v:D4} {VariableCatalog.Variables[v].Name}: file {expected.Var(v).Value}, port {actual.Var(v).Value}";
        foreach (int page in expected.AllocatedPages)
            for (int i = 0; i < expected.PageSize(page); i++)
                if (expected.ReadPage(page, i) != actual.ReadPage(page, i))
                    yield return $"page {page}[{i}]: file {expected.ReadPage(page, i).Value}, port {actual.ReadPage(page, i).Value}";
        for (int s = 0; s < expected.Strings.Length; s++)
            if (expected.Strings[s] != actual.Strings[s])
                yield return $"string {s}: file \"{expected.Strings[s]}\", port \"{actual.Strings[s]}\"";
    }

    /// <summary>Slots in memory with a clock that ticks per write; some slots refuse writes; every access is logged.</summary>
    private sealed class TestStore(IReadOnlySet<int> failing, List<string> log) : ISaveStore
    {
        private readonly Dictionary<int, (SaveData Data, DateTime Time)> _slots = [];
        private DateTime _now = new(2026, 10, 9, 12, 0, 0);

        public void Fill(int slot, SaveData data) => _slots[slot] = (data.Clone(), Tick());

        public SaveData? Peek(int slot) => _slots.TryGetValue(slot, out var s) ? s.Data : null;

        public SaveData? Read(int slot)
        {
            log.Add($"read {slot}{(_slots.ContainsKey(slot) ? "" : " empty")}");
            return _slots.TryGetValue(slot, out var s) ? s.Data.Clone() : null;
        }

        public bool Write(int slot, SaveData data)
        {
            if (failing.Contains(slot))
            {
                log.Add($"write {slot} refused");
                return false;
            }
            _slots[slot] = (data.Clone(), Tick());
            log.Add($"write {slot} {data.Resume}");
            return true;
        }

        public DateTime? Timestamp(int slot)
        {
            log.Add($"time {slot}");
            return _slots.TryGetValue(slot, out var s) ? s.Time : null;
        }

        private DateTime Tick() => _now = _now.AddSeconds(37);
    }

    /// <summary><c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>, read once.</summary>
    private sealed class OneProgram(ScriptProgram program) : ISceneSource
    {
        public ScriptProgram ReadProgram(string file) =>
            file == LoadSave ? program : throw new InvalidOperationException($"{file} is not expected here.");
    }

    private sealed class NoHooks : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry) => throw new InvalidOperationException($"Unexpected call of {file}.");
    }

    private sealed class NoRandom : IScriptRandom
    {
        public Sys3Int Next(Sys3Int n) => throw new InvalidOperationException($"Unexpected draw Y 4, {n}.");
    }

    /// <summary>Keys, the pointer and the menus answered at random and logged; after a few menus every menu is cancelled.</summary>
    private sealed class RandomInput(int seed, List<string> log) : ISceneInput
    {
        private readonly Random _rng = new(seed * 31 + 7);
        private int _answers;
        private int _menus;

        public Task<Sys3Int> WaitForKeyAsync()
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(4) == 0 ? _rng.Next(1, 33) : 0);
            log.Add($"key {key}");
            return Task.FromResult(key);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks)
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(5) == 0 ? 1 : 0);
            log.Add($"wait {ticks} {key}");
            return Task.FromResult(key);
        }

        public Task<MouseState> GetMouseAsync()
        {
            Spend();
            var m = new MouseState((Sys3Int)(_rng.Next(3) == 0 ? _rng.Next(1, 3) * 16 : 0), (Sys3Int)_rng.Next(0, 640), (Sys3Int)_rng.Next(0, 400));
            log.Add($"mouse {m}");
            return Task.FromResult(m);
        }

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            Spend();
            int choice = ++_menus > 6 || _rng.Next(5) == 0 ? 0 : _rng.Next(1, items.Count + 1);
            string list = string.Join("; ", items.Select(i =>
                $"@{i.At} " + string.Join(" + ", i.Texts.Select(t => $"{t.Text} [{string.Join(",", t.Numbers)}] [{string.Join(",", t.Strings)}]"))));
            log.Add($"menu {list}");
            log.Add($"choice {choice} of [{string.Join(", ", items.Select(i => i.At))}] {(choice > 0 ? items[choice - 1].At.ToString() : "cancel")}");
            return Task.FromResult(choice);
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => throw new InvalidOperationException("IC");

        public Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException("SR 0");

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");

        public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");

        public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");

        public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");

        private void Spend()
        {
            if (++_answers > 3000)
                throw new InvalidOperationException("The input budget ran out: the loop does not stop.");
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
}
