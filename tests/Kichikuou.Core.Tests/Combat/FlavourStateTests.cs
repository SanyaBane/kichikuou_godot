using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>Every state change of FIGT_M (spec 02 §5.4).</summary>
public class FlavourStateTests
{
    // --- Asuka meets Mill (:8325-8372) -------------------------------------------------------

    [Theory]
    [InlineData(25, 0, 0, 0, 5)]
    [InlineData(0, 25, 0, 0, 5)]
    [InlineData(0, 0, 25, 0, 5)] // QUIRK Q8: the Mill-state test applies to slot 4 only
    [InlineData(0, 0, 0, 25, 1)]
    public async Task Asuka_with_mill_deployed_sets_F_Asuka_State(int s1, int s2, int s3, int s4, int millState)
    {
        var rig = new FlavourRig(1, 1).Player(unit: 11, cmdr: 0);
        rig.State.Bt_PlayerSlot1 = (Sys3Int)s1;
        rig.State.Bt_PlayerSlot2 = (Sys3Int)s2;
        rig.State.Bt_PlayerSlot3 = (Sys3Int)s3;
        rig.State.Bt_PlayerSlot4 = (Sys3Int)s4;
        rig.State.F_Mill_State = (Sys3Int)millState;

        await rig.RunAsync();

        Assert.Equal(1, rig.State.F_Asuka_State.Value);
        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([8330, 8339, 8342, 8347, 8356, 8369], rig.Lines); // one window, six pages
        Assert.Single(rig.View.Events.OfType<WindowEvent>(), w => w.Op > 140);
        Assert.Equal(6, rig.View.Events.OfType<WindowEvent>().Count(w => w.Op == 11));
    }

    [Fact]
    public async Task Asuka_with_mill_in_slot_4_and_mill_state_2_does_not_trigger()
    {
        var rig = new FlavourRig(1, 1, 1).Player(unit: 11, cmdr: 0);
        rig.State.Bt_PlayerSlot4 = 25;
        rig.State.F_Mill_State = 2;

        await rig.RunAsync();

        Assert.Equal(0, rig.State.F_Asuka_State.Value);
        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([8433], rig.Lines);
    }

    [Fact]
    public async Task Asuka_scene_plays_once()
    {
        var rig = new FlavourRig(1, 1, 2).Player(unit: 11, cmdr: 0);
        rig.State.Bt_PlayerSlot1 = 25;
        rig.State.F_Asuka_State = 1;

        await rig.RunAsync();

        Assert.Equal(1, rig.State.F_Asuka_State.Value);
        Assert.Equal([8442], rig.Lines);
    }

    // --- Satella (unit 29) and unit 30 one-shot scenes (:1255-1259, :1415, L_0ba24/0bb6d/0bc93)

