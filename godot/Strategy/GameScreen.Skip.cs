using Godot;
using Kichikuou.Original.Content.Strategy;
using MouseButton = Godot.MouseButton;

namespace Kichikuou.Game.Strategy;

// The touch mode's Skip and Log buttons (owner, 2026-10-24; docs/ui/touch.md, "The Skip and Log buttons" and "The dialogue
// buttons, round 2"): the menu bar's MsgSkip and the wheel's message log, which a finger cannot reach — in the scenes and on
// the waiting map (the owner's L1, 2026-10-26; "The message log from the map, round L") a drawer at the screen's left edge,
// in our battle screen Skip alone under the cards.
public partial class GameScreen
{
    private TouchPill _skipButton = null!;              // the battle's
    private SkipDrawer _drawer = null!;                 // the scenes'
    private bool _autoSkipTouched;                      // unattended play's taps on them done (once a launch)

    private void ReadySkipButtons()
    {
        _skipButton = new TouchPill(TouchPill.Icon.Skip, "Skip");
        _skipButton.Tapped += SkipTapped;
        _skipButton.Forwarded += StripInput;
        _content.AddChild(_skipButton);
        _drawer = new SkipDrawer();
        _drawer.SkipTapped += SkipTapped;
        _drawer.LogTapped += LogTapped;
        _drawer.Forwarded += StripInput;
        _content.AddChild(_drawer);
    }

    /// <summary>Skip tapped: MsgSkip on or off, as the menu bar's item and F1 turn it.</summary>
    private void SkipTapped()
    {
        if (_skip.Enabled)
            _skip.Toggle();
    }

    /// <summary>Log tapped: the message log, as the wheel up opens it (<see cref="LogOpens"/>, unattended play's taps included).</summary>
    private void LogTapped()
    {
        if (LogOpens)
            OpenLog();
    }

    /// <summary>
    /// Shows the buttons in the touch mode: in the scenes the drawer while a message window or a key wait is up — and
    /// while MsgSkip is on, to turn it off again —, and on <c>COMMAND</c>'s waiting map (<see cref="MapLogWait"/>; the
    /// owner's L1), never under a menu or over our own screens; on our battle screen Skip alone, the whole battle. A window
    /// that would cover the drawer (131) is drawn smaller beside it (<see cref="SkipButtons"/>). On the phone held
    /// landscape the drawer stands in the left side, out of the frame, and shows at the week report too (LW1).
    /// </summary>
    private void UpdateSkipButtons()
    {
        bool battle = BattleScreenShown;
        bool scene = !battle && _presenter.Menu is null
            && (_keyWaiting || _messageView.Window is not null || _presenter.MessageWindowShown || _skip.Activated || MapLogWait
                || PhoneWeekReport);
        bool show = TouchMode.On && _presenter.State is not null && !_modal && !ViewHidden && !EnemyAttackShown
            && !_commandView.Visible && !_provinceView.Visible && !_saveView.Visible && !_sceneMenuView.Visible && !_ladderView.Visible
            && (battle || scene);
        _skipButton.Visible = show && battle;
        _drawer.Visible = show && !battle;
        if (!show)
            return;
        var area = PictureArea();
        if (battle)
        {
            _skipButton.On = _skip.Activated;
            _skipButton.Disabled = !_skip.Enabled;
            _skipButton.Label = _skip.Activated ? "Skip: ON" : "Skip";
            float k = area.Size.X / 1280;
            var b = SkipButtons.Battle;
            _skipButton.Place(new Rect2(area.Position + new Vector2(b.X, b.Y) * k, new Vector2(b.W, b.H) * k), k, 20);
            return;
        }
        _drawer.SkipOn = _skip.Activated;
        _drawer.SkipDisabled = !_skip.Enabled;
        _drawer.LogDisabled = !LogReachable;
        _drawer.Place(area);
    }

    /// <summary>
    /// Unattended play in the touch mode, once a launch (in a window), at a key wait of a scene with our message window
    /// once something is logged (the week-18 save's harem scene, <c>--harem</c>): the buttons (dump "touch-skip"), a tap on
    /// Log opens the message log ("touch-log"), a tap off its frame closes it; a tap on Skip turns MsgSkip on ("touch-skip-on"), another
    /// off — the presses and releases through Godot's input, at the drawer's cells; on the phone held landscape then the
    /// Menu sheet's taps (<see cref="AutoPhoneMenuAsync"/>). Anything else is an error.
    /// </summary>
    private async Task AutoSkipTouchAsync()
    {
        _autoSkipTouched = true;
        UpdateSkipButtons();
        await NextFrameAsync();
        if (!_drawer.Visible || _drawer.LogDisabled)
            throw new InvalidOperationException("--auto found the touch mode's Skip and Log drawer hidden or Log greyed at a key wait.");
        await AutoDumpAsync("touch-skip");
        await TapAsync(_drawer.LogCentre);
        if (_logView is null)
            throw new InvalidOperationException("--auto's tap on Log opened no message log.");
        for (int i = 0; i < 20; i++)
            await FrameAsync();                                         // the log in place
        Shot("touch-log");
        await FrameAsync();
        await TapAsync(_logView!.OutsidePoint);                                   // left of the log's frame (on the phone, its paper)
        if (_logView is not null)
            throw new InvalidOperationException("--auto's tap off the message log's frame did not close it.");
        await NextFrameAsync();
        await TapAsync(_drawer.SkipCentre);
        if (!_skip.Activated)
            throw new InvalidOperationException("--auto's tap on Skip did not turn MsgSkip on.");
        UpdateSkipButtons();
        await NextFrameAsync();
        await AutoDumpAsync("touch-skip-on");
        await TapAsync(_drawer.SkipCentre);
        if (_skip.Activated)
            throw new InvalidOperationException("--auto's second tap on Skip did not turn MsgSkip off.");
        if (Device.Layout == Original.Content.Devices.DeviceLayout.PhoneLandscape)
        {
            UpdatePhone();
            await FrameAsync();
            await AutoPhoneMenuAsync(TapAsync);
        }
        if (_input.PeekInput() != 0)
            throw new InvalidOperationException("--auto's taps on Skip and Log reached the script's input.");
        GD.Print("Kichikuou: --auto taps the touch mode's Log and Skip buttons.");

        // Frames that pass while the log, a modal view, is open (NextFrameAsync waits for its close).
        async Task FrameAsync() => await ToSignal(_tree, SceneTree.SignalName.ProcessFrame);

        async Task TapAsync(Vector2 at)
        {
            SendPointerMotion(at);
            await FrameAsync();
            foreach (bool pressed in (bool[])[true, false])
            {
                SendPointerButton(at, MouseButton.Left, pressed);
                await FrameAsync();
            }
        }
    }
}
