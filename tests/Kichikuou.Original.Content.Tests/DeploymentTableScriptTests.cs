using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The deployment table (docs/ui/deployment-table.md) on the booted new game: its rows from the
/// boot's string table, and its gestures answering <c>Sﾌﾞﾀｲ</c> run from the player's files — the
/// script still hit-tests every one. Skipped without the decompile.
/// </summary>
public class DeploymentTableScriptTests(ITestOutputHelper output)
{
    [Fact]
    public async Task The_rows_of_the_booted_game_carry_the_units_names_and_troop_types()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;

        var rows = DeploymentTable.Rows(game.State);

        output.WriteLine(string.Join("; ", rows.Select(r => $"{r.Unit} {r.Name} / {r.TroopType} / {r.Text(DeploymentColumn.Cost)}")));
        Assert.Equal([1, 2, 3, 4], rows.Select(r => r.Unit));                // Rance (unit 1) to Soul (unit 4): the prologue's army
        Assert.Equal(["Rance", "Sill", "Bound", "Soul"], rows.Select(r => r.Name));
        Assert.All(rows, r => Assert.NotEmpty(r.TroopType));
    }

    /// <summary>
    /// A hover on Sill's (unit 2) row shows her details; presses on Sill, Bound (unit 3) and Soul
    /// (unit 4) build the party; a press on Bound, a member, takes him out (his cell, then left of
    /// the list); Attack sends Sill and Soul.
    /// </summary>
    [Fact]
    public async Task The_tables_presses_build_a_party_take_a_member_out_and_send_it()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;
        var s = game.State;
        for (int u = 1; u <= 4; u++)
            s.Units[u].Status = 1;
        s.Tmp0021 = 4;                                                  // up to four units,
        s.Tmp0022 = 1;                                                  // an attack
        s.Gold = 999;
        int expenses = new[] { 2, 4 }.Sum(u => (s.Units[u].DeployCost + s.Units[u].Troops / 300).Value);

        var pointer = game.Input.Pointer;
        var steps = new Queue<Action>();
        var seen = new List<string>();
        int[] Party() => [s.Bt_PlayerParty1.Value, s.Bt_PlayerParty2.Value, s.Bt_PlayerParty3.Value, s.Bt_PlayerParty4.Value];
        steps.Enqueue(() =>
        {
            var (x, y) = DeploymentTable.PointerAt(2);
            pointer.Hover(x, y);
        });
        steps.Enqueue(() => seen.Add($"hovered {s.Tmp0033.Value}, details {s.Tmp0026.Value}: {DeploymentTable.Details(s)?.Name}"));
        foreach (int unit in new[] { 2, 3, 4 })
            steps.Enqueue(() => DeploymentTable.Press(s, unit, pointer));
        steps.Enqueue(() => seen.Add("party " + string.Join(" ", Party())));
        steps.Enqueue(() => DeploymentTable.Press(s, 3, pointer));
        steps.Enqueue(() => seen.Add("party " + string.Join(" ", Party())));
        steps.Enqueue(() =>
        {
            var go = GesturePoints.PickerButton(1);
            pointer.Click(go.X, go.Y);
        });
        game.Input.Idle = () =>
        {
            if (steps.TryDequeue(out var step))
                step();
        };
        var runner = new SceneRunner(new ScenarioScripts(LocalData.ScenarioDirectory), new RecordingRules { State = s }, f => f == "Sﾌﾞﾀｲ.ADV");
        var script = new OriginalScriptContext(s, game.Random, game.View, game.Input, runner);
        runner.Bind(script);

        await runner.RunFileAsync("Sﾌﾞﾀｲ.ADV");

        output.WriteLine(string.Join("; ", seen));
        Assert.Empty(steps);
        Assert.Equal(["hovered 2, details 1: Sill", "party 2 3 4 0", "party 2 4 0 0"], seen);
        Assert.Equal(501, s.CallArg.Value);                             // Attack
        Assert.Equal([2, 4, 0, 0], Party());
        Assert.Equal([2, 4], DeploymentTable.Party(s).Places.Where(m => m is not null).Select(m => m!.Unit));   // the party panel
        Assert.Equal([1, 2, 1, 2], new[] { 1, 2, 3, 4 }.Select(u => s.Units[u].Status.Value));   // the party is spent for the week
        Assert.Equal(999 - expenses, s.Gold.Value);
    }
}
