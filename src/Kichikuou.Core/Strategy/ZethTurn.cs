using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Tｾﾞｽ.ADV</c>, the Zeth Kingdom's weekly turn while the war is on (part N2, milestone 3 step 2;
/// called by <c>MAIN.ADV:592</c> while <c>F_Zeth_War = 1</c>), ported one-to-one on the nation
/// template (<c>docs/strategy/survey-nations.md</c> §2.2-2.4). First the war's story scenes: Ragarl's
/// ritual (<c>Gﾅｷﾞ</c> 1-3, <c>Gﾊﾟﾊﾟｲｱ</c> 1) and the war's own (<c>Iｾﾞｽ</c> 2-6 by <c>F_Zeth_State</c>;
/// at state 2 the state moves on to 4 and Saias's army marches alone that week), Magic's
/// <c>Gﾏｼﾞｯｸ</c> 3. Then each of the four ministers' armies — Chizuko's (101) Zamoor (165) … Annis
/// (168), Papaya's (104) Backslock (169) … Am (172), Nagi's (107) Lawson (173) … Hiroko (176),
/// Magic's (110) Kenichi (177) … Rue (180) — marches on one roll in two while its minister lives
/// (not in the week <c>Iｾﾞｽ</c> 2 opens the war), with her scene before its battle. Then the
/// generals: a roll in seven plays one of <c>Iｾﾞｽ</c> 30-36 and sends one general's army (all four
/// on 7) if the generals it names live, else up to two rolls in four send one general's army —
/// Saias (113), Uspira (117), Kabachahn (121), Alex (125), each reviving one of his three officers
/// on a roll in three first and marching on the first of fifteen provinces from Chizuko's Tower
/// (68) to Leazas Castle (1) the player holds — and at last the ministers' scenes (<c>Gﾊﾟﾊﾟｲｱ</c> 8,
/// <c>Gﾁｽﾞｺ</c> 4/5, <c>Gﾅｷﾞ</c> 21, <c>Gﾏｼﾞｯｸ</c> 21/22). Retreat mode 4 throughout; nobody is left
/// out of a pick. The army name (<c>MS 10</c>) is read from the player's files through
/// <paramref name="scenario"/>.
/// </summary>
public sealed class ZethTurn(ScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "Tｾﾞｽ.ADV", scenario)
{
    /// <summary><c>Tｾﾞｽ.ADV:1-237</c>, up to its <c>%0:</c> (<c>L_0063d</c>).</summary>
    public async Task RunAsync()
    {
        State.Ev_PhaseNation = 7;                                       // :1
        SetLiteral(3);                                                  // :2-3 MS 10, "Zeth Army"
        if (State.F_RagarlRitual_State == 4 || State.F_RagarlRitual_State == 5)
            await Script.EventAsync("Gﾊﾟﾊﾟｲｱ.ADV", 1);                   // :4-19 Ragarl's ritual
        if (State.F_RagarlRitual_State == 3)
            await Script.EventAsync("Gﾅｷﾞ.ADV", 3);
        if (State.F_RagarlRitual_State == 2)
            await Script.EventAsync("Gﾅｷﾞ.ADV", 2);
        if (State.F_RagarlRitual_State == 1)
            await Script.EventAsync("Gﾅｷﾞ.ADV", 1);
        if (State.F_Zeth_State == 10)                                   // :20-35 the war's scenes
            await Script.EventAsync("Iｾﾞｽ.ADV", 5);
        if (State.F_Zeth_State == 8)
            await Script.EventAsync("Iｾﾞｽ.ADV", 6);
        if (State.F_Zeth_State == 5)
            await Script.EventAsync("Iｾﾞｽ.ADV", 4);
        if (State.F_Zeth_State == 4)
            await Script.EventAsync("Iｾﾞｽ.ADV", 3);
        if (State.F_Zeth_State == 2)                                    // :36-44 Saias's army alone
        {
            State.F_Zeth_State = 4;
            State.Cmdr_Cur = 113;
            if (Cmdr.Status == 0)
                return;                                                 // %0:
            await SaiasArmyAsync();
            return;
        }
        if (State.F_Zeth_State == 1)                                    // :45-48 Iｾﾞｽ 2 sets the state to 2 (Iｾﾞｽ.ADV:2900)
            await Script.EventAsync("Iｾﾞｽ.ADV", 2);
        State.Cmdr_Cur = 110;                                           // :49-53 Magic
        if (State.F_Magic_State == 1 && Cmdr.Status != 0)
            await Script.EventAsync("Gﾏｼﾞｯｸ.ADV", 3);
        if (State.F_Zeth_State != 2)                                    // :54-75 the ministers' armies
        {
            Script.Rnd(2);
            State.Cmdr_Cur = 101;
            if (Cmdr.Status != 0 && State.RetVal == 1)
                await ChizukoArmyAsync();                               // \L_00a5e
            Script.Rnd(2);
            State.Cmdr_Cur = 104;
            if (Cmdr.Status != 0 && State.RetVal == 1)
                await PapayaArmyAsync();                                // \L_00bb1
            Script.Rnd(2);
            State.Cmdr_Cur = 107;
            if (Cmdr.Status != 0 && State.RetVal == 1)
                await NagiArmyAsync();                                  // \L_00d04
            Script.Rnd(2);
            State.Cmdr_Cur = 110;
            if (Cmdr.Status != 0 && State.RetVal == 1)
                await MagicArmyAsync();                                 // \L_00e55
        }
        State.Tmp0001 = 0;                                              // :76-95 which generals live
        State.Tmp0002 = 0;
        State.Tmp0003 = 0;
        State.Tmp0004 = 0;
        State.Cmdr_Cur = 113;
        if (Cmdr.Status != 0)
            State.Tmp0001 = 1;
        State.Cmdr_Cur = 117;
        if (Cmdr.Status != 0)
            State.Tmp0002 = 1;
        State.Cmdr_Cur = 121;
        if (Cmdr.Status != 0)
            State.Tmp0003 = 1;
        State.Cmdr_Cur = 125;
        if (Cmdr.Status != 0)
            State.Tmp0004 = 1;
        Script.Rnd(7);                                                  // :96 the generals' scene
        if (State.F_Zeth_State == 2)                                    // :97-99 the week Iｾﾞｽ 2 played: no scene
            State.RetVal = 10;
        if (State.RetVal == 1 && State.Tmp0004 == 1 && State.Tmp0001 == 1 && State.Tmp0002 == 1)
        {
            await Script.EventAsync("Iｾﾞｽ.ADV", 33);                    // :100-105
            await AlexArmyAsync();
            return;
        }
        if (State.RetVal == 2 && State.Tmp0002 == 1 && State.Tmp0003 == 1)
        {
            await Script.EventAsync("Iｾﾞｽ.ADV", 32);                    // :106-111
            await UspiraArmyAsync();
            return;
        }
        if (State.RetVal == 3 && State.Tmp0001 == 1 && State.Tmp0002 == 1 && State.Tmp0004 == 1)
        {
            await Script.EventAsync("Iｾﾞｽ.ADV", 31);                    // :112-117
            await SaiasArmyAsync();
            return;
        }
        if (State.RetVal == 4 && State.Tmp0001 == 1 && State.Tmp0002 == 1)
        {
            await Script.EventAsync("Iｾﾞｽ.ADV", 34);                    // :118-123
            await SaiasArmyAsync();
            return;
        }
        if (State.RetVal == 5 && State.Tmp0004 == 1)
        {
            await Script.EventAsync("Iｾﾞｽ.ADV", 35);                    // :124-129
            await AlexArmyAsync();
            return;
        }
        // QUIRK: NAT-07 — the unit cursor is set, but the status tested is Alex's (125), from :92.
        State.Unit_Cur = 101;                                           // :130
        if (State.RetVal == 6 && State.Tmp0001 == 1 && State.Tmp0003 == 1 && Cmdr.Status != 0)
        {
            await Script.EventAsync("Iｾﾞｽ.ADV", 36);                    // :131-136
            await KabachahnArmyAsync();
            return;
        }
        if (State.RetVal == 7 && State.Tmp0001 == 1 && State.Tmp0002 == 1 && State.Tmp0003 == 1 && State.Tmp0004 == 1)
        {
            await Script.EventAsync("Iｾﾞｽ.ADV", 30);                    // :137-145 all four armies
            await AlexArmyAsync();
            await SaiasArmyAsync();
            await UspiraArmyAsync();
            await KabachahnArmyAsync();
            return;
        }
        if (await OneGeneralAsync())                                    // :146-166
            return;
        if (await OneGeneralAsync())                                    // :167-187
            return;
        State.Cmdr_Cur = 104;                                           // :188-193 Papaya
        if (Cmdr.Status != 0 && State.F_Papaya_State == 1)
        {
            await Script.EventAsync("Gﾊﾟﾊﾟｲｱ.ADV", 8);
            return;
        }
        if (State.F_Zeth_State == 8 || State.F_Zeth_State == 9)         // :194-214
        {
            if (await MinisterSceneAsync(chizuko: 4, nagi: 21, magic: 21))
                return;
        }
        if (State.F_Zeth_State == 10 || State.F_Zeth_State == 11)       // :215-235
        {
            if (await MinisterSceneAsync(chizuko: 5, nagi: 21, magic: 22))
                return;
        }
    }                                                                   // :236-237 L_0063d, %0:

