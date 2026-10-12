using Kichikuou.Original.Tests.Combat;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Kichikuou.TestSupport;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary>
/// <see cref="StrategyRules"/> as the game's router: it needs the game's input, and runs the
/// battle files as a <see cref="Battle"/> on its own context (owner's decision 7).
/// </summary>
public class StrategyRulesTests
{
    [Fact]
    public void Binding_needs_an_input_that_answers_the_battle_and_the_scenes()
    {
        var rules = new StrategyRules(new NoScenario(), new ScriptedStory());
        var battleOnly = new OriginalScriptContext(new GameState(), new ScriptedRandom(), new RecordingView(), new ScriptedInput(), rules);

        var e = Assert.Throws<ArgumentException>(() => rules.Bind(battleOnly));

        Assert.Contains("IGameInput", e.Message);
    }

    [Fact]
    public void The_battle_files_are_routed()
    {
        foreach (var file in new[] { "FIGT_A.ADV", "FIGT_B.ADV", "FIGT.ADV", "死亡捕獲.ADV" })
            Assert.Contains(file, StrategyRules.PortedFiles);
    }

    /// <summary>
    /// <c>%#FIGT_A.ADV</c> through the router fights the same battle as <see cref="Battle.AttackProvinceAsync"/>
    /// on a context of its own: the same draws, story calls, events and memory, on battles with
    /// troops and several commanders a side.
    /// </summary>
    [Theory]
    [InlineData(1u)]
    [InlineData(2u)]
    [InlineData(3u)]
    [InlineData(4u)]
    public async Task An_attack_through_the_router_is_the_battle_itself(uint seed)
    {
        var alone = Attack(seed);
        var direct = new Battle(alone.State, alone.Random, alone.View, alone.Input, alone.Story);
        await direct.AttackProvinceAsync();

        var routed = Attack(seed);
        var rules = new StrategyRules(new NoScenario(), routed.Story);
        var script = new OriginalScriptContext(routed.State, routed.Random, routed.View, routed.Input, rules);
        rules.Bind(script);
        await script.CallFileAsync("FIGT_A.ADV");

        Assert.Equal(alone.Random.Draws, routed.Random.Draws);
        Assert.Equal(alone.Story.Calls, routed.Story.Calls);
        Assert.Equal(alone.View.Events.Select(e => e.GetType().Name),
            routed.View.Events.Where(e => e is not ScreenEvent).Select(e => e.GetType().Name));
        Assert.Equal(new BattleScreenEvent("FIGT_A.ADV", true), routed.View.Events[0]);   // the router frames the battle
        Assert.Equal(new BattleScreenEvent("FIGT_A.ADV", false), routed.View.Events[^1]);
        Assert.Contains(routed.View.Events, e => e is RoundHeaderEvent);
        for (int v = 0; v < VariableCatalog.Variables.Count; v++)
            Assert.True(alone.State.Memory.Var(v) == routed.State.Memory.Var(v), $"VAR{v:D4} {VariableCatalog.Variables[v].Name}");
        foreach (int page in alone.State.Memory.AllocatedPages)
            for (int i = 0; i < alone.State.Memory.PageSize(page); i++)
                Assert.True(alone.State.Memory.ReadPage(page, i) == routed.State.Memory.ReadPage(page, i), $"page {page}[{i}]");
    }

    private sealed record Rig(GameState State, LoggingRandom Random, RecordingView View, AttackingInput Input, ScriptedStory Story);

    /// <summary>Two units with troops against two commanders on province 20, after the battle tests' setup.</summary>
    private static Rig Attack(uint seed)
    {
        var s = new GameState();
        s.StoryStage = 1;
        s.Units[1].Status = 1;
        s.Prov_Cur = 20;
        s.Bt_TargetProv = 20;
        s.Provs[20].Owner = 4;
        s.Bt_BackgroundCg = 900;
        // Units and commanders whose FIGT_M lines call no scene (EndToEndBattleTests).
        int[] units = [44, 42];
        int[] cmdrs = [5, 6];
        s.Bt_PlayerParty1 = (Sys3Int)units[0];
        s.Bt_PlayerParty2 = (Sys3Int)units[1];
        s.Bt_EnemyParty1 = (Sys3Int)cmdrs[0];
        s.Bt_EnemyParty2 = (Sys3Int)cmdrs[1];
        var rng = new Random((int)seed);
        foreach (int id in units)
        {
            var u = s.Units[id];
            u.Troops = u.MaxTroops = (Sys3Int)rng.Next(100, 900);
            u.HP = u.MaxHP = (Sys3Int)rng.Next(20, 60);
            u.Atk = (Sys3Int)rng.Next(10, 40);
            u.Def = (Sys3Int)rng.Next(10, 40);
            u.Attacks = (Sys3Int)rng.Next(1, 6);
            u.Level = (Sys3Int)rng.Next(1, 10);
            u.Status = 2;
        }
        foreach (int id in cmdrs)
        {
            var c = s.Cmdrs[id];
            c.Troops = c.MaxTroops = (Sys3Int)rng.Next(100, 900);
            c.HP = c.MaxHP = (Sys3Int)rng.Next(5, 40);
            c.Atk = (Sys3Int)rng.Next(5, 30);
            c.Def = (Sys3Int)rng.Next(5, 30);
            c.Attacks = (Sys3Int)rng.Next(1, 6);
            c.Status = 1;
        }
        return new Rig(s, new LoggingRandom(seed), new RecordingView(), new AttackingInput(), new ScriptedStory());
    }

    /// <summary>The script's generator from a seed, noting every draw's n.</summary>
    private sealed class LoggingRandom(uint seed) : IScriptRandom
    {
        private readonly XSystem35Random _random = new(new Mt19937Legacy(seed));

        public List<int> Draws { get; } = [];

        public Sys3Int Next(Sys3Int n)
        {
            Draws.Add(n.Value);
            return _random.Next(n);
        }
    }

    /// <summary>Attacks the first enemy it may, or defends; every menu's first option; keys and pauses 0.</summary>
    private sealed class AttackingInput : UnexpectedInput, IGameInput
    {
        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            var direct = menu.Find(FigtSelCommand.Direct);
            return Task.FromResult(direct is { Arguments.Count: > 0 }
                ? new FigtSelChoice(FigtSelCommand.Direct, direct.Arguments[0])
                : new FigtSelChoice(FigtSelCommand.Defend, menu.Find(FigtSelCommand.Defend)!.Arguments[0]));
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) => Task.FromResult<Sys3Int?>(menu.Options[0].Code);

        public override Task<Sys3Int> WaitForKeyAsync() => Task.FromResult(Sys3Int.Zero);

        public override Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Task.FromResult(Sys3Int.Zero);

        public override Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult((Sys3Int)7);
    }
}
