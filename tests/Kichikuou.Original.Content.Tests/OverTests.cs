using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// <c>OVER</c>, the ending (milestone 6 step 1; docs/strategy/survey-w4.md §2), run from the player's
/// files on a booted new game (<see cref="EndGame"/>) with chosen states: the clear count, the
/// fates of the Completion Report merged into page 4 (0, 1 fortune, 10 misfortune, 100 both) and
/// kept in every slot by <c>ﾛｰﾄﾞｾｰﾌﾞ</c> 11, the staff roll, and the file's oddities (STR-25, STR-27 to STR-30).
/// Every key wait and pause is answered at once. Skipped without the decompile.
/// </summary>
public class OverTests
{
    private const int Clears = 79;
    private const int Sill = 0, Lia = 1, Wendy = 4, Maria = 12, Satella = 25, Kouhime = 41, Sanakia = 52, Cell = 53;

    /// <summary>
    /// Maria's ending (5) is a clear: the count goes from 0 to 1; Sill's fortune (her fate 1, Sill
    /// (unit 2) at Rance's side) and Maria's are recorded, and page 4 is written into the slots that
    /// hold a save (1 and 3), not into an empty one. The report, "THE END", "Closing", then the quit.
    /// </summary>
    [ScenarioFact]
    public async Task A_clear_is_counted_and_its_fates_are_recorded_on_page_4_and_kept_in_every_slot()
    {
        var game = await AtTheEndAsync(5);
        game.State.F_Sill_Fate = 1;
        game.State.Units[2].Status = 1;
        game.Script.SaveResume = new ScriptRef("MAIN.ADV", 20);
        game.Script.SaveAll(1);
        game.Script.SaveAll(3);

        var quit = await RunOverAsync(game);

        Assert.Equal(0, quit.ExitCode.Value);                           // RetVal from the last key wait (:2081)
        Assert.Equal(1, Fate(game, Clears));
        Assert.Equal(1, Fate(game, Sill));
        Assert.Equal(1, Fate(game, Maria));
        foreach (int slot in new[] { 1, 3 })
        {
            var page = game.Script.Saves.Read(slot)!.Pages[4];
            Assert.Equal(1, page[Clears].Value);
            Assert.Equal(1, page[Sill].Value);
            Assert.Equal(1, page[Maria].Value);
        }
        Assert.Null(game.Script.Saves.Read(2));
        Assert.Contains(game.Calls, c => c.StartsWith("ﾛｰﾄﾞｾｰﾌﾞ.ADV", StringComparison.Ordinal));
        int report = MessageAt(game, 201), end = MessageAt(game, 2077), closing = MessageAt(game, 2080);
        Assert.True(report >= 0 && report < end && end < closing, $"report {report}, THE END {end}, Closing {closing}");
        Assert.DoesNotContain(game.View.Events, e => IsCg(e, 850));     // no staff roll for ending 5
    }

    /// <summary>
    /// The merge across plays (<c>L_02a10</c>): a fortune over a misfortune seen before, and a
    /// misfortune over a fortune, make 100; a fortune seen again stays 1; 100 stays 100. A second
    /// clear counts 2.
    /// </summary>
    [ScenarioFact]
    public async Task Fates_merge_with_those_of_earlier_plays()
    {
        var game = await AtTheEndAsync(5);
        var s = game.State;
        s.Fates[Clears].Fate = 1;
        s.F_Sill_Fate = 1;                                              // Sill's fortune over her misfortune
        s.Units[2].Status = 1;
        s.Fates[Sill].Fate = 10;
        s.F_Ending_LiaRoute = 2;                                        // Lia's misfortune over her fortune
        s.Fates[Lia].Fate = 1;
        s.Fates[Maria].Fate = 1;                                        // Maria's fortune again
        s.F_Satella_State = 12;                                         // Satella's misfortune after both
        s.Fates[Satella].Fate = 100;

        await RunOverAsync(game);

        Assert.Equal(2, Fate(game, Clears));
        Assert.Equal(100, Fate(game, Sill));
        Assert.Equal(100, Fate(game, Lia));
        Assert.Equal(1, Fate(game, Maria));
        Assert.Equal(100, Fate(game, Satella));
    }

    /// <summary>
    /// STR-30: ending 9 is a clear but shows no fortune (<c>Tmp0002</c>, <c>:15-17</c>) — Sill's is
    /// neither shown nor recorded — while the misfortunes are: Wendy's (her state below 2, any ending
    /// but 14) is.
    /// </summary>
    [ScenarioFact]
    public async Task Ending_9_counts_as_a_clear_but_records_no_fortune()
    {
        var game = await AtTheEndAsync(9);
        game.State.F_Sill_Fate = 1;
        game.State.Units[2].Status = 1;

        await RunOverAsync(game);

        Assert.Equal(1, Fate(game, Clears));
        Assert.Equal(0, Fate(game, Sill));
        Assert.Equal(10, Fate(game, Wendy));
        Assert.DoesNotContain(game.View.Events, e => e is MessageEvent { Text: { File: "OVER.ADV", Line: 212 } });
    }

