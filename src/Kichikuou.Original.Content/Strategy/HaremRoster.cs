using Kichikuou.Content;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// A girl on offer in the harem: her number (her cell in <c>Sﾊｰﾚﾑ</c>'s grid), her name from its string table, how
/// often she was summoned, the week she last was (0: never), her picture (0: none) and her visit's mark (ours, decision
/// 30: NEW, ??? or read; null when none is known).
/// </summary>
public sealed record HaremGirl(int Number, string Name, int Visits, int LastWeek, int PictureCg, SceneMark? Mark = null);

/// <summary>
/// Our own Harem screen's model (docs/ui/pickers.md, mock-up H2): the girls <c>Cﾊｰﾚﾑ</c> offers, read from the
/// state as <c>Sﾊｰﾚﾑ</c> shows them, and the gestures by which the screen answers the picker. It decides nothing:
/// the picker still hit-tests the click and <c>Cﾊｰﾚﾑ</c> plays her scene.
/// </summary>
public static class HaremRoster
{
    /// <summary>The file whose string table names the girls (<c>#L_00848, 1:</c>, <c>Sﾊｰﾚﾑ.ADV:56-58</c>).</summary>
    public static readonly ScriptRef NamesAt = new("Sﾊｰﾚﾑ.ADV", 57);

    /// <summary>
    /// The girls on offer in their numbers' order: <c>HaremAvail_*</c> (<c>VAR0249</c>-<c>VAR0298</c>, which
    /// <c>Sﾊｰﾚﾑ.ADV:15-16</c> copies into its list), their counts from the history records
    /// (<c>Hist.Harem_SexCount</c>, <c>Hist.Harem_LastWeek</c>, <c>:131-148</c>), their visits' marks from
    /// <paramref name="marks"/> (<see cref="PickerMarks.Visits"/>) — none in a game imported from the original's save
    /// (<see cref="GameState.ImportedFromOriginal"/>, owner, 2026-11-10).
    /// </summary>
    public static IReadOnlyList<HaremGirl> Girls(GameState s, IReadOnlyList<string> names, IReadOnlyDictionary<int, SceneMark>? marks = null)
    {
        var girls = new List<HaremGirl>();
        for (int n = 1; n <= 50; n++)
        {
            if (s.Memory.Var(249 + n - 1) == 0)
                continue;
            var history = s.History[(Sys3Int)n];
            string name = n - 1 < names.Count ? Hankaku.ToHalfWidth(names[n - 1]) : "";
            SceneMark? mark = marks is not null && !s.ImportedFromOriginal && marks.TryGetValue(n, out var m) ? m : null;
            girls.Add(new HaremGirl(n, name, history.Harem_SexCount.Value, history.Harem_LastWeek.Value, PictureOf(s, n), mark));
        }
        return girls;
    }

    /// <summary>
    /// The girl's picture as <c>Sﾊｰﾚﾑ.ADV:131-148</c> draws it: in the prologue only Sill (1), Soul (11) and Anasel
    /// (12) have one (699, 698, 697); later 700 + her number, grown-up Mill (18) 695.
    /// </summary>
    public static int PictureOf(GameState s, int girl)
    {
        if (s.F_Mill_State > 1 && girl == 18)
            return 695;
        if (s.StoryStage != 0)
            return 700 + girl;
        return girl switch { 1 => 699, 11 => 698, 12 => 697, _ => 0 };
    }

    /// <summary>The screen's background and the picture panel's empty picture (<c>:5-8</c>, <c>:112-117</c>): the prologue's 694/696, later 818/700.</summary>
    public static (int Screen, int Empty) Background(GameState s) => s.StoryStage == 0 ? (694, 696) : (818, 700);

    /// <summary>
    /// The girl the screen chooses first: the one chosen before while she is still on offer, else the girl
    /// summoned last time (<c>Harem_LastPick</c>) if she is, else the first.
    /// </summary>
    public static int FirstChoice(GameState s, IReadOnlyList<HaremGirl> girls, int chosen)
    {
        if (girls.Any(g => g.Number == chosen))
            return chosen;
        int last = s.Harem_LastPick.Value;
        return girls.Any(g => g.Number == last) ? last : girls.Count > 0 ? girls[0].Number : 0;
    }

    /// <summary>
    /// Ours (decision 30, the owner's H): the info panel's "This visit" — "Something new" (NEW), "Something may happen" (???),
    /// "Nothing new" (read); null without a mark.
    /// </summary>
    public static string? VisitText(SceneMark? mark) => mark switch
    {
        SceneMark.New => "Something new",
        SceneMark.Maybe => "Something may happen",
        SceneMark.Read => "Nothing new",
        _ => null,
    };

    /// <summary>"LP 3 · Month 5 · Week 2", or null for week 0 (never).</summary>
    public static string? DateText(int week)
    {
        if (week == 0)
            return null;
        var (year, month, w) = StatusStrip.DateOf(week);
        return $"LP {year} · Month {month} · Week {w}";
    }

    /// <summary>Where the script's pointer rests under our screen: the grid's empty 51st cell, so the picker hovers nobody.</summary>
    public static (int X, int Y) Rest => GesturePoints.PickerCell(51);

    /// <summary>Summon: a click on her cell, which the picker answers with her number (<c>:46-91</c>).</summary>
    public static void Summon(int girl, PointerScript pointer)
    {
        var (x, y) = GesturePoints.PickerCell(girl);
        pointer.Click(x, y);
    }

    /// <summary>End: a click on the bar's "End" (503, <c>:178-207</c>); <c>Cﾊｰﾚﾑ</c> gives the week's visit back.</summary>
    public static void End(PointerScript pointer)
    {
        var (x, y) = GesturePoints.PickerButton(3);
        pointer.Click(x, y);
    }
}
