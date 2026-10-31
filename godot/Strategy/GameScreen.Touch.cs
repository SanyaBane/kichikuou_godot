using Godot;
using Kichikuou.Engine.Sys3;
using Kichikuou.Game.Battles;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Content.Strategy;
using MouseButton = Godot.MouseButton;

namespace Kichikuou.Game.Strategy;

// A touch screen's way to the right button (docs/backlog.md, "Android") and the touch mode's parts (docs/ui/touch.md,
// "The tablet"): the battle's preselected target.
public partial class GameScreen
{
    private readonly TouchTargets _touchTargets = new();   // each unit's last attacked enemy, for this battle
    private int _touchBattle = -1;                          // the battle they are of (GamePresenter.Battles)
    private bool _autoTouched;                              // unattended play's touch exercise done (once a launch)
    private bool _autoCommandsTap, _autoCommandsTapped;     // unattended play's tap on the map's Commands button: wanted, done
    private bool _autoCommandsTapping;                      // that tap on its way

    /// <summary>How long a finger stays on a battle card to open its menu.</summary>
    private const double LongPressSeconds = 0.5;

    /// <summary>How far the finger may wander meanwhile (this control's pixels): further, it is a drag.</summary>
    private const float LongPressSlop = 12;

    private Vector2? _cardTouch;        // where a finger came down on a card, its press held back
    private int _cardTouches;           // the touches counted: a timer acts only for its own
    private bool _longPressed;          // the finger still down after its long press opened a menu

    /// <summary>
    /// Android's Back (the button or the gesture; project.godot keeps it from quitting) is a right click where the
    /// last touch left the pointer: the original cancels with the right button, and a touch screen has none. It goes
    /// through Godot's input, so the script's reads and our own screens see it as a mouse's. While a save's comment is
    /// written it leaves the comment as it was (the PC's Esc), not the save screen.
    /// </summary>
    private void BackAsRightClick()
    {
        if (_marginsView is not null)
        {
            _marginsView.Close();                       // the screen margins' calibration: back to the options
            return;
        }
        if (_saveView.Visible && _saveView.Editing)
        {
            _saveView.CancelEdit();
            return;
        }
        _ = AutoViewClickAsync(TouchMode.Pointer(GetViewport()), MouseButton.Right);
    }

    /// <summary>
    /// The map's Commands button tapped (the touch mode, owner's M3): the right click that opens the command panel, at
    /// the button, queued for the script as a plate's click is (<see cref="MarkerPressed"/>).
    /// </summary>
    private void CommandsTapped()
    {
        if (_commandView.Visible)
        {
            _commandView.Cancel();                                      // the button reads Close over the open panel
            return;
        }
        if ((!_options.Auto || _autoCommandsTapping) && _markerPointer.Pending == 0 && _markers.Visible)
        {
            var at = ToScript(_content.Position + _commandsButton.Centre);
            _markerPointer.RightClick(at.X, at.Y);
        }
    }

    private bool _autoCommandCloses;        // unattended play's taps closing the command panel (Close, outside), once
    private bool _autoProvinceOutside;      // and the province menu (outside), once

    /// <summary>
    /// Unattended play in the touch mode, in a window, at the first command panel: a tap on the map's button reading
    /// Close (dump "touch-close"), then one outside the panel ("touch-outside"); at the first province menu one outside it
    /// ("touch-province-outside") — each press and release through Godot's input, which our view must answer with the
    /// cancel, 0, as a player's tap. The script is then answered with the bot's own choice, so its play goes on as before.
    /// </summary>
    private async Task AutoTouchDismissAsync(bool command)
    {
        if (command)
        {
            _autoCommandCloses = true;
            UpdateMapMarkers();
            if (!_commandsButton.Visible || !_commandsButton.Closes)
                throw new InvalidOperationException("--auto finds the map's button not reading Close over the command panel.");
            await TapAsync(CommandChoice(_commandView.ChooseAsync()), _commandsButton.GetGlobalRect().GetCenter(), "touch-close");
            await TapAsync(CommandChoice(_commandView.ChooseAsync()), _commandView.OutsidePoint, "touch-outside");
        }
        else
        {
            _autoProvinceOutside = true;
            await TapAsync(_provinceView.ChooseAsync(), _provinceView.OutsidePoint, "touch-province-outside");
        }

        async Task TapAsync(Task<int> answer, Vector2 at, string dump)
        {
            await AutoDumpAsync(dump);
            foreach (bool pressed in (bool[])[true, false])
            {
                SendPointerButton(at, MouseButton.Left, pressed);
                await NextFrameAsync();
            }
            if (!answer.IsCompleted || await answer != 0)
                throw new InvalidOperationException($"--auto's tap ({dump}) did not cancel the {(command ? "command panel" : "province menu")}.");
            GD.Print($"Kichikuou: --auto's tap cancels the {(command ? "command panel" : "province menu")} ({dump}).");
        }

        static async Task<int> CommandChoice(Task<(int Choice, SaveList? Opens)> answer) => (await answer).Choice;
    }

