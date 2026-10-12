using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>TCﾘｰｻﾞｽ2.ADV</c>, the defenders of a rebel province the player attacks (part N1; called by
/// <c>Cﾃｷﾄｼ.ADV:318-320</c> before <c>FIGT_A</c>), ported one-to-one on the nation template
/// (<c>docs/strategy/survey-nations.md</c> §2.5): the rebel armies of <see cref="RebelTurn"/>
/// defend by province — B (1, 2, 6, 7) at 7, 8 and anywhere else, A (3, 5, 4) at 2-4, both with
/// retreat mode 4 when they muster 100 troops; C (1, 2, 3, 6, 4, 5, 7) at 5 (retreat mode 2) and
/// at 6, the only province with walls (retreat mode 1). No filter, no draws, no calls. The army
/// name (<c>MS 10</c>) is read from the player's files through <paramref name="scenario"/>.
/// </summary>
public sealed class RebelDefenders(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "TCﾘｰｻﾞｽ2.ADV", scenario)
{
    /// <summary><c>TCﾘｰｻﾞｽ2.ADV:1-73</c>.</summary>
    public Task RunAsync()
    {
        State.Ev_PhaseNation = 2;                                       // :1
        SetLiteral(3);                                                  // :2-3 MS 10, "Leazas Rebel Army"
        // QUIRK: NAT-16 — FIGT_A recomputes the limit from Bt_WallBonus (FIGT_A.ADV:31-55).
        State.Bt_TroopLimit = 5000;                                     // :4-9
        State.Bt_WallBonus = 0;
        if (State.Prov_Cur == 6)                                        // QUIRK: NAT-17 — only Oaks gets its walls
        {
            State.Bt_TroopLimit = Prov.BattleArea;
            State.Bt_WallBonus = Prov.Defense;
        }
        if (State.Prov_Cur == 8 || State.Prov_Cur == 7)                 // :10-18
            ArmyB();
        else if (State.Prov_Cur == 2 || State.Prov_Cur == 3 || State.Prov_Cur == 4)
            ArmyA();
        else if (State.Prov_Cur == 5 || State.Prov_Cur == 6)
            ArmyC();
        else
            ArmyB();                                                    // falls into L_000b5
        return Task.CompletedTask;
    }

    /// <summary><c>TCﾘｰｻﾞｽ2.ADV:19-35</c> (<c>L_000b5</c>) — army B (1, 2, 6, 7).</summary>
    private void ArmyB()
    {
        SetPriority(1, 2, 6, 7);
        PickArmy();                                                     // \L_001eb
        SumTroops();                                                    // \L_001a5
        if (State.CallArg < 100)
            return;                                                     // QUIRK: BFL-13 — @L_001a2, the retreat mode left as it was
        State.Bt_EnemyRetreatMode = 4;
    }

    /// <summary><c>TCﾘｰｻﾞｽ2.ADV:36-52</c> (<c>L_00100</c>) — army A (3, 5, 4).</summary>
    private void ArmyA()
    {
        SetPriority(3, 5, 4);
        PickArmy();                                                     // \L_001eb
        SumTroops();                                                    // \L_001a5
        if (State.CallArg < 100)
            return;                                                     // QUIRK: BFL-13 — @L_001a2, the retreat mode left as it was
        State.Bt_EnemyRetreatMode = 4;
    }

    /// <summary><c>TCﾘｰｻﾞｽ2.ADV:53-73</c> (<c>L_0014b</c>) — army C (1, 2, 3, 6, 4, 5, 7), without a troop sum.</summary>
    private void ArmyC()
    {
        SetPriority(1, 2, 3, 6, 4, 5, 7);
        PickArmy();                                                     // \L_001eb
        if (State.Prov_Cur == 5)
            State.Bt_EnemyRetreatMode = 2;
        else if (State.Prov_Cur == 6)
            State.Bt_EnemyRetreatMode = 1;
    }
}
