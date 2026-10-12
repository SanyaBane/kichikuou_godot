using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;
using Kichikuou.Original.State;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Cﾒｲｷｭｳ.ADV</c>, a dungeon clicked on the map (<c>docs/strategy/survey-dungeon.md</c>): the
/// entry checks and resets, the camera, the week's dungeon action spent, the dungeon's name
/// (string 8, from the player's files) and background, the Cafe trip's takeover (<c>:1-166</c>);
/// the dispatch (<c>L_005b6</c>: the hideout, the story-only spots, the pre-scenes), the dungeon's
/// menu (<c>L_0076a</c>), the searcher picked through <c>Sﾌﾞﾀｲ</c> and the refusals, the way there
/// (blizzards, arrival scenes, the dungeon's parameters, the resume floor) and the floor-by-floor
/// search: the fixed floors (treasure chests, story scenes, the Uncurse Spring, KD's quiz, Orochi),
/// the moves with their encounter rolls, the battles (monsters, Hanny tiers, the bosses, whose
/// setters are read from the player's files), Merim's inscriptions and the end of the search.
/// God's Door (<c>L_04ddc</c>, <c>L_05437</c>, then <c>Iｶﾐ</c>) is in <c>DungeonCommand.GodsDoor.cs</c>.
/// </summary>
public sealed partial class DungeonCommand(OriginalScriptContext script, IScenarioSource scenario) : CommandFile(script, "Cﾒｲｷｭｳ.ADV", scenario)
{
    /// <summary>
    /// <c>:29-150</c>: each dungeon's <c>MS 8, "name"</c> line (<c>MS 9</c> for 9, never printed —
    /// DGN-15) and its background if it has its own (0: the default 1032 of <c>:28</c>).
    /// </summary>
    private static readonly (int Dungeon, int NameLine, int Background)[] Names =
    [
        (1, 31, 1008), (2, 36, 0), (3, 40, 0), (4, 44, 0), (5, 48, 1017), (6, 57, 0), (7, 61, 1029),
        (8, 66, 0), (9, 70, 1037), (10, 75, 0), (11, 79, 1025), (12, 84, 0), (13, 88, 0), (14, 92, 0),
        (15, 96, 0), (16, 100, 1016), (17, 105, 0), (18, 109, 0), (19, 113, 0), (20, 117, 0),
        (21, 121, 0), (22, 125, 0), (23, 129, 0), (24, 133, 0), (25, 137, 0), (26, 145, 0), (30, 149, 0),
    ];

    /// <summary>
    /// <c>:477-612</c>, the search's parameters per dungeon: the last floor, the floor mode (1 a pit,
    /// 2 a tower), the battle's troop limit, the monsters' commander and their power.
    /// </summary>
    private static readonly (int Dungeon, int MaxFloor, int Mode, int TroopLimit, int Monster, int Power)[] Parameters =
    [
        (2, 14, 1, 80, 83, 40), (4, 12, 1, 100, 83, 200), (6, 25, 1, 150, 84, 300), (8, 30, 1, 200, 85, 300),
        (10, 28, 1, 400, 83, 600), (11, 30, 2, 500, 83, 300), (14, 38, 1, 400, 83, 600), (15, 38, 1, 270, 84, 320),
        (16, 20, 1, 500, 100, 800), (18, 27, 1, 1000, 83, 1000), (19, 27, 1, 1000, 83, 1000), (20, 50, 1, 1000, 83, 1000),
        (21, 50, 1, 1000, 85, 1000), (22, 50, 1, 1000, 83, 1000), (23, 76, 1, 500, 85, 350), (24, 50, 1, 750, 84, 380),
        (25, 66, 1, 600, 100, 1000), (26, 201, 2, 600, 83, 100), (30, 1000, 1, 600, 83, 250),
    ];

    /// <summary>
    /// The bosses in the order the moves (<c>:1020-1064</c>) and the battle (<c>:1197-1271</c>) test
    /// them: the dungeon, the floor, the flag that must still be 0 (none for Okayu Fever) and the
    /// first line of the setter that writes commander record 100 (read from the player's files).
    /// </summary>
    private static readonly (int Dungeon, int Floor, Func<GameState, Sys3Int>? Flag, int Setter)[] Bosses =
    [
        (6, 10, null, 2873),                                            // L_03f14 Okayu Fever
        (11, 5, s => s.F_YellowDragon_Beaten, 3222),                    // L_04727
        (11, 10, s => s.F_RedDragon_Beaten, 3254),                      // L_047e7
        (11, 15, s => s.F_GreenDragon_Beaten, 3286),                    // L_048a4
        (11, 20, s => s.F_BlueDragon_Beaten, 3318),                     // L_04964
        (11, 25, s => s.F_BlackDragon_Beaten, 3350),                    // L_04a23
        (30, 1000, s => s.F_Gunagan_State, 3190),                       // L_0466c
        (8, 10, s => s.F_Boss_StoneGuardian, 3030),                     // L_042b5
        (2, 5, s => s.F_Boss_Marder, 3062),                             // L_04376
        (10, 10, s => s.F_Boss_HawKyu, 3094),                           // L_0442f
        (14, 12, s => s.F_Boss_Musashibou, 3126),                       // L_044ea
        (30, 100, s => s.F_Boss_JapaneseSoldier, 3158),                 // L_045a7
        (20, 10, s => s.F_Boss_ShimoJoe, 2934),                         // L_0407d
        (21, 15, s => s.F_Boss_Vevetar, 2966),                          // L_04139
        (22, 13, s => s.F_Boss_GGCaterpillar, 2998),                    // L_041f4
    ];

