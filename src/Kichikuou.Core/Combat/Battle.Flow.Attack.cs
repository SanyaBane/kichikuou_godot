using Kichikuou.Core.Script;
namespace Kichikuou.Core.Combat;

// FIGT_A.ADV: the player attacks a province (docs/battle/01-flow.md §2.1, §2.6, §11.3).
public sealed partial class Battle
{
    /// <summary>
    /// <c>%#FIGT_A.ADV</c> (<c>FIGT_A.ADV:1-204</c>, 01 §2.1): the player's attack — pre-battle
    /// story hooks, plains or city from <c>Bt_WallBonus</c>, the battle, the result messages and
    /// ownership change, story hooks, <c>死亡捕獲.ADV</c>.
    /// </summary>
    /// <remarks>
    /// Preconditions (the caller — <c>Cﾃｷﾄｼ.ADV</c> with a <c>T*2.ADV</c> defender setup, or a story
    /// scene — sets them; the defender selection is not part of this call path, 01 §2.2):
    /// <c>Prov_Cur</c> = <c>Bt_TargetProv</c> = the attacked province; <c>Bt_PlayerParty1..4</c>
    /// (deployed units, <c>Unit_Status = 2</c>, cost paid); <c>Bt_EnemyParty1..4</c> (slot 1
    /// non-zero); <c>Bt_WallBonus</c> (0 = plains; else city, limit <c>Prov_BattleArea</c>);
    /// <c>Bt_EnemyRetreatMode</c>; <c>Bt_IsDuel</c> (FIGT_A does not reset it);
    /// <c>Bt_BackgroundCg</c> (normally 900). <c>Ev_PhaseNation</c> is read by one story check.
    /// A game over (Rance's death) throws <see cref="ScenarioJumpException"/>.
    /// </remarks>
    public async Task AttackProvinceAsync()
    {
        await AttackPreludeAsync();
        await FightAsync();
        await AttackAftermathAsync();
    }

    /// <summary>
    /// <c>FIGT_A.ADV:1-68</c>: story hooks before the battle (Ex, JAPAN invasion), plains or city,
    /// background, <c>Bt_Kind := 1</c>.
    /// </summary>
    internal async Task AttackPreludeAsync()
    {
        if (State.Prov_Cur == 5 && State.F_Ex_State == 1)
            await EventAsync("Gｴｸｽ.ADV", 2);
        if (State.Prov_Cur == 6 && State.F_Ex_State == 2)
            await EventAsync("Gｴｸｽ.ADV", 3);
        if (State.Prov_Cur == 55 && State.N_JapanInv_Stage == 0)
        {
            State.N_JapanInv_Stage = 1;
            State.Cmdr_Cur = 75;
            State.Tmp0001 = 0;
            if (Cmdr.Status != 0)
                State.Tmp0001 = 1;
            State.Cmdr_Cur = 82;
            State.Tmp0002 = 0;
            if (Cmdr.Status != 0)
                State.Tmp0002 = 1;
            if (State.Tmp0001 == 1 && State.Tmp0002 == 1)
                await EventAsync("IJAPAN.ADV", 3);
        }
        if (State.Prov_Cur == 56 && State.N_JapanInv_Stage == 1)
        {
            State.N_JapanInv_Stage = 2;
            await EventAsync("IJAPAN.ADV", 4);
        }
        if (State.Bt_WallBonus == 0)
        {
            State.Bt_TroopLimit = 5000;
            await WaoAsync(113);
            await FlowTextAsync(FigtAFile, 35);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
        }
        if (State.Bt_WallBonus != 0)
        {
            State.Bt_TroopLimit = Prov.BattleArea;
            await WaoAsync(113);
            await FlowTextAsync(FigtAFile, 47);
            await FlowWaitKeyKeepRetValAsync();
            State.Bt_BackgroundCg = 902;
            await WaoAsync(0);
        }
        if (State.Prov_Cur > 77 && State.Bt_BackgroundCg == 900)
        {
            State.Bt_BackgroundCg = 908;
            if (State.Prov_Cur == 82 || State.Prov_Cur == 83)
                State.Bt_BackgroundCg = 918;
            if (State.Prov_Cur == 79)
                State.Bt_BackgroundCg = 910;
        }
        if (State.Bt_EnemyParty1 == 18)
            State.Bt_BackgroundCg = 904;
        State.Bt_Kind = 1;
    }

