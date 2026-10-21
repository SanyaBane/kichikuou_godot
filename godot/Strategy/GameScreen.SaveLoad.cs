using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Unattended play through our Save/Load screen (<see cref="SaveLoadView"/>, docs/ui/save-load.md, §5), with
/// <c>--saveload</c>: the command panel's Save row at the first map wait, then the screen clicked as a player clicks it —
/// press and release through Godot's input at the view's points, the system pointer left alone.
/// </summary>
public partial class GameScreen
{
    /// <summary>The unattended Load/Save visit (<see cref="GameOptions.AutoSaveLoad"/>): null not yet, true wanted at the menu, false chosen.</summary>
    private bool? _autoSaveLoadWanted;

    /// <summary>How many of the visit's lists have been answered (<see cref="AutoSaveLoadAsync"/>).</summary>
    private int _autoSaveStep;

    /// <summary>
    /// A list over our Save/Load screen in unattended play. With <c>--saveload</c> from the command panel (decision 29): the
    /// save list (<c>save-load-save</c>, New save chosen; the button hovered, <c>save-load-button</c>) saved as a new manual
    /// save by the red button; listed again with it chosen (<c>save-load-saved</c>), its comment written through Edit
    /// (<c>save-load-comment</c>, then kept by Enter), Delete asked and answered Keep (<c>save-load-delete</c>), the
    /// auto-saves' side (<c>save-load-autos-save</c>, no Save there), the Load tab; the load list (<c>save-load</c>, the save
    /// just written chosen), the newest auto-save chosen (<c>save-load-auto</c>), a right click
    /// back to the map. Any other list (the title's at <c>--load</c>) is answered as before (<see cref="AutoChoice"/>: the
    /// first item, the newest auto-save), the title's dumped once in a window (<c>save-load-title</c>).
    /// </summary>
    private async Task<int> AutoSaveLoadAsync(SaveList list, IReadOnlyList<SceneMenuItem> items)
    {
        if (!_options.AutoSaveLoad || _autoSaveStep >= 3 || !_saveShortcut.Running)
        {
            if (_presenter.SaveScreenMode == 0 && Windowed && _messageDumps.Add("save-load-title"))
            {
                await DelayAsync(0.3);
                await AutoDumpAsync("save-load-title");
            }
            return AutoChoice(items);
        }
        var answer = _saveView.ChooseAsync();
        await DelayAsync(_options.Fast ? 0.1 : 0.4);
        switch (_autoSaveStep++)
        {
            case 0:
                Expect(list == SaveList.Save && _saveView.NewChosen, "the save list with New save chosen");
                await AutoDumpAsync("save-load-save");
                await AutoViewMoveAsync(_saveView.ButtonCentre);
                await AutoDumpAsync("save-load-button");
                await AutoViewClickAsync(_saveView.ButtonCentre, MouseButton.Left);
                break;
            case 1:
                Expect(list == SaveList.Save && _saveView.Chosen is { Kind: SaveKind.Manual } written
                    && _library?.Read(written) is { } saved && WeekLoop.IsMomentSave(saved.Resume), "a new manual save of the game as it is now, chosen");
                await AutoDumpAsync("save-load-saved");
                await AutoViewClickAsync(_saveView.EditCentre, MouseButton.Left);
                _saveView.CommentField.Text = "before the attack on Oaks";
                await NextFrameAsync();
                await AutoDumpAsync("save-load-comment");
                _saveView.CommentField.EmitSignal(LineEdit.SignalName.TextSubmitted, _saveView.CommentField.Text);
                await NextFrameAsync();
                Expect(_library?.Read(_saveView.Chosen!.Value)?.Comment == "before the attack on Oaks", "the comment kept");
                await AutoViewClickAsync(_saveView.DeleteCentre, MouseButton.Left);
                await AutoDumpAsync("save-load-delete");
                await AutoViewClickAsync(_saveView.DeleteAnswerCentre(yes: false), MouseButton.Left);
                Expect(_library?.Read(_saveView.Chosen!.Value) is not null, "the save kept");
                await AutoViewClickAsync(_saveView.SideCentre(auto: true), MouseButton.Left);
                if (_saveView.AutoSaves.Count > 0)
                    await AutoViewClickAsync(_saveView.RowCentre(_saveView.AutoSaves[0])!.Value, MouseButton.Left);
                await AutoDumpAsync("save-load-autos-save");
                await AutoViewClickAsync(_saveView.TabCentre(SaveList.Load), MouseButton.Left);
                break;
            default:
                Expect(list == SaveList.Load && _saveView.Chosen is { Kind: SaveKind.Manual }, "the load list with the new manual save chosen");
                Expect(_saveView.ManualSaves[0] == _saveView.Chosen, "the save just written at the top");
                await AutoDumpAsync("save-load");
                await AutoViewClickAsync(_saveView.SideCentre(auto: true), MouseButton.Left);
                if (_saveView.AutoSaves.Count > 0)
                {
                    await AutoViewClickAsync(_saveView.RowCentre(_saveView.AutoSaves[0])!.Value, MouseButton.Left);
                    Expect(_saveView.Chosen == _saveView.AutoSaves[0], "the newest auto-save chosen");
                }
                await AutoDumpAsync("save-load-auto");
                await AutoViewClickAsync(_saveView.ButtonCentre, MouseButton.Right);
                GD.Print("Kichikuou: --auto saved a new manual save through our Save/Load screen, wrote its comment and left it.");
                break;
        }
        if (!answer.IsCompleted)
            throw new InvalidOperationException($"--saveload's clicks on the {list} list gave the script no answer.");
        var (choice, other) = await answer;
        if (other is { } next)
            _saveShortcut.Switch(next);
        return choice;
    }

    private static void Expect(bool holds, string what)
    {
        if (!holds)
            throw new InvalidOperationException($"--saveload expected {what}.");
    }

    /// <summary>The pointer moved to <paramref name="at"/> (global) through Godot's input: the view's hover follows.</summary>
    private async Task AutoViewMoveAsync(Vector2 at)
    {
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        await NextFrameAsync();
        await NextFrameAsync();
    }

    /// <summary>A click at <paramref name="at"/> (global) through Godot's input: the press and, a frame later, the release.</summary>
    private async Task AutoViewClickAsync(Vector2 at, MouseButton button)
    {
        await AutoViewMoveAsync(at);
        var mask = button == MouseButton.Left ? MouseButtonMask.Left : MouseButtonMask.Right;
        foreach (bool pressed in (bool[])[true, false])
        {
            Input.ParseInputEvent(new InputEventMouseButton
            {
                ButtonIndex = button, Pressed = pressed, Position = at, GlobalPosition = at, ButtonMask = pressed ? mask : 0,
            });
            await NextFrameAsync();
        }
    }
}
