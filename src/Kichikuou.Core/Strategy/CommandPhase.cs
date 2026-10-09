using Kichikuou.Core.Script;
using Kichikuou.Core.State;
using Kichikuou.Core.Sys3;

namespace Kichikuou.Core.Strategy;

/// <summary>
/// <c>COMMAND.ADV</c>, the player phase (part W3), ported one-to-one
/// (<c>docs/strategy/survey-main.md</c> §2.2, <c>docs/porting-conventions.md</c>): the weekly upkeep
/// (<c>COM</c>, <see cref="WeeklyEvents"/>), the construction timer, the weekly command flags, then
/// the map loop until "Turn END". The map is read the way the script reads it — <c>IM</c> polled
/// in loops, <c>IC</c> for the cursor, the drag-scroll of <c>WAO_MAP</c> op 1 — so the input is
/// <see cref="ISceneInput"/> (a UI that thinks in provinces turns its choices into pointer
/// gestures, <see cref="PointerScript"/>). Every other file — <c>COM</c>, the <c>C*</c> commands,
/// the save screen, the scenes — is called by name through the context's hooks
/// (<see cref="ScriptContext.CallFileAsync"/>), which run the ported ones.
/// </summary>
public sealed class CommandPhase(ScriptContext script)
{
    private const string File = "COMMAND.ADV";

    /// <summary>The shared services the file runs on.</summary>
    public ScriptContext Script { get; } = script;

    private GameState State => Script.State;

    private ISceneInput Input =>
        Script.Input as ISceneInput
        ?? throw new InvalidOperationException("The player phase reads the mouse: the context's input must be an ISceneInput.");

    /// <summary>The player-unit record selected by <c>Unit_Cur</c>, re-read on every access.</summary>
    private UnitRecord Unit => State.Units[State.Unit_Cur];

    /// <summary>The province record selected by <c>Prov_Cur</c>, re-read on every access.</summary>
    private ProvRecord Prov => State.Provs[State.Prov_Cur];

    /// <summary>The dungeon record selected by <c>Dgn_Cur</c>, re-read on every access.</summary>
    private DgnRecord Dgn => State.Dgns[State.Dgn_Cur];

    /// <summary>The item record selected by <c>Item_Cur</c>, re-read on every access.</summary>
    private ItemRecord Item => State.Items[State.Item_Cur];

    /// <summary>What a pass of the map loop or the menu leads to.</summary>
    private enum Next
    {
        /// <summary><c>@L_00129</c>: the music again, then the map.</summary>
        Map,

        /// <summary><c>@L_00431</c>: the command menu again (a "×" item).</summary>
        Menu,

        /// <summary><c>%0:</c> — the phase ends.</summary>
        End,
    }

    /// <summary><c>COMMAND.ADV:1-577</c>: the whole phase, up to the <c>%0:</c> of Turn END or of the prologue's end.</summary>
    public async Task RunAsync()
    {
        await StartAsync();
        var next = Next.Map;
        while (true)
        {
            if (next == Next.Map)
            {
                await Script.MusicAsync(State.CurrentBgm);              // L_00129 (:47-48)
                if (await MapAsync())                                  // L_0012e-L_0041a
                {
                    next = await ProvinceClickedAsync();               // L_003f9 → L_00a95
                    if (next == Next.End)
                        return;
                    continue;
                }
            }
            next = await CommandMenuAsync();                           // L_00431
            if (next == Next.End)
                return;
        }
    }

