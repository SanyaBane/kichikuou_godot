using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Scripts;

/// <summary>
/// Turns the lines of a raw decompiled file (<c>xsys35dc</c> output, one command per line,
/// variables as <c>VARnnnn</c>) into a <see cref="ScriptProgram"/>. Strict: every form outside the
/// list below fails with its location, so a different version or translation of the scenario
/// cannot be misread silently.
/// </summary>
/// <remarks>
/// Forms: labels <c>*L_x:</c>; <c>@L_x:</c>, <c>{expr:</c> … <c>}</c>, <c>\L_x:</c>, <c>\0:</c>,
/// <c>%#FILE.ADV:</c>, <c>%0:</c>, <c>&amp;#FILE.ADV:</c>; <c>!VARnnnn : expr!</c>; text commands
/// (<c>'…'</c>, <c>R</c>, <c>X</c>, <c>H</c>, <c>HH</c>, <c>MP</c>, with <c>ZH 1:</c> in between) grouped
/// into runs; menus <c>$L_x$</c>, <c>$</c> and <c>]</c>; <c>A</c>; the data reader <c>#L_x, 1:</c>
/// (<see cref="DataTable"/>) with <c>F1</c>/<c>F3</c>, and the data lines in the code area
/// (<c>_L_x:</c>, <c>"text"</c>); and the commands in <see cref="Command"/>. Expressions: see
/// <see cref="Expr"/>.
/// </remarks>
internal static partial class ScenarioParser
{
    /// <summary>The presentation-only engine commands (<see cref="EngineStatement"/>) and their argument counts.</summary>
    private static readonly Dictionary<string, int[]> EngineCommands = new(StringComparer.Ordinal)
    {
        ["G"] = [1, 2], ["J0"] = [2], ["J1"] = [2], ["PC"] = [1],
        ["PF0"] = [1], ["PF1"] = [1], ["PW0"] = [1], ["PW1"] = [1],
        ["EC"] = [1], ["CB"] = [5], ["CC"] = [6], ["CF"] = [5], ["CL"] = [5], ["CM"] = [9], ["CS"] = [7],
        ["B1"] = [6], ["B2"] = [6], ["B3"] = [6], ["B4"] = [6],
        ["T"] = [2], ["WV"] = [4], ["ZC"] = [2], ["ZM"] = [1], ["SL"] = [1], ["IZ"] = [2],
    };

    /// <summary>Reads a whole file as a program.</summary>
    public static ScriptProgram Program(string file, string[] lines)
    {
        var builder = new ProgramBuilder(file, lines);
        for (int i = 0; i < lines.Length; i++)
            builder.Add(new ScriptRef(file, i + 1), lines[i].Trim());
        return builder.Finish(new ScriptRef(file, lines.Length));
    }

    /// <summary>
    /// <c>#label, 1:</c> (<c>getDataTableAdr</c>): the label <c>*L_a:</c> holds an address word
    /// <c>_L_b:</c>, and the data reader points at what follows <c>*L_b:</c> — a word table
    /// <c>[w, w, …]</c> (which may span lines) or a run of <c>"text"</c> lines.
    /// </summary>
    public static ScriptStatement DataTable(ScriptRef at, string label, string[] lines)
    {
        int pointer = Array.FindIndex(lines, l => l.Trim() == $"*{label}:");
        if (pointer < 0 || pointer + 1 >= lines.Length || !AddressWordRegex().IsMatch(lines[pointer + 1].Trim()))
            throw new ScenarioFormatException($"{at}: no address word at *{label}:.");
        string target = AddressWordRegex().Match(lines[pointer + 1].Trim()).Groups[1].Value;
        int table = Array.FindIndex(lines, l => l.Trim() == $"*{target}:");
        if (table < 0)
            throw new ScenarioFormatException($"{at}: label *{target}: not found.");
        if (table + 1 < lines.Length && DataStringRegex().IsMatch(lines[table + 1].Trim()))
        {
            var strings = new List<string>();
            for (int i = table + 1; i < lines.Length && DataStringRegex().Match(lines[i].Trim()) is { Success: true } s; i++)
                strings.Add(Unescape(s.Groups[1].Value));
            return new SetStringTableStatement(at, strings);
        }
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
        return new SetDataTableStatement(at, list[1..^1].Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(w => (Sys3Int)Number(at, w))
            .ToArray());
    }

