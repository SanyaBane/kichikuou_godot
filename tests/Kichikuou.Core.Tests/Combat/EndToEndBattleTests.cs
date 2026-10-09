using Kichikuou.Core.Combat;
using Kichikuou.Core.Script;
using Kichikuou.Core.Sys3;
using static Kichikuou.Core.Tests.Combat.Sys3Assert;

namespace Kichikuou.Core.Tests.Combat;

/// <summary>
/// Whole battles through every part (flow F, resolution R1, flavour text R2): the wrappers
/// FIGT_A / FIGT_B, FIGT.ADV's round loop, the exchanges with FIGT_M and FIGTATTK, the deaths,
/// AwardExp and 死亡捕獲. The draw sequences are derived by hand from the script (line numbers in
/// the comments are FIGT.ADV unless another file is named); see docs/battle/03-rng-order.md.
/// </summary>
public class EndToEndBattleTests
{
    // Units and commanders whose FIGT_M lines draw nothing beyond the two entry draws
    // (FIGT_M.ADV:5 Y 4,2 and :25 / :2825 Y 4,3): units 42 (:2337) and 44 (:2455),
    // commanders 5, 6, 7 (:3058-3089). None of them has a story hook in 死亡捕獲.
    private const int UnitA = 44;
    private const int UnitB = 42;

    private static FlowRig NewRig(params int[] draws)
    {
        var rig = new FlowRig(draws);
        var s = rig.State;
        s.StoryStage = 1;
        s.Units[1].Status = 1;          // Rance alive at home (死亡捕獲.ADV:26-30)
        s.Prov_Cur = 20;
        s.Bt_TargetProv = 20;
        s.Provs[20].Owner = 4;
        s.Bt_BackgroundCg = 900;
        return rig;
    }

    private static void SetUnit(FlowRig rig, int id, int troops = 0, int hp = 50, int atk = 30, int def = 30,
        int attacks = 5, int level = 5, int troopAtk = 0, int troopDef = 0, int exp = 0, int recovery = 0)
    {
        var u = rig.State.Units[id];
        u.Troops = (Sys3Int)troops;
        u.MaxTroops = (Sys3Int)troops;
        u.HP = (Sys3Int)hp;
        u.MaxHP = (Sys3Int)hp;
        u.Atk = (Sys3Int)atk;
        u.Def = (Sys3Int)def;
        u.Attacks = (Sys3Int)attacks;
        u.Level = (Sys3Int)level;
        u.TroopAtk = (Sys3Int)troopAtk;
        u.TroopDef = (Sys3Int)troopDef;
        u.Exp = (Sys3Int)exp;
        u.Recovery = (Sys3Int)recovery;
        u.Status = 2;                   // deployed
    }

    private static void SetCmdr(FlowRig rig, int id, int troops = 0, int hp = 2, int atk = 1, int def = 1,
        int attacks = 5, int cls = 0, int troopAtk = 0, int troopDef = 0)
    {
        var c = rig.State.Cmdrs[id];
        c.Troops = (Sys3Int)troops;
        c.MaxTroops = (Sys3Int)troops;
        c.HP = (Sys3Int)hp;
        c.MaxHP = (Sys3Int)hp;
        c.Atk = (Sys3Int)atk;
        c.Def = (Sys3Int)def;
        c.Attacks = (Sys3Int)attacks;
        c.Class = (Sys3Int)cls;
        c.TroopAtk = (Sys3Int)troopAtk;
        c.TroopDef = (Sys3Int)troopDef;
        c.Status = 1;
    }

    private static FigtSelChoice Direct(int enemySlot = 1) => new(FigtSelCommand.Direct, (Sys3Int)(enemySlot + 3));

    private static int Count<T>(FlowRig rig) where T : ScriptEvent => rig.View.Events.OfType<T>().Count();

    /// <summary>
    /// The draws of one commanders-only exchange without specials (troops 0 on both sides,
    /// Unit_Attacks / Cmdr_Attacks 5 → RND(1), no draw): FIGT_M :5 and :25/:2825, the player
    /// commander's single shot :2648→:2191 (RND(1) no draw), the enemy commander's :2832→:2191,
    /// FIGTATTK.ADV:234, and R14 :1915 unless the commander fell.
    /// </summary>
    private static readonly int[] CmdrExchange = [2, 3, 100, 100, 2];

    // ------------------------------------------------------------------------------------
    // (a) FIGT_A, plains, 1 unit vs 1 commander, direct attacks to annihilation
    // ------------------------------------------------------------------------------------

