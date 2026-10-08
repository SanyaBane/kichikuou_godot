namespace Kichikuou.Core.Combat;

// The enemy's in-battle AI, FIGT.ADV:1411-1491 (docs/battle/01-flow.md §8.5).
public sealed partial class Battle
{
    /// <summary>
    /// <c>L_02112</c>, <c>FIGT.ADV:1411-1491</c> (01 §8.5): the enemy in <c>Bt_EnemyActSlot</c>
    /// picks a target, may defend, and attacks through R1's <c>Resolve</c>/<c>ResolveMagic</c>
    /// (which end in <c>@L_007ac</c>; here they return and the loop continues at Step).
    /// </summary>
    private async Task<FigtLabel> EnemyTurnAsync()
    {
        State.Bt_SpecialAtk = 0;
        FlowCopyString(4, CmdrNameString(State.Cmdr_Cur));
        State.CallArg = State.Bt_EnemyActSlot + 4;
        await HighlightAsync();
        await PauseAsync(15);
        EnemyChooseTarget();
        State.CallArg = State.RetVal;
        await HighlightAsync();
        State.Bt_MsgMode = 4;
        switch (EnemyDecide())
        {
            case AutoKind.Resolve:
                await ResolveAsync();
                return FigtLabel.Step;
            case AutoKind.ResolveMagic:
                await ResolveMagicAsync();
                return FigtLabel.Step;
            default:
                // L_0232f (:1489-1491)
                await RedrawAsync();
                return FigtLabel.Step;
        }
    }

    /// <summary>
    /// <c>FIGT.ADV:1419-1444</c>: the target slot. <c>RetVal := 1</c>, then <c>RND(2)</c>,
    /// <c>RND(3)</c>, <c>RND(4)</c> [R9-R11] for each occupied player slot 2-4 (only the last
    /// draw counts); melee commanders re-roll a back-row pick with <c>RND(2)</c> [R12]. Sets
    /// <c>Unit_Cur</c> and <c>Bt_PlayerActSlot</c>; the slot stays in <c>RetVal</c>.
    /// </summary>
    internal void EnemyChooseTarget()
    {
        State.RetVal = 1;
        if (State.Bt_PlayerSlot2 != 0)
            Rnd(2);
        if (State.Bt_PlayerSlot3 != 0)
            Rnd(3);
        if (State.Bt_PlayerSlot4 != 0)
            Rnd(4);
        if ((Cmdr.Class == 0 || Cmdr.Class == 10) && State.RetVal > 2)
            Rnd(2);
        if (IsSlot(State.RetVal, out int slot))
            State.Unit_Cur = PSlot(slot);
        State.Bt_PlayerActSlot = State.RetVal;
    }

    /// <summary>
    /// <c>FIGT.ADV:1450-1488</c>: <c>RND(3)</c> [R13]; on 1 a weakened (troops below a fifth of
    /// max) non-demon (class &lt; 9) commander defends (slot state 20). Back-row melee idles;
    /// classes 3, 4, 5 and others never act (Q-14). Otherwise sets the exchange actions.
    /// </summary>
    internal AutoKind EnemyDecide()
    {
        Rnd(3);
        if (State.RetVal == 1 && Cmdr.Troops < Cmdr.MaxTroops / 5 && Cmdr.Class < 9)
        {
            if (IsSlot(State.Bt_EnemyActSlot, out int slot))
                EState(slot) = 20;
            return AutoKind.None;
        }
        if ((Cmdr.Class == 0 || Cmdr.Class == 10) && State.Bt_EnemyActSlot > 2)
            return AutoKind.None;
        if (Cmdr.Class == 0 || Cmdr.Class == 10)
        {
            State.Bt_EnemyAction = 0;
            State.Bt_PlayerAction = 0;
            return AutoKind.Resolve;
        }
        // QUIRK: Q-14 — archers in the front row against a front-row target trade direct blows.
        if (Cmdr.Class == 1 && (State.Bt_EnemyActSlot > 2 || State.Bt_PlayerActSlot > 2))
        {
            State.Bt_EnemyAction = 1;
            State.Bt_PlayerAction = 99;
            return AutoKind.Resolve;
        }
        if (Cmdr.Class == 1)
        {
            State.Bt_EnemyAction = 0;
            State.Bt_PlayerAction = 0;
            return AutoKind.Resolve;
        }
        if (Cmdr.Class == 2 || Cmdr.Class == 11)
        {
            State.Bt_EnemyAction = 2;
            State.Bt_PlayerAction = 99;
            return AutoKind.ResolveMagic;
        }
        return AutoKind.None;
    }
}
