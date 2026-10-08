using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// <c>COM.ADV</c> (<see cref="WeeklyEvents"/>): the early exits, the upkeep, tier 1 ("all that
/// apply") and tier 2 ("first match"), with the quirks STR-05 and STR-14 to STR-18. The whole file
/// is also compared with the decompile on random states (Content tests, <c>WeeklyEventsDifferentialTests</c>).
/// </summary>
public class WeeklyEventsTests
{
    /// <summary>The tier-2 rolls of a week in which no trigger applies: <c>:459 :465 :471 :612 :618 :633 :694 :800 :878 :934</c>.</summary>
    private static readonly int[] QuietDraws = [4, 2, 5, 15, 3, 10, 10, 2, 10, 10];

    /// <summary>Results for <see cref="QuietDraws"/> that open no roll-gated trigger.</summary>
    private static readonly int[] QuietRolls = [2, 2, 2, 2, 2, 6, 6, 2, 6, 6];

    /// <summary>
    /// A week of stage 2 in which no trigger applies: from a zero state, the flags whose zero
    /// opens a trigger are moved off it, and Lady Caravan is out of the walk (no upkeep roll).
    /// </summary>
    private static ScriptRig QuietWeek(params int[] rolls)
    {
        var rig = new ScriptRig(rolls);
        var s = rig.State;
        s.StoryStage = 2;
        s.Week = 10;
        s.N_ALChurch_Stage = 50;     // :152 GAL 1 at stage 2
        s.F_Fururu_State = 10;       // :326 Gｺﾙﾄﾞﾊﾞ 1 while unit 8 is absent
        s.F_Julia_State = 1;         // :589 Gｼﾞｭﾘｱ 1 while unit 5 is absent
        s.Bld_GirlsAcademy = 2;      // :597 Gｱﾋﾞｧﾄｰﾙ 2
        s.F_ShangriLa_State = 1;     // :775 Gﾏﾘｽ 12 after stage 1
        s.F_Cafe_State = 20;         // :58 the walk
        return rig;
    }

    private static Task RunAsync(ScriptRig rig) => new WeeklyEvents(rig.Script).RunAsync();

    private static (string, int) Call(string file, int entry) => (file, entry);

