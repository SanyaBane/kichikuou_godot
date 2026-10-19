using Kichikuou.Content;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Content.Strategy;

/// <summary>A column of the <see cref="DeploymentTable"/> (docs/ui/deployment-table.md).</summary>
public enum DeploymentColumn
{
    /// <summary>The unit's record number.</summary>
    Number,

    /// <summary>The unit's name.</summary>
    Name,

    /// <summary>Troops and their maximum.</summary>
    Troops,

    /// <summary>The troops' attack.</summary>
    TroopAtk,

    /// <summary>The troops' defense.</summary>
    TroopDef,

    /// <summary>The strategy's success chance.</summary>
    Strategy,

    /// <summary>The war expense of sending the unit.</summary>
    Cost,

    /// <summary>The commander's HP and maximum.</summary>
    Hp,

    /// <summary>The level.</summary>
    Level,

    /// <summary>Experience and the next level's.</summary>
    Exp,

    /// <summary>The commander's attack.</summary>
    Atk,

    /// <summary>The commander's defense.</summary>
    Def,

    /// <summary>The number of attacks.</summary>
    Attacks,

    /// <summary>Magic.</summary>
    Magic,

    /// <summary>The troop type (the unit's second string).</summary>
    TroopType,

    /// <summary>Replenish's price (<c>Cﾌﾞｶ</c>), none when the troops are at their maximum.</summary>
    Refill,

    /// <summary>The unit's item.</summary>
    Item,

    /// <summary>The rows the unit fights from (its class: "Front", "Back" or "Front, Back").</summary>
    Rows,
}

/// <summary>A column's compact header, its full name (the header's tooltip) and whether it is text.</summary>
public sealed record DeploymentColumnInfo(DeploymentColumn Column, string Header, string FullName, bool IsText = false);

/// <summary>
/// One listed unit of the deployment screen (<c>Sﾌﾞﾀｲ</c>) as the table shows it: its values from
/// GameState, whether it is grey (deployed this week, status 2) and its place in the party (1-4, or 0).
/// </summary>
public sealed record DeploymentRow(
    int Unit, string Name, int Troops, int MaxTroops, int Hp, int MaxHp, int Exp, int NextExp,
    int Level, int Atk, int Def, int Attacks, int Strategy, int Magic, int TroopAtk, int TroopDef, int Cost,
    bool Grey, int PartyPlace, string TroopType = "", int Refill = 0, string Item = "", string Rows = "")
{
    /// <summary>The cell's text.</summary>
    public string Text(DeploymentColumn column) => column switch
    {
        DeploymentColumn.Number => Unit.ToString(),
        DeploymentColumn.Name => Name,
        DeploymentColumn.Troops => $"{Troops}/{MaxTroops}",
        DeploymentColumn.Hp => $"{Hp}/{MaxHp}",
        DeploymentColumn.Exp => $"{Exp}/{NextExp}",
        DeploymentColumn.Level => Level.ToString(),
        DeploymentColumn.Atk => Atk.ToString(),
        DeploymentColumn.Def => Def.ToString(),
        DeploymentColumn.Attacks => Attacks.ToString(),
        DeploymentColumn.Strategy => $"{Strategy}%",
        DeploymentColumn.Magic => Magic.ToString(),
        DeploymentColumn.TroopAtk => TroopAtk.ToString(),
        DeploymentColumn.TroopDef => TroopDef.ToString(),
        DeploymentColumn.Cost => $"{Cost * 100}K",
        DeploymentColumn.TroopType => TroopType,
        DeploymentColumn.Refill => Refill == 0 ? "—" : $"{Refill * 100}K",
        DeploymentColumn.Item => Item == "" ? "—" : Item,
        DeploymentColumn.Rows => Rows == "" ? "—" : Rows,
        _ => throw new ArgumentOutOfRangeException(nameof(column)),
    };

    /// <summary>The cell's sort key: the number (of "a / b", a), or the text for a text column.</summary>
    public IComparable Key(DeploymentColumn column) => column switch
    {
        DeploymentColumn.Name => Name,
        DeploymentColumn.Number => Unit,
        DeploymentColumn.Troops => Troops,
        DeploymentColumn.Hp => Hp,
        DeploymentColumn.Exp => Exp,
        DeploymentColumn.Level => Level,
        DeploymentColumn.Atk => Atk,
        DeploymentColumn.Def => Def,
        DeploymentColumn.Attacks => Attacks,
        DeploymentColumn.Strategy => Strategy,
        DeploymentColumn.Magic => Magic,
        DeploymentColumn.TroopAtk => TroopAtk,
        DeploymentColumn.TroopDef => TroopDef,
        DeploymentColumn.Cost => Cost,
        DeploymentColumn.TroopType => TroopType,
        DeploymentColumn.Refill => Refill,
        DeploymentColumn.Item => Item,
        DeploymentColumn.Rows => Rows,
        _ => throw new ArgumentOutOfRangeException(nameof(column)),
    };
}

