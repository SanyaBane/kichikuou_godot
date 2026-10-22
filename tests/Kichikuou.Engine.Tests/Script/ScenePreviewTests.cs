using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.TestSupport;

namespace Kichikuou.Engine.Tests.Script;

/// <summary>
/// The dry run (owner's decision 30, docs/ui/talk-marks.md §8 step 3): a start run on copies of a state once per path,
/// every draw and every menu forked, key waits answered at once, nothing shown, played or saved; a path ends at the
/// start's end, a jump out, a quit, a stop of the caller's hooks or past the steps' cap.
/// </summary>
public class ScenePreviewTests
{
    private const string File = "GTEST.ADV";

    private static ScriptRef At(int line) => new(File, line);

    private static MessageStatement Message(int line) => new(At(line), new ScriptText(At(line), [], []));

    private static ConstExpr Const(int value) => new((Sys3Int)value);

    private static BinaryExpr Is(int var, int value) => new("=", new VarExpr(var), Const(value));

    private static ScriptProgram Program(params ScriptStatement[] statements) => new(File, statements, new Dictionary<string, int>());

    /// <summary>Every call is the caller's stop: the files the preview does not run (a battle).</summary>
    private sealed class StopHooks(PreviewRun run) : IStoryHooks
    {
        public Task RunEventAsync(string file, Sys3Int entry)
        {
            run.Stop($"call {file}");
            return Task.CompletedTask;
        }
    }

    /// <summary>The program's scene run on a context the start builds over the run's parts, as a caller does.</summary>
    private static ScenePreview<TestState> Preview(TestState source, ScriptProgram program, int maxPaths = 256, int maxSteps = 100_000, bool keep = true) =>
        new(source, run =>
        {
            var runner = new SceneRunner(new OneProgram(program), new StopHooks(run), f => f == File);
            var script = new ScriptContext(run.State, run.Random, run.View, run.Input, runner) { Saves = run.Saves };
            runner.Bind(script);
            return runner.RunFileAsync(File);
        })
        {
            MaxPaths = maxPaths,
            MaxSteps = maxSteps,
            KeepEndStates = keep,
        };

    private static int[] LinesOf(PreviewPath path) => [.. path.Lines.Select(l => l.Line)];

    [Fact]
    public void A_start_with_no_decision_is_one_path()
    {
        var program = Program(Message(1), new AssignStatement(At(2), 5, Const(9)), Message(3), new ReturnStatement(At(4)));

        var result = Preview(new TestState(), program).Run();

        var path = Assert.Single(result.Paths);
        Assert.False(result.Truncated);
        Assert.Empty(path.Decisions);
        Assert.Equal([1, 3], LinesOf(path));
        Assert.Equal(PreviewEnd.Returned, path.End);
        Assert.Null(path.Reason);
        Assert.Equal(9, path.EndState!.Memory.Var(5).Value);
    }

    /// <summary>
    /// <c>Y 4, 3</c> at line 1, then VAR5 := RetVal × 10 and a message per value (lines 4, 7, 10), line 12 on every path.
    /// </summary>
    private static ScriptProgram DrawOfThree() => Program(
        new RandomStatement(At(1), Const(3)),                                           // 0
        new AssignStatement(At(2), 5, new BinaryExpr("*", new VarExpr(0), Const(10))),  // 1
        new IfStatement(At(3), Is(0, 1), 4),                                            // 2
        Message(4),                                                                     // 3
        new IfStatement(At(6), Is(0, 2), 6),                                            // 4
        Message(7),                                                                     // 5
        new IfStatement(At(9), Is(0, 3), 8),                                            // 6
        Message(10),                                                                    // 7
        Message(12),                                                                    // 8
        new ReturnStatement(At(13)));                                                   // 9

    [Fact]
    public void A_draw_of_three_is_three_paths_each_with_its_lines_and_its_end_state()
    {
        var result = Preview(new TestState(), DrawOfThree()).Run();

        Assert.Equal(3, result.Paths.Count);
        for (int value = 1; value <= 3; value++)
        {
            var path = result.Paths[value - 1];
            Assert.Equal([PreviewDecision.Draw(3, value, At(1))], path.Decisions);     // the draw's place is the Y 4's
            Assert.Equal([1 + 3 * value, 12], LinesOf(path));
            Assert.Equal(10 * value, path.EndState!.Memory.Var(5).Value);
            Assert.Equal(value, path.EndState.RetVal.Value);
            Assert.Equal(PreviewEnd.Returned, path.End);
        }
        Assert.Equal(3, result.Paths.Select(p => p.EndState).Distinct().Count());     // kept: a state each
    }

