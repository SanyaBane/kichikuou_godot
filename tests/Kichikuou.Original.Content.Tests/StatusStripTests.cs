using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="StatusStrip"/>: the status window and the week's uses on our strip (docs/ui/command-screen.md, "The status strip").</summary>
public class StatusStripTests
{
    /// <summary>Week 18, LP 3, month 5, week 2; budget 1164 (116400K), units 1 and 2 present, unit 3 not.</summary>
    private static GameState Week18()
    {
        var s = new GameState { Week = 18, Gold = 1164, StoryStage = 1 };
        s.Units[1].Status = 1;
        s.Units[1].Troops = 2000;
        s.Units[2].Status = 1;
        s.Units[2].Troops = 251;
        s.Units[3].Troops = 900;                                                // no status: not counted
        s.Ui_CmdBuildAvail = 1;
        s.Ui_CmdHaremAvail = 0;
        s.Ui_CmdMerchantAvail = 1;
        s.Ui_CmdSubordAvail = 1;
        s.CmdAvail_SubTroops = 0;
        s.CmdAvail_SubTalk = 1;
        s.Ui_CmdOwnCityAvail = 1;
        s.Ui_CmdAttackAvail = 1;
        s.Ui_CmdDungeonAvail = 0;
        return s;
    }

    private static string Fake(ScriptRef at, IReadOnlyList<Sys3Int> n) => at.File == "Cﾃｲﾄ.ADV"
        ? (at.Line == 339 ? "［Girls’ Military Academy］" : at.Line == 426 ? "[Library]" : $"[Building {at.Line}]")
            + $"\n Construction Time: 2 Months\n (Budget: {n[0].Value}00KG)"
        : at.Line == 443
        ? $"Leazas\n Budget: {n[0].Value}00K GOLD\n Troops: {n[1].Value}\n   Month {n[2].Value}, Week {n[3].Value}"
        : $"Rance’s Thieves\n Money: {n[0].Value}00K GOLD\n Subordinates: {n[1].Value}\n   Month {n[2].Value}, Week {n[3].Value}";

    [Theory]
    [InlineData(1, 3, 1, 1)]
    [InlineData(18, 3, 5, 2)]
    [InlineData(48, 3, 12, 4)]
    [InlineData(49, 4, 1, 1)]
    public void The_date_is_the_scripts(int week, int year, int month, int weekOfMonth) =>
        Assert.Equal((year, month, weekOfMonth), StatusStrip.DateOf(week));

    [Fact]
    public void Leazas_strip_shows_the_status_and_the_weeks_uses_in_groups()
    {
        var strip = StatusStrip.For(Week18(), Fake)!;

        Assert.Equal(("Leazas", false, "LP 3 · Month 5 · Week 2"), (strip.Nation, strip.Thieves, strip.Date));
        Assert.Equal([("Budget", "116,400K GOLD"), ("Troops", "2,251")], strip.Status);
        Assert.Equal(["Castle", "Subordinates", "Map"], strip.Groups.Select(g => g.Caption));
        Assert.Equal([("Construction", true), ("Harem", false), ("Merchant", true), ("Troops", false), ("Talk", true),
            ("Own city", true), ("Attack", true), ("Dungeon", false)], strip.Groups.SelectMany(g => g.Uses).Select(u => (u.Text, u.Free)));
    }

    [Fact]
    public void Subordinates_off_turns_both_parts_off_and_the_thieves_count_their_three_units()
    {
        var s = Week18();
        s.Ui_CmdSubordAvail = 0;
        s.CmdAvail_SubTroops = 1;
        Assert.All(StatusStrip.For(s, Fake)!.Groups[1].Uses, u => Assert.False(u.Free));    // △: neither part

        s.StoryStage = 0;
        s.Units[3].Status = 0;
        s.Units[4].Troops = 50;
        var thieves = StatusStrip.For(s, Fake)!;
        Assert.True(thieves.Thieves);
        Assert.Equal(("Rance's Thieves", ("Troops", "2,950")), (thieves.Nation, thieves.Status[1]));   // units 1, 3, 4 whatever their status; :443's label
        Assert.Equal(["Hideout", "Map"], thieves.Groups.Select(g => g.Caption));
        Assert.Equal(["Harem", "Subordinates", "Pillage"], thieves.Groups.SelectMany(g => g.Uses).Select(u => u.Text));   // no Dungeon: DGN-20
    }