    /// <summary>
    /// Unattended play in the touch mode, once a launch (in a window): the command panel opened by a tap on the map's
    /// Commands button instead of the bot's right click — dump "touch-commands", the press and the release through
    /// Godot's input.
    /// </summary>
    private async Task AutoCommandsTapAsync()
    {
        _autoCommandsTap = false;
        var at = _commandsButton.GetGlobalRect().GetCenter();
        SendPointerMotion(at);
        await NextFrameAsync();
        await AutoDumpAsync("touch-commands");
        _autoCommandsTapping = true;
        try
        {
            foreach (bool pressed in (bool[])[true, false])
            {
                SendPointerButton(at, MouseButton.Left, pressed);
                await NextFrameAsync();
            }
        }
        finally
        {
            _autoCommandsTapping = false;
        }
        if (_markerPointer.Pending == 0)
            throw new InvalidOperationException("--auto's tap on the Commands button queued no right click for the script.");
        GD.Print("Kichikuou: --auto taps the map's Commands button.");
    }

    /// <summary>
    /// A long press on a card of our battle screen opens its menu, as a right click does (owner, 2026-10-24): while the
    /// player chooses and no menu is open, a finger on a card holds its press back. Lifted before
    /// <see cref="LongPressSeconds"/>, the press and the release go on as a tap's; held there that long, it is a
    /// right click at the card and its release is dropped; moved away, the press goes on as a drag's. Only the mouse
    /// Godot makes of a touch (<see cref="InputEvent.DeviceIdEmulation"/>): a mouse's left button keeps doing nothing
    /// on our own cards. Returns whether <paramref name="button"/> was taken.
    /// </summary>
    private bool CardTouch(InputEventMouseButton button)
    {
        if (button.Device != InputEvent.DeviceIdEmulation || button.ButtonIndex != MouseButton.Left)
            return false;
        if (!button.Pressed)
        {
            if (_longPressed)
            {
                _longPressed = false;
                return true;                                            // the long press's own release
            }
            ReleaseCardTouch();
            return false;
        }
        var commands = _battleView.Commands;
        if (!commands.Active || commands.MenuOpen || _presenter.State is not { } s
            || _battleView.PlaceAt(button.Position - _content.Position, s) is null)
            return false;
        _cardTouch = button.Position;
        int touch = ++_cardTouches;
        _tree.CreateTimer(LongPressSeconds).Timeout += () =>
        {
            if (touch != _cardTouches || _cardTouch is not { } at)
                return;
            if (!_battleView.Commands.Active)
            {
                ReleaseCardTouch();                                     // the choice ended meanwhile: a plain press
                return;
            }
            _cardTouch = null;
            _longPressed = true;                                        // no vibration (owner, 2026-10-24)
            PointerButton(at, KeySpace, true);
            PointerButton(at, KeySpace, false);
        };
        return true;
    }

    /// <summary>A finger that wandered off its card: the press held back goes on, as a drag's.</summary>
    private void TouchMoved(InputEventMouseMotion motion)
    {
        if (_cardTouch is { } at && motion.Position.DistanceTo(at) > LongPressSlop)
            ReleaseCardTouch();
    }

