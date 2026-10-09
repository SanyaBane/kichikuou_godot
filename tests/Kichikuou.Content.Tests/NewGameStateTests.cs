using Kichikuou.Content.Scripts;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The state a new game starts the prologue with: the port's boot and "Start"
/// (<see cref="GameSetup"/>) up to the call of <c>Iﾌﾟﾛﾛｰｸﾞ.ADV</c>, against the <c>enter</c> dump of
/// every <c>Iﾌﾟﾛﾛｰｸﾞ</c> capture of a new game (root <c>ｽﾀMENU.ADV@69d</c>). Skipped without the
/// decompile or the captures.
/// </summary>
public class NewGameStateTests(ITestOutputHelper output)
{
    /// <summary>
    /// What the port cannot know: the benchmark's results (<c>XSPEC.ADV</c>, not run, QUIRK: DAT-04)
    /// and the opening's scratch (<c>ﾌﾟﾛﾛｰｸﾞ.ADV</c>, not ported; <c>Tmp0020</c> and <c>Tmp0039</c>
    /// depend on when it was clicked away). The persistent page 4 comes from the player's slots.
    /// </summary>
    private static readonly HashSet<string> Unknowable =
        ["SpecBaseline", "SpecGraphics", "SpecCpu", "Tmp0003", "Tmp0020", "Tmp0039"];

    [Fact]
    public async Task A_new_game_enters_the_prologue_in_the_original_state()
    {
        string dir = Path.Combine(LocalData.GoldenDirectory, "scenes");
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")) || !Directory.Exists(dir))
            return;
        var captures = Directory.GetFiles(dir, "*-Iﾌﾟﾛﾛｰｸﾞ.golden")
            .Select(path => GoldenCapture.Parse(new StringReader(File.ReadAllText(path)), path))
            .Where(c => c.RootCall.ToString().Contains("ｽﾀMENU.ADV@69d", StringComparison.Ordinal))
            .ToList();
        if (captures.Count == 0)
            return;

        var state = await NewGameAsync();
        foreach (var capture in captures)
        {
            var diffs = MemoryComparison.Compare(capture.Enter, state.Memory, null, GoldenFilter.Strategic)
                .Where(d => !d.Ignored && !d.Location.Contains("(page4[", StringComparison.Ordinal)
                    && !Unknowable.Any(name => d.Location.EndsWith(" " + name, StringComparison.Ordinal)))
                .ToList();
            foreach (var d in diffs)
                output.WriteLine($"{Path.GetFileName(capture.Source)}: {d}");
            Assert.Empty(diffs);
            Assert.Equal(1000, state.MapCamX.Value);        // ignored as ui, but the first map view shows it
            Assert.Equal(400, state.MapCamY.Value);
        }
    }

    /// <summary>Boot and "Start" through the port; the call of <c>Iﾌﾟﾛﾛｰｸﾞ</c> is recorded, not run.</summary>
    private static async Task<GameState> NewGameAsync()
    {
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var stubs = new RecordingRules { State = state };
        var rules = new StrategyRules(scripts, stubs);
        var runner = new SceneRunner(scripts, rules);
        var script = new ScriptContext(state, new QueuedRandom(), new RecordingView(), new NoInput(), new Prologue(runner, stubs))
        {
            Saves = new MemorySaveStore(),
        };
        runner.Bind(script);
        rules.Bind(script);
        var setup = new GameSetup(script, scripts);
        await setup.BootAsync();
        await setup.NewGameAsync();
        Assert.Equal([("Iﾌﾟﾛﾛｰｸﾞ.ADV", 0)], stubs.Calls);
        return state;
    }

    /// <summary>The boot and "Start" ask for nothing but pauses.</summary>
    private sealed class NoInput : IGameInput
    {
        private static T No<T>() => throw new InvalidOperationException("The new game asked for input.");

        public Task<Sys3Int> WaitForKeyAsync() => No<Task<Sys3Int>>();
        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Task.FromResult(Sys3Int.Zero);
        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);
        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => No<Task<FigtSelChoice>>();
        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => No<Task<Sys3Int?>>();
        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => No<Task<int>>();
        public Task<MouseState> GetMouseAsync() => No<Task<MouseState>>();
        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => No<Task<Sys3Int>>();
        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => No<Task<IReadOnlyList<Sys3Int>>>();
        public Task ResetTimerAsync(Sys3Int value) => No<Task>();
        public Task<Sys3Int> GetTimerAsync(int mode) => No<Task<Sys3Int>>();
        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => No<Task<IReadOnlyList<Sys3Int>>>();
        public Task<Sys3Int> GetMidiTrackAsync() => No<Task<Sys3Int>>();
    }

    /// <summary>The scene calls: <c>Iﾌﾟﾛﾛｰｸﾞ</c> to the recorder, the others to the runner.</summary>
    private sealed class Prologue(IStoryHooks runner, IStoryHooks stubs) : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry) =>
            file == "Iﾌﾟﾛﾛｰｸﾞ.ADV" ? stubs.RunEventAsync(file, entry) : runner.RunEventAsync(file, entry);
    }
}
