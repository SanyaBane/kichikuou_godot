namespace Kichikuou.Original.Content.Strategy;

/// <summary>A tab of the unit ledger (docs/ui/unit-ledger.md): which part of its columns it shows.</summary>
public enum LedgerTab
{
    /// <summary>The unit's troops, what they cost and the item (the owner's first preset).</summary>
    Overview,

    /// <summary>The troops' attack and defense, the strikes, magic and the six situational modifiers.</summary>
    Troops,

    /// <summary>The commander's figures and the item.</summary>
    Commander,
}

/// <summary>A header over some of a tab's columns (<see cref="First"/> to <see cref="Last"/>, indices into its columns).</summary>
public sealed record LedgerGroup(string Name, int First, int Last);

/// <summary>What a tab shows: its columns in order and the groups over them.</summary>
public sealed record LedgerTabColumns(IReadOnlyList<DeploymentColumnInfo> Columns, IReadOnlyList<LedgerGroup> Groups);

/// <summary>
/// The unit ledger's tabs (the owner's B1-B5, docs/ui/unit-ledger.md): the deployment screen's and the Subordinates
/// screen's ledger down the screen's left shows a part of its columns at a time. A tab picks its columns from the
/// screen's own (<see cref="DeploymentTable.Columns"/>, <see cref="SubordinatesScreen.Columns"/>), so each keeps its
/// headers and full names, and leaves out what the screen has not (Subordinates has no Rows). The rows, the sort and the
/// gestures stay the table's: a tab changes only the columns shown.
/// </summary>
public static class LedgerTabs
{
    /// <summary>The tabs in their order.</summary>
    public static IReadOnlyList<LedgerTab> All { get; } = [LedgerTab.Overview, LedgerTab.Troops, LedgerTab.Commander];

    /// <summary>
    /// The columns of each tab, the owner's presets: No. and Name in every one; Rows after Type in Overview (the details
    /// the original shows for a hovered unit on the deployment screen, <c>Sﾌﾞﾀｲ.ADV:524-569</c>); the Item in Overview too,
    /// after Cost, as well as in Commander (owner, 2026-11-04).
    /// </summary>
    private static readonly IReadOnlyDictionary<LedgerTab, DeploymentColumn[]> Presets = new Dictionary<LedgerTab, DeploymentColumn[]>
    {
        [LedgerTab.Overview] =
        [
            DeploymentColumn.Number, DeploymentColumn.Name, DeploymentColumn.TroopType, DeploymentColumn.Rows,
            DeploymentColumn.Troops, DeploymentColumn.Refill, DeploymentColumn.Cost, DeploymentColumn.Item,
        ],
        [LedgerTab.Troops] =
        [
            DeploymentColumn.Number, DeploymentColumn.Name, DeploymentColumn.TroopAtk, DeploymentColumn.TroopDef,
            DeploymentColumn.Attacks, DeploymentColumn.Magic,
            DeploymentColumn.OffAtkMod, DeploymentColumn.OffDefMod, DeploymentColumn.DefAtkMod, DeploymentColumn.DefDefMod,
            DeploymentColumn.DgnAtkMod, DeploymentColumn.DgnDefMod,
        ],
        [LedgerTab.Commander] =
        [
            DeploymentColumn.Number, DeploymentColumn.Name, DeploymentColumn.Strategy, DeploymentColumn.Hp,
            DeploymentColumn.Level, DeploymentColumn.Exp, DeploymentColumn.Atk, DeploymentColumn.Def, DeploymentColumn.Item,
        ],
    };

    /// <summary>
    /// The modifiers' groups (round A's A2): as Off.A … Dng.D the Troops tab's twelve columns run past the ledger's paper,
    /// so each pair stands under its situation's name, headed only Atk and Def.
    /// </summary>
    private static readonly (string Name, DeploymentColumn Attack, DeploymentColumn Defense)[] ModifierGroups =
    [
        ("Offense", DeploymentColumn.OffAtkMod, DeploymentColumn.OffDefMod),
        ("Defense", DeploymentColumn.DefAtkMod, DeploymentColumn.DefDefMod),
        ("Dungeon", DeploymentColumn.DgnAtkMod, DeploymentColumn.DgnDefMod),
    ];

    /// <summary>The tab's name on its paper tab.</summary>
    public static string Title(LedgerTab tab) => tab.ToString();

    /// <summary>
    /// What <paramref name="tab"/> shows of a screen whose columns are <paramref name="screen"/>: the preset's columns the
    /// screen has, in the preset's order, a grouped modifier headed Atk or Def (its full name kept), and the groups.
    /// </summary>
    public static LedgerTabColumns Of(IReadOnlyList<DeploymentColumnInfo> screen, LedgerTab tab)
    {
        var columns = new List<DeploymentColumnInfo>();
        foreach (var column in Presets[tab])
        {
            if (screen.FirstOrDefault(c => c.Column == column) is not { } info)
                continue;
            columns.Add(ModifierGroups.Any(g => g.Attack == column) ? info with { Header = "Atk" }
                : ModifierGroups.Any(g => g.Defense == column) ? info with { Header = "Def" }
                : info);
        }
        var groups = new List<LedgerGroup>();
        foreach (var (name, attack, defense) in ModifierGroups)
        {
            int first = columns.FindIndex(c => c.Column == attack), last = columns.FindIndex(c => c.Column == defense);
            if (first >= 0 && last == first + 1)
                groups.Add(new LedgerGroup(name, first, last));
        }
        return new LedgerTabColumns(columns, groups);
    }

    /// <summary>The tab saved under <paramref name="name"/> (a settings file's text), else <see cref="LedgerTab.Overview"/>.</summary>
    public static LedgerTab Parse(string? name) => Enum.TryParse<LedgerTab>(name, out var tab) && All.Contains(tab) ? tab : LedgerTab.Overview;
}