    /// <summary>
    /// The touch mode's target for the acting unit of <paramref name="model"/> (<see cref="TouchTargets"/>): its last
    /// attacked enemy, else the last any unit attacked, else the first enemy card — an enemy none of its attacks reaches
    /// passed over. The memory is forgotten when a new battle opens.
    /// </summary>
    private BattlePlace? TouchTarget(BattleCommands model)
    {
        if (_touchBattle != _presenter.Battles)
        {
            _touchTargets.Clear();
            _touchBattle = _presenter.Battles;
        }
        var s = _presenter.State!;
        var enemies = Enumerable.Range(1, 4).Select(n => BattleBoard.Record(s, new BattlePlace(BattleSide.Enemy, n))).ToList();
        return _touchTargets.Preselect(BattleBoard.Record(s, model.Acting), enemies,
            n => model.Banners(new BattlePlace(BattleSide.Enemy, n)).Count > 0) is { } slot
            ? new BattlePlace(BattleSide.Enemy, slot)
            : null;
    }

    /// <summary>The player's <paramref name="choice"/>: an attack on an enemy is remembered as the acting unit's target.</summary>
    private void TouchAttacked(BattleCommands model, FigtSelChoice choice)
    {
        var s = _presenter.State!;
        int slot = choice.Argument.Value - 3;
        if (slot is < 1 or > 4)
            return;
        var target = new BattlePlace(BattleSide.Enemy, slot);
        if (model.Banners(target).Any(b => b.Choice == choice))
            _touchTargets.Attacked(BattleBoard.Record(s, model.Acting), BattleBoard.Record(s, target), slot);
    }

    /// <summary>
    /// Unattended play in the touch mode, once a launch (in a window): the rails with the preselected target (dump
    /// "touch-rails"; with the damage forecast, whatever the option, "touch-rails-forecast"), a tap on another enemy card
    /// that an attack reaches ("touch-target"), then a tap on its Melee rail button attacks. Anything else is an error.
    /// </summary>
    private async Task<FigtSelChoice> AutoTouchCommandsAsync(FigtSelMenu menu, IReadOnlyList<Sys3Int> direct)
    {
        _autoTouched = true;
        var model = new BattleCommands(menu);
        var commands = _battleView.Commands;
        _battleButtons = 0;
        _answered = new TaskCompletionSource();
        _presenter.Scene.Choosing = true;
        commands.Begin(model, _presenter.State!, _presenter.Battle.Text, TouchTarget(model));
        try
        {
            GD.Print($"Kichikuou: --auto taps the touch mode's rails (target {commands.Target}).");
            HoverScene();
            _presenter.Invalidate();
            await AutoDumpAsync("touch-rails");
            commands.Forecast = true;                                   // the damage forecast on the rail, its figures waited for
            await commands.ForecastsDone!;
            await NextFrameAsync();
            await AutoDumpAsync("touch-rails-forecast");
            commands.Forecast = null;
            var other = direct.Select(b => new BattlePlace(BattleSide.Enemy, b.Value - 3)).FirstOrDefault(p => p != commands.Target);
            if (other.Slot != 0)
            {
                await AutoClickAsync(BattleBoardView.CardArea(other).Position + new Vector2(220, 75), KeyReturn);
                if (commands.Target != other)
                    throw new InvalidOperationException($"--auto's tap on {other} made {commands.Target} the target.");
                await AutoDumpAsync("touch-target");
            }
            var target = commands.Target ?? throw new InvalidOperationException("--auto found no target on the touch mode's rail.");
            var expected = new FigtSelChoice(FigtSelCommand.Direct, (Sys3Int)(target.Slot + 3));
            if (commands.RailCentre(expected) is not { } melee)
                throw new InvalidOperationException($"--auto found no Melee on the rail for {target}.");
            await AutoClickAsync(melee, KeyReturn);
            if (commands.Result != expected)
                throw new InvalidOperationException($"--auto's tap on the Melee rail button answered {commands.Result}, not {expected}.");
            TouchAttacked(model, expected);
            return expected;
        }
        finally
        {
            commands.End();
            _presenter.Scene.Choosing = false;
            _presenter.Invalidate();
            _input.ReadMouse();
        }
    }

    /// <summary>
    /// Unattended play in the touch mode: a tap on the Subordinates ledger's <paramref name="tab"/> shows it (one shown
    /// already stays) — through Godot's input. Anything else is an error.
    /// </summary>
    private async Task AutoTabTouchAsync(LedgerTab tab)
    {
        await AutoTapAsync(_subordinates.TabCentre(tab));
        if (_subordinates.Tab != tab)
            throw new InvalidOperationException($"--auto's tap on the {LedgerTabs.Title(tab)} tab left {LedgerTabs.Title(_subordinates.Tab)} shown.");
    }

