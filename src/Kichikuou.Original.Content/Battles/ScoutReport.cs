using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// One commander of a ninja's scout report: what the report prints for it (<c>L_00dad</c>,
/// <c>FIGT_B.ADV:649-672</c>) — its name, and while it has troops their type and number — with its place in the
/// battle to come and the picture our cards cut for it. No maximum and no HP: the report does not reveal them.
/// </summary>
/// <param name="Slot">The place it will take in the battle (1-2 the front row, 3-4 the back row).</param>
/// <param name="Record">The commander's record number.</param>
/// <param name="Name">The record's first string.</param>
/// <param name="TroopType">The record's second string; empty when it has no troops (the report leaves the line out).</param>
/// <param name="Picture">The CG our screens cut for it: the face, else its battle sprite (<see cref="BattleCard.Picture"/>); 0 none.</param>
/// <param name="Troops">Its troops (0: none).</param>
public sealed record ScoutedCommander(int Slot, int Record, string Name, string TroopType, int Picture, int Troops);

/// <summary>
/// A ninja's scout report (<c>FIGT_B.ADV:506-648</c>, <c>NinjaScoutReportAsync</c>) as our own overlay shows it
/// (docs/ui/battle-screen.md, "The scout report"): the approaching army's name (string 10), its commanders in the
/// places the battle will give them, and the totals the script sums (<c>Tmp0002</c> commanders, <c>Tmp0001</c>
/// troops). The report lists <c>Bt_EnemyParty1-4</c> in order, the empty ones skipped, and the battle's
/// <c>InitBattle</c> puts party member i into enemy slot i.
/// </summary>
/// <param name="NinjaCg">The reporting ninja's portrait (the op-321 picture, <c>CallArg</c>: Kanami 9).</param>
/// <param name="Army">The approaching army's name (string 10).</param>
/// <param name="Places">Places 1-4 (index 0-3); an empty one is null.</param>
public sealed record ScoutReport(int NinjaCg, string Army, IReadOnlyList<ScoutedCommander?> Places)
{
    /// <summary>The commanders reported (<c>Tmp0002</c>).</summary>
    public int Commanders => Places.Count(p => p is not null);

    /// <summary>Their troops together, summed as the script does (<c>Tmp0001</c>, 16-bit saturating).</summary>
    public int Troops => Places.Aggregate(Sys3Int.Zero, (sum, p) => p is null ? sum : sum + (Sys3Int)p.Troops).Value;

    /// <summary>The report as GameState holds the approaching army now, told by the ninja whose portrait is <paramref name="ninjaCg"/>.</summary>
    public static ScoutReport Of(GameState s, int ninjaCg)
    {
        var places = new ScoutedCommander?[4];
        for (int slot = 1; slot <= 4; slot++)
        {
            int record = Party(s, slot).Value;
            if (record == 0)
                continue;
            var (name, troopType, face, standIn) = BattleBoard.Commander(s, record);
            int troops = s.Cmdrs[record].Troops.Value;
            places[slot - 1] = new ScoutedCommander(slot, record, name, troops == 0 ? "" : troopType,
                face != 0 ? face : standIn, troops);
        }
        return new ScoutReport(ninjaCg, BattleBoard.Armies(s).Enemy, places);
    }

    private static Sys3Int Party(GameState s, int slot) => slot switch
    {
        1 => s.Bt_EnemyParty1,
        2 => s.Bt_EnemyParty2,
        3 => s.Bt_EnemyParty3,
        _ => s.Bt_EnemyParty4,
    };

    /// <inheritdoc/>
    public bool Equals(ScoutReport? other) =>
        other is not null && NinjaCg == other.NinjaCg && Army == other.Army && Places.SequenceEqual(other.Places);

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(NinjaCg, Army, Places.Count);
}
