using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
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

/// <summary>A successful pre-battle strategy as our cut-in shows it (docs/ui/battle-screen.md, "The strategy's cut-in").</summary>
public class BattleStrategyTests
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    [Theory]
    [InlineData(BattleSide.Player, 0, 0, "", false, "")]
    [InlineData(BattleSide.Player, 1, 10, "Strategy +10%", false, "Success")]
    [InlineData(BattleSide.Player, 2, 50, "Strategy +50%", true, "Great Success")]
    [InlineData(BattleSide.Enemy, 11, 10, "Strategy +10%", false, "Success")]
    [InlineData(BattleSide.Enemy, 12, 50, "Strategy +50%", true, "Great Success")]
    public void The_result_gives_the_label_and_the_title(BattleSide side, int result, int percent, string label, bool great, string title)
    {
        var strategy = new BattleStrategy(side, 1, 0, "", result, null);
        Assert.Equal((percent, label, great), (strategy.Percent, strategy.Label, strategy.Great));
        if (result != 0)
            Assert.Equal(title, strategy.Title);
    }

    /// <summary>
    /// The presenter's hold through <c>FIGT作</c>'s player side as the port shows it: from the strategy screen to the
    /// close of the effect's box; the announcement's and the effect's key waits answered by the cut-in, the line's not.
    /// </summary>
    [Fact]
    public void The_presenter_holds_the_strategy_from_its_screen_to_the_effects_close()
    {
        var s = new GameState();
        s.Units[(Sys3Int)12].FaceCg = 10;
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        presenter.Attach(s);
        Assert.Null(presenter.Strategy);

        presenter.Show(new StrategyScreenEvent((Sys3Int)1, (Sys3Int)12));
        Assert.Equal(new BattleStrategy(BattleSide.Player, 12, 10, "", 0, null), presenter.Strategy);
        var backdrop = presenter.StrategyBackdrop;
        Assert.Equal(Surface.ScreenWidth * Surface.Scale * Surface.ScreenHeight * Surface.Scale * 4, backdrop?.Length);
        var screen = (byte[])backdrop!.Clone();

        Box(143, 37);
        Assert.Equal(screen, presenter.StrategyBackdrop);                      // the screen before the boxes
        Assert.True(presenter.StrategyAnswered);                                // the announcement's wait
        Assert.Equal("Ally army's strategy was successful.", presenter.Strategy!.Announcement);
        Close();
        s.Bt_StrategyResult = 1;

        Box(142, 114);
        Assert.False(presenter.StrategyAnswered);                               // the line's wait: the player's click
        Assert.Equal(new BattleSpeech(BattleSide.Player, true, "We were able to take the enemy from behind. Success."),
            presenter.Strategy!.Line);
        Assert.Equal((1, "Strategy +10%"), (presenter.Strategy.Result, presenter.Strategy.Label));
        Close();
        Assert.NotNull(presenter.Strategy!.Line);                               // kept after the bubble closes

        Box(143, 321);
        presenter.Show(new MessageEvent(new ScriptRef("FIGT作.ADV", 327)));
        Assert.True(presenter.StrategyAnswered);                                // the effect's wait
        Close();
        Assert.Null(presenter.Strategy);
        Assert.Null(presenter.StrategyBackdrop);
        Assert.False(presenter.StrategyAnswered);

        void Box(int op, int line)
        {
            presenter.Show(new WindowEvent((Sys3Int)op, Sys3Int.Zero));
            presenter.Show(new MessageEvent(new ScriptRef("FIGT作.ADV", line)));
            presenter.Show(new WindowEvent((Sys3Int)11, Sys3Int.Zero));
        }

        void Close()
        {
            presenter.AfterKey();
            presenter.Show(new WindowEvent((Sys3Int)10, Sys3Int.Zero));
        }
    }

    /// <summary>
    /// The test over every capture, spread over <see cref="Shards"/> classes (<c>Captures0</c> and on) that run in
    /// parallel (Shards.cs).
    /// </summary>
    public abstract class CaptureShard(ITestOutputHelper output)
    {
        private const int Shards = 5;

        /// <summary>
        /// In every captured battle with a successful strategy, the cut-in at the line's key wait is what the script shows: the
        /// strategist (<c>Bt_PlayerStrategist</c> / <c>Bt_EnemyStrategist</c>) and its face, the result (<c>Bt_StrategyResult</c>),
        /// the bubble's line as printed; the cut-in answers exactly the announcement's and the effect's waits, and it is gone
        /// before the battle's field is drawn.
        /// </summary>
        [CaptureTheory]
        [ShardedData(nameof(BattleSceneTests.Captures), typeof(BattleSceneTests), Shards)]
        public async Task The_cut_in_is_what_the_script_shows(string file)
        {
            var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
            var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
            GameState? state = null;
            var printed = new System.Text.StringBuilder();
            int strategies = 0, answered = 0, lines = 0;
            var wrong = new List<string>();
            var view = new ObservingView(e =>
            {
                presenter.Show(e);
                switch (e)
                {
                    case StrategyScreenEvent:
                        strategies++;
                        printed.Clear();
                        break;
                    case WindowEvent { Op.Value: 141 or 142 or 151 or 152 }:
                        printed.Clear();
                        break;
                    case MessageEvent m when presenter.Strategy is not null:
                        printed.Append(Texts.Value.Read(m.Text).Format(strings: v => state is { } st && v >= 0 && v < st.Strings.Length ? st.Strings[v] : "", stringVariables: m.StringVariables));
                        break;
                    case SlotPanelEvent or BattlefieldBackgroundEvent or RoundHeaderEvent when presenter.Strategy is not null:
                        wrong.Add($"strategy {strategies}: still held at {e.GetType().Name}");
                        break;
                }
            });

            Sys3Int Key(Sys3Int captured)
            {
                if (presenter.Strategy is { } shown && state is { } s)
                {
                    if (presenter.StrategyAnswered)
                    {
                        answered++;
                    }
                    else
                    {
                        lines++;
                        bool player = shown.Side == BattleSide.Player;
                        var record = player ? s.Bt_PlayerStrategist : s.Bt_EnemyStrategist;
                        var face = player ? s.Units[record].FaceCg : s.Cmdrs[record].FaceCg;
                        if ((shown.Record, shown.Portrait, shown.Result) != (record.Value, face.Value, s.Bt_StrategyResult.Value))
                            wrong.Add($"strategy {strategies}: shown {shown}");
                        if (shown.Line?.Text != BattleSpeech.Flow(printed.ToString()))
                            wrong.Add($"strategy {strategies}: line \"{shown.Line?.Text}\", printed \"{BattleSpeech.Flow(printed.ToString())}\"");
                        if (shown.Announcement == "" || shown.Label == "")
                            wrong.Add($"strategy {strategies}: no announcement or label");
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
            output.WriteLine($"{file}: {strategies} strategies, {lines} line waits, {answered} waits answered");
            Assert.Equal(strategies, lines);
            Assert.Equal(2 * strategies, answered);
            Assert.Empty(wrong);
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
