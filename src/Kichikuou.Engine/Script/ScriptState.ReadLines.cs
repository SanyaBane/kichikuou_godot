namespace Kichikuou.Engine.Script;

public abstract partial class ScriptState
{
    /// <summary>
    /// The prefix of the keys of the read lines in <see cref="Extras"/>: one key per scenario file,
    /// <c>read:&lt;file&gt;</c> (<see cref="ReadLinesKey"/>).
    /// </summary>
    public const string ReadLinesKeyPrefix = "read:";

    /// <summary>The key in <see cref="Extras"/> of the read lines of <paramref name="file"/>.</summary>
    public static string ReadLinesKey(string file) => ReadLinesKeyPrefix + file;

    /// <summary>
    /// The lines of <paramref name="file"/> the game has shown, ascending: every message run and every
    /// menu item's text the context showed (<see cref="ScriptContext.View"/>, <see cref="ScriptContext.SelectAsync"/>).
    /// Ours (owner's decision 30, docs/ui/talk-marks.md §2), kept with the saves; a save from before it has none.
    /// No rule reads it.
    /// </summary>
    public IReadOnlyList<int> ReadLines(string file) => Extras.GetValueOrDefault(ReadLinesKey(file)) ?? [];

    /// <summary>Whether <paramref name="line"/> has been shown (<see cref="ReadLines"/>).</summary>
    public bool IsLineRead(ScriptRef line) =>
        Extras.TryGetValue(ReadLinesKey(line.File), out var read) && Array.BinarySearch(read, line.Line) >= 0;

    /// <summary>Adds <paramref name="line"/> to the read lines of its file (<see cref="ReadLines"/>).</summary>
    public void MarkLineRead(ScriptRef line)
    {
        string key = ReadLinesKey(line.File);
        var read = Extras.GetValueOrDefault(key) ?? [];
        int at = Array.BinarySearch(read, line.Line);
        if (at >= 0)
            return;
        at = ~at;
        Extras[key] = [.. read[..at], line.Line, .. read[at..]];
    }
}
