using Godot;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// The phone held landscape (docs/ui/phone-build.md, step L1): the menu bar is gone, its commands are the Menu cell's on
// top of the left side, which opens the Menu sheet (the owner's LN3, MenuSheetView); the drawer's Skip and Log stand at
// the left thumb (SkipDrawer), and the week report, the scenes' menus and the message log have layouts of their own.
// Held upright (step P1) the same parts serve it: Menu, Log and Skip in the bar at the foot (PK1), the sheet rising from
// it (PN1), the message windows over or under the picture (PS2).
public partial class GameScreen
{
    private MenuSheetView _menuSheet = null!;

    private void ReadyPhone()
    {
        _menuSheet = new MenuSheetView(PhoneMenuSettings);
        _menuSheet.Opened += () =>
        {
            _modal = true;                              // the game waits under the sheet, as under the PC's menus' boxes
            _input.Clear();
        };
        _menuSheet.Closed += () =>
        {
            _modal = false;
            _input.Clear();
        };
        _menuSheet.Chosen += PhoneMenuChosen;
        _menuSheet.Forwarded += StripInput;
        _content.AddChild(_menuSheet);
    }

    /// <summary>The phone held landscape: our screens with layouts of their own lay themselves out in its frame and sides.</summary>
    private static bool PhoneLandscape => Device.Layout == DeviceLayout.PhoneLandscape;

    /// <summary>The phone held upright: the scenes' parts (step P1) in the bar at the foot and over or under the picture.</summary>
    private static bool PhonePortrait => Device.Layout == DeviceLayout.PhonePortrait;

    private PhoneMenu.Settings PhoneMenuSettings() =>
        new(_skip.Activated, _skip.Enabled, SoundChoice.On, OS.IsDebugBuild(), MapNamesChoice.HideDimmed);

    /// <summary>
    /// The Menu cell on the phone, each frame: on top of the left side held landscape, in the bar's left upright, wherever
    /// a game is shown and no screen of ours takes the side or waits on its own (a box, the log, the options, our battle
    /// and full screens, the command panel; upright a province's sheet too, whose bar holds Return alone); the sheet,
    /// once open, until it closes.
    /// </summary>
    private void UpdatePhone()
    {
        _menuSheet.Visible = (PhoneLandscape || PhonePortrait) && (_menuSheet.IsOpen
            || MenuCellShown && !(PhonePortrait && _provinceView.Visible));     // PM1c: the sheet's bar holds Return alone
        if (_menuSheet.Visible)
            _menuSheet.Place();
    }

    private bool MenuCellShown => _presenter.State is not null && !_modal && !ViewHidden && !BattleScreenShown && !EnemyAttackShown
        && !ScoutReportShown && !StrategyCutInShown && !_saveView.Visible && !_commandView.Visible;

    /// <summary>The week report on the phone: the left side, or the bar, as in the scenes (LW1, PW1), Skip and Log with it.</summary>
    private bool PhoneWeekReport => _weekView.Visible && (PhoneLandscape || PhonePortrait);


    /// <summary>A tile of the Menu sheet acted (Restart's and Exit's questions answered already), as the menu bar's item.</summary>
    private void PhoneMenuChosen(PhoneMenu.Command command)
    {
        switch (command)
        {
            case PhoneMenu.Command.Options:
                OpenOptions();
                break;
            case PhoneMenu.Command.MessageSkip when _skip.Enabled:
                _skip.Toggle();
                break;
            case PhoneMenu.Command.Sound:
                ToggleSound();
                break;
            case PhoneMenu.Command.Screenshot:
                SaveScreenshot();
                break;
            case PhoneMenu.Command.Restart:
                Restart();
                break;
            case PhoneMenu.Command.Exit:
                _tree.Quit();
                break;
            case PhoneMenu.Command.BattleCaptures:
                OpenCaptures();
                break;
            case PhoneMenu.Command.Portraits:
                OpenPortraits();
                break;
            case PhoneMenu.Command.Effects:
                OpenEffects();
                break;
            case PhoneMenu.Command.ScreenGallery:
                OpenScreenGallery();
                break;
            case PhoneMenu.Command.HideDimmedNames:
                ToggleDimmedNames();
                break;
            case PhoneMenu.Command.MenuRowHeight:
                OpenMenuRows();
                break;
        }
    }

    /// <summary>
    /// Unattended play on the phone held landscape, in the touch exercise (<see cref="AutoSkipTouchAsync"/>): a tap on the
    /// Menu cell opens the sheet (dump "phone-menu"), a tap on Sound turns it and another back, Restart asks (dump
    /// "phone-menu-restart") and Cancel's tap closes the question, a tap on Close closes the sheet — the game waiting
    /// meanwhile and nothing reaching the script's input. Anything else is an error.
    /// </summary>
    private async Task AutoPhoneMenuAsync(Func<Vector2, Task> tap)
    {
        bool sound = SoundChoice.On;
        await tap(_menuSheet.CellCentre);
        if (!_menuSheet.IsOpen || !_modal)
            throw new InvalidOperationException("--auto's tap on the phone's Menu cell opened no Menu sheet.");
        Shot("phone-menu");
        await tap(_menuSheet.TileCentre(PhoneMenu.Command.Sound));
        await tap(_menuSheet.TileCentre(PhoneMenu.Command.Sound));
        if (SoundChoice.On != sound || !_menuSheet.IsOpen)
            throw new InvalidOperationException("--auto's two taps on the Menu sheet's Sound did not leave it as it was, the sheet open.");
        await tap(_menuSheet.TileCentre(PhoneMenu.Command.Restart));
        if (_menuSheet.Asking is null)
            throw new InvalidOperationException("--auto's tap on the Menu sheet's Restart asked nothing.");
        Shot("phone-menu-restart");
        await tap(_menuSheet.CancelCentre);
        if (_menuSheet.Asking is not null || !_menuSheet.IsOpen)
            throw new InvalidOperationException("--auto's tap on the Restart question's Cancel did not close the question alone.");
        await tap(_menuSheet.CellCentre);
        if (_menuSheet.IsOpen || _modal)
            throw new InvalidOperationException("--auto's tap on the Menu sheet's Close did not close it.");
        GD.Print("Kichikuou: --auto opens the phone's Menu sheet, turns Sound and back, asks Restart and cancels it, closes it.");
    }
}
