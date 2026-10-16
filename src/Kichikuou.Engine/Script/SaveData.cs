using System.Text;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Engine.Script;

/// <summary>
/// One save slot's contents, as the engine's <c>QD</c> writes it (xsystem35 <c>savedata.c</c>
/// <c>save_saveAll</c>): page 0 (every numbered variable), the array pages allocated with the save
/// flag, every string variable, and where the game goes on after a load. The original stores the
/// program counter and the call stack; the port stores the place of the call that saved
/// (<see cref="Resume"/>, e.g. <c>MAIN.ADV:20</c>, the weekly autosave), which is the only
/// <c>QD</c> the scenario has (<c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:33</c>). Presentation settings the original also
/// saves (window rectangles, font sizes and colours) are not kept: the UI redraws from the state.
/// The random generator's state is not saved either, as in the original. The slot format is the
/// engine's; whether a slot fits a game is the context's check (its variable count). The port's own
/// data (<see cref="ScriptState.Extras"/>) goes with the memory, from format 2 on.
/// </summary>
public sealed class SaveData
{
    private const string Magic = "KKSAVE";
    private const int FormatVersion = 2;

    /// <summary>Creates a slot's contents; the arrays are kept, not copied.</summary>
    public SaveData(ScriptRef resume, Sys3Int[] variables, IReadOnlyDictionary<int, Sys3Int[]> pages, string[] strings,
        IReadOnlyDictionary<string, int[]>? extras = null)
    {
        Resume = resume;
        Variables = variables;
        Pages = pages;
        Strings = strings;
        Extras = extras ?? new Dictionary<string, int[]>();
    }

    /// <summary>The call that saved: a load goes on right after it.</summary>
    public ScriptRef Resume { get; }

    /// <summary>Page 0, <see cref="ScriptMemory.VariableCount"/> values.</summary>
    public Sys3Int[] Variables { get; }

    /// <summary>The saved array pages by number.</summary>
    public IReadOnlyDictionary<int, Sys3Int[]> Pages { get; }

    /// <summary>The string variables, 1-based as in <see cref="ScriptMemory.Strings"/>.</summary>
    public string[] Strings { get; }

    /// <summary>The port's own data (<see cref="ScriptState.Extras"/>); none in a slot of format 1.</summary>
    public IReadOnlyDictionary<string, int[]> Extras { get; }

    /// <summary><c>QD</c>: the memory as it is now, with the pages the variant saves (<see cref="VariantProfile.SavedPages"/>).</summary>
    public static SaveData Capture(ScriptState state, ScriptRef resume)
    {
        var memory = state.Memory;
        var variables = new Sys3Int[memory.VariableCount];
        for (int v = 0; v < variables.Length; v++)
            variables[v] = memory.Var(v);
        var pages = new Dictionary<int, Sys3Int[]>();
        foreach (int page in state.Profile.SavedPages)
        {
            int size = memory.PageSize(page);
            if (size == 0)
                continue;
            var values = new Sys3Int[size];
            for (int i = 0; i < size; i++)
                values[i] = memory.ReadPage(page, i);
            pages[page] = values;
        }
        return new SaveData(resume, variables, pages, (string[])memory.Strings.Clone(),
            state.Extras.ToDictionary(e => e.Key, e => (int[])e.Value.Clone()));
    }

    /// <summary>
    /// <c>LD</c>: the slot back into the memory (<c>save_loadAll</c>): the strings, page 0 and each
    /// saved page; a page smaller than the saved one is reallocated to the saved size
    /// (<c>loadVarPage</c>); a larger one keeps the slots past the saved values. Page 3 stays.
    /// </summary>
    public void Restore(ScriptMemory memory)
    {
        Array.Copy(Strings, memory.Strings, Math.Min(Strings.Length, memory.Strings.Length));
        for (int v = 0; v < Variables.Length; v++)
            memory.Var(v) = Variables[v];
        foreach (var (page, values) in Pages)
        {
            if (memory.PageSize(page) < values.Length)
            {
                memory.FreePage(page);
                memory.AllocatePage(page, values.Length);
            }
            for (int i = 0; i < values.Length; i++)
                memory.WritePage(page, i, values[i]);
        }
    }

