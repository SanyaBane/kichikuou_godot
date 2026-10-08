using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Cﾊｰﾚﾑ.ADV</c>, the Harem command (<c>docs/strategy/survey-commands.md</c> §3.8): the visit is
/// paid up front and refunded on a cancel; the girls on offer go into <c>HaremAvail_*</c>, the
/// picker <c>Sﾊｰﾚﾑ</c> (a hook) answers the girl in <c>CallArg</c>, her scene plays with
/// <c>Ev_Entry</c> 10, and if it set <c>Harem_ExpReward</c> Rance gains that much experience. In
/// the prologue only Sill, Soul and (once captured) Anasel are on offer; the intro draw and the
/// list of the later stages (<c>:6-28, 47-212</c>) are part P7 and fail as not ported.
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

    /// <summary><c>Cﾊｰﾚﾑ.ADV:1-577</c>: the command, up to its return.</summary>
    public async Task RunAsync()
    {
        if (State.F_SillCow_State == 5)                                 // :1-5
        {
            await Script.EventAsync("Gｼｨﾙ.ADV", 5);
            return;
        }
        if (State.StoryStage != 0)
            throw NotPorted(6, "the harem beyond the prologue", "P7");
        if (State.F_Lia_State == 1)                                     // :29-34
        {
            State.N_Lia_Neglect = State.N_Lia_Neglect + 1;
            if (State.N_Lia_Neglect > 10)
                State.N_Lia_Neglect = 10;
        }
        State.Ev_Entry = 10;                                            // :35-46
        State.Ui_CmdHaremAvail = State.Ui_CmdHaremAvail - 1;
        State.Harem_ExpReward = 0;
        for (int i = 0; i < 50; i++)                                    // NC HaremAvail_Sill, 50
            State.Memory.Var(249 + i) = Sys3Int.Zero;
        State.HaremAvail_Sill = 1;
        State.HaremAvail_Soul = 1;
        if (State.F_Thieves_State > 1)
            State.HaremAvail_Anasel = 1;

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
    }
}
