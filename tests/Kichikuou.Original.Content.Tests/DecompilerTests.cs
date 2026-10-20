using System.Diagnostics;
using System.Text;
using Kichikuou.Content.Archives;
using Kichikuou.Content.Decompiler;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The decompiler translated from xsys35dc 1.13 against the decompile xsys35dc made of the player's files
/// (docs/import.md, step 6): every <c>.ADV</c> byte for byte.
/// </summary>
public class DecompilerTests(ITestOutputHelper output)
{
    [LocalFolderFact("game", "scenario")]
    public void Every_page_decompiles_to_the_extracts_file()
    {
        var watch = Stopwatch.StartNew();
        var archive = AldArchive.Open(LocalData.GameDirectory, "KICHIKUS");
        var ain = AinFile.Read(Path.Combine(LocalData.GameDirectory, "System39.ain"));
        var warnings = new List<string>();
        int passes = 0;
        var files = ScenarioDecompiler.Decompile(archive, ain,
            new SyncProgress(p => passes = Math.Max(passes, p.Pass)), warnings.Add);
        watch.Stop();

        output.WriteLine($"{files.Count} files in {watch.ElapsedMilliseconds} ms, {passes} analysis passes, {warnings.Count} warnings");
        foreach (string warning in warnings.Take(10))
            output.WriteLine("  " + warning);

        Assert.Equal(201, files.Count);
        string[] hed = File.ReadAllLines(Path.Combine(LocalData.ScenarioDirectory, "xsys35dc.hed"), Encoding.UTF8);
        Assert.Equal(hed[1..], files.Select(f => f.Name));

        var differing = new List<string>();
        long lines = 0;
        foreach (var file in files)
        {
            byte[] expected = File.ReadAllBytes(Path.Combine(LocalData.ScenarioDirectory, file.Name));
            lines += expected.Count(b => b == '\n');
            if (!expected.AsSpan().SequenceEqual(file.Text))
                differing.Add($"{file.Name}: {FirstDifference(expected, file.Text)}");
        }
        output.WriteLine($"{lines} lines");
        foreach (string difference in differing.Take(10))
            output.WriteLine(difference);
        Assert.Empty(differing);
    }

    /// <summary>The first line where two files differ, both versions.</summary>
    private static string FirstDifference(byte[] expected, byte[] actual)
    {
        string[] want = Encoding.UTF8.GetString(expected).Split("\r\n");
        string[] got = Encoding.UTF8.GetString(actual).Split("\r\n");
        for (int i = 0; i < Math.Max(want.Length, got.Length); i++)
        {
            string? a = i < want.Length ? want[i] : null;
            string? b = i < got.Length ? got[i] : null;
            if (a != b)
                return $"line {i + 1}: expected '{a}', got '{b}' ({want.Length} vs {got.Length} lines)";
        }
        return "the same lines, other bytes";
    }

    /// <summary>A progress report that runs on the reporting thread (<see cref="Progress{T}"/> posts it).</summary>
    private sealed class SyncProgress(Action<DecompileProgress> report) : IProgress<DecompileProgress>
    {
        public void Report(DecompileProgress value) => report(value);
    }
}
