using Kichikuou.Core.Script;
using Kichikuou.Core.Setup;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// The prologue slices of the command files: <c>Cﾃｷﾄｼ</c> (<see cref="EnemyCityCommand"/>),
/// <c>Cﾃｷﾄｼ2</c> (<see cref="DistantCityCommand"/>), <c>Cﾌﾞｶ</c> (<see cref="SubordinatesCommand"/>),
/// <c>Cﾊｰﾚﾑ</c> (<see cref="HaremCommand"/>), the entry of <c>Cﾒｲｷｭｳ</c> with the Thieves' Hideout
/// (<see cref="DungeonCommand"/>) and the router <see cref="StrategyRules"/>. Each file is also
/// compared with the decompile on random states (Content tests, <c>CommandSliceDifferentialTests</c>).
/// </summary>
public class CommandSliceTests
{
    private sealed class SliceRig
    {
        public SliceRig(int stage, params int[] draws)
        {
            Random = new ScriptedRandom(draws);
            Script = new ScriptContext(State, Random, View, Input, Story);
            State.StoryStage = (Sys3Int)stage;
            State.Week = 12;
            for (int s = 0; s < State.Strings.Length; s++)
                State.Strings[s] = $"s{s}";
        }

        public GameState State { get; } = new();
        public ScriptedRandom Random { get; }
        public RecordingView View { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public ScriptedStory Story { get; } = new();
        public ScriptContext Script { get; }

        public List<(string File, int Entry)> Calls => Story.Calls;

        /// <summary>The files called, without the picker calls.</summary>
        public List<string> Scenes => Story.Calls.Select(c => c.File).Where(f => f[0] != 'S').ToList();

        /// <summary>The modes the picker was called with, in order.</summary>
        public List<int> PickerModes { get; } = [];

        public List<int> Lines => View.Events
            .Select(e => e switch { MessageEvent m => m.Text.Line, ValueMessageEvent v => v.Text.Line, _ => 0 })
            .Where(l => l != 0)
            .ToList();

        public List<int> Windows => View.Events.OfType<WindowEvent>().Select(w => (int)w.Op.Value).ToList();

        public List<List<int>> MenuLines => Input.Menus.Select(m => m.Select(i => i.At.Line).ToList()).ToList();

        /// <summary>The picker answers <paramref name="picks"/> in turn to its pick mode (1), then 0.</summary>
        public void Picker(string file, params int[] picks)
        {
            var queue = new Queue<int>(picks);
            var previous = Story.OnCall;
            Story.OnCall = (f, entry) =>
            {
                previous?.Invoke(f, entry);
                if (f != file)
                    return;
                PickerModes.Add(State.CallArg.Value);
                if (file == "Sﾊｰﾚﾑ.ADV" || State.CallArg == 1)
                {
                    int pick = queue.Count > 0 ? queue.Dequeue() : 0;
                    State.CallArg = (Sys3Int)pick;
                    if (file == "Sﾌﾞｶ.ADV" && pick is >= 1 and <= 50)
                        State.Unit_Cur = (Sys3Int)pick;
                }
            };
        }

        /// <summary>Runs <paramref name="effect"/> when <paramref name="file"/> is called.</summary>
        public void On(string file, Action<GameState> effect)
        {
            var previous = Story.OnCall;
            Story.OnCall = (f, entry) =>
            {
                previous?.Invoke(f, entry);
                if (f == file)
                    effect(State);
            };
        }
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, actual.Value);

    // ---------------------------------------------------------------------------------------
    // Cﾃｷﾄｼ
    // ---------------------------------------------------------------------------------------

    private static SliceRig EnemyCity(int stage, int province)
    {
        var rig = new SliceRig(stage);
        rig.State.Prov_Cur = (Sys3Int)province;
        rig.State.Prov_StrBase = 100;
        rig.State.Provs[province].BgCg = 777;
        rig.State.Provs[province].Owner = 2;
        return rig;
    }

    [Fact]
    public async Task Pillage_plays_the_thieves_raid_and_the_opening_when_the_prologue_ends()
    {
        var rig = EnemyCity(stage: 0, province: 34);
        rig.Input.Choices.Enqueue(1);                                   // Pillage
        rig.On("Iﾄｳｿﾞｸ.ADV", s => s.StoryStage = 99);

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal([("Iﾄｳｿﾞｸ.ADV", 1), ("ｵｰﾌﾟﾆﾝｸﾞ.ADV", 1)], rig.Calls);
        Assert.Equal([[70, 81]], rig.MenuLines);                        // Pillage, Return
        Is(34, rig.State.Bt_TargetProv);                                // left set (ENG-05)
        Is(0, rig.State.Ui_CityScouted);
        Assert.Equal("s133", rig.State.Strings[8]);                     // the province's name
        Assert.Equal([20, 4368, 28], rig.Lines);                        // name, (Rebel Army), "??"
        Assert.Equal([331, 113, 0], rig.Windows);
        Assert.Contains(new WindowEvent(331, 777), rig.View.Events);
    }

