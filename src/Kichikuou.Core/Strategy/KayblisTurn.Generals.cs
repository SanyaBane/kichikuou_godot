using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Tｹｲﾌﾞﾘｽ.ADV:729-1328</c>, the units of Kayblis's generals (milestone 4 step 2): Babolat, Redeye,
/// Kite, Galtia and Kesselring. Each picks its unit from a fixed list and walks a fixed line of
/// provinces to the first one that is not Kayblis's: one the player does not hold is taken without
/// a battle (<see cref="CaptureAsync"/>), the player's is attacked through <c>FIGT_B</c>. Babolat,
/// Redeye, Kite and Galtia attack once more when the first battle of the week ran out of time with
/// the player holding (NAT-03). Every unit's troop sum is never read (NAT-16).
/// </summary>
public sealed partial class KayblisTurn
{
    /// <summary>
    /// Babolat's line (<c>:746-801</c>): Tower of Evil (78), Great Wall of Helman A (49), Ikoma (53),
    /// Laurengrad (48), Lang Bau (45), Micrograd (44), Sudori 10 (42), Rosesgrad (39), Siberia (38),
    /// Golak (34), Log A (32), Scale (12), North (2), Leazas Castle (1).
    /// </summary>
    private static readonly int[] BabolatTargets = [78, 49, 53, 48, 45, 44, 42, 39, 38, 34, 32, 12, 2, 1];

    /// <summary>
    /// Redeye's line (<c>:848-911</c>): Grief Valley (89), Great Wall of Helman B (50), Laurengrad (48),
    /// Parnas (47), Sudori 17 (46), Sudori 13 (41), Pawn (40), Micrograd (44), Sudori 10 (42), Rosesgrad
    /// (39), Archgrad (36), Log B (31), Maunes (13), North (2), Leazas Castle (1).
    /// </summary>
    private static readonly int[] RedeyeTargets = [89, 50, 48, 47, 46, 41, 40, 44, 42, 39, 36, 31, 13, 2, 1];

    /// <summary>
    /// Kite's line (<c>:1004-1071</c>): Maginot Stronghold (76), Marc (74), Papaya's Tower (70), Magic's
    /// Tower (66), Zeth Palace (69), Ardennes (73), Amber Castle (65), Nagi's Tower (67), Nagarmaule (62),
    /// Sabasaba (59), Adam's Fortress (58), Parapara Fortress (16), Rich (15), Poor (14), Rockearth (17),
    /// Ice (18), Diphtheria (19).
    /// </summary>
    private static readonly int[] KiteTargets = [76, 74, 70, 66, 69, 73, 65, 67, 62, 59, 58, 16, 15, 14, 17, 18, 19];

    /// <summary>
    /// Galtia's line (<c>:1129-1184</c>): Maginot Stronghold (76), Marc (74), Amber Castle (65), Old Zeth
    /// (63), Magic's Tower (66), Tape (61), Italia (60), Sabasaba (59), Adam's Fortress (58), Parapara
    /// Fortress (16), Rich (15), Maunes (13), North (2), Leazas Castle (1).
    /// </summary>
    private static readonly int[] GaltiaTargets = [76, 74, 65, 63, 66, 61, 60, 59, 58, 16, 15, 13, 2, 1];

