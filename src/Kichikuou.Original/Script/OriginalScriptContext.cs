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
    /// Ours (owner's decision 30, the third rule change on purpose; docs/ui/talk-marks.md §5): "New scenes first", the
    /// player's option (Option → Tweaks, on by default in the game; copied here by the UI). Off by default on a context, so
    /// a context built by hand (the differential tests) runs the files as the original. On, it is one switch for all of
    /// it: during a talk or a visit an unread harmless variant comes, and a listed draw that changes the state gives that
    /// change once its usual scene is read (<see cref="SceneChoices"/>, with <see cref="Marks"/>); Kanami's choice in
    /// Code's event 4 and Kentarou's hint lines follow it too (<see cref="Strategy.ScenePatches"/>, the game's runner's hooks).
    /// </summary>
    public bool NewScenesFirst { get; set; }

    /// <summary>
    /// Ours (owner's decision 32, the fourth rule change on purpose; docs/tweaks.md): BRS-24's fix, the player's option
    /// (Option → Tweaks, "Chaos and Nikkou reach demons", off by default; copied here by the UI). The original's rule on a
    /// context built by hand (the differential tests).
    /// </summary>
    public Combat.DemonReach DemonReach { get; set; }

    /// <summary>
    /// Ours (owner's decision 33, the fifth rule change on purpose; docs/tweaks.md): the ninja follow-up's chance, the
    /// player's option (Option → Tweaks, "Ninja follow-up", the original's 1 in 3 by default; copied here by the UI). The
    /// original's on a context built by hand (the differential tests).
    /// </summary>
    public Combat.NinjaChance NinjaChance { get; set; }

    /// <summary>
    /// Ours (owner's decision 30): the marks over the game's scenes, whose dry run "New scenes first" runs at a draw of a
    /// talk or a visit (<c>GameSession.Marks</c>); without them the rule does not act.
    /// </summary>
    public Strategy.SceneMarks? Marks { get; set; }

    /// <summary>Ours (owner's decision 30): "New scenes first" during the real talks and visits (<see cref="NewScenesFirst"/>).</summary>
    public Strategy.SceneChoices SceneChoices => _sceneChoices ??= new Strategy.SceneChoices(this);

    private Strategy.SceneChoices? _sceneChoices;

    /// <inheritdoc />
    /// <remarks>Ours (owner's decision 30): during a talk or a visit "New scenes first" may replace the value (<see cref="SceneChoices"/>).</remarks>
    protected override Sys3Int Drawn(Sys3Int n, ScriptRef? at, Sys3Int drawn) =>
        _sceneChoices is { } choices ? choices.Draw(n, at, drawn) : drawn;

    /// <inheritdoc />
    /// <remarks>Ours (owner's decision 30): noted for "New scenes first" during a talk or a visit (<see cref="SceneChoices"/>).</remarks>
    protected override void Selected(IReadOnlyList<SceneMenuItem> items, int choice) => _sceneChoices?.Selected(items, choice);

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
