using Kichikuou.Engine.Script;
using Kichikuou.Original.Golden;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Content.Tests;

/// <summary>
/// The gallery, <c>CG</c> (milestone 7 step 2; docs/strategy/survey-gallery.md §2), run from the
/// player's files on the booted title (<see cref="EndGame"/> without the new game) with a scripted
/// mouse. Its pages act on a left click on what the read before lit (§2.5), so each click follows
/// a hover. An entry is shown when its CG was loaded (<c>Glob_CgSeen</c> ≠ 0, STR-43); the others
/// keep the lock and ignore clicks. A picture's prompt goes back on a left click without closing its
/// window (G1) and replays the scene on a right click, from the scene's own file at its entry 300-304
/// (or text kept in <c>CG.ADV</c>), then the page is drawn again; Exit leaves <c>Ev_Entry</c> at 0.
/// Every key wait and pause is answered at once. Skipped without the decompile.
/// </summary>
public class GalleryTests
{
    private const string Gallery = "CG.ADV";

    /// <summary>The middle of thumbnail <paramref name="entry"/> (1-30) of a page (<c>CG.ADV:51-171</c>).</summary>
    private static (int X, int Y) Entry(int entry) => (55 + 106 * ((entry - 1) % 6), 69 + 72 * ((entry - 1) / 6));

    /// <summary>The middle of page <paramref name="page"/>'s tab (<c>:172-201</c>).</summary>
    private static (int X, int Y) Tab(int page) => (58 + 55 * (page - 1), 14);

    private static readonly (int X, int Y) Exit = (595, 12);

    private static async Task<EndGame?> AtTheTitleAsync(params int[] seen)
    {
        var game = await EndGame.BootAsync(newGame: false);
        if (game is null)
            return null;
        foreach (int cg in seen)
            game.State.CgSeen[cg].CgSeen = 1;
        return game;
    }

    /// <summary>A left click on what the pointer lit first (a hover, then the click).</summary>
    private static void Choose(EndGame game, (int X, int Y) at)
    {
        game.Input.Pointer.Hover(at.X, at.Y);
        game.Input.Pointer.Click(at.X, at.Y);
    }

    private static bool IsCg(ScriptEvent e, int cg) => e is EngineEvent { Command: "G" } g && g.Args[0].Value == cg;

    private static bool IsCopy(ScriptEvent e, params int[] args) =>
        e is EngineEvent { Command: "CC" } c && c.Args.Select(a => (int)a.Value).SequenceEqual(args);

    /// <summary>The lock tile stamped from (0, 400) onto a page sheet (<c>:3040-3044</c>), by destination.</summary>
    private static List<(int X, int Y)> LockStamps(EndGame game) =>
    [
        .. game.View.Events.OfType<EngineEvent>()
            .Where(e => e.Command == "CC" && e.Args[0].Value == 0 && e.Args[1].Value == 400 && e.Args[4].Value >= 640)
            .Select(e => ((int)e.Args[4].Value, (int)e.Args[5].Value)),
    ];

    /// <summary>
    /// The gallery opens silent (<c>SS 0</c>) with its five sheets and the lock tile, stamps the
    /// lock on every entry whose CG was never loaded — once, at the entry (G3) — and shows page 1. A
    /// locked entry is not lit and ignores a click; Exit waits for the release and returns with
    /// <c>Ev_Entry</c> 0.
    /// </summary>
    [Fact]
    public async Task Entries_never_loaded_keep_the_lock_and_ignore_a_click()
    {
        var game = await AtTheTitleAsync(101, 162, 862);
        if (game is null)
            return;
        game.State.Ev_Entry = 5;
        Choose(game, Entry(2));                                         // CG 102, never loaded
        Choose(game, Exit);

        await game.Script.CallFileAsync(Gallery);

        var events = game.View.Events;
        Assert.Contains(events, e => e is MusicEvent { Track.Value: 0 });
        foreach (int cg in new[] { 844, 845, 846, 847, 848, 849 })
            Assert.Contains(events, e => IsCg(e, cg));
        var stamps = LockStamps(game);
        Assert.Equal(147, stamps.Count);                                // 150 entries, 3 loaded
        Assert.DoesNotContain((1923, 440), stamps);                     // page 1 (at 1920, 400), entry 1: CG 101
        Assert.Contains((2029, 440), stamps);                           // entry 2: CG 102
        Assert.Contains(events, e => IsCopy(e, 1920, 400, 640, 400, 0, 0));
        Assert.DoesNotContain(events, e => IsCg(e, 806));               // no picture opened
        Assert.DoesNotContain(events, e => e is WindowEvent);
        Assert.Equal(0, game.State.Ev_Entry.Value);
        Assert.Equal(0, game.Input.Pointer.Pending);
        Assert.Equal(["CG.ADV 5"], game.Calls);
    }

