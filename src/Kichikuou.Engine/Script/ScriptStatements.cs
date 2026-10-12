using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

// The statement model for scenario code that is read from the player's decompile at run time
// instead of being hand-ported: the straight-line data files (SET_MAP, SET_CHR, SET_ITEM) and the
// story scenes (G*, I*, read as a ScriptProgram). It is a goto program that mirrors the decompile
// line for line — every engine command the scenes use is one statement, jumps are resolved to
// statement indices — so running it keeps the original's side effects (cursors, scratch
// variables, RetVal, the WAO_SUB stub calls) without any idiom being rewritten. Statements keep
// the script's variable numbers and the location they came from. Semantics cited from xsystem35
// (cmd_check.c and the cmd*.c file of each command).

/// <summary>An expression of the script, evaluated with the engine's 16-bit saturating arithmetic.</summary>
public abstract record ScriptExpr;

/// <summary>A number.</summary>
public sealed record ConstExpr(Sys3Int Value) : ScriptExpr;

/// <summary>Variable <c>VARnnnn</c> as the script reads it (through its <c>DS</c> binding, if any).</summary>
public sealed record VarExpr(int Var) : ScriptExpr;

/// <summary>
/// <paramref name="Left"/> <paramref name="Op"/> <paramref name="Right"/>. Operators as the
/// decompiler prints them, by precedence (xsys35dc <c>print_cali</c>, all left-associative):
/// <c>* / %</c>, then <c>+ -</c>, then <c>&amp; | ^</c>, then <c>&lt; &gt; &lt;= &gt;=</c>, then
/// <c>=</c> and <c>\</c> (not equal). Comparisons yield 1 or 0.
/// </summary>
public sealed record BinaryExpr(string Op, ScriptExpr Left, ScriptExpr Right) : ScriptExpr;

/// <summary>One statement, with where it is in the decompiled scenario.</summary>
public abstract record ScriptStatement(ScriptRef At);

// --- Data and strings ---------------------------------------------------------------------------

/// <summary><c>!VARnnnn : expr!</c>.</summary>
public sealed record AssignStatement(ScriptRef At, int Var, ScriptExpr Value) : ScriptStatement(At);

/// <summary><c>MS n, "text":</c> — string variable <paramref name="Slot"/> := <paramref name="Text"/> (read from the player's files).</summary>
public sealed record SetStringStatement(ScriptRef At, int Slot, string Text) : ScriptStatement(At);

/// <summary><c>MM dst, src:</c> — copies string variable <paramref name="Source"/> into <paramref name="Target"/>.</summary>
public sealed record CopyStringStatement(ScriptRef At, ScriptExpr Target, ScriptExpr Source) : ScriptStatement(At);

/// <summary><c>MA dst, src:</c> — appends string variable <paramref name="Source"/> to <paramref name="Target"/> (<c>cmdm.c</c>).</summary>
public sealed record AppendStringStatement(ScriptRef At, ScriptExpr Target, ScriptExpr Source) : ScriptStatement(At);

/// <summary>
/// <c>MH slot, width, value:</c> — string variable <paramref name="Slot"/> := <paramref name="Value"/>
/// in full-width digits, formatted like <c>H</c> (<c>cmdm.c</c>).
/// </summary>
public sealed record NumberToStringStatement(ScriptRef At, ScriptExpr Slot, ScriptExpr Width, ScriptExpr Value) : ScriptStatement(At);

/// <summary>
/// <c>MC a, b:</c> — <c>RetVal</c> := 1 when string variables <paramref name="Left"/> and
/// <paramref name="Right"/> hold the same text, else 0 (<c>cmdm.c</c> <c>commandMC</c>, a <c>strcmp</c>;
/// the title's requests slip finds its separators so, <c>ｽﾀMENU.ADV:212, 222</c>).
/// </summary>
public sealed record CompareStringsStatement(ScriptRef At, ScriptExpr Left, ScriptExpr Right) : ScriptStatement(At);

/// <summary>
/// <c>#label, n:</c> — points the data reader at the word table the label's n-th address word
/// names (xsystem35 <c>cmd_check.c</c>); <paramref name="Words"/> is that table.
/// </summary>
public sealed record SetDataTableStatement(ScriptRef At, IReadOnlyList<Sys3Int> Words) : ScriptStatement(At);

/// <summary><c>F3, VARnnnn, k:</c> — the variable := word <c>k</c> of the current data table (<c>cmdf.c</c>).</summary>
public sealed record ReadDataStatement(ScriptRef At, int Var, ScriptExpr Index) : ScriptStatement(At);

