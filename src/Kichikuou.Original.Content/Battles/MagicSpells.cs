using Kichikuou.Engine.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Battles;

/// <summary>The element of a magic attack's effect (docs/ui/mage-lines.md, "The plan: magic effects by element").</summary>
public enum MagicElement
{
    /// <summary>Fire, and every caster without an element of its own: the Fire Blast.</summary>
    Fire,
    /// <summary>Ice and snow: Ice Arrow, White Chill, Snow Laser, "freeze".</summary>
    Ice,
    /// <summary>Lightning, Lightning Laser.</summary>
    Lightning,
    /// <summary>Darkness Beam, Black Wave, and the demons without a spell.</summary>
    Dark,
    /// <summary>Friendship Beam, Light Bomb, Miracle Straight Flash.</summary>
    Light,
    /// <summary>Papaya's nerve gas, Ragarl's Poison Gauntlet.</summary>
    Poison,
}

/// <summary>
/// Which element a magic attack plays (owner, 2026-10-21; docs/ui/mage-lines.md): the spell its caster shouts in
/// <c>FIGT_M</c> before the exchange, by the line shown, else the caster's own element. Interface only: the rules never
/// read it.
/// </summary>
public static class MagicSpells
{
    private const string Flavour = "FIGT_M.ADV";

    /// <summary>
    /// The lines that name a spell, by the line of their text run's first text (the <c>ScriptRef</c> the flavour
    /// raises, <c>Battle.Flavour.cs</c>). The specials' lines (the Destruction Beams) are left out: a special plays
    /// its own effect.
    /// </summary>
    private static readonly Dictionary<int, MagicElement> Lines = new()
    {
        [8893] = MagicElement.Fire,         // Sill (unit 2): Flame Arrow!!
        [8901] = MagicElement.Ice,          // Sill: Ice Arrow!!
        [8423] = MagicElement.Ice,          // Asuka (unit 11): White Chiiill!
        [187] = MagicElement.Ice,           // Melfeis (unit 13): Ice Arrow!
        [425] = MagicElement.Fire,          // Shizuka (unit 17): Fire Laser!
        [946] = MagicElement.Lightning,     // Eleanor (unit 23): Lightning!!
        [1848] = MagicElement.Fire,         // Gandhi (unit 37): Super Royal Fire Laser!!
        [1976] = MagicElement.Dark,         // Chizuko (unit 38), at Zeth: ... Darkness Beam!!
        [2005] = MagicElement.Dark,         // Chizuko: Hohohohohoh, Darkness Beam!!
        [2173] = MagicElement.Fire,         // Magic (unit 39): Uuuum... Fire Laser.
        [2239] = MagicElement.Fire,         // Nagi (unit 40): Fire Laser.
        [2341] = MagicElement.Light,        // Seizel Sisters (unit 42): Friendship Beam!!
        [2541] = MagicElement.Lightning,    // Somita (unit 46): Hyaaaaah!! Lightning!
        [3038] = MagicElement.Lightning,    // Melfeis (commander 4): Lightning!!
        [4625] = MagicElement.Lightning,    // Hanty (commander 57): Lightning Laser!!
        [5138] = MagicElement.Ice,          // La Seizel (commander 90): ... There, freeze!
        [5152] = MagicElement.Ice,          // La Seizel: Snow Laser.
        [5188] = MagicElement.Ice,          // La Seizel: Hahaha!! Freeze!
        [5605] = MagicElement.Dark,         // Chizuko (commander 101): Black Wave!!
        [5784] = MagicElement.Poison,       // Papaya (commander 104): ... nerve gas.
        [5886] = MagicElement.Fire,         // Nagi (commander 107): Fire Laser.
        [5995] = MagicElement.Poison,       // Ragarl (commander 109): Poison Gauntlet!!
        [6111] = MagicElement.Lightning,    // Magic (commander 110), in the Zeth war: Lightning Laser!!
        [6160] = MagicElement.Lightning,    // Magic: Lightning Laser!!
        [6262] = MagicElement.Fire,         // Saias (commander 113): Fire Laser!!
        [6406] = MagicElement.Ice,          // Uspira (commander 117): Snow Laser.
        [6536] = MagicElement.Lightning,    // Kabachahn (commander 121): Lightning Laser.
        [6672] = MagicElement.Light,        // Alex (commander 125): Light Bomb!
        [6840] = MagicElement.Light,        // Redeye (commander 130): Miracle Straight Flash!!
    };