    /// <summary><c>COMMAND.ADV:1-46</c> — <c>COM</c>, the camera, the construction timer, the weekly flags.</summary>
    private async Task StartAsync()
    {
        await Script.SoundAsync(10);                                    // :1-2 PCMPLAY op 10 plays nothing
        await Script.CallFileAsync("COM.ADV");                          // :3
        if (State.StoryStage != 0)                                      // :4-13 pan to Leazas Castle
        {
            State.Prov_Cur = 1;
            State.MapTargetX = Prov.MapX;
            State.MapTargetY = Prov.MapY;
            await Script.MapCallAsync(1);
            await Script.MapCallAsync(10);
            await Script.MapCallAsync(State.CallArg);                   // :12 again with the CallArg op 10 left (0)
        }
        State.MapCamX = State.MapTargetX;                               // :14-17
        State.MapCamY = State.MapTargetY;
        await Script.MapCallAsync(0);
        await BuildTickAsync();                                         // :18-23
        if (State.DebugMode == 1)                                       // :24-37 debug: three weeks a week
        {
            await BuildTickAsync();
            await BuildTickAsync();
        }
        State.Ui_CmdHaremAvail = 1;                                     // :38-46
        State.Ui_CmdSubordAvail = 1;
        State.Ui_CmdOwnCityAvail = 1;
        State.Ui_CmdBuildAvail = 1;
        State.Ui_CmdAttackAvail = 1;
        State.Ui_CmdMerchantAvail = 1;
        State.Ui_CmdDungeonAvail = 1;
        State.CmdAvail_SubTroops = 1;
        State.CmdAvail_SubTalk = 1;
    }

    /// <summary><c>COMMAND.ADV:18-23</c> — one week of the construction timer; at 0 the building is finished.</summary>
    private async Task BuildTickAsync()
    {
        if (State.BuildWeeksLeft == 0)
            return;
        State.BuildWeeksLeft = State.BuildWeeksLeft - 1;
        if (State.BuildWeeksLeft == 0)
            await BuildingFinishedAsync();
    }

    /// <summary>
    /// <c>L_00c21</c> (<c>COMMAND.ADV:578-614</c>) — the first building under construction (state 2)
    /// is finished and its scene plays; with none, the label falls through into <c>L_00ced</c>.
    /// </summary>
    private async Task BuildingFinishedAsync()
    {
        // QUIRK: STR-09 — only the first building in this order is finished.
        if (State.Bld_GirlsAcademy == 2)
        {
            State.Bld_GirlsAcademy = 1;
            await Script.EventAsync("Gﾏﾘｽ.ADV", 4);
            return;
        }
        if (State.Bld_Library == 2)
        {
            State.Bld_Library = 1;
            await Script.EventAsync("Gﾏﾘｽ.ADV", 5);
            return;
        }
        if (State.Bld_Hospital == 2)
        {
            State.Bld_Hospital = 1;
            await Script.EventAsync("Gｱｰﾔ.ADV", 2);
            return;
        }
        if (State.Bld_SMTower == 2)
        {
            State.Bld_SMTower = 1;
            await Script.EventAsync("Gﾏﾘｽ.ADV", 6);
            return;
        }
        if (State.Bld_MariaLab == 2)
        {
            State.Bld_MariaLab = 1;
            await Script.EventAsync("Gﾏﾘｽ.ADV", 7);
            return;
        }
        if (State.Bld_MagicLab == 2)
        {
            State.Bld_MagicLab = 1;
            await Script.EventAsync("Gﾏﾘｽ.ADV", 8);
            return;
        }
        // QUIRK: STR-21 — with no building at state 2 the label runs on into L_00ced.
        await SavePointerAsync();
    }

