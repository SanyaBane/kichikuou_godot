using Kichikuou.Content;
using Kichikuou.Engine;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Golden;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Local game data: the decompiled scenario and the golden captures (docs/golden-tests.md). Both
/// stay outside the repository; the folders come from <see cref="LocalPaths"/> (<c>local.env</c>,
/// overridden by the environment): <c>KICHIKUOU_EXTRACT_DIR</c> the whole extracted folder (CGs,
/// sounds), <c>KICHIKUOU_SCENARIO_DIR</c> / <c>KICHIKUOU_GOLDEN_DIR</c> one of its subfolders,
/// <c>KICHIKUOU_GAME_DIR</c> the game folder (music). An unset folder is empty: the tests that need
/// it skip or fail.
/// </summary>
public static class LocalData
{
    private static string Extract => LocalPaths.ExtractDirectory ?? "";

    public static string ScenarioDirectory => LocalPaths.ScenarioDirectory ?? "";

    public static string GoldenDirectory => LocalPaths.GoldenDirectory ?? "";

    public static string CgDirectory => Path.Combine(Extract, "cg_png");

    public static string PmsDirectory => Path.Combine(Extract, "cg_pms");

    public static string WavDirectory => Path.Combine(Extract, "wav");

    public static string GameDirectory => LocalPaths.GameDirectory ?? "";

    public static IEnumerable<string> Captures =>
        Directory.Exists(GoldenDirectory)
            ? Directory.EnumerateFiles(GoldenDirectory, "*.golden").Order(StringComparer.Ordinal)
            : [];
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

    private sealed class ResolvingView(ScenarioText text) : IScriptView
    {
        public List<string> Errors { get; } = [];

        public int Resolved { get; private set; }

        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            switch (scriptEvent)
            {
                case MessageEvent m:
                    Check(m.Text, [], m.StringVariables ?? []);
                    break;
                case ValueMessageEvent v:
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
