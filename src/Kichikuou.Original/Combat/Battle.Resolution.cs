using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Combat;

// Part R1: the resolution of one exchange (docs/battle/02-resolution.md §1-4, §6, §7, §9).
// Line numbers are FIGT.ADV unless another file is named.
public sealed partial class Battle
{
    // ------------------------------------------------------------------------------------
    // 02 §2.1 — attack command setters, SpendFocus, ManualAttackPrelude
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// The attack results of the command dispatch (<c>FIGT.ADV:645-707</c>): <c>RetVal</c> holds
    /// the FIGT_SEL command (1-3, 10-21); jumps to its setter (<see cref="SetAttackCommand"/>), which
    /// continues into <see cref="ManualAttackPreludeAsync"/> and <c>Resolve</c> /
    /// <c>ResolveMagic</c>. Spec 02 §2.1.
    /// </summary>
    internal async Task PlayerAttackAsync()
    {
        SetAttackCommand();
        await ManualAttackPreludeAsync();
    }

    /// <summary>
    /// The setter of the attack command in <c>RetVal</c> (1-3, 10-21; <c>FIGT.ADV:1492-1585</c>): the exchange's
    /// actions and special, the Focus it spends. Shared by <see cref="PlayerAttackAsync"/> and the damage forecast
    /// (<see cref="ExchangeForecast"/>). Spec 02 §2.1.
    /// </summary>
    internal void SetAttackCommand()
    {
        switch ((int)State.RetVal)
        {
            case 1: SetDirectAttack(); break;
            case 2: SetBowAttack(); break;
            case 3: SetMagicAttack(); break;
            case 10: SetCheaterBeam(); break;
            case 11: SetRanceAttack(); break;
            case 12: SetByLaWay(); break;
            case 13 or 17 or 21: SetDestructionBeam(); break;
            case 14 or 18: SetHighSpeed(); break;
            case 15: SetVanishBomb(); break;
            case 16: SetWarDance(); break;
            case 19: SetChuhai(); break;
            case 20: SetSoaringWave(); break;
            default:
                throw new InvalidOperationException($"RetVal {State.RetVal} is not an attack command.");
        }
    }

    /// <summary><c>L_02339</c> (<c>FIGT.ADV:1492-1496</c>) — Direct attack; the enemy counters. 02 §2.1.</summary>
    private void SetDirectAttack()
    {
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 0;
        State.Bt_SkillSfx = 0;
    }

    /// <summary><c>L_0234d</c> (<c>FIGT.ADV:1497-1501</c>) — Bow (indirect) attack. 02 §2.1.</summary>
    private void SetBowAttack()
    {
        State.Bt_PlayerAction = 1;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 0;
    }

    /// <summary><c>L_02362</c> (<c>FIGT.ADV:1502-1506</c>) — Magic attack. 02 §2.1.</summary>
    private void SetMagicAttack()
    {
        State.Bt_PlayerAction = 2;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 0;
    }

    /// <summary><c>L_02377</c> (<c>FIGT.ADV:1507-1515</c>) — Rance Attack, special 1. 02 §2.1, §3.3.</summary>
    private void SetRanceAttack()
    {
        // QUIRK: 02 Q19 — the special's name goes to string 7, which the battle never prints.
        State.Strings[7] = "Rance Attack";
        SpendFocus();
        State.Bt_SpecialAtk = 1;
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 1;
    }

    /// <summary><c>L_023ab</c> (<c>FIGT.ADV:1516-1524</c>) — By la Way, special 1. 02 §2.1, §3.3.</summary>
    private void SetByLaWay()
    {
        State.Strings[7] = "By la Way";
        SpendFocus();
        State.Bt_SpecialAtk = 1;
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 2;
    }

    /// <summary>
    /// <c>L_023dc</c> (<c>FIGT.ADV:1525-1531</c>) — T2 Soaring Wave, special 7; the only special
    /// the enemy counters (<c>Bt_EnemyAction = 0</c>). 02 §2.1, §3.3.
    /// </summary>
    private void SetSoaringWave()
    {
        SpendFocus();
        State.Bt_SpecialAtk = 7;
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 0;
        State.Bt_SkillSfx = 3;
    }

    /// <summary>
    /// <c>L_023fa</c> (<c>FIGT.ADV:1532-1541</c>) — White/Black Destruction Beam and EPO Light,
    /// special 2. 02 §2.1, §3.3.
    /// </summary>
    private void SetDestructionBeam()
    {
        State.Strings[7] = "White Destruction Beam";
        // QUIRK: 02 Q6 — listed with 1 Focus, spends 2 (saturating at 0).
        SpendFocus();
        SpendFocus();
        State.Bt_SpecialAtk = 2;
        State.Bt_PlayerAction = 2;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 4;
    }

    /// <summary><c>L_0243d</c> (<c>FIGT.ADV:1542-1550</c>) — High Speed / Whirlwind Shot, special 3. 02 §2.1, §3.3.</summary>
    private void SetHighSpeed()
    {
        State.Strings[7] = "Highspeed";
        SpendFocus();
        State.Bt_SpecialAtk = 3;
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 5;
    }

    /// <summary><c>L_0246e</c> (<c>FIGT.ADV:1551-1559</c>) — Chuhai, special 6. 02 §2.1, §3.3.</summary>
    private void SetChuhai()
    {
        State.Strings[7] = "Chuhai";
        SpendFocus();
        State.Bt_SpecialAtk = 6;
        State.Bt_PlayerAction = 2;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 6;
    }

    /// <summary>
    /// <c>L_0249c</c> (<c>FIGT.ADV:1560-1570</c>) — Vanish Bomb, special 4; also Miki's involuntary
    /// conversion from <see cref="ManualAttackPreludeAsync"/>. 02 §2.1, §3.3.
    /// </summary>
    private void SetVanishBomb()
    {
        State.Strings[7] = "Vanish Bomb";
        SpendFocus();
        SpendFocus();
        SpendFocus();
        State.Bt_SpecialAtk = 4;
        State.Bt_PlayerAction = 2;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 7;
    }

    /// <summary><c>L_024d9</c> (<c>FIGT.ADV:1571-1577</c>) — Crazy War Dance, special 5. 02 §2.1, §3.3.</summary>
    private void SetWarDance()
    {
        SpendFocus();
        State.Bt_SpecialAtk = 5;
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 8;
    }

    /// <summary><c>L_024f8</c> (<c>FIGT.ADV:1578-1585</c>) — Cheater Beam (debug), special 100; no Focus. 02 §2.1, §3.3.</summary>
    private void SetCheaterBeam()
    {
        State.Strings[7] = "Cheater Beam";
        State.Bt_SpecialAtk = 100;
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 99;
        State.Bt_SkillSfx = 9;
    }

    /// <summary>
    /// <c>L_01dd3</c> (<c>FIGT.ADV:1265-1278</c>) — spends one Focus charge of the acting slot
    /// <c>Bt_PlayerActSlot</c>, saturating at 0. 02 §2.1, §3.1.
    /// </summary>
    internal void SpendFocus()
    {
        if (State.Bt_PlayerActSlot == 1)
            State.Bt_PSlot1Focus = State.Bt_PSlot1Focus - 1;
        if (State.Bt_PlayerActSlot == 2)
            State.Bt_PSlot2Focus = State.Bt_PSlot2Focus - 1;
        if (State.Bt_PlayerActSlot == 3)
            State.Bt_PSlot3Focus = State.Bt_PSlot3Focus - 1;
        if (State.Bt_PlayerActSlot == 4)
            State.Bt_PSlot4Focus = State.Bt_PSlot4Focus - 1;
    }

    /// <summary>
    /// <c>L_02528</c> (<c>FIGT.ADV:1586-1604</c>) — the Miki roll, the target highlight,
    /// <c>Bt_MsgMode = 3</c>, then <c>Resolve</c> or <c>ResolveMagic</c>. 02 §2.1.
    /// </summary>
    private async Task ManualAttackPreludeAsync()
    {
        while (true)
        {
            Rnd(10); // 1587 [R6]
            if (State.RetVal == 1 && State.Unit_Cur == 32)
            {
                // QUIRK: 02 Q7 — Miki turns any manual attack into Vanish Bomb 1 time in 10,
                // spending 3 Focus each time (saturating) and rolling again (@L_0249c → L_02528).
                SetVanishBomb();
                continue;
            }

            break;
        }

        State.CallArg = State.Bt_EnemyActSlot + 4; // 1592
        await HighlightAsync(); // 1593
        State.Bt_MsgMode = 3; // 1595
        if (State.Bt_PlayerAction == 0)
        {
            await ResolveAsync();
            return;
        }

        if (State.Bt_PlayerAction == 1)
        {
            await ResolveAsync();
            return;
        }

        // 1602: action 2 jumps to L_025a0; any other value falls through into the same label.
        await ResolveMagicAsync();
    }

    // ------------------------------------------------------------------------------------
    // 02 §1.1 — Resolve, ResolveMagic, GoliathStrike
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_025a0</c> (<c>FIGT.ADV:1605-1614</c>) — <c>Bt_MsgMode = 1</c>, then the body of
    /// <see cref="ResolveAsync"/> (the two labels hold identical copies). The script then jumps to
    /// the actor loop <c>L_007ac</c>, which is the caller's job. 02 §1.1.
    /// </summary>
    internal async Task ResolveMagicAsync()
    {
        State.Bt_MsgMode = 1;
        await ResolveAsync();
    }

    /// <summary>
    /// <c>L_025d7</c> (<c>FIGT.ADV:1615-1623</c>) — one exchange: <see cref="PresentEngagementAsync"/>,
    /// <see cref="ComputeExchange"/>, <see cref="ApplyExchangeAsync"/>, the retreat flag and the
    /// battlefield redraw. The script then jumps to <c>L_007ac</c> (caller's job). 02 §1.1.
    /// </summary>
    internal async Task ResolveAsync()
    {
        await PresentEngagementAsync();
        ComputeExchange();
        await ApplyExchangeAsync();
        if (State.Bt_EnemyRetreatMode == 20 || State.Bt_EnemyRetreatMode == 50)
            State.Bt_EnemyRetreat = 3;
        await RedrawAsync();
    }

    /// <summary>
    /// <c>L_01abd</c> (<c>FIGT.ADV:1120-1148</c>) — the T2 Goliath strikes enemy slot 1 as
    /// special 10 through unit record 51, then the battlefield is redrawn (no retreat-flag check,
    /// 02 Q25). The script then jumps to <c>L_007ac</c> (caller's job). 02 §3.4.
    /// </summary>
    internal async Task GoliathStrikeAsync()
    {
        PrepareGoliath();
        await PresentEngagementAsync();
        ComputeExchange();
        await ApplyExchangeAsync();
        // QUIRK: 02 Q25 — no Bt_EnemyRetreatMode 20/50 → Bt_EnemyRetreat check on this path.
        await RedrawAsync();
    }

