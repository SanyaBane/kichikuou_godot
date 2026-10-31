using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneTitleUpright"/>: the title's own screen on the phone held upright (docs/ui/phone-build.md, step P8d), at
/// the mock-up's figures on the reference phone (docs/ui/phone.md, §44-§45: PE6, <c>tools/ui/mock_title_upright.py</c>).
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
    public void Every_button_s_row_stands_over_the_bar_the_logo_in_the_middle_over_them()
    {
        // rows 208 high, 14 apart, from 40 to PW − 40, the last 20 over the bar: top = 2504 − 20 − 8·208 − 7·14.
        Equal(new(40, 722, 1140, 208), PhoneTitleUpright.Row(Reference, 0, 8));
        Equal(new(40, 2276, 1140, 208), PhoneTitleUpright.Row(Reference, 7, 8));
        // lay_logo: ×2.2, in the middle of 110 + 40 to the rows' top − 40.
        Equal(new((1220 - 416 * 2.2f) / 2, (150 + 682 - 139 * 2.2f) / 2, 416 * 2.2f, 139 * 2.2f), PhoneTitleUpright.Logo(Reference, 8, 416, 139));
    }

    [Fact]
    public void A_first_launch_s_three_rows_stand_lower_and_the_logo_with_them()
    {
        Equal(new(40, 1832, 1140, 208), PhoneTitleUpright.Row(Reference, 0, 3));
        Assert.Equal((150 + 1792 - 139 * 2.2f) / 2, PhoneTitleUpright.Logo(Reference, 3, 416, 139).Y, Close);
    }

    [Fact]
    public void The_characters_stand_in_one_column_the_words_left_aligned_right_of_it()
    {
        // Alice's easel and ghost, 113 × 95: fitted to 186 high (×1.958), the widest of the eight.
        var (w, h) = PhoneTitleUpright.IconSize(Reference, 113, 95);
        Assert.Equal(113 * 186 / 95f, w, Close);
        Assert.Equal(186, h, Close);
        Assert.Equal((150, 170), PhoneTitleUpright.IconSize(Reference, 75, 85));     // Please: at most twice her pixels
        var (left, words) = PhoneTitleUpright.Column(Reference, w, 300);
        Assert.Equal(610 - (w + 44 + 300) / 2, left, Close);
        Assert.Equal(left + w + 44, words, Close);
        var row = PhoneTitleUpright.Row(Reference, 0, 3);
        Equal(new(left + (w - 150) / 2, 1832 + (208 - 170) / 2f, 150, 170), PhoneTitleUpright.Icon(row, left, w, (150, 170)));
    }

    [Fact]
    public void The_texture_runs_down_the_screen_every_second_copy_mirrored()
    {
        var tiles = PhoneTitleUpright.Tiles(Reference, 364).ToList();
        float h = 364 * 1220 / 640f;                                          // the screen's width: ×1.906
        Assert.Equal(4, tiles.Count);
        Equal(new(0, 3 * h, 1220, h), tiles[3].Box);
        Assert.Equal([false, true, false, true], tiles.Select(t => t.Flipped));
        Equal(new(0, 0, 1220, 16 * 1220 / 640f), PhoneTitleUpright.FriezeTop(Reference, 16));
        Equal(new(0, 2712 - 20 * 1220 / 640f, 1220, 20 * 1220 / 640f), PhoneTitleUpright.FriezeFoot(Reference, 20));
    }

    [Fact]
    public void A_description_stands_the_picture_on_the_logo()
    {
        var logo = PhoneTitleUpright.Logo(Reference, 8, 416, 139);
        Assert.Equal(logo.Y + logo.H / 2 - 762.5f / 2, PhoneTitleUpright.DescriptionPictureTop(Reference, 8, 139), Close);
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
