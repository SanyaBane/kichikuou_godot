using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>Cｼﾊｲﾄｼ.ADV</c>, an own city (<c>docs/strategy/survey-commands.md</c> §3.3), ported whole: the
/// city's header and its menu — Temporary Levy (gold, then the first story trigger of the city or
/// the item and scene rolls, or a girl for the Special Room), Bolster Defense, the Babolat and
/// Galtia commands, and the city's places (the castle's buildings and cells, story places, the
/// Ruins Search). One action a week across all own cities (<c>Ui_CmdOwnCityAvail</c>; spent items
/// show "×" and lead back to the menu); the Ruins Search is gated by the dungeon flag alone.
/// </summary>
public sealed class OwnCityCommand(OriginalScriptContext script) : CommandFile(script, "Cｼﾊｲﾄｼ.ADV")
{
    private enum Next
    {
        /// <summary><c>L_000cb</c>: the menu again.</summary>
        Menu,

        /// <summary><c>L_0006d</c>: the header, then the menu.</summary>
        Header,

        /// <summary>The file returns.</summary>
        Done,
    }

    /// <summary><c>Cｼﾊｲﾄｼ.ADV:1-507</c>: the command, up to its return.</summary>
    public async Task RunAsync()
    {
        CopyString(8, State.Prov_StrBase + State.Prov_Cur - 1);        // :1 MM 8, the city's name
        State.CallArg = Prov.BgCg;
        await WaoAsync(331);                                            // \L_024ad
        if (State.Ev_CommandMode == 1)                                  // :4-13
        {
            await Script.EventAsync("Gｶﾌｪ.ADV", 31);
            return;
        }
        if (State.Ev_CommandMode == 2)
        {
            await Script.EventAsync("Gﾏﾘｱ.ADV", 5);
            return;
        }
        while (true)
        {
            await WaoAsync(113);                                        // L_0006d: \L_023e9
            await ValuesAsync(17, Prov.Economy, Prov.Defense, Prov.BattleArea);   // "[name] (Leazas Army) Economy …"
            Next next;
            do
                next = await MenuAsync();
            while (next == Next.Menu);
            if (next == Next.Done)
                return;
        }
    }

