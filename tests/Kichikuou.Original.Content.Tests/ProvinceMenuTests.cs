using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="ProvinceMenu"/>: a city file's first menu as our panel shows it (docs/ui/command-screen.md, "The province menu").</summary>
public class ProvinceMenuTests
{
    /// <summary>A stage-1 world: <c>Cｼﾊｲﾄｼ</c> is reached from stage 1 only, and owner 1 is the player's from then (<see cref="GameState.IsOwnProvince"/>).</summary>
    private static GameState World(int province, int owner)
    {
        var s = new GameState { Prov_StrBase = 10, Prov_Count = 6, Prov_Cur = (Sys3Int)province, StoryStage = 1 };
        s.Strings[10 + province - 1] = "Oaks";
        var p = s.Provs[province];
        p.Owner = (Sys3Int)owner;
        p.BgCg = 1003;
        p.Economy = 12;
        p.Defense = 4;
        p.BattleArea = 232;
        return s;
    }

    private static IReadOnlyList<SceneMenuItem> Items(string file, params int[] lines) =>
        [.. lines.Select(line => new SceneMenuItem(new ScriptRef(file, line), [new CapturedText(new ScriptRef(file, line + 1), [], [])]))];

    private static string? Army(ScriptRef at) => (at.File, at.Line) switch
    {
        ("Cｼﾊｲﾄｼ.ADV", 20) => "\n   (Leazas Army)\nEconomy: 0  Defense: 0\nBattle Area: 0",
        ("Cﾃｷﾄｼ2.ADV", 71) => "(Rebel Army)",
        _ => null,
    };

    [Fact]
    public void A_neighbours_menu_offers_its_commands_with_tags_and_Return_last()
    {
        var s = World(6, 2);
        var menu = ProvinceMenu.For(s, Items("Cﾃｷﾄｼ.ADV", 64, 76, 81), ["Attack", "Ninja Maneuver", "Return"], Army)!;

        Assert.Equal((6, "Oaks", "Rebel Army", 1003), (menu.Province, menu.Name, menu.Army, menu.Picture));
        Assert.Equal([new ProvinceMenuItem(1, "Attack", ProvinceAction.Battle, true), new ProvinceMenuItem(2, "Ninja Maneuver", ProvinceAction.Ninja, true)],
            menu.Items);
        Assert.Equal(3, menu.Return);
        Assert.Equal(["Economy: ??", "Defense: ??", "Battle Area: ??"], menu.Card);
    }

    [Fact]
    public void An_own_citys_spent_items_are_off_but_the_Ruins_Search_follows_the_dungeon_flag()
    {
        var s = World(1, 1);
        var menu = ProvinceMenu.For(s, Items("Cｼﾊｲﾄｼ.ADV", 65, 71, 340, 436, 502),
            ["×Temporary Levy", "×Bolster Defense", "×Dungeon", "○Ruins Search", "Return"], Army)!;

        Assert.Equal("Leazas Army", menu.Army);
        Assert.Equal([("Temporary Levy", ProvinceAction.Levy, false), ("Bolster Defense", ProvinceAction.Build, false),
            ("Dungeon", ProvinceAction.Visit, false), ("Ruins Search", ProvinceAction.Search, true)],
            menu.Items.Select(i => (i.Text, i.Action, i.Enabled)));
        Assert.Equal("Economy: 12 (1,200K GOLD)", menu.Card[0]);

        var free = ProvinceMenu.For(s, Items("Cｼﾊｲﾄｼ.ADV", 41, 47, 133, 502), ["○Temporary Levy", "○Bolster Defense", "○Dungeon", "Return"], Army)!;
        Assert.All(free.Items, i => Assert.True(i.Enabled));
    }

