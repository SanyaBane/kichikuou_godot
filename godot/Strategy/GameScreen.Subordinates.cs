using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Our own Subordinates screen (docs/ui/subordinates.md) in the game: shown over <c>Sﾌﾞｶ</c> and while one of
/// its orders runs (our interface); an order's inputs reach the script through the screen's
/// pointer, its menus and key waits are answered here (<see cref="SubordinateOrder"/>).
/// </summary>
public partial class GameScreen
{
    private SubordinatesView _subordinates = null!;
    private PointerScript _subPointer = new();          // the script's pointer while our Subordinates screen shows
    private SubordinateOrder? _order;                   // the order under way, or the last one
    private bool _subordinatesDumped;

    /// <summary>The pointer an order's gestures go to: unattended play's own, else the screen's.</summary>
    private PointerScript SubordinatesPointer => _options.Auto ? _pointer : _subPointer;

    private bool OrderRunning => _order is { Running: true };

    private void ReadySubordinates(FacePictures faces)
    {
        _subordinates = new SubordinatesView(GodotGlyphRenderer.Chosen, faces);
        _subordinates.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _subordinates.OrderPressed += SubordinateOrderPressed;
        _subordinates.EndPressed += SubordinatesEndPressed;
        _content.AddChild(_subordinates);                       // over every other view: the order's way runs under it
    }

    /// <summary>
    /// Shows our Subordinates screen while <c>Sﾌﾞｶ</c> is open with our interface on (not while a
    /// message window is up over it) and while an order runs; places it over the picture and fills it from
    /// GameState. Returns whether it shows.
    /// </summary>
    private bool UpdateSubordinatesScreen(double delta = 0)
    {
        bool running = OrderRunning;
        bool ours = InterfaceChoice.New && (_presenter.Picker is { Kind: PickerKind.Subordinates } || running);
        bool show = ours && (running || !_presenter.MessageWindowShown) && !SkippingScene && _presenter.State is not null;
        if (show != _subordinates.Visible)
        {
            _subordinates.Visible = show;
            if (!running)
                _subPointer = new PointerScript();
            if (show)
                _subordinates.LoadCrops();
        }
        if (!show)
            return false;
        var size = _view.Size;
        float scale = Math.Min(size.X / (Surface.ScreenWidth * Surface.Scale), size.Y / (Surface.ScreenHeight * Surface.Scale));
        var drawn = new Vector2(Surface.ScreenWidth, Surface.ScreenHeight) * Surface.Scale * scale;
        _subordinates.Busy = running || SubordinatesPointer.Pending > 0;
        _subordinates.Place(_view.Position + (size - drawn) / 2, scale * Surface.Scale);
        _subordinates.Refresh(_presenter.State!, delta, _pickerMarks?.Talks);
        return true;
    }

    /// <summary>The script reads the pointer over our screen: the order's gestures, else at rest on the empty 51st cell (the script hovers nothing).</summary>
    private MouseState SubordinatesRead()
    {
        if (_subPointer.Pending == 0)
        {
            var (x, y) = DeploymentTable.PointerAt(0);
            _subPointer.Move(x, y);
        }
        return _subPointer.Next(out _);
    }

    /// <summary>An order's button: the order starts with the click on the unit's cell, unless one is under way.</summary>
    private void SubordinateOrderPressed(SubordinateOrderKind kind, int unit, int step)
    {
        var pointer = SubordinatesPointer;
        if (OrderRunning || pointer.Pending > 0 || _presenter.State is not { } s)
            return;
        _order = SubordinateOrder.Start(s, kind, unit, step, pointer);
        GD.Print($"Kichikuou: Subordinates order {kind} for unit {unit}{(kind == SubordinateOrderKind.AddTroops ? $" at step {step}" : "")} (week {s.Week.Value}).");
    }

    /// <summary>End, or a right press on the screen: a click on the bar's End.</summary>
    private void SubordinatesEndPressed()
    {
        var pointer = SubordinatesPointer;
        if (OrderRunning || pointer.Pending > 0)
            return;
        var (x, y) = GesturePoints.PickerButton(3);
        pointer.Click(x, y);
    }

    /// <summary>A menu while an order runs: the order's answer, or null (the menu is the player's).</summary>
    private int? SubordinatesChoice(IReadOnlyList<SceneMenuItem> items) => OrderRunning ? _order!.Choose(items) : null;

    /// <summary>Whether a key wait is an order's, answered at once.</summary>
    private bool SubordinatesAnswersKey() => _order is { } order && order.AnswerKey();

    /// <summary>
    /// Unattended play's replenish through our screen: Replenish pressed, its question (<c>subordinates-question</c> in a
    /// window with screen dumps on) answered Yes.
    /// </summary>
    private async void AutoReplenishThroughScreen(int unit)
    {
        _subordinates.Press(SubordinateOrderKind.Replenish, unit);
        if (!_subordinates.Asking)
        {
            GD.PushError($"Kichikuou: --auto found no question before replenishing unit {unit}.");
            return;
        }
        if (DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
            await AutoDumpAsync("subordinates-question");
        _subordinates.Answer(true);
    }

    /// <summary>
    /// The script's events for an order: <c>Sﾌﾞｶ</c> opening or closing, COMMAND's map wait (the right click
    /// that opens the command again); the plate once a troop order is over. Unattended play dumps the screen
    /// as it opens and after its order.
    /// </summary>
    private async Task SubordinatesEventAsync(ScriptEvent scriptEvent)
    {
        if (_presenter.State is not { } s)
            return;
        bool wasRunning = OrderRunning;
        if (scriptEvent is PickerScreenEvent picker)
            _order?.OnPicker(picker);
        if (scriptEvent is MapScreenEvent { Waiting: true } && OrderRunning && _order!.OnMapWait())
        {
            var (x, y) = SubordinateOrder.RightClickAt;
            SubordinatesPointer.RightClick(x, y);
            _autoMapWait = false;                               // unattended play: the map wait is the order's
        }
        bool dumps = _options.Auto && DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null;
        if (wasRunning && !OrderRunning && _order!.Result(s) is { } result)
        {
            GD.Print($"Kichikuou: Subordinates order done, the command open again: {result.Title} {result.TroopsLine} ({result.Gain}), cost {result.CostLine}, budget {result.BudgetLine}.");
            UpdateSubordinatesScreen();
            _subordinates.ShowResult(result);
            if (dumps)
                await AutoDumpAsync("subordinates-done");
            if (_options.Auto)
                _subordinates.ShowResult(null);                 // the plate waits for a click: unattended play's
        }
        else if (scriptEvent is PickerScreenEvent { Kind: PickerKind.Subordinates, Open: true } && !OrderRunning && dumps && !_subordinatesDumped)
        {
            _subordinatesDumped = true;
            UpdateSubordinatesScreen();
            await AutoDumpAsync("subordinates");
            if (TouchMode.On)
            {
                await AutoHeaderTouchAsync(_subordinates.Table);
                await AutoTableDragAsync(_subordinates.Table);
            }
        }
    }
}
