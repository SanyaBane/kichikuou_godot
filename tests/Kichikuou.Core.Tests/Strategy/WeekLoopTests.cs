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
    /// <summary>A week over a fresh state: week 8 before the loop adds one, 50 gold, music on; the thieves' by default.</summary>
    private sealed class WeekRig
    {
        public WeekRig(int stage = 0, params int[] draws)
        {
            Random = new ScriptedRandom(draws);
            Script = new OriginalScriptContext(State, Random, View, Input, Story);
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
        public ScriptedRandom Random { get; }
        public OriginalScriptContext Script { get; }

        public List<string> Files => Story.Calls.Select(c => c.File).ToList();

        public Task RunWeekAsync() => new WeekLoop(Script, new Literals()).RunWeekAsync();
    }

    /// <summary>The army names (<c>MS 10, "…"</c>) as "file:line", standing in for the player's files.</summary>
    private sealed class Literals : Kichikuou.Core.Setup.IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) => new SetStringStatement(at, 10, at.ToString());
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

    /// <summary>A Leazas week of stage 1: Leazas owns provinces 1 and 3, the rebels 2; unit 1 can be refilled.</summary>
    private static WeekRig LeazasRig(params int[] draws)
    {
        var rig = new WeekRig(stage: 1, draws);
        rig.State.Prov_Count = 3;
        rig.State.Provs[1].Owner = 1;
        rig.State.Provs[1].Economy = 30;
        rig.State.Provs[2].Owner = 2;
        rig.State.Provs[2].Economy = 99;
        rig.State.Provs[3].Owner = 1;
        rig.State.Provs[3].Economy = 12;
        rig.State.Provs[6].MapX = 700;
        rig.State.Provs[6].MapY = 200;
        var rance = rig.State.Units[1];
        rance.Status = 2;
        rance.HP = 39;
        rance.MaxHP = 40;
        rance.Troops = 100;
        rance.MaxTroops = 150;
        rance.Recovery = 10;
        return rig;
    }

    [Fact]
    public async Task The_Leazas_week_collects_the_income_refills_the_units_and_pays_the_AL_Church()
    {
        var rig = LeazasRig(7);                         // Y 4, 20 for unit 1: 7 + 10 troops
        Sys3Int fundsAtCommand = Sys3Int.Zero;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "COMMAND.ADV")
                fundsAtCommand = rig.State.History[rig.State.Hist_Cur].Funds;
        };

        await rig.RunWeekAsync();

        Assert.Equal(["ﾛｰﾄﾞｾｰﾌﾞ.ADV", "COMMAND.ADV", "TCﾘｰｻﾞｽ.ADV"], rig.Files);
        Assert.Equal([20], rig.Random.Draws);
        Is(92, fundsAtCommand);                         // :171 after the income, before the expenses
        Is(50 + 42 - 65, rig.State.Gold);               // N_ALChurch_Stage 0 < 19: the donation
        Is(65, rig.State.History[0].SundryExp);
        var rance = rig.State.Units[1];
        Is(1, rance.Status);
        Is(40, rance.HP);
        Is(117, rance.Troops);
        Is(28, rig.State.Unit_Cur);                     // :224, after :185's 41

        var events = rig.View.Events;
        Assert.Equal(new ScriptRef("MAIN.ADV", 103), events.OfType<ValueMessageEvent>().First().Text);
        var report = events.OfType<ValueMessageEvent>().Single(v => v.Text.Line == 173);
        Assert.Equal([42, 17], report.Values.Select(v => v.Value));   // provinces 1 and 3; 7 + 10 troops (the map ops reuse Tmp0021-0026 later)
        Assert.Equal([98, 188, 201, 313, 1041], events.OfType<MessageEvent>().Select(m => m.Text.Line));
    }

    [Fact]
    public async Task The_rebel_phase_heals_commanders_1_to_7_fully_before_TCLeazas()
    {
        var rig = LeazasRig(1);
        var cmdrs = rig.State.Cmdrs;
        for (int c = 1; c <= 8; c++)
        {
            cmdrs[c].Status = 2;
            cmdrs[c].HP = 1;
            cmdrs[c].MaxHP = 30;
            cmdrs[c].Troops = 100;
            cmdrs[c].MaxTroops = 200;
            cmdrs[c].Recovery = 20;
        }
        cmdrs[3].Status = 0;                            // dead: left alone
        cmdrs[4].Troops = 190;                          // capped
        Sys3Int cursorAtTurn = Sys3Int.Zero;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "TCﾘｰｻﾞｽ.ADV")
                cursorAtTurn = rig.State.Cmdr_Cur;
        };

        await rig.RunWeekAsync();

        Is(1, cmdrs[1].Status);
        Is(30, cmdrs[1].HP);
        Is(160, cmdrs[1].Troops);
        Is(0, cmdrs[3].Status);
        Is(1, cmdrs[3].HP);
        Is(200, cmdrs[4].Troops);
        Is(2, cmdrs[8].Status);                         // only 1-7
        Is(8, cursorAtTurn);
        Is(6, rig.State.Prov_Cur);
        Is(700, rig.State.MapTargetX);
        Is(25, rig.State.CurrentBgm);
    }

    [Fact]
    public async Task Sill_is_kidnapped_by_the_weeks_and_a_scene_can_open_the_next_line()
    {
        var rig = LeazasRig(1);
        rig.State.Week = 21;                            // week 22 at the end
        rig.Story.OnCall = (file, entry) =>
        {
            if (file == "Gｼｨﾙ.ADV")
                rig.State.F_SillKidnap_Stage = (Sys3Int)(entry - 20);   // each scene sets the next stage first
        };

        await rig.RunWeekAsync();

        Assert.Equal(new[] { ("Gｼｨﾙ.ADV", 21), ("Gｼｨﾙ.ADV", 22) }, rig.Story.Calls.Where(c => c.File == "Gｼｨﾙ.ADV").ToArray());
    }

    [Fact]
    public async Task The_Camilla_unit_attacks_and_a_stale_roll_plays_Camilla_s_scene()
    {
        var rig = LeazasRig(1, 5, 1, 16, 2);            // unit 1's refill, then 93: province 5, attack; 92: province 16, no attack
        rig.State.StoryStage = 2;
        rig.State.N_Camilla_Stage = 3;
        rig.State.N_Rei_Stage = 1;
        rig.State.Provs[5].Owner = 1;
        rig.State.Provs[16].Owner = 1;
        var cmdrs = rig.State.Cmdrs;
        cmdrs[93].Status = 1;
        cmdrs[92].Status = 1;
        cmdrs[92].HP = 5;
        cmdrs[92].MaxHP = 10;
        var parties = new List<int>();
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "FIGT_B.ADV")
                parties.Add(rig.State.Bt_EnemyParty1.Value);
        };

        await rig.RunWeekAsync();

        Assert.Equal([93], parties);
        Is(5, rig.State.Bt_EnemyRetreatMode);           // 92's mode last
        Is(7, cmdrs[92].HP);                            // L_01831
        // QUIRK: STR-02 — 91 is dead, so :798-807 test 92's roll of 2: Camilla's first scene.
        Assert.Contains(("Gｶﾐｰﾗ.ADV", 1), rig.Story.Calls);
        Assert.DoesNotContain(("Gｶﾐｰﾗ.ADV", 8), rig.Story.Calls);
        Assert.Equal("MAIN.ADV:664", rig.State.Strings[10]);   // MS 10, "Camilla Unit", from the player's files
        Is(8, rig.State.Ev_PhaseNation);
    }

    [Fact]
    public async Task The_demons_first_attack_plays_its_music_with_the_music_switched_off()
    {
        var rig = LeazasRig(1);
        rig.State.StoryStage = 2;
        rig.State.N_Camilla_Stage = 1;
        rig.State.BgmEnabled = 0;

        await rig.RunWeekAsync();

        // QUIRK: STR-24 — SS 16 and SS 0 around Iﾏｼﾞﾝ 1 without testing BgmEnabled.
        Assert.Equal([16, 0], rig.View.Events.OfType<MusicEvent>().Select(m => m.Track.Value));
        Assert.Contains(("Iﾏｼﾞﾝ.ADV", 1), rig.Story.Calls);
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
        var saves = new StoppingSaves();
        var script = new OriginalScriptContext(state, new ScriptedRandom(), new RecordingView(), new ScriptedSceneInput(), rules)
        {
            Saves = saves,
        };
        rules.Bind(script);

        Assert.Contains("MAIN.ADV", StrategyRules.PortedFiles);
        await Assert.ThrowsAsync<StopException>(() => script.CallFileAsync("MAIN.ADV"));
        Is(31, state.Week);
        Assert.Equal(new ScriptRef("MAIN.ADV", 83), saves.Resume);         // the Leazas week's autosave, through the save screen's port
        Assert.Empty(story.Calls);
    }

    private sealed class StopException : Exception;

    private sealed class NoScenario : Kichikuou.Core.Setup.IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) => throw new InvalidOperationException(at.ToString());
    }

    /// <summary>Slots that stop the run at the first save.</summary>
    private sealed class StoppingSaves : ISaveStore
    {
        public ScriptRef? Resume { get; private set; }

        public SaveData? Read(int slot) => null;

        public bool Write(int slot, SaveData data)
        {
            Resume = data.Resume;
            throw new StopException();
        }

        public DateTime? Timestamp(int slot) => null;
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
    public async Task A_load_goes_on_after_either_autosave()
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

        rig.State.Gold = 100;
        await Assert.ThrowsAsync<OperationCanceledException>(() => new WeekLoop(rig.Script).ResumeAsync(new ScriptRef("MAIN.ADV", 83)));

        Assert.Equal(["COMMAND.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV", "COMMAND.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV"], rig.Files);   // the Leazas week from :84
        Is(35, rig.State.Gold);                                     // its AL Church donation
        Is(10, rig.State.Week);
        await Assert.ThrowsAsync<ArgumentException>(() => new WeekLoop(rig.Script).ResumeAsync(new ScriptRef("MAIN.ADV", 21)));
    }
}