    [Fact]
    public void Other_menus_of_the_city_files_and_of_other_files_are_not_province_menus()
    {
        var s = World(6, 2);
        Assert.Null(ProvinceMenu.For(s, Items("Cﾃｷﾄｼ.ADV", 700, 710, 720), ["a", "b", "c"], Army));      // a later menu
        Assert.Null(ProvinceMenu.For(s, Items("Cﾌﾞｶ.ADV", 64, 76, 81), ["a", "b", "c"], Army));
        Assert.Null(ProvinceMenu.For(s, Items("Cﾃｷﾄｼ.ADV", 64, 99, 81), ["a", "b", "c"], Army));       // an unknown item
        var distant = ProvinceMenu.For(s, Items("Cﾃｷﾄｼ2.ADV", 50, 55), ["Ninja Maneuver", "Return"], Army)!;
        Assert.Equal([ProvinceAction.Ninja], distant.Items.Select(i => i.Action));
    }

    /// <summary>
    /// The dungeon's menu (<c>Cｼﾊｲﾄｼ.ADV:732-763</c>, after the place Dungeon in Leazas Castle (1)) is a province
    /// menu too: the cells and Look Around as rows, Return last, the place's name from <c>:134</c>, and a cancel
    /// that only opens it again; the first menus' cancel leaves.
    /// </summary>
    [Fact]
    public void The_dungeons_menu_shows_its_cells_under_the_places_name()
    {
        var s = World(1, 1);
        string? Text(ScriptRef at) => at is ("Cｼﾊｲﾄｼ.ADV", 134) ? "○Dungeon" : Army(at);
        var menu = ProvinceMenu.For(s, Items("Cｼﾊｲﾄｼ.ADV", 741, 758, 762), ["Ruberan’s Cell", "Look Around", "Return"], Text)!;

        Assert.Equal([new ProvinceMenuItem(1, "Ruberan's Cell", ProvinceAction.Cell, true), new ProvinceMenuItem(2, "Look Around", ProvinceAction.Look, true)],
            menu.Items);
        Assert.Equal((3, "Dungeon", true, "Leazas Army"), (menu.Return, menu.Place, menu.CancelReopens, menu.Army));
        var first = ProvinceMenu.For(s, Items("Cｼﾊｲﾄｼ.ADV", 41, 502), ["○Temporary Levy", "Return"], Text)!;
        Assert.Equal((null, false), (first.Place, first.CancelReopens));
        Assert.Null(ProvinceMenu.For(s, Items("Cｼﾊｲﾄｼ.ADV", 700, 762), ["a", "Return"], Text));
    }

    /// <summary>
    /// The armies' names read from the player's files through the presenter, as the game reads them: ours
    /// (<c>Cｼﾊｲﾄｼ.ADV:20</c>, a run that starts with its line break) and every other owner's, none unresolved
    /// (the "[Cｼﾊｲﾄｼ.ADV:21]" the owner saw, 2026-10-16). Skipped without the player's files.
    /// </summary>
    [ScenarioFact]
    public void Every_owners_army_name_resolves_from_the_players_files()
    {
        var missing = Path.Combine(Path.GetTempPath(), "kichikuou-no-data");
        var presenter = new GamePresenter(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(missing), new BoxGlyphs());
        var names = new List<string>();
        for (int owner = 1; owner <= 11; owner++)
        {
            var s = World(1, owner);
            presenter.Bind(s);
            var (file, lines) = owner == 1 ? ("Cｼﾊｲﾄｼ.ADV", new[] { 41, 502 }) : ("Cﾃｷﾄｼ2.ADV", new[] { 50, 55 });
            var items = Items(file, lines);
            presenter.OpenMenu(items);
            names.Add(presenter.ProvinceMenu!.Army);
            presenter.CloseMenu();
        }
        presenter.Bind(World(1, 1));
        presenter.OpenMenu(Items("Cｼﾊｲﾄｼ.ADV", 758, 762));
        Assert.Equal("Dungeon", presenter.ProvinceMenu!.Place);
        presenter.CloseMenu();
        Assert.Empty(presenter.Unresolved);
        Assert.Equal("Leazas Army", names[0]);
        Assert.All(names, name => Assert.False(string.IsNullOrWhiteSpace(name) || name.Contains('[')));
    }
}
