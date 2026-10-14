using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>Which of our effects an exchange plays (docs/ui/battle-screen.md, "The exchange's effects").</summary>
public enum ExchangeEffectKind
{
    /// <summary>Action 0, the Direct attack (after Sengoku Rance's Samurai effect 40000).</summary>
    Melee,
    /// <summary>Action 1, the Indirect attack (after the Archer's 40260).</summary>
    Bow,
    /// <summary>Action 2, the Magic attack (after the Mage's Fire Blast 40360).</summary>
    Magic,
    /// <summary>The Rance Attack command, Rance's or Kentarou's (after Sengoku Rance's own Rance Attack, 40500).</summary>
    RanceAttack,
}

/// <summary>
/// The effect an exchange plays from its shots' first frame (owner, 2026-10-14): the firing side's, by its action. An
/// exchange is one-sided but for direct against direct — the defending side's action is 99 and it fires nothing — and
/// in a two-sided one only the turn's actor's plays, as Sengoku Rance's blow has one actor and one effect. The actor
/// is read from <c>Bt_MsgMode</c>, which the turn sets before the exchange (3 or 1 on the player's, 4 on the enemy's;
/// <c>FIGT_M</c> turns 1 into 2 for the enemy's magic). The player's specials get Sengoku Rance's counterparts one by
/// one (owner): the Rance Attack command its own (one effect: Kichikuou's is the commander's blow alone, as Sengoku
/// Rance's first phase), Rance's or Kentarou's alike; the others play by their action for now.
/// </summary>
/// <param name="Kind">The effect.</param>
/// <param name="Side">Whose it is: the enemy's is mirrored, its actor's half on the right.</param>
public readonly record struct ExchangeEffect(ExchangeEffectKind Kind, BattleSide Side)
{
    private const int None = 99;

    /// <summary>The effect of the exchange the state is in; null when neither side fires.</summary>
    public static ExchangeEffect? Of(GameState s)
    {
        int player = s.Bt_PlayerAction.Value, enemy = s.Bt_EnemyAction.Value;
        bool playerFires = player != None, enemyFires = enemy != None;
        if (!playerFires && !enemyFires)
            return null;
        var side = playerFires && enemyFires
            ? s.Bt_MsgMode.Value is 2 or 4 ? BattleSide.Enemy : BattleSide.Player
            : playerFires ? BattleSide.Player : BattleSide.Enemy;
        if (side == BattleSide.Player && IsRanceAttack(s))
            return new ExchangeEffect(ExchangeEffectKind.RanceAttack, side);
        int action = side == BattleSide.Player ? player : enemy;
        return KindOf(action) is { } kind ? new ExchangeEffect(kind, side) : null;
    }

    /// <summary>
    /// The Rance Attack command (<c>FIGT.ADV:1507-1515</c>): special 1 by Rance (unit 1) or Kentarou (unit 31), the
    /// only ones it is offered to (<c>FIGT_SEL.ADV:579-588</c>); special 1 by anyone else is Rick's By la Way. Its
    /// <c>Bt_SkillSfx</c> would tell them apart, but the shots' sound selection has reset it by then
    /// (<c>FIGTATTK.ADV</c> <c>L_00796</c>).
    /// </summary>
    private static bool IsRanceAttack(GameState s) => s.Bt_SpecialAtk.Value == 1 && s.Unit_Cur.Value is 1 or 31;

    private static ExchangeEffectKind? KindOf(int action) => action switch
    {
        0 => ExchangeEffectKind.Melee,
        1 => ExchangeEffectKind.Bow,
        2 => ExchangeEffectKind.Magic,
        _ => null,
    };
}
