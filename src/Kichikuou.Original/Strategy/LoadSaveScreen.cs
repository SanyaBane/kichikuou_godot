using Kichikuou.Engine.Script;
using Kichikuou.Engine.Sys3;
using Kichikuou.Original.Script;

namespace Kichikuou.Original.Strategy;

/// <summary>
/// <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV</c>, the save screen and the save slots (part G0, decision 6), ported whole and
/// one-to-one; the mode is the caller's <c>RetVal</c> (<c>:1-29</c>): 0 the load list and 1 the
/// save list straight away (<c>Ui_SaveLoadDirect := 1</c>, the title's way in), 2 the Load / Save /
/// Don't menu (<c>COMMAND</c>'s Load/Save), 3 the weekly autosave (<c>MAIN.ADV:20, 83</c>), 10 the
/// persistent page 4 read from every slot (the boot, <c>SET.ADV:340</c>), 11 page 4 written to every
/// slot (the ending); anything else shows the file's error box. The slots are the context's
/// (<see cref="ScriptContext.Saves"/>, <c>ScriptContext.Save.cs</c>).
/// </summary>
/// <remarks>
/// Slot 1 is the autosave, taken at the start of each player phase; a manual save copies it into
/// slot 2-10 (<c>QC n, 1</c>) and writes the slot number into its <c>Tmp0022</c> (STR-11), and a
/// load first makes the chosen slot the autosave (<c>QC 1, n</c>), then loads it: <c>LD n</c>
/// leaves the running code by <see cref="GameLoadedException"/>, and the game goes on after the
/// autosave's call with <c>RetVal</c> 0, which is <see cref="ResumeAsync"/> (<c>:34-35</c>, "Load
/// Complete"). The menus are read as the script builds them (an item's text is captured when
/// printed, <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:129-131</c>); the literals (<c>MS 1, "Auto"</c>, …) come from the
/// player's files.
/// <para>
/// Ours (owner's decision 26): a save from <c>COMMAND</c> (the week loop sets
/// <see cref="ScriptContext.SaveHere"/>) is the game as it is now, not a copy of the autosave — the
/// memory into the slot (<c>QD</c>) resuming in that <c>COMMAND</c> after a load, its window saying so
/// in our words; loading such a slot leaves the autosave as it is. Without <c>SaveHere</c> (the
/// differential tests) the file runs as the original.
/// </para>
/// <para>
/// Ours (owner's decision 29): with the context's <see cref="ScriptContext.Library"/> the saves are its two lists, not
/// slots 1-10 — the autosave a new auto-save (the oldest past ten dropped), the load list every auto-save and manual
/// save, the save list a new manual save and the manual saves to save over (<see cref="SaveLibraryListEvent"/> names
/// each item's save), a load never copied into the auto-save, page 4 read and written once in the library's persistent
/// page instead of in every slot, the notices naming the save in our words. Without it (the differential tests) the
/// file runs on the slots as the original.
/// </para>
/// </remarks>
public sealed class LoadSaveScreen(OriginalScriptContext script, IScenarioSource scenario)
    : CommandFile(script, "ﾛｰﾄﾞｾｰﾌﾞ.ADV", scenario)
{
    // Variables the save commands address by reference (getCaliArray), by number.
    private const int VarTmp0001 = 1;
    private const int VarTmp0021 = 21;
    private const int VarTmp0022 = 22;
    private const int VarTmp0039 = 39;
    private const int VarUnused0194 = 194;
    private const int VarGlobFate = 195;

    private enum Next
    {
        Menu,
        LoadList,
        LoadSlots,
        SaveList,
        SaveSlots,
        Done,
    }

    /// <summary><c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:1-29</c>: the mode in <c>RetVal</c>.</summary>
    public async Task RunAsync()
    {
        State.Ui_SaveLoadDirect = 0;                                    // :1
        Next next;
        switch (State.RetVal.Value)
        {
            case 0:                                                     // :2-5
                State.Ui_SaveLoadDirect = 1;
                next = Next.LoadList;
                break;
            case 1:                                                     // :6-9
                State.Ui_SaveLoadDirect = 1;
                next = Next.SaveList;
                break;
            case 2:                                                     // :10-12
                next = Next.Menu;
                break;
            case 3:                                                     // :13-15
                await AutosaveAsync();
                return;
            case 10:                                                    // :16-19
                ReadPersistentPage();
                return;
            case 11:                                                    // :20-23
                WritePersistentPage();
                return;
            default:                                                    // :24-29
                await WaoAsync(113);                                    // \L_00c45
                await TextAsync(26);                                    // "ロードセーブ．ＡＤＶエラー"
                await WaoAsync(1);                                      // \L_00d32
                await WaoAsync(0);                                      // \L_00d25
                return;
        }

        await Script.ShowAsync(new SaveScreenEvent(State.RetVal, true));
        Sys3Int mode = State.RetVal;
        while (next != Next.Done)
        {
            next = next switch
            {
                Next.Menu => await MenuAsync(),
                Next.LoadList => await LoadListAsync(),
                Next.LoadSlots => await LoadSlotsAsync(),
                Next.SaveList => await SaveListAsync(),
                Next.SaveSlots => await SaveSlotsAsync(),
                _ => throw new InvalidOperationException($"No step {next}."),
            };
        }
        await Script.ShowAsync(new SaveScreenEvent(mode, false));
    }

    /// <summary>
    /// <c>ﾛｰﾄﾞｾｰﾌﾞ.ADV:34-35, 59-81</c> — where a load comes back: <c>LD</c> restored the slot,
    /// whose <c>QD 1</c> (<c>:33</c>) is now answered with <c>RetVal</c> 0: the map redrawn (its
    /// scratch <c>Tmp0021-0040</c> kept in the buffer), "Load Complete" with the slot's number and
    /// date, the slot's music again; then back to the autosave's caller.
    /// </summary>
    public async Task ResumeAsync()
    {
        if (State.RetVal == 0)                                          // :34-36
        {
            await LoadCompleteAsync();
            return;
        }
        await AfterSaveAsync();
    }

    /// <summary><c>L_000c3</c> (<c>:30-58</c>): the autosave into slot 1, then page 4 to every slot and the notice.</summary>
    private async Task AutosaveAsync()
    {
        State.Tmp0021 = State.Week;                                     // :31
        State.Tmp0022 = 1;                                              // :32
        if (Script.Library is not null)
            Script.SaveAuto();                                          // ours (decision 29): a new auto-save
        else
            Script.SaveAll(1);                                          // :33 QD 1
        await ResumeAsync();
    }

    /// <summary><c>:37-58</c> — after <c>QD 1</c>: an error box, or page 4 to every slot and the "Auto-Save" notice.</summary>
    private async Task AfterSaveAsync()
    {
        if (State.RetVal > 200)                                         // :37-45
        {
            await WaoAsync(113);                                        // \L_00c45
            await EngineAsync("ZM", 24);
            await TextAsync(41);                                        // "＜　An Error Occurred　＞"
            await WaoAsync(1);                                          // \L_00d32
            await WaoAsync(0);                                          // \L_00d25
            await EndAsync();                                           // @L_001ad
            return;
        }
        WritePersistentPage();                                          // :46 \L_00ad6
        await WaoAsync(113);                                            // :47 \L_00c45
        await EngineAsync("ZM", 24);
        await TextAsync(50);                                            // "＜　Auto-Save　＞"
        State.RetVal = 1;                                               // :52
        await SlotInfoAsync();                                          // :53 \L_00b31
        await WaoAsync(1);                                              // :54 \L_00d32
        await WaoAsync(0);                                              // :55 \L_00d25
        await EngineAsync("B2", 1, 1, 0, 0, 0, 0);                      // :56
        await EngineAsync("ZM", 24);                                    // :57
    }

    /// <summary><c>L_00155</c> (<c>:59-81</c>): "Load Complete".</summary>
    private async Task LoadCompleteAsync()
    {
        await Script.ShowAsync(new SaveScreenEvent(Sys3Int.Zero, false));
        State.Buf_Cur = 0;                                              // :60
        for (int i = 0; i < 20; i++)                                    // :61 NB Buf_Val, Tmp0021, 20
            State.Buffer[State.Buf_Cur.Value + i].Val = State.Memory.Var(VarTmp0021 + i);
        await Script.WaoMapCallAsync(3);                                // :62-63
        await Script.MapCallAsync(0);                                   // :64-65
        State.Buf_Cur = 0;                                              // :66
        for (int i = 0; i < 20; i++)                                    // :67 NB Tmp0021, Buf_Val, 20
            State.Memory.Var(VarTmp0021 + i) = State.Buffer[State.Buf_Cur.Value + i].Val;
        await WaoAsync(113);                                            // :68 \L_00c45
        await EngineAsync("ZM", 24);
        await TextAsync(71);                                            // "＜　Load Complete　＞"
        State.RetVal = State.Tmp0022;                                   // :73
        await SlotInfoAsync();                                          // :74 \L_00b31
        await WaoAsync(1);                                              // :75 \L_00d32
        await WaoAsync(0);                                              // :76 \L_00d25
        await Script.MusicAsync(State.CurrentBgm);                      // :77 SS CurrentBgm
        await EndAsync();
    }

    /// <summary><c>L_001ad</c> (<c>:78-81</c>): the selection window back, the message size, return.</summary>
    private async Task EndAsync()
    {
        await EngineAsync("B2", 1, 1, 0, 0, 0, 0);
        await EngineAsync("ZM", 24);
    }

    /// <summary><c>L_001c2</c> (<c>:82-104</c>): Load / Save / Don't.</summary>
    private async Task<Next> MenuAsync()
    {
        await EngineAsync("B1", 2, 250, 50, 16 * 3, 100, 1);           // :83
        await EngineAsync("B2", 2, 1, 0, 0, 0, 0);                      // :84
        await WaitForReleaseAsync();                                    // :85-89 L_001e2
        var items = new List<SceneMenuItem> { MenuItem(91) };           // $L_0022f$ 'Load' $
        await LineBreakAsync();                                         // :94
        items.Add(MenuItem(95));                                        // $L_006a2$ 'Save' $
        await LineBreakAsync();                                         // :98
        items.Add(MenuItem(99));                                        // $L_0022a$ 'Don’t' $
        switch (await SelectAsync(items))                               // :102 ]
        {
            case 1:
                return Next.LoadList;
            case 2:
                return Next.SaveList;
            default:                                                    // L_0022a, or a cancel: @L_001ad
                await EndAsync();
                return Next.Done;
        }
    }

    /// <summary><c>L_0022f</c> (<c>:105-119</c>): the load window and its header.</summary>
    private async Task<Next> LoadListAsync()
    {
        await EngineAsync("B1", 2, 250, 50, 16 * 18, 100, 1);          // :106
        await EngineAsync("B2", 2, 1, 0, 0, 0, 0);                      // :107
        await WaoAsync(113);                                            // :108 \L_00c45
        await EngineAsync("ZM", 24);
        await TextAsync(111);                                           // "＜　Data Load　＞"
        await EngineAsync("ZM", 16);
        await TextAsync(115);                                           // "Please select what slot to load from." …
        return Next.LoadSlots;
    }

    /// <summary>
    /// <c>L_0027f</c> (<c>:120-238</c>, <c>L_00529-L_0061d</c> <c>:239-299</c>): slots 1-10 as
    /// "Auto", "1"-"9" with their dates; an empty slot is an item that lists them again. A chosen
    /// slot becomes the autosave and is loaded; an empty one (the load fails) lists them again.
    /// </summary>
    private async Task<Next> LoadSlotsAsync()
    {
        if (Script.Library is { } library)
            return await LibraryLoadAsync(library);
        var items = new List<SceneMenuItem>();
        var slots = new List<int>();                                    // the slot of each item; 0 = list again
        State.Tmp0021 = 1;                                              // :121
        for (int slot = 1; slot <= 10; slot++)
        {
            int at = 122 + 11 * (slot - 1);                             // LP slot, Tmp0021, 1
            Script.LoadPartial((Sys3Int)slot, VarTmp0021, 1);
            SetLiteral(at + 2);                                         // MS 1, "Auto" / "1" … "9"
            if (State.RetVal > 200)                                     // \L_0067f
            {
                items.Add(EmptySlotItem());
                slots.Add(0);
            }
            if (State.RetVal == 0)                                      // $L_00529$ \L_00622: $ …
            {
                items.Add(new SceneMenuItem(Ref(at + 7), [SlotDateText(304)]));
                slots.Add(slot);
            }
        }
        int choice = await SelectAsync(items);                          // :232 ]
        if (choice == 0)
        {
            await WaoAsync(0);                                          // :233 \L_00d25
            return await CancelAsync();
        }
        int chosen = slots[choice - 1];
        if (chosen == 0)
            return Next.LoadSlots;                                      // $L_0027f$
        await WaoAsync(0);                                              // \L_00d25
        ReadPersistentPage();                                           // \L_00a7b
        // Ours (owner's decision 26): a save of the moment is not made the autosave; Auto keeps its turn's start.
        if (chosen > 1)
            Script.CopySave(1, (Sys3Int)chosen, WeekLoop.IsMomentSave); // QC 1, n
        Script.LoadAll((Sys3Int)chosen);                                // LD n: leaves on success
        return Next.LoadSlots;                                          // @L_0061d → @L_0027f
    }

    /// <summary>
    /// Ours (decision 29): the saves a list of the library offers, an item each — to load, the auto-saves newest first,
    /// then the manual saves by number; to save, a new manual save (null) first, then the manual saves to save over.
    /// </summary>
    public static IReadOnlyList<SaveId?> Items(ISaveLibrary library, bool save) => save
        ? [null, .. library.Manuals.Select(id => (SaveId?)id)]
        : [.. library.Autos.Concat(library.Manuals).Select(id => (SaveId?)id)];

    /// <summary>
    /// Ours (decision 29): the load list over the library — the auto-saves newest first, then the manual saves by number;
    /// a chosen save is loaded after page 4 is read (<c>L_00a7b</c>), never made the auto-save; one that is gone lists
    /// them again.
    /// </summary>
    private async Task<Next> LibraryLoadAsync(ISaveLibrary library)
    {
        var ids = Items(library, save: false);
        await Script.ShowAsync(new SaveLibraryListEvent(false, ids));
        int choice = await SelectAsync([.. ids.Select(LibraryItem)]);
        await WaoAsync(0);                                              // \L_00d25
        if (choice == 0)
            return await CancelAsync();
        ReadPersistentPage();                                           // \L_00a7b
        Script.LoadSave(ids[choice - 1]!.Value, KeepPersistentPage);    // leaves on success
        return Next.LoadSlots;
    }

    /// <summary>
    /// Ours (decision 29): after a load the library's page 4 over the save's copy — the original's slots all held the
    /// same page, a save of the library may hold an older one — the loaded cursors and loop counter kept.
    /// </summary>
    private void KeepPersistentPage()
    {
        var (counter, fate, cg, menu) = (State.Tmp0040, State.Glob_FateCur, State.Glob_CgCur, State.Glob_MenuCur);
        ReadPersistentPage();
        (State.Tmp0040, State.Glob_FateCur, State.Glob_CgCur, State.Glob_MenuCur) = (counter, fate, cg, menu);
    }

    /// <summary>Ours (decision 29): the save list over the library — a new manual save first, then the manual saves to save over.</summary>
    private async Task<Next> LibrarySaveAsync(ISaveLibrary library)
    {
        var ids = Items(library, save: true);
        await Script.ShowAsync(new SaveLibraryListEvent(true, ids));
        int choice = await SelectAsync([.. ids.Select(LibraryItem)]);
        if (choice == 0)
        {
            await WaoAsync(0);                                          // \L_00d25
            return await CancelAsync();
        }
        var manuals = library.Manuals;
        int number = ids[choice - 1]?.Number ?? (manuals.Count == 0 ? 1 : manuals[^1].Number + 1);
        State.Tmp0022 = (Sys3Int)number;
        if (Script.SaveHere is { } here)
        {
            State.Tmp0021 = State.Week;                                 // as SaveSlotsAsync's save of the moment
            Script.SaveManual(ids[choice - 1]?.Number, here);
        }
        else
            Script.SaveManual(ids[choice - 1]?.Number, null);           // a copy of the newest auto-save, as QC n, 1
        WritePersistentPage();                                          // :511 \L_00ad6
        return Next.SaveSlots;
    }

    /// <summary>A library list's item, an empty slot's line (<c>:319-320</c>) printing our label as string 1.</summary>
    private SceneMenuItem LibraryItem(SaveId? id) =>
        new(Ref(319), [new CapturedText(Ref(320), [], [id is not { } save ? "New save" : LibraryLabel(save)])]);

    private static string LibraryLabel(SaveId id) => id.Kind == SaveKind.Auto ? "Auto-save" : $"Manual save {id.Number}";

    /// <summary><c>:233-238</c> / <c>:452-457</c> — a cancelled list: back to the caller when entered directly, else the menu.</summary>
    private async Task<Next> CancelAsync()
    {
        if (State.Ui_SaveLoadDirect == 1)
        {
            await EngineAsync("B2", 1, 1, 0, 0, 0, 0);
            return Next.Done;
        }
        return Next.Menu;
    }

    /// <summary><c>L_0067f</c> (<c>:317-324</c>): an empty slot, an item that lists the slots again; <c>Tmp0021 + 1</c> (STR-23).</summary>
    private SceneMenuItem EmptySlotItem()
    {
        var item = new SceneMenuItem(Ref(319), [new CapturedText(Ref(320), [], [State.Strings[1]])]);
        State.Tmp0021 = State.Tmp0021 + 1;                              // QUIRK: STR-23
        return item;
    }

    /// <summary>
    /// <c>L_00622</c> / <c>L_009ee</c>: the slot's week (in <c>Tmp0021</c>) as a date, printed into
    /// the item after its name (string 1) — "［ＬＰ：y, Month m, Week w］".
    /// </summary>
    private CapturedText SlotDateText(int line)
    {
        State.Tmp0001 = State.Tmp0021;
        SetDate();                                                      // \L_00ba9 QUIRK: STR-23
        return new CapturedText(Ref(line), [State.DateYear, State.DateMonth, State.DateWeek], [State.Strings[1]]);
    }

    /// <summary><c>L_006a2</c> (<c>:325-350</c>): the save window, with the autosave's date — what a save stores.</summary>
    private async Task<Next> SaveListAsync()
    {
        await EngineAsync("B1", 2, 250, 50, 16 * 18, 100, 1);          // :326
        await EngineAsync("B2", 2, 1, 0, 0, 0, 0);                      // :327
        await WaoAsync(113);                                            // :328 \L_00c45
        await EngineAsync("ZM", 24);
        await TextAsync(331);                                           // "＜　Saving Data　＞"
        await EngineAsync("ZM", 16);                                    // :333
        if (Script.SaveHere is not null)
        {
            // Ours (owner's decision 26): the save is the game as it is now, so the window says so.
            State.Tmp0001 = State.Week;
            SetDate();
            await Script.ShowAsync(new PortTextEvent($"　ＬＰ：{State.DateYear.Value,4}, Month {State.DateMonth.Value,2}, Week {State.DateWeek.Value,2}"));
            await LineBreakAsync();
            await Script.ShowAsync(new PortTextEvent("　Saving the game as it is now."));
            return Next.SaveSlots;
        }
        Script.LoadPartial(1, VarTmp0021, 1);                           // :334 LP 1, Tmp0021, 1
        State.Tmp0001 = State.Tmp0021;                                  // :335
        SetDate();                                                      // :336 \L_00ba9
        await ValuesAsync(338, State.DateYear, State.DateMonth, State.DateWeek); // "Auto ＬＰ：…" "Saving data from the beginning of this turn."
        return Next.SaveSlots;
    }

    /// <summary>
    /// <c>L_00739</c> (<c>:351-457</c>, <c>L_0099d-L_00a63</c> <c>:458-512</c>): slots 2-10 as
    /// "1"-"9", each with its date or dashes; the chosen one gets a copy of the autosave with its
    /// number in <c>Tmp0022</c>, page 4 goes to every slot, and the slots are listed again.
    /// </summary>
    private async Task<Next> SaveSlotsAsync()
    {
        if (Script.Library is { } library)
            return await LibrarySaveAsync(library);
        var items = new List<SceneMenuItem>();
        for (int slot = 2; slot <= 10; slot++)
        {
            int at = 352 + 11 * (slot - 2);                             // LP slot, Tmp0021, 1
            Script.LoadPartial((Sys3Int)slot, VarTmp0021, 1);
            SetLiteral(at + 2);                                         // MS 1, "1" … "9"
            var texts = new List<CapturedText>();                       // $L_0099d$ …
            if (State.RetVal == 0)
                texts.Add(SlotDateText(489));                           // \L_009ee
            if (State.RetVal > 200)
                texts.Add(new CapturedText(Ref(504), [], [State.Strings[1]])); // \L_00a4b
            items.Add(new SceneMenuItem(Ref(at + 3), texts));
        }
        int choice = await SelectAsync(items);                          // :451 ]
        if (choice == 0)
        {
            await WaoAsync(0);                                          // :452 \L_00d25
            return await CancelAsync();
        }
        State.Tmp0022 = (Sys3Int)(choice + 1);                          // L_0099d … L_009e5
        if (Script.SaveHere is { } here)
        {
            // Ours (owner's decision 26): the game as it is now, resuming in COMMAND after a load; the slot's
            // week in Tmp0021 as the autosave keeps it (:31), its number in Tmp0022 as a copy gets it (STR-11).
            State.Tmp0021 = State.Week;
            Script.SaveAll(State.Tmp0022, here);
        }
        else
        {
            Script.CopySave(State.Tmp0022, 1);                          // :509 QC Tmp0022, 1  QUIRK: STR-11
            Script.SavePartial(State.Tmp0022, VarTmp0022, 1);           // :510 QP Tmp0022, Tmp0022, 1
        }
        WritePersistentPage();                                          // :511 \L_00ad6
        return Next.SaveSlots;                                          // :512 @L_00739
    }

    /// <summary>
    /// <c>L_00a7b</c> (<c>:513-528</c>): page 4 (<c>VAR0194</c> and the 2100 slots from
    /// <c>Glob_Fate</c>) read from every slot 1-10 that has a save, in order — the last one wins;
    /// each slot is probed by loading its <c>Tmp0039</c> (STR-23).
    /// </summary>
    private void ReadPersistentPage()
    {
        State.Tmp0040 = 1;
        State.Glob_FateCur = 0;
        State.Glob_CgCur = 0;
        State.Glob_MenuCur = 0;
        if (Script.Library is not null)
        {
            // Ours (decision 29, STR-31): the library's persistent page, once.
            Script.LoadPersistent(VarUnused0194, 1);
            Script.LoadPersistent(VarGlobFate, 2100);
            return;
        }
        while (true)                                                    // L_00a8e
        {
            Script.LoadPartial(State.Tmp0040, VarTmp0039, 1);           // QUIRK: STR-23
            if (State.RetVal < 200)
            {
                Script.LoadPartial(State.Tmp0040, VarUnused0194, 1);
                Script.LoadPartial(State.Tmp0040, VarGlobFate, 2100);
            }
            if (State.Tmp0040 < 10)
            {
                State.Tmp0040 = State.Tmp0040 + 1;
                continue;
            }
            return;
        }
    }

    /// <summary><c>L_00ad6</c> (<c>:529-544</c>): page 4 written into every slot 1-10 that has a save.</summary>
    private void WritePersistentPage()
    {
        State.Tmp0040 = 1;
        State.Glob_FateCur = 0;
        State.Glob_CgCur = 0;
        State.Glob_MenuCur = 0;
        if (Script.Library is not null)
        {
            // Ours (decision 29, STR-31): the library's persistent page, once, not every save rewritten.
            Script.SavePersistent(VarUnused0194, 1);
            Script.SavePersistent(VarGlobFate, 2100);
            return;
        }
        while (true)                                                    // L_00ae9
        {
            Script.LoadPartial(State.Tmp0040, VarTmp0039, 1);           // QUIRK: STR-23
            if (State.RetVal < 200)
            {
                Script.SavePartial(State.Tmp0040, VarUnused0194, 1);
                Script.SavePartial(State.Tmp0040, VarGlobFate, 2100);
            }
            if (State.Tmp0040 < 10)
            {
                State.Tmp0040 = State.Tmp0040 + 1;
                continue;
            }
            return;
        }
    }

    /// <summary>
    /// <c>L_00b31</c> (<c>:545-567</c>): "Data Number（n）" (n = <c>Tmp0022</c>) and this week's
    /// date; <c>LT RetVal, Tmp0001</c> first reads the slot's file time into <c>Tmp0001-0006</c>,
    /// of which only <c>Tmp0006</c> (the second) outlives the date that follows (STR-22).
    /// </summary>
    private async Task SlotInfoAsync()
    {
        await EngineAsync("ZM", 16);                                    // :546
        if (Script.Library is not null && Script.LastSave is { } save)
        {
            // Ours (decision 29): the save in our words for "Data Number (n)", its time as LT reads it.
            Script.ReadSaveTime(save, VarTmp0001);                      // QUIRK: STR-22
            await LineBreakAsync();
            await Script.ShowAsync(new PortTextEvent("　" + LibraryLabel(save)));
            await LineBreakAsync();
        }
        else
        {
            Script.ReadSaveTime(State.RetVal, VarTmp0001);              // :547 LT RetVal, Tmp0001  QUIRK: STR-22
            await ValuesAsync(549, State.Tmp0022);                      // "Data Number（n）"
        }
        State.Tmp0001 = State.Week;                                     // :555
        SetDate();                                                      // :556 \L_00ba9
        await ValuesAsync(558, State.DateYear, State.DateMonth, State.DateWeek); // "ＬＰ：y, Month m, Week w"
    }

    /// <summary>
    /// <c>L_00ba9</c> (<c>:568-584</c>): <c>DateYear/Month/Week</c> from the week in <c>Tmp0001</c>
    /// (48 weeks a year from year 3, 4 weeks a month), through <c>Tmp0001-0005</c>.
    /// </summary>
    private void SetDate()
    {
        State.Tmp0005 = State.Tmp0001;
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
}
