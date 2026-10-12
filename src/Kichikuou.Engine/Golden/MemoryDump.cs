using System.Globalization;
using System.Text;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Golden;

/// <summary>A <c>DS</c> binding as recorded by the harness (<c>bind var page offset pointvar</c>).</summary>
/// <param name="Var">The bound variable.</param>
/// <param name="Page">The array page.</param>
/// <param name="Offset">Slot of record 0.</param>
/// <param name="Cursor">The cursor variable (-1 when the cursor is not a page-0 variable).</param>
public readonly record struct DumpBinding(int Var, int Page, int Offset, int Cursor);

/// <summary>
/// One memory dump of a golden capture (<c>mem … end-mem</c>, <c>docs/golden-tests.md</c>):
/// the generator state, page 0, every allocated array page, the <c>DS</c> bindings and the
/// non-empty string variables.
/// </summary>
public sealed class MemoryDump
{
    /// <summary>The dump's tag: <c>enter</c>, <c>exit</c>, <c>hook-enter</c> or <c>hook-exit</c>.</summary>
    public required string Tag { get; init; }

    /// <summary>The generator's state vector <c>mt[0..623]</c>.</summary>
    public required uint[] RngState { get; init; }

    /// <summary>The generator's index <c>mti</c>.</summary>
    public required int RngIndex { get; init; }

    /// <summary>Page 0 (<c>sysVar[]</c>), all of it (65537 slots in System 3.9).</summary>
    public required ushort[] Vars { get; init; }

    /// <summary>Every allocated array page by number.</summary>
    public required SortedDictionary<int, ushort[]> Pages { get; init; }

    /// <summary>Every bound variable.</summary>
    public required List<DumpBinding> Bindings { get; init; }

    /// <summary>Every non-empty string variable by number.</summary>
    public required SortedDictionary<int, string> Strings { get; init; }

    /// <summary>A generator in the dumped state.</summary>
    public Mt19937Legacy CreateGenerator() => Mt19937Legacy.FromState(RngState, RngIndex);

    /// <summary>
    /// Replaces the whole of <paramref name="memory"/> with this dump: variables 0..805, the
    /// pages (allocated at the dumped sizes; pages the dump lacks are released) and the strings.
    /// </summary>
    /// <exception cref="GoldenFormatException">
    /// The dump holds what the core cannot represent: a non-zero variable beyond 805 or a string
    /// number beyond <see cref="ScriptMemory.DefaultStringCount"/>.
    /// </exception>
    public void ApplyTo(ScriptMemory memory)
    {
        for (int i = memory.VariableCount; i < Vars.Length; i++)
        {
            if (Vars[i] != 0)
                throw new GoldenFormatException(
                    $"Dump '{Tag}': VAR{i:D4} = {Vars[i]} lies beyond the core's {memory.VariableCount} variables.");
        }
        for (int i = 0; i < memory.VariableCount; i++)
            memory.Var(i) = i < Vars.Length ? Vars[i] : (ushort)0;

        foreach (int page in memory.AllocatedPages.ToList())
        {
            if (!Pages.ContainsKey(page))
                memory.FreePage(page);
        }
        foreach (var (page, values) in Pages)
        {
            memory.AllocatePage(page, values.Length);
            for (int i = 0; i < values.Length; i++)
                memory.WritePage(page, i, values[i]);
        }

        for (int i = 0; i < memory.Strings.Length; i++)
            memory.Strings[i] = string.Empty;
        foreach (var (number, text) in Strings)
        {
            if (number >= memory.Strings.Length)
                throw new GoldenFormatException($"Dump '{Tag}': string {number} lies beyond the core's string table.");
            memory.Strings[number] = text;
        }
    }

    /// <summary>Takes a dump of the core's memory and generator, in the harness's layout.</summary>
    public static MemoryDump FromMemory(string tag, ScriptMemory memory, Mt19937Legacy generator,
        IEnumerable<DumpBinding> bindings)
    {
        var vars = new ushort[memory.VariableCount];
        for (int i = 0; i < vars.Length; i++)
            vars[i] = memory.Var(i).Value;
        var pages = new SortedDictionary<int, ushort[]>();
        foreach (int page in memory.AllocatedPages)
        {
            var values = new ushort[memory.PageSize(page)];
            for (int i = 0; i < values.Length; i++)
                values[i] = memory.ReadPage(page, i).Value;
            pages[page] = values;
        }
        var strings = new SortedDictionary<int, string>();
        for (int i = 0; i < memory.Strings.Length; i++)
        {
            if (memory.Strings[i].Length > 0)
                strings[i] = memory.Strings[i];
        }
        return new MemoryDump
        {
            Tag = tag,
            RngState = generator.CopyState(),
            RngIndex = generator.StateIndex,
            Vars = vars,
            Pages = pages,
            Bindings = bindings.ToList(),
            Strings = strings,
        };
    }

    // ---------------------------------------------------------------------------------------
    // Text format
    // ---------------------------------------------------------------------------------------

    private const int Row = 16;

