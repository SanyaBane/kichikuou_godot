using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.State;

/// <summary>
/// The whole game state as a typed view over <see cref="ScriptMemory"/>: the original's state
/// (<see cref="OriginalVariant"/>). Every scenario variable is a property named by
/// <c>docs/variables.tsv</c> (generated part), and the array-bound tables are exposed as
/// <see cref="RecordTable{T}"/>.
/// </summary>
/// <remarks>
/// Rule code addresses records explicitly (<c>state.Units[state.Unit_Cur].Troops</c>) rather
/// than through an implicit cursor, so cursor-dependent quirks of the script stay visible.
/// </remarks>
public sealed partial class GameState : ScriptState
{
    /// <summary>Creates an empty state with the array pages allocated as <c>SET.ADV</c> does.</summary>
    public GameState()
        : base(OriginalVariant.Profile)
    {
        AllocatePages(Memory);
    }

    /// <inheritdoc />
    protected override VariableBinding? BindingOf(int number) =>
        (uint)number < (uint)BindingByVar.Length ? BindingByVar[number] : throw new ArgumentOutOfRangeException(nameof(number));

    private static readonly VariableBinding?[] BindingByVar = BuildBindingIndex();

    private static VariableBinding?[] BuildBindingIndex()
    {
        var index = new VariableBinding?[VariableCatalog.Variables.Count];
        foreach (var b in VariableCatalog.Bindings)
            index[b.Var] = new VariableBinding(b.Page, b.Offset, b.Cursor);
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
