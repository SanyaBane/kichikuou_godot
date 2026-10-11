using System.Text;
using Kichikuou.Content.Scripts;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The hand-ported nation files (<see cref="RebelTurn"/>, <see cref="RebelDefenders"/>,
/// <see cref="HelmanTurn"/>, <see cref="HelmanDefenders"/>, <see cref="PattonTurn"/>, <see cref="ZethTurn"/>, <see cref="ZethDefenders"/>, <see cref="KayblisTurn"/>, <see cref="KayblisDefenders"/>, <see cref="JapanTurn"/>, <see cref="JapanDefenders"/>, on the template
/// <see cref="NationFile"/>) against the files
/// themselves run from the player's decompile by <see cref="SceneRunner"/>, on random states: both
/// runs must call the same files with the same <c>Ev_Entry</c> and the same party, draw the same
/// rolls, show the same events and leave the same memory. Every other file (<c>FIGT_B</c>, the scenes) is a hook that scribbles
/// over what a battle or a scene may change — the commanders, the owners, the cursors, scratch,
/// the retreat mode — and now and then ends the game by a jump. Skipped without the decompile.
/// </summary>
public class NationTurnDifferentialTests(ITestOutputHelper output)
{
    private const int Cases = 3000;

    [Fact]
    public Task The_rebel_turn_runs_exactly_as_TCﾘｰｻﾞｽ() =>
        CompareAsync(
            "TCﾘｰｻﾞｽ.ADV",
            (script, scenario) => new RebelTurn(script, scenario).RunAsync(),
            (state, rng) =>
            {
                state.F_LeazasRebels_State = (Sys3Int)rng.Next(0, 3);
                state.Prov_Cur = (Sys3Int)rng.Next(0, 101);
            },
            (before, after, _, log) => RebelTurnFeatures(before, after, log),
            ["call Gｴｸｽ.ADV", "A at 4", "A at 3", "A at 2", "A at 1", "B at 7", "B at 8", "B at 1", "C at 6", "C at 5",
             "C after B", "two attacks", "no attack", "jump", "record 0 picked"]);

    [Fact]
    public Task The_rebel_defenders_are_picked_exactly_as_TCﾘｰｻﾞｽ2() =>
        CompareAsync(
            "TCﾘｰｻﾞｽ2.ADV",
            (script, scenario) => new RebelDefenders(script, scenario).RunAsync(),
            (state, rng) => state.Prov_Cur = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 101) : rng.Next(2, 9)),
            (before, after, _, log) => RebelDefenderFeatures(before, after, log),
            ["B at 7", "B at 8", "B elsewhere", "A at 2", "A at 3", "A at 4", "C at 5", "C at 6", "stale B", "stale A",
             "empty party", "full party", "walls"]);

    [Fact]
    public Task The_Helman_defenders_are_picked_exactly_as_Tﾍﾙﾏﾝ2() =>
        CompareAsync(
            "Tﾍﾙﾏﾝ2.ADV",
            (script, scenario) => new HelmanDefenders(script, scenario).RunAsync(),
            AimAtHelman,
            (before, after, _, log) => HelmanDefenderFeatures(before, after, log),
            ["call Gｽﾃｯｾﾙ.ADV", "call Gｼｰﾗ.ADV 1", "call Gｼｰﾗ.ADV 2", "called off", "call Gﾈﾛ.ADV", "call Gﾊﾟｯﾄﾝ.ADV",
             "call Gﾙｰﾍﾞﾗﾝ.ADV", "1st army", "1st army, first order", "Laumiere alone", "2nd army", "3rd army",
             "3rd army with militia", "4th army", "5th army", "Lelyukov alone", "Ruberan", "militia", "leader dead",
             "Mineva met", "pass against one unit", "filtered"],
            ScribbleHelman);

    [Fact]
    public Task The_Helman_turn_runs_exactly_as_Tﾍﾙﾏﾝ() =>
        CompareAsync(
            "Tﾍﾙﾏﾝ.ADV",
            (script, scenario) => new HelmanTurn(script, scenario).RunAsync(),
            AimAtHelmanTurn,
            HelmanTurnFeatures,
            [
                "call Gｽﾃｯｾﾙ.ADV 3", "call Gｽﾃｯｾﾙ.ADV 4", "call Gｽﾃｯｾﾙ.ADV 5", "call Gｽﾃｯｾﾙ.ADV 6", "call Gｽﾃｯｾﾙ.ADV 14",
                "call Gﾌﾚｲｱ.ADV 1", "call Gﾚﾘｭｰｺﾌ.ADV 1", "call Gﾚﾘｭｰｺﾌ.ADV 2", "call Gﾚﾘｭｰｺﾌ.ADV 5",
                "call Gｱﾘｽﾄﾚｽ.ADV 1", "call Gｱﾘｽﾄﾚｽ.ADV 5", "call Gﾊﾟｯﾄﾝ.ADV 2", "call Gﾊﾟｯﾄﾝ.ADV 12",
                "call Gﾐﾈﾊﾞ.ADV 1", "call Gﾐﾈﾊﾞ.ADV 4", "call Gﾈﾛ.ADV 2", "call Gﾛﾚｯｸｽ.ADV 1", "call Gﾛﾚｯｸｽ.ADV 2",
                "call Gｵｰﾙﾊｳﾝﾄ.ADV 1",
                .. HelmanAttacks,
                "capture", "capture at 13", "capture at 2", "1st army, first order", "1st army, second order",
                "two attacks", "three attacks", "no attack", "jump", "Tainyar revived", "Rolex 0 to 1", "Rolex 1 to 2",
                "Rolex 2 at Rosesgrad", "Nero counted up", "Aristoles 1 to 2", "Aristoles 2 to 3", "filtered",
            ],
            ScribbleHelmanTurn,
            cases: 4000);

    [Fact]
    public Task The_Patton_turn_runs_exactly_as_Tﾊﾟｯﾄﾝ() =>
        CompareAsync(
            "Tﾊﾟｯﾄﾝ.ADV",
            (script, scenario) => new PattonTurn(script, scenario).RunAsync(),
            AimAtPatton,
            PattonFeatures,
            ["call Gﾊﾟｯﾄﾝ.ADV 11", "call Gｱﾘｽﾄﾚｽ.ADV 3", "call Gﾊﾟｯﾄﾝ.ADV 9", "call Gﾊﾟｯﾄﾝ.ADV 8", "call Gﾊﾟｯﾄﾝ.ADV 7",
             "two scenes", "at 46", "at 41", "at 40", "at 44", "at 45", "with Aristoles", "full party", "too weak",
             "no target", "jump"],
            ScribblePatton);

    /// <summary>
    /// Patton's army state at its tested values and around them, Aristoles's state below and above
    /// 9, Patton (54), Hubert (55), Freak (56), Hanty (57) and Aristoles (32) alive or not with
    /// troops around the army's 500, and its five targets held by the player at the case's density.
    /// </summary>
    private static void AimAtPatton(GameState state, Random rng)
    {
        state.F_PattonArmy_State = (Sys3Int)rng.Next(0, 8);
        state.F_Aristoles_Helman2ndState = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 10) : rng.Next(10, 21));
        foreach (int c in PattonArmy)
        {
            state.Cmdrs[c].Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
            state.Cmdrs[c].Troops = (Sys3Int)(rng.Next(10) == 0 ? rng.Next(60000, 65536) : rng.Next(0, 300));
        }
        double mine = rng.NextDouble() * 0.6;
        foreach (int p in PattonTargets)
            state.Provs[p].Owner = (Sys3Int)(rng.NextDouble() < mine ? 1 : rng.Next(3) == 0 ? rng.Next(0, 12) : 5);
    }

    private static readonly int[] PattonArmy = [54, 55, 56, 57, 32];

    private static readonly int[] PattonTargets = [46, 41, 40, 44, 45];

    /// <summary>What Patton's scenes and battles may change: the two states, the army, its targets, the army name.</summary>
    private static void ScribblePatton(GameState state, Random rng)
    {
        if (rng.Next(3) == 0)
            state.F_PattonArmy_State = (Sys3Int)rng.Next(0, 8);
        if (rng.Next(4) == 0)
            state.F_Aristoles_Helman2ndState = (Sys3Int)rng.Next(0, 21);
        foreach (int c in PattonArmy)
        {
            if (rng.Next(6) == 0)
                state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 2);
            if (rng.Next(6) == 0)
                state.Cmdrs[c].Troops = (Sys3Int)rng.Next(0, 300);
        }
        foreach (int p in PattonTargets)
            if (rng.Next(8) == 0)
                state.Provs[p].Owner = (Sys3Int)(rng.Next(2) == 0 ? 1 : 5);
        if (rng.Next(6) == 0)
            state.Strings[10] = "scribbled";
    }

    private static IEnumerable<string> PattonFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        var calls = log.Where(e => e.StartsWith("call ", StringComparison.Ordinal)).Select(e => e.Split(' ')).ToList();
        var scenes = calls.Where(c => c[1] != "FIGT_B.ADV").ToList();
        foreach (string[] scene in scenes)
            yield return $"call {scene[1]} {scene[3]}";
        if (scenes.Count >= 2)
            yield return "two scenes";
        var battle = log.FirstOrDefault(e => e.StartsWith("call FIGT_B.ADV", StringComparison.Ordinal));
        if (battle is not null)
        {
            string[] parts = battle.Split(' ');
            yield return $"at {parts[Array.IndexOf(parts, "Prov_Cur") + 1]}";
            int[] party = [.. parts[Array.IndexOf(parts, "Party") + 1].Split(',').Select(int.Parse)];
            if (party.Contains(32))
                yield return "with Aristoles";
            if (party[3] != 0)
                yield return "full party";
        }
        else if (!log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return after.CallArg < 500 ? "too weak" : "no target";
        if (log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return "jump";
    }

    [Fact]
    public Task The_Zeth_defenders_are_picked_exactly_as_Tｾﾞｽ2() =>
        CompareAsync(
            "Tｾﾞｽ2.ADV",
            (script, scenario) => new ZethDefenders(script, scenario).RunAsync(),
            AimAtZeth,
            ZethDefenderFeatures,
            ["call Iｾﾞｽ.ADV 1", "call Gﾏｼﾞｯｸ.ADV 1", "call Gﾅｷﾞ.ADV 5", "call Gﾁｽﾞｺ.ADV 1", "call Gﾊﾟﾊﾟｲｱ.ADV 3",
             "Magic's army", "Alex's army at Magic's Tower", "Nagi's army", "Chizuko's army", "Papaya's army",
             "113's army", "117's army", "121's army", "125's army",
             "113 with the Walls", "117 with the Walls", "121 with the Walls", "125 with the Walls",
             "sent back at 66", "sent back at 67", "sent back at 68", "sent back at 70", "the Blade", "barrier down",
             "garrison", "Mage Leader alone", "garrison weakened", "Parapara Fortress", "Papaya's trap"],
            ScribbleZeth);

    /// <summary>Zeth's ministers (101-112 with their officers) and generals (113-128 with theirs).</summary>
    private static readonly int[] ZethLeaders = [.. Enumerable.Range(101, 28)];

    /// <summary>
    /// A Zeth province, mostly a general's city or a minister's tower, now and then attacked again
    /// (the garrison's draws), with walls and battle area; the garrison Wall (94), Wall (95), Mage
    /// Leader (96) with random maxima; Zeth's leaders alive or not (a general alone now and then);
    /// the player's party of up to four units, one of them holding the Barrier Blade (item 12) now
    /// and then; the barrier, Papaya's state and the war at their tested values.
    /// </summary>
    private static void AimAtZeth(GameState state, Random rng)
    {
        int[] provinces = [16, 58, 61, 63, 65, 66, 66, 67, 67, 68, 68, 70, 70, 59, 60, 62, 69];
        state.Prov_Cur = (Sys3Int)(rng.Next(8) == 0 ? rng.Next(0, 101) : provinces[rng.Next(provinces.Length)]);
        state.Provs[state.Prov_Cur].BattleArea = (Sys3Int)rng.Next(0, 1000);
        state.Provs[state.Prov_Cur].Defense = (Sys3Int)rng.Next(0, 30);
        state.Bt_GarrisonProv = (Sys3Int)(rng.Next(3) == 0 ? state.Prov_Cur.Value : rng.Next(0, 101));
        state.Bt_GarrisonHits = (Sys3Int)rng.Next(0, 5);
        foreach (int c in new[] { 94, 95, 96 })
        {
            state.Cmdrs[c].MaxTroops = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 2) : rng.Next(0, 3000));
            state.Cmdrs[c].MaxHP = (Sys3Int)rng.Next(0, 500);
        }
        bool alone = rng.Next(4) == 0;
        foreach (int c in ZethLeaders)
            state.Cmdrs[c].Status = (Sys3Int)(alone && (c - 113) % 4 != 0 || rng.Next(5) == 0 ? 0 : 1);
        state.F_Papaya_State = (Sys3Int)rng.Next(0, 4);
        state.F_Zeth_War = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
        state.F_ZethBarrier_State = (Sys3Int)rng.Next(0, 3);
        int units = rng.Next(0, 5);
        Sys3Int[] party = [.. Enumerable.Range(0, 4).Select(k => (Sys3Int)(k < units ? rng.Next(1, 51) : 0))];
        state.Bt_PlayerParty1 = party[0];
        state.Bt_PlayerParty2 = party[1];
        state.Bt_PlayerParty3 = party[2];
        state.Bt_PlayerParty4 = party[3];
        foreach (var unit in party)
            state.Units[unit].Item = (Sys3Int)(rng.Next(4) == 0 ? 12 : rng.Next(0, 20));
    }

    /// <summary>What Zeth's scenes may change: <c>Gﾏｼﾞｯｸ</c> 1's answer in <c>Tmp0001</c>, the leaders, the barrier, Papaya's state, the party.</summary>
    private static void ScribbleZeth(GameState state, Random rng)
    {
        if (rng.Next(2) == 0)
            state.Tmp0001 = (Sys3Int)rng.Next(0, 2);
        foreach (int c in ZethLeaders)
            if (rng.Next(10) == 0)
                state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(6) == 0)
            state.F_ZethBarrier_State = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(6) == 0)
            state.F_Papaya_State = (Sys3Int)rng.Next(0, 4);
        if (rng.Next(6) == 0)
            state.Bt_PlayerParty2 = (Sys3Int)rng.Next(0, 51);
        if (rng.Next(6) == 0)
            state.Strings[10] = "scribbled";
    }

    private static IEnumerable<string> ZethDefenderFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        foreach (string[] call in log.Where(e => e.StartsWith("call ", StringComparison.Ordinal)).Select(e => e.Split(' ')))
            yield return $"call {call[1]} {call[3]}";
        int prov = after.Prov_Cur.Value;
        int[] party = [after.Bt_EnemyParty1.Value, after.Bt_EnemyParty2.Value, after.Bt_EnemyParty3.Value, after.Bt_EnemyParty4.Value];
        bool tower = prov is 66 or 67 or 68 or 70;
        if (log.Any(e => e.StartsWith("rnd ", StringComparison.Ordinal)))
            yield return "garrison weakened";
        if (before.Prov_Cur == 16 && after.Bt_TroopLimit == 5000 && after.Bt_WallBonus == 0)
            yield return "Parapara Fortress";
        if (before.F_Papaya_State == 1 && first.F_Papaya_State == 2)
            yield return "Papaya's trap";
        if (tower && after.Bt_PlayerParty1 == 0 && before.Bt_PlayerParty1 != 0)
        {
            yield return $"sent back at {prov}";
            yield break;
        }
        if (tower && after.F_ZethBarrier_State != 2 && log.All(e => !e.StartsWith("call Iｾﾞｽ", StringComparison.Ordinal)))
            yield return "the Blade";
        if (tower && after.F_ZethBarrier_State == 2)
            yield return "barrier down";
        if (party is [94, 95, 96, 0])
            yield return "garrison";
        else if (party is [96, 0, 0, 0] && prov == 66)
            yield return "Mage Leader alone";
        else if (after.Tmp0001 == 94 && after.Tmp0002 == 95 && after.Tmp0003.Value is 113 or 117 or 121 or 125)
            yield return $"{after.Tmp0003.Value} with the Walls";
        else if (after.Tmp0004.Value is 113 or 117 or 121 or 125 && after.Tmp0005 == 0)
            yield return prov == 66 ? "Alex's army at Magic's Tower" : $"{after.Tmp0004.Value}'s army";
        else
            yield return after.Tmp0003.Value switch
            {
                110 => "Magic's army",
                107 => "Nagi's army",
                101 => "Chizuko's army",
                104 => "Papaya's army",
                _ => "?",
            };
    }

    [Fact]
    public Task The_Zeth_turn_runs_exactly_as_Tｾﾞｽ() =>
        CompareAsync(
            "Tｾﾞｽ.ADV",
            (script, scenario) => new ZethTurn(script, scenario).RunAsync(),
            AimAtZethTurn,
            ZethTurnFeatures,
            [
                "call Gﾊﾟﾊﾟｲｱ.ADV 1", "call Gﾅｷﾞ.ADV 3", "call Gﾅｷﾞ.ADV 2", "call Gﾅｷﾞ.ADV 1",
                .. new[] { 2, 3, 4, 5, 6, 30, 31, 32, 33, 34, 35, 36 }.Select(e => $"call Iｾﾞｽ.ADV {e}"),
                "call Gﾏｼﾞｯｸ.ADV 3", "call Gﾁｽﾞｺ.ADV 6", "call Gﾊﾟﾊﾟｲｱ.ADV 9", "call Gﾅｷﾞ.ADV 22", "call Gﾏｼﾞｯｸ.ADV 24",
                "call Gﾊﾟﾊﾟｲｱ.ADV 8", "call Gﾁｽﾞｺ.ADV 4", "call Gﾁｽﾞｺ.ADV 5", "call Gﾅｷﾞ.ADV 21", "call Gﾏｼﾞｯｸ.ADV 21",
                "call Gﾏｼﾞｯｸ.ADV 22",
                .. ZethAttacks,
                "state 2: Saias alone", "state 2: Saias dead", "no scene the week Iｾﾞｽ 2 played", "6 without Alex",
                "officer revived", "general too weak", "fell into Chizuko's army",
                "no attack", "one attack", "two attacks", "three attacks", "jump",
            ],
            ScribbleZethTurn,
            cases: 4000);

    /// <summary>The generals with the first commander of their armies: Saias (113) … Alex (125), each with his three officers after him.</summary>
    private static readonly (string Name, int Leader)[] ZethGenerals = [("Saias", 113), ("Uspira", 117), ("Kabachahn", 121), ("Alex", 125)];

    /// <summary>The ministers' armies (four commanders each from the first) and their targets, in the file's order.</summary>
    private static readonly (string Name, int First, int[] Targets)[] ZethMinisterArmies =
    [
        ("Chizuko", 165, [70, 69, 67, 62, 59, 58]),
        ("Papaya", 169, [66, 74, 76, 65, 63, 64]),
        ("Nagi", 173, [66, 69, 62, 59, 58, 16]),
        ("Magic", 177, [67, 63, 61, 60, 59, 58]),
    ];

    /// <summary>The generals' march, Chizuko's Tower (68) to Leazas Castle (1).</summary>
    private static readonly int[] ZethGeneralsTargets = [68, 70, 66, 67, 62, 63, 61, 60, 59, 58, 16, 15, 13, 2, 1];

    /// <summary>Every attack <c>Tｾﾞｽ</c> can make: each general's army, the generals' march at each province, each minister's army at each.</summary>
    private static readonly string[] ZethAttacks =
    [
        .. ZethGenerals.Select(g => $"{g.Name}'s army"),
        .. ZethGeneralsTargets.Select(p => $"generals at {p}"),
        .. ZethMinisterArmies.SelectMany(m => m.Targets.Select(p => $"{m.Name}'s army at {p}")),
    ];

    /// <summary>The provinces <c>Tｾﾞｽ</c> tests: the generals' march and the ministers' targets.</summary>
    private static readonly int[] ZethTurnProvinces = [1, 2, 13, 15, 16, 58, 59, 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 74, 76];

    /// <summary>The ministers, the generals and their officers (101-128), the ministers' armies (165-180).</summary>
    private static readonly int[] ZethTurnCommanders = [.. Enumerable.Range(101, 28), .. Enumerable.Range(165, 16)];

    private static Sys3Int ZethState(Random rng) =>
        (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 13) : new[] { 1, 2, 4, 5, 8, 9, 10, 11 }[rng.Next(8)]);

    /// <summary>
    /// The ritual, the war's state, Magic's and Papaya's at their tested values and around them;
    /// Zeth's commanders alive or not, with troops (now and then nobody has any: the generals'
    /// march stays home) and maxima for a revival; the provinces of the lists held by the player at
    /// a density of the case's own, now and then none of the generals' targets but Zeth Palace (69)
    /// (the march falls into Chizuko's army).
    /// </summary>
    private static void AimAtZethTurn(GameState state, Random rng)
    {
        state.F_RagarlRitual_State = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(1, 6) : rng.Next(6) == 0 ? rng.Next(6, 8) : 0);
        state.F_Zeth_State = ZethState(rng);
        state.F_Magic_State = (Sys3Int)rng.Next(0, 3);
        state.F_Papaya_State = (Sys3Int)rng.Next(0, 3);
        bool weak = rng.Next(8) == 0;
        foreach (int c in ZethTurnCommanders)
        {
            var cmdr = state.Cmdrs[c];
            cmdr.Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
            cmdr.Troops = (Sys3Int)(weak ? 0 : rng.Next(10) == 0 ? rng.Next(60000, 65536) : rng.Next(0, 1500));
            cmdr.MaxHP = (Sys3Int)rng.Next(0, 500);
            cmdr.MaxTroops = (Sys3Int)(weak ? rng.Next(0, 8) : rng.Next(0, 3000));
        }
        if (weak)
            state.Cmdrs[0].Troops = 0;
        double mine = rng.Next(8) == 0 ? 0 : rng.NextDouble() * 0.4;
        foreach (int p in ZethTurnProvinces)
            state.Provs[p].Owner = (Sys3Int)(rng.NextDouble() < mine ? 1 : rng.Next(4) switch { 0 => 8, 1 => rng.Next(0, 12), _ => 7 });
        if (rng.Next(8) == 0)
        {
            foreach (int p in ZethGeneralsTargets)
                if (state.Provs[p].Owner == 1)
                    state.Provs[p].Owner = 7;
            state.Provs[69].Owner = 1;
        }
    }

    /// <summary>
    /// What Zeth's scenes and battles may change: the war's state (<c>Iｾﾞｽ</c> 2 moves it to 2,
    /// <c>Iｾﾞｽ.ADV:2900</c>: an <c>Ev_Entry</c> of 2 does so on one call in two), the ritual,
    /// Magic's and Papaya's states, the commanders, the provinces and the army name.
    /// </summary>
    private static void ScribbleZethTurn(GameState state, Random rng)
    {
        if (state.Ev_Entry == 2 && rng.Next(2) == 0)
            state.F_Zeth_State = 2;
        else if (rng.Next(5) == 0)
            state.F_Zeth_State = ZethState(rng);
        if (rng.Next(6) == 0)
            state.F_RagarlRitual_State = (Sys3Int)rng.Next(0, 8);
        if (rng.Next(6) == 0)
            state.F_Magic_State = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(6) == 0)
            state.F_Papaya_State = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(6) == 0)
            state.Strings[10] = "scribbled";
        foreach (int c in ZethTurnCommanders)
        {
            if (rng.Next(10) == 0)
                state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 2);
            if (rng.Next(10) == 0)
                state.Cmdrs[c].Troops = (Sys3Int)rng.Next(0, 1500);
        }
        foreach (int p in ZethTurnProvinces)
            if (rng.Next(10) == 0)
                state.Provs[p].Owner = (Sys3Int)(rng.Next(2) == 0 ? 1 : 7);
    }

    private static IEnumerable<string> ZethTurnFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        int attacks = 0;
        foreach (string call in log.Where(e => e.StartsWith("call ", StringComparison.Ordinal)))
        {
            string[] parts = call.Split(' ');
            if (parts[1] != "FIGT_B.ADV")
            {
                yield return $"call {parts[1]} {parts[3]}";
                continue;
            }
            attacks++;
            string prov = Field(call, "Prov_Cur");
            int[] party = [.. Field(call, "Party").Split(',').Select(int.Parse)];
            var general = ZethGenerals.Where(g => party.Any(c => c >= g.Leader && c < g.Leader + 4)).Select(g => g.Name).FirstOrDefault();
            var minister = ZethMinisterArmies.Where(m => party.Any(c => c >= m.First && c < m.First + 4)).Select(m => m.Name).FirstOrDefault();
            if (general is not null)
            {
                yield return $"{general}'s army";
                yield return $"generals at {prov}";
            }
            else
                yield return $"{minister ?? "?"}'s army at {prov}";
        }
        yield return attacks switch { 0 => "no attack", 1 => "one attack", 2 => "two attacks", 3 => "three attacks", _ => "four attacks or more" };
        if (log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return "jump";

        var draws = log.Where(e => e.StartsWith("rnd ", StringComparison.Ordinal)).ToList();
        if (draws.Count > 0 && draws[0].StartsWith("rnd 3 ", StringComparison.Ordinal))
            yield return "state 2: Saias alone";
        if (draws.Count == 0 && !log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return "state 2: Saias dead";
        if (draws.Count > 0 && draws[0].StartsWith("rnd 7 ", StringComparison.Ordinal)
            && log.Any(e => e.StartsWith("call Iｾﾞｽ.ADV entry 2 ", StringComparison.Ordinal)))
            yield return "no scene the week Iｾﾞｽ 2 played";
        int scene = log.FindIndex(e => e.StartsWith("rnd 7 ", StringComparison.Ordinal));
        if (scene >= 0 && log[scene] == "rnd 7 6" && !log.Take(scene).Any(e => e.StartsWith("call ", StringComparison.Ordinal))
            && before.Cmdrs[113].Status != 0 && before.Cmdrs[121].Status != 0 && before.Cmdrs[125].Status == 0)
            yield return "6 without Alex";
        for (int i = 0; i + 1 < log.Count; i++)
            if (log[i].StartsWith("rnd 3 ", StringComparison.Ordinal) && log[i + 1].StartsWith("call Gﾁｽﾞｺ.ADV entry 6 ", StringComparison.Ordinal))
                yield return "fell into Chizuko's army";
        if (log.Count >= 2 && log[^2].StartsWith("rnd 3 ", StringComparison.Ordinal) && log[^1] == "return" && after.CallArg == 0)
            yield return "general too weak";
        // What the file changed before its first call (or before its end).
        foreach (var (_, leader) in ZethGenerals)
            for (int c = leader + 1; c < leader + 4; c++)
                if (before.Cmdrs[c].Status == 0 && first.Cmdrs[c].Status == 1 && first.Cmdrs[c].Troops == before.Cmdrs[c].MaxTroops / 8)
                    yield return "officer revived";

        static string Field(string call, string name)
        {
            int at = call.IndexOf($" {name} ", StringComparison.Ordinal) + name.Length + 2;
            return call[at..].Split(' ')[0];
        }
    }

    [Fact]
    public Task The_Kayblis_defenders_are_picked_exactly_as_Tｹｲﾌﾞﾘｽ2() =>
        CompareAsync(
            "Tｹｲﾌﾞﾘｽ2.ADV",
            (script, scenario) => new KayblisDefenders(script, scenario).RunAsync(),
            AimAtKayblis,
            KayblisDefenderFeatures,
            ["call Gﾜｰｸﾞ.ADV 1", "call Gｹｲﾌﾞﾘｽ.ADV 4", "plains, two", "plains, four", "91 in the plains",
             "gate at 78", "gate at 89", "gate at 90", "gate at 91", "gate in another stage",
             "Kaybwan", "Kaybnyan", "Kesselring", "Camilla", "Camilla raised", "Kayblis", "Medusa", "Monster Generals",
             "Monster Generals with walls", "attacked again", "a general without a draw"],
            ScribbleKayblis);

    [Fact]
    public Task The_JAPAN_defenders_are_picked_exactly_as_TJAPAN2() =>
        CompareAsync(
            "TJAPAN2.ADV",
            (script, scenario) => new JapanDefenders(script, scenario).RunAsync(),
            AimAtJapan,
            JapanDefenderFeatures,
            ["Nagasaki", "Nagasaki too weak", "Osaka", "strongest 1", "strongest 2", "strongest 3", "too weak",
             "retreat mode 4", "war over an alliance"]);

    [Fact]
    public Task The_JAPAN_turn_runs_exactly_as_TJAPAN() =>
        CompareAsync(
            "TJAPAN.ADV",
            (script, scenario) => new JapanTurn(script, scenario).RunAsync(),
            AimAtJapanTurn,
            JapanTurnFeatures,
            [
                "call Gｺｳﾋﾒ.ADV 1", "call Gｺｳﾋﾒ.ADV 2", "call Gｺｳﾋﾒ.ADV 3", "call Gｺｳﾋﾒ.ADV 4", "call IJAPAN.ADV 7",
                "call IJAPAN.ADV 8", "quiet week", "army 1", "army 2", "army 3", "army 4", "army 5", "no army", "too weak",
                .. JapanTurnTargets.Select(p => $"attack at {p}"), .. new[] { 27, 26, 24, 23 }.Select(p => $"taken {p}"),
                "M Land passed by", "the walk ran out", "a weak commander left out",
                "Rance fought: draw in three", "Gekkou: half the gold", "Gekkou: a quarter", "Gekkou: nothing stolen",
                "Gekkou: nobody found", .. GekkouTargets.SelectMany(u => new[] { $"Gekkou kills unit {u}", $"Gekkou fails at unit {u}" }),
                "Shinobu fails", "Shinobu: ending 4", "Shinobu caught, Leila alive", "Shinobu caught, Leila dead",
                "army after an attempt",
            ],
            ScribbleJapanTurn);

    /// <summary>The units Gekkou's draw in six picks (<c>TJAPAN.ADV:490-514</c>): Barres (6), Rick (7), Cordoba (8), Ex (14), Eleanor (23), Gengorou (15).</summary>
    private static readonly int[] GekkouTargets = [6, 7, 8, 14, 23, 15];

    /// <summary>The provinces <c>TJAPAN</c>'s walk tests, in order: Nagasaki (55) … Leazas Castle (1).</summary>
    private static readonly int[] JapanTurnTargets = [55, 28, 27, 25, 26, 24, 23, 6, 3, 2, 5, 4, 1];

    /// <summary>
    /// Gekkou's attempts under way (1-4) in three cases of four, else not (0 or 5); Rance (unit 1),
    /// Leila (5) and Gekkou's targets dead, idle or fought this week (status 0-2); Kouhime's state;
    /// JAPAN's commanders (Nobunaga Oda (72) to Isoroku Yamamoto (82)) alive or not with troops
    /// around 190 each and 500 in all; the walk's provinces held by the player at a density of the
    /// case's own, by the independent cities or by others.
    /// </summary>
    private static void AimAtJapanTurn(GameState state, Random rng)
    {
        state.N_Gekkou_Attempts = (Sys3Int)(rng.Next(4) == 0 ? (rng.Next(2) == 0 ? 0 : 5) : rng.Next(1, 5));
        foreach (int u in new[] { 1, 5 }.Concat(GekkouTargets))
            state.Units[u].Status = (Sys3Int)rng.Next(0, 3);
        state.F_Kou_State = (Sys3Int)rng.Next(0, 3);
        for (int c = 72; c <= 82; c++)
        {
            state.Cmdrs[c].Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
            state.Cmdrs[c].Troops = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 190) : rng.Next(0, 400));
        }
        double held = rng.NextDouble() * 0.4;
        foreach (int p in JapanTurnTargets)
            state.Provs[p].Owner = (Sys3Int)(rng.NextDouble() < held ? 1 : rng.Next(3) == 0 ? 3 : rng.Next(4, 12));
    }

    /// <summary>What the scenes may change: Kouhime's state, the week's event flag, JAPAN's commanders, the walk's owners.</summary>
    private static void ScribbleJapanTurn(GameState state, Random rng)
    {
        if (rng.Next(4) == 0)
            state.F_Kou_State = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(6) == 0)
            state.Ev_WeeklyEventDone = (Sys3Int)rng.Next(0, 2);
        for (int c = 72; c <= 82; c++)
            if (rng.Next(10) == 0)
                state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(8) == 0)
            state.Provs[JapanTurnTargets[rng.Next(JapanTurnTargets.Length)]].Owner = (Sys3Int)rng.Next(0, 4);
    }

    private static IEnumerable<string> JapanTurnFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        int attacked = 0;
        foreach (string call in log.Where(e => e.StartsWith("call ", StringComparison.Ordinal)))
        {
            string[] parts = call.Split(' ');
            if (parts[1] != "FIGT_B.ADV")
            {
                yield return $"call {parts[1]} {parts[3]}";
                continue;
            }
            attacked = int.Parse(Field(call, "Prov_Cur"));
            yield return $"attack at {attacked}";
        }
        var draws = log.Where(e => e.StartsWith("rnd ", StringComparison.Ordinal)).ToList();
        int k = 0;                                                      // the attempt's draws
        if (before.N_Gekkou_Attempts.Value is >= 1 and <= 4)
        {
            k = before.Units[1].Status == 2 ? 2 : 1;
            if (k == 2)
                yield return "Rance fought: draw in three";
            if (draws[k - 1] == $"rnd {k + 1} 2")
            {
                if (log.Contains("jump OVER.ADV") && after.EndingId == 4)
                {
                    yield return "Shinobu: ending 4";
                    yield break;
                }
                yield return after.N_Gekkou_Attempts == 5
                    ? before.Units[5].Status == 0 ? "Shinobu caught, Leila dead" : "Shinobu caught, Leila alive"
                    : "Shinobu fails";
            }
            else if (draws[k++] == "rnd 2 1")
            {
                int pick = int.Parse(draws[k++].Split(' ')[2]);
                int unit = pick <= 6 ? GekkouTargets[pick - 1] : 0;
                yield return unit == 0 || before.Units[unit].Status == 0 ? "Gekkou: nobody found"
                    : before.Units[unit].Status == 2 ? $"Gekkou kills unit {unit}" : $"Gekkou fails at unit {unit}";
            }
            else
                yield return draws[k++] switch
                {
                    "rnd 3 1" => "Gekkou: half the gold",
                    "rnd 3 2" => "Gekkou: a quarter",
                    _ => "Gekkou: nothing stolen",
                };
        }
        if (draws.Count >= k + 3 && draws[k + 2] == "rnd 4 1")
        {
            yield return "quiet week";
            yield break;
        }
        if (draws.Count < k + 4)
            yield break;
        yield return $"army {draws[k + 3].Split(' ')[2]}";
        if (k > 0)
            yield return "army after an attempt";
        if (after.Bt_EnemyParty1 == 0)
        {
            yield return "no army";
            yield break;
        }
        int[] party = [after.Bt_EnemyParty1.Value, after.Bt_EnemyParty2.Value, after.Bt_EnemyParty3.Value, after.Bt_EnemyParty4.Value];
        int[] list = [after.Tmp0001.Value, after.Tmp0002.Value, after.Tmp0003.Value, after.Tmp0004.Value];
        if (party[3] == 0 && list.Any(c => c != 0 && !party.Contains(c) && after.Cmdrs[c].Status != 0 && after.Cmdrs[c].Troops < 190))
            yield return "a weak commander left out";
        int army = log.FindIndex(e => e == draws[k + 3]);               // past the attempt's camera
        bool took = log.Skip(army).Any(e => e.Contains("MapEvent { Mode = 1 }", StringComparison.Ordinal));   // L_00366's camera
        if (attacked == 0 && !took)
        {
            yield return "too weak";
            yield break;
        }
        int end = attacked != 0 ? attacked : after.Prov_Cur.Value;      // where the walk ended
        if (took)
            yield return end == 1 ? "the walk ran out" : $"taken {end}"; // NAT-23: Leazas Castle (1) taken
        if (after.Provs[25].Owner == 3 && Array.IndexOf(JapanTurnTargets, end) > Array.IndexOf(JapanTurnTargets, 25))
            yield return "M Land passed by";

        static string Field(string call, string name)
        {
            int at = call.IndexOf($" {name} ", StringComparison.Ordinal) + name.Length + 2;
            return call[at..].Split(' ')[0];
        }
    }

    /// <summary>
    /// Nagasaki (55), Osaka (56) or another province, with walls and battle area; JAPAN's commanders
    /// (Nobunaga Oda (72) to Isoroku Yamamoto (82)) alive or not, their troops around the thresholds
    /// (10 at Nagasaki, 500 elsewhere), now and then all alike (ties between the lists);
    /// <c>F_Japan_State</c> 0-3, a stale retreat mode.
    /// </summary>
    private static void AimAtJapan(GameState state, Random rng)
    {
        state.Prov_Cur = (Sys3Int)(rng.Next(3) switch { 0 => 55, 1 => 56, _ => rng.Next(0, 101) });
        state.Provs[state.Prov_Cur].BattleArea = (Sys3Int)rng.Next(0, 1000);
        state.Provs[state.Prov_Cur].Defense = (Sys3Int)rng.Next(0, 30);
        int scale = rng.Next(4) switch { 0 => 4, 1 => 40, _ => 300 };
        int same = rng.Next(5) == 0 ? rng.Next(0, scale) : -1;
        for (int c = 72; c <= 82; c++)
        {
            state.Cmdrs[c].Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : 1);
            state.Cmdrs[c].Troops = (Sys3Int)(same >= 0 ? same : rng.Next(0, scale));
        }
        state.F_Japan_State = (Sys3Int)rng.Next(0, 4);
        state.Bt_EnemyRetreatMode = (Sys3Int)rng.Next(0, 60);
    }

    private static IEnumerable<string> JapanDefenderFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        if (before.F_Japan_State is { Value: 2 or 3 } && after.F_Japan_State == 1)
            yield return "war over an alliance";
        int prov = before.Prov_Cur.Value;
        if (prov == 55)
        {
            yield return after.CallArg < 10 ? "Nagasaki too weak" : "Nagasaki";
            yield break;
        }
        if (prov == 56)
        {
            yield return "Osaka";
            yield break;
        }
        yield return $"strongest {after.Tmp0022}";
        yield return after.CallArg < 500 ? "too weak" : "retreat mode 4";
    }

    /// <summary>Kayblis's named commanders: Camilla (91), Kesselring (133), Medusa (134), Kayblis (135), the Houses (159-164), Monster Generals (181-183).</summary>
    private static readonly int[] KayblisCommanders = [91, 133, 134, 135, .. Enumerable.Range(159, 6), 181, 182, 183];

    /// <summary>
    /// A province of Kayblis's (the gate, the Houses, the castles), now and then any, attacked again
    /// now and then with up to four hits, with walls and battle area; the story stage around 2 and 3;
    /// the Monster Generals (86-89) with random maxima (now and then too small to draw), the named
    /// commanders alive or not, Camilla's maximum 200 now and then; Warg's flag and the first attack on the Demon Lord's Castle (79).
    /// </summary>
    private static void AimAtKayblis(GameState state, Random rng)
    {
        int[] provinces = [78, 78, 79, 84, 85, 86, 87, 88, 89, 90, 91, 91];
        state.Prov_Cur = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 101) : provinces[rng.Next(provinces.Length)]);
        state.Provs[state.Prov_Cur].BattleArea = (Sys3Int)rng.Next(0, 1000);
        state.Provs[state.Prov_Cur].Defense = (Sys3Int)rng.Next(0, 30);
        state.StoryStage = (Sys3Int)(rng.Next(4) switch { 0 => rng.Next(0, 6), 1 => 2, _ => 3 });
        state.Bt_GarrisonProv = (Sys3Int)(rng.Next(3) == 0 ? state.Prov_Cur.Value : rng.Next(0, 101));
        state.Bt_GarrisonHits = (Sys3Int)rng.Next(0, 5);
        for (int c = 86; c <= 89; c++)
        {
            state.Cmdrs[c].MaxTroops = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 8) : rng.Next(0, 4000));
            state.Cmdrs[c].MaxHP = (Sys3Int)rng.Next(0, 500);
        }
        foreach (int c in KayblisCommanders)
            state.Cmdrs[c].Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
        state.Cmdrs[91].MaxTroops = (Sys3Int)(rng.Next(2) == 0 ? 200 : rng.Next(0, 3000));
        state.N_Warg_Stage = (Sys3Int)(rng.Next(2) == 0 ? 0 : rng.Next(1, 3));
        state.F_DemonCastle_Attacked = (Sys3Int)rng.Next(0, 2);
    }

    /// <summary>What Warg's and Kayblis's scenes may change: the flags, the commanders, the stage, the army name.</summary>
    private static void ScribbleKayblis(GameState state, Random rng)
    {
        if (rng.Next(3) == 0)
            state.F_Warg_State = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(4) == 0)
            state.N_Warg_Stage = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(4) == 0)
            state.F_DemonCastle_Attacked = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(6) == 0)
            state.StoryStage = (Sys3Int)rng.Next(2, 5);
        foreach (int c in KayblisCommanders)
            if (rng.Next(8) == 0)
                state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(6) == 0)
            state.Strings[10] = "scribbled";
    }

    private static IEnumerable<string> KayblisDefenderFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        foreach (string[] call in log.Where(e => e.StartsWith("call ", StringComparison.Ordinal)).Select(e => e.Split(' ')))
            yield return $"call {call[1]} {call[3]}";
        int prov = before.Prov_Cur.Value;
        if (before.Bt_GarrisonProv == before.Prov_Cur)
            yield return "attacked again";
        if (log.Any(e => e is "rnd 0 0" or "rnd 1 1"))
            yield return "a general without a draw";
        if (before.StoryStage != 3 && after.StoryStage == 3)
        {
            yield return $"gate at {prov}";
            if (before.StoryStage != 2)
                yield return "gate in another stage";
        }
        if (log.Any(e => e.StartsWith("call Gﾜｰｸﾞ", StringComparison.Ordinal)))
            yield break;
        if (after.Bt_TroopLimit == 5000 && after.Bt_WallBonus == 0 && after.Tmp0001 == 86 && before.StoryStage != 3)
        {
            yield return after.Tmp0003 == 88 ? "plains, four" : "plains, two";
            if (prov == 91)
                yield return "91 in the plains";
            yield break;
        }
        yield return after.Tmp0001.Value switch
        {
            160 => "Kaybwan",
            163 => "Kaybnyan",
            133 => "Kesselring",
            91 => "Camilla",
            181 => "Kayblis",
            134 => "Medusa",
            86 => "Monster Generals",
            _ => "?",
        };
        if (after.Tmp0001 == 91 && before.Cmdrs[91].MaxTroops == 200)
            yield return "Camilla raised";
        if (after.Tmp0001 == 86 && after.Bt_WallBonus == before.Provs[prov].Defense && before.StoryStage != 3)
            yield return "Monster Generals with walls";
    }

    [Fact]
    public Task The_Kayblis_turn_runs_exactly_as_Tｹｲﾌﾞﾘｽ() =>
        CompareAsync(
            "Tｹｲﾌﾞﾘｽ.ADV",
            (script, scenario) => new KayblisTurn(script, scenario).RunAsync(),
            AimAtKayblisTurn,
            KayblisTurnFeatures,
            [
                "week S", "call Gｹｲﾌﾞﾘｽ.ADV 2", "call Gﾒﾃﾞｨｳｻ.ADV 1", "call Gﾒﾃﾞｨｳｻ.ADV 2", "call Gﾒﾃﾞｨｳｻ.ADV 3",
                "call Gﾒﾃﾞｨｳｻ.ADV 4", "call Gﾒﾃﾞｨｳｻ.ADV 5", "Medusa's counter moved", "call Gｶﾐｰﾗ.ADV 3", "call Gｶﾐｰﾗ.ADV 4",
                "call Gｶﾐｰﾗ.ADV 5", "a marked week ends the chain",
                .. new[] { 1, 2, 3, 4, 5, 6, 8, 9 }.Select(n => $"call Gｼﾙｷｨ.ADV {n}"), "the chain goes on after Gｼﾙｷｨ 8",
                "call Gﾜｰｸﾞ.ADV 3", "call Gﾜｰｸﾞ.ADV 4", "call Gﾅｷﾞ.ADV 9", "call Gｹｲﾌﾞﾜﾝ.ADV 3", "call Gｹｲﾌﾞﾆｬﾝ.ADV 3",
                "call Gｻﾃﾗ.ADV 2", "no scene", "guards", "guards again", "guards with portrait 415", "guards with portrait 466",
                "call Gﾊﾞﾎﾞﾗ.ADV 1", "call Gﾚｯﾄﾞｱｲ.ADV 1", "call Gｶｲﾄ.ADV 1", "call Gｶﾞﾙﾃｨｱ.ADV 1", "call Gｹｯｾﾙ.ADV 1",
                "call Gﾊﾟｲｱｰﾙ.ADV 1", "call Gｹｲﾌﾞﾘｽ.ADV 3", "the end", "jump",
                .. new[] { 135, 156, 131, 157, 158, 87 }.Select(c => $"Kayblis's army led by {c}"),
                .. KayblisGenerals[1].Line.Select(p => $"Kayblis's army attacks {p}"),
                "Kayblis's army without Kayblis", "Monster Generals above their maximum",
                .. KayblisGenerals.SelectMany(g => g.Line.Select(p => $"{g.Army} attacks {p}")),
                .. KayblisGenerals.Where(g => g.Army != "Kesselring").Select(g => $"{g.Army} attacks again"),
                .. KayblisGenerals.Select(g => $"{g.Army} killed"),
                .. KayblisGenerals.SelectMany(g => g.Line).Distinct().Where(p => p != 89).Select(p => $"capture at {p}"),
                "Kesselring captures", "Redeye alone at Maunes", "the battle at Maunes after Gﾚｯﾄﾞｱｲ 8",
                "Gﾚｯﾄﾞｱｲ 8 calls the battle off", "Redeye's state 1 to 2", "Redeye's state 3 to 4", "Redeye's state 6 to 7",
                "Kite's draw in ten", "Gｹｯｾﾙ 2 at Bone Forest", "Gｹｯｾﾙ 2 at the Red House",
                "call Gﾊﾞﾎﾞﾗ.ADV 2", "call Gﾊﾞﾎﾞﾗ.ADV 4", .. new[] { 2, 4, 5, 6, 8 }.Select(n => $"call Gﾚｯﾄﾞｱｲ.ADV {n}"),
                "call Gﾌﾘｰｸ.ADV 4", .. Enumerable.Range(2, 6).Select(n => $"call Gｶｲﾄ.ADV {n}"), "call Gｶﾞﾙﾃｨｱ.ADV 2",
                "call Gｹｯｾﾙ.ADV 2",
                "Piar draws again", "Piar gives up", "Piar's unit empty", "Piar attacks", "Piar captures",
                "Piar's state 2 to 3", "Piar's state 4 to 5", .. Enumerable.Range(2, 5).Select(n => $"call Gﾊﾟｲｱｰﾙ.ADV {n}"),
                .. Enumerable.Range(191, 8).Select(c => $"Assault Leader {c} marches"),
                .. AssaultLists.SelectMany(l => l.Line.Select(p => $"{l.Name} list attacks {p}")),
                .. AssaultLists.SelectMany(l => l.Line.Select(p => $"{l.Name} list captures at {p}")),
                "the south list takes Leazas Castle again", "the north list all Kayblis's", "an Assault Leader alone at Maunes",
                "an Assault Leader revived", "an Assault Leader too weak", "an Assault Leader held under 1000 by his maximum",
                "a Monster General left out", "a Monster General called up without a draw",
            ],
            ScribbleKayblisTurn,
            cases: 4000,
            logPlaces: true);

    [Fact]
    public Task Kayblis_captures_exactly_as_L_026bf() =>
        CompareAsync(
            "Tｹｲﾌﾞﾘｽ.ADV",
            (script, scenario) => new KayblisTurn(script, scenario).CaptureAsync(),
            AimAtKayblisCapture,
            KayblisCaptureFeatures,
            [
                "plain capture", "Helman falls", "Helman fallen already", "Shangri-La to Kayblis", "Shangri-La kept",
                .. new[] { 101, 104, 107, 110 }.Select(c => $"minister {c} killed"), "Zeth's story moved on", "Zeth falls",
                "Zeth fallen already", "Adam's Fortress to Kayblis", "Saias killed by the collapse",
                .. new[] { 113, 117, 121, 125 }.Select(c => $"general {c} killed"), "general dead already",
            ],
            label: "L_026bf");

    /// <summary>
    /// Kayblis's commanders: Camilla (91), the generals and their officers (129-133, 148-155), Medusa
    /// (134), Kayblis (135), Piar's (136-138), Silky (156), Satella (157), Meglass (158), Lis Guard (186).
    /// </summary>
    private static readonly int[] KayblisTurnCommanders = [91, .. Enumerable.Range(129, 10), .. Enumerable.Range(148, 11), 186];

    /// <summary>
    /// Week S now and then (the demon war below 20), the assault pending now and then; Kayblis's
    /// commanders alive or not, with HP, troops, maxima and recovery around their caps; the event
    /// chain's flags around their tests (Medusa's state and counter, Camilla's state, the war, Silky's
    /// state, souls and week, Warg, Stessel, Nagi, Kaybwan, Kaybnyan, Satella); Grief Valley (89),
    /// Silky's Castle (81), Bone Forest (90), the Red House (85) held by the player or not, Kayblis
    /// moved or not. A quarter of the cases keep the armies home (each general dead or not yet
    /// introduced, Piar at 0 or 100, no war, week 100 at most); a quarter send the generals
    /// (<see cref="AimAtKayblisGenerals"/>) and keep the later armies home; a quarter send them all,
    /// the Assault Leaders (<see cref="AimAtAssaultLeaders"/>) among them past week 100; a quarter keep
    /// all but the Assault Leaders home, past week 100, half of them with nothing at all before the
    /// leaders (<see cref="QuietBeforeTheLeaders"/>).
    /// </summary>
    private static void AimAtKayblisTurn(GameState state, Random rng)
    {
        state.F_DemonWar_Stage = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 20) : rng.Next(20, 22));
        state.Ev_KayblisAssault = (Sys3Int)(rng.Next(4) == 0 ? 1 : rng.Next(0, 3));
        foreach (int c in KayblisTurnCommanders)
        {
            var cmdr = state.Cmdrs[c];
            cmdr.Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
            cmdr.MaxHP = (Sys3Int)rng.Next(0, 200);
            cmdr.HP = (Sys3Int)Math.Max(0, cmdr.MaxHP.Value - rng.Next(-2, 6));
            cmdr.MaxTroops = (Sys3Int)rng.Next(0, 5000);
            cmdr.Troops = (Sys3Int)(rng.Next(8) == 0 ? rng.Next(60000, 65536) : Math.Max(0, cmdr.MaxTroops.Value - rng.Next(-20, 300)));
            cmdr.Recovery = (Sys3Int)(rng.Next(8) == 0 ? rng.Next(20000, 32767) : rng.Next(0, 100));
            cmdr.Class = (Sys3Int)rng.Next(0, 11);
        }
        state.F_Medusa_State = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 11));
        state.N_Medusa_Events = (Sys3Int)rng.Next(0, 8);
        state.F_Camilla_State = (Sys3Int)rng.Next(5, 13);
        state.F_Kayblis_War = (Sys3Int)(rng.Next(6) switch { 0 => 2, 1 => rng.Next(0, 4), _ => 0 });
        state.F_Silky_State = (Sys3Int)rng.Next(0, 10);
        state.F_SoulRedeye = (Sys3Int)(rng.Next(6) == 0 ? 1 : rng.Next(0, 3) * 2);
        state.F_SoulPiar = (Sys3Int)(rng.Next(6) == 0 ? 1 : 0);
        state.F_SoulMedusa = (Sys3Int)(rng.Next(6) == 0 ? 1 : 0);
        state.F_SoulKesselring = (Sys3Int)(rng.Next(6) == 0 ? 1 : 0);
        state.F_SoulCamilla = (Sys3Int)(rng.Next(6) == 0 ? 1 : 0);
        state.Week = (Sys3Int)rng.Next(60, 170);
        state.Silky_NextEventWeek = (Sys3Int)(state.Week.Value + rng.Next(-3, 3));
        state.N_Warg_Stage = (Sys3Int)rng.Next(0, 3);
        state.F_Stessel_State = (Sys3Int)rng.Next(3, 7);
        state.F_Nagi_State = (Sys3Int)rng.Next(4, 7);
        state.F_Kaybwan_State = (Sys3Int)rng.Next(0, 4);
        state.F_Kaybnyan_State = (Sys3Int)rng.Next(0, 4);
        state.F_Satella_State = (Sys3Int)rng.Next(9, 13);
        foreach (int p in new[] { 89, 81, 90, 85 })
            state.Provs[p].Owner = (Sys3Int)(rng.Next(3) == 0 ? 1 : rng.Next(0, 12));
        foreach (int c in new[] { 86, 87, 88, 89 })
            state.Cmdrs[c].MaxTroops = (Sys3Int)(rng.Next(4) == 0 ? rng.Next(0, 4) : rng.Next(0, 3000));
        state.F_Kayblis_Moved = (Sys3Int)rng.Next(0, 2);
        state.F_Babolat_State = (Sys3Int)rng.Next(0, 3);
        state.F_Redeye_State = (Sys3Int)rng.Next(0, 3);
        state.F_Calory_State = (Sys3Int)rng.Next(0, 3);
        state.F_Galtia_State = (Sys3Int)rng.Next(0, 3);
        state.F_Galtia_DefectedToKayblis = (Sys3Int)rng.Next(0, 2);
        state.F_Kesselring_State = (Sys3Int)rng.Next(0, 3);
        state.F_Piar_State = (Sys3Int)(rng.Next(3) switch { 0 => 0, 1 => 100, _ => rng.Next(3) != 0 ? rng.Next(1, 7) : rng.Next(1, 100) });
        int group = rng.Next(4);
        if (group is 2 or 3)
            AimAtAssaultLeaders(state, rng);                            // before the generals, whose lines win
        if (group is 1 or 2)
        {
            AimAtKayblisGenerals(state, rng);
            if (group == 1)
            {
                state.F_Piar_State = (Sys3Int)(rng.Next(2) == 0 ? 0 : 100);
                if (state.F_Kayblis_War == 2)
                    state.F_Kayblis_War = 0;
                state.Week = (Sys3Int)rng.Next(60, 101);
            }
        }
        else
        {
            foreach (int general in new[] { 129, 130, 132, 131, 133 })
                if (rng.Next(2) == 0)
                    state.Cmdrs[general].Status = 0;
            state.F_Babolat_State = state.Cmdrs[129].Status == 0 ? state.F_Babolat_State : Sys3Int.Zero;
            state.F_Redeye_State = state.Cmdrs[130].Status == 0 ? state.F_Redeye_State : Sys3Int.Zero;
            state.F_Calory_State = state.Cmdrs[132].Status == 0 ? state.F_Calory_State : Sys3Int.Zero;
            state.F_Galtia_State = state.Cmdrs[131].Status == 0 ? state.F_Galtia_State : Sys3Int.Zero;
            state.F_Kesselring_State = state.Cmdrs[133].Status == 0 ? state.F_Kesselring_State : Sys3Int.Zero;
            state.F_Piar_State = (Sys3Int)(rng.Next(2) == 0 ? 0 : 100);
            if (state.F_Kayblis_War == 2)
                state.F_Kayblis_War = 0;
            state.Week = (Sys3Int)(group == 3 ? rng.Next(101, 171) : rng.Next(60, 101));
            if (group == 3 && rng.Next(2) == 0)
                QuietBeforeTheLeaders(state);
        }
    }

    /// <summary>
    /// Nothing before the Assault Leaders, so they find the provinces as laid out: past week S, no
    /// assault pending, the event chain shut (Medusa (134), Camilla (91), Kesselring (133), Satella
    /// (157) dead, Medusa met, Silky, Warg, Kaybwan and Kaybnyan past their scenes), the generals dead
    /// and introduced, Piar's arc over, no war, Kayblis moved (no guards, no <c>Gｹｲﾌﾞﾘｽ</c> 3).
    /// </summary>
    private static void QuietBeforeTheLeaders(GameState state)
    {
        state.F_DemonWar_Stage = 20;
        state.Ev_KayblisAssault = 0;
        foreach (int c in new[] { 91, 129, 130, 131, 132, 133, 134, 157 })
            state.Cmdrs[c].Status = 0;
        state.F_Medusa_State = 1;
        state.F_Silky_State = 10;
        state.N_Warg_Stage = 1;
        state.F_Kaybwan_State = 0;
        state.F_Kaybnyan_State = 0;
        state.F_Babolat_State = 1;
        state.F_Redeye_State = 1;
        state.F_Calory_State = 1;
        state.F_Galtia_State = 1;
        state.F_Kesselring_State = 1;
        state.F_Piar_State = 100;
        state.F_Kayblis_War = 0;
        state.F_Kayblis_Moved = 1;
    }

    /// <summary>The Assault Leaders' two lists (<c>Tｹｲﾌﾞﾘｽ.ADV:364-490</c>, <c>:530-617</c>).</summary>
    private static readonly (string Name, int[] Line)[] AssaultLists =
    [
        ("north", [80, 81, 89, 50, 48, 45, 83, 87, 90, 72, 71, 68, 69, 44, 42, 39, 36, 31, 13, 67, 62, 59, 58, 16, 15, 2, 3, 12, 11, 4, 1]),
        ("south", [80, 81, 82, 84, 86, 88, 91, 76, 74, 70, 68, 66, 69, 67, 62, 59, 58, 16, 15, 13, 2, 1]),
    ];

    /// <summary>
    /// The Assault Leaders on the march: the 1st to 8th (191-198) dead now and then, with troops and
    /// recovery around the 1000 they need (held under it by a smaller maximum now and then, near the
    /// 16-bit ceiling now and then); Monster Generals (87, 88) at their boot maximum, small (under 99
    /// after the call-up) or tiny (no draw), of classes on both sides of 5; each list Kayblis's up to a
    /// random province (one of the last six more often), the player's or another owner's, all of it
    /// Kayblis's now and then (Leazas Castle (1) included) — the lists laid in a random order, so the
    /// later one may cut the earlier short; the north list stopping at Maunes (13) more often.
    /// </summary>
    private static void AimAtAssaultLeaders(GameState state, Random rng)
    {
        for (int c = 191; c <= 198; c++)
        {
            var leader = state.Cmdrs[c];
            leader.Status = (Sys3Int)(rng.Next(5) == 0 ? 0 : rng.Next(1, 3));
            leader.MaxHP = (Sys3Int)rng.Next(0, 20);
            leader.HP = (Sys3Int)Math.Max(0, leader.MaxHP.Value - rng.Next(-2, 6));
            leader.MaxTroops = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 1100) : rng.Next(1000, 12000));
            leader.Troops = (Sys3Int)(rng.Next(12) == 0 ? rng.Next(60000, 65536) : rng.Next(3) switch
            {
                0 => rng.Next(700, 1000),
                1 => rng.Next(0, 300),
                _ => rng.Next(0, leader.MaxTroops.Value + 1),
            });
            leader.Recovery = (Sys3Int)(rng.Next(10) == 0 ? rng.Next(20000, 32767) : rng.Next(0, 300));
        }
        foreach (int c in new[] { 87, 88 })
        {
            state.Cmdrs[c].MaxTroops = (Sys3Int)(rng.Next(4) switch { 0 => rng.Next(0, 6), 1 => rng.Next(0, 500), _ => 1200 });
            state.Cmdrs[c].MaxHP = (Sys3Int)rng.Next(0, 20);
            state.Cmdrs[c].Class = (Sys3Int)rng.Next(0, 8);
        }
        int[] others = [0, 2, 3, 4, 5, 6, 7, 9, 10, 11];
        foreach (var (name, line) in AssaultLists.OrderBy(_ => rng.Next()).ToList())
        {
            int depth = rng.Next(20) == 0 ? line.Length : rng.Next(4) == 0 ? rng.Next(line.Length - 6, line.Length) : rng.Next(line.Length);
            if (name == "north" && rng.Next(4) == 0)
                depth = 18;                                             // Maunes (13)
            for (int i = 0; i < depth; i++)
                state.Provs[line[i]].Owner = 8;
            if (depth < line.Length)
                state.Provs[line[depth]].Owner = (Sys3Int)(rng.Next(2) == 0 ? others[rng.Next(others.Length)] : 1);
        }
    }

    /// <summary>Kayblis's generals in the order they march: the army's name, the general, his line.</summary>
    private static readonly (string Army, int General, int[] Line)[] KayblisGenerals =
    [
        ("Babolat", 129, [78, 49, 53, 48, 45, 44, 42, 39, 38, 34, 32, 12, 2, 1]),
        ("Redeye", 130, [89, 50, 48, 47, 46, 41, 40, 44, 42, 39, 36, 31, 13, 2, 1]),
        ("Kite", 132, [76, 74, 70, 66, 69, 73, 65, 67, 62, 59, 58, 16, 15, 14, 17, 18, 19]),
        ("Galtia", 131, [76, 74, 65, 63, 66, 61, 60, 59, 58, 16, 15, 13, 2, 1]),
        ("Kesselring", 133, [70, 61, 19, 23, 4, 1]),
    ];

    /// <summary>
    /// The generals on the march: each alive in most cases and past his introduction, his line
    /// Kayblis's up to a random province, the player's or another owner's, all of it Kayblis's now
    /// and then — the lines laid in a random order, so a later one may cut an earlier short; Kite's
    /// stopping at Zeth Palace (69) and Redeye's at Maunes (13) more often. Babolat's pitfall at his
    /// target, elsewhere on his line or nowhere; the states around their tests (Redeye's 1-9, 8 more
    /// often, and Freak's stage at 3 or 5, Calory's 0-5, Galtia's defection, Kesselring's 3); the ninjas
    /// busy now and then all four; Zeth Palace (69) with its own picture or not; the attack marks and
    /// the last battle's rounds and result.
    /// </summary>
    private static void AimAtKayblisGenerals(GameState state, Random rng)
    {
        int[] others = [0, 2, 3, 4, 5, 6, 7, 9, 10, 11];
        foreach (var (_, general, line) in KayblisGenerals.OrderBy(_ => rng.Next()).ToList())
        {
            int depth = rng.Next(20) == 0 ? line.Length : rng.Next(line.Length);
            if (general == 132 && rng.Next(3) == 0)
                depth = 4;                                              // Zeth Palace (69)
            if (general == 130 && rng.Next(3) == 0)
                depth = 12;                                             // Maunes (13)
            for (int i = 0; i < depth; i++)
                state.Provs[line[i]].Owner = 8;
            if (depth < line.Length)
                state.Provs[line[depth]].Owner = (Sys3Int)(rng.Next(2) == 0 ? others[rng.Next(others.Length)] : 1);
            if (general == 129)
                state.Babolat_PitfallProv = (Sys3Int)(rng.Next(3) switch { 0 => line[Math.Min(depth, line.Length - 1)], 1 => 0, _ => line[rng.Next(line.Length)] });
            state.Cmdrs[general].Status = (Sys3Int)(rng.Next(6) == 0 ? 0 : rng.Next(1, 3));
        }
        state.F_Babolat_State = (Sys3Int)(rng.Next(6) == 0 ? 0 : rng.Next(1, 4));
        state.F_Redeye_State = (Sys3Int)(rng.Next(4) == 0 ? 8 : rng.Next(6) == 0 ? 0 : rng.Next(1, 10));
        state.N_Freak_Stage = (Sys3Int)(rng.Next(3) switch { 0 => 3, 1 => 5, _ => rng.Next(0, 8) });
        state.F_Calory_State = (Sys3Int)(rng.Next(6) == 0 ? 0 : rng.Next(1, 6));
        state.F_Galtia_State = (Sys3Int)(rng.Next(6) == 0 ? 0 : rng.Next(1, 3));
        state.F_Galtia_DefectedToKayblis = (Sys3Int)(rng.Next(6) == 0 ? 1 : 0);
        state.F_Kesselring_State = (Sys3Int)(rng.Next(6) == 0 ? 0 : rng.Next(1, 5));
        state.F_Kanami_NinjaUsed = Ninja(rng);
        state.F_Freya_NinjaUsed = Ninja(rng);
        state.F_Kaoru_NinjaUsed = Ninja(rng);
        state.F_Wichita_NinjaUsed = Ninja(rng);
        state.Provs[69].BgCg = (Sys3Int)(rng.Next(4) == 0 ? rng.Next(1000, 1050) : 1011);
        state.Ai_BabolatAtk = Mark(rng);
        state.Ai_RedeyeAtk = Mark(rng);
        state.Ai_KiteAtk = Mark(rng);
        state.Ai_GaltiaAtk = Mark(rng);
        state.Bt_RoundsLeft = (Sys3Int)rng.Next(0, 3);
        state.Bt_Result = (Sys3Int)rng.Next(0, 3);

        static Sys3Int Ninja(Random rng) => (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(2) == 0 ? 1 : 99);

        static Sys3Int Mark(Random rng) => (Sys3Int)(rng.Next(4) switch { 0 => 10, 1 => 11, _ => 0 });
    }

    /// <summary>
    /// What the scenes and battles may change: Grief Valley (89)'s owner (so the guards stop), Kayblis
    /// moved, the week's event flag, the chain's flags, the generals' states and lives, Piar's state,
    /// a battle's rounds left and result, Zeth Palace (69) lost, the player's defenders (Miki (unit 32)
    /// among them now and then).
    /// </summary>
    private static void ScribbleKayblisTurn(GameState state, Random rng)
    {
        if (rng.Next(2) == 0)
            state.Provs[89].Owner = (Sys3Int)rng.Next(0, 9);
        if (rng.Next(6) == 0)
            state.F_Kayblis_Moved = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(6) == 0)
            state.Ev_WeeklyEventDone = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(6) == 0)
            state.F_Silky_State = (Sys3Int)rng.Next(0, 10);
        if (rng.Next(6) == 0)
            state.N_Warg_Stage = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(8) == 0)
            state.F_Kayblis_War = (Sys3Int)rng.Next(0, 3);
        foreach (int general in new[] { 129, 130, 131, 132, 133 })
            if (rng.Next(10) == 0)
                state.Cmdrs[general].Status = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(10) == 0)
            state.F_Piar_State = (Sys3Int)(rng.Next(2) == 0 ? 100 : rng.Next(0, 3));
        foreach (int p in new[] { 81, 90, 85 })
            if (rng.Next(10) == 0)
                state.Provs[p].Owner = (Sys3Int)rng.Next(0, 9);
        if (rng.Next(6) == 0)
            state.Strings[10] = "scribbled";
        if (rng.Next(2) == 0)
        {
            state.Bt_RoundsLeft = (Sys3Int)(rng.Next(2) == 0 ? 0 : rng.Next(1, 5));
            state.Bt_Result = (Sys3Int)rng.Next(0, 3);
        }
        if (state.Prov_Cur == 69 && rng.Next(3) == 0)
            state.Provs[69].Owner = 8;                                  // Zeth Palace lost (Calory's last scene)
        if (rng.Next(6) == 0)
        {
            Sys3Int defender = (Sys3Int)(rng.Next(3) == 0 ? 32 : rng.Next(0, 40));   // Miki (unit 32) among the defenders
            switch (rng.Next(4))
            {
                case 0: state.Bt_PlayerParty1 = defender; break;
                case 1: state.Bt_PlayerParty2 = defender; break;
                case 2: state.Bt_PlayerParty3 = defender; break;
                default: state.Bt_PlayerParty4 = defender; break;
            }
        }
    }

    private static IEnumerable<string> KayblisTurnFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        var calls = log.Where(e => e.StartsWith("call ", StringComparison.Ordinal)).Select(e => e.Split(' ')).ToList();
        string[] chain = ["Gﾒﾃﾞｨｳｻ.ADV", "Gｶﾐｰﾗ.ADV", "Gｼﾙｷｨ.ADV", "Gﾜｰｸﾞ.ADV", "Gﾅｷﾞ.ADV", "Gｹｲﾌﾞﾜﾝ.ADV", "Gｹｲﾌﾞﾆｬﾝ.ADV", "Gｻﾃﾗ.ADV"];
        int guards = 0;
        for (int i = 0; i < calls.Count; i++)
        {
            string[] call = calls[i];
            if (call[1] == "Gｹｲﾌﾞﾘｽ.ADV" && call[3] == "1")
                yield return "week S";
            if (call[1] != "FIGT_B.ADV")
            {
                yield return $"call {call[1]} {call[3]}";
                if (call[1] == "Gｼﾙｷｨ.ADV" && call[3] == "8" && i + 1 < calls.Count && chain.Contains(calls[i + 1][1]))
                    yield return "the chain goes on after Gｼﾙｷｨ 8";
                continue;
            }
            string line = string.Join(' ', call);
            if (line.Contains(" Prov_Cur 89 ", StringComparison.Ordinal) && line.Contains(" Tmp 186,", StringComparison.Ordinal))
            {
                guards++;
                yield return "guards";
                if (line.Contains(" CallArg 415 ", StringComparison.Ordinal))
                    yield return "guards with portrait 415";
                if (line.Contains(" CallArg 466 ", StringComparison.Ordinal))
                    yield return "guards with portrait 466";
            }
        }
        if (guards > 1)
            yield return "guards again";
        if (after.N_Medusa_Events != before.N_Medusa_Events)
            yield return "Medusa's counter moved";
        if (before.F_DemonWar_Stage >= 20 && !calls.Any(c => chain.Contains(c[1])))
        {
            yield return "no scene";
            // Camilla's draw, then no draw for Medusa's last scene (:181): the chain ended at :107-109.
            int camilla = log.FindIndex(e => e.StartsWith("rnd 3 ", StringComparison.Ordinal));
            if (camilla >= 0 && !log.Skip(camilla).Any(e => e.StartsWith("rnd 5 ", StringComparison.Ordinal)))
                yield return "a marked week ends the chain";
        }
        if (log.LastOrDefault() == "return")
            yield return "the end";
        if (log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return "jump";
        foreach (string feature in KayblisGeneralFeatures(before, after, log))
            yield return feature;
        foreach (string feature in KayblisAssaultFeatures(before, after, log))
            yield return feature;
    }

    /// <summary>
    /// The Assault Leaders: each march by leader, list and province — a battle told by the head of
    /// the priority list in its call, a capture by the head in the log of its report, with the owner
    /// the capture's first map call saw (Kayblis's: the south list run on into Leazas Castle (1),
    /// NAT-16); the leader alone at Maunes (13), a Monster General left out of the unit. In a run that
    /// reached the file's end: a leader revived, too weak (held under 1000 by his maximum or not) or
    /// finding all of the north list Kayblis's; a Monster General called up without a draw.
    /// </summary>
    private static IEnumerable<string> KayblisAssaultFeatures(GameState before, GameState after, List<string> log)
    {
        var marched = new HashSet<int>();
        for (int i = 0; i < log.Count; i++)
        {
            string entry = log[i];
            bool battle = entry.StartsWith("call FIGT_B.ADV ", StringComparison.Ordinal);
            bool capture = entry.StartsWith("MessageEvent Tｹｲﾌﾞﾘｽ.ADV:1628 ", StringComparison.Ordinal);
            if (!battle && !capture)
                continue;
            int leader = int.Parse(battle ? Field(entry, "Tmp").Split(',')[0] : Field(entry, "Tmp0001"));
            if (leader is < 191 or > 198)
                continue;
            marched.Add(leader);
            string list = AssaultLists[(leader - 191) % 2].Name;
            string prov = Field(entry, "Prov_Cur");
            if (battle)
            {
                yield return $"{list} list attacks {prov}";
                string[] party = Field(entry, "Party").Split(',');
                if (list == "north" && prov == "13" && party[1..].All(p => p == "0"))
                    yield return "an Assault Leader alone at Maunes";
                else if (party.Count(p => p != "0") < 3)
                    yield return "a Monster General left out";
                continue;
            }
            yield return $"{list} list captures at {prov}";
            int map = log.FindLastIndex(i, e => e.StartsWith("MapEvent { Mode = 1 }", StringComparison.Ordinal));
            if (map >= 0 && Field(log[map], "Owner") == "8")
                yield return list == "south" && prov == "1" ? "the south list takes Leazas Castle again" : $"the {list} list takes {prov} again";
        }
        foreach (int leader in marched)
            yield return $"Assault Leader {leader} marches";
        if (log.LastOrDefault() != "return")
            yield break;
        int[] weeks = [100, 110, 120, 130, 140, 150, 155, 160];
        for (int k = 0; k < weeks.Length && before.Week > (Sys3Int)weeks[k]; k++)
        {
            int leader = 191 + k;
            var was = before.Cmdrs[leader];
            if (was.Status == 0)
                yield return "an Assault Leader revived";
            else if (after.Cmdrs[leader].Troops < 1000)
                yield return was.Troops.Value + was.Recovery.Value >= 1000 ? "an Assault Leader held under 1000 by his maximum" : "an Assault Leader too weak";
            else if (k % 2 == 0 && !marched.Contains(leader))
                yield return "the north list all Kayblis's";
        }
        if (before.Week > 100 && (before.Cmdrs[87].MaxTroops / 3 <= 1 || before.Cmdrs[88].MaxTroops / 3 <= 1))
            yield return "a Monster General called up without a draw";

        static string Field(string entry, string name) =>
            entry[(entry.IndexOf($" {name} ", StringComparison.Ordinal) + name.Length + 2)..].Split(' ')[0];
    }

    /// <summary>
    /// The generals' marches: each battle by army (its name in string 10) and province, a second
    /// attack, a general killed; a province on the lines taken without a battle (told by its owner:
    /// Grief Valley (89), whose owner the scenes write, left out; Zeth Palace (69) only without a battle
    /// there), Kesselring's capture (his name at the head of the priority list when its report shows);
    /// Redeye alone at Maunes (13), the battle there
    /// after <c>Gﾚｯﾄﾞｱｲ</c> 8 or not (NAT-28), his state's steps; Kite's draw in ten; <c>Gｹｯｾﾙ</c> 2 at
    /// Bone Forest (90) or the Red House (85). Piar's unit: her target drawn again, given up, her unit
    /// after <c>Gﾊﾟｲｱｰﾙ</c> 2 attacking, capturing (a scene's scribble: dead in play) or empty (the same),
    /// her state's steps. Kayblis's army: each march by its leader (dead Kayblis's empty unit
    /// included) and province, the Monster Generals refilled above their maximum (NAT-10).
    /// </summary>
    private static IEnumerable<string> KayblisGeneralFeatures(GameState before, GameState after, List<string> log)
    {
        var battles = log.Where(e => e.StartsWith("call FIGT_B.ADV ", StringComparison.Ordinal)).ToList();
        foreach (var (army, general, _) in KayblisGenerals)
        {
            var own = battles.Where(b => b.EndsWith($" Str10 {army} Unit", StringComparison.Ordinal)).ToList();
            foreach (string battle in own)
            {
                yield return $"{army} attacks {Field(battle, "Prov_Cur")}";
                if (army == "Redeye" && Field(battle, "Prov_Cur") == "13" && Field(battle, "Party") == "130,0,0,0")
                    yield return "Redeye alone at Maunes";
            }
            if (own.Count > 1)
                yield return $"{army} attacks again";
            if (own.Count > 0 && after.Cmdrs[general].Status == 0)
                yield return $"{army} killed";
        }
        foreach (int p in KayblisGenerals.SelectMany(g => g.Line).Distinct())
            if (p != 89 && before.Provs[p].Owner != 8 && after.Provs[p].Owner == 8
                && (p != 69 || !battles.Any(b => Field(b, "Prov_Cur") == "69")))
                yield return $"capture at {p}";
        if (log.Any(e => e.StartsWith("MessageEvent Tｹｲﾌﾞﾘｽ.ADV:1628 ", StringComparison.Ordinal) && Field(e, "Tmp0001") == "133"))
            yield return "Kesselring captures";
        int ninja = log.FindIndex(e => e.StartsWith("call Gﾚｯﾄﾞｱｲ.ADV entry 8 ", StringComparison.Ordinal));
        if (ninja >= 0)
        {
            string? next = log.Skip(ninja + 1).FirstOrDefault(e => e.StartsWith("call ", StringComparison.Ordinal));
            bool fought = next is not null && next.StartsWith("call FIGT_B.ADV ", StringComparison.Ordinal) && Field(next, "Prov_Cur") == "13"
                && !KayblisGenerals.Any(g => g.Army != "Redeye" && next.EndsWith($" Str10 {g.Army} Unit", StringComparison.Ordinal));
            yield return fought ? "the battle at Maunes after Gﾚｯﾄﾞｱｲ 8" : "Gﾚｯﾄﾞｱｲ 8 calls the battle off";
        }
        foreach (var (from, to) in new[] { (1, 2), (3, 4), (6, 7) })
            if (before.F_Redeye_State == (Sys3Int)from && after.F_Redeye_State == (Sys3Int)to)
                yield return $"Redeye's state {from} to {to}";
        if (log.Any(e => e.StartsWith("rnd 10 ", StringComparison.Ordinal)))
            yield return "Kite's draw in ten";
        foreach (string call in log.Where(e => e.StartsWith("call Gｹｯｾﾙ.ADV entry 2 ", StringComparison.Ordinal)))
            yield return Field(call, "Prov_Cur") == "90" ? "Gｹｯｾﾙ 2 at Bone Forest" : "Gｹｯｾﾙ 2 at the Red House";
        int target = log.FindIndex(e => e.StartsWith("rnd 77 ", StringComparison.Ordinal));
        if (target >= 0)
        {
            if (log.Count(e => e.StartsWith("rnd 77 ", StringComparison.Ordinal)) > 1)
                yield return "Piar draws again";
            int last = log.FindLastIndex(e => e.StartsWith("rnd 77 ", StringComparison.Ordinal));
            if (last + 1 < log.Count && log[last + 1] == "rnd 10 1")
                yield return "Piar gives up";
        }
        int scene = log.FindIndex(e => e.StartsWith("call Gﾊﾟｲｱｰﾙ.ADV entry 2 ", StringComparison.Ordinal));
        if (scene >= 0)
        {
            string? next = log.Skip(scene + 1).FirstOrDefault(e => e.StartsWith("call ", StringComparison.Ordinal) || e.Contains("Tｹｲﾌﾞﾘｽ.ADV:1628", StringComparison.Ordinal));
            yield return next is null || !(next.StartsWith("call FIGT_B.ADV ", StringComparison.Ordinal) || next.Contains("Tｹｲﾌﾞﾘｽ.ADV:1628", StringComparison.Ordinal))
                ? "Piar's unit empty"
                : next.StartsWith("call ", StringComparison.Ordinal) ? "Piar attacks" : "Piar captures";
        }
        foreach (var (from, to) in new[] { (2, 3), (4, 5) })
            if (before.F_Piar_State == (Sys3Int)from && after.F_Piar_State == (Sys3Int)to)
                yield return $"Piar's state {from} to {to}";
        foreach (string battle in battles.Where(b => b.EndsWith(" Str10 Kayblis Unit", StringComparison.Ordinal)
                                                     && int.Parse(Field(b, "Tmp").Split(',')[0]) is < 191 or > 198))   // not an Assault Leader's
        {
            string leader = Field(battle, "Party").Split(',')[0];
            yield return leader == "0" ? "Kayblis's army without Kayblis" : $"Kayblis's army led by {leader}";
            yield return $"Kayblis's army attacks {Field(battle, "Prov_Cur")}";
        }
        if (new[] { 86, 87, 88, 89 }.Any(c => before.Cmdrs[c].Troops <= before.Cmdrs[c].MaxTroops && after.Cmdrs[c].Troops > after.Cmdrs[c].MaxTroops))
            yield return "Monster Generals above their maximum";

        static string Field(string call, string name) =>
            call[(call.IndexOf($" {name} ", StringComparison.Ordinal) + name.Length + 2)..].Split(' ')[0];
    }

    /// <summary>
    /// A province Kayblis takes: Lang Bau (45), a Zeth tower (68, 70, 67, 66), a Zeth general's city
    /// (58, 61, 65, 63) or any; Helman's and Zeth's wars at 0-2, Shangri-La (29) Helman's now and
    /// then, Adam's Fortress (58) Zeth's now and then; the ministers Chizuko (101), Papaya (104), Nagi
    /// (107), Magic (110) dead in half the cases each, the generals Saias (113), Uspira (117),
    /// Kabachahn (121), Alex (125) alive or not; Zeth's story around 7.
    /// </summary>
    private static void AimAtKayblisCapture(GameState state, Random rng)
    {
        int[] provinces = [45, 45, 68, 70, 67, 66, 68, 70, 67, 66, 58, 61, 65, 63];
        state.Prov_Cur = (Sys3Int)(rng.Next(5) == 0 ? rng.Next(0, 92) : provinces[rng.Next(provinces.Length)]);
        state.F_Helman_War = (Sys3Int)rng.Next(0, 3);
        state.F_Zeth_War = (Sys3Int)rng.Next(0, 3);
        state.F_Zeth_State = (Sys3Int)rng.Next(4, 13);
        state.Provs[29].Owner = (Sys3Int)(rng.Next(3) == 0 ? 4 : rng.Next(0, 4));
        state.Provs[58].Owner = (Sys3Int)(rng.Next(2) == 0 ? 7 : rng.Next(0, 9));
        foreach (int c in new[] { 101, 104, 107, 110 })
            state.Cmdrs[c].Status = (Sys3Int)(rng.Next(2) == 0 ? 0 : rng.Next(1, 3));
        foreach (int c in new[] { 113, 117, 121, 125 })
            state.Cmdrs[c].Status = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 3));
    }

    private static IEnumerable<string> KayblisCaptureFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        int prov = before.Prov_Cur.Value;
        if (prov == 45)
            yield return before.F_Helman_War == 2 ? "Helman fallen already" : "Helman falls";
        if (prov == 45 && before.F_Helman_War != 2)
            yield return after.Provs[29].Owner == 8 && before.Provs[29].Owner == 4 ? "Shangri-La to Kayblis" : "Shangri-La kept";
        foreach (int c in new[] { 101, 104, 107, 110 })
            if (before.Cmdrs[c].Status != 0 && after.Cmdrs[c].Status == 0)
                yield return $"minister {c} killed";
        if (prov is 68 or 70 or 67 or 66)
        {
            if (before.F_Zeth_State < 7)
                yield return "Zeth's story moved on";
            if (after.F_Zeth_War == 2 && before.F_Zeth_War != 2)
                yield return "Zeth falls";
            else if (before.F_Zeth_War == 2 && new[] { 101, 104, 107, 110 }.All(c => after.Cmdrs[c].Status == 0))
                yield return "Zeth fallen already";
            if (after.F_Zeth_War == 2 && before.F_Zeth_War != 2 && before.Provs[58].Owner == 7)
                yield return "Adam's Fortress to Kayblis";
            if (after.F_Zeth_War == 2 && before.F_Zeth_War != 2 && before.Cmdrs[113].Status != 0)
                yield return "Saias killed by the collapse";
        }
        foreach (int c in new[] { 113, 117, 121, 125 })
            if (before.Cmdrs[c].Status != 0 && after.Cmdrs[c].Status == 0)
                yield return $"general {c} killed";
        if (prov is 58 or 61 or 65 or 63 && before.Cmdrs[prov switch { 58 => 113, 61 => 117, 65 => 121, _ => 125 }].Status == 0)
            yield return "general dead already";
        if (prov is not (45 or 68 or 70 or 67 or 66 or 58 or 61 or 65 or 63))
            yield return "plain capture";
    }

    /// <summary>Every attack <c>Tﾍﾙﾏﾝ</c> can make: the army and the province, in the file's order.</summary>
    private static readonly string[] HelmanAttacks =
    [
        .. new[] { 36, 38, 34, 32, 31, 30, 2, 1 }.Select(p => $"1st at {p}"), "Tainyar at 13",
        .. new[] { 44, 47, 46, 40, 41, 52 }.Select(p => $"2nd at {p}"),
        "3rd re-picked at 44", .. new[] { 48, 40, 42, 47, 39, 36 }.Select(p => $"3rd at {p}"),
        .. new[] { 40, 41 }.Select(p => $"4th at {p}"), .. new[] { 40, 41 }.Select(p => $"turned 4th at {p}"),
        .. new[] { 39, 44, 40, 41, 36, 31, 30, 48, 47, 46 }.Select(p => $"5th at {p}"),
        .. new[] { 43, 44, 48, 47, 46, 40, 41, 42, 39, 36 }.Select(p => $"Crusaders at {p}"),
    ];

    /// <summary>The provinces <c>Tﾍﾙﾏﾝ</c> tests: every army's targets and the scenes' (Log B (31), Rosesgrad (39), Pawn (40), Sudori 13 (41)).</summary>
    private static readonly int[] HelmanTargets = [1, 2, 13, 30, 31, 32, 34, 36, 38, 39, 40, 41, 42, 43, 44, 46, 47, 48, 52];

    /// <summary>Helman's commanders: the five armies, Tainyar (26), the Crusader Olhound (60), Capricorn (184) and Ortega (185).</summary>
    private static readonly int[] HelmanCommanders = [.. Enumerable.Range(22, 39), 184, 185];

    /// <summary>
    /// The week against the war's start, every story flag <c>Tﾍﾙﾏﾝ</c> tests at its tested values
    /// (the war mostly on), Helman's commanders alive or not with troops around the filler's 190 and
    /// the armies' 500, 1000 and 1500, and the provinces of the lists held by the player at a density
    /// of the case's own (so that the walks reach their last targets now and then), else by Helman,
    /// the monsters or anyone.
    /// </summary>
    private static void AimAtHelmanTurn(GameState state, Random rng)
    {
        state.Week = (Sys3Int)rng.Next(20, 120);
        state.Stessel_StartWeek = (Sys3Int)Math.Max(0, state.Week.Value - rng.Next(-2, 20));
        state.F_StesselSheila_Stage = (Sys3Int)rng.Next(0, 6);
        state.N_Freya_State = (Sys3Int)(rng.Next(4) switch { 0 => 10, 1 => 20, 2 => 12, _ => rng.Next(0, 30) });
        state.F_Lelyukov_State = (Sys3Int)(rng.Next(4) switch { 0 => 1, 1 => 10, _ => 0 });
        state.F_Patton_State = PattonState(rng);
        state.F_Aristoles_Helman2ndState = (Sys3Int)(rng.Next(5) == 0 ? 20 : rng.Next(0, 4));
        state.F_Helman_War = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 3) : 1);
        state.F_Mineva_State = (Sys3Int)rng.Next(0, 3);
        state.F_Pastel_State = (Sys3Int)rng.Next(0, 2);
        state.F_Nero_Helman4thState = NeroState(rng);
        state.F_Cream_State = (Sys3Int)rng.Next(0, 2);
        state.F_Rolex_State = (Sys3Int)rng.Next(0, 6);
        state.N_Olhound_Stage = OlhoundStage(rng);
        double mine = rng.NextDouble() * 0.5;
        foreach (int p in HelmanTargets)
            state.Provs[p].Owner = (Sys3Int)(rng.NextDouble() < mine ? 1 : rng.Next(4) switch { 0 => 8, 1 => rng.Next(0, 12), _ => 4 });
        foreach (int c in HelmanCommanders)
        {
            var cmdr = state.Cmdrs[c];
            cmdr.Status = (Sys3Int)(rng.Next(5) == 0 ? 0 : rng.Next(1, 3));
            cmdr.Troops = (Sys3Int)(rng.Next(10) switch
            {
                0 => rng.Next(60000, 65536),
                < 3 => rng.Next(185, 195),
                _ => rng.Next(0, 1200),
            });
            cmdr.MaxHP = (Sys3Int)rng.Next(0, 500);
            cmdr.MaxTroops = (Sys3Int)rng.Next(0, 3000);
        }
    }

    private static Sys3Int PattonState(Random rng) => (Sys3Int)(rng.Next(7) switch { 0 => 0, 1 => 1, 2 => 2, 3 => 10, 4 => 11, 5 => 12, _ => 20 });

    private static Sys3Int NeroState(Random rng) => (Sys3Int)(rng.Next(4) switch { 0 => 0, 1 => 100, 2 => 6, _ => rng.Next(1, 7) });

    private static Sys3Int OlhoundStage(Random rng) => (Sys3Int)(rng.Next(3) switch { 0 => 0, 1 => 100, _ => rng.Next(1, 10) });

    /// <summary>
    /// What Helman's scenes and battles may change besides the generic scribble: the week's mark,
    /// every flag the turn reads, the commanders, the provinces of its lists, the two order sums and
    /// the army name.
    /// </summary>
    private static void ScribbleHelmanTurn(GameState state, Random rng)
    {
        if (rng.Next(3) == 0)
            state.Ev_WeeklyEventDone = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(8) == 0)
            state.F_Helman_War = (Sys3Int)rng.Next(1, 3);
        if (rng.Next(8) == 0)
            state.F_StesselSheila_Stage = (Sys3Int)rng.Next(0, 6);
        if (rng.Next(8) == 0)
            state.F_Lelyukov_State = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(8) == 0)
            state.F_Patton_State = PattonState(rng);
        if (rng.Next(8) == 0)
            state.F_Aristoles_Helman2ndState = (Sys3Int)rng.Next(0, 4);
        if (rng.Next(8) == 0)
            state.F_Mineva_State = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(8) == 0)
            state.F_Pastel_State = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(8) == 0)
            state.F_Nero_Helman4thState = NeroState(rng);
        if (rng.Next(8) == 0)
            state.F_Cream_State = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(8) == 0)
            state.F_Rolex_State = (Sys3Int)rng.Next(0, 6);
        if (rng.Next(8) == 0)
            state.N_Olhound_Stage = OlhoundStage(rng);
        if (rng.Next(4) == 0)
        {
            state.Tmp0011 = (Sys3Int)rng.Next(0, 3000);
            state.Tmp0012 = (Sys3Int)rng.Next(0, 3000);
        }
        if (rng.Next(6) == 0)
            state.Strings[10] = "scribbled";
        foreach (int c in HelmanCommanders)
        {
            if (rng.Next(8) == 0)
                state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 2);
            if (rng.Next(8) == 0)
                state.Cmdrs[c].Troops = (Sys3Int)rng.Next(0, 1200);
        }
        foreach (int p in HelmanTargets)
            if (rng.Next(10) == 0)
                state.Provs[p].Owner = (Sys3Int)(rng.Next(3) switch { 0 => 1, 1 => 4, _ => 8 });
    }

    private static IEnumerable<string> HelmanTurnFeatures(GameState before, GameState after, GameState first, List<string> log)
    {
        int attacks = 0;
        foreach (string call in log.Where(e => e.StartsWith("call ", StringComparison.Ordinal)))
        {
            string[] parts = call.Split(' ');
            if (parts[1] != "FIGT_B.ADV")
            {
                yield return $"call {parts[1]} {parts[3]}";
                continue;
            }
            attacks++;
            string prov = Field(call, "Prov_Cur");
            int[] tmp = [.. Field(call, "Tmp").Split(',').Select(int.Parse)];
            int[] party = [.. Field(call, "Party").Split(',').Select(int.Parse)];
            string army = call[(call.IndexOf(" Str10 ", StringComparison.Ordinal) + 7)..];
            if (army == "Helman 1st Army" && prov == "13" && party is [26, 0, 0, 0])
                yield return "Tainyar at 13";
            else if (army == "Helman 1st Army")
            {
                yield return $"1st at {prov}";
                yield return tmp[0] == 24 ? "1st army, first order" : "1st army, second order";
            }
            else if (prov == "44" && tmp is [42, 43, 39, 40, 41, 0, 0, 0, 0])
                yield return "3rd re-picked at 44";
            else
                yield return army switch
                {
                    "Helman 2nd Army" => $"2nd at {prov}",
                    "Helman 3rd Army" => $"3rd at {prov}",
                    "Helman 4th Army" when tmp[0] == 49 => $"4th at {prov}",
                    "Helman 4th Army" => $"turned 4th at {prov}",
                    "Helman 5th Army" => $"5th at {prov}",
                    "Crusader Army" => $"Crusaders at {prov}",
                    _ => $"? at {prov}",
                };
            if (party[0] != 0 && party.Contains(0)
                && tmp.Any(c => HelmanCommanders.Contains(c) && before.Cmdrs[c].Status != 0 && before.Cmdrs[c].Troops < 190 && !party.Contains(c)))
                yield return "filtered";
        }
        yield return attacks switch { 0 => "no attack", 1 => "one attack", 2 => "two attacks", _ => "three attacks" };
        if (log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return "jump";
        if (log.Any(e => e.Contains("Tﾍﾙﾏﾝ.ADV:689", StringComparison.Ordinal)))
        {
            yield return "capture";
            if (before.Provs[13].Owner == 8 && after.Provs[13].Owner == 4)
                yield return "capture at 13";
            if (before.Provs[2].Owner == 8 && after.Provs[2].Owner == 4 && before.Provs[13].Owner != 8)
                yield return "capture at 2";
        }
        // What the file changed before its first call (or before its end).
        if (before.Cmdrs[26].Status == 0 && first.Cmdrs[26].Status == 1)
            yield return "Tainyar revived";
        if (before.F_Rolex_State == 0 && first.F_Rolex_State == 1)
            yield return "Rolex 0 to 1";
        if (before.F_Rolex_State == 1 && first.F_Rolex_State == 2 && before.Provs[39].Owner != 1)
            yield return "Rolex 1 to 2";
        if (before.F_Rolex_State < 2 && first.F_Rolex_State >= 2 && before.Provs[39].Owner == 1)
            yield return "Rolex 2 at Rosesgrad";
        if (before.F_Nero_Helman4thState is { Value: 1 or 2 } && first.F_Nero_Helman4thState == before.F_Nero_Helman4thState + 1)
            yield return "Nero counted up";
        if (before.F_Aristoles_Helman2ndState == 1 && first.F_Aristoles_Helman2ndState == 2)
            yield return "Aristoles 1 to 2";
        if (before.F_Aristoles_Helman2ndState == 2 && first.F_Aristoles_Helman2ndState == 3)
            yield return "Aristoles 2 to 3";

        static string Field(string call, string name)
        {
            int at = call.IndexOf($" {name} ", StringComparison.Ordinal) + name.Length + 2;
            return call[at..].Split(' ')[0];
        }
    }

    /// <summary>
    /// Helman's provinces, mostly, held by Helman (now and then by Patton); the war on or won, the
    /// Sheila, Mineva, Lelyukov, Nero, Cream, Rolex, Patton and Ruberan flags at their tested values;
    /// commanders 22-59 alive or not with troops around the filler's 190, and Civilian Generals
    /// 58 and 59 with a maximum HP; the walls and battle area of the province; the second party slot.
    /// </summary>
    private static void AimAtHelman(GameState state, Random rng)
    {
        int[] provinces = [2, 13, 30, 31, 32, 34, 35, 36, 37, 38, 39, 40, 41, 42, 44, 45, 46, 47, 48];
        state.Prov_Cur = (Sys3Int)(rng.Next(8) == 0 ? rng.Next(0, 101) : provinces[rng.Next(provinces.Length)]);
        state.Provs[state.Prov_Cur].Owner = (Sys3Int)(rng.Next(10) == 0 ? 5 : 4);
        state.Provs[state.Prov_Cur].BattleArea = (Sys3Int)rng.Next(0, 1000);
        state.Provs[state.Prov_Cur].Defense = (Sys3Int)rng.Next(0, 30);
        state.F_StesselSheila_Stage = (Sys3Int)(rng.Next(3) == 0 ? 0 : 1);
        state.F_Helman_War = (Sys3Int)(rng.Next(5) == 0 ? 2 : 1);
        state.F_Sheila_State = (Sys3Int)rng.Next(0, 2);
        state.F_Sheila_Flag = (Sys3Int)rng.Next(0, 2);
        state.Units[43].Status = (Sys3Int)rng.Next(0, 2);
        state.F_Mineva_State = (Sys3Int)rng.Next(0, 2);
        state.F_Lelyukov_State = (Sys3Int)(rng.Next(3) switch { 0 => 1, 1 => 10, _ => 0 });
        state.F_Nero_Helman4thState = (Sys3Int)(rng.Next(3) switch { 0 => 100, 1 => 0, _ => rng.Next(1, 7) });
        state.F_Cream_State = (Sys3Int)rng.Next(0, 2);
        state.F_Rolex_State = (Sys3Int)rng.Next(0, 6);
        state.F_Patton_State = (Sys3Int)(rng.Next(3) switch { 0 => 10, 1 => 20, _ => 0 });
        state.F_Ruberan_State = (Sys3Int)rng.Next(0, 2);
        state.Bt_PlayerParty2 = (Sys3Int)(rng.Next(2) == 0 ? 0 : rng.Next(1, 50));
        for (int c = 22; c <= 59; c++)
        {
            var cmdr = state.Cmdrs[c];
            cmdr.Status = (Sys3Int)(rng.Next(5) == 0 ? 0 : rng.Next(1, 3));
            cmdr.Troops = (Sys3Int)(rng.Next(10) switch
            {
                0 => rng.Next(60000, 65536),
                1 => rng.Next(185, 195),
                _ => rng.Next(0, 3000),
            });
            cmdr.MaxHP = (Sys3Int)rng.Next(0, 500);
        }
    }

    /// <summary>What Helman's scenes may change: the cancel flag in <c>Tmp0001</c>, Sheila's flag, the commanders.</summary>
    private static void ScribbleHelman(GameState state, Random rng)
    {
        if (rng.Next(2) == 0)
            state.Tmp0001 = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(3) == 0)
            state.F_Sheila_Flag = (Sys3Int)rng.Next(0, 2);
        if (rng.Next(3) == 0)
            state.F_Helman_War = (Sys3Int)rng.Next(1, 3);
        for (int c = 22; c <= 59; c++)
            if (rng.Next(8) == 0)
                state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 2);
    }

    private static IEnumerable<string> HelmanDefenderFeatures(GameState before, GameState after, List<string> log)
    {
        foreach (string call in log.Where(e => e.StartsWith("call ", StringComparison.Ordinal)))
        {
            string[] parts = call.Split(' ');
            yield return parts[1] == "Gｼｰﾗ.ADV" ? $"call Gｼｰﾗ.ADV {parts[3]}" : $"call {parts[1]}";
        }
        if (log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return "jump";
        int prov = before.Prov_Cur.Value;
        bool war = log.Any(e => e.StartsWith("call Gｽﾃｯｾﾙ", StringComparison.Ordinal)) || before.F_Helman_War == 2;
        if (after.Bt_EnemyParty1 == 0 && log.Any(e => e.StartsWith("call Gｼｰﾗ.ADV entry 2", StringComparison.Ordinal)))
            yield return "called off";
        if (after.Bt_EnemyParty1 == 58 && after.Bt_EnemyParty2 == 59 && after.Bt_EnemyParty3 == 0)
        {
            yield return "militia";
            yield break;
        }
        if (after.Ev_PhaseNation != 4 || war && before.F_Helman_War == 2)
            yield break;
        switch (after.Tmp0001.Value)
        {
            case 22:
                yield return "1st army, first order";
                yield return "1st army";
                break;
            case 27 when after.Tmp0002 == 0:
                yield return "Laumiere alone";
                break;
            case 27:
                yield return "1st army";
                break;
            case 34:
                yield return "2nd army";
                break;
            case 42:
                yield return after.Tmp0006 == 58 ? "3rd army with militia" : "3rd army";
                break;
            case 49:
                yield return "4th army";
                break;
            case 53:
                yield return "5th army";
                break;
            case 31:
                yield return "Ruberan";
                break;
        }
        if (after.F_Lelyukov_State == 10 && before.F_Lelyukov_State == 1)
            yield return "Lelyukov alone";
        if ((prov is 30 or 31 or 32 or 34 or 38 or 39 or 2 or 13 or 36 && before.Cmdrs[22].Status == 0)
            || (prov is 46 or 47 or 48 && before.Cmdrs[32].Status == 0))
            yield return "leader dead";
        if (before.F_Mineva_State == 0 && after.F_Mineva_State == 1)
            yield return "Mineva met";
        if (prov is 31 or 32 && before.Bt_PlayerParty2 == 0 && after.Bt_EnemyRetreatMode == 1)
            yield return "pass against one unit";
        int[] party = [after.Bt_EnemyParty1.Value, after.Bt_EnemyParty2.Value, after.Bt_EnemyParty3.Value, after.Bt_EnemyParty4.Value];
        if (prov != 45 && party[0] != 0 && party.Skip(1).Any(c => c == 0)
            && Enumerable.Range(22, 38).Any(c => before.Cmdrs[c].Status != 0 && before.Cmdrs[c].Troops < 190 && !party.Contains(c)))
            yield return "filtered";
    }

    /// <summary>
    /// The features one run reached, read from the state before it, after it and at its first hook
    /// call (before that call's scribble; the state after the run when nothing was called), and from
    /// its log.
    /// </summary>
    private delegate IEnumerable<string> FeatureReader(GameState before, GameState after, GameState first, List<string> log);

    private async Task CompareAsync(
        string file,
        Func<ScriptContext, ScenarioScripts, Task> port,
        Action<GameState, Random> aim,
        FeatureReader features,
        string[] expected,
        Action<GameState, Random>? scribble = null,
        int cases = Cases,
        bool unported = false,
        string? label = null,
        bool logPlaces = false)
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, file)))
            return;
        var scenario = new ScenarioScripts(LocalData.ScenarioDirectory);
        var program = scenario.ReadProgram(file);
        if (label is not null)
            program = FromLabel(program, label);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        cases = DifferentialRuns.Cases(cases);

        for (int seed = 1; seed <= cases; seed++)
        {
            var rng = new Random(seed);
            var state = RandomState(rng);
            aim(state, rng);
            var ported = await RunAsync(Copy(state), seed, script => port(script, scenario), null, file, scribble, logPlaces);
            var original = await RunAsync(Copy(state), seed, null, program, file, scribble, logPlaces);

            if (ported.Unported is not null)
            {
                // A path of a later step: the runs are compared up to the port's stop.
                Assert.True(unported, $"seed {seed}: {ported.Unported}");
                int prefix = FirstDifference(original.Log.Take(ported.Log.Count).ToList(), ported.Log);
                Assert.True(prefix < 0, $"seed {seed}: the runs differ at entry {prefix} before the port's '{ported.Unported}'\n{Around(file, original.Log, ported.Log, prefix)}");
                string where = ported.Unported[(ported.Unported.IndexOf(':') + 1)..].Split(':')[0];
                foreach (string feature in features(state, ported.State, ported.AtFirstCall ?? ported.State, ported.Log).Append($"unported {where}").Distinct())
                    seen[feature] = seen.GetValueOrDefault(feature) + 1;
                continue;
            }

            int at = FirstDifference(original.Log, ported.Log);
            Assert.True(at < 0, $"seed {seed}: the runs differ at entry {at}\n{Around(file, original.Log, ported.Log, at)}");
            var differences = Differences(file, original.State.Memory, ported.State.Memory).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));

            foreach (string feature in features(state, ported.State, ported.AtFirstCall ?? ported.State, ported.Log).Distinct())
                seen[feature] = seen.GetValueOrDefault(feature) + 1;
        }

        output.WriteLine($"{file}: {cases} runs");
        foreach (var (feature, count) in seen.OrderBy(f => f.Key, StringComparer.Ordinal))
            output.WriteLine($"  {feature}: {count}");
        var missed = expected.Where(e => !seen.ContainsKey(e)).ToList();
        DifferentialRuns.AssertReached(missed, output);
    }

    /// <summary>
    /// The program with its first two statements turned into <c>\label:</c> and <c>%0:</c>: runs
    /// <paramref name="label"/> as a label call and returns from the file after it.
    /// </summary>
    private static ScriptProgram FromLabel(ScriptProgram program, string label)
    {
        var statements = program.Statements.ToList();
        statements[0] = new CallLabelStatement(statements[0].At, label, program.Labels[label]);
        statements[1] = new ReturnStatement(statements[1].At);
        return new ScriptProgram(program.File, statements, program.Labels);
    }

    /// <summary>A run: the state after it, its log, the state at its first call, and the port's stop at a later step's path.</summary>
    private sealed record Run(GameState State, List<string> Log, GameState? AtFirstCall, string? Unported = null);

    private static async Task<Run> RunAsync(
        GameState state, int seed, Func<ScriptContext, Task>? port, ScriptProgram? program, string file,
        Action<GameState, Random>? scribble, bool logPlaces)
    {
        var log = new List<string>();
        var hooks = new ScribblingHooks(state, seed, log, scribble);
        var input = new NoInput(log);
        var view = new LoggingView(log, logPlaces ? state : null);
        var random = new LoggingRandom(seed, log);
        try
        {
            if (port is not null)
                await port(new ScriptContext(state, random, view, input, hooks));
            else
            {
                var runner = new SceneRunner(new OneProgram(file, program!), hooks, f => f == file);
                var script = new ScriptContext(state, random, view, input, runner);
                runner.Bind(script);
                await runner.RunFileAsync(file);
            }
            log.Add("return");
        }
        catch (ScenarioJumpException e)
        {
            log.Add($"jump {e.File}");
        }
        catch (NotSupportedException e) when (port is not null)
        {
            return new Run(state, log, hooks.AtFirstCall, e.Message);
        }
        return new Run(state, log, hooks.AtFirstCall);
    }

    /// <summary>The army of a <c>FIGT_B</c> call, told by the priority list it was picked from.</summary>
    private static string Army(string call) =>
        call.Contains(" Tmp 3,5,4,0,", StringComparison.Ordinal) ? "A"
        : call.Contains(" Tmp 1,2,6,7,0,", StringComparison.Ordinal) ? "B"
        : call.Contains(" Tmp 1,2,3,6,4,5,7,", StringComparison.Ordinal) ? "C"
        : "?";

    private static IEnumerable<string> RebelTurnFeatures(GameState before, GameState after, List<string> log)
    {
        var battles = log.Where(e => e.StartsWith("call FIGT_B.ADV", StringComparison.Ordinal)).ToList();
        foreach (string call in battles)
        {
            string prov = call[(call.IndexOf(" Prov_Cur ", StringComparison.Ordinal) + 10)..].Split(' ')[0];
            yield return $"{Army(call)} at {prov}";
            if (Army(call) == "C" && before.Provs[5].Owner != 1)
                yield return "C after B";
        }
        if (battles.Count == 2)
            yield return "two attacks";
        if (battles.Count == 0)
            yield return "no attack";
        if (log.Any(e => e.StartsWith("call Gｴｸｽ.ADV", StringComparison.Ordinal)))
            yield return "call Gｴｸｽ.ADV";
        if (log.Any(e => e.StartsWith("jump ", StringComparison.Ordinal)))
            yield return "jump";
        if (before.Cmdrs[0].Status != 0)
            yield return "record 0 picked";
    }

    private static IEnumerable<string> RebelDefenderFeatures(GameState before, GameState after, List<string> log)
    {
        int prov = before.Prov_Cur.Value;
        string army = after.Tmp0003.Value switch { 4 => "A", 6 => "B", _ => "C" };
        yield return prov is >= 2 and <= 8 ? $"{army} at {prov}" : $"{army} elsewhere";
        if (army != "C" && after.CallArg < 100)
            yield return $"stale {army}";
        if (after.Bt_EnemyParty1 == 0)
            yield return "empty party";
        if (after.Bt_EnemyParty4 != 0)
            yield return "full party";
        if (prov == 6 && after.Bt_WallBonus != 0)
            yield return "walls";
    }

    private static int FirstDifference(List<string> a, List<string> b)
    {
        for (int i = 0; i < Math.Min(a.Count, b.Count); i++)
            if (a[i] != b[i])
                return i;
        return a.Count == b.Count ? -1 : Math.Min(a.Count, b.Count);
    }

    private static string Around(string file, List<string> original, List<string> port, int at)
    {
        var sb = new StringBuilder();
        for (int i = Math.Max(0, at - 8); i < at + 3; i++)
        {
            sb.Append($"  {i,5} {file}: {(i < original.Count ? original[i] : "-")}\n");
            sb.Append($"  {i,5} port: {(i < port.Count ? port[i] : "-")}\n");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Every unbound variable and the record pages random (commander record 0, which the picker
    /// and the troop sum read, among them), then: the rebel commanders 1-7 alive or not, with troops
    /// around the filler's 190 and the armies' 100 and 500 (near the 16-bit ceiling now and then),
    /// provinces 1-8 held by the player, the rebels or another owner, the walls of province 6, and a
    /// party and retreat mode left by an earlier battle.
    /// </summary>
    private static GameState RandomState(Random rng)
    {
        var state = new GameState();
        double zero = rng.NextDouble() * 0.6;
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (!GameState.IsBound(v))
                state.Memory.Var(v) = (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(5) == 0 ? rng.Next(0, 3000) : rng.Next(0, 12));
        for (int i = 0; i < state.Memory.PageSize(1); i++)
            state.Memory.WritePage(1, i, (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(0, 4)));
        for (int s = 0; s < state.Strings.Length; s++)
            state.Strings[s] = $"s{s}";

        for (int c = 0; c <= 7; c++)
        {
            if (c == 0 && rng.Next(3) != 0)
                continue;
            var cmdr = state.Cmdrs[c];
            cmdr.Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
            cmdr.Troops = (Sys3Int)(rng.Next(12) switch
            {
                0 => rng.Next(60000, 65536),
                1 => rng.Next(0, 3000),
                2 => rng.Next(185, 195),
                _ => rng.Next(0, 400),
            });
        }
        for (int p = 1; p <= 8; p++)
            state.Provs[p].Owner = (Sys3Int)(rng.Next(8) switch { < 3 => 1, < 7 => 2, _ => rng.Next(0, 9) });
        state.Provs[6].BattleArea = (Sys3Int)rng.Next(0, 1000);
        state.Provs[6].Defense = (Sys3Int)rng.Next(0, 30);
        state.Bt_EnemyParty1 = (Sys3Int)rng.Next(0, 200);
        state.Bt_EnemyParty2 = (Sys3Int)rng.Next(0, 200);
        state.Bt_EnemyParty3 = (Sys3Int)rng.Next(0, 200);
        state.Bt_EnemyParty4 = (Sys3Int)rng.Next(0, 200);
        state.Bt_EnemyRetreatMode = (Sys3Int)(rng.Next(3) switch { 0 => 20, 1 => 50, _ => rng.Next(0, 7) });
        state.Bt_TroopLimit = (Sys3Int)rng.Next(0, 6000);
        state.Bt_WallBonus = (Sys3Int)rng.Next(0, 30);
        return state;
    }

    private static GameState Copy(GameState source)
    {
        var copy = new GameState();
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            copy.Memory.Var(v) = source.Memory.Var(v);
        foreach (int page in source.Memory.AllocatedPages)
            for (int i = 0; i < source.Memory.PageSize(page); i++)
                copy.Memory.WritePage(page, i, source.Memory.ReadPage(page, i));
        Array.Copy(source.Strings, copy.Strings, source.Strings.Length);
        return copy;
    }

    /// <summary>The file under test, read once.</summary>
    private sealed class OneProgram(string file, ScriptProgram program) : ISceneSource
    {
        public ScriptProgram ReadProgram(string name) =>
            name == file ? program : throw new InvalidOperationException($"{name} is a hook here.");
    }

    /// <summary>
    /// Draws from a generator of the seed, logged with their range: <c>Y 4, n</c> with n of 0 or 1
    /// gives n without drawing, as the engine's.
    /// </summary>
    private sealed class LoggingRandom(int seed, List<string> log) : IScriptRandom
    {
        private readonly Random _rng = new(seed * 17 + 3);

        public Sys3Int Next(Sys3Int n)
        {
            var value = n.Value <= 1 ? n : (Sys3Int)_rng.Next(1, n.Value + 1);
            log.Add($"rnd {n} {value}");
            return value;
        }
    }

    /// <summary>The nation files read no input but key waits and the map's pauses (logged, answered with no key): every other request fails.</summary>
    private sealed class NoInput(List<string> log) : ISceneInput
    {
        public Task<Sys3Int> WaitForKeyAsync()
        {
            log.Add("IK 0");
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            log.Add($"Y 3, {ticks}");
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<MouseState> GetMouseAsync() => throw new InvalidOperationException("IM");

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => throw new InvalidOperationException("IC");

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => throw new InvalidOperationException("]");

        public Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException("SR 0");

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");

        public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");

        public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");

        public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");
    }

    /// <summary>
    /// The other files as hooks: each call is logged with what it sees (the party, the retreat
    /// mode, the cursors, the priority list in scratch, the army name), then writes what a battle or
    /// a scene writes — the same writes for the same (seed, call) in both runs; one battle in
    /// twelve loses Leazas Castle and jumps to the ending; a file's own <paramref name="scribble"/>
    /// writes what its scenes may change besides.
    /// </summary>
    private sealed class ScribblingHooks(GameState state, int seed, List<string> log, Action<GameState, Random>? scribble) : IStoryHooks
    {
        private int _calls;

        /// <summary>The state at the first call, before its writes; null while nothing was called.</summary>
        public GameState? AtFirstCall { get; private set; }

        public Task RunEventAsync(string file, Sys3Int entry)
        {
            if (state.Ev_Entry != entry)
                throw new InvalidOperationException($"{file}: Ev_Entry {state.Ev_Entry.Value}, entry {entry.Value}");
            AtFirstCall ??= Copy(state);
            log.Add($"call {file} entry {entry} RetVal {state.RetVal} CallArg {state.CallArg} Cmdr_Cur {state.Cmdr_Cur} " +
                    $"Prov_Cur {state.Prov_Cur} Tmp {state.Tmp0001},{state.Tmp0002},{state.Tmp0003},{state.Tmp0004},{state.Tmp0005}," +
                    $"{state.Tmp0006},{state.Tmp0007},{state.Tmp0008},{state.Tmp0009} Phase {state.Ev_PhaseNation} " +
                    $"Party {state.Bt_EnemyParty1},{state.Bt_EnemyParty2},{state.Bt_EnemyParty3},{state.Bt_EnemyParty4} " +
                    $"Retreat {state.Bt_EnemyRetreatMode} Rebels {state.F_LeazasRebels_State} Str10 {state.Strings[10]}");

            var rng = new Random(seed * 1000 + ++_calls);
            if (file == "FIGT_B.ADV" && rng.Next(12) == 0)
            {
                state.EndingId = 2;
                ScriptContext.JumpTo("OVER.ADV");
            }
            if (rng.Next(2) == 0)
                state.RetVal = (Sys3Int)rng.Next(0, 6);
            if (rng.Next(2) == 0)
                state.CallArg = (Sys3Int)rng.Next(0, 1000);
            for (int v = 1; v <= 9; v++)
                if (rng.Next(3) == 0)
                    state.Memory.Var(v) = (Sys3Int)rng.Next(0, 600);
            if (rng.Next(2) == 0)
                state.Cmdr_Cur = (Sys3Int)rng.Next(0, 200);
            if (rng.Next(3) == 0)
                state.Prov_Cur = (Sys3Int)rng.Next(0, 101);
            if (rng.Next(2) == 0)
                state.Bt_EnemyRetreatMode = (Sys3Int)(rng.Next(2) == 0 ? 20 : rng.Next(0, 60));
            if (rng.Next(3) == 0)
                state.Bt_EnemyParty1 = (Sys3Int)rng.Next(0, 8);
            if (rng.Next(6) == 0)
                state.Ev_Entry = (Sys3Int)rng.Next(0, 30);
            if (rng.Next(4) == 0)
                state.F_LeazasRebels_State = (Sys3Int)rng.Next(0, 3);
            for (int c = 1; c <= 7; c++)
            {
                if (rng.Next(4) == 0)
                    state.Cmdrs[c].Status = (Sys3Int)rng.Next(0, 3);
                if (rng.Next(3) == 0)
                    state.Cmdrs[c].Troops = (Sys3Int)rng.Next(0, 600);
            }
            for (int p = 1; p <= 8; p++)
                if (rng.Next(5) == 0)
                    state.Provs[p].Owner = (Sys3Int)(rng.Next(2) + 1);
            scribble?.Invoke(state, rng);
            return Task.CompletedTask;
        }
    }

    /// <summary>
    /// Logs every event with its lists spelled out; with <paramref name="places"/>, a map call or a
    /// message also with the province under the cursor, its owner and the head of the priority list
    /// (the army whose capture it reports).
    /// </summary>
    private sealed class LoggingView(List<string> log, GameState? places = null) : IScriptView
    {
        public Task ShowAsync(ScriptEvent e)
        {
            if (e is ScreenEvent)
                return Task.CompletedTask;                              // the port's semantic events: no statement in the script
            string entry = e switch
            {
                ValueMessageEvent v => $"ValueMessageEvent {v.Text} [{string.Join(",", v.Values)}] [{string.Join(",", v.StringVariables ?? [])}]",
                MessageEvent m => $"MessageEvent {m.Text} [{string.Join(",", m.StringVariables ?? [])}]",
                EngineEvent g => $"EngineEvent {g.Command} [{string.Join(",", g.Args)}]",
                _ => e.ToString(),
            };
            if (places is not null && e is MapEvent or MessageEvent)
                entry += $" Prov_Cur {places.Prov_Cur} Owner {places.Provs[places.Prov_Cur].Owner} Tmp0001 {places.Tmp0001}";
            log.Add(entry);
            return Task.CompletedTask;
        }
    }

    private static IEnumerable<string> Differences(string file, ScriptMemory expected, ScriptMemory actual)
    {
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (expected.Var(v) != actual.Var(v))
                yield return $"VAR{v:D4} {VariableCatalog.Variables[v].Name}: {file} {expected.Var(v).Value}, port {actual.Var(v).Value}";
        foreach (int page in expected.AllocatedPages)
            for (int i = 0; i < expected.PageSize(page); i++)
                if (expected.ReadPage(page, i) != actual.ReadPage(page, i))
                    yield return $"page {page}[{i}]: {file} {expected.ReadPage(page, i).Value}, port {actual.ReadPage(page, i).Value}";
        for (int s = 0; s < expected.Strings.Length; s++)
            if (expected.Strings[s] != actual.Strings[s])
                yield return $"string {s}: {file} \"{expected.Strings[s]}\", port \"{actual.Strings[s]}\"";
    }
}