    /// <summary>
    /// <c>Tｾﾞｽ.ADV:146-166</c> (and its copy <c>:167-187</c>): a roll in four names Saias (113),
    /// Uspira (117), Kabachahn (121) or Alex (125), whose army marches if he lives; true if one did.
    /// </summary>
    private async Task<bool> OneGeneralAsync()
    {
        Script.Rnd(4);
        State.Cmdr_Cur = 113;
        if (State.RetVal == 1 && Cmdr.Status != 0)
        {
            await SaiasArmyAsync();
            return true;
        }
        State.Cmdr_Cur = 117;
        if (State.RetVal == 2 && Cmdr.Status != 0)
        {
            await UspiraArmyAsync();
            return true;
        }
        State.Cmdr_Cur = 121;
        if (State.RetVal == 3 && Cmdr.Status != 0)
        {
            await KabachahnArmyAsync();
            return true;
        }
        State.Cmdr_Cur = 125;
        if (State.RetVal == 4 && Cmdr.Status != 0)
        {
            await AlexArmyAsync();
            return true;
        }
        return false;
    }

    /// <summary>
    /// <c>Tｾﾞｽ.ADV:195-213</c> / <c>:216-234</c>: a roll in four; on 1 Chizuko's (101), on 3 Nagi's
    /// (107), on 4 Magic's (110) scene of the given entry if she lives (2, Papaya's, plays nothing);
    /// true if one played.
    /// </summary>
    private async Task<bool> MinisterSceneAsync(Sys3Int chizuko, Sys3Int nagi, Sys3Int magic)
    {
        Script.Rnd(4);
        State.Cmdr_Cur = 101;
        if (Cmdr.Status != 0 && State.RetVal == 1)
        {
            await Script.EventAsync("Gﾁｽﾞｺ.ADV", chizuko);
            return true;
        }
        State.Cmdr_Cur = 107;
        if (Cmdr.Status != 0 && State.RetVal == 3)
        {
            await Script.EventAsync("Gﾅｷﾞ.ADV", nagi);
            return true;
        }
        State.Cmdr_Cur = 110;
        if (Cmdr.Status != 0 && State.RetVal == 4)
        {
            await Script.EventAsync("Gﾏｼﾞｯｸ.ADV", magic);
            return true;
        }
        return false;
    }

