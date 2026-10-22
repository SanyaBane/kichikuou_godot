using Kichikuou.Engine.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// Ours (owner's decision 30, docs/ui/talk-marks.md §4): which parts of the state count when a talk or a visit is
/// marked — a path that changes one of them, by value, has something new — and the differences between a path's end
/// and its start. Everything counts (every variable, every record slot, the records' names) except:
/// <list type="bullet">
/// <item>the bookkeeping (<see cref="IsBookkeeping"/>): the dictionary's groups <c>scratch</c>, <c>ui</c>,
/// <c>system</c>, <c>array</c>, <c>unused</c> and <c>debug</c> (the temporaries, the window, map and camera state, the
/// mouse, the harem's availability flags, <c>Harem_LastPick</c>, the week's command flags, <c>RetVal</c>,
/// <c>CallArg</c>, the persistent page with <c>Glob_CgSeen</c>), and by name the record cursors and strides, the string
/// bases, <c>Ev_Entry</c>, <c>Harem_ExpReward</c>, <c>Harem_SexCount</c>, <c>Harem_LastWeek</c>, the week's talk and
/// troop orders (<c>CmdAvail_SubTalk</c>, <c>CmdAvail_SubTroops</c>) and Rance's (unit 1) self-talk counter
/// <c>N_RanceSelfTalk</c> (the owner: no mark on his talks 2-9 and 11-19);</item>
/// <item>for a visit, what every visit pays or earns (<see cref="VisitFeeNames"/>): Rance's experience, Rebecca's
/// <c>Bt_ExpBonusNext</c>, Shariela's <c>Shariela_EvWeek</c>, Lia's <c>N_Lia_Neglect</c>;</item>
/// <item>the strings outside the records' names (strings 1-19 and the rest are the scripts' scratch), and the port's own
/// data (<see cref="Engine.Script.ScriptState.Extras"/>: the read lines, the provinces seen).</item>
/// </list>
/// A variable bound onto a page (<c>DS</c>) is compared on its page, slot by slot; a page slot takes the class of the
/// field that covers it, an unbound one counts unless every field of its page is bookkeeping.
/// </summary>
public static class SceneMarkState
{
    /// <summary>The dictionary groups whose variables (and page fields) are bookkeeping.</summary>
    public static readonly IReadOnlySet<string> BookkeepingGroups = new HashSet<string>(StringComparer.Ordinal)
    {
        "scratch", "ui", "system", "array", "unused", "debug",
    };

    /// <summary>The variables in counted groups that are bookkeeping too (docs/ui/talk-marks.md §4).</summary>
    public static readonly IReadOnlySet<string> BookkeepingNames = new HashSet<string>(StringComparer.Ordinal)
    {
        "Unit_Cur", "Unit_Count", "Cmdr_Cur", "Cmdr_Count", "Prov_Cur", "Prov_Count", "Item_Cur", "Item_Count",
        "Dgn_Cur", "Dgn_Count", "Hist_Cur", "Hist_Count", "Hist_PrevCur",
        "Unit_StrBase", "Cmdr_StrBase", "Prov_StrBase", "Item_StrBase",
        "Ev_Entry", "Harem_ExpReward", "Harem_SexCount", "Harem_LastWeek",
        "CmdAvail_SubTalk", "CmdAvail_SubTroops",
        "N_RanceSelfTalk",
    };

    /// <summary>
    /// What every visit pays or earns (docs/ui/talk-marks.md §4, the owner: as proposed): Rance's (unit 1) experience
    /// from the harem, Rebecca's <c>Bt_ExpBonusNext</c> (<c>Gﾚﾍﾞｯｶ:14</c>), Shariela's <c>Shariela_EvWeek</c>
    /// (<c>Gｼｬﾘｴﾗ:18</c>) and Lia's <c>N_Lia_Neglect</c> (Sill's visit raises it, Lia's resets it). Not counted on a
    /// visit; a talk counts them.
    /// </summary>
    public static readonly IReadOnlyList<string> VisitFeeNames = ["Bt_ExpBonusNext", "Shariela_EvWeek", "N_Lia_Neglect"];

    /// <summary>The page of <c>Units[1].Exp</c>, Rance's experience, a visit's fee too (<see cref="VisitFeeNames"/>).</summary>
    private const int RanceExpPage = 1;

    private static readonly bool[] CountedVariables = BuildVariables();

    private static readonly Dictionary<int, bool[]> CountedSlots = BuildSlots();

    private static readonly int[] VisitFeeVariables = [.. VisitFeeNames.Select(Number)];

    private static readonly int RanceExpSlot = Binding("Units", "Exp").Offset + 1;

    /// <summary>Whether variable <paramref name="number"/> is bookkeeping (by its group or its name).</summary>
    public static bool IsBookkeeping(int number)
    {
        var info = VariableCatalog.Variables[number];
        return BookkeepingGroups.Contains(info.Group) || BookkeepingNames.Contains(info.Name);
    }

    /// <summary>Whether variable <paramref name="number"/> counts on a path of <paramref name="kind"/>.</summary>
    public static bool Counts(int number, MarkTargetKind kind) =>
        CountedVariables[number] && !(kind == MarkTargetKind.Visit && VisitFeeVariables.Contains(number));