    private sealed class ProgramBuilder(string file, string[] lines)
    {
        private readonly List<ScriptStatement> _statements = [];
        private readonly Dictionary<string, int> _labels = new(StringComparer.Ordinal);
        private readonly Stack<int> _blocks = new();
        private TextBuilder? _run;

        public void Add(ScriptRef at, string line)
        {
            if (TextBuilder.IsText(at, line))
            {
                (_run ??= new TextBuilder()).Add(at, line);
                return;
            }
            FlushRun();
            Match m;
            if (line.Length == 0)
                return;
            if ((m = LabelRegex().Match(line)).Success)
            {
                if (!_labels.TryAdd(m.Groups[1].Value, _statements.Count))
                    throw new ScenarioFormatException($"{at}: label {m.Groups[1].Value} defined twice.");
            }
            else if (line == "}")
            {
                if (_blocks.Count == 0)
                    throw new ScenarioFormatException($"{at}: '}}' without an open block.");
                int open = _blocks.Pop();
                _statements[open] = ((IfStatement)_statements[open]) with { EndTarget = _statements.Count };
            }
            else if ((m = IfRegex().Match(line)).Success)
            {
                _blocks.Push(_statements.Count);
                _statements.Add(new IfStatement(at, Expr(at, m.Groups[1].Value), -1));
            }
            else if ((m = DataTableRegex().Match(line)).Success)
                _statements.Add(DataTable(at, m.Groups[1].Value, lines));
            else if (AddressWordRegex().IsMatch(line) || DataStringRegex().IsMatch(line))
                _statements.Add(new DataStatement(at));
            else
                _statements.Add(Statement(at, line));
        }

        public ScriptProgram Finish(ScriptRef end)
        {
            FlushRun();
            if (_blocks.Count > 0)
                throw new ScenarioFormatException($"{_statements[_blocks.Peek()].At}: block not closed by the end of the file ({end}).");
            for (int i = 0; i < _statements.Count; i++)
            {
                _statements[i] = _statements[i] switch
                {
                    GotoStatement s => s with { Target = Resolve(s.At, s.Label) },
                    CallLabelStatement s => s with { Target = Resolve(s.At, s.Label) },
                    MenuItemStatement s => s with { Target = Resolve(s.At, s.Label) },
                    var s => s,
                };
            }
            return new ScriptProgram(file, _statements, _labels);
        }

        private int Resolve(ScriptRef at, string label) =>
            _labels.TryGetValue(label, out int target)
                ? target
                : throw new ScenarioFormatException($"{at}: label {label} not found.");

        private void FlushRun()
        {
            if (_run is null)
                return;
            _statements.AddRange(_run.ToStatements());
            _run = null;
        }
    }

    /// <summary>The consecutive text commands of one run (see <c>ScenarioText</c>).</summary>
    private sealed class TextBuilder
    {
        private readonly List<ScriptRef> _breaks = [];
        private readonly List<ScriptExpr> _numbers = [];
        private readonly List<ScriptExpr> _strings = [];
        private ScriptRef? _start;
        private bool _hasText;

        /// <summary>True for a text command or a <c>ZH</c> switch (which belongs to the run around it).</summary>
        public static bool IsText(ScriptRef at, string line)
        {
            if (!ScenarioText.TryParse(line, []))
                return false;
            if (line.StartsWith("ZH ", StringComparison.Ordinal) && line != "ZH 1:")
                throw new ScenarioFormatException($"{at}: only 'ZH 1:' is supported (the message texts assume half-width conversion), not '{line}'.");
            return true;
        }

