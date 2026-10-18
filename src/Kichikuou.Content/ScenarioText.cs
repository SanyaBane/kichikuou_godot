using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Content;

/// <summary>A piece of a <see cref="TextRun"/>.</summary>
public abstract record TextSegment;

/// <summary>Literal text (<c>'…'</c>).</summary>
public sealed record LiteralText(string Text) : TextSegment;

/// <summary>A line break (<c>R</c>).</summary>
public sealed record LineBreak : TextSegment;

/// <summary>
/// A printed number (<c>H width, expr:</c> full-width digits, <c>HH width, expr:</c> ASCII
/// digits); the value comes from the event that shows the run.
/// </summary>
public sealed record NumberSlot(int Width, bool FullWidth) : TextSegment;

/// <summary>
/// A printed string variable: <c>MP var, chars:</c> (cut or padded to <see cref="Chars"/>) or
/// <c>X var:</c> (the whole string, <see cref="Chars"/> = <c>null</c>). <see cref="Variable"/> is
/// <c>null</c> when the script computes the variable number; the event then supplies it.
/// </summary>
public sealed record StringSlot(int? Variable, int? Chars) : TextSegment;

/// <summary>
/// The text the script prints from one location: the consecutive text commands starting at a
/// <see cref="ScriptRef"/> (<c>'…'</c>, <c>R</c>, <c>H</c>/<c>HH</c>, <c>MP</c>, <c>X</c>, with
/// <c>ZH</c> switches in between), up to the first other command.
/// </summary>
public sealed record TextRun(ScriptRef Source, IReadOnlyList<TextSegment> Segments)
{
    /// <summary>The number of <see cref="NumberSlot"/>s, in the order their values are expected.</summary>
    public int NumberCount => Segments.Count(s => s is NumberSlot);

    /// <summary>The number of string slots whose variable the event must supply.</summary>
    public int ComputedStringCount => Segments.Count(s => s is StringSlot { Variable: null });

    /// <summary>
    /// The text as the engine prints it: numbers from <paramref name="numbers"/> in order
    /// (formatted like xsystem35 <c>format_number</c>/<c>format_number_zenkaku</c>), string
    /// variables read with <paramref name="strings"/> (the computed ones numbered by
    /// <paramref name="stringVariables"/> in order; <c>MP</c> cuts and pads like
    /// <c>commandMP</c>), line breaks as <c>\n</c>. Everything then goes through the engine's
    /// full-width → half-width conversion (<see cref="Hankaku"/>): the scenario sets only
    /// <c>ZH 1</c>, so it is on for every message (<c>nact.c</c> <c>sys_addMsg</c>).
    /// </summary>
    public string Format(
        IReadOnlyList<Sys3Int>? numbers = null,
        Func<int, string>? strings = null,
        IReadOnlyList<Sys3Int>? stringVariables = null)
    {
        var sb = new StringBuilder();
        int next = 0;
        int nextString = 0;
        foreach (var segment in Segments)
        {
            switch (segment)
            {
                case LiteralText t:
                    sb.Append(t.Text);
                    break;
                case LineBreak:
                    sb.Append('\n');
                    break;
                case NumberSlot n:
                    if (numbers is null || next >= numbers.Count)
                        throw new ArgumentException($"{Source}: the text prints {NumberCount} numbers, {numbers?.Count ?? 0} given.", nameof(numbers));
                    sb.Append(FormatNumber(numbers[next++].Value, n.Width, n.FullWidth));
                    break;
                case StringSlot s:
                    int variable;
                    if (s.Variable is int fixedVariable)
                        variable = fixedVariable;
                    else if (stringVariables is not null && nextString < stringVariables.Count)
                        variable = stringVariables[nextString++].Value;
                    else
                        throw new ArgumentException($"{Source}: the text prints {ComputedStringCount} computed string variables, {stringVariables?.Count ?? 0} given.", nameof(stringVariables));
                    string value = strings?.Invoke(variable) ?? "";
                    sb.Append(s.Chars is int chars ? FormatString(value, chars) : value);
                    break;
            }
        }
        return Hankaku.ToHalfWidth(sb.ToString());
    }

    /// <summary>
    /// The text as it was printed into a menu item (<see cref="CapturedText"/>): the numbers in
    /// order, and each string slot, fixed or computed, filled with the next of
    /// <paramref name="strings"/> (their contents when printed); line breaks as <c>\n</c>.
    /// </summary>
    public string FormatCaptured(IReadOnlyList<Sys3Int> numbers, IReadOnlyList<string> strings)
    {
        var sb = new StringBuilder();
        int next = 0;
        int nextString = 0;
        foreach (var segment in Segments)
        {
            switch (segment)
            {
                case LiteralText t:
                    sb.Append(t.Text);
                    break;
                case LineBreak:
                    sb.Append('\n');
                    break;
                case NumberSlot n:
                    if (next >= numbers.Count)
                        throw new ArgumentException($"{Source}: the text prints {NumberCount} numbers, {numbers.Count} captured.", nameof(numbers));
                    sb.Append(FormatNumber(numbers[next++].Value, n.Width, n.FullWidth));
                    break;
                case StringSlot s:
                    if (nextString >= strings.Count)
                        throw new ArgumentException($"{Source}: the text prints more strings than the {strings.Count} captured.", nameof(strings));
                    string value = strings[nextString++];
                    sb.Append(s.Chars is int chars ? FormatString(value, chars) : value);
                    break;
            }
        }
        return Hankaku.ToHalfWidth(sb.ToString());
    }

