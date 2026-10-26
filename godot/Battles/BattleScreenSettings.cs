using Godot;

namespace Kichikuou.Game.Battles;

/// <summary>
/// The settings of our own battle screen (Option → Battle, <see cref="Strategy.OptionsView"/>),
/// kept in <c>[battle_screen]</c> of <c>user://settings.cfg</c>; a change applies at once. So far the order
/// strip's moves (<see cref="OrderStrip"/>), the armies' slides, the spotlight's darkness, the cards' counters (<see cref="BattleBoardView"/>),
/// the exchange's counters (<see cref="ExchangeCountersView"/>), the exchange's result hold (<c>GameScreen</c>), the opening's leave (<see cref="BattleOpeningLayer"/>), the cards' slide and the strip's fly-in at a battle's start, the exchange's effects and the damage forecast (<see cref="BattleCommandLayer"/>).
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
        "How long a card's troops and HP, the number and the bar, take to run to a new value (an exchange's blow runs with the "
        + "scene's counters, Counters: run; 0: at once).");

    /// <summary>How long the exchange's counters on the scene run down, and the fighters' cards with them (owner, 2026-10-16).</summary>
    public static readonly Setting CountSeconds = new("count_seconds", "Counters: run", 0, 3, 0.1, 1.3, "s",
        "How long the troops and HP counters at the scene's foot take to count down an exchange's losses, in Sengoku Rance's "
        + "steps of 0.2 s; the two fighters' cards run with them (0: at once).");

    /// <summary>How long the battle's opening takes to leave after its click (the tints fade, the names part, the plate rises).</summary>
    public static readonly Setting OpeningSeconds = new("opening_seconds", "Opening: leave", 0, 3, 0.05, 0.5, "s",
        "How long the battle's opening takes to leave after the click: the colours fade, the armies' names part to the sides, "
        + "the plate rises (0: at once).");

    /// <summary>How long the front row's cards take to slide in when a battle begins (owner, 2026-10-15; Sengoku Rance's 1 s).</summary>
    public static readonly Setting CardsSlideSeconds = new("cards_slide_seconds", "Cards: slide in", 0, 3, 0.05, 1.0, "s",
        "When a battle begins, how long the front row's cards take to slide in from their sides under the opening; "
        + "the back row's come a little later; in a dungeon they fade in at their places as long (0: at once).");

    /// <summary>How far apart the order strip's flags fly in after the opening's click (owner, 2026-10-15; Sengoku Rance's 0.25 s).</summary>
    public static readonly Setting StripFlySeconds = new("strip_fly_seconds", "Strip: fly in", 0, 1, 0.01, 0.25, "s",
        "After the opening's click, the order strip's flags fly in from the right one after another, this far apart; "
        + "they fly as much slower or faster (0: at once).");

    /// <summary>How long the battle's opening takes to fade in over the field when the battle screen comes up.</summary>
    public static readonly Setting OpeningAppearSeconds = new("opening_appear_seconds", "Opening: appear", 0, 3, 0.05, 0.35, "s",
        "How long the battle's opening takes to fade in over the field when the battle screen comes up (0: at once).");

    /// <summary>How long the exchange's result stays before the battle goes on by itself (owner, 2026-10-14).</summary>
    public static readonly Setting ResultSeconds = new("result_seconds", "Results: hold", 0, 5, 0.05, 1.2, "s",
        "How long an exchange's result stays before the battle goes on by itself; a click or a key goes on sooner (0: at once). "
        + "A result with ANNIHILATED or UNABLE TO FIGHT still waits for the click.");

    /// <summary>Whether the exchanges play our effects (owner, 2026-10-14; Sengoku Rance's Combat Effect Display): 1 on, 0 off.</summary>
    public static readonly Setting Effects = new("effects", "Effects", 0, 1, 1, 1, Toggle,
        "Our attack effects over the scene from an exchange's shots, after Sengoku Rance's (melee, bow, magic). None while skipping.");

    /// <summary>
    /// Whether the attack banners (the touch mode's attack buttons) show the damage each attack is likely to deal (owner,
    /// 2026-10-26/27; docs/ui/damage-forecast.md, §7): 1 on, 0 off, off by default; our interface only.
    /// </summary>
    public static readonly Setting Forecast = new("damage_forecast", "Damage forecast", 0, 1, 1, 0, Toggle,
        "Each attack banner shows the damage it is likely to deal: the target's troops and its commander's HP, 9 times in 10.");

    /// <summary>The unit of an on/off setting (a check box in the dialog).</summary>
    public const string Toggle = "toggle";

    public static IReadOnlyList<Setting> All { get; } =
        [StripSeconds, StripHurrySeconds, StripDropShare, ArmySeconds, SpotDarkness, MeterSeconds, CountSeconds, ResultSeconds, OpeningAppearSeconds, OpeningSeconds,
         CardsSlideSeconds, StripFlySeconds, Effects, Forecast];

    /// <summary>The development switches that set a setting for the launch.</summary>
    private static readonly (string Switch, Setting Setting)[] Switches =
        [("--strip-seconds", StripSeconds), ("--army-seconds", ArmySeconds), ("--meter-seconds", MeterSeconds), ("--count-seconds", CountSeconds), ("--opening-seconds", OpeningSeconds), ("--opening-appear-seconds", OpeningAppearSeconds),
         ("--result-seconds", ResultSeconds), ("--cards-slide-seconds", CardsSlideSeconds), ("--strip-fly-seconds", StripFlySeconds),
         ("--damage-forecast", Forecast)];

    private static Dictionary<string, double>? _values;

    /// <summary>
    /// A setting's value: <c>--strip-seconds x</c> / <c>--army-seconds x</c> / <c>--meter-seconds x</c> / <c>--count-seconds x</c> / <c>--opening-seconds x</c> / <c>--opening-appear-seconds x</c>
    /// / <c>--result-seconds x</c> / <c>--damage-forecast 0|1</c> for the launch (development) for <see cref="StripSeconds"/> / <see cref="ArmySeconds"/> /
    /// <see cref="MeterSeconds"/> / <see cref="CountSeconds"/> / <see cref="OpeningSeconds"/> / <see cref="OpeningAppearSeconds"/> / <see cref="ResultSeconds"/> / <see cref="Forecast"/>,
    /// else the file's, else the default.
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
