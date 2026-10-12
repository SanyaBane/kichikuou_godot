using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Tｾﾞｽ2.ADV</c>, the defenders of a Zeth province the player attacks (part N2, milestone 3 step
/// 2; called by <c>Cﾃｷﾄｼ.ADV:344-346</c> before <c>FIGT_A</c>), ported one-to-one on the nation
/// template (<c>docs/strategy/survey-nations.md</c> §2.5). The garrison — Wall (94), Wall (95) and
/// the Mage Leader (96) — is called up at full strength for every attack, less a draw of up to half
/// its troops each when the same province is attacked again; Papaya's trap is set by an attack of
/// two units or more; the first attack starts the war (<c>Iｾﾞｽ</c> 1). A general's city sends his
/// army (Saias (113) at Adam's Fortress (58), Uspira (117) at Tape (61), Kabachahn (121) at Amber
/// Castle (65), Alex (125) at Old Zeth (63); the general alone gets the two Walls at 10 troops). A
/// minister's tower — Magic's Tower (66), Nagi's Tower (67), Chizuko's Tower (68), Papaya's Tower
/// (70) — sends the army back (an empty player party) while the barrier stands and no unit of the
/// party holds the Barrier Blade (item 12), else her scene and her army (<c>Gﾏｼﾞｯｸ</c> 1 may send
/// Alex's instead). Anywhere else, or when the leader is dead, the garrison defends (the Mage Leader
/// alone at Magic's Tower). Retreat mode 1 throughout; nobody is left out of a pick. The army name
/// (<c>MS 10</c>) is read from the player's files through <paramref name="scenario"/>.
/// </summary>
public sealed class ZethDefenders(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "Tｾﾞｽ2.ADV", scenario)
{
    /// <summary><c>Tｾﾞｽ2.ADV:1-142</c>, then the label the province leads to.</summary>
    public async Task RunAsync()
    {
        State.Ev_PhaseNation = 7;                                       // :1
        SetLiteral(3);                                                  // :2-3 MS 10, "Zeth Army"
        State.Bt_TroopLimit = Prov.BattleArea;                          // :4-9 QUIRK: NAT-16 — FIGT_A recomputes the limit
        State.Bt_WallBonus = Prov.Defense;
        if (State.Prov_Cur == 16)                                       // Parapara Fortress: no walls
        {
            State.Bt_TroopLimit = 5000;
            State.Bt_WallBonus = 0;
        }
        State.Tmp0226 = 0;                                              // :10-17 the same province again?
        if (State.Bt_GarrisonProv == State.Prov_Cur)
            State.Tmp0226 = 1;
        if (State.Bt_GarrisonProv != State.Prov_Cur)
        {
            State.Bt_GarrisonHits = 0;
            State.Bt_GarrisonProv = State.Prov_Cur;
        }
        CallUpGarrison(94);                                             // :18-41
        CallUpGarrison(95);
        CallUpGarrison(96);
        if (State.F_Papaya_State == 1 && State.Bt_PlayerParty1 != 0 && State.Bt_PlayerParty2 != 0)
            State.F_Papaya_State = 2;                                   // :42-44 Papaya's trap
        if (State.F_Zeth_War == 0)                                      // :45-49 the first attack starts the war
        {
            State.F_Zeth_War = 1;
            await Script.EventAsync("Iｾﾞｽ.ADV", 1);
        }
        if (General(58, 113, 115, 116, 114))                            // :50-73 @L_0050c, Saias
            return;
        if (General(61, 117, 120, 119, 118))                            // @L_0057e, Uspira
            return;
        if (General(65, 121, 124, 123, 122))                            // @L_005f0, Kabachahn
            return;
        if (General(63, 125, 128, 127, 126))                            // @L_00662, Alex
            return;
        State.Tmp0001 = 0;                                              // :74-90 the Barrier Blade in the party?
        // QUIRK: ENG-06 — an empty party slot reads unit record 0's item.
        State.Unit_Cur = State.Bt_PlayerParty1;
        if (Unit.Item == 12)
            State.Tmp0001 = 1;
        State.Unit_Cur = State.Bt_PlayerParty2;
        if (Unit.Item == 12)
            State.Tmp0001 = 1;
        State.Unit_Cur = State.Bt_PlayerParty3;
        if (Unit.Item == 12)
            State.Tmp0001 = 1;
        State.Unit_Cur = State.Bt_PlayerParty4;
        if (Unit.Item == 12)
            State.Tmp0001 = 1;
        if (State.Prov_Cur == 66)                                       // :91-100 Magic's Tower
        {
            if (BarrierHolds())
            {
                State.Bt_PlayerParty1 = 0;                              // Cﾃｷﾄｼ.ADV:381-386 sends the army back
                return;
            }
            State.Cmdr_Cur = 110;
            if (Cmdr.Status != 0)
            {
                await MagicAsync();                                     // @L_003d1
                return;
            }
        }
        if (await MinisterAsync(67, 107, "Gﾅｷﾞ.ADV", 5, 108, 109, 107)) // :101-110 @L_00455, Nagi
            return;
        if (await MinisterAsync(68, 101, "Gﾁｽﾞｺ.ADV", 1, 103, 102, 101)) // :111-120 @L_00492, Chizuko
            return;
        if (await MinisterAsync(70, 104, "Gﾊﾟﾊﾟｲｱ.ADV", 3, 106, 105, 104)) // :121-130 @L_004cf, Papaya
            return;
        State.Bt_EnemyParty1 = 94;                                      // :131-142 the garrison
        State.Bt_EnemyParty2 = 95;
        State.Bt_EnemyParty3 = 96;
        State.Bt_EnemyParty4 = 0;
        if (State.Prov_Cur == 66)
        {
            State.Bt_EnemyParty1 = 96;
            State.Bt_EnemyParty2 = 0;
            State.Bt_EnemyParty3 = 0;
            State.Bt_EnemyParty4 = 0;
        }
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>Tｾﾞｽ2.ADV:18-41</c> — one of the garrison (Wall 94, Wall 95, Mage Leader 96): alive, full
    /// troops, on a repeated attack less <c>RND(troops / 2)</c>, full HP.
    /// </summary>
    private void CallUpGarrison(Sys3Int commander)
    {
        State.Cmdr_Cur = commander;
        Cmdr.Status = 1;
        Cmdr.Troops = Cmdr.MaxTroops;
        if (State.Tmp0226 == 1)
        {
            Script.Rnd(Cmdr.Troops / 2);
            Cmdr.Troops = Cmdr.Troops - State.RetVal;
        }
        Cmdr.HP = Cmdr.MaxHP;
    }

    /// <summary>
    /// <c>Tｾﾞｽ2.ADV:50-73</c> and the general's label (<c>L_0050c</c>, <c>L_0057e</c>, <c>L_005f0</c>,
    /// <c>L_00662</c>, <c>:218-313</c>): at <paramref name="province"/>, while the general lives, his
    /// three officers and he; picked alone, he gets Wall (94) and Wall (95) with 10 troops each.
    /// Retreat mode 1. False (with <c>Cmdr_Cur</c> on the general when the province is his) when
    /// the file goes on.
    /// </summary>
    private bool General(int province, int general, int first, int second, int third)
    {
        if (State.Prov_Cur != (Sys3Int)province)
            return false;
        State.Cmdr_Cur = (Sys3Int)general;
        if (Cmdr.Status == 0)
            return false;
        SetPriority(first, second, third, general);
        PickArmy();                                                     // \L_0071a
        if (State.Bt_EnemyParty2 == 0)
        {
            State.Tmp0001 = 94;                                         // Tmp0005-0009 stay 0
            State.Tmp0002 = 95;
            State.Tmp0003 = (Sys3Int)general;
            State.Tmp0004 = 0;
            State.Cmdr_Cur = 94;
            Cmdr.Troops = 10;
            State.Cmdr_Cur = 95;
            Cmdr.Troops = 10;
            PickArmy();
        }
        State.Bt_EnemyRetreatMode = 1;
        return true;
    }

    /// <summary>
    /// <c>Tｾﾞｽ2.ADV:101-130</c> and the minister's label (<c>L_00455</c>, <c>L_00492</c>,
    /// <c>L_004cf</c>, <c>:173-217</c>): at <paramref name="province"/> the barrier sends the army
    /// back; else, while the minister lives, her scene, then her two officers and she, retreat mode 1.
    /// False when the file goes on.
    /// </summary>
    private async Task<bool> MinisterAsync(int province, int minister, string scene, int entry, int first, int second, int third)
    {
        if (State.Prov_Cur != (Sys3Int)province)
            return false;
        if (BarrierHolds())
        {
            State.Bt_PlayerParty1 = 0;
            return true;
        }
        State.Cmdr_Cur = (Sys3Int)minister;
        if (Cmdr.Status == 0)
            return false;
        await Script.EventAsync(scene, (Sys3Int)entry);
        SetPriority(first, second, third);
        PickArmy();                                                     // \L_0071a
        State.Bt_EnemyRetreatMode = 1;
        return true;
    }

    /// <summary>
    /// <c>Tｾﾞｽ2.ADV:143-172</c> (<c>L_003d1</c>) — Magic's Tower: <c>Gﾏｼﾞｯｸ</c> 1, which answers in
    /// <c>Tmp0001</c>: 1 sends Alex's army (128, 127, 126, 125), else Magic's own (112, 111, 110).
    /// Retreat mode 1.
    /// </summary>
    private async Task MagicAsync()
    {
        State.Tmp0001 = 0;
        await Script.EventAsync("Gﾏｼﾞｯｸ.ADV", 1);
        if (State.Tmp0001 == 1)
            SetPriority(128, 127, 126, 125);
        else
            SetPriority(112, 111, 110);
        PickArmy();                                                     // \L_0071a
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>(F_ZethBarrier_State ≠ 2) * (Tmp0001 = 0)</c> — the barrier stands and nobody in the party
    /// holds the Barrier Blade.
    /// </summary>
    private bool BarrierHolds() => State.F_ZethBarrier_State != 2 && State.Tmp0001 == 0;
}
