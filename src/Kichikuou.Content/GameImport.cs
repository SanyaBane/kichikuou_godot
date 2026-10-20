using Kichikuou.Content.Archives;
using Kichikuou.Content.Audio;
using Kichikuou.Content.Decompiler;
using Kichikuou.Content.Graphics;

namespace Kichikuou.Content;

/// <summary>What the import is doing: a stage's name and how far the whole import is (0-1).</summary>
public readonly record struct ImportProgress(string Stage, double Fraction);

/// <summary>
/// A game folder imported (docs/import.md, step 8): the version check passed and the scenario decompiled into the
/// cache; the CGs, the sounds and the music are read from the folder itself at run time.
/// </summary>
public sealed class ImportedGame(GameFolder folder, string scenarioDirectory)
{
    /// <summary>The player's game folder, checked.</summary>
    public GameFolder Folder { get; } = folder;

    /// <summary>The decompiled scenario, the <c>.ADV</c> files <see cref="ScenarioText"/> and the scripts read.</summary>
    public string ScenarioDirectory { get; } = scenarioDirectory;

    /// <summary>The CGs, from <c>KICHIKUGA.ALD</c> and <c>KICHIKUGB.ALD</c>.</summary>
    public CgLibrary OpenCgs() => new(AldArchive.Open(Folder.Directory, "KICHIKUG"));

    /// <summary>The sound effects, from <c>KICHIKUWA.ALD</c>.</summary>
    public SoundLibrary OpenSounds() => new(AldArchive.Open(Folder.Directory, "KICHIKUW"));
}

/// <summary>
/// The import of the player's game folder (docs/import.md, step 8), engine-independent: the version check, then the
/// scenario decompiled into a cache folder of its own, <c>&lt;cache&gt;/&lt;key&gt;-&lt;importer version&gt;/scenario</c>
/// (<see cref="GameFolder.Key"/>), which is reused while the files and the importer stay the same. A marker file
/// written last says the folder is whole, so an import cut short is done again.
/// </summary>
public static class GameImport
{
    /// <summary>The importer's version: raised when its output changes, so older caches are not reused.</summary>
    public const int ImporterVersion = 1;

    private const string MarkerName = "import.done";

    /// <summary>The cache folder of a game folder.</summary>
    public static string CacheDirectory(GameFolder folder, string cacheRoot) =>
        Path.Combine(cacheRoot, $"{folder.Key}-{ImporterVersion}");

    /// <summary>The import of a checked folder when its cache is whole; null when it must be imported.</summary>
    public static ImportedGame? Find(GameFolder folder, string cacheRoot)
    {
        string cache = CacheDirectory(folder, cacheRoot);
        return File.Exists(Path.Combine(cache, MarkerName)) && Directory.Exists(Path.Combine(cache, "scenario"))
            ? new ImportedGame(folder, Path.Combine(cache, "scenario"))
            : null;
    }

    /// <summary>
    /// Imports a game folder: checks it (<see cref="GameFiles.Check(string)"/>, a <see cref="GameFolderException"/>
    /// when it is no known version), and decompiles the scenario into the cache unless a whole one is there.
    /// A scenario that does not decompile throws <see cref="DecompileException"/>.
    /// </summary>
    public static ImportedGame Import(string gameDirectory, string cacheRoot, IProgress<ImportProgress>? progress = null)
    {
        progress?.Report(new ImportProgress("Checking the game files", 0));
        var folder = GameFiles.Check(gameDirectory);
        if (Find(folder, cacheRoot) is { } found)
        {
            progress?.Report(new ImportProgress("Done", 1));
            return found;
        }

        progress?.Report(new ImportProgress("Reading the scenario", 0.05));
        var scenario = AldArchive.Open(folder.Directory, "KICHIKUS");
        var ain = AinFile.Read(folder.Paths["System39.ain"]);
        var decompiling = progress is null ? null : new Relay(p => progress.Report(Decompiling(p)));
        var files = ScenarioDecompiler.Decompile(scenario, ain, decompiling);

        progress?.Report(new ImportProgress("Writing the scenario", 0.95));
        string cache = CacheDirectory(folder, cacheRoot);
        string target = Path.Combine(cache, "scenario");
        string temporary = Path.Combine(cache, $"scenario.{Guid.NewGuid():N}");
        ScenarioDecompiler.WriteTo(files, temporary);
        if (Directory.Exists(target))
            Directory.Delete(target, recursive: true); // a cut-short import of our own: no marker
        Directory.Move(temporary, target);
        File.WriteAllText(Path.Combine(cache, MarkerName),
            $"{folder.Version.Name}\nimporter {ImporterVersion}\n{files.Count} files\n");
        progress?.Report(new ImportProgress("Done", 1));
        return new ImportedGame(folder, target);
    }

    /// <summary>
    /// The decompile's share of the bar, 0.05-0.95: the first analysis pass a third of it, the later ones the
    /// next third (their pages are fewer and fewer), the files written the last.
    /// </summary>
    private static ImportProgress Decompiling(DecompileProgress p)
    {
        double page = (double)p.Page / Math.Max(1, p.Pages);
        double part = p.Pass switch
        {
            0 => 2 / 3.0 + page / 3,
            1 => page / 3,
            _ => 1 / 3.0 + page / 3,
        };
        return new ImportProgress(p.Pass == 0 ? "Writing the scenario" : "Decompiling the scenario", Math.Min(0.95, 0.05 + 0.9 * part));
    }

    /// <summary>A progress report on the reporting thread.</summary>
    private sealed class Relay(Action<DecompileProgress> report) : IProgress<DecompileProgress>
    {
        public void Report(DecompileProgress value) => report(value);
    }
}