    [Fact]
    public async Task Pillage_without_the_end_of_the_prologue_returns_at_once()
    {
        var rig = EnemyCity(stage: 0, province: 33);
        rig.Input.Choices.Enqueue(1);

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal([("Iﾄｳｿﾞｸ.ADV", 1)], rig.Calls);
    }

    [Fact]
    public async Task Return_clears_the_target_and_redraws_the_map()
    {
        var rig = EnemyCity(stage: 0, province: 34);
        rig.Input.Choices.Enqueue(2);                                   // Return

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Empty(rig.Calls);
        Is(0, rig.State.Bt_TargetProv);
        Assert.Equal([331, 113, 0, 332], rig.Windows);
        Assert.Equal(new MapEvent(0), rig.View.Events[^1]);
    }

    [Fact]
    public async Task A_cancelled_menu_puts_the_pointer_back_and_returns()
    {
        var rig = EnemyCity(stage: 0, province: 34);
        rig.Input.Choices.Enqueue(0);

        await new EnemyCityCommand(rig.Script).RunAsync();

        var iz = Assert.Single(rig.View.Events.OfType<EngineEvent>());
        Assert.Equal("IZ", iz.Command);
        Is(0, rig.State.Bt_TargetProv);
    }

    [Fact]
    public async Task The_AL_Church_item_is_crossed_out_once_the_own_city_action_is_spent()
    {
        var rig = EnemyCity(stage: 2, province: 77);
        rig.State.N_ALChurch_Stage = 5;
        rig.State.Ui_CmdOwnCityAvail = 0;
        rig.Input.Choices.Enqueue(1);                                   // ×Go to AL Church: the menu again
        rig.Input.Choices.Enqueue(4);                                   // Return

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal([[58, 64, 76, 81], [58, 64, 76, 81]], rig.MenuLines);
        Assert.Equal([20, 4368, 28], rig.Lines);                        // the header once
        Assert.Empty(rig.Calls);

        rig = EnemyCity(stage: 2, province: 77);
        rig.State.N_ALChurch_Stage = 5;
        rig.State.Ui_CmdOwnCityAvail = 1;
        rig.Input.Choices.Enqueue(1);                                   // ○Go to AL Church

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal([("GAL.ADV", 7)], rig.Calls);
        Is(0, rig.State.Ui_CmdOwnCityAvail);
    }

    [Fact]
    public async Task Ninja_Maneuver_comes_back_to_the_header_with_what_was_scouted()
    {
        var rig = EnemyCity(stage: 2, province: 40);
        rig.State.Provs[40].Economy = 3;
        rig.State.Provs[40].Defense = 4;
        rig.State.Provs[40].BattleArea = 500;
        rig.On("Cﾆﾝｼﾞｬ.ADV", s => s.Ui_CityScouted = 1);
        rig.Input.Choices.Enqueue(2);                                   // Ninja Maneuver
        rig.Input.Choices.Enqueue(3);                                   // Return

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal(["Cﾆﾝｼﾞｬ.ADV"], rig.Scenes);
        Assert.Equal([20, 4368, 28, 20, 4368, 34], rig.Lines);
        Assert.Contains(new ValueMessageEvent(new ScriptRef("Cﾃｷﾄｼ.ADV", 34), [(Sys3Int)3, (Sys3Int)4, (Sys3Int)500]), rig.View.Events,
            new ValueMessageComparer());
    }

    /// <summary>A rebel province at stage 1 with the week's attack left; the picker sends <paramref name="party"/>.</summary>
    private static SliceRig Attack(int province, params int[] party)
    {
        var rig = EnemyCity(stage: 1, province: province);
        rig.State.Ui_CmdAttackAvail = 1;
        rig.State.F_LeazasRebels_State = 2;
        rig.State.Gold = 500;
        rig.Input.Choices.Enqueue(1);                                   // Attack
        rig.On("Sﾌﾞﾀｲ.ADV", s =>
        {
            s.Bt_PlayerParty1 = (Sys3Int)(party.Length > 0 ? party[0] : 0);
            s.Bt_PlayerParty2 = (Sys3Int)(party.Length > 1 ? party[1] : 0);
            s.Bt_PlayerParty3 = (Sys3Int)(party.Length > 2 ? party[2] : 0);
            s.Bt_PlayerParty4 = (Sys3Int)(party.Length > 3 ? party[3] : 0);
            foreach (int u in party)
                s.Units[u].Status = 2;
            if (party.Length > 0)
                s.Gold = s.Gold - (Sys3Int)30;                          // the war expenses
        });
        return rig;
    }

