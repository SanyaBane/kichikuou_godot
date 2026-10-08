using Kichikuou.Core.Combat;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Tests;

public class ScenarioTextTests : IDisposable
{
    private readonly string _dir = Directory.CreateTempSubdirectory("kichikuou-text-").FullName;

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    private ScenarioText Write(params string[] lines)
    {
        File.WriteAllLines(Path.Combine(_dir, "TEST.ADV"), lines);
        return new ScenarioText(_dir);
    }

    [Fact]
    public void A_run_takes_text_breaks_numbers_and_strings_up_to_the_next_command()
    {
        var text = Write(
            "\tZH 1:",
            "\t'The Popularity'",
            "\tR",
            "\t'troops by '",
            "\tH 0, VAR0005:",
            "\tZH 1:",
            "\t'.'",
            "\tMP 4, 3:",
            "\tX 4:",
            "\tX VAR0198 + (VAR0002 - 1) * 3:",
            "\t\\L_05119:",
            "\t'not part of the run'");

        var run = text.Read(new ScriptRef("TEST.ADV", 2));

        Assert.Equal(1, run.NumberCount);
        Assert.Equal(1, run.ComputedStringCount);
        Assert.Equal(
            "The Popularity\ntroops by 50.RanRanceBound",
            run.Format([(Sys3Int)50], v => v switch { 4 => "Rance", 9 => "Bound", _ => "?" }, [(Sys3Int)9]));
        Assert.Throws<ArgumentException>(() => run.Format([(Sys3Int)50], _ => ""));
    }

    [Fact]
    public void Escaped_quotes_are_unescaped()
    {
        var text = Write("\t'as \\\"Cat Bread,\\\" often'");

        Assert.Equal("as \"Cat Bread,\" often", text.Read(new ScriptRef("TEST.ADV", 1)).Format());
    }

    [Fact]
    public void A_location_without_text_is_an_error()
    {
        var text = Write("\t!VAR0001 : 0!", "\t'late'");

        Assert.Throws<ScenarioTextException>(() => text.Read(new ScriptRef("TEST.ADV", 1)));
        Assert.Throws<ScenarioTextException>(() => text.Read(new ScriptRef("TEST.ADV", 3)));
        Assert.Throws<ScenarioTextException>(() => text.Read(new ScriptRef("NONE.ADV", 1)));
    }

    [Fact]
    public void A_run_may_start_with_a_line_break_but_not_right_after_one()
    {
        // FIGTﾃｷDE.ADV:1805-1808: the name is a separate PrintStringEvent, then R + text.
        var text = Write(
            "\tMP 4, 18:",
            "\tZH 1:",
            "\tR",
            "\tZH 1:",
            "\t'fell in battle.'");

        Assert.Equal("\nfell in battle.", text.Read(new ScriptRef("TEST.ADV", 3)).Format());
        Assert.Throws<ScenarioTextException>(() => text.Read(new ScriptRef("TEST.ADV", 5)));
    }

    [Fact]
    public void Missing_numbers_are_an_error()
    {
        var text = Write("\tH 2, VAR0001:");

        Assert.Throws<ArgumentException>(() => text.Read(new ScriptRef("TEST.ADV", 1)).Format());
    }

    [Theory]
    [InlineData(7, 0, false, "7")]
    [InlineData(7, 3, false, "  7")]
    [InlineData(1234, 2, false, "34")]          // %*d keeps the last `width` characters
    [InlineData(1234, 0, true, "１２３４")]
    [InlineData(5, 2, true, "　５")]
    public void Numbers_are_formatted_like_xsystem35(int value, int width, bool fullWidth, string expected) =>
        Assert.Equal(expected, TextRun.FormatNumber(value, width, fullWidth));

    [Theory]
    [InlineData("　（１２３４）：", " (1234):")]
    [InlineData("It’s ＡＢＣ　ｱﾞ", "It's ABC ｱﾞ")]
    [InlineData("ガア～＂", "ガｱ～＂")]   // voiced kana and the characters zen2han_utf8 has no case for stay
    public void Messages_are_converted_to_half_width_like_ZH_1(string text, string expected) =>
        Assert.Equal(expected, Hankaku.ToHalfWidth(text));

    [Theory]
    [InlineData("Rance", 3, "Ran")]
    [InlineData("Rance", 7, "Rance　　")]
    [InlineData("", 2, "　　")]
    public void String_variables_are_cut_and_padded_like_MP(string value, int chars, string expected) =>
        Assert.Equal(expected, TextRun.FormatString(value, chars));
}
