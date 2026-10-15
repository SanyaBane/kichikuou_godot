using Kichikuou.Engine.Sys3;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>
/// The stats our battle cards show under the portrait, always (owner, 2026-10-15, after a Sengoku Rance fan mod;
/// docs/ui/battle-screen.md, "The cards' stats"): what the original's info box prints (<c>FIGT.ADV:834-1119</c>,
/// docs/battle/screen-layout.md §6.1) but HP and troops, which the card shows already, and the standing order — the troop
/// attack and defense with the battle kind's modifiers (the commander's own attack and defense without troops), magic,
/// the number of attacks; the player's Battle LV; and whether the unit's partner is deployed ("Surging with power").
/// </summary>
/// <param name="Level">The player unit's Battle LV (<c>Unit_Level</c>); null for an enemy commander, which has none.</param>
/// <param name="Attack">Troop attack, or the commander's own attack without troops (<see cref="Commander"/>).</param>
/// <param name="Defense">Troop defense, or the commander's own defense without troops.</param>
/// <param name="AttackModifier">The troop attack's modifier as the info box prints it (<see cref="Modifier"/>); 0 without troops.</param>
/// <param name="DefenseModifier">The troop defense's modifier, the same way.</param>
/// <param name="Magic">Magic attack/defense.</param>
/// <param name="Attacks">The number of attacks.</param>
/// <param name="Commander">Whether the unit has no troops: attack and defense are the commander's own (<c>:883-899</c>).</param>
/// <param name="Surging">"Surging with power": a player unit whose partner stands in a slot (<c>:1001-1004</c>).</param>
public sealed record BattleCardStats(int? Level, int Attack, int Defense, int AttackModifier, int DefenseModifier,
    int Magic, int Attacks, bool Commander, bool Surging)
{
    /// <summary>
    /// Whether the partner's bonus applies now: it raises the troops' attack and defense rolls only (<c>FIGT.ADV:2457-2482</c>),
    /// so a unit without troops gets nothing from it, though the info box still prints the line.
    /// </summary>
    public bool SurgingApplies => Surging && !Commander;

    /// <summary>A player unit's stats.</summary>
    public static BattleCardStats OfUnit(GameState s, int record)
    {
        var u = s.Units[record];
        Sys3Int partner = u.Partner;
        bool surging = partner != 0 && (partner == s.Bt_PlayerSlot1 || partner == s.Bt_PlayerSlot2
            || partner == s.Bt_PlayerSlot3 || partner == s.Bt_PlayerSlot4);
        if (u.Troops == 0)
            return new(u.Level.Value, u.Atk.Value, u.Def.Value, 0, 0, u.Magic.Value, u.Attacks.Value, true, surging);
        // The modifiers of the battle's kind (:900-989); none printed outside the three kinds.
        var (atk, def) = s.Bt_Kind.Value switch
        {
            1 => (Modifier(u.OffAtkMod), Modifier(u.OffDefMod)),
            2 => (Modifier(u.DefAtkMod), Modifier(u.DefDefMod)),
            3 => (Modifier(u.DgnAtkMod), Modifier(u.DgnDefMod)),
            _ => (0, 0),
        };
        return new(u.Level.Value, u.TroopAtk.Value, u.TroopDef.Value, atk, def, u.Magic.Value, u.Attacks.Value, false, surging);
    }

    /// <summary>An enemy commander's stats.</summary>
    public static BattleCardStats OfCommander(GameState s, int record)
    {
        var c = s.Cmdrs[record];
        if (c.Troops == 0)
            return new(null, c.Atk.Value, c.Def.Value, 0, 0, c.Magic.Value, c.Attacks.Value, true, false);
        // QUIRK: BRS-08 — the box decodes the enemy's modifiers as the player's (11-19 as minus); the battle adds them raw.
        return new(null, c.TroopAtk.Value, c.TroopDef.Value, Modifier(c.TroopAtkMod), Modifier(c.TroopDefMod),
            c.Magic.Value, c.Attacks.Value, false, false);
    }

    /// <summary>
    /// A troop modifier as the info box prints it (<c>:900-989</c>): below 10 +m, above it −(m − 10); 10, which the box
    /// prints as a lone ")" (BSC-04), is 0 — the modifier it stands for.
    /// </summary>
    public static int Modifier(Sys3Int m) => m > 10 ? -(m.Value - 10) : m < 10 ? m.Value : 0;
}
