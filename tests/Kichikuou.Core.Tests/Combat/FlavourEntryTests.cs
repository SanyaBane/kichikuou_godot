using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.State;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// FlavourText entry and window procedures — FIGT_M.ADV:1-23, 10306-10353, WAO_SUB.ADV:453-519
/// (spec 02 §5.1). Unit 14 (RND(3)@234, lines 239/250/263) and commander 8 (RND(3)@3091, lines
/// 3096/3109/3126) mark which side spoke.
/// </summary>
public class FlavourEntryTests
{
    [Fact]
    public async Task Tmp0621_is_zeroed_on_entry()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 60, cmdr: 0);
        rig.State.Tmp0621 = 7;

        await rig.RunAsync();

        Assert.Equal(0, rig.State.Tmp0621.Value);
        Assert.Equal([2, 3], rig.Draws);
        Assert.Empty(rig.View.Events);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(3, 2)]
    public async Task Player_modes_speak_from_the_player_side_whatever_the_first_roll(int mode, int firstRoll)
    {
        var rig = new FlavourRig(firstRoll, 2, 1).Player(unit: 14, cmdr: 8, mode: mode);

        await rig.RunAsync();

        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([239], rig.Lines);
        Assert.Equal(mode, rig.State.Bt_MsgMode.Value);
    }

    [Theory]
    [InlineData(2, 1)]
    [InlineData(4, 1)]
    public async Task Enemy_modes_speak_from_the_enemy_side_whatever_the_first_roll(int mode, int firstRoll)
    {
        var rig = new FlavourRig(firstRoll, 2, 3).Enemy(cmdr: 8, unit: 14, mode: mode);

        await rig.RunAsync();

        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([3126], rig.Lines);
    }

    [Fact]
    public async Task Player_magic_answered_by_enemy_magic_becomes_mode_2_and_the_enemy_speaks()
    {
        var rig = new FlavourRig(1, 1, 2).Player(unit: 14, cmdr: 8, mode: 1);
        rig.State.Bt_EnemyAction = 2;

        await rig.RunAsync();

        Assert.Equal(2, rig.State.Bt_MsgMode.Value); // FIGT_M.ADV:3, persists after the call
        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([3109], rig.Lines);
    }

    [Fact]
    public async Task Player_magic_with_another_enemy_action_stays_mode_1()
    {
        var rig = new FlavourRig(1, 1, 3).Player(unit: 14, cmdr: 8, mode: 1);
        rig.State.Bt_EnemyAction = 0;

        await rig.RunAsync();

        Assert.Equal(1, rig.State.Bt_MsgMode.Value);
        Assert.Equal([263], rig.Lines);
    }

    [Theory] // Q11: a stale Bt_MsgMode outside 1..4 lets the roll at :5 pick the side.
    [InlineData(1, 239)]
    [InlineData(2, 3096)]
    [InlineData(3, 239)] // Q20: Y 4,2 returning 3 falls through into the player side (:24)
    public async Task Mode_outside_1_to_4_lets_the_first_roll_pick_the_side(int firstRoll, int line)
    {
        var rig = new FlavourRig(firstRoll, 1, 1).Player(unit: 14, cmdr: 8, mode: 0);

        await rig.RunAsync();

        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([line], rig.Lines);
        Assert.Equal(0, rig.State.Bt_MsgMode.Value);
    }

    [Fact]
    public async Task One_message_is_open_text_wait_close()
    {
        var rig = new FlavourRig(1, 1, 1).Player(unit: 14, cmdr: 0);

        await rig.RunAsync();

        Assert.Collection(rig.View.Events,
            e => WindowAssert.Op((int)FlavourWindow.Left, e),
            e => Assert.Equal(new MessageEvent(new ScriptRef("FIGT_M.ADV", 239)), e),
            e => WindowAssert.Op(11, e),
            e => WindowAssert.Op(10, e));
    }

    [Fact]
    public async Task Window_procedures_write_the_wao_state()
    {
        var rig = new FlavourRig(1, 1, 1).Player(unit: 14, cmdr: 0);

        await rig.RunAsync();

        // The window event comes before WAO_SUB L_00b78 runs: op 141, nothing set yet.
        Assert.Equal((141, 0, 0), rig.View.Snapshots[0]);
        // At the text: frame 895, type 41.
        Assert.Equal((141, 895, 41), rig.View.Snapshots[1]);
        // At IK 0 in L_00ce1: op 11, the window is open (type 41).
        Assert.Equal((11, 895, 41), rig.View.Snapshots[2]);
        // After L_00cb9: op 10, type and frame cleared.
        Assert.Equal(10, rig.State.Ui_WaoOp.Value);
        Assert.Equal(0, rig.State.Ui_WinType.Value);
        Assert.Equal(0, rig.State.Ui_WinFrameCg.Value);
    }

    [Fact]
    public async Task Alternate_left_window_uses_frame_896_and_type_42()
    {
        var rig = new FlavourRig(1, 1, 2).Player(unit: 14, cmdr: 0); // arm 2 opens L_0c4dd

        await rig.RunAsync();

        WindowAssert.Op((int)FlavourWindow.LeftAlt, rig.View.Events[0]);
        Assert.Equal((142, 896, 42), rig.View.Snapshots[1]);
        Assert.Equal((11, 896, 42), rig.View.Snapshots[2]);
        Assert.Equal([250], rig.Lines);
    }

    [Fact]
    public async Task Right_window_uses_frame_897_and_type_51()
    {
        var rig = new FlavourRig(1, 1, 1).Enemy(cmdr: 8, unit: 0);

        await rig.RunAsync();

        WindowAssert.Op((int)FlavourWindow.Right, rig.View.Events[0]);
        Assert.Equal((11, 897, 51), rig.View.Snapshots[2]);
    }

    [Fact]
    public async Task The_key_that_closes_a_message_is_left_in_RetVal()
    {
        var rig = new FlavourRig([1, 1, 1], keys: [16]).Player(unit: 14, cmdr: 0);

        await rig.RunAsync();

        Assert.Equal(16, rig.State.RetVal.Value); // IK 0 overwrites the Y 4 result (README O1)
        Assert.Equal(1, rig.State.Tmp0690.Value);
    }

    [Fact]
    public async Task No_story_scene_is_called()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 1, cmdr: 93);
        rig.State.F_Sieg_State = 2;

        await rig.RunAsync();

        Assert.Empty(rig.Story.Calls); // the Sieg scene is inline in FIGT_M (L_0c0fc)
    }
}