    [Fact]
    public void A_building_under_way_is_a_third_state_from_the_week_it_is_ordered()
    {
        var s = Week18();
        Assert.Null(StatusStrip.For(s, Fake)!.Groups[0].Uses[0].UnderWay);        // BuildWeeksLeft 0: free as before
        s.Ui_CmdBuildAvail = 0;
        Assert.Equal(new WeekUse("Construction", WeekAction.Build, false), StatusStrip.For(s, Fake)!.Groups[0].Uses[0]);   // spent

        // Ordered this week: Cﾃｲﾄ spends the week's construction and sets the timer and the building to 2.
        s.BuildWeeksLeft = 16;
        s.Bld_Library = 2;
        var ordered = StatusStrip.For(s, Fake)!;
        Assert.Equal(new WeekUse("Construction", WeekAction.Build, false, new BuildingUnderWay("Library", 16)), ordered.Groups[0].Uses[0]);
        Assert.Equal("16 weeks left", ordered.Groups[0].Uses[0].UnderWay!.WeeksText);

        // The next weeks: the flag is lit again, the building still under way.
        s.Ui_CmdBuildAvail = 1;
        s.BuildWeeksLeft = 1;
        var last = StatusStrip.For(s, Fake)!;
        Assert.Equal(new BuildingUnderWay("Library", 1), last.Groups[0].Uses[0].UnderWay);
        Assert.Equal("1 week left", last.Groups[0].Uses[0].UnderWay!.WeeksText);
        Assert.NotEqual(ordered.Key, last.Key);

        s.Bld_Library = 0;
        s.Bld_GirlsAcademy = 2;
        Assert.Equal("Girls' Military Academy", StatusStrip.For(s, Fake)!.Groups[0].Uses[0].UnderWay!.Name);
        Assert.All(StatusStrip.For(s, Fake)!.Groups.Skip(1).SelectMany(g => g.Uses), u => Assert.Null(u.UnderWay));
    }

    /// <summary>The buildings' names from the player's files (<c>Cﾃｲﾄ.ADV</c>'s terms). Skipped without them.</summary>
    [ScenarioFact]
    public void The_building_under_way_is_named_from_the_players_files()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(missing), new BoxGlyphs());
        var names = new List<string?>();
        foreach (var set in (Action<GameState>[])[s => s.Bld_GirlsAcademy = 2, s => s.Bld_Library = 2, s => s.Bld_Hospital = 2,
            s => s.Bld_SMTower = 2, s => s.Bld_MariaLab = 2, s => s.Bld_MagicLab = 2])
        {
            var s = Week18();
            s.BuildWeeksLeft = 5;
            set(s);
            presenter.Bind(s);
            names.Add(presenter.StatusStrip!.Groups[0].Uses[0].UnderWay!.Name);
        }
        Assert.Empty(presenter.Unresolved);
        Assert.Equal(["Girls' Military Academy", "Library", "Hospital", "S&M Tower", "Maria Research Institute", "Magic Research Building"], names);
    }

    /// <summary>The status window's text from the player's files (<c>COMMAND.ADV:443</c>) through the presenter. Skipped without them.</summary>
    [ScenarioFact]
    public void The_strip_reads_the_status_window_from_the_players_files()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(missing), new BoxGlyphs());
        presenter.Bind(Week18());

        var strip = presenter.StatusStrip!;
        Assert.Empty(presenter.Unresolved);
        Assert.Equal(("Leazas", "LP 3 · Month 5 · Week 2"), (strip.Nation, strip.Date));
        Assert.Equal([("Budget", "116,400K GOLD"), ("Troops", "2,251")], strip.Status);

        var thieves = Week18();
        thieves.StoryStage = 0;
        presenter.Bind(thieves);
        Assert.Equal([("Money", "116,400K GOLD"), ("Troops", "2,900")], presenter.StatusStrip!.Status);   // :472's "Subordinates" relabelled
    }
}
