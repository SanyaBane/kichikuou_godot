using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The screen gallery's states and samples (docs/ui/gallery.md): each builds the model its screen shows.</summary>
public class ScreenGalleryTests
{
    /// <summary>Two units holding items (Rance: item 6, Rick: item 7), a third at status 1 holding nothing, the items' names.</summary>
    private static GameState World()
    {
        var s = new GameState { Unit_StrBase = 20, Item_StrBase = 300, Gold = 500 };
        void Unit(int n, string name, int status, int item, int troops)
        {
            s.Strings[20 + (n - 1) * 3] = name;
            var u = s.Units[n];
            u.Status = (Sys3Int)status;
            u.Item = (Sys3Int)item;
            u.Troops = (Sys3Int)troops;
            u.MaxTroops = 1000;
        }
        Unit(1, "Rance", 1, 6, 900);
        Unit(7, "Rick", 2, 7, 1000);
        Unit(12, "Haurein", 1, 0, 200);
        s.Strings[305] = "L. Holy Sword";
        s.Strings[306] = "By Lord";
        return s;
    }

    [Fact]
    public void Every_message_sample_is_its_kind_of_window()
    {
        foreach (var sample in GalleryStates.Messages)
        {
            var window = sample.Window();
            Assert.Equal(sample.Kind, window.Kind);
            Assert.Equal(sample.Read, window.Read);
            Assert.NotEmpty(window.Glyphs);
        }
    }

    [Fact]
    public void The_samples_cover_every_kind_and_their_names_are_unique()
    {
        Assert.Equal(Enum.GetValues<MessageKind>().ToHashSet(), GalleryStates.Messages.Select(m => m.Kind).ToHashSet());
        Assert.Equal(GalleryStates.Messages.Count, GalleryStates.Messages.Select(m => m.Id).Distinct().Count());
    }

    [Fact]
    public void A_place_title_sample_is_its_title_and_room()
    {
        var window = GalleryStates.Messages.Single(m => m.Kind == MessageKind.PlaceTitle).Window();
        Assert.Equal(("Leazas Castle", "The Throne Room"), (window.Title, window.Subtitle));
    }

    [Fact]
    public void The_cg_bar_samples_split_their_speaker_off()
    {
        var speech = GalleryStates.Messages.Single(m => m.Id == "message-cg-bar-speech").Window();
        var thought = GalleryStates.Messages.Single(m => m.Id == "message-cg-bar-thought").Window();
        Assert.Equal(("Maris", false), (speech.Speaker, speech.Thinks));
        Assert.Equal(("Rance", true), (thought.Speaker, thought.Thinks));
        Assert.Equal("", GalleryStates.Messages.Single(m => m.Id == "message-cg-bar-narration").Window().Speaker);
    }

    [Fact]
    public void A_unit_picked_first_asks_which_item()
    {
        var s = World();
        GalleryStates.TreasureUnitPicked(s, 1);
        Assert.Equal(1, TreasureRoster.View(s));
        Assert.Equal(new TreasurePrompt(true, 1, "Rance", 0), TreasureRoster.Prompt(s));
    }

    [Fact]
    public void An_item_picked_first_asks_before_it_replaces_a_holders_item()
    {
        var s = World();
        GalleryStates.TreasureItemPicked(s, 6);
        Assert.Equal(0, TreasureRoster.View(s));
        Assert.Equal(["Rick already has By Lord equipped.", "Replace it with L. Holy Sword?"], TreasureRoster.ReplaceQuestion(s, 7));
    }

    [Fact]
    public void The_holders_and_the_neediest_unit_are_found()
    {
        var s = World();
        Assert.Equal([1, 7], GalleryStates.MovableHolders(s));
        Assert.Equal(12, GalleryStates.NeediestUnit(s));
    }

    [Fact]
    public void Replenishs_preview_fills_the_troops_at_its_price()
    {
        var result = SubordinateResult.ReplenishPreview(World(), 12);
        Assert.Equal((200, 1000, 16, 484), (result.TroopsBefore, result.Troops, result.Cost, result.Gold));
        Assert.Equal("Troops Replenished", result.Title);
    }

    private static int[] Lines(IReadOnlyList<SceneMenuItem> items) => [.. items.Select(i => i.At.Line)];

