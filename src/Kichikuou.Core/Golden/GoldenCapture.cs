using System.Globalization;

namespace Kichikuou.Core.Golden;

/// <summary>A capture file is malformed, or holds what the core cannot represent.</summary>
public sealed class GoldenFormatException(string message) : Exception(message);

/// <summary>A command location in the original game: scenario file and byte address.</summary>
/// <param name="File">The scenario file, e.g. <c>FIGT.ADV</c>.</param>
/// <param name="Address">The command's address (the decompiler's <c>L_xxxxx</c> labels use the same numbers).</param>
public readonly record struct GoldenLocation(string File, int Address)
{
    /// <inheritdoc />
    public override string ToString() => $"{File}@{Address:x}";
}

/// <summary>One event of a capture, in the order the original produced it.</summary>
/// <param name="Line">Line in the capture file.</param>
/// <param name="Where">Where the original executed it.</param>
public abstract record GoldenRecord(int Line, GoldenLocation Where);

/// <summary><c>rnd n result</c>: a <c>Y 4, n:</c> that stored <paramref name="Result"/>.</summary>
public sealed record RndRecord(int Line, GoldenLocation Where, int N, int Result) : GoldenRecord(Line, Where);

/// <summary><c>key code</c>: an <c>IK 0</c>/<c>IK 1</c> that stored <paramref name="Code"/>.</summary>
public sealed record KeyRecord(int Line, GoldenLocation Where, int Code) : GoldenRecord(Line, Where);

/// <summary><c>wait ticks code</c>: a timed <c>Y 3</c> (diagnostic only; the core has no input for it).</summary>
public sealed record WaitRecord(int Line, GoldenLocation Where, int Ticks, int Code) : GoldenRecord(Line, Where);

/// <summary><c>menu choice count</c>: a menu; <paramref name="Choice"/> is 1-based, 0 = cancelled.</summary>
public sealed record MenuRecord(int Line, GoldenLocation Where, int Choice, int Count) : GoldenRecord(Line, Where);

/// <summary><c>music track</c>: an <c>SR 0</c>.</summary>
public sealed record MusicRecord(int Line, GoldenLocation Where, int Track) : GoldenRecord(Line, Where);

/// <summary><c>pick retval tmp0001</c>: the result pair of a pick file (<c>FIGT_SEL.ADV</c>).</summary>
public sealed record PickRecord(int Line, GoldenLocation Where, int RetVal, int Tmp0001) : GoldenRecord(Line, Where);

/// <summary>
/// <c>call file</c> … <c>ret file</c>: a call into a hook file with the memory at entry and at
/// return.
/// </summary>
public sealed record HookRecord(int Line, GoldenLocation Where, string File, MemoryDump Enter, MemoryDump Exit)
    : GoldenRecord(Line, Where);

/// <summary><c>jump file</c>: the battle left by a page jump; the capture's last event.</summary>
public sealed record JumpRecord(int Line, GoldenLocation Where, string File) : GoldenRecord(Line, Where);

/// <summary>
/// One battle recorded by the harness (<c>docs/golden-tests.md</c>): the memory at entry and
/// exit and every draw, input and hook call in between.
/// </summary>
public sealed class GoldenCapture
{
    /// <summary>The format version this parser reads.</summary>
    public const int FormatVersion = 1;

    /// <summary>Where the capture was read from (file name or a description).</summary>
    public required string Source { get; init; }

    /// <summary>The <c>sgenrand()</c> seed of the session.</summary>
    public required uint Seed { get; init; }

    /// <summary>The root file the battle entered (<c>FIGT_A.ADV</c>, <c>FIGT_B.ADV</c>, <c>FIGT.ADV</c>).</summary>
    public required string Root { get; init; }

    /// <summary>The call site of the root in its caller.</summary>
    public required GoldenLocation RootCall { get; init; }

    /// <summary>Memory right after the root call.</summary>
    public required MemoryDump Enter { get; init; }

    /// <summary>The events in order.</summary>
    public required IReadOnlyList<GoldenRecord> Records { get; init; }

    /// <summary>Memory after the return (or at the jump).</summary>
    public required MemoryDump Exit { get; init; }

    /// <summary>The file the battle jumped to, or <c>null</c> when it returned.</summary>
    public required string? JumpTarget { get; init; }

    /// <summary>Reads a capture file.</summary>
    public static GoldenCapture Load(string path)
    {
        using var reader = new StreamReader(path, System.Text.Encoding.UTF8);
        return Parse(reader, Path.GetFileName(path));
    }

