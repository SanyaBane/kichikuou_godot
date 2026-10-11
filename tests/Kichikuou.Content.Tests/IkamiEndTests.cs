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
/// The end of the game (milestone 5 step 2; docs/strategy/survey-ikami.md): <c>Iｶﾐ</c> run from the
/// player's files, end to end, with the rule files it calls routed — the garrison picker
/// <c>Sﾌﾞﾀｲ</c> (<see cref="DeploymentScreen"/>), the kaiju battles <c>FIGT</c> (the battle's port),
/// <c>FIGT表示</c> 21-24 as a scene — to each of its endings, which jump to <c>OVER</c>
/// (<c>GameEnd</c>). A garrison is left on 2F-5F: Bound (unit 3), Soul (unit 4), Leila (unit 5),
/// Rick (unit 7). The fights are won with the debug Repel unless a test says otherwise. Skipped
/// without the decompile.
/// </summary>
public class IkamiEndTests(ITestOutputHelper output)
{
    /// <summary>The garrisons of 2F, 3F, 4F and 5F, in the order they are picked.</summary>
    private static readonly int[] Garrisons = [3, 4, 5, 7];

    [Fact]
    public async Task Charge_ends_the_game_with_ending_9()
    {
        var game = await AtGodsDoorAsync();
        if (game is null)
            return;
        game.Input.Menu.Enqueue(1);                                     // Charge

        await RunToTheEndAsync(game);

        Assert.Equal(9, game.State.EndingId.Value);
        Assert.Equal(10, game.Fights);                                  // 2F 1, 3F 2, 4F 3, 5F 4
        Assert.Equal(Garrisons, new[] { game.State.Ev_GodRear2F_Unit1, game.State.Ev_GodRear3F_Unit1,
            game.State.Ev_GodRear4F_Unit1, game.State.Ev_GodRear5F_Unit1 }.Select(u => (int)u.Value));
    }

    [Fact]
    public async Task Create_Chaos_with_Sill_at_Rance_s_side_ends_the_game_with_ending_10()
    {
        var game = await AtGodsDoorAsync();
        if (game is null)
            return;
        game.Input.Menu.Enqueue(2);                                     // Think: "Create Chaos" shows
        game.Input.Menu.Enqueue(3);                                     // Create Chaos

        await RunToTheEndAsync(game);

        Assert.Equal(10, game.State.EndingId.Value);
        Assert.Equal(3, game.Input.MenuSizes[1]);
    }

    [Fact]
    public async Task Create_Chaos_without_Sill_ends_the_game_with_ending_9()
    {
        var game = await AtGodsDoorAsync();
        if (game is null)
            return;
        game.State.Units[2].Status = 0;                                 // Sill (unit 2) gone
        game.Input.Menu.Enqueue(2);
        game.Input.Menu.Enqueue(3);

        await RunToTheEndAsync(game);

        Assert.Equal(9, game.State.EndingId.Value);
    }

    [Fact]
    public async Task Without_Warg_a_second_Think_finds_nothing_and_Leave_it_to_Warg_never_shows()
    {
        var game = await AtGodsDoorAsync();
        if (game is null)
            return;
        game.State.N_Warg_Stage = 1;
        game.Input.Menu.Enqueue(2);
        game.Input.Menu.Enqueue(2);                                     // "Can't think of anything"
        game.Input.Menu.Enqueue(1);

        await RunToTheEndAsync(game);

        Assert.Equal([2, 3, 3], game.Input.MenuSizes);
        Assert.Equal(9, game.State.EndingId.Value);
    }

    [Fact]
    public async Task Leave_it_to_Warg_with_every_garrison_held_and_Sill_alive_ends_with_ending_11()
    {
        var game = await AtGodsDoorAsync();
        if (game is null)
            return;
        LeaveItToWarg(game);

        await RunToTheEndAsync(game);

        Assert.Equal(11, game.State.EndingId.Value);
        Assert.Equal(1, game.State.F_Sill_Fate.Value);
        Assert.Equal(2, game.State.F_Ending_LiaRoute.Value);
        Assert.Equal(20, game.Fights);                                  // 10 down, 10 back
        Assert.DoesNotContain("FIGT表示.ADV", game.Calls.Select(c => c.Split(' ')[0]));
    }

