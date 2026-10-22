using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.TestSupport;

namespace Kichikuou.Engine.Tests.Script;

/// <summary>
/// A rule set's patches inside a scene (<see cref="ISceneHooks"/>; ours, owner's decision 30, docs/ui/talk-marks.md §8
/// step 6): a test answered by the rules, a step after a statement, asked only at their places; without hooks the
/// runner runs as before. And the port's own menu items (<see cref="SceneMenuItem.OfPort"/>), which are no lines.
/// </summary>
public class SceneHookTests
{
    private const string File = "GTEST.ADV";

    private static ScriptRef At(int line) => new(File, line);

    /// <summary>
    /// <c>!V1 : v!</c> (1), <c>{V1 &gt; 1:</c> (2) <c>!V2 : 1!</c> (3) <c>}</c>, <c>!V3 : 7!</c> (4), a message (5), the page's
    /// return (6).
    /// </summary>
    private static ScriptProgram Scene(int v) => new(File,
    [
        new AssignStatement(At(1), 1, new ConstExpr((Sys3Int)v)),
        new IfStatement(At(2), new BinaryExpr(">", new VarExpr(1), new ConstExpr((Sys3Int)1)), 3),
        new AssignStatement(At(3), 2, new ConstExpr((Sys3Int)1)),
        new AssignStatement(At(4), 3, new ConstExpr((Sys3Int)7)),
        new MessageStatement(At(5), new ScriptText(At(5), [], [])),
        new ReturnStatement(At(6)),
    ], new Dictionary<string, int>());

    /// <summary>Hooks at given places that record every call; the test's answer is <see cref="Answer"/>, the step shows a port text.</summary>
    private sealed class Hooks(ScriptRef[] conditions, ScriptRef[] steps) : ISceneHooks
    {
        public IReadOnlyCollection<ScriptRef> Conditions => conditions;

        public IReadOnlyCollection<ScriptRef> Steps => steps;

        public Func<bool, bool> Answer { get; set; } = holds => holds;

        public List<(ScriptRef At, bool Holds)> Asked { get; } = [];

        public List<(ScriptRef At, int V3)> Stepped { get; } = [];

        public Task<bool> ConditionAsync(ScriptContext script, ScriptRef at, bool holds)
        {
            Asked.Add((at, holds));
            return Task.FromResult(Answer(holds));
        }

        public async Task AfterAsync(ScriptContext script, ScriptRef at)
        {
            Stepped.Add((at, script.State.Memory.Var(3).Value));
            await script.ShowAsync(new PortTextEvent("Ours.", at));
        }
    }

    private sealed class Rig
    {
        public Rig(ScriptProgram program, ISceneHooks? hooks)
        {
            Runner = new SceneRunner(new OneProgram(program), new NoHooks(), f => f == File, hooks);
            Script = new ScriptContext(State, new NoRandom(), View, new UnexpectedInput(), Runner);
            Runner.Bind(Script);
        }

        public TestState State { get; } = new();
        public RecordingView View { get; } = new();
        public SceneRunner Runner { get; }
        public ScriptContext Script { get; }

        public int Var(int n) => State.Memory.Var(n).Value;
    }

    [Fact]
    public async Task Without_hooks_the_scene_runs_as_its_statements_say()
    {
        var rig = new Rig(Scene(5), null);
        await rig.Runner.RunFileAsync(File);
        Assert.Equal((1, 7), (rig.Var(2), rig.Var(3)));
        Assert.Equal([At(5)], rig.View.Events.OfType<MessageEvent>().Select(e => e.Text));
        Assert.DoesNotContain(rig.View.Events, e => e is PortTextEvent);

        var skipped = new Rig(Scene(0), null);
        await skipped.Runner.RunFileAsync(File);
        Assert.Equal((0, 7), (skipped.Var(2), skipped.Var(3)));
    }