    /// <summary>
    /// <c>FIGT_A.ADV:70-204</c> (01 §2.1, §11.3): defeat or victory (outside the prologue a
    /// non-duel victory gives the current province to the player, Q-25; a defeat keeps the
    /// attack command available, Q-2), story hooks, <c>死亡捕獲.ADV</c>, more story hooks.
    /// </summary>
    internal async Task AttackAftermathAsync()
    {
        State.Bt_BackgroundCg = 900;
        await FlowMapAsync(0);
        if (State.Bt_IsDuel != 1)
        {
            if (State.Bt_Result == 1)
            {
                await FlowSuspendMusicAsync();
                State.RetVal = 2;
                await FlowSoundAsync(0);
                await WaoAsync(113);
                await FlowTextAsync(FigtAFile, 86);
                if (State.Bt_Kind == 1 && State.StoryStage != 0)
                    await FlowTextAsync(FigtAFile, 90);
                await FlowWaitKeyKeepRetValAsync();
                await FlowSoundAsync(3);
                await FlowResumeMusicAsync();
                await WaoAsync(0);
            }
            if (State.Bt_Result == 2)
            {
                // QUIRK: Q-25 / Q-2 — any non-duel win outside the prologue takes Prov_Cur.
                if (State.StoryStage != 0 && State.Bt_Kind == 1)
                {
                    Prov.Owner = 1;
                    State.Ui_CmdAttackAvail = 0;
                }
                State.Tmp0021 = Prov.Owner + 819;
                State.MapTargetX = Prov.MapX;
                State.MapTargetY = Prov.MapY;
                await FlowMapAsync(4);
                await FlowMapAsync(3);
                await FlowMapAsync(0);
                await FlowSuspendMusicAsync();
                State.RetVal = 1;
                await FlowSoundAsync(0);
                await FlowMapAsync(13);
                await WaoAsync(113);
                await FlowTextAsync(FigtAFile, 128);
                if (State.StoryStage != 0 && State.Bt_Kind == 1)
                    await FlowTextAsync(FigtAFile, 132);
                await FlowWaitKeyKeepRetValAsync();
                await WaoAsync(0);
                await FlowSoundAsync(3);
                await FlowResumeMusicAsync();
            }
        }
        // L_00380 (:147)
        State.Bt_Kind = 0;
        State.Prov_Cur = State.Bt_TargetProv;
        if (State.Bt_IsDuel == 1)
        {
            State.Prov_Cur = 1;
            State.Bt_TargetProv = 1;
        }
        if (State.Prov_Cur == 56 && State.N_JapanInv_Stage == 2)
        {
            State.N_JapanInv_Stage = 3;
            State.Cmdr_Cur = 72;
            if (Cmdr.Status != 0)
                await EventAsync("IJAPAN.ADV", 5);
        }
        if (State.Prov_Cur == 56 && State.F_Japan_State != 10 && State.Bt_Result == 2)
            await EventAsync("IJAPAN.ADV", 6);
        if (State.Prov_Cur == 30 && State.Bt_Result == 2 && State.F_SillKidnap_Stage < 50)
            await EventAsync("Gｼｨﾙ.ADV", 28);
        State.Unit_Cur = 9;
        if ((State.Bt_PlayerParty1 == 9 || State.Bt_PlayerParty2 == 9) && Unit.Status != 0 &&
            State.F_Kinkaid_Done == 0 && State.Bt_Result == 2)
        {
            if (State.Prov_Cur > 29 && State.Prov_Cur < 54)
                await EventAsync("Gｷﾝｹｰﾄﾞ.ADV", 1);
        }
        State.Unit_Cur = 39;
        if (InPlayerParty(39) && Unit.Status != 0)
        {
            if (State.Prov_Cur == 91 && State.Bt_Result == 2 && State.F_Magic_State == 5)
                await EventAsync("Gﾏｼﾞｯｸ.ADV", 25);
        }
        if (State.Prov_Cur == 69 && Prov.Owner == 1 && State.Ev_PhaseNation == 7)
            await EventAsync("Iｾﾞｽ.ADV", 50);
        State.Prov_Cur = State.Bt_TargetProv;
        await DeathCaptureAsync();
        State.Prov_Cur = State.Bt_TargetProv;
        State.Bt_TargetProv = 0;
        if (State.StoryStage == 1 && State.Prov_Cur == 6 && State.Bt_Result == 2)
            await EventAsync("Iｼﾚｲﾌﾞ.ADV", 2);
        if (State.Prov_Cur == 45 && State.Bt_Result == 2 && State.F_Helman_War == 1)
            await EventAsync("Gﾐﾈﾊﾞ.ADV", 5);
        if (State.Prov_Cur == 77 && State.N_ALChurch_Stage < 50 && State.Bt_Result == 2)
            await EventAsync("GAL.ADV", 10);
    }
}
