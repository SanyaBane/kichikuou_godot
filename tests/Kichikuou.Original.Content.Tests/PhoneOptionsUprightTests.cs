using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneOptionsUpright"/>: the options window on the phone held upright (docs/ui/phone-build.md, step P8a), at
/// the mock-ups' figures on the reference phone (docs/ui/phone.md, §10 and §21: PO1 with PO1o).
/// </summary>
public class PhoneOptionsUprightTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Upright(1220, 2712);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box? actual)
    {
        Assert.NotNull(actual);
        Assert.Equal(expected.X, actual.Value.X, Close);
        Assert.Equal(expected.Y, actual.Value.Y, Close);
        Assert.Equal(expected.W, actual.Value.W, Close);
        Assert.Equal(expected.H, actual.Value.H, Close);
    }

    [Fact]
    public void The_title_and_the_tabs_stand_under_the_hole_across_the_room()
    {
        // po1: the title at (30, TOP + 4); the tabs at TOP + 112, (PW - 48 - 4 × 8) / 5 wide, 8 apart, 110 high, one not
        // chosen 12 lower.
        var (x, y) = PhoneOptionsUpright.Title(Reference);
        Assert.Equal(30, x, Close);
        Assert.Equal(114, y, Close);
        Equal(new(24, 222, 228, 110), PhoneOptionsUpright.Tab(Reference, 0, chosen: true));
        Equal(new(260, 234, 228, 98), PhoneOptionsUpright.Tab(Reference, 1, chosen: false));
        Assert.Equal(1196, PhoneOptionsUpright.Tab(Reference, 4, chosen: false).Right, Close);
    }

    [Fact]
    public void The_sheet_stands_under_the_tabs_down_to_the_bar_its_column_inside()
    {
        // po1: the sheet from 24 to PW - 24, from the tabs' foot to BAR[0] - 20; the column 30 in, 40 under its top.
        Equal(new(24, 332, 1172, 2152), PhoneOptionsUpright.Sheet(Reference));
        Equal(new(54, 344, 1106, 2128), PhoneOptionsUpright.View(Reference));      // 36 from the right: the bar's room and 6
        Assert.Equal(372, PhoneOptionsUpright.ColumnTop(Reference, 0), Close);
        Assert.Equal(272, PhoneOptionsUpright.ColumnTop(Reference, 100), Close);
    }

    [Fact]
    public void A_column_scrolls_only_as_far_as_it_is_longer_than_the_sheet()
    {
        // Its foot stands 30 over the sheet's at the most: 2082 of the reference's column in view.
        Assert.Equal(0, PhoneOptionsUpright.MaxScroll(Reference, 2000), Close);
        Assert.Equal(0, PhoneOptionsUpright.MaxScroll(Reference, 2082), Close);
        Assert.Equal(418, PhoneOptionsUpright.MaxScroll(Reference, 2500), Close);
        Assert.Null(PhoneOptionsUpright.ScrollBar(Reference, 2000, 0));

        var (track, thumb) = PhoneOptionsUpright.ScrollBar(Reference, 2500, 0)!.Value;
        Equal(new(1170, 344, 20, 2128), track);                                   // the reference (S2), 6 in from the sheet's right
        float length = 2122 * 2082f / 2500;
        Equal(new(1173, 347, 14, length), thumb);
        Equal(new(1173, 347 + 2122 - length, 14, length), PhoneOptionsUpright.ScrollBar(Reference, 2500, 418)!.Value.Thumb);

        // A longer screen shows more of it.
        var longer = PhoneFrame.Upright(1220, 3000);
        Assert.Equal(130, PhoneOptionsUpright.MaxScroll(longer, 2500), Close);
    }

    [Fact]
    public void Defaults_and_close_fill_the_bar_close_at_the_right_thumb()
    {
        // defaults_close: Defaults (16, BAR[0], 420, BAR[1]), Close (436, …, PW - 16, …).
        Equal(new(16, 2504, 404, 148), PhoneOptionsUpright.Defaults(Reference));
        Equal(new(436, 2504, 768, 148), PhoneOptionsUpright.Close(Reference));
    }

    [Fact]
    public void A_question_stands_over_the_sheet_s_middle_within_its_sides()
    {
        // The PC's 500 × 200 at the parts' scale (1160 × 464) is wider than the sheet less 2 × 20.
        Equal(new(44, 332 + (2152 - 464) / 2f, 1132, 464), PhoneOptionsUpright.Question(Reference));
    }
}
