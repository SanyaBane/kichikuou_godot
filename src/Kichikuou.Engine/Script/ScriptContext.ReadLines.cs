namespace Kichikuou.Engine.Script;

/// <summary>
/// Ours (owner's decision 30, docs/ui/talk-marks.md §2): the record of the read lines in the state's
/// <see cref="ScriptState.Extras"/> (<see cref="ScriptState.ReadLines"/>). Every line the game shows is
/// noted here, whoever shows it: the messages through <see cref="View"/> (the scenes, the hand ports, the
/// battle), the menus through <see cref="SelectAsync"/>. Notes only: no rule reads them, so a run is the
/// same with them; the port's own text (<see cref="PortTextEvent"/>) is no line and is not noted.
/// </summary>
public partial class ScriptContext
{
    /// <summary>
    /// <c>]</c>: shows <paramref name="items"/> and returns the one the player picks, 1-based, or 0 for a
    /// cancel (<see cref="ISceneInput.SelectAsync"/>). Every item's texts are noted as read first — the
    /// whole menu was on the screen, the items not chosen too; an item's <see cref="SceneMenuItem.At"/> (its
    /// <c>$label$</c>) holds no words and is not noted, nor is the port's own text (<see cref="CapturedText.Port"/>).
    /// </summary>
    /// <exception cref="InvalidOperationException">The context's input does not answer menus (no <see cref="ISceneInput"/>).</exception>
    public async Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
    {
        var input = Input as ISceneInput
            ?? throw new InvalidOperationException("A menu is shown: the context's input must answer the scene inputs (ISceneInput).");
        foreach (var item in items)
            foreach (var text in item.Texts.Where(t => t.Port is null))
                State.MarkLineRead(text.Text);
        int choice = await input.SelectAsync(items);
        Selected(items, choice);
        return choice;
    }

    /// <summary>
    /// A menu answered (<see cref="SelectAsync"/>): <paramref name="choice"/> of <paramref name="items"/>. Nothing here; a
    /// variant's rule notes it (ours, owner's decision 30: the decisions a talk or a visit makes, which "New scenes first"
    /// replays in the dry run).
    /// </summary>
    protected virtual void Selected(IReadOnlyList<SceneMenuItem> items, int choice)
    {
    }

    /// <summary>The context's view: notes the line of each message it passes on (<see cref="MessageEvent"/>, <see cref="ValueMessageEvent"/>).</summary>
    private sealed class ReadLineView(ScriptState state, IScriptView view) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            switch (scriptEvent)
            {
                case MessageEvent m:
                    state.MarkLineRead(m.Text);
                    break;
                case ValueMessageEvent m:
                    state.MarkLineRead(m.Text);
                    break;
            }
            return view.ShowAsync(scriptEvent);
        }
    }
}
