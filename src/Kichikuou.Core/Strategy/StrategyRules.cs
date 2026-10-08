using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// The strategic rule files as story hooks: a call of <c>MAIN.ADV</c> (the week loop, which never
/// returns; the game enters it by the jump <c>&amp;#MAIN.ADV:</c>), <c>COM.ADV</c>, <c>COMMAND.ADV</c>, one of
/// the ported command files or one of their picker screens runs its port on the bound context;
/// every other file goes to the hooks it wraps (the battle, the save screen — whatever is not
/// ported here). Given to a
/// <see cref="SceneRunner"/> as its rule hooks, so a scene that calls a rule file and a rule file
/// that calls a scene both work:
/// <code>
/// var rules = new StrategyRules(scenario, unported);
/// var runner = new SceneRunner(scenes, rules);
/// var script = new ScriptContext(state, random, view, input, runner);
/// runner.Bind(script);
/// rules.Bind(script);
/// </code>
/// </summary>
public sealed class StrategyRules(IScenarioSource scenario, IStoryHooks unported) : IStoryHooks
{
    private ScriptContext? _script;

    /// <summary>The files this router runs itself.</summary>
    public static readonly IReadOnlySet<string> PortedFiles = new HashSet<string>(StringComparer.Ordinal)
    {
        "MAIN.ADV", "COM.ADV", "COMMAND.ADV", "Cﾃｷﾄｼ.ADV", "Cﾃｷﾄｼ2.ADV", "Cﾌﾞｶ.ADV", "Cﾊｰﾚﾑ.ADV", "Cﾒｲｷｭｳ.ADV",
        "Sﾌﾞﾀｲ.ADV", "Sﾌﾞｶ.ADV", "Sﾊｰﾚﾑ.ADV",
    };

    /// <summary>The context the rule files run on; set once with <see cref="Bind"/>.</summary>
    public ScriptContext Script => _script ?? throw new InvalidOperationException("The strategic rules are not bound to a context.");

    /// <summary>Binds the router to the context whose hooks it serves (built after it, so bound afterwards).</summary>
    public void Bind(ScriptContext script)
    {
        if (_script is not null && !ReferenceEquals(_script, script))
            throw new InvalidOperationException("The strategic rules are already bound to another context.");
        _script = script;
    }

    /// <inheritdoc />
    public Task RunEventAsync(string file, Sys3Int entry) => file switch
    {
        "MAIN.ADV" => new WeekLoop(Script).RunAsync(),
        "COM.ADV" => new WeeklyEvents(Script).RunAsync(),
        "COMMAND.ADV" => new CommandPhase(Script).RunAsync(),
        "Cﾃｷﾄｼ.ADV" => new EnemyCityCommand(Script).RunAsync(),
        "Cﾃｷﾄｼ2.ADV" => new DistantCityCommand(Script).RunAsync(),
        "Cﾌﾞｶ.ADV" => new SubordinatesCommand(Script).RunAsync(),
        "Cﾊｰﾚﾑ.ADV" => new HaremCommand(Script).RunAsync(),
        "Cﾒｲｷｭｳ.ADV" => new DungeonCommand(Script, scenario).RunAsync(),
        "Sﾌﾞﾀｲ.ADV" => new DeploymentScreen(Script, scenario).RunAsync(),
        "Sﾌﾞｶ.ADV" => new SubordinateScreen(Script, scenario).RunAsync(),
        "Sﾊｰﾚﾑ.ADV" => new HaremScreen(Script, scenario).RunAsync(),
        _ => unported.RunEventAsync(file, entry),
    };
}
