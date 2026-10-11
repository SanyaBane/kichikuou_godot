using Kichikuou.Core.Script;

namespace Kichikuou.Core.Tests.Script;

/// <summary>
/// The engine timer of <c>ZT</c> (<c>cmdz.c</c>): the real clock, or one 1/100 s tick a read while the
/// message skip is on (the owner's request 2026-10-11).
/// </summary>
public class EngineTimerTests
{
    private static TimeSpan Ms(double ms) => TimeSpan.FromMilliseconds(ms);

    [Fact]
    public void It_follows_the_real_clock_from_its_restart()
    {
        var timer = new EngineTimer();
        timer.Restart(500, Ms(1000));

        Assert.Equal(500, timer.Read(Ms(1000), skipping: false));
        Assert.Equal(750, timer.Read(Ms(1250), skipping: false));
        Assert.Equal(2500, timer.Read(Ms(3000), skipping: false));
    }

    [Fact]
    public void Its_speed_scales_the_real_clock()
    {
        var timer = new EngineTimer { Speed = 20 };
        timer.Restart(0, TimeSpan.Zero);

        Assert.Equal(2000, timer.Read(Ms(100), skipping: false));
    }

    [Fact]
    public void Short_reads_do_not_lose_time_to_rounding()
    {
        var timer = new EngineTimer();
        timer.Restart(0, TimeSpan.Zero);
        long ms = 0;
        for (int frame = 1; frame <= 600; frame++)
            ms = timer.Read(TimeSpan.FromSeconds(frame / 60.0), skipping: false);

        Assert.InRange(ms, 9999, 10000);
    }

    [Fact]
    public void While_skipping_every_read_is_one_tick_whatever_the_clock_says()
    {
        var timer = new EngineTimer();
        timer.Restart(0, TimeSpan.Zero);

        Assert.Equal(10, timer.Read(Ms(5000), skipping: true));
        Assert.Equal(20, timer.Read(Ms(5000), skipping: true));
        Assert.Equal(30, timer.Read(Ms(9000), skipping: true));
    }

    [Fact]
    public void Turning_the_skip_on_and_off_mid_way_keeps_the_timer_continuous()
    {
        var timer = new EngineTimer();
        timer.Restart(0, TimeSpan.Zero);

        Assert.Equal(1000, timer.Read(Ms(1000), skipping: false));
        Assert.Equal(1010, timer.Read(Ms(1016), skipping: true));
        Assert.Equal(1026, timer.Read(Ms(1032), skipping: false));   // only the real time since the last read
    }

    [Theory]
    [InlineData(2, 123456, 1234)]
    [InlineData(3, 1000, 30)]
    [InlineData(4, 1000, 60)]
    [InlineData(5, 130500, 13050)]
    [InlineData(5, 700000, 70000 & 0xffff)]
    public void ZT_modes_read_tenths_thirtieths_sixtieths_and_hundredths(int mode, long ms, int expected)
    {
        Assert.Equal(expected, EngineTimer.InUnits(ms, mode).Value);
    }
}
