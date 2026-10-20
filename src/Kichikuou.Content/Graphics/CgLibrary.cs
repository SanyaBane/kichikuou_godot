using System.Buffers.Binary;
using System.Collections.Concurrent;
using Kichikuou.Content.Archives;

namespace Kichikuou.Content.Graphics;

/// <summary>
/// The player's CGs by script number (<c>G n</c>), from one of two sources: the game's own CG archive
/// (<c>KICHIKUGA.ALD</c> + <c>KICHIKUGB.ALD</c>, the CG n at link-table index n − 1, each a PMS decoded by
/// <see cref="Pms"/>; docs/import.md, step 2), or the developer's extract, where <c>CGnnnn.png</c> in
/// <c>cg_png\</c> is CG n (docs/assets.md §1). Both give the same images. Decoded images are cached.
/// </summary>
public sealed class CgLibrary
{
    private readonly ConcurrentDictionary<int, CgImage?> _cache = new();
    private readonly string? _directory;
    private readonly AldArchive? _archive;

    /// <summary>The CGs of the extract's <c>cg_png\</c> folder; a missing folder has none.</summary>
    public CgLibrary(string directory) => _directory = directory;

    /// <summary>The CGs of the game's CG archive set (<c>AldArchive.Open(gameFolder, "KICHIKUG")</c>).</summary>
    public CgLibrary(AldArchive archive) => _archive = archive;

    /// <summary>
    /// CG <paramref name="number"/>, or <c>null</c> when there is none: 0 and the unused numbers
    /// make <c>G</c> a silent no-op in the original (<c>cmdg.c:32-36</c>, <c>cg.c:392-394</c>).
    /// </summary>
    public CgImage? Get(int number) => number <= 0 ? null : _cache.GetOrAdd(number, Load);

    /// <summary>Every CG number there is, ascending.</summary>
    public IEnumerable<int> Numbers =>
        _archive is not null ? _archive.Entries.Select(e => e.Index + 1)
        : System.IO.Directory.Exists(_directory)
            ? System.IO.Directory.EnumerateFiles(_directory, "CG*.png")
                .Select(path => int.TryParse(Path.GetFileNameWithoutExtension(path)[2..], out int n) ? n : 0)
                .Where(n => n > 0 && File.Exists(PathOf(n))).Order()
            : [];

    /// <summary>The size of CG <paramref name="number"/> from its header alone, without decoding it; null when there is none.</summary>
    public (int Width, int Height)? SizeOf(int number)
    {
        if (number <= 0)
            return null;
        if (_cache.TryGetValue(number, out var cached))
            return cached is null ? null : (cached.Width, cached.Height);
        if (_archive is not null)
        {
            if (_archive[number - 1] is not { } entry)
                return null;
            byte[] pms = entry.ReadAllBytes();
            if (pms.Length < 32)
                throw new CgFormatException($"CG {number}: not a PMS file.");
            return (BinaryPrimitives.ReadInt32LittleEndian(pms.AsSpan(24)), BinaryPrimitives.ReadInt32LittleEndian(pms.AsSpan(28)));
        }
        if (!File.Exists(PathOf(number)))
            return null;
        var header = new byte[24];
        using (var file = File.OpenRead(PathOf(number)))
            file.ReadExactly(header);
        return (BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16)), BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20)));
    }

    private CgImage? Load(int number)
    {
        if (_archive is null)
            return File.Exists(PathOf(number)) ? IndexedPng.Load(PathOf(number)) : null;
        if (_archive[number - 1] is not { } entry)
            return null;
        try
        {
            return Pms.Decode(entry.ReadAllBytes());
        }
        catch (CgFormatException e)
        {
            throw new CgFormatException($"CG {number} (volume {(char)('A' + entry.Volume - 1)}): {e.Message}");
        }
    }

    private string PathOf(int number) => Path.Combine(_directory!, $"CG{number:D4}.png");
}
