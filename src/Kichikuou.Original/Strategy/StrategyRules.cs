using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// The ported rule files as story hooks: a call of <c>MAIN.ADV</c> (the week loop, which never
/// returns; the game enters it by the jump <c>&amp;#MAIN.ADV:</c>), <c>COM.ADV</c>, <c>COMMAND.ADV</c>,
/// one of the ported command files or one of their picker screens, a nation's turn or defender
/// picker (<c>TCﾘｰｻﾞｽ</c>, <c>TCﾘｰｻﾞｽ2</c>, <c>Tﾍﾙﾏﾝ</c>, <c>Tﾍﾙﾏﾝ2</c>, <c>Tﾊﾟｯﾄﾝ</c>, <c>Tｾﾞｽ</c>, <c>Tｾﾞｽ2</c>, <c>Tｹｲﾌﾞﾘｽ</c>, <c>Tｹｲﾌﾞﾘｽ2</c>, <c>TJAPAN</c>, <c>TJAPAN2</c>, <c>Tﾙﾄﾞﾗ</c>), or a battle file
/// (<c>FIGT_A</c>, <c>FIGT_B</c>, <c>FIGT</c>, <c>死亡捕獲</c>, run as a <see cref="Battle"/>; also
/// <c>FIGTATTK</c>, the battle's result animation, which the scene <c>FIGT表示</c> calls) runs its
/// port on the bound context, and so does the save screen (<c>ﾛｰﾄﾞｾｰﾌﾞ</c>, on the context's
/// save slots); every other file goes to the hooks it wraps (whatever is not ported). Given to a <see cref="SceneRunner"/> as its rule hooks, so a scene that
/// calls a rule file and a rule file that calls a scene both work:
/// <code>
/// var rules = new StrategyRules(scenario, unported);
/// var runner = new SceneRunner(scenes, rules);
/// var script = new OriginalScriptContext(state, random, view, input, runner);   // input: an IGameInput
/// runner.Bind(script);
/// rules.Bind(script);
/// </code>
/// The battle and the screens it calls (the party picker of <c>FIGT_B</c>) share the context's
/// input, so it must answer both: <see cref="IGameInput"/>, checked when the router is bound.
/// </summary>
public sealed class StrategyRules(IScenarioSource scenario, IStoryHooks unported) : IStoryHooks
{
    private OriginalScriptContext? _script;

    /// <summary>The files this router runs itself.</summary>
    public static readonly IReadOnlySet<string> PortedFiles = new HashSet<string>(StringComparer.Ordinal)
    {
        "MAIN.ADV", "COM.ADV", "COMMAND.ADV", "Cﾃｷﾄｼ.ADV", "Cﾃｷﾄｼ2.ADV", "Cﾌﾞｶ.ADV", "Cﾊｰﾚﾑ.ADV", "Cｼｮｳﾆﾝ.ADV", "Cｼﾊｲﾄｼ.ADV", "Cﾆﾝｼﾞｬ.ADV",
        "Cﾃｲﾄ.ADV", "Cﾀｶﾗ.ADV", "各種DATA.ADV", "Cﾒｲｷｭｳ.ADV",
        "Sﾌﾞﾀｲ.ADV", "Sﾌﾞｶ.ADV", "Sﾊｰﾚﾑ.ADV", "Sｱｲﾃﾑ.ADV", "TCﾘｰｻﾞｽ.ADV", "TCﾘｰｻﾞｽ2.ADV", "Tﾍﾙﾏﾝ.ADV", "Tﾍﾙﾏﾝ2.ADV", "Tﾊﾟｯﾄﾝ.ADV", "Tｾﾞｽ.ADV", "Tｾﾞｽ2.ADV", "Tｹｲﾌﾞﾘｽ.ADV", "Tｹｲﾌﾞﾘｽ2.ADV", "TJAPAN.ADV", "TJAPAN2.ADV", "Tﾙﾄﾞﾗ.ADV", "FIGT_A.ADV", "FIGT_B.ADV", "FIGT.ADV",
        "死亡捕獲.ADV", "FIGTATTK.ADV", "ﾛｰﾄﾞｾｰﾌﾞ.ADV",
    };

    /// <summary>The context the rule files run on; set once with <see cref="Bind"/>.</summary>
    public OriginalScriptContext Script => _script ?? throw new InvalidOperationException("The strategic rules are not bound to a context.");

    /// <summary>
    /// Binds the router to the context whose hooks it serves (built after it, so bound afterwards).
    /// The context's input must be an <see cref="IGameInput"/>: the battles run here.
    /// </summary>
    public void Bind(OriginalScriptContext script)
    {
        if (_script is not null && !ReferenceEquals(_script, script))
            throw new InvalidOperationException("The strategic rules are already bound to another context.");
        if (script.Input is not IGameInput)
            throw new ArgumentException("The game's input must answer the battles and the scenes (IGameInput).", nameof(script));
        _script = script;
    }

