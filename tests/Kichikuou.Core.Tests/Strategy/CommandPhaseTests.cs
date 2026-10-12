using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Strategy;
using Kichikuou.Core.Sys3;
using Kichikuou.Core.Tests.Combat;
using Kichikuou.Core.Tests.Script;

namespace Kichikuou.Core.Tests.Strategy;

/// <summary>
/// <c>COMMAND.ADV</c> (<see cref="CommandPhase"/>): the start of the phase, the map loop with its
/// clicks, drags and right clicks, the routing of a province click, the two command menus and Turn
/// END, with the quirks STR-09, STR-19 to STR-21. The whole file is also compared with the
/// decompile on random states and gestures (Content tests, <c>CommandPhaseDifferentialTests</c>).
/// </summary>
public class CommandPhaseTests
{
    /// <summary>A phase over a fresh state: 100 provinces and 30 dungeons, all at the map's corner unless placed.</summary>
    private sealed class PhaseRig
    {
        public PhaseRig(int stage)
        {
            Script = new OriginalScriptContext(State, new ScriptedRandom(), View, Input, Story);
            State.StoryStage = (Sys3Int)stage;
            State.Week = 10;
            State.Prov_Count = 100;
            State.Dgn_Count = 30;
            // The camera: stage 0 starts at the last target, later stages pan to Leazas Castle.
            State.MapTargetX = 800;
            State.MapTargetY = 400;
            Place(1, 800, 400);
            State.Provs[1].Owner = 1;
            State.MapCamX = 800;
            State.MapCamY = 400;
        }

        public GameState State { get; } = new();
        public ScriptedSceneInput Input { get; } = new();
        public RecordingView View { get; } = new();
        public ScriptedStory Story { get; } = new();
        public OriginalScriptContext Script { get; }

        public List<string> Files => Story.Calls.Select(c => c.File).ToList();

        public void Place(int province, int x, int y)
        {
            State.Provs[province].MapX = (Sys3Int)x;
            State.Provs[province].MapY = (Sys3Int)y;
        }

        /// <summary>A left click on a map point (the camera centred on (800, 400)).</summary>
        public void Click(int mapX, int mapY)
        {
            var (x, y) = GesturePoints.OnScreen(State, (Sys3Int)mapX, (Sys3Int)mapY)!.Value;
            Input.Pointer.Click(x, y);
        }

        /// <summary>A right click (the menu), then the item chosen.</summary>
        public void Menu(int choice)
        {
            Input.Pointer.RightClick(5, 6);
            Input.Choices.Enqueue(choice);
        }

        public Task RunAsync() => new CommandPhase(Script).RunAsync();
    }

    private static void Is(int expected, Sys3Int actual) => Assert.Equal(expected, actual.Value);

    private static IEnumerable<int> MessageLines(PhaseRig rig) =>
        rig.View.Events.Select(e => e switch { MessageEvent m => m.Text.Line, ValueMessageEvent v => v.Text.Line, _ => 0 }).Where(l => l != 0);

    [Fact]
    public async Task The_phase_starts_with_COM_and_sets_the_weekly_flags()
    {
        var rig = new PhaseRig(stage: 0);
        rig.State.Ui_CmdHaremAvail = 0;
        rig.State.CmdAvail_SubTalk = 0;
        rig.Menu(5);                                   // Turn END

        await rig.RunAsync();

        Assert.Equal(["COM.ADV"], rig.Files);
        Is(1, rig.State.Ui_CmdHaremAvail);
        Is(1, rig.State.Ui_CmdSubordAvail);
        Is(1, rig.State.Ui_CmdOwnCityAvail);
        Is(1, rig.State.Ui_CmdBuildAvail);
        Is(1, rig.State.Ui_CmdAttackAvail);
        Is(1, rig.State.Ui_CmdMerchantAvail);
        Is(1, rig.State.Ui_CmdDungeonAvail);
        Is(1, rig.State.CmdAvail_SubTroops);
        Is(1, rig.State.CmdAvail_SubTalk);
        var start = rig.View.Events.Take(3).ToList();
        Assert.Equal(new SoundEvent((Sys3Int)10, Sys3Int.Zero), start[0]);   // PCMPLAY op 10, then COM (a hook here)
        Assert.Equal(new MapEvent(Sys3Int.Zero), start[1]);                 // stage 0: no pan, a redraw at the last target
        Assert.Equal(new MusicEvent(Sys3Int.Zero), start[2]);               // L_00129 SS CurrentBgm
    }