    /// <summary>The rules answer the test both ways, whatever its condition says; the hook hears the scenario's answer.</summary>
    [Fact]
    public async Task The_rules_answer_a_test_both_ways()
    {
        var no = new Hooks([At(2)], []) { Answer = _ => false };
        var rig = new Rig(Scene(5), no);
        await rig.Runner.RunFileAsync(File);
        Assert.Equal([(At(2), true)], no.Asked);
        Assert.Equal((0, 7), (rig.Var(2), rig.Var(3)));                 // the block skipped, the scene goes on after it

        var yes = new Hooks([At(2)], []) { Answer = _ => true };
        rig = new Rig(Scene(0), yes);
        await rig.Runner.RunFileAsync(File);
        Assert.Equal([(At(2), false)], yes.Asked);
        Assert.Equal((1, 7), (rig.Var(2), rig.Var(3)));                 // the block run

        var same = new Hooks([At(2)], []);
        rig = new Rig(Scene(5), same);
        await rig.Runner.RunFileAsync(File);
        Assert.Equal((1, 7), (rig.Var(2), rig.Var(3)));                 // the scenario's answer kept
    }

    /// <summary>A step after a statement runs once it has run and before the next; the scene goes on after it.</summary>
    [Fact]
    public async Task A_step_runs_after_its_statement()
    {
        var hooks = new Hooks([], [At(4)]);
        var rig = new Rig(Scene(5), hooks);
        await rig.Runner.RunFileAsync(File);
        Assert.Equal([(At(4), 7)], hooks.Stepped);                     // V3 already written
        Assert.Collection(rig.View.Events.Where(e => e is PortTextEvent or MessageEvent),
            e => Assert.Equal(new PortTextEvent("Ours.", At(4)), e),
            e => Assert.Equal(new MessageEvent(At(5)), e));
        Assert.DoesNotContain(4, rig.State.ReadLines(File));            // the port's text is no line
    }

    /// <summary>
    /// Nothing is asked away from the places: not at a test elsewhere, not at a statement that is no test for a
    /// condition's place, not in another file; a step skipped with the block it is in is not taken.
    /// </summary>
    [Fact]
    public async Task Nothing_is_asked_away_from_the_places()
    {
        var elsewhere = new Hooks([At(1), new ScriptRef("OTHER.ADV", 2)], [new ScriptRef("OTHER.ADV", 4)]);
        var rig = new Rig(Scene(5), elsewhere);
        await rig.Runner.RunFileAsync(File);
        Assert.Empty(elsewhere.Asked);
        Assert.Empty(elsewhere.Stepped);
        Assert.Equal((1, 7), (rig.Var(2), rig.Var(3)));

        var inBlock = new Hooks([], [At(3)]);
        rig = new Rig(Scene(0), inBlock);
        await rig.Runner.RunFileAsync(File);
        Assert.Empty(inBlock.Stepped);
        await new Rig(Scene(5), inBlock).Runner.RunFileAsync(File);
        Assert.Equal([At(3)], inBlock.Stepped.Select(s => s.At));
    }

    /// <summary>The port's own menu items: shown and answered as any, their texts never noted as read.</summary>
    [Fact]
    public async Task A_port_menu_is_no_line()
    {
        var input = new ChoosingInput();
        var script = new ScriptContext(new TestState(), new NoRandom(), new RecordingView(), input, new NoHooks());
        var item = SceneMenuItem.OfPort(At(10), "Go on");
        Assert.Equal("Go on", item.Texts.Single().Port);
        Assert.Equal(At(10), item.Texts.Single().Text);

        int choice = await script.SelectAsync([item, new SceneMenuItem(At(20), [new CapturedText(At(21), [], [])])]);

        Assert.Equal(2, choice);
        Assert.Equal([21], script.State.ReadLines(File));
        Assert.Null(input.Shown![1].Texts.Single().Port);
    }

    private sealed class ChoosingInput : UnexpectedInput
    {
        public IReadOnlyList<SceneMenuItem>? Shown { get; private set; }

        public override Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            Shown = items;
            return Task.FromResult(2);
        }
    }
}