/// <summary>
/// The details panel of our deployment screen: the unit whose details the script shows (its
/// portrait is on the screen), with what <c>Sﾌﾞﾀｲ</c> prints for it (<c>:524-569</c>) — the war
/// expense, the troops, the rows it fights in (<see cref="Rows"/>: "Front", "Back", "Front, Back"
/// or "") and its item ("" without one) — and its troop type, which the original shows in the grid
/// (the owner's choice, 2026-10-12: here rather than in a column of the table).
/// </summary>
public sealed record DeploymentDetails(int Unit, string Name, string TroopType, int Cost, int Troops, int MaxTroops, string Rows, string Item);

/// <summary>
/// A place of the party panel: the member's number, name, war expense, troops, troop type, face CG
/// (<c>Unit_FaceCg</c>, 0 = none; <see cref="UnitFaces"/> cuts it) and maximum troops (the card's strength bar).
/// </summary>
public sealed record DeploymentMember(int Place, int Unit, string Name, int Cost, int Troops, string TroopType, int FaceCg, int MaxTroops);

/// <summary>
/// The party panel of our deployment screen: the places the screen offers (<c>Tmp0040</c>, at most
/// four), each a member or null, the troop total and the war expenses as <c>Sﾌﾞﾀｲ</c> sums them
/// (<c>:269-366</c>), and the gold they are paid from (both ×100K).
/// </summary>
public sealed record DeploymentParty(IReadOnlyList<DeploymentMember?> Places, int Troops, int Cost, int Gold)
{
    /// <summary>Whether the gold pays the expenses (Go is refused otherwise, <c>:159-210</c>).</summary>
    public bool Affordable => Cost <= Gold;
}

/// <summary>
/// The deployment table, our own screen over <c>Sﾌﾞﾀｲ</c>'s unit grid (owner's decision 25,
/// docs/ui/deployment-table.md): the units the script lists, their values and the sort. It decides
/// nothing — a click on a row is a click on the unit's cell, which the script hit-tests.
/// </summary>
public static class DeploymentTable
{
    /// <summary>
    /// The columns in order, with their compact headers and full names: across the whole screen's top, so the
    /// details the original shows for the hovered unit (type, rows, item) are columns too (the owner's E1/E2,
    /// docs/ui/deployment-table.md, "The lacquer look").
    /// </summary>
    public static IReadOnlyList<DeploymentColumnInfo> Columns { get; } =
    [
        new(DeploymentColumn.Number, "№", "Unit number"),
        new(DeploymentColumn.Name, "Name", "Name", IsText: true),
        new(DeploymentColumn.TroopType, "Type", "Troop type", IsText: true),
        new(DeploymentColumn.Troops, "Troops", "Troops / maximum"),
        new(DeploymentColumn.Cost, "Cost", "War expense (GOLD)"),
        new(DeploymentColumn.Rows, "Rows", "The rows the unit fights from", IsText: true),
        new(DeploymentColumn.TroopAtk, "T.Atk", "Troop attack"),
        new(DeploymentColumn.TroopDef, "T.Def", "Troop defense"),
        new(DeploymentColumn.Strategy, "Str%", "Strategy success chance"),
        new(DeploymentColumn.Hp, "HP", "Commander HP / maximum"),
        new(DeploymentColumn.Level, "Lv", "Level"),
        new(DeploymentColumn.Exp, "EXP", "Experience / next level"),
        new(DeploymentColumn.Atk, "Atk", "Commander attack"),
        new(DeploymentColumn.Def, "Def", "Commander defense"),
        new(DeploymentColumn.Attacks, "Act", "Attacks"),
        new(DeploymentColumn.Magic, "Mag", "Magic"),
        new(DeploymentColumn.Item, "Item", "Item", IsText: true),
    ];

