using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>
/// An item owned, as our Treasure screen's items column shows it: its number, name, holder (100 the stock, else the
/// unit), where it is ("In stock", "On Rance", "never moves", "can't be equipped"), whether it is greyed, and why it
/// cannot be dragged (null: it can).
/// </summary>
public sealed record TreasureItem(int Number, string Name, int Holder, string Where, bool Greyed, string? Refusal);

/// <summary>
/// A character of our Treasure screen's list (a unit at status 1 or 2): its name, troop type, the item it holds (0:
/// none) and its name, and whether that item is Chaos (1) or Nikkou (2), which never leave it.
/// </summary>
public sealed record TreasureCharacter(int Unit, string Name, string Type, int Item, string ItemName, bool Locked);

/// <summary>
/// The chosen character as the middle of our Treasure screen shows it: the figures of its strip (the cost
/// <c>Unit_DeployCost</c> ×100K, as <c>Sｱｲﾃﾑ</c>'s details), its battle sprite (<c>Unit_CgAlone</c>) and its item.
/// </summary>
public sealed record TreasureDetails(
    int Unit, string Name, string Type, int Troops, int MaxTroops, int Cost, int Level, int Hp, int MaxHp,
    int Atk, int Def, int Attacks, int Magic, int SpriteCg, int Item, string ItemName, bool Locked);

/// <summary>
/// Our own Treasure screen's model (docs/ui/pickers.md, mock-up S5): the items owned and the characters, read from the
/// state as <c>Sｱｲﾃﾑ</c> lists them, what the screen lets the player drag and drop — read the way
/// <c>ItemScreen.PickAsync</c> decides it, so the script's refusals and its question about another unit's item are
/// never reached —, and the orders that answer the picker (<see cref="TreasureOrder"/>). It decides nothing: the
/// picker still hit-tests the clicks and <c>Sｱｲﾃﾑ</c> equips.
/// </summary>
public static class TreasureRoster
{
    /// <summary>The items that never move (<c>Sｱｲﾃﾑ.ADV:249-266</c>, <c>:290-308</c>): Chaos (1) and Nikkou (2).</summary>
    public static bool Fixed(int item) => item is 1 or 2;

    /// <summary>Item <paramref name="item"/>'s name: its string (<c>Item_StrBase + n - 1</c>).</summary>
    public static string ItemName(GameState s, int item) =>
        item is >= 1 and <= 50 ? DeploymentTable.StringAt(s, (s.Item_StrBase + (Sys3Int)(item - 1)).Value) : "";

    /// <summary>
    /// The items owned (<c>Item_Holder</c> not 0: <c>Sｱｲﾃﾑ</c>'s item view) in their numbers' order, with where each one
    /// is and why it cannot be dragged while <paramref name="chosen"/> is the character chosen: Chaos and Nikkou never
    /// move, an item without effect cannot be equipped (<c>:367</c>), the chosen one's own item is already his (<c>:311</c>,
    /// taken off by a drag out of the Equipment window), and another unit's item is taken off that unit first (the
    /// owner, 2026-10-19: so <c>:341</c>'s "remove it and equip X instead?" is never asked).
    /// </summary>
    public static IReadOnlyList<TreasureItem> Items(GameState s, int chosen)
    {
        var items = new List<TreasureItem>();
        for (int n = 1; n <= 50; n++)
        {
            var item = s.Items[n];
            int holder = item.Holder.Value;
            if (holder == 0)
                continue;
            string holderName = holder == 100 ? "" : DeploymentTable.UnitName(s, holder);
            bool noEffect = item.Effect == 0;
            string where = Fixed(n) ? "never moves" : noEffect ? "can't be equipped" : holder == 100 ? "In stock" : $"On {holderName}";
            string? refusal = Fixed(n) ? (holder == 100 ? "Never moves" : $"Never leaves {holderName}")
                : noEffect ? "This item can't be equipped"
                : holder == 100 ? null
                : holder == chosen ? "Already equipped — drag it out of the Equipment window to take it off"
                : $"Equipped to {holderName} — take it off {holderName} first";
            items.Add(new TreasureItem(n, ItemName(s, n), holder, where, Fixed(n) || noEffect, refusal));
        }
        return items;
    }