    /// <summary><c>LD</c> into a state: its memory (<see cref="Restore(ScriptMemory)"/>), and the port's own data replaced whole.</summary>
    public void Restore(ScriptState state)
    {
        Restore(state.Memory);
        state.Extras.Clear();
        foreach (var (key, values) in Extras)
            state.Extras[key] = (int[])values.Clone();
    }

    /// <summary>The saved values of <paramref name="page"/> (0 = the numbered variables), or null when the slot has no such page.</summary>
    public Sys3Int[]? Page(int page) => page == 0 ? Variables : Pages.GetValueOrDefault(page);

    /// <summary>A deep copy.</summary>
    public SaveData Clone() => new(
        Resume,
        (Sys3Int[])Variables.Clone(),
        Pages.ToDictionary(p => p.Key, p => (Sys3Int[])p.Value.Clone()),
        (string[])Strings.Clone(),
        Extras.ToDictionary(e => e.Key, e => (int[])e.Value.Clone()));

    /// <summary>The port's own slot format (not the original's <c>.ASD</c>): versioned, little-endian.</summary>
    public void WriteTo(Stream stream)
    {
        using var w = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        w.Write(Magic);
        w.Write(FormatVersion);
        w.Write(Resume.File);
        w.Write(Resume.Line);
        WriteValues(w, Variables);
        w.Write(Pages.Count);
        foreach (var (page, values) in Pages.OrderBy(p => p.Key))
        {
            w.Write(page);
            WriteValues(w, values);
        }
        w.Write(Strings.Length);
        foreach (string s in Strings)
            w.Write(s);
        w.Write(Extras.Count);
        foreach (var (key, values) in Extras.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            w.Write(key);
            w.Write(values.Length);
            foreach (int value in values)
                w.Write(value);
        }
    }

    /// <summary>Reads a slot written by <see cref="WriteTo"/>; fails with <see cref="InvalidDataException"/> on anything else.</summary>
    public static SaveData ReadFrom(Stream stream)
    {
        try
        {
            using var r = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
            if (r.ReadString() != Magic)
                throw new InvalidDataException("Not a save slot of this port.");
            int version = r.ReadInt32();
            if (version is < 1 or > FormatVersion)
                throw new InvalidDataException($"Save format {version} is not supported (expected 1-{FormatVersion}).");
            var resume = new ScriptRef(r.ReadString(), r.ReadInt32());
            var variables = ReadValues(r);
            int pageCount = r.ReadInt32();
            var pages = new Dictionary<int, Sys3Int[]>();
            for (int i = 0; i < pageCount; i++)
            {
                int page = r.ReadInt32();
                if (page <= 0 || page >= ScriptMemory.PageLimit || pages.ContainsKey(page))
                    throw new InvalidDataException($"Bad page number {page}.");
                pages[page] = ReadValues(r);
            }
            int stringCount = r.ReadInt32();
            if (stringCount is < 0 or > 65536)
                throw new InvalidDataException($"Bad string count {stringCount}.");
            var strings = new string[stringCount];
            for (int i = 0; i < stringCount; i++)
                strings[i] = r.ReadString();
            var extras = new Dictionary<string, int[]>();
            if (version >= 2)
            {
                int extraCount = r.ReadInt32();
                if (extraCount is < 0 or > 1024)
                    throw new InvalidDataException($"Bad extra count {extraCount}.");
                for (int i = 0; i < extraCount; i++)
                {
                    string key = r.ReadString();
                    int count = r.ReadInt32();
                    if (count is < 0 or > 65536 || extras.ContainsKey(key))
                        throw new InvalidDataException($"Bad extra {key}.");
                    var values = new int[count];
                    for (int j = 0; j < count; j++)
                        values[j] = r.ReadInt32();
                    extras[key] = values;
                }
            }
            return new SaveData(resume, variables, pages, strings, extras);
        }
        catch (EndOfStreamException e)
        {
            throw new InvalidDataException("The save slot is truncated.", e);
        }
    }

    private static void WriteValues(BinaryWriter w, Sys3Int[] values)
    {
        w.Write(values.Length);
        foreach (var value in values)
            w.Write(value.Value);
    }

