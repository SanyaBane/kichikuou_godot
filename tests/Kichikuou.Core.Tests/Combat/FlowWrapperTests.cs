using Kichikuou.Core.Combat;
using Kichikuou.Core.Sys3;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>FIGT_A.ADV — the player's attack (01 §2.1, §2.6, §11.3).</summary>
public class FlowAttackTests
{
    [Fact]
    public async Task Plains_battle_uses_the_5000_limit()
    {
        var rig = new FlowRig();
        rig.State.Prov_Cur = 20;
        rig.State.Bt_BackgroundCg = 900;
        rig.State.RetVal = 77;
        await rig.Battle.AttackPreludeAsync();
        Is(5000, rig.State.Bt_TroopLimit);
        Is(900, rig.State.Bt_BackgroundCg);
        Is(1, rig.State.Bt_Kind);
        Is(77, rig.State.RetVal);   // FIGT_A's message wait preserves RetVal
        Assert.Equal([35], rig.Lines);
    }

    [Fact]
    public async Task City_battle_uses_the_battle_area()
    {
        var rig = new FlowRig();
        rig.State.Prov_Cur = 20;
        rig.State.Provs[20].BattleArea = 1200;
        rig.State.Bt_WallBonus = 30;
        rig.State.Bt_BackgroundCg = 900;
        await rig.Battle.AttackPreludeAsync();
        Is(1200, rig.State.Bt_TroopLimit);
        Is(902, rig.State.Bt_BackgroundCg);
        Assert.Equal([47], rig.Lines);
    }

    [Theory]
    [InlineData(78, 0, 908)]
    [InlineData(82, 0, 918)]
    [InlineData(79, 0, 910)]
    [InlineData(20, 18, 904)]
    public async Task Background_by_province_and_enemy(int prov, int enemy, int cg)
    {
        var rig = new FlowRig();
        rig.State.Prov_Cur = (Sys3Int)prov;
        rig.State.Bt_BackgroundCg = 900;
        rig.State.Bt_EnemyParty1 = (Sys3Int)enemy;
        await rig.Battle.AttackPreludeAsync();
        Is(cg, rig.State.Bt_BackgroundCg);
    }

    [Theory]
    [InlineData(1, 1, true)]
    [InlineData(1, 0, false)]
    public async Task Japan_invasion_scene_needs_both_commanders(int status75, int status82, bool scene)
    {
        var rig = new FlowRig();
        rig.State.Prov_Cur = 55;
        rig.State.Cmdrs[75].Status = (Sys3Int)status75;
        rig.State.Cmdrs[82].Status = (Sys3Int)status82;
        await rig.Battle.AttackPreludeAsync();
        Is(1, rig.State.N_JapanInv_Stage);
        Assert.Equal(scene, rig.Story.Calls.Contains(("IJAPAN.ADV", 3)));
        Is(82, rig.State.Cmdr_Cur);
    }

    [Fact]
    public async Task Ex_scene_before_the_battle_sets_Ev_Entry()
    {
        var rig = new FlowRig();
        rig.State.Prov_Cur = 5;
        rig.State.F_Ex_State = 1;
        await rig.Battle.AttackPreludeAsync();
        Assert.Equal([("Gｴｸｽ.ADV", 2)], rig.Story.Calls);
        Is(2, rig.State.Ev_Entry);
    }

    private static FlowRig Aftermath(int result, int stage = 1, int duel = 0)
    {
        var rig = new FlowRig();
        rig.State.Bt_Result = (Sys3Int)result;
        rig.State.StoryStage = (Sys3Int)stage;
        rig.State.Bt_IsDuel = (Sys3Int)duel;
        rig.State.Bt_Kind = 1;
        rig.State.Prov_Cur = 20;
        rig.State.Bt_TargetProv = 20;
        rig.State.Provs[20].Owner = 4;
        rig.State.Ui_CmdAttackAvail = 1;
        rig.State.Bt_BackgroundCg = 902;
        rig.State.Units[1].Status = 2;
        rig.PlayerParty(1);
        return rig;
    }

    [Fact]
    public async Task Victory_takes_the_province()
    {
        var rig = Aftermath(2);
        await rig.Battle.AttackAftermathAsync();
        Is(1, rig.State.Provs[20].Owner);
        Is(0, rig.State.Ui_CmdAttackAvail);
        // The owner's flag (Tmp0021 = owner + 819) becomes a map marker (M.ADV op 4, 10000 + CG).
        Is(1, rig.State.Ui_MarkCount);
        Is(10000 + 819 + 1, rig.State.Marks[0].Cg);
        Is(0, rig.State.Bt_Kind);
        Is(900, rig.State.Bt_BackgroundCg);
        Is(20, rig.State.Prov_Cur);
        Is(0, rig.State.Bt_TargetProv);
        Assert.Equal([128, 132], rig.Lines);
    }

