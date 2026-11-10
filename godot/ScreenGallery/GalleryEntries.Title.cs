using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Audio;
using Kichikuou.Engine.Script;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.State;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The screens before a game (docs/ui/gallery.md, §7 step 7): the import screen held in each of its
/// phases, and the title, <c>ｽﾀMENU</c>, run from the player's files after a fresh launch's boot and held at a moment
/// (<see cref="GalleryTitle"/>). Neither needs a kept save.
/// </summary>
internal static partial class GalleryEntries
{
    private const string TitleSection = "Title";

    /// <summary>
    /// The game folder the import screen's entries show: the gallery's own, not the player's — on the phone one in Android's
    /// storage, as LI's and PI's.
    /// </summary>
    private static string SampleFolder => PhoneHeld
        ? "/storage/emulated/0/Games/Kichikuou Rance"
        : @"C:\Games\Kichikuou Rance";

    private const string FolderNote = "The folder's path is the gallery's own; nothing is checked or imported.";

    /// <summary>The translation's playlist as the music's note names it: with the phone's separators on the phone (Android's).</summary>
    private static string[] SampleTracks =>
        [.. MusicTable.TranslationPlaylist.Select(line => line.Replace('\\', PhoneHeld ? '/' : '\\'))];

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
        yield return new("import-no-music", TitleSection, "Import: a copy without music", null,
            "Imported, but no track of the playlist in the folder (MusicNote, the owner's B): the row with ♪ between the scenario "
            + "and Play, the screen waiting for the red Play. " + FolderNote,
            g => Import(g, ImportScreen.Held(g.Font, SampleFolder, done: true, music: MusicNote.Of(SampleTracks, SampleTracks))));
        yield return new("import-some-music", TitleSection, "Import: some tracks missing", null,
            "Made up: three tracks of the playlist not in the folder, named in the row. " + FolderNote,
            g => Import(g, ImportScreen.Held(g.Font, SampleFolder, done: true,
                music: MusicNote.Of(SampleTracks, [SampleTracks[5], SampleTracks[10], SampleTracks[18]]))));
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

        yield return new("fortune-check", TitleSection, "Fortune Check", null,
            "Made up: a clear and three fates seen (GalleryStates.FatesSeen). A click on the title's Fortune Check (button 0) "
            + "runs HAPPY_C from the player's files, held at its mouse poll on its first page with the stamps, the pointer off its cells.",
            g => Title(g, new TitleMoment(GalleryStates.FatesSeen, Gesture: ClickOn(GalleryTitle.FortuneCheck, (610, 200))), title: false));
        yield return new("cg-gallery", TitleSection, "The CG gallery", null,
            "After a clear: a click on the title's C.G (button 7) runs CG from the player's files, held at its mouse poll on "
            + "page 1. Its sheets are masked as every agent's dump masks them (GamePresenter.MaskEventCgs).",
            g => Title(g, new TitleMoment(GalleryStates.Cleared, Gesture: ClickOn(GalleryTitle.CgGallery, (600, 395))), title: false));
        yield return new("music-room", TitleSection, "The music room", null,
            "After a clear: a click on the title's B.G.M (button 1) runs MMODE from the player's files, held at its mouse "
            + "poll with no track playing, the pointer off its grid.",
            g => Title(g, new TitleMoment(GalleryStates.Cleared, Gesture: ClickOn(GalleryTitle.MusicRoom, (160, 100))), title: false));
    }

    /// <summary>
    /// A click in the middle of the title's <paramref name="button"/>, as the title loaded its box, then the pointer moved
    /// to <paramref name="rest"/>, a point the screen it opens does not light.
    /// </summary>
    private static Action<PointerScript, GameState> ClickOn(int button, (int X, int Y) rest) => (pointer, s) =>
    {
        var (x, y) = GalleryTitle.Centre(s, button);
        pointer.Click(x, y);
        pointer.Hover(rest.X, rest.Y);
    };

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
    /// nothing is logged at the title). Held upright the title's own (<paramref name="title"/>: not a screen it opens) is
    /// our screen over its picture, a row for each button it draws at its mouse poll, none under a description, which
    /// stands the picture on the logo (PE6, as <c>GameScreen.Title.cs</c>).
    /// </summary>
    private static Action<Rect2> Title(GalleryStage g, TitleMoment moment, bool title = true)
    {
        var p = GalleryTitle.HeldAt(g.Fresh, moment);
        var picture = g.Picture(p);
        TitlePortraitView? screen = null;
        if (title && Device.Layout == DeviceLayout.PhonePortrait && p.State is { } s)
        {
            var buttons = PhoneTitlePortrait.Buttons(s);
            bool polls = !moment.AtKey && p.MessageWindow is null;
            if (polls || p.MessageWindow is not null)
            {
                screen = g.Add(new TitlePortraitView(g.Faces.Cgs));
                screen.ShowTitle(buttons, polls);
                if (!polls && screen.LogoHeight is int logo)
                    Device.PictureTop = f => PhoneTitlePortrait.DescriptionPictureTop(f, buttons.Count, logo);
            }
        }
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
        var menu = MenuCell(g);                                 // the phone held landscape: Menu in the left side (LE1)
        return area =>
        {
            GalleryStage.Fill(picture, area);
            window?.Place(area);
            drawer?.Place(area);
            screen?.Place();
            menu?.Place();
        };
    }
}
