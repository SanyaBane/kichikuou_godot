using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// <c>ApplyExchange</c> (<c>L_02d45</c>, <c>FIGT.ADV:1873-1971</c>) with <c>FIGTATTK.ADV</c>, the
/// defeat scenes and the ninja test (docs/battle/02-resolution.md §7, 01 §9.3). Paths that start
/// a ninja follow-up reach the flavour text and are covered after the merge with part R2.
/// </summary>
public class ResolutionApplyExchangeTests
{
    private static ResolutionFixture Arrange(params int[] draws)
    {
        var fx = ResolutionFixture.Create(draws);
        var s = fx.State;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_PlayerSlot1 = 1;
        s.Bt_EnemySlot1 = 1;
        s.Bt_PlayerActSlot = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_MsgMode = 3;
        s.StoryStage = 1;
        fx.Unit(1).Status = 1;
        fx.Unit(1).HP = 20;
        fx.Unit(1).Troops = 100;
        fx.Cmdr(1).Status = 1;
        fx.Cmdr(1).HP = 15;
        fx.Cmdr(1).Troops = 80;
        return fx;
    }

    [Fact]
    public async Task Applies_the_results_and_plays_the_animation()
    {
        var fx = Arrange(1, 2);
        var s = fx.State;
        s.Bt_ETroopVsCmdrShots = 3; s.Bt_ETroopVsCmdrDmg = 3;
        s.Bt_ETroopVsTroopShots = 10; s.Bt_ETroopVsTroopDmg = 10;
        s.Bt_ECmdrVsTroopShots = 5; s.Bt_ECmdrVsTroopDmg = 5;
        s.Bt_ECmdrVsCmdrShots = 2; s.Bt_ECmdrVsCmdrDmg = 2;
        s.Bt_PTroopVsCmdrShots = 4; s.Bt_PTroopVsCmdrDmg = 4;
        s.Bt_PTroopVsTroopShots = 20; s.Bt_PTroopVsTroopDmg = 20;
        s.Bt_PCmdrVsTroopShots = 6; s.Bt_PCmdrVsTroopDmg = 6;
        s.Bt_PCmdrVsCmdrShots = 1; s.Bt_PCmdrVsCmdrDmg = 1;
        s.Bt_EnemyRetreatMode = 5;
        s.Bt_SkillSfx = 3;

        await fx.Battle.ApplyExchangeAsync();

        Assert.Equal(15, (int)s.Bt_PlayerLosses);   // 10 + 5
        Assert.Equal(15, (int)fx.Unit(1).HP);        // 20 - 3 - 2
        Assert.Equal(85, (int)fx.Unit(1).Troops);    // 100 - 10 - 5
        Assert.Equal(26, (int)s.Bt_EnemyLosses);     // 20 + 6
        Assert.Equal(10, (int)fx.Cmdr(1).HP);        // 15 - 4 - 1
        Assert.Equal(54, (int)fx.Cmdr(1).Troops);    // 80 - 20 - 6
        Assert.Equal(50, (int)s.Bt_EnemyRetreatMode);
        Assert.Equal([2, 3], fx.Random.Draws);       // FIGTATTK:234, FIGT:1915
        Assert.Equal(2, (int)s.RetVal);
        Assert.Equal(1, (int)s.Unit_Cur);            // no ninja

        // FIGTATTK state: sounds (SkillSfx 3 overrides the attack pair, then is reset)
        Assert.Equal(39, (int)s.Bt_SfxAtk1);
        Assert.Equal(40, (int)s.Bt_SfxAtk2);
        Assert.Equal(8, (int)s.Bt_SfxDef1);
        Assert.Equal(9, (int)s.Bt_SfxDef2);
        Assert.Equal(0, (int)s.Bt_SkillSfx);
        // loop counters: 0..360 step 40, Tmp0003 = 360 * 5 / 11
        Assert.Equal(360, (int)s.Tmp0002);
        Assert.Equal(163, (int)s.Tmp0003);
        Assert.Equal(408, (int)s.Tmp0004);
        Assert.Equal(56, (int)s.Tmp0005);
        Assert.Equal(10, (int)s.CallArg);
        // the last number drawn: ECVC damage 2 at (640 + 416 - 360, 300), 5 digits
        Assert.Equal(696 + 4 * 32, (int)s.Ui_MouseX);
        Assert.Equal(300, (int)s.Ui_MouseY);
        Assert.Equal(5, (int)s.Tmp0021);
        Assert.Equal(1, (int)s.Tmp0022);
        Assert.Equal(0, (int)s.Tmp0023);
        Assert.Equal(2, (int)s.Tmp0024);
        Assert.Equal(2, (int)s.Tmp0025);

        Assert.Equal(new CountersEvent(100, 80, 20, 15), fx.View.Events[0]);
        Assert.Equal([new SoundPairEvent(39, 8), new SoundPairEvent(9, 40)], fx.Events<SoundPairEvent>());
        Assert.Equal(
            Enumerable.Range(0, 10).Select(i => new ShotsFrameEvent((Sys3Int)(i * 40))),
            fx.Events<ShotsFrameEvent>());
        Assert.Equal([.. Enumerable.Repeat(new PauseEvent(1), 10), new PauseEvent(40)], fx.Events<PauseEvent>());
        Assert.Equal(new WipedOutMarksEvent(false, false, false, false), fx.Events<WipedOutMarksEvent>().Single());
        Assert.IsType<KeyWaitEvent>(fx.View.Events[^1]);
    }

