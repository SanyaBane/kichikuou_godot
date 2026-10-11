using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

/// <summary>
/// The battle of <c>FIGT*.ADV</c>, ported one-to-one (see <c>docs/battle/</c> and
/// <c>docs/porting-conventions.md</c>). All state lives in <see cref="State"/>; the partial
/// files hold the procedures: the flow (<c>docs/battle/01-flow.md</c>) and the resolution of
/// one exchange (<c>docs/battle/02-resolution.md</c>). The shared services (message windows,
/// map, sound, scene calls, draws) are the <see cref="ScriptContext"/> it runs on.
/// </summary>
public sealed partial class Battle
{
    /// <summary>A battle on a context shared with the rest of the game; <paramref name="input"/> is its input too.</summary>
    public Battle(ScriptContext script, IBattleInput input)
    {
        if (!ReferenceEquals(script.Input, input))
            throw new ArgumentException("The battle input must be the context's input.", nameof(input));
        Script = script;
        Input = input;
    }

    /// <summary>A battle on a context of its own.</summary>
    public Battle(GameState state, IScriptRandom random, IScriptView view, IBattleInput input, IStoryHooks story)
        : this(new ScriptContext(state, random, view, input, story), input)
    {
    }

    /// <summary>The shared services the battle runs on.</summary>
    public ScriptContext Script { get; }

    /// <summary>The game state the battle reads and changes.</summary>
    public GameState State => Script.State;

    private IScriptView View => Script.View;

    private IBattleInput Input { get; }

    /// <summary>The player-unit record selected by <c>Unit_Cur</c>, re-read on every access.</summary>
    private UnitRecord Unit => State.Units[State.Unit_Cur];

    /// <summary>The enemy-commander record selected by <c>Cmdr_Cur</c>, re-read on every access.</summary>
    private CmdrRecord Cmdr => State.Cmdrs[State.Cmdr_Cur];

    /// <summary>The province record selected by <c>Prov_Cur</c>, re-read on every access.</summary>
    private ProvRecord Prov => State.Provs[State.Prov_Cur];

    /// <summary>The item record selected by <c>Item_Cur</c>, re-read on every access.</summary>
    private ItemRecord Item => State.Items[State.Item_Cur];

    /// <summary><c>Y 4, n:</c> (<see cref="ScriptContext.Rnd"/>).</summary>
    private Sys3Int Rnd(Sys3Int n) => Script.Rnd(n);

    /// <summary>
    /// <c>G n</c> of a battle file, drawn through the battle's own events: the engine counts the
    /// load (<see cref="ScriptContext.CountCgLoad"/>).
    /// </summary>
    private void CountCgLoad(Sys3Int cg) => Script.CountCgLoad(cg);
}

/// <summary>Something the battle shows; the presentation layer decides how.</summary>
public abstract record BattleEvent : ScriptEvent;
