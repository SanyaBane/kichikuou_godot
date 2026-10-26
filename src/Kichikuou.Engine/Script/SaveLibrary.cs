namespace Kichikuou.Engine.Script;

/// <summary>Which of the library's two lists a save is in (owner's decision 29).</summary>
public enum SaveKind
{
    /// <summary>The game's own, written at each turn's start and its end; the newest <see cref="ISaveLibrary.AutoCount"/> of each <see cref="AutoRing"/> are kept.</summary>
    Auto,

    /// <summary>The player's, as many as the player keeps.</summary>
    Manual,
}

/// <summary>When an auto-save was written (owner's decision 31): the library keeps the newest <see cref="ISaveLibrary.AutoCount"/> of each.</summary>
public enum AutoRing
{
    /// <summary>At a turn's start, the original's autosave.</summary>
    TurnStart,

    /// <summary>As the player ended the turn (ours).</summary>
    TurnEnd,
}

/// <summary>A save of the library: its list and its number there (auto-saves numbered as written, manual saves as made).</summary>
public readonly record struct SaveId(SaveKind Kind, int Number)
{
    /// <inheritdoc />
    public override string ToString() => $"{Kind} {Number}";
}

/// <summary>
/// Ours (owner's decision 29): the saves as two lists in place of the engine's numbered slots — the last
/// <see cref="AutoCount"/> auto-saves of each <see cref="AutoRing"/> (decision 31) and any number of manual saves, each
/// deletable, a save with the player's comment (<see cref="SaveData.Comment"/>) — plus the persistent page, which the
/// original copies into every slot (<c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>'s <c>L_00ad6</c>, STR-31) and the library keeps once.
/// </summary>
public interface ISaveLibrary
{
    /// <summary>The auto-saves kept of each ring: an eleventh pushes that ring's oldest out.</summary>
    const int AutoCount = 10;

    /// <summary>The auto-saves of both rings, numbered together as written, newest first.</summary>
    IReadOnlyList<SaveId> Autos { get; }

    /// <summary>The manual saves by number, the first made first.</summary>
    IReadOnlyList<SaveId> Manuals { get; }

    /// <summary>A save's contents with its stamp, or null when there is none or it cannot be read.</summary>
    SaveData? Read(SaveId id);

    /// <summary>The ring of auto-save <paramref name="id"/>; <see cref="AutoRing.TurnStart"/> for any other save.</summary>
    AutoRing RingOf(SaveId id);

    /// <summary>
    /// A new auto-save in <paramref name="ring"/> (stamped with the time now when the data brings none), numbered after
    /// every auto-save, that ring's oldest past <see cref="AutoCount"/> deleted; null when it cannot be written.
    /// </summary>
    SaveId? WriteAuto(SaveData data, AutoRing ring = AutoRing.TurnStart);

    /// <summary>
    /// A manual save: over number <paramref name="over"/>, or a new one numbered after the highest; stamped with the
    /// time now when the data brings none, the old comment kept over a save that has one. Null when it cannot be written.
    /// </summary>
    SaveId? WriteManual(SaveData data, int? over = null);

    /// <summary>Deletes a save; false when there is none.</summary>
    bool Delete(SaveId id);

    /// <summary>The save's comment replaced, its time kept; false when there is none or it cannot be written.</summary>
    bool SetComment(SaveId id, string comment);

    /// <summary>The persistent page's holder (a whole memory, of which only the persistent values are read), or null before the first.</summary>
    SaveData? ReadPersistent();

    /// <summary>Writes the persistent page's holder; false when it cannot be written.</summary>
    bool WritePersistent(SaveData data);
}

/// <summary>The library's lists and the persistent page held in memory, in the file format (tests, a game without disk saves).</summary>
public sealed class MemorySaveLibrary(Func<DateTime>? clock = null) : ISaveLibrary
{
    private readonly Dictionary<SaveId, byte[]> _saves = [];
    private readonly HashSet<SaveId> _turnEnds = [];
    private readonly Func<DateTime> _clock = clock ?? (() => DateTime.Now);
    private byte[]? _persistent;
    private int _lastAuto;

    /// <inheritdoc />
    public IReadOnlyList<SaveId> Autos => [.. _saves.Keys.Where(id => id.Kind == SaveKind.Auto).OrderByDescending(id => id.Number)];

    /// <inheritdoc />
    public IReadOnlyList<SaveId> Manuals => [.. _saves.Keys.Where(id => id.Kind == SaveKind.Manual).OrderBy(id => id.Number)];

    /// <inheritdoc />
    public SaveData? Read(SaveId id) => _saves.TryGetValue(id, out var bytes) ? FromBytes(bytes) : null;

    /// <inheritdoc />
    public AutoRing RingOf(SaveId id) => _turnEnds.Contains(id) ? AutoRing.TurnEnd : AutoRing.TurnStart;

