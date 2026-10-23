using Kichikuou.Content.Screen;

namespace Kichikuou.Content.Tests;

/// <summary>The text box's read-before flags (docs/ui/read-text.md): kept with each character, gone with it.</summary>
public class TextBoxTests
{
    [Fact]
    public void The_box_is_read_when_every_message_in_it_was()
    {
        var box = new TextBox((0, 0, 400, 100));
        Assert.False(box.ReadBefore);                                           // empty

        box.Print("Rance", 24, 15);                                             // no message's: neither way
        Assert.False(box.ReadBefore);
        box.Print("「Gahaha!」", 24, 15, read: true);
        Assert.True(box.ReadBefore);
        box.Put(300, 2, 24, 15, '!', read: false);
        Assert.False(box.ReadBefore);

        box.Clear();
        Assert.False(box.ReadBefore);
        box.Print("Again", 24, 15, read: true);
        Assert.True(box.ReadBefore);
    }

    [Fact]
    public void A_wiped_character_takes_its_flag_with_it()
    {
        var box = new TextBox((0, 0, 400, 100));
        box.Print("Old", 24, 15, read: true);
        box.Put(300, 2, 24, 15, 'N', read: false);
        Assert.False(box.ReadBefore);

        box.Wipe(290, 0, 50, 40);                                               // the new character only
        Assert.Equal("Old", box.Text);
        Assert.True(box.ReadBefore);
    }
}
