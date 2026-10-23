using Godot;
using Kichikuou.Original.Content;

namespace Kichikuou.Game.Strategy;

/// <summary>
/// Option → Display → "Thousands" (owner, 2026-10-23, docs/ui/options.md): what our screens write between a number's
/// thousands — gold, troops, damage (<see cref="Figures"/>). It keeps the player's choice and hands it to
/// <see cref="Figures.Separator"/>, which the library's text reads.
/// </summary>
internal static class ThousandsChoice
{
    /// <summary>The choices in the order Option lists them.</summary>
    public static readonly ThousandsSeparator[] All =
    [
        ThousandsSeparator.Comma, ThousandsSeparator.Period, ThousandsSeparator.Space, ThousandsSeparator.Apostrophe,
        ThousandsSeparator.None,
    ];

    /// <summary>The sample Option shows for <paramref name="separator"/>: "129,300K GOLD".</summary>
    public static string Sample(ThousandsSeparator separator) => Figures.Number(129300, separator) + "K GOLD";

    private static ThousandsSeparator? _chosen;

    /// <summary>
    /// The chosen separator: <c>[display] thousands</c> in <c>user://settings.cfg</c>, else the comma. Setting it saves
    /// it for later launches and makes it <see cref="Figures.Separator"/>.
    /// </summary>
    public static ThousandsSeparator Chosen
    {
        get
        {
            if (_chosen is { } chosen)
                return chosen;
            var config = new ConfigFile();
            int saved = config.Load(GameData.SettingsPath) == Error.Ok
                ? (int)config.GetValue("display", "thousands", (int)ThousandsSeparator.Comma) : (int)ThousandsSeparator.Comma;
            return (_chosen = Enum.IsDefined((ThousandsSeparator)saved) ? (ThousandsSeparator)saved : ThousandsSeparator.Comma).Value;
        }
        set
        {
            _chosen = Figures.Separator = value;
            var config = new ConfigFile();
            config.Load(GameData.SettingsPath);
            config.SetValue("display", "thousands", (int)value);
            config.Save(GameData.SettingsPath);
        }
    }

    /// <summary>The saved choice made <see cref="Figures.Separator"/>, once at the launch.</summary>
    public static void Apply() => Figures.Separator = Chosen;
}
