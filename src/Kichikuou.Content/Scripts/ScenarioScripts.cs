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
/// <c>MS n, "text":</c>, <c>MM expr, expr:</c>, <c>#L_xxxxx, 1:</c> (resolved to the table its
/// address word names, <see cref="ScenarioParser.DataTable"/>), <c>F3, VARnnnn, expr:</c>,
/// <c>%#FILE.ADV:</c> and <c>%0:</c> (which ends the file's code); <c>ZH n:</c> and <c>R</c> are
/// message formatting and produce nothing. <see cref="ReadStatement"/> reads one line in these
/// forms, e.g. an <c>MS</c> or a <c>#label</c> of a hand-ported file.
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

    /// <inheritdoc />
    public IReadOnlyList<ScriptStatement> ReadBlock(ScriptRef at)
    {
        string[] lines = Lines(at.File);
        var block = new List<ScriptStatement>();
        for (int n = Math.Max(at.Line, 1); n <= lines.Length; n++)
        {
            var here = new ScriptRef(at.File, n);
            string line = lines[n - 1].Trim();
            if (line == "\\0:")
            {
                block.Add(new ReturnLabelStatement(here));
                return block;
            }
            if (line == "R")
            {
                block.Add(new LineBreakStatement(here));
                continue;
            }
            switch (ParseData(here, line))
            {
                case null:
                    continue;
                case var statement when statement is AssignStatement or SetStringStatement or CopyStringStatement:
                    block.Add(statement);
                    break;
                default:
                    throw new ScenarioFormatException($"{here}: only assignments, MS, MM and R are read in a block, not '{line}'.");
            }
        }
        throw new ScenarioFormatException($"{at}: the block has no return (\\0:).");
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
        if ((m = ScenarioParser.DataTableRegex().Match(line)).Success)
            return ScenarioParser.DataTable(at, m.Groups[1].Value, Lines(at.File));
        if ((m = ReadDataRegex().Match(line)).Success)
            return new ReadDataStatement(at, ScenarioParser.Var(m.Groups[1].Value), ScenarioParser.Expr(at, m.Groups[2].Value));
        if ((m = ScenarioParser.CallRegex().Match(line)).Success)
            return new CallFileStatement(at, m.Groups[1].Value);
        if (line == "%0:")
            return new ReturnStatement(at);
        throw new ScenarioFormatException($"{at}: unsupported statement '{line}'.");
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

    [GeneratedRegex(@"^F3, VAR(\d{4}), (.+):$")]
    private static partial Regex ReadDataRegex();
}
