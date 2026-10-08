using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

/// <summary>The labels of <c>FIGT_B.ADV</c> that its goto machine jumps between.</summary>
internal enum FigtBLabel
{
    /// <summary><c>L_0003d</c> (<c>FIGT_B.ADV:5</c>): announce the enemy army.</summary>
    Announce,

    /// <summary><c>L_00105</c> (<c>:46</c>): Intercept / Ninja Scout / Abandon City.</summary>
    Menu,

    /// <summary><c>L_00152</c> (<c>:65</c>): intercept — party picker and refusals.</summary>
    Intercept,

    /// <summary><c>L_00393</c> (<c>:164</c>): fight on the plains.</summary>
    Plains,

    /// <summary><c>L_003e4</c> (<c>:181</c>): fight inside the city.</summary>
    City,

    /// <summary><c>L_0049b</c> (<c>:217</c>): the battle and its aftermath.</summary>
    Fight,

    /// <summary><c>L_00727</c> (<c>:311</c>): ninja scout.</summary>
    NinjaScout,

    /// <summary><c>L_00f1b</c> (<c>:714</c>): abandon the city.</summary>
    Abandon,

    /// <summary><c>%0</c>: return from FIGT_B.</summary>
    Return,
}

// FIGT_B.ADV: an enemy army attacks a player province (docs/battle/01-flow.md §2.3, §11.3).
public sealed partial class Battle
{
    /// <summary>The ninjas of the scout menu: menu code, flag accessor, menu text, scene lines, portrait.</summary>
    private static readonly (int Code, int MenuLine, int CallLine, int Portrait, int ArriveLine, int ArriveOp, int ReplyLine, int ReturnLine)[] Ninjas =
    [
        (1, 348, 398, 9, 406, 113, 413, 541),
        (2, 354, 422, 449, 430, 113, 437, 551),
        (3, 360, 446, 72, 454, 113, 461, 561),
        (4, 366, 470, 71, 478, 113, 485, 571),
        (5, 372, 494, 94, 0, 0, 502, 581),
    ];

    /// <summary>
    /// <c>%#FIGT_B.ADV</c> (<c>FIGT_B.ADV:1-883</c>, 01 §2.3): an enemy army attacks the
    /// player's province <c>Prov_Cur</c>. The player intercepts (party picker <c>Sﾌﾞﾀｲ.ADV</c>,
    /// then plains or city), scouts with a ninja, or abandons the city.
    /// </summary>
    /// <remarks>
    /// Preconditions (the nation-phase caller sets them): <c>Prov_Cur</c> = the attacked
    /// province (owner 1), <c>Ev_PhaseNation</c> = the attacking nation,
    /// <c>Bt_EnemyParty1..4</c>, <c>Bt_EnemyRetreatMode</c>, string 10 = the army's name.
    /// <c>Bt_IsDuel</c> is expected to be 0. The party picker <c>Sﾌﾞﾀｲ.ADV</c> and every story
    /// scene run through <see cref="IStoryHooks"/>; losing province 1 throws
    /// <see cref="ScenarioJumpException"/> (<c>OVER.ADV</c>, <c>EndingId = 2</c>).
    /// </remarks>
    public async Task DefendProvinceAsync()
    {
        if (State.Bt_EnemyParty1 == 1 && State.F_Ex_State == 0)
            await EventAsync("Gｴｸｽ.ADV", 1);
        var label = FigtBLabel.Announce;
        while (label != FigtBLabel.Return)
        {
            label = label switch
            {
                FigtBLabel.Announce => await DefenceAnnounceAsync(),
                FigtBLabel.Menu => await DefenceMenuAsync(),
                FigtBLabel.Intercept => await DefenceInterceptAsync(),
                FigtBLabel.Plains => await DefencePlainsAsync(),
                FigtBLabel.City => await DefenceCityAsync(),
                FigtBLabel.Fight => await DefenceFightAsync(),
                FigtBLabel.NinjaScout => await DefenceNinjaScoutAsync(),
                FigtBLabel.Abandon => await DefenceAbandonAsync(),
                _ => throw new InvalidOperationException(),
            };
        }
    }

