using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneTitleUpright"/>: the title's rows on the phone held upright (docs/ui/phone-build.md, step P8d), at the
/// mock-up's figures on the reference phone (docs/ui/phone.md, §22: PE2 as the owner changed it).
/// </summary>
public class PhoneTitleUprightTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Upright(1220, 2712);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void Every_button_s_row_stands_over_the_bar_the_picture_in_the_middle_over_them()
    {
        // pe2: rows 130 high, 12 apart, from 40 to PW − 40, the last 20 over the bar: top = 2504 − 20 − 8·130 − 7·12.
        Equal(new(40, 1360, 1140, 130), PhoneTitleUpright.Row(Reference, 0, 8));
        Equal(new(40, 2354, 1140, 130), PhoneTitleUpright.Row(Reference, 7, 8));
        Equal(new(16, 1332, 1188, 2498 - 1332), PhoneTitleUpright.Panel(Reference, 8));
        // py = (TOP + top − 30) // 2 − 381.
        Assert.Equal((110 + 1360 - 30) / 2f - 762.5f / 2, PhoneTitleUpright.PictureTop(Reference, 8), Close);
    }

    [Fact]
    public void A_first_launch_s_three_rows_stand_lower_and_the_picture_with_them()
    {
        Equal(new(40, 2070, 1140, 130), PhoneTitleUpright.Row(Reference, 0, 3));
        Assert.Equal((110 + 2070 - 30) / 2f - 762.5f / 2, PhoneTitleUpright.PictureTop(Reference, 3), Close);
        var moved = Reference.WithPictureAt(PhoneTitleUpright.PictureTop(Reference, 3));
        Assert.Equal(PhoneTitleUpright.PictureTop(Reference, 3), moved.Frame.Y, Close);
        Assert.Equal(Reference.Frame.W, moved.Frame.W, Close);
    }

    [Fact]
    public void The_rows_are_the_buttons_the_title_draws_jokes_on_top_and_start_at_the_thumb()
    {
        var s = new GameState();
        foreach (int n in (int[])[2, 5, 6, 8])                         // a first launch (ｽﾀMENU.ADV:56-60)
            s.MenuItems[(Sys3Int)n].MenuItem = 1;
        s.MenuItems[(Sys3Int)9].MenuItem = 2;
        Assert.Equal([2, 5, 6], PhoneTitleUpright.Buttons(s));
        foreach (int n in (int[])[0, 1, 3, 4, 7])                      // a clear and both rolls
            s.MenuItems[(Sys3Int)n].MenuItem = 1;
        Assert.Equal([4, 3, 0, 1, 7, 2, 5, 6], PhoneTitleUpright.Buttons(s));
        Assert.Equal(["Spy", "Please", "Destiny", "B.G.M", "C.G", "Alice", "Load", "Start"],
            PhoneTitleUpright.Buttons(s).Select(n => PhoneTitleUpright.Words[n]));
        var start = s.Buttons[(Sys3Int)PhoneTitleUpright.Start];
        (start.X1, start.Y1, start.W, start.H) = ((Sys3Int)261, (Sys3Int)263, (Sys3Int)173, (Sys3Int)137);
        Assert.Equal((261 + 86, 263 + 68), PhoneTitleUpright.Centre(s, PhoneTitleUpright.Start));
    }
}
