// Translated from xsys35c 1.13.0 (https://github.com/kichikuou/xsys35c): decompiler/decompile.c (the passes,
// decompile_page and its helpers), decompiler/preprocess.c (scan_for_data_tables), decompiler/xsys35dc.c (sco_new,
// the main's page loop) and decompiler/xsys35dc.h (the annotations): Copyright (C) 2020
// <KichikuouChrome@gmail.com>. GNU General Public License, version 2 or later, as KichikuouGodot (LICENSE).

using System.Buffers.Binary;
using System.Text;
using Kichikuou.Content.Archives;
using Kichikuou.Content.Text;

namespace Kichikuou.Content.Decompiler;

/// <summary>One decompiled scenario file: its name as the page's header gives it (<c>Cﾒｲｷｭｳ.ADV</c>) and its text.</summary>
/// <param name="Name">The relative path, with <c>/</c> between folders (none in this game).</param>
/// <param name="Text">UTF-8 without a byte order mark, CRLF.</param>
public sealed record DecompiledFile(string Name, byte[] Text);

/// <summary>A page of the scenario archive: its bytes (an <c>.SCO</c> file), its name there (Shift JIS) and its volume.</summary>
public sealed record ScenarioPage(byte[] Bytes, byte[] Name, int Volume = 1);

/// <summary>Where the decompile stands: an analysis pass over the pages, or the pages written.</summary>
/// <param name="Pass">The analysis pass (1, 2, …), or 0 while the files are written.</param>
/// <param name="Page">The page done.</param>
/// <param name="Pages">The pages there are.</param>
public readonly record struct DecompileProgress(int Pass, int Page, int Pages);

/// <summary>
/// xsys35dc 1.13's decompiler, translated (docs/import.md, step 6): the scenario archive (<c>KICHIKUSA.ALD</c>)
/// and <c>System39.ain</c> become the <c>.ADV</c> files xsys35dc writes, byte for byte, so every line the port
/// names (<c>ScriptRef</c>, the docs, the saves) is the same line.
/// <para>
/// As xsys35dc, it marks each byte of each page (code, data, label and a construct type), first from byte scans
/// (<c>preprocess.c</c>), then by decompiling every page without output until no pass changes a mark that an
/// earlier part of a page depends on (the <c>analyzed</c> flag), and writes the files in a last pass. The text is
/// built in Shift JIS and converted when it is written.
/// </para>
/// Only what this game reaches is translated; the rest fails with the file and the address: <c>S380</c> pages
/// only, no Unicode scenario, no functions (<c>**</c>, <c>~</c> and the commands taking a function), no DLL
/// calls, no missing page. The command table is xsys35dc's whole, since its analysis passes read data as code
/// before they know it is data, and a command it knows there is not retried as data.
/// </summary>
public static class ScenarioDecompiler
{
    /// <summary>Decompiles every page of the archive, in page order.</summary>
    public static IReadOnlyList<DecompiledFile> Decompile(AldArchive scenario, AinFile ain,
        IProgress<DecompileProgress>? progress = null, Action<string>? warning = null)
    {
        int last = -1;
        for (int i = 0; i < scenario.Count; i++)
            if (scenario[i] is not null)
                last = i;
        var pages = new ScenarioPage?[last + 1];
        for (int i = 0; i <= last; i++)
            if (scenario[i] is { } entry)
                pages[i] = new ScenarioPage(entry.ReadAllBytes(), entry.RawName.ToArray(), entry.Volume);
        return Decompile(pages, ain.Messages, ain.Variables, progress, warning);
    }

    /// <summary>
    /// Decompiles pages, in page order, with the texts and the variables' names of a <c>.ain</c> (Shift JIS).
    /// The list is the archive's link table up to its last page; a null is a page missing from the archive.
    /// </summary>
    public static IReadOnlyList<DecompiledFile> Decompile(IReadOnlyList<ScenarioPage?> pages,
        IReadOnlyList<byte[]> messages, IReadOnlyList<byte[]> variables,
        IProgress<DecompileProgress>? progress = null, Action<string>? warning = null) =>
        new ScoDecompiler(pages, messages, variables, warning).Run(progress);