    /// <summary>A finger's tap at <paramref name="at"/> (global coordinates) through Godot's input: moved there, down, up.</summary>
    private async Task AutoTapAsync(Vector2 at)
    {
        SendPointerMotion(at);
        await NextFrameAsync();
        foreach (bool pressed in (bool[])[true, false])
        {
            SendPointerButton(at, MouseButton.Left, pressed);
            await NextFrameAsync();
        }
    }

    /// <summary>
    /// Unattended play in the touch mode, once a launch (in a window): on the Subordinates screen's table (its Troops tab,
    /// <see cref="AutoTabTouchAsync"/>) a long press on T.Atk's header shows its tip (dump "touch-header-tip") and sorts
    /// nothing, a tap on it sorts by it ("touch-header-sorted"), a tap on №'s sorts back — through Godot's input. Anything
    /// else is an error.
    /// </summary>
    private async Task AutoHeaderTouchAsync(DeploymentTableView table)
    {
        var atk = table.HeaderCentre(DeploymentColumn.TroopAtk)
            ?? throw new InvalidOperationException("--auto found T.Atk's header out of sight.");
        var number = table.HeaderCentre(DeploymentColumn.Number)
            ?? throw new InvalidOperationException("--auto found №'s header out of sight.");
        var sort = table.SortColumn;
        SendPointerMotion(atk);
        await NextFrameAsync();
        SendPointerButton(atk, MouseButton.Left, true);
        ulong until = Time.GetTicksMsec() + 3000;
        while (table.HeaderTipShown is null && Time.GetTicksMsec() < until)
            await NextFrameAsync();
        if (table.HeaderTipShown != "Troop attack")
            throw new InvalidOperationException($"--auto's long press on T.Atk's header showed {table.HeaderTipShown ?? "no tip"}.");
        await AutoDumpAsync("touch-header-tip");
        SendPointerButton(atk, MouseButton.Left, false);
        await NextFrameAsync();
        if (table.SortColumn != sort || table.HeaderTipShown is null)
            throw new InvalidOperationException("--auto's long press on T.Atk's header sorted, or its release hid the tip.");
        await AutoTapAsync(atk);
        if (table.SortColumn != DeploymentColumn.TroopAtk || table.HeaderTipShown is not null)
            throw new InvalidOperationException($"--auto's tap on T.Atk's header sorted by {table.SortColumn}, its tip {table.HeaderTipShown ?? "hidden"}.");
        await AutoDumpAsync("touch-header-sorted");
        await AutoTapAsync(number);
        if (table.SortColumn != DeploymentColumn.Number)
            throw new InvalidOperationException($"--auto's tap on №'s header sorted by {table.SortColumn}.");
        GD.Print("Kichikuou: --auto long-presses and taps the Subordinates table's headers.");
    }

