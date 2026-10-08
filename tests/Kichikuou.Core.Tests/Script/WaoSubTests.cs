using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Script;

/// <summary>
/// <c>WAO_SUB.ADV</c> state effects (<c>ScriptContext.Wao.cs</c>). FIGT_B shows and removes portraits
/// through ops 311/312/321/322/331/332; every removal runs <c>L_00b0f</c>, which leaves the
/// clamped camera in <c>Tmp0021/0022</c> — scratch outside the ignored <c>ui</c> group that the
/// port did not write. The key waits (ops 1, 11) do nothing while no window is open.
/// </summary>
public class WaoSubTests
{
    private static ScriptRig Wao(int op, int camX = 960, int camY = 400)
    {
        var rig = new ScriptRig();
        rig.State.Ui_WaoOp = (Sys3Int)op;
        rig.State.MapCamX = (Sys3Int)camX;
        rig.State.MapCamY = (Sys3Int)camY;
        return rig;
    }

    [Theory]
    [InlineData(312)]
    [InlineData(322)]
    [InlineData(332)]
    public async Task Removing_a_picture_leaves_the_clamped_camera_in_Tmp0021_0022(int op)
    {
        var rig = Wao(op, camX: 1800, camY: 50);
        rig.State.Ui_PortraitL = 1;
        rig.State.Ui_PortraitR = 6;
        rig.State.Ui_CenterCg = 900;
        rig.State.Ui_CenterOverlay2 = 901;

        await rig.Script.WaoSubAsync();

        Is(1600, rig.State.Tmp0021);
        Is(200, rig.State.Tmp0022);
        Is(op == 312 ? 0 : 1, rig.State.Ui_PortraitL);
        Is(op == 322 ? 0 : 6, rig.State.Ui_PortraitR);
        Is(op == 332 ? 0 : 900, rig.State.Ui_CenterCg);
        Is(op == 332 ? 0 : 901, rig.State.Ui_CenterOverlay2);
    }

    [Fact]
    public async Task Removing_runs_even_when_no_picture_is_shown()
    {
        var rig = Wao(312, camX: 100, camY: 700);

        await rig.Script.WaoSubAsync();

        Is(320, rig.State.Tmp0021);
        Is(600, rig.State.Tmp0022);
    }

    [Fact]
    public async Task Showing_a_portrait_removes_the_previous_one_first()
    {
        var rig = Wao(321, camX: 700, camY: 300);
        rig.State.Ui_PortraitR = 6;
        rig.State.CallArg = 1001;
        rig.State.Tmp0021 = 5;

        await rig.Script.WaoSubAsync();

        Is(1001, rig.State.Ui_PortraitR);
        Is(700, rig.State.Tmp0021);
        Is(300, rig.State.Tmp0022);
    }

    [Fact]
    public async Task Showing_a_portrait_on_an_empty_slot_leaves_the_scratch_alone()
    {
        var rig = Wao(311, camX: 700, camY: 300);
        rig.State.CallArg = 1;
        rig.State.Tmp0021 = 5;
        rig.State.Tmp0022 = 7;

        await rig.Script.WaoSubAsync();

        Is(1, rig.State.Ui_PortraitL);
        Is(5, rig.State.Tmp0021);
        Is(7, rig.State.Tmp0022);
    }

    [Fact]
    public async Task The_full_screen_window_clears_the_pictures()
    {
        var rig = Wao(131);
        rig.State.Ui_PortraitL = 1;
        rig.State.Ui_CenterCg = 900;
        rig.State.Ui_CenterOverlay4 = 3;

        await rig.Script.WaoSubAsync();

        Is(815, rig.State.Ui_WinFrameCg);
        Is(31, rig.State.Ui_WinType);
        Is(0, rig.State.Ui_PortraitL);
        Is(0, rig.State.Ui_CenterCg);
        Is(0, rig.State.Ui_CenterOverlay4);
    }

    [Fact]
    public async Task Op_11_waits_and_stores_the_key_only_with_a_window_open()
    {
        var closed = Wao(11);
        closed.State.RetVal = 3;
        closed.Input.Keys.Enqueue(13);

        await closed.Script.WaoSubAsync();

        Assert.Equal(0, closed.Input.KeyWaits);
        Is(3, closed.State.RetVal);

        var open = Wao(113);
        await open.Script.WaoSubAsync();
        Is(43, open.State.Ui_WinType);
        Is(809, open.State.Ui_WinFrameCg);
        open.State.Ui_WaoOp = 11;
        open.Input.Keys.Enqueue(13);

        await open.Script.WaoSubAsync();

        Assert.Equal(1, open.Input.KeyWaits);
        Is(13, open.State.RetVal);
    }

    [Fact]
    public async Task Op_1_waits_but_keeps_RetVal()
    {
        var rig = Wao(1);
        rig.State.Ui_WinType = 11;
        rig.State.RetVal = 3;
        rig.Input.Keys.Enqueue(13);

        await rig.Script.WaoSubAsync();

        Assert.Equal(1, rig.Input.KeyWaits);
        Is(3, rig.State.RetVal);
    }

    [Fact]
    public async Task Op_0_closes_an_open_window()
    {
        var rig = Wao(0);
        rig.State.Ui_WinType = 11;
        rig.State.Ui_WinFrameCg = 807;

        await rig.Script.WaoSubAsync();

        Is(0, rig.State.Ui_WinType);
        Is(0, rig.State.Ui_WinFrameCg);
    }

    [Theory]
    [InlineData(111, 807, 11)]
    [InlineData(112, 808, 12)]
    [InlineData(113, 809, 43)]
    [InlineData(121, 810, 21)]
    [InlineData(122, 811, 22)]
    [InlineData(141, 895, 41)]
    [InlineData(142, 896, 42)]
    [InlineData(143, 894, 43)]
    [InlineData(151, 897, 51)]
    [InlineData(152, 898, 52)]
    public async Task Opening_a_window_sets_its_frame_and_type(int op, int frame, int type)
    {
        var rig = Wao(op);

        await rig.Script.WaoSubAsync();

        Is(frame, rig.State.Ui_WinFrameCg);
        Is(type, rig.State.Ui_WinType);
    }

    [Fact]
    public async Task A_call_sets_the_op_and_shows_the_event_before_its_effects()
    {
        var rig = new ScriptRig();
        rig.State.CallArg = 1137;
        Sys3Int portraitAtEvent = 1;
        var view = new SnapshotView(() => portraitAtEvent = rig.State.Ui_PortraitL);
        var script = new ScriptContext(rig.State, rig.Random, view, rig.Input, rig.Story);

        await script.WaoAsync(311);

        Assert.Equal([new WindowEvent(311, 1137)], view.Events);
        Is(0, portraitAtEvent);
        Is(311, rig.State.Ui_WaoOp);
        Is(1137, rig.State.Ui_PortraitL);
    }

    private sealed class SnapshotView(Action snapshot) : IScriptView
    {
        public List<ScriptEvent> Events { get; } = [];

        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            snapshot();
            Events.Add(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