    [Fact]
    public async Task An_attack_on_the_rebels_picks_the_party_lets_the_rebels_defend_and_fights()
    {
        var rig = Attack(5, 1, 6);
        int pickerSize = 0, pickerMode = 0, battleKind = 0;
        rig.On("Sﾌﾞﾀｲ.ADV", s => (pickerSize, pickerMode) = (s.Tmp0021.Value, s.Tmp0022.Value));
        rig.On("TCﾘｰｻﾞｽ2.ADV", s => s.Bt_EnemyParty1 = 3);
        rig.On("FIGT_A.ADV", s => battleKind = s.Bt_Kind.Value);

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal(["Sﾌﾞﾀｲ.ADV", "TCﾘｰｻﾞｽ2.ADV", "FIGT_A.ADV"], rig.Calls.Select(c => c.File));
        Assert.Equal((4, 1), (pickerSize, pickerMode));                  // four units, attack mode
        Is(1, (Sys3Int)battleKind);
        Is(0, rig.State.Bt_Kind);
        Is(5, rig.State.Bt_TargetProv);                                 // FIGT_A clears it, not Cﾃｷﾄｼ
        Is(0, rig.State.Bt_WallBonus);
        Assert.Equal([20, 4368, 28, 139, 162, 165, 311], rig.Lines);   // header, briefing, "advanced to"
        Assert.Equal(new MapEvent(0), rig.View.Events[^1]);
        Is(470, rig.State.Gold);
    }

    [Fact]
    public async Task A_city_nobody_defends_is_taken_without_a_battle()
    {
        var rig = Attack(4, 1);
        rig.State.Provs[4].MapX = 300;
        rig.State.Provs[4].MapY = 200;
        rig.On("TCﾘｰｻﾞｽ2.ADV", s => s.Bt_EnemyParty1 = 0);

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.DoesNotContain(rig.Calls, c => c.File == "FIGT_A.ADV");
        Is(1, rig.State.Provs[4].Owner);
        Is(0, rig.State.Ui_CmdAttackAvail);
        Is(300, rig.State.MapTargetX);
        Is(200, rig.State.MapTargetY);
        Assert.Equal([new MapEvent(0), new MapEvent(4), new MapEvent(3), new MapEvent(0)], rig.View.Events.OfType<MapEvent>());
        Assert.Equal(399, rig.Lines[^1]);                               // "occupied … with no difficulties"
    }

    [Theory]
    [InlineData(1, false, 1, 3868)]     // stage 1, not the rebels: the civil war comes first
    [InlineData(4, true, 1, 4064)]      // stage 4: chaos
    [InlineData(1, true, 0, 4014)]      // the week's conquest is made …
    [InlineData(1, false, 0, 4014)]     // … which is tested before the civil war
    public async Task Maris_refuses_an_attack_and_the_city_is_left(int stage, bool rebels, int attackLeft, int line)
    {
        var rig = Attack(5);
        rig.State.StoryStage = (Sys3Int)stage;
        rig.State.Ui_CmdAttackAvail = (Sys3Int)attackLeft;
        rig.State.Provs[5].Owner = (Sys3Int)(rebels ? 2 : 4);

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Empty(rig.Calls);
        Assert.Contains(line, rig.Lines);
        Is(0, rig.State.Bt_TargetProv);
        Assert.Equal([0, 332], rig.Windows.TakeLast(2));                // L_00205
        Assert.Equal(new MapEvent(0), rig.View.Events[^1]);
    }

    [Fact]
    public async Task The_rebels_first_week_has_the_army_disrupted()
    {
        var rig = Attack(3, 1);
        rig.State.F_LeazasRebels_State = 1;

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Empty(rig.Calls);
        Assert.Equal([20, 4368, 28, 139, 3834, 3841, 3846, 3853, 3858], rig.Lines);
        Is(0, rig.State.Bt_TargetProv);
    }

    [Fact]
    public async Task A_picker_left_without_a_party_comes_back_to_the_city_menu()
    {
        var rig = Attack(3);                                            // the picker sends nobody
        rig.Input.Choices.Enqueue(3);                                   // Return

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal(["Sﾌﾞﾀｲ.ADV"], rig.Calls.Select(c => c.File));
        Assert.Equal([[64, 76, 81], [64, 76, 81]], rig.MenuLines);
        Assert.Equal([20, 4368, 28, 139, 162, 165, 20, 4368, 28], rig.Lines);   // the header again, not the picture
        Assert.Equal([new WindowEvent(331, 777), new WindowEvent(331, 777)], rig.View.Events.OfType<WindowEvent>().Where(w => w.Op == 331));
    }

