using Kichikuou.Original.Tests.Combat;
using Kichikuou.Original.Tests.Script;
using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;
using Kichikuou.Original.Strategy;

namespace Kichikuou.Original.Tests.Strategy;

/// <summary>
/// The prologue slices of the command files: <c>Cﾃｷﾄｼ</c> (<see cref="EnemyCityCommand"/>),
/// <c>Cﾃｷﾄｼ2</c> (<see cref="DistantCityCommand"/>), <c>Cﾌﾞｶ</c> (<see cref="SubordinatesCommand"/>),
/// <c>Cﾊｰﾚﾑ</c> (<see cref="HaremCommand"/>), <c>Cｼｮｳﾆﾝ</c> (<see cref="MerchantCommand"/>), <c>Cｼﾊｲﾄｼ</c> (<see cref="OwnCityCommand"/>),
/// <c>Cﾀｶﾗ</c> (<see cref="TreasureCommand"/>), <c>各種DATA</c> (<see cref="AllDataCommand"/>), <c>Cﾒｲｷｭｳ</c> — the entry, the Thieves' Hideout
/// and the search (<see cref="DungeonCommand"/>) — and the router <see cref="StrategyRules"/>. Each file is also
/// compared with the decompile on random states (Content tests, <c>CommandSliceDifferentialTests</c>).
/// </summary>
public class CommandSliceTests
{
    private sealed class SliceRig
    {
        public SliceRig(int stage, params int[] draws)
        {
            Random = new ScriptedRandom(draws);
            Script = new OriginalScriptContext(State, Random, View, Input, Story);
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
        public OriginalScriptContext Script { get; }

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

    /// <summary>An attack on independent city <paramref name="province"/> at stage 2, then the cities' menu answers <paramref name="choices"/>.</summary>
    private static SliceRig IndependentCity(int province, int[] party, params int[] choices)
    {
        var rig = Attack(province, party);
        rig.State.StoryStage = 2;
        rig.State.Provs[province].Owner = 3;
        foreach (int choice in choices)
            rig.Input.Choices.Enqueue(choice);
        return rig;
    }

    [Fact]
    public async Task Use_Force_on_an_independent_city_fights_its_heroes_without_refusals()
    {
        var rig = IndependentCity(20, [29], 2);                         // Satella, who refuses elsewhere; Use Force
        rig.State.Cmdrs[65].Status = 1;
        rig.State.DXTalk_RetryWeek = 9;
        int kind = -1;
        rig.On("FIGT_A.ADV", s => kind = s.Bt_Kind.Value);
        var scenario = new LiteralSource(new Dictionary<int, (int Slot, string Text)> { [3710] = (10, "Lazeal Army") });

        await new EnemyCityCommand(rig.Script, scenario).RunAsync();

        Assert.Equal(["Sﾌﾞﾀｲ.ADV", "FIGT_A.ADV"], rig.Calls.Select(c => c.File));
        Assert.Equal([493, 498, 502], rig.MenuLines[^1]);               // Negotiate, Use Force, Stop
        Assert.Equal([20, 448, 476, 483, 3652], rig.Lines.Where(l => l is 20 or 448 or 476 or 483 or 3652));
        Assert.Equal("Lazeal Army", rig.State.Strings[10]);
        Assert.Equal([65, 0, 0, 0], [rig.State.Bt_EnemyParty1.Value, rig.State.Bt_EnemyParty2.Value, rig.State.Bt_EnemyParty3.Value, rig.State.Bt_EnemyParty4.Value]);
        Is(1, rig.State.Bt_EnemyRetreatMode);
        Is(0, rig.State.DXTalk_RetryWeek);
        Assert.Equal(1, kind);
        Is(0, rig.State.Bt_Kind);
    }

    [Fact]
    public async Task Taking_Portugal_by_force_while_Pluepet_waits_plays_Rebeccas_scene_and_Mars_his()
    {
        var rig = IndependentCity(28, [1], 2);
        rig.State.F_Merchant_State = 2;
        rig.On("FIGT_A.ADV", s => s.Provs[28].Owner = 1);
        var scenario = new LiteralSource(new Dictionary<int, (int Slot, string Text)> { [3789] = (10, "Portugal Army") });

        await new EnemyCityCommand(rig.Script, scenario).RunAsync();

        Assert.Equal([("Sﾌﾞﾀｲ.ADV", 0), ("FIGT_A.ADV", 0), ("Gﾚﾍﾞｯｶ.ADV", 2)], rig.Calls);
        Is(3, rig.State.F_Merchant_State);

        rig = IndependentCity(22, [1], 2);
        rig.On("FIGT_A.ADV", s => s.Provs[22].Owner = 1);
        scenario = new LiteralSource(new Dictionary<int, (int Slot, string Text)> { [3724] = (10, "Mars Army") });
        await new EnemyCityCommand(rig.Script, scenario).RunAsync();
        Assert.Equal(("Gｶｾｲ.ADV", 1), rig.Calls[^1]);
    }

    [Fact]
    public async Task Stop_goes_back_to_the_citys_menu_and_Rockearth_refuses_talks_after_the_DX_president_died()
    {
        var rig = IndependentCity(17, [1], 1, 3, 0);                    // Negotiate, Stop, then leave
        rig.State.F_DX_State = 100;

        await new EnemyCityCommand(rig.Script).RunAsync();

        Assert.Equal([532, 543, 510, 515, 520], rig.Lines.Where(l => l is 532 or 543 or 510 or 515 or 520));
        Assert.Single(rig.Lines, 20);                                   // the menu again, without the header
        Assert.Empty(rig.Calls);
    }

    /// <summary>Whether the city became Leazas's, the week's attack was spent and its heroes are gone.</summary>
    private static void Taken(SliceRig rig, int province, params int[] heroes)
    {
        Is(1, rig.State.Provs[province].Owner);
        Is(0, rig.State.Ui_CmdAttackAvail);
        foreach (int c in heroes)
        {
            Is(0, rig.State.Cmdrs[c].Status);
            Is(0, rig.State.Cmdrs[c].Troops);
        }
        Assert.Contains(4, rig.View.Events.OfType<MapEvent>().Select(m => (int)m.Mode.Value));   // the owner's flag
    }

    [Fact]
    public async Task Ice_and_Red_give_in_and_Diphtheria_and_Zeo_refuse()
    {
        var rig = IndependentCity(18, [1], 1);                          // Negotiate
        rig.State.Cmdrs[66].Status = 1;
        rig.State.Cmdrs[66].Troops = 900;
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Equal([595, 1434], rig.Lines.Where(l => l is 595 or 1434));
        Taken(rig, 18, 66, 67);

        rig = IndependentCity(23, [1], 1, 1);                           // Negotiate, I promise
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Equal([2085, 2089], rig.MenuLines[^1]);
        Taken(rig, 23, 63, 64);

        rig = IndependentCity(23, [1], 1, 2, 3);                        // I can't promise: the header, Return
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Contains(2149, rig.Lines);
        Assert.Equal(2, rig.Lines.Count(l => l == 20));
        Is(3, rig.State.Provs[23].Owner);

        rig = IndependentCity(19, [1], 1, 3, 3);                        // Negotiate: back to the menu; Stop, Return
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Contains(953, rig.Lines);
        Assert.Equal(2, rig.MenuLines.Count(m => m.SequenceEqual([493, 498, 502])));
        Is(3, rig.State.Provs[19].Owner);

        rig = IndependentCity(24, [1], 1);                              // the file is left
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Contains(2804, rig.Lines);
        Is(3, rig.State.Provs[24].Owner);
        Is(1, rig.State.Ui_CmdAttackAvail);
    }

    [Fact]
    public async Task Rockearth_pays_500_when_approved_and_never_talks_again_after_die()
    {
        var rig = IndependentCity(17, [1], 1, 1);                       // Negotiate, I'll approve you
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(1, rig.State.F_DX_State);
        Is(1000, rig.State.Gold);
        Taken(rig, 17, 68, 69);

        rig = IndependentCity(17, [1], 1, 2, 3);                        // Disgusting, die: the header, Return
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(100, rig.State.F_DX_State);
        Is(500, rig.State.Gold);
        Is(3, rig.State.Provs[17].Owner);
        Assert.Contains(1317, rig.Lines);
    }

    [Fact]
    public async Task Lazeal_waits_four_weeks_after_Ill_wait_and_gives_in_after_them()
    {
        var rig = IndependentCity(20, [1], 1, 2);                       // Negotiate, I'll wait
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(16, rig.State.DXTalk_RetryWeek);                             // week 12 + 4
        Is(3, rig.State.Provs[20].Owner);
        Is(1, rig.State.Ui_CmdAttackAvail);

        rig = IndependentCity(20, [1], 1);
        rig.State.DXTalk_RetryWeek = 12;                                // not before this week: wait on
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Contains(1849, rig.Lines);
        Assert.Empty(rig.Calls);
        Is(3, rig.State.Provs[20].Owner);

        rig = IndependentCity(20, [1], 1);
        rig.State.DXTalk_RetryWeek = 11;
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Equal([("Gﾚｨﾘｨ.ADV", 1)], rig.Calls);
        Taken(rig, 20, 65);
    }

    [Fact]
    public async Task Mars_joins_as_a_general_and_M_Land_is_funded()
    {
        var rig = IndependentCity(22, [1], 1, 2);                       // Negotiate, Make a General
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(1, rig.State.Units[26].Status);
        Is(2, rig.State.N_Mars_Score);
        Assert.Empty(rig.Calls);                                        // no Gｶｾｲ 1, unlike Use Force
        Taken(rig, 22, 71);

        rig = IndependentCity(22, [1], 1, 1, 3);                        // Reject: the header, Return
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(0, rig.State.Units[26].Status);
        Is(3, rig.State.Provs[22].Owner);

        rig = IndependentCity(25, [1], 1, 2);                           // Maintain the Park
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(1, rig.State.F_MLand_Funded);
        Is(1, rig.State.F_Sayori_State);
        Taken(rig, 25, 11, 12);

        rig = IndependentCity(25, [1], 1, 1);                           // No: the file is left
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(0, rig.State.F_MLand_Funded);
        Is(3, rig.State.Provs[25].Owner);
    }

    [Fact]
    public async Task Portugal_is_taken_for_free_without_paying_and_refuses_89_gold()
    {
        var rig = IndependentCity(28, [1], 1, 2, 1);                    // Pay and Conquer (refused), Don't Pay
        rig.State.Gold = 89;
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Equal([2375, 2332], rig.Lines.Where(l => l is 2375 or 2332));
        Assert.Equal(2, rig.MenuLines.Count(m => m.SequenceEqual([2319, 2323])));
        Is(89, rig.State.Gold);                                         // CMD-03
        Taken(rig, 28, 17);

        rig = IndependentCity(28, [1], 1, 2);                           // Pay and Conquer
        rig.State.Gold = 90;
        rig.State.Hist_Cur = 3;
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(0, rig.State.Gold);
        Is(90, rig.State.History[3].SundryExp);                         // CMD-14
        Taken(rig, 28, 17);
    }

    [Fact]
    public async Task Hanna_takes_29_gold_for_30_and_Gengorou_has_his_say_while_alive()
    {
        var rig = IndependentCity(26, [1], 1, 1);                       // Negotiate, Pay
        rig.State.Gold = 29;
        rig.State.Hist_Cur = 2;
        rig.State.Cmdrs[8].Status = 1;
        await new EnemyCityCommand(rig.Script).RunAsync();
        Is(0, rig.State.Gold);                                          // CMD-04
        Is(30, rig.State.History[2].SundryExp);
        Is(1, rig.State.F_Shinoda_State);
        Assert.Contains(3544, rig.Lines);
        Taken(rig, 26, 8, 9, 10);

        rig = IndependentCity(26, [1], 1, 1, 2);                        // Pay (refused at 28), Refuse
        rig.State.Gold = 28;
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Equal([3518, 3611], rig.Lines.Where(l => l is 3518 or 3611));
        Is(28, rig.State.Gold);
        Is(0, rig.State.F_Shinoda_State);
        Is(3, rig.State.Provs[26].Owner);
        Is(1, rig.State.Ui_CmdAttackAvail);
    }

    [Fact]
    public async Task Pluepet_visits_Portugal_once_and_Custom_and_Toushin_City_have_their_own_scenes()
    {
        var rig = IndependentCity(28, [1], 0);
        rig.State.F_Merchant_State = 1;
        rig.State.Gold = 50;

        await new EnemyCityCommand(rig.Script).RunAsync();

        Is(2, rig.State.F_Merchant_State);
        Is(150, rig.State.Gold);
        Assert.Contains(2442, rig.Lines);
        Assert.Equal(2, rig.Lines.Count(l => l == 20));                // the picture and the header again

        rig = IndependentCity(21, [1]);
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Equal([("Iｼﾚｲﾌﾞ.ADV", 3)], rig.Calls);

        rig = IndependentCity(27, [1]);
        await new EnemyCityCommand(rig.Script).RunAsync();
        Assert.Equal([("Sﾌﾞﾀｲ.ADV", 0), ("Gｻｰﾅｷｱ.ADV", 1)], rig.Calls);
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
    public async Task Later_a_unit_opens_its_troop_menu_with_the_offer_of_its_class()
    {
        var rig = TroopMenu(unit: 2);
        rig.State.Units[2].Class = 0;
        rig.State.Units[2].TroopAtk = 10;                               // 110 for 25 men
        rig.State.Week = 21;                                            // + 21/4
        rig.Input.Choices.Enqueue(5);                                   // Do Nothing

        await new SubordinatesCommand(rig.Script).RunAsync();

        Assert.Equal([[807, 812, 817, 836, 847]], rig.MenuLines.Take(1));   // Info, Replenish, Add, Talk, Nothing
        Assert.Equal([2, 1, 3, 4, 1], rig.PickerModes);                // open, pick, close, redraw, pick (none)
        Assert.Contains(new ValueMessageEvent(new ScriptRef("Cﾌﾞｶ.ADV", 744), [(Sys3Int)30, (Sys3Int)40, (Sys3Int)100, (Sys3Int)400]),
            rig.View.Events, new ValueMessageComparer());
        Assert.Contains(new WindowEvent(311, 900), rig.View.Events);   // the unit's face, Maris on the right
        Assert.Contains(new WindowEvent(321, 6), rig.View.Events);
        Is(1, rig.State.CmdAvail_SubTroops);
        Is(1, rig.State.Ui_CmdSubordAvail);
        Assert.Single(rig.View.Events.OfType<EngineEvent>(), e => e.Command == "IZ");
    }

    [Fact]
    public async Task Add_New_Troops_pays_and_raises_the_units_troops_and_maximum()
    {
        var rig = TroopMenu(unit: 3);                                   // Bound: 30 for 50 men
        rig.State.Week = 8;                                             // + 2
        rig.State.Gold = 100;
        rig.State.Hist_Cur = 0;
        rig.Input.Choices.Enqueue(3);                                   // Add New Troops
        rig.Input.Choices.Enqueue(1);                                   // Do It

        await new SubordinatesCommand(rig.Script).RunAsync();

        Is(68, rig.State.Gold);
        Is(32, rig.State.History[0].MilitaryExp);
        Is(150, rig.State.Units[3].Troops);
        Is(450, rig.State.Units[3].MaxTroops);
        Is(1, rig.State.Units[3].Recovery);
        Is(0, rig.State.CmdAvail_SubTroops);
        Is(1, rig.State.Ui_CmdSubordAvail);                             // the talk is left
        Is(0, rig.State.Tmp0001);
        Assert.Equal([870, 883, 945], rig.Lines.Where(l => l is 870 or 883 or 945));
        Assert.Equal(new MapEvent(0), rig.View.Events.OfType<MapEvent>().Last());
    }

    [Fact]
    public async Task Add_More_doubles_the_men_for_three_times_the_price_and_a_cancel_keeps_it()
    {
        var rig = TroopMenu(unit: 3);
        rig.State.Week = 8;                                             // 32 for 50 men
        rig.State.Gold = 1000;
        rig.Input.Choices.Enqueue(3);                                   // Add New Troops
        rig.Input.Choices.Enqueue(2);                                   // Add More: 96 for 100
        rig.Input.Choices.Enqueue(0);                                   // cancel: the menu again …
        rig.Input.Choices.Enqueue(3);                                   // … the offer as it was
        rig.Input.Choices.Enqueue(1);                                   // Do It

        await new SubordinatesCommand(rig.Script).RunAsync();

        // QUIRK: CMD-12
        Assert.Equal([(50, 32), (100, 96), (100, 96)],
            rig.View.Events.OfType<ValueMessageEvent>().Where(v => v.Text.Line == 870).Select(v => (v.Values[0].Value, v.Values[1].Value)));
        Is(904, rig.State.Gold);
        Is(200, rig.State.Units[3].Troops);
    }

    [Theory]
    [InlineData(400, 400, 0, 0)]        // full: refused, the menu again, Do Nothing
    [InlineData(100, 400, 6, 6)]        // 300 short: 6
    [InlineData(380, 400, 1, 1)]        // 20 short: at least 1
    public async Task Replenish_fills_a_short_unit_for_one_per_fifty_men(int troops, int max, int price, int paid)
    {
        var rig = TroopMenu(unit: 2);
        rig.State.Units[2].Troops = (Sys3Int)troops;
        rig.State.Units[2].MaxTroops = (Sys3Int)max;
        rig.State.Gold = 50;
        rig.Input.Choices.Enqueue(2);                                   // Replenish Troops
        rig.Input.Choices.Enqueue(troops == max ? 5 : 1);               // Do Nothing / Do

        await new SubordinatesCommand(rig.Script).RunAsync();

        Is(50 - paid, rig.State.Gold);
        Is(max, rig.State.Units[2].Troops);
        Is(troops == max ? 1 : 0, rig.State.CmdAvail_SubTroops);
        if (troops == max)
            Assert.Contains(979, rig.Lines);                            // "already has n troops"
        else
            Assert.Contains(new ValueMessageEvent(new ScriptRef("Cﾌﾞｶ.ADV", 1012), [(Sys3Int)(max - troops), (Sys3Int)price]),
                rig.View.Events, new ValueMessageComparer());
    }

    [Fact]
    public async Task Talk_spends_the_talk_and_plays_the_units_scene()
    {
        var rig = TroopMenu(unit: 3);
        rig.State.CmdAvail_SubTroops = 0;
        rig.Input.Choices.Enqueue(4);                                   // Talk (after ×Replenish, ×Add)

        await new SubordinatesCommand(rig.Script).RunAsync();

        Assert.Equal([[807, 824, 829, 836, 847]], rig.MenuLines);
        Assert.Equal([("Gﾊﾞｳﾝﾄﾞ.ADV", 20)], rig.Calls.Where(c => c.File[0] == 'G'));
        Is(0, rig.State.CmdAvail_SubTalk);
        Is(0, rig.State.Ui_CmdSubordAvail);                             // both spent
        Assert.Contains(1104, rig.Lines);
    }

    /// <summary>A stage-1 Subordinates command whose picker answers <paramref name="unit"/> once.</summary>
    private static SliceRig TroopMenu(int unit)
    {
        var rig = Subordinates(stage: 1);
        rig.State.CmdAvail_SubTroops = 1;
        rig.State.CmdAvail_SubTalk = 1;
        rig.State.Units[unit].FaceCg = 900;
        rig.State.Units[unit].HP = 30;
        rig.State.Units[unit].MaxHP = 40;
        rig.State.Units[unit].Troops = 100;
        rig.State.Units[unit].MaxTroops = 400;
        rig.Picker("Sﾌﾞｶ.ADV", unit);
        return rig;
    }

    // ---------------------------------------------------------------------------------------
    // Cﾊｰﾚﾑ
    // ---------------------------------------------------------------------------------------

    private static SliceRig Harem(int stage = 0, params int[] draws)
    {
        var rig = new SliceRig(stage, draws);
        rig.State.Ui_CmdHaremAvail = 1;
        rig.State.CurrentBgm = 7;
        return rig;
    }

    /// <summary>The girls on offer when the picker is called, by their <c>HaremAvail_*</c> index (1-50).</summary>
    private static List<int> Offered(GameState s) =>
        Enumerable.Range(1, 50).Where(k => s.Memory.Var(248 + k) != 0).ToList();

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
    public async Task After_the_prologue_a_visit_draws_first_even_when_cancelled_and_offers_Lia_and_Maris()
    {
        var rig = Harem(stage: 1, draws: 3);
        rig.State.F_Thieves_State = 3;                                  // the prologue's Anasel rule is gone
        rig.Picker("Sﾊｰﾚﾑ.ADV", 0);
        List<int> offered = [];
        rig.On("Sﾊｰﾚﾑ.ADV", s => offered = Offered(s));

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([4], rig.Random.Draws);                            // QUIRK: STR-14
        Assert.Equal([2, 3], offered);                                  // Lia, Maris
        Assert.Equal([("Sﾊｰﾚﾑ.ADV", 10)], rig.Calls);
        Is(1, rig.State.Ui_CmdHaremAvail);
        Is(27, rig.State.Unit_Cur);                                     // the list's last cursor (ENG-05)
    }

    [Fact]
    public async Task The_draw_plays_Suzumes_first_meeting_and_the_visit_goes_on_with_her()
    {
        var rig = Harem(stage: 1, draws: 1);
        rig.Picker("Sﾊｰﾚﾑ.ADV", 4);
        List<int> offered = [];
        rig.On("Sﾊｰﾚﾑ.ADV", s => offered = Offered(s));

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([("Gｽｽﾞﾒ.ADV", 2), ("Sﾊｰﾚﾑ.ADV", 10), ("Gｽｽﾞﾒ.ADV", 10)], rig.Calls);
        Is(1, rig.State.F_Suzume_Met);
        Assert.Equal([2, 3, 4], offered);
        Is(4, rig.State.Harem_LastPick);
        Is(12, rig.State.History[4].Harem_LastWeek);
    }

    [Fact]
    public async Task Each_intro_test_reads_RetVal_as_the_scene_before_it_left_it()
    {
        // Wendy's first meeting, then her bedroom in the same visit (state 4, RetVal still 2).
        var rig = Harem(stage: 2, draws: 2);
        rig.State.F_Wendy_State = 4;
        rig.Picker("Sﾊｰﾚﾑ.ADV", 0);

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([("Gｳｪﾝﾃﾞｨ.ADV", 2), ("Gｳｪﾝﾃﾞｨ.ADV", 4), ("Sﾊｰﾚﾑ.ADV", 10)], rig.Calls);
        Is(1, rig.State.F_Wendy_Met);

        // A scene leaving RetVal 2 after Suzume's would bring on Wendy's as well (none of the
        // intro scenes writes RetVal, so in play the draw decides alone).
        rig = Harem(stage: 1, draws: 1);
        rig.On("Gｽｽﾞﾒ.ADV", s => s.RetVal = 2);
        rig.Picker("Sﾊｰﾚﾑ.ADV", 0);

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([("Gｽｽﾞﾒ.ADV", 2), ("Gｳｪﾝﾃﾞｨ.ADV", 2), ("Sﾊｰﾚﾑ.ADV", 10)], rig.Calls);
    }

    [Fact]
    public async Task The_cafes_scene_takes_the_visit_before_Lias_counter()
    {
        var rig = Harem(stage: 1, draws: 4);
        rig.State.F_Cafe_State = 11;
        rig.State.LadyCaravan_Prov = 5;
        rig.State.F_Lia_State = 1;
        rig.State.Ui_CmdHaremAvail = 2;

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([("Gｶﾌｪ.ADV", 3)], rig.Calls);
        Is(0, rig.State.Ui_CmdHaremAvail);
        Is(0, rig.State.N_Lia_Neglect);

        rig = Harem(stage: 1, draws: 4);
        rig.State.F_Cafe_State = 10;
        rig.State.LadyCaravan_Prov = 4;                                 // the caravan not past 4 yet
        rig.Picker("Sﾊｰﾚﾑ.ADV", 0);
        await new HaremCommand(rig.Script).RunAsync();
        Assert.Equal([("Sﾊｰﾚﾑ.ADV", 10)], rig.Calls);
    }

    [Fact]
    public async Task The_list_reads_the_units_present_and_the_story_flags()
    {
        var rig = Harem(stage: 3, draws: 4);
        var s = rig.State;
        s.F_Sill_State = 1;                                             // Sill by her flag, unit 2 gone
        s.Units[4].Status = 1;                                          // Soul by her unit
        s.Units[5].Status = 0;
        s.F_Leila_State = 2;                                            // Leila's second set only
        s.Units[43].Status = 0;
        s.F_Helman_FallCause = 2;                                       // Sheila while unit 43 is gone
        s.N_Amiran_Stage = 90;                                          // excluded
        s.F_Mokomoko_State = 12;
        s.Units[27].Status = 2;
        s.N_Isoroku_Stage = 1;                                          // Isoroku with her unit
        s.F_Saitou_State = 2;
        rig.Picker("Sﾊｰﾚﾑ.ADV", 0);
        List<int> offered = [];
        rig.On("Sﾊｰﾚﾑ.ADV", st => offered = Offered(st));

        await new HaremCommand(rig.Script).RunAsync();

        Assert.Equal([1, 2, 3, 11, 13, 19, 35, 44, 50], offered);
    }

    // ---------------------------------------------------------------------------------------
    // Cｼｮｳﾆﾝ
    // ---------------------------------------------------------------------------------------

    private static SliceRig Merchant(int gold = 100, int state = 4)
    {
        var rig = new SliceRig(stage: 1);
        rig.State.Ui_CmdMerchantAvail = 1;
        rig.State.Gold = (Sys3Int)gold;
        rig.State.F_Merchant_State = (Sys3Int)state;
        return rig;
    }

    [Fact]
    public async Task The_first_summons_meets_Pluepet_and_only_Send_Away_spends_the_merchant()
    {
        var rig = Merchant(state: 0);
        rig.Input.Choices.Enqueue(0);                                   // a cancel shows the menu again
        rig.Input.Choices.Enqueue(6);                                   // Send Away

        await new MerchantCommand(rig.Script).RunAsync();

        Is(1, rig.State.F_Merchant_State);
        Assert.Equal([[95, 100, 106, 113, 120, 125], [95, 100, 106, 113, 120, 125]], rig.MenuLines);
        Is(0, rig.State.Ui_CmdMerchantAvail);
        Assert.Equal(1407, rig.Lines[0]);
        Assert.Equal([134, 141, 148], rig.Lines[^3..]);
        Assert.Contains(rig.View.Events, e => e is MapEvent);
        Is(50, rig.State.Unit_Cur);                                     // the menu's last cursor (ENG-05)
    }

    [Fact]
    public async Task Sills_scenes_leave_without_spending_the_merchant()
    {
        var rig = Merchant(state: 0);
        rig.State.F_SillCow_State = 6;

        await new MerchantCommand(rig.Script).RunAsync();

        Assert.Equal([("Gｼｨﾙ.ADV", 6)], rig.Calls);
        Is(1, rig.State.Ui_CmdMerchantAvail);                           // QUIRK: CMD-08
        Is(0, rig.State.F_Merchant_State);
    }

    [Fact]
    public async Task A_mercenary_is_hired_with_one_gold_under_the_price_and_booked_to_the_stale_history_slot()
    {
        var rig = Merchant(gold: 39);
        rig.State.Hist_Cur = 7;
        rig.Input.Choices.Enqueue(2);                                   // Hire Mercenary A
        rig.Input.Choices.Enqueue(1);                                   // 4000K Contract
        rig.Input.Choices.Enqueue(3);                                   // Send Away

        await new MerchantCommand(rig.Script).RunAsync();

        Assert.Equal([[95, 113, 120, 125], [266, 270, 274], [95, 120, 125]], rig.MenuLines);
        Is(0, rig.State.Gold);                                          // QUIRK: CMD-06
        Is(40, rig.State.History[7].SundryExp);                         // QUIRK: CMD-14
        Is(1, rig.State.Units[49].Status);
        Is(200, rig.State.Units[49].Troops);
        Is(200, rig.State.Units[49].MaxTroops);
        Is(1, rig.State.F_Louis_HiredBefore);
        Assert.Contains(381, rig.Lines);                                // the first hire's scene

        rig = Merchant(gold: 38);
        rig.Input.Choices.Enqueue(2);
        rig.Input.Choices.Enqueue(1);
        rig.Input.Choices.Enqueue(4);
        await new MerchantCommand(rig.Script).RunAsync();
        Is(38, rig.State.Gold);
        Is(0, rig.State.Units[49].Status);
        Assert.Contains(176, rig.Lines);                                // "you don't have enough money"
    }

    [Fact]
    public async Task The_shop_sells_each_item_once_and_the_clothes_are_free_at_0()
    {
        var rig = Merchant(gold: 0);
        rig.State.Units[2].Status = 1;
        rig.Input.Choices.Enqueue(1);                                   // Buy Item
        rig.Input.Choices.Enqueue(5);                                   // Clothes
        rig.Input.Choices.Enqueue(1);                                   // Buy
        rig.Input.Choices.Enqueue(1);                                   // Popularity Staff
        rig.Input.Choices.Enqueue(5);                                   // Don't Buy (the list)
        rig.Input.Choices.Enqueue(4);                                   // Send Away

        await new MerchantCommand(rig.Script).RunAsync();

        Assert.Equal(
            [[95, 113, 120, 125], [1088, 1095, 1102, 1116, 1124, 1144], [1183, 1187], [1088, 1095, 1102, 1116, 1144],
             [1088, 1095, 1102, 1116, 1144], [95, 113, 120, 125]],
            rig.MenuLines);
        Is(100, rig.State.Items[19].Holder);                            // QUIRK: CMD-06 — 0 < 1 - 1 is false
        Is(1, rig.State.F_SillClothes_State);
        Is(0, rig.State.Gold);
        Is(0, rig.State.Items[3].Holder);                               // the staff refused
        Assert.Contains(1299, rig.Lines);
        Assert.Contains(1158, rig.Lines);                               // "Not enough money."
        Is(28, rig.State.Item_Cur);                                     // the list's last cursor
    }

    [Fact]
    public async Task Rebecca_is_offered_once_a_visit_while_the_merchant_has_her()
    {
        var rig = Merchant(state: 1);
        rig.Input.Choices.Enqueue(2);                                   // Fuck Rebecca
        rig.Input.Choices.Enqueue(5);                                   // Send Away

        await new MerchantCommand(rig.Script).RunAsync();

        Assert.Equal([("Gﾚﾍﾞｯｶ.ADV", 1)], rig.Calls);
        Assert.Equal([[95, 100, 106, 113, 120, 125], [95, 106, 113, 120, 125]], rig.MenuLines);
    }

    // ---------------------------------------------------------------------------------------
    // Cｼﾊｲﾄｼ
    // ---------------------------------------------------------------------------------------

    private static SliceRig OwnCity(int province, params int[] draws)
    {
        var rig = new SliceRig(1, draws);
        rig.State.Prov_Cur = (Sys3Int)province;
        rig.State.Provs[province].Economy = 5;
        rig.State.Provs[province].Defense = 3;
        rig.State.Ui_CmdOwnCityAvail = 1;
        rig.State.Gold = 100;
        return rig;
    }

    [Fact]
    public async Task A_levy_collects_its_draw_and_plays_the_citys_first_trigger()
    {
        var rig = OwnCity(4, 2, 37);
        rig.Input.Choices.Enqueue(1);                                   // Temporary Levy

        await new OwnCityCommand(rig.Script).RunAsync();

        Assert.Equal([[41, 47, 502]], rig.MenuLines);
        Assert.Equal([3, 50], rig.Random.Draws);                        // the flavour, Economy × 10
        Is(137, rig.State.Gold);                                        // QUIRK: CMD-07
        Is(37, rig.State.VAR0055);
        Is(0, rig.State.Ui_CmdOwnCityAvail);
        Is(11, rig.State.F_Mokomoko_State);
        Assert.Equal([("Gﾓｺﾓｺ.ADV", 1)], rig.Calls);
        Assert.Contains(865, rig.Lines);                                // the second flavour
    }

    [Fact]
    public async Task Without_a_trigger_the_levy_draws_every_roll_and_takes_a_girl()
    {
        var rig = OwnCity(3, 1, 10, 1, 2, 2, 4, 7, 1);
        rig.State.Items[24].Holder = 100;                               // the Cat Bread's 1 finds nothing
        rig.State.Provs[3].Girls = 2;
        rig.Input.Choices.Enqueue(1);

        await new OwnCityCommand(rig.Script).RunAsync();

        Assert.Equal([3, 50, 5, 5, 5, 5, 10, 3], rig.Random.Draws);     // QUIRK: STR-14
        Is(1, rig.State.Provs[3].Girls);
        Is(1, rig.State.N_SpecialRoom_Girls);
        Assert.Contains(1078, rig.Lines);                               // "cute"
        Assert.Contains(1113, rig.Lines);                               // "dancer"
        Assert.Contains(1150, rig.Lines);                               // the soldier's aside
        Assert.Empty(rig.Calls);
        Is(25, rig.State.Item_Cur);
    }

    [Fact]
    public async Task Bolster_Defense_pays_without_a_gold_check_and_returns_to_the_city()
    {
        var rig = OwnCity(3);
        rig.State.Gold = 10;
        rig.State.Ui_CmdOwnCityAvail = 2;
        rig.Input.Choices.Enqueue(2);                                   // Bolster Defense
        rig.Input.Choices.Enqueue(1);                                   // Reinforce
        rig.Input.Choices.Enqueue(3);                                   // Return

        await new OwnCityCommand(rig.Script).RunAsync();

        Assert.Equal([[41, 47, 502], [1583, 1587], [41, 47, 502]], rig.MenuLines);
        Assert.Equal(60, rig.View.Events.OfType<ValueMessageEvent>().First(v => v.Text.Line == 1532).Values[0].Value);
        Is(0, rig.State.Gold);                                          // QUIRK: CMD-05
        Is(4, rig.State.Provs[3].Defense);
        Is(1, rig.State.Ui_CmdOwnCityAvail);
    }

    [Fact]
    public async Task A_spent_week_shows_crosses_but_the_Ruins_Search_follows_the_dungeon_flag()
    {
        var rig = OwnCity(35);
        rig.State.Ui_CmdOwnCityAvail = 0;
        rig.State.Ui_CmdDungeonAvail = 1;
        rig.State.Provs[35].MapX = 123;
        rig.State.Dgns[30].State = 5;
        rig.Input.Choices.Enqueue(1);                                   // ×Temporary Levy: the menu again
        rig.Input.Choices.Enqueue(3);                                   // ○Ruins Search

        await new OwnCityCommand(rig.Script).RunAsync();

        Assert.Equal([[65, 71, 436, 502], [65, 71, 436, 502]], rig.MenuLines);
        Is(30, rig.State.Dgn_Cur);                                      // QUIRK: CMD-09
        Is(0, rig.State.Dgns[30].State);
        Is(35, rig.State.Dgns[30].Prov);
        Is(123, rig.State.Dgns[30].MapX);
        Assert.Equal(["Cﾒｲｷｭｳ.ADV"], rig.Scenes);
        Is(16, rig.State.Unit_Cur);                                     // the place list's cursor
    }

    // ---------------------------------------------------------------------------------------
    // Cﾆﾝｼﾞｬ
    // ---------------------------------------------------------------------------------------

    private static SliceRig Ninja(int owner, params int[] draws)
    {
        var rig = new SliceRig(1, draws);
        rig.State.Prov_Cur = 5;
        rig.State.Provs[5].Owner = (Sys3Int)owner;
        rig.State.Provs[5].Economy = 4;
        rig.State.Provs[5].Defense = 3;
        rig.State.Provs[5].BattleArea = 600;
        rig.State.Cmdr_StrBase = 100;
        return rig;
    }

    [Fact]
    public async Task Every_ninja_busy_leaves_with_Maris_regrets()
    {
        var rig = Ninja(2);
        rig.State.F_Kanami_NinjaUsed = 1;
        rig.State.F_Freya_NinjaUsed = 99;                               // not recruited
        rig.State.F_Kaoru_NinjaUsed = 1;
        rig.State.F_Wichita_NinjaUsed = 99;
        rig.State.F_Saitou_NinjaUsed = 1;

        await new NinjaCommand(rig.Script).RunAsync();

        Assert.Empty(rig.MenuLines);
        Assert.Equal([6, 16, 25, 30, 35], rig.Lines);
    }

    [Fact]
    public async Task A_picked_ninja_is_spent_even_without_a_mission()
    {
        var rig = Ninja(2, 2, 3);                                       // no interrupt, no burger
        rig.State.F_Freya_NinjaUsed = 99;
        rig.State.F_Wichita_NinjaUsed = 1;
        rig.Input.Choices.Enqueue(1);                                   // Kanami
        rig.Input.Choices.Enqueue(3);                                   // No Mission

        await new NinjaCommand(rig.Script).RunAsync();

        Assert.Equal([[51, 63, 75, 80], [283, 288, 299]], rig.MenuLines);
        Assert.Equal([5, 5], rig.Random.Draws);                         // QUIRK: STR-14 — the burger's draw
        Is(1, rig.State.F_Kanami_NinjaUsed);                            // QUIRK: CMD-10
        Is(1, rig.State.Bt_ScoutNinja);
        Assert.Equal(322, rig.Lines[^1]);                               // "Boring girl."
        Assert.Empty(rig.Calls);
    }

    [Fact]
    public async Task Kanami_may_interrupt_and_is_given_back_or_offers_the_burger()
    {
        var rig = Ninja(2, 1);
        rig.Input.Choices.Enqueue(1);

        await new NinjaCommand(rig.Script).RunAsync();

        Assert.Equal([("Gｶﾅﾐ.ADV", 12)], rig.Calls);
        Is(0, rig.State.F_Kanami_NinjaUsed);                            // QUIRK: CMD-10 — given back
        Is(0, rig.State.Bt_ScoutNinja);
        Assert.Single(rig.MenuLines);

        rig = Ninja(2, 1, 1);
        rig.State.F_Kanami_Scene4 = 1;                                  // seen: no interrupt
        rig.Input.Choices.Enqueue(1);
        rig.Input.Choices.Enqueue(3);                                   // Get Me A Burger
        await new NinjaCommand(rig.Script).RunAsync();
        Assert.Equal([283, 288, 294, 299], rig.MenuLines[1]);
        Assert.Equal([("Gｶﾅﾐ.ADV", 11)], rig.Calls);
    }

    [Fact]
    public async Task Collect_Info_shows_the_citys_figures_and_no_sabotage_before_the_war()
    {
        var rig = Ninja(4, 5, 50);                                      // Helman, not at war
        rig.Input.Choices.Enqueue(3);                                   // Kaoru
        rig.Input.Choices.Enqueue(1);                                   // Collect Info

        await new NinjaCommand(rig.Script).RunAsync();

        Assert.Equal([[51, 57, 63, 69, 75, 80], [283, 299]], rig.MenuLines);
        Assert.Equal([5, 100], rig.Random.Draws);
        Is(1, rig.State.Ui_CityScouted);
        Assert.Contains(393, rig.Lines);                                // "Kaoru came back with information."
        var info = rig.View.Events.OfType<ValueMessageEvent>().Single(v => v.Text.Line == 419);
        Assert.Equal([4, 3, 600], info.Values.Select(v => v.Value));
    }

    [Fact]
    public async Task A_caught_ninja_is_lost_unless_a_Substitute_Ninja_takes_her_place()
    {
        var rig = Ninja(4, 5, 1);
        rig.State.N_Kaoru_State = 2;
        rig.Input.Choices.Enqueue(3);
        rig.Input.Choices.Enqueue(1);

        await new NinjaCommand(rig.Script).RunAsync();

        Is(0, rig.State.N_Kaoru_State);
        Assert.Equal([604, 625], rig.Lines[^2..]);                      // "never returned", "Kaoru was captured."
        Assert.Contains(rig.View.Events, e => e is MapEvent);
        Is(0, rig.State.Ui_CityScouted);

        rig = Ninja(4, 5, 1);
        rig.State.N_Kaoru_State = 2;
        rig.State.Items[8].Holder = 100;
        rig.Input.Choices.Enqueue(3);
        rig.Input.Choices.Enqueue(1);
        await new NinjaCommand(rig.Script).RunAsync();
        Is(0, rig.State.Items[8].Holder);
        Is(2, rig.State.N_Kaoru_State);
        Assert.Contains(586, rig.Lines);
        Assert.Contains(419, rig.Lines);                                // she brings the figures all the same
    }

    [Theory]
    [InlineData(1, 0, 2, 656)]                                          // success: the walls lose one
    [InlineData(2, 1, 3, 703)]                                          // failure
    [InlineData(3, 1, 2, 656)]                                          // QUIRK: ENG-02 — a 3 keeps the stale Tmp0001
    [InlineData(3, 2, 3, 703)]
    public async Task Kanamis_sabotage_lowers_the_citys_walls(int roll, int staleTmp, int defense, int line)
    {
        var rig = Ninja(2, 2, 3, 90, roll);                             // no interrupt, no burger, not caught at 90
        rig.State.Tmp0001 = (Sys3Int)staleTmp;
        rig.Input.Choices.Enqueue(1);                                   // Kanami
        rig.Input.Choices.Enqueue(2);                                   // Sabotage

        await new NinjaCommand(rig.Script).RunAsync();

        Assert.Equal([5, 5, 100, 2], rig.Random.Draws);
        Is(defense, rig.State.Provs[5].Defense);
        Assert.Contains(line, rig.Lines);
        Assert.Contains(rig.View.Events, e => e is MapEvent);
    }

    [Fact]
    public async Task Against_owner_8_the_capture_is_drawn_again_of_300()
    {
        var rig = Ninja(8, 2, 3, 10, 91);
        rig.Input.Choices.Enqueue(1);
        rig.Input.Choices.Enqueue(2);

        await new NinjaCommand(rig.Script).RunAsync();

        Assert.Equal([5, 5, 100, 300], rig.Random.Draws);               // QUIRK: STR-14 — the 10 is dropped
        Assert.Equal([("Gｶﾅﾐ.ADV", 100)], rig.Calls);
    }

    [Fact]
    public async Task Freya_kills_troops_of_a_commander_drawn_from_the_owners_whole_range()
    {
        var rig = Ninja(4, 3, 50, 1, 7, 150);
        rig.State.F_Helman_War = 1;
        rig.State.Cmdrs[28].Status = 1;
        rig.State.Cmdrs[28].Troops = 1000;
        rig.State.Cmdrs[100].Status = 1;
        rig.State.Cmdrs[100].Troops = 500;
        rig.Input.Choices.Enqueue(2);                                   // Freya
        rig.Input.Choices.Enqueue(2);                                   // Sabotage

        await new NinjaCommand(rig.Script).RunAsync();

        Assert.Equal([5, 100, 2, 30, 200], rig.Random.Draws);
        Is(28, rig.State.Cmdr_Cur);                                     // QUIRK: CMD-11 — 21 + 7
        Is(850, rig.State.Cmdrs[28].Troops);
        Is(150, rig.State.Tmp0002);
        Is(0, rig.State.Cmdrs[100].Status);                             // QUIRK: EVT-04
        Is(0, rig.State.Cmdrs[100].Troops);
        Assert.Equal("s181", rig.State.Strings[4]);                     // the commander's name
        Assert.Equal(150, rig.View.Events.OfType<ValueMessageEvent>().Single(v => v.Text.Line == 787).Values[0].Value);
        Is(1, rig.State.Ui_CityScouted);

        rig = Ninja(4, 3, 50, 1, 7, 150, 60);                           // fewer troops than the draw: a second one
        rig.State.F_Helman_War = 1;
        rig.State.Cmdrs[28].Status = 1;
        rig.State.Cmdrs[28].Troops = 100;
        rig.Input.Choices.Enqueue(2);
        rig.Input.Choices.Enqueue(2);
        await new NinjaCommand(rig.Script).RunAsync();
        Assert.Equal([5, 100, 2, 30, 200, 100], rig.Random.Draws);
        Is(40, rig.State.Cmdrs[28].Troops);
    }

    [Fact]
    public async Task Freya_against_the_rebels_always_fails_and_zeroes_record_100()
    {
        var rig = Ninja(2, 3, 50, 1);
        rig.State.Cmdrs[100].Status = 1;
        rig.State.Cmdrs[100].Troops = 500;
        rig.Input.Choices.Enqueue(2);
        rig.Input.Choices.Enqueue(2);

        await new NinjaCommand(rig.Script).RunAsync();

        Assert.Equal([5, 100, 2], rig.Random.Draws);                    // QUIRK: CMD-11 — no target drawn
        Is(100, rig.State.Cmdr_Cur);
        Is(0, rig.State.Cmdrs[100].Troops);                             // QUIRK: EVT-04
        Is(0, rig.State.Tmp0001);
        Assert.Contains(807, rig.Lines);                                // her failure
        Is(0, rig.State.Ui_CityScouted);
    }

    [Fact]
    public async Task Kaoru_and_Wichita_weaken_a_living_commander_and_Saitou_never_succeeds()
    {
        var rig = Ninja(6, 3, 50, 1, 5, 2);                             // JAPAN at war; the target 70 + 5
        rig.State.F_Japan_State = 1;
        rig.State.Cmdrs[75].Status = 1;
        rig.State.Cmdrs[75].TroopDef = 4;
        rig.Input.Choices.Enqueue(3);                                   // Kaoru
        rig.Input.Choices.Enqueue(2);
        await new NinjaCommand(rig.Script).RunAsync();
        Is(3, rig.State.Cmdrs[75].TroopDef);
        Assert.Contains(880, rig.Lines);

        rig = Ninja(7, 3, 50, 1, 10);                                   // Zeth at war; the target 100 + 10
        rig.State.F_Zeth_War = 1;
        rig.State.Cmdrs[110].Status = 1;
        rig.State.Cmdrs[110].Recovery = 101;
        rig.Input.Choices.Enqueue(4);                                   // Wichita
        rig.Input.Choices.Enqueue(2);
        await new NinjaCommand(rig.Script).RunAsync();
        Is(50, rig.State.Cmdrs[110].Recovery);
        Assert.Contains(991, rig.Lines);

        rig = Ninja(2, 3, 50, 1);
        rig.Input.Choices.Enqueue(5);                                   // Saitou
        rig.Input.Choices.Enqueue(2);
        await new NinjaCommand(rig.Script).RunAsync();
        Is(1, rig.State.Tmp0001);                                       // QUIRK: CMD-11 — a success all the same
        Is(3, rig.State.Provs[5].Defense);
        Is(0, rig.State.Ui_CityScouted);
        Assert.Equal(1143, rig.Lines[^1]);                              // "Typical of a spy..."
    }

    // ---------------------------------------------------------------------------------------
    // Cﾃｲﾄ
    // ---------------------------------------------------------------------------------------

    private static SliceRig Construction(int gold)
    {
        var rig = new SliceRig(1);
        rig.State.Gold = (Sys3Int)gold;
        rig.State.Ui_CmdBuildAvail = 1;
        return rig;
    }

    [Fact]
    public async Task Construct_pays_starts_the_timer_and_spends_the_weeks_construction()
    {
        var rig = Construction(500);
        rig.State.Hist_Cur = 7;
        rig.Input.Choices.Enqueue(1);                                   // Build
        rig.Input.Choices.Enqueue(2);                                   // NEW Library
        rig.Input.Choices.Enqueue(1);                                   // Construct

        await new ConstructionCommand(rig.Script).RunAsync();

        Assert.Equal([[62, 66], [246, 252, 258, 264, 277, 282], [442, 446]], rig.MenuLines);
        Assert.Equal(500, rig.View.Events.OfType<ValueMessageEvent>().Single(v => v.Text.Line == 426).Values[0].Value);
        Is(100, rig.State.Gold);
        Is(400, rig.State.History[7].SundryExp);                        // QUIRK: CMD-14
        Is(8, rig.State.BuildWeeksLeft);
        Is(2, rig.State.Bld_Library);
        Is(0, rig.State.Ui_CmdBuildAvail);
        Is(16, rig.State.Unit_Cur);                                     // QUIRK: CMD-20 — the list's cursor
        Assert.Equal([464, 469], rig.Lines[^2..]);
    }

    [Fact]
    public async Task Something_Else_or_too_little_gold_shows_the_list_again()
    {
        var rig = Construction(399);
        rig.Input.Choices.Enqueue(1);                                   // Build
        rig.Input.Choices.Enqueue(2);                                   // NEW Library
        rig.Input.Choices.Enqueue(2);                                   // Something Else
        rig.Input.Choices.Enqueue(2);                                   // NEW Library
        rig.Input.Choices.Enqueue(1);                                   // Construct: not enough
        rig.Input.Choices.Enqueue(6);                                   // Don't Build: the first menu
        rig.Input.Choices.Enqueue(2);                                   // Don't

        await new ConstructionCommand(rig.Script).RunAsync();

        List<int> list = [246, 252, 258, 264, 277, 282];
        Assert.Equal([[62, 66], list, [442, 446], list, [442, 446], list, [62, 66]], rig.MenuLines);
        Is(399, rig.State.Gold);
        Is(0, rig.State.Bld_Library);
        Is(1, rig.State.Ui_CmdBuildAvail);
        Assert.Contains(966, rig.Lines);                                // "lacks the necessary funds"
    }

    [Theory]
    [InlineData(3, true)]
    [InlineData(1, false)]
    public async Task While_a_building_is_under_way_Maris_names_it_and_the_weeks_left(int weeks, bool plural)
    {
        var rig = Construction(5000);
        rig.State.BuildWeeksLeft = (Sys3Int)weeks;
        rig.State.Bld_Hospital = 2;
        rig.Input.Choices.Enqueue(1);                                   // Build

        await new ConstructionCommand(rig.Script).RunAsync();

        Assert.Single(rig.MenuLines);
        Assert.Contains(115, rig.Lines);                                // "the hospital"
        Assert.DoesNotContain(107, rig.Lines);
        Assert.Equal(plural, rig.Lines.Contains(135));                  // " n"
        Assert.Equal(plural, rig.Lines.Contains(142));                  // "s"
        Assert.Contains(rig.View.Events, e => e is MapEvent);
        Is(5000, rig.State.Gold);
        Is(1, rig.State.Ui_CmdBuildAvail);
    }

    [Fact]
    public async Task Marias_institute_needs_her_and_her_tulip_and_the_magic_lab_can_be_left()
    {
        var rig = Construction(2000);
        rig.State.Units[16].Status = 1;
        rig.State.F_Maria_Tulip = 2;
        rig.State.Bld_GirlsAcademy = 1;
        rig.State.Bld_Library = 1;
        rig.Input.Choices.Enqueue(1);                                   // Build
        rig.Input.Choices.Enqueue(4);                                   // Magic Research
        rig.Input.Choices.Enqueue(3);                                   // Don't Construct

        await new ConstructionCommand(rig.Script).RunAsync();

        Assert.Equal([[62, 66], [258, 264, 271, 277, 282], [920, 924, 928]], rig.MenuLines);
        Is(2000, rig.State.Gold);
        Is(0, rig.State.Bld_MagicLab);
        Assert.DoesNotContain(rig.View.Events, e => e is MapEvent);
    }

    [Fact]
    public async Task The_opening_for_everything_built_needs_the_pool_nothing_builds()
    {
        var rig = Construction(0);
        rig.State.Bld_GirlsAcademy = rig.State.Bld_Library = rig.State.Bld_Hospital = 2;
        rig.State.Bld_SMTower = rig.State.Bld_MariaLab = rig.State.Bld_MagicLab = 2;
        rig.Input.Choices.Enqueue(2);                                   // Don't

        await new ConstructionCommand(rig.Script).RunAsync();
        Assert.Equal([[62, 66]], rig.MenuLines);                        // QUIRK: CMD-20

        rig.State.F_Pool_Built = 2;
        rig.Input.Menus.Clear();
        await new ConstructionCommand(rig.Script).RunAsync();
        Assert.Empty(rig.Input.Menus);
        Assert.Equal(51, rig.Lines[^1]);                                // "There's nothing left to build"
    }

    // ---------------------------------------------------------------------------------------
    // Cﾒｲｷｭｳ
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// The <c>MS</c> lines of <c>Cﾒｲｷｭｳ.ADV</c>, as "line n" in the slot the line writes, and the
    /// label bodies a test hands out (the boss and Hanny setters), by their first line.
    /// </summary>
    private sealed class NameSource : IScenarioSource
    {
        public Dictionary<int, IReadOnlyList<ScriptStatement>> Blocks { get; } = [];

        /// <summary>The first lines of the blocks read, in order.</summary>
        public List<int> BlocksRead { get; } = [];

        public IReadOnlyList<ScriptStatement> ReadFile(string file) => throw new InvalidOperationException(file);

        public ScriptStatement ReadStatement(ScriptRef at) =>
            new SetStringStatement(at, at.Line switch { 27 => 10, 70 => 9, _ => 8 }, $"line {at.Line}");

        public IReadOnlyList<ScriptStatement> ReadBlock(ScriptRef at)
        {
            BlocksRead.Add(at.Line);
            return Blocks.TryGetValue(at.Line, out var block) ? block : throw new InvalidOperationException($"No block at {at}.");
        }
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

    [Theory]
    [InlineData(1, 3554, 3691)]
    [InlineData(2, 3691, 3554)]
    public async Task Gods_door_is_found_by_Rance_holding_item_1_or_2_and_joins_the_menu(int item, int scene, int other)
    {
        var rig = Dungeon(stage: 2, dungeon: 30);
        rig.State.F_GodsDoor_Quest = 2;
        rig.State.Units[1].Item = (Sys3Int)item;
        rig.Input.Choices.Enqueue(3);                                   // Return

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Is(1, rig.State.F_GodsDoor_Found);
        Assert.Contains(3513, rig.Lines);
        Assert.Contains(scene, rig.Lines);
        Assert.DoesNotContain(other, rig.Lines);
        Assert.Equal([267, 290, 295], rig.MenuLines[0]);
        Is(1, rig.State.Ui_CmdDungeonAvail);                            // Return gives the action back
    }

    [Fact]
    public async Task The_last_statue_opens_the_door_and_Enter_leads_into_Iｶﾐ_for_good()
    {
        var rig = Dungeon(stage: 2, dungeon: 30);
        rig.State.F_GodsDoor_Found = 1;
        rig.State.F_GodsDoor_Quest = 4;
        rig.State.F_Statue_Gourd = 1;
        rig.State.F_Statue_Monkey = 1;
        rig.State.F_Statue_Sunflower = 1;
        rig.State.Items[18].Holder = 1;                                 // the bonsai
        rig.On("Iｶﾐ.ADV", _ => throw new ScenarioJumpException("OVER.ADV"));
        foreach (int choice in new[] { 2, 1, 1 })                       // God's Door, the bonsai, Enter
            rig.Input.Choices.Enqueue(choice);

        var jump = await Assert.ThrowsAsync<ScenarioJumpException>(() => new DungeonCommand(rig.Script, new NameSource()).RunAsync());

        Assert.Equal("OVER.ADV", jump.File);
        Assert.Equal([3911, 3917, 3928], rig.MenuLines[1]);             // the statue, Rance's talk, Leave
        Assert.Equal([3923, 3928], rig.MenuLines[2]);                   // the door open: Enter, Leave
        Is(0, rig.State.Items[18].Holder);
        Is(1, rig.State.F_Statue_Bonsai);
        Is(6, rig.State.F_GodsDoor_Quest);
        Assert.Contains(4032, rig.Lines);                               // the door opens
        Assert.Contains(4549, rig.Lines);
        Assert.Equal("Iｶﾐ.ADV", rig.Calls[^1].File);
    }

    [Fact]
    public async Task Kouhimes_hint_names_the_missing_statues_and_moves_the_quest_on()
    {
        var rig = Dungeon(stage: 2, dungeon: 30);
        rig.State.F_GodsDoor_Found = 1;
        rig.State.F_GodsDoor_Quest = 3;
        rig.State.F_Kou_State = 10;
        rig.State.F_Statue_Monkey = 1;                                  // placed: the sunflower is the first missing
        foreach (int choice in new[] { 2, 2, 3 })                       // God's Door, Leave, Return
            rig.Input.Choices.Enqueue(choice);

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Is(4, rig.State.F_GodsDoor_Quest);
        Assert.Equal([3917, 3928], rig.MenuLines[1]);
        Assert.Contains(4415, rig.Lines);
        Assert.Equal([4463, 4473], rig.Lines.Where(l => l is 4445 or 4455 or 4463 or 4473 or 4480).ToList());
        Assert.DoesNotContain(3879, rig.Lines);
        Assert.Contains(3936, rig.Lines);                               // Leave
    }

    /// <summary>
    /// A search of <paramref name="dungeon"/> at stage 1: unit 5 (100 men) is picked in <c>Sﾌﾞﾀｲ</c>
    /// (status 2, as the picker leaves it); Start Search, then <paramref name="choices"/>.
    /// </summary>
    private static SliceRig Search(int dungeon, int[] draws, params int[] choices)
    {
        var rig = Dungeon(stage: 1, dungeon, draws);
        rig.State.Unit_StrBase = 100;
        rig.State.Units[5].Status = 1;
        rig.State.Units[5].Troops = 100;
        rig.State.Units[5].FaceCg = 555;
        rig.State.Cmdrs[83].MaxHP = 12;
        rig.On("Sﾌﾞﾀｲ.ADV", s =>
        {
            s.Bt_PlayerParty1 = 5;
            s.Units[5].Status = 2;
        });
        rig.Input.Choices.Enqueue(1);                                   // Start Search
        foreach (int choice in choices)
            rig.Input.Choices.Enqueue(choice);
        return rig;
    }

    /// <summary>The floors the arrivals' headers show (pit mode, <c>:679</c>).</summary>
    private static List<int> FloorsArrived(SliceRig rig) =>
        rig.View.Events.OfType<ValueMessageEvent>().Where(v => v.Text.Line == 679).Select(v => (int)v.Values[0].Value).ToList();

    private static List<string> Files(SliceRig rig) => rig.Calls.Select(c => c.File).ToList();

    [Fact]
    public async Task A_search_of_the_Monsters_Cave_fights_on_the_way_down_and_ends_on_Finish()
    {
        // Descend; Ascend/Descend/Retreat on floor 2: Retreat; Finish.
        var rig = Search(2, [1, 17], 1, 3, 1);
        (int Kind, int Enemy, int Background, int Troops, int Limit) atBattle = default;
        rig.On("FIGT.ADV", s => atBattle = (s.Bt_Kind.Value, s.Bt_EnemyParty1.Value, s.Bt_BackgroundCg.Value,
            s.Cmdrs[83].Troops.Value, s.Bt_TroopLimit.Value));
        rig.On("死亡捕獲.ADV", s => s.RetVal = 0);
        (int Size, int Mode) picker = default;
        rig.On("Sﾌﾞﾀｲ.ADV", s => picker = (s.Tmp0021.Value, s.Tmp0022.Value));

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Assert.Empty(rig.Input.Choices);
        Assert.Equal(["Sﾌﾞﾀｲ.ADV", "FIGT.ADV", "死亡捕獲.ADV"], Files(rig));
        Assert.Equal((1, 3), picker);                                   // one unit, for a dungeon
        Assert.Equal("s112", rig.State.Strings[1]);                     // the searcher's name
        Assert.Equal([4, 40], rig.Random.Draws);                        // the encounter (1: a battle), the monsters' troops
        Assert.Equal((3, 83, 910, 40 / 3 + 17, 80), atBattle);
        Is(12, rig.State.Cmdrs[83].HP);
        Assert.Equal([[267, 295], [884, 889], [866, 884, 889], [934, 938]], rig.MenuLines);
        Assert.Equal([1, 2], FloorsArrived(rig));
        Assert.Contains(1178, rig.Lines);                               // "Monsters appeared."
        Assert.Equal(975, rig.Lines.Last());                            // "Search of … ended."
        Is(1, rig.State.Dgns[2].State);                                 // in progress
        Is(2, rig.State.Dgns[2].SavedFloor);
        Is(14, rig.State.DgnRun_MaxFloor);
        Is(1031, rig.State.DungeonBgCg);
        Is(0, rig.State.Bt_Kind);
        Is(900, rig.State.Bt_BackgroundCg);
        Is(0, rig.State.Ui_CmdDungeonAvail);                            // the week's search is spent
        Assert.Equal([new MapEvent(1), new MapEvent(10), new MapEvent(0), new MapEvent(0), new MapEvent(0)], rig.View.Events.OfType<MapEvent>());
    }

    [Fact]
    public async Task A_battle_that_leaves_RetVal_2_draws_the_inscription_roll_without_Merim()
    {
        var rig = Search(2, [1, 17, 1], 1, 3, 1);
        rig.On("死亡捕獲.ADV", s => s.RetVal = 2);

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        // QUIRK: DGN-02, STR-14 — the battle's RetVal reaches the inscription test.
        Assert.Equal([4, 40, 10], rig.Random.Draws);
        Assert.Equal([1, 2], FloorsArrived(rig));
    }

    [Fact]
    public async Task A_searcher_who_falls_ends_the_command_at_once_with_the_floor_before_the_move()
    {
        var rig = Search(2, [1, 17], 1);
        rig.On("死亡捕獲.ADV", s => s.Units[5].Status = 0);

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        // QUIRK: DGN-08 — no end of the search, the floor saved before the move.
        Assert.DoesNotContain(975, rig.Lines);
        Is(1, rig.State.Dgns[2].SavedFloor);
        Is(2, rig.State.DgnRun_Floor);
        Assert.Equal([1], FloorsArrived(rig));
        Assert.Equal(3, rig.Windows.Last());
    }

    [Theory]
    [InlineData(1, 6)]                                                  // "6F On"
    [InlineData(2, 1)]                                                  // From the Start
    [InlineData(0, 1)]                                                  // a cancel starts over too
    public async Task A_saved_floor_offers_to_go_on_from_there(int choice, int floor)
    {
        var rig = Search(2, [], choice, floor == 1 ? 2 : 3, 1);         // then Retreat, Finish
        rig.State.Dgns[2].State = 1;
        rig.State.Dgns[2].SavedFloor = 6;

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        var resume = rig.Input.Menus[1];
        Assert.Equal([652, 657], resume.Select(i => i.At.Line));
        Assert.Equal([6], resume[0].Texts.Single().Numbers.Select(n => (int)n.Value));
        Assert.Contains(rig.View.Events, e => e is ValueMessageEvent { Text.Line: 634 } v && v.Values[0] == 6);
        Assert.Equal([floor], FloorsArrived(rig));
        Is(floor, rig.State.Dgns[2].SavedFloor);
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task A_cancelled_retreat_meets_an_empty_menu_before_the_floor_comes_back()
    {
        var rig = Search(2, [], 0, 2, 0, 0, 2, 1);                      // cancel the floor menu, Retreat, cancel, (empty), Retreat, Finish

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        // QUIRK: DGN-07 — the second ] has no items.
        Assert.Equal([[267, 295], [884, 889], [884, 889], [934, 938], [], [884, 889], [934, 938]], rig.MenuLines);
        Assert.Equal([1, 1], FloorsArrived(rig));                       // the floor arrived at again
        Assert.Equal(975, rig.Lines.Last());
    }

    [Theory]
    [InlineData(4, 0, 100, 30, 70)]                                     // the Snow Dungeon: r men lost
    [InlineData(4, 100, 100, -1, 100)]                                  // the charm in stock: no blizzard
    [InlineData(4, 7, 100, -1, 100)]                                    // QUIRK: DGN-09 — equipped protects too
    [InlineData(5, 0, 100, 60, 0)]                                      // Ho Raga's Tower: half plus r, at most all
    [InlineData(5, 0, 100, 20, 30)]
    public async Task A_blizzard_costs_men_without_the_Snow_Charm(int dungeon, int holder, int troops, int roll, int left)
    {
        var rig = Search(dungeon, roll < 0 ? [] : [roll], 2, 1);       // Retreat, Finish
        rig.State.Items[11].Holder = (Sys3Int)holder;
        rig.State.Units[5].Troops = (Sys3Int)troops;

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Is(left, rig.State.Units[5].Troops);
        Assert.Equal(roll < 0 ? [] : [troops], rig.Random.Draws);
        int line = dungeon == 4 ? 374 : 398;
        Assert.Equal(roll >= 0, rig.Lines.Contains(line));
        Assert.Equal(holder == 0 ? 5377 : holder == 100 ? 5350 : 324, rig.Lines.First(l => l is 5350 or 5377 or 324));
        if (dungeon == 5)
            Assert.Equal([("Gﾎﾗｶﾞ.ADV", 2), ("Sﾌﾞﾀｲ.ADV", 2), ("Gﾎﾗｶﾞ.ADV", 1)], rig.Calls);
    }

    /// <summary><c>L_04376</c> (<c>:3062-3092</c>), shortened: Marder over commander 100.</summary>
    private static IReadOnlyList<ScriptStatement> MarderSetter()
    {
        var at = new ScriptRef("Cﾒｲｷｭｳ.ADV", 3062);
        AssignStatement Set(string name, int value) =>
            new(at, VariableCatalog.Variables.Select(v => v.Name).ToList().IndexOf(name), new ConstExpr((Sys3Int)value));
        return
        [
            Set("Bt_IsDuel", 1), Set("F_Boss_Marder", 1), Set("Cmdr_Cur", 100), new SetStringStatement(at, 4, "Marder"),
            Set("Cmdr_CgFull", 1328), Set("Cmdr_Troops", 0), Set("Cmdr_Atk", 15), Set("Cmdr_HP", 10), Set("Cmdr_MaxHP", 10),
            new ReturnLabelStatement(at),
        ];
    }

    [Fact]
    public async Task Marder_waits_on_the_fifth_floor_once_and_the_monsters_troops_are_drawn_first()
    {
        var rig = Search(2, [3, 25], 1, 2, 3, 1);                       // 4F On, Descend, Retreat, Finish
        rig.State.Dgns[2].State = 1;
        rig.State.Dgns[2].SavedFloor = 4;
        rig.State.BgmEnabled = 1;
        var source = new NameSource();
        source.Blocks[3062] = MarderSetter();
        (int Enemy, int Duel, int Troops) atBattle = default;
        rig.On("FIGT.ADV", s => atBattle = (s.Bt_EnemyParty1.Value, s.Bt_IsDuel.Value, s.Cmdrs[100].Troops.Value));

        await new DungeonCommand(rig.Script, source).RunAsync();

        // QUIRK: STR-14 — a roll of 3 is no battle, but the boss forces one; the troops are drawn for nothing.
        Assert.Equal([4, 40], rig.Random.Draws);
        Assert.Equal([3062], source.BlocksRead);
        Assert.Equal((100, 1, 0), atBattle);
        Is(40 / 3 + 25, rig.State.Cmdrs[83].Troops);                    // the monsters', overwritten by nobody
        // QUIRK: DGN-04 — the flag is set before the fight.
        Is(1, rig.State.F_Boss_Marder);
        Is(0, rig.State.Bt_IsDuel);
        Assert.Equal("Marder", rig.State.Strings[4]);
        var music = rig.View.Events.OfType<MusicEvent>().Select(m => (int)m.Track.Value).ToList();
        Assert.Equal([21, 18, 21], music.Take(3));                      // the dungeon's, the boss's, the dungeon's again
        Assert.Equal([4, 5], FloorsArrived(rig));
    }

    [Fact]
    public async Task Without_Marder_a_roll_of_3_on_the_fifth_floor_is_no_battle()
    {
        var rig = Search(2, [3], 1, 2, 3, 1);                           // 4F On, Descend, Retreat, Finish
        rig.State.Dgns[2].State = 1;
        rig.State.Dgns[2].SavedFloor = 4;
        rig.State.F_Boss_Marder = 1;

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Assert.Equal([4], rig.Random.Draws);
        Assert.Equal(["Sﾌﾞﾀｲ.ADV"], Files(rig));
    }

    [Fact]
    public async Task The_DO_Pot_waits_on_the_last_floor_and_finishes_the_dungeon()
    {
        var rig = Search(2, [3], 1, 2, 2, 1);                           // 13F On, Descend, Retreat, Finish
        rig.State.Dgns[2].State = 1;
        rig.State.Dgns[2].SavedFloor = 13;
        rig.State.BgmEnabled = 1;

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        Is(100, rig.State.Items[9].Holder);                             // to Leazas's stock
        Is(2, rig.State.Dgns[2].State);                                 // fully searched
        Assert.Equal([1455, 1462, 1467], rig.Lines.Where(l => l is >= 1455 and <= 1467));
        Assert.Contains(new WindowEvent(311, 555), rig.View.Events);   // the searcher's portrait
        Assert.Contains(rig.View.Events, e => e is SoundEvent { Sound.Value: 101 });   // the fanfare
        Assert.Equal([866, 889], rig.MenuLines[3]);                     // the last floor: only up and out
        Assert.Equal(975, rig.Lines.Last());
    }

    [Fact]
    public async Task Satella_refuses_the_dungeon_keeps_the_charge_and_the_menu_comes_back()
    {
        var rig = Dungeon(stage: 1, dungeon: 2);
        rig.State.Units[29].Status = 1;
        rig.On("Sﾌﾞﾀｲ.ADV", s =>
        {
            s.Bt_PlayerParty1 = 29;
            s.Units[29].Status = 2;
        });
        rig.Input.Choices.Enqueue(1);                                   // Start Search
        rig.Input.Choices.Enqueue(2);                                   // Return

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        // QUIRK: BFL-01 — status 1 again, nothing refunded; DGN-06 — back to the dispatch.
        Is(1, rig.State.Units[29].Status);
        Assert.Equal([5315, 5320, 5325, 5339], rig.Lines.Where(l => l >= 5315));
        Assert.Equal([[267, 295], [267, 295]], rig.MenuLines);
        Is(0, rig.State.Dgns[2].State);                                 // never searched
        Is(1, rig.State.Ui_CmdDungeonAvail);                            // Return gives the week's search back
        Assert.Empty(rig.Random.Draws);
    }

    [Fact]
    public async Task An_empty_picker_goes_back_to_the_dispatch_and_its_pre_scene()
    {
        var rig = Dungeon(stage: 1, dungeon: 16);
        rig.Input.Choices.Enqueue(1);                                   // Start Search, nobody picked
        rig.Input.Choices.Enqueue(0);                                   // a cancel leaves

        await new DungeonCommand(rig.Script, new NameSource()).RunAsync();

        // QUIRK: DGN-06 — the Giant Battleship's scene again.
        Assert.Equal([("G1.ADV", 10), ("Sﾌﾞﾀｲ.ADV", 10), ("G1.ADV", 10)], rig.Calls);
        Is(1, rig.State.Ui_CmdDungeonAvail);
    }

    // ---------------------------------------------------------------------------------------
    // Cﾀｶﾗ
    // ---------------------------------------------------------------------------------------

    [Fact]
    public async Task Treasure_loses_the_items_of_fallen_units_then_shows_the_item_screen()
    {
        var rig = new SliceRig(stage: 1);
        rig.State.Units[2].Status = 0;
        rig.State.Units[5].Status = 1;
        foreach (var (item, holder) in new[] { (3, 2), (4, 5), (9, 100) })
        {
            rig.State.Items[item].Holder = (Sys3Int)holder;
            if (holder != 100)
                rig.State.Units[holder].Item = (Sys3Int)item;
        }

        await new TreasureCommand(rig.Script).RunAsync();

        // QUIRK: CMD-18 — not owned (0), not back in stock (100).
        Assert.Equal(0, rig.State.Items[3].Holder.Value);
        Assert.Equal(0, rig.State.Units[2].Item.Value);
        Assert.Equal(5, rig.State.Items[4].Holder.Value);
        Assert.Equal(100, rig.State.Items[9].Holder.Value);
        Assert.Equal(["Sｱｲﾃﾑ.ADV"], rig.Scenes.Concat(rig.Calls.Select(c => c.File).Where(f => f[0] == 'S')).Distinct());
        Assert.Equal(51, rig.State.Item_Cur.Value);
    }

    // ---------------------------------------------------------------------------------------
    // 各種DATA
    // ---------------------------------------------------------------------------------------

    private static SliceRig AllData()
    {
        var rig = new SliceRig(stage: 1);
        for (int k = 0; k < 16; k++)
        {
            rig.State.History[k].Funds = (Sys3Int)(1000 * k);
            rig.State.History[k].MilitaryExp = (Sys3Int)(100 * k);
            rig.State.History[k].SundryExp = (Sys3Int)(50 * k);
            rig.State.History[k].Casualties = (Sys3Int)(10 * k);
        }
        rig.State.Hist_Cur = 9;
        return rig;
    }

    [Fact]
    public async Task All_Data_shows_a_graph_and_keeps_the_scale_picked()
    {
        var rig = AllData();
        rig.Input.Choices.Enqueue(1);                                   // All Funds
        rig.Input.Pointer.RightClick(300, 200);                         // the scale menu
        rig.Input.Choices.Enqueue(3);                                   // 4x
        rig.Input.Pointer.Click(300, 200);                              // back to the menu
        rig.Input.Choices.Enqueue(5);                                   // Return

        await new AllDataCommand(rig.Script).RunAsync();

        Assert.Equal((10000 + 6383) / 300, rig.State.Ui_MoneyGraphDiv.Value);
        Assert.Equal(4, rig.State.Ui_MoneyGraphScale.Value);
        Assert.Equal(4095 / 300, rig.State.Ui_DamageGraphDiv.Value);    // set on the first visit
        Assert.Equal(16, rig.State.Ui_DamageGraphScale.Value);
        Assert.Equal(0, rig.State.Hist_Cur.Value);
        Assert.Equal([[28, 32, 36, 40, 44], [316, 320, 324, 328, 332, 336, 340, 344], [28, 32, 36, 40, 44]], rig.MenuLines);
        var titles = rig.View.Events.OfType<ValueMessageEvent>().Where(v => v.Text.Line == 71).Select(v => (int)v.Values[0].Value);
        Assert.Equal([16, 4], titles);                                  // drawn again at the new scale
        var funds = rig.View.Events.OfType<ValueMessageEvent>().Where(v => v.Text.Line == 117).Take(16).Select(v => (int)v.Values[0].Value);
        Assert.Equal(Enumerable.Range(0, 16).Select(k => 1000 * k / 100), funds);   // newest first, in hundreds
        Assert.Equal(
            [DataView.Map, DataView.Funds, DataView.Funds, DataView.Map, DataView.Closed],
            rig.View.Events.OfType<DataScreenEvent>().Select(e => e.View));
    }

    [Fact]
    public async Task A_bar_past_the_scale_is_cut_and_marked()
    {
        var rig = AllData();
        rig.State.History[3].Casualties = 9000;                         // 9000 / 13 > 300
        rig.Input.Choices.Enqueue(4);                                   // All Casualties
        rig.Input.Pointer.Click(300, 200);
        rig.Input.Choices.Enqueue(5);

        await new AllDataCommand(rig.Script).RunAsync();

        Assert.Equal([271], rig.Lines.Where(l => l == 271));           // "OV" once
        var labels = rig.View.Events.OfType<ValueMessageEvent>().Where(v => v.Text.Line == 264).Select(v => (int)v.Values[0].Value).ToList();
        Assert.Equal(900, labels[3]);                                   // in tens
        Assert.Contains(rig.View.Events, e => e is EngineEvent { Command: "CF" } cf && cf.Args[3] == 300);
    }

    [Fact]
    public async Task A_cancelled_menu_comes_back_on_a_left_click_and_ends_on_a_right_click()
    {
        var rig = AllData();
        rig.Input.Choices.Enqueue(0);
        rig.Input.Pointer.Click(300, 200);                              // the menu again
        rig.Input.Choices.Enqueue(0);
        rig.Input.Pointer.RightClick(300, 200);                         // the screen ends

        await new AllDataCommand(rig.Script).RunAsync();

        Assert.Equal(2, rig.Input.Menus.Count);
        Assert.Equal(0, rig.State.Hist_Cur.Value);
        Assert.Equal(1, rig.Input.Pointer.Pending);                     // it ends at the press: the release is not read
        Assert.Equal(DataView.Closed, rig.View.Events.OfType<DataScreenEvent>().Last().View);
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
        var script = new OriginalScriptContext(state, new ScriptedRandom(2), new RecordingView(), input, rules);
        rules.Bind(script);
        state.Dgn_Cur = 1;
        state.Units[4].Status = 1;

        await script.CallFileAsync("Cﾒｲｷｭｳ.ADV");                       // ported: the hideout's banter
        await script.EventAsync("Gｼｨﾙ.ADV", 10);                       // not here: handed on

        Is(2, state.Tmp0690);
        Assert.Equal([("Gｼｨﾙ.ADV", 10)], unported.Calls);
        Assert.Contains("Cﾊｰﾚﾑ.ADV", StrategyRules.PortedFiles);
        Assert.Throws<InvalidOperationException>(() => rules.Bind(new OriginalScriptContext(state, new ScriptedRandom(), new RecordingView(), input, rules)));
    }

    /// <summary>Compares value messages by their lists' contents.</summary>
    private sealed class ValueMessageComparer : IEqualityComparer<ScriptEvent>
    {
        public bool Equals(ScriptEvent? x, ScriptEvent? y) =>
            x is ValueMessageEvent a && y is ValueMessageEvent b && a.Text == b.Text && a.Values.SequenceEqual(b.Values);

        public int GetHashCode(ScriptEvent obj) => 0;
    }
}
