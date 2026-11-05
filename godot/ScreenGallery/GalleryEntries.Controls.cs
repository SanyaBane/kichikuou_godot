using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;

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
        foreach (var tab in new[] { OptionsView.Tab.Messages, OptionsView.Tab.Display, OptionsView.Tab.Battle, OptionsView.Tab.Tweaks })
        {
            yield return new($"options-{tab.ToString().ToLowerInvariant()}-end", "Settings", $"Options: {tab} at its end", Week18,
                "Every tab one column, scrolled (owner, 2026-11-03): this one scrolled to its end, where it scrolls; nothing is "
                + "written unless a control is used.", g => Options(g, tab, end: true));
        }
        yield return new("options-defaults", "Settings", "Options: Defaults asking", Week18,
            "Defaults asks first, the same words on every tab (owner, 2026-10-31), here over Messages, whose preview hides "
            + "under it; nothing is reset unless Reset is used.", g => Options(g, OptionsView.Tab.Messages, askDefaults: true));
        if (!Device.TouchLayout)                    // the screen margins are the touch layout's only: not in the PC's pass
            yield break;
        yield return new("screen-margins", "Settings", "Screen margins: the calibration", Week18,
            "Option → Display → Screen margins' Adjust… (docs/ui/screen-margins.md, C) at 2.8 mm all round, over the map's "
            + "plates laid out inside them.", g => Margins(g, calibration: true));
        yield return new("map-margins", "Settings", "Screen margins: the map inside them", Week18,
            "The map's plates and controls laid out inside screen margins of 2.8 mm all round (docs/ui/screen-margins.md).",
            g => Margins(g, calibration: false));
    }

    /// <summary>
    /// The map's plates (<c>map-markers</c>) laid out inside screen margins held at 2.8 mm all round for the entry
    /// (<see cref="ScreenMarginsChoice.Show"/>, never saved; every entry starts at none; the stage is inset as the game's
    /// room is), with the calibration over the whole stage or not.
    /// </summary>
    private static Action<Rect2>? Margins(GalleryStage g, bool calibration)
    {
        ScreenMarginsChoice.Show(new ScreenMargins(28, 28, 28));    // before the map's views are laid out
        var place = Markers(g, hideDimmed: false, hover: null);
        if (place is not null && calibration)
            g.Over(new ScreenMarginsView());
        return place;
    }

    /// <summary>
    /// The options window on <paramref name="tab"/> over the map, with a message skip of its own that records nothing;
    /// with <paramref name="askDefaults"/> Defaults' question over it, with <paramref name="end"/> the tab scrolled to its end.
    /// </summary>
    private static Action<Rect2>? Options(GalleryStage g, OptionsView.Tab tab, bool askDefaults = false, bool end = false)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var backdrop = g.Backdrop(s);
        var view = g.Add(new OptionsView(new MessageSkip([]), folderLocked: GameData.UsesExtract, tab));
        if (askDefaults)
            view.AskDefaults();
        if (end)
            view.ScrollToEnd();
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area);
        };
    }
}