    /// <summary>Without kept end states one state serves every run, and the inspection sees each path's end as it ends.</summary>
    [Fact]
    public void Without_kept_states_the_inspection_sees_each_end_before_the_state_is_reused()
    {
        var ends = new List<(int Value, int Var5)>();
        var states = new HashSet<TestState>();

        var result = Preview(new TestState(), DrawOfThree(), keep: false).Run(inspect: (path, end) =>
        {
            ends.Add((path.Decisions[0].Value, end.Memory.Var(5).Value));
            states.Add(end);
        });

        Assert.Equal([(1, 10), (2, 20), (3, 30)], ends);
        Assert.Single(states);
        Assert.All(result.Paths, p => Assert.Null(p.EndState));
    }

    /// <summary>A draw of 2; its value 1 draws again of 3: four paths, run depth first in the order of the values.</summary>
    private static ScriptProgram NestedDraws() => Program(
        new RandomStatement(At(1), Const(2)),           // 0
        new IfStatement(At(2), Is(0, 1), 4),            // 1
        new RandomStatement(At(3), Const(3)),           // 2
        Message(4),                                     // 3
        Message(6),                                     // 4
        new ReturnStatement(At(7)));                    // 5

    [Fact]
    public void Nested_draws_are_run_depth_first()
    {
        var result = Preview(new TestState(), NestedDraws()).Run();

        Assert.Equal(
        [
            [PreviewDecision.Draw(2, 1, At(1)), PreviewDecision.Draw(3, 1, At(3))],
            [PreviewDecision.Draw(2, 1, At(1)), PreviewDecision.Draw(3, 2, At(3))],
            [PreviewDecision.Draw(2, 1, At(1)), PreviewDecision.Draw(3, 3, At(3))],
            [PreviewDecision.Draw(2, 2, At(1))],
        ], result.Paths.Select(p => p.Decisions.ToArray()));
        Assert.Equal([[4, 6], [4, 6], [4, 6], [6]], result.Paths.Select(LinesOf));
        Assert.All(result.Paths, p => Assert.Equal(p.Decisions, p.Draws));
    }

    [Fact]
    public void A_menu_is_its_items_and_the_cancel()
    {
        // Two items ($label$ at 1 and 4, their texts at 2 and 5): L_A → line 9, L_B → line 12, the cancel on to line 15.
        var program = Program(
            new MenuItemStatement(At(1), "L_A", 8),         // 0
            Message(2),                                     // 1
            new MenuItemEndStatement(At(3)),                // 2
            new MenuItemStatement(At(4), "L_B", 10),        // 3
            Message(5),                                     // 4
            new MenuItemEndStatement(At(6)),                // 5
            new MenuStatement(At(7)),                       // 6
            new GotoStatement(At(8), "L_END", 12),          // 7   the cancel goes on here
            Message(9),                                     // 8   L_A
            new ReturnStatement(At(10)),                    // 9
            Message(12),                                    // 10  L_B
            new ReturnStatement(At(13)),                    // 11
            Message(15),                                    // 12  L_END
            new ReturnStatement(At(16)));                   // 13

        var result = Preview(new TestState(), program).Run();

        Assert.Equal([1, 2, 0], result.Paths.Select(p => Assert.Single(p.Decisions).Value));
        Assert.All(result.Paths, p => Assert.Equal((PreviewDecisionKind.Menu, 2, (ScriptRef?)At(1)), (p.Decisions[0].Kind, p.Decisions[0].Count, p.Decisions[0].At)));
        Assert.Equal([[2, 5, 9], [2, 5, 12], [2, 5, 15]], result.Paths.Select(LinesOf));   // both items' texts on every path
        Assert.Empty(result.Paths[0].Draws);
    }

