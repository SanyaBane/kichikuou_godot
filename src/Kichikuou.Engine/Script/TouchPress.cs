namespace Kichikuou.Engine.Script;

/// <summary>
/// A finger's press on a touch screen (docs/ui/phone-build.md, §7, "A tap's move before its press"): a mouse moves before
/// it clicks, so the script has read the pointer at the click's place before the button goes down; a finger comes down
/// without a move, its place and its press in one event. Some of the original's screens act on a click on what the read
/// before it lit — Fortune Check and the CG gallery (<c>HAPPY_C</c>, <c>CG</c>; STR-39's kind) — so a press whose move
/// and button reached the script in one read would act on what the finger left. A press that lands elsewhere is held back
/// here until a read has taken the move (<see cref="Read"/>), then goes into the buttons' <see cref="InputLatch"/>, its
/// release with it when the finger was lifted meanwhile (a tap). A press no read takes goes in at the end of the frame
/// after its own (<see cref="EndFrame"/>), to age there as any tap.
/// </summary>
public sealed class TouchPress
{
    private ushort _code;
    private bool _released;
    private int _frames;

    /// <summary>Whether a press is held back.</summary>
    public bool Holding => _code != 0;

    /// <summary>A press of <paramref name="code"/> that moved the pointer: held back until the move is read; one held already goes in first.</summary>
    public void Hold(ushort code, InputLatch buttons)
    {
        Give(buttons);
        _code = code;
        _released = false;
        _frames = 0;
    }

    /// <summary>A release of <paramref name="code"/>: taken (true) when its press is still held back, the two then going in as a tap.</summary>
    public bool Release(ushort code)
    {
        if (_code != code)
            return false;
        _released = true;
        return true;
    }

    /// <summary>The script read the pointer (the move): the press held back goes in, for the next read.</summary>
    public void Read(InputLatch buttons) => Give(buttons);

    /// <summary>A frame is over: a press held back through the frame after its own goes in (call before the latch's own end of frame).</summary>
    public void EndFrame(InputLatch buttons)
    {
        if (_code != 0 && ++_frames >= 2)
            Give(buttons);
    }

    /// <summary>Everything forgotten (the window lost the focus).</summary>
    public void Clear() => _code = 0;

    private void Give(InputLatch buttons)
    {
        if (_code == 0)
            return;
        buttons.Set(_code, true);
        if (_released)
            buttons.Set(_code, false);
        _code = 0;
    }
}
