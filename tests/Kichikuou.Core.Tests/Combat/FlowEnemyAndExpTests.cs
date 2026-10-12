using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Script;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>FIGT.ADV:1411-1491 — the enemy's in-battle AI (01 §8.5).</summary>
public class FlowEnemyTests
{
    private static FlowRig Enemy(int cls, int enemySlot, int playerSlots, params int[] draws)
    {
        var rig = new FlowRig(draws);
        for (int i = 1; i <= playerSlots; i++)
            rig.SetPlayerSlot(i, 10 + i);
        rig.SetEnemySlot(enemySlot, 60);
        rig.State.Cmdr_Cur = 60;
        rig.State.Bt_EnemyActSlot = (Sys3Int)enemySlot;
        rig.State.Cmdrs[60].Class = (Sys3Int)cls;
        rig.State.Cmdrs[60].Troops = 500;
        rig.State.Cmdrs[60].MaxTroops = 1000;
        return rig;
    }

    [Fact]
    public void Target_is_the_last_of_the_slot_draws()
    {
        var rig = Enemy(2, 1, 4, 1, 2, 3);
        rig.Battle.EnemyChooseTarget();
        Assert.Equal([2, 3, 4], rig.Random.Draws);
        Is(3, rig.State.RetVal);
        Is(3, rig.State.Bt_PlayerActSlot);
        Is(13, rig.State.Unit_Cur);
    }

    [Fact]
    public void Melee_rerolls_a_back_row_target()
    {
        var rig = Enemy(10, 1, 4, 2, 3, 4, 2);
        rig.Battle.EnemyChooseTarget();
        Assert.Equal([2, 3, 4, 2], rig.Random.Draws);
        Is(2, rig.State.Bt_PlayerActSlot);
        Is(12, rig.State.Unit_Cur);
    }

    [Fact]
    public void Melee_keeps_a_front_row_target()
    {
        var rig = Enemy(0, 1, 3, 1, 2);
        rig.Battle.EnemyChooseTarget();
        Assert.Equal([2, 3], rig.Random.Draws);
        Is(2, rig.State.Bt_PlayerActSlot);
    }

    [Fact]
    public void A_single_player_unit_is_targeted_without_a_draw()
    {
        var rig = Enemy(0, 1, 1);
        rig.Battle.EnemyChooseTarget();
        Assert.Empty(rig.Random.Draws);
        Is(1, rig.State.Bt_PlayerActSlot);
        Is(11, rig.State.Unit_Cur);
    }

    [Theory]
    [InlineData(199, 0, true)]    // below a fifth of 1000
    [InlineData(200, 0, false)]
    [InlineData(100, 9, false)]   // demons never defend
    [InlineData(100, 8, true)]
    public void A_weak_commander_defends_on_1(int troops, int cls, bool defends)
    {
        var rig = Enemy(cls, 2, 2, 1);
        rig.State.Cmdrs[60].Troops = (Sys3Int)troops;
        rig.State.Bt_PlayerActSlot = 1;
        var kind = rig.Battle.EnemyDecide();
        Assert.Equal(defends ? 20 : 0, (int)rig.State.Bt_ESlot2State);
        if (defends)
            Assert.Equal(Battle.AutoKind.None, kind);
        Assert.Equal([3], rig.Random.Draws);
    }

    [Fact]
    public void A_weak_commander_attacks_unless_the_roll_is_1()
    {
        var rig = Enemy(0, 1, 2, 2);
        rig.State.Cmdrs[60].Troops = 10;
        rig.State.Bt_PlayerActSlot = 1;
        Assert.Equal(Battle.AutoKind.Resolve, rig.Battle.EnemyDecide());
        Is(0, rig.State.Bt_ESlot1State);
    }