    /// <summary>
    /// <c>Tｾﾞｽ.ADV:238-244</c> (<c>L_00640</c>): a dead officer under <c>Cmdr_Cur</c> comes back with
    /// full HP and an eighth of his maximum troops.
    /// </summary>
    private void Revive()
    {
        if (Cmdr.Status == 0)
        {
            Cmdr.Status = 1;
            Cmdr.HP = Cmdr.MaxHP;
            Cmdr.Troops = Cmdr.MaxTroops / 8;
        }
    }

    /// <summary><c>Tｾﾞｽ.ADV:245-271</c> (<c>L_00667</c>) — Saias's (113) army: Dabinch (116), Maskloader (115), Reshie (114).</summary>
    private Task SaiasArmyAsync() => GeneralArmyAsync(116, 115, 114, [116, 115, 114, 113]);

    /// <summary><c>Tｾﾞｽ.ADV:272-298</c> (<c>L_006f3</c>) — Uspira's (117) army: Minamin (120), Dan Cero (119), Starlighter (118).</summary>
    private Task UspiraArmyAsync() => GeneralArmyAsync(118, 119, 120, [120, 119, 118, 117]);

    /// <summary><c>Tｾﾞｽ.ADV:299-325</c> (<c>L_0077f</c>) — Kabachahn's (121) army: Dancelove (124), The Bomber (123), Kyouko (122).</summary>
    private Task KabachahnArmyAsync() => GeneralArmyAsync(122, 123, 124, [124, 123, 122, 121]);

