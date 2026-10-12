namespace Kichikuou.Original.Combat;

// The battle flavour text (FIGT_M.ADV, spec 02 §5) is a MessageEvent(ScriptRef("FIGT_M.ADV",
// line)) shown inside a window that FIGT_M opens through WAO_SUB.ADV (a WindowEvent with the
// FlavourWindow value as its op).

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



