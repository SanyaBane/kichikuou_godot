using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// Events of the exchange resolution (docs/battle/02-resolution.md §9) and of the battlefield
// redraw (01-flow.md §9.4). They carry what the script shows; values the script reads from the
// state at that moment (e.g. the 16 exchange variables) are read by the presenter from
// Battle.State while the event is being shown.

/// <summary>
/// A call of a <c>WAO_SUB.ADV</c> window operation (<c>Ui_WaoOp</c>), e.g. 141/151 open a
/// speaker window, 143 a narration window, 11 marks a click wait (the wait itself is the
/// <see cref="IBattleInput.WaitForKeyAsync"/> call that follows), 10 closes the window.
/// <c>WAO_SUB.ADV</c>'s own UI-variable writes are not ported (presentation).
/// </summary>
public sealed record WaoCallEvent(Sys3Int Op) : BattleEvent;

/// <summary><c>MP slot, width</c> — prints string variable <paramref name="Slot"/> into the message window.</summary>
public sealed record PrintStringEvent(Sys3Int Slot) : BattleEvent;

/// <summary>
/// <c>L_0320f</c> (<c>FIGT.ADV:2057-2060</c>) — a frame around battlefield panel
/// <paramref name="Panel"/> (1-4 player slots, 5-8 enemy slots).
/// </summary>
public sealed record PanelHighlightEvent(Sys3Int Panel) : BattleEvent;

/// <summary>
/// <c>L_0306c</c> (<c>FIGT.ADV:1992-2009</c>) — the battlefield background: the province's map
/// background <paramref name="Cg"/>, or a plain fill in a dungeon (<paramref name="Cg"/> = 0).
/// </summary>
public sealed record BattlefieldBackgroundEvent(Sys3Int Cg) : BattleEvent;

/// <summary>
/// <c>L_03236</c> (<c>FIGT.ADV:2061-2136</c>) — one battlefield slot panel (1-4 player,
/// 5-8 enemy) for the record under <c>Unit_Cur</c> / <c>Cmdr_Cur</c>.
/// </summary>
public sealed record SlotPanelEvent(Sys3Int Panel, Sys3Int Record) : BattleEvent;

/// <summary>
/// The duel screen of <c>PresentEngagement</c> (<c>FIGT.ADV:1624-1724</c>, 02 §4): the
/// background, the enemy and player sprites in drawing order (usually one each; the player list
/// holds the special-attack CG when a special is used, and is empty for Goliath), whether the
/// background overlay is drawn, and the two faces (enemy face 0 = none).
/// </summary>
public sealed record EngagementSceneEvent(
    Sys3Int Background,
    IReadOnlyList<Sys3Int> EnemySprites,
    IReadOnlyList<Sys3Int> PlayerSprites,
    bool Overlay,
    Sys3Int PlayerFace,
    Sys3Int EnemyFace) : BattleEvent;

/// <summary>
/// The "player (name) action vs enemy (name) action" window of <c>PresentEngagement</c>
/// (<c>FIGT.ADV:1739-1802</c>): the names are string variables 1 and 4, the labels are the
/// script lines shown (<c>null</c> = no label line matched).
/// </summary>
public sealed record ActionLabelsEvent(ScriptRef? PlayerLabel, ScriptRef? EnemyLabel) : BattleEvent;

/// <summary>
/// One step of the duel window sliding in (<c>FIGT.ADV:1807-1828</c>): the window is drawn
/// <paramref name="Offset"/> pixels below its final position (112, then after the flavour text
/// 84, 56, 28, 0), followed by a <see cref="CountersEvent"/>.
/// </summary>
public sealed record EngagementSlideEvent(Sys3Int Offset) : BattleEvent;

/// <summary>
/// <c>L_02c00</c> (<c>FIGT.ADV:1830-1863</c>) or the first block of <c>FIGTATTK.ADV:4-31</c> —
/// troops (shown only when non-zero) and HP of both commanders.
/// </summary>
public sealed record CountersEvent(Sys3Int UnitTroops, Sys3Int CmdrTroops, Sys3Int UnitHP, Sys3Int CmdrHP) : BattleEvent;

/// <summary><c>SQ a, b, 1</c> — plays two sound effects (<c>FIGTATTK.ADV:344</c>, <c>:350</c>).</summary>
public sealed record PlaySfxEvent(Sys3Int First, Sys3Int Second) : BattleEvent;

/// <summary>
/// One frame of the shot animation (<c>FIGTATTK.ADV:34-131</c>): the non-zero shot counters
/// <c>Bt_*Shots</c> moved <paramref name="Step"/> pixels (0, 40, ..., 360).
/// </summary>
public sealed record ShotsFrameEvent(Sys3Int Step) : BattleEvent;

/// <summary>
/// The result screen of <c>FIGTATTK.ADV:134-232</c>: predicted troops/HP (current value minus the
/// two damages, saturating) and the damage of every non-zero shot counter.
/// </summary>
public sealed record ExchangeResultsEvent : BattleEvent;

/// <summary>
/// The "wiped out" marks of <c>FIGTATTK.ADV:235-246</c> (CG 889-892): shown for a value that was
/// non-zero and is predicted to reach 0.
/// </summary>
public sealed record WipedOutMarksEvent(bool PlayerTroops, bool EnemyTroops, bool PlayerCmdr, bool EnemyCmdr) : BattleEvent;

/// <summary>
/// <c>IK 0</c> at <c>FIGTATTK.ADV:247</c>: the exchange animation is complete and the battle is
/// about to wait for a key (<see cref="IBattleInput.WaitForKeyAsync"/>, result in <c>RetVal</c>).
/// </summary>
public sealed record KeyWaitEvent : BattleEvent;

/// <summary>
/// <c>&amp;#FILE.ADV:</c> — the script jumped to another scenario file and never comes back (e.g.
/// the game over at <c>FIGTﾐｶDE.ADV:39</c>). Unwinds the battle; the caller runs <see cref="File"/>.
/// </summary>
public sealed class ScenarioJumpException(string file)
    : Exception($"The scenario jumped to {file}.")
{
    /// <summary>The scenario file the script jumped to, e.g. <c>OVER.ADV</c>.</summary>
    public string File { get; } = file;
}
