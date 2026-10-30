using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneScenes"/> and <see cref="PhoneMenu"/> on the phone held upright (docs/ui/phone-build.md, step P1), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §18 PS2, §21 PK1, §25 PN1 and PR2; <c>mock_phone.py</c>'s
/// <c>TOP</c>, <c>BAR</c>, <c>SCENE_Y</c>, <c>SPEECH_H</c>).
/// </summary>
public class PhoneScenesUprightTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Upright(1220, 2712);

    /// <summary>The picture's top and foot: SCENE_Y and SCENE_Y + 762.5.</summary>
    private const float SceneTop = 925.75f, SceneFoot = SceneTop + 762.5f;

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void Menu_log_and_skip_are_the_bars_cells_at_its_left()
    {
        Equal(new(16, 2504, 170, 148), PhoneScenes.MenuCell(Reference));
        Equal(new(200, 2504, 170, 148), PhoneScenes.LogCell(Reference));
        Equal(new(384, 2504, 170, 148), PhoneScenes.SkipCell(Reference));
        Equal(PhoneScenes.LogCell(Reference), PhoneScenes.LogClose(Reference));        // PR2: Close in Log's place
        Equal(PhoneScenes.MenuCell(PhoneFrame.Landscape(2712, 1220)), PhoneScenes.LogClose(PhoneFrame.Landscape(2712, 1220)));
    }

    [Fact]
    public void The_left_portraits_speech_stands_above_the_picture_its_tail_down_at_him()
    {
        var w = PhoneScenes.Window(Reference, MessageKind.SpeechLeft, (470, 16, 756, 282), (true, false));
        Assert.True(w.Above);
        Equal(new(16, SceneTop - 64 - 420, 1188, 420), w.Panel);
        Assert.Equal(64f / 48, w.Scale, Close);                                        // the script's 24 at 3.6 mm
        Assert.Equal(60, w.TextX, Close);
        Assert.Equal(SceneTop - 484 + 34, w.TextY, Close);
        Assert.Equal(240 * Reference.Scale, w.PointerX!.Value, Close);
        // Five lines of the script's (26 apart at 1×) fit the four lines' height.
        Assert.True(w.TextY + (4 + 4 * 52) * w.RowScale + 48 * w.Scale < w.Panel.Bottom - 12);
    }

    [Fact]
    public void Every_other_window_stands_under_the_picture()
    {
        var right = PhoneScenes.Window(Reference, MessageKind.SpeechRight, (86, 530, 756, 282), (true, true));
        Assert.False(right.Above);
        Equal(new(16, SceneFoot + 64, 1188, 420), right.Panel);
        Assert.Equal(1040 * Reference.Scale, right.PointerX!.Value, Close);

        var alone = PhoneScenes.Window(Reference, MessageKind.ThoughtLeft, (470, 16, 756, 282), (false, true));
        Assert.False(alone.Above);                                                      // no left portrait: Rance's lines
        Assert.Null(alone.PointerX);
        Assert.Null(PhoneScenes.Window(Reference, MessageKind.SpeechRight, (86, 530, 756, 282), (true, false)).PointerX);

        var narration = PhoneScenes.Window(Reference, MessageKind.Narration, (280, 556, 752, 280), (true, true));
        Assert.False(narration.Above);
        Assert.Null(narration.PointerX);
        Assert.Equal(64f / 48, narration.Scale, Close);
    }

    [Fact]
    public void Window_131_stands_under_its_cg_its_lines_fitted_to_the_width()
    {
        var w = PhoneScenes.Window(Reference, MessageKind.CgBar, (16, 610, 1252, 280), (false, false));
        Assert.False(w.Above);
        Equal(new(16, SceneFoot + 64, 1188, 330), w.Panel);
        Assert.Equal((1188 - 48) / 1252f, w.Scale, Close);                             // 2.5 mm, under its own 2.6
        Assert.Equal(16 + 24, w.TextX, Close);
        Assert.Equal(SceneFoot + 64 + 56, w.TextY, Close);                             // under its speaker's plate
        Assert.Null(w.PointerX);
    }

    [Fact]
    public void A_scene_menus_rows_stand_at_the_thumb_under_the_lower_window()
    {
        var menu = PhoneScenes.SceneMenu(Reference, 3, 300);
        Assert.Equal(3, menu.Rows.Count);
        Equal(new(40, 2652 - 3 * 130 - 2 * 14, 1140, 130), menu.Rows[0]);
        Equal(new(40, 2652 - 130, 1140, 130), menu.Rows[2]);
        Equal(new(16, 2234 - 30, 1188, 2660 - 2204), menu.Panel);
        Assert.Equal(PhoneScenes.ItemTextSize(Reference), menu.TextSize, Close);

        float under = SceneFoot + 64 + 420 + 16 + 30;
        var five = PhoneScenes.SceneMenu(Reference, 5, 300);                            // lower, still under the window
        Assert.Equal(under, five.Rows[0].Y, Close);
        Assert.Equal(2652, five.Rows[4].Bottom, Close);

        var seven = PhoneScenes.SceneMenu(Reference, 7, 300);                           // at 0.6, over the window's foot
        Assert.Equal(130 * 0.6f, seven.Rows[0].H, Close);
        Assert.True(seven.Rows[0].Y < under);

        var wide = PhoneScenes.SceneMenu(Reference, 2, 2000);                           // an item too wide: smaller type
        Assert.Equal(PhoneScenes.ItemTextSize(Reference) * (1140 - 48) / 2000, wide.TextSize, Close);
    }

    [Fact]
    public void The_log_paper_and_the_week_report_take_the_width_over_the_bar()
    {
        Equal(new(16, 120, 1188, 2504 - 20 - 120), PhoneScenes.LogPaper(Reference));
        Assert.Equal(1188 / (600 * Reference.Scale), PhoneScenes.ReportScale(Reference, 600, 400), Close);
        Assert.Equal((2504 - 110 - 120) / (2000 * Reference.Scale), PhoneScenes.ReportScale(Reference, 600, 2000), Close);
    }

    [Fact]
    public void The_log_puts_each_speaker_over_his_lines()
    {
        LogLine[] lines =
        [
            new(LogKind.Place, "", "Leazas Castle"),
            new(LogKind.Speech, "Maris", "Good morning."),
            new(LogKind.Speech, "", "The ministers wait."),
            new(LogKind.Narration, "", "The sun rose."),
            new(LogKind.Choice, MessageLog.ChoiceLabel, "Attack", true),
        ];
        LogLine[] expected =
        [
            lines[0],
            new(LogKind.Speech, "Maris", ""),
            new(LogKind.Speech, "", "Good morning."),
            lines[2],
            lines[3],
            new(LogKind.Choice, MessageLog.ChoiceLabel, ""),
            new(LogKind.Choice, "", "Attack", true),
        ];
        Assert.Equal(expected, PhoneScenes.NamesOver(lines));
        Assert.Equal([0, 1, 3, 4, 5, 7], Enumerable.Range(0, 6).Select(i => PhoneScenes.ToNamesOver(lines, i)));
        Assert.Equal([0, 1, 1, 2, 3, 4, 4, 5], Enumerable.Range(0, 8).Select(i => PhoneScenes.FromNamesOver(lines, i)));
    }

    [Fact]
    public void The_menu_sheet_rises_from_the_bar_options_at_the_thumb()
    {
        var main = PhoneMenu.Main(new PhoneMenu.Settings(false, true, true, true, true));
        var rows = PhoneMenu.Tiles(Reference, main);
        Equal(new(40, 2504 - 40 - 130, 1140, 130), rows[0]);                           // Options
        Assert.Equal(rows[0].Y - 142, rows[1].Y, Close);                              // Message skip
        Assert.Equal(rows[3].Bottom + 52, rows[2].Y, Close);                          // the rule between Save Screenshot and Sound
        Assert.Equal(rows[4].Bottom + 12, rows[3].Y, Close);
        Assert.Equal(1442, rows[6].Y, Close);                                          // Debug on top
        Equal(new(16, 1442 - 130, 1188, 2504 - 14 - 1312), PhoneMenu.Panel(Reference, main));
        Assert.Equal((48f, 1312f + 66), PhoneMenu.Title(Reference, main));
        Assert.Equal(4, PhoneMenu.TileAt(Reference, main, 600, rows[4].Y + 10));
        Assert.Equal(-1, PhoneMenu.TileAt(Reference, main, 600, rows[3].Bottom + 20));

        var debug = PhoneMenu.Debug(new PhoneMenu.Settings(false, true, true, true, true));
        var pages = PhoneMenu.Tiles(Reference, debug);
        Assert.Equal(2504 - 40 - 7 * 142 + 12, pages[0].Y, Close);                    // ‹ Menu on top
        Assert.Equal(2504 - 40, pages[6].Bottom, Close);
    }

    [Fact]
    public void A_question_stands_across_the_screen_over_its_row()
    {
        var main = PhoneMenu.Main(new PhoneMenu.Settings(false, true, true, true, true));
        var restart = PhoneMenu.Tiles(Reference, main)[4];
        var q = PhoneMenu.QuestionAt(Reference, restart);
        Assert.False(q.Below);
        Equal(new(60, restart.Y - 44 - 420, 1100, 420), q.Box);
        Assert.Equal(610, q.TipX, Close);
        Assert.Equal(restart.Y, q.TipY, Close);
        Assert.True(q.Confirm.X > q.Cancel.Right);

        var high = PhoneMenu.QuestionAt(Reference, new PhoneFrame.Box(40, 300, 1140, 130));
        Assert.True(high.Below);
        Assert.Equal(300 + 130 + 44, high.Box.Y, Close);
    }
}
