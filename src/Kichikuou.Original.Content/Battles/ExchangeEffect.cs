using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>Which of our effects an exchange plays (docs/ui/battle-screen.md, "The exchange's effects").</summary>
public enum ExchangeEffectKind
{
    /// <summary>Action 0, the Direct attack (after Sengoku Rance's Samurai effect 40000).</summary>
    Melee,
    /// <summary>Action 1, the Indirect attack (after the Archer's 40260).</summary>
    Bow,
    /// <summary>Action 1 by Maria (unit 16), her portable cannons (after Sengoku Rance's Cannoneer effect 40330).</summary>
    Cannon,
    /// <summary>Action 1 by Mill (unit 25), her Phantom Beasts: little phantoms flying at the enemy (ours).</summary>
    Phantoms,
    /// <summary>Action 2, the Magic attack of fire or of a caster without an element (after the Mage's Fire Blast 40360).</summary>
    Magic,
    /// <summary>A Magic attack of ice (docs/ui/mage-lines.md, "The plan: magic effects by element").</summary>
    IceMagic,
    /// <summary>A Magic attack of lightning.</summary>
    LightningMagic,
    /// <summary>A Magic attack of darkness.</summary>
    DarkMagic,
    /// <summary>A Magic attack of light.</summary>
    LightMagic,
    /// <summary>A Magic attack of poison.</summary>
    PoisonMagic,
    /// <summary>The Rance Attack command, Rance's (after Sengoku Rance's own Rance Attack, 40500).</summary>
    RanceAttack,
    /// <summary>The Rance Attack command by Kentarou (after Sengoku Rance's Kentarou-kun Slash, 40800).</summary>
    KentarouSlash,
    /// <summary>Shizuka's White D. Beam (after Sengoku Rance's White Destruction Beam, 40460, placed for her).</summary>
    ShizukaBeam,
    /// <summary>Magic's White D. Beam (the same, placed and turned for her).</summary>
    MagicBeam,
    /// <summary>Gandhi's EPO Light (the same, placed for him).</summary>
    EpoLight,
    /// <summary>Chizuko's Black D. Beam (the White Destruction Beam laid over dark, placed for her).</summary>
    ChizukoBeam,
    /// <summary>Nagi's Black D. Beam (the same without its opening orb).</summary>
    NagiBeam,
    /// <summary>Isoroku's Whirlwind Shot (after Sengoku Rance's Piercing Tempest, 40660).</summary>
    WhirlwindShot,
    /// <summary>Meglass's High Speed (after Sengoku Rance's Commander Charge, 41400).</summary>
    HighSpeed,
    /// <summary>Rick's By la Way (Rance Attack's recoloured red).</summary>
    ByLaWay,
    /// <summary>The ninja's follow-up by a ninja none of the four (after Sengoku Rance's Ninja troop effect, 40100; never in play).</summary>
    NinjaFollowUp,
    /// <summary>Kanami's follow-up (ours, a single weak throw after Sengoku Rance's Sandal Shuriken, 46780).</summary>
    KanamiFollowUp,
    /// <summary>Freya's follow-up (ours, a lash of her whip).</summary>
    FreyaFollowUp,
    /// <summary>Kaoru's follow-up (ours, her fists, after Sengoku Rance's Motonari Punch, 46620).</summary>
    KaoruFollowUp,
    /// <summary>Wichita's follow-up (ours, a cut of her flaming sword, after Sengoku Rance's Fiend Slash, 46800).</summary>
    WichitaFollowUp,
    /// <summary>Miki's Vanish Bomb (after Sengoku Rance's Begone Bomb, 40560).</summary>
    VanishBomb,
    /// <summary>Patton's C. War Dance (after Sengoku Rance's Ieyasu's Special Move, 47400).</summary>
    WarDance,
    /// <summary>Asuka's Chuhai (after Sengoku Rance's Shogun Whirlwind, 47060).</summary>
    Chuhai,
    /// <summary>T2 Goliath's punch (after Sengoku Rance's Gigai Punch, 46820).</summary>
    Goliath,
    /// <summary>Rolex's T2 Soaring Wave (ours: no counterpart in Sengoku Rance).</summary>
    SoaringWave,
}

