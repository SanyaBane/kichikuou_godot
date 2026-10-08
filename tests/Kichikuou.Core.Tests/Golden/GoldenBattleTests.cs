using Kichikuou.Core.Golden;
using Xunit.Abstractions;

namespace Kichikuou.Core.Tests.Golden;

/// <summary>
/// Where the captures of the original game live. They contain game data and stay outside the
/// repository (<c>docs/golden-tests.md</c>); <c>KICHIKUOU_GOLDEN_DIR</c> overrides the default.
/// </summary>
public static class GoldenData
{
    public const string DefaultDirectory = @"E:\Modding games\Modding Rance\Kichikou\_extract\golden";

    public static string Directory =>
        Environment.GetEnvironmentVariable("KICHIKUOU_GOLDEN_DIR") is { Length: > 0 } dir ? dir : DefaultDirectory;

    /// <summary>The complete captures (<c>*.golden</c>; unfinished <c>.part</c> files are not listed).</summary>
    public static IEnumerable<string> Files =>
        System.IO.Directory.Exists(Directory)
            ? System.IO.Directory.EnumerateFiles(Directory, "*.golden").Order(StringComparer.Ordinal)
            : [];
}

/// <summary>A theory over the golden captures; skipped when there are none.</summary>
public sealed class GoldenTheoryAttribute : TheoryAttribute
{
    public GoldenTheoryAttribute()
    {
        if (!GoldenData.Files.Any())
            Skip = $"No golden captures in {GoldenData.Directory} (play battles with tools/golden/capture.ps1).";
    }
}

/// <summary>Every captured battle of the original game, replayed through the battle port.</summary>
public class GoldenBattleTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Captures => GoldenData.Files.Select(f => new object[] { Path.GetFileName(f) });

    [GoldenTheory]
    [MemberData(nameof(Captures))]
    public async Task Battle_matches_the_original(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(GoldenData.Directory, file));

        var report = await GoldenReplay.RunAsync(capture);

        output.WriteLine(report.Describe(maxLines: 200));
        Assert.True(report.Passed, report.Describe());
    }
}
