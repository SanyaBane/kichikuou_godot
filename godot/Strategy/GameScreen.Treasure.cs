using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Treasure screen (docs/ui/pickers.md, round V) in the game: shown over <c>Sｱｲﾃﾑ</c> (Option → Interface → New),
/// under the message windows and menus, so the script's refusals and questions show over it as the original's do; a
/// cell's click, a tab, Back and End are the picker's player's clicks through the screen's pointer
/// (<see cref="TreasureRoster"/>). Unattended play clicks through the screen with its own pointer
/// (<see cref="AutoTreasureAsync"/>).
/// </summary>
public partial class GameScreen
{
    private TreasureView _treasure = null!;
    private PointerScript _treasurePointer = new();     // the script's pointer while our Treasure screen shows

    /// <summary>The pointer the screen's clicks go to: unattended play's own, else the screen's.</summary>
    private PointerScript TreasurePointer => _options.Auto ? _pointer : _treasurePointer;

    private void ReadyTreasure(FacePictures faces)
    {
        _treasure = new TreasureView(GodotGlyphRenderer.Chosen, faces);
        _treasure.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _treasure.PickPressed += n => TreasureGesture(p => TreasureRoster.Pick(n, p));
        _treasure.TurnPressed += () => TreasureGesture(TreasureRoster.Turn);
        _treasure.BackPressed += () => TreasureGesture(TreasureRoster.Back);
        _treasure.EndPressed += () => TreasureGesture(TreasureRoster.End);
        _content.AddChild(_treasure);
        _content.MoveChild(_treasure, _messageView.GetIndex());        // the script's windows and menus over it
    }

    /// <summary>
    /// Shows our Treasure screen while <c>Sｱｲﾃﾑ</c> is open with Option → Interface on New; places it over the picture and
    /// fills it from GameState. While a message window is up (a refusal's key wait) the clicks pass through it to the
    /// window. Returns whether it shows.
    /// </summary>
    private bool UpdateTreasureScreen()
    {
        bool show = InterfaceChoice.New && _presenter.Picker is { Kind: PickerKind.Items } && !SkippingScene && _presenter.State is not null;
        if (show != _treasure.Visible)
        {
            _treasure.Visible = show;
            _treasurePointer = new PointerScript();
            var (x, y) = TreasureRoster.Rest;
            _treasurePointer.Move(x, y);                                // then where the last click was, as the player's pointer
        }
        if (!show)
            return false;
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        _treasure.MouseFilter = _presenter.MessageWindowShown ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
        _treasure.Busy = TreasurePointer.Pending > 0;
        _treasure.Place(_view.Position + (size - drawn) / 2, scale * Surface.Scale);
        _treasure.Refresh(_presenter.State!);
        return true;
    }

    /// <summary>The script reads the pointer over our screen: the screen's clicks, else at rest where the last one was.</summary>
    private MouseState TreasureRead() => _treasurePointer.Next(out _);

    /// <summary>A click of the screen for the picker, unless one is still on its way.</summary>
    private void TreasureGesture(Action<PointerScript> gesture)
    {
        if (TreasurePointer.Pending > 0)
            return;
        gesture(TreasurePointer);
    }

    // ---------------------------------------------------------------- unattended play

    private PickerView? _autoTreasureFrom;
    private Queue<Func<Task>> _autoTreasureSteps = new();

    /// <summary>
    /// Unattended play on our Treasure screen: <see cref="AutoTreasureSteps"/> one at a time while no click is on its way,
    /// then End. False when the screen does not show (the original picker is played by <see cref="AutoItemSteps"/>).
    /// </summary>
    private async Task<bool> AutoTreasureAsync()
    {
        if (_presenter.Picker is not { Kind: PickerKind.Items } picker || !UpdateTreasureScreen() || _presenter.State is not { } s)
            return false;
        if (_pointer.Pending > 0)
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
    /// What the unattended player does on our Treasure screen, leaving the items where they were: the first character at
    /// status 1 or 2 holding an item that moves is picked ("Which item?"), the pick dropped with Back; in the items' view his
    /// item is clicked with no pick (the script asks, the first answer, Yes, takes it off), then he is picked and the item
    /// given back. In a window with screen dumps on: <c>treasure</c>, <c>treasure-hover</c>, <c>treasure-which-item</c>,
    /// <c>treasure-items</c>, <c>treasure-unequipped</c>, <c>treasure-equipped</c>. With nobody holding such an item,
    /// only the first dumps, then End.
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
            UpdateTreasureScreen();                                 // a click's result: the step runs once the script read it
            await AutoDumpAsync(name);
        });
        void TurnTo(int view) => Act(() =>
        {
            if (_presenter.State is { } now && TreasureRoster.View(now) != view)
                TreasureRoster.Turn(_pointer);
        });
        int unit = Enumerable.Range(1, 50).FirstOrDefault(n => s.Units[n].Status.Value is 1 or 2
            && s.Units[n].Item.Value is var item && item != 0 && !TreasureRoster.Fixed(item));
        int held = unit == 0 ? 0 : s.Units[unit].Item.Value;
        GD.Print($"Kichikuou: --auto opens our Treasure screen (week {s.Week.Value}; unit {unit}, item {held}).");
        Dump("treasure");
        if (unit == 0)
            return steps;
        TurnTo(0);
        Act(() => _treasure.ShowHover(unit));
        Dump("treasure-hover");
        Act(() =>
        {
            _treasure.ShowHover(0);
            TreasureRoster.Pick(unit, _pointer);
        });
        Dump("treasure-which-item");
        Act(() => TreasureRoster.Back(_pointer));                   // the pick dropped: the characters again
        TurnTo(1);
        Act(() => _treasure.ShowHover(held));
        Dump("treasure-items");
        Act(() =>
        {
            _treasure.ShowHover(0);
            TreasureRoster.Pick(held, _pointer);                    // asked; the menu's first answer is Yes
        });
        Dump("treasure-unequipped");
        TurnTo(0);
        Act(() => TreasureRoster.Pick(unit, _pointer));
        Act(() => TreasureRoster.Pick(held, _pointer));
        Dump("treasure-equipped");
        Act(() =>
        {
            if (_presenter.State is { } now && now.Units[unit].Item.Value != held)
                GD.PushError($"Kichikuou: --auto did not give item {held} back to unit {unit}.");
        });
        return steps;
    }
}
