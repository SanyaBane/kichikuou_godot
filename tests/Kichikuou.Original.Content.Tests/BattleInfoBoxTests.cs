using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The battle's info boxes (<c>WAO_SUB</c> 143/153, frame 894) as our battle screen shows them (docs/ui/messages.md, §5
/// step 5): a defence's badge, a fall's stamp, the results plate, our panel for the rest.
/// </summary>
public class BattleInfoBoxTests
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));
    private const string Figt = "FIGT.ADV";

    [Fact]
    public void A_pages_lines_flow_into_sentences_parted_by_empty_lines()
    {
        Assert.Equal(["The battle dragged on and it became night."],
            BattleInfoBox.SentencesOf(["The battle dragged", "on and it became", "night."]));
        Assert.Equal(["Leazas: Damages (822)", "2 EXP obtained."],
            BattleInfoBox.SentencesOf(["Leazas: Damages", " (822)", "", "", "2 EXP obtained."]));
        Assert.Empty(BattleInfoBox.SentencesOf([]));
    }

    /// <summary>
    /// A defence (<c>FIGT.ADV:1399</c>): Barres (unit 6) in the second slot prepares, 7 — the badge on his card, the sentence
    /// without his name and the increase; its wait answered at once.
    /// </summary>
    [ScenarioFact]
    public void A_defence_is_a_badge_on_the_acting_units_card()
    {
        var (presenter, s) = Presenter();
        s.Bt_PlayerSlot2 = 6;
        s.Bt_PlayerActSlot = 2;
        s.Strings[1] = "Barres";
        Open(presenter, 143);
        presenter.Show(new ValueMessageEvent(new ScriptRef(Figt, 1399), [(Sys3Int)7]));
        Assert.Null(presenter.InfoBox);                                         // not before its wait
        Wait(presenter);

        var box = presenter.InfoBox!;
        Assert.Equal((InfoBoxKind.Defence, new BattlePlace(BattleSide.Player, 2), "ENEMY HIT \u22127%"), (box.Kind, box.Place, box.Mark));
        Assert.Equal(["Prepared for the enemy's attack"], box.Sentences);
        Assert.True(box.AnsweredAtOnce);
        Assert.Empty(presenter.Windows);                                        // the original's box is not in the windows' layer

        presenter.AfterKey();
        presenter.Show(new WindowEvent((Sys3Int)10, Sys3Int.Zero));
        Assert.Null(presenter.InfoBox);
    }

    /// <summary>A unit down (<c>FIGTﾐｶDE.ADV:724</c>): a stamp on Barres's (unit 6) card, answered at once; Ex's (commander 1) retreat.</summary>
    [Fact]
    public void A_fall_is_a_stamp_on_the_fallen_card()
    {
        var (presenter, s) = Presenter();
        s.Bt_PlayerSlot3 = 6;
        s.Unit_Cur = 6;
        s.Strings[1] = "Barres";
        Open(presenter, 143);
        presenter.Show(new PrintStringEvent(1));
        presenter.Show(new MessageEvent(new ScriptRef("FIGTﾐｶDE.ADV", 724)));
        Wait(presenter);
        var box = presenter.InfoBox!;
        Assert.Equal((InfoBoxKind.Fall, new BattlePlace(BattleSide.Player, 3), "FELL IN BATTLE", true),
            (box.Kind, box.Place, box.Mark, box.AnsweredAtOnce));
        Close(presenter);

        s.Bt_EnemySlot1 = 1;
        s.Cmdr_Cur = 1;
        Open(presenter, 153);
        presenter.Show(new MessageEvent(new ScriptRef("FIGTﾃｷDE.ADV", 13)));
        Wait(presenter);
        Assert.Equal((InfoBoxKind.Fall, new BattlePlace(BattleSide.Enemy, 1), "RETREATED"),
            (presenter.InfoBox!.Kind, presenter.InfoBox.Place, presenter.InfoBox.Mark));
        Close(presenter);

        s.Cmdr_Cur = 2;                                                         // not on the board: our panel
        Open(presenter, 153);
        presenter.Show(new MessageEvent(new ScriptRef("FIGTﾃｷDE.ADV", 13)));
        Wait(presenter);
        Assert.Equal(InfoBoxKind.Panel, presenter.InfoBox!.Kind);
    }

    /// <summary>Any other box is our panel with its sentences: the Land of Death's damage (<c>FIGT.ADV:272</c>).</summary>
    [ScenarioFact]
    public void Another_box_is_our_panel()
    {
        var (presenter, _) = Presenter();
        Open(presenter, 143);
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 272)));
        Wait(presenter);
        var box = presenter.InfoBox!;
        Assert.Equal((InfoBoxKind.Panel, null, ""), (box.Kind, box.Place, box.Mark));
        Assert.Equal(["The Land of Death's corrupt atmosphere damaged all units."], box.Sentences);
    }

    /// <summary>
    /// The end: the night's box answered at once, the tally the plate with its reason (one click), Barres's (unit 6) level-up
    /// its first page answered at once and its second a row on the plate; a new battle forgets them.
    /// </summary>
    [ScenarioFact]
    public void The_end_gathers_into_one_plate()
    {
        var (presenter, s) = Presenter();
        s.StoryStage = 1;
        s.Strings[10] = "Leazas Reb";
        s.Bt_Result = 1;

        Open(presenter, 143);
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 453)));
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 458)));
        Wait(presenter);
        Assert.Equal(InfoBoxKind.Passed, presenter.InfoBox!.Kind);
        Close(presenter, op: 10);

        Open(presenter, 143);
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 2994)));
        presenter.Show(new ValueMessageEvent(new ScriptRef(Figt, 3003), [(Sys3Int)822, (Sys3Int)1105, (Sys3Int)2]));
        Wait(presenter);
        var tally = presenter.InfoBox!;
        Assert.Equal(InfoBoxKind.Results, tally.Kind);
        Assert.Equal(new BattleResults("Night Falls", "The battle dragged on and it became night.", "Leazas", 822, "Leazas Reb", 1105, 2, []),
            tally.Results, new ResultsComparer());
        Close(presenter, op: 10);

        s.Unit_StrBase = 100;
        s.Unit_Cur = 6;
        s.Strings[115] = "Barres";
        s.Strings[1] = "Barres";
        Open(presenter, 143);
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 3065)));
        Wait(presenter);
        Assert.Equal(InfoBoxKind.Passed, presenter.InfoBox!.Kind);              // "Barres leveled up."
        presenter.AfterKey();
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 3071)));
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 3142)));
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 3157)));
        Wait(presenter);
        var levels = presenter.InfoBox!;
        Assert.Equal(InfoBoxKind.Results, levels.Kind);
        var up = Assert.Single(levels.Results!.LevelUps);
        Assert.Equal("Barres", up.Unit);
        Assert.Equal(["Leader Attacks +1", "Def on Defense UP"], up.Gains);
        Close(presenter, op: 10);

        Open(presenter, 143);                                                   // a second unit's: two rows
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 3065)));
        Wait(presenter);
        presenter.AfterKey();
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 3071)));
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 3157)));            // nothing rose: " UP" alone (BTX-09)
        Wait(presenter);
        Assert.Equal(2, presenter.InfoBox!.Results!.LevelUps.Count);
        Assert.Equal(["Leader Attacks +1", "UP"], presenter.InfoBox.Results.LevelUps[1].Gains);
        Close(presenter, op: 10);

        presenter.Show(new RoundHeaderEvent(Sys3Int.Zero, (Sys3Int)1, (Sys3Int)5000, Sys3Int.Zero, Sys3Int.Zero, (Sys3Int)10));
        presenter.AfterKey();
        Open(presenter, 143);                                                   // a tally without a reason: a duel's
        presenter.Show(new MessageEvent(new ScriptRef(Figt, 2994)));
        presenter.Show(new ValueMessageEvent(new ScriptRef(Figt, 3003), [(Sys3Int)0, (Sys3Int)5, (Sys3Int)9]));
        Wait(presenter);
        Assert.Equal(("Defeat", ""), (presenter.InfoBox!.Results!.Title, presenter.InfoBox.Results.Reason));
    }

    /// <summary>The test over every capture, spread over <see cref="Shards"/> classes that run in parallel (Shards.cs).</summary>
    public abstract class CaptureShard(ITestOutputHelper output)
    {
        private const int Shards = 5;

        /// <summary>
        /// In every captured battle, each info box at its key wait is what the script shows: a defence's badge holds the acting
        /// slot's new state, a fall's stamp lands on a card whose record is down (HP 0), the results plate holds the battle's
        /// losses and EXP (<c>Bt_PlayerLosses</c>, <c>Bt_EnemyLosses</c>, <c>Tmp0001</c>) and one row more at each level-up's
        /// second page, a panel's sentences are resolved; every tally is a plate.
        /// </summary>
        [CaptureTheory]
        [ShardedData(nameof(BattleSceneTests.Captures), typeof(BattleSceneTests), Shards)]
        public async Task Each_box_is_what_the_script_shows(string file)
        {
            var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
            var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
            GameState? state = null;
            var counts = new Dictionary<InfoBoxKind, int>();
            int tallies = 0, levelUps = 0, rows = 0;
            var wrong = new List<string>();
            var view = new ObservingView(e =>
            {
                presenter.Show(e);
                if (e is MessageEvent { Text: { File: Figt, Line: 2994 or 2999 } })
                    tallies++;
                if (e is MessageEvent { Text: { File: Figt, Line: 3071 } })
                    levelUps++;
            });

            Sys3Int Key(Sys3Int captured)
            {
                if (presenter.InfoBox is { } box && state is { } s)
                {
                    counts[box.Kind] = counts.GetValueOrDefault(box.Kind) + 1;
                    switch (box)
                    {
                        case { Kind: InfoBoxKind.Defence, Place: { } place }:
                            if (box.Mark != BattlePresenter.DefenceMark(BattleBoard.StateOf(s, place)))
                                wrong.Add($"defence {box.Mark} at {place}, state {BattleBoard.StateOf(s, place)}");
                            break;
                        case { Kind: InfoBoxKind.Fall, Place: { } place }:
                            int record = BattleBoard.Record(s, place);
                            var hp = place.Side == BattleSide.Player ? s.Units[(Sys3Int)record].HP : s.Cmdrs[(Sys3Int)record].HP;
                            if (hp != 0)
                                wrong.Add($"fall {box.Mark} at {place}: record {record} has HP {hp}");
                            break;
                        case { Kind: InfoBoxKind.Results, Results: { } r }:
                            if ((r.PlayerDamages, r.EnemyDamages, r.Exp) != (s.Bt_PlayerLosses.Value, s.Bt_EnemyLosses.Value, s.Tmp0001.Value)
                                || r.Title.Length == 0 || r.PlayerArmy.Length == 0 || r.EnemyArmy.Length == 0)
                                wrong.Add($"results {r}");
                            rows += r.LevelUps.Count == 0 ? 0 : 1;
                            if (r.LevelUps.Count > 0 && r.LevelUps[^1].Gains.Count < 2)
                                wrong.Add($"level-up {r.LevelUps[^1].Unit}: {string.Join(" / ", r.LevelUps[^1].Gains)}");
                            break;
                        case { Kind: InfoBoxKind.Panel }:
                            if (box.Sentences.Any(t => t.Contains(".ADV:")))
                                wrong.Add($"panel {string.Join(" ", box.Sentences)}");
                            break;
                    }
                }
                presenter.AfterKey();
                return captured;
            }

            await GoldenReplay.RunAsync(capture, view: view, started: s =>
            {
                state = s;
                presenter.Start(s);
            }, answers: new ReplayAnswers(Key: Key));
            output.WriteLine($"{file}: {string.Join(", ", counts.Select(c => $"{c.Key} {c.Value}"))}; {tallies} tallies, {levelUps} level-ups");
            Assert.Empty(wrong);
            Assert.Equal(tallies + levelUps, counts.GetValueOrDefault(InfoBoxKind.Results));
            Assert.Equal(levelUps, rows);
            Assert.Empty(presenter.Unresolved);
        }
    }

    [Shard(0)] public sealed class Captures0(ITestOutputHelper output) : CaptureShard(output);
    [Shard(1)] public sealed class Captures1(ITestOutputHelper output) : CaptureShard(output);
    [Shard(2)] public sealed class Captures2(ITestOutputHelper output) : CaptureShard(output);
    [Shard(3)] public sealed class Captures3(ITestOutputHelper output) : CaptureShard(output);
    [Shard(4)] public sealed class Captures4(ITestOutputHelper output) : CaptureShard(output);

    private static (BattlePresenter, GameState) Presenter()
    {
        var s = new GameState();
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        presenter.Attach(s);
        return (presenter, s);
    }

    private static void Open(BattlePresenter presenter, int op) => presenter.Show(new WindowEvent((Sys3Int)op, Sys3Int.Zero));

    /// <summary>Op 11, the box's key wait.</summary>
    private static void Wait(BattlePresenter presenter) => presenter.Show(new WindowEvent((Sys3Int)11, Sys3Int.Zero));

    private static void Close(BattlePresenter presenter, int op = 10)
    {
        presenter.AfterKey();
        presenter.Show(new WindowEvent((Sys3Int)op, Sys3Int.Zero));
    }

    private sealed class ResultsComparer : IEqualityComparer<BattleResults?>
    {
        public bool Equals(BattleResults? a, BattleResults? b) => a is not null && b is not null
            && (a.Title, a.Reason, a.PlayerArmy, a.PlayerDamages, a.EnemyArmy, a.EnemyDamages, a.Exp, a.LevelUps.Count)
            == (b.Title, b.Reason, b.PlayerArmy, b.PlayerDamages, b.EnemyArmy, b.EnemyDamages, b.Exp, b.LevelUps.Count);

        public int GetHashCode(BattleResults? results) => 0;
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