    /// <summary>
    /// State part of <c>L_01abd</c> before the exchange (<c>FIGT.ADV:1121-1143</c>): builds unit
    /// record 51 as the T2 Goliath (only these fields; the rest stays stale, 02 §1.4) and sets
    /// the exchange inputs. 02 §3.4.
    /// </summary>
    internal void PrepareGoliath()
    {
        State.Goliath_State = 2;
        State.Strings[1] = "T2 Goliath";
        State.Strings[2] = "　";
        State.Unit_Cur = 51;
        CopyString(UnitString(0), 1);
        CopyString(UnitString(1), 2);
        Unit.CgFull = 0;
        Unit.CgPartial = 0;
        Unit.CgAlone = 0;
        Unit.FaceCg = 0;
        Unit.Status = 1;
        Unit.Troops = 0;
        Unit.MaxTroops = 0;
        Unit.HP = 0;
        Unit.MaxHP = 0;
        State.Bt_PlayerActSlot = 1;
        State.Bt_EnemyActSlot = 1;
        State.Cmdr_Cur = State.Bt_EnemySlot1;
        State.Bt_SpecialAtk = 10;
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 99;
        // QUIRK: 02 Q11 — Bt_MsgMode is not set: the flavour text follows the previous exchange.
    }

    // ------------------------------------------------------------------------------------
    // 02 §4 — PresentEngagement
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_02609</c> (<c>FIGT.ADV:1624-1829</c>) — shows the duel scene, copies the situational
    /// troop modifiers into <c>Unit_CurAtkMod</c>/<c>Unit_CurDefMod</c>, runs the flavour text
    /// (<c>FIGT_M.ADV</c>, with commander 200 in story stage 4) and slides the window in.
    /// 02 §4, 01 §9.1.
    /// </summary>
    internal async Task PresentEngagementAsync()
    {
        await ShowEngagementSceneAsync();

        // 1726: MM 4, Cmdr_StrBase + (Cmdr_Cur - 1) * 3
        CopyString(4, CmdrString(0));
        CopyBattleKindMods(); // 1727-1738
        await WaoAsync(143); // L_0509c, 1739
        await View.ShowAsync(new ActionLabelsEvent(PlayerActionLabel(), EnemyActionLabel()));
        // QUIRK: BSC-07 — IK 0 takes a button still held: after a target clicked (FIGT_SEL acts on
        // the press) the window is gone in a frame; otherwise it waits for a click.
        await WaoAsync(11); // L_05119, 1801
        await WaoAsync(10); // L_0510c, 1802

        if (State.StoryStage == 4)
        {
            // QUIRK: 01 Q-22 — the flavour text of story stage 4 always speaks as commander 200.
            State.Tmp0229 = State.Cmdr_Cur;
            State.Cmdr_Cur = 200;
        }

        // QUIRK: FIGT.ADV:1803-1810 — the first counters run after the swap, so in story stage 4
        // they read (and draw) commander 200's troops and HP.
        CountCgLoad(888); // 1807-1808 J1 640, 400: G 888
        await SlideEngagementAsync(112); // 1807-1811
        await FlavourTextAsync(); // 1812 %#FIGT_M.ADV
        if (State.StoryStage == 4)
            State.Cmdr_Cur = State.Tmp0229;

        await SlideEngagementAsync(84); // 1816-1818
        await SlideEngagementAsync(56); // 1819-1821
        await SlideEngagementAsync(28); // 1822-1824
        await SlideEngagementAsync(0); // 1825-1827
    }

    /// <summary>
    /// <c>FIGT.ADV:1727-1738</c> (and <c>2103-2114</c> in the slot panel) — the situational troop modifiers of the battle's
    /// kind (<c>Bt_Kind</c>: attack, defence, dungeon) copied into <c>Unit_CurAtkMod</c>/<c>Unit_CurDefMod</c>. 02 §4, 01 §9.4.
    /// </summary>
    internal void CopyBattleKindMods()
    {
        if (State.Bt_Kind == 1)
        {
            Unit.CurAtkMod = Unit.OffAtkMod;
            Unit.CurDefMod = Unit.OffDefMod;
        }

        if (State.Bt_Kind == 2)
        {
            Unit.CurAtkMod = Unit.DefAtkMod;
            Unit.CurDefMod = Unit.DefDefMod;
        }

        if (State.Bt_Kind == 3)
        {
            Unit.CurAtkMod = Unit.DgnAtkMod;
            Unit.CurDefMod = Unit.DgnDefMod;
        }
    }

    /// <summary>
    /// <c>FIGT.ADV:1625-1724</c> — the duel scene: background, enemy and player sprites (by troops
    /// versus <c>Bt_TroopLimit</c>, or the special-attack CG), overlay and faces. Presentation
    /// only. 02 §4.
    /// </summary>
    private async Task ShowEngagementSceneAsync()
    {
        var enemy = new List<Sys3Int>();
        if (Cmdr.Troops > State.Bt_TroopLimit || Cmdr.Troops == State.Bt_TroopLimit)
            enemy.Add(Cmdr.CgFull);
        if (Cmdr.Troops < State.Bt_TroopLimit && Cmdr.Troops != 0)
            enemy.Add(Cmdr.CgPartial);
        if (Cmdr.Troops == 0)
            enemy.Add(Cmdr.CgAlone);

        var player = new List<Sys3Int>();
        if (State.Bt_SpecialAtk == 0)
        {
            // Ours (decision 32): the holder our fix sends in person is shown with his troops (BRS-24).
            bool slayer = DemonSlayerInPerson;
            if ((Unit.Troops > State.Bt_TroopLimit || Unit.Troops == State.Bt_TroopLimit) && !slayer)
                player.Add(Unit.CgFull);
            if ((Unit.Troops < State.Bt_TroopLimit || slayer) && Unit.Troops != 0)
                player.Add(Unit.CgPartial);
            if (Unit.Troops == 0)
                player.Add(Unit.CgAlone);
        }

        if (State.Bt_SpecialAtk != 0)
        {
            // 1660-1713: each test is independent.
            foreach (var (unit, cg) in SpecialAttackSprites)
            {
                if (State.Unit_Cur == unit)
                    player.Add(cg);
            }

            foreach (var ninjaCg in NinjaSprites)
            {
                if (Unit.CgFull == ninjaCg)
                    player.Add(ninjaCg);
            }
        }

        // QUIRK: 01 §14 open question 3 — a commander id is never 1137, so the overlay is always drawn.
        bool overlay = State.Bt_EnemyParty1 != 1137;
        CountCgLoad(899);                       // 1629
        CountCgLoad(State.Bt_BackgroundCg);     // 1631
        foreach (var cg in enemy)
            CountCgLoad(cg);
        foreach (var cg in player)
            CountCgLoad(cg);
        if (overlay)
            CountCgLoad(State.Bt_BackgroundCg + 1);     // 1717
        CountCgLoad(Unit.FaceCg);               // 1720
        if (Cmdr.FaceCg != 0)
            CountCgLoad(Cmdr.FaceCg);           // 1723
        await View.ShowAsync(new EngagementSceneEvent(
            State.Bt_BackgroundCg, enemy, player, overlay, Unit.FaceCg, Cmdr.FaceCg));
    }

    /// <summary>Unit → special-attack CG of <c>FIGT.ADV:1660-1701</c>, in test order.</summary>
    private static readonly (ushort Unit, ushort Cg)[] SpecialAttackSprites =
    [
        (1, 1520), (7, 1521), (11, 1121), (17, 1522), (39, 1523), (37, 1524), (40, 1525),
        (38, 1526), (30, 1527), (27, 1528), (31, 1222), (32, 1529), (18, 1530), (47, 1252),
    ];

    /// <summary>The four ninja CGs tested at <c>FIGT.ADV:1702-1713</c>.</summary>
    private static readonly ushort[] NinjaSprites = [1164, 1176, 1182, 1185];

    /// <summary>The player action label line shown at <c>FIGT.ADV:1746-1767</c>.</summary>
    private ScriptRef? PlayerActionLabel()
    {
        if (State.Bt_SpecialAtk != 0)
            return new ScriptRef("FIGT.ADV", 1748);
        if (State.Bt_PlayerAction == 0)
            return new ScriptRef("FIGT.ADV", 1753);
        if (State.Bt_PlayerAction == 1)
            return new ScriptRef("FIGT.ADV", 1757);
        if (State.Bt_PlayerAction == 2)
            return new ScriptRef("FIGT.ADV", 1761);
        if (State.Bt_PlayerAction == 99)
            return new ScriptRef("FIGT.ADV", 1765);
        return null;
    }

    /// <summary>The enemy action label line shown at <c>FIGT.ADV:1779-1800</c> (blank for a special).</summary>
    private ScriptRef? EnemyActionLabel()
    {
        if (State.Bt_SpecialAtk != 0)
            return new ScriptRef("FIGT.ADV", 1781);
        if (State.Bt_EnemyAction == 0)
            return new ScriptRef("FIGT.ADV", 1786);
        if (State.Bt_EnemyAction == 1)
            return new ScriptRef("FIGT.ADV", 1790);
        if (State.Bt_EnemyAction == 2)
            return new ScriptRef("FIGT.ADV", 1794);
        if (State.Bt_EnemyAction == 99)
            return new ScriptRef("FIGT.ADV", 1798);
        return null;
    }

    /// <summary>
    /// One <c>CC</c> step of the duel window followed by <c>L_02c00</c>
    /// (<c>FIGT.ADV:1809-1827</c>). 02 §4.
    /// </summary>
    private async Task SlideEngagementAsync(Sys3Int offset)
    {
        await View.ShowAsync(new EngagementSlideEvent(offset));
        await ShowCountersAsync();
    }

    // ------------------------------------------------------------------------------------
    // 02 §6 — ComputeExchange and helpers
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_0347f</c> (<c>FIGT.ADV:2137-2149</c>) — base hit chance from <c>Tmp0001</c> (attack)
    /// and <c>Tmp0002</c> (defence) into <c>Bt_HitChance</c>. 02 §6.1.
    /// </summary>
    internal void HitChance()
    {
        if (State.Tmp0001 > State.Tmp0002)
        {
            State.Bt_HitChance = (State.Tmp0001 - State.Tmp0002) * 8;
            if (State.Bt_HitChance > 99)
                State.Bt_HitChance = 100;
            return;
        }

        // QUIRK: 02 Q2 — (8 + atk) - def: atk = def and atk = def + 1 both give 8.
        State.Bt_HitChance = (Sys3Int)8 + State.Tmp0001 - State.Tmp0002;
        if (State.Bt_HitChance == 0)
            State.Bt_HitChance = 1;
    }