    /// <summary>The highest unit the deployment screen lists (<c>Sﾌﾞﾀｲ</c>'s grid: 1-50, <c>PickerScreen</c>).</summary>
    public const int LastUnit = 50;

    /// <summary>
    /// The listed units (status not 0) in number order, as <c>Sﾌﾞﾀｲ</c> lists them, with their places
    /// in the party — or in <paramref name="order"/>, the party a drop asked for (<see cref="Drop"/>).
    /// </summary>
    public static IReadOnlyList<DeploymentRow> Rows(GameState state, IReadOnlyList<int>? order = null)
    {
        var rows = new List<DeploymentRow>();
        for (int n = 1; n <= LastUnit; n++)
        {
            var u = state.Units[n];
            if (u.Status == 0)
                continue;
            int strings = (state.Unit_StrBase + (Sys3Int)((n - 1) * 3)).Value;
            rows.Add(new DeploymentRow(
                n, StringAt(state, strings),
                u.Troops.Value, u.MaxTroops.Value, u.HP.Value, u.MaxHP.Value,
                u.Exp.Value, ((Sys3Int)100 + u.Level * 30).Value,
                u.Level.Value, u.Atk.Value, u.Def.Value, u.Attacks.Value, u.Strategy.Value, u.Magic.Value,
                u.TroopAtk.Value, u.TroopDef.Value, (u.DeployCost + u.Troops / 300).Value,
                Grey: u.Status == 2, PartyPlace: order is null ? PartyPlace(state, n) : order.ToList().IndexOf(n) + 1,
                TroopType: StringAt(state, strings + 1),
                Refill: u.Troops == u.MaxTroops ? 0 : SubordinatesCommand.Refill(u).Price.Value,
                Item: u.Item == 0 ? "" : StringAt(state, (state.Item_StrBase + u.Item - 1).Value),
                Rows: RowsOf(u.Class.Value)));
        }
        return rows;
    }

    /// <summary>
    /// <paramref name="rows"/> sorted by <paramref name="column"/>, ties by number; grey rows sort like
    /// the rest (the owner's choice: Rance (unit 1) stays first by number after he has fought).
    /// </summary>
    public static IReadOnlyList<DeploymentRow> Sort(IEnumerable<DeploymentRow> rows, DeploymentColumn column, bool descending)
    {
        var sorted = descending
            ? rows.OrderByDescending(r => r.Key(column), KeyComparer)
            : rows.OrderBy(r => r.Key(column), KeyComparer);
        return [.. sorted.ThenBy(r => r.Unit)];
    }

    /// <summary>
    /// Where the script's pointer is while the player's pointer is over the table: on the hovered
    /// row's cell (the script shows that unit's details, as when its cell is hovered), or, off the
    /// rows (<paramref name="unit"/> 0), on the empty 51st cell, which the script never highlights
    /// (only ids below <c>Tmp0032</c>, 51) and the table never clicks (CMD-24).
    /// </summary>
    public static (int X, int Y) PointerAt(int unit) => GesturePoints.PickerCell(unit is >= 1 and <= LastUnit ? unit : 51);

