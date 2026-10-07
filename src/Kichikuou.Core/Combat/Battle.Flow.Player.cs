using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// The player turn of FIGT.ADV (docs/battle/01-flow.md §8.1-§8.4, §8.6, §8.7) and FIGT_SEL.ADV.
public sealed partial class Battle
{
    /// <summary>
    /// The panel rectangles of <c>L_0512e</c> (<c>FIGT.ADV:3267-3268</c>): x, y, w, h of buttons
    /// 0-3 (player slots) and 4-7 (enemy slots). <c>#L_05126, 1</c> is read as selecting the
    /// first sub-table (the alternative <c>L_0516e</c> differs only in pixels).
    /// </summary>
    private static readonly ushort[] PanelRects =
    [
        12, 8, 182, 84, 12, 108, 182, 84, 12, 208, 182, 84, 12, 308, 182, 84,
        448, 8, 182, 84, 448, 108, 182, 84, 448, 208, 182, 84, 448, 308, 182, 84,
    ];

    /// <summary>
    /// <c>L_00e0d</c>, <c>FIGT.ADV:595-623</c> (01 §8.1): highlights the acting slot, clears the
    /// special, loads the button rectangles, then either follows the standing order or derives
    /// the attack type from the class.
    /// </summary>
    private async Task<FigtLabel> PlayerTurnAsync()
    {
        State.CallArg = State.Bt_PlayerActSlot;
        await HighlightAsync();
        return PlayerTurnAfterHighlight();
    }

    /// <summary><c>FIGT.ADV:600-623</c>: the part of <c>L_00e0d</c> after the highlight.</summary>
    internal FigtLabel PlayerTurnAfterHighlight()
    {
        State.Bt_SpecialAtk = 0;
        LoadButtonRects();
        if (State.Bt_CurOrder != 0)
            return FigtLabel.AutoAction;
        State.Bt_PlayerAtkType = 0;
        if (Unit.Class == 1)
            State.Bt_PlayerAtkType = 1;
        if (Unit.Class == 2 || Unit.Class == 3 || Unit.Class == 11)
            State.Bt_PlayerAtkType = 2;
        return FigtLabel.Select;
    }

    /// <summary>
    /// <c>FIGT.ADV:601-613</c>: <c>#L_05126, 1</c> and the <c>F3</c> loop that copies the eight
    /// panel rectangles into the button records 0-7 (left at <c>Ui_BtnCur = 8</c>).
    /// </summary>
    internal void LoadButtonRects()
    {
        State.Ui_BtnCur = 0;
        do
        {
            var button = State.Buttons[State.Ui_BtnCur];
            int k = State.Ui_BtnCur.Value * 4;
            button.X1 = PanelRects[k + 0];
            button.Y1 = PanelRects[k + 1];
            button.W = PanelRects[k + 2];
            button.H = PanelRects[k + 3];
            button.X2 = button.X1 + button.W;
            button.Y2 = button.Y1 + button.H;
            State.Ui_BtnCur = State.Ui_BtnCur + 1;
        }
        while (State.Ui_BtnCur < 8);
    }

    /// <summary>
    /// <c>L_00ee3</c>, <c>FIGT.ADV:624-640</c> (01 §8.1): runs <c>FIGT_SEL.ADV</c> and binds the
    /// target: <c>Bt_EnemyActSlot := Tmp0001 - 3</c> for every command (Q-18).
    /// </summary>
    internal async Task<FigtLabel> SelectAsync()
    {
        State.VAR0617 = State.Bt_PlayerActSlot;
        await FigtSelAsync();
        // QUIRK: Q-18 — computed for every command, garbage (saturated) for non-attacks.
        State.Bt_EnemyActSlot = State.Tmp0001 - 3;
        if (IsSlot(State.Bt_EnemyActSlot, out int slot))
            State.Cmdr_Cur = ESlot(slot);
        FlowCopyString(4, CmdrNameString(State.Cmdr_Cur));
        return FigtLabel.Dispatch;
    }