    /// <summary><c>L_034d3</c> (<c>FIGT.ADV:2150-2157</c>) — clamps <c>Bt_HitChance</c> to 1..100. 02 §6.1.</summary>
    internal void ClampHit()
    {
        if (State.Bt_HitChance > 100)
            State.Bt_HitChance = 100;
        if (State.Bt_HitChance == 0)
            State.Bt_HitChance = 1;
    }

    /// <summary>
    /// <c>L_03502</c> (<c>FIGT.ADV:2158-2173</c>) — random spread of the expected hits in
    /// <c>CallArg</c>: ±1 below 11, otherwise <c>CallArg - 2k + RND(3k)</c> with <c>k = CallArg / 10</c>
    /// (kept in <c>Bt_RoundStep</c>). Always one draw. 02 §6.1.
    /// </summary>
    internal void Spread()
    {
        if (State.CallArg < 11)
        {
            Rnd(3); // 2160
            if (State.RetVal == 1)
                State.CallArg = State.CallArg + 1;
            if (State.RetVal == 2)
                State.CallArg = State.CallArg - 1;
            return;
        }

        // QUIRK: 02 Q4 — Bt_RoundStep (VAR0623) is used as a temporary here.
        State.Bt_RoundStep = State.CallArg / 10;
        State.CallArg = State.CallArg - State.Bt_RoundStep * 2;
        // QUIRK: 02 Q5 — the spread is biased: [E - 2k + 1, E + k].
        Rnd(State.Bt_RoundStep * 3); // 2171
        State.CallArg = State.CallArg + State.RetVal;
    }

    /// <summary>
    /// <c>L_0356f</c> (<c>FIGT.ADV:2174-2186</c>) and its identical copy <c>L_035b5</c>
    /// (<c>2187-2199</c>) — rolls <c>Tmp0001</c> shots against <c>Bt_HitChance</c>; hits into
    /// <c>CallArg</c>. A do-while: at least one roll even for 0 shots. 02 §6.1.
    /// </summary>
    internal void RollShots()
    {
        State.Tmp0004 = 1;
        State.CallArg = 0;
        while (true)
        {
            Rnd(100); // 2178 / 2191
            if (State.RetVal < State.Bt_HitChance + 1)
                State.CallArg = State.CallArg + 1;
            State.Tmp0004 = State.Tmp0004 + 1;
            // QUIRK: 02 Q3 — phantom shot: the test is after the roll, so 0 shots still roll once.
            if (State.Tmp0004 > State.Tmp0001)
                return;
        }
    }

    /// <summary>The partner test of <c>FIGT.ADV:2457</c> / <c>2474</c>: <c>Unit_Partner</c> is non-zero and in a player slot.</summary>
    private bool PartnerPresent() =>
        (Unit.Partner == State.Bt_PlayerSlot1 || Unit.Partner == State.Bt_PlayerSlot2
            || Unit.Partner == State.Bt_PlayerSlot3 || Unit.Partner == State.Bt_PlayerSlot4)
        && Unit.Partner != 0;

    /// <summary>
    /// <c>L_03c69</c> (<c>FIGT.ADV:2450-2466</c>) — player troop attack <c>Tmp0001</c>: situational
    /// modifier (0-9 = +n, 11-19 = -(n-10)) and the partner bonus RND(3). 02 §6.1.
    /// </summary>
    internal void AtkMods()
    {
        if (Unit.CurAtkMod < 10)
            State.Tmp0001 = State.Tmp0001 + Unit.CurAtkMod;
        if (Unit.CurAtkMod > 10)
            State.Tmp0001 = State.Tmp0001 - (Unit.CurAtkMod - 10);
        if (PartnerPresent())
        {
            Rnd(3); // 2458
            if (State.RetVal == 1)
                State.Tmp0001 = State.Tmp0001 + 2;
            if (State.RetVal == 2)
                State.Tmp0001 = State.Tmp0001 + 1;
        }
    }

    /// <summary>
    /// <c>L_03cf0</c> (<c>FIGT.ADV:2467-2483</c>) — player troop defence <c>Tmp0002</c>: situational
    /// modifier and the partner bonus RND(3). 02 §6.1.
    /// </summary>
    internal void DefMods()
    {
        if (Unit.CurDefMod < 10)
            State.Tmp0002 = State.Tmp0002 + Unit.CurDefMod;
        if (Unit.CurDefMod > 10)
            State.Tmp0002 = State.Tmp0002 - (Unit.CurDefMod - 10);
        if (PartnerPresent())
        {
            Rnd(3); // 2475
            if (State.RetVal == 1)
                State.Tmp0002 = State.Tmp0002 + 2;
            if (State.RetVal == 2)
                State.Tmp0002 = State.Tmp0002 + 1;
        }
    }

    /// <summary>
    /// <c>L_035fb</c> (<c>FIGT.ADV:2200-2449</c>) — computes the 16 exchange results
    /// <c>Bt_[PE]{TroopVsCmdr,TroopVsTroop,CmdrVsTroop,CmdrVsCmdr}{Shots,Dmg}</c> for
    /// <c>Unit_Cur</c> against <c>Cmdr_Cur</c>: engagement, the six sub-exchanges, strategy,
    /// clamps, special-attack overrides and demon shields. Modifies no record. 02 §6.2, §3.3.
    /// </summary>
    internal void ComputeExchange()
    {
        State.Bt_PlayerAllEngaged = 1;
        State.Bt_EnemyAllEngaged = 1;
        State.Bt_PlayerEngaged = Unit.Troops;
        State.Bt_EnemyEngaged = Cmdr.Troops;
        // QUIRK: 02 Q17 — Unit_Item 0 binds item record 0 (its effect aliases E. Nurse's (item 50) amount).
        State.Item_Cur = Unit.Item;
        State.Bt_PTroopVsCmdrShots = 0;
        State.Bt_PTroopVsCmdrDmg = 0;
        State.Bt_PTroopVsTroopShots = 0;
        State.Bt_PTroopVsTroopDmg = 0;
        State.Bt_PCmdrVsTroopShots = 0;
        State.Bt_PCmdrVsTroopDmg = 0;
        State.Bt_PCmdrVsCmdrShots = 0;
        State.Bt_PCmdrVsCmdrDmg = 0;
        State.Bt_ETroopVsCmdrShots = 0;
        State.Bt_ETroopVsCmdrDmg = 0;
        State.Bt_ETroopVsTroopShots = 0;
        State.Bt_ETroopVsTroopDmg = 0;
        State.Bt_ECmdrVsTroopShots = 0;
        State.Bt_ECmdrVsTroopDmg = 0;
        State.Bt_ECmdrVsCmdrShots = 0;
        State.Bt_ECmdrVsCmdrDmg = 0;

        if (State.Bt_PlayerActSlot == 1)
            State.Bt_PlayerSlotDef = State.Bt_PSlot1State;
        if (State.Bt_PlayerActSlot == 2)
            State.Bt_PlayerSlotDef = State.Bt_PSlot2State;
        if (State.Bt_PlayerActSlot == 3)
            State.Bt_PlayerSlotDef = State.Bt_PSlot3State;
        if (State.Bt_PlayerActSlot == 4)
            State.Bt_PlayerSlotDef = State.Bt_PSlot4State;
        if (State.Bt_EnemyActSlot == 1)
            State.Bt_EnemySlotDef = State.Bt_ESlot1State;
        if (State.Bt_EnemyActSlot == 2)
            State.Bt_EnemySlotDef = State.Bt_ESlot2State;
        if (State.Bt_EnemyActSlot == 3)
            State.Bt_EnemySlotDef = State.Bt_ESlot3State;
        if (State.Bt_EnemyActSlot == 4)
            State.Bt_EnemySlotDef = State.Bt_ESlot4State;

        if (Unit.Troops > State.Bt_TroopLimit)
        {
            State.Bt_PlayerEngaged = State.Bt_TroopLimit;
            State.Bt_PlayerAllEngaged = 0;
        }

        if (Cmdr.Troops > State.Bt_TroopLimit)
        {
            State.Bt_EnemyEngaged = State.Bt_TroopLimit;
            State.Bt_EnemyAllEngaged = 0;
        }

        // QUIRK: BRS-24 — the holder of Chaos or Nikkou over the limit cannot wound a demon (the shields below), unless
        // the player chose our fix (decision 32, DemonReach): then he fights in person (b), and on the field the demon's
        // troops may shoot at him (d). The flag itself keeps the original's value.
        bool slayer = State.Bt_PlayerAllEngaged == 0 && DemonSlayerInPerson;
        bool playerInPerson = State.Bt_PlayerAllEngaged == 1 || slayer;
        bool playerShotAt = State.Bt_PlayerAllEngaged == 1 || (slayer && Script.DemonReach == DemonReach.OnField);

        // ---- player side (2254-2273) ----
        // QUIRK: 02 Q24 — troops shoot a commander and a commander fights in person only when
        // the relevant army fits the battle limit.
        if (State.Bt_EnemyAllEngaged == 1)
        {
            State.Bt_PTroopVsCmdrShots = State.Bt_PlayerEngaged / State.Bt_EnemyEngaged * 2;
            if (State.Bt_EnemyEngaged == 0)
                State.Bt_PTroopVsCmdrShots = State.Bt_PlayerEngaged;
            if (State.Bt_PTroopVsCmdrShots != 0)
                PlayerTroopsVsCmdr();
        }

        if (playerInPerson)
        {
            if (State.Bt_EnemyEngaged > 9)
                PlayerCmdrVsTroops();
            if (State.Bt_EnemyEngaged < 10)
                PlayerCmdrVsCmdr();
        }

        if (State.Bt_PlayerEngaged != 0)
            PlayerTroopsVsTroops();

        // ---- enemy side (2274-2293) ----
        if (playerShotAt)
        {
            State.Bt_ETroopVsCmdrShots = State.Bt_EnemyEngaged / State.Bt_PlayerEngaged * 2;
            if (State.Bt_PlayerEngaged == 0)
                State.Bt_ETroopVsCmdrShots = State.Bt_EnemyEngaged;
            if (State.Bt_ETroopVsCmdrShots != 0)
                EnemyTroopsVsCmdr();
        }

        if (State.Bt_EnemyAllEngaged == 1)
        {
            if (State.Bt_PlayerEngaged > 9)
                EnemyCmdrVsTroops();
            if (State.Bt_PlayerEngaged < 10)
                EnemyCmdrVsCmdr();
        }

        if (State.Bt_EnemyEngaged != 0)
            EnemyTroopsVsTroops();

        // ---- strategy, troop-vs-troop damage only (2294-2317) ----
        if (State.Bt_StrategyResult == 1)
        {
            State.Tmp0001 = State.Bt_ETroopVsTroopDmg / 10;
            State.Bt_ETroopVsTroopDmg = State.Bt_ETroopVsTroopDmg - State.Tmp0001;
            State.Tmp0001 = State.Bt_PTroopVsTroopDmg / 10;
            State.Bt_PTroopVsTroopDmg = State.Bt_PTroopVsTroopDmg + State.Tmp0001;
        }

        if (State.Bt_StrategyResult == 2)
        {
            State.Tmp0001 = State.Bt_ETroopVsTroopDmg / 2;
            State.Bt_ETroopVsTroopDmg = State.Bt_ETroopVsTroopDmg - State.Tmp0001;
            State.Tmp0001 = State.Bt_PTroopVsTroopDmg / 2;
            State.Bt_PTroopVsTroopDmg = State.Bt_PTroopVsTroopDmg + State.Tmp0001;
        }

        if (State.Bt_StrategyResult == 11)
        {
            State.Tmp0001 = State.Bt_ETroopVsTroopDmg / 10;
            State.Bt_ETroopVsTroopDmg = State.Bt_ETroopVsTroopDmg + State.Tmp0001;
            State.Tmp0001 = State.Bt_PTroopVsTroopDmg / 10;
            State.Bt_PTroopVsTroopDmg = State.Bt_PTroopVsTroopDmg - State.Tmp0001;
        }

        if (State.Bt_StrategyResult == 12)
        {
            State.Tmp0001 = State.Bt_ETroopVsTroopDmg / 2;
            State.Bt_ETroopVsTroopDmg = State.Bt_ETroopVsTroopDmg + State.Tmp0001;
            State.Tmp0001 = State.Bt_PTroopVsTroopDmg / 2;
            State.Bt_PTroopVsTroopDmg = State.Bt_PTroopVsTroopDmg - State.Tmp0001;
        }

        // ---- clamps (2318-2323) ----
        if (State.Bt_ETroopVsTroopDmg + State.Bt_ECmdrVsTroopDmg > State.Bt_PlayerEngaged)
            State.Bt_ETroopVsTroopDmg = State.Bt_PlayerEngaged - State.Bt_ECmdrVsTroopDmg;
        if (State.Bt_PTroopVsTroopDmg + State.Bt_PCmdrVsTroopDmg > State.Bt_EnemyEngaged)
            State.Bt_PTroopVsTroopDmg = State.Bt_EnemyEngaged - State.Bt_PCmdrVsTroopDmg;

        ApplySpecialAttack();
        ApplyDemonShields();
    }

