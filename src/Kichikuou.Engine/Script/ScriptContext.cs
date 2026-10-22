using System.Diagnostics.CodeAnalysis;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>
/// The engine's side of a running game, which every part that runs the scenario shares: the
/// state, the script's random generator, the presentation and input interfaces, the calls into
/// other files, the pauses, the CG load counter and the music commands; the save commands are in
/// <c>ScriptContext.Save.cs</c>, the statements and strings in <c>ScriptContext.Statements.cs</c>, the
/// record of the read lines and the menus in <c>ScriptContext.ReadLines.cs</c>.
/// A variant's context derives from it with the ports of its helper files, which
/// <see cref="CallHelperAsync"/> reaches (the original's <c>OriginalScriptContext</c>). One context
/// serves a whole game.
/// </summary>
public partial class ScriptContext(
    ScriptState state,
    IScriptRandom random,
    IScriptView view,
    IScriptInput input,
    IStoryHooks story)
{
    /// <summary>The game state the scripts read and change.</summary>
    public ScriptState State { get; } = state;

    /// <summary>
    /// Receives the presentation events in script order: the view given, behind the record of the read
    /// lines, which notes every message it shows (<c>ScriptContext.ReadLines.cs</c>).
    /// </summary>
    public IScriptView View { get; } = new ReadLineView(state, view);

    /// <summary>Answers the engine's input commands.</summary>
    public IScriptInput Input { get; } = input;

    /// <summary>Runs the files that are not ported as rules (story scenes, unported screens).</summary>
    public IStoryHooks Story { get; } = story;

    /// <summary>
    /// <c>Y 4, n:</c> — draws 1..n (n of 0 or 1 returns n without drawing) into <c>RetVal</c>.
    /// The only caller of <see cref="IScriptRandom.Next"/>. A scene gives the draw's place
    /// <paramref name="at"/> (<see cref="SceneRunner"/>; the hand ports give none), which a generator that
    /// wants it receives (<see cref="IPlacedRandom"/>, the dry run's); the draw is the same either way.
    /// </summary>
    public Sys3Int Rnd(Sys3Int n, ScriptRef? at = null)
    {
        var drawn = at is { } place && random is IPlacedRandom placed ? placed.Next(n, place) : random.Next(n);
        State.RetVal = Drawn(n, at, drawn);
        return State.RetVal;
    }

    /// <summary>
    /// The value a draw gives the script (<see cref="Rnd"/>), once the generator has drawn <paramref name="drawn"/>:
    /// that value, unless a variant's rule replaces it (ours, owner's decision 30: "New scenes first" during a talk or
    /// a visit, the original's <c>OriginalScriptContext</c>). The generator has moved exactly as without it, so every
    /// later draw is the same; an override never draws.
    /// </summary>
    protected virtual Sys3Int Drawn(Sys3Int n, ScriptRef? at, Sys3Int drawn) => drawn;

    /// <summary>Shows one presentation event.</summary>
    public Task ShowAsync(ScriptEvent scriptEvent) => View.ShowAsync(scriptEvent);

    /// <summary>
    /// A presentation-only engine command (<see cref="EngineEvent"/>) with its evaluated arguments,
    /// from a scene (<see cref="SceneRunner"/>) or a hand-ported file: <c>G n</c> and <c>G n, sprite</c>
    /// load CG n first (<see cref="CountCgLoad"/>).
    /// </summary>
    public Task EngineAsync(string command, IReadOnlyList<Sys3Int> args)
    {
        if (command == "G")
            CountCgLoad(args[0]);
        return View.ShowAsync(new EngineEvent(command, args));
    }

    /// <summary>
    /// Where the engine's CG load counter points: the page slot of CG 0, null until <c>ZG</c>
    /// binds it (<see cref="BindCgLoadCounter"/>). Engine state, like <see cref="PauseCancelable"/>:
    /// never saved; a loaded slot overwrites the page in place (xsystem35 <c>loadVarPage</c>), so
    /// the binding holds as long as the context.
    /// </summary>
    public (int Page, int Index)? CgLoadCounter { get; private set; }

    /// <summary>
    /// <c>ZG var:</c> (<c>SET.ADV:297</c> <c>ZG Glob_CgSeen:</c>) — the CG load counter starts at
    /// where variable <paramref name="variable"/> points now (xsystem35 <c>cmdz.c:480-485</c> keeps
    /// the address <c>getCaliVariable</c> resolves, through the variable's binding at its cursor).
    /// </summary>
    public void BindCgLoadCounter(int variable) => CgLoadCounter = State.Locate(variable);

    /// <summary>
    /// The engine's CG load counter (owner's decision 21): loading CG <paramref name="cg"/> adds 1
    /// to the n-th slot after the bound one (xsystem35 <c>cg.c:276-277</c>,
    /// <c>(*(cg_loadCountVar + no + 1))++</c> with <c>no = n - 1</c>, <c>cmdg.c:38, 53</c>) — record
    /// n of <c>Glob_CgSeen</c>, which the gallery reads. Called wherever the script loads a CG:
    /// <c>G</c> (<see cref="EngineAsync"/>) and the helper files and the battle that draw through
    /// their own events. <c>G 0</c> loads nothing (<c>cmdg.c:31-35</c>); a slot past the page is not
    /// written (the original writes past its buffer; no CG of the game is numbered so high).
    /// </summary>
    public void CountCgLoad(Sys3Int cg)
    {
        if (CgLoadCounter is not { } at || cg == 0)
            return;
        int slot = at.Index + cg.Value;
        // QUIRK: STR-43 — every load counts here, a cache hit too; the 16-bit count wraps.
        var count = State.Memory.ReadPage(at.Page, slot);
        State.Memory.WritePage(at.Page, slot, unchecked((ushort)(count.Value + 1)));
    }

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

    /// <summary>
    /// <c>Ev_Entry := entry; call file</c> — a story scene through the hooks (the scene number goes
    /// into the variant's <see cref="VariantProfile.EventEntryVariable"/>).
    /// </summary>
    public Task EventAsync(string file, Sys3Int entry)
    {
        EventEntry = entry;
        return Story.RunEventAsync(file, entry);
    }

    /// <summary>
    /// <c>%#file</c> without an entry number (a sub-file that takes its arguments in other
    /// variables, e.g. <c>Sﾌﾞﾀｲ.ADV</c>, <c>Gﾀﾐ.ADV</c>): routed through the story hook with the
    /// current <c>Ev_Entry</c>, so the hook's <c>Ev_Entry := entry</c> changes nothing.
    /// </summary>
    public Task CallFileAsync(string file) => Story.RunEventAsync(file, EventEntry);

    /// <summary>The scene number a page call passes (the original's <c>Ev_Entry</c>).</summary>
    private ref Sys3Int EventEntry => ref State.Memory.Var(State.Profile.EventEntryVariable);

    /// <summary><c>SS n</c>: plays music track n (0 stops the music).</summary>
    public Task MusicAsync(Sys3Int track) => View.ShowAsync(new MusicEvent(track));

    /// <summary><c>SR 0, var</c>: the track that is playing (the caller stores it).</summary>
    public Task<Sys3Int> GetPlayingTrackAsync() => Input.GetPlayingTrackAsync();

    /// <summary>
    /// <c>&amp;#file:</c> — the script jumps to another file and never returns; the running code
    /// is abandoned by throwing <see cref="ScenarioJumpException"/>.
    /// </summary>
    [DoesNotReturn]
    public static void JumpTo(string file) => throw new ScenarioJumpException(file);
}
