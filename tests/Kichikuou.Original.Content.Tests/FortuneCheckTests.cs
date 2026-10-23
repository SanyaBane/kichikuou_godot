using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Fortune Check, <c>HAPPY_C</c> (milestone 6 step 3; docs/strategy/survey-w4.md §4), run from the
/// player's files: from the title (its button 0, unlocked by a clear) with a scripted mouse — the
/// two pages of <c>Glob_Fate[0..65]</c> with a stamp for each fate seen, a cell's detail, the page
/// flip and the way back — or on its own for the hover state's start (STR-39). The script tests the
/// button before the pointer's place (<c>:50-72</c>), so a click acts on what the read before it
/// highlighted: each click here follows a hover (<see cref="PointerScript.Hover"/>). A run ends
/// where the title (or Fortune Check) polls the mouse again with every gesture done. Skipped
/// without the decompile.
/// </summary>
public class FortuneCheckTests
{
    private const string Fortune = "HAPPY_C.ADV";
    private const string Title = "ｽﾀMENU.ADV";
    private const int Clears = 79;

    /// <summary>The title's Fortune Check button, the middle of its box (<c>ｽﾀMENU.ADV:805</c>).</summary>
    private static readonly (int X, int Y) Button = (120, 77);

    /// <summary>The page flip (98) and the way back (99) under the grid (<c>:66-71</c>).</summary>
    private static readonly (int X, int Y) Flip = (491, 379), Back = (579, 379);

    /// <summary>
    /// The middle of cell <paramref name="i"/> (0-32) of a page: three columns 150 wide from x 15,
    /// 223 and 431 (<c>:57-65</c>), eleven rows 32 high from y 11 (<c>:136-191</c>).
    /// </summary>
    private static (int X, int Y) Cell(int i) => (89 + i % 3 * 208, 27 + i / 3 * 32);

    /// <summary>Menad (fate 7): her portrait (CG 8) and her six lines (<c>:505-519</c>).</summary>
    private const int Menad = 7, MenadCg = 8;

    private static readonly string[] MenadFortune = ["Freed herself of Zarak, the", "man that manipulated her, and", "moved on with her life."];
    private static readonly string[] MenadMisfortune = ["When the world was in danger,", "Zarak betrayed her and she", "learned she’d been deceived."];

    /// <summary>
    /// Fortune Check from the title: the pages (CG 837 at (640, 0), 838 at (1280, 0)), the stamps
    /// (CG 841 at (0, 400): both at x 0, fortune 40, misfortune 80) copied for each fate seen — at
    /// x 166, 374, 582 and y 14 + 32 per row of its page (<c>:258-296</c>) — the detail's sheet (CG
    /// 839 at (640, 400)) and page 1 shown. A right click anywhere (<c>:54-55</c>) goes back after its
    /// release (<c>:84-89</c>, <c>%0</c>), and the title is drawn again (<c>ｽﾀMENU.ADV:236</c>).
    /// No random draw but the title's two rolls on each of its passes, and page 4 is not written.
    /// </summary>
    [ScenarioFact]
    public async Task Fortune_Check_stamps_each_fate_seen_on_its_page_and_goes_back_to_the_title()
    {
        var game = await EndGame.BootAsync(newGame: false);
        var s = game.State;
        s.Fates[Clears].Fate = 1;
        s.Fates[0].Fate = 100;                                          // Sill: both
        s.Fates[Menad].Fate = 1;                                        // fortune
        s.Fates[32].Fate = 10;                                          // Chizuko: misfortune, page 1's last cell
        s.Fates[33].Fate = 100;                                         // Magic: page 2's first
        s.Fates[65].Fate = 1;                                           // Seizel & Hawzel: page 2's last
        int[] fates = [.. Enumerable.Range(0, 80).Select(i => (int)s.Fates[i].Fate.Value)];
        var during = Watch(game);
        game.Input.Pointer.Click(Button.X, Button.Y);
        game.Input.Pointer.RightClick(300, 200);

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        Assert.Contains($"{Fortune} 0", game.Calls);
        var events = during();
        foreach (var (cg, x, y) in new[] { (837, 640, 0), (838, 1280, 0), (841, 0, 400), (839, 640, 400) })
            Assert.True(DrawnAt(events, cg, x, y), $"CG {cg} at ({x}, {y})");
        Assert.Equal(
            [(0, 806, 14), (40, 1014, 78), (80, 1222, 334), (0, 1446, 14), (40, 1862, 334)],
            Copies(events).Where(c => c.W == 40 && c.H == 20).Select(c => (c.SrcX, c.DstX, c.DstY)));
        Assert.Equal([(640, 0, 640, 400, 0, 0)], Copies(events).Where(c => c.W == 640));   // page 1 shown
        Assert.Equal(2, game.View.Events.Count(e => IsCg(e, 301)));     // the title before and after
        Assert.Equal([10, 20, 10, 20], game.Random.Draws);
        Assert.Equal(fates, Enumerable.Range(0, 80).Select(i => (int)s.Fates[i].Fate.Value));
    }

