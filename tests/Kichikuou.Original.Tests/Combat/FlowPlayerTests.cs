using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using static Kichikuou.Original.Tests.Combat.Sys3Assert;

namespace Kichikuou.Original.Tests.Combat;

/// <summary>The player turn: FIGT.ADV:595-1410 and FIGT_SEL.ADV (01 §8.1-§8.4, §8.6, §8.7).</summary>
public class FlowPlayerTests
{
    /// <summary>Unit <paramref name="unit"/> of class <paramref name="cls"/> acts from <paramref name="slot"/>; 4 enemies.</summary>
    private static FlowRig Acting(int unit, int cls, int slot = 1, int enemies = 4, params int[] draws)
    {
        var rig = new FlowRig(draws);
        var s = rig.State;
        for (int i = 1; i <= 4; i++)
            rig.SetPlayerSlot(i, i == slot ? unit : (i < slot || i <= 2 ? 40 + i : 0));
        for (int i = 1; i <= enemies; i++)
            rig.SetEnemySlot(i, 100 + i);
        s.Units[unit].Class = (Sys3Int)cls;
        s.Unit_Cur = (Sys3Int)unit;
        s.Bt_PlayerActSlot = (Sys3Int)slot;
        s.Bt_PlayerAtkType = cls == 1 ? (Sys3Int)1 : cls is 2 or 3 or 11 ? (Sys3Int)2 : Sys3Int.Zero;
        return rig;
    }

