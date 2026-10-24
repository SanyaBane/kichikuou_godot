using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="ConstructionShortcut"/>: our panel's Construction answers <c>Cﾃｲﾄ</c>'s Build / Don't menu.</summary>
public class ConstructionShortcutTests
{
    private static IReadOnlyList<SceneMenuItem> Items(string file, params int[] lines) =>
        [.. lines.Select(line => new SceneMenuItem(new ScriptRef(file, line), [new CapturedText(new ScriptRef(file, line + 1), [], [])]))];

    private static readonly IReadOnlyList<SceneMenuItem> Menu = Items("Cﾃｲﾄ.ADV", 62, 66);

    [Fact]
    public void Construction_builds_then_leaves_with_Dont()
    {
        var shortcut = new ConstructionShortcut();
        shortcut.Start(true);

        Assert.Equal(1, shortcut.Choose(Menu));                         // Build
        Assert.True(shortcut.Running);
        Assert.Equal(2, shortcut.Choose(Menu));                         // the list's Don't Build: Don't, back to the map
        Assert.False(shortcut.Running);
        Assert.Null(shortcut.Choose(Menu));                             // the original's menu again
    }

    [Fact]
    public void Other_menus_and_other_answers_are_left_alone()
    {
        var shortcut = new ConstructionShortcut();
        Assert.Null(shortcut.Choose(Menu));                             // no Construction answered

        shortcut.Start(true);
        Assert.Null(shortcut.Choose(Items("Cﾃｲﾄ.ADV", 246, 252, 282))); // the buildings' list is the player's
        Assert.Null(shortcut.Choose(Items("Cﾃｲﾄ.ADV", 442, 446)));      // so is Construct / Something Else
        Assert.Null(shortcut.Choose(Items("COMMAND.ADV", 62, 66)));

        shortcut.Start(false);                                          // another row of the panel
        Assert.Null(shortcut.Choose(Menu));
    }
}
