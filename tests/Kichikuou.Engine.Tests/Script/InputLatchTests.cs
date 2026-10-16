using Kichikuou.Engine.Script;

namespace Kichikuou.Engine.Tests.Script;

/// <summary>The input as the script's reads see it (<see cref="InputLatch"/>): a level, and a quick tap only while read.</summary>
public class InputLatchTests
{
    private const ushort Left = 16, Right = 32;

    [Fact]
    public void A_held_button_is_seen_by_every_read_until_it_is_released()
    {
        var input = new InputLatch();
        input.Set(Left, down: true);
        Assert.Equal(Left, input.Read());
        input.EndFrame();
        input.EndFrame();
        Assert.Equal(Left, input.Read());
        input.Set(Left, down: false);
        Assert.Equal(0, input.Read());
    }

    [Fact]
    public void A_tap_shorter_than_a_frame_is_seen_once()
    {
        var input = new InputLatch();
        input.Set(Left, down: true);
        input.Set(Left, down: false);
        Assert.Equal(Left, input.Peek());
        Assert.Equal(Left, input.Read());
        Assert.Equal(0, input.Read());
    }

    [Fact]
    public void A_tap_is_read_in_the_frame_after_it()
    {
        var input = new InputLatch();
        input.Set(Right, down: true);
        input.Set(Right, down: false);
        input.EndFrame();                                   // the frame it came in: no read
        Assert.Equal(Right, input.Read());
    }

    [Fact]
    public void A_tap_no_read_takes_by_the_next_frames_end_is_dropped()
    {
        var input = new InputLatch();
        input.Set(Left, down: true);
        input.Set(Left, down: false);
        input.EndFrame();
        input.EndFrame();
        Assert.Equal(0, input.Read());
    }

    /// <summary>
    /// The bug of 2026-10-16: a menu our screen answers by events (FIGT_B's, chosen on the release) took a click the script
    /// never read; its press stayed latched, and the next key wait read it at once — Rance's line after Ninja Scout showed
    /// for a moment and was gone. A click held for frames is dropped by the time it is released.
    /// </summary>
    [Fact]
    public void A_click_answered_by_events_over_several_frames_is_not_read_afterwards()
    {
        var input = new InputLatch();
        input.Set(Left, down: true);                        // the press, on the menu's item
        for (int frame = 0; frame < 6; frame++)
            input.EndFrame();                               // held a tenth of a second; nobody reads
        input.Set(Left, down: false);                       // the release chooses the item
        input.EndFrame();
        Assert.Equal(0, input.Read());                      // the next key wait's first read
    }

    [Fact]
    public void Clear_forgets_what_is_held_and_tapped()
    {
        var input = new InputLatch();
        input.Set(Left, down: true);
        input.Set(Right, down: true);
        input.Set(Right, down: false);
        input.Clear();
        Assert.Equal(0, input.Peek());
    }
}
