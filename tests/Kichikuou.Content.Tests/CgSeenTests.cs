using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The engine's CG load counter on the player's files (milestone 7 step 1, owner's decision 21,
/// STR-43): the boot binds it (<c>SET.ADV:297</c> <c>ZG Glob_CgSeen</c>); the CGs the title and the
/// prologue load — by <c>G</c> in the scenes and by the window helper's pictures — count on page 4,
/// and <c>ﾛｰﾄﾞｾｰﾌﾞ</c> 11 and 10 carry the counts to the slots and back (STR-31). The other paths
/// (the map, the battle, the pickers) are in the core's tests. Skipped without the decompile.
/// </summary>
public class CgSeenTests
{
    private const string LoadSave = "ﾛｰﾄﾞｾｰﾌﾞ.ADV";

    private static int Seen(EndGame game, int cg) => game.State.CgSeen[cg].CgSeen.Value;

    /// <summary>How many times each CG was loaded by a <c>G</c>, by CG (<c>G 0</c> loads nothing).</summary>
    private static Dictionary<int, int> GLoads(EndGame game) =>
        game.View.Events.OfType<EngineEvent>()
            .Where(e => e.Command == "G" && e.Args[0].Value != 0)
            .GroupBy(e => (int)e.Args[0].Value)
            .ToDictionary(g => g.Key, g => g.Count());

    /// <summary>
    /// The boot binds the counter at <c>Glob_CgSeen</c>'s record 0 and loads nothing; the title's
    /// <c>G</c>s, run by <c>SceneRunner</c> — the logo, the title's sheets — count once each, and CG 240
    /// is the one the title marks itself (<c>ｽﾀMENU.ADV:84-85</c>).
    /// </summary>
    [Fact]
    public async Task The_boot_binds_the_counter_and_each_G_of_the_title_counts_once()
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return;
        Assert.Equal((4, 80), game.Script.CgLoadCounter);
        Assert.All(Enumerable.Range(0, 2000), cg => Assert.Equal(0, Seen(game, cg)));

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync("ｽﾀMENU.ADV"));

        var loads = GLoads(game);
        Assert.Contains(1700, loads.Keys);                              // the logo
        Assert.Contains(301, loads.Keys);                               // the title
        Assert.DoesNotContain(240, loads.Keys);
        loads[240] = 1;
        Assert.All(Enumerable.Range(0, 2000), cg => Assert.Equal(loads.GetValueOrDefault(cg), Seen(game, cg)));
    }

    /// <summary>
    /// The prologue (a new game's <c>Iﾌﾟﾛﾛｰｸﾞ</c>, run from the player's files) counts every CG its
    /// scenes load by <c>G</c>, every portrait and centre picture its windows show (<c>WAO_SUB</c> 311,
    /// 321, 331, 333 with the CG in <c>CallArg</c>) and the frames of the windows it opens.
    /// </summary>
    [Fact]
    public async Task The_prologue_counts_its_scenes_G_and_its_window_pictures()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;

        var windows = game.View.Events.OfType<WindowEvent>().ToList();
        var pictures = windows.Where(w => w.Op.Value is 311 or 321 or 331 or 333 && w.CallArg != 0)
            .Select(w => (int)w.CallArg.Value).ToHashSet();
        Assert.NotEmpty(pictures);
        foreach (int cg in pictures)
            Assert.True(Seen(game, cg) > 0, $"window picture {cg} not counted");
        foreach (var (cg, count) in GLoads(game))
            Assert.True(Seen(game, cg) >= count, $"CG {cg}: {Seen(game, cg)} for {count} G");
        var frames = new Dictionary<int, int> { [111] = 807, [112] = 808, [113] = 809, [121] = 810, [122] = 811, [131] = 815, [143] = 894 };
        foreach (var w in windows.Where(w => frames.ContainsKey(w.Op.Value)))
            Assert.True(Seen(game, frames[w.Op.Value]) > 0, $"frame of window {w.Op} not counted");
    }

    /// <summary>
    /// <c>ﾛｰﾄﾞｾｰﾌﾞ</c> 11 writes page 4, the counts with it, into every slot that holds a save (1 and
    /// 3, not the empty 2); <c>ﾛｰﾄﾞｾｰﾌﾞ</c> 10 reads them back (<c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:16-23</c>).
    /// </summary>
    [Fact]
    public async Task The_counts_reach_every_slot_that_holds_a_save_and_come_back_from_them()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;
        game.Script.SaveResume = new ScriptRef("MAIN.ADV", 20);
        game.Script.SaveAll(1);
        game.Script.SaveAll(3);
        const int Cg = 101;                                             // the gallery's first picture
        int before = Seen(game, Cg);
        await game.Script.EngineAsync("G", [(Sys3Int)Cg]);
        Assert.Equal(before + 1, Seen(game, Cg));

        game.State.RetVal = 11;
        await game.Script.CallFileAsync(LoadSave);

        foreach (int slot in new[] { 1, 3 })
            Assert.Equal(before + 1, game.Script.Saves.Read(slot)!.Pages[4][80 + Cg].Value);
        Assert.Null(game.Script.Saves.Read(2));

        game.State.CgSeen[Cg].CgSeen = 0;
        game.State.RetVal = 10;
        await game.Script.CallFileAsync(LoadSave);

        Assert.Equal(before + 1, Seen(game, Cg));
    }
}
