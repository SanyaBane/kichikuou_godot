using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneAttack"/>: our enemy attack screen on the phone held landscape (docs/ui/phone-build.md, step L6), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §14: LA2 and LA2n with the owner's changes).
/// </summary>
public class PhoneAttackTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Landscape(2712, 1220);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void The_facts_stand_at_the_left_and_the_answers_in_a_column_at_the_right()
    {
        // la2_base: ys_panel (SIDE + 6, 16, x0 + 580, LH - 16), VS at x0 + 630, rebels_panel (x0 + 680, 16, LA2_COL - 16, LH - 16),
        // the column from LA2_COL = 1792 to LW - SIDE - 6 = 2596.
        Equal(new(116, 16, 580, 1188), PhoneAttack.Province(Reference));
        Assert.Equal((746f, 610f), PhoneAttack.Vs(Reference));
        Equal(new(796, 16, 980, 1188), PhoneAttack.Army(Reference));
        Equal(new(1792, 0, 804, 1170), PhoneAttack.Column(Reference));
    }

    [Fact]
    public void The_province_s_picture_and_figures_stand_in_the_panel_s_middle()
    {
        // ys_panel(centre=True): the picture 540 wide (the PC's 3:2, 360 high), its figures 18 under it in rows of 88, the
        // block centred in the panel: (16 + 1204) / 2 - (360 + 18 + 4 * 88) / 2 = 245.
        Equal(new(136, 245, 540, 360), PhoneAttack.Picture(Reference));
        Equal(new(146, 623, 520, 88), PhoneAttack.Figure(Reference, 0));
        Assert.Equal(975, PhoneAttack.Figure(Reference, 3).Bottom, Close);
    }

    [Fact]
    public void The_army_s_cards_stand_two_a_row_the_front_row_facing_the_province()
    {
        // rebels_panel(centre=True): cx0 = 796 + 64, cw = (1776 - 24 - 860 - 16) // 2 = 438, ch = 300; y = 610 - 780 / 2 = 220,
        // the captions at y + 14, the cards from y + 40, the front row (1, 2) left, 16 apart across and 14 down.
        Equal(new(860, 260, 438, 300), PhoneAttack.Card(Reference, 1));
        Equal(new(860, 574, 438, 300), PhoneAttack.Card(Reference, 2));
        Equal(new(1314, 260, 438, 300), PhoneAttack.Card(Reference, 3));
        Equal(new(1314, 574, 438, 300), PhoneAttack.Card(Reference, 4));
        Assert.Equal((1079f, 234f), PhoneAttack.RowCaptionAt(Reference, 0));
        Assert.Equal((1533f, 234f), PhoneAttack.RowCaptionAt(Reference, 1));
        Assert.Equal((820f, 567f, 30f, 34f), PhoneAttack.Pointer(Reference));        // (x0 + 24, y + ch + 7), 30 deep, 68 high
        Assert.Equal((860f, 1748f, 894f), PhoneAttack.Totals(Reference));            // y += 2 * ch + 14 + 20
    }

    [Fact]
    public void Intercept_stands_at_the_thumb_under_Abandon_City_and_Ninja_Scout()
    {
        // la2_base: Intercept (1792, FOOT - 10 - 130, 2596, FOOT - 10); row1 = 1030 - 16 - 180 = 834, half = 394: Abandon City
        // (1792, row1 + 18, 1792 + round(394 * 0.9), row1 + 162), Ninja Scout (1792 + 394 + 16, row1, 2596, row1 + 180); Maris
        // (1792, row1 - 16 - 320, 2596, row1 - 16).
        Equal(new(1792, 1030, 804, 130), PhoneAttack.Intercept(Reference));
        Equal(new(1792, 852, 355, 144), PhoneAttack.Abandon(Reference));
        Equal(new(2202, 834, 394, 180), PhoneAttack.NinjaScout(Reference));
        Equal(new(1792, 498, 804, 320), PhoneAttack.Advisor(Reference));
        Assert.True(PhoneAttack.Advisor(Reference).Y >= PhoneAttack.Divider);
    }

    [Fact]
    public void Five_ninjas_fit_two_a_row_above_Ninja_Scout_under_the_news()
    {
        // la2n: rh = 128, h = 86 + 3 * rh + 2 * 12 + 24 = 518, the box (1792, 834 - 30 - h, 2596, 834 - 30); the rows from
        // (x0 + 24, y0 + 86), cw = (804 - 48 - 16) // 2 = 370, 16 and 12 apart.
        Equal(new(1792, 286, 804, 518), PhoneAttack.List(Reference, 5));
        Equal(new(1816, 372, 370, 128), PhoneAttack.ListRowBox(Reference, 0, 5));
        Equal(new(2202, 372, 370, 128), PhoneAttack.ListRowBox(Reference, 1, 5));
        Equal(new(1816, 652, 370, 128), PhoneAttack.ListRowBox(Reference, 4, 5));
        Assert.True(PhoneAttack.List(Reference, 5).Y >= PhoneAttack.Divider - Close);
    }

    [Fact]
    public void One_ninja_takes_the_list_s_whole_width()
    {
        Equal(new(1792, 804 - 238, 804, 238), PhoneAttack.List(Reference, 1));      // 86 + 128 + 24
        Equal(new(1816, 652, 756, 128), PhoneAttack.ListRowBox(Reference, 0, 1));
    }

    [Fact]
    public void A_longer_screen_widens_the_army_s_panel_and_keeps_the_rest()
    {
        var longer = PhoneFrame.Landscape(2712 * 21f / 20, 1220);
        Assert.Equal(580, PhoneAttack.Province(longer).W, Close);
        Assert.Equal(804, PhoneAttack.Column(longer).W, Close);
        Assert.Equal(longer.Screen.W - 116, PhoneAttack.Column(longer).Right, Close);
        Assert.True(PhoneAttack.Army(longer).W > PhoneAttack.Army(Reference).W);
        Assert.True(PhoneAttack.Card(longer, 1).W > PhoneAttack.Card(Reference, 1).W);
    }

    [Fact]
    public void The_Poco_s_hole_keeps_both_edges_free()
    {
        // The Poco reports a hole of 120 on its left (docs/ui/phone-build.md, step F, "The Poco"): kept on both sides.
        var poco = PhoneFrame.Landscape(2712, 1220, new PhoneFrame.Insets(120, 0, 0, 0));
        Assert.Equal(126, PhoneAttack.Province(poco).X, Close);
        Assert.Equal(2712 - 126, PhoneAttack.Column(poco).Right, Close);
        Assert.Equal(16, PhoneAttack.Column(poco).X - PhoneAttack.Army(poco).Right, Close);
    }
}
