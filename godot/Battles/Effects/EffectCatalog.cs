using Godot;
using Kichikuou.Content.Graphics;
using Kichikuou.Original.Content.Battles;

namespace Kichikuou.Game.Battles.Effects;

/// <summary>
/// A side's army in an effect's picture: who it is, named, and the battle sprite CG it shows (0: none); for a unit of the
/// game also its other two sprites (<see cref="EffectUser"/>): its troops below the battle's limit with the commander
/// among them, and the commander alone (0: not known).
/// </summary>
internal readonly record struct EffectArmy(string Name, int Cg, int WithCommander = 0, int Alone = 0)
{
    /// <summary>The sprite shown as <paramref name="sprite"/> asks, else <see cref="Cg"/>.</summary>
    public EffectArmy As(EffectSprite sprite) => sprite switch
    {
        EffectSprite.WithCommander when WithCommander != 0 => this with { Cg = WithCommander },
        EffectSprite.Alone when Alone != 0 => this with { Cg = Alone },
        _ => this,
    };
}

/// <summary>Which of a unit's three battle sprites Debug → Effects shows (<c>FIGT.ADV:1647-1655</c>).</summary>
internal enum EffectSprite
{
    /// <summary>The troops below the battle's limit, the commander among them (<c>Unit_CgPartial</c>).</summary>
    WithCommander,
    /// <summary>The troops at the limit (<c>Unit_CgFull</c>), the commander not drawn.</summary>
    Full,
    /// <summary>The commander without troops (<c>Unit_CgAlone</c>).</summary>
    Alone,
}

/// <summary>Where an effect stands in the screen gallery's Combat VFX tab.</summary>
internal enum EffectGroup { Attacks, Magic, Specials, Ninjas, Defence }

/// <summary>
/// One of our exchange effects as Debug → Effects and the screen gallery's Combat VFX tab show it: the list's name, the
/// saved frames' prefix, the builder, laid over dark, the users to pick from (the first the default: the army on the
/// player's side), the enemy's army, whether the enemy casts it (the picture mirrored), its length in frames, and
/// whether play never shows it; a plain attack's or a magic element's <see cref="Plain"/> kind, by which Debug → Effects
/// lists every unit of the game that plays it (<see cref="EffectUsers"/>) in place of <see cref="Users"/>.
/// </summary>
internal sealed record EffectShow(string Name, string Prefix, EffectGroup Group, Func<int, IReadOnlyList<Particle>> Build, bool Dark,
    IReadOnlyList<EffectArmy> Users, EffectArmy? Enemy = null, bool EnemyCasts = false, int Frames = EffectCanvas.Frames,
    bool Unused = false, ExchangeEffectKind? Plain = null)
{
    /// <summary>The enemy's army: <see cref="Enemy"/>, else Ex's (commander 1) Leazas Knights.</summary>
    public EffectArmy EnemyArmy => Enemy ?? EffectCatalog.Ex;

    /// <summary>
    /// The player's side as shown by default, the first user with its commander among its troops where it is a unit of the
    /// game (owner, 2026-11-08: the Combat VFX tab shows the commander, as Debug → Effects does by default).
    /// </summary>
    public EffectArmy User => Users[0].As(EffectSprite.WithCommander);

    /// <summary>The enemy's side as shown by default, its army with its commander among its troops (owner, 2026-11-08).</summary>
    public EffectArmy Opponent => EnemyArmy.As(EffectSprite.WithCommander);

    /// <summary>
    /// The six frames the Combat VFX tab shows (owner, 2026-10-29): every fourth from 2, the part before the tail that the
    /// message skip shows too (2, 6 … 22 of 30); every second from 1 for a short one (1, 3 … 11 of 14 or 15).
    /// </summary>
    public IReadOnlyList<int> GridFrames
    {
        get
        {
            int step = Frames >= EffectCanvas.Frames ? 4 : 2;
            return [.. Enumerable.Range(0, 6).Select(i => step / 2 + i * step)];
        }
    }
}

/// <summary>
/// Our exchange effects and the armies shown with them (docs/ui/battle-screen.md, "The exchange's effects"; the owner's
/// table of 2026-10-29, docs/ui/gallery.md, §11): the plain attacks by Rance's (unit 1) Leazas Knights and Isoroku's
/// (unit 27) archers, each magic element by a caster of it (docs/ui/mage-lines.md, "The elements"), a special by the
/// sprite its user shows while using it (<c>FIGT.ADV:1660-1713</c>, <c>SpecialAttackSprites</c> in <c>Battle.Resolution.cs</c>;
/// the ninjas' follow-ups their own, record 51's <c>CgFull</c>, <c>:1922-1968</c>), against Ex's (commander 1) army. The
/// sprites are the records' as <c>SET_CHR.ADV</c> sets them, Rance's Leazas Knights as the rebellion does
/// (<c>Iﾊﾝﾗﾝ.ADV:27</c>). A unit of the game has its three sprites (<c>CgFull</c>, <c>CgPartial</c>, <c>CgAlone</c>; Hanny
/// King's (unit 48) as <c>Gﾊﾆｰ.ADV:348</c> sets them), so its commander can be shown among its troops (<see cref="EffectShow.User"/>).
/// </summary>
internal static class EffectCatalog
{
    /// <summary>
    /// Ex's (commander 1) Leazas Knights, the enemy's army unless an effect names another; a commander's three sprites are
    /// <c>Cmdr_CgFull</c>, <c>Cmdr_CgPartial</c> and <c>Cmdr_CgAlone</c> (<c>SET_CHR.ADV</c>, <c>FIGT.ADV:1634-1642</c>).
    /// </summary>
    public static readonly EffectArmy Ex = new("Ex (commander 1), Leazas Knights", 1131, 1132, 1133);

