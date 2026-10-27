using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Tests.Combat;

/// <summary>
/// BRS-24's fix (owner's decision 32, docs/tweaks.md; <see cref="DemonReach"/>): against a demon commander the holder of
/// Chaos or Nikkou over the battle's limit fights in person (<c>ComputeExchange</c>'s b), on the field shot at by the
/// demon's troops too (d), shown with his troops; the original's rule unless chosen. The draws are a real generator's:
/// what is tested is which streams run, not their figures.
/// </summary>
public class DemonReachTests
{
    /// <summary>
    /// One exchange: Rance (unit 1) holding <paramref name="item"/> with 1000 troops in slot 1, acting in melee, against
    /// commander 1 of <paramref name="cls"/> with <paramref name="enemyTroops"/> troops, the limit <paramref name="limit"/>
    /// — Kayblis alone at the Demon Lord's Castle by default (limit 500).
    /// </summary>
    private static GameState Exchange(DemonReach reach, int item = 1, int cls = 10, int enemyTroops = 0, int limit = 500,
        uint seed = 1)
    {
        var s = new GameState();
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = (Sys3Int)limit;
        s.Bt_PlayerActSlot = 1;
        s.Bt_PSlot1State = 1;
        s.Bt_EnemyActSlot = 1;
        var u = s.Units[1];
        u.Troops = 1000; u.TroopAtk = 8; u.TroopDef = 8; u.Atk = 30; u.Def = 20; u.Attacks = 50; u.HP = 40; u.Magic = 3;
        u.Level = 10; u.Item = (Sys3Int)item;
        var c = s.Cmdrs[1];
        c.Troops = (Sys3Int)enemyTroops; c.TroopAtk = 8; c.TroopDef = 8; c.Atk = 30; c.Def = 10; c.Attacks = 50; c.HP = 40;
        c.Magic = 9; c.Class = (Sys3Int)cls;
        var battle = new Battle(s, new XSystem35Random(seed), new RecordingView(), new ScriptedInput(), new RecordingStory());
        battle.Script.DemonReach = reach;
        battle.ComputeExchange();
        return s;
    }

    [Fact]
    public void The_original_keeps_the_holder_behind_his_troops()
    {
        var s = Exchange(DemonReach.Original);
        Assert.Equal(0, (int)s.Bt_PlayerAllEngaged);
        Assert.Equal(0, (int)s.Bt_PCmdrVsCmdrShots);                // b skipped (BRS-19)
        Assert.NotEqual(0, (int)s.Bt_PTroopVsCmdrShots);            // a: his troops shoot at the demon…
        Assert.Equal(0, (int)s.Bt_PTroopVsCmdrDmg);                 // … and the shield zeroes them
        Assert.Equal(2, (int)s.Bt_EnemyCmdrShield);
    }

    [Theory]
    [InlineData(DemonReach.OnField)]
    [InlineData(DemonReach.OutOfReach)]
    public void The_fix_sends_the_holder_in_person(DemonReach reach)
    {
        var s = Exchange(reach);
        Assert.InRange((int)s.Bt_PCmdrVsCmdrShots, 1, 6);           // b: the demon alone → commander on commander
        Assert.Equal(0, (int)s.Bt_PTroopVsCmdrDmg);                 // his troops still shielded
        Assert.Equal(0, (int)s.Bt_PlayerAllEngaged);                // the flag keeps the original's value
        Assert.Equal(500, (int)s.Bt_PlayerEngaged);
    }

    [Theory]
    [InlineData(DemonReach.OnField)]
    [InlineData(DemonReach.OutOfReach)]
    public void The_fix_lets_the_holder_wound_the_demon(DemonReach reach)
    {
        int wounds = Enumerable.Range(1, 20).Sum(seed => (int)Exchange(reach, seed: (uint)seed).Bt_PCmdrVsCmdrDmg);
        Assert.True(wounds > 0);
        Assert.Equal(0, Enumerable.Range(1, 20).Sum(seed => (int)Exchange(DemonReach.Original, seed: (uint)seed).Bt_PCmdrVsCmdrDmg));
    }