        public void Add(ScriptRef at, string line)
        {
            var segments = new List<TextSegment>();
            ScenarioText.TryParse(line, segments);
            if (segments.Count == 0)
                return;   // ZH 1: does not print
            _start ??= at;
            switch (segments[0])
            {
                case LineBreak:
                    _breaks.Add(at);
                    return;
                case NumberSlot:
                    _numbers.Add(Expr(at, Args(at, line, line.StartsWith("HH", StringComparison.Ordinal) ? "HH" : "H", 2)[1]));
                    break;
                case StringSlot { Chars: null }:
                    _strings.Add(Expr(at, Args(at, line, "X", 1)[0]));
                    break;
                case StringSlot:
                    _strings.Add(Expr(at, Args(at, line, "MP", 2)[0]));
                    break;
            }
            _hasText = true;
        }

        /// <summary>A message, or one line break per <c>R</c> when the run prints nothing else.</summary>
        public IEnumerable<ScriptStatement> ToStatements() =>
            _hasText
                ? [new MessageStatement(_start!.Value, new ScriptText(_start.Value, _numbers, _strings))]
                : _breaks.Select(at => new LineBreakStatement(at));
    }

    /// <summary>One statement that is not a label, block, text or menu item.</summary>
    private static ScriptStatement Statement(ScriptRef at, string line)
    {
        Match m;
        if ((m = AssignRegex().Match(line)).Success)
            return new AssignStatement(at, Var(m.Groups[1].Value), Expr(at, m.Groups[2].Value));
        if ((m = GotoRegex().Match(line)).Success)
            return new GotoStatement(at, m.Groups[1].Value, -1);
        if (line == "\\0:")
            return new ReturnLabelStatement(at);
        if ((m = CallLabelRegex().Match(line)).Success)
            return new CallLabelStatement(at, m.Groups[1].Value, -1);
        if ((m = CallRegex().Match(line)).Success)
            return new CallFileStatement(at, m.Groups[1].Value);
        if (line == "%0:")
            return new ReturnStatement(at);
        if ((m = JumpRegex().Match(line)).Success)
            return new JumpFileStatement(at, m.Groups[1].Value);
        if ((m = MenuItemRegex().Match(line)).Success)
            return new MenuItemStatement(at, m.Groups[1].Value, -1);
        if (line == "$")
            return new MenuItemEndStatement(at);
        if (line == "]")
            return new MenuStatement(at);
        if (line == "A")
            return new HitAnyKeyStatement(at);
        if ((m = SetStringRegex().Match(line)).Success)
            return new SetStringStatement(at, Number(at, m.Groups[1].Value), Unescape(m.Groups[2].Value));
        if ((m = CommandRegex().Match(line)).Success)
            return Command(at, m.Groups[1].Value, m.Groups[2].Success ? SplitArgs(m.Groups[2].Value) : []);
        throw new ScenarioFormatException($"{at}: unsupported statement '{line}'.");
    }

