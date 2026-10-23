using System.Reflection;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Xunit.Abstractions;

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
    /// <remarks>
    /// The runs (each command file × <c>DifferentialRuns.Cases(48)</c> seeds) are spread over <see cref="AuditShards"/>
    /// classes (<c>Audit0</c> and on), each taking every <c>AuditShards</c>-th run, that run in parallel (Shards.cs).
    /// Each shard reports and asserts its own runs.
    /// </remarks>
    public abstract class AuditShard(ITestOutputHelper output)
    {
        [ScenarioFact]
        public Task Random_runs_of_the_commands_never_read_a_page_a_screen_drawn_by_hand_left_stale() =>
            AuditAsync(GetType().GetCustomAttribute<ShardAttribute>()!.Index, output);
    }

    [Shard(0)] public sealed class Audit0(ITestOutputHelper output) : AuditShard(output);
    [Shard(1)] public sealed class Audit1(ITestOutputHelper output) : AuditShard(output);
    [Shard(2)] public sealed class Audit2(ITestOutputHelper output) : AuditShard(output);
    [Shard(3)] public sealed class Audit3(ITestOutputHelper output) : AuditShard(output);
    [Shard(4)] public sealed class Audit4(ITestOutputHelper output) : AuditShard(output);
    [Shard(5)] public sealed class Audit5(ITestOutputHelper output) : AuditShard(output);
    [Shard(6)] public sealed class Audit6(ITestOutputHelper output) : AuditShard(output);
    [Shard(7)] public sealed class Audit7(ITestOutputHelper output) : AuditShard(output);
    [Shard(8)] public sealed class Audit8(ITestOutputHelper output) : AuditShard(output);
    [Shard(9)] public sealed class Audit9(ITestOutputHelper output) : AuditShard(output);
    [Shard(10)] public sealed class Audit10(ITestOutputHelper output) : AuditShard(output);
    [Shard(11)] public sealed class Audit11(ITestOutputHelper output) : AuditShard(output);
    [Shard(12)] public sealed class Audit12(ITestOutputHelper output) : AuditShard(output);
    [Shard(13)] public sealed class Audit13(ITestOutputHelper output) : AuditShard(output);
    [Shard(14)] public sealed class Audit14(ITestOutputHelper output) : AuditShard(output);
    [Shard(15)] public sealed class Audit15(ITestOutputHelper output) : AuditShard(output);
    [Shard(16)] public sealed class Audit16(ITestOutputHelper output) : AuditShard(output);
    [Shard(17)] public sealed class Audit17(ITestOutputHelper output) : AuditShard(output);
    [Shard(18)] public sealed class Audit18(ITestOutputHelper output) : AuditShard(output);
    [Shard(19)] public sealed class Audit19(ITestOutputHelper output) : AuditShard(output);
    [Shard(20)] public sealed class Audit20(ITestOutputHelper output) : AuditShard(output);
    [Shard(21)] public sealed class Audit21(ITestOutputHelper output) : AuditShard(output);
    [Shard(22)] public sealed class Audit22(ITestOutputHelper output) : AuditShard(output);
    [Shard(23)] public sealed class Audit23(ITestOutputHelper output) : AuditShard(output);

    private static async Task AuditAsync(int shard, ITestOutputHelper output)
    {
        int cases = DifferentialRuns.Cases(48);
        var findings = new List<PageAudit.Finding>();
        var ends = new Dictionary<string, int>(StringComparer.Ordinal);
        var reopened = new Dictionary<string, int>(StringComparer.Ordinal);
        var failures = new List<string>();
        var runs = AuditedCommands.SelectMany(c => Enumerable.Range(1, cases).Select(seed => (c.File, c.Shape, Seed: seed)))
            .Where((_, i) => i % AuditShards == shard);
        foreach (var (file, shape, seed) in runs)
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

    private const int AuditShards = 24;

    // Shared by the shards on their threads: the library's cache is concurrent and its images are never written.
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
