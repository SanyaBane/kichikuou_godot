using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The marks on Talk and the harem (owner's decision 30, docs/ui/talk-marks.md §8 step 4) on the player's files: a
/// talk or a visit run in the dry run (<see cref="SceneMarks"/>) from a booted new game at story stage 1, the story set
/// as each case needs, its lines marked read as the case needs. Skipped without the decompile.
/// </summary>
public class SceneMarksTests(ITestOutputHelper output)
{
    /// <summary>
    /// Sill's (unit 2) silent step (§4): at her state 2 her talk plays her repeated line and sets 3 without a word
    /// (<c>Gｼｨﾙ:1025</c>); with that line read the talk is still not "read" — the next talk plays a new scene.
    /// </summary>
    [ScenarioFact]
    public async Task Sills_silent_step_is_new_though_its_lines_are_read()
    {
        var (state, marks) = await BootAsync();
        state.Units[2].Status = 1;
        state.Units[2].Troops = 100;                                    // not Gｼｨﾙ2's scene for few troops
        state.F_Sill_State = 2;
        state.F_SillClothes_State = 0;
        Assert.Equal(SceneMark.New, Talk(marks, state, 2).Mark);

        ReadAll(state, Talk(marks, state, 2));
        var report = Talk(marks, state, 2);
        output.WriteLine(report.ToString());
        Assert.Equal(SceneMark.New, report.Mark);
        var leaf = Assert.Single(report.Tree.Leaves);
        Assert.Equal(-1, leaf.FirstUnread);
        Assert.Contains(leaf.Changes, c => c.Space == StateChange.Variables && c.Slot.EndsWith("F_Sill_State") && c.After == "3");

        state.F_Sill_State = 4;                                         // her state 4's scene, read too: nothing changes
        ReadAll(state, Talk(marks, state, 2));
        var fourth = Talk(marks, state, 2);
        output.WriteLine(fourth.ToString());
        Assert.Equal(SceneMark.Read, fourth.Mark);
    }

    /// <summary>
    /// Rance's (unit 1) self-talk (§1, §4): talk 1 shows new lines; talk 2 shows them again and changes only
    /// <c>N_RanceSelfTalk</c>, which does not count; talk 10 shows the next lines. The command's own "Maybe I'll meet
    /// with X." (<c>Cﾌﾞｶ:1104</c>) is never news.
    /// </summary>
    [ScenarioFact]
    public async Task Rances_talks_1_and_10_are_new_and_talk_2_is_read()
    {
        var (state, marks) = await BootAsync();
        state.N_RanceSelfTalk = 0;
        var first = Talk(marks, state, 1);
        output.WriteLine(first.ToString());
        Assert.Equal(SceneMark.New, first.Mark);
        Assert.Contains(new ScriptRef("Cﾌﾞｶ.ADV", 1104), first.Tree.Leaves[0].Path.Lines);
        ReadAll(state, first);                                          // line 1104 left unread: it does not count

        state.N_RanceSelfTalk = 1;
        Assert.Equal(SceneMark.Read, Talk(marks, state, 1).Mark);
        Assert.Equal(SceneMark.Read, Talk(marks, state, 1, newScenesFirst: false).Mark);

        state.N_RanceSelfTalk = 9;
        var tenth = Talk(marks, state, 1);
        Assert.Equal(SceneMark.New, tenth.Mark);
        Assert.Contains(new ScriptRef("Cﾌﾞｶ.ADV", 1357), tenth.Tree.Leaves[0].Path.Lines);
    }

    /// <summary>
    /// Maria's (unit 16) talk ends with one of five harmless answers (<c>Gﾏﾘｱ:679</c>, §3 kind 1): with one answer
    /// unread, "New scenes first" makes it come (New); off, a draw decides (???); all read, nothing is new.
    /// </summary>
    [ScenarioFact]
    public async Task Marias_unread_variant_is_new_with_the_option_and_maybe_without()
    {
        var (state, marks) = await BootAsync();
        state.Units[16].Status = 1;
        state.F_Maria_State = 0;
        state.F_Maria_Tulip = 1;                                        // the default talk, L_00a91
        var draw = new ScriptRef("Gﾏﾘｱ.ADV", 679);
        var report = Talk(marks, state, 16);
        output.WriteLine(report.ToString());
        Assert.Equal(5, report.Tree.Leaves.Count);
        ReadAll(state, report, leaf => leaf.Path.Draws.Single(d => d.At == draw).Value != 3);

        var unread = Talk(marks, state, 16);
        Assert.Equal(SceneMark.New, unread.Mark);
        Assert.Equal(DrawRule.HarmlessUnread, Choice(unread, true).Rule);
        Assert.Equal([3], Choice(unread, true).Values);
        Assert.Equal(SceneMark.Maybe, Talk(marks, state, 16, newScenesFirst: false).Mark);
        Assert.Equal(DrawRule.Odds, Choice(unread, false).Rule);

        ReadAll(state, unread);
        Assert.Equal(SceneMark.Read, Talk(marks, state, 16).Mark);
        Assert.Equal(SceneMark.Read, Talk(marks, state, 16, newScenesFirst: false).Mark);
    }

    /// <summary>
    /// Haurein's (unit 12) talk past stage 1 (<c>Gﾊｳﾚｰﾝ:2299</c>, §3.3): with her kiss prank unread, the prank comes (New);
    /// with it read and her conditions met (her S&amp;M Tower night, Barres (unit 6) in the army), she may leave: ???,
    /// the leaving chosen with the option on; without her conditions, read.
    /// </summary>
    [ScenarioFact]
    public async Task Haureins_leaving_is_maybe_once_her_kiss_is_read()
    {
        var (state, marks) = await BootAsync();
        state.StoryStage = 2;
        state.Units[12].Status = 1;
        state.Units[6].Status = 1;
        state.F_Haurein_Arc = 0;
        state.F_Haurein_State2 = 2;
        var draw = new ScriptRef("Gﾊｳﾚｰﾝ.ADV", 2299);
        var fresh = Talk(marks, state, 12);
        output.WriteLine(fresh.ToString());
        Assert.Equal(SceneMark.New, fresh.Mark);
        Assert.Equal(DrawRule.HarmlessUnread, Choice(fresh, true).Rule);
        Assert.Equal([2, 3], Choice(fresh, true).Values);

        ReadAll(state, fresh, leaf => leaf.Path.Draws.Single(d => d.At == draw).Value != 1);
        var read = Talk(marks, state, 12);
        Assert.Equal(SceneMark.Maybe, read.Mark);
        Assert.Equal(DrawRule.Listed, Choice(read, true).Rule);
        Assert.Equal([1], Choice(read, true).Values);
        Assert.Equal(SceneMark.Maybe, Talk(marks, state, 12, newScenesFirst: false).Mark);
        Assert.Contains(read.Tree.Leaves, l => l.Changes.Any(c => c.Slot == "Units[12].Status" && c.After == "0"));

        state.F_Haurein_State2 = 0;                                     // no tower night: the draw changes nothing
        Assert.Equal(SceneMark.Read, Talk(marks, state, 12).Mark);
    }

    /// <summary>
    /// Kentarou's (unit 31) training at his state 5 (<c>Gｹﾝﾀﾛｳ:608</c>, §3.5): unread, New; read, the draw may move his
    /// state on silently: ???, the step chosen with the option on.
    /// </summary>
    [ScenarioFact]
    public async Task Kentarous_training_is_maybe_once_read()
    {
        var (state, marks) = await BootAsync();
        state.Units[31].Status = 1;
        state.F_Kentarou_State = 5;
        var fresh = Talk(marks, state, 31);
        output.WriteLine(fresh.ToString());
        Assert.Equal(SceneMark.New, fresh.Mark);

        ReadAll(state, fresh);
        var read = Talk(marks, state, 31);
        Assert.Equal(SceneMark.Maybe, read.Mark);
        Assert.Equal(DrawRule.Listed, Choice(read, true).Rule);
        Assert.Equal([1], Choice(read, true).Values);
        Assert.Equal(SceneMark.Maybe, Talk(marks, state, 31, newScenesFirst: false).Mark);
    }

    /// <summary>
    /// Isoroku's (unit 27) stage-2 visit with the Hospital built (<c>Gｲｿﾛｸ:4599</c>, §3.6): read, her pregnancy may come
    /// silently: ???; without the Hospital, read. Rance's experience and the harem's counts are no news.
    /// </summary>
    [ScenarioFact]
    public async Task Isorokus_pregnancy_is_maybe_once_her_visit_is_read()
    {
        var (state, marks) = await BootAsync();
        state.StoryStage = 2;
        state.Units[27].Status = 1;
        state.N_Isoroku_Stage = 2;
        state.Bld_Hospital = 1;
        var fresh = Visit(marks, state, 44);
        output.WriteLine(fresh.ToString());
        Assert.Equal(SceneMark.New, fresh.Mark);

        ReadAll(state, fresh);
        var read = Visit(marks, state, 44);
        output.WriteLine(read.ToString());
        Assert.Equal(SceneMark.Maybe, read.Mark);
        Assert.Equal(DrawRule.Listed, Choice(read, true).Rule);
        Assert.Equal([1], Choice(read, true).Values);

        state.Bld_Hospital = 0;
        Assert.Equal(SceneMark.Read, Visit(marks, state, 44).Mark);
    }

    /// <summary>
    /// Kanami's visit after her first (<c>Gｶﾅﾐ:182</c>, §3.4): her usual visit read, scene 2 or the tower scene may come —
    /// with the option on they come for certain, New (Code's event 4 asks the player); off, ???.
    /// </summary>
    [ScenarioFact]
    public async Task Kanamis_scenes_are_new_with_the_option_once_her_usual_visit_is_read()
    {
        var (state, marks) = await BootAsync();
        state.N_Kanami_State = 2;
        state.F_Kanami_Scene1 = 0;
        state.F_Kanami_Scene2 = 0;
        state.Bld_SMTower = 1;
        var draw = new ScriptRef("Gｶﾅﾐ.ADV", 182);
        var fresh = Visit(marks, state, 6);
        output.WriteLine(fresh.ToString());
        ReadAll(state, fresh, leaf => leaf.Path.Draws.Single(d => d.At == draw).Value >= 3);

        var read = Visit(marks, state, 6);
        output.WriteLine(read.ToString());
        Assert.Equal(SceneMark.New, read.Mark);
        Assert.Equal(DrawRule.Listed, Choice(read, true).Rule);
        Assert.Equal([1, 2], Choice(read, true).Values);
        Assert.Equal(SceneMark.Maybe, Visit(marks, state, 6, newScenesFirst: false).Mark);
    }

    /// <summary>
    /// Leila's (unit 5) talk with "who do I like" (§3 kind 5): a menu is the player's, so an item leading to an unread
    /// line makes the talk New with the option on or off; its cancel, which shows the menu again, is no outcome.
    /// </summary>
    [ScenarioFact]
    public async Task Leilas_menu_item_to_an_unread_line_is_new()
    {
        var (state, marks) = await BootAsync();
        foreach (int unit in new[] { 5, 6, 7, 8 })
            state.Units[(Sys3Int)unit].Status = 1;
        state.F_LeilaRick_State = 0;
        state.F_Leila_State = 3;
        state.F_Leila_Flag = 0;
        var fresh = Talk(marks, state, 5);
        output.WriteLine(fresh.ToString());
        var menu = fresh.Tree.Leaves.SelectMany(l => l.Path.Decisions).First(d => d.Kind == PreviewDecisionKind.Menu);
        Assert.Equal(5, menu.Count);                                    // Barres, Maris, Rick, Cordoba, Me
        Assert.Contains(fresh.Tree.Leaves, l => l.Repeats);
        ReadAll(state, fresh, leaf => leaf.Path.Decisions.First(d => d.Kind == PreviewDecisionKind.Menu).Value != 3);

        Assert.Equal(SceneMark.New, Talk(marks, state, 5).Mark);
        Assert.Equal(SceneMark.New, Talk(marks, state, 5, newScenesFirst: false).Mark);
        ReadAll(state, fresh);
        Assert.Equal(SceneMark.Read, Talk(marks, state, 5).Mark);
    }

    /// <summary>
    /// Katyusha's visit with Saulnia in the game and Sill (unit 2) absent: "Have Threesome" ends the game (ending 12,
    /// <c>Gｶﾁｭｰｼｬ:2293</c>), which the marks leave out — its lines unread, the visit is still read.
    /// </summary>
    [ScenarioFact]
    public async Task Katyushas_path_to_ending_12_is_left_out()
    {
        var (state, marks) = await BootAsync();
        state.Units[2].Status = 0;
        state.F_Katyusha_State = 2;
        state.F_Saulnia_State = 2;
        var fresh = Visit(marks, state, 27);
        output.WriteLine(fresh.ToString());
        var ending = Assert.Single(fresh.Tree.Leaves, l => l.LeftOut);
        Assert.Equal("OVER.ADV", ending.Path.Reason);
        ReadAll(state, fresh, leaf => !leaf.LeftOut);

        var read = Visit(marks, state, 27);
        Assert.True(Assert.Single(read.Tree.Leaves, l => l.LeftOut).FirstUnread >= 0);
        Assert.Equal(SceneMark.Read, read.Mark);
    }

    /// <summary>
    /// Every talk and every visit runs in the dry run (no file met that it cannot run, no loop cut by the steps' cap, no
    /// tree past the paths' cap), and the game's state is not touched: its memory and its read lines are as before.
    /// </summary>
    [ScenarioFact]
    public async Task Every_talk_and_visit_runs_and_leaves_the_state_alone()
    {
        var (state, marks) = await BootAsync();
        for (int unit = 1; unit <= 50; unit++)
            state.Units[(Sys3Int)unit].Status = 1;
        for (int girl = 1; girl <= 50; girl++)
            state.Memory.Var(248 + girl) = 1;
        state.MarkLineRead(new ScriptRef("Gｼｨﾙ.ADV", 1));
        ulong memory = state.Memory.Fingerprint();
        var extras = state.Extras.ToDictionary(e => e.Key, e => e.Value.ToArray());

        var talks = Enumerable.Range(1, 50).Select(unit => Talk(marks, state, unit)).ToList();
        var visits = Enumerable.Range(1, 50).Select(girl => Visit(marks, state, girl)).ToList();

        Assert.Equal(Enumerable.Range(1, 50), marks.Talks(state, newScenesFirst: true).Keys.Order());
        Assert.Equal(Enumerable.Range(1, 50), marks.Visits(state, newScenesFirst: true).Keys.Order());
        Assert.Equal(memory, state.Memory.Fingerprint());
        Assert.Equal(extras.Keys, state.Extras.Keys);
        foreach (var (key, values) in extras)
            Assert.Equal(values, state.Extras[key]);
        foreach (var report in talks.Concat(visits))
        {
            Assert.False(report.Tree.Truncated, report.ToString());
            Assert.DoesNotContain(report.Tree.Leaves, l => l.Path.End == PreviewEnd.Aborted);
            Assert.DoesNotContain(report.Tree.Leaves, l => l.Path.End == PreviewEnd.Stopped && !IsBattle(l.Path.Reason));
        }
    }

    private static bool IsBattle(string? file) => file is "FIGT_A.ADV" or "FIGT_B.ADV" or "FIGT.ADV" or "死亡捕獲.ADV" or "Sﾌﾞﾀｲ.ADV";

    /// <summary>The booted new game at story stage 1 and the marks over the player's scenes.</summary>
    private static async Task<(GameState State, SceneMarks Marks)> BootAsync()
    {
        var game = await EndGame.BootAsync();
        game.State.StoryStage = 1;
        return (game.State, new SceneMarks(new ScenarioScripts(LocalData.ScenarioDirectory)));
    }

    private static SceneMarkReport Talk(SceneMarks marks, GameState state, int unit, bool newScenesFirst = true) =>
        marks.Talk(state, unit, newScenesFirst);

    private static SceneMarkReport Visit(SceneMarks marks, GameState state, int girl, bool newScenesFirst = true) =>
        marks.Visit(state, girl, newScenesFirst);

    /// <summary>The choice at the report's first draw (each case's listed or variant draw).</summary>
    private static DrawChoice Choice(SceneMarkReport report, bool newScenesFirst) =>
        report.Tree.Choose(DrawNodes(report.Tree.Root).First(), SceneMarks.Rules(newScenesFirst));

    private static IEnumerable<MarkNode> DrawNodes(MarkNode node)
    {
        if (node.Decision is { Kind: PreviewDecisionKind.Draw })
            yield return node;
        foreach (var child in node.Children.Values)
            foreach (var draw in DrawNodes(child))
                yield return draw;
    }

    /// <summary>Marks every line of the report's paths (those <paramref name="which"/> takes) read in <paramref name="state"/>, but the command's own.</summary>
    private static void ReadAll(GameState state, SceneMarkReport report, Func<MarkLeaf, bool>? which = null)
    {
        foreach (var leaf in report.Tree.Leaves.Where(which ?? (_ => true)))
            foreach (var line in leaf.Path.Lines)
                if (!SceneMarks.FrameLines.Contains(line))
                    state.MarkLineRead(line);
    }
}
