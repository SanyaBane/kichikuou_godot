using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Golden;
using Kichikuou.Original.Golden;
using Kichikuou.Original.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The owner's captures of the dungeon clicks (group <c>dungeon</c>, root <c>Cﾒｲｷｭｳ</c>) replayed
/// through <see cref="DungeonCommand"/>, which reads the dungeons' names from the player's files.
/// Skipped while there are none (or without the decompile).
/// </summary>
public sealed class DungeonReplayTests(ITestOutputHelper output)
{
    [CaptureGroupFact("dungeon")]
    public async Task The_dungeon_captures_replay_through_the_port()
    {
        string dir = Path.Combine(LocalData.GoldenDirectory, "dungeon");
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var failed = new List<string>();
        foreach (string path in Directory.EnumerateFiles(dir, "*.golden").Order(StringComparer.Ordinal))
        {
            var capture = GoldenCapture.Load(path);
            Assert.Equal("Cﾒｲｷｭｳ.ADV", capture.Root);
            var report = await GoldenReplay.RunAsync(capture, entryPoint: s => new DungeonCommand(s, scripts).RunAsync());
            output.WriteLine(report.Describe(maxLines: 40));
            if (!report.Passed)
                failed.Add(Path.GetFileName(path));
        }
        Assert.Empty(failed);
    }
}
