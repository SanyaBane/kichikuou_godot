using Godot;
using Kichikuou.Core;

namespace Kichikuou.Game;

/// <summary>
/// Where the player's own game data lives. For now the folder of extracted data
/// (<c>_extract</c>: decompiled <c>scenario\</c>, <c>cg_png\</c>, <c>wav\</c>, golden captures in
/// <c>golden\</c>); kept in <c>user://settings.cfg</c>, by default the developer's local paths
/// (<see cref="LocalPaths"/>: <c>local.env</c> or the environment). Nothing from it is part of the project.
/// </summary>
public static class GameData
{
    private const string SettingsPath = "user://settings.cfg";
    private static string DefaultExtract => LocalPaths.ExtractDirectory ?? "";

    /// <summary>The folder of extracted data.</summary>
    public static string ExtractDirectory
    {
        get
        {
            var config = new ConfigFile();
            return config.Load(SettingsPath) == Error.Ok
                ? (string)config.GetValue("data", "extract_dir", DefaultExtract)
                : DefaultExtract;
        }
        set
        {
            var config = new ConfigFile();
            config.Load(SettingsPath);
            config.SetValue("data", "extract_dir", value);
            config.Save(SettingsPath);
        }
    }

    /// <summary>The player's game folder (music: <c>_inmm.ini</c>, <c>bgm\</c>).</summary>
    public static string GameDirectory
    {
        get
        {
            var config = new ConfigFile();
            return config.Load(SettingsPath) == Error.Ok
                ? (string)config.GetValue("data", "game_dir", DefaultGame)
                : DefaultGame;
        }
        set
        {
            var config = new ConfigFile();
            config.Load(SettingsPath);
            config.SetValue("data", "game_dir", value);
            config.Save(SettingsPath);
        }
    }

    private static string DefaultGame => LocalPaths.GameDirectory ?? "";

    /// <summary>The sound effects (<c>.WAV</c> files named as in the archive).</summary>
    public static string WavDirectory => System.IO.Path.Combine(ExtractDirectory, "wav");

    /// <summary>The decompiled scenario (<c>.ADV</c> files).</summary>
    public static string ScenarioDirectory => System.IO.Path.Combine(ExtractDirectory, "scenario");

    /// <summary>The CGs as PNG files (<c>CGnnnn.png</c>).</summary>
    public static string CgDirectory => System.IO.Path.Combine(ExtractDirectory, "cg_png");

    /// <summary>The golden captures (<c>*.golden</c>).</summary>
    public static string GoldenDirectory => System.IO.Path.Combine(ExtractDirectory, "golden");
}
