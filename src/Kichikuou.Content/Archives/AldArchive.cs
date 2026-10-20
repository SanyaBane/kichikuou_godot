// Parts translated from xsystem35 (https://github.com/kichikuou/xsystem35-sdl2), dri.c (read_index,
// dri_init: the tables, the volumes, the link table of the first volume): Copyright (C) 1997-1998
// Masaki Chikama (Wren); and from xsys35c 1.13.0 (https://github.com/kichikuou/xsys35c), common/ald.c
// (ald_read, ald_read_entries: the footer, the entry header): Copyright (C) 2020
// <KichikuouChrome@gmail.com>. GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

using System.Buffers.Binary;
using Microsoft.Win32.SafeHandles;

namespace Kichikuou.Content.Archives;

/// <summary>A file that is not a readable ALD archive, or an archive set that does not fit together.</summary>
public sealed class AldFormatException(string message) : Exception(message);

/// <summary>
/// An ALD archive set of the player's game: <c>KICHIKUSA.ALD</c> (the scenario), <c>KICHIKUGA.ALD</c> +
/// <c>KICHIKUGB.ALD</c> (the CGs), <c>KICHIKUWA.ALD</c> (the sounds). A set is one to 26 volumes, the files
/// <c>&lt;prefix&gt;A.ALD</c> to <c>&lt;prefix&gt;Z.ALD</c>; the volume is the file's letter (A = 1), as
/// xsystem35 opens them (<c>dri_init</c>).
/// <para>
/// A volume is built from 256-byte sectors: a pointer table of 3-byte sector numbers (its first entry is
/// where the link table starts, its second where the link table ends), the link table (3 bytes per
/// resource: the volume, then a 16-bit index into that volume's pointer table), then the entries (a header
/// — its own size, the data's size, a Windows file time, the name in Shift JIS — and the data), and a
/// 16-byte footer. In System 3.9 only the first volume's link table counts (<c>dri.c:47-49</c>); the
/// resource number is the link table's index: the scenario page n is index n, the CG n index n − 1
/// (docs/import.md).
/// </para>
/// Entries are read from their files on demand; opening reads only the tables and the entries' headers.
/// </summary>
public sealed class AldArchive
{
    private const uint Signature = 0x14c4e;
    private const uint Signature2 = 0x12020;
    private const int FooterSize = 16;
    private const int EntryHeaderSize = 16;

    private readonly AldEntry?[] _entries;

    private AldArchive(AldEntry?[] entries) => _entries = entries;

    /// <summary>
    /// The length of the link table, padded to whole sectors as xsystem35 counts it (<c>nr_files</c>): the
    /// indices past the last resource are gaps (256 for the scenario's 201 pages).
    /// </summary>
    public int Count => _entries.Length;

    /// <summary>The resource at a link-table index; null for a gap (volume 0) or an index past the table.</summary>
    public AldEntry? this[int index] => index >= 0 && index < _entries.Length ? _entries[index] : null;

    /// <summary>The resources in link-table order, the gaps left out.</summary>
    public IEnumerable<AldEntry> Entries => _entries.OfType<AldEntry>();

    /// <summary>The resource of an ASCII name (<c>R5FEF01.WAV</c>), compared without case as the DOS names are; null when none.</summary>
    public AldEntry? Find(string asciiName)
    {
        foreach (var entry in Entries)
        {
            var name = entry.RawName.Span;
            if (name.Length == asciiName.Length && AsciiEquals(name, asciiName))
                return entry;
        }
        return null;
    }

    /// <summary>
    /// Opens the set <c>&lt;prefix&gt;A.ALD</c>, <c>&lt;prefix&gt;B.ALD</c>, … in a folder (the names
    /// matched without case, for case-sensitive file systems). Volume A must exist; a volume the link
    /// table names must exist too.
    /// </summary>
    public static AldArchive Open(string directory, string prefix)
    {
        var volumes = new string?[26];
        foreach (string path in Directory.EnumerateFiles(directory))
        {
            string name = Path.GetFileName(path);
            if (name.Length == prefix.Length + 5 && name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && name.EndsWith(".ALD", StringComparison.OrdinalIgnoreCase))
            {
                int letter = char.ToUpperInvariant(name[prefix.Length]) - 'A';
                if (letter is >= 0 and < 26)
                    volumes[letter] = path;
            }
        }
        if (volumes[0] is null)
            throw new AldFormatException($"{Path.Combine(directory, prefix + "A.ALD")}: not found.");
        int last = Array.FindLastIndex(volumes, v => v is not null);
        return Open(volumes[..(last + 1)]);
    }

