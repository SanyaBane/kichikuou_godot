using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneFrame"/> and <see cref="DeviceLayouts"/>: the phone's frame held landscape and how the device is told
/// (docs/ui/phone-build.md, §3; docs/ui/phone.md, §12).
/// </summary>
public class PhoneFrameTests
{
    private const float Close = 0.01f;

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void At_the_reference_phones_own_pixels_the_frame_is_phone_mds()
    {
        var phone = PhoneFrame.Landscape(2712, 1220);
        Assert.Equal(1, phone.Unit, Close);
        Equal(new(380, 0, 1952, 1220), phone.Frame);            // 1.525 phone pixels a layout pixel, sides of 21.6 mm
        Assert.Equal(1.525f, phone.Scale, Close);
        Equal(new(0, 0, 380, 1220), phone.LeftSide);
        Equal(new(2332, 0, 380, 1220), phone.RightSide);
        Assert.Equal(new PhoneFrame.Insets(110, 0, 110, 50), phone.Safe);
        Assert.Equal(default, phone.Bar);                                       // the sides hold the bar's cells
        Equal(new(110, 0, 270, 1170), phone.LeftRoom);          // the hole's strip and the gesture zone left out
        Equal(new(2332, 0, 270, 1170), phone.RightRoom);
    }

    [Fact]
    public void On_the_canvas_the_reference_pixel_is_scaled_by_the_screens_height()
    {
        // The project's canvas (1280×800, aspect "expand") on the reference phone: 800 high, 2712 / 1.525 wide.
        float width = 2712 / 1.525f;
        var phone = PhoneFrame.Landscape(width, 800);
        Assert.Equal(800f / 1220, phone.Unit, Close);
        Equal(new((width - 1280) / 2, 0, 1280, 800), phone.Frame);
        Assert.Equal(1, phone.Scale, Close);
        Assert.Equal(110 * 800f / 1220, phone.Safe.Left, Close);
        Assert.Equal(254 * phone.Unit, phone.Px(254), Close);
        Assert.Equal(800 - 50 * phone.Unit, phone.LeftRoom.Bottom, Close);
    }

    [Fact]
    public void A_longer_screen_has_wider_sides_and_the_same_frame()
    {
        var phone = PhoneFrame.Landscape(2520 * 1220f / 1080, 1220);    // 21:9 at the reference's height
        Equal(new((phone.Screen.W - 1952) / 2, 0, 1952, 1220), phone.Frame);
        Assert.True(phone.LeftSide.W > 380);
        Assert.Equal(phone.LeftSide.W, phone.RightSide.W, Close);
    }

    [Fact]
    public void The_systems_insets_are_read_with_the_larger_side_on_both_and_the_gesture_zone_kept()
    {
        var phone = PhoneFrame.Landscape(2712, 1220, new PhoneFrame.Insets(0, 0, 96, 0));   // the hole on the right
        Assert.Equal(new PhoneFrame.Insets(96, 0, 96, 50), phone.Safe);
        Equal(new(96, 0, 284, 1170), phone.LeftRoom);
        Equal(new(2332, 0, 284, 1170), phone.RightRoom);

        var bars = PhoneFrame.Landscape(2712, 1220, new PhoneFrame.Insets(120, 30, 0, 80));
        Assert.Equal(new PhoneFrame.Insets(120, 30, 120, 80), bars.Safe);
        Equal(new(120, 30, 260, 1110), bars.LeftRoom);
        Equal(new(0, 0, 2712, 1220), bars.Screen);
    }

    [Fact]
    public void A_screen_too_short_for_the_frame_between_the_holes_strips_fits_it_to_the_width_between_them()
    {
        var phone = PhoneFrame.Landscape(1300, 800);                    // 16:10 and a little: no room for the sides
        float side = 110 * 800f / 1220;
        Assert.Equal(1300 - 2 * side, phone.Frame.W, Close);
        Assert.Equal(side, phone.Frame.X, Close);
        Assert.Equal((800 - phone.Frame.H) / 2, phone.Frame.Y, Close);
        Assert.Equal(phone.Frame.W / 1.6f, phone.Frame.H, Close);
        Assert.Equal(0, phone.LeftRoom.W, Close);
    }

    [Fact]
    public void Upright_at_the_reference_phones_own_pixels_the_picture_is_at_the_width_between_the_hole_and_the_bar()
    {
        var phone = PhoneFrame.Upright(1220, 2712);
        Assert.True(phone.IsUpright);
        Assert.Equal(1, phone.Unit, Close);
        Assert.Equal(new PhoneFrame.Insets(0, 110, 0, 50), phone.Safe);       // the hole at the top, the gesture zone at the foot
        Equal(new(0, 110, 1220, 2552), phone.Room);
        Equal(new(16, 2504, 1188, 148), phone.Bar);                           // mock_phone.py's BAR (PK1)
        Equal(new(0, (110 + 2504 - 762.5f) / 2, 1220, 762.5f), phone.Frame);  // its SCENE_Y; 0.953 phone pixels a layout pixel (phone.md, §1)
        Assert.Equal(0.953125f, phone.Scale, Close);
        Equal(new(0, 600, 1220, 762.5f), phone.PictureAt(600));
    }

