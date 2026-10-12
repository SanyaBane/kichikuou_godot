using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// The attack setters, <c>SpendFocus</c>, <c>ManualAttackPrelude</c>, the Goliath set-up and the
/// state part of <c>PresentEngagement</c> (<c>FIGT.ADV:1120-1143</c>, <c>1265-1278</c>,
/// <c>1492-1829</c>; docs/battle/02-resolution.md §2-§4).
/// </summary>
/// <remarks>
/// These paths continue into the flavour text (part R2). The tests let that call fail — the stub
/// throws before the merge, and after it the first FIGT_M draw finds no scripted value — and
/// check the state written up to it, which is the same either way.
/// </remarks>
public class ResolutionCommandTests
{
    // ---- L_01dd3 SpendFocus ----

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SpendFocus_takes_one_from_the_acting_slot(int slot)
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Bt_PSlot1Focus = 2;
        s.Bt_PSlot2Focus = 2;
        s.Bt_PSlot3Focus = 2;
        s.Bt_PSlot4Focus = 2;
        s.Bt_PlayerActSlot = (Sys3Int)slot;
        fx.Battle.SpendFocus();
        int[] focus = [(int)s.Bt_PSlot1Focus, (int)s.Bt_PSlot2Focus, (int)s.Bt_PSlot3Focus, (int)s.Bt_PSlot4Focus];
        Assert.Equal(Enumerable.Range(1, 4).Select(k => k == slot ? 1 : 2), focus);
    }

    [Fact]
    public void SpendFocus_saturates_at_zero()
    {
        var fx = ResolutionFixture.Create();
        fx.State.Bt_PlayerActSlot = 3;
        fx.Battle.SpendFocus();
        Assert.Equal(0, (int)fx.State.Bt_PSlot3Focus);
    }

    // ---- setters + ManualAttackPrelude ----

    private static ResolutionFixture ArrangeAttack(int command, params int[] draws)
    {
        var fx = ResolutionFixture.Create(draws);
        var s = fx.State;
        s.RetVal = (Sys3Int)command;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 3;
        s.Bt_PlayerSlot1 = 1;
        s.Bt_EnemySlot2 = 3;
        s.Bt_PlayerActSlot = 1;
        s.Bt_EnemyActSlot = 2;
        s.Bt_PSlot1Focus = 5;
        s.Bt_TroopLimit = 5000;
        s.Strings[7] = "old";
        return fx;
    }

    [Theory]
    // command, PA, EA, special, sfx, focus left, MsgMode, string 7
    [InlineData(1, 0, 0, 0, 0, 5, 3, "old")]
    [InlineData(2, 1, 99, 0, 0, 5, 3, "old")]
    [InlineData(3, 2, 99, 0, 0, 5, 1, "old")]
    [InlineData(10, 0, 99, 100, 9, 5, 3, "Cheater Beam")]
    [InlineData(11, 0, 99, 1, 1, 4, 3, "Rance Attack")]
    [InlineData(12, 0, 99, 1, 2, 4, 3, "By la Way")]
    [InlineData(13, 2, 99, 2, 4, 3, 1, "White Destruction Beam")]
    [InlineData(14, 0, 99, 3, 5, 4, 3, "Highspeed")]
    [InlineData(15, 2, 99, 4, 7, 2, 1, "Vanish Bomb")]
    [InlineData(16, 0, 99, 5, 8, 4, 3, "old")]
    [InlineData(17, 2, 99, 2, 4, 3, 1, "White Destruction Beam")]
    [InlineData(18, 0, 99, 3, 5, 4, 3, "Highspeed")]
    [InlineData(19, 2, 99, 6, 6, 4, 1, "Chuhai")]
    [InlineData(20, 0, 0, 7, 3, 4, 3, "old")]
    [InlineData(21, 2, 99, 2, 4, 3, 1, "White Destruction Beam")]
    public async Task Attack_commands_set_the_exchange_inputs(
        int command, int pa, int ea, int special, int sfx, int focus, int msgMode, string name)
    {
        var fx = ArrangeAttack(command, 5);
        await fx.RunIntoFlavourAsync(fx.Battle.PlayerAttackAsync);
        var s = fx.State;
        Assert.Equal(pa, (int)s.Bt_PlayerAction);
        Assert.Equal(ea, (int)s.Bt_EnemyAction);
        Assert.Equal(special, (int)s.Bt_SpecialAtk);
        Assert.Equal(sfx, (int)s.Bt_SkillSfx);
        Assert.Equal(focus, (int)s.Bt_PSlot1Focus);
        Assert.Equal(msgMode, (int)s.Bt_MsgMode); // 3, or 1 set by ResolveMagic
        Assert.Equal(name, s.Strings[7]);
        fx.AssertDrawsStartWith(10);
        // the target panel (enemy slot 2 = panel 6) is highlighted before the duel screen
        int highlight = fx.View.Events.IndexOf(new PanelHighlightEvent(6));
        Assert.True(highlight >= 0);
        Assert.True(highlight < fx.View.Events.FindIndex(e => e is EngagementSceneEvent));
    }

    [Fact]
    public async Task Miki_can_turn_any_manual_attack_into_vanish_bomb_repeatedly()
    {
        // Q7: RND(10) = 1 twice → two conversions, 3 Focus each (4 → 1 → 0), then 5 → go.
        var fx = ArrangeAttack(1, 1, 1, 5);
        fx.State.Unit_Cur = 32;
        fx.State.Bt_PlayerSlot1 = 32;
        fx.State.Bt_PSlot1Focus = 4;
        await fx.RunIntoFlavourAsync(fx.Battle.PlayerAttackAsync);
        var s = fx.State;
        Assert.Equal(4, (int)s.Bt_SpecialAtk);
        Assert.Equal(2, (int)s.Bt_PlayerAction);
        Assert.Equal(99, (int)s.Bt_EnemyAction);
        Assert.Equal(7, (int)s.Bt_SkillSfx);
        Assert.Equal(0, (int)s.Bt_PSlot1Focus);
        Assert.Equal("Vanish Bomb", s.Strings[7]);
        Assert.Equal(1, (int)s.Bt_MsgMode);
        fx.AssertDrawsStartWith(10, 10, 10);
    }

    [Fact]
    public async Task Other_units_ignore_the_miki_roll()
    {
        var fx = ArrangeAttack(1, 1);
        await fx.RunIntoFlavourAsync(fx.Battle.PlayerAttackAsync);
        Assert.Equal(0, (int)fx.State.Bt_SpecialAtk);
        Assert.Equal(0, (int)fx.State.Bt_PlayerAction);
        fx.AssertDrawsStartWith(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(30)]
    public async Task Non_attack_commands_are_rejected(int command)
    {
        var fx = ArrangeAttack(command);
        await Assert.ThrowsAsync<InvalidOperationException>(fx.Battle.PlayerAttackAsync);
        Assert.Empty(fx.Random.Draws);
    }

    // ---- L_01abd Goliath set-up ----

    [Fact]
    public void PrepareGoliath_builds_record_51_and_targets_enemy_slot_1()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Goliath_State = 1;
        s.Unit_StrBase = 20;
        s.Bt_EnemySlot1 = 44;
        s.Bt_MsgMode = 2;
        var u = fx.Unit(51);
        u.Attacks = 80; u.Atk = 15; u.Class = 0; u.Item = 1; // stale kaiju fields
        u.CgFull = 1158; u.HP = 18; u.MaxHP = 18; u.Troops = 7;

        fx.Battle.PrepareGoliath();

        Assert.Equal(2, (int)s.Goliath_State);
        Assert.Equal(51, (int)s.Unit_Cur);
        Assert.Equal("T2 Goliath", s.Strings[1]);
        Assert.Equal("T2 Goliath", s.Strings[170]);
        Assert.Equal("　", s.Strings[171]);
        Assert.Equal(0, (int)u.CgFull + (int)u.CgPartial + (int)u.CgAlone + (int)u.FaceCg);
        Assert.Equal(1, (int)u.Status);
        Assert.Equal(0, (int)u.Troops + (int)u.MaxTroops + (int)u.HP + (int)u.MaxHP);
        Assert.Equal(80, (int)u.Attacks); // 02 §1.4: stale
        Assert.Equal(1, (int)u.Item);
        Assert.Equal(1, (int)s.Bt_PlayerActSlot);
        Assert.Equal(1, (int)s.Bt_EnemyActSlot);
        Assert.Equal(44, (int)s.Cmdr_Cur);
        Assert.Equal(10, (int)s.Bt_SpecialAtk);
        Assert.Equal(0, (int)s.Bt_PlayerAction);
        Assert.Equal(99, (int)s.Bt_EnemyAction);
        Assert.Equal(2, (int)s.Bt_MsgMode); // Q11: stale
    }

    // ---- L_02609 PresentEngagement, state part before FIGT_M ----

    private static ResolutionFixture ArrangeEngagement()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Unit_Cur = 3;
        s.Cmdr_Cur = 4;
        s.Cmdr_StrBase = 200;
        s.Strings[209] = "Commander four"; // 200 + (4 - 1) * 3
        s.Bt_TroopLimit = 100;
        s.Bt_BackgroundCg = 1200;
        s.Bt_EnemyParty1 = 4;
        var u = fx.Unit(3);
        u.Troops = 100; u.CgFull = 11; u.CgPartial = 12; u.CgAlone = 13; u.FaceCg = 33; u.HP = 25;
        u.OffAtkMod = 2; u.OffDefMod = 12; u.DefAtkMod = 3; u.DefDefMod = 13; u.DgnAtkMod = 4; u.DgnDefMod = 14;
        var c = fx.Cmdr(4);
        c.Troops = 50; c.CgFull = 21; c.CgPartial = 22; c.CgAlone = 23; c.FaceCg = 44; c.HP = 47;
        return fx;
    }

    [Theory]
    [InlineData(0, 0, 0)] // field battle: no copy
    [InlineData(1, 2, 12)]
    [InlineData(2, 3, 13)]
    [InlineData(3, 4, 14)]
    public async Task PresentEngagement_copies_the_situational_modifiers(int kind, int atk, int def)
    {
        var fx = ArrangeEngagement();
        fx.State.Bt_Kind = (Sys3Int)kind;
        await fx.RunIntoFlavourAsync(fx.Battle.PresentEngagementAsync);
        Assert.Equal(atk, (int)fx.Unit(3).CurAtkMod);
        Assert.Equal(def, (int)fx.Unit(3).CurDefMod);
    }

    [Fact]
    public async Task PresentEngagement_events_and_counters_before_the_flavour_text()
    {
        var fx = ArrangeEngagement();
        await fx.RunIntoFlavourAsync(fx.Battle.PresentEngagementAsync);
        var s = fx.State;
        Assert.Equal("Commander four", s.Strings[4]);
        Assert.Equal(10, (int)s.Ui_WaoOp);
        Assert.Equal(
            [
                new WindowEvent(143, 0),
                new ActionLabelsEvent(new ScriptRef("FIGT.ADV", 1753), new ScriptRef("FIGT.ADV", 1786)),
                new WindowEvent(11, 0),
                new WindowEvent(10, 0),
                new EngagementSlideEvent(112),
                new CountersEvent(100, 50, 25, 47),
            ],
            fx.View.Events.Skip(1));
        var scene = Assert.IsType<EngagementSceneEvent>(fx.View.Events[0]);
        Assert.Equal(1200, (int)scene.Background);
        Assert.Equal([(Sys3Int)22], scene.EnemySprites);
        Assert.Equal([(Sys3Int)11], scene.PlayerSprites);
        Assert.True(scene.Overlay);
        Assert.Equal(33, (int)scene.PlayerFace);
        Assert.Equal(44, (int)scene.EnemyFace);
        // L_02c00's last number: commander HP 47 at (480, 768), 5 digits
        Assert.Equal(480 + 4 * 32, (int)s.Ui_MouseX);
        Assert.Equal(768, (int)s.Ui_MouseY);
        Assert.Equal(0, (int)s.Tmp0023);
        Assert.Equal(7, (int)s.Tmp0024);
        Assert.Equal(11, (int)s.Tmp0025);
    }

    [Fact]
    public async Task PresentEngagement_story_stage_4_speaks_as_commander_200()
    {
        // Q-22: Cmdr_Cur = 200 during FIGT_M; the slide-in counters already read commander 200.
        var fx = ArrangeEngagement();
        fx.State.StoryStage = 4;
        fx.Cmdr(200).HP = 9;
        await fx.RunIntoFlavourAsync(fx.Battle.PresentEngagementAsync);
        Assert.Equal(200, (int)fx.State.Cmdr_Cur);
        Assert.Equal(4, (int)fx.State.Tmp0229);
        Assert.Equal(new CountersEvent(100, 0, 25, 9), fx.View.Events[^1]);
        Assert.Equal("Commander four", fx.State.Strings[4]); // copied before the swap
    }

    [Theory]
    [InlineData(100, 100, new[] { 11 })]
    [InlineData(150, 100, new[] { 11 })]
    [InlineData(50, 100, new[] { 12 })]
    [InlineData(0, 100, new[] { 13 })]
    [InlineData(0, 0, new[] { 11, 13 })] // limit 0: both "full" and "alone" are drawn
    public async Task PresentEngagement_player_sprite_by_troops(int troops, int limit, int[] sprites)
    {
        var fx = ArrangeEngagement();
        fx.Unit(3).Troops = (Sys3Int)troops;
        fx.State.Bt_TroopLimit = (Sys3Int)limit;
        await fx.RunIntoFlavourAsync(fx.Battle.PresentEngagementAsync);
        var scene = fx.Events<EngagementSceneEvent>().Single();
        Assert.Equal(sprites, scene.PlayerSprites.Select(x => (int)x));
    }

    [Theory]
    [InlineData(1, 0, new[] { 1520 })]   // Rance Attack
    [InlineData(51, 1176, new[] { 1176 })] // Freya
    [InlineData(51, 0, new int[0])]      // Goliath: nothing
    public async Task PresentEngagement_special_attack_sprite(int unit, int cgFull, int[] sprites)
    {
        var fx = ArrangeEngagement();
        fx.State.Unit_Cur = (Sys3Int)unit;
        fx.Unit(unit).CgFull = (Sys3Int)cgFull;
        fx.State.Bt_SpecialAtk = 3;
        await fx.RunIntoFlavourAsync(fx.Battle.PresentEngagementAsync);
        var scene = fx.Events<EngagementSceneEvent>().Single();
        Assert.Equal(sprites, scene.PlayerSprites.Select(x => (int)x));
        Assert.Equal(
            new ActionLabelsEvent(new ScriptRef("FIGT.ADV", 1748), new ScriptRef("FIGT.ADV", 1781)),
            fx.Events<ActionLabelsEvent>().Single());
    }

    [Theory]
    [InlineData(99, 2, 1765, 1794)]
    [InlineData(1, 1, 1757, 1790)]
    [InlineData(2, 99, 1761, 1798)]
    [InlineData(5, 7, null, null)]
    public async Task PresentEngagement_action_labels(int pa, int ea, int? playerLine, int? enemyLine)
    {
        var fx = ArrangeEngagement();
        fx.State.Bt_PlayerAction = (Sys3Int)pa;
        fx.State.Bt_EnemyAction = (Sys3Int)ea;
        await fx.RunIntoFlavourAsync(fx.Battle.PresentEngagementAsync);
        var labels = fx.Events<ActionLabelsEvent>().Single();
        Assert.Equal(playerLine, labels.PlayerLabel?.Line);
        Assert.Equal(enemyLine, labels.EnemyLabel?.Line);
    }
}