    /// <summary><c>COMMAND</c> from its start, or where its Load/Save returns after a load of a save made in it (<see cref="OriginalScriptContext.ResumeCommand"/>).</summary>
    private Task Command()
    {
        bool resume = Script.ResumeCommand;
        Script.ResumeCommand = false;
        return resume ? new CommandPhase(Script).ResumeAfterSaveAsync() : new CommandPhase(Script).RunAsync();
    }

    /// <summary>A battle on the bound context, with its input.</summary>
    private Battle Battle => new(Script, (IGameInput)Script.Input);

    /// <inheritdoc />
    public Task RunEventAsync(string file, Sys3Int entry) => file switch
    {
        "MAIN.ADV" => new WeekLoop(Script, scenario).RunAsync(),
        "COM.ADV" => new WeeklyEvents(Script).RunAsync(),
        "COMMAND.ADV" => Command(),
        "Cﾃｷﾄｼ.ADV" => new EnemyCityCommand(Script, scenario).RunAsync(),
        "Cﾃｷﾄｼ2.ADV" => new DistantCityCommand(Script).RunAsync(),
        "Cﾌﾞｶ.ADV" => new SubordinatesCommand(Script).RunAsync(),
        "Cﾊｰﾚﾑ.ADV" => new HaremCommand(Script).RunAsync(),
        "Cｼｮｳﾆﾝ.ADV" => new MerchantCommand(Script).RunAsync(),
        "Cｼﾊｲﾄｼ.ADV" => new OwnCityCommand(Script).RunAsync(),
        "Cﾆﾝｼﾞｬ.ADV" => new NinjaCommand(Script).RunAsync(),
        "Cﾃｲﾄ.ADV" => new ConstructionCommand(Script).RunAsync(),
        "Cﾀｶﾗ.ADV" => new TreasureCommand(Script).RunAsync(),
        "各種DATA.ADV" => new AllDataCommand(Script).RunAsync(),
        "Cﾒｲｷｭｳ.ADV" => new DungeonCommand(Script, scenario).RunAsync(),
        "Sﾌﾞﾀｲ.ADV" => new DeploymentScreen(Script, scenario).RunAsync(),
        "Sﾌﾞｶ.ADV" => new SubordinateScreen(Script, scenario).RunAsync(),
        "Sﾊｰﾚﾑ.ADV" => new HaremScreen(Script, scenario).RunAsync(),
        "Sｱｲﾃﾑ.ADV" => new ItemScreen(Script, scenario).RunAsync(),
        "TCﾘｰｻﾞｽ.ADV" => new RebelTurn(Script, scenario).RunAsync(),
        "TCﾘｰｻﾞｽ2.ADV" => new RebelDefenders(Script, scenario).RunAsync(),
        "Tﾍﾙﾏﾝ.ADV" => new HelmanTurn(Script, scenario).RunAsync(),
        "Tﾍﾙﾏﾝ2.ADV" => new HelmanDefenders(Script, scenario).RunAsync(),
        "Tﾊﾟｯﾄﾝ.ADV" => new PattonTurn(Script, scenario).RunAsync(),
        "Tｾﾞｽ.ADV" => new ZethTurn(Script, scenario).RunAsync(),
        "Tｾﾞｽ2.ADV" => new ZethDefenders(Script, scenario).RunAsync(),
        "Tｹｲﾌﾞﾘｽ.ADV" => new KayblisTurn(Script, scenario).RunAsync(),
        "Tｹｲﾌﾞﾘｽ2.ADV" => new KayblisDefenders(Script, scenario).RunAsync(),
        "TJAPAN.ADV" => new JapanTurn(Script, scenario).RunAsync(),
        "TJAPAN2.ADV" => new JapanDefenders(Script, scenario).RunAsync(),
        "Tﾙﾄﾞﾗ.ADV" => new CreatorTurn(Script, scenario).RunAsync(),
        "FIGT_A.ADV" => BattleAsync(file, battle => battle.AttackProvinceAsync()),
        "FIGT_B.ADV" => BattleAsync(file, battle => battle.DefendProvinceAsync()),
        "FIGT.ADV" => BattleAsync(file, battle => battle.FightAsync()),
        "死亡捕獲.ADV" => BattleAsync(file, battle => battle.DeathCaptureAsync()),
        "FIGTATTK.ADV" => Battle.PlayExchangeAnimationAsync(),            // from FIGT表示, inside its own screen
        "ﾛｰﾄﾞｾｰﾌﾞ.ADV" => new LoadSaveScreen(Script, scenario).RunAsync(),
        _ => unported.RunEventAsync(file, entry),
    };

    /// <summary>A battle file between its <see cref="BattleScreenEvent"/>s.</summary>
    private async Task BattleAsync(string file, Func<Battle, Task> run)
    {
        await Script.ShowAsync(new BattleScreenEvent(file, true));
        await run(Battle);
        await Script.ShowAsync(new BattleScreenEvent(file, false));
    }
}