    /// <summary>
    /// A left press on <paramref name="unit"/>'s row as the gestures <c>Sﾌﾞﾀｲ</c> expects: a unit out
    /// of the party is a click on its cell (it joins, or the script refuses it with its own sound and
    /// message); a member is taken out — a click on its cell moves it to the end (CMD-16), then a
    /// click left of the list takes the last member out (<c>Sﾌﾞﾀｲ.ADV:123-145</c>).
    /// </summary>
    public static void Press(GameState state, int unit, PointerScript pointer)
    {
        var cell = GesturePoints.PickerCell(unit);
        pointer.Click(cell.X, cell.Y);
        if (PartyPlace(state, unit) == 0)
            return;
        var outside = GesturePoints.PickerOutside;
        pointer.Click(outside.X, outside.Y);
    }

    /// <summary>The party's members in place order (<c>Bt_PlayerParty1-4</c>; <c>Sﾌﾞﾀｲ</c> keeps them without gaps).</summary>
    public static IReadOnlyList<int> PartyOrder(GameState state) =>
        [.. new[] { state.Bt_PlayerParty1, state.Bt_PlayerParty2, state.Bt_PlayerParty3, state.Bt_PlayerParty4 }
            .Select(p => p.Value).Where(u => u != 0)];

    /// <summary>
    /// The order a drag of place <paramref name="from"/>'s member onto place <paramref name="to"/>
    /// asks for (places 1-4): the two members swap places; onto a place no member stands at, the
    /// member goes to the end (the party has no gaps). Null when nothing changes.
    /// </summary>
    public static IReadOnlyList<int>? Dropped(IReadOnlyList<int> order, int from, int to)
    {
        if (from < 1 || from > order.Count || to < 1 || from == to || (to > order.Count && from == order.Count))
            return null;
        var wanted = order.ToList();
        if (to <= order.Count)
            (wanted[from - 1], wanted[to - 1]) = (wanted[to - 1], wanted[from - 1]);
        else
        {
            wanted.RemoveAt(from - 1);
            wanted.Add(order[from - 1]);
        }
        return wanted;
    }

    /// <summary>
    /// The members to click, in turn, so that the party <paramref name="order"/> becomes
    /// <paramref name="wanted"/> (the same members): a click moves a member to the end (CMD-16), so
    /// the members not clicked keep their order at the front. The fewest clicks leave alone the
    /// longest start of <paramref name="wanted"/> that stands in that order in the party and click
    /// the rest in the wanted order.
    /// </summary>
    public static IReadOnlyList<int> Reordering(IReadOnlyList<int> order, IReadOnlyList<int> wanted)
    {
        if (order.Count != wanted.Count || order.Except(wanted).Any())
            throw new ArgumentException("The wanted order must hold the party's members.", nameof(wanted));
        int kept = 0;
        foreach (int unit in order)
            if (kept < wanted.Count && wanted[kept] == unit)
                kept++;
        return [.. wanted.Skip(kept)];
    }

    /// <summary>
    /// A drop of place <paramref name="from"/>'s member onto place <paramref name="to"/> (<see cref="Dropped"/>)
    /// as the gestures <c>Sﾌﾞﾀｲ</c> expects: a click on each cell of <see cref="Reordering"/>. Returns
    /// the order wanted (the panels show it while the clicks run), or null when nothing changes.
    /// </summary>
    public static IReadOnlyList<int>? Drop(GameState state, int from, int to, PointerScript pointer)
    {
        var order = PartyOrder(state);
        if (Dropped(order, from, to) is not { } wanted)
            return null;
        foreach (int unit in Reordering(order, wanted))
        {
            var cell = GesturePoints.PickerCell(unit);
            pointer.Click(cell.X, cell.Y);
        }
        return wanted;
    }

    /// <summary>A right press on the table: a right click where the script's pointer is (<see cref="PointerAt"/>), which takes the last member out (999).</summary>
    public static void RightPress(int unit, PointerScript pointer)
    {
        var at = PointerAt(unit);
        pointer.RightClick(at.X, at.Y);
    }

    /// <summary>The rows a unit of <paramref name="unitClass"/> fights from, as <c>Sﾌﾞﾀｲ</c>'s details name them (<c>:524-569</c>).</summary>
    public static string RowsOf(int unitClass) => unitClass switch
    {
        0 or 4 or 10 => "Front",
        1 or 2 or 11 => "Back",
        3 => "Front, Back",
        _ => "",
    };

