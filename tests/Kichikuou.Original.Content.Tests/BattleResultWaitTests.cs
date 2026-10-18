using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The exchange's result wait our screen answers itself after a hold unless a wiped-out mark is up
/// (docs/ui/battle-screen.md, "The exchange's result").
/// </summary>
public class BattleResultWaitTests
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    /// <summary>
    /// The test over every capture, spread over <see cref="Shards"/> classes (<c>Captures0</c> and on) that run in
    /// parallel (Shards.cs).
    /// </summary>
    public abstract class CaptureShard(ITestOutputHelper output)
    {
        private const int Shards = 5;

        /// <summary>
        /// In every captured battle the result screen's key wait, and only it, is the exchange's result wait, wiped out
        /// exactly when a mark is shown, and the next event ends it.
        /// </summary>
        [CaptureTheory]
        [ShardedData(nameof(BattleSceneTests.Captures), typeof(BattleSceneTests), Shards)]
        public async Task Only_the_result_screen_waits_as_the_exchange_result_marked_by_its_wiped_out_marks(string file)
        {
            var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
            var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
            WipedOutMarksEvent? marks = null;
            int results = 0, wipedOut = 0;
            var wrong = new List<string>();
            var view = new ObservingView(e =>
            {
                presenter.Show(e);
                if (e is WipedOutMarksEvent m)
                    marks = m;
                bool resultWait = e is KeyWaitEvent && marks is not null;
                if (resultWait)
                {
                    results++;
                    bool shown = marks is { PlayerTroops: true } or { EnemyTroops: true } or { PlayerCmdr: true } or { EnemyCmdr: true };
                    if (shown)
                        wipedOut++;
                    if (!presenter.ExchangeResultWaits || presenter.ExchangeResultWipedOut != shown)
                        wrong.Add($"result {results}: waits {presenter.ExchangeResultWaits}, wiped out {presenter.ExchangeResultWipedOut}, marks {shown}");
                    marks = null;
                }
                else if (presenter.ExchangeResultWaits || presenter.ExchangeResultWipedOut)
                {
                    wrong.Add($"a result wait after {e.GetType().Name}");
                }
            });
            await GoldenReplay.RunAsync(capture, view: view, started: presenter.Start);
            output.WriteLine($"{file}: {results} results, {wipedOut} with a wiped-out mark");
            Assert.Empty(wrong.Distinct());
        }
    }

    [Shard(0)] public sealed class Captures0(ITestOutputHelper output) : CaptureShard(output);
    [Shard(1)] public sealed class Captures1(ITestOutputHelper output) : CaptureShard(output);
    [Shard(2)] public sealed class Captures2(ITestOutputHelper output) : CaptureShard(output);
    [Shard(3)] public sealed class Captures3(ITestOutputHelper output) : CaptureShard(output);
    [Shard(4)] public sealed class Captures4(ITestOutputHelper output) : CaptureShard(output);

    private sealed class ObservingView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
