using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The player's mouse and keys as xsystem35 reads them: a state, not events. Every Godot event
/// updates it, whoever waits; the script's input commands read it (<see cref="ReadMouse"/> is
/// <c>event_get_mouse</c>, <see cref="ReadInput"/> is <c>sys_getInputInfo</c>) and nothing is
/// consumed — a button still held is seen again by the next read. The codes are the engine's
/// <c>SYS35KEY_*</c> (<c>input.h</c>, <c>event.c</c>): the left button and Return are 16, the
/// right button and Space 32.
/// </summary>
/// <remarks>
/// One addition to the engine: a press that is released again before the next read (a tap
/// shorter than a frame) still shows in that one read, so a quick click is never lost.
/// </remarks>
internal sealed class PlayerInput
{
    public const ushort Up = 1;
    public const ushort Down = 2;
    public const ushort Left = 4;
    public const ushort Right = 8;
    public const ushort Return = 16;
    public const ushort Space = 32;
    public const ushort Escape = 64;
    public const ushort Tab = 128;

    private ushort _buttons;
    private ushort _keys;
    private ushort _buttonsTapped;
    private ushort _keysTapped;

    /// <summary>The pointer in script coordinates (640×400).</summary>
    public Vector2I Position { get; set; }

    /// <summary>A mouse button (16 or 32) went down or up.</summary>
    public void Button(ushort code, bool pressed)
    {
        if (pressed)
        {
            _buttons |= code;
            _buttonsTapped |= code;
        }
        else
        {
            _buttons &= (ushort)~code;
        }
    }

    /// <summary>A key went down or up; keys the engine does not read are ignored.</summary>
    public void Key(Godot.Key key, bool pressed)
    {
        ushort code = Code(key);
        if (code == 0)
            return;
        if (pressed)
        {
            _keys |= code;
            _keysTapped |= code;
        }
        else
        {
            _keys &= (ushort)~code;
        }
    }

    /// <summary>Everything released (the window lost the focus).</summary>
    public void Clear() => _buttons = _keys = _buttonsTapped = _keysTapped = 0;

    /// <summary><c>event_get_mouse</c> (<c>IM</c>): the buttons held, 16 | 32.</summary>
    public ushort ReadMouse()
    {
        ushort code = (ushort)(_buttons | _buttonsTapped);
        _buttonsTapped = 0;
        return code;
    }

    /// <summary><c>sys_getInputInfo</c> (<c>Y 3</c>, <c>IK 0</c>, the menus): the buttons and keys held.</summary>
    public ushort ReadInput()
    {
        ushort code = (ushort)(_buttons | _keys | _buttonsTapped | _keysTapped);
        _buttonsTapped = _keysTapped = 0;
        return code;
    }

    /// <summary>What <see cref="ReadInput"/> would return, without reading.</summary>
    public ushort PeekInput() => (ushort)(_buttons | _keys | _buttonsTapped | _keysTapped);

    /// <summary><c>event_get_key</c>: the arrows (and the keypad's 8, 2, 4, 6), Return, Space, Escape, Tab.</summary>
    private static ushort Code(Godot.Key key) => key switch
    {
        Godot.Key.Up or Godot.Key.Kp8 => Up,
        Godot.Key.Down or Godot.Key.Kp2 => Down,
        Godot.Key.Left or Godot.Key.Kp4 => Left,
        Godot.Key.Right or Godot.Key.Kp6 => Right,
        Godot.Key.Enter or Godot.Key.KpEnter => Return,
        Godot.Key.Space => Space,
        Godot.Key.Escape => Escape,
        Godot.Key.Tab => Tab,
        _ => 0,
    };
}