    /// <summary>Where the search goes on after a step of the floor loop (the script's labels).</summary>
    private enum Step
    {
        /// <summary><c>L_01137</c>: the floor's header, then its fixed event.</summary>
        Arrive,

        /// <summary><c>L_0177d</c>: the header again, then the menu.</summary>
        Header,

        /// <summary><c>L_016b4</c>: the floor menu.</summary>
        Menu,

        /// <summary><c>L_018cd</c>: the end of the search.</summary>
        End,

        /// <summary><c>%0:</c> — the searcher fell, or a scene ended the visit.</summary>
        Exit,
    }

    /// <summary><c>Cﾒｲｷｭｳ.ADV:1-237</c>: the entry, then the dispatch.</summary>
    public async Task RunAsync()
    {
        if (State.Ev_CommandMode == 2)                                  // :1-3 the forced Maria attack
            return;
        if (State.Ui_CmdDungeonAvail == 0 && State.StoryStage != 0)
        {
            await AlreadySearchedAsync();                               // :4-6 @L_07418
            return;
        }
        State.DgnRun_MerimInParty = 0;                                  // :7-16
        if (State.F_Merim_MillArc == 3)
            State.F_Merim_MillArc = 4;
        // QUIRK: DGN-01 — whatever commander record the cursor was left on.
        if (Cmdr.CgFull == 1137)
            State.F_Gunagan_State = 2;
        if (State.Dgn_Cur == 9 && State.F_PG9_State != 2)
            return;
        State.MapTargetX = Dgn.MapX;                                    // :17-24 the camera on the dungeon
        State.MapTargetY = Dgn.MapY;
        await Script.MapCallAsync(1);
        State.MapCamX = State.MapTargetX;
        State.MapCamY = State.MapTargetY;
        await Script.MapCallAsync(10);
        // QUIRK: DGN-14 — the week's dungeon action is spent before anything is known.
        State.Ui_CmdDungeonAvail = 0;                                   // :25
        SetLiteral(27);                                                 // MS 10, "Monsters"
        State.DungeonBgCg = 1032;
        foreach (var (dungeon, nameLine, background) in Names)          // :29-150, each test on the cursor as it is then
        {
            if (State.Dgn_Cur != (Sys3Int)dungeon)
                continue;
            SetLiteral(nameLine);
            if (background != 0)
                State.DungeonBgCg = (Sys3Int)background;
            if (dungeon == 5 && Dgn.State == 0)
                await Script.EventAsync("Gﾎﾗｶﾞ.ADV", 2);
            if (dungeon == 25 && State.F_HannyKing_State > 1)
                SetLiteral(140);                                        // "Haniwa Temple"
        }
        if (State.Ev_CommandMode == 1 && State.Dgn_Cur == 10)          // :151-161 the Cafe trip's search
        {
            await Script.EventAsync("Gｶﾌｪ.ADV", 32);
            // QUIRK: DGN-13 — Rance alone, without the picker: free, and not spent for the week.
            State.Unit_Cur = 1;
            CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
            State.Bt_PlayerParty1 = 1;
            State.Bt_PlayerParty2 = 0;
            State.Bt_PlayerParty3 = 0;
            State.Bt_PlayerParty4 = 0;
            await SearchAsync();                                        // @L_00a2c
            return;
        }
        if (State.Ev_CommandMode == 1)                                  // :162-166
        {
            await Script.EventAsync("Gｶﾌｪ.ADV", 31);
            return;
        }

        while (true)
        {
            // L_005b6 (:167-237), entered again when the picker is left empty or the searcher refuses.
            if (State.Dgn_Cur == 1)
            {
                State.Ui_CmdDungeonAvail = 1;                           // the hideout costs nothing
                await HideoutAsync();
                return;
            }
            if (State.StoryStage == 0)
                return;
            // QUIRK: DGN-15 — :175-181 repeat the two tests above and are never reached.
            if (State.Dgn_Cur == 3)
            {
                await Script.EventAsync("Gﾌﾞﾘﾃｨ.ADV", 1);
                return;
            }
            if (State.Dgn_Cur == 7)
            {
                await Script.EventAsync("Gｷｬﾛﾘ.ADV", 1);
                return;
            }
            if (State.Dgn_Cur == 9)
            {
                if (State.F_PG9_State != 2)
                    return;
                await Script.EventAsync("Gﾊﾟｲｱｰﾙ.ADV", 7);
                return;
            }
            if (State.Dgn_Cur == 12)
            {
                await Script.EventAsync("Gｶﾗｰ.ADV", 1);
                return;
            }
            if (State.Dgn_Cur == 13)
            {
                await Script.EventAsync("Gﾄｳｹﾞﾝ.ADV", 1);
                return;
            }
            if (State.Dgn_Cur == 15 && State.F_Arcy_State == 0)
                await Script.EventAsync("G1.ADV", 2);
            if (State.Dgn_Cur == 15 && State.DateWeek != 1)
            {
                await Script.EventAsync("G1.ADV", 3);
                return;
            }
            if (State.Dgn_Cur == 15 && State.DateWeek == 1)
                await Script.EventAsync("G1.ADV", 4);
            if (State.Dgn_Cur == 16 && State.F_Battleship_Door == 0)
                await Script.EventAsync("G1.ADV", 10);
            if (State.Dgn_Cur == 17)
            {
                await Script.EventAsync("G1.ADV", 15);
                return;
            }
            State.Unit_Cur = 1;                                         // :232-237
            if (State.Dgn_Cur == 30 && State.F_GodsDoor_Quest > 1 && State.F_GodsDoor_Found == 0
                && (Unit.Item == 1 || Unit.Item == 2))
            {
                await GodsDoorFoundAsync();                             // \L_04ddc
                await Script.MapCallAsync(0);
            }

            if (!await MainMenuAsync())
                return;
            // QUIRK: DGN-06 — an empty picker or a refusal goes back to the dispatch and its pre-scenes.
            if (!await PickSearcherAsync())
                continue;
            await SearchAsync();
            return;
        }
    }