    [Theory]
    [InlineData(0, 3, 1, (int)Battle.AutoKind.None, -1, -1)]          // back-row melee idles
    [InlineData(10, 2, 1, (int)Battle.AutoKind.Resolve, 0, 0)]
    [InlineData(1, 3, 1, (int)Battle.AutoKind.Resolve, 1, 99)]        // archer at range
    [InlineData(1, 1, 3, (int)Battle.AutoKind.Resolve, 1, 99)]
    [InlineData(1, 2, 2, (int)Battle.AutoKind.Resolve, 0, 0)]         // Q-14: front vs front trades blows
    [InlineData(2, 4, 4, (int)Battle.AutoKind.ResolveMagic, 2, 99)]
    [InlineData(11, 1, 1, (int)Battle.AutoKind.ResolveMagic, 2, 99)]
    [InlineData(3, 1, 1, (int)Battle.AutoKind.None, -1, -1)]          // Q-14: classes 3, 4, 5 never act
    [InlineData(4, 1, 1, (int)Battle.AutoKind.None, -1, -1)]
    [InlineData(5, 1, 1, (int)Battle.AutoKind.None, -1, -1)]
    public void Actions_by_class(int cls, int enemySlot, int target, int kindCode, int ea, int pa)
    {
        var kind = (Battle.AutoKind)kindCode;
        var rig = Enemy(cls, enemySlot, 4, 2);
        rig.State.Bt_PlayerActSlot = (Sys3Int)target;
        rig.State.Bt_EnemyAction = 77;
        rig.State.Bt_PlayerAction = 77;
        Assert.Equal(kind, rig.Battle.EnemyDecide());
        Is(ea < 0 ? 77 : ea, rig.State.Bt_EnemyAction);
        Is(pa < 0 ? 77 : pa, rig.State.Bt_PlayerAction);
    }
}

/// <summary>FIGT.ADV:2977-3160 — EXP and level-up (01 §11.1-§11.2).</summary>
public class FlowExpTests
{
    private static FlowRig Rig(params int[] draws)
    {
        var rig = new FlowRig(draws);
        rig.PlayerSlots((3, 100));
        return rig;
    }

    [Fact]
    public async Task Exp_is_5_plus_rnd15_for_every_survivor()
    {
        var rig = Rig(10);
        rig.State.Units[3].Exp = 20;
        rig.State.Bt_PlayerLosses = 33;
        rig.State.History[0].Casualties = 100;
        rig.State.Hist_Cur = 5;
        await rig.Battle.AwardExpAsync();
        Is(15, rig.State.Tmp0001);
        Is(35, rig.State.Units[3].Exp);
        Is(0, rig.State.Hist_Cur);
        Is(133, rig.State.History[0].Casualties);
        Assert.Equal([15], rig.Random.Draws);
        var shown = Assert.Single(rig.View.Events.OfType<ValueMessageEvent>());
        Assert.Equal(3003, shown.Text.Line);
        Assert.Equal([2999, 3003], rig.Lines);
        Is(33, shown.Values[0]);
        Is(15, shown.Values[2]);
    }

    [Fact]
    public async Task Rebecca_bonus_replaces_the_exp_with_rnd50()
    {
        var rig = Rig(15, 40);
        rig.State.Bt_ExpBonusNext = 1;
        rig.State.StoryStage = 2;
        await rig.Battle.AwardExpAsync();
        Is(40, rig.State.Tmp0001);
        Is(0, rig.State.Bt_ExpBonusNext);
        Assert.Equal([15, 50], rig.Random.Draws);
        Assert.Equal([2994, 3003], rig.Lines);
    }

    [Theory]
    [InlineData(3, 2, 1)]
    [InlineData(3, 1, 0)]   // dungeon loss: 1 / 3
    [InlineData(1, 1, 5)]   // 15 / 3
    [InlineData(2, 0, 15)]
    public async Task Exp_by_kind_and_result(int kind, int result, int exp)
    {
        var rig = Rig(10);
        rig.State.Bt_Kind = (Sys3Int)kind;
        rig.State.Bt_Result = (Sys3Int)result;
        await rig.Battle.AwardExpAsync();
        Is(exp, rig.State.Tmp0001);
        Is(exp, rig.State.Units[3].Exp);
    }

    [Theory]
    [InlineData(201, 4)]
    [InlineData(200, 3)]
    public async Task Garrison_hits_count_heavy_enemy_losses(int losses, int hits)
    {
        var rig = Rig(1);
        rig.State.Bt_GarrisonProv = 20;
        rig.State.Prov_Cur = 20;
        rig.State.Bt_GarrisonHits = 3;
        rig.State.Bt_EnemyLosses = (Sys3Int)losses;
        await rig.Battle.AwardExpAsync();
        Is(hits, rig.State.Bt_GarrisonHits);
    }

    [Fact]
    public async Task Level_up_raises_a_modifier_above_its_value()
    {
        // Level 2 → threshold 160; 150 + 15 = 165.
        var rig = Rig(10, 3, 1, 2);
        var u = rig.State.Units[3];
        u.Level = 2;
        u.Exp = 150;
        u.Attacks = 4;
        u.Recovery = 5;
        u.OffAtkMod = 1;
        await rig.Battle.AwardExpAsync();
        Is(3, u.Level);
        Is(0, u.Exp);
        Is(5, u.Attacks);
        Is(8, u.Recovery);
        Is(2, u.OffAtkMod);
        Assert.Equal([15, 5, 15, 3], rig.Random.Draws);   // R15, R17, R18 = 12 + new level, R19
        Assert.Contains(3106, rig.Lines);
        Assert.Equal(3157, rig.Lines[^1]);
        var sound = Assert.Single(rig.View.Events.OfType<SoundEvent>());
        Is(104, sound.Sound);
    }

