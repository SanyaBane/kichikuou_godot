using Kichikuou.Core.Combat;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// FlavourText, player side (spec 02 §5.2). Expected lines are the first text line of each
/// window in the raw FIGT_M.ADV decompile. Rolls: :5, :25, then the branch draw.
/// </summary>
public class FlavourPlayerTests
{
    // --- Rance (unit 1, L_0ac75, FIGT_M.ADV:8956-9635) -----------------------------------

    [Fact]
    public async Task Rance_special_1_is_the_rance_attack_line()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 1, cmdr: 2);
        rig.State.Bt_SpecialAtk = 1;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([8965], rig.Lines); // :8962 is tested before the commander guards
    }

    [Fact]
    public async Task Rance_has_a_line_per_known_commander()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 1, cmdr: 2);
        rig.State.Units[1].Troops = 100;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([9001], rig.Lines);
    }

    [Fact]
    public async Task Rance_vs_commander_31_has_a_line_only_at_stage_0()
    {
        var stage0 = new FlavourRig(1, 1).Player(unit: 1, cmdr: 31);
        stage0.State.StoryStage = 0;
        await stage0.RunAsync();
        Assert.Equal([8975], stage0.Lines);

        var stage1 = new FlavourRig(1, 1, 5).Player(unit: 1, cmdr: 31);
        stage1.State.StoryStage = 1;
        stage1.State.Units[1].Troops = 100;
        await stage1.RunAsync();
        Assert.Equal([2, 3, 10], stage1.Draws);
        Assert.Equal([9584], stage1.Lines);
    }

    [Fact]
    public async Task Rance_with_chaos_against_a_strong_commander_has_two_texts_in_one_window()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 1, cmdr: 200);
        rig.State.Cmdrs[200].Class = 10;
        rig.State.Units[1].Item = 1;
        rig.State.Units[1].Troops = 100;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Collection(rig.View.Events,
            e => Assert.IsType<FlavourWindowOpenEvent>(e),
            e => Assert.Equal(new MessageEvent(new ScriptRef("FIGT_M.ADV", 9498)), e),
            e => Assert.IsType<FlavourKeyWaitEvent>(e),
            e => Assert.Equal(new MessageEvent(new ScriptRef("FIGT_M.ADV", 9511)), e),
            e => Assert.IsType<FlavourKeyWaitEvent>(e),
            e => Assert.IsType<FlavourWindowCloseEvent>(e));
    }

    [Fact]
    public async Task Rance_without_troops_has_a_line()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 1, cmdr: 0);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([9525], rig.Lines);
    }

    [Fact]
    public async Task Rance_otherwise_rolls_one_of_ten_lines()
    {
        var rig = new FlavourRig(1, 1, 4).Player(unit: 1, cmdr: 0);
        rig.State.Units[1].Troops = 100;

        await rig.RunAsync();

        Assert.Equal([2, 3, 10], rig.Draws);
        Assert.Equal([9571], rig.Lines);
        Assert.Equal(4, rig.State.Tmp0690.Value);
    }

    [Fact]
    public async Task Rance_roll_of_eleven_shows_nothing()
    {
        // QUIRK Q20: Y 4,10 returning n+1 matches no arm.
        var rig = new FlavourRig(1, 1, 11).Player(unit: 1, cmdr: 0);
        rig.State.Units[1].Troops = 100;

        await rig.RunAsync();

        Assert.Equal([2, 3, 10], rig.Draws);
        Assert.Empty(rig.View.Events);
    }

    // --- Sill (unit 2, L_0ab18, :8889-8955): the :25 roll picks her magic line --------------

    [Theory]
    [InlineData(1, 8893)]
    [InlineData(2, 8901)]
    public async Task Sill_magic_line_follows_the_roll_at_25(int roll25, int line)
    {
        var rig = new FlavourRig(1, roll25).Player(unit: 2, cmdr: 0, mode: 1);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([line], rig.Lines);
    }

    [Fact]
    public async Task Sill_magic_with_roll_3_at_25_rolls_one_of_five()
    {
        var rig = new FlavourRig(1, 3, 2).Player(unit: 2, cmdr: 0, mode: 1);

        await rig.RunAsync();

        Assert.Equal([2, 3, 5], rig.Draws);
        Assert.Equal([8920], rig.Lines);
    }

    [Fact]
    public async Task Sill_physical_ignores_the_roll_at_25()
    {
        var rig = new FlavourRig(1, 1, 5).Player(unit: 2, cmdr: 0, mode: 3);

        await rig.RunAsync();

        Assert.Equal([2, 3, 5], rig.Draws);
        Assert.Equal([8949], rig.Lines);
        Assert.Equal(new FlavourWindowOpenEvent(FlavourWindow.LeftAlt, 896), rig.View.Events[0]);
    }

    // --- Miki (unit 32, :1493-1548) --------------------------------------------------------

    [Fact]
    public async Task Miki_special_has_a_line()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 32, cmdr: 0);
        rig.State.Bt_SpecialAtk = 3;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([1497], rig.Lines);
    }

    [Fact]
    public async Task Miki_vs_kayblis_has_a_line()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 32, cmdr: 135);

        await rig.RunAsync();

        Assert.Equal([1505], rig.Lines);
    }

    [Fact]
    public async Task Miki_otherwise_rolls_one_of_three()
    {
        var rig = new FlavourRig(1, 1, 2).Player(unit: 32, cmdr: 0);

        await rig.RunAsync();

        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([1528], rig.Lines);
    }

    // --- Asuka (unit 11, L_0a041, :8314-8457) ----------------------------------------------

    [Fact]
    public async Task Asuka_special_has_a_line()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 11, cmdr: 0);
        rig.State.Bt_SpecialAtk = 4;
        rig.State.Bt_PlayerSlot1 = 25;

        await rig.RunAsync();

        Assert.Equal([8318], rig.Lines);
        Assert.Equal(0, rig.State.F_Asuka_State.Value); // tested before the Mill scene
    }

    [Fact]
    public async Task Asuka_magic_line_inserts_a_text_for_commanders_1_to_7()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 11, cmdr: 2, mode: 1);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Collection(rig.View.Events,
            e => Assert.IsType<FlavourWindowOpenEvent>(e),
            e => Assert.Equal(new MessageEvent(new ScriptRef("FIGT_M.ADV", 8386)), e),
            e => Assert.Equal(new MessageEvent(new ScriptRef("FIGT_M.ADV", 8423)), e),
            e => Assert.IsType<FlavourKeyWaitEvent>(e),
            e => Assert.IsType<FlavourWindowCloseEvent>(e));
    }

    [Fact]
    public async Task Asuka_magic_line_without_insert()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 11, cmdr: 50, mode: 1);

        await rig.RunAsync();

        Assert.Equal([8423], rig.Lines);
    }

    [Fact]
    public async Task Asuka_otherwise_rolls_one_of_three()
    {
        var rig = new FlavourRig(1, 1, 3).Player(unit: 11, cmdr: 0);

        await rig.RunAsync();

        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([8451], rig.Lines);
    }

    // --- Bound (unit 3, L_0aa40, :8844-8888): arms 2/3 test RetVal (Q13) --------------------

    [Fact]
    public async Task Bound_arm_1_then_key_2_also_shows_arm_2()
    {
        var rig = new FlavourRig([1, 1, 1], keys: [2]).Player(unit: 3, cmdr: 0);

        await rig.RunAsync();

        Assert.Equal([2, 3, 3], rig.Draws);
        Assert.Equal([8864, 8873], rig.Lines);
    }

    [Fact]
    public async Task Bound_arm_1_with_an_ordinary_key_shows_one_line()
    {
        var rig = new FlavourRig([1, 1, 1], keys: [16]).Player(unit: 3, cmdr: 0);

        await rig.RunAsync();

        Assert.Equal([8864], rig.Lines);
    }

    [Theory]
    [InlineData(2, 8873)]
    [InlineData(3, 8882)]
    public async Task Bound_arms_2_and_3_read_the_roll_through_RetVal(int roll, int line)
    {
        var rig = new FlavourRig(1, 1, roll).Player(unit: 3, cmdr: 0);

        await rig.RunAsync();

        Assert.Equal([line], rig.Lines);
    }

    [Fact]
    public async Task Bound_mode_2_line_is_dead_because_mode_2_speaks_from_the_enemy_side()
    {
        // QUIRK Q18: FIGT_M.ADV:8845 is unreachable.
        var rig = new FlavourRig(1, 1).Enemy(cmdr: 0, unit: 3, mode: 2);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Empty(rig.View.Events);
    }

    // --- Others ------------------------------------------------------------------------------

    [Theory]
    [InlineData(1, 386)]
    [InlineData(0, 395)]
    public async Task Unit_17_vs_commander_107_depends_on_the_duel_flag(int duel, int line)
    {
        var rig = new FlavourRig(1, 1).Player(unit: 17, cmdr: 107);
        rig.State.Bt_IsDuel = (Sys3Int)duel;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal([line], rig.Lines);
    }

    [Theory]
    [InlineData(1, new[] { 2676 })]
    [InlineData(2, new int[0])]
    public async Task Unit_48_speaks_only_as_the_hanny_king(int identity, int[] lines)
    {
        var rig = new FlavourRig(1, 1).Player(unit: 48, cmdr: 0);
        rig.State.Unit48_Identity = (Sys3Int)identity;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal(lines, rig.Lines);
    }

    [Theory]
    [InlineData(449, new[] { 2773 })]
    [InlineData(0, new int[0])] // Goliath / kaiju: nothing
    public async Task Unit_51_speaks_by_its_face(int face, int[] lines)
    {
        var rig = new FlavourRig(1, 1).Player(unit: 51, cmdr: 0);
        rig.State.Units[51].FaceCg = (Sys3Int)face;

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Equal(lines, rig.Lines);
    }

    [Fact]
    public async Task An_unlisted_unit_shows_nothing()
    {
        var rig = new FlavourRig(1, 1).Player(unit: 52, cmdr: 1);

        await rig.RunAsync();

        Assert.Equal([2, 3], rig.Draws);
        Assert.Empty(rig.View.Events);
    }
}