    [Fact]
    public void Upright_on_the_canvas_the_reference_pixel_is_scaled_by_the_screens_width()
    {
        // The project's canvas (1280×800, aspect "expand") on the reference phone held upright: 1280 wide, 2712 / 0.953 high.
        float height = 2712 * 1280f / 1220;
        var phone = PhoneFrame.Upright(1280, height);
        Assert.Equal(1280f / 1220, phone.Unit, Close);
        Assert.Equal(1, phone.Scale, Close);
        Assert.Equal(1280, phone.Frame.W, Close);
        Assert.Equal(800, phone.Frame.H, Close);
        Assert.Equal(110 * phone.Unit, phone.Room.Y, Close);
        Assert.Equal(height - 50 * phone.Unit, phone.Room.Bottom, Close);
        Assert.Equal(phone.Frame.Y - phone.Room.Y, phone.Bar.Y - phone.Frame.Bottom, Close);
    }

    [Fact]
    public void Upright_a_longer_screen_has_more_free_height_and_the_same_picture()
    {
        var phone = PhoneFrame.Upright(1220, 2520 * 1220f / 1080);              // 21:9 at the reference's width
        Assert.Equal(1220, phone.Frame.W, Close);
        Assert.Equal(762.5f, phone.Frame.H, Close);
        Assert.True(phone.Room.H > 2552);
    }

    [Fact]
    public void Upright_the_systems_insets_are_read_and_the_gesture_zone_kept()
    {
        var phone = PhoneFrame.Upright(1220, 2712, new PhoneFrame.Insets(0, 120, 0, 0));   // the Poco's hole, no gesture zone
        Assert.Equal(new PhoneFrame.Insets(0, 120, 0, 50), phone.Safe);
        Equal(new(0, 120, 1220, 2542), phone.Room);

        var sides = PhoneFrame.Upright(1220, 2712, new PhoneFrame.Insets(20, 0, 20, 80));
        Equal(new(20, 0, 1180, 2632), sides.Room);
        Assert.Equal(1180, sides.Frame.W, Close);                              // the picture at the room's width
        Assert.Equal(20, sides.Frame.X, Close);
    }

    [Fact]
    public void A_frame_for_a_screen_is_held_as_its_shape_says()
    {
        Assert.False(PhoneFrame.For(2712, 1220).IsUpright);
        Assert.True(PhoneFrame.For(1220, 2712).IsUpright);
        Assert.False(PhoneFrame.For(1000, 1000).IsUpright);
        Equal(PhoneFrame.Upright(1220, 2712).Frame, PhoneFrame.For(1220, 2712).Frame);
    }

    [Theory]
    [InlineData(1220, 446, true)]           // the Poco X6 Pro: 69.5 mm
    [InlineData(1220, 480, true)]           // the same at Android's density bucket
    [InlineData(1080, 400, true)]           // a 6.5" 20:9 phone: 68.6 mm
    [InlineData(1600, 240, false)]          // the Galaxy Tab S7 FE at its bucket: 169 mm
    [InlineData(1600, 243, false)]
    [InlineData(1200, 0, false)]            // no dpi known: not a phone
    public void A_phone_is_a_screen_whose_short_side_is_under_90_mm(int shortSide, double dpi, bool phone) =>
        Assert.Equal(phone, DeviceLayouts.IsPhone(shortSide, dpi));

    [Fact]
    public void The_layout_follows_the_phones_window_and_the_touch_mode()
    {
        Assert.Equal(DeviceLayout.Pc, DeviceLayouts.Decide(touch: false, phone: false, 1280, 824));
        Assert.Equal(DeviceLayout.Tablet, DeviceLayouts.Decide(touch: true, phone: false, 2340, 1080));
        Assert.Equal(DeviceLayout.Tablet, DeviceLayouts.Decide(touch: true, phone: false, 1080, 2340));
        Assert.Equal(DeviceLayout.PhoneLandscape, DeviceLayouts.Decide(touch: true, phone: true, 2712, 1220));
        Assert.Equal(DeviceLayout.PhoneUpright, DeviceLayouts.Decide(touch: true, phone: true, 1220, 2712));
        Assert.Equal(DeviceLayout.PhoneLandscape, DeviceLayouts.Decide(touch: true, phone: true, 1000, 1000));
    }
}
