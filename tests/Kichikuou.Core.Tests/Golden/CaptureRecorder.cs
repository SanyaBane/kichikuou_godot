using System.Text;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Golden;

/// <summary>
/// Writes a golden capture from a battle run by the core itself, the way the harness writes
/// one from the original: tests of the replay machinery use it in place of game data.
/// </summary>
public sealed class CaptureRecorder
{
    private readonly List<string> _lines = [];
    private readonly Mt19937Legacy _generator;
    private readonly XSystem35Random _random;

    public CaptureRecorder(uint seed)
    {
        Seed = seed;
        _generator = new Mt19937Legacy(seed);
        _random = new XSystem35Random(_generator);
        Battle = new Battle(State, new RecordingRandom(this), new NullView(), new RecordingInput(this), new RecordingHooks(this));
    }

    public uint Seed { get; }

    public GameState State { get; } = new();

    public Battle Battle { get; }

    /// <summary>Runs on every story hook between its two dumps, with the generator (to simulate a scene).</summary>
    public Action<string, GameState, IScriptRandom>? OnHook { get; set; }

    /// <summary>
    /// Runs the root and returns the capture text: a page call into <paramref name="root"/>, or with
    /// <paramref name="labelRoot"/> (e.g. <c>MAIN.ADV@20</c> in group <paramref name="group"/>) a
    /// label root that ends at the next arrival at the label.
    /// </summary>
    public async Task<string> RecordAsync(string root, Func<Battle, Task> entry, string? labelRoot = null, string group = "week")
    {
        var enter = Dump("enter");
        string? jumped = null;
        try
        {
            await entry(Battle);
        }
        catch (ScenarioJumpException e)
        {
            jumped = e.File;
            _lines.Add($"jump {e.File} FIGT.ADV@99");
        }

        var sb = new StringBuilder();
        if (labelRoot is null)
            sb.Append("golden 1\n").Append($"seed {Seed}\n").Append($"root {root} Cﾃｷﾄｼ.ADV@1a5\n");
        else
            sb.Append("golden 2\n").Append($"seed {Seed}\n").Append($"root {root} {labelRoot} label\n").Append($"group {group}\n");
        enter.Write(new StringWriter(sb));
        foreach (var line in _lines)
            sb.Append(line).Append('\n');
        Dump("exit").Write(new StringWriter(sb));
        sb.Append(jumped is not null ? $"end jump {jumped}\n" : labelRoot is null ? "end return\n" : "end label\n");
        return sb.ToString();
    }

    private MemoryDump Dump(string tag) =>
        MemoryDump.FromMemory(tag, State.Memory, _generator,
            VariableCatalog.Bindings.Select(b => new DumpBinding(b.Var, b.Page, b.Offset, b.Cursor)));

    private void Add(string line) => _lines.Add(line);

    private sealed class RecordingRandom(CaptureRecorder recorder) : IScriptRandom
    {
        public Sys3Int Next(Sys3Int n)
        {
            var value = recorder._random.Next(n);
            recorder.Add($"rnd {n} {value} FIGT.ADV@6f");
            return value;
        }
    }

    /// <summary>Always attacks the first valid target directly, or defends; answers menus with item 1.</summary>
    private sealed class RecordingInput(CaptureRecorder recorder) : IBattleInput
    {
        public Task<Sys3Int> WaitForKeyAsync()
        {
            recorder.Add("key 0 WAO_SUB.ADV@1fe");
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            recorder.Add($"wait {ticks} 0 M.ADV@5aa");
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            var direct = menu.Find(FigtSelCommand.Direct);
            var choice = direct is { Arguments.Count: > 0 }
                ? new FigtSelChoice(FigtSelCommand.Direct, direct.Arguments[0])
                : new FigtSelChoice(FigtSelCommand.Defend, menu.Find(FigtSelCommand.Defend)!.Arguments[0]);
            recorder.Add($"pick {choice.Command} {choice.Argument} FIGT_SEL.ADV@3e2");
            return Task.FromResult(choice);
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu)
        {
            recorder.Add($"menu 1 {menu.Options.Count} FIGT.ADV@a10");
            return Task.FromResult<Sys3Int?>(menu.Options[0].Code);
        }

        public Task<Sys3Int> GetPlayingTrackAsync()
        {
            recorder.Add("music 7 FIGT_A.ADV@120");
            return Task.FromResult((Sys3Int)7);
        }
    }

    private sealed class RecordingHooks(CaptureRecorder recorder) : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry)
        {
            recorder.Add($"call {file} FIGT_A.ADV@40");
            var enter = recorder.Dump("hook-enter");
            var sb = new StringBuilder();
            enter.Write(new StringWriter(sb));
            recorder.OnHook?.Invoke(file, recorder.State, recorder._random);
            recorder.Dump("hook-exit").Write(new StringWriter(sb));
            recorder.Add(sb.ToString().TrimEnd('\n'));
            recorder.Add($"ret {file} {file}@2c0");
            return Task.CompletedTask;
        }
    }

    private sealed class NullView : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent) => Task.CompletedTask;
    }
}
