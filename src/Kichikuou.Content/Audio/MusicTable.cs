namespace Kichikuou.Content.Audio;

/// <summary>
/// Music tracks: <c>SS n</c> plays CD track n + 1 (<c>cmds.c:37-50</c>), which xsystem35 maps
/// through the <c>_inmm.ini</c> playlist next to the game: line k (0-based) is track k + 2
/// (<c>cdrom.mp3.c:51-86</c>), so <c>SS n</c> = line n − 1 (docs/assets.md §5).
/// </summary>
public sealed class MusicTable
{
    private readonly IReadOnlyList<string> _lines;

    /// <summary>Reads <c>_inmm.ini</c> from the game folder <paramref name="gameDirectory"/>.</summary>
    public MusicTable(string gameDirectory)
    {
        GameDirectory = gameDirectory;
        string ini = Path.Combine(gameDirectory, "_inmm.ini");
        _lines = File.Exists(ini)
            ? File.ReadAllLines(ini).Select(l => l.Trim()).ToList()
            : [];
    }

    /// <summary>The game folder.</summary>
    public string GameDirectory { get; }

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
        return Path.Combine(GameDirectory, _lines[n - 1].Replace('\\', Path.DirectorySeparatorChar));
    }
}