    [Fact]
    public async Task Defeat_keeps_the_attack_command()
    {
        // QUIRK Q-2.
        var rig = Aftermath(1);
        rig.State.BgmEnabled = 1;
        rig.Input.Track = 9;
        await rig.Battle.AttackAftermathAsync();
        Is(4, rig.State.Provs[20].Owner);
        Is(1, rig.State.Ui_CmdAttackAvail);
        Assert.Equal([86, 90], rig.Lines);
        var music = rig.View.Events.OfType<FlowMusicEvent>().Select(m => (int)m.Track).ToList();
        Assert.Equal([0, 8], music);
    }

    [Fact]
    public async Task Prologue_victory_changes_no_owner()
    {
        var rig = Aftermath(2, stage: 0);
        await rig.Battle.AttackAftermathAsync();
        Is(4, rig.State.Provs[20].Owner);
        Assert.Equal([128], rig.Lines);
    }

    [Fact]
    public async Task Scripted_win_takes_whatever_province_is_current()
    {
        // QUIRK Q-25: Prov_Cur differs from the target.
        var rig = Aftermath(2);
        rig.State.Prov_Cur = 33;
        rig.State.Provs[33].Owner = 7;
        await rig.Battle.AttackAftermathAsync();
        Is(1, rig.State.Provs[33].Owner);
        Is(4, rig.State.Provs[20].Owner);
    }

    [Fact]
    public async Task Duel_skips_the_messages_and_reports_province_1()
    {
        var rig = Aftermath(2, duel: 1);
        await rig.Battle.AttackAftermathAsync();
        Assert.Empty(rig.Lines);
        Is(4, rig.State.Provs[20].Owner);
        Is(1, rig.State.Prov_Cur);
        Is(0, rig.State.Bt_TargetProv);
    }

    [Fact]
    public async Task Story_hooks_after_the_battle()
    {
        var rig = Aftermath(2);
        rig.State.Prov_Cur = 45;
        rig.State.Bt_TargetProv = 45;
        rig.State.F_Helman_War = 1;
        rig.State.F_Japan_State = 10;
        await rig.Battle.AttackAftermathAsync();
        Assert.Equal([("Gﾐﾈﾊﾞ.ADV", 5)], rig.Story.Calls);
    }

    [Fact]
    public async Task Kinkaid_scene_needs_unit_9_in_the_front_two()
    {
        var rig = Aftermath(2);
        rig.State.Prov_Cur = 40;
        rig.State.Bt_TargetProv = 40;
        rig.PlayerParty(1, 9);
        rig.State.Units[9].Status = 2;
        await rig.Battle.AttackAftermathAsync();
        Assert.Contains(("Gｷﾝｹｰﾄﾞ.ADV", 1), rig.Story.Calls);
    }

    [Fact]
    public async Task Rances_death_ends_the_game_in_the_capture_step()
    {
        var rig = Aftermath(1);
        rig.State.Units[1].Status = 0;
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(rig.Battle.AttackAftermathAsync);
        Assert.Equal("OVER.ADV", jump.File);
        Is(1, rig.State.EndingId);
    }

    [Fact]
    public async Task Attack_runs_until_the_first_redraw_of_the_battle()
    {
        var rig = new FlowRig(2);
        rig.State.Prov_Cur = 20;
        rig.PlayerParty(1);
        rig.EnemyParty(30);
        Assert.True(await FlowRig.UntilStub(rig.Battle.AttackProvinceAsync));
        Is(1, rig.State.Bt_Kind);
        Is(5, rig.State.Bt_RoundsLeft);
    }
}

/// <summary>FIGT_B.ADV — an enemy army attacks (01 §2.3, §11.3).</summary>
public class FlowDefenceTests
{
    private static FlowRig Rig(params int[] draws)
    {
        var rig = new FlowRig(draws);
        rig.State.Prov_Cur = 35;
        rig.State.Provs[35].Owner = 1;
        rig.State.Ev_PhaseNation = 4;
        rig.EnemyParty(22, 23);
        rig.State.Cmdrs[22].Troops = 3000;
        rig.State.Cmdrs[23].Troops = 2500;
        rig.State.Cmdrs[22].Status = 1;
        rig.State.Cmdrs[23].Status = 1;
        return rig;
    }