    [Fact]
    public async Task Plains_attack_one_on_one_is_fought_to_annihilation()
    {
        int[] values =
        [
            2,                              // :17 R3 → 5 rounds
            // round 1, player (unit 44 acts first: troop tie goes to the player, Q-17)
            5,                              // :1587 R6
            1, 1,                           // FIGT_M :5, :25 (Bt_MsgMode 3 → player side, unit 44: no draw)
            50,                             // :2191 player commander's shot, hit chance 100 → hit (Cmdr HP 2 → 1)
            100,                            // :2191 enemy commander's shot, hit chance 1 → miss
            1,                              // FIGTATTK.ADV:234
            2,                              // :1915 R14 (Cmdr HP 1 ≠ 0)
            // round 1, enemy turn (one player slot: no R9-R12)
            2,                              // :1450 R13
            2, 3,                           // FIGT_M :5, :2825 (Bt_MsgMode 4 → enemy side, commander 5: no draw)
            30,                             // player commander counters → hit (Cmdr HP 1 → 0)
            99,                             // enemy shot → miss
            2,                              // FIGTATTK.ADV:234; commander falls → no R14 (:1902-1914 returns)
            // AwardExp
            10,                             // :2978 R15 → 15 EXP
            4,                              // :3057 R17 (Recovery ≠ 0)
            1,                              // :3100 R18 = RND(12 + new level 6) → case 1
            3,                              // :3102 R19 → 3 - OffAtkMod(0) ≠ 0 → +1
        ];
        var rig = NewRig(values);
        var s = rig.State;
        rig.PlayerParty(UnitA);
        rig.EnemyParty(5);
        SetUnit(rig, UnitA, exp: 240, recovery: 3);    // threshold 100 + 5 * 30 = 250
        SetCmdr(rig, 5);
        rig.Input.Commands.Enqueue(Direct());

        await rig.Battle.AttackProvinceAsync();

        Assert.Equal(
            [2, 10, .. CmdrExchange, 3, 3, .. CmdrExchange, 15, 5, 18, 3],
            rig.Random.Draws);
        Assert.Equal(0, rig.Random.Remaining);
        Is(2, s.Bt_Result);
        Is(0, s.Cmdrs[5].Status);
        Is(0, s.Cmdrs[5].HP);
        Is(0, s.Bt_EnemySlot1);
        Is(0, s.Bt_PlayerLosses);
        Is(1, s.Bt_EnemyLosses);
        Is(50, s.Units[UnitA].HP);
        // Level-up: EXP reset, +1 attack, level 6, recovery + R17, OffAtkMod raised.
        Is(0, s.Units[UnitA].Exp);
        Is(6, s.Units[UnitA].Attacks);
        Is(6, s.Units[UnitA].Level);
        Is(7, s.Units[UnitA].Recovery);
        Is(1, s.Units[UnitA].OffAtkMod);
        Is(1, s.Provs[20].Owner);      // FIGT_A.ADV:104-107
        Is(0, s.Bt_Kind);
        Is(0, s.Bt_TargetProv);
        Assert.Equal(1, Count<RoundHeaderEvent>(rig));
        Assert.Equal(2, Count<ExchangeResultsEvent>(rig));
    }

    // ------------------------------------------------------------------------------------
    // (b) FIGT_B, intercepted on the plains and won (troops on both sides)
    // ------------------------------------------------------------------------------------

    [Fact]
    public async Task Enemy_attack_intercepted_on_the_plains_and_won()
    {
        int[] values =
        [
            1,                              // :17 R3 → 4 rounds
            // round 1: the enemy acts first (40 troops < 100)
            2,                              // :1450 R13
            1, 1,                           // FIGT_M :5, :2825 (commander 6)
            // ComputeExchange (Bt_Kind 2, limit 5000, both armies fit):
            10, 20, 80, 30,                 // :2178 ×4 player troops → commander (100/40*2 = 4 shots, hit 72-1 = 71): 3 hits
            50,                             // :2191 player commander → troops (RND(1) → 1 shot, hit 3): miss
            5,                              // :2171 spread of 96*39/100 = 37: k = 3, 31 + RND(9) = 36
            60,                             // :2191 enemy commander → troops (1 shot, hit 4): miss
            2,                              // :2160 spread of 40*40/100 = 16: k = 1, 14 + RND(3) = 16
            1,                              // FIGTATTK.ADV:234; commander HP 3 - 3 = 0 → no R14
            3,                              // :2978 R15 → 8 EXP
            1,                              // FIGT_B.ADV:282 R2 (province 35 is in 31..49)
        ];
        var rig = NewRig(values);
        var s = rig.State;
        s.Prov_Cur = 35;
        s.Bt_TargetProv = 0;
        s.Provs[35].Owner = 1;
        s.Provs[40].Owner = 0;          // FIGT_B.ADV:285-287 → RetVal 4, no Elena scene
        s.Ev_PhaseNation = 3;
        rig.EnemyParty(6);
        SetUnit(rig, UnitA, troops: 100, hp: 30, atk: 1, def: 50, attacks: 1, level: 0, troopAtk: 10, troopDef: 5);
        s.Units[UnitA].Status = 1;      // at home until Sﾌﾞﾀｲ deploys it
        SetCmdr(rig, 6, troops: 40, hp: 3, atk: 1, def: 1, attacks: 1, troopAtk: 10, troopDef: 5);
        rig.Input.MenuAnswers.Enqueue(1);   // Intercept
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Sﾌﾞﾀｲ.ADV")
            {
                s.Bt_PlayerParty1 = UnitA;
                s.Units[UnitA].Status = 2;
                s.CallArg = 501;        // plains
            }
        };

