using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="PhoneSavesPortrait"/>: our Save/Load screen on the phone held upright (docs/ui/phone-build.md, step P7), at the
/// mock-ups' figures on the reference phone (docs/ui/phone.md, §9: PL2 as the owner changed it; §25: PY2).
/// </summary>
public class PhoneSavesPortraitTests
{
    private const float Close = 0.01f;

    private static readonly PhoneFrame Reference = PhoneFrame.Portrait(1220, 2712);

    /// <summary>The map at the room's width less 32, its top 80%: 1188 × 533 / 1280 × 0.8.</summary>
    private const float MapH = 1188f * 533f / 1280f * 0.8f;

    /// <summary>The page on the bar: 24 over it, the title band, the map and the 458 under it.</summary>
    private const float PageY = 2504 - 24 - (100 + MapH + 458);

    private static void Equal(PhoneFrame.Box expected, PhoneFrame.Box actual)
    {
        Assert.Equal(expected.X, actual.X, Close);
        Assert.Equal(expected.Y, actual.Y, Close);
        Assert.Equal(expected.W, actual.W, Close);
        Assert.Equal(expected.H, actual.H, Close);
    }

    private static float Width(string s) => s.Length * 10;

    [Fact]
    public void The_band_holds_Load_and_Save_and_the_tabs_stand_under_it()
    {
        // save_title: the chips (PW - 16 - 330, TOP + 8) and (PW - 16 - 160, …), 160 × 88 (owner, 2026-11-04); lacquer_tabs at TOP + 134, 104
        // high, 12 apart across 16 to PW - 16.
        Equal(new(874, 118, 160, 88), PhoneSavesPortrait.Chip(Reference, save: false));
        Equal(new(1044, 118, 160, 88), PhoneSavesPortrait.Chip(Reference, save: true));
        Equal(new(16, 244, 588, 104), PhoneSavesPortrait.Tab(Reference, auto: false));
        Equal(new(616, 244, 588, 104), PhoneSavesPortrait.Tab(Reference, auto: true));
    }

    [Fact]
    public void Nine_rows_are_in_view_above_the_page()
    {
        // save_list: the heading 18 under the tabs, the sweep (PW - 16 - 470, y, PW - 16, y + 64); the rows 84 under its top,
        // 104 high and 10 apart down to the page's top less 36: (foot - y + 10) // 114 = 9; the reference groove (S2) at its right.
        Equal(new(734, 366, 470, 64), PhoneSavesPortrait.Sweep(Reference));
        Assert.Equal(398, PhoneSavesPortrait.HeadMiddle(Reference), Close);
        Assert.Equal(9, PhoneSavesPortrait.RowsShown(Reference, newRow: false));
        Equal(new(16, 450, 1158, 104), PhoneSavesPortrait.Row(Reference, 0, newRow: false));
        Equal(new(16, 1362, 1158, 104), PhoneSavesPortrait.Row(Reference, 8, newRow: false));
        Equal(new(1184, 450, 20, 1016), PhoneSavesPortrait.ScrollBar(Reference, newRow: false));      // the reference, S2
        Equal(new(16, 450, 1188, 1016), PhoneSavesPortrait.Scrolled(Reference, newRow: false));
        Equal(new(16, 110, 1188, PageY - 36 - 110), PhoneSavesPortrait.List(Reference));
        Assert.Equal(PageY - 18, PhoneSavesPortrait.PageDividerY(Reference), Close);
    }

    [Fact]
    public void New_save_heads_the_rows_and_takes_one_from_view()
    {
        Equal(new(16, 450, 1158, 88), PhoneSavesPortrait.New(Reference));
        Assert.Equal(8, PhoneSavesPortrait.RowsShown(Reference, newRow: true));
        Equal(new(16, 548, 1158, 104), PhoneSavesPortrait.Row(Reference, 0, newRow: true));
        Equal(new(16, 450, 1188, 98 + 8 * 114 - 10), PhoneSavesPortrait.Scrolled(Reference, newRow: true));
    }

    [Fact]
    public void The_page_stands_on_the_bar_its_map_cut_its_figures_in_two_columns()
    {
        // save_page (compact): Delete (PW - 16 - 230, y, PW - 16, y + 84); the map 100 under the top; the figures 20 under it,
        // two columns, rows 96 apart; COMMENT 298 under them, its box 36 under it, 104 high, to PW - 16 - 220; Edit 204 wide.
        Equal(new(16, PageY, 1188, 100 + MapH + 458), PhoneSavesPortrait.Page(Reference));
        Assert.Equal(PageY + 40, PhoneSavesPortrait.TitleMiddle(Reference), Close);
        Equal(new(974, PageY, 230, 84), PhoneSavesPortrait.Delete(Reference));
        Equal(new(16, PageY + 100, 1188, MapH), PhoneSavesPortrait.Map(Reference, lines: false));
        float figures = PageY + 100 + MapH + 20;
        var first = PhoneSavesPortrait.Figure(Reference, 0, lines: false);
        Assert.Equal(24, first.X, Close);
        Assert.Equal(figures, first.Y, Close);
        Assert.Equal(594, first.W, Close);
        var last = PhoneSavesPortrait.Figure(Reference, 5, lines: false);
        Assert.Equal(618, last.X, Close);
        Assert.Equal(figures + 192, last.Y, Close);
        Assert.Equal(figures + 298, PhoneSavesPortrait.CommentLabel(Reference, lines: false), Close);
        Equal(new(16, figures + 334, 968, 104), PhoneSavesPortrait.Comment(Reference, lines: false));
        Equal(new(1000, figures + 334, 204, 104), PhoneSavesPortrait.Edit(Reference, lines: false));
        Assert.Equal(2480, PhoneSavesPortrait.Comment(Reference, lines: false).Bottom, Close);
    }

