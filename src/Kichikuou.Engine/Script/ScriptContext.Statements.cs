using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

// Runs straight-line statements read from the player's decompile (ScriptStatements.cs): the
// SET_* data files today. A call goes through CallHelperAsync: a variant's context runs the
// helper files it ports; any other file is a hook.
public partial class ScriptContext
{
    /// <summary>The word table of the last <c>#label, n:</c> (<see cref="ReadDataStatement"/> reads it).</summary>
    private IReadOnlyList<Sys3Int> _dataTable = [];

    /// <summary>The string table of the last <c>#label, n:</c> that named one, or null (<see cref="ReadStringStatement"/> reads it).</summary>
    private IReadOnlyList<string>? _stringTable;

    /// <summary>The data reader's position in <see cref="_stringTable"/> (<c>F1</c> moves it on).</summary>
    private int _stringPosition;

    /// <summary><c>#label, n:</c> naming a word table: the data reader points at its first word.</summary>
    public void SetDataTable(IReadOnlyList<Sys3Int> words)
    {
        _dataTable = words;
        _stringTable = null;
    }

    /// <summary><c>#label, n:</c> naming a run of strings: the data reader points at the first.</summary>
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
    /// Past the table's end the engine reads on into the scenario's bytes (<c>cmdf.c:85-90</c>), which
    /// the decompile does not hold: the port reads an empty string there (owner's decision 21).
    /// </summary>
    public void ReadDataString(ScriptRef at, Sys3Int slot, Sys3Int skip)
    {
        if (_stringTable is null)
            throw new ScenarioFormatException($"{at}: F1 with no string table set.");
        int index = _stringPosition + skip.Value;
        // QUIRK: STR-44 — the music room's Stop with nothing playing reads string 999 of 224.
        string text = index < _stringTable.Count ? _stringTable[index] : "";
        if (slot.Value < State.Strings.Length)
            State.Strings[slot.Value] = text;
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
                case LineBreakStatement:
                    await View.ShowAsync(new LineBreakEvent());
                    break;
                case ReturnStatement or ReturnLabelStatement:
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
    /// <c>MC a, b</c>: 1 when string variables <paramref name="a"/> and <paramref name="b"/> hold the
    /// same text, else 0 (<c>cmdm.c</c>, <c>strcmp</c>); an index outside the string table reads as empty.
    /// </summary>
    public Sys3Int CompareStrings(Sys3Int a, Sys3Int b)
    {
        var strings = State.Strings;
        string Text(Sys3Int n) => n.Value < strings.Length ? strings[n.Value] : string.Empty;
        return string.Equals(Text(a), Text(b), StringComparison.Ordinal) ? (Sys3Int)1 : Sys3Int.Zero;
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

    /// <summary><c>%#file:</c> from a statement list or a scene (<see cref="SceneRunner"/>): <see cref="CallHelperAsync"/>.</summary>
    internal Task CallAsync(string file) => CallHelperAsync(file);

    /// <summary>
    /// A call of <paramref name="file"/> with the arguments the statements left in the variables: a
    /// variant's context runs the helper files it ports and passes the others on to this; any file
    /// is a hook here (<see cref="CallFileAsync"/>).
    /// </summary>
    protected virtual Task CallHelperAsync(string file) => CallFileAsync(file);
}
