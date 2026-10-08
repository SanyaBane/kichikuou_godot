namespace Kichikuou.Core.State;

/// <summary>A scenario variable as named by <c>docs/variables.tsv</c>.</summary>
/// <param name="Name">The dictionary name (<c>VARnnnn</c> when unnamed).</param>
/// <param name="Group">The dictionary group (<c>battle</c>, <c>ui</c>, <c>scratch</c>, …).</param>
public readonly record struct VariableInfo(string Name, string Group);

/// <summary>
/// A variable bound onto an array page with <c>DS</c>: record <c>k</c> of the field is page slot
/// <c>Offset + k</c>, selected by the cursor variable.
/// </summary>
/// <param name="Var">The bound variable.</param>
/// <param name="Page">The array page.</param>
/// <param name="Offset">Slot of record 0.</param>
/// <param name="Count">Slots reserved for the field (the table's stride).</param>
/// <param name="Cursor">The cursor variable that selects the record.</param>
/// <param name="Table">The <see cref="GameState"/> table property.</param>
/// <param name="Field">The record field name.</param>
public readonly record struct PageBinding(int Var, int Page, int Offset, int Count, int Cursor, string Table, string Field);

/// <summary>
/// Names, groups and page layout of the scenario's memory (generated part:
/// <c>VariableCatalog.g.cs</c>), used to describe memory locations in reports.
/// </summary>
public static partial class VariableCatalog
{
    /// <summary>Every variable, indexed by its number (0..805).</summary>
    public static IReadOnlyList<VariableInfo> Variables => VariableTable;

    /// <summary>Every <c>DS</c> binding the core assumes (<c>SET.ADV</c>), aliases included.</summary>
    public static IReadOnlyList<PageBinding> Bindings => BindingTable;

    /// <summary>The array pages <c>SET.ADV</c> allocates, with their sizes.</summary>
    public static IReadOnlyList<(int Page, int Size)> Pages => PageTable;

    /// <summary>
    /// The binding whose field covers a page slot (the primary one when an alias covers it too),
    /// or <c>null</c> when no field does.
    /// </summary>
    public static PageBinding? FindSlot(int page, int index)
    {
        foreach (var b in BindingTable)
        {
            if (b.Page == page && index >= b.Offset && index < b.Offset + b.Count)
                return b;
        }
        return null;
    }

    /// <summary>A readable name for a page slot, e.g. <c>Units[3].Troops</c>, or <c>page1[17]</c>.</summary>
    public static string DescribeSlot(int page, int index) =>
        FindSlot(page, index) is { } b ? $"{b.Table}[{index - b.Offset}].{b.Field}" : $"page{page}[{index}]";

    /// <summary>A readable name for a variable, e.g. <c>VAR0074 Unit_Troops</c>.</summary>
    public static string DescribeVar(int number) =>
        number < VariableTable.Length ? $"VAR{number:D4} {VariableTable[number].Name}" : $"VAR{number:D4}";
}