    /// <summary>
    /// <c>FIGT.ADV:2324-2413</c> — the special-attack overrides of <see cref="ComputeExchange"/>,
    /// applied after the normal exchange (whose draws all happened). 02 §3.3, §6.2.
    /// </summary>
    internal void ApplySpecialAttack()
    {
        if (State.Bt_SpecialAtk == 1)
        {
            PlayerCmdrVsCmdr();
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
            State.Bt_PCmdrVsTroopShots = 0;
            State.Bt_PCmdrVsTroopDmg = 0;
        }

        if (State.Bt_SpecialAtk == 2)
        {
            PlayerCmdrVsTroops();
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
            State.Bt_PCmdrVsCmdrShots = 0;
            State.Bt_PCmdrVsCmdrDmg = 0;
        }

        if (State.Bt_SpecialAtk == 3)
        {
            State.Bt_PCmdrVsCmdrShots = 1;
            State.Bt_PCmdrVsCmdrDmg = 1;
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
            State.Bt_PCmdrVsTroopShots = 0;
            State.Bt_PCmdrVsTroopDmg = 0;
        }

        if (State.Bt_SpecialAtk == 4)
        {
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
            State.Bt_PCmdrVsCmdrShots = Cmdr.HP / 2;
            State.Bt_PCmdrVsCmdrDmg = Cmdr.HP / 2;
            State.Bt_PCmdrVsTroopShots = Cmdr.Troops;
            State.Bt_PCmdrVsTroopDmg = Cmdr.Troops;
        }

        if (State.Bt_SpecialAtk == 5)
        {
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
            // QUIRK: 02 Q26 — War Dance hurts Patton: enemy troop → commander values are overwritten.
            State.Bt_ETroopVsCmdrShots = Cmdr.Troops / 5;
            State.Bt_ETroopVsCmdrDmg = Cmdr.Troops / 100;
            State.Bt_PCmdrVsTroopShots = Cmdr.Troops;
            Rnd(Cmdr.Troops / 2); // 2370
            State.Bt_PCmdrVsTroopDmg = Cmdr.Troops / 2 + State.RetVal;
            State.Bt_PCmdrVsCmdrShots = 0;
            State.Bt_PCmdrVsCmdrDmg = 0;
        }

        if (State.Bt_SpecialAtk == 6)
        {
            PlayerCmdrVsTroops();
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
            State.Bt_PCmdrVsCmdrShots = 0;
            State.Bt_PCmdrVsCmdrDmg = 0;
        }

        if (State.Bt_SpecialAtk == 7)
        {
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
            State.Bt_PCmdrVsTroopShots = Cmdr.Troops / 2;
            State.Bt_PCmdrVsTroopDmg = Cmdr.Troops / 2;
            State.Bt_PCmdrVsCmdrShots = 2;
            State.Bt_PCmdrVsCmdrDmg = 2;
        }

        if (State.Bt_SpecialAtk == 10)
        {
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
            State.Bt_PCmdrVsCmdrShots = 0;
            State.Bt_PCmdrVsCmdrDmg = 0;
            State.Bt_PCmdrVsTroopShots = Cmdr.Troops;
            State.Bt_PCmdrVsTroopDmg = Cmdr.Troops / 2;
        }

        if (State.Bt_SpecialAtk == 100)
        {
            State.Bt_PTroopVsCmdrShots = 10000;
            State.Bt_PTroopVsTroopShots = 10000;
            State.Bt_PCmdrVsTroopShots = 10000;
            State.Bt_PCmdrVsCmdrShots = 10000;
            State.Bt_PTroopVsCmdrDmg = 10000;
            State.Bt_PTroopVsTroopDmg = 10000;
            State.Bt_PCmdrVsTroopDmg = 10000;
            State.Bt_PCmdrVsCmdrDmg = 10000;
        }
    }

    /// <summary>
    /// Ours (owner's decision 32, docs/tweaks.md): whether BRS-24's fix sends the player's commander in person against this
    /// enemy whatever his troops — the player chose it (<see cref="Script.OriginalScriptContext.DemonReach"/>), the enemy
    /// commander is a demon and the unit holds Chaos (item 1) or Nikkou (item 2), tested as the shields test them
    /// (<see cref="ApplyDemonShields"/>).
    /// </summary>
    private bool DemonSlayerInPerson =>
        Script.DemonReach != DemonReach.Original && Cmdr.Class > 9 && (Unit.Item == 1 || Unit.Item == 2);

    /// <summary>
    /// <c>FIGT.ADV:2414-2448</c> — demon shields: a demon commander (class &gt; 9) takes no HP
    /// damage from troops, and from a commander only if that one is a demon too (or, for enemy
    /// demons, wields Chaos/Nikkou). Only damage is zeroed. 02 §6.2.
    /// </summary>
    internal void ApplyDemonShields()
    {
        State.Bt_PlayerCmdrShield = 0;
        State.Bt_EnemyCmdrShield = 0;
        if (Unit.Class > 9)
        {
            State.Bt_PlayerCmdrShield = 1;
            if (Cmdr.Class > 9)
                State.Bt_PlayerCmdrShield = 2;
        }

        if (State.Cmdr_Cur == 146 || State.Cmdr_Cur == 147)
            State.Bt_PlayerCmdrShield = 0;
        if (State.Cmdr_Cur == 187 || State.Cmdr_Cur == 188 || State.Cmdr_Cur == 189 || State.Cmdr_Cur == 190)
            State.Bt_PlayerCmdrShield = 0;
        if (Cmdr.Class > 9)
        {
            State.Bt_EnemyCmdrShield = 1;
            if (Unit.Class > 9)
                State.Bt_EnemyCmdrShield = 2;
            if (Unit.Item == 1 || Unit.Item == 2)
                State.Bt_EnemyCmdrShield = 2;
        }

        if (State.Bt_PlayerCmdrShield != 0)
        {
            State.Bt_ETroopVsCmdrDmg = 0;
            if (State.Bt_PlayerCmdrShield != 2)
                State.Bt_ECmdrVsCmdrDmg = 0;
        }

        if (State.Bt_EnemyCmdrShield != 0)
        {
            State.Bt_PTroopVsCmdrDmg = 0;
            if (State.Bt_EnemyCmdrShield != 2)
                State.Bt_PCmdrVsCmdrDmg = 0;
        }
    }

    /// <summary>
    /// <c>L_03d77</c> (<c>FIGT.ADV:2484-2523</c>) — player troops shoot the enemy commander
    /// (<c>Bt_PTroopVsCmdrShots</c> set by the caller). 02 §6.3.
    /// </summary>
    internal void PlayerTroopsVsCmdr()
    {
        if (State.Bt_PlayerAction == 0)
        {
            State.Tmp0001 = Unit.TroopAtk;
            if (Item.Effect == 3)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            AtkMods();
            State.Tmp0002 = Cmdr.Def;
        }

        if (State.Bt_PlayerAction == 1)
        {
            State.Tmp0001 = Unit.TroopAtk;
            if (Item.Effect == 3)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            AtkMods();
            State.Tmp0002 = Cmdr.Def;
        }

        if (State.Bt_PlayerAction == 2)
        {
            State.Tmp0001 = Unit.Magic;
            AtkMods();
            State.Tmp0002 = Cmdr.Magic;
        }

        // 02 §1.4: with action 99 Tmp0001/Tmp0002 are stale here.
        HitChance();
        State.Bt_HitChance = State.Bt_HitChance + Unit.Level - State.Bt_EnemySlotDef;
        ClampHit();
        if (State.Bt_PTroopVsCmdrShots > 80)
        {
            // QUIRK: 02 Q1 — shots * hit chance saturates at 65535 before / 100 (cap 655).
            State.CallArg = State.Bt_PTroopVsCmdrShots * State.Bt_HitChance / 100;
            Spread();
            State.Bt_PTroopVsCmdrDmg = State.CallArg;
        }

        if (State.Bt_PTroopVsCmdrShots < 81)
        {
            State.Tmp0001 = State.Bt_PTroopVsCmdrShots;
            RollShots();
            State.Bt_PTroopVsCmdrDmg = State.CallArg;
        }

        if (State.Bt_PlayerAction == 99)
        {
            State.Bt_PTroopVsCmdrShots = 0;
            State.Bt_PTroopVsCmdrDmg = 0;
        }
    }

