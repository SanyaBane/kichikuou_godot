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
        GalleryStates.CastleBuildings(s);
        Assert.Equal([41, 47, 97, 103, 133, 502], Lines(GalleryStates.ProvinceMenuItems(s, 1)));
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

    [Fact]
    public void An_enemy_attack_names_its_army_and_province_and_the_scouts_report_its_places()
    {
        var s = new GameState { Prov_StrBase = 200, Week = 18, F_Kanami_NinjaUsed = 0, F_Freya_NinjaUsed = 99, F_Kaoru_NinjaUsed = 99,
            F_Wichita_NinjaUsed = 99, F_Saitou_NinjaUsed = 99 };
        s.Strings[204] = "Ys";
        GalleryStates.EnemyAttack(s, 5, 2, "Leazas Rebel Army");
        var attack = Battles.EnemyAttack.Of(s, _ => "Kanami");
        Assert.Equal(("Leazas Rebel Army", "Ys", 5, 821), (attack.Army, attack.Province, attack.Record, attack.FlagCg));
        Assert.Equal("Ys", s.Strings[8]);
        Assert.Single(attack.Ninjas);
        GalleryStates.EnemyParty(s, 1, 6);
        Assert.Equal([1, 6, 0, 0], new[] { s.Bt_EnemyParty1, s.Bt_EnemyParty2, s.Bt_EnemyParty3, s.Bt_EnemyParty4 }.Select(v => v.Value));
    }

    [Fact]
    public void A_deployment_shows_its_party_its_totals_and_the_details()
    {
        var s = World();
        GalleryStates.Deployment(s, 4, [1, 12], 12);
        var party = DeploymentTable.Party(s);
        Assert.Equal([1, 12], party.Places.Where(m => m is not null).Select(m => m!.Unit));
        Assert.Equal(4, party.Places.Count);
        Assert.Equal(1100, party.Troops);
        Assert.Equal(12, DeploymentTable.Details(s)?.Unit);
        Assert.Equal(2, DeploymentTable.PartyPlace(s, 12));
        GalleryStates.Deployment(s, 1, [1], 0);
        Assert.Single(DeploymentTable.Party(s).Places);
        Assert.Null(DeploymentTable.Details(s));
    }

    [Fact]
    public void The_sample_library_holds_ten_autos_and_four_commented_manuals_newest_first()
    {
        var s = World();
        s.Week = 70;
        s.Tmp0021 = 70;
        var data = SaveData.Capture(s, new ScriptRef("COMMAND.ADV", 1));
        var newest = new DateTime(2026, 10, 20, 21, 30, 0);
        var library = GalleryStates.SampleLibrary(data, newest);
        Assert.Equal(ISaveLibrary.AutoCount, library.Autos.Count);
        Assert.Equal(GalleryStates.SampleComments.Count, library.Manuals.Count);
        var shelf = SaveSlots.ReadLibrary(library);
        Assert.Equal(newest, library.Read(library.Autos[0])?.Written);
        Assert.Equal(70, shelf.Autos[0].Week);
        var manuals = SaveSlots.NewestFirst(shelf.Manuals);
        Assert.Equal(GalleryStates.SampleComments, manuals.Select(m => m.Comment));
        Assert.Equal([70, 68, 66, 64], manuals.Select(m => m.Week));
        Assert.Equal([null, .. library.Manuals.Select(id => (SaveId?)id)], LoadSaveScreen.Items(library, save: true));
        Assert.Equal(ISaveLibrary.AutoCount + 4, LoadSaveScreen.Items(library, save: false).Count);
        Assert.Equal(library.Autos[0], LoadSaveScreen.Items(library, save: false)[0]);
    }
}