    /// <summary>
    /// xsystem35 <c>format_number</c> (<c>hankaku.c</c>): <c>%*d</c> with the width, of which only
    /// the last <paramref name="width"/> characters are kept; full-width digits and blanks for <c>H</c>.
    /// </summary>
    public static string FormatNumber(int value, int width, bool fullWidth) =>
        Kichikuou.Engine.Sys3.Sys3Text.FormatNumber(value, width, fullWidth);

    /// <summary>
    /// xsystem35 <c>commandMP</c> (<c>cmdm.c</c>, non-English-patch branch): the first
    /// <paramref name="chars"/> characters of the string, padded with full-width blanks.
    /// </summary>
    public static string FormatString(string value, int chars)
    {
        var elements = StringInfo.GetTextElementEnumerator(value);
        var sb = new StringBuilder();
        int count = 0;
        while (count < chars && elements.MoveNext())
        {
            sb.Append(elements.GetTextElement());
            count++;
        }
        sb.Append('　', chars - count);
        return sb.ToString();
    }
}

/// <summary>
/// Reads message texts from the player's decompiled scenario (one <c>.ADV</c> file per scenario
/// page, as written by <c>xsys35dc</c>): the texts are never part of this project.
/// </summary>
public sealed class ScenarioText(string directory)
{
    private readonly ConcurrentDictionary<string, string[]> _files = new(StringComparer.Ordinal);

    /// <summary>The folder with the decompiled <c>.ADV</c> files.</summary>
    public string Directory { get; } = directory;

    /// <summary>The text run that starts at <paramref name="source"/>.</summary>
    /// <exception cref="ScenarioTextException">
    /// The location holds no text command, or it follows an <c>R</c> (only <c>ZH</c> switches in
    /// between) that the run leaves out: that line break would be lost, or printed twice if an
    /// earlier run reaches it, so the reference must point at the <c>R</c>.
    /// </exception>
    public TextRun Read(ScriptRef source)
    {
        string[] lines = Lines(source.File);
        if (source.Line < 1 || source.Line > lines.Length)
            throw new ScenarioTextException($"{source}: the file has {lines.Length} lines.");
        for (int i = source.Line - 2; i >= 0; i--)
        {
            string before = lines[i].Trim();
            if (before == "R")
                throw new ScenarioTextException($"{source}: the run starts after the line break at line {i + 1}; point the reference at it.");
            if (!(before.StartsWith("ZH ", StringComparison.Ordinal) && before.EndsWith(':')))
                break;
        }
        var segments = new List<TextSegment>();
        for (int i = source.Line - 1; i < lines.Length; i++)
        {
            if (!TryParse(lines[i].Trim(), segments))
                break;
        }
        if (!segments.Any(s => s is not LineBreak))
            throw new ScenarioTextException($"{source}: no text at this line ('{lines[source.Line - 1].Trim()}').");
        return new TextRun(source, segments);
    }

    private string[] Lines(string file) => _files.GetOrAdd(file, f =>
    {
        string path = Path.Combine(Directory, f);
        if (!File.Exists(path))
            throw new ScenarioTextException($"Scenario file {path} not found.");
        return File.ReadAllLines(path, Encoding.UTF8);
    });

    /// <summary>Adds the segment of one text command; false for any other command (the run ends).</summary>
    internal static bool TryParse(string line, List<TextSegment> segments)
    {
        if (line.Length >= 2 && line[0] == '\'' && line[^1] == '\'')
        {
            segments.Add(new LiteralText(Unescape(line[1..^1])));
            return true;
        }
        if (line == "R")
        {
            segments.Add(new LineBreak());
            return true;
        }
        if (line.StartsWith("ZH ", StringComparison.Ordinal) && line.EndsWith(':'))
            return true;   // full-width conversion switch: does not print
        if (TryArgs(line, "HH ", out var args) && args.Length == 2 && int.TryParse(args[0], out int hw))
        {
            segments.Add(new NumberSlot(hw, FullWidth: false));
            return true;
        }
        if (TryArgs(line, "H ", out args) && args.Length == 2 && int.TryParse(args[0], out int w))
        {
            segments.Add(new NumberSlot(w, FullWidth: true));
            return true;
        }
        if (TryArgs(line, "MP ", out args) && args.Length == 2
            && int.TryParse(args[0], out int variable) && int.TryParse(args[1], out int chars))
        {
            segments.Add(new StringSlot(variable, chars));
            return true;
        }
        if (TryArgs(line, "X ", out args) && args.Length == 1)
        {
            // xsystem35 cmd_check.c: X prints the whole string variable (sys_addMsg(svar_get(n))).
            segments.Add(new StringSlot(int.TryParse(args[0], out int whole) ? whole : null, null));
            return true;
        }
        return false;
    }

    /// <summary><c>CMD a, b:</c> → <c>[a, b]</c> (split at top-level commas).</summary>
    private static bool TryArgs(string line, string command, out string[] args)
    {
        args = [];
        if (!line.StartsWith(command, StringComparison.Ordinal) || !line.EndsWith(':'))
            return false;
        args = line[command.Length..^1].Split(',').Select(a => a.Trim()).ToArray();
        return true;
    }

    /// <summary><c>xsys35dc</c> escapes <c>'</c> and <c>\</c> with a backslash inside messages.</summary>
    private static string Unescape(string text)
    {
        if (!text.Contains('\\'))
            return text;
        var sb = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\\' && i + 1 < text.Length)
                i++;
            sb.Append(text[i]);
        }
        return sb.ToString();
    }
}

/// <summary>A message text could not be read from the scenario.</summary>
public sealed class ScenarioTextException(string message) : Exception(message);
