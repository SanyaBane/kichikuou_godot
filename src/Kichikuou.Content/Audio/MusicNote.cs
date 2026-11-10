namespace Kichikuou.Content.Audio;

/// <summary>
/// What the import screen says when the chosen copy lacks music (the owner's B, 2026-11-10; docs/import.md, step 8): a row
/// of its own between the scenario read and Play, its <see cref="Title"/> in vermilion ink and its <see cref="Body"/> under
/// it. The playlist is no part of the version check (<see cref="MusicTable"/>), so a copy without its music imports and
/// plays silent where a track is missing; this says so once.
/// </summary>
public sealed record MusicNote(string Title, string Body)
{
    /// <summary>The most missing tracks the body names before "and n more".</summary>
    public const int Named = 3;

    /// <summary>The note for <paramref name="music"/>'s folder, or null when every listed track is there.</summary>
    public static MusicNote? Of(MusicTable music) => Of(music.Listed, music.Missing());

    /// <summary>
    /// The note for the tracks <paramref name="listed"/> of which <paramref name="missing"/> are not in the folder (paths
    /// under the game folder, as <see cref="MusicTable.Listed"/>), or null when none is missing. All missing: "No music
    /// found", the list's first and last track; some: how many, the first <see cref="Named"/> of them.
    /// </summary>
    public static MusicNote? Of(IReadOnlyList<string> listed, IReadOnlyList<string> missing)
    {
        if (missing.Count == 0)
            return null;
        if (missing.Count >= listed.Count)
        {
            string tracks = listed.Count == 1 ? missing[0] : $"{missing[0]} … {Path.GetFileName(missing[^1])}";
            return new MusicNote("No music found",
                $"No {tracks} in the folder: the game plays without music. {CopyHint(missing[0])}");
        }
        bool one = missing.Count == 1;
        return new MusicNote($"{missing.Count} of the {listed.Count} tracks {(one ? "is" : "are")} missing",
            $"{Names(missing)} {(one ? "is" : "are")} not in the folder: the game plays the others and is silent where "
            + $"{(one ? "it" : "these")} would play.");
    }

    /// <summary>The first missing tracks, the first with its folder: "bgm\a.mp3, b.mp3 and c.mp3", "… and 4 more".</summary>
    private static string Names(IReadOnlyList<string> missing)
    {
        var names = missing.Take(Named).Select((track, i) => i == 0 ? track : Path.GetFileName(track)).ToList();
        int more = missing.Count - names.Count;
        if (more > 0)
            return $"{string.Join(", ", names)} and {more} more";
        return names.Count == 1 ? names[0] : $"{string.Join(", ", names[..^1])} and {names[^1]}";
    }

    /// <summary>Where to copy the music from: the tracks' folder (the translation's <c>bgm</c>), else the files themselves.</summary>
    private static string CopyHint(string track) =>
        Path.GetDirectoryName(track) is { Length: > 0 } folder
            ? $"Copy the {folder} folder of your copy here to hear it."
            : "Copy the music files of your copy here to hear it.";
}
