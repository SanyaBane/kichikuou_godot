using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>MAIN.ADV</c>, the week loop (part W2), ported one-to-one
/// (<c>docs/strategy/survey-main.md</c> §2.1, <c>docs/porting-conventions.md</c>) as far as the
/// prologue reaches it: the top of the week (<c>Week + 1</c>, the history shift) and the thieves'
/// week (<c>StoryStage = 0</c>, <c>MAIN.ADV:19-82</c>) — the autosave, the date, the banner, the
/// refresh of units 1-4, <c>COMMAND</c>, and the end of the prologue (<c>StoryStage 99 → 1</c>,
/// <c>Iﾊﾝﾗﾝ</c>). The Leazas week (<c>MAIN.ADV:83-1045</c>: income, refill, expenses, the nation
/// phases, the end-of-week checks) is not ported yet and fails with
/// <see cref="NotSupportedException"/> before its first statement. Every file the loop calls —
/// the save screen (<c>ﾛｰﾄﾞｾｰﾌﾞ</c>, a hook until the save/load part), <c>COMMAND</c>, the scenes —
/// goes through the context's hooks (<see cref="ScriptContext.CallFileAsync"/>).
/// </summary>
/// <remarks>
/// <c>MAIN</c> is entered by a jump (<c>&amp;#MAIN.ADV:</c>, <c>ｽﾀMENU.ADV:291</c>) and never returns:
/// <see cref="RunAsync"/> loops for ever, ending only by an exception (a jump to <c>OVER</c>, an
/// unported path). One pass, from <c>L_00020</c> to the next arrival there, is
/// <see cref="RunWeekAsync"/> — the span of a <c>week</c> golden capture (root <c>MAIN.ADV@20</c>).
/// </remarks>
public sealed class WeekLoop(ScriptContext script)
{
    private const string File = "MAIN.ADV";

    /// <summary>The shared services the file runs on.</summary>
    public ScriptContext Script { get; } = script;

    private GameState State => Script.State;

    /// <summary>The player-unit record selected by <c>Unit_Cur</c>, re-read on every access.</summary>
    private UnitRecord Unit => State.Units[State.Unit_Cur];

    /// <summary>The dungeon record selected by <c>Dgn_Cur</c>, re-read on every access.</summary>
    private DgnRecord Dgn => State.Dgns[State.Dgn_Cur];

    /// <summary><c>L_00020</c>: the week loop, for ever (<c>@L_00020</c> at <c>MAIN.ADV:81, 1045</c>).</summary>
    public async Task RunAsync()
    {
        while (true)
            await RunWeekAsync();
    }

    /// <summary>
    /// <c>MAIN.ADV:1-81</c>: one week, from <c>L_00020</c> up to the jump back to it. Fails with
    /// <see cref="NotSupportedException"/> at <c>MAIN.ADV:83</c> when the week is not the thieves' one.
    /// </summary>
    public async Task RunWeekAsync()
    {
        await Script.WaoAsync(3);                                       // :2 \L_01a25: the face over the map
        await Script.MapCallAsync(0);                                   // :3-4
        State.Week = State.Week + 1;                                    // :5
        ShiftHistory();                                                 // :6-15
        if (State.BgmEnabled == 1)                                      // :16-18
            await Script.MusicAsync(Sys3Int.Zero);
        if (State.StoryStage != 0)
            throw new NotSupportedException($"{File}:83: the Leazas week (MAIN.ADV:83-1045) is not ported yet.");
        await ThievesWeekAsync();                                       // :19-82
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
        State.History[State.Hist_Cur].Funds = 0;                        // :12-15
        State.History[State.Hist_Cur].SundryExp = 0;
        State.History[State.Hist_Cur].MilitaryExp = 0;
        State.History[State.Hist_Cur].Casualties = 0;
    }

    /// <summary>
    /// <c>MAIN.ADV:19-82</c> — the thieves' week: the autosave, the date, the banner over the
    /// hideout, units 1-4 refreshed, <c>COMMAND</c>; a prologue ended by the lost duel
    /// (<c>StoryStage = 99</c>) becomes stage 1 with <c>Iﾊﾝﾗﾝ</c>; then "Then some time passed".
    /// </summary>
    private async Task ThievesWeekAsync()
    {
        await AutosaveAsync();                                          // :20 \L_01875
        SetDate();                                                      // :21 \L_01881
        if (State.BgmEnabled == 1)                                      // :22-25
        {
            State.CurrentBgm = 1;
            await Script.MusicAsync(State.CurrentBgm);
        }
        State.Dgn_Cur = 1;                                              // :26-32 to the thieves' hideout
        State.MapTargetX = Dgn.MapX;
        State.MapTargetY = Dgn.MapY;
        await Script.MapCallAsync(1);
        await Script.MapCallAsync(10);
        await Script.WaoAsync(113);                                     // :33 \L_0191e
        await Script.TextAsync(File, 35);                               // "Rance's Thieves Phase"
        await ZoomAsync(20);
        await Script.ShowAsync(new ValueMessageEvent(Ref(40), [State.Tmp0001, State.Tmp0002, State.Tmp0003])); // "LP y, Month m, Week w"
        await ZoomAsync(24);
        RefreshThieves();                                               // :50-64
        await Script.WaoAsync(1);                                       // :66 \L_01a0b
        await Script.WaoAsync(0);                                       // :67 \L_019fe
        State.History[State.Hist_Cur].Funds = State.Gold;               // :68
        await Script.CallFileAsync("COMMAND.ADV");                      // :69
        if (State.StoryStage == 99)                                     // :70-73 the prologue is over
        {
            State.StoryStage = 1;
            await Script.CallFileAsync("Iﾊﾝﾗﾝ.ADV");
        }
        await TimePassesAsync();                                        // :74-80
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

    /// <summary><c>MAIN.ADV:74-80</c> (and <c>:1038-1044</c>): "Then some time passed." between fades.</summary>
    private async Task TimePassesAsync()
    {
        await Script.ShowAsync(new EngineEvent("PF1", [State.FadeSpeed]));
        await Script.WaoAsync(113);                                     // \L_0191e
        await Script.TextAsync(File, 77);                               // "Then some time passed."
        await Script.ShowAsync(new EngineEvent("PF0", [State.FadeSpeed]));
        await Script.WaoAsync(1);                                       // \L_01a0b
        await Script.WaoAsync(0);                                       // \L_019fe
    }

    /// <summary>
    /// <c>L_01875</c> (<c>MAIN.ADV:1056-1059</c>) — the autosave: <c>ﾛｰﾄﾞｾｰﾌﾞ</c> with <c>RetVal 3</c>
    /// (slot 1, then page 4 to every slot). Not ported: a hook until the save/load part; a load
    /// resumes the original right after this call.
    /// </summary>
    private Task AutosaveAsync()
    {
        State.RetVal = 3;
        return Script.CallFileAsync("ﾛｰﾄﾞｾｰﾌﾞ.ADV");
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
    private Task ZoomAsync(int size) => Script.ShowAsync(new EngineEvent("ZM", [(Sys3Int)size]));

    private static ScriptRef Ref(int line) => new(File, line);
}
