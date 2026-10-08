using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// Presentation events of the engine commands and helper files every part uses. Messages use
// MessageEvent with the line of the first output command of a text run (a quoted string, an R
// when the run starts with a line break, or X / MP / H when it starts with a variable); the
// presentation layer reads string variables (X n, MP n) from GameState.Strings at the time of
// the event. A run that prints a number (H 0, var) uses ValueMessageEvent, because the
// variable may change before it is drawn.

/// <summary>A message from the script, identified by where its text is in the scenario.</summary>
/// <param name="Text">Where the text run starts.</param>
/// <param name="StringVariables">
/// The string variables printed by the run's <c>X expr:</c> commands whose number the script
/// computes (in order); the presentation cannot evaluate script expressions.
/// </param>
public sealed record MessageEvent(ScriptRef Text, IReadOnlyList<Sys3Int>? StringVariables = null) : ScriptEvent;

/// <summary>A message that prints numbers; <paramref name="Values"/> are the printed values in order.</summary>
public sealed record ValueMessageEvent(ScriptRef Text, IReadOnlyList<Sys3Int> Values) : ScriptEvent;

/// <summary><c>MP slot, width</c> — prints string variable <paramref name="Slot"/> into the message window.</summary>
public sealed record PrintStringEvent(Sys3Int Slot) : ScriptEvent;

/// <summary>
/// <c>Y 3, n:</c> — a timed pause of n ticks starts; the pause itself (and the key that ends it)
/// is <see cref="IScriptInput.PauseAsync"/>.
/// </summary>
public sealed record PauseEvent(Sys3Int Ticks) : ScriptEvent;

/// <summary>
/// A bare <c>IK 0</c> outside <c>WAO_SUB.ADV</c> (e.g. <c>FIGTATTK.ADV:247</c>): the screen is
/// complete and the script is about to wait for a key (<see cref="IScriptInput.WaitForKeyAsync"/>,
/// result in <c>RetVal</c>).
/// </summary>
public sealed record KeyWaitEvent : ScriptEvent;

/// <summary>
/// A call of <c>WAO_SUB.ADV</c> (<c>Ui_WaoOp</c> = <paramref name="Op"/>) that opens, closes or
/// decorates a message window: e.g. 111-153 open a window, 143 the centre window, 141/151 the
/// speaker windows, 311/321 a portrait with the CG number in <paramref name="CallArg"/>, 331/333
/// the centre picture, 1/5/11 a key wait (the wait itself is the
/// <see cref="IScriptInput.WaitForKeyAsync"/> call that follows while a window is open), 0/10
/// close. Raised before the operation's state effects (<c>ScriptContext.Wao.cs</c>).
/// </summary>
public sealed record WindowEvent(Sys3Int Op, Sys3Int CallArg) : ScriptEvent;

/// <summary><c>PCMPLAY.ADV</c>: a sound effect or jingle (<c>CallArg</c> = mode, <c>RetVal</c> = sound number).</summary>
public sealed record SoundEvent(Sys3Int Mode, Sys3Int Sound) : ScriptEvent;

/// <summary><c>SQ a, b, 1</c> — plays two sound effects (e.g. <c>FIGTATTK.ADV:344</c>, <c>:350</c>).</summary>
public sealed record SoundPairEvent(Sys3Int First, Sys3Int Second) : ScriptEvent;

/// <summary><c>SS n</c>: plays music track n (0 stops the music).</summary>
public sealed record MusicEvent(Sys3Int Track) : ScriptEvent;

/// <summary>
/// A call of <c>M.ADV</c> (strategic map) with <c>CallArg</c> = <paramref name="Mode"/>
/// (0 redraw, 1 scroll to the target, 3/4 province markers, 10/13 blink the target, 11/12
/// pause). Raised before the call's state effects (camera, scratch variables, markers), which
/// <c>ScriptContext.Map.cs</c> ports; its pauses follow as <see cref="IScriptInput.PauseAsync"/> calls.
/// </summary>
public sealed record MapEvent(Sys3Int Mode) : ScriptEvent;

/// <summary>
/// A direct call of <c>WAO_MAP.ADV</c> with <c>CallArg</c> = <paramref name="Mode"/> (3 redraws the
/// map tiles and every province marker). Raised before the call's state effects.
/// </summary>
public sealed record WaoMapEvent(Sys3Int Mode) : ScriptEvent;

/// <summary><c>EC 0</c>: clears the dungeon screen (the dungeon counterpart of <see cref="MapEvent"/>).</summary>
public sealed record ClearScreenEvent : ScriptEvent;
