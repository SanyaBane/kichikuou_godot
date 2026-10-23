using System.Text.RegularExpressions;
using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Content.Tests;

/// <summary>A magic attack's effect by its element (docs/ui/mage-lines.md, "The plan: magic effects by element").</summary>
public class MagicSpellsTests
{
    private const string Flavour = "FIGT_M.ADV";

    private static GameState PlayerMagic(int unit)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)2;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)1;
        s.Unit_Cur = (Sys3Int)unit;
        return s;
    }

    private static GameState EnemyMagic(int commander)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)99;
        s.Bt_EnemyAction = (Sys3Int)2;
        s.Bt_MsgMode = (Sys3Int)2;
        s.Cmdr_Cur = (Sys3Int)commander;
        return s;
    }

    /// <summary>Sill (unit 2) shouts Flame Arrow or Ice Arrow; her generic lines take her own element, fire.</summary>
    [Theory]
    [InlineData(8893, ExchangeEffectKind.Magic)]
    [InlineData(8901, ExchangeEffectKind.IceMagic)]
    [InlineData(8906, ExchangeEffectKind.Magic)]
    public void The_shouted_spell_picks_the_effect(int line, ExchangeEffectKind kind)
    {
        var shouted = MagicSpells.OfLine(new ScriptRef(Flavour, line));
        Assert.Equal(new ExchangeEffect(kind, BattleSide.Player), ExchangeEffect.Of(PlayerMagic(2), shouted));
    }

    /// <summary>
    /// Without a spell the caster's element: Asuka (unit 11) ice, Melfeis (unit 13) ice, Hanty (unit 19) lightning,
    /// Chizuko (unit 38) dark, Hanny King (unit 48) light, Freak (unit 20) and Cafe (unit 45) none — the Fire Blast.
    /// </summary>
    [Theory]
    [InlineData(11, ExchangeEffectKind.IceMagic)]
    [InlineData(13, ExchangeEffectKind.IceMagic)]
    [InlineData(19, ExchangeEffectKind.LightningMagic)]
    [InlineData(38, ExchangeEffectKind.DarkMagic)]
    [InlineData(48, ExchangeEffectKind.LightMagic)]
    [InlineData(20, ExchangeEffectKind.Magic)]
    [InlineData(45, ExchangeEffectKind.Magic)]
    public void A_unit_without_a_spell_plays_its_element(int unit, ExchangeEffectKind kind) =>
        Assert.Equal(new ExchangeEffect(kind, BattleSide.Player), ExchangeEffect.Of(PlayerMagic(unit)));

    /// <summary>
    /// The enemy's commanders by their own record: Melfeis (commander 4) lightning where her unit is ice, Magic
    /// (commander 110) lightning where her unit is fire, Papaya (commander 104) poison, Zeth's Mage Leader
    /// (commander 96) none.
    /// </summary>
    [Theory]
    [InlineData(4, ExchangeEffectKind.LightningMagic)]
    [InlineData(110, ExchangeEffectKind.LightningMagic)]
    [InlineData(104, ExchangeEffectKind.PoisonMagic)]
    [InlineData(96, ExchangeEffectKind.Magic)]
    public void A_commander_plays_its_own_element(int commander, ExchangeEffectKind kind) =>
        Assert.Equal(new ExchangeEffect(kind, BattleSide.Enemy), ExchangeEffect.Of(EnemyMagic(commander)));

    /// <summary>
    /// A Demon Magic caster (class 11) without an element of its own is dark: Satella (unit 29), and commander 100 as
    /// Satella; Marder in the same slot (class 2) plays the Fire Blast.
    /// </summary>
    [Fact]
    public void A_demon_without_a_spell_is_dark()
    {
        var unit = PlayerMagic(29);
        unit.Units[29].Class = (Sys3Int)11;
        Assert.Equal(ExchangeEffectKind.DarkMagic, ExchangeEffect.Of(unit)?.Kind);

        var satella = EnemyMagic(100);
        satella.Cmdrs[100].Class = (Sys3Int)11;
        Assert.Equal(ExchangeEffectKind.DarkMagic, ExchangeEffect.Of(satella)?.Kind);

        var marder = EnemyMagic(100);
        marder.Cmdrs[100].Class = (Sys3Int)2;
        Assert.Equal(ExchangeEffectKind.Magic, ExchangeEffect.Of(marder)?.Kind);
    }

    /// <summary>A special plays its own effect and a Direct attack its melee, whatever spell was heard.</summary>
    [Fact]
    public void A_special_or_a_direct_attack_ignores_the_spell()
    {
        var beam = PlayerMagic(17);
        beam.Bt_SpecialAtk = (Sys3Int)2;
        Assert.Equal(ExchangeEffectKind.ShizukaBeam, ExchangeEffect.Of(beam, MagicElement.Ice)?.Kind);

        var direct = PlayerMagic(13);
        direct.Bt_PlayerAction = (Sys3Int)0;
        direct.Bt_MsgMode = (Sys3Int)3;
        Assert.Equal(ExchangeEffectKind.Melee, ExchangeEffect.Of(direct, MagicElement.Ice)?.Kind);
    }

    /// <summary>The presenter takes the spell of the line shown before the shots, and forgets it at them.</summary>
    [ScenarioFact]
    public void The_presenter_takes_the_line_shown_before_the_shots()
    {
        var presenter = new BattlePresenter(new ScenarioText(LocalData.ScenarioDirectory), new CgLibrary(LocalData.CgDirectory), new BoxGlyphs());
        var s = PlayerMagic(2);
        presenter.Start(s);
        presenter.Show(new MessageEvent(new ScriptRef(Flavour, 8901)));       // Sill: Ice Arrow!!
        presenter.Show(new ShotsFrameEvent((Sys3Int)0));
        Assert.Equal(ExchangeEffectKind.IceMagic, presenter.Effect?.Kind);
        presenter.Show(new ShotsFrameEvent((Sys3Int)0));                      // the next exchange heard nothing
        Assert.Equal(ExchangeEffectKind.Magic, presenter.Effect?.Kind);
    }

    /// <summary>
    /// Each line of the table starts a text run of <c>FIGT_M</c> that names its spell, and every run of <c>FIGT_M</c>
    /// that names a spell is in the table but the specials' and the talk about the Flash.
    /// </summary>
    [ScenarioFact]
    public void The_table_holds_every_spell_line_of_the_scenario()
    {
        var texts = new ScenarioText(LocalData.ScenarioDirectory);
        var names = new Dictionary<MagicElement, Regex>
        {
            [MagicElement.Fire] = new("Flame Arrow|Fire Laser"),
            [MagicElement.Ice] = new("Ice Arrow|Chi+ll|Snow Laser|[Ff]reeze"),
            [MagicElement.Lightning] = new("Lightning"),
            [MagicElement.Dark] = new("Darkness Beam|Black Wave"),
            [MagicElement.Light] = new("Friendship Beam|Light Bomb|Fla+sh"),
            [MagicElement.Poison] = new("nerve gas|Poison Gauntlet"),
        };
        foreach (var (line, element) in MagicSpells.SpellLines)
        {
            string text = texts.Read(new ScriptRef(Flavour, line)).Format([], _ => "").Replace('\n', ' ');
            Assert.Matches(names[element], text);
        }

        // Every text run in the file: its first text line, the lines before it neither text nor R.
        string[] lines = File.ReadAllLines(Path.Combine(LocalData.ScenarioDirectory, Flavour), System.Text.Encoding.UTF8);
        var spell = new Regex(string.Join('|', names.Values.Select(r => r.ToString())) + "|Beam|Laser|Breath");
        int[] notSpells =
        [
            415, 1906, 2060, 2229,      // the Destruction Beams: specials, with effects of their own
            1736, 1918, 5693,           // the talk about Papaya's Flash
            5456,                       // a dragon's Yellow Breath (commander 100, class 0: melee)
        ];
        var missing = new List<string>();
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].TrimStart().StartsWith('\'') || i > 0 && (lines[i - 1].TrimStart().StartsWith('\'') || lines[i - 1].Trim() == "R"))
                continue;
            var run = new List<string>();
            for (int j = i; j < lines.Length && (lines[j].TrimStart().StartsWith('\'') || lines[j].Trim() == "R"); j++)
                run.Add(lines[j].Trim().Trim('\''));
            string text = string.Join(' ', run);
            if (spell.IsMatch(text) && !MagicSpells.SpellLines.ContainsKey(i + 1) && !notSpells.Contains(i + 1))
                missing.Add($":{i + 1} {text}");
        }
        Assert.Empty(missing);
    }

    /// <summary>
    /// Every unit and commander of a magic class in the booted game (<c>SET_CHR</c>) has an element in the tables, is a
    /// Demon Magic caster (dark), or is one of those without an element (the Fire Blast).
    /// </summary>
    [Fact]
    public async Task Every_magic_caster_is_accounted_for()
    {
        var game = await EndGame.BootAsync();
        if (game is null)
            return;
        var s = game.State;
        int[] unitsWithout = [20, 45];                                      // Freak, Cafe
        int[] commandersWithout =
        [
            56, 88, 89, 96, 103, 106, 112, 114, 118, 122, 126,              // Freak, the Monster Generals, Zeth's chanters
            150, 152, 153, 167, 171, 172, 175, 176, 179, 180,               // Ouzawa, Deckmaster, Strength, more chanters
        ];
        var unknown = new List<string>();
        for (int u = 1; u <= 50; u++)
        {
            int c = s.Units[u].Class.Value;
            if (c is 2 or 3 or 11 && !MagicSpells.UnitCasters.ContainsKey(u) && c != 11 && !unitsWithout.Contains(u))
                unknown.Add($"unit {u} class {c}");
        }
        for (int m = 1; m < 200; m++)
        {
            int c = s.Cmdrs[m].Class.Value;
            if (c is 2 or 3 or 11 && !MagicSpells.CommanderCasters.ContainsKey(m) && c != 11 && !commandersWithout.Contains(m))
                unknown.Add($"commander {m} class {c}");
        }
        Assert.Empty(unknown);
    }
}
