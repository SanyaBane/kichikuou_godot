using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Tﾙﾄﾞﾗ.ADV</c>, the Creator's phase at story stage 4 (milestone 5 step 1; called by
/// <c>MAIN.ADV:846</c> at stage 4, after <c>MS 10, "Creator"</c>), ported one-to-one
/// (<c>docs/strategy/stage-4-sweep.md</c>, "The Creator phase"). It uses none of the nation
/// template's routines. The first phase plays <c>Gﾙﾄﾞﾗ</c> 1, which sets <c>N_Rudrasaum_Turns</c> to
/// 1; every phase counts it up by one, then searches seven times for a province of the player's:
/// Angel Knight (146) and Angel Knight (147), at full HP and troops, attack each of the first five
/// through <c>FIGT_B</c>, and an earthquake ruins each of the last two. A search that finds nothing
/// skips its attack or earthquake. The dead code is left out: the earthquake's jitter
/// <c>L_001d4</c> (<c>:83-104</c>, NAT-16) and the window helpers' unreachable ops (<c>:132-143</c>,
/// <c>:148-186</c>, <c>:203-205</c>, EVT-01).
/// </summary>
public sealed class CreatorTurn(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "Tﾙﾄﾞﾗ.ADV", scenario)
{
    /// <summary><c>Tﾙﾄﾞﾗ.ADV:1-21</c>, up to its <c>%0:</c>.</summary>
    public async Task RunAsync()
    {
        if (State.StoryStage == 4 && State.N_Rudrasaum_Turns == 0)      // :1-4 the first phase
            await Script.EventAsync("Gﾙﾄﾞﾗ.ADV", 1);
        // QUIRK: NAT-30 — Gﾙﾄﾞﾗ set 1, so the first phase counts 2.
        State.N_Rudrasaum_Turns = State.N_Rudrasaum_Turns + 1;          // :5
        State.Ev_PhaseNation = 10;                                      // :6
        for (int k = 0; k < 5; k++)                                     // :7-16
        {
            Search();                                                   // \L_00255
            await AttackAsync();                                        // \L_00093
        }
        for (int k = 0; k < 2; k++)                                     // :17-20
        {
            Search();
            await EarthquakeAsync();                                    // \L_000f8
        }
    }

    /// <summary>
    /// <c>Tﾙﾄﾞﾗ.ADV:105-131</c> (<c>L_00255</c>, <c>L_00259</c>) — the target into <c>Prov_Cur</c>, 0
    /// for none: <c>Tmp0001</c> counts the tries, and the 20th gives up, so at most 19 draws of
    /// 1-91. Nothing (75) gives up at once. With the counter past 5 a draw in two puts Leazas Castle
    /// (1) in its place; below 5 Leazas Castle (1), Siberia (38) and Ancient Ruins (35) are drawn
    /// again. A province of the player's is the target; any other is drawn again.
    /// </summary>
    private void Search()
    {
        State.Tmp0001 = 0;
        while (true)
        {
            State.Tmp0001 = State.Tmp0001 + 1;                          // :108-112
            if (State.Tmp0001 == 20)
            {
                State.Prov_Cur = 0;
                return;
            }
            Script.Rnd(91);                                             // :113-114
            State.Prov_Cur = State.RetVal;
            // QUIRK: NAT-29 — Nothing (75) ends the search: no retry.
            if (State.Prov_Cur == 75)                                   // :115-118
            {
                State.Prov_Cur = 0;
                return;
            }
            // QUIRK: NAT-30 — the castle roll from 6 on and the spared provinces below 5: 5 has neither.
            if (State.N_Rudrasaum_Turns > 5)                            // :119-124 the castle roll
            {
                Script.Rnd(2);
                if (State.RetVal == 1)
                    State.Prov_Cur = 1;
            }
            if (State.N_Rudrasaum_Turns < 5                             // :125-127 spared
                && (State.Prov_Cur == 1 || State.Prov_Cur == 38 || State.Prov_Cur == 35))
                continue;
            if (Prov.Owner == 1)                                        // :128-131
                return;
        }
    }

    /// <summary>
    /// <c>Tﾙﾄﾞﾗ.ADV:22-40</c> (<c>L_00093</c>) — unless the search found nothing, Angel Knight (146)
    /// and Angel Knight (147) attack <c>Prov_Cur</c>: each at status 0 with his HP and troops to
    /// their maximum, retreat mode 1, <c>FIGT_B</c>.
    /// </summary>
    private async Task AttackAsync()
    {
        if (State.Prov_Cur == 0)
            return;
        State.Bt_EnemyParty1 = 146;                                     // :26-29
        State.Bt_EnemyParty2 = 147;
        State.Bt_EnemyParty3 = 0;
        State.Bt_EnemyParty4 = 0;
        foreach (int knight in new[] { 146, 147 })                      // :30-37
        {
            State.Cmdr_Cur = (Sys3Int)knight;
            // QUIRK: NAT-11 — status 0 (dead) for a commander who fights; nothing reads it.
            Cmdr.Status = 0;
            Cmdr.HP = Cmdr.MaxHP;
            Cmdr.Troops = Cmdr.MaxTroops;
        }
        State.Bt_EnemyRetreatMode = 1;                                  // :38
        await Script.CallFileAsync("FIGT_B.ADV");                       // :39
    }

    /// <summary>
    /// <c>Tﾙﾄﾞﾗ.ADV:41-82</c> (<c>L_000f8</c>) — unless the search found nothing, the earthquake at
    /// <c>Prov_Cur</c>: the camera to it, its picture and "［name］" (key wait, the picture taken
    /// down), then its economy and walls to 0, its battle area doubled and its picture the ruin
    /// (1043), shown with "Suddenly, a great earthquake brought ［name］ to ruin." (key wait; that
    /// picture stays up).
    /// </summary>
    private async Task EarthquakeAsync()
    {
        if (State.Prov_Cur == 0)
            return;
        State.MapTargetX = Prov.MapX;                                   // :45-50
        State.MapTargetY = Prov.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);         // :51 MM 8, the province's name
        State.CallArg = Prov.BgCg;                                      // :52-61
        await WaoAsync(331);                                            // \L_003fb
        await SpeakAsync(113, 56);                                      // \L_00337, "［<province>］"
        await WaoAsync(332);                                            // \L_00409
        // QUIRK: NAT-16 — the four calls of the jitter L_001d4 (:62-65) return at once.
        Prov.Economy = 0;                                               // :66-69
        Prov.Defense = 0;
        Prov.BattleArea = Prov.BattleArea * 2;
        Prov.BgCg = 1043;
        State.CallArg = 1043;                                           // :70-81
        await WaoAsync(331);
        await SpeakAsync(113, 74);                                      // "Suddenly, a great earthquake brought <province> to ruin."
    }
}