    [Fact]
    public async Task Week_9_plays_the_first_council_and_skips_the_rest()
    {
        var rig = new ScriptRig();
        rig.State.Week = 9;
        rig.State.StoryStage = 1;
        rig.State.LadyCaravan_Prov = 40;
        rig.State.TangeDog_UsedThisWeek = 1;

        await RunAsync(rig);

        Assert.Equal([Call("Iｼﾚｲﾌﾞ.ADV", 10)], rig.Story.Calls);
        Assert.Equal(10, rig.State.Ev_Entry.Value);
        // QUIRK: STR-05 — no upkeep: the caravan stays, the weekly flags are not reset.
        Assert.Equal(40, rig.State.LadyCaravan_Prov.Value);
        Assert.Equal(1, rig.State.TangeDog_UsedThisWeek.Value);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task A_thieves_week_plays_Souls_weekly_message_only()
    {
        var rig = new ScriptRig();
        rig.State.Week = 12;
        rig.State.StoryStage = 0;
        rig.State.F_Kanami_NinjaUsed = 0;

        await RunAsync(rig);

        Assert.Equal([Call("Gｿｳﾙ.ADV", 4)], rig.Story.Calls);
        Assert.Equal(0, rig.State.F_Kanami_NinjaUsed.Value);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task A_quiet_week_draws_every_tier_two_roll_in_order_and_plays_nothing()
    {
        var rig = QuietWeek(QuietRolls);

        await RunAsync(rig);

        Assert.Empty(rig.Story.Calls);
        // QUIRK: STR-14 — every roll is drawn although no trigger it gates is open.
        Assert.Equal(QuietDraws, rig.Random.Draws);
        Assert.Equal(0, rig.State.Ev_WeeklyEventDone.Value);
    }

    [Fact]
    public async Task The_upkeep_steps_the_weekly_counters()
    {
        var rig = QuietWeek(QuietRolls);
        var s = rig.State;
        s.N_Kanami_State = 1;              // serving: freed
        s.N_Freya_State = 19;              // not yet: spent
        s.N_Kaoru_State = 100;             // gone: spent
        s.N_Wichita_State = 3;             // freed
        s.F_Saitou_State = 0;              // not yet: spent
        s.Units[(Sys3Int)37].Status = 1;   // with Kaoru gone, an absent unit 37 opens Gｶﾞﾝｼﾞｰ 7
        s.Bt_GarrisonProv = 7;
        s.Bt_GarrisonHits = 3;
        s.Goliath_State = 2;
        s.N_Melfeis_Stage = 9;
        s.N_Isoroku_Stage = 5;
        s.N_Aviator_StudyWeeks = 0;        // floors at 0
        s.N_Miki_Timer = 5;                // stops at 5
        s.N_Aaya_Stage = 1;
        s.N_Saizel_Weeks = 41;
        s.N_Soul_Stage = 200;              // stops at 200
        s.TangeDog_UsedThisWeek = 1;
        s.Items[(Sys3Int)26].Holder = 100; // the kaiju items: attacks grow while owned, up to 2000
        s.Kaiju_JagiJagiAttacks = 1990;
        s.Kaiju_AmoebaAttacks = 500;

        await RunAsync(rig);

        Assert.Equal(0, s.F_Kanami_NinjaUsed.Value);
        Assert.Equal(99, s.F_Freya_NinjaUsed.Value);
        Assert.Equal(99, s.F_Kaoru_NinjaUsed.Value);
        Assert.Equal(0, s.F_Wichita_NinjaUsed.Value);
        Assert.Equal(99, s.F_Saitou_NinjaUsed.Value);
        Assert.Equal(0, s.Bt_GarrisonProv.Value);
        Assert.Equal(0, s.Bt_GarrisonHits.Value);
        Assert.Equal(1, s.Goliath_State.Value);
        Assert.Equal(10, s.N_Melfeis_Stage.Value);
        Assert.Equal(6, s.N_Isoroku_Stage.Value);
        Assert.Equal(0, s.N_Aviator_StudyWeeks.Value);
        Assert.Equal(5, s.N_Miki_Timer.Value);
        Assert.Equal(2, s.N_Aaya_Stage.Value);
        Assert.Equal(42, s.N_Saizel_Weeks.Value);
        Assert.Equal(200, s.N_Soul_Stage.Value);
        Assert.Equal(0, s.TangeDog_UsedThisWeek.Value);
        Assert.Equal(2000, s.Kaiju_JagiJagiAttacks.Value);
        Assert.Equal(500, s.Kaiju_AmoebaAttacks.Value);
        Assert.Empty(rig.Story.Calls);
    }

    [Theory]
    [InlineData(40, 1, 41)]
    [InlineData(40, 2, 39)]
    [InlineData(0, 2, 0)]      // QUIRK: STR-18 — saturates at 0
    [InlineData(40, 3, 40)]    // RND(2) = 3 (ENG-02) moves nowhere
    public async Task Lady_Caravan_walks_one_province_a_week(int from, int roll, int to)
    {
        var rig = QuietWeek([roll, .. QuietRolls]);
        rig.State.F_Cafe_State = 3;
        rig.State.LadyCaravan_Prov = (Sys3Int)from;

        await RunAsync(rig);

        Assert.Equal(to, rig.State.LadyCaravan_Prov.Value);
        Assert.Equal([2, .. QuietDraws], rig.Random.Draws);
    }

    [Fact]
    public async Task After_her_capture_the_caravan_variable_counts_weeks_without_a_roll()
    {
        var rig = QuietWeek(QuietRolls);
        rig.State.F_Cafe_State = 11;
        rig.State.LadyCaravan_Prov = 4;

        await RunAsync(rig);

        Assert.Equal(5, rig.State.LadyCaravan_Prov.Value);
        Assert.Equal(QuietDraws, rig.Random.Draws);
    }

    [Fact]
    public async Task Tier_one_plays_every_trigger_that_applies_and_skips_tier_two()
    {
        var rig = QuietWeek();
        rig.State.F_Kentarou_State = 2;
        rig.State.N_Miki_Stage = 2;

        await RunAsync(rig);

        Assert.Equal([Call("Gｹﾝﾀﾛｳ.ADV", 2), Call("Gｹﾝﾀﾛｳ.ADV", 1)], rig.Story.Calls);
        Assert.Equal(1, rig.State.Ev_WeeklyEventDone.Value);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task Tier_one_reads_the_cursor_a_scene_left()
    {
        var rig = QuietWeek();
        rig.State.F_Gandhi_State = 99;
        rig.State.N_Kaoru_State = 1;
        rig.State.Units[(Sys3Int)37].Status = 1;
        rig.Story.OnCall = (file, entry) =>
        {
            if (file == "Gｶﾞﾝｼﾞｰ.ADV" && entry == 8)
                rig.State.Unit_Cur = 40;   // a unit that is absent
        };

        await RunAsync(rig);

        // QUIRK: STR-15 — :279 tests Unit_Status with the cursor Gｶﾞﾝｼﾞｰ 8 left, not unit 37.
        Assert.Equal([Call("Gｶﾞﾝｼﾞｰ.ADV", 8), Call("Gｶﾞﾝｼﾞｰ.ADV", 7)], rig.Story.Calls);
    }

    [Fact]
    public async Task Zeths_generals_down_is_no_weekly_event()
    {
        var rig = QuietWeek(QuietRolls);
        rig.State.F_Zeth_War = 1;   // commanders 101, 104, 107 and 110 are all at status 0

        await RunAsync(rig);

        // QUIRK: STR-17 — Iｾﾞｽ 100 leaves Ev_WeeklyEventDone at 0, so tier 2 runs too.
        Assert.Equal([Call("Iｾﾞｽ.ADV", 100)], rig.Story.Calls);
        Assert.Equal(QuietDraws, rig.Random.Draws);
    }

    [Fact]
    public async Task A_tier_two_trigger_ends_the_file()
    {
        var rig = QuietWeek();
        rig.State.F_Mokomoko_State = 14;

        await RunAsync(rig);

        Assert.Equal([Call("Gﾓｺﾓｺ.ADV", 2)], rig.Story.Calls);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task Miki_stage_4_spends_the_week_without_a_scene()
    {
        var rig = QuietWeek(2, 2, 2);
        rig.State.N_Miki_Stage = 4;

        await RunAsync(rig);

        Assert.Equal(5, rig.State.N_Miki_Stage.Value);
        Assert.Empty(rig.Story.Calls);
        Assert.Equal([4, 2, 5], rig.Random.Draws);
    }

    [Fact]
    public async Task Triggers_without_a_return_let_the_next_one_play_the_same_week()
    {
        var rig = QuietWeek(2, 2, 2, 2, 2, 6);
        rig.State.Units[(Sys3Int)36].Status = 1;
        rig.State.Units[(Sys3Int)36].Troops = 150;
        rig.State.F_Aviator_State = 10;
        rig.State.N_Aaya_Stage = 4;
        rig.State.F_Seizel_State = 6;

        await RunAsync(rig);

        // QUIRK: STR-16 — Gｱﾋﾞｧﾄｰﾙ 3 and Gｱｰﾔ 3 have no %0.
        Assert.Equal([Call("Gｱﾋﾞｧﾄｰﾙ.ADV", 3), Call("Gｱｰﾔ.ADV", 3), Call("Gｻｲｾﾞﾙ.ADV", 5)], rig.Story.Calls);
        Assert.Equal([4, 2, 5, 15, 3, 10], rig.Random.Draws);
    }

    [Fact]
    public async Task Freaks_first_scene_marks_the_week_and_falls_through()
    {
        var rig = QuietWeek();
        rig.State.Units[(Sys3Int)20].Status = 1;
        rig.State.N_Freak_Stage = 1;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Gﾌﾘｰｸ.ADV")
                rig.State.N_Freak_Stage = 2;
        };

        await RunAsync(rig);

        // QUIRK: STR-16 — the stage the scene sets opens the next trigger at once.
        Assert.Equal([Call("Gﾌﾘｰｸ.ADV", 2), Call("Gﾌﾘｰｸ.ADV", 3)], rig.Story.Calls);
        Assert.Equal(1, rig.State.Ev_WeeklyEventDone.Value);
    }

    [Fact]
    public async Task One_roll_chooses_among_five_scenes()
    {
        var rig = QuietWeek(2, 2, 2, 2, 2, 6, 6, 2, 3);
        rig.State.Units[(Sys3Int)33].Status = 1;

        await RunAsync(rig);

        Assert.Equal([Call("Gｼﾞｭﾘｱ.ADV", 2)], rig.Story.Calls);
        Assert.Equal(33, rig.State.Unit_Cur.Value);
        Assert.Equal([4, 2, 5, 15, 3, 10, 10, 2, 10], rig.Random.Draws);
    }

    [Fact]
    public async Task A_won_war_makes_the_kaiju_prince_certain_and_the_church_follows()
    {
        var rig = QuietWeek(QuietRolls);
        rig.State.F_Helman_War = 2;
        rig.State.F_KaijuPrince_State = 1;
        rig.State.N_ALChurch_Stage = 100;

        await RunAsync(rig);

        Assert.Equal(1, rig.State.RetVal.Value);
        // QUIRK: STR-16 — Gｵｳｼﾞ 1 has no %0, so GAL 5 plays the same week.
        Assert.Equal([Call("Gｵｳｼﾞ.ADV", 1), Call("GAL.ADV", 5)], rig.Story.Calls);
        Assert.Equal(QuietDraws, rig.Random.Draws);
    }
}
