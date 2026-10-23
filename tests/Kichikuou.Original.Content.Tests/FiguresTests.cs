namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="Figures"/>: how our screens write gold, troops and damage, each separator Option offers.</summary>
public class FiguresTests
{
    [Theory]
    [InlineData(ThousandsSeparator.Comma, "1,234,567")]
    [InlineData(ThousandsSeparator.Period, "1.234.567")]
    [InlineData(ThousandsSeparator.Space, "1 234 567")]
    [InlineData(ThousandsSeparator.Apostrophe, "1'234'567")]
    [InlineData(ThousandsSeparator.None, "1234567")]
    public void Separates_the_thousands_as_chosen(ThousandsSeparator separator, string expected) =>
        Assert.Equal(expected, Figures.Number(1234567, separator));

    [Theory]
    [InlineData(0, "0")]
    [InlineData(999, "999")]
    [InlineData(1000, "1,000")]
    [InlineData(-2300, "−2,300")]
    [InlineData(int.MinValue, "−2,147,483,648")]
    public void Writes_small_and_negative_numbers(int n, string expected) =>
        Assert.Equal(expected, Figures.Number(n, ThousandsSeparator.Comma));

    [Fact]
    public void Signs_gains_and_losses_with_a_minus_not_a_hyphen()
    {
        Assert.Equal("−50", Figures.Number(-50, ThousandsSeparator.None));
        Assert.DoesNotContain("-", Figures.Number(-50, ThousandsSeparator.None));
    }

    [Fact]
    public void Writes_gold_long_and_short_with_no_space_before_the_K()
    {
        Assert.Equal(ThousandsSeparator.Comma, Figures.Separator);     // the library's default; no test changes it
        Assert.Equal("129,300K GOLD", Figures.Gold(129300));
        Assert.Equal("2,300K", Figures.GoldShort(2300));
        Assert.Equal("+1,191", Figures.Signed(1191));
        Assert.Equal("0", Figures.Signed(0));
    }
}