    [Fact]
    public async Task Leave_it_to_Warg_with_Sill_dead_ends_with_ending_13()
    {
        var game = await AtGodsDoorAsync();
        if (game is null)
            return;
        game.State.Units[2].Status = 0;
        LeaveItToWarg(game);

        await RunToTheEndAsync(game);

        Assert.Equal(13, game.State.EndingId.Value);
        Assert.Equal(1, game.State.F_Ending_LiaRoute.Value);
    }

    [Fact]
    public async Task A_garrison_wiped_on_the_way_down_is_found_lost_on_the_way_back_ending_9()
    {
        var game = await AtGodsDoorAsync();
        if (game is null)
            return;
        LeaveItToWarg(game);
        WipeIn(game, 7, 7);                                             // Rick (unit 7), 5F's first fight

        await RunToTheEndAsync(game);

        Assert.Equal(9, game.State.EndingId.Value);
        Assert.Equal(0, game.State.Units[7].Status.Value);
        Assert.Contains("FIGT表示.ADV 24", game.Calls);                // :2272 finds 5F's garrison empty
        Assert.Equal(13, game.Fights);                                  // 10 down; 2F, 3F, 4F at :2269-2271
        Assert.Equal(1, game.State.Tmp0229.Value);
    }

    [Fact]
    public async Task A_garrison_wiped_in_its_last_fight_is_never_found_lost()
    {
        var game = await AtGodsDoorAsync();
        if (game is null)
            return;
        LeaveItToWarg(game);
        WipeIn(game, 20, 3);                                            // Bound (unit 3), 2F's last fight (:2362)

        await RunToTheEndAsync(game);

        // QUIRK candidate Q4 (survey-ikami §4.5): the check at :2373 passes, the run ends as if it held.
        Assert.Equal(11, game.State.EndingId.Value);
        Assert.Equal(0, game.State.Units[3].Status.Value);
        Assert.Equal(0, game.State.Ev_GodRear2F_Unit1.Value);
        Assert.Equal(0, game.State.Tmp0226.Value);
        Assert.DoesNotContain(game.Calls, c => c.StartsWith("FIGT表示.ADV", StringComparison.Ordinal));
        Assert.Equal(20, game.Fights);
    }

    [Fact]
    public async Task With_nobody_but_Rance_free_the_garrison_picker_never_ends()
    {
        var game = await AtGodsDoorAsync(picks: false);
        if (game is null)
            return;
        int[] gone = [2, .. Garrisons];
        foreach (int unit in gone)
            game.State.Units[unit].Status = 0;
        var stop = PointerScript.PickerButton(3);
        for (int k = 0; k < 3; k++)
            game.Input.Pointer.Click(stop.X, stop.Y);

        // QUIRK candidate Q3 (survey-ikami §3): Stop empties the party and :4817-4836 opens the
        // picker again; "Attack" without a unit is refused inside Sﾌﾞﾀｲ. No way on.
        var e = await Assert.ThrowsAsync<InvalidOperationException>(() => game.Script.CallFileAsync("Iｶﾐ.ADV"));

        Assert.Equal("The mouse is read with nothing left to click.", e.Message);
        Assert.Equal(4, game.Calls.Count(c => c.StartsWith("Sﾌﾞﾀｲ.ADV", StringComparison.Ordinal)));
        Assert.Equal(0, game.Fights);
    }

