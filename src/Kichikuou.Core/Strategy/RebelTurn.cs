using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>TCﾘｰｻﾞｽ.ADV</c>, the Leazas rebels' weekly turn (story stage 1, part N1), ported one-to-one
/// on the nation template (<c>docs/strategy/survey-nations.md</c> §2.3-2.4): the first rebel turn
/// plays <c>Gｴｸｽ</c> 10, then three armies, each picked from its priority list (commanders with
/// fewer than 190 troops left out) and sent only with at least 500 troops, attack the first of
/// their target provinces the player holds through <c>FIGT_B</c> — army A (3, 5, 4) at 4, 3, 2 or 1
/// and then army B (1, 2, 6, 7) at 7, 8 or 1; army C (1, 2, 3, 6, 4, 5, 7) at 6 or 5 instead of
/// both once the player holds province 5. The army name (<c>MS 10</c>) is read from the player's
/// files through <paramref name="scenario"/>.
/// </summary>
public sealed class RebelTurn(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "TCﾘｰｻﾞｽ.ADV", scenario)
{
    /// <summary><c>TCﾘｰｻﾞｽ.ADV:1-107</c>, up to the <c>%0:</c> at <c>L_0024e</c>.</summary>
    public async Task RunAsync()
    {
        State.Ev_PhaseNation = 2;                                       // :1
        SetLiteral(3);                                                  // :2-3 MS 10, "Leazas Rebel Army"
        State.Bt_EnemyRetreatMode = 4;                                  // :4
        if (State.F_LeazasRebels_State == 1)                            // :5-9 the first rebel turn
        {
            State.F_LeazasRebels_State = 2;
            await Script.EventAsync("Gｴｸｽ.ADV", 10);
        }
        State.Prov_Cur = 5;                                             // :10-13
        if (Prov.Owner == 1)
        {
            await ArmyCAsync();                                         // @L_001ce
            return;
        }
        // QUIRK: NAT-06 — army A always goes on to army B, attack or not.
        await ArmyAAsync();                                             // :14-48, then L_00132
        if (await ArmyBAsync())                                         // :49-79
            return;                                                     // @L_0024e
        // QUIRK: NAT-06 — army B found no province of the player's, not even Leazas Castle (1),
        // whose loss ends the game: army C.
        await ArmyCAsync();                                             // :80-105
    }

    /// <summary>The filler's filter (<c>TCﾘｰｻﾞｽ.ADV:180-182</c>): commanders with fewer than 190 troops stay home, in every slot.</summary>
    protected override bool SkipsCommander() => Cmdr.Troops < 190;

    /// <summary><c>TCﾘｰｻﾞｽ.ADV:14-48</c> — army A (3, 5, 4) against 4, 3, 2, 1.</summary>
    private async Task ArmyAAsync()
    {
        State.Bt_EnemyRetreatMode = 4;
        SetPriority(3, 5, 4);
        PickArmy();                                                     // \L_00297
        SumTroops();                                                    // \L_00251
        if (State.CallArg < 500)
            return;                                                     // @L_00132
        await AttackFirstAsync(4, 3, 2, 1);
    }

    /// <summary>
    /// <c>TCﾘｰｻﾞｽ.ADV:49-79</c> (<c>L_00132</c>) — army B (1, 2, 6, 7) against 7, 8, 1; true when
    /// the turn ends here (too weak, or it attacked), false when it falls into army C.
    /// </summary>
    private async Task<bool> ArmyBAsync()
    {
        State.Bt_EnemyRetreatMode = 4;
        SetPriority(1, 2, 6, 7);
        PickArmy();                                                     // \L_00297
        SumTroops();                                                    // \L_00251
        if (State.CallArg < 500)
            return true;                                                // @L_0024e
        return await AttackFirstAsync(7, 8, 1);
    }

    /// <summary><c>TCﾘｰｻﾞｽ.ADV:80-105</c> (<c>L_001ce</c>) — army C (1, 2, 3, 6, 4, 5, 7) against 6, 5.</summary>
    private async Task ArmyCAsync()
    {
        State.Bt_EnemyRetreatMode = 4;
        SetPriority(1, 2, 3, 6, 4, 5, 7);
        PickArmy();                                                     // \L_00297
        SumTroops();                                                    // \L_00251
        if (State.CallArg < 500)
            return;                                                     // @L_0024e
        await AttackFirstAsync(6, 5);
    }
}