    /// <summary>
    /// <c>L_0076a</c> (<c>:238-305</c>): the dungeon's name and state, then its menu — Start Search,
    /// Bring or Leave Merim, Bring Pink Cow, God's Door, Return. Merim and the cow come back to the
    /// menu. True for Start Search; false when the player left (<c>L_008bf</c>: the week's action
    /// given back).
    /// </summary>
    private async Task<bool> MainMenuAsync()
    {
        while (true)
        {
            State.CallArg = State.DungeonBgCg;
            await WaoAsync(331);                                        // \L_0766d
            await WaoAsync(113);                                        // \L_075a9
            await TextAsync(243);                                       // ［name］
            if (Dgn.State == 0)
                await TextAsync(250);                                   // (Unsearched)
            if (Dgn.State == 1)
                await TextAsync(254);
            if (Dgn.State == 2)
                await TextAsync(258);
            await WaitForReleaseAsync();                                // L_007df
            await SavePointerAsync();                                   // \L_0755b
            var items = new List<SceneMenuItem>();
            var labels = new List<int>();
            Offer(267);                                                 // Start Search
            if (State.F_Merim_Dungeon == 1 && State.DgnRun_MerimInParty == 0)
                Offer(272);                                             // Bring Merim
            if (State.F_Merim_Dungeon == 1 && State.DgnRun_MerimInParty == 1)
                Offer(278);                                             // Leave Merim
            if (State.Dgn_Cur == 10 && Dgn.State == 2 && State.F_SillCow_State == 7)
                Offer(284);                                             // Bring Pink Cow
            if (State.Dgn_Cur == 30 && State.F_GodsDoor_Found != 0)
                Offer(290);                                             // God's Door
            Offer(295);                                                 // Return
            int choice = await SelectAsync(items);
            switch (choice == 0 ? 0 : labels[choice - 1])
            {
                case 267:
                    return true;                                        // L_008d1
                case 272:
                    await MerimJoinsAsync();
                    continue;
                case 278:
                    await MerimLeavesAsync();
                    continue;
                case 284:
                    await PinkCowAsync();
                    continue;
                case 290:
                    await GodsDoorAsync();                              // @L_05437, back by @L_0076a
                    continue;
                case 0:
                    await RestorePointerAsync();                        // \L_07566, @L_008bf
                    break;
            }
            State.Ui_CmdDungeonAvail = 1;                               // L_008bf
            await WaoAsync(0);
            await WaoAsync(332);
            return false;

            void Offer(int line)
            {
                items.Add(MenuItem(line));
                labels.Add(line);
            }
        }
    }

    /// <summary>
    /// <c>L_008d1</c> (<c>:306-368</c>): the search's start — Rance's line, Maris's advice on the
    /// snow (dungeons 4, 5, 16), the picker with one unit for the dungeon (<c>Sﾌﾞﾀｲ</c> mode 3), the
    /// searcher's name into string 1 and the refusals of units 29, 30, 32, 41 and 42. False when the
    /// picker was left empty or the searcher refused (back to the dispatch).
    /// </summary>
    private async Task<bool> PickSearcherAsync()
    {
        await WaoAsync(0);
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(111, 312);                                     // "Alright, time to search."
        State.CallArg = 6;
        await WaoAsync(321);
        if (State.Dgn_Cur == 4 || State.Dgn_Cur == 5 || State.Dgn_Cur == 16)
            await SnowAdviceAsync();                                    // \L_06fe4
        await SpeakAsync(121, 324);                                     // "…select the unit"
        State.Tmp0021 = 1;
        State.Tmp0022 = 3;
        await Script.CallFileAsync("Sﾌﾞﾀｲ.ADV");
        await Script.MapCallAsync(0);
        if (State.Bt_PlayerParty1 == 0)
        {
            await WaoAsync(322);
            return false;
        }
        State.Unit_Cur = State.Bt_PlayerParty1;
        CopyString(1, State.Unit_StrBase + (State.Unit_Cur - 1) * 3);
        Func<Task>? refusal = State.Unit_Cur.Value switch
        {
            29 => SatellaRefusesAsync,
            30 => MeglassRefusesAsync,
            32 => MikiRefusesAsync,
            41 => GaltiaRefusesAsync,
            42 => HawzelRefusesAsync,
            _ => null,
        };
        if (refusal is null)
            return true;
        // QUIRK: BFL-01 — the picker's charge and the week are kept.
        Unit.Status = 1;
        await refusal();
        return false;
    }

