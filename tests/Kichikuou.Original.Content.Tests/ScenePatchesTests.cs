using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;
using static Kichikuou.Original.Content.Tests.SceneChoicesTests;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The original's patches inside the scenes (<see cref="ScenePatches"/>; ours, owner's decision 30, docs/ui/talk-marks.md
/// §5, §8 step 6) on the player's files, wired as <c>GameSession</c> wires them (<see cref="SceneChoicesTests"/>' rig):
/// Kanami's choice in Code's event 4 and Kentarou's hint lines, with "New scenes first" on; off, the scenes as the
/// original — nothing asked, nothing shown. Skipped without the decompile.
/// </summary>
public class ScenePatchesTests(ITestOutputHelper output)
{
    private const string Code = "Gｺｰﾄﾞ.ADV";

    /// <summary>
    /// Code's event 4 (<c>Gｺｰﾄﾞ</c> entry 4, as <c>COM:248-251</c> calls it during the Zeth war) with Kanami's
    /// <paramref name="flags"/> of her four set: the event's state, the rule on or off.
    /// </summary>
    private static Play CodeEvent(Rig rig, int flags, bool on)
    {
        var start = rig.Start();
        start.F_Code_State = 3;
        start.F_Zeth_War = 1;
        start.F_Kanami_Scene1 = (Sys3Int)(flags >= 1 ? 1 : 0);
        start.F_Kanami_Scene2 = (Sys3Int)(flags >= 2 ? 1 : 0);
        start.F_Kanami_Scene3 = (Sys3Int)(flags >= 3 ? 1 : 0);
        start.F_Kanami_Scene4 = (Sys3Int)(flags >= 4 ? 1 : 0);
        var play = rig.Play(start, new ConstantRandom(1), on);
        play.State.Ev_Entry = 4;
        return play;
    }

    /// <summary>Our menu at the test, asked with the owner's two texts after the clock strikes one, nothing of it noted as read.</summary>
    private static void AssertKanamisMenu(Play play)
    {
        var menu = Assert.Single(play.Input.Menus);
        Assert.Equal([ScenePatches.StopCodeAt, ScenePatches.GoWithCodeAt], menu);
        var clock = play.View.Events.FindLastIndex(e => e is MessageEvent { Text: { File: Code, Line: 1441 } });
        Assert.True(clock >= 0, "The clock struck one. was not shown");
        Assert.DoesNotContain(play.View.Events.Skip(clock), e => e is MessageEvent { Text.Line: > 1441 and < 1458 });
        Assert.DoesNotContain(1457, play.State.ReadLines(Code));
        Assert.DoesNotContain(1458, play.State.ReadLines(Code));
        Assert.DoesNotContain(1460, play.State.ReadLines(Code));
    }

    /// <summary>Two flags, the rule on, "Go with Code": the test's block — <c>F_Code_State</c> 5, Code kills Rance, ending 3.</summary>
    [Fact]
    public async Task Go_with_code_plays_ending_3()
    {
        if (await BootAsync() is not { } rig)
            return;
        var play = CodeEvent(rig, 2, on: true);
        play.Input.Menu.Enqueue(items =>
        {
            Assert.Equal([ScenePatches.StopCode, ScenePatches.GoWithCode], items.Select(i => i.Texts.Single().Port));
            return 2;
        });

        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => play.Script.CallFileAsync(Code));

