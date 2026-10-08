using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

/// <summary>The labels of <c>FIGT.ADV</c> that the flow jumps between (01 §3-§8).</summary>
internal enum FigtLabel
{
    /// <summary><c>L_00295</c> (<c>FIGT.ADV:105</c>): round start.</summary>
    RoundStart,

    /// <summary><c>L_007ac</c> (<c>:314</c>): target after every action.</summary>
    Step,

    /// <summary><c>L_007ca</c> (<c>:320</c>): actor search.</summary>
    Pick,

    /// <summary><c>L_01abd</c> (<c>:1120</c>): Goliath strike (R1).</summary>
    Goliath,

    /// <summary><c>L_00b3c</c> (<c>:447</c>): round end.</summary>
    RoundEnd,

    /// <summary><c>L_00c26</c> (<c>:489</c>).</summary>
    PlayerAnnihilated,

    /// <summary><c>L_00c73</c> (<c>:505</c>).</summary>
    AttackerLost,

    /// <summary><c>L_00c82</c> (<c>:509</c>).</summary>
    EnemyAnnihilated,

    /// <summary><c>L_00ccf</c> (<c>:525</c>).</summary>
    DefenderWon,

    /// <summary><c>L_00cde</c> (<c>:529</c>).</summary>
    EnemyRetreats,

    /// <summary><c>L_00d3f</c> (<c>:548</c>).</summary>
    PlayerRetreat,

    /// <summary><c>L_00dd3</c> (<c>:581</c>).</summary>
    RetreatConfirmed,

    /// <summary><c>L_00dfd</c> (<c>:591</c>): return from FIGT.</summary>
    End,

    /// <summary><c>L_00e0d</c> (<c>:595</c>): player turn.</summary>
    PlayerTurn,

    /// <summary><c>L_00ee3</c> (<c>:624</c>): command selection.</summary>
    Select,

    /// <summary><c>L_00f55</c> (<c>:641</c>): command dispatch on <c>RetVal</c>.</summary>
    Dispatch,

    /// <summary><c>L_01fd5</c> (<c>:1357</c>): standing order.</summary>
    AutoAction,

    /// <summary><c>L_02078</c> (<c>:1382</c>): defend.</summary>
    Defend,

    /// <summary><c>L_02112</c> (<c>:1411</c>): enemy turn.</summary>
    EnemyTurn,
}

// FIGT.ADV: setup, strategy, the round loop and the exits (docs/battle/01-flow.md §3-§7).
//
// The script is a goto machine; it is ported as a loop over FigtLabel where every handler
// returns the label the script jumps to next. Where the script ends a procedure with a jump
// back into the actor loop after an R1 exchange (Resolve / ResolveMagic / GoliathStrike end
// with "@L_007ac"), the R1 seam method returns instead and the handler returns
// FigtLabel.Step: the same code runs next, with the same state.
public sealed partial class Battle
{
    /// <summary>
    /// <c>%#FIGT.ADV</c> called directly — the dungeon (<c>Cﾒｲｷｭｳ.ADV:1282</c>, 01 §2.4) and the
    /// God-dungeon garrisons (<c>Iｶﾐ.ADV:4607-4796</c>, §2.5); the wrappers call it too.
    /// Runs the whole battle (<c>FIGT.ADV:1-594</c>) and returns with <c>Bt_Result</c> set
    /// (0 dungeon timeout, 1 lost, 2 won).
    /// </summary>
    /// <remarks>
    /// Preconditions (set by the caller exactly as the script does): <c>Bt_Kind</c> (1 attack,
    /// 2 defence, 3 dungeon), <c>Bt_PlayerParty1..4</c> and <c>Bt_EnemyParty1..4</c> (slot 1
    /// non-zero), <c>Bt_TroopLimit</c>, <c>Bt_WallBonus</c>, <c>Bt_EnemyRetreatMode</c> (not reset
    /// here: a caller that does not write it inherits the last battle's value, Q-23),
    /// <c>Bt_IsDuel</c>, <c>Bt_BackgroundCg</c>, <c>Prov_Cur</c>, and the unit / commander records.
    /// The dungeon resets <c>Bt_Kind</c>/<c>Bt_IsDuel</c> and calls <see cref="DeathCaptureAsync"/>
    /// itself afterwards; the God dungeon copies <c>Bt_PlayerSlot1..4</c> back to its garrison.
    /// A game over inside the battle throws <see cref="ScenarioJumpException"/>.
    /// </remarks>
    public async Task FightAsync()
    {
        InitBattle();
        // FIGT.ADV:34-40 — ZC/B3 (presentation), battle music, first redraw.
        State.CallArg = 1;
        State.RetVal = 102;
        await FlowSoundAsync(1);
        await RedrawAsync();
        ChooseStrategists();
        await StrategyPhaseAsync();

        var label = FigtLabel.RoundStart;
        while (label != FigtLabel.End)
            label = await RunLabelAsync(label);
        // FIGT.ADV:591-594 — ZC 6/1 (presentation), %0.
    }

