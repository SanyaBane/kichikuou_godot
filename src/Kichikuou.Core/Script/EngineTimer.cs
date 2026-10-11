using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

/// <summary>
/// The engine timer that <c>ZT1</c> restarts and <c>ZT2</c>-<c>ZT5</c> read (<c>cmdz.c</c>, counter 0),
/// in milliseconds. It follows the real clock, <see cref="Speed"/> times as fast, except while the
/// message skip is on (ours, the owner's request 2026-10-11): then every read moves it on by one
/// 1/100 s tick (<see cref="SkipTickMs"/>) and the real clock is ignored.
/// </summary>
/// <remarks>
/// The only scene that reads the timer is the opening (<c>ｵｰﾌﾟﾆﾝｸﾞ.ADV</c>, at the prologue's end;
/// <c>XSPEC</c> is dead), which the message skip skips whole (<see cref="MessageSkip.WholeScenes"/>).
/// It shows each picture when the timer reaches the picture's time, waiting in a loop that reads
/// <c>ZT5</c> (<c>:1422-1427</c>), and its scrolls give up at 69 s (<c>{RetVal > 6900:</c>, e.g.
/// <c>:661</c>), skipping the CGs left in them. One tick a read ends every wait exactly at its time,
/// as on an infinitely fast machine: the skipped opening runs every picture on its schedule and loads
/// every CG a watched one does (<c>Glob_CgSeen</c>), only at once. A jump of the timer would cut the
/// scrolls and lose those CGs.
/// </remarks>
public sealed class EngineTimer
{
    /// <summary>How far a read moves the timer while skipping: one tick of <c>ZT5</c>, the finest unit a script reads.</summary>
    public const int SkipTickMs = 10;

    private double _ms;         // the timer at _at
    private TimeSpan _at;       // the real clock at the last restart or read

    /// <summary>How much faster than the real clock the timer runs while not skipping (the development switch <c>--fast</c>).</summary>
    public double Speed { get; init; } = 1;

    /// <summary><c>ZT1 n</c>: the timer restarts at <paramref name="ms"/> at the real time <paramref name="now"/>.</summary>
    public void Restart(int ms, TimeSpan now)
    {
        _ms = ms;
        _at = now;
    }

    /// <summary>The timer, in milliseconds, at the real time <paramref name="now"/>; while <paramref name="skipping"/>, one tick on from the last read.</summary>
    public long Read(TimeSpan now, bool skipping)
    {
        _ms += skipping ? SkipTickMs : (now - _at).TotalMilliseconds * Speed;
        _at = now;
        return (long)_ms;
    }

    /// <summary><c>ZT2</c>-<c>ZT5</c>: <paramref name="ms"/> in 1/10, 1/30, 1/60 or 1/100 s, as a 16-bit value.</summary>
    public static Sys3Int InUnits(long ms, int mode)
    {
        long value = mode switch
        {
            2 => ms / 100,
            3 => ms * 3 / 100,
            4 => ms * 3 / 50,
            _ => ms / 10,
        };
        return (Sys3Int)(ushort)(value & 0xffff);
    }
}
