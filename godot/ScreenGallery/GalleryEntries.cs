using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// One screen of ours in one state (docs/ui/gallery.md, §3): <see cref="Id"/> names its dump (the name unattended play
/// dumps it by, where it does), <see cref="Save"/> the kept save its state starts from, <see cref="Note"/> what is made up
/// or missing. <see cref="Build"/> adds its views to the stage and returns how to place them over the picture's area
/// (the 1280×800 screen fitted), or null when it cannot be shown (its save is missing). <see cref="Settle"/>: seconds the
/// dump waits for a view that rises into place (a few frames otherwise).
/// </summary>
internal sealed record GalleryEntry(string Id, string Section, string Title, string? Save, string Note,
    Func<GalleryStage, Action<Rect2>?> Build, double Settle = 0);

/// <summary>The screen gallery's entries (docs/ui/gallery.md, §7: step 1 Treasure, Harem, Subordinates, the message windows; step 2 the map, GalleryEntries.Map.cs).</summary>
internal static partial class GalleryEntries
{
    /// <summary>
    /// The owner's game at week 18 in <c>COMMAND</c>: the kept save <c>saves-week18</c> played through the week's start and
    /// saved on the map (<c>--save-command</c>, docs/ui/gallery.md, §2), so the screens show the week's figures and uses.
    /// </summary>
    public const string Week18 = "saves-week18-command";

    public static IReadOnlyList<GalleryEntry> All() =>
    [
        .. Treasure(),
        .. Harem(),
        .. Subordinates(),
        .. DeploymentScreens(),
        .. SaveLoadScreens(),
        .. Messages(),
        .. MapScreens(),
        .. SceneScreens(),
        .. Controls(),
        .. BattleScreens(),
        .. BoardScreens(),
    ];

    /// <summary>Script pixels a screen pixel of the picture's area: the 640×400 script screen fitted into it.</summary>
    private static float PixelsPerScript(Rect2 area) => area.Size.X / 640f;

    // ---------------------------------------------------------------- Treasure (docs/ui/pickers.md, round V)

    private static IEnumerable<GalleryEntry> Treasure()
    {
        yield return new("treasure", "Screens", "Treasure: the characters", Week18, "",
            g => TreasureScreen(g, (s, view, _) => GalleryStates.TreasureView(s, 0)));
        yield return new("treasure-hover", "Screens", "Treasure: a holder hovered", Week18, "",
            g => TreasureScreen(g, (s, view, _) =>
            {
                GalleryStates.TreasureView(s, 0);
                view.ShowHover(FirstHolder(s));
            }));
        yield return new("treasure-items", "Screens", "Treasure: the items, one hovered", Week18, "",
            g => TreasureScreen(g, (s, view, _) =>
            {
                GalleryStates.TreasureView(s, 1);
                view.ShowHover(s.Units[FirstHolder(s)].Item.Value);
            }));
        yield return new("treasure-which-item", "Screens", "Treasure: a unit picked first", Week18, "",
            g => TreasureScreen(g, (s, _, _) => GalleryStates.TreasureUnitPicked(s, FirstHolder(s))));
        yield return new("treasure-replace", "Screens", "Treasure: our question before a replace", Week18, "",
            g => TreasureScreen(g, (s, view, callout) =>
            {
                var holders = GalleryStates.MovableHolders(s);
                if (holders.Count < 2)
                    throw new InvalidOperationException("The save has fewer than two holders of an item that moves.");
                GalleryStates.TreasureItemPicked(s, s.Units[holders[0]].Item.Value);
                var lines = TreasureRoster.ReplaceQuestion(s, holders[1])
                    ?? throw new InvalidOperationException("No question for the second holder.");
                callout.OpenMenu(["Yes", "No"]);
                return (lines, holders[1], false);
            }));
        yield return new("treasure-refusal", "Screens", "Treasure: the script's refusal", Week18,
            "The refusal's line is Sｱｲﾃﾑ.ADV:311's.",
            g => TreasureScreen(g, (s, view, callout) =>
            {
                int unit = FirstHolder(s);
                GalleryStates.TreasureUnitPicked(s, unit);
                return (["Already equipped."], s.Units[unit].Item.Value, true);
            }));
    }

    /// <summary>The first unit at status 1 or 2 holding an item that moves.</summary>
    private static int FirstHolder(GameState s) =>
        GalleryStates.MovableHolders(s) is [var first, ..] ? first
            : throw new InvalidOperationException("The save has no holder of an item that moves.");