    /// <summary>Runs one label of the FIGT goto machine and returns the next one.</summary>
    private async Task<FigtLabel> RunLabelAsync(FigtLabel label)
    {
        switch (label)
        {
            case FigtLabel.RoundStart: return await RoundStartAsync();
            case FigtLabel.Step: return StepEntry();
            case FigtLabel.Pick: return Pick();
            case FigtLabel.Goliath:
                await GoliathStrikeAsync();
                return FigtLabel.Step;
            case FigtLabel.RoundEnd: return await RoundEndAsync();
            case FigtLabel.PlayerAnnihilated: return await PlayerAnnihilatedAsync();
            case FigtLabel.AttackerLost: return await AttackerLostAsync();
            case FigtLabel.EnemyAnnihilated: return await EnemyAnnihilatedAsync();
            case FigtLabel.DefenderWon: return await DefenderWonAsync();
            case FigtLabel.EnemyRetreats: return await EnemyRetreatsAsync();
            case FigtLabel.PlayerRetreat: return await PlayerRetreatAsync();
            case FigtLabel.RetreatConfirmed: return await RetreatConfirmedAsync();
            case FigtLabel.PlayerTurn: return await PlayerTurnAsync();
            case FigtLabel.Select: return await SelectAsync();
            case FigtLabel.Dispatch: return await DispatchAsync();
            case FigtLabel.AutoAction: return await AutoActionAsync();
            case FigtLabel.Defend: return await DefendAsync();
            case FigtLabel.EnemyTurn: return await EnemyTurnAsync();
            default: throw new ArgumentOutOfRangeException(nameof(label));
        }
    }

    /// <summary>
    /// Battle initialisation, <c>FIGT.ADV:1-33</c> (01 §3): slots from the parties, focus and
    /// orders cleared, rounds <c>3 + RND(2)</c> [R3] (3 for retreat mode 6), counters, troop sums.
    /// </summary>
    internal void InitBattle()
    {
        for (int i = 1; i <= 4; i++)
            PSlot(i) = PParty(i);
        for (int i = 1; i <= 4; i++)
            ESlot(i) = EParty(i);
        for (int i = 1; i <= 4; i++)
            PFocus(i) = 0;
        for (int i = 1; i <= 4; i++)
            POrder(i) = 0;
        Rnd(2);
        State.Bt_RoundsLeft = State.RetVal + 3;
        if (State.Bt_EnemyRetreatMode == 6)
            State.Bt_RoundsLeft = 3;
        State.VAR0603 = 0;
        State.Bt_PlayerLosses = 0;
        State.Bt_EnemyLosses = 0;
        State.Bt_StrategyResult = 0;
        State.Bt_SpecialAtk = 0;
        State.CallArg = 1;
        TroopSum();
        State.Bt_PlayerTroopsStart = State.CallArg;
        State.CallArg = 2;
        TroopSum();
        State.Bt_EnemyTroopsStart = State.CallArg;
        State.Bt_EnemyRetreat = 0;
    }

