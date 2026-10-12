using Kichikuou.Content;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

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

    /// <summary>The commander's HP and maximum.</summary>
    Hp,

    /// <summary>Experience and the next level's.</summary>
    Exp,

    /// <summary>The level.</summary>
    Level,

    /// <summary>The commander's attack.</summary>
    Atk,

    /// <summary>The commander's defense.</summary>
    Def,

    /// <summary>The number of attacks.</summary>
    Attacks,

    /// <summary>The strategy's success chance.</summary>
    Strategy,

    /// <summary>Magic.</summary>
    Magic,

    /// <summary>The troop type's name.</summary>
    TroopType,

    /// <summary>The troops' attack.</summary>
    TroopAtk,

    /// <summary>The troops' defense.</summary>
    TroopDef,

    /// <summary>The war expense of sending the unit.</summary>
    Cost,
}

/// <summary>A column's compact header, its full name (the header's tooltip) and whether it is text.</summary>
public sealed record DeploymentColumnInfo(DeploymentColumn Column, string Header, string FullName, bool IsText = false);

/// <summary>
/// One listed unit of the deployment screen (<c>Sﾌﾞﾀｲ</c>) as the table shows it: its values from
/// GameState, whether it is grey (deployed this week, status 2) and its place in the party (1-4, or 0).
/// </summary>
public sealed record DeploymentRow(
    int Unit, string Name, string TroopType, int Troops, int MaxTroops, int Hp, int MaxHp, int Exp, int NextExp,
    int Level, int Atk, int Def, int Attacks, int Strategy, int Magic, int TroopAtk, int TroopDef, int Cost,
    bool Grey, int PartyPlace)
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
        DeploymentColumn.TroopType => TroopType,
        DeploymentColumn.TroopAtk => TroopAtk.ToString(),
        DeploymentColumn.TroopDef => TroopDef.ToString(),
        DeploymentColumn.Cost => $"{Cost}00K",
        _ => throw new ArgumentOutOfRangeException(nameof(column)),
    };

    /// <summary>The cell's sort key: the number (of "a / b", a), or the text for a text column.</summary>
    public IComparable Key(DeploymentColumn column) => column switch
    {
        DeploymentColumn.Name => Name,
        DeploymentColumn.TroopType => TroopType,
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
        _ => throw new ArgumentOutOfRangeException(nameof(column)),
    };
}

/// <summary>
/// The deployment table, our own screen over <c>Sﾌﾞﾀｲ</c>'s unit grid (owner's decision 25,
/// docs/ui/deployment-table.md): the units the script lists, their values and the sort. It decides
/// nothing — a click on a row is a click on the unit's cell, which the script hit-tests.
/// </summary>
public static class DeploymentTable
{
    /// <summary>The columns in order, with their compact headers and full names.</summary>
    public static IReadOnlyList<DeploymentColumnInfo> Columns { get; } =
    [
        new(DeploymentColumn.Number, "№", "Unit number"),
        new(DeploymentColumn.Name, "Name", "Name", IsText: true),
        new(DeploymentColumn.Troops, "Troops", "Troops / maximum"),
        new(DeploymentColumn.Hp, "HP", "Commander HP / maximum"),
        new(DeploymentColumn.Exp, "EXP", "Experience / next level"),
        new(DeploymentColumn.Level, "Lv", "Level"),
        new(DeploymentColumn.Atk, "Atk", "Commander attack"),
        new(DeploymentColumn.Def, "Def", "Commander defense"),
        new(DeploymentColumn.Attacks, "Act", "Attacks"),
        new(DeploymentColumn.Strategy, "Str%", "Strategy success chance"),
        new(DeploymentColumn.Magic, "Mag", "Magic"),
        new(DeploymentColumn.TroopType, "Type", "Troop type", IsText: true),
        new(DeploymentColumn.TroopAtk, "T.Atk", "Troop attack"),
        new(DeploymentColumn.TroopDef, "T.Def", "Troop defense"),
        new(DeploymentColumn.Cost, "Cost", "War expense (GOLD)"),
    ];

    /// <summary>The highest unit the deployment screen lists (<c>Sﾌﾞﾀｲ</c>'s grid: 1-50, <c>PickerScreen</c>).</summary>
    public const int LastUnit = 50;

    /// <summary>The listed units (status not 0) in number order, as <c>Sﾌﾞﾀｲ</c> lists them.</summary>
    public static IReadOnlyList<DeploymentRow> Rows(GameState state)
    {
        Sys3Int[] party = [state.Bt_PlayerParty1, state.Bt_PlayerParty2, state.Bt_PlayerParty3, state.Bt_PlayerParty4];
        var rows = new List<DeploymentRow>();
        for (int n = 1; n <= LastUnit; n++)
        {
            var u = state.Units[n];
            if (u.Status == 0)
                continue;
            int strings = (state.Unit_StrBase + (Sys3Int)((n - 1) * 3)).Value;
            rows.Add(new DeploymentRow(
                n, StringAt(state, strings), StringAt(state, strings + 1),
                u.Troops.Value, u.MaxTroops.Value, u.HP.Value, u.MaxHP.Value,
                u.Exp.Value, ((Sys3Int)100 + u.Level * 30).Value,
                u.Level.Value, u.Atk.Value, u.Def.Value, u.Attacks.Value, u.Strategy.Value, u.Magic.Value,
                u.TroopAtk.Value, u.TroopDef.Value, (u.DeployCost + u.Troops / 300).Value,
                Grey: u.Status == 2, PartyPlace: Array.FindIndex(party, p => p.Value == n) + 1));
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

    private static readonly IComparer<IComparable> KeyComparer = Comparer<IComparable>.Create((a, b) =>
        a is string x && b is string y ? string.Compare(x, y, StringComparison.OrdinalIgnoreCase) : a.CompareTo(b));

    private static string StringAt(GameState state, int index) =>
        index >= 0 && index < state.Strings.Length ? Hankaku.ToHalfWidth(state.Strings[index]) : "";
}
