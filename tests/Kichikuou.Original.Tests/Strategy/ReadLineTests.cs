using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Kichikuou.Original.Tests.Script;
using Kichikuou.TestSupport;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary>
/// The hand ports note the lines they show in the record of the read lines (owner's decision 30,
/// docs/ui/talk-marks.md §2) as the scenes do: a command file's texts and its menus' items.
/// </summary>
public class ReadLineTests
{
    private const string FileName = "CTEST.ADV";

    /// <summary>A command file that shows a text (line 10), a value message (12) and a menu of two items (20, 23).</summary>
    private sealed class Probe(OriginalScriptContext script) : CommandFile(script, FileName)
    {
        public async Task<int> RunAsync()
        {
            await TextAsync(10);
            await ValuesAsync(12, (Sys3Int)400);
            return await SelectAsync([MenuItem(20), MenuItem(23)]);
        }
    }

    [Fact]
    public async Task A_command_file_notes_its_texts_and_its_menus_items()
    {
        var state = new GameState();
        var input = new ScriptedSceneInput();
        var script = new OriginalScriptContext(state, new NoRandom(), new NullView(), input, new NoHooks());
        input.Choices.Enqueue(2);

        Assert.Equal(2, await new Probe(script).RunAsync());

        Assert.Equal([10, 12, 21, 24], state.ReadLines(FileName));     // each item's text, the line after its $label$
        Assert.Single(input.Menus);
    }
}
