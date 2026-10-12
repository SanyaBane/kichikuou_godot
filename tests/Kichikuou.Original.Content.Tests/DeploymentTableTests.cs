using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary><see cref="DeploymentTable"/>: the units <c>Sﾌﾞﾀｲ</c> lists, their cells and the sort (docs/ui/deployment-table.md).</summary>
public class DeploymentTableTests
{
    /// <summary>
    /// Rance (unit 1) deployed this week, Sill (unit 2) ready and second in the party, Bound (unit 3)
    /// first in it, unit 4 not in the army, unit 50 ready; the names in the string table as the boot
    /// puts them (base 20, three strings a unit).
    /// </summary>
    private static GameState World()
    {
        var s = new GameState { Unit_StrBase = 20 };
        void Unit(int n, string name, string type, int status, int troops, int atk)
        {
            s.Strings[20 + (n - 1) * 3] = name;
            s.Strings[21 + (n - 1) * 3] = type;
            var u = s.Units[n];
            u.Status = (Sys3Int)status;
            u.Troops = (Sys3Int)troops;
            u.MaxTroops = 1000;
            u.Atk = (Sys3Int)atk;
            u.Level = 2;
            u.Exp = 40;
            u.DeployCost = 1;
        }
        Unit(1, "Rance", "Thieves", 2, 900, 5);
        Unit(2, "Sill", "Mages", 1, 300, 1);
        Unit(3, "Bound", "Thieves", 1, 600, 5);
        Unit(4, "Gone", "Thieves", 0, 100, 9);
        Unit(50, "Last", "Knights", 1, 600, 3);
        s.Bt_PlayerParty1 = 3;
        s.Bt_PlayerParty2 = 2;
        return s;
    }

    [Fact]
    public void The_rows_are_the_listed_units_in_number_order()
    {
        var rows = DeploymentTable.Rows(World());

        Assert.Equal([1, 2, 3, 50], rows.Select(r => r.Unit));
        Assert.Equal(["Rance", "Sill", "Bound", "Last"], rows.Select(r => r.Name));
        Assert.Equal([true, false, false, false], rows.Select(r => r.Grey));       // status 2: deployed this week
        Assert.Equal([0, 2, 1, 0], rows.Select(r => r.PartyPlace));
        var rance = rows[0];
        Assert.Equal("900/1000", rance.Text(DeploymentColumn.Troops));
        Assert.Equal("40/160", rance.Text(DeploymentColumn.Exp));                 // the next level at 100 + 2·30
        Assert.Equal("Thieves", rance.Text(DeploymentColumn.TroopType));
        Assert.Equal("400K", rance.Text(DeploymentColumn.Cost));                  // 1 + 900/300, ×100K
        Assert.Equal("300K", rows[2].Text(DeploymentColumn.Cost));                // Bound: 1 + 600/300
    }

    [Fact]
    public void Every_column_has_a_header_a_full_name_a_text_and_a_key()
    {
        var row = DeploymentTable.Rows(World())[0];
        Assert.Equal(Enum.GetValues<DeploymentColumn>(), DeploymentTable.Columns.Select(c => c.Column));
        Assert.All(DeploymentTable.Columns, c =>
        {
            Assert.NotEmpty(c.Header);
            Assert.NotEmpty(c.FullName);
            Assert.NotNull(row.Text(c.Column));
            Assert.Equal(c.IsText, row.Key(c.Column) is string);
        });
    }

    [Fact]
    public void A_sort_keeps_grey_rows_in_place_and_breaks_ties_by_number()
    {
        var rows = DeploymentTable.Rows(World());

        Assert.Equal([1, 3, 50, 2], DeploymentTable.Sort(rows, DeploymentColumn.Troops, descending: true).Select(r => r.Unit));
        Assert.Equal([1, 3, 50, 2], DeploymentTable.Sort(rows, DeploymentColumn.Atk, descending: true).Select(r => r.Unit));   // 5, 5 by number
        Assert.Equal([2, 50, 1, 3], DeploymentTable.Sort(rows, DeploymentColumn.Atk, descending: false).Select(r => r.Unit));
        Assert.Equal([3, 50, 1, 2], DeploymentTable.Sort(rows, DeploymentColumn.Name, descending: false).Select(r => r.Unit));
        Assert.Equal([1, 2, 3, 50], DeploymentTable.Sort(rows, DeploymentColumn.Number, descending: false).Select(r => r.Unit));
    }
}
