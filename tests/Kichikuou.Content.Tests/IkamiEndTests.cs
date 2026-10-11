using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The end of the game (milestone 5 step 2; docs/strategy/survey-ikami.md): <c>Iｶﾐ</c> run from the
/// player's files, end to end, with the rule files it calls routed — the garrison picker
/// <c>Sﾌﾞﾀｲ</c> (<see cref="DeploymentScreen"/>), the kaiju battles <c>FIGT</c> (the battle's port),
/// <c>FIGT表示</c> 21-24 as a scene — to each of its endings, which jump to <c>OVER</c>, run from the
/// player's files to its quit. A garrison is left on 2F-5F: Bound (unit 3), Soul (unit 4), Leila (unit 5),
/// Rick (unit 7). The fights are won with the debug Repel unless a test says otherwise. Skipped
/// without the decompile.
/// </summary>
public class IkamiEndTests(ITestOutputHelper output)
{
    /// <summary>The garrisons of 2F, 3F, 4F and 5F, in the order they are picked.</summary>
    private static readonly int[] Garrisons = EndGame.Garrisons;

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

        // QUIRK: EVT-13 (survey-ikami §4.5, Q4): the check at :2373 passes, the run ends as if it held.
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

        // QUIRK: EVT-12 (survey-ikami §3, Q3): Stop empties the party and :4817-4836 opens the
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

        // QUIRK: EVT-14 (survey-ikami §3, Q5): :4838-4854 sets Rance back to 1, nothing refunds Sﾌﾞﾀｲ:206.
        Assert.True(cost.Value > 0);
        Assert.Equal(baseline.State.Gold.Value - cost.Value, game.State.Gold.Value);
        Assert.Equal(1, game.State.Units[1].Status.Value);
        Assert.Equal(9, game.State.EndingId.Value);
    }

    /// <summary>
    /// In fight <paramref name="fight"/> (counted from 1) <paramref name="unit"/>, alone in his
    /// garrison, goes in with 0 troops and 1 HP and only defends, so the kaiju kills him.
    /// </summary>
    private static void WipeIn(EndGame game, int fight, int unit)
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
    private static void LeaveItToWarg(EndGame game)
    {
        game.State.N_Warg_Stage = 2;
        game.Input.Menu.Enqueue(2);
        game.Input.Menu.Enqueue(2);
        game.Input.Menu.Enqueue(4);
    }

    /// <summary>
    /// Runs <c>Iｶﾐ</c> to its jump to <c>OVER</c>, then <c>OVER</c> from the player's files to its
    /// quit (milestone 6): each of <c>Iｶﾐ</c>'s endings (9, 10, 11, 13) counts a clear.
    /// </summary>
    private async Task RunToTheEndAsync(EndGame game, int pickers = 4)
    {
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => game.Script.CallFileAsync("Iｶﾐ.ADV"));
        output.WriteLine(string.Join(", ", game.Calls.Where(c => !c.StartsWith("FIGT.ADV", StringComparison.Ordinal))));
        output.WriteLine($"fights {game.Fights}, mouse reads {game.Input.Reads}, ending {game.State.EndingId.Value}");
        Assert.Equal("OVER.ADV", jump.File);
        Assert.Equal(pickers, game.Calls.Count(c => c.StartsWith("Sﾌﾞﾀｲ.ADV", StringComparison.Ordinal)));

        int clears = game.State.Fates[79].Fate.Value;
        await Assert.ThrowsAsync<GameQuitException>(() => game.Script.CallFileAsync("OVER.ADV"));
        Assert.Equal(clears + 1, game.State.Fates[79].Fate.Value);
    }

    /// <summary>
    /// The booted new game (<see cref="EndGame"/>), then the state God's Door leaves: story stage 4,
    /// the Ancient Ruins (35) under the cursor, no duel; Rance (unit 1) and Sill (unit 2) with him;
    /// Bound (unit 3), Soul (unit 4), Leila (unit 5) and Rick (unit 7) free for the garrisons and the
    /// Gold to send them; Warg at stage 2 and the debug commands on (Repel); unless
    /// <paramref name="picks"/> is false, the four garrisons' picks queued.
    /// </summary>
    private static async Task<EndGame?> AtGodsDoorAsync(bool picks = true)
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return null;
        var state = game.State;
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
            QueuePicks(game.Input);
        return game;
    }

    /// <summary>The four garrisons' picks: each unit's cell, then "Attack".</summary>
    private static void QueuePicks(EndInput input) => input.Pick(Garrisons);
}
