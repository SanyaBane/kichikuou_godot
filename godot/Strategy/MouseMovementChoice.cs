using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Messages → Mouse movement (xsystem35's <c>ags.mouse_warp_enabled</c>, on at start as its <c>menu_init</c>;
/// docs/window-menu.md): the script's <c>IZ</c> — after a menu, the pointer put back where it was — moves the system
/// pointer; off, only the position the script reads. Kept for later launches (owner, 2026-10-27; for the launch before).
/// The touch mode does not offer it and has it off: a finger leaves no pointer to put back, and a warped one would be
/// the script's alone (owner, 2026-10-27).
/// </summary>
internal static class MouseMovementChoice
{
    private static bool? _on;

    /// <summary>
    /// Whether <c>IZ</c> moves the system pointer: never in the touch mode, else <c>[input] mouse_movement</c> in
    /// <c>user://settings.cfg</c>, else it does. Setting it saves it for later launches.
    /// </summary>
    public static bool On
    {
        get
        {
            if (TouchMode.On)
                return false;
            if (_on is { } chosen)
                return chosen;
            var config = new ConfigFile();
            bool saved = config.Load(GameData.SettingsPath) != Error.Ok || (bool)config.GetValue("input", "mouse_movement", true);
            return (_on = saved).Value;
        }
        set
        {
            _on = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("input", "mouse_movement", value);
            config.Save(GameData.SettingsPath);
        }
    }
}