    /// <summary>
    /// <c>L_03e79</c> (<c>FIGT.ADV:2524-2563</c>) — player troops versus enemy troops; always the
    /// spread path. 02 §6.4.
    /// </summary>
    internal void PlayerTroopsVsTroops()
    {
        if (State.Bt_PlayerAction == 0)
        {
            State.Tmp0001 = Unit.TroopAtk;
            if (Item.Effect == 3)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            AtkMods();
            // QUIRK: 02 Q9 — the enemy troop modifier is added raw.
            State.Tmp0002 = Cmdr.TroopDef + Cmdr.TroopDefMod;
        }

        if (State.Bt_PlayerAction == 1)
        {
            State.Tmp0001 = Unit.TroopAtk;
            if (Item.Effect == 3)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            AtkMods();
            State.Tmp0002 = Cmdr.TroopDef + Cmdr.TroopDefMod;
        }

        if (State.Bt_PlayerAction == 2)
        {
            State.Tmp0001 = Unit.Magic;
            AtkMods();
            State.Tmp0002 = Cmdr.Magic + Cmdr.TroopDefMod;
        }

        HitChance();
        State.Bt_HitChance = State.Bt_HitChance + Unit.Level - State.Bt_EnemySlotDef;
        // QUIRK: 02 Q23 — the wall penalty applies to player troops only when Bt_Kind = 1.
        if (State.Bt_Kind == 1)
            State.Bt_HitChance = State.Bt_HitChance - State.Bt_WallBonus * 2;
        ClampHit();
        State.Bt_PTroopVsTroopShots = State.Bt_PlayerEngaged - State.Bt_PTroopVsCmdrShots;
        State.CallArg = State.Bt_PTroopVsTroopShots * State.Bt_HitChance / 100;
        Spread();
        State.Bt_PTroopVsTroopDmg = State.CallArg;
        if (State.Bt_PTroopVsTroopDmg + State.Bt_PCmdrVsTroopDmg > State.Bt_EnemyEngaged)
            State.Bt_PTroopVsTroopDmg = State.Bt_EnemyEngaged - State.Bt_PCmdrVsTroopDmg;
        if (State.Bt_PlayerAction == 99)
        {
            State.Bt_PTroopVsTroopShots = 0;
            State.Bt_PTroopVsTroopDmg = 0;
        }
    }

    /// <summary>
    /// <c>L_03f93</c> (<c>FIGT.ADV:2564-2622</c>) — the player commander fights enemy troops;
    /// shots <c>RND(Unit_Attacks) + Unit_Attacks / 2</c>, or the whole enemy army (special 2), or
    /// <c>min(RND(15) * 100, troops)</c> (special 6). 02 §6.5.
    /// </summary>
    internal void PlayerCmdrVsTroops()
    {
        if (State.Bt_PlayerAction == 0)
        {
            State.Tmp0001 = Unit.Atk;
            if (Item.Effect == 1)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            State.Tmp0002 = Cmdr.TroopDef + Cmdr.TroopDefMod;
        }

        if (State.Bt_PlayerAction == 1)
        {
            State.Tmp0001 = Unit.Atk;
            if (Item.Effect == 1)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            State.Tmp0002 = Cmdr.TroopDef + Cmdr.TroopDefMod;
        }

        if (State.Bt_PlayerAction == 2)
        {
            State.Tmp0001 = Unit.Magic;
            if (Item.Effect == 5)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            if (State.Bt_SpecialAtk == 2 && State.Unit_Cur == 37)
                State.Tmp0001 = State.Tmp0001 + 3; // Gandhi's EPO Light
            State.Tmp0002 = Cmdr.Magic;
        }

        HitChance();
        State.Bt_HitChance = State.Bt_HitChance + Unit.Level - State.Bt_EnemySlotDef;
        ClampHit();
        Rnd(Unit.Attacks); // 2592
        State.Tmp0001 = State.RetVal + Unit.Attacks / 2;
        if (State.Bt_SpecialAtk == 2)
            State.Tmp0001 = Cmdr.Troops;
        if (State.Bt_SpecialAtk == 6)
        {
            Rnd(15); // 2598
            State.RetVal = State.RetVal * 100;
            if (Cmdr.Troops < State.RetVal)
                State.RetVal = Cmdr.Troops;
            State.Tmp0001 = State.RetVal;
        }

        State.Bt_PCmdrVsTroopShots = State.Tmp0001;
        if (State.Bt_PCmdrVsTroopShots > 80)
        {
            State.CallArg = State.Bt_PCmdrVsTroopShots * State.Bt_HitChance / 100;
            Spread();
            State.Bt_PCmdrVsTroopDmg = State.CallArg;
        }

        if (State.Bt_PCmdrVsTroopShots < 81)
        {
            RollShots(); // L_035b5 with Tmp0001 = shots
            State.Bt_PCmdrVsTroopDmg = State.CallArg;
        }

        if (State.Bt_PCmdrVsTroopDmg > State.Bt_EnemyEngaged && State.Bt_SpecialAtk != 2)
            State.Bt_PCmdrVsTroopDmg = State.Bt_EnemyEngaged;
        if (State.Bt_PlayerAction == 99)
        {
            State.Bt_PCmdrVsTroopShots = 0;
            State.Bt_PCmdrVsTroopDmg = 0;
        }
    }

    /// <summary>
    /// <c>L_0412d</c> (<c>FIGT.ADV:2623-2670</c>) — the player commander fights the enemy
    /// commander; shots <c>RND(Unit_Attacks / 5)</c> (at least 1, re-rolled as RND(6) above 5).
    /// 02 §6.6.
    /// </summary>
    internal void PlayerCmdrVsCmdr()
    {
        if (State.Bt_PlayerAction == 0)
        {
            State.Tmp0001 = Unit.Atk;
            if (Item.Effect == 1)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            State.Tmp0002 = Cmdr.Def;
        }

        if (State.Bt_PlayerAction == 1)
        {
            State.Tmp0001 = Unit.Atk;
            if (Item.Effect == 1)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            State.Tmp0002 = Cmdr.Def;
        }

        if (State.Bt_PlayerAction == 2)
        {
            State.Tmp0001 = Unit.Magic;
            if (Item.Effect == 5)
                State.Tmp0001 = State.Tmp0001 + Item.Amount;
            State.Tmp0002 = Cmdr.Magic;
        }

        HitChance();
        State.Bt_HitChance = State.Bt_HitChance + Unit.Level - State.Bt_EnemySlotDef;
        ClampHit();
        Rnd(Unit.Attacks / 5); // 2648
        if (State.RetVal == 0)
            State.RetVal = 1;
        if (State.RetVal > 5)
            Rnd(6); // 2653
        State.Tmp0001 = State.RetVal;
        State.Bt_PCmdrVsCmdrShots = State.Tmp0001;
        if (State.Bt_PCmdrVsCmdrShots > 80)
        {
            State.CallArg = State.Bt_PCmdrVsCmdrShots * State.Bt_HitChance / 100;
            Spread();
            State.Bt_PCmdrVsCmdrDmg = State.CallArg;
        }

        if (State.Bt_PCmdrVsCmdrShots < 81)
        {
            RollShots(); // L_035b5
            State.Bt_PCmdrVsCmdrDmg = State.CallArg;
        }

        if (State.Bt_PlayerAction == 99)
        {
            State.Bt_PCmdrVsCmdrShots = 0;
            State.Bt_PCmdrVsCmdrDmg = 0;
        }
    }

    /// <summary>
    /// <c>L_04267</c> (<c>FIGT.ADV:2671-2713</c>) — enemy troops shoot the player commander
    /// (<c>Bt_ETroopVsCmdrShots</c> set by the caller). 02 §6.7.
    /// </summary>
    internal void EnemyTroopsVsCmdr()
    {
        if (State.Bt_EnemyAction == 0)
        {
            State.Tmp0001 = Cmdr.TroopAtk + Cmdr.TroopAtkMod;
            State.Tmp0002 = Unit.Def;
            if (Item.Effect == 2)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
        }

        if (State.Bt_EnemyAction == 1)
        {
            State.Tmp0001 = Cmdr.TroopAtk + Cmdr.TroopAtkMod;
            State.Tmp0002 = Unit.Def;
            if (Item.Effect == 2)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
        }

        if (State.Bt_EnemyAction == 2)
        {
            State.Tmp0001 = Cmdr.Magic + Cmdr.TroopAtkMod;
            State.Tmp0002 = Unit.Magic;
            // QUIRK: 02 Q10 — the magic-attack item (effect 5) also defends here.
            if (Item.Effect == 5)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
            if (Item.Effect == 8)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
        }

        HitChance();
        State.Bt_HitChance = State.Bt_HitChance - Unit.Level - State.Bt_PlayerSlotDef;
        ClampHit();
        if (State.Bt_ETroopVsCmdrShots > 80)
        {
            State.CallArg = State.Bt_ETroopVsCmdrShots * State.Bt_HitChance / 100;
            Spread();
            State.Bt_ETroopVsCmdrDmg = State.CallArg;
        }

        if (State.Bt_ETroopVsCmdrShots < 81)
        {
            State.Tmp0001 = State.Bt_ETroopVsCmdrShots;
            RollShots(); // L_0356f
            State.Bt_ETroopVsCmdrDmg = State.CallArg;
        }

        if (State.Bt_EnemyAction == 99)
        {
            State.Bt_ETroopVsCmdrShots = 0;
            State.Bt_ETroopVsCmdrDmg = 0;
        }
    }

