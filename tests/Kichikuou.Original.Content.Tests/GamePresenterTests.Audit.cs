using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

public partial class GamePresenterTests
{
    /// <summary>The command files a random run calls, each with its differential test's shape of the state.</summary>
    private static readonly (string File, Action<GameState, Random> Shape)[] AuditedCommands =
    [
        ("Cﾌﾞｶ.ADV", ShapeSubordinates),
        ("Cﾊｰﾚﾑ.ADV", CommandSliceDifferentialTests.ShapeHarem),
        ("Cﾀｶﾗ.ADV", CommandSliceDifferentialTests.ShapeItems),
        ("各種DATA.ADV", CommandSliceDifferentialTests.ShapeAllData),
        ("Cﾒｲｷｭｳ.ADV", CommandSliceDifferentialTests.ShapeDungeon),
        ("Cﾃｷﾄｼ.ADV", CommandSliceDifferentialTests.ShapeEnemyCity),
        ("Cｼﾊｲﾄｼ.ADV", CommandSliceDifferentialTests.ShapeOwnCity),
        ("Cｼｮｳﾆﾝ.ADV", CommandSliceDifferentialTests.ShapeMerchant),
        ("Cﾆﾝｼﾞｬ.ADV", CommandSliceDifferentialTests.ShapeNinja),
        ("Cﾃｲﾄ.ADV", CommandSliceDifferentialTests.ShapeConstruction),
    ];

    /// <summary>
    /// The command files on the booted world, shaped as their differential tests shape it, with random units at hand,
    /// played by a random player through the presenter with its page check (<see cref="PageAudit"/>): the pickers, All
    /// Data and the scenes run as in the game, and no copy the presenter runs reads an off-screen page a screen drawn
    /// by hand left stale (Subordinates came back over one, 2026-10-14). The captures cannot show it: their nested files
    /// are recorded calls, so no picker is drawn there. Runs end at the pointer's budget or a path not ported yet too.
    /// </summary>
    [Fact]
    public async Task Random_runs_of_the_commands_never_read_a_page_a_screen_drawn_by_hand_left_stale()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "SET.ADV")))
            return;
        int cases = DifferentialRuns.Cases(48);
        var findings = new List<PageAudit.Finding>();
        var ends = new Dictionary<string, int>(StringComparer.Ordinal);
        var reopened = new Dictionary<string, int>(StringComparer.Ordinal);
        var failures = new List<string>();
        foreach (var (file, shape) in AuditedCommands)
        {
            for (int seed = 1; seed <= cases; seed++)
            {
                var run = await RunAsync(stopAtWeek: 0);
                var s = run.Session.State;
                var rng = new Random(seed * 7919 + file.Length);
                for (int unit = 1; unit <= 50; unit++)
                    s.Units[unit].Status = (Sys3Int)(rng.Next(5) switch { 0 => 0, 1 => 2, _ => 1 });
                shape(s, rng);
                run.Player.Fuzz = rng;
                bool closed = false;
                run.Player.OnShow = e =>
                {
                    // Coverage: a screen drawn by hand opened again after it closed (where a stale page shows).
                    if (e is PickerScreenEvent { Open: false } || e is DataScreenEvent { View: DataView.Closed })
                        closed = true;
                    else if (closed && (e is PickerScreenEvent { Open: true } || e is DataScreenEvent { View: not DataView.Closed }))
                    {
                        closed = false;
                        reopened[file] = reopened.GetValueOrDefault(file) + 1;
                    }
                };
                var audit = new PageAudit(Cgs.Value) { Context = $"{file} seed {seed}" };
                run.Presenter.Audit = audit;
                string end;
                try
                {
                    await run.Session.Script.CallFileAsync(file);
                    end = "returned";
                }
                catch (Exception e)
                {
                    end = e.GetType().Name;
                    if (e is not (InvalidOperationException or NotSupportedException) && failures.Count < 5)
                        failures.Add($"{file} seed {seed}: {e}");
                }
                ends[$"{file} {end}"] = ends.GetValueOrDefault($"{file} {end}") + 1;
                findings.AddRange(audit.Findings);
            }
        }
        foreach (var (end, count) in ends.OrderBy(e => e.Key, StringComparer.Ordinal))
            output.WriteLine($"{end}: {count}");
        foreach (string failure in failures)
            output.WriteLine(failure.Length > 1500 ? failure[..1500] : failure);
        foreach (var (opened, count) in reopened.OrderBy(e => e.Key, StringComparer.Ordinal))
            output.WriteLine($"{opened}: a screen drawn by hand opened again {count}×");
        foreach (var group in findings.GroupBy(f => (f.Kind, f.Command, f.Args, f.Origin?.Command, f.Origin?.Args, f.Origin?.Screen)))
        {
            var f = group.First();
            output.WriteLine($"{f.Kind}: {f.Command} {f.Args} ({f.Pixels} px) — stale from {f.Origin?.Command} {f.Origin?.Args} " +
                             $"under {f.Origin?.Screen}; {group.Count()}×, first in {f.Context}");
        }
        Assert.Empty(findings);
    }

    private static readonly Lazy<Kichikuou.Content.Graphics.CgLibrary> Cgs = new(() => new(LocalData.CgDirectory));

    /// <summary><c>Cﾌﾞｶ</c>'s shape (its differential test's): the command and its two sub-commands, the stage, Sill's state.</summary>
    private static void ShapeSubordinates(GameState state, Random rng)
    {
        state.Ui_CmdSubordAvail = (Sys3Int)(rng.Next(4) == 0 ? 0 : 1);
        state.F_SillCow_State = (Sys3Int)(rng.Next(6) == 0 ? 3 : 0);
        state.StoryStage = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 5));
        state.CmdAvail_SubTroops = (Sys3Int)(rng.Next(4) == 0 ? 0 : 1);
        state.CmdAvail_SubTalk = (Sys3Int)(rng.Next(4) == 0 ? 0 : 1);
        state.Gold = (Sys3Int)rng.Next(0, 3000);
    }
}
