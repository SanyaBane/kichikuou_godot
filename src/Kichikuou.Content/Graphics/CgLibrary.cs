using System.Collections.Concurrent;

namespace Kichikuou.Content.Graphics;

/// <summary>
/// The player's CGs by script number (<c>G n</c>): <c>CGnnnn.png</c> in the extracted
/// <c>cg_png\</c> folder is CG n (docs/assets.md §1). Decoded images are cached.
/// </summary>
public sealed class CgLibrary(string directory)
{
    private readonly ConcurrentDictionary<int, CgImage?> _cache = new();

    /// <summary>The folder of <c>CGnnnn.png</c> files.</summary>
    public string Directory { get; } = directory;

    /// <summary>The file of CG <paramref name="number"/>.</summary>
    public string PathOf(int number) => Path.Combine(Directory, $"CG{number:D4}.png");

    /// <summary>
    /// CG <paramref name="number"/>, or <c>null</c> when there is none: 0 and the unused numbers
    /// make <c>G</c> a silent no-op in the original (<c>cmdg.c:32-36</c>, <c>cg.c:392-394</c>).
    /// </summary>
    public CgImage? Get(int number) =>
        number <= 0 ? null : _cache.GetOrAdd(number, n => File.Exists(PathOf(n)) ? IndexedPng.Load(PathOf(n)) : null);
}