    /// <summary>
    /// <c>L_00a2c</c> (<c>:369-668</c>): the way there (the blizzards without the Snow Charm), the
    /// arrival and its scenes, the dungeon's parameters, the music, the Holy Gal's scene and the
    /// resume floor; then the floors.
    /// </summary>
    private async Task SearchAsync()
    {
        State.Item_Cur = 11;
        // QUIRK: DGN-09 — the Snow Charm protects wherever it is, equipped or in stock.
        if ((State.Dgn_Cur == 4 || State.Dgn_Cur == 16) && Item.Holder == 0)
        {
            await WaoAsync(113);
            await TextAsync(374);                                       // "…assailed by a blizzard…"
            await WaoAsync(1);
            await ValuesAsync(382, Unit.Troops);                        // "Of the n in the army, "
            Script.Rnd(Unit.Troops);
            Unit.Troops = Unit.Troops - State.RetVal;
            await ValuesAsync(388, State.RetVal);                       // "n were injured…"
            await WaoAsync(1);
            await WaoAsync(0);
        }
        if (State.Dgn_Cur == 5 && Item.Holder == 0)
        {
            await WaoAsync(113);
            await TextAsync(398);
            await WaoAsync(1);
            Script.Rnd(Unit.Troops);
            State.RetVal = Unit.Troops / 2 + State.RetVal;
            if (State.RetVal > Unit.Troops)
                State.RetVal = Unit.Troops;
            Unit.Troops = Unit.Troops - State.RetVal;
            await ValuesAsync(411, State.RetVal);                       // "n people were lost."
            await WaoAsync(1);
            await WaoAsync(0);
        }
        State.CallArg = 1032;                                           // :417-433 the arrival
        if (State.Dgn_Cur == 5)
            State.CallArg = 1017;
        if (State.Dgn_Cur == 16)
            State.CallArg = 1016;
        await WaoAsync(331);
        await WaoAsync(113);
        await TextAsync(427);                                           // "…’s unit arrived at …"
        await WaoAsync(1);
        await WaoAsync(0);
        if (State.Dgn_Cur == 5)
        {
            await Script.EventAsync("Gﾎﾗｶﾞ.ADV", 1);
            return;
        }
        State.Item_Cur = 45;
        // QUIRK: DGN-10 — an equipped key (holder 1-50) passes neither test.
        if (State.Dgn_Cur == 16 && Item.Holder == 0 && State.F_Battleship_Door == 1)
        {
            await Script.EventAsync("G1.ADV", 11);
            return;
        }
        if (State.Dgn_Cur == 16 && Item.Holder == 100 && State.F_Battleship_Door == 1)
            await Script.EventAsync("G1.ADV", 12);
        if (State.Dgn_Cur == 25 && (State.F_HannyKing_State == 0 || State.F_HannyKing_State == 1))
        {
            await Script.EventAsync("G1.ADV", 17);
            if (State.F_HannyKing_State == 0 || State.F_HannyKing_State == 1)
                return;
        }
        await WaoAsync(4);                                              // :456-468
        if (State.DungeonBgCg == 1032)
            State.DungeonBgCg = 1031;
        if (State.Dgn_Cur == 11)
            State.DungeonBgCg = 1025;
        if (State.Dgn_Cur == 16 || State.Dgn_Cur == 26)
            State.DungeonBgCg = 1056;
        await EngineAsync("EC", 0);
        State.CallArg = State.DungeonBgCg;
        await WaoAsync(331);
        State.DgnRun_Floor = 1;                                         // :469-476
        State.Bt_EnemyParty1 = 83;
        State.Bt_EnemyParty2 = 0;
        State.Bt_EnemyParty3 = 0;
        State.Bt_EnemyParty4 = 0;
        if (Dgn.State == 0)
            Dgn.State = 1;
        foreach (var p in Parameters)                                   // :477-612, each test on the cursor as it is then
        {
            if (State.Dgn_Cur != (Sys3Int)p.Dungeon)
                continue;
            State.DgnRun_MaxFloor = (Sys3Int)p.MaxFloor;
            State.DgnRun_FloorMode = (Sys3Int)p.Mode;
            State.Bt_TroopLimit = (Sys3Int)p.TroopLimit;
            State.DungeonMonsterCmdr = (Sys3Int)p.Monster;
            State.DgnRun_MonsterPower = (Sys3Int)p.Power;
            if (p.Dungeon == 11)
                State.DungeonBgCg = 1025;
            if (p.Dungeon == 16)
                await Script.EventAsync("G1.ADV", 13);
        }
        await FootstepsAsync();                                         // \L_074d0
        if (State.BgmEnabled == 1)
            await Script.MusicAsync(21);
        if (State.Bt_PlayerParty1 == 1 && State.VAR0427 == 3 && State.Dgn_Cur == 23)
        {
            State.Ev_Entry = 2;
            await WaoAsync(3);
            await Script.CallFileAsync("G2.ADV");
            await WaoAsync(4);
            await EngineAsync("EC", 0);
            State.CallArg = State.DungeonBgCg;
            await WaoAsync(331);
        }
        if (Dgn.SavedFloor != 0)
            await ResumeAsync();
        await FloorsAsync();
    }

    /// <summary>
    /// <c>:629-668</c>: where the last search ended (980 for the Ancient Ruins in debug mode) and
    /// "nF On" or "From the Start" (also on a cancel).
    /// </summary>
    private async Task ResumeAsync()
    {
        if (State.DebugMode == 1 && State.Dgn_Cur == 30)
            Dgn.SavedFloor = 980;
        await WaoAsync(113);
        await ValuesAsync(634, Dgn.SavedFloor);                         // "…ended at floor n."
        await WaoAsync(1);
        await TextAsync(643);                                           // "Would you like to start from there?"
        await WaitForReleaseAsync();                                    // L_010d5
        var on = new SceneMenuItem(Ref(652), [new CapturedText(Ref(653), [Dgn.SavedFloor], [])]);
        await LineBreakAsync();
        int choice = await SelectAsync([on, MenuItem(657)]);
        if (choice == 1)
            State.DgnRun_Floor = Dgn.SavedFloor;                        // L_01127
        await WaoAsync(0);                                              // L_0111d / L_01127
    }