    /// <summary><c>Tｾﾞｽ.ADV:326-352</c> (<c>L_0080b</c>) — Alex's (125) army: Shitareriha (128), Haseras (127), Sanneri (126).</summary>
    private Task AlexArmyAsync() => GeneralArmyAsync(126, 127, 128, [128, 127, 126, 125]);

    /// <summary>
    /// A general's army (<c>L_00667</c> and its three copies): retreat mode 4; a roll in three
    /// revives the officer it names (1, 2, 3 → <paramref name="on1"/>, <paramref name="on2"/>,
    /// <paramref name="on3"/>, <c>L_00640</c>); the army is picked from <paramref name="army"/>
    /// (<c>L_00fee</c>) and marches (<c>L_00897</c>).
    /// </summary>
    private async Task GeneralArmyAsync(int on1, int on2, int on3, int[] army)
    {
        State.Bt_EnemyRetreatMode = 4;
        Script.Rnd(3);
        if (State.RetVal == 1)
        {
            State.Cmdr_Cur = (Sys3Int)on1;
            Revive();
        }
        if (State.RetVal == 2)
        {
            State.Cmdr_Cur = (Sys3Int)on2;
            Revive();
        }
        if (State.RetVal == 3)
        {
            State.Cmdr_Cur = (Sys3Int)on3;
            Revive();
        }
        SetPriority(army);
        PickArmy();                                                     // \L_00fee
        await GeneralsMarchAsync();                                     // \L_00897
    }

    /// <summary>
    /// <c>Tｾﾞｽ.ADV:353-432</c> (<c>L_00897</c>) — the generals' march: with any troops at all, the
    /// first of Chizuko's Tower (68), Papaya's Tower (70), Magic's Tower (66), Nagi's Tower (67),
    /// Nagarmaule (62), Old Zeth (63), Tape (61), Italia (60), Sabasaba (59), Adam's Fortress (58),
    /// Parapara Fortress (16), Rich (15), Maunes (13), North (2), Leazas Castle (1) the player holds
    /// is attacked through <c>FIGT_B</c>.
    /// </summary>
    private async Task GeneralsMarchAsync()
    {
        SumTroops();                                                    // \L_00fa8
        if (State.CallArg == 0)
            return;
        if (await AttackFirstAsync(68, 70, 66, 67, 62, 63, 61, 60, 59, 58, 16, 15, 13, 2, 1))
            return;
        // QUIRK: NAT-20 — no return after the last province: the label falls into Chizuko's army.
        await ChizukoArmyAsync();
    }