    [Fact]
    public async Task Announcement_remembers_the_target_province()
    {
        var rig = Rig();
        rig.State.Prov_StrBase = 3000;
        rig.State.Strings[3000 + 34] = "Prov35";
        Assert.Equal(FigtBLabel.Menu, await rig.Battle.DefenceAnnounceAsync());
        Is(35, rig.State.Bt_TargetProv);
        Assert.Equal("Prov35", rig.State.Strings[8]);
        Assert.Equal([20, 35, 43], rig.Lines);
    }

    [Theory]
    [InlineData(1, (int)FigtBLabel.Intercept)]
    [InlineData(2, (int)FigtBLabel.NinjaScout)]
    [InlineData(3, (int)FigtBLabel.Abandon)]
    [InlineData(null, (int)FigtBLabel.Menu)]
    public async Task Menu(int? answer, int nextCode)
    {
        var next = (FigtBLabel)nextCode;
        var rig = Rig();
        rig.Input.MenuAnswers.Enqueue(answer);
        Assert.Equal(next, await rig.Battle.DefenceMenuAsync());
        Assert.Equal(BattleMenuKind.EnemyAttackResponse, rig.Input.Menus[0].Kind);
    }

    [Fact]
    public async Task Menu_rejects_an_unlisted_answer()
    {
        var rig = Rig();
        rig.Input.MenuAnswers.Enqueue(4);
        await Assert.ThrowsAsync<InvalidOperationException>(rig.Battle.DefenceMenuAsync);
    }