    /// <summary>
    /// <c>L_04905</c>, <c>FIGT.ADV:2935-2976</c>: troop sum of the side in <c>CallArg</c>
    /// (1 player, 2 enemy) into <c>CallArg</c>; moves the side's record cursor.
    /// </summary>
    internal void TroopSum()
    {
        if (State.CallArg == 1)
        {
            State.CallArg = 0;
            for (int i = 1; i <= 4; i++)
            {
                if (PSlot(i) != 0)
                {
                    State.Unit_Cur = PSlot(i);
                    State.CallArg = State.CallArg + Unit.Troops;
                }
            }
            return;
        }
        if (State.CallArg == 2)
        {
            State.CallArg = 0;
            for (int i = 1; i <= 4; i++)
            {
                if (ESlot(i) != 0)
                {
                    State.Cmdr_Cur = ESlot(i);
                    State.CallArg = State.CallArg + Cmdr.Troops;
                }
            }
        }
    }

    /// <summary>
    /// <c>FIGT.ADV:41-92</c> (01 §3): the best strategist of each side — player value in
    /// <c>Tmp0021</c> (slot 1 taken without a non-zero check), enemy value in <c>Tmp0022</c>;
    /// ties keep the earlier slot.
    /// </summary>
    internal void ChooseStrategists()
    {
        State.Unit_Cur = State.Bt_PlayerSlot1;
        StrategyValue();
        State.Tmp0021 = State.Tmp0023;
        State.Bt_PlayerStrategist = State.Bt_PlayerSlot1;
        for (int i = 2; i <= 4; i++)
        {
            if (PSlot(i) != 0)
            {
                State.Unit_Cur = PSlot(i);
                StrategyValue();
                if (State.Tmp0023 > State.Tmp0021)
                {
                    State.Tmp0021 = State.Tmp0023;
                    State.Bt_PlayerStrategist = PSlot(i);
                }
            }
        }
        State.Cmdr_Cur = State.Bt_EnemySlot1;
        State.Tmp0022 = Cmdr.Strategy;
        State.Bt_EnemyStrategist = State.Bt_EnemySlot1;
        for (int i = 2; i <= 4; i++)
        {
            if (ESlot(i) != 0)
            {
                State.Cmdr_Cur = ESlot(i);
                if (Cmdr.Strategy > State.Tmp0022)
                {
                    State.Tmp0022 = Cmdr.Strategy;
                    State.Bt_EnemyStrategist = ESlot(i);
                }
            }
        }
    }

    /// <summary>
    /// <c>L_04e85</c>, <c>FIGT.ADV:3161-3169</c>: strategy of <c>Unit_Cur</c> plus an equipped
    /// effect-6 item's amount, into <c>Tmp0023</c>; moves <c>Item_Cur</c>.
    /// </summary>
    internal void StrategyValue()
    {
        State.Tmp0023 = Unit.Strategy;
        if (Unit.Item != 0)
        {
            State.Item_Cur = Unit.Item;
            if (Item.Effect == 6)
                State.Tmp0023 = State.Tmp0023 + Item.Amount;
        }
    }

    /// <summary>
    /// <c>FIGT.ADV:93-104</c> (01 §3): the pre-battle strategy, skipped in dungeons.
    /// </summary>
    private async Task StrategyPhaseAsync()
    {
        if (State.Bt_Kind != 3)
        {
            if (State.Tmp0021 > State.Tmp0022)
            {
                State.CallArg = 1;
                await StrategyAsync();
                await RedrawAsync();
            }
            // QUIRK: Q-28 — a second independent test; after the first branch it is false only
            // because RedrawAsync overwrote Tmp0021/Tmp0022 with panel coordinates.
            if (State.Tmp0022 > State.Tmp0021)
            {
                State.CallArg = 2;
                await StrategyAsync();
                await RedrawAsync();
            }
        }
    }

