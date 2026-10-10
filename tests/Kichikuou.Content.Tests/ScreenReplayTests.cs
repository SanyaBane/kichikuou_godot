using Kichikuou.Content.Scripts;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Script;
using Kichikuou.Core.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The owner's captures of the picker screens (group <c>screens</c>, roots <c>Sﾌﾞﾀｲ</c>,
/// <c>Sﾌﾞｶ</c>, <c>Sﾊｰﾚﾑ</c>, <c>Sｱｲﾃﾑ</c>) replayed through their ports, which read their literals from the
/// player's files. Skipped while there are none (or without the decompile).
/// </summary>
public sealed class ScreenReplayTests(ITestOutputHelper output)
{
    [Fact]
    public async Task The_screen_captures_replay_through_the_ports()
    {
        string dir = Path.Combine(LocalData.GoldenDirectory, "screens");
        if (!Directory.Exists(dir) || !Directory.Exists(LocalData.ScenarioDirectory))
            return;
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var failed = new List<string>();
        foreach (string path in Directory.EnumerateFiles(dir, "*.golden").Order(StringComparer.Ordinal))
        {
            var capture = GoldenCapture.Load(path);
            Func<ScriptContext, Task> entry = capture.Root switch
            {
                "Sﾌﾞﾀｲ.ADV" => s => new DeploymentScreen(s, scripts).RunAsync(),
                "Sﾌﾞｶ.ADV" => s => new SubordinateScreen(s, scripts).RunAsync(),
                "Sﾊｰﾚﾑ.ADV" => s => new HaremScreen(s, scripts).RunAsync(),
                "Sｱｲﾃﾑ.ADV" => s => new ItemScreen(s, scripts).RunAsync(),
                _ => throw new InvalidOperationException($"{path}: root {capture.Root} is not a picker screen."),
            };
            var report = await GoldenReplay.RunAsync(capture, entryPoint: entry);
            output.WriteLine(report.Describe(maxLines: 40));
            if (!report.Passed)
                failed.Add(Path.GetFileName(path));
        }
        Assert.Empty(failed);
    }
}