    /// <summary>
    /// <c>Tｾﾞｽ.ADV:433-496</c> (<c>L_00a5e</c>) — Chizuko's army, Zamoor (165), Urias (166), Renko
    /// (167), Annis (168), against Papaya's Tower (70), Zeth Palace (69), Nagi's Tower (67),
    /// Nagarmaule (62), Sabasaba (59), Adam's Fortress (58); <c>Gﾁｽﾞｺ</c> 6 before the battle.
    /// </summary>
    private Task ChizukoArmyAsync() =>
        MinisterArmyAsync([165, 166, 167, 168], "Gﾁｽﾞｺ.ADV", 6, 70, 69, 67, 62, 59, 58);

    /// <summary>
    /// <c>Tｾﾞｽ.ADV:497-560</c> (<c>L_00bb1</c>) — Papaya's army, Backslock (169), Beethoven (170),
    /// Jessica (171), Am (172), against Magic's Tower (66), Marc (74), Maginot Stronghold (76), Amber
    /// Castle (65), Old Zeth (63), Bath (64); <c>Gﾊﾟﾊﾟｲｱ</c> 9 before the battle.
    /// </summary>
    private Task PapayaArmyAsync() =>
        MinisterArmyAsync([169, 170, 171, 172], "Gﾊﾟﾊﾟｲｱ.ADV", 9, 66, 74, 76, 65, 63, 64);

    /// <summary>
    /// <c>Tｾﾞｽ.ADV:561-624</c> (<c>L_00d04</c>) — Nagi's army, Lawson (173), Mustard (174), Mayumi
    /// (175), Hiroko (176), against Magic's Tower (66), Zeth Palace (69), Nagarmaule (62), Sabasaba
    /// (59), Adam's Fortress (58), Parapara Fortress (16); <c>Gﾅｷﾞ</c> 22 before the battle.
    /// </summary>
    private Task NagiArmyAsync() =>
        MinisterArmyAsync([173, 174, 175, 176], "Gﾅｷﾞ.ADV", 22, 66, 69, 62, 59, 58, 16);

    /// <summary>
    /// <c>Tｾﾞｽ.ADV:625-688</c> (<c>L_00e55</c>) — Magic's army, Kenichi (177), Takeege (178), Suppli
    /// (179), Rue (180), against Nagi's Tower (67), Old Zeth (63), Tape (61), Italia (60), Sabasaba
    /// (59), Adam's Fortress (58); <c>Gﾏｼﾞｯｸ</c> 24 before the battle.
    /// </summary>
    private Task MagicArmyAsync() =>
        MinisterArmyAsync([177, 178, 179, 180], "Gﾏｼﾞｯｸ.ADV", 24, 67, 63, 61, 60, 59, 58);

    /// <summary>
    /// A minister's army (<c>L_00a5e</c> and its three copies): retreat mode 4; the army picked from
    /// <paramref name="army"/> (<c>L_00fee</c>) marches if anyone is in it, on the first of
    /// <paramref name="provinces"/> the player holds — the minister's scene first, then the
    /// province set again and <c>FIGT_B</c>.
    /// </summary>
    private async Task MinisterArmyAsync(int[] army, string scene, Sys3Int entry, params int[] provinces)
    {
        State.Bt_EnemyRetreatMode = 4;
        SetPriority(army);
        PickArmy();                                                     // \L_00fee
        if (State.Bt_EnemyParty1 == 0)
            return;
        foreach (int province in provinces)
        {
            State.Prov_Cur = (Sys3Int)province;
            if (Prov.Owner == 1)
            {
                await Script.EventAsync(scene, entry);
                State.Prov_Cur = (Sys3Int)province;
                await Script.CallFileAsync("FIGT_B.ADV");
                return;
            }
        }
    }
}
