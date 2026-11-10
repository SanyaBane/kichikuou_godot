// The menu's texts are taken from xsystem35 (https://github.com/kichikuou/xsystem35-sdl2): menu.c
// (the dialogs), Copyright (C) 2000- Masaki Chikama (Wren), and win/resources.rc (the labels) by the
// xsystem35-sdl2 authors. GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Battles.Effects;
using Kichikuou.Original.Content.Devices;

namespace Kichikuou.Game.Strategy;

// xsystem35's window menu bar over the game (docs/window-menu.md): File (Save Screenshot, Restart,
// Exit), Screen (Window, Fullscreen), Options (ours: the options window, OptionsView, with xsystem35's Mouse
// Movement among its settings), Sound[on|off] (ours, SoundChoice), MsgSkip[off|on]; hot keys F1 and F4 as xsystem35's, Alt+R; and ours for development, Debug
// (Battle Captures: the capture viewer, which our own title had until ｽﾀMENU replaced it; Portraits,
// Effects, Hide Dimmed Map Names), in a development build only — the editor's runs and the debug exports, not a release
// export (OS.IsDebugBuild; owner, 2026-11-08), as the phone's Menu sheet (PhoneMenu.Settings.Development).
public partial class GameScreen
{
    private const int MenuBarHeight = 24;

    private static readonly Color BarColor = new(0.94f, 0.94f, 0.94f);
    private static readonly Color HoverColor = new(0.8f, 0.88f, 0.97f);

    private Button _skipItem = null!;
    private Control _menuBar = null!;
    private int _turnSeen = Device.Turns;               // the layout's changes seen (Device.Turns)

    /// <summary>
    /// The screen laid out for the device (docs/ui/phone-build.md, §3), each frame, so a phone turned or a window resized
    /// is followed: on a phone the menu bar is gone and <c>_content</c> takes the whole screen, its black sides ours to
    /// draw in; the picture stands in the phone's frame (<see cref="ScreenFrame.In"/>: held landscape between the sides,
    /// upright at the width in the room's middle, on the title's logo under a description, <see cref="TitlePictureTop"/>). On a PC and a
    /// tablet the bar stays and the picture fills the room under it, fitted. In the touch layout both stand inside the
    /// player's screen margins (<see cref="Device.Margins"/>, docs/ui/screen-margins.md), the black background round them.
    /// </summary>
    private void UpdateDevice()
    {
        if (_turnSeen != Device.Turns)
        {
            _turnSeen = Device.Turns;
            (_screenTouch, _cardTouch) = (null, null);          // a press held back at the old layout's pixels is let go
            _screenTouches++;
            _cardTouches++;
        }
        bool phone = Device.Phone;
        var m = Device.Margins;
        _menuBar.Visible = !phone;
        _menuBar.OffsetLeft = m.Left;
        _menuBar.OffsetTop = m.Top;
        _menuBar.OffsetRight = -m.Right;
        _menuBar.OffsetBottom = m.Top + MenuBarHeight;
        _content.OffsetLeft = m.Left;
        _content.OffsetTop = (phone ? 0 : MenuBarHeight) + m.Top;
        _content.OffsetRight = -m.Right;
        _content.OffsetBottom = -m.Bottom;
        _content.ClipContents = m != default;   // nothing spills into them (the wave backdrop's rings reach past its rect)
        if (phone)
        {
            Device.PictureTop = TitlePictureTop(TitleButtons());
            var frame = ScreenFrame.In(new Rect2(Vector2.Zero, _content.Size)).Area;
            _view.OffsetLeft = frame.Position.X;
            _view.OffsetTop = frame.Position.Y;
            _view.OffsetRight = frame.End.X - _content.Size.X;
            _view.OffsetBottom = frame.End.Y - _content.Size.Y;
        }
    }

