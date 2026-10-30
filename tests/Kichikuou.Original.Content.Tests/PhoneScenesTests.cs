using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneScenes"/> and <see cref="PhoneMenu"/>: the scenes' parts and the Menu sheet on the phone held landscape
/// (docs/ui/phone-build.md, step L1), at the mock-ups' figures on the reference phone (docs/ui/phone.md, §19, §26).
/// </summary>
public class PhoneScenesTests
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
    public void The_left_sides_cells_are_the_mock_ups_rail()
    {
        // RAIL_L = (116, 370): Menu on top, Log at the foot over the gesture zone, Skip over it.
        Equal(new(116, 16, 254, 110), PhoneScenes.MenuCell(Reference));
        Equal(new(116, 1042, 254, 118), PhoneScenes.LogCell(Reference));
        Equal(new(116, 908, 254, 118), PhoneScenes.SkipCell(Reference));
    }

    [Fact]
    public void The_cells_follow_the_canvas_and_the_systems_hole()
    {
        // The Poco on the project's canvas (800 high), its hole of 120 phone pixels reported on the left only.
        float unit = 800f / 1220, width = 2712 * unit;
        var poco = PhoneFrame.Landscape(width, 800, new PhoneFrame.Insets(120 * unit, 0, 0, 0));
        var menu = PhoneScenes.MenuCell(poco);
        Assert.Equal((120 + 6) * unit, menu.X, Close);
        Assert.Equal(poco.Frame.X - 10 * unit, menu.Right, Close);
        Assert.Equal(800 - 50 * unit - 10 * unit, PhoneScenes.LogCell(poco).Bottom, Close);
    }

    [Fact]
    public void The_log_paper_runs_from_the_frames_left_edge_to_the_right_rooms()
    {
        // LR2: (RAIL_L[1] + 12, 14, LX1, FOOT - 6), LX1 = 2712 - 110 - 6.
        Equal(new(382, 14, 2596 - 382, 1164 - 14), PhoneScenes.LogPaper(Reference));
    }

    [Fact]
    public void A_short_menus_rows_stand_at_the_frames_right_foot()
    {
        var rows = PhoneScenes.SceneMenu(Reference, 3, 300);
        Assert.Equal(3, rows.Rows.Count);
        // LS1m: x1 = FX1 - 30, 500 wide, 123 high, 12 apart, the last one's foot at FOOT - 10.
        Equal(new(2302 - 500, 1160 - 3 * 123 - 2 * 12, 500, 123), rows.Rows[0]);
        Equal(new(2302 - 500, 1160 - 123, 500, 123), rows.Rows[2]);
        Equal(new(2302 - 500 - 16, 1160 - 393 - 16, 532, 393 + 32), rows.Panel);
        Assert.Equal(48, rows.TextSize, Close);
    }

    [Fact]
    public void A_long_item_widens_the_rows_up_to_half_the_frame()
    {
        Assert.Equal(700 + 48, PhoneScenes.SceneMenu(Reference, 2, 700).Rows[0].W, Close);
        Assert.Equal(1952 / 2f, PhoneScenes.SceneMenu(Reference, 2, 2000).Rows[0].W, Close);
    }

    [Fact]
    public void A_menu_too_long_for_the_height_has_lower_rows_that_fit()
    {
        var rows = PhoneScenes.SceneMenu(Reference, 12, 300);
        Assert.Equal(12, rows.Rows.Count);
        Assert.True(rows.Rows[0].H < 123);
        Assert.True(rows.Panel.Y >= 16 - Close);
        Assert.Equal(1160, rows.Rows[11].Bottom, Close);
        Assert.Equal(48 * rows.Rows[0].H / 123, rows.TextSize, Close);
    }

    [Fact]
    public void The_week_reports_card_is_the_mock_ups_width_unless_it_is_too_high()
    {
        // The PC's card is 600 wide; LW1's 1320 phone pixels at 1.525 a layout pixel.
        Assert.Equal(1320 / (600 * 1.525f), PhoneScenes.ReportScale(Reference, 600, 474), Close);
        Assert.Equal((1220 - 120) / (700 * 1.525f), PhoneScenes.ReportScale(Reference, 600, 700), Close);
    }

    [Fact]
    public void The_menu_sheets_tiles_are_four_a_row_in_the_frames_middle()
    {
        // LN3: the panel (FX0 + 160, 150, FX1 - 160, FOOT - 60), the tiles under its title, two rows.
        Equal(new(540, 150, 1632, 960), PhoneMenu.Panel(Reference));
        var tiles = PhoneMenu.Tiles(Reference, 7);
        float w = (1632 - 60 - 48) / 4f, h = (960 - 170 - 16) / 2f;
        Equal(new(570, 290, w, h), tiles[0]);
        Equal(new(570 + 3 * (w + 16), 290, w, h), tiles[3]);
        Equal(new(570 + 2 * (w + 16), 290 + h + 16, w, h), tiles[6]);
        Assert.Equal(5, PhoneMenu.TileAt(Reference, 7, 570 + w + 16 + 1, 290 + h + 16 + 1));
        Assert.Equal(-1, PhoneMenu.TileAt(Reference, 7, 570 + w + 8, 300));            // the gap between two tiles
        Assert.Equal(-1, PhoneMenu.TileAt(Reference, 7, 570 + 3 * (w + 16) + 1, 290 + h + 16 + 1));   // no eighth tile
    }

    [Fact]
    public void A_question_stands_over_its_tile_or_under_it_near_the_top()
    {
        var tiles = PhoneMenu.Tiles(Reference, 7);
        var restart = PhoneMenu.QuestionAt(Reference, tiles[4]);
        Assert.False(restart.Below);
        Equal(new(570, tiles[4].Y - 30 - 420, 1100, 420), restart.Box);
        Assert.Equal(tiles[4].X + tiles[4].W / 2, restart.TipX, Close);
        Assert.Equal(tiles[4].Y, restart.TipY, Close);
        Assert.True(restart.Cancel.X > restart.Confirm.Right);              // a way back right of the action (owner, 2026-10-30)
        Assert.Equal(restart.Box.Right - 40, restart.Cancel.Right, Close);

        var captures = PhoneMenu.QuestionAt(Reference, tiles[1]);       // the first row: no room above it
        Assert.True(captures.Below);
        Assert.Equal(tiles[1].Bottom + 30, captures.Box.Y, Close);
        Assert.Equal(tiles[1].Bottom, captures.TipY, Close);

        var exit = PhoneMenu.QuestionAt(Reference, tiles[6]);           // kept within the panel
        Assert.Equal(PhoneMenu.Panel(Reference).Right - 30, exit.Box.Right, Close);
    }

    [Fact]
    public void The_sheets_pages_are_the_menu_bars_commands()
    {
        var settings = new PhoneMenu.Settings(SkipOn: false, SkipEnabled: true, SoundOn: true, Development: true, HideDimmedNames: true);
        var main = PhoneMenu.Main(settings);
        Assert.Equal(["Options", "Message skip", "Sound", "Screenshot", "Restart", "Exit", "Debug"], main.Select(t => t.Label));
        Assert.Equal(["", "Off", "On", "", "asks first", "asks first", "›"], main.Select(t => t.Sub));
        Assert.DoesNotContain(PhoneMenu.Main(settings with { Development = false }), t => t.Command == PhoneMenu.Command.Debug);
        Assert.True(PhoneMenu.Main(settings with { SkipEnabled = false })[1].Disabled);

        var debug = PhoneMenu.Debug(settings);
        Assert.Equal(PhoneMenu.Command.Back, debug[0].Command);
        Assert.Equal("", debug[0].Sub);
        Assert.Equal("On", debug.Single(t => t.Command == PhoneMenu.Command.HideDimmedNames).Sub);
        Assert.All(main.Concat(debug).Where(t => t.Kind == PhoneMenu.Kind.Asks), t => Assert.NotNull(PhoneMenu.QuestionOf(t.Command)));
        Assert.Equal(new PhoneMenu.Question("Restart game?", PhoneMenu.LoseProgress, "Restart"), PhoneMenu.QuestionOf(PhoneMenu.Command.Restart));
        Assert.Null(PhoneMenu.QuestionOf(PhoneMenu.Command.Options));
    }
}
