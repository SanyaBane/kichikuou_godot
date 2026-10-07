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