    /// <summary>
    /// The details the script shows now (<c>Tmp0026</c> set, for the listed unit <c>Tmp0033</c>, as
    /// the picker draws its portrait), or null.
    /// </summary>
    public static DeploymentDetails? Details(GameState state)
    {
        int unit = state.Tmp0033.Value;
        if (state.Tmp0026 == 0 || unit is < 1 or > LastUnit || state.Units[unit].Status == 0)
            return null;
        var u = state.Units[unit];
        string rows = RowsOf(u.Class.Value);
        string item = u.Item == 0 ? "" : StringAt(state, (state.Item_StrBase + u.Item - 1).Value);
        return new DeploymentDetails(unit, UnitName(state, unit), StringAt(state, (state.Unit_StrBase + (Sys3Int)((unit - 1) * 3)).Value + 1),
            (u.DeployCost + u.Troops / 300).Value, u.Troops.Value, u.MaxTroops.Value, rows, item);
    }

    /// <summary>
    /// The party being built (<c>Bt_PlayerParty1-4</c>) in the places the screen offers, with its
    /// totals — or the party in <paramref name="order"/>, the order a drop asked for (<see cref="Drop"/>).
    /// </summary>
    public static DeploymentParty Party(GameState state, IReadOnlyList<int>? order = null)
    {
        Sys3Int[] party = order is null
            ? [state.Bt_PlayerParty1, state.Bt_PlayerParty2, state.Bt_PlayerParty3, state.Bt_PlayerParty4]
            : [.. Enumerable.Range(0, 4).Select(i => (Sys3Int)(i < order.Count ? order[i] : 0))];
        // :269-343: the lines and the sums stop at the place Tmp0040 names; any other limit shows all four.
        int places = state.Tmp0040.Value is >= 1 and <= 4 ? state.Tmp0040.Value : 4;
        var members = new DeploymentMember?[places];
        Sys3Int troops = 0, cost = 0;
        for (int place = 1; place <= places; place++)
        {
            int unit = party[place - 1].Value;
            if (unit is < 1 or > 51)
                continue;
            var u = state.Units[unit];
            troops += u.Troops;
            cost += u.DeployCost + u.Troops / 300;
            int strings = (state.Unit_StrBase + (Sys3Int)((unit - 1) * 3)).Value;
            members[place - 1] = new DeploymentMember(place, unit, StringAt(state, strings),
                (u.DeployCost + u.Troops / 300).Value, u.Troops.Value, StringAt(state, strings + 1), u.FaceCg.Value, u.MaxTroops.Value);
        }
        return new DeploymentParty(members, troops.Value, cost.Value, state.Gold.Value);
    }

    /// <summary>The unit's place in the party being built (<c>Bt_PlayerParty1-4</c>), or 0.</summary>
    public static int PartyPlace(GameState state, int unit)
    {
        Sys3Int[] party = [state.Bt_PlayerParty1, state.Bt_PlayerParty2, state.Bt_PlayerParty3, state.Bt_PlayerParty4];
        return Array.FindIndex(party, p => p.Value == unit) + 1;
    }

    private static readonly IComparer<IComparable> KeyComparer = Comparer<IComparable>.Create((a, b) =>
        a is string x && b is string y ? string.Compare(x, y, StringComparison.OrdinalIgnoreCase) : a.CompareTo(b));

    /// <summary>Unit <paramref name="unit"/>'s name: its first string (<c>Unit_StrBase + (n-1)·3</c>; scenes may rename it).</summary>
    public static string UnitName(GameState state, int unit) =>
        StringAt(state, (state.Unit_StrBase + (Sys3Int)((unit - 1) * 3)).Value);

    internal static string StringAt(GameState state, int index) =>
        index >= 0 && index < state.Strings.Length ? Hankaku.ToHalfWidth(state.Strings[index]) : "";
}