    [Fact]
    public async Task Satella_vs_commander_91_plays_the_scene_once()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 29, cmdr: 91);
        rig.State.StoryStage = 2;

        await rig.RunAsync();

        Assert.Equal(1, rig.State.F_Satella_LineCmdr91.Value);
        Assert.Equal(9681, rig.Lines[0]);
        Assert.Equal([2, 3], rig.Draws);

        var again = new FlavourRig(1, 1).Player(unit: 29, cmdr: 91);
        again.State.StoryStage = 2;
        again.State.F_Satella_LineCmdr91 = 1;
        await again.RunAsync();
        Assert.Equal([1294], again.Lines);
    }

    [Fact]
    public async Task Satella_vs_commander_91_from_stage_3_skips_the_scene()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 29, cmdr: 91);
        rig.State.StoryStage = 3;

        await rig.RunAsync();

        Assert.Equal(0, rig.State.F_Satella_LineCmdr91.Value);
        Assert.Equal([1294], rig.Lines);
    }

    [Fact]
    public async Task Satella_vs_commander_92_sets_its_own_flag()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 29, cmdr: 92);
        rig.State.StoryStage = 1;

        await rig.RunAsync();

        Assert.Equal(1, rig.State.F_Satella_LineCmdr92.Value);
        Assert.Equal(0, rig.State.F_Satella_LineCmdr91.Value);
        Assert.Equal(9759, rig.Lines[0]);
    }

    [Fact]
    public async Task Unit_30_vs_commander_93_sets_F_Meglass_LineCmdr93()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 30, cmdr: 93);
        rig.State.StoryStage = 2;

        await rig.RunAsync();

        Assert.Equal(1, rig.State.F_Meglass_LineCmdr93.Value);
        Assert.Equal(9830, rig.Lines[0]);
    }

    [Fact]
    public async Task Unit_30_after_the_scene_has_its_fixed_line()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 30, cmdr: 93);
        rig.State.StoryStage = 2;
        rig.State.F_Meglass_LineCmdr93 = 1;

        await rig.RunAsync();

        Assert.Equal([1436], rig.Lines);
    }

    // --- Freak (unit 20) vs commander 130 (L_0bddd, :9900-10089) -------------------------------

    [Fact]
    public async Task Freak_first_meeting_advances_N_Freak_Stage_to_1()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 20, cmdr: 130);

        await rig.RunAsync();

        Assert.Equal(1, rig.State.N_Freak_Stage.Value);
        Assert.Equal(9905, rig.Lines[0]);
        Assert.Equal([2, 3], rig.Draws);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    public async Task Freak_later_meetings_play_the_short_scene(int stage)
    {
        var rig = new FlavourRig(1, 1).Player(unit: 20, cmdr: 130);
        rig.State.N_Freak_Stage = (Sys3Int)stage;

        await rig.RunAsync();

        Assert.Equal(stage, rig.State.N_Freak_Stage.Value);
        Assert.Equal([10053, 10058, 10069, 10076], rig.Lines);
    }

    [Fact]
    public async Task Freak_vs_another_commander_has_a_plain_line()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 20, cmdr: 22);

        await rig.RunAsync();

        Assert.Equal([657], rig.Lines);
        Assert.Equal(0, rig.State.N_Freak_Stage.Value);
    }

    // --- Sieg (Rance vs commander 93, :8957-8960 → L_0c0fc :10090-10304) ----------------------

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Sieg_event_moves_chaos_to_rance(int siegState)
    {
        var rig = new FlavourRig(1, 1).Player(unit: 1, cmdr: 93);
        var s = rig.State;
        s.F_Sieg_State = (Sys3Int)siegState;
        s.N_Miki_Stage = 1;
        s.Cmdr_StrBase = 176;
        s.Units[1].Item = 7;
        s.Units[1].CgFull = 1000;
        s.Units[1].CgPartial = 1001;
        s.Units[1].CgAlone = 1002;
        s.Cmdrs[93].Item = 1;
        s.Cmdrs[93].VAR0126 = 5;
        s.Cmdrs[93].VAR0127 = 6;
        s.Items[1].Holder = 0;
        s.Items[7].Holder = 1;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal(4, s.Ev_Entry.Value);
        Assert.Equal(4, s.F_Sieg_State.Value);
        Assert.Equal(1, s.F_Sieg_Defeated.Value);
        Assert.Equal(2, s.N_Miki_Stage.Value);
        Assert.Equal(1, s.Item_Cur.Value);
        Assert.Equal(1, s.Items[1].Holder.Value);
        Assert.Equal(1, s.Items[7].Holder.Value); // QUIRK Q21: Rance's previous item still names him as holder
        Assert.Equal("Sieg", s.Strings[4]);
        Assert.Equal("Maneshitas", s.Strings[5]);
        Assert.Equal("Sieg", s.Strings[176 + 92 * 3]);
        Assert.Equal("Maneshitas", s.Strings[176 + 92 * 3 + 1]);
        Assert.Equal(93, s.Cmdr_Cur.Value);
        Assert.Equal(1325, s.Cmdrs[93].CgFull.Value);
        Assert.Equal(1326, s.Cmdrs[93].CgPartial.Value);
        Assert.Equal(1327, s.Cmdrs[93].CgAlone.Value);
        Assert.Equal(59, s.Cmdrs[93].FaceCg.Value);
        Assert.Equal(0, s.Cmdrs[93].Item.Value);
        Assert.Equal(0, s.Cmdrs[93].VAR0126.Value);
        Assert.Equal(0, s.Cmdrs[93].VAR0127.Value);
        Assert.Equal(1, s.Unit_Cur.Value);
        Assert.Equal(1, s.Units[1].Item.Value);
        Assert.Equal(1000, s.Units[1].CgFull.Value); // unchanged
        Assert.Equal(1358, s.Units[1].CgPartial.Value);
        Assert.Equal(1361, s.Units[1].CgAlone.Value);
        Assert.Equal(19, rig.Lines.Length);
        Assert.Equal(10118, rig.Lines[0]);
        Assert.Equal(10296, rig.Lines[^1]);
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public async Task Sieg_event_leaves_other_miki_stages_alone()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 1, cmdr: 93);
        rig.State.F_Sieg_State = 2;
        rig.State.N_Miki_Stage = 3;

        await rig.RunAsync();

        Assert.Equal(3, rig.State.N_Miki_Stage.Value);
        Assert.Equal(4, rig.State.F_Sieg_State.Value);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    public async Task Rance_vs_commander_93_outside_sieg_states_2_and_3_is_a_plain_line(int siegState)
    {
        var rig = new FlavourRig(1, 1).Player(unit: 1, cmdr: 93);
        rig.State.F_Sieg_State = (Sys3Int)siegState;

        await rig.RunAsync();

        Assert.Equal([9260], rig.Lines);
        Assert.Equal(siegState, rig.State.F_Sieg_State.Value);
        Assert.Equal(0, rig.State.Ev_Entry.Value);
        Assert.Equal(0, rig.State.F_Sieg_Defeated.Value);
    }
}
