using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// Events raised by the battle flow (docs/battle/01-flow.md). Messages use MessageEvent with the
// line of the first output command of a text run (a quoted string, or X / MP / H when the run
// starts with a string or number variable); the presentation layer reads string variables
// (X n, MP n) from GameState.Strings at the time of the event. A run that prints a number
// (H 0, var) uses FlowValueMessageEvent, because the variable may change before it is drawn.

/// <summary>
/// A call of <c>WAO_SUB.ADV</c> that opens, closes or decorates a message window
/// (<c>Ui_WaoOp</c> = <paramref name="Op"/>; e.g. 143 centre window, 113 wrapper window,
/// 311/321 portrait with the CG number in <paramref name="CallArg"/>, 0/10 close).
/// </summary>
public sealed record FlowWindowEvent(Sys3Int Op, Sys3Int CallArg) : BattleEvent;

/// <summary>A message that prints numbers; <paramref name="Values"/> are the printed values in order.</summary>
public sealed record FlowValueMessageEvent(ScriptRef Text, IReadOnlyList<Sys3Int> Values) : BattleEvent;

/// <summary><c>PCMPLAY.ADV</c>: a sound effect or jingle (<c>CallArg</c> = mode, <c>RetVal</c> = sound number).</summary>
public sealed record FlowSoundEvent(Sys3Int Mode, Sys3Int Sound) : BattleEvent;

/// <summary><c>SS n</c>: plays music track n (0 stops the music).</summary>
public sealed record FlowMusicEvent(Sys3Int Track) : BattleEvent;

/// <summary>
/// A call of <c>M.ADV</c> (strategic map) with <c>CallArg</c> = <paramref name="Mode"/>
/// (0 redraw, 1/10 centre on the target, 3/4 province marker, 13 …). The map screen's own
/// state (camera, its scratch variables) belongs to the map port.
/// </summary>
public sealed record FlowMapEvent(Sys3Int Mode) : BattleEvent;

/// <summary><c>EC 0</c>: clears the dungeon screen (the dungeon counterpart of <see cref="FlowMapEvent"/>).</summary>
public sealed record FlowClearScreenEvent : BattleEvent;

/// <summary>
/// The round header box (<c>FIGT.ADV:112-213</c>): battle kind, troop limit, the two advantage
/// numbers and the rounds left.
/// </summary>
public sealed record RoundHeaderEvent(
    Sys3Int IsDuel,
    Sys3Int Kind,
    Sys3Int TroopLimit,
    Sys3Int PlayerAdvantage,
    Sys3Int EnemyAdvantage,
    Sys3Int RoundsLeft) : BattleEvent;

/// <summary><c>FIGT作.ADV</c>: the strategist's portrait on a successful strategy (side 1 player, 2 enemy).</summary>
public sealed record StrategyScreenEvent(Sys3Int Side, Sys3Int Strategist) : BattleEvent;

/// <summary><c>FIGT.ADV:834-1020</c>: the info box of a player unit (<paramref name="Order"/> = its standing order).</summary>
public sealed record UnitInfoEvent(Sys3Int Unit, Sys3Int Order) : BattleEvent;

/// <summary><c>FIGT.ADV:1021-1119</c>: the info box of an enemy commander.</summary>
public sealed record EnemyInfoEvent(Sys3Int Cmdr) : BattleEvent;

/// <summary>An error box of the script (<c>FIGT.ADV:1257-1264</c>, command error).</summary>
public sealed record FlowErrorEvent(ScriptRef Text) : BattleEvent;

/// <summary><c>FIGT_B.ADV:649-672</c>: one line of the ninja scout report (an enemy commander).</summary>
public sealed record ScoutReportEvent(Sys3Int Cmdr, Sys3Int Troops) : BattleEvent;
