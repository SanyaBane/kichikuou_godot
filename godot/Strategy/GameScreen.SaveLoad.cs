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
    /// A slot list over our Save/Load screen in unattended play. With <c>--saveload</c> from the command panel: the save list
    /// (<c>save-load-save</c>, the first empty slot chosen; the button hovered, <c>save-load-button</c>) saved into its slot
    /// by the red button; listed again (<c>save-load-saved</c>, "Save over Slot 1"), the Load tab; the load list
    /// (<c>save-load</c>, the slot just written chosen), Auto's row clicked (<c>save-load-auto</c>), a right click back to the
    /// map. Any other list (the title's at <c>--load</c>) is answered as before (<see cref="AutoChoice"/>), the title's dumped
    /// once in a window (<c>save-load-title</c>).
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
                Expect(list == SaveList.Save && _saveView.Chosen == 2, "the save list with Slot 1 chosen");
                await AutoDumpAsync("save-load-save");
                await AutoViewMoveAsync(_saveView.ButtonCentre);
                await AutoDumpAsync("save-load-button");
                await AutoViewClickAsync(_saveView.ButtonCentre, MouseButton.Left);
                break;
            case 1:
                Expect(list == SaveList.Save && _saves?.Read(2) is { } saved && WeekLoop.IsMomentSave(saved.Resume),
                    "Slot 1 saved as the game is now");
                await AutoDumpAsync("save-load-saved");
                await AutoViewClickAsync(_saveView.TabCentre(SaveList.Load), MouseButton.Left);
                break;
            default:
                Expect(list == SaveList.Load && _saveView.Chosen == 2, "the load list with Slot 1 chosen");
                await AutoDumpAsync("save-load");
                await AutoSortAsync();
                await AutoViewClickAsync(_saveView.RowCentre(1), MouseButton.Left);
                Expect(_saveView.Chosen == 1, "Auto chosen");
                await AutoDumpAsync("save-load-auto");
                await AutoViewClickAsync(_saveView.RowCentre(1), MouseButton.Right);
                GD.Print("Kichikuou: --auto saved into Slot 1 through our Save/Load screen and left it.");
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
    /// The order's switch (docs/ui/save-load.md, §7) from the player's order back to it: by slot ascending, then Date
    /// (<c>save-load-date</c>, newest first) and Date again (<c>save-load-date-oldest</c>), Slot, and Slot again
    /// (<c>save-load-slot-down</c>, 9 at the top); the player's order is clicked back, so the setting is theirs.
    /// </summary>
    private async Task AutoSortAsync()
    {
        var players = _saveView.Order;
        await ClickOrderUntilAsync(SlotOrder.Default);
        (SlotSortKey Key, SlotOrder Then, string Dump)[] steps =
        [
            (SlotSortKey.Date, new(SlotSortKey.Date, true), "save-load-date"),
            (SlotSortKey.Date, new(SlotSortKey.Date, false), "save-load-date-oldest"),
            (SlotSortKey.Slot, SlotOrder.Default, ""),
            (SlotSortKey.Slot, new(SlotSortKey.Slot, true), "save-load-slot-down"),
        ];
        foreach (var (key, then, dump) in steps)
        {
            await AutoViewClickAsync(_saveView.SortCentre(key), MouseButton.Left);
            Expect(_saveView.Order == then, $"the order {then}");
            Expect(_saveView.ManualSlots.Count == SaveSlots.Count - 1 && _saveView.ManualSlots.Order().SequenceEqual(Enumerable.Range(2, SaveSlots.Count - 1)),
                "the nine manual saves listed");
            if (dump != "")
                await AutoDumpAsync(dump);
        }
        Expect(_saveView.ManualSlots[0] == 10, "Slot 9 at the top");
        await ClickOrderUntilAsync(players);
    }

    /// <summary>Clicks <paramref name="order"/>'s button until the view's order is it: its key first, then its direction.</summary>
    private async Task ClickOrderUntilAsync(SlotOrder order)
    {
        for (int i = 0; i < 2 && _saveView.Order != order; i++)
            await AutoViewClickAsync(_saveView.SortCentre(order.Key), MouseButton.Left);
        Expect(_saveView.Order == order, $"the order {order}");
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
