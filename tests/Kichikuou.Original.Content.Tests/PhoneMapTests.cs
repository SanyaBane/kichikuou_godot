using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneMap"/>: <c>COMMAND</c>'s map on the phone held landscape (docs/ui/phone-build.md, step L2), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §12: LM1a, LM1b, LM1c).
/// </summary>
public class PhoneMapTests
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
    public void The_left_side_holds_the_status_under_the_Menu_cell()
    {
        // RAIL_L = (116, 370); side_status from y 160, its last line ending by 406.
        Equal(new(116, 160, 254, 246), PhoneMap.StatusBox(Reference));
        Assert.True(PhoneMap.StatusBox(Reference).Bottom < PhoneScenes.SkipCell(Reference).Y);
    }

    [Fact]
    public void The_right_side_holds_the_weeks_uses_by_group_and_Commands_at_the_foot()
    {
        // side_tags from y 24 at RAIL_R = (2342, 2596): a caption 36, tags 46 high 54 apart, 12 after a group.
        var groups = PhoneMap.Tags(Reference, [3, 2, 3]);
        Assert.Equal(3, groups.Count);
        Equal(new(2348, 24, 242, 36), groups[0].Caption);
        Equal(new(2348, 60, 242, 46), groups[0].Tags[0]);
        Equal(new(2348, 168, 242, 46), groups[0].Tags[2]);
        Assert.Equal(234, groups[1].Caption.Y, Close);
        Assert.True(groups[2].Tags[^1].Bottom < PhoneMap.CommandsCell(Reference).Y);
        // commands_cell: (RAIL_R[0], FOOT - 10 - 150, RAIL_R[1], FOOT - 10).
        Equal(new(2342, 1010, 254, 150), PhoneMap.CommandsCell(Reference));
    }

    [Fact]
    public void The_command_panel_takes_both_sides_from_the_foot_up()
    {
        // land_panel_rails: THIS WEEK at 176; five commands 123 high 12 apart, the first at FOOT - 10.
        Equal(new(116, 176, 254, 34), PhoneMap.CaptionBox(Reference));
        var commands = PhoneMap.CommandRows(Reference, 5);
        Equal(new(116, 1037, 254, 123), commands[0]);
        Equal(new(116, 497, 254, 123), commands[4]);
        // Turn END on the right's top; Close at the foot, the items over it, the first lowest.
        Equal(new(2342, 16, 254, 140), PhoneMap.TurnEnd(Reference));
        Equal(new(2342, 1037, 254, 123), PhoneMap.FootCell(Reference));
        var links = PhoneMap.LinkRows(Reference, 5);
        Equal(new(2342, 902, 254, 123), links[0]);
        Equal(new(2342, 362, 254, 123), links[4]);
    }

    [Fact]
    public void Rows_too_many_for_a_rail_are_lower_and_stay_under_its_top()
    {
        var links = PhoneMap.LinkRows(Reference, 8);
        Assert.True(links[0].H < 123);
        Assert.Equal(1025, links[0].Bottom, Close);
        Assert.Equal(PhoneMap.TurnEnd(Reference).Bottom + 30, links[^1].Y, Close);
        var commands = PhoneMap.CommandRows(Reference, 9);
        Assert.True(commands[^1].Y >= PhoneMap.CaptionBox(Reference).Bottom);
        Assert.Empty(PhoneMap.CommandRows(Reference, 0));
    }

    [Fact]
    public void A_provinces_commands_stand_over_Return_and_its_caption_over_them()
    {
        // province_rail: Attack at FOOT - 10 - 2h - gap, Ninja Maneuver over it.
        var rows = PhoneMap.ProvinceRows(Reference, 2);
        Equal(new(2342, 902, 254, 123), rows[0]);
        Equal(new(2342, 767, 254, 123), rows[1]);
        var place = PhoneMap.PlaceBox(Reference, rows);
        Assert.Equal(767, place.Bottom, Close);
        // The dungeon's menu keeps its caption's room under the side's top.
        var many = PhoneMap.ProvinceRows(Reference, 9, caption: true);
        Assert.True(PhoneMap.PlaceBox(Reference, many).Y >= 16 - Close);
    }

    [Fact]
    public void A_provinces_card_stands_left_of_it_when_the_frame_has_room()
    {
        // province_card: 560 wide, 90 from the province, its middle level with it, kept over FOOT.
        var card = PhoneMap.ProvinceCard(Reference, (1980, 600), 3);
        float h = 24 + 512 * 180f / 272 + 16 + 72 + 3 * 44 + 24;
        Equal(new(1980 - 90 - 560, 600 - h / 2, 560, h), card.Box);
        Equal(new(1354, 600 - h / 2 + 24, 512, 512 * 180f / 272), card.Picture);
        Assert.Equal(card.Picture.Bottom + 16, card.NameTop, Close);
        Assert.Equal(card.NameTop + 72, card.FiguresTop, Close);
        // A long name has the army under it: the figures a line lower, the card as much taller.
        var two = PhoneMap.ProvinceCard(Reference, (1980, 600), 3, armyUnder: true);
        Assert.Equal(two.NameTop + 72 + 36, two.FiguresTop, Close);
        Assert.Equal(h + 36, two.Box.H, Close);
        // Low on the map it stays over the gesture zone.
        Assert.Equal(1170 - 20, PhoneMap.ProvinceCard(Reference, (1980, 1100), 3).Box.Bottom, Close);
    }

    [Fact]
    public void A_card_with_no_room_left_of_its_province_stands_right_of_it()
    {
        Assert.Equal(500 + 90, PhoneMap.ProvinceCard(Reference, (500, 400), 3).Box.X, Close);
        // Out of view: in the frame's middle.
        var middle = PhoneMap.ProvinceCard(Reference, null, 3).Box;
        Assert.Equal(380 + (1952 - 560) / 2f, middle.X, Close);
    }

    [Fact]
    public void The_rails_follow_the_canvas_and_the_systems_hole()
    {
        // The Poco on the project's canvas (800 high), its hole of 120 phone pixels reported on the left only: both sides keep it.
        float unit = 800f / 1220, width = 2712 * unit;
        var poco = PhoneFrame.Landscape(width, 800, new PhoneFrame.Insets(120 * unit, 0, 0, 0));
        var status = PhoneMap.StatusBox(poco);
        Assert.Equal((120 + 6) * unit, status.X, Close);
        Assert.Equal(poco.Frame.X - 10 * unit, status.Right, Close);
        var commands = PhoneMap.CommandsCell(poco);
        Assert.Equal(poco.Frame.Right + 10 * unit, commands.X, Close);
        Assert.Equal(width - (120 + 6) * unit, commands.Right, Close);
        Assert.Equal(800 - 50 * unit - 10 * unit, commands.Bottom, Close);
    }
}