    /// <summary>
    /// <c>FIGT_SEL.ADV</c> (01 §8.2): clears <c>Buf_Val[0..99]</c> and writes the unread command
    /// flags (Q-7), counts the used slots, then asks the player. Leaves the command in
    /// <c>RetVal</c> and the argument in <c>Tmp0001</c>.
    /// </summary>
    /// <remarks>
    /// Of the selection UI's own scratch state, <c>Tmp0002</c> (button under the cursor),
    /// <c>Tmp0019</c> (mode) and <c>Tmp0020</c> (highlight colour) are set to the values the
    /// script has at its return where the choice determines them; for popup commands that do not
    /// change the mode, <c>Tmp0019</c>/<c>Tmp0020</c> keep the entry values (the original keeps
    /// the last mode the player browsed — never read). Mouse coordinates, popup geometry
    /// (<c>Tmp0010-0015</c>) and <c>Ui_BtnCur</c> depend only on the pointer and are not modelled.
    /// </remarks>
    internal async Task FigtSelAsync()
    {
        State.Buf_Cur = 0;
        for (int i = 0; i < 100; i++)
            State.Buffer[State.Buf_Cur.Value + i].Val = 0;
        State.Bt_PlayerSlotsUsed = 0;
        for (int i = 1; i <= 4; i++)
        {
            if (PSlot(i) != 0)
                State.Bt_PlayerSlotsUsed = (Sys3Int)i;
        }
        if (State.Bt_PlayerSlotsUsed == 0)
        {
            State.RetVal = 0;
            return;
        }
        State.Bt_EnemySlotsUsed = 0;
        for (int i = 1; i <= 4; i++)
        {
            if (ESlot(i) != 0)
                State.Bt_EnemySlotsUsed = (Sys3Int)i;
        }
        if (State.Bt_EnemySlotsUsed == 0)
        {
            State.RetVal = 0;
            return;
        }
        // QUIRK: Q-7 — flags written to Buf_Val and never read.
        if (State.Bt_PlayerActSlot == 1 || State.Bt_PlayerActSlot == 2)
        {
            State.Buf_Cur = 1;
            State.Buffer[State.Buf_Cur].Val = 1;
        }
        if (State.Bt_PlayerAtkType == 1)
        {
            State.Buf_Cur = 2;
            State.Buffer[State.Buf_Cur].Val = 1;
        }
        if (State.Bt_PlayerAtkType == 2)
        {
            State.Buf_Cur = 3;
            State.Buffer[State.Buf_Cur].Val = 1;
        }
        State.Buf_Cur = 4;
        State.Buffer[State.Buf_Cur].Val = 1;
        State.Buf_Cur = 5;
        State.Buffer[State.Buf_Cur].Val = 1;
        if (State.Bt_IsDuel == 0)
        {
            State.Buf_Cur = 6;
            State.Buffer[State.Buf_Cur].Val = 1;
        }
        State.Tmp0002 = 98;
        State.Tmp0019 = State.Bt_PlayerAtkType + 1;
        State.Tmp0020 = 255;

        var menu = BuildFigtSelMenu();
        var choice = await Input.SelectCommandAsync(menu);
        if (!menu.Allows(choice))
            throw new InvalidOperationException(
                $"FIGT_SEL cannot return command {choice.Command} with argument {choice.Argument}.");
        ApplyFigtSelChoice(choice);
    }

