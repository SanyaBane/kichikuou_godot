using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="ScreenMargins"/>, the frame inside them (<see cref="PhoneFrame.Within"/>) and their calibration
/// (<see cref="MarginsCalibration"/>; docs/ui/screen-margins.md).
/// </summary>
public class ScreenMarginsTests
{
    private const float Close = 0.01f;
    private const float PerMm = MarginsCalibration.ReferencePerMillimetre;     // the reference phone at its own pixels

    [Fact]
    public void A_margin_is_held_within_none_and_eight_millimetres()
    {
        var m = new ScreenMargins(28, 28, 28);
        Assert.Equal(new ScreenMargins(29, 28, 28), m.Stepped(ScreenMargins.Edge.Top, 1));
        Assert.Equal(new ScreenMargins(28, 0, 28), m.Stepped(ScreenMargins.Edge.Bottom, -40));
        Assert.Equal(new ScreenMargins(28, 28, ScreenMargins.Max), m.With(ScreenMargins.Edge.Sides, 500));
        Assert.Equal(new ScreenMargins(0, 80, 5), new ScreenMargins(-3, 99, 5).Clamped());
    }

    [Fact]
    public void Display_tells_a_set_in_words()
    {
        Assert.Equal("none", ScreenMargins.None.Words);
        Assert.Equal("2.8 mm all round", new ScreenMargins(28, 28, 28).Words);
        Assert.Equal("top 1.5 · bottom 2.8 · sides 0.0 mm", new ScreenMargins(15, 28, 0).Words);
        Assert.Equal("0.5 mm", ScreenMargins.Millimetres(5));
    }

    [Fact]
    public void The_sides_margin_is_both_sides_inset()
    {
        var insets = new ScreenMargins(10, 20, 30).Insets(PerMm);
        Assert.Equal(3 * PerMm, insets.Left, Close);
        Assert.Equal(3 * PerMm, insets.Right, Close);
        Assert.Equal(1 * PerMm, insets.Top, Close);
        Assert.Equal(2 * PerMm, insets.Bottom, Close);
    }

    [Fact]
    public void A_margin_within_the_cameras_strip_moves_nothing_there()
    {
        // Landscape on the reference phone with 2.8 mm all round: the room is the screen less them, the camera's strip
        // (110) less the margin still kept free on both sides, the gesture zone (50) wholly under the bottom's margin.
        var m = new ScreenMargins(28, 28, 28).Insets(PerMm);
        var phone = PhoneFrame.Landscape(2712 - m.Left - m.Right, 1220 - m.Top - m.Bottom, margins: m);
        Assert.Equal(110 - m.Left, phone.Safe.Left, 0.5f);
        Assert.Equal(110 - m.Right, phone.Safe.Right, 0.5f);
        Assert.Equal(0, phone.Safe.Top, Close);
        Assert.Equal(Math.Max(0, 50 - m.Bottom), phone.Safe.Bottom, Close);           // the zone is the whole screen's
        Assert.Equal(1220 - m.Top - m.Bottom, phone.Frame.H, Close);             // the picture at the room's height
    }

    [Fact]
    public void Upright_the_room_inside_the_margins_scales_the_layout_by_its_width()
    {
        var m = new ScreenMargins(28, 28, 28).Insets(PerMm);
        float w = 1220 - m.Left - m.Right;
        var phone = PhoneFrame.Upright(w, 2712 - m.Top - m.Bottom, margins: m);
        Assert.Equal(w / 1220, phone.Unit, Close);
        Assert.Equal(0, phone.Safe.Left, Close);
        Assert.Equal(110 - m.Top, phone.Safe.Top, Close);                         // the camera's strip left under the margin
        Assert.Equal(w, phone.Frame.W, Close);
    }

    [Fact]
    public void Without_margins_the_frame_is_as_before()
    {
        Assert.Equal(PhoneFrame.Landscape(2712, 1220).Safe, PhoneFrame.Landscape(2712, 1220, margins: new(0, 0, 0, 0)).Safe);
        Assert.Equal(PhoneFrame.For(1220, 2712).Frame, PhoneFrame.For(1220, 2712, upright: true).Frame);
    }

    [Fact]
    public void The_tabs_are_taken_out_of_the_systems_gestures()
    {
        // Android's back gesture takes a strip of the sides, its home gesture the foot: a tab's ridges, where the finger
        // takes it, stand 9 mm × 0.62 in from the frame's edge, which is itself the margin in from the screen's.
        foreach (bool upright in new[] { true, false })
        {
            var c = upright ? MarginsCalibration.For(1220, 2712, default, PerMm, true)
                : MarginsCalibration.For(2712, 1220, default, PerMm, false);
            var (_, topY) = c.Ridges(MarginsCalibration.Side.Top);
            var (_, bottomY) = c.Ridges(MarginsCalibration.Side.Bottom);
            var (leftX, _) = c.Ridges(MarginsCalibration.Side.Left);
            var (rightX, _) = c.Ridges(MarginsCalibration.Side.Right);
            Assert.True(topY > 5 * PerMm);
            Assert.True(c.Screen.H - bottomY > 5 * PerMm);
            Assert.True(leftX > 5 * PerMm);
            Assert.True(c.Screen.W - rightX > 5 * PerMm);
        }
    }

