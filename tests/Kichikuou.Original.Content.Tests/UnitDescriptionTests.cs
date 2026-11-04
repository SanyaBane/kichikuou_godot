using System.Security.Cryptography;
using System.Text;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The units' descriptions read from the panels of their status CGs (docs/ui/unit-descriptions.md). The texts are the
/// player's, so the test holds their hash only: the reading was checked against the pictures once, by eye, and the hash
/// keeps it from changing unseen.
/// </summary>
public class UnitDescriptionTests
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));

    private static IReadOnlyList<string>? Read(int number) =>
        Cgs.Value.Get(number) is { } cg ? UnitDescription.Read(cg, number) : null;

    /// <summary>
    /// Every CG with an English panel reads whole — the units' 601-653 and 695-699, the harem's 701-750 and the items'
    /// 751-800, 157 in all — and the texts are the ones checked: each CG's number, then its paragraphs a line each.
    /// </summary>
    [ScenarioFact]
    public void Every_english_panel_reads_as_checked()
    {
        var all = new StringBuilder();
        var read = new List<int>();
        for (int n = 600; n < 900; n++)
            if (Read(n) is { } paragraphs)
            {
                read.Add(n);
                all.Append(n).Append('\n');
                foreach (var p in paragraphs)
                    all.Append(p).Append('\n');
            }
        if (Environment.GetEnvironmentVariable("KICHIKUOU_DESCRIPTIONS_OUT") is { Length: > 0 } outPath)
            File.WriteAllText(outPath, all.ToString());   // for tools/ui/mock_unit_descriptions.py; keep it outside the repository
        Assert.Equal([.. Enumerable.Range(601, 53), 695, 697, 698, 699, .. Enumerable.Range(701, 100)], read);
        Assert.Equal("FB6AD0BB87B1C4B13C0324AFDC5C1150BEDC0D77C3B306E95E3245E979B0ABC7",
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(all.ToString()))));
    }

    /// <summary>Rance's own (CG 601), the one the docs quote: three paragraphs, the quotation marks and apostrophes whole.</summary>
    [ScenarioFact]
    public void Rances_panel_reads_as_three_paragraphs()
    {
        var text = Read(601);
        Assert.NotNull(text);
        Assert.Equal(3, text.Count);
        Assert.StartsWith("A brutal warrior that's honest about his lust,", text[0]);
        Assert.Equal("Uses the special attack \"Rance Attack\".", text[2]);
    }

    /// <summary>
    /// Chaos (item 1, CG 751): "Equippable" a paragraph of its own, its figure's digits whole ("+10", not "+1 0"); the
    /// patch's kerned "A t" a space (a harem girl's, CG 727).
    /// </summary>
    [ScenarioFact]
    public void An_items_panel_and_the_kerned_a()
    {
        var chaos = Read(751);
        Assert.NotNull(chaos);
        Assert.Equal(["Equippable", "Commander Attack +10"], chaos.Take(2));
        Assert.StartsWith("A tea lady", Read(727)![0]);
    }

    /// <summary>The empty panels (600, 692, 696, 700) and the Japanese one (654) have no text; a CG of another size has no panel.</summary>
    [ScenarioFact]
    public void Empty_japanese_and_other_cgs_have_none()
    {
        Assert.Null(Read(600));
        Assert.Null(Read(654));
        Assert.Null(Read(692));
        Assert.Null(Read(696));
        Assert.Null(Read(700));
        Assert.Null(Read(693));
    }
}
