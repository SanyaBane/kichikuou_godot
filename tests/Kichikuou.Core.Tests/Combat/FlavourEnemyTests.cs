using Kichikuou.Core.Combat;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// FlavourText, enemy side (<c>L_036f2</c>, FIGT_M.ADV:2824-7860, spec 02 §5.3). Rolls: :5,
/// :2825 (discarded), then the branch draw.
/// </summary>
public class FlavourEnemyTests
{
    [Theory]
    [InlineData(1, 2830)]
    [InlineData(7, 2844)]
    public async Task Commander_1_has_lines_for_units_1_and_7(int unit, int line)
    {
        var rig = new FlavourRig(1, 1).Enemy(cmdr: 1, unit: unit);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([line], rig.Lines);
    }

    [Theory]
    [InlineData(1, 2860)]
    [InlineData(2, 2873)]
    [InlineData(3, 2880)]
    public async Task Commander_1_otherwise_rolls_one_of_three(int roll, int line)
    {
        var rig = new FlavourRig(1, 1, roll).Enemy(cmdr: 1, unit: 0);

        await rig.RunAsync();

        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([line], rig.Lines);
        Assert.Equal(new FlavourWindowOpenEvent(FlavourWindow.Right, 897), rig.View.Events[0]);
    }

    [Fact]
    public async Task A_generic_commander_has_one_fixed_line()
    {
        var rig = new FlavourRig(1, 1).Enemy(cmdr: 5, unit: 1);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([3061], rig.Lines);
    }

    [Theory]
    [InlineData(191)]
    [InlineData(195)]
    [InlineData(199)]
    public async Task Commanders_191_to_199_share_one_line(int cmdr)
    {
        var rig = new FlavourRig(1, 1).Enemy(cmdr: cmdr, unit: 1);

        await rig.RunAsync();

        Assert.Equal([7851], rig.Lines);
    }

    [Theory]
    [InlineData(94)]
    [InlineData(95)]
    [InlineData(61)]
    [InlineData(99)]
    public async Task Some_commanders_say_nothing(int cmdr)
    {
        var rig = new FlavourRig(1, 1).Enemy(cmdr: cmdr, unit: 1);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Empty(rig.View.Events);
    }

    [Fact]
    public async Task Story_stage_4_substitute_commander_200_says_nothing()
    {
        // PresentEngagement sets Cmdr_Cur = 200 at StoryStage 4 (FIGT.ADV:1803-1806).
        var rig = new FlavourRig(2, 3).Enemy(cmdr: 200, unit: 1);
        rig.State.StoryStage = 4;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Empty(rig.View.Events);
        Assert.Equal(200, rig.State.Cmdr_Cur.Value);
    }

    [Fact]
    public async Task Commander_83_arm_1_then_key_3_also_shows_arm_3_and_closes_once()
    {
        // QUIRK Q13: the arms test RetVal, which the key wait overwrites.
        var rig = new FlavourRig([1, 1, 1], keys: [3]).Enemy(cmdr: 83, unit: 1);

        await rig.RunAsync();

        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([5019, 5033], rig.Lines);
        Assert.Equal(2, rig.View.Events.OfType<FlavourWindowOpenEvent>().Count());
        Assert.Single(rig.View.Events.OfType<FlavourWindowCloseEvent>());
    }

    [Fact]
    public async Task Commander_83_with_an_ordinary_key_shows_one_line()
    {
        var rig = new FlavourRig([1, 1, 2], keys: [16]).Enemy(cmdr: 83, unit: 1);

        await rig.RunAsync();

        Assert.Equal([5027], rig.Lines);
    }

    [Theory]
    [InlineData(4, 1, new[] { 2, 3, 3 }, 6129)]
    [InlineData(2, 1, new[] { 2, 3 }, 6111)]
    public async Task Commander_110_after_zeth_state_9_has_its_own_lines(int mode, int roll, int[] draws, int line)
    {
        var rig = new FlavourRig(1, 1, roll).Enemy(cmdr: 110, unit: 1, mode: mode);
        rig.State.F_Zeth_State = 9;

        await rig.RunAsync();

        Assert.Equal(draws, rig.Draws);
        Assert.Equal([line], rig.Lines);
    }

    [Fact]
    public async Task Commander_110_before_zeth_state_9_has_a_fixed_line()
    {
        var rig = new FlavourRig(1, 1).Enemy(cmdr: 110, unit: 1);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([6171], rig.Lines);
    }

    [Fact]
    public async Task Commander_58_rolls_one_of_two_at_stage_0()
    {
        var rig = new FlavourRig(1, 1, 2).Enemy(cmdr: 58, unit: 1);
        rig.State.StoryStage = 0;

        await rig.RunAsync();

        Assert.Equal([2, 3, 2], rig.Draws);
        Assert.Equal([4684], rig.Lines);
    }

    [Fact]
    public async Task Commander_58_has_a_fixed_line_later()
    {
        var rig = new FlavourRig(1, 1).Enemy(cmdr: 58, unit: 1);
        rig.State.StoryStage = 2;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([4693], rig.Lines);
    }

    [Theory]
    [InlineData(0, new[] { 2, 3 }, 3485)]
    [InlineData(1, new[] { 2, 3, 3 }, -1)]
    public async Task Commander_31_has_a_fixed_line_only_at_stage_0(int stage, int[] draws, int line)
    {
        var rig = new FlavourRig(1, 1, 1).Enemy(cmdr: 31, unit: 1);
        rig.State.StoryStage = (Sys3Int)stage;

        await rig.RunAsync();

        Assert.Equal(draws, rig.Draws);
        if (line > 0)
            Assert.Equal([line], rig.Lines);
    }

    [Fact]
    public async Task Kayblis_rolls_one_of_four()
    {
        var rig = new FlavourRig(1, 1, 4).Enemy(cmdr: 135, unit: 0);

        await rig.RunAsync();

        Assert.Equal([2, 3, 4], rig.Draws);
        Assert.Equal([7199], rig.Lines);
    }

    [Theory]
    [InlineData(1, new[] { 2, 3 }, 9640)]
    [InlineData(0, new[] { 2, 3, 3 }, -1)]
    public async Task Commander_157_jumps_to_its_own_block(int unit, int[] draws, int line)
    {
        var rig = new FlavourRig(1, 1, 1).Enemy(cmdr: 157, unit: unit);

        await rig.RunAsync();

        Assert.Equal(draws, rig.Draws);
        if (line > 0)
            Assert.Equal([line], rig.Lines);
    }

    [Fact]
    public async Task Commander_93_against_rance_is_a_plain_line_on_the_enemy_side()
    {
        // The Sieg event is player-side only (spec 02 §5.4).
        var rig = new FlavourRig(1, 1).Enemy(cmdr: 93, unit: 1);
        rig.State.F_Sieg_State = 2;

        await rig.RunAsync();

        Assert.Equal([5322], rig.Lines);
        Assert.Equal(2, rig.State.F_Sieg_State.Value);
        Assert.Equal(0, rig.State.Ev_Entry.Value);
    }
}