    /// <inheritdoc />
    public SaveId? WriteAuto(SaveData data, AutoRing ring = AutoRing.TurnStart)
    {
        var id = new SaveId(SaveKind.Auto, ++_lastAuto);
        _saves[id] = ToBytes(data.Stamped(data.Written ?? _clock()));
        if (ring == AutoRing.TurnEnd)
            _turnEnds.Add(id);
        foreach (var old in Autos.Where(a => RingOf(a) == ring).Skip(ISaveLibrary.AutoCount))
            Delete(old);
        return id;
    }

    /// <inheritdoc />
    public SaveId? WriteManual(SaveData data, int? over = null)
    {
        var id = new SaveId(SaveKind.Manual, over ?? (Manuals.Count == 0 ? 1 : Manuals[^1].Number + 1));
        string comment = Read(id)?.Comment ?? data.Comment;
        _saves[id] = ToBytes(data.Stamped(data.Written ?? _clock()).Commented(comment));
        return id;
    }

    /// <inheritdoc />
    public bool Delete(SaveId id)
    {
        _turnEnds.Remove(id);
        return _saves.Remove(id);
    }

    /// <inheritdoc />
    public bool SetComment(SaveId id, string comment)
    {
        if (Read(id) is not { } data)
            return false;
        _saves[id] = ToBytes(data.Commented(comment));
        return true;
    }

    /// <inheritdoc />
    public SaveData? ReadPersistent() => _persistent is null ? null : FromBytes(_persistent);

    /// <inheritdoc />
    public bool WritePersistent(SaveData data)
    {
        _persistent = ToBytes(data.Stamped(data.Written ?? _clock()));
        return true;
    }

    private static byte[] ToBytes(SaveData data)
    {
        var stream = new MemoryStream();
        data.WriteTo(stream);
        return stream.ToArray();
    }

    private static SaveData FromBytes(byte[] bytes) => SaveData.ReadFrom(new MemoryStream(bytes));
}

/// <summary>
/// The library as files in a directory (created on the first write): <c>auto-NNNN.ksav</c> (a turn's start),
/// <c>end-NNNN.ksav</c> (its end, numbered with the starts), <c>save-NNNN.ksav</c> and <c>persistent.ksav</c>. A directory of the engine's slots (<c>slotNN.ksav</c>, <see cref="DirectorySaveStore"/>) and
/// no save of the library is migrated as the library opens it: slot 1 becomes the newest auto-save, slots 2-10 manual
/// saves 1-9 (their numbers in the original's lists), their times kept; the slots are moved into <c>legacy</c>, not
/// deleted. The persistent page is not made from them here: the game reads it from its saves the first time
/// (<see cref="ScriptContext.LoadPersistent"/>).
/// </summary>
public sealed class DirectorySaveLibrary : ISaveLibrary
{
    private const string AutoPrefix = "auto-";
    private const string TurnEndPrefix = "end-";
    private const string ManualPrefix = "save-";
    private const string Extension = ".ksav";
    private const string PersistentName = "persistent.ksav";

    /// <summary>Opens the library in <paramref name="directory"/>, migrating the engine's slots there.</summary>
    public DirectorySaveLibrary(string directory)
    {
        Directory = directory;
        Migrate();
    }

    /// <summary>The directory the saves are in.</summary>
    public string Directory { get; }

    /// <inheritdoc />
    public IReadOnlyList<SaveId> Autos =>
        [.. Numbers(AutoPrefix).Concat(Numbers(TurnEndPrefix)).Distinct().OrderDescending().Select(n => new SaveId(SaveKind.Auto, n))];

    /// <inheritdoc />
    public IReadOnlyList<SaveId> Manuals => [.. Numbers(ManualPrefix).Order().Select(n => new SaveId(SaveKind.Manual, n))];

    /// <inheritdoc />
    public SaveData? Read(SaveId id) => ReadFile(PathOf(id));

    /// <inheritdoc />
    public AutoRing RingOf(SaveId id) =>
        id.Kind == SaveKind.Auto && File.Exists(PathOf(id, AutoRing.TurnEnd)) ? AutoRing.TurnEnd : AutoRing.TurnStart;

    /// <inheritdoc />
    public SaveId? WriteAuto(SaveData data, AutoRing ring = AutoRing.TurnStart)
    {
        var autos = Autos;
        var id = new SaveId(SaveKind.Auto, autos.Count == 0 ? 1 : autos[0].Number + 1);
        if (!WriteFile(PathOf(id, ring), data.Stamped(data.Written ?? DateTime.Now)))
            return null;
        foreach (var old in Numbers(ring == AutoRing.TurnEnd ? TurnEndPrefix : AutoPrefix).OrderDescending().Skip(ISaveLibrary.AutoCount))
            Delete(new SaveId(SaveKind.Auto, old));
        return id;
    }

