using Godot;
using Kichikuou.Original.Combat;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Option → Tweaks → "Ninja follow-up" (owner's decision 33, docs/tweaks.md, the original's 1 in 3 by default): the
/// player's chance of the ninja's follow-up, which <c>GameScreen</c> copies onto the game's context
/// (<see cref="Kichikuou.Original.Script.OriginalScriptContext.NinjaChance"/>) when the session starts and whenever it is
/// changed. The rule is the rule set's; this only keeps the player's choice: <c>[tweaks] ninja_chance</c> in
/// <c>user://settings.cfg</c> (<c>original</c>, <c>two_in_three</c>, <c>always</c>), or
/// <c>--ninja-chance original|two-in-three|always</c> for a launch (development).
/// </summary>
internal static class NinjaChanceChoice
{
    private static readonly (NinjaChance Chance, string Key)[] Keys =
        [(NinjaChance.Original, "original"), (NinjaChance.TwoInThree, "two_in_three"), (NinjaChance.Always, "always")];

    private static NinjaChance? _chosen;

    /// <summary>The chosen chance; setting it saves it for later launches.</summary>
    public static NinjaChance Chosen
    {
        get
        {
            if (_chosen is { } chosen)
                return chosen;
            var args = OS.GetCmdlineUserArgs();
            int at = Array.IndexOf(args, "--ninja-chance");
            string? key = at >= 0 && at + 1 < args.Length ? args[at + 1].Replace('-', '_') : null;
            if (key is null)
            {
                var config = new ConfigFile();
                key = config.Load(GameData.SettingsPath) == Error.Ok ? (string)config.GetValue("tweaks", "ninja_chance", "original") : null;
            }
            return (_chosen = Keys.FirstOrDefault(k => k.Key == key).Chance).Value;
        }
        set
        {
            _chosen = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("tweaks", "ninja_chance", Keys.First(k => k.Chance == value).Key);
            config.Save(GameData.SettingsPath);
        }
    }
}
