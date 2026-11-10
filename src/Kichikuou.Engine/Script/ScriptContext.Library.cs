using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>
/// Ours (owner's decision 29): the save commands on the library of auto-saves and manual saves
/// (<see cref="ISaveLibrary"/>), which a variant's save screen uses in place of the numbered slots while
/// <see cref="Library"/> is set. Each leaves the engine's status in <c>RetVal</c> as its slot command does: 0 for a
/// load, 1 for a save, 255 for an error.
/// </summary>
public partial class ScriptContext
{
    private ISaveLibrary? _library;

    /// <summary>
    /// The library, or null where the scenario's numbered slots are the saves (the original's way, the differential
    /// tests). Setting it puts <see cref="LibrarySlots"/> over it as <see cref="Saves"/>, so the scenario's own slot
    /// commands read the newest auto-save as slot 1.
    /// </summary>
    public ISaveLibrary? Library
    {
        get => _library;
        set
        {
            _library = value;
            if (value is not null)
                Saves = new LibrarySlots(value);
        }
    }

    /// <summary>The save last written or loaded through the library.</summary>
    public SaveId? LastSave { get; private set; }

    /// <summary>
    /// Ours (docs/save-import.md, §7): the save loaded last from outside the library (<see cref="LoadImported"/>), while
    /// no save has been written or loaded through the library since; null otherwise.
    /// </summary>
    public ImportedSave? LastImport { get; private set; }

    /// <summary>The weekly autosave (<c>QD 1</c>): the memory as a new auto-save, resuming at <see cref="SaveResume"/>.</summary>
    public void SaveAuto()
    {
        var at = SaveResume ?? throw new InvalidOperationException("An auto-save with no resume point: the autosave's caller sets SaveResume.");
        Saved(RequireLibrary().WriteAuto(SaveData.Capture(State, at)));
    }

    /// <summary>
    /// Ours (owner's decision 31): the auto-save as the player ends the turn, the memory resuming at
    /// <paramref name="resume"/>. No script command writes it, so <c>RetVal</c> is left alone; false when it could not
    /// be written.
    /// </summary>
    public bool SaveTurnEnd(ScriptRef resume)
    {
        var id = RequireLibrary().WriteAuto(SaveData.Capture(State, resume), AutoRing.TurnEnd);
        if (id is not null)
            (LastSave, LastImport) = (id, null);
        return id is not null;
    }

    /// <summary>
    /// A manual save over number <paramref name="over"/> or a new one: the memory resuming at <paramref name="resume"/>,
    /// or with none a copy of the newest auto-save (the original's <c>QC n, 1</c>).
    /// </summary>
    public void SaveManual(int? over, ScriptRef? resume)
    {
        var library = RequireLibrary();
        var data = resume is { } at ? SaveData.Capture(State, at)
            : library.Autos is [var newest, ..] && ReadLibrary(newest) is { } auto ? auto.Stamped(null).Commented("") : null;
        Saved(data is null ? null : library.WriteManual(data, over));
    }

    /// <summary>
    /// <c>LD</c> of a save: back into the memory, then <paramref name="restored"/> (the variant's persistent values laid
    /// over the save's older copy of them), <c>RetVal</c> 0, and the running code is left by
    /// <see cref="GameLoadedException"/>; a save that is gone only sets <c>RetVal</c> 255.
    /// </summary>
    public void LoadSave(SaveId id, Action? restored = null)
    {
        var data = ReadLibrary(id);
        if (data is null)
        {
            State.RetVal = SaveError;
            return;
        }
        data.Restore(State);
        restored?.Invoke();
        State.RetVal = 0;
        (LastSave, LastImport) = (id, null);
        throw new GameLoadedException(id.Number, data.Resume);
    }

    /// <summary>
    /// Ours (docs/save-import.md, §7): <c>LD</c> of a save from outside the library and the slots — a save of the original
    /// imported, which the library never holds: back into the memory as any load (<see cref="LoadSave"/>), then
    /// <paramref name="restored"/>, <c>RetVal</c> 0, and the running code is left by <see cref="GameLoadedException"/>
    /// (slot 0). Nothing is written. <paramref name="label"/> names it where the save screen names a loaded save.
    /// </summary>
    /// <exception cref="ArgumentException">The save is not of this memory's size.</exception>
    public void LoadImported(SaveData data, string label, Action? restored = null)
    {
        if (Fit(data) is null)
            throw new ArgumentException($"The save has {data.Variables.Length} variables, the game {State.Memory.VariableCount}.", nameof(data));
        data.Restore(State);
        restored?.Invoke();
        State.RetVal = 0;
        (LastSave, LastImport) = (null, new ImportedSave(label, data.Written));
        throw new GameLoadedException(0, data.Resume);
    }

    /// <summary><c>LT</c> of the save loaded from outside the library (<see cref="LastImport"/>): its time, as <see cref="ReadSaveTime(SaveId, int)"/>.</summary>
    public void ReadImportTime(int var) => WriteSaveTime(LastImport?.Written, var);

    /// <summary>
    /// <c>LP</c> from the persistent page: <paramref name="count"/> values from where <paramref name="var"/> points.
    /// Before the library has one, from the save written last (the original copies the page into every slot, so any
    /// of them holds it); <c>RetVal</c> 255 when there is none.
    /// </summary>
    public void LoadPersistent(int var, Sys3Int count)
    {
        var library = RequireLibrary();
        var data = Fit(library.ReadPersistent()) ?? library.Autos.Concat(library.Manuals)
            .Select(ReadLibrary).OfType<SaveData>().MaxBy(d => d.Written ?? DateTime.MinValue);
        LoadPartial(data, var, count);
    }

    /// <summary><c>QP</c> into the persistent page; the first one keeps the whole memory, of which only the persistent values are read.</summary>
    public void SavePersistent(int var, Sys3Int count)
    {
        var library = RequireLibrary();
        var data = Fit(library.ReadPersistent()) ?? SaveData.Capture(State, PersistentHolder);
        SavePartial(data, var, count, library.WritePersistent);
    }

    /// <summary><c>LT</c> of a save: when it was saved into <paramref name="var"/> and the five after it (<see cref="ReadSaveTime(Sys3Int, int)"/>).</summary>
    public void ReadSaveTime(SaveId id, int var) => WriteSaveTime(ReadLibrary(id)?.Written, var);

    /// <summary>The persistent page holder's resume point, which nothing loads.</summary>
    private static readonly ScriptRef PersistentHolder = new("persistent", 0);

    private ISaveLibrary RequireLibrary() => Library ?? throw new InvalidOperationException("No save library is in use.");

    private void Saved(SaveId? id)
    {
        State.RetVal = id is null ? SaveError : (Sys3Int)1;
        if (id is not null)
            (LastSave, LastImport) = (id, null);
    }

    /// <summary>A save of the library, or null when it is gone or not of this memory's size.</summary>
    private SaveData? ReadLibrary(SaveId id) => Fit(Library?.Read(id));

    private SaveData? Fit(SaveData? data) => data is not null && data.Variables.Length == State.Memory.VariableCount ? data : null;
}

/// <summary>
/// Ours (docs/save-import.md, §7): a save loaded from outside the library (<see cref="ScriptContext.LoadImported"/>): the
/// label the save screen names it by and when it was saved.
/// </summary>
public sealed record ImportedSave(string Label, DateTime? Written);
