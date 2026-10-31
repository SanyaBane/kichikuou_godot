using Godot;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The deployment screen and the Save/Load screen (docs/ui/gallery.md, step 3). The deployment picker is opened in the
/// presenter for its bar's labels, as <c>Sﾌﾞﾀｲ</c> opens it; the Save/Load screen reads a library in memory made of the
/// kept save (<see cref="GalleryStates.SampleLibrary"/>), so nothing it does reaches a disk, and its clicks are given to
/// it as input (<see cref="GalleryStage.Click"/>).
/// </summary>
internal static partial class GalleryEntries
{
    /// <summary>The party the week-18 look sends against Oaks (6): Rance (unit 1), Haurein (unit 12), Cordoba (unit 8), Barres (unit 6).</summary>
    private static readonly int[] OaksParty = [1, 12, 8, 6];

    private const int Rance = 1;

    private static IEnumerable<GalleryEntry> DeploymentScreens()
    {
        yield return new("deployment", "Screens", "Deployment: an attack's party", Week18,
            "The party the week-18 look sends against Oaks (6); Rance's details shown, as when his row is hovered. " + RosterNote,
            g => Deployment(g, 1, 4, OaksParty, Rance));
        yield return new("deployment-picking", "Screens", "Deployment: two picked", Week18,
            "Rance and Haurein picked, no row hovered. " + RosterNote,
            g => Deployment(g, 1, 4, [Rance, 12], 0));
        yield return new("deployment-defence", "Screens", "Deployment: a defence", Week18,
            "Sﾌﾞﾀｲ in mode 2 (FIGT_B.ADV:78-80), the same party. " + RosterNote,
            g => Deployment(g, 2, 4, OaksParty, Rance));
        yield return new("deployment-search", "Screens", "Deployment: a dungeon search", Week18,
            "Sﾌﾞﾀｲ in mode 3 for one unit (Cﾒｲｷｭｳ.ADV:333-335). " + RosterNote,
            g => Deployment(g, 3, 1, [Rance], Rance));
        yield return new("deployment-refill-question", "Screens", "Deployment: Replenish asks first", Week18,
            "The unit --refill takes: the fewest troops of its maximum among those Refill offers. " + RosterNote,
            g => Deployment(g, 1, 4, OaksParty, 0, refill: Refill.Question));
        yield return new("deployment-refill-done", "Screens", "Deployment: Replenish's plate", Week18,
            "The plate's figures are Replenish's for the unit as it is now (SubordinateResult.ReplenishPreview); the table under it is not replenished. " + RosterNote,
            g => Deployment(g, 1, 4, OaksParty, 0, refill: Refill.Plate));
        yield return new("deployment-troops", "Screens", "Deployment: the Troops tab", Week18,
            "The ledger's Troops tab sorted by T.Atk (docs/ui/unit-ledger.md, B2). " + RosterNote,
            g => Deployment(g, 1, 4, OaksParty, Rance, tab: LedgerTab.Troops, sort: (DeploymentColumn.TroopAtk, true)));
        yield return new("deployment-commander", "Screens", "Deployment: the Commander tab, the order kept", Week18,
            "The Commander tab with the rows still in the Troops tab's T.Atk order: the order's seal right of the tabs (B3, round D). " + RosterNote,
            g => Deployment(g, 1, 4, OaksParty, Rance, tab: LedgerTab.Commander, sort: (DeploymentColumn.TroopAtk, true)));
    }

    private enum Refill { None, Question, Plate }

    /// <summary>
    /// Our deployment screen — the panels over the whole of <c>Sﾌﾞﾀｲ</c>'s screen, the table across their top — as
    /// <c>GameScreen.UpdateDeploymentScreen</c> lays it, the picker opened in <paramref name="mode"/> with
    /// <paramref name="party"/> picked; with <paramref name="refill"/> the Replenish question at a Refill cell or its plate.
    /// </summary>
    private static Action<Rect2>? Deployment(GalleryStage g, int mode, int limit, int[] party, int details, Refill refill = Refill.None,
        LedgerTab tab = LedgerTab.Overview, (DeploymentColumn Column, bool Descending)? sort = null)
    {
        if (g.State(Week18) is not { } s)
            return null;
        GalleryStates.LongRoster(s);
        GalleryStates.Deployment(s, limit, party, details);
        var p = g.Bound(s);
        p.Show(new PickerScreenEvent(PickerKind.Deployment, (Sys3Int)mode, true));
        var picker = p.Picker ?? throw new InvalidOperationException("The deployment picker did not open.");
        p.Show(new PickerScreenEvent(PickerKind.Deployment, (Sys3Int)mode, false));
        var panels = g.Add(new DeploymentPanels(g.Font, g.Faces) { Visible = true });
        panels.LoadCrops();
        var table = g.Add(new DeploymentTableView(g.Font) { Visible = true });
        panels.Table = table;
        panels.ShowTab(tab, remember: false);                   // the entry's tab, not the player's
        if (sort is { } order)
            table.Sort(order.Column, order.Descending);
        int unit = refill == Refill.None ? 0
            : DeploymentTable.Rows(s).Where(r => r.CanRefill).OrderBy(r => r.Troops / (double)Math.Max(1, r.MaxTroops))
                .ThenBy(r => r.Unit).Select(r => r.Unit).FirstOrDefault();
        if (refill != Refill.None && unit == 0)
            throw new InvalidOperationException("No unit can be replenished on the save.");
        var callout = refill == Refill.Question ? g.Add(new TreasureCallout(g.Font)) : null;
        var plate = refill == Refill.Plate ? g.Add(new OrderResultPlate(g.Font, g.Faces)) : null;
        plate?.LoadCrops();
        plate?.Show(SubordinateResult.ReplenishPreview(s, unit));
        return area =>
        {
            float pixels = area.Size.X / 640f;
            panels.Place(area.Position, pixels);
            panels.Refresh(s, picker);
            DeploymentPanels.PlaceTable(table, area.Position, pixels);
            table.Refresh(s);
            plate?.Place(area);
            if (callout is null)
                return;
            callout.PlaceOnDevice(area);
            g.AfterLayout(() =>
            {
                if (table.RefillCellRect(unit) is not { } cell)
                    return;
                var row = DeploymentTable.Rows(s).First(r => r.Unit == unit);
                var screenCell = callout.FromGlobal(cell);
                table.AskedRefill = unit;
                callout.Show(DeploymentTable.ReplenishQuestion(row), screenCell, waiting: false);
                callout.OpenMenu(["Yes", "No"]);
            });
        };
    }

