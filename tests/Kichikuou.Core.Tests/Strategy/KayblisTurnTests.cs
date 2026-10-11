using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// Kayblis's turn <c>Tｹｲﾌﾞﾘｽ</c> (<see cref="KayblisTurn"/>, milestone 4 step 1: the frame — week S,
/// the recoveries, the event chain, the dispatch, the Grief Valley (89) guards, the capture with
/// Helman's and Zeth's collapses; step 2: the units of Babolat, Redeye, Kite, Galtia and
/// Kesselring). Also compared with the decompile on random states (Content tests,
/// <c>NationTurnDifferentialTests</c>).
/// </summary>
public class KayblisTurnTests
{
    /// <summary>
    /// A quiet stage-3 week: past the war council, Medusa (134), Camilla (91), Kesselring (133) and
    /// Satella (157) dead, no scene's flag set; the generals Babolat (129) to Kesselring (133) dead
    /// and introduced, Piar's arc over, no war; Grief Valley (89) and Silky's Castle (81) not the
    /// player's; week 50. Such a turn draws Camilla's roll and Medusa's last one and calls nothing.
    /// </summary>
    private sealed class TurnRig
    {
        public TurnRig(params int[] draws)
        {
            Random = new ScriptedRandom(draws);
            Script = new ScriptContext(State, Random, View, Input, Story);
            State.F_DemonWar_Stage = 20;
            State.F_Medusa_State = 1;
            State.F_Silky_State = 10;
            State.N_Warg_Stage = 1;
            foreach (int general in new[] { 129, 130, 131, 132, 133 })
                State.Cmdrs[general].Status = 0;
            State.F_Babolat_State = 1;
            State.F_Redeye_State = 1;
            State.F_Calory_State = 1;
            State.F_Galtia_State = 1;
            State.F_Kesselring_State = 1;
            State.F_Piar_State = 100;
            State.Week = 50;
            State.Cmdr_StrBase = 1000;
        }

        public GameState State { get; } = new();
        public ScriptedRandom Random { get; }
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public ScriptContext Script { get; }

        public KayblisTurn Turn => new(Script, new Literals());

        public Task RunAsync() => Turn.RunAsync();

        public List<int> Lines => View.Events.OfType<MessageEvent>().Select(m => m.Text.Line).ToList();

        public List<int> Windows => View.Events.OfType<WindowEvent>().Select(w => (int)w.Op.Value).ToList();
    }