    [Fact]
    public async Task Picking_Rance_for_a_garrison_is_refused_but_the_war_expenses_stay_paid()
    {
        var baseline = await AtGodsDoorAsync();
        if (baseline is null)
            return;
        baseline.Input.Menu.Enqueue(1);
        await RunToTheEndAsync(baseline);

        var game = await AtGodsDoorAsync(picks: false);
        game!.Input.Menu.Enqueue(1);
        var rance = PointerScript.PickerCell(1);
        var attack = PointerScript.PickerButton(1);
        game.Input.Pointer.Click(rance.X, rance.Y);
        game.Input.Pointer.Click(attack.X, attack.Y);
        QueuePicks(game.Input);
        game.State.Units[1].Troops = 1500;                              // Rance never fights here: his troops only price him
        var cost = game.State.Units[1].DeployCost + game.State.Units[1].Troops / 300;

        await RunToTheEndAsync(game, pickers: 5);

        // QUIRK candidate Q5 (survey-ikami §3): :4838-4854 sets Rance back to 1, nothing refunds Sﾌﾞﾀｲ:206.
        Assert.True(cost.Value > 0);
        Assert.Equal(baseline.State.Gold.Value - cost.Value, game.State.Gold.Value);
        Assert.Equal(1, game.State.Units[1].Status.Value);
        Assert.Equal(9, game.State.EndingId.Value);
    }

    /// <summary>
    /// In fight <paramref name="fight"/> (counted from 1) <paramref name="unit"/>, alone in his
    /// garrison, goes in with 0 troops and 1 HP and only defends, so the kaiju kills him.
    /// </summary>
    private static void WipeIn(Game game, int fight, int unit)
    {
        game.OnCall = file =>
        {
            if (file != "FIGT.ADV" || game.Fights != fight)
                return;
            game.State.Units[unit].Troops = 0;
            game.State.Units[unit].HP = 1;
        };
        var repel = game.Input.Command;
        game.Input.Command = (state, menu) => game.Fights == fight
            ? new FigtSelChoice(FigtSelCommand.Defend, menu.Find(FigtSelCommand.Defend)!.Arguments[0])
            : repel(state, menu);
    }

    /// <summary>The menu answers of the way back: Think twice (with Warg at stage 2), Leave it to Warg.</summary>
    private static void LeaveItToWarg(Game game)
    {
        game.State.N_Warg_Stage = 2;
        game.Input.Menu.Enqueue(2);
        game.Input.Menu.Enqueue(2);
        game.Input.Menu.Enqueue(4);
    }