    // ---------------------------------------------------------------- Save/Load (docs/ui/save-load.md, §9)

    /// <summary>When the newest sample save was written: a fixed time, so the pictures stay the same.</summary>
    private static readonly DateTime SavedAt = new(2026, 10, 20, 21, 30, 0);

    private const string SamplesNote = "Made up: ten Turn Start and ten Turn End auto-saves and twelve manual saves, all the week-18 "
        + "game, the older ones a week earlier each (only the date differs), kept in memory.";

    private static IEnumerable<GalleryEntry> SaveLoadScreens()
    {
        yield return new("save-load-save", "Screens", "Save: a new save chosen", Week18, SamplesNote,
            g => SaveScreen(g, SaveList.Save, tabs: true));
        yield return new("save-load", "Screens", "Load: the save written last", Week18, SamplesNote,
            g => SaveScreen(g, SaveList.Load, tabs: true));
        yield return new("save-load-title", "Screens", "Load from the title", Week18, "No tabs, as from the title. " + SamplesNote,
            g => SaveScreen(g, SaveList.Load, tabs: false));
        yield return new("save-load-auto", "Screens", "Load: the auto-saves", Week18, SamplesNote,
            g => SaveScreen(g, SaveList.Load, tabs: true, view =>
            {
                GalleryStage.Click(view, view.SideCentre(auto: true));
                if (view.RowCentre(view.AutoSaves[0]) is { } row)
                    GalleryStage.Click(view, row);
            }));
        yield return new("save-load-auto-end", "Screens", "Load: a Turn End auto-save with a comment", Week18,
            "Owner's decision 31 (docs/ui/save-load.md, §12). " + SamplesNote,
            g => SaveScreen(g, SaveList.Load, tabs: true, view =>
            {
                GalleryStage.Click(view, view.SideCentre(auto: true));
                if (view.RowCentre(view.AutoSaves[1]) is { } row)
                    GalleryStage.Click(view, row);
            }));
        yield return new("save-load-delete", "Screens", "Load: Delete asks first", Week18, SamplesNote,
            g => SaveScreen(g, SaveList.Load, tabs: true, view => GalleryStage.Click(view, view.DeleteCentre)));
        yield return new("save-load-sweep", "Screens", "Save: the sweep asks first", Week18, SamplesNote,
            g => SaveScreen(g, SaveList.Save, tabs: true, view => GalleryStage.Click(view, view.SweepCentre)));
        yield return new("save-load-comment", "Screens", "Save: a comment written", Week18, SamplesNote,
            g => SaveScreen(g, SaveList.Save, tabs: true, view =>
            {
                if (view.RowCentre(view.ManualSaves[1]) is { } row)       // No. 3, without a comment
                    GalleryStage.Click(view, row);
                GalleryStage.Click(view, view.EditCentre);
                view.CommentField.Text = "the rebels retreat to Oaks";
                view.CommentField.CaretBlink = false;
            }));
    }

    /// <summary>
    /// Our Save/Load screen on <paramref name="list"/> of the sample library, waiting for its answer as <c>ﾛｰﾄﾞｾｰﾌﾞ</c>'s
    /// menu does, the game now the save's for a save; <paramref name="then"/> clicks once it is laid out.
    /// </summary>
    private static Action<Rect2>? SaveScreen(GalleryStage g, SaveList list, bool tabs, Action<SaveLoadView>? then = null)
    {
        if (g.Data(Week18) is not { } data || g.State(Week18) is not { } s)
            return null;
        var library = GalleryStates.SampleLibrary(data, SavedAt);
        bool save = list == SaveList.Save;
        var view = g.Add(new SaveLoadView(g.Font, g.Faces));
        view.Open(list, LoadSaveScreen.Items(library, save), library, save ? SaveSlots.Now(s) : null, null, tabs);
        _ = view.ChooseAsync();
        bool done = false;
        return area =>
        {
            view.Place(area);
            if (done || then is null)
                return;
            done = true;
            g.AfterLayout(() => then(view));
        };
    }
}
