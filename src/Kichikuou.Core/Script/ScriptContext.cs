using System.Diagnostics.CodeAnalysis;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

/// <summary>
/// What every ported scenario file shares: the game state, the script's random generator, the
/// presentation and input interfaces and the calls into non-rule files, plus the helper files
/// every part calls — <c>WAO_SUB.ADV</c> (<c>ScriptContext.Wao.cs</c>), <c>M.ADV</c> /
/// <c>WAO_MAP.ADV</c> (<c>ScriptContext.Map.cs</c>) and <c>PCMPLAY.ADV</c> with the music
/// commands (<c>ScriptContext.Sound.cs</c>). One context serves a whole game: the battle, the
/// week loop, the commands and the dungeon all run on it.
/// </summary>
public sealed partial class ScriptContext(
    GameState state,
    IScriptRandom random,
    IScriptView view,
    IScriptInput input,
    IStoryHooks story)
{
    /// <summary>The game state the scripts read and change.</summary>
    public GameState State { get; } = state;

    /// <summary>Receives the presentation events in script order.</summary>
    public IScriptView View { get; } = view;

    /// <summary>Answers the engine's input commands.</summary>
    public IScriptInput Input { get; } = input;

    /// <summary>Runs the files that are not ported as rules (story scenes, unported screens).</summary>
    public IStoryHooks Story { get; } = story;

    /// <summary>
    /// <c>Y 4, n:</c> — draws 1..n (n of 0 or 1 returns n without drawing) into <c>RetVal</c>.
    /// The only caller of <see cref="IScriptRandom.Next"/>.
    /// </summary>
    public Sys3Int Rnd(Sys3Int n)
    {
        State.RetVal = random.Next(n);
        return State.RetVal;
    }

    /// <summary>Shows one presentation event.</summary>
    public Task ShowAsync(ScriptEvent scriptEvent) => View.ShowAsync(scriptEvent);

    /// <summary>A text run of <paramref name="file"/> starting at <paramref name="line"/>.</summary>
    public Task TextAsync(string file, int line) => View.ShowAsync(new MessageEvent(new ScriptRef(file, line)));

    /// <summary>
    /// Whether a key or click ends a <c>Y 3, n</c> pause early: the engine's <c>Y3waitFlags</c>
    /// (xsystem35 <c>cmdy.c</c>), set by <c>Y 3, 10000</c> (off) and <c>Y 3, 10001</c> (on).
    /// Engine state, not script memory: it starts on and is never saved, so it lives as long as
    /// the context (one game session, like the engine's process).
    /// </summary>
    public bool PauseCancelable { get; private set; } = true;

    /// <summary>
    /// <c>Y 3, n</c>: a timed pause. The engine stores the key that cut it short (0 if none) in
    /// <c>RetVal</c> (xsystem35 <c>cmdy.c</c>), so the pause is an input. <c>Y 3, 10000</c> and
    /// <c>Y 3, 10001</c> are not pauses: they only set <see cref="PauseCancelable"/>, raise no
    /// event and leave <c>RetVal</c> alone (the golden harness records no wait for them either).
    /// </summary>
    public async Task PauseAsync(Sys3Int ticks)
    {
        if (ticks.Value is 10000 or 10001)
        {
            PauseCancelable = ticks.Value == 10001;
            return;
        }
        await View.ShowAsync(new PauseEvent(ticks));
        var key = await Input.PauseAsync(ticks, PauseCancelable);
        State.RetVal = key;
    }

    /// <summary><c>Ev_Entry := entry; call file</c> — a story scene through the hooks.</summary>
    public Task EventAsync(string file, Sys3Int entry)
    {
        State.Ev_Entry = entry;
        return Story.RunEventAsync(file, entry);
    }

    /// <summary>
    /// <c>%#file</c> without an entry number (a sub-file that takes its arguments in other
    /// variables, e.g. <c>Sﾌﾞﾀｲ.ADV</c>, <c>Gﾀﾐ.ADV</c>): routed through the story hook with the
    /// current <c>Ev_Entry</c>, so the hook's <c>Ev_Entry := entry</c> changes nothing.
    /// </summary>
    public Task CallFileAsync(string file) => Story.RunEventAsync(file, State.Ev_Entry);

    /// <summary>
    /// <c>&amp;#file:</c> — the script jumps to another file and never returns; the running code
    /// is abandoned by throwing <see cref="ScenarioJumpException"/>.
    /// </summary>
    [DoesNotReturn]
    public static void JumpTo(string file) => throw new ScenarioJumpException(file);
}
