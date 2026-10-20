using Kichikuou.Content.Archives;

namespace Kichikuou.Content.Audio;

/// <summary>
/// The sound effects by script number (<c>SP n</c>, <c>SQ l, r</c>) as WAV bytes, from one of two
/// sources: the game's sound archive (<c>KICHIKUWA.ALD</c>, the sound n at link-table index n − 1, as
/// xsystem35 looks it up, <c>pcm.sdlmixer.c:67-82</c>; docs/import.md, step 3), or the developer's
/// extract, where <c>wav\</c> keeps the archive's entry names. Both give the same bytes.
/// </summary>
public sealed class SoundLibrary
{
    private readonly AldArchive? _archive;
    private readonly string? _directory;
    private readonly Func<int, string?>? _fileName;

    /// <summary>The sounds of the game's sound archive (<c>AldArchive.Open(gameFolder, "KICHIKUW")</c>).</summary>
    public SoundLibrary(AldArchive archive) => _archive = archive;

    /// <summary>The sounds of the extract's <c>wav\</c> folder, each under the name <paramref name="fileName"/> gives its number; a missing folder has none.</summary>
    public SoundLibrary(string directory, Func<int, string?> fileName)
    {
        _directory = directory;
        _fileName = fileName;
    }

    /// <summary>The WAV file of sound <paramref name="number"/>, or <c>null</c> when it is not linked.</summary>
    public byte[]? Get(int number)
    {
        if (number <= 0)
            return null;
        if (_archive is not null)
            return _archive[number - 1]?.ReadAllBytes();
        return _fileName!(number) is { } name && Path.Combine(_directory!, name) is var path && File.Exists(path)
            ? File.ReadAllBytes(path)
            : null;
    }
}
