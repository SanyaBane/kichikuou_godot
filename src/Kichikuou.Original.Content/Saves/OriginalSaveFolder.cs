using System.Globalization;
using Kichikuou.Engine.Script;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Saves;

/// <summary>
/// One save file of the original in a folder, as Android offers it (docs/save-import.md, §8: the phone's Menu sheet's page,
/// the tablet's list): read and checked (<see cref="OriginalSave.Read"/>) — its slot of the port, its week's date and story
/// stage — or refused, with the reason in words and in a word or two. Presentation only: no rule reads it.
/// </summary>
/// <param name="Path">The file.</param>
/// <param name="Save">The slot it becomes; null when refused or a sample (the screen gallery's).</param>
/// <param name="Week">Its week (<c>Week</c>, VAR0517), 0 when refused.</param>
/// <param name="Stage">Its story stage (<c>StoryStage</c>), 0 when refused.</param>
/// <param name="Written">The file's time.</param>
/// <param name="Refusal">Why it cannot be imported, in the reader's words; null when it can.</param>
/// <param name="RefusalWord">The same in a word or two (<see cref="OriginalSaveException.Word"/>).</param>
public sealed record OriginalSaveFile(string Path, SaveData? Save, int Week, int Stage, DateTime? Written,
    string? Refusal = null, string RefusalWord = "")
{
    /// <summary>The file's name, <c>KICHIKUSE.asd</c>.</summary>
    public string File => System.IO.Path.GetFileName(Path);

    /// <summary>"Original save E", as "Load Complete" names it (<see cref="OriginalSave.Label"/>).</summary>
    public string Label => OriginalSave.Label(Path);

    /// <summary>The slot's letter, "E"; empty for a file named otherwise.</summary>
    public string Letter => Label.Length > OriginalSave.LabelStem.Length ? Label[^1..] : "";

    /// <summary>The tile's word: "Save E", or the file's name without its extension.</summary>
    public string Word => Letter.Length > 0 ? $"Save {Letter}" : System.IO.Path.GetFileNameWithoutExtension(Path);

    /// <summary>Whether it cannot be imported.</summary>
    public bool Refused => Refusal is not null;

    /// <summary>"LP 3 · Month 5 · Week 1".</summary>
    public string DateText
    {
        get
        {
            var (year, month, week) = StatusStrip.DateOf(Week);
            return $"LP {year} · Month {month} · Week {week}";
        }
    }

    /// <summary>"LP 3 · M5 · W1", a tile's.</summary>
    public string ShortDate
    {
        get
        {
            var (year, month, week) = StatusStrip.DateOf(Week);
            return $"LP {year} · M{month} · W{week}";
        }
    }

    /// <summary>"Stage 1", or "Prologue" before story stage 1.</summary>
    public string StageText => Stage is 0 or 99 ? "Prologue" : $"Stage {Stage}";

    /// <summary>", saved 2026-10-10 21:40", as the PC's question says it; empty without a time.</summary>
    public string WrittenText => Written is { } time ? $", saved {time.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)}" : "";

    /// <summary>The question's lines before a load (the PC's box, docs/save-import.md, §7): the save and its date, the file and its time.</summary>
    public IReadOnlyList<string> QuestionLines => [$"{Label}: {DateText}", File + WrittenText];
}

/// <summary>The original's save files in a folder (docs/save-import.md, §8): every <c>*.asd</c>, by name.</summary>
public static class OriginalSaveFolder
{
    /// <summary>The <c>*.asd</c> files of <paramref name="folder"/> (any case) by name, each read; none when it is not there.</summary>
    public static IReadOnlyList<OriginalSaveFile> Read(string folder)
    {
        if (folder.Length == 0 || !Directory.Exists(folder))
            return [];
        return [.. Directory.EnumerateFiles(folder)
            .Where(f => f.EndsWith(".asd", StringComparison.OrdinalIgnoreCase))
            .Order(StringComparer.OrdinalIgnoreCase)
            .Select(ReadFile)];
    }

    /// <summary>One file read: its slot, or the refusal.</summary>
    public static OriginalSaveFile ReadFile(string path)
    {
        try
        {
            return Of(path, OriginalSave.Read(path));
        }
        catch (OriginalSaveException e)
        {
            return new OriginalSaveFile(path, null, 0, 0, null, e.Message, e.Word);
        }
    }

    /// <summary><paramref name="save"/>, read from <paramref name="path"/>: its week and stage from its memory.</summary>
    public static OriginalSaveFile Of(string path, SaveData save)
    {
        var state = new GameState();
        save.Restore(state);
        return new OriginalSaveFile(path, save, state.Week.Value, state.StoryStage.Value, save.Written);
    }
}
