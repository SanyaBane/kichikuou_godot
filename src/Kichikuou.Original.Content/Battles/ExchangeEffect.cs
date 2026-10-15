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
    /// <summary>White D. Beam and EPO Light (after Sengoku Rance's White Destruction Beam, 40460).</summary>
    WhiteBeam,
    /// <summary>Black D. Beam (the White Destruction Beam recoloured dark).</summary>
    BlackBeam,
    /// <summary>Isoroku's Whirlwind Shot (after Sengoku Rance's Piercing Tempest, 40660).</summary>
    WhirlwindShot,
    /// <summary>Meglass's High Speed (after Sengoku Rance's Commander Charge, 41400).</summary>
    HighSpeed,
    /// <summary>Rick's By la Way (Rance Attack's recoloured red).</summary>
    ByLaWay,
    /// <summary>The ninja's follow-up (after Sengoku Rance's Ninja troop effect, 40100).</summary>
    NinjaFollowUp,
    /// <summary>Miki's Vanish Bomb (after Sengoku Rance's Begone Bomb, 40560).</summary>
    VanishBomb,
}

/// <summary>
/// The effect an exchange plays from its shots' first frame (owner, 2026-10-14): the firing side's, by its action. An
/// exchange is one-sided but for direct against direct — the defending side's action is 99 and it fires nothing — and
/// in a two-sided one only the turn's actor's plays, as Sengoku Rance's blow has one actor and one effect. The actor
/// is read from <c>Bt_MsgMode</c>, which the turn sets before the exchange (3 or 1 on the player's, 4 on the enemy's;
/// <c>FIGT_M</c> turns 1 into 2 for the enemy's magic). The player's specials get Sengoku Rance's counterparts one by
/// one (owner): the Rance Attack command its own (one effect: Kichikuou's is the commander's blow alone, as Sengoku
/// Rance's first phase), Rance's or Kentarou's alike; the beams (special 2) the White Destruction Beam's, white or
/// dark by the actor; Isoroku's Whirlwind Shot (special 3) Piercing Tempest's, Meglass's High Speed Commander Charge's,
/// the ninja's follow-up the Ninja troop's; Miki's Vanish Bomb Begone Bomb's; Rick's By la Way Rance Attack's recoloured red; the others play by their action for now.
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
        if (side == BattleSide.Player && SpecialOf(s) is { } special)
            return new ExchangeEffect(special, side);
        int action = side == BattleSide.Player ? player : enemy;
        return KindOf(action) is { } kind ? new ExchangeEffect(kind, side) : null;
    }

    /// <summary>
    /// The player's special by its setter (<c>Bt_SpecialAtk</c>) and the actor (<c>Unit_Cur</c>); its
    /// <c>Bt_SkillSfx</c> would tell some apart, but the shots' sound selection has reset it by then (<c>FIGTATTK.ADV</c>
    /// <c>L_00796</c>). Special 1 (<c>FIGT.ADV:1507-1515</c>) is the Rance Attack command by Rance (unit 1) or Kentarou
    /// (unit 31), the only ones it is offered to (<c>FIGT_SEL.ADV:579-588</c>), and By la Way by Rick (unit 7,
    /// <c>:589-597</c>), Rance Attack's recoloured red (owner, 2026-10-15). Special 2 (<c>FIGT.ADV:1532-1541</c>), one
    /// setter for three menu items (<c>FIGT_SEL.ADV:608-643, 680</c>): White D. Beam by Shizuka (unit 17) or Magic
    /// (unit 39) and EPO Light by Gandhi (unit 37) play the white beam, Black D. Beam by Chizuko (unit 38) or Nagi
    /// (unit 40) the dark one (owner, 2026-10-14). Special 3 (<c>FIGT.ADV:1542-1550</c>; docs/battle/02-resolution.md
    /// §3.3: the commander's one shot at the commander), one setter for two menu items: Whirlwind Shot by Isoroku
    /// (unit 27, <c>FIGT_SEL.ADV:653</c>) and High Speed by Meglass (unit 30, <c>:599</c>; Commander Charge's, owner,
    /// 2026-10-15); the ninja's follow-up sets special 3 too (<c>FIGT.ADV:1980</c>) with the ninja as record 51
    /// (<c>:1922-1968</c>, Kanami, Freya, Kaoru or Wichita alike) and plays the Ninja troop's. Special 4
    /// (<c>FIGT.ADV:1560-1570</c>) is Vanish Bomb, Miki's (unit 32) alone — the menu offers it to her only
    /// (<c>FIGT_SEL.ADV:644</c>) and her involuntary bomb is hers too (<c>FIGT.ADV:1588-1590</c>) — and plays Begone
    /// Bomb's. Any other plays by its action.
    /// </summary>
    private static ExchangeEffectKind? SpecialOf(GameState s) => (s.Bt_SpecialAtk.Value, s.Unit_Cur.Value) switch
    {
        (1, 1 or 31) => ExchangeEffectKind.RanceAttack,
        (1, 7) => ExchangeEffectKind.ByLaWay,
        (2, 17 or 39 or 37) => ExchangeEffectKind.WhiteBeam,
        (2, 38 or 40) => ExchangeEffectKind.BlackBeam,
        (3, 27) => ExchangeEffectKind.WhirlwindShot,
        (3, 30) => ExchangeEffectKind.HighSpeed,
        (3, 51) => ExchangeEffectKind.NinjaFollowUp,
        (4, 32) => ExchangeEffectKind.VanishBomb,
        _ => null,
    };

    private static ExchangeEffectKind? KindOf(int action) => action switch
    {
        0 => ExchangeEffectKind.Melee,
        1 => ExchangeEffectKind.Bow,
        2 => ExchangeEffectKind.Magic,
        _ => null,
    };
}
