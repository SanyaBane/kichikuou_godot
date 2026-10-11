using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>COM.ADV</c>, the weekly upkeep and story triggers (part W1), ported one-to-one
/// (<c>docs/strategy/survey-main.md</c> §2.3, <c>docs/porting-conventions.md</c>). <c>COMMAND.ADV:3</c>
/// calls it once a week, at the start of the player phase. It draws nothing: it advances the
/// weekly counters, then plays the story scenes whose conditions hold — every tier-1 trigger that
/// applies, then, if none did, the first tier-2 trigger that applies. Scenes are called through
/// <see cref="ScriptContext.EventAsync"/> (the scene runner); their effects on the state are
/// theirs, and later conditions read the state as the scenes left it.
/// </summary>
public sealed partial class WeeklyEvents(ScriptContext script)
{
    /// <summary>The shared services the file runs on.</summary>
    public ScriptContext Script { get; } = script;

    private GameState State => Script.State;

    /// <summary>The player-unit record selected by <c>Unit_Cur</c>, re-read on every access.</summary>
    private UnitRecord Unit => State.Units[State.Unit_Cur];

    /// <summary>The enemy-commander record selected by <c>Cmdr_Cur</c>, re-read on every access.</summary>
    private CmdrRecord Cmdr => State.Cmdrs[State.Cmdr_Cur];

    /// <summary>The province record selected by <c>Prov_Cur</c>, re-read on every access.</summary>
    private ProvRecord Prov => State.Provs[State.Prov_Cur];

    /// <summary>The item record selected by <c>Item_Cur</c>, re-read on every access.</summary>
    private ItemRecord Item => State.Items[State.Item_Cur];

    /// <summary><c>Y 4, n:</c> (<see cref="ScriptContext.Rnd"/>).</summary>
    private Sys3Int Rnd(Sys3Int n) => Script.Rnd(n);

    /// <summary><c>!Ev_Entry : entry!</c> <c>%#file:</c>.</summary>
    private Task EventAsync(string file, Sys3Int entry) => Script.EventAsync(file, entry);

    /// <summary>
    /// A tier-1 trigger: <c>!Ev_Entry : entry!</c> <c>%#file:</c> <c>!Ev_WeeklyEventDone : 1!</c>,
    /// and execution goes on with the next trigger.
    /// </summary>
    private async Task FireAsync(string file, Sys3Int entry)
    {
        await EventAsync(file, entry);
        State.Ev_WeeklyEventDone = 1;
    }

    /// <summary><c>COM.ADV:1-947</c>, the whole file; every <c>%0:</c> returns from here.</summary>
    public async Task RunAsync()
    {
        // QUIRK: STR-05 — week 9 and the whole thieves phase skip the upkeep and the triggers.
        if (State.Week == 9)                           // :1-5 the first week of the prologue
        {
            await EventAsync("Iｼﾚｲﾌﾞ.ADV", 10);
            return;
        }
        if (State.StoryStage == 0)                     // :6-10 the weekly thieves' message
        {
            await EventAsync("Gｿｳﾙ.ADV", 4);
            return;
        }
        Upkeep();                                      // :11-84
        await AllThatApplyAsync();                     // :85-300
        if (State.Ev_WeeklyEventDone != 0)             // :301-303
            return;
        await FirstMatchAsync();                       // :304-947
    }