    /// <summary>The characters: the units at status 1 or 2 in their numbers' order, those <c>Sｱｲﾃﾑ</c>'s unit view lets the player pick (<c>:244</c>).</summary>
    public static IReadOnlyList<TreasureCharacter> Characters(GameState s)
    {
        var characters = new List<TreasureCharacter>();
        for (int n = 1; n <= DeploymentTable.LastUnit; n++)
        {
            var u = s.Units[n];
            if (u.Status.Value is not (1 or 2))
                continue;
            int item = u.Item.Value;
            characters.Add(new TreasureCharacter(n, DeploymentTable.UnitName(s, n), TypeOf(s, n), item, ItemName(s, item), Fixed(item)));
        }
        return characters;
    }

    /// <summary>The chosen character's figures, sprite and item.</summary>
    public static TreasureDetails Details(GameState s, int unit)
    {
        var u = s.Units[unit];
        int item = u.Item.Value;
        return new TreasureDetails(unit, DeploymentTable.UnitName(s, unit), TypeOf(s, unit), u.Troops.Value, u.MaxTroops.Value,
            u.DeployCost.Value, u.Level.Value, u.HP.Value, u.MaxHP.Value, u.Atk.Value, u.Def.Value, u.Attacks.Value, u.Magic.Value,
            u.CgAlone.Value, item, ItemName(s, item), Fixed(item));
    }

    /// <summary>The character the screen chooses first: the one chosen before while it is still listed, else the first (Rance (unit 1)).</summary>
    public static int FirstChoice(IReadOnlyList<TreasureCharacter> characters, int chosen) =>
        characters.Any(c => c.Unit == chosen) ? chosen : characters.Count > 0 ? characters[0].Unit : 0;

    /// <summary>
    /// Why <paramref name="unit"/>'s Equipment window takes no drop of <paramref name="item"/> (null: it does): a unit
    /// holding Chaos or Nikkou (<c>:249-254</c>), the item's own refusal (<see cref="Items"/>).
    /// </summary>
    public static string? DropRefusal(GameState s, int unit, int item)
    {
        int held = s.Units[unit].Item.Value;
        if (Fixed(held))
            return $"{ItemName(s, held)} never leaves {DeploymentTable.UnitName(s, unit)}";
        return Items(s, unit).FirstOrDefault(i => i.Number == item) is { } owned ? owned.Refusal : "Not owned";
    }

    /// <summary>Whether <paramref name="unit"/>'s item can be dragged out of its Equipment window (it holds one, not Chaos or Nikkou).</summary>
    public static bool CanTakeOff(GameState s, int unit) => s.Units[unit].Item.Value is var item && item != 0 && !Fixed(item);

    /// <summary>Where the script's pointer rests under our screen: the grid's empty 51st cell, so the picker hovers nothing.</summary>
    public static (int X, int Y) Rest => GesturePoints.PickerCell(51);

    /// <summary>End: a click on the bar's "End" (503, <c>Sｱｲﾃﾑ.ADV:84-85</c>).</summary>
    public static void End(PointerScript pointer)
    {
        var (x, y) = GesturePoints.PickerButton(3);
        pointer.Click(x, y);
    }

    /// <summary>The unit's troop type: its second string.</summary>
    private static string TypeOf(GameState s, int unit) =>
        DeploymentTable.StringAt(s, (s.Unit_StrBase + (Sys3Int)((unit - 1) * 3 + 1)).Value);
}

/// <summary>The orders our Treasure screen gives.</summary>
public enum TreasureOrderKind
{
    /// <summary>A stock item onto a character (his own item back to stock).</summary>
    Equip,

    /// <summary>A character's item back to stock.</summary>
    Unequip,
}

