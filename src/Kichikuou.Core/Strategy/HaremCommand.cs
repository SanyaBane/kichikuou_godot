using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾊｰﾚﾑ.ADV</c>, the Harem command (<c>docs/strategy/survey-commands.md</c> §3.8), ported whole:
/// after the prologue a draw on every visit may first play Suzume's or Wendy's first meeting (or
/// Wendy's bedroom), and the cafe's scene takes the visit; the visit is paid up front and
/// refunded on a cancel; the girls on offer go into <c>HaremAvail_*</c> (in the prologue Sill, Soul
/// and, once captured, Anasel; later a 50-row list of units present and story flags), the picker
/// <c>Sﾊｰﾚﾑ</c> answers the girl in <c>CallArg</c>, her scene plays with <c>Ev_Entry</c> 10, and if
/// it set <c>Harem_ExpReward</c> Rance gains that much experience.
/// </summary>
public sealed class HaremCommand(ScriptContext script) : CommandFile(script, "Cﾊｰﾚﾑ.ADV")
{
    /// <summary>The girls' scenes by their picker number (<c>:224-546</c>); 1, 2, 13 and 25 are handled apart.</summary>
    private static readonly Dictionary<int, string> Scenes = new()
    {
        [3] = "Gﾏﾘｽ.ADV", [4] = "Gｽｽﾞﾒ.ADV", [5] = "Gｳｪﾝﾃﾞｨ.ADV", [6] = "Gｶﾅﾐ.ADV",
        [7] = "Gﾚｲﾗ.ADV", [8] = "Gﾒﾅﾄﾞ.ADV", [9] = "Gﾊｳﾚｰﾝ.ADV", [10] = "Gﾒﾙﾌｪｲｽ.ADV",
        [11] = "Gｿｳﾙ.ADV", [12] = "Gｱﾅｾﾙ.ADV", [14] = "Gﾏﾘｱ.ADV", [15] = "Gｼｽﾞｶ.ADV",
        [16] = "Gﾗﾝ.ADV", [17] = "Gﾐﾘ.ADV", [18] = "Gﾐﾙ.ADV", [19] = "Gﾓｺﾓｺ.ADV",
        [20] = "Gｳﾝｶﾞ.ADV", [21] = "Gﾒﾘﾑ.ADV", [22] = "Gﾚｨﾘｨ.ADV", [23] = "Gｱｵｲ.ADV",
        [24] = "Gｼｬﾘｴﾗ.ADV", [26] = "Gｱﾐﾗﾝ.ADV", [27] = "Gｶﾁｭｰｼｬ.ADV", [28] = "Gｿﾙﾆｱ.ADV",
        [29] = "Gｻﾃﾗ.ADV", [30] = "Gｼﾞｭﾘｱ.ADV", [31] = "Gﾗﾌｧﾘｱ.ADV", [32] = "Gｱｰﾙｺｰﾄ.ADV",
        [33] = "Gｸﾘｰﾑ.ADV", [34] = "Gﾙｰﾍﾞﾗﾝ.ADV", [35] = "Gｼｰﾗ.ADV", [36] = "Gﾁｽﾞｺ.ADV",
        [37] = "Gﾏｼﾞｯｸ.ADV", [38] = "Gﾅｷﾞ.ADV", [39] = "Gﾏｺﾄ.ADV", [40] = "Gﾜｰｸﾞ.ADV",
        [41] = "Gｶｸｻﾝ.ADV", [42] = "Gｽｹｻﾝ.ADV", [43] = "Gｴﾘｻﾞﾍﾞ.ADV", [44] = "Gｲｿﾛｸ.ADV",
        [45] = "Gﾒﾙｼｨ.ADV", [46] = "Gｺｳﾋﾒ.ADV", [47] = "Gﾚﾍﾞｯｶ.ADV", [48] = "Gｱﾆｽ.ADV",
        [49] = "Gｴﾚﾅ.ADV", [50] = "Gｻｲﾄｳ.ADV",
    };

