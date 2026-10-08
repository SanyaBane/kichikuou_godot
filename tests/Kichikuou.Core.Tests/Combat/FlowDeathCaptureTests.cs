using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>死亡捕獲.ADV — outcomes after a battle (01 §11.4).</summary>
public class FlowDeathCaptureTests
{
    /// <summary>Stage 1, Rance alive and deployed, the given enemies (all fallen unless set).</summary>
    private static FlowRig Rig(params int[] enemies)
    {
        var rig = new FlowRig();
        rig.State.StoryStage = 1;
        rig.State.Units[1].Status = 2;
        rig.PlayerParty(1);
        rig.EnemyParty(enemies);
        rig.State.Bt_Result = 2;
        return rig;
    }

    [Fact]
    public async Task Rance_fallen_outside_the_prologue_is_game_over()
    {
        var rig = Rig();
        rig.State.Units[1].Status = 0;
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(rig.Battle.DeathCaptureAsync);
        Assert.Equal("OVER.ADV", jump.File);
        Is(1, rig.State.EndingId);
    }

    [Fact]
    public async Task In_the_prologue_Rance_stays_down_and_Sill_is_revived()
    {
        var rig = Rig();
        rig.State.StoryStage = 0;
        rig.State.Units[1].Status = 0;
        rig.State.Units[2].Status = 0;
        rig.State.Units[2].HP = 0;
        await rig.Battle.DeathCaptureAsync();
        Is(0, rig.State.Units[1].Status);
        Is(2, rig.State.Units[2].Status);
        Is(1, rig.State.Units[2].HP);
    }

    [Fact]
    public async Task Fallen_mercenaries_are_lost()
    {
        var rig = Rig();
        rig.PlayerParty(1, 49, 50);
        rig.State.Units[50].Status = 2;
        await rig.Battle.DeathCaptureAsync();
        Is(1, rig.State.F_MercA_Lost);
        Is(0, rig.State.F_MercB_Lost);
    }

    [Theory]
    [InlineData(4, 100, 1000, 1, "Gﾒﾅﾄﾞ.ADV", 2)]
    [InlineData(3, 300, 1000, 1, "Gﾒﾅﾄﾞ.ADV", 1)]
    [InlineData(3, 334, 1000, 1, null, 0)]   // 334 * 3 = 1002, not below
    [InlineData(4, 100, 1000, 3, null, 0)]   // dungeon
    public async Task Menad_scenes(int menad, int troops, int max, int kind, string? file, int entry)
    {
        var rig = Rig();
        rig.PlayerParty(1, 10);
        rig.State.Bt_Kind = (Sys3Int)kind;
        rig.State.F_Menad_State = (Sys3Int)menad;
        rig.State.Units[10].HP = 5;
        rig.State.Units[10].Troops = (Sys3Int)troops;
        rig.State.Units[10].MaxTroops = (Sys3Int)max;
        await rig.Battle.DeathCaptureAsync();
        if (file is null)
            Assert.Empty(rig.Story.Calls);
        else
            Assert.Equal([(file, entry)], rig.Story.Calls);
    }

    [Theory]
    [InlineData(2, 6)]
    [InlineData(0, 5)]
    public async Task Somita_counts_survived_battles(int status, int count)
    {
        var rig = Rig();
        rig.PlayerParty(1, 46);
        rig.State.Units[46].Status = (Sys3Int)status;
        rig.State.N_Somita_BattlesSurvived = 5;
        await rig.Battle.DeathCaptureAsync();
        Is(count, rig.State.N_Somita_BattlesSurvived);
    }

    [Fact]
    public async Task Scenes_run_in_script_order()
    {
        var rig = Rig(2, 4);
        await rig.Battle.DeathCaptureAsync();
        Assert.Equal([("Gﾒﾙﾌｪｲｽ.ADV", 1), ("Gﾊｳﾚｰﾝ.ADV", 1)], rig.Story.Calls);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(72)]
    [InlineData(55)]
    [InlineData(57)]
    public async Task Escaping_commanders_are_revived_with_1_hp(int cmdr)
    {
        var rig = Rig(cmdr);
        rig.State.Cmdrs[cmdr].HP = 0;
        await rig.Battle.DeathCaptureAsync();
        Is(1, rig.State.Cmdrs[cmdr].Status);
        Is(1, rig.State.Cmdrs[cmdr].HP);
    }