    // ---------------------------------------------------------------------------------------
    // The map
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_0012e</c>-<c>L_0041a</c> (<c>COMMAND.ADV:49-172</c>): waits on the map. A left click
    /// (press and release at the same point) is hit-tested against the provinces, then the
    /// dungeons; a press that moves becomes a drag-scroll; a right click opens the menu. Returns
    /// true for a province hit (<c>Prov_Cur</c> = the province) and false for the menu; a dungeon
    /// hit runs <c>Cﾒｲｷｭｳ</c> and starts the map again from <c>L_00129</c>.
    /// </summary>
    private async Task<bool> MapAsync()
    {
    Wait:                                                               // L_0012e
        await Script.ShowAsync(new MapScreenEvent(true));
        await WaitForReleaseAsync();                                    // \L_003fe
        State.MapCamX = State.MapCamX - 320;                            // the camera as the view's corner
        State.MapCamY = State.MapCamY - 200;
        if (State.MapCamX > 1280)
            State.MapCamX = 1280;
        if (State.MapCamY > 400)
            State.MapCamY = 400;
    Drag:                                                               // L_00169
        await Script.WaoMapCallAsync(1);
        while (true)                                                    // L_00171
        {
            var m = await MouseAsync();
            State.Tmp0021 = m.X;
            State.Tmp0022 = m.Y;
            if (State.RetVal == 16)
                break;
            if (State.RetVal == 32)
            {
                State.MapCamX = State.MapCamX + 320;
                State.MapCamY = State.MapCamY + 200;
                await WaitForReleaseAsync();                            // \L_003fe
                do                                                      // L_0041a (:168-172)
                {
                    m = await MouseAsync();
                    State.Tmp0021 = m.X;
                    State.Tmp0022 = m.Y;
                }
                while (State.RetVal != 0);
                await Script.ShowAsync(new MapScreenEvent(false));
                return false;
            }
        }

        State.Tmp0029 = 999;                                            // L_001b5
        while (true)                                                    // L_001ba: until the button is released
        {
            var m = await MouseAsync();
            State.Tmp0023 = m.X;
            State.Tmp0024 = m.Y;
            if (State.RetVal != 16)
                break;
            if (State.Tmp0029 == 999)
            {
                var shape = await Input.SetCursorAsync(6);
                State.Tmp0029 = shape;
            }
            if (State.Tmp0021 != State.Tmp0023 || State.Tmp0022 != State.Tmp0024)
            {
                var shape = await Input.SetCursorAsync(State.Tmp0029);
                State.RetVal = shape;
                State.Ui_DragX = State.Tmp0021;
                State.Ui_DragY = State.Tmp0022;
                State.Ui_DragPending = 1;
                goto Drag;
            }
        }
        if (State.Tmp0029 != 999)                                       // :91-93
        {
            var shape = await Input.SetCursorAsync(State.Tmp0029);
            State.RetVal = shape;
        }
        State.Tmp0025 = State.MapCamX + State.Tmp0021;                  // :94-98 the pressed point on the map
        State.Tmp0026 = State.MapCamY + State.Tmp0022;
        State.MapCamX = State.MapCamX + 320;
        State.MapCamY = State.MapCamY + 200;
        await WaitForReleaseAsync();

        // QUIRK: STR-12 — both hit tests start at record 0.
        State.Prov_Cur = 0;                                             // :99-144
        while (true)                                                    // L_0024d
        {
            State.Tmp0030 = HitRadius(State.Prov_Cur);
            if (Hits(Prov.MapX, Prov.MapY))
            {
                await Script.ShowAsync(new MapScreenEvent(false));
                return true;                                            // L_003f9 → L_00a95
            }
            if (State.Prov_Cur >= State.Prov_Count)
                break;
            State.Prov_Cur = State.Prov_Cur + 1;
        }
        State.Dgn_Cur = 0;                                              // :145-158
        while (true)                                                    // L_0038a
        {
            State.Tmp0030 = 10;
            if (Hits(Dgn.MapX, Dgn.MapY))
            {
                // QUIRK: STR-19 — a dungeon the player cannot enter still moves Prov_Cur.
                State.Prov_Cur = Dgn.Prov;
                if (Prov.Owner == 1 || Dgn.Prov == 0)
                {
                    await Script.ShowAsync(new MapScreenEvent(false));
                    await Script.CallFileAsync("Cﾒｲｷｭｳ.ADV");
                    await Script.MusicAsync(State.CurrentBgm);          // @L_00129
                    goto Wait;
                }
            }
            if (State.Dgn_Cur >= State.Dgn_Count)
                break;
            State.Dgn_Cur = State.Dgn_Cur + 1;
        }
        goto Wait;                                                      // :159 @L_0012e
    }

    /// <summary><c>COMMAND.ADV:101-137</c>: the half-size of a province's click box (12, larger for the big ones).</summary>
    private static Sys3Int HitRadius(Sys3Int province) => province.Value switch
    {
        1 or 56 or 69 or 78 or 79 => 30,
        25 => 36,
        27 => 20,
        45 or 81 or 84 or 87 or 88 => 32,
        _ => 12,
    };