        await rig.Battle.DefendProvinceAsync();

        Assert.Equal([2, 3, 2, 3, 100, 100, 100, 100, 100, 9, 100, 3, 2, 15, 5], rig.Random.Draws);
        Assert.Equal(0, rig.Random.Remaining);
        Is(2, s.Bt_Result);
        Is(0, s.Bt_Kind);
        Is(5000, s.Bt_TroopLimit);
        Is(0, s.Bt_WallBonus);
        Is(84, s.Units[UnitA].Troops);  // 100 - 16
        Is(30, s.Units[UnitA].HP);
        Is(4, s.Cmdrs[6].Troops);       // 40 - 36
        Is(0, s.Cmdrs[6].HP);
        Is(0, s.Cmdrs[6].Status);
        Is(16, s.Bt_PlayerLosses);
        Is(37, s.Bt_EnemyLosses);       // 36 troops + the commander (Q16: damage, not real losses)
        Is(8, s.Units[UnitA].Exp);
        Is(16, s.History[0].Casualties);
        Is(1, s.Provs[35].Owner);
        Is(35, s.Prov_Cur);
        Is(0, s.Bt_TargetProv);
        Assert.Contains(("Sﾌﾞﾀｲ.ADV", 0), rig.Story.Calls);
        Assert.DoesNotContain(rig.Story.Calls, c => c.File == "Gｴﾚﾅ.ADV");
        Assert.Contains(rig.View.Events, e => e is MessageEvent { Text.File: "FIGT_B.ADV", Text.Line: 257 });
    }

    // ------------------------------------------------------------------------------------
    // (c) timeout, real generator
    // ------------------------------------------------------------------------------------

    /// <summary>Records every <c>Y 4</c> of a real generator.</summary>
    private sealed class RecordingRandom(IScriptRandom inner) : IScriptRandom
    {
        public List<(int N, int Value)> Draws { get; } = [];

        public Sys3Int Next(Sys3Int n)
        {
            var value = inner.Next(n);
            if (n.Value > 1)
                Draws.Add((n.Value, value.Value));
            return value;
        }
    }

    [Theory]
    [InlineData(12345u)]
    [InlineData(2026u)]
    public async Task Attack_that_reaches_the_round_limit_is_lost(uint seed)
    {
        var random = new RecordingRandom(new XSystem35Random(seed));
        var rig = NewRig();
        var battle = new Battle(rig.State, random, rig.View, rig.Input, rig.Story);
        var s = rig.State;
        rig.PlayerParty(UnitA);
        rig.EnemyParty(7);
        // Hit chances 1 % both ways: at most 20 HP lost in five rounds, nobody falls.
        SetUnit(rig, UnitA, hp: 100, atk: 1, def: 30, level: 0);
        SetCmdr(rig, 7, hp: 100, atk: 1, def: 30);
        for (int i = 0; i < 5; i++)
            rig.Input.Commands.Enqueue(Direct());

        await battle.AttackProvinceAsync();

        int rounds = 3 + random.Draws[0].Value;
        // Per round: the player's manual attack (R6 + exchange + R14), then the enemy's turn
        // (R13 + exchange + R14); then AwardExp's R15. No level-up, no story draws.
        var expected = new List<int> { 2 };
        for (int r = 0; r < rounds; r++)
        {
            expected.AddRange([10, .. CmdrExchange, 3]);
            expected.AddRange([3, .. CmdrExchange, 3]);
        }
        expected.Add(15);
        Assert.Equal(expected, random.Draws.Select(d => d.N));

        Is(1, s.Bt_Result);             // attacker timed out (:472-474 → L_00c73)
        Is(0, s.Bt_RoundsLeft);
        Assert.Equal(rounds, Count<RoundHeaderEvent>(rig));
        Assert.Equal(2 * rounds, Count<ExchangeResultsEvent>(rig));
        Is(4, s.Provs[20].Owner);       // not taken
        Is(1, s.Cmdrs[7].Status);
        Is(2, s.Units[UnitA].Status);
        Assert.True(s.Units[UnitA].HP.Value >= 100 - 2 * rounds);
        Assert.True(s.Cmdrs[7].HP.Value >= 100 - 2 * rounds);
        Is((5 + random.Draws[^1].Value) / 3, s.Units[UnitA].Exp);   // a third on a loss (:2988-2990)
        Is(0, s.Bt_PlayerLosses);
        Is(0, s.Bt_EnemyLosses);
    }

    // ------------------------------------------------------------------------------------
    // (d) a player commander (not Rance) falls mid-battle; the next unit fights on and wins
    // ------------------------------------------------------------------------------------

    [Fact]
    public async Task Player_commander_death_compacts_the_slots_and_the_battle_goes_on()
    {
        int[] values =
        [
            1,                              // R3
            // unit 44 (slot 1) attacks commander 6
            3,                              // R6
            2, 2,                           // FIGT_M (unit 44)
            40,                             // player shot, hit 100 → Cmdr HP 2 → 1
            50,                             // enemy shot, hit 100 - 5 - 1 = 94 → unit 44 HP 1 → 0
            1,                              // FIGTATTK
            1,                              // R14 at :1915 on Cmdr_Cur after the redraw (commander 6, HP 1):
                                            //   Unit_Cur is unit 42 now, but Cmdr_HP > 1 fails → no ninja
            // unit 42, compacted into slot 1 with slot 2's state 0, acts in the same round
            7,                              // R6
            1, 3,                           // FIGT_M (unit 42, Bt_MsgMode 3 → :2348)
            60,                             // player shot → Cmdr HP 0
            70,                             // enemy shot, hit 8 - 5 - 1 = 2 → miss
            2,                              // FIGTATTK; commander falls → no R14
            5,                              // R15 → 10 EXP
        ];
        var rig = NewRig(values);
        var s = rig.State;
        rig.PlayerParty(UnitA, UnitB);
        rig.EnemyParty(6);
        SetUnit(rig, UnitA, hp: 1, def: 1);
        SetUnit(rig, UnitB);
        SetCmdr(rig, 6, atk: 30);
        rig.Input.Commands.Enqueue(Direct());
        rig.Input.Commands.Enqueue(Direct());

        await rig.Battle.AttackProvinceAsync();

        Assert.Equal([2, 10, .. CmdrExchange, 3, 10, .. CmdrExchange, 15], rig.Random.Draws);
        Assert.Equal(0, rig.Random.Remaining);
        Is(2, s.Bt_Result);
        Is(0, s.Units[UnitA].Status);
        Is(0, s.Units[UnitA].HP);
        Is(0, s.Units[UnitA].Exp);       // a fallen unit is no longer in a slot: no EXP
        Is(UnitB, s.Bt_PlayerSlot1);
        Is(0, s.Bt_PlayerSlot2);
        Is(2, s.Units[UnitB].Status);
        Is(10, s.Units[UnitB].Exp);
        Is(1, s.Bt_PlayerLosses);
        Is(1, s.Bt_EnemyLosses);
        Is(0, s.Cmdrs[6].Status);
        Is(1, s.Provs[20].Owner);
        // Both menus were offered to slot 1: unit 44, then unit 42 after the compaction.
        Assert.Equal([1, 1], rig.Input.CommandMenus.Select(m => (int)m.ActingSlot));
        Assert.Contains(rig.View.Events, e => e is MessageEvent { Text.File: "FIGTﾐｶDE.ADV" });
    }

    // ------------------------------------------------------------------------------------
    // (e) a special attack: Rance Attack after a Wait
    // ------------------------------------------------------------------------------------

    [Fact]
    public async Task Rance_attack_after_gaining_focus()
    {
        int[] values =
        [
            1,                              // R3 → 4 rounds
            // round 1: Rance waits (no draw for anyone but Miki, :1280); the class-3 enemy idles
            2,                              // :1450 R13, then class 3 → L_0232f (Q-14)
            // round 2: Rance Attack (special 1, one Focus spent)
            8,                              // :1587 R6
            1, 2,                           // FIGT_M :5, :25; unit 1 with Bt_SpecialAtk 1 → :8965, no draw
            2,                              // :2648 RND(Unit_Attacks / 5 = 2) → 2 shots
            10, 20,                         // :2191 ×2 (hit 100)
            50,                             // :2191 enemy commander (Bt_EnemyAction 99: computed from stale
                                            //   Tmp0001/0002, then zeroed at :2853-2856)
            2,                              // :2648 again — special 1 re-runs L_0412d (:2324-2325)
            30, 40,                         // :2191 ×2 → 2 damage, Cmdr HP 2 → 0
            1,                              // FIGTATTK; commander falls → no R14
            1,                              // R15 → 6 EXP
        ];
        var rig = NewRig(values);
        var s = rig.State;
        rig.PlayerParty(1);
        rig.EnemyParty(7);
        SetUnit(rig, 1, attacks: 10);
        SetCmdr(rig, 7, cls: 3);
        rig.Input.Commands.Enqueue(new FigtSelChoice(FigtSelCommand.Wait, 99));
        rig.Input.Commands.Enqueue(new FigtSelChoice(FigtSelCommand.RanceAttack, 4));

        await rig.Battle.AttackProvinceAsync();

        Assert.Equal([2, 3, 10, 2, 3, 2, 100, 100, 100, 2, 100, 100, 2, 15], rig.Random.Draws);
        Assert.Equal(0, rig.Random.Remaining);
        Is(2, s.Bt_Result);
        Is(0, s.Bt_PSlot1Focus);
        Is(0, s.Cmdrs[7].Status);
        Is(2, s.Units[1].Status);
        Is(6, s.Units[1].Exp);
        Is(1, s.Provs[20].Owner);
        Is(0, s.Bt_SkillSfx);           // reset by FIGTATTK's L_00796
        Assert.Equal(2, Count<RoundHeaderEvent>(rig));
        Assert.Equal(1, Count<ExchangeResultsEvent>(rig));
        var secondMenu = rig.Input.CommandMenus[1];
        Assert.NotNull(secondMenu.Find(FigtSelCommand.RanceAttack));
        Assert.Null(rig.Input.CommandMenus[0].Find(FigtSelCommand.RanceAttack));
        var scene = Assert.Single(rig.View.Events.OfType<EngagementSceneEvent>());
        Assert.Equal(new[] { (Sys3Int)1520 }, scene.PlayerSprites);    // :1660 Rance's special CG
        Assert.Contains(rig.View.Events, e => e is SoundPairEvent { First.Value: 31 });   // skill 1 sound
        Assert.Contains(rig.View.Events, e => e is MessageEvent { Text.File: "FIGT_M.ADV", Text.Line: 8965 });
    }

    // ------------------------------------------------------------------------------------
    // (f) Q-27: both commanders fall in one exchange, the enemy not in the last slot
    // ------------------------------------------------------------------------------------

    private static FlowRig Q27Rig(int[] values)
    {
        var rig = NewRig(values);
        rig.PlayerParty(UnitA, UnitB);
        rig.EnemyParty(5, 6);
        SetUnit(rig, UnitA, hp: 1, def: 1);
        SetUnit(rig, UnitB);
        SetCmdr(rig, 5, hp: 1, atk: 30);
        SetCmdr(rig, 6, hp: 20, atk: 1, def: 30);
        return rig;
    }

    private static readonly int[] Q27FirstExchange =
    [
        1,                                  // R3
        4,                                  // R6 (unit 44 → enemy slot 1, commander 5)
        1, 1,                               // FIGT_M
        1,                                  // player shot → commander 5 HP 1 → 0
        1,                                  // enemy shot (hit 94) → unit 44 HP 1 → 0
        1,                                  // FIGTATTK
        2,                                  // :1915 R14: drawn because the HP test at :1902 reads
                                            //   commander 6 (Cmdr_Cur left on the last enemy slot by
                                            //   the redraw at :1897), not the fallen commander 5
    ];

    [Fact]
    public async Task Q27_enemy_death_in_the_same_exchange_is_not_processed()
    {
        var rig = Q27Rig(Q27FirstExchange);
        var s = rig.State;
        rig.Input.Commands.Enqueue(Direct());

        // Runs until unit 42's command selection, which the test does not answer.
        Assert.True(await FlowRig.UntilStub(rig.Battle.AttackProvinceAsync));

        Assert.Equal([2, 10, .. CmdrExchange, 3], rig.Random.Draws);
        Is(0, s.Units[UnitA].Status);
        Is(UnitB, s.Bt_PlayerSlot1);
        Is(0, s.Bt_PlayerSlot2);
        // QUIRK Q-27: commander 5 is at 0 HP but keeps its status and its slot.
        Is(0, s.Cmdrs[5].HP);
        Is(1, s.Cmdrs[5].Status);
        Is(5, s.Bt_EnemySlot1);
        Is(6, s.Bt_EnemySlot2);
        Is(1, s.Bt_PlayerLosses);
        Is(0, s.Bt_EnemyLosses);
        Assert.DoesNotContain(rig.View.Events, e => e is MessageEvent { Text.File: "FIGTﾃｷDE.ADV" });
        // Unit 42 inherited slot 2's state 0 and acts next, still in round 1.
        Assert.Equal(2, rig.Input.CommandMenus.Count);
        Assert.Equal(1, Count<RoundHeaderEvent>(rig));
    }

    [Fact]
    public async Task Q27_zero_hp_commander_falls_when_next_struck()
    {
        int[] values =
        [
            .. Q27FirstExchange,
            // unit 42 strikes the 0-HP commander 5
            6,                              // R6
            2, 2,                           // FIGT_M
            90,                             // player shot (hit 100)
            80,                             // enemy shot, hit 8 - 5 - 1 = 2 → miss
            2,                              // FIGTATTK; Cmdr HP 0 → the death is processed now, no R14
        ];
        var rig = Q27Rig(values);
        var s = rig.State;
        rig.Input.Commands.Enqueue(Direct());
        rig.Input.Commands.Enqueue(Direct());

        // Runs until commander 6's turn draws R13 (:1450), which the test does not script.
        Assert.True(await FlowRig.UntilStub(rig.Battle.AttackProvinceAsync));

        Assert.Equal([2, 10, .. CmdrExchange, 3, 10, .. CmdrExchange, 3], rig.Random.Draws);
        Is(0, s.Cmdrs[5].Status);
        Is(1, s.Bt_EnemyLosses);
        Is(6, s.Bt_EnemySlot1);
        Is(0, s.Bt_EnemySlot2);
        Is(1, s.Bt_ESlot1State);        // commander 6 is acting (slot 2's state 0 moved up)
        Is(6, s.Cmdr_Cur);
        Assert.Contains(rig.View.Events, e => e is MessageEvent { Text.File: "FIGTﾃｷDE.ADV" });
    }

    // ------------------------------------------------------------------------------------
    // Smoke: random parties, stats and commands; every battle must reach an exit
    // ------------------------------------------------------------------------------------

    /// <summary>Answers every request with a random allowed option.</summary>
    private sealed class RandomInput(Random rng) : IBattleInput
    {
        private int _selects;

        public Task<Sys3Int> WaitForKeyAsync() => Task.FromResult(Sys3Int.Zero);

        public Task<Sys3Int> PauseAsync(Sys3Int ticks, bool cancelable) => Task.FromResult(Sys3Int.Zero);

        public Task<FigtSelChoice> SelectCommandAsync(FigtSelMenu menu)
        {
            if (++_selects > 400)
                throw new InvalidOperationException("The battle does not end.");
            var options = menu.Options.Where(o => o.Command != FigtSelCommand.Retreat || rng.Next(10) == 0).ToList();
            var option = options[rng.Next(options.Count)];
            return Task.FromResult(new FigtSelChoice(option.Command, option.Arguments[rng.Next(option.Arguments.Count)]));
        }

        public Task<Sys3Int?> ChooseAsync(BattleMenu menu) =>
            Task.FromResult<Sys3Int?>(menu.Options[rng.Next(menu.Options.Count)].Code);

        public Task<Sys3Int> GetPlayingTrackAsync() => Task.FromResult((Sys3Int)3);
    }

    [Fact]
    public async Task Random_battles_always_reach_an_exit()
    {
        var failures = new List<string>();
        for (int seed = 1; seed <= 500; seed++)
        {
            var rng = new Random(seed);
            var rig = NewRig();
            var s = rig.State;
            s.Prov_Cur = (Sys3Int)(rng.Next(5) == 0 ? 82 : rng.Next(2, 30));   // 82: Land of Death
            s.Bt_TargetProv = s.Prov_Cur;
            s.Provs[s.Prov_Cur].BattleArea = (Sys3Int)rng.Next(100, 3000);
            s.Bt_WallBonus = (Sys3Int)(rng.Next(3) == 0 ? rng.Next(1, 30) : 0);
            s.Bt_EnemyRetreatMode = (Sys3Int)rng.Next(0, 7);
            s.Kaiju_AmoebaAttacks = 100;
            s.Kaiju_JagiJagiAttacks = 80;
            if (rng.Next(6) == 0)
            {
                s.Goliath_State = 1;
                s.Goliath_Prov = s.Prov_Cur;
            }
            int[] units = Enumerable.Range(2, 49).Where(u => u != 25).OrderBy(_ => rng.Next()).Take(rng.Next(1, 5)).ToArray();
            if (rng.Next(4) == 0)
                units[0] = 1;
            int[] cmdrs = Enumerable.Range(1, 199).OrderBy(_ => rng.Next()).Take(rng.Next(1, 5)).ToArray();
            rig.PlayerParty(units);
            rig.EnemyParty(cmdrs);
            int[] classes = [0, 1, 2, 3, 10, 11];
            foreach (int id in units)
            {
                SetUnit(rig, id, troops: rng.Next(3) == 0 ? 0 : rng.Next(1, 4000), hp: rng.Next(1, 30),
                    atk: rng.Next(1, 30), def: rng.Next(1, 30), attacks: rng.Next(1, 40), level: rng.Next(0, 20),
                    troopAtk: rng.Next(1, 30), troopDef: rng.Next(1, 30), exp: rng.Next(0, 300), recovery: rng.Next(0, 5));
                var u = s.Units[id];
                u.Magic = (Sys3Int)rng.Next(0, 30);
                u.Class = (Sys3Int)classes[rng.Next(classes.Length)];
                u.Partner = (Sys3Int)rng.Next(0, 50);
                u.Item = (Sys3Int)new[] { 0, 0, 0, 1, 3, 26, 27, 28, 29 }[rng.Next(9)];
                u.OffAtkMod = (Sys3Int)rng.Next(0, 20);
                u.DefDefMod = (Sys3Int)rng.Next(0, 20);
            }
            foreach (int id in cmdrs)
            {
                SetCmdr(rig, id, troops: rng.Next(3) == 0 ? 0 : rng.Next(1, 4000), hp: rng.Next(1, 30),
                    atk: rng.Next(1, 30), def: rng.Next(1, 30), attacks: rng.Next(1, 40),
                    cls: classes[rng.Next(classes.Length)], troopAtk: rng.Next(1, 30), troopDef: rng.Next(1, 30));
                s.Cmdrs[id].Magic = (Sys3Int)rng.Next(0, 30);
                s.Cmdrs[id].Strategy = (Sys3Int)rng.Next(0, 100);
            }
            var battle = new Battle(s, new XSystem35Random((uint)seed), rig.View, new RandomInput(rng), rig.Story);
            try
            {
                await battle.AttackProvinceAsync();
                if (s.Bt_Result.Value is not (1 or 2))
                    failures.Add($"seed {seed}: Bt_Result {s.Bt_Result}");
            }
            catch (ScenarioJumpException)
            {
                // Rance fell outside the prologue: game over.
            }
            catch (Exception e)
            {
                failures.Add($"seed {seed}: {e.GetType().Name}: {e.Message}");
            }
        }
        Assert.Empty(failures);
    }

    // ------------------------------------------------------------------------------------
    // Seam regression: R1's window waits are IK 0 like everyone else's
    // ------------------------------------------------------------------------------------

    private static FlowRig ExchangeRig(params int[] draws)
    {
        var rig = NewRig(draws);
        var s = rig.State;
        rig.SetPlayerSlot(1, UnitA);
        rig.SetEnemySlot(1, 6);
        SetUnit(rig, UnitA);
        SetCmdr(rig, 6, hp: 5);
        s.Unit_Cur = UnitA;
        s.Cmdr_Cur = 6;
        s.Bt_PlayerActSlot = 1;
        s.Bt_EnemyActSlot = 1;
        s.Bt_Kind = 1;
        s.Bt_TroopLimit = 5000;
        s.Bt_MsgMode = 3;
        return rig;
    }

    [Fact]
    public async Task Engagement_window_wait_goes_through_the_input()
    {
        // FIGT.ADV:1801 \L_05119 → WAO_SUB.ADV:510-519 IK 0, then FIGT_M's own line wait.
        var rig = ExchangeRig(1, 1);
        rig.Input.Keys.Enqueue(7);
        rig.Input.Keys.Enqueue(8);

        await rig.Battle.PresentEngagementAsync();

        Assert.Equal([2, 3], rig.Random.Draws);
        Assert.Equal(2, rig.Input.KeyWaits);
        Is(8, rig.State.RetVal);
    }

    [Fact]
    public async Task Defeat_scene_waits_go_through_the_input()
    {
        // FIGTATTK.ADV:247 IK 0, then FIGTﾐｶDE.ADV:662 and :725 (\L_00ecb → WAO_SUB op 11).
        var rig = ExchangeRig(1, 1);
        var s = rig.State;
        s.Units[UnitA].HP = 1;
        s.Bt_ECmdrVsCmdrDmg = 1;

        await rig.Battle.ApplyExchangeAsync();

        Assert.Equal([2, 3], rig.Random.Draws);   // FIGTATTK.ADV:234, then R14 on commander 6
        Is(0, s.Units[UnitA].Status);
        Assert.Equal(3, rig.Input.KeyWaits);
    }

    // ------------------------------------------------------------------------------------
    // More control flow across the parts: ninja recursion, Goliath, Miki's loop
    // ------------------------------------------------------------------------------------

    [Fact]
    public async Task Ninja_follow_up_runs_a_nested_exchange_inside_the_battle()
    {
        int[] values =
        [
            1,                              // R3
            // Menad (unit 10) attacks commander 6
            5,                              // R6
            1, 1,                           // FIGT_M (unit 10 vs commander 6 → :8261, no draw)
            10,                             // player shot → Cmdr HP 3 → 2
            100,                            // enemy shot → miss
            1,                              // FIGTATTK
            1,                              // :1915 R14 = 1, Unit HP ≠ 0, Cmdr HP 2 > 1, Bt_MsgMode 3 → Kanami
            // nested exchange (L_02ff0): unit record 51 as special 3
            1, 1,                           // FIGT_M (unit 51, face 9 → :2761, no draw)
            5,                              // :2191 RND(Unit_Attacks 0 / 5) does not draw → 1 shot
            100,                            // enemy commander (Bt_EnemyAction 99, zeroed)
            1,                              // FIGTATTK; special 3 → 1 damage, Cmdr HP 2 → 1
            2,                              // nested R14 (Cmdr HP 1 ≠ 0); Cmdr HP > 1 fails anyway
            // enemy turn
            2,                              // R13
            2, 3,                           // FIGT_M (commander 6)
            10,                             // Menad's counter → Cmdr HP 1 → 0
            100,                            // enemy shot → miss
            2,                              // FIGTATTK; commander falls → no R14
            4,                              // R15 → 9 EXP
        ];
        var rig = NewRig(values);
        var s = rig.State;
        rig.PlayerParty(10);
        rig.EnemyParty(6);
        SetUnit(rig, 10);
        SetCmdr(rig, 6, hp: 3);
        rig.Input.Commands.Enqueue(Direct());

        await rig.Battle.AttackProvinceAsync();

        Assert.Equal(
            [2, 10, .. CmdrExchange, 3, .. CmdrExchange, 3, 3, .. CmdrExchange, 15],
            rig.Random.Draws);
        Assert.Equal(0, rig.Random.Remaining);
        Is(2, s.Bt_Result);
        Is(0, s.Cmdrs[6].Status);
        Is(9, s.Units[10].Exp);
        Is(0, s.F_Kanami_NinjaUsed);    // QUIRK 02 Q14: tested, never set
        Is(1164, s.Units[51].CgFull);
        Is(9, s.Units[51].FaceCg);
        Is(3, s.Units[51].HP);
        Assert.Equal(3, Count<ExchangeResultsEvent>(rig));
        Assert.Contains(rig.View.Events, e => e is MessageEvent { Text.File: "FIGT_M.ADV", Text.Line: 2761 });
        Assert.Contains(rig.View.Events, e => e is EngagementSceneEvent { PlayerSprites: [{ Value: 1164 }] });
    }

    [Fact]
    public async Task Goliath_strikes_before_the_first_actor_without_a_retreat_check()
    {
        int[] values =
        [
            1,                              // R3
            // L_01abd (:1120-1148) from the first actor search (:327-329)
            1, 1,                           // FIGT_M: Bt_MsgMode 3 left by an earlier battle (Q11) →
                                            //   player side, unit 51 with face 0 → no line
            50,                             // :2191 phantom shot: RND(Unit_Attacks 0) → 0 shots, one roll (Q3)
            2,                              // :2160 enemy troops → commander (Bt_EnemyAction 99): 100 shots,
                                            //   stale Tmp0001 0 / Tmp0002 5 → hit 3 → spread of 3
            100,                            // :2191 enemy commander → commander (zeroed)
            1,                              // :2160 enemy troops → troops: stale Tmp0001 1 → hit 4 → spread of 4
            1,                              // FIGTATTK; special 10 → 100 shots, 50 troops lost
            2,                              // R14: Unit HP 0 → no ninja
        ];
        var rig = NewRig(values);
        var s = rig.State;
        rig.PlayerParty(UnitA);
        rig.EnemyParty(6);
        SetUnit(rig, UnitA);
        SetCmdr(rig, 6, troops: 100, hp: 10, troopDef: 5);
        s.Goliath_State = 1;
        s.Goliath_Prov = 20;
        s.Bt_MsgMode = 3;
        s.Bt_EnemyRetreatMode = 20;

        // Runs until unit 44's command selection after the strike.
        Assert.True(await FlowRig.UntilStub(rig.Battle.AttackProvinceAsync));

        Assert.Equal([2, 2, 3, 100, 3, 100, 3, 2, 3], rig.Random.Draws);
        Is(2, s.Goliath_State);
        Is(50, s.Cmdrs[6].Troops);
        Is(50, s.Bt_EnemyLosses);
        Is(10, s.Cmdrs[6].HP);
        Is(0, s.Bt_EnemyRetreat);       // QUIRK 02 Q25: no mode 20/50 check after the strike
        Is(0, s.Units[51].HP);
        Assert.Single(rig.Input.CommandMenus);
        var scene = Assert.Single(rig.View.Events.OfType<EngagementSceneEvent>());
        Assert.Empty(scene.PlayerSprites);
    }

    [Fact]
    public async Task Miki_turns_a_direct_attack_into_vanish_bomb_until_the_roll_misses()
    {
        int[] values =
        [
            1,                              // R3
            // round 1: Miki waits
            2,                              // :1281 R8 → focuses (1 charge)
            2,                              // R13, class 3 → idle
            // round 2: Miki's direct attack
            1,                              // :1587 R6 = 1 → L_0249c: Vanish Bomb, 3 Focus (1 → 0, saturating)
            1,                              // :1587 again → Vanish Bomb again (0 → 0)
            4,                              // :1587 → on to the exchange (ResolveMagic, Bt_MsgMode 1)
            1, 1,                           // FIGT_M (unit 32 with a special → :1497, no draw)
            50,                             // player commander's magic shot (overridden by special 4)
            100,                            // enemy commander (Bt_EnemyAction 99, zeroed)
            1,                              // FIGTATTK; special 4 → Cmdr HP / 2 = 2 damage
            2,                              // R14
        ];
        var rig = NewRig(values);
        var s = rig.State;
        rig.PlayerParty(32);
        rig.EnemyParty(7);
        SetUnit(rig, 32);
        s.Units[32].Magic = 30;
        SetCmdr(rig, 7, hp: 4, cls: 3);
        s.Cmdrs[7].Magic = 1;
        rig.Input.Commands.Enqueue(new FigtSelChoice(FigtSelCommand.Wait, 99));
        rig.Input.Commands.Enqueue(Direct());

        // Runs until commander 7's R13 in round 2, which the test does not script.
        Assert.True(await FlowRig.UntilStub(rig.Battle.AttackProvinceAsync));

        Assert.Equal([2, 2, 3, 10, 10, 10, 2, 3, 100, 100, 2, 3, 3], rig.Random.Draws);
        Is(0, s.Bt_PSlot1Focus);
        Is(0, s.Bt_SpecialAtk);         // cleared again by the enemy turn (:1412)
        Is(4, s.Bt_MsgMode);            // set by the enemy turn (:1449) before its R13
        Is(2, s.Cmdrs[7].HP);
        var scene = Assert.Single(rig.View.Events.OfType<EngagementSceneEvent>());
        Assert.Equal(new[] { (Sys3Int)1529 }, scene.PlayerSprites);    // :1660-1701 Miki's special CG
        Assert.Contains(rig.View.Events, e => e is SoundPairEvent { First.Value: 39 });   // Bt_SkillSfx 7
        Assert.Equal("Vanish Bomb", s.Strings[7]);
        Assert.Contains(rig.View.Events, e => e is MessageEvent { Text.File: "FIGT_M.ADV", Text.Line: 1497 });
    }
}