    /// <summary>
    /// Everything <c>FIGT_SEL.ADV</c> lets the acting unit return (01 §8.2, 02 §3.2), from the
    /// current state: the targeting modes (<c>:522-697</c>, <c>:241</c>) with the targets that
    /// pass the validity test (<c>:163-171</c>), the command popup (<c>:241-278</c>) and the own
    /// slot popup / left click (<c>:295-372</c>).
    /// </summary>
    internal FigtSelMenu BuildFigtSelMenu()
    {
        var options = new List<FigtSelOption>();
        var hovered = new List<Sys3Int> { 99 };
        var enemySlots = new List<Sys3Int>();
        for (int s = 1; s <= State.Bt_EnemySlotsUsed.Value; s++)
        {
            hovered.Add((Sys3Int)(s + 3));
            enemySlots.Add((Sys3Int)s);
        }
        var ownSlots = new List<Sys3Int>();
        for (int s = 1; s <= State.Bt_PlayerSlotsUsed.Value; s++)
            ownSlots.Add((Sys3Int)s);

        void AddMode(ushort mode)
        {
            var targets = new List<Sys3Int>();
            for (int s = 1; s <= State.Bt_EnemySlotsUsed.Value; s++)
            {
                if (IsValidTarget(mode, (Sys3Int)(s + 3)))
                    targets.Add((Sys3Int)(s + 3));
            }
            options.Add(new FigtSelOption(mode, targets));
        }

        // Modes the player can switch to (the specials are listed while the mode is 1-3, and the
        // player can always switch back to Direct first, so they are always reachable).
        AddMode(FigtSelCommand.Direct);
        if (State.Bt_PlayerAtkType == 1)
            AddMode(FigtSelCommand.Bow);
        if (State.Bt_PlayerAtkType == 2)
            AddMode(FigtSelCommand.Magic);
        if (Unit.Item == 27)
            options.Add(new FigtSelOption(FigtSelCommand.Amoeba, hovered));
        if (Unit.Item == 26)
            options.Add(new FigtSelOption(FigtSelCommand.JagiJagi, hovered));
        if (State.Bt_IsDuel == 0 && Unit.Item == 28)
            options.Add(new FigtSelOption(FigtSelCommand.FogCard, hovered));
        if (State.Bt_ActFocus != 0)
        {
            if (State.Bt_PlayerActSlot == 1 || State.Bt_PlayerActSlot == 2)
            {
                // (Unit_Cur = 1) + (Unit_Cur = 31) * (F_Kentarou_State > 7): * binds tighter.
                if (State.Unit_Cur == 1 || (State.Unit_Cur == 31 && State.F_Kentarou_State > 7))
                    AddMode(FigtSelCommand.RanceAttack);
                if (State.Unit_Cur == 7)
                    AddMode(FigtSelCommand.ByLaWay);
            }
            if (State.Unit_Cur == 30)
                AddMode(FigtSelCommand.HighSpeed);
            if (State.Unit_Cur == 17 || State.Unit_Cur == 39)
                AddMode(FigtSelCommand.WhiteBeam);
            if (State.Unit_Cur == 38 || State.Unit_Cur == 40)
                AddMode(FigtSelCommand.BlackBeam);
            if (State.Unit_Cur == 32 && State.Bt_ActFocus > 2)
                AddMode(FigtSelCommand.VanishBomb);
            if (State.Unit_Cur == 27)
                AddMode(FigtSelCommand.WhirlwindShot);
            if (State.Unit_Cur == 18)
                AddMode(FigtSelCommand.WarDance);
            if (State.Unit_Cur == 11 && State.F_Asuka_State > 7)
                AddMode(FigtSelCommand.Chuhai);
            if (State.Unit_Cur == 37 && State.Bt_PlayerAtkType == 2 && State.Bt_ActFocus > 0)
                AddMode(FigtSelCommand.EpoLight);
            if (State.Unit_Cur == 47 && State.Bt_PlayerAtkType == 0 && State.Bt_ActFocus > 0)
                AddMode(FigtSelCommand.SoaringWave);
        }
        if (State.DebugMode == 1)
            AddMode(FigtSelCommand.CheaterBeam);
        options.Add(new FigtSelOption(FigtSelCommand.Defend, hovered));
        options.Add(new FigtSelOption(FigtSelCommand.Wait, hovered));
        options.Add(new FigtSelOption(FigtSelCommand.AutoAttack, hovered));
        options.Add(new FigtSelOption(FigtSelCommand.AutoDefend, hovered));
        options.Add(new FigtSelOption(FigtSelCommand.EnemyInfo, enemySlots));
        options.Add(new FigtSelOption(FigtSelCommand.Retreat, hovered));
        if (State.DebugMode == 1)
            options.Add(new FigtSelOption(FigtSelCommand.Repel, hovered));
        options.Add(new FigtSelOption(FigtSelCommand.UnitInfo, ownSlots));
        options.Add(new FigtSelOption(FigtSelCommand.OrderManual, ownSlots));
        options.Add(new FigtSelOption(FigtSelCommand.OrderAttack, ownSlots));
        options.Add(new FigtSelOption(FigtSelCommand.OrderDefend, ownSlots));
        options.RemoveAll(o => o.Arguments.Count == 0);
        return new FigtSelMenu(State.Bt_PlayerActSlot, State.Bt_PlayerAtkType, State.Bt_PlayerAtkType + 1,
            State.Bt_PlayerSlotsUsed, State.Bt_EnemySlotsUsed, options);
    }