/// <summary>
/// <c>#label, n:</c> when the label's n-th address word names a run of strings (<c>"text"</c> lines,
/// each null-terminated in the bytecode, e.g. <c>Sﾊｰﾚﾑ.ADV:311-363</c>): the data reader points at
/// the first one; <paramref name="Strings"/> is the run (read from the player's files).
/// </summary>
public sealed record SetStringTableStatement(ScriptRef At, IReadOnlyList<string> Strings) : ScriptStatement(At);

/// <summary>
/// <c>F1, slot, skip:</c> — skips <paramref name="Skip"/> strings from the data reader's position,
/// copies the next one into string variable <paramref name="Slot"/> and leaves the reader after it
/// (<c>cmdf.c</c> <c>commandF1</c>).
/// </summary>
public sealed record ReadStringStatement(ScriptRef At, ScriptExpr Slot, ScriptExpr Skip) : ScriptStatement(At);

/// <summary>
/// A line of data inside a file's code area — an address word <c>_label:</c> or a string
/// <c>"text"</c> of a table that <c>#label</c> points at. Running into it is an error.
/// </summary>
public sealed record DataStatement(ScriptRef At) : ScriptStatement(At);

/// <summary><c>NC var, count:</c> — <paramref name="Count"/> variables from <paramref name="Var"/> on := 0 (<c>cmdn.c</c>).</summary>
public sealed record ClearVariablesStatement(ScriptRef At, int Var, ScriptExpr Count) : ScriptStatement(At);

/// <summary><c>NB dst, src, count:</c> — copies <paramref name="Count"/> variables from <paramref name="Source"/> on to <paramref name="Target"/> on (<c>cmdn.c</c>).</summary>
public sealed record CopyVariablesStatement(ScriptRef At, int Target, int Source, ScriptExpr Count) : ScriptStatement(At);

/// <summary>
/// <c>US var, count:</c> (<paramref name="Push"/>) / <c>UG var, count:</c> — saves <paramref name="Count"/>
/// variables from <paramref name="Var"/> on on the call stack, or restores them (<c>cmdu.c</c>,
/// <c>sl_pushVar</c>/<c>sl_popVar</c>; a return pops them too).
/// </summary>
public sealed record LocalVariablesStatement(ScriptRef At, bool Push, int Var, ScriptExpr Count) : ScriptStatement(At);

/// <summary><c>Y 4, n:</c> — <c>RetVal</c> := a draw 1..n (n of 0 or 1 returns n without drawing).</summary>
public sealed record RandomStatement(ScriptRef At, ScriptExpr Max) : ScriptStatement(At);

// --- Control flow -------------------------------------------------------------------------------

/// <summary><c>@label:</c> — continues at statement <paramref name="Target"/>.</summary>
public sealed record GotoStatement(ScriptRef At, string Label, int Target) : ScriptStatement(At);

/// <summary>
/// <c>{cond:</c> … <c>}</c> — when <paramref name="Condition"/> is 0 the script continues at
/// <paramref name="EndTarget"/>, the statement after the matching <c>}</c>; otherwise with the next
/// statement. The <c>}</c> itself is no command, so labels inside the block are ordinary targets.
/// </summary>
public sealed record IfStatement(ScriptRef At, ScriptExpr Condition, int EndTarget) : ScriptStatement(At);

/// <summary><c>\label:</c> — a call of a label of the same file (<c>sl_callNear</c>).</summary>
public sealed record CallLabelStatement(ScriptRef At, string Label, int Target) : ScriptStatement(At);

/// <summary><c>\0:</c> — returns from the innermost label call (<c>sl_retNear</c>).</summary>
public sealed record ReturnLabelStatement(ScriptRef At) : ScriptStatement(At);

/// <summary><c>%#FILE.ADV:</c> — a page call.</summary>
public sealed record CallFileStatement(ScriptRef At, string File) : ScriptStatement(At);

/// <summary>
/// <c>%0:</c> — return from the page. Label calls still pending in the page are dropped
/// (<c>sl_retFar</c> unwinds them).
/// </summary>
public sealed record ReturnStatement(ScriptRef At) : ScriptStatement(At);

/// <summary><c>&amp;#FILE.ADV:</c> — a page jump: the file runs from its start and nothing returns here.</summary>
public sealed record JumpFileStatement(ScriptRef At, string File) : ScriptStatement(At);

// --- Messages and menus -------------------------------------------------------------------------

