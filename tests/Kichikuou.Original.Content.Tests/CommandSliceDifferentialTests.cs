using Kichikuou.Content.Scripts;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;
using Kichikuou.TestSupport;
using Xunit.Abstractions;
using static Kichikuou.TestSupport.Differential;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// The hand-ported prologue slices of the command files (<see cref="EnemyCityCommand"/>,
/// <see cref="DistantCityCommand"/>, <see cref="SubordinatesCommand"/>, <see cref="HaremCommand"/>, <see cref="MerchantCommand"/>, <see cref="OwnCityCommand"/>,
/// <see cref="NinjaCommand"/>, <see cref="ConstructionCommand"/>, <see cref="TreasureCommand"/>, <see cref="AllDataCommand"/>,
/// <see cref="DungeonCommand"/>) and the pickers (<see cref="DeploymentScreen"/>, <see cref="SubordinateScreen"/>,
/// <see cref="HaremScreen"/>, <see cref="ItemScreen"/>) against the files themselves run from the player's decompile by
/// <see cref="SceneRunner"/>, on random states (mostly the prologue, <c>StoryStage</c> 0) and
/// random input: both runs must make the same input requests, call the same files with the same
/// <c>Ev_Entry</c>, draw the same rolls, show the same events and leave the same memory. Every other
/// file is a hook that scribbles over cursors, scratch and the variables the slice reads after it
/// (the pickers answer a unit or a girl). Where the port reaches a path it does not port yet it
/// fails with <see cref="NotSupportedException"/>; the run is then checked up to that point
/// (the original's log must begin with the port's). Skipped without the decompile.
/// </summary>
public class CommandSliceDifferentialTests(ITestOutputHelper output)
{
    private const int Cases = 1500;

    [Fact]
    public Task The_enemy_city_runs_exactly_as_Cﾃｷﾄｼ() =>
        CompareAsync(
            "Cﾃｷﾄｼ.ADV",
            (script, scenario) => new EnemyCityCommand(script, scenario).RunAsync(),
            ShapeEnemyCity,
            Scribble,
            ["call Iﾄｳｿﾞｸ.ADV", "call ｵｰﾌﾟﾆﾝｸﾞ.ADV", "call Cﾆﾝｼﾞｬ.ADV", "call GAL.ADV", "call Gｶﾌｪ.ADV", "call Gﾏﾘｱ.ADV",
             "text 28", "text 34", "text 4368", "text 4404", "item 52", "item 58", "item 64", "item 70", "item 76", "IZ", "M 0",
             // the attack: Patton, the stage-2 gate, the picker, the defenders, the battle
             "call Gﾊﾟｯﾄﾝ.ADV", "call Iｼﾚｲﾌﾞ.ADV", "call Sﾌﾞﾀｲ.ADV", "call TCﾘｰｻﾞｽ2.ADV", "call TJAPAN2.ADV", "call Tｾﾞｽ2.ADV",
             "call Tﾍﾙﾏﾝ2.ADV", "call Tｹｲﾌﾞﾘｽ2.ADV", "call Gﾊﾟﾊﾟｲｱ.ADV", "call FIGT_A.ADV", "fleeing soldier",
             "text 139", "text 162", "text 205", "text 231", "text 255", "text 281", "text 300", "text 311", "text 399",
             // the refusals, the Land of Death and the barrier
             "text 3834", "text 3868", "text 3953", "text 4014", "text 4064", "text 4138", "text 4256", "M 1", "M 3", "M 4",
             // the independent cities: their exits, the menu, Stop, the way to a negotiation, Use Force
             "call Iｼｬﾝｸﾞﾘﾗ.ADV", "call Gｻｰﾅｷｱ.ADV", "call Gﾚﾍﾞｯｶ.ADV", "call Gｶｾｲ.ADV", "item 493", "item 498", "item 502",
             "text 448", "text 476", "text 510", "text 532", "text 2442", "text 2689", "text 3652", "text 595"],
            cases: 5000);

    [Fact]
    public Task The_independent_cities_negotiate_exactly_as_Cﾃｷﾄｼ() =>
        CompareAsync(
            "Cﾃｷﾄｼ.ADV",
            (script, scenario) => new EnemyCityCommand(script, scenario).RunAsync(),
            ShapeNegotiation,
            Scribble,
            [// Mars (22), Diphtheria (19), Rockearth (17), Ice (18)
             "text 637", "item 810", "item 814", "text 822", "text 859", "text 924",
             "text 960", "item 1258", "item 1262", "text 1271", "text 1327", "text 1434",
             // Lazeal (20): the question, the wait, the scene
             "text 1545", "item 1665", "item 1669", "text 1677", "text 1699", "text 1849", "call Gﾚｨﾘｨ.ADV",
             // Red (23), Portugal (28), Zeo (24)
             "text 1985", "item 2085", "item 2089", "text 2097", "text 2149",
             "text 2206", "text 2306", "item 2319", "item 2323", "text 2332", "text 2375", "text 2386", "text 2701",
             // M Land (25), Hanna (26)
             "text 2814", "item 3022", "item 3026", "text 3034", "text 3082",
             "text 3314", "text 3491", "item 3504", "item 3508", "text 3518", "text 3529", "text 3544", "text 3611",
             "M 0", "M 3", "M 4", "M 10"],
            cases: 3000,
            choose: ChooseNegotiation);

    /// <summary>
    /// A city that negotiates at story stage 2 or 3 — Rockearth (17), Ice (18), Diphtheria (19),
    /// Lazeal (20), Mars (22), Red (23), Zeo (24), M Land (25), Hanna (26), Portugal (28) — held by
    /// the cities, now and then any province they hold (the dispatch's fall-through); the DX
    /// Association's state, Lazeal's week around the current one, gold around the demands of
    /// Portugal (90) and Hanna (29, 30), Gengorou (commander 8) alive or not.
    /// </summary>
    private static void ShapeNegotiation(GameState state, Random rng)
    {
        int[] cities = [17, 18, 19, 20, 22, 23, 24, 25, 26, 28];
        state.Prov_Cur = (Sys3Int)(rng.Next(20) == 0 ? rng.Next(0, 100) : cities[rng.Next(cities.Length)]);
        state.Provs[state.Prov_Cur].Owner = 3;
        state.StoryStage = (Sys3Int)rng.Next(2, 4);
        state.Ev_CommandMode = Sys3Int.Zero;
        state.Ui_CmdAttackAvail = 1;
        state.F_Merchant_State = (Sys3Int)(rng.Next(6) == 0 ? 1 : rng.Next(2, 4));
        state.F_DX_State = (Sys3Int)(rng.Next(4) == 0 ? 100 : rng.Next(0, 3));
        state.DXTalk_RetryWeek = (Sys3Int)(rng.Next(3) == 0 ? 0 : state.Week.Value + rng.Next(-5, 6));
        state.Gold = (Sys3Int)(rng.Next(3) switch { 0 => rng.Next(27, 32), 1 => rng.Next(88, 93), _ => rng.Next(0, 3000) });
        state.Cmdrs[8].Status = (Sys3Int)rng.Next(0, 2);
    }

    /// <summary>
    /// The city's menus: half the time Attack, then Negotiate; else at random. Once the run is long
    /// (the talks are) Return and Stop, but either answer of a city's question.
    /// </summary>
    private static int ChooseNegotiation(IReadOnlyList<SceneMenuItem> items, Random rng, bool tired)
    {
        if (tired)
            return items.Any(i => i.At.Line is 81 or 502) ? items.Count : rng.Next(1, items.Count + 1);
        foreach (int line in new[] { 64, 493 })
            for (int i = 0; i < items.Count; i++)
                if (items[i].At == new ScriptRef("Cﾃｷﾄｼ.ADV", line) && rng.Next(2) == 0)
                    return i + 1;
        return rng.Next(6) == 0 ? 0 : rng.Next(1, items.Count + 1);
    }

