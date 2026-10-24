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
    /// through Godot's input, so the script's reads and our own screens see it as a mouse's.
    /// </summary>
    private void BackAsRightClick() =>
        _ = AutoViewClickAsync(TouchMode.Pointer(GetViewport()), MouseButton.Right);

    /// <summary>
    /// The map's Commands button tapped (the touch mode, owner's M3): the right click that opens the command panel, at
    /// the button, queued for the script as a plate's click is (<see cref="MarkerPressed"/>).
    /// </summary>
    private void CommandsTapped()
    {
        if ((!_options.Auto || _autoCommandsTapping) && _markerPointer.Pending == 0 && _markers.Visible)
        {
            var at = ToScript(_content.Position + _commandsButton.Centre);
            _markerPointer.RightClick(at.X, at.Y);
        }
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
    /// "touch-rails"), a tap on another enemy card that an attack reaches ("touch-target"), then a tap on its Melee rail
    /// button attacks. Anything else is an error.
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
    /// Unattended play in the touch mode, once a launch (in a window): on the Subordinates screen's table a long press on
    /// T.Atk's header shows its tip (dump "touch-header-tip") and sorts nothing, a tap on it sorts by it
    /// ("touch-header-sorted"), a tap on №'s sorts back — through Godot's input. Anything else is an error.
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
        async Task TapAsync(Vector2 at)
        {
            SendPointerMotion(at);
            await NextFrameAsync();
            foreach (bool pressed in (bool[])[true, false])
            {
                SendPointerButton(at, MouseButton.Left, pressed);
                await NextFrameAsync();
            }
        }
        await TapAsync(atk);
        if (table.SortColumn != DeploymentColumn.TroopAtk || table.HeaderTipShown is not null)
            throw new InvalidOperationException($"--auto's tap on T.Atk's header sorted by {table.SortColumn}, its tip {table.HeaderTipShown ?? "hidden"}.");
        await AutoDumpAsync("touch-header-sorted");
        await TapAsync(number);
        if (table.SortColumn != DeploymentColumn.Number)
            throw new InvalidOperationException($"--auto's tap on №'s header sorted by {table.SortColumn}.");
        GD.Print("Kichikuou: --auto long-presses and taps the Subordinates table's headers.");
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