        Assert.Equal("OVER.ADV", jump.File);
        Assert.Equal((5, 3), (play.State.F_Code_State.Value, play.State.EndingId.Value));
        AssertKanamisMenu(play);
        Assert.True(play.State.IsLineRead(new ScriptRef(Code, 1867)));     // "...Yeah... This is no good."
    }

    /// <summary>
    /// Two flags, the rule on, "Stop Code" after a cancel (the menu comes again): the fewer-than-two branch — she stops
    /// him, <c>F_Code_State</c> stays 4, the event returns.
    /// </summary>
    [Fact]
    public async Task Stop_code_plays_the_loyal_branch_and_a_cancel_asks_again()
    {
        if (await BootAsync() is not { } rig)
            return;
        var play = CodeEvent(rig, 2, on: true);
        play.Input.Menu.Enqueue(0);
        play.Input.Menu.Enqueue(1);

        await play.Script.CallFileAsync(Code);

        Assert.Equal((4, 0), (play.State.F_Code_State.Value, play.State.EndingId.Value));
        Assert.Equal(2, play.Input.Menus.Count);
        Assert.All(play.Input.Menus, m => Assert.Equal([ScenePatches.StopCodeAt, ScenePatches.GoWithCodeAt], m));
        Assert.True(play.State.IsLineRead(new ScriptRef(Code, 1462)));     // "...No..."
        Assert.False(play.State.IsLineRead(new ScriptRef(Code, 1867)));
    }

    /// <summary>One flag, the rule on: no menu, the original's branch (she stops him).</summary>
    [Fact]
    public async Task With_one_flag_nothing_is_asked()
    {
        if (await BootAsync() is not { } rig)
            return;
        var play = CodeEvent(rig, 1, on: true);
        await play.Script.CallFileAsync(Code);
        Assert.Empty(play.Input.Menus);
        Assert.Equal((4, 0), (play.State.F_Code_State.Value, play.State.EndingId.Value));
    }

    /// <summary>Two flags (and four), the rule off: no menu, ending 3 as the original.</summary>
    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public async Task Off_two_flags_play_ending_3_unasked(int flags)
    {
        if (await BootAsync() is not { } rig)
            return;
        var play = CodeEvent(rig, flags, on: false);
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => play.Script.CallFileAsync(Code));
        Assert.Equal("OVER.ADV", jump.File);
        Assert.Empty(play.Input.Menus);
        Assert.Equal((5, 3), (play.State.F_Code_State.Value, play.State.EndingId.Value));
    }

    /// <summary>
    /// Kentarou (unit 31) from his state 5, every draw 1 (the training steps), the rule on: the first training talk ends
    /// with "Kentarou seems to be catching on to the move." (5 → 6), the second with "Kentarou has nearly caught on to the
    /// move." (6 → 7) — each in window 113 after the scene's last line and its closed window, with a key wait, before
    /// <c>M 0</c> —, the third plays Rance Attack (7 → 8) with none. Off, the same draws step him and nothing is shown.
    /// </summary>
    [Fact]
    public async Task Kentarous_training_steps_end_with_our_hint()
    {
        if (await BootAsync() is not { } rig)
            return;
        var start = rig.Start();
        start.Units[31].Status = 1;
        start.F_Kentarou_State = 5;

        var play = rig.Play(start, new ConstantRandom(1), on: true);
        var original = rig.Play(start, new ConstantRandom(1), on: false);
        string?[] expected = [ScenePatches.KentarouCatchingOn, ScenePatches.KentarouNearlyCaughtOn, null];
        for (int talk = 0; talk < 3; talk++)
        {
            int from = play.View.Events.Count;
            await play.TalkAsync(31);
            await original.TalkAsync(31);
            var events = play.View.Events.Skip(from).ToList();
            var texts = events.OfType<PortTextEvent>().ToList();
            output.WriteLine($"talk {talk + 1}: state {play.State.F_Kentarou_State.Value}: {string.Join(" / ", texts.Select(t => t.Text))}");
            Assert.Equal(6 + talk, play.State.F_Kentarou_State.Value);
            Assert.Equal(6 + talk, original.State.F_Kentarou_State.Value);
            if (expected[talk] is not { } line)
            {
                Assert.Empty(texts);
                continue;
            }
            Assert.Equal(line, string.Join(" ", texts.Select(t => t.Text)));
            Assert.All(texts, t => Assert.Equal(ScenePatches.KentarouStep, t.At));
            Assert.All(texts, t => Assert.True(t.Text.Length <= 30, $"{t.Text}: wider than the scene's lines"));
            int first = events.IndexOf(texts[0]);
            int last = events.IndexOf(texts[^1]);
            Assert.Equal(113, events.Take(first).OfType<WindowEvent>().Last().Op.Value);   // the narration window
            Assert.Contains(events.Take(first), e => e is MessageEvent { Text: { File: "Gｹﾝﾀﾛｳ.ADV", Line: 603 } });   // "Ah... Yes."
            Assert.DoesNotContain(events.Take(first).SkipWhile(e => e is not MessageEvent { Text.Line: 603 }).Skip(1),
                e => e is MessageEvent);
            var after = events.Skip(last + 1).OfType<WindowEvent>().Take(2).Select(w => w.Op.Value).ToList();
            Assert.Equal([1, 0], after);                                   // the key wait, the window closed
            Assert.Contains(events.Skip(last + 1), e => e is MapEvent { Mode.Value: 0 });   // then the map, M 0
        }
        Assert.DoesNotContain(original.View.Events, e => e is PortTextEvent);
        Assert.Equal(0, play.Choices.Fallbacks);
    }
}
