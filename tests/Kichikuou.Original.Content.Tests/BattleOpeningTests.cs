using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The battle's opening on our battle screen (docs/ui/battle-screen.md, "The opening").</summary>
public class BattleOpeningTests(ITestOutputHelper output)
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    private static GameState Armies()
    {
        var s = new GameState();
        s.Strings[7] = " Rance Army";
        s.Strings[8] = "Oaks";
        s.Strings[10] = "Leazas Rebel Army";
        return s;
    }

    private static RoundHeaderEvent Header(int duel, int kind, int limit, int player = 0, int enemy = 4, int rounds = 5) =>
        new((Sys3Int)duel, (Sys3Int)kind, (Sys3Int)limit, (Sys3Int)player, (Sys3Int)enemy, (Sys3Int)rounds);

    [Fact]
    public void The_opening_tells_what_the_first_header_prints()
    {
        var s = Armies();
        Assert.Equal(new BattleOpening("Oaks — Offensive Battle", "City: Battle Limit  ·  up to 232", 5, "Rance Army", "Leazas Rebel Army", 0, 4),
            BattleOpening.Of(Header(0, 1, 232), s));
        Assert.Equal(("Oaks — Defensive Battle", "Plains: Battle Limit  ·  up to 5000", (int?)3),
            Parts(BattleOpening.Of(Header(0, 2, 5000, rounds: 3), s)));
        Assert.Equal(("Duel", "Duel: Battle Limit  ·  up to 5000", (int?)null), Parts(BattleOpening.Of(Header(1, 1, 5000), s)));
        Assert.Equal(("Dungeon Battle", "Dungeon: Battle Limit  ·  up to 300", (int?)4), Parts(BattleOpening.Of(Header(0, 3, 300, rounds: 4), s)));
        // A dungeon at the plains' limit reads "Plains", as the script's conditions have it.
        Assert.Equal("Plains: Battle Limit  ·  up to 5000", BattleOpening.Of(Header(0, 3, 5000), s).Limit);

        static (string, string, int?) Parts(BattleOpening o) => (o.Title, o.Limit, o.Turns);
    }

    [Fact]
    public void The_advantages_are_the_walls_and_the_strategy()
    {
        var s = new GameState { Bt_Kind = 1, Bt_WallBonus = 4 };
        Assert.Equal((0, 4), BattleOpening.Advantages(s));
        s.Bt_StrategyResult = 2;
        Assert.Equal((50, 4), BattleOpening.Advantages(s));
        s.Bt_Kind = 2;
        s.Bt_StrategyResult = 11;
        Assert.Equal((4, 10), BattleOpening.Advantages(s));
    }

    [Fact]
    public void A_name_wraps_into_the_fewest_lines_whose_widest_is_the_narrowest()
    {
        static float Chars(string s) => s.Length;
        Assert.Equal(["Rance Army"], BattleOpening.Wrap("Rance Army", 3, Chars, 12));
        Assert.Equal(["Leazas", "Rebel Army"], BattleOpening.Wrap("Leazas Rebel Army", 3, Chars, 12));
        Assert.Equal(["A Certain", "Country’s", "People"], BattleOpening.Wrap("A Certain Country’s People", 3, Chars, 10));
        Assert.Null(BattleOpening.Wrap("A Certain Country’s People", 2, Chars, 10));
        Assert.Null(BattleOpening.Wrap("Destruction", 3, Chars, 8));                  // a word wider than the room
    }

    /// <summary>
    /// In every captured battle each round header waits for its key with our screen's scene up; the first is the
    /// battle's opening and the later ones are not; the advantages our screen shows over the cards are the header's.
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(BattleSceneTests.Captures), MemberType = typeof(BattleSceneTests))]
    public async Task Each_header_is_the_opening_or_a_later_round(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        var scene = new BattleScene(Cgs.Value);
        var order = new RoundOrder();
        GameState? state = null;
        int headers = 0;
        var wrong = new List<string>();
        var view = new ObservingView(e =>
        {
            presenter.Show(e);
            if (state is not { } s)
                return;
            order.Observe(e, s);
            scene.Observe(e, s, order.Acting);
            if (e is not RoundHeaderEvent header)
                return;
            headers++;
            if (presenter.RoundHeader != header || !scene.Active)
                wrong.Add($"header {headers}: held {presenter.RoundHeader is not null}, scene {scene.Active}");
            if ((order.Round == 1) != (headers == 1))
                wrong.Add($"header {headers}: round {order.Round}");
            var opening = BattleOpening.Of(header, s);
            if (BattleOpening.Advantages(s) != (opening.PlayerAdvantage, opening.EnemyAdvantage))
                wrong.Add($"header {headers}: advantages {BattleOpening.Advantages(s)}, printed {(opening.PlayerAdvantage, opening.EnemyAdvantage)}");
            if (opening.Turns is { } turns && turns != s.Bt_RoundsLeft.Value)
                wrong.Add($"header {headers}: {turns} turns");
        });
        await GoldenReplay.RunAsync(capture, view: view, started: s =>
        {
            state = s;
            presenter.Start(s);
            scene.Start(s);
            order.Reset();
        });
        output.WriteLine($"{file}: {headers} round headers");
        Assert.Empty(wrong);
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
