using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The options window (docs/ui/gallery.md, step 2): the player's settings, nothing written unless its controls are used.
/// The touch mode's buttons are drawn on the entries they stand on, in the touch pass (the map's, the message windows').
/// </summary>
internal static partial class GalleryEntries
{
    private static IEnumerable<GalleryEntry> Controls()
    {
        foreach (var tab in Enum.GetValues<OptionsView.Tab>())
        {
            yield return new($"options-{tab.ToString().ToLowerInvariant()}", "Settings", $"Options: {tab}", Week18,
                "The player's settings as they are; nothing is written unless a control is used.", g => Options(g, tab));
        }
    }

    /// <summary>The options window on <paramref name="tab"/> over the map, with a message skip of its own that records nothing.</summary>
    private static Action<Rect2>? Options(GalleryStage g, OptionsView.Tab tab)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var backdrop = g.Backdrop(s);
        var view = g.Add(new OptionsView(new MessageSkip([]), folderLocked: GameData.UsesExtract, tab));
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area);
        };
    }
}
