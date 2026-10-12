using Godot;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Interface (owner's decision 25): the original's screens, or our own where there is one
/// over the original layout — so far the deployment table (docs/ui/deployment-table.md).
/// </summary>
internal static class InterfaceChoice
{
    private static bool? _new;

    /// <summary>
    /// Whether our own screens are used: <c>--interface original|new</c> for the launch (development),
    /// else <c>[display] interface</c> in <c>user://settings.cfg</c>, else the original's. Setting it
    /// saves it for later launches.
    /// </summary>
    public static bool New
    {
        get
        {
            if (_new is { } chosen)
                return chosen;
            var args = OS.GetCmdlineUserArgs();
            int at = Array.IndexOf(args, "--interface");
            if (at >= 0 && at + 1 < args.Length && Parse(args[at + 1]) is { } given)
                return (_new = given).Value;
            var config = new ConfigFile();
            bool saved = config.Load(GameData.SettingsPath) == Error.Ok && Parse((string)config.GetValue("display", "interface", "")) == true;
            return (_new = saved).Value;
        }
        set
        {
            _new = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "interface", value ? "new" : "original");
            config.Save(GameData.SettingsPath);
        }
    }

    private static bool? Parse(string name) => name switch
    {
        "original" => false,
        "new" => true,
        _ => null,
    };
}
