using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// Events of the battle flavour text (FIGT_M.ADV, spec 02 §5). The text itself is a
// MessageEvent(ScriptRef("FIGT_M.ADV", line)) shown inside an open window.

/// <summary>
/// The message-window style opened by FIGT_M's window procedures; the value is the
/// <c>Ui_WaoOp</c> code passed to <c>WAO_SUB.ADV</c>.
/// </summary>
public enum FlavourWindow
{
    /// <summary><c>L_0c4c1</c> (op 141): the left (player-side) speaker window, frame CG 895.</summary>
    Left = 141,

    /// <summary><c>L_0c4dd</c> (op 142): left window, frame CG 896, alternate text colour (<c>ZC 1, 11</c>).</summary>
    LeftAlt = 142,

    /// <summary><c>L_0c515</c> (op 151): the right (enemy-side) speaker window, frame CG 897.</summary>
    Right = 151,

    /// <summary><c>L_0c531</c> (op 152): right window, frame CG 898, alternate text colour (<c>ZC 1, 11</c>).</summary>
    RightAlt = 152,
}

/// <summary>A flavour-text message window opens (<c>WAO_SUB.ADV</c> <c>G Ui_WinFrameCg</c>).</summary>
/// <param name="Window">The window style.</param>
/// <param name="FrameCg">The frame CG drawn (<c>Ui_WinFrameCg</c>).</param>
public sealed record FlavourWindowOpenEvent(FlavourWindow Window, Sys3Int FrameCg) : BattleEvent;

/// <summary>
/// <c>IK 0</c> after a message (<c>WAO_SUB.ADV:515</c>): the text is complete and the battle is
/// about to wait for the player (<see cref="IBattleInput.WaitForKeyAsync"/>). The key code goes
/// to <c>RetVal</c>, which FIGT_M sometimes tests afterwards (C83/C84/unit 3, spec 02 Q13).
/// </summary>
public sealed record FlavourKeyWaitEvent : BattleEvent;

/// <summary>The flavour-text window closes (<c>WAO_SUB.ADV</c> <c>L_00cb9</c>).</summary>
public sealed record FlavourWindowCloseEvent : BattleEvent;