    /// <summary>Whether slot <paramref name="index"/> of page <paramref name="page"/> counts on a path of <paramref name="kind"/>.</summary>
    public static bool CountsSlot(int page, int index, MarkTargetKind kind) =>
        CountedSlots.TryGetValue(page, out var slots) && index < slots.Length && slots[index]
        && !(kind == MarkTargetKind.Visit && page == RanceExpPage && index == RanceExpSlot);

    /// <summary>
    /// Every part of the state that counts and whose value differs between <paramref name="start"/> and
    /// <paramref name="end"/> (docs/ui/talk-marks.md §4: values, not writes): variables, page slots, the records'
    /// names. A page allocated on one side only, or at another size, differs in each slot past the shorter one.
    /// </summary>
    public static List<StateChange> Changes(GameState start, GameState end, MarkTargetKind kind)
    {
        var changes = new List<StateChange>();
        for (int v = 0; v < CountedVariables.Length; v++)
        {
            if (!Counts(v, kind))
                continue;
            int before = start.Memory.Var(v).Value, after = end.Memory.Var(v).Value;
            if (before != after)
                changes.Add(new StateChange(StateChange.Variables, v, before.ToString(), after.ToString()));
        }
        foreach (var (page, slots) in CountedSlots)
        {
            int startSize = start.Memory.PageSize(page), endSize = end.Memory.PageSize(page);
            int size = Math.Max(startSize, endSize);
            for (int i = 0; i < size; i++)
            {
                if (!(i < slots.Length ? slots[i] : true) || kind == MarkTargetKind.Visit && page == RanceExpPage && i == RanceExpSlot)
                    continue;
                int? before = i < startSize ? start.Memory.ReadPage(page, i).Value : null;
                int? after = i < endSize ? end.Memory.ReadPage(page, i).Value : null;
                if (before != after)
                    changes.Add(new StateChange(page, i, before?.ToString() ?? "", after?.ToString() ?? ""));
            }
        }
        var (first, last) = NameStrings(start);
        for (int s = first; s < last && s < start.Strings.Length && s < end.Strings.Length; s++)
            if (!string.Equals(start.Strings[s], end.Strings[s], StringComparison.Ordinal))
                changes.Add(new StateChange(StateChange.Strings, s, start.Strings[s], end.Strings[s]));
        return changes;
    }

    /// <summary>
    /// The strings of the records' names, from the first unit's to the last item's (<c>SET.ADV:298-301</c>: units,
    /// commanders, provinces, items in a row); some scenes rename a record (<c>MM Unit_StrBase…</c>).
    /// </summary>
    public static (int First, int End) NameStrings(GameState state) =>
        (state.Unit_StrBase.Value, state.Item_StrBase.Value + state.Item_Count.Value);

    private static bool[] BuildVariables()
    {
        var counted = new bool[VariableCatalog.Variables.Count];
        var bound = VariableCatalog.Bindings.Select(b => b.Var).ToHashSet();
        for (int v = 0; v < counted.Length; v++)
            counted[v] = !IsBookkeeping(v) && !bound.Contains(v);     // a bound variable lives on its page
        foreach (string name in BookkeepingNames.Concat(VisitFeeNames))
            _ = Number(name);                                           // every name must be in the dictionary
        return counted;
    }

    private static Dictionary<int, bool[]> BuildSlots()
    {
        var slots = new Dictionary<int, bool[]>();
        foreach (var (page, size) in VariableCatalog.Pages)
        {
            var onPage = VariableCatalog.Bindings.Where(b => b.Page == page).ToList();
            bool unboundCounts = onPage.Any(b => !IsBookkeeping(b.Var));
            if (!unboundCounts)
                continue;                                               // a page of bookkeeping alone
            var counted = new bool[size];
            for (int i = 0; i < size; i++)
                counted[i] = VariableCatalog.FindSlot(page, i) is { } b ? !IsBookkeeping(b.Var) : unboundCounts;
            slots[page] = counted;
        }
        return slots;
    }

    private static int Number(string name)
    {
        for (int v = 0; v < VariableCatalog.Variables.Count; v++)
            if (VariableCatalog.Variables[v].Name == name)
                return v;
        throw new InvalidOperationException($"{name} is not in the variable dictionary.");
    }

    private static PageBinding Binding(string table, string field) =>
        VariableCatalog.Bindings.First(b => b.Table == table && b.Field == field);
}

/// <summary>
/// A part of the state that differs between a path's end and its start (<see cref="SceneMarkState.Changes"/>): in
/// <paramref name="Space"/> — <see cref="Variables"/>, <see cref="Strings"/> or an array page's number — at
/// <paramref name="Index"/>, from <paramref name="Before"/> to <paramref name="After"/> (a missing page slot is "").
/// </summary>
public readonly record struct StateChange(int Space, int Index, string Before, string After)
{
    /// <summary>The space of the variables.</summary>
    public const int Variables = 0;

    /// <summary>The space of the strings.</summary>
    public const int Strings = -1;

    /// <summary>Where it is, readably: <c>VAR0397 F_Sill_State</c>, <c>Units[12].Status</c>, <c>string 25</c>.</summary>
    public string Slot => Space switch
    {
        Variables => VariableCatalog.DescribeVar(Index),
        Strings => $"string {Index}",
        _ => VariableCatalog.DescribeSlot(Space, Index),
    };

    /// <inheritdoc />
    public override string ToString() => $"{Slot}: {Before} → {After}";
}