    /// <summary>
    /// Unattended play in the touch mode, once a launch (in a window), after <see cref="AutoHeaderTouchAsync"/>: on the
    /// Subordinates screen's table a finger dragged up over the rows and held still before it rises scrolls them down
    /// (dump "touch-table-dragged"), flicks nothing and chooses nothing; dragged left and risen at speed it scrolls the
    /// columns and flicks on after the release ("touch-table-flicked" once the flick stopped); a tap on a row chooses its
    /// unit — through Godot's input. A scroll bar the table does not show is left out; the chosen unit is put back.
    /// Anything else is an error.
    /// </summary>
    private async Task AutoTableDragAsync(DeploymentTableView table)
    {
        var rows = table.RowsArea;
        int chosen = table.Chosen;
        Vector2 At(float x, float y) => rows.Position + rows.Size * new Vector2(x, y);
        var ends = table.ScrollEnds;
        var before = table.ScrollValues;
        await AutoDragAsync(At(0.5f, 0.9f), At(0.5f, 0.1f), 8, 0.25);
        var dragged = table.ScrollValues;
        if (table.Flinging || table.Chosen != chosen || dragged.X != before.X)
            throw new InvalidOperationException($"--auto's drag up, held still before it rose, flicked ({table.Flinging}), chose unit {table.Chosen} (not {chosen}) or scrolled from {before} to {dragged}.");
        if (ends.Y > before.Y && dragged.Y <= before.Y)
            throw new InvalidOperationException($"--auto's drag up did not scroll the rows down ({before.Y} of {ends.Y}).");
        string vertical = ends.Y > 0 ? $"rows down {dragged.Y - before.Y:0} of {ends.Y:0} px" : "no vertical scroll bar";
        await AutoDumpAsync("touch-table-dragged");
        await AutoDragAsync(At(0.8f, 0.5f), At(0.3f, 0.5f), 4, 0);
        var released = table.ScrollValues;
        bool flicked = table.Flinging;
        ulong stop = Time.GetTicksMsec() + 4000;
        while (table.Flinging && Time.GetTicksMsec() < stop)
            await NextFrameAsync();
        var flung = table.ScrollValues;
        if (table.Flinging || table.Chosen != chosen || flung.Y != dragged.Y)
            throw new InvalidOperationException($"--auto's flick left never stopped ({table.Flinging}), chose unit {table.Chosen} (not {chosen}) or scrolled the rows ({dragged.Y} to {flung.Y}).");
        if (ends.X > dragged.X && released.X <= dragged.X)
            throw new InvalidOperationException($"--auto's drag left did not scroll the columns ({dragged.X} of {ends.X}).");
        if (released.X < ends.X && !(flicked && flung.X > released.X))
            throw new InvalidOperationException($"--auto's quick drag left flicked nothing ({released.X} to {flung.X} of {ends.X}).");
        string horizontal = ends.X <= 0 ? "no horizontal scroll bar"
            : $"columns right {released.X - dragged.X:0} px, the flick {flung.X - released.X:0} more, of {ends.X:0}";
        await AutoDumpAsync("touch-table-flicked");
        var tap = new[] { 0.2f, 0.5f, 0.8f }.Select(y => At(0.3f, y)).FirstOrDefault(p => table.UnitAtPoint(p) is var u and > 0 && u != chosen);
        if (tap == default)
            throw new InvalidOperationException("--auto found no row to tap but the chosen unit's.");
        int unit = table.UnitAtPoint(tap);
        foreach (bool pressed in (bool[])[true, false])
        {
            SendPointerButton(tap, MouseButton.Left, pressed);
            await NextFrameAsync();
        }
        if (table.Chosen != unit)
            throw new InvalidOperationException($"--auto's tap on unit {unit}'s row chose unit {table.Chosen}.");
        table.Chosen = chosen;
        GD.Print($"Kichikuou: --auto drags the Subordinates table: {vertical}; {horizontal}; a tap chose unit {unit}.");
    }

    /// <summary>
    /// A finger's drag through Godot's input (unattended play): down at <paramref name="from"/>, moved to
    /// <paramref name="to"/> in <paramref name="steps"/> moves, a frame and at least 1/60 s apart (as a touch screen
    /// reports them: a window drawing hundreds of frames a second would make a flick too short to measure), held still
    /// <paramref name="holdSeconds"/> (0: risen at once, a flick), up — global coordinates.
    /// </summary>
    private async Task AutoDragAsync(Vector2 from, Vector2 to, int steps, double holdSeconds)
    {
        async Task TouchFrameAsync()
        {
            ulong next = Time.GetTicksMsec() + 17;
            do
                await NextFrameAsync();
            while (Time.GetTicksMsec() < next);
        }
        SendPointerButton(from, MouseButton.Left, true);
        await TouchFrameAsync();
        for (int i = 1; i <= steps; i++)
        {
            SendPointerDrag(from.Lerp(to, i / (float)steps));
            await TouchFrameAsync();
        }
        ulong until = Time.GetTicksMsec() + (ulong)(holdSeconds * 1000);
        while (Time.GetTicksMsec() < until)
            await NextFrameAsync();
        SendPointerButton(to, MouseButton.Left, false);
        await NextFrameAsync();
    }

    /// <summary>The press held back by <see cref="CardTouch"/>, if any, goes on now.</summary>
    private void ReleaseCardTouch()
    {
        if (_cardTouch is not { } at)
            return;
        _cardTouch = null;
        _cardTouches++;
        PointerButton(at, KeyReturn, true);
    }
}
