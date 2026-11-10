using Kichikuou.Content;
using Kichikuou.Content.Saves;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Saves;

/// <summary>
/// Ours (owner, 2026-11-10; docs/save-import.md, §2-§3, §7): a save of the original game (<see cref="AsdSave"/>) as a
/// slot of the port, checked against the one version the port knows (<see cref="GameFiles.EnglishTranslation10"/>). The
/// scenario has one <c>QD</c> (<c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:33</c>), reached from the two weekly autosaves of <c>MAIN.ADV</c> only,
/// so every save of the original goes on right after it with one of two call stacks: a near call returning after
/// <c>MAIN.ADV:20</c> (the thieves' week) or <c>:83</c> (the Leazas week), then the far call from <c>L_01875</c>. Those
/// are the port's own autosaves (<c>WeekLoop.ResumeAsync</c>), so the save becomes a slot resuming there: page 0's
/// first 806 values, pages 1, 2, 4 and 10, the strings by the same numbers, the file's time — and the mark of a game
/// imported from the original (<see cref="GameState.ImportedFromOriginal"/>) as its only data of ours. Anything else is
/// refused with the reason in words (<see cref="OriginalSaveException"/>). The file itself is never written.
/// </summary>
public static class OriginalSave
{
    /// <summary><c>MAIN.ADV</c>'s page (0-based, the scenario archive's link order).</summary>
    public const int MainPage = 12;

    /// <summary><c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>'s page.</summary>
    public const int SaveScreenPage = 33;

    /// <summary>Right after <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:33</c>'s <c>QD 1:</c> (at 0xcc): where every save of the original goes on.</summary>
    public const int ResumeAddress = 0xd0;

    /// <summary>Right after <c>L_01875</c>'s <c>%#ﾛｰﾄﾞｾｰﾌﾞ.ADV:</c> (<c>MAIN.ADV:1058</c>): the far call's return.</summary>
    public const int FarCallReturn = 0x187c;

    /// <summary>The near calls' returns in <c>MAIN.ADV</c>, after <c>:20</c>'s and <c>:83</c>'s <c>\L_01875:</c> (at 0x9c and 0x1eb), and the autosave each is.</summary>
    public static IReadOnlyDictionary<int, ScriptRef> Autosaves { get; } = new Dictionary<int, ScriptRef>
    {
        [0xa1] = new("MAIN.ADV", 20),
        [0x1f0] = new("MAIN.ADV", 83),
    };

    /// <summary>The pages the version's saves hold besides page 0, with their sizes (<c>SET.ADV</c>'s <c>DC</c> with the save flag).</summary>
    public static IReadOnlyDictionary<int, int> PageSizes { get; } = new Dictionary<int, int>
    {
        [1] = 20001,
        [2] = 1801,
        [4] = 2101,
        [10] = 301,
    };

    private static string Version => GameFiles.EnglishTranslation10.Name;

    /// <summary>Reads the save file at <paramref name="path"/> as a slot of the port, dated by the file's time.</summary>
    /// <exception cref="OriginalSaveException">It cannot be read, or is no save of the version the port knows; the message says why.</exception>
    public static SaveData Read(string path)
    {
        AsdSave save;
        DateTime written;
        try
        {
            save = AsdSave.Read(path);
            written = File.GetLastWriteTime(path);
        }
        catch (AsdFormatException e)
        {
            throw new OriginalSaveException(e.Message, e);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            throw new OriginalSaveException($"The file cannot be read: {e.Message}", e);
        }
        return FromAsd(save, written);
    }

    /// <summary>The save as a slot of the port, stamped with <paramref name="written"/>.</summary>
    /// <exception cref="OriginalSaveException">It is no save of the version the port knows; the message says why.</exception>
    public static SaveData FromAsd(AsdSave save, DateTime? written)
    {
        var resume = Resume(save);
        var page0 = save.Pages[0];
        int variableCount = OriginalVariant.Profile.VariableCount;
        if (page0.Length > variableCount && Array.FindIndex(page0, variableCount, v => v != 0) is var beyond and >= 0)
            throw Another($"it uses variable {beyond}, past the {variableCount} this version has.");
        var variables = new Sys3Int[variableCount];
        for (int v = 0; v < variableCount; v++)
            variables[v] = v < page0.Length ? page0[v] : Sys3Int.Zero;

        var saved = save.Pages.Keys.Where(p => p != 0).Order().ToList();
        if (!saved.SequenceEqual(PageSizes.Keys.Order()))
            throw Another($"it holds the pages {string.Join(", ", saved)}, this version's saves {string.Join(", ", PageSizes.Keys.Order())}.");
        var pages = new Dictionary<int, Sys3Int[]>();
        foreach (var (page, size) in PageSizes)
        {
            var values = save.Pages[page];
            if (values.Length != size)
                throw Another($"its page {page} holds {values.Length} values, this version's {size}.");
            pages[page] = [.. values.Select(v => (Sys3Int)v)];
        }

        var strings = Enumerable.Repeat(string.Empty, ScriptMemory.DefaultStringCount + 1).ToArray();
        for (int i = 0; i < save.Strings.Count; i++)
        {
            if (i < strings.Length)
                strings[i] = save.Strings[i];
            else if (save.Strings[i].Length > 0)
                throw Another($"it uses string {i}, past the {strings.Length - 1} this version has.");
        }

        var extras = new Dictionary<string, int[]> { [GameState.ImportedFromOriginalKey] = [1] };
        return new SaveData(resume, variables, pages, strings, extras, written);
    }

    /// <summary>The autosave the save goes on after, from its resume point and call stack (§2).</summary>
    private static ScriptRef Resume(AsdSave save)
    {
        if ((save.ResumePage, save.ResumeAddress) != (SaveScreenPage, ResumeAddress))
            throw Another($"it goes on at page {save.ResumePage}, address 0x{save.ResumeAddress:x}, where none of this version's saves does.");
        if (save.Stack is not [AsdNearCall near, AsdFarCall { Page: MainPage, Address: FarCallReturn }]
            || !Autosaves.TryGetValue(near.Address, out var autosave))
        {
            throw new OriginalSaveException(
                $"It is a save of {Version} the port cannot go on from: it was not made at a week's start ({Frames(save.Stack)}).");
        }
        return autosave;
    }

    private static string Frames(IReadOnlyList<AsdFrame> stack) => stack.Count == 0 ? "no calls" : string.Join(", ", stack.Select(f => f switch
    {
        AsdNearCall n => $"a call returning to 0x{n.Address:x}",
        AsdFarCall c => $"a call returning to page {c.Page} at 0x{c.Address:x}",
        AsdVariableFrame v => $"a variable of page {v.Page}",
        _ => "the text's settings",
    }));

    private static OriginalSaveException Another(string why) =>
        new($"It is not a save of the version of Kichikuou Rance this game reads ({Version}): {why}");
}

/// <summary>A file that cannot become a save of the port (<see cref="OriginalSave"/>); the message says why, in words for the player.</summary>
public sealed class OriginalSaveException(string message, Exception? inner = null) : Exception(message, inner);
