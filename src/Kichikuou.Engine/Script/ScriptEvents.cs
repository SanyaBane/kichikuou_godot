using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

// Presentation events of the engine commands every part uses (the original's helper files raise
// theirs, HelperEvents.cs). Messages use MessageEvent with the line of the first output command
// of a text run (a quoted string, an R when the run starts with a line break, or X / MP / H when
// it starts with a variable); the
// presentation layer reads string variables (X n, MP n) from the state's strings at the time of
// the event. A run that prints a number (H 0, var) uses ValueMessageEvent, because the
// variable may change before it is drawn.

/// <summary>A message from the script, identified by where its text is in the scenario.</summary>
/// <param name="Text">Where the text run starts.</param>
/// <param name="StringVariables">
/// The string variables printed by the run's <c>X expr:</c> commands whose number the script
/// computes (in order); the presentation cannot evaluate script expressions.
/// </param>
public sealed record MessageEvent(ScriptRef Text, IReadOnlyList<Sys3Int>? StringVariables = null) : ScriptEvent;

/// <summary>
/// A message that prints numbers; <paramref name="Values"/> are the printed values in order
/// (<paramref name="StringVariables"/> as for <see cref="MessageEvent"/>).
/// </summary>
public sealed record ValueMessageEvent(ScriptRef Text, IReadOnlyList<Sys3Int> Values, IReadOnlyList<Sys3Int>? StringVariables = null) : ScriptEvent;

/// <summary>
/// A text of the port's own where an owner's decision changed what the script does (none in the scenario
/// says it), printed as a message run; no line breaks in it. <paramref name="At"/>, when given, is the place in a scene
/// it stands at (no line: it is never noted as read), so a presenter treats it as that file's text (the message log
/// keeps a scene's narration).
/// </summary>
public sealed record PortTextEvent(string Text, ScriptRef? At = null) : ScriptEvent;

/// <summary><c>R</c> outside a text run (e.g. between menu items): a line break in the message window.</summary>
public sealed record LineBreakEvent : ScriptEvent;

/// <summary>
/// The message window is cleared and its text position reset (<c>msg_nextPage(true)</c>): after
/// <c>A</c> and after every menu choice.
/// </summary>
public sealed record MessageClearEvent : ScriptEvent;

/// <summary><c>Y 1, mode:</c> — mode 0 clears the message area, 1 only moves the text position to the top left.</summary>
public sealed record MessageResetEvent(Sys3Int Mode) : ScriptEvent;

/// <summary>
/// A presentation-only engine command (<c>EngineStatement</c>: <c>G</c>, <c>PF0</c>, <c>CC</c>, <c>ZM</c>,
/// …) with its evaluated arguments; the presentation interprets it.
/// </summary>
public sealed record EngineEvent(string Command, IReadOnlyList<Sys3Int> Args) : ScriptEvent;

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

/// <summary><c>SQ a, b, 1</c> — plays two sound effects (e.g. <c>FIGTATTK.ADV:344</c>, <c>:350</c>).</summary>
public sealed record SoundPairEvent(Sys3Int First, Sys3Int Second) : ScriptEvent;

/// <summary><c>SS n</c>: plays music track n (0 stops the music).</summary>
public sealed record MusicEvent(Sys3Int Track) : ScriptEvent;

/// <summary><c>EC 0</c>: clears the dungeon screen (the dungeon counterpart of the original's <c>MapEvent</c>).</summary>
public sealed record ClearScreenEvent : ScriptEvent;