    /// <summary>
    /// <c>L_00295</c>, <c>FIGT.ADV:105-313</c> (01 §5): retreat check of modes 3/4, the header
    /// (duel troop limit), the Land of Death drain and the slot-state reset.
    /// </summary>
    private async Task<FigtLabel> RoundStartAsync()
    {
        if (RoundStartRetreats())
            return FigtLabel.EnemyRetreats;
        await RoundHeaderAsync();
        await RedrawAsync();
        if (State.Prov_Cur == 82)
            await LandOfDeathAsync();
        ResetSlotStates();
        return FigtLabel.Step;
    }

    /// <summary>
    /// <c>FIGT.ADV:106-111</c>: modes 3 and 4 retreat at round start when the enemy lost more than
    /// a half / a quarter of its starting troops.
    /// </summary>
    internal bool RoundStartRetreats() =>
        (State.Bt_EnemyRetreatMode == 3 && State.Bt_EnemyLosses * 2 > State.Bt_EnemyTroopsStart) ||
        (State.Bt_EnemyRetreatMode == 4 && State.Bt_EnemyLosses * 4 > State.Bt_EnemyTroopsStart);

    /// <summary>
    /// The header box, <c>FIGT.ADV:112-214</c>: in a duel outside dungeons the troop limit is
    /// forced to 5000 (Q-6); the advantage numbers go to <c>Tmp0001</c> (player) and
    /// <c>Tmp0002</c> (enemy); string 7 is set; then <c>IK 0</c>.
    /// </summary>
    internal async Task RoundHeaderAsync()
    {
        if (State.Bt_IsDuel == 1)
        {
            // QUIRK: Q-6 — a state write inside the header drawing.
            if (State.Bt_Kind != 3)
                State.Bt_TroopLimit = 5000;
        }
        FlowSetString(7, " Rance Army");
        State.Tmp0001 = 0;
        State.Tmp0002 = 0;
        if (State.Bt_Kind == 1 || State.Bt_Kind == 3)
            State.Tmp0002 = State.Tmp0002 + State.Bt_WallBonus;
        if (State.Bt_Kind == 2)
            State.Tmp0001 = State.Tmp0001 + State.Bt_WallBonus;
        if (State.Bt_StrategyResult == 1)
            State.Tmp0001 = State.Tmp0001 + 10;
        if (State.Bt_StrategyResult == 2)
            State.Tmp0001 = State.Tmp0001 + 50;
        if (State.Bt_StrategyResult == 11)
            State.Tmp0002 = State.Tmp0002 + 10;
        if (State.Bt_StrategyResult == 12)
            State.Tmp0002 = State.Tmp0002 + 50;
        await View.ShowAsync(new RoundHeaderEvent(
            State.Bt_IsDuel, State.Bt_Kind, State.Bt_TroopLimit, State.Tmp0001, State.Tmp0002, State.Bt_RoundsLeft));
        var answer = await Input.WaitForKeyAsync();
        State.RetVal = answer;
    }

    /// <summary>
    /// <c>FIGT.ADV:217-281</c> (01 §5): in the Land of Death (province 82) every unit loses
    /// <c>RND(troops / 10)</c> [R5] troops, in the order P1-P4, E1-E4 (slot 1 of each side is not
    /// checked for being occupied), with a redraw after each.
    /// </summary>
    private async Task LandOfDeathAsync()
    {
        for (int i = 1; i <= 4; i++)
        {
            State.Unit_Cur = PSlot(i);
            if (i == 1 || State.Unit_Cur != 0)
            {
                State.Tmp0001 = Unit.Troops;
                LandOfDeathDrain();
                Unit.Troops = State.Tmp0001;
                await RedrawAsync();
            }
        }
        for (int i = 1; i <= 4; i++)
        {
            State.Cmdr_Cur = ESlot(i);
            if (i == 1 || State.Cmdr_Cur != 0)
            {
                State.Tmp0001 = Cmdr.Troops;
                LandOfDeathDrain();
                Cmdr.Troops = State.Tmp0001;
                await RedrawAsync();
            }
        }
        await FigtBoxAsync(272);
    }