    /// <summary>
    /// A cell's detail (<c>:297-381</c>): the sheet copied to (0, 400), the portrait at (16, 416)
    /// keyed by 40 — for a fate not yet seen too (STR-40) — the fortune's three lines at y 492, 523, 554
    /// and the misfortune's at 652, 683, 714 from x 246, a half not yet seen printed "Undiscovered"
    /// (the two halves of fate 0 spaced unlike the single ones', <c>:310-342</c>), then the whole
    /// shown at once, a key, and the page shown again.
    /// </summary>
    [ScenarioTheory]
    [InlineData(100, true, true)]
    [InlineData(1, true, false)]
    [InlineData(10, false, true)]
    [InlineData(0, false, false)]
    public async Task A_cell_shows_the_fortune_and_misfortune_seen_and_Undiscovered_for_the_rest(int fate, bool fortune, bool misfortune)
    {
        var game = await EndGame.BootAsync(newGame: false);
        var s = game.State;
        s.Fates[Clears].Fate = 1;
        s.Fates[Menad].Fate = (Sys3Int)fate;
        var printed = new List<(int X, int Y, string Text)>();
        (int X, int Y) at = (0, 0);
        int keyAt = -1, cursor = -1;
        bool inside = false;                                            // the title prints blanks too (ｽﾀMENU.ADV:8-38)
        game.OnCall = file => inside |= file == Fortune;
        game.View.OnShow = e =>
        {
            if (IsCg(e, 301))
                inside = false;
            if (!inside)
                return;
            if (e is EngineEvent { Command: "T" } t)
                at = (t.Args[0].Value, t.Args[1].Value);
            if (e is MessageEvent { Text: { File: Fortune, Line: >= 351 and <= 366 } m })   // X 1 … X 6, a run each (:351-366)
                printed.Add((at.X, at.Y, s.Strings[(m.Line - 348) / 3]));
            if (e is KeyWaitEvent && keyAt < 0)
            {
                keyAt = game.View.Events.Count - 1;
                cursor = s.Glob_FateCur.Value;
            }
        };
        game.Input.Pointer.Click(Button.X, Button.Y);
        var cell = Cell(Menad);
        game.Input.Pointer.Hover(cell.X, cell.Y);
        game.Input.Pointer.Click(cell.X, cell.Y);
        game.Input.Pointer.RightClick(300, 200);

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        string[] undiscoveredFortune = fate == 0 ? ["　", "　　Undiscovered", "　"] : ["　", "　Undiscovered　", "　"];
        string[] undiscoveredMisfortune = fate == 0 ? ["　", "　　Undiscovered　", "　"] : ["　", "　Undiscovered　", "　"];
        string[] lines = [.. fortune ? MenadFortune : undiscoveredFortune, .. misfortune ? MenadMisfortune : undiscoveredMisfortune];
        Assert.Equal(lines.Zip(new[] { 492, 523, 554, 652, 683, 714 }, (text, y) => (246, y, text)), printed);
        Assert.Equal(Menad, cursor);
        var detail = game.View.Events.Take(keyAt).ToList();
        int portrait = detail.FindLastIndex(e => IsCg(e, MenadCg));
        Assert.True(portrait > 0, "the portrait drawn");                // STR-40: for a fate not yet seen too
        Assert.Equal([MenadCg, 40], ((EngineEvent)detail[portrait]).Args.Select(a => (int)a.Value));
        Assert.True(detail[portrait - 1] is EngineEvent { Command: "J0" } j && j.Args[0].Value == 16 && j.Args[1].Value == 416, "the portrait at (16, 416)");
        Assert.Equal((0, 400, 640, 400, 0, 0), Copies(detail).Last());   // :367, the detail shown
        var after = game.View.Events.Skip(keyAt).OfType<EngineEvent>().First(e => e.Command == "CC");
        Assert.Equal([640, 0, 640, 400, 0, 0], after.Args.Select(a => (int)a.Value));   // :375, page 1 again
        Assert.Empty(game.Unported.Calls);
    }