    [Fact]
    public void A_forced_prefix_is_followed_on_every_path_and_one_that_does_not_fit_is_an_error()
    {
        var preview = Preview(new TestState(), NestedDraws());

        var result = preview.Run([PreviewDecision.Draw(2, 1)]);       // no place given: any place fits
        Assert.Equal([1, 2, 3], result.Paths.Select(p => p.Decisions[1].Value));
        Assert.All(result.Paths, p => Assert.Equal(PreviewDecision.Draw(2, 1, At(1)), p.Decisions[0]));

        var second = Assert.Single(preview.Run([PreviewDecision.Draw(2, 1, At(1)), PreviewDecision.Draw(3, 2, At(3))]).Paths);
        Assert.Equal([1, 2], second.Decisions.Select(d => d.Value));

        Assert.Throws<InvalidOperationException>(() => preview.Run([PreviewDecision.Draw(4, 1)]));              // another n
        Assert.Throws<InvalidOperationException>(() => preview.Run([PreviewDecision.Menu(2, 1)]));              // another kind
        Assert.Throws<InvalidOperationException>(() => preview.Run([PreviewDecision.Draw(2, 1, At(9))]));       // another place
        Assert.Throws<InvalidOperationException>(() => preview.Run([PreviewDecision.Draw(2, 3)]));              // out of range
        Assert.Throws<InvalidOperationException>(() => preview.Run([PreviewDecision.Draw(2, 2), PreviewDecision.Draw(3, 1)]));   // past the path's end
    }

    [Fact]
    public void A_jump_out_and_a_quit_end_their_paths()
    {
        var program = Program(
            new RandomStatement(At(1), Const(3)),       // 0
            new IfStatement(At(2), Is(0, 1), 4),        // 1
            Message(3),                                 // 2
            new JumpFileStatement(At(4), "OVER.ADV"),   // 3
            new IfStatement(At(6), Is(0, 2), 7),        // 4
            Message(7),                                 // 5
            new QuitStatement(At(8), Const(0)),         // 6
            Message(10),                                // 7
            new ReturnStatement(At(11)));               // 8

        var result = Preview(new TestState(), program).Run();

        Assert.Equal(
            [(PreviewEnd.Jumped, "OVER.ADV"), (PreviewEnd.Quit, null), (PreviewEnd.Returned, null)],
            result.Paths.Select(p => (p.End, p.Reason)));
        Assert.Equal([[3], [7], [10]], result.Paths.Select(LinesOf));
    }

    /// <summary>A call the preview does not run ends the path as the caller's stop, and so does a touch of the save data.</summary>
    [Fact]
    public void A_stop_of_the_callers_hooks_and_a_save_end_their_paths()
    {
        var program = Program(
            new RandomStatement(At(1), Const(2)),                       // 0
            new IfStatement(At(2), Is(0, 1), 4),                        // 1
            Message(3),                                                 // 2
            new CallFileStatement(At(4), "FIGT_A.ADV"),                 // 3
            new SaveDataStatement(At(6), "LD", Const(1)),               // 4   a load reads the save data
            Message(7),                                                 // 5
            new ReturnStatement(At(8)));                                // 6
        var source = new TestState();

        var result = Preview(source, program).Run();

        Assert.Equal([(PreviewEnd.Stopped, "call FIGT_A.ADV"), (PreviewEnd.Stopped, "save data")], result.Paths.Select(p => (p.End, p.Reason)));
        Assert.Equal([[3], []], result.Paths.Select(LinesOf));
    }

    [Fact]
    public void A_loop_that_never_ends_is_aborted_by_the_steps_cap()
    {
        // *L: IM VAR1, VAR2: {RetVal = 0: @L:} — the mouse never pressed, the release loop of the scenes turned round.
        var program = Program(
            Message(1),                                             // 0
            new MouseStatement(At(2), 1, 2),                        // 1
            new IfStatement(At(3), Is(0, 0), 4),                    // 2
            new GotoStatement(At(4), "L", 1),                       // 3
            new ReturnStatement(At(6)));                            // 4

        var path = Assert.Single(Preview(new TestState(), program, maxSteps: 1000).Run().Paths);

        Assert.Equal(PreviewEnd.Aborted, path.End);
        Assert.Equal("more than 1000 steps", path.Reason);
        Assert.Equal([1], LinesOf(path));
    }

