using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// Battle flavour text, FIGT_M.ADV (spec docs/battle/02-resolution.md §5).
//
// This file holds the hand-ported entry (FIGT_M.ADV:1-23) and the window procedures
// (10306-10353, with the parts of WAO_SUB.ADV they run). Everything in between — the player
// and enemy dispatch, every unit/commander branch, the Asuka/Satella/Freak/Sieg scenes — is
// in Battle.Flavour.g.cs, translated statement by statement from the script by
// tools/analysis/gen_figt_m.py (re-run it against the raw decompile; `--check` verifies).
public sealed partial class Battle
{
    private const string FlavourFile = "FIGT_M.ADV";

    /// <summary>
    /// FlavourText — the call of <c>FIGT_M.ADV</c> from <c>PresentEngagement</c>: picks the
    /// speaking side from <c>Bt_MsgMode</c> and shows one battle line (2 or 3 draws).
    /// <c>FIGT_M.ADV:1-23</c>, spec 02 §5.1 (draw count §5.5).
    /// </summary>
    internal async Task FlavourTextAsync()
    {
        State.Tmp0621 = 0; // :1
        if (State.Bt_MsgMode == 1 && State.Bt_EnemyAction == 2) // :2  enemy-initiated magic
            State.Bt_MsgMode = 2; // :3  QUIRK: §5.4 / Q11 — persists after the call
        Rnd(2); // :5  QUIRK: Q11 — always drawn, its result survives only for Bt_MsgMode outside 1..4
        if (State.Bt_MsgMode == 1) // :6
            State.RetVal = 1;
        if (State.Bt_MsgMode == 2) // :9
            State.RetVal = 2;
        if (State.Bt_MsgMode == 3) // :12
            State.RetVal = 1;
        if (State.Bt_MsgMode == 4) // :15
            State.RetVal = 2;
        if (State.RetVal == 1) // :18 @L_000b5
        {
            await FlavourPlayerSideAsync();
            return;
        }
        if (State.RetVal == 2) // :21 @L_036f2
        {
            await FlavourEnemySideAsync();
            return;
        }
        // QUIRK: §5.1 / Q20 — any other RetVal (a stale Bt_MsgMode with Y 4,2 returning 3)
        // falls through into the player side at :24.
        await FlavourPlayerSideAsync();
    }

    /// <summary>
    /// One complete message: open <paramref name="window"/>, then for each text line show it and
    /// wait for a key, then close. The generator's shorthand for the script's sequence
    /// <c>\L_open: ZH 1: text \L_0c576: [ZH 1: text \L_0c576:]… \L_0c569:</c>.
    /// </summary>
    private async Task SayAsync(FlavourWindow window, params int[] lines)
    {
        await OpenWindowAsync(window);
        foreach (int line in lines)
        {
            await ShowTextAsync(line);
            await WaitKeyAsync();
        }
        await CloseWindowAsync();
    }

    /// <summary>A text run of FIGT_M (<c>ZH 1:</c> then <c>'…'</c>/<c>R</c> lines starting at <paramref name="line"/>).</summary>
    private Task ShowTextAsync(int line) =>
        View.ShowAsync(new MessageEvent(new ScriptRef(FlavourFile, line)));

    /// <summary>
    /// Opens a message window: <c>L_0c4c1</c>/<c>L_0c4dd</c>/<c>L_0c515</c>/<c>L_0c531</c>
    /// (<c>FIGT_M.ADV:10306-10345</c>) → <c>WAO_SUB.ADV</c> ops 141/142/151/152
    /// (<c>L_00b78</c>/<c>L_00b94</c>/<c>L_00bee</c>/<c>L_00c0a</c>, <c>WAO_SUB.ADV:453-491</c>).
    /// Each FIGT_M procedure returns right after its first WAO_SUB call; the second op
    /// (241, 242, …) after <c>\0:</c> is unreachable.
    /// </summary>
    private async Task OpenWindowAsync(FlavourWindow window)
    {
        State.Ui_WaoOp = (Sys3Int)(int)window;
        // WAO_SUB: \L_00bb6 / \L_00c2d (screen save and box, display only).
        (Sys3Int frame, Sys3Int type) = window switch
        {
            FlavourWindow.Left => ((Sys3Int)895, (Sys3Int)41),
            FlavourWindow.LeftAlt => ((Sys3Int)896, (Sys3Int)42),
            FlavourWindow.Right => ((Sys3Int)897, (Sys3Int)51),
            FlavourWindow.RightAlt => ((Sys3Int)898, (Sys3Int)52),
            _ => throw new ArgumentOutOfRangeException(nameof(window), window, null),
        };
        State.Ui_WinFrameCg = frame;
        await View.ShowAsync(new FlavourWindowOpenEvent(window, State.Ui_WinFrameCg)); // G Ui_WinFrameCg, 40 (ZC 1, 11 for the Alt styles)
        State.Ui_WinType = type;
    }

    /// <summary>
    /// Waits for the player after a message: <c>L_0c576</c> (<c>FIGT_M.ADV:10350-10353</c>) →
    /// <c>WAO_SUB.ADV</c> op 11, <c>L_00ce1</c> (<c>WAO_SUB.ADV:510-519</c>): returns at once when
    /// no window is open, otherwise <c>IK 0</c> stores the key code in <c>RetVal</c>.
    /// </summary>
    private async Task WaitKeyAsync()
    {
        State.Ui_WaoOp = 11;
        if (State.Ui_WinType == 0)
            return; // WAO_SUB.ADV:511-512 (the error text after \0: is unreachable)
        var wait = new FlavourKeyWaitEvent();
        await View.ShowAsync(wait); // IK 0
        State.RetVal = wait.KeyCode; // QUIRK: Q13 / README O1 — IK 0 overwrites RetVal
        // Y 1, 1 and G Ui_WinFrameCg, 40: clear the text (display only).
    }

    /// <summary>
    /// Closes the message window: <c>L_0c569</c> (<c>FIGT_M.ADV:10346-10349</c>) →
    /// <c>WAO_SUB.ADV</c> op 10, <c>L_00cb9</c> (<c>WAO_SUB.ADV:504-509</c>).
    /// </summary>
    private async Task CloseWindowAsync()
    {
        State.Ui_WaoOp = 10;
        await View.ShowAsync(new FlavourWindowCloseEvent()); // CC (restore the screen), ZC 1, 15
        State.Ui_WinType = 0;
        State.Ui_WinFrameCg = 0;
    }
}
