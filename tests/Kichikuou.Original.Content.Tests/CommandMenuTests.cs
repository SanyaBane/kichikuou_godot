using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="CommandMenu"/>: <c>COMMAND.ADV</c>'s command menu as our panel shows it (docs/ui/command-screen.md, "The command panel").</summary>
public class CommandMenuTests
{
    private static IReadOnlyList<SceneMenuItem> Items(params int[] lines) =>
        [.. lines.Select(line => new SceneMenuItem(new ScriptRef("COMMAND.ADV", line), [new CapturedText(new ScriptRef("COMMAND.ADV", line + 1), [], [])]))];

    [Fact]
    public void Leazas_menu_shows_the_commands_the_links_and_Turn_END()
    {
        var menu = CommandMenu.For(Items(229, 247, 252, 263, 270, 288, 292, 296, 300, 310),
            ["○Construction", "×Harem", "○Treasure", "△Merchant", "◎Subordinates", "→Leazas Castle", "Load/Save", "All Data", "Think", "Turn END"])!;

        Assert.Equal([
            new CommandMenuItem(1, "Construction", CommandAction.Build, true),
            new CommandMenuItem(2, "Harem", CommandAction.Harem, false),
            new CommandMenuItem(3, "Treasure", CommandAction.Items, true),
            new CommandMenuItem(4, "Merchant", CommandAction.Trade, false),
            new CommandMenuItem(5, "Subordinates", CommandAction.Units, true)], menu.Commands);
        Assert.Equal([new CommandMenuLink(6, "→Leazas Castle"), new CommandMenuLink(7, "Load", SaveList.Load),
            new CommandMenuLink(7, "Save", SaveList.Save), new CommandMenuLink(8, "All Data"), new CommandMenuLink(9, "Think")], menu.Links);
        Assert.Equal((10, "Turn END"), (menu.TurnEnd, menu.TurnEndText));
    }

    [Fact]
    public void Subordinates_triangle_still_opens_and_the_thieves_have_their_own_menu()
    {
        var leazas = CommandMenu.For(Items(229, 283, 310), ["○Construction", "△Subordinates", "Turn END"])!;
        Assert.Equal(new CommandMenuItem(2, "Subordinates", CommandAction.Units, true), leazas.Commands[1]);

        var thieves = CommandMenu.For(Items(187, 205, 210, 214, 218), ["○Harem", "△Subordinates", "→Thieves’ Hideout", "Load/Save", "Turn END"])!;
        Assert.Equal([CommandAction.Harem, CommandAction.Units], thieves.Commands.Select(c => c.Action));
        Assert.Equal(["→Thieves' Hideout", "Load", "Save"], thieves.Links.Select(l => l.Text));
        Assert.Equal([null, SaveList.Load, SaveList.Save], thieves.Links.Select(l => l.Opens));
        Assert.Equal([4, 4], thieves.Links.Skip(1).Select(l => l.Choice));
    }

    [Fact]
    public void Construction_shows_the_weeks_left_while_a_building_is_under_way()
    {
        string[] texts = ["○Construction", "○Harem", "Turn END"];
        var plain = CommandMenu.For(Items(229, 241, 310), texts)!;
        Assert.Equal([0, 0], plain.Commands.Select(c => c.WeeksLeft));
        Assert.Null(plain.Commands[0].WeeksText);

        var later = CommandMenu.For(Items(229, 241, 310), texts, 5)!;               // a later week: ○, still opens Cﾃｲﾄ
        Assert.Equal(new CommandMenuItem(1, "Construction", CommandAction.Build, true, 5), later.Commands[0]);
        Assert.Equal("5 weeks left", later.Commands[0].WeeksText);
        Assert.Equal(0, later.Commands[1].WeeksLeft);

        var ordered = CommandMenu.For(Items(235, 241, 310), ["×Construction", "○Harem", "Turn END"], 16)!;   // the week it is ordered
        Assert.Equal(new CommandMenuItem(1, "Construction", CommandAction.Build, false, 16), ordered.Commands[0]);
        Assert.Equal("1 week left", CommandMenu.For(Items(229, 241, 310), texts, 1)!.Commands[0].WeeksText);
    }

    [Fact]
    public void Other_menus_are_not_the_command_menu()
    {
        Assert.Null(CommandMenu.For(Items(229, 300), ["○Construction", "Think"]));                  // Turn END not last
        Assert.Null(CommandMenu.For(Items(229, 999, 310), ["○Construction", "?", "Turn END"]));     // an unknown item
    }
}
