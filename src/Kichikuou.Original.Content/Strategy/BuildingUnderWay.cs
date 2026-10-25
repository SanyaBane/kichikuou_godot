using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// The building under way while <c>BuildWeeksLeft</c> is above 0, for the Construction tag's third state on our status
/// strip and command panel (docs/ui/command-screen.md, "A building under way"; ours: <c>Cﾃｲﾄ.ADV</c> only says so in
/// Maris's refusal, <c>:74-219</c>). <paramref name="Name"/> is the building whose <c>Bld_*</c> is 2, as the heading
/// of its terms in <c>Cﾃｲﾄ</c> names it ("Library"); null when none is (no state the script makes).
/// </summary>
public sealed record BuildingUnderWay(string? Name, int WeeksLeft)
{
    private const string File = "Cﾃｲﾄ.ADV";

    /// <summary>"5 weeks left", "1 week left".</summary>
    public string WeeksText => WeeksLeft == 1 ? "1 week left" : $"{WeeksLeft} weeks left";

    /// <summary>
    /// The building under way in <paramref name="state"/>, or null while none is (<c>BuildWeeksLeft</c> 0: it is
    /// counted down before the command menu, <c>COMMAND.ADV:18-23</c>, so the week it is done it is 0 again);
    /// <paramref name="text"/> formats a text run with its numbers.
    /// </summary>
    public static BuildingUnderWay? For(GameState state, Func<ScriptRef, IReadOnlyList<Sys3Int>, string> text)
    {
        int left = state.BuildWeeksLeft.Value;
        if (left <= 0)
            return null;
        // The buildings in BusyAsync's order (:107-127); the line is the heading of each one's terms, its first line
        // ("［Library］"), the budget after it the run's one number.
        int line = state.Bld_GirlsAcademy == 2 ? 339
            : state.Bld_Library == 2 ? 426
            : state.Bld_Hospital == 2 ? 528
            : state.Bld_SMTower == 2 ? 709
            : state.Bld_MariaLab == 2 ? 814
            : state.Bld_MagicLab == 2 ? 904
            : 0;
        string? name = line == 0 ? null
            : text(new ScriptRef(File, line), [state.Gold]).Split('\n')[0].Trim(' ', '　', '[', ']', '［', '］').Replace('’', '\'');
        return new BuildingUnderWay(name, left);
    }
}
