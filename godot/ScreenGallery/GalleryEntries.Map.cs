using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;
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
        yield return new("map", Map, "Map: the backdrop alone", Week18,
            "The map wait's picture the map's entries stand on, nothing of ours over it: for mock-ups (tools/ui/mock_*.py).",
            Backdrop);
        yield return new("map-markers", Map, "Map: the plates", Week18, "The dimmed plates shown (Debug → Hide Dimmed Map Names off).",
            g => Markers(g, hideDimmed: false, hover: null));
        yield return new("map-markers-hover", Map, "Map: a province hovered", Week18, "Leazas Castle (1) hovered, its card beside its plate.",
            g => Markers(g, hideDimmed: false, hover: LeazasCastle));
        yield return new("map-markers-active", Map, "Map: the dimmed plates hidden", Week18, "Debug → Hide Dimmed Map Names on (the default).",
            g => Markers(g, hideDimmed: true, hover: null));
        yield return new("map-north", Map, "Map: the room under the strip at the map's top", Week18,
            "The view at the map's top edge (GalleryStates.MapNorth): the picture drawn lower by the strip's height, the Great "
            + "Wall of Helman A (49) and the Tower of Evil (78) under it (docs/ui/command-screen.md, \"The map's room under the strip\").",
            g => Markers(g, hideDimmed: false, hover: null, GalleryStates.MapNorth));
        yield return new("command-menu", Map, "Command panel and status strip", Week18, "",
            g => CommandPanel(g, _ => { }));
        yield return new("command-menu-spent", Map, "Command panel: the week's commands spent", Week18,
            "Made up: Construction, Harem, Merchant and Subordinates spent at once (GalleryStates.CommandsSpent).",
            g => CommandPanel(g, GalleryStates.CommandsSpent));
        yield return new("command-menu-building", Map, "Command panel: a building under way", Week18,
            "Made up: the Military Academy under way, 5 weeks left (GalleryStates.AcademyUnderWay).",
            g => CommandPanel(g, GalleryStates.AcademyUnderWay));
        yield return new("command-menu-building-tip", Map, "Status strip: the building's tooltip", Week18,
            "Made up: the Military Academy under way, 5 weeks left; the tooltip as the pointer over Construction's tag shows it "
            + "(on the phone held landscape on the map's wait, where the right side shows the tags).",
            g => PhoneHeld ? Markers(g, hideDimmed: true, hover: null, GalleryStates.AcademyUnderWay, tip: true)
                : CommandPanel(g, GalleryStates.AcademyUnderWay, tip: true));
        yield return new("province-menu", Map, "Province menu: an enemy's, Oaks (6)", Week18, "",
            g => Province(g, Oaks));
        yield return new("province-menu-own", Map, "Province menu: our own, Leazas Castle (1)", Week18, "",
            g => Province(g, LeazasCastle));
        yield return new("province-menu-own-five", Map, "Province menu: Leazas Castle (1) with two buildings", Week18,
            "Made up: the Military Academy and the Genius Hospital built (GalleryStates.CastleBuildings), so the castle has five commands.",
            g => Province(g, LeazasCastle, GalleryStates.CastleBuildings));
        yield return new("province-menu-own-all", Map, "Province menu: Leazas Castle (1) with every command", Week18,
            "Made up: all 15 commands the castle's menu can hold at once, and Return (GalleryStates.CastleEveryCommand).",
            g => Province(g, LeazasCastle, GalleryStates.CastleEveryCommand));
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
        yield return new("dungeon-entry", Map, "Dungeon: the search's menu", Week18,
            "The Monsters' Cave (dungeon 2), unsearched: Start Search and Return (Cﾒｲｷｭｳ.ADV:238-305) over its picture.",
            g => DungeonEntry(g, "Monsters' Cave", "(Unsearched)", 267, 295));
        yield return new("dungeon-entry-more", Map, "Dungeon: the search's menu, four items", Week18,
            "Made up: the Ancient Ruins (dungeon 30) in progress, Merim to bring and God's Door found (the picture is "
            + "the ruins' own, 1032).",
            g => DungeonEntry(g, "Ancient Ruins", "(Search in Progress)", 267, 272, 290, 295));
    }

    /// <summary>
    /// Our plates over the map and our status strip over its top, as <c>COMMAND</c>'s map wait shows them,
    /// <paramref name="hover"/>'s province hovered, the picture drawn lower near the map's top (<see cref="MapHeadroom"/>); in
    /// the touch mode the Commands button and the Skip and Log drawer with them, Log lit as with something logged; with
    /// <paramref name="tip"/> the strip's building tooltip shown.
    /// </summary>
    private static Action<Rect2>? Markers(GalleryStage g, bool hideDimmed, int? hover, Action<GameState>? edit = null, bool tip = false)
    {
        if (g.State(Week18) is not { } s)
            return null;
        edit?.Invoke(s);
        var backdrop = g.Backdrop(s);
        var p = g.Bound(s);
        var corner = Room(backdrop, p);
        var markers = p.MapMarkers();
        var map = PortraitMap(g, s, p);
        var view = g.Add(new MapMarkersView(g.Font) { Visible = true });
        view.SetCoveredTop(PhoneHeld ? 0 : MapHeadroom.Strip);              // the phone's status is in the sides or on top
        view.SetHideDimmed(hideDimmed);
        view.Load(markers);
        var bar = Strip(g, p);
        if (tip)
            bar.ShowBuildingTip();
        var button = TouchCommandsButton(g, closes: false);
        var drawer = TouchMode.On ? g.Add(new SkipDrawer { Visible = true }) : null;  // Skip and Log on the waiting map (the owner's L1)
        var menu = MenuCell(g);                                 // the phone held landscape: Menu in the left side (LM1)
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            bar.Place(StripArea(area));
            if (map is not null && PlacePortrait(map, bar) is var (box, scale, width))
                view.Place(box, scale, map.Corner, (width, PhoneMapPortrait.MapHeight));
            else
                view.Place(area, area.Size.X / Surface.ScreenWidth, corner);
            button?.Place(area);
            drawer?.Place(area);
            menu?.Place();
            if (hover is { } province)
                view.ShowHover(markers.FirstOrDefault(m => m is { Kind: MapHitKind.Province } && m.Id == province));
        };
    }

    /// <summary>
    /// The phone held upright (docs/ui/phone-build.md, step P2): the map's whole height under the status band, our own view
    /// of it centred on the script's, as <c>GameScreen</c> shows it under its plates and menus; null elsewhere.
    /// </summary>
    private static PortraitMapView? PortraitMap(GalleryStage g, GameState s, GamePresenter p)
    {
        if (Device.Layout != DeviceLayout.PhonePortrait)
            return null;
        var map = g.Add(new PortraitMapView(g.Faces));
        map.Show(s, p.MapViewShown ?? throw new InvalidOperationException("The map was not shown."));
        return map;
    }

    /// <summary>
    /// The upright map laid under <paramref name="bar"/>'s band (placed already); returns the map's box, its scale and the
    /// columns it shows, where the plates go; null off an upright phone.
    /// </summary>
    private static (Rect2 Box, float Scale, int Width)? PlacePortrait(PortraitMapView map, StatusStripView bar)
    {
        if (Device.Portrait(map) is not { } f)
            return null;
        map.Place(bar.PortraitBand(f));
        var view = map.View;
        return (new Rect2(view.Box.X, view.Box.Y, view.Box.W, view.Box.H), view.Scale, (int)MathF.Ceiling(view.Width));
    }

    /// <summary>
    /// The map's room under the strip as <c>GameScreen</c> gives it: the backdrop drawn lower by the view's
    /// <see cref="MapHeadroom.Shift"/> (none on the phone, its status in the sides or on top); returns the corner the
    /// picture is drawn at, which our plates and menus are placed from.
    /// </summary>
    private static (int X, int Y) Room(TextureRect backdrop, GamePresenter p)
    {
        var (x, y) = p.MapViewShown ?? throw new InvalidOperationException("The map was not shown.");
        int shift = PhoneHeld ? 0 : MapHeadroom.Shift(y);
        var material = MapRoomPicture.Material();
        MapRoomPicture.SetShift(material, shift);
        backdrop.Material = material;
        return (x, y - shift);
    }

    /// <summary>
    /// Our status strip for the presenter's state, as the screen shows it over <c>COMMAND</c>'s map; on the phone held
    /// landscape in the sides, holding what <paramref name="mode"/> leaves them.
    /// </summary>
    private static StatusStripView Strip(GalleryStage g, GamePresenter p, StatusStripView.SideMode mode = StatusStripView.SideMode.Map)
    {
        var bar = g.Add(new StatusStripView(g.Font, g.Faces) { Mode = mode });
        bar.Show(p.StatusStrip ?? throw new InvalidOperationException("No status strip."));
        return bar;
    }

    /// <summary>The strip's place over the picture's top (<c>GameScreen.UpdateStatusStrip</c>).</summary>
    private static Rect2 StripArea(Rect2 area) =>
        new(area.Position, new Vector2(area.Size.X, StatusStripView.Height * area.Size.X / Surface.ScreenWidth));

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

    /// <summary>The map alone, as under the map's other entries.</summary>
    private static Action<Rect2>? Backdrop(GalleryStage g)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var backdrop = g.Backdrop(s);
        return area => GalleryStage.Fill(backdrop, area);
    }

    /// <summary>
    /// Our command panel over the map with the status strip over its top, as a right click on the map opens it; with
    /// <paramref name="tip"/> the strip's building tooltip shown.
    /// </summary>
    private static Action<Rect2>? CommandPanel(GalleryStage g, Action<GameState> edit, bool tip = false)
    {
        if (g.State(Week18) is not { } s)
            return null;
        edit(s);
        var backdrop = g.Backdrop(s);
        var p = g.Bound(s);
        Room(backdrop, p);
        var menu = Opened(p, CommandPhase.MenuItems(s), q => q.CommandMenu, "COMMAND's menu");
        var map = PortraitMap(g, s, p);
        var panel = g.Add(new CommandMenuView(g.Font));
        panel.Open(menu);
        var button = TouchCommandsButton(g, closes: true);
        var bar = Strip(g, p, StatusStripView.SideMode.Commands);
        if (tip)
            bar.ShowBuildingTip();
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            bar.Place(StripArea(area));
            if (map is not null && PlacePortrait(map, bar) is not null && Device.Portrait(map) is { } f)
                panel.PortraitBand = bar.PortraitBand(f);
            panel.Place(area);
            button?.Place(area);
        };
    }

    /// <summary>Our province menu for <paramref name="province"/> over the map, as a click on it opens it, our status strip over the map's top.</summary>
    private static Action<Rect2>? Province(GalleryStage g, int province, Action<GameState>? edit = null)
    {
        if (g.State(Week18) is not { } s)
            return null;
        edit?.Invoke(s);
        var backdrop = g.Backdrop(s);
        var items = GalleryStates.ProvinceMenuItems(s, province);
        var p = g.Bound(s);
        var corner = Room(backdrop, p);
        var menu = Opened(p, items, q => q.ProvinceMenu, "a city's first menu");
        var map = PortraitMap(g, s, p);
        var view = g.Add(new ProvinceMenuView(g.Font, g.Faces) { Map = map });
        view.Open(menu, ProvinceMenuView.PointOf(s, province, corner), ProvinceMenuView.MapPointOf(s, province));
        var bar = Strip(g, p, StatusStripView.SideMode.Province);
        var cell = map is null ? MenuCell(g) : null;            // the phone held landscape: Menu in the left side (LM1c); upright none (PM1c)
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            bar.Place(StripArea(area));
            if (map is not null && PlacePortrait(map, bar) is not null && Device.Portrait(map) is { } f)
                view.PortraitBand = bar.PortraitBand(f);
            view.Place(area);
            cell?.Place();
        };
    }

    /// <summary>The castle's cells' menu over the map, as our province menu shows it after Dungeon.</summary>
    private static Action<Rect2>? Cells(GalleryStage g, Action<GameState> edit)
    {
        if (g.State(Week18) is not { } s)
            return null;
        edit(s);
        var backdrop = g.Backdrop(s);
        var p = g.Bound(s);
        Room(backdrop, p);
        var menu = Opened(p, GalleryStates.CellsMenuItems(s), q => q.ProvinceMenu, "the cells' menu");
        var map = PortraitMap(g, s, p);
        var view = g.Add(new ProvinceMenuView(g.Font, g.Faces) { Map = map });
        view.Open(menu);
        var bar = Strip(g, p, StatusStripView.SideMode.Province);
        var cell = map is null ? MenuCell(g) : null;            // the phone held landscape: Menu in the left side (LM1c); upright none (PM1c)
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            bar.Place(StripArea(area));
            if (map is not null && PlacePortrait(map, bar) is not null && Device.Portrait(map) is { } f)
                view.PortraitBand = bar.PortraitBand(f);
            view.Place(area);
            cell?.Place();
        };
    }

    /// <summary>
    /// The dungeon's own menu before a search (<c>Cﾒｲｷｭｳ.ADV:238-305</c>, <c>L_0076a</c>): the picture (<c>WAO_SUB</c> 331)
    /// over the map, window 113 with the dungeon's <paramref name="name"/> and <paramref name="state"/>, the items registered
    /// at <paramref name="lines"/> — our <c>]</c> menu at the boot's selection box, in the touch mode the rail in the lower
    /// right corner (docs/ui/dungeon-menu.md, §6).
    /// </summary>
    private static Action<Rect2>? DungeonEntry(GalleryStage g, string name, string state, params int[] lines)
    {
        if (g.State(Week18) is not { } s)
            return null;
        GalleryStates.DungeonFloor(s, GalleryStates.Cave, 1);
        var backdrop = g.Backdrop(s, new WindowEvent(331, s.CallArg));
        var window = g.Add(new MessageWindowView(g.Font) { Docks = TouchMode.On });
        var sample = MessageWindow.SampleOf(MessageKind.Narration, [$"[{name}]", "", state]);
        window.Show(sample, false, null, null);
        window.Complete();
        var p = g.Bound(s);
        var items = lines.Select(Item).ToList();
        if (TouchMode.On)
        {
            var entry = Opened(p, items, q => q.DungeonEntry, "the menu before a search");
            var rail = g.Add(new FloorLadderView(g.Font) { Screen = backdrop.Texture, Window = sample });   // held upright it draws them (PF1e)
            rail.OpenEntry(entry);
            return area =>
            {
                GalleryStage.Fill(backdrop, area);
                window.Place(area);
                rail.Place(area);
            };
        }
        p.Show(GalleryStates.SelectionWindow);
        p.OpenMenu(items);
        MenuView menu;
        (int X, int Y, int W, int H) box;
        try
        {
            menu = p.Menu ?? throw new InvalidOperationException("No menu.");
            box = p.MenuBoxArea ?? throw new InvalidOperationException("No selection box.");
        }
        finally
        {
            p.CloseMenu();
        }
        var view = g.Add(new SceneMenuView(g.Font));
        view.Open(menu, box);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            window.Place(area);
            view.Place(area);
        };

        // A menu item of the dungeon's file registered at `line`, its text on the next line (CommandFile.MenuItemOf).
        static SceneMenuItem Item(int line) =>
            new(new ScriptRef("Cﾒｲｷｭｳ.ADV", line), [new CapturedText(new ScriptRef("Cﾒｲｷｭｳ.ADV", line + 1), [], [])]);
    }

    /// <summary>
    /// Our floor ladder on <paramref name="floor"/> as the floor's header leaves the screen (<c>Cﾒｲｷｭｳ.ADV:669-699</c>): the
    /// screen cleared to black (<c>EC 0</c>), the dungeon's picture (<c>WAO_SUB</c> 331) and window 113 under it with the
    /// dungeon, the floor and the searcher's unit (the gallery's own lines).
    /// </summary>
    private static Action<Rect2>? Ladder(GalleryStage g, int floor)
    {
        if (g.State(Week18) is not { } s)
            return null;
        GalleryStates.DungeonFloor(s, GalleryStates.Cave, floor);
        var backdrop = g.Backdrop(s, new EngineEvent("EC", [0]), new WindowEvent(331, s.CallArg));
        var header = g.Add(new MessageWindowView(g.Font) { Docks = TouchMode.On });
        var lines = MessageWindow.SampleOf(MessageKind.Narration,
            ["Monsters' Cave", $"  {(int)s.DgnRun_Floor}F", "Rance Unit", $" ({(int)s.Units[1].Troops})"]);
        header.Show(lines, false, null, null);
        header.Complete();
        var ladder = Opened(g.Bound(s), DungeonCommand.FloorMenuItems(s), q => q.FloorLadder, "the floor menu");
        var view = g.Add(new FloorLadderView(g.Font) { Screen = backdrop.Texture, Window = lines });   // held upright it draws them (PF1)
        view.Open(ladder);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            header.Place(area);
            view.Place(area);
        };
    }
}
