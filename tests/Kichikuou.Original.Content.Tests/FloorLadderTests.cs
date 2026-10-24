using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="FloorLadder"/>: the dungeon's floor menu as our ladder shows it (docs/ui/dungeon-menu.md).</summary>
public class FloorLadderTests
{
    private const string File = "Cﾒｲｷｭｳ.ADV";

    private static GameState Floor(int floor) => new() { DgnRun_Floor = (Sys3Int)floor };

    private static IReadOnlyList<SceneMenuItem> Items(string file, params int[] lines) =>
        [.. lines.Select(line => new SceneMenuItem(new ScriptRef(file, line), [new CapturedText(new ScriptRef(file, line + 1), [], [])]))];

    [Fact]
    public void ACaveFirstFloorHasNoWayUp()
    {
        var ladder = FloorLadder.For(Floor(1), Items(File, 884, 889), ["Descend", "Retreat"])!;
        Assert.Null(ladder.Up);
        Assert.Equal(new FloorWay(1, "Descend", 2, true), ladder.Down);
        Assert.Equal(2, ladder.Retreat);
        Assert.Equal("Retreat", ladder.RetreatText);
        Assert.Equal(1, ladder.Floor);
    }

    [Fact]
    public void ACaveMiddleFloorGoesBothWays()
    {
        var ladder = FloorLadder.For(Floor(2), Items(File, 866, 884, 889), ["Ascend", "Descend", "Retreat"])!;
        Assert.Equal(new FloorWay(1, "Ascend", 1, false), ladder.Up);
        Assert.Equal(new FloorWay(2, "Descend", 3, true), ladder.Down);
        Assert.Equal(3, ladder.Retreat);
    }

    [Fact]
    public void ACaveLastFloorHasNoWayDown()
    {
        var ladder = FloorLadder.For(Floor(14), Items(File, 866, 889), ["Ascend", "Retreat"])!;
        Assert.Equal(new FloorWay(1, "Ascend", 13, false), ladder.Up);
        Assert.Null(ladder.Down);
        Assert.Equal(2, ladder.Retreat);
    }

    [Fact]
    public void ATowerGoesDeeperUp()
    {
        var first = FloorLadder.For(Floor(1), Items(File, 872, 889), ["Ascend", "Retreat"])!;
        Assert.Equal(new FloorWay(1, "Ascend", 2, true), first.Up);
        Assert.Null(first.Down);
        var middle = FloorLadder.For(Floor(5), Items(File, 872, 878, 889), ["Ascend", "Descend", "Retreat"])!;
        Assert.Equal(new FloorWay(1, "Ascend", 6, true), middle.Up);
        Assert.Equal(new FloorWay(2, "Descend", 4, false), middle.Down);
    }

    [Fact]
    public void OnlyTheFloorMenuIsALadder()
    {
        Assert.NotNull(FloorLadder.For(Floor(3), Items(File, 889), ["Retreat"]));                           // a one-floor dungeon
        Assert.Null(FloorLadder.For(Floor(3), Items(File, 934, 938), ["Finish", "Continue"]));               // Retreat's own question
        Assert.Null(FloorLadder.For(Floor(3), Items(File, 866, 900, 889), ["Ascend", "x", "Retreat"]));      // an unknown item
        Assert.Null(FloorLadder.For(Floor(3), Items(File, 866, 866, 889), ["Ascend", "Ascend", "Retreat"])); // two ways up
        Assert.Null(FloorLadder.For(Floor(3), Items("Cｼﾊｲﾄｼ.ADV", 866, 889), ["Ascend", "Retreat"]));
        Assert.Null(FloorLadder.For(Floor(3), [], []));
    }
}
