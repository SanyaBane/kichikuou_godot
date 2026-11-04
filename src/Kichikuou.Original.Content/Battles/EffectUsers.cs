using Kichikuou.Original.Content.Strategy;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>A unit of the player's whose plain attack plays an effect, as Debug → Effects lists it.</summary>
/// <param name="Unit">The unit's record.</param>
/// <param name="Name">The unit's name.</param>
/// <param name="TroopType">Its troop type's name.</param>
/// <param name="Sprite">Its battle sprite with its troops full (<c>Unit_CgFull</c>), else alone; 0 none.</param>
/// <param name="WithCommander">
/// The one below the battle's limit (<c>Unit_CgPartial</c>, <c>FIGT.ADV:1651</c>): fewer troops, the commander among them.
/// </param>
/// <param name="Alone">The commander without troops (<c>Unit_CgAlone</c>, <c>:1655</c>).</param>
public readonly record struct EffectUser(int Unit, string Name, string TroopType, int Sprite, int WithCommander, int Alone)
{
    /// <summary>"Maria (unit 16), Cannon Corps".</summary>
    public string Label => TroopType.Length > 0 ? $"{Name} (unit {Unit}), {TroopType}" : $"{Name} (unit {Unit})";
}

/// <summary>
/// Which of the player's units play a plain attack's effect (owner, 2026-11-04: Debug → Effects lists every real user of an
/// attack type, to check that each unit plays the effect that fits it). A unit has one attack type, by its troop class
/// (<c>FIGT.ADV:600-623</c>, <c>Bt_PlayerAtkType</c>): Archers (1) the Indirect attack, Mages (2), Magic Warriors (3) and
/// Demon Magic (11) the Magic attack, any other the Direct attack — so <see cref="ExchangeEffect.Of"/> plays the bow, the
/// caster's element (<see cref="MagicSpells.OfUnit"/>) or the melee for it. A spell its caster shouts may play another
/// element (<see cref="MagicSpells.OfLine"/>); the list goes by the caster's own. Interface only. Engine-independent.
/// </summary>
public static class EffectUsers
{
    /// <summary>The effect unit <paramref name="unit"/>'s plain attack plays, by its class in <paramref name="state"/>.</summary>
    public static ExchangeEffectKind KindOf(GameState state, int unit) => state.Units[unit].Class.Value switch
    {
        1 => ExchangeEffect.IndirectOf(unit),
        2 or 3 or 11 => ExchangeEffect.MagicKindOf(MagicSpells.OfUnit(state, unit)),
        _ => ExchangeEffectKind.Melee,
    };

    /// <summary>The named units (1-50) whose plain attack plays <paramref name="kind"/>, by record.</summary>
    public static IReadOnlyList<EffectUser> Of(GameState state, ExchangeEffectKind kind)
    {
        var users = new List<EffectUser>();
        for (int n = 1; n <= DeploymentTable.LastUnit; n++)
        {
            string name = DeploymentTable.UnitName(state, n).Trim();
            if (name.Length == 0 || KindOf(state, n) != kind)
                continue;
            var u = state.Units[n];
            int sprite = u.CgFull != 0 ? u.CgFull.Value : u.CgAlone.Value;
            users.Add(new EffectUser(n, name, DeploymentTable.TroopType(state, n).Trim(), sprite, u.CgPartial.Value, u.CgAlone.Value));
        }
        return users;
    }
}