    /// <summary>
    /// <c>L_04eba</c>, <c>FIGT.ADV:3170-3174</c>: <c>Tmp0001 -= RND(Tmp0001 / 10)</c> [R5]
    /// (no draw when the quotient is 0 or 1: the quotient itself is subtracted).
    /// </summary>
    internal void LandOfDeathDrain()
    {
        State.Tmp0002 = State.Tmp0001 / 10;
        Rnd(State.Tmp0002);
        State.Tmp0001 = State.Tmp0001 - State.RetVal;
    }

    /// <summary><c>FIGT.ADV:282-313</c>: every slot's state to 0, empty slots to 100.</summary>
    internal void ResetSlotStates()
    {
        for (int i = 1; i <= 4; i++)
            PState(i) = 0;
        for (int i = 1; i <= 4; i++)
            EState(i) = 0;
        for (int i = 1; i <= 4; i++)
        {
            if (PSlot(i) == 0)
                PState(i) = 100;
        }
        for (int i = 1; i <= 4; i++)
        {
            if (ESlot(i) == 0)
                EState(i) = 100;
        }
    }

    /// <summary>
    /// <c>L_007ac</c>, <c>FIGT.ADV:314-319</c> (01 §6): the pending enemy retreat is taken here;
    /// otherwise the idle counter and the acting player slot are reset.
    /// </summary>
    internal FigtLabel StepEntry()
    {
        if (State.Bt_EnemyRetreat == 3)
            return FigtLabel.EnemyRetreats;
        State.Bt_RoundStep = 0;
        State.Bt_PlayerActSlot = 0;
        return FigtLabel.Pick;
    }

    /// <summary>
    /// <c>L_007ca</c>, <c>FIGT.ADV:320-446</c> (01 §6): annihilation checks, Goliath, the idle
    /// counter, then the not-yet-acted unit with the fewest troops (slot 1 taken without a
    /// comparison, ties to the lower slot and the player, Q-17) becomes the actor. The choice is
    /// in <c>RetVal</c> (9 = nobody) and the lowest troops in <c>Tmp0002</c>.
    /// </summary>
    internal FigtLabel Pick()
    {
        if (State.Bt_PlayerSlot1 == 0)
            return FigtLabel.PlayerAnnihilated;
        if (State.Bt_EnemySlot1 == 0)
            return FigtLabel.EnemyAnnihilated;
        if (State.Goliath_State == 1 && State.Goliath_Prov == State.Prov_Cur)
            return FigtLabel.Goliath;
        State.Bt_RoundStep = State.Bt_RoundStep + 1;
        if (State.Bt_RoundStep == 10)
            return FigtLabel.RoundEnd;
        State.RetVal = 9;
        State.Tmp0002 = (Sys3Int)10000 * 6 + 5535;
        // QUIRK: Q-17 — slot 1 is taken without comparing against the 65535 sentinel.
        if (State.Bt_PSlot1State == 0 && State.Bt_PlayerSlot1 != 0)
        {
            State.Unit_Cur = State.Bt_PlayerSlot1;
            State.RetVal = 1;
            State.Tmp0002 = Unit.Troops;
        }
        for (int i = 2; i <= 4; i++)
        {
            if (PState(i) == 0 && PSlot(i) != 0)
            {
                State.Unit_Cur = PSlot(i);
                if (Unit.Troops < State.Tmp0002)
                {
                    State.RetVal = (Sys3Int)i;
                    State.Tmp0002 = Unit.Troops;
                }
            }
        }
        for (int i = 1; i <= 4; i++)
        {
            if (EState(i) == 0 && ESlot(i) != 0)
            {
                State.Cmdr_Cur = ESlot(i);
                if (Cmdr.Troops < State.Tmp0002)
                {
                    State.RetVal = (Sys3Int)(4 + i);
                    State.Tmp0002 = Cmdr.Troops;
                }
            }
        }
        for (int i = 1; i <= 4; i++)
        {
            if (State.RetVal == (Sys3Int)i && PState(i) == 0 && PSlot(i) != 0)
            {
                PState(i) = 1;
                State.Bt_PlayerActSlot = (Sys3Int)i;
                State.Unit_Cur = PSlot(i);
                State.Bt_ActFocus = PFocus(i);
                State.Bt_CurOrder = POrder(i);
                return FigtLabel.PlayerTurn;
            }
        }
        for (int i = 1; i <= 4; i++)
        {
            if (State.RetVal == (Sys3Int)(4 + i) && EState(i) == 0 && ESlot(i) != 0)
            {
                EState(i) = 1;
                State.Bt_EnemyActSlot = (Sys3Int)i;
                State.Cmdr_Cur = ESlot(i);
                return FigtLabel.EnemyTurn;
            }
        }
        return FigtLabel.Pick;
    }

