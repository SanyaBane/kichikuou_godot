using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// 死亡捕獲.ADV: deaths, revivals and story scenes after a battle (docs/battle/01-flow.md §11.4).
public sealed partial class Battle
{
    /// <summary>
    /// <c>%#死亡捕獲.ADV</c> (<c>死亡捕獲.ADV:1-442</c>, 01 §11.4): after a battle (called by the
    /// wrappers with <c>Prov_Cur = Bt_TargetProv</c>, and by the dungeon and some story scenes
    /// directly) — mercenary losses, Rance's death (game over outside the prologue), Sill's
    /// prologue revival, the Somita counter, and per enemy commander a story scene or a revival.
    /// Story scenes run through <see cref="IStoryHooks"/> and may change any state, so every test
    /// after one re-reads the state. Draws nothing.
    /// </summary>
    public async Task DeathCaptureAsync()
    {
        if (InPlayerParty(10))
        {
            State.Unit_Cur = 10;
            if (State.Bt_Kind != 3 && Unit.HP != 0)
            {
                if (State.F_Menad_State == 4)
                    await EventAsync("Gﾒﾅﾄﾞ.ADV", 2);
                if (State.F_Menad_State == 3 && Unit.Troops * 3 < Unit.MaxTroops)
                    await EventAsync("Gﾒﾅﾄﾞ.ADV", 1);
            }
        }
        if (InPlayerParty(49))
        {
            State.Unit_Cur = 49;
            if (Unit.Status == 0)
                State.F_MercA_Lost = 1;
        }
        if (InPlayerParty(50))
        {
            State.Unit_Cur = 50;
            if (Unit.Status == 0)
                State.F_MercB_Lost = 1;
        }
        State.Tmp0001 = 1;
        UnitIn();
        State.Unit_Cur = 1;
        if (Unit.Status == 0 && State.StoryStage != 0)
            JumpToGameOver(1);
        State.Tmp0001 = 2;
        CmdrIn();   // computed and unused (01 §11.4 row 4)
        State.Unit_Cur = 2;
        if (Unit.Status == 0 && State.StoryStage == 0)
        {
            Unit.Status = 2;
            Unit.HP = 1;
        }
        State.Tmp0001 = 46;
        UnitIn();
        if (State.Tmp0002 == 1)
            State.N_Somita_BattlesSurvived = State.N_Somita_BattlesSurvived + 1;
        await DeadCmdrEventAsync(4, "Gﾒﾙﾌｪｲｽ.ADV", 1);
        State.Tmp0001 = 2;
        CmdrIn();
        if (State.Tmp0002 == 2)
            await EventAsync("Gﾊｳﾚｰﾝ.ADV", 1);
        State.Tmp0001 = 1;
        CmdrIn();
        if (State.Tmp0002 == 2)
            Revive(1);
        State.Tmp0001 = 72;
        CmdrIn();
        if (State.Tmp0002 == 2)
            Revive(1);
        await DeadCmdrEventAsync(82, "Gｲｿﾛｸ.ADV", 1);
        await DeadCmdrEventAsync(22, "Gﾚﾘｭｰｺﾌ.ADV", 3);
        await DeadCmdrEventAsync(44, "Gﾈﾛ.ADV", 3);
        if (InEnemyParty(45))
        {
            State.Cmdr_Cur = 45;
            if (Cmdr.Status == 0)
            {
                await EventAsync("Gｸﾘｰﾑ.ADV", 2);
                if (State.F_Nero_Helman4thState != 100)
                    State.F_Nero_Helman4thState = 5;
            }
            if (State.F_Nero_Helman4thState == 4)
            {
                State.F_Nero_Helman4thState = 5;
                await EventAsync("Gｸﾘｰﾑ.ADV", 1);
            }
        }
        State.Tmp0001 = 31;
        CmdrIn();
        if (State.StoryStage == 0)
            Cmdr.Status = 1;
        if (State.Tmp0002 == 2)
        {
            if (State.StoryStage != 0 && Cmdr.Status == 0)
                await EventAsync("Gﾙｰﾍﾞﾗﾝ.ADV", 2);
        }
        await DeadCmdrEventAsync(32, "Gｱﾘｽﾄﾚｽ.ADV", 2);
        State.Tmp0001 = 54;
        CmdrIn();
        if (State.Tmp0002 == 2)
            await EventAsync("Gﾊﾟｯﾄﾝ.ADV", 5);
        for (int id = 55; id <= 57; id++)
        {
            State.Tmp0001 = (Sys3Int)id;
            CmdrIn();
            if (State.Tmp0002 == 2)
                Revive(1);
        }
        await DeadCmdrEventAsync(51, "Gｶﾁｭｰｼｬ.ADV", 1);
        await DeadCmdrEventAsync(52, "Gｿﾙﾆｱ.ADV", 1);
        await DeadCmdrEventAsync(50, "Gﾛﾚｯｸｽ.ADV", 3);
        if (InEnemyParty(39))
        {
            State.Cmdr_Cur = 39;
            if (Cmdr.Status == 0)
            {
                Revive(2);
                if (State.Prov_Cur == 45 && State.Bt_Result == 2 && State.Bt_IsDuel == 0)
                {
                    Cmdr.Status = 0;
                    await EventAsync("Gﾐﾈﾊﾞ.ADV", 5);
                }
            }
        }
        await ZethLordAsync(101, "Gﾁｽﾞｺ.ADV", 2);
        await ZethLordAsync(104, "Gﾊﾟﾊﾟｲｱ.ADV", 4);
        await ZethLordAsync(110, "Gﾏｼﾞｯｸ.ADV", 23);
        await TowerBossAsync(113, 58, 40);
        if (InEnemyParty(107))
        {
            State.Cmdr_Cur = 107;
            if (Cmdr.Status == 0)
            {
                Revive(1);
                if (State.Prov_Cur == 67 && State.Bt_Result == 2)
                {
                    Cmdr.Status = 0;
                    await EventAsync("Gﾅｷﾞ.ADV", 6);
                }
            }
        }
        if (InEnemyParty(109))
        {
            State.Cmdr_Cur = 109;
            if (Cmdr.Status == 0)
            {
                Cmdr.Status = 1;
                Cmdr.HP = Cmdr.MaxHP;
            }
        }
        State.Tmp0001 = 125;
        CmdrIn();
        if (State.Tmp0002 == 2 && State.Prov_Cur == 66)
            await EventAsync("Gﾏｼﾞｯｸ.ADV", 2);
        if (State.Tmp0002 == 2 && State.Prov_Cur != 66)
            Revive(1);
        if (State.Tmp0002 == 2 && State.Prov_Cur == 63 && State.Bt_Result == 2)
        {
            Cmdr.Status = 0;
            await EventAsync("Iｾﾞｽ.ADV", 41);
        }
        await TowerBossAsync(121, 65, 42);
        await TowerBossAsync(117, 61, 43);
        if (State.F_Zeth_War == 1)
        {
            State.Cmdr_Cur = 101;
            if (Cmdr.Status == 0)
            {
                State.Cmdr_Cur = 104;
                if (Cmdr.Status == 0)
                {
                    State.Cmdr_Cur = 107;
                    if (Cmdr.Status == 0)
                    {
                        State.Cmdr_Cur = 110;
                        if (Cmdr.Status == 0)
                            await EventAsync("Iｾﾞｽ.ADV", 100);
                    }
                }
            }
        }
        await DeadCmdrEventAsync(129, "Gﾊﾞﾎﾞﾗ.ADV", 10);
        await DeadCmdrEventAsync(130, "Gﾚｯﾄﾞｱｲ.ADV", 10);
        await DeadCmdrEventAsync(132, "Gｶｲﾄ.ADV", 10);
        await DeadCmdrEventAsync(131, "Gｶﾞﾙﾃｨｱ.ADV", 10);
        State.Tmp0001 = 133;
        CmdrIn();
        if (State.Tmp0002 == 2 && State.Bt_IsDuel == 0)
            await EventAsync("Gｹｯｾﾙ.ADV", 10);
        await DeadCmdrEventAsync(134, "Gﾒﾃﾞｨｳｻ.ADV", 10);
        // QUIRK: EVT-20 — Gｹｲﾌﾞﾘｽ 10 → Iｼﾚｲﾌﾞ 7 leaves Prov_Cur at 92 for the tests below.
        await DeadCmdrEventAsync(135, "Gｹｲﾌﾞﾘｽ.ADV", 10);
        State.Tmp0001 = 98;
        CmdrIn();
        if (State.Tmp0002 == 2)
            await EventAsync("Gｻｲｾﾞﾙ.ADV", 11);
        State.Tmp0001 = 91;
        CmdrIn();
        if (State.Tmp0002 == 2)
        {
            Revive(2);
            if (State.Prov_Cur == 88 && State.Bt_Result == 2)
            {
                Cmdr.Status = 0;
                await EventAsync("Gｶﾐｰﾗ.ADV", 11);
            }
        }
        await DeadCmdrEventAsync(136, "Gﾊﾟｲｱｰﾙ.ADV", 11);
        await DeadCmdrEventAsync(137, "Gﾊﾟｲｱｰﾙ.ADV", 12);
        await KayblisGuardAsync(159, "Gｹｲﾌﾞﾜﾝ.ADV");
        await KayblisGuardAsync(162, "Gｹｲﾌﾞﾆｬﾝ.ADV");
        State.Tmp0001 = 5;
        UnitIn();
        if (State.Tmp0002 == 1)
        {
            State.Tmp0001 = 7;
            UnitIn();
            if (State.Tmp0002 == 1 && State.F_LeilaRick_State < 6 && State.Prov_Cur != 1 &&
                State.F_Leila_State > 3 && State.Bt_Result == 2)
            {
                await EventAsync("Gﾚｲﾗ.ADV", 2);
                return;
            }
        }
        State.Tmp0001 = 6;
        UnitIn();
        if (State.Tmp0002 == 1)
        {
            State.Tmp0001 = 12;
            UnitIn();
            if (State.Tmp0002 == 1 && Unit.Level > 3 && State.F_Haurein_Fate == 0 && State.Bt_Result == 2)
            {
                await EventAsync("Gﾊｳﾚｰﾝ.ADV", 2);
                return;
            }
        }
    }

