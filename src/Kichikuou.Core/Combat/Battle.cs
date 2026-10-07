using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

/// <summary>
/// The battle of <c>FIGT*.ADV</c>, ported one-to-one (see <c>docs/battle/</c> and
/// <c>docs/porting-conventions.md</c>). All state lives in <see cref="State"/>; the partial
/// files hold the procedures: the flow (<c>docs/battle/01-flow.md</c>) and the resolution of
/// one exchange (<c>docs/battle/02-resolution.md</c>).
/// </summary>
public sealed partial class Battle(
    GameState state,
    IScriptRandom random,
    IBattleView view,
    IBattleInput input,
    IStoryHooks story)
{
    /// <summary>The game state the battle reads and changes.</summary>
    public GameState State { get; } = state;

    private IBattleView View { get; } = view;

    private IBattleInput Input { get; } = input;

    private IStoryHooks Story { get; } = story;

    /// <summary>The player-unit record selected by <c>Unit_Cur</c>, re-read on every access.</summary>
    private UnitRecord Unit => State.Units[State.Unit_Cur];

    /// <summary>The enemy-commander record selected by <c>Cmdr_Cur</c>, re-read on every access.</summary>
    private CmdrRecord Cmdr => State.Cmdrs[State.Cmdr_Cur];

    /// <summary>The province record selected by <c>Prov_Cur</c>, re-read on every access.</summary>
    private ProvRecord Prov => State.Provs[State.Prov_Cur];

    /// <summary>The item record selected by <c>Item_Cur</c>, re-read on every access.</summary>
    private ItemRecord Item => State.Items[State.Item_Cur];

    /// <summary><c>Y 4, n:</c> — draws 1..n (n of 0 or 1 returns n without drawing) into <c>RetVal</c>.</summary>
    private Sys3Int Rnd(Sys3Int n)
    {
        State.RetVal = random.Next(n);
        return State.RetVal;
    }
}

/// <summary>A location in the decompiled scenario, used to identify a message or scene text.</summary>
/// <param name="File">Scenario file name as decompiled, e.g. <c>FIGT_M.ADV</c>.</param>
/// <param name="Line">1-based line in the raw decompile (<c>_extract\scenario</c>).</param>
public readonly record struct ScriptRef(string File, int Line)
{
    /// <inheritdoc />
    public override string ToString() => $"{File}:{Line}";
}

/// <summary>Something the battle shows; the presentation layer decides how.</summary>
public abstract record BattleEvent;

/// <summary>Receives the battle's presentation events in script order.</summary>
public interface IBattleView
{
    /// <summary>Shows one event; the battle continues when the task completes.</summary>
    Task ShowAsync(BattleEvent battleEvent);
}

/// <summary>Calls from the battle into non-rule scenario files (story and death scenes).</summary>
public interface IStoryHooks
{
    /// <summary>
    /// <c>Ev_Entry := entry; call file</c> — a story scene that may change any state.
    /// </summary>
    Task RunEventAsync(string file, Sys3Int entry);
}
