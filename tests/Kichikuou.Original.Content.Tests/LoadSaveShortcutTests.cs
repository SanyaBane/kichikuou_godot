using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="LoadSaveShortcut"/>: our panel's Load and Save rows answer <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s Load / Save / Don't menu.</summary>
public class LoadSaveShortcutTests
{
    private static IReadOnlyList<SceneMenuItem> Items(string file, params int[] lines) =>
        [.. lines.Select(line => new SceneMenuItem(new ScriptRef(file, line), [new CapturedText(new ScriptRef(file, line + 1), [], [])]))];

    private static readonly IReadOnlyList<SceneMenuItem> Menu = Items("ﾛｰﾄﾞｾｰﾌﾞ.ADV", 91, 95, 99);

    [Theory]
    [InlineData(SaveList.Load, 1)]
    [InlineData(SaveList.Save, 2)]
    public void A_row_opens_its_list_then_leaves_with_Dont(SaveList list, int answer)
    {
        var shortcut = new LoadSaveShortcut();
        shortcut.Start(list);

        Assert.Equal(answer, shortcut.Choose(Menu));
        Assert.True(shortcut.Running);
        Assert.Equal(3, shortcut.Choose(Menu));                        // the list left: Don't, back to the map
        Assert.False(shortcut.Running);
        Assert.Null(shortcut.Choose(Menu));                             // the original's menu again
    }

    [Fact]
    public void Other_menus_and_other_answers_are_left_alone()
    {
        var shortcut = new LoadSaveShortcut();
        Assert.Null(shortcut.Choose(Menu));                             // no row answered

        shortcut.Start(SaveList.Save);
        Assert.Null(shortcut.Choose(Items("ﾛｰﾄﾞｾｰﾌﾞ.ADV", 122, 133)));  // the slot list is the player's
        Assert.Null(shortcut.Choose(Items("COMMAND.ADV", 91, 95, 99)));

        shortcut.Start(null);                                           // another row of the panel
        Assert.Null(shortcut.Choose(Menu));
    }

    [Fact]
    public void A_tab_answers_the_menu_with_the_other_list()
    {
        var shortcut = new LoadSaveShortcut();
        shortcut.Switch(SaveList.Load);                                 // no list under way: nothing to switch
        Assert.False(shortcut.Running);

        shortcut.Start(SaveList.Save);
        Assert.Equal(2, shortcut.Choose(Menu));
        shortcut.Switch(SaveList.Load);                                 // our screen's Load tab: the save list left with 0
        Assert.Equal(1, shortcut.Choose(Menu));
        Assert.Equal(3, shortcut.Choose(Menu));                         // the load list left: Don't
        Assert.False(shortcut.Running);
    }

    [Fact]
    public void A_load_ends_it()
    {
        var shortcut = new LoadSaveShortcut();
        shortcut.Start(SaveList.Load);
        Assert.Equal(1, shortcut.Choose(Menu));

        shortcut.Stop();                                                // "Load Complete": the save screen closed
        Assert.Null(shortcut.Choose(Menu));
    }
}
