using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using static Kichikuou.Original.Tests.Combat.Sys3Assert;

namespace Kichikuou.Original.Tests.Script;

/// <summary>
/// <c>M.ADV</c> / <c>WAO_MAP.ADV</c> state effects (<c>OriginalScriptContext.Map.cs</c>) and the <c>Y 3</c>
/// pauses that leave the key in <c>RetVal</c>. Both were found by the first golden capture
/// (a FIGT_A victory at Golak): the original returned from FIGT_A with M.ADV op 13's scratch
/// values and the last pause's key, which the port did not produce.
/// </summary>
public class MapTests
{
    private static ScriptRig Map(int op, int camX, int camY, int targetX, int targetY)
    {
        var rig = new ScriptRig();
        rig.State.CallArg = (Sys3Int)op;
        rig.State.MapCamX = (Sys3Int)camX;
        rig.State.MapCamY = (Sys3Int)camY;
        rig.State.MapTargetX = (Sys3Int)targetX;
        rig.State.MapTargetY = (Sys3Int)targetY;
        return rig;
    }

    [Fact]
    public async Task Op_13_blinks_the_target_and_leaves_the_last_pause_key()
    {
        // The values of the golden capture: camera and target on Golak (1297, 206).
        var rig = Map(13, 1297, 206, 1297, 206);
        rig.State.RetVal = 1;
        for (int i = 0; i < 7; i++)
            rig.Input.PauseKeys.Enqueue(0);
        rig.Input.PauseKeys.Enqueue(16);

        await rig.Script.MapAsync();

        Assert.Equal(Enumerable.Repeat(8, 8), rig.Input.Pauses);
        Is(1297, rig.State.Tmp0021);
        Is(206, rig.State.Tmp0022);
        Is(0, rig.State.Tmp0023);
        Is(206, rig.State.Tmp0024);
        Is(304, rig.State.Tmp0025);
        Is(184, rig.State.Tmp0026);
        Is(16, rig.State.RetVal);
    }

    [Fact]
    public async Task Op_10_also_plays_a_sound_with_RetVal_109()
    {
        var rig = Map(10, 640, 400, 700, 300);

        await rig.Script.MapAsync();

        var sound = Assert.Single(rig.View.Events.OfType<SoundEvent>());
        Is(0, sound.Mode);
        Is(109, sound.Sound);
        Is(0, rig.State.CallArg);
        Is(320 + 60 - 16, rig.State.Tmp0025);
        Is(200 + 0 - 100 - 16, rig.State.Tmp0026);   // (300 - 400) saturates to 0, minus (400 - 300)
        Is(0, rig.State.RetVal);                     // the last pause ran out
    }

    [Fact]
    public async Task Op_1_scrolls_in_steps_and_ends_on_the_unclamped_target()
    {
        // Target 100 is clamped to 320 for the scroll: distance 320, step 320 / 15 = 21,
        // fifteen steps and a snap; then camera and target become the original 100.
        var rig = Map(1, 640, 200, 100, 200);

        await rig.Script.MapAsync();

        Assert.Equal(Enumerable.Repeat(5, 16), rig.Input.Pauses);
        Is(100, rig.State.MapCamX);
        Is(100, rig.State.MapTargetX);
        Is(200, rig.State.MapCamY);
        Is(320, rig.State.Tmp0029);
        Is(0, rig.State.Tmp0030);
        Is(21, rig.State.Tmp0023);
        Is(1, rig.State.Tmp0024);
        Is(0, rig.State.Tmp0021);                    // corner of the last view: 320 - 320
    }

    [Fact]
    public void The_scroll_s_cameras_are_its_steps_and_a_snap()
    {
        // The case above: fifteen steps of 21 from 640, then 325 snaps to 320.
        var cameras = OriginalScriptContext.MapScrollCameras((Sys3Int)640, (Sys3Int)200, (Sys3Int)100, (Sys3Int)200);
        Assert.Equal([.. Enumerable.Range(1, 15).Select(k => 640 - 21 * k), 320], cameras.Select(c => (int)c.X.Value));
        Assert.All(cameras, c => Is(200, c.Y));
        Assert.Empty(OriginalScriptContext.MapScrollCameras((Sys3Int)100, (Sys3Int)150, (Sys3Int)320, (Sys3Int)200));  // held: no way
        // A short way down goes a pixel a step: 15 is not above 19.
        Assert.Equal(15, OriginalScriptContext.MapScrollCameras((Sys3Int)800, (Sys3Int)300, (Sys3Int)800, (Sys3Int)315).Count);
    }

    /// <summary>
    /// Op 1 run by the port on random cameras and targets (some outside the scroll's range, some short ways, some none): at
    /// each step's pause the camera is the one <see cref="OriginalScriptContext.MapScrollCameras"/> lists for it, which the
    /// screen's scroll drawn every frame follows (<c>Original.Content/Strategy/MapScrollGlide</c>).
    /// </summary>
    [Fact]
    public async Task Op_1_steps_through_the_cameras_MapScrollCameras_lists()
    {
        var rng = new Random(11);
        for (int i = 0; i < 400; i++)
        {
            int Near(int at, int max) => Math.Clamp(at + rng.Next(-25, 26), 0, max);
            int camX = rng.Next(0, 2001), camY = rng.Next(0, 901);
            int targetX = rng.Next(3) == 0 ? Near(camX, 2000) : rng.Next(0, 2001);
            int targetY = rng.Next(3) == 0 ? Near(camY, 900) : rng.Next(0, 901);
            var rig = Map(1, camX, camY, targetX, targetY);
            var view = new CamerasAtPauses(rig.State);
            var script = new OriginalScriptContext(rig.State, rig.Random, view, rig.Input, rig.Story);
            var expected = OriginalScriptContext.MapScrollCameras((Sys3Int)camX, (Sys3Int)camY, (Sys3Int)targetX, (Sys3Int)targetY);

            await script.MapAsync();

            Assert.Equal(expected, view.Cameras);
            Is(targetX, rig.State.MapCamX);
        }
    }

