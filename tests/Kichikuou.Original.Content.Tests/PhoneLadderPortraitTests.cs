using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneLadderPortrait"/>: the dungeon's menus on the phone held upright (docs/ui/phone-build.md, step P8b), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §10: PF1 and PF1e).
/// </summary>
public class PhoneLadderPortraitTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Portrait(1220, 2712);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void The_ladder_stands_on_the_bar_s_foot_at_the_right_thumb()
    {
        // floor_rail at (PW - 16 - 440, …, PW - 16) on BAR[1]: Descend 150 high, the floor 110, Ascend, Retreat, 14 apart.
        Equal(new(764, 2502, 440, 150), PhoneLadderPortrait.Way(Reference, down: true));
        Equal(new(764, 2378, 440, 110), PhoneLadderPortrait.Floor(Reference));
        Equal(new(764, 2214, 440, 150), PhoneLadderPortrait.Way(Reference, down: false));
        Equal(new(764, 2050, 440, 150), PhoneLadderPortrait.Retreat(Reference));
        Assert.Equal(2050, PhoneLadderPortrait.Top(Reference, entry: false, others: 0), Close);
    }

    [Fact]
    public void Before_a_search_start_search_takes_descend_s_corner_the_rail_growing_upwards()
    {
        // pf1e: Start Search in the corner, Return 14 over it; the dungeon's other items between them.
        Equal(new(764, 2502, 440, 150), PhoneLadderPortrait.Start(Reference));
        Equal(new(764, 2338, 440, 150), PhoneLadderPortrait.Return(Reference, 0));
        Equal(new(764, 2174, 440, 150), PhoneLadderPortrait.Other(Reference, 0, 2));
        Equal(new(764, 2338, 440, 150), PhoneLadderPortrait.Other(Reference, 1, 2));
        Equal(new(764, 2010, 440, 150), PhoneLadderPortrait.Return(Reference, 2));
        Assert.Equal(2010, PhoneLadderPortrait.Top(Reference, entry: true, others: 2), Close);
    }

    [Fact]
    public void Window_113_stands_left_of_the_rail_the_picture_over_them_at_the_room_s_width()
    {
        // info_window (16, top, rail - 16, BAR[1]); pf_frame: the picture PW - 32 wide in its 552 × 328 shape, its foot about
        // 40 over the rail's top (the mock's 38 and 46).
        Equal(new(16, 2050, 732, 602), PhoneLadderPortrait.Window(Reference, entry: false, others: 0));
        Equal(new(16, 2338, 732, 314), PhoneLadderPortrait.Window(Reference, entry: true, others: 0));
        float h = 1188 * 328f / 552;
        Equal(new(16, 2010 - h, 1188, h), PhoneLadderPortrait.Picture(Reference, entry: false, others: 0));
        Equal(new(16, 2298 - h, 1188, h), PhoneLadderPortrait.Picture(Reference, entry: true, others: 0));
    }
}