    /// <summary>
    /// <c>FIGT_SEL.ADV:163-171</c>: an enemy button is an invalid target when
    /// (it is enemy slot 3-4 or the actor is in slot 3-4) and (the mode is Direct, or a special
    /// > 10 of a melee unit). Bow, Magic and the Cheater Beam reach every slot.
    /// </summary>
    internal bool IsValidTarget(Sys3Int mode, Sys3Int button)
    {
        bool far = button > 5 || State.Bt_PlayerActSlot > 2;
        bool restricted = (mode != 2 && mode != 3 && mode < 10) || (mode > 10 && State.Bt_PlayerAtkType == 0);
        return !(far && restricted);
    }

    /// <summary>
    /// The state <c>FIGT_SEL.ADV</c> leaves at its return for a chosen pair (<c>L_00af4</c>):
    /// <c>RetVal</c>, <c>Tmp0001</c>, and the hover / mode variables they determine.
    /// </summary>
    private void ApplyFigtSelChoice(FigtSelChoice choice)
    {
        var command = choice.Command;
        State.RetVal = command;
        State.Tmp0001 = choice.Argument;
        if (command <= 3 || (command >= 10 && command <= 21))
        {
            // Committed by clicking target button Tmp0001 in mode Tmp0019 (:192-202, L_00981).
            State.Tmp0002 = choice.Argument;
            State.Tmp0019 = command;
            State.Tmp0020 = command <= 3 ? (Sys3Int)255 : (Sys3Int)249;
        }
        else if (command >= 50 && command <= 53)
        {
            // Own panel: Tmp0002 = button, Tmp0001 = button + 1 (:192-194, :356-373).
            State.Tmp0002 = choice.Argument - 1;
        }
        else if (command == 54)
        {
            // Info from the command popup: Tmp0001 := Tmp0002 + 1 - 4 (:374-376).
            State.Tmp0002 = choice.Argument + 3;
        }
        else
        {
            // Command popup: Tmp0001 = Tmp0002 = button under the cursor at the right click.
            State.Tmp0002 = choice.Argument;
            if (command >= 30 && command <= 32)
            {
                State.Tmp0019 = command;   // L_00abb / L_00ace / L_00ae1
                State.Tmp0020 = 249;
            }
        }
    }

    /// <summary>
    /// <c>L_00f55</c>, <c>FIGT.ADV:641-775</c> (01 §8.3): dispatch on the command in <c>RetVal</c>.
    /// Attack commands go to R1's <see cref="PlayerAttackAsync"/>, which ends in <c>Resolve</c> →
    /// <c>@L_007ac</c>; here it returns and the loop continues at <see cref="FigtLabel.Step"/>.
    /// </summary>
    internal async Task<FigtLabel> DispatchAsync()
    {
        var command = State.RetVal.Value;
        switch (command)
        {
            case 0:
                return await AttackRoutineErrorAsync();
            case >= 1 and <= 3:
            case >= 10 and <= 21:
                await PlayerAttackAsync();
                return FigtLabel.Step;
            case 4:
                return FigtLabel.Defend;
            case 5:
                return await FocusAsync();
            case 6:
                return FigtLabel.PlayerRetreat;
            case 7:
                return FigtLabel.DefenderWon;
            case 8:
                return await OrderSelfAsync(1, 1319);
            case 9:
                return await OrderSelfAsync(2, 1333);
            case 30:
                SummonKaiju(amoeba: true);
                return FigtLabel.PlayerTurn;
            case 31:
                SummonKaiju(amoeba: false);
                return FigtLabel.PlayerTurn;
            case 32:
                return await FogCardAsync();
            case 50:
            case 51:
            case 52:
                if (IsSlot(State.Tmp0001, out int slot))
                    POrder(slot) = (Sys3Int)(command - 50);
                return await OrderOtherAsync();
            case 53:
                await UnitInfoAsync();
                return FigtLabel.Select;
            case 54:
                await EnemyInfoAsync();
                return FigtLabel.Select;
            default:
                // FIGT.ADV:770-775 — "FIGT command error" box (L_05064, op 141).
                await FlowWindowAsync(141);
                await View.ShowAsync(new FlowErrorEvent(new ScriptRef(FigtFile, 772)));
                await FlowWaitKeyAsync();
                await FlowWindowAsync(10);
                return FigtLabel.PlayerTurn;
        }
    }