    /// <summary><c>COM.ADV:11-84</c> — ninja availability, battle leftovers and the weekly counters.</summary>
    private void Upkeep()
    {
        State.F_Kanami_NinjaUsed = 99;                                          // :11-15 every ninja spent …
        State.F_Freya_NinjaUsed = 99;
        State.F_Kaoru_NinjaUsed = 99;
        State.F_Wichita_NinjaUsed = 99;
        State.F_Saitou_NinjaUsed = 99;
        if (State.N_Kanami_State != 0)                                          // :16-18 … then freed for each ninja serving
            State.F_Kanami_NinjaUsed = 0;
        State.Bt_GarrisonProv = 0;                                              // :19
        State.Bt_GarrisonHits = 0;                                              // :20
        if (State.N_Freya_State == 20)                                          // :21-23
            State.F_Freya_NinjaUsed = 0;
        if (State.N_Kaoru_State > 2 && State.N_Kaoru_State != 100)              // :24-26
            State.F_Kaoru_NinjaUsed = 0;
        if (State.N_Wichita_State > 2)                                          // :27-29
            State.F_Wichita_NinjaUsed = 0;
        if (State.F_Saitou_State > 1)                                           // :30-32
            State.F_Saitou_NinjaUsed = 0;
        if (State.Goliath_State == 2)                                           // :33-35
            State.Goliath_State = 1;
        if (State.N_Melfeis_Stage != 0 && State.N_Melfeis_Stage < 10)           // :36-38
            State.N_Melfeis_Stage = State.N_Melfeis_Stage + 1;
        if (State.N_Isoroku_Stage == 5)                                         // :39-41
            State.N_Isoroku_Stage = 6;
        State.N_Aviator_StudyWeeks = State.N_Aviator_StudyWeeks - 1;            // :42 (floors at 0)
        if (State.N_Miki_Timer != 5)                                            // :43-45
            State.N_Miki_Timer = State.N_Miki_Timer - 1;
        if (State.N_Aaya_Stage != 0 && State.N_Aaya_Stage < 4)                  // :46-48
            State.N_Aaya_Stage = State.N_Aaya_Stage + 1;
        if (State.N_Saizel_Weeks != 0)                                          // :49-51
            State.N_Saizel_Weeks = State.N_Saizel_Weeks + 1;
        if (State.N_ALChurch_Stage != 0 && State.N_ALChurch_Stage < 3)          // :52-54
            State.N_ALChurch_Stage = State.N_ALChurch_Stage + 1;
        if (State.F_Cafe_State == 10 || State.F_Cafe_State == 11)               // :55-57 weeks since the capture
            State.LadyCaravan_Prov = State.LadyCaravan_Prov + 1;
        if (State.F_Cafe_State < 10)                                            // :58-66 Lady Caravan's walk
        {
            // QUIRK: STR-18 — the walk has no bounds (0 saturates, no upper limit).
            Rnd(2);
            if (State.RetVal == 1)
                State.LadyCaravan_Prov = State.LadyCaravan_Prov + 1;
            if (State.RetVal == 2)
                State.LadyCaravan_Prov = State.LadyCaravan_Prov - 1;
        }
        if (State.N_Soul_Stage > 5 && State.N_Soul_Stage != 200)                // :67-69
            State.N_Soul_Stage = State.N_Soul_Stage + 1;
        State.TangeDog_UsedThisWeek = 0;                                        // :70
        State.Item_Cur = 26;                                                    // :71-77 kaiju attacks grow while the item is owned
        if (Item.Holder != 0)
        {
            State.Kaiju_JagiJagiAttacks = State.Kaiju_JagiJagiAttacks + 50;
            if (State.Kaiju_JagiJagiAttacks > 2000)
                State.Kaiju_JagiJagiAttacks = 2000;
        }
        State.Item_Cur = 27;                                                    // :78-84
        if (Item.Holder != 0)
        {
            State.Kaiju_AmoebaAttacks = State.Kaiju_AmoebaAttacks + 60;
            if (State.Kaiju_AmoebaAttacks > 2000)
                State.Kaiju_AmoebaAttacks = 2000;
        }
    }