    /// <summary>
    /// <c>COMMAND.ADV:138, 148</c>: the pressed map point (<c>Tmp0025</c>, <c>Tmp0026</c>) lies strictly
    /// inside the box of half-size <c>Tmp0030</c> around (<paramref name="x"/>, <paramref name="y"/>);
    /// the subtraction saturates at 0, as the script's does.
    /// </summary>
    private bool Hits(Sys3Int x, Sys3Int y) =>
        State.Tmp0025 > x - State.Tmp0030 && State.Tmp0026 > y - State.Tmp0030
        && State.Tmp0025 < x + State.Tmp0030 && State.Tmp0026 < y + State.Tmp0030;

    /// <summary><c>IM x, y:</c> — the button into <c>RetVal</c>; the caller stores the position.</summary>
    private async Task<MouseState> MouseAsync()
    {
        var m = await Input.GetMouseAsync();
        State.RetVal = m.Button;
        return m;
    }

    /// <summary><c>L_00ced</c> (<c>COMMAND.ADV:615-617</c>): <c>IM Tmp0019, Tmp0020</c>, where a cancelled menu puts the pointer back.</summary>
    private async Task SavePointerAsync()
    {
        var m = await MouseAsync();
        State.Tmp0019 = m.X;
        State.Tmp0020 = m.Y;
    }

    /// <summary><c>L_003fe</c> (<c>COMMAND.ADV:162-167</c>): <c>IM Tmp0021, Tmp0022</c> until no button is down.</summary>
    private async Task WaitForReleaseAsync()
    {
        do
        {
            var m = await MouseAsync();
            State.Tmp0021 = m.X;
            State.Tmp0022 = m.Y;
        }
        while (State.RetVal != 0);
    }

    /// <summary>
    /// <c>L_00a95</c> (<c>COMMAND.ADV:514-577</c>) — the clicked province (<c>Prov_Cur</c>) goes to its
    /// command: in the prologue Golak / Vladivostok to <c>Cﾃｷﾄｼ</c> (which may end the phase), any
    /// other to <c>Cﾃｷﾄｼ2</c>; later an own province to <c>Cｼﾊｲﾄｼ</c>, one next to an own province
    /// to <c>Cﾃｷﾄｼ</c>, any other to <c>Cﾃｷﾄｼ2</c>.
    /// </summary>
    private async Task<Next> ProvinceClickedAsync()
    {
        State.Ui_ClickedProv = State.Prov_Cur;
        if (State.StoryStage == 0)
        {
            if (State.Prov_Cur == 34 || State.Prov_Cur == 33)
            {
                await Script.CallFileAsync("Cﾃｷﾄｼ.ADV");
                return State.StoryStage == 99 ? Next.End : Next.Map;
            }
            await Script.CallFileAsync("Cﾃｷﾄｼ2.ADV");
            return Next.Map;
        }
        if (Prov.Owner == 1)
        {
            await Script.CallFileAsync("Cｼﾊｲﾄｼ.ADV");
            return Next.Map;
        }
        // :531-574 — Adj1..Adj5 of the clicked province, each read with the cursor back on it.
        for (int k = 1; k <= 5; k++)
        {
            var neighbour = Adjacent(Prov, k);
            if (neighbour != 0)
            {
                State.Prov_Cur = neighbour;
                if (Prov.Owner == 1)
                {
                    State.Prov_Cur = State.Ui_ClickedProv;
                    await Script.CallFileAsync("Cﾃｷﾄｼ.ADV");
                    return Next.Map;
                }
            }
            State.Prov_Cur = State.Ui_ClickedProv;
        }
        await Script.CallFileAsync("Cﾃｷﾄｼ2.ADV");
        return Next.Map;
    }

    private static Sys3Int Adjacent(ProvRecord prov, int k) => k switch
    {
        1 => prov.Adj1,
        2 => prov.Adj2,
        3 => prov.Adj3,
        4 => prov.Adj4,
        _ => prov.Adj5,
    };

    // ---------------------------------------------------------------------------------------
    // The command menu
    // ---------------------------------------------------------------------------------------

    /// <summary>The items' targets.</summary>
    private enum Command
    {
        Unavailable,        // L_006eb
        Think,              // L_006f5
        Debug,              // L_00708
        Construction,       // L_0071b
        Harem,              // L_00735
        Treasure,           // L_0074f
        Merchant,           // L_00761
        Subordinates,       // L_00774
        AllData,            // L_007a2
        LoadSave,           // L_007b4
        TurnEnd,            // L_007cf
        ToHideout,          // L_00a33
        ToCastle,           // L_00a64
    }

