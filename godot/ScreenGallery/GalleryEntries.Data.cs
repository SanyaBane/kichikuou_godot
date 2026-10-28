using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// All Data (<c>各種DATA</c>, <c>AllDataCommand</c>; docs/ui/gallery.md, §7 step 7): the screen the presenter draws by hand
/// from the save's state — the map in miniature with its markers, or a graph of the last 16 weeks — and our <c>]</c> menu
/// over it at the boot's selection box, as our interface shows them after the command panel's All Data.
/// </summary>
internal static partial class GalleryEntries
{
    private const string DataNote = "The save's own history: under xsystem35 every week before this one holds last week's figures, so their bars are level (STR-01).";

    private static IEnumerable<GalleryEntry> AllDataScreens()
    {
        yield return new("all-data", "Screens", "All Data: the menu over the map", Week18,
            "The map in miniature with its markers (各種DATA.ADV:1-11) and the menu of the four graphs.",
            g => AllData(g, DataView.Map, GalleryStates.AllDataMenu));
        yield return new("all-data-funds", "Screens", "All Data: All Funds", Week18,
            "The funds with the military and sundry spending stacked in each bar, at 16x. " + DataNote,
            g => AllData(g, DataView.Funds));
        yield return new("all-data-sundry", "Screens", "All Data: Sundry Expenses", Week18, DataNote,
            g => AllData(g, DataView.SundryExpenses));
        yield return new("all-data-military", "Screens", "All Data: Military Spending", Week18, DataNote,
            g => AllData(g, DataView.MilitarySpending));
        yield return new("all-data-casualties", "Screens", "All Data: All Casualties", Week18,
            "The casualties in tens, on their own scale. " + DataNote,
            g => AllData(g, DataView.Casualties));
        yield return new("all-data-scale", "Screens", "All Data: the scale menu", Week18,
            "A right click on All Funds: 1x to 128x (各種DATA.ADV:310-387).",
            g => AllData(g, DataView.Funds, GalleryStates.AllDataScaleMenu));
    }

    /// <summary>
    /// All Data on <paramref name="view"/> for the save's state as the file opens it (<see cref="GalleryStates.AllDataOpened"/>),
    /// composed by a presenter of the entry's own over the map <c>COMMAND</c> left on the stage, and our menu of
    /// <paramref name="items"/> over it where the game opens one.
    /// </summary>
    private static Action<Rect2>? AllData(GalleryStage g, DataView view, IReadOnlyList<SceneMenuItem>? items = null)
    {
        if (g.State(Week18) is not { } s)
            return null;
        GalleryStates.AllDataOpened(s);
        var p = g.Fresh();
        p.Bind(s);
        p.Show(new MapEvent(3));                                        // the tiles drawn, as COMMAND's map left them
        p.Show(new MapEvent(0));
        p.Show(GalleryStates.SelectionWindow);
        p.Show(new DataScreenEvent(view));
        SceneMenuView? menuView = null;
        if (items is not null)
        {
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
                p.CloseMenu();                                          // our menu draws it: the box is not composed
            }
            menuView = g.Add(new SceneMenuView(g.Font));
            menuView.Open(menu, box);
        }
        var picture = g.Picture(p);
        var cell = MenuCell(g);                                 // the phone held landscape: Menu in the left side (LG2m)
        return area =>
        {
            GalleryStage.Fill(picture, area);
            menuView?.Place(area);
            cell?.Place();
        };
    }
}