    /// <summary>
    /// <c>Cﾊｰﾚﾑ.ADV:1-577</c>: the command, up to its return. <c>:578-633</c> follow the return
    /// without a label (CMD-20): a draw of one of Rance's six modifiers and a +1 to it, never run.
    /// </summary>
    public async Task RunAsync()
    {
        if (State.F_SillCow_State == 5)                                 // :1-5
        {
            await Script.EventAsync("Gｼｨﾙ.ADV", 5);
            return;
        }
        if (State.StoryStage != 0 && !await IntroAsync())
            return;
        if (State.F_Lia_State == 1)                                     // :29-34
        {
            State.N_Lia_Neglect = State.N_Lia_Neglect + 1;
            if (State.N_Lia_Neglect > 10)
                State.N_Lia_Neglect = 10;
        }
        State.Ev_Entry = 10;                                            // :35-38
        State.Ui_CmdHaremAvail = State.Ui_CmdHaremAvail - 1;
        State.Harem_ExpReward = 0;
        for (int i = 0; i < 50; i++)                                    // NC HaremAvail_Sill, 50
            State.Memory.Var(249 + i) = Sys3Int.Zero;
        if (State.StoryStage == 0)                                      // :39-46
        {
            State.HaremAvail_Sill = 1;
            State.HaremAvail_Soul = 1;
            if (State.F_Thieves_State > 1)
                State.HaremAvail_Anasel = 1;
        }
        else
            OfferGirls();

        while (true)
        {
            // L_00610 (:213-383)
            await Script.CallFileAsync("Sﾊｰﾚﾑ.ADV");
            State.Tmp0001 = State.CallArg;
            State.CallArg = State.Tmp0001;
            if (State.CallArg == 0 || State.CallArg > 500)
            {
                // L_00c73 (:547-552) — the visit is given back.
                // QUIRK: CMD-13 — the cancel lowers Lia's counter even when the visit's +1 was capped.
                if (State.F_Lia_State == 1)
                    State.N_Lia_Neglect = State.N_Lia_Neglect - 1;
                State.Ui_CmdHaremAvail = State.Ui_CmdHaremAvail + 1;
                return;
            }
            State.Hist_Cur = State.CallArg;
            State.Harem_LastPick = State.CallArg;
            Hist.Harem_LastWeek = State.Week;
            State.Hist_Cur = 0;
            if (State.CallArg <= 50)
                break;
            await Script.ShowAsync(new MessageResetEvent(Sys3Int.Zero));   // :374-383 Y 1, 0 and the error
            await ValuesAsync(376, State.CallArg);
            await HitAnyKeyAsync();
        }

        if (await VisitAsync(State.CallArg.Value))
            await RewardAsync();
    }

    /// <summary>
    /// <c>:6-28</c>, after the prologue: one draw, then Suzume's (1) or Wendy's (2) first meeting,
    /// Wendy's bedroom (2, her state 4), and the cafe's scene, which takes the week's visit and
    /// leaves the file (false). Each test reads <c>RetVal</c> as the scene before it left it.
    /// </summary>
    private async Task<bool> IntroAsync()
    {
        // QUIRK: STR-14 — drawn on every visit, also when nothing comes of it or the visit is cancelled.
        Script.Rnd(4);
        if (State.RetVal == 1 && State.F_Suzume_Met == 0)              // :8-12
        {
            State.F_Suzume_Met = 1;
            await Script.EventAsync("Gｽｽﾞﾒ.ADV", 2);
        }
        if (State.RetVal == 2 && State.F_Wendy_Met == 0)               // :13-17
        {
            State.F_Wendy_Met = 1;
            await Script.EventAsync("Gｳｪﾝﾃﾞｨ.ADV", 2);
        }
        if (State.RetVal == 2 && State.F_Wendy_State == 4 && State.F_Wendy_BedroomSeen == 0)   // :18-21
            await Script.EventAsync("Gｳｪﾝﾃﾞｨ.ADV", 4);
        if ((State.F_Cafe_State == 10 || State.F_Cafe_State == 11) && State.LadyCaravan_Prov > 4)   // :22-27
        {
            await Script.EventAsync("Gｶﾌｪ.ADV", 3);
            State.Ui_CmdHaremAvail = 0;
            return false;
        }
        return true;
    }