    /// <summary>
    /// A loaded entry opens its picture (the backdrop CG 806, the event CG, the full-screen window
    /// 131 and the prompt); a left click goes back to the page without closing the window (G1:
    /// <c>Ui_WinType</c> stays 31), the page drawn again (<c>G 841</c>, <c>SS 0</c>, the sheet).
    /// </summary>
    [Fact]
    public async Task A_left_click_at_the_prompt_goes_back_without_closing_the_window()
    {
        var game = await AtTheTitleAsync(101);
        if (game is null)
            return;
        Choose(game, Entry(1));
        game.Input.Pointer.Click(300, 200);
        Choose(game, Exit);

        await game.Script.CallFileAsync(Gallery);

        var events = game.View.Events;
        int picture = events.FindIndex(e => IsCg(e, 101));
        int window = events.FindIndex(e => e is WindowEvent { Op.Value: 131 });
        int redraw = events.FindIndex(e => IsCg(e, 841));
        Assert.True(events.FindIndex(e => IsCg(e, 806)) is var backdrop && backdrop >= 0 && backdrop < picture);
        Assert.True(picture < window && window < redraw, $"picture {picture}, window {window}, page {redraw}");
        Assert.Contains(events.Skip(redraw), e => IsCopy(e, 1920, 400, 640, 400, 0, 0));
        Assert.DoesNotContain(events, e => e is WindowEvent { Op.Value: 0 });
        Assert.Equal(31, game.State.Ui_WinType.Value);                  // G1: the window is left open
        Assert.DoesNotContain(game.Calls, c => c.StartsWith('G') || c.StartsWith('I'));
        Assert.Equal(0, game.State.Ev_Entry.Value);
    }

    /// <summary>
    /// A right click at the prompt replays the scene: <c>Gｼｨﾙ</c> entered at entry 300 from the
    /// player's files, which leaves at its own guard; the window closed (<c>WAO_SUB</c> 0), the page
    /// drawn again, and nothing of the world changed (§2.4: EVT-07 does not hold) — no map, no other
    /// file. The CG shown counts again (STR-43).
    /// </summary>
    [Fact]
    public async Task A_right_click_replays_the_scene_from_its_entry_and_comes_back()
    {
        var game = await AtTheTitleAsync(101);
        if (game is null)
            return;
        var before = World(game.State);
        Choose(game, Entry(1));
        game.Input.Pointer.RightClick(300, 200);
        Choose(game, Exit);

        await game.Script.CallFileAsync(Gallery);

        Assert.Contains("Gｼｨﾙ.ADV 300", game.Calls);
        Assert.All(game.Calls, c => Assert.True(
            c is "CG.ADV 0" or "Gｼｨﾙ.ADV 300" || c.StartsWith("WAO_SUB.ADV", StringComparison.Ordinal), c));
        var events = game.View.Events;
        int replay = events.FindIndex(e => e is MessageEvent { Text.File: "Gｼｨﾙ.ADV" });
        int closed = events.FindLastIndex(e => e is WindowEvent { Op.Value: 0 });
        int redraw = events.FindLastIndex(e => IsCg(e, 841));
        Assert.True(replay >= 0 && replay < closed && closed < redraw, $"replay {replay}, closed {closed}, page {redraw}");
        Assert.DoesNotContain(events.Skip(events.FindIndex(e => IsCg(e, 844))), e => e is MapEvent or WaoMapEvent);
        Assert.Equal(0, game.State.Ui_WinType.Value);
        Assert.Equal(0, game.State.Ev_Entry.Value);
        Assert.Equal(2, game.State.CgSeen[101].CgSeen.Value);
        Assert.Equal(before, World(game.State));
    }

    /// <summary>
    /// A tab shows its page (page 2's sheet at (640, 400), <c>:323-326</c>); its first entry opens
    /// CG 131. Entry 3, CG 133, replays text kept in <c>CG.ADV</c> itself (<c>L_03453</c>, G7) and
    /// calls no scene.
    /// </summary>
    [Fact]
    public async Task A_tab_shows_its_page_and_an_entry_there_may_replay_text_kept_in_the_gallery()
    {
        var game = await AtTheTitleAsync(131, 133);
        if (game is null)
            return;
        Choose(game, Tab(2));
        Choose(game, Entry(1));
        game.Input.Pointer.Click(300, 200);
        Choose(game, Entry(3));
        game.Input.Pointer.RightClick(300, 200);
        Choose(game, Exit);

        await game.Script.CallFileAsync(Gallery);

        var events = game.View.Events;
        int page2 = events.FindIndex(e => IsCopy(e, 640, 400, 640, 400, 0, 0));
        Assert.True(page2 >= 0 && page2 < events.FindIndex(e => IsCg(e, 131)));
        Assert.Contains(events, e => IsCg(e, 133));
        Assert.Contains(events, e => e is MessageEvent { Text.File: Gallery } m && m.Text.Line > 2350);
        Assert.DoesNotContain(game.Calls, c => c.StartsWith('G') || c.StartsWith('I'));
        Assert.Equal(0, game.State.Ui_WinType.Value);
    }

    /// <summary>Every variable of the story's groups and every page slot but the CG load counts.</summary>
    private static List<int> World(GameState s)
    {
        string[] groups = ["story", "history", "time", "economy", "item", "prov", "cmdr", "event"];
        var world = VariableCatalog.Variables
            .Select((v, n) => (v.Group, n))
            .Where(x => groups.Contains(x.Group) && VariableCatalog.Variables[x.n].Name != "Ev_Entry")
            .Select(x => (int)s.Memory.Var(x.n).Value)
            .ToList();
        foreach (var p in VariableCatalog.Pages)
            for (int i = 0; i < p.Size; i++)
                if (!GoldenFilter.Strategic.IgnoresSlot(p.Page, i))
                    world.Add(s.Memory.ReadPage(p.Page, i).Value);
        return world;
    }
}
