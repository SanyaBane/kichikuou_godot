using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.State;

/// <summary>The typed view must address exactly the slots SET.ADV binds with DS.</summary>
public class GameStateLayoutTests
{
    [Fact]
    public void Pages_are_allocated_as_in_SET_ADV()
    {
        var memory = new GameState().Memory;
        Assert.Equal(20000, memory.PageSize(1));
        Assert.Equal(1800, memory.PageSize(2));
        Assert.Equal(7000, memory.PageSize(3));
        Assert.Equal(2100, memory.PageSize(4));
        Assert.Equal(300, memory.PageSize(10));
    }

    [Fact]
    public void Scalar_variables_map_to_their_numbers()
    {
        var state = new GameState();
        state.Week = 12;
        state.Week += 1;
        Assert.Equal((Sys3Int)13, state.Memory.Var(517));
    }

    [Fact]
    public void Unit_fields_follow_the_DS_order_not_the_variable_numbers()
    {
        var state = new GameState();
        var rance = state.Units[1];
        rance.Troops = 50;     // field 0
        rance.Recovery = 6;    // VAR0099, bound as field 7
        rance.DefDefMod = 3;   // VAR0081, bound as field 31

        Assert.Equal((Sys3Int)50, state.Memory.ReadPage(1, 0 + 1));
        Assert.Equal((Sys3Int)6, state.Memory.ReadPage(1, 52 * 7 + 1));
        Assert.Equal((Sys3Int)3, state.Memory.ReadPage(1, 52 * 31 + 1));
    }

    [Fact]
    public void Tables_follow_each_other_on_page_1()
    {
        var state = new GameState();
        state.Cmdrs[0].Troops = 1;
        state.Provs[0].Owner = 2;
        state.Items[0].Holder = 3;
        state.Dgns[0].MapX = 4;

        Assert.Equal((Sys3Int)1, state.Memory.ReadPage(1, 1664));
        Assert.Equal((Sys3Int)2, state.Memory.ReadPage(1, 6944));
        Assert.Equal((Sys3Int)3, state.Memory.ReadPage(1, 8444));
        Assert.Equal((Sys3Int)4, state.Memory.ReadPage(1, 8594));
    }

    [Fact]
    public void An_index_past_the_count_reads_the_next_field_like_the_script()
    {
        var state = new GameState();
        state.Units[0].MaxTroops = 77;
        Assert.Equal((Sys3Int)77, state.Units[52].Troops);
    }

    [Fact]
    public void Outside_the_page_reads_zero_and_writes_are_ignored()
    {
        var state = new GameState();
        state.Memory.WritePage(1, 20000, 5);
        Assert.Equal(Sys3Int.Zero, state.Memory.ReadPage(1, 20000));
        Assert.Equal(Sys3Int.Zero, state.Memory.ReadPage(7, 0));
    }

    [Fact]
    public void History_previous_cursor_addresses_the_same_slots()
    {
        var state = new GameState();
        state.History[5].Funds = 9;
        state.Hist_PrevCur = 5;
        // Hist_PrevFunds (VAR0172) is bound to the same offset as Hist_Funds with Hist_PrevCur.
        Assert.Equal((Sys3Int)9, state.History[state.Hist_PrevCur].Funds);
    }
}