    /// <summary>Parses a capture.</summary>
    public static GoldenCapture Parse(TextReader text, string source)
    {
        var reader = new CaptureReader(text, source);
        var header = reader.Next() ?? throw reader.Error("empty capture");
        var h = header.Split(' ');
        if (h[0] != "golden" || h.Length != 2 || reader.Int(h[1]) != FormatVersion)
            throw reader.Error($"expected 'golden {FormatVersion}'");

        uint seed = 0;
        string? root = null;
        GoldenLocation rootCall = default;
        MemoryDump? enter = null;
        MemoryDump? exit = null;
        string? jumpTarget = null;
        bool ended = false;
        var records = new List<GoldenRecord>();
        (int Line, GoldenLocation Where, string File, MemoryDump? Enter)? openHook = null;

        string? line;
        while (!ended && (line = reader.Next()) is not null)
        {
            var t = line.Split(' ');
            int n = reader.LineNumber;
            switch (t[0])
            {
                case "seed":
                    seed = uint.Parse(t[1], CultureInfo.InvariantCulture);
                    break;
                case "root":
                    root = t[1];
                    rootCall = reader.Where(t[2]);
                    break;
                case "mem":
                {
                    var dump = MemoryDump.Read(reader, t[1]);
                    switch (t[1])
                    {
                        case "enter":
                            enter = dump;
                            break;
                        case "exit":
                            exit = dump;
                            break;
                        case "hook-enter":
                            if (openHook is not { Enter: null } hook)
                                throw reader.Error("hook-enter without call");
                            openHook = hook with { Enter = dump };
                            break;
                        case "hook-exit":
                            if (openHook is not { Enter: not null } open)
                                throw reader.Error("hook-exit without hook-enter");
                            records.Add(new HookRecord(open.Line, open.Where, open.File, open.Enter, dump));
                            openHook = null;
                            break;
                        default:
                            throw reader.Error($"unknown dump tag '{t[1]}'");
                    }
                    break;
                }
                case "rnd":
                    records.Add(new RndRecord(n, reader.Where(t[3]), reader.Int(t[1]), reader.Int(t[2])));
                    break;
                case "key":
                    records.Add(new KeyRecord(n, reader.Where(t[2]), reader.Int(t[1])));
                    break;
                case "wait":
                    records.Add(new WaitRecord(n, reader.Where(t[3]), reader.Int(t[1]), reader.Int(t[2])));
                    break;
                case "menu":
                    records.Add(new MenuRecord(n, reader.Where(t[3]), reader.Int(t[1]), reader.Int(t[2])));
                    break;
                case "music":
                    records.Add(new MusicRecord(n, reader.Where(t[2]), reader.Int(t[1])));
                    break;
                case "pick":
                    records.Add(new PickRecord(n, reader.Where(t[3]), reader.Int(t[1]), reader.Int(t[2])));
                    break;
                case "call":
                    if (openHook is not null)
                        throw reader.Error("nested hook call");
                    openHook = (n, reader.Where(t[2]), t[1], null);
                    break;
                case "ret":
                    // The hook record was completed by its hook-exit dump.
                    break;
                case "jump":
                    records.Add(new JumpRecord(n, reader.Where(t[2]), t[1]));
                    break;
                case "end":
                    if (t[1] == "jump")
                        jumpTarget = t[2];
                    else if (t[1] != "return")
                        throw reader.Error($"capture ended with '{line}'");
                    ended = true;
                    break;
                default:
                    throw reader.Error($"unknown record '{t[0]}'");
            }
        }

        if (!ended)
            throw reader.Error("capture has no end record (incomplete)");
        if (openHook is not null)
            throw reader.Error("capture ends inside a hook");
        return new GoldenCapture
        {
            Source = source,
            Seed = seed,
            Root = root ?? throw reader.Error("no root record"),
            RootCall = rootCall,
            Enter = enter ?? throw reader.Error("no enter dump"),
            Records = records,
            Exit = exit ?? throw reader.Error("no exit dump"),
            JumpTarget = jumpTarget,
        };
    }
}

/// <summary>Line reader with error reporting for capture files.</summary>
internal sealed class CaptureReader(TextReader text, string source)
{
    public int LineNumber { get; private set; }

    /// <summary>The next non-empty line, or <c>null</c> at the end.</summary>
    public string? Next()
    {
        string? line;
        do
        {
            line = text.ReadLine();
            LineNumber++;
        }
        while (line is not null && line.Length == 0);
        return line;
    }

    public GoldenFormatException Error(string message) => new($"{source}:{LineNumber}: {message}");

    public int Int(string s) =>
        int.TryParse(s, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int v) ? v : throw Error($"bad number '{s}'");

    public ushort UShort(string s) =>
        ushort.TryParse(s, NumberStyles.None, CultureInfo.InvariantCulture, out ushort v) ? v : throw Error($"bad value '{s}'");

    /// <summary>Parses <c>file@hexaddress</c>.</summary>
    public GoldenLocation Where(string s)
    {
        int at = s.LastIndexOf('@');
        if (at < 0 || !int.TryParse(s.AsSpan(at + 1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int address))
            throw Error($"bad location '{s}'");
        return new GoldenLocation(s[..at], address);
    }
}
