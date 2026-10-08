using System.Text;
using Kichikuou.Content.Scripts;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The replay of scene captures (<c>GoldenReplay</c> with a <c>SceneRunner</c>): on a capture
/// the core writes itself the way the harness writes one, and on the owner's <c>scenes</c>
/// captures when they exist.
/// </summary>
public sealed class SceneReplayTests(ITestOutputHelper output) : IDisposable
{
    /// <summary>The roots of the <c>scenes</c> group (<c>tools/golden/classes.txt</c>); every other scene file is a hook there.</summary>
    private static readonly HashSet<string> SceneRoots = new(StringComparer.Ordinal)
    {
        "Iﾄｳｿﾞｸ.ADV", "Iﾌﾟﾛﾛｰｸﾞ.ADV", "Iｼﾚｲﾌﾞ.ADV", "Iﾊﾝﾗﾝ.ADV", "ｵｰﾌﾟﾆﾝｸﾞ.ADV",
        "Gｼｨﾙ.ADV", "Gｿｳﾙ.ADV", "Gﾊﾞｳﾝﾄﾞ.ADV", "Gｱﾅｾﾙ.ADV",
    };

    private readonly string _dir = Directory.CreateTempSubdirectory("kichikuou-replay").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private static readonly string[] Scene =
    [
        "\t{VAR0520 = 3:",
        "\t\t@L_00010:",
        "\t}",
        "\t%0:",
        "*L_00010:",
        "\tIM VAR0202, VAR0203:",
        "\t{VAR0000:",
        "\t\t@L_00010:",
        "\t}",
        "\t$L_00020$",
        "\t'Fight'",
        "\t$",
        "\t$L_00030$",
        "\t'Rest'",
        "\t$",
        "\t]",
        "\t%0:",
        "*L_00020:",
        "\tY 4, 6:",
        "\t!VAR0001 : VAR0000!",
        "\t!VAR0520 : 1!",
        "\t%#FIGT_A.ADV:",
        "\tZT5 VAR0002:",
        "\tIK 0:",
        "\tY 3, 40:",
        "\t%0:",
        "*L_00030:",
        "\t%0:",
    ];

    [Fact]
    public async Task A_scene_capture_written_by_the_core_replays_with_no_difference()
    {
        File.WriteAllLines(Path.Combine(_dir, "GTEST.ADV"), Scene);
        string capture = await RecordAsync(choice: 1);
        output.WriteLine(string.Join('\n', capture.Split('\n').Where(l => !l.StartsWith("var", StringComparison.Ordinal) && !l.StartsWith("rngv", StringComparison.Ordinal) && !l.StartsWith("page", StringComparison.Ordinal) && !l.StartsWith("str", StringComparison.Ordinal))));

        var report = await ReplayAsync(capture);

        Assert.True(report.Passed, report.Describe());
    }

    [Fact]
    public async Task A_menu_with_another_item_count_diverges()
    {
        File.WriteAllLines(Path.Combine(_dir, "GTEST.ADV"), Scene);
        string capture = (await RecordAsync(choice: 2)).Replace("menu 2 2 ", "menu 2 3 ", StringComparison.Ordinal);

        var report = await ReplayAsync(capture);

        Assert.Contains("the menu has 2 items in the core and 3 in the original", report.Divergence, StringComparison.Ordinal);
    }

    /// <summary>The owner's captures of the prologue scenes (group <c>scenes</c>), replayed through the runtime.</summary>
    [Fact]
    public async Task The_scene_captures_replay_through_the_runtime()
    {
        string dir = Path.Combine(LocalData.GoldenDirectory, "scenes");
        if (!Directory.Exists(dir) || !Directory.Exists(LocalData.ScenarioDirectory))
            return;
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var failed = new List<string>();
        foreach (string path in Directory.EnumerateFiles(dir, "*.golden").Order(StringComparer.Ordinal))
        {
            var capture = GoldenCapture.Load(path);
            var report = await GoldenReplay.RunAsync(capture,
                entryPoint: script => script.CallFileAsync(capture.Root),
                scenes: hooks => new SceneRunner(scripts, hooks, SceneRoots.Contains));
            output.WriteLine(report.Describe(maxLines: 40));
            if (!report.Passed)
                failed.Add(Path.GetFileName(path));
        }
        Assert.Empty(failed);
    }

    private async Task<GoldenReport> ReplayAsync(string text)
    {
        string path = Path.Combine(_dir, "test.golden");
        await File.WriteAllTextAsync(path, text);
        var capture = GoldenCapture.Load(path);
        return await GoldenReplay.RunAsync(capture,
            entryPoint: script => script.CallFileAsync(capture.Root),
            scenes: hooks => new SceneRunner(new ScenarioScripts(_dir), hooks));
    }

    /// <summary>Runs <c>GTEST.ADV</c> entry 3 and writes what happened as the harness would.</summary>
    private async Task<string> RecordAsync(int choice)
    {
        var recorder = new SceneRecorder(seed: 4321);
        recorder.State.Ev_Entry = 3;
        recorder.Input.Mouse.Enqueue(new MouseState((Sys3Int)1, (Sys3Int)5, (Sys3Int)6));
        recorder.Input.Mouse.Enqueue(new MouseState((Sys3Int)1, (Sys3Int)5, (Sys3Int)6));
        recorder.Input.Mouse.Enqueue(new MouseState(Sys3Int.Zero, (Sys3Int)5, (Sys3Int)6));
        recorder.Input.Choices.Enqueue(choice);
        recorder.Input.Keys.Enqueue(32);
        recorder.OnHook = (file, state) => state.Bt_Result = 2;
        return await recorder.RecordAsync("GTEST.ADV", new ScenarioScripts(_dir));
    }

