using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneLedger"/>: the deployment table and Subordinates on the phone held landscape (docs/ui/phone-build.md,
/// step L5), at the mock-ups' figures on the reference phone (docs/ui/phone.md, §13: LD2, LU1, LU1q with the owner's changes).
/// </summary>
public class PhoneLedgerTests
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
    public void The_deployment_s_ledger_stands_wholly_at_the_left_and_the_rest_at_the_right()
    {
        // ld2: land_ledger at (SIDE + 6, 104, LD2_SPLIT, LH - 12), the right part from LD2_SPLIT + 16 to LW - SIDE - 6.
        Equal(new(116, 104, 1700 - 116, 1208 - 104), PhoneLedger.Ledger(Reference, deployment: true));
        Equal(new(1716, 0, 880, 1170), PhoneLedger.Part(Reference));
        Equal(new(116, 16, 1584, 88), PhoneLedger.Tabs(Reference, deployment: true));
    }

    [Fact]
    public void Subordinates_ledger_fills_the_frame()
    {
        // lu1: land_ledger at (FX0 + 12, 104, FX1 - 12, LH - 12), FX0 = 380, FX1 = 2332.
        Equal(new(392, 104, 1928, 1104), PhoneLedger.Ledger(Reference, deployment: false));
        Equal(new(392, 16, 1928, 88), PhoneLedger.Tabs(Reference, deployment: false));
    }

    [Fact]
    public void The_party_stands_two_cards_a_row_as_the_battle_shows_our_army()
    {
        // ld2: cw = (880 - 12) // 2 = 434, ch = 214 from y 200; the back row (3, 4) left, places 1 and 3 on top.
        Equal(new(1716, 200, 434, 214), PhoneLedger.Card(Reference, 3));
        Equal(new(2162, 200, 434, 214), PhoneLedger.Card(Reference, 1));
        Equal(new(1716, 426, 434, 214), PhoneLedger.Card(Reference, 4));
        Equal(new(2162, 426, 434, 214), PhoneLedger.Card(Reference, 2));
        Equal(new(2502, 210, 84, 84), PhoneLedger.RemoveBox(Reference, PhoneLedger.Card(Reference, 1)));
        Assert.Equal(686, PhoneLedger.TotalsMiddle(Reference), Close);           // y + 24 after y += 10 under the cards
    }

    [Fact]
    public void Stop_and_Attack_stand_at_the_right_thumb()
    {
        // ld2: Stop (rx0, FOOT - 160, rx0 + 300, FOOT - 10), Attack (rx0 + 316, …, rx1, FOOT - 10), FOOT = 1170.
        var one = PhoneLedger.Bar(Reference, 1);
        Assert.Equal(2, one.Count);
        Equal(new(1716, 1010, 300, 150), one[0]);
        Equal(new(2032, 1010, 2596 - 2032, 150), one[1]);
        var two = PhoneLedger.Bar(Reference, 2);                                  // a defence's Plains and City share the room
        Assert.Equal(3, two.Count);
        Assert.Equal(two[1].W, two[2].W, Close);
        Assert.Equal(2596, two[2].Right, Close);
        Assert.Equal(16, two[2].X - two[1].Right, Close);
        Assert.Single(PhoneLedger.Bar(Reference, 0));
    }

    [Fact]
    public void Subordinates_orders_stand_at_both_thumbs()
    {
        // lu_orders_side: the right rail (2342, 2596): End (FOOT - 150, FOOT - 10), Replenish 150 high 14 over it, Talk 140
        // over that; the left rail (116, 370): the six steps 118 high, 10 apart, from FOOT - 10 up.
        Equal(new(2342, 1020, 254, 140), PhoneLedger.End(Reference));
        Equal(new(2342, 856, 254, 150), PhoneLedger.Replenish(Reference));
        Equal(new(2342, 702, 254, 140), PhoneLedger.Talk(Reference));
        var steps = PhoneLedger.Steps(Reference, 6);
        Assert.Equal(6, steps.Count);
        Equal(new(116, 1042, 254, 118), steps[0]);                               // +25 lowest
        Equal(new(116, 1160 - 6 * 118 - 5 * 10, 254, 118), steps[5]);           // +800 on top
        Assert.True(steps[5].Y >= 190);
    }

    [Fact]
    public void Steps_that_would_not_fit_under_their_caption_are_lower()
    {
        var steps = PhoneLedger.Steps(Reference, 9);
        Assert.Equal(9, steps.Count);
        Assert.True(steps[0].H < 118);
        Assert.True(steps[8].Y >= 190 - Close);
        Assert.Equal(1160, steps[0].Bottom, Close);
    }

    [Fact]
    public void Replenish_s_question_stands_left_of_its_button_on_its_foot()
    {
        // lu1q: box (rep[0] - 40 - 620, rep[3] - 300, rep[0] - 40, rep[3]).
        var room = PhoneLedger.ReplenishQuestion(Reference);
        Assert.Equal(2342 - 40, room.Right, Close);
        Assert.Equal(620, room.W, Close);
        Assert.Equal(1006, room.Bottom, Close);
        Assert.True(room.Y <= 1006 - 300);
    }

    [Fact]
    public void A_long_press_raises_the_unit_s_picture_over_the_frame()
    {
        var picture = PhoneLedger.Picture(Reference);
        Assert.Equal(1220 - 120, picture.H, Close);
        Assert.Equal(1100 * 548f / 342f, picture.W, Close);
        Assert.Equal(2712 / 2f, picture.X + picture.W / 2, Close);
        Assert.True(picture.X >= Reference.Frame.X && picture.Right <= Reference.Frame.Right);
    }

    [Fact]
    public void A_longer_screen_widens_the_deployment_s_ledger_and_keeps_the_right_part()
    {
        var longer = PhoneFrame.Landscape(2712 * 21f / 20, 1220);
        Assert.Equal(880, PhoneLedger.Part(longer).W, Close);
        Assert.Equal(longer.Screen.W - 116, PhoneLedger.Part(longer).Right, Close);
        Assert.True(PhoneLedger.Ledger(longer, deployment: true).W > PhoneLedger.Ledger(Reference, deployment: true).W);
        Assert.Equal(16, PhoneLedger.Part(longer).X - PhoneLedger.Ledger(longer, deployment: true).Right, Close);
    }
}
