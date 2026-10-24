using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.TestSupport;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <see cref="ScenarioFigures"/>: the scenario's sums of gold grouped under our message windows, only at the places
/// docs/ui/gold-figures.md verified, with the player's files' texts. Skipped without them.
/// </summary>
public class ScenarioFiguresTests
{
    private static readonly ScenarioText Text = new(LocalData.ScenarioDirectory);

    /// <summary>Every listed text line holds a figure before K; every listed <c>H 0</c> is followed by <c>00K…</c>.</summary>
    [ScenarioFact]
    public void The_listed_places_are_the_sums_of_the_survey()
    {
        foreach (var at in ScenarioFigures.Literals)
            Assert.Matches("^'.*[0-9]K.*'$", Hankaku.ToHalfWidth(Line(at)));
        foreach (var at in ScenarioFigures.Printed)
        {
            Assert.StartsWith("H 0, ", Line(at));
            Assert.Equal("ZH 1:", Line(at with { Line = at.Line + 1 }));
            Assert.StartsWith("'00K", Line(at with { Line = at.Line + 2 }));
        }
    }

    private static string Line(ScriptRef at) =>
        File.ReadLines(Path.Combine(LocalData.ScenarioDirectory, at.File)).ElementAt(at.Line - 1).Trim();

    /// <summary>The merchant's Louis Unit (unit 49): its troops, prices and the budget, 77700K in the treasury.</summary>
    [ScenarioFact]
    public void A_window_groups_its_prices_troops_and_budget()
    {
        string text = Presenter(cover: true).ReadText(new ScriptRef("Cｼｮｳﾆﾝ.ADV", 250), (Sys3Int)777);

        Assert.Contains("200 Troops:", text);
        Assert.Contains("4,000KG", text);
        Assert.Contains("1,000 Troops:", text);
        Assert.Contains("8,000KG", text);
        Assert.Contains("77,700KG", text);
    }

    /// <summary>An empty treasury is "0K", not the original's "000K".</summary>
    [ScenarioFact]
    public void An_empty_treasury_is_a_zero() =>
        Assert.Contains("Budget: 0KG", Presenter(cover: true).ReadText(new ScriptRef("Cｼｮｳﾆﾝ.ADV", 250), (Sys3Int)0));

    /// <summary>The original's windows (<c>--interface original</c>) keep the original's numbers.</summary>
    [ScenarioFact]
    public void The_original_windows_keep_the_original_numbers()
    {
        string text = Presenter(cover: false).ReadText(new ScriptRef("Cｼｮｳﾆﾝ.ADV", 250), (Sys3Int)777);

        Assert.Contains("1000 Troops:", text);
        Assert.Contains("4000KG", text);
        Assert.Contains("77700KG", text);
    }

    /// <summary>A menu item's sum (the merchant's contract), and each separator.</summary>
    [ScenarioTheory]
    [InlineData(ThousandsSeparator.Comma, "4,000K Contract", "77,700KG")]
    [InlineData(ThousandsSeparator.Space, "4 000K Contract", "77 700KG")]
    [InlineData(ThousandsSeparator.None, "4000K Contract", "77700KG")]
    public void Menus_and_separators(ThousandsSeparator separator, string contract, string budget)
    {
        var (item, _) = ScenarioFigures.Group(Text.Read(new ScriptRef("Cｼｮｳﾆﾝ.ADV", 267)), [], separator);
        Assert.Equal(contract, item.FormatCaptured([], []));

        var (run, numbers) = ScenarioFigures.Group(Text.Read(new ScriptRef("Cｼｮｳﾆﾝ.ADV", 250)), [(Sys3Int)777], separator);
        Assert.Empty(numbers);
        Assert.Contains(budget, run.Format(numbers));
    }

    /// <summary>A place the survey leaves out keeps its figures: the status window's budget (our status strip reads it).</summary>
    [ScenarioFact]
    public void Places_left_out_keep_their_figures()
    {
        var run = Text.Read(new ScriptRef("COMMAND.ADV", 446));
        var (same, numbers) = ScenarioFigures.Group(run, [(Sys3Int)1027, (Sys3Int)0, (Sys3Int)0, (Sys3Int)0]);

        Assert.Same(run, same);
        Assert.Contains("102700K", same.Format(numbers));
    }

    private static GamePresenter Presenter(bool cover)
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        return new GamePresenter(Text, new CgLibrary(missing), new BoxGlyphs()) { CoverMessages = cover };
    }
}