    /// <summary>Writes a capture of a scene run by the core (the scene counterpart of the core tests' <c>CaptureRecorder</c>).</summary>
    private sealed class SceneRecorder
    {
        private readonly List<string> _lines = [];
        private readonly Mt19937Legacy _generator;
        private readonly XSystem35Random _random;
        private readonly uint _seed;

        public SceneRecorder(uint seed)
        {
            _seed = seed;
            _generator = new Mt19937Legacy(seed);
            _random = new XSystem35Random(_generator);
        }

        public GameState State { get; } = new();

        public ScriptedSceneInput Input { get; } = new();

        public Action<string, GameState>? OnHook { get; set; }

        public async Task<string> RecordAsync(string root, ISceneSource scenes)
        {
            var runner = new SceneRunner(scenes, new Hooks(this));
            var script = new ScriptContext(State, new RecordingRandom(this), new RecordingView(), new RecordingInput(this), runner);
            runner.Bind(script);
            var enter = Dump("enter");
            await script.CallFileAsync(root);
            var sb = new StringBuilder();
            sb.Append("golden 2\n").Append($"seed {_seed}\n").Append($"root {root} COM.ADV@10\n").Append("group scenes\n");
            enter.Write(new StringWriter(sb));
            foreach (string line in _lines)
                sb.Append(line).Append('\n');
            Dump("exit").Write(new StringWriter(sb));
            sb.Append("end return\n");
            return sb.ToString();
        }

        private MemoryDump Dump(string tag) =>
            MemoryDump.FromMemory(tag, State.Memory, _generator,
                VariableCatalog.Bindings.Select(b => new DumpBinding(b.Var, b.Page, b.Offset, b.Cursor)));

        /// <summary>Adds a record; consecutive equal mouse reads become one run-length record, as in the harness.</summary>
        private void Add(string line)
        {
            if (line.StartsWith("mouse ", StringComparison.Ordinal) && _lines.Count > 0 && _lines[^1].StartsWith("mouse ", StringComparison.Ordinal))
            {
                string[] last = _lines[^1].Split(' ');
                string[] next = line.Split(' ');
                if (last[1] == next[1] && last[2] == next[2] && last[3] == next[3])
                {
                    last[4] = (long.Parse(last[4], System.Globalization.CultureInfo.InvariantCulture) + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                    _lines[^1] = string.Join(' ', last);
                    return;
                }
            }
            _lines.Add(line);
        }

        private sealed class RecordingRandom(SceneRecorder r) : IScriptRandom
        {
            public Sys3Int Next(Sys3Int n)
            {
                var value = r._random.Next(n);
                r.Add($"rnd {n} {value} GTEST.ADV@40");
                return value;
            }
        }

        private sealed class RecordingInput(SceneRecorder r) : ISceneInput
        {
            public async Task<Sys3Int> WaitForKeyAsync()
            {
                var key = await r.Input.WaitForKeyAsync();
                r.Add($"key {key} GTEST.ADV@50");
                return key;
            }

            public async Task<Sys3Int> PauseAsync(Sys3Int ticks)
            {
                var code = await r.Input.PauseAsync(ticks);
                r.Add($"wait {ticks} {code} GTEST.ADV@60");
                return code;
            }

            public Task<Sys3Int> GetPlayingTrackAsync() => throw new NotSupportedException();

            public async Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
            {
                int choice = await r.Input.SelectAsync(items);
                r.Add($"menu {choice} {items.Count} GTEST.ADV@70");
                return choice;
            }

            public async Task<MouseState> GetMouseAsync()
            {
                var m = await r.Input.GetMouseAsync();
                r.Add($"mouse {m.Button} {m.X} {m.Y} 1 GTEST.ADV@80");
                return m;
            }

            public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => throw new NotSupportedException();

            public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new NotSupportedException();

            public Task ResetTimerAsync(Sys3Int value) => r.Input.ResetTimerAsync(value);

            public async Task<Sys3Int> GetTimerAsync(int mode)
            {
                var value = await r.Input.GetTimerAsync(mode);
                r.Add($"timer {mode} {value} GTEST.ADV@90");
                return value;
            }

            public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new NotSupportedException();

            public Task<Sys3Int> GetMidiTrackAsync() => throw new NotSupportedException();
        }

        private sealed class Hooks(SceneRecorder r) : IStoryHooks
        {
            public Task RunEventAsync(string file, Sys3Int entry)
            {
                r.Add($"call {file} GTEST.ADV@a0");
                var sb = new StringBuilder();
                r.Dump("hook-enter").Write(new StringWriter(sb));
                r.OnHook?.Invoke(file, r.State);
                r.Dump("hook-exit").Write(new StringWriter(sb));
                r.Add(sb.ToString().TrimEnd('\n'));
                r.Add($"ret {file} {file}@2c0");
                return Task.CompletedTask;
            }
        }
    }
}