    private static readonly EffectArmy RanceKnights = new("Rance (unit 1), Leazas Knights", 1100, 1101, 1102);

    /// <summary>A caster whose troops are the Leazas Mages (CG 1125), with its own sprites with the troops and alone.</summary>
    private static EffectArmy Mages(string who, int withCommander, int alone) => new($"{who}, Leazas Mages", 1125, withCommander, alone);

    public static readonly IReadOnlyList<EffectShow> All =
    [
        new("Melee (after 40000)", "melee", EffectGroup.Attacks, MeleeEffect.Build, false, [RanceKnights], Plain: ExchangeEffectKind.Melee),
        new("Bow (after 40260)", "bow", EffectGroup.Attacks, BowEffect.Build, false, [new("Isoroku (unit 27), JAPAN Archers", 1209, 1210, 1211)],
            Plain: ExchangeEffectKind.Bow),
        new("Cannon (after 40330)", "cannon", EffectGroup.Attacks, CannonEffect.Build, false, [new("Maria (unit 16), Maria's Pupils", 1167, 1168, 1169)],
            Plain: ExchangeEffectKind.Cannon),
        new("Phantoms (ours)", "phantoms", EffectGroup.Attacks, PhantomEffect.Build, false, [new("Mill (unit 25), Phantom Beasts", 1179, 1180, 1181)],
            Plain: ExchangeEffectKind.Phantoms),
        new("Magic: fire (after 40360)", "magic", EffectGroup.Magic, MagicEffect.Build, false, [Mages("Sill (unit 2)", 1292, 1293)],
            Plain: ExchangeEffectKind.Magic),
        new("Ice Magic (after 40730)", "ice-magic", EffectGroup.Magic, IceEffect.Build, false, [Mages("Melfeis (unit 13)", 1126, 1127)],
            Plain: ExchangeEffectKind.IceMagic),
        new("Lightning Magic (after 40630)", "lightning-magic", EffectGroup.Magic, LightningEffect.Build, false,
            [new("Somita (unit 46), Kalars", 1450, 1451, 1452), new("Eleanor (unit 23), Magic Warriors", 1173, 1174, 1175),
                new("Hanty (unit 19), Freak Robots", 1240, 1241, 1242)],
            Plain: ExchangeEffectKind.LightningMagic),
        new("Dark Magic (after 47030)", "dark-magic", EffectGroup.Magic, DarkEffect.Build, true,
            [new("Satella (unit 29), Guardians", 1316, 1317, 1318), Mages("Chizuko (unit 38)", 1392, 1393), Mages("Miki (unit 32)", 1338, 1339)],
            Plain: ExchangeEffectKind.DarkMagic),
        new("Light Magic (after 40430)", "light-magic", EffectGroup.Magic, LightEffect.Build, false,
            [new("Seizel Sisters (unit 42), False Angels", 1514, 1405, 1413), new("Hanny King (unit 48)", 1517, 1518, 1519)],
            Plain: ExchangeEffectKind.LightMagic),
        // No unit of ours casts poison: Papaya (commander 104) and Ragarl (commander 109) do (docs/ui/mage-lines.md).
        new("Poison Magic (after 41300)", "poison-magic", EffectGroup.Magic, PoisonEffect.Build, false, [RanceKnights],
            new("Papaya (commander 104), Zeth Mages", 1343, 1362, 1363), EnemyCasts: true, Plain: ExchangeEffectKind.PoisonMagic),
        new("Rance Attack (after 40500)", "rance-attack", EffectGroup.Specials, RanceAttackEffect.Build, false, [new("Rance (unit 1)", 1520)]),
        new("Kentarou-kun Slash (after 40800)", "kentarou-slash", EffectGroup.Specials, KentarouSlashEffect.Build, false, [new("Kentarou (unit 31)", 1222)]),
        new("White Beam: Shizuka (after 40460)", "white-beam-shizuka", EffectGroup.Specials, ShizukaBeamEffect.Build, false, [new("Shizuka (unit 17)", 1522)]),
        new("White Beam: Magic (after 40460)", "white-beam-magic", EffectGroup.Specials, MagicBeamEffect.Build, false, [new("Magic (unit 39)", 1523)]),
        new("EPO Light: Gandhi (after 40460)", "epo-light", EffectGroup.Specials, EpoLightEffect.Build, false, [new("Gandhi (unit 37)", 1524)]),
        new("Black Beam: Chizuko (40460 dark)", "black-beam-chizuko", EffectGroup.Specials, ChizukoBeamEffect.Build, true, [new("Chizuko (unit 38)", 1526)]),
        new("Black Beam: Nagi (40460 dark)", "black-beam-nagi", EffectGroup.Specials, NagiBeamEffect.Build, true, [new("Nagi (unit 40)", 1525)]),
        new("Whirlwind Shot (after 40660)", "whirlwind-shot", EffectGroup.Specials, WhirlwindShotEffect.Build, false, [new("Isoroku (unit 27)", 1528)]),
        new("High Speed (after 41400)", "high-speed", EffectGroup.Specials, HighSpeedEffect.Build, false, [new("Meglass (unit 30)", 1527)]),
        new("By la Way (40500 red)", "by-la-way", EffectGroup.Specials, RanceAttackEffect.BuildRed, false, [new("Rick (unit 7)", 1521)]),
        new("Ninja troop (after 40100), unused", "ninja", EffectGroup.Ninjas, NinjaEffect.Build, false, [RanceKnights], Unused: true),
        new("Ninja: Kanami (ours, after 46780)", "kanami", EffectGroup.Ninjas, KanamiEffect.Build, false, [new("Kanami", 1164)]),
        new("Ninja: Freya (ours, the whip)", "freya", EffectGroup.Ninjas, FreyaEffect.Build, false, [new("Freya", 1176)]),
        new("Ninja: Kaoru (ours, after 46620)", "kaoru", EffectGroup.Ninjas, KaoruEffect.Build, false, [new("Kaoru", 1182)]),
        new("Ninja: Wichita (ours, after 46800)", "wichita", EffectGroup.Ninjas, WichitaEffect.Build, false, [new("Wichita", 1185)]),
        new("Vanish Bomb (after 40560)", "vanish-bomb", EffectGroup.Specials, VanishBombEffect.Build, false, [new("Miki (unit 32)", 1529)]),
        new("C. War Dance (after 47400)", "war-dance", EffectGroup.Specials, WarDanceEffect.Build, false, [new("Patton (unit 18)", 1530)]),
        new("Chuhai (after 47060)", "chuhai", EffectGroup.Specials, ChuhaiEffect.Build, false, [new("Asuka (unit 11)", 1121)]),
        new("T2 Goliath (after 46820)", "goliath", EffectGroup.Specials, GoliathEffect.Build, false,
            [new("T2 Goliath (record 51), no sprite", 0)], Frames: 15),
        new("T2 Soaring Wave (ours)", "soaring-wave", EffectGroup.Specials, SoaringWaveEffect.Build, false, [new("Rolex (unit 47)", 1252)]),
        new("Guard: Defend (after 45080)", "guard", EffectGroup.Defence, GuardEffect.Build, false, [RanceKnights], Frames: 14),
    ];