    [Theory]
    [InlineData(1, 10)]     // Chaos against a demon: the fix acts
    [InlineData(2, 11)]     // Nikkou against a demon mage
    [InlineData(3, 10)]     // the Pop. Staff (item 3): no holy sword
    [InlineData(0, 10)]     // no item
    [InlineData(1, 0)]      // Chaos against a man: BRS-19 stands
    public void Only_chaos_or_nikkou_against_a_demon(int item, int cls)
    {
        var s = Exchange(DemonReach.OnField, item, cls);
        bool fixes = item is 1 or 2 && cls > 9;
        Assert.Equal(fixes, s.Bt_PCmdrVsCmdrShots != 0);
    }

    [Theory]
    [InlineData(DemonReach.Original, false, false)]
    [InlineData(DemonReach.OnField, true, true)]
    [InlineData(DemonReach.OutOfReach, true, false)]
    public void Against_a_demon_with_troops_only_on_the_field_is_he_shot_at(DemonReach reach, bool inPerson, bool shotAt)
    {
        // Medusa at Kayblis's Castle: limit 300, her army over it — 300 engaged on each side.
        var s = Exchange(reach, enemyTroops: 4500, limit: 300);
        Assert.Equal(inPerson, s.Bt_PCmdrVsTroopShots != 0);       // b: her engaged troops ≥ 10 → he fights them
        Assert.Equal(0, (int)s.Bt_PCmdrVsCmdrShots);
        Assert.Equal(shotAt ? 2 : 0, (int)s.Bt_ETroopVsCmdrShots); // d: 300 / 300 × 2
    }

    [Theory]
    [InlineData(DemonReach.Original, 1, 10, 11)]    // over the limit: the army alone (CgFull)
    [InlineData(DemonReach.OnField, 1, 10, 12)]     // the fix: him with his troops (CgPartial)
    [InlineData(DemonReach.OutOfReach, 2, 10, 12)]
    [InlineData(DemonReach.OnField, 3, 10, 11)]     // no holy sword
    [InlineData(DemonReach.OnField, 1, 0, 11)]      // no demon
    public async Task The_scene_shows_the_holder_with_his_troops(DemonReach reach, int item, int cls, int sprite)
    {
        var fx = ResolutionFixture.Create();
        var s = fx.State;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 500;
        var u = fx.Unit(1);
        u.Troops = 1000; u.CgFull = 11; u.CgPartial = 12; u.CgAlone = 13; u.Item = (Sys3Int)item;
        var c = fx.Cmdr(1);
        c.Troops = 0; c.CgAlone = 23; c.Class = (Sys3Int)cls;
        fx.Battle.Script.DemonReach = reach;
        await fx.RunIntoFlavourAsync(fx.Battle.PresentEngagementAsync);
        var scene = Assert.IsType<EngagementSceneEvent>(fx.View.Events[0]);
        Assert.Equal([(Sys3Int)sprite], scene.PlayerSprites);
    }

    [Fact]
    public void The_forecast_follows_the_choice()
    {
        var s = new GameState();
        s.Bt_Kind = 1;
        s.Unit_Cur = 1;
        s.Cmdr_Cur = 1;
        s.Bt_TroopLimit = 500;
        s.Bt_PlayerSlot1 = 1;
        s.Bt_EnemySlot1 = 1;
        s.Bt_PlayerActSlot = 1;
        s.Bt_PSlot1State = 1;
        var u = s.Units[1];
        u.Status = 1; u.Troops = 1000; u.TroopAtk = 8; u.TroopDef = 8; u.Atk = 30; u.Def = 20; u.Attacks = 50; u.HP = 40;
        u.Level = 10; u.Item = 1;
        var c = s.Cmdrs[1];
        c.Status = 1; c.Troops = 0; c.Atk = 30; c.Def = 10; c.Attacks = 50; c.HP = 40; c.Class = 10;
        var melee = new FigtSelChoice(FigtSelCommand.Direct, 4);

        Assert.Null(ExchangeForecast.Of(s, melee).Hp);              // shielded, the holder behind his troops
        Assert.NotNull(ExchangeForecast.Of(s, melee, DemonReach.OnField).Hp);
        Assert.NotNull(ExchangeForecast.Of(s, melee, DemonReach.OutOfReach).Hp);
    }
}
