using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>TJAPAN.ADV</c>, JAPAN's turn (part N2, milestone 3 step 5; called by <c>MAIN.ADV:399</c> while
/// <c>F_Japan_State = 1</c>), ported one-to-one on the nation template
/// (<c>docs/strategy/survey-nations.md</c> §2.3-§2.4). Gekkou's attempt first while
/// <c>N_Gekkou_Attempts</c> is 1-4 (<c>L_006ac</c>, the scenes <c>:295-2739</c>, not ported yet);
/// Kouhime's scenes on two draws (<c>Gｺｳﾋﾒ</c> 4 while <c>F_Kou_State = 1</c>, 3 while 0); one week in
/// four nothing more — a random event (<c>Gｺｳﾋﾒ</c> 1/2, <c>IJAPAN</c> 7/8) when nothing happened
/// yet —, else one of three armies on a draw in five: the whole army from Nobunaga Oda (72) (1),
/// Katsuie (75) to Denden Mori (78) (2, 3) or One-Shot Tarou (79) to Isoroku Yamamoto (82) (4, 5),
/// a commander under 190 troops left out once someone leads. With 500 troops it attacks the first
/// of Nagasaki (55), Portugal (28), Toushin City (27), M Land (25), Hanna (26), Zeo (24), Red (23),
/// Oaks (6), Oku (3), North (2), Ys (5), South (4), Leazas Castle (1) the player holds, and takes
/// Toushin City, Hanna, Zeo or Red from the independent cities on its way without a battle
/// (NAT-22); a walk that finds nothing falls into that capture at Leazas Castle (1) (NAT-23). The
/// army name (<c>MS 10</c>) is read from the player's files through
/// <paramref name="scenario"/>.
/// </summary>
public sealed class JapanTurn(ScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "TJAPAN.ADV", scenario)
{
    /// <summary>The walk's provinces in order, and whether the cities lose each without a battle.</summary>
    private static readonly (int Province, bool Taken)[] Targets =
    [
        (55, false), (28, false), (27, true), (25, false), (26, true), (24, true), (23, true),
        (6, false), (3, false), (2, false), (5, false), (4, false), (1, false),
    ];

    /// <summary><c>TJAPAN.ADV:1-147</c>.</summary>
    public async Task RunAsync()
    {
        State.Ev_PhaseNation = 6;                                       // :1
        SetLiteral(3);                                                  // :2-3 MS 10, "JAPAN Army"
        State.Bt_EnemyRetreatMode = 3;                                  // :4-5
        State.Ev_WeeklyEventDone = 0;
        if (State.N_Gekkou_Attempts != 0 && State.N_Gekkou_Attempts != 5)   // :6-9
            throw NotPorted(295, "Gekkou's attempt (L_006ac)", "N2");
        Script.Rnd(2);                                                  // :10-15
        if (State.RetVal == 1 && State.F_Kou_State == 1 && State.Ev_WeeklyEventDone == 0)
        {
            State.Ev_WeeklyEventDone = 1;
            await Script.EventAsync("Gｺｳﾋﾒ.ADV", 4);
        }
        Script.Rnd(2);                                                  // :16-21
        if (State.RetVal == 1 && State.F_Kou_State == 0 && State.Ev_WeeklyEventDone == 0)
        {
            State.Ev_WeeklyEventDone = 1;
            await Script.EventAsync("Gｺｳﾋﾒ.ADV", 3);
        }
        Script.Rnd(4);                                                  // :22-28 a quiet week
        if (State.RetVal == 1)
        {
            if (State.Ev_WeeklyEventDone == 0)
                await RandomEventAsync();                               // \L_0327c
            return;
        }
        Script.Rnd(5);                                                  // :29-62 the army
        if (State.RetVal == 1)
            SetPriority(72, 73, 74, 78, 77, 75, 82, 79, 80);
        if (State.RetVal == 2 || State.RetVal == 3)
            SetPriority(75, 76, 77, 78);
        if (State.RetVal == 4 || State.RetVal == 5)
            SetPriority(79, 80, 81, 82);
        PickArmy();                                                     // \L_004b3
        SumTroops();                                                    // \L_0046d
        if (State.Bt_EnemyParty1 == 0)                                  // :65-70
            return;
        if (State.CallArg < 500)
            return;
        foreach (var (province, taken) in Targets)                      // :71-147
        {
            State.Prov_Cur = (Sys3Int)province;
            if (Prov.Owner == 1)
            {
                await Script.CallFileAsync("FIGT_B.ADV");
                return;
            }
            // QUIRK: NAT-22 — only these four are taken from the cities; M Land (25) is passed by.
            if (taken && Prov.Owner == 3)
            {
                await TakeCityAsync();                                  // @L_00366
                return;
            }
        }
        // QUIRK: NAT-23 — the walk has no end: it falls into L_00366 at Leazas Castle (1).
        await TakeCityAsync();
    }

    /// <summary>The filler's filter (<c>TJAPAN.ADV:274-277</c>): under 190 troops, once slot 1 is taken.</summary>
    protected override bool SkipsCommander() => Cmdr.Troops < 190 && State.Bt_EnemyParty1 != 0;

    /// <summary>
    /// <c>TJAPAN.ADV:2745-2767</c> (<c>L_0327c</c>) — on a draw in four: <c>Gｺｳﾋﾒ</c> 1 or 2,
    /// <c>IJAPAN</c> 7 or 8.
    /// </summary>
    private async Task RandomEventAsync()
    {
        Script.Rnd(4);
        if (State.RetVal == 1)
            await Script.EventAsync("Gｺｳﾋﾒ.ADV", 1);
        else if (State.RetVal == 2)
            await Script.EventAsync("Gｺｳﾋﾒ.ADV", 2);
        else if (State.RetVal == 3)
            await Script.EventAsync("IJAPAN.ADV", 7);
        else if (State.RetVal == 4)
            await Script.EventAsync("IJAPAN.ADV", 8);
    }

    /// <summary>
    /// <c>TJAPAN.ADV:148-202</c> (<c>L_00366</c>) — JAPAN takes <c>Prov_Cur</c> from the independent
    /// cities without a battle: the camera to it and its mark blinked, its picture and the report
    /// (army and city names), owner <c>Ev_PhaseNation</c>, the flag set and the map redrawn.
    /// </summary>
    private async Task TakeCityAsync()
    {
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        await Script.MapCallAsync(10);
        CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);         // MM 8, the province's name
        State.CallArg = Prov.BgCg;
        await WaoAsync(331);                                            // \L_033f2
        await SpeakAsync(113, 162, 171, 179, 183);                      // \L_0332e
        Prov.Owner = State.Ev_PhaseNation;
        State.Tmp0021 = Prov.Owner + 819;
        State.MapTargetX = Prov.MapX;
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(4);
        await Script.MapCallAsync(3);
        await Script.MapCallAsync(0);
    }
}