    /// <summary>
    /// The battlefield as our scene shows it between exchanges (<see cref="BattleScene"/>'s order: CG 900, the enemy's
    /// army <paramref name="enemy"/>, the player's <paramref name="player"/>, the overlay 901; a sprite 0 is none),
    /// 640×256; null when the CGs are not there.
    /// </summary>
    public static Image? Battlefield(CgLibrary cgs, int player, int enemy)
    {
        if (cgs.Get(900) is not { } field)
            return null;
        var layers = new List<(CgImage Cg, bool Keyed)> { (field, false) };
        foreach (int sprite in new[] { enemy, player })
        {
            if (sprite != 0 && cgs.Get(sprite) is { } army)
                layers.Add((army, true));
        }
        if (cgs.Get(901) is { } overlay)
            layers.Add((overlay, true));
        var palette = Palette.Initial;
        foreach (var (cg, _) in layers)
            palette = cg.PaletteOver(palette);
        var rgba = new byte[BattleScene.Width * BattleScene.Height * 4];
        foreach (var (cg, keyed) in layers)
        {
            var pixels = cg.ToRgba(keyed ? 40 : null, palette);
            for (int y = Math.Max(0, cg.Y); y < Math.Min(BattleScene.Height, cg.Y + cg.Height); y++)
            {
                for (int x = Math.Max(0, cg.X); x < Math.Min(BattleScene.Width, cg.X + cg.Width); x++)
                {
                    int from = ((y - cg.Y) * cg.Width + x - cg.X) * 4, to = (y * BattleScene.Width + x) * 4;
                    if (pixels[from + 3] != 0)
                        Array.Copy(pixels, from, rgba, to, 4);
                }
            }
        }
        return Image.CreateFromData(BattleScene.Width, BattleScene.Height, false, Image.Format.Rgba8, rgba);
    }
}
