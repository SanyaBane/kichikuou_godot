using System.Text;
using Kichikuou.Content.Scripts;
using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Xunit.Abstractions;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The hand-ported prologue slices of the command files (<see cref="EnemyCityCommand"/>,
/// <see cref="DistantCityCommand"/>, <see cref="SubordinatesCommand"/>, <see cref="HaremCommand"/>, <see cref="MerchantCommand"/>, <see cref="OwnCityCommand"/>,
/// <see cref="DungeonCommand"/>) and the pickers (<see cref="DeploymentScreen"/>,
/// <see cref="SubordinateScreen"/>, <see cref="HaremScreen"/>) against the files themselves run from the player's decompile by
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
             "unported"],
            cases: 5000);

    /// <summary>
    /// A city of the prologue now and then (Pillage, the AL Church), mostly an attack later on:
    /// the rebels' provinces, the mountain passes and their neighbours, the Land of Death, the
    /// stage-2 gate, JAPAN's 56; the flags of every refusal and exit, Patton's list alive or not.
    /// </summary>
    private static void ShapeEnemyCity(GameState state, Random rng)
    {
        state.Prov_Cur = (Sys3Int)(rng.Next(14) switch
        {
            < 3 => 33 + rng.Next(2),
            3 => 77,
            < 7 => rng.Next(3, 8),
            7 => 31 + rng.Next(2),
            8 => 82,
            9 => 56,
            10 => rng.Next(4) switch { 0 => 78, 1 => 89, 2 => 90, _ => 91 },
            _ => rng.Next(0, 100),
        });
        state.N_ALChurch_Stage = (Sys3Int)rng.Next(0, 25);
        state.StoryStage = (Sys3Int)(rng.Next(5) switch { 0 => 0, < 3 => 1, 3 => 2, _ => rng.Next(1, 5) });
        if (state.StoryStage != 0 && state.Prov_Cur != 77)
            state.Ev_CommandMode = Sys3Int.Zero;
        var city = state.Provs[state.Prov_Cur];
        city.Owner = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(0, 13) : state.StoryStage == 1 || rng.Next(3) == 0 ? 2 : rng.Next(4, 12));
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
    public Task The_dungeon_entry_runs_exactly_as_Cﾒｲｷｭｳ() =>
        CompareAsync(
            "Cﾒｲｷｭｳ.ADV",
            (script, scenario) => new DungeonCommand(script, scenario).RunAsync(),
            (state, rng) =>
            {
                state.Dgn_Cur = (Sys3Int)(rng.Next(10) switch { < 3 => 1, 3 => 2, 4 => 3, 5 => 15, _ => rng.Next(0, 32) });
                state.Ui_CmdDungeonAvail = (Sys3Int)rng.Next(0, 2);
                state.F_Merim_MillArc = (Sys3Int)rng.Next(0, 5);
                state.Cmdr_Cur = (Sys3Int)rng.Next(0, 221);
                if (rng.Next(4) == 0)
                    state.Cmdrs[state.Cmdr_Cur].CgFull = 1137;
                state.F_PG9_State = (Sys3Int)rng.Next(0, 4);
                state.N_Soul_Stage = (Sys3Int)(rng.Next(4) == 0 ? 5 : rng.Next(0, 5));
                state.F_HannyKing_State = (Sys3Int)rng.Next(0, 4);
                if (rng.Next(4) == 0)
                    state.Dgns[5].State = Sys3Int.Zero;
            },
            Scribble,
            ["draw 3", "call Gﾊﾞｳﾝﾄﾞ.ADV", "call Gﾎﾗｶﾞ.ADV", "call Gｶﾌｪ.ADV", "text 2763", "text 2795", "text 2811",
             "text 2827", "text 2852", "M 1", "M 10", "unported"]);

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

    // ---------------------------------------------------------------------------------------
    // The comparison
    // ---------------------------------------------------------------------------------------

    private delegate Task Port(ScriptContext script, ScenarioScripts scenario);

    private async Task CompareAsync(
        string file, Port port, Action<GameState, Random> shape, Scribbler scribble, string[] expected, int cases = Cases)
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, file)))
            return;
        var scenario = new ScenarioScripts(LocalData.ScenarioDirectory);
        var program = scenario.ReadProgram(file);
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);
        int stopped = 0, unported = 0;

        for (int seed = 1; seed <= cases; seed++)
        {
            var rng = new Random(seed);
            var state = RandomState(rng);
            shape(state, rng);
            var mine = await RunAsync(Copy(state), seed, file, null, port, scenario, scribble);
            var original = await RunAsync(Copy(state), seed, file, program, port, scenario, scribble);

            if (mine.Unported is not null)
            {
                unported++;
                seen["unported"] = seen.GetValueOrDefault("unported") + 1;
                int prefix = FirstDifference(original.Log.Take(mine.Log.Count).ToList(), mine.Log);
                Assert.True(prefix < 0, $"seed {seed}: the runs differ at entry {prefix} before the port's '{mine.Unported}'\n{Around(file, original.Log, mine.Log, prefix)}");
                continue;
            }
            Assert.True(original.Unported is null, $"seed {seed}: the port finished, the decompile failed: {original.Unported}");
            int at = FirstDifference(original.Log, mine.Log);
            Assert.True(at < 0, $"seed {seed}: the runs differ at entry {at}\n{Around(file, original.Log, mine.Log, at)}");
            Assert.Equal(original.Stopped, mine.Stopped);
            var differences = Differences(file, original.State.Memory, mine.State.Memory).Take(10).ToList();
            Assert.True(differences.Count == 0, $"seed {seed}: memory differs\n  " + string.Join("\n  ", differences));
            if (mine.Stopped)
                stopped++;
            foreach (string feature in Features(file, mine.Log).Distinct())
                seen[feature] = seen.GetValueOrDefault(feature) + 1;
        }

        output.WriteLine($"{file}: {cases} runs, {stopped} stopped by the input budget, {unported} reached an unported path");
        foreach (var (feature, count) in seen.OrderBy(f => f.Key, StringComparer.Ordinal))
            output.WriteLine($"  {feature}: {count}");
        var missed = expected.Where(e => !seen.ContainsKey(e)).ToList();
        Assert.True(missed.Count == 0, "never reached: " + string.Join(", ", missed));
        Assert.True(unported < cases / 3, $"{unported} of {cases} runs reached an unported path");
    }

    private sealed record Run(GameState State, List<string> Log, bool Stopped, string? Unported);

    private static async Task<Run> RunAsync(
        GameState state, int seed, string file, ScriptProgram? program, Port port, ScenarioScripts scenario, Scribbler scribble)
    {
        var log = new List<string>();
        var hooks = new ScribblingHooks(state, seed, log, scribble);
        var input = new RandomInput(seed, log, hooks);
        var view = new LoggingView(log);
        var random = new LoggingRandom(seed, log);
        bool stopped = false;
        string? unported = null;
        try
        {
            if (program is null)
            {
                var script = new ScriptContext(state, random, view, input, hooks);
                await port(script, scenario);
            }
            else
            {
                var runner = new SceneRunner(new OneProgram(program), hooks, f => f == file);
                var script = new ScriptContext(state, random, view, input, runner);
                runner.Bind(script);
                await runner.RunFileAsync(file);
            }
            log.Add("end");
        }
        catch (InputBudgetException)
        {
            stopped = true;
        }
        catch (NotSupportedException e)
        {
            // The port: a path of a later part. The decompile run past that point may meet
            // forms the scene runtime does not run (e.g. NB on bound variables); only the part
            // before the port's stop is compared then.
            unported = e.Message;
        }
        catch (InvalidOperationException e) when (program is not null)
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
            }
            else if (entry.StartsWith("draw ", StringComparison.Ordinal))
                yield return entry[..entry.IndexOf(" ->", StringComparison.Ordinal)];
            else if (entry.StartsWith("EngineEvent IZ", StringComparison.Ordinal))
                yield return "IZ";
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
        for (int v = 0; v < ScriptMemory.VariableCount; v++)
            if (!GameState.IsBound(v))
                state.Memory.Var(v) = (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(5) == 0 ? rng.Next(0, 3000) : rng.Next(0, 12));
        for (int i = 0; i < state.Memory.PageSize(1); i++)
            state.Memory.WritePage(1, i, (Sys3Int)(rng.NextDouble() < zero ? 0 : rng.Next(8) == 0 ? rng.Next(0, 3000) : rng.Next(0, 21)));
        for (int i = 0; i < state.Memory.PageSize(2); i++)
            state.Memory.WritePage(2, i, (Sys3Int)rng.Next(0, 5));
        for (int s = 0; s < state.Strings.Length; s++)
            state.Strings[s] = $"s{s}";
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
                state.Provs[state.Prov_Cur].Owner = (Sys3Int)rng.Next(0, 12);
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

    private sealed class ScribblingHooks(GameState state, int seed, List<string> log, Scribbler scribble) : IStoryHooks
    {
        public int Calls { get; private set; }

        public bool Tired => Calls > 40;

        public Task RunEventAsync(string file, Sys3Int entry)
        {
            if (state.Ev_Entry != entry)
                throw new InvalidOperationException($"{file}: Ev_Entry {state.Ev_Entry.Value}, entry {entry.Value}");
            log.Add($"call {file} entry {entry} RetVal {state.RetVal} CallArg {state.CallArg} Unit_Cur {state.Unit_Cur} "
                + $"Prov_Cur {state.Prov_Cur} Dgn_Cur {state.Dgn_Cur} Tmp0001 {state.Tmp0001} Hist_Cur {state.Hist_Cur} "
                + $"Tmp0021 {state.Tmp0021} Tmp0022 {state.Tmp0022} Bt_Kind {state.Bt_Kind} EParty1 {state.Bt_EnemyParty1}");
            var rng = new Random(seed * 1000 + ++Calls);
            scribble(file, state, rng, Tired);
            return Task.CompletedTask;
        }
    }

    /// <summary>The slice's file, read once; every other file is a hook.</summary>
    private sealed class OneProgram(ScriptProgram program) : ISceneSource
    {
        public ScriptProgram ReadProgram(string file) =>
            file == program.File ? program : throw new InvalidOperationException($"{file} is a hook here.");
    }

    // ---------------------------------------------------------------------------------------
    // Input, draws, view
    // ---------------------------------------------------------------------------------------

    private sealed class InputBudgetException() : Exception("The input budget ran out.");

    /// <summary>
    /// The player at random: the pointer mostly at rest (now and then a button still down, for
    /// the release waits), random menu choices (the last item — Return — once the run is long),
    /// keys that are sometimes a right click (32), which closes the status pages.
    /// </summary>
    private sealed class RandomInput(int seed, List<string> log, ScribblingHooks hooks) : ISceneInput
    {
        private readonly Random _rng = new(seed * 31 + 7);
        private int _answers;
        private int _cursor = 1;

        private bool Tired => hooks.Tired || _answers > 300;

        public Task<MouseState> GetMouseAsync()
        {
            Spend();
            int r = _rng.Next(10);
            var m = new MouseState((Sys3Int)(r < 8 ? 0 : r == 8 ? 16 : 32), (Sys3Int)_rng.Next(0, 640), (Sys3Int)_rng.Next(0, 400));
            log.Add($"IM {m.Button} {m.X} {m.Y}");
            return Task.FromResult(m);
        }

        public Task<Sys3Int> SetCursorAsync(Sys3Int shape)
        {
            Spend();
            int old = _cursor;
            _cursor = shape.Value;
            log.Add($"IC {shape} {old}");
            return Task.FromResult((Sys3Int)old);
        }

        public Task<int> SelectAsync(IReadOnlyList<SceneMenuItem> items)
        {
            Spend();
            int choice = items.Count == 0 ? 0 : Tired ? items.Count : _rng.Next(6) == 0 ? 0 : _rng.Next(1, items.Count + 1);
            log.Add($"menu {string.Join(" | ", items.Select(Format))} -> {choice}");
            return Task.FromResult(choice);
        }

        public Task<Sys3Int> WaitForKeyAsync()
        {
            Spend();
            var key = (Sys3Int)(_rng.Next(5) switch { 0 => 32, 1 => 13, _ => 0 });
            log.Add($"key {key}");
            return Task.FromResult(key);
        }

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable)
        {
            Spend();
            log.Add($"wait {ticks}");
            return Task.FromResult(Sys3Int.Zero);
        }

        public Task<Sys3Int> GetPlayingTrackAsync() => throw new InvalidOperationException("SR 0");

        public Task<IReadOnlyList<Sys3Int>> GetDateAsync() => throw new InvalidOperationException("ZT 0");

        public Task ResetTimerAsync(Sys3Int value) => throw new InvalidOperationException("ZT 1");

        public Task<Sys3Int> GetTimerAsync(int mode) => throw new InvalidOperationException("ZT n");

        public Task<IReadOnlyList<Sys3Int>> GetCdPositionAsync() => throw new InvalidOperationException("SC");

        public Task<Sys3Int> GetMidiTrackAsync() => throw new InvalidOperationException("SR 1");

        private void Spend()
        {
            if (++_answers > 2000)
                throw new InputBudgetException();
        }

        private static string Format(SceneMenuItem item) =>
            $"{item.At} {string.Join(",", item.Texts.Select(t => $"{t.Text}[{string.Join(",", t.Numbers)}][{string.Join(",", t.Strings)}]"))}";
    }

    /// <summary>Draws 1..n from a seeded generator, logged.</summary>
    private sealed class LoggingRandom(int seed, List<string> log) : IScriptRandom
    {
        private readonly Random _rng = new(seed * 7 + 1);

        public Sys3Int Next(Sys3Int n)
        {
            var value = (Sys3Int)(n.Value <= 1 ? n.Value : _rng.Next(1, n.Value + 1));
            log.Add($"draw {n} -> {value}");
            return value;
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

    // ---------------------------------------------------------------------------------------
    // Reports
    // ---------------------------------------------------------------------------------------

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