/// <summary>
/// One order of our Treasure screen as the clicks <c>Sｱｲﾃﾑ</c> expects from its player (docs/ui/pickers.md):
/// <list type="bullet">
/// <item>Equip — the unit view (the bar's Item/Character, 502, when the item view is up), the unit's cell (its pick turns
/// the view to the items), the item's cell: the script equips it, the unit's own item going back to stock
/// (<c>L_0043a</c>).</item>
/// <item>Unequip — the item view (502 when the unit view is up), the item's cell with no unit picked: the script asks
/// "would you like to unequip it?" (<c>:322</c>), answered Yes (<see cref="Choose"/>), and takes it off (504,
/// <c>L_00b39</c>).</item>
/// </list>
/// The order runs while its clicks are on their way and, for Unequip, until the question is answered; a key wait on the
/// way (a refusal the screen did not foresee) ends it, so the player sees the window.
/// </summary>
public sealed class TreasureOrder
{
    /// <summary>The Yes of <c>Sｱｲﾃﾑ</c>'s Yes/No (<c>L_00a3f</c>, <c>:399</c>).</summary>
    private static readonly ScriptRef Yes = new("Sｱｲﾃﾑ.ADV", 399);

    private readonly PointerScript _pointer;
    private bool _asking, _stopped;

    private TreasureOrder(TreasureOrderKind kind, int unit, int item, PointerScript pointer)
    {
        Kind = kind;
        Unit = unit;
        Item = item;
        _pointer = pointer;
        _asking = kind == TreasureOrderKind.Unequip;
    }

    /// <summary>The order given.</summary>
    public TreasureOrderKind Kind { get; }

    /// <summary>The character it is given for.</summary>
    public int Unit { get; }

    /// <summary>The item it moves.</summary>
    public int Item { get; }

    /// <summary>Whether the order still runs: its clicks on their way, or Unequip's question not answered yet.</summary>
    public bool Running => !_stopped && (_pointer.Pending > 0 || _asking);

    /// <summary>
    /// Equip: <paramref name="item"/> (in stock) onto <paramref name="unit"/> — the clicks queued for the script's pointer.
    /// Null when the screen should not have offered it (<see cref="TreasureRoster.DropRefusal"/>).
    /// </summary>
    public static TreasureOrder? Equip(GameState s, int unit, int item, PointerScript pointer)
    {
        if (TreasureRoster.DropRefusal(s, unit, item) is not null || s.Units[unit].Status.Value is not (1 or 2))
            return null;
        if (s.Ui_ItemViewMode != 0)
            Click(GesturePoints.PickerButton(2), pointer);              // the unit view
        Click(GesturePoints.PickerCell(unit), pointer);                 // the unit: the view turns to the items
        Click(GesturePoints.PickerCell(item), pointer);                 // the item: equipped
        return new TreasureOrder(TreasureOrderKind.Equip, unit, item, pointer);
    }

    /// <summary>Unequip: <paramref name="unit"/>'s item back to stock. Null when it holds none, or Chaos or Nikkou.</summary>
    public static TreasureOrder? Unequip(GameState s, int unit, PointerScript pointer)
    {
        if (!TreasureRoster.CanTakeOff(s, unit))
            return null;
        int item = s.Units[unit].Item.Value;
        if (s.Ui_ItemViewMode != 1)
            Click(GesturePoints.PickerButton(2), pointer);              // the item view
        Click(GesturePoints.PickerCell(item), pointer);                 // the held item, no unit picked: the question
        return new TreasureOrder(TreasureOrderKind.Unequip, unit, item, pointer);
    }

    /// <summary>The choice for a menu the script opens while the order runs (1-based): Unequip's Yes; null for any other.</summary>
    public int? Choose(IReadOnlyList<SceneMenuItem> items)
    {
        if (!_asking)
            return null;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i].At == Yes)
            {
                _asking = false;
                return i + 1;
            }
        }
        return null;
    }

    /// <summary>
    /// A key wait while the order runs: a refusal the screen did not foresee; the order is over (the screen drops its
    /// clicks left with the pointer) and the window is the player's.
    /// </summary>
    public void KeyWait() => _stopped = true;

    private static void Click((int X, int Y) at, PointerScript pointer) => pointer.Click(at.X, at.Y);
}