    /// <summary>
    /// <c>L_000cb</c> (<c>:32-507</c>): the menu — the four commands (○ while the week's action is
    /// free, × otherwise), the city's places likewise (their × items 207 lines below the ○ ones;
    /// the Ruins Search ○ in both while the dungeon flag is up), Return; a cancel puts the pointer
    /// back and leaves (<c>@L_010ff</c>).
    /// </summary>
    private async Task<Next> MenuAsync()
    {
        await WaitForReleaseAsync();
        await SavePointerAsync();                                       // \L_0239b
        State.Tmp0021 = 0;
        bool free = State.Ui_CmdOwnCityAvail != 0;
        var items = new List<SceneMenuItem>();
        foreach (var (line, offered) in Commands())
            if (offered)
                items.Add(MenuItem(free ? line : line + 24));
        State.Unit_Cur = 16;                                            // :88, :295
        foreach (var (line, offered) in Places())
            if (offered)
                items.Add(MenuItem(free ? line : line + 207));
        items.Add(MenuItem(502));                                       // Return
        int choice = await SelectAsync(items);
        if (choice == 0)
        {
            await RestorePointerAsync();                                // \L_023a6
            await LeaveAsync();
            return Next.Done;
        }
        int at = items[choice - 1].At.Line;
        if (at == 436)
            at = 229;                                                   // the Ruins Search's ○ item in the × block
        else if (at is 65 or 71 or 77 or 83 or 235 || at > 294 && at < 500)
            return Next.Menu;                                           // a × item: $L_000cb$
        switch (at)
        {
            case 41:
                await LevyAsync();
                return Next.Done;
            case 47:
                await BolsterAsync();
                return Next.Header;
            case 133:
                await CellsAsync();
                return Next.Done;
            case 115:
                await LibraryAsync();
                return Next.Done;
            case 229:
                await RuinsSearchAsync();
                return Next.Done;
            case 502:
                await LeaveAsync();
                return Next.Done;
        }
        State.Ui_CmdOwnCityAvail = 0;                                   // :508-727, :806-809
        switch (at)
        {
            case 53: await Script.EventAsync("Gﾊﾞﾎﾞﾗ.ADV", 3); break;  // L_00e7e
            case 59: await Script.EventAsync("Gｶﾞﾙﾃｨｱ.ADV", 3); break; // L_00e5c
            case 91: await Script.EventAsync("Gﾏﾘｱ.ADV", 1); break;    // L_00f38
            case 97:                                                    // L_00f0e
                await Script.EventAsync("Gｱﾋﾞｧﾄｰﾙ.ADV", 1);
                await Script.MapCallAsync(0);
                break;
            case 103: await Script.EventAsync("Gｱｰﾔ.ADV", 1); break;   // L_00efd
            case 109: await Script.EventAsync("Gﾏｹﾝ.ADV", 1); break;   // L_00f27
            case 121: await Script.EventAsync("Gｽｽﾞﾒ.ADV", 3); break;  // L_00df6
            case 127: await Script.EventAsync("Gｱｵｲ.ADV", 1); break;   // L_00f49
            case 139: await Script.EventAsync("Gﾐｷ.ADV", 2); break;    // L_00eb1
            case 145: await Script.EventAsync("Gﾄｸﾍﾞﾂ.ADV", 1); break;  // L_00e3a
            case 151: await Script.EventAsync("Gﾏﾘｱ.ADV", 4); break;   // L_00c49
            case 157: await Script.EventAsync("Gｶﾊﾟｰﾗ.ADV", 2); break;  // L_00e07
            case 163: await Script.EventAsync("Gﾚｲ.ADV", 3); break;    // L_00ec2
            case 169: await Script.EventAsync("Gｶｾｲ.ADV", 3); break;   // L_00eec
            case 175:                                                   // L_00ed3
                await Script.EventAsync("Gｾﾙ.ADV", 2);
                await Script.MapCallAsync(0);
                break;
            case 181: await Script.EventAsync("Gｼﾉﾀﾞ.ADV", 1); break;  // L_00f72
            case 187: await Script.CallFileAsync("IMﾗﾝﾄﾞ.ADV"); break;  // L_0110c
            case 193: await Script.CallFileAsync("Gｱﾘｽ.ADV"); break;    // L_00f66
            case 199: await Script.EventAsync("Gｱﾃﾅ2.ADV", 1); break;  // L_00c38
            case 205: await Script.CallFileAsync("GDX.ADV"); break;     // L_00f5a
            case 211: await Script.EventAsync("Iｼｬﾝｸﾞﾘﾗ.ADV", 5); break; // L_00f83
            case 217: await Script.EventAsync("Iｼｬﾝｸﾞﾘﾗ.ADV", 6); break; // L_00f94
            case 223: await Script.EventAsync("Gｴﾚﾅ.ADV", 6); break;   // L_00c8d
            case 241: await Script.EventAsync("Gﾊﾟﾊﾟｲｱ.ADV", 7); break; // L_00ea0
            case 247: await Script.EventAsync("Gﾏｼﾞｯｸ.ADV", 4); break;  // L_00e8f
            case 253: await Script.EventAsync("Gﾌｳｶ.ADV", 10); break;  // L_00c5a
            case 259: await Script.EventAsync("Gﾏﾙﾁﾅ.ADV", 1); break;  // L_00e6d
            case 265: await Script.EventAsync("Gｻｲｾﾞﾙ.ADV", 4); break; // L_00e4b
            case 271: await Script.EventAsync("Gｹｲﾌﾞﾜﾝ.ADV", 2); break; // L_00c6b
            case 277: await Script.EventAsync("Gｹｲﾌﾞﾆｬﾝ.ADV", 2); break; // L_00c7c
            case 283: await Script.EventAsync("GAL.ADV", 6); break;     // L_00e29
            default: await Script.EventAsync("Gｶﾌｪ.ADV", 1); break;    // 289, L_00e18
        }
        return Next.Done;
    }

