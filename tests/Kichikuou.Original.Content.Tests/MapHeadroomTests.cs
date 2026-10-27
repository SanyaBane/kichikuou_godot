using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The map's room under our status strip (docs/ui/command-screen.md, "The map's room under the strip").</summary>
public class MapHeadroomTests
{
    [Fact]
    public void At_the_maps_top_its_first_row_lies_right_under_the_strip()
    {
        Assert.Equal(MapHeadroom.Strip, MapHeadroom.Shift(0));
        Assert.Equal(-MapHeadroom.Strip, MapHeadroom.Shown(0));
        Assert.Equal(0, MapHeadroom.Shift(MapHeadroom.Range));
        Assert.Equal(0, MapHeadroom.Shift(MapHeadroom.MaxTop));
    }

    [Fact]
    public void The_room_closes_a_row_at_a_time_so_the_picture_never_stands_still_or_turns()
    {
        for (int top = 0; top < MapHeadroom.MaxTop; top++)
        {
            int step = MapHeadroom.Shift(top) - MapHeadroom.Shift(top + 1);
            Assert.InRange(step, 0, 1);
            Assert.InRange(MapHeadroom.Shown(top + 1) - MapHeadroom.Shown(top), 1, 2);
        }
    }

    [Fact]
    public void TopShowing_finds_the_camera_for_a_row_at_the_screens_top()
    {
        for (int top = 0; top <= MapHeadroom.MaxTop; top++)
            Assert.Equal(top, MapHeadroom.TopShowing(MapHeadroom.Shown(top)));
        Assert.Equal(0, MapHeadroom.TopShowing(-60));
        Assert.Equal(MapHeadroom.MaxTop, MapHeadroom.TopShowing(700));
        for (int row = MapHeadroom.Shown(0); row <= MapHeadroom.MaxTop; row++)
            Assert.InRange(row - MapHeadroom.Shown(MapHeadroom.TopShowing(row)), 0, 1);
    }

    [Fact]
    public void The_pointer_reads_the_views_row_under_it()
    {
        var room = new MapHeadroom();
        Assert.Equal(75, room.ScriptY(100, 0, held: false));
        Assert.Equal(100 - MapHeadroom.Shift(40), room.ScriptY(100, 40, held: false));
        Assert.Equal(100, room.ScriptY(100, 250, held: false));
        Assert.Equal(0, room.ScriptY(10, 0, held: false));          // over the strip: the map's top row
        Assert.Equal(399, room.ScriptY(399, 300, held: false));
    }

    [Fact]
    public void While_the_room_opens_the_pointer_reads_the_picture_as_drawn()
    {
        var room = new MapHeadroom();
        Assert.Equal(100 - 10, room.ScriptY(100, 0, held: false, shift: 10));
        int press = room.ScriptY(120, 0, held: true, shift: 10);
        Assert.Equal(110, press);
        Assert.Equal(press, room.ScriptY(120, 0, held: true));               // the room open now: the press keeps its shift
        Assert.Equal(press, room.ScriptY(120, 0, held: false));              // a still click: one point
        Assert.Equal(120 - MapHeadroom.Shift(0), room.ScriptY(120, 0, held: false));
    }

    [Fact]
    public void A_still_click_reads_one_point()
    {
        var room = new MapHeadroom();
        int press = room.ScriptY(120, 30, held: true);
        Assert.Equal(press, room.ScriptY(120, 30, held: true));
        Assert.Equal(press, room.ScriptY(120, 30, held: false));
    }

    /// <summary>
    /// A drag from the middle down past the map's top and back up: the script moves the camera by the steps it reads
    /// (<c>WAO_MAP.ADV:29-43</c>, its top saturating at 0 and kept to 400), and the row the press took stays under the
    /// pointer (a row off where a camera's step moves the picture by two) until the camera stops at the map's top; there
    /// the pointer takes the row under it, and the first step back scrolls.
    /// </summary>
    [Fact]
    public void A_drag_keeps_the_pressed_row_under_the_pointer()
    {
        var room = new MapHeadroom();
        int top = 150, y = 60;
        int previous = room.ScriptY(y, top, held: true);
        int row = y + MapHeadroom.Shown(top);
        bool stopped = false;
        for (y = 61; y < 400; y++)
        {
            Step();
            if (top == 0)
            {
                stopped = true;                                     // the camera at the map's top: the pointer goes on alone
                row = y + MapHeadroom.Shown(0);
                continue;
            }
            Assert.False(stopped);
            Assert.InRange(row - (y + MapHeadroom.Shown(top)), 0, 1);
        }
        Assert.True(stopped);
        for (y = 398; y >= 30; y--)
        {
            Step();
            Assert.True(top > 0);
            Assert.InRange(row - (y + MapHeadroom.Shown(top)), 0, 1);
        }

        void Step()
        {
            int read = room.ScriptY(y, top, held: true);
            top = Math.Clamp(top + previous - read, 0, MapHeadroom.MaxTop);
            previous = read;
        }
    }

    [Fact]
    public void A_release_lets_the_pressed_row_go()
    {
        var room = new MapHeadroom();
        room.ScriptY(200, 50, held: true);
        Assert.Equal(200 - MapHeadroom.Shift(50), room.ScriptY(200, 50, held: false));
        Assert.Equal(300 - MapHeadroom.Shift(10), room.ScriptY(300, 10, held: true));   // a new press takes its own row
    }
}
