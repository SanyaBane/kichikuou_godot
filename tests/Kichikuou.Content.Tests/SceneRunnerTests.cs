using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Tests;

/// <summary>
/// <c>SceneRunner</c> (part E2) on synthetic scene files read through the E1 parser: control flow,
/// the call stack, messages, menus, draws, inputs and the calls out of a scene.
/// </summary>
public sealed class SceneRunnerTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("kichikuou-scenes").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private SceneRig Rig(QueuedRandom? random = null, params (string File, string[] Lines)[] files)
    {
        foreach (var (file, lines) in files)
            File.WriteAllLines(Path.Combine(_dir, file), lines);
        return new SceneRig(_dir, random);
    }

    private static int Var(SceneRig rig, int n) => rig.State.Memory.Var(n).Value;

    [Fact]
    public async Task A_scene_dispatches_on_its_entry_and_returns_from_any_call_depth()
    {
        var rig = Rig(null, ("GTEST.ADV",
        [
            "\t{VAR0520 = 1:",
            "\t\t@L_00010:",
            "\t}",
            "\t{VAR0520 = 2:",
            "\t\t!VAR0002 : 22!",
            "\t\t@L_00020:",
            "\t}",
            "*L_00010:",                    // the default scene
            "\t!VAR0001 : 1!",
            "\t%0:",
            "*L_00020:",
            "\t\\L_00030:",
            "\t!VAR0003 : 99!",             // never reached: %0 inside the call leaves the page
            "\t%0:",
            "*L_00030:",
            "\t!VAR0003 : VAR0002 + 1!",
            "\t%0:",
        ]));

        await rig.EventAsync("GTEST.ADV", 2);

        Assert.Equal((0, 22, 23), (Var(rig, 1), Var(rig, 2), Var(rig, 3)));
        Assert.Equal(2, rig.State.Ev_Entry.Value);

        await rig.EventAsync("GTEST.ADV", 7);

        Assert.Equal(1, Var(rig, 1));
    }

    [Fact]
    public async Task Label_calls_return_and_blocks_skip_on_zero()
    {
        var rig = Rig(null, ("GTEST.ADV",
        [
            "\t!VAR0001 : 0!",
            "*L_00010:",
            "\t\\L_00020:",
            "\t{VAR0001 < 3:",
            "\t\t@L_00010:",
            "\t}",
            "\t{(VAR0001 = 3) * (VAR0002 \\ 0):",
            "\t\t!VAR0004 : 1!",
            "\t}",
            "\t%0:",
            "*L_00020:",
            "\t!VAR0001 : VAR0001 + 1!",
            "\t!VAR0002 : VAR0002 + 10!",
            "\t\\0:",
        ]));

        await rig.EventAsync("GTEST.ADV", 1);

        Assert.Equal((3, 30, 1), (Var(rig, 1), Var(rig, 2), Var(rig, 4)));
    }

    [Fact]
    public async Task Pushed_variables_come_back_with_UG_and_with_every_return()
    {
        var rig = Rig(null, ("GTEST.ADV",
        [
            "\t!VAR0001 : 1!",
            "\t!VAR0002 : 2!",
            "\tUS VAR0001, 2:",
            "\t!VAR0001 : 10!",
            "\t!VAR0002 : 20!",
            "\tUG VAR0001, 2:",
            "\t!VAR0005 : VAR0001 * 10 + VAR0002!",   // 12
            "\t\\L_00010:",
            "\t%0:",
            "*L_00010:",
            "\tUS VAR0001, 1:",
            "\t!VAR0001 : 7!",
            "\t\\0:",                                  // restores VAR0001 := 1
        ]));

        await rig.EventAsync("GTEST.ADV", 1);

        Assert.Equal((1, 2, 12), (Var(rig, 1), Var(rig, 2), Var(rig, 5)));
    }

    [Fact]
    public async Task Messages_carry_their_values_and_window_stubs_run_the_WAO_SUB_port()
    {
        var rig = Rig(null, ("GTEST.ADV",
        [
            "\t!VAR0001 : 5!",
            "\t\\L_00010:",
            "\tZH 1:",
            "\t'Gold: '",
            "\tH 0, VAR0001 * 2:",
            "\tR",
            "\tX VAR0001 - 4:",
            "\t\\L_00020:",
            "\tR",
            "\tA",
            "\t%0:",
            "*L_00010:",
            "\t!VAR0521 : 111!",
            "\t%#WAO_SUB.ADV:",
            "\t\\0:",
            "*L_00020:",
            "\t!VAR0521 : 1!",
            "\t%#WAO_SUB.ADV:",
            "\t\\0:",
        ]));
        rig.Input.Keys.Enqueue(13);

        await rig.EventAsync("GTEST.ADV", 1);

        Assert.Equal(1, rig.State.Ui_WaoOp.Value);
        Assert.Equal(11, rig.State.Ui_WinType.Value);
        var events = rig.View.Events;
        Assert.Equal(new WindowEvent(111, 0), events[0]);
        var message = Assert.IsType<ValueMessageEvent>(events[1]);
        Assert.Equal((new ScriptRef("GTEST.ADV", 4), 10, 1), (message.Text, message.Values.Single().Value, message.StringVariables!.Single().Value));
        Assert.Equal(new WindowEvent(1, 0), events[2]);
        Assert.IsType<LineBreakEvent>(events[3]);
        Assert.IsType<KeyWaitEvent>(events[4]);
        Assert.IsType<MessageClearEvent>(events[5]);
        Assert.Equal(2, rig.Input.Answers);         // the window's key wait and A
    }

    [Fact]
    public async Task A_menu_captures_its_items_and_jumps_to_the_choice()
    {
        var rig = Rig(null, ("GTEST.ADV",
        [
            "*L_00010:",
            "\tIM VAR0202, VAR0203:",
            "\t{VAR0000:",
            "\t\t@L_00010:",                   // wait for the button release
            "\t}",
            "\tMS 1, \"Auto\":",
            "\t$L_00020$",
            "\t\\L_00040:",                    // the item's text comes from a call
            "\t$",
            "\tMS 1, \"Slot 1\":",
            "\t{VAR0009 = 0:",
            "\t\t$L_00030$",
            "\t\tX 1:",
            "\t\t$",
            "\t}",
            "\t]",
            "\t!VAR0001 : 100!",               // cancelled
            "\t%0:",
            "*L_00020:",
            "\t!VAR0001 : 1!",
            "\t%0:",
            "*L_00030:",
            "\t!VAR0001 : 2!",
            "\t%0:",
            "*L_00040:",
            "\tH 2, VAR0005:",
            "\tX 1:",
            "\t\\0:",
        ]));
        rig.State.Memory.Var(5) = (Sys3Int)7;
        rig.Input.Mouse.Enqueue(new MouseState((Sys3Int)1, (Sys3Int)10, (Sys3Int)20));
        rig.Input.Mouse.Enqueue(new MouseState(Sys3Int.Zero, (Sys3Int)11, (Sys3Int)21));
        rig.Input.Choices.Enqueue(2);

        await rig.EventAsync("GTEST.ADV", 1);

        Assert.Equal(2, Var(rig, 1));
        Assert.Equal((11, 21), (Var(rig, 202), Var(rig, 203)));
        var items = Assert.Single(rig.Input.Menus);
        Assert.Equal(2, items.Count);
        var auto = Assert.Single(items[0].Texts);
        Assert.Equal((new ScriptRef("GTEST.ADV", 26), 7, "Auto"), (auto.Text, auto.Numbers.Single().Value, auto.Strings.Single()));
        Assert.Equal("Slot 1", Assert.Single(items[1].Texts).Strings.Single());
        Assert.IsType<MessageClearEvent>(rig.View.Events[^1]);
        Assert.DoesNotContain(rig.View.Events, e => e is MessageEvent or ValueMessageEvent);

        rig.Input.Choices.Enqueue(0);
        await rig.EventAsync("GTEST.ADV", 1);

        Assert.Equal(100, Var(rig, 1));
    }

    [Fact]
    public async Task Draws_go_to_RetVal_in_order()
    {
        var random = new QueuedRandom(3, 1);
        var rig = Rig(random, ("GTEST.ADV",
        [
            "\tY 4, 4:",
            "\t!VAR0001 : VAR0000!",
            "\tY 4, 1:",                        // n of 1: no draw
            "\t!VAR0002 : VAR0000!",
            "\tY 4, VAR0001 - 1:",
            "\t!VAR0003 : VAR0000!",
            "\t%0:",
        ]));

        await rig.EventAsync("GTEST.ADV", 1);

        Assert.Equal([4, 2], random.Draws);
        Assert.Equal((3, 1, 1), (Var(rig, 1), Var(rig, 2), Var(rig, 3)));
    }

    [Fact]
    public async Task Calls_reach_other_scenes_the_helper_ports_and_the_rule_files()
    {
        var rig = Rig(null,
            ("GTEST.ADV",
            [
                "\t!VAR0520 : 3!",
                "\t%#GOTHER.ADV:",
                "\t!VAR0520 : 1!",
                "\t%#FIGT_A.ADV:",
                "\t!VAR0516 : 0!",
                "\t%#M.ADV:",
                "\t!VAR0004 : VAR0001 + VAR0002!",
                "\t&#OVER.ADV:",
            ]),
            ("GOTHER.ADV",
            [
                "\t!VAR0001 : VAR0520 * 10!",
                "\t%0:",
            ]));
        rig.Rules.Effects["FIGT_A.ADV"] = s => s.Memory.Var(2) = (Sys3Int)5;

        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => rig.EventAsync("GTEST.ADV", 1));

        Assert.Equal("OVER.ADV", jump.File);
        Assert.Equal([("FIGT_A.ADV", 1)], rig.Rules.Calls);
        Assert.Equal(35, Var(rig, 4));
        Assert.Contains(rig.View.Events, e => e is MapEvent { Mode.Value: 0 });
    }

    [Fact]
    public async Task Engine_commands_and_clock_reads_reach_the_view_and_the_input()
    {
        var rig = Rig(null, ("GTEST.ADV",
        [
            "\tG 5, VAR0001 + 1:",
            "\tZT1 0:",
            "\tZT5 VAR0010:",
            "\tZT0 VAR0021:",
            "\tSC VAR0040:",
            "\tSS 3:",
            "\tY 3, 40:",
            "\tY 1, 1:",
            "\t%0:",
        ]));

        await rig.EventAsync("GTEST.ADV", 1);

        Assert.Equal(new EngineEvent("G", [(Sys3Int)5, (Sys3Int)1]).Args, Assert.IsType<EngineEvent>(rig.View.Events[0]).Args);
        Assert.Equal(100, Var(rig, 10));
        Assert.Equal((2026, 5), (Var(rig, 21), Var(rig, 27)));
        Assert.Equal(999, Var(rig, 43));
        Assert.Equal(new MusicEvent(3), rig.View.Events[1]);
        Assert.Equal(new PauseEvent(40), rig.View.Events[2]);
        Assert.Equal(new MessageResetEvent(1), rig.View.Events[3]);
    }

    [Theory]
    [InlineData("\t!VAR0001 : 1!", "ran past the end")]
    [InlineData("\tUG VAR0001, 1:", "UG with no pushed variable")]
    [InlineData("\t\\0:", "label return with no label call")]
    [InlineData("\tQD 1:", "not ported yet")]
    [InlineData("\t$", "no menu item open")]
    public async Task Runtime_errors_name_the_location(string line, string message)
    {
        var rig = Rig(null, ("GTEST.ADV", [line]));

        var e = await Assert.ThrowsAnyAsync<Exception>(() => rig.EventAsync("GTEST.ADV", 1));

        Assert.Contains(message, e.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Files_that_are_not_scenes_go_to_the_rules()
    {
        var rig = Rig(null);

        await rig.EventAsync("Sﾌﾞﾀｲ.ADV", 4);
        await rig.Script.CallFileAsync("死亡捕獲.ADV");

        Assert.Equal([("Sﾌﾞﾀｲ.ADV", 4), ("死亡捕獲.ADV", 4)], rig.Rules.Calls);
        Assert.True(SceneRunner.IsSceneFile("Iﾄｳｿﾞｸ.ADV"));
        Assert.True(SceneRunner.IsSceneFile("ｵｰﾌﾟﾆﾝｸﾞ.ADV"));
        Assert.False(SceneRunner.IsSceneFile("FIGT_A.ADV"));
    }
}
