using Kichikuou.Engine.Script;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Tﾍﾙﾏﾝ2.ADV</c>, the defenders of a Helman province the player attacks (part N2, milestone 3
/// step 1; called by <c>Cﾃｷﾄｼ.ADV:347-349</c> before <c>FIGT_A</c>), ported one-to-one on the nation
/// template (<c>docs/strategy/survey-nations.md</c> §2.5). Its first run plays <c>Gｽﾃｯｾﾙ</c> 1,
/// which starts the Helman war. Once Helman has fallen every province is held by its people (the
/// militia, Civilian Generals 58 and 59 with <c>100 + RND(800)</c> troops; <c>Gｼｰﾗ</c> 2 may call
/// the attack off); before, the province decides: the 1st army (the stronger of its two orders) at
/// Rosesgrad (39), Siberia (38), Golak (34), Log A (32), Log B (31), Volgo Z (30), Maunes (13),
/// North (2) and Archgrad (36), the 2nd at Sudori 17 (46) through Laurengrad (48), the 3rd at Lang
/// Bau (45) and Micrograd (44), the 4th at Pawn (40), Sudori 13 (41) and Cossack (37), the 5th at
/// Sudori 10 (42), Ruberan (commander 31) at the Ancient Ruins (35), the militia anywhere else or
/// when the army's leader is dead. The filler leaves out commanders under 190 troops once slot 1
/// is taken, except at Lang Bau (45). The army name (<c>MS 10</c>) is read from the player's files
/// through <paramref name="scenario"/>.
/// </summary>
public sealed class HelmanDefenders(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "Tﾍﾙﾏﾝ2.ADV", scenario)
{
    /// <summary><c>Tﾍﾙﾏﾝ2.ADV:1-60</c>, then the label the province leads to.</summary>
    public async Task RunAsync()
    {
        State.Ev_PhaseNation = 4;                                       // :1
        if (State.F_StesselSheila_Stage == 0)                           // :2-5 the first attack starts the war
            await Script.EventAsync("Gｽﾃｯｾﾙ.ADV", 1);
        if ((State.Prov_Cur == 44 || State.Prov_Cur == 48 || State.Prov_Cur == 42) && State.F_Mineva_State == 0)
            State.F_Mineva_State = 1;                                   // :6-8
        if (State.F_Helman_War == 2)                                    // :9-26 Helman has fallen
        {
            SetLiteral(11);                                             // MS 10, "A Certain Country's People"
            State.Unit_Cur = 43;
            if (State.F_Sheila_State != 0 && State.F_SheilaDevotees_State != 0 && Unit.Status == 0)
            {
                await Script.EventAsync("Gｼｰﾗ.ADV", 2);
                if (State.Tmp0001 == 1)                                 // the attack is called off
                {
                    State.Bt_EnemyParty1 = 0;
                    return;
                }
            }
            if (State.F_SheilaDevotees_State == 0)
                await Script.EventAsync("Gｼｰﾗ.ADV", 1);
            Militia();                                                  // @L_00210
            return;
        }
        SetLiteral(28);                                                 // :27-28 MS 10, "Helman Army"
        State.Bt_TroopLimit = Prov.BattleArea;                          // :29-30 QUIRK: NAT-16 — FIGT_A recomputes the limit
        State.Bt_WallBonus = Prov.Defense;
        if (Prov.Owner == 5)                                            // :31-33 QUIRK: NAT-16 — dead: Cﾃｷﾄｼ calls this file for owner 4 only
        {
            await PattonAsync();
            return;
        }
        switch (State.Prov_Cur.Value)                                   // :34-60
        {
            case 40 or 41 or 37:
                await FourthArmyAsync();
                return;
            case 39 or 38 or 34 or 32 or 31 or 30 or 13 or 2:
                FirstArmy();
                return;
            case 46 or 47 or 48:
                SecondArmy();
                return;
            case 45 or 44:
                ThirdArmy();
                return;
            case 36 when State.F_Lelyukov_State == 1:
                LelyukovAlone();
                return;
            case 36:
                FirstArmy();
                return;
            case 42:
                FifthArmy();
                return;
            case 35:
                await RuberanAsync();
                return;
            default:
                Militia();                                              // falls into L_00210
                return;
        }
    }

    /// <summary>
    /// The filler's filter (<c>Tﾍﾙﾏﾝ2.ADV:358-360</c>): once slot 1 is taken, commanders with fewer
    /// than 190 troops stay home — except at Lang Bau (45).
    /// </summary>
    protected override bool SkipsCommander() =>
        State.Prov_Cur != 45 && Cmdr.Troops < 190 && State.Bt_EnemyParty1 != 0;

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:61-77</c> (<c>L_00210</c>) — the militia: Civilian Generals 58 and 59 called up
    /// with <c>100 + RND(800)</c> troops each and healed, alone in the party, retreat mode 1.
    /// </summary>
    private void Militia()
    {
        CallUpCivilians();
        State.Bt_EnemyParty1 = 58;
        State.Bt_EnemyParty2 = 59;
        State.Bt_EnemyParty3 = 0;
        State.Bt_EnemyParty4 = 0;
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary><c>Tﾍﾙﾏﾝ2.ADV:62-71</c>, <c>:171-180</c> — Civilian Generals 58 and 59: alive, <c>100 + RND(800)</c> troops, full HP.</summary>
    private void CallUpCivilians()
    {
        State.Cmdr_Cur = 58;
        Cmdr.Status = 1;
        Script.Rnd(800);
        Cmdr.Troops = 100 + State.RetVal;
        Cmdr.HP = Cmdr.MaxHP;
        State.Cmdr_Cur = 59;
        Cmdr.Status = 1;
        Script.Rnd(800);
        Cmdr.Troops = 100 + State.RetVal;
        Cmdr.HP = Cmdr.MaxHP;
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:78-142</c> (<c>L_0026c</c>) — the 1st army, led by Lelyukov (commander 22): the
    /// militia once he has fallen back (<c>F_Lelyukov_State = 1</c>) or died; else the stronger of
    /// its two orders (each picked and summed; on a tie the second stays), Laumiere (27) alone at
    /// Volgo Z (30) while she lives; retreat mode 4, 1 at Rosesgrad (39) and at the passes Log B
    /// (31) and Log A (32) against a single unit.
    /// </summary>
    private void FirstArmy()
    {
        State.Cmdr_Cur = 22;
        if (State.F_Lelyukov_State == 1 || Cmdr.Status == 0)
        {
            Militia();                                                  // @L_00210
            return;
        }
        SetPriority(22, 23, 24, 25, 29, 30, 27, 28, 26);
        PickArmy();                                                     // \L_006b7
        SumTroops();                                                    // \L_00671
        State.Tmp0011 = State.CallArg;
        SetPriority(27, 28, 29, 30, 24, 25, 22, 23, 26);
        PickArmy();
        SumTroops();
        State.Tmp0012 = State.CallArg;
        if (State.Tmp0011 > State.Tmp0012)
        {
            SetPriority(22, 23, 24, 25, 29, 30, 27, 28, 26);
            PickArmy();
        }
        State.Cmdr_Cur = 27;
        if (Cmdr.Status != 0 && State.Prov_Cur == 30)
        {
            SetPriority(27);
            PickArmy();
        }
        State.Bt_EnemyRetreatMode = 4;
        if (State.Prov_Cur == 39)
            State.Bt_EnemyRetreatMode = 1;
        if ((State.Prov_Cur == 31 || State.Prov_Cur == 32) && State.Bt_PlayerParty2 == 0)
            State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:143-162</c> (<c>L_003b2</c>) — the 2nd army, led by Aristoles (commander 32): the
    /// militia once he is dead; retreat mode 4, 1 at Laurengrad (48).
    /// </summary>
    private void SecondArmy()
    {
        State.Cmdr_Cur = 32;
        if (Cmdr.Status == 0)
        {
            Militia();                                                  // @L_00210
            return;
        }
        SetPriority(34, 35, 36, 37, 38, 32, 33);
        PickArmy();
        State.Bt_EnemyRetreatMode = 4;
        if (State.Prov_Cur == 48)
            State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:163-199</c> (<c>L_00410</c>) — the 3rd army, led by Mineva (commander 39): the
    /// militia at Micrograd (44) before she is met there, or once she is dead; else the militia is
    /// called up anyway (two draws), joining her army only at Lang Bau (45); retreat mode 4, 1 at
    /// Lang Bau (45).
    /// </summary>
    private void ThirdArmy()
    {
        if (State.F_Mineva_State == 0 && State.Prov_Cur == 44)
        {
            Militia();                                                  // @L_00210
            return;
        }
        State.Cmdr_Cur = 39;
        if (Cmdr.Status == 0)
        {
            Militia();
            return;
        }
        CallUpCivilians();
        if (State.Prov_Cur == 45)
            SetPriority(42, 43, 39, 40, 41, 58, 59);
        else
            SetPriority(42, 43, 39, 40, 41);
        PickArmy();
        State.Bt_EnemyRetreatMode = 4;
        if (State.Prov_Cur == 45)
            State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:200-225</c> (<c>L_004de</c>) — the 4th army: the militia when Nero has left
    /// (<c>F_Nero_Helman4thState = 100</c>) and Cream is met, or when nobody of the army is left;
    /// retreat mode 4, 1 at Cossack (37); Nero's first scene before the battle.
    /// </summary>
    private async Task FourthArmyAsync()
    {
        if (State.F_Nero_Helman4thState == 100 && State.F_Cream_State != 0)
        {
            Militia();                                                  // @L_00210
            return;
        }
        SetPriority(49, 46, 47, 44, 45, 48);
        PickArmy();
        if (State.Bt_EnemyParty1 == 0)
        {
            Militia();
            return;
        }
        State.Bt_EnemyRetreatMode = 4;
        if (State.Prov_Cur == 37)
            State.Bt_EnemyRetreatMode = 1;
        if (State.F_Nero_Helman4thState == 0)
            await Script.EventAsync("Gﾈﾛ.ADV", 1);
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:226-235</c> (<c>L_00569</c>) — Archgrad (36) after Lelyukov fell back: he alone,
    /// with 10 troops, retreat mode 1; <c>F_Lelyukov_State := 10</c>.
    /// </summary>
    private void LelyukovAlone()
    {
        State.F_Lelyukov_State = 10;
        State.Bt_EnemyParty1 = 22;
        State.Bt_EnemyParty2 = 0;
        State.Bt_EnemyParty3 = 0;
        State.Bt_EnemyParty4 = 0;
        State.Cmdr_Cur = 22;
        Cmdr.Troops = 10;
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:236-251</c> (<c>L_00594</c>) — the 5th army (Alexei 53, Rolex 50, Hyam 47,
    /// Saulnia 52, Katyusha 51) at Sudori 10 (42); <c>F_Rolex_State</c> raised to 4; retreat mode 1.
    /// </summary>
    private void FifthArmy()
    {
        if (State.F_Rolex_State < 4)
            State.F_Rolex_State = 4;
        SetPriority(53, 50, 47, 52, 51);
        PickArmy();
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:252-268</c> (<c>L_005db</c>) — Patton's army (54-57), retreat mode 1, his scene
    /// 4 when <c>F_Patton_State = 10</c>. Unreachable (NAT-16).
    /// </summary>
    private async Task PattonAsync()
    {
        SetPriority(54, 55, 56, 57);
        PickArmy();
        State.Bt_EnemyRetreatMode = 1;
        if (State.F_Patton_State == 10)
            await Script.EventAsync("Gﾊﾟｯﾄﾝ.ADV", 4);
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ2.ADV:269-285</c> (<c>L_00628</c>) — the Ancient Ruins (35): Ruberan (commander 31)
    /// alone, retreat mode 1, his first scene before the battle.
    /// </summary>
    private async Task RuberanAsync()
    {
        SetPriority(31);
        PickArmy();
        State.Bt_EnemyRetreatMode = 1;
        if (State.F_Ruberan_State == 0)
            await Script.EventAsync("Gﾙｰﾍﾞﾗﾝ.ADV", 1);
    }
}
