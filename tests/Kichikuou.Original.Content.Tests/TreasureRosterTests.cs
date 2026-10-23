using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Our own Treasure screen (docs/ui/pickers.md, round V): <see cref="TreasureRoster"/> on hand-made states, and its
/// gestures answering <c>Cﾀｶﾗ</c> on the booted new game — the picker still hit-tests the clicks, asks and equips. The
/// script tests are skipped without the decompile.
/// </summary>
public class TreasureRosterTests(ITestOutputHelper output)
{
    /// <summary>Rance (unit 1) with Chaos (1), Barres (unit 6) with Sword (6), Rick (unit 7) deployed, a captive (unit 9); Ring (7) in stock, Stone (8) without effect.</summary>
    private static GameState State()
    {
        var s = new GameState { Unit_StrBase = 20, Item_StrBase = 300 };
        void Unit(int n, string name, int status, int item)
        {
            s.Strings[20 + (n - 1) * 3] = name;
            s.Units[n].Status = (Sys3Int)status;
            s.Units[n].Item = (Sys3Int)item;
            if (item != 0)
                s.Items[item].Holder = (Sys3Int)n;
        }
        void Item(int n, string name, int effect)
        {
            s.Strings[300 + n - 1] = name;
            s.Items[n].Effect = (Sys3Int)effect;
        }
        Item(1, "Chaos", 1);
        Item(6, "Sword", 1);
        Item(7, "Ring", 2);
        Item(8, "Stone", 0);
        Unit(1, "Rance", 1, 1);
        Unit(6, "Barres", 1, 6);
        Unit(7, "Rick", 2, 0);
        Unit(9, "Captive", 3, 0);
        s.Items[7].Holder = 100;
        s.Items[8].Holder = 100;
        return s;
    }

    [Fact]
    public void The_characters_view_lists_every_unit_with_a_status_grey_but_at_1_and_2()
    {
        var s = State();

        var entries = TreasureRoster.Entries(s);

        Assert.Equal([1, 6, 7, 9], entries.Select(e => e.Number));
        Assert.Equal(new TreasureEntry(1, "Rance", "", TreasureInk.Normal, true, true), entries[0]);
        Assert.Equal(new TreasureEntry(6, "Barres", "", TreasureInk.Normal, false, true), entries[1]);
        Assert.Equal(new TreasureEntry(9, "Captive", "", TreasureInk.Off, false, false), entries[3]);
    }

    [Fact]
    public void The_items_view_lists_the_items_owned_inked_by_where_they_are()
    {
        var s = State();
        s.Ui_ItemViewMode = 1;

        var entries = TreasureRoster.Entries(s);

        Assert.Equal([1, 6, 7, 8], entries.Select(e => e.Number));
        Assert.Equal([TreasureInk.Held, TreasureInk.Held, TreasureInk.Normal, TreasureInk.Off], entries.Select(e => e.Ink));
        Assert.Equal(["Rance", "Barres", "", ""], entries.Select(e => e.Holder));
        Assert.Equal([true, false, false, false], entries.Select(e => e.Fixed));
    }

    [Fact]
    public void The_details_are_the_original_s_line()
    {
        var s = State();
        var u = s.Units[6];
        (u.PortraitCg, u.DeployCost, u.Troops, u.Class) = (606, 8, 918, 0);

        var unit = TreasureRoster.Details(s, 6)!;

        Assert.Equal((606, 600, "Barres", "Assault Troops"), (unit.PictureCg, unit.EmptyCg, unit.Name, unit.Subtitle));
        Assert.Equal(["War cost 800K", "Troops 918", "Equipped Sword"], unit.Figures.Select(f => $"{f.Label} {f.Value}"));
        Assert.Null(TreasureRoster.Details(s, 8));

        s.Ui_ItemViewMode = 1;
        Assert.Equal(["Held by Barres"], TreasureRoster.Details(s, 6)!.Figures.Select(f => $"{f.Label} {f.Value}"));
        Assert.Equal(["Held by Nobody (in stock)"], TreasureRoster.Details(s, 7)!.Figures.Select(f => $"{f.Label} {f.Value}"));
        var stone = TreasureRoster.Details(s, 8)!;
        Assert.Equal((758, 692, "Can't be equipped", true), (stone.PictureCg, stone.EmptyCg, stone.Subtitle, stone.SubtitleOff));
    }

    [Fact]
    public void The_prompt_follows_the_first_pick()
    {
        var s = State();
        s.Units[6].FaceCg = 506;
        Assert.Null(TreasureRoster.Prompt(s));
        s.Ui_ItemUnitSel = 6;
        Assert.Equal(new TreasurePrompt(true, 6, "Barres", 506), TreasureRoster.Prompt(s));
        s.Ui_ItemUnitSel = 0;
        s.Ui_ItemSel = 7;
        Assert.Equal(new TreasurePrompt(false, 7, "Ring", 0), TreasureRoster.Prompt(s));
    }

