namespace Kichikuou.Content.Audio;

/// <summary>
/// Music tracks: <c>SS n</c> plays CD track n + 1 (<c>cmds.c:37-50</c>), which xsystem35 maps
/// through the <c>_inmm.ini</c> playlist next to the game: line k (0-based) is track k + 2
/// (<c>cdrom.mp3.c:51-86</c>), so <c>SS n</c> = line n − 1 (docs/assets.md §5). The playlist is no
/// part of the version check (docs/import.md, step 7): a copy's own plays as it lists (a mod's longer
/// one too), and a copy without one plays the English translation's, <see cref="TranslationPlaylist"/>.
/// </summary>
public sealed class MusicTable
{
    /// <summary>The playlist's file name in the game folder (matched without case).</summary>
    public const string PlaylistName = "_inmm.ini";

    /// <summary>
    /// The lines of the English translation v1.0's <c>_inmm.ini</c>: <c>bgm\kichiku_2.mp3</c> … <c>bgm\kichiku_29.mp3</c>,
    /// tracks 2-29, the list a copy without the file plays.
    /// </summary>
    public static IReadOnlyList<string> TranslationPlaylist { get; } =
        [.. Enumerable.Range(2, 28).Select(track => $@"bgm\kichiku_{track}.mp3")];

    private readonly IReadOnlyList<string> _lines;

    /// <summary>Reads <c>_inmm.ini</c> from the game folder <paramref name="gameDirectory"/>, else takes <see cref="TranslationPlaylist"/>.</summary>
    public MusicTable(string gameDirectory)
    {
        GameDirectory = gameDirectory;
        string? playlist = FindPlaylist(gameDirectory);
        FromPlaylist = playlist is not null;
        _lines = playlist is not null
            ? File.ReadAllLines(playlist).Select(l => l.Trim()).ToList()
            : TranslationPlaylist;
    }

    /// <summary>The game folder.</summary>
    public string GameDirectory { get; }

    /// <summary>Whether the folder's own <c>_inmm.ini</c> was read (false: <see cref="TranslationPlaylist"/>).</summary>
    public bool FromPlaylist { get; }

    /// <summary>The number of tracks listed.</summary>
    public int Count => _lines.Count;

    /// <summary>
    /// The music file of <c>SS <paramref name="n"/></c> (full path), or <c>null</c> for 0 (stop) and
    /// tracks the playlist does not list.
    /// </summary>
    public string? PathOf(int n)
    {
        if (n < 1 || n > _lines.Count || _lines[n - 1].Length == 0)
            return null;
        return Path.Combine(GameDirectory, Relative(_lines[n - 1]));
    }

    /// <summary>The tracks the playlist lists, in its order, each as a path under the game folder (this system's separators).</summary>
    public IReadOnlyList<string> Listed => [.. _lines.Where(l => l.Length > 0).Select(Relative)];

    /// <summary>The listed tracks whose file is not in the game folder, in the playlist's order (as <see cref="Listed"/>).</summary>
    public IReadOnlyList<string> Missing() =>
        [.. Listed.Where(track => !File.Exists(Path.Combine(GameDirectory, track)))];

    private static string Relative(string line) => line.Replace('\\', Path.DirectorySeparatorChar);

    private static string? FindPlaylist(string directory) =>
        Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory)
                .FirstOrDefault(f => string.Equals(Path.GetFileName(f), PlaylistName, StringComparison.OrdinalIgnoreCase))
            : null;
}