    /// <summary><c>L_01dae</c>, <c>FIGT.ADV:1257-1264</c>: "attack routine error" box (<c>A</c> waits), back to the turn.</summary>
    private async Task<FigtLabel> AttackRoutineErrorAsync()
    {
        await View.ShowAsync(new FlowErrorEvent(new ScriptRef(FigtFile, 1260)));
        _ = await Input.WaitForKeyAsync();
        return FigtLabel.PlayerTurn;
    }

    /// <summary>
    /// <c>L_0129e</c>, <c>FIGT.ADV:776-833</c> (01 §8.3): after an order for slot <c>Tmp0001</c>;
    /// an auto order for the acting unit itself acts at once (8/9 → dispatch), otherwise a
    /// message and back to the selection (the turn is not consumed).
    /// </summary>
    internal async Task<FigtLabel> OrderOtherAsync()
    {
        if (IsSlot(State.Tmp0001, out int slot))
            State.Tmp0002 = PSlot(slot);
        if (State.Tmp0002 == State.Unit_Cur)
        {
            if (State.RetVal == 51)
            {
                State.RetVal = 8;
                return FigtLabel.Dispatch;
            }
            if (State.RetVal == 52)
            {
                State.RetVal = 9;
                return FigtLabel.Dispatch;
            }
        }
        await FlowWindowAsync(143);
        await FlowTextAsync(FigtFile, 801);
        if (State.RetVal == 50)
        {
            await FlowTextAsync(FigtFile, 805);
            await FlowWaitKeyAsync();
        }
        if (State.RetVal == 51)
        {
            await FlowTextAsync(FigtFile, 814);
            await FlowWaitKeyAsync();
        }
        if (State.RetVal == 52)
        {
            await FlowTextAsync(FigtFile, 821);
            await FlowWaitKeyAsync();
        }
        await FlowWindowAsync(10);
        // :828-832 — IM loop until the mouse button is released.
        State.RetVal = 0;
        return FigtLabel.Select;
    }

    /// <summary>
    /// <c>L_013e0</c>, <c>FIGT.ADV:834-1020</c>: info box of the unit in slot <c>Tmp0001</c>
    /// (display only; <c>Unit_Cur</c> saved and restored through <c>Tmp0020</c>, the order in
    /// <c>Tmp0002</c>).
    /// </summary>
    internal async Task UnitInfoAsync()
    {
        State.Tmp0020 = State.Unit_Cur;
        if (IsSlot(State.Tmp0001, out int slot))
        {
            State.Unit_Cur = PSlot(slot);
            State.Tmp0002 = POrder(slot);
        }
        await FlowWindowAsync(143);
        await View.ShowAsync(new UnitInfoEvent(State.Unit_Cur, State.Tmp0002));
        State.RetVal = 0;   // :1005-1009 IM release loop
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
        State.Unit_Cur = State.Tmp0020;
        State.RetVal = 0;   // :1015-1019 IM release loop
    }

    /// <summary>
    /// <c>L_01878</c>, <c>FIGT.ADV:1021-1119</c>: info box of the commander in enemy slot
    /// <c>Tmp0001</c> (<c>Cmdr_Cur</c> saved and restored through <c>Tmp0020</c>).
    /// </summary>
    internal async Task EnemyInfoAsync()
    {
        State.Tmp0020 = State.Cmdr_Cur;
        if (IsSlot(State.Tmp0001, out int slot))
            State.Cmdr_Cur = ESlot(slot);
        await FlowWindowAsync(143);
        await View.ShowAsync(new EnemyInfoEvent(State.Cmdr_Cur));
        State.RetVal = 0;
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
        State.Cmdr_Cur = State.Tmp0020;
        State.RetVal = 0;
    }