    /// <summary>A command <c>CMD a, b:</c> with its argument texts.</summary>
    private static ScriptStatement Command(ScriptRef at, string command, string[] args)
    {
        ScriptExpr Arg(int i) => Expr(at, args[i]);
        int VarArg(int i) => Expr(at, args[i]) is VarExpr v
            ? v.Var
            : throw new ScenarioFormatException($"{at}: {command} needs a variable, not '{args[i]}'.");
        void Count(params int[] counts)
        {
            if (!counts.Contains(args.Length))
                throw new ScenarioFormatException($"{at}: {command} with {args.Length} arguments is not supported.");
        }

        switch (command)
        {
            case "Y":
                Count(2);
                return Arg(0) switch
                {
                    ConstExpr { Value.Value: 1 } => new MessageResetStatement(at, Arg(1)),
                    ConstExpr { Value.Value: 3 } => new PauseStatement(at, Arg(1)),
                    ConstExpr { Value.Value: 4 } => new RandomStatement(at, Arg(1)),
                    _ => throw new ScenarioFormatException($"{at}: Y {args[0]} is not supported."),
                };
            case "IK":
                Count(1);
                return Arg(0) is ConstExpr { Value.Value: 0 }
                    ? new KeyWaitStatement(at)
                    : throw new ScenarioFormatException($"{at}: IK {args[0]} is not supported.");
            case "IM":
                Count(2);
                return new MouseStatement(at, VarArg(0), VarArg(1));
            case "IC":
                Count(2);
                return new CursorStatement(at, Arg(0), VarArg(1));
            case "SS":
                Count(1);
                return new MusicStatement(at, Arg(0));
            case "SR0" or "SR1":
                Count(1);
                return new MusicInfoStatement(at, command[2] - '0', VarArg(0));
            case "SC":
                Count(1);
                return new CdPositionStatement(at, VarArg(0));
            case "MM":
                Count(2);
                return new CopyStringStatement(at, Arg(0), Arg(1));
            case "MA":
                Count(2);
                return new AppendStringStatement(at, Arg(0), Arg(1));
            case "MH":
                Count(3);
                return new NumberToStringStatement(at, Arg(0), Arg(1), Arg(2));
            case "NC":
                Count(2);
                return new ClearVariablesStatement(at, VarArg(0), Arg(1));
            case "NB":
                Count(3);
                return new CopyVariablesStatement(at, VarArg(0), VarArg(1), Arg(2));
            case "US" or "UG":
                Count(2);
                return new LocalVariablesStatement(at, command == "US", VarArg(0), Arg(1));
            case "LD" or "QD":
                Count(1);
                return new SaveDataStatement(at, command, Arg(0));
            case "LP" or "QP":
                Count(3);
                return new SaveDataStatement(at, command, Arg(0), VarArg(1), Arg(2));
            case "QC":
                Count(2);
                return new SaveDataStatement(at, command, Arg(0), Other: Arg(1));
            case "LT":
                Count(2);
                return new SaveDataStatement(at, command, Arg(0), VarArg(1));
            case "ZT0":
                Count(1);
                return new DateStatement(at, VarArg(0));
            case "ZT1":
                Count(1);
                return new TimerResetStatement(at, Arg(0));
            case "ZT2" or "ZT3" or "ZT4" or "ZT5":
                Count(1);
                return new TimerStatement(at, command[2] - '0', VarArg(0));
            case "ZZ0":
                Count(1);
                return new QuitStatement(at, Arg(0));
            case "F1":
                Count(2);
                return new ReadStringStatement(at, Arg(0), Arg(1));
            case "F3":
                Count(2);
                return new ReadDataStatement(at, VarArg(0), Arg(1));
        }
        if (EngineCommands.TryGetValue(command, out var counts))
        {
            Count(counts);
            return new EngineStatement(at, command, args.Select((_, i) => Arg(i)).ToArray());
        }
        throw new ScenarioFormatException($"{at}: unsupported command {command}.");
    }

    /// <summary>The arguments of <c>CMD a, b:</c> after checking the command and their number.</summary>
    private static string[] Args(ScriptRef at, string line, string command, int count)
    {
        var m = CommandRegex().Match(line);
        if (!m.Success || m.Groups[1].Value != command || !m.Groups[2].Success)
            throw new ScenarioFormatException($"{at}: expected {command}, found '{line}'.");
        var args = SplitArgs(m.Groups[2].Value);
        return args.Length == count
            ? args
            : throw new ScenarioFormatException($"{at}: {command} with {args.Length} arguments is not supported.");
    }

