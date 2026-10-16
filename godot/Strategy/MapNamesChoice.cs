using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Hide Dimmed Map Names (owner's request 2026-10-16): our map's plates of the provinces
/// neither ours nor next to ours and of the dungeons not ours — the dimmed ones — are not shown
/// (docs/ui/command-screen.md, "The map's markers").
/// </summary>
internal static class MapNamesChoice
{
    private static bool? _hideDimmed;

    /// <summary>Whether the dimmed plates are hidden: <c>[display] hide_dimmed_map_names</c> in <c>user://settings.cfg</c>, else not. Setting it saves it for later launches.</summary>
    public static bool HideDimmed
    {
        get
        {
            if (_hideDimmed is { } chosen)
                return chosen;
            var config = new ConfigFile();
            bool saved = config.Load(GameData.SettingsPath) == Error.Ok && (bool)config.GetValue("display", "hide_dimmed_map_names", false);
            return (_hideDimmed = saved).Value;
        }
        set
        {
            _hideDimmed = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "hide_dimmed_map_names", value);
            config.Save(GameData.SettingsPath);
        }
    }
}
