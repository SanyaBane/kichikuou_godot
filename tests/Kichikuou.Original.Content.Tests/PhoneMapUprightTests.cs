using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneMapUpright"/>: <c>COMMAND</c>'s map on the phone held upright (docs/ui/phone-build.md, step P2), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §3: PM1a, PM1b, PM1c; <c>mock_phone.py</c>'s <c>m1()</c>,
/// <c>status_top</c>, <c>command_panel</c>, <c>province_sheet</c>).
/// </summary>
public class PhoneMapUprightTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Upright(1220, 2712);

    private static readonly PhoneFrame.Box Band = PhoneMapUpright.StatusBand(Reference, 1).Band;

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    [Fact]
    public void The_status_lies_on_top_and_the_map_fills_the_height_down_to_the_bar()
    {
        // status_top(d, TOP - 10, 390): the date at y0 + 26, the budget at y0 + 30 right, the troops at y0 + 86, 32 in.
        var status = PhoneMapUpright.StatusBand(Reference, 1);
        Equal(new(0, 100, 1220, 290), status.Band);
        Assert.Equal(32, status.Left, Close);
        Assert.Equal(1188, status.Right, Close);
        Assert.Equal(126, status.DateTop, Close);
        Assert.Equal(130, status.BudgetTop, Close);
        Assert.Equal(186, status.TroopsTop, Close);
        Assert.Equal(290, PhoneMapUpright.StatusBand(Reference, 2).Band.H, Close);
        Assert.Equal(146 + 2 * 58 + 48 + 38, PhoneMapUpright.StatusBand(Reference, 3).Band.H, Close);
        // m1(): the map from 390 to BAR[0] - 12, its whole height at 2.63 phone pixels a map pixel.
        Equal(new(0, 390, 1220, 2102), PhoneMapUpright.MapBox(Reference, Band));
        // bottom_bar(right=('Commands', …)): 440 wide at the bar's right.
        Equal(new(764, 2504, 440, 148), PhoneMapUpright.CommandsCell(Reference));
    }

    [Fact]
    public void The_weeks_uses_run_across_the_band_in_rows_a_wider_gap_between_groups()
    {
        // status_top: from (32, y0 + 146), 48 high, 8 apart, 22 more after a group, a new row 58 lower past 32 from the right.
        var tags = PhoneMapUpright.Tags(Reference, [[200, 100], [150], [400, 300]]);
        Equal(new(32, 246, 200, 48), tags[0][0]);
        Equal(new(240, 246, 100, 48), tags[0][1]);
        Equal(new(370, 246, 150, 48), tags[1][0]);
        Equal(new(550, 246, 400, 48), tags[2][0]);
        Equal(new(32, 304, 300, 48), tags[2][1]);
        Assert.Equal(2, PhoneMapUpright.TagRows(tags));
        Assert.Equal(1, PhoneMapUpright.TagRows([]));
    }

    [Fact]
    public void The_command_panel_is_a_sheet_over_the_maps_foot_two_columns_from_the_foot_up()
    {
        // command_panel(d, 1634, BAR[0] - 40) in a sheet from 1610: columns 586 wide at 16 and 618, rows 124 high 14 apart.
        var sheet = PhoneMapUpright.Commands(Reference, Band, 5, 5);
        Equal(new(0, 1610, 1220, 882), sheet.Sheet);
        Equal(new(16, 1634, 586, 124), sheet.Caption);
        Equal(new(618, 1634, 586, 124), sheet.TurnEnd);
        Equal(new(16, 2340, 586, 124), sheet.Commands[0]);
        Equal(new(16, 1788, 586, 124), sheet.Commands[4]);
        Equal(new(618, 2340, 586, 124), sheet.Links[0]);
        // A shorter column is as low; the sheet is as tall as the longer one.
        var four = PhoneMapUpright.Commands(Reference, Band, 4, 6);
        Assert.Equal(4, four.Commands.Count);
        Assert.Equal(1788 - 138, four.Links[5].Y, Close);
        Assert.Equal(1610 - 138, four.Sheet.Y, Close);
        // Too many rows for the room: lower, the sheet kept 240 under the band.
        var many = PhoneMapUpright.Commands(Reference, Band, 14, 5);
        Assert.Equal(390 + 240, many.Sheet.Y, Close);
        Assert.True(many.Commands[0].H < 124);
        Assert.True(many.Commands[13].Y >= many.TurnEnd.Bottom);
    }

    [Fact]
    public void A_provinces_card_and_commands_are_a_sheet_the_card_over_the_rows()
    {
        // province_sheet: the picture 420 wide at (30, top + 34), the text at 486, the name at top + 40, the army at + 126,
        // the figures from + 182 50 apart to 40 from the right; the rows across, 128 high 14 apart, from the foot less 24.
        var sheet = PhoneMapUpright.Province(Reference, Band, 3, 2);
        Equal(new(24, 2340, 1172, 128), sheet.Rows[0]);
        Equal(new(24, 2198, 1172, 128), sheet.Rows[1]);
        float top = 2198 - 56 - 332;                            // the figures' foot, 182 + 3 × 50, under the picture's
        Equal(new(0, top, 1220, 2492 - top), sheet.Sheet);
        Equal(new(30, top + 34, 420, 420 * 180f / 272), sheet.Picture);
        Assert.Equal(486, sheet.TextLeft, Close);
        Assert.Equal(1180, sheet.TextRight, Close);
        Assert.Equal(top + 40, sheet.NameTop, Close);
        Assert.Equal(top + 126, sheet.ArmyTop, Close);
        Assert.Equal(top + 182, sheet.FiguresTop, Close);
        Assert.Equal(50, sheet.FigurePitch, Close);
        Assert.Null(sheet.Place);
        // The dungeon's menu: its place's caption over the rows, the card over it.
        var dungeon = PhoneMapUpright.Province(Reference, Band, 0, 3, caption: true);
        Equal(new(24, dungeon.Rows[2].Y - 60, 1172, 60), dungeon.Place!.Value);
        Assert.Equal(dungeon.Place!.Value.Y - 56 - (34 + 420 * 180f / 272), dungeon.Sheet.Y, Close);
    }

    [Fact]
    public void The_maps_view_shows_its_whole_height_and_scrolls_sideways_only()
    {
        var box = PhoneMapUpright.MapBox(Reference, Band);
        var view = new PhoneMapUpright.MapView(box, 0, 0);
        Assert.Equal(2102 / 800f, view.Scale, 0.0001f);
        Assert.Equal(1220 / (2102 / 800f), view.Width, Close);
        // Centred on the script's view (corner 1280, 208: 640 × 400) to a whole column, kept on the map.
        var centred = view.Centred((1280, 208));
        Assert.Equal(MathF.Round(1280 + 320 - view.Width / 2), centred.Left, Close);
        Assert.Equal(MathF.Floor(1920 - view.Width), view.Centred((1280, 0)).At(5000).Left, Close);
        Assert.Equal(MathF.Round(320 - view.Width / 2), view.Centred((0, 0)).Left, Close);
        Assert.Equal(0, view.At(-50).Left, Close);
        // A point and back.
        var (x, y) = centred.FromMap(1500, 600);
        Assert.Equal(box.X + (1500 - centred.Left) * view.Scale, x, Close);
        Assert.Equal(box.Y + 600 * view.Scale, y, Close);
        var (mx, my) = centred.ToMap(x, y);
        Assert.Equal(1500, mx, Close);
        Assert.Equal(600, my, Close);
        Assert.True(centred.Shows(1500, 600));
        Assert.False(centred.Shows(1000, 600));
    }

    [Fact]
    public void A_ringed_province_is_lifted_over_its_sheet_and_kept_off_the_sides()
    {
        var view = new PhoneMapUpright.MapView(PhoneMapUpright.MapBox(Reference, Band), 1000, 0);
        float scale = view.Scale, room = 74 + 30;
        // Under the sheet's top: drawn higher by whole map rows until its ring clears it.
        var lifted = PhoneMapUpright.Lift(Reference, view, 1200, 600, 1810);
        int rows = (int)MathF.Ceiling((390 + 600 * scale + room - 1810) / scale);
        Assert.Equal(rows * scale, lifted.Lift, Close);
        Assert.Equal(rows, lifted.LiftRows);
        Assert.Equal(1000, lifted.Left, Close);
        var (_, y) = lifted.FromMap(1200, 600);
        Assert.InRange(y, 1810 - room - scale, 1810 - room);
        // Over it already: not lifted; at the map's foot: lifted at most by the sheet's height.
        Assert.Equal(0, PhoneMapUpright.Lift(Reference, view, 1200, 300, 1810).Lift, Close);
        Assert.Equal(2492 - 1810, PhoneMapUpright.Lift(Reference, view, 1200, 799, 1810).Lift, Close);
        // By the left side: moved by whole columns as far as its ring needs; by the right likewise.
        var left = PhoneMapUpright.Lift(Reference, view, 1010, 300, 1810);
        Assert.Equal(1000 - MathF.Ceiling((room - 10 * scale) / scale), left.Left, Close);
        var right = PhoneMapUpright.Lift(Reference, view, 1000 + MathF.Floor(view.Width) - 5, 300, 1810);
        Assert.True(right.FromMap(1000 + MathF.Floor(view.Width) - 5, 300).X + room <= right.Box.Right + Close);
        Assert.True(right.Left - 1000 < (room - 5 * scale) / scale + 1);
    }

    [Fact]
    public void A_tap_means_the_nearest_marker_within_4_mm_of_it()
    {
        var view = new PhoneMapUpright.MapView(PhoneMapUpright.MapBox(Reference, Band), 1000, 0);
        MapMarker Marker(int id, int x, int y) => new(MapHitKind.Province, id, $"P{id}", x, y, false, MapRelation.Other, true,
            DungeonKind.Pit, null);
        MapMarker[] markers = [Marker(1, 1100, 300), Marker(2, 1120, 300), Marker(3, 1300, 700)];
        var (x, y) = view.FromMap(1100, 300);
        Assert.Equal(1, PhoneMapUpright.MarkerAt(Reference, view, markers, x + 10, y)!.Id);
        Assert.Equal(2, PhoneMapUpright.MarkerAt(Reference, view, markers, x + 40, y)!.Id);
        // 4 mm is 70 reference pixels: a tap 80 away from every marker means none.
        Assert.Null(PhoneMapUpright.MarkerAt(Reference, view, markers, x - 80, y));
        Assert.Equal(3, PhoneMapUpright.MarkerAt(Reference, view, markers, view.FromMap(1300, 700).X, view.FromMap(1300, 700).Y + 60)!.Id);
    }
}
