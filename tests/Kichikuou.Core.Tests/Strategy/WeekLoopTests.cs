using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// <c>MAIN.ADV</c> (<see cref="WeekLoop"/>) as the prologue reaches it: the top of the week with
/// the history shift (STR-01), the thieves' week, the end of the prologue, the loop, and the
/// Leazas week failing as not ported. The file is also compared with the decompile on random
/// states (Content tests, <c>WeekLoopDifferentialTests</c>).
/// </summary>
public class WeekLoopTests
{
    /// <summary>A thieves' week over a fresh state: week 8 before the loop adds one, 50 gold, music on.</summary>
    private sealed class WeekRig
    {
        public WeekRig(int stage = 0)
        {
            Script = new ScriptContext(State, new ScriptedRandom(), View, Input, Story);
            State.StoryStage = (Sys3Int)stage;
            State.Week = 8;
            State.Gold = 50;
            State.BgmEnabled = 1;
            State.FadeSpeed = 3;
            State.Dgns[1].MapX = 900;
            State.Dgns[1].MapY = 300;
        }

        public GameState State { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public RecordingView View { get; } = new();
        public ScriptedStory Story { get; } = new();
        public ScriptContext Script { get; }

        public List<string> Files => Story.Calls.Select(c => c.File).ToList();

        public Task RunWeekAsync() => new WeekLoop(Script).RunWeekAsync();
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, actual.Value);

    [Fact]
    public async Task The_week_advances_and_the_history_shift_smears_last_week_over_every_slot()
    {
        var rig = new WeekRig();
        var history = rig.State.History;
        for (int i = 0; i < 300; i++)
        {
            history[i].Funds = (Sys3Int)(100 + i);
            history[i].SundryExp = (Sys3Int)(200 + i);
            history[i].MilitaryExp = (Sys3Int)(300 + i);
            history[i].Casualties = (Sys3Int)(400 + i);
            history[i].Harem_SexCount = (Sys3Int)i;
        }
        rig.State.Hist_Cur = 7;                         // the harem left it on a girl (CMD-14)
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "ﾛｰﾄﾞｾｰﾌﾞ.ADV")
            {
                // QUIRK: STR-01 — every slot from 1 on holds last week's (slot 0's) value.
                for (int i = 1; i < 300; i++)
                {
                    Is(100, history[i].Funds);
                    Is(200, history[i].SundryExp);
                    Is(300, history[i].MilitaryExp);
                    Is(400, history[i].Casualties);
                }
                Is(0, history[0].Funds);
                Is(0, history[0].SundryExp);
                Is(0, history[0].MilitaryExp);
                Is(0, history[0].Casualties);
            }
        };

        await rig.RunWeekAsync();

