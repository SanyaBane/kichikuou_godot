using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// Presentation events of the original's helper files (OriginalScriptContext): what a call of
// WAO_SUB, PCMPLAY, M or WAO_MAP shows, raised before the call's state effects.

/// <summary>
/// A call of <c>WAO_SUB.ADV</c> (<c>Ui_WaoOp</c> = <paramref name="Op"/>) that opens, closes or
/// decorates a message window: e.g. 111-153 open a window, 143 the centre window, 141/151 the
/// speaker windows, 311/321 a portrait with the CG number in <paramref name="CallArg"/>, 331/333
/// the centre picture, 1/5/11 a key wait (the wait itself is the
/// <see cref="IScriptInput.WaitForKeyAsync"/> call that follows while a window is open), 0/10
/// close. Raised before the operation's state effects (<c>OriginalScriptContext.Wao.cs</c>).
/// </summary>
public sealed record WindowEvent(Sys3Int Op, Sys3Int CallArg) : ScriptEvent;

/// <summary><c>PCMPLAY.ADV</c>: a sound effect or jingle (<c>CallArg</c> = mode, <c>RetVal</c> = sound number).</summary>
public sealed record SoundEvent(Sys3Int Mode, Sys3Int Sound) : ScriptEvent;

/// <summary>
/// A call of <c>M.ADV</c> (strategic map) with <c>CallArg</c> = <paramref name="Mode"/>
/// (0 redraw, 1 scroll to the target, 3/4 province markers, 10/13 blink the target, 11/12
/// pause). Raised before the call's state effects (camera, scratch variables, markers), which
/// <c>OriginalScriptContext.Map.cs</c> ports; its pauses follow as <see cref="IScriptInput.PauseAsync"/> calls.
/// </summary>
public sealed record MapEvent(Sys3Int Mode) : ScriptEvent;

/// <summary>
/// A direct call of <c>WAO_MAP.ADV</c> with <c>CallArg</c> = <paramref name="Mode"/> (3 redraws the
/// map tiles and every province marker). Raised before the call's state effects.
/// </summary>
public sealed record WaoMapEvent(Sys3Int Mode) : ScriptEvent;
