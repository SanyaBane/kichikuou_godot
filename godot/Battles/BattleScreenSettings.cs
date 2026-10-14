using Godot;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The settings of our own battle screen (Option → Battle Screen…, <see cref="BattleScreenSettingsDialog"/>),
/// kept in <c>[battle_screen]</c> of <c>user://settings.cfg</c>; a change applies at once. So far the order
/// strip's moves (<see cref="OrderStrip"/>), the armies' slides, the spotlight's darkness and the cards' counters (<see cref="BattleBoardView"/>).
/// </summary>
internal static class BattleScreenSettings
{
    private const string Section = "battle_screen";

    /// <summary>A setting: its key in the file, its label, its range and step, its default and its unit.</summary>
    public sealed record Setting(string Key, string Label, double Min, double Max, double Step, double Default, string Unit, string Hint);

    /// <summary>How long a change of the order moves; 0 places the chips at once.</summary>
    public static readonly Setting StripSeconds = new("strip_seconds", "Order strip: move", 0, 3, 0.05, 1.0, "s",
        "How long a chip takes to reach its new place when the order changes (0: at once).");

    /// <summary>How fast a move under way finishes when the next change comes.</summary>
    public static readonly Setting StripHurrySeconds = new("strip_hurry_seconds", "Order strip: hurry", 0, 1, 0.01, 0.15, "s",
        "When the order changes again (or the next unit acts) while chips still move, they finish within this.");

    /// <summary>The share of a move the jumping chip spends going down a row, and as much coming up.</summary>
    public static readonly Setting StripDropShare = new("strip_drop_share", "Order strip: drop and rise", 0.05, 0.45, 0.01, 0.2, "%",
        "The part of the move the jumping chip spends going down a row, and again coming up into its place.");

    /// <summary>How long an army takes to slide into the scene or out of it (Sengoku Rance's slides).</summary>
    public static readonly Setting ArmySeconds = new("army_seconds", "Armies: slide", 0, 3, 0.05, 0.2, "s",
        "How long an army takes to slide into the scene from its side, or out of it (0: at once). The exchange waits for it.");

    /// <summary>How dark the panel below the scene gets around the two fighters' cards during an exchange.</summary>
    public static readonly Setting SpotDarkness = new("spot_darkness", "Spotlight: darkness", 0, 1, 0.05, 0.5, "%",
        "During an exchange, how dark the panel below the scene gets but the two fighters' cards (0: not at all).");

    /// <summary>How long a card's troops and HP take to run to a new value.</summary>
    public static readonly Setting MeterSeconds = new("meter_seconds", "Cards: counters", 0, 3, 0.05, 0.6, "s",
        "How long a card's troops and HP, the number and the bar, take to run to a new value after an exchange (0: at once).");

    public static IReadOnlyList<Setting> All { get; } = [StripSeconds, StripHurrySeconds, StripDropShare, ArmySeconds, SpotDarkness, MeterSeconds];

    /// <summary>The development switches that set a setting for the launch.</summary>
    private static readonly (string Switch, Setting Setting)[] Switches =
        [("--strip-seconds", StripSeconds), ("--army-seconds", ArmySeconds), ("--meter-seconds", MeterSeconds)];

    private static Dictionary<string, double>? _values;

    /// <summary>
    /// A setting's value: <c>--strip-seconds x</c> / <c>--army-seconds x</c> / <c>--meter-seconds x</c> for the launch (development) for
    /// <see cref="StripSeconds"/> / <see cref="ArmySeconds"/> / <see cref="MeterSeconds"/>, else the file's, else the default.
    /// </summary>
    public static double Get(Setting setting)
    {
        if (_values is null)
        {
            _values = [];
            var config = new ConfigFile();
            bool loaded = config.Load(GameData.SettingsPath) == Error.Ok;
            foreach (var s in All)
                _values[s.Key] = loaded ? Math.Clamp((double)config.GetValue(Section, s.Key, s.Default), s.Min, s.Max) : s.Default;
            var args = OS.GetCmdlineUserArgs();
            foreach (var (name, s) in Switches)
            {
                int at = Array.IndexOf(args, name);
                if (at >= 0 && at + 1 < args.Length
                    && double.TryParse(args[at + 1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double given))
                    _values[s.Key] = Math.Clamp(given, s.Min, s.Max);
            }
        }
        return _values[setting.Key];
    }

    /// <summary>Sets a setting and saves it for later launches.</summary>
    public static void Set(Setting setting, double value)
    {
        Get(setting);
        _values![setting.Key] = Math.Clamp(value, setting.Min, setting.Max);
        var config = new ConfigFile();
        config.Load(GameData.SettingsPath);
        config.SetValue(Section, setting.Key, _values[setting.Key]);
        config.Save(GameData.SettingsPath);
    }
}
