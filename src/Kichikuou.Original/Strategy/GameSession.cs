using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.Setup;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// One game from the boot to its end (part G0): the rule files routed (<see cref="StrategyRules"/>),
/// the scenes run from the player's files (<see cref="SceneRunner"/>), one context with the save
/// slots, then the boot (<c>SET.ADV</c>) and the title (<c>ｽﾀMENU.ADV</c>, from the player's files,
/// owner's decision 19) — or, for development, the new game or a load at once — and the week loop
/// for ever. A load (<c>LD</c>, from the title or from <c>COMMAND</c>'s Load/Save) leaves
/// whatever runs by <see cref="GameLoadedException"/>; the session catches it and goes on where the
/// slot was saved — the save screen's "Load Complete" (<see cref="LoadSaveScreen.ResumeAsync"/>),
/// then the rest of that week (<see cref="WeekLoop.ResumeAsync"/>: after its autosave, or for a save
/// of the moment, owner's decision 26, in its <c>COMMAND</c>). The game ends at a jump to
/// <c>OVER.ADV</c> (an ending, <c>EndingId</c>): the file runs from the player's files (owner's
/// decision 19) — the ending's picture, the staff roll, the Completion Report recorded on page 4 and
/// kept in the slots — up to its <c>ZZ0</c>, and the session returns the ending. A path not ported
/// yet fails with <see cref="NotSupportedException"/>.
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
        Script = new OriginalScriptContext(state, random, view, input, Scenes) { Saves = saves };
        Scenes.Bind(Script);
        Rules.Bind(Script);
    }

    /// <summary>The shared services the game runs on.</summary>
    public OriginalScriptContext Script { get; }

    /// <summary>The router of the ported rule files.</summary>
    public StrategyRules Rules { get; }

    /// <summary>The scene runtime (the context's story hooks).</summary>
    public SceneRunner Scenes { get; }

    /// <summary>The game state.</summary>
    public GameState State => Script.State;

    /// <summary>The boot: <c>ｽﾀｰﾄ.ADV</c>'s state lines and <c>SET.ADV</c> (which reads page 4 from the slots, <c>ﾛｰﾄﾞｾｰﾌﾞ</c> 10).</summary>
    public Task BootAsync() => new GameSetup(Script, _scenario).BootAsync();

    /// <summary>
    /// The title (<c>ｽﾀｰﾄ.ADV:872</c> <c>&amp;#ｽﾀMENU.ADV:</c>): <c>ｽﾀMENU</c> from the player's files
    /// (owner's decision 19) — the logo, the buttons the clears unlock, Fortune Check (<c>HAPPY_C</c>),
    /// Alice's Mansion, <c>Gｻｲﾄｳ</c>, the requests slip — until a game started from it (New game, a
    /// debug start, Load) ends. The
    /// gallery and the music room (<c>CG</c>, <c>MMODE</c>) are not ported: they go to the unported
    /// files' hooks, and the title goes on.
    /// </summary>
    public Task<GameEnd> TitleAsync() => PlayAsync(() => Script.CallFileAsync(TitleFile));

    /// <summary>The title, run from the player's files.</summary>
    public const string TitleFile = "ｽﾀMENU.ADV";

    /// <summary>
    /// Development: "Start" on the title without it — the new game (<c>ｽﾀMENU.ADV:244-291</c>,
    /// <see cref="GameSetup.NewGameAsync"/>), then the week loop until the game ends.
    /// </summary>
    public async Task<GameEnd> NewGameAsync()
    {
        return await PlayAsync(async () =>
        {
            await new GameSetup(Script, _scenario).NewGameAsync();
            await Script.CallFileAsync("MAIN.ADV");                     // :291 &#MAIN.ADV:
        });
    }

    /// <summary>
    /// Development (<c>--load</c>): "Load" on the title without it (<c>ｽﾀMENU.ADV:238-243</c>): the
    /// save screen's load list (<c>ﾛｰﾄﾞｾｰﾌﾞ</c> with <c>RetVal 0</c>); null when the player backs out
    /// of it, otherwise the loaded game played until it ends.
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

    /// <summary>
    /// Development (<c>--ending</c>): an ending without playing to it — <c>OVER</c> on the booted
    /// state with <paramref name="endingId"/> (the report shows a fresh game's fates), to its <c>ZZ0</c>.
    /// </summary>
    public Task<GameEnd> EndingAsync(Sys3Int endingId) => PlayAsync(() =>
    {
        State.EndingId = endingId;
        return Script.CallFileAsync("OVER.ADV");
    });

    /// <summary>
    /// Runs <paramref name="start"/> and whatever a load or a jump to <c>MAIN</c> or <c>OVER</c>
    /// continues with, until <c>OVER</c> quits.
    /// </summary>
    private async Task<GameEnd> PlayAsync(Func<Task> start)
    {
        var next = start;
        while (true)
        {
            try
            {
                await next();
                throw new InvalidOperationException("The title, the week loop or OVER.ADV returned.");
            }
            catch (GameLoadedException loaded)
            {
                var resume = loaded.Resume;
                next = () => ResumeAsync(resume);
            }
            catch (ScenarioJumpException jump) when (jump.File is "MAIN.ADV" or "OVER.ADV")
            {
                next = () => Script.CallFileAsync(jump.File);
            }
            catch (GameQuitException)
            {
                return new GameEnd(State.EndingId);                     // OVER.ADV:2083 ZZ0, 0
            }
        }
    }

    /// <summary>After <c>LD</c>: "Load Complete", then the week the slot was saved in, from right after its autosave.</summary>
    private async Task ResumeAsync(ScriptRef autosave)
    {
        await new LoadSaveScreen(Script, _scenario).ResumeAsync();
        await new WeekLoop(Script, _scenario).ResumeAsync(autosave);
    }
}

/// <summary>How a game ended: <c>OVER.ADV</c> ran to its <c>ZZ0</c> for the ending <paramref name="EndingId"/>.</summary>
public sealed record GameEnd(Sys3Int EndingId);