    /// <summary>The four commands (<c>:39-62</c>; their × items 24 lines below, <c>:63-86</c>).</summary>
    private (int Line, bool Offered)[] Commands() =>
    [
        (41, Prov.Economy != 0),                                        // Temporary Levy
        (47, true),                                                     // Bolster Defense
        (53, State.F_Babolat_State == 2),                               // Dig Giant Hole
        (59, State.F_Galtia_State == 3 || State.F_Galtia_State == 4),   // Set Poison Food
    ];

    /// <summary>The city's places (<c>:87-292</c>, <c>:294-499</c>), with <c>Unit_Cur</c> at 16.</summary>
    private (int Line, bool Offered)[] Places()
    {
        int p = State.Prov_Cur.Value;
        return
        [
            (91, p == 1 && Unit.Status != 0 && State.Bld_MariaLab == 1 && State.F_Maria_Tulip != 15),  // Tulip Research
            (97, p == 1 && State.Bld_GirlsAcademy == 1),                // Military Academy
            (103, p == 1 && State.Bld_Hospital == 1),                   // Genius Hospital
            (109, p == 1 && State.Bld_MagicLab == 1),                   // Magic Research
            (115, p == 1 && State.Bld_Library == 1),                    // Giant Library
            (121, p == 1 && State.Bld_SMTower == 1),                    // S&M Tower
            (127, p == 1 && State.F_Aoi_State > 9 && State.F_Aoi_State != 100),   // Aoi Rehab
            (133, p == 1),                                              // Dungeon (the cells)
            (139, p == 1 && State.F_Miki_InCastle != 0),                // Demon Lord's Room
            (145, p == 1 && State.N_SpecialRoom_Girls != 0),            // Special Room
            (151, p == 2 && State.Goliath_State != 0),                  // T2 Goliath
            (157, p == 10 && State.N_Kapala_Rank != 0 && State.F_Kapala_State != 3),   // Nadate TV
            (163, p == 15 && State.N_Rei_Stage > 1),                    // Mary's House
            (169, p == 22 && State.N_Mars_Score == 1),                  // Mars's Palace
            (175, p == 23 && State.F_Cell_ChurchArc < 12),              // Cell's Church
            (181, p == 26 && State.F_Shinoda_State != 0 && State.F_Sayori_Fate != 2),  // Shinoda's Estate
            (187, p == 25 && State.F_MLand_Funded == 1),                // Amusement Park
            (193, p == 24),                                             // Hanny Building
            (199, p == 18),                                             // Rance's House
            (205, p == 17 && State.F_DX_State != 0 && State.F_DX_State != 100),   // DX Association
            (211, p == 29 && State.F_ShangriLa_State == 5),             // Secret Room
            (217, p == 29 && State.F_ShangriLa_State == 6),             // Housesnurse
            (223, p == 40 && State.F_Elena_State == 6),                 // Elena's House
            // QUIRK: CMD-09 — the Ruins Search follows the dungeon flag, ○ also when the week's action is spent.
            (229, p == 35 && State.Ui_CmdDungeonAvail != 0),            // Ruins Search
            (235, p == 35 && State.Ui_CmdDungeonAvail == 0),            // ×Ruins Search
            (241, p == 69 && State.F_Papaya_State == 8),                // Zeth Palace
            (247, p == 66 && (State.F_Magic_State == 2 || State.F_Magic_State == 3)),  // Magic's Tower
            (253, p == 55 && State.F_Fuuka_State != 0 && State.F_Fuuka_State != 19),   // Shrine
            (259, p == 59 && State.F_Martina_State != 100),             // Sakura & Pasta
            (265, p == 84),                                             // Dungeon (84)
            (271, p == 86 && State.F_Kaybwan_State != 0 && State.F_Kaybwan_State < 5),   // Blue House
            (277, p == 85 && State.F_Kaybnyan_State != 0 && State.F_Kaybnyan_State < 5), // Red House
            (283, p == 77 && State.N_ALChurch_Stage != 100),            // AL Church HQ
            (289, State.Prov_Cur == State.LadyCaravan_Prov && State.F_Cafe_State < 10),  // Lady Caravan
        ];
    }

    /// <summary><c>L_010ff</c> (<c>:802-805</c>): the windows closed; the file returns.</summary>
    private async Task LeaveAsync()
    {
        await WaoAsync(0);                                              // \L_024c9
        await WaoAsync(332);                                            // \L_024bb
    }