    /// <summary>The floor loop: <c>L_01137</c>, <c>L_0177d</c>, <c>L_016b4</c> and their exits.</summary>
    private async Task FloorsAsync()
    {
        var step = Step.Arrive;
        while (true)
        {
            step = step switch
            {
                Step.Arrive => await ArriveAsync(),
                Step.Header => await HeaderAgainAsync(),
                Step.Menu => await FloorMenuAsync(),
                _ => step,
            };
            if (step == Step.End)
            {
                await EndAsync();
                return;
            }
            if (step == Step.Exit)
                return;
        }
    }

    /// <summary><c>L_01137</c> (<c>:669-699</c>): the floor's header, then its fixed event.</summary>
    private async Task<Step> ArriveAsync()
    {
        await EngineAsync("EC", 0);
        State.CallArg = State.DungeonBgCg;
        await WaoAsync(331);
        await WaoAsync(113);
        await FloorLinesAsync(675);
        await ValuesAsync(692, Unit.Troops);                            // "name Unit (troops)"
        return await FixedFloorAsync();
    }

    /// <summary><c>L_0177d</c> (<c>:894-922</c>): the header printed again.</summary>
    private async Task<Step> HeaderAgainAsync()
    {
        await WaoAsync(113);
        await FloorLinesAsync(897);
        await ValuesAsync(914, Unit.Troops);
        return Step.Menu;
    }

    /// <summary>The dungeon's name and <c>　　nF</c> in either floor mode, starting at <paramref name="line"/>.</summary>
    private async Task FloorLinesAsync(int line)
    {
        await TextAsync(line);                                          // X 8: R
        if (State.DgnRun_FloorMode == 1)
            await ValuesAsync(line + 4, State.DgnRun_Floor);
        if (State.DgnRun_FloorMode == 2)
            await ValuesAsync(line + 11, State.DgnRun_Floor);
    }

    /// <summary>Whether the search is on floor <paramref name="floor"/> of dungeon <paramref name="dungeon"/>.</summary>
    private bool At(int dungeon, int floor) => State.Dgn_Cur == (Sys3Int)dungeon && State.DgnRun_Floor == (Sys3Int)floor;

    /// <summary>
    /// <c>:700-857</c>: the fixed floors, tested in order on every arrival (the first match ends
    /// the chain): the treasure chests, the story scenes, the Uncurse Spring, KD, Orochi, the end of
    /// the Giant Battleship; then the Ancient Ruins' deeper monsters.
    /// </summary>
    private async Task<Step> FixedFloorAsync()
    {
        if (At(2, 14) && Dgn.State == 1)
            return await ChestDoneAsync(9, true, 1455, 1462, 1467);     // \L_02528 DO Pot
        if (At(4, 12) && Dgn.State == 1)
            return await ChestDoneAsync(11, true, 1487, 1494, 1499);    // \L_025bd Snow Charm
        if (At(6, 25) && Dgn.State == 1)
            return await ChestDoneAsync(41, true, 1607, 1614, 1619);    // \L_027f5 Hawaiian Sword
        State.Item_Cur = 42;
        if (At(8, 20) && Item.Holder == 0)
            return await ChestOnlyAsync(42, true, 1637, 1644, 1649);    // \L_02883 Aztec Armor
        if (At(8, 30) && State.F_Seizel_State < 6)
            return await SceneAsync("Gｻｲｾﾞﾙ.ADV", 3);
        if (At(10, 27) && State.DgnRun_MerimInParty == 1)
            return await SceneAsync("Gﾒﾘﾑ.ADV", 4);
        if (At(10, 28))
        {
            await UncurseSpringAsync();                                 // \L_02e29
            Dgn.State = 2;
            return Step.Header;
        }
        if (At(11, 30))
        {
            await SummitAsync();                                        // \L_02f97
            Dgn.State = 2;
            return Step.Header;
        }
        if (At(14, 38) && State.F_Fuuka_State == 9 && State.F_Fuuka_ShrineVisited == 1)
        {
            await OrochiAsync();                                        // \L_023da
            if (Unit.Status == 0)
                return await FallenAsync();
            return Step.Header;
        }
        if (At(14, 20) && Dgn.State == 1)
            return await ChestDoneAsync(22, false, 1913, 1920, 1925);   // \L_02da1 Orgasmic Men
        if (At(15, 4) && State.F_Arcy_State == 1)
            return await SceneAsync("G1.ADV", 5);
        if (At(15, 38))
            return await SceneAsync("G1.ADV", 6);
        if (At(16, 20))
        {
            await Script.EventAsync("G1.ADV", 14);
            return Step.End;
        }
        if (At(18, 27) && Dgn.State == 1)
            return await ChestDoneAsync(33, false, 1755, 1762, 1767);   // \L_02abb Evil Statue of Strength
        if (At(19, 25) && Dgn.State == 1)
            return await ChestDoneAsync(21, false, 1786, 1793, 1798);   // \L_02b46 Crystal Ring
        if (At(23, 33))
        {
            await HolyGalAsync();                                       // \L_02bce
            return Step.Header;
        }
        if (At(23, 76) && Dgn.State == 1)
            return await ChestDoneAsync(34, false, 1825, 1832, 1837);   // \L_02c03 Tange Dog
        if (At(24, 50) && Dgn.State == 1)
            return await ChestDoneAsync(29, false, 1854, 1861, 1866);   // \L_02c8b Happiness Ring
        State.Item_Cur = 28;
        if (At(24, 10) && Item.Holder == 0 && State.FogCard_Price == 0)
            return await ChestOnlyAsync(28, false, 1884, 1891, 1896);   // \L_02d13 Fog Card
        if (At(22, 50) && (State.F_Kaybwan_State == 2 || State.F_Kaybwan_State == 3))
            return await SceneAsync("Gｹｲﾌﾞﾜﾝ.ADV", 4);
        State.Item_Cur = 38;
        if (At(20, 5) && Item.Holder == 0)
            return await ChestOnlyAsync(38, true, 1547, 1554, 1559);    // \L_026d9 Vevetar Mutant
        State.Item_Cur = 39;
        if (At(21, 16) && Item.Holder == 0)
            return await ChestOnlyAsync(39, true, 1577, 1584, 1589);    // \L_02767 Jugon
        if (At(20, 50) && (State.F_Kaybnyan_State == 2 || State.F_Kaybnyan_State == 3))
            return await SceneAsync("Gｹｲﾌﾞﾆｬﾝ.ADV", 4);
        if (At(25, 2))
            return await SceneAsync("G1.ADV", 18);
        State.Item_Cur = 30;
        if (At(25, 8) && Item.Holder == 0)
            return await ChestOnlyAsync(30, true, 1667, 1674, 1679);    // \L_02911 Space Shell
        if (At(25, 66) && State.F_HannyKing_State < 7)
        {
            await Script.EventAsync("Gﾊﾆｰ.ADV", 1);
            Dgn.State = 2;
            return Step.Header;
        }
        if (At(26, 201) && Dgn.State == 1)
            return await ChestDoneAsync(23, true, 1517, 1524, 1529);    // \L_0264b Wisdom Ring
        State.Item_Cur = 48;
        if (At(30, 20) && Item.Holder == 0)
            return await ChestOnlyAsync(48, true, 1697, 1704, 1709);    // \L_0299f Doping Drug
        State.Item_Cur = 36;
        if (At(30, 50) && Item.Holder == 0)
            return await ChestOnlyAsync(36, true, 1727, 1734, 1739);    // \L_02a2d Bird Corpse
        if (State.Dgn_Cur == 30 && State.DgnRun_Floor > 200)
        {
            State.DungeonMonsterCmdr = 84;
            State.DgnRun_MonsterPower = 250;
        }
        if (State.Dgn_Cur == 30 && State.DgnRun_Floor > 500)
        {
            State.DungeonMonsterCmdr = 85;
            State.DgnRun_MonsterPower = 250;
        }
        return Step.Menu;
    }

