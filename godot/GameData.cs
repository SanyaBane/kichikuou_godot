using Godot;
using Kichikuou.Content;
using Kichikuou.Content.Audio;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine;
using Kichikuou.Original.Content.Audio;

namespace Kichikuou.Game;

/// <summary>
/// Where the player's own game data lives (docs/import.md, step 8). Two sources:
/// <list type="bullet">
/// <item>the developer's extract (<c>_extract</c>: decompiled <c>scenario\</c>, <c>cg_png\</c>, <c>wav\</c>, golden
/// captures in <c>golden\</c>), when one is set — <c>user://settings.cfg</c>, by default the developer's local paths
/// (<see cref="LocalPaths"/>: <c>local.env</c> or the environment) — and holds a scenario: it wins;</item>
/// <item>else the import of the player's game folder (<see cref="Imported"/>): the scenario decompiled into
/// <c>user://import/&lt;key&gt;-&lt;importer version&gt;/</c>, the CGs and sounds read from the folder's archives.</item>
/// </list>
/// Nothing from either is part of the project. The music is the game folder's in both.
/// </summary>
public static class GameData
{
    /// <summary>The player's settings: these folders, and the font (<see cref="Battles.GodotGlyphRenderer.Chosen"/>).</summary>
    internal const string SettingsPath = "user://settings.cfg";

    /// <summary>The import's cache folder (<see cref="GameImport"/>).</summary>
    public const string ImportRoot = "user://import";

    private static string DefaultExtract => LocalPaths.ExtractDirectory ?? "";

    /// <summary>The folder of extracted data (empty: none).</summary>
    public static string ExtractDirectory
    {
        get => Setting("extract_dir", DefaultExtract);
        set => SetSetting("extract_dir", value);
    }

    /// <summary>The player's game folder: the music (<c>_inmm.ini</c>, <c>bgm\</c>), and what the import reads.</summary>
    public static string GameDirectory
    {
        get => Imported?.Folder.Directory ?? Setting("game_dir", DefaultGame);
        set => SetSetting("game_dir", value);
    }

    private static string DefaultGame => LocalPaths.GameDirectory ?? AndroidGameDirectory ?? "";

    /// <summary>The Android build's package, as <c>export_presets.cfg</c> names it.</summary>
    public const string AndroidPackage = "org.kichikuou.port";

    /// <summary>
    /// On Android, the game folder the player copied into the app's own external folder, when it is there: the
    /// app reads it without a permission, and the native folder dialog would give a <c>content://</c> URI that
    /// <c>System.IO</c> cannot open (docs/backlog.md, "Android").
    /// </summary>
    public static string? AndroidGameDirectory =>
        OS.GetName() == "Android" && System.IO.Directory.Exists(AndroidGameFolder) ? AndroidGameFolder : null;

    /// <summary>Where the Android build looks for the game folder (<see cref="AndroidGameDirectory"/>).</summary>
    public const string AndroidGameFolder = "/storage/emulated/0/Android/data/" + AndroidPackage + "/files/game";

    /// <summary>The game folder the settings keep (the import screen's last), whatever is open now.</summary>
    public static string SavedGameDirectory => Setting("game_dir", DefaultGame);

    /// <summary>The import of the game folder, once it is open (<see cref="OpenImport"/>, or the import screen).</summary>
    public static ImportedGame? Imported { get; set; }

    /// <summary>The launch reads the import even where an extract is set (<c>--import</c>, to try the release's way).</summary>
    public static bool ForceImport { get; set; }

    /// <summary>Whether the developer's extract is the data: set, holding a scenario, and no import forced.</summary>
    public static bool UsesExtract =>
        !ForceImport && ExtractDirectory is { Length: > 0 } extract && System.IO.Directory.Exists(System.IO.Path.Combine(extract, "scenario"));

    /// <summary>The import's cache folder as a path of the file system.</summary>
    public static string ImportCache => ProjectSettings.GlobalizePath(ImportRoot);

    /// <summary>
    /// Opens the import of the saved game folder when its cache is whole (the version checked again): true, or
    /// false with the reason when the import screen must run first (no folder chosen yet, another version, no cache).
    /// </summary>
    public static bool OpenImport(out string? problem)
    {
        problem = null;
        string folder = SavedGameDirectory;
        if (folder.Length == 0)
        {
            problem = "No game folder is chosen yet.";
            return false;
        }
        try
        {
            Imported = GameImport.Find(GameFiles.Check(folder), ImportCache);
            if (Imported is null)
                problem = $"The game folder {folder} is not imported yet.";
            return Imported is not null;
        }
        catch (GameFolderException e)
        {
            problem = e.Message;
            return false;
        }
    }

    /// <summary>The sound effects (<c>.WAV</c> files named as in the archive), in the extract.</summary>
    public static string WavDirectory => System.IO.Path.Combine(ExtractDirectory, "wav");

    /// <summary>The decompiled scenario (<c>.ADV</c> files): the extract's, else the import's.</summary>
    public static string ScenarioDirectory =>
        UsesExtract ? System.IO.Path.Combine(ExtractDirectory, "scenario") : Imported?.ScenarioDirectory ?? "";

    /// <summary>The CGs as PNG files (<c>CGnnnn.png</c>), in the extract.</summary>
    public static string CgDirectory => System.IO.Path.Combine(ExtractDirectory, "cg_png");

    /// <summary>The golden captures (<c>*.golden</c>), in the extract.</summary>
    public static string GoldenDirectory => System.IO.Path.Combine(ExtractDirectory, "golden");

    /// <summary>The CGs: the extract's PNGs, else the game folder's archives.</summary>
    public static CgLibrary Cgs() =>
        !UsesExtract && Imported is { } imported ? imported.OpenCgs() : new CgLibrary(CgDirectory);

    /// <summary>The sound effects: the extract's WAVs, else the game folder's archive.</summary>
    public static SoundLibrary Sounds() =>
        !UsesExtract && Imported is { } imported ? imported.OpenSounds() : new SoundLibrary(WavDirectory, SoundTable.FileName);

    private static string Setting(string key, string fallback)
    {
        var config = new ConfigFile();
        return config.Load(SettingsPath) == Error.Ok ? (string)config.GetValue("data", key, fallback) : fallback;
    }

    private static void SetSetting(string key, string value)
    {
        var config = new ConfigFile();
        config.Load(SettingsPath);
        config.SetValue("data", key, value);
        config.Save(SettingsPath);
    }
}