    // -----------------------------------------------------------------------------------------
    // The places with more than a scene
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_00c9e</c> (<c>:538-548</c>): the Ancient Ruins as dungeon record 30 at this city, then
    /// <c>Cﾒｲｷｭｳ</c>.
    /// </summary>
    private async Task RuinsSearchAsync()
    {
        // QUIRK: CMD-09 — the ruins' record is rewritten (its search state reset) on every use.
        State.Dgn_Cur = 30;
        Dgn.MapX = Prov.MapX;
        Dgn.MapY = Prov.MapY;
        Dgn.State = 0;
        Dgn.Prov = 35;
        await WaoAsync(0);
        await Script.MapCallAsync(0);
        await Script.CallFileAsync("Cﾒｲｷｭｳ.ADV");
    }

    /// <summary><c>L_00ccd</c> (<c>:549-600</c>): the Giant Library's first scene that applies, else Maris's.</summary>
    private async Task LibraryAsync()
    {
        State.Ui_CmdOwnCityAvail = 0;
        State.Unit_Cur = 16;
        if (Unit.Status != 0 && State.F_LibraryGhost_State == 0)
        {
            await Script.EventAsync("Gﾕｳﾚｲ.ADV", 1);
            return;
        }
        if (State.N_Kanami_State != 0 && State.F_LibraryGhost_State == 1)
        {
            await Script.EventAsync("Gﾕｳﾚｲ.ADV", 2);
            return;
        }
        State.Unit_Cur = 12;
        if (Unit.Status != 0 && State.F_LibraryGhost_State == 2)
        {
            await Script.EventAsync("Gﾕｳﾚｲ.ADV", 3);
            return;
        }
        if (State.F_LibraryGhost_State == 4)
        {
            await Script.EventAsync("Gﾕｳﾚｲ.ADV", 5);
            return;
        }
        if (State.F_LibraryGhost_State == 5)
        {
            await Script.EventAsync("Gﾕｳﾚｲ.ADV", 7);
            return;
        }
        State.Unit_Cur = 4;
        if (Unit.Status != 0 && State.N_Soul_Stage == 4)
        {
            State.Unit_Cur = 16;
            if (Unit.Status != 0)
            {
                await Script.EventAsync("Gｿｳﾙ.ADV", 1);
                return;
            }
        }
        if (State.F_Raylee_State == 3)
        {
            await Script.EventAsync("Gﾚｨﾘｨ.ADV", 2);
            return;
        }
        State.Unit_Cur = 38;
        if (Unit.Status != 0 && State.F_Annis_State == 3)
        {
            await Script.EventAsync("Gｱﾆｽ.ADV", 3);
            return;
        }
        await Script.EventAsync("Gﾏﾘｽ.ADV", 11);
    }

    /// <summary>
    /// <c>L_00fa5</c> (<c>:728-801</c>): the castle's cells — the prisoners held, Look Around,
    /// Return (which leaves the command); a cancel shows the list again. A visit plays sound 74
    /// and spends the week's action.
    /// </summary>
    private async Task CellsAsync()
    {
        int at;
        while (true)
        {
            await WaitForReleaseAsync();
            var items = new List<SceneMenuItem>();
            if (State.F_Lelyukov_State == 3)
                items.Add(MenuItem(735));                               // Lelyukov's Cell
            if (State.F_Ruberan_State != 0 && State.F_Ruberan_State < 4)
                items.Add(MenuItem(741));                               // Ruberan's Cell
            if (State.F_Papaya_State == 6 || State.F_Papaya_State == 7 || State.F_Papaya_State == 8 || State.F_Papaya_State == 11)
                items.Add(MenuItem(747));                               // Papaya's Cell
            if (State.F_Cafe_State == 10 || State.F_Cafe_State == 11)
                items.Add(MenuItem(753));                               // Lady's Cell
            items.Add(MenuItem(758));                                   // Look Around
            await LineBreakAsync();
            items.Add(MenuItem(762));                                   // Return
            int choice = await SelectAsync(items);
            if (choice != 0)
            {
                at = items[choice - 1].At.Line;
                break;
            }
        }
        if (at == 762)
        {
            await LeaveAsync();
            return;
        }
        State.RetVal = 74;
        await Script.SoundAsync(1);                                     // \L_0238d
        State.Ui_CmdOwnCityAvail = 0;
        switch (at)
        {
            case 758: await Script.EventAsync("G1.ADV", 1); break;      // L_01078
            case 735: await Script.EventAsync("Gﾚﾘｭｰｺﾌ.ADV", 4); break;  // L_01093
            case 741: await Script.EventAsync("Gﾙｰﾍﾞﾗﾝ.ADV", 10); break; // L_010ae
            case 747: await Script.EventAsync("Gﾊﾟﾊﾟｲｱ.ADV", 6); break;  // L_010c9
            default: await Script.EventAsync("Gｶﾌｪ.ADV", 2); break;     // 753, L_010e4
        }
    }

