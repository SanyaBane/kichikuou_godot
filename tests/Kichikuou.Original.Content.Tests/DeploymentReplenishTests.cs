using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Replenish on our deployment screen (owner's decision 27, docs/ui/deployment-table.md): the shortcut does to the game
/// what <c>Cﾌﾞｶ</c>'s Replenish does, and the port of <c>Sﾌﾞﾀｲ</c> does it at our screen's request, sums the party
/// again and sends it at the new war expenses. On the booted new game moved to stage 1; skipped without the decompile.
/// </summary>
public class DeploymentReplenishTests(ITestOutputHelper output)
{
    /// <summary>The booted game at stage 1, week 18, Sill (unit 2) short of troops, the week's orders left, as SubordinatesScriptTests.</summary>
    private static async Task<EndGame> GameAsync()
    {
        var game = await EndGame.BootAsync();
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

    /// <summary>What a Replenish changes, and what it must leave: the unit's record, the gold, the week's flags, the spending.</summary>
    private static string Outcome(GameState s) =>
        $"troops {s.Units[2].Troops.Value}/{s.Units[2].MaxTroops.Value}, gold {s.Gold.Value}, "
        + $"orders {s.CmdAvail_SubTroops.Value}{s.CmdAvail_SubTalk.Value}{s.Ui_CmdSubordAvail.Value}, "
        + $"military {s.History[s.Hist_Cur].MilitaryExp.Value}, status {s.Units[2].Status.Value}";

    [ScenarioFact]
    public async Task The_shortcut_does_what_subordinates_replenish_does()
    {
        var viaCommand = await GameAsync();
        var viaShortcut = await GameAsync();

        SubordinateOrder? order = null;
        viaCommand.Input.Idle = () => order ??= SubordinateOrder.Start(viaCommand.State, SubordinateOrderKind.Replenish, 2, 0, viaCommand.Input.Pointer);
        viaCommand.Input.OnKey = () => order?.AnswerKey();
        for (int i = 0; i < 4; i++)
            viaCommand.Input.Menu.Enqueue(items => order?.Choose(items) ?? throw new InvalidOperationException("A menu the order does not answer."));
        await viaCommand.Script.CallFileAsync("Cﾌﾞｶ.ADV");
        SubordinatesCommand.ReplenishNow(viaShortcut.State, 2);

        output.WriteLine(Outcome(viaCommand.State));
        Assert.Equal(1000, viaCommand.State.Units[2].Troops.Value);
        Assert.Equal(Outcome(viaCommand.State), Outcome(viaShortcut.State));
    }

    [ScenarioFact]
    public async Task The_deployment_screen_replenishes_at_our_request_and_sends_the_party_at_its_new_expenses()
    {
        var game = await GameAsync();
        var s = game.State;
        s.Tmp0021 = 4;                                                  // up to four units
        s.Tmp0022 = 1;                                                  // an attack
        var pointer = game.Input.Pointer;
        DeploymentReplenish? order = null;
        int step = 0;
        game.Input.Idle = () =>
        {
            switch (step++)
            {
                case 0:
                    order = DeploymentReplenish.Start(game.Script, 2, pointer);
                    break;
                case 1:
                    DeploymentTable.Press(s, 2, pointer);              // Sill joins
                    break;
                case 2:
                    var (x, y) = GesturePoints.PickerButton(1);         // Attack
                    pointer.Click(x, y);
                    break;
            }
        };

        await game.Script.CallFileAsync("Sﾌﾞﾀｲ.ADV");

        output.WriteLine(Outcome(s));
        Assert.NotNull(order);
        Assert.True(order!.Done);
        var expenses = s.Units[2].DeployCost.Value + 1000 / 300;
        Assert.Equal(1000, s.Units[2].Troops.Value);
        Assert.Equal(expenses, s.Tmp0016.Value);                        // summed again after the Replenish
        Assert.Equal(1000 - 18 - expenses, s.Gold.Value);               // 900 men at one per 50, then the war expenses
        Assert.Equal((0, 2), (s.CmdAvail_SubTroops.Value, s.Units[2].Status.Value));
        Assert.Equal(2, s.Bt_PlayerParty1.Value);
        Assert.Equal(0, game.Script.ReplenishRequest.Value);
        var result = order.Result(s)!;
        Assert.Equal((100, 1000, 18 + expenses), (result.TroopsBefore, result.Troops, result.Cost));
    }

    [ScenarioFact]
    public async Task No_request_starts_once_the_troop_order_is_spent()
    {
        var game = await GameAsync();
        game.State.CmdAvail_SubTroops = 0;

        Assert.Null(DeploymentReplenish.Start(game.Script, 2, game.Input.Pointer));
        Assert.Equal(0, game.Script.ReplenishRequest.Value);
        Assert.Equal(0, game.Input.Pointer.Pending);
        Assert.Equal("Replenish: used this week", DeploymentTable.RefillNote(game.State, 2));
        Assert.False(DeploymentTable.Rows(game.State).Single(r => r.Unit == 2).CanRefill);
    }
}