    [Fact]
    public async Task Surplus_exp_is_discarded()
    {
        var rig = Rig(15, 7);   // 20 exp, R18 = 7: nothing
        var u = rig.State.Units[3];
        u.Exp = 99;
        await rig.Battle.AwardExpAsync();
        Is(1, u.Level);
        Is(0, u.Exp);
        Assert.Equal([15, 13], rig.Random.Draws);   // no recovery draw (Recovery 0), no R19
    }

    [Fact]
    public async Task Up_is_shown_even_when_nothing_rose()
    {
        // QUIRK Q-13: modifier 3 never rises; R18 = 6 (dungeon defence), R19 = 3.
        var rig = Rig(15, 6, 3);
        var u = rig.State.Units[3];
        u.Exp = 99;
        u.DgnDefMod = 3;
        await rig.Battle.AwardExpAsync();
        Is(3, u.DgnDefMod);
        Assert.DoesNotContain(3151, rig.Lines);
        Assert.Equal(3157, rig.Lines[^1]);
    }

    [Fact]
    public async Task Negative_encodings_never_rise()
    {
        var rig = Rig(15, 4, 3);
        var u = rig.State.Units[3];
        u.Exp = 99;
        u.OffDefMod = 12;   // −2
        await rig.Battle.AwardExpAsync();
        Is(12, u.OffDefMod);
    }

    [Fact]
    public async Task Happiness_ring_doubles_exp()
    {
        var rig = Rig(5);
        rig.State.Units[3].Item = 29;
        await rig.Battle.AwardExpAsync();
        Is(20, rig.State.Units[3].Exp);
    }

    [Fact]
    public async Task Mill_summons_50_more_and_does_not_roll_a_modifier()
    {
        var rig = new FlowRig(15, 2);
        rig.PlayerSlots((25, 100));
        var u = rig.State.Units[25];
        u.Exp = 99;
        u.Recovery = 4;
        u.MaxTroops = 200;
        await rig.Battle.AwardExpAsync();
        Is(250, u.MaxTroops);
        Is(250, u.Recovery);   // the recovery roll is overwritten
        Assert.Equal([15, 5], rig.Random.Draws);
        Assert.Equal(3077, rig.Lines[^1]);
    }

    [Fact]
    public async Task Popularity_staff_adds_troops_by_the_new_level()
    {
        var rig = Rig(15);
        var u = rig.State.Units[3];
        u.Exp = 99;
        u.Item = 3;
        u.MaxTroops = 300;   // new level 1: 300 / 12 = 25
        await rig.Battle.AwardExpAsync();
        Is(325, u.MaxTroops);
        Is(25, rig.State.Tmp0005);
        Assert.Equal([15], rig.Random.Draws);
        var shown = rig.View.Events.OfType<ValueMessageEvent>().Last();
        Assert.Equal(3088, shown.Text.Line);
        Is(25, shown.Values[0]);
    }

    [Fact]
    public async Task Popularity_staff_without_troops_rolls_a_modifier()
    {
        var rig = Rig(15, 2, 1);
        var u = rig.State.Units[3];
        u.Exp = 99;
        u.Item = 3;
        u.MaxTroops = 0;
        await rig.Battle.AwardExpAsync();
        Is(1, u.DefAtkMod);
        Assert.Equal([15, 13, 3], rig.Random.Draws);
    }

    [Fact]
    public async Task Survivors_level_up_in_slot_order()
    {
        var rig = new FlowRig(15, 8, 9);
        rig.PlayerSlots((3, 10), (4, 10));
        rig.State.Units[3].Exp = 99;
        rig.State.Units[4].Exp = 99;
        rig.State.Units[4].Level = 2;   // threshold 160: no level
        rig.State.Units[4].Exp = 80;
        await rig.Battle.AwardExpAsync();
        Is(1, rig.State.Units[3].Level);
        Is(100, rig.State.Units[4].Exp);
        Is(4, rig.State.Unit_Cur);
        Assert.Equal([15, 13], rig.Random.Draws);
        Assert.Equal(1, rig.Random.Remaining);
    }
}
