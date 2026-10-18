using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The blow our cards show from the exchange's shots on, and the exchange's counters (docs/ui/battle-screen.md, "The cards'
/// counters", "The exchange's counters").
/// </summary>
public class BattleStrikeTests
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    [Fact]
    public void A_strike_gives_the_two_fighters_cards_its_values_and_leaves_the_others()
    {
        var strike = new BattleStrike(Unit: 3, UnitTroops: 400, UnitHp: 5, Cmdr: 3, CmdrTroops: 90, CmdrHp: 2);
        BattleCard Card(BattleSide side, int record) =>
            new(new BattlePlace(side, 1), record, "", "", 0, 0, 1000, 1000, 8, 8, null, 0, false, false);

        Assert.Equal((400, 5), Pair(strike.Apply(Card(BattleSide.Player, 3))));
        Assert.Equal((90, 2), Pair(strike.Apply(Card(BattleSide.Enemy, 3))));
        Assert.Equal((1000, 8), Pair(strike.Apply(Card(BattleSide.Player, 4))));
        Assert.Equal((1000, 8), Pair(strike.Apply(Card(BattleSide.Enemy, 5))));

        static (int, int) Pair(BattleCard c) => (c.Troops, c.Hp);
    }

    /// <summary>
    /// The test over every capture, spread over <see cref="Shards"/> classes (<c>Captures0</c> and on) that run in
    /// parallel (Shards.cs).
    /// </summary>
    public abstract class CaptureShard(ITestOutputHelper output)
    {
        private const int Shards = 5;

        /// <summary>
        /// In every captured battle the blow is up from the shots' first frame to the result's key wait, the same at the
        /// result screen, its counters running from the fighters' values at the shots to it, and its values are what the
        /// core has written into the two fighters when the next event comes — so the cards do not move again then.
        /// </summary>
        [CaptureTheory]
        [ShardedData(nameof(BattleSceneTests.Captures), typeof(BattleSceneTests), Shards)]
        public async Task A_strike_holds_what_the_core_writes_after_the_key_wait(string file)
        {
            var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
            var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
            GameState? state = null;
            BattleStrike? landed = null;
            int strikes = 0;
            bool waited = false;
            var wrong = new List<string>();
            var view = new ObservingView(e =>
            {
                if (landed is { } blow && waited && state is { } s)
                {
                    // The first event after the key wait: the core has applied the exchange.
                    var (unit, cmdr) = (s.Units[blow.Unit], s.Cmdrs[blow.Cmdr]);
                    var written = new BattleStrike(blow.Unit, unit.Troops.Value, unit.HP.Value, blow.Cmdr, cmdr.Troops.Value, cmdr.HP.Value);
                    if (written != blow)
                        wrong.Add($"strike {strikes}: shown {blow}, written {written}");
                    (landed, waited) = (null, false);
                }
                if (landed is not null && e is KeyWaitEvent)
                    waited = true;
                presenter.Show(e);
                if (e is ShotsFrameEvent { Step.Value: 0 } && state is { } at)
                {
                    strikes++;
                    landed = presenter.Strike;
                    if (landed is null || presenter.ExchangeCounters is not { } c)
                    {
                        wrong.Add($"strike {strikes}: none at the shots");
                        return;
                    }
                    var (unit, cmdr) = (at.Units[landed.Unit], at.Cmdrs[landed.Cmdr]);
                    var expected = new ExchangeCounters(new(unit.Troops.Value, landed.UnitTroops), new(unit.HP.Value, landed.UnitHp),
                        new(cmdr.Troops.Value, landed.CmdrTroops), new(cmdr.HP.Value, landed.CmdrHp));
                    if (c != expected)
                        wrong.Add($"strike {strikes}: counters {c}, expected {expected}");
                }
                else if (e is ExchangeResultsEvent && presenter.Strike != landed)
                {
                    wrong.Add($"strike {strikes}: {presenter.Strike} at the result screen, {landed} at the shots");
                }
                else if (landed is null && (presenter.Strike is not null || presenter.ExchangeCounters is not null))
                {
                    wrong.Add($"a strike left after {e.GetType().Name}");
                }
            });
            await GoldenReplay.RunAsync(capture, view: view, started: s =>
            {
                state = s;
                presenter.Start(s);
            });
            output.WriteLine($"{file}: {strikes} strikes");
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
