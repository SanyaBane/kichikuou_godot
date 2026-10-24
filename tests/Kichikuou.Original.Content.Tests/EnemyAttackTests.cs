using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary>An enemy attack on a province of ours as our screen shows it (docs/ui/enemy-attack.md, §6).</summary>
public class EnemyAttackTests
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    /// <summary>
    /// The week-18 save's attack (docs/ui/enemy-attack.md, §3): the Leazas Rebel Army (owner 2) at Ys (5) — Defense 2,
    /// Battle Area 330, Economy 8, its picture CG 1003 — in week 18; Kanami free, the other ninjas not in service; the
    /// army Sakanaku (commander 6) alone.
    /// </summary>
    private static GameState YsAttacked()
    {
        var s = new GameState { Cmdr_StrBase = 200, Prov_StrBase = 150, Prov_Cur = 5, Ev_PhaseNation = 2, Week = 18 };
        s.Strings[10] = " Leazas Rebel Army ";
        s.Strings[154] = "Ys";
        s.Strings[8] = "Ys";                                                                   // FIGT_B.ADV:15 copies it
        var ys = s.Provs[5];
        (ys.Owner, ys.Defense, ys.BattleArea, ys.Economy, ys.BgCg) = (1, 2, 330, 8, 1003);
        (s.F_Kanami_NinjaUsed, s.F_Freya_NinjaUsed, s.F_Kaoru_NinjaUsed, s.F_Wichita_NinjaUsed, s.F_Saitou_NinjaUsed) =
            (0, 99, 99, 99, 99);
        s.Strings[215] = "Sakanaku";
        s.Strings[216] = "Leazas Soldiers";
        (s.Cmdrs[6].Troops, s.Cmdrs[6].FaceCg) = (970, 1106);
        s.Bt_EnemyParty1 = 6;
        return s;
    }

    private static string Name(ScriptRef line) => line.Line switch
    {
        348 => "Kanami", 354 => "Freya", 360 => "Kaoru", 366 => "Wichita", 372 => "Saitou", _ => "?",
    };

    [Fact]
    public void An_attack_holds_what_the_announcement_says_and_the_province_the_player_decides_on()
    {
        var attack = EnemyAttack.Of(YsAttacked(), Name);

        Assert.Equal(("Leazas Rebel Army", "Ys", 5), (attack.Army, attack.Province, attack.Record));
        Assert.Equal((821, 1003), (attack.FlagCg, attack.PictureCg));                       // Ev_PhaseNation + 819
        Assert.Equal((4, 330, 800), (attack.Walls, attack.CityLimit, attack.Revenue));       // Defense * 2, K GOLD
        Assert.Equal("LP 3, Month 5, Week 2", attack.Date);
        Assert.Equal([new AttackNinja(1, "Kanami", 9, true)], attack.Ninjas);
        Assert.True(attack.CanScout);
        Assert.Null(attack.Report);
    }

    [Fact]
    public void The_ninjas_are_those_in_service_in_the_menus_order_the_spent_ones_not_free()
    {
        var s = YsAttacked();
        (s.F_Kanami_NinjaUsed, s.F_Freya_NinjaUsed, s.F_Kaoru_NinjaUsed, s.F_Wichita_NinjaUsed) = (1, 1, 0, 0);
        var attack = EnemyAttack.Of(s, Name);

        Assert.Equal([new AttackNinja(1, "Kanami", 9, false), new AttackNinja(2, "Freya", 449, false),
            new AttackNinja(3, "Kaoru", 72, true), new AttackNinja(4, "Wichita", 71, true)], attack.Ninjas);
        Assert.True(attack.CanScout);
        (s.F_Kaoru_NinjaUsed, s.F_Wichita_NinjaUsed) = (1, 1);
        Assert.False(EnemyAttack.Of(s, Name).CanScout);
    }

    [Theory]
    [InlineData(1, "LP 3, Month 1, Week 1")]
    [InlineData(18, "LP 3, Month 5, Week 2")]
    [InlineData(48, "LP 3, Month 12, Week 4")]
    [InlineData(49, "LP 4, Month 1, Week 1")]
    public void The_date_is_the_week_banners(int week, string date) => Assert.Equal(date, EnemyAttack.DateOf(week));

    /// <summary>
    /// The presenter holds the attack from the announcement (<c>FIGT_B.ADV:20</c>) to the menu's answer Intercept or
    /// Abandon City, Maris's line as her window prints it, the scout report once one is made; no window of ours shows
    /// meanwhile. Without the cover nothing is held.
    /// </summary>
    [ScenarioFact]
    public void The_presenter_holds_the_attack_from_the_announcement_to_the_menus_answer()
    {
        var s = YsAttacked();
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        presenter.Attach(s);
        Announce(presenter);
        Assert.Null(presenter.Attack);                                                        // the original's screen

        presenter.CoverEnemyAttack = true;
        Announce(presenter);
        Assert.Equal(EnemyAttack.Of(s, Name) with { AdvisorLines = presenter.Attack!.AdvisorLines }, presenter.Attack);
        Assert.Equal("King Rance, if we don't act, Ys will be occupied. What should we do?",
            string.Join(" ", presenter.Attack.AdvisorLines).Replace('’', '\''));
        Assert.Null(presenter.MessageWindow);

        presenter.EnemyAttackAnswered(2);                                                    // Ninja Scout: still up
        presenter.Show(new WindowEvent((Sys3Int)113, Sys3Int.Zero));
        presenter.Show(new MessageEvent(new ScriptRef("FIGT_B.ADV", 587)));                  // "The Approaching Enemy"
        Assert.NotNull(presenter.Attack.Report);
        Assert.Equal("Sakanaku", presenter.Attack.Report!.Places[0]!.Name);
        presenter.Show(new WindowEvent(Sys3Int.Zero, Sys3Int.Zero));
        Assert.NotNull(presenter.Attack.Report);                                              // kept after the window

        presenter.EnemyAttackAnswered(1);                                                    // Intercept
        Assert.Null(presenter.Attack);
    }

    /// <summary>
    /// After the screen, Rance's lines at Intercept, Plains and City and the six of Abandon City are hidden while their
    /// window holds them; a refusal and the result are not.
    /// </summary>
    [ScenarioFact]
    public void Rances_repeated_lines_and_abandons_are_hidden_a_refusal_is_not()
    {
        var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
        presenter.Attach(YsAttacked());
        presenter.CoverEnemyAttack = true;
        foreach (int line in new[] { 68, 73, 171, 174, 196, 203, 214, 737, 742, 747, 754, 759, 766 })
        {
            presenter.Show(new WindowEvent((Sys3Int)111, Sys3Int.Zero));
            presenter.Show(new MessageEvent(new ScriptRef("FIGT_B.ADV", line)));
            Assert.True(presenter.HidesLine, $"line {line}");
            Assert.Null(presenter.MessageWindow);
            presenter.Show(new WindowEvent(Sys3Int.Zero, Sys3Int.Zero));
            Assert.False(presenter.HidesLine);
        }
        foreach (int line in new[] { 90, 101, 795 })                                            // Gandhi, his notice, the loss
        {
            presenter.Show(new WindowEvent((Sys3Int)113, Sys3Int.Zero));
            presenter.Show(new MessageEvent(new ScriptRef("FIGT_B.ADV", line)));
            Assert.False(presenter.HidesLine, $"line {line}");
            Assert.NotNull(presenter.MessageWindow);
            presenter.Show(new WindowEvent(Sys3Int.Zero, Sys3Int.Zero));
        }
    }

    /// <summary><c>FIGT_B.ADV:5-45</c> as the port shows it (<c>DefenceAnnounceAsync</c>), without the map's steps.</summary>
    private static void Announce(BattlePresenter presenter)
    {
        presenter.Show(new WindowEvent((Sys3Int)331, (Sys3Int)1003));
        presenter.Show(new WindowEvent((Sys3Int)113, Sys3Int.Zero));
        presenter.Show(new MessageEvent(new ScriptRef("FIGT_B.ADV", 20), [(Sys3Int)10, (Sys3Int)8]));
        presenter.Show(new WindowEvent((Sys3Int)1, Sys3Int.Zero));
        presenter.Show(new WindowEvent(Sys3Int.Zero, Sys3Int.Zero));
        presenter.Show(new WindowEvent((Sys3Int)311, (Sys3Int)1));
        presenter.Show(new WindowEvent((Sys3Int)321, (Sys3Int)6));
        presenter.Show(new WindowEvent((Sys3Int)121, Sys3Int.Zero));
        presenter.Show(new MessageEvent(new ScriptRef("FIGT_B.ADV", 35), [(Sys3Int)8]));
        presenter.Show(new WindowEvent((Sys3Int)1, Sys3Int.Zero));
        presenter.Show(new MessageEvent(new ScriptRef("FIGT_B.ADV", 43)));
        presenter.Show(new WindowEvent((Sys3Int)1, Sys3Int.Zero));
        presenter.Show(new WindowEvent(Sys3Int.Zero, Sys3Int.Zero));
    }
}