    /// <summary>
    /// <c>L_00b3c</c>, <c>FIGT.ADV:447-488</c> (01 §7): outside duels one round passes; at 0 the
    /// battle times out — the attacking player loses, the defending player wins, a dungeon ends
    /// with <c>Bt_Result = 0</c>.
    /// </summary>
    internal async Task<FigtLabel> RoundEndAsync()
    {
        if (State.Bt_IsDuel == 0)
        {
            State.Bt_RoundsLeft = State.Bt_RoundsLeft - 1;
            if (State.Bt_RoundsLeft == 0)
            {
                await WaoAsync(143);
                await FlowTextAsync(FigtFile, 453);
                if (State.Bt_Kind != 3)
                {
                    await FlowTextAsync(FigtFile, 458);
                    await FlowWaitKeyAsync();
                }
                if (State.Bt_Kind == 3)
                {
                    await FlowTextAsync(FigtFile, 465);
                    await FlowWaitKeyAsync();
                }
                await WaoAsync(10);
                State.Bt_Result = 0;
                if (State.Bt_Kind == 1)
                    return FigtLabel.AttackerLost;
                if (State.Bt_Kind == 2)
                    return FigtLabel.DefenderWon;
                // Dead code in practice: mode 101 is never assigned (01 §7).
                if (State.Bt_EnemyRetreatMode == 101)
                {
                    State.CallArg = 1;
                    await CallFileAsync("Gﾀﾐ.ADV");
                    return FigtLabel.EnemyAnnihilated;
                }
                await AwardExpAsync();
                await MapRedrawAsync();
                return FigtLabel.End;
            }
        }
        return FigtLabel.RoundStart;
    }

    /// <summary><c>L_00c26</c>, <c>FIGT.ADV:489-504</c>: the player side is wiped out.</summary>
    internal async Task<FigtLabel> PlayerAnnihilatedAsync()
    {
        State.Bt_Result = 1;
        if (State.Bt_IsDuel == 1)
        {
            await MapRedrawAsync();
            await AwardExpAsync();
            return FigtLabel.End;
        }
        await FigtBoxAsync(498);
        await AwardExpAsync();
        return FigtLabel.End;
    }

    /// <summary><c>L_00c73</c>, <c>FIGT.ADV:505-508</c>: timeout as attacker, or a retreat.</summary>
    internal async Task<FigtLabel> AttackerLostAsync()
    {
        State.Bt_Result = 1;
        await AwardExpAsync();
        return FigtLabel.End;
    }

