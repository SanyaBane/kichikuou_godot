using Kichikuou.Core.Combat;
using Kichikuou.Core.Golden;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// Local game data: the decompiled scenario and the golden captures (docs/golden-tests.md). Both
/// stay outside the repository; <c>KICHIKUOU_SCENARIO_DIR</c> / <c>KICHIKUOU_GOLDEN_DIR</c>
/// override the defaults; <c>KICHIKUOU_EXTRACT_DIR</c> moves the whole extracted folder
/// (CGs, sounds) and <c>KICHIKUOU_GAME_DIR</c> the game folder (music).
/// </summary>
public static class LocalData
{
    private static string Extract => Env("KICHIKUOU_EXTRACT_DIR") ?? @"E:\Modding games\Modding Rance\Kichikou\_extract";

    public static string ScenarioDirectory => Env("KICHIKUOU_SCENARIO_DIR") ?? Path.Combine(Extract, "scenario");

    public static string GoldenDirectory => Env("KICHIKUOU_GOLDEN_DIR") ?? Path.Combine(Extract, "golden");

    public static string CgDirectory => Path.Combine(Extract, "cg_png");

    public static string PmsDirectory => Path.Combine(Extract, "cg_pms");

    public static string WavDirectory => Path.Combine(Extract, "wav");

    public static string GameDirectory => Env("KICHIKUOU_GAME_DIR") ?? @"E:\Modding games\Modding Rance\Kichikou\Kichikuou Rance";

    public static IEnumerable<string> Captures =>
        Directory.Exists(GoldenDirectory)
            ? Directory.EnumerateFiles(GoldenDirectory, "*.golden").Order(StringComparer.Ordinal)
            : [];

    private static string? Env(string name) =>
        Environment.GetEnvironmentVariable(name) is { Length: > 0 } value ? value : null;
}

/// <summary>A theory over the golden captures; skipped without captures or scenario.</summary>
public sealed class CaptureTheoryAttribute : TheoryAttribute
{
    public CaptureTheoryAttribute()
    {
        if (!Directory.Exists(LocalData.ScenarioDirectory))
            Skip = $"No decompiled scenario in {LocalData.ScenarioDirectory}.";
        else if (!LocalData.Captures.Any())
            Skip = $"No golden captures in {LocalData.GoldenDirectory}.";
    }
}

/// <summary>
/// Every text the battle shows in the captured battles can be read from the scenario: each
/// location starts a text run, and a run that prints numbers gets as many values as it prints.
/// </summary>
public class ScenarioTextGoldenTests(ITestOutputHelper output)
{
    public static IEnumerable<object[]> Captures => LocalData.Captures.Select(f => new object[] { Path.GetFileName(f) });

    [CaptureTheory]
    [MemberData(nameof(Captures))]
    public async Task Every_shown_text_resolves(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var view = new ResolvingView(new ScenarioText(LocalData.ScenarioDirectory));

        await GoldenReplay.RunAsync(capture, view: view);

        output.WriteLine($"{view.Resolved} texts resolved");
        Assert.True(view.Errors.Count == 0, string.Join("\n", view.Errors.Distinct()));
        Assert.True(view.Resolved > 0);
    }

    private sealed class ResolvingView(ScenarioText text) : IBattleView
    {
        public List<string> Errors { get; } = [];

        public int Resolved { get; private set; }

        public Task ShowAsync(BattleEvent battleEvent)
        {
            switch (battleEvent)
            {
                case MessageEvent m:
                    Check(m.Text, [], m.StringVariables ?? []);
                    break;
                case FlowValueMessageEvent v:
                    Check(v.Text, v.Values, []);
                    break;
                case FlowErrorEvent e:
                    Check(e.Text, [], []);
                    break;
                case ActionLabelsEvent a:
                    if (a.PlayerLabel is { } p)
                        Check(p, [], []);
                    if (a.EnemyLabel is { } en)
                        Check(en, [], []);
                    break;
            }
            return Task.CompletedTask;
        }

        private void Check(ScriptRef source, IReadOnlyList<Sys3Int> values, IReadOnlyList<Sys3Int> stringVariables)
        {
            try
            {
                var run = text.Read(source);
                if (run.NumberCount != values.Count)
                    Errors.Add($"{source}: prints {run.NumberCount} numbers, the event carries {values.Count}");
                else if (run.ComputedStringCount != stringVariables.Count)
                    Errors.Add($"{source}: prints {run.ComputedStringCount} computed strings, the event carries {stringVariables.Count}");
                else
                    run.Format(values, _ => "", stringVariables);
                Resolved++;
            }
            catch (ScenarioTextException e)
            {
                Errors.Add(e.Message);
            }
        }
    }
}
