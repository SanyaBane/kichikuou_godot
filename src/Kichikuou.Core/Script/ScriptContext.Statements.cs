using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Script;

// Runs straight-line statements read from the player's decompile (ScriptStatements.cs): the
// SET_* data files today. Calls into the helper files go to their ports in this context; any
// other file is a hook.
public sealed partial class ScriptContext
{
    /// <summary>The word table of the last <c>#label, 1:</c> (<see cref="ReadDataStatement"/> reads it).</summary>
    private IReadOnlyList<Sys3Int> _dataTable = [];

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
                    _dataTable = s.Words;
                    break;
                case ReadDataStatement s:
                    {
                        int k = Evaluate(s.Index).Value;
                        if (k >= _dataTable.Count)
                            throw new ScenarioFormatException($"{s.At}: word {k} is outside the data table ({_dataTable.Count} words).");
                        State.WriteVariable(s.Var, _dataTable[k]);
                        break;
                    }
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
    /// <c>%#file:</c> from a statement list: the helper files run their ports with the arguments
    /// the statements left in <c>CallArg</c> / <c>Ui_WaoOp</c>; any other file is a hook.
    /// </summary>
    private Task CallAsync(string file) => file switch
    {
        "M.ADV" => MapCallAsync(State.CallArg),
        "WAO_MAP.ADV" => WaoMapCallAsync(State.CallArg),
        "WAO_SUB.ADV" => WaoAsync(State.Ui_WaoOp),
        "PCMPLAY.ADV" => SoundAsync(State.CallArg),
        _ => CallFileAsync(file),
    };
}
