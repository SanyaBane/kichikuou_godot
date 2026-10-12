using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Golden;
using Kichikuou.Original.Script;
using Kichikuou.Original.Setup;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The state a new game starts the prologue with: the port's boot and "Start"
/// (<see cref="GameSetup"/>) up to the call of <c>Iﾌﾟﾛﾛｰｸﾞ.ADV</c>, against the <c>enter</c> dump of
/// every <c>Iﾌﾟﾛﾛｰｸﾞ</c> capture of a new game (root <c>ｽﾀMENU.ADV@69d</c>). Skipped without the
/// decompile or the captures.
/// </summary>
public class NewGameStateTests(ITestOutputHelper output)
{
    /// <summary>
    /// What the port's "Start" without the title cannot know: the benchmark's results (<c>XSPEC.ADV</c>,
    /// not run, QUIRK: DAT-04) and the title's scratch — the display's depth (<c>Tmp0003</c>, <c>ZZ3</c>),
    /// the logo's last pause (<c>Tmp0020</c>) and the last map scroll's target (<c>Tmp0039</c>,
    /// <c>M.ADV:21</c>, left by what was watched on the title first [I: Alice's opening or
    /// <c>Gｻｲﾄｳ</c>]). The persistent page 4 comes from the player's slots.
    /// </summary>
    private static readonly HashSet<string> Unknowable =
        ["SpecBaseline", "SpecGraphics", "SpecCpu", "Tmp0003", "Tmp0020", "Tmp0039"];

    [Fact]
    public async Task A_new_game_enters_the_prologue_in_the_original_state()
    {
        var captures = Captures();
        if (captures.Count == 0)
            return;

        Compare(captures, await NewGameAsync(), Unknowable);
    }

    /// <summary>
    /// The same through the title run from the player's files (milestone 6 step 2): the logo run
    /// through, the title, a click on "Start". Its own scratch is now the original's too — but for the
    /// display's depth (<c>Tmp0003</c>, the machine's), the benchmark's results, the logo's last pause
    /// (<c>Tmp0020</c>: 30 when it runs through, as in one capture; 180 in the other, clicked away)
    /// and <c>Tmp0039</c> (that capture's owner watched a map scroll on the title first, see
    /// <see cref="Unknowable"/>).
    /// </summary>
    [Fact]
    public async Task Start_on_the_title_enters_the_prologue_in_the_original_state()
    {
        var captures = Captures();
        if (captures.Count == 0)
            return;
        var game = (await EndGame.BootAsync(newGame: false))!;
        GameState? atPrologue = null;
        game.OnCall = file =>
        {
            if (file == "Iﾌﾟﾛﾛｰｸﾞ.ADV" && atPrologue is null)
                atPrologue = Snapshot(game.State);
        };
        game.Input.Pointer.Click(347, 331);                             // Start (button 6)

        await Assert.ThrowsAsync<ScenarioJumpException>(() => game.Script.CallFileAsync("ｽﾀMENU.ADV"));

        Compare(captures, atPrologue!, ["SpecBaseline", "SpecGraphics", "SpecCpu", "Tmp0003", "Tmp0020", "Tmp0039"]);
    }

    private static List<GoldenCapture> Captures()
    {
        string dir = Path.Combine(LocalData.GoldenDirectory, "scenes");
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")) || !Directory.Exists(dir))
            return [];
        return Directory.GetFiles(dir, "*-Iﾌﾟﾛﾛｰｸﾞ.golden")
            .Select(path => GoldenCapture.Parse(new StringReader(File.ReadAllText(path)), path))
            .Where(c => c.RootCall.ToString().Contains("ｽﾀMENU.ADV@69d", StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>A copy of the memory as it is now.</summary>
    private static GameState Snapshot(GameState state)
    {
        var copy = new GameState();
        var (from, to) = (state.Memory, copy.Memory);
        for (int v = 0; v < VariableCatalog.Variables.Count; v++)
            to.Var(v) = from.Var(v);
        foreach (int page in from.AllocatedPages)
        {
            to.AllocatePage(page, from.PageSize(page));
            for (int i = 0; i < from.PageSize(page); i++)
                to.WritePage(page, i, from.ReadPage(page, i));
        }
        Array.Copy(from.Strings, to.Strings, from.Strings.Length);
        return copy;
    }

    private void Compare(List<GoldenCapture> captures, GameState state, IReadOnlyCollection<string> unknowable)
    {
        foreach (var capture in captures)
        {
            var diffs = MemoryComparison.Compare(capture.Enter, state.Memory, null, GoldenFilter.Strategic)
                .Where(d => !d.Ignored && !d.Location.Contains("(page4[", StringComparison.Ordinal)
                    && !unknowable.Any(name => d.Location.EndsWith(" " + name, StringComparison.Ordinal)))
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
        var script = new OriginalScriptContext(state, new QueuedRandom(), new RecordingView(), new NoInput(), new Prologue(runner, stubs))
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
