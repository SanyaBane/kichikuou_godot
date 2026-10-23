using Godot;

namespace Kichikuou.Game.Strategy;

// A touch screen's way to the right button (docs/backlog.md, "Android").
public partial class GameScreen
{
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
    /// through Godot's input, so the script's reads and our own screens see it as a mouse's. Godot's input takes the
    /// window's pixels, the pointer is read in the viewport's: on the tablet two apart, and each Back would have
    /// halved the point until it fell on the menu bar.
    /// </summary>
    private void BackAsRightClick()
    {
        var viewport = GetViewport();
        _ = AutoViewClickAsync(viewport.GetScreenTransform() * viewport.GetMousePosition(), MouseButton.Right);
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
            _longPressed = true;
            Input.VibrateHandheld(30);
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