    [Fact]
    public void The_tab_on_the_cameras_edge_stands_off_the_hole()
    {
        var up = MarginsCalibration.For(1220, 2712, default, PerMm, true);
        var top = up.Tab(MarginsCalibration.Side.Top);
        Assert.True(top.Right < 1220 / 2 - 30);                                    // the hole: 48 wide in the middle
        var land = MarginsCalibration.For(2712, 1220, default, PerMm, false);
        Assert.True(land.Tab(MarginsCalibration.Side.Left).Bottom < 1220 / 2 - 30);
        Assert.True(land.Tab(MarginsCalibration.Side.Right).Bottom < 1220 / 2 - 30);   // either side may hold it
    }

    [Fact]
    public void The_frame_follows_the_margins_and_the_panel_stays_in_it()
    {
        foreach (var (w, h, perMm, upright) in new[]
                 {
                     (1220f, 2712f, PerMm, true), (2712f, 1220f, PerMm, false),
                     (1280f, 800f, 800f / 167, false),                               // the tablet's canvas, 167 mm high
                 })
        {
            foreach (var set in new[] { ScreenMargins.None, new ScreenMargins(ScreenMargins.Max, ScreenMargins.Max, ScreenMargins.Max) })
            {
                var m = set.Insets(perMm);
                var c = MarginsCalibration.For(w, h, m, perMm, upright);
                Assert.Equal(m.Left, c.Frame.X, Close);
                Assert.Equal(h - m.Bottom, c.Frame.Bottom, Close);
                var p = c.Panel;
                Assert.True(p.X >= c.Frame.X && p.Right <= c.Frame.Right && p.Y >= c.Frame.Y && p.Bottom <= c.Frame.Bottom,
                    $"{w}×{h}, {set}: the panel {p} leaves the frame {c.Frame}");
                foreach (var side in Enum.GetValues<MarginsCalibration.Side>())
                {
                    var t = c.Tab(side);
                    Assert.False(t.X < p.Right && t.Right > p.X && t.Y < p.Bottom && t.Bottom > p.Y, $"{side}'s tab under the panel");
                }
            }
        }
    }

    [Fact]
    public void A_press_lands_on_what_is_under_it()
    {
        var c = MarginsCalibration.For(1220, 2712, default, PerMm, true);
        static (float, float) Mid(PhoneFrame.Box b) => (b.X + b.W / 2, b.Y + b.H / 2);
        var (x, y) = Mid(c.Minus(ScreenMargins.Edge.Bottom));
        Assert.Equal(new MarginsCalibration.Target(MarginsCalibration.Kind.Minus, ScreenMargins.Edge.Bottom), c.Hit(x, y));
        (x, y) = Mid(c.Plus(ScreenMargins.Edge.Sides));
        Assert.Equal(new MarginsCalibration.Target(MarginsCalibration.Kind.Plus, ScreenMargins.Edge.Sides), c.Hit(x, y));
        (x, y) = Mid(c.Done);
        Assert.Equal(MarginsCalibration.Kind.Done, c.Hit(x, y).Kind);
        (x, y) = Mid(c.Reset);
        Assert.Equal(MarginsCalibration.Kind.Reset, c.Hit(x, y).Kind);
        (x, y) = c.Ridges(MarginsCalibration.Side.Right);
        Assert.Equal(new MarginsCalibration.Target(MarginsCalibration.Kind.Tab, ScreenMargins.Edge.Sides, MarginsCalibration.Side.Right),
            c.Hit(x, y));
        var label = c.Label(ScreenMargins.Edge.Top);
        Assert.Equal(MarginsCalibration.Kind.None, c.Hit(label.X + 5, label.Y).Kind);   // the panel itself takes nothing
        Assert.Equal(MarginsCalibration.Kind.None, c.Hit(600, 1900).Kind);              // nor the screen under it
    }

    [Fact]
    public void An_edge_follows_the_fingers_move_inward()
    {
        var start = new ScreenMargins(10, 10, 10);
        Assert.Equal(new ScreenMargins(20, 10, 10), MarginsCalibration.Dragged(start, MarginsCalibration.Side.Top, 0, PerMm, PerMm));
        Assert.Equal(new ScreenMargins(10, 15, 10), MarginsCalibration.Dragged(start, MarginsCalibration.Side.Bottom, 0, -PerMm / 2, PerMm));
        Assert.Equal(new ScreenMargins(10, 10, 30), MarginsCalibration.Dragged(start, MarginsCalibration.Side.Left, 2 * PerMm, 0, PerMm));
        Assert.Equal(new ScreenMargins(10, 10, 0), MarginsCalibration.Dragged(start, MarginsCalibration.Side.Right, 5 * PerMm, 0, PerMm));
        Assert.Equal(new ScreenMargins(10, 10, 20), MarginsCalibration.Dragged(start, MarginsCalibration.Side.Right, -PerMm, 0, PerMm));
    }
}