    /// <summary>
    /// <c>:47-212</c>, the girls on offer after the prologue: Lia and Maris always, the others by
    /// their unit's presence (<c>Unit_Status</c> through the cursor, left at 27) and story flags.
    /// </summary>
    private void OfferGirls()
    {
        State.HaremAvail_Lia = 1;
        State.HaremAvail_Maris = 1;
        if (State.F_Suzume_Met == 1)
            State.HaremAvail_Suzume = 1;
        if (State.F_Wendy_Met == 1)
            State.HaremAvail_Wendy = 1;
        State.Unit_Cur = 2;                                             // :55
        if (State.F_Sill_State == 1 || Unit.Status != 0)
            State.HaremAvail_Sill = 1;
        State.Unit_Cur = 4;                                             // :59
        if (State.N_Soul_Stage == 1 || Unit.Status != 0)
            State.HaremAvail_Soul = 1;
        if (State.N_Kanami_State != 0)
            State.HaremAvail_Kanami = 1;
        State.Unit_Cur = 5;                                             // :66
        if (Unit.Status != 0 && State.F_Leila_State != 0)
            State.HaremAvail_Leila = 1;
        if (State.F_Leila_State > 1)
            State.HaremAvail_Leila2 = 1;
        State.Unit_Cur = 10;                                            // :73
        if (State.F_Menad_State != 0 && Unit.Status != 0)
            State.HaremAvail_Menad = 1;
        State.Unit_Cur = 12;                                            // :77
        if (Unit.Status != 0)
            State.HaremAvail_Haurein = 1;
        State.Unit_Cur = 13;                                            // :81
        if (Unit.Status != 0)
            State.HaremAvail_Melfeis = 1;
        if (State.F_Anasel_Fate > 1)
            State.HaremAvail_Anasel = 1;
        State.Unit_Cur = 16;                                            // :88
        if (Unit.Status != 0)
            State.HaremAvail_Maria = 1;
        State.Unit_Cur = 17;                                            // :92
        if (Unit.Status != 0)
            State.HaremAvail_Shizuka = 1;
        State.Unit_Cur = 23;                                            // :96
        if (Unit.Status != 0)
            State.HaremAvail_Ran = 1;
        State.Unit_Cur = 24;                                            // :100
        if (Unit.Status != 0)
            State.HaremAvail_Milli = 1;
        State.Unit_Cur = 25;                                            // :104
        if (Unit.Status != 0)
            State.HaremAvail_Mill = 1;
        if (State.F_Mokomoko_State > 10 && State.F_Mokomoko_State != 13 && State.F_Mokomoko_State != 14)
            State.HaremAvail_Mokomoko = 1;
        if (State.F_Sayori_State != 0)
            State.HaremAvail_Sayori = 1;
        if (State.F_Merim_Relation != 0 && State.F_Merim_Dungeon == 1)
            State.HaremAvail_Merim = 1;
        if (State.F_Raylee_State != 0 && State.F_Raylee_State < 10)
            State.HaremAvail_Raylee = 1;
        if (State.F_Aoi_State != 0 && State.F_Aoi_State < 10)
            State.HaremAvail_Aoi = 1;
        if (State.F_ShangriLa_State > 2 && State.F_Shariela_State != 4)
            State.HaremAvail_Shariela = 1;
        if (State.F_SillCow_State == 7 || State.F_SillCow_State == 8)
            State.HaremAvail_CowSill = 1;
        if (State.N_Amiran_Stage > 1 && State.N_Amiran_Stage != 100 && State.N_Amiran_Stage != 90)
            State.HaremAvail_Amiran = 1;
        if (State.F_Katyusha_State != 0 && State.F_Katyusha_State < 9)
            State.HaremAvail_Katyusha = 1;
        if (State.F_Saulnia_State != 0 && State.F_Saulnia_State < 9)
            State.HaremAvail_Saulnia = 1;
        State.Unit_Cur = 29;                                            // :138
        if (Unit.Status != 0 && State.F_Satella_State > 2)
            State.HaremAvail_Satella = 1;
        State.Unit_Cur = 33;                                            // :142
        if (Unit.Status != 0 && State.F_Julia_State > 1)
            State.HaremAvail_Julia = 1;
        State.Unit_Cur = 34;                                            // :146
        if (Unit.Status != 0 && State.F_Rafalia_State > 1)
            State.HaremAvail_Rafalia = 1;
        State.Unit_Cur = 35;                                            // :150
        if (Unit.Status != 0 && State.F_Arlcoate_State > 1)
            State.HaremAvail_Arlcoate = 1;
        State.Unit_Cur = 28;                                            // :154
        if (Unit.Status != 0)
            State.HaremAvail_Cream = 1;
        if (State.F_Ruberan_State > 3 && State.F_Ruberan_State != 100)
            State.HaremAvail_Ruberan = 1;
        State.Unit_Cur = 43;                                            // :161
        if (Unit.Status == 0 && State.F_Helman_FallCause == 2)
            State.HaremAvail_Sheila = 1;
        if (State.F_Chizuko_State > 2)
            State.HaremAvail_Chizuko = 1;
        State.Unit_Cur = 39;                                            // :168
        if (Unit.Status != 0)
            State.HaremAvail_Magic = 1;
        State.Unit_Cur = 40;                                            // :172
        if (Unit.Status != 0 && State.F_Nagi_State > 10)
            State.HaremAvail_Nagi = 1;
        if (State.F_Makoto_State != 0)
            State.HaremAvail_Makoto = 1;
        if (State.F_Warg_State > 1)
            State.HaremAvail_Warg = 1;
        if (State.N_Kaoru_State != 0 && State.N_Kaoru_State != 100)
            State.HaremAvail_Kaoru = 1;
        if (State.N_Wichita_State != 0)
            State.HaremAvail_Wichita = 1;
        if (State.F_Elisabeth_State != 0 && State.F_Elisabeth_State != 10)
            State.HaremAvail_Elisabeth = 1;
        State.Unit_Cur = 27;                                            // :191
        if (State.N_Isoroku_Stage != 0 && State.F_Isoroku_Fate != 2 && Unit.Status != 0)
            State.HaremAvail_Isoroku = 1;
        if (State.F_Merci_State > 1)
            State.HaremAvail_Merci = 1;
        if (State.F_Kou_State > 2 && State.F_Kouhime_Dead != 2)
            State.HaremAvail_Kou = 1;
        if (State.F_Rebecca_State > 1 && State.F_Rebecca_State != 100)
            State.HaremAvail_Rebecca = 1;
        if (State.F_Annis_State != 0)
            State.HaremAvail_Annis = 1;
        if (State.F_Elena_State > 6)
            State.HaremAvail_Elena = 1;
        if (State.F_Saitou_State > 1)
            State.HaremAvail_Saitou = 1;
    }