    /// <inheritdoc />
    public SaveId? WriteManual(SaveData data, int? over = null)
    {
        var manuals = Manuals;
        var id = new SaveId(SaveKind.Manual, over ?? (manuals.Count == 0 ? 1 : manuals[^1].Number + 1));
        string comment = Read(id)?.Comment ?? data.Comment;
        return WriteFile(PathOf(id), data.Stamped(data.Written ?? DateTime.Now).Commented(comment)) ? id : null;
    }

    /// <inheritdoc />
    public bool Delete(SaveId id)
    {
        string path = PathOf(id);
        try
        {
            if (!File.Exists(path))
                return false;
            File.Delete(path);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <inheritdoc />
    public bool SetComment(SaveId id, string comment) => Read(id) is { } data && WriteFile(PathOf(id), data.Commented(comment));

    /// <inheritdoc />
    public SaveData? ReadPersistent() => ReadFile(Path.Combine(Directory, PersistentName));

    /// <inheritdoc />
    public bool WritePersistent(SaveData data) => WriteFile(Path.Combine(Directory, PersistentName), data.Stamped(data.Written ?? DateTime.Now));

    /// <summary>The save's file: an auto-save's in its ring's name, a turn's start when there is none yet.</summary>
    private string PathOf(SaveId id) => PathOf(id, RingOf(id));

    private string PathOf(SaveId id, AutoRing ring)
    {
        string prefix = id.Kind == SaveKind.Manual ? ManualPrefix : ring == AutoRing.TurnEnd ? TurnEndPrefix : AutoPrefix;
        return Path.Combine(Directory, $"{prefix}{id.Number:D4}{Extension}");
    }

    /// <summary>The numbers of the saves whose files start with <paramref name="prefix"/>.</summary>
    private IEnumerable<int> Numbers(string prefix)
    {
        if (!System.IO.Directory.Exists(Directory))
            return [];
        return System.IO.Directory.EnumerateFiles(Directory, prefix + "*" + Extension)
            .Select(path => Path.GetFileNameWithoutExtension(path)[prefix.Length..])
            .Select(s => int.TryParse(s, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int n) ? n : 0)
            .Where(n => n > 0)
            .ToList();
    }

    /// <summary>The file's save, dated by the file's time when it has no stamp (format 1-2); null when there is none or it cannot be read.</summary>
    private static SaveData? ReadFile(string path)
    {
        if (!File.Exists(path))
            return null;
        try
        {
            using var stream = File.OpenRead(path);
            var data = SaveData.ReadFrom(stream);
            return data.Written is null ? data.Stamped(File.GetLastWriteTime(path)) : data;
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private bool WriteFile(string path, SaveData data)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            string temp = path + ".tmp";
            using (var stream = File.Create(temp))
                data.WriteTo(stream);
            File.Move(temp, path, overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>The engine's slots made the library's saves when the directory has slots and no save of the library.</summary>
    private void Migrate()
    {
        if (!System.IO.Directory.Exists(Directory) || Autos.Count > 0 || Manuals.Count > 0)
            return;
        var slots = new DirectorySaveStore(Directory);
        string legacy = Path.Combine(Directory, "legacy");
        for (int slot = 1; slot <= ISaveStore.SlotCount; slot++)
        {
            string path = Path.Combine(Directory, $"slot{slot:D2}{Extension}");
            if (!File.Exists(path))
                continue;
            if (slot <= 10 && slots.Read(slot) is { } data)
            {
                var id = slot == 1 ? new SaveId(SaveKind.Auto, 1) : new SaveId(SaveKind.Manual, slot - 1);
                if (!WriteFile(PathOf(id), data))
                    continue;
            }
            System.IO.Directory.CreateDirectory(legacy);
            File.Move(path, Path.Combine(legacy, Path.GetFileName(path)), overwrite: true);
        }
    }
}

/// <summary>
/// The engine's numbered slots over a library, for the scenario's own commands while the library is in use: slot 1 is
/// the newest auto-save (<c>Iｱﾘｽ.ADV:19</c>'s <c>LP 1</c>, <c>LT 1</c> after the autosave); every other slot is empty, and
/// nothing is written through the slots — the port of <c>ﾛｰﾄﾞｾｰﾌﾞ</c> writes the library itself.
/// </summary>
public sealed class LibrarySlots(ISaveLibrary library) : ISaveStore
{
    /// <inheritdoc />
    public SaveData? Read(int slot) => slot == 1 && library.Autos is [var newest, ..] ? library.Read(newest) : null;

    /// <inheritdoc />
    public bool Write(int slot, SaveData data) => false;

    /// <inheritdoc />
    public DateTime? Timestamp(int slot) => Read(slot)?.Written;
}
