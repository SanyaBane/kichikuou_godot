using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneBattleStatesUpright"/>: the battle's other states on the phone held upright (docs/ui/phone-build.md, step
/// P4), at the mock-ups' figures on the reference phone (docs/ui/phone.md, §11 and §20: PB4o3, PB4s, PB4c, PB4r, PB4b).
/// </summary>
public class PhoneBattleStatesUprightTests
{
    private const float Close = 0.01f;

    /// <summary>The reference phone held upright: the room from 110 down to the gesture zone, the bar's top at 2504.</summary>
    private static readonly PhoneFrame Reference = PhoneFrame.Upright(1220, 2712);

    /// <summary>The reference board's height in its layout pixels.</summary>
    private static float Height => PhoneBattleUpright.Height(Reference);

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
        Equal(OnBoard(190, 16, 840, 176), PhoneBattleStatesUpright.OpeningPlate);
        var player = PhoneBattleStatesUpright.OpeningNames(player: true);
        Assert.Equal(L(230), player.X + player.W / 2, Close);
        Assert.Equal(L(420), player.W, Close);
        Assert.Equal(L(304), player.Y + player.H / 2, Close);
        Assert.True(player.Y >= PhoneBattleStatesUpright.OpeningPlate.Bottom);
        var enemy = PhoneBattleStatesUpright.OpeningNames(player: false);
        Assert.Equal(1280 - player.Right, enemy.X, Close);                           // mirrored
        Assert.True(enemy.Bottom < PhoneBattleStatesUpright.OpeningAdvantage);
        Assert.True(PhoneBattleStatesUpright.OpeningAdvantage < PhoneBattleUpright.SceneFoot);
        Assert.Equal(640, PhoneBattleStatesUpright.OpeningVs.X, Close);
        Assert.Equal(L(360), PhoneBattleStatesUpright.OpeningVs.Y, Close);
        Assert.Equal(L(660), PhoneBattleStatesUpright.Seam.Top, Close);
        Assert.Equal(L(560), PhoneBattleStatesUpright.Seam.Foot, Close);
    }

    [Fact]
    public void The_results_plate_stands_over_the_board_at_the_screen_s_width()
    {
        // pb4r: (24, 1000, PW - 24, 1720) on the screen, the room's top 110 down.
        Equal(OnBoard(24, 890, 1172, 720), PhoneBattleStatesUpright.ResultsPlate(0, Height));
        // Over the battle's Skip, which stands under the enemy's rows.
        Assert.True(PhoneBattleStatesUpright.ResultsPlate(0, Height).Bottom <= PhoneBattleUpright.Skip.Y);
        // Four level-ups: taller, grown up, its foot where the plate's is (still over Skip).
        var four = PhoneBattleStatesUpright.ResultsPlate(4, Height);
        Assert.Equal(L(890 + 720), four.Bottom, Close);
        Assert.Equal(L(720 + 40 + 4 * 92 + 80), four.H, Close);
        Assert.Equal(four.Y + L(720 + 40 + 3 * 92), PhoneBattleStatesUpright.LevelUpRow(four, 3).Y, Close);
        Assert.True(PhoneBattleStatesUpright.LevelUpRow(four, 3).Bottom <= four.Bottom);
        // A short board: the plate rises to stay over its foot.
        var short_ = PhoneBattleStatesUpright.ResultsPlate(4, L(1500));
        Assert.Equal(L(1500 - 20), short_.Bottom, Close);
    }

    [Fact]
    public void A_panel_stands_where_the_plate_does()
    {
        var plate = PhoneBattleStatesUpright.ResultsPlate(0, Height);
        var panel = PhoneBattleStatesUpright.Panel(2, L(600));
        Assert.Equal(plate.Y + plate.H / 2, panel.Y + panel.H / 2, Close);
        Assert.Equal(640, panel.X + panel.W / 2, Close);
        Assert.Equal(L(600 + 120), panel.W, Close);
        Assert.Equal(L(44 + 2 * 60 + 40), panel.H, Close);
        Assert.Equal(1172 - 120, PhoneBattleStatesUpright.PanelTextWidth, Close);  // the plate's width less the padding
    }

    [Fact]
    public void A_line_s_balloon_stands_over_the_scene_s_foot_beside_its_portrait()
    {
        // pb4b: the portrait the PC's at the width (286 × 379 from (8, 6)); the balloon right of it, its tail's room left,
        // to 16 short of the edge, its foot at 440.
        Equal(OnBoard(8, 6, 286, 379), PhoneBattleStatesUpright.Speaker(player: true));
        Equal(OnBoard(1220 - 8 - 286, 6, 286, 379), PhoneBattleStatesUpright.Speaker(player: false));
        var player = PhoneBattleStatesUpright.Balloon(player: true, L(62));
        Equal(OnBoard(340, 440 - 190, 864, 190), player);
        Assert.True(player.X - L(PhoneBattleStatesUpright.TailDepth) >= PhoneBattleStatesUpright.Speaker(true).Right);
        var enemy = PhoneBattleStatesUpright.Balloon(player: false, L(62));
        Assert.Equal(1280 - player.Right, enemy.X, Close);                           // mirrored
        Assert.True(enemy.Right + L(PhoneBattleStatesUpright.TailDepth) <= PhoneBattleStatesUpright.Speaker(false).X);
        var three = PhoneBattleStatesUpright.Balloon(player: true, L(3 * 62 + 2 * 16));
        Assert.Equal(player.Bottom, three.Bottom, Close);                              // it grows up
        Assert.True(three.Y > 0);
    }

    [Fact]
    public void A_defence_s_caption_stands_over_the_scene_s_foot_on_its_side()
    {
        var player = PhoneBattleStatesUpright.Caption(player: true, 400);
        Assert.Equal(L(34), player.X, Close);
        Assert.Equal(PhoneBattleUpright.SceneFoot - L(64), player.Bottom, Close);
        Assert.Equal(L(117), player.H, Close);
        var enemy = PhoneBattleStatesUpright.Caption(player: false, 400);
        Assert.Equal(1280 - L(34), enemy.Right, Close);
        // The same millimetres as held landscape: a layout pixel upright is 1.6 of the landscape frame's.
        Assert.Equal(1.6f, PhoneBattleStatesUpright.Larger, 0.01f);
        Assert.Equal(PhoneBattleStates.RibbonScale * PhoneBattleStatesUpright.Larger, PhoneBattleStatesUpright.RibbonScale, Close);
    }

    [Fact]
    public void The_cards_menu_keeps_its_finger_s_rows()
    {
        Assert.Equal(L(104), PhoneBattleStatesUpright.MenuRowHeight(9, Height), Close);
        Assert.True(PhoneBattleStatesUpright.MenuRowHeight(9, L(600)) < L(104));     // only a board too short lowers them
    }

    [Fact]
    public void The_scout_report_is_a_screen_of_its_own()
    {
        var f = Reference;
        // PB4sf2m2 (mock_attack_facing.py, round P28): Kanami, her report and the cards one block 580 + 304 + 250 = 1134
        // high, in the middle between the room's top (TOP) and Rance's portrait's (2204): from (110 + 2204 - 1134) / 2 = 590.
        float foot = PhoneBattleStatesUpright.BlockFoot(f, replied: true, commented: false);
        Assert.Equal(2204, foot, Close);
        // pb4s: Kanami framed at the block's top, 24 in, 330 × 440; her balloon from 380 (400 with its tail's room) to
        // PW - 16, 10 to 310 under the block's top.
        Equal(new PhoneFrame.Box(24, 590, 330, 440), PhoneBattleStatesUpright.NinjaPortrait(f, foot));
        Equal(new PhoneFrame.Box(400, 600, 804, 300), PhoneBattleStatesUpright.ReportBalloon(f, foot));
        Assert.True(PhoneBattleStatesUpright.ReportBalloon(f, foot).X - 40 >= PhoneBattleStatesUpright.NinjaPortrait(f, foot).Right);
        // The cards (PW - 32 - 14) // 2 wide, 250 high: the front row 580 under the block's top (PB4sf2), the back row 304
        // under it; the bracket's line 30 over "FRONT ROW"'s middle, its head's tip clear of Kanami's balloon.
        Equal(new PhoneFrame.Box(16, 1170, 587, 250), PhoneBattleStatesUpright.ScoutCard(f, 1, foot));
        Equal(new PhoneFrame.Box(617, 1170, 587, 250), PhoneBattleStatesUpright.ScoutCard(f, 2, foot));
        Equal(new PhoneFrame.Box(16, 1474, 587, 250), PhoneBattleStatesUpright.ScoutCard(f, 3, foot));
        Equal(new PhoneFrame.Box(617, 1474, 587, 250), PhoneBattleStatesUpright.ScoutCard(f, 4, foot));
        Assert.Equal((610f, 1148f), PhoneBattleStatesUpright.RowCaptionAt(f, 0, foot));
        Assert.Equal((610f, 1452f), PhoneBattleStatesUpright.RowCaptionAt(f, 1, foot));
        Assert.Equal((17f, 1203f, 610f, 1118f, 1170f), PhoneBattleStatesUpright.Bracket(f, foot));
        Assert.True(1118 - PhoneBracket.HeadTip > PhoneBattleStatesUpright.ReportBalloon(f, foot).Bottom);
        Assert.Equal(PhoneBattleStatesUpright.NinjaPortrait(f, foot).Y - f.Room.Y, foot - PhoneBattleStatesUpright.ScoutCard(f, 3, foot).Bottom, Close);
        // With Maris's comment the block rises with Rance's row; with no answer it stands in the middle over the bar.
        float raised = PhoneBattleStatesUpright.BlockFoot(f, replied: true, commented: true);
        Assert.Equal(428, PhoneBattleStatesUpright.NinjaPortrait(f, raised).Y, Close);
        Assert.Equal(raised - 318, PhoneBattleStatesUpright.ScoutCard(f, 3, raised).Bottom, Close);
        Assert.Equal(740, PhoneBattleStatesUpright.NinjaPortrait(f, PhoneBattleStatesUpright.BlockFoot(f, replied: false, commented: false)).Y, Close);
        // Rance at the foot on the bar (231 × 300 at (24, BAR[0] - 300)), his balloon from 40 under his top, 160 high.
        Equal(new PhoneFrame.Box(24, 2204, 231, 300), PhoneBattleStatesUpright.SpeakerPortrait(f, right: false, commented: false));
        Equal(new PhoneFrame.Box(300, 2244, 904, 160), PhoneBattleStatesUpright.ReplyBalloon(f, comment: false, commented: false, 52));
        // With Maris's comment: his row a row higher, hers at the foot, mirrored.
        Equal(new PhoneFrame.Box(24, 1880, 231, 300), PhoneBattleStatesUpright.SpeakerPortrait(f, right: false, commented: true));
        Equal(new PhoneFrame.Box(965, 2204, 231, 300), PhoneBattleStatesUpright.SpeakerPortrait(f, right: true, commented: true));
        Equal(new PhoneFrame.Box(16, 2244, 904, 160), PhoneBattleStatesUpright.ReplyBalloon(f, comment: true, commented: true, 52));
        Assert.Equal(52 + 3 * 52 * 1.25f + 60, PhoneBattleStatesUpright.ReplyBalloon(f, false, false, 52 + 3 * 52 * 1.25f).H, Close);
    }

    [Fact]
    public void The_strategy_s_cut_in_is_stacked_in_the_room_s_middle()
    {
        var f = Reference;
        // pb4c: the block round the middle between the room's top and the bar ((110 + 2504) / 2 = 1307).
        Assert.Equal(1307, PhoneBattleStatesUpright.CutInMiddle(f), Close);
        Assert.Equal((1060f, 1000f, 1520f, 1460f), PhoneBattleStatesUpright.CutInBand(f));
        Equal(new PhoneFrame.Box(40, 1014, 372, 496), PhoneBattleStatesUpright.Strategist(f, player: true));
        Equal(new PhoneFrame.Box(1220 - 40 - 372, 1014, 372, 496), PhoneBattleStatesUpright.Strategist(f, player: false));
        Assert.Equal((830f, 1260f), PhoneBattleStatesUpright.CutInLabel(f, player: true));   // 440 + (PW - 440) / 2
        Assert.Equal((390f, 1260f), PhoneBattleStatesUpright.CutInLabel(f, player: false));
        Equal(new PhoneFrame.Box(16, 1600, 1188, 290), PhoneBattleStatesUpright.CutInBalloon(f, 3 * 46));
        Assert.Equal(1307 - 547, 1307 + PhoneBattleStatesUpright.AnnouncementMiddle, Close); // the announcement at 760
    }
}