        Is(9, rig.State.Week);
        Is(0, rig.State.Hist_Cur);
        Is(1, rig.State.Hist_PrevCur);
        Is(7, history[7].Harem_SexCount);               // only the four weekly fields move
        Is(50, history[0].Funds);                       // :68 Hist_Funds := Gold
    }

    [Fact]
    public async Task The_thieves_week_autosaves_shows_the_banner_and_runs_COMMAND()
    {
        var rig = new WeekRig();
        Sys3Int retValAtSave = Sys3Int.Zero, fundsAtCommand = Sys3Int.Zero;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "ﾛｰﾄﾞｾｰﾌﾞ.ADV")
                retValAtSave = rig.State.RetVal;
            if (file == "COMMAND.ADV")
                fundsAtCommand = rig.State.History[rig.State.Hist_Cur].Funds;
        };

        await rig.RunWeekAsync();

        Assert.Equal(["ﾛｰﾄﾞｾｰﾌﾞ.ADV", "COMMAND.ADV"], rig.Files);
        Is(3, retValAtSave);                            // L_01875: the autosave mode
        Is(50, fundsAtCommand);
        // Week 9 = LP 3, month 3, week 1 (L_01881).
        Is(3, rig.State.DateYear);
        Is(3, rig.State.DateMonth);
        Is(1, rig.State.DateWeek);
        Is(1, rig.State.CurrentBgm);
        Is(1, rig.State.Dgn_Cur);
        Is(900, rig.State.MapTargetX);
        Is(300, rig.State.MapTargetY);

        var events = rig.View.Events;
        Assert.Equal([0, 1], events.OfType<MusicEvent>().Select(m => m.Track.Value));
        Assert.Equal([3, 113, 1, 0, 113, 1, 0], events.OfType<WindowEvent>().Select(w => w.Op.Value));
        Assert.Equal([0, 1, 10], events.OfType<MapEvent>().Select(m => m.Mode.Value));
        var banner = Assert.Single(events.OfType<ValueMessageEvent>());
        Assert.Equal(new ScriptRef("MAIN.ADV", 40), banner.Text);
        Assert.Equal([3, 3, 1], banner.Values.Select(v => v.Value));
        Assert.Equal([35, 77], events.OfType<MessageEvent>().Select(m => m.Text.Line));
        Assert.Equal(["ZM 20", "ZM 24", "PF1 3", "PF0 3"],
            events.OfType<EngineEvent>().Select(e => $"{e.Command} {string.Join(",", e.Args)}"));
    }

    [Fact]
    public async Task Units_1_to_4_still_standing_are_refreshed()
    {
        var rig = new WeekRig();
        var units = rig.State.Units;
        for (int u = 1; u <= 5; u++)
        {
            units[u].Status = 2;
            units[u].HP = 3;
            units[u].MaxHP = 40;
            units[u].Troops = 10;
            units[u].MaxTroops = 20;
        }
        units[2].Status = 0;                            // fallen: left alone (README "Open [I]" #4)
        units[3].Troops = 19;                           // capped at the maximum
        units[4].Status = 1;

        await rig.RunWeekAsync();

        Is(1, units[1].Status);
        Is(40, units[1].HP);
        Is(12, units[1].Troops);
        Is(0, units[2].Status);
        Is(3, units[2].HP);
        Is(10, units[2].Troops);
        Is(20, units[3].Troops);
        Is(1, units[4].Status);
        Is(40, units[4].HP);
        Is(2, units[5].Status);                         // only units 1-4
        Is(3, units[5].HP);
        Is(5, rig.State.Unit_Cur);
    }

    [Fact]
    public async Task Without_music_no_track_is_played_or_set()
    {
        var rig = new WeekRig();
        rig.State.BgmEnabled = 0;
        rig.State.CurrentBgm = 7;

        await rig.RunWeekAsync();

        Assert.Empty(rig.View.Events.OfType<MusicEvent>());
        Is(7, rig.State.CurrentBgm);
    }

    [Fact]
    public async Task The_lost_duel_ends_the_prologue_with_the_rebellion_scene()
    {
        var rig = new WeekRig();
        rig.State.Ev_Entry = 4;
        int stageAtScene = -1;
        rig.Story.OnCall = (file, entry) =>
        {
            if (file == "COMMAND.ADV")
                rig.State.StoryStage = 99;              // Iﾄｳｿﾞｸ.ADV:1841, through Turn END or Pillage
            if (file == "Iﾊﾝﾗﾝ.ADV")
            {
                stageAtScene = rig.State.StoryStage.Value;
                Assert.Equal(4, entry);                 // %#Iﾊﾝﾗﾝ.ADV with Ev_Entry as left
            }
        };

        await rig.RunWeekAsync();

        Assert.Equal(["ﾛｰﾄﾞｾｰﾌﾞ.ADV", "COMMAND.ADV", "Iﾊﾝﾗﾝ.ADV"], rig.Files);
        Assert.Equal(1, stageAtScene);
        Is(1, rig.State.StoryStage);
    }

    [Fact]
    public async Task The_Leazas_week_is_not_ported_yet()
    {
        var rig = new WeekRig(stage: 1);

        var e = await Assert.ThrowsAsync<NotSupportedException>(rig.RunWeekAsync);

        Assert.Contains("MAIN.ADV:83", e.Message);
        Is(9, rig.State.Week);                          // :1-18 ran
        Assert.Empty(rig.Files);
        Assert.Equal([0], rig.View.Events.OfType<MusicEvent>().Select(m => m.Track.Value));
    }

    [Fact]
    public async Task The_loop_runs_week_after_week()
    {
        var rig = new WeekRig();
        int saves = 0;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "ﾛｰﾄﾞｾｰﾌﾞ.ADV" && ++saves == 3)
                throw new StopException();
        };

        await Assert.ThrowsAsync<StopException>(new WeekLoop(rig.Script).RunAsync);

        Is(11, rig.State.Week);
        Assert.Equal(["ﾛｰﾄﾞｾｰﾌﾞ.ADV", "COMMAND.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV", "COMMAND.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV"], rig.Files);
    }

    [Fact]
    public async Task The_router_runs_MAIN_as_the_week_loop()
    {
        var state = new GameState();
        state.StoryStage = 2;
        state.Week = 30;
        var story = new ScriptedStory();
        var rules = new StrategyRules(new NoScenario(), story);
        var script = new ScriptContext(state, new ScriptedRandom(), new RecordingView(), new ScriptedSceneInput(), rules);
        rules.Bind(script);

        Assert.Contains("MAIN.ADV", StrategyRules.PortedFiles);
        await Assert.ThrowsAsync<NotSupportedException>(() => script.CallFileAsync("MAIN.ADV"));
        Is(31, state.Week);
        Assert.Empty(story.Calls);
    }

    private sealed class StopException : Exception;

    private sealed class NoScenario : Kichikuou.Core.Setup.IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) => throw new InvalidOperationException(at.ToString());
    }

    [Fact]
    public async Task The_autosave_names_its_call_for_the_slot()
    {
        var rig = new WeekRig();
        ScriptRef? during = null;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "ﾛｰﾄﾞｾｰﾌﾞ.ADV")
                during = rig.Script.SaveResume;
            if (file == "COMMAND.ADV")
                throw new OperationCanceledException();
        };

        await Assert.ThrowsAsync<OperationCanceledException>(rig.RunWeekAsync);

        Assert.Equal(new ScriptRef("MAIN.ADV", 20), during);
        Assert.Null(rig.Script.SaveResume);
    }

    [Fact]
    public async Task A_load_goes_on_after_the_thieves_autosave_and_fails_for_the_Leazas_one()
    {
        var rig = new WeekRig();
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "ﾛｰﾄﾞｾｰﾌﾞ.ADV")
                throw new OperationCanceledException();             // the next week's autosave
        };

        await Assert.ThrowsAsync<OperationCanceledException>(() => new WeekLoop(rig.Script).ResumeAsync(new ScriptRef("MAIN.ADV", 20)));

        Assert.Equal(["COMMAND.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV"], rig.Files);   // no autosave first: the week goes on from :21
        Is(9, rig.State.Week);                                      // the next week began
        var leazas = await Assert.ThrowsAsync<NotSupportedException>(() => new WeekLoop(rig.Script).ResumeAsync(new ScriptRef("MAIN.ADV", 83)));
        Assert.StartsWith("MAIN.ADV:83", leazas.Message);
        await Assert.ThrowsAsync<ArgumentException>(() => new WeekLoop(rig.Script).ResumeAsync(new ScriptRef("MAIN.ADV", 21)));
    }
}
