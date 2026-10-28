using Godot;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Replenish on our deployment screen (owner's decision 27, docs/ui/deployment-table.md, "Replenish on the deployment
/// screen", mock-up A1): a Refill button in the table asks first in a callout at its cell (the Subordinates screen's C3);
/// Yes sets the request and clicks, which <c>Sﾌﾞﾀｲ</c> answers by replenishing and summing its party again
/// (<see cref="DeploymentReplenish"/>); then the Subordinates screen's plate (R4) says what it did.
/// </summary>
public partial class GameScreen
{
    private OriginalScriptContext? _script;             // the session's: the request goes to it
    private TreasureCallout _refillCallout = null!;
    private OrderResultPlate _refillPlate = null!;
    private DeploymentReplenish? _replenish;            // the Replenish asked for, until Sﾌﾞﾀｲ has taken it
    private bool _autoRefillDone;

    private void ReadyDeploymentReplenish(FacePictures faces)
    {
        _table.RefillPressed += RefillPressed;
        _refillCallout = new TreasureCallout(GodotGlyphRenderer.Chosen);
        _content.AddChild(_refillCallout);
        _refillPlate = new OrderResultPlate(GodotGlyphRenderer.Chosen, faces);
        _content.AddChild(_refillPlate);
    }

    /// <summary>Whether a Replenish is asked about or under way: the table's other presses wait.</summary>
    private bool RefillBusy => _refillCallout.MenuOpen || _replenish is not null;

    /// <summary>
    /// Places the callout and the plate over the picture while our deployment screen shows (else hides them: a question
    /// left open is a No); once <c>Sﾌﾞﾀｲ</c> has taken the request, the plate.
    /// </summary>
    private void UpdateDeploymentReplenish(bool shown, Rect2 picture)
    {
        if (!shown)
        {
            _refillCallout.Hide();
            _refillPlate.Show(null);
            _table.AskedRefill = 0;
            _replenish = null;
            return;
        }
        _refillCallout.PlaceOnDevice(picture);                  // on the phone at its sizes over the whole screen
        _refillPlate.Place(picture);
        if (_replenish is not { Done: true } done || _presenter.State is not { } s)
            return;
        _replenish = null;
        var result = done.Result(s);
        GD.Print(result is null
            ? $"Kichikuou: the deployment screen's Replenish of unit {done.Unit} was not done."
            : $"Kichikuou: the deployment screen replenished {result.Name}: {result.TroopsLine} ({result.Gain}), cost {result.CostLine}, budget {result.BudgetLine}.");
        _refillPlate.Show(result);
        if (_options.Auto)
            AutoCloseRefillPlateAsync(result is not null);
    }

    /// <summary>Unattended play: the plate dumped in a window with screen dumps on (<c>deployment-refill-done</c>), then closed as by a click.</summary>
    private async void AutoCloseRefillPlateAsync(bool done)
    {
        if (done && DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
            await AutoDumpAsync("deployment-refill-done");
        _refillPlate.Show(null);
    }

    /// <summary>
    /// A Refill button pressed (by the mouse, or by unattended play): the question in a callout at its cell, the button
    /// lit; Yes asks <c>Sﾌﾞﾀｲ</c> for it, No or a right click leaves it. Nothing reaches the script before Yes.
    /// </summary>
    private async void RefillPressed(int unit)
    {
        var pointer = _options.Auto ? _pointer : _tablePointer;
        if (RefillBusy || pointer.Pending > 0 || _wantedOrder is not null || _presenter.State is not { } s
            || DeploymentTable.Rows(s).FirstOrDefault(r => r.Unit == unit) is not { CanRefill: true } row
            || _table.RefillCellRect(unit) is not { } cell)
            return;
        var screenCell = _refillCallout.FromGlobal(cell);
        _table.AskedRefill = unit;
        _refillCallout.Show(DeploymentTable.ReplenishQuestion(row), screenCell, waiting: false);
        _refillCallout.OpenMenu(["Yes", "No"]);
        int answer;
        try
        {
            answer = await _refillCallout.ChooseAsync();
        }
        catch (OperationCanceledException)
        {
            answer = 0;                                         // the screen closed under the question
        }
        _refillCallout.Hide();
        _table.AskedRefill = 0;
        pointer = _options.Auto ? _pointer : _tablePointer;     // the screen may have started again meanwhile
        if (answer != 1 || !_table.Visible || _script is null || pointer.Pending > 0 || _wantedOrder is not null)
            return;
        _replenish = DeploymentReplenish.Start(_script, unit, pointer);
        if (_replenish is not null)
            GD.Print($"Kichikuou: the deployment screen asks to replenish unit {unit} (week {s.Week.Value}).");
    }

    /// <summary>
    /// Unattended play with <c>--refill</c> and our screen: once a launch, the unit with the fewest troops of its maximum
    /// that can be replenished, through its Refill button — the question dumped in a window with screen dumps on
    /// (<c>deployment-refill-question</c>), answered Yes; the plate after it is <c>deployment-refill-done</c>. Returns
    /// whether the pointer is the Replenish's now (the deployment's own steps wait).
    /// </summary>
    private bool AutoRefill(GameState s)
    {
        if (RefillBusy || _refillPlate.Showing)
            return true;
        if (!_options.AutoRefill || _autoRefillDone || !_table.Visible)
            return false;
        int unit = DeploymentTable.Rows(s).Where(r => r.CanRefill)
            .OrderBy(r => r.Troops / (double)Math.Max(1, r.MaxTroops)).ThenBy(r => r.Unit).Select(r => r.Unit).FirstOrDefault();
        _autoRefillDone = true;
        if (unit == 0)
        {
            GD.Print($"Kichikuou: --refill found no unit to replenish in the deployment table (week {s.Week.Value}).");
            return false;
        }
        GD.Print($"Kichikuou: --refill replenishes unit {unit} through the deployment table (week {s.Week.Value}).");
        AutoRefillAsync(unit);
        return true;
    }

    private async void AutoRefillAsync(int unit)
    {
        if (!_table.CanRefill(unit))
            _panels.ShowTab(LedgerTab.Overview, remember: false);   // the Refill buttons are the Overview's
        _table.PressRefill(unit);
        if (!_refillCallout.MenuOpen)
        {
            GD.PushError($"Kichikuou: --refill found no question before replenishing unit {unit}.");
            return;
        }
        if (DisplayServer.GetName() != "headless" && _options.ScreenshotDirectory is not null)
            await AutoDumpAsync("deployment-refill-question");
        _refillCallout.Choose(1);
    }
}