    [Theory]
    [InlineData(0, 0, 5, 5)]  // no commander damage: mode 5 stays
    [InlineData(1, 0, 5, 50)] // troop → commander damage
    [InlineData(0, 1, 5, 50)] // commander → commander damage
    [InlineData(1, 1, 4, 4)]
    public async Task Retreat_mode_5_becomes_50_when_the_commander_is_hurt(int ptvc, int pcvc, int mode, int expected)
    {
        var fx = Arrange(1, 2);
        fx.State.Bt_PTroopVsCmdrDmg = (Sys3Int)ptvc;
        fx.State.Bt_PCmdrVsCmdrDmg = (Sys3Int)pcvc;
        fx.State.Bt_EnemyRetreatMode = (Sys3Int)mode;
        await fx.Battle.ApplyExchangeAsync();
        Assert.Equal(expected, (int)fx.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task Damage_floors_at_zero_and_losses_count_damage()
    {
        // Q16: losses add the damage numbers even when fewer troops were present.
        var fx = Arrange(1, 2);
        fx.Cmdr(1).Troops = 10;
        fx.State.Bt_PTroopVsTroopDmg = 10000;
        fx.State.Bt_PCmdrVsTroopDmg = 10000;
        await fx.Battle.ApplyExchangeAsync();
        Assert.Equal(0, (int)fx.Cmdr(1).Troops);
        Assert.Equal(20000, (int)fx.State.Bt_EnemyLosses);
        Assert.True(fx.Events<WipedOutMarksEvent>().Single().EnemyTroops);
    }

    [Fact]
    public async Task Enemy_commander_death_runs_the_scene_redraws_and_compacts()
    {
        var fx = Arrange(1);
        var s = fx.State;
        s.Cmdr_Cur = 2;
        s.Bt_EnemySlot1 = 2;
        s.Bt_EnemySlot2 = 5;
        s.Bt_ESlot2State = 20;
        fx.Cmdr(2).Status = 1;
        fx.Cmdr(2).HP = 5;
        fx.Cmdr(5).HP = 30;
        s.Bt_PCmdrVsCmdrShots = 1;
        s.Bt_PCmdrVsCmdrDmg = 5;
        s.Bt_EnemyRetreatMode = 3;

        await fx.Battle.ApplyExchangeAsync();

        Assert.Equal(0, (int)fx.Cmdr(2).HP);
        Assert.Equal(0, (int)fx.Cmdr(2).Status);
        Assert.Equal(20, (int)s.Bt_EnemyRetreatMode); // Q-15: mode 3 → 20
        Assert.Equal(1, (int)s.Bt_EnemyLosses);
        Assert.Equal([2], fx.Random.Draws);           // no RND(3) after a death
        Assert.Equal(5, (int)s.Bt_EnemySlot1);
        Assert.Equal(20, (int)s.Bt_ESlot1State);
        Assert.Equal(0, (int)s.Bt_EnemySlot2);
        Assert.Equal(0, (int)s.Bt_ESlot2State);
        Assert.Equal(5, (int)s.Cmdr_Cur);             // left by the redraw
        Assert.Equal(1, (int)s.CallArg);              // compaction argument
        Assert.True(fx.Events<WipedOutMarksEvent>().Single().EnemyCmdr);

        // FIGTﾃｷDE.ADV:34-43 → L_0251e → L_0259b
        var scene = fx.View.Events
            .SkipWhile(e => e is not KeyWaitEvent).Skip(1)
            .TakeWhile(e => e is not BattlefieldBackgroundEvent).ToList();
        Assert.Equal(
            [
                new WindowEvent(151, 10), new MessageEvent(new ScriptRef("FIGTﾃｷDE.ADV", 37)), new WindowEvent(11, 10),
                new WindowEvent(10, 10), new WindowEvent(153, 10), new PrintStringEvent(4),
                new MessageEvent(new ScriptRef("FIGTﾃｷDE.ADV", 1816)), new WindowEvent(11, 10), new WindowEvent(10, 10),
            ],
            scene);
        Assert.Equal(
            [new SlotPanelEvent(1, 1), new SlotPanelEvent(5, 2), new SlotPanelEvent(6, 5)],
            fx.Events<SlotPanelEvent>());
        Assert.Equal(new PauseEvent(10), fx.View.Events[^1]);
    }

    [Fact]
    public async Task Player_death_moves_the_cursors_before_the_enemy_and_ninja_tests()
    {
        // 01 Q-27: Sill dies; the redraw leaves Unit_Cur = 5 and Cmdr_Cur = 4 (last occupied
        // slots); commander 3, also at HP 0 but in slot 1, is not processed now.
        var fx = Arrange(1, 1);
        var s = fx.State;
        s.Bt_Kind = 1;
        s.Unit_Cur = 2;
        s.Cmdr_Cur = 3;
        s.Bt_PlayerSlot1 = 2;
        s.Bt_PlayerSlot2 = 5;
        s.Bt_PSlot2State = 1;
        s.Bt_PSlot2Focus = 2;
        s.Bt_PlayerOrder2 = 8;
        s.Bt_PlayerOrder4 = 9;
        s.Bt_EnemySlot1 = 3;
        s.Bt_EnemySlot2 = 4;
        fx.Unit(2).Status = 1;
        fx.Unit(2).HP = 3;
        fx.Unit(5).HP = 10;
        fx.Unit(5).OffAtkMod = 4;
        fx.Cmdr(3).Status = 1;
        fx.Cmdr(3).HP = 2;
        fx.Cmdr(4).HP = 10;
        s.Bt_ECmdrVsCmdrShots = 1; s.Bt_ECmdrVsCmdrDmg = 3;
        s.Bt_PCmdrVsCmdrShots = 1; s.Bt_PCmdrVsCmdrDmg = 2;

        await fx.Battle.ApplyExchangeAsync();

        Assert.Equal(0, (int)fx.Unit(2).Status);
        Assert.Equal(1, (int)s.Bt_PlayerLosses);
        Assert.Equal(1, (int)s.F_Sill_Died);          // FIGTﾐｶDE.ADV:90
        Assert.Equal(5, (int)s.Unit_Cur);
        Assert.Equal(4, (int)s.Cmdr_Cur);
        Assert.Equal(4, (int)fx.Unit(5).CurAtkMod);   // Q-20: redraw rewrote the modifiers
        // compaction of slot 1 (Q15: Bt_PlayerOrder4 kept)
        Assert.Equal(5, (int)s.Bt_PlayerSlot1);
        Assert.Equal(1, (int)s.Bt_PSlot1State);
        Assert.Equal(2, (int)s.Bt_PSlot1Focus);
        Assert.Equal(8, (int)s.Bt_PlayerOrder1);
        Assert.Equal(0, (int)s.Bt_PlayerSlot2);
        Assert.Equal(9, (int)s.Bt_PlayerOrder3);
        Assert.Equal(9, (int)s.Bt_PlayerOrder4);
        // the lingering commander
        Assert.Equal(0, (int)fx.Cmdr(3).HP);
        Assert.Equal(1, (int)fx.Cmdr(3).Status);
        Assert.Equal(0, (int)s.Bt_EnemyLosses);
        Assert.Equal(3, (int)s.Bt_EnemySlot1);
        // RND(3) = 1 is drawn for unit 5 vs commander 4, which has no ninja
        Assert.Equal([2, 3], fx.Random.Draws);
        Assert.Contains(new MessageEvent(new ScriptRef("FIGTﾐｶDE.ADV", 93)), fx.View.Events);
        Assert.Contains(new MessageEvent(new ScriptRef("FIGTﾐｶDE.ADV", 98)), fx.View.Events);
    }

    [Fact]
    public async Task Rance_death_after_the_prologue_is_game_over()
    {
        var fx = Arrange(1);
        fx.Unit(1).HP = 1;
        fx.State.Bt_ETroopVsCmdrShots = 1;
        fx.State.Bt_ETroopVsCmdrDmg = 1;
        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(fx.Battle.ApplyExchangeAsync);
        Assert.Equal("OVER.ADV", jump.File);
        Assert.Equal(1, (int)fx.State.EndingId);
        Assert.Equal(0, (int)fx.Unit(1).Status);
        Assert.Equal(1, (int)fx.State.Bt_PlayerLosses);
        Assert.Equal([2], fx.Random.Draws);
        Assert.Equal(new MessageEvent(new ScriptRef("FIGTﾐｶDE.ADV", 35)), fx.Events<MessageEvent>()[^1]);
    }

    [Fact]
    public async Task Rance_only_faints_in_the_prologue()
    {
        var fx = Arrange(1, 1);
        fx.State.StoryStage = 0;
        fx.Unit(1).HP = 1;
        fx.State.Bt_ETroopVsCmdrDmg = 1;
        await fx.Battle.ApplyExchangeAsync();
        Assert.Equal(0, (int)fx.Unit(1).Status);
        Assert.Equal(0, (int)fx.State.EndingId);
        Assert.Equal(
            [new MessageEvent(new ScriptRef("FIGTﾐｶDE.ADV", 4)), new MessageEvent(new ScriptRef("FIGTﾐｶDE.ADV", 13))],
            fx.Events<MessageEvent>());
        Assert.Equal(0, (int)fx.State.Bt_PlayerSlot1); // compacted
        Assert.Equal([2, 3], fx.Random.Draws);        // RND(3) still drawn; Rance's HP is 0 → no ninja
    }

    [Fact]
    public async Task Asuka_death_resets_her_state_and_falls_into_the_common_closing()
    {
        var fx = Arrange(1, 2);
        fx.State.Unit_Cur = 11;
        fx.State.Bt_PlayerSlot1 = 11;
        fx.State.F_Asuka_State = 9;
        fx.Unit(11).HP = 2;
        fx.State.Bt_ECmdrVsCmdrDmg = 2;
        await fx.Battle.ApplyExchangeAsync();
        Assert.Equal(0, (int)fx.State.F_Asuka_State);
        Assert.Equal(
            [231, 238, 724],
            fx.Events<MessageEvent>().Select(e => e.Text.Line));
        Assert.Contains(new PrintStringEvent(1), fx.View.Events);
    }

    [Fact]
    public async Task Unit_51_at_zero_hp_does_not_die()
    {
        // Goliath's record has HP 0; Unit_Cur = 51 skips the death branch, and HP 0 blocks the ninja.
        var fx = Arrange(1, 1);
        fx.State.Unit_Cur = 51;
        fx.Unit(51).Status = 1;
        fx.Unit(51).HP = 0;
        await fx.Battle.ApplyExchangeAsync();
        Assert.Equal(1, (int)fx.Unit(51).Status);
        Assert.Equal(0, (int)fx.State.Bt_PlayerLosses);
        Assert.Equal([2, 3], fx.Random.Draws);
        Assert.Empty(fx.Events<MessageEvent>());
    }

    [Theory]
    [InlineData(4, 5, 1, 0)] // enemy-initiated physical
    [InlineData(2, 5, 1, 0)] // enemy magic (FIGT_M wrote 2)
    [InlineData(3, 1, 1, 0)] // Q14: commander HP must be > 1
    [InlineData(3, 5, 2, 0)] // RND(3) ≠ 1
    [InlineData(1, 5, 3, 0)]
    [InlineData(3, 5, 1, 1)] // Kanami already used
    public async Task Ninja_is_not_called(int msgMode, int cmdrHp, int roll, int used)
    {
        var fx = Arrange(1, roll);
        fx.State.Unit_Cur = 10; // Menad
        fx.State.Bt_PlayerSlot1 = 10;
        fx.Unit(10).HP = 8;
        fx.Cmdr(1).HP = (Sys3Int)cmdrHp;
        fx.State.Bt_MsgMode = (Sys3Int)msgMode;
        fx.State.F_Kanami_NinjaUsed = (Sys3Int)used;
        await fx.Battle.ApplyExchangeAsync();
        Assert.Equal(10, (int)fx.State.Unit_Cur);
        Assert.Equal([2, 3], fx.Random.Draws);
    }

    // ---- the ninja set-up (FIGT.ADV:1917-1982) ----

    [Theory]
    [InlineData(10, "Kanami", 1164, 9)]
    [InlineData(21, "Freya", 1176, 449)]
    [InlineData(37, "Kaoru", 1182, 72)]
    [InlineData(39, "Wichita", 1185, 71)]
    public void SetUpNinja_builds_the_ninja_in_record_51(int unit, string name, int cg, int face)
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Unit_Cur = (Sys3Int)unit;
        s.N_Hubert_FreyaUses = 4;
        fx.Unit(51).Status = 0;
        fx.Unit(51).Attacks = 100; // stale, kept
        Assert.True(fx.Battle.SetUpNinja());
        Assert.Equal(51, (int)s.Unit_Cur);
        Assert.Equal(name, s.Strings[1]);
        Assert.Equal("　", s.Strings[2]);
        Assert.Equal(cg, (int)fx.Unit(51).CgFull);
        Assert.Equal(cg, (int)fx.Unit(51).CgPartial);
        Assert.Equal(cg, (int)fx.Unit(51).CgAlone);
        Assert.Equal(face, (int)fx.Unit(51).FaceCg);
        Assert.Equal(1, (int)fx.Unit(51).Status);
        Assert.Equal(100, (int)fx.Unit(51).Attacks);
        Assert.Equal(unit == 21 ? 5 : 4, (int)s.N_Hubert_FreyaUses);
        Assert.Equal(0, (int)s.F_Kanami_NinjaUsed + (int)s.F_Freya_NinjaUsed + (int)s.F_Kaoru_NinjaUsed + (int)s.F_Wichita_NinjaUsed);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(21)]
    [InlineData(37)]
    [InlineData(39)]
    [InlineData(22)]
    public void SetUpNinja_needs_an_owner_with_the_flag_clear(int unit)
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Unit_Cur = (Sys3Int)unit;
        s.F_Kanami_NinjaUsed = 1;
        s.F_Freya_NinjaUsed = 1;
        s.F_Kaoru_NinjaUsed = 1;
        s.F_Wichita_NinjaUsed = 1;
        s.N_Hubert_FreyaUses = 4;
        Assert.False(fx.Battle.SetUpNinja());
        Assert.Equal(unit, (int)s.Unit_Cur);
        Assert.Equal(4, (int)s.N_Hubert_FreyaUses);
    }