    /// <summary>A chest that ends the dungeon's search (<c>!Dgn_State : 2!</c> after it), then the header.</summary>
    private async Task<Step> ChestDoneAsync(int item, bool resetsCursor, int found, int what, int added)
    {
        await ChestAsync(item, resetsCursor, found, what, added);
        Dgn.State = 2;
        return Step.Header;
    }

    /// <summary>A chest gated on its item not being owned, then the header.</summary>
    private async Task<Step> ChestOnlyAsync(int item, bool resetsCursor, int found, int what, int added)
    {
        await ChestAsync(item, resetsCursor, found, what, added);
        return Step.Header;
    }

    /// <summary>A story scene of a fixed floor, then the header.</summary>
    private async Task<Step> SceneAsync(string file, int entry)
    {
        await Script.EventAsync(file, (Sys3Int)entry);
        return Step.Header;
    }

    /// <summary>
    /// <c>:742-747</c>, <c>:1067-1072</c>, <c>:1149-1154</c>: the searcher fell — the command ends at
    /// once (<c>%0:</c>).
    /// </summary>
    private async Task<Step> FallenAsync()
    {
        // QUIRK: DGN-08 — no end-of-search scenes, Ev_CommandMode kept, the saved floor the one before the move.
        await WaoAsync(3);
        await Script.MapCallAsync(0);
        return Step.Exit;
    }

    /// <summary>
    /// <c>L_016b4</c> (<c>:858-893</c>): the floor saved, then Ascend / Descend (their labels and
    /// order follow the floor mode) and Retreat; a cancel shows the menu again.
    /// </summary>
    private async Task<Step> FloorMenuAsync()
    {
        while (true)
        {
            await WaitForReleaseAsync();
            Dgn.SavedFloor = State.DgnRun_Floor;
            var items = new List<SceneMenuItem>();
            var moves = new List<int>();
            if (State.DgnRun_FloorMode == 1 && State.DgnRun_Floor != 1)
                Offer(866, -1);                                         // Ascend
            if (State.DgnRun_FloorMode == 2 && State.DgnRun_Floor < State.DgnRun_MaxFloor)
                Offer(872, +1);                                         // Ascend
            if (State.DgnRun_FloorMode == 2 && State.DgnRun_Floor != 1)
                Offer(878, -1);                                         // Descend
            if (State.DgnRun_FloorMode == 1 && State.DgnRun_Floor < State.DgnRun_MaxFloor)
                Offer(884, +1);                                         // Descend
            Offer(889, 0);                                              // Retreat
            int choice = await SelectAsync(items);
            if (choice == 0)
                continue;
            return moves[choice - 1] switch
            {
                +1 => await DeeperAsync(),
                -1 => await BackAsync(),
                _ => await RetreatAsync(),
            };

            void Offer(int line, int move)
            {
                items.Add(MenuItem(line));
                moves.Add(move);
            }
        }
    }

    /// <summary>
    /// <c>L_01982</c> (<c>:999-1080</c>): one floor deeper (up in a tower), the encounter roll and,
    /// on a 2, the inscription roll.
    /// </summary>
    private async Task<Step> DeeperAsync()
    {
        await WaoAsync(0);
        State.DgnRun_Floor = State.DgnRun_Floor + 1;
        await MoveTextAsync(1006, 1011);
        if (!await EncounterAsync())
            return await FallenAsync();
        // QUIRK: DGN-02 — after a battle RetVal is whatever the battle's path left.
        // QUIRK: DGN-03 — only this direction has the inscription branch.
        if (State.RetVal == 2)
        {
            // QUIRK: STR-14 — drawn with or without Merim.
            Script.Rnd(10);
            if (State.DgnRun_MerimInParty == 1 && State.RetVal == 1)
                await InscriptionAsync();                               // \L_06153
        }
        return Step.Arrive;
    }

