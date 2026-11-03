using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneOptions"/> and <see cref="PhoneLadder"/>: the options window and the dungeon's menus on the phone held
/// landscape (docs/ui/phone-build.md, step L8), at the mock-ups' figures on the reference phone (docs/ui/phone.md, §16: LO1,
/// LF2, LF2e).
/// </summary>
public class PhoneOptionsTests
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
    public void The_tabs_stand_on_the_sheet_from_its_left_the_title_at_its_right()
    {
        // lo1: x = LX0 + i * (330 + 10), 16 (+ 12 when not chosen) down to 16 + 110; the title at LX1 - 4, 20.
        Equal(new(116, 16, 330, 110), PhoneOptions.Tab(Reference, 0, chosen: true));
        Equal(new(456, 28, 330, 98), PhoneOptions.Tab(Reference, 1, chosen: false));
        Equal(new(1476, 28, 330, 98), PhoneOptions.Tab(Reference, 4, chosen: false));
        Assert.Equal(2592, PhoneOptions.TitleRight(Reference), Close);
        Equal(new(116, 126, 2480, 1034), PhoneOptions.Sheet(Reference));                 // (LX0, 16 + h, LX1, FOOT - 10)
    }

    [Fact]
    public void The_sheet_holds_two_columns_either_side_of_its_middle()
    {
        // messages_columns: mid = 1356; the left (x0 + 40, y0 + 36, mid - 30), the right (mid + 30, …, x1 - 40); the
        // divider (mid, y0 + 40, y1 - 40), where the columns end.
        Equal(new(156, 162, 1170, 958), PhoneOptions.Column(Reference, 0));
        Equal(new(1386, 162, 1170, 958), PhoneOptions.Column(Reference, 1));
        Equal(new(156, 162, 2400, 958), PhoneOptions.Across(Reference));
        Assert.Equal((1356f, 166f, 1120f), PhoneOptions.Divider(Reference));
    }

    [Fact]
    public void The_one_column_scrolls_between_the_sheet_s_top_and_Defaults()
    {
        // owner, 2026-11-03: the view (across.x, y0 + 12, across.w, Defaults.y - 20 - its top), the column from y0 + 36; it
        // scrolls until its foot stands 20 over the view's; the bar's groove 20 wide, 10 in from the sheet's right.
        Equal(new(156, 138, 2400, 842), PhoneOptions.View(Reference));
        Assert.Equal(162, PhoneOptions.ColumnTop(Reference, 0), Close);
        Assert.Equal(62, PhoneOptions.ColumnTop(Reference, 100), Close);
        Assert.Equal(0, PhoneOptions.MaxScroll(Reference, 798), Close);
        Assert.Equal(402, PhoneOptions.MaxScroll(Reference, 1200), Close);
        Assert.Null(PhoneOptions.ScrollBar(Reference, 798, 0));
        var (track, thumb) = PhoneOptions.ScrollBar(Reference, 1200, 402)!.Value;
        Equal(new(2566, 138, 20, 842), track);
        Assert.Equal(track.Bottom - 3, thumb.Bottom, Close);                                 // at its end
    }

    [Fact]
    public void Defaults_and_Close_stand_at_the_right_column_s_foot()
    {
        // lo1: foot = (FOOT - 10 - 30 - 130, FOOT - 10 - 30); Defaults (mid + 30, …, mid + 30 + 380), Close (mid + 430, …, LX1 - 40).
        Equal(new(1386, 1000, 380, 130), PhoneOptions.Defaults(Reference));
        Equal(new(1786, 1000, 770, 130), PhoneOptions.Close(Reference));
        Assert.True(PhoneOptions.Close(Reference).Bottom <= Reference.Screen.H - Reference.Safe.Bottom);
    }

    [Fact]
    public void The_parts_scale_gives_PO1_s_type()
    {
        // PO1: the names 2.3 mm (40), the explanations 1.7 mm (30), the boxes 2.6 mm (46) — the PC's 17, 13 and 20.
        Assert.Equal(40, 17 * PhoneOptions.PartScale, 1f);
        Assert.Equal(30, 13 * PhoneOptions.PartScale, 1f);
        Assert.Equal(46, 20 * PhoneOptions.PartScale, 1f);
        var question = PhoneOptions.Question(Reference);
        Assert.Equal(1356, question.X + question.W / 2, Close);
        Assert.Equal(643, question.Y + question.H / 2, Close);
    }

    [Fact]
    public void A_longer_screen_widens_the_sheet_and_its_columns()
    {
        var longer = PhoneFrame.Landscape(2800, 1220);
        Assert.Equal(PhoneOptions.Sheet(Reference).W + 88, PhoneOptions.Sheet(longer).W, Close);
        Assert.Equal(PhoneOptions.Column(Reference, 0).W + 44, PhoneOptions.Column(longer, 0).W, Close);
        Equal(PhoneOptions.Tab(Reference, 2, chosen: true), PhoneOptions.Tab(longer, 2, chosen: true));
        Assert.Equal(PhoneOptions.Close(Reference).W + 44, PhoneOptions.Close(longer).W, Close);
    }

    [Fact]
    public void The_ladder_stands_at_the_frame_s_right_foot_and_grows_upwards()
    {
        // lf2_rail: x1 = FX1 - 16 = 2316, x0 = x1 - 356; floor_rail from foot = FOOT - 10 = 1160 up: Descend (150), 14, the
        // floor (110), 14, Ascend, 14, Retreat; the panel 14 round it, from foot - 602 - 14.
        Equal(new(1960, 1010, 356, 150), PhoneLadder.Way(Reference, down: true));
        Equal(new(1960, 886, 356, 110), PhoneLadder.Floor(Reference));
        Equal(new(1960, 722, 356, 150), PhoneLadder.Way(Reference, down: false));
        Equal(new(1960, 558, 356, 150), PhoneLadder.Retreat(Reference));
        Equal(new(1946, 544, 384, 630), PhoneLadder.LadderPanel(Reference));
        Assert.Equal(Reference.Frame.Right, PhoneLadder.LadderPanel(Reference).Right + 2, Close);    // in the frame
    }

    [Fact]
    public void Before_a_search_Return_stands_over_Start_Search_and_the_other_items_between_them()
    {
        // lf2e: Start Search in Descend's place, Return 14 over it (foot - 314), the panel round both.
        Equal(new(1960, 1010, 356, 150), PhoneLadder.Start(Reference));
        Equal(new(1960, 846, 356, 150), PhoneLadder.Return(Reference, 0));
        Equal(new(1946, 832, 384, 342), PhoneLadder.EntryPanel(Reference, 0));
        // Two other items (Merim, God's Door): the first highest, Return over them.
        Equal(new(1960, 682, 356, 150), PhoneLadder.Other(Reference, 0, 2));
        Equal(new(1960, 846, 356, 150), PhoneLadder.Other(Reference, 1, 2));
        Equal(new(1960, 518, 356, 150), PhoneLadder.Return(Reference, 2));
    }
}
