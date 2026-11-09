using Godot;
using Kichikuou.Engine.Script;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// The player's mouse and keys as xsystem35 reads them: a state, not events. Every Godot event
/// updates it, whoever waits; the script's input commands read it (<see cref="ReadMouse"/> is
/// <c>event_get_mouse</c>, <see cref="ReadInput"/> is <c>sys_getInputInfo</c>) and nothing is
/// consumed — a button still held is seen again by the next read. The codes are the engine's
/// <c>SYS35KEY_*</c> (<c>input.h</c>, <c>event.c</c>): the left button and Return are 16, the
/// right button and Space 32. A quick tap shows in the next read, unless no read takes it by the
/// end of the next frame (<see cref="InputLatch"/>, <see cref="EndFrame"/>).
/// </summary>
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

    private readonly InputLatch _buttons = new();
    private readonly InputLatch _keys = new();
    private readonly TouchPress _touch = new();

    /// <summary>The pointer in script coordinates (640×400).</summary>
    public Vector2I Position { get; set; }

    /// <summary>A mouse button (16 or 32) went down or up; a release whose finger's press is held back goes with it (<see cref="Press"/>).</summary>
    public void Button(ushort code, bool pressed)
    {
        if (pressed || !_touch.Release(code))
            _buttons.Set(code, pressed);
    }

    /// <summary>
    /// A finger came down at <paramref name="at"/> (script coordinates) with button <paramref name="code"/>: the pointer moves
    /// there at once, and when that is a move the press waits for a read to take it (<see cref="TouchPress"/>; docs/ui/
    /// phone-build.md, §7) — a finger has no move before its press, and some of the original's screens act on what the read
    /// before the click lit.
    /// </summary>
    public void Press(ushort code, Vector2I at)
    {
        bool moved = at != Position;
        Position = at;
        if (moved)
            _touch.Hold(code, _buttons);
        else
            _buttons.Set(code, true);
    }

    /// <summary>A key went down or up; keys the engine does not read are ignored.</summary>
    public void Key(Godot.Key key, bool pressed)
    {
        ushort code = Code(key);
        if (code != 0)
            _keys.Set(code, pressed);
    }

    /// <summary>Everything released (the window lost the focus).</summary>
    public void Clear()
    {
        _touch.Clear();
        _buttons.Clear();
        _keys.Clear();
    }

    /// <summary>A frame is over: the taps no read took in it or the frame before are dropped.</summary>
    public void EndFrame()
    {
        _touch.EndFrame(_buttons);
        _buttons.EndFrame();
        _keys.EndFrame();
    }

    /// <summary><c>event_get_mouse</c> (<c>IM</c>): the buttons held, 16 | 32.</summary>
    public ushort ReadMouse()
    {
        ushort code = _buttons.Read();
        _touch.Read(_buttons);
        return code;
    }

    /// <summary><c>sys_getInputInfo</c> (<c>Y 3</c>, <c>IK 0</c>, the menus): the buttons and keys held.</summary>
    public ushort ReadInput()
    {
        ushort code = (ushort)(_buttons.Read() | _keys.Read());
        _touch.Read(_buttons);
        return code;
    }

    /// <summary>What <see cref="ReadMouse"/> would return, without reading.</summary>
    public ushort PeekMouse() => _buttons.Peek();

    /// <summary>What <see cref="ReadInput"/> would return, without reading.</summary>
    public ushort PeekInput() => (ushort)(_buttons.Peek() | _keys.Peek());

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