    /// <summary>The literals (<c>MS n, "…"</c>) as "file:line", standing in for the player's files: <c>MS 4</c> at <c>:323</c>, <c>MS 10</c> elsewhere.</summary>
    private sealed class Literals : Kichikuou.Core.Setup.IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) => new SetStringStatement(at, at.Line == 323 ? 4 : 10, at.ToString());
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, actual.Value);

    [Fact]
    public async Task A_quiet_week_names_the_army_and_gives_Monster_General_its_name_back()
    {
        var rig = new TurnRig(3, 5);
        rig.State.Ai_BabolatAtk = 11;
        rig.State.Ai_GaltiaAtk = 10;

        await rig.RunAsync();

        Assert.Empty(rig.Story.Calls);
        Assert.Equal([3, 5], rig.Random.Draws);                 // Camilla's roll, Medusa's last
        Is(0, rig.State.Ai_BabolatAtk);
        Is(0, rig.State.Ai_GaltiaAtk);
        Is(2, rig.State.Bt_EnemyRetreatMode);
        Assert.Equal("Tｹｲﾌﾞﾘｽ.ADV:274", rig.State.Strings[10]); // "Kayblis Unit"
        Is(86, rig.State.Cmdr_Cur);
        Assert.Equal("Tｹｲﾌﾞﾘｽ.ADV:323", rig.State.Strings[1000 + 85 * 3]);
        Assert.Equal([0, 87, 88, 0, 0, 0, 0, 0, 0],
            new[] { rig.State.Tmp0001, rig.State.Tmp0002, rig.State.Tmp0003, rig.State.Tmp0004, rig.State.Tmp0005,
                    rig.State.Tmp0006, rig.State.Tmp0007, rig.State.Tmp0008, rig.State.Tmp0009 }.Select(v => v.Value));
    }

    [Fact]
    public async Task In_week_S_the_war_council_plays_and_the_dispatch_follows_at_once()
    {
        var rig = new TurnRig();
        rig.State.F_DemonWar_Stage = 12;
        rig.State.Ai_KiteAtk = 10;
        rig.State.Cmdrs[91].Status = 1;
        rig.State.Cmdrs[91].HP = 5;
        rig.State.Cmdrs[91].MaxHP = 10;
        rig.State.Ev_KayblisAssault = 1;

        await rig.RunAsync();

        Assert.Equal([("Gｹｲﾌﾞﾘｽ.ADV", 1)], rig.Story.Calls);   // no assault, no event chain
        Assert.Empty(rig.Random.Draws);
        Is(1, rig.State.Ev_WeeklyEventDone);
        Is(10, rig.State.Ai_KiteAtk);                           // QUIRK: NAT-04 — no reset
        Is(5, rig.State.Cmdrs[91].HP);                          // no recovery
        Assert.Equal("Tｹｲﾌﾞﾘｽ.ADV:274", rig.State.Strings[10]);
    }

    [Fact]
    public async Task Kayblis_s_commanders_recover_and_Babolat_three_times()
    {
        var rig = new TurnRig(1, 1);
        rig.State.Ev_KayblisAssault = 1;
        var babolat = rig.State.Cmdrs[129];
        babolat.Status = 2;
        babolat.HP = 1;
        babolat.MaxHP = 100;
        babolat.Troops = 10;
        babolat.Recovery = 5;
        babolat.MaxTroops = 1000;
        rig.State.F_Babolat_State = 0;                          // introduced this week, no army yet
        var kayblis = rig.State.Cmdrs[135];
        kayblis.Status = 1;
        kayblis.HP = 39;
        kayblis.MaxHP = 40;
        kayblis.Troops = 990;
        kayblis.Recovery = 5;
        kayblis.MaxTroops = 1000;
        var silky = rig.State.Cmdrs[156];
        silky.HP = 3;
        silky.MaxHP = 50;

        await rig.RunAsync();

        Assert.Equal([("Gｹｲﾌﾞﾘｽ.ADV", 2), ("Gﾊﾞﾎﾞﾗ.ADV", 1)], rig.Story.Calls);
        Is(1, babolat.Status);
        Is(10, babolat.HP);                                     // QUIRK: NAT-05 — 1 + 3 × 3
        Is(55, babolat.Troops);                                 // 10 + 3 × (5 × 3)
        Is(40, kayblis.HP);                                     // capped
        Is(1000, kayblis.Troops);
        Is(3, silky.HP);                                        // dead: not recovered
        Is(0, silky.Status);
    }

    [Fact]
    public async Task Medusa_s_scenes_play_while_her_counter_stays_on_2_4_6()
    {
        var rig = new TurnRig(1);
        rig.State.Cmdrs[134].Status = 1;
        rig.State.N_Medusa_Events = 1;

        await rig.RunAsync();

        Is(2, rig.State.N_Medusa_Events);
        Assert.Equal([("Gﾒﾃﾞｨｳｻ.ADV", 2)], rig.Story.Calls);
        Assert.Equal([5], rig.Random.Draws);                    // the chain ends: no Camilla roll

        // QUIRK: NAT-25 — the next week the roll misses and the scene comes again.
        rig = new TurnRig(4);
        rig.State.Cmdrs[134].Status = 1;
        rig.State.N_Medusa_Events = 2;
        await rig.RunAsync();
        Is(2, rig.State.N_Medusa_Events);
        Assert.Equal([("Gﾒﾃﾞｨｳｻ.ADV", 2)], rig.Story.Calls);

        // Her first scene comes before any roll, while the week is not marked.
        rig = new TurnRig();
        rig.State.F_Medusa_State = 0;
        await rig.RunAsync();
        Assert.Equal([("Gﾒﾃﾞｨｳｻ.ADV", 1)], rig.Story.Calls);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task Camilla_s_scenes_follow_her_state_and_a_roll_in_three()
    {
        var rig = new TurnRig(1);
        rig.State.Cmdrs[91].Status = 1;
        rig.State.F_Camilla_State = 10;
        await rig.RunAsync();
        Assert.Equal([("Gｶﾐｰﾗ.ADV", 4)], rig.Story.Calls);

        rig = new TurnRig(2, 5);
        rig.State.Cmdrs[91].Status = 1;
        rig.State.F_Camilla_State = 11;
        await rig.RunAsync();
        Assert.Empty(rig.Story.Calls);                          // the roll missed: the chain goes on

        rig = new TurnRig(3);
        rig.State.Cmdrs[91].Status = 1;
        rig.State.F_Camilla_State = 9;
        await rig.RunAsync();
        Assert.Equal([("Gｶﾐｰﾗ.ADV", 3)], rig.Story.Calls);      // below 10, whatever the roll
    }

    [Fact]
    public async Task Silky_s_capture_does_not_end_the_chain()
    {
        var rig = new TurnRig(2);
        rig.State.F_Silky_State = 3;
        rig.State.Silky_NextEventWeek = 40;
        rig.State.N_Warg_Stage = 0;
        rig.State.F_Stessel_State = 4;

        await rig.RunAsync();

        // QUIRK: NAT-26 — Gｼﾙｷｨ 8 has no jump after it: Warg's scene follows in the same phase.
        Assert.Equal([("Gｼﾙｷｨ.ADV", 8), ("Gﾜｰｸﾞ.ADV", 3)], rig.Story.Calls);

        // Before the twelve weeks are up, nothing.
        rig = new TurnRig(2, 5);
        rig.State.F_Silky_State = 3;
        rig.State.Silky_NextEventWeek = 50;
        await rig.RunAsync();
        Assert.Empty(rig.Story.Calls);

        // A soul freed, outside the war only.
        rig = new TurnRig(2);
        rig.State.F_Silky_State = 2;
        rig.State.F_SoulMedusa = 1;
        await rig.RunAsync();
        Assert.Equal([("Gｼﾙｷｨ.ADV", 4)], rig.Story.Calls);
        rig = new TurnRig(2, 5);
        rig.State.F_Silky_State = 2;
        rig.State.F_SoulMedusa = 1;
        rig.State.F_Kayblis_War = 2;
        await Assert.ThrowsAsync<NotSupportedException>(rig.RunAsync);   // Kayblis's army at war
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public async Task The_guards_attack_Grief_Valley_again_after_every_battle_it_holds()
    {
        var rig = new TurnRig(2, 5, 30, 70);
        rig.State.Provs[89].Owner = 1;
        rig.State.Cmdrs[186].MaxHP = 30;
        rig.State.Cmdrs[186].Class = 2;
        int battles = 0;
        var seen = new List<int>();
        rig.Story.OnCall = (file, _) =>
        {
            if (file != "FIGT_B.ADV")
                return;
            seen.Add(rig.State.CallArg.Value);
            if (++battles == 2)
                rig.State.Provs[89].Owner = 8;                  // lost the second time
        };

        await rig.RunAsync();

        // QUIRK: NAT-02 — one battle per win, a new draw each time.
        Assert.Equal([("FIGT_B.ADV", 0), ("FIGT_B.ADV", 0)], rig.Story.Calls);
        Assert.Equal([3, 5, 100, 100], rig.Random.Draws);
        Is(90, rig.State.Cmdrs[186].Troops);                    // 20 + 70
        Is(1, rig.State.Cmdrs[186].Status);
        Is(30, rig.State.Cmdrs[186].HP);
        Assert.Equal([415, 415], seen);                         // Kayblis's portrait outside the war
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Is(186, rig.State.Bt_EnemyParty1);
        Assert.Equal([311, 111, 1, 1, 0, 312, 311, 111, 1, 1, 0, 312], rig.Windows);
        Assert.Equal([655, 662, 655, 662], rig.Lines);

        // Once Kayblis has moved, the guards stay home.
        rig = new TurnRig(2, 5);
        rig.State.Provs[89].Owner = 1;
        rig.State.F_Kayblis_Moved = 1;
        await rig.RunAsync();
        Assert.Empty(rig.Story.Calls);
    }

    [Fact]
    public async Task Babolat_s_introduction_marks_the_week_and_his_unit_marches_at_once()
    {
        var rig = new TurnRig(2, 5);
        rig.State.Cmdrs[129].Status = 1;
        rig.State.F_Babolat_State = 0;
        rig.State.F_Redeye_State = 0;                           // Redeye (130) dead: no army anyway
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Gﾊﾞﾎﾞﾗ.ADV")
                rig.State.F_Babolat_State = 1;
        };

        await rig.RunAsync();

        // Tower of Evil (78), the first on his line, is nobody's: taken without a battle.
        Assert.Equal([("Gﾊﾞﾎﾞﾗ.ADV", 1)], rig.Story.Calls);
        Is(1, rig.State.Ev_WeeklyEventDone);
        Is(8, rig.State.Provs[78].Owner);
        Assert.Equal([1628], rig.Lines);
        Is(0, rig.State.Ai_BabolatAtk);

        // Babolat's introduction marks the week, so Redeye's waits for another.
        rig = new TurnRig(2, 5);
        rig.State.F_Babolat_State = 0;
        rig.State.F_Redeye_State = 0;
        await rig.RunAsync();
        Assert.Equal([("Gﾊﾞﾎﾞﾗ.ADV", 1)], rig.Story.Calls);
    }

    /// <summary>The files called, in order.</summary>
    private static List<string> Files(TurnRig rig) => rig.Story.Calls.Select(c => c.File).ToList();

    [Fact]
    public async Task Babolat_attacks_the_first_player_province_on_his_line_and_once_more_after_a_held_battle()
    {
        var rig = new TurnRig(2, 5);
        rig.State.Cmdrs[129].Status = 1;
        rig.State.Provs[78].Owner = 8;                          // Tower of Evil, Great Wall of Helman A: Kayblis's
        rig.State.Provs[49].Owner = 8;
        rig.State.Provs[53].Owner = 1;                          // Ikoma: the player's
        var seen = new List<(int Prov, int Party, int Retreat, int Mark, string Army)>();
        rig.Story.OnCall = (file, _) =>
        {
            if (file != "FIGT_B.ADV")
                return;
            seen.Add((rig.State.Prov_Cur.Value, rig.State.Bt_EnemyParty1.Value, rig.State.Bt_EnemyRetreatMode.Value, rig.State.Ai_BabolatAtk.Value, rig.State.Strings[10]));
            rig.State.Bt_RoundsLeft = 0;                        // held to the time limit, both times
            rig.State.Bt_Result = 2;
        };

        await rig.RunAsync();

        // QUIRK: NAT-03 — fought again once; Gﾊﾞﾎﾞﾗ 2 after each battle at state 1.
        Assert.Equal(["FIGT_B.ADV", "Gﾊﾞﾎﾞﾗ.ADV", "FIGT_B.ADV", "Gﾊﾞﾎﾞﾗ.ADV"], Files(rig));
        Assert.Equal(2, rig.Story.Calls[1].Entry);
        Assert.Equal([(53, 129, 2, 10, "Tｹｲﾌﾞﾘｽ.ADV:731"), (53, 129, 2, 11, "Tｹｲﾌﾞﾘｽ.ADV:731")], seen);   // "Babolat Unit"
        Is(11, rig.State.Ai_BabolatAtk);
        Assert.Equal([3, 5], rig.Random.Draws);

        // A battle he does not survive ends his march.
        rig = new TurnRig(2, 5);
        rig.State.Cmdrs[129].Status = 1;
        rig.State.Provs[78].Owner = 1;
        rig.Story.OnCall = (file, _) => rig.State.Cmdrs[129].Status = 0;
        await rig.RunAsync();
        Assert.Equal(["FIGT_B.ADV"], Files(rig));
    }

    [Fact]
    public async Task Babolat_s_pitfall_replaces_the_battle()
    {
        var rig = new TurnRig(2, 5);
        rig.State.Cmdrs[129].Status = 1;
        rig.State.F_Babolat_State = 2;
        rig.State.Provs[78].Owner = 8;
        rig.State.Provs[49].Owner = 1;
        rig.State.Babolat_PitfallProv = 49;

        await rig.RunAsync();

        Assert.Equal([("Gﾊﾞﾎﾞﾗ.ADV", 4)], rig.Story.Calls);
        Is(0, rig.State.Ai_BabolatAtk);

        // Only on the player's province: one nobody holds is taken all the same.
        rig = new TurnRig(2, 5);
        rig.State.Cmdrs[129].Status = 1;
        rig.State.Babolat_PitfallProv = 78;
        await rig.RunAsync();
        Assert.Empty(rig.Story.Calls);
        Is(8, rig.State.Provs[78].Owner);
    }

    /// <summary>Redeye (130) and his three officers alive, his line Kayblis's up to Maunes (13), the player's.</summary>
    private static TurnRig RedeyeAtMaunes(params int[] draws)
    {
        var rig = new TurnRig(draws);
        foreach (int c in new[] { 148, 149, 150, 130 })
        {
            rig.State.Cmdrs[c].Status = 1;
            rig.State.Cmdrs[c].Troops = 500;
            rig.State.Cmdrs[c].MaxTroops = 1000;                // the recovery caps the troops at it
        }
        foreach (int p in new[] { 89, 50, 48, 47, 46, 41, 40, 44, 42, 39, 36, 31 })
            rig.State.Provs[p].Owner = 8;
        rig.State.Provs[13].Owner = 1;
        return rig;
    }

    [Fact]
    public async Task Redeye_fights_alone_at_Maunes_and_steps_his_state_after_the_battle()
    {
        var rig = RedeyeAtMaunes(2, 5, 1);
        int party = -1;
        string? army = null;
        rig.Story.OnCall = (file, _) =>
        {
            if (file != "FIGT_B.ADV")
                return;
            party = rig.State.Bt_EnemyParty1.Value * 1000 + rig.State.Bt_EnemyParty2.Value;
            army = rig.State.Strings[10];
        };

        await rig.RunAsync();

        Assert.Equal(["FIGT_B.ADV"], Files(rig));
        Assert.Equal(130 * 1000, party);                        // alone, the officers left behind
        Assert.Equal([3, 5, 2], rig.Random.Draws);              // the draw in two after his battle
        Is(2, rig.State.F_Redeye_State);                        // 1 to 2
        Is(13, rig.State.Tmp0229);
        Assert.Equal("Tｹｲﾌﾞﾘｽ.ADV:833", army);                  // "Redeye Unit"

        // Freak's stage 5, the player lost, the draw not 1: Gﾚｯﾄﾞｱｲ 6; then at state 5 his fourth scene.
        rig = RedeyeAtMaunes(2, 5, 2);
        rig.State.F_Redeye_State = 5;
        rig.State.N_Freak_Stage = 5;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "FIGT_B.ADV")
                rig.State.Bt_Result = 1;
        };
        await rig.RunAsync();
        Assert.Equal([("FIGT_B.ADV", 0), ("Gﾚｯﾄﾞｱｲ.ADV", 6), ("Gﾚｯﾄﾞｱｲ.ADV", 4)], rig.Story.Calls);

        // At state 2 his second scene comes before the battle, which is fought where he stood.
        rig = RedeyeAtMaunes(2, 5, 1);
        rig.State.F_Redeye_State = 2;
        int foughtAt = 0;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Gﾚｯﾄﾞｱｲ.ADV")
                rig.State.Prov_Cur = 1;
            else
                foughtAt = rig.State.Prov_Cur.Value;
        };
        await rig.RunAsync();
        Assert.Equal([("Gﾚｯﾄﾞｱｲ.ADV", 2), ("FIGT_B.ADV", 2)], rig.Story.Calls);
        Assert.Equal(13, foughtAt);
    }

    [Fact]
    public async Task At_state_8_Freak_or_the_ninja_comes_before_Redeye_s_battle_and_a_stale_cursor_decides_it()
    {
        // Freak's stage 3: his scene instead of the battle.
        var rig = RedeyeAtMaunes(2, 5);
        rig.State.F_Redeye_State = 8;
        rig.State.N_Freak_Stage = 3;
        await rig.RunAsync();
        Assert.Equal([("Gﾌﾘｰｸ.ADV", 4)], rig.Story.Calls);

        // QUIRK: NAT-28 — with no ninja the scene leaves the cursor at the troop sum's last slot:
        // record 0 in a unit of three (dead: no battle) …
        rig = RedeyeAtMaunes(2, 5);
        rig.State.F_Redeye_State = 8;
        rig.State.Cmdrs[148].Status = 0;
        await rig.RunAsync();
        Assert.Equal([("Gﾚｯﾄﾞｱｲ.ADV", 8)], rig.Story.Calls);

        // … Redeye himself in a full unit (alive: the battle).
        rig = RedeyeAtMaunes(2, 5, 1);
        rig.State.F_Redeye_State = 8;
        await rig.RunAsync();
        Assert.Equal(["Gﾚｯﾄﾞｱｲ.ADV", "FIGT_B.ADV"], Files(rig));

        // The scene killing him calls the battle off.
        rig = RedeyeAtMaunes(2, 5);
        rig.State.F_Redeye_State = 8;
        rig.Story.OnCall = (file, _) =>
        {
            rig.State.Cmdr_Cur = 130;
            rig.State.Cmdrs[130].Status = 0;
        };
        await rig.RunAsync();
        Assert.Equal(["Gﾚｯﾄﾞｱｲ.ADV"], Files(rig));
    }

    /// <summary>Kite (132) alive, Calory at <paramref name="calory"/>, Zeth Palace (69) Kayblis's with its own picture.</summary>
    private static TurnRig KiteRig(int calory, params int[] draws)
    {
        var rig = new TurnRig(draws);
        rig.State.Cmdrs[132].Status = 1;
        rig.State.F_Calory_State = (Sys3Int)calory;
        rig.State.Provs[69].Owner = 8;
        rig.State.Provs[69].BgCg = 1011;
        return rig;
    }

    [Fact]
    public async Task Kite_s_draws_may_bring_Calory_s_scenes_instead_of_the_march()
    {
        // Below state 4 a draw in ten replaces the draw in three.
        var rig = KiteRig(2, 2, 5, 3, 1);
        await rig.RunAsync();
        Assert.Equal([("Gｶｲﾄ.ADV", 6)], rig.Story.Calls);
        Assert.Equal([3, 5, 3, 10], rig.Random.Draws);

        rig = KiteRig(4, 2, 5, 2);
        await rig.RunAsync();
        Assert.Equal([("Gｶｲﾄ.ADV", 7)], rig.Story.Calls);
        Assert.Equal([3, 5, 3], rig.Random.Draws);

        // Not with Zeth Palace's picture changed: Kite marches, and takes Maginot Stronghold (76).
        rig = KiteRig(4, 2, 5, 1);
        rig.State.Provs[69].BgCg = 1043;
        await rig.RunAsync();
        Assert.Empty(rig.Story.Calls);
        Is(8, rig.State.Provs[76].Owner);
    }

    [Fact]
    public async Task After_Kite_s_battle_Calory_s_scene_follows_her_state()
    {
        var rig = KiteRig(2, 2, 5, 3, 3);
        rig.State.Provs[76].Owner = 1;
        string? army = null;
        rig.Story.OnCall = (file, _) => army ??= rig.State.Strings[10];
        await rig.RunAsync();
        Assert.Equal(["FIGT_B.ADV", "Gｶｲﾄ.ADV"], Files(rig));
        Assert.Equal("Tｹｲﾌﾞﾘｽ.ADV:974", army);                  // "Kite Unit"
        Assert.Equal(3, rig.Story.Calls[1].Entry);              // state 2: her third scene
        Is(10, rig.State.Ai_KiteAtk);

        // State 4: her fifth once Zeth Palace (69), the province fought over, has fallen to Kayblis.
        rig = KiteRig(4, 2, 5, 3);
        foreach (int p in new[] { 76, 74, 70, 66 })
            rig.State.Provs[p].Owner = 8;
        rig.State.Provs[69].Owner = 1;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "FIGT_B.ADV")
                rig.State.Provs[69].Owner = 8;
        };
        await rig.RunAsync();
        Assert.Equal(["FIGT_B.ADV", "Gｶｲﾄ.ADV"], Files(rig));
        Assert.Equal(5, rig.Story.Calls[1].Entry);
    }

    [Fact]
    public async Task Galtia_s_second_scene_follows_a_lost_defence()
    {
        var rig = new TurnRig(2, 5);
        foreach (int c in new[] { 154, 131 })                   // Derandron, Galtia; Nasport (155) dead
        {
            rig.State.Cmdrs[c].Status = 1;
            rig.State.Cmdrs[c].Troops = 300;
            rig.State.Cmdrs[c].MaxTroops = 1000;
        }
        rig.State.Provs[76].Owner = 1;
        int party = -1;
        string? army = null;
        rig.Story.OnCall = (file, _) =>
        {
            if (file != "FIGT_B.ADV")
                return;
            party = rig.State.Bt_EnemyParty1.Value * 1000 + rig.State.Bt_EnemyParty2.Value;
            army = rig.State.Strings[10];
            rig.State.Bt_RoundsLeft = 1;
            rig.State.Bt_Result = 1;
        };

        await rig.RunAsync();

        Assert.Equal([("FIGT_B.ADV", 0), ("Gｶﾞﾙﾃｨｱ.ADV", 2)], rig.Story.Calls);
        Assert.Equal(154 * 1000 + 131, party);                  // Derandron, Galtia
        Assert.Equal("Tｹｲﾌﾞﾘｽ.ADV:1114", army);                 // "Galtia Unit"

        // A line all Kayblis's: the unit stays home.
        rig = new TurnRig(2, 5);
        rig.State.Cmdrs[131].Status = 1;
        foreach (int p in new[] { 76, 74, 65, 63, 66, 61, 60, 59, 58, 16, 15, 13, 2, 1 })
            rig.State.Provs[p].Owner = 8;
        await rig.RunAsync();
        Assert.Empty(rig.Story.Calls);
        Assert.Empty(rig.Lines);
    }

    [Fact]
    public async Task Kesselring_flies_from_the_province_before_his_target_and_attacks_without_a_second_battle()
    {
        var rig = new TurnRig(2, 5);
        rig.State.Cmdrs[133].Status = 1;
        rig.State.F_Kesselring_State = 3;
        rig.State.Prov_StrBase = 2000;
        rig.State.Strings[2000 + 60] = "Tape";
        rig.State.Provs[70].Owner = 8;                          // Papaya's Tower, Tape: Kayblis's
        rig.State.Provs[61].Owner = 8;
        rig.State.Provs[19].Owner = 1;                          // Diphtheria: the player's
        rig.State.Provs[85].Owner = 1;                          // the Red House
        string? from = null, army = null;
        int scene = 0;
        rig.Story.OnCall = (file, _) =>
        {
            if (file != "FIGT_B.ADV")
            {
                scene = rig.State.Prov_Cur.Value;
                return;
            }
            from = rig.State.Strings[8];
            army = rig.State.Strings[10];
            rig.State.Bt_RoundsLeft = 0;                        // held: no second attack for him
            rig.State.Bt_Result = 2;
        };

        await rig.RunAsync();

        Assert.Equal(["FIGT_B.ADV", "Gｹｯｾﾙ.ADV"], Files(rig));
        Assert.Equal(2, rig.Story.Calls[1].Entry);
        Assert.Equal("Tape", from);                             // "［Tape］ Sky Above"
        Is(19, rig.State.Kesselring_LastProv);
        Assert.Equal(85, scene);                                // at the Red House
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Assert.Equal([1302, 1308, 1316, 1321], rig.Lines);
        Assert.Equal([331, 113, 1, 0, 311, 111, 1, 0, 113, 1, 0, 312, 332], rig.Windows);
        Assert.Equal([32, 24], rig.View.Events.OfType<EngineEvent>().Where(e => e.Command == "ZM").Select(e => (int)e.Args[0].Value));
        Assert.Equal("Tｹｲﾌﾞﾘｽ.ADV:1211", army);                 // "Kesselring Unit"

        // From his castle (87) to Papaya's Tower (70), nobody's: taken without a battle, no last province.
        rig = new TurnRig(2, 5);
        rig.State.Cmdrs[133].Status = 1;
        rig.State.F_Kesselring_State = 1;
        rig.State.F_Zeth_War = 2;                               // Papaya's Tower: Zeth fallen already
        await rig.RunAsync();
        Assert.Empty(rig.Story.Calls);
        Is(8, rig.State.Provs[70].Owner);
        Assert.Equal([1302, 1308, 1316, 1321, 1628], rig.Lines);
        Is(0, rig.State.Kesselring_LastProv);
    }

    [Fact]
    public async Task Piar_comes_once_three_generals_are_dead_and_Kayblis_moves_once_Silky_s_Castle_falls()
    {
        var rig = new TurnRig(2, 5);
        rig.State.Cmdrs[131].Status = 1;                        // Galtia alive, defected
        rig.State.F_Galtia_DefectedToKayblis = 1;
        rig.State.F_Piar_State = 0;
        rig.State.Provs[81].Owner = 1;

        await rig.RunAsync();

        // Babolat (129), Redeye (130) and Kite (132) dead: Piar's arc begins; Kayblis moves.
        Assert.Equal([("Gﾊﾟｲｱｰﾙ.ADV", 1), ("Gｹｲﾌﾞﾘｽ.ADV", 3)], rig.Story.Calls);
        Is(1, rig.State.Ev_WeeklyEventDone);

        // Piar's arc under way: her unit marches (step 3).
        rig = new TurnRig(2, 5);
        rig.State.F_Piar_State = 2;
        var e = await Assert.ThrowsAsync<NotSupportedException>(rig.RunAsync);
        Assert.Contains("Tｹｲﾌﾞﾘｽ.ADV:670", e.Message);
    }

    [Fact]
    public async Task After_week_100_the_first_Assault_Leader_marches()
    {
        var rig = new TurnRig(2, 5);
        rig.State.Week = 101;

        var e = await Assert.ThrowsAsync<NotSupportedException>(rig.RunAsync);

        Assert.Contains("Tｹｲﾌﾞﾘｽ.ADV:326", e.Message);
        Is(191, rig.State.Tmp0001);
        Is(87, rig.State.Tmp0002);
        Is(88, rig.State.Tmp0003);
    }

    [Fact]
    public async Task A_capture_turns_the_province_Kayblis_s_with_its_report()
    {
        var rig = new TurnRig();
        rig.State.Prov_StrBase = 2000;
        rig.State.Strings[2000 + 19] = "Lazeal";
        rig.State.Prov_Cur = 20;
        rig.State.Provs[20].Owner = 3;

        await rig.Turn.CaptureAsync();

        Is(8, rig.State.Provs[20].Owner);
        Assert.Equal("Lazeal", rig.State.Strings[8]);
        Assert.Equal([1628], rig.Lines);
        Assert.Equal([113, 1, 0], rig.Windows);
        Is(20, rig.State.Prov_Cur);
    }

    [Fact]
    public async Task Lang_Bau_brings_Helman_down_but_only_Shangri_La_could_change_hands()
    {
        var rig = new TurnRig();
        rig.State.Prov_Cur = 45;
        rig.State.F_Helman_War = 1;
        rig.State.Provs[29].Owner = 4;
        rig.State.Provs[48].Owner = 4;

        await rig.Turn.CaptureAsync();

        Is(2, rig.State.F_Helman_War);
        Is(1, rig.State.F_Helman_FallCause);
        Is(8, rig.State.Provs[45].Owner);
        // QUIRK: NAT-01 — the loop stops after Shangri-La (29); Laurengrad (48) stays Helman's.
        Is(8, rig.State.Provs[29].Owner);
        Is(4, rig.State.Provs[48].Owner);
        Is(29, rig.State.Prov_Cur);
        Assert.Equal([1628, 1721], rig.Lines);

        // QUIRK: NAT-27 — a war never started falls all the same.
        rig = new TurnRig();
        rig.State.Prov_Cur = 45;
        await rig.Turn.CaptureAsync();
        Is(2, rig.State.F_Helman_War);

        rig = new TurnRig();
        rig.State.Prov_Cur = 45;
        rig.State.F_Helman_War = 2;
        await rig.Turn.CaptureAsync();
        Is(0, rig.State.F_Helman_FallCause);
        Assert.Equal([1628], rig.Lines);
    }

    [Fact]
    public async Task A_Zeth_tower_kills_its_minister_and_the_last_one_brings_Zeth_down_with_Saias()
    {
        var rig = new TurnRig();
        rig.State.Prov_Cur = 70;                                // Papaya's Tower, the others alive
        rig.State.Cmdrs[101].Status = 1;
        rig.State.Cmdrs[104].Status = 1;
        rig.State.F_Zeth_State = 5;
        rig.State.F_Zeth_War = 1;

        await rig.Turn.CaptureAsync();

        Is(0, rig.State.Cmdrs[104].Status);
        Is(11, rig.State.F_Zeth_State);
        Is(6, rig.State.F_RagarlRitual_State);
        Is(3, rig.State.F_Papaya_State);
        Is(2, rig.State.F_ZethBarrier_State);
        Is(1, rig.State.F_Zeth_War);
        Is(101, rig.State.Cmdr_Cur);

        rig = new TurnRig();
        rig.State.Prov_Cur = 68;                                // Chizuko's Tower, the last minister
        rig.State.Cmdrs[101].Status = 1;
        rig.State.Cmdrs[113].Status = 1;
        rig.State.F_Zeth_State = 8;
        rig.State.F_Zeth_War = 1;
        rig.State.Provs[58].Owner = 7;
        rig.State.Provs[61].Owner = 7;

        await rig.Turn.CaptureAsync();

        Is(0, rig.State.Cmdrs[101].Status);
        Is(8, rig.State.F_Zeth_State);                          // 7 or more: the story stays
        Is(2, rig.State.F_Zeth_War);
        // QUIRK: NAT-01 — only Adam's Fortress (58) changes hands, and Saias (113) dies with it.
        Is(8, rig.State.Provs[58].Owner);
        Is(7, rig.State.Provs[61].Owner);
        Is(0, rig.State.Cmdrs[113].Status);
        Is(113, rig.State.Cmdr_Cur);
        Assert.Equal([1628, 1752], rig.Lines);
    }

    [Fact]
    public async Task A_Zeth_general_s_city_kills_its_general()
    {
        var rig = new TurnRig();
        rig.State.Prov_Cur = 61;                                // Tape
        rig.State.Cmdrs[117].Status = 2;

        await rig.Turn.CaptureAsync();

        Is(0, rig.State.Cmdrs[117].Status);
        Is(117, rig.State.Cmdr_Cur);
    }

    [Fact]
    public void Tｹｲﾌﾞﾘｽ_is_routed() => Assert.Contains("Tｹｲﾌﾞﾘｽ.ADV", StrategyRules.PortedFiles);
}