    // -----------------------------------------------------------------------------------------
    // Temporary Levy
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_01118</c> (<c>:810-1181</c>): the levy — a flavour draw, then <c>RND(Economy × 10)</c>
    /// gold; the first story trigger of the city that applies, else the Cat Bread, Kuriripu and
    /// Kapala rolls, else a girl for the Special Room when the city has one. The windows close and
    /// the file returns.
    /// </summary>
    private async Task LevyAsync()
    {
        await WaoAsync(0);
        State.CallArg = 1;
        await WaoAsync(311);                                            // \L_02405
        await SpeakAsync(111, 816, 823, 826);                           // "Alright, time for a levy."
        State.CallArg = 6;
        await WaoAsync(321);                                            // \L_02475
        await SpeakAsync(121, 835);
        await WaoAsync(322);                                            // \L_02483
        State.Ui_CmdOwnCityAvail = 0;
        await WaoAsync(312);                                            // \L_02413
        await WaoAsync(332);                                            // \L_024bb
        await WaoAsync(113);
        await TextAsync(850);                                           // "The Leazas Army began a temporary levy."
        await WaoAsync(1);
        Script.Rnd(3);                                                  // :854 the flavour
        State.Tmp0690 = State.RetVal;
        if (State.Tmp0690 == 1)
        {
            await TextAsync(858);
            await WaoAsync(1);
        }
        if (State.Tmp0690 == 2)
        {
            await TextAsync(865);
            await WaoAsync(1);
        }
        if (State.Tmp0690 == 3)
        {
            await TextAsync(872);
            await WaoAsync(1);
        }
        await TextAsync(882);                                           // R "........."
        await WaoAsync(1);
        // QUIRK: CMD-07 — gold with no penalty and no history entry.
        Script.Rnd(Prov.Economy * 10);                                  // :885
        await ValuesAsync(887, State.RetVal);                           // "… a total of n00K GOLD was collected."
        await WaoAsync(1);
        State.VAR0055 = State.VAR0055 + State.RetVal;                   // a write-only tally
        State.Gold = State.Gold + State.RetVal;
        await WaoAsync(0);

        if (!await StoryTriggerAsync())
            await FindAsync();
        await WaoAsync(312);                                            // L_019a8
        await WaoAsync(322);
        await WaoAsync(332);
        // QUIRK: CMD-20 — :1182-1235 (the audience room after a levy, back to the header) follow the %0: unlabelled.
    }