    /// <summary>
    /// <c>L_00d4d</c>, <c>死亡捕獲.ADV:443-452</c>: unit <c>Tmp0001</c> → <c>Tmp0002</c> = 0 not
    /// deployed, 1 deployed and alive, 2 deployed and fallen; leaves <c>Unit_Cur</c> on it.
    /// </summary>
    internal void UnitIn()
    {
        State.Unit_Cur = State.Tmp0001;
        State.Tmp0002 = 0;
        if (InPlayerParty(State.Tmp0001))
        {
            State.Tmp0002 = 1;
            if (Unit.Status == 0)
                State.Tmp0002 = 2;
        }
    }

    /// <summary>
    /// <c>L_00d90</c>, <c>死亡捕獲.ADV:453-462</c>: commander <c>Tmp0001</c> → <c>Tmp0002</c> = 0 not
    /// in the enemy party, 1 alive, 2 fallen; leaves <c>Cmdr_Cur</c> on it.
    /// </summary>
    internal void CmdrIn()
    {
        State.Cmdr_Cur = State.Tmp0001;
        State.Tmp0002 = 0;
        if (InEnemyParty(State.Tmp0001))
        {
            State.Tmp0002 = 1;
            if (Cmdr.Status == 0)
                State.Tmp0002 = 2;
        }
    }