    /// <summary>
    /// <c>L_00431</c> (<c>COMMAND.ADV:173-316</c>) — the status window and the command menu of the
    /// thieves (stage 0) or of Leazas; a cancel closes the window and goes back to the map.
    /// </summary>
    private async Task<Next> CommandMenuAsync()
    {
        await SavePointerAsync();                                       // \L_00ced: the pointer, for the cancel
        if (State.Ev_CommandMode == 1)                                  // :175-179 the forced Cafe trip
        {
            await Script.EventAsync("Gｶﾌｪ.ADV", 30);
            return Next.Map;
        }
        if (State.Ev_CommandMode == 2)                                  // :180-182 the map only
            return Next.Map;

        var menu = new List<(SceneMenuItem Item, Command Command)>();
        void Add(int itemLine, Command command) =>
            menu.Add((new SceneMenuItem(Ref(itemLine), [new CapturedText(Ref(itemLine + 1), [], [])]), command));

        if (State.StoryStage == 0)                                      // :183-221
        {
            await ThievesStatusAsync();
            if (State.Ui_CmdHaremAvail != 0)
                Add(187, Command.Harem);                                // ○Harem
            if (State.Ui_CmdHaremAvail == 0)
                Add(193, Command.Unavailable);                          // ×Harem
            if (State.Ui_CmdSubordAvail != 0)
                Add(199, Command.Subordinates);                         // ○Subordinates
            if (State.Ui_CmdSubordAvail == 0)
                Add(205, Command.Subordinates);                         // △Subordinates
            Add(210, Command.ToHideout);
            await LineBreakAsync();
            Add(214, Command.LoadSave);
            await LineBreakAsync();
            Add(218, Command.TurnEnd);
        }
        else                                                            // :226-313
        {
            await LeazasStatusAsync();
            if (State.Ui_CmdBuildAvail != 0)
                Add(229, Command.Construction);                         // ○Construction
            if (State.Ui_CmdBuildAvail == 0)
                Add(235, Command.Unavailable);                          // ×Construction
            if (State.Ui_CmdHaremAvail != 0)
                Add(241, Command.Harem);
            if (State.Ui_CmdHaremAvail == 0)
                Add(247, Command.Unavailable);
            Add(252, Command.Treasure);
            if (State.Ui_CmdMerchantAvail != 0)
                Add(257, Command.Merchant);                             // ○Merchant
            if (State.Ui_CmdMerchantAvail == 0)
                Add(263, Command.Unavailable);                          // △Merchant blocks
            if (State.Ui_CmdSubordAvail != 0)
            {
                // QUIRK: STR-20 — with the flag set but neither sub-flag, no Subordinates item at all.
                var left = State.CmdAvail_SubTroops + State.CmdAvail_SubTalk;
                if (left == 2)
                    Add(270, Command.Subordinates);                     // ◎Subordinates
                if (left == 1)
                    Add(276, Command.Subordinates);                     // ○Subordinates
            }
            if (State.Ui_CmdSubordAvail == 0)
                Add(283, Command.Subordinates);                         // △Subordinates still opens Cﾌﾞｶ
            Add(288, Command.ToCastle);
            await LineBreakAsync();
            Add(292, Command.LoadSave);
            await LineBreakAsync();
            Add(296, Command.AllData);
            await LineBreakAsync();
            Add(300, Command.Think);
            if (State.DebugMode == 1)
                Add(305, Command.Debug);
            Add(310, Command.TurnEnd);
        }

        int choice = await Input.SelectAsync(menu.Select(i => i.Item).ToArray());   // ]
        if (choice < 0 || choice > menu.Count)
            throw new InvalidOperationException($"{File}: choice {choice} of a menu with {menu.Count} items.");
        await Script.ShowAsync(new MessageClearEvent());
        if (choice == 0)                                                // right click: back to the map
        {
            await Script.WaoAsync(0);                                   // \L_00e93
            await Script.ShowAsync(new EngineEvent("IZ", [State.Tmp0019, State.Tmp0020])); // \L_00cf8
            return Next.Map;
        }
        return await RunCommandAsync(menu[choice - 1].Command);
    }