    /// <summary>
    /// <c>L_01b68</c> / <c>L_01c53</c>, <c>FIGT.ADV:1149-1232</c> (01 §8.6): rebuilds unit
    /// record 51 as the capsule kaiju Amoeba or Jagi Jagi and hands it the turn. The capsule is
    /// not consumed; the kaiju acts from the summoner's slot (Q-19).
    /// </summary>
    internal void SummonKaiju(bool amoeba)
    {
        FlowSetString(1, amoeba ? "Amoeba" : "Jagi Jagi");
        FlowSetString(2, "　");
        State.Unit_Cur = 51;
        FlowCopyString(UnitNameString(State.Unit_Cur) + 0, 1);
        FlowCopyString(UnitNameString(State.Unit_Cur) + 1, 2);
        Sys3Int cg = amoeba ? (Sys3Int)1161 : (Sys3Int)1158;
        Unit.CgFull = cg;
        Unit.CgPartial = cg;
        Unit.CgAlone = cg;
        Unit.FaceCg = 0;
        Unit.Status = 1;
        Unit.Troops = 0;
        Unit.MaxTroops = 0;
        Unit.TroopAtk = 0;
        Unit.TroopDef = 0;
        Unit.OffAtkMod = 0;
        Unit.OffDefMod = 0;
        Unit.DefAtkMod = 0;
        Unit.DefDefMod = 0;
        Unit.DgnAtkMod = 0;
        Unit.DgnDefMod = 0;
        Unit.PortraitCg = 0;
        Unit.Class = 0;
        Unit.Recovery = 0;
        Unit.DeployCost = 0;
        if (amoeba)
        {
            Unit.Atk = 20;
            Unit.Def = 10;
            Unit.Attacks = State.Kaiju_AmoebaAttacks;
            State.Kaiju_AmoebaAttacks = 100;
        }
        else
        {
            Unit.Atk = 15;
            Unit.Def = 13;
            Unit.Attacks = State.Kaiju_JagiJagiAttacks;
            State.Kaiju_JagiJagiAttacks = 80;
        }
        Unit.HP = 18;
        Unit.MaxHP = 18;
        Unit.Strategy = 0;
        Unit.Magic = amoeba ? Sys3Int.Zero : (Sys3Int)8;
        Unit.Item = 0;
        Unit.CurAtkMod = 0;
        Unit.CurDefMod = 0;
        Unit.Partner = 2;
        Unit.Exp = 0;
        Unit.Level = 0;
    }

    /// <summary>
    /// <c>L_01d41</c>, <c>FIGT.ADV:1233-1256</c> (01 §8.7): the Fog Card ends the battle at
    /// the end of this round (rounds left 0), is unequipped and lost, and its price rises 10 %.
    /// </summary>
    private async Task<FigtLabel> FogCardAsync()
    {
        await FogCardEffectAsync();
        await RedrawAsync();
        return FigtLabel.Step;
    }

    /// <summary><c>FIGT.ADV:1234-1254</c>: the Fog Card's effect and message, before the redraw.</summary>
    internal async Task FogCardEffectAsync()
    {
        State.Bt_RoundsLeft = 0;
        Unit.Item = 0;
        State.Item_Cur = 28;
        Item.Holder = 0;
        State.FogCard_Price = State.FogCard_Price + State.FogCard_Price / 10;
        await FlowWindowAsync(143);
        await FlowTextAsync(FigtFile, 1241);
        await FlowWaitKeyAsync();
        await FlowTextAsync(FigtFile, 1244);
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
    }

    /// <summary>
    /// <c>L_01e34</c>, <c>FIGT.ADV:1279-1314</c> (01 §8.3): Wait — the acting slot gains one
    /// focus charge; Miki (32) fails on <c>RND(2) = 1</c> [R8].
    /// </summary>
    private async Task<FigtLabel> FocusAsync()
    {
        await FocusEffectAsync();
        await RedrawAsync();
        return FigtLabel.Step;
    }

    /// <summary><c>FIGT.ADV:1280-1312</c>: the part of <c>L_01e34</c> before its redraw.</summary>
    internal async Task FocusEffectAsync()
    {
        if (State.Unit_Cur == 32)
        {
            Rnd(2);
            if (State.RetVal == 1)
            {
                await FigtBoxAsync(1285);
                return;
            }
        }
        if (IsSlot(State.Bt_PlayerActSlot, out int slot))
            PFocus(slot) = PFocus(slot) + 1;
        await FigtBoxAsync(1308);
    }