/// <summary>
/// A text run of the script: the consecutive text commands starting at <paramref name="Start"/>
/// (the presentation layer reads them from the player's files, <c>ScenarioText</c>), with the
/// values only the script can compute: the expressions printed by <c>H</c>/<c>HH</c>
/// (<paramref name="Numbers"/>) and the string variable of every <c>X</c>/<c>MP</c>
/// (<paramref name="Strings"/>, constants included), each in order.
/// </summary>
public sealed record ScriptText(ScriptRef Start, IReadOnlyList<ScriptExpr> Numbers, IReadOnlyList<ScriptExpr> Strings);

/// <summary>A text run printed into the message window.</summary>
public sealed record MessageStatement(ScriptRef At, ScriptText Text) : ScriptStatement(At);

/// <summary>
/// <c>R</c> on its own (no text follows before the next command): a line break in the message
/// window, e.g. between two menu items.
/// </summary>
public sealed record LineBreakStatement(ScriptRef At) : ScriptStatement(At);

/// <summary>
/// <c>$label$</c> — starts registering a menu item that continues at <paramref name="Target"/>
/// (<c>sel_addRetValue</c>). Until the matching <see cref="MenuItemEndStatement"/> every printed
/// text goes into the item instead of the message window (<c>sys_addMsg</c>), also from label
/// calls (<c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:129-131</c>); a line break (<c>R</c>) still goes to the window.
/// </summary>
public sealed record MenuItemStatement(ScriptRef At, string Label, int Target) : ScriptStatement(At);

/// <summary><c>$</c> — ends the item being registered (<c>sel_fixElement</c>).</summary>
public sealed record MenuItemEndStatement(ScriptRef At) : ScriptStatement(At);

/// <summary>
/// <c>]</c> — shows the registered items and forgets them (<c>sel_select</c>): choosing one continues
/// at its label, cancelling (right click) with the next statement; either way the message window
/// is cleared afterwards.
/// </summary>
public sealed record MenuStatement(ScriptRef At) : ScriptStatement(At);

/// <summary>
/// <c>Y 1, mode:</c> — mode 0 clears the message area, 1 only moves the text position back to the
/// top left (<c>cmdy.c</c>).
/// </summary>
public sealed record MessageResetStatement(ScriptRef At, ScriptExpr Mode) : ScriptStatement(At);

// --- Input --------------------------------------------------------------------------------------

/// <summary><c>A</c> — waits for a key or click, then clears the message window (<c>sys_hit_any_key</c>, <c>msg_nextPage</c>).</summary>
public sealed record HitAnyKeyStatement(ScriptRef At) : ScriptStatement(At);

/// <summary><c>IK 0:</c> — waits for a key or click; <c>RetVal</c> := its code (0 while skipping).</summary>
public sealed record KeyWaitStatement(ScriptRef At) : ScriptStatement(At);

/// <summary>
/// <c>IM x, y:</c> — the mouse position into the two variables, the button state into
/// <c>RetVal</c>; the scenes use it to wait for a button release before a menu.
/// </summary>
public sealed record MouseStatement(ScriptRef At, int XVar, int YVar) : ScriptStatement(At);

/// <summary>
/// <c>IC shape, var:</c> — sets the mouse cursor's shape and stores the shape set before in the
/// variable (an engine static, <see cref="ISceneInput.SetCursorAsync"/>).
/// </summary>
public sealed record CursorStatement(ScriptRef At, ScriptExpr Shape, int Var) : ScriptStatement(At);

/// <summary><c>Y 3, n:</c> — a pause of n ticks that a key cuts short; <c>RetVal</c> := that key (0 when it ran out).</summary>
public sealed record PauseStatement(ScriptRef At, ScriptExpr Ticks) : ScriptStatement(At);

// --- Sound, timers, save data, system -----------------------------------------------------------

/// <summary><c>SS n:</c> — plays music track n (0 stops it).</summary>
public sealed record MusicStatement(ScriptRef At, ScriptExpr Track) : ScriptStatement(At);

/// <summary>
/// <c>SR device, var:</c> — the variable := the track playing on device 0 (CD; 0 when stopped) or
/// 1 (MIDI, <c>OVER.ADV:160</c> polls it until the ending music stops) (<c>cmds.c</c>).
/// </summary>
public sealed record MusicInfoStatement(ScriptRef At, int Device, int Var) : ScriptStatement(At);

/// <summary><c>SC var:</c> — the CD play position into four variables from <paramref name="Var"/> on (999 each when none).</summary>
public sealed record CdPositionStatement(ScriptRef At, int Var) : ScriptStatement(At);

