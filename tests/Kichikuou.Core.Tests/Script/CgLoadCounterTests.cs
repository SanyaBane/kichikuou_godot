using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Script;

/// <summary>
/// The engine's CG load counter (owner's decision 21, STR-43): once <c>ZG Glob_CgSeen</c> binds it
/// (<c>SET.ADV:297</c>), every CG the script loads adds 1 to its record of <c>Glob_CgSeen</c> on
/// page 4 — through <c>G</c> (<see cref="ScriptContext.EngineAsync"/>) and through the helpers that
/// draw by their own events: the window pictures of <c>WAO_SUB</c> and the map. The battle's and the
/// pickers' loads are in their own tests (<c>EndToEndBattleTests</c>, <c>FlowStrategyTests</c>,
/// <c>PickerScreenTests</c>).
/// </summary>
public class CgLoadCounterTests
{
    private const int GlobCgSeen = 196;

    private static ScriptRig Bound()
    {
        var rig = new ScriptRig();
        rig.Script.BindCgLoadCounter(GlobCgSeen);
        return rig;
    }

    private static int Seen(ScriptRig rig, int cg) => rig.State.CgSeen[cg].CgSeen.Value;

    private static Task G(ScriptRig rig, params int[] args) =>
        rig.Script.EngineAsync("G", args.Select(a => (Sys3Int)a).ToArray());

    private static Task Wao(ScriptRig rig, int op, int callArg = 0)
    {
        rig.State.CallArg = (Sys3Int)callArg;
        return rig.Script.WaoAsync((Sys3Int)op);
    }

    [Fact]
    public async Task Nothing_counts_before_ZG_binds_the_counter()
    {
        var rig = new ScriptRig();

        await G(rig, 101, 40);

        Assert.Null(rig.Script.CgLoadCounter);
        Assert.Equal(0, Seen(rig, 101));
        var shown = Assert.IsType<EngineEvent>(Assert.Single(rig.View.Events));
        Assert.Equal("G", shown.Command);
        Assert.Equal([101, 40], shown.Args.Select(a => (int)a.Value));
    }

    [Fact]
    public async Task G_adds_one_to_the_CGs_record_on_page_4_and_G_0_loads_nothing()
    {
        var rig = Bound();

        await G(rig, 101, 40);
        await G(rig, 101);
        await G(rig, 0);

        Assert.Equal((4, 80), rig.Script.CgLoadCounter);
        Assert.Equal(2, Seen(rig, 101));
        Is(2, rig.State.Memory.ReadPage(4, 80 + 101));
        Is(0, rig.State.Memory.ReadPage(4, 80));
        Assert.Equal(3, rig.View.Events.OfType<EngineEvent>().Count());
    }

    /// <summary>
    /// <c>ZG</c> keeps the address the variable resolves to then, through its binding at the cursor
    /// (xsystem35 <c>getCaliVariable</c>); <c>SET.ADV</c> binds it with <c>Glob_CgCur</c> at 0.
    /// </summary>
    [Fact]
    public async Task ZG_binds_where_the_cursor_points_then()
    {
        var rig = new ScriptRig();
        rig.State.Glob_CgCur = 5;
        rig.Script.BindCgLoadCounter(GlobCgSeen);
        rig.State.Glob_CgCur = 0;

        await G(rig, 101);

        Assert.Equal((4, 85), rig.Script.CgLoadCounter);
        Is(1, rig.State.Memory.ReadPage(4, 85 + 101));
        Assert.Equal(0, Seen(rig, 101));
    }

    [Fact]
    public async Task The_count_wraps_at_16_bits_and_a_CG_past_the_page_writes_nothing()
    {
        var rig = Bound();
        rig.State.CgSeen[101].CgSeen = 65535;

        await G(rig, 101);
        await G(rig, 3000);

        Assert.Equal(0, Seen(rig, 101));
        Assert.Equal(2101, rig.State.Memory.PageSize(4));
    }

