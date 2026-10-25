using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The map's entries (docs/ui/gallery.md, step 2): the plates, the command panel with the status strip, the province
/// menus and the dungeon's floor ladder, each over the map the presenter draws for the save's state. A menu's items are
/// the ones its file registers for that state (<c>CommandPhase.MenuItems</c>, the city files' and the floor menu's),
/// opened in the presenter, which makes our menu of them as it does in a game.
/// </summary>
internal static partial class GalleryEntries
{
    private const string Map = "Map";

    /// <summary>The week's attack target on the week-18 save: Oaks (6), the rebels' (CLAUDE.md, "Commands").</summary>
    private const int Oaks = 6;

    private const int LeazasCastle = 1;

    private static IEnumerable<GalleryEntry> MapScreens()
    {
        yield return new("map-markers", Map, "Map: the plates", Week18, "The dimmed plates shown (Debug → Hide Dimmed Map Names off).",
            g => Markers(g, hideDimmed: false, hover: null));
        yield return new("map-markers-hover", Map, "Map: a province hovered", Week18, "Leazas Castle (1) hovered, its card beside its plate.",
            g => Markers(g, hideDimmed: false, hover: LeazasCastle));
        yield return new("map-markers-active", Map, "Map: the dimmed plates hidden", Week18, "Debug → Hide Dimmed Map Names on (the default).",
            g => Markers(g, hideDimmed: true, hover: null));
        yield return new("command-menu", Map, "Command panel and status strip", Week18, "",
            g => CommandPanel(g, _ => { }));
        yield return new("command-menu-spent", Map, "Command panel: the week's commands spent", Week18,
            "Made up: Construction, Harem, Merchant and Subordinates spent at once (GalleryStates.CommandsSpent).",
            g => CommandPanel(g, GalleryStates.CommandsSpent));
        yield return new("province-menu", Map, "Province menu: an enemy's, Oaks (6)", Week18, "",
            g => Province(g, Oaks));
        yield return new("province-menu-own", Map, "Province menu: our own, Leazas Castle (1)", Week18, "",
            g => Province(g, LeazasCastle));
        yield return new("province-menu-own-five", Map, "Province menu: Leazas Castle (1) with two buildings", Week18,
            "Made up: the Military Academy and the Genius Hospital built (GalleryStates.CastleBuildings), so the castle has five commands.",
            g => Province(g, LeazasCastle, GalleryStates.CastleBuildings));
        yield return new("province-menu-cells", Map, "The castle's cells", Week18,
            "Dungeon from Leazas Castle (1): the prisoners the save holds, Look Around, Return.",
            g => Cells(g, _ => { }));
        yield return new("province-menu-cells-full", Map, "The castle's cells: four prisoners", Week18,
            "Made up: Lelyukov, Ruberan, Papaya and the Lady held at once (GalleryStates.PrisonersHeld).",
            g => Cells(g, GalleryStates.PrisonersHeld));
        yield return new("floor-ladder", Map, "Floor ladder: the first floor", Week18,
            "The Monsters' Cave (dungeon 2) on floor 1: no Ascend, its place kept empty.",
            g => Ladder(g, 1));
        yield return new("floor-ladder-middle", Map, "Floor ladder: a middle floor", Week18,
            "The Monsters' Cave (dungeon 2) on floor 7: both ways.",
            g => Ladder(g, 7));
        yield return new("floor-ladder-last", Map, "Floor ladder: the last floor", Week18,
            "The Monsters' Cave (dungeon 2) on floor 14, its last: no Descend.",
            g => Ladder(g, 14));
    }

