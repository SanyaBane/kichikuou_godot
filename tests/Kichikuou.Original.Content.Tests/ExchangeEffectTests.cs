using Kichikuou.Content;
using Kichikuou.Content.Graphics;
using Kichikuou.Engine.Golden;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Combat;
using Kichikuou.Original.Content.Battles;
using Kichikuou.Original.Golden;
using Kichikuou.Original.State;
using Xunit.Abstractions;

namespace Kichikuou.Original.Content.Tests;

/// <summary>The effect an exchange plays from its shots (docs/ui/battle-screen.md, "The exchange's effects").</summary>
public class ExchangeEffectTests
{
    private static readonly Lazy<CgLibrary> Cgs = new(() => new CgLibrary(LocalData.CgDirectory));
    private static readonly Lazy<ScenarioText> Texts = new(() => new ScenarioText(LocalData.ScenarioDirectory));

    [Theory]
    [InlineData(1, 99, 3, ExchangeEffectKind.Bow, BattleSide.Player)]
    [InlineData(2, 99, 1, ExchangeEffectKind.Magic, BattleSide.Player)]
    [InlineData(99, 2, 2, ExchangeEffectKind.Magic, BattleSide.Enemy)]
    [InlineData(99, 1, 4, ExchangeEffectKind.Bow, BattleSide.Enemy)]
    [InlineData(0, 0, 3, ExchangeEffectKind.Melee, BattleSide.Player)]
    [InlineData(0, 0, 4, ExchangeEffectKind.Melee, BattleSide.Enemy)]
    public void The_firing_side_plays_and_in_a_two_sided_exchange_the_turns_actor(
        int player, int enemy, int msgMode, ExchangeEffectKind kind, BattleSide side)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)player;
        s.Bt_EnemyAction = (Sys3Int)enemy;
        s.Bt_MsgMode = (Sys3Int)msgMode;
        Assert.Equal(new ExchangeEffect(kind, side), ExchangeEffect.Of(s));
    }

    /// <summary>Maria's (unit 16) Indirect attack fires her cannons; Isoroku's (unit 27) and the enemy's shoot the bow.</summary>
    [Theory]
    [InlineData(16, 1, 99, 3, ExchangeEffectKind.Cannon)]
    [InlineData(27, 1, 99, 3, ExchangeEffectKind.Bow)]
    [InlineData(16, 99, 1, 4, ExchangeEffectKind.Bow)]
    public void Marias_shots_are_cannon_fire(int unit, int player, int enemy, int msgMode, ExchangeEffectKind kind)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)player;
        s.Bt_EnemyAction = (Sys3Int)enemy;
        s.Bt_MsgMode = (Sys3Int)msgMode;
        s.Unit_Cur = (Sys3Int)unit;
        Assert.Equal(kind, ExchangeEffect.Of(s)!.Value.Kind);
    }

    /// <summary>
    /// Special 1 is Rance Attack by Rance (unit 1), Kentarou-kun Slash's by Kentarou (unit 31), By la Way by Rick
    /// (unit 7), Rance Attack's in red.
    /// </summary>
    [Theory]
    [InlineData(1, ExchangeEffectKind.RanceAttack)]
    [InlineData(31, ExchangeEffectKind.KentarouSlash)]
    [InlineData(7, ExchangeEffectKind.ByLaWay)]
    public void Special_1_plays_Rance_Attack_or_By_la_Way_by_the_actor(int unit, ExchangeEffectKind kind)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)0;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)3;
        s.Bt_SpecialAtk = (Sys3Int)1;
        s.Unit_Cur = (Sys3Int)unit;
        Assert.Equal(new ExchangeEffect(kind, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>
    /// Special 2 is White D. Beam by Shizuka (unit 17) or Magic (unit 39), EPO Light by Gandhi (unit 37) — the white
    /// beam, each user's own — and Black D. Beam by Chizuko (unit 38) or Nagi (unit 40), the dark one, each user's own.
    /// </summary>
    [Theory]
    [InlineData(17, ExchangeEffectKind.ShizukaBeam)]
    [InlineData(39, ExchangeEffectKind.MagicBeam)]
    [InlineData(37, ExchangeEffectKind.EpoLight)]
    [InlineData(38, ExchangeEffectKind.ChizukoBeam)]
    [InlineData(40, ExchangeEffectKind.NagiBeam)]
    public void The_beams_play_the_white_or_the_dark_beam_by_the_actor(int unit, ExchangeEffectKind kind)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)2;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)1;
        s.Bt_SpecialAtk = (Sys3Int)2;
        s.Unit_Cur = (Sys3Int)unit;
        Assert.Equal(new ExchangeEffect(kind, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>
    /// Special 3 is Whirlwind Shot by Isoroku (unit 27), High Speed by Meglass (unit 30) and the ninja's follow-up by
    /// the ninja as record 51 (<c>FIGT.ADV:1922-1980</c>).
    /// </summary>
    [Theory]
    [InlineData(27, ExchangeEffectKind.WhirlwindShot)]
    [InlineData(30, ExchangeEffectKind.HighSpeed)]
    [InlineData(51, ExchangeEffectKind.NinjaFollowUp)]
    public void Special_3_plays_Whirlwind_Shot_High_Speed_or_the_ninja_by_the_actor(int unit, ExchangeEffectKind kind)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)0;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)3;
        s.Bt_SpecialAtk = (Sys3Int)3;
        s.Unit_Cur = (Sys3Int)unit;
        Assert.Equal(new ExchangeEffect(kind, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>
    /// The ninja's follow-up is each ninja's own, told by her sprite in record 51 (<c>FIGT.ADV:1922-1968</c>; owner,
    /// 2026-10-21); any other sprite plays the Ninja troop's.
    /// </summary>
    [Theory]
    [InlineData(1164, ExchangeEffectKind.KanamiFollowUp)]
    [InlineData(1176, ExchangeEffectKind.FreyaFollowUp)]
    [InlineData(1182, ExchangeEffectKind.KaoruFollowUp)]
    [InlineData(1185, ExchangeEffectKind.WichitaFollowUp)]
    [InlineData(0, ExchangeEffectKind.NinjaFollowUp)]
    public void The_ninja_follow_up_is_each_ninja_s_own_by_her_sprite(int sprite, ExchangeEffectKind kind)
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)0;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)3;
        s.Bt_SpecialAtk = (Sys3Int)3;
        s.Unit_Cur = (Sys3Int)51;
        s.Units[51].CgFull = (Sys3Int)sprite;
        Assert.Equal(new ExchangeEffect(kind, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>Special 4 is Miki's (unit 32) Vanish Bomb (<c>FIGT.ADV:1560-1570</c>).</summary>
    [Fact]
    public void Special_4_by_Miki_plays_Vanish_Bomb()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)2;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)1;
        s.Bt_SpecialAtk = (Sys3Int)4;
        s.Unit_Cur = (Sys3Int)32;
        Assert.Equal(new ExchangeEffect(ExchangeEffectKind.VanishBomb, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>Special 5 is Patton's (unit 18) C. War Dance (<c>FIGT.ADV:1571-1577</c>).</summary>
    [Fact]
    public void Special_5_by_Patton_plays_War_Dance()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)0;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)3;
        s.Bt_SpecialAtk = (Sys3Int)5;
        s.Unit_Cur = (Sys3Int)18;
        Assert.Equal(new ExchangeEffect(ExchangeEffectKind.WarDance, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>Special 6 is Asuka's (unit 11) Chuhai (<c>FIGT.ADV:1551-1559</c>).</summary>
    [Fact]
    public void Special_6_by_Asuka_plays_Chuhai()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)2;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)1;
        s.Bt_SpecialAtk = (Sys3Int)6;
        s.Unit_Cur = (Sys3Int)11;
        Assert.Equal(new ExchangeEffect(ExchangeEffectKind.Chuhai, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>
    /// Special 10 is T2 Goliath's strike (<c>FIGT.ADV:1120-1148</c>): record 51 as "T2 Goliath", the player's action 0,
    /// the enemy's 99, <c>Bt_MsgMode</c> left as it was (here the enemy's magic, 2).
    /// </summary>
    [Fact]
    public void Special_10_by_record_51_plays_Goliath()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)0;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)2;
        s.Bt_SpecialAtk = (Sys3Int)10;
        s.Unit_Cur = (Sys3Int)51;
        Assert.Equal(new ExchangeEffect(ExchangeEffectKind.Goliath, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>
    /// Special 7 is Rolex's (unit 47) T2 Soaring Wave (<c>FIGT.ADV:1525-1531</c>): the enemy answers it (both actions 0)
    /// and <c>L_02528</c> sets <c>Bt_MsgMode</c> 3, so the player's plays.
    /// </summary>
    [Fact]
    public void Special_7_by_Rolex_plays_Soaring_Wave()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)0;
        s.Bt_EnemyAction = (Sys3Int)0;
        s.Bt_MsgMode = (Sys3Int)3;
        s.Bt_SpecialAtk = (Sys3Int)7;
        s.Unit_Cur = (Sys3Int)47;
        Assert.Equal(new ExchangeEffect(ExchangeEffectKind.SoaringWave, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>Isoroku (unit 27) without the special plays by her action.</summary>
    [Fact]
    public void Isoroku_without_the_special_plays_by_the_action()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)1;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)3;
        s.Unit_Cur = (Sys3Int)27;
        Assert.Equal(new ExchangeEffect(ExchangeEffectKind.Bow, BattleSide.Player), ExchangeEffect.Of(s));
    }

    /// <summary>A beam's actor without the special plays its magic.</summary>
    [Fact]
    public void A_beam_user_without_the_special_plays_by_the_action()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)2;
        s.Bt_EnemyAction = (Sys3Int)99;
        s.Bt_MsgMode = (Sys3Int)1;
        s.Unit_Cur = (Sys3Int)17;
        Assert.Equal(new ExchangeEffect(ExchangeEffectKind.Magic, BattleSide.Player), ExchangeEffect.Of(s));
    }

    [Fact]
    public void Nothing_plays_when_neither_side_fires()
    {
        var s = new GameState();
        s.Bt_PlayerAction = (Sys3Int)99;
        s.Bt_EnemyAction = (Sys3Int)99;
        Assert.Null(ExchangeEffect.Of(s));
    }

    /// <summary>
    /// The test over every capture, spread over <see cref="Shards"/> classes (<c>Captures0</c> and on) that run in
    /// parallel (Shards.cs).
    /// </summary>
    public abstract class CaptureShard(ITestOutputHelper output)
    {
        private const int Shards = 5;

        /// <summary>
        /// In every captured battle each exchange's shots start an effect: its side fires (its action is not 99, and it
        /// has a shot), and a two-sided exchange is direct against direct with the turn's actor in <c>Bt_MsgMode</c> (3 or 4);
        /// the troops of a Rance Attack or a By la Way fire nothing, nor a beam's, a War Dance's, a Chuhai's or a Goliath's (nor its commander at the commander), nor a Soaring Wave's (its
        /// commander fires two shots at the commander); a
        /// Whirlwind Shot or a High Speed fires only the commander's one shot at the commander. And each of the enemy's
        /// defences — its turn (an enemy panel highlighted, <c>Bt_MsgMode</c> 4) ending in a redraw without an exchange, its
        /// actor's state 20 — is one <see cref="BattlePresenter.EnemyDefence"/> at the actor's place (its Guard), and no
        /// other redraw notes one.
        /// </summary>
        [CaptureTheory]
        [ShardedData(nameof(BattleSceneTests.Captures), typeof(BattleSceneTests), Shards)]
        public async Task Every_exchange_and_enemy_defence_plays_its_effect(string file)
        {
            var capture = GoldenCapture.Load(Path.Combine(LocalData.GoldenDirectory, file));
            var presenter = new BattlePresenter(Texts.Value, Cgs.Value, new BoxGlyphs());
            var wrong = new List<string>();
            var kinds = new Dictionary<string, int>();
            int exchanges = 0, defences = 0;
            bool engaged = false, enemyTurn = false;
            var view = new ObservingView(e =>
            {
                presenter.Show(e);
                if (e is EngagementSceneEvent)
                    engaged = true;
                if (e is PanelHighlightEvent highlight && BattleBoard.PanelPlace(highlight.Panel.Value) is { Side: BattleSide.Enemy })
                    enemyTurn = true;
                if (e is BattlefieldBackgroundEvent && presenter.State is { } r)
                {
                    int slot = r.Bt_EnemyActSlot.Value;
                    var actor = new BattlePlace(BattleSide.Enemy, slot);
                    int record = slot is >= 1 and <= 4 ? BattleBoard.Record(r, actor) : 0;
                    if (enemyTurn && !engaged && r.Bt_MsgMode == 4 && record != 0 && BattleBoard.StateOf(r, actor) == 20)
                    {
                        defences++;
                        if (presenter.EnemyDefences != defences || presenter.EnemyDefence != actor)
                            wrong.Add($"enemy defence {defences} by commander {record}: {presenter.EnemyDefences} noted, at {presenter.EnemyDefence}");
                    }
                    else if (presenter.EnemyDefences != defences)
                    {
                        wrong.Add($"a defence noted at {presenter.EnemyDefence} after exchange {exchanges} with no enemy turn defending: "
                            + $"turn {enemyTurn}, engaged {engaged}, mode {r.Bt_MsgMode.Value}, the enemy's acting slot {slot}");
                        defences = presenter.EnemyDefences;
                    }
                    (engaged, enemyTurn) = (false, false);
                }
                if (e is not ShotsFrameEvent { Step.Value: 0 } || presenter.State is not { } s)
                    return;
                exchanges++;
                if (presenter.Effect is not { } effect)
                {
                    wrong.Add($"exchange {exchanges}: no effect (actions {s.Bt_PlayerAction.Value}, {s.Bt_EnemyAction.Value})");
                    return;
                }
                string key = $"{effect.Side} {effect.Kind}";
                kinds[key] = kinds.GetValueOrDefault(key) + 1;
                bool player = effect.Side == BattleSide.Player;
                var shots = player
                    ? new[] { s.Bt_PTroopVsTroopShots, s.Bt_PTroopVsCmdrShots, s.Bt_PCmdrVsTroopShots, s.Bt_PCmdrVsCmdrShots }
                    : new[] { s.Bt_ETroopVsTroopShots, s.Bt_ETroopVsCmdrShots, s.Bt_ECmdrVsTroopShots, s.Bt_ECmdrVsCmdrShots };
                if ((player ? s.Bt_PlayerAction : s.Bt_EnemyAction) == 99 || shots.All(v => v == 0))
                    wrong.Add($"exchange {exchanges}: {key} does not fire");
                // Rance Attack (Kentarou's too) and By la Way are the commander's blow alone: the troops' shots are zeroed
                // (02-resolution.md §3.3).
                if (effect.Kind is ExchangeEffectKind.RanceAttack or ExchangeEffectKind.KentarouSlash or ExchangeEffectKind.ByLaWay && (shots[0] != 0 || shots[1] != 0))
                    wrong.Add($"exchange {exchanges}: {effect.Kind} with the troops firing");
                // A beam is the commander's blast at the troops: the troops' shots and the commander's at the commander
                // are zeroed (02-resolution.md §3.3).
                if (effect.Kind is ExchangeEffectKind.ShizukaBeam or ExchangeEffectKind.MagicBeam or ExchangeEffectKind.EpoLight or ExchangeEffectKind.ChizukoBeam or ExchangeEffectKind.NagiBeam && (shots[0] != 0 || shots[1] != 0 || shots[3] != 0))
                    wrong.Add($"exchange {exchanges}: {effect.Kind} with the troops or at the commander firing");
                // C. War Dance, Chuhai and T2 Goliath's strike are the commander's blows at the troops: the troops' shots and
                // the commander's at the commander are zeroed (02-resolution.md §3.3).
                if (effect.Kind is ExchangeEffectKind.WarDance or ExchangeEffectKind.Chuhai or ExchangeEffectKind.Goliath && (shots[0] != 0 || shots[1] != 0 || shots[3] != 0))
                    wrong.Add($"exchange {exchanges}: {effect.Kind} with the troops or at the commander firing");
                // Vanish Bomb is the commander's blast at the commander and the troops: the troops' shots are zeroed
                // (02-resolution.md §3.3).
                if (effect.Kind is ExchangeEffectKind.VanishBomb && (shots[0] != 0 || shots[1] != 0))
                    wrong.Add($"exchange {exchanges}: {effect.Kind} with the troops firing");
                // T2 Soaring Wave is the commander's blow at the troops and two shots at the commander: the troops' shots are
                // zeroed (02-resolution.md §3.3).
                if (effect.Kind is ExchangeEffectKind.SoaringWave && (shots[0] != 0 || shots[1] != 0 || shots[3] != 2))
                    wrong.Add($"exchange {exchanges}: {effect.Kind} with the troops firing or other than two shots at the commander");
                // Whirlwind Shot, High Speed and the ninja's follow-up are the commander's one shot at the commander: every
                // other shot is zeroed (02-resolution.md §3.3).
                if (effect.Kind is ExchangeEffectKind.WhirlwindShot or ExchangeEffectKind.HighSpeed or ExchangeEffectKind.NinjaFollowUp or ExchangeEffectKind.KanamiFollowUp
                    or ExchangeEffectKind.FreyaFollowUp or ExchangeEffectKind.KaoruFollowUp or ExchangeEffectKind.WichitaFollowUp && (shots[0] != 0 || shots[1] != 0 || shots[2] != 0 || shots[3] != 1))
                    wrong.Add($"exchange {exchanges}: {effect.Kind} firing other than the commander's one shot at the commander");
                bool twoSided = s.Bt_PlayerAction != 99 && s.Bt_EnemyAction != 99;
                if (twoSided && (s.Bt_PlayerAction != 0 || s.Bt_EnemyAction != 0 || s.Bt_MsgMode.Value is not (3 or 4)))
                    wrong.Add($"exchange {exchanges}: two-sided, actions {s.Bt_PlayerAction.Value}, {s.Bt_EnemyAction.Value}, mode {s.Bt_MsgMode.Value}");
            });
            await GoldenReplay.RunAsync(capture, view: view, started: presenter.Start);
            output.WriteLine($"{file}: {exchanges} exchanges, {defences} enemy defences; " + string.Join(", ", kinds.OrderBy(k => k.Key).Select(k => $"{k.Key} {k.Value}")));
            Assert.Empty(wrong);
            Assert.Equal(exchanges, presenter.EffectsStarted);
            Assert.Equal(defences, presenter.EnemyDefences);
        }
    }

    [Shard(0)] public sealed class Captures0(ITestOutputHelper output) : CaptureShard(output);
    [Shard(1)] public sealed class Captures1(ITestOutputHelper output) : CaptureShard(output);
    [Shard(2)] public sealed class Captures2(ITestOutputHelper output) : CaptureShard(output);
    [Shard(3)] public sealed class Captures3(ITestOutputHelper output) : CaptureShard(output);
    [Shard(4)] public sealed class Captures4(ITestOutputHelper output) : CaptureShard(output);

    private sealed class ObservingView(Action<ScriptEvent> show) : IScriptView
    {
        public Task ShowAsync(ScriptEvent scriptEvent)
        {
            show(scriptEvent);
            return Task.CompletedTask;
        }
    }
}
