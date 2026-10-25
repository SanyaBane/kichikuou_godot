using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The touch mode's buttons and the options window (docs/ui/gallery.md, step 2). The buttons are drawn whatever the
/// launch's mode, as the touch mode shows them; the options window shows the player's settings and writes nothing
/// unless its controls are used.
/// </summary>
internal static partial class GalleryEntries
{
    private static IEnumerable<GalleryEntry> Controls()
    {
        yield return new("touch-skip", "Touch", "Skip and Log drawer at a key wait", null,
            "The window's lines are the gallery's own.", g => Drawer(g, MessageKind.SpeechRight, skipOn: false));
        yield return new("touch-skip-on", "Touch", "Skip and Log drawer, MsgSkip on", null,
            "The window's lines are the gallery's own.", g => Drawer(g, MessageKind.SpeechRight, skipOn: true));
        yield return new("touch-skip-docked", "Touch", "Skip and Log docked beside a 131 bar", null,
            "Over 131 the window is drawn smaller beside the drawer's dock (MessageWindowView.Docks).",
            g => Drawer(g, MessageKind.CgBar, skipOn: false));
        yield return new("touch-commands", "Touch", "Commands button on the map", Week18, "",
            g => CommandsButtonOver(g, panel: false));
        yield return new("touch-commands-close", "Touch", "Commands button as Close over the panel", Week18, "",
            g => CommandsButtonOver(g, panel: true));
        foreach (var tab in Enum.GetValues<OptionsView.Tab>())
        {
            yield return new($"options-{tab.ToString().ToLowerInvariant()}", "Settings", $"Options: {tab}", Week18,
                "The player's settings as they are; nothing is written unless a control is used.", g => Options(g, tab));
        }
    }

    /// <summary>A key wait's window of <paramref name="kind"/> and the touch mode's drawer at the screen's left edge, or on its dock.</summary>
    private static Action<Rect2>? Drawer(GalleryStage g, MessageKind kind, bool skipOn)
    {
        var sample = GalleryStates.Messages.First(m => m.Kind == kind);
        var window = sample.Window();
        var view = g.Add(new MessageWindowView(g.Font) { Docks = true });
        view.Show(window, true, null, null);
        view.Complete();
        var drawer = g.Add(new SkipDrawer { Visible = true, SkipOn = skipOn });
        bool docked = SkipButtons.WindowScale(window.Panel) < 1;
        return area =>
        {
            view.Place(area);
            drawer.Place(area, docked ? SkipButtons.Dock(window.Panel) : SkipButtons.Drawer, docked);
        };
    }

    /// <summary>The map's Commands button, or over our command panel its Close.</summary>
    private static Action<Rect2>? CommandsButtonOver(GalleryStage g, bool panel)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var backdrop = g.Backdrop(s);
        CommandMenuView? commands = null;
        if (panel)
        {
            var p = g.Bound(s);
            commands = g.Add(new CommandMenuView(g.Font));
            commands.Open(Opened(p, CommandPhase.MenuItems(s), q => q.CommandMenu, "COMMAND's menu"));
        }
        var button = g.Add(new CommandsButton { Visible = true, Closes = panel });
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            commands?.Place(area);
            button.Place(area);
        };
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
