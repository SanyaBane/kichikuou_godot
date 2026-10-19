using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Script;

/// <summary>
/// The original's context: the engine's <see cref="ScriptContext"/> on the original's
/// <see cref="GameState"/>, with the hand ports of the helper files every part calls —
/// <c>WAO_SUB.ADV</c> (<c>OriginalScriptContext.Wao.cs</c>), <c>M.ADV</c> / <c>WAO_MAP.ADV</c>
/// (<c>OriginalScriptContext.Map.cs</c>) and <c>PCMPLAY.ADV</c> (<c>OriginalScriptContext.Sound.cs</c>).
/// One context serves a whole game: the battle, the week loop, the commands and the dungeon all
/// run on it.
/// </summary>
public sealed partial class OriginalScriptContext(
    GameState state,
    IScriptRandom random,
    IScriptView view,
    IScriptInput input,
    IStoryHooks story)
    : ScriptContext(state, random, view, input, story)
{
    /// <summary>The game state the scripts read and change.</summary>
    public new GameState State { get; } = state;

    /// <summary>
    /// Ours (owner's decision 26): the next call of <c>COMMAND.ADV</c> goes on where its Load/Save returns,
    /// after a load of a save made in it (<c>CommandPhase.ResumeAfterSaveAsync</c>); the call takes it back.
    /// </summary>
    public bool ResumeCommand { get; set; }

    /// <summary>
    /// Ours (owner's decision 27): the unit our deployment screen asks to replenish (0: none). The next press
    /// <c>Sﾌﾞﾀｲ</c> reads answers <c>DeploymentScreen.ReplenishCode</c>, which does it and takes the request back.
    /// </summary>
    public Sys3Int ReplenishRequest { get; set; }

    /// <summary>
    /// The helper files run their ports with the arguments the statements left in <c>CallArg</c> /
    /// <c>Ui_WaoOp</c>; any other file is a hook.
    /// </summary>
    protected override Task CallHelperAsync(string file) => file switch
    {
        "M.ADV" => MapCallAsync(State.CallArg),
        "WAO_MAP.ADV" => WaoMapCallAsync(State.CallArg),
        "WAO_SUB.ADV" => WaoAsync(State.Ui_WaoOp),
        "PCMPLAY.ADV" => SoundAsync(State.CallArg),
        _ => base.CallHelperAsync(file),
    };
}
