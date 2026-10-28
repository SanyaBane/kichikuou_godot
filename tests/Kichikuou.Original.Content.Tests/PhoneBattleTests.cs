using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneBattle"/>: our battle screen on the phone held landscape (docs/ui/phone-build.md, step L3), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §12 LB2, §17 LB2r1 and LB2x).
/// </summary>
public class PhoneBattleTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Landscape(2712, 1220);

    /// <summary>The frame's left edge on the reference phone (FX0): the mock-ups' x less it, over 1.525, is the layout's.</summary>
    private const float FrameX = 380;

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    /// <summary>A box of the mock-ups' reference pixels in the frame as the frame's layout pixels.</summary>
    private static PhoneFrame.Box InFrame(float x, float y, float w, float h) =>
        new((x - FrameX) / 1.525f, y / 1.525f, w / 1.525f, h / 1.525f);

    [Fact]
    public void The_scene_is_seventy_percent_high_its_picture_in_the_middle()
    {
        // LB2_SCENE = 438; SHARP_W = round(1024 * 438 / 410) = 1094 at (2712 - 1094) / 2.
        Assert.Equal(438 / 1.525f, PhoneBattle.SceneBand, Close);
        Assert.Equal(0.7005f, PhoneBattle.SceneScale, 0.001f);
        Assert.Equal(1093.9f / 1.525f, PhoneBattle.ScenePicture.W, 0.1f);
        Assert.Equal(640, PhoneBattle.ScenePicture.X + PhoneBattle.ScenePicture.W / 2, Close);
    }

    [Fact]
    public void The_cards_stand_under_it_with_the_gauge_between_the_front_rows()
    {
        // land_board: cw 447 at xs [392, 851, 1413, 1872]; rows from 438 + 50, 334 high, 12 apart; the gauge 90 wide at 1356.
        Equal(InFrame(392, 488, 447, 334), PhoneBattle.Card(0, 0));
        Equal(InFrame(851, 488, 447, 334), PhoneBattle.Card(1, 0));
        Equal(InFrame(1413, 834, 447, 334), PhoneBattle.Card(2, 1));
        Equal(InFrame(1872, 834, 447, 334), PhoneBattle.Card(3, 1));
        Equal(InFrame(1311, 488, 90, 680), PhoneBattle.Gauge);
        Assert.True(PhoneBattle.Card(3, 0).Right <= 1280);
        Assert.True(PhoneBattle.Card(0, 1).Bottom * 1.525f <= 1220 - 50);           // over the gesture zone
        Assert.True(PhoneBattle.Names(true).Bottom <= PhoneBattle.Card(0, 0).Y);
        Assert.Equal(1280 - PhoneBattle.Names(true).X, PhoneBattle.Names(false).Right, Close);
    }

    [Fact]
    public void The_rails_stand_in_the_sides_from_the_foot_up()
    {
        // land_rails: RAIL_L = (116, 370), the panel 6 out, the rows 8 in; five commands 123 high 12 apart from FOOT - 10.
        // The Poco keeps 120 for the hole where the mock-ups kept 110 (PhoneFrame), the reference's 110 here.
        Equal(new(110, 0, 266, 1220), PhoneBattle.LeftPanel(Reference));
        Equal(new(2336, 0, 266, 1220), PhoneBattle.RightPanel(Reference));
        Equal(new(124, 20, 238, 100), PhoneBattle.SkipCell(Reference));
        var commands = PhoneBattle.CommandRows(Reference, 5);
        Equal(new(124, 1037, 238, 123), commands[0]);
        Equal(new(124, 497, 238, 123), commands[4]);                                // Retreat on top: RETREAT_BOX
        Assert.True(commands[4].Y > PhoneBattle.LeftLine(Reference, PhoneBattle.LeftRule, 0).Y);
        // Melee 260 high at the right thumb (gold_button's box).
        var attacks = PhoneBattle.AttackRows(Reference, 2);
        Equal(new(2350, 900, 238, 260), attacks[0]);
        Equal(new(2350, 628, 238, 260), attacks[1]);
    }

    [Fact]
    public void Too_many_rows_are_lower_under_the_rule()
    {
        var attacks = PhoneBattle.AttackRows(Reference, 4);
        Assert.True(attacks[3].Y >= 172 + 18 - Close);
        Assert.True(attacks[0].H < 260);
        var commands = PhoneBattle.CommandRows(Reference, 7);
        Assert.True(commands[6].Y >= 222 + 18 - Close);
        Assert.Equal(1160, commands[0].Bottom, Close);
    }

    [Fact]
    public void The_retreat_question_stands_beside_Retreat_in_the_frame()
    {
        // lb2r1: 660 × 350 at FX0 + 44, its middle level with RETREAT_BOX's; the answers 138 high from 186, 16 apart.
        var retreat = PhoneBattle.CommandRows(Reference, 5)[4];
        var box = PhoneBattle.RetreatQuestion(Reference, retreat);
        Equal(new(424, 383.5f, 660, 350), box);
        var answers = PhoneBattle.RetreatAnswers(Reference, box, 2);
        Equal(new(464, 569.5f, 282, 138), answers[0]);
        Equal(new(762, 569.5f, 282, 138), answers[1]);
        // A Retreat low on its rail: the question stays over the gesture zone.
        var low = PhoneBattle.RetreatQuestion(Reference, new PhoneFrame.Box(124, 1037, 238, 123));
        Assert.Equal(1220 - 50 - 20 - 350, low.Y, Close);
    }
}