    /// <summary>Writes decompiled files into a folder (created, with any folders their names hold).</summary>
    public static void WriteTo(IEnumerable<DecompiledFile> files, string directory)
    {
        foreach (var file in files)
        {
            string path = Path.Combine(directory, file.Name.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, file.Text);
        }
    }
}

/// <summary>A page of the scenario (<c>Sco</c>): its bytes and the annotation of each.</summary>
internal sealed class Sco
{
    /// <summary>Bytes read past the page's end, which xsys35dc reads from the archive's padding: zeros here.</summary>
    public const int Padding = 16;

    /// <summary>The page, followed by <see cref="Padding"/> zeros.</summary>
    public required byte[] Data { get; init; }

    /// <summary>The annotation of each byte, and one past the end (a label at the end).</summary>
    public required byte[] Mark { get; init; }

    public required uint HdrSize { get; init; }

    public required uint FileSize { get; init; }

    /// <summary>The source name in the header (<c>Cﾒｲｷｭｳ.ADV</c>), Shift JIS.</summary>
    public required byte[] SrcName { get; init; }

    /// <summary>The name in the archive, Shift JIS.</summary>
    public required byte[] ScoName { get; init; }

    public required int Volume { get; init; }

    public bool Analyzed { get; set; }
}

internal sealed partial class ScoDecompiler : IDecompileErrors
{
    // Sco.mark: a 3-bit type and flags (xsys35dc.h).
    private const byte WhileStart = 1, ForStart = 2, Else = 3, ElseIf = 4, FuncallTop = 5, DataTableType = 6, TypeMask = 7;
    private const byte Code = 1 << 4, Data = 1 << 5, Label = 1 << 6, FuncTop = 1 << 7;

    private const int StringEscape = 1, StringExpand = 2;

    private readonly Sco?[] _scos;
    private readonly IReadOnlyList<byte[]> _messages;
    private readonly List<byte[]?> _variables;
    private readonly Action<string>? _warning;

    private DecompileOutput? _out;
    private int _page;
    private int _p;
    private int _indent;
    private bool _disableElse;

    public ScoDecompiler(IReadOnlyList<ScenarioPage?> pages, IReadOnlyList<byte[]> messages,
        IReadOnlyList<byte[]> variables, Action<string>? warning)
    {
        _warning = warning;
        _messages = messages;
        _variables = variables.Count > 0
            ? variables.Select(v => (byte[]?)v).ToList()
            : ["RND"u8.ToArray(), .. Enumerable.Range(1, 20).Select(i => (byte[]?)Encoding.ASCII.GetBytes($"D{i:D2}"))];

        _scos = new Sco?[pages.Count];
        for (int i = 0; i < pages.Count; i++)
        {
            // xsys35dc writes _missing<n>.adv for a gap, which this game has none of.
            var page = pages[i] ?? throw new DecompileException($"Page {i} is missing from the scenario archive.");
            _scos[i] = NewSco(page);
        }
        if (_scos.Length > 0 && _scos[0] is { } first && first.Data.AsSpan((int)first.HdrSize).StartsWith("ZU\x41\x7f"u8))
            throw new DecompileException($"{Name(first)}: a Unicode scenario is not supported.");
    }