    [Fact]
    public void PrepareNinjaFollowUp_copies_strings_and_keeps_stale_fields()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Unit_StrBase = 20;
        s.Unit_Cur = 51;
        s.Strings[1] = "Kanami";
        s.Strings[2] = "　";
        s.Strings[3] = "leftover";
        fx.Unit(51).Attacks = 80;
        fx.Unit(51).Level = 7;
        fx.Unit(51).Troops = 40;
        s.Bt_MsgMode = 4;
        fx.Battle.PrepareNinjaFollowUp();
        Assert.Equal("Kanami", s.Strings[170]); // 20 + (51 - 1) * 3
        Assert.Equal("　", s.Strings[171]);
        Assert.Equal("leftover", s.Strings[172]);
        Assert.Equal(0, (int)fx.Unit(51).Troops);
        Assert.Equal(0, (int)fx.Unit(51).MaxTroops);
        Assert.Equal(3, (int)fx.Unit(51).HP);
        Assert.Equal(3, (int)fx.Unit(51).MaxHP);
        Assert.Equal(80, (int)fx.Unit(51).Attacks);
        Assert.Equal(7, (int)fx.Unit(51).Level);
        Assert.Equal(3, (int)s.Bt_SpecialAtk);
        Assert.Equal(0, (int)s.Bt_PlayerAction);
        Assert.Equal(99, (int)s.Bt_EnemyAction);
        Assert.Equal(4, (int)s.Bt_MsgMode); // not set (Q11)
    }
}
