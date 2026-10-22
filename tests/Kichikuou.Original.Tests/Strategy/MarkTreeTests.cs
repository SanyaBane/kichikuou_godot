using Kichikuou.Engine.Script;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary>
/// The marks' tree (owner's decision 30, docs/ui/talk-marks.md §8 step 4) on hand-made paths: which path is new, a
/// menu is the player's, a draw is chance, and the choice at a draw (<see cref="MarkTree.Choose"/>) with "New scenes
/// first" on and off. A line is <c>T.ADV:n</c>; the read ones are given per case.
/// </summary>
public class MarkTreeTests
{
    private static readonly ScriptRef Listed = new("T.ADV", 500);

    private static readonly MarkRules On = new(true, new Dictionary<ScriptRef, SceneMark> { [Listed] = SceneMark.Maybe });

    private static readonly MarkRules Off = On with { NewScenesFirst = false };

    private static readonly MarkRules OnNew = new(true, new Dictionary<ScriptRef, SceneMark> { [Listed] = SceneMark.New });

    private static readonly StateChange Flag = new(StateChange.Variables, 400, "0", "1");

    [Fact]
    public void A_single_path_is_new_by_an_unread_line_a_change_or_a_stop()
    {
        Assert.Equal(SceneMark.Read, Tree(read: [1, 2], Path([], [1, 2])).Mark(On));
        Assert.Equal(SceneMark.New, Tree(read: [1], Path([], [1, 2])).Mark(On));
        Assert.Equal(SceneMark.New, Tree(read: [1, 2], Path([], [1, 2], changes: [Flag])).Mark(On));
        Assert.Equal(SceneMark.New, Tree(read: [1], Path([], [1], end: PreviewEnd.Stopped)).Mark(On));
        Assert.Equal(SceneMark.Maybe, Tree(read: [1], Path([], [1], end: PreviewEnd.Aborted)).Mark(On));
    }

    [Fact]
    public void A_path_that_ends_the_game_is_left_out()
    {
        var tree = Tree(read: [1, 2, 3],
            Path([Menu(2, 1)], [1, 2, 9], [2], end: PreviewEnd.Jumped, changes: [Flag]),
            Path([Menu(2, 2)], [1, 2, 3], [2]),
            Path([Menu(2, 0)], [1, 2], [2], end: PreviewEnd.Repeated));

        Assert.Equal(SceneMark.Read, tree.Mark(On));
        Assert.Equal(SceneMark.Read, tree.Mark(Off));
    }

    [Fact]
    public void A_menu_takes_its_best_item_and_its_repeating_cancel_is_no_outcome()
    {
        var tree = Tree(read: [1, 2, 3],
            Path([Menu(2, 1)], [1, 2, 3], [2]),
            Path([Menu(2, 2)], [1, 2, 4], [2]),
            Path([Menu(2, 0)], [1, 2], [2], end: PreviewEnd.Repeated));

        Assert.Equal(SceneMark.New, tree.Mark(On));
        Assert.Equal(SceneMark.New, tree.Mark(Off));
        Assert.Equal([1, 2], tree.Root.Outcomes);
    }

    [Fact]
    public void An_unread_harmless_variant_comes_with_the_option_and_is_maybe_without()
    {
        var tree = Tree(read: [1, 11, 12],
            Path([Draw(3, 1)], [1, 11], [1]),
            Path([Draw(3, 2)], [1, 12], [1]),
            Path([Draw(3, 3)], [1, 13], [1]));

        Assert.Equal(new DrawChoice(DrawRule.HarmlessUnread, [3]).Values, tree.Choose(tree.Root, On).Values);
        Assert.Equal(DrawRule.HarmlessUnread, tree.Choose(tree.Root, On).Rule);
        Assert.Equal(SceneMark.New, tree.Mark(On));
        Assert.Equal(DrawRule.Odds, tree.Choose(tree.Root, Off).Rule);
        Assert.Equal(SceneMark.Maybe, tree.Mark(Off));
    }

    [Fact]
    public void Every_value_new_is_certain_and_every_value_read_is_read()
    {
        var unread = Tree(read: [],
            Path([Draw(2, 1)], [1, 11], [1]),
            Path([Draw(2, 2)], [1, 12], [1]));
        var read = Tree(read: [1, 11, 12],
            Path([Draw(2, 1)], [1, 11], [1]),
            Path([Draw(2, 2)], [1, 12], [1]));

        Assert.Equal(SceneMark.New, unread.Mark(Off));
        Assert.Equal(SceneMark.New, unread.Mark(On));
        Assert.Equal(SceneMark.Read, read.Mark(Off));
        Assert.Equal(SceneMark.Read, read.Mark(On));
    }

    [Fact]
    public void A_variant_that_changes_the_state_is_not_harmless()
    {
        var tree = Tree(read: [1],
            Path([Draw(2, 1)], [1, 11], [1], changes: [Flag]),
            Path([Draw(2, 2)], [1, 12], [1]));

        Assert.Equal([2], tree.Choose(tree.Root, On).Values);
        Assert.False(MarkTree.IsHarmless(tree.Root, 1));
        Assert.True(MarkTree.ChangesState(tree.Root, 1));
    }

    [Fact]
    public void What_every_path_through_a_draw_changes_alike_is_its_prefix_not_the_variant()
    {
        var tree = Tree(read: [1, 11],
            Path([Draw(2, 1)], [1, 11], [1], changes: [Flag]),
            Path([Draw(2, 2)], [1, 12], [1], changes: [Flag]));

        Assert.Equal([Flag], MarkTree.Common(tree.Root));
        Assert.True(MarkTree.IsHarmless(tree.Root, 2));
        Assert.Equal(DrawRule.HarmlessUnread, tree.Choose(tree.Root, On).Rule);
        Assert.Equal([2], tree.Choose(tree.Root, On).Values);
    }