    /// <summary>
    /// <c>L_0438f</c> (<c>FIGT.ADV:2714-2756</c>) — enemy troops versus player troops; always the
    /// spread path. 02 §6.8.
    /// </summary>
    internal void EnemyTroopsVsTroops()
    {
        if (State.Bt_EnemyAction == 0)
        {
            State.Tmp0001 = Cmdr.TroopAtk + Cmdr.TroopAtkMod;
            State.Tmp0002 = Unit.TroopDef;
            if (Item.Effect == 4)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
            DefMods();
        }

        if (State.Bt_EnemyAction == 1)
        {
            State.Tmp0001 = Cmdr.TroopAtk + Cmdr.TroopAtkMod;
            State.Tmp0002 = Unit.TroopDef;
            if (Item.Effect == 4)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
            DefMods();
        }

        if (State.Bt_EnemyAction == 2)
        {
            State.Tmp0001 = Cmdr.Magic + Cmdr.TroopAtkMod;
            State.Tmp0002 = Unit.Magic;
            if (Item.Effect == 8)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
            DefMods();
        }

        HitChance();
        State.Bt_HitChance = State.Bt_HitChance - Unit.Level - State.Bt_PlayerSlotDef;
        if (State.Bt_Kind == 2)
            State.Bt_HitChance = State.Bt_HitChance - State.Bt_WallBonus * 2;
        ClampHit();
        State.Bt_ETroopVsTroopShots = State.Bt_EnemyEngaged - State.Bt_ETroopVsCmdrShots;
        State.CallArg = State.Bt_ETroopVsTroopShots * State.Bt_HitChance / 100;
        Spread();
        State.Bt_ETroopVsTroopDmg = State.CallArg;
        if (State.Bt_ETroopVsTroopDmg + State.Bt_ECmdrVsTroopDmg > State.Bt_PlayerEngaged)
            State.Bt_ETroopVsTroopDmg = State.Bt_PlayerEngaged - State.Bt_ECmdrVsTroopDmg;
        if (State.Bt_EnemyAction == 99)
        {
            State.Bt_ETroopVsTroopShots = 0;
            State.Bt_ETroopVsTroopDmg = 0;
        }
    }

    /// <summary>
    /// <c>L_044bf</c> (<c>FIGT.ADV:2757-2803</c>) — the enemy commander fights player troops;
    /// shots <c>RND(Cmdr_Attacks) + Cmdr_Attacks / 2</c>. 02 §6.9.
    /// </summary>
    internal void EnemyCmdrVsTroops()
    {
        if (State.Bt_EnemyAction == 0)
        {
            State.Tmp0001 = Cmdr.Atk;
            State.Tmp0002 = Unit.TroopDef;
            if (Item.Effect == 4)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
            DefMods();
        }

        if (State.Bt_EnemyAction == 1)
        {
            State.Tmp0001 = Cmdr.Atk;
            State.Tmp0002 = Unit.TroopDef;
            if (Item.Effect == 4)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
            DefMods();
        }

        if (State.Bt_EnemyAction == 2)
        {
            State.Tmp0001 = Cmdr.Magic;
            State.Tmp0002 = Unit.Magic;
            if (Item.Effect == 8)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
        }

        HitChance();
        State.Bt_HitChance = State.Bt_HitChance - Unit.Level - State.Bt_PlayerSlotDef;
        ClampHit();
        Rnd(Cmdr.Attacks); // 2784
        State.Tmp0001 = State.RetVal + Cmdr.Attacks / 2;
        State.Bt_ECmdrVsTroopShots = State.Tmp0001;
        if (State.Bt_ECmdrVsTroopShots > 80)
        {
            State.CallArg = State.Bt_ECmdrVsTroopShots * State.Bt_HitChance / 100;
            Spread();
            State.Bt_ECmdrVsTroopDmg = State.CallArg;
        }

        if (State.Bt_ECmdrVsTroopShots < 81)
        {
            RollShots(); // L_035b5
            State.Bt_ECmdrVsTroopDmg = State.CallArg;
        }

        if (State.Bt_ECmdrVsTroopDmg > State.Bt_PlayerEngaged)
            State.Bt_ECmdrVsTroopDmg = State.Bt_PlayerEngaged;
        if (State.Bt_EnemyAction == 99)
        {
            State.Bt_ECmdrVsTroopShots = 0;
            State.Bt_ECmdrVsTroopDmg = 0;
        }
    }

    /// <summary>
    /// <c>L_045f7</c> (<c>FIGT.ADV:2804-2857</c>) — the enemy commander fights the player
    /// commander; shots as <see cref="PlayerCmdrVsCmdr"/>, except Kayblis at war (RND(100)).
    /// 02 §6.9.
    /// </summary>
    internal void EnemyCmdrVsCmdr()
    {
        if (State.Bt_EnemyAction == 0)
        {
            State.Tmp0001 = Cmdr.Atk;
            State.Tmp0002 = Unit.Def;
            if (Item.Effect == 2)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
        }

        if (State.Bt_EnemyAction == 1)
        {
            State.Tmp0001 = Cmdr.Atk;
            State.Tmp0002 = Unit.Def;
            if (Item.Effect == 2)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
        }

        if (State.Bt_EnemyAction == 2)
        {
            State.Tmp0001 = Cmdr.Magic;
            State.Tmp0002 = Unit.Magic;
            if (Item.Effect == 5)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
            if (Item.Effect == 8)
                State.Tmp0002 = State.Tmp0002 + Item.Amount;
        }

        HitChance();
        State.Bt_HitChance = State.Bt_HitChance - Unit.Level - State.Bt_PlayerSlotDef;
        ClampHit();
        Rnd(Cmdr.Attacks / 5); // 2832
        if (State.RetVal == 0)
            State.RetVal = 1;
        if (State.RetVal > 5)
            Rnd(6); // 2837
        // QUIRK: 02 Q22 — Kayblis at war rolls 1..100 shots, bypassing the RND(6) cap.
        if (State.Cmdr_Cur == 135 && State.F_Kayblis_War == 2)
            Rnd(100); // 2840
        State.Tmp0001 = State.RetVal;
        State.Bt_ECmdrVsCmdrShots = State.RetVal;
        if (State.Bt_ECmdrVsCmdrShots > 80)
        {
            State.CallArg = State.Bt_ECmdrVsCmdrShots * State.Bt_HitChance / 100;
            Spread();
            State.Bt_ECmdrVsCmdrDmg = State.CallArg;
        }

        if (State.Bt_ECmdrVsCmdrShots < 81)
        {
            RollShots(); // L_035b5
            State.Bt_ECmdrVsCmdrDmg = State.CallArg;
        }

        if (State.Bt_EnemyAction == 99)
        {
            State.Bt_ECmdrVsCmdrShots = 0;
            State.Bt_ECmdrVsCmdrDmg = 0;
        }
    }

    // ------------------------------------------------------------------------------------
    // 02 §7 — ApplyExchange, FIGTATTK, ninja follow-up, slot compaction
    // ------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_02d45</c> (<c>FIGT.ADV:1873-1971</c>) — plays the result animation, applies the 16
    /// results to <c>Unit_Cur</c>/<c>Cmdr_Cur</c>, handles deaths (scenes, redraw, compaction) and
    /// rolls the ninja follow-up. 02 §7.1, 01 §9.3.
    /// </summary>
    internal async Task ApplyExchangeAsync()
    {
        await PlayExchangeAnimationAsync(); // %#FIGTATTK.ADV
        State.Bt_PlayerLosses = State.Bt_PlayerLosses + State.Bt_ETroopVsTroopDmg;
        State.Bt_PlayerLosses = State.Bt_PlayerLosses + State.Bt_ECmdrVsTroopDmg;
        Unit.HP = Unit.HP - State.Bt_ETroopVsCmdrDmg;
        Unit.Troops = Unit.Troops - State.Bt_ETroopVsTroopDmg;
        Unit.Troops = Unit.Troops - State.Bt_ECmdrVsTroopDmg;
        Unit.HP = Unit.HP - State.Bt_ECmdrVsCmdrDmg;
        // QUIRK: 02 Q16 — losses count damage, not real losses (can exceed the troops present).
        State.Bt_EnemyLosses = State.Bt_EnemyLosses + State.Bt_PTroopVsTroopDmg;
        State.Bt_EnemyLosses = State.Bt_EnemyLosses + State.Bt_PCmdrVsTroopDmg;
        Cmdr.HP = Cmdr.HP - State.Bt_PTroopVsCmdrDmg;
        if (State.Bt_PTroopVsCmdrDmg != 0 && State.Bt_EnemyRetreatMode == 5)
            State.Bt_EnemyRetreatMode = 50;
        Cmdr.Troops = Cmdr.Troops - State.Bt_PTroopVsTroopDmg;
        Cmdr.Troops = Cmdr.Troops - State.Bt_PCmdrVsTroopDmg;
        Cmdr.HP = Cmdr.HP - State.Bt_PCmdrVsCmdrDmg;
        if (State.Bt_PCmdrVsCmdrDmg != 0 && State.Bt_EnemyRetreatMode == 5)
            State.Bt_EnemyRetreatMode = 50;

        if (Unit.HP == 0 && State.Unit_Cur != 51)
        {
            Unit.Status = 0;
            State.Bt_PlayerLosses = State.Bt_PlayerLosses + 1;
            await PlayerUnitDefeatedSceneAsync(); // %#FIGTﾐｶDE.ADV
            // QUIRK: 01 Q-27 — the redraw leaves Unit_Cur/Cmdr_Cur on the last occupied slots;
            // the enemy-death and ninja tests below run on those records.
            await RedrawAsync();
            await PauseAsync(10);
            State.CallArg = State.Bt_PlayerActSlot;
            CompactPlayer();
        }

        if (Cmdr.HP == 0)
        {
            // QUIRK: 01 Q-15 — retreat modes 3 and 4 also retreat on the first commander death.
            if (State.Bt_EnemyRetreatMode == 2 || State.Bt_EnemyRetreatMode == 3 || State.Bt_EnemyRetreatMode == 4)
                State.Bt_EnemyRetreatMode = 20;
            Cmdr.Status = 0;
            State.Bt_EnemyLosses = State.Bt_EnemyLosses + 1;
            await EnemyCommanderDefeatedSceneAsync(); // %#FIGTﾃｷDE.ADV
            await RedrawAsync();
            await PauseAsync(10);
            State.CallArg = State.Bt_EnemyActSlot;
            CompactEnemy();
            return;
        }

        Rnd(3); // 1915 [R14]
        // QUIRK: 02 Q14 — the ninja never kills (Cmdr_HP > 1) and follows only player-initiated exchanges.
        if (Unit.HP != 0 && Cmdr.HP > 1 && State.RetVal == 1 && State.Bt_MsgMode != 4 && State.Bt_MsgMode != 2)
        {
            if (SetUpNinja())
            {
                // @L_02ff0: a jump; its "\0" returns to the caller of this ApplyExchange.
                await NinjaFollowUpAsync();
            }
        }
    }

