using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.State;

/// <summary>
/// The whole game state as a typed view over <see cref="ScriptMemory"/>. Every scenario
/// variable is a property named by <c>docs/variables.tsv</c> (generated part), and the
/// array-bound tables are exposed as <see cref="RecordTable{T}"/>.
/// </summary>
/// <remarks>
/// Rule code addresses records explicitly (<c>state.Units[state.Unit_Cur].Troops</c>) rather
/// than through an implicit cursor, so cursor-dependent quirks of the script stay visible.
/// </remarks>
public sealed partial class GameState
{
    /// <summary>Creates an empty state with the array pages allocated as <c>SET.ADV</c> does.</summary>
    public GameState()
    {
        AllocatePages(Memory);
    }

    /// <summary>The underlying memory, in the original engine's layout.</summary>
    public ScriptMemory Memory { get; } = new();

    /// <summary>String variables (<c>MS</c>/<c>MM</c>), 1-based.</summary>
    public string[] Strings => Memory.Strings;

    /// <summary>
    /// Variable <paramref name="number"/> as the script reads it: through its <c>DS</c> binding
    /// (page slot <c>Offset + cursor</c>) when it has one, otherwise the plain slot. For code that
    /// runs script statements by number (data files, scenes); rule code uses the properties.
    /// </summary>
    public Sys3Int ReadVariable(int number) =>
        BindingOf(number) is { } b
            ? Memory.ReadPage(b.Page, b.Offset + Memory.Var(b.Cursor).Value)
            : Memory.Var(number);

    /// <summary>Writes variable <paramref name="number"/> as the script does (see <see cref="ReadVariable"/>).</summary>
    public void WriteVariable(int number, Sys3Int value)
    {
        if (BindingOf(number) is { } b)
            Memory.WritePage(b.Page, b.Offset + Memory.Var(b.Cursor).Value, value);
        else
            Memory.Var(number) = value;
    }

    /// <summary>
    /// True when variable <paramref name="number"/> is bound to a page through <c>DS</c>, so it is
    /// not the plain slot that commands addressing a run of variables (<c>NC</c>, <c>US</c>, …) start from.
    /// </summary>
    public static bool IsBound(int number) => BindingOf(number) is not null;

    private static readonly PageBinding?[] BindingByVar = BuildBindingIndex();

    private static PageBinding? BindingOf(int number) =>
        (uint)number < (uint)BindingByVar.Length ? BindingByVar[number] : throw new ArgumentOutOfRangeException(nameof(number));

    private static PageBinding?[] BuildBindingIndex()
    {
        var index = new PageBinding?[ScriptMemory.VariableCount];
        foreach (var b in VariableCatalog.Bindings)
            index[b.Var] = b;
        return index;
    }
}

/// <summary>A record view created for a cursor value.</summary>
public interface IRecord<out TSelf>
{
    /// <summary>Creates the view of record <paramref name="index"/>.</summary>
    static abstract TSelf Create(ScriptMemory memory, int index);
}

/// <summary>
/// A table of records stored field-by-field on an array page. Indexing past
/// <see cref="Count"/> is allowed and, as in the script, reads the next field's slots.
/// </summary>
public readonly struct RecordTable<T>(ScriptMemory memory, int count) where T : class, IRecord<T>
{
    /// <summary>Slots reserved per field (the script's <c>*_Count</c> stride).</summary>
    public int Count => count;

    /// <summary>The record selected by a cursor value.</summary>
    public T this[int index] => T.Create(memory, index);

    /// <summary>The record selected by a script value.</summary>
    public T this[Sys3Int index] => T.Create(memory, index.Value);
}
