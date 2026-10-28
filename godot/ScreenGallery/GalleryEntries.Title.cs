using Godot;
using Kichikuou.Content;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.ScreenGallery;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The screens before a game (docs/ui/gallery.md, §7 step 7): the import screen held in each of its
/// phases, and the title, <c>ｽﾀMENU</c>, run from the player's files after a fresh launch's boot and held at a moment
/// (<see cref="GalleryTitle"/>). Neither needs a kept save.
/// </summary>
internal static partial class GalleryEntries
{
    private const string TitleSection = "Title";

    /// <summary>The game folder the import screen's entries show: the gallery's own, not the player's.</summary>
    private const string SampleFolder = @"C:\Games\Kichikuou Rance";

    private const string FolderNote = "The folder's path is the gallery's own; nothing is checked or imported.";

    private static IEnumerable<GalleryEntry> TitleScreens()
    {
        yield return new("import-none", TitleSection, "Import: the first launch", null,
            "No folder chosen yet, Import not offered (docs/import.md, step 8). Nothing is checked or imported.",
            g => Import(g, ImportScreen.Held(g.Font, "")));
        yield return new("import-choose", TitleSection, "Import: a folder chosen", null, FolderNote,
            g => Import(g, ImportScreen.Held(g.Font, SampleFolder)));
        yield return new("import-back", TitleSection, "Import: from Option → Game", null,
            "With its way back to the title, the folder the player chose before. " + FolderNote,
            g => Import(g, ImportScreen.Held(g.Font, SampleFolder, back: true)));
        yield return new("import-progress", TitleSection, "Import: decompiling", null,
            "Made up: the bar held at 45% of the decompile (GameImport's stage). " + FolderNote,
            g => Import(g, ImportScreen.Held(g.Font, SampleFolder, new ImportProgress("Decompiling the scenario", 0.45))));
        yield return new("import-done", TitleSection, "Import: imported", null,
            "The half second before the title opens. " + FolderNote,
            g => Import(g, ImportScreen.Held(g.Font, SampleFolder, done: true)));
        yield return new("import-refused", TitleSection, "Import: the wrong folder", null,
            "GameFiles' refusal of a folder without the game's files (GameFiles.MissingFile). " + FolderNote,
            g => Import(g, ImportScreen.Held(g.Font, SampleFolder,
                refusal: GameFiles.MissingFile(SampleFolder, GameFiles.EnglishTranslation10, GameFiles.EnglishTranslation10.Files[0].File))));
        yield return new("import-refused-version", TitleSection, "Import: another version's file", null,
            "GameFiles' refusal of a file of another version, its hashes on the small line: made up, an empty KICHIKUSA.ALD "
            + "(GameFiles.OtherFile). " + FolderNote,
            g => Import(g, ImportScreen.Held(g.Font, SampleFolder, refusal: OtherVersion())));

        yield return new("title", TitleSection, "Title: a first launch", null,
            "ｽﾀMENU run from the player's files after a fresh boot, held at its mouse poll: no clear yet, so Alice's Mansion, "
            + "Load and Start; the pointer off every button.",
            g => Title(g, new TitleMoment()));
        yield return new("title-hover", TitleSection, "Title: Start hovered", null,
            "The pointer at rest over Start (button 6): its highlight from the title's sheet (ｽﾀMENU.ADV:103-117).",
            g => Title(g, new TitleMoment(Gesture: (pointer, s) =>
            {
                var (x, y) = GalleryTitle.Centre(s, GalleryTitle.NewGame);
                pointer.Move(x, y);
            })));
        yield return new("title-clears", TitleSection, "Title: after a clear, every button", null,
            "Made up: a clear (GalleryStates.Cleared) and both rolls at their top at once (ｽﾀMENU.ADV:62-70: 1 in 10 and "
            + "1 in 20), so the requests slip and Saitou are there too.",
            g => Title(g, new TitleMoment(GalleryStates.Cleared, [10, 20])));
        yield return new("title-description", TitleSection, "Title: a button's description", null,
            "A right click on Start: its description in window 113 (ours) until a key (ｽﾀMENU.ADV:585-685).",
            g => Title(g, new TitleMoment(Gesture: (pointer, s) =>
            {
                var (x, y) = GalleryTitle.Centre(s, GalleryTitle.NewGame);
                pointer.RightClick(x, y);
            }, AtKey: true)));
        yield return new("title-requests", TitleSection, "Title: the requests slip", null,
            "After a clear with the slip's roll (Saitou's not): a click on it, its second request of the first five "
            + "(ｽﾀMENU.ADV:182-234), held at its key wait.",
            g => Title(g, new TitleMoment(GalleryStates.Cleared, [10, 1, 64, 2], (pointer, s) =>
            {
                var (x, y) = GalleryTitle.Centre(s, GalleryTitle.Requests);
                pointer.Click(x, y);
            }, AtKey: true)));
    }

    /// <summary>The refusal of a KICHIKUSA.ALD of no known version: the SHA-256 of an empty file against the one expected.</summary>
    private static string OtherVersion()
    {
        var version = GameFiles.EnglishTranslation10;
        var (file, expected) = version.Files[0];
        return GameFiles.OtherFile(SampleFolder, version, file,
            "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", expected);
    }

    /// <summary>The import screen held in its phase, over the whole picture (it letterboxes itself as at a launch).</summary>
    private static Action<Rect2> Import(GalleryStage g, ImportScreen screen)
    {
        g.Add(screen);
        return area => GalleryStage.Fill(screen, area);
    }

    /// <summary>
    /// The title held at <paramref name="moment"/>: the presenter's screen, and at a key wait our message window if one is
    /// up and, in the touch mode, the Skip and Log drawer as <c>GameScreen.UpdateSkipButtons</c> shows it there (Log off:
    /// nothing is logged at the title).
    /// </summary>
    private static Action<Rect2> Title(GalleryStage g, TitleMoment moment)
    {
        var p = GalleryTitle.HeldAt(g.Fresh, moment);
        var picture = g.Picture(p);
        MessageWindowView? window = null;
        if (moment.AtKey && p.MessageWindow is { } shown)
        {
            window = g.Add(new MessageWindowView(g.Font) { Docks = TouchMode.On });
            window.Show(shown, true, null, null);
            window.Complete();
        }
        var drawer = TouchMode.On && moment.AtKey
            ? g.Add(new SkipDrawer { Visible = true, LogDisabled = p.Log.Pages.Count == 0 })
            : null;
        return area =>
        {
            GalleryStage.Fill(picture, area);
            window?.Place(area);
            drawer?.Place(area);
        };
    }
}
