using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Sound[on|off] on the menu bar (ours, owner, 2026-10-24; docs/window-menu.md): the game's music and sound effects
/// heard or not, kept for later launches — <c>[audio] sound</c> in <c>user://settings.cfg</c>, on by default. Off mutes
/// the Master bus, so the players go on playing unheard: the music's position the script reads (<c>SC</c>) and every
/// wait stay as they are.
/// </summary>
internal static class SoundChoice
{
    private static bool? _on;

    /// <summary>Whether the sound is heard. Setting it mutes or unmutes at once and saves it for later launches.</summary>
    public static bool On
    {
        get
        {
            if (_on is { } chosen)
                return chosen;
            var config = new ConfigFile();
            bool saved = config.Load(GameData.SettingsPath) != Error.Ok || (bool)config.GetValue("audio", "sound", true);
            return (_on = saved).Value;
        }
        set
        {
            _on = value;
            Apply();
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("audio", "sound", value);
            config.Save(GameData.SettingsPath);
        }
    }

    /// <summary>The choice on the Master bus (at the launch, and after each change).</summary>
    public static void Apply() => AudioServer.SetBusMute(AudioServer.GetBusIndex("Master"), !On);
}
