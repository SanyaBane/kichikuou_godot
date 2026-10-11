using Kichikuou.Content.Scripts;
using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The end of story stage 3 (milestone 4 step 6; docs/strategy/stage-3-sweep.md, "End of stage 3"):
/// Kayblis (commander 135) dead after a battle — <c>死亡捕獲</c>, the battle's port, calls
/// <c>Gｹｲﾌﾞﾘｽ</c> 10, which calls <c>Iｼﾚｲﾌﾞ</c> 7, both run from the player's files. With every
/// province but Shangri-La (29) and Nothing (75) Leazas's the story moves on to stage 4 (and the
/// session goes on to the Creator phase, <c>Tﾙﾄﾞﾗ</c>, not ported yet); else the game ends, ending 8.
/// Skipped without the decompile.
/// </summary>
public class StageThreeEndTests(ITestOutputHelper output)
{
    [Fact]
    public async Task Kayblis_s_death_with_the_whole_map_Leazas_s_moves_the_story_to_stage_4()
    {
        var game = await KayblisDeadAsync();
        if (game is null)
            return;

        await game.Script.CallFileAsync("死亡捕獲.ADV");

        output.WriteLine(string.Join(", ", game.Calls));
        Assert.Equal(4, game.State.StoryStage.Value);
        Assert.Equal(0, game.State.EndingId.Value);
        AssertTheEndRan(game);
        Assert.DoesNotContain("Tﾙﾄﾞﾗ.ADV", StrategyRules.PortedFiles);  // the week's next phase stops the session
    }

    [Theory]
    [InlineData(84, 8)]                                                 // Kayblis's Castle still Kayblis's
    [InlineData(48, 4)]                                                 // Laurengrad Helman's
    [InlineData(23, 3)]                                                 // Red an independent city
    public async Task Kayblis_s_death_with_a_province_not_Leazas_s_ends_the_game_with_ending_8(int province, int owner)
    {
        var game = await KayblisDeadAsync();
        if (game is null)
            return;
        game.State.Provs[province].Owner = (Sys3Int)owner;

        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => game.Script.CallFileAsync("死亡捕獲.ADV"));

        Assert.Equal("OVER.ADV", jump.File);
        Assert.Equal(8, game.State.EndingId.Value);
        Assert.Equal(3, game.State.StoryStage.Value);
        AssertTheEndRan(game);
    }

    private static void AssertTheEndRan(Game game)
    {
        int kayblis = game.Calls.IndexOf("Gｹｲﾌﾞﾘｽ.ADV 10");
        Assert.True(kayblis >= 0, string.Join(", ", game.Calls));
        Assert.Equal("Iｼﾚｲﾌﾞ.ADV 7", game.Calls[kayblis + 1]);
        Assert.Equal(6, game.State.F_Kaybwan_State.Value);              // Gｹｲﾌﾞﾘｽ.ADV:2111-2112
        Assert.Equal(6, game.State.F_Kaybnyan_State.Value);
    }

    private sealed record Game(GameState State, ScriptContext Script, List<string> Calls);

    /// <summary>
    /// The boot and the new game with the rule files routed and the scenes run from the decompile
    /// (every draw 1, every key wait answered), then story stage 3 with the whole map Leazas's but
    /// Shangri-La (29) and Nothing (75), and Kayblis (135) dead in the enemy party of the last
    /// battle; every scene and rule call recorded as "file entry".
    /// </summary>
    private static async Task<Game?> KayblisDeadAsync()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return null;
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var unported = new RecordingRules { State = state };
        var rules = new StrategyRules(scripts, unported);
        var runner = new SceneRunner(scripts, rules);
        var calls = new List<string>();
        var script = new ScriptContext(state, new QueuedRandom { DefaultToOne = true }, new RecordingView(), new KeysOnly(),
            new Recorder(runner, state, calls))
        {
            Saves = new MemorySaveStore(),
        };
        runner.Bind(script);
        rules.Bind(script);
        var setup = new GameSetup(script, scripts);
        await setup.BootAsync();
        await setup.NewGameAsync();
        calls.Clear();

        state.StoryStage = 3;
        for (int p = 1; p <= 91; p++)
            state.Provs[p].Owner = (Sys3Int)(p is 29 or 75 ? 3 : 1);
        state.Bt_EnemyParty1 = 135;
        state.Bt_EnemyParty2 = 0;
        state.Bt_EnemyParty3 = 0;
        state.Bt_EnemyParty4 = 0;
        state.Cmdrs[135].Status = 0;
        return new Game(state, script, calls);
    }

    /// <summary>Every call to another file, recorded with its entry, then run.</summary>
    private sealed class Recorder(IStoryHooks target, GameState state, List<string> calls) : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry)
        {
            calls.Add($"{file} {state.Ev_Entry.Value}");
            return target.RunEventAsync(file, entry);
        }
    }

    /// <summary>The scenes ask for key waits and pauses only; both are answered at once.</summary>
    private sealed class KeysOnly : IGameInput
    {
        private static T No<T>() => throw new InvalidOperationException("The scenes asked for more than a key.");

        public Task<Sys3Int> WaitForKeyAsync() => Task.FromResult(Sys3Int.Zero);
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
}
