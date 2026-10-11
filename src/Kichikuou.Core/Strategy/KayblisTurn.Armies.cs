namespace Kichikuou.Core.Strategy;

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

    /// <summary><c>Tｹｲﾌﾞﾘｽ.ADV:1329-1508</c> (<c>L_02038</c>) — Kayblis's army at war (milestone 4 step 3).</summary>
    private Task KayblisArmyAsync() => throw NotPorted(1329, "Kayblis's army (L_02038)", "N3, milestone 4 step 3");
}
