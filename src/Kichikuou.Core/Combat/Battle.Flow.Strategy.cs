using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Combat;

// FIGT作.ADV: the pre-battle strategy roll (docs/battle/01-flow.md §4).
public sealed partial class Battle
{
    /// <summary>Player strategists with their own success line, and the line's text (<c>FIGT作.ADV:47-312</c>).</summary>
    private static readonly (int Unit, int Line)[] PlayerStrategyLines =
    [
        (2, 50), (5, 62), (6, 80), (10, 98), (12, 114), (13, 128), (14, 142), (15, 156),
        (16, 173), (20, 189), (22, 205),
    ];

    /// <summary>Player strategists after unit 28 (<c>FIGT作.ADV:247-312</c>).</summary>
    private static readonly (int Unit, int Line)[] PlayerStrategyLinesAfterCream =
    [
        (34, 250), (35, 268), (36, 284),
    ];

    /// <summary>Enemy strategists with their own line and window op (<c>FIGT作.ADV:389-454</c>).</summary>
    private static readonly (int Cmdr, int Op, int Line)[] EnemyStrategyLines =
    [
        (1, 151, 392), (4, 151, 406), (8, 151, 416), (22, 152, 430), (45, 151, 444),
    ];

    /// <summary>
    /// <c>FIGT作.ADV</c> (01 §4), side in <c>CallArg</c> (2 = enemy; anything else falls into the
    /// player side). Input <c>Tmp0021</c>/<c>Tmp0022</c> = the best player / enemy strategy
    /// values. Draws <c>RND(100)</c> [R4]; on success sets <c>Bt_StrategyResult</c> (1/2 or
    /// 11/12; great success when the margin left after the roll exceeds 50).
    /// </summary>
    internal async Task StrategyAsync()
    {
        if (State.CallArg == 2)
            await EnemyStrategyAsync();
        else
            await PlayerStrategyAsync();
    }

    /// <summary><c>L_00048</c>, <c>FIGT作.ADV:7-347</c>: the player's strategy.</summary>
    private async Task PlayerStrategyAsync()
    {
        State.Tmp0021 = State.Tmp0021 - State.Tmp0022;
        Rnd(100);
        if (State.Tmp0021 < State.RetVal)
            return;
        State.Unit_Cur = State.Bt_PlayerStrategist;
        FlowCopyString(1, UnitNameString(State.Unit_Cur));
        await View.ShowAsync(new StrategyScreenEvent(1, State.Unit_Cur));
        State.Tmp0021 = State.Tmp0021 - State.RetVal;
        await StrategyBoxAsync(143, 37);
        State.Bt_StrategyResult = 1;
        if (State.Tmp0021 > 50)
            State.Bt_StrategyResult = 2;
        await PlayerStrategistLineAsync();
        // L_00632 (:318-347)
        await FlowWindowAsync(143);
        await FlowTextAsync(StrategyFile, 321);
        if (State.Bt_StrategyResult == 1)
            await FlowTextAsync(StrategyFile, 327);
        if (State.Bt_StrategyResult == 2)
            await FlowTextAsync(StrategyFile, 337);
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
    }

    /// <summary><c>FIGT作.ADV:47-317</c>: the strategist's line, by <c>Unit_Cur</c>.</summary>
    private async Task PlayerStrategistLineAsync()
    {
        foreach (var (unit, line) in PlayerStrategyLines)
        {
            if (State.Unit_Cur == (Sys3Int)unit)
            {
                await StrategyBoxAsync(142, line);
                return;
            }
        }
        if (State.Unit_Cur == 28)
        {
            State.N_Cream_Tactics = State.N_Cream_Tactics + 1;
            if (State.N_Cream_Tactics > 10)
            {
                await StrategyBoxAsync(142, 219);
                return;
            }
            await StrategyBoxAsync(142, 236);
            return;
        }
        foreach (var (unit, line) in PlayerStrategyLinesAfterCream)
        {
            if (State.Unit_Cur == (Sys3Int)unit)
            {
                await StrategyBoxAsync(142, line);
                return;
            }
        }
        if (State.Unit_Cur == 37)
        {
            await StrategyBoxAsync(141, 298);
            return;
        }
        await StrategyBoxAsync(142, 315);
    }

    /// <summary><c>L_006b2</c>, <c>FIGT作.ADV:348-489</c>: the enemy's strategy.</summary>
    private async Task EnemyStrategyAsync()
    {
        State.Tmp0022 = State.Tmp0022 - State.Tmp0021;
        Rnd(100);
        if (State.Tmp0022 < State.RetVal)
            return;
        State.Tmp0022 = State.Tmp0022 - State.RetVal;
        State.Cmdr_Cur = State.Bt_EnemyStrategist;
        FlowCopyString(4, CmdrNameString(State.Cmdr_Cur));
        await View.ShowAsync(new StrategyScreenEvent(2, State.Cmdr_Cur));
        await StrategyBoxAsync(143, 378);
        State.Bt_StrategyResult = 11;
        if (State.Tmp0022 > 50)
            State.Bt_StrategyResult = 12;
        bool spoken = false;
        foreach (var (cmdr, op, line) in EnemyStrategyLines)
        {
            if (State.Cmdr_Cur == (Sys3Int)cmdr)
            {
                await StrategyBoxAsync((Sys3Int)op, line);
                spoken = true;
                break;
            }
        }
        if (!spoken)
            await StrategyBoxAsync(152, 457);
        // L_00919 (:460-489)
        await FlowWindowAsync(143);
        await FlowTextAsync(StrategyFile, 463);
        if (State.Bt_StrategyResult == 11)
            await FlowTextAsync(StrategyFile, 469);
        if (State.Bt_StrategyResult == 12)
            await FlowTextAsync(StrategyFile, 479);
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
    }

    /// <summary>A FIGT作 message: window op, text, <c>\L_00a4e</c> (wait, op 11), <c>\L_00a41</c> (close, op 10).</summary>
    private async Task StrategyBoxAsync(Sys3Int op, int line)
    {
        await FlowWindowAsync(op);
        await FlowTextAsync(StrategyFile, line);
        await FlowWaitKeyAsync();
        await FlowWindowAsync(10);
    }
}
