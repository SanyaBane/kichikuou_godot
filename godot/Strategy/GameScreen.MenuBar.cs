// The menu's texts are taken from xsystem35 (https://github.com/kichikuou/xsystem35-sdl2): menu.c
// (the dialogs), Copyright (C) 2000- Masaki Chikama (Wren), and win/resources.rc (the labels) by the
// xsystem35-sdl2 authors. GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

using Godot;
using Kichikuou.Content.Screen;
using Kichikuou.Game.Battles;

namespace Kichikuou.Game.Strategy;

// xsystem35's window menu bar over the game (docs/window-menu.md): File (Save Screenshot, Restart,
// Exit), Screen (Window, Fullscreen), Option (Mouse Movement as xsystem35's; ours: Skip Unseen
// Text, MsgSkip Speeds Up Fades, Forget Seen Messages, Fonts, Interface),
// MsgSkip[off|on]; hot keys F1 and F4 as xsystem35's, Alt+R; and ours for development, Debug
// (Battle Captures: the capture viewer, which our own title had until ｽﾀMENU replaced it).
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
        PopupMenu options = null!;
        var option = Menu("Option",
            ("Mouse Movement", ToggleMouseMovement),
            ("Skip Unseen Text", ToggleSkipUnseen),
            ("MsgSkip Speeds Up Fades", ToggleFastFades),
            ("Forget Seen Messages...", ConfirmForgetSeen));
        options = option.GetPopup();
        options.SetItemAsCheckable(0, true);
        options.SetItemChecked(0, EngineState.MouseMovement);
        options.SetItemAsCheckable(1, true);
        options.SetItemChecked(1, _skip.SkipUnseen);
        options.SetItemAsCheckable(2, true);
        options.SetItemChecked(2, _skip.FastFades);
        PopupMenu fonts = null!;
        fonts = Submenu(
            ("MS Gothic", () => SetFont(GameFont.MsGothic)),
            ("BIZ UDGothic", () => SetFont(GameFont.BizUdGothic)));
        fonts.SetItemAsRadioCheckable(0, true);
        fonts.SetItemAsRadioCheckable(1, true);
        fonts.SetItemDisabled(0, !GodotGlyphRenderer.MsGothicInstalled);
        // Only the chosen font is marked: the unchecked mark is blank, as wide as the checked one.
        var mark = fonts.GetThemeIcon("radio_checked").GetSize();
        var blank = ImageTexture.CreateFromImage(
            Image.CreateEmpty(Math.Max(1, (int)mark.X), Math.Max(1, (int)mark.Y), false, Image.Format.Rgba8));
        fonts.AddThemeIconOverride("radio_unchecked", blank);
        fonts.AddThemeIconOverride("radio_unchecked_disabled", blank);
        CheckFont();
        options.AddSubmenuNodeItem(Padded("Fonts"), fonts, SubmenuId);
        PopupMenu interfaces = null!;
        interfaces = Submenu(
            ("Original", () => SetInterface(false)),
            ("New", () => SetInterface(true)));
        interfaces.SetItemAsRadioCheckable(0, true);
        interfaces.SetItemAsRadioCheckable(1, true);
        interfaces.AddThemeIconOverride("radio_unchecked", blank);
        CheckInterface();
        options.AddSubmenuNodeItem(Padded("Interface"), interfaces, SubmenuId);
        void ToggleMouseMovement()
        {
            EngineState.MouseMovement = !EngineState.MouseMovement;
            options.SetItemChecked(0, EngineState.MouseMovement);
        }
        void ToggleSkipUnseen()
        {
            _skip.SkipUnseen = !_skip.SkipUnseen;
            options.SetItemChecked(1, _skip.SkipUnseen);
        }
        void ToggleFastFades()
        {
            _skip.FastFades = !_skip.FastFades;
            options.SetItemChecked(2, _skip.FastFades);
        }
        // Saved for later launches; the windows, menus and pickers are drawn again at once, text the
        // script printed on the stage keeps its font until it is drawn again.
        void SetFont(GameFont font)
        {
            GodotGlyphRenderer.Chosen = font;
            _glyphs.Font = font;
            _table.SetFont(font);
            CheckFont();
            _presenter.Invalidate();
        }
        void CheckFont()
        {
            fonts.SetItemChecked(0, _glyphs.Font == GameFont.MsGothic);
            fonts.SetItemChecked(1, _glyphs.Font == GameFont.BizUdGothic);
        }
        // Saved for later launches; an open screen of ours appears or goes at once (UpdateTable).
        void SetInterface(bool ours)
        {
            InterfaceChoice.New = ours;
            CheckInterface();
        }
        void CheckInterface()
        {
            interfaces.SetItemChecked(0, !InterfaceChoice.New);
            interfaces.SetItemChecked(1, InterfaceChoice.New);
        }
        row.AddChild(option);
        _skipItem = new Button();
        BarItem(_skipItem);
        _skipItem.Pressed += _skip.Toggle;
        row.AddChild(_skipItem);
        _skip.Changed += UpdateSkipItem;
        UpdateSkipItem();
        row.AddChild(Menu("Debug",
            ("Battle Captures...", ConfirmCaptures)));
    }

    /// <summary>The id of an item that opens a submenu: past every item's index, so it runs nothing.</summary>
    private const int SubmenuId = 1000;

    private static MenuButton Menu(string title, params (string Text, Action Run)[] items)
    {
        var menu = new MenuButton { Text = title };
        BarItem(menu);
        Fill(menu.GetPopup(), items);
        return menu;
    }

    /// <summary>A submenu (<c>AddSubmenuNodeItem</c>), looking as the bar's menus do.</summary>
    private static PopupMenu Submenu(params (string Text, Action Run)[] items)
    {
        var popup = new PopupMenu();
        Fill(popup, items);
        return popup;
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

    private static void SetFullscreen(bool on) =>
        DisplayServer.WindowSetMode(on ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);

    /// <summary>Save Screenshot: the screen as it is shown (event CGs not masked), 1280×800, in <c>user://screenshots</c>.</summary>
    private void SaveScreenshot()
    {
        string dir = ProjectSettings.GlobalizePath("user://screenshots");
        System.IO.Directory.CreateDirectory(dir);
        string path = System.IO.Path.Combine(dir, $"kichikuou-{DateTime.Now:yyyyMMdd-HHmmss}.png");
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

    /// <summary>Forget Seen Messages (ours): after the confirmation, every message counts as unseen again.</summary>
    private void ConfirmForgetSeen() =>
        Confirm("Forget seen messages?", "Forget", () => _skip.ForgetSeen(),
            $"All {_skip.SeenCount} messages seen so far will count as unseen again.");

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
