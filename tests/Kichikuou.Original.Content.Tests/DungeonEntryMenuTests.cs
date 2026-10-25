using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="DungeonEntryMenu"/>: the dungeon's menu before a search as the touch rail shows it (docs/ui/dungeon-menu.md, §6).</summary>
public class DungeonEntryMenuTests
{
    private const string File = "Cﾒｲｷｭｳ.ADV";

    private static IReadOnlyList<SceneMenuItem> Items(string file, params int[] lines) =>
        [.. lines.Select(line => new SceneMenuItem(new ScriptRef(file, line), [new CapturedText(new ScriptRef(file, line + 1), [], [])]))];

    [Fact]
    public void AnUnsearchedDungeonOffersStartAndReturn()
    {
        var menu = DungeonEntryMenu.For(Items(File, 267, 295), ["Start Search", "Return"])!;
        Assert.Equal(1, menu.Start);
        Assert.Equal("Start Search", menu.StartText);
        Assert.Empty(menu.Others);
        Assert.Equal(2, menu.Return);
        Assert.Equal("Return", menu.ReturnText);
    }

    [Fact]
    public void TheOtherItemsKeepTheirPlaces()
    {
        var menu = DungeonEntryMenu.For(Items(File, 267, 272, 290, 295), ["Start Search", "Bring Merim", "God's Door", "Return"])!;
        Assert.Equal([new DungeonEntryItem(2, "Bring Merim"), new DungeonEntryItem(3, "God's Door")], menu.Others);
        Assert.Equal(4, menu.Return);
    }

    [Fact]
    public void OnlyTheMenuBeforeASearchIsOne()
    {
        Assert.Null(DungeonEntryMenu.For(Items(File, 884, 889), ["Descend", "Retreat"]));                       // the floor menu
        Assert.Null(DungeonEntryMenu.For(Items(File, 267), ["Start Search"]));
        Assert.Null(DungeonEntryMenu.For(Items(File, 295, 267), ["Return", "Start Search"]));
        Assert.Null(DungeonEntryMenu.For(Items(File, 267, 300, 295), ["Start Search", "x", "Return"]));         // an unknown item
        Assert.Null(DungeonEntryMenu.For(Items(File, 267, 272, 272, 295), ["Start Search", "a", "b", "Return"])); // twice
        Assert.Null(DungeonEntryMenu.For(Items("Cｼﾊｲﾄｼ.ADV", 267, 295), ["Start Search", "Return"]));
        Assert.Null(DungeonEntryMenu.For([], []));
    }
}