    /// <summary>
    /// <c>L_01f08</c> / <c>L_01f42</c> + <c>L_01f7c</c>, <c>FIGT.ADV:1315-1356</c> (01 §8.4):
    /// commands 8/9 give the acting slot a standing order and act on it now.
    /// </summary>
    internal async Task<FigtLabel> OrderSelfAsync(Sys3Int order, int line)
    {
        State.Bt_CurOrder = order;
        await FlowWindowAsync(143);
        await FlowTextAsync(FigtFile, line);
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
        if (IsSlot(State.Bt_PlayerActSlot, out int slot))
            POrder(slot) = State.Bt_CurOrder;
        return FigtLabel.AutoAction;
    }

    /// <summary>
    /// <c>L_01fd5</c>, <c>FIGT.ADV:1357-1381</c> (01 §8.4): standing orders. Auto attack always
    /// targets enemy slot 1 and skips <c>ManualAttackPrelude</c> (no R6, Q-16); melee units in
    /// the back row and class 4 defend instead.
    /// </summary>
    private async Task<FigtLabel> AutoActionAsync()
    {
        switch (AutoActionSetup())
        {
            case AutoKind.Resolve:
                await ResolveAsync();
                return FigtLabel.Step;
            case AutoKind.ResolveMagic:
                await ResolveMagicAsync();
                return FigtLabel.Step;
            default:
                return FigtLabel.Defend;
        }
    }

    /// <summary>How an auto action or enemy turn continues.</summary>
    internal enum AutoKind
    {
        /// <summary>Jump to <c>L_02078</c> (defend) / <c>L_0232f</c> (idle).</summary>
        None,

        /// <summary>Jump to <c>Resolve</c> (<c>L_025d7</c>).</summary>
        Resolve,

        /// <summary>Jump to <c>ResolveMagic</c> (<c>L_025a0</c>).</summary>
        ResolveMagic,
    }

    /// <summary><c>FIGT.ADV:1358-1381</c>: sets the exchange inputs of a standing order.</summary>
    internal AutoKind AutoActionSetup()
    {
        if (State.Bt_CurOrder == 2)
            return AutoKind.None;
        State.Bt_EnemyActSlot = 1;
        State.Cmdr_Cur = State.Bt_EnemySlot1;
        if ((Unit.Class == 0 || Unit.Class == 10) && State.Bt_PlayerActSlot < 3)
        {
            State.Bt_MsgMode = 3;
            State.Bt_EnemyAction = 0;
            State.Bt_PlayerAction = 0;
            return AutoKind.Resolve;
        }
        if (Unit.Class == 1)
        {
            State.Bt_MsgMode = 3;
            State.Bt_EnemyAction = 99;
            State.Bt_PlayerAction = 1;
            return AutoKind.Resolve;
        }
        if (Unit.Class == 2 || Unit.Class == 11 || Unit.Class == 3)
        {
            State.Bt_MsgMode = 1;
            State.Bt_EnemyAction = 99;
            State.Bt_PlayerAction = 2;
            return AutoKind.ResolveMagic;
        }
        return AutoKind.None;
    }

    /// <summary>
    /// <c>L_02078</c>, <c>FIGT.ADV:1382-1410</c> (01 §8.3): Defend — the acting slot's state
    /// becomes <c>RND(20) + 1</c> [R7] (2..21), the enemy's hit chance against it drops by that.
    /// </summary>
    private async Task<FigtLabel> DefendAsync()
    {
        await DefendEffectAsync();
        await RedrawAsync();
        return FigtLabel.Step;
    }

    /// <summary><c>FIGT.ADV:1383-1408</c>: the part of <c>L_02078</c> before its redraw.</summary>
    internal async Task DefendEffectAsync()
    {
        Rnd(20);
        State.RetVal = State.RetVal + 1;
        if (IsSlot(State.Bt_PlayerActSlot, out int slot))
            PState(slot) = State.RetVal;
        await FlowWindowAsync(143);
        await FlowValueTextAsync(FigtFile, 1399, State.RetVal);
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
    }
}
