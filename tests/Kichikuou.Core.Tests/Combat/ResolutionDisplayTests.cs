using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.State;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// State effects of the battlefield redraw <c>L_03058</c> and highlight <c>L_0320f</c>
/// (<c>FIGT.ADV:1987-2136</c>, docs/battle/01-flow.md §9.4) and slot compaction
/// <c>L_04762</c> / <c>L_04864</c> (<c>2858-2934</c>, 02 §7.5).
/// </summary>
public class ResolutionDisplayTests
{
    [Fact]
    public async Task Redraw_leaves_the_cursors_on_the_last_occupied_slots()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Bt_Kind = 1;
        s.Prov_Cur = 5;
        s.Provs[5].BgCg = 1003;
        s.MapCamX = 500;
        s.MapCamY = 100;
        s.Unit_StrBase = 20;
        s.Cmdr_StrBase = 200;
        s.Bt_PlayerSlot1 = 3;
        s.Bt_PlayerSlot2 = 7;
        s.Bt_EnemySlot1 = 10;
        s.Bt_EnemySlot4 = 12;
        s.Strings[38] = "unit 7";   // 20 + 6 * 3
        s.Strings[39] = "troops 7";
        s.Strings[233] = "cmdr 12"; // 200 + 11 * 3
        s.Strings[234] = "troops 12";
        fx.Unit(3).OffAtkMod = 2;
        fx.Unit(3).OffDefMod = 13;
        fx.Unit(7).OffAtkMod = 4;
        fx.Unit(7).OffDefMod = 14;

        await fx.Battle.RedrawAsync();