    /// <summary>The menu items' code (<c>COMMAND.ADV:317-513</c>).</summary>
    private async Task<Next> RunCommandAsync(Command command)
    {
        await Script.WaoAsync(0);                                       // \L_00e93: every item closes the window
        switch (command)
        {
            case Command.Unavailable:                                   // L_006eb: the menu again
                return Next.Menu;
            case Command.Think:
                await MenuSoundAsync();
                await Script.CallFileAsync("Iｶﾝｶﾞｴ.ADV");
                return Next.Map;
            case Command.Debug:
                await MenuSoundAsync();
                await Script.CallFileAsync("TEST.ADV");
                return Next.Map;
            case Command.Construction:
                await MenuSoundAsync();
                await Script.CallFileAsync("Cﾃｲﾄ.ADV");
                await Script.MapCallAsync(0);
                return Next.Map;
            case Command.Harem:
                await MenuSoundAsync();
                await Script.CallFileAsync("Cﾊｰﾚﾑ.ADV");
                await Script.MapCallAsync(0);
                return Next.Map;
            case Command.Treasure:
                await MenuSoundAsync();
                await Script.CallFileAsync("Cﾀｶﾗ.ADV");
                return Next.Map;
            case Command.Merchant:
                await MenuSoundAsync();
                await Script.CallFileAsync("Cｼｮｳﾆﾝ.ADV");
                return Next.Map;
            case Command.Subordinates:
                await MenuSoundAsync();
                await Script.CallFileAsync("Cﾌﾞｶ.ADV");
                await Script.MapCallAsync(0);
                return Next.Map;
            case Command.AllData:
                await MenuSoundAsync();
                await Script.CallFileAsync("各種DATA.ADV");
                return Next.Map;
            case Command.LoadSave:
                await MenuSoundAsync();
                State.RetVal = 2;
                await Script.CallFileAsync("ﾛｰﾄﾞｾｰﾌﾞ.ADV");
                SetDate();
                return Next.Map;
            case Command.TurnEnd:
                await MenuSoundAsync();
                await TurnEndAsync();
                return Next.End;
            case Command.ToHideout:                                     // L_00a33 (:490-501)
                State.Dgn_Cur = 1;
                State.MapTargetX = Dgn.MapX;
                State.MapTargetY = Dgn.MapY;
                await PanToTargetAsync();
                return Next.Map;
            case Command.ToCastle:                                      // L_00a64 (:502-513)
                State.Prov_Cur = 1;
                State.MapTargetX = Prov.MapX;
                State.MapTargetY = Prov.MapY;
                await PanToTargetAsync();
                return Next.Map;
            default:
                throw new InvalidOperationException($"No command {command}.");
        }
    }

    /// <summary><c>COMMAND.ADV:495-500</c>: scroll to the target, then blink it.</summary>
    private async Task PanToTargetAsync()
    {
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        await Script.MapCallAsync(10);
    }

    /// <summary><c>L_00d68</c> (<c>COMMAND.ADV:638-642</c>): the menu's confirmation sound (<c>PCMPLAY</c> 0, sound 108).</summary>
    private Task MenuSoundAsync()
    {
        State.CallArg = 0;
        State.RetVal = 108;
        return Script.SoundAsync(0);
    }