    private static Action<Rect2>? TreasureScreen(GalleryStage g, Action<GameState, TreasureView, TreasureCallout> setUp) =>
        TreasureScreen(g, (s, view, callout) =>
        {
            setUp(s, view, callout);
            return null;
        });

    /// <summary>
    /// Our Treasure screen on the save's state as <paramref name="setUp"/> leaves it, and the callout it returns — its lines,
    /// the cell it is at, whether it waits for a click (a refusal) — at that cell, as <c>GameScreen.Treasure</c> shows it.
    /// </summary>
    private static Action<Rect2>? TreasureScreen(GalleryStage g,
        Func<GameState, TreasureView, TreasureCallout, (IReadOnlyList<string> Lines, int Cell, bool Waiting)?> setUp)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var view = g.Add(new TreasureView(g.Font, g.Faces) { Visible = true });
        view.LoadCrops();
        var callout = g.Add(new TreasureCallout(g.Font));
        var asked = setUp(s, view, callout);
        return area =>
        {
            view.Place(area.Position, PixelsPerScript(area));
            view.Refresh(s);
            if (asked is not var (lines, cell, waiting))
                return;
            callout.Show(lines, TreasureView.CellRect(cell), waiting);
            callout.Place(area);
            view.Asked = cell;
        };
    }

    // ---------------------------------------------------------------- Harem (docs/ui/pickers.md, H2)

    private static IEnumerable<GalleryEntry> Harem()
    {
        yield return new("harem", "Screens", "Harem", Week18, "", g =>
        {
            if (g.State(Week18) is not { } s)
                return null;
            var view = g.Add(new HaremView(g.Font, g.Faces) { Visible = true });
            var girls = g.Bound(s).HaremGirls();
            return area =>
            {
                view.Place(area.Position, PixelsPerScript(area));
                view.Refresh(s, girls);
            };
        });
    }

    // ---------------------------------------------------------------- Subordinates (docs/ui/subordinates.md)

    private static IEnumerable<GalleryEntry> Subordinates()
    {
        yield return new("subordinates", "Screens", "Subordinates", Week18, "",
            g => SubordinatesScreen(g, (_, _) => { }));
        yield return new("subordinates-question", "Screens", "Subordinates: Replenish asks first", Week18, "",
            g => SubordinatesScreen(g, (s, view) => view.Press(SubordinateOrderKind.Replenish, Neediest(s))));
        yield return new("subordinates-result", "Screens", "Subordinates: Replenish's plate", Week18,
            "The plate's figures are Replenish's for the unit as it is now; the order is not run (SubordinateResult.ReplenishPreview).",
            g => SubordinatesScreen(g, (s, view) =>
            {
                int unit = Neediest(s);
                view.Table.Chosen = unit;                       // the order's unit, chosen as its click chose it
                view.ShowResult(SubordinateResult.ReplenishPreview(s, unit));
            }));
    }

    private static int Neediest(GameState s) =>
        GalleryStates.NeediestUnit(s) is var unit and > 0 ? unit
            : throw new InvalidOperationException("The save has no unit at status 1 short of troops.");

    /// <summary>
    /// Our Subordinates screen on the save's state, then <paramref name="then"/> once it is placed and filled, and filled
    /// again after it.
    /// </summary>
    private static Action<Rect2>? SubordinatesScreen(GalleryStage g, Action<GameState, SubordinatesView> then)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var view = g.Add(new SubordinatesView(g.Font, g.Faces) { Visible = true });
        view.LoadCrops();
        bool done = false;
        return area =>
        {
            view.Place(area.Position, PixelsPerScript(area));
            view.Refresh(s, 0);
            if (done)
                return;
            done = true;
            then(s, view);
            view.Refresh(s, 0);
        };
    }

    // ---------------------------------------------------------------- the message windows (docs/ui/messages.md)

    private static IEnumerable<GalleryEntry> Messages() =>
        GalleryStates.Messages.Select(sample => new GalleryEntry(sample.Id, "Messages", $"Message window: {sample.Kind}{(sample.Read ? ", read before" : "")}",
            null, "The lines are the gallery's own; the portraits and the scene under the window are not drawn.", g =>
            {
                var view = g.Add(new MessageWindowView(g.Font) { Docks = TouchMode.On });
                view.Show(sample.Window(), true, null, null);
                view.Complete();
                return view.Place;
            }));
}