    /// <summary><c>sco_new</c>.</summary>
    private static Sco NewSco(ScenarioPage page)
    {
        byte[] raw = page.Bytes;
        string name = Sjis.Decode(page.Name);
        if (raw.Length < 18 || !raw.AsSpan(0, 4).SequenceEqual("S380"u8))
            throw new DecompileException($"{name}: not an S380 page (only S380 is supported).");
        uint fileSize = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(8));
        if (raw.Length != fileSize)
            throw new DecompileException($"{name}: unexpected file size in SCO header (expected {raw.Length}, got {fileSize})");
        int nameLength = raw[16] | raw[17] << 8;
        var src = raw.AsSpan(18, Math.Min(nameLength, raw.Length - 18));
        int nul = src.IndexOf((byte)0);
        var data = new byte[raw.Length + Sco.Padding];
        raw.CopyTo(data, 0);
        return new Sco
        {
            Data = data,
            Mark = new byte[raw.Length + 1],
            HdrSize = BinaryPrimitives.ReadUInt32LittleEndian(raw.AsSpan(4)),
            FileSize = fileSize,
            SrcName = (nul >= 0 ? src[..nul] : src).ToArray(),
            ScoName = page.Name,
            Volume = page.Volume,
        };
    }

    /// <summary><c>decompile</c>: the byte scans, the analysis passes, the files.</summary>
    public IReadOnlyList<DecompiledFile> Run(IProgress<DecompileProgress>? progress)
    {
        Preprocess();

        bool done = false;
        for (int pass = 1; !done; pass++)
        {
            done = true;
            for (int i = 0; i < _scos.Length; i++)
            {
                var sco = _scos[i];
                if (sco is null || sco.Analyzed)
                    continue;
                done = false;
                sco.Analyzed = true;
                DecompilePage(i);
                progress?.Report(new DecompileProgress(pass, i + 1, _scos.Length));
            }
        }

        var files = new List<DecompiledFile>(_scos.Length);
        for (int i = 0; i < _scos.Length; i++)
        {
            var sco = _scos[i]!;
            _out = new DecompileOutput();
            if (sco.Volume != 1)
            {
                _out.Ascii("pragma ald_volume ");
                _out.Int(sco.Volume);
                _out.Ascii(":\n");
            }
            DecompilePage(i);
            files.Add(new DecompiledFile(Sjis.Decode(UnixPath(sco.SrcName)), _out.ToUtf8Crlf()));
            _out = null;
            progress?.Report(new DecompileProgress(0, i + 1, _scos.Length));
        }
        return files;
    }

    // ---- preprocess.c ----

    /// <summary>
    /// <c>preprocess</c>. The <c>.ain</c> names no functions in this game (<see cref="AinFile"/> refuses one that
    /// does), so <c>mark_functions_from_ain</c> marks nothing and is left out.
    /// </summary>
    private void Preprocess()
    {
        foreach (var sco in _scos)
            if (sco is not null)
                ScanForDataTables(sco);
    }

    /// <summary><c>scan_for_data_tables</c>: the bytes that look like data tables and data blocks.</summary>
    private void ScanForDataTables(Sco sco)
    {
        byte[] d = sco.Data;
        int p = (int)sco.HdrSize;
        int end = (int)sco.FileSize - 6; // -6 for address and cali

        // Scan for the pattern '#' <32-bit address> <cali>
        while (p < end && (p = Array.IndexOf(d, (byte)'#', p, end - p)) >= 0)
        {
            uint ptrAddr = Le32(d, ++p);
            if (d[p + 5] != 0x7f) // Only check if it is a simple 2-byte cali
                continue;
            if (ptrAddr < sco.HdrSize || ptrAddr > sco.FileSize - 4)
                continue;
            // Mark only backward references heuristically. Forward references
            // will be marked in the analyze phase.
            if (ptrAddr < p)
                sco.Mark[ptrAddr] |= DataTableType;

            uint dataAddr = Le32(d, (int)ptrAddr);
            if (dataAddr >= sco.HdrSize && dataAddr < sco.FileSize)
                sco.Mark[dataAddr] |= Data;
        }

        // Scan for dataSetPointer (0x2f 0x80) command
        p = (int)sco.HdrSize;
        end = (int)sco.FileSize - 7;
        while (p < end && (p = Array.IndexOf(d, (byte)0x2f, p, end - p)) >= 0)
        {
            if (d[++p] != 0x80)
                continue;
            p++;
            int page = (ushort)((d[p] | d[p + 1] << 8) - 1);
            uint addr = Le32(d, p + 2);
            if (page >= _scos.Length)
                continue;
            var target = _scos[page]!;
            if (addr >= target.FileSize)
                continue;
            // Must be adready marked using ain->functions
            if ((target.Mark[addr] & FuncTop) == 0)
                continue;
            target.Mark[addr] |= Data;
        }
    }

    // ---- decompile.c ----

    private Sco Current => _scos[_page]!;

    private static uint Le32(byte[] d, int at) => BinaryPrimitives.ReadUInt32LittleEndian(d.AsSpan(at));

    private uint Le32(int at) => Le32(Current.Data, at);

    private static string Name(Sco sco) => Sjis.Decode(sco.ScoName);

    public DecompileException At(int address, string message) =>
        new($"{Name(Current)}:{address:x}: {message}");

    public void Warning(int address, string message) =>
        _warning?.Invoke($"Warning: {Name(Current)}:{address:x}: {message}");

    private static DecompileException Error(string message) => new(message);

    private static void Annotate(ref byte mark, byte type) => mark = (byte)(mark & ~TypeMask | type);

    /// <summary><c>code_at</c>: the index of a byte of the current page, checked.</summary>
    private int CodeAt(uint addr)
    {
        if (addr >= Current.FileSize)
            throw Error($"address out of range ({_page:x}:{addr:x})");
        return (int)addr;
    }

    /// <summary><c>mark_at</c>.</summary>
    private ref byte MarkAt(int page, uint addr)
    {
        if (page >= _scos.Length)
            throw Error($"page out of range ({page:x}:{addr:x})");
        var sco = _scos[page] ?? throw Error($"page does not exist ({page:x}:{addr:x})");
        if (addr > sco.FileSize)
            throw Error($"address out of range ({page:x}:{addr:x})");
        return ref sco.Mark[addr];
    }

    /// <summary><c>advance_char</c> (Shift JIS input).</summary>
    private static int AdvanceChar(byte[] s, int at) => at + (Sjis.IsLeadByte(s[at]) ? 2 : 1);

    /// <summary><c>unix_path</c>: <c>\</c> to <c>/</c>, stepping over two-byte characters.</summary>
    private static byte[] UnixPath(byte[] path)
    {
        var buf = (byte[])path.Clone();
        for (int i = 0; i < buf.Length; i = AdvanceChar(buf, i))
            if (buf[i] == '\\')
                buf[i] = (byte)'/';
        return buf;
    }

    private void Putc(char c) => _out?.Char(c);

    private void Putc(byte c) => _out?.Byte(c);

    private void Puts(string s) => _out?.Ascii(s);

    private static bool IsGraph(byte c) => c is >= 0x21 and <= 0x7e;

    private static bool IsPrint(byte c) => c is >= 0x20 and <= 0x7e;

    /// <summary><c>dc_put_string_n</c>.</summary>
    private void PutStringN(byte[] s, int start, int length, int flags)
    {
        if (_out is null)
            return;
        int end = start + length;
        int i = start;
        while (i < end)
        {
            byte c = s[i++];
            if (IsGraph(c) || c == '\t')
            {
                if ((flags & StringEscape) != 0 && c is (byte)'\\' or (byte)'\'' or (byte)'"' or (byte)'<')
                    _out.Char('\\');
                _out.Byte(c);
            }
            else if (Sjis.IsCompacted(c))
            {
                if ((flags & StringExpand) != 0)
                {
                    ushort full = Sjis.Expand(c);
                    _out.Byte((byte)(full >> 8));
                    _out.Byte((byte)full);
                }
                else
                {
                    _out.Byte(c);
                }
            }
            else if (c is 0xde or 0xdf) // Halfwidth (semi-)voiced sound mark
            {
                _out.Byte(c);
            }
            else
            {
                if (!Sjis.IsLeadByte(c))
                    throw At(_p, $"a string holds the byte {c:x2}, which is no character");
                byte c2 = i < s.Length ? s[i] : (byte)0;
                i++;
                if ((flags & StringEscape) != 0 && !Sjis.IsUnicodeSafe(c, c2))
                {
                    _out.Ascii("<0x");
                    _out.Hex4Upper(c << 8 | c2);
                    _out.Char('>');
                }
                else
                {
                    _out.Byte(c);
                    _out.Byte(c2);
                }
            }
        }
    }

    /// <summary><c>dc_put_string</c>: the string up to its terminator, written; returns the index past the terminator.</summary>
    private int PutString(byte[] s, int start, byte terminator, int flags)
    {
        int end = start;
        while (s[end] != terminator)
        {
            if (s[end] == 0 || end + 1 >= s.Length)
                throw At(start, $"a string without its terminator 0x{terminator:x2}");
            end++;
        }
        PutStringN(s, start, end - start, flags);
        return end + 1;
    }

    private void Indent()
    {
        if (_out is null)
            return;
        for (int i = 0; i < _indent; i++)
            _out.Char('\t');
    }

    private Cali Expr(bool isLhs)
    {
        var node = CaliCode.Parse(Current.Data, ref _p, isLhs, this);
        if (_out is not null)
            CaliCode.Print(node, _variables, _out);
        return node;
    }

    /// <summary><c>page_name</c>: a page number as <c>#</c> and the page's file name.</summary>
    private void PageName(int cmd)
    {
        var node = CaliCode.Parse(Current.Data, ref _p, false, this);
        if (_out is null)
            return;
        if (node.Type == Cali.Kind.Number)
        {
            int page = node.Val;
            if ((cmd != '%' || page != 0) && page < _scos.Length && _scos[page] is { } sco)
            {
                _out.Char('#');
                _out.Bytes(UnixPath(sco.SrcName));
                return;
            }
        }
        CaliCode.Print(node, _variables, _out);
    }

    private int SubcommandNum()
    {
        int c = Current.Data[_p++];
        _out?.Int(c);
        return c;
    }

    /// <summary><c>label</c>: a jump target, marked as a label.</summary>
    private void LabelRef()
    {
        uint addr = Le32(_p);
        _p += 4;
        if (addr == 0)
        {
            Putc('0');
        }
        else
        {
            Puts("L_");
            _out?.Hex5(addr);
        }

        ref byte mark = ref MarkAt(_page, addr);
        if ((mark & Label) == 0 && addr < (uint)_p)
            Current.Analyzed = false;
        mark |= Label;
    }

    /// <summary><c>is_string_data</c> (S380: nothing to expand).</summary>
    private static bool IsStringData(byte[] d, int begin, int end)
    {
        if (d[begin] == 0 && begin + 1 == end)
            return true;
        for (int p = begin; p < end;)
        {
            if (d[p] == 0)
                return p - begin >= 2;
            if (Sjis.IsValid(d[p], d[p + 1]))
                p += 2;
            else if (IsPrint(d[p]))
                p++;
            else
                break;
        }
        return false;
    }

    /// <summary><c>data_block</c>: data as <c>"string"</c> and <c>[words]</c> lines.</summary>
    private void DataBlock(int end)
    {
        if (_out is null)
        {
            _p = end;
            return;
        }

        byte[] d = Current.Data;
        bool preferString = false;
        while (_p < end)
        {
            Indent();
            if (IsStringData(d, _p, end) || (d[_p] == 0 && (preferString || IsStringData(d, _p + 1, end))))
            {
                Putc('"');
                _p = PutString(d, _p, 0, StringEscape);
                Puts("\"\n");
                preferString = true;
                continue;
            }
            preferString = false;

            Putc('[');
            string sep = "";
            for (; _p < end && !IsStringData(d, _p, end); _p += 2)
            {
                if (_p + 1 == end)
                {
                    Warning(_p, "data block with odd number of bytes");
                    Puts(sep);
                    _out.Int(d[_p]);
                    Putc('b');
                    _p++;
                    break;
                }
                Puts(sep);
                _out.Int(d[_p] | d[_p + 1] << 8);
                sep = ", ";
            }
            Puts("]\n");
        }
    }

    /// <summary><c>data_table_addr</c>: the command <c>#</c>, a data table's address and an index.</summary>
    private void DataTableAddr()
    {
        uint addr = Le32(_p);
        _p += 4;
        Puts("L_");
        _out?.Hex5(addr);
        Puts(", ");
        Expr(false);
        Putc(':');

        ref byte mark = ref MarkAt(_page, addr);
        byte oldMark = mark;
        Annotate(ref mark, DataTableType | Label);
        if (mark != oldMark)
            Current.Analyzed = false;
    }

    /// <summary><c>data_table</c>: a table of data addresses, one <c>_L_</c> line each; false when it is none.</summary>
    private bool DataTable()
    {
        var sco = Current;
        uint pos = (uint)_p;
        uint firstData = sco.FileSize;
        while (pos < firstData)
        {
            uint addr = Le32(sco.Data, (int)pos);
            pos += 4;
            if (addr < sco.HdrSize || addr >= sco.FileSize)
                return false;
            if (pos <= addr && addr < firstData)
                firstData = addr;
            if (pos >= sco.Mark.Length)
                throw At(_p, "a data table runs past the end of the page");
            if (sco.Mark[pos] != 0)
                break;
        }

        for (; _p < pos; _p += 4)
        {
            uint addr = Le32(_p);
            Indent();
            Puts("_L_");
            _out?.Hex5(addr);
            Puts(":\n");
            if ((sco.Mark[addr] & (Data | Label)) != (Data | Label))
            {
                sco.Mark[addr] |= Data | Label;
                sco.Analyzed = false;
            }
        }
        return true;
    }

    private static uint Top(List<uint> stack) =>
        stack.Count > 0 ? stack[^1] : throw Error("stack underflow");

    private static void Pop(List<uint> stack)
    {
        if (stack.Count == 0)
            throw Error("stack underflow");
        stack.RemoveAt(stack.Count - 1);
    }

    /// <summary><c>get_surrounding_else</c>: the index of the mark of an <c>@</c> just before, when it ends the branch around.</summary>
    private int GetSurroundingElse(List<uint> branchEnd)
    {
        if (branchEnd.Count == 0)
            return -1;
        uint at = (uint)(_p - 6);
        if ((MarkAt(_page, at) & TypeMask) != Else)
            return -1;
        uint addr = Le32(_p - 5);
        if (addr != Top(branchEnd))
            return -1;
        return (int)at;
    }

    /// <summary><c>conditional</c>: the condition of <c>{</c> and its end, an <c>else</c> when the branch ends in a jump past it.</summary>
    private void Conditional(List<uint> branchEnd)
    {
        int surroundingElse = GetSurroundingElse(branchEnd);

        _indent++;
        Expr(false);
        Putc(':');
        uint endaddr = Le32(_p);
        _p += 4;

        MarkAt(_page, endaddr) |= Code;
        int epilogue = CodeAt(endaddr - 5);
        switch (Current.Data[epilogue])
        {
            case (byte)'@':
                if (!_disableElse)
                {
                    uint addr = Le32(epilogue + 1);
                    if (endaddr <= addr && addr <= Current.FileSize)
                    {
                        if (surroundingElse >= 0 && Top(branchEnd) == addr)
                        {
                            Pop(branchEnd);
                            Annotate(ref Current.Mark[surroundingElse], ElseIf);
                        }
                        ref byte m = ref MarkAt(_page, endaddr - 5);
                        if ((m & TypeMask) != ElseIf)
                            Annotate(ref m, Else);
                        endaddr = addr;
                    }
                    else
                    {
                        _disableElse = true;
                    }
                }
                break;
            case (byte)'>':
                break;
            default:
                _disableElse = true;
                break;
        }
        branchEnd.Add(endaddr);
    }

    private static bool IsBranchEnd(uint addr, List<uint> branchEnd) => branchEnd.Count > 0 && branchEnd[^1] == addr;

    /// <summary><c>for_loop</c>.</summary>
    private void ForLoop()
    {
        var sco = Current;
        MarkAt(_page, (uint)_p);
        int mark = _p - 2;
        while (mark >= 0 && (sco.Mark[mark] & Code) == 0)
            mark--;
        if (mark < 0)
            throw At(_p, "for_loop: no code before the loop");
        Annotate(ref sco.Mark[mark], ForStart);
        byte[] d = sco.Data;
        if (d[_p++] != 0)
            throw Error($"for_loop: 0 expected, got 0x{d[--_p]:x2}");
        if (d[_p++] != '<')
            throw Error($"for_loop: '<' expected, got 0x{d[--_p]:x2}");
        if (d[_p++] != 1)
            throw Error($"for_loop: 1 expected, got 0x{d[--_p]:x2}");
        _p += 4; // skip label
        CaliCode.Parse(d, ref _p, false, this); // var
        Expr(false); // e2
        Puts(", ");
        Expr(false); // e3
        Puts(", ");
        Expr(false); // e4
        Putc(':');
        _indent++;
    }

    /// <summary><c>loop_end</c>.</summary>
    private void LoopEnd(List<uint> branchEnd)
    {
        uint addr = Le32(_p);
        _p += 4;

        ref byte mark = ref MarkAt(_page, addr);
        switch (Current.Data[addr])
        {
            case (byte)'{':
                Annotate(ref mark, WhileStart);
                if (Top(branchEnd) != (uint)_p)
                    throw Error($"while-loop: unexpected address ({Top(branchEnd):x} != {_p:x})");
                Pop(branchEnd);
                break;
            case (byte)'<':
                break;
            default:
                throw At(_p - 5, $"Broken loop structure (address {addr:x} is not a valid loop start)");
        }
    }

    /// <summary><c>inline_menu_string</c>: a menu item's text up to its closing <c>$</c>, when it has one.</summary>
    private bool InlineMenuString()
    {
        byte[] d = Current.Data;
        int end = _p;
        while (d[end] == 0x20 || d[end] > 0x80)
            end = AdvanceChar(d, end);
        if (d[end] != '$')
            return false;

        PutStringN(d, _p, end - _p, StringExpand);
        _p = end;
        Putc(d[_p++]); // '$'
        return true;
    }

    /// <summary><c>ain_msg</c>: a text of the <c>.ain</c> by its number, or a command whose text must be empty.</summary>
    private void AinMsg(string? cmd, string? args)
    {
        uint id = Le32(_p);
        _p += 4;
        if (id >= _messages.Count)
            throw At(_p - 6, $"invalid message id {id}");

        if (cmd is not null)
        {
            Puts(cmd);
            Arguments(args!);
            if (_messages[(int)id].Length != 0)
                throw At(_p - 6, $"Unexpected non-empty message id {id}");
        }
        else
        {
            var message = _messages[(int)id];
            int nul = Array.IndexOf(message, (byte)0);
            Putc('\'');
            PutStringN(message, 0, nul >= 0 ? nul : message.Length, StringEscape);
            Putc('\'');
        }
    }

    /// <summary><c>decompile_page</c>: one pass over a page, written when <see cref="_out"/> is set.</summary>
    private void DecompilePage(int page)
    {
        var sco = _scos[page]!;
        _page = page;
        _p = (int)sco.HdrSize;
        _indent = 1;
        bool inMenuItem = false;
        var branchEnd = new List<uint>();
        byte[] d = sco.Data;

        while (_p < sco.FileSize)
        {
            int topaddr = _p;
            byte mark = sco.Mark[_p];
            while (IsBranchEnd((uint)topaddr, branchEnd))
            {
                Pop(branchEnd);
                _indent--;
                if (_indent <= 0)
                    throw At(_p, "a branch ends outside any block");
                Indent();
                Puts("}\n");
            }
            if ((mark & FuncTop) != 0)
                throw At(_p, "functions are not supported");
            if ((mark & Label) != 0)
            {
                Puts("*L_");
                _out?.Hex5((uint)_p);
                Puts(":\n");
            }

            if ((mark & TypeMask) == DataTableType)
            {
                if (DataTable())
                    continue;
                mark |= Data;
            }
            if ((mark & Data) != 0)
            {
                int dataEnd = _p + 1;
                for (; dataEnd < sco.FileSize; dataEnd++)
                {
                    if ((sco.Mark[dataEnd] & ~Data) != 0)
                        break;
                }
                DataBlock(dataEnd);
                continue;
            }
            if ((mark & TypeMask) == ElseIf && !_disableElse)
            {
                if (d[_p] != '@' || d[_p + 5] != '{')
                    throw At(_p, "an else-if mark not at '@' … '{'");
                _p += 6;
                _indent--;
                Pop(branchEnd);
                Indent();
                Puts("} else if {");
                Conditional(branchEnd);
                Putc('\n');
                continue;
            }
            if ((mark & TypeMask) == Else && !_disableElse)
            {
                if (d[_p] != '@')
                    throw At(_p, "an else mark not at '@'");
                _p += 5;
                if (Le32(_p - 4) != (uint)_p)
                {
                    _indent--;
                    Indent();
                    Puts("} else {\n");
                    _indent++;
                }
                continue;
            }
            if (d[_p] == '>')
                _indent--;
            Indent();
            if (d[_p] == 0 || d[_p] == 0x20 || d[_p] > 0x80)
            {
                int stringStart = _p;
                Putc('\'');
                int begin = _p;
                while (d[_p] == 0x20 || d[_p] > 0x80)
                {
                    _p = AdvanceChar(d, _p);
                    if (MarkAt(_page, (uint)_p) != 0)
                        break;
                }
                PutStringN(d, begin, _p - begin, StringEscape | StringExpand);
                Putc('\'');
                if (d[_p] == 0)
                {
                    // String data in code area. This happens when the author
                    // accidentally use double quotes instead of single quotes.
                    sco.Mark[stringStart] |= Data;
                    _p++;
                }
                else
                {
                    sco.Mark[stringStart] |= Code;
                    // Print subsequent R/A command on the same line if possible.
                    if ((d[_p] == 'R' || d[_p] == 'A')
                        && !IsBranchEnd((uint)_p, branchEnd)
                        && (sco.Mark[_p] & ~Code) == 0)
                    {
                        Putc(d[_p++]);
                    }
                }
                Putc('\n');
                continue;
            }
            sco.Mark[_p] |= Code;
            if ((mark & TypeMask) == ForStart)
            {
                if (d[_p] != '!')
                    throw At(_p, "a for-loop mark not at '!'");
                _p++;
                Putc('<');
                Expr(true);
                Puts(", ");
                Expr(false);
                Puts(", ");
                if (d[_p] != '<')
                    throw At(_p, "a for-loop without its '<'");
                _p++;
                ForLoop();
                Putc('\n');
                continue;
            }
            if ((mark & TypeMask) == WhileStart)
            {
                if (d[_p] != '{')
                    throw At(_p, "a while-loop mark not at '{'");
                _p++;
                Puts("<@");
                Conditional(branchEnd);
                Putc('\n');
                continue;
            }
            if ((mark & TypeMask) == FuncallTop)
                throw At(_p, "function calls are not supported");

            int cmd = GetCommand();
            if (!Command(cmd, branchEnd, ref inMenuItem))
            {
                if (_out is not null)
                {
                    string bytes = Convert.ToHexString(d, topaddr, _p - topaddr);
                    throw new DecompileException($"{Name(sco)}:{topaddr:x}: unknown command (bytes {bytes})");
                }
                // If we're in the analyze phase, retry as a data block.
                _p = topaddr;
                sco.Mark[topaddr] = (byte)(sco.Mark[topaddr] & ~Code | Data);
            }
            Putc('\n');
        }
        while (branchEnd.Count > 0 && Top(branchEnd) == sco.FileSize)
        {
            Pop(branchEnd);
            _indent--;
            if (_indent <= 0)
                throw At(_p - 1, "a branch ends outside any block");
            Indent();
            Puts("}\n");
        }
        if ((sco.Mark[sco.FileSize] & Label) != 0)
        {
            Puts("*L_");
            _out?.Hex5(sco.FileSize);
            Puts(":\n");
        }
    }
}
