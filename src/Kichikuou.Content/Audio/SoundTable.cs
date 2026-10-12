namespace Kichikuou.Content.Audio;

/// <summary>
/// Sound effect numbers (<c>SP n</c>, <c>SQ l, r</c>, <c>SM n</c>) → WAV files. The engine looks
/// n up 1-based in the link table of <c>KICHIKUWA.ALD</c> (<c>pcm.sdlmixer.c:67-82</c>); the
/// extracted <c>wav\</c> folder keeps the archive's entry names (docs/assets.md §4).
/// </summary>
public static class SoundTable
{
    private static readonly Dictionary<int, string> Files = new()
    {
        [1] = "R5FEF01.WAV", [2] = "R5FEF02.WAV", [5] = "DUMMY.WAV",
        [6] = "R5ATK01A.WAV", [7] = "R5ATK01B.WAV", [8] = "R5ATK01C.WAV", [9] = "R5ATK01D.WAV",
        [10] = "R5ATK02A.WAV", [11] = "R5ATK02B.WAV", [12] = "R5ATK02C.WAV", [13] = "R5ATK02D.WAV",
        [14] = "R5ATK03A.WAV", [15] = "R5ATK03B.WAV", [16] = "R5ATK03C.WAV", [17] = "R5ATK03D.WAV",
        [18] = "R5ATK03E.WAV", [19] = "R5ATK03F.WAV", [20] = "R5ATK04A.WAV", [21] = "R5ATK04B.WAV",
        [31] = "R5SAK01A.WAV", [32] = "R5SAK01B.WAV", [33] = "R5SAK02A.WAV", [34] = "R5SAK02B.WAV",
        [35] = "R5SAK03A.WAV", [36] = "R5SAK03B.WAV", [37] = "R5SAK04A.WAV", [38] = "R5SAK04B.WAV",
        [39] = "R5SAK05A.WAV", [40] = "R5SAK05B.WAV", [41] = "R5SAK06A.WAV", [42] = "R5SAK06B.WAV",
        [43] = "R5SAK07A.WAV", [44] = "R5SAK07B.WAV", [45] = "R5SAK08A.WAV", [46] = "R5SAK08B.WAV",
        [51] = "R5ESE001.WAV", [63] = "R5ESE013.WAV", [70] = "PDEMO003.WAV", [74] = "R5ESE024.WAV",
        [80] = "R5ESE030.WAV", [89] = "R5ESE039.WAV", [90] = "R5ESE040.WAV",
        [101] = "R5ESE051.WAV", [102] = "R5ESE052.WAV", [103] = "R5ESE053.WAV", [104] = "R5ESE054.WAV",
        [105] = "R5ESE055.WAV", [108] = "R5ESE058.WAV", [109] = "R5ESE059.WAV", [110] = "R5ESE060.WAV",
        [111] = "R5ESE061.WAV",
    };

    /// <summary>Every linked sound number.</summary>
    public static IReadOnlyCollection<int> Numbers => Files.Keys;

    /// <summary>The WAV file name of sound <paramref name="number"/>, or <c>null</c> when it is not linked.</summary>
    public static string? FileName(int number) => Files.GetValueOrDefault(number);

    /// <summary>
    /// Whether sound <paramref name="number"/> plays nothing: 5 (<c>DUMMY.WAV</c>) is the script's
    /// "no sound" value (<c>FIGTATTK.ADV:349</c>), and an unlinked number loads nothing.
    /// </summary>
    public static bool IsSilent(int number) => number == 5 || !Files.ContainsKey(number);
}
