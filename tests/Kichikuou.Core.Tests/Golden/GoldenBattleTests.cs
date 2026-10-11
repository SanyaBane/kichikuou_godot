using Kichikuou.Core.Golden;
using Xunit.Abstractions;

namespace Kichikuou.Core.Tests.Golden;

/// <summary>
/// Where the captures of the original game live. They contain game data and stay outside the
/// repository (<c>docs/golden-tests.md</c>): <see cref="LocalPaths.GoldenDirectory"/>
/// (<c>KICHIKUOU_GOLDEN_DIR</c>, else <c>golden</c> in <c>KICHIKUOU_EXTRACT_DIR</c>; <c>local.env</c>).
/// Battle captures lie in the folder itself, the other groups' in subfolders named after the group.
/// </summary>
public static class GoldenData
{
    public static string Directory => LocalPaths.GoldenDirectory ?? "";

    /// <summary>The complete captures (<c>*.golden</c>, every subfolder; unfinished <c>.part</c> files are not listed).</summary>
    public static IEnumerable<string> Files =>
        System.IO.Directory.Exists(Directory)
            ? System.IO.Directory.EnumerateFiles(Directory, "*.golden", SearchOption.AllDirectories)
                .Order(StringComparer.Ordinal)
            : [];

    private static readonly Lazy<List<(string Path, GoldenHeader Header)>> Headers =
        new(() => Files.Select(f => (f, GoldenCapture.ReadHeader(f))).ToList());

    /// <summary>Captures whose root the replay has an entry point for.</summary>
    public static IEnumerable<string> Replayable => Headers.Value.Where(h => GoldenReplay.CanReplay(h.Header)).Select(h => h.Path);

    /// <summary>Captures waiting for the port of their root.</summary>
    public static IEnumerable<(string Path, GoldenHeader Header)> Pending =>
        Headers.Value.Where(h => !GoldenReplay.CanReplay(h.Header));

    /// <summary>A capture's path relative to <see cref="Directory"/> (the test-case name).</summary>
    public static string Relative(string path) => Path.GetRelativePath(Directory, path);
}

/// <summary>A theory over the golden captures; skipped when there are none.</summary>
public sealed class GoldenTheoryAttribute : TheoryAttribute
{
    public GoldenTheoryAttribute()
    {
        if (!GoldenData.Replayable.Any())
            Skip = $"No replayable captures in {GoldenData.Directory} (play with tools/golden/capture.ps1).";
    }
}

/// <summary>Every capture of the original game whose root is ported (the battle, <c>COM</c>), replayed through the port.</summary>
public class GoldenBattleTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Captures => GoldenData.Replayable.Select(f => new object[] { GoldenData.Relative(f) });

    [GoldenTheory]
    [MemberData(nameof(Captures))]
    public async Task Capture_matches_the_original(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(GoldenData.Directory, file));

        var report = await GoldenReplay.RunAsync(capture);

        output.WriteLine(report.Describe(maxLines: 200));
        Assert.True(report.Passed, report.Describe());
    }
}
