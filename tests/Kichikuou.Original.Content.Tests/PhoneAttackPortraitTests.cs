using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneAttackPortrait"/>: our enemy attack screen on the phone held upright (docs/ui/phone-build.md, step P6), at
/// the mock-ups' figures on the reference phone (docs/ui/phone.md, §8: PA1 and PA1n with the owner's change).
/// </summary>
public class PhoneAttackPortraitTests
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
    public void The_head_and_the_province_stand_under_the_hole()
    {
        // attack_head at TOP + 6, its divider at y + 172, the panels from y + 186; pa1_base: the province's panel
        // (16, y, PW - 16, y + 540), its picture at (40, y + 84) 600 wide, the figures from 670 to PW - 44 in rows of 88 from
        // y + 100, each rule 82 under its row's top.
        Equal(new(0, 116, 1220, 186), PhoneAttackPortrait.Head(Reference));
        Equal(new(16, 302, 1188, 540), PhoneAttackPortrait.Province(Reference));
        Equal(new(40, 386, 600, 400), PhoneAttackPortrait.Picture(Reference));
        Equal(new(670, 402, 506, 82), PhoneAttackPortrait.Figure(Reference, 0));
        Equal(new(670, 666, 506, 82), PhoneAttackPortrait.Figure(Reference, 3));
        Assert.True(PhoneAttackPortrait.Figure(Reference, 3).Bottom <= PhoneAttackPortrait.Province(Reference).Bottom);
        Assert.True(PhoneAttackPortrait.Picture(Reference).Right < PhoneAttackPortrait.Figure(Reference, 0).X);
        Assert.Equal((610f, 882f), PhoneAttackPortrait.Vs(Reference));                   // y + 540 + 40
    }

    [Fact]
    public void The_army_s_cards_stand_two_a_row_the_front_row_on_top_in_the_panel_s_middle()
    {
        // pa1_base: the army's panel (16, 922, PW - 16, MARIS_TOP - 20 = 1966); its cards cw = (PW - 64 - 14) // 2 = 571,
        // ch = 270, at x 32 and 617, the rows' captions in the 40 over each row, the totals under them. Their block (756)
        // centred in the panel, 922 + (1044 - 756) / 2 = 1066, and 20 lower for the bracket (round P25): the front row's
        // cards from 1126.
        Equal(new(16, 922, 1188, 1044), PhoneAttackPortrait.Army(Reference));
        Equal(new(32, 1126, 571, 270), PhoneAttackPortrait.Card(Reference, 1));
        Equal(new(617, 1126, 571, 270), PhoneAttackPortrait.Card(Reference, 2));
        Equal(new(32, 1450, 571, 270), PhoneAttackPortrait.Card(Reference, 3));
        Equal(new(617, 1450, 571, 270), PhoneAttackPortrait.Card(Reference, 4));
        Assert.Equal((610f, 1102f), PhoneAttackPortrait.RowCaptionAt(Reference, 0));
        Assert.Equal((610f, 1426f), PhoneAttackPortrait.RowCaptionAt(Reference, 1));
        Assert.Equal((44f, 1176f, 1736f), PhoneAttackPortrait.Totals(Reference));       // 28 in, 16 under the cards
    }

    [Fact]
    public void The_bracket_holds_the_front_row_from_above_its_head_under_the_panel_s_top()
    {
        // mock_attack_facing.py, PA1f2a: the line 30 over "FRONT ROW"'s middle, from the cards' outer sides (1 in); the head's
        // tip 75 over it, 75 under the panel's top.
        Assert.Equal((33f, 1187f, 610f, 1072f, 1126f), PhoneAttackPortrait.Bracket(Reference));
        var (_, _, _, line, _) = PhoneAttackPortrait.Bracket(Reference);
        Assert.Equal(997, line - PhoneBracket.HeadTip, Close);
    }

    [Fact]
    public void After_a_scouting_the_army_s_block_stands_lower_under_the_seal()
    {
        // PK3c (mock_attack_scout_pk3.py): the seal 192 over the panel's top frame, 34 over it and 28 in; the block 48 lower,
        // the seal's foot 40 over the bracket's line, the totals still over the panel's foot.
        var seal = PhoneAttackPortrait.Seal(Reference);
        var army = PhoneAttackPortrait.Army(Reference);
        Equal(new(army.Right - 220, army.Y - 34, 192, 192), seal);
        Assert.Equal(PhoneAttackPortrait.Card(Reference, 1).Y + 48, PhoneAttackPortrait.Card(Reference, 1, scouted: true).Y, Close);
        var (_, _, _, line, _) = PhoneAttackPortrait.Bracket(Reference, scouted: true);
        Assert.Equal(40, line - seal.Bottom, Close);
        var (_, _, totals) = PhoneAttackPortrait.Totals(Reference, scouted: true);
        Assert.True(totals + PhoneAttack.TotalsRule <= army.Bottom - 24);
    }

    [Fact]
    public void Maris_and_the_answers_stand_at_the_thumb_Intercept_in_the_bar()
    {
        // ROW1 = BAR[0] - 168 = 2336: Maris (16, ROW1 - 20 - 330, PW - 16, ROW1 - 20); Abandon City (16, ROW1 + 10,
        // 16 + round(586 * 0.9), ROW1 + 138), Ninja Scout (PW // 2 + 8, ROW1, PW - 16, ROW1 + 148), Intercept the bar.
        Equal(new(16, 1986, 1188, 330), PhoneAttackPortrait.Advisor(Reference));
        Equal(new(16, 2346, 527, 128), PhoneAttackPortrait.Abandon(Reference));
        Equal(new(618, 2336, 586, 148), PhoneAttackPortrait.NinjaScout(Reference));
        Equal(new(16, 2504, 1188, 148), PhoneAttackPortrait.Intercept(Reference));
        Assert.Equal(Reference.Bar, PhoneAttackPortrait.Intercept(Reference));
    }

    [Fact]
    public void One_ninja_s_list_stands_above_Ninja_Scout_as_drawn()
    {
        // pa1n: the list (440, ROW1 - 30 - 150 - 110, PW - 16, ROW1 - 30), its row (x0 + 24, y0 + 86, x1 - 24, y0 + 236).
        Equal(new(440, 2046, 764, 260), PhoneAttackPortrait.List(Reference, 1));
        Equal(new(464, 2132, 716, 150), PhoneAttackPortrait.ListRowBox(Reference, 0, 1));
    }

    [Fact]
    public void Five_ninjas_stand_two_a_row_across_the_room()
    {
        // h = 86 + 3 * 150 + 2 * 12 + 24 = 584; the rows (1188 - 48 - 16) / 2 = 562 wide, 16 and 12 apart.
        Equal(new(16, 1722, 1188, 584), PhoneAttackPortrait.List(Reference, 5));
        Equal(new(40, 1808, 562, 150), PhoneAttackPortrait.ListRowBox(Reference, 0, 5));
        Equal(new(618, 1808, 562, 150), PhoneAttackPortrait.ListRowBox(Reference, 1, 5));
        Equal(new(40, 2132, 562, 150), PhoneAttackPortrait.ListRowBox(Reference, 4, 5));
        Assert.True(PhoneAttackPortrait.List(Reference, 5).Y > PhoneAttackPortrait.Card(Reference, 4).Bottom);
    }

    [Fact]
    public void The_Poco_s_hole_moves_the_head_and_the_province_down()
    {
        // The Poco reports a hole of 120 (docs/ui/phone-build.md, step F, "The Poco"); the bar stays where it was.
        var poco = PhoneFrame.Portrait(1220, 2712, new PhoneFrame.Insets(0, 120, 0, 0));
        Assert.Equal(126, PhoneAttackPortrait.Head(poco).Y, Close);
        Assert.Equal(312, PhoneAttackPortrait.Province(poco).Y, Close);
        Equal(PhoneAttackPortrait.Abandon(Reference), PhoneAttackPortrait.Abandon(poco));
        Assert.Equal(270, PhoneAttackPortrait.Card(poco, 1).H, Close);
    }

    [Fact]
    public void A_longer_screen_gives_the_army_s_panel_the_height_and_a_shorter_one_takes_it_from_the_cards()
    {
        var longer = PhoneFrame.Portrait(1220, 2712 * 21f / 20);
        Assert.True(PhoneAttackPortrait.Army(longer).H > PhoneAttackPortrait.Army(Reference).H);
        Assert.Equal(270, PhoneAttackPortrait.Card(longer, 1).H, Close);

        var shorter = PhoneFrame.Portrait(1080, 2160);                                   // 18:9: 2440 reference pixels high
        float cards = PhoneAttackPortrait.Card(shorter, 1).H / shorter.Unit;
        Assert.InRange(cards, 200f, 269f);
        var (_, _, top) = PhoneAttackPortrait.Totals(shorter);
        Assert.True(top + shorter.Px(106) <= PhoneAttackPortrait.Army(shorter).Bottom);
        // The bracket's head stays in the panel, its line under the panel's caption.
        var (_, _, _, line, _) = PhoneAttackPortrait.Bracket(shorter);
        Assert.True(line - shorter.Px(PhoneBracket.HeadTip) >= PhoneAttackPortrait.Army(shorter).Y + shorter.Px(20) - Close);
        Assert.True(line >= PhoneAttackPortrait.Army(shorter).Y + shorter.Px(84) - Close);
    }
}