    private sealed class CamerasAtPauses(GameState state) : IScriptView
    {
        public List<(Sys3Int X, Sys3Int Y)> Cameras { get; } = [];

        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            if (scriptEvent is PauseEvent)
                Cameras.Add((state.MapCamX, state.MapCamY));
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Op_1_without_a_distance_does_not_pause()
    {
        var rig = Map(1, 800, 300, 800, 300);

        await rig.Script.MapAsync();

        Assert.Empty(rig.Input.Pauses);
        Is(800, rig.State.MapCamX);
    }

    [Fact]
    public async Task Op_0_sets_the_view_corner_and_clears_the_pictures()
    {
        var rig = Map(0, 1700, 700, 0, 0);
        rig.State.Ui_PortraitL = 5;
        rig.State.Ui_CenterCg = 7;

        await rig.Script.MapAsync();

        Is(1280, rig.State.Tmp0021);                 // 1700 - 320 capped
        Is(400, rig.State.Tmp0022);                  // 700 - 200 capped
        Is(0, rig.State.Ui_PortraitL);
        Is(0, rig.State.Ui_CenterCg);
    }

    [Fact]
    public async Task Op_4_replaces_the_marker_at_the_target_or_appends_one()
    {
        var rig = Map(4, 0, 0, 50, 60);
        rig.State.Ui_MarkCount = 2;
        rig.State.Marks[0].X = 10;
        rig.State.Marks[0].Y = 20;
        rig.State.Marks[1].X = 50;
        rig.State.Marks[1].Y = 60;
        rig.State.Marks[1].Cg = 10823;
        rig.State.Tmp0021 = 820;

        await rig.Script.MapAsync();

        Is(2, rig.State.Ui_MarkCount);
        Is(10820, rig.State.Marks[1].Cg);
        Is(0, rig.State.RetVal);

        rig.State.CallArg = 4;
        rig.State.MapTargetX = 70;
        rig.State.Tmp0021 = 821;
        await rig.Script.MapAsync();

        Is(3, rig.State.Ui_MarkCount);
        Is(70, rig.State.Marks[2].X);
        Is(10821, rig.State.Marks[2].Cg);
    }

    [Fact]
    public async Task WAO_MAP_op_5_moves_the_markers_after_the_one_taken_out_down_and_keeps_the_count()
    {
        // Shangri-La's end (Iｼｬﾝｸﾞﾘﾗ.ADV:1075-1079): its entrance (CG 843 at (1048, 326)) taken out.
        var rig = new ScriptRig();
        rig.State.Ui_MarkCount = 3;
        rig.State.Marks[0].X = 10;
        rig.State.Marks[0].Cg = 10820;
        rig.State.Marks[1].X = 1048;
        rig.State.Marks[1].Y = 326;
        rig.State.Marks[1].Cg = 843;
        rig.State.Marks[2].X = 30;
        rig.State.Marks[2].Y = 40;
        rig.State.Marks[2].Cg = 10822;
        rig.State.Marks[99].Cg = 7;                  // NB's last source: copied down, kept
        rig.State.Tmp0021 = 1048;
        rig.State.Tmp0022 = 326;

        await rig.Script.WaoMapCallAsync(5);

        Is(0, rig.State.RetVal);
        Is(1, rig.State.Ui_MarkCur);
        Is(2, rig.State.Ui_MarkNextCur);
        Is(3, rig.State.Ui_MarkCount);               // QUIRK: STR-59
        Is(10820, rig.State.Marks[0].Cg);
        Is(30, rig.State.Marks[1].X);
        Is(40, rig.State.Marks[1].Y);
        Is(10822, rig.State.Marks[1].Cg);
        Is(0, rig.State.Marks[2].X);                 // what lay past the list
        Is(0, rig.State.Marks[2].Cg);
        Is(7, rig.State.Marks[98].Cg);
        Is(7, rig.State.Marks[99].Cg);
        Assert.Equal([new WaoMapEvent(5)], rig.View.Events);
    }

    [Fact]
    public async Task WAO_MAP_op_5_leaves_the_list_alone_without_a_marker_there()
    {
        var rig = new ScriptRig();
        rig.State.Ui_MarkCount = 1;
        rig.State.Marks[0].X = 1129;
        rig.State.Marks[0].Y = 400;
        rig.State.Marks[0].Cg = 842;
        rig.State.Tmp0021 = 1129;
        rig.State.Tmp0022 = 401;

        await rig.Script.WaoMapCallAsync(5);

        Is(1, rig.State.RetVal);
        Is(1, rig.State.Ui_MarkCur);
        Is(0, rig.State.Ui_MarkNextCur);
        Is(1, rig.State.Ui_MarkCount);
        Is(1129, rig.State.Marks[0].X);
        Is(842, rig.State.Marks[0].Cg);
    }

    [Fact]
    public async Task Op_3_walks_every_marker_and_lifts_the_high_flags()
    {
        var rig = Map(3, 0, 0, 0, 0);
        rig.State.Ui_MarkCount = 2;
        rig.State.Marks[1].X = 30;
        rig.State.Marks[1].Y = 40;
        rig.State.Marks[1].Cg = 10822;

        await rig.Script.MapAsync();

        Is(2, rig.State.Ui_MarkCur);
        Is(1310, rig.State.Tmp0021);
        Is(22, rig.State.Tmp0022);
        Is(822, rig.State.Tmp0023);
    }
}
