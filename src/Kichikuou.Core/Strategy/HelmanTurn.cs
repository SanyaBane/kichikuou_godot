using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Tﾍﾙﾏﾝ.ADV</c>, the Helman Empire's weekly turn while the war is on (part N2, milestone 3 step 1;
/// called by <c>MAIN.ADV:452</c>), ported one-to-one on the nation template
/// (<c>docs/strategy/survey-nations.md</c> §2.3-2.4). First the week's scenes: Stessel and Sheila
/// by the weeks since the war began, Tainyar (commander 26) back while Log B (31) and Sudori 13 (41)
/// are not the player's, Freya, Lelyukov's retreat or his fall-back from Rosesgrad (39), Aristoles,
/// Patton's two scenes (one draw, every turn). Then every army whose leader lives marches, in
/// order, each attacking the first province of its list the player holds through <c>FIGT_B</c>: the
/// 1st (the stronger of its two orders, whatever its strength; at Maunes (13) one roll between
/// <c>Gﾚﾘｭｰｺﾌ</c> 5 and Tainyar alone; Maunes (13) and North (2) taken from the monsters without a
/// battle), the 2nd (Aristoles's three-week wait), the 3rd (Mineva's army re-picked without
/// Capricorn (184) and Ortega (185) at Micrograd (44)), the 4th (Nero's states; a smaller army
/// once he has turned), the 5th (Rolex's two rolls and scenes before it marches) and the Crusaders
/// (Olhound (60) alone, <c>500 + RND(500)</c> troops). <c>Gｽﾃｯｾﾙ</c> 14 closes a week where nothing
/// happened. The filler leaves out commanders under 190 troops once slot 1 is taken. The army
/// names (<c>MS 10</c>) are read from the player's files through <paramref name="scenario"/>.
/// </summary>
public sealed class HelmanTurn(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "Tﾍﾙﾏﾝ.ADV", scenario)
{
    /// <summary><c>Tﾍﾙﾏﾝ.ADV:1-98</c>, up to its <c>%0:</c>.</summary>
    public async Task RunAsync()
    {
        State.Ev_PhaseNation = 4;                                       // :1
        SetLiteral(3);                                                  // :2-3 MS 10, "Helman Army"
        State.Ev_WeeklyEventDone = 0;                                   // :4
        if (State.Week > 15 + State.Stessel_StartWeek && State.F_StesselSheila_Stage == 4)
            await WeeklyEventAsync("Gｽﾃｯｾﾙ.ADV", 6);                     // :5-9
        if (State.Week > 9 + State.Stessel_StartWeek && State.F_StesselSheila_Stage == 3)
            await WeeklyEventAsync("Gｽﾃｯｾﾙ.ADV", 5);                     // :10-14
        if (State.Week > 6 + State.Stessel_StartWeek && State.F_StesselSheila_Stage == 2)
            await WeeklyEventAsync("Gｽﾃｯｾﾙ.ADV", 4);                     // :15-19
        if (State.Week > 2 + State.Stessel_StartWeek && State.F_StesselSheila_Stage == 1)
            await WeeklyEventAsync("Gｽﾃｯｾﾙ.ADV", 3);                     // :20-24
        State.Cmdr_Cur = 26;                                            // :25-36 Tainyar back
        if (Cmdr.Status == 0)
        {
            State.Prov_Cur = 31;
            if (Prov.Owner != 1)
            {
                State.Prov_Cur = 41;
                if (Prov.Owner != 1)
                {
                    Cmdr.Status = 1;
                    Cmdr.HP = Cmdr.MaxHP;
                    Cmdr.Troops = Cmdr.MaxTroops;
                }
            }
        }
        if (State.N_Freya_State != 10 && State.N_Freya_State != 20 && State.N_Freya_State != 12)
            await Script.EventAsync("Gﾌﾚｲｱ.ADV", 1);                     // :37-40
        if (State.F_Lelyukov_State == 1)
            await WeeklyEventAsync("Gﾚﾘｭｰｺﾌ.ADV", 2);                    // :41-45
        State.Prov_Cur = 31;                                            // :46-54
        if (State.F_Lelyukov_State == 0 && Prov.Owner == 1)
        {
            State.Prov_Cur = 39;
            if (Prov.Owner == 4)
                await WeeklyEventAsync("Gﾚﾘｭｰｺﾌ.ADV", 1);
        }
        if (State.F_Patton_State > 11 && State.F_Aristoles_Helman2ndState == 0)
            await WeeklyEventAsync("Gｱﾘｽﾄﾚｽ.ADV", 1);                   // :55-59
        Script.Rnd(2);                                                  // :60
        // QUIRK: NAT-08 — Patton's scenes 2 and 12 need another scene to have played this week.
        if (State.F_Patton_State == 1 && State.RetVal == 1 && State.Ev_WeeklyEventDone != 0)
            await WeeklyEventAsync("Gﾊﾟｯﾄﾝ.ADV", 2);                     // :61-65
        State.Prov_Cur = 40;                                            // :66-71
        if (State.F_Patton_State == 2 && State.RetVal == 1 && State.Ev_WeeklyEventDone != 0 && Prov.Owner == 1)
            await WeeklyEventAsync("Gﾊﾟｯﾄﾝ.ADV", 12);
        State.Cmdr_Cur = 22;                                            // :72-75 Lelyukov
        if (State.F_Helman_War == 1 && Cmdr.Status != 0 && State.F_Lelyukov_State != 1)
            await FirstArmyAsync();                                     // \L_002e1
        State.Cmdr_Cur = 32;                                            // :76-79 Aristoles
        if (State.F_Aristoles_Helman2ndState != 0 && Cmdr.Status != 0)
            await SecondArmyAsync();                                    // \L_00521
        State.Cmdr_Cur = 39;                                            // :80-83 Mineva
        if (State.F_Mineva_State != 0 && Cmdr.Status != 0)
            await ThirdArmyAsync();                                     // \L_0068c
        if (State.F_Nero_Helman4thState != 0 && (State.F_Nero_Helman4thState != 100 || State.F_Cream_State == 0))
            await FourthArmyAsync();                                    // :84-86 \L_00824
        State.Cmdr_Cur = 50;                                            // :87-90 Rolex
        if (Cmdr.Status != 0)
            await FifthArmyAsync();                                     // \L_009c3
        if (State.N_Olhound_Stage != 0 && State.N_Olhound_Stage != 100)
            await CrusadersAsync();                                     // :91-93 \L_00c09
        if (State.Ev_WeeklyEventDone == 0)                              // :94-97 a quiet week
            await Script.EventAsync("Gｽﾃｯｾﾙ.ADV", 14);
    }

