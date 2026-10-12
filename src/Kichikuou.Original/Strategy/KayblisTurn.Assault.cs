using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Tｹｲﾌﾞﾘｽ.ADV:326-624</c>, the Assault Leaders' armies (milestone 4 step 5): past week 100 the
/// dispatch sends the 1st Assault Leader (191), past 110 the 2nd (192) as well, and so on to the 8th
/// (198) past 160, on the north and the south list in turn, each with Monster Generals (87, 88).
/// </summary>
public sealed partial class KayblisTurn
{
    /// <summary>
    /// The north list (<c>:364-490</c>): Sulfur Forest (80), Silky's Castle (81), Grief Valley (89),
    /// Great Wall of Helman B (50), Laurengrad (48), Lang Bau (45), Cascade Bau (83), Kesselring's Castle
    /// (87), Bone Forest (90), Maginot Stronghold (72), Paritylaon (71), Chizuko's Tower (68), Zeth Palace
    /// (69), Micrograd (44), Sudori 10 (42), Rosesgrad (39), Archgrad (36), Log B (31), Maunes (13),
    /// Nagi's Tower (67), Nagarmaule (62), Sabasaba (59), Adam's Fortress (58), Parapara Fortress (16),
    /// Rich (15), North (2), Oku (3), Scale (12), Alan (11), South (4), Leazas Castle (1).
    /// </summary>
    private static readonly int[] AssaultNorthTargets =
        [80, 81, 89, 50, 48, 45, 83, 87, 90, 72, 71, 68, 69, 44, 42, 39, 36, 31, 13, 67, 62, 59, 58, 16, 15, 2, 3, 12, 11, 4, 1];

    /// <summary>
    /// The south list (<c>:530-617</c>): Sulfur Forest (80), Silky's Castle (81), Land of Death (82),
    /// Kayblis's Castle (84), Blue House (86), Camilla's Castle (88), Tearing Forest (91), Maginot
    /// Stronghold (76), Marc (74), Papaya's Tower (70), Chizuko's Tower (68), Magic's Tower (66), Zeth
    /// Palace (69), Nagi's Tower (67), Nagarmaule (62), Sabasaba (59), Adam's Fortress (58), Parapara
    /// Fortress (16), Rich (15), Maunes (13), North (2), Leazas Castle (1).
    /// </summary>
    private static readonly int[] AssaultSouthTargets =
        [80, 81, 82, 84, 86, 88, 91, 76, 74, 70, 68, 66, 69, 67, 62, 59, 58, 16, 15, 13, 2, 1];

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:326-491</c> / <c>:492-617</c> (<c>L_00881</c> / <c>L_00c91</c>) — the Assault
    /// Leader in <c>Tmp0001</c> with Monster Generals (87, 88), retreat mode 1. The two Monster Generals
    /// are called up alive at full HP with a fifth of their maximum troops and a draw of up to a third
    /// more, whether the leader marches or not. A dead leader is revived at 1 HP and stays home; a
    /// living one gains 3 HP and his recovery in troops (once, each up to its maximum) and marches
    /// with 1000 troops or more, to the first province of his list that is not Kayblis's — on the
    /// north list alone at Maunes (13). With all of the north list Kayblis's he stays home; the south
    /// list runs on into the attack at Leazas Castle (1) (NAT-16).
    /// </summary>
    private async Task AssaultLeaderAsync(bool north)
    {
        State.Tmp0002 = 87;                                             // :327-334, Tmp0001 the leader
        State.Tmp0003 = 88;
        State.Tmp0004 = 0;
        State.Tmp0005 = 0;
        State.Tmp0006 = 0;
        State.Tmp0007 = 0;
        State.Tmp0008 = 0;
        State.Tmp0009 = 0;
        State.Bt_EnemyRetreatMode = 1;                                  // :335
        foreach (int general in new[] { 87, 88 })                       // :336-345
        {
            State.Cmdr_Cur = (Sys3Int)general;
            Cmdr.Status = 1;
            Cmdr.HP = Cmdr.MaxHP;
            Script.Rnd(Cmdr.MaxTroops / 3);
            Cmdr.Troops = Cmdr.MaxTroops / 5 + State.RetVal;
        }
        State.Cmdr_Cur = State.Tmp0001;                                 // :346-351 the leader
        if (Cmdr.Status == 0)
        {
            Cmdr.Status = 1;
            Cmdr.HP = 1;
            return;
        }
        Cmdr.HP = Cmdr.HP + 3;                                          // :352-362
        if (Cmdr.HP > Cmdr.MaxHP)
            Cmdr.HP = Cmdr.MaxHP;
        Cmdr.Troops = Cmdr.Troops + Cmdr.Recovery;
        if (Cmdr.Troops > Cmdr.MaxTroops)
            Cmdr.Troops = Cmdr.MaxTroops;
        if (Cmdr.Troops < 1000)
            return;
        PickArmy();                                                     // \L_02464
        if (WalkToFirstNotKayblis(north ? AssaultNorthTargets : AssaultSouthTargets))
        {
            if (north && State.Prov_Cur == 13)                          // :436-442 the leader alone at Maunes
            {
                State.Bt_EnemyParty2 = 0;
                State.Bt_EnemyParty3 = 0;
                State.Bt_EnemyParty4 = 0;
            }
        }
        else if (north)
            return;                                                     // :491
        // QUIRK: NAT-16 — the south list has no return after Leazas Castle (1): with all of it
        // Kayblis's it runs on into L_00faf there and takes the castle again (dead in play).
        await AssaultAsync();                                           // @L_00faf
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ.ADV:618-624</c> (<c>L_00faf</c>) — the Assault Leader at <c>Prov_Cur</c>: taken when
    /// not the player's, else attacked through <c>FIGT_B</c>.
    /// </summary>
    private async Task AssaultAsync()
    {
        if (Prov.Owner != 1)
        {
            await CaptureAsync();                                       // \L_026bf
            return;
        }
        await Script.CallFileAsync("FIGT_B.ADV");
    }
}
