using Godot;

namespace Kichikuou.Game;

/// <summary>
/// Where the player's own game data lives. For now the folder of extracted data
/// (<c>_extract</c>: decompiled <c>scenario\</c>, <c>cg_png\</c>, <c>wav\</c>, golden captures in
/// <c>golden\</c>); kept in <c>user://settings.cfg</c>. Nothing from it is part of the project.
/// </summary>
public static class GameData
{
    private const string SettingsPath = "user://settings.cfg";
    private const string DefaultExtract = @"E:\Modding games\Modding Rance\Kichikou\_extract";

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

    /// <summary>The decompiled scenario (<c>.ADV</c> files).</summary>
    public static string ScenarioDirectory => System.IO.Path.Combine(ExtractDirectory, "scenario");

    /// <summary>The CGs as PNG files (<c>CGnnnn.png</c>).</summary>
    public static string CgDirectory => System.IO.Path.Combine(ExtractDirectory, "cg_png");

    /// <summary>The golden captures (<c>*.golden</c>).</summary>
    public static string GoldenDirectory => System.IO.Path.Combine(ExtractDirectory, "golden");
}
