using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneLedgerPortrait"/>: the deployment table and Subordinates on the phone held upright (docs/ui/phone-build.md,
/// step P5), at the mock-ups' figures on the reference phone (docs/ui/phone.md, §7 and §20: PD2, PD2d, PU1, PU1q with the
/// owner's PD2s2 and PU1e3).
/// </summary>
public class PhoneLedgerPortraitTests
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
    public void Both_screens_open_with_the_title_band_under_the_hole()
    {
        // title_band at TOP + 4, 156 high to its divider and under it.
        Equal(new(0, 114, 1220, 156), PhoneLedgerPortrait.Band(Reference));
        Equal(new(0, 202, 1190, 40), PhoneLedgerPortrait.OrderNote(Reference));       // the subtitle's line, ending under the date
    }

    [Fact]
    public void The_party_stands_on_top_as_the_battle_shows_our_army()
    {
        // pd2_base with round P26's PD2f2 (mock_attack_facing.py): the panel from y + 4 (645 high); cw = (PW - 32 - 14) // 2,
        // ch = 170; the back row (3, 4) on top from top + 96 + 42, the front row (1, 2) under it, 14 apart; places 3 and 1 at
        // the left; "Front row" 24 under its cards.
        Equal(new(0, 274, 1220, 645), PhoneLedgerPortrait.Party(Reference));
        Equal(new(16, 412, 587, 170), PhoneLedgerPortrait.Card(Reference, 3));
        Equal(new(617, 412, 587, 170), PhoneLedgerPortrait.Card(Reference, 4));
        Equal(new(16, 596, 587, 170), PhoneLedgerPortrait.Card(Reference, 1));
        Equal(new(617, 596, 587, 170), PhoneLedgerPortrait.Card(Reference, 2));
        Assert.Equal(390, PhoneLedgerPortrait.CaptionMiddle(Reference, front: false), Close);
        Assert.Equal(790, PhoneLedgerPortrait.CaptionMiddle(Reference, front: true), Close);
        Equal(new(1204 - 94, 422, 84, 84), PhoneLedgerPortrait.Insides.RemoveBox(Reference, PhoneLedgerPortrait.Card(Reference, 4)));
    }

    [Fact]
    public void The_bracket_holds_the_front_row_from_below_its_head_beside_the_totals()
    {
        // PD2f2: the line 30 under "Front row"'s middle (P + 546), from the cards' outer sides (1 in), its ends up to their
        // foot; the head's tip 75 under the line, over the panel's foot; the totals' middle beside the head.
        Assert.Equal((17f, 1203f, 610f, 820f, 766f), PhoneLedgerPortrait.Bracket(Reference, 4));
        Assert.Equal((17f, 602f, 309.5f, 820f, 766f), PhoneLedgerPortrait.Bracket(Reference, 1));  // the dungeon search
        var (_, _, _, line, _) = PhoneLedgerPortrait.Bracket(Reference, 4);
        var party = PhoneLedgerPortrait.Party(Reference);
        Assert.True(line + PhoneBracket.HeadTip < party.Bottom - 20);
        Assert.InRange(party.Y + PhoneLedgerPortrait.TotalsMiddle, line + PhoneBracket.HeadBase, line + PhoneBracket.HeadTip);
    }

    [Fact]
    public void The_deployment_s_ledger_lies_at_the_thumb_under_its_tabs()
    {
        // pd2_base: paper_tabs at top + 645 + 14, 116 high from x 24; the ledger from x 16 to PW - 16, down to BAR[0] - 20.
        Equal(new(24, 933, 1180, 116), PhoneLedgerPortrait.Tabs(Reference, deployment: true));
        Equal(new(16, 1049, 1188, 1435), PhoneLedgerPortrait.Ledger(Reference, deployment: true));
    }

    [Fact]
    public void The_sends_and_Stop_share_the_bar()
    {
        // stop_attack, its buttons swapped (owner, 2026-10-30): Attack 16-784, Stop 800 to PW - 16 over BAR (2504-2652).
        var one = PhoneLedgerPortrait.Bar(Reference, 1);
        Equal(new(800, 2504, 404, 148), one[0]);
        Equal(new(16, 2504, 768, 148), one[1]);
        var two = PhoneLedgerPortrait.Bar(Reference, 2);
        Equal(new(16, 2504, 376, 148), two[1]);
        Equal(new(408, 2504, 376, 148), two[2]);
    }

    [Fact]
    public void Subordinates_ledger_lies_over_the_orders()
    {
        // pu1_base: paper_tabs at y + 6; the ledger to orders - 14; the orders' panel from 1868 to BAR[0] - 16.
        Equal(new(24, 276, 1180, 116), PhoneLedgerPortrait.Tabs(Reference, deployment: false));
        Equal(new(16, 392, 1188, 1462), PhoneLedgerPortrait.Ledger(Reference, deployment: false));
        Equal(new(0, 1868, 1220, 620), PhoneLedgerPortrait.Orders(Reference));
    }

    [Fact]
    public void Replenish_and_Talk_share_a_row_and_the_steps_go_three_a_row()
    {
        // pu1_base: Replenish (24, orders + 100, PW // 2 - 10, orders + 236), Talk (PW // 2 + 10, …, PW - 24, …);
        // add_new_grid from orders + 258 + 52, 128 high, 12 apart, three a row from 24 to PW - 24.
        Equal(new(24, 1968, 576, 136), PhoneLedgerPortrait.Replenish(Reference));
        Equal(new(620, 1968, 576, 136), PhoneLedgerPortrait.Talk(Reference));
        var steps = PhoneLedgerPortrait.Steps(Reference, 6);
        float w = (1172 - 24) / 3f;
        Equal(new(24, 2178, w, 128), steps[0]);
        Equal(new(24 + 2 * (w + 12), 2178, w, 128), steps[2]);
        Equal(new(24 + w + 12, 2318, w, 128), steps[4]);
    }

    [Fact]
    public void End_stands_at_the_right_thumb()
    {
        // end_alone: (PW - 16 - 420, BAR[0], PW - 16, BAR[1]).
        Equal(new(784, 2504, 420, 148), PhoneLedgerPortrait.End(Reference));
    }

    [Fact]
    public void A_unit_s_picture_rises_as_a_sheet_over_the_screen_s_foot()
    {
        // pd2d: the picture at PW - 32 wide, the PC's panel's shape (548 × 342), 116 under the sheet's top; its foot here at
        // the bar's.
        float h = 1188 * 342f / 548f;
        Equal(new(16, 2652 - h, 1188, h), PhoneLedgerPortrait.Picture(Reference));
        Equal(new(0, 2652 - h - 116, 1220, 2662 - (2652 - h - 116)), PhoneLedgerPortrait.PictureSheet(Reference));
    }

    [Fact]
    public void A_longer_screen_gives_the_ledgers_the_height()
    {
        var longer = PhoneFrame.Portrait(1220, 2900);
        Assert.Equal(1435 + 188, PhoneLedgerPortrait.Ledger(longer, deployment: true).H, Close);
        Assert.Equal(1462 + 188, PhoneLedgerPortrait.Ledger(longer, deployment: false).H, Close);
        Equal(PhoneLedgerPortrait.Card(Reference, 1), PhoneLedgerPortrait.Card(longer, 1));
    }

    [Fact]
    public void A_tab_scrolls_its_columns_but_No_the_name_the_type_and_the_rows()
    {
        var overview = PhoneLedgerPortrait.Scrolled(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Overview));
        Assert.Equal([DeploymentColumn.Troops, DeploymentColumn.Refill, DeploymentColumn.Cost,
            DeploymentColumn.Item],
            overview.Columns.Select(c => c.Column));
        var subordinates = PhoneLedgerPortrait.Scrolled(LedgerTabs.Of(SubordinatesScreen.Columns, LedgerTab.Overview));
        Assert.Equal([DeploymentColumn.Troops, DeploymentColumn.Refill, DeploymentColumn.Cost,
            DeploymentColumn.Item], subordinates.Columns.Select(c => c.Column));
        var commander = PhoneLedgerPortrait.Scrolled(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Commander));
        Assert.Equal(DeploymentColumn.Strategy, commander.Columns[0].Column);
        Assert.Equal(7, commander.Columns.Count);
    }

    [Fact]
    public void The_troops_tab_s_groups_follow_their_columns()
    {
        var shown = LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Troops);
        var troops = PhoneLedgerPortrait.Scrolled(shown);
        Assert.Equal(10, troops.Columns.Count);
        Assert.Equal([("Offense", 4, 5), ("Defense", 6, 7), ("Dungeon", 8, 9)], troops.Groups.Select(g => (g.Name, g.First, g.Last)));
        Assert.Equal(DeploymentColumn.OffAtkMod, troops.Columns[troops.Groups[0].First].Column);
    }

    [Fact]
    public void The_result_plate_stands_in_the_middle_over_the_bar()
    {
        // pu1r1: x 16 to PW - 16, 800 high from ROOM_MID - 400, ROOM_MID = (TOP + BAR[0]) // 2 = 1307.
        Equal(new(16, 907, 1188, 800), PhoneLedgerPortrait.ResultPlate(Reference));
    }

    [Fact]
    public void Held_landscape_the_result_plate_is_the_upright_card_in_the_room_s_middle()
    {
        // mock_result_plate_land.py, lu1r1: 1188 × 800 in the middle.
        var land = PhoneFrame.Landscape(2712, 1220);
        var plate = PhoneLedgerPortrait.ResultPlate(land);
        Assert.Equal(1188, plate.W, 0.01f);
        Assert.Equal(800, plate.H, 0.01f);
        Assert.Equal(land.Room.X + land.Room.W / 2, plate.X + plate.W / 2, 0.01f);
        Assert.Equal(land.Room.Y + land.Room.H / 2, plate.Y + plate.H / 2, 0.01f);
    }
}
