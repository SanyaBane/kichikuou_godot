using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The model of our battle screen (docs/ui/battle-screen.md, step 2): the cards and the round's order.</summary>
public class BattleBoardTests(ITestOutputHelper output)
{
    /// <summary>
    /// Rance (unit 1) and Barres (unit 6) against Dodge (commander 5) and Melfeis (commander 4), as in
    /// the week-26 battle; Rance in slot 1, Barres in slot 2, Dodge and Melfeis in the enemy's 1 and 2.
    /// </summary>
    private static GameState Battle()
    {
        var s = new GameState { Unit_StrBase = 20, Cmdr_StrBase = 200 };
        void Unit(int n, string name, string type, int troops, int max, int hp, int maxHp, int face)
        {
            s.Strings[20 + (n - 1) * 3] = name;
            s.Strings[21 + (n - 1) * 3] = type;
            var u = s.Units[n];
            (u.Troops, u.MaxTroops, u.HP, u.MaxHP, u.FaceCg) = ((Sys3Int)troops, (Sys3Int)max, (Sys3Int)hp, (Sys3Int)maxHp, (Sys3Int)face);
        }
        void Cmdr(int n, string name, string type, int troops, int max, int hp, int maxHp, int face, int alone = 0)
        {
            s.Strings[200 + (n - 1) * 3] = name;
            s.Strings[201 + (n - 1) * 3] = type;
            var c = s.Cmdrs[n];
            (c.Troops, c.MaxTroops, c.HP, c.MaxHP, c.FaceCg, c.CgAlone) =
                ((Sys3Int)troops, (Sys3Int)max, (Sys3Int)hp, (Sys3Int)maxHp, (Sys3Int)face, (Sys3Int)alone);
        }
        Unit(1, "Rance", "Leazas Knights", 912, 1000, 8, 8, 1001);
        Unit(6, "Barres", "Leazas Knights", 800, 1000, 4, 5, 1006);
        Cmdr(5, "Dodge", "Leazas Soldiers", 1000, 1000, 6, 6, 1105);
        Cmdr(4, "Melfeis", "Leazas Mages", 300, 300, 4, 4, 0, alone: 1200);
        (s.Bt_PlayerSlot1, s.Bt_PlayerSlot2, s.Bt_EnemySlot1, s.Bt_EnemySlot2) = (1, 6, 5, 4);
        (s.Bt_PSlot3State, s.Bt_PSlot4State, s.Bt_ESlot3State, s.Bt_ESlot4State) = (100, 100, 100, 100);
        return s;
    }

    [Fact]
    public void A_card_shows_the_record_in_its_slot_as_the_state_has_it()
    {
        var s = Battle();
        s.Bt_PlayerOrder2 = 1;
        s.Bt_PSlot2Focus = 2;
        s.Bt_PSlot2State = 12;

        var barres = BattleBoard.Card(s, new BattlePlace(BattleSide.Player, 2))!;
        Assert.Equal((6, "Barres", "Leazas Knights", 1006, 800, 1000, 4, 5), (barres.Record, barres.Name, barres.TroopType, barres.Picture, barres.Troops, barres.MaxTroops, barres.Hp, barres.MaxHp));
        Assert.Equal((StandingOrder.AutoAttack, 2, true, true), (barres.Order, barres.Focus, barres.Acted, barres.Defending));

        var melfeis = BattleBoard.Card(s, new BattlePlace(BattleSide.Enemy, 2))!;
        Assert.Equal((4, "Melfeis", 0, 1200, 1200, null), (melfeis.Record, melfeis.Name, melfeis.FaceCg, melfeis.StandIn, melfeis.Picture, melfeis.Order));
        Assert.False(melfeis.Acted);

        var enemies = BattleBoard.Cards(s, BattleSide.Enemy);
        Assert.Equal(4, enemies.Count);
        Assert.Null(enemies[2]);
        Assert.Null(enemies[3]);
    }

    [Fact]
    public void The_round_goes_fewest_troops_first_ties_to_the_lower_slot_and_the_player()
    {
        var s = Battle();
        Assert.Equal(["E2", "P2", "P1", "E1"], Names(BattleBoard.Remaining(s)));

        s.Units[6].Troops = 300;                                        // a tie with Melfeis: the player first
        Assert.Equal(["P2", "E2", "P1", "E1"], Names(BattleBoard.Remaining(s)));

        s.Units[1].Troops = 300;                                        // three at 300: the lower slot first
        Assert.Equal(["P1", "P2", "E2", "E1"], Names(BattleBoard.Remaining(s)));

        s.Bt_PSlot1State = 1;                                           // acted
        s.Bt_ESlot2State = 20;                                          // defends
        Assert.Equal(["P2", "E1"], Names(BattleBoard.Remaining(s)));
    }

    [Fact]
    public void A_commander_without_troops_acts_first()
    {
        var s = Battle();
        s.Cmdrs[5].Troops = 0;
        Assert.Equal("E1", Names(BattleBoard.Remaining(s))[0]);
    }