    /// <summary>
    /// <c>:897-1038</c>: the levy's story triggers, the first that applies plays (true); the
    /// cursors they set stay (<c>Cmdr_Cur</c> 92, 39, 50; <c>Item_Cur</c> 10, 20, 45, 35, 31, 32, 40).
    /// </summary>
    private async Task<bool> StoryTriggerAsync()
    {
        int p = State.Prov_Cur.Value;
        if (State.Prov_Cur == State.Freya_TargetProv)
            return await SceneAsync("Gﾌﾚｲｱ.ADV", 2);
        State.Cmdr_Cur = 92;
        if (p == 15 && Cmdr.Status != 0 && State.N_Camilla_Stage != 100 && State.N_Rei_Stage == 1)
            return await SceneAsync("Gﾚｲ.ADV", 2);
        if (p == 4 && State.F_Mokomoko_State == 0)
        {
            State.F_Mokomoko_State = 11;
            return await SceneAsync("Gﾓｺﾓｺ.ADV", 1);
        }
        if (p == 56 && State.F_Makoto_State == 0)
        {
            State.F_Makoto_State = 1;
            return await SceneAsync("Gﾏｺﾄ.ADV", 1);
        }
        State.Item_Cur = 10;
        if (p == 1 && Item.Holder == 0)
            return await SceneAsync("G1.ADV", 7);
        State.Item_Cur = 20;
        State.Cmdr_Cur = 39;
        if (p == 45 && Cmdr.TroopAtk == 20 && Item.Holder == 0)
            return await SceneAsync("G1.ADV", 8);
        if (p == 34 && State.F_Merci_State == 1)
            return await SceneAsync("Gﾒﾙｼｨ.ADV", 1);
        if (p == 34 && State.F_Anasel_Fate == 1 && State.Bld_SMTower == 1)
            return await SceneAsync("Gｱﾅｾﾙ.ADV", 1);
        if (p == 36 && State.F_Lelyukov_State == 3 && State.N_Amiran_Stage == 1)
            return await SceneAsync("Gｱﾐﾗﾝ.ADV", 1);
        State.Cmdr_Cur = 50;
        if (p == 42 && State.F_Rolex_GraveSeen == 0 && State.F_Saulnia_State != 0 && State.F_Katyusha_State != 0
            && Cmdr.Status != 0 && State.F_Helman_War == 2)
            return await SceneAsync("Gﾛﾚｯｸｽ.ADV", 4);
        if (p == 43 && State.F_Helman_War == 1 && State.N_Olhound_Stage != 100 && State.N_Olhound_Stage != 0)
            return await SceneAsync("Gｵｰﾙﾊｳﾝﾄ.ADV", 2);
        State.Item_Cur = 45;
        if (p == 43 && Item.Holder == 0)
            return await SceneAsync("G1.ADV", 9);
        State.Item_Cur = 35;
        if (p == 62 && Item.Holder == 0)
            return await SceneAsync("G2.ADV", 4);
        State.Item_Cur = 31;
        if (p == 41 && Item.Holder == 0)
            return await SceneAsync("G2.ADV", 5);
        State.Item_Cur = 32;
        if (p == 11 && Item.Holder == 0)
            return await SceneAsync("G2.ADV", 6);
        State.Item_Cur = 40;
        if (p == 54 && Item.Holder == 0)
            return await SceneAsync("G2.ADV", 7);
        if (p == 47 && State.F_FieldHQReportSeen == 0)
            return await SceneAsync("G2.ADV", 8);
        if (p == 48 && State.F_Stessel_State == 1)
            return await SceneAsync("Gｽﾃｯｾﾙ.ADV", 11);
        if (p == 53 && State.F_Stessel_State == 2)
            return await SceneAsync("Gｽﾃｯｾﾙ.ADV", 12);
        if (p == 49 && State.F_Stessel_State == 3)
            return await SceneAsync("Gｽﾃｯｾﾙ.ADV", 13);
        if (p == 24 && State.F_Gandhi_State == 1)
            return await SceneAsync("Gｶﾞﾝｼﾞｰ.ADV", 2);
        if (p == 23 && State.F_Gandhi_State == 2)
            return await SceneAsync("Gｶﾞﾝｼﾞｰ.ADV", 3);
        if (p == 20 && State.F_Gandhi_State == 3)
            return await SceneAsync("Gｶﾞﾝｼﾞｰ.ADV", 4);
        if (p == 20 && State.F_Youko_AxeEventDone == 0)
            return await SceneAsync("G1.ADV", 16);
        if (p == 65 && State.F_Kabachahn_EstateSeen == 0)
            return await SceneAsync("Iｾﾞｽ.ADV", 7);
        if (p == 68 && State.F_RagarlRitual_State == 7 && State.F_Annis_State == 0 && State.F_Chizuko_State > 2)
            return await SceneAsync("Gｱﾆｽ.ADV", 1);
        return false;
    }

    private async Task<bool> SceneAsync(string file, int entry)
    {
        await Script.EventAsync(file, (Sys3Int)entry);
        return true;
    }

