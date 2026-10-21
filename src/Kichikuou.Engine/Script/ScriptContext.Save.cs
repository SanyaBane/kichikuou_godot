using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>
/// The engine's save-data commands (xsystem35 <c>cmdq.c</c>, <c>cmdl.c</c>, <c>savedata.c</c>):
/// <c>QD</c>, <c>LD</c>, <c>LP</c>, <c>QP</c>, <c>QC</c>, <c>LT</c> on the context's
/// <see cref="Saves"/>. Each leaves the engine's status in <c>RetVal</c>: 0 for a load, 1 for a
/// save, 255 for an error (an empty slot, a slot number out of 1..26, a page without the save flag).
/// A slot whose variable count is not the memory's reads as empty. Only <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c> uses
/// them; its port and <see cref="SceneRunner"/> share these.
/// </summary>
public partial class ScriptContext
{
    private const ushort SaveError = 255;

    /// <summary>The save slots; in memory unless the game gives a store on disk.</summary>
    public ISaveStore Saves { get; set; } = new MemorySaveStore();

    /// <summary>
    /// The call a <c>QD</c> now would return to after a load: set by the caller of the autosave
    /// (<c>MAIN.ADV:20</c>, <c>:83</c>) for the length of the call. The original saves the
    /// program counter and the call stack instead.
    /// </summary>
    public ScriptRef? SaveResume { get; set; }

    /// <summary>
    /// Ours: where a manual save of the moment resumes after a load — a variant's own place (the original's
    /// <c>COMMAND</c> call under way, owner's decision 26), set by its caller for the length of the call; null
    /// where a manual save is the script's own (the original's: a copy of the autosave).
    /// </summary>
    public ScriptRef? SaveHere { get; set; }

    /// <summary>
    /// <c>QD n:</c> — the memory into slot n (<c>RetVal</c> 1, or 255), resuming after a load at
    /// <paramref name="resume"/>, else at <see cref="SaveResume"/>.
    /// </summary>
    public void SaveAll(Sys3Int slot, ScriptRef? resume = null)
    {
        if (!IsSlot(slot))
        {
            State.RetVal = SaveError;
            return;
        }
        var at = resume ?? SaveResume ?? throw new InvalidOperationException("QD with no resume point: the autosave's caller sets SaveResume.");
        State.RetVal = Saves.Write(slot.Value, SaveData.Capture(State, at)) ? (Sys3Int)1 : SaveError;
    }

    /// <summary>
    /// <c>LD n:</c> — slot n back into the memory, <c>RetVal</c> 0, and the running code is left
    /// by <see cref="GameLoadedException"/>; an empty slot only sets <c>RetVal</c> 255 and the
    /// script goes on.
    /// </summary>
    public void LoadAll(Sys3Int slot)
    {
        var data = ReadSave(slot);
        if (data is null)
        {
            State.RetVal = SaveError;
            return;
        }
        data.Restore(State);
        State.RetVal = 0;
        throw new GameLoadedException(slot.Value, data.Resume);
    }

    /// <summary>
    /// <c>LP n, var, count:</c> — <paramref name="count"/> values of slot n, from the place
    /// <paramref name="var"/> points to now, into the memory there (<c>save_loadPartial</c>);
    /// <c>RetVal</c> 0, or 255 for an empty slot or a page the slot lacks.
    /// </summary>
    public void LoadPartial(Sys3Int slot, int var, Sys3Int count) => LoadPartial(ReadSave(slot), var, count);

    private void LoadPartial(SaveData? data, int var, Sys3Int count)
    {
        var (page, index) = State.Locate(var);
        var saved = data?.Page(page);
        if (saved is null)
        {
            State.RetVal = SaveError;
            return;
        }
        int n = SliceCount(page, index, count);
        for (int i = 0; i < n; i++)
            WriteSlot(page, index + i, index + i < saved.Length ? saved[index + i] : Sys3Int.Zero);
        State.RetVal = 0;
    }

