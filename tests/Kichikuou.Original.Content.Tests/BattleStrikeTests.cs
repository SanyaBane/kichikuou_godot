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

/// <summary>The blow our cards show from the exchange's result screen on (docs/ui/battle-screen.md, "The cards' counters").</summary>
public class BattleStrikeTests(ITestOutputHelper output)
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
    /// In every captured battle the blow is up from the result screen to its key wait, and its values are what the
    /// core has written into the two fighters when the next event comes — so the cards do not move again then.
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(BattleSceneTests.Captures), MemberType = typeof(BattleSceneTests))]
    public async Task A_strike_holds_what_the_core_writes_after_the_key_wait(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        GameState? state = null;
        BattleStrike? landed = null;
        int strikes = 0;
        var wrong = new List<string>();
        var view = new ObservingView(e =>
        {
            if (landed is { } blow && e is not WipedOutMarksEvent and not KeyWaitEvent && state is { } s)
            {
                // The first event after the key wait: the core has applied the exchange.
                var (unit, cmdr) = (s.Units[blow.Unit], s.Cmdrs[blow.Cmdr]);
                var written = new BattleStrike(blow.Unit, unit.Troops.Value, unit.HP.Value, blow.Cmdr, cmdr.Troops.Value, cmdr.HP.Value);
                if (written != blow)
                    wrong.Add($"strike {strikes}: shown {blow}, written {written}");
                landed = null;
            }
            presenter.Show(e);
            if (e is ExchangeResultsEvent)
            {
                strikes++;
                landed = presenter.Strike;
                if (landed is null)
                    wrong.Add($"strike {strikes}: none at the result screen");
            }
            else if (landed is null && presenter.Strike is not null)
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

    private sealed class ObservingView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
