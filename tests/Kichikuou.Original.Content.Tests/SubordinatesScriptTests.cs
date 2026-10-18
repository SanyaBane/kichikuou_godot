using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Our own Subordinates screen's orders (docs/ui/subordinates.md) answering <c>Cﾌﾞｶ</c> on the booted new game
/// moved to stage 1: <see cref="SubordinateOrder"/>'s click on the unit's cell, its answers to the troop menu
/// and the confirmation, every key wait on the way its own — the script still hit-tests the click and does
/// the order. Skipped without the decompile.
/// </summary>
public class SubordinatesScriptTests(ITestOutputHelper output)
{
    /// <summary>The booted game at stage 1, week 18, with Sill (unit 2) short of troops and both orders left.</summary>
    private static async Task<EndGame?> GameAsync()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return null;
        var s = game.State;
        s.StoryStage = 1;
        s.Week = 18;
        s.Gold = 1000;
        (s.Ui_CmdSubordAvail, s.CmdAvail_SubTroops, s.CmdAvail_SubTalk) = (1, 1, 1);
        for (int u = 1; u <= 4; u++)
            s.Units[u].Status = 1;
        s.Units[2].Troops = 100;
        s.Units[2].MaxTroops = 1000;
        return game;
    }

    /// <summary>Runs <c>Cﾌﾞｶ</c> with <paramref name="kind"/> given to Sill (unit 2) as the picker first reads the pointer.</summary>
    private static async Task<(SubordinateOrder Order, List<bool> Keys)> RunAsync(EndGame game, SubordinateOrderKind kind, int step = 0)
    {
        var s = game.State;
        SubordinateOrder? order = null;
        var keys = new List<bool>();
        game.Input.Idle = () => order ??= SubordinateOrder.Start(s, kind, 2, step, game.Input.Pointer);
        game.Input.OnKey = () => keys.Add(order?.AnswerKey() ?? false);
        for (int i = 0; i < 8; i++)
            game.Input.Menu.Enqueue(items => order?.Choose(items) ?? throw new InvalidOperationException("A menu the order does not answer."));
        await game.Script.CallFileAsync("Cﾌﾞｶ.ADV");
        return (order!, keys);
    }

    [Fact]
    public async Task Replenish_fills_the_unit_and_spends_the_troop_order_without_a_wait_the_order_does_not_answer()
    {
        if (await GameAsync() is not { } game)
            return;
        var s = game.State;

        var (order, keys) = await RunAsync(game, SubordinateOrderKind.Replenish);

        output.WriteLine($"menus {string.Join(" | ", game.Input.Menus.Select(m => string.Join(",", m.Select(r => r.Line))))}; keys {keys.Count}");
        Assert.Equal(2, game.Input.Menus.Count);                            // the troop menu, Do / Don't
        Assert.Contains(new ScriptRef("Cﾌﾞｶ.ADV", 812), game.Input.Menus[0]);
        Assert.Contains(new ScriptRef("Cﾌﾞｶ.ADV", 1026), game.Input.Menus[1]);
        Assert.All(keys, Assert.True);
        Assert.NotEmpty(keys);
        Assert.Equal(1000, s.Units[2].Troops.Value);
        Assert.Equal(1000 - 18, s.Gold.Value);                              // 900 men at one per 50
        Assert.Equal((0, 1, 1), (s.CmdAvail_SubTroops.Value, s.CmdAvail_SubTalk.Value, s.Ui_CmdSubordAvail.Value));
        Assert.Equal(new SubordinateResult(SubordinateOrderKind.Replenish, "Replenished: +900 → 1000", "Budget −1800K"), order.Result(s));
    }

    [Fact]
    public async Task Add_troops_at_its_second_step_buys_four_times_the_men_for_nine_times_the_price()
    {
        if (await GameAsync() is not { } game)
            return;
        var s = game.State;
        var (price, men) = SubordinatesCommand.Offer(s, 2);
        (price, men) = SubordinatesCommand.AddMore(price, men);
        (price, men) = SubordinatesCommand.AddMore(price, men);

        var (order, keys) = await RunAsync(game, SubordinateOrderKind.AddTroops, step: 2);

        output.WriteLine($"offer {price.Value} for {men.Value}; menus {game.Input.Menus.Count}; keys {keys.Count}");
        Assert.True(order.Succeeded);
        Assert.Equal(4, game.Input.Menus.Count);                            // the troop menu, Add More twice, Do It
        Assert.All(keys, Assert.True);
        Assert.Equal((100 + men.Value, 1000 + men.Value), (s.Units[2].Troops.Value, s.Units[2].MaxTroops.Value));
        Assert.Equal(1000 - price.Value, s.Gold.Value);
        Assert.Equal(0, s.CmdAvail_SubTroops.Value);
    }

    [Fact]
    public async Task Talk_answers_rances_thought_and_plays_the_units_talk_scene()
    {
        if (await GameAsync() is not { } game)
            return;
        var s = game.State;
        game.OnCall = file =>
        {
            if (file == "Gｼｨﾙ.ADV")
                throw new TalkScene();                                     // the scene is the player's; not run here
        };

        SubordinateOrder? order = null;
        var keys = new List<bool>();
        game.Input.Idle = () => order ??= SubordinateOrder.Start(s, SubordinateOrderKind.Talk, 2, 0, game.Input.Pointer);
        game.Input.OnKey = () => keys.Add(order?.AnswerKey() ?? false);
        game.Input.Menu.Enqueue(items => order!.Choose(items) ?? throw new InvalidOperationException("Not the troop menu."));
        await Assert.ThrowsAsync<TalkScene>(() => game.Script.CallFileAsync("Cﾌﾞｶ.ADV"));

        Assert.Equal([true], keys);                                         // "Hm... Maybe I'll meet with Sill."
        Assert.False(order!.Running);
        Assert.Equal((1, 0), (s.CmdAvail_SubTroops.Value, s.CmdAvail_SubTalk.Value));
    }

    private sealed class TalkScene : Exception;
}
