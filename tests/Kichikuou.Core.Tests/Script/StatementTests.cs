using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Script;

/// <summary>Variables by number (<c>GameState.ReadVariable</c>) and the straight-line statement runner.</summary>
public class StatementTests
{
    private static readonly ScriptRef At = new("TEST.ADV", 1);

    private static ConstExpr C(int v) => new((Sys3Int)v);

    private static VarExpr V(int n) => new(n);

    [Fact]
    public void A_bound_variable_goes_through_its_cursor()
    {
        var state = new GameState();
        state.Unit_Cur = 3;

        state.WriteVariable(74, 120); // Unit_Troops

        Is(120, state.Units[3].Troops);
        Is(0, state.Memory.Var(74));
        Is(120, state.ReadVariable(74));
        state.Unit_Cur = 4;
        Is(0, state.ReadVariable(74));
    }

    [Fact]
    public void A_plain_variable_is_its_slot()
    {
        var state = new GameState();

        state.WriteVariable(519, 7);

        Is(7, state.Gold);
        Is(7, state.ReadVariable(519));
    }

    [Fact]
    public async Task Assignments_evaluate_with_saturating_arithmetic()
    {
        var rig = new ScriptRig();
        rig.State.Gold = 5;

        await rig.Script.RunAsync(
        [
            new AssignStatement(At, 1, new BinaryExpr('-', V(519), C(9))),                                 // 5 - 9 = 0
            new AssignStatement(At, 2, new BinaryExpr('*', C(1000), C(1000))),                            // caps at 65535
            new AssignStatement(At, 3, new BinaryExpr('+', C(2), new BinaryExpr('*', C(3), C(4)))),       // 14
            new AssignStatement(At, 4, new BinaryExpr('/', C(7), C(0))),                                  // 0
        ]);

        Is(0, rig.State.Tmp0001);
        Is(65535, rig.State.Tmp0002);
        Is(14, rig.State.Tmp0003);
        Is(0, rig.State.Tmp0004);
    }

    [Fact]
    public async Task Strings_are_set_and_copied()
    {
        var rig = new ScriptRig();
        rig.State.Unit_StrBase = 20;
        rig.State.Unit_Cur = 2;

        await rig.Script.RunAsync(
        [
            new SetStringStatement(At, 1, "Sill"),
            // MM Unit_StrBase + (Unit_Cur - 1) * 3 + 0, 1
            new CopyStringStatement(At, new BinaryExpr('+', new BinaryExpr('+', V(198), new BinaryExpr('*', new BinaryExpr('-', V(154), C(1)), C(3))), C(0)), C(1)),
        ]);

        Assert.Equal("Sill", rig.State.Strings[1]);
        Assert.Equal("Sill", rig.State.Strings[23]);
    }

    [Fact]
    public async Task F3_reads_the_current_data_table()
    {
        var rig = new ScriptRig();
        rig.State.Prov_Cur = 2;

        await rig.Script.RunAsync(
        [
            new SetDataTableStatement(At, [(Sys3Int)10, (Sys3Int)11, (Sys3Int)20, (Sys3Int)21]),
            new ReadDataStatement(At, 142, new BinaryExpr('*', new BinaryExpr('-', V(158), C(1)), C(2))),
            new ReadDataStatement(At, 143, new BinaryExpr('+', new BinaryExpr('*', new BinaryExpr('-', V(158), C(1)), C(2)), C(1))),
        ]);

        Is(20, rig.State.Provs[2].MapX);
        Is(21, rig.State.Provs[2].MapY);
    }

    [Fact]
    public async Task A_call_of_M_ADV_runs_the_map_op_and_other_files_are_hooks()
    {
        var rig = new ScriptRig();

        await rig.Script.RunAsync(
        [
            new AssignStatement(At, 45, C(300)),     // MapTargetX
            new AssignStatement(At, 46, C(400)),     // MapTargetY
            new AssignStatement(At, 21, C(821)),     // Tmp0021: owner flag
            new AssignStatement(At, 516, C(4)),      // CallArg
            new CallFileStatement(At, "M.ADV"),
            new CallFileStatement(At, "Gｿｳﾙ.ADV"),
            new ReturnStatement(At),
            new AssignStatement(At, 519, C(99)),     // after the return: not run
        ]);

        Is(1, rig.State.Ui_MarkCount);
        Is(300, rig.State.Marks[0].X);
        Is(400, rig.State.Marks[0].Y);
        Is(10821, rig.State.Marks[0].Cg);
        Assert.Equal([new MapEvent(4)], rig.View.Events);
        Assert.Equal([("Gｿｳﾙ.ADV", 0)], rig.Story.Calls);
        Is(0, rig.State.Gold);
    }

    [Fact]
    public async Task Reading_past_the_data_table_fails_with_the_location()
    {
        var rig = new ScriptRig();

        var e = await Assert.ThrowsAsync<ScenarioFormatException>(() => rig.Script.RunAsync(
            [new SetDataTableStatement(At, [(Sys3Int)1]), new ReadDataStatement(At, 1, C(1))]));

        Assert.Contains("TEST.ADV:1", e.Message, StringComparison.Ordinal);
    }
}