    /// <summary><c>L_00c82</c>, <c>FIGT.ADV:509-524</c>: the enemy side is wiped out.</summary>
    internal async Task<FigtLabel> EnemyAnnihilatedAsync()
    {
        State.Bt_Result = 2;
        if (State.Bt_IsDuel == 1)
        {
            await MapRedrawAsync();
            await AwardExpAsync();
            return FigtLabel.End;
        }
        await FigtBoxAsync(518);
        await AwardExpAsync();
        return FigtLabel.End;
    }

    /// <summary><c>L_00ccf</c>, <c>FIGT.ADV:525-528</c>: timeout as defender, enemy retreat, debug Repel.</summary>
    internal async Task<FigtLabel> DefenderWonAsync()
    {
        State.Bt_Result = 2;
        await AwardExpAsync();
        return FigtLabel.End;
    }

    /// <summary>
    /// <c>L_00cde</c>, <c>FIGT.ADV:529-547</c>: the enemy retreats (reported as annihilation when
    /// its slot 1 is already empty).
    /// </summary>
    private async Task<FigtLabel> EnemyRetreatsAsync()
    {
        await RedrawAsync();
        return await EnemyRetreatsAfterRedrawAsync();
    }

    /// <summary><c>FIGT.ADV:531-547</c>: the part of <c>L_00cde</c> after its redraw.</summary>
    internal async Task<FigtLabel> EnemyRetreatsAfterRedrawAsync()
    {
        if (State.Bt_EnemySlot1 == 0)
            return FigtLabel.EnemyAnnihilated;
        State.Bt_Result = 2;
        if (State.Bt_IsDuel == 1)
        {
            await MapRedrawAsync();
            await AwardExpAsync();
            return FigtLabel.End;
        }
        await FigtBoxAsync(542);
        return FigtLabel.DefenderWon;
    }

    /// <summary>
    /// <c>L_00d3f</c>, <c>FIGT.ADV:548-580</c> (01 §7): command 6. Refused in duels and dungeons
    /// (the turn is not consumed); otherwise a Retreat / Cease Retreat menu (cancel = cease).
    /// </summary>
    internal async Task<FigtLabel> PlayerRetreatAsync()
    {
        if (State.Bt_IsDuel == 1)
        {
            await FigtBoxAsync(552);
            return FigtLabel.PlayerTurn;
        }
        if (State.Bt_Kind == 3)
        {
            await FigtBoxAsync(562);
            return FigtLabel.PlayerTurn;
        }
        var menu = new BattleMenu(BattleMenuKind.RetreatConfirm,
        [
            new BattleMenuOption(1, new ScriptRef(FigtFile, 573)),
            new BattleMenuOption(2, new ScriptRef(FigtFile, 577)),
        ]);
        var choice = await ChooseAsync(menu);
        return choice == 1 ? FigtLabel.RetreatConfirmed : FigtLabel.PlayerTurn;
    }

    /// <summary><c>L_00dd3</c>, <c>FIGT.ADV:581-590</c>: the player's army retreats.</summary>
    internal async Task<FigtLabel> RetreatConfirmedAsync()
    {
        State.Bt_Result = 1;
        await FigtBoxAsync(585);
        return FigtLabel.AttackerLost;
    }

    /// <summary>
    /// <c>L_02d16</c>, <c>FIGT.ADV:1864-1872</c>: back to the strategic map (<c>M.ADV</c> with
    /// <c>CallArg = 0</c>) or, in a dungeon, clear the screen.
    /// </summary>
    internal async Task MapRedrawAsync()
    {
        if (State.Bt_Kind != 3)
            await FlowMapAsync(0);
        if (State.Bt_Kind == 3)
            await View.ShowAsync(new ClearScreenEvent());
    }

    /// <summary>Asks the input for a menu choice and rejects an item the menu does not list.</summary>
    private async Task<Sys3Int?> ChooseAsync(BattleMenu menu)
    {
        var choice = await Input.ChooseAsync(menu);
        if (!menu.Allows(choice))
            throw new InvalidOperationException($"Menu {menu.Kind} has no item {choice}.");
        return choice;
    }
}