    [Fact]
    public async Task Satella_refuses_to_fight_humans_and_the_party_stays_paid()
    {
        var rig = Attack(3, 1, 29);
        rig.Input.Choices.Enqueue(3);                                   // Return

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal(["Sﾌﾞﾀｲ.ADV"], rig.Calls.Select(c => c.File));
        Assert.Contains(272, rig.Lines);
        Assert.Contains(281, rig.Lines);                                // "Satella refused to fight."
        Is(1, rig.State.Units[1].Status);                               // back at status 1 …
        Is(1, rig.State.Units[29].Status);
        Is(470, rig.State.Gold);                                        // … the expenses not refunded (BFL-01)
        Assert.Contains(new WindowEvent(311, 75), rig.View.Events);
    }

    [Fact]
    public async Task At_56_while_at_war_with_JAPAN_Satella_and_Meglass_go_along()
    {
        var rig = Attack(56, 29, 30);
        rig.State.StoryStage = 2;
        rig.State.Provs[56].Owner = 6;
        rig.State.F_Japan_State = 1;
        rig.On("TJAPAN2.ADV", s => s.Bt_EnemyParty1 = 75);

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal(["Sﾌﾞﾀｲ.ADV", "TJAPAN2.ADV", "FIGT_A.ADV"], rig.Calls.Select(c => c.File));
    }

    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 4)]
    public async Task A_mountain_pass_lets_one_unit_through_unless_a_neighbour_is_held(bool neighbourHeld, int size)
    {
        var rig = Attack(32);
        rig.State.StoryStage = 2;
        rig.State.Provs[32].Owner = 4;
        rig.State.Provs[34].Owner = (Sys3Int)(neighbourHeld ? 1 : 4);
        rig.State.Tmp0030 = 7;
        int pickerSize = 0;
        rig.On("Sﾌﾞﾀｲ.ADV", s => pickerSize = s.Tmp0021.Value);
        rig.Input.Choices.Enqueue(3);                                   // Return after the cancelled picker

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal(size, pickerSize);
        Assert.Equal(!neighbourHeld, rig.Lines.Contains(205));          // "only one unit can attack"
        Is(32, rig.State.Prov_Cur);
        Is(7, rig.State.Tmp0030);                                       // the scratch saved around the speech
    }

    [Fact]
    public async Task Patton_defends_with_his_own_four_and_Aristoles_once_he_sided_with_him()
    {
        var rig = Attack(50, 1);
        rig.State.StoryStage = 2;
        rig.State.Provs[50].Owner = 5;
        rig.State.F_Aristoles_Helman2ndState = 10;
        rig.State.Bt_EnemyRetreatMode = 4;
        foreach (int c in new[] { 54, 56, 57, 32 })
            rig.State.Cmdrs[c].Status = 1;
        Sys3Int[] party = [];
        rig.On("FIGT_A.ADV", s => party = [s.Bt_EnemyParty1, s.Bt_EnemyParty2, s.Bt_EnemyParty3, s.Bt_EnemyParty4]);

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal([54, 56, 57, 32], party.Select(c => c.Value));     // 55 is dead
        Is(1, rig.State.Bt_EnemyRetreatMode);
    }

    [Fact]
    public async Task The_JAPAN_remnant_is_a_single_fleeing_soldier_named_from_the_players_files()
    {
        var rig = Attack(60, 1);
        rig.State.StoryStage = 2;
        rig.State.Provs[60].Owner = 6;
        rig.State.F_Japan_State = 10;
        rig.State.Cmdr_StrBase = 400;
        rig.State.Cmdrs[73].MaxHP = 9;
        rig.State.Cmdrs[73].MaxTroops = 300;
        var scenario = new LiteralSource(new Dictionary<int, (int Slot, string Text)>
        {
            [324] = (10, "Fleeing Soldier"),
            [334] = (4, "Fleeing Soldier"),
            [336] = (5, "Foot Soldiers"),
        });

        await new EnemyCityCommand(rig.Script, scenario).RunAsync();

        Assert.Equal(["Sﾌﾞﾀｲ.ADV", "FIGT_A.ADV"], rig.Calls.Select(c => c.File));
        Is(73, rig.State.Bt_EnemyParty1);
        Is(1, rig.State.Cmdrs[73].Status);
        Is(9, rig.State.Cmdrs[73].HP);
        Is(300, rig.State.Cmdrs[73].Troops);
        Assert.Equal("Fleeing Soldier", rig.State.Strings[10]);
        Assert.Equal("Fleeing Soldier", rig.State.Strings[400 + 72 * 3]);
        Assert.Equal("Foot Soldiers", rig.State.Strings[400 + 72 * 3 + 1]);
    }

    [Theory]
    [InlineData(0, 4156)]           // the first time: the talk about the barrier at the castle
    [InlineData(1, 4138)]           // afterwards: the short report
    public async Task Zeths_barrier_sends_the_army_back_without_a_refund(int barrier, int line)
    {
        var rig = Attack(66, 1);
        rig.State.StoryStage = 2;
        rig.State.Provs[66].Owner = 7;
        rig.State.F_ZethBarrier_State = (Sys3Int)barrier;
        rig.State.F_Papaya_State = 2;
        rig.On("Tｾﾞｽ2.ADV", s => s.Bt_PlayerParty1 = 0);

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal(["Sﾌﾞﾀｲ.ADV", "Tｾﾞｽ2.ADV"], rig.Calls.Select(c => c.File));
        Assert.Contains(line, rig.Lines);
        Is(1, rig.State.F_ZethBarrier_State);
        Is(1, rig.State.F_Papaya_State);
        Is(2, rig.State.Units[1].Status);                               // still deployed (BFL-01)
        Is(1, rig.State.Ui_CmdAttackAvail);
    }

    [Fact]
    public async Task An_independent_city_is_not_ported_yet()
    {
        var rig = Attack(20, 1);
        rig.State.StoryStage = 2;
        rig.State.Provs[20].Owner = 3;

        await Assert.ThrowsAsync<NotSupportedException>(() => new EnemyCityCommand(rig.Script).RunAsync());
    }

    /// <summary>The <c>MS n, "text"</c> statements a test hands a file at given lines of <c>Cﾃｷﾄｼ.ADV</c>.</summary>
    private sealed class LiteralSource(Dictionary<int, (int Slot, string Text)> literals) : IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException($"No file {file}.");

        public ScriptStatement ReadStatement(ScriptRef at) =>
            literals.TryGetValue(at.Line, out var l)
                ? new SetStringStatement(at, l.Slot, l.Text)
                : throw new InvalidOperationException($"No literal at {at}.");
    }

    [Fact]
    public async Task A_forced_trip_takes_over_both_city_commands()
    {
        var rig = EnemyCity(stage: 2, province: 40);
        rig.State.Ev_CommandMode = 1;
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Equal([("Gｶﾌｪ.ADV", 31)], rig.Calls);
        Assert.Empty(rig.Input.Menus);

        rig = EnemyCity(stage: 2, province: 40);
        rig.State.Ev_CommandMode = 2;
        await new DistantCityCommand(rig.Script).RunAsync();
        Assert.Equal([("Gﾏﾘｱ.ADV", 5)], rig.Calls);
        Assert.Empty(rig.Input.Menus);
    }

    // ---------------------------------------------------------------------------------------
    // Cﾃｷﾄｼ2
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task A_distant_province_in_the_prologue_offers_only_Return()
    {
        var rig = EnemyCity(stage: 0, province: 12);
        rig.State.Provs[12].Owner = 11;
        rig.Input.Choices.Enqueue(1);

        await new DistantCityCommand(rig.Script).RunAsync();

        Assert.Equal([[55]], rig.MenuLines);
        Assert.Equal([18, 107, 26], rig.Lines);                         // name, (ALism), "??"
        Assert.Equal([331, 113, 0, 332], rig.Windows);
        Assert.Empty(rig.Calls);
        Assert.DoesNotContain(rig.View.Events, e => e is MapEvent);
    }

    [Fact]
    public async Task A_distant_province_later_offers_the_ninja()
    {
        var rig = EnemyCity(stage: 1, province: 12);
        rig.Input.Choices.Enqueue(1);                                   // Ninja Maneuver
        rig.Input.Choices.Enqueue(0);                                   // cancel

        await new DistantCityCommand(rig.Script).RunAsync();

        Assert.Equal([[50, 55], [50, 55]], rig.MenuLines);
        Assert.Equal(["Cﾆﾝｼﾞｬ.ADV"], rig.Scenes);
        Assert.Single(rig.View.Events.OfType<EngineEvent>(), e => e.Command == "IZ");
    }

    // ---------------------------------------------------------------------------------------
    // Cﾌﾞｶ
    // ---------------------------------------------------------------------------------------

    private static SliceRig Subordinates(int stage)
    {
        var rig = new SliceRig(stage);
        rig.State.Ui_CmdSubordAvail = 1;
        rig.State.Unit_StrBase = 50;
        for (int u = 1; u <= 4; u++)
            rig.State.Units[u].Status = 1;
        return rig;
    }

    [Fact]
    public async Task A_prologue_talk_spends_the_command_and_plays_the_units_scene_with_entry_20()
    {
        var rig = Subordinates(stage: 0);
        rig.State.Ev_Entry = 3;
        rig.Picker("Sﾌﾞｶ.ADV", 3);

        await new SubordinatesCommand(rig.Script).RunAsync();

        Assert.Equal([2, 1, 3], rig.PickerModes);
        Assert.Equal([("Gﾊﾞｳﾝﾄﾞ.ADV", 20)], rig.Calls.Where(c => c.File[0] == 'G'));
        Is(0, rig.State.Ui_CmdSubordAvail);
        Is(3, rig.State.Unit_Cur);
        Is(2, rig.State.Tmp0001);
        Assert.Equal(["s56", "s57", "s58"], rig.State.Strings[1..4]);
        Assert.Empty(rig.Input.Menus);
    }

    [Theory]
    [InlineData(0, new[] { 1344, 1348 })]
    [InlineData(9, new[] { 1344, 1357, 1366 })]
    [InlineData(19, new[] { 1344, 1374 })]
    public async Task Rance_talks_to_himself_by_how_often_he_has(int before, int[] lines)
    {
        var rig = Subordinates(stage: 0);
        rig.State.N_RanceSelfTalk = (Sys3Int)before;
        rig.Picker("Sﾌﾞｶ.ADV", 1);

        await new SubordinatesCommand(rig.Script).RunAsync();

        Is(before + 1, rig.State.N_RanceSelfTalk);
        Assert.Equal(lines, rig.Lines);
        Assert.Empty(rig.Scenes);
        Assert.Contains(new WindowEvent(311, 1), rig.View.Events);
    }

    [Fact]
    public async Task A_unit_that_cannot_act_shows_its_status_page_and_the_pick_goes_on()
    {
        var rig = Subordinates(stage: 0);
        rig.State.Units[2].Status = 2;
        rig.State.Units[2].MaxTroops = 0;
        rig.State.Units[2].Level = 4;
        rig.Picker("Sﾌﾞｶ.ADV", 2);                                     // then a cancel

        await new SubordinatesCommand(rig.Script).RunAsync();

        Assert.Equal([2, 1, 5, 1], rig.PickerModes);
        Assert.Equal([1116, 1149], rig.Lines);
        var first = rig.View.Events.OfType<ValueMessageEvent>().First();
        Is(100 + 4 * 30, first.Values[^1]);                             // the experience needed
        Is(1, rig.State.Ui_CmdSubordAvail);                             // nothing spent
        Assert.Empty(rig.Scenes);
    }

    [Fact]
    public async Task The_view_only_loop_shows_pages_until_the_picker_is_cancelled()
    {
        var rig = Subordinates(stage: 0);
        rig.State.Ui_CmdSubordAvail = 0;
        rig.State.Units[3].MaxTroops = 100;
        rig.State.Units[3].OffAtkMod = 12;                              // −2
        rig.State.Units[3].OffDefMod = 3;                               // +3
        rig.State.Units[3].DefAtkMod = 10;                              // nothing
        rig.State.Units[3].DefDefMod = 10;
        rig.State.Units[3].DgnAtkMod = 10;
        rig.State.Units[3].DgnDefMod = 10;
        rig.Picker("Sﾌﾞｶ.ADV", 3);

        await new SubordinatesCommand(rig.Script).RunAsync();

        Assert.Equal([2, 1, 5, 1, 3], rig.PickerModes);
        Assert.Equal([1116, 1149, 1169, 1179, 1183, 1186, 1195, 1209, 1221, 1235, 1247, 1254], rig.Lines);
        Assert.Empty(rig.Scenes);
    }

    [Fact]
    public async Task A_right_click_closes_the_status_page()
    {
        var rig = Subordinates(stage: 0);
        rig.State.Ui_CmdSubordAvail = 0;
        rig.State.Ui_WinType = 43;                                      // the window is open, so op 5 waits
        rig.Input.Keys.Enqueue(32);
        rig.Picker("Sﾌﾞｶ.ADV", 3);

        await new SubordinatesCommand(rig.Script).RunAsync();

        Assert.Equal([1116], rig.Lines);
        Assert.Equal([113, 5, 0], rig.Windows);
    }

    [Fact]
    public async Task The_troop_menu_beyond_the_prologue_is_not_ported_yet()
    {
        var rig = Subordinates(stage: 1);
        rig.Picker("Sﾌﾞｶ.ADV", 2);

        await Assert.ThrowsAsync<NotSupportedException>(() => new SubordinatesCommand(rig.Script).RunAsync());
    }

    // ---------------------------------------------------------------------------------------
    // Cﾊｰﾚﾑ
    // ---------------------------------------------------------------------------------------

    private static SliceRig Harem()
    {
        var rig = new SliceRig(stage: 0);
        rig.State.Ui_CmdHaremAvail = 1;
        rig.State.CurrentBgm = 7;
        return rig;
    }

    [Fact]
    public async Task The_prologue_harem_offers_Sill_Soul_and_Anasel_once_captured()
    {
        var rig = Harem();
        rig.State.HaremAvail_Lia = 1;                                   // cleared by NC
        rig.Picker("Sﾊｰﾚﾑ.ADV", 0);
        rig.On("Sﾊｰﾚﾑ.ADV", s =>
        {
            Is(1, s.HaremAvail_Sill);
            Is(1, s.HaremAvail_Soul);
            Is(0, s.HaremAvail_Anasel);
            Is(0, s.HaremAvail_Lia);
            Is(0, s.Ui_CmdHaremAvail);                                  // paid before the pick
        });

        await new HaremCommand(rig.Script).RunAsync();

        Is(1, rig.State.Ui_CmdHaremAvail);                              // refunded on the cancel
        Assert.Equal([("Sﾊｰﾚﾑ.ADV", 10)], rig.Calls);

        rig = Harem();
        rig.State.F_Thieves_State = 2;
        rig.Picker("Sﾊｰﾚﾑ.ADV", 0);
        rig.On("Sﾊｰﾚﾑ.ADV", s => Is(1, s.HaremAvail_Anasel));
        await new HaremCommand(rig.Script).RunAsync();
    }

    [Fact]
    public async Task A_visit_with_a_reward_gives_Rance_experience_and_counts_it_for_the_girl()
    {
        var rig = Harem();
        rig.State.Units[1].Exp = 40;
        rig.Picker("Sﾊｰﾚﾑ.ADV", 11);                                   // Soul
        rig.On("Gｿｳﾙ.ADV", s => s.Harem_ExpReward = 3);

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([("Sﾊｰﾚﾑ.ADV", 10), ("Gｿｳﾙ.ADV", 10)], rig.Calls);
        Is(43, rig.State.Units[1].Exp);
        Is(1, rig.State.History[11].Harem_SexCount);
        Is(12, rig.State.History[11].Harem_LastWeek);
        Is(11, rig.State.Hist_Cur);                                     // QUIRK: CMD-14
        Is(11, rig.State.Harem_LastPick);
        Is(1, rig.State.Unit_Cur);
        Assert.Equal([566], rig.Lines);
        Assert.Contains(new MusicEvent(7), rig.View.Events);
        Is(0, rig.State.Ui_CmdHaremAvail);
    }

    [Fact]
    public async Task A_visit_without_a_reward_ends_after_the_map_and_the_music()
    {
        var rig = Harem();
        rig.Picker("Sﾊｰﾚﾑ.ADV", 1);                                    // Sill

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([("Sﾊｰﾚﾑ.ADV", 10), ("Gｼｨﾙ.ADV", 10)], rig.Calls);
        Is(0, rig.State.History[1].Harem_SexCount);
        Is(0, rig.State.Hist_Cur);
        Assert.Empty(rig.Lines);
        Assert.IsType<MusicEvent>(rig.View.Events[^1]);
    }

    [Fact]
    public async Task A_cancel_lowers_Lias_counter_even_when_the_visit_was_capped()
    {
        var rig = Harem();
        rig.State.F_Lia_State = 1;
        rig.State.N_Lia_Neglect = 10;
        rig.Picker("Sﾊｰﾚﾑ.ADV", 0);

        await new HaremCommand(rig.Script).RunAsync();

        Is(9, rig.State.N_Lia_Neglect);                                 // QUIRK: CMD-13
        Is(1, rig.State.Ui_CmdHaremAvail);
    }

    [Fact]
    public async Task Cow_Sill_leaves_without_a_reward_and_Sill_state_5_takes_over_the_command()
    {
        var rig = Harem();
        rig.State.F_SillCow_State = 7;
        rig.Picker("Sﾊｰﾚﾑ.ADV", 25);
        rig.On("Gｼｨﾙ.ADV", s => s.Harem_ExpReward = 5);

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([("Sﾊｰﾚﾑ.ADV", 10), ("Gｼｨﾙ.ADV", 7)], rig.Calls);
        Assert.DoesNotContain(rig.View.Events, e => e is MapEvent);

        rig = Harem();
        rig.State.F_SillCow_State = 5;
        await new HaremCommand(rig.Script).RunAsync();
        Assert.Equal([("Gｼｨﾙ.ADV", 5)], rig.Calls);
        Is(1, rig.State.Ui_CmdHaremAvail);
    }

    [Fact]
    public async Task The_harem_beyond_the_prologue_is_not_ported_yet()
    {
        var rig = Harem();
        rig.State.StoryStage = 1;

        await Assert.ThrowsAsync<NotSupportedException>(() => new HaremCommand(rig.Script).RunAsync());
        Assert.Empty(rig.Random.Draws);
    }

    // ---------------------------------------------------------------------------------------
    // Cﾒｲｷｭｳ
    // ---------------------------------------------------------------------------------------

    /// <summary>The <c>MS</c> lines of <c>Cﾒｲｷｭｳ.ADV</c>, as "line n" in the slot the line writes.</summary>
    private sealed class NameSource : IScenarioSource
    {
        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) =>
            new SetStringStatement(at, at.Line switch { 27 => 10, 70 => 9, _ => 8 }, $"line {at.Line}");
    }

    private static SliceRig Dungeon(int stage, int dungeon, params int[] draws)
    {
        var rig = new SliceRig(stage, draws);
        rig.State.Dgn_Cur = (Sys3Int)dungeon;
        rig.State.Dgns[dungeon].MapX = 300;
        rig.State.Dgns[dungeon].MapY = 250;
        rig.State.Ui_CmdDungeonAvail = 1;
        rig.State.Units[4].Status = 1;
        return rig;
    }

    [Theory]
    [InlineData(1, new[] { 2795, 2801, 2811, 2818 })]
    [InlineData(2, new[] { 2795, 2801, 2827, 2830, 2839 })]
    [InlineData(3, new[] { 2795, 2801, 2852, 2857, 2864 })]
    public async Task The_hideout_in_the_prologue_plays_one_of_three_banters(int roll, int[] lines)
    {
        var rig = Dungeon(stage: 0, dungeon: 1, roll);

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Assert.Equal([3], rig.Random.Draws);
        Is(roll, rig.State.Tmp0690);
        Assert.Equal(lines, rig.Lines);
        Is(1, rig.State.Ui_CmdDungeonAvail);                            // the hideout gives the action back
        Assert.Equal("line 31", rig.State.Strings[8]);
        Assert.Equal("line 27", rig.State.Strings[10]);
        Is(1008, rig.State.DungeonBgCg);
        Is(300, rig.State.MapCamX);
        Is(250, rig.State.MapCamY);
        Assert.Equal([new MapEvent(1), new MapEvent(10)], rig.View.Events.OfType<MapEvent>());
        Assert.Empty(rig.Calls);
    }

    [Fact]
    public async Task Another_dungeon_in_the_prologue_spends_the_action_and_returns()
    {
        var rig = Dungeon(stage: 0, dungeon: 15);

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Is(0, rig.State.Ui_CmdDungeonAvail);                            // QUIRK: DGN-14
        Assert.Equal("line 96", rig.State.Strings[8]);
        Is(1032, rig.State.DungeonBgCg);
        Assert.Empty(rig.Lines);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task A_leftover_commander_cursor_on_Gunagan_marks_him_beaten()
    {
        var rig = Dungeon(stage: 0, dungeon: 2);
        rig.State.Cmdr_Cur = 100;
        rig.State.Cmdrs[100].CgFull = 1137;

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Is(2, rig.State.F_Gunagan_State);                               // QUIRK: DGN-01
    }

    [Fact]
    public async Task The_hideout_after_the_prologue_is_only_looked_at()
    {
        var rig = Dungeon(stage: 2, dungeon: 1);

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Assert.Equal([2763, 2771, 2776, 2779], rig.Lines);
        Assert.Empty(rig.Random.Draws);
        Assert.Equal(2, rig.View.Events.OfType<EngineEvent>().Count(e => e.Command == "ZM"));
    }

    [Fact]
    public async Task Bound_takes_over_the_hideout_when_Souls_story_reaches_it()
    {
        var rig = Dungeon(stage: 2, dungeon: 1);
        rig.State.N_Soul_Stage = 5;

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Assert.Equal([("Gﾊﾞｳﾝﾄﾞ.ADV", 1)], rig.Calls);
        Assert.Empty(rig.Lines);
    }

    [Fact]
    public async Task A_dungeon_beyond_the_prologue_is_not_ported_yet()
    {
        var rig = Dungeon(stage: 2, dungeon: 4);

        await Assert.ThrowsAsync<NotSupportedException>(() => new DungeonCommand(rig.Script, new NameSource()).RunAsync());
    }

    // ---------------------------------------------------------------------------------------
    // The router
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task The_router_runs_the_ported_files_and_hands_on_the_rest()
    {
        var state = new GameState();
        var unported = new ScriptedStory();
        var rules = new StrategyRules(new NameSource(), unported);
        var input = new ScriptedSceneInput();
        var script = new ScriptContext(state, new ScriptedRandom(2), new RecordingView(), input, rules);
        rules.Bind(script);
        state.Dgn_Cur = 1;
        state.Units[4].Status = 1;

        await script.CallFileAsync("Cﾒｲｷｭｳ.ADV");                       // ported: the hideout's banter
        await script.EventAsync("Gｼｨﾙ.ADV", 10);                       // not here: handed on

        Is(2, state.Tmp0690);
        Assert.Equal([("Gｼｨﾙ.ADV", 10)], unported.Calls);
        Assert.Contains("Cﾊｰﾚﾑ.ADV", StrategyRules.PortedFiles);
        Assert.Throws<InvalidOperationException>(() => rules.Bind(new ScriptContext(state, new ScriptedRandom(), new RecordingView(), input, rules)));
    }

    /// <summary>Compares value messages by their lists' contents.</summary>
    private sealed class ValueMessageComparer : IEqualityComparer<ScriptEvent>
    {
        public bool Equals(ScriptEvent? x, ScriptEvent? y) =>
            x is ValueMessageEvent a && y is ValueMessageEvent b && a.Text == b.Text && a.Values.SequenceEqual(b.Values);

        public int GetHashCode(ScriptEvent obj) => 0;
    }
}
