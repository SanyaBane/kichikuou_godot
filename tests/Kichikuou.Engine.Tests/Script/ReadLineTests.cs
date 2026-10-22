using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.TestSupport;

namespace Kichikuou.Engine.Tests.Script;

/// <summary>
/// The record of the read lines (owner's decision 30, docs/ui/talk-marks.md §2): every line the context shows —
/// a message, a value message, the texts of a menu's items — is noted in the state's <c>Extras</c>, one key per
/// file, and goes with the saves.
/// </summary>
public class ReadLineTests
{
    private const string File = "GTEST.ADV";

    private static ScriptRef At(int line) => new(File, line);

    private static ScriptText Text(int line, params ScriptExpr[] numbers) => new(At(line), numbers, []);

    /// <summary>Answers the menus from a queue, recording what each showed.</summary>
    private sealed class MenuInput : UnexpectedInput
    {
        public Queue<int> Choices { get; } = new();

        public List<IReadOnlyList<SceneMenuItem>> Menus { get; } = [];

        public override Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            Menus.Add(items);
            return Task.FromResult(Choices.Dequeue());
        }
    }

    private sealed class Rig
    {
        public Rig(ScriptProgram? program = null)
        {
            Runner = new SceneRunner(new OneProgram(program ?? new ScriptProgram(File, [], new Dictionary<string, int>())), new NoHooks(), f => f == File);
            Script = new ScriptContext(State, new NoRandom(), View, Input, Runner);
            Runner.Bind(Script);
        }

        public TestState State { get; } = new();
        public RecordingView View { get; } = new();
        public MenuInput Input { get; } = new();
        public SceneRunner Runner { get; }
        public ScriptContext Script { get; }
    }

    /// <summary>
    /// A scene: a message (line 1), a value message (2), a menu of two items (<c>$label$</c> at 3 and 6, their
    /// texts at 4 and 7), the first chosen (its message at 11), the second's message (14) never shown.
    /// </summary>
    private static ScriptProgram Scene() => new(File,
    [
        new MessageStatement(At(1), Text(1)),
        new MessageStatement(At(2), Text(2, new ConstExpr(5))),
        new MenuItemStatement(At(3), "L_A", 9),
        new MessageStatement(At(4), Text(4)),
        new MenuItemEndStatement(At(5)),
        new MenuItemStatement(At(6), "L_B", 11),
        new MessageStatement(At(7), Text(7)),
        new MenuItemEndStatement(At(8)),
        new MenuStatement(At(9)),
        new MessageStatement(At(11), Text(11)),     // L_A
        new ReturnStatement(At(12)),
        new MessageStatement(At(14), Text(14)),     // L_B
        new ReturnStatement(At(15)),
    ], new Dictionary<string, int> { ["L_A"] = 9, ["L_B"] = 11 });

    [Fact]
    public async Task A_scene_notes_its_messages_and_every_item_of_a_menu_it_shows()
    {
        var rig = new Rig(Scene());
        rig.Input.Choices.Enqueue(1);

        await rig.Runner.RunFileAsync(File);

        Assert.Equal([1, 2, 4, 7, 11], rig.State.ReadLines(File));    // the item not chosen too; not the $label$ lines
        Assert.Equal([ScriptState.ReadLinesKey(File)], rig.State.Extras.Keys);
        Assert.Equal("read:GTEST.ADV", ScriptState.ReadLinesKey(File));
        Assert.True(rig.State.IsLineRead(At(7)));
        Assert.False(rig.State.IsLineRead(At(14)));
        Assert.False(rig.State.IsLineRead(At(3)));
        Assert.Contains(rig.View.Events, e => e is ValueMessageEvent { Text.Line: 2 });   // the view still gets them
        Assert.Single(rig.Input.Menus);

        rig.Input.Choices.Enqueue(2);
        await rig.Runner.RunFileAsync(File);

        Assert.Equal([1, 2, 4, 7, 11, 14], rig.State.ReadLines(File));
    }

    /// <summary>
    /// The hand ports' and the battle's ways: the context's text, any message through <see cref="ScriptContext.ShowAsync"/>
    /// or straight to its <see cref="ScriptContext.View"/>, a menu through <see cref="ScriptContext.SelectAsync"/>. The
    /// port's own text is no line.
    /// </summary>
    [Fact]
    public async Task Every_way_to_show_a_line_notes_it_and_the_ports_own_text_is_not_noted()
    {
        var rig = new Rig();
        await rig.Script.ShowAsync(new PortTextEvent("Saved here."));
        await rig.Script.ShowAsync(new LineBreakEvent());
        Assert.Empty(rig.State.Extras);

        await rig.Script.TextAsync("CTEST.ADV", 30);
        await rig.Script.ShowAsync(new ValueMessageEvent(new ScriptRef("CTEST.ADV", 10), [(Sys3Int)3]));
        await rig.Script.View.ShowAsync(new MessageEvent(new ScriptRef("FIGT_M.ADV", 120)));
        rig.Input.Choices.Enqueue(0);
        int choice = await rig.Script.SelectAsync(
        [
            new SceneMenuItem(new ScriptRef("CTEST.ADV", 40), [new CapturedText(new ScriptRef("CTEST.ADV", 41), [], [])]),
            new SceneMenuItem(new ScriptRef("CTEST.ADV", 43), [new CapturedText(new ScriptRef("CTEST.ADV", 44), [], [])]),
        ]);

        Assert.Equal(0, choice);
        Assert.Equal([10, 30, 41, 44], rig.State.ReadLines("CTEST.ADV"));
        Assert.Equal([120], rig.State.ReadLines("FIGT_M.ADV"));
        Assert.Empty(rig.State.ReadLines("OTHER.ADV"));
        Assert.Equal(5, rig.View.Events.Count);                     // every event reached the view given
    }

    [Fact]
    public void The_record_stays_sorted_and_notes_a_line_once()
    {
        var state = new TestState();
        foreach (int line in (int[])[30, 10, 20, 10, 30, 5])
            state.MarkLineRead(At(line));
        state.MarkLineRead(new ScriptRef("OTHER.ADV", 10));

        Assert.Equal([5, 10, 20, 30], state.ReadLines(File));
        Assert.Equal([10], state.ReadLines("OTHER.ADV"));
        Assert.True(state.IsLineRead(At(20)));
        Assert.False(state.IsLineRead(At(15)));
        Assert.False(state.IsLineRead(new ScriptRef("THIRD.ADV", 10)));
    }

    /// <summary>The record goes into the save and a load replaces it whole; a save without one leaves none.</summary>
    [Fact]
    public void A_save_and_a_load_keep_the_record_and_a_save_without_one_loads_empty()
    {
        var state = new TestState();
        state.MarkLineRead(At(7));
        state.MarkLineRead(At(2));
        state.MarkLineRead(new ScriptRef("Gｼｨﾙ.ADV", 1025));
        var stream = new MemoryStream();
        SaveData.Capture(state, At(100)).WriteTo(stream);
        stream.Position = 0;
        var read = SaveData.ReadFrom(stream);

        var loaded = new TestState();
        loaded.MarkLineRead(At(50));
        read.Restore(loaded);
        Assert.Equal([2, 7], loaded.ReadLines(File));
        Assert.Equal([1025], loaded.ReadLines("Gｼｨﾙ.ADV"));
        Assert.False(loaded.IsLineRead(At(50)));

        var old = SaveData.Capture(new TestState(), At(100));       // a save from before the record
        old.Restore(loaded);
        Assert.Empty(loaded.ReadLines(File));
        Assert.Empty(loaded.Extras);
    }
}