    [Fact]
    public void An_auto_saves_lines_cut_the_map_and_leave_the_page_where_it_is()
    {
        Equal(new(16, PageY + 100, 1188, MapH - 94), PhoneSavesPortrait.Map(Reference, lines: true));
        Assert.Equal(2386, PhoneSavesPortrait.Comment(Reference, lines: true).Bottom, Close);
        Assert.Equal(2420, PhoneSavesPortrait.LineMiddle(Reference, 0), Close);
        Assert.Equal(2460, PhoneSavesPortrait.LineMiddle(Reference, 1), Close);
        Assert.Equal(PageY, PhoneSavesPortrait.Page(Reference).Y, Close);
    }

    [Fact]
    public void The_red_button_and_Back_share_the_bar()
    {
        // back_and, its two buttons swapped (owner, 2026-10-30): the red button (16, y0, PW - 400, y1), Back (PW - 400, y0,
        // PW - 16, y1).
        Equal(new(820, 2504, 384, 148), PhoneSavesPortrait.Back(Reference));
        Equal(new(16, 2504, 788, 148), PhoneSavesPortrait.Button(Reference));
    }

    [Fact]
    public void The_questions_hang_under_their_buttons()
    {
        var delete = PhoneSavesPortrait.DeleteQuestion(Reference);
        Equal(new(304, PageY + 112, 900, 250), delete);
        Equal(new(204, 458, 1000, 250), PhoneSavesPortrait.SweepQuestion(Reference));
        Equal(PhoneSaves.Answer(Reference, delete, yes: true), PhoneSavesPortrait.Answer(Reference, delete, yes: true));
        Equal(new(1204 - 24 - 180, PageY + 112 + 250 - 108, 180, 84), PhoneSavesPortrait.Answer(Reference, delete, yes: false));
    }

    [Fact]
    public void The_comment_card_stands_over_the_keyboard()
    {
        // py2: the card (16, KB_TOP - 24 - 560, PW - 16, KB_TOP - 24), KB_TOP = PH - 1000; comment_card: the field (x0 + 24,
        // y0 + 100, x1 - 24, y0 + 300), the count's right x1 - 32 at the field's foot + 36, Keep | Cancel (swapped, owner,
        // 2026-10-30) 130 high 36 over the foot, 16 apart.
        Equal(new(16, 1128, 1188, 560), PhoneSavesPortrait.Card(Reference, 0));
        Equal(new(40, 1228, 1140, 200), PhoneSavesPortrait.Field(Reference, 0));
        var (right, middle) = PhoneSavesPortrait.Count(Reference, 0);
        Assert.Equal(1172, right, Close);
        Assert.Equal(1464, middle, Close);
        Equal(new(618, 1522, 562, 130), PhoneSavesPortrait.Cancel(Reference, 0));
        Equal(new(40, 1522, 562, 130), PhoneSavesPortrait.Keep(Reference, 0));
    }

    [Fact]
    public void The_card_follows_the_keyboard_Android_reports_and_never_leaves_the_room()
    {
        Equal(new(16, 928, 1188, 560), PhoneSavesPortrait.Card(Reference, 1200));
        Equal(new(16, 124, 1188, 560), PhoneSavesPortrait.Card(Reference, 2600));
    }

    [Fact]
    public void A_longer_screen_shows_more_rows_and_keeps_the_page_on_the_bar()
    {
        var longer = PhoneFrame.Portrait(1220, 2712 + 228);
        Assert.Equal(11, PhoneSavesPortrait.RowsShown(longer, newRow: false));
        Assert.Equal(longer.Bar.Y - 24, PhoneSavesPortrait.Page(longer).Bottom, Close);
    }

    [Fact]
    public void The_words_wrap_after_a_space_and_a_long_word_between_its_letters()
    {
        Assert.Equal([new(0, "abc "), new(4, "def "), new(8, "ghi")], PhoneSavesPortrait.Wrap("abc def ghi", Width, 50));
        Assert.Equal([new(0, "abc"), new(3, "def"), new(6, "gh")], PhoneSavesPortrait.Wrap("abcdefgh", Width, 30));
        Assert.Equal([new(0, "abcde "), new(6, "fg")], PhoneSavesPortrait.Wrap("abcde fg", Width, 50));
        Assert.Equal([new(0, "")], PhoneSavesPortrait.Wrap("", Width, 50));
    }

    [Fact]
    public void The_caret_stands_on_its_line_and_a_tap_finds_the_nearest_letter()
    {
        var lines = PhoneSavesPortrait.Wrap("abc def ghi", Width, 50);
        Assert.Equal((0, "abc"), PhoneSavesPortrait.CaretAt(lines, 3));
        Assert.Equal((1, ""), PhoneSavesPortrait.CaretAt(lines, 4));
        Assert.Equal((2, "ghi"), PhoneSavesPortrait.CaretAt(lines, 11));
        Assert.Equal(6, PhoneSavesPortrait.ColumnAt(lines, 1, 17, Width));
        Assert.Equal(3, PhoneSavesPortrait.ColumnAt(lines, 0, 100, Width));
        Assert.Equal(11, PhoneSavesPortrait.ColumnAt(lines, 2, 100, Width));
    }
}
