using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneLedgerUpright"/>: the deployment table and Subordinates on the phone held upright (docs/ui/phone-build.md,
/// step P5), at the mock-ups' figures on the reference phone (docs/ui/phone.md, §7 and §20: PD2, PD2d, PU1, PU1q with the
/// owner's PD2s2 and PU1e3).
/// </summary>
public class PhoneLedgerUprightTests
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
    public void Both_screens_open_with_the_title_band_under_the_hole()
    {
        // title_band at TOP + 4, 156 high to its divider and under it.
        Equal(new(0, 114, 1220, 156), PhoneLedgerUpright.Band(Reference));
        Equal(new(0, 202, 1190, 40), PhoneLedgerUpright.OrderNote(Reference));       // the subtitle's line, ending under the date
    }

    [Fact]
    public void The_party_stands_on_top_as_the_battle_shows_our_army()
    {
        // pd2_base: the panel from y + 4 (600 high); cw = (PW - 32 - 14) // 2, ch = 170; the back row (3, 4) on top from
        // top + 96 + 42, the front row (1, 2) under it, 42 + 170 + 10 apart; places 3 and 1 at the left.
        Equal(new(0, 274, 1220, 600), PhoneLedgerUpright.Party(Reference));
        Equal(new(16, 412, 587, 170), PhoneLedgerUpright.Card(Reference, 3));
        Equal(new(617, 412, 587, 170), PhoneLedgerUpright.Card(Reference, 4));
        Equal(new(16, 634, 587, 170), PhoneLedgerUpright.Card(Reference, 1));
        Equal(new(617, 634, 587, 170), PhoneLedgerUpright.Card(Reference, 2));
        Assert.Equal(390, PhoneLedgerUpright.CaptionMiddle(Reference, front: false), Close);
        Assert.Equal(612, PhoneLedgerUpright.CaptionMiddle(Reference, front: true), Close);
        Equal(new(1204 - 94, 422, 84, 84), PhoneLedgerUpright.Insides.RemoveBox(Reference, PhoneLedgerUpright.Card(Reference, 4)));
    }

    [Fact]
    public void The_deployment_s_ledger_lies_at_the_thumb_under_its_tabs()
    {
        // pd2_base: paper_tabs at top + 600 + 14, 116 high from x 24; the ledger from x 16 to PW - 16, down to BAR[0] - 20.
        Equal(new(24, 888, 1180, 116), PhoneLedgerUpright.Tabs(Reference, deployment: true));
        Equal(new(16, 1004, 1188, 1480), PhoneLedgerUpright.Ledger(Reference, deployment: true));
    }

    [Fact]
    public void Stop_and_the_sends_share_the_bar()
    {
        // stop_attack: Stop 16-420, Attack 436 to PW - 16 over BAR (2504-2652).
        var one = PhoneLedgerUpright.Bar(Reference, 1);
        Equal(new(16, 2504, 404, 148), one[0]);
        Equal(new(436, 2504, 768, 148), one[1]);
        var two = PhoneLedgerUpright.Bar(Reference, 2);
        Equal(new(436, 2504, 376, 148), two[1]);
        Equal(new(828, 2504, 376, 148), two[2]);
    }

    [Fact]
    public void Subordinates_ledger_lies_over_the_orders()
    {
        // pu1_base: paper_tabs at y + 6; the ledger to orders - 14; the orders' panel from 1868 to BAR[0] - 16.
        Equal(new(24, 276, 1180, 116), PhoneLedgerUpright.Tabs(Reference, deployment: false));
        Equal(new(16, 392, 1188, 1462), PhoneLedgerUpright.Ledger(Reference, deployment: false));
        Equal(new(0, 1868, 1220, 620), PhoneLedgerUpright.Orders(Reference));
    }

    [Fact]
    public void Replenish_and_Talk_share_a_row_and_the_steps_go_three_a_row()
    {
        // pu1_base: Replenish (24, orders + 100, PW // 2 - 10, orders + 236), Talk (PW // 2 + 10, …, PW - 24, …);
        // add_new_grid from orders + 258 + 52, 128 high, 12 apart, three a row from 24 to PW - 24.
        Equal(new(24, 1968, 576, 136), PhoneLedgerUpright.Replenish(Reference));
        Equal(new(620, 1968, 576, 136), PhoneLedgerUpright.Talk(Reference));
        var steps = PhoneLedgerUpright.Steps(Reference, 6);
        float w = (1172 - 24) / 3f;
        Equal(new(24, 2178, w, 128), steps[0]);
        Equal(new(24 + 2 * (w + 12), 2178, w, 128), steps[2]);
        Equal(new(24 + w + 12, 2318, w, 128), steps[4]);
    }

    [Fact]
    public void End_stands_at_the_right_thumb()
    {
        // end_alone: (PW - 16 - 420, BAR[0], PW - 16, BAR[1]).
        Equal(new(784, 2504, 420, 148), PhoneLedgerUpright.End(Reference));
    }

    [Fact]
    public void A_unit_s_picture_rises_as_a_sheet_over_the_screen_s_foot()
    {
        // pd2d: the picture at PW - 32 wide, the PC's panel's shape (548 × 342), 116 under the sheet's top; its foot here at
        // the bar's.
        float h = 1188 * 342f / 548f;
        Equal(new(16, 2652 - h, 1188, h), PhoneLedgerUpright.Picture(Reference));
        Equal(new(0, 2652 - h - 116, 1220, 2662 - (2652 - h - 116)), PhoneLedgerUpright.PictureSheet(Reference));
    }

    [Fact]
    public void A_longer_screen_gives_the_ledgers_the_height()
    {
        var longer = PhoneFrame.Upright(1220, 2900);
        Assert.Equal(1480 + 188, PhoneLedgerUpright.Ledger(longer, deployment: true).H, Close);
        Assert.Equal(1462 + 188, PhoneLedgerUpright.Ledger(longer, deployment: false).H, Close);
        Equal(PhoneLedgerUpright.Card(Reference, 1), PhoneLedgerUpright.Card(longer, 1));
    }

    [Fact]
    public void A_tab_scrolls_its_columns_but_No_the_name_and_the_type()
    {
        var overview = PhoneLedgerUpright.Scrolled(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Overview));
        Assert.Equal([DeploymentColumn.Rows, DeploymentColumn.Troops, DeploymentColumn.Refill, DeploymentColumn.Cost],
            overview.Columns.Select(c => c.Column));
        var subordinates = PhoneLedgerUpright.Scrolled(LedgerTabs.Of(SubordinatesScreen.Columns, LedgerTab.Overview));
        Assert.Equal([DeploymentColumn.Troops, DeploymentColumn.Refill, DeploymentColumn.Cost], subordinates.Columns.Select(c => c.Column));
        var commander = PhoneLedgerUpright.Scrolled(LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Commander));
        Assert.Equal(DeploymentColumn.Strategy, commander.Columns[0].Column);
        Assert.Equal(7, commander.Columns.Count);
    }

    [Fact]
    public void The_troops_tab_s_groups_follow_their_columns()
    {
        var shown = LedgerTabs.Of(DeploymentTable.Columns, LedgerTab.Troops);
        var troops = PhoneLedgerUpright.Scrolled(shown);
        Assert.Equal(10, troops.Columns.Count);
        Assert.Equal([("Offense", 4, 5), ("Defense", 6, 7), ("Dungeon", 8, 9)], troops.Groups.Select(g => (g.Name, g.First, g.Last)));
        Assert.Equal(DeploymentColumn.OffAtkMod, troops.Columns[troops.Groups[0].First].Column);
    }
}
