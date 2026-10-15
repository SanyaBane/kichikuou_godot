using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The stats our battle cards show under the portrait (docs/ui/battle-screen.md, "The cards' stats").</summary>
public class BattleCardStatsTests
{
    /// <summary>
    /// The week-18 attack on Oaks (6): Rance (unit 1) and Barres (unit 6), whose partner Haurein (unit 12) is deployed,
    /// against Sakanaku (commander 6); an offensive battle (kind 1).
    /// </summary>
    private static GameState Battle()
    {
        var s = new GameState { Unit_StrBase = 20, Cmdr_StrBase = 200, Bt_Kind = 1 };
        void Unit(int n, string name, int troops, int level, int tatk, int tdef, int offAtk, int offDef, int atk, int def, int attacks, int partner)
        {
            s.Strings[20 + (n - 1) * 3] = name;
            s.Strings[21 + (n - 1) * 3] = "Leazas Knights";
            var u = s.Units[n];
            (u.Troops, u.MaxTroops, u.HP, u.MaxHP, u.Level) = ((Sys3Int)troops, (Sys3Int)1000, (Sys3Int)5, (Sys3Int)5, (Sys3Int)level);
            (u.TroopAtk, u.TroopDef, u.OffAtkMod, u.OffDefMod, u.DefAtkMod) = ((Sys3Int)tatk, (Sys3Int)tdef, (Sys3Int)offAtk, (Sys3Int)offDef, (Sys3Int)7);
            (u.Atk, u.Def, u.Magic, u.Attacks, u.Partner) = ((Sys3Int)atk, (Sys3Int)def, (Sys3Int)0, (Sys3Int)attacks, (Sys3Int)partner);
        }
        Unit(1, "Rance", 926, 1, 10, 6, 0, 0, 22, 18, 14, 2);
        Unit(6, "Barres", 980, 0, 10, 6, 12, 10, 14, 7, 3, 12);
        Unit(12, "Haurein", 1400, 0, 8, 4, 0, 1, 13, 9, 6, 0);
        (s.Bt_PlayerSlot1, s.Bt_PlayerSlot2, s.Bt_PlayerSlot4) = (1, 12, 6);
        s.Strings[200 + 5 * 3] = "Sakanaku";
        s.Strings[201 + 5 * 3] = "Leazas Soldiers";
        var c = s.Cmdrs[6];
        (c.Troops, c.MaxTroops, c.HP, c.MaxHP, c.TroopAtk, c.TroopDef) = ((Sys3Int)730, (Sys3Int)1000, (Sys3Int)6, (Sys3Int)6, (Sys3Int)8, (Sys3Int)4);
        (c.TroopAtkMod, c.TroopDefMod, c.Atk, c.Def, c.Magic, c.Attacks) = ((Sys3Int)0, (Sys3Int)11, (Sys3Int)10, (Sys3Int)4, (Sys3Int)0, (Sys3Int)2);
        s.Bt_EnemySlot1 = 6;
        return s;
    }

    private static BattleCardStats Stats(GameState s, BattleSide side, int slot) => BattleBoard.Card(s, new BattlePlace(side, slot))!.Stats!;

    [Fact]
    public void A_unit_with_troops_shows_its_level_troop_values_with_the_battles_modifiers_magic_and_attacks()
    {
        Assert.Equal(new BattleCardStats(1, 10, 6, 0, 0, 0, 14, Commander: false, Surging: false),   // Sill (unit 2) is not deployed
            Stats(Battle(), BattleSide.Player, 1));
    }

    [Fact]
    public void Modifiers_above_10_are_minus_and_10_is_none_and_the_kind_picks_them()
    {
        var s = Battle();
        var barres = Stats(s, BattleSide.Player, 4);
        Assert.Equal((-2, 0), (barres.AttackModifier, barres.DefenseModifier));   // 12; 10 is the box's lone ")" (BSC-04)
        Assert.True(barres.Surging && barres.SurgingApplies);                       // Haurein (unit 12) stands in slot 2

        s.Bt_Kind = 2;
        Assert.Equal(7, Stats(s, BattleSide.Player, 4).AttackModifier);
        s.Bt_Kind = 0;
        Assert.Equal(0, Stats(s, BattleSide.Player, 4).AttackModifier);
    }

    [Fact]
    public void Without_troops_a_unit_shows_its_commanders_attack_and_defense_and_surging_gives_nothing()
    {
        var s = Battle();
        s.Units[6].Troops = 0;
        var barres = Stats(s, BattleSide.Player, 4);
        Assert.Equal(new BattleCardStats(0, 14, 7, 0, 0, 0, 3, Commander: true, Surging: true), barres);
        Assert.False(barres.SurgingApplies);
    }

    [Fact]
    public void An_enemy_commander_has_no_level_and_its_modifiers_as_the_box_decodes_them()
    {
        var s = Battle();
        Assert.Equal(new BattleCardStats(null, 8, 4, 0, -1, 0, 2, Commander: false, Surging: false),   // 11 as the box prints it (BRS-08)
            Stats(s, BattleSide.Enemy, 1));
        s.Cmdrs[6].Troops = 0;
        Assert.Equal(new BattleCardStats(null, 10, 4, 0, 0, 0, 2, Commander: true, Surging: false), Stats(s, BattleSide.Enemy, 1));
    }
}