    /// <summary>
    /// <c>COM.ADV:85-300</c> — tier 1: every trigger that applies plays its scene and sets
    /// <c>Ev_WeeklyEventDone</c>; the next conditions are tested on the state the scenes left.
    /// </summary>
    private async Task AllThatApplyAsync()
    {
        // QUIRK: STR-15 — the conditions below are read after the scenes above them ran.
        State.Ev_WeeklyEventDone = 0;                                                       // :85
        if (State.StoryStage == 1 && State.F_LeazasRebels_State != 0 && State.F_Maris_RebelTalk == 1) // :86-90
            await FireAsync("Gﾏﾘｽ.ADV", 15);
        if (State.StoryStage == 1 && State.F_LeazasRebels_State == 0)                       // :91-95
            await FireAsync("Iｼﾚｲﾌﾞ.ADV", 1);
        if (State.F_DemonCastle_Attacked != 0 && State.N_Warg_Stage == 0 && State.F_Kayblis_War == 0) // :96-100
            await FireAsync("Gﾜｰｸﾞ.ADV", 2);
        State.Prov_Cur = 28;                                                                // :101
        if (Prov.Owner == 1 && State.F_Japan_State == 0)                                    // :102-106
            await FireAsync("IJAPAN.ADV", 1);
        if (State.N_Camilla_Stage == 3)                                                     // :107-111
            await FireAsync("Iﾏｼﾞﾝ.ADV", 3);
        if (State.N_Camilla_Stage == 2)                                                     // :112-116
            await FireAsync("Iﾏｼﾞﾝ.ADV", 2);
        if (State.F_Kentarou_State == 2)                                                    // :117-121
            await FireAsync("Gｹﾝﾀﾛｳ.ADV", 2);
        if (State.N_Miki_Stage == 2)                                                        // :122-126
            await FireAsync("Gｹﾝﾀﾛｳ.ADV", 1);
        if ((State.F_Japan_State == 2 || State.F_Japan_State == 3)                          // :127-131
            && (State.F_Helman_War == 1 || State.F_Zeth_War == 1)
            && State.Ev_JapanNextWeek < State.Week)
            await FireAsync("IJAPAN.ADV", 2);
        if (State.F_Helman_War == 1 && State.F_Patton_State == 0)                           // :132-136
            await FireAsync("Gﾊﾟｯﾄﾝ.ADV", 1);
        if (State.F_ShangriLa_State == 2)                                                   // :137-141
            await FireAsync("Iｼｬﾝｸﾞﾘﾗ.ADV", 3);
        if (State.F_ShangriLa_State == 3)                                                   // :142-146
            await FireAsync("Iｼｬﾝｸﾞﾘﾗ.ADV", 4);
        if (State.N_ALChurch_Stage == 20)                                                   // :147-151
            await FireAsync("GAL.ADV", 4);
        if (State.StoryStage == 2 && State.N_ALChurch_Stage == 0)                           // :152-156
            await FireAsync("GAL.ADV", 1);
        if (State.N_ALChurch_Stage == 3)                                                    // :157-161
            await FireAsync("GAL.ADV", 2);
        if (State.F_Lia_State == 1 && State.N_Lia_Neglect == 10)                            // :162-166
            await FireAsync("Gﾘｱ.ADV", 1);
        if (State.F_Lia_State == 3 && State.N_Lia_Neglect > 4)                              // :167-171
            await FireAsync("Gﾘｱ.ADV", 2);
        State.Unit_Cur = 27;                                                                // :172
        if (Unit.Status != 0 && State.Week == State.Isoroku_BirthWeek)                      // :173-177
            await FireAsync("Gｲｿﾛｸ.ADV", 2);
        State.Unit_Cur = 31;                                                                // :178-186
        if (Unit.Status == 0)
        {
            State.Unit_Cur = 32;
            if (Unit.Status != 0)
                await FireAsync("Gﾐｷ.ADV", 8);
        }
        if (State.Papaya_EventWeek == State.Week)                                           // :187-191
            await FireAsync("Gﾊﾟﾊﾟｲｱ.ADV", 5);
        if (State.F_HQMeeting_Done == 0 && State.F_Zeth_War == 2 && State.F_Helman_War == 2 && State.StoryStage == 2) // :192-196
            await FireAsync("Iｼﾚｲﾌﾞ.ADV", 4);
        if (State.N_Aaya_Stage == 7)                                                        // :197-201
            await FireAsync("Gｱｰﾔ.ADV", 5);
        State.Unit_Cur = 18;                                                                // :202-210
        if (Unit.Status == 0)
        {
            State.Unit_Cur = 19;
            if (Unit.Status != 0)
            {
                State.Ev_WeeklyEventDone = 1;                                               // :206, before the call
                await EventAsync("Gﾊﾝﾃｨ.ADV", 1);
            }
        }
        State.Unit_Cur = 25;                                                                // :211
        if (Unit.Status != 0 && State.F_Asuka_State == 2 && State.F_Mill_State < 2)         // :212-216
            await FireAsync("Gｶﾄﾞﾐｭｳﾑ.ADV", 2);
        if (State.F_Kou_State == 7)                                                         // :217-221
        {
            State.Ev_Entry = 5;
            State.Ev_WeeklyEventDone = 1;                                                   // :219, before the call
            await Script.CallFileAsync("Gｺｳﾋﾒ.ADV");
        }
        State.Cmdr_Cur = 54;                                                                // :222
        if (Cmdr.Status != 0 && State.F_Helman_War == 2 && State.F_PattonArmy_State == 0 && State.F_Helman_FallCause != 1) // :223-227
            await FireAsync("Gﾊﾟｯﾄﾝ.ADV", 6);
        State.Cmdr_Cur = 129;                                                               // :228
        if (Cmdr.Status != 0 && State.N_Babolat_PitfallStep != 0 && State.Babolat_PitfallProv == 0) // :229-233
            await FireAsync("Gﾊﾞﾎﾞﾗ.ADV", 5);
        State.Cmdr_Cur = 101;                                                               // :234-247 Zeth's four generals all down
        if (Cmdr.Status == 0)
        {
            State.Cmdr_Cur = 104;
            if (Cmdr.Status == 0)
            {
                State.Cmdr_Cur = 107;
                if (Cmdr.Status == 0)
                {
                    State.Cmdr_Cur = 110;
                    if (State.F_Zeth_War == 1 && Cmdr.Status == 0)
                        await EventAsync("Iｾﾞｽ.ADV", 100); // QUIRK: STR-17 — Ev_WeeklyEventDone stays as it was
                }
            }
        }
        if (State.F_Code_State == 3 && State.F_Zeth_War == 1)                               // :248-252
            await FireAsync("Gｺｰﾄﾞ.ADV", 4);
        if (State.DgnRun_MerimInParty == 2)                                                 // :253-257
            await FireAsync("Gﾒﾘﾑ.ADV", 1);
        if (State.Week > 39 && State.F_Fuuka_State == 0)                                    // :258-262
            await FireAsync("Gﾌｳｶ.ADV", 1);
        if (State.Week > 51 && State.F_Fuuka_State != 0 && State.F_Fuuka_State < 10)        // :263-267
            await FireAsync("Gﾌｳｶ.ADV", 2);
        if (State.F_Kou_State == 10)                                                        // :268-272
            await FireAsync("Gｺｳﾋﾒ.ADV", 6);
        State.Unit_Cur = 37;                                                                // :273
        if (State.F_Gandhi_State == 99 && (State.N_Kaoru_State != 0 || State.N_Wichita_State != 0)) // :274-278
            await FireAsync("Gｶﾞﾝｼﾞｰ.ADV", 8);
        // QUIRK: STR-15 — Unit_Cur is whatever Gｶﾞﾝｼﾞｰ 8 left when it just ran.
        if (Unit.Status == 0 && (State.N_Kaoru_State != 0 || State.N_Wichita_State != 0))  // :279-283
            await FireAsync("Gｶﾞﾝｼﾞｰ.ADV", 7);
        if (State.StoryStage == 3 && State.F_Maris_DemonTalkSeen == 0 && State.F_Redeye_State != 0) // :284-288
            await FireAsync("Gﾏﾘｽ.ADV", 17);
        State.Unit_Cur = 49;                                                                // :289
        if (Unit.Status == 2)                                                               // :290-294
            await FireAsync("Gﾖｳﾍｲ.ADV", 1);
        State.Unit_Cur = 50;                                                                // :295
        if (Unit.Status == 2)                                                               // :296-300
            await FireAsync("Gﾖｳﾍｲ.ADV", 2);
    }
}
