using System.Text;
using System.Text.RegularExpressions;
using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Scripts;

/// <summary>
/// Reads scenario code from the player's raw decompile (<c>xsys35dc</c> output, variables as
/// <c>VARnnnn</c>), so data files such as <c>SET_CHR.ADV</c> and the story scenes run from the
/// player's own copy and nothing derived from the game is committed. Only the forms the port
/// knows are accepted; anything else fails loudly with its location, so a different version or
/// translation of the scenario cannot be misread silently.
/// </summary>
/// <remarks>
/// <see cref="ReadProgram"/> reads a whole file as a goto program (<see cref="ScenarioParser"/>).
/// <see cref="ReadFile"/> reads a straight-line data file: <c>!VARnnnn : expr!</c>,
/// <c>MS n, "text":</c>, <c>MM expr, expr:</c>, <c>#L_xxxxx, 1:</c> (resolved to the word table
/// its address word names), <c>F3, VARnnnn, expr:</c>, <c>%#FILE.ADV:</c> and <c>%0:</c> (which
/// ends the file's code); <c>ZH n:</c> and <c>R</c> are message formatting and produce nothing.
/// </remarks>
public sealed partial class ScenarioScripts(string directory) : IScenarioSource, ISceneSource
{
    private readonly Dictionary<string, string[]> _files = new(StringComparer.Ordinal);

    /// <summary>The folder with the raw decompiled <c>.ADV</c> files.</summary>
    public string Directory { get; } = directory;

    /// <inheritdoc />
    public ScriptProgram ReadProgram(string file) => ScenarioParser.Program(file, Lines(file));

    /// <inheritdoc />
    public IReadOnlyList<ScriptStatement> ReadFile(string file)
    {
        string[] lines = Lines(file);
        var statements = new List<ScriptStatement>();
        for (int i = 0; i < lines.Length; i++)
        {
            var at = new ScriptRef(file, i + 1);
            var statement = ParseData(at, lines[i].Trim());
            if (statement is null)
                continue;
            statements.Add(statement);
            if (statement is ReturnStatement)
                return statements;
        }
        throw new ScenarioFormatException($"{file}: no return (%0:) found.");
    }

    /// <inheritdoc />
    public ScriptStatement ReadStatement(ScriptRef at)
    {
        string[] lines = Lines(at.File);
        if (at.Line < 1 || at.Line > lines.Length)
            throw new ScenarioFormatException($"{at}: the file has {lines.Length} lines.");
        return ParseData(at, lines[at.Line - 1].Trim())
            ?? throw new ScenarioFormatException($"{at}: no statement at this line.");
    }

    /// <summary>One line of a data file (the forms listed in the remarks).</summary>
    private ScriptStatement? ParseData(ScriptRef at, string line)
    {
        if (line.Length == 0 || line == "R" || ZhRegex().IsMatch(line))
            return null;
        Match m;
        if ((m = ScenarioParser.AssignRegex().Match(line)).Success)
            return new AssignStatement(at, ScenarioParser.Var(m.Groups[1].Value), ScenarioParser.Expr(at, m.Groups[2].Value));
        if ((m = ScenarioParser.SetStringRegex().Match(line)).Success)
            return new SetStringStatement(at, ScenarioParser.Number(at, m.Groups[1].Value), ScenarioParser.Unescape(m.Groups[2].Value));
        if ((m = CopyStringRegex().Match(line)).Success)
            return new CopyStringStatement(at, ScenarioParser.Expr(at, m.Groups[1].Value), ScenarioParser.Expr(at, m.Groups[2].Value));
        if ((m = DataTableRegex().Match(line)).Success)
            return new SetDataTableStatement(at, DataTable(at, m.Groups[1].Value));
        if ((m = ReadDataRegex().Match(line)).Success)
            return new ReadDataStatement(at, ScenarioParser.Var(m.Groups[1].Value), ScenarioParser.Expr(at, m.Groups[2].Value));
        if ((m = ScenarioParser.CallRegex().Match(line)).Success)
            return new CallFileStatement(at, m.Groups[1].Value);
        if (line == "%0:")
            return new ReturnStatement(at);
        throw new ScenarioFormatException($"{at}: unsupported statement '{line}'.");
    }

    /// <summary>
    /// <c>#L_a, 1:</c>: the label <c>*L_a:</c> holds an address word <c>_L_b:</c>; the table is the
    /// <c>[w, w, …]</c> list after <c>*L_b:</c> (which may span lines).
    /// </summary>
    private IReadOnlyList<Sys3Int> DataTable(ScriptRef at, string label)
    {
        string[] lines = Lines(at.File);
        int pointer = Array.FindIndex(lines, l => l.Trim() == $"*{label}:");
        if (pointer < 0 || pointer + 1 >= lines.Length || !AddressWordRegex().IsMatch(lines[pointer + 1].Trim()))
            throw new ScenarioFormatException($"{at}: no address word at *{label}:.");
        string target = AddressWordRegex().Match(lines[pointer + 1].Trim()).Groups[1].Value;
        int table = Array.FindIndex(lines, l => l.Trim() == $"*{target}:");
        if (table < 0)
            throw new ScenarioFormatException($"{at}: label *{target}: not found.");
        var text = new StringBuilder();
        for (int i = table + 1; i < lines.Length; i++)
        {
            text.Append(lines[i].Trim());
            if (text.Length > 0 && text[^1] == ']')
                break;
        }
        string list = text.ToString();
        if (!list.StartsWith('[') || !list.EndsWith(']'))
            throw new ScenarioFormatException($"{at}: no word table after *{target}:.");
        return list[1..^1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(w => (Sys3Int)ScenarioParser.Number(at, w))
            .ToArray();
    }

    private string[] Lines(string file)
    {
        if (!_files.TryGetValue(file, out var lines))
        {
            string path = Path.Combine(Directory, file);
            if (!File.Exists(path))
                throw new ScenarioFormatException($"Scenario file {path} not found.");
            lines = File.ReadAllLines(path, Encoding.UTF8);
            _files[file] = lines;
        }
        return lines;
    }

    [GeneratedRegex(@"^ZH \d+:$")]
    private static partial Regex ZhRegex();

    [GeneratedRegex(@"^MM ([^,]+), ([^,]+):$")]
    private static partial Regex CopyStringRegex();

    [GeneratedRegex(@"^#(L_[0-9a-f]+), 1:$")]
    private static partial Regex DataTableRegex();

    [GeneratedRegex(@"^_(L_[0-9a-f]+):$")]
    private static partial Regex AddressWordRegex();

    [GeneratedRegex(@"^F3, VAR(\d{4}), (.+):$")]
    private static partial Regex ReadDataRegex();
}