    /// <summary>
    /// Ending 7 (Satella's) is no clear (STR-25) and has the staff roll: the credits' CGs 850-857, the
    /// credits music, the wait for it polled once (STR-26: <c>SR 1</c> is 0 at once). Satella's fortune has
    /// no <c>Tmp0002</c> guard (STR-29), so it shows although ending 7 turns the fortunes off. With
    /// <c>Tmp0020</c> left above 1 by earlier code (it counts the clears only in a clear), the staff
    /// roll is made skippable (<c>Y 3, 10001</c>, <c>:73-75</c>) on a first viewing.
    /// </summary>
    [ScenarioFact]
    public async Task Ending_7_is_no_clear_has_the_staff_roll_and_shows_Satella_s_unguarded_fortune()
    {
        var game = await AtTheEndAsync(7);
        game.State.F_Satella_State = 10;
        game.State.F_Sill_Fate = 1;
        game.State.Units[2].Status = 1;
        game.State.Tmp0020 = 5;

        await RunOverAsync(game);

        Assert.Equal(0, Fate(game, Clears));
        Assert.Equal(1, Fate(game, Satella));
        Assert.Equal(0, Fate(game, Sill));
        for (int cg = 850; cg <= 857; cg++)
            Assert.Contains(game.View.Events, e => IsCg(e, cg));
        Assert.Contains(game.View.Events, e => e is EngineEvent { Command: "SL" });
        Assert.True(game.Script.PauseCancelable);                      // STR-25
    }

    /// <summary>A first clear with the staff roll (ending 13) leaves its waits uncancelable (<c>Y 3, 10000</c>, <c>:72</c>).</summary>
    [ScenarioFact]
    public async Task A_first_clear_cannot_skip_the_staff_roll()
    {
        var game = await AtTheEndAsync(13);

        await RunOverAsync(game);

        Assert.Equal(1, Fate(game, Clears));
        Assert.Contains(game.View.Events, e => IsCg(e, 857));
        Assert.False(game.Script.PauseCancelable);
    }

    /// <summary>
    /// STR-29: Kouhime's fortune (ending 8, Kou at state 3, Isoroku (unit 27) at stage 6 and present) has
    /// no <c>Tmp0002</c> guard either: with the Kayblis war at 2, which turns the fortunes off, hers is
    /// still recorded and Sill's is not.
    /// </summary>
    [ScenarioFact]
    public async Task Kouhime_s_fortune_shows_when_the_fortunes_are_off()
    {
        var game = await AtTheEndAsync(8);
        var s = game.State;
        s.F_Kayblis_War = 2;
        s.F_Kou_State = 3;
        s.N_Isoroku_Stage = 6;
        s.Units[27].Status = 1;
        s.F_Sill_Fate = 1;
        s.Units[2].Status = 1;

        await RunOverAsync(game);

        Assert.Equal(1, Fate(game, Clears));
        Assert.Equal(1, Fate(game, Kouhime));
        Assert.Equal(0, Fate(game, Sill));
    }

    /// <summary>
    /// STR-28: an entry whose two conditions read different variables can show both texts — Wendy's
    /// fortune (her bedroom seen) and her misfortune (her state below 2) — and records the misfortune.
    /// </summary>
    [ScenarioFact]
    public async Task Both_texts_of_an_entry_can_show_and_the_misfortune_is_recorded()
    {
        var game = await AtTheEndAsync(5);
        game.State.F_Wendy_BedroomSeen = 1;

        await RunOverAsync(game);

        Assert.Equal(10, Fate(game, Wendy));
        Assert.True(MessageAt(game, 322) >= 0, "Wendy's fortune");
        Assert.True(MessageAt(game, 335) >= 0, "Wendy's misfortune");
    }

    /// <summary>STR-27: Cell's fortune prints "（Misfortune）" (<c>:1707</c>) and records a fortune.</summary>
    [ScenarioFact]
    public async Task Cell_s_fortune_reads_misfortune_but_records_a_fortune()
    {
        var game = await AtTheEndAsync(5);
        game.State.F_Cell_Arc = 5;
        game.State.F_Cell_ChurchArc = 1;

        await RunOverAsync(game);

        Assert.Equal(1, Fate(game, Cell));
        Assert.True(MessageAt(game, 1705) >= 0);
    }

    /// <summary>Sanakia's fate is Toushin City (27)'s owner: the city-states (3) a fortune, Leazas (1) a misfortune.</summary>
    [ScenarioTheory]
    [InlineData(3, 1)]
    [InlineData(1, 10)]
    [InlineData(8, 0)]
    public async Task Sanakia_s_fate_follows_Toushin_City_s_owner(int owner, int fate)
    {
        var game = await AtTheEndAsync(5);
        game.State.Provs[27].Owner = (Sys3Int)owner;

        await RunOverAsync(game);

        Assert.Equal(fate, Fate(game, Sanakia));
        Assert.Equal(27, game.State.Prov_Cur.Value);                    // the cursor is left there (:1671)
    }

    /// <summary>The booted new game with <c>EndingId</c> set.</summary>
    private static async Task<EndGame> AtTheEndAsync(int ending)
    {
        var game = await EndGame.BootAsync();
        game.State.EndingId = (Sys3Int)ending;
        return game;
    }

    /// <summary>Runs <c>OVER</c> to its <c>ZZ0</c>.</summary>
    private static Task<GameQuitException> RunOverAsync(EndGame game) =>
        Assert.ThrowsAsync<GameQuitException>(() => game.Script.CallFileAsync("OVER.ADV"));

    private static int Fate(EndGame game, int entry) => game.State.Fates[entry].Fate.Value;

    /// <summary>The index of the first message of <c>OVER.ADV</c> starting at <paramref name="line"/>, or −1.</summary>
    private static int MessageAt(EndGame game, int line) =>
        game.View.Events.FindIndex(e => e is MessageEvent { Text: { File: "OVER.ADV" } t } && t.Line == line);

    private static bool IsCg(ScriptEvent e, int cg) =>
        e is EngineEvent { Command: "G" } g && g.Args[0] == (Sys3Int)cg;
}
