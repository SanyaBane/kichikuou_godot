using System.Security.Cryptography;
using System.Text;

namespace Kichikuou.Content;

/// <summary>A game folder the port cannot read: the message says which file and which version it expects.</summary>
public sealed class GameFolderException(string message) : Exception(message);

/// <summary>A version of the game the port reads: its name and the SHA-256 of each file it reads (lower-case hex).</summary>
public sealed record GameVersion(string Name, IReadOnlyList<(string File, string Sha256)> Files);

/// <summary>A game folder whose files are a known version (<see cref="GameFiles.Check(string)"/>).</summary>
/// <param name="Directory">The folder.</param>
/// <param name="Version">The version its files are.</param>
/// <param name="Paths">Each file the version names, by that name, at its path in the folder (the case may differ).</param>
public sealed record GameFolder(string Directory, GameVersion Version, IReadOnlyDictionary<string, string> Paths)
{
    /// <summary>
    /// A key of the files' contents: the first 16 hex digits of the SHA-256 of the version's file lines
    /// (<c>name sha256</c>), the same for every copy of the version (docs/import.md, step 8 keys its cache by it).
    /// </summary>
    public string Key
    {
        get
        {
            var lines = string.Concat(Version.Files.Select(f => $"{f.File} {f.Sha256}\n"));
            return Convert.ToHexStringLower(SHA256.HashData(Encoding.ASCII.GetBytes(lines)))[..16];
        }
    }
}

/// <summary>
/// The version check of the player's game folder (docs/import.md, step 7): before anything reads the folder,
/// the SHA-256 of each file the import reads is compared with the table of the versions the port knows — one,
/// the English translation v1.0. A missing file or another file refuses the folder with a message that names
/// the file and the version, instead of a failure somewhere inside a reader. (The table holds hashes of the
/// player's files, not their contents, so it is committed.)
/// </summary>
public static class GameFiles
{
    /// <summary>Kichikuou Rance English Translation v1.0 (Arunaru, Pi-R, Vodka, 2014): the version the port is made from.</summary>
    public static readonly GameVersion EnglishTranslation10 = new("Kichikuou Rance English Translation v1.0",
    [
        ("KICHIKUSA.ALD", "699d0d5bf2f80629a356ea73ce27a622573a11296cbf018349cfb26928c031ba"),
        ("System39.ain", "9e7631ece3e580802799bd6e05747201b9b01a0f4a634b71a176ec08eead5f70"),
        ("KICHIKUGA.ALD", "45a4e7269be599a5ee1e3f215f3a106f1fc06bd1d6b1c0250fd706f93ac90180"),
        ("KICHIKUGB.ALD", "0a1a11e5d9fef6c5887c4d26e290f8a76c9a00fc168b2695a572ca5da8c137a0"),
        ("KICHIKUWA.ALD", "159612c81aa858fcd54a20215e6771bc55901b7c298a4302b4fd65398b810ce1"),
        ("_inmm.ini", "5fb1e9f84b44bf2c0a7cc4f743b0161d7abe2bb6f564bb5a7376adc1a8644484"),
    ]);

    /// <summary>The versions the port reads.</summary>
    public static IReadOnlyList<GameVersion> Known { get; } = [EnglishTranslation10];

    /// <summary>
    /// Checks a folder against the known versions: the version whose files are all there with their hashes,
    /// or a <see cref="GameFolderException"/> naming what is missing or different.
    /// </summary>
    public static GameFolder Check(string directory) => Check(directory, Known);

    /// <summary><see cref="Check(string)"/> against a table of versions.</summary>
    public static GameFolder Check(string directory, IReadOnlyList<GameVersion> versions)
    {
        if (!System.IO.Directory.Exists(directory))
            throw new GameFolderException($"The folder {directory} does not exist.");
        var present = System.IO.Directory.EnumerateFiles(directory)
            .GroupBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key!, g => g.First(), StringComparer.OrdinalIgnoreCase);
        var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        string? firstProblem = null;
        foreach (var version in versions)
        {
            string? problem = null;
            var paths = new Dictionary<string, string>();
            foreach (var (file, sha256) in version.Files)
            {
                if (!present.TryGetValue(file, out var path))
                {
                    problem = MissingFile(directory, version, file);
                    break;
                }
                if (!hashes.TryGetValue(file, out var hash))
                    hashes[file] = hash = Sha256(path);
                if (hash != sha256)
                {
                    problem = OtherFile(directory, version, file, hash, sha256);
                    break;
                }
                paths[file] = path;
            }
            if (problem is null)
                return new GameFolder(directory, version, paths);
            firstProblem ??= problem;
        }
        throw new GameFolderException(firstProblem ?? "No game version is known.");
    }

    /// <summary>
    /// The refusal of a folder without <paramref name="file"/> of <paramref name="version"/> (the wrong folder chosen). Also
    /// the screen gallery's, which shows the import screen's refusals for a folder of its own (docs/ui/gallery.md).
    /// </summary>
    public static string MissingFile(string directory, GameVersion version, string file) =>
        $"The folder {directory} has no {file}. Choose the folder of {version.Name}, the one with system39.exe.";

    /// <summary>
    /// The refusal of a folder whose <paramref name="file"/> is not <paramref name="version"/>'s: its SHA-256
    /// <paramref name="hash"/>, <paramref name="expected"/> expected (another version, or a damaged copy).
    /// </summary>
    public static string OtherFile(string directory, GameVersion version, string file, string hash, string expected) =>
        $"{file} in {directory} is not the file of {version.Name}, the version this game reads "
        + $"(its SHA-256 is {hash}, {expected} is expected).";

    /// <summary>A file's SHA-256, lower-case hex.</summary>
    public static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(SHA256.HashData(stream));
    }
}