    /// <summary>
    /// Kesselring's line (<c>:1223-1264</c>): Papaya's Tower (70), Tape (61), Diphtheria (19), Red (23),
    /// South (4), Leazas Castle (1); he flies to each from the one before, to the first from his castle (87).
    /// </summary>
    private static readonly int[] KesselringTargets = [70, 61, 19, 23, 4, 1];

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:729-830</c> (<c>L_0120e</c>, <c>L_013cb</c>) — Babolat's unit: Babolat (129) alone
    /// on his line; at the province of his pitfall (<c>Babolat_PitfallProv</c>) <c>Gﾊﾞﾎﾞﾗ</c> 4 instead of
    /// the battle (he dies there); after a battle he survives, <c>Gﾊﾞﾎﾞﾗ</c> 2 while his state is 1.
    /// </summary>
    private async Task BabolatAsync()
    {
        while (true)                                                    // L_0120e, again after a held battle
        {
            SetLiteral(731);                                            // MS 10, "Babolat Unit"
            SetPriority(129);
            PickArmy();                                                 // \L_02464
            SumTroops();                                                // \L_0241e, QUIRK: NAT-16 — never read
            if (State.Bt_EnemyParty1 == 0)
                return;
            if (!WalkToFirstNotKayblis(BabolatTargets))                 // :746-802
                return;
            if (Prov.Owner != 1)                                        // L_013cb
            {
                await CaptureAsync();                                   // \L_026bf
                return;
            }
            if (State.Babolat_PitfallProv == State.Prov_Cur)            // :808-812 the pitfall
            {
                await Script.EventAsync("Gﾊﾞﾎﾞﾗ.ADV", 4);
                return;
            }
            State.Bt_EnemyRetreatMode = 2;                              // :813-817
            if (State.Ai_BabolatAtk == 0)
                State.Ai_BabolatAtk = 10;
            await Script.CallFileAsync("FIGT_B.ADV");
            State.Cmdr_Cur = 129;                                       // :818-821
            if (Cmdr.Status == 0)
                return;
            if (State.F_Babolat_State == 1)                             // :822-825
                await Script.EventAsync("Gﾊﾞﾎﾞﾗ.ADV", 2);
            // QUIRK: NAT-03 — a battle held to the time limit is fought again, once a week.
            if (!HeldToTheLimit(State.Ai_BabolatAtk))                   // :826-829
                return;
            State.Ai_BabolatAtk = 11;                                   // @L_0120e
        }
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:831-971</c> (<c>L_01478</c>, <c>L_01664</c>) — Redeye's unit: Truffle 77 (148),
    /// Moreinyan (149), Ouzawa (150), Redeye (130) on his line, Redeye alone at Maunes (13). Before the
    /// battle <c>Gﾚｯﾄﾞｱｲ</c> 2 at state 2; at state 8 <c>Gﾌﾘｰｸ</c> 4 instead of the battle while Freak's
    /// stage is 3, else at Maunes the ninja's chance, <c>Gﾚｯﾄﾞｱｲ</c> 8 (NAT-28). After a battle he
    /// survives, a draw in two, <c>Gﾚｯﾄﾞｱｲ</c> 6 at Freak's stage 5 when the player lost and the draw is
    /// not 1, and his state's steps: <c>Gﾚｯﾄﾞｱｲ</c> 5 at 7, 6 to 7, <c>Gﾚｯﾄﾞｱｲ</c> 4 at 5, 3 to 4, 1 to 2.
    /// </summary>
    private async Task RedeyeAsync()
    {
        while (true)                                                    // L_01478, again after a held battle
        {
            SetLiteral(833);                                            // MS 10, "Redeye Unit"
            SetPriority(148, 149, 150, 130);
            PickArmy();                                                 // \L_02464
            SumTroops();                                                // \L_0241e, QUIRK: NAT-16 — never read
            if (State.Bt_EnemyParty1 == 0)
                return;
            if (!WalkToFirstNotKayblis(RedeyeTargets))                  // :848-912
                return;
            if (State.Prov_Cur == 13)                                   // :896-903 Redeye alone at Maunes
            {
                State.Bt_EnemyParty1 = 130;
                State.Bt_EnemyParty2 = 0;
                State.Bt_EnemyParty3 = 0;
                State.Bt_EnemyParty4 = 0;
            }
            if (Prov.Owner != 1)                                        // L_01664
            {
                await CaptureAsync();                                   // \L_026bf
                return;
            }
            State.Tmp0229 = State.Prov_Cur;                             // :918-922
            if (State.F_Redeye_State == 2)
                await Script.EventAsync("Gﾚｯﾄﾞｱｲ.ADV", 2);
            if (State.N_Freak_Stage == 3 && State.F_Redeye_State == 8)  // :923-927 Freak instead of the battle
            {
                await Script.EventAsync("Gﾌﾘｰｸ.ADV", 4);
                return;
            }
            if (State.Prov_Cur == 13 && State.F_Redeye_State == 8)      // :928-934 a ninja at Maunes
            {
                await Script.EventAsync("Gﾚｯﾄﾞｱｲ.ADV", 8);
                // QUIRK: NAT-28 — with no ninja the scene returns at once and this tests the troop
                // sum's last slot: Redeye only in a full unit, else commander record 0.
                if (Cmdr.Status == 0)
                    return;
            }
            State.Prov_Cur = State.Tmp0229;                             // :935-940
            State.Bt_EnemyRetreatMode = 2;
            if (State.Ai_RedeyeAtk == 0)
                State.Ai_RedeyeAtk = 10;
            await Script.CallFileAsync("FIGT_B.ADV");
            State.Cmdr_Cur = 130;                                       // :941-944
            if (Cmdr.Status == 0)
                return;
            // QUIRK: NAT-03 — a battle held to the time limit is fought again, once a week.
            if (HeldToTheLimit(State.Ai_RedeyeAtk))                     // :945-948
            {
                State.Ai_RedeyeAtk = 11;                                // @L_01478
                continue;
            }
            Script.Rnd(2);                                              // :949-953
            if (State.N_Freak_Stage == 5 && State.RetVal != 1 && State.Bt_Result == 1)
                await Script.EventAsync("Gﾚｯﾄﾞｱｲ.ADV", 6);
            if (State.F_Redeye_State == 7)                              // :954-970
                await Script.EventAsync("Gﾚｯﾄﾞｱｲ.ADV", 5);
            if (State.F_Redeye_State == 6)
                State.F_Redeye_State = 7;
            if (State.F_Redeye_State == 5)
                await Script.EventAsync("Gﾚｯﾄﾞｱｲ.ADV", 4);
            if (State.F_Redeye_State == 3)
                State.F_Redeye_State = 4;
            if (State.F_Redeye_State == 1)
                State.F_Redeye_State = 2;
            return;
        }
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:972-1111</c> (<c>L_017e4</c>, <c>L_01a62</c>) — Kite's unit: Kite (132), Tobroiv
    /// (151), Deckmaster (152), Strength (153). First a draw in three (in ten while Calory's state is
    /// below 4): on 1 or 2, while Zeth Palace (69) is Kayblis's with its own picture, <c>Gｶｲﾄ</c> 6 or 7
    /// instead of the march. Then his line; after a battle he survives, <c>Gｶｲﾄ</c> 2, 3, 4 at Calory's
    /// states 1, 2, 3, and 5 at state 4 while Zeth Palace (69) is still the province, Kayblis's.
    /// </summary>
    private async Task KiteAsync()
    {
        while (true)                                                    // L_017e4, again after a held battle
        {
            SetLiteral(974);                                            // MS 10, "Kite Unit"
            SetPriority(132, 151, 152, 153);
            PickArmy();                                                 // \L_02464
            SumTroops();                                                // \L_0241e, QUIRK: NAT-16 — never read
            if (State.Bt_EnemyParty1 == 0)
                return;
            Script.Rnd(3);                                              // :989-1003 Calory at Zeth Palace
            State.Prov_Cur = 69;
            if (State.F_Calory_State < 4)
                Script.Rnd(10);
            if (State.RetVal == 1 && Prov.Owner == 8 && Prov.BgCg == 1011)
            {
                await Script.EventAsync("Gｶｲﾄ.ADV", 6);
                return;
            }
            if (State.RetVal == 2 && Prov.Owner == 8 && Prov.BgCg == 1011)
            {
                await Script.EventAsync("Gｶｲﾄ.ADV", 7);
                return;
            }
            if (!WalkToFirstNotKayblis(KiteTargets))                    // :1004-1072
                return;
            if (Prov.Owner != 1)                                        // L_01a62
            {
                await CaptureAsync();                                   // \L_026bf
                return;
            }
            State.Bt_EnemyRetreatMode = 2;                              // :1078-1082
            if (State.Ai_KiteAtk == 0)
                State.Ai_KiteAtk = 10;
            await Script.CallFileAsync("FIGT_B.ADV");
            State.Cmdr_Cur = 132;                                       // :1083-1086
            if (Cmdr.Status == 0)
                return;
            // QUIRK: NAT-03 — a battle held to the time limit is fought again, once a week.
            if (HeldToTheLimit(State.Ai_KiteAtk))                       // :1087-1090
            {
                State.Ai_KiteAtk = 11;                                  // @L_017e4
                continue;
            }
            if (State.F_Calory_State == 1)                              // :1091-1110
                await Script.EventAsync("Gｶｲﾄ.ADV", 2);
            else if (State.F_Calory_State == 2)
                await Script.EventAsync("Gｶｲﾄ.ADV", 3);
            else if (State.F_Calory_State == 3)
                await Script.EventAsync("Gｶｲﾄ.ADV", 4);
            else if (State.F_Calory_State == 4 && Prov.Owner == 8 && State.Prov_Cur == 69 && Prov.BgCg == 1011)
                await Script.EventAsync("Gｶｲﾄ.ADV", 5);
            return;
        }
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1112-1208</c> (<c>L_01b5e</c>, <c>L_01d23</c>) — Galtia's unit: Derandron (154),
    /// Nasport (155), Galtia (131) on his line; after a battle he survives and the player lost,
    /// <c>Gｶﾞﾙﾃｨｱ</c> 2.
    /// </summary>
    private async Task GaltiaAsync()
    {
        while (true)                                                    // L_01b5e, again after a held battle
        {
            SetLiteral(1114);                                           // MS 10, "Galtia Unit"
            SetPriority(154, 155, 131);
            PickArmy();                                                 // \L_02464
            SumTroops();                                                // \L_0241e, QUIRK: NAT-16 — never read
            if (State.Bt_EnemyParty1 == 0)
                return;
            if (!WalkToFirstNotKayblis(GaltiaTargets))                  // :1129-1185
                return;
            if (Prov.Owner != 1)                                        // L_01d23
            {
                await CaptureAsync();                                   // \L_026bf
                return;
            }
            State.Bt_EnemyRetreatMode = 2;                              // :1191-1195
            if (State.Ai_GaltiaAtk == 0)
                State.Ai_GaltiaAtk = 10;
            await Script.CallFileAsync("FIGT_B.ADV");
            State.Cmdr_Cur = 131;                                       // :1196-1199
            if (Cmdr.Status == 0)
                return;
            // QUIRK: NAT-03 — a battle held to the time limit is fought again, once a week.
            if (HeldToTheLimit(State.Ai_GaltiaAtk))                     // :1200-1203
            {
                State.Ai_GaltiaAtk = 11;                                // @L_01b5e
                continue;
            }
            if (State.Bt_Result == 1)                                   // :1204-1207
                await Script.EventAsync("Gｶﾞﾙﾃｨｱ.ADV", 2);
            return;
        }
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1209-1286</c> (<c>L_01db2</c>, <c>L_01ef5</c>) — Kesselring's unit: Kesselring
    /// (133) alone (no test of an empty unit) flies along his line from the province before the first
    /// one not Kayblis's (<see cref="FlyAsync"/>); the player's is attacked with retreat mode 1, no
    /// second attack. After the battle his last province is the one fought over, and <c>Gｹｯｾﾙ</c> 2 at
    /// his state 3 while the player holds Bone Forest (90) or the Red House (85) — dead or alive.
    /// </summary>
    private async Task KesselringAsync()
    {
        SetLiteral(1211);                                               // MS 10, "Kesselring Unit"
        SetPriority(133);
        PickArmy();                                                     // \L_02464
        SumTroops();                                                    // \L_0241e, QUIRK: NAT-16 — never read
        int from = 87;                                                  // :1223-1265
        foreach (int province in KesselringTargets)
        {
            State.Prov_Cur = (Sys3Int)province;
            if (Prov.Owner != 8)
            {
                State.Prov_Cur = (Sys3Int)from;
                await FlyAsync();                                       // \L_01f71
                State.Prov_Cur = (Sys3Int)province;
                await KesselringAttackAsync();                          // @L_01ef5
                return;
            }
            from = province;
        }
    }

    /// <summary><c>Tｹｲﾌﾞﾘｽ.ADV:1266-1286</c> (<c>L_01ef5</c>) — Kesselring at <c>Prov_Cur</c>.</summary>
    private async Task KesselringAttackAsync()
    {
        if (Prov.Owner != 1)
        {
            await CaptureAsync();                                       // \L_026bf
            return;
        }
        State.Bt_EnemyRetreatMode = 1;                                  // :1271-1273
        await Script.CallFileAsync("FIGT_B.ADV");
        State.Kesselring_LastProv = State.Prov_Cur;
        State.Prov_Cur = 90;                                            // :1274-1285 Bone Forest, the Red House
        if (Prov.Owner == 1 && State.F_Kesselring_State == 3)
        {
            await Script.EventAsync("Gｹｯｾﾙ.ADV", 2);
            return;
        }
        State.Prov_Cur = 85;
        if (Prov.Owner == 1 && State.F_Kesselring_State == 3)
            await Script.EventAsync("Gｹｯｾﾙ.ADV", 2);
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:1287-1328</c> (<c>L_01f71</c>) — Kesselring takes off from <c>Prov_Cur</c>: the
    /// camera to it, its picture, "［&lt;province&gt;］ Sky Above", his "Shall I go?" with his portrait
    /// (416) and "Kesselring's unit flew towards its destination.", each with its key wait.
    /// </summary>
    private async Task FlyAsync()
    {
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        await Script.MapCallAsync(10);
        CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);         // MM 8, the province's name
        State.CallArg = Prov.BgCg;
        await WaoAsync(331);                                            // \L_02b41 the province's picture
        await WaoAsync(113);                                            // \L_02a7d
        await EngineAsync("ZM", 32);
        await TextAsync(1302);                                          // "［<province>］"
        await EngineAsync("ZM", 24);
        await TextAsync(1308);                                          // "Sky Above"
        await WaoAsync(1);                                              // \L_02b6a
        await WaoAsync(0);                                              // \L_02b5d
        State.CallArg = 416;
        await WaoAsync(311);                                            // \L_02a99 Kesselring's portrait
        await WaoAsync(111);                                            // \L_02a45
        await TextAsync(1316);                                          // "Shall I go?"
        await WaoAsync(1);
        await WaoAsync(0);
        await WaoAsync(113);                                            // \L_02a7d
        await TextAsync(1321);                                          // "Kesselring's unit flew towards its destination."
        await WaoAsync(1);
        await WaoAsync(0);
        await WaoAsync(312);                                            // \L_02aa7
        await WaoAsync(332);                                            // \L_02b4f
    }

    /// <summary>
    /// The generals' walk (<c>!Prov_Cur : p! {Prov_Owner \ 8: @…:}</c> per province): <c>Prov_Cur</c> to
    /// the first of <paramref name="provinces"/> that is not Kayblis's; false when all are his
    /// (<c>Prov_Cur</c> at the last).
    /// </summary>
    private bool WalkToFirstNotKayblis(int[] provinces)
    {
        foreach (int province in provinces)
        {
            State.Prov_Cur = (Sys3Int)province;
            if (Prov.Owner != 8)
                return true;
        }
        return false;
    }

    /// <summary><c>(Bt_RoundsLeft = 0) * (Bt_Result = 2) * (mark = 10)</c> — the week's first battle ran out of time with the player holding.</summary>
    private bool HeldToTheLimit(Sys3Int mark) => State.Bt_RoundsLeft == 0 && State.Bt_Result == 2 && mark == 10;
}