    /// <summary>Each decision notes how many lines the path had shown before it: the lines between are its value's.</summary>
    [Fact]
    public void A_path_notes_the_lines_shown_before_each_decision()
    {
        var program = Program(
            Message(1),                                             // 0
            new RandomStatement(At(2), Const(2)),                   // 1
            Message(3),                                             // 2
            new RandomStatement(At(4), Const(2)),                   // 3
            Message(5),                                             // 4
            new ReturnStatement(At(6)));                            // 5

        var result = Preview(new TestState(), program).Run();

        Assert.Equal(4, result.Paths.Count);
        Assert.All(result.Paths, p => Assert.Equal([1, 2], p.DecisionLines));
        Assert.All(result.Paths, p => Assert.Equal([1, 3, 5], LinesOf(p)));
    }

    /// <summary>
    /// <c>*L: Y 4, 2: {RetVal = 2: @L:}</c> — a draw drawn again until 1 comes: the first redraw still differs (RetVal was 0
    /// before the first draw, 2 before the second), the next meets the same memory and is cut as repeated.
    /// </summary>
    [Fact]
    public void A_decision_met_again_with_the_same_memory_ends_its_path_as_repeated()
    {
        var program = Program(
            Message(1),                                             // 0
            new RandomStatement(At(2), Const(2)),                   // 1
            new IfStatement(At(3), Is(0, 2), 4),                    // 2
            new GotoStatement(At(4), "L", 1),                       // 3
            Message(5),                                             // 4
            new ReturnStatement(At(6)));                            // 5

        var result = Preview(new TestState(), program).Run();

        Assert.Equal(
            [[PreviewDecision.Draw(2, 1, At(2))], [PreviewDecision.Draw(2, 2, At(2)), PreviewDecision.Draw(2, 1, At(2))],
             [PreviewDecision.Draw(2, 2, At(2)), PreviewDecision.Draw(2, 2, At(2))]],
            result.Paths.Select(p => p.Decisions.ToArray()));
        Assert.Equal([PreviewEnd.Returned, PreviewEnd.Returned, PreviewEnd.Repeated], result.Paths.Select(p => p.End));
        Assert.Equal([1], LinesOf(result.Paths[2]));

        var forced = Preview(new TestState(), program).Run([PreviewDecision.Draw(2, 2), PreviewDecision.Draw(2, 2), PreviewDecision.Draw(2, 2)]);
        var path = Assert.Single(forced.Paths);                         // forced decisions are never cut, the next one is
        Assert.Equal(3, path.Decisions.Count);
        Assert.Equal(PreviewEnd.Repeated, path.End);
    }

    [Fact]
    public void Past_the_path_cap_the_result_is_truncated()
    {
        var program = Program(new RandomStatement(At(1), Const(10)), new ReturnStatement(At(2)));

        var capped = Preview(new TestState(), program, maxPaths: 4).Run();
        Assert.True(capped.Truncated);
        Assert.Equal([1, 2, 3, 4], capped.Paths.Select(p => p.Decisions[0].Value));

        var whole = Preview(new TestState(), program, maxPaths: 10).Run();
        Assert.False(whole.Truncated);
        Assert.Equal(10, whole.Paths.Count);
    }

    /// <summary>
    /// The runs change only their copies: the source keeps its variables and its read lines, while each end state has
    /// the lines its path noted. A path's lines are what its run showed, whatever the copy's record already held.
    /// </summary>
    [Fact]
    public void The_source_state_is_untouched_and_a_paths_lines_do_not_depend_on_the_record()
    {
        var source = new TestState();
        source.Memory.Var(5) = (Sys3Int)7;
        source.MarkLineRead(At(12));
        source.Extras["known"] = [1, 2];

        var result = Preview(source, DrawOfThree()).Run();

        Assert.Equal(7, source.Memory.Var(5).Value);
        Assert.Equal([12], source.ReadLines(File));
        Assert.Equal([1, 2], source.Extras["known"]);
        Assert.Equal(2, source.Extras.Count);
        Assert.All(result.Paths, p => Assert.Contains(12, LinesOf(p)));            // read before, still the path's line
        Assert.Equal([4, 12], result.Paths[0].EndState!.ReadLines(File));          // the copy notes as a game does
        Assert.True(result.Paths[0].ShowsUnreadLine(source));
        Assert.False(result.Paths[0].ShowsUnreadLine(result.Paths[0].EndState!));
    }

