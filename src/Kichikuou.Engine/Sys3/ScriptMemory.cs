namespace Kichikuou.Engine.Sys3;

/// <summary>
/// The scenario's memory, laid out exactly as System 3.9 / xsystem35 keeps it
/// (<c>variable.c</c>): numbered variables, array pages allocated by <c>DC</c>, and string
/// variables. Keeping the original layout lets golden tests compare the core's state with the
/// original game's variable by variable.
/// </summary>
/// <remarks>
/// Prefer the typed view, a variant's <see cref="Script.ScriptState"/>; this class is the storage beneath it.
/// </remarks>
/// <param name="variableCount">The numbered variables the scenario declares (the variant's, <see cref="Script.VariantProfile.VariableCount"/>).</param>
public sealed class ScriptMemory(int variableCount)
{
    /// <summary>Number of numbered variables the scenario declares (the original's <c>VAR0000..VAR0805</c>).</summary>
    public int VariableCount => _vars.Length;

    /// <summary>Highest array page number + 1 (xsystem35 <c>PAGE_MAX</c>).</summary>
    public const int PageLimit = 256;

    /// <summary>Default number of string variables (xsystem35 <c>STRVAR_MAX</c>).</summary>
    public const int DefaultStringCount = 5000;

    private readonly Sys3Int[] _vars = new Sys3Int[variableCount];
    private readonly Sys3Int[]?[] _pages = new Sys3Int[]?[PageLimit];

    /// <summary>String variables, 1-based as in the script (<c>MS n, "..."</c>); index 0 is unused.</summary>
    public string[] Strings { get; } = Enumerable.Repeat(string.Empty, DefaultStringCount + 1).ToArray();

    /// <summary>A numbered variable (page 0).</summary>
    public ref Sys3Int Var(int number) => ref _vars[number];

    /// <summary>
    /// Allocates (or resizes, keeping contents) an array page of <paramref name="size"/> slots.
    /// <c>DC page, maxindex</c> allocates <c>maxindex + 1</c> slots (xsystem35 <c>cmdd.c</c>).
    /// </summary>
    public void AllocatePage(int page, int size)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(page, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(page, PageLimit);
        ArgumentOutOfRangeException.ThrowIfLessThan(size, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(size, 65536);

        var current = _pages[page];
        if (current is null)
            _pages[page] = new Sys3Int[size];
        else if (current.Length != size)
            Array.Resize(ref _pages[page], size);
    }

    /// <summary>Releases an array page (the state before its <c>DC</c>).</summary>
    public void FreePage(int page) => _pages[page] = null;

    /// <summary>Size of an array page, or 0 if it is not allocated.</summary>
    public int PageSize(int page) => _pages[page]?.Length ?? 0;

    /// <summary>The numbers of the allocated array pages, ascending.</summary>
    public IEnumerable<int> AllocatedPages
    {
        get
        {
            for (int page = 1; page < PageLimit; page++)
            {
                if (_pages[page] is not null)
                    yield return page;
            }
        }
    }

    /// <summary>
    /// Reads <c>page[index]</c>. Outside the page the original engine resolves the reference to
    /// nothing and the read yields 0 (<c>v_ref_indexed</c> returns NULL, <c>getCaliValue</c>
    /// substitutes 0).
    /// </summary>
    public Sys3Int ReadPage(int page, int index)
    {
        var values = _pages[page];
        return values is not null && (uint)index < (uint)values.Length ? values[index] : Sys3Int.Zero;
    }

    /// <summary>Writes <c>page[index]</c>; outside the page the write is ignored, as in the original.</summary>
    public void WritePage(int page, int index, Sys3Int value)
    {
        var values = _pages[page];
        if (values is not null && (uint)index < (uint)values.Length)
            values[index] = value;
    }
}
