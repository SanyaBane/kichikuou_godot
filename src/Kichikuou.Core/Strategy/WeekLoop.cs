using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>MAIN.ADV</c>, the week loop (part W2), ported one-to-one
/// (<c>docs/strategy/survey-main.md</c> §2.1, <c>docs/porting-conventions.md</c>): the top of the
/// week (<c>Week + 1</c>, the history shift), the thieves' week of the prologue
/// (<c>StoryStage = 0</c>, <c>MAIN.ADV:19-82</c>) and the Leazas week (<c>:83-1044</c>) — the
/// autosave, income, the refill of the units, the expenses, <c>COMMAND</c>, the scenes after it,
/// the nation phases in their fixed order (the <c>T*</c> files, the Camilla unit's inline AI) and
/// the end-of-week story checks. Every file the loop calls — the save screen (<c>ﾛｰﾄﾞｾｰﾌﾞ</c>,
/// <see cref="LoadSaveScreen"/> in the router), <c>COMMAND</c>, the nation turns, <c>FIGT_B</c>,
/// the scenes — goes through the context's hooks (<see cref="ScriptContext.CallFileAsync"/>).
/// </summary>
/// <remarks>
/// <c>MAIN</c> is entered by a jump (<c>&amp;#MAIN.ADV:</c>, <c>ｽﾀMENU.ADV:291</c>) and never returns:
/// <see cref="RunAsync"/> loops for ever, ending only by an exception (a jump to <c>OVER</c>, an
/// unported path). One pass, from <c>L_00020</c> to the next arrival there, is
/// <see cref="RunWeekAsync"/> — the span of a <c>week</c> golden capture (root <c>MAIN.ADV@20</c>).
/// The army names of the Kayblis, Camilla and Creator phases (<c>MS 10, "…"</c>) are read from the
/// player's files through <paramref name="scenario"/>; a week without those phases needs none.
/// </remarks>
public sealed class WeekLoop(ScriptContext script, IScenarioSource? scenario = null)
    : CommandFile(script, "MAIN.ADV", scenario)
{
    /// <summary>The autosave of the thieves' week (<c>MAIN.ADV:20</c>).</summary>
    private const int ThievesAutosave = 20;

    /// <summary>The autosave of the Leazas week (<c>MAIN.ADV:83</c>).</summary>
    private const int LeazasAutosave = 83;

    /// <summary><c>L_00020</c>: the week loop, for ever (<c>@L_00020</c> at <c>MAIN.ADV:81, 1045</c>).</summary>
    public async Task RunAsync()
    {
        while (true)
            await RunWeekAsync();
    }

    /// <summary><c>MAIN.ADV:1-1045</c>: one week, from <c>L_00020</c> up to the jump back to it.</summary>
    public async Task RunWeekAsync()
    {
        await WaoAsync(3);                                              // :2 \L_01a25: the face over the map
        await Script.MapCallAsync(0);                                   // :3-4
        State.Week = State.Week + 1;                                    // :5
        ShiftHistory();                                                 // :6-15
        if (State.BgmEnabled == 1)                                      // :16-18
            await Script.MusicAsync(Sys3Int.Zero);
        if (State.StoryStage == 0)
        {
            await AutosaveAsync(ThievesAutosave);                       // :19-82, then @L_00020
            await ThievesWeekAfterAutosaveAsync();
            return;
        }
        await AutosaveAsync(LeazasAutosave);                            // :83
        await LeazasWeekAfterAutosaveAsync();
    }

    /// <summary>
    /// After a load: the rest of the week whose autosave the slot holds (the call at
    /// <paramref name="autosave"/>, <c>MAIN.ADV:20</c> or <c>:83</c> — the original comes back there
    /// from <c>LD</c> through the save screen's "Load Complete", <see cref="LoadSaveScreen.ResumeAsync"/>,
    /// which the caller runs first), then the loop for ever.
    /// </summary>
    public async Task ResumeAsync(ScriptRef autosave)
    {
        if (autosave == Ref(ThievesAutosave))
            await ThievesWeekAfterAutosaveAsync();
        else if (autosave == Ref(LeazasAutosave))
            await LeazasWeekAfterAutosaveAsync();
        else
            throw new ArgumentException($"{autosave} is not an autosave of {File}.", nameof(autosave));
        await RunAsync();
    }

    /// <summary>
    /// <c>MAIN.ADV:6-15</c> — last week's history moves up one slot and this week's (slot 0)
    /// starts at 0. <c>Hist_Cur := 0</c> also takes back the cursor the harem left on a girl (CMD-14).
    /// </summary>
    private void ShiftHistory()
    {
        State.Hist_Cur = 0;
        State.Hist_PrevCur = 1;
        // QUIRK: STR-01 — NB onto the overlapping block copies cell by cell, forward: every
        // slot from Hist_PrevCur on becomes slot Hist_Cur's value.
        for (int i = 0; i < 299; i++)                                   // :8 NB Hist_PrevFunds, Hist_Funds, 299
            State.History[State.Hist_PrevCur.Value + i].Funds = State.History[State.Hist_Cur.Value + i].Funds;
        for (int i = 0; i < 299; i++)                                   // :9
            State.History[State.Hist_PrevCur.Value + i].SundryExp = State.History[State.Hist_Cur.Value + i].SundryExp;
        for (int i = 0; i < 299; i++)                                   // :10
            State.History[State.Hist_PrevCur.Value + i].MilitaryExp = State.History[State.Hist_Cur.Value + i].MilitaryExp;
        for (int i = 0; i < 299; i++)                                   // :11
            State.History[State.Hist_PrevCur.Value + i].Casualties = State.History[State.Hist_Cur.Value + i].Casualties;
        Hist.Funds = 0;                                                 // :12-15
        Hist.SundryExp = 0;
        Hist.MilitaryExp = 0;
        Hist.Casualties = 0;
    }

    // ------------------------------------------------------------------ the thieves' week

    /// <summary>
    /// <c>MAIN.ADV:21-80</c> — the thieves' week after its autosave: the date, the banner over the
    /// hideout, units 1-4 refreshed, <c>COMMAND</c>; a prologue ended by the lost duel
    /// (<c>StoryStage = 99</c>) becomes stage 1 with <c>Iﾊﾝﾗﾝ</c>; then "Then some time passed".
    /// </summary>
    private async Task ThievesWeekAfterAutosaveAsync()
    {
        SetDate();                                                      // :21 \L_01881
        await PlayAsync(1);                                             // :22-25
        State.Dgn_Cur = 1;                                              // :26-32 to the thieves' hideout
        State.MapTargetX = Dgn.MapX;
        State.MapTargetY = Dgn.MapY;
        await Script.MapCallAsync(1);
        await Script.MapCallAsync(10);
        await BannerAsync(35, 40);                                      // :33-49 "Rance's Thieves Phase"
        RefreshThieves();                                               // :50-64
        await WaoAsync(1);                                              // :66 \L_01a0b
        await WaoAsync(0);                                              // :67 \L_019fe
        Hist.Funds = State.Gold;                                        // :68
        await Script.CallFileAsync("COMMAND.ADV");                      // :69
        if (State.StoryStage == 99)                                     // :70-73 the prologue is over
        {
            State.StoryStage = 1;
            await Script.CallFileAsync("Iﾊﾝﾗﾝ.ADV");
        }
        await TimePassesAsync(77);                                      // :74-80
    }

    /// <summary>
    /// <c>MAIN.ADV:50-64</c> — units 1-4 with a status ≠ 0 stand up (status 1), heal fully and get
    /// 2 troops, up to their maximum; leaves <c>Unit_Cur</c> at 5.
    /// </summary>
    private void RefreshThieves()
    {
        State.Unit_Cur = 0;
        while (true)                                                    // L_00139
        {
            State.Unit_Cur = State.Unit_Cur + 1;
            if (State.Unit_Cur > 4)
                return;                                                 // @L_00192
            if (Unit.Status != 0)
            {
                Unit.Status = 1;
                Unit.HP = Unit.MaxHP;
                Unit.Troops = Unit.Troops + 2;
                if (Unit.Troops > Unit.MaxTroops)
                    Unit.Troops = Unit.MaxTroops;
            }
        }
    }

    // ------------------------------------------------------------------ the Leazas week

    /// <summary>
    /// <c>MAIN.ADV:84-1044</c> — the Leazas week after its autosave, to "Then some time passed":
    /// the player's phase, the nation phases, the end-of-week story checks.
    /// </summary>
    private async Task LeazasWeekAfterAutosaveAsync()
    {
        SetDate();                                                      // :84 \L_01881
        await PlayAsync(4);                                             // :85-88
        State.Prov_Cur = 1;                                             // :89-95 to Leazas Castle
        await PanToProvinceAsync();
        await BannerAsync(98, 103);                                     // :96-112 "Leazas Army Phase"
        Income();                                                       // :113-126
        CreamCounter();                                                 // :127-133
        RefillUnits();                                                  // :134-167
        await WaoAsync(1);                                              // :169 \L_01a0b
        State.Gold = State.Gold + State.Tmp0022;                        // :170-171
        Hist.Funds = State.Gold;
        await ValuesAsync(173, State.Tmp0022, State.Tmp0024);           // "Revenue: n00K GOLD / Replenished Troops: n"
        await WaoAsync(1);                                              // :184 \L_01a0b
        await ExpensesAsync();                                          // :185-211
        await WaoAsync(0);                                              // :212 \L_019fe
        await Script.CallFileAsync("COMMAND.ADV");                      // :213
        await Script.MapCallAsync(0);                                   // :214-215
        await ScenesAfterCommandAsync();                                // :216-228
        await MarsPhaseAsync();                                         // :229-259
        await ALChurchPhaseAsync();                                     // :260-297
        await RebelPhaseAsync();                                        // :298-347
        await JapanPhaseAsync();                                        // :348-400
        await HelmanPhaseAsync();                                       // :401-453
        await PattonPhaseAsync();                                       // :454-506
        await ZethPhaseAsync();                                         // :507-593
        await KayblisPhaseAsync();                                      // :594-630
        await CamillaPhaseAsync();                                      // :631-815
        await CreatorPhaseAsync();                                      // :816-847
        await LaVaswaldPhaseAsync();                                    // :848-883
        if (State.BgmEnabled == 1)                                      // :884-886
            await Script.MusicAsync(Sys3Int.Zero);
        await EndOfWeekAsync();                                         // :887-1035
        await Script.MapCallAsync(0);                                   // :1036-1037
        await TimePassesAsync(1041);                                    // :1038-1044
    }

    /// <summary>
    /// <c>MAIN.ADV:113-126</c> — the income: the economy of every province Leazas owns, summed into
    /// <c>Tmp0022</c> (their count in <c>Tmp0021</c>, never read); leaves <c>Prov_Cur</c> at
    /// <c>Prov_Count</c> and <c>Tmp0023/0024</c> at 0.
    /// </summary>
    private void Income()
    {
        State.Prov_Cur = 1;
        State.Tmp0021 = 0;
        State.Tmp0022 = 0;
        State.Tmp0023 = 0;
        State.Tmp0024 = 0;
        while (true)                                                    // L_00298
        {
            if (Prov.Owner == 1)
            {
                State.Tmp0022 = State.Tmp0022 + Prov.Economy;
                State.Tmp0021 = State.Tmp0021 + 1;
            }
            if (State.Prov_Cur < State.Prov_Count)
            {
                State.Prov_Cur = State.Prov_Cur + 1;
                continue;
            }
            return;
        }
    }

    /// <summary><c>MAIN.ADV:127-133</c> — Cream's counter (unit 28) runs on while she is at home and stops at 11 while she is out.</summary>
    private void CreamCounter()
    {
        State.Unit_Cur = 28;
        if (Unit.Status == 1 && State.F_Cream_State > 10)
            State.F_Cream_State = State.F_Cream_State + 1;
        if (Unit.Status == 2 && State.F_Cream_State > 10)
            State.F_Cream_State = 11;
    }

    /// <summary>
    /// <c>MAIN.ADV:134-167</c> — units 1-48 with a status ≠ 0 stand up (status 1), get 2 HP up to
    /// their maximum and <c>RND(Recovery × 2) + Recovery</c> troops up to theirs, summed into
    /// <c>Tmp0024</c>; the holder of item 48 loses troops instead and the holder of item 50 gains
    /// some first. Leaves <c>Unit_Cur</c> at 49 and <c>Tmp0023</c> at the last refilled unit's troops before.
    /// </summary>
    private void RefillUnits()
    {
        State.Unit_Cur = 0;
        while (true)                                                    // L_00310
        {
            State.Unit_Cur = State.Unit_Cur + 1;
            if (State.Unit_Cur > 48)
                return;                                                 // @L_003f7
            if (Unit.Status == 0)
                continue;
            if (Unit.Item == 48)
            {
                // QUIRK: STR-06 — the holder of item 48 loses troops and skips the rest.
                Unit.Status = 1;
                Script.Rnd(Unit.Troops / 20);
                Unit.Troops = Unit.Troops - State.RetVal;
                continue;
            }
            if (Unit.Item == 50)
            {
                Script.Rnd(Unit.MaxTroops / 10);
                Unit.Troops = Unit.Troops + State.RetVal;
            }
            Unit.Status = 1;
            Unit.HP = Unit.HP + 2;
            if (Unit.HP > Unit.MaxHP)
                Unit.HP = Unit.MaxHP;
            if (Unit.Troops == Unit.MaxTroops)
                continue;
            State.Tmp0023 = Unit.Troops;
            Script.Rnd(Unit.Recovery * 2);
            Unit.Troops = Unit.Troops + State.RetVal + Unit.Recovery;
            if (Unit.Troops > Unit.MaxTroops)
                Unit.Troops = Unit.MaxTroops;
            State.Tmp0024 = State.Tmp0024 + (Unit.Troops - State.Tmp0023);
        }
    }

    /// <summary>
    /// <c>MAIN.ADV:185-211</c> — the weekly donations: M Land (−20) while the park is funded, the
    /// AL Church (−65) until its stage 19, Galtia's food (−45) while unit 41 lives; each to the week's
    /// sundry expenses. Leaves <c>Unit_Cur</c> at 41.
    /// </summary>
    private async Task ExpensesAsync()
    {
        State.Unit_Cur = 41;
        if (!(State.F_MLand_Funded == 1 || State.N_ALChurch_Stage < 19 || Unit.Status != 0))
            return;
        await TextAsync(188);                                           // "Special Expenses (Donations)"
        if (State.F_MLand_Funded == 1)
        {
            State.Gold = State.Gold - 20;
            Hist.SundryExp = Hist.SundryExp + 20;
            await TextAsync(194);                                       // "M Land: -2000K GOLD"
        }
        if (State.N_ALChurch_Stage < 19)
        {
            State.Gold = State.Gold - 65;
            Hist.SundryExp = Hist.SundryExp + 65;
            await TextAsync(201);                                       // "AL Church: -6500K GOLD"
        }
        if (Unit.Status != 0)
        {
            State.Gold = State.Gold - 45;
            Hist.SundryExp = Hist.SundryExp + 45;
            await TextAsync(208);                                       // "Galtia's Food: -4500K GOLD"
        }
        await WaoAsync(1);                                              // :210 \L_01a0b
    }

    /// <summary><c>MAIN.ADV:216-228</c> — the scenes that wait for the end of the player's phase.</summary>
    private async Task ScenesAfterCommandAsync()
    {
        if (State.F_Fuuka_State == 9)
            await Script.EventAsync("Gﾌｳｶ.ADV", 4);
        if (State.F_Asuka_State == 1)
            await Script.EventAsync("Gｶﾄﾞﾐｭｳﾑ.ADV", 1);
        State.Unit_Cur = 28;
        if (Unit.Status == 1 && State.F_Cream_State > 19)
            await Script.EventAsync("Gｸﾘｰﾑ.ADV", 3);
    }

    // ------------------------------------------------------------------ the nation phases

    /// <summary><c>MAIN.ADV:229-259</c> — Mars the Great's phase, a scene.</summary>
    private async Task MarsPhaseAsync()
    {
        if (State.N_Mars_Score != 1)
            return;
        SetDate();                                                      // \L_01881
        await PlayAsync(1);
        await Script.MapCallAsync(0);
        State.Ev_PhaseNation = 3;
        await BannerAsync(240, 245);                                    // "Mars the Great Phase"
        await WaoAsync(1);                                              // \L_01a0b
        await WaoAsync(0);                                              // \L_019fe
        await Script.EventAsync("Gｶｾｲ.ADV", 2);
    }

    /// <summary><c>MAIN.ADV:260-297</c> — the AL Church's phase, a scene.</summary>
    private async Task ALChurchPhaseAsync()
    {
        if (State.N_ALChurch_Stage != 21 && State.N_ALChurch_Stage != 100)
            return;
        SetDate();                                                      // \L_01881
        State.Ev_PhaseNation = 11;
        await PlayAsync(20);
        if (State.N_ALChurch_Stage == 21)
        {
            State.Prov_Cur = 77;
            await PanToProvinceAsync();
        }
        await BannerAsync(278, 283);                                    // "AL Church Phase"
        await WaoAsync(1);
        await WaoAsync(0);
        await Script.EventAsync("GAL.ADV", 5);
    }

    /// <summary>
    /// <c>MAIN.ADV:298-347</c> — the Leazas rebels' phase (stage 1): commanders 1-7 alive stand
    /// up, heal fully and get three times their recovery in troops; then <c>TCﾘｰｻﾞｽ</c>.
    /// </summary>
    private async Task RebelPhaseAsync()
    {
        if (State.StoryStage != 1)
            return;
        SetDate();
        await PlayAsync(25);
        State.Prov_Cur = 6;
        await PanToProvinceAsync();
        await BannerAsync(313, 318);                                    // "Leazas Rebel Army Phase"
        RefreshCommanders(0, 8, fullHealth: true);                      // L_0076c
        await WaoAsync(1);
        await WaoAsync(0);
        await Script.CallFileAsync("TCﾘｰｻﾞｽ.ADV");
    }

    /// <summary><c>MAIN.ADV:348-400</c> — JAPAN's phase: commanders 72-82 refreshed, then <c>TJAPAN</c>.</summary>
    private async Task JapanPhaseAsync()
    {
        if (State.F_Japan_State != 1)
            return;
        SetDate();
        await PlayAsync(13);
        State.Prov_Cur = 56;
        await PanToProvinceAsync();
        await BannerAsync(363, 368);                                    // "JAPAN Phase"
        RefreshCommanders(71, 83);                                      // L_0087e
        await WaoAsync(1);
        await WaoAsync(0);
        await Script.CallFileAsync("TJAPAN.ADV");
    }

    /// <summary><c>MAIN.ADV:401-453</c> — Helman's phase: commanders 22-57 refreshed, then <c>Tﾍﾙﾏﾝ</c>.</summary>
    private async Task HelmanPhaseAsync()
    {
        if (State.F_Helman_War != 1)
            return;
        SetDate();
        await PlayAsync(7);
        State.Prov_Cur = 45;
        await PanToProvinceAsync();
        await BannerAsync(416, 421);                                    // "Helman Phase"
        RefreshCommanders(21, 58);                                      // L_009a6
        await WaoAsync(1);
        await WaoAsync(0);
        await Script.CallFileAsync("Tﾍﾙﾏﾝ.ADV");
    }

    /// <summary><c>MAIN.ADV:454-506</c> — the Patton army's phase: commanders 54-57 refreshed, then <c>Tﾊﾟｯﾄﾝ</c>.</summary>
    private async Task PattonPhaseAsync()
    {
        if (State.F_PattonArmy_State == 0)
            return;
        SetDate();
        await PlayAsync(7);
        State.Prov_Cur = 46;
        await PanToProvinceAsync();
        await BannerAsync(469, 474);                                    // "Patton Army Phase"
        // QUIRK: STR-07 — commanders 54-57 were refreshed in the Helman phase too if it ran.
        RefreshCommanders(53, 58);                                      // L_00ad0
        await WaoAsync(1);
        await WaoAsync(0);
        await Script.CallFileAsync("Tﾊﾟｯﾄﾝ.ADV");
    }

    /// <summary>
    /// <c>MAIN.ADV:507-593</c> — Zeth's phase: the map centres on the first of provinces 69, 68, 66,
    /// 70 (else 67) Zeth still holds; commanders 101-128 and 165-180 refreshed, then <c>Tｾﾞｽ</c>.
    /// </summary>
    private async Task ZethPhaseAsync()
    {
        if (State.F_Zeth_War != 1)
            return;
        SetDate();
        // QUIRK: STR-24 — CurrentBgm is set whether the music is on or not.
        State.CurrentBgm = 10;
        if (State.F_Zeth_State == 9)
            State.CurrentBgm = 12;
        if (State.BgmEnabled == 1)
            await Script.MusicAsync(State.CurrentBgm);
        State.Prov_Cur = 69;
        if (Prov.Owner != 7)
        {
            State.Prov_Cur = 68;
            if (Prov.Owner != 7)
            {
                State.Prov_Cur = 66;
                if (Prov.Owner != 7)
                {
                    State.Prov_Cur = 70;
                    if (Prov.Owner != 7)
                        State.Prov_Cur = 67;
                }
            }
        }
        await PanToProvinceAsync();
        await BannerAsync(537, 542);                                    // "Zeth Phase"
        RefreshCommanders(100, 129);                                    // L_00c62
        RefreshCommanders(164, 181);                                    // L_00cdd
        await WaoAsync(1);
        await WaoAsync(0);
        await Script.CallFileAsync("Tｾﾞｽ.ADV");
    }

    /// <summary>
    /// <c>MAIN.ADV:594-630</c> — the Kayblis empire's phase (stage 3, outside the Kayblis war):
    /// no refresh here (<c>Tｹｲﾌﾞﾘｽ</c> does its own); the army is named "Kayblis Unit".
    /// </summary>
    private async Task KayblisPhaseAsync()
    {
        if (!(State.StoryStage == 3 && (State.F_Kayblis_War == 0 || State.F_Kayblis_War == 2)))
            return;
        SetDate();
        await PlayAsync(16);
        State.Prov_Cur = 79;
        await PanToProvinceAsync();
        await BannerAsync(609, 614);                                    // "Kayblis Empire Phase"
        await WaoAsync(1);
        await WaoAsync(0);
        State.Ev_PhaseNation = 8;
        SetLiteral(628);                                                // MS 10, "Kayblis Unit"
        await Script.CallFileAsync("Tｹｲﾌﾞﾘｽ.ADV");
    }

    /// <summary>
    /// <c>MAIN.ADV:631-815</c> — the Camilla unit's phase (<c>N_Camilla_Stage</c> 2-99 and beyond 100),
    /// its AI written inline: the Sieg and Seizel scenes, then commanders 93, 90, 92 and 91 in turn
    /// each roll a province among 1-16 and attack it through <c>FIGT_B</c> if Leazas holds it and
    /// their roll allows, one battle a week as a rule (<c>Ev_WeeklyEventDone</c>); then Camilla's
    /// scenes.
    /// </summary>
    private async Task CamillaPhaseAsync()
    {
        if (!(State.N_Camilla_Stage > 1 && State.N_Camilla_Stage != 100))
            return;
        SetDate();
        await PlayAsync(16);
        State.MapTargetX = 1377;
        State.MapTargetY = 343;
        await Script.MapCallAsync(1);
        await Script.MapCallAsync(10);
        await BannerAsync(645, 650);                                    // "Camilla Unit Phase"
        await WaoAsync(1);
        await WaoAsync(0);
        State.Ev_PhaseNation = 8;
        SetLiteral(664);                                                // MS 10, "Camilla Unit"
        State.Ev_WeeklyEventDone = 0;

        State.Cmdr_Cur = 92;                                            // :666-681
        if (Cmdr.Status == 0 && State.F_Sieg_State == 0)
        {
            await Script.EventAsync("Gｼﾞｰｸ.ADV", 1);
            State.Ev_WeeklyEventDone = 1;
        }
        if (State.F_Seizel_State == 2)
        {
            await Script.EventAsync("Gｻｲｾﾞﾙ.ADV", 2);
            State.Ev_WeeklyEventDone = 1;
        }
        if (State.F_Seizel_State == 1)
        {
            await Script.EventAsync("Gｻｲｾﾞﾙ.ADV", 1);
            State.Ev_WeeklyEventDone = 1;
        }

        State.Cmdr_Cur = 93;                                            // :682-720
        if (Cmdr.Status != 0)
        {
            State.Bt_EnemyRetreatMode = 1;
            if (State.N_Miki_Stage == 1)
            {
                // QUIRK: STR-04 — commander 93 is refreshed only while N_Miki_Stage = 1.
                RefreshCamillaCommander();                              // \L_01831
                State.Bt_EnemyRetreatMode = 5;
            }
            Script.Rnd(16);
            State.Prov_Cur = State.RetVal;
            Script.Rnd(2);
            if (State.F_Cell_Arc == 3)
                State.RetVal = 1;
            if (State.F_Cell_Arc == 1 && State.F_Sieg_State == 1)
            {
                State.Ev_Entry = 2;
                State.Ev_WeeklyEventDone = 1;
                await Script.CallFileAsync("Gｼﾞｰｸ.ADV");
                State.RetVal = 5;
            }
            if (State.RetVal == 2 && State.N_Miki_Stage == 1)
            {
                State.Ev_Entry = 5;
                State.Ev_WeeklyEventDone = 1;
                await Script.CallFileAsync("Gｼﾞｰｸ.ADV");
                State.RetVal = 2;
            }
            if (Prov.Owner == 1 && State.RetVal != 2 && State.F_Cell_Arc != 2)
            {
                await AttackAsync(93);
                State.Cmdr_Cur = 93;
                if (Cmdr.Status != 0 && State.F_Sieg_State == 2)
                    await Script.EventAsync("Gｼﾞｰｸ.ADV", 3);
            }
        }

        State.Cmdr_Cur = 90;                                            // :721-741
        if (Cmdr.Status != 0)
        {
            RefreshCamillaCommander();                                  // \L_01831
            State.Bt_EnemyRetreatMode = 5;
            Script.Rnd(16);
            State.Prov_Cur = State.RetVal;
            Script.Rnd(3);
            if (State.N_Camilla_Stage == 4)
            {
                State.RetVal = 1;
                State.Prov_Cur = 1;
                Cmdr.HP = Cmdr.MaxHP;
            }
            if (Prov.Owner == 1 && State.RetVal != 2 && Cmdr.HP == Cmdr.MaxHP)
                await AttackAsync(90);
        }
        if (State.N_Camilla_Stage == 4)                                 // :742-746
        {
            State.Ev_Entry = 1;
            State.Ev_WeeklyEventDone = 1;
            await Script.CallFileAsync("Gｻﾃﾗ.ADV");
        }

        State.Cmdr_Cur = 92;                                            // :747-777
        if (Cmdr.Status != 0)
        {
            RefreshCamillaCommander();                                  // \L_01831
            State.Bt_EnemyRetreatMode = 5;
            Script.Rnd(16);
            State.Prov_Cur = State.RetVal;
            Script.Rnd(3);
            // QUIRK: STR-03 — while Rei has not joined, 92 may attack even after another battle this week.
            if (State.N_Rei_Stage == 0)
                State.Ev_WeeklyEventDone = 0;
            if (State.N_Camilla_Stage == 5)
            {
                State.RetVal = 1;
                State.Ev_WeeklyEventDone = 0;
            }
            if (Prov.Owner == 1 && State.RetVal != 2 && State.Ev_WeeklyEventDone == 0)
            {
                await AttackAsync(92);
                State.Cmdr_Cur = 92;
                if (Cmdr.Status != 0 && State.N_Rei_Stage == 0 && State.N_Camilla_Stage > 5)
                    await Script.EventAsync("Gﾚｲ.ADV", 1);
                if (Cmdr.Status == 0)
                    State.N_Rei_Stage = 0;
            }
        }

        State.Cmdr_Cur = 91;                                            // :778-797
        if (Cmdr.Status != 0)
        {
            RefreshCamillaCommander();                                  // \L_01831
            State.Bt_EnemyRetreatMode = 5;
            Script.Rnd(16);
            State.Prov_Cur = State.RetVal;
            Script.Rnd(2);
            if (State.N_Camilla_Stage == 5)
            {
                State.RetVal = 1;
                State.Ev_WeeklyEventDone = 0;
            }
            if (Prov.Owner == 1 && State.RetVal == 1 && State.Ev_WeeklyEventDone == 0)
                await AttackAsync(91);
        }

        // QUIRK: STR-02 — RetVal is 91's roll only if 91 lives; else whatever came last.
        if (State.RetVal == 2 && State.F_Camilla_State == 1)            // :798-807
        {
            State.Ev_Entry = 2;
            State.Ev_WeeklyEventDone = 1;
            await Script.CallFileAsync("Gｶﾐｰﾗ.ADV");
        }
        if (State.RetVal == 2 && State.F_Camilla_State == 0)
        {
            State.Ev_Entry = 1;
            State.Ev_WeeklyEventDone = 1;
            await Script.CallFileAsync("Gｶﾐｰﾗ.ADV");
        }
        if (State.N_Camilla_Stage == 5)                                 // :808-814
            State.N_Camilla_Stage = 6;
        if (State.Ev_WeeklyEventDone == 0)
            await Script.EventAsync("Gｶﾐｰﾗ.ADV", 8);
    }

    /// <summary>One Camilla-unit commander alone attacks <c>Prov_Cur</c> (<c>FIGT_B</c>), counting as the week's event.</summary>
    private async Task AttackAsync(int commander)
    {
        State.Bt_EnemyParty1 = (Sys3Int)commander;
        State.Bt_EnemyParty2 = 0;
        State.Bt_EnemyParty3 = 0;
        State.Bt_EnemyParty4 = 0;
        State.Ev_WeeklyEventDone = 1;
        await Script.CallFileAsync("FIGT_B.ADV");
    }

    /// <summary>
    /// <c>MAIN.ADV:816-847</c> — the Creator's phase (stage 4), named "Creator", then <c>Tﾙﾄﾞﾗ</c>.
    /// </summary>
    private async Task CreatorPhaseAsync()
    {
        if (State.StoryStage != 4)
            return;
        SetDate();
        await PlayAsync(19);
        await Script.MapCallAsync(0);
        await BannerAsync(826, 831);                                    // "Creator Phase"
        await WaoAsync(1);
        await WaoAsync(0);
        // QUIRK: STR-13 — 11 here; Tﾙﾄﾞﾗ.ADV:6 overwrites it with 10.
        State.Ev_PhaseNation = 11;
        SetLiteral(845);                                                // MS 10, "Creator"
        await Script.CallFileAsync("Tﾙﾄﾞﾗ.ADV");
    }

    /// <summary><c>MAIN.ADV:848-883</c> — La Vaswald's phase while commander 98 lives, a scene; leaves <c>Cmdr_Cur</c> at 98.</summary>
    private async Task LaVaswaldPhaseAsync()
    {
        State.Cmdr_Cur = 98;
        if (Cmdr.Status == 0)
            return;
        SetDate();
        await PlayAsync(18);
        State.Prov_Cur = State.LaVaswald_Prov;
        await PanToProvinceAsync();
        await BannerAsync(864, 869);                                    // "La Vaswald Phase"
        await WaoAsync(1);
        await WaoAsync(0);
        await Script.EventAsync("Gｻｲｾﾞﾙ.ADV", 12);
    }

    // ------------------------------------------------------------------ the end of the week

    /// <summary>
    /// <c>MAIN.ADV:887-1035</c> — the end-of-week story checks: the demons' first attack on Leazas
    /// (stage 2), the start of the Camilla unit, Sill's kidnapping by the weeks, Magic's scene, and
    /// the demon and Kayblis wars on the majin timer.
    /// </summary>
    private async Task EndOfWeekAsync()
    {
        if (State.StoryStage == 2 && State.N_Camilla_Stage == 1)        // :887-892
        {
            // QUIRK: STR-24 — the music is played without testing BgmEnabled.
            await Script.MusicAsync(16);
            await Script.EventAsync("Iﾏｼﾞﾝ.ADV", 1);
            await Script.MusicAsync(Sys3Int.Zero);
        }
        if (State.StoryStage == 2 && State.N_Camilla_Stage == 0)        // :893-895
            State.N_Camilla_Stage = 1;
        if (State.F_Helman_War != 2)                                    // :896-925
        {
            // Each scene sets the next stage first, so later lines may follow it the same week.
            if (State.F_SillKidnap_Stage == 0 && State.Week > 10)
                await Script.EventAsync("Gｼｨﾙ.ADV", 21);
            if (State.F_SillKidnap_Stage == 1 && State.Week > 20)
                await Script.EventAsync("Gｼｨﾙ.ADV", 22);
            if (State.F_SillKidnap_Stage == 2 && State.Week > 30)
                await Script.EventAsync("Gｼｨﾙ.ADV", 23);
            if (State.F_SillKidnap_Stage == 3 && State.Week > 35)
                await Script.EventAsync("Gｼｨﾙ.ADV", 24);
            if (State.F_SillKidnap_Stage == 4 && State.Week > 40)
                await Script.EventAsync("Gｼｨﾙ.ADV", 25);
            if (State.F_SillKidnap_Stage == 5 && State.Week > 45)
                await Script.EventAsync("Gｼｨﾙ.ADV", 26);
            if (State.F_SillKidnap_Stage == 6 && State.Week > 50)
                await Script.EventAsync("Gｼｨﾙ.ADV", 27);
        }
        if ((State.F_Magic_State == 2 || State.F_Magic_State == 3) && State.N_Xacalite_KillsLeft != 0)  // :926-931
        {
            State.Ev_Entry = 5;
            // QUIRK: STR-24 — the music is played without testing BgmEnabled.
            await Script.MusicAsync(18);
            await Script.CallFileAsync("Gﾏｼﾞｯｸ.ADV");
            await Script.MusicAsync(Sys3Int.Zero);
        }
        State.N_Majin_EventTimer = State.N_Majin_EventTimer - 1;        // :932-935
        if (State.F_Zeth_War == 2 && State.F_Helman_War == 2 && State.F_DemonWar_Stage < 10)
            State.N_Majin_EventTimer = 0;
        if (State.F_Kayblis_War == 0 && State.N_Camilla_Stage > 5 && State.N_Majin_EventTimer == 0)  // :936-985
        {
            // QUIRK: STR-08 — tested from the last stage down, each on the state the scenes before it left.
            foreach (var (stage, entry) in DemonWar)
                if (State.F_DemonWar_Stage == (Sys3Int)stage)
                    await Script.EventAsync("Iﾏｼﾞﾝ.ADV", (Sys3Int)entry);
        }
        if (State.F_Kayblis_War == 1 && State.N_Majin_EventTimer == 0)  // :986-1035
        {
            // QUIRK: STR-08 — Satella or Meglass in the army sends the chain back to its start.
            State.Unit_Cur = 29;
            if (Unit.Status != 0)
                State.F_DemonWar_Stage = 0;
            State.Unit_Cur = 30;
            if (Unit.Status != 0)
                State.F_DemonWar_Stage = 0;
            foreach (var (stage, entry) in KayblisWar)
                if (State.F_DemonWar_Stage == (Sys3Int)stage)
                    await Script.EventAsync("Iﾏｼﾞﾝ2.ADV", (Sys3Int)entry);
        }
    }

    /// <summary>The demon war's scenes (<c>Iﾏｼﾞﾝ</c> entries) by <c>F_DemonWar_Stage</c>, in the order tested (<c>MAIN.ADV:937-984</c>).</summary>
    private static readonly (int Stage, int Entry)[] DemonWar =
    [
        (11, 51), (10, 50), (9, 20), (8, 19), (7, 18), (6, 17), (5, 16), (4, 15), (3, 14), (2, 13), (1, 12), (0, 11),
    ];

    /// <summary>The Kayblis war's scenes (<c>Iﾏｼﾞﾝ2</c> entries) by <c>F_DemonWar_Stage</c>, in the order tested (<c>MAIN.ADV:995-1034</c>).</summary>
    private static readonly (int Stage, int Entry)[] KayblisWar =
    [
        (9, 30), (8, 29), (7, 28), (6, 27), (5, 26), (4, 25), (3, 24), (2, 23), (1, 22), (0, 21),
    ];

    // ------------------------------------------------------------------ shared steps

    /// <summary>
    /// The refresh of a nation's commanders before its phase: from <paramref name="before"/> + 1 up
    /// to <paramref name="end"/> − 1 (the script's <c>Cmdr_Cur : before</c> … <c>{Cmdr_Cur = end: exit}</c>),
    /// each one alive stands up (status 1), gets 2 HP (or all, for the rebels) and three times its
    /// recovery in troops, both up to their maximum. Leaves <c>Cmdr_Cur</c> at <paramref name="end"/>.
    /// </summary>
    private void RefreshCommanders(int before, int end, bool fullHealth = false)
    {
        State.Cmdr_Cur = (Sys3Int)before;
        while (true)
        {
            State.Cmdr_Cur = State.Cmdr_Cur + 1;
            if (State.Cmdr_Cur == (Sys3Int)end)
                return;
            if (Cmdr.Status == 0)
                continue;
            Cmdr.Status = 1;
            if (fullHealth)
                Cmdr.HP = Cmdr.MaxHP;
            else
            {
                Cmdr.HP = Cmdr.HP + 2;
                if (Cmdr.HP > Cmdr.MaxHP)
                    Cmdr.HP = Cmdr.MaxHP;
            }
            Cmdr.Troops = Cmdr.Troops + Cmdr.Recovery * 3;
            if (Cmdr.Troops > Cmdr.MaxTroops)
                Cmdr.Troops = Cmdr.MaxTroops;
        }
    }

    /// <summary><c>L_01831</c> (<c>MAIN.ADV:1046-1055</c>) — a Camilla-unit commander gets 2 HP and three times its recovery in troops, both capped.</summary>
    private void RefreshCamillaCommander()
    {
        Cmdr.HP = Cmdr.HP + 2;
        if (Cmdr.HP > Cmdr.MaxHP)
            Cmdr.HP = Cmdr.MaxHP;
        Cmdr.Troops = Cmdr.Troops + Cmdr.Recovery * 3;
        if (Cmdr.Troops > Cmdr.MaxTroops)
            Cmdr.Troops = Cmdr.MaxTroops;
    }

    /// <summary><c>{BgmEnabled = 1: !CurrentBgm : track! SS CurrentBgm:}</c> — a phase's music, when the music is on.</summary>
    private async Task PlayAsync(int track)
    {
        if (State.BgmEnabled != 1)
            return;
        State.CurrentBgm = (Sys3Int)track;
        await Script.MusicAsync(State.CurrentBgm);
    }

    /// <summary><c>MapTarget := Prov_MapX/Y</c>, <c>M</c> ops 1 and 10 — the map pans to <c>Prov_Cur</c>.</summary>
    private async Task PanToProvinceAsync()
    {
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(1);
        await Script.MapCallAsync(10);
    }

    /// <summary>
    /// A phase's banner: <c>\L_0191e</c> (the window), its title run at <paramref name="title"/>,
    /// <c>ZM 20</c>, "LP y, Month m, Week w" at <paramref name="date"/> from <c>Tmp0001-0003</c>, <c>ZM 24</c>.
    /// </summary>
    private async Task BannerAsync(int title, int date)
    {
        await WaoAsync(113);                                            // \L_0191e
        await TextAsync(title);
        await ZoomAsync(20);
        await ValuesAsync(date, State.Tmp0001, State.Tmp0002, State.Tmp0003);
        await ZoomAsync(24);
    }

    /// <summary>"Then some time passed." (its run at <paramref name="line"/>) between fades — the end of every week.</summary>
    private async Task TimePassesAsync(int line)
    {
        await EngineAsync("PF1", State.FadeSpeed);
        await WaoAsync(113);                                            // \L_0191e
        await TextAsync(line);
        await EngineAsync("PF0", State.FadeSpeed);
        await WaoAsync(1);                                              // \L_01a0b
        await WaoAsync(0);                                              // \L_019fe
    }

    /// <summary>
    /// <c>L_01875</c> (<c>MAIN.ADV:1056-1059</c>), called at <paramref name="line"/> — the
    /// autosave: <c>ﾛｰﾄﾞｾｰﾌﾞ</c> with <c>RetVal 3</c> (slot 1, then page 4 to every slot). The
    /// slot remembers the call, so a load goes on right after it.
    /// </summary>
    private async Task AutosaveAsync(int line)
    {
        State.RetVal = 3;
        Script.SaveResume = Ref(line);
        try
        {
            await Script.CallFileAsync("ﾛｰﾄﾞｾｰﾌﾞ.ADV");
        }
        finally
        {
            Script.SaveResume = null;
        }
    }

    /// <summary>
    /// <c>L_01881</c> (<c>MAIN.ADV:1060-1076</c>) — <c>DateYear/Month/Week</c> from <c>Week</c>
    /// (48 weeks a year from year 3, 4 weeks a month), through <c>Tmp0001-0005</c>.
    /// </summary>
    private void SetDate()
    {
        State.Tmp0005 = State.Week;
        State.Tmp0004 = (State.Tmp0005 - 1) / 48;
        State.Tmp0001 = (Sys3Int)3 + State.Tmp0004;
        if (State.Tmp0004 != 0)
            State.Tmp0005 = State.Tmp0005 - State.Tmp0004 * 48;
        State.Tmp0004 = (State.Tmp0005 - 1) / 4;
        State.Tmp0002 = State.Tmp0004 + 1;
        if (State.Tmp0004 != 0)
            State.Tmp0005 = State.Tmp0005 - State.Tmp0004 * 4;
        State.Tmp0003 = State.Tmp0005;
        State.DateYear = State.Tmp0001;
        State.DateMonth = State.Tmp0002;
        State.DateWeek = State.Tmp0003;
    }

    /// <summary><c>ZM size:</c> — the message font size.</summary>
    private Task ZoomAsync(int size) => EngineAsync("ZM", (Sys3Int)size);
}
