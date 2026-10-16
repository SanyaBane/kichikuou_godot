namespace Kichikuou.Engine.Script;

/// <summary>
/// Buttons or keys as xsystem35 reads them (<c>event_get_mouse</c>, <c>sys_getInputInfo</c>): a level, not events —
/// a read sees what is held, and nothing is consumed, so a button still held is seen again by the next read. The
/// codes are the engine's <c>SYS35KEY_*</c> bits.
/// </summary>
/// <remarks>
/// One addition to the engine: a press released again before a read (a tap shorter than a frame) still shows in the
/// next read, so a quick click is never lost while the script polls. Only while it polls: a tap no read takes in the
/// frame it came in or the next one is dropped (<see cref="EndFrame"/>), as the engine never sees it. Otherwise a
/// click our own screens answer by events — a menu chosen on its release, a row of our table — would be read again
/// by the script's next key wait or <c>IM</c>, long after, as a click of its own.
/// </remarks>
public sealed class InputLatch
{
    private ushort _held;
    private ushort _fresh;      // pressed since the last frame's end
    private ushort _aging;      // pressed in the frame before: dropped at this frame's end

    /// <summary>A button or key of <paramref name="code"/> went down or up.</summary>
    public void Set(ushort code, bool down)
    {
        if (down)
        {
            _held |= code;
            _fresh |= code;
        }
        else
        {
            _held &= (ushort)~code;
        }
    }

    /// <summary>What is held, and the taps not read yet; the taps are read.</summary>
    public ushort Read()
    {
        ushort code = Peek();
        _fresh = _aging = 0;
        return code;
    }

    /// <summary>What <see cref="Read"/> would return, without reading.</summary>
    public ushort Peek() => (ushort)(_held | _fresh | _aging);

    /// <summary>A frame is over: the taps of the frame before it that no read took are dropped.</summary>
    public void EndFrame()
    {
        _aging = _fresh;
        _fresh = 0;
    }

    /// <summary>Everything released and forgotten (the window lost the focus).</summary>
    public void Clear() => _held = _fresh = _aging = 0;
}