/// <summary>
/// The effect an exchange plays from its shots' first frame (owner, 2026-10-14): the firing side's, by its action. An
/// exchange is one-sided but for direct against direct — the defending side's action is 99 and it fires nothing — and
/// in a two-sided one only the turn's actor's plays, as Sengoku Rance's blow has one actor and one effect. The actor
/// is read from <c>Bt_MsgMode</c>, which the turn sets before the exchange (3 or 1 on the player's, 4 on the enemy's;
/// <c>FIGT_M</c> turns 1 into 2 for the enemy's magic). The player's specials get Sengoku Rance's counterparts one by
/// one (owner): the Rance Attack command its own (one effect: Kichikuou's is the commander's blow alone, as Sengoku
/// Rance's first phase), Kentarou's his own Kentarou-kun Slash's; the beams (special 2) the White Destruction Beam's, white or
/// dark by the actor; Isoroku's Whirlwind Shot (special 3) Piercing Tempest's, Meglass's High Speed Commander Charge's,
/// the ninja's follow-up each ninja's own, weak (Kanami's throw, Freya's whip, Kaoru's fists, Wichita's flaming sword); Miki's Vanish Bomb Begone Bomb's; Patton's C. War Dance Ieyasu's Special
/// Move's; Asuka's Chuhai Shogun Whirlwind's; T2 Goliath's strike Gigai Punch's; Rolex's T2 Soaring Wave our own; Rick's By la Way Rance Attack's recoloured red; the others play by their action for now.
/// A magic attack plays its element's (owner, 2026-10-21; docs/ui/mage-lines.md): the spell the caster shouted, else
/// the caster's own (<see cref="MagicSpells"/>).
/// </summary>
/// <param name="Kind">The effect.</param>
/// <param name="Side">Whose it is: the enemy's is mirrored, its actor's half on the right.</param>
public readonly record struct ExchangeEffect(ExchangeEffectKind Kind, BattleSide Side)
{
    private const int None = 99;


    /// <summary>
    /// The effect of the exchange the state is in; null when neither side fires. <paramref name="shouted"/> is the
    /// element of the spell a flavour line named before the exchange (<see cref="MagicSpells.OfLine"/>), null when none.
    /// </summary>
    public static ExchangeEffect? Of(GameState s, MagicElement? shouted = null)
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
        if (action == 2)
            return new ExchangeEffect(MagicKindOf(shouted ?? MagicSpells.OfCaster(s, side)), side);
        if (action == 1 && side == BattleSide.Player)
            return new ExchangeEffect(IndirectOf(s.Unit_Cur.Value), side);
        return KindOf(action) is { } kind ? new ExchangeEffect(kind, side) : null;
    }

    /// <summary>
    /// The player's Indirect attack by <paramref name="unit"/>: the cannon for Maria's (unit 16) portable cannons (owner,
    /// 2026-11-04; after Sengoku Rance's Cannoneer effect 40330, which her Tulip plays there), Mill's (unit 25) Phantom
    /// Beasts' little phantoms (owner, 2026-11-04: ours), else the bow.
    /// </summary>
    public static ExchangeEffectKind IndirectOf(int unit) => unit switch
    {
        16 => ExchangeEffectKind.Cannon,
        25 => ExchangeEffectKind.Phantoms,
        _ => ExchangeEffectKind.Bow,
    };

    /// <summary>The magic effect of <paramref name="element"/>.</summary>
    public static ExchangeEffectKind MagicKindOf(MagicElement element) => element switch
    {
        MagicElement.Ice => ExchangeEffectKind.IceMagic,
        MagicElement.Lightning => ExchangeEffectKind.LightningMagic,
        MagicElement.Dark => ExchangeEffectKind.DarkMagic,
        MagicElement.Light => ExchangeEffectKind.LightMagic,
        MagicElement.Poison => ExchangeEffectKind.PoisonMagic,
        _ => ExchangeEffectKind.Magic,
    };

    /// <summary>
    /// The player's special by its setter (<c>Bt_SpecialAtk</c>) and the actor (<c>Unit_Cur</c>); its
    /// <c>Bt_SkillSfx</c> would tell some apart, but the shots' sound selection has reset it by then (<c>FIGTATTK.ADV</c>
    /// <c>L_00796</c>). Special 1 (<c>FIGT.ADV:1507-1515</c>) is the Rance Attack command by Rance (unit 1) or Kentarou
    /// (unit 31), the only ones it is offered to (<c>FIGT_SEL.ADV:579-588</c>) — Rance's plays Rance Attack's,
    /// Kentarou's Kentarou-kun Slash's, his own move in Sengoku Rance (owner, 2026-10-15) — and By la Way by Rick (unit 7,
    /// <c>:589-597</c>), Rance Attack's recoloured red (owner, 2026-10-15). Special 2 (<c>FIGT.ADV:1532-1541</c>), one
    /// setter for three menu items (<c>FIGT_SEL.ADV:608-643, 680</c>): White D. Beam by Shizuka (unit 17) or Magic
    /// (unit 39) and EPO Light by Gandhi (unit 37) play the white beam, each user's own (placed for its sprite; owner,
    /// 2026-10-15), Black D. Beam by Chizuko (unit 38) or Nagi
    /// (unit 40) the dark one (owner, 2026-10-14), each user's own too (owner, 2026-10-15). Special 3 (<c>FIGT.ADV:1542-1550</c>; docs/battle/02-resolution.md
    /// §3.3: the commander's one shot at the commander), one setter for two menu items: Whirlwind Shot by Isoroku
    /// (unit 27, <c>FIGT_SEL.ADV:653</c>) and High Speed by Meglass (unit 30, <c>:599</c>; Commander Charge's, owner,
    /// 2026-10-15); the ninja's follow-up sets special 3 too (<c>FIGT.ADV:1980</c>) with the ninja as record 51
    /// (<c>:1922-1968</c>, Kanami, Freya, Kaoru or Wichita, told apart by its sprite) and plays her own weak effect (owner,
    /// 2026-10-21). Special 4
    /// (<c>FIGT.ADV:1560-1570</c>) is Vanish Bomb, Miki's (unit 32) alone — the menu offers it to her only
    /// (<c>FIGT_SEL.ADV:644</c>) and her involuntary bomb is hers too (<c>FIGT.ADV:1588-1590</c>) — and plays Begone
    /// Bomb's. Special 5 (<c>FIGT.ADV:1571-1577</c>) is C. War Dance, Patton's (unit 18) alone — its menu item
    /// (<c>FIGT_SEL.ADV:449</c>) is offered under <c>Unit_Cur = 18</c> only (<c>:662-664</c>) — and plays Ieyasu's
    /// Special Move's. Special 6 (<c>FIGT.ADV:1551-1559</c>) is Chuhai, Asuka's (unit 11) alone — its menu item
    /// (<c>FIGT_SEL.ADV:464</c>) is offered under <c>Unit_Cur = 11</c> only (<c>:671-673</c>) — and plays Shogun
    /// Whirlwind's. Special 10 is set by T2 Goliath's strike alone (<c>FIGT.ADV:1120-1148</c>, <c>L_01abd</c>: record 51
    /// rebuilt as "T2 Goliath", the player's action 0, the enemy's 99) and plays Gigai Punch's. Special 7 (<c>FIGT.ADV:1525-1531</c>) is T2 Soaring Wave, Rolex's (unit 47) alone (the menu's item 20,
    /// <c>FIGT_SEL.ADV:689</c>) — the only special the enemy answers (its action 0, two-sided; <c>L_02528</c> sets
    /// <c>Bt_MsgMode</c> 3, the player's turn) — and plays our own effect (owner, 2026-10-15). Any other plays by its action.
    /// </summary>
    private static ExchangeEffectKind? SpecialOf(GameState s) => (s.Bt_SpecialAtk.Value, s.Unit_Cur.Value) switch
    {
        (1, 1) => ExchangeEffectKind.RanceAttack,
        (1, 31) => ExchangeEffectKind.KentarouSlash,
        (1, 7) => ExchangeEffectKind.ByLaWay,
        (2, 17) => ExchangeEffectKind.ShizukaBeam,
        (2, 39) => ExchangeEffectKind.MagicBeam,
        (2, 37) => ExchangeEffectKind.EpoLight,
        (2, 38) => ExchangeEffectKind.ChizukoBeam,
        (2, 40) => ExchangeEffectKind.NagiBeam,
        (3, 27) => ExchangeEffectKind.WhirlwindShot,
        (3, 30) => ExchangeEffectKind.HighSpeed,
        (3, 51) => NinjaKindOf(s.Units[51].CgFull.Value),
        (4, 32) => ExchangeEffectKind.VanishBomb,
        (5, 18) => ExchangeEffectKind.WarDance,
        (6, 11) => ExchangeEffectKind.Chuhai,
        (10, 51) => ExchangeEffectKind.Goliath,
        (7, 47) => ExchangeEffectKind.SoaringWave,
        _ => null,
    };

    /// <summary>
    /// The ninja's follow-up by the ninja record 51 holds, told by its sprite (<c>FIGT.ADV:1922-1968</c>): each of the four
    /// plays her own (owner, 2026-10-21).
    /// </summary>
    private static ExchangeEffectKind NinjaKindOf(int sprite) => sprite switch
    {
        1164 => ExchangeEffectKind.KanamiFollowUp,
        1176 => ExchangeEffectKind.FreyaFollowUp,
        1182 => ExchangeEffectKind.KaoruFollowUp,
        1185 => ExchangeEffectKind.WichitaFollowUp,
        _ => ExchangeEffectKind.NinjaFollowUp,
    };

    private static ExchangeEffectKind? KindOf(int action) => action switch
    {
        0 => ExchangeEffectKind.Melee,
        1 => ExchangeEffectKind.Bow,
        _ => null,
    };
}