    /// <summary>Runs <c>Iｶﾐ</c> to its jump to <c>OVER</c>.</summary>
    private async Task RunToTheEndAsync(Game game, int pickers = 4)
    {
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => game.Script.CallFileAsync("Iｶﾐ.ADV"));
        output.WriteLine(string.Join(", ", game.Calls.Where(c => !c.StartsWith("FIGT.ADV", StringComparison.Ordinal))));
        output.WriteLine($"fights {game.Fights}, mouse reads {game.Input.Reads}, ending {game.State.EndingId.Value}");
        Assert.Equal("OVER.ADV", jump.File);
        Assert.Equal(pickers, game.Calls.Count(c => c.StartsWith("Sﾌﾞﾀｲ.ADV", StringComparison.Ordinal)));
    }

    private sealed class Game(GameState state, ScriptContext script, EndInput input, Recorder recorder)
    {
        public GameState State { get; } = state;
        public ScriptContext Script { get; } = script;
        public EndInput Input { get; } = input;
        public List<string> Calls => recorder.Calls;
        public int Fights => Calls.Count(c => c.StartsWith("FIGT.ADV", StringComparison.Ordinal));

        /// <summary>Runs on every call, after it is recorded and before it runs.</summary>
        public Action<string>? OnCall { get => recorder.OnCall; set => recorder.OnCall = value; }
    }

    /// <summary>
    /// The boot and the new game with the rule files routed and the scenes run from the decompile
    /// (every draw 1), then the state God's Door leaves: story stage 4, the Ancient Ruins (35)
    /// under the cursor, no duel; Rance (unit 1) and Sill (unit 2) with him; Bound (unit 3), Soul
    /// (unit 4), Leila (unit 5) and Rick (unit 7) free for the garrisons and the Gold to send them;
    /// Warg at stage 2 and the debug commands on (Repel); unless <paramref name="picks"/> is false, the
    /// four garrisons' picks queued.
    /// </summary>
    private static async Task<Game?> AtGodsDoorAsync(bool picks = true)
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return null;
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var state = new GameState();
        var rules = new StrategyRules(scripts, new RecordingRules { State = state });
        var runner = new SceneRunner(scripts, rules);
        var input = new EndInput(state);
        var recorder = new Recorder(runner, state);
        var script = new ScriptContext(state, new QueuedRandom { DefaultToOne = true }, new RecordingView(), input, recorder)
        {
            Saves = new MemorySaveStore(),
        };
        runner.Bind(script);
        rules.Bind(script);
        var setup = new GameSetup(script, scripts);
        await setup.BootAsync();
        await setup.NewGameAsync();
        recorder.Calls.Clear();

        state.StoryStage = 4;
        state.Prov_Cur = 35;
        state.Bt_IsDuel = 0;
        state.Gold = 30000;
        state.DebugMode = 1;
        state.N_Warg_Stage = 2;
        int[] free = [1, 2, .. Garrisons];
        foreach (int unit in free)
            state.Units[unit].Status = 1;
        if (picks)
            QueuePicks(input);
        return new Game(state, script, input, recorder);
    }

    /// <summary>The four garrisons' picks: each unit's cell, then "Attack".</summary>
    private static void QueuePicks(EndInput input)
    {
        foreach (int unit in Garrisons)
        {
            var at = PointerScript.PickerCell(unit);
            input.Pointer.Click(at.X, at.Y);
            var attack = PointerScript.PickerButton(1);
            input.Pointer.Click(attack.X, attack.Y);
        }
    }

    /// <summary>Every call to another file, recorded with its entry, then run.</summary>
    private sealed class Recorder(IStoryHooks target, GameState state) : IStoryHooks
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

    /// <summary>
    /// Key waits and pauses answered at once; the mouse from a <see cref="PointerScript"/> (the
    /// garrison picks); the menus from a queue; every battle turn answered by
    /// <see cref="Command"/> (Repel by default). A run that reads the mouse with nothing left to
    /// click fails instead of waiting for ever.
    /// </summary>
    private sealed class EndInput(GameState state) : IGameInput
    {
        public PointerScript Pointer { get; } = new();
        public Queue<int> Menu { get; } = new();
        public List<int> MenuSizes { get; } = [];
        public int Reads { get; private set; }
        private int _idle;

        /// <summary>The answer to a battle turn; Repel by default.</summary>
        public Func<GameState, FigtSelMenu, FigtSelChoice> Command { get; set; } = (_, menu) =>
            new FigtSelChoice(FigtSelCommand.Repel, (menu.Find(FigtSelCommand.Repel)
                ?? throw new InvalidOperationException("No Repel: is DebugMode 1?")).Arguments[0]);

        public Task<Sys3Int> WaitForKeyAsync() => Task.FromResult(Sys3Int.Zero);
        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Task.FromResult(Sys3Int.Zero);
        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);

        public Task<MouseState> GetMouseAsync()
        {
            Reads++;
            var m = Pointer.Next(out bool read);
            _idle = read || Pointer.Pending > 0 ? 0 : _idle + 1;
            if (_idle > 1000)
                throw new InvalidOperationException("The mouse is read with nothing left to click.");
            return Task.FromResult(m);
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Task.FromResult(shape);

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            MenuSizes.Add(items.Count);
            return Task.FromResult(Menu.Count > 0 ? Menu.Dequeue() : throw new InvalidOperationException($"A menu of {items.Count} items with no answer left."));
        }

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu) => Task.FromResult(Command(state, menu));

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => throw new InvalidOperationException("A battle menu in Iｶﾐ.");
        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");
        public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");
        public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");
        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");
        public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");
    }
}
