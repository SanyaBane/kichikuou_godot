// Parts translated from xsys35c 1.13.0 (https://github.com/kichikuou/xsys35c), decompiler/ain.c (ain_read,
// read_strings_section: the header, the decryption, the sections): Copyright (C) 2020
// <KichikuouChrome@gmail.com>. GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

using System.Buffers.Binary;

namespace Kichikuou.Content.Archives;

/// <summary>A file that is not a readable <c>System39.ain</c>, or one with parts this reader does not know.</summary>
public sealed class AinFormatException(string message) : Exception(message);

/// <summary>
/// <c>System39.ain</c>, the System 3.9 side file next to the scenario archive (docs/import.md, step 4). In
/// the English translation it holds every text the scenario shows: a text is the command <c>2F 7C</c> with
/// a message number into <see cref="Messages"/>. It also names the variables (<see cref="Variables"/>, all
/// <c>VAR0000</c>… in this game).
/// <para>
/// The file is <c>AINI</c> or <c>AIN2</c>, a 32-bit version (1 or 2, stored plain), then sections, each a
/// 4-letter tag, 4 bytes skipped and its contents, stored with every byte rotated right by 2 bits (the reader
/// rotates every byte after the first 4 left by 2, the version's too, and skips the version). <c>VARI</c>
/// and <c>MSGI</c> are a 32-bit count and that many NUL-terminated strings. <c>HEL0</c> (the DLLs' functions)
/// and <c>FUNC</c> (the scenario's function names) are empty in this game; a file with any entry in them
/// is refused, since the decompiler reads no game that has them (docs/import.md, step 6).
/// </para>
/// The strings are kept as stored, in Shift JIS (the decoder comes with step 5).
/// </summary>
public sealed class AinFile
{
    private AinFile(int version, IReadOnlyList<byte[]> variables, IReadOnlyList<byte[]> messages)
    {
        Version = version;
        Variables = variables;
        Messages = messages;
    }

    /// <summary>The version in the header (1 or 2).</summary>
    public int Version { get; }

    /// <summary>The <c>VARI</c> section: the variables' names, by variable number; empty without the section.</summary>
    public IReadOnlyList<byte[]> Variables { get; }

    /// <summary>The <c>MSGI</c> section: the texts, by message number, in Shift JIS; empty without the section.</summary>
    public IReadOnlyList<byte[]> Messages { get; }

    /// <summary>Reads a file.</summary>
    public static AinFile Read(string path)
    {
        try
        {
            return Decode(File.ReadAllBytes(path));
        }
        catch (AinFormatException e)
        {
            throw new AinFormatException($"{path}: {e.Message}");
        }
    }

    /// <summary>Reads a file's bytes (left untouched: the decryption works on a copy).</summary>
    public static AinFile Decode(ReadOnlySpan<byte> file)
    {
        if (file.Length < 8 || !(file[..4].SequenceEqual("AINI"u8) || file[..4].SequenceEqual("AIN2"u8)))
            throw new AinFormatException("not an AIN file.");
        // The version is read as stored, before the decryption (which takes it along: ain.c:135-141).
        int version = BinaryPrimitives.ReadInt32LittleEndian(file[4..]);
        if (version is not (1 or 2))
            throw new AinFormatException($"unknown AIN version {version}.");
        var data = file.ToArray();
        for (int i = 4; i < data.Length; i++)
            data[i] = (byte)(data[i] << 2 | data[i] >> 6);

        IReadOnlyList<byte[]> variables = [], messages = [];
        int at = 8;
        while (at < data.Length)
        {
            if (at + 12 > data.Length)
                throw new AinFormatException($"a section at {at} runs past the end.");
            string tag = System.Text.Encoding.ASCII.GetString(data, at, 4);
            int count = BinaryPrimitives.ReadInt32LittleEndian(data.AsSpan(at + 8));
            at += 12;
            switch (tag)
            {
                case "HEL0" or "FUNC" when count == 0:
                    break;
                case "HEL0" or "FUNC":
                    throw new AinFormatException($"{tag} has {count} entries; only games without any are read.");
                case "VARI":
                    variables = ReadStrings(data, ref at, count, tag);
                    break;
                case "MSGI":
                    messages = ReadStrings(data, ref at, count, tag);
                    break;
                default:
                    throw new AinFormatException($"unknown ain section '{tag}' at {at - 12}.");
            }
        }
        return new AinFile(version, variables, messages);
    }

    private static byte[][] ReadStrings(byte[] data, ref int at, int count, string tag)
    {
        if (count < 0)
            throw new AinFormatException($"{tag} has a negative count.");
        var strings = new byte[count][];
        for (int i = 0; i < count; i++)
        {
            int end = Array.IndexOf(data, (byte)0, at);
            if (end < 0)
                throw new AinFormatException($"{tag} entry {i} has no end.");
            strings[i] = data[at..end];
            at = end + 1;
        }
        return strings;
    }
}
