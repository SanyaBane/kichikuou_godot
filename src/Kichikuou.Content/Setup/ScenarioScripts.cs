using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Setup;

/// <summary>
/// Reads scenario code from the player's raw decompile (<c>xsys35dc</c> output, variables as
/// <c>VARnnnn</c>) into <see cref="ScriptStatement"/>s, so data files such as <c>SET_CHR.ADV</c>
/// run from the player's own copy and nothing derived from the game is committed. Only the forms
/// the data files use are accepted; anything else fails loudly with its location, so a
/// different version or translation of the scenario cannot be misread silently.
/// </summary>
/// <remarks>
/// Accepted: <c>!VARnnnn : expr!</c>, <c>MS n, "text":</c>, <c>MM expr, expr:</c>,
/// <c>#L_xxxxx, 1:</c> (resolved to the word table its address word names), <c>F3, VARnnnn,
/// expr:</c>, <c>%#FILE.ADV:</c> and <c>%0:</c> (which ends the file's code); <c>ZH n:</c> and
/// <c>R</c> are message formatting and produce nothing. Expressions: <c>VARnnnn</c>, numbers,
/// <c>+ - * /</c> and parentheses, <c>*</c> and <c>/</c> binding tighter.
/// </remarks>
public sealed partial class ScenarioScripts(string directory) : IScenarioSource
{
    private readonly Dictionary<string, string[]> _files = new(StringComparer.Ordinal);

    /// <summary>The folder with the raw decompiled <c>.ADV</c> files.</summary>
    public string Directory { get; } = directory;

    /// <inheritdoc />
    public IReadOnlyList<ScriptStatement> ReadFile(string file)
    {
        string[] lines = Lines(file);
        var statements = new List<ScriptStatement>();
        for (int i = 0; i < lines.Length; i++)
        {
            var at = new ScriptRef(file, i + 1);
            var statement = Parse(at, lines[i].Trim());
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
        return Parse(at, lines[at.Line - 1].Trim())
            ?? throw new ScenarioFormatException($"{at}: no statement at this line.");
    }

    private ScriptStatement? Parse(ScriptRef at, string line)
    {
        if (line.Length == 0 || line == "R" || ZhRegex().IsMatch(line))
            return null;
        Match m;
        if ((m = AssignRegex().Match(line)).Success)
            return new AssignStatement(at, Var(m.Groups[1].Value), Expr(at, m.Groups[2].Value));
        if ((m = SetStringRegex().Match(line)).Success)
            return new SetStringStatement(at, Number(at, m.Groups[1].Value), Unescape(m.Groups[2].Value));
        if ((m = CopyStringRegex().Match(line)).Success)
            return new CopyStringStatement(at, Expr(at, m.Groups[1].Value), Expr(at, m.Groups[2].Value));
        if ((m = DataTableRegex().Match(line)).Success)
            return new SetDataTableStatement(at, DataTable(at, m.Groups[1].Value));
        if ((m = ReadDataRegex().Match(line)).Success)
            return new ReadDataStatement(at, Var(m.Groups[1].Value), Expr(at, m.Groups[2].Value));
        if ((m = CallRegex().Match(line)).Success)
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
            .Select(w => (Sys3Int)Number(at, w))
            .ToArray();
    }

    private static int Var(string digits) => int.Parse(digits, CultureInfo.InvariantCulture);

    private static int Number(ScriptRef at, string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n <= ushort.MaxValue
            ? n
            : throw new ScenarioFormatException($"{at}: '{text}' is not a 16-bit number.");

    /// <summary>Parses an expression (precedence climbing over <c>+ -</c> and <c>* /</c>).</summary>
    private static ScriptExpr Expr(ScriptRef at, string text)
    {
        var tokens = TokenRegex().Matches(text).Select(t => t.Value).ToList();
        if (string.Concat(tokens) != text.Replace(" ", string.Empty, StringComparison.Ordinal))
            throw new ScenarioFormatException($"{at}: unsupported expression '{text}'.");
        int pos = 0;
        var expr = Sum();
        if (pos != tokens.Count)
            throw new ScenarioFormatException($"{at}: unsupported expression '{text}'.");
        return expr;

        ScriptExpr Sum()
        {
            var left = Product();
            while (pos < tokens.Count && tokens[pos] is "+" or "-")
            {
                char op = tokens[pos++][0];
                left = new BinaryExpr(op, left, Product());
            }
            return left;
        }

        ScriptExpr Product()
        {
            var left = Atom();
            while (pos < tokens.Count && tokens[pos] is "*" or "/")
            {
                char op = tokens[pos++][0];
                left = new BinaryExpr(op, left, Atom());
            }
            return left;
        }

        ScriptExpr Atom()
        {
            if (pos >= tokens.Count)
                throw new ScenarioFormatException($"{at}: incomplete expression '{text}'.");
            string t = tokens[pos++];
            if (t == "(")
            {
                var inner = Sum();
                if (pos >= tokens.Count || tokens[pos++] != ")")
                    throw new ScenarioFormatException($"{at}: unbalanced parentheses in '{text}'.");
                return inner;
            }
            if (t.StartsWith("VAR", StringComparison.Ordinal))
                return new VarExpr(Var(t[3..]));
            if (char.IsAsciiDigit(t[0]))
                return new ConstExpr((Sys3Int)Number(at, t));
            throw new ScenarioFormatException($"{at}: unexpected '{t}' in '{text}'.");
        }
    }

    /// <summary><c>xsys35dc</c> escapes <c>"</c> and <c>\</c> with a backslash inside strings.</summary>
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

    [GeneratedRegex(@"^!VAR(\d{4}) : (.+)!$")]
    private static partial Regex AssignRegex();

    [GeneratedRegex(@"^MS (\d+), ""((?:[^""\\]|\\.)*)"":$")]
    private static partial Regex SetStringRegex();

    [GeneratedRegex(@"^MM ([^,]+), ([^,]+):$")]
    private static partial Regex CopyStringRegex();

    [GeneratedRegex(@"^#(L_[0-9a-f]+), 1:$")]
    private static partial Regex DataTableRegex();

    [GeneratedRegex(@"^_(L_[0-9a-f]+):$")]
    private static partial Regex AddressWordRegex();

    [GeneratedRegex(@"^F3, VAR(\d{4}), (.+):$")]
    private static partial Regex ReadDataRegex();

    [GeneratedRegex(@"^%#([^:]+\.ADV):$")]
    private static partial Regex CallRegex();

    [GeneratedRegex(@"VAR\d{4}|\d+|[-+*/()]")]
    private static partial Regex TokenRegex();
}