    private static Sys3Int[] ReadValues(BinaryReader r)
    {
        int count = r.ReadInt32();
        if (count is < 0 or > 65537)
            throw new InvalidDataException($"Bad value count {count}.");
        var values = new Sys3Int[count];
        for (int i = 0; i < count; i++)
            values[i] = r.ReadUInt16();
        return values;
    }
}

/// <summary>
/// Where the save slots live (the engine's <c>save_fname[]</c> files). Slots are numbered as the
/// script numbers them, 1-based; the engine has <see cref="SlotCount"/> of them, the game uses 1-10
/// (1 is the autosave).
/// </summary>
public interface ISaveStore
{
    /// <summary>The engine's slot count (xsystem35 <c>SAVE_MAXNUMBER</c>).</summary>
    const int SlotCount = 26;

    /// <summary>The slot's contents, or null when it is empty or cannot be read.</summary>
    SaveData? Read(int slot);

    /// <summary>Writes the slot whole; false when it cannot be written.</summary>
    bool Write(int slot, SaveData data);

    /// <summary>When the slot was last written (local time, as the file's mtime), or null when it is empty.</summary>
    DateTime? Timestamp(int slot);
}

/// <summary>Save slots held in memory, in the file format, stamped by a clock (tests, a game without disk saves).</summary>
public sealed class MemorySaveStore(Func<DateTime>? clock = null) : ISaveStore
{
    private readonly Dictionary<int, (byte[] Bytes, DateTime Time)> _slots = [];
    private readonly Func<DateTime> _clock = clock ?? (() => DateTime.Now);

    /// <summary>The slots written so far.</summary>
    public IEnumerable<int> Slots => _slots.Keys.Order();

    /// <inheritdoc />
    public SaveData? Read(int slot) =>
        _slots.TryGetValue(slot, out var s) ? SaveData.ReadFrom(new MemoryStream(s.Bytes)) : null;

    /// <inheritdoc />
    public bool Write(int slot, SaveData data)
    {
        var stream = new MemoryStream();
        data.WriteTo(stream);
        _slots[slot] = (stream.ToArray(), _clock());
        return true;
    }

    /// <inheritdoc />
    public DateTime? Timestamp(int slot) => _slots.TryGetValue(slot, out var s) ? s.Time : null;

    /// <summary>Empties a slot.</summary>
    public void Delete(int slot) => _slots.Remove(slot);
}

/// <summary>Save slots as files <c>slotNN.ksav</c> in a directory (created on the first write).</summary>
public sealed class DirectorySaveStore(string directory) : ISaveStore
{
    /// <summary>The directory the slots are in.</summary>
    public string Directory { get; } = directory;

    private string PathOf(int slot) => Path.Combine(Directory, $"slot{slot:D2}.ksav");

    /// <inheritdoc />
    public SaveData? Read(int slot)
    {
        string path = PathOf(slot);
        if (!File.Exists(path))
            return null;
        try
        {
            using var stream = File.OpenRead(path);
            return SaveData.ReadFrom(stream);
        }
        catch (Exception e) when (e is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public bool Write(int slot, SaveData data)
    {
        try
        {
            System.IO.Directory.CreateDirectory(Directory);
            string path = PathOf(slot);
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

    /// <inheritdoc />
    public DateTime? Timestamp(int slot)
    {
        string path = PathOf(slot);
        return File.Exists(path) ? File.GetLastWriteTime(path) : null;
    }
}

/// <summary>
/// <c>LD n</c> succeeded: the slot is in the memory and <c>RetVal</c> is 0; the running code is
/// abandoned, and the game goes on right after <see cref="Resume"/> (the original jumps to the
/// saved program counter with the saved call stack).
/// </summary>
public sealed class GameLoadedException(int slot, ScriptRef resume)
    : Exception($"Slot {slot} loaded: the game goes on after {resume}.")
{
    /// <summary>The slot that was loaded.</summary>
    public int Slot { get; } = slot;

    /// <summary>The call that saved the slot.</summary>
    public ScriptRef Resume { get; } = resume;
}
