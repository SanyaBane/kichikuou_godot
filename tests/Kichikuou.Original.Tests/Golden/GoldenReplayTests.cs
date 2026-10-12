using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Tests.Golden;

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
        Assert.Equal(capture.Records.Count, report.ConsumedRecords);
        Assert.Contains(capture.Records, r => r is WaitRecord);
        Assert.Contains(capture.Records, r => r is PickRecord);
    }

    /// <summary>
    /// A <c>COM</c> capture (group <c>com</c>) replays through <see cref="WeeklyEvents"/>:
    /// the caravan's roll, then one tier-1 scene as a hook whose exit dump moves a flag.
    /// </summary>
    [Fact]
    public async Task A_COM_capture_replays_through_the_weekly_events()
    {
        var recorder = new CaptureRecorder(777)
        {
            OnHook = (file, state, _) =>
            {
                if (file == "Gｹﾝﾀﾛｳ.ADV")
                    state.N_Miki_Stage = 3;
            },
        };
        var s = recorder.State;
        s.StoryStage = 2;
        s.Week = 10;
        s.N_ALChurch_Stage = 50;
        s.F_Cafe_State = 3;
        s.LadyCaravan_Prov = 40;
        s.F_Kentarou_State = 2;
        var capture = Parse(await recorder.RecordAsync("COM.ADV", b => new WeeklyEvents(b.Script).RunAsync()));

        var report = await GoldenReplay.RunAsync(capture);

        Assert.True(report.Passed, report.Describe());
        Assert.Equal(capture.Records.Count, report.ConsumedRecords);
        Assert.Single(capture.Records.OfType<HookRecord>());
        Assert.Equal(2, report.ConsumedRecords);   // the roll and the hook
    }

    /// <summary>
    /// A week capture (label root <c>MAIN.ADV@20</c>, group <c>week</c>) replays through
    /// <see cref="WeekLoop.RunWeekAsync"/>: the autosave, <c>COMMAND</c> ending the prologue and
    /// <c>Iﾊﾝﾗﾝ</c> as hooks, the camera's pauses and the key waits in between.
    /// </summary>
    [Fact]
    public async Task A_week_capture_replays_through_the_week_loop()
    {
        var recorder = new CaptureRecorder(4242)
        {
            OnHook = (file, state, _) =>
            {
                if (file == "COMMAND.ADV")
                {
                    state.Gold = 61;
                    state.StoryStage = 99;
                }
                if (file == "Iﾊﾝﾗﾝ.ADV")
                    state.Week = state.Week + 1;
            },
        };
        var s = recorder.State;
        s.Week = 22;
        s.Gold = 50;
        s.BgmEnabled = 1;
        s.Dgns[1].MapX = 900;
        s.Dgns[1].MapY = 300;
        for (int u = 1; u <= 4; u++)
        {
            s.Units[u].Status = 2;
            s.Units[u].MaxHP = 30;
            s.Units[u].MaxTroops = 20;
            s.Units[u].Troops = 5;
        }
        var capture = Parse(await recorder.RecordAsync("MAIN.ADV", b => new WeekLoop(b.Script).RunWeekAsync(), "MAIN.ADV@20"));

        var report = await GoldenReplay.RunAsync(capture);

        Assert.True(GoldenReplay.CanReplay(capture.Header));
        Assert.True(report.Passed, report.Describe());
        Assert.Equal(capture.Records.Count, report.ConsumedRecords);
        Assert.Equal(["ﾛｰﾄﾞｾｰﾌﾞ.ADV", "COMMAND.ADV", "Iﾊﾝﾗﾝ.ADV"], capture.Records.OfType<HookRecord>().Select(h => h.File));
        Assert.Contains(capture.Records, r => r is KeyRecord);
    }

    [Fact]
    public async Task A_path_the_port_does_not_reach_ends_the_comparison_there()
    {
        var capture = Parse(await RecordPlainsAttackAsync());

        var report = await GoldenReplay.RunAsync(capture,
            entryPoint: _ => throw new NotSupportedException("FIGT.ADV:1: not ported yet."));

        Assert.True(report.Passed, report.Describe());
        Assert.Equal("FIGT.ADV:1: not ported yet.", report.Unported);
        Assert.Equal(0, report.ConsumedRecords);
        Assert.Empty(report.Checkpoints);
        Assert.Contains("as far as the port reaches", report.Describe());
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
    public async Task Mouse_and_cursor_records_are_passive_in_a_battle()
    {
        var capture = Parse(await RecordPlainsAttackAsync());
        var records = capture.Records.ToList();
        int i = records.FindIndex(r => r is RndRecord);
        records.Insert(i, new MouseRecord(records[i].Line, new GoldenLocation("FIGT.ADV", 0x13c0), 0, 100, 50, 3));
        records.Add(new CursorRecord(records[^1].Line + 1, new GoldenLocation("WAO_MAP.ADV", 0x9a), 6, 1));

        var report = await GoldenReplay.RunAsync(WithRecords(capture, records));

        Assert.True(report.Passed, report.Describe());
    }

    [Fact]
    public async Task A_root_without_an_entry_point_is_a_format_error()
    {
        var c = Parse(await RecordPlainsAttackAsync());
        var capture = new GoldenCapture
        {
            Source = c.Source, Seed = c.Seed, Root = "Tﾍﾙﾏﾝ.ADV", RootCall = c.RootCall, Enter = c.Enter,
            Records = [], Exit = c.Exit, JumpTarget = null, Group = "nations",
        };

        var e = await Assert.ThrowsAsync<GoldenFormatException>(() => GoldenReplay.RunAsync(capture));
        Assert.Contains("No entry point for root Tﾍﾙﾏﾝ.ADV", e.Message);
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
