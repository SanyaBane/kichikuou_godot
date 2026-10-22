using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The marks our Harem and Subordinates screens carry (owner's decision 30, docs/ui/talk-marks.md §6, §8 step 8): the
/// models on hand-made states — the girls' marks, the ledger's and Talk's marks hidden while the week's talk is spent —
/// and <see cref="PickerMarks"/> on the booted game at stage 1 over the player's scenes (skipped without the decompile).
/// </summary>
public class PickerMarksTests(ITestOutputHelper output)
{
    private static readonly string[] Names = ["Sill", "Lia", "Maris", "Suzume", "Wendy", "Kanami"];

    private static readonly Dictionary<int, SceneMark> Marks = new()
    {
        [1] = SceneMark.New, [2] = SceneMark.Read, [11] = SceneMark.Maybe, [12] = SceneMark.New,
    };

    /// <summary>Stage 1, both orders and the talk left: Rance (unit 1) deployed, Sill (unit 2), Asuka (unit 11) and Haurein (unit 12) able to act.</summary>
    private static GameState World()
    {
        var s = new GameState { Unit_StrBase = 20, StoryStage = 1, Week = 18, Gold = 1141 };
        (s.Ui_CmdSubordAvail, s.CmdAvail_SubTroops, s.CmdAvail_SubTalk) = (1, 1, 1);
        foreach (var (n, status) in new[] { (1, 2), (2, 1), (11, 1), (12, 1) })
        {
            var u = s.Units[n];
            (u.Status, u.Troops, u.MaxTroops) = ((Sys3Int)status, (Sys3Int)100, (Sys3Int)100);
        }
        return s;
    }

    [Fact]
    public void The_girls_carry_their_visits_marks_and_the_panel_says_them_in_words()
    {
        var s = new GameState { StoryStage = 1 };
        s.HaremAvail_Lia = 1;
        s.HaremAvail_Maris = 1;
        s.HaremAvail_Kanami = 1;

        var girls = HaremRoster.Girls(s, Names, new Dictionary<int, SceneMark> { [2] = SceneMark.New, [6] = SceneMark.Maybe });

        Assert.Equal([SceneMark.New, null, SceneMark.Maybe], girls.Select(g => g.Mark));
        Assert.All(HaremRoster.Girls(s, Names), g => Assert.Null(g.Mark));
        Assert.Equal(["Something new", "Something may happen", "Nothing new", null],
            new SceneMark?[] { SceneMark.New, SceneMark.Maybe, SceneMark.Read, null }.Select(HaremRoster.VisitText));
        Assert.Equal(["NEW", "???", null, null],
            new SceneMark?[] { SceneMark.New, SceneMark.Maybe, SceneMark.Read, null }.Select(PickerMarks.Label));
    }

    [Fact]
    public void The_ledger_marks_the_units_able_to_act_and_Talk_carries_the_chosen_ones()
    {
        var s = World();

        var shown = SubordinatesScreen.TalkMarks(s, Marks);

        Assert.Equal([2, 11, 12], shown.Keys.Order());                  // Rance deployed: no bubble
        Assert.Equal(SceneMark.Maybe, shown[11]);
        Assert.Equal(SceneMark.New, SubordinatesScreen.Panel(s, 12, Marks).TalkMark);
        Assert.Equal(SceneMark.Read, SubordinatesScreen.Panel(s, 2, Marks).TalkMark);
        Assert.Null(SubordinatesScreen.Panel(s, 1, Marks).TalkMark);    // greyed: "deployed this week"
        Assert.Null(SubordinatesScreen.Panel(s, 12).TalkMark);
    }

    [Fact]
    public void No_marks_show_while_the_weeks_talk_is_spent_or_the_screen_is_view_only()
    {
        var s = World();
        s.CmdAvail_SubTalk = 0;
        Assert.False(SubordinatesScreen.TalkOpen(s));
        Assert.Empty(SubordinatesScreen.TalkMarks(s, Marks));
        Assert.Null(SubordinatesScreen.Panel(s, 12, Marks).TalkMark);
        Assert.False(SubordinatesScreen.Panel(s, 12, Marks).Talk.Enabled);

        s.CmdAvail_SubTalk = 1;
        s.Ui_CmdSubordAvail = 0;                                        // the view-only picker
        Assert.Empty(SubordinatesScreen.TalkMarks(s, Marks));
        Assert.Null(SubordinatesScreen.Panel(s, 12, Marks).TalkMark);

        s.Ui_CmdSubordAvail = 1;
        s.StoryStage = 0;                                               // the prologue: the pick is the talk
        s.CmdAvail_SubTalk = 0;
        Assert.Equal([2, 11, 12], SubordinatesScreen.TalkMarks(s, Marks).Keys.Order());
    }

    /// <summary>
    /// On the booted game: the harem's opening marks every girl on offer, the Subordinates screen's every unit able to act
    /// while the talk is left and none once it is spent (nothing is run then); the game's state is untouched; a switch of
    /// "New scenes first" reads the kept trees as a fresh run with it would; the read-ahead goes through every talk and visit.
    /// </summary>
    [Fact]
    public async Task The_pickers_openings_mark_what_they_offer_and_leave_the_state_alone()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;
        var s = game.State;
        s.StoryStage = 1;
        foreach (int unit in new[] { 1, 2, 12, 16 })
            s.Units[unit].Status = 1;
        s.Units[6].Status = 2;
        foreach (int girl in new[] { 2, 3, 6, 9 })
            s.Memory.Var(248 + girl) = 1;
        (s.Ui_CmdSubordAvail, s.CmdAvail_SubTroops, s.CmdAvail_SubTalk) = (1, 1, 1);
        var scenes = new SceneMarks(new ScenarioScripts(LocalData.ScenarioDirectory));
        var marks = new PickerMarks(scenes);
        ulong memory = s.Memory.Fingerprint();
        var extras = s.Extras.ToDictionary(e => e.Key, e => e.Value.ToArray());

        Assert.True(marks.OnPicker(new PickerScreenEvent(PickerKind.Harem, Sys3Int.Zero, true), s, newScenesFirst: true));
        output.WriteLine($"visits: {string.Join(", ", marks.Visits.Select(v => $"{v.Key} {v.Value}"))} ({marks.LastMilliseconds:F0} ms)");
        Assert.Equal([2, 3, 6, 9], marks.Visits.Keys.Order());
        Assert.Empty(marks.Failures);

        Assert.True(marks.OnPicker(new PickerScreenEvent(PickerKind.Subordinates, (Sys3Int)1, true), s, newScenesFirst: true));
        output.WriteLine($"talks: {string.Join(", ", marks.Talks.Select(t => $"{t.Key} {t.Value}"))} ({marks.LastMilliseconds:F0} ms)");
        Assert.Equal(SceneMarks.TalkingUnits(s), marks.Talks.Keys.Order());
        Assert.Contains(12, marks.Talks.Keys);
        Assert.DoesNotContain(6, marks.Talks.Keys);
        Assert.Equal(marks.Talks.Keys.Order(), SubordinatesScreen.TalkMarks(s, marks.Talks).Keys.Order());
        Assert.Equal(scenes.Talks(s, newScenesFirst: true), marks.Talks);

        marks.SetNewScenesFirst(false);
        Assert.Equal(scenes.Talks(s, newScenesFirst: false), marks.Talks);
        Assert.Equal(scenes.Visits(s, newScenesFirst: false), marks.Visits);

        Assert.False(marks.OnPicker(new PickerScreenEvent(PickerKind.Subordinates, (Sys3Int)1, false), s, true));
        Assert.False(marks.OnPicker(new PickerScreenEvent(PickerKind.Items, Sys3Int.Zero, true), s, true));
        s.CmdAvail_SubTalk = 0;
        memory = s.Memory.Fingerprint();
        Assert.True(marks.OnPicker(new PickerScreenEvent(PickerKind.Subordinates, (Sys3Int)1, true), s, true));
        Assert.Empty(marks.Talks);
        Assert.NotEmpty(marks.Visits);                                  // the harem's kept

        int steps = 0;
        while (marks.Warm(s))
            steps++;
        Assert.Equal(SceneMarks.TalkingUnits(s).Count + SceneMarks.OfferedGirls(s).Count - 1, steps);
        Assert.False(marks.Warming);
        Assert.Equal(memory, s.Memory.Fingerprint());
        Assert.Equal(extras.Keys, s.Extras.Keys);
        foreach (var (key, values) in extras)
            Assert.Equal(values, s.Extras[key]);
    }
}
