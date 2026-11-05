using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneBattleStates"/>: the battle's other states on the phone held landscape (docs/ui/phone-build.md, step L4),
/// at the mock-ups' figures on the reference phone (docs/ui/phone.md, §17: LB3o1, LB3s, LB3r, LB3b).
/// </summary>
public class PhoneBattleStatesTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Landscape(2712, 1220);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual, float close = Close)
    {
        Assert.Equal(expected.X, actual.X, close);
        Assert.Equal(expected.Y, actual.Y, close);
        Assert.Equal(expected.W, actual.W, close);
        Assert.Equal(expected.H, actual.H, close);
    }

    /// <summary>A box of the mock-ups' reference pixels in the frame (its left edge at 380) as the frame's layout pixels.</summary>
    private static PhoneFrame.Box InFrame(float x, float y, float w, float h) =>
        new((x - 380) / 1.525f, y / 1.525f, w / 1.525f, h / 1.525f);

    [Fact]
    public void The_opening_has_its_plate_on_top_and_the_names_beside_it()
    {
        // opening_plate(d, 18): 940 × 196 at CX - 470 = 886; the names centred in side = plate[0] - FX0 - 60 = 446.
        Equal(InFrame(886, 18, 940, 196), PhoneBattleStates.OpeningPlate);
        var player = PhoneBattleStates.OpeningNames(player: true, advantage: false);
        Assert.Equal((633 - 380) / 1.525f, player.X + player.W / 2, Close);          // (FX0 + plate[0]) // 2
        Assert.Equal(446 / 1.525f, player.W, Close);
        Assert.Equal(438 / 1.525f, player.Bottom, Close);
        var enemy = PhoneBattleStates.OpeningNames(player: false, advantage: true);
        Assert.Equal((2079 - 380) / 1.525f, enemy.X + enemy.W / 2, Close);           // (plate[2] + FX1) // 2
        Assert.True(enemy.Bottom < PhoneBattleStates.OpeningAdvantage);
        Assert.Equal(1280 - player.Right, enemy.X, Close);                           // mirrored
        Assert.Equal((640f, 324 / 1.525f), PhoneBattleStates.OpeningVs);
    }

    [Fact]
    public void The_results_plate_stands_in_the_frame_s_middle()
    {
        // lb3r: 1400 × 720 at (CX - 700, (LH - h) // 2 - 20).
        Equal(InFrame(656, 230, 1400, 720), PhoneBattleStates.ResultsPlate(0));
        // Level-up rows: the plate as tall as its head and them, its head whole while they fit.
        float rows = 200 / 1.525f;
        var some = PhoneBattleStates.ResultsPlate(rows);
        Assert.Equal(1, PhoneBattleStates.HeadScale(rows));
        Assert.Equal((720 + 40 + 80) / 1.525f + rows, some.H, Close);
        Assert.Equal(some.Y + (720 + 40) / 1.525f, PhoneBattleStates.LevelRows(some, rows).Y, Close);
        // Four long rows (A2's 445): the head closes up, the plate still over the gesture zone.
        float four = 445 / 1.525f;
        var tall = PhoneBattleStates.ResultsPlate(four);
        Assert.InRange(PhoneBattleStates.HeadScale(four), PhoneBattleStates.HeadSqueezeLeast, 0.99f);
        Assert.True(tall.Bottom * 1.525f <= 1220 - 50 + Close);
        Assert.True(tall.Y >= 20 / 1.525f - Close);
        var (_, top, width) = PhoneBattleStates.LevelRows(tall, four);
        Assert.True(top + four <= tall.Bottom - 80 / 1.525f + Close);
        Assert.Equal((1400 - 2 * 60) / 1.525f, width, Close);
    }

    [Fact]
    public void A_panel_stands_in_the_frame_s_middle_as_the_plate()
    {
        var one = PhoneBattleStates.Panel(1, 400);
        Assert.Equal(640, one.X + one.W / 2, Close);
        Assert.Equal(400 + 2 * 60 / 1.525f, one.W, Close);
        Assert.True(PhoneBattleStates.Panel(3, 400).H > one.H);
    }

    [Fact]
    public void A_line_s_balloon_stands_over_the_scene_s_foot_beside_its_speaker()
    {
        // lb3b: (SHARP_X0 + 50, 296, SHARP_X0 + 1050, 486), SHARP_X0 = 809 on the screen.
        Equal(InFrame(859, 296, 1000, 190), PhoneBattleStates.Balloon(player: true, textHeight: 62 / 1.525f), 0.05f);
        var two = PhoneBattleStates.Balloon(player: true, textHeight: 150 / 1.525f);
        Assert.Equal(486 / 1.525f, two.Bottom, Close);                              // it grows up from its foot
        Assert.Equal(210 / 1.525f, two.H, Close);
        var enemy = PhoneBattleStates.Balloon(player: false, textHeight: 0);
        Assert.Equal(1280 - PhoneBattleStates.Balloon(true, 0).Right, enemy.X, 0.05f);
        Assert.True(PhoneBattleStates.Caption(true, 300).Bottom < PhoneBattle.SceneBand);
        Assert.Equal(1280 - PhoneBattleStates.Caption(true, 300).Right, PhoneBattleStates.Caption(false, 300).X, 0.05f);
    }

    [Fact]
    public void The_card_menu_has_finger_sized_rows_that_fit_the_frame()
    {
        Assert.Equal(104 / 1.525f, PhoneBattleStates.MenuRowHeight(5), Close);
        Assert.True(12 * PhoneBattleStates.MenuRowHeight(12) * 1.525f <= 1220 - 50 - 24);
    }
}