/// <summary><c>ZT0 var:</c> — the current date and time into seven variables from <paramref name="Var"/> on (<c>cmdz.c</c>).</summary>
public sealed record DateStatement(ScriptRef At, int Var) : ScriptStatement(At);

/// <summary><c>ZT1 n:</c> — restarts the timer at <paramref name="Value"/>.</summary>
public sealed record TimerResetStatement(ScriptRef At, ScriptExpr Value) : ScriptStatement(At);

/// <summary>
/// <c>ZTn var:</c> with n = 2..5 — the timer into the variable, in 1/10 s (2), 1/30 s (3),
/// 1/60 s (4) or 1/100 s (5).
/// </summary>
public sealed record TimerStatement(ScriptRef At, int Mode, int Var) : ScriptStatement(At);

/// <summary>
/// The save-data commands (<c>cmdl.c</c>, <c>cmdq.c</c>): <c>LD slot</c> loads everything,
/// <c>QD slot</c> saves everything, <c>LP slot, var, count</c> / <c>QP slot, var, count</c>
/// load / save <paramref name="Count"/> variables from <paramref name="Var"/> on, <c>QC to, from</c>
/// copies a slot (<paramref name="Slot"/> = to, <paramref name="Other"/> = from), <c>LT slot, var</c>
/// reads a slot's time stamp into six variables from <paramref name="Var"/> on. Every one sets
/// <c>RetVal</c> to its result code (<c>LT</c> too, <c>cmdl.c:83-104</c>); they run on
/// <see cref="ScriptContext.Saves"/> (<c>ScriptContext.Save.cs</c>).
/// </summary>
public sealed record SaveDataStatement(
    ScriptRef At, string Command, ScriptExpr Slot, int? Var = null, ScriptExpr? Count = null, ScriptExpr? Other = null)
    : ScriptStatement(At);

/// <summary><c>ZZ0 mode:</c> — quits the engine (mode 0) or waits for the window to close (1).</summary>
public sealed record QuitStatement(ScriptRef At, ScriptExpr Mode) : ScriptStatement(At);

/// <summary>
/// <c>ZZ3 var:</c> — the display's width, height and bits per pixel into three variables from
/// <paramref name="Var"/> on (<c>cmdz.c</c> <c>commandZZ3</c>, <see cref="ISceneInput.GetDisplayAsync"/>).
/// </summary>
public sealed record DisplayInfoStatement(ScriptRef At, int Var) : ScriptStatement(At);

/// <summary>
/// <c>CE sx, sy, w, h, dx, dy, effect, option, wait:</c> — copies an area like <c>CC</c> with a
/// transition effect that takes its time (<c>cmdc.c</c> <c>commandCE</c>, <c>ecopy.c</c>
/// <c>ags_eCopyArea</c>): with <c>wait</c> set a key or click cuts it short, and with
/// <c>wait</c> 1 <c>RetVal</c> := that key (0 when it ran out). The presentation draws the copy
/// (an <see cref="EngineEvent"/> <c>CE</c>); the time is an input. Only the logo uses it
/// (<c>ﾌﾟﾛﾛｰｸﾞ.ADV:158</c>, effect 4).
/// </summary>
public sealed record EffectCopyStatement(ScriptRef At, IReadOnlyList<ScriptExpr> Args) : ScriptStatement(At);

/// <summary>
/// An engine command that changes only what is shown and never a script variable but through the
/// engine's CG load counter (<c>G</c> counts its CG, <see cref="ScriptContext.CountCgLoad"/>): CG
/// loading and placement (<c>G</c>, <c>J0</c>, <c>J1</c>, <c>PC</c>), fades and palette (<c>PF0</c>, <c>PF1</c>,
/// <c>PW0</c>, <c>PW1</c>), screen copies and fills (<c>EC</c>, <c>CB</c>, <c>CC</c>, <c>CF</c>,
/// <c>CL</c>, <c>CM</c>, <c>CS</c>), window and text layout (<c>B1</c>-<c>B4</c>, <c>T</c>, <c>WV</c>,
/// <c>ZC</c>, <c>ZM</c>), the next track's loop count (<c>SL</c>), the pointer warp (<c>IZ x, y</c>,
/// whose effect shows in the next <c>IM</c>), the world's size (<c>WW</c>). The presentation interprets them;
/// <paramref name="Args"/> are evaluated when the statement runs.
/// </summary>
public sealed record EngineStatement(ScriptRef At, string Command, IReadOnlyList<ScriptExpr> Args) : ScriptStatement(At);

/// <summary>A scenario file the port reads could not be read in the expected form.</summary>
public sealed class ScenarioFormatException(string message) : Exception(message);
