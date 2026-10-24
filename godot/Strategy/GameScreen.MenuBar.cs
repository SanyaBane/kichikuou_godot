// The menu's texts are taken from xsystem35 (https://github.com/kichikuou/xsystem35-sdl2): menu.c
// (the dialogs), Copyright (C) 2000- Masaki Chikama (Wren), and win/resources.rc (the labels) by the
// xsystem35-sdl2 authors. GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles;
using Kichikuou.Game.Battles.Effects;

namespace Kichikuou.Game.Strategy;

// xsystem35's window menu bar over the game (docs/window-menu.md): File (Save Screenshot, Restart,
// Exit), Screen (Window, Fullscreen), Option (ours: the options window, OptionsView, with xsystem35's Mouse
// Movement among its settings), MsgSkip[off|on]; hot keys F1 and F4 as xsystem35's, Alt+R; and ours for development, Debug
// (Battle Captures: the capture viewer, which our own title had until ｽﾀMENU replaced it; Portraits,
// Effects, Hide Dimmed Map Names).
public partial class GameScreen
{
    private const int MenuBarHeight = 24;

    private static readonly Color BarColor = new(0.94f, 0.94f, 0.94f);
    private static readonly Color HoverColor = new(0.8f, 0.88f, 0.97f);

    private Button _skipItem = null!;

    private void BuildMenuBar()
    {
        var bar = new PanelContainer { MouseFilter = MouseFilterEnum.Stop };
        bar.AddThemeStyleboxOverride("panel", new StyleBoxFlat { BgColor = BarColor, ContentMarginLeft = 4 });
        bar.SetAnchorsAndOffsetsPreset(LayoutPreset.TopWide);
        bar.OffsetBottom = MenuBarHeight;
        AddChild(bar);
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 0);
        bar.AddChild(row);

        row.AddChild(Menu("File",
            ("Save Screenshot", SaveScreenshot),
            ("Restart", ConfirmRestart),
            ("Exit", ConfirmExit)));
        row.AddChild(Menu("Screen",
            ("Window", () => SetFullscreen(false)),
            ("Fullscreen", () => SetFullscreen(true))));
        var option = new Button { Text = "Option" };
        BarItem(option);
        option.Pressed += () => OpenOptions();
        row.AddChild(option);
        _skipItem = new Button();
        BarItem(_skipItem);
        _skipItem.Pressed += _skip.Toggle;
        row.AddChild(_skipItem);
        _skip.Changed += UpdateSkipItem;
        UpdateSkipItem();
        PopupMenu debug = null!;
        var debugMenu = Menu("Debug",
            ("Battle Captures...", ConfirmCaptures),
            ("Portraits...", OpenPortraits),
            ("Effects...", OpenEffects),
            ("Hide Dimmed Map Names", ToggleDimmedNames));
        debug = debugMenu.GetPopup();
        debug.SetItemAsCheckable(3, true);
        debug.SetItemChecked(3, MapNamesChoice.HideDimmed);
        row.AddChild(debugMenu);
        // Saved for later launches; the map's plates follow at once.
        void ToggleDimmedNames()
        {
            MapNamesChoice.HideDimmed = !MapNamesChoice.HideDimmed;
            debug.SetItemChecked(3, MapNamesChoice.HideDimmed);
            _markers.SetHideDimmed(MapNamesChoice.HideDimmed);
        }
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
    private void ConfirmRestart() => Confirm("Restart game?", "Restart", () => Screens.ToTitle(this));

    /// <summary>Exit (<c>menu_quitmenu_open</c>): after the confirmation, the program ends.</summary>
    private void ConfirmExit() => Confirm("Quit game?", "Quit", () => _tree.Quit());

    /// <summary>Debug → Battle Captures (ours, development): after the confirmation, the capture viewer, which comes back to the title.</summary>
    private void ConfirmCaptures() => Confirm("Open the battle captures?", "Open", () => Screens.Open(this, new CaptureList()));

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
        viewer.PopupCentered();
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
    /// Option (ours, docs/ui/options.md; the owner's mock-up D): the options window over the game, which waits while it is
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
        var view = new OptionsView(_skip, folderLocked: GameData.UsesExtract, tab);
        view.TextSpeedChanged += step => _messageView.LettersPerSecond = TextSpeedChoice.LettersPerSecond(step);
        view.FontChosen += ApplyFont;
        view.ReadColourChosen += () => _messageView.QueueRedraw();
        view.PaletteChosen += ApplyPalette;
        view.ThousandsChosen += () => RedrawAll(this);   // the figures drawn already, in the new way
        view.NewScenesFirstChanged += on =>
        {
            if (_script is not null)
                _script.NewScenesFirst = on;    // at once: the next talk or visit, the next patched statement
            _pickerMarks?.SetNewScenesFirst(on);  // the open picker's marks read with it
        };
        view.GameFolderChosen += () => Screens.Open(this,
            new ImportScreen(GameData.SavedGameDirectory, start: false, next: Main.Title, back: Main.Title));
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
        _scoutView.SetFont(font);
        _attackView.SetFont(font);
        _strategyView.SetFont(font);
        _messageView.SetFont(font);
        _sceneMenuView.SetFont(font);
        _presenter.Invalidate();
    }

    /// <summary>
    /// A palette chosen in the options (saved already): our battle screen's and the map plates' colours (the deployment
    /// screen is lacquer since the owner's E1/E2), an open screen recoloured at once.
    /// </summary>
    private static void RedrawAll(Node node)
    {
        if (node is CanvasItem item)
            item.QueueRedraw();
        foreach (var child in node.GetChildren())
            RedrawAll(child);
    }

    private void ApplyPalette(DeploymentPalette palette)
    {
        _markers.SetPalette(palette);
        _commandsButton.SetPalette(palette);
        _skipButton.SetPalette(palette);
        _battleView.SetPalette(palette);
        _scoutView.SetPalette(palette);
        _strategyView.SetPalette(palette);
    }

    /// <summary><c>confirm_lose_progress</c>: a modal box; the game waits while it is open.</summary>
    private void Confirm(string title, string confirm, Action confirmed, string text = "Any unsaved progress will be lost.")
    {
        if (_modal)
            return;
        var dialog = new ConfirmationDialog
        {
            Title = title,
            DialogText = text,
            OkButtonText = confirm,
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