    [Fact]
    public void A_listed_draw_with_its_usual_scene_read_chooses_the_change_and_keeps_its_mark()
    {
        MarkTree Make(params int[] read) => Tree(read,
            Path([Draw(3, 1, Listed)], [1, 11], [1], changes: [Flag]),
            Path([Draw(3, 2, Listed)], [1, 12], [1]),
            Path([Draw(3, 3, Listed)], [1, 12], [1]));

        var read = Make(1, 11, 12);
        Assert.Equal(DrawRule.Listed, read.Choose(read.Root, On).Rule);
        Assert.Equal([1], read.Choose(read.Root, On).Values);
        Assert.Equal(SceneMark.Maybe, read.Mark(On));
        Assert.Equal(SceneMark.New, read.Mark(OnNew));
        Assert.Equal(SceneMark.Maybe, read.Mark(Off));

        var usualUnread = Make(1, 11);                                  // values 2 and 3 show line 12, unread: they come first
        Assert.Equal(DrawRule.HarmlessUnread, usualUnread.Choose(usualUnread.Root, On).Rule);
        Assert.Equal([2, 3], usualUnread.Choose(usualUnread.Root, On).Values);

        var prefixUnread = Make(11, 12);                                // the scene before the draw unread: the odds
        Assert.Equal(DrawRule.Odds, prefixUnread.Choose(prefixUnread.Root, On).Rule);
        Assert.Equal(SceneMark.New, prefixUnread.Mark(On));
    }

    [Fact]
    public void A_listed_draw_never_forces_a_value_that_may_end_the_game()
    {
        var tree = Tree(read: [1, 11, 12],
            Path([Draw(2, 1, Listed), Draw(2, 1)], [1, 11], [1, 2], end: PreviewEnd.Jumped),
            Path([Draw(2, 1, Listed), Draw(2, 2)], [1, 11], [1, 2], changes: [Flag]),
            Path([Draw(2, 2, Listed)], [1, 12], [1]));

        Assert.False(MarkTree.ChangesState(tree.Root, 1));
        Assert.Equal(DrawRule.Odds, tree.Choose(tree.Root, On).Rule);
        Assert.Equal(SceneMark.Maybe, tree.Mark(On));
    }

    [Fact]
    public void A_draw_drawn_again_ends_with_another_value()
    {
        var tree = Tree(read: [1, 11],
            Path([Draw(2, 1)], [1, 11], [1]),
            Path([Draw(2, 2), Draw(2, 1)], [1, 11], [1, 1]),
            Path([Draw(2, 2), Draw(2, 2)], [1], [1, 1], end: PreviewEnd.Repeated));

        Assert.Equal(SceneMark.Read, tree.Mark(On));
        Assert.Equal(SceneMark.Read, tree.Mark(Off));
        Assert.Equal([1], tree.Root.Children[2].Outcomes);
    }

    [Fact]
    public void A_value_past_the_cap_makes_the_mark_maybe()
    {
        var tree = Tree(read: [1, 11], truncated: true, Path([Draw(2, 1)], [1, 11], [1]));

        Assert.True(tree.Root.Incomplete);
        Assert.Equal(SceneMark.Maybe, tree.Mark(On));
        Assert.Equal(DrawRule.Odds, tree.Choose(tree.Root, On).Rule);
    }

    [Fact]
    public void A_harmless_nested_draw_is_chosen_through_its_parent()
    {
        // Value 2 leads to a second draw whose value 2 is the only unread line: both draws choose their way to it.
        var tree = Tree(read: [1, 11, 21],
            Path([Draw(2, 1)], [1, 11], [1]),
            Path([Draw(2, 2), Draw(2, 1)], [1, 21], [1, 1]),
            Path([Draw(2, 2), Draw(2, 2)], [1, 22], [1, 1]));

        Assert.Equal([2], tree.Choose(tree.Root, On).Values);
        Assert.Equal([2], tree.Choose(tree.Root.Children[2], On).Values);
        Assert.Equal(SceneMark.New, tree.Mark(On));
        Assert.Equal(SceneMark.Maybe, tree.Mark(Off));
        Assert.Same(tree.Root.Children[2], tree.NodeAt([Draw(2, 2)]));
    }

    private static PreviewDecision Draw(int n, int value, ScriptRef? at = null) => PreviewDecision.Draw(n, value, at ?? new ScriptRef("T.ADV", 100));

    private static PreviewDecision Menu(int items, int value) => PreviewDecision.Menu(items, value, new ScriptRef("T.ADV", 200));

    private static (PreviewPath Path, StateChange[] Changes) Path(
        PreviewDecision[] decisions, int[] lines, int[]? decisionLines = null, PreviewEnd end = PreviewEnd.Returned, StateChange[]? changes = null) =>
        (new PreviewPath(decisions, [.. lines.Select(l => new ScriptRef("T.ADV", l))], end, end == PreviewEnd.Jumped ? "OVER.ADV" : null, null)
        {
            DecisionLines = decisionLines ?? [],
        }, changes ?? []);

    private static MarkTree Tree(int[] read, params (PreviewPath Path, StateChange[] Changes)[] paths) => Tree(read, false, paths);

    private static MarkTree Tree(int[] read, bool truncated, params (PreviewPath Path, StateChange[] Changes)[] paths)
    {
        Func<ScriptRef, bool> unread = line => !read.Contains(line.Line);
        return MarkTree.Build([.. paths.Select(p => new MarkLeaf(p.Path, p.Changes, unread))], truncated);
    }
}