/// <summary>A battle wired for calling <c>FlavourTextAsync</c> directly.</summary>
internal sealed class FlavourRig
{
    public FlavourRig(params int[] rolls)
        : this(rolls, [])
    {
    }

    public FlavourRig(int[] rolls, int[] keys)
    {
        Random = new ScriptedRandom(rolls);
        View = new FlavourView(State);
        foreach (int key in keys)
            Input.Keys.Enqueue(key);
        Battle = new Battle(State, Random, View, Input, Story);
    }

    public GameState State { get; } = new();

    public ScriptedRandom Random { get; }

    public FlavourView View { get; }

    public ScriptedInput Input { get; } = new();

    public RecordingStory Story { get; } = new();

    public Battle Battle { get; }

    public int[] Draws => [.. Random.Draws];

    public int[] Lines => [.. View.Events.OfType<MessageEvent>().Select(e => e.Text.Line)];

    public Task RunAsync() => Battle.FlavourTextAsync();

    /// <summary>A player-initiated engagement: unit <paramref name="unit"/> attacks.</summary>
    public FlavourRig Player(int unit, int cmdr, int mode = 3)
    {
        State.Unit_Cur = (Sys3Int)unit;
        State.Cmdr_Cur = (Sys3Int)cmdr;
        State.Bt_MsgMode = (Sys3Int)mode;
        return this;
    }

    /// <summary>An enemy-initiated engagement: commander <paramref name="cmdr"/> attacks.</summary>
    public FlavourRig Enemy(int cmdr, int unit, int mode = 4) => Player(unit, cmdr, mode);
}

/// <summary>Records events and the window state at each one.</summary>
internal sealed class FlavourView(GameState state) : IScriptView
{
    public List<ScriptEvent> Events { get; } = [];

    /// <summary>(<c>Ui_WaoOp</c>, <c>Ui_WinFrameCg</c>, <c>Ui_WinType</c>) when each event was shown.</summary>
    public List<(int WaoOp, int FrameCg, int WinType)> Snapshots { get; } = [];

    public Task ShowAsync(ScriptEvent scriptEvent)
    {
        Events.Add(scriptEvent);
        Snapshots.Add((state.Ui_WaoOp.Value, state.Ui_WinFrameCg.Value, state.Ui_WinType.Value));
        return Task.CompletedTask;
    }
}