    /// <summary>
    /// A key wait is answered at once by a click (Return), a pause runs out, the date is fixed; every event goes to the
    /// run's recorder, which drops it: a view of the game's sees nothing, and no CG load is counted (no counter bound).
    /// </summary>
    [Fact]
    public void Waits_are_answered_at_once_and_nothing_reaches_the_games_view()
    {
        var game = new EngineRig();
        var program = Program(
            Message(1),                                             // 0
            new KeyWaitStatement(At(2)),                            // 1
            new AssignStatement(At(3), 5, new VarExpr(0)),          // 2   the key
            new PauseStatement(At(4), Const(30)),                   // 3
            new AssignStatement(At(5), 6, new VarExpr(0)),          // 4   the pause's key
            new EngineStatement(At(6), "G", [Const(12)]),           // 5
            new DateStatement(At(7), 10),                           // 6
            new HitAnyKeyStatement(At(8)),                          // 7
            new ReturnStatement(At(9)));                            // 8

        var path = Assert.Single(Preview(game.State, program).Run().Paths);

        Assert.Equal(PreviewInput.Click, path.EndState!.Memory.Var(5).Value);
        Assert.Equal(0, path.EndState.Memory.Var(6).Value);
        Assert.Equal([2000, 1, 1, 0, 0, 0, 7], Enumerable.Range(10, 7).Select(v => path.EndState.Memory.Var(v).Value));
        Assert.Null(game.Script.CgLoadCounter);
        Assert.Empty(game.View.Events);
        Assert.Empty(game.Input.Pauses);
        Assert.Equal([1], LinesOf(path));
    }

    /// <summary>A hand port's draw (the context's <see cref="ScriptContext.Rnd"/> with no place) is a decision with no place.</summary>
    [Fact]
    public void A_hand_ports_draw_is_a_decision_without_a_place_and_a_draw_of_one_is_none()
    {
        var preview = new ScenePreview<TestState>(new TestState(), run =>
        {
            var script = new ScriptContext(run.State, run.Random, run.View, run.Input, new NoHooks());
            script.Rnd((Sys3Int)1);
            script.Rnd(Sys3Int.Zero);
            run.State.Memory.Var(5) = script.Rnd((Sys3Int)2);
            return script.TextAsync(File, 40 + script.State.Memory.Var(5).Value);
        });

        var result = preview.Run();

        Assert.Equal([[PreviewDecision.Draw(2, 1)], [PreviewDecision.Draw(2, 2)]], result.Paths.Select(p => p.Decisions.ToArray()));
        Assert.Equal([[41], [42]], result.Paths.Select(LinesOf));
    }

    [Fact]
    public void A_start_that_awaits_what_the_run_does_not_answer_is_an_error()
    {
        var never = new TaskCompletionSource();
        var preview = new ScenePreview<TestState>(new TestState(), _ => never.Task);

        Assert.Throws<InvalidOperationException>(() => preview.Run());
    }

    /// <summary>Any other exception is a bug of the start, not a path's end.</summary>
    [Fact]
    public void Another_exception_goes_on_up()
    {
        var preview = new ScenePreview<TestState>(new TestState(), _ => throw new ScenarioFormatException("broken"));

        Assert.Throws<ScenarioFormatException>(() => preview.Run());
    }

    private sealed class CountingSource(ScriptProgram program) : ISceneSource
    {
        public int Reads { get; private set; }

        public ScriptProgram ReadProgram(string file)
        {
            Reads++;
            return program;
        }
    }

    [Fact]
    public void The_cached_source_reads_each_program_once_for_every_run()
    {
        var counting = new CountingSource(DrawOfThree());
        var scenes = new CachedSceneSource(counting);
        var preview = new ScenePreview<TestState>(new TestState(), run =>
        {
            var runner = new SceneRunner(scenes, new NoHooks(), f => f == File);
            var script = new ScriptContext(run.State, run.Random, run.View, run.Input, runner);
            runner.Bind(script);
            return runner.RunFileAsync(File);
        });

        Assert.Equal(3, preview.Run().Paths.Count);
        Assert.Equal(1, counting.Reads);
    }
}
