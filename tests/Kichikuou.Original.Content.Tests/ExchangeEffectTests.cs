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

/// <summary>The effect an exchange plays from its shots (docs/ui/battle-screen.md, "The exchange's effects").</summary>
public class ExchangeEffectTests(ITestOutputHelper output)
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    [Theory]
    [InlineData(1, 99, 3, ExchangeEffectKind.Bow, BattleSide.Player)]
    [InlineData(2, 99, 1, ExchangeEffectKind.Magic, BattleSide.Player)]
    [InlineData(99, 2, 2, ExchangeEffectKind.Magic, BattleSide.Enemy)]
    [InlineData(99, 1, 4, ExchangeEffectKind.Bow, BattleSide.Enemy)]
    [InlineData(0, 0, 3, ExchangeEffectKind.Melee, BattleSide.Player)]
    [InlineData(0, 0, 4, ExchangeEffectKind.Melee, BattleSide.Enemy)]
    public void The_firing_side_plays_and_in_a_two_sided_exchange_the_turns_actor(
        int player, int enemy, int msgMode, ExchangeEffectKind kind, BattleSide side)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)player;
        s.Bt_EnemyAction = (Sys3Int)enemy;
        s.Bt_MsgMode = (Sys3Int)msgMode;
        Assert.Equal(new ExchangeEffect(kind, side), ExchangeEffect.Of(s));
    }

    [Fact]
    public void Nothing_plays_when_neither_side_fires()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)99;
        s.Bt_EnemyAction = (Sys3Int)99;
        Assert.Null(ExchangeEffect.Of(s));
    }

    /// <summary>
    /// In every captured battle each exchange's shots start an effect: its side fires (its action is not 99, and it
    /// has a shot), and a two-sided exchange is direct against direct with the turn's actor in <c>Bt_MsgMode</c> (3 or 4).
    /// </summary>
    [CaptureTheory]
    [MemberData(nameof(BattleSceneTests.Captures), MemberType = typeof(BattleSceneTests))]
    public async Task Every_exchange_plays_the_effect_of_a_side_that_fires(string file)
    {
        var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        var wrong = new List<string>();
        var kinds = new Dictionary<string, int>();
        int exchanges = 0;
        var view = new ObservingView(e =>
        {
            presenter.Show(e);
            if (e is not ShotsFrameEvent { Step.Value: 0 } || presenter.State is not { } s)
                return;
            exchanges++;
            if (presenter.Effect is not { } effect)
            {
                wrong.Add($"exchange {exchanges}: no effect (actions {s.Bt_PlayerAction.Value}, {s.Bt_EnemyAction.Value})");
                return;
            }
            string key = $"{effect.Side} {effect.Kind}";
            kinds[key] = kinds.GetValueOrDefault(key) + 1;
            bool player = effect.Side == BattleSide.Player;
            var shots = player
                ? new[] { s.Bt_PTroopVsTroopShots, s.Bt_PTroopVsCmdrShots, s.Bt_PCmdrVsTroopShots, s.Bt_PCmdrVsCmdrShots }
                : new[] { s.Bt_ETroopVsTroopShots, s.Bt_ETroopVsCmdrShots, s.Bt_ECmdrVsTroopShots, s.Bt_ECmdrVsCmdrShots };
            if ((player ? s.Bt_PlayerAction : s.Bt_EnemyAction) == 99 || shots.All(v => v == 0))
                wrong.Add($"exchange {exchanges}: {key} does not fire");
            bool twoSided = s.Bt_PlayerAction != 99 && s.Bt_EnemyAction != 99;
            if (twoSided && (s.Bt_PlayerAction != 0 || s.Bt_EnemyAction != 0 || s.Bt_MsgMode.Value is not (3 or 4)))
                wrong.Add($"exchange {exchanges}: two-sided, actions {s.Bt_PlayerAction.Value}, {s.Bt_EnemyAction.Value}, mode {s.Bt_MsgMode.Value}");
        });
        await GoldenReplay.RunAsync(capture, view: view, started: presenter.Start);
        output.WriteLine($"{file}: {exchanges} exchanges; " + string.Join(", ", kinds.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value}")));
        Assert.Equal(exchanges, presenter.EffectsStarted);
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
