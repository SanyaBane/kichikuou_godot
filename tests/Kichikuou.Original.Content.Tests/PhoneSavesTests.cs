using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneSaves"/>: our Save/Load screen on the phone held landscape (docs/ui/phone-build.md, step L7), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §15: LL1; §26: LY1).
/// </summary>
public class PhoneSavesTests
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
    public void The_list_stands_at_the_left_its_band_with_Load_and_Save()
    {
        // ll1: the list (LX0, 124, LL1_SPLIT, FOOT - 10) from LX0 = SIDE + 6 = 116 to 1000; load_save_chips at
        // (LL1_SPLIT - 312, 18), 150 × 80 (owner, 2026-11-04), 12 apart; lacquer_tabs at 124, 96 high, 12 apart.
        Equal(new(116, 0, 884, 1160), PhoneSaves.List(Reference));
        Equal(new(688, 18, 150, 80), PhoneSaves.Chip(Reference, save: false));
        Equal(new(850, 18, 150, 80), PhoneSaves.Chip(Reference, save: true));
        Equal(new(116, 124, 436, 96), PhoneSaves.Tab(Reference, auto: false));
        Equal(new(564, 124, 436, 96), PhoneSaves.Tab(Reference, auto: true));
    }

    [Fact]
    public void The_heading_holds_the_sweep_and_seven_rows_are_in_view()
    {
        // land_save_list: the heading 16 under the tabs, the sweep (x1 - 440, y, x1, y + 64); the rows 84 under its top, 104
        // high and 10 apart, (y1 - y + 10) // 114 = 7 of them, ending 30 short of the right; the scroll bar at x1 - 14.
        Equal(new(560, 236, 440, 64), PhoneSaves.Sweep(Reference));
        Assert.Equal(268, PhoneSaves.HeadMiddle(Reference), Close);
        Assert.Equal(7, PhoneSaves.RowsShown(Reference, newRow: false));
        Equal(new(116, 320, 854, 104), PhoneSaves.Row(Reference, 0, newRow: false));
        Equal(new(116, 1004, 854, 104), PhoneSaves.Row(Reference, 6, newRow: false));
        Equal(new(980, 320, 20, 788), PhoneSaves.ScrollBar(Reference, newRow: false));        // the reference, S2
        Equal(new(116, 320, 884, 788), PhoneSaves.Scrolled(Reference, newRow: false));
    }

    [Fact]
    public void New_save_heads_the_rows_and_takes_one_from_view()
    {
        Equal(new(116, 320, 854, 88), PhoneSaves.New(Reference));
        Assert.Equal(6, PhoneSaves.RowsShown(Reference, newRow: true));
        Equal(new(116, 418, 854, 104), PhoneSaves.Row(Reference, 0, newRow: true));
        Equal(new(116, 320, 884, 772), PhoneSaves.Scrolled(Reference, newRow: true));
    }

    [Fact]
    public void The_page_holds_the_map_cut_lower_the_figures_and_the_comment()
    {
        // ll1: the page (LL1_SPLIT + 40, 14, LX1, foot[0] - 16), foot = (FOOT - 160, FOOT - 10); land_save_page: Delete
        // (x1 - 240, y, x1, y + 84), the map 100 under the top at the page's width cut to leave 18 + 2 × 92 + 10 + 136 under
        // it, the figures 18 under it in three columns, the comment's label, its box (x0, y + 36, x1 - 220, y + 136), Edit
        // (x1 - 204, …, x1).
        Equal(new(1040, 14, 1556, 980), PhoneSaves.Page(Reference));
        Equal(new(2356, 14, 240, 84), PhoneSaves.Delete(Reference));
        Equal(new(1040, 114, 1556, 532), PhoneSaves.Map(Reference, lines: false));
        var (x, y, w) = PhoneSaves.Figure(Reference, 4, lines: false);
        Assert.Equal(1040 + 8 + 1556f / 3, x, Close);
        Assert.Equal(664 + 92, y, Close);
        Assert.Equal(1556f / 3, w, Close);
        Assert.Equal(858, PhoneSaves.CommentLabel(Reference, lines: false), Close);
        Equal(new(1040, 894, 1336, 100), PhoneSaves.Comment(Reference, lines: false));
        Equal(new(2392, 894, 204, 100), PhoneSaves.Edit(Reference, lines: false));
    }

    [Fact]
    public void An_auto_save_s_lines_cut_the_map_lower_still()
    {
        var map = PhoneSaves.Map(Reference, lines: true);
        Assert.Equal(532 - 94, map.H, Close);
        var comment = PhoneSaves.Comment(Reference, lines: true);
        Assert.Equal(PhoneSaves.Page(Reference).Bottom - 94, comment.Bottom, Close);
        Assert.Equal(comment.Bottom + 34, PhoneSaves.LineMiddle(Reference, 0), Close);
        Assert.Equal(comment.Bottom + 74, PhoneSaves.LineMiddle(Reference, 1), Close);
    }

    [Fact]
    public void The_red_button_and_Back_stand_at_the_page_s_foot()
    {
        // ll1's foot, its two buttons swapped (owner, 2026-10-30): red_button (px0, foot[0], LX1 - 376, foot[1]); land_back
        // (LX1 - 360, foot[0], LX1, foot[1]).
        Equal(new(2236, 1010, 360, 150), PhoneSaves.Back(Reference));
        Equal(new(1040, 1010, 1180, 150), PhoneSaves.Button(Reference));
    }

    [Fact]
    public void The_questions_stand_at_their_buttons()
    {
        var delete = PhoneSaves.DeleteQuestion(Reference);
        Equal(new(1696, 126, 900, 250), delete);
        Equal(new(2392, 268, 180, 84), PhoneSaves.Answer(Reference, delete, yes: false));
        Equal(new(2116, 268, 260, 84), PhoneSaves.Answer(Reference, delete, yes: true));
        Equal(new(1040, 143, 1000, 250), PhoneSaves.SweepQuestion(Reference));
    }

    [Fact]
    public void The_comment_s_card_stands_just_over_the_keyboard()
    {
        // ly1: the card (LX0, LKB_TOP - 24 - 360, LX1, LKB_TOP - 24), LKB_TOP = LH - 650; the field (x0 + 24, y0 + 100, fx1,
        // y0 + 230), fx1 = x1 - 32 - 2 × 330 - 32; Keep (fx1 + 32, …, fx1 + 362), Cancel (x1 - 354, …, x1 - 24) — the two
        // swapped (owner, 2026-10-30); the count's right at fx1, its top y0 + 270.
        Equal(new(116, 186, 2480, 360), PhoneSaves.Card(Reference, 0));
        Equal(new(140, 286, 1732, 130), PhoneSaves.Field(Reference, 0));
        Equal(new(2242, 286, 330, 130), PhoneSaves.Cancel(Reference, 0));
        Equal(new(1904, 286, 330, 130), PhoneSaves.Keep(Reference, 0));
        Assert.Equal((1872f, 456f), PhoneSaves.Count(Reference, 0));
        // A keyboard reported 500 high: the card 24 over it; one taller than the screen leaves it at the top.
        Assert.Equal(1220 - 500 - 24, PhoneSaves.Card(Reference, 500).Bottom, Close);
        Assert.Equal(14, PhoneSaves.Card(Reference, 1200).Y, Close);
    }

    [Fact]
    public void A_longer_screen_widens_the_page_and_the_frame_s_unit_scales_it_all()
    {
        var longer = PhoneFrame.Landscape(3000, 1220);
        Assert.Equal(884, PhoneSaves.List(longer).W, Close);
        Assert.Equal(PhoneSaves.Page(Reference).W + 288, PhoneSaves.Page(longer).W, Close);
        Assert.Equal(PhoneSaves.Button(Reference).W + 288, PhoneSaves.Button(longer).W, Close);

        var half = PhoneFrame.Landscape(1356, 610);
        Equal(new(58, 160, 427, 52), PhoneSaves.Row(half, 0, newRow: false));
        Assert.Equal(7, PhoneSaves.RowsShown(half, newRow: false));
    }
}
