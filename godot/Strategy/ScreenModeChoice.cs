using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Screen → Window / Fullscreen (and F4), kept for later launches (owner, 2026-10-24): <c>[display] fullscreen</c> in
/// <c>user://settings.cfg</c>. Before the player has chosen, a launch opens as the project or the export preset says
/// (full screen on Android, docs/backlog.md, "Android"). On Android, full screen hides the status and navigation bars.
/// </summary>
internal static class ScreenModeChoice
{
    private static bool _applied;

    /// <summary>Switches the window to <paramref name="fullscreen"/> or out of it and saves the choice.</summary>
    public static void Set(bool fullscreen)
    {
        SetMode(fullscreen);
        var config = new ConfigFile();
        config.Load(GameData.SettingsPath);
        config.SetValue("display", "fullscreen", fullscreen);
        config.Save(GameData.SettingsPath);
    }

    /// <summary>
    /// The saved choice applied, once at the launch. A launch with development switches (unattended play, screen
    /// dumps) or headless keeps the window it opens with.
    /// </summary>
    public static void Apply()
    {
        if (_applied)
            return;
        _applied = true;
        if (OS.GetCmdlineUserArgs().Length > 0 || DisplayServer.GetName() == "headless")
            return;
        var config = new ConfigFile();
        if (config.Load(GameData.SettingsPath) == Error.Ok && config.HasSectionKey("display", "fullscreen"))
            SetMode((bool)config.GetValue("display", "fullscreen"));
    }

    private static void SetMode(bool fullscreen) =>
        DisplayServer.WindowSetMode(fullscreen ? DisplayServer.WindowMode.Fullscreen : DisplayServer.WindowMode.Windowed);
}