    [Fact]
    public void Spent_commands_make_the_command_menu_offer_its_crossed_items()
    {
        var s = new GameState { StoryStage = 1, Ui_CmdBuildAvail = 1, Ui_CmdHaremAvail = 1, Ui_CmdMerchantAvail = 1, Ui_CmdSubordAvail = 1,
            CmdAvail_SubTroops = 1, CmdAvail_SubTalk = 1 };
        Assert.Equal([229, 241, 252, 257, 270, 288, 292, 296, 300, 310], Lines(CommandPhase.MenuItems(s)));
        GalleryStates.CommandsSpent(s);
        Assert.Equal([235, 247, 252, 263, 283, 288, 292, 296, 300, 310], Lines(CommandPhase.MenuItems(s)));
        Assert.NotNull(CommandMenu.For(CommandPhase.MenuItems(s), [.. CommandPhase.MenuItems(s).Select(_ => "○Item")]));
    }

    [Fact]
    public void A_province_menu_is_its_city_files_by_the_owner()
    {
        var s = new GameState { StoryStage = 1, Prov_Count = 10, Ui_CmdOwnCityAvail = 1 };
        s.Provs[1].Owner = 1;
        s.Provs[1].Economy = 1;
        s.Provs[6].Owner = 3;
        Assert.Equal([64, 76, 81], Lines(GalleryStates.ProvinceMenuItems(s, 6)));
        Assert.Equal(6, s.Prov_Cur.Value);
        Assert.Equal([41, 47, 133, 502], Lines(GalleryStates.ProvinceMenuItems(s, 1)));
        Assert.Equal((1, 16), (s.Prov_Cur.Value, s.Unit_Cur.Value));
    }

    [Theory]
    [InlineData(1, new[] { 884, 889 })]
    [InlineData(7, new[] { 866, 884, 889 })]
    [InlineData(14, new[] { 866, 889 })]
    public void The_caves_floors_offer_their_ways(int floor, int[] lines)
    {
        var s = new GameState();
        GalleryStates.DungeonFloor(s, GalleryStates.Cave, floor);
        var items = DungeonCommand.FloorMenuItems(s);
        Assert.Equal(lines, Lines(items));
        var ladder = FloorLadder.For(s, items, [.. items.Select(_ => "Way")]);
        Assert.NotNull(ladder);
        Assert.Equal((floor, floor > 1, floor < 14), (ladder.Floor, ladder.Up is not null, ladder.Down is not null));
        Assert.Equal(1032, s.CallArg.Value);
    }

    [Fact]
    public void A_week_starts_report_reads_its_figures_and_the_budget_around_them()
    {
        var s = new GameState { Gold = 1000, DateYear = 3, DateMonth = 5, DateWeek = 2 };
        WeekReport? report = null;
        static string Text(ScriptRef at, IReadOnlyList<Sys3Int> numbers) => at.Line switch
        {
            98 => "Leazas Army Phase",
            103 => $"LP {numbers[0].Value}, Month {numbers[1].Value}, Week {numbers[2].Value}",
            173 => $"Revenue & Troop Replenishment\nRevenue: {numbers[0].Value}00K GOLD\nReplenished Troops: {numbers[1].Value}",
            188 => "Special Expenses (Donations)",
            201 => "AL Church: -6500K GOLD",
            _ => "",
        };
        foreach (var e in GalleryStates.LeazasWeekStart(s, 202, 1385, 201))
        {
            if (report is null)
                report = WeekReport.Start(e, s, Text);
            else
                report.Observe(e, s, Text);
        }
        Assert.NotNull(report);
        Assert.True(report.Complete);
        Assert.Equal(("LP 3 · Month 5 · Week 2", 86300, 100000), (report.Date, report.BudgetBefore, report.BudgetAfter));
        Assert.Equal((20200, 1385), (report.Revenue!.Amount, report.Troops!.Amount));
        Assert.Equal([new ReportLine("AL Church", -6500)], report.Expenses);
        Assert.Equal(1000, s.Gold.Value);
    }

    [Fact]
    public void The_log_sample_has_every_kind_of_page_and_more_than_a_paper()
    {
        var log = new MessageLog();
        foreach (var page in GalleryStates.LogSample)
            log.Add(page);
        Assert.Equal(Enum.GetValues<LogKind>().ToHashSet(), GalleryStates.LogSample.Select(p => p.Kind).ToHashSet());
        Assert.True(log.Lines().Count > 18);                         // the rows MessageLogView's paper holds
        Assert.Contains(log.Lines(), line => line.Chosen);
    }
}