    /// <summary>
    /// The page flip (98, <c>:226-257</c>) shows page 2 (CG 838's copy at (1280, 0)); a cell there opens
    /// fate 33 on (<c>:303-305</c>: Nagi (fate 34), CG 45); 98 again goes back to page 1, and 99 back to
    /// the title (<c>:73-89</c>), each after its release.
    /// </summary>
    [ScenarioFact]
    public async Task The_page_flips_both_ways_and_the_back_button_leaves()
    {
        var game = await EndGame.BootAsync(newGame: false);
        var s = game.State;
        s.Fates[Clears].Fate = 1;
        s.Fates[34].Fate = 100;
        string? first = null;
        game.View.OnShow = e =>
        {
            if (e is KeyWaitEvent)
                first ??= s.Strings[1];
        };
        var during = Watch(game);
        var p = game.Input.Pointer;
        p.Click(Button.X, Button.Y);
        p.Hover(Flip.X, Flip.Y);
        p.Click(Flip.X, Flip.Y);
        p.Hover(Cell(1).X, Cell(1).Y);
        p.Click(Cell(1).X, Cell(1).Y);
        p.Hover(Flip.X, Flip.Y);
        p.Click(Flip.X, Flip.Y);
        p.Hover(Back.X, Back.Y);
        p.Click(Back.X, Back.Y);

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Title));

        var events = during();
        var pages = Copies(events).Where(c => c is { SrcY: 0, W: 640, H: 400, DstX: 0, DstY: 0 }).Select(c => c.SrcX);
        Assert.Equal([640, 1280, 1280, 640], pages);                    // shown, flipped, after the detail, flipped back
        Assert.Contains(events, e => IsCg(e, 45));
        Assert.Equal("She was chosen by Rance and", first);
        Assert.Equal(2, game.View.Events.Count(e => IsCg(e, 301)));
        Assert.Contains(events, e => e is EngineEvent { Command: "CB" } b && b.Args[0].Value == 536 && b.Args[2].Value == 87);   // 99 lit
    }

    /// <summary>
    /// Every cell opens its own fate: page 1's 33 cells fates 0-32 row by row (<c>Tmp0009</c>, the
    /// column + 3 × the row, <c>:124-191</c>), page 2's fates 33-65 (<c>:303-305</c>), each with its one
    /// portrait (<c>:392-1448</c>): the one <c>OVER</c> sets before an entry's tests (eleven entries
    /// change it for one half), but for STR-41 — Amiran (fate 22) 39, OVER's 40 (39 her misfortune's);
    /// Somita (54) 46, OVER's 47 (48 her misfortune's; 46 OVER never draws); Pastel (55) 41, OVER's
    /// 493 (41 her misfortune's).
    /// </summary>
    [ScenarioFact]
    public async Task Every_cell_opens_its_own_fate_with_its_portrait()
    {
        var game = await EndGame.BootAsync(newGame: false);
        var s = game.State;
        var opened = new List<(int Fate, int Portrait)>();
        int portrait = 0;
        game.View.OnShow = e =>
        {
            if (e is EngineEvent { Command: "G" } g && g.Args.Count == 2)
                portrait = g.Args[0].Value;
            if (e is KeyWaitEvent)
                opened.Add((s.Glob_FateCur.Value, portrait));
        };
        var p = game.Input.Pointer;
        for (int page = 0; page < 2; page++)
        {
            for (int i = 0; i < 33; i++)
            {
                p.Hover(Cell(i).X, Cell(i).Y);
                p.Click(Cell(i).X, Cell(i).Y);
            }
            p.Hover(Flip.X, Flip.Y);
            p.Click(Flip.X, Flip.Y);
        }

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Fortune));

        int[] portraits =
        [
            2, 5, 6, 37, 36, 9, 7, 8, 10, 11, 4, 15, 50, 49, 66, 67, 68, 32, 12, 63, 65, 22, 39, 28, 29, 75, 81, 78, 77, 26, 17, 18, 43,
            58, 45, 33, 72, 71, 38, 24, 16, 25, 460, 52, 99, 79, 27, 76, 457, 62, 86, 57, 64, 14, 46, 41, 44, 100, 97, 449, 83, 54, 249, 56, 424, 428,
        ];
        Assert.Equal(Enumerable.Range(0, 66).Select(f => (f, portraits[f])), opened);
    }

    /// <summary>
    /// STR-39: the hover state starts at 0, not 100 (<c>:45</c>, and again after a page flip, <c>:255</c>),
    /// so a button already down at the first read (pressed again during the fade-in, the title having
    /// waited for its release, <c>ｽﾀMENU.ADV:131-134</c>) opens fate 0 — or 33 after a flip — wherever
    /// the pointer is: here over the page flip, then over a cell of page 2 (fate 38).
    /// </summary>
    [ScenarioFact]
    public async Task A_press_at_the_first_read_opens_fate_0_or_33_wherever_the_pointer_is()
    {
        var game = await EndGame.BootAsync(newGame: false);
        var s = game.State;
        var opened = new List<int>();
        game.View.OnShow = e =>
        {
            if (e is KeyWaitEvent)
                opened.Add(s.Glob_FateCur.Value);
        };
        var p = game.Input.Pointer = new PointerScript { SettleReads = 0 };
        p.Click(Flip.X, Flip.Y);                                        // the first read: fate 0, not the flip
        p.Hover(Flip.X, Flip.Y);                                        // the detail's release wait (:370)
        p.Hover(Flip.X, Flip.Y);                                        // the flip lit
        p.Click(Flip.X, Flip.Y);                                        // now the flip: page 2, the state 0 again
        p.Click(Cell(5).X, Cell(5).Y);                                  // the first read after it: fate 33, not 38

        await Assert.ThrowsAsync<PointerIdleException>(() => game.Script.CallFileAsync(Fortune));

        Assert.Equal([0, 33], opened);
        Assert.Contains(game.View.Events, e => IsCg(e, 2));             // Sill's portrait
        Assert.Contains(game.View.Events, e => IsCg(e, 58));            // Magic's
    }

    /// <summary>The events Fortune Check shows: from its call up to the title's next background (CG 301).</summary>
    private static Func<List<ScriptEvent>> Watch(EndGame game)
    {
        int start = -1;
        game.OnCall = file =>
        {
            if (file == Fortune)
                start = game.View.Events.Count;
        };
        return () =>
        {
            Assert.True(start >= 0, "Fortune Check was called");
            var rest = game.View.Events.Skip(start).ToList();
            int end = rest.FindIndex(e => IsCg(e, 301));
            return end < 0 ? rest : rest[..end];
        };
    }

    private static List<(int SrcX, int SrcY, int W, int H, int DstX, int DstY)> Copies(IEnumerable<ScriptEvent> events) =>
    [
        .. events.OfType<EngineEvent>().Where(e => e.Command == "CC")
            .Select(e => (e.Args[0].Value, e.Args[1].Value, e.Args[2].Value, e.Args[3].Value, e.Args[4].Value, e.Args[5].Value)),
    ];

    /// <summary>Whether CG <paramref name="cg"/> was drawn right after a <c>J0</c> to (<paramref name="x"/>, <paramref name="y"/>).</summary>
    private static bool DrawnAt(List<ScriptEvent> events, int cg, int x, int y)
    {
        for (int i = 1; i < events.Count; i++)
            if (IsCg(events[i], cg) && events[i - 1] is EngineEvent { Command: "J0" } j && j.Args[0].Value == x && j.Args[1].Value == y)
                return true;
        return false;
    }

    private static bool IsCg(ScriptEvent e, int cg) =>
        e is EngineEvent { Command: "G" } g && g.Args[0] == (Sys3Int)cg;
}