    /// <summary>Opens a set from its volumes' paths: the first is volume 1 (A), null where a volume is missing.</summary>
    public static AldArchive Open(IReadOnlyList<string?> volumes)
    {
        if (volumes.Count == 0 || volumes[0] is null)
            throw new AldFormatException("An ALD set needs its first volume.");
        byte[]? link = null;
        AldEntry?[]? entries = null;
        for (int v = 0; v < volumes.Count; v++)
        {
            if (volumes[v] is not { } path)
                continue;
            int volume = v + 1;
            using var file = File.OpenHandle(path);
            long size = RandomAccess.GetLength(file);
            CheckFooter(file, size, path);

            var header = new byte[6];
            ReadExactly(file, header, 0, path);
            int pointersSize = Read24(header) << 8;
            int linkEnd = Read24(header[3..]) << 8;
            if (pointersSize <= 0 || linkEnd <= pointersSize || linkEnd > size)
                throw new AldFormatException($"{path}: the pointer and link tables do not fit the file.");
            var pointers = new byte[pointersSize];
            ReadExactly(file, pointers, 0, path);
            if (link is null)
            {
                link = new byte[linkEnd - pointersSize];
                ReadExactly(file, link, pointersSize, path);
                entries = new AldEntry?[link.Length / 3];
            }

            for (int i = 0; i < entries!.Length; i++)
            {
                if (link[i * 3] != volume)
                    continue;
                int pointer = link[i * 3 + 1] | link[i * 3 + 2] << 8;
                if (pointer * 3 + 3 > pointersSize)
                    throw new AldFormatException($"{path}: resource {i} points past the pointer table ({pointer}).");
                long offset = (long)Read24(pointers.AsSpan(pointer * 3)) << 8;
                entries[i] = ReadEntry(file, size, offset, path, i, volume);
            }
        }

        for (int i = 0; i < entries!.Length; i++)
        {
            int volume = link![i * 3];
            if (volume != 0 && entries[i] is null)
                throw new AldFormatException(
                    $"{volumes[0]}: resource {i} is in volume {(char)('A' + volume - 1)}, which is missing.");
        }
        return new AldArchive(entries);
    }

    /// <summary>The footer <c>ald_read</c> checks: the file's size 16 past a sector, one of the two signatures.</summary>
    private static void CheckFooter(SafeFileHandle file, long size, string path)
    {
        if ((size & 0xff) != FooterSize)
            throw new AldFormatException($"{path}: unexpected file size (not an ALD file?).");
        Span<byte> footer = stackalloc byte[FooterSize];
        ReadExactly(file, footer, size - FooterSize, path);
        uint signature = BinaryPrimitives.ReadUInt32LittleEndian(footer);
        if (signature != Signature && signature != Signature2)
            throw new AldFormatException($"{path}: invalid signature (not an ALD file?).");
    }

    private static AldEntry ReadEntry(SafeFileHandle file, long size, long offset,
        string path, int index, int volume)
    {
        if (offset + 256 > size)
            throw new AldFormatException($"{path}: resource {index} starts past the file ({offset}).");
        Span<byte> fixedPart = stackalloc byte[EntryHeaderSize];
        ReadExactly(file, fixedPart, offset, path);
        int headerSize = BinaryPrimitives.ReadInt32LittleEndian(fixedPart);
        int dataSize = BinaryPrimitives.ReadInt32LittleEndian(fixedPart[4..]);
        long fileTime = BinaryPrimitives.ReadInt64LittleEndian(fixedPart[8..]);
        if (headerSize < EntryHeaderSize || dataSize < 0 || offset + headerSize + dataSize > size)
            throw new AldFormatException($"{path}: resource {index} does not fit the file.");

        var nameField = new byte[headerSize - EntryHeaderSize];
        ReadExactly(file, nameField, offset + EntryHeaderSize, path);
        int end = Array.IndexOf(nameField, (byte)0);
        if (end < 0)
            throw new AldFormatException($"{path}: resource {index} has no end to its name.");
        return new AldEntry(index, volume, path, offset + headerSize, dataSize, nameField[..end], fileTime);
    }

    private static void ReadExactly(SafeFileHandle file, Span<byte> buffer, long offset, string path)
    {
        if (RandomAccess.Read(file, buffer, offset) != buffer.Length)
            throw new AldFormatException($"{path}: unexpected end of file at {offset}.");
    }

    private static int Read24(ReadOnlySpan<byte> bytes) => bytes[0] | bytes[1] << 8 | bytes[2] << 16;

    private static bool AsciiEquals(ReadOnlySpan<byte> name, string ascii)
    {
        for (int i = 0; i < name.Length; i++)
        {
            if (name[i] >= 0x80 || char.ToUpperInvariant((char)name[i]) != char.ToUpperInvariant(ascii[i]))
                return false;
        }
        return true;
    }
}

/// <summary>One resource of an <see cref="AldArchive"/>: where it is, its name and its data, read on demand.</summary>
public sealed class AldEntry
{
    private readonly string _path;
    private readonly long _dataOffset;
    private readonly long _fileTime;

    internal AldEntry(int index, int volume, string path, long dataOffset, int size, byte[] rawName, long fileTime)
    {
        Index = index;
        Volume = volume;
        _path = path;
        _dataOffset = dataOffset;
        Size = size;
        RawName = rawName;
        _fileTime = fileTime;
    }

    /// <summary>The link-table index (the resource number; see <see cref="AldArchive"/>).</summary>
    public int Index { get; }

    /// <summary>The volume the data is in (A = 1).</summary>
    public int Volume { get; }

    /// <summary>The data's length in bytes.</summary>
    public int Size { get; }

    /// <summary>The name as stored, Shift JIS without its terminator (<c>Cﾒｲｷｭｳ.SCO</c>, <c>CG0807.PMS</c>).</summary>
    public ReadOnlyMemory<byte> RawName { get; }

    /// <summary>The time stored with the entry (UTC); null when it is no valid Windows file time.</summary>
    public DateTime? Written => _fileTime is >= 0 and <= 2650467743999999999 ? DateTime.FromFileTimeUtc(_fileTime) : null;

    /// <summary>Reads the data from the volume's file.</summary>
    public byte[] ReadAllBytes()
    {
        var data = new byte[Size];
        using var file = File.OpenHandle(_path);
        if (RandomAccess.Read(file, data, _dataOffset) != Size)
            throw new AldFormatException($"{_path}: unexpected end of file in resource {Index}.");
        return data;
    }
}