    [Theory]
    [InlineData(501, (int)FigtBLabel.Plains)]
    [InlineData(502, (int)FigtBLabel.City)]
    [InlineData(503, (int)FigtBLabel.Announce)]
    [InlineData(999, (int)FigtBLabel.Plains)]
    public async Task Intercept_follows_the_party_pickers_code(int code, int nextCode)
    {
        var next = (FigtBLabel)nextCode;
        var rig = Rig();
        rig.State.Ev_Entry = 12;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Sﾌﾞﾀｲ.ADV")
            {
                Is(4, rig.State.Tmp0021);
                Is(2, rig.State.Tmp0022);
                rig.PlayerParty(1, 2);
                rig.State.CallArg = (Sys3Int)code;
            }
        };
        Assert.Equal(next, await rig.Battle.DefenceInterceptAsync());
        Is(code, rig.State.Tmp0621);
        Assert.Equal([("Sﾌﾞﾀｲ.ADV", 12)], rig.Story.Calls);
        Is(12, rig.State.Ev_Entry);
    }

    [Theory]
    [InlineData(37, 7, 5, true)]
    [InlineData(37, 7, 8, false)]
    [InlineData(29, 4, 0, true)]
    [InlineData(29, 8, 0, false)]
    [InlineData(30, 10, 0, false)]
    [InlineData(30, 6, 0, true)]
    public async Task Refusals_undo_the_deployment(int unit, int nation, int zeth, bool refuses)
    {
        var rig = Rig();
        rig.State.Ev_PhaseNation = (Sys3Int)nation;
        rig.State.F_Zeth_State = (Sys3Int)zeth;
        rig.State.Gold = 500;
        rig.Story.OnCall = (file, _) =>
        {
            if (file != "Sﾌﾞﾀｲ.ADV")
                return;
            rig.PlayerParty(1, unit);
            rig.State.Units[1].Status = 2;
            rig.State.Units[unit].Status = 2;
            rig.State.Gold = 400;   // paid
            rig.State.CallArg = 501;
        };
        var next = await rig.Battle.DefenceInterceptAsync();
        Assert.Equal(refuses ? FigtBLabel.Announce : FigtBLabel.Plains, next);
        Is(refuses ? 1 : 2, rig.State.Units[unit].Status);
        Is(refuses ? 1 : 2, rig.State.Units[1].Status);
        Is(400, rig.State.Gold);   // QUIRK Q-1: not refunded
    }

    [Fact]
    public async Task Plains_defence()
    {
        var rig = Rig();
        rig.State.Bt_WallBonus = 30;
        Assert.Equal(FigtBLabel.Fight, await rig.Battle.DefencePlainsAsync());
        Is(5000, rig.State.Bt_TroopLimit);
        Is(0, rig.State.Bt_WallBonus);
    }

    [Theory]
    [InlineData(1, 4)]
    [InlineData(2, 5)]
    public async Task City_defence_grows_the_battle_area(int draw, int var0692)
    {
        var rig = Rig(draw);
        rig.State.Provs[35].BattleArea = 1000;
        rig.State.Provs[35].Defense = 40;
        rig.State.VAR0692 = 5;
        Assert.Equal(FigtBLabel.Fight, await rig.Battle.DefenceCityAsync());
        Is(1000, rig.State.Bt_TroopLimit);
        Is(40, rig.State.Bt_WallBonus);
        Is(902, rig.State.Bt_BackgroundCg);
        Is(1100, rig.State.Provs[35].BattleArea);   // QUIRK Q-3
        Is(var0692, rig.State.VAR0692);
        Assert.Equal([2], rig.Random.Draws);
    }

    [Fact]
    public async Task Kayblis_war_scenes_may_empty_the_party()
    {
        var rig = Rig();
        rig.State.F_Kayblis_War = 2;
        rig.EnemyParty(135);
        rig.PlayerParty(29, 41);
        rig.Story.OnCall = (file, entry) =>
        {
            if (entry == 7)
                rig.PlayerParty();
        };
        Assert.Equal(FigtBLabel.Announce, await rig.Battle.DefenceBeforeBattleAsync());
        Assert.Equal([("Gｹｲﾌﾞﾘｽ.ADV", 5), ("Gｹｲﾌﾞﾘｽ.ADV", 7)], rig.Story.Calls);
    }

    [Fact]
    public async Task Before_the_battle_sets_kind_2_and_the_background()
    {
        var rig = Rig();
        rig.State.Prov_Cur = 83;
        rig.State.Bt_BackgroundCg = 900;
        rig.PlayerParty(1);
        Assert.Equal(FigtBLabel.Fight, await rig.Battle.DefenceBeforeBattleAsync());
        Is(2, rig.State.Bt_Kind);
        Is(918, rig.State.Bt_BackgroundCg);
    }

    [Theory]
    [InlineData(1, 1, 1)]   // RND(5) = 1, Elena state 0 → scene 1
    [InlineData(1, 0, 0)]   // province 40 not ours: forced to 4
    [InlineData(2, 1, 0)]
    public async Task Elena_appears_after_a_win_in_31_to_49(int draw, int owner40, int scene)
    {
        var rig = Rig(draw);
        rig.State.Bt_Result = 2;
        rig.State.Bt_TargetProv = 35;
        rig.State.Provs[40].Owner = (Sys3Int)owner40;
        await rig.Battle.DefenceAftermathAsync();
        Assert.Equal([5], rig.Random.Draws);
        Assert.Equal(scene != 0, rig.Story.Calls.Contains(("Gｴﾚﾅ.ADV", scene)));
        Is(35, rig.State.Prov_Cur);
        Is(35, rig.State.Tmp0583);
        Is(0, rig.State.Bt_TargetProv);
        Assert.Equal([257], rig.Lines);
    }

    [Fact]
    public async Task A_lost_defence_hands_the_province_over()
    {
        var rig = Rig();
        rig.State.Bt_Result = 1;
        rig.State.Bt_TargetProv = 35;
        await rig.Battle.DefenceAftermathAsync();
        Is(4, rig.State.Provs[35].Owner);
        Is(1, rig.State.Ui_MarkCount);
        Is(10000 + 819 + 4, rig.State.Marks[0].Cg);
        Assert.Empty(rig.Random.Draws);
        Assert.Equal([840, 843], rig.Lines);
    }

    [Fact]
    public async Task Losing_the_castle_is_game_over()
    {
        var rig = Rig();
        rig.State.Prov_Cur = 1;
        rig.State.Bt_Result = 1;
        rig.State.Bt_TargetProv = 1;
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(rig.Battle.DefenceAftermathAsync);
        Assert.Equal("OVER.ADV", jump.File);
        Is(2, rig.State.EndingId);
        Is(4, rig.State.Provs[1].Owner);
    }

    [Fact]
    public async Task Abandoning_hands_over_the_province_without_a_battle()
    {
        var rig = Rig();
        rig.PlayerParty(1, 2);
        rig.State.Bt_TargetProv = 35;
        Assert.Equal(FigtBLabel.Return, await rig.Battle.DefenceAbandonAsync());
        Is(1, rig.State.Bt_Result);
        Is(0, rig.State.Bt_PlayerParty1);
        Is(0, rig.State.Bt_TargetProv);
        Is(4, rig.State.Provs[35].Owner);
        Assert.Equal([737, 742, 747, 754, 759, 766, 795], rig.Lines);
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public async Task Abandoning_the_castle_is_game_over()
    {
        var rig = Rig();
        rig.State.Prov_Cur = 1;
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(rig.Battle.DefenceAbandonAsync);
        Is(2, rig.State.EndingId);
        Assert.Equal("OVER.ADV", jump.File);
    }

    [Fact]
    public async Task Ex_makes_a_last_stand_with_target_province_already_0()
    {
        // QUIRK Q-5.
        var rig = Rig();
        rig.State.Prov_Cur = 1;
        rig.State.F_Ex_State = 4;
        rig.State.Week = 21;
        rig.State.Bt_TargetProv = 1;
        rig.State.Provs[1].BattleArea = 900;
        rig.State.Provs[1].Defense = 50;
        Assert.Equal(FigtBLabel.Fight, await rig.Battle.DefenceAbandonAsync());
        Is(14, rig.State.Bt_PlayerParty1);
        Is(2, rig.State.Units[14].Status);
        Is(900, rig.State.Bt_TroopLimit);
        Is(50, rig.State.Bt_WallBonus);
        Is(0, rig.State.Bt_TargetProv);
        Assert.Equal([("Gｴｸｽ.ADV", 5)], rig.Story.Calls);
    }

    [Fact]
    public async Task No_ninja_left()
    {
        var rig = Rig();
        rig.State.F_Kanami_NinjaUsed = 1;
        rig.State.F_Freya_NinjaUsed = 1;
        rig.State.F_Kaoru_NinjaUsed = 1;
        rig.State.F_Wichita_NinjaUsed = 1;
        rig.State.F_Saitou_NinjaUsed = 1;
        Assert.Equal(FigtBLabel.Menu, await rig.Battle.DefenceNinjaScoutAsync());
        Assert.Empty(rig.Input.Menus);
        Assert.Equal([314, 322, 331], rig.Lines);
    }

    [Fact]
    public async Task Ninja_scout_reports_the_enemy_army()
    {
        var rig = Rig();
        rig.State.F_Kanami_NinjaUsed = 1;
        rig.Input.MenuAnswers.Enqueue(2);   // Freya
        Assert.Equal(FigtBLabel.Menu, await rig.Battle.DefenceNinjaScoutAsync());
        var menu = Assert.Single(rig.Input.Menus);
        Assert.Equal([2, 3, 4, 5, 0], menu.Options.Select(o => (int)o.Code));
        Is(2, rig.State.Bt_ScoutNinja);
        Is(1, rig.State.F_Freya_NinjaUsed);
        Is(5500, rig.State.Tmp0001);
        Is(2, rig.State.Tmp0002);
        Is(23, rig.State.Cmdr_Cur);
        Assert.Equal([22, 23], rig.View.Events.OfType<ScoutReportEvent>().Select(e => (int)e.Cmdr));
        Assert.Equal(637, rig.Lines[^1]);   // more than 5000: "quite a strong force"
        Assert.Contains(551, rig.Lines);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public async Task Ninja_menu_none_or_cancel(int? answer)
    {
        var rig = Rig();
        rig.Input.MenuAnswers.Enqueue(answer);
        Assert.Equal(FigtBLabel.Menu, await rig.Battle.DefenceNinjaScoutAsync());
        Is(0, rig.State.Bt_ScoutNinja);
        Assert.Equal([314, 384, 389], rig.Lines);
    }

    [Fact]
    public async Task Saitou_has_no_arrival_line()
    {
        var rig = Rig();
        rig.EnemyParty(22);
        rig.Input.MenuAnswers.Enqueue(5);
        await rig.Battle.DefenceNinjaScoutAsync();
        Is(1, rig.State.F_Saitou_NinjaUsed);
        Assert.Equal([314, 494, 502, 509, 518, 525, 529, 533, 581, 587, 613, 629, 644], rig.Lines);
    }

    [Fact]
    public async Task Whole_defence_scout_then_abandon_runs_without_a_battle()
    {
        var rig = Rig();
        rig.Input.MenuAnswers.Enqueue(2);   // Ninja Scout
        rig.Input.MenuAnswers.Enqueue(1);   // Kanami
        rig.Input.MenuAnswers.Enqueue(3);   // Abandon City
        await rig.Battle.DefendProvinceAsync();
        Is(1, rig.State.F_Kanami_NinjaUsed);
        Is(1, rig.State.Bt_Result);
        Is(4, rig.State.Provs[35].Owner);
        Assert.Equal(3, rig.Input.Menus.Count);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task Ex_scene_plays_once_before_the_announcement()
    {
        var rig = Rig();
        rig.EnemyParty(1);
        rig.Input.MenuAnswers.Enqueue(3);
        await rig.Battle.DefendProvinceAsync();
        Assert.Equal([("Gｴｸｽ.ADV", 1)], rig.Story.Calls);
    }
}
