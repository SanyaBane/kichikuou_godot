using Kichikuou.Engine.Script;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// The system text the already-read text's colour leaves in the window's own colour (owner, 2026-10-26;
/// docs/ui/read-text.md, §6): the narration (the windows 113/123) of the files that are not event scenes
/// (<see cref="MessageLog.IsEventScene"/>) — the status windows, the questions, the notices, the results and a command's
/// repeated lines ("Kanami came back with information.") — but for the story those files tell (<see cref="Story"/>),
/// and the lines of <see cref="Lines"/> in any window. A speech or a thought takes the colour from any file. A presenter
/// asks it as a message is printed: system text never counts as read, as a message with numbers.
/// </summary>
public static class SystemText
{
    /// <summary>
    /// The story told in narration outside the event scenes, by its lines in the decompile (first and last, both in): the
    /// scenes a player meets once or in a talk, not a command's lines that come back on every use.
    /// </summary>
    private static readonly (string File, int First, int Last)[] Story =
    [
        ("Cｼｮｳﾆﾝ.ADV", 378, 527),         // Louis (unit 49) hired the first time
        ("Cｼｮｳﾆﾝ.ADV", 725, 925),         // Cecil (unit 50) hired the first time
        ("Cｼｮｳﾆﾝ.ADV", 1401, 1714),       // the first summons; Pluepet back after his "death"
        ("Cｼﾊｲﾄｼ.ADV", 1177, 1503),       // the Cat Bread (item 24) and the Kuriripu (item 25) found
        ("Cﾃｷﾄｼ.ADV", 604, 3648),         // the talks with the independent cities' mayors
        ("Cﾃｷﾄｼ.ADV", 4150, 4264),        // Zeth's magic barrier met the first time
        ("Cﾒｲｷｭｳ.ADV", 1300, 1370),       // Gunagan's treasure; Merim's injury or death
        ("Cﾒｲｷｭｳ.ADV", 1376, 1444),       // Orochi
        ("Cﾒｲｷｭｳ.ADV", 2009, 2871),       // KD at the Rising Dragon Mountain's summit; the Thieves' Hideout
        ("Cﾒｲｷｭｳ.ADV", 3500, 3854),       // God's Door found
        ("Cﾒｲｷｭｳ.ADV", 4027, 4556),       // God's Door opens, Rance's talk before it, Kouhime's hint, Enter
        ("Cﾒｲｷｭｳ.ADV", 4557, 5036),       // Merim's inscriptions, up to the Super Egg's gains (:5037-5046)
        ("TJAPAN.ADV", 295, 2744),        // Gekkou's and Shinobu's attempts
        ("Tｹｲﾌﾞﾘｽ.ADV", 1716, 1728),      // Helman collapses
        ("Tｹｲﾌﾞﾘｽ.ADV", 1748, 1759),      // Zeth collapses
        ("FIGT_B.ADV", 813, 820),         // Leazas Castle falls
        ("FIGT_B.ADV", 870, 877),
    ];

    /// <summary>The system lines a window other than the narration shows.</summary>
    private static readonly HashSet<ScriptRef> Lines =
    [
        new("CG.ADV", 2332),              // the gallery, in 131: "Right click (cancel key) to replay the scene."
    ];

    /// <summary>Whether the message at <paramref name="at"/> is system text, printed into a narration window (113/123) or not.</summary>
    public static bool Is(ScriptRef at, bool narration) =>
        Lines.Contains(at) || narration && !MessageLog.IsEventScene(at.File) && !InStory(at);

    private static bool InStory(ScriptRef at)
    {
        foreach (var (file, first, last) in Story)
        {
            if (file == at.File && at.Line >= first && at.Line <= last)
                return true;
        }
        return false;
    }
}
