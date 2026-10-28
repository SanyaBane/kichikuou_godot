using Kichikuou.Engine.Script;

namespace Kichikuou.Engine.Tests.Script;

/// <summary>
/// A finger's press (<see cref="TouchPress"/>): one that lands elsewhere reaches the script's reads only after a read has
/// taken its move (docs/ui/phone-build.md, §7).
/// </summary>
public class TouchPressTests
{
    private const ushort Left = 16, Right = 32;

    /// <summary>A read of the buttons as the player's input makes it: the latch's, then the press held back goes in.</summary>
    private static ushort Read(InputLatch buttons, TouchPress touch)
    {
        ushort code = buttons.Read();
        touch.Read(buttons);
        return code;
    }

    [Fact]
    public void A_held_press_is_seen_by_the_read_after_the_move()
    {
        var buttons = new InputLatch();
        var touch = new TouchPress();
        touch.Hold(Left, buttons);
        Assert.True(touch.Holding);
        Assert.Equal(0, Read(buttons, touch));                  // the move alone
        Assert.Equal(Left, Read(buttons, touch));               // then the press, held
        Assert.Equal(Left, Read(buttons, touch));
        buttons.Set(Left, down: false);
        Assert.Equal(0, Read(buttons, touch));
    }

    [Fact]
    public void A_tap_lifted_before_any_read_is_still_seen_once_after_the_move()
    {
        var buttons = new InputLatch();
        var touch = new TouchPress();
        touch.Hold(Left, buttons);
        Assert.True(touch.Release(Left));                       // taken: its press is held back
        Assert.Equal(0, Read(buttons, touch));
        Assert.Equal(Left, Read(buttons, touch));
        Assert.Equal(0, Read(buttons, touch));
    }

    [Fact]
    public void A_release_of_another_button_is_not_taken()
    {
        var touch = new TouchPress();
        touch.Hold(Left, new InputLatch());
        Assert.False(touch.Release(Right));
        Assert.False(new TouchPress().Release(Left));
    }

    [Fact]
    public void A_press_no_read_takes_goes_in_after_the_frame_after_its_own_and_ages_as_a_tap()
    {
        var buttons = new InputLatch();
        var touch = new TouchPress();
        touch.Hold(Left, buttons);
        touch.Release(Left);
        touch.EndFrame(buttons);
        buttons.EndFrame();                                     // its own frame: still held back
        Assert.True(touch.Holding);
        touch.EndFrame(buttons);
        buttons.EndFrame();                                     // the frame after: in, as a tap of this frame
        Assert.False(touch.Holding);
        Assert.Equal(Left, buttons.Peek());
        touch.EndFrame(buttons);
        buttons.EndFrame();
        touch.EndFrame(buttons);
        buttons.EndFrame();                                     // unread: dropped as any tap
        Assert.Equal(0, buttons.Peek());
    }

    [Fact]
    public void A_second_press_lets_the_first_go_in()
    {
        var buttons = new InputLatch();
        var touch = new TouchPress();
        touch.Hold(Left, buttons);
        touch.Release(Left);
        touch.Hold(Right, buttons);
        Assert.Equal(Left, Read(buttons, touch));               // the first tap, with the second's move
        Assert.Equal(Right, Read(buttons, touch));
    }
}
