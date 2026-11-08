using Godot;
using Kichikuou.Original.Combat;

namespace Kichikuou.Game.Battles;

/// <summary>
/// Option → Tweaks → "Chaos and Nikkou reach demons" (owner's decision 32, docs/tweaks.md, off by default): the player's
/// choice of BRS-24's fix, which <c>GameScreen</c> copies onto the game's context
/// (<see cref="Kichikuou.Original.Script.OriginalScriptContext.DemonReach"/>) when the session starts and whenever it is
/// changed, and the damage forecast passes to its copy. The rule is the rule set's; this only keeps the player's choice:
/// <c>[tweaks] demon_reach</c> in <c>user://settings.cfg</c> (<c>original</c>, <c>on_field</c>, <c>out_of_reach</c>), or
/// <c>--demon-reach original|on-field|out-of-reach</c> for a launch (development).
/// </summary>
internal static class DemonReachChoice
{
    private static readonly (DemonReach Reach, string Key)[] Keys =
        [(DemonReach.Original, "original"), (DemonReach.OnField, "on_field"), (DemonReach.OutOfReach, "out_of_reach")];

    private static DemonReach? _chosen;

    /// <summary>The chosen rule; setting it saves it for later launches.</summary>
    public static DemonReach Chosen
    {
        get
        {
            if (_chosen is { } chosen)
                return chosen;
            var args = CommandLine.Args;
            int at = Array.IndexOf(args, "--demon-reach");
            string? key = at >= 0 && at + 1 < args.Length ? args[at + 1].Replace('-', '_') : null;
            if (key is null)
            {
                var config = new ConfigFile();
                key = config.Load(GameData.SettingsPath) == Error.Ok ? (string)config.GetValue("tweaks", "demon_reach", "original") : null;
            }
            return (_chosen = Keys.FirstOrDefault(k => k.Key == key).Reach).Value;
        }
        set
        {
            _chosen = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("tweaks", "demon_reach", Keys.First(k => k.Reach == value).Key);
            config.Save(GameData.SettingsPath);
        }
    }
}
