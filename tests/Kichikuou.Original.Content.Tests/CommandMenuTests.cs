using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="CommandMenu"/>: <c>COMMAND.ADV</c>'s command menu as our panel shows it (docs/ui/command-screen.md, "The command panel").</summary>
public class CommandMenuTests
{
    private const string Status = "Leazas\n Budget: 116400K GOLD\n Troops: 6251\n   Month 5, Week 2";

    private static IReadOnlyList<SceneMenuItem> Items(params int[] lines) =>
        [.. lines.Select(line => new SceneMenuItem(new ScriptRef("COMMAND.ADV", line), [new CapturedText(new ScriptRef("COMMAND.ADV", line + 1), [], [])]))];

    [Fact]
    public void Leazas_menu_shows_the_status_the_commands_uses_the_links_and_Turn_END()
    {
        var s = new GameState { DateYear = 3 };
        var menu = CommandMenu.For(s, Items(229, 247, 252, 263, 270, 288, 292, 296, 300, 310),
            ["○Construction", "×Harem", "○Treasure", "△Merchant", "◎Subordinates", "→Leazas Castle", "Load/Save", "All Data", "Think", "Turn END"], Status)!;

        Assert.Equal(("Leazas", "LP 3 · Month 5 · Week 2"), (menu.Nation, menu.Date));
        Assert.Equal([("Budget", "116400K GOLD"), ("Troops", "6251")], menu.Status);
        Assert.Equal([
            new CommandMenuItem(1, "Construction", CommandAction.Build, true, 1, 1),
            new CommandMenuItem(2, "Harem", CommandAction.Harem, false, 0, 1),
            new CommandMenuItem(3, "Treasure", CommandAction.Items, true, 0, 0),
            new CommandMenuItem(4, "Merchant", CommandAction.Trade, false, 0, 1),
            new CommandMenuItem(5, "Subordinates", CommandAction.Units, true, 2, 2)], menu.Commands);
        Assert.Equal([new CommandMenuLink(6, "→Leazas Castle"), new CommandMenuLink(7, "Load/Save"), new CommandMenuLink(8, "All Data"),
            new CommandMenuLink(9, "Think")], menu.Links);
        Assert.Equal((10, "Turn END", false), (menu.TurnEnd, menu.TurnEndText, menu.Thieves));
    }

    [Fact]
    public void Subordinates_triangle_still_opens_and_the_thieves_have_their_own_menu()
    {
        var s = new GameState { DateYear = 3 };
        var leazas = CommandMenu.For(s, Items(229, 283, 310), ["○Construction", "△Subordinates", "Turn END"], Status)!;
        Assert.Equal(new CommandMenuItem(2, "Subordinates", CommandAction.Units, true, 0, 2), leazas.Commands[1]);

        var thieves = CommandMenu.For(s, Items(187, 205, 210, 214, 218), ["○Harem", "△Subordinates", "→Thieves’ Hideout", "Load/Save", "Turn END"],
            "Rance’s Thieves\n Money: 20K GOLD\n Subordinates: 150\n   Month 1, Week 3")!;
        Assert.True(thieves.Thieves);
        Assert.Equal("Rance's Thieves", thieves.Nation);
        Assert.Equal([("Money", "20K GOLD"), ("Subordinates", "150")], thieves.Status);
        Assert.Equal([CommandAction.Harem, CommandAction.Units], thieves.Commands.Select(c => c.Action));
        Assert.Equal(["→Thieves' Hideout", "Load/Save"], thieves.Links.Select(l => l.Text));
    }

    [Fact]
    public void Other_menus_or_no_status_are_not_the_command_menu()
    {
        var s = new GameState();
        Assert.Null(CommandMenu.For(s, Items(229, 310), ["○Construction", "Turn END"], null));
        Assert.Null(CommandMenu.For(s, Items(229, 300), ["○Construction", "Think"], Status));               // Turn END not last
        Assert.Null(CommandMenu.For(s, Items(229, 999, 310), ["○Construction", "?", "Turn END"], Status));  // an unknown item
    }

    /// <summary>
    /// The status window as the presenter reads it from the player's files (<c>COMMAND.ADV:443</c> with the
    /// week-18 figures) gives the panel's lines. Skipped without the player's files.
    /// </summary>
    [Fact]
    public void The_status_window_from_the_players_files_makes_the_panels_lines()
    {
        if (!File.Exists(Path.Combine(LocalData.ScenarioDirectory, "COMMAND.ADV")))
            return;
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(missing), new BoxGlyphs());
        presenter.Bind(new GameState { DateYear = 3, StoryStage = 1 });
        presenter.Show(new ValueMessageEvent(new ScriptRef("COMMAND.ADV", 443), [(Sys3Int)1164, (Sys3Int)6251, (Sys3Int)5, (Sys3Int)2]));
        presenter.OpenMenu(Items(229, 241, 252, 257, 270, 288, 292, 296, 300, 310));

        var menu = presenter.CommandMenu!;
        Assert.Empty(presenter.Unresolved);
        Assert.Equal(("Leazas", "LP 3 · Month 5 · Week 2"), (menu.Nation, menu.Date));
        Assert.Equal([("Budget", "116400K GOLD"), ("Troops", "6251")], menu.Status);
        Assert.Equal(["Construction", "Harem", "Treasure", "Merchant", "Subordinates"], menu.Commands.Select(c => c.Text));
        Assert.Equal(["→Leazas Castle", "Load/Save", "All Data", "Think"], menu.Links.Select(l => l.Text));
        Assert.Equal("Turn END", menu.TurnEndText);
    }
}
