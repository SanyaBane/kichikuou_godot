using System.Buffers.Binary;
using System.Text;
using Kichikuou.Content.Text;

namespace Kichikuou.Content.Saves;

/// <summary>
/// A save file of the original engine, "System3.8 SaveData" — what <c>System39.exe</c> and xsystem35 write, one file
/// per slot (<c>KICHIKUSA.asd</c> …; xsystem35 <c>savedata.c</c>, <c>save_saveAll</c>), read whole and checked as a
/// file: the header (the resume point, the offsets), the engine's call stack, the string variables and every variable
/// page saved (page 0 the numbered variables). The selection and message windows' rectangles and the font sizes and
/// colours the header also holds are not kept (no save of the port keeps them). Whether the save belongs to a game the
/// port knows is the variant's question (docs/save-import.md, §2), not the reader's.
/// </summary>
public sealed class AsdSave
{
    /// <summary>The signature of the format (<c>SAVEFMT_SYS38</c>).</summary>
    public const string Signature = "System3.8 SaveData(c)ALICE-SOFT";

    /// <summary>The header's version field (<c>SAVE_DATAVERSION</c>).</summary>
    public const int DataVersion = 0x350200;

    /// <summary>The header's size: the rest of the file is reached through its offsets.</summary>
    public const int HeaderSize = 4096;

    private const int SelectionWindows = 128, MessageWindows = 128;
    private const int StackOffset = 96 + 8 * (SelectionWindows + MessageWindows);    // stackinfo
    private const int PageOffsets = StackOffset + 16;                                  // varSys[256]
    private const int PageCount = 256;
    private const int PartHeader = 16;                                                 // every part's own header

    private AsdSave(int resumePage, int resumeAddress, IReadOnlyList<AsdFrame> stack, IReadOnlyList<string> strings,
        IReadOnlyDictionary<int, ushort[]> pages, bool xsystem35)
    {
        ResumePage = resumePage;
        ResumeAddress = resumeAddress;
        Stack = stack;
        Strings = strings;
        Pages = pages;
        WrittenByXSystem35 = xsystem35;
    }

    /// <summary>The page the game goes on in after a load (0-based, the archive's link order; the header keeps it + 1).</summary>
    public int ResumePage { get; }

    /// <summary>The address in <see cref="ResumePage"/> the game goes on at: right after the <c>QD</c> that saved.</summary>
    public int ResumeAddress { get; }

    /// <summary>The engine's call stack at the save, bottom first.</summary>
    public IReadOnlyList<AsdFrame> Stack { get; }

    /// <summary>The string variables by the engine's number from 0 (the port's <c>ScriptMemory.Strings</c> numbering).</summary>
    public IReadOnlyList<string> Strings { get; }

    /// <summary>The saved variable pages by number, page 0 the numbered variables, each at its saved size.</summary>
    public IReadOnlyDictionary<int, ushort[]> Pages { get; }

    /// <summary>Whether xsystem35 wrote the file (its footer's <c>XS35</c> mark); <c>System39.exe</c> writes no footer.</summary>
    public bool WrittenByXSystem35 { get; }

    /// <summary>Reads a save file.</summary>
    /// <exception cref="AsdFormatException">The file is not a save of this format, or a damaged one.</exception>
    /// <exception cref="IOException">The file cannot be read.</exception>
    public static AsdSave Read(string path) => Read(File.ReadAllBytes(path));

    /// <summary>Reads a save file's bytes.</summary>
    /// <exception cref="AsdFormatException">They are not a save of this format, or a damaged one.</exception>
    public static AsdSave Read(ReadOnlySpan<byte> file)
    {
        if (file.Length < 36)
            throw new AsdFormatException("It is not a save file of the original game: it is too short.");
        var id = file[..32];
        int nul = id.IndexOf((byte)0);
        string signature = Encoding.ASCII.GetString(nul >= 0 ? id[..nul] : id);
        if (signature != Signature)
        {
            throw new AsdFormatException(signature.StartsWith("System3.5 Sa", StringComparison.Ordinal)
                ? $"It is a save of an older engine (\"{signature}\"), not of System 3.9."
                : "It is not a save file of the original game.");
        }
        if (file.Length < HeaderSize)
            throw new AsdFormatException("The save file is cut short: its header is not whole.");
        if (Int(file, 32) != DataVersion)
            throw new AsdFormatException($"The save file has an unknown version 0x{Int(file, 32):x}.");

        int resumePage = Int(file, 80) - 1, resumeAddress = Int(file, 84);
        var stack = ReadStack(file, Int(file, StackOffset));
        var strings = ReadStrings(file, Int(file, StackOffset + 4));
        var pages = new Dictionary<int, ushort[]>();
        int end = 0;
        for (int page = 0; page < PageCount; page++)
        {
            int at = Int(file, PageOffsets + 4 * page);
            if (at == 0)
                continue;
            int size = Part(file, at, $"page {page}");
            if (Int(file, at + 4) != page)
                throw new AsdFormatException($"The save file is damaged: page {page}'s part says it is page {Int(file, at + 4)}.");
            if (size % 2 != 0 || size / 2 > 65537)
                throw new AsdFormatException($"The save file is damaged: page {page} has {size} bytes.");
            var values = new ushort[size / 2];
            for (int i = 0; i < values.Length; i++)
                values[i] = BinaryPrimitives.ReadUInt16LittleEndian(file[(at + PartHeader + 2 * i)..]);
            pages[page] = values;
            end = Math.Max(end, at + PartHeader + size);
        }
        if (!pages.ContainsKey(0))
            throw new AsdFormatException("The save file is damaged: it holds no numbered variables (page 0).");
        bool xsystem35 = file[end..].IndexOf("XS35"u8) >= 0;
        return new AsdSave(resumePage, resumeAddress, stack, strings, pages, xsystem35);
    }

