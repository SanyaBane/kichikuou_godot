using Kichikuou.Core.Golden;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Golden;

/// <summary>
/// The replay machinery on captures that the core writes of itself (<see cref="CaptureRecorder"/>):
/// an unchanged capture must pass, and every kind of tampering must be reported where it is.
/// </summary>
public class GoldenReplayTests
{
    private const int Unit = 44;
    private const int Cmdr = 5;

    /// <summary>
    /// A plains attack on province 5 while Ex is at stage 1, so FIGT_A calls the story hook
    /// <c>Gｴｸｽ.ADV</c> (entry 2) before the battle; the simulated scene changes a flag and draws.
    /// </summary>
    private static async Task<string> RecordPlainsAttackAsync(uint seed = 12345)
    {
        var recorder = new CaptureRecorder(seed)
        {
            OnHook = (_, state, random) =>
            {
                state.StoryStage = 2;
                random.Next(10);
            },
        };
        var s = recorder.State;
        s.StoryStage = 1;
        s.Units[1].Status = 1;
        s.Prov_Cur = 5;
        s.Bt_TargetProv = 5;
        s.F_Ex_State = 1;
        s.Provs[5].Owner = 4;
        s.Bt_BackgroundCg = 900;
        s.Bt_PlayerParty1 = Unit;
        s.Bt_EnemyParty1 = Cmdr;
        s.Strings[8] = "Leazas Castle";

        var u = s.Units[Unit];
        u.Troops = 300;
        u.MaxTroops = 300;
        u.HP = 50;
        u.MaxHP = 50;
        u.Atk = 30;
        u.Def = 30;
        u.Attacks = 5;
        u.Level = 5;
        u.TroopAtk = 6;
        u.TroopDef = 4;
        u.Status = 2;

        var c = s.Cmdrs[Cmdr];
        c.Troops = 200;
        c.MaxTroops = 200;
        c.HP = 20;
        c.MaxHP = 20;
        c.Atk = 10;
        c.Def = 10;
        c.Attacks = 5;
        c.TroopAtk = 3;
        c.TroopDef = 3;
        c.Status = 1;

        return await recorder.RecordAsync("FIGT_A.ADV", b => b.AttackProvinceAsync());
    }

    private static GoldenCapture Parse(string text) => GoldenCapture.Parse(new StringReader(text), "self-recorded");

    private static int SlotOf(string table, string field, int record) =>
        VariableCatalog.Bindings.First(b => b.Table == table && b.Field == field).Offset + record;

    [Fact]
    public async Task An_unchanged_capture_matches()
    {
        var capture = Parse(await RecordPlainsAttackAsync());

        var report = await GoldenReplay.RunAsync(capture);

        Assert.True(report.Passed, report.Describe());
        Assert.Equal(["hook-enter Gｴｸｽ.ADV at FIGT_A.ADV@40 (line " + capture.Records.OfType<HookRecord>().Single().Line + ")", "exit"],
            report.Checkpoints.Select(c => c.Name));
        Assert.Equal(capture.Records.Count(r => r is not WaitRecord), report.ConsumedRecords);
        Assert.Contains(capture.Records, r => r is PickRecord);
    }

    [Fact]
    public async Task A_different_final_value_is_reported_by_name()
    {
        var capture = Parse(await RecordPlainsAttackAsync());
        capture.Exit.Pages[1][SlotOf("Units", "HP", Unit)] += 1;

        var report = await GoldenReplay.RunAsync(capture);

        Assert.False(report.Passed);
        var failure = Assert.Single(report.Checkpoints.Single(c => c.Name == "exit").Failures);
        Assert.StartsWith($"Units[{Unit}].HP", failure.Location);
    }

    [Fact]
    public async Task A_different_ui_value_is_listed_but_ignored()
    {
        var capture = Parse(await RecordPlainsAttackAsync());
        int mouseX = VariableCatalog.Variables.ToList().FindIndex(v => v.Name == "Ui_MouseX");
        capture.Exit.Vars[mouseX] += 1;

        var report = await GoldenReplay.RunAsync(capture);

        Assert.True(report.Passed, report.Describe());
        var exit = report.Checkpoints.Single(c => c.Name == "exit");
        Assert.True(Assert.Single(exit.Differences).Ignored);
    }

    [Fact]
    public async Task A_difference_before_a_hook_is_reported_at_the_hook()
    {
        var capture = Parse(await RecordPlainsAttackAsync());
        var hook = capture.Records.OfType<HookRecord>().Single();
        hook.Enter.Vars[(int)VariableCatalog.Variables.ToList().FindIndex(v => v.Name == "StoryStage")] = 9;

        var report = await GoldenReplay.RunAsync(capture);

        Assert.False(report.Passed);
        Assert.Contains("StoryStage", Assert.Single(report.Checkpoints[0].Failures).Location);
        Assert.Empty(report.Checkpoints.Single(c => c.Name == "exit").Failures);
    }

    [Fact]
    public async Task A_draw_the_generator_does_not_produce_stops_the_replay()
    {
        var capture = Parse(await RecordPlainsAttackAsync());
        var records = capture.Records.ToList();
        int i = records.FindIndex(r => r is RndRecord { N: > 1 });
        var draw = (RndRecord)records[i];
        records[i] = draw with { Result = draw.Result % draw.N + 1 };

        var report = await GoldenReplay.RunAsync(WithRecords(capture, records));

        Assert.False(report.Passed);
        Assert.Contains("out of step", report.Divergence);
    }

    [Fact]
    public async Task An_event_the_core_never_asks_for_stops_the_replay()
    {
        var capture = Parse(await RecordPlainsAttackAsync());
        var records = capture.Records.ToList();
        var last = records[^1];
        records.Add(new KeyRecord(last.Line + 1, new GoldenLocation("FIGT_A.ADV", 0x200), 0));

        var report = await GoldenReplay.RunAsync(WithRecords(capture, records));

        Assert.False(report.Passed);
        Assert.Contains("the original went on with IK", report.Divergence);
    }

    [Fact]
    public async Task A_key_where_the_core_draws_stops_the_replay()
    {
        var capture = Parse(await RecordPlainsAttackAsync());
        var records = capture.Records.ToList();
        int i = records.FindIndex(r => r is RndRecord);
        records.Insert(i, new KeyRecord(records[i].Line, records[i].Where, 13));

        var report = await GoldenReplay.RunAsync(WithRecords(capture, records));

        Assert.Contains("the core asked for Y 4", report.Divergence);
        Assert.Contains("next event is IK → 13", report.Divergence);
    }

    [Fact]
    public async Task Capture_text_round_trips_through_the_parser()
    {
        string text = await RecordPlainsAttackAsync();
        var capture = Parse(text);

        Assert.Equal(12345u, capture.Seed);
        Assert.Equal("FIGT_A.ADV", capture.Root);
        Assert.Equal(new GoldenLocation("Cﾃｷﾄｼ.ADV", 0x1a5), capture.RootCall);
        Assert.Null(capture.JumpTarget);
        Assert.Equal("Leazas Castle", capture.Enter.Strings[8]);
        var generator = capture.Enter.CreateGenerator();
        Assert.Equal(new Mt19937Legacy(12345).NextUInt32(), generator.NextUInt32());
    }

    private static GoldenCapture WithRecords(GoldenCapture c, List<GoldenRecord> records) => new()
    {
        Source = c.Source,
        Seed = c.Seed,
        Root = c.Root,
        RootCall = c.RootCall,
        Enter = c.Enter,
        Records = records,
        Exit = c.Exit,
        JumpTarget = c.JumpTarget,
    };
}