    [Fact]
    public async Task In_the_prologue_a_click_on_Vladivostok_runs_the_enemy_city_and_the_end_of_the_prologue_ends_the_phase()
    {
        var rig = new PhaseRig(stage: 0);
        rig.Place(34, 900, 450);
        rig.Click(902, 447);
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Cﾃｷﾄｼ.ADV")
                rig.State.StoryStage = 99;             // Iﾄｳｿﾞｸ's lost duel
        };

        await rig.RunAsync();

        Assert.Equal(["COM.ADV", "Cﾃｷﾄｼ.ADV"], rig.Files);
        Is(34, rig.State.Ui_ClickedProv);
        Is(34, rig.State.Prov_Cur);
        Is(902, rig.State.Tmp0025);                    // the pressed point on the map
        Is(447, rig.State.Tmp0026);
        Assert.Empty(rig.Input.Cursors);               // released at once: no IC
    }

    [Fact]
    public async Task In_the_prologue_another_province_is_only_viewed_and_the_thieves_menu_shows_their_status()
    {
        var rig = new PhaseRig(stage: 0);
        rig.Place(5, 700, 300);
        rig.Click(700, 300);
        rig.Menu(5);
        rig.State.Gold = 50;
        rig.State.Units[1].Troops = 10;
        rig.State.Units[2].Troops = 1000;              // Sill is not counted
        rig.State.Units[3].Troops = 20;
        rig.State.Units[4].Troops = 30;

        await rig.RunAsync();

        Assert.Equal(["COM.ADV", "Cﾃｷﾄｼ2.ADV"], rig.Files);
        var menu = Assert.Single(rig.Input.Menus);
        Assert.Equal([187, 199, 210, 214, 218], menu.Select(i => i.At.Line));
        Assert.Equal([188, 200, 211, 215, 219], menu.Select(i => i.Texts.Single().Text.Line));
        var status = rig.View.Events.OfType<ValueMessageEvent>().Single();
        Assert.Equal(new ScriptRef("COMMAND.ADV", 472), status.Text);
        Assert.Equal([50, 60, 3, 2], status.Values.Select(v => v.Value));   // week 10: month 3, week 2
        Is(3, rig.State.DateYear);
    }

    [Fact]
    public async Task Turn_END_after_week_22_plays_the_last_fight_and_the_opening_and_ends_the_prologue()
    {
        var rig = new PhaseRig(stage: 0);
        rig.State.Week = 23;
        rig.Menu(5);

        await rig.RunAsync();

        Assert.Equal([("COM.ADV", 0), ("Iﾄｳｿﾞｸ.ADV", 2), ("ｵｰﾌﾟﾆﾝｸﾞ.ADV", 2)], rig.Story.Calls);
        Is(99, rig.State.StoryStage);
    }

    [Fact]
    public async Task A_click_goes_to_the_own_city_an_adjacent_enemy_city_or_the_view_of_a_far_one()
    {
        var rig = new PhaseRig(stage: 2);
        rig.Place(7, 600, 300);
        rig.State.Provs[7].Owner = 1;
        rig.Place(40, 700, 300);
        rig.State.Provs[40].Owner = 2;
        rig.State.Provs[40].Adj1 = 41;                 // not own
        rig.State.Provs[40].Adj2 = 7;                  // own
        rig.Place(41, 900, 500);
        rig.State.Provs[41].Owner = 2;
        rig.State.Provs[41].Adj1 = 40;
        var at = new List<int>();
        rig.Story.OnCall = (_, _) => at.Add(rig.State.Prov_Cur.Value);
        rig.Click(600, 300);
        rig.Click(700, 300);
        rig.Click(900, 500);
        rig.Menu(10);                                  // Turn END, the last of the Leazas menu

        await rig.RunAsync();

        Assert.Equal(["COM.ADV", "Cｼﾊｲﾄｼ.ADV", "Cﾃｷﾄｼ.ADV", "Cﾃｷﾄｼ2.ADV"], rig.Files);
        Assert.Equal([0, 7, 40, 41], at);              // the cursor is back on the clicked province
        Is(41, rig.State.Ui_ClickedProv);
    }

    [Fact]
    public async Task A_drag_moves_the_view_with_the_pointer()
    {
        var rig = new PhaseRig(stage: 0);
        rig.Input.Pointer.Drag(100, 100, 160, 130);
        rig.Menu(5);

        await rig.RunAsync();

        Is(740, rig.State.MapCamX);                    // the map follows the pointer: 60 right, 30 down
        Is(370, rig.State.MapCamY);
        Assert.Equal([(6, 1), (1, 6), (6, 1), (1, 6)], rig.Input.Cursors);  // COMMAND's press cursor, then WAO_MAP's
        Assert.Single(rig.View.Events.OfType<EngineEvent>(), e => e.Command == "CC");
        Assert.Equal(["COM.ADV"], rig.Files);          // a drag is no click
    }

    [Fact]
    public async Task A_province_out_of_view_is_dragged_in_and_clicked_with_the_camera_as_the_corner()
    {
        var rig = new PhaseRig(stage: 2);
        rig.Place(7, 1500, 700);
        rig.State.Provs[7].Owner = 1;
        Assert.Null(GesturePoints.OnScreen(rig.State, (Sys3Int)1500, (Sys3Int)700));
        var drag = GesturePoints.DragToward(rig.State, (Sys3Int)1500, (Sys3Int)700)!.Value;
        Assert.Equal((620, 300, 20, 100), drag);       // the corner (480, 200) moves 600 right (at most), 200 down
        rig.Input.Pointer.Drag(drag.FromX, drag.FromY, drag.ToX, drag.ToY);
        rig.Input.Pointer.Click(420, 300);             // the corner is (1080, 400) now
        rig.Menu(10);

        await rig.RunAsync();

        Assert.Equal(["COM.ADV", "Cｼﾊｲﾄｼ.ADV"], rig.Files);
        Is(7, rig.State.Ui_ClickedProv);
        Assert.Equal((420, 300), GesturePoints.OnScreen(rig.State, (Sys3Int)1500, (Sys3Int)700));   // centred again
        rig.State.MapCamX = 1080;
        rig.State.MapCamY = 400;
        Assert.Equal((420, 300), GesturePoints.OnScreen(rig.State, (Sys3Int)1500, (Sys3Int)700, centred: false));
        Assert.Null(GesturePoints.DragToward(rig.State, (Sys3Int)1400, (Sys3Int)600, centred: false));   // already in the middle
    }

    [Fact]
    public async Task A_dungeon_is_entered_only_in_an_own_province_but_its_province_stays_selected()
    {
        var rig = new PhaseRig(stage: 2);
        rig.State.Dgns[5].MapX = 1000;
        rig.State.Dgns[5].MapY = 500;
        rig.State.Dgns[5].Prov = 40;
        rig.State.Provs[40].Owner = 2;
        rig.State.Dgns[6].MapX = 600;
        rig.State.Dgns[6].MapY = 300;
        rig.Click(1003, 498);
        rig.Click(600, 300);
        var entered = new List<int>();
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Cﾒｲｷｭｳ.ADV")
                entered.Add(rig.State.Dgn_Cur.Value);
        };
        rig.Menu(10);

        await rig.RunAsync();

        Assert.Equal([6], entered);                    // dungeon 6 has no province: always enterable
        Assert.Equal(["COM.ADV", "Cﾒｲｷｭｳ.ADV"], rig.Files);
        Is(0, rig.State.Prov_Cur);
    }

    [Fact]
    public async Task A_click_on_a_dungeon_in_an_enemy_province_moves_the_province_cursor()
    {
        var rig = new PhaseRig(stage: 2);
        rig.State.Dgns[5].MapX = 1000;
        rig.State.Dgns[5].MapY = 500;
        rig.State.Dgns[5].Prov = 40;
        rig.State.Provs[40].Owner = 2;
        rig.Click(1003, 498);
        rig.Menu(10);

        await rig.RunAsync();

        Assert.Equal(["COM.ADV"], rig.Files);
        // QUIRK: STR-19 — nothing happens, but Prov_Cur is the dungeon's province.
        Is(40, rig.State.Prov_Cur);
        Is(30, rig.State.Dgn_Cur);                     // the search went on to the last record
    }

    [Fact]
    public async Task An_unavailable_item_opens_the_menu_again()
    {
        var rig = new PhaseRig(stage: 0);
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "Cﾊｰﾚﾑ.ADV")
                rig.State.Ui_CmdHaremAvail = 0;        // the visit uses the week's flag
        };
        rig.Menu(1);                                   // ○Harem
        rig.Menu(1);                                   // ×Harem: straight back to the menu …
        rig.Input.Choices.Enqueue(5);                  // … where Turn END is chosen

        await rig.RunAsync();

        Assert.Equal(["COM.ADV", "Cﾊｰﾚﾑ.ADV"], rig.Files);
        Assert.Equal([187, 193, 193], rig.Input.Menus.Select(m => m[0].At.Line));
    }

    [Fact]
    public async Task A_cancelled_menu_puts_the_pointer_back()
    {
        var rig = new PhaseRig(stage: 0);
        rig.Input.Pointer.RightClick(5, 6);
        rig.Input.Choices.Enqueue(0);
        rig.Menu(5);

        await rig.RunAsync();

        Assert.Equal(2, rig.Input.Menus.Count);
        var warp = rig.View.Events.OfType<EngineEvent>().First(e => e.Command == "IZ");
        Assert.Equal([5, 6], warp.Args.Select(a => a.Value));
    }

    [Fact]
    public async Task The_construction_timer_finishes_the_first_building_under_construction()
    {
        var rig = new PhaseRig(stage: 2);
        rig.State.BuildWeeksLeft = 1;
        rig.State.Bld_Library = 2;
        rig.State.Bld_MagicLab = 2;
        rig.Menu(10);

        await rig.RunAsync();

        Assert.Equal([("COM.ADV", 0), ("Gﾏﾘｽ.ADV", 5)], rig.Story.Calls);
        Is(0, rig.State.BuildWeeksLeft);
        Is(1, rig.State.Bld_Library);
        Is(2, rig.State.Bld_MagicLab);                 // QUIRK: STR-09 — one building per completion
    }

    [Fact]
    public async Task A_timer_that_runs_out_with_no_building_reads_the_pointer()
    {
        var rig = new PhaseRig(stage: 2);
        rig.State.BuildWeeksLeft = 1;
        rig.State.Tmp0019 = 99;
        rig.State.Tmp0020 = 99;
        int beforeTheMap = 0;
        rig.Input.OnRead = () =>
        {
            if (!rig.View.Events.OfType<MusicEvent>().Any())
                beforeTheMap++;
        };
        rig.Menu(10);

        await rig.RunAsync();

        // QUIRK: STR-21 — L_00c21 falls into L_00ced: an IM into Tmp0019/0020 before the map.
        Assert.Equal(1, beforeTheMap);
        Assert.Equal(["COM.ADV"], rig.Files);
    }

    [Fact]
    public async Task Turn_END_lets_the_Tange_Dog_stand_its_holder_up_once_a_week()
    {
        var rig = new PhaseRig(stage: 2);
        rig.State.Items[34].Holder = 3;
        rig.State.Units[3].Status = 2;
        rig.State.Units[3].FaceCg = 77;
        rig.State.Unit_StrBase = 10;
        rig.State.Strings[16] = "Bound";
        rig.Menu(10);

        await rig.RunAsync();

        Is(1, rig.State.Units[3].Status);
        Is(1, rig.State.TangeDog_UsedThisWeek);
        Assert.Equal("Bound", rig.State.Strings[1]);
        Assert.Equal([443, 397, 407], MessageLines(rig));
        Is(0, rig.State.Ui_PortraitL);                 // the portrait shown with the first line is removed
    }

    [Fact]
    public async Task The_Leazas_menu_offers_what_the_week_left()
    {
        var rig = new PhaseRig(stage: 2);
        rig.State.DebugMode = 1;
        rig.Story.OnCall = (file, _) =>
        {
            if (file == "COM.ADV")
                return;
            rig.State.Ui_CmdBuildAvail = 0;
            rig.State.Ui_CmdMerchantAvail = 0;
            rig.State.CmdAvail_SubTroops = 0;
        };
        rig.Menu(3);                                   // Treasure
        rig.Menu(11);                                  // Turn END after TADA

        await rig.RunAsync();

        Assert.Equal([229, 241, 252, 257, 270, 288, 292, 296, 300, 305, 310], rig.Input.Menus[0].Select(i => i.At.Line));
        Assert.Equal([235, 241, 252, 263, 276, 288, 292, 296, 300, 305, 310], rig.Input.Menus[1].Select(i => i.At.Line));
    }
}
