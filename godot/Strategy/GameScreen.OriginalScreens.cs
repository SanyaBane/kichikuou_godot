using Godot;

namespace Kichikuou.Game.Strategy;

// The original's own screens on the phone held landscape (docs/ui/phone-build.md, step L9; docs/ui/phone.md, §23 LE1 and
// §26 LXf, LXc, LXm, LXr): the title and the screens it opens, and an ending's report, shown as they are in the frame with
// Menu on top of the left side (Skip and Log at the report's key waits); a tap is the click, a long press the right click.
// Held upright the same (step P8d; §22 PEd, §25 PXf, PXc, PXm, PXr), Menu at the bar's left.
public partial class GameScreen
{
    /// <summary>The title (<c>ｽﾀMENU</c>), its Fortune Check, gallery and music room, and the endings (<c>OVER</c>).</summary>
    private static readonly HashSet<string> OriginalScreens = ["ｽﾀMENU.ADV", "HAPPY_C.ADV", "CG.ADV", "MMODE.ADV", "OVER.ADV"];

    private Vector2? _screenTouch;      // where a finger came down on one of them, its press held back
    private int _screenTouches;         // the touches counted: a timer acts only for its own
    private bool _screenLongPressed;    // the finger still down after its long press clicked right

    /// <summary>Whether the scene running now is one of the original's own screens.</summary>
    private bool OriginalScreenShown => _scenes?.RunningScenes is [.., var file] && OriginalScreens.Contains(file);

    /// <summary>
    /// A finger on the original's own screens on the phone held landscape: its press held back. Lifted before
    /// <see cref="LongPressSeconds"/>, the press and the release go on as a tap's (the click); held there that long, it is a
    /// right click where it is (the title's descriptions) and its release is dropped; moved away, the press goes on. Only
    /// the mouse Godot makes of a touch, as a battle card's (<see cref="CardTouch"/>). Returns whether
    /// <paramref name="button"/> was taken.
    /// </summary>
    private bool ScreenTouch(InputEventMouseButton button)
    {
        if (button.Device != InputEvent.DeviceIdEmulation || button.ButtonIndex != MouseButton.Left)
            return false;
        if (!button.Pressed)
        {
            if (_screenLongPressed)
            {
                _screenLongPressed = false;
                return true;                                            // the long press's own release
            }
            ReleaseScreenTouch();
            return false;
        }
        if (!(PhoneLandscape || PhoneUpright) || !OriginalScreenShown)
            return false;
        _screenTouch = button.Position;
        int touch = ++_screenTouches;
        _tree.CreateTimer(LongPressSeconds).Timeout += () =>
        {
            if (touch != _screenTouches || _screenTouch is not { } at)
                return;
            _screenTouch = null;
            _screenLongPressed = true;
            PointerButton(at, KeySpace, true);
            PointerButton(at, KeySpace, false);
        };
        return true;
    }

    /// <summary>The press held back goes on (a tap lifted, a finger moved away).</summary>
    private void ReleaseScreenTouch()
    {
        if (_screenTouch is not { } at)
            return;
        _screenTouch = null;
        PointerButton(at, KeyReturn, true);
    }

    /// <summary>A finger that wandered from where it came down: no long press, its press goes on.</summary>
    private void ScreenTouchMoved(InputEventMouseMotion motion)
    {
        if (_screenTouch is { } at && motion.Position.DistanceTo(at) > LongPressSlop)
            ReleaseScreenTouch();
    }
}