    /// <summary>Our plates over the map, as <c>COMMAND</c>'s map wait shows them, <paramref name="hover"/>'s province hovered.</summary>
    private static Action<Rect2>? Markers(GalleryStage g, bool hideDimmed, int? hover)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var backdrop = g.Backdrop(s);
        var p = g.Bound(s);
        var corner = p.MapViewShown ?? throw new InvalidOperationException("The map was not shown.");
        var markers = p.MapMarkers();
        var view = g.Add(new MapMarkersView(g.Font) { Visible = true });
        view.SetHideDimmed(hideDimmed);
        view.Load(markers);
        var button = TouchCommandsButton(g, closes: false);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area, area.Size.X / Surface.ScreenWidth, corner);
            button?.Place(area);
            if (hover is { } province)
                view.ShowHover(markers.FirstOrDefault(m => m is { Kind: MapHitKind.Province } && m.Id == province));
        };
    }

    /// <summary>
    /// The touch mode's Commands button, which GameScreen shows with the plates and as Close over our command panel; none
    /// outside the touch mode.
    /// </summary>
    private static CommandsButton? TouchCommandsButton(GalleryStage g, bool closes) =>
        TouchMode.On ? g.Add(new CommandsButton { Visible = true, Closes = closes }) : null;

    /// <summary>
    /// The menu <paramref name="items"/> make, opened in the presenter and read as our menu by <paramref name="read"/>, then
    /// closed again (the presenter is the gallery's, shared by its entries).
    /// </summary>
    private static T Opened<T>(GamePresenter p, IReadOnlyList<SceneMenuItem> items, Func<GamePresenter, T?> read, string what)
        where T : class
    {
        p.OpenMenu(items);
        try
        {
            return read(p) ?? throw new InvalidOperationException($"The items are not {what}.");
        }
        finally
        {
            p.CloseMenu();
        }
    }

    /// <summary>Our command panel over the map with the status strip over its top, as a right click on the map opens it.</summary>
    private static Action<Rect2>? CommandPanel(GalleryStage g, Action<GameState> edit)
    {
        if (g.State(Week18) is not { } s)
            return null;
        edit(s);
        var backdrop = g.Backdrop(s);
        var p = g.Bound(s);
        var menu = Opened(p, CommandPhase.MenuItems(s), q => q.CommandMenu, "COMMAND's menu");
        var strip = p.StatusStrip ?? throw new InvalidOperationException("No status strip.");
        var panel = g.Add(new CommandMenuView(g.Font));
        panel.Open(menu);
        var button = TouchCommandsButton(g, closes: true);
        var bar = g.Add(new StatusStripView(g.Font, g.Faces));
        bar.Show(strip);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            panel.Place(area);
            button?.Place(area);
            bar.Place(new Rect2(area.Position, new Vector2(area.Size.X, StatusStripView.Height * area.Size.X / Surface.ScreenWidth)));
        };
    }

    /// <summary>Our province menu for <paramref name="province"/> over the map, as a click on it opens it.</summary>
    private static Action<Rect2>? Province(GalleryStage g, int province, Action<GameState>? edit = null)
    {
        if (g.State(Week18) is not { } s)
            return null;
        edit?.Invoke(s);
        var backdrop = g.Backdrop(s);
        var items = GalleryStates.ProvinceMenuItems(s, province);
        var menu = Opened(g.Bound(s), items, q => q.ProvinceMenu, "a city's first menu");
        var view = g.Add(new ProvinceMenuView(g.Font, g.Faces));
        view.Open(menu);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area);
        };
    }

    /// <summary>The castle's cells' menu over the map, as our province menu shows it after Dungeon.</summary>
    private static Action<Rect2>? Cells(GalleryStage g, Action<GameState> edit)
    {
        if (g.State(Week18) is not { } s)
            return null;
        edit(s);
        var backdrop = g.Backdrop(s);
        var menu = Opened(g.Bound(s), GalleryStates.CellsMenuItems(s), q => q.ProvinceMenu, "the cells' menu");
        var view = g.Add(new ProvinceMenuView(g.Font, g.Faces));
        view.Open(menu);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area);
        };
    }

    /// <summary>Our floor ladder right of the dungeon's picture (<c>WAO_SUB</c> 331 over the map), on <paramref name="floor"/>.</summary>
    private static Action<Rect2>? Ladder(GalleryStage g, int floor)
    {
        if (g.State(Week18) is not { } s)
            return null;
        GalleryStates.DungeonFloor(s, GalleryStates.Cave, floor);
        var backdrop = g.Backdrop(s, new WindowEvent(331, s.CallArg));
        var ladder = Opened(g.Bound(s), DungeonCommand.FloorMenuItems(s), q => q.FloorLadder, "the floor menu");
        var view = g.Add(new FloorLadderView(g.Font));
        view.Open(ladder);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area);
        };
    }
}