    /// <summary>The player's units with an element of their own (owner, 2026-10-21).</summary>
    private static readonly Dictionary<int, MagicElement> Units = new()
    {
        [2] = MagicElement.Fire,            // Sill
        [11] = MagicElement.Ice,            // Asuka
        [13] = MagicElement.Ice,            // Melfeis
        [17] = MagicElement.Fire,           // Shizuka
        [19] = MagicElement.Lightning,      // Hanty: her spell as the enemy's
        [23] = MagicElement.Lightning,      // Eleanor
        [37] = MagicElement.Fire,           // Gandhi
        [38] = MagicElement.Dark,           // Chizuko
        [39] = MagicElement.Fire,           // Magic
        [40] = MagicElement.Fire,           // Nagi
        [42] = MagicElement.Light,          // Seizel Sisters
        [46] = MagicElement.Lightning,      // Somita
        [48] = MagicElement.Light,          // Hanny King
    };

    /// <summary>The enemy's commanders with an element of their own (owner, 2026-10-21).</summary>
    private static readonly Dictionary<int, MagicElement> Commanders = new()
    {
        [4] = MagicElement.Lightning,       // Melfeis
        [57] = MagicElement.Lightning,      // Hanty
        [90] = MagicElement.Ice,            // La Seizel
        [101] = MagicElement.Dark,          // Chizuko
        [104] = MagicElement.Poison,        // Papaya
        [107] = MagicElement.Fire,          // Nagi
        [109] = MagicElement.Poison,        // Ragarl
        [110] = MagicElement.Lightning,     // Magic
        [113] = MagicElement.Fire,          // Saias
        [117] = MagicElement.Ice,           // Uspira
        [121] = MagicElement.Lightning,     // Kabachahn
        [125] = MagicElement.Light,         // Alex
        [130] = MagicElement.Light,         // Redeye
        [168] = MagicElement.Fire,          // Annis
    };

    private const int DemonMagic = 11;

    /// <summary>The element a flavour line names; null when it names no spell.</summary>
    public static MagicElement? OfLine(ScriptRef line) =>
        line.File == Flavour && Lines.TryGetValue(line.Line, out var element) ? element : null;

    /// <summary>
    /// The caster's own element: the player's unit (<c>Unit_Cur</c>) or the enemy's commander (<c>Cmdr_Cur</c>) from
    /// the tables, else Dark for a Demon Magic caster (class 11: Satella, Miki, La Vaswald, commander 100 as Satella),
    /// else Fire (the Fire Blast of every caster without an element).
    /// </summary>
    public static MagicElement OfCaster(GameState s, BattleSide side)
    {
        bool player = side == BattleSide.Player;
        int record = player ? s.Unit_Cur.Value : s.Cmdr_Cur.Value;
        if ((player ? Units : Commanders).TryGetValue(record, out var element))
            return element;
        if (record <= 0)
            return MagicElement.Fire;
        int troopClass = player ? s.Units[s.Unit_Cur].Class.Value : s.Cmdrs[s.Cmdr_Cur].Class.Value;
        return troopClass == DemonMagic ? MagicElement.Dark : MagicElement.Fire;
    }

    /// <summary>The spell lines of the table, for the tests that read them in the player's scenario.</summary>
    public static IReadOnlyDictionary<int, MagicElement> SpellLines => Lines;

    /// <summary>The casters of the tables, for the tests.</summary>
    public static IReadOnlyDictionary<int, MagicElement> UnitCasters => Units;

    /// <summary>The commanders of the tables, for the tests.</summary>
    public static IReadOnlyDictionary<int, MagicElement> CommanderCasters => Commanders;
}