    /// <summary><c>L_01c0a</c> (<c>:1081-1156</c>): one floor back, the encounter roll.</summary>
    private async Task<Step> BackAsync()
    {
        await WaoAsync(0);
        State.DgnRun_Floor = State.DgnRun_Floor - 1;
        await MoveTextAsync(1088, 1093);
        if (!await EncounterAsync())
            return await FallenAsync();
        return Step.Arrive;
    }

    /// <summary>The steps, "Slowly progressing …" by the floor mode, the sound stopped.</summary>
    private async Task MoveTextAsync(int pit, int tower)
    {
        await FootstepsAsync();
        await WaoAsync(113);
        if (State.DgnRun_FloorMode == 1)
        {
            await TextAsync(pit);
            await WaoAsync(1);
        }
        if (State.DgnRun_FloorMode == 2)
        {
            await TextAsync(tower);
            await WaoAsync(1);
        }
        await Script.SoundAsync(3);                                     // \L_07506
        await WaoAsync(0);
    }

    /// <summary>
    /// <c>:1016-1072</c>, <c>:1098-1154</c>: the encounter roll (1 in 4; the Safety Bonsai's 1 in
    /// 10 replaces it), the bosses forcing a battle, the battle; false when the searcher fell.
    /// </summary>
    private async Task<bool> EncounterAsync()
    {
        Script.Rnd(4);
        // QUIRK: STR-14 — with the Safety Bonsai both rolls are drawn.
        if (Unit.Item == 10)
            Script.Rnd(10);
        // QUIRK: STR-14 — a boss floor still draws the rolls above.
        foreach (var boss in Bosses)
            if (BossHere(boss))
                State.RetVal = 1;
        if (State.RetVal != 1)
            return true;
        await MonstersAsync();                                          // \L_01e67
        return Unit.Status != 0;
    }

    private bool BossHere((int Dungeon, int Floor, Func<GameState, Sys3Int>? Flag, int Setter) boss) =>
        At(boss.Dungeon, boss.Floor) && (boss.Flag is null || boss.Flag(State) == 0);

    /// <summary>
    /// <c>L_0181c</c> (<c>:923-944</c>): "Are you finished searching?" — Finish ends the search,
    /// Keep Going goes back to the floor (<c>L_018c3</c>); a cancel meets a second, empty menu first.
    /// </summary>
    private async Task<Step> RetreatAsync()
    {
        await WaoAsync(0);
        await WaoAsync(113);
        await TextAsync(927);
        await WaitForReleaseAsync();                                    // L_01830
        var finish = MenuItem(934);
        await LineBreakAsync();
        int choice = await SelectAsync([finish, MenuItem(938)]);
        if (choice == 1)
            return Step.End;
        if (choice == 0)
        {
            // QUIRK: DGN-07 — the second ] has no items: only a cancel leaves it.
            await SelectAsync([]);
        }
        // QUIRK: DGN-15 — :945-962 ("Will you go outside?") is never reached.
        await WaoAsync(0);                                              // L_018c3
        return Step.Arrive;
    }

    /// <summary>
    /// <c>L_018cd</c> (<c>:966-998</c>): "Search of … ended.", the week's music back, the map, the
    /// end-of-search scenes (Melfeis, Mill, Ruberan) and the command mode cleared.
    /// </summary>
    private async Task EndAsync()
    {
        await WaoAsync(0);
        await EngineAsync("EC", 0);
        await Script.MapCallAsync(0);
        State.CallArg = State.DungeonBgCg;
        await WaoAsync(331);
        await WaoAsync(113);
        await TextAsync(975);
        await WaoAsync(1);
        await WaoAsync(0);
        // QUIRK: DGN-16 — played whether the music is on or not.
        await Script.MusicAsync(State.CurrentBgm);
        await WaoAsync(3);
        await Script.MapCallAsync(0);
        if (State.Unit_Cur == 13 && State.N_Melfeis_Stage == 11)
            await Script.EventAsync("Gﾒﾙﾌｪｲｽ.ADV", 4);
        State.Ev_CommandMode = 0;
        // QUIRK: DGN-11 — reset to 4 on entry, so only a 3 set during this search plays it.
        if (State.F_Merim_MillArc == 3)
            await Script.EventAsync("Gﾐﾙ.ADV", 2);
        if (State.Dgn_Cur == 30 && State.F_Ruberan_State == 0 && State.DgnRun_MerimInParty == 1)
            await Script.EventAsync("Gﾙｰﾍﾞﾗﾝ.ADV", 3);
    }

    /// <summary>
    /// <c>L_01e67</c> (<c>:1157-1195</c>): "Monsters appeared." — the monsters' commander (in Titi
    /// Lake the Hanny tier of the floor, written over commander 100), their troops rolled, then the
    /// battle.
    /// </summary>
    private async Task MonstersAsync()
    {
        State.Bt_EnemyParty1 = State.DungeonMonsterCmdr;
        await WaoAsync(113);
        await FloorLinesAsync(1161);
        await TextAsync(1178);                                          // "Monsters appeared."
        await WaoAsync(1);
        await WaoAsync(0);
        if (State.Dgn_Cur == 25)
        {
            await RunBlockAsync(3382);                                  // \L_04ae3 Red Hanny
            if (State.DgnRun_Floor > 25)
                await RunBlockAsync(3412);                              // \L_04ba0 Super Hanny
            if (State.DgnRun_Floor > 40)
                await RunBlockAsync(3442);                              // \L_04c61 Hanny Knight
        }
        State.Cmdr_Cur = State.Bt_EnemyParty1;
        // QUIRK: STR-14 — drawn even when a boss replaces the monsters.
        Script.Rnd(State.DgnRun_MonsterPower);
        // QUIRK: DGN-05 — the troops can exceed the commander's maximum.
        Cmdr.Troops = State.DgnRun_MonsterPower / 3 + State.RetVal;
        Cmdr.HP = Cmdr.MaxHP;
        await FightAsync();
    }