        Assert.Equal(7, (int)s.Unit_Cur);
        Assert.Equal(12, (int)s.Cmdr_Cur);
        Assert.Equal(8, (int)s.CallArg);
        Assert.Equal(28, (int)s.RetVal);
        // panel 8 of L_0512e
        Assert.Equal(448, (int)s.Tmp0021); // Q-28
        Assert.Equal(308, (int)s.Tmp0022);
        Assert.Equal(182, (int)s.Tmp0023);
        Assert.Equal(84, (int)s.Tmp0024);
        Assert.Equal("unit 7", s.Strings[1]);
        Assert.Equal("troops 7", s.Strings[2]);
        Assert.Equal("cmdr 12", s.Strings[4]);
        Assert.Equal("troops 12", s.Strings[5]);
        // Q-20: every drawn player unit gets its Bt_Kind modifiers
        Assert.Equal(2, (int)fx.Unit(3).CurAtkMod);
        Assert.Equal(13, (int)fx.Unit(3).CurDefMod);
        Assert.Equal(4, (int)fx.Unit(7).CurAtkMod);
        Assert.Equal(14, (int)fx.Unit(7).CurDefMod);
        Assert.Equal(
            [
                new BattlefieldBackgroundEvent(1003),
                new SlotPanelEvent(1, 3), new SlotPanelEvent(2, 7), new SlotPanelEvent(5, 10), new SlotPanelEvent(8, 12),
            ],
            fx.View.Events);
        Assert.Empty(fx.Random.Draws);
    }

    [Fact]
    public async Task Redraw_background_sets_the_camera_corner_capped()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.MapCamX = 2000;
        s.MapCamY = 50;
        s.CallArg = 77;
        await fx.Battle.RedrawAsync();
        Assert.Equal(0, (int)s.CallArg);
        Assert.Equal(1280, (int)s.Tmp0021); // 1680 → 1280
        Assert.Equal(0, (int)s.Tmp0022);    // 50 - 200 floors at 0
    }

    [Fact]
    public async Task Redraw_in_a_dungeon_skips_the_camera_writes()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Bt_Kind = 3;
        s.CallArg = 77;
        s.Tmp0021 = 55;
        s.Unit_Cur = 9;
        await fx.Battle.RedrawAsync();
        Assert.Equal(77, (int)s.CallArg);
        Assert.Equal(55, (int)s.Tmp0021);
        Assert.Equal(9, (int)s.Unit_Cur);
        Assert.Equal([new BattlefieldBackgroundEvent(0)], fx.View.Events);
    }

    [Fact]
    public async Task Highlight_uses_the_current_commander_not_the_slot()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Cmdr_StrBase = 200;
        s.Cmdr_Cur = 4;
        s.Bt_EnemySlot2 = 9;
        s.Strings[209] = "cmdr 4";
        s.Strings[210] = "troops 4";
        s.CallArg = 6;
        await fx.Battle.HighlightAsync();
        Assert.Equal(4, (int)s.Cmdr_Cur);
        Assert.Equal(20, (int)s.RetVal);
        Assert.Equal([448, 108, 182, 84], new[] { s.Tmp0021, s.Tmp0022, s.Tmp0023, s.Tmp0024 }.Select(v => (int)v));
        Assert.Equal("cmdr 4", s.Strings[4]);
        Assert.Equal("troops 4", s.Strings[5]);
        Assert.Equal([new SlotPanelEvent(6, 4), new PanelHighlightEvent(6)], fx.View.Events);
    }

    [Fact]
    public async Task Highlight_of_an_empty_slot_writes_nothing()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.CallArg = 6;
        s.RetVal = 99;
        s.Tmp0021 = 55;
        await fx.Battle.HighlightAsync();
        Assert.Equal(99, (int)s.RetVal);
        Assert.Equal(55, (int)s.Tmp0021);
        Assert.Equal([new PanelHighlightEvent(6)], fx.View.Events);
    }

    [Fact]
    public async Task Highlight_of_a_player_panel_copies_the_modifiers_of_Unit_Cur()
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Bt_Kind = 2;
        s.Unit_Cur = 5;
        s.Bt_PlayerSlot2 = 8;
        fx.Unit(5).DefAtkMod = 3;
        fx.Unit(5).DefDefMod = 11;
        s.CallArg = 2;
        await fx.Battle.HighlightAsync();
        Assert.Equal(3, (int)fx.Unit(5).CurAtkMod);
        Assert.Equal(11, (int)fx.Unit(5).CurDefMod);
        Assert.Equal(4, (int)s.RetVal);
        Assert.Equal([12, 108, 182, 84], new[] { s.Tmp0021, s.Tmp0022, s.Tmp0023, s.Tmp0024 }.Select(v => (int)v));
    }

    // ---- compaction ----

    private static void FillPlayerSlots(GameState s)
    {
        s.Bt_PlayerSlot1 = 11; s.Bt_PlayerSlot2 = 12; s.Bt_PlayerSlot3 = 13; s.Bt_PlayerSlot4 = 14;
        s.Bt_PSlot1State = 1; s.Bt_PSlot2State = 2; s.Bt_PSlot3State = 3; s.Bt_PSlot4State = 4;
        s.Bt_PlayerOrder1 = 21; s.Bt_PlayerOrder2 = 22; s.Bt_PlayerOrder3 = 23; s.Bt_PlayerOrder4 = 24;
        s.Bt_PSlot1Focus = 31; s.Bt_PSlot2Focus = 32; s.Bt_PSlot3Focus = 33; s.Bt_PSlot4Focus = 34;
    }

    private static int[] PlayerSlots(GameState s) =>
    [
        (int)s.Bt_PlayerSlot1, (int)s.Bt_PlayerSlot2, (int)s.Bt_PlayerSlot3, (int)s.Bt_PlayerSlot4,
        (int)s.Bt_PSlot1State, (int)s.Bt_PSlot2State, (int)s.Bt_PSlot3State, (int)s.Bt_PSlot4State,
        (int)s.Bt_PlayerOrder1, (int)s.Bt_PlayerOrder2, (int)s.Bt_PlayerOrder3, (int)s.Bt_PlayerOrder4,
        (int)s.Bt_PSlot1Focus, (int)s.Bt_PSlot2Focus, (int)s.Bt_PSlot3Focus, (int)s.Bt_PSlot4Focus,
    ];

    [Theory]
    // Q15: Bt_PlayerOrder4 is cleared only for slot 4
    [InlineData(1, new[] { 12, 13, 14, 0, 2, 3, 4, 0, 22, 23, 24, 24, 32, 33, 34, 0 })]
    [InlineData(2, new[] { 11, 13, 14, 0, 1, 3, 4, 0, 21, 23, 24, 24, 31, 33, 34, 0 })]
    [InlineData(3, new[] { 11, 12, 14, 0, 1, 2, 4, 0, 21, 22, 24, 24, 31, 32, 34, 0 })]
    [InlineData(4, new[] { 11, 12, 13, 0, 1, 2, 3, 0, 21, 22, 23, 0, 31, 32, 33, 0 })]
    [InlineData(0, new[] { 11, 12, 13, 14, 1, 2, 3, 4, 21, 22, 23, 24, 31, 32, 33, 34 })]
    public void CompactPlayer_shifts_the_later_slots_up(int slot, int[] expected)
    {
        var fx = ResolutionFixture.Create();
        FillPlayerSlots(fx.State);
        fx.State.CallArg = (Sys3Int)slot;
        fx.Battle.CompactPlayer();
        Assert.Equal(expected, PlayerSlots(fx.State));
    }

    [Theory]
    [InlineData(1, new[] { 42, 43, 44, 0, 20, 0, 1, 0 })]
    [InlineData(2, new[] { 41, 43, 44, 0, 1, 0, 1, 0 })]
    [InlineData(3, new[] { 41, 42, 44, 0, 1, 20, 1, 0 })]
    [InlineData(4, new[] { 41, 42, 43, 0, 1, 20, 0, 0 })]
    [InlineData(5, new[] { 41, 42, 43, 44, 1, 20, 0, 1 })]
    public void CompactEnemy_shifts_the_later_slots_up(int slot, int[] expected)
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Bt_EnemySlot1 = 41; s.Bt_EnemySlot2 = 42; s.Bt_EnemySlot3 = 43; s.Bt_EnemySlot4 = 44;
        s.Bt_ESlot1State = 1; s.Bt_ESlot2State = 20; s.Bt_ESlot3State = 0; s.Bt_ESlot4State = 1;
        s.CallArg = (Sys3Int)slot;
        fx.Battle.CompactEnemy();
        Assert.Equal(
            expected,
            new[]
            {
                s.Bt_EnemySlot1, s.Bt_EnemySlot2, s.Bt_EnemySlot3, s.Bt_EnemySlot4,
                s.Bt_ESlot1State, s.Bt_ESlot2State, s.Bt_ESlot3State, s.Bt_ESlot4State,
            }.Select(v => (int)v));
    }
}
