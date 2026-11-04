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
    /// Every status CG with an English panel reads whole — 601-653 and 695-699, 57 in all — and the texts are the ones
    /// checked: each CG's number, then its paragraphs a line each.
    /// </summary>
    [ScenarioFact]
    public void Every_english_panel_reads_as_checked()
    {
        var all = new StringBuilder();
        var read = new List<int>();
        for (int n = 600; n < 700; n++)
            if (Read(n) is { } paragraphs)
            {
                read.Add(n);
                all.Append(n).Append('\n');
                foreach (var p in paragraphs)
                    all.Append(p).Append('\n');
            }
        if (Environment.GetEnvironmentVariable("KICHIKUOU_DESCRIPTIONS_OUT") is { Length: > 0 } outPath)
            File.WriteAllText(outPath, all.ToString());   // for tools/ui/mock_unit_descriptions.py; keep it outside the repository
        Assert.Equal([.. Enumerable.Range(601, 53), 695, 697, 698, 699], read);
        Assert.Equal("2A8740F78BB7143636E2F93E616A223DB07F14CBDFAF14D913E3C63AF9A20531",
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

    /// <summary>The empty panels (600, 692, 696) and the Japanese one (654) have no text; a CG of another size is not a status CG.</summary>
    [ScenarioFact]
    public void Empty_japanese_and_other_cgs_have_none()
    {
        Assert.Null(Read(600));
        Assert.Null(Read(654));
        Assert.Null(Read(692));
        Assert.Null(Read(696));
        Assert.Null(Read(693));
    }
}