    /// <summary><c>L_0003d</c>, <c>FIGT_B.ADV:5-45</c>: target province, map, "the army appeared".</summary>
    internal async Task<FigtBLabel> DefenceAnnounceAsync()
    {
        await FlowMapAsync(0);
        State.Bt_TargetProv = State.Prov_Cur;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await FlowMapAsync(1);
        await FlowMapAsync(10);
        FlowCopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);
        State.CallArg = Prov.BgCg;
        await WaoAsync(331);
        await WaoAsync(113);
        await FlowTextAsync(FigtBFile, 20);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await PortraitAsync(1, 311);
        await PortraitAsync(6, 321);
        await WaoAsync(121);
        await FlowTextAsync(FigtBFile, 35);
        await FlowWaitKeyKeepRetValAsync();
        await FlowTextAsync(FigtBFile, 43);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        return FigtBLabel.Menu;
    }

    /// <summary><c>L_00105</c>, <c>FIGT_B.ADV:46-64</c>: the three-way menu; cancelling shows it again.</summary>
    internal async Task<FigtBLabel> DefenceMenuAsync()
    {
        State.RetVal = 0;   // :47-50 IM release loop
        var menu = new BattleMenu(BattleMenuKind.EnemyAttackResponse,
        [
            new BattleMenuOption(1, new ScriptRef(FigtBFile, 53)),
            new BattleMenuOption(2, new ScriptRef(FigtBFile, 57)),
            new BattleMenuOption(3, new ScriptRef(FigtBFile, 61)),
        ]);
        var choice = await ChooseAsync(menu);
        if (choice == 1)
            return FigtBLabel.Intercept;
        if (choice == 2)
            return FigtBLabel.NinjaScout;
        if (choice == 3)
            return FigtBLabel.Abandon;
        return FigtBLabel.Menu;
    }

    /// <summary>
    /// <c>L_00152</c>, <c>FIGT_B.ADV:65-163</c>: the party picker (<c>Sﾌﾞﾀｲ.ADV</c>, at most 4 units,
    /// mode 2, return code in <c>CallArg</c>: 501 plains, 502 city, 503 stop) and the refusals of
    /// Gandhi, Satella and Meglass (deployment undone, gold not refunded, Q-1).
    /// </summary>
    internal async Task<FigtBLabel> DefenceInterceptAsync()
    {
        await WaoAsync(111);
        await FlowTextAsync(FigtBFile, 68);
        await FlowWaitKeyKeepRetValAsync();
        await FlowTextAsync(FigtBFile, 73);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await WaoAsync(312);
        await WaoAsync(322);
        State.Tmp0021 = 4;
        State.Tmp0022 = 2;
        await CallFileAsync("Sﾌﾞﾀｲ.ADV");
        State.Tmp0621 = State.CallArg;
        await FlowMapAsync(0);
        if (InPlayerParty(37) && State.Ev_PhaseNation == 7 && State.F_Zeth_State < 8)
            return await DefenceRefusalAsync(42, 90, 101);
        if (InPlayerParty(29) && State.Ev_PhaseNation != 8 && State.Ev_PhaseNation != 10)
            return await DefenceRefusalAsync(75, 115, 124);
        if (InPlayerParty(30) && State.Ev_PhaseNation != 8 && State.Ev_PhaseNation != 10)
            return await DefenceRefusalAsync(447, 138, 143);
        if (State.Tmp0621 == 501)
            return FigtBLabel.Plains;
        if (State.Tmp0621 == 502)
            return FigtBLabel.City;
        if (State.Tmp0621 == 503)
            return FigtBLabel.Announce;
        // Any other code falls through to the plains after a portrait refresh (open question 8).
        await PortraitAsync(1, 311);
        await PortraitAsync(6, 321);
        return FigtBLabel.Plains;
    }

    /// <summary>A refusal (<c>FIGT_B.ADV:84-149</c>): portrait, the unit's line, the notice, <c>L_01270</c>, back to the announcement.</summary>
    private async Task<FigtBLabel> DefenceRefusalAsync(Sys3Int portrait, int line, int notice)
    {
        await PortraitAsync(portrait, 311);
        await WaoAsync(111);
        await FlowTextAsync(FigtBFile, line);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await WaoAsync(113);
        await FlowTextAsync(FigtBFile, notice);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await WaoAsync(312);
        UndoDeploy();
        return FigtBLabel.Announce;
    }

    /// <summary>
    /// <c>L_01270</c>, <c>FIGT_B.ADV:884-901</c>: every deployed unit back to <c>Unit_Status = 1</c>
    /// (the war expenses paid in <c>Sﾌﾞﾀｲ</c> are not refunded, Q-1).
    /// </summary>
    internal void UndoDeploy()
    {
        for (int i = 1; i <= 4; i++)
        {
            if (PParty(i) != 0)
            {
                State.Unit_Cur = PParty(i);
                Unit.Status = 1;
            }
        }
    }

    /// <summary><c>L_00393</c>, <c>FIGT_B.ADV:164-180</c>: plains — limit 5000, no wall bonus.</summary>
    internal async Task<FigtBLabel> DefencePlainsAsync()
    {
        State.Bt_TroopLimit = 5000;
        State.Bt_WallBonus = 0;
        await PortraitAsync(1, 311);
        await WaoAsync(111);
        await FlowTextAsync(FigtBFile, 171);
        await FlowWaitKeyKeepRetValAsync();
        await FlowTextAsync(FigtBFile, 174);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        return FigtBLabel.Fight;
    }

    /// <summary>
    /// <c>L_003e4</c>, <c>FIGT_B.ADV:181-216</c>: city — the battle area as limit, the province
    /// defence as wall bonus; the battle area permanently grows by 10 % (Q-3); <c>RND(2)</c> [R1]
    /// decrements the write-only <c>VAR0692</c>.
    /// </summary>
    internal async Task<FigtBLabel> DefenceCityAsync()
    {
        State.Bt_TroopLimit = Prov.BattleArea;
        State.Bt_WallBonus = Prov.Defense;
        State.Bt_BackgroundCg = 902;
        // QUIRK: Q-3 — permanent +10 % after copying the old value into the limit.
        Prov.BattleArea = Prov.BattleArea + Prov.BattleArea / 10;
        Rnd(2);
        if (State.RetVal == 1)
            State.VAR0692 = State.VAR0692 - 1;
        await PortraitAsync(1, 311);
        await PortraitAsync(6, 321);
        await WaoAsync(111);
        await FlowTextAsync(FigtBFile, 196);
        await FlowWaitKeyKeepRetValAsync();
        await FlowTextAsync(FigtBFile, 203);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await WaoAsync(121);
        await FlowTextAsync(FigtBFile, 214);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        return FigtBLabel.Fight;
    }

    /// <summary>
    /// <c>L_0049b</c>, <c>FIGT_B.ADV:217-310</c>: Kayblis-war scenes that may remove units,
    /// background, <c>Bt_Kind := 2</c>, the battle, then <see cref="DefenceAftermathAsync"/>.
    /// </summary>
    private async Task<FigtBLabel> DefenceFightAsync()
    {
        var next = await DefenceBeforeBattleAsync();
        if (next != FigtBLabel.Fight)
            return next;
        await FightAsync();
        await DefenceAftermathAsync();
        return FigtBLabel.Return;
    }

    /// <summary>
    /// <c>FIGT_B.ADV:218-242</c>: the part of <c>L_0049b</c> before <c>%#FIGT.ADV</c>. Returns
    /// <see cref="FigtBLabel.Announce"/> when the party became empty, else <see cref="FigtBLabel.Fight"/>.
    /// </summary>
    internal async Task<FigtBLabel> DefenceBeforeBattleAsync()
    {
        if (State.F_Kayblis_War == 2 && State.Bt_EnemyParty1 == 135 && InPlayerParty(29))
        {
            await WaoAsync(322);
            await EventAsync("Gｹｲﾌﾞﾘｽ.ADV", 5);
        }
        if (State.F_Kayblis_War == 2 && State.Bt_EnemyParty1 == 135 && InPlayerParty(30))
        {
            await WaoAsync(322);
            await EventAsync("Gｹｲﾌﾞﾘｽ.ADV", 6);
        }
        if (State.F_Kayblis_War == 2 && State.Bt_EnemyParty1 == 135 && InPlayerParty(41))
        {
            await WaoAsync(322);
            await EventAsync("Gｹｲﾌﾞﾘｽ.ADV", 7);
        }
        if (State.Bt_PlayerParty1 == 0)
            return FigtBLabel.Announce;
        if (State.Prov_Cur > 77 && State.Bt_BackgroundCg == 900)
        {
            State.Bt_BackgroundCg = 908;
            if (State.Prov_Cur == 82 || State.Prov_Cur == 83)
                State.Bt_BackgroundCg = 918;
        }
        State.Bt_Kind = 2;
        return FigtBLabel.Fight;
    }

    /// <summary>
    /// <c>FIGT_B.ADV:244-310</c> (01 §2.3, §11.3): victory message, or the province is lost
    /// (<c>L_01165</c>; province 1 → game over); <c>死亡捕獲.ADV</c>; the Ex and Elena scenes
    /// (<c>RND(5)</c> [R2] after a win in provinces 31-49).
    /// </summary>
    internal async Task DefenceAftermathAsync()
    {
        State.Bt_BackgroundCg = 900;
        await FlowMapAsync(0);
        if (State.Bt_Result == 2)
        {
            await FlowSuspendMusicAsync();
            State.RetVal = 1;
            await FlowSoundAsync(0);
            await WaoAsync(113);
            await FlowTextAsync(FigtBFile, 257);
            await FlowWaitKeyKeepRetValAsync();
            await FlowSoundAsync(3);
            await FlowResumeMusicAsync();
            await WaoAsync(0);
        }
        State.Bt_Kind = 0;
        if (State.Bt_Result == 1)
            await ProvinceLostAsync();
        State.Prov_Cur = State.Bt_TargetProv;
        await DeathCaptureAsync();
        State.Prov_Cur = State.Bt_TargetProv;
        State.Bt_TargetProv = 0;
        if (State.Bt_PlayerParty1 == 14 && State.F_Ex_State == 4)
            await EventAsync("Gｴｸｽ.ADV", 6);
        if (State.Prov_Cur > 30 && State.Prov_Cur < 50 && State.Bt_Result == 2)
        {
            Rnd(5);
            State.Tmp0583 = State.Prov_Cur;
            State.Prov_Cur = 40;
            if (Prov.Owner != 1)
                State.RetVal = 4;
            State.Prov_Cur = State.Tmp0583;
            if (State.RetVal == 1 && State.F_Elena_State == 0)
            {
                await EventAsync("Gｴﾚﾅ.ADV", 1);
                return;
            }
            if (State.RetVal == 1 && State.F_Elena_State == 1)
            {
                await EventAsync("Gｴﾚﾅ.ADV", 2);
                return;
            }
            if (State.RetVal == 1 && State.F_Elena_State == 2)
            {
                await EventAsync("Gｴﾚﾅ.ADV", 3);
                return;
            }
            if (State.RetVal == 1 && State.F_Elena_State < 7)
            {
                await EventAsync("Gｴﾚﾅ.ADV", 7);
                return;
            }
        }
    }

    /// <summary>
    /// <c>L_01165</c>, <c>FIGT_B.ADV:830-883</c>: the defence failed — the attacking nation takes
    /// the province; losing province 1 ends the game (<c>EndingId 2</c>, <c>OVER.ADV</c>).
    /// </summary>
    internal async Task ProvinceLostAsync()
    {
        await FlowSuspendMusicAsync();
        State.RetVal = 2;
        await FlowSoundAsync(0);
        await WaoAsync(113);
        await FlowTextAsync(FigtBFile, 840);
        await FlowWaitKeyKeepRetValAsync();
        await FlowTextAsync(FigtBFile, 843);
        await FlowWaitKeyKeepRetValAsync();
        await FlowSoundAsync(3);
        await FlowResumeMusicAsync();
        await WaoAsync(0);
        Prov.Owner = State.Ev_PhaseNation;
        State.Tmp0021 = Prov.Owner + 819;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await FlowMapAsync(4);
        await FlowMapAsync(3);
        await FlowMapAsync(0);
        if (State.Prov_Cur == 1)
        {
            await WaoAsync(113);
            await FlowTextAsync(FigtBFile, 870);
            await FlowWaitKeyKeepRetValAsync();
            await FlowTextAsync(FigtBFile, 875);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
            JumpToGameOver(2);
        }
    }

    /// <summary>
    /// <c>L_00727</c>, <c>FIGT_B.ADV:311-648</c> (01 §2.3): a ninja whose weekly flag is 0 scouts
    /// the enemy army (its flag is set); the report shows each commander and the totals, then
    /// the menu returns. The capture branch <c>:673-713</c> is unreachable (Q-4) and not ported.
    /// </summary>
    internal async Task<FigtBLabel> DefenceNinjaScoutAsync()
    {
        await WaoAsync(112);
        await FlowTextAsync(FigtBFile, 314);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        if (State.F_Kanami_NinjaUsed != 0 && State.F_Freya_NinjaUsed != 0 && State.F_Kaoru_NinjaUsed != 0 &&
            State.F_Wichita_NinjaUsed != 0 && State.F_Saitou_NinjaUsed != 0)
        {
            await WaoAsync(121);
            await FlowTextAsync(FigtBFile, 322);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
            await WaoAsync(111);
            await FlowTextAsync(FigtBFile, 331);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
            return FigtBLabel.Menu;
        }
        // L_007bd (:340-380)
        State.RetVal = 0;   // IM release loop
        var options = new List<BattleMenuOption>();
        foreach (var ninja in Ninjas)
        {
            if (NinjaFlag(ninja.Code) == 0)
                options.Add(new BattleMenuOption((Sys3Int)ninja.Code, new ScriptRef(FigtBFile, ninja.MenuLine)));
        }
        options.Add(new BattleMenuOption(0, new ScriptRef(FigtBFile, 377)));
        var choice = await ChooseAsync(new BattleMenu(BattleMenuKind.NinjaScout, options));
        if (choice is null || choice == 0)
        {
            // L_00889 (:381-392)
            await WaoAsync(111);
            await FlowTextAsync(FigtBFile, 384);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
            await WaoAsync(121);
            await FlowTextAsync(FigtBFile, 389);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
            return FigtBLabel.Menu;
        }
        await NinjaScoutReportAsync(choice.Value);
        return FigtBLabel.Menu;
    }

    /// <summary>The weekly availability flag of ninja 1-5.</summary>
    private ref Sys3Int NinjaFlag(int code)
    {
        switch (code)
        {
            case 1: return ref State.F_Kanami_NinjaUsed;
            case 2: return ref State.F_Freya_NinjaUsed;
            case 3: return ref State.F_Kaoru_NinjaUsed;
            case 4: return ref State.F_Wichita_NinjaUsed;
            case 5: return ref State.F_Saitou_NinjaUsed;
            default: throw new ArgumentOutOfRangeException(nameof(code));
        }
    }

    /// <summary>
    /// <c>L_008c0</c>…<c>L_00a83</c> and <c>L_00ad4</c>, <c>FIGT_B.ADV:393-648</c>: the chosen
    /// ninja is called (<c>Bt_ScoutNinja</c>, its flag := 1) and reports; the report sums
    /// <c>Tmp0001</c> (troops) and counts <c>Tmp0002</c> (commanders) over <c>EParty</c>.
    /// </summary>
    internal async Task NinjaScoutReportAsync(Sys3Int code)
    {
        var ninja = Ninjas[code.Value - 1];
        State.Bt_ScoutNinja = code;
        NinjaFlag(ninja.Code) = 1;
        await WaoAsync(111);
        await FlowTextAsync(FigtBFile, ninja.CallLine);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await WaoAsync(322);
        State.CallArg = (Sys3Int)ninja.Portrait;
        await WaoAsync(321);
        if (ninja.ArriveLine != 0)
        {
            await WaoAsync(113);
            await FlowTextAsync(FigtBFile, ninja.ArriveLine);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
        }
        await WaoAsync(code == 1 ? (Sys3Int)122 : (Sys3Int)121);
        await FlowTextAsync(FigtBFile, ninja.ReplyLine);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        // L_00ad4 (:506-648)
        await WaoAsync(111);
        await FlowTextAsync(FigtBFile, 509);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await WaoAsync(121);
        await FlowTextAsync(FigtBFile, 518);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await WaoAsync(322);
        await WaoAsync(113);
        await FlowTextAsync(FigtBFile, 524);
        await FlowWaitKeyKeepRetValAsync();
        await FlowTextAsync(FigtBFile, 528);
        await FlowWaitKeyKeepRetValAsync();
        await FlowTextAsync(FigtBFile, 532);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        foreach (var n in Ninjas)
        {
            if (State.Bt_ScoutNinja == (Sys3Int)n.Code)
            {
                State.CallArg = (Sys3Int)n.Portrait;
                await WaoAsync(321);
                await WaoAsync(113);
                await FlowTextAsync(FigtBFile, n.ReturnLine);
                await FlowWaitKeyKeepRetValAsync();
            }
        }
        await FlowTextAsync(FigtBFile, 587);
        await FlowWaitKeyKeepRetValAsync();
        State.Tmp0001 = 0;
        State.Tmp0002 = 0;
        for (int i = 1; i <= 4; i++)
        {
            if (EParty(i) != 0)
            {
                State.Cmdr_Cur = EParty(i);
                await ScoutReportLineAsync();
            }
        }
        await FlowValueTextAsync(FigtBFile, 613, State.Tmp0002, State.Tmp0001);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await WaoAsync(322);
        await WaoAsync(111);
        await FlowTextAsync(FigtBFile, 629);
        await FlowWaitKeyKeepRetValAsync();
        await WaoAsync(0);
        await PortraitAsync(6, 321);
        await WaoAsync(121);
        if (State.Tmp0001 > 5000)
        {
            await FlowTextAsync(FigtBFile, 637);
            await FlowWaitKeyKeepRetValAsync();
        }
        if (State.Tmp0001 < 5001)
        {
            await FlowTextAsync(FigtBFile, 644);
            await FlowWaitKeyKeepRetValAsync();
        }
        await WaoAsync(0);
    }

    /// <summary><c>L_00dad</c>, <c>FIGT_B.ADV:649-672</c>: one commander line of the scout report.</summary>
    private async Task ScoutReportLineAsync()
    {
        State.Tmp0001 = State.Tmp0001 + Cmdr.Troops;
        State.Tmp0002 = State.Tmp0002 + 1;
        FlowCopyString(4, CmdrNameString(State.Cmdr_Cur));
        FlowCopyString(5, CmdrNameString(State.Cmdr_Cur) + 1);
        await View.ShowAsync(new ScoutReportEvent(State.Cmdr_Cur, Cmdr.Troops));
        await FlowWaitKeyKeepRetValAsync();
    }

    /// <summary>
    /// <c>L_00f1b</c>, <c>FIGT_B.ADV:714-829</c> (01 §2.3): the player abandons the city —
    /// <c>Bt_Result := 1</c>, no party, <c>Bt_TargetProv := 0</c>. Ex's last stand at the
    /// castle (week > 20) fights instead with unit 14 alone (Q-5: the target province is
    /// already 0). Otherwise the province goes to the attacker (province 1: game over); no
    /// <c>死亡捕獲</c>, no EXP.
    /// </summary>
    internal async Task<FigtBLabel> DefenceAbandonAsync()
    {
        State.Bt_Result = 1;
        for (int i = 1; i <= 4; i++)
            PParty(i) = 0;
        // QUIRK: Q-5 — cleared before Ex's last stand jumps to the fight.
        State.Bt_TargetProv = 0;
        if (State.Prov_Cur == 1 && State.F_Ex_State == 4 && State.Week > 20)
        {
            await EventAsync("Gｴｸｽ.ADV", 5);
            State.Unit_Cur = 14;
            Unit.Status = 2;
            State.Bt_PlayerParty1 = 14;
            State.Bt_PlayerParty2 = 0;
            State.Bt_PlayerParty3 = 0;
            State.Bt_PlayerParty4 = 0;
            State.Bt_TroopLimit = Prov.BattleArea;
            State.Bt_WallBonus = Prov.Defense;
            State.Bt_BackgroundCg = 902;
            return FigtBLabel.Fight;
        }
        int[] lines = [737, 742, 747, 754, 759, 766];
        for (int k = 0; k < lines.Length; k++)
        {
            await WaoAsync(k % 2 == 0 ? (Sys3Int)111 : (Sys3Int)121);
            await FlowTextAsync(FigtBFile, lines[k]);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
        }
        await WaoAsync(312);
        await WaoAsync(322);
        await WaoAsync(332);
        Prov.Owner = State.Ev_PhaseNation;
        State.Tmp0021 = Prov.Owner + 819;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await FlowMapAsync(4);
        await FlowMapAsync(3);
        await FlowMapAsync(0);
        await FlowMapAsync(10);
        State.CallArg = Prov.BgCg;
        await WaoAsync(331);
        await FlowSuspendMusicAsync();
        State.RetVal = 2;
        await FlowSoundAsync(0);
        await WaoAsync(113);
        await FlowTextAsync(FigtBFile, 795);
        await FlowWaitKeyKeepRetValAsync();
        await FlowSoundAsync(3);
        await FlowResumeMusicAsync();
        if (State.Prov_Cur == 1)
        {
            State.CallArg = Prov.BgCg;
            await WaoAsync(331);
            await WaoAsync(113);
            await FlowTextAsync(FigtBFile, 813);
            await FlowWaitKeyKeepRetValAsync();
            await FlowTextAsync(FigtBFile, 818);
            await FlowWaitKeyKeepRetValAsync();
            await WaoAsync(0);
            await WaoAsync(332);
            JumpToGameOver(2);
        }
        await WaoAsync(0);
        await WaoAsync(332);
        return FigtBLabel.Return;
    }

    /// <summary><c>!CallArg : cg! \L_01331 / \L_013a1</c>: a portrait (op 311 left, 321 right) with CG <c>cg</c>.</summary>
    private Task PortraitAsync(Sys3Int cg, Sys3Int op)
    {
        State.CallArg = cg;
        return WaoAsync(op);
    }
}
