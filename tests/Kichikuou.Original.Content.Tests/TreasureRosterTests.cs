using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>
/// Our own Treasure screen (docs/ui/pickers.md, mock-up S5): <see cref="TreasureRoster"/> on hand-made states, and its
/// orders answering <c>Cﾀｶﾗ</c> on the booted new game — the picker still hit-tests the clicks and <c>Sｱｲﾃﾑ</c> equips.
/// The script tests are skipped without the decompile.
/// </summary>
public class TreasureRosterTests(ITestOutputHelper output)
{
    /// <summary>Rance (unit 1) with Chaos (1), Barres (unit 6) with Sword (6), Rick (unit 7) deployed with nothing; Ring (7) in stock, Stone (8) without effect.</summary>
    private static GameState State()
    {
        var s = new GameState { Unit_StrBase = 20, Item_StrBase = 300 };
        void Unit(int n, string name, string type, int status, int item)
        {
            s.Strings[20 + (n - 1) * 3] = name;
            s.Strings[21 + (n - 1) * 3] = type;
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
        Unit(1, "Rance", "Warrior", 1, 1);
        Unit(6, "Barres", "Knight", 1, 6);
        Unit(7, "Rick", "Knight", 2, 0);
        Unit(9, "Fallen", "Soldier", 3, 0);
        s.Items[7].Holder = 100;
        s.Items[8].Holder = 100;
        return s;
    }

    [Fact]
    public void The_items_owned_say_where_they_are_and_which_can_be_dragged()
    {
        var s = State();

        var items = TreasureRoster.Items(s, 7);

        Assert.Equal([1, 6, 7, 8], items.Select(i => i.Number));
        Assert.Equal(["never moves", "On Barres", "In stock", "can't be equipped"], items.Select(i => i.Where));
        Assert.Equal([true, false, false, true], items.Select(i => i.Greyed));
        Assert.Equal("Never leaves Rance", items[0].Refusal);
        Assert.Equal("Equipped to Barres — take it off Barres first", items[1].Refusal);
        Assert.Null(items[2].Refusal);
        Assert.Equal("This item can't be equipped", items[3].Refusal);
        Assert.StartsWith("Already equipped", TreasureRoster.Items(s, 6)[1].Refusal);
    }

    [Fact]
    public void The_characters_are_the_units_at_status_1_or_2_with_their_items()
    {
        var s = State();

        var characters = TreasureRoster.Characters(s);

        Assert.Equal([1, 6, 7], characters.Select(c => c.Unit));
        Assert.Equal(new TreasureCharacter(1, "Rance", "Warrior", 1, "Chaos", true), characters[0]);
        Assert.Equal(new TreasureCharacter(6, "Barres", "Knight", 6, "Sword", false), characters[1]);
        Assert.Equal(new TreasureCharacter(7, "Rick", "Knight", 0, "", false), characters[2]);
        Assert.Equal(6, TreasureRoster.FirstChoice(characters, 6));
        Assert.Equal(1, TreasureRoster.FirstChoice(characters, 9));
    }

    [Fact]
    public void A_drop_is_refused_on_Chaos_s_bearer_and_for_items_the_script_would_refuse()
    {
        var s = State();
        Assert.Equal("Chaos never leaves Rance", TreasureRoster.DropRefusal(s, 1, 7));
        Assert.Null(TreasureRoster.DropRefusal(s, 6, 7));                   // his Sword back to stock
        Assert.Null(TreasureRoster.DropRefusal(s, 7, 7));
        Assert.NotNull(TreasureRoster.DropRefusal(s, 7, 6));                // Barres's: taken off him first
        Assert.NotNull(TreasureRoster.DropRefusal(s, 7, 8));
        Assert.NotNull(TreasureRoster.DropRefusal(s, 7, 1));
        Assert.Equal((false, true, false), (TreasureRoster.CanTakeOff(s, 1), TreasureRoster.CanTakeOff(s, 6), TreasureRoster.CanTakeOff(s, 7)));
    }

    [Fact]
    public void The_details_carry_the_strip_s_figures_and_the_sprite()
    {
        var s = State();
        var u = s.Units[6];
        (u.Troops, u.MaxTroops, u.DeployCost, u.Level, u.HP, u.MaxHP, u.CgAlone) = (900, 1200, 3, 5, 40, 52, 1234);

        var d = TreasureRoster.Details(s, 6);

        Assert.Equal((900, 1200, 3, 5, 40, 52, 1234, 6, "Sword", false),
            (d.Troops, d.MaxTroops, d.Cost, d.Level, d.Hp, d.MaxHp, d.SpriteCg, d.Item, d.ItemName, d.Locked));
    }

    [Fact]
    public void Orders_the_screen_should_not_offer_queue_nothing()
    {
        var s = State();
        var pointer = new Engine.Script.PointerScript();
        Assert.Null(TreasureOrder.Equip(s, 1, 7, pointer));
        Assert.Null(TreasureOrder.Equip(s, 7, 6, pointer));
        Assert.Null(TreasureOrder.Unequip(s, 1, pointer));
        Assert.Null(TreasureOrder.Unequip(s, 7, pointer));
        Assert.Equal(0, pointer.Pending);
    }

    /// <summary>
    /// The booted game with two units at status 1 (Rance (unit 1) and the next), two items with an effect: the first in
    /// stock, the second on the other unit; the picker's view <paramref name="view"/>.
    /// </summary>
    private static async Task<(EndGame Game, int Other, int Stock, int Held)?> GameAsync(int view)
    {
        if (await EndGame.BootAsync() is not { } game)
            return null;
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

    /// <summary>Runs <c>Cﾀｶﾗ</c>: <paramref name="order"/> at the first read, End once it is over.</summary>
    private static async Task RunAsync(EndGame game, Func<TreasureOrder?> order)
    {
        TreasureOrder? given = null;
        bool ended = false;
        game.Input.Idle = () =>
        {
            if (given is null)
                given = order() ?? throw new InvalidOperationException("No order.");
            else if (!given.Running && !ended)
            {
                ended = true;
                TreasureRoster.End(game.Input.Pointer);
            }
        };
        game.Input.Menu.Enqueue(items => given?.Choose(items) ?? throw new InvalidOperationException("A menu no order answers."));
        await game.Script.CallFileAsync("Cﾀｶﾗ.ADV");
        Assert.True(ended);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Equip_picks_the_unit_then_the_item_and_the_script_equips(int view)
    {
        if (await GameAsync(view) is not ({ } game, _, var stock, _))
            return;
        var s = game.State;

        await RunAsync(game, () => TreasureOrder.Equip(s, 1, stock, game.Input.Pointer));

        output.WriteLine($"item {stock} on Rance; view {s.Ui_ItemViewMode.Value}; menus {game.Input.Menus.Count}");
        Assert.Equal(stock, s.Units[1].Item.Value);
        Assert.Equal(1, s.Items[stock].Holder.Value);
        Assert.Empty(game.Input.Menus);
    }

    [Fact]
    public async Task Equip_on_a_unit_holding_an_item_sends_his_own_back_to_stock()
    {
        if (await GameAsync(1) is not ({ } game, var other, var stock, var held))
            return;
        var s = game.State;

        await RunAsync(game, () => TreasureOrder.Equip(s, other, stock, game.Input.Pointer));

        Assert.Equal(stock, s.Units[other].Item.Value);
        Assert.Equal(other, s.Items[stock].Holder.Value);
        Assert.Equal(100, s.Items[held].Holder.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public async Task Unequip_clicks_the_held_item_and_answers_Yes(int view)
    {
        if (await GameAsync(view) is not ({ } game, var other, _, var held))
            return;
        var s = game.State;

        await RunAsync(game, () => TreasureOrder.Unequip(s, other, game.Input.Pointer));

        Assert.Equal(0, s.Units[other].Item.Value);
        Assert.Equal(100, s.Items[held].Holder.Value);
        Assert.Single(game.Input.Menus);
    }
}
