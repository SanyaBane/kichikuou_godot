using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Combat;

// Events raised by the battle flow (docs/battle/01-flow.md). Windows, messages, sound, music,
// the map and pauses use the shared events of Kichikuou.Engine.Script (ScriptEvents.cs) and the
// helper files' events of Kichikuou.Original.Script (HelperEvents.cs).







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