    /// <summary>
    /// <c>L_01f47</c> (<c>:1196-1375</c>): the boss of the floor over commander 100 (its flag set
    /// by the setter, before the fight), the dungeon battle (<c>FIGT</c>, then <c>死亡捕獲</c>);
    /// after it Gunagan's treasure, Merim's injury roll and Aaya's scene.
    /// </summary>
    private async Task FightAsync()
    {
        foreach (var boss in Bosses)
        {
            if (!BossHere(boss))
                continue;
            State.Bt_EnemyParty1 = 100;
            // QUIRK: DGN-04 — the setter writes the boss's flag: one attempt, whatever the outcome.
            await RunBlockAsync(boss.Setter);
            // QUIRK: DGN-16 — played whether the music is on or not.
            await Script.MusicAsync(18);
        }
        State.Bt_EnemyRetreatMode = 1;
        State.Bt_WallBonus = 0;
        State.Bt_BackgroundCg = 910;
        if (State.Dgn_Cur == 11)
            State.Bt_BackgroundCg = 912;
        if (State.Dgn_Cur == 16 || State.Dgn_Cur == 26)
            State.Bt_BackgroundCg = 914;
        State.Bt_Kind = 3;
        await Script.CallFileAsync("FIGT.ADV");
        await Script.MusicAsync(21);
        State.Bt_BackgroundCg = 900;
        await EngineAsync("EC", 0);
        State.CallArg = State.DungeonBgCg;
        await WaoAsync(331);
        await Script.CallFileAsync("死亡捕獲.ADV");
        State.Bt_Kind = 0;
        State.Bt_IsDuel = 0;
        State.Unit_Cur = State.Bt_PlayerParty1;
        if (Unit.Status == 0)
        {
            if (State.DgnRun_MerimInParty == 1)
            {
                State.F_Merim_Dungeon = 3;
                State.DgnRun_MerimInParty = 2;
            }
            return;
        }
        State.Cmdr_Cur = 100;
        // QUIRK: DGN-01 — any fight with Gunagan the searcher survives.
        if (Cmdr.CgFull == 1137 && State.F_Gunagan_State == 1)
        {
            State.F_Gunagan_State = 2;
            await SpeakAsync(113, 1304, 1309);                          // "A treasure was found behind Gunagan’s corpse."
            await EngineAsync("EC", 0);
            await EngineAsync("G", 806);
            await EngineAsync("G", 862);
            await SpeakAsync(131, 1319, 1322);
            await EngineAsync("EC", 0);
            State.CallArg = State.DungeonBgCg;
            await WaoAsync(331);
            await SpeakAsync(113, 1332);                                // "Threw away Gunagan’s treasure."
        }
        if (State.DgnRun_MerimInParty == 1)
        {
            Script.Rnd(20);
            State.Tmp0690 = State.RetVal;
            if (State.Tmp0690 == 1)
            {
                State.DgnRun_MerimInParty = 2;
                if (State.Bld_Hospital == 1)
                {
                    State.F_Merim_Dungeon = 2;
                    await SpeakAsync(113, 1347);                        // "…Merim took a serious injury."
                }
                if (State.Bld_Hospital != 1)
                {
                    State.F_Merim_Dungeon = 3;
                    await SpeakAsync(113, 1359);                        // "…killed during the battle."
                }
            }
        }
        if (State.Dgn_Cur == 6 && State.Bt_EnemyParty1 == 100 && State.Bt_PlayerParty1 == 1
            && (State.N_Aaya_Stage == 5 || State.N_Aaya_Stage == 6))
            await Script.EventAsync("Gｱｰﾔ.ADV", 4);
    }

    /// <summary><c>L_074d0</c>: the footsteps (sound 74), except in the dungeons 5, 7, 11, 12 and 17.</summary>
    private async Task FootstepsAsync()
    {
        if (State.Dgn_Cur != 5 && State.Dgn_Cur != 7 && State.Dgn_Cur != 11 && State.Dgn_Cur != 12 && State.Dgn_Cur != 17)
        {
            State.RetVal = 74;
            await Script.SoundAsync(0);
        }
    }

    /// <summary><c>L_07514</c>: the music stopped (if on) and the fanfare (sound 101).</summary>
    private async Task FanfareAsync()
    {
        if (State.BgmEnabled == 1)
            await Script.MusicAsync(0);
        State.RetVal = 101;
        await Script.SoundAsync(0);
    }

    /// <summary><c>L_0753a</c>: the sound stopped and the dungeon's music again (if on).</summary>
    private async Task FanfareEndAsync()
    {
        await Script.SoundAsync(3);
        if (State.BgmEnabled == 1)
            await Script.MusicAsync(21);
    }

    /// <summary><c>ZM 32: 'title' ZM 24:</c> — the place's name in the large font.</summary>
    private async Task TitleAsync(int line)
    {
        await Script.ShowAsync(new EngineEvent("ZM", [(Sys3Int)32]));
        await TextAsync(line);
        await Script.ShowAsync(new EngineEvent("ZM", [(Sys3Int)24]));
    }
}
