using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// A small statement model for scenario code that is read from the player's decompile at run
// time instead of being hand-ported: today the straight-line data files (SET_MAP, SET_CHR,
// SET_ITEM); the event converter (part E1) extends it with control flow, messages and menus.
// Statements keep the script's variable numbers and the location they came from.

/// <summary>An expression of the script, evaluated with the engine's 16-bit saturating arithmetic.</summary>
public abstract record ScriptExpr;

/// <summary>A number.</summary>
public sealed record ConstExpr(Sys3Int Value) : ScriptExpr;

/// <summary>Variable <c>VARnnnn</c> as the script reads it (through its <c>DS</c> binding, if any).</summary>
public sealed record VarExpr(int Var) : ScriptExpr;

/// <summary><paramref name="Left"/> <paramref name="Op"/> <paramref name="Right"/> with <paramref name="Op"/> one of <c>+ - * /</c>.</summary>
public sealed record BinaryExpr(char Op, ScriptExpr Left, ScriptExpr Right) : ScriptExpr;

/// <summary>One statement, with where it is in the decompiled scenario.</summary>
public abstract record ScriptStatement(ScriptRef At);

/// <summary><c>!VARnnnn : expr!</c>.</summary>
public sealed record AssignStatement(ScriptRef At, int Var, ScriptExpr Value) : ScriptStatement(At);

/// <summary><c>MS n, "text":</c> — string variable <paramref name="Slot"/> := <paramref name="Text"/> (read from the player's files).</summary>
public sealed record SetStringStatement(ScriptRef At, int Slot, string Text) : ScriptStatement(At);

/// <summary><c>MM dst, src:</c> — copies string variable <paramref name="Source"/> into <paramref name="Target"/>.</summary>
public sealed record CopyStringStatement(ScriptRef At, ScriptExpr Target, ScriptExpr Source) : ScriptStatement(At);

/// <summary>
/// <c>#label, 1:</c> — points the data reader at the word table the label's address word names
/// (xsystem35 <c>cmd_check.c</c>); <paramref name="Words"/> is that table.
/// </summary>
public sealed record SetDataTableStatement(ScriptRef At, IReadOnlyList<Sys3Int> Words) : ScriptStatement(At);

/// <summary><c>F3, VARnnnn, k:</c> — the variable := word <c>k</c> of the current data table (<c>cmdf.c</c>).</summary>
public sealed record ReadDataStatement(ScriptRef At, int Var, ScriptExpr Index) : ScriptStatement(At);

/// <summary><c>%#FILE.ADV:</c> — a page call.</summary>
public sealed record CallFileStatement(ScriptRef At, string File) : ScriptStatement(At);

/// <summary><c>%0:</c> — return from the page.</summary>
public sealed record ReturnStatement(ScriptRef At) : ScriptStatement(At);

/// <summary>A scenario file the port reads could not be read in the expected form.</summary>
public sealed class ScenarioFormatException(string message) : Exception(message);