    private static List<int> Targets(FigtSelMenu menu, int command) =>
        menu.Find((Sys3Int)command)?.Arguments.Select(a => (int)a).ToList() ?? [];

    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(2, 2)]
    [InlineData(3, 2)]
    [InlineData(11, 2)]
    [InlineData(4, 0)]
    [InlineData(10, 0)]
    public void Attack_type_comes_from_the_class(int cls, int type)
    {
        var rig = Acting(5, cls);
        rig.State.Bt_SpecialAtk = 3;
        Assert.Equal(FigtLabel.Select, rig.Battle.PlayerTurnAfterHighlight());
        Is(type, rig.State.Bt_PlayerAtkType);
        Is(0, rig.State.Bt_SpecialAtk);
    }

    [Fact]
    public void A_standing_order_skips_the_menu()
    {
        var rig = Acting(5, 1);
        rig.State.Bt_CurOrder = 2;
        rig.State.Bt_PlayerAtkType = 7;
        Assert.Equal(FigtLabel.AutoAction, rig.Battle.PlayerTurnAfterHighlight());
        Is(7, rig.State.Bt_PlayerAtkType);
    }

    [Fact]
    public void Button_rectangles_are_loaded_from_the_panel_table()
    {
        var rig = new FlowRig();
        rig.Battle.LoadButtonRects();
        var s = rig.State;
        Is(8, s.Ui_BtnCur);
        Is(12, s.Buttons[0].X1);
        Is(8, s.Buttons[0].Y1);
        Is(194, s.Buttons[0].X2);
        Is(92, s.Buttons[0].Y2);
        Is(448, s.Buttons[7].X1);
        Is(308, s.Buttons[7].Y1);
        Is(182, s.Buttons[7].W);
        Is(84, s.Buttons[7].H);
        Is(630, s.Buttons[7].X2);
        Is(392, s.Buttons[7].Y2);
    }

    [Fact]
    public async Task FigtSel_clears_the_buffer_and_writes_the_unread_flags()
    {
        var rig = Acting(5, 1, slot: 1, enemies: 2);
        var s = rig.State;
        for (int i = 0; i <= 100; i++)
            s.Buffer[i].Val = 50;
        rig.Input.Commands.Enqueue(new FigtSelChoice(4, 99));

        await rig.Battle.FigtSelAsync();

        Is(1, s.Buffer[1].Val);   // front row
        Is(1, s.Buffer[2].Val);   // bow
        Is(0, s.Buffer[3].Val);   // not magic
        Is(1, s.Buffer[4].Val);
        Is(1, s.Buffer[5].Val);
        Is(1, s.Buffer[6].Val);   // not a duel
        Is(0, s.Buffer[0].Val);
        Is(0, s.Buffer[99].Val);
        Is(50, s.Buffer[100].Val);
        Is(6, s.Buf_Cur);
        Is(2, s.Bt_PlayerSlotsUsed);
        Is(2, s.Bt_EnemySlotsUsed);
        Is(4, s.RetVal);
        Is(99, s.Tmp0001);
    }

    [Fact]
    public async Task FigtSel_returns_0_without_asking_when_a_side_is_empty()
    {
        var rig = Acting(5, 0, enemies: 0);
        await rig.Battle.FigtSelAsync();
        Is(0, rig.State.RetVal);
        Assert.Empty(rig.Input.CommandMenus);
    }

    [Fact]
    public void Melee_in_the_front_row_attacks_only_the_enemy_front_row()
    {
        var rig = Acting(5, 0, slot: 2);
        rig.State.Bt_PlayerSlotsUsed = 2;
        rig.State.Bt_EnemySlotsUsed = 4;
        var menu = rig.Battle.BuildFigtSelMenu();
        Assert.Equal([4, 5], Targets(menu, FigtSelCommand.Direct));
        Assert.Null(menu.Find(FigtSelCommand.Bow));
        Assert.Null(menu.Find(FigtSelCommand.Magic));
    }

    [Fact]
    public void Melee_in_the_back_row_cannot_attack()
    {
        var rig = Acting(5, 0, slot: 3);
        rig.State.Bt_PlayerSlotsUsed = 3;
        rig.State.Bt_EnemySlotsUsed = 4;
        var menu = rig.Battle.BuildFigtSelMenu();
        Assert.Null(menu.Find(FigtSelCommand.Direct));
        Assert.NotNull(menu.Find(FigtSelCommand.Defend));
        Assert.Equal([FigtSelCommand.Direct], menu.ListedModes!.Select(m => m.Value));
    }

    [Fact]
    public void Popup_lists_modes_and_items_in_script_order_without_cheat()
    {
        var rig = Acting(17, 2, slot: 1);
        rig.State.Bt_PlayerSlotsUsed = 2;
        rig.State.Bt_EnemySlotsUsed = 2;
        rig.State.Bt_ActFocus = 1;
        rig.State.DebugMode = 1;
        rig.State.Units[17].Item = 28;
        var menu = rig.Battle.BuildFigtSelMenu();
        Assert.Equal(
            [FigtSelCommand.Direct, FigtSelCommand.Magic, FigtSelCommand.FogCard, FigtSelCommand.WhiteBeam],
            menu.ListedModes!.Select(m => m.Value));
        Assert.NotNull(menu.Find(FigtSelCommand.CheaterBeam));
    }

    [Fact]
    public void Bow_reaches_every_slot_from_the_back_row_but_direct_does_not()
    {
        var rig = Acting(5, 1, slot: 4);
        rig.State.Bt_PlayerSlotsUsed = 4;
        rig.State.Bt_EnemySlotsUsed = 3;
        var menu = rig.Battle.BuildFigtSelMenu();
        Assert.Equal([4, 5, 6], Targets(menu, FigtSelCommand.Bow));
        Assert.Null(menu.Find(FigtSelCommand.Direct));
        Is(2, menu.InitialMode);
    }

    [Fact]
    public void Popup_commands_take_the_hovered_button_or_99()
    {
        var rig = Acting(5, 0);
        rig.State.Bt_PlayerSlotsUsed = 2;
        rig.State.Bt_EnemySlotsUsed = 2;
        var menu = rig.Battle.BuildFigtSelMenu();
        Assert.Equal([99, 4, 5], Targets(menu, FigtSelCommand.Defend));
        Assert.Equal([1, 2], Targets(menu, FigtSelCommand.EnemyInfo));
        Assert.Equal([1, 2], Targets(menu, FigtSelCommand.OrderAttack));
        Assert.Null(menu.Find(FigtSelCommand.Repel));
        Assert.Null(menu.Find(FigtSelCommand.CheaterBeam));
    }

    [Theory]
    [InlineData(1, 1, 0, 1, true)]
    [InlineData(1, 1, 0, 0, false)]     // no focus
    [InlineData(1, 3, 0, 1, false)]     // back row
    [InlineData(31, 1, 8, 1, true)]     // Kentarou after his story
    [InlineData(31, 1, 7, 1, false)]
    public void Rance_attack_availability(int unit, int slot, int kentarou, int focus, bool listed)
    {
        var rig = Acting(unit, 0, slot);
        rig.State.Bt_PlayerSlotsUsed = (Sys3Int)Math.Max(slot, 2);
        rig.State.Bt_EnemySlotsUsed = 2;
        rig.State.F_Kentarou_State = (Sys3Int)kentarou;
        rig.State.Bt_ActFocus = (Sys3Int)focus;
        var menu = rig.Battle.BuildFigtSelMenu();
        Assert.Equal(listed, menu.Find(FigtSelCommand.RanceAttack) is not null);
    }

    [Theory]
    [InlineData(32, 0, 2, FigtSelCommand.VanishBomb, false)]
    [InlineData(32, 0, 3, FigtSelCommand.VanishBomb, true)]
    [InlineData(17, 2, 1, FigtSelCommand.WhiteBeam, true)]
    [InlineData(39, 2, 1, FigtSelCommand.WhiteBeam, true)]
    [InlineData(40, 2, 1, FigtSelCommand.BlackBeam, true)]
    [InlineData(37, 2, 1, FigtSelCommand.EpoLight, true)]
    [InlineData(37, 0, 1, FigtSelCommand.EpoLight, false)]
    [InlineData(47, 0, 1, FigtSelCommand.SoaringWave, true)]
    [InlineData(47, 1, 1, FigtSelCommand.SoaringWave, false)]
    [InlineData(30, 0, 1, FigtSelCommand.HighSpeed, true)]
    [InlineData(27, 1, 1, FigtSelCommand.WhirlwindShot, true)]
    [InlineData(18, 0, 1, FigtSelCommand.WarDance, true)]
    [InlineData(7, 0, 1, FigtSelCommand.ByLaWay, true)]
    public void Special_availability(int unit, int cls, int focus, ushort command, bool listed)
    {
        var rig = Acting(unit, cls);
        rig.State.Bt_PlayerSlotsUsed = 2;
        rig.State.Bt_EnemySlotsUsed = 2;
        rig.State.Bt_ActFocus = (Sys3Int)focus;
        var menu = rig.Battle.BuildFigtSelMenu();
        Assert.Equal(listed, menu.Find(command) is not null);
    }

    [Theory]
    [InlineData(8, true)]
    [InlineData(7, false)]
    public void Chuhai_needs_Asuka_past_state_7(int state, bool listed)
    {
        var rig = Acting(11, 2);
        rig.State.Bt_PlayerSlotsUsed = 2;
        rig.State.Bt_EnemySlotsUsed = 2;
        rig.State.Bt_ActFocus = 1;
        rig.State.F_Asuka_State = (Sys3Int)state;
        Assert.Equal(listed, rig.Battle.BuildFigtSelMenu().Find(FigtSelCommand.Chuhai) is not null);
    }

    [Fact]
    public void Melee_specials_reach_only_the_front_row_magic_specials_everything()
    {
        var rig = Acting(18, 0, slot: 1);
        rig.State.Bt_PlayerSlotsUsed = 2;
        rig.State.Bt_EnemySlotsUsed = 4;
        rig.State.Bt_ActFocus = 1;
        Assert.Equal([4, 5], Targets(rig.Battle.BuildFigtSelMenu(), FigtSelCommand.WarDance));

        var mage = Acting(17, 2, slot: 4);
        mage.State.Bt_PlayerSlotsUsed = 4;
        mage.State.Bt_EnemySlotsUsed = 4;
        mage.State.Bt_ActFocus = 1;
        Assert.Equal([4, 5, 6, 7], Targets(mage.Battle.BuildFigtSelMenu(), FigtSelCommand.WhiteBeam));
    }

    [Fact]
    public void Debug_mode_adds_the_cheater_beam_everywhere_and_repel()
    {
        var rig = Acting(5, 0, slot: 3);
        rig.State.Bt_PlayerSlotsUsed = 3;
        rig.State.Bt_EnemySlotsUsed = 4;
        rig.State.DebugMode = 1;
        var menu = rig.Battle.BuildFigtSelMenu();
        Assert.Equal([4, 5, 6, 7], Targets(menu, FigtSelCommand.CheaterBeam));
        Assert.NotNull(menu.Find(FigtSelCommand.Repel));
    }

    [Theory]
    [InlineData(27, 0, FigtSelCommand.Amoeba, true)]
    [InlineData(26, 0, FigtSelCommand.JagiJagi, true)]
    [InlineData(28, 0, FigtSelCommand.FogCard, true)]
    [InlineData(28, 1, FigtSelCommand.FogCard, false)]   // not in duels
    [InlineData(0, 0, FigtSelCommand.Amoeba, false)]
    public void Item_commands(int item, int duel, ushort command, bool listed)
    {
        var rig = Acting(5, 0);
        rig.State.Bt_PlayerSlotsUsed = 2;
        rig.State.Bt_EnemySlotsUsed = 2;
        rig.State.Units[5].Item = (Sys3Int)item;
        rig.State.Bt_IsDuel = (Sys3Int)duel;
        Assert.Equal(listed, rig.Battle.BuildFigtSelMenu().Find(command) is not null);
    }

    [Fact]
    public async Task FigtSel_rejects_an_unavailable_answer()
    {
        var rig = Acting(5, 0, slot: 1, enemies: 4);
        rig.Input.Commands.Enqueue(new FigtSelChoice(FigtSelCommand.Direct, 6));   // enemy slot 3
        await Assert.ThrowsAsync<InvalidOperationException>(rig.Battle.FigtSelAsync);
    }

    [Fact]
    public async Task An_attack_leaves_the_command_and_button()
    {
        var rig = Acting(5, 1, slot: 1, enemies: 2);
        rig.Input.Commands.Enqueue(new FigtSelChoice(FigtSelCommand.Bow, 5));
        await rig.Battle.FigtSelAsync();
        Is(2, rig.State.RetVal);
        Is(5, rig.State.Tmp0001);
        Is(5, rig.State.Tmp0002);
        Is(2, rig.State.Tmp0019);
        Is(255, rig.State.Tmp0020);
    }

    [Fact]
    public async Task Select_binds_the_target_and_garbage_for_other_commands()
    {
        var rig = Acting(5, 0, slot: 1, enemies: 2);
        rig.State.Cmdr_StrBase = 1000;
        rig.State.Strings[1000 + 101 * 3] = "Second";
        rig.Input.Commands.Enqueue(new FigtSelChoice(FigtSelCommand.Direct, 5));
        Assert.Equal(FigtLabel.Dispatch, await rig.Battle.SelectAsync());
        Is(2, rig.State.Bt_EnemyActSlot);
        Is(102, rig.State.Cmdr_Cur);
        Is(1, rig.State.VAR0617);
        Assert.Equal("Second", rig.State.Strings[4]);

        // QUIRK Q-18: Defend with the cursor nowhere → 99 - 3 = 96, cursor untouched.
        rig.Input.Commands.Enqueue(new FigtSelChoice(FigtSelCommand.Defend, 99));
        await rig.Battle.SelectAsync();
        Is(96, rig.State.Bt_EnemyActSlot);
        Is(102, rig.State.Cmdr_Cur);

        // Defend with the cursor over enemy slot 1 binds that commander.
        rig.Input.Commands.Enqueue(new FigtSelChoice(FigtSelCommand.Defend, 4));
        await rig.Battle.SelectAsync();
        Is(1, rig.State.Bt_EnemyActSlot);
        Is(101, rig.State.Cmdr_Cur);
    }

    [Fact]
    public async Task Attack_commands_are_handed_to_R1_with_RetVal_intact()
    {
        var rig = Acting(5, 0);
        rig.State.RetVal = FigtSelCommand.RanceAttack;
        Assert.True(await FlowRig.UntilStub(async () => await rig.Battle.DispatchAsync()));
        Is(11, rig.State.RetVal);
    }

    [Theory]
    [InlineData(4, (int)FigtLabel.Defend)]
    [InlineData(6, (int)FigtLabel.PlayerRetreat)]
    [InlineData(7, (int)FigtLabel.DefenderWon)]
    public async Task Dispatch_jumps(int command, int nextCode)
    {
        var next = (FigtLabel)nextCode;
        var rig = Acting(5, 0);
        rig.State.RetVal = (Sys3Int)command;
        Assert.Equal(next, await rig.Battle.DispatchAsync());
    }

    [Fact]
    public async Task Unknown_commands_show_the_error_and_restart_the_turn()
    {
        var rig = Acting(5, 0);
        rig.State.RetVal = 40;
        Assert.Equal(FigtLabel.PlayerTurn, await rig.Battle.DispatchAsync());
        var error = Assert.Single(rig.View.Events.OfType<FlowErrorEvent>());
        Assert.Equal(772, error.Text.Line);
    }

    [Fact]
    public async Task Command_0_is_the_attack_routine_error()
    {
        var rig = Acting(5, 0);
        rig.State.RetVal = 0;
        Assert.Equal(FigtLabel.PlayerTurn, await rig.Battle.DispatchAsync());
        Assert.Equal(1260, Assert.Single(rig.View.Events.OfType<FlowErrorEvent>()).Text.Line);
    }

    [Fact]
    public async Task Defend_raises_the_slot_state_by_rnd20_plus_1()
    {
        var rig = Acting(5, 0, slot: 2, draws: [7]);
        rig.Input.Keys.Enqueue(32);
        await rig.Battle.DefendEffectAsync();
        Is(8, rig.State.Bt_PSlot2State);
        Assert.Equal([20], rig.Random.Draws);
        var shown = Assert.Single(rig.View.Events.OfType<ValueMessageEvent>());
        Assert.Equal(1399, shown.Text.Line);
        Is(8, shown.Values[0]);
        Is(32, rig.State.RetVal);   // the message wait overwrote it
    }

    [Theory]
    [InlineData(32, new[] { 1 }, 0, 1285)]   // Miki fails
    [InlineData(32, new[] { 2 }, 1, 1308)]
    [InlineData(5, new int[0], 1, 1308)]     // nobody else draws
    public async Task Focus(int unit, int[] draws, int gained, int line)
    {
        var rig = Acting(unit, 0, slot: 2, draws: draws);
        rig.State.Bt_PSlot2Focus = 2;
        await rig.Battle.FocusEffectAsync();
        Is(2 + gained, rig.State.Bt_PSlot2Focus);
        Assert.Equal(draws.Length == 0 ? new List<int>() : new List<int> { 2 }, rig.Random.Draws);
        Assert.Equal([line], rig.Lines);
    }

    [Fact]
    public async Task Order_for_another_slot_keeps_the_turn()
    {
        var rig = Acting(5, 0, slot: 1);
        rig.State.RetVal = FigtSelCommand.OrderDefend;
        rig.State.Tmp0001 = 2;
        Assert.Equal(FigtLabel.Select, await rig.Battle.DispatchAsync());
        Is(2, rig.State.Bt_PlayerOrder2);
        Is(42, rig.State.Tmp0002);
        Is(0, rig.State.RetVal);
        Assert.Equal([801, 821], rig.Lines);
    }

    [Theory]
    [InlineData(51, 8)]
    [InlineData(52, 9)]
    public async Task Auto_order_for_the_acting_unit_acts_now(int command, int redirected)
    {
        var rig = Acting(5, 0, slot: 1);
        rig.State.RetVal = (Sys3Int)command;
        rig.State.Tmp0001 = 1;
        Assert.Equal(FigtLabel.Dispatch, await rig.Battle.DispatchAsync());
        Is(redirected, rig.State.RetVal);
        Is(command - 50, rig.State.Bt_PlayerOrder1);
        Assert.Empty(rig.Lines);
    }

    [Fact]
    public async Task Manual_order_for_the_acting_unit_only_reports()
    {
        var rig = Acting(5, 0, slot: 1);
        rig.State.Bt_PlayerOrder1 = 1;
        rig.State.RetVal = FigtSelCommand.OrderManual;
        rig.State.Tmp0001 = 1;
        Assert.Equal(FigtLabel.Select, await rig.Battle.DispatchAsync());
        Is(0, rig.State.Bt_PlayerOrder1);
        Assert.Equal([801, 805], rig.Lines);
    }

    [Theory]
    [InlineData(8, 1, 1319)]
    [InlineData(9, 2, 1333)]
    public async Task Standing_order_for_self(int command, int order, int line)
    {
        var rig = Acting(5, 0, slot: 2);
        rig.State.RetVal = (Sys3Int)command;
        Assert.Equal(FigtLabel.AutoAction, await rig.Battle.DispatchAsync());
        Is(order, rig.State.Bt_CurOrder);
        Is(order, rig.State.Bt_PlayerOrder2);
        Assert.Equal([line], rig.Lines);
    }

    [Theory]
    [InlineData(0, 1, (int)Battle.AutoKind.Resolve, 3, 0, 0)]
    [InlineData(10, 2, (int)Battle.AutoKind.Resolve, 3, 0, 0)]
    [InlineData(1, 4, (int)Battle.AutoKind.Resolve, 3, 1, 99)]
    [InlineData(2, 3, (int)Battle.AutoKind.ResolveMagic, 1, 2, 99)]
    [InlineData(3, 1, (int)Battle.AutoKind.ResolveMagic, 1, 2, 99)]
    [InlineData(11, 1, (int)Battle.AutoKind.ResolveMagic, 1, 2, 99)]
    public void Auto_attack_targets_enemy_slot_1(int cls, int slot, int kindCode, int msgMode, int pa, int ea)
    {
        var kind = (Battle.AutoKind)kindCode;
        var rig = Acting(5, cls, slot);
        rig.State.Bt_CurOrder = 1;
        rig.State.Bt_EnemyActSlot = 3;
        Assert.Equal(kind, rig.Battle.AutoActionSetup());
        Is(1, rig.State.Bt_EnemyActSlot);
        Is(101, rig.State.Cmdr_Cur);
        Is(msgMode, rig.State.Bt_MsgMode);
        Is(pa, rig.State.Bt_PlayerAction);
        Is(ea, rig.State.Bt_EnemyAction);
    }

    [Theory]
    [InlineData(0, 3, 1)]   // melee in the back row
    [InlineData(4, 1, 1)]   // class 4
    [InlineData(1, 1, 2)]   // keep defending
    public void Auto_action_falls_back_to_defend(int cls, int slot, int order)
    {
        var rig = Acting(5, cls, slot);
        rig.State.Bt_CurOrder = (Sys3Int)order;
        Assert.Equal(Battle.AutoKind.None, rig.Battle.AutoActionSetup());
    }

    [Fact]
    public async Task Standing_order_dispatch_draws_nothing_before_the_auto_action()
    {
        var rig = Acting(5, 0, slot: 1);
        rig.State.Bt_CurOrder = 1;
        rig.State.RetVal = FigtSelCommand.AutoAttack;
        Assert.Equal(FigtLabel.AutoAction, await rig.Battle.DispatchAsync());
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public void Amoeba_rebuilds_record_51_and_keeps_the_capsule()
    {
        var rig = Acting(5, 0, slot: 2);
        var s = rig.State;
        s.Units[5].Item = 27;
        s.Kaiju_AmoebaAttacks = 7;
        s.Units[51].Troops = 900;
        s.Units[51].Class = 4;
        s.Units[51].Item = 12;
        s.Unit_StrBase = 2000;

        rig.Battle.SummonKaiju(amoeba: true);

        var k = s.Units[51];
        Is(51, s.Unit_Cur);
        Is(1161, k.CgFull);
        Is(1161, k.CgAlone);
        Is(1, k.Status);
        Is(0, k.Troops);
        Is(0, k.Class);
        Is(0, k.Item);
        Is(20, k.Atk);
        Is(10, k.Def);
        Is(7, k.Attacks);
        Is(100, s.Kaiju_AmoebaAttacks);
        Is(18, k.HP);
        Is(18, k.MaxHP);
        Is(0, k.Magic);
        Is(2, k.Partner);
        Is(27, s.Units[5].Item);   // QUIRK Q-19: the capsule is not consumed
        Assert.Equal("Amoeba", s.Strings[2000 + 50 * 3]);
        Assert.Equal("　", s.Strings[2000 + 50 * 3 + 1]);
    }

    [Fact]
    public async Task Jagi_jagi_summon_returns_to_the_player_turn()
    {
        var rig = Acting(5, 0);
        rig.State.Kaiju_JagiJagiAttacks = 3;
        rig.State.RetVal = FigtSelCommand.JagiJagi;
        Assert.Equal(FigtLabel.PlayerTurn, await rig.Battle.DispatchAsync());
        var k = rig.State.Units[51];
        Is(15, k.Atk);
        Is(13, k.Def);
        Is(8, k.Magic);
        Is(3, k.Attacks);
        Is(80, rig.State.Kaiju_JagiJagiAttacks);
        Is(1158, k.CgPartial);
    }

    [Fact]
    public async Task Fog_card_ends_the_battle_this_round()
    {
        var rig = Acting(5, 0);
        var s = rig.State;
        s.Bt_RoundsLeft = 3;
        s.Units[5].Item = 28;
        s.Items[28].Holder = 5;
        s.FogCard_Price = 1005;
        await rig.Battle.FogCardEffectAsync();
        Is(0, s.Bt_RoundsLeft);
        Is(0, s.Units[5].Item);
        Is(28, s.Item_Cur);
        Is(0, s.Items[28].Holder);
        Is(1105, s.FogCard_Price);
        Assert.Equal([1241, 1244], rig.Lines);
    }

    [Fact]
    public async Task Unit_info_restores_the_cursor_and_reads_the_order()
    {
        var rig = Acting(5, 0, slot: 1);
        rig.State.Bt_PlayerOrder2 = 2;
        rig.State.RetVal = FigtSelCommand.UnitInfo;
        rig.State.Tmp0001 = 2;
        Assert.Equal(FigtLabel.Select, await rig.Battle.DispatchAsync());
        Is(5, rig.State.Unit_Cur);
        Is(5, rig.State.Tmp0020);
        Is(2, rig.State.Tmp0002);
        Is(0, rig.State.RetVal);
        var info = Assert.Single(rig.View.Events.OfType<UnitInfoEvent>());
        Is(42, info.Unit);
        Is(2, info.Order);
    }

    [Fact]
    public async Task Enemy_info_restores_the_commander_cursor()
    {
        var rig = Acting(5, 0, slot: 1);
        rig.State.Cmdr_Cur = 101;
        rig.State.RetVal = FigtSelCommand.EnemyInfo;
        rig.State.Tmp0001 = 3;
        Assert.Equal(FigtLabel.Select, await rig.Battle.DispatchAsync());
        Is(101, rig.State.Cmdr_Cur);
        Is(103, Assert.Single(rig.View.Events.OfType<EnemyInfoEvent>()).Cmdr);
    }
}