    /// <summary>Splits an argument list at its top-level commas.</summary>
    private static string[] SplitArgs(string text)
    {
        var args = new List<string>();
        int depth = 0, start = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '(')
                depth++;
            else if (text[i] == ')')
                depth--;
            else if (text[i] == ',' && depth == 0)
            {
                args.Add(text[start..i].Trim());
                start = i + 1;
            }
        }
        args.Add(text[start..].Trim());
        return args.ToArray();
    }

    /// <summary>The variable number of <c>VARnnnn</c>'s digits.</summary>
    public static int Var(string digits) => int.Parse(digits, CultureInfo.InvariantCulture);

    /// <summary>A 16-bit number literal.</summary>
    public static int Number(ScriptRef at, string text) =>
        int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int n) && n <= ushort.MaxValue
            ? n
            : throw new ScenarioFormatException($"{at}: '{text}' is not a 16-bit number.");

    /// <summary>The operators by precedence level, weakest first (xsys35dc <c>print_cali</c>; all left-associative).</summary>
    private static readonly string[][] Levels =
    [
        ["=", "\\"],
        ["<", ">", "<=", ">="],
        ["&", "|", "^"],
        ["+", "-"],
        ["*", "/", "%"],
    ];

    /// <summary>
    /// Parses an expression: <c>VARnnnn</c>, numbers, parentheses and the binary operators of
    /// <see cref="Levels"/> (precedence climbing).
    /// </summary>
    public static ScriptExpr Expr(ScriptRef at, string text)
    {
        var tokens = TokenRegex().Matches(text).Select(t => t.Value).ToList();
        if (string.Concat(tokens) != text.Replace(" ", string.Empty, StringComparison.Ordinal))
            throw new ScenarioFormatException($"{at}: unsupported expression '{text}'.");
        int pos = 0;
        var expr = Level(0);
        if (pos != tokens.Count)
            throw new ScenarioFormatException($"{at}: unsupported expression '{text}'.");
        return expr;

        ScriptExpr Level(int level)
        {
            if (level == Levels.Length)
                return Atom();
            var left = Level(level + 1);
            while (pos < tokens.Count && Levels[level].Contains(tokens[pos]))
            {
                string op = tokens[pos++];
                left = new BinaryExpr(op, left, Level(level + 1));
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
                var inner = Level(0);
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
    public static string Unescape(string text)
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

    [GeneratedRegex(@"^\*(L_[0-9a-f]+):$")]
    private static partial Regex LabelRegex();

    [GeneratedRegex(@"^@(L_[0-9a-f]+):$")]
    private static partial Regex GotoRegex();

    [GeneratedRegex(@"^\\(L_[0-9a-f]+):$")]
    private static partial Regex CallLabelRegex();

    [GeneratedRegex(@"^\{(.+):$")]
    private static partial Regex IfRegex();

    [GeneratedRegex(@"^\$(L_[0-9a-f]+)\$$")]
    private static partial Regex MenuItemRegex();

    [GeneratedRegex(@"^#(L_[0-9a-f]+), 1:$")]
    public static partial Regex DataTableRegex();

    [GeneratedRegex(@"^_(L_[0-9a-f]+):$")]
    private static partial Regex AddressWordRegex();

    /// <summary>A string of a data table: <c>"text"</c> on its own line.</summary>
    [GeneratedRegex(@"^""((?:[^""\\]|\\.)*)""$")]
    private static partial Regex DataStringRegex();

    [GeneratedRegex(@"^!VAR(\d{4}) : (.+)!$")]
    public static partial Regex AssignRegex();

    [GeneratedRegex(@"^MS (\d+), ""((?:[^""\\]|\\.)*)"":$")]
    public static partial Regex SetStringRegex();

    [GeneratedRegex(@"^%#([^:]+\.ADV):$")]
    public static partial Regex CallRegex();

    [GeneratedRegex(@"^&#([^:]+\.ADV):$")]
    private static partial Regex JumpRegex();

    /// <summary><c>CMD:</c> or <c>CMD args:</c>; the decompiler writes some forms with a comma after the name (<c>SR0, VAR0001:</c>).</summary>
    [GeneratedRegex(@"^([A-Z][A-Z0-9]*)(?:,? (.+))?:$")]
    private static partial Regex CommandRegex();

    [GeneratedRegex(@"VAR\d{4}|\d+|<=|>=|[-+*/%&|^=\\<>()]")]
    private static partial Regex TokenRegex();
}
