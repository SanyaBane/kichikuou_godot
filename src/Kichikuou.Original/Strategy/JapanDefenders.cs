using Kichikuou.Engine.Script;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>TJAPAN2.ADV</c>, the defenders of a JAPAN province the player attacks (part N2, milestone 3
/// step 5; called by <c>Cﾃｷﾄｼ.ADV:341-343</c> before <c>FIGT_A</c>, unless JAPAN has fallen), ported
/// one-to-one on the nation template (<c>docs/strategy/survey-nations.md</c> §2.5). Every defence
/// starts the JAPAN war (<c>F_Japan_State := 1</c>, also over the alliance states 2 and 3) and keeps
/// the province's walls. Nagasaki (55):
/// Katsuie (75), Harumi Ueda (76), Mori Harada (77), Denden Mori (78), retreat mode 1 unless they
/// muster fewer than 10 troops; Osaka (56): the whole army, Nobunaga Oda (72) first, retreat mode
/// 1; any other province: the strongest by troops of the whole army, the 75-78 list and One-Shot
/// Tarou (79), Noberu Maeda (80), Washi Tada (81), Isoroku Yamamoto (82) (the earlier on a tie),
/// retreat mode 4 when it musters 500. A too weak army leaves the retreat mode as the last battle
/// left it (BFL-13). No filter, no draws, no calls. The army name (<c>MS 10</c>) is read from the
/// player's files through <paramref name="scenario"/>.
/// </summary>
public sealed class JapanDefenders(OriginalScriptContext script, IScenarioSource? scenario = null)
    : NationFile(script, "TJAPAN2.ADV", scenario)
{
    /// <summary><c>TJAPAN2.ADV:1-130</c>.</summary>
    public Task RunAsync()
    {
        State.Ev_PhaseNation = 6;                                       // :1
        SetLiteral(3);                                                  // :2-3 MS 10, "JAPAN Army"
        State.F_Japan_State = 1;                                        // :4
        // QUIRK: NAT-16 — FIGT_A recomputes the limit from Bt_WallBonus (FIGT_A.ADV:31-55).
        State.Bt_TroopLimit = Prov.BattleArea;                          // :5-6
        State.Bt_WallBonus = Prov.Defense;
        if (State.Prov_Cur == 55)                                       // :7-13
            Nagasaki();
        else if (State.Prov_Cur == 56)
            Osaka();
        else
            Strongest();
        return Task.CompletedTask;
    }

    /// <summary>The whole army: Nobunaga Oda (72), Kenichi Oda (73), Yamato Akechi (74), Denden Mori (78), Mori Harada (77), Katsuie (75), Isoroku Yamamoto (82), One-Shot Tarou (79), Noberu Maeda (80).</summary>
    private void WholeArmy() => SetPriority(72, 73, 74, 78, 77, 75, 82, 79, 80);

    /// <summary>Katsuie (75), Harumi Ueda (76), Mori Harada (77), Denden Mori (78).</summary>
    private void FirstFour() => SetPriority(75, 76, 77, 78);

    /// <summary>One-Shot Tarou (79), Noberu Maeda (80), Washi Tada (81), Isoroku Yamamoto (82).</summary>
    private void LastFour() => SetPriority(79, 80, 81, 82);

    /// <summary><c>TJAPAN2.ADV:14-30</c> (<c>L_00078</c>) — Nagasaki (55).</summary>
    private void Nagasaki()
    {
        FirstFour();
        PickArmy();                                                     // \L_002ee
        SumTroops();                                                    // \L_002a8
        if (State.CallArg < 10)
            return;                                                     // QUIRK: BFL-13 — @L_002a5, the retreat mode left as it was
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary><c>TJAPAN2.ADV:31-44</c> (<c>L_000c6</c>) — Osaka (56), the troop sum unused.</summary>
    private void Osaka()
    {
        WholeArmy();
        PickArmy();                                                     // \L_002ee
        SumTroops();                                                    // \L_002a8
        State.Bt_EnemyRetreatMode = 1;
    }

    /// <summary>
    /// <c>TJAPAN2.ADV:45-128</c> (<c>L_00105</c>) — the three lists picked and summed in turn, the
    /// strongest (<c>Tmp0021</c> its troops, <c>Tmp0022</c> its number; a later one only when
    /// stronger) picked again.
    /// </summary>
    private void Strongest()
    {
        WholeArmy();                                                    // :46-56
        PickArmy();
        SumTroops();
        State.Tmp0021 = State.CallArg;
        State.Tmp0022 = 1;
        FirstFour();                                                    // :59-73
        PickArmy();
        SumTroops();
        if (State.Tmp0021 < State.CallArg)
        {
            State.Tmp0021 = State.CallArg;
            State.Tmp0022 = 2;
        }
        LastFour();                                                     // :74-88
        PickArmy();
        SumTroops();
        if (State.Tmp0021 < State.CallArg)
        {
            State.Tmp0021 = State.CallArg;
            State.Tmp0022 = 3;
        }
        if (State.Tmp0022 == 1)                                         // :89-121
            WholeArmy();
        if (State.Tmp0022 == 2)
            FirstFour();
        if (State.Tmp0022 == 3)
            LastFour();
        PickArmy();                                                     // :122-128
        SumTroops();
        if (State.CallArg < 500)
            return;                                                     // QUIRK: BFL-13 — the retreat mode left as it was
        State.Bt_EnemyRetreatMode = 4;
    }
}
