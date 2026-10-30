using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The scenes' entries (docs/ui/gallery.md, step 2): the week report fed with a week's start as <c>MAIN.ADV</c> prints
/// it, a scene's menu under its question, the message log.
/// </summary>
internal static partial class GalleryEntries
{
    private static IEnumerable<GalleryEntry> SceneScreens()
    {
        yield return new("week-report", "Messages", "Week report: a Leazas week", Week18,
            "Made up: the revenue (20,200K), the troops (1,385) and the AL Church's donation are the gallery's own; the save is past its week's start.",
            g => WeekReportOf(g, s => GalleryStates.LeazasWeekStart(s, 202, 1385, 201)));
        yield return new("week-report-thieves", "Messages", "Week report: the thieves' week", Week18,
            "The banner alone, as the thieves' week has it, with the save's date.",
            g => WeekReportOf(g, GalleryStates.ThievesWeekStart));
        yield return new("scene-menu", "Messages", "Scene menu under a speech (121)", Week18,
            "The question and its answers are the gallery's own; the menu at the boot's selection window (ｽﾀｰﾄ.ADV:860).",
            g => SceneMenu(g, MessageKind.SpeechRight));
        yield return new("message-log", "Messages", "Message log", Week18, "The pages are the gallery's own.", g =>
        {
            if (g.State(Week18) is not { } s)
                return null;
            var backdrop = g.Backdrop(s);
            var log = new MessageLog();
            foreach (var page in GalleryStates.LogSample)
                log.Add(page);
            var view = g.Add(new MessageLogView(g.Font, log));
            return area =>
            {
                GalleryStage.Fill(backdrop, area);
                view.Place(area);
            };
        });
    }

    /// <summary>Our week report over the map, gathered by the presenter from <paramref name="weekStart"/>'s events.</summary>
    private static Action<Rect2>? WeekReportOf(GalleryStage g, Func<Original.State.GameState, IEnumerable<ScriptEvent>> weekStart)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var backdrop = g.Backdrop(s);
        var p = g.Bound(s);
        WeekReport report;
        p.CoverWeekReport = true;
        try
        {
            foreach (var e in weekStart(s))
                p.Show(e);
            report = p.WeekReport ?? throw new InvalidOperationException("The events started no week report.");
        }
        finally
        {
            p.EndWeekReport();
            p.CoverWeekReport = false;
        }
        var view = g.Add(new WeekReportView(g.Faces));
        view.Open(report);
        // The phone held landscape: the left side as in the scenes (LW1), Log lit as a week's start has pages logged.
        var drawer = PhoneHeld ? g.Add(new SkipDrawer { Visible = true }) : null;
        var menu = MenuCell(g);
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            view.Place(area);
            drawer?.Place(area);
            menu?.Place();
        };
    }

    /// <summary>A speech asking a question and our menu of its answers at the selection box's place, as a scene shows them.</summary>
    private static Action<Rect2>? SceneMenu(GalleryStage g, MessageKind asking)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var p = g.Bound(s);
        p.Show(GalleryStates.SelectionWindow);
        p.OpenMenu([.. GalleryStates.MenuSample.Select((text, i) => SceneMenuItem.OfPort(new ScriptRef("gallery", i + 1), text))]);
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
        var window = g.Add(new MessageWindowView(g.Font) { Docks = TouchMode.On });
        var question = MessageWindow.SampleOf(asking, ["The rebels gather near Oaks.", "What shall we do, King Rance?"]);
        window.Show(question, false, null, null);
        window.Complete();
        var view = g.Add(new SceneMenuView(g.Font));
        view.Open(menu, box, question.Panel);
        var cell = MenuCell(g);             // the drawer hides under a menu, Menu stays (LS1m; upright in the bar, PS2m)
        return area =>
        {
            window.Place(area);
            view.Place(area);
            cell?.Place();
        };
    }
}
