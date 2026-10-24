using Godot;
using Kichikuou.Original.Content.Strategy;
using MouseButton = Godot.MouseButton;

namespace Kichikuou.Game.Strategy;

// The touch mode's Skip and Log buttons (owner, 2026-10-24; docs/ui/touch.md, "The Skip and Log buttons"): the menu
// bar's MsgSkip and the wheel's message log, which a finger cannot reach, as buttons at the bottom left.
public partial class GameScreen
{
    private TouchPill _skipButton = null!, _logButton = null!;
    private bool _autoSkipTouched;                      // unattended play's taps on them done (once a launch)

    private void ReadySkipButtons()
    {
        _skipButton = new TouchPill(TouchPill.Icon.Skip, "Skip");
        _skipButton.Tapped += SkipTapped;
        _skipButton.Forwarded += StripInput;
        _content.AddChild(_skipButton);
        _logButton = new TouchPill(TouchPill.Icon.Log, "Log");
        _logButton.Tapped += LogTapped;
        _logButton.Forwarded += StripInput;
        _content.AddChild(_logButton);
    }

    /// <summary>Skip tapped: MsgSkip on or off, as the menu bar's item and F1 turn it.</summary>
    private void SkipTapped()
    {
        if (_skip.Enabled)
            _skip.Toggle();
    }

    /// <summary>Log tapped: the message log, as the wheel up opens it.</summary>
    private void LogTapped()
    {
        if (LogReachable)
            OpenLog();
    }

    /// <summary>Whether the message log opens now: the player's key is awaited (<see cref="CanOpenLog"/>, unattended play's taps included).</summary>
    private bool LogReachable => _keyWaiting && !_modal && _logView is null && _presenter.Log.Pages.Count > 0;

    /// <summary>
    /// Shows the buttons in the touch mode: in the scenes Skip and Log while a message window or a key wait is up — and
    /// Skip while MsgSkip is on, to turn it off again —, never under a menu or over our own screens; on our battle screen
    /// Skip alone, the whole battle. Their places follow the message window (<see cref="SkipButtons"/>).
    /// </summary>
    private void UpdateSkipButtons()
    {
        bool battle = BattleScreenShown;
        bool scene = !battle && _presenter.Menu is null
            && (_keyWaiting || _messageView.Window is not null || _presenter.MessageWindowShown || _skip.Activated);
        bool show = TouchMode.On && _presenter.State is not null && !_modal && !ViewHidden && !EnemyAttackShown
            && !_commandView.Visible && !_provinceView.Visible && !_saveView.Visible && !_sceneMenuView.Visible
            && (battle || scene);
        _skipButton.Visible = show;
        _logButton.Visible = show && !battle;
        if (!show)
            return;
        _skipButton.On = _skip.Activated;
        _skipButton.Disabled = !_skip.Enabled;
        _skipButton.Label = _skip.Activated ? "Skip: ON" : "Skip";
        var area = PictureArea();
        float k = area.Size.X / 1280;
        Rect2 At(SkipButtons.Box b) => new(area.Position + new Vector2(b.X, b.Y) * k, new Vector2(b.W, b.H) * k);
        if (battle)
        {
            _skipButton.Place(At(SkipButtons.Battle), k, 20);
            return;
        }
        var (skip, log) = SkipButtons.Scene(_messageView.Window?.Panel);
        _skipButton.Place(At(skip), k, 24);
        _logButton.Place(At(log), k, 24);
        _logButton.Disabled = !LogReachable;
    }

    /// <summary>
    /// Unattended play in the touch mode, once a launch (in a window), at a key wait of a scene with our message window
    /// once something is logged (the week-18 save's harem scene, <c>--harem</c>): the buttons (dump "touch-skip"), a tap on
    /// Log opens the message log ("touch-log"), a tap off its frame closes it; a tap on Skip turns MsgSkip on ("touch-skip-on"), another
    /// off — the presses and releases through Godot's input.
    /// Anything else is an error.
    /// </summary>
    private async Task AutoSkipTouchAsync()
    {
        _autoSkipTouched = true;
        UpdateSkipButtons();
        await NextFrameAsync();
        if (!_skipButton.Visible || !_logButton.Visible || _logButton.Disabled)
            throw new InvalidOperationException("--auto found the touch mode's Skip and Log buttons hidden or Log greyed at a key wait.");
        await AutoDumpAsync("touch-skip");
        await TapAsync(_logButton.GetGlobalRect().GetCenter());
        if (_logView is null)
            throw new InvalidOperationException("--auto's tap on Log opened no message log.");
        for (int i = 0; i < 20; i++)
            await FrameAsync();                                         // the log in place
        Shot("touch-log");
        await FrameAsync();
        var area = PictureArea();
        await TapAsync(area.Position + new Vector2(100, 400) * (area.Size.X / 1280));     // left of the log's frame
        if (_logView is not null)
            throw new InvalidOperationException("--auto's tap off the message log's frame did not close it.");
        await NextFrameAsync();
        await TapAsync(_skipButton.GetGlobalRect().GetCenter());
        if (!_skip.Activated)
            throw new InvalidOperationException("--auto's tap on Skip did not turn MsgSkip on.");
        UpdateSkipButtons();
        await NextFrameAsync();
        await AutoDumpAsync("touch-skip-on");
        await TapAsync(_skipButton.GetGlobalRect().GetCenter());
        if (_skip.Activated)
            throw new InvalidOperationException("--auto's second tap on Skip did not turn MsgSkip off.");
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
