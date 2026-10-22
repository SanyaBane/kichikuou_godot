using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Game → "New scenes first" (owner's decision 30, docs/ui/talk-marks.md §5, on by default): the player's
/// switch for the rule change, which <c>GameScreen</c> copies onto the game's context
/// (<see cref="Kichikuou.Original.Script.OriginalScriptContext.NewScenesFirst"/>) when the session starts and whenever
/// it is changed. The rule is the rule set's; this only keeps the player's choice.
/// </summary>
internal static class NewScenesChoice
{
    private static bool? _on;

    /// <summary>Whether it is on: <c>[game] new_scenes_first</c> in <c>user://settings.cfg</c>, else it is. Setting it saves it for later launches.</summary>
    public static bool On
    {
        get
        {
            if (_on is { } chosen)
                return chosen;
            var config = new ConfigFile();
            bool saved = config.Load(GameData.SettingsPath) != Error.Ok || (bool)config.GetValue("game", "new_scenes_first", true);
            return (_on = saved).Value;
        }
        set
        {
            _on = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("game", "new_scenes_first", value);
            config.Save(GameData.SettingsPath);
        }
    }
}
