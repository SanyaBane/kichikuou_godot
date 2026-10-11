using Kichikuou.Content.Scripts;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Script;
using Kichikuou.Core.Strategy;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The owner's captures of the nation turns and the defender pickers (group <c>nations</c>)
/// replayed through their ports, which read the army name (<c>MS 10</c>) from the player's files:
/// <c>TCﾘｰｻﾞｽ</c> (<see cref="RebelTurn"/>), <c>TCﾘｰｻﾞｽ2</c> (<see cref="RebelDefenders"/>),
/// <c>Tﾍﾙﾏﾝ</c> (<see cref="HelmanTurn"/>), <c>Tﾍﾙﾏﾝ2</c> (<see cref="HelmanDefenders"/>),
/// <c>Tﾊﾟｯﾄﾝ</c> (<see cref="PattonTurn"/>), <c>Tｾﾞｽ</c> (<see cref="ZethTurn"/>), <c>Tｾﾞｽ2</c> (<see cref="ZethDefenders"/>),
/// <c>Tｹｲﾌﾞﾘｽ</c> (<see cref="KayblisTurn"/>), <c>Tｹｲﾌﾞﾘｽ2</c> (<see cref="KayblisDefenders"/>), <c>TJAPAN</c> (<see cref="JapanTurn"/>) and <c>TJAPAN2</c> (<see cref="JapanDefenders"/>);
/// <c>FIGT_B</c> and the scenes stay hooks. Captures of nations not ported yet are listed and left
/// to <c>GoldenPendingTests</c>. Skipped while there are none (or without the decompile).
/// </summary>
public sealed class NationReplayTests(ITestOutputHelper output)
{
    /// <summary>The port of a nation root, or null while it waits for its part.</summary>
    private static Func<ScriptContext, Task>? EntryPoint(string root, ScenarioScripts scripts) => root switch
    {
        "TCﾘｰｻﾞｽ.ADV" => s => new RebelTurn(s, scripts).RunAsync(),
        "TCﾘｰｻﾞｽ2.ADV" => s => new RebelDefenders(s, scripts).RunAsync(),
        "Tﾍﾙﾏﾝ.ADV" => s => new HelmanTurn(s, scripts).RunAsync(),
        "Tﾍﾙﾏﾝ2.ADV" => s => new HelmanDefenders(s, scripts).RunAsync(),
        "Tﾊﾟｯﾄﾝ.ADV" => s => new PattonTurn(s, scripts).RunAsync(),
        "Tｾﾞｽ.ADV" => s => new ZethTurn(s, scripts).RunAsync(),
        "Tｾﾞｽ2.ADV" => s => new ZethDefenders(s, scripts).RunAsync(),
        "Tｹｲﾌﾞﾘｽ.ADV" => s => new KayblisTurn(s, scripts).RunAsync(),
        "Tｹｲﾌﾞﾘｽ2.ADV" => s => new KayblisDefenders(s, scripts).RunAsync(),
        "TJAPAN.ADV" => s => new JapanTurn(s, scripts).RunAsync(),
        "TJAPAN2.ADV" => s => new JapanDefenders(s, scripts).RunAsync(),
        _ => null,
    };

    [Fact]
    public async Task The_nation_captures_replay_through_the_port()
    {
        string dir = Path.Combine(LocalData.GoldenDirectory, "nations");
        if (!Directory.Exists(dir) || !Directory.Exists(LocalData.ScenarioDirectory))
            return;
        var scripts = new ScenarioScripts(LocalData.ScenarioDirectory);
        var failed = new List<string>();
        foreach (string path in Directory.EnumerateFiles(dir, "*.golden").Order(StringComparer.Ordinal))
        {
            var capture = GoldenCapture.Load(path);
            if (EntryPoint(capture.Root, scripts) is not { } entryPoint)
            {
                output.WriteLine($"{Path.GetFileName(path)}: {capture.Root} is not ported yet");
                continue;
            }
            var report = await GoldenReplay.RunAsync(capture, entryPoint: entryPoint);
            output.WriteLine(report.Describe(maxLines: 40));
            if (!report.Passed || report.Unported is not null)
                failed.Add(Path.GetFileName(path));
        }
        Assert.Empty(failed);
    }
}
