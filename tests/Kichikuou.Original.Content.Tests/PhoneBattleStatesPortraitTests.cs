using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneBattleStatesPortrait"/>: the battle's other states on the phone held upright (docs/ui/phone-build.md, step
/// P4), at the mock-ups' figures on the reference phone (docs/ui/phone.md, §11 and §20: PB4o3, PB4s, PB4c, PB4r, PB4b).
/// </summary>
public class PhoneBattleStatesPortraitTests
{
    private const float Close = 0.01f;

    /// <summary>The reference phone held upright: the room from 110 down to the gesture zone, the bar's top at 2504.</summary>
    private static readonly PhoneFrame Reference = PhoneFrame.Portrait(1220, 2712);

    /// <summary>The reference board's height in its layout pixels.</summary>
    private static float Height => PhoneBattlePortrait.Height(Reference);

    private static float L(float reference) => reference * 1280 / 1220f;

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual, float close = Close)
    {
        Assert.Equal(expected.X, actual.X, close);
        Assert.Equal(expected.Y, actual.Y, close);
        Assert.Equal(expected.W, actual.W, close);
        Assert.Equal(expected.H, actual.H, close);
    }

    /// <summary>A box of the mock-ups' reference pixels, from the room's corner, as the board's layout pixels.</summary>
    private static PhoneFrame.Box OnBoard(float x, float y, float w, float h) => new(L(x), L(y), L(w), L(h));

    [Fact]
    public void The_opening_has_its_plate_on_top_and_the_names_at_its_sides()
    {
        // pb4o3: the plate (PW // 2 - 420, 16, PW // 2 + 420, 192); the names centred on 230 and PW - 230, 420 wide, their
        // lines on 304 (268 and 340 for two, 232 to 376 for three); the advantage at 448, VS at (PW / 2, 360).
        Equal(OnBoard(190, 16, 840, 176), PhoneBattleStatesPortrait.OpeningPlate);
        var player = PhoneBattleStatesPortrait.OpeningNames(player: true);
        Assert.Equal(L(230), player.X + player.W / 2, Close);
        Assert.Equal(L(420), player.W, Close);
        Assert.Equal(L(304), player.Y + player.H / 2, Close);
        Assert.True(player.Y >= PhoneBattleStatesPortrait.OpeningPlate.Bottom);
        var enemy = PhoneBattleStatesPortrait.OpeningNames(player: false);
        Assert.Equal(1280 - player.Right, enemy.X, Close);                           // mirrored
        Assert.True(enemy.Bottom < PhoneBattleStatesPortrait.OpeningAdvantage);
        Assert.True(PhoneBattleStatesPortrait.OpeningAdvantage < PhoneBattlePortrait.SceneFoot);
        Assert.Equal(640, PhoneBattleStatesPortrait.OpeningVs.X, Close);
        Assert.Equal(L(360), PhoneBattleStatesPortrait.OpeningVs.Y, Close);
        Assert.Equal(L(660), PhoneBattleStatesPortrait.Seam.Top, Close);
        Assert.Equal(L(560), PhoneBattleStatesPortrait.Seam.Foot, Close);
    }

    [Fact]
    public void The_results_plate_stands_over_the_board_at_the_screen_s_width()
    {
        // pb4r: (24, 1000, PW - 24, 1720) on the screen, the room's top 110 down.
        Equal(OnBoard(24, 890, 1172, 720), PhoneBattleStatesPortrait.ResultsPlate(0, Height));
        // Over the battle's Skip, which stands under the enemy's rows.
        Assert.True(PhoneBattleStatesPortrait.ResultsPlate(0, Height).Bottom <= PhoneBattlePortrait.Skip.Y);
        // Level-up rows: taller by them, grown up, its foot where the plate's is (still over Skip).
        float rows = L(400);
        var four = PhoneBattleStatesPortrait.ResultsPlate(rows, Height);
        Assert.Equal(L(890 + 720), four.Bottom, Close);
        Assert.Equal(L(720 + 40 + 80) + rows, four.H, Close);
        var (x, top, width) = PhoneBattleStatesPortrait.LevelRows(four);
        Assert.Equal(four.Y + L(720 + 40), top, Close);
        Assert.Equal((four.X + L(40), four.W - L(80)), (x, width));
        // A short board: the plate rises to stay over its foot.
        var short_ = PhoneBattleStatesPortrait.ResultsPlate(rows, L(1500));
        Assert.Equal(L(1500 - 20), short_.Bottom, Close);
    }

    [Fact]
    public void A_panel_stands_where_the_plate_does()
    {
        var plate = PhoneBattleStatesPortrait.ResultsPlate(0, Height);
        var panel = PhoneBattleStatesPortrait.Panel(2, L(600));
        Assert.Equal(plate.Y + plate.H / 2, panel.Y + panel.H / 2, Close);
        Assert.Equal(640, panel.X + panel.W / 2, Close);
        Assert.Equal(L(600 + 120), panel.W, Close);
        Assert.Equal(L(44 + 2 * 60 + 40), panel.H, Close);
        Assert.Equal(1172 - 120, PhoneBattleStatesPortrait.PanelTextWidth, Close);  // the plate's width less the padding
    }

    [Fact]
    public void A_line_s_balloon_stands_over_the_scene_s_foot_beside_its_portrait()
    {
        // pb4b: the portrait the PC's at the width (286 × 379 from (8, 6)); the balloon right of it, its tail's room left,
        // to 16 short of the edge, its foot at 440.
        Equal(OnBoard(8, 6, 286, 379), PhoneBattleStatesPortrait.Speaker(player: true));
        Equal(OnBoard(1220 - 8 - 286, 6, 286, 379), PhoneBattleStatesPortrait.Speaker(player: false));
        var player = PhoneBattleStatesPortrait.Balloon(player: true, L(62));
        Equal(OnBoard(340, 440 - 190, 864, 190), player);
        Assert.True(player.X - L(PhoneBattleStatesPortrait.TailDepth) >= PhoneBattleStatesPortrait.Speaker(true).Right);
        var enemy = PhoneBattleStatesPortrait.Balloon(player: false, L(62));
        Assert.Equal(1280 - player.Right, enemy.X, Close);                           // mirrored
        Assert.True(enemy.Right + L(PhoneBattleStatesPortrait.TailDepth) <= PhoneBattleStatesPortrait.Speaker(false).X);
        var three = PhoneBattleStatesPortrait.Balloon(player: true, L(3 * 62 + 2 * 16));
        Assert.Equal(player.Bottom, three.Bottom, Close);                              // it grows up
        Assert.True(three.Y > 0);
    }

    [Fact]
    public void A_defence_s_caption_stands_over_the_scene_s_foot_on_its_side()
    {
        var player = PhoneBattleStatesPortrait.Caption(player: true, 400);
        Assert.Equal(L(34), player.X, Close);
        Assert.Equal(PhoneBattlePortrait.SceneFoot - L(64), player.Bottom, Close);
        Assert.Equal(L(117), player.H, Close);
        var enemy = PhoneBattleStatesPortrait.Caption(player: false, 400);
        Assert.Equal(1280 - L(34), enemy.Right, Close);
        // The same millimetres as held landscape: a layout pixel upright is 1.6 of the landscape frame's.
        Assert.Equal(1.6f, PhoneBattleStatesPortrait.Larger, 0.01f);
        Assert.Equal(PhoneBattleStates.RibbonScale * PhoneBattleStatesPortrait.Larger, PhoneBattleStatesPortrait.RibbonScale, Close);
    }

    [Fact]
    public void The_cards_menu_keeps_its_finger_s_rows()
    {
        Assert.Equal(L(104), PhoneBattleStatesPortrait.MenuRowHeight(9, Height), Close);
        Assert.True(PhoneBattleStatesPortrait.MenuRowHeight(9, L(600)) < L(104));     // only a board too short lowers them
    }

    [Fact]
    public void The_strategy_s_cut_in_is_stacked_in_the_room_s_middle()
    {
        var f = Reference;
        // pb4c: the block round the middle between the room's top and the bar ((110 + 2504) / 2 = 1307).
        Assert.Equal(1307, PhoneBattleStatesPortrait.CutInMiddle(f), Close);
        Assert.Equal((1060f, 1000f, 1520f, 1460f), PhoneBattleStatesPortrait.CutInBand(f));
        Equal(new PhoneFrame.Box(40, 1014, 372, 496), PhoneBattleStatesPortrait.Strategist(f, player: true));
        Equal(new PhoneFrame.Box(1220 - 40 - 372, 1014, 372, 496), PhoneBattleStatesPortrait.Strategist(f, player: false));
        Assert.Equal((830f, 1260f), PhoneBattleStatesPortrait.CutInLabel(f, player: true));   // 440 + (PW - 440) / 2
        Assert.Equal((390f, 1260f), PhoneBattleStatesPortrait.CutInLabel(f, player: false));
        Equal(new PhoneFrame.Box(16, 1600, 1188, 290), PhoneBattleStatesPortrait.CutInBalloon(f, 3 * 46));
        Assert.Equal(1307 - 547, 1307 + PhoneBattleStatesPortrait.AnnouncementMiddle, Close); // the announcement at 760
    }
}