    [Fact]
    public async Task Commanders_not_in_the_battle_are_left_alone()
    {
        var rig = Rig(30);
        await rig.Battle.DeathCaptureAsync();
        Is(0, rig.State.Cmdrs[1].Status);
        Is(0, rig.State.Cmdrs[72].Status);
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public async Task Cmdr_31_is_restored_in_the_prologue_whether_he_fought_or_not()
    {
        var rig = Rig(30);
        rig.State.StoryStage = 0;
        await rig.Battle.DeathCaptureAsync();
        Is(1, rig.State.Cmdrs[31].Status);
    }

    [Fact]
    public async Task Cmdr_31_fallen_later_plays_Ruberan()
    {
        var rig = Rig(31);
        await rig.Battle.DeathCaptureAsync();
        Assert.Equal([("Gﾙｰﾍﾞﾗﾝ.ADV", 2)], rig.Story.Calls);
    }

    [Theory]
    [InlineData(3, 5)]      // Cream fallen: scene 2, state set to 5
    [InlineData(100, 100)]  // 100 is kept
    public async Task Cream_fallen(int before, int after)
    {
        var rig = Rig(45);
        rig.State.F_Nero_Helman4thState = (Sys3Int)before;
        await rig.Battle.DeathCaptureAsync();
        Is(after, rig.State.F_Nero_Helman4thState);
        Assert.Equal([("Gｸﾘｰﾑ.ADV", 2)], rig.Story.Calls);
    }

    [Fact]
    public async Task Cream_alive_with_Nero_state_4_plays_scene_1()
    {
        var rig = Rig(45);
        rig.State.Cmdrs[45].Status = 1;
        rig.State.F_Nero_Helman4thState = 4;
        await rig.Battle.DeathCaptureAsync();
        Is(5, rig.State.F_Nero_Helman4thState);
        Assert.Equal([("Gｸﾘｰﾑ.ADV", 1)], rig.Story.Calls);
    }

    [Fact]
    public async Task Mineva_is_revived_unless_beaten_at_45()
    {
        var rig = Rig(39);
        await rig.Battle.DeathCaptureAsync();
        Is(1, rig.State.Cmdrs[39].Status);
        Is(2, rig.State.Cmdrs[39].HP);

        var at45 = Rig(39);
        at45.State.Prov_Cur = 45;
        await at45.Battle.DeathCaptureAsync();
        Is(0, at45.State.Cmdrs[39].Status);
        Assert.Equal([("Gﾐﾈﾊﾞ.ADV", 5)], at45.Story.Calls);
    }

    [Theory]
    [InlineData(2, true)]
    [InlineData(1, false)]
    [InlineData(0, false)]
    public async Task Zeth_lords_play_a_scene_on_a_win_else_revive(int result, bool scene)
    {
        var rig = Rig(101);
        rig.State.Bt_Result = (Sys3Int)result;
        await rig.Battle.DeathCaptureAsync();
        Assert.Equal(scene, rig.Story.Calls.Contains(("Gﾁｽﾞｺ.ADV", 2)));
        Is(scene ? 0 : 1, rig.State.Cmdrs[101].Status);
    }

    [Theory]
    [InlineData(57, 2, 1, 1, false)]   // away from home: revived
    [InlineData(58, 2, 0, 0, true)]    // home and won: Zeth scene 40
    [InlineData(58, 1, 2, 1, false)]   // home and lost
    [InlineData(58, 0, 0, 0, false)]   // home, timeout: stays down
    public async Task Tower_boss_113(int prov, int result, int status, int hp, bool scene)
    {
        var rig = Rig(113);
        rig.State.Prov_Cur = (Sys3Int)prov;
        rig.State.Bt_Result = (Sys3Int)result;
        await rig.Battle.DeathCaptureAsync();
        Is(status, rig.State.Cmdrs[113].Status);
        Is(hp, rig.State.Cmdrs[113].HP);
        Assert.Equal(scene, rig.Story.Calls.Contains(("Iｾﾞｽ.ADV", 40)));
    }

    [Fact]
    public async Task Cmdr_109_is_revived_with_full_hp()
    {
        var rig = Rig(109);
        rig.State.Cmdrs[109].MaxHP = 12;
        await rig.Battle.DeathCaptureAsync();
        Is(1, rig.State.Cmdrs[109].Status);
        Is(12, rig.State.Cmdrs[109].HP);
    }

    [Fact]
    public async Task Cmdr_125_at_63_is_revived_then_taken_down_again_by_the_win()
    {
        var rig = Rig(125);
        rig.State.Prov_Cur = 63;
        await rig.Battle.DeathCaptureAsync();
        Is(0, rig.State.Cmdrs[125].Status);
        Is(1, rig.State.Cmdrs[125].HP);
        Assert.Equal([("Iｾﾞｽ.ADV", 41)], rig.Story.Calls);
    }

    [Fact]
    public async Task Zeths_four_lords_all_down_regardless_of_who_fought()
    {
        var rig = Rig(30);
        rig.State.F_Zeth_War = 1;
        await rig.Battle.DeathCaptureAsync();
        Assert.Equal([("Iｾﾞｽ.ADV", 100)], rig.Story.Calls);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public async Task Kessel_scene_only_outside_duels(int duel, bool scene)
    {
        var rig = Rig(133);
        rig.State.Bt_IsDuel = (Sys3Int)duel;
        await rig.Battle.DeathCaptureAsync();
        Assert.Equal(scene, rig.Story.Calls.Contains(("Gｹｯｾﾙ.ADV", 10)));
    }

    [Theory]
    [InlineData(2, 0, true)]
    [InlineData(1, 1, false)]
    public async Task Kayblis_guards(int result, int status, bool scene)
    {
        var rig = Rig(159);
        rig.State.Bt_Result = (Sys3Int)result;
        await rig.Battle.DeathCaptureAsync();
        Is(status, rig.State.Cmdrs[159].Status);
        Assert.Equal(scene, rig.Story.Calls.Contains(("Gｹｲﾌﾞﾜﾝ.ADV", 1)));
    }

    [Fact]
    public async Task Cmdr_91_revived_with_2_hp_or_beaten_at_88()
    {
        var rig = Rig(91);
        await rig.Battle.DeathCaptureAsync();
        Is(1, rig.State.Cmdrs[91].Status);
        Is(2, rig.State.Cmdrs[91].HP);
    }

    [Fact]
    public async Task Leila_and_Rick_scene_ends_the_file()
    {
        var rig = Rig();
        rig.PlayerParty(1, 5, 7, 6);
        rig.State.Units[5].Status = 2;
        rig.State.Units[7].Status = 2;
        rig.State.Units[6].Status = 2;
        rig.State.Units[12].Status = 2;
        rig.State.F_Leila_State = 4;
        rig.State.Prov_Cur = 20;
        await rig.Battle.DeathCaptureAsync();
        Assert.Equal([("Gﾚｲﾗ.ADV", 2)], rig.Story.Calls);
    }

    [Theory]
    [InlineData(4, true)]
    [InlineData(3, false)]
    public async Task Barres_and_Haurein_scene_needs_Haurein_above_level_3(int level, bool scene)
    {
        var rig = Rig();
        rig.PlayerParty(1, 6, 12);
        rig.State.Units[6].Status = 2;
        rig.State.Units[12].Status = 2;
        rig.State.Units[12].Level = (Sys3Int)level;
        await rig.Battle.DeathCaptureAsync();
        Assert.Equal(scene, rig.Story.Calls.Contains(("Gﾊｳﾚｰﾝ.ADV", 2)));
    }

    [Fact]
    public async Task Scenes_see_the_state_left_by_earlier_scenes()
    {
        // Gﾏｼﾞｯｸ 2 (cmdr 125 at province 66) changes Tmp0002; the next test reads the new value.
        var rig = Rig(125);
        rig.State.Prov_Cur = 66;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Gﾏｼﾞｯｸ.ADV")
                rig.State.Tmp0002 = 0;
        };
        await rig.Battle.DeathCaptureAsync();
        Is(0, rig.State.Cmdrs[125].Status);   // not revived: Tmp0002 was cleared by the scene
        Assert.Equal([("Gﾏｼﾞｯｸ.ADV", 2)], rig.Story.Calls);
    }
}