    /// <summary>
    /// The filler's filter (<c>Tﾍﾙﾏﾝ.ADV:656-658</c>): once slot 1 is taken, commanders with fewer
    /// than 190 troops stay home.
    /// </summary>
    protected override bool SkipsCommander() => Cmdr.Troops < 190 && State.Bt_EnemyParty1 != 0;

    /// <summary>A scene, then <c>!Ev_WeeklyEventDone : 1!</c>.</summary>
    private async Task WeeklyEventAsync(string file, Sys3Int entry)
    {
        await Script.EventAsync(file, entry);
        State.Ev_WeeklyEventDone = 1;
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ.ADV:99-207</c> (<c>L_002e1</c>) — the 1st army: the stronger of its two orders (each
    /// picked and summed; on a tie the second stays), retreat mode 3, no troop threshold, against
    /// Archgrad (36), Siberia (38), Golak (34), Log A (32), Log B (31), Volgo Z (30); at Maunes (13)
    /// a roll: <c>Gﾚﾘｭｰｺﾌ</c> 5 or Tainyar (26) alone; Maunes (13) or North (2) taken from the
    /// monsters; Leazas Castle (1).
    /// </summary>
    private async Task FirstArmyAsync()
    {
        SetLiteral(101);                                                // MS 10, "Helman 1st Army"
        State.Bt_EnemyRetreatMode = 3;
        SetPriority(24, 23, 22, 25, 29, 30, 27, 28, 26);
        PickArmy();                                                     // \L_00dc9
        SumTroops();                                                    // \L_00d83
        State.Tmp0011 = State.CallArg;
        SetPriority(27, 28, 29, 30, 24, 25, 22, 23, 26);
        PickArmy();
        SumTroops();
        State.Tmp0012 = State.CallArg;
        if (State.Tmp0011 > State.Tmp0012)
        {
            SetPriority(24, 23, 22, 25, 29, 30, 27, 28, 26);
            PickArmy();
        }
        State.Ev_WeeklyEventDone = 1;
        // QUIRK: NAT-13 — the 1st army marches whatever its strength.
        if (await AttackFirstAsync(36, 38, 34, 32, 31, 30))
            return;
        State.Prov_Cur = 13;                                            // :170-192 Maunes
        if (Prov.Owner == 1)
        {
            Script.Rnd(2);
            if (State.RetVal == 1)
            {
                await Script.EventAsync("Gﾚﾘｭｰｺﾌ.ADV", 5);
                return;
            }
            State.Cmdr_Cur = 26;
            if (Cmdr.Status == 0)
                return;
            State.Bt_EnemyParty1 = 26;
            State.Bt_EnemyParty2 = 0;
            State.Bt_EnemyParty3 = 0;
            State.Bt_EnemyParty4 = 0;
            await Script.CallFileAsync("FIGT_B.ADV");
            return;
        }
        if (Prov.Owner == 8)
        {
            await CaptureAsync();                                       // \L_00fc2
            return;
        }
        State.Prov_Cur = 2;                                             // :193-201 North
        if (Prov.Owner == 1)
        {
            await Script.CallFileAsync("FIGT_B.ADV");
            return;
        }
        if (Prov.Owner == 8)
        {
            await CaptureAsync();
            return;
        }
        await AttackFirstAsync(1);                                      // :202-206
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ.ADV:208-269</c> (<c>L_00521</c>) — the 2nd army: Aristoles's wait (state 1 → 2 → 3,
    /// then <c>Gｱﾘｽﾄﾚｽ</c> 5 at 3), retreat mode 4, at least 1500 troops, against Micrograd (44),
    /// Parnas (47), Sudori 17 (46), Pawn (40), Sudori 13 (41), Laborie (52).
    /// </summary>
    private async Task SecondArmyAsync()
    {
        SetLiteral(210);                                                // MS 10, "Helman 2nd Army"
        State.Bt_EnemyRetreatMode = 4;
        if (State.F_Aristoles_Helman2ndState == 3)
            await Script.EventAsync("Gｱﾘｽﾄﾚｽ.ADV", 5);
        if (State.F_Aristoles_Helman2ndState == 2)
            State.F_Aristoles_Helman2ndState = 3;
        if (State.F_Aristoles_Helman2ndState == 1)
            State.F_Aristoles_Helman2ndState = 2;
        SetLiteral(223);                                                // MS 10, "Helman 2nd Army" again
        SetPriority(37, 38, 34, 35, 36, 32, 33);
        PickArmy();                                                     // \L_00dc9
        SumTroops();                                                    // \L_00d83
        if (State.CallArg < 1500)
            return;
        State.Ev_WeeklyEventDone = 1;
        await AttackFirstAsync(44, 47, 46, 40, 41, 52);
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ.ADV:270-344</c> (<c>L_0068c</c>) — the 3rd army: Mineva's first scene, retreat mode
    /// 3, at least 1500 troops; at Micrograd (44) <c>Gﾐﾈﾊﾞ</c> 4 while Maha (41) lives and Pastel is
    /// not met, and the army re-picked without Capricorn (184) and Ortega (185), not summed again;
    /// else against Laurengrad (48), Pawn (40), Sudori 10 (42), Parnas (47), Rosesgrad (39),
    /// Archgrad (36).
    /// </summary>
    private async Task ThirdArmyAsync()
    {
        if (State.F_Mineva_State == 1)
            await Script.EventAsync("Gﾐﾈﾊﾞ.ADV", 1);
        SetLiteral(276);                                                // MS 10, "Helman 3rd Army"
        State.Bt_EnemyRetreatMode = 3;
        SetPriority(184, 185, 42, 43, 39, 40, 41);
        PickArmy();                                                     // \L_00dc9
        SumTroops();                                                    // \L_00d83
        if (State.CallArg < 1500)
            return;
        State.Ev_WeeklyEventDone = 1;
        State.Prov_Cur = 44;                                            // :293-313 Micrograd
        if (Prov.Owner == 1)
        {
            State.Cmdr_Cur = 41;
            if (Cmdr.Status != 0 && State.F_Pastel_State == 0)
                await Script.EventAsync("Gﾐﾈﾊﾞ.ADV", 4);
            State.Prov_Cur = 44;
            SetPriority(42, 43, 39, 40, 41);
            PickArmy();
            await Script.CallFileAsync("FIGT_B.ADV");
            return;
        }
        await AttackFirstAsync(48, 40, 42, 47, 39, 36);
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ.ADV:345-383</c> (<c>L_00824</c>) — the 4th army: retreat mode 4; once Nero has turned
    /// (<c>F_Nero_Helman4thState = 6</c>) the smaller army of <c>L_0090d</c>; else <c>Gﾈﾛ</c> 2 at
    /// state 3 before Cream is met, states 1 and 2 counted up, the army (none alive: no march)
    /// against Pawn (40), Sudori 13 (41).
    /// </summary>
    private async Task FourthArmyAsync()
    {
        SetLiteral(347);                                                // MS 10, "Helman 4th Army"
        State.Bt_EnemyRetreatMode = 4;
        if (State.F_Nero_Helman4thState == 6)
        {
            await TurnedFourthArmyAsync();                              // @L_0090d
            return;
        }
        if (State.F_Nero_Helman4thState == 3 && State.F_Cream_State == 0)
            await Script.EventAsync("Gﾈﾛ.ADV", 2);
        if (State.F_Nero_Helman4thState == 1 || State.F_Nero_Helman4thState == 2)
            State.F_Nero_Helman4thState = State.F_Nero_Helman4thState + 1;
        SetPriority(49, 46, 47, 44, 45, 48);
        PickArmy();                                                     // \L_00dc9
        if (State.Bt_EnemyParty1 == 0)
            return;
        State.Ev_WeeklyEventDone = 1;
        await AttackFirstAsync(40, 41);                                 // @L_009be
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ.ADV:384-417</c> (<c>L_0090d</c>) — the 4th army after Nero turned: Hyam (47), Marquez
    /// (46), Sukakumi (49), Cream (45), Yang (48), retreat mode 3, at least 1000 troops, against Pawn
    /// (40), Sudori 13 (41).
    /// </summary>
    private async Task TurnedFourthArmyAsync()
    {
        SetLiteral(386);                                                // MS 10, "Helman 4th Army"
        State.Bt_EnemyRetreatMode = 3;
        SetPriority(47, 46, 49, 45, 48);
        PickArmy();                                                     // \L_00dc9
        if (State.Bt_EnemyParty1 == 0)
            return;
        SumTroops();                                                    // \L_00d83
        if (State.CallArg < 1000)
            return;                                                     // @L_009be
        // QUIRK: NAT-18 — the week is not marked: Gｽﾃｯｾﾙ 14 may follow the battle.
        await AttackFirstAsync(40, 41);
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ.ADV:418-516</c> (<c>L_009c3</c>) — the 5th army: Rolex's way to it (state 2 once
    /// Rosesgrad (39) is the player's; two rolls, each raising state 0 or 1 by one and ending the
    /// turn's army; <c>Gﾛﾚｯｸｽ</c> 1 eight weeks into the war, <c>Gﾛﾚｯｸｽ</c> 2 the week after), then
    /// from state 4 the army, retreat mode 4, at least 500 troops, against Rosesgrad (39), Micrograd
    /// (44), Pawn (40), Sudori 13 (41), Archgrad (36), Log B (31), Volgo Z (30), Laurengrad (48),
    /// Parnas (47), Sudori 17 (46).
    /// </summary>
    private async Task FifthArmyAsync()
    {
        State.Prov_Cur = 39;
        if (Prov.Owner == 1 && State.F_Rolex_State < 3)
            State.F_Rolex_State = 2;
        Script.Rnd(2);
        if (State.RetVal == 1 && State.F_Rolex_State == 0)
        {
            State.F_Rolex_State = 1;
            return;
        }
        Script.Rnd(2);
        if (State.RetVal == 1 && State.F_Rolex_State == 1)
        {
            State.F_Rolex_State = 2;
            return;
        }
        if (State.Week > State.Stessel_StartWeek + 8 && State.F_Rolex_State == 2)
        {
            State.F_Rolex_State = 3;
            await Script.EventAsync("Gﾛﾚｯｸｽ.ADV", 1);
            return;
        }
        if (State.F_Rolex_State == 3)
        {
            State.F_Rolex_State = 4;
            await Script.EventAsync("Gﾛﾚｯｸｽ.ADV", 2);
            return;
        }
        if (State.F_Rolex_State < 4)
            return;
        SetLiteral(449);                                                // MS 10, "Helman 5th Army"
        State.Bt_EnemyRetreatMode = 4;
        SetPriority(53, 50, 52, 51);
        PickArmy();                                                     // \L_00dc9
        SumTroops();                                                    // \L_00d83
        if (State.CallArg < 500)
            return;
        State.Ev_WeeklyEventDone = 1;
        await AttackFirstAsync(39, 44, 40, 41, 36, 31, 30, 48, 47, 46);
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ.ADV:517-583</c> (<c>L_00c09</c>) — the Crusaders: Olhound (60) alone, retreat mode 1,
    /// his scene every turn, then alive with full HP and <c>500 + RND(500)</c> troops, against Path
    /// (43), Micrograd (44), Laurengrad (48), Parnas (47), Sudori 17 (46), Pawn (40), Sudori 13 (41),
    /// Sudori 10 (42), Rosesgrad (39), Archgrad (36).
    /// </summary>
    private async Task CrusadersAsync()
    {
        SetLiteral(519);                                                // MS 10, "Crusader Army"
        State.Bt_EnemyRetreatMode = 1;
        State.Bt_EnemyParty1 = 60;
        State.Bt_EnemyParty2 = 0;
        State.Bt_EnemyParty3 = 0;
        State.Bt_EnemyParty4 = 0;
        await Script.EventAsync("Gｵｰﾙﾊｳﾝﾄ.ADV", 1);
        State.Cmdr_Cur = 60;
        Cmdr.Status = 1;
        Cmdr.HP = Cmdr.MaxHP;
        Script.Rnd(500);
        Cmdr.Troops = 500 + State.RetVal;
        State.Ev_WeeklyEventDone = 1;
        await AttackFirstAsync(43, 44, 48, 47, 46, 40, 41, 42, 39, 36);
    }

    /// <summary>
    /// <c>Tﾍﾙﾏﾝ.ADV:676-718</c> (<c>L_00fc2</c>) — the 1st army takes <c>Prov_Cur</c> from the
    /// monsters without a battle: the camera to it, its picture and name, "&lt;army&gt; VS Monster
    /// Army", "The &lt;army&gt; took control of the city." (each with its key wait), owner 4, the
    /// flag redrawn.
    /// </summary>
    private async Task CaptureAsync()
    {
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);         // MM 8, the province's name
        State.CallArg = Prov.BgCg;
        await WaoAsync(331);                                            // \L_01192
        await WaoAsync(113);                                            // \L_010ce
        await EngineAsync("ZM", 32);
        await TextAsync(689);                                           // "［<province>］"
        await WaoAsync(1);                                              // \L_011bb
        await EngineAsync("ZM", 24);
        await TextAsync(695);                                           // "<army> VS Monster Army"
        await WaoAsync(1);
        await TextAsync(702);                                           // "The <army> took control of the city."
        await WaoAsync(1);
        await WaoAsync(0);                                              // \L_011ae
        await WaoAsync(332);                                            // \L_011a0
        Prov.Owner = 4;
        State.Tmp0021 = Prov.Owner + 819;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(4);
        await Script.MapCallAsync(3);
    }
}
