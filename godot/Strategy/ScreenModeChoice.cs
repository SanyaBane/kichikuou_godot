using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Screen → Window / Fullscreen (and F4), kept for later launches (owner, 2026-10-24): <c>[display] fullscreen</c> in
/// <c>user://settings.cfg</c>. Before the player has chosen, a launch opens as the project or the export preset says
/// (full screen on Android, docs/backlog.md, "Android"). On Android, full screen hides the status and navigation bars.
/// On a desktop the window's size is kept too, <c>[display] window_size</c>: the size the player last gave the window
/// (not maximised, not full screen), the window centred on its screen at the launch.
/// </summary>
internal static class ScreenModeChoice
{
    /// <summary>How long a size must stay before it is saved: a drag of the window's border resizes it every frame.</summary>
    private const double SaveDelay = 0.5;

    private static bool _applied;
    private static int _resizes;

    /// <summary>Switches the window to <paramref name="fullscreen"/> or out of it and saves the choice.</summary>
    public static void Set(bool fullscreen)
    {
        SetMode(fullscreen);
        Save("fullscreen", fullscreen);
    }

    /// <summary>
    /// The saved choices applied, once at the launch, and the window's size watched from then on. A launch with
    /// development switches (unattended play, screen dumps) or headless keeps the window it opens with.
    /// </summary>
    public static void Apply(Node node)
    {
        if (_applied)
            return;
        _applied = true;
        if (CommandLine.Args.Length > 0 || DisplayServer.GetName() == "headless")
            return;
        var config = new ConfigFile();
        bool loaded = config.Load(GameData.SettingsPath) == Error.Ok;
        if (OS.HasFeature("pc"))
        {
            // Set first, while the window is still a window: leaving full screen later goes back to it.
            if (loaded && config.GetValue("display", "window_size", default(Vector2I)).AsVector2I() is { X: > 0, Y: > 0 } size)
                Resize(size);
            var tree = node.GetTree();
            tree.Root.SizeChanged += () => SaveSizeSoon(tree);
        }
        if (loaded && config.HasSectionKey("display", "fullscreen"))
            SetMode((bool)config.GetValue("display", "fullscreen"));
    }

    /// <summary>The window at <paramref name="size"/>, no larger than its screen's usable area, centred in it.</summary>
    private static void Resize(Vector2I size)
    {
        var usable = DisplayServer.ScreenGetUsableRect(DisplayServer.WindowGetCurrentScreen());
        size = new Vector2I(Math.Min(size.X, usable.Size.X), Math.Min(size.Y, usable.Size.Y));
        DisplayServer.WindowSetSize(size);
        DisplayServer.WindowSetPosition(usable.Position + (usable.Size - size) / 2);
        GD.Print($"Kichikuou: window {size.X}×{size.Y}, as saved.");
    }

    /// <summary>The window's size saved once it has stayed <see cref="SaveDelay"/>, read then (a change of mode passes through other sizes).</summary>
    private static void SaveSizeSoon(SceneTree tree)
    {
        int resize = ++_resizes;
        tree.CreateTimer(SaveDelay).Timeout += () =>
        {
            if (resize == _resizes && DisplayServer.WindowGetMode() == DisplayServer.WindowMode.Windowed)
                Save("window_size", DisplayServer.WindowGetSize());
        };
    }

    private static void Save(string key, Variant value)
    {
        var config = new ConfigFile();
        config.Load(GameData.SettingsPath);
        config.SetValue("display", key, value);
        config.Save(GameData.SettingsPath);
    }

    private static void SetMode(bool fullscreen) =>
        DisplayServer.WindowSetMode(fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
}
