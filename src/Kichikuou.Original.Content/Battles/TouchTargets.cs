namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// The target our touch mode preselects for the acting unit (owner, 2026-10-24; docs/ui/touch.md, "The tablet"): each
/// unit's last attacked enemy, remembered as the enemy (its record), not its card, since the cards close up when one
/// falls; a unit that has not attacked yet in this battle takes the last enemy any unit attacked; the battle's first
/// takes the first enemy card. An enemy no attack of the acting unit reaches is passed over for the next in that
/// chain. Interface only: the attack is still the player's tap, and every choice one the core offers.
/// Engine-independent.
/// </summary>
public sealed class TouchTargets
{
    // An enemy as attacked: its record, and its slot then (two enemies of one record — a dungeon's monsters — are
    // told apart by the slot while it still holds that record).
    private readonly record struct Enemy(int Record, int Slot);

    private readonly Dictionary<int, Enemy> _byUnit = [];
    private Enemy? _last;

    /// <summary>Forgets every target: a new battle.</summary>
    public void Clear()
    {
        _byUnit.Clear();
        _last = null;
    }

    /// <summary>Unit <paramref name="unit"/> (its record) attacked the enemy of record <paramref name="enemy"/> in slot <paramref name="slot"/>.</summary>
    public void Attacked(int unit, int enemy, int slot)
    {
        var e = new Enemy(enemy, slot);
        _byUnit[unit] = e;
        _last = e;
    }

    /// <summary>
    /// The enemy slot (1-4) to preselect for <paramref name="unit"/>, or null when the enemy side is empty.
    /// <paramref name="enemies"/> holds the enemy slots' records (slot 1 first, 0 empty); <paramref name="reaches"/>
    /// tells whether one of the unit's attacks reaches a slot.
    /// </summary>
    public int? Preselect(int unit, IReadOnlyList<int> enemies, Func<int, bool> reaches)
    {
        int? Find(Enemy? e)
        {
            if (e is not { } wanted)
                return null;
            int? slot = wanted.Slot >= 1 && wanted.Slot <= enemies.Count && enemies[wanted.Slot - 1] == wanted.Record
                ? wanted.Slot
                : Enumerable.Range(1, enemies.Count).Where(n => enemies[n - 1] == wanted.Record).Cast<int?>().FirstOrDefault();
            return slot is { } n && reaches(n) ? n : null;
        }

        var occupied = Enumerable.Range(1, enemies.Count).Where(n => enemies[n - 1] != 0).ToList();
        return (_byUnit.TryGetValue(unit, out var own) ? Find(own) : null)
            ?? Find(_last)
            ?? occupied.Where(reaches).Cast<int?>().FirstOrDefault()
            ?? occupied.Cast<int?>().FirstOrDefault();
    }
}