    /// <summary>Writes the dump in the capture format, from <c>mem</c> to <c>end-mem</c>.</summary>
    public void Write(TextWriter writer)
    {
        writer.Write($"mem {Tag}\n");
        writer.Write($"rng {RngIndex}\n");
        for (int i = 0; i < RngState.Length; i += Row)
        {
            writer.Write($"rngv {i}");
            for (int j = i; j < Math.Min(i + Row, RngState.Length); j++)
                writer.Write($" {RngState[j]:x8}");
            writer.Write('\n');
        }
        writer.Write($"vars {Vars.Length}\n");
        WriteRows(writer, "v", null, Vars);
        foreach (var (page, values) in Pages)
        {
            writer.Write($"page {page} {values.Length}\n");
            WriteRows(writer, "p", page, values);
        }
        foreach (var b in Bindings)
            writer.Write($"bind {b.Var} {b.Page} {b.Offset} {b.Cursor}\n");
        foreach (var (number, text) in Strings)
            writer.Write($"str {number} {Escape(text)}\n");
        writer.Write("end-mem\n");
    }

    private static void WriteRows(TextWriter writer, string tag, int? page, ushort[] values)
    {
        for (int i = 0; i < values.Length; i += Row)
        {
            int end = Math.Min(i + Row, values.Length);
            bool nonZero = false;
            for (int j = i; j < end; j++)
                nonZero |= values[j] != 0;
            if (!nonZero)
                continue;
            writer.Write(page is null ? $"{tag} {i}" : $"{tag} {page} {i}");
            for (int j = i; j < end; j++)
                writer.Write($" {values[j]}");
            writer.Write('\n');
        }
    }

    // Only ASCII is escaped, so working on chars is the same as working on the UTF-8 bytes.
    private static string Escape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            if (c == '\\')
                sb.Append(@"\\");
            else if (c < 0x21 || c == 0x7f)
                sb.Append(CultureInfo.InvariantCulture, $"\\x{(int)c:x2}");
            else
                sb.Append(c);
        }
        return sb.ToString();
    }

    /// <summary>Decodes a <c>str</c> value: <c>\\</c> and <c>\xHH</c> escapes.</summary>
    internal static string Unescape(string text)
    {
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\\')
            {
                sb.Append(text[i]);
            }
            else if (i + 1 < text.Length && text[i + 1] == '\\')
            {
                sb.Append('\\');
                i++;
            }
            else if (i + 3 < text.Length && text[i + 1] == 'x')
            {
                sb.Append((char)int.Parse(text.AsSpan(i + 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                i += 3;
            }
            else
            {
                throw new FormatException($"Bad escape in string value '{text}'.");
            }
        }
        return sb.ToString();
    }

    /// <summary>Reads the lines after <c>mem &lt;tag&gt;</c> up to and including <c>end-mem</c>.</summary>
    internal static MemoryDump Read(CaptureReader reader, string tag)
    {
        var rng = new uint[Mt19937Legacy.StateSize];
        int rngIndex = -1;
        ushort[]? vars = null;
        var pages = new SortedDictionary<int, ushort[]>();
        var bindings = new List<DumpBinding>();
        var strings = new SortedDictionary<int, string>();

        while (true)
        {
            var line = reader.Next() ?? throw reader.Error($"dump '{tag}' has no end-mem");
            int space = line.IndexOf(' ');
            string key = space < 0 ? line : line[..space];
            if (key == "end-mem")
                break;
            if (key == "str")
            {
                int second = line.IndexOf(' ', space + 1);
                if (second < 0)
                    throw reader.Error("str needs a number and a value");
                strings[reader.Int(line[(space + 1)..second])] = Unescape(line[(second + 1)..]);
                continue;
            }
            var t = line.Split(' ');
            switch (key)
            {
                case "rng":
                    rngIndex = reader.Int(t[1]);
                    break;
                case "rngv":
                {
                    int start = reader.Int(t[1]);
                    for (int j = 2; j < t.Length; j++)
                        rng[start + j - 2] = uint.Parse(t[j], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    break;
                }
                case "vars":
                    vars = new ushort[reader.Int(t[1])];
                    break;
                case "v":
                {
                    if (vars is null)
                        throw reader.Error("v before vars");
                    int start = reader.Int(t[1]);
                    for (int j = 2; j < t.Length; j++)
                        vars[start + j - 2] = reader.UShort(t[j]);
                    break;
                }
                case "page":
                    pages[reader.Int(t[1])] = new ushort[reader.Int(t[2])];
                    break;
                case "p":
                {
                    if (!pages.TryGetValue(reader.Int(t[1]), out var values))
                        throw reader.Error("p before its page");
                    int start = reader.Int(t[2]);
                    for (int j = 3; j < t.Length; j++)
                        values[start + j - 3] = reader.UShort(t[j]);
                    break;
                }
                case "bind":
                    bindings.Add(new DumpBinding(reader.Int(t[1]), reader.Int(t[2]), reader.Int(t[3]), reader.Int(t[4])));
                    break;
                default:
                    throw reader.Error($"unknown dump record '{key}'");
            }
        }

        if (rngIndex < 0)
            throw reader.Error($"dump '{tag}' has no rng");
        return new MemoryDump
        {
            Tag = tag,
            RngState = rng,
            RngIndex = rngIndex,
            Vars = vars ?? throw reader.Error($"dump '{tag}' has no vars"),
            Pages = pages,
            Bindings = bindings,
            Strings = strings,
        };
    }
}