    /// <summary>
    /// <c>L_007cf</c> (<c>COMMAND.ADV:377-425</c>) — Turn END: Sill's grave scene, the Tange Dog
    /// (once a week it stands up its holder if a battle left them at status 2), and in the
    /// prologue after week 22 the thieves' last fight and the opening.
    /// </summary>
    private async Task TurnEndAsync()
    {
        if (State.F_Sill_Died == 1)
            await Script.EventAsync("Gｼｨﾙ2.ADV", 5);
        State.Item_Cur = 34;
        if (Item.Holder != 100 && Item.Holder != 0)
        {
            State.Unit_Cur = Item.Holder;
            Script.CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
            if (Unit.Status == 2 && State.TangeDog_UsedThisWeek == 0)
            {
                State.TangeDog_UsedThisWeek = 1;
                Unit.Status = 1;
                await Script.WaoAsync(312);                             // \L_00ddd
                await Script.WaoAsync(322);                             // \L_00e4d
                State.CallArg = Unit.FaceCg;
                await Script.WaoAsync(311);                             // \L_00dcf
                await Script.WaoAsync(121);                             // \L_00deb
                await Script.TextAsync(File, 397);                      // "(Tange Dog) Stand... Stand up... <name>!"
                await Script.WaoAsync(1);                               // \L_00ea0
                await Script.WaoAsync(0);
                await Script.WaoAsync(113);                             // \L_00db3
                await Script.TextAsync(File, 407);                      // "Thanks to the Tange Dog's love, <name> is able to move again."
                await Script.WaoAsync(1);
                await Script.WaoAsync(0);
                await Script.WaoAsync(312);
            }
        }
        if (State.StoryStage == 0 && State.Week > 22)                   // :419-424 the end of the prologue
        {
            await Script.EventAsync("Iﾄｳｿﾞｸ.ADV", 2);
            await Script.CallFileAsync("ｵｰﾌﾟﾆﾝｸﾞ.ADV");
            State.StoryStage = 99;
        }
    }

    // ---------------------------------------------------------------------------------------
    // The status window
    // ---------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_0099a</c> (<c>COMMAND.ADV:461-489</c>) — "Rance's Thieves": money, the troops of units
    /// 1, 3 and 4, month and week.
    /// </summary>
    private async Task ThievesStatusAsync()
    {
        SetDate();
        State.Tmp0001 = 0;
        State.Unit_Cur = 1;
        State.Tmp0001 = State.Tmp0001 + Unit.Troops;
        State.Unit_Cur = 3;
        State.Tmp0001 = State.Tmp0001 + Unit.Troops;
        State.Unit_Cur = 4;
        State.Tmp0001 = State.Tmp0001 + Unit.Troops;
        await Script.WaoAsync(113);
        await Script.ShowAsync(new ValueMessageEvent(Ref(472), [State.Gold, State.Tmp0001, State.DateMonth, State.DateWeek]));
    }

    /// <summary>
    /// <c>L_008e4</c> (<c>COMMAND.ADV:426-460</c>) — "Leazas": budget, the troops of every unit
    /// 1-50 with a status, month and week; leaves <c>Unit_Cur</c> at 51.
    /// </summary>
    private async Task LeazasStatusAsync()
    {
        SetDate();
        State.Tmp0001 = 0;
        State.Unit_Cur = 0;
        while (true)
        {
            State.Unit_Cur = State.Unit_Cur + 1;
            if (State.Unit_Cur == 51)
                break;
            if (Unit.Status == 0)
                continue;
            State.Tmp0001 = State.Tmp0001 + Unit.Troops;
        }
        await Script.WaoAsync(113);
        await Script.ShowAsync(new ValueMessageEvent(Ref(443), [State.Gold, State.Tmp0001, State.DateMonth, State.DateWeek]));
    }

    /// <summary>
    /// <c>L_00d03</c> (<c>COMMAND.ADV:621-637</c>) — <c>DateYear/Month/Week</c> from <c>Week</c>
    /// (48 weeks a year from year 3, 4 weeks a month), through <c>Tmp0001-0005</c>.
    /// </summary>
    private void SetDate()
    {
        State.Tmp0005 = State.Week;
        State.Tmp0004 = (State.Tmp0005 - 1) / 48;
        State.Tmp0001 = (Sys3Int)3 + State.Tmp0004;
        if (State.Tmp0004 != 0)
            State.Tmp0005 = State.Tmp0005 - State.Tmp0004 * 48;
        State.Tmp0004 = (State.Tmp0005 - 1) / 4;
        State.Tmp0002 = State.Tmp0004 + 1;
        if (State.Tmp0004 != 0)
            State.Tmp0005 = State.Tmp0005 - State.Tmp0004 * 4;
        State.Tmp0003 = State.Tmp0005;
        State.DateYear = State.Tmp0001;
        State.DateMonth = State.Tmp0002;
        State.DateWeek = State.Tmp0003;
    }

    /// <summary><c>R</c> between menu items.</summary>
    private Task LineBreakAsync() => Script.ShowAsync(new LineBreakEvent());

    private static ScriptRef Ref(int line) => new(File, line);
}
