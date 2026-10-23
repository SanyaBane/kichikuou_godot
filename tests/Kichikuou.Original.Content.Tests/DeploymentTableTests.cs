using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.Script;
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
        Assert.Equal("400K", rance.Text(DeploymentColumn.Cost));                  // 1 + 900/300, ×100K
        Assert.Equal("300K", rows[2].Text(DeploymentColumn.Cost));                // Bound: 1 + 600/300
    }

    [Fact]
    public void A_modifier_is_what_the_battle_adds_below_10_itself_above_10_minus_the_excess_10_none()
    {
        Assert.Equal([0, 3, 9, 0, -1, -9], new[] { 0, 3, 9, 10, 11, 19 }.Select(v => DeploymentTable.Modifier((Sys3Int)v)));
    }

    [Fact]
    public void The_six_modifier_cells_are_signed_in_the_status_pages_order_and_a_unit_without_troops_has_none()
    {
        var s = World();
        var bound = s.Units[3];
        (bound.OffAtkMod, bound.OffDefMod, bound.DefAtkMod, bound.DefDefMod, bound.DgnAtkMod, bound.DgnDefMod) = (2, 10, 12, 0, 5, 11);
        s.Units[50].MaxTroops = 0;
        var rows = DeploymentTable.Rows(s);
        DeploymentColumn[] six = [.. DeploymentTable.ModifierColumns.Select(c => c.Column)];

        Assert.Equal(["+2", "0", "−2", "0", "+5", "−1"], six.Select(c => rows[2].Text(c)));
        Assert.All(six, c => Assert.Equal("—", rows[3].Text(c)));      // Cﾌﾞｶ.ADV:1168: no troops, no modifiers
        Assert.Equal([3, 1, 2, 50], DeploymentTable.Sort(rows, DeploymentColumn.DgnAtkMod, descending: true).Select(r => r.Unit));
        Assert.All([DeploymentTable.Columns, SubordinatesScreen.Columns], columns => Assert.Equal(six,
            columns.SkipWhile(c => c.Column != DeploymentColumn.TroopDef).Skip(1).Take(6).Select(c => c.Column)));   // after T.Def
    }

    [Fact]
    public void Every_column_has_a_header_a_full_name_a_text_and_a_key()
    {
        var row = DeploymentTable.Rows(World())[0];
        var columns = DeploymentTable.Columns.Concat(SubordinatesScreen.Columns).ToList();      // the two screens' tables
        Assert.Equal(Enum.GetValues<DeploymentColumn>(), columns.Select(c => c.Column).Distinct().Order());
        Assert.All(columns, c =>
        {
            Assert.NotEmpty(c.Header);
            Assert.NotEmpty(c.FullName);
            Assert.NotNull(row.Text(c.Column));
            Assert.Equal(c.IsText && c.Column != DeploymentColumn.Rows, row.Key(c.Column) is string);   // the rows by rank
        });
    }

    [Fact]
    public void Values_where_more_is_better_and_the_rows_sort_descending_first_the_number_texts_and_prices_ascending()
    {
        var columns = DeploymentTable.Columns.Concat(SubordinatesScreen.Columns).DistinctBy(c => c.Column).ToList();
        DeploymentColumn[] ascending = [DeploymentColumn.Number, DeploymentColumn.Cost, DeploymentColumn.Refill];
        Assert.All(columns, c => Assert.Equal(
            c.Column == DeploymentColumn.Rows || !c.IsText && !ascending.Contains(c.Column), c.HighFirst));
    }

    [Fact]
    public void The_rows_sort_descending_front_then_front_and_back_then_back_then_none()
    {
        DeploymentRow Unit(int n, string rows) => new(n, "", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, false, 0, Rows: rows);
        DeploymentRow[] rows = [Unit(1, "Back"), Unit(2, ""), Unit(3, "Front, Back"), Unit(4, "Front"), Unit(5, "Back")];

        Assert.Equal([4, 3, 1, 5, 2], DeploymentTable.Sort(rows, DeploymentColumn.Rows, descending: true).Select(r => r.Unit));
        Assert.Equal([2, 1, 5, 3, 4], DeploymentTable.Sort(rows, DeploymentColumn.Rows, descending: false).Select(r => r.Unit));
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

    [Fact]
    public void A_press_joins_with_a_click_on_the_cell_and_takes_a_member_out_with_a_second_left_of_the_list()
    {
        var s = World();
        var cell = GesturePoints.PickerCell(50);
        var bound = GesturePoints.PickerCell(3);
        var outside = GesturePoints.PickerOutside;

        Assert.Equal([(16, cell.X, cell.Y), (0, cell.X, cell.Y)], Gestures(p => DeploymentTable.Press(s, 50, p)));
        Assert.Equal([(16, bound.X, bound.Y), (0, bound.X, bound.Y), (16, outside.X, outside.Y), (0, outside.X, outside.Y)],
            Gestures(p => DeploymentTable.Press(s, 3, p)));                 // Bound is first in the party
        var rest = GesturePoints.PickerCell(51);
        Assert.Equal([(32, rest.X, rest.Y), (0, rest.X, rest.Y)], Gestures(p => DeploymentTable.RightPress(0, p)));
        Assert.Equal(GesturePoints.PickerCell(2), DeploymentTable.PointerAt(2));
        Assert.Equal(rest, DeploymentTable.PointerAt(0));                  // off the rows: the empty 51st cell
    }

    [Fact]
    public void The_details_are_the_hovered_units_as_the_script_shows_them()
    {
        var s = World();
        s.Item_StrBase = 300;
        s.Strings[300 + 6] = "By Lord";
        s.Units[3].Class = 3;
        s.Units[3].Item = 7;

        Assert.Null(DeploymentTable.Details(s));                            // nothing shown (Tmp0026 0)
        s.Tmp0026 = 1;
        s.Tmp0033 = 3;
        Assert.Equal(new DeploymentDetails(3, "Bound", "Thieves", 3, 600, 1000, "Front, Back", "By Lord"), DeploymentTable.Details(s));
        s.Tmp0033 = 4;                                                      // not in the army
        Assert.Null(DeploymentTable.Details(s));
    }

    [Fact]
    public void The_party_has_the_places_the_screen_offers_and_the_scripts_sums()
    {
        var s = World();
        s.Gold = 5;
        s.Tmp0040 = 3;
        s.Units[3].FaceCg = 3;                                              // Bound's face

        var party = DeploymentTable.Party(s);

        Assert.Equal([new DeploymentMember(1, 3, "Bound", 3, 600, "Thieves", 3, 1000), new DeploymentMember(2, 2, "Sill", 2, 300, "Mages", 0, 1000), null], party.Places);
        Assert.Equal(900, party.Troops);
        Assert.Equal(5, party.Cost);                                        // Bound 1 + 600/300, Sill 1 + 300/300
        Assert.True(party.Affordable);
        s.Tmp0040 = 1;                                                      // the sums stop at the last place shown
        Assert.Equal((1, 600, 3), (DeploymentTable.Party(s).Places.Count, DeploymentTable.Party(s).Troops, DeploymentTable.Party(s).Cost));
        s.Tmp0040 = 0;                                                      // a limit the panel never meets: all four
        Assert.Equal(4, DeploymentTable.Party(s).Places.Count);
        s.Gold = 4;
        Assert.False(DeploymentTable.Party(s).Affordable);
    }

    [Fact]
    public void A_drop_swaps_two_members_or_sends_one_to_the_end()
    {
        int[] party = [1, 2, 3];

        Assert.Equal([2, 1, 3], DeploymentTable.Dropped(party, 1, 2));
        Assert.Equal([3, 2, 1], DeploymentTable.Dropped(party, 3, 1));
        Assert.Equal([2, 3, 1], DeploymentTable.Dropped(party, 1, 4));     // onto the empty fourth place: to the end
        Assert.Null(DeploymentTable.Dropped(party, 3, 4));                  // the last member is at the end already
        Assert.Null(DeploymentTable.Dropped(party, 2, 2));
        Assert.Null(DeploymentTable.Dropped(party, 4, 1));                  // no member there
    }

    /// <summary>
    /// Every order of up to four members from every other: the clicks give it as <c>Sﾌﾞﾀｲ</c> moves a
    /// clicked member to the end (CMD-16), and no fewer clicks would (a search over all click series).
    /// </summary>
    [Fact]
    public void The_clicks_of_a_reordering_give_the_wanted_order_with_the_fewest_clicks()
    {
        static List<int> Click(List<int> party, int unit) => [.. party.Where(u => u != unit), unit];
        static IEnumerable<List<int>> Orders(List<int> units) => units.Count == 0 ? [[]]
            : units.SelectMany(u => Orders([.. units.Where(v => v != u)]).Select(rest => (List<int>)[u, .. rest]));

        for (int n = 1; n <= 4; n++)
        {
            var members = Enumerable.Range(1, n).ToList();
            var all = Orders(members).ToList();
            foreach (var from in all)
            {
                // The fewest clicks to every order, breadth first.
                var fewest = new Dictionary<string, int> { [string.Join(",", from)] = 0 };
                var queue = new Queue<List<int>>([from]);
                while (queue.TryDequeue(out var at))
                    foreach (int unit in at)
                    {
                        var next = Click(at, unit);
                        if (fewest.TryAdd(string.Join(",", next), fewest[string.Join(",", at)] + 1))
                            queue.Enqueue(next);
                    }
                foreach (var wanted in all)
                {
                    var clicks = DeploymentTable.Reordering(from, wanted);
                    Assert.Equal(wanted, clicks.Aggregate(from, Click));
                    Assert.Equal(fewest[string.Join(",", wanted)], clicks.Count);
                }
            }
        }
        Assert.Throws<ArgumentException>(() => DeploymentTable.Reordering([1, 2], [1, 3]));
    }

    [Fact]
    public void A_drop_is_a_click_on_each_cell_of_its_reordering()
    {
        var s = World();                                                    // the party: Bound (unit 3), Sill (unit 2)
        s.Bt_PlayerParty3 = 50;
        var sill = GesturePoints.PickerCell(2);
        var bound = GesturePoints.PickerCell(3);
        var last = GesturePoints.PickerCell(50);

        Assert.Equal([3, 2, 50], DeploymentTable.PartyOrder(s));
        IReadOnlyList<int>? wanted = null;
        var gestures = Gestures(p => wanted = DeploymentTable.Drop(s, 1, 3, p));   // Bound onto Last's place
        Assert.Equal([50, 2, 3], wanted);
        Assert.Equal([(16, sill.X, sill.Y), (0, sill.X, sill.Y), (16, bound.X, bound.Y), (0, bound.X, bound.Y)], gestures);   // Last stays first
        gestures = Gestures(p => wanted = DeploymentTable.Drop(s, 1, 2, p));       // [3, 2, 50] → [2, 3, 50]: Sill stays first
        Assert.Equal([2, 3, 50], wanted);
        Assert.Equal([(16, bound.X, bound.Y), (0, bound.X, bound.Y), (16, last.X, last.Y), (0, last.X, last.Y)], gestures);
        Assert.Empty(Gestures(p => Assert.Null(DeploymentTable.Drop(s, 3, 4, p))));
    }

    [Fact]
    public void The_panels_show_the_order_a_drop_asked_for()
    {
        var s = World();
        int[] wanted = [2, 3];

        Assert.Equal([2, 3], DeploymentTable.Party(s, wanted).Places.Where(m => m is not null).Select(m => m!.Unit));
        Assert.Equal([1, 2], DeploymentTable.Party(s, wanted).Places.Where(m => m is not null).Select(m => m!.Place));
        Assert.Equal(DeploymentTable.Party(s).Troops, DeploymentTable.Party(s, wanted).Troops);
        Assert.Equal([0, 1, 2, 0], DeploymentTable.Rows(s, wanted).Select(r => r.PartyPlace));
    }

    /// <summary>The reads of the gestures <paramref name="queue"/> puts on a pointer: button, x, y.</summary>
    private static List<(int, int, int)> Gestures(Action<PointerScript> queue)
    {
        var pointer = new PointerScript { SettleReads = 0 };
        queue(pointer);
        var reads = new List<(int, int, int)>();
        while (pointer.Pending > 0)
        {
            var m = pointer.Next(out _);
            reads.Add((m.Button.Value, m.X.Value, m.Y.Value));
        }
        return reads;
    }
}