    /// <summary>
    /// <c>QP n, var, count:</c> — the values from where <paramref name="var"/> points into slot n,
    /// the rest of the slot kept (<c>save_savePartial</c>); <c>RetVal</c> 1, or 255 for an empty
    /// slot or a page without the save flag. Ours: the slot keeps the time it was saved (ENG-07).
    /// </summary>
    public void SavePartial(Sys3Int slot, int var, Sys3Int count) =>
        SavePartial(ReadSave(slot), var, count, data => Saves.Write(slot.Value, data));

    private void SavePartial(SaveData? data, int var, Sys3Int count, Func<SaveData, bool> write)
    {
        var (page, index) = State.Locate(var);
        var saved = page == 0 || State.Profile.SavedPages.Contains(page) ? data?.Page(page) : null;
        if (data is null || saved is null)
        {
            State.RetVal = SaveError;
            return;
        }
        int n = SliceCount(page, index, count);
        for (int i = 0; i < n && index + i < saved.Length; i++)
            saved[index + i] = ReadSlot(page, index + i);
        State.RetVal = write(data) ? (Sys3Int)1 : SaveError;
    }

    /// <summary>
    /// <c>QC to, from:</c> — slot <paramref name="from"/> copied whole into <paramref name="to"/> (<c>RetVal</c> 1, or
    /// 255), stamped with the time now. Ours: a slot whose resume point <paramref name="skip"/> names is not copied
    /// (<c>RetVal</c> 1 all the same).
    /// </summary>
    public void CopySave(Sys3Int to, Sys3Int from, Func<ScriptRef, bool>? skip = null)
    {
        var data = IsSlot(to) ? ReadSave(from) : null;
        if (data is not null && skip?.Invoke(data.Resume) == true)
        {
            State.RetVal = 1;
            return;
        }
        State.RetVal = data is not null && Saves.Write(to.Value, data.Stamped(null)) ? (Sys3Int)1 : SaveError;
    }

    /// <summary>
    /// <c>LT n, var:</c> — when slot n was saved (<see cref="ISaveStore.Timestamp"/>; ENG-07), as year, month, day,
    /// hour, minute, second into <paramref name="var"/> and the five variables after it; all 0 and <c>RetVal</c> 255
    /// when the slot is empty.
    /// </summary>
    public void ReadSaveTime(Sys3Int slot, int var) => WriteSaveTime(IsSlot(slot) ? Saves.Timestamp(slot.Value) : null, var);

    private void WriteSaveTime(DateTime? time, int var)
    {
        int[] values = time is { } t ? [t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second] : [0, 0, 0, 0, 0, 0];
        for (int i = 0; i < values.Length; i++)
            State.WriteRun(var, i, (Sys3Int)values[i]);
        State.RetVal = time is null ? SaveError : Sys3Int.Zero;
    }

    private static bool IsSlot(Sys3Int slot) => slot.Value >= 1 && slot.Value <= ISaveStore.SlotCount;

    /// <summary>Slot <paramref name="slot"/>'s contents, or null when it is empty, out of range or not of this memory's size.</summary>
    private SaveData? ReadSave(Sys3Int slot) =>
        IsSlot(slot) && Saves.Read(slot.Value) is { } data && data.Variables.Length == State.Memory.VariableCount ? data : null;

    /// <summary><c>min(count, v_sliceSize)</c>: no further than the end of the page.</summary>
    private int SliceCount(int page, int index, Sys3Int count)
    {
        int size = page == 0 ? State.Memory.VariableCount : State.Memory.PageSize(page);
        return Math.Max(0, Math.Min(count.Value, size - index));
    }

    private Sys3Int ReadSlot(int page, int index) =>
        page == 0 ? State.Memory.Var(index) : State.Memory.ReadPage(page, index);

    private void WriteSlot(int page, int index, Sys3Int value)
    {
        if (page == 0)
            State.Memory.Var(index) = value;
        else
            State.Memory.WritePage(page, index, value);
    }
}