    [Fact]
    public void The_strip_lists_the_acted_the_acting_and_the_rest()
    {
        var s = Battle();
        var order = new RoundOrder();
        order.Observe(new RoundHeaderEvent(0, 1, 5000, 0, 0, 4), s);
        Assert.Equal((4, 1), (order.Rounds, order.Round));

        // Melfeis acts first (fewest troops): the enemy turn highlights her panel (6), then her target (1).
        s.Bt_ESlot2State = 1;
        s.Bt_EnemyActSlot = 2;
        order.Observe(new PanelHighlightEvent(6), s);
        order.Observe(new PanelHighlightEvent(1), s);
        Assert.Equal(["Melfeis:Acting", "Barres:Waiting", "Rance:Waiting", "Dodge:Waiting"], Strip(order, s));

        // Barres's turn.
        s.Bt_PSlot2State = 1;
        s.Bt_PlayerActSlot = 2;
        order.Observe(new PanelHighlightEvent(2), s);
        Assert.Equal(["Melfeis:Acted", "Barres:Acting", "Rance:Waiting", "Dodge:Waiting"], Strip(order, s));

        // A new round forgets the acted.
        order.Observe(new RoundHeaderEvent(0, 1, 5000, 0, 0, 3), s);
        Assert.Equal((4, 2), (order.Rounds, order.Round));
        Assert.DoesNotContain(order.Chips(s), c => c.State != ChipState.Waiting);
    }

    /// <summary>
    /// In every captured battle, each turn's actor is the first of <see cref="BattleBoard.Remaining"/>
    /// as the state was before the core's choice (the actor's state back to 0), and the strip of
    /// <see cref="RoundOrder"/> names it as acting.
    /// </summary>
    [Theory]
    [MemberData(nameof(Captures))]
    public async Task The_order_names_the_core_s_choice_in_every_captured_battle(string file)
    {
        if (file == "")
            return;
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        GameState? state = null;
        var order = new RoundOrder();
        int turns = 0;
        var wrong = new List<string>();
        SlotSnapshot? before = null;
        var view = new ObservingView(e =>
        {
            if (state is null)
                return;
            var now = SlotSnapshot.Of(state);
            if (before is not null && RoundOrder.TurnStarted(before, now) is { } actor)
            {
                // The core's choice ran since the last event: the model, on the states before it, names the same place.
                var states = Snapshot(state);
                SetState(state, actor, 0);
                var expected = BattleBoard.Remaining(state);
                Restore(state, states);
                turns++;
                if (expected.Count == 0 || expected[0] != actor)
                    wrong.Add($"round {order.Round}: the core picked {actor}, the model {string.Join(", ", expected)}");
            }
            bool started = before is not null && RoundOrder.TurnStarted(before, now) is not null;
            before = now;
            order.Observe(e, state);
            if (started && order.Acting is { } acting
                && order.Chips(state).Single(c => c.State == ChipState.Acting).Record != BattleBoard.Record(state, acting))
                wrong.Add($"round {order.Round}: the strip's acting chip is not {acting}");
        });
        var report = await GoldenReplay.RunAsync(capture, view: view, started: s => { state = s; order.Reset(); });
        output.WriteLine($"{file}: {turns} turns; replay {(report.Passed ? "matches" : "diverges")}");
        Assert.Empty(wrong);
    }

    public static IEnumerable<object[]> Captures =>
        LocalData.Captures.Any() ? LocalData.Captures.Select(f => new object[] { Path.GetFileName(f) }) : [[""]];

    private static string[] Names(IEnumerable<BattlePlace> places) =>
        [.. places.Select(p => (p.Side == BattleSide.Player ? "P" : "E") + p.Slot)];

    private static string[] Strip(RoundOrder order, GameState s) => [.. order.Chips(s).Select(c => $"{c.Name}:{c.State}")];

    private static Sys3Int[] Snapshot(GameState s) =>
        [s.Bt_PSlot1State, s.Bt_PSlot2State, s.Bt_PSlot3State, s.Bt_PSlot4State, s.Bt_ESlot1State, s.Bt_ESlot2State, s.Bt_ESlot3State, s.Bt_ESlot4State];

    private static void Restore(GameState s, Sys3Int[] v) =>
        (s.Bt_PSlot1State, s.Bt_PSlot2State, s.Bt_PSlot3State, s.Bt_PSlot4State, s.Bt_ESlot1State, s.Bt_ESlot2State, s.Bt_ESlot3State, s.Bt_ESlot4State) =
        (v[0], v[1], v[2], v[3], v[4], v[5], v[6], v[7]);

    private static void SetState(GameState s, BattlePlace p, int value)
    {
        var v = Snapshot(s);
        v[(p.Side == BattleSide.Player ? 0 : 4) + p.Slot - 1] = (Sys3Int)value;
        Restore(s, v);
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
