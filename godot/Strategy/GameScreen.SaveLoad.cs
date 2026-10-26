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
    /// save by the red button; listed again with it chosen (<c>save-load-saved</c>), the sweep of the saves without a
    /// comment asked and answered Keep (<c>save-load-sweep</c>), its comment written through Edit
    /// (<c>save-load-comment</c>, then kept by Enter), Delete asked and answered Keep (<c>save-load-delete</c>), the
    /// auto-saves' side (<c>save-load-autos-save</c>, no Save there), the Load tab; the load list (<c>save-load</c>, the save
    /// just written chosen), the newest auto-save chosen (<c>save-load-auto</c>), Back to
    /// the map. Any other list (the title's at <c>--load</c>) is answered as before (<see cref="AutoChoice"/>: the
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
                int bare = _saveView.UncommentedCount;
                Expect(bare >= 1, "the new save without a comment, counted by the sweep");
                await AutoViewClickAsync(_saveView.SweepCentre, MouseButton.Left);
                Expect(_saveView.SweepAsked, "the sweep's question open");
                await AutoDumpAsync("save-load-sweep");
                await AutoViewClickAsync(_saveView.SweepAnswerCentre(yes: false), MouseButton.Left);
                Expect(!_saveView.SweepAsked && _library?.Read(_saveView.Chosen!.Value) is not null, "the sweep answered Keep, the save kept");
                await AutoViewClickAsync(_saveView.EditCentre, MouseButton.Left);
                _saveView.CommentField.Text = "before the attack on Oaks";
                await NextFrameAsync();
                await AutoDumpAsync("save-load-comment");
                _saveView.CommentField.EmitSignal(LineEdit.SignalName.TextSubmitted, _saveView.CommentField.Text);
                await NextFrameAsync();
                Expect(_library?.Read(_saveView.Chosen!.Value)?.Comment == "before the attack on Oaks", "the comment kept");
                Expect(_saveView.UncommentedCount == bare - 1, "the commented save starred, out of the sweep's count");
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
                if (TouchMode.On)
                    await AutoSaveListDragAsync();
                await AutoViewClickAsync(_saveView.SideCentre(auto: true), MouseButton.Left);
                if (_saveView.AutoSaves.Count > 0)
                {
                    await AutoViewClickAsync(_saveView.RowCentre(_saveView.AutoSaves[0])!.Value, MouseButton.Left);
                    Expect(_saveView.Chosen == _saveView.AutoSaves[0], "the newest auto-save chosen");
                }
                await AutoDumpAsync("save-load-auto");
                await AutoViewClickAsync(_saveView.BackCentre, MouseButton.Left);
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

    /// <summary>
    /// The touch mode's manual saves (docs/ui/touch.md, "The lists scrolled by a finger"), in the load list of
    /// <c>--saveload</c>: a finger dragged up over the rows and held still before it rises scrolls them a row per row's
    /// height (dump <c>touch-saves-dragged</c>), flicks nothing and chooses nothing; dragged up and risen at speed it
    /// flicks them on (<c>touch-saves-flicked</c>, once the flick stopped); a tap on another row chooses its save — through
    /// Godot's input. With every manual save in view it only says so: a folder with more of them makes the list scroll
    /// (docs/ui/touch.md gives one).
    /// </summary>
    private async Task AutoSaveListDragAsync()
    {
        int end = _saveView.ManualScrollEnd;
        if (end == 0)
        {
            GD.Print($"Kichikuou: --saveload finds all {_saveView.ManualSaves.Count} manual saves in view: no drag.");
            return;
        }
        var chosen = _saveView.Chosen;
        var area = _saveView.ManualRowsArea;
        Vector2 At(float y) => area.Position + area.Size * new Vector2(0.5f, y);
        await AutoDragAsync(At(0.75f), At(0.45f), 6, 0.25);
        int dragged = _saveView.ManualScroll;
        Expect(dragged > 0 && !_saveView.Flinging && _saveView.Chosen == chosen,
            $"a drag up held still before it rose to scroll the rows ({dragged} of {end}), flick nothing and choose nothing");
        await AutoDumpAsync("touch-saves-dragged");
        await AutoDragAsync(At(0.6f), At(0.45f), 2, 0);                // a row's drag, so the flick takes them on
        bool flicked = _saveView.Flinging;
        int released = _saveView.ManualScroll;
        ulong stop = Time.GetTicksMsec() + 4000;
        while (_saveView.Flinging && Time.GetTicksMsec() < stop)
            await NextFrameAsync();
        int flung = _saveView.ManualScroll;
        Expect(!_saveView.Flinging && _saveView.Chosen == chosen && flung >= released,
            $"a quick drag up's flick to stop, scroll on ({released} to {flung} of {end}) and choose nothing");
        Expect(released == end || (flicked && flung > released), $"a quick drag up to flick the rows on ({released} to {flung} of {end})");
        await AutoDumpAsync("touch-saves-flicked");
        var other = _saveView.ManualSaves.Skip(flung).Select(id => (Id: id, At: _saveView.RowCentre(id)))
            .FirstOrDefault(r => r.Id != chosen && r.At is not null);
        Expect(other.At is not null, "a row in view to tap besides the chosen save");
        foreach (bool pressed in (bool[])[true, false])
        {
            SendPointerButton(other.At!.Value, MouseButton.Left, pressed);
            await NextFrameAsync();
        }
        Expect(_saveView.Chosen == other.Id, $"the tap on manual save {other.Id.Number} to choose it");
        GD.Print($"Kichikuou: --saveload drags the manual saves: rows down {dragged}, then {released - dragged} by the drag and "
            + $"{flung - released} by the flick, of {end}; a tap chose save {other.Id.Number}.");
    }

    private static void Expect(bool holds, string what)
    {
        if (!holds)
            throw new InvalidOperationException($"--saveload expected {what}.");
    }

    /// <summary>The pointer moved to <paramref name="at"/> (global) through Godot's input: the view's hover follows.</summary>
    private async Task AutoViewMoveAsync(Vector2 at)
    {
        SendPointerMotion(at);
        await NextFrameAsync();
        await NextFrameAsync();
    }

    /// <summary>A click at <paramref name="at"/> (global) through Godot's input: the press and, a frame later, the release.</summary>
    private async Task AutoViewClickAsync(Vector2 at, MouseButton button)
    {
        await AutoViewMoveAsync(at);
        foreach (bool pressed in (bool[])[true, false])
        {
            SendPointerButton(at, button, pressed);
            await NextFrameAsync();
        }
    }
}