    /// <summary>
    /// A framed window loads its frame when it opens (<c>WAO_SUB.ADV:173</c>) and again after every
    /// key wait and clear (<c>L_0072b</c>, <c>:305</c>; op 11 <c>:518</c>); with no window open the
    /// waits do nothing.
    /// </summary>
    [Fact]
    public async Task A_window_loads_its_frame_when_it_opens_and_after_each_key()
    {
        var rig = Bound();

        await Wao(rig, 111);
        Assert.Equal(1, Seen(rig, 807));
        await Wao(rig, 1);
        await Wao(rig, 5);
        await Wao(rig, 11);
        await Wao(rig, 2);
        Assert.Equal(5, Seen(rig, 807));
        await Wao(rig, 0);
        await Wao(rig, 1);
        await Wao(rig, 2);

        Assert.Equal(5, Seen(rig, 807));
        Assert.Equal(3, rig.Input.KeyWaits);
    }

    /// <summary>The full-screen window's frame (<c>:219</c>) is drawn twice by a clear (<c>:302-305</c>), once by op 11.</summary>
    [Fact]
    public async Task The_full_screen_window_draws_its_frame_twice_after_a_key()
    {
        var rig = Bound();

        await Wao(rig, 131);
        Assert.Equal(1, Seen(rig, 815));
        await Wao(rig, 1);
        Assert.Equal(3, Seen(rig, 815));
        await Wao(rig, 11);

        Assert.Equal(4, Seen(rig, 815));
    }

    /// <summary>
    /// The pictures (<c>:318</c>, <c>:357</c>, <c>:396</c>, <c>:408-416</c>) and a portrait's removal,
    /// which draws the centre picture again with its overlays (<c>:329-347</c>, <c>:368-386</c>); the
    /// centre's own removal loads nothing.
    /// </summary>
    [Fact]
    public async Task Pictures_count_and_a_portraits_removal_draws_the_centre_again()
    {
        var rig = Bound();
        rig.State.Tmp0001 = 121;

        await Wao(rig, 333, 120);                   // centre 120, overlay 121 (Tmp0002-0004 are 0)
        await Wao(rig, 311, 1400);                  // no portrait yet: nothing removed
        Assert.Equal((1, 1, 1), (Seen(rig, 120), Seen(rig, 121), Seen(rig, 1400)));
        await Wao(rig, 311, 1401);                  // replaces 1400: the centre again
        await Wao(rig, 322);                        // no right portrait, removed all the same
        Assert.Equal((3, 3, 1), (Seen(rig, 120), Seen(rig, 121), Seen(rig, 1401)));
        await Wao(rig, 332);
        await Wao(rig, 312);                        // no centre now
        await Wao(rig, 321, 1402);
        await Wao(rig, 331, 122);

        Assert.Equal((3, 3, 1, 1), (Seen(rig, 120), Seen(rig, 121), Seen(rig, 1402), Seen(rig, 122)));
        Is(0, rig.State.Memory.ReadPage(4, 80));    // the overlays at 0 load nothing
    }

    /// <summary>
    /// <c>WAO_MAP</c> op 3 loads the six tiles and every marker, a flag above 10000 as its CG minus 10000
    /// (<c>WAO_MAP.ADV:86-112</c>); <c>M</c> op 0 loads the top-left tile (<c>M.ADV:11-12</c>, <c>PC 6</c>:
    /// loaded, not drawn) and op 13 the blinking mark (<c>:227-228</c>).
    /// </summary>
    [Fact]
    public async Task The_map_counts_its_tiles_its_markers_and_the_target_mark()
    {
        var rig = Bound();
        var s = rig.State;
        s.Ui_MapCgTopL = 1001;
        s.Ui_MapCgTopM = 1002;
        s.Ui_MapCgTopR = 1003;
        s.Ui_MapCgBotL = 1004;
        s.Ui_MapCgBotM = 1005;
        s.Ui_MapCgBotR = 1006;
        s.Ui_MarkCount = 2;
        s.Marks[0].Cg = 10007;
        s.Marks[1].Cg = 30;

        await rig.Script.WaoMapCallAsync(3);
        Assert.Equal([1, 1, 1, 1, 1, 1, 1, 1], new[] { 1001, 1002, 1003, 1004, 1005, 1006, 7, 30 }.Select(cg => Seen(rig, cg)));
        await rig.Script.MapCallAsync(0);
        await rig.Script.MapCallAsync(13);

        Assert.Equal(2, Seen(rig, 1001));
        Assert.Equal(1, Seen(rig, 1002));
        Assert.Equal(1, Seen(rig, 814));
    }
}
