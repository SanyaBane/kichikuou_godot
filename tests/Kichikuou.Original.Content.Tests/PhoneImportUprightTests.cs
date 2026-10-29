using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneImportUpright"/>: the import screen on the phone held upright (docs/ui/phone-build.md, step P8d), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §22: PI, PIp, PIr).
/// </summary>
public class PhoneImportUprightTests
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
    public void The_card_stands_in_the_room_s_middle_its_head_as_held_landscape_s()
    {
        // import_upright: the card (16, cy − 780, PW − 16, cy + 780), cy = (TOP + BAR[0]) // 2; the paper 26 in.
        Equal(new(16, 527, 1188, 1560), PhoneImportUpright.Panel(Reference));
        Equal(new(42, 553, 1136, 1508), PhoneImportUpright.Paper(Reference));
        // sheet_head: the heading 90 under the paper's top, the version 166, the rule 206 (40 in), the steps 262.
        Assert.Equal(643, PhoneImportUpright.HeadingMiddle(Reference), Close);
        Assert.Equal(719, PhoneImportUpright.VersionMiddle(Reference), Close);
        Assert.Equal((82f, 1138f, 759f), PhoneImportUpright.Rule(Reference));
        Assert.Equal(815, PhoneImportUpright.StepsTop(Reference), Close);
    }

    [Fact]
    public void The_steps_stand_one_under_another_with_the_field_tick_and_stamp_in_their_column()
    {
        // mx = p[0] + 76, tx = p[0] + 140, x1 = p[2] − 40; the field from tx to x1, 150 high.
        Assert.Equal(118, PhoneImportUpright.MarkX(Reference), Close);
        Assert.Equal(182, PhoneImportUpright.TextLeft(Reference), Close);
        Assert.Equal(1138, PhoneImportUpright.Right(Reference), Close);
        Equal(new(182, 900, 956, 150), PhoneImportUpright.Field(Reference, 900));
        // tick(x1 − 20), its words to x1 − 60; stamp(x1 − 140, the field's top + 360).
        Assert.Equal(1118, PhoneImportUpright.TickX(Reference), Close);
        Assert.Equal(1078, PhoneImportUpright.TickedRight(Reference), Close);
        Assert.Equal(998, PhoneImportUpright.StampX(Reference), Close);
    }

    [Fact]
    public void Back_and_the_red_button_stand_in_the_bar()
    {
        // back_and: Back (16 … 400), the red button (416 … PW − 16).
        Equal(new(16, 2504, 384, 148), PhoneImportUpright.Back(Reference));
        Equal(new(416, 2504, 788, 148), PhoneImportUpright.Button(Reference));
    }

    [Fact]
    public void A_short_screen_shrinks_the_card_to_the_room_over_the_bar()
    {
        var shorter = PhoneFrame.Upright(1220, 1600);
        var panel = PhoneImportUpright.Panel(shorter);
        Assert.Equal(shorter.Bar.Y - shorter.Room.Y - 32, panel.H, Close);
        Assert.Equal(shorter.Room.Y + 16, panel.Y, Close);
    }
}
