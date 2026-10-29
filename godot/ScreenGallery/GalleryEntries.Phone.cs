using Godot;
using Kichikuou.Game.Strategy;
using Kichikuou.Original.Content.Devices;
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

    private static bool PhoneLandscape => Device.Layout == DeviceLayout.PhoneLandscape;

    /// <summary>The phone held either way: the scenes' parts (the Menu cell, the drawer, the Menu sheet) serve both (step P1).</summary>
    private static bool PhoneHeld => Device.Layout is DeviceLayout.PhoneLandscape or DeviceLayout.PhoneUpright;

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
    }

    /// <summary>
    /// The Menu sheet open over a speech at its key wait (LN3: the screen dimmed, the cell reading Close), then a tap on
    /// <paramref name="tap"/>'s tile given to the sheet as the player's.
    /// </summary>
    private static Action<Rect2>? MenuSheet(GalleryStage g, PhoneMenu.Command? tap)
    {
        if (g.State(Week18) is not { } s)
            return null;
        var backdrop = g.Backdrop(s);
        var speech = GalleryStates.Messages.First(m => m is { Kind: MessageKind.SpeechRight, Read: false });
        var window = g.Add(new MessageWindowView(g.Font) { Docks = TouchMode.On });
        window.Show(speech.Window(), true, null, null);
        window.Complete();
        var drawer = g.Add(new SkipDrawer { Visible = true });
        var sheet = g.Add(new MenuSheetView(() => SampleMenu) { Visible = true });
        bool tapped = false;
        return area =>
        {
            GalleryStage.Fill(backdrop, area);
            window.Place(area);
            drawer.Place(area);
            sheet.Open();
            sheet.Place();
            if (tapped || tap is not { } command)
                return;
            tapped = true;
            GalleryStage.Click(sheet, sheet.TileCentre(command));
        };
    }
}
