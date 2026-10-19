using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Treasure screen (docs/ui/pickers.md, mock-up S5) in the game: shown over <c>Sｱｲﾃﾑ</c> (Option → Interface →
/// New) and while one of its orders runs; an order's clicks reach the script through the screen's pointer and its
/// question is answered here (<see cref="TreasureOrder"/>). Unattended play gives the same orders through the screen with
/// its own pointer (<see cref="AutoTreasureAsync"/>).
/// </summary>
public partial class GameScreen
{
    private TreasureView _treasure = null!;
    private PointerScript _treasurePointer = new();     // the script's pointer while our Treasure screen shows
    private TreasureOrder? _treasureOrder;              // the order under way, or the last one

    /// <summary>The pointer an order's clicks go to: unattended play's own, else the screen's.</summary>
    private PointerScript TreasurePointer => _options.Auto ? _pointer : _treasurePointer;

    private bool TreasureRunning => _treasureOrder is { Running: true };

    private void ReadyTreasure(FacePictures faces)
    {
        _treasure = new TreasureView(GodotGlyphRenderer.Chosen, faces);
        _treasure.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _treasure.EquipPressed += TreasureEquipPressed;
        _treasure.UnequipPressed += TreasureUnequipPressed;
        _treasure.EndPressed += TreasureEndPressed;
        _content.AddChild(_treasure);
    }

    /// <summary>
    /// Shows our Treasure screen while <c>Sｱｲﾃﾑ</c> is open with Option → Interface on New — not under a message window,
    /// unless an order runs (its question's window stays under the screen); places it over the picture and fills it from
    /// GameState. Returns whether it shows.
    /// </summary>
    private bool UpdateTreasureScreen()
    {
        bool running = TreasureRunning;
        bool show = InterfaceChoice.New && _presenter.Picker is { Kind: PickerKind.Items } && (running || !_presenter.MessageWindowShown)
            && !SkippingScene && _presenter.State is not null;
        if (show != _treasure.Visible)
        {
            _treasure.Visible = show;
            if (!running)
                _treasurePointer = new PointerScript();
        }
        if (!show)
            return false;
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        _treasure.Busy = running || TreasurePointer.Pending > 0;
        _treasure.Place(_view.Position + (size - drawn) / 2, scale * Surface.Scale);
        _treasure.Refresh(_presenter.State!);
        return true;
    }

    /// <summary>The script reads the pointer over our screen: an order's clicks, else at rest on the empty 51st cell (the picker hovers nothing).</summary>
    private MouseState TreasureRead()
    {
        if (_treasurePointer.Pending == 0)
        {
            var (x, y) = TreasureRoster.Rest;
            _treasurePointer.Move(x, y);
        }
        return _treasurePointer.Next(out _);
    }

    private void TreasureEquipPressed(int unit, int item)
    {
        if (TreasureRunning || TreasurePointer.Pending > 0 || _presenter.State is not { } s)
            return;
        _treasureOrder = TreasureOrder.Equip(s, unit, item, TreasurePointer);
        if (_treasureOrder is not null)
            GD.Print($"Kichikuou: Treasure equips item {item} on unit {unit} (week {s.Week.Value}).");
    }

    private void TreasureUnequipPressed(int unit)
    {
        if (TreasureRunning || TreasurePointer.Pending > 0 || _presenter.State is not { } s)
            return;
        _treasureOrder = TreasureOrder.Unequip(s, unit, TreasurePointer);
        if (_treasureOrder is not null)
            GD.Print($"Kichikuou: Treasure takes item {_treasureOrder.Item} off unit {unit} (week {s.Week.Value}).");
    }

    /// <summary>End, or a right press on the screen: a click on the bar's End.</summary>
    private void TreasureEndPressed()
    {
        if (TreasureRunning || TreasurePointer.Pending > 0)
            return;
        TreasureRoster.End(TreasurePointer);
    }

    /// <summary>A menu while an order runs: Unequip's Yes, or null (the menu is the player's).</summary>
    private int? TreasureChoice(IReadOnlyList<SceneMenuItem> items) => TreasureRunning ? _treasureOrder!.Choose(items) : null;

    /// <summary>
    /// A key wait while an order runs: a refusal the screen did not foresee — the order is over, its clicks left dropped,
    /// and the window is the player's (the screen hides under it).
    /// </summary>
    private void TreasureKeyWait()
    {
        if (!TreasureRunning)
            return;
        GD.PushWarning("Kichikuou: a key wait under a Treasure order; the order is dropped.");
        _treasureOrder!.KeyWait();
        _treasurePointer = new PointerScript();
    }

    // ---------------------------------------------------------------- unattended play

    private PickerView? _autoTreasureFrom;
    private Queue<Func<Task>> _autoTreasureSteps = new();

    /// <summary>
    /// Unattended play on our Treasure screen (its pointer is the unattended one): <see cref="AutoTreasureSteps"/> one at a
    /// time while no order runs, then End. False when the screen does not show (the original picker is played by
    /// <see cref="AutoItemSteps"/>).
    /// </summary>
    private async Task<bool> AutoTreasureAsync()
    {
        if (_presenter.Picker is not { Kind: PickerKind.Items } picker || !UpdateTreasureScreen() || _presenter.State is not { } s)
            return false;
        if (_pointer.Pending > 0 || TreasureRunning)
            return true;
        if (_autoTreasureFrom != picker)
        {
            _autoTreasureFrom = picker;
            _autoTreasureSteps = AutoTreasureSteps(s);
        }
        if (_autoTreasureSteps.TryDequeue(out var step))
            await step();
        else
            TreasureRoster.End(_pointer);
        return true;
    }

    /// <summary>
    /// What the unattended player does on our Treasure screen, leaving the items where they were: the first character
    /// whose item can be taken off is chosen, the item taken off and given back by a drop on the window (in a window with
    /// screen dumps on: <c>treasure</c>, <c>treasure-tooltip</c> over another unit's item, <c>treasure-held</c>,
    /// <c>treasure-unequipped</c>, <c>treasure-drag</c>, <c>treasure-equipped</c>); with nobody holding one, a stock item
    /// is equipped on the first character that takes it and taken off again.
    /// </summary>
    private Queue<Func<Task>> AutoTreasureSteps(GameState s)
    {
        bool dumps = DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null;
        var steps = new Queue<Func<Task>>();
        void Act(Action act) => steps.Enqueue(() =>
        {
            act();
            return Task.CompletedTask;
        });
        void Dump(string name) => steps.Enqueue(async () =>
        {
            if (!dumps)
                return;
            UpdateTreasureScreen();                                 // an order's result: the step runs once it is over
            await AutoDumpAsync(name);
        });
        var characters = TreasureRoster.Characters(s);
        int holder = characters.FirstOrDefault(c => TreasureRoster.CanTakeOff(s, c.Unit))?.Unit ?? 0;
        int item = holder != 0 ? s.Units[holder].Item.Value : 0;
        if (holder == 0)
        {
            item = TreasureRoster.Items(s, 0).FirstOrDefault(i => i.Refusal is null)?.Number ?? 0;
            holder = item == 0 ? 0 : characters.FirstOrDefault(c => TreasureRoster.DropRefusal(s, c.Unit, item) is null)?.Unit ?? 0;
        }
        GD.Print($"Kichikuou: --auto opens our Treasure screen (week {s.Week.Value}; unit {holder}, item {item}).");
        Dump("treasure");
        if (holder == 0)
            return steps;
        int first = _treasure.Chosen;
        int other = TreasureRoster.Items(s, first).FirstOrDefault(i => i.Holder != 100 && i.Holder != first)?.Number ?? 0;
        if (other != 0 && _treasure.ItemPoint(other) is { } at)
        {
            Act(() => _treasure.ShowPointer(at));
            Dump("treasure-tooltip");
            Act(() => _treasure.ShowPointer(new Vector2(-1, -1)));
        }
        Act(() => _treasure.Choose(holder));
        Dump("treasure-held");
        if (s.Units[holder].Item.Value == item)
        {
            Act(_treasure.Unequip);
            Dump("treasure-unequipped");
            Act(() => _treasure.ShowDrag(item, TreasureView.WindowCentre + new Vector2(60, 20)));
            Dump("treasure-drag");
            Act(() =>
            {
                _treasure.ShowDrag(0, new Vector2(-1, -1));
                _treasure.Equip(item);
            });
            Dump("treasure-equipped");
        }
        else
        {
            Act(() => _treasure.Equip(item));
            Dump("treasure-equipped");
            Act(_treasure.Unequip);
            Dump("treasure-unequipped");
        }
        return steps;
    }
}
