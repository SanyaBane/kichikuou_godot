using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Tｹｲﾌﾞﾘｽ.ADV:670-728</c> and <c>:1329-1508</c>, Piar's unit and Kayblis's army at war
/// (milestone 4 step 3).
/// </summary>
public sealed partial class KayblisTurn
{
    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:670-728</c> (<c>L_010af</c>, <c>L_0114c</c>) — Piar's unit: a random province of
    /// 1-77 until one is the player's and not Nothing (75), a draw in ten giving up after each miss
    /// (NAT-15); <c>Gﾊﾟｲｱｰﾙ</c> 2 (PG-X mk2 (138) revived when PG-7 (136) and PG-9 (137) are dead);
    /// the three of them through <c>FIGT_B</c> with retreat mode 2. After the battle Miki (unit 32) among
    /// the defenders brings <c>Gﾊﾟｲｱｰﾙ</c> 6 (the assault); else Piar's state steps: <c>Gﾊﾟｲｱｰﾙ</c> 5 at
    /// 5, 4 to 5, <c>Gﾊﾟｲｱｰﾙ</c> 4 at 3, 2 to 3, <c>Gﾊﾟｲｱｰﾙ</c> 3 at 1.
    /// </summary>
    private async Task PiarAsync()
    {
        while (true)                                                    // L_010af
        {
            SetLiteral(672);                                            // MS 10, "Piar Unit"
            Script.Rnd(77);
            State.Prov_Cur = State.RetVal;
            if (Prov.Owner == 1 && State.Prov_Cur != 75)                // :675-681
                break;
            // QUIRK: NAT-15 — a miss gives up on a draw in ten, else draws again, unbounded.
            Script.Rnd(10);
            if (State.RetVal == 1)
                return;
        }
        await Script.EventAsync("Gﾊﾟｲｱｰﾙ.ADV", 2);                     // :682-683
        SetPriority(136, 137, 138);                                     // :684-693
        PickArmy();                                                     // \L_02464
        // QUIRK: NAT-16 — dead: Gﾊﾟｲｱｰﾙ 2 leaves somebody alive and the province the player's.
        if (State.Bt_EnemyParty1 == 0)                                  // :694-696
            return;
        if (Prov.Owner != 1)                                            // L_0114c
        {
            await CaptureAsync();                                       // \L_026bf
            return;
        }
        State.Bt_EnemyRetreatMode = 2;                                  // :703-704
        await Script.CallFileAsync("FIGT_B.ADV");
        if (State.Bt_PlayerParty1 == 32 || State.Bt_PlayerParty2 == 32 || State.Bt_PlayerParty3 == 32 || State.Bt_PlayerParty4 == 32)
        {
            await Script.EventAsync("Gﾊﾟｲｱｰﾙ.ADV", 6);                 // :705-709 Miki
            return;
        }
        if (State.F_Piar_State == 5)                                    // :710-727
            await Script.EventAsync("Gﾊﾟｲｱｰﾙ.ADV", 5);
        if (State.F_Piar_State == 4)
            State.F_Piar_State = 5;
        if (State.F_Piar_State == 3)
            await Script.EventAsync("Gﾊﾟｲｱｰﾙ.ADV", 4);
        if (State.F_Piar_State == 2)
            State.F_Piar_State = 3;
        if (State.F_Piar_State == 1)
            await Script.EventAsync("Gﾊﾟｲｱｰﾙ.ADV", 3);
    }

    /// <summary>
    /// Kayblis's line (<c>:1440-1499</c>), Redeye's without his stop at Maunes: Grief Valley (89), Great
    /// Wall of Helman B (50), Laurengrad (48), Parnas (47), Sudori 17 (46), Sudori 13 (41), Pawn (40),
    /// Micrograd (44), Sudori 10 (42), Rosesgrad (39), Archgrad (36), Log B (31), Maunes (13), North (2),
    /// Leazas Castle (1).
    /// </summary>
    private static readonly int[] KayblisArmyTargets = [89, 50, 48, 47, 46, 41, 40, 44, 42, 39, 36, 31, 13, 2, 1];

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1329-1416</c> (<c>L_02038</c>) — Kayblis's army at war, up to six marches on his
    /// line, all with retreat mode 2: Kayblis (135) alone; then each of Silky (156), Galtia (131, once he
    /// has defected), Satella (157) and Meglass (158) alive with Monster Generals (86, 88, 89); then
    /// Monster Generals (87, 86, 88, 89) — each of these after the four Monster Generals are refilled
    /// (<see cref="RefillMonsterGenerals"/>). No unit is tested for being empty (Kayblis is alive
    /// whenever the phase runs: his death ends stage 3).
    /// </summary>
    private async Task KayblisArmyAsync()
    {
        State.Bt_EnemyRetreatMode = 2;                                  // :1330-1343
        SetLiteral(1332);                                               // MS 10, "Kayblis Unit"
        SetPriority(135);
        PickArmy();                                                     // \L_02464
        await KayblisMarchAsync();                                      // \L_0227b
        foreach (int leader in new[] { 156, 131, 157, 158 })            // :1344-1403
        {
            State.Cmdr_Cur = (Sys3Int)leader;
            if (Cmdr.Status == 0 || (leader == 131 && State.F_Galtia_DefectedToKayblis == 0))
                continue;
            RefillMonsterGenerals();                                    // \L_021f2
            SetPriority(leader, 86, 88, 89);
            PickArmy();
            await KayblisMarchAsync();
        }
        RefillMonsterGenerals();                                        // :1404-1416
        SetPriority(87, 86, 88, 89);
        PickArmy();
        await KayblisMarchAsync();
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1417-1438</c> (<c>L_021f2</c>) — Monster Generals (86-89) alive at full HP with
    /// their maximum troops and up to half again (NAT-10).
    /// </summary>
    private void RefillMonsterGenerals()
    {
        foreach (int general in new[] { 86, 87, 88, 89 })
        {
            State.Cmdr_Cur = (Sys3Int)general;
            Cmdr.Status = 1;
            Cmdr.HP = Cmdr.MaxHP;
            // QUIRK: NAT-10 — above the maximum.
            Script.Rnd(Cmdr.MaxTroops / 2);
            Cmdr.Troops = Cmdr.MaxTroops + State.RetVal;
        }
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1439-1508</c> (<c>L_0227b</c>, <c>L_023f8</c>) — one march on Kayblis's line: the
    /// first province not his attacked through <c>FIGT_B</c> when the player's, else taken.
    /// </summary>
    private async Task KayblisMarchAsync()
    {
        if (!WalkToFirstNotKayblis(KayblisArmyTargets))
            return;
        if (Prov.Owner != 1)
        {
            await CaptureAsync();                                       // \L_026bf
            return;
        }
        State.Bt_EnemyRetreatMode = 2;
        // QUIRK: NAT-31 — Kayblis dying here moves the story to stage 4, and the phase marches on.
        await Script.CallFileAsync("FIGT_B.ADV");
    }
}
