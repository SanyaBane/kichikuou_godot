using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>
/// A variant's game state as the engine sees it: the scenario's memory, sized by the variant
/// (<see cref="Profile"/>), the engine's result register and the variables as the script reads them
/// through their <c>DS</c> bindings, which the variant's state type knows. That type (the original's
/// <c>GameState</c>, generated from its dictionary) adds the named variables and the record tables.
/// </summary>
public abstract class ScriptState
{
    /// <summary>Creates the memory of a variant with <paramref name="profile"/>; no array page is allocated.</summary>
    protected ScriptState(VariantProfile profile)
    {
        Profile = profile;
        Memory = new ScriptMemory(profile.VariableCount);
    }

    /// <summary>The facts of the variant this state belongs to.</summary>
    public VariantProfile Profile { get; }

    /// <summary>The underlying memory, in the original engine's layout.</summary>
    public ScriptMemory Memory { get; }

    /// <summary>String variables (<c>MS</c>/<c>MM</c>), 1-based.</summary>
    public string[] Strings => Memory.Strings;

    /// <summary>
    /// The port's own data, by key, kept in the save slots next to the memory: what our interface
    /// remembers and the original does not (the provinces whose figures the player has seen). No rule
    /// reads it; a load replaces it whole (<see cref="SaveData.Restore(ScriptState)"/>).
    /// </summary>
    public Dictionary<string, int[]> Extras { get; } = [];

    /// <summary>
    /// <c>VAR0000</c>, the engine's result register: <c>Y 4, n</c> draws into it, <c>IM</c> leaves the
    /// mouse button there, the save commands their status; scenarios also use it for menu and
    /// sub-file return codes and as a temporary.
    /// </summary>
    public ref Sys3Int RetVal => ref Memory.Var(0);

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
    /// The slot <paramref name="offset"/> places after variable <paramref name="number"/> as the
    /// commands that walk a run of variables from a pointer see it (<c>NC</c>, <c>NB</c>; xsystem35
    /// <c>getCaliVariable</c> resolves the variable once, through its binding at the cursor, and
    /// the command steps on from there): the plain slots after a plain variable, the page slots
    /// after a bound one. A page slot past the page's end is read as 0 and not written (the
    /// original steps out of the page's buffer).
    /// </summary>
    public Sys3Int ReadRun(int number, int offset) =>
        BindingOf(number) is { } b
            ? Memory.ReadPage(b.Page, b.Offset + Memory.Var(b.Cursor).Value + offset)
            : Memory.Var(number + offset);

    /// <summary>Writes the slot <see cref="ReadRun"/> reads.</summary>
    public void WriteRun(int number, int offset, Sys3Int value)
    {
        if (BindingOf(number) is { } b)
            Memory.WritePage(b.Page, b.Offset + Memory.Var(b.Cursor).Value + offset, value);
        else
            Memory.Var(number + offset) = value;
    }

    /// <summary>
    /// Where a reference to variable <paramref name="number"/> points as the commands that take a
    /// variable reference resolve it (<c>getCaliArray</c>, e.g. <c>LP</c>/<c>QP</c>): the page slot at
    /// its binding's offset plus the cursor now, or the plain slot on page 0.
    /// </summary>
    public (int Page, int Index) Locate(int number) =>
        BindingOf(number) is { } b ? (b.Page, b.Offset + Memory.Var(b.Cursor).Value) : (0, number);

    /// <summary>
    /// True when variable <paramref name="number"/> is bound to a page through <c>DS</c>, so it is
    /// not the plain slot that commands addressing a run of variables (<c>NC</c>, <c>US</c>, …) start from.
    /// </summary>
    public bool IsBound(int number) => BindingOf(number) is not null;

    /// <summary>The <c>DS</c> binding of variable <paramref name="number"/>, or null for a plain slot.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="number"/> is not a variable of the scenario.</exception>
    protected abstract VariableBinding? BindingOf(int number);
}

/// <summary>
/// A variable bound onto an array page with <c>DS</c>: the script reads page slot
/// <paramref name="Offset"/> + the value of <paramref name="Cursor"/>.
/// </summary>
/// <param name="Page">The array page.</param>
/// <param name="Offset">Slot of record 0.</param>
/// <param name="Cursor">The cursor variable that selects the record.</param>
public readonly record struct VariableBinding(int Page, int Offset, int Cursor);