    /// <summary>
    /// The four ninja tests of <c>FIGT.ADV:1917-1969</c> (state part before each
    /// <c>@L_02ff0</c>): Menad → Kanami, Hubert → Freya (<c>N_Hubert_FreyaUses + 1</c>),
    /// Gandhi → Kaoru, Magic → Wichita. Moves <c>Unit_Cur</c> to 51 and writes the ninja's name,
    /// CGs, face and status. Returns whether a ninja was set up. 02 §7.3, 01 §9.5.
    /// </summary>
    internal bool SetUpNinja()
    {
        // QUIRK: 02 Q14 / 01 Q-11 — the ninja flags are tested, never set.
        if (State.Unit_Cur == 10 && State.F_Kanami_NinjaUsed == 0)
        {
            SetNinjaRecord("Kanami", 1164, 9);
            return true;
        }

        if (State.Unit_Cur == 21 && State.F_Freya_NinjaUsed == 0)
        {
            State.N_Hubert_FreyaUses = State.N_Hubert_FreyaUses + 1;
            SetNinjaRecord("Freya", 1176, 449);
            return true;
        }

        if (State.Unit_Cur == 37 && State.F_Kaoru_NinjaUsed == 0)
        {
            SetNinjaRecord("Kaoru", 1182, 72);
            return true;
        }

        if (State.Unit_Cur == 39 && State.F_Wichita_NinjaUsed == 0)
        {
            SetNinjaRecord("Wichita", 1185, 71);
            return true;
        }

        return false;
    }

    /// <summary>One ninja block of <c>FIGT.ADV:1918-1927</c> (and its three copies).</summary>
    private void SetNinjaRecord(string name, Sys3Int cg, Sys3Int face)
    {
        State.Strings[1] = name;
        State.Strings[2] = "　";
        State.Unit_Cur = 51;
        Unit.CgFull = cg;
        Unit.CgPartial = cg;
        Unit.CgAlone = cg;
        Unit.FaceCg = face;
        Unit.Status = 1;
    }

    /// <summary>
    /// <c>L_02ff0</c> (<c>FIGT.ADV:1972-1986</c>) — the ninja (unit record 51) strikes the same
    /// commander as special 3 in a nested exchange. 02 §7.3, 01 §9.5.
    /// </summary>
    private async Task NinjaFollowUpAsync()
    {
        PrepareNinjaFollowUp();
        await PresentEngagementAsync();
        ComputeExchange();
        // Nested: Unit_Cur = 51, so no death handling and no further ninja.
        await ApplyExchangeAsync();
    }

    /// <summary>
    /// State part of <c>L_02ff0</c> before the exchange (<c>FIGT.ADV:1973-1982</c>): copies strings
    /// 1-3 to record 51's strings, sets troops/HP and the special-3 inputs; other record-51 fields
    /// stay stale (02 §1.4, 01 Q-12).
    /// </summary>
    internal void PrepareNinjaFollowUp()
    {
        CopyString(UnitString(0), 1);
        CopyString(UnitString(1), 2);
        CopyString(UnitString(2), 3);
        Unit.Troops = 0;
        Unit.MaxTroops = 0;
        Unit.HP = 3;
        Unit.MaxHP = 3;
        State.Bt_SpecialAtk = 3;
        State.Bt_PlayerAction = 0;
        State.Bt_EnemyAction = 99;
    }

    /// <summary>
    /// <c>FIGTATTK.ADV</c> (whole file) — the result animation: counters, sound selection
    /// (<c>L_00796</c>, which resets <c>Bt_SkillSfx</c>), ten shot frames, the predicted results,
    /// one discarded RND(2) and the "wiped out" marks. Ported for its state writes
    /// (<c>Ui_MouseX/Y</c>, <c>Tmp0002-0005</c>, <c>Tmp0021-0025</c>, <c>CallArg</c>, the SFX
    /// variables) and its draw. 02 §7.2. Public for the router: the scene <c>FIGT表示</c> calls the
    /// file too, with the results it sets by hand (01 §2.7).
    /// </summary>
    public async Task PlayExchangeAnimationAsync()
    {
        CountCgLoad(893); // :2-3 J0 640, 400: G 893 (the backdrop the counters are drawn on)

        // :1-31 current troops and HP
        if (Unit.Troops != 0)
            DrawCounter(100, 0, 0, Unit.Troops);
        if (Cmdr.Troops != 0)
            DrawCounter(480, 0, 0, Cmdr.Troops);
        DrawCounter(0, 368, 0, Unit.HP);
        DrawCounter(480, 368, 0, Cmdr.HP);
        await View.ShowAsync(new CountersEvent(Unit.Troops, Cmdr.Troops, Unit.HP, Cmdr.HP));

        await SelectSfxAsync(); // :32 \L_00796

        // :33-131 the shot counters move in ten steps
        State.Tmp0002 = 0;
        do
        {
            State.Tmp0003 = State.Tmp0002 * 5 / 11;
            State.Tmp0004 = (Sys3Int)48 + State.Tmp0002;
            State.Tmp0005 = (Sys3Int)416 - State.Tmp0002;
            DrawShotsOrDamage(
                State.Bt_PTroopVsTroopShots, (Sys3Int)640 + State.Tmp0004, 80, State.Bt_PTroopVsTroopShots);
            DrawShotsOrDamage(
                State.Bt_PTroopVsCmdrShots, (Sys3Int)640 + State.Tmp0004, (Sys3Int)100 + State.Tmp0003, State.Bt_PTroopVsCmdrShots);
            DrawShotsOrDamage(
                State.Bt_PCmdrVsTroopShots, (Sys3Int)640 + State.Tmp0004, (Sys3Int)280 - State.Tmp0003, State.Bt_PCmdrVsTroopShots);
            DrawShotsOrDamage(
                State.Bt_PCmdrVsCmdrShots, (Sys3Int)640 + State.Tmp0004, 300, State.Bt_PCmdrVsCmdrShots);
            DrawShotsOrDamage(
                State.Bt_ETroopVsTroopShots, (Sys3Int)640 + State.Tmp0005, 80, State.Bt_ETroopVsTroopShots);
            DrawShotsOrDamage(
                State.Bt_ETroopVsCmdrShots, (Sys3Int)640 + State.Tmp0005, (Sys3Int)100 + State.Tmp0003, State.Bt_ETroopVsCmdrShots);
            DrawShotsOrDamage(
                State.Bt_ECmdrVsTroopShots, (Sys3Int)640 + State.Tmp0005, (Sys3Int)280 - State.Tmp0003, State.Bt_ECmdrVsTroopShots);
            DrawShotsOrDamage(
                State.Bt_ECmdrVsCmdrShots, (Sys3Int)640 + State.Tmp0005, 300, State.Bt_ECmdrVsCmdrShots);
            await View.ShowAsync(new ShotsFrameEvent(State.Tmp0002));
            await PauseAsync(1); // :127
            State.Tmp0002 = State.Tmp0002 + 40;
        }
        while (State.Tmp0002 < 400);

        State.Tmp0002 = State.Tmp0002 - 40;
        await PauseAsync(40); // :133

        // :135 \L_009b8 — second pair of sounds
        State.CallArg = 10; // %#PCMPLAY.ADV with 10 writes nothing
        if (State.Bt_SfxDef2 != 5 || State.Bt_SfxAtk2 != 5)
            await View.ShowAsync(new SoundPairEvent(State.Bt_SfxDef2, State.Bt_SfxAtk2));

        // :136-232 predicted troops/HP and the damage numbers
        if (Unit.Troops != 0)
            DrawCounter((Sys3Int)100 + 640, 0, 0, Unit.Troops - State.Bt_ETroopVsTroopDmg - State.Bt_ECmdrVsTroopDmg);
        if (Cmdr.Troops != 0)
            DrawCounter((Sys3Int)480 + 640, 0, 0, Cmdr.Troops - State.Bt_PTroopVsTroopDmg - State.Bt_PCmdrVsTroopDmg);
        DrawCounter((Sys3Int)0 + 640, 368, 0, Unit.HP - State.Bt_ETroopVsCmdrDmg - State.Bt_ECmdrVsCmdrDmg);
        DrawCounter((Sys3Int)480 + 640, 368, 0, Cmdr.HP - State.Bt_PTroopVsCmdrDmg - State.Bt_PCmdrVsCmdrDmg);
        State.Tmp0003 = State.Tmp0002 * 5 / 11;
        Sys3Int left = (Sys3Int)640 + 48 + State.Tmp0002;
        Sys3Int right = (Sys3Int)640 + 416 - State.Tmp0002;
        DrawShotsOrDamage(State.Bt_PTroopVsTroopShots, left, 80, State.Bt_PTroopVsTroopDmg);
        DrawShotsOrDamage(State.Bt_PTroopVsCmdrShots, left, (Sys3Int)100 + State.Tmp0003, State.Bt_PTroopVsCmdrDmg);
        DrawShotsOrDamage(State.Bt_PCmdrVsTroopShots, left, (Sys3Int)280 - State.Tmp0003, State.Bt_PCmdrVsTroopDmg);
        DrawShotsOrDamage(State.Bt_PCmdrVsCmdrShots, left, 300, State.Bt_PCmdrVsCmdrDmg);
        DrawShotsOrDamage(State.Bt_ETroopVsTroopShots, right, 80, State.Bt_ETroopVsTroopDmg);
        DrawShotsOrDamage(State.Bt_ETroopVsCmdrShots, right, (Sys3Int)100 + State.Tmp0003, State.Bt_ETroopVsCmdrDmg);
        DrawShotsOrDamage(State.Bt_ECmdrVsTroopShots, right, (Sys3Int)280 - State.Tmp0003, State.Bt_ECmdrVsTroopDmg);
        DrawShotsOrDamage(State.Bt_ECmdrVsCmdrShots, right, 300, State.Bt_ECmdrVsCmdrDmg);
        await View.ShowAsync(new ExchangeResultsEvent());

        Rnd(2); // :234, result never used

        // :235-246 wiped-out marks
        var marks = new WipedOutMarksEvent(
            Unit.Troops != 0 && Unit.Troops - State.Bt_ETroopVsTroopDmg - State.Bt_ECmdrVsTroopDmg == 0,
            Cmdr.Troops != 0 && Cmdr.Troops - State.Bt_PTroopVsTroopDmg - State.Bt_PCmdrVsTroopDmg == 0,
            Unit.HP != 0 && Unit.HP - State.Bt_ETroopVsCmdrDmg - State.Bt_ECmdrVsCmdrDmg == 0,
            Cmdr.HP != 0 && Cmdr.HP - State.Bt_PTroopVsCmdrDmg - State.Bt_PCmdrVsCmdrDmg == 0);
        if (marks.PlayerTroops)
            CountCgLoad(889);
        if (marks.EnemyTroops)
            CountCgLoad(890);
        if (marks.PlayerCmdr)
            CountCgLoad(891);
        if (marks.EnemyCmdr)
            CountCgLoad(892);
        await View.ShowAsync(marks);
        await View.ShowAsync(new KeyWaitEvent());
        Sys3Int key = await Input.WaitForKeyAsync(); // :247 IK 0
        State.RetVal = key;
    }

