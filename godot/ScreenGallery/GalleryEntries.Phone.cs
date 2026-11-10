using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;
using Kichikuou.Original.Content.Saves;
using Kichikuou.Original.Content.ScreenGallery;
using Kichikuou.Original.Content.Strategy;

namespace Kichikuou.Game.ScreenGallery;

/// <summary>
/// The phone held landscape's own (docs/ui/phone-build.md, step L1): the Menu cell on top of the left side, which the
/// entries of the screens that show it add on the phone pass, and the Menu sheet it opens (entries of the phone pass alone,
/// as <c>touch-skip-on</c> is the touch pass's).
/// </summary>
internal static partial class GalleryEntries
{
    /// <summary>The Menu sheet's switches as the gallery shows them: the skip off, the sound on, a development build's Debug.</summary>
    private static readonly PhoneMenu.Settings SampleMenu = new(SkipOn: false, SkipEnabled: true, SoundOn: true, Development: true,
        HideDimmedNames: true);

    /// <summary>
    /// The game folder's saves as the saves' page shows them (docs/save-import.md, §8): the owner's ten (§4, their weeks and
    /// stages), the file's time theirs; with <paramref name="refused"/> the last one refused for another version, a sample.
    /// </summary>
    private static PhoneMenu.SaveShelf SampleShelf(bool refused = false) => new(
        [.. new (char Slot, int Week, int Stage)[]
            {
                ('A', 22, 2), ('B', 20, 1), ('C', 15, 1), ('D', 17, 1), ('E', 18, 1), ('F', 19, 2), ('G', 22, 2), ('H', 21, 2),
                ('I', 16, 1), ('J', 15, 1),
            }
            .Select(s => refused && s.Slot == 'J'
                ? new OriginalSaveFile("KICHIKUSJ.asd", null, 0, 0, null,
                    "It is not a save of the version of Kichikuou Rance this game reads (the English translation 1.0): it holds the pages 1, 2, 4, this version's saves 1, 2, 4, 10.",
                    OriginalSave.AnotherVersion)
                : new OriginalSaveFile($"KICHIKUS{s.Slot}.asd", null, s.Week, s.Stage, new DateTime(2026, 10, 10, 21, 40, 0)))],
        "Android/data/org.kichikuou.port/files/game");

    private static bool PhoneLandscape => Device.Layout == DeviceLayout.PhoneLandscape;

    /// <summary>The phone held either way: the scenes' parts (the Menu cell, the drawer, the Menu sheet) serve both (step P1).</summary>
    private static bool PhoneHeld => Device.Layout is DeviceLayout.PhoneLandscape or DeviceLayout.PhonePortrait;

    /// <summary>On the phone held landscape the Menu cell, closed, as <c>GameScreen.UpdatePhone</c> shows it; else null.</summary>
    private static MenuSheetView? MenuCell(GalleryStage g) =>
        PhoneHeld ? g.Add(new MenuSheetView(() => SampleMenu) { Visible = true }) : null;

    private static IEnumerable<GalleryEntry> PhoneScreens()
    {
        if (!PhoneHeld)
            yield break;
        const string Note = "Over a speech at its key wait on the map (the gallery's own lines); the switches are the gallery's own.";
        yield return new("menu-sheet", "Messages", "Phone: the Menu sheet", Week18, Note, g => MenuSheet(g, null));
        yield return new("menu-sheet-restart", "Messages", "Phone: the Menu sheet, Restart asks first", Week18,
            "A tap on Restart: its question in a callout at the tile, in the PC's dialog's words. " + Note,
            g => MenuSheet(g, PhoneMenu.Command.Restart));
        yield return new("menu-sheet-debug", "Messages", "Phone: the Menu sheet's Debug page", Week18,
            "A tap on Debug (a development build's). " + Note, g => MenuSheet(g, PhoneMenu.Command.Debug));
        const string Saves = "The game folder's saves of the original (docs/save-import.md, §8; the owner's ten, their weeks and stages). ";
        yield return new("menu-sheet-saves", "Messages", "Phone: the Menu sheet's original saves", Week18,
            "A tap on Import original save (the owner's S1). " + Saves + Note,
            g => MenuSheet(g, null, SampleShelf(), s => s.TileCentre(PhoneMenu.Command.ImportSave)));
        yield return new("menu-sheet-save-question", "Messages", "Phone: an original save asks before its load", Week18,
            "A tap on save E: the PC's question in the callout, Load | Cancel. " + Saves + Note,
            g => MenuSheet(g, null, SampleShelf(), s => s.TileCentre(PhoneMenu.Command.ImportSave), s => s.SaveCentre("E")));
        yield return new("menu-sheet-save-refused", "Messages", "Phone: an original save that cannot be imported", Week18,
            "Save J refused (a sample: another version), greyed with the reason in a word; its tap says why, OK. " + Saves + Note,
            g => MenuSheet(g, null, SampleShelf(refused: true), s => s.TileCentre(PhoneMenu.Command.ImportSave), s => s.SaveCentre("J")));
        yield return new("menu-sheet-no-saves", "Messages", "Phone: no original save in the game folder", Week18,
            "A tap on Import original save with none in the folder: where the original keeps them and where they go. " + Note,
            g => MenuSheet(g, null, new PhoneMenu.SaveShelf([], "Android/data/org.kichikuou.port/files/game"),
                s => s.TileCentre(PhoneMenu.Command.ImportSave)));
    }

    /// <summary>
    /// The Menu sheet open over a speech at its key wait (LN3: the screen dimmed, the cell reading Close), then a tap on
    /// <paramref name="tap"/>'s tile given to the sheet as the player's.
    /// </summary>
    private static Action<Rect2>? MenuSheet(GalleryStage g, PhoneMenu.Command? tap, PhoneMenu.SaveShelf? shelf = null,
        params Func<MenuSheetView, Vector2>[] taps)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var backdrop = g.Backdrop(s);
        var speech = GalleryStates.Messages.First(m => m is { Kind: MessageKind.SpeechRight, Read: false });
        var window = g.Add(new MessageWindowView(g.Font) { Docks = TouchMode.On });
        window.Show(speech.Window(), true, null, null);
        window.Complete();
        var drawer = g.Add(new SkipDrawer { Visible = true });
        var sheet = g.Add(new MenuSheetView(() => SampleMenu, () => shelf ?? SampleShelf()) { Visible = true });
        if (tap is { } command)
            taps = [s => s.TileCentre(command)];
        bool tapped = false;
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            window.Place(area);
            drawer.Place(area);
            sheet.Open();
            sheet.Place();
            if (tapped)
                return;
            tapped = true;
            foreach (var at in taps)
                GalleryStage.Click(sheet, at(sheet));
        };
    }
}
