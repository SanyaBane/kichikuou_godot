using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>Tﾊﾟｯﾄﾝ.ADV</c>, the Patton army's weekly turn once Helman has fallen (part N2, milestone 3
/// step 1; called by <c>MAIN.ADV:505</c> while <c>F_PattonArmy_State ≠ 0</c>), ported one-to-one on
/// the nation template (<c>docs/strategy/survey-nations.md</c> §2.3-2.4): the scenes of the army's
/// story by its state (6: <c>Gﾊﾟｯﾄﾝ</c> 11, 5: <c>Gｱﾘｽﾄﾚｽ</c> 3, 3, 2, 1: <c>Gﾊﾟｯﾄﾝ</c> 9, 8, 7, each
/// tested after the one before), then one army — Patton (54), Hubert (55), Freak (56), Hanty (57),
/// and Aristoles (32) once his state is past 9; nobody left out — that marches with at least 500
/// troops, retreat mode 4, on the first of Sudori 17 (46), Sudori 13 (41), Pawn (40), Micrograd
/// (44), Lang Bau (45) the player holds. No draws. The army name (<c>MS 10</c>) is read from the
/// player's files through <paramref name="scenario"/>.
/// </summary>
public sealed class PattonTurn(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "Tﾊﾟｯﾄﾝ.ADV", scenario)
{
    /// <summary><c>Tﾊﾟｯﾄﾝ.ADV:1-68</c>, up to its <c>%0:</c>.</summary>
    public async Task RunAsync()
    {
        State.Ev_PhaseNation = 5;                                       // :1
        State.Bt_EnemyRetreatMode = 4;                                  // :2
        SetLiteral(4);                                                  // :3-4 MS 10, "Patton Army"
        State.Ev_WeeklyEventDone = 0;                                   // :5
        if (State.F_PattonArmy_State == 6)                              // :6-25 the army's story
            await Script.EventAsync("Gﾊﾟｯﾄﾝ.ADV", 11);
        if (State.F_PattonArmy_State == 5)
            await Script.EventAsync("Gｱﾘｽﾄﾚｽ.ADV", 3);
        if (State.F_PattonArmy_State == 3)
            await Script.EventAsync("Gﾊﾟｯﾄﾝ.ADV", 9);
        if (State.F_PattonArmy_State == 2)
            await Script.EventAsync("Gﾊﾟｯﾄﾝ.ADV", 8);
        if (State.F_PattonArmy_State == 1)
            await Script.EventAsync("Gﾊﾟｯﾄﾝ.ADV", 7);
        SetPriority(54, 55, 56, 57);                                    // :26-37
        if (State.F_Aristoles_Helman2ndState > 9)
            State.Tmp0005 = 32;
        PickArmy();                                                     // \L_001de
        SumTroops();                                                    // \L_00198
        if (State.CallArg < 500)
            return;                                                     // %0:
        await AttackFirstAsync(46, 41, 40, 44, 45);                     // :43-68
    }
}