    /// <summary>
    /// A counter block of <c>FIGTATTK.ADV</c> (<c>\L_009e4</c>) or of <c>L_02c00</c>
    /// (<c>\L_04ed0</c>): <c>Ui_MouseX</c>, <c>Ui_MouseY</c>, <c>Tmp0021 = 5</c>,
    /// <c>Tmp0022</c> = style, <c>Tmp0023</c> = value, then <see cref="DrawNumber"/>.
    /// </summary>
    private void DrawCounter(Sys3Int x, Sys3Int y, Sys3Int style, Sys3Int value)
    {
        State.Ui_MouseX = x;
        State.Ui_MouseY = y;
        State.Tmp0021 = 5;
        State.Tmp0022 = style;
        State.Tmp0023 = value;
        DrawNumber();
    }

    /// <summary>A <c>{Bt_*Shots: ...}</c> block of <c>FIGTATTK.ADV</c>: draws style-1 <paramref name="value"/> when <paramref name="shots"/> ≠ 0.</summary>
    private void DrawShotsOrDamage(Sys3Int shots, Sys3Int x, Sys3Int y, Sys3Int value)
    {
        if (shots != 0)
            DrawCounter(x, y, 1, value);
    }

    /// <summary>
    /// <c>L_00796</c> (<c>FIGTATTK.ADV:256-345</c>) — selects <c>Bt_SfxAtk1/2</c> and
    /// <c>Bt_SfxDef1/2</c>, resets <c>Bt_SkillSfx</c> to 0 and plays the first pair. 02 §9.3.
    /// </summary>
    private async Task SelectSfxAsync()
    {
        State.Bt_SfxAtk1 = 5;
        State.Bt_SfxAtk2 = 5;
        State.Bt_SfxDef1 = 5;
        State.Bt_SfxDef2 = 5;
        if (State.Bt_PlayerAction == 0)
        {
            State.Bt_SfxAtk1 = 6;
            State.Bt_SfxAtk2 = 7;
        }

        if (State.Bt_PlayerAction == 1)
        {
            State.Bt_SfxAtk1 = 10;
            State.Bt_SfxAtk2 = 11;
            if (State.Unit_Cur == 16)
            {
                State.Bt_SfxAtk1 = 20;
                State.Bt_SfxAtk2 = 21;
            }
        }

        if (State.Bt_PlayerAction == 2)
        {
            State.Bt_SfxAtk1 = 14;
            State.Bt_SfxAtk2 = 19;
        }

        if (State.Bt_PlayerAction == 99)
        {
            State.Bt_SfxAtk1 = 5;
            State.Bt_SfxAtk2 = 5;
        }

        if (State.Unit_Cur == 51)
        {
            State.Bt_SfxAtk1 = 10;
            State.Bt_SfxAtk2 = 11;
        }

        if (State.Bt_EnemyAction == 0)
        {
            State.Bt_SfxDef1 = 8;
            State.Bt_SfxDef2 = 9;
        }

        if (State.Bt_EnemyAction == 1)
        {
            State.Bt_SfxDef1 = 12;
            State.Bt_SfxDef2 = 13;
        }

        if (State.Bt_EnemyAction == 2)
        {
            State.Bt_SfxDef1 = 14;
            State.Bt_SfxDef2 = 19;
        }

        if (State.Bt_EnemyAction == 99)
        {
            State.Bt_SfxDef1 = 5;
            State.Bt_SfxDef2 = 5;
        }

        foreach (var (skill, atk1, atk2) in SkillSfx)
        {
            if (State.Bt_SkillSfx == skill)
            {
                State.Bt_SfxAtk1 = atk1;
                State.Bt_SfxAtk2 = atk2;
            }
        }

        State.Bt_SkillSfx = 0;
        State.CallArg = 10; // :342, %#PCMPLAY.ADV with 10 writes nothing
        await View.ShowAsync(new SoundPairEvent(State.Bt_SfxAtk1, State.Bt_SfxDef1)); // :344 SQ
    }

    /// <summary><c>Bt_SkillSfx</c> → attack sounds, <c>FIGTATTK.ADV:301-336</c>.</summary>
    private static readonly (ushort Skill, ushort Atk1, ushort Atk2)[] SkillSfx =
    [
        (1, 31, 32), (2, 33, 34), (3, 39, 40), (4, 35, 36), (5, 41, 42),
        (6, 43, 44), (7, 39, 40), (8, 43, 44), (9, 35, 36),
    ];

    /// <summary>
    /// <c>L_04762</c> (<c>FIGT.ADV:2858-2904</c>) — removes player slot <c>CallArg</c> (1-4) and
    /// shifts the later slots up (unit, state, order, focus). 02 §7.5, 01 §9.4.
    /// </summary>
    internal void CompactPlayer()
    {
        // QUIRK: 02 Q15 — Bt_PlayerOrder4 is cleared only when slot 4 itself falls.
        if (State.CallArg == 1)
        {
            State.Bt_PlayerSlot1 = State.Bt_PlayerSlot2;
            State.Bt_PSlot1State = State.Bt_PSlot2State;
            State.Bt_PlayerOrder1 = State.Bt_PlayerOrder2;
            State.Bt_PSlot1Focus = State.Bt_PSlot2Focus;
            State.Bt_PlayerSlot2 = State.Bt_PlayerSlot3;
            State.Bt_PSlot2State = State.Bt_PSlot3State;
            State.Bt_PlayerOrder2 = State.Bt_PlayerOrder3;
            State.Bt_PSlot2Focus = State.Bt_PSlot3Focus;
            State.Bt_PlayerSlot3 = State.Bt_PlayerSlot4;
            State.Bt_PSlot3State = State.Bt_PSlot4State;
            State.Bt_PlayerOrder3 = State.Bt_PlayerOrder4;
            State.Bt_PSlot3Focus = State.Bt_PSlot4Focus;
            State.Bt_PlayerSlot4 = 0;
            State.Bt_PSlot4State = 0;
            State.Bt_PSlot4Focus = 0;
        }

        if (State.CallArg == 2)
        {
            State.Bt_PlayerSlot2 = State.Bt_PlayerSlot3;
            State.Bt_PSlot2State = State.Bt_PSlot3State;
            State.Bt_PlayerOrder2 = State.Bt_PlayerOrder3;
            State.Bt_PSlot2Focus = State.Bt_PSlot3Focus;
            State.Bt_PlayerSlot3 = State.Bt_PlayerSlot4;
            State.Bt_PSlot3State = State.Bt_PSlot4State;
            State.Bt_PlayerOrder3 = State.Bt_PlayerOrder4;
            State.Bt_PSlot3Focus = State.Bt_PSlot4Focus;
            State.Bt_PlayerSlot4 = 0;
            State.Bt_PSlot4State = 0;
            State.Bt_PSlot4Focus = 0;
        }

        if (State.CallArg == 3)
        {
            State.Bt_PlayerSlot3 = State.Bt_PlayerSlot4;
            State.Bt_PSlot3State = State.Bt_PSlot4State;
            State.Bt_PlayerOrder3 = State.Bt_PlayerOrder4;
            State.Bt_PSlot3Focus = State.Bt_PSlot4Focus;
            State.Bt_PlayerSlot4 = 0;
            State.Bt_PSlot4State = 0;
            State.Bt_PSlot4Focus = 0;
        }

        if (State.CallArg == 4)
        {
            State.Bt_PlayerSlot4 = 0;
            State.Bt_PSlot4State = 0;
            State.Bt_PlayerOrder4 = 0;
            State.Bt_PSlot4Focus = 0;
        }
    }

    /// <summary>
    /// <c>L_04864</c> (<c>FIGT.ADV:2905-2934</c>) — removes enemy slot <c>CallArg</c> (1-4) and
    /// shifts the later slots up (commander, state). 02 §7.5, 01 §9.4.
    /// </summary>
    internal void CompactEnemy()
    {
        if (State.CallArg == 1)
        {
            State.Bt_EnemySlot1 = State.Bt_EnemySlot2;
            State.Bt_ESlot1State = State.Bt_ESlot2State;
            State.Bt_EnemySlot2 = State.Bt_EnemySlot3;
            State.Bt_ESlot2State = State.Bt_ESlot3State;
            State.Bt_EnemySlot3 = State.Bt_EnemySlot4;
            State.Bt_ESlot3State = State.Bt_ESlot4State;
            State.Bt_EnemySlot4 = 0;
            State.Bt_ESlot4State = 0;
        }

        if (State.CallArg == 2)
        {
            State.Bt_EnemySlot2 = State.Bt_EnemySlot3;
            State.Bt_ESlot2State = State.Bt_ESlot3State;
            State.Bt_EnemySlot3 = State.Bt_EnemySlot4;
            State.Bt_ESlot3State = State.Bt_ESlot4State;
            State.Bt_EnemySlot4 = 0;
            State.Bt_ESlot4State = 0;
        }

        if (State.CallArg == 3)
        {
            State.Bt_EnemySlot3 = State.Bt_EnemySlot4;
            State.Bt_ESlot3State = State.Bt_ESlot4State;
            State.Bt_EnemySlot4 = 0;
            State.Bt_ESlot4State = 0;
        }

        if (State.CallArg == 4)
        {
            State.Bt_EnemySlot4 = 0;
            State.Bt_ESlot4State = 0;
        }
    }

    // ------------------------------------------------------------------------------------
    // Small shared helpers
    // ------------------------------------------------------------------------------------

    /// <summary><c>Unit_StrBase + (Unit_Cur - 1) * 3 + k</c> — string k (0 name, 1 troop type, 2 extra) of <c>Unit_Cur</c>.</summary>
    private Sys3Int UnitString(Sys3Int k) => State.Unit_StrBase + (State.Unit_Cur - 1) * 3 + k;

    /// <summary><c>Cmdr_StrBase + (Cmdr_Cur - 1) * 3 + k</c> — string k of <c>Cmdr_Cur</c>.</summary>
    private Sys3Int CmdrString(Sys3Int k) => State.Cmdr_StrBase + (State.Cmdr_Cur - 1) * 3 + k;

    /// <summary><c>MM dst, src</c> — copies string variable <paramref name="src"/> into <paramref name="dst"/>.</summary>
    private void CopyString(Sys3Int dst, Sys3Int src) => State.Strings[(int)dst] = State.Strings[(int)src];
}
