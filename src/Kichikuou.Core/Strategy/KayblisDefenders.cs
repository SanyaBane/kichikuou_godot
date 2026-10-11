using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Tｹｲﾌﾞﾘｽ2.ADV</c>, the defenders of a province of Kayblis's the player attacks (part N3,
/// milestone 3 step 3; called by <c>Cﾃｷﾄｼ.ADV:353-355</c> before <c>FIGT_A</c>), ported one-to-one on
/// the nation template (<c>docs/strategy/survey-nations.md</c> §2.5). Every attack calls up the four
/// Monster Generals (86-89) at full HP with a share of their maximum troops and a draw on top — a
/// quarter and up to a quarter more, or, when the same province is attacked again,
/// <c>1 / (4 + 2 hits)</c> and up to <c>1 / (2 + hits)</c> more. Before stage 3 the gate — Tower of
/// Evil (78), Grief Valley (89), Bone Forest (90), and Tearing Forest (91) at stage 2 only (NAT-09) —
/// moves the story to stage 3 and is defended like a stage-3 province but with its walls; any other
/// province is fought in the plains by Monster Generals (86, 87), or all four on a roll in two. At
/// stage 3: Kaybwan (159) with Hikaru (160) and Rika (161) at the Blue House (86), Kaybnyan (162)
/// with Wao (163) and Kunihiko (164) at the Red House (85), Warg's scene instead of a battle at the
/// Tower of Evil (78) until she is met (<c>Gﾜｰｸﾞ</c> 1), Kesselring (133) with three Monster Generals
/// at his castle (87), Camilla (91) with one at hers (88; her maximum raised from 200 to 2000,
/// NAT-10), Kayblis (135) with three Monster Generals (181-183) at the Demon Lord's Castle (79;
/// <c>Gｹｲﾌﾞﾘｽ</c> 4 on the first attack), Medusa (134) with two at Kayblis's Castle (84),
/// else the four Monster Generals (86-89); only the last two castles keep their walls (NAT-21).
/// Retreat mode 1 throughout; nobody is left out of a pick. The army name (<c>MS 10</c>) is read from
/// the player's files through <paramref name="scenario"/>.
/// </summary>
public sealed class KayblisDefenders(ScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "Tｹｲﾌﾞﾘｽ2.ADV", scenario)
{
    /// <summary><c>Tｹｲﾌﾞﾘｽ2.ADV:1-60</c>, then <c>L_001c7</c> at stage 3 and at the gate.</summary>
    public async Task RunAsync()
    {
        State.Ev_PhaseNation = 8;                                       // :1
        SetLiteral(3);                                                  // :2-3 MS 10, "Kayblis Army"
        State.Tmp0226 = 4;                                              // :4-11 the same province again?
        if (State.Bt_GarrisonProv == State.Prov_Cur)
            State.Tmp0226 = 2 + State.Bt_GarrisonHits;
        if (State.Bt_GarrisonProv != State.Prov_Cur)
        {
            State.Bt_GarrisonHits = 0;
            State.Bt_GarrisonProv = State.Prov_Cur;
        }
        CallUpMonsterGeneral(86);                                       // :12-31
        CallUpMonsterGeneral(87);
        CallUpMonsterGeneral(88);
        CallUpMonsterGeneral(89);
        if (State.StoryStage == 3)                                      // :32-34
        {
            await StageThreeAsync();
            return;
        }
        State.Bt_EnemyRetreatMode = 1;                                  // :35-37
        State.Bt_TroopLimit = Prov.BattleArea;                          // QUIRK: NAT-16 — FIGT_A recomputes the limit
        State.Bt_WallBonus = Prov.Defense;
        // QUIRK: NAT-09 — only Tearing Forest (91) is gated by the stage.
        if (State.Prov_Cur == 78 || State.Prov_Cur == 89 || State.Prov_Cur == 90 || (State.Prov_Cur == 91 && State.StoryStage == 2))
        {
            State.StoryStage = 3;                                       // :38-41 the gate
            await StageThreeAsync();
            return;
        }
        State.Bt_EnemyRetreatMode = 1;                                  // :42-44 the plains
        State.Bt_TroopLimit = 5000;
        State.Bt_WallBonus = 0;
        Script.Rnd(2);                                                  // :45-58 two Monster Generals, or four
        SetPriority(86, 87);
        if (State.RetVal == 1)
        {
            State.Tmp0003 = 88;
            State.Tmp0004 = 89;
        }
        PickArmy();                                                     // \L_004a2
    }                                                                   // :60 %0:

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ2.ADV:12-31</c>, one of four: a Monster General under <paramref name="commander"/>
    /// alive at full HP, his troops <c>MaxTroops / (4 + Bt_GarrisonHits × 2) + RND(MaxTroops / Tmp0226)</c>.
    /// </summary>
    private void CallUpMonsterGeneral(Sys3Int commander)
    {
        State.Cmdr_Cur = commander;
        Cmdr.Status = 1;
        Cmdr.HP = Cmdr.MaxHP;
        Script.Rnd(Cmdr.MaxTroops / State.Tmp0226);
        Cmdr.Troops = Cmdr.MaxTroops / (4 + State.Bt_GarrisonHits * 2) + State.RetVal;
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ2.ADV:61-123</c> (<c>L_001c7</c>) — the named garrisons of stage 3, up to the
    /// file's <c>%0:</c> (the labels <c>L_0032f</c>, <c>L_0038d</c>, <c>L_003d2</c>, <c>L_00407</c> it jumps
    /// to end in one each). Entered at stage 3 the walls are left as <c>Cﾃｷﾄｼ.ADV:317</c> zeroed them.
    /// </summary>
    private async Task StageThreeAsync()
    {
        // QUIRK: NAT-21 — at stage 3 only the Demon Lord's Castle (79) and Kayblis's Castle (84) set walls.
        State.Cmdr_Cur = 159;                                           // :62-76 Kaybwan at the Blue House
        if (Cmdr.Status != 0 && State.Prov_Cur == 86)
        {
            State.Bt_EnemyRetreatMode = 1;
            SetPriority(160, 161, 159);
            PickArmy();
            return;
        }
        State.Cmdr_Cur = 162;                                           // :77-91 Kaybnyan at the Red House
        if (Cmdr.Status != 0 && State.Prov_Cur == 85)
        {
            State.Bt_EnemyRetreatMode = 1;
            SetPriority(163, 164, 162);
            PickArmy();
            return;
        }
        if (State.Prov_Cur == 78 && State.N_Warg_Stage == 0)            // :92-96 Warg at the Tower of Evil
        {
            await Script.EventAsync("Gﾜｰｸﾞ.ADV", 1);
            return;
        }
        State.Cmdr_Cur = 133;                                           // :97-100 Kesselring's Castle
        if (Cmdr.Status != 0 && State.Prov_Cur == 87)
        {
            SetPriority(133, 87, 88, 89);                               // @L_003d2, :160-172
            PickArmy();
            State.Bt_EnemyRetreatMode = 1;
            return;
        }
        State.Cmdr_Cur = 91;                                            // :101-104 Camilla's Castle
        if (Cmdr.Status != 0 && State.Prov_Cur == 88)
        {
            CamillasCastle();                                           // @L_00407
            return;
        }
        if (State.Prov_Cur == 79)                                       // :105-107 the Demon Lord's Castle
        {
            await DemonLordsCastleAsync();                              // @L_0032f
            return;
        }
        State.Cmdr_Cur = 134;                                           // :108-111 Medusa at Kayblis's Castle
        if (Cmdr.Status != 0 && State.Prov_Cur == 84)
        {
            State.Bt_EnemyRetreatMode = 1;                              // @L_0038d, :144-159
            State.Bt_TroopLimit = Prov.BattleArea;                      // QUIRK: NAT-16
            State.Bt_WallBonus = Prov.Defense;
            SetPriority(134, 87, 88);
            PickArmy();
            State.Bt_EnemyRetreatMode = 1;
            return;
        }
        SetPriority(86, 87, 88, 89);                                    // :112-123 the four Monster Generals
        PickArmy();
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ2.ADV:173-190</c> (<c>L_00407</c>) — Camilla (91) with Monster General (88); a
    /// maximum of 200 is raised to 2000 and filled.
    /// </summary>
    private void CamillasCastle()
    {
        State.Cmdr_Cur = 91;
        // QUIRK: NAT-10 — Camilla's maximum patched from 200 to 2000.
        if (Cmdr.MaxTroops == 200)
        {
            Cmdr.Troops = 2000;
            Cmdr.MaxTroops = 2000;
        }
        SetPriority(91, 88);
        PickArmy();
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>Tｹｲﾌﾞﾘｽ2.ADV:124-143</c> (<c>L_0032f</c>) — Kayblis (135) with Monster Generals (181-183),
    /// walls; <c>Gｹｲﾌﾞﾘｽ</c> 4 first on the first attack (Kayblis hears of it).
    /// </summary>
    private async Task DemonLordsCastleAsync()
    {
        if (State.F_DemonCastle_Attacked == 0)
            await Script.EventAsync("Gｹｲﾌﾞﾘｽ.ADV", 4);
        State.Bt_EnemyRetreatMode = 1;
        State.Bt_TroopLimit = Prov.BattleArea;                          // QUIRK: NAT-16
        State.Bt_WallBonus = Prov.Defense;
        SetPriority(181, 182, 183, 135);
        PickArmy();
        State.Bt_EnemyRetreatMode = 1;
    }
}