    /// <summary><c>Cmdr_Status := 1; Cmdr_HP := hp</c> on the current commander.</summary>
    private void Revive(Sys3Int hp)
    {
        Cmdr.Status = 1;
        Cmdr.HP = hp;
    }

    /// <summary>The pattern <c>{id ∈ EParty: Cmdr_Cur := id; {Cmdr_Status = 0: Event(file, entry)}}</c>.</summary>
    private async Task DeadCmdrEventAsync(Sys3Int id, string file, Sys3Int entry)
    {
        if (InEnemyParty(id))
        {
            State.Cmdr_Cur = id;
            if (Cmdr.Status == 0)
                await EventAsync(file, entry);
        }
    }

    /// <summary>
    /// <c>死亡捕獲.ADV:181-219</c>: a fallen Zeth lord — on a win its scene, otherwise revived
    /// with 1 HP (the two tests are independent: the scene may change <c>Bt_Result</c>).
    /// </summary>
    private async Task ZethLordAsync(Sys3Int id, string file, Sys3Int entry)
    {
        if (InEnemyParty(id))
        {
            State.Cmdr_Cur = id;
            if (Cmdr.Status == 0)
            {
                if (State.Bt_Result == 2)
                    await EventAsync(file, entry);
                if (State.Bt_Result != 2)
                    Revive(1);
            }
        }
    }

    /// <summary>
    /// <c>死亡捕獲.ADV:220-236</c>, <c>:271-304</c>: a fallen tower boss — revived with 1 HP away
    /// from its home province; at home a win plays <c>Iｾﾞｽ</c> <paramref name="entry"/>, a loss
    /// sets <c>Status 2, HP 1</c>.
    /// </summary>
    private async Task TowerBossAsync(Sys3Int id, Sys3Int home, Sys3Int entry)
    {
        if (InEnemyParty(id))
        {
            State.Cmdr_Cur = id;
            if (Cmdr.Status == 0 && State.Prov_Cur != home)
                Revive(1);
            if (Cmdr.Status == 0 && State.Prov_Cur == home)
            {
                if (State.Bt_Result == 2)
                    await EventAsync("Iｾﾞｽ.ADV", entry);
                if (State.Bt_Result == 1)
                {
                    Cmdr.Status = 2;
                    Cmdr.HP = 1;
                }
            }
        }
    }

    /// <summary>
    /// <c>死亡捕獲.ADV:400-419</c>: a fallen Kayblis guard is revived (<c>Status := 1</c>); on a
    /// win it is set back to 0 and its scene plays.
    /// </summary>
    private async Task KayblisGuardAsync(Sys3Int id, string file)
    {
        State.Tmp0001 = id;
        CmdrIn();
        if (State.Tmp0002 == 2)
        {
            Cmdr.Status = 1;
            if (State.Bt_Result == 2)
            {
                Cmdr.Status = 0;
                await EventAsync(file, 1);
            }
        }
    }
}