    /// <summary>
    /// A city of the prologue now and then (Pillage, the AL Church), mostly an attack later on:
    /// the rebels' provinces, the mountain passes and their neighbours, the Land of Death, the
    /// stage-2 gate, JAPAN's 56, the independent cities (17-29, mostly held by them); the flags of
    /// every refusal and exit, Patton's list alive or not, the merchant's, Shangri-La's and the DX
    /// Association's states.
    /// </summary>
    private static void ShapeEnemyCity(GameState state, Random rng)
    {
        state.Prov_Cur = (Sys3Int)(rng.Next(17) switch
        {
            < 3 => 33 + rng.Next(2),
            3 => 77,
            < 7 => rng.Next(3, 8),
            7 => 31 + rng.Next(2),
            8 => 82,
            9 => 56,
            10 => rng.Next(4) switch { 0 => 78, 1 => 89, 2 => 90, _ => 91 },
            < 14 => rng.Next(3) == 0 ? rng.Next(3) switch { 0 => 17, 1 => 22, _ => 28 } : rng.Next(17, 30),
            _ => rng.Next(0, 100),
        });
        state.N_ALChurch_Stage = (Sys3Int)rng.Next(0, 25);
        state.StoryStage = (Sys3Int)(rng.Next(5) switch { 0 => 0, < 3 => 1, 3 => 2, _ => rng.Next(1, 5) });
        if (state.StoryStage != 0 && state.Prov_Cur != 77)
            state.Ev_CommandMode = Sys3Int.Zero;
        var city = state.Provs[state.Prov_Cur];
        city.Owner = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 13) : state.StoryStage == 1 || rng.Next(3) == 0 ? 2 : rng.Next(4, 12));
        if (state.Prov_Cur.Value is >= 17 and <= 29 && rng.Next(4) != 0)
        {
            city.Owner = 3;
            if (state.StoryStage == 1 || state.StoryStage == 0)
                state.StoryStage = (Sys3Int)rng.Next(2, 4);
        }
        state.F_Merchant_State = (Sys3Int)rng.Next(0, 4);
        state.F_ShangriLa_State = (Sys3Int)rng.Next(0, 2);
        state.F_DX_State = (Sys3Int)(rng.Next(3) == 0 ? 100 : rng.Next(0, 3));
        foreach (int p in new[] { 30, 31, 32, 33, 34, 36 })
            if (p != state.Prov_Cur.Value && rng.Next(3) == 0)
                state.Provs[p].Owner = 1;
        state.Ui_CmdAttackAvail = (Sys3Int)(rng.Next(6) == 0 ? 0 : 1);
        state.F_LeazasRebels_State = (Sys3Int)(rng.Next(5) == 0 ? 1 : 2);
        state.F_Helman_War = (Sys3Int)rng.Next(0, 3);
        state.F_Zeth_War = (Sys3Int)rng.Next(0, 3);
        state.F_DemonWar_Stage = (Sys3Int)(rng.Next(2) == 0 ? 12 : rng.Next(0, 13));
        state.F_Patton_State = (Sys3Int)(rng.Next(4) switch { 0 => 10, 1 => 20, _ => rng.Next(0, 13) });
        state.F_Aristoles_Helman2ndState = (Sys3Int)rng.Next(0, 12);
        state.F_Japan_State = (Sys3Int)(rng.Next(3) switch { 0 => 1, 1 => 10, _ => rng.Next(0, 4) });
        state.F_Zeth_State = (Sys3Int)rng.Next(0, 12);
        state.F_ZethBarrier_State = (Sys3Int)rng.Next(0, 3);
        state.F_Papaya_State = (Sys3Int)(rng.Next(6) == 0 ? 2 : rng.Next(0, 12));
        state.F_Warg_State = (Sys3Int)rng.Next(0, 3);
        foreach (int c in new[] { 32, 54, 55, 56, 57, 73 })
            state.Cmdrs[c].Status = (Sys3Int)(rng.Next(3) == 0 ? 0 : 1);
    }

    [Fact]
    public Task The_distant_city_runs_exactly_as_Cﾃｷﾄｼ2() =>
        CompareAsync(
            "Cﾃｷﾄｼ2.ADV",
            (script, _) => new DistantCityCommand(script).RunAsync(),
            (state, rng) => state.Prov_Cur = (Sys3Int)rng.Next(0, 100),
            Scribble,
            ["call Cﾆﾝｼﾞｬ.ADV", "call Gｶﾌｪ.ADV", "call Gﾏﾘｱ.ADV", "text 26", "text 32", "text 71", "text 107", "item 50", "item 55", "IZ"]);

    [Fact]
    public Task The_subordinates_command_runs_exactly_as_Cﾌﾞｶ() =>
        CompareAsync(
            "Cﾌﾞｶ.ADV",
            (script, _) => new SubordinatesCommand(script).RunAsync(),
            (state, rng) =>
            {
                state.Ui_CmdSubordAvail = (Sys3Int)rng.Next(0, 2);
                state.F_SillCow_State = (Sys3Int)(rng.Next(6) == 0 ? 3 : 0);
                state.N_RanceSelfTalk = (Sys3Int)rng.Next(0, 25);
                state.Unit48_Identity = (Sys3Int)rng.Next(0, 3);
                // The troop menu of the later stages: the week's two sub-commands, the offers by
                // class and troop attack, full and short units, a budget around the prices.
                if (rng.Next(2) == 0)
                    state.StoryStage = (Sys3Int)rng.Next(1, 5);
                state.CmdAvail_SubTroops = (Sys3Int)(rng.Next(4) == 0 ? 0 : 1);
                state.CmdAvail_SubTalk = (Sys3Int)(rng.Next(4) == 0 ? 0 : 1);
                state.Week = (Sys3Int)rng.Next(9, 120);
                state.Gold = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 40) : rng.Next(0, 3000));
                for (int u = 1; u <= 50; u++)
                {
                    var unit = state.Units[u];
                    unit.Class = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 13) : rng.Next(0, 4));
                    unit.TroopAtk = (Sys3Int)(rng.Next(2) == 0 ? rng.Next(0, 15) : rng.Next(3) switch { 0 => 10, 1 => 11, _ => 8 });
                    unit.MaxTroops = (Sys3Int)rng.Next(0, 3000);
                    unit.Troops = rng.Next(3) switch { 0 => unit.MaxTroops, 1 => (Sys3Int)rng.Next(0, 3000), _ => (Sys3Int)rng.Next(0, unit.MaxTroops.Value + 1) };
                }
            },
            Scribble,
            ["call Sﾌﾞｶ.ADV", "call Gｼｨﾙ.ADV", "call Gﾊﾞｳﾝﾄﾞ.ADV", "call Gｿｳﾙ.ADV", "call Gﾖｳﾍｲ.ADV", "call Gﾊﾆｰ.ADV",
             "call Gｵｳｼﾞ.ADV", "call Gﾛﾚｯｸｽ.ADV", "text 1348", "text 1357", "text 1374", "text 1116", "text 1149",
             "text 1174", "text 1179", "text 1254", "text 1280", "text 1286", "text 1328", "text 199",
             // the troop menu: header, offer and Add More, budget refusals, replenish, talk
             "text 729", "text 738", "text 744", "text 870", "text 883", "text 927", "text 945", "text 979", "text 995",
             "text 1012", "text 1046", "text 1053", "text 1073", "text 1080", "text 1104",
             "item 807", "item 812", "item 817", "item 824", "item 829", "item 836", "item 842", "item 847", "item 904",
             "item 909", "item 914", "item 1026", "item 1030", "M 0", "IZ"],
            cases: 4000);

    [Fact]
    public Task The_harem_runs_exactly_as_Cﾊｰﾚﾑ() =>
        CompareAsync(
            "Cﾊｰﾚﾑ.ADV",
            (script, _) => new HaremCommand(script).RunAsync(),
            ShapeHarem,
            Scribble,
            ["call Sﾊｰﾚﾑ.ADV", "draw 4", "call Gｽｽﾞﾒ.ADV", "call Gｳｪﾝﾃﾞｨ.ADV", "call Gｶﾌｪ.ADV", "text 376", "text 566", "M 0",
             .. HaremScenes.Select(f => "call " + f)],
            cases: 4000);

    /// <summary>Every girl's scene file of <c>Cﾊｰﾚﾑ.ADV:384-546</c>.</summary>
    private static readonly string[] HaremScenes =
    [
        "Gｼｨﾙ.ADV", "Gﾘｱ.ADV", "Gﾏﾘｽ.ADV", "Gｽｽﾞﾒ.ADV", "Gｳｪﾝﾃﾞｨ.ADV", "Gｶﾅﾐ.ADV", "Gﾚｲﾗ.ADV", "Gﾒﾅﾄﾞ.ADV",
        "Gﾊｳﾚｰﾝ.ADV", "Gﾒﾙﾌｪｲｽ.ADV", "Gｿｳﾙ.ADV", "Gｱﾅｾﾙ.ADV", "Gﾏﾘｱ.ADV", "Gｼｽﾞｶ.ADV", "Gﾗﾝ.ADV", "Gﾐﾘ.ADV",
        "Gﾐﾙ.ADV", "Gﾓｺﾓｺ.ADV", "Gｳﾝｶﾞ.ADV", "Gﾒﾘﾑ.ADV", "Gﾚｨﾘｨ.ADV", "Gｱｵｲ.ADV", "Gｼｬﾘｴﾗ.ADV", "Gｱﾐﾗﾝ.ADV",
        "Gｶﾁｭｰｼｬ.ADV", "Gｿﾙﾆｱ.ADV", "Gｻﾃﾗ.ADV", "Gｼﾞｭﾘｱ.ADV", "Gﾗﾌｧﾘｱ.ADV", "Gｱｰﾙｺｰﾄ.ADV", "Gｸﾘｰﾑ.ADV",
        "Gﾙｰﾍﾞﾗﾝ.ADV", "Gｼｰﾗ.ADV", "Gﾁｽﾞｺ.ADV", "Gﾏｼﾞｯｸ.ADV", "Gﾅｷﾞ.ADV", "Gﾏｺﾄ.ADV", "Gﾜｰｸﾞ.ADV", "Gｶｸｻﾝ.ADV",
        "Gｽｹｻﾝ.ADV", "Gｴﾘｻﾞﾍﾞ.ADV", "Gｲｿﾛｸ.ADV", "Gﾒﾙｼｨ.ADV", "Gｺｳﾋﾒ.ADV", "Gﾚﾍﾞｯｶ.ADV", "Gｱﾆｽ.ADV", "Gｴﾚﾅ.ADV",
        "Gｻｲﾄｳ.ADV",
    ];

    /// <summary>
    /// The prologue a third of the time, else a later stage: the intro draw's flags (Suzume and
    /// Wendy met or not, Wendy's state 4, the cafe at 10/11 with the caravan past province 4), Cow
    /// Sill, Lia's counter around its cap, and the girls' flags around every bound of the list.
    /// </summary>
    private static void ShapeHarem(GameState state, Random rng)
    {
        state.F_SillCow_State = (Sys3Int)(rng.Next(8) switch { 0 => 5, 1 => 7, 2 => 8, _ => 0 });
        state.F_Lia_State = (Sys3Int)(rng.Next(4) switch { 0 => 1, 1 => 3, _ => 0 });
        state.N_Lia_Neglect = (Sys3Int)rng.Next(0, 12);
        state.F_Thieves_State = (Sys3Int)rng.Next(0, 4);
        state.Ui_CmdHaremAvail = (Sys3Int)rng.Next(0, 3);
        state.StoryStage = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 5));
        state.F_Suzume_Met = (Sys3Int)rng.Next(0, 2);
        state.F_Wendy_Met = (Sys3Int)rng.Next(0, 2);
        state.F_Wendy_State = (Sys3Int)(rng.Next(3) == 0 ? 4 : rng.Next(0, 8));
        state.F_Wendy_BedroomSeen = (Sys3Int)rng.Next(0, 2);
        state.F_Cafe_State = (Sys3Int)(rng.Next(8) switch { 0 => 10, 1 => 11, _ => rng.Next(0, 13) });
        state.LadyCaravan_Prov = (Sys3Int)rng.Next(0, 9);
        state.F_Sill_State = (Sys3Int)rng.Next(0, 3);
        state.N_Soul_Stage = (Sys3Int)rng.Next(0, 3);
        state.F_Mokomoko_State = (Sys3Int)rng.Next(9, 16);
        state.F_Merim_Dungeon = (Sys3Int)rng.Next(0, 3);
        state.F_Raylee_State = (Sys3Int)rng.Next(0, 12);
        state.F_Aoi_State = (Sys3Int)rng.Next(0, 12);
        state.F_Shariela_State = (Sys3Int)rng.Next(2, 6);
        state.N_Amiran_Stage = (Sys3Int)(rng.Next(4) switch { 0 => 90, 1 => 100, _ => rng.Next(0, 4) });
        state.F_Katyusha_State = (Sys3Int)rng.Next(0, 11);
        state.F_Saulnia_State = (Sys3Int)rng.Next(0, 11);
        state.F_Ruberan_State = (Sys3Int)(rng.Next(3) == 0 ? 100 : rng.Next(2, 6));
        state.F_Helman_FallCause = (Sys3Int)rng.Next(0, 4);
        state.F_Nagi_State = (Sys3Int)rng.Next(9, 13);
        state.N_Kaoru_State = (Sys3Int)(rng.Next(3) == 0 ? 100 : rng.Next(0, 3));
        state.F_Elisabeth_State = (Sys3Int)(rng.Next(3) == 0 ? 10 : rng.Next(0, 3));
        state.F_Isoroku_Fate = (Sys3Int)rng.Next(0, 4);
        state.F_Kouhime_Dead = (Sys3Int)rng.Next(0, 4);
        state.F_Rebecca_State = (Sys3Int)(rng.Next(3) == 0 ? 100 : rng.Next(0, 4));
        state.F_Elena_State = (Sys3Int)rng.Next(4, 9);
    }

    [Fact]
    public Task The_merchant_runs_exactly_as_Cｼｮｳﾆﾝ() =>
        CompareAsync(
            "Cｼｮｳﾆﾝ.ADV",
            (script, _) => new MerchantCommand(script).RunAsync(),
            ShapeMerchant,
            Scribble,
            ["call Gｼｨﾙ.ADV", "call Gﾚﾍﾞｯｶ.ADV", "M 0",
             // the arrivals, Send Away, the contracts and their refusals, the hires, Rebecca
             "text 30", "text 61", "text 1407", "text 1600", "text 134", "text 158", "text 176", "text 250", "text 587",
             "text 307", "text 381", "text 644", "text 728", "text 934",
             // the shop: the budget, every page, the refusal, Buy and Don't Buy
             "text 1062", "text 1075", "text 1299", "text 1311", "text 1323", "text 1338", "text 1352", "text 1366",
             "text 1380", "text 1394", "text 1158", "text 1203", "text 1268",
             "item 95", "item 100", "item 106", "item 113", "item 120", "item 125", "item 266", "item 270", "item 274",
             "item 603", "item 607", "item 611", "item 1088", "item 1095", "item 1102", "item 1109", "item 1116",
             "item 1124", "item 1132", "item 1139", "item 1144", "item 1183", "item 1187"],
            cases: 3000);

    /// <summary>
    /// Every arrival (the merchant's states 0-5, Sill's two scenes), the mercenaries free or not and
    /// hired before or not, a budget around every price (one under it above all), the shop's items
    /// owned or not with their gates, a stale history cursor.
    /// </summary>
    private static void ShapeMerchant(GameState state, Random rng)
    {
        state.F_SillCow_State = (Sys3Int)(rng.Next(10) switch { 0 => 2, 1 => 6, _ => rng.Next(0, 2) * 7 });
        state.F_Merchant_State = (Sys3Int)rng.Next(0, 6);
        state.F_Rebecca_State = (Sys3Int)rng.Next(0, 4);
        state.Units[49].Status = (Sys3Int)(rng.Next(3) == 0 ? 1 : 0);
        state.Units[50].Status = (Sys3Int)(rng.Next(3) == 0 ? 1 : 0);
        state.F_MercA_Lost = (Sys3Int)(rng.Next(4) == 0 ? 1 : 0);
        state.F_MercB_Lost = (Sys3Int)(rng.Next(4) == 0 ? 1 : 0);
        state.F_Louis_HiredBefore = (Sys3Int)rng.Next(0, 2);
        state.F_Cecil_HiredBefore = (Sys3Int)rng.Next(0, 2);
        int[] edges = [0, 1, 28, 29, 30, 38, 39, 40, 78, 79, 80, 108, 109, 110, 498, 499, 500, 798, 799, 998, 999];
        state.Gold = (Sys3Int)(rng.Next(3) switch { 0 => edges[rng.Next(edges.Length)], 1 => rng.Next(0, 150), _ => rng.Next(0, 1500) });
        foreach (int item in new[] { 3, 4, 5, 8, 19, 28, 43, 44 })
            state.Items[item].Holder = (Sys3Int)(rng.Next(4) == 0 ? rng.Next(1, 101) : 0);
        state.N_Freya_State = (Sys3Int)(rng.Next(2) == 0 ? 3 : rng.Next(0, 5));
        state.F_SillClothes_State = (Sys3Int)(rng.Next(3) == 0 ? 1 : 0);
        state.Units[2].Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : 1);
        state.Units[32].Status = (Sys3Int)(rng.Next(4) == 0 ? 0 : 1);
        state.FogCard_Price = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 300));
        state.Hist_Cur = (Sys3Int)rng.Next(0, 300);
    }

    [Fact]
    public Task The_own_city_runs_exactly_as_Cｼﾊｲﾄｼ() =>
        CompareAsync(
            "Cｼﾊｲﾄｼ.ADV",
            (script, _) => new OwnCityCommand(script).RunAsync(),
            ShapeOwnCity,
            Scribble,
            ["M 0", "draw 3", "draw 5", "draw 10", "item 41", "item 47", "item 53", "item 59", "item 65", "item 71", "item 340",
             "item 436", "item 442", "item 502", "item 735", "item 741", "item 747", "item 753", "item 758", "item 762",
             "item 1583", "item 1587", "text 17", "text 887", "text 1062", "text 1150", "text 1242", "text 1355", "text 1532",
             "text 1597", "text 1687", "text 1704",
             // the places and the castle
             .. new[]
             {
                 "Gｶﾌｪ", "Gﾏﾘｱ", "Gﾊﾞﾎﾞﾗ", "Gｶﾞﾙﾃｨｱ", "Gｱﾋﾞｧﾄｰﾙ", "Gｱｰﾔ", "Gﾏｹﾝ", "Gｽｽﾞﾒ", "Gｱｵｲ", "Gﾐｷ", "Gﾄｸﾍﾞﾂ", "Gｶﾊﾟｰﾗ",
                 "Gﾚｲ", "Gｶｾｲ", "Gｾﾙ", "Gｼﾉﾀﾞ", "IMﾗﾝﾄﾞ", "Gｱﾘｽ", "Gｱﾃﾅ2", "GDX", "Iｼｬﾝｸﾞﾘﾗ", "Gｴﾚﾅ", "Gﾊﾟﾊﾟｲｱ", "Gﾏｼﾞｯｸ",
                 "Gﾌｳｶ", "Gﾏﾙﾁﾅ", "Gｻｲｾﾞﾙ", "Gｹｲﾌﾞﾜﾝ", "Gｹｲﾌﾞﾆｬﾝ", "GAL", "Cﾒｲｷｭｳ", "Gﾕｳﾚｲ", "Gｿｳﾙ", "Gﾚｨﾘｨ", "Gｱﾆｽ",
                 "Gﾏﾘｽ", "G1", "Gﾚﾘｭｰｺﾌ", "Gﾙｰﾍﾞﾗﾝ",
                 // the levy's triggers
                 "Gﾌﾚｲｱ", "Gﾓｺﾓｺ", "Gﾏｺﾄ", "Gﾒﾙｼｨ", "Gｱﾅｾﾙ", "Gｱﾐﾗﾝ", "Gﾛﾚｯｸｽ", "Gｵｰﾙﾊｳﾝﾄ", "G2", "Gｽﾃｯｾﾙ", "Gｶﾞﾝｼﾞｰ", "Iｾﾞｽ",
             }.Select(f => $"call {f}.ADV")],
            cases: 12000);

    /// <summary>
    /// A city with a place or a levy trigger most of the time (the castle above all), the week's
    /// action mostly free, the economy, defence and girls of the city, the buildings, the flags
    /// around the bounds of the places and triggers, the items the levy can find owned or not.
    /// </summary>
    private static void ShapeOwnCity(GameState state, Random rng)
    {
        int[] places = [1, 1, 1, 1, 2, 4, 10, 11, 15, 17, 18, 20, 22, 23, 24, 25, 26, 29, 34, 35, 35, 36, 40, 41, 42, 42, 43, 43,
                        45, 45, 47, 48, 49, 53, 54, 55, 56, 59, 62, 65, 66, 68, 68, 69, 77, 84, 85, 86];
        state.Prov_Cur = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(0, 100) : places[rng.Next(places.Length)]);
        if (rng.Next(6) == 0)
            state.Freya_TargetProv = state.Prov_Cur;
        if (rng.Next(6) == 0)
            state.LadyCaravan_Prov = state.Prov_Cur;
        state.Ev_CommandMode = (Sys3Int)(rng.Next(15) switch { 0 => 1, 1 => 2, _ => 0 });
        state.Ui_CmdOwnCityAvail = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 3));
        state.Ui_CmdDungeonAvail = (Sys3Int)rng.Next(0, 2);
        var city = state.Provs[state.Prov_Cur];
        city.Economy = (Sys3Int)(rng.Next(6) == 0 ? 0 : rng.Next(1, 12));
        city.Defense = (Sys3Int)rng.Next(0, 13);
        city.Girls = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 4));
        state.Gold = (Sys3Int)rng.Next(0, 3000);
        foreach (var set in new Action<Sys3Int>[]
                 {
                     v => state.Bld_MariaLab = v, v => state.Bld_GirlsAcademy = v, v => state.Bld_Hospital = v,
                     v => state.Bld_MagicLab = v, v => state.Bld_SMTower = v,
                 })
            set((Sys3Int)(rng.Next(3) == 0 ? 1 : 0));
        state.Bld_Library = (Sys3Int)(rng.Next(3) == 0 ? 0 : 1);
        state.F_Miki_InCastle = (Sys3Int)(rng.Next(3) == 0 ? 1 : 0);
        state.N_SpecialRoom_Girls = (Sys3Int)(rng.Next(3) == 0 ? 1 : 0);
        state.F_Lelyukov_State = (Sys3Int)(rng.Next(2) == 0 ? 3 : rng.Next(0, 5));
        state.F_Ruberan_State = (Sys3Int)rng.Next(0, 6);
        state.N_Amiran_Stage = (Sys3Int)rng.Next(0, 3);
        state.F_Merci_State = (Sys3Int)rng.Next(0, 3);
        state.F_Anasel_Fate = (Sys3Int)rng.Next(0, 3);
        state.N_Mars_Score = (Sys3Int)rng.Next(0, 3);
        state.F_MLand_Funded = (Sys3Int)rng.Next(0, 2);
        state.F_Magic_State = (Sys3Int)rng.Next(1, 5);
        state.F_Raylee_State = (Sys3Int)(rng.Next(2) == 0 ? 3 : rng.Next(0, 5));
        state.F_Maria_Tulip = (Sys3Int)(rng.Next(3) == 0 ? 15 : rng.Next(0, 15));
        state.F_Aoi_State = (Sys3Int)(rng.Next(3) == 0 ? 100 : rng.Next(8, 12));
        state.F_DX_State = (Sys3Int)(rng.Next(3) == 0 ? 100 : rng.Next(0, 3));
        state.F_Fuuka_State = (Sys3Int)(rng.Next(3) == 0 ? 19 : rng.Next(0, 3));
        state.F_Martina_State = (Sys3Int)(rng.Next(3) == 0 ? 100 : 0);
        state.N_ALChurch_Stage = (Sys3Int)(rng.Next(3) == 0 ? 100 : 0);
        state.N_Camilla_Stage = (Sys3Int)(rng.Next(3) == 0 ? 100 : 0);
        state.N_Olhound_Stage = (Sys3Int)(rng.Next(3) switch { 0 => 100, 1 => 0, _ => 1 });
        state.F_Cafe_State = (Sys3Int)(rng.Next(4) switch { 0 => 10, 1 => 11, _ => rng.Next(0, 10) });
        state.F_Papaya_State = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(6, 12) : rng.Next(0, 12));
        state.F_ShangriLa_State = (Sys3Int)rng.Next(4, 7);
        state.F_Kapala_State = (Sys3Int)rng.Next(2, 5);
        state.F_Helman_War = (Sys3Int)rng.Next(0, 3);
        state.F_RagarlRitual_State = (Sys3Int)(rng.Next(2) == 0 ? 7 : 0);
        state.F_Stessel_State = (Sys3Int)rng.Next(0, 4);
        state.F_Gandhi_State = (Sys3Int)rng.Next(0, 4);
        state.F_LibraryGhost_State = (Sys3Int)(rng.Next(2) == 0 ? rng.Next(0, 2) * 3 + 3 : rng.Next(0, 7));
        state.N_Soul_Stage = (Sys3Int)(rng.Next(2) == 0 ? 4 : rng.Next(3, 6));
        state.N_Kapala_Rank = (Sys3Int)(rng.Next(2) == 0 ? 0 : 1);
        state.Cmdrs[39].TroopAtk = (Sys3Int)(rng.Next(2) == 0 ? 20 : rng.Next(0, 25));
        foreach (int item in new[] { 10, 20, 24, 25, 31, 32, 35, 40, 45 })
            state.Items[item].Holder = (Sys3Int)(rng.Next(3) == 0 ? 100 : 0);
    }

    [Fact]
    public Task The_ninja_runs_exactly_as_Cﾆﾝｼﾞｬ() =>
        CompareAsync(
            "Cﾆﾝｼﾞｬ.ADV",
            (script, _) => new NinjaCommand(script).RunAsync(),
            ShapeNinja,
            Scribble,
            ["draw 5", "draw 100", "draw 300", "draw 2", "draw 200", "draw 30", "draw 12", "draw 28", "M 0",
             "call Gｶﾅﾐ.ADV", "call Gｺｰﾄﾞ.ADV", "call Gﾌﾚｲｱ.ADV",
             "item 51", "item 57", "item 63", "item 69", "item 75", "item 80", "item 283", "item 288", "item 294", "item 299",
             "text 16", "text 87", "text 119", "text 146", "text 169", "text 203", "text 235", "text 306", "text 373",
             "text 383", "text 393", "text 403", "text 413", "text 419", "text 586", "text 604", "text 608", "text 615",
             "text 625", "text 631", "text 637",
             // each ninja's sabotage, success and failure
             "text 441", "text 450", "text 459", "text 468", "text 477", "text 656", "text 703", "text 774", "text 787",
             "text 807", "text 880", "text 923", "text 991", "text 1027", "text 1096"],
            cases: 6000);

    /// <summary>
    /// The five ninjas free, spent or not recruited (now and then all of them busy), Kanami's scene 4
    /// seen or not; a city of every owner whose sabotage is barred or open (the three wars), owner 8,
    /// Zeth's Code; the Substitute Ninja in stock or not; the commanders a sabotage may hit alive or
    /// not, with troops around the draws.
    /// </summary>
    private static void ShapeNinja(GameState state, Random rng)
    {
        bool busy = rng.Next(15) == 0;
        foreach (var set in new Action<Sys3Int>[]
                 {
                     v => state.F_Kanami_NinjaUsed = v, v => state.F_Freya_NinjaUsed = v, v => state.F_Kaoru_NinjaUsed = v,
                     v => state.F_Wichita_NinjaUsed = v, v => state.F_Saitou_NinjaUsed = v,
                 })
            set((Sys3Int)(busy ? rng.Next(2) == 0 ? 1 : 99 : rng.Next(4) switch { 0 => 1, 1 => 99, _ => 0 }));
        state.F_Kanami_Scene4 = (Sys3Int)(rng.Next(3) == 0 ? 1 : 0);
        state.Prov_Cur = (Sys3Int)rng.Next(0, 100);
        state.Provs[state.Prov_Cur].Owner = (Sys3Int)(rng.Next(8) switch
        {
            0 => 2, 1 => 3, 2 => 4, 3 => 6, 4 => 7, 5 => 7, 6 => 8, _ => rng.Next(0, 13),
        });
        state.F_Japan_State = (Sys3Int)rng.Next(0, 3);
        state.F_Helman_War = (Sys3Int)rng.Next(0, 3);
        state.F_Zeth_War = (Sys3Int)rng.Next(0, 3);
        state.F_Code_State = (Sys3Int)(rng.Next(3) switch { 0 => 0, 1 => 2, _ => rng.Next(0, 6) });
        state.Items[8].Holder = (Sys3Int)(rng.Next(3) == 0 ? 100 : rng.Next(0, 51));
        foreach (int c in Enumerable.Range(21, 31).Concat(Enumerable.Range(70, 13)).Concat(Enumerable.Range(100, 29)))
        {
            var cmdr = state.Cmdrs[c];
            cmdr.Status = (Sys3Int)(rng.Next(3) == 0 ? 0 : 1);
            cmdr.Troops = (Sys3Int)(rng.Next(4) switch { 0 => 0, 1 => rng.Next(1, 200), _ => rng.Next(0, 3000) });
            cmdr.TroopAtk = (Sys3Int)rng.Next(0, 12);
            cmdr.TroopDef = (Sys3Int)rng.Next(0, 12);
            cmdr.Recovery = (Sys3Int)rng.Next(0, 300);
        }
    }

    [Fact]
    public Task The_construction_runs_exactly_as_Cﾃｲﾄ() =>
        CompareAsync(
            "Cﾃｲﾄ.ADV",
            (script, _) => new ConstructionCommand(script).RunAsync(),
            ShapeConstruction,
            Scribble,
            ["M 0", "item 62", "item 66", "item 246", "item 252", "item 258", "item 264", "item 271", "item 277", "item 282",
             "item 355", "item 359", "item 442", "item 446", "item 544", "item 548", "item 725", "item 729", "item 830",
             "item 834", "item 920", "item 924", "item 928",
             "text 8", "text 80", "text 107", "text 111", "text 115", "text 119", "text 123", "text 127", "text 135",
             "text 142", "text 339", "text 426", "text 528", "text 709", "text 814", "text 904", "text 966",
             "text 377", "text 464", "text 566", "text 747", "text 852", "text 946"],
            cases: 4000);

    /// <summary>
    /// The buildings not started, under way or built (now and then all of them and the pool, the dead
    /// opening), a timer running or not, Maria in the army with her tulip, a budget around the prices,
    /// a stale history cursor.
    /// </summary>
    private static void ShapeConstruction(GameState state, Random rng)
    {
        bool all = rng.Next(20) == 0;
        foreach (var set in new Action<Sys3Int>[]
                 {
                     v => state.Bld_GirlsAcademy = v, v => state.Bld_Library = v, v => state.Bld_Hospital = v,
                     v => state.Bld_SMTower = v, v => state.Bld_MariaLab = v, v => state.Bld_MagicLab = v,
                 })
            set((Sys3Int)(all ? 2 : rng.Next(5) switch { 0 => 1, 1 => 2, _ => 0 }));
        state.F_Pool_Built = (Sys3Int)(all ? 2 : rng.Next(0, 3));
        state.BuildWeeksLeft = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(1, 17) : 0);
        state.Units[16].Status = (Sys3Int)(rng.Next(3) == 0 ? 0 : 1);
        state.F_Maria_Tulip = (Sys3Int)rng.Next(0, 4);
        int[] edges = [0, 399, 400, 401, 599, 600, 799, 800, 899, 900, 979, 980, 3000];
        state.Gold = (Sys3Int)(rng.Next(2) == 0 ? edges[rng.Next(edges.Length)] : rng.Next(0, 1500));
        state.Hist_Cur = (Sys3Int)rng.Next(0, 300);
    }

    [Fact]
    public Task The_dungeon_runs_exactly_as_Cﾒｲｷｭｳ() =>
        CompareAsync(
            "Cﾒｲｷｭｳ.ADV",
            (script, scenario) => new DungeonCommand(script, scenario).RunAsync(),
            ShapeDungeon,
            ScribbleDungeon,
            ["draw 3", "draw 4", "draw 10", "draw 20", "draw 5", "draw 40", "call Gﾊﾞｳﾝﾄﾞ.ADV", "call Gﾎﾗｶﾞ.ADV", "call Gｶﾌｪ.ADV",
             "text 2763", "text 2795", "text 2811", "text 2827", "text 2852", "M 0", "M 1", "M 10", "IZ", "boss fight",
             // God's Door: found by either item, its menu, the statues, the door opening, Rance's talk, Kouhime's hint, Iｶﾐ
             "text 3513", "text 3554", "text 3691", "item 290", "text 3866", "text 3879", "item 3890", "item 3897", "item 3904",
             "item 3911", "item 3917", "item 3923", "item 3928", "text 3953", "text 3973", "text 3993", "text 4013", "text 4032",
             "text 4084", "text 4381", "text 4415", "text 4445", "text 4455", "text 4463", "text 4473", "text 4480",
             "call Iｶﾐ.ADV", "jump OVER.ADV",
             // the dispatch, the menu, the picker and the refusals, the week's refusal
             .. new[]
             {
                 "Gﾌﾞﾘﾃｨ", "Gｷｬﾛﾘ", "Gﾊﾟｲｱｰﾙ", "Gｶﾗｰ", "Gﾄｳｹﾞﾝ", "G1", "G2", "Sﾌﾞﾀｲ", "FIGT", "死亡捕獲", "Gｻｲｾﾞﾙ", "Gﾒﾘﾑ", "Gﾌｳｶ",
                 "Gｹｲﾌﾞﾜﾝ", "Gｹｲﾌﾞﾆｬﾝ", "Gﾊﾆｰ", "Gﾐﾘ", "Gﾒﾙﾌｪｲｽ", "Gﾐﾙ", "Gﾙｰﾍﾞﾗﾝ", "Gｱｰﾔ",
             }.Select(f => $"call {f}.ADV"),
             "item 267", "item 272", "item 278", "item 284", "item 295", "text 243", "text 250", "text 254", "text 258", "text 312",
             "text 5064", "text 5139", "text 5218", "text 5260", "text 5315", "text 5350", "text 5377", "text 5423", "text 5509",
             "text 5540", "text 5584",
             // the way, the resume floor, the floors, the moves, the battle, the end
             "text 374", "text 398", "text 427", "text 634", "item 652", "item 657", "text 692", "text 914", "item 866", "item 872",
             "item 878", "item 884", "item 889", "text 1006", "text 1011", "text 1088", "text 1093", "text 1178", "text 1304",
             "text 1347", "text 1359", "text 927", "item 934", "item 938", "text 975",
             // the chests, Orochi, the spring, KD and his quiz, Merim's inscriptions
             "text 1455", "text 1487", "text 1517", "text 1547", "text 1577", "text 1607", "text 1637", "text 1667", "text 1697",
             "text 1727", "text 1755", "text 1786", "text 1825", "text 1854", "text 1884", "text 1913", "text 1381", "text 1940",
             "item 1954", "item 1958", "text 1966", "text 1989", "text 2036", "text 2051", "text 2081", "text 2122", "text 2209",
             "text 2266", "item 2363", "item 2367", "text 2375", "text 2449", "text 2464", "text 2493", "text 2526", "text 2557",
             "text 2586", "text 2620", "text 2634", "text 2699", "text 4600", "text 4736", "text 4856"],
            cases: 8000,
            choose: ChooseInDungeon);

    /// <summary>
    /// <c>FIGT表示</c> (the scenes' scripted battles, every entry) with its 24 calls of
    /// <c>FIGTATTK</c> run by the battle's port (<see cref="Kichikuou.Original.Combat.Battle.PlayExchangeAnimationAsync"/>,
    /// the router's route) against <c>FIGTATTK</c> run from the decompile, on random states: the same
    /// draws and calls and the same memory. The screen events are not compared — the port raises
    /// the battle's own events where the file draws.
    /// </summary>
    [Fact]
    public async Task FIGTATTK_called_from_FIGT表示_runs_as_the_file()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "FIGTATTK.ADV")))
            return;
        var scenario = new ScenarioScripts(LocalData.ScenarioDirectory);
        var programs = new[] { "FIGT表示.ADV", "FIGTATTK.ADV" }.ToDictionary(f => f, scenario.ReadProgram);
        int[] entries = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 21, 22, 23, 24, 26];
        var seen = new HashSet<int>();
        int cases = DifferentialRuns.Cases(1900);
        for (int seed = 1; seed <= cases; seed++)
        {
            var rng = new Random(seed);
            var state = RandomState(rng);
            state.Ev_Entry = (Sys3Int)entries[rng.Next(entries.Length)];
            var mine = await RunCutsceneAsync(Copy(state), seed, programs, port: true);
            var original = await RunCutsceneAsync(Copy(state), seed, programs, port: false);
            int at = FirstDifference(original.Log, mine.Log);
            Assert.True(at < 0, $"seed {seed}: the runs differ at entry {at}\n{Around("FIGTATTK.ADV", original.Log, mine.Log, at)}");
            var differences = Differences("FIGTATTK.ADV", original.State.Memory, mine.State.Memory, v => VariableCatalog.Variables[v].Name).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));
            if (mine.Log.Any(e => e.StartsWith("rnd ", StringComparison.Ordinal)))
                seen.Add(state.Ev_Entry.Value);
        }
        output.WriteLine($"FIGT表示.ADV: {cases} runs, entries with an animation: {string.Join(",", seen.Order())}");
        int[] animated = [2, 3, 4, 5, 6, 7, 11, 12, 13, 14, 26];         // 1, 8-10, 21-24 call no FIGTATTK
        DifferentialRuns.AssertReached(animated.Where(e => !seen.Contains(e)).Select(e => $"entry {e}").ToList(), output);
    }

    /// <summary>
    /// One run of <c>FIGT表示</c>: <c>FIGTATTK</c> by the port or from the decompile, every other
    /// file a logged no-op; the log keeps the draws and the calls only.
    /// </summary>
    private static async Task<(GameState State, List<string> Log)> RunCutsceneAsync(
        GameState state, int seed, Dictionary<string, ScriptProgram> programs, bool port)
    {
        var log = new List<string>();
        var input = new CutsceneInput();
        var hooks = new CutsceneHooks(log, input, port);
        var runner = new SceneRunner(new Programs(programs), hooks, f => f == "FIGT表示.ADV" || (!port && f == "FIGTATTK.ADV"));
        var script = new OriginalScriptContext(state, new LoggingRandom(seed * 7 + 1, log), new LoggingView([], e => e is ScreenEvent), input, runner);
        runner.Bind(script);
        hooks.Script = script;
        try
        {
            await runner.RunEventAsync("FIGT表示.ADV", state.Ev_Entry);
            log.Add("end");
        }
        catch (ScenarioJumpException e)
        {
            log.Add($"jump {e.File}");
        }
        return (state, log);
    }

    private sealed class Programs(Dictionary<string, ScriptProgram> programs) : ISceneSource
    {
        public ScriptProgram ReadProgram(string file) =>
            programs.TryGetValue(file, out var program) ? program : throw new InvalidOperationException($"{file} is a hook here.");
    }

    /// <summary><c>FIGTATTK</c> to the battle's port (when <c>port</c>), every other call logged.</summary>
    private sealed class CutsceneHooks(List<string> log, CutsceneInput input, bool port) : IStoryHooks
    {
        public OriginalScriptContext? Script { get; set; }

        public Task RunEventAsync(string file, Sys3Int entry)
        {
            if (port && file == "FIGTATTK.ADV")
                return new Kichikuou.Original.Combat.Battle(Script!, input).PlayExchangeAnimationAsync();
            log.Add($"call {file} entry {entry}");
            return Task.CompletedTask;
        }
    }

    /// <summary>A cutscene asks for keys and pauses only; the pointer rests.</summary>
    private sealed class CutsceneInput : IGameInput
    {
        private static T No<T>() => throw new InvalidOperationException("The cutscene asked for input.");

        public Task<Sys3Int> WaitForKeyAsync() => Task.FromResult(Sys3Int.Zero);
        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Task.FromResult(Sys3Int.Zero);
        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult(Sys3Int.Zero);
        public Task<Kichikuou.Original.Combat.FigtSelChoice> SelectCommandAsync(Kichikuou.Original.Combat.FigtSelMenu menu) => No<Task<Kichikuou.Original.Combat.FigtSelChoice>>();
        public Task<Sys3Int?> ChooseAsync(Kichikuou.Original.Combat.BattleMenu menu) => No<Task<Sys3Int?>>();
        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items) => No<Task<int>>();
        public Task<MouseState> GetMouseAsync() => Task.FromResult(new MouseState(Sys3Int.Zero, Sys3Int.Zero, Sys3Int.Zero));
        public Task<Sys3Int> SetCursorAsync(Sys3Int shape) => Task.FromResult(Sys3Int.Zero);
        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => No<Task<IReadOnlyList<Sys3Int>>>();
        public Task ResetTimerAsync(Sys3Int value) => Task.CompletedTask;
        public Task<Sys3Int> GetTimerAsync(int mode) => Task.FromResult(Sys3Int.Zero);
        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => No<Task<IReadOnlyList<Sys3Int>>>();
        public Task<Sys3Int> GetMidiTrackAsync() => No<Task<Sys3Int>>();
    }

    /// <summary>
    /// The dungeon's menus: half the time the way further in — Start Search, the saved floor, a move
    /// deeper, the spring, the quiz and its right answers —, else at random; Finish once the run is long.
    /// </summary>
    private static int ChooseInDungeon(IReadOnlyList<SceneMenuItem> items, Random rng, bool tired)
    {
        if (tired)
            return Find(934) is var finish and > 0 ? finish : items.Count;
        foreach (int line in new[] { 272, 290, 3890, 3897, 3904, 3911, 3923, 267, 652, 872, 884, 1954, 2363, 2476, 2513, 2548, 2569, 2600 })
            if (Find(line) is var k and > 0 && rng.Next(2) == 0)
                return k;
        return rng.Next(6) == 0 ? 0 : rng.Next(1, items.Count + 1);

        int Find(int line)
        {
            for (int i = 0; i < items.Count; i++)
                if (items[i].At == new ScriptRef("Cﾒｲｷｭｳ.ADV", line))
                    return i + 1;
            return 0;
        }
    }

    /// <summary>
    /// A dungeon to search most of the time (stage 1 and later), else the hideout, a story spot or
    /// any record; the week's action mostly free; a saved floor around the fixed floors and the
    /// bosses; the bosses' flags, the Snow Charm, the Battleship's key and the treasures owned or
    /// not; Merim and her inscriptions, KD's quest, the Fuuka arc, the Hanny King, the scenes' flags.
    /// </summary>
    private static void ShapeDungeon(GameState state, Random rng)
    {
        state.Cmdrs[100].CgFull = (Sys3Int)(rng.Next(6) == 0 ? 1137 : rng.Next(0, 2000));
        int[] searches = [2, 2, 2, 4, 5, 6, 8, 10, 10, 11, 11, 11, 11, 14, 14, 15, 15, 16, 18, 19, 20, 21, 22, 23, 23, 24, 25, 25, 26, 30, 30, 30];
        state.Dgn_Cur = (Sys3Int)(rng.Next(12) switch
        {
            0 => 1,
            1 => rng.Next(0, 32),
            2 => rng.Next(1, 18),
            _ => searches[rng.Next(searches.Length)],
        });
        state.StoryStage = (Sys3Int)(rng.Next(10) == 0 ? 0 : rng.Next(1, 5));
        state.Ev_CommandMode = (Sys3Int)(rng.Next(12) switch { 0 => 1, 1 => 2, _ => 0 });
        if (state.Ev_CommandMode == 1 && rng.Next(2) == 0)
            state.Dgn_Cur = 10;
        state.Ui_CmdDungeonAvail = (Sys3Int)(rng.Next(10) == 0 ? 0 : 1);
        state.F_Merim_MillArc = (Sys3Int)rng.Next(0, 5);
        state.Cmdr_Cur = (Sys3Int)rng.Next(0, 221);
        if (rng.Next(8) == 0)
            state.Cmdrs[state.Cmdr_Cur].CgFull = 1137;
        state.F_PG9_State = (Sys3Int)rng.Next(0, 4);
        state.N_Soul_Stage = (Sys3Int)(rng.Next(4) == 0 ? 5 : rng.Next(0, 5));
        state.F_HannyKing_State = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 2) : rng.Next(2, 9));
        var dungeon = state.Dgns[state.Dgn_Cur];
        dungeon.State = (Sys3Int)rng.Next(0, 3);
        dungeon.SavedFloor = (Sys3Int)(rng.Next(6) switch
        {
            0 => 0,
            1 => rng.Next(0, 60),
            _ when SavedFloors.TryGetValue(state.Dgn_Cur.Value, out var near) => near[rng.Next(near.Length)],
            _ => rng.Next(0, 3),
        });
        foreach (var set in new Action<Sys3Int>[]
                 {
                     v => state.F_YellowDragon_Beaten = v, v => state.F_RedDragon_Beaten = v, v => state.F_GreenDragon_Beaten = v,
                     v => state.F_BlueDragon_Beaten = v, v => state.F_BlackDragon_Beaten = v, v => state.F_Boss_Marder = v,
                     v => state.F_Boss_StoneGuardian = v, v => state.F_Boss_HawKyu = v, v => state.F_Boss_Musashibou = v,
                     v => state.F_Boss_JapaneseSoldier = v, v => state.F_Boss_ShimoJoe = v, v => state.F_Boss_Vevetar = v,
                     v => state.F_Boss_GGCaterpillar = v,
                 })
            set((Sys3Int)(rng.Next(3) == 0 ? 1 : 0));
        state.F_Gunagan_State = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(1, 3) : 0);
        state.F_Merim_Dungeon = (Sys3Int)(rng.Next(2) == 0 ? 1 : rng.Next(0, 4));
        state.N_Merim_Inscriptions = (Sys3Int)rng.Next(0, 13);
        state.F_SillCow_State = (Sys3Int)(rng.Next(3) == 0 ? 7 : rng.Next(0, 9));
        state.F_GodsDoor_Found = (Sys3Int)(rng.Next(4) == 0 ? 1 : 0);
        state.F_GodsDoor_Quest = (Sys3Int)(rng.Next(2) == 0 ? rng.Next(2, 7) : rng.Next(0, 2));
        foreach (int item in new[] { 11, 17, 21, 22, 28, 29, 30, 33, 34, 36, 38, 39, 42, 45, 48 })
            state.Items[item].Holder = (Sys3Int)(rng.Next(3) switch { 0 => 0, 1 => 100, _ => rng.Next(0, 51) });
        state.F_Battleship_Door = (Sys3Int)rng.Next(0, 2);
        state.F_Arcy_State = (Sys3Int)rng.Next(0, 3);
        state.DateWeek = (Sys3Int)(rng.Next(2) == 0 ? 1 : rng.Next(0, 5));
        state.F_Seizel_State = (Sys3Int)rng.Next(0, 8);
        state.F_Kaybwan_State = (Sys3Int)rng.Next(0, 5);
        state.F_Kaybnyan_State = (Sys3Int)rng.Next(0, 5);
        state.FogCard_Price = (Sys3Int)(rng.Next(2) == 0 ? 0 : 120);
        state.F_Fuuka_State = (Sys3Int)(rng.Next(2) == 0 ? 9 : rng.Next(0, 20));
        state.F_Fuuka_ShrineVisited = (Sys3Int)rng.Next(0, 2);
        state.VAR0427 = (Sys3Int)(rng.Next(2) == 0 ? 3 : rng.Next(0, 5));
        state.F_Wenlina_CureMilli = (Sys3Int)rng.Next(0, 2);
        state.N_Melfeis_Stage = (Sys3Int)rng.Next(8, 13);
        state.F_Ruberan_State = (Sys3Int)rng.Next(0, 2);
        state.N_Aaya_Stage = (Sys3Int)rng.Next(4, 8);
        state.Bld_Hospital = (Sys3Int)rng.Next(0, 2);
        state.DebugMode = (Sys3Int)(rng.Next(10) == 0 ? 1 : 0);
        state.BgmEnabled = (Sys3Int)rng.Next(0, 2);
        for (int u = 1; u <= 50; u++)
        {
            state.Units[u].Item = (Sys3Int)(rng.Next(4) switch { 0 => 10, 1 => rng.Next(1, 3), _ => rng.Next(0, 51) });
            state.Units[u].Troops = (Sys3Int)(rng.Next(4) == 0 ? rng.Next(0, 3) : rng.Next(0, 3000));
        }
        if (state.Dgn_Cur == 11 && rng.Next(2) == 0)
        {
            // KD's statue and quiz: the quest needs it and item 17 is not owned.
            state.F_GodsDoor_Quest = 3;
            state.Items[17].Holder = Sys3Int.Zero;
        }
        if (state.Dgn_Cur == 30 && rng.Next(2) == 0)
        {
            // God's Door: found or about to be (Rance with item 1 or 2), the quest at 2-6, the statues
            // placed, held or lost, Kouhime past 9 or not.
            state.F_GodsDoor_Found = (Sys3Int)rng.Next(0, 2);
            state.F_GodsDoor_Quest = (Sys3Int)(rng.Next(2) == 0 ? 3 : rng.Next(1, 7));
            state.Units[1].Item = (Sys3Int)rng.Next(0, 4);
            state.F_Kou_State = (Sys3Int)rng.Next(9, 12);
            int placed = rng.Next(0, 5);
            foreach (var set in new Action<Sys3Int>[]
                     {
                         v => state.F_Statue_Gourd = v, v => state.F_Statue_Monkey = v,
                         v => state.F_Statue_Sunflower = v, v => state.F_Statue_Bonsai = v,
                     })
                set((Sys3Int)(rng.Next(4) < placed ? 1 : 0));
            for (int item = 15; item <= 18; item++)
                state.Items[item].Holder = (Sys3Int)(rng.Next(4) switch { 0 or 1 => 0, 2 => 1, _ => rng.Next(0, 101) });
        }
    }

    /// <summary>The floors next to each dungeon's fixed floors and bosses, where a search resumes.</summary>
    private static readonly Dictionary<int, int[]> SavedFloors = new()
    {
        [2] = [4, 5, 13, 14], [4] = [11, 12], [6] = [9, 10, 24, 25], [8] = [9, 10, 19, 20, 29, 30], [10] = [9, 10, 26, 27, 28],
        [11] = [4, 5, 9, 10, 14, 15, 19, 20, 24, 25, 29, 30, 30, 30], [14] = [11, 12, 19, 20, 37, 38, 38], [15] = [3, 4, 37, 38],
        [16] = [19, 20], [18] = [26, 27], [19] = [24, 25], [20] = [4, 5, 9, 10, 49, 50, 50], [21] = [14, 15, 16],
        [22] = [12, 13, 49, 50], [23] = [32, 33, 75, 76], [24] = [9, 10, 49, 50], [25] = [1, 2, 7, 8, 26, 41, 65, 66],
        [26] = [200, 201], [30] = [19, 20, 49, 50, 50, 99, 100, 201, 501, 999, 1000],
    };

    /// <summary>
    /// <see cref="Scribble"/>, and for the dungeon: the searcher falls now and then in the battle,
    /// which leaves <c>RetVal</c> anywhere (DGN-02); the picker hands over a refuser now and then.
    /// </summary>
    private static void ScribbleDungeon(string file, GameState state, Random rng, bool tired)
    {
        Scribble(file, state, rng, tired);
        switch (file)
        {
            case "Iｶﾐ.ADV":
                throw new ScenarioJumpException("OVER.ADV");             // it ends only in &#OVER
            case "FIGT.ADV" or "死亡捕獲.ADV":
                if (rng.Next(6) == 0)
                    state.Units[state.Bt_PlayerParty1].Status = Sys3Int.Zero;
                state.RetVal = (Sys3Int)rng.Next(0, 4);
                break;
            case "Sﾌﾞﾀｲ.ADV" when state.Bt_PlayerParty1 != 0 && rng.Next(3) != 0:
                state.Bt_PlayerParty1 = (Sys3Int)(rng.Next(8) switch { 0 => 32, 1 => 41, 2 => 42, _ => 1 });
                break;
            default:
                // A scene of the search may start Mill's part of Merim's story (DGN-11).
                if (rng.Next(8) == 0)
                    state.F_Merim_MillArc = 3;
                break;
        }
    }

    [Fact]
    public Task The_deployment_screen_runs_exactly_as_Sﾌﾞﾀｲ() =>
        CompareAsync(
            "Sﾌﾞﾀｲ.ADV",
            (script, scenario) => new DeploymentScreen(script, scenario).RunAsync(),
            (state, rng) =>
            {
                state.Tmp0021 = (Sys3Int)(rng.Next(8) == 0 ? rng.Next(0, 7) : 4);
                state.Tmp0022 = (Sys3Int)(rng.Next(8) == 0 ? rng.Next(0, 6) : rng.Next(1, 4));
                state.Gold = (Sys3Int)rng.Next(0, 400);
                for (int u = 1; u <= 51; u++)
                {
                    state.Units[u].Status = (Sys3Int)(rng.Next(4) == 0 ? rng.Next(0, 4) : 1);
                    state.Units[u].Troops = (Sys3Int)rng.Next(0, 3000);
                    state.Units[u].DeployCost = (Sys3Int)rng.Next(0, 40);
                    state.Units[u].Class = (Sys3Int)rng.Next(0, 13);
                    state.Units[u].Item = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(1, 51) : 0);
                }
                state.Hist_Cur = (Sys3Int)rng.Next(0, 300);
            },
            Scribble,
            ["text 22", "text 49", "text 79", "text 167", "text 179", "text 255", "text 261", "text 264", "text 329",
             "text 349", "text 370", "text 394", "text 530", "text 551", "text 555", "text 559", "text 563"]);

    [Fact]
    public Task The_subordinate_picker_runs_exactly_as_Sﾌﾞｶ() =>
        CompareAsync(
            "Sﾌﾞｶ.ADV",
            (script, scenario) => new SubordinateScreen(script, scenario).RunAsync(),
            (state, rng) =>
            {
                state.CallArg = (Sys3Int)(rng.Next(4) == 0 ? rng.Next(0, 7) : 1);
                state.RetVal = (Sys3Int)rng.Next(0, 2);
            },
            Scribble,
            ["text 25", "text 71", "text 80", "text 208", "text 213"]);

    [Fact]
    public Task The_harem_picker_runs_exactly_as_Sﾊｰﾚﾑ() =>
        CompareAsync(
            "Sﾊｰﾚﾑ.ADV",
            (script, scenario) => new HaremScreen(script, scenario).RunAsync(),
            (state, rng) =>
            {
                for (int i = 0; i < 50; i++)
                    state.Memory.Var(249 + i) = (Sys3Int)(rng.Next(3) == 0 ? 1 : 0);
                state.F_Mill_State = (Sys3Int)rng.Next(0, 4);
                state.Week = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 65536) : rng.Next(9, 30));
                for (int k = 1; k <= 50; k++)
                    state.History[k].Harem_LastWeek = (Sys3Int)(rng.Next(2) == 0 ? 0 : rng.Next(0, 65536));
            },
            Scribble,
            ["text 14", "text 62", "text 235", "text 241", "text 253", "text 256", "text 260", "text 264", "text 268",
             "text 276", "text 280", "text 283"]);

    [Fact]
    public Task The_item_screen_runs_exactly_as_Sｱｲﾃﾑ() =>
        CompareAsync(
            "Sｱｲﾃﾑ.ADV",
            (script, scenario) => new ItemScreen(script, scenario).RunAsync(),
            ShapeItems,
            Scribble,
            ["M 0", "item 399", "sound 90", "sound 108", "sound 110", "sound 111",
             "text 104", "text 121", "text 185", "text 194", "text 215", "text 225", "text 254", "text 291", "text 301",
             "text 311", "text 322", "text 341", "text 367", "text 540", "text 546", "text 549", "text 556", "text 559",
             "text 587", "text 600", "text 604", "text 607", "text 612"],
            cases: 4000,
            mouse: ItemPointer);

    /// <summary>
    /// The view kept from the last visit (now and then one the file never sets), the units' states,
    /// the items lost, in stock or equipped (each on one unit, which holds it; items 1 and 2 often;
    /// now and then a holder out of range), the alias of <c>Unit_Item[100]</c> and of the bar's 997.
    /// </summary>
    private static void ShapeItems(GameState state, Random rng)
    {
        state.Ui_ItemViewMode = (Sys3Int)(rng.Next(25) == 0 ? rng.Next(2, 4) : rng.Next(0, 2));
        for (int u = 0; u <= 51; u++)
        {
            state.Units[u].Item = Sys3Int.Zero;
            state.Units[u].Status = (Sys3Int)(rng.Next(4) switch { 0 => 0, 1 => rng.Next(2, 4), _ => 1 });
        }
        for (int k = 1; k <= 50; k++)
        {
            var item = state.Items[k];
            item.Effect = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(1, 10));
            int roll = k <= 2 ? rng.Next(2, 6) : rng.Next(6);
            item.Holder = (Sys3Int)(roll switch { 0 or 1 => 0, 2 => 100, _ => 0 });
            if (roll >= 3)
            {
                int u = rng.Next(1, 51);
                if (state.Units[u].Item == 0)
                {
                    state.Units[u].Item = (Sys3Int)k;
                    item.Holder = (Sys3Int)u;
                }
            }
            if (rng.Next(60) == 0)
                item.Holder = (Sys3Int)rng.Next(0, 1200);
        }
        state.Units[48].DgnAtkMod = (Sys3Int)rng.Next(0, 3);
        state.Cmdrs[165].Troops = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 60));
    }

    /// <summary>The pointer of the item screen: mostly over the grid, now and then on the bar or elsewhere; a quarter of the reads press a button.</summary>
    private static MouseState ItemPointer(Random rng)
    {
        int r = rng.Next(20);
        var button = (Sys3Int)(r < 15 ? 0 : r < 19 ? 16 : 32);
        var (x, y) = rng.Next(10) switch
        {
            < 7 => (rng.Next(328, 640), rng.Next(18, 324)),
            < 9 => (rng.Next(320, 640), rng.Next(324, 400)),
            _ => (rng.Next(0, 640), rng.Next(0, 400)),
        };
        return new MouseState(button, (Sys3Int)x, (Sys3Int)y);
    }

    [Fact]
    public Task The_treasure_command_runs_exactly_as_Cﾀｶﾗ() =>
        CompareAsync(
            "Cﾀｶﾗ.ADV",
            (script, _) => new TreasureCommand(script).RunAsync(),
            (state, rng) =>
            {
                for (int k = 1; k <= 50; k++)
                    state.Items[k].Holder = (Sys3Int)(rng.Next(6) switch { 0 => 0, 1 => 100, 5 => rng.Next(0, 1200), _ => rng.Next(1, 52) });
                for (int u = 1; u <= 51; u++)
                    state.Units[u].Status = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 3));
            },
            Scribble,
            ["call Sｱｲﾃﾑ.ADV"],
            cases: 500);

    [Fact]
    public Task The_all_data_screen_runs_exactly_as_各種DATA() =>
        CompareAsync(
            "各種DATA.ADV",
            (script, _) => new AllDataCommand(script).RunAsync(),
            ShapeAllData,
            Scribble,
            ["item 28", "item 316", "IM 48", "text 71", "text 117", "text 124", "text 146", "text 166", "text 173", "text 195",
             "text 215", "text 222", "text 244", "text 264", "text 271", "text 395"],
            mouse: DataPointer);

    /// <summary>The 16 weeks of history from nothing to past the scale, the scales unset or kept, the map markers, a stale history cursor.</summary>
    private static void ShapeAllData(GameState state, Random rng)
    {
        state.Ui_MoneyGraphDiv = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 220));
        state.Ui_DamageGraphDiv = (Sys3Int)(rng.Next(3) == 0 ? 0 : rng.Next(1, 220));
        state.Ui_MoneyGraphScale = (Sys3Int)rng.Next(0, 200);
        state.Ui_DamageGraphScale = (Sys3Int)rng.Next(0, 200);
        for (int k = 0; k < 300; k++)
        {
            var h = state.History[k];
            h.Funds = Amount();
            h.SundryExp = Amount();
            h.MilitaryExp = Amount();
            h.Casualties = Amount();
        }
        state.Ui_MarkCount = (Sys3Int)rng.Next(0, 12);
        for (int m = 0; m < 100; m++)
        {
            var mark = state.Marks[m];
            mark.Cg = (Sys3Int)rng.Next(0, 20000);
            mark.X = (Sys3Int)rng.Next(0, 1920);
            mark.Y = (Sys3Int)rng.Next(0, 800);
        }
        state.Hist_Cur = (Sys3Int)rng.Next(0, 300);

        Sys3Int Amount() => (Sys3Int)(rng.Next(5) switch { 0 => 0, 1 => rng.Next(0, 300), 2 => rng.Next(0, 4000), _ => rng.Next(0, 65536) });
    }

    /// <summary>The pointer of the All Data screen: anywhere; a third of the reads press a button, now and then both.</summary>
    private static MouseState DataPointer(Random rng)
    {
        int r = rng.Next(30);
        var button = (Sys3Int)(r < 20 ? 0 : r < 25 ? 16 : r < 29 ? 32 : 48);
        return new MouseState(button, (Sys3Int)rng.Next(0, 640), (Sys3Int)rng.Next(0, 400));
    }

    // ---------------------------------------------------------------------------------------
    // The comparison
    // ---------------------------------------------------------------------------------------

    private delegate Task Port(OriginalScriptContext script, ScenarioScripts scenario);

    /// <summary>A test's own menu player: the choice (0 a cancel) for <paramref name="items"/>, from <paramref name="rng"/>.</summary>
    private delegate int MenuPolicy(IReadOnlyList<SceneMenuItem> items, Random rng, bool tired);

    private async Task CompareAsync(
        string file, Port port, Action<GameState, Random> shape, Scribbler scribble, string[] expected, int cases = Cases,
        Func<Random, MouseState>? mouse = null, MenuPolicy? choose = null)
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, file)))
            return;
        var scenario = new ScenarioScripts(LocalData.ScenarioDirectory);
        var program = scenario.ReadProgram(file);
        cases = DifferentialRuns.Cases(cases);

        // The cases are independent (each has its own state, draws, input and log), so they run in
        // parallel; they are judged in seed order, so a failure still names the first seed that fails.
        var outcomes = new Outcome[cases];
        await Parallel.ForAsync(1, cases + 1, async (seed, _) =>
            outcomes[seed - 1] = await CompareCaseAsync(seed, file, program, port, shape, scenario, scribble, mouse, choose));

        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        int stopped = 0, unported = 0;
        foreach (var outcome in outcomes)
        {
            Assert.True(outcome.Failure is null, outcome.Failure);
            if (outcome.Stopped)
                stopped++;
            if (outcome.Unported)
                unported++;
            foreach (string feature in outcome.Features)
                seen[feature] = seen.GetValueOrDefault(feature) + 1;
        }

        output.WriteLine($"{file}: {cases} runs, {stopped} stopped by the input budget, {unported} reached an unported path");
        foreach (var (feature, count) in seen.OrderBy(f => f.Key, StringComparer.Ordinal))
            output.WriteLine($"  {feature}: {count}");
        var missed = expected.Where(e => !seen.ContainsKey(e)).ToList();
        DifferentialRuns.AssertReached(missed, output);
        Assert.True(unported < cases / 3, $"{unported} of {cases} runs reached an unported path");
    }

    /// <summary>One case judged: the failure (or <c>null</c>), how it ended, the features it reached.</summary>
    private sealed record Outcome(string? Failure, bool Stopped, bool Unported, IReadOnlyList<string> Features);

    /// <summary>Runs one seed through the port and the decompile and compares the two runs.</summary>
    private static async Task<Outcome> CompareCaseAsync(
        int seed, string file, ScriptProgram program, Port port, Action<GameState, Random> shape, ScenarioScripts scenario,
        Scribbler scribble, Func<Random, MouseState>? mouse, MenuPolicy? choose)
    {
        var rng = new Random(seed);
        var state = RandomState(rng);
        shape(state, rng);
        var mine = await RunAsync(Copy(state), seed, file, null, port, scenario, scribble, mouse, choose);
        var original = await RunAsync(Copy(state), seed, file, program, port, scenario, scribble, mouse, choose);

        if (mine.Unported is not null)
        {
            int prefix = FirstDifference(original.Log.Take(mine.Log.Count).ToList(), mine.Log);
            return new Outcome(
                prefix < 0 ? null : $"seed {seed}: the runs differ at entry {prefix} before the port's '{mine.Unported}'\n{Around(file, original.Log, mine.Log, prefix)}",
                Stopped: false, Unported: true, ["unported"]);
        }
        if (original.Unported is not null)
            return Failed($"seed {seed}: the port finished, the decompile failed: {original.Unported}");
        int at = FirstDifference(original.Log, mine.Log);
        if (at >= 0)
            return Failed($"seed {seed}: the runs differ at entry {at}\n{Around(file, original.Log, mine.Log, at)}");
        if (original.Stopped != mine.Stopped)
            return Failed($"seed {seed}: stopped by the input budget: decompile {original.Stopped}, port {mine.Stopped}");
        var differences = Differences(file, original.State.Memory, mine.State.Memory, v => VariableCatalog.Variables[v].Name).Take(10).ToList();
        if (differences.Count > 0)
            return Failed($"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));
        return new Outcome(null, mine.Stopped, Unported: false, Features(file, mine.Log).Distinct().ToList());

        static Outcome Failed(string failure) => new(failure, Stopped: false, Unported: false, []);
    }

    private sealed record Run(GameState State, List<string> Log, bool Stopped, string? Unported);

    private static async Task<Run> RunAsync(
        GameState state, int seed, string file, ScriptProgram? program, Port port, ScenarioScripts scenario, Scribbler scribble,
        Func<Random, MouseState>? mouse, MenuPolicy? choose)
    {
        var log = new List<string>();
        var hooks = new SliceHooks(state, seed, log, scribble);
        var input = new RandomInput(seed, log, hooks, mouse, choose);
        var view = new LoggingView(log, e => e is ScreenEvent);
        var random = new LoggingRandom(seed * 7 + 1, log);
        bool stopped = false;
        string? unported = null;
        try
        {
            if (program is null)
            {
                var script = new OriginalScriptContext(state, random, view, input, hooks);
                await port(script, scenario);
            }
            else
            {
                var runner = new SceneRunner(new OneProgram(program), hooks, f => f == file);
                var script = new OriginalScriptContext(state, random, view, input, runner);
                runner.Bind(script);
                await runner.RunFileAsync(file);
            }
            log.Add("end");
        }
        catch (InputBudgetException)
        {
            stopped = true;
        }
        catch (ScenarioJumpException e)
        {
            log.Add($"jump {e.File}");                                  // Iｶﾐ's ends (the hook below)
        }
        catch (NotSupportedException e)
        {
            // The port: a path of a later part. The decompile run past that point may meet
            // forms the scene runtime does not run (e.g. NB on bound variables); only the part
            // before the port's stop is compared then.
            unported = e.Message;
        }
        catch (Exception e) when (program is not null && e is InvalidOperationException or ScenarioFormatException)
        {
            unported = e.Message;
        }
        return new Run(state, log, stopped, unported);
    }

    private static IEnumerable<string> Features(string file, List<string> log)
    {
        foreach (string entry in log)
        {
            if (entry.StartsWith("call ", StringComparison.Ordinal))
            {
                yield return entry[..entry.IndexOf(" entry", StringComparison.Ordinal)];
                if (entry.StartsWith("call FIGT_A.ADV", StringComparison.Ordinal) && entry.EndsWith(" EParty1 73", StringComparison.Ordinal))
                    yield return "fleeing soldier";                     // Cﾃｷﾄｼ.ADV:321-339 (the hooks never write 73)
                if (entry.StartsWith("call FIGT.ADV", StringComparison.Ordinal) && entry.EndsWith(" EParty1 100", StringComparison.Ordinal))
                    yield return "boss fight";                          // Cﾒｲｷｭｳ.ADV:1196-1271, a setter run from the player's files
            }
            else if (entry.StartsWith("rnd ", StringComparison.Ordinal))
                yield return $"draw {entry.Split(' ')[1]}";
            else if (entry.StartsWith("EngineEvent IZ", StringComparison.Ordinal))
                yield return "IZ";
            else if (entry.StartsWith("jump ", StringComparison.Ordinal))
                yield return entry;
            else if (entry.StartsWith("IM 48 ", StringComparison.Ordinal))
                yield return "IM 48";                                   // both buttons
            else if (entry.StartsWith("SoundEvent", StringComparison.Ordinal))
                yield return $"sound {entry[(entry.IndexOf("Sound = ", StringComparison.Ordinal) + 8)..].TrimEnd(' ', '}')}";
            else if (entry.StartsWith("MapEvent", StringComparison.Ordinal))
                yield return $"M {entry[(entry.IndexOf("Mode = ", StringComparison.Ordinal) + 7)..].TrimEnd(' ', '}')}";
            else if (entry.StartsWith("menu ", StringComparison.Ordinal))
            {
                foreach (var part in entry.Split(file + ":").Skip(1))
                    yield return $"item {new string(part.TakeWhile(char.IsAsciiDigit).ToArray())}";
            }
            else if (entry.Contains(file + ":", StringComparison.Ordinal) && entry.Contains("MessageEvent", StringComparison.Ordinal))
            {
                int i = entry.IndexOf(file + ":", StringComparison.Ordinal) + file.Length + 1;
                yield return $"text {new string(entry[i..].TakeWhile(char.IsAsciiDigit).ToArray())}";
            }
        }
    }

    // ---------------------------------------------------------------------------------------
    // States
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// Every unbound variable and the record page random (the page around 10, where the status
    /// page's modifiers change sign), the strings named, the prologue most of the time.
    /// </summary>
    private static GameState RandomState(Random rng)
    {
        var state = new GameState();
        double zero = rng.NextDouble() * 0.6;
        RandomStates.Variables(state, rng, zero);
        for (int i = 0; i < state.Memory.PageSize(1); i++)
            state.Memory.WritePage(1, i, (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(8) == 0 ? rng.Next(0, 3000) : rng.Next(0, 21)));
        for (int i = 0; i < state.Memory.PageSize(2); i++)
            state.Memory.WritePage(2, i, (Sys3Int)rng.Next(0, 5));
        RandomStates.NameStrings(state);
        state.StoryStage = (Sys3Int)(rng.Next(6) == 0 ? rng.Next(1, 5) : 0);
        state.Ev_CommandMode = (Sys3Int)(rng.Next(12) == 0 ? rng.Next(1, 3) : 0);
        state.Week = (Sys3Int)rng.Next(9, 30);
        for (int u = 1; u <= 50; u++)
            state.Units[u].Status = (Sys3Int)rng.Next(0, 3);
        for (int p = 0; p <= 100; p++)
            state.Provs[p].Owner = (Sys3Int)rng.Next(0, 13);
        for (int k = 1; k <= 50; k++)
            state.Items[k].Effect = (Sys3Int)rng.Next(0, 8);
        return state;
    }

    // ---------------------------------------------------------------------------------------
    // The hooks
    // ---------------------------------------------------------------------------------------

    /// <summary>What a hook call writes, decided by (seed, call) so both runs get the same writes.</summary>
    private delegate void Scribbler(string file, GameState state, Random rng, bool tired);

    /// <summary>
    /// The other files' writes: cursors, scratch, <c>RetVal</c>; the pickers answer (a cancel once
    /// the run is long); scenes move the story stage, the talk and harem variables, the camera.
    /// </summary>
    private static void Scribble(string file, GameState state, Random rng, bool tired)
    {
        if (rng.Next(2) == 0)
            state.Unit_Cur = (Sys3Int)rng.Next(0, 52);
        if (rng.Next(3) == 0)
            state.Prov_Cur = (Sys3Int)rng.Next(0, 100);
        if (rng.Next(3) == 0)
            state.Dgn_Cur = (Sys3Int)rng.Next(0, 31);
        if (rng.Next(3) == 0)
            state.Item_Cur = (Sys3Int)rng.Next(0, 50);
        if (rng.Next(3) == 0)
            state.Hist_Cur = (Sys3Int)rng.Next(0, 60);
        if (rng.Next(2) == 0)
            state.RetVal = (Sys3Int)rng.Next(0, 40);
        if (rng.Next(3) == 0)
            state.Tmp0001 = (Sys3Int)rng.Next(0, 3);
        if (rng.Next(3) == 0)
            state.Tmp0019 = (Sys3Int)rng.Next(0, 640);
        if (rng.Next(3) == 0)
            state.Ui_WinType = (Sys3Int)(rng.Next(0, 2) * 43);
        if (rng.Next(4) == 0)
            state.Ev_Entry = (Sys3Int)rng.Next(0, 30);
        if (state.Ev_CommandMode != 0 && rng.Next(2) == 0)
            state.Ev_CommandMode = Sys3Int.Zero;
        switch (file)
        {
            case "Sﾌﾞｶ.ADV":
                if (state.CallArg == 1)
                {
                    int pick = tired ? 0 : rng.Next(10) switch
                    {
                        < 4 => rng.Next(1, 5),
                        4 => 0,
                        5 => 503,
                        6 => rng.Next(51, 60),
                        7 => rng.Next(48, 51),
                        _ => rng.Next(5, 51),
                    };
                    state.CallArg = (Sys3Int)pick;
                    state.Unit_Cur = (Sys3Int)(pick is >= 1 and <= 50 && rng.Next(4) != 0 ? pick : rng.Next(0, 52));
                }
                else if (rng.Next(3) == 0)
                    state.CallArg = (Sys3Int)rng.Next(0, 60);
                break;
            case "Sﾊｰﾚﾑ.ADV":
                state.CallArg = (Sys3Int)(tired ? 0 : rng.Next(10) switch
                {
                    0 => 0,
                    < 4 => rng.Next(3) switch { 0 => 1, 1 => 11, _ => 12 },
                    < 8 => rng.Next(1, 51),
                    8 => rng.Next(51, 501),
                    _ => rng.Next(501, 600),
                });
                break;
            case "Iﾄｳｿﾞｸ.ADV":
                if (rng.Next(2) == 0)
                    state.StoryStage = 99;
                break;
            case "Cﾆﾝｼﾞｬ.ADV":
                state.Ui_CityScouted = (Sys3Int)rng.Next(0, 2);
                break;
            case "Gｶﾅﾐ.ADV" or "Gｺｰﾄﾞ.ADV" or "Gﾌﾚｲｱ.ADV":
                // The ninja's scenes: Kanami's scene 4 (seen when Rance does not forgive her
                // interrupt), Code's states, now and then another ninja for the tests after them.
                if (rng.Next(2) == 0)
                    state.F_Kanami_Scene4 = (Sys3Int)rng.Next(0, 2);
                if (rng.Next(2) == 0)
                    state.F_Code_State = (Sys3Int)(rng.Next(3) * 2);
                if (rng.Next(4) == 0)
                    state.Bt_ScoutNinja = (Sys3Int)rng.Next(0, 6);
                break;
            case "Sﾌﾞﾀｲ.ADV":
                {
                    // The party picked (Go: deployed, paid) or nothing (Stop); the refusers often.
                    int size = tired || rng.Next(5) == 0 ? 0 : rng.Next(1, 5);
                    var party = new Sys3Int[4];
                    for (int i = 0; i < size; i++)
                    {
                        party[i] = (Sys3Int)(rng.Next(6) switch { 0 => 29, 1 => 30, 2 => 37, 3 => 44, _ => rng.Next(1, 51) });
                        state.Units[party[i]].Status = 2;
                    }
                    state.Bt_PlayerParty1 = party[0];
                    state.Bt_PlayerParty2 = party[1];
                    state.Bt_PlayerParty3 = party[2];
                    state.Bt_PlayerParty4 = party[3];
                    if (size > 0)
                        state.Gold = (Sys3Int)rng.Next(0, 400);
                    state.Tmp0021 = (Sys3Int)rng.Next(0, 30);
                    break;
                }
            case "TCﾘｰｻﾞｽ2.ADV" or "TJAPAN2.ADV" or "Tｾﾞｽ2.ADV" or "Tﾍﾙﾏﾝ2.ADV" or "Tｹｲﾌﾞﾘｽ2.ADV" or "GAL.ADV":
                // The defenders (none now and then); Zeth's barrier empties the party; Warg.
                state.Bt_EnemyParty1 = (Sys3Int)(rng.Next(4) == 0 ? 0 : rng.Next(74, 200));
                state.Bt_EnemyParty2 = (Sys3Int)rng.Next(0, 3);
                state.Bt_EnemyRetreatMode = (Sys3Int)rng.Next(0, 5);
                state.Bt_WallBonus = (Sys3Int)rng.Next(0, 3);
                state.Cmdr_Cur = (Sys3Int)rng.Next(0, 221);
                if (rng.Next(file == "Tｾﾞｽ2.ADV" ? 2 : 8) == 0)
                    state.Bt_PlayerParty1 = Sys3Int.Zero;
                if (rng.Next(file == "Tｹｲﾌﾞﾘｽ2.ADV" ? 2 : 8) == 0)
                    state.F_Warg_State = (Sys3Int)rng.Next(0, 3);
                if (rng.Next(4) == 0)
                    state.F_Japan_State = (Sys3Int)rng.Next(0, 11);
                break;
            case "FIGT_A.ADV":
                state.Bt_Kind = (Sys3Int)rng.Next(0, 4);
                state.Bt_Result = (Sys3Int)rng.Next(0, 3);
                if (rng.Next(2) == 0)
                    state.Ui_CmdAttackAvail = Sys3Int.Zero;
                state.Provs[state.Prov_Cur].Owner = (Sys3Int)(rng.Next(3) == 0 ? 1 : rng.Next(0, 12));
                break;
            case "Gﾊﾟｯﾄﾝ.ADV":
                state.F_Patton_State = (Sys3Int)(rng.Next(3) switch { 0 => 20, 1 => 10, _ => rng.Next(0, 13) });
                break;
            case "Gｽｽﾞﾒ.ADV" or "Gｳｪﾝﾃﾞｨ.ADV" or "Gｶﾌｪ.ADV" when state.Ev_Entry != 10:
                // The harem's intro scenes: what the tests after them read, RetVal above all.
                state.RetVal = (Sys3Int)rng.Next(0, 4);
                if (rng.Next(3) == 0)
                    state.F_Wendy_State = (Sys3Int)(rng.Next(2) == 0 ? 4 : rng.Next(0, 8));
                if (rng.Next(3) == 0)
                    state.F_Wendy_BedroomSeen = (Sys3Int)rng.Next(0, 2);
                if (rng.Next(3) == 0)
                    state.F_Cafe_State = (Sys3Int)rng.Next(9, 13);
                if (rng.Next(3) == 0)
                    state.LadyCaravan_Prov = (Sys3Int)rng.Next(0, 9);
                if (rng.Next(3) == 0)
                    state.F_Lia_State = (Sys3Int)rng.Next(0, 3);
                if (rng.Next(3) == 0)
                    state.Ui_CmdHaremAvail = (Sys3Int)rng.Next(0, 3);
                if (rng.Next(4) == 0)
                    state.StoryStage = (Sys3Int)rng.Next(0, 3);
                break;
            default:
                if (rng.Next(2) == 0)
                    state.Harem_ExpReward = (Sys3Int)rng.Next(0, 7);
                if (rng.Next(4) == 0)
                    state.Unit48_Identity = (Sys3Int)rng.Next(0, 3);
                if (rng.Next(4) == 0)
                    state.F_SillCow_State = (Sys3Int)rng.Next(0, 9);
                if (rng.Next(4) == 0)
                    state.CurrentBgm = (Sys3Int)rng.Next(0, 30);
                if (rng.Next(4) == 0)
                    state.Harem_LastPick = (Sys3Int)rng.Next(0, 60);
                break;
        }
    }

    private sealed class SliceHooks(GameState state, int seed, List<string> log, Scribbler scribble) : ScribblingHooks<GameState>(state, seed, log)
    {
        public bool Tired => Calls > 40;

        protected override string Describe() =>
            $"RetVal {State.RetVal} CallArg {State.CallArg} Unit_Cur {State.Unit_Cur} "
            + $"Prov_Cur {State.Prov_Cur} Dgn_Cur {State.Dgn_Cur} Tmp0001 {State.Tmp0001} Hist_Cur {State.Hist_Cur} "
            + $"Tmp0021 {State.Tmp0021} Tmp0022 {State.Tmp0022} Bt_Kind {State.Bt_Kind} EParty1 {State.Bt_EnemyParty1}";

        protected override void Scribble(string file, Sys3Int entry, Random rng) => scribble(file, State, rng, Tired);
    }

    // ---------------------------------------------------------------------------------------
    // Input, draws, view
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The player at random: the pointer mostly at rest (now and then a button still down, for
    /// the release waits; or as the test's <c>mouse</c> places it), random menu choices (the last
    /// item — Return — once the run is long; or the test's <c>choose</c>), keys that are
    /// sometimes a right click (32), which closes the status pages.
    /// </summary>
    private sealed class RandomInput(
        int seed, List<string> log, SliceHooks hooks, Func<Random, MouseState>? mouse, MenuPolicy? choose) : UnexpectedInput
    {
        private readonly Random _rng = new(seed * 31 + 7);
        private int _answers;
        private int _cursor = 1;

        private bool Tired => hooks.Tired || _answers > 300;

        public override Task<MouseState> GetMouseAsync()
        {
            Spend();
            int r = _rng.Next(10);
            var m = mouse?.Invoke(_rng)
                ?? new MouseState((Sys3Int)(r < 8 ? 0 : r == 8 ? 16 : 32), (Sys3Int)_rng.Next(0, 640), (Sys3Int)_rng.Next(0, 400));
            log.Add($"IM {m.Button} {m.X} {m.Y}");
            return Task.FromResult(m);
        }

        public override Task<Sys3Int> SetCursorAsync(Sys3Int shape)
        {
            Spend();
            int old = _cursor;
            _cursor = shape.Value;
            log.Add($"IC {shape} {old}");
            return Task.FromResult((Sys3Int)old);
        }

        public override Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            Spend();
            int choice = items.Count == 0 ? 0
                : choose is not null ? choose(items, _rng, Tired)
                : Tired ? items.Count : _rng.Next(6) == 0 ? 0 : _rng.Next(1, items.Count + 1);
            log.Add($"menu {string.Join(" | ", items.Select(Format))} -> {choice}");
            return Task.FromResult(choice);
        }

        public override Task<Sys3Int> WaitForKeyAsync()
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(5) switch { 0 => 32, 1 => 13, _ => 0 });
            log.Add($"key {key}");
            return Task.FromResult(key);
        }

        public override Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            Spend();
            log.Add($"wait {ticks}");
            return Task.FromResult(Sys3Int.Zero);
        }

        private void Spend()
        {
            if (++_answers > 2000)
                throw new InputBudgetException();
        }

        private static string Format(SceneMenuItem item) =>
            $"{item.At} {string.Join(",", item.Texts.Select(t => $"{t.Text}[{string.Join(",", t.Numbers)}][{string.Join(",", t.Strings)}]"))}";
    }

    // ---------------------------------------------------------------------------------------
    // Reports
    // ---------------------------------------------------------------------------------------
}