    [Fact]
    public void An_item_picked_first_asks_before_it_replaces_a_unit_s_own()
    {
        var s = State();
        Assert.Null(TreasureRoster.ReplaceQuestion(s, 6));                // nothing picked first
        s.Ui_ItemSel = 7;

        Assert.Equal(["Barres already has Sword equipped.", "Replace it with Ring?"], TreasureRoster.ReplaceQuestion(s, 6));
        Assert.Null(TreasureRoster.ReplaceQuestion(s, 7));                // Rick holds nothing
        Assert.Null(TreasureRoster.ReplaceQuestion(s, 1));                // Chaos: the script refuses
        Assert.Null(TreasureRoster.ReplaceQuestion(s, 9));                // a captive: no pick
        s.Ui_ItemViewMode = 1;
        Assert.Null(TreasureRoster.ReplaceQuestion(s, 6));
    }

    /// <summary>
    /// The booted game with Rance (unit 1) and the next unit at status 1, two items with an effect: the first in stock,
    /// the second on the other unit; the picker's view <paramref name="view"/>.
    /// </summary>
    private static async Task<(EndGame Game, int Other, int Stock, int Held)> GameAsync(int view)
    {
        var game = await EndGame.BootAsync();
        var s = game.State;
        s.StoryStage = 1;
        s.Week = 18;
        int[] effective = [.. Enumerable.Range(3, 48).Where(n => s.Items[n].Effect != 0).Take(2)];
        int other = Enumerable.Range(2, 49).First(n => s.Units[n].Status != 0);
        foreach (int n in Enumerable.Range(1, 50))
        {
            s.Items[n].Holder = 0;
            s.Units[n].Item = 0;
        }
        s.Units[1].Status = 1;
        s.Units[other].Status = 1;
        s.Items[effective[0]].Holder = 100;
        s.Items[effective[1]].Holder = (Sys3Int)other;
        s.Units[other].Item = (Sys3Int)effective[1];
        s.Ui_ItemViewMode = (Sys3Int)view;
        return (game, other, effective[0], effective[1]);
    }

    /// <summary>Runs <c>Cﾀｶﾗ</c>: each of <paramref name="steps"/> once the pointer is idle, then End.</summary>
    private static async Task RunAsync(EndGame game, params Action<GameState>[] steps)
    {
        var left = new Queue<Action<GameState>>(steps);
        bool ended = false;
        game.Input.Idle = () =>
        {
            if (left.TryDequeue(out var step))
                step(game.State);
            else if (!ended)
            {
                ended = true;
                TreasureRoster.End(game.Input.Pointer);
            }
        };
        await game.Script.CallFileAsync("Cﾀｶﾗ.ADV");
        Assert.True(ended);
    }

    [ScenarioFact]
    public async Task A_unit_then_an_item_equips_it_and_the_unit_s_own_goes_back_to_stock()
    {
        var (game, other, stock, held) = await GameAsync(0);
        var s = game.State;
        var pointer = game.Input.Pointer;
        TreasurePrompt? prompt = null;

        await RunAsync(game,
            _ => TreasureRoster.Pick(other, pointer),
            st =>
            {
                prompt = TreasureRoster.Prompt(st);
                TreasureRoster.Pick(stock, pointer);
            });

        output.WriteLine($"unit {other}: item {s.Units[other].Item.Value}; view {s.Ui_ItemViewMode.Value}");
        Assert.Equal(new TreasurePrompt(true, other, DeploymentTable.UnitName(s, other), s.Units[other].FaceCg.Value), prompt);
        Assert.Equal(stock, s.Units[other].Item.Value);
        Assert.Equal(other, s.Items[stock].Holder.Value);
        Assert.Equal(100, s.Items[held].Holder.Value);
        Assert.Empty(game.Input.Menus);
    }

    [ScenarioFact]
    public async Task Turning_the_view_and_an_item_then_a_unit_equips_it_too()
    {
        var (game, _, stock, _) = await GameAsync(0);
        var s = game.State;
        var pointer = game.Input.Pointer;

        await RunAsync(game,
            _ => TreasureRoster.Turn(pointer),
            st =>
            {
                Assert.Equal(1, TreasureRoster.View(st));
                TreasureRoster.Pick(stock, pointer);
            },
            st =>
            {
                Assert.False(TreasureRoster.Prompt(st)!.UnitFirst);
                TreasureRoster.Pick(1, pointer);
            });

        Assert.Equal(stock, s.Units[1].Item.Value);
        Assert.Equal(1, s.Items[stock].Holder.Value);
    }

    [ScenarioFact]
    public async Task A_held_item_without_a_pick_asks_and_Yes_takes_it_off()
    {
        var (game, other, _, held) = await GameAsync(1);
        var s = game.State;
        game.Input.Menu.Enqueue("Sｱｲﾃﾑ.ADV", 399);                        // Yes

        await RunAsync(game, _ => TreasureRoster.Pick(held, game.Input.Pointer));

        Assert.Equal(0, s.Units[other].Item.Value);
        Assert.Equal(100, s.Items[held].Holder.Value);
        Assert.Single(game.Input.Menus);
    }

    [ScenarioFact]
    public async Task Back_drops_a_half_made_pick_and_turns_the_view_back()
    {
        var (game, other, _, held) = await GameAsync(0);
        var s = game.State;
        var pointer = game.Input.Pointer;

        await RunAsync(game,
            _ => TreasureRoster.Pick(1, pointer),
            _ => TreasureRoster.Back(pointer),
            st =>
            {
                Assert.Null(TreasureRoster.Prompt(st));
                Assert.Equal(0, TreasureRoster.View(st));
            });

        Assert.Equal(0, s.Units[1].Item.Value);
        Assert.Equal(held, s.Units[other].Item.Value);
    }
}
