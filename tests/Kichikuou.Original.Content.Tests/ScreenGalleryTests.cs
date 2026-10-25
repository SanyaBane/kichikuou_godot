using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

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
}
