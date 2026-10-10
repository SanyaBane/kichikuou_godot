using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// How many random cases the differential tests (a port against its file run from the decompile)
/// run. A quick run, the default, takes an eighth of each test's full count: enough to catch a port
/// that went wrong while working on it. The full count also reaches the rare paths every test lists
/// and must pass before a commit: <c>KICHIKUOU_FULL_TESTS=1</c> (CLAUDE.md, "Commands").
/// </summary>
internal static class DifferentialRuns
{
    private const int QuickShare = 8;

    /// <summary>The full counts run (<c>KICHIKUOU_FULL_TESTS=1</c>).</summary>
    public static bool Full { get; } = Environment.GetEnvironmentVariable("KICHIKUOU_FULL_TESTS") == "1";

    /// <summary>The cases to run for a test whose full count is <paramref name="full"/>.</summary>
    public static int Cases(int full) => Full ? full : Math.Max(1, full / QuickShare);

    /// <summary>
    /// Fails a full run that never reached the listed <paramref name="missed"/> paths; a quick run's few
    /// cases may miss a rare one, so it only reports them.
    /// </summary>
    public static void AssertReached(IReadOnlyCollection<string> missed, ITestOutputHelper output, string what = "never reached")
    {
        if (missed.Count == 0)
            return;
        string message = $"{what}: {string.Join(", ", missed)}";
        if (Full)
            Assert.Fail(message);
        output.WriteLine($"quick run (KICHIKUOU_FULL_TESTS=1 runs them all), {message}");
    }
}