    /// <summary>
    /// <c>:1039-1176</c>: the rolls for the Cat Bread (24), the Kuriripu (25) and Kapala's scene
    /// (cities 1-16, while she has no rank) — each drawn whether or not its item is owned — then a
    /// girl when the city has one.
    /// </summary>
    private async Task FindAsync()
    {
        // QUIRK: STR-14 — the three rolls are drawn even when the item is owned.
        Script.Rnd(5);
        State.Item_Cur = 24;
        if (Item.Holder == 0 && State.RetVal == 1)
        {
            await CatBreadAsync();
            return;
        }
        Script.Rnd(5);
        State.Item_Cur = 25;
        if (Item.Holder == 0 && State.RetVal == 1)
        {
            await KuriripuAsync();
            return;
        }
        Script.Rnd(5);
        if (State.Prov_Cur < 17 && State.RetVal == 1 && State.N_Kapala_Rank == 0)
        {
            await Script.EventAsync("Gｶﾊﾟｰﾗ.ADV", 1);
            return;
        }
        if (Prov.Girls != 0)
            await GirlAsync();
    }

    /// <summary>
    /// <c>:1055-1176</c>: a girl of the city taken for the Special Room; her description is drawn
    /// (an adjective of five, an occupation of ten), and a third draw may bring a soldier's aside.
    /// </summary>
    private async Task GirlAsync()
    {
        Prov.Girls = Prov.Girls - 1;
        State.N_SpecialRoom_Girls = State.N_SpecialRoom_Girls + 1;
        State.CallArg = 473;
        await WaoAsync(311);
        await WaoAsync(111);
        await TextAsync(1062);                                          // "I found a "
        // QUIRK: STR-14 — the description's draws only choose words.
        Script.Rnd(5);
        if (State.RetVal >= 1 && State.RetVal <= 5)
            await TextAsync(1066 + (State.RetVal.Value - 1) * 4);       // beautiful, neat, sweet, cute, virgin
        await LineBreakAsync();                                         // :1085
        Script.Rnd(10);
        if (State.RetVal >= 1 && State.RetVal <= 10)
            await TextAsync(1089 + (State.RetVal.Value - 1) * 4);       // the occupation
        await TextAsync(1128);                                          // R "the King should like."
        await WaoAsync(1);
        await WaoAsync(0);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 1136);
        await SpeakAsync(111, 1143);
        Script.Rnd(3);
        if (State.RetVal == 1)
        {
            await SpeakAsync(112, 1150);
            await SpeakAsync(121, 1161);
            await SpeakAsync(112, 1170);
        }
    }

    /// <summary><c>L_01aaf</c> (<c>:1236-1348</c>): the Cat Bread (item 24) found and stored.</summary>
    private async Task CatBreadAsync()
    {
        Item.Holder = 100;
        State.CallArg = 261;
        await WaoAsync(311);
        await SpeakAsync(111, 1242);
        State.CallArg = 1;
        await WaoAsync(321);
        await SpeakAsync(121, 1249);
        await SpeakAsync(113, 1254, 1261);
        await SpeakAsync(121, 1268);
        await SpeakAsync(113, 1275);
        await SpeakAsync(121, 1280);
        await SpeakAsync(111, 1287);
        await SpeakAsync(121, 1298);
        await SpeakAsync(111, 1305);
        await SpeakAsync(121, 1312);
        await SpeakAsync(111, 1317);
        await SpeakAsync(121, 1322);
        await SpeakAsync(111, 1327);
        await SpeakAsync(121, 1334);
        await SpeakAsync(111, 1345);
    }

    /// <summary><c>L_01cb7</c> (<c>:1349-1503</c>): the Kuriripu (item 25) found and stored.</summary>
    private async Task KuriripuAsync()
    {
        Item.Holder = 100;
        State.CallArg = 261;
        await WaoAsync(311);
        await SpeakAsync(111, 1355);
        State.CallArg = 1;
        await WaoAsync(321);
        await SpeakAsync(121, 1364);
        await SpeakAsync(111, 1371);
        await SpeakAsync(113, 1376);
        await SpeakAsync(121, 1385);
        await SpeakAsync(111, 1396);
        await SpeakAsync(121, 1405);
        await SpeakAsync(111, 1410, 1419);
        await SpeakAsync(121, 1426);
        await SpeakAsync(111, 1433);
        await SpeakAsync(121, 1442);
        await WaoAsync(312);
        State.CallArg = 262;
        await WaoAsync(311);
        await SpeakAsync(111, 1456);
        await WaoAsync(322);
        State.CallArg = 261;
        await WaoAsync(321);
        await SpeakAsync(121, 1464);
        await SpeakAsync(111, 1473);
        await WaoAsync(312);
        await SpeakAsync(121, 1483);
        await WaoAsync(322);
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(113, 1491);
        await SpeakAsync(111, 1498);
    }

    // -----------------------------------------------------------------------------------------
    // Bolster Defense
    // -----------------------------------------------------------------------------------------

    /// <summary>
    /// <c>L_01f72</c> (<c>:1504-1731</c>): the cost (<c>Defense × 10 × 2</c>, 5 at 0) against the
    /// budget, Reinforce or Don't; Reinforce spends one of the week's actions, pays and raises the
    /// defence. Both go back to the city's header.
    /// </summary>
    private async Task BolsterAsync()
    {
        await WaoAsync(0);
        State.CallArg = 1;
        await WaoAsync(311);
        await SpeakAsync(111, 1510);
        State.CallArg = 6;
        await WaoAsync(321);
        await WaoAsync(121);
        await TextAsync(1523);                                          // "Understood. The expenses will be..."
        await WaoAsync(1);
        State.Tmp0001 = Prov.Defense * 10 * 2;
        if (State.Tmp0001 == 0)
            State.Tmp0001 = 5;
        await ValuesAsync(1532, State.Tmp0001);                         // "n00K GOLD in total."
        await WaoAsync(1);
        await WaoAsync(0);
        await SpeakAsync(123, 1539);
        await WaoAsync(111);
        await ValuesAsync(1546, State.Tmp0001);                         // "Geh... n00K GOLD!?"
        await WaoAsync(1);
        await TextAsync(1553);
        await WaoAsync(1);
        await WaoAsync(0);
        await SpeakAsync(121, 1560);
        await WaoAsync(113);
        await ValuesAsync(1565, State.Tmp0001, State.Gold);             // "Cost: … National Budget: …"

        int choice;
        do                                                              // L_020d1
        {
            await WaitForReleaseAsync();
            var items = new List<SceneMenuItem> { MenuItem(1583) };     // Reinforce
            await LineBreakAsync();
            items.Add(MenuItem(1587));                                  // Don't
            choice = await SelectAsync(items);
        }
        while (choice == 0);

        if (choice == 2)
        {
            await WaoAsync(0);                                          // L_02304
            await SpeakAsync(111, 1704);
            await SpeakAsync(121, 1715);
            await SpeakAsync(111, 1720);
            await WaoAsync(312);
            await WaoAsync(322);
            return;
        }
        State.Ui_CmdOwnCityAvail = State.Ui_CmdOwnCityAvail - 1;       // L_0210f
        await WaoAsync(0);
        await SpeakAsync(111, 1597);
        await SpeakAsync(121, 1604);
        // QUIRK: CMD-05 — no gold check and no cap: Gold floors at 0.
        State.VAR0055 = State.VAR0055 - State.Tmp0001;
        State.Gold = State.Gold - State.Tmp0001;
        Prov.Defense = Prov.Defense + 1;
        await WaoAsync(312);
        await WaoAsync(322);
        State.CallArg = Prov.BgCg;
        await WaoAsync(331);
        State.RetVal = 103;
        await Script.SoundAsync(2);
        await SpeakAsync(123, 1619, 1626, 1629, 1634, 1637);
        await WaoAsync(332);
        await SpeakAsync(113, 1643, 1647, 1651);
        await Script.SoundAsync(3);
        State.CallArg = 1;
        await WaoAsync(311);
        State.CallArg = 6;
        await WaoAsync(321);
        await SpeakAsync(121, 1663, 1666);
        await SpeakAsync(111, 1675);
        if (Prov.Defense > 9)
            await SpeakAsync(121, 1687);
        await WaoAsync(312);
        await WaoAsync(322);
    }
}