    /// <summary>
    /// The girl's label (<c>:384-546</c>): her scene, with the few that do more first; false when
    /// the label leaves the file (<c>%0:</c>) instead of going on to the reward.
    /// </summary>
    private async Task<bool> VisitAsync(int girl)
    {
        switch (girl)
        {
            case 25:                                                    // L_00a63: Cow Sill
                if (State.F_SillCow_State == 7)
                {
                    await Script.EventAsync("Gｼｨﾙ.ADV", 7);
                    return false;
                }
                if (State.F_SillCow_State == 8)
                {
                    await Script.EventAsync("Gｼｨﾙ.ADV", 8);
                    return false;
                }
                // QUIRK: CMD-21 — no %0: in any other state the label runs on into Sill's.
                goto case 1;
            case 1:                                                     // L_00a99
                if (State.F_Lia_State == 3)
                    State.N_Lia_Neglect = State.N_Lia_Neglect + 1;
                await Script.CallFileAsync("Gｼｨﾙ.ADV");
                break;
            case 2:                                                     // L_00acb: Lia
                State.N_Lia_Neglect = 0;
                await Script.CallFileAsync("Gﾘｱ.ADV");
                break;
            case 13:                                                    // L_00b06: Leila's second scene
                await Script.EventAsync("Gﾚｲﾗ.ADV", 1);
                break;
            default:
                await Script.CallFileAsync(Scenes[girl]);
                break;
        }
        return true;
    }

    /// <summary>
    /// <c>L_00c95</c> (<c>:553-577</c>): the map back, the music, and the experience the scene set
    /// in <c>Harem_ExpReward</c> (none: nothing more) — counted for the girl, added to Rance.
    /// </summary>
    private async Task RewardAsync()
    {
        await Script.MapCallAsync(0);
        await Script.MusicAsync(State.CurrentBgm);
        // QUIRK: CMD-15 — scenes that never set Harem_ExpReward end here.
        if (State.Harem_ExpReward == 0)
            return;
        // QUIRK: CMD-14 — Hist_Cur is left at the girl's index.
        State.Hist_Cur = State.Harem_LastPick;
        Hist.Harem_SexCount = Hist.Harem_SexCount + 1;
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_00e95
        await WaoAsync(113);                                            // \L_00e79
        await ValuesAsync(566, State.Harem_ExpReward);                  // "Rance gained experience? n EXP obtained."
        await WaoAsync(1);                                              // \L_00f66
        State.Unit_Cur = 1;
        Unit.Exp = Unit.Exp + State.Harem_ExpReward;
        await WaoAsync(0);                                              // \L_00f59
        await WaoAsync(312);                                            // \L_00ea3
        // QUIRK: CMD-20 — :578-633 (a random +1 to one of Rance's modifiers) follow the %0: unlabelled.
    }
}