    /// <summary>
    /// The stack (<c>sl_saveStack</c>, the format's own: xsystem35's stack buffer as it is), read from its top down by
    /// each frame's last byte: a near call <c>0xEE</c> (the return address), a far call <c>0xFF</c> (the page + 1 and
    /// the address), a variable pushed <c>0xDD</c> (its index, page and value), the text's colour <c>0xCC</c>, size
    /// <c>0xBB</c> and place <c>0xAA</c>.
    /// </summary>
    private static List<AsdFrame> ReadStack(ReadOnlySpan<byte> file, int at)
    {
        int size = Part(file, at, "the call stack");
        var stack = file.Slice(at + PartHeader, size);
        var frames = new List<AsdFrame>();
        int top = stack.Length;
        while (top > 0)
        {
            byte tag = stack[top - 1];
            int length = tag switch
            {
                0xEE => 5,
                0xFF => 7,
                0xDD => 7,
                0xCC => 3,
                0xBB => 6,
                0xAA => 9,
                _ => throw new AsdFormatException($"The save file is damaged: its call stack has an unknown frame 0x{tag:x2}."),
            };
            if (top < length)
                throw new AsdFormatException("The save file is damaged: its call stack is cut short.");
            var frame = stack.Slice(top - length, length);
            frames.Add(tag switch
            {
                0xEE => new AsdNearCall(BinaryPrimitives.ReadInt32LittleEndian(frame)),
                0xFF => new AsdFarCall(BinaryPrimitives.ReadUInt16LittleEndian(frame) - 1, BinaryPrimitives.ReadInt32LittleEndian(frame[2..])),
                0xDD => new AsdVariableFrame(BinaryPrimitives.ReadUInt16LittleEndian(frame[2..]),
                    BinaryPrimitives.ReadUInt16LittleEndian(frame), BinaryPrimitives.ReadUInt16LittleEndian(frame[4..])),
                _ => new AsdTextFrame(tag),
            });
            top -= length;
        }
        frames.Reverse();
        return frames;
    }

    /// <summary>The strings (<c>saveStrVars</c>): a count, then that many zero-ended Shift JIS strings from number 0.</summary>
    private static string[] ReadStrings(ReadOnlySpan<byte> file, int at)
    {
        int size = Part(file, at, "the strings");
        int count = Int(file, at + 4);
        if (count is < 0 or > 65536)
            throw new AsdFormatException($"The save file is damaged: it says it holds {count} strings.");
        var bytes = file.Slice(at + PartHeader, size);
        var strings = new string[count];
        for (int i = 0; i < count; i++)
        {
            int nul = bytes.IndexOf((byte)0);
            if (nul < 0)
                throw new AsdFormatException($"The save file is damaged: its strings end at string {i} of {count}.");
            strings[i] = nul == 0 ? string.Empty : Sjis.Decode(bytes[..nul]);
            bytes = bytes[(nul + 1)..];
        }
        return strings;
    }

    /// <summary>A part's size from its header at <paramref name="at"/>, checked to lie in the file.</summary>
    private static int Part(ReadOnlySpan<byte> file, int at, string what)
    {
        if (at < HeaderSize || at > file.Length - PartHeader)
            throw new AsdFormatException($"The save file is damaged: {what} lie outside it.");
        int size = Int(file, at);
        if (size < 0 || size > file.Length - at - PartHeader)
            throw new AsdFormatException($"The save file is cut short: {what} are not whole.");
        return size;
    }

    private static int Int(ReadOnlySpan<byte> file, int at) => BinaryPrimitives.ReadInt32LittleEndian(file[at..]);
}

/// <summary>A frame of the engine's call stack in a save file.</summary>
public abstract record AsdFrame;

/// <summary>A call of a label (<c>\label:</c>): the game returns to <paramref name="Address"/> in the same page.</summary>
public sealed record AsdNearCall(int Address) : AsdFrame;

/// <summary>A call of a page (<c>%page:</c>): the game returns to <paramref name="Address"/> in <paramref name="Page"/> (0-based).</summary>
public sealed record AsdFarCall(int Page, int Address) : AsdFrame;

/// <summary>A variable pushed on the stack (the call's own variables): put back to <paramref name="Value"/> at the return.</summary>
public sealed record AsdVariableFrame(int Page, int Index, int Value) : AsdFrame;

/// <summary>The text's colour, size or place pushed on the stack (<paramref name="Tag"/> <c>0xCC</c>, <c>0xBB</c>, <c>0xAA</c>).</summary>
public sealed record AsdTextFrame(byte Tag) : AsdFrame;

/// <summary>A file that is not a save of the original's format, or a damaged one; the message says why in words.</summary>
public sealed class AsdFormatException(string message) : Exception(message);
