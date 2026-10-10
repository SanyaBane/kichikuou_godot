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
/// <see cref="HelmanTurn"/>, <see cref="HelmanDefenders"/>, <see cref="PattonTurn"/>, <see cref="ZethTurn"/>, <see cref="ZethDefenders"/>, on the template
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
        int cases = Cases)
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, file)))
            return;
        var scenario = new ScenarioScripts(LocalData.ScenarioDirectory);
        var program = scenario.ReadProgram(file);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        cases = DifferentialRuns.Cases(cases);

        for (int seed = 1; seed <= cases; seed++)
        {
            var rng = new Random(seed);
            var state = RandomState(rng);
            aim(state, rng);
            var ported = await RunAsync(Copy(state), seed, script => port(script, scenario), null, file, scribble);
            var original = await RunAsync(Copy(state), seed, null, program, file, scribble);

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

    private sealed record Run(GameState State, List<string> Log, GameState? AtFirstCall);

    private static async Task<Run> RunAsync(
        GameState state, int seed, Func<ScriptContext, Task>? port, ScriptProgram? program, string file,
        Action<GameState, Random>? scribble)
    {
        var log = new List<string>();
        var hooks = new ScribblingHooks(state, seed, log, scribble);
        var input = new NoInput(log);
        var view = new LoggingView(log);
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

    /// <summary>Logs every event with its lists spelled out.</summary>
    private sealed class LoggingView(List<string> log) : IScriptView
    {
        public Task ShowAsync(ScriptEvent e)
        {
            if (e is ScreenEvent)
                return Task.CompletedTask;                              // the port's semantic events: no statement in the script
            log.Add(e switch
            {
                ValueMessageEvent v => $"ValueMessageEvent {v.Text} [{string.Join(",", v.Values)}] [{string.Join(",", v.StringVariables ?? [])}]",
                MessageEvent m => $"MessageEvent {m.Text} [{string.Join(",", m.StringVariables ?? [])}]",
                EngineEvent g => $"EngineEvent {g.Command} [{string.Join(",", g.Args)}]",
                _ => e.ToString(),
            });
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