    private void BuildMenuBar()
    {
        var bar = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        bar.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = BarColor, ContentMarginLeft = 4 });
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        bar.OffsetBottom = MenuBarHeight;
        AddChild(bar);
        _menuBar = bar;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        bar.AddChild(row);

        row.AddChild(Menu("File",
            ("Save Screenshot", SaveScreenshot),
            ("Import Original Save…", ImportOriginalSave),          // ours (docs/save-import.md, §7)
            ("Restart", ConfirmRestart),
            ("Exit", ConfirmExit)));
        row.AddChild(Menu("Screen",
            ("Window", () => SetFullscreen(false)),
            ("Fullscreen", () => SetFullscreen(true))));
        var options = new Button { Text = "Options" };
        BarItem(options);
        options.Pressed += () => OpenOptions();
        row.AddChild(options);
        _soundItem = new Button();
        BarItem(_soundItem);
        _soundItem.Pressed += ToggleSound;
        row.AddChild(_soundItem);
        UpdateSoundItem();
        _skipItem = new Button();
        BarItem(_skipItem);
        _skipItem.Pressed += _skip.Toggle;
        row.AddChild(_skipItem);
        _skip.Changed += UpdateSkipItem;
        UpdateSkipItem();
        if (!OS.IsDebugBuild())
            return;                                         // a release build has no Debug menu
        var debugMenu = Menu("Debug",
            ("Battle Captures...", ConfirmCaptures),
            ("Portraits...", OpenPortraits),
            ("Effects...", OpenEffects),
            ("Screen Gallery...", ConfirmScreenGallery),
            ("Hide Dimmed Map Names", ToggleDimmedNames),
            ("Menu Row Height...", OpenMenuRows));
        _debugPopup = debugMenu.GetPopup();
        _debugPopup.SetItemAsCheckable(DimmedNamesItem, true);
        _debugPopup.SetItemChecked(DimmedNamesItem, MapNamesChoice.HideDimmed);
        row.AddChild(debugMenu);
    }

    private Button _soundItem = null!;
    private PopupMenu? _debugPopup;                         // none in a release build

    /// <summary>Sound on or off (ours, <see cref="SoundChoice"/>; saved for later launches).</summary>
    private void ToggleSound()
    {
        SoundChoice.On = !SoundChoice.On;
        UpdateSoundItem();
    }

    private void UpdateSoundItem() => _soundItem.Text = SoundChoice.On ? "Sound[on]" : "Sound[off]";

    /// <summary>Debug → Hide Dimmed Map Names: saved for later launches; the map's plates follow at once.</summary>
    private void ToggleDimmedNames()
    {
        MapNamesChoice.HideDimmed = !MapNamesChoice.HideDimmed;
        _debugPopup?.SetItemChecked(DimmedNamesItem, MapNamesChoice.HideDimmed);
        _markers.SetHideDimmed(MapNamesChoice.HideDimmed);
    }

    private static MenuButton Menu(string title, params (string Text, Action Run)[] items)
    {
        var menu = new MenuButton { Text = title };
        BarItem(menu);
        Fill(menu.GetPopup(), items);
        return menu;
    }

    /// <summary>The bar's look on a menu, and its items, whose ids are their indices.</summary>
    private static void Fill(PopupMenu popup, (string Text, Action Run)[] items)
    {
        popup.AddThemeStyleboxOverride("panel", new StyleBoxFlat
        {
            BgColor = BarColor, BorderColor = new Color(0.6f, 0.6f, 0.6f),
            BorderWidthLeft = 1, BorderWidthTop = 1, BorderWidthRight = 1, BorderWidthBottom = 1,
            ContentMarginTop = 2, ContentMarginBottom = 2,
        });
        popup.AddThemeStyleboxOverride("hover", new StyleBoxFlat { BgColor = HoverColor });
        popup.AddThemeColorOverride("font_color", Colors.Black);
        popup.AddThemeColorOverride("font_hover_color", Colors.Black);
        popup.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.55f, 0.55f));
        popup.AddThemeFontSizeOverride("font_size", 14);
        for (int i = 0; i < items.Length; i++)
            popup.AddItem(Padded(items[i].Text), i);
        popup.IdPressed += id =>
        {
            if (id < items.Length)
                items[(int)id].Run();
        };
    }

    private static string Padded(string text) => "   " + text + "      ";

    /// <summary>A top-level item: black text on the bar, a light highlight under the pointer, no focus.</summary>
    private static void BarItem(Button item)
    {
        item.FocusMode = FocusModeEnum.None;
        item.AddThemeFontSizeOverride("font_size", 14);
        foreach (var color in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color" })
            item.AddThemeColorOverride(color, Colors.Black);
        item.AddThemeColorOverride("font_disabled_color", new Color(0.55f, 0.55f, 0.55f));
        var plain = new StyleBoxFlat { BgColor = BarColor, ContentMarginLeft = 8, ContentMarginRight = 8 };
        var hover = new StyleBoxFlat { BgColor = HoverColor, ContentMarginLeft = 8, ContentMarginRight = 8 };
        item.AddThemeStyleboxOverride("normal", plain);
        item.AddThemeStyleboxOverride("disabled", plain);
        item.AddThemeStyleboxOverride("hover", hover);
        item.AddThemeStyleboxOverride("pressed", hover);
        item.AddThemeStyleboxOverride("hover_pressed", hover);
    }

    /// <summary><c>menu_setSkipState</c>: the label shows the switch, greyed out while skipping is not allowed.</summary>
    private void UpdateSkipItem()
    {
        _skipItem.Text = _skip.Activated ? "MsgSkip[on]" : "MsgSkip[off]";
        _skipItem.Disabled = !_skip.Enabled;
    }

    /// <summary>The menu bar's hot keys: F1 MsgSkip and F4 full screen (on release, as xsystem35's), Alt+R Restart.</summary>
    private bool MenuHotKey(InputEventKey key)
    {
        switch (key.Keycode)
        {
            case Godot.Key.F1 when !key.Pressed:
                _skip.Toggle();
                return true;
            case Godot.Key.F4 when !key.Pressed:
                SetFullscreen(DisplayServer.WindowGetMode() is not DisplayServer.WindowMode.Fullscreen
                    and not DisplayServer.WindowMode.ExclusiveFullscreen);
                return true;
            case Godot.Key.R when key.Pressed && key.AltPressed:
                ConfirmRestart();
                return true;
            case Godot.Key.F1 or Godot.Key.F4:
                return true;
        }
        return false;
    }

    /// <summary>Saved for later launches (<see cref="ScreenModeChoice"/>).</summary>
    private static void SetFullscreen(bool on) => ScreenModeChoice.Set(on);

    /// <summary>Save Screenshot: the screen as it is shown (event CGs not masked), 1280×800, in <c>user://screenshots</c>.</summary>
    private void SaveScreenshot()
    {
        string dir = ProjectSettings.GlobalizePath("user://screenshots");
        System.IO.Directory.CreateDirectory(dir);
        string path = System.IO.Path.Combine(dir, $"kichikuou-{DateTime.Now:yyyyMMdd-HHmmss}.png");
        _viewStale |= _presenter.Compose();                                    // a change left for later under our screens
        var image = Image.CreateFromData(Surface.ScreenWidth * Surface.Scale, Surface.ScreenHeight * Surface.Scale,
            false, Image.Format.Rgba8, _presenter.Screen.ReadScreen());
        image.SavePng(path);
        Notice("Screenshot saved: " + path);
    }

    /// <summary>
    /// Restart (<c>menu_resetmenu_open</c>): after the confirmation, back to the title; the next game
    /// starts on a fresh state, with the random generator and the message skip as they are
    /// (<see cref="EngineState"/>).
    /// </summary>
    private void ConfirmRestart() => Confirm(PhoneMenu.Command.Restart, Restart);

    private void Restart() => Screens.ToTitle(this);

    /// <summary>Exit (<c>menu_quitmenu_open</c>): after the confirmation, the program ends.</summary>
    private void ConfirmExit() => Confirm(PhoneMenu.Command.Exit, () => _tree.Quit());

    private MenuRowsWindow? _menuRows;

    /// <summary>Debug → Menu Row Height (ours, development): the slider's window, over the game, which goes on under it.</summary>
    private void OpenMenuRows()
    {
        if (IsInstanceValid(_menuRows))
        {
            _menuRows!.GrabFocus();
            return;
        }
        _menuRows = new MenuRowsWindow();
        AddChild(_menuRows);
        _menuRows.PopupCentered();
    }

    /// <summary>Debug → Battle Captures (ours, development): after the confirmation, the capture viewer, which comes back to the title.</summary>
    private void ConfirmCaptures() => Confirm(PhoneMenu.Command.BattleCaptures, OpenCaptures);

    private void OpenCaptures() => Screens.Open(this, new CaptureList());

    /// <summary>The Debug menu's checkable Hide Dimmed Map Names.</summary>
    private const int DimmedNamesItem = 4;

    /// <summary>
    /// Debug → Screen Gallery (ours, development; docs/ui/gallery.md): after the confirmation, our screens from the kept saves,
    /// which comes back to the title.
    /// </summary>
    private void ConfirmScreenGallery() =>
        Confirm(PhoneMenu.Command.ScreenGallery, OpenScreenGallery);

    private void OpenScreenGallery() => Screens.Open(this, new ScreenGallery.GalleryScreen());

    /// <summary>Debug → Portraits (ours, development): the faces our screens cut, over the game, which waits while it is open.</summary>
    private void OpenPortraits()
    {
        if (_modal)
            return;
        _modal = true;
        _input.Clear();
        var viewer = new PortraitViewer(GameData.Cgs(), _presenter.State);
        viewer.TreeExiting += () => _modal = false;
        AddChild(viewer);
        viewer.PopupCentered();
        if (_options.Portraits && _options.ScreenshotDirectory is { } shots)
            _ = SavePortraitsAsync(viewer, shots);
    }

    /// <summary>Development (<see cref="GameOptions.Portraits"/>): saves the viewer's units' list, then quits with <c>--quit</c>.</summary>
    private async Task SavePortraitsAsync(PortraitViewer viewer, string dir)
    {
        await viewer.SaveUnitsAsync(dir);
        if (_options.QuitWhenDone)
            _tree.Quit();
    }

    /// <summary>Debug → Effects (ours, development): our exchange effects over a battlefield, over the game, which waits while it is open.</summary>
    private void OpenEffects()
    {
        if (_modal)
            return;
        _modal = true;
        _input.Clear();
        var viewer = new EffectViewer(GameData.Cgs(), _presenter.State);
        viewer.TreeExiting += () => _modal = false;
        AddChild(viewer);
        viewer.Popup();     // where the viewer fits itself
        if (_options.Effects && _options.ScreenshotDirectory is { } shots)
            _ = SaveEffectsAsync(viewer, shots);
    }

    /// <summary>Development (<see cref="GameOptions.Effects"/>): saves the effect's frames, then quits with <c>--quit</c>.</summary>
    private async Task SaveEffectsAsync(EffectViewer viewer, string dir)
    {
        await viewer.SaveFramesAsync(dir, _options.EffectsOnly);
        if (_options.QuitWhenDone)
            _tree.Quit();
    }

    private OptionsView? _optionsView;

    /// <summary>
    /// Options (ours, docs/ui/options.md; the owner's mock-up D): the options window over the game, which waits while it is
    /// open. Its changes are kept at once; the screens follow them here, and the game's context follows "New scenes first"
    /// (decision 30). The game folder's change leaves the game for the import screen (and a fresh title after it); the
    /// developer's extract, which wins over any import (<see cref="GameData.UsesExtract"/>), locks it.
    /// </summary>
    private OptionsView? OpenOptions(OptionsView.Tab tab = OptionsView.Tab.Messages)
    {
        if (_modal)
            return null;
        _modal = true;
        _input.Clear();
        var view = new OptionsView(_skip, folderLocked: GameData.UsesExtract, tab)
        {
            ImportedGame = _script?.State.ImportedFromOriginal == true,  // "New scenes first" locked off (docs/save-import.md, §7)
        };
        view.TextSpeedChanged += step => _messageView.LettersPerSecond = TextSpeedChoice.LettersPerSecond(step);
        view.FontChosen += ApplyFont;
        view.ReadColourChosen += () => _messageView.QueueRedraw();
        view.PaletteChosen += () => UiPalettes.Repaint(this);    // every screen open recoloured at once
        view.ThousandsChosen += () => RedrawAll(this);   // the figures drawn already, in the new way
        view.NewScenesFirstChanged += on =>
        {
            if (_script is not null)
                _script.NewScenesOption = on;   // at once: the next talk or visit, the next patched statement
            _pickerMarks?.SetNewScenesFirst(on);  // the open picker's marks read with it
        };
        view.DemonReachChosen += reach =>
        {
            if (_script is not null)
                _script.DemonReach = reach;     // at once: the next exchange (decision 32)
        };
        view.NinjaChanceChosen += chance =>
        {
            if (_script is not null)
                _script.NinjaChance = chance;   // at once: the next exchange (decision 33)
        };
        view.GameFolderChosen += () => Screens.Open(this,
            new ImportScreen(GameData.SavedGameDirectory, start: false, next: Main.Title, back: Main.Title));
        view.MarginsAdjust += () => OpenMargins(view);
        view.Closed += () =>
        {
            _optionsView = null;
            _modal = false;
            _input.Clear();
        };
        _optionsView = view;
        _content.AddChild(view);
        view.Place(PictureArea());
        return view;
    }

    private ScreenMarginsView? _marginsView;

    /// <summary>
    /// Option → Display → Screen margins (docs/ui/screen-margins.md, the owner's C): the options window hidden, the
    /// calibration over the screen it was opened from, which lays itself out inside the margins as they change
    /// (<see cref="UpdateDevice"/>); closed, the window shows again, laid out in the new room. The game still waits.
    /// </summary>
    private void OpenMargins(OptionsView options)
    {
        if (_marginsView is not null)
            return;
        options.Visible = false;
        var view = new ScreenMarginsView();
        view.Closed += () =>
        {
            _marginsView = null;
            options.Visible = true;
            options.QueueRedraw();
        };
        _marginsView = view;
        AddChild(view);                                     // over the whole screen, not in the room inside the margins
    }

    /// <summary>
    /// A font chosen in the options (saved already): the windows, menus and pickers are drawn again at once, text the
    /// script printed on the stage keeps its font until it is drawn again.
    /// </summary>
    private void ApplyFont(GameFont font)
    {
        _glyphs.Font = font;
        _table.SetFont(font);
        _markers.SetFont(font);
        _provinceView.SetFont(font);
        _saveView.SetFont(font);
        _panels.SetFont(font);
        _subordinates.SetFont(font);
        _harem.SetFont(font);
        _treasure.SetFont(font);
        _treasureCallout.SetFont(font);
        _battleView.SetFont(font);
        _attackView.SetFont(font);
        _strategyView.SetFont(font);
        _messageView.SetFont(font);
        _sceneMenuView.SetFont(font);
        _ladderView.SetFont(font);
        _dataView.SetFont(font);
        _presenter.Invalidate();
    }

    /// <summary>Every canvas item of the screen drawn again (the thousands' separator chosen).</summary>
    private static void RedrawAll(Node node)
    {
        if (node is CanvasItem item)
            item.QueueRedraw();
        foreach (var child in node.GetChildren())
            RedrawAll(child);
    }

    /// <summary>
    /// <c>confirm_lose_progress</c>: a modal box asking <paramref name="command"/>'s question (<see cref="PhoneMenu.QuestionOf"/>,
    /// which the phone's Menu sheet asks in its callout); the game waits while it is open.
    /// </summary>
    private void Confirm(PhoneMenu.Command command, Action confirmed)
    {
        if (PhoneMenu.QuestionOf(command) is { } question)
            Ask(question, confirmed);
    }

    /// <summary>The modal box of <see cref="Confirm"/> with <paramref name="question"/> (Import Original Save's too).</summary>
    private void Ask(PhoneMenu.Question question, Action confirmed)
    {
        if (_modal)
            return;
        var dialog = new ConfirmationDialog
        {
            Title = question.Title,
            DialogText = question.Line,
            OkButtonText = question.Confirm,
        };
        _modal = true;
        _input.Clear();
        dialog.Confirmed += () =>
        {
            _modal = false;
            dialog.QueueFree();
            confirmed();
        };
        dialog.Canceled += () =>
        {
            _modal = false;
            dialog.QueueFree();
        };
        AddChild(dialog);
        dialog.PopupCentered();
    }
}
