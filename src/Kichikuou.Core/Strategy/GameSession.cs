using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// One game from the boot to its end (part G0): the rule files routed (<see cref="StrategyRules"/>),
/// the scenes run from the player's files (<see cref="SceneRunner"/>), one context with the save
/// slots, then the boot (<c>SET.ADV</c>) and either the new game or a load from the title, and the
/// week loop for ever. A load (<c>LD</c>, from the title or from <c>COMMAND</c>'s Load/Save) leaves
/// whatever runs by <see cref="GameLoadedException"/>; the session catches it and goes on where the
/// slot was saved — the save screen's "Load Complete" (<see cref="LoadSaveScreen.ResumeAsync"/>),
/// then the rest of that week (<see cref="WeekLoop.ResumeAsync"/>). The game ends at a jump to
/// <c>OVER.ADV</c> (an ending, <c>EndingId</c>), which the session returns; <c>OVER</c> itself is
/// not ported (owner's decision 10). A path not ported yet fails with <see cref="NotSupportedException"/>.
/// </summary>
/// <remarks>
/// The original boots once per launch and quits at every ending (STR-10); a session is one boot
/// on a fresh state, so a UI starts a new session for every game.
/// </remarks>
public sealed class GameSession
{
    private readonly IScenarioSource _scenario;

    /// <summary>
    /// Wires a game on <paramref name="state"/> (a fresh one: the boot runs on it). The input must
    /// answer the battles and the scenes; files the port does not run yet go to
    /// <paramref name="unported"/>.
    /// </summary>
    public GameSession(
        GameState state, IScriptRandom random, IScriptView view, IGameInput input,
        IScenarioSource scenario, ISceneSource scenes, ISaveStore saves, IStoryHooks unported)
    {
        _scenario = scenario;
        Rules = new StrategyRules(scenario, unported);
        Scenes = new SceneRunner(scenes, Rules);
        Script = new ScriptContext(state, random, view, input, Scenes) { Saves = saves };
        Scenes.Bind(Script);
        Rules.Bind(Script);
    }

    /// <summary>The shared services the game runs on.</summary>
    public ScriptContext Script { get; }

    /// <summary>The router of the ported rule files.</summary>
    public StrategyRules Rules { get; }

    /// <summary>The scene runtime (the context's story hooks).</summary>
    public SceneRunner Scenes { get; }

    /// <summary>The game state.</summary>
    public GameState State => Script.State;

    /// <summary>The boot: <c>ｽﾀｰﾄ.ADV</c>'s state lines and <c>SET.ADV</c> (which reads page 4 from the slots, <c>ﾛｰﾄﾞｾｰﾌﾞ</c> 10).</summary>
    public Task BootAsync() => new GameSetup(Script, _scenario).BootAsync();

    /// <summary>"Start" on the title: the new game (<c>ｽﾀMENU.ADV:244-291</c>), then the week loop until the game ends.</summary>
    public async Task<GameEnd> NewGameAsync()
    {
        return await PlayAsync(async () =>
        {
            await new GameSetup(Script, _scenario).NewGameAsync();
            await Script.CallFileAsync("MAIN.ADV");                     // :291 &#MAIN.ADV:
        });
    }

    /// <summary>
    /// "Load" on the title (<c>ｽﾀMENU.ADV:238-243</c>): the save screen's load list
    /// (<c>ﾛｰﾄﾞｾｰﾌﾞ</c> with <c>RetVal 0</c>); null when the player backs out of it, otherwise the
    /// loaded game played until it ends.
    /// </summary>
    public async Task<GameEnd?> LoadFromTitleAsync()
    {
        State.RetVal = 0;
        try
        {
            await Script.CallFileAsync("ﾛｰﾄﾞｾｰﾌﾞ.ADV");
            return null;
        }
        catch (GameLoadedException loaded)
        {
            return await PlayAsync(() => ResumeAsync(loaded.Resume));
        }
    }

    /// <summary>Runs <paramref name="start"/> and whatever a load or a jump to <c>MAIN</c> continues with, until an ending.</summary>
    private async Task<GameEnd> PlayAsync(Func<Task> start)
    {
        var next = start;
        while (true)
        {
            try
            {
                await next();
                throw new InvalidOperationException("The week loop returned.");
            }
            catch (GameLoadedException loaded)
            {
                var resume = loaded.Resume;
                next = () => ResumeAsync(resume);
            }
            catch (ScenarioJumpException jump) when (jump.File == "MAIN.ADV")
            {
                next = () => Script.CallFileAsync("MAIN.ADV");
            }
            catch (ScenarioJumpException jump) when (jump.File == "OVER.ADV")
            {
                return new GameEnd(State.EndingId);
            }
        }
    }

    /// <summary>After <c>LD</c>: "Load Complete", then the week the slot was saved in, from right after its autosave.</summary>
    private async Task ResumeAsync(ScriptRef autosave)
    {
        await new LoadSaveScreen(Script, _scenario).ResumeAsync();
        await new WeekLoop(Script).ResumeAsync(autosave);
    }
}

/// <summary>How a game ended: the jump to <c>OVER.ADV</c> with <paramref name="EndingId"/> set by the caller.</summary>
public sealed record GameEnd(Sys3Int EndingId);
