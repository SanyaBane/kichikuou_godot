using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// Runs straight-line statements read from the player's decompile (ScriptStatements.cs): the
// SET_* data files today. Calls into the helper files go to their ports in this context; any
// other file is a hook.
public sealed partial class ScriptContext
{
    /// <summary>The word table of the last <c>#label, 1:</c> (<see cref="ReadDataStatement"/> reads it).</summary>
    private IReadOnlyList<Sys3Int> _dataTable = [];

    /// <summary>The string table of the last <c>#label, 1:</c> that named one, or null (<see cref="ReadStringStatement"/> reads it).</summary>
    private IReadOnlyList<string>? _stringTable;

    /// <summary>The data reader's position in <see cref="_stringTable"/> (<c>F1</c> moves it on).</summary>
    private int _stringPosition;

    /// <summary><c>#label, 1:</c> naming a word table: the data reader points at its first word.</summary>
    public void SetDataTable(IReadOnlyList<Sys3Int> words)
    {
        _dataTable = words;
        _stringTable = null;
    }

    /// <summary><c>#label, 1:</c> naming a run of strings: the data reader points at the first.</summary>
    public void SetStringTable(IReadOnlyList<string> strings)
    {
        _stringTable = strings;
        _stringPosition = 0;
        _dataTable = [];
    }

    /// <summary><c>F3, var, k:</c> — word <paramref name="k"/> of the current word table.</summary>
    public Sys3Int ReadDataWord(ScriptRef at, Sys3Int k) =>
        k.Value < _dataTable.Count
            ? _dataTable[k.Value]
            : throw new ScenarioFormatException($"{at}: word {k.Value} is outside the data table ({_dataTable.Count} words).");

    /// <summary>
    /// <c>F1, slot, skip:</c> — string variable <paramref name="slot"/> := the string
    /// <paramref name="skip"/> places after the reader's position, which moves past it (<c>commandF1</c>).
    /// </summary>
    public void ReadDataString(ScriptRef at, Sys3Int slot, Sys3Int skip)
    {
        if (_stringTable is null)
            throw new ScenarioFormatException($"{at}: F1 with no string table set.");
        int index = _stringPosition + skip.Value;
        if (index >= _stringTable.Count)
            throw new ScenarioFormatException($"{at}: string {index} is outside the string table ({_stringTable.Count} strings).");
        if (slot.Value < State.Strings.Length)
            State.Strings[slot.Value] = _stringTable[index];
        _stringPosition = index + 1;
    }

    /// <summary>Runs <paramref name="statements"/> in order, up to the first return.</summary>
    public async Task RunAsync(IEnumerable<ScriptStatement> statements)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case AssignStatement s:
                    State.WriteVariable(s.Var, Evaluate(s.Value));
                    break;
                case SetStringStatement s:
                    SetString(s.Slot, s.Text);
                    break;
                case CopyStringStatement s:
                    CopyString(Evaluate(s.Target), Evaluate(s.Source));
                    break;
                case SetDataTableStatement s:
                    SetDataTable(s.Words);
                    break;
                case ReadDataStatement s:
                    State.WriteVariable(s.Var, ReadDataWord(s.At, Evaluate(s.Index)));
                    break;
                case CallFileStatement s:
                    await CallAsync(s.File);
                    break;
                case ReturnStatement:
                    return;
                default:
                    throw new ScenarioFormatException($"{statement.At}: {statement.GetType().Name} is not supported here.");
            }
        }
    }

    /// <summary>The value of <paramref name="expr"/> in the current state.</summary>
    public Sys3Int Evaluate(ScriptExpr expr) => expr switch
    {
        ConstExpr c => c.Value,
        VarExpr v => State.ReadVariable(v.Var),
        BinaryExpr b => Apply(b.Op, Evaluate(b.Left), Evaluate(b.Right)),
        _ => throw new ScenarioFormatException($"Expression {expr} is not supported."),
    };

    /// <summary>One operator of the engine's expressions (xsystem35 <c>cali.c</c>): comparisons yield 1 or 0.</summary>
    private static Sys3Int Apply(string op, Sys3Int a, Sys3Int b) => op switch
    {
        "+" => a + b,
        "-" => a - b,
        "*" => a * b,
        "/" => a / b,
        "%" => a % b,
        "&" => a & b,
        "|" => a | b,
        "^" => a ^ b,
        "<" => Bool(a < b),
        ">" => Bool(a > b),
        "<=" => Bool(a <= b),
        ">=" => Bool(a >= b),
        "=" => Bool(a == b),
        "\\" => Bool(a != b),
        _ => throw new ScenarioFormatException($"Operator {op} is not supported."),
    };

    private static Sys3Int Bool(bool value) => value ? (Sys3Int)1 : Sys3Int.Zero;

    /// <summary><c>MS n, "text"</c>: sets string variable <paramref name="slot"/>.</summary>
    public void SetString(int slot, string text) => State.Strings[slot] = text;

    /// <summary>
    /// <c>MM dst, src</c>: copies string variable <paramref name="source"/> into
    /// <paramref name="target"/>. Indices outside the engine's string table are ignored.
    /// </summary>
    public void CopyString(Sys3Int target, Sys3Int source)
    {
        var strings = State.Strings;
        if (target.Value < strings.Length && source.Value < strings.Length)
            strings[target.Value] = strings[source.Value];
    }

    /// <summary>
    /// <c>MA dst, src</c>: appends string variable <paramref name="source"/> to
    /// <paramref name="target"/> (<c>cmdm.c</c>, <c>svar_append</c>). A target outside the string
    /// table is ignored; a source outside it reads as empty.
    /// </summary>
    public void AppendString(Sys3Int target, Sys3Int source)
    {
        var strings = State.Strings;
        if (target.Value < strings.Length)
            strings[target.Value] += source.Value < strings.Length ? strings[source.Value] : string.Empty;
    }

    /// <summary>
    /// <c>MH slot, width, value</c>: string variable <paramref name="slot"/> := the value in
    /// full-width digits (<c>cmdm.c</c>, <c>format_number_zenkaku</c>).
    /// </summary>
    public void NumberToString(Sys3Int slot, Sys3Int width, Sys3Int value)
    {
        if (slot.Value < State.Strings.Length)
            State.Strings[slot.Value] = Sys3Text.FormatNumber(value.Value, width.Value, fullWidth: true);
    }

    /// <summary>
    /// <c>%#file:</c> from a statement list: the helper files run their ports with the arguments
    /// the statements left in <c>CallArg</c> / <c>Ui_WaoOp</c>; any other file is a hook.
    /// </summary>
    internal Task CallAsync(string file) => file switch
    {
        "M.ADV" => MapCallAsync(State.CallArg),
        "WAO_MAP.ADV" => WaoMapCallAsync(State.CallArg),
        "WAO_SUB.ADV" => WaoAsync(State.Ui_WaoOp),
        "PCMPLAY.ADV" => SoundAsync(State.CallArg),
        _ => CallFileAsync(file),
    };
}
